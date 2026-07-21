using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Extensions.Caching.Memory;
using ServiceApotheke.API.Models.DTOs;

namespace ServiceApotheke.API.Services.Workers
{
    public class NewsRssService
    {
        private readonly ILogger<NewsRssService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        
        private const string CacheKey = "NewsRssCache";

        private readonly string[] _feedUrls = new[]
        {
            "https://www.pharmazeutische-zeitung.de/fileadmin/rss/pz_online_rss.php",
            "https://www.apotheke-adhoc.de/nachrichten/rss.xml"
        };

        public NewsRssService(ILogger<NewsRssService> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public async Task<List<NewsItemDto>> GetLatestNewsAsync(CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(CacheKey, out List<NewsItemDto>? cachedNews) && cachedNews != null)
            {
                return cachedNews;
            }

            var allItems = new List<NewsItemDto>();
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

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
                                // Fallback if PublishDate fails to parse
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

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(CacheKey, topItems, cacheEntryOptions);
            _logger.LogInformation($"Updated news cache with {topItems.Count} items.");

            return topItems;
        }
    }
}
