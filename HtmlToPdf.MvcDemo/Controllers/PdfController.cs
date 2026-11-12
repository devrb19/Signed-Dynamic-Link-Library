
using System;
using System.Web;
using System.Web.Mvc;

using HtmlToPdf.Core.Contracts;
using HtmlToPdf.Core.Models;

using HtmlToPdf.MvcDemo.Infrastructure;
using HtmlToPdf.MvcDemo.Models;

namespace HtmlToPdf.MvcDemo.Controllers
{
    public class PdfController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View(new PdfDemoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generate(PdfDemoViewModel model)
        {
            // This demo endpoint is restricted to local requests.
            // Do not publish it as a public conversion endpoint.
            if (!Request.IsLocal)
            {
                return new HttpStatusCodeResult(
                    403,
                    "This demo is available only for local requests.");
            }

            bool hasUrl =
                !string.IsNullOrWhiteSpace(model.Url);

            bool hasHtml =
                !string.IsNullOrWhiteSpace(model.Html);

            if (hasUrl == hasHtml)
            {
                ModelState.AddModelError(
                    "",
                    "Provide either a webpage URL or raw HTML, not both.");

                return View("Index", model);
            }

            try
            {
                HtmlToPdfRequest request = new HtmlToPdfRequest
                {
                    Url = model.Url,
                    Html = model.Html,
                    BaseUrl = model.BaseUrl,
                    FileName = model.FileName
                };

                IPdfStorage storage;

                IHtmlToPdfService service =
                    PdfServiceFactory.Create(out storage);

                PdfDocumentResult result =
                    service.Convert(request);

                // Read the privately stored PDF.
                byte[] pdfBytes =
                    storage.Read(result.DocumentId);

                // Prevent browser caching.
                Response.Cache.SetCacheability(
                    HttpCacheability.NoCache);

                Response.Cache.SetNoStore();

                // Return the PDF as a download.
                return File(
                    pdfBytes,
                    result.ContentType,
                    result.FileName);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "PDF generation failed: " + ex.Message);

                return View("Index", model);
            }
        }
    }
}
