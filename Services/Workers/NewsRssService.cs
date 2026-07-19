using System.ServiceModel.Syndication;
using System.Xml;
using ServiceApotheke.API.Models.DTOs;

namespace ServiceApotheke.API.Services.Workers
{
    public class NewsRssService : BackgroundService
    {
        private readonly ILogger<NewsRssService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        
        // Cache to store the latest news items safely
        private static List<NewsItemDto> _cachedNews = new();
        private static readonly object _cacheLock = new();

        private readonly string[] _feedUrls = new[]
        {
            "https://www.pharmazeutische-zeitung.de/fileadmin/rss/pz_online_rss.php",
            "https://www.apotheke-adhoc.de/nachrichten/rss.xml"
        };

        public NewsRssService(ILogger<NewsRssService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public static List<NewsItemDto> GetLatestNews()
        {
            lock (_cacheLock)
            {
                return _cachedNews.ToList();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateNewsCacheAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching RSS news feeds.");
                }

                // Refresh every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task UpdateNewsCacheAsync(CancellationToken cancellationToken)
        {
            var allItems = new List<NewsItemDto>();
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            foreach (var url in _feedUrls)
            {
                try
                {
                    var response = await client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var xmlReader = XmlReader.Create(stream);
                    var feed = SyndicationFeed.Load(xmlReader);

                    if (feed != null)
                    {
                        var sourceName = feed.Title?.Text ?? (url.Contains("pz_online") ? "PZ Online" : "Apotheke Adhoc");
                        
                        foreach (var item in feed.Items)
                        {
                            try
                            {
                                allItems.Add(new NewsItemDto
                                {
                                    Title = item.Title?.Text ?? "Kein Titel",
                                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                                    Description = item.Summary?.Text ?? "",
                                    PubDate = item.PublishDate.UtcDateTime,
                                    Source = sourceName
                                });
                            }
                            catch
                            {
                                // Fallback if PublishDate fails to parse (common in some German RSS feeds)
                                allItems.Add(new NewsItemDto
                                {
                                    Title = item.Title?.Text ?? "Kein Titel",
                                    Link = item.Links.FirstOrDefault()?.Uri.ToString() ?? "",
                                    Description = item.Summary?.Text ?? "",
                                    PubDate = DateTime.UtcNow,
                                    Source = sourceName
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to fetch or parse RSS from {url}");
                }
            }

            // Sort descending by date and take top 20
            var topItems = allItems.OrderByDescending(i => i.PubDate).Take(20).ToList();

            lock (_cacheLock)
            {
                _cachedNews = topItems;
            }

            _logger.LogInformation($"Updated news cache with {topItems.Count} items.");
        }
    }
}
