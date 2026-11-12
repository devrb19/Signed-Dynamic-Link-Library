
using System.Web.Mvc;

namespace HtmlToPdf.MvcDemo.Models
{
    public class PdfDemoViewModel
    {
        public string Url { get; set; }

        [AllowHtml]
        public string Html { get; set; }

        public string BaseUrl { get; set; }

        public string FileName { get; set; }

        public PdfDemoViewModel()
        {
            FileName = "document.pdf";
        }
    }
}
