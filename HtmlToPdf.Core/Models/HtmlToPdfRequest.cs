namespace HtmlToPdf.Core.Models
{
    public class HtmlToPdfRequest
    {
        // URL of an existing webpage
        public string Url { get; set; }

        // Raw HTML content
        public string Html { get; set; }

        // Optional base URL for relative resources in raw HTML
        public string BaseUrl { get; set; }

        // Desired PDF filename
        public string FileName { get; set; }
    }
}