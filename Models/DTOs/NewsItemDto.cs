namespace ServiceApotheke.API.Models.DTOs
{
    public class NewsItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime PubDate { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
