namespace HtmlToPdf.Core.Models
{
    public class PdfDocumentResult
    {
        // Unique identifier used to retrieve the PDF
        public string DocumentId { get; set; }

        // Name of the generated PDF
        public string FileName { get; set; }

        // PDF MIME type
        public string ContentType { get; set; }

        // File size in bytes
        public long Length { get; set; }

        // PDF creation timestamp
        public System.DateTime CreatedUtc { get; set; }
    }
}