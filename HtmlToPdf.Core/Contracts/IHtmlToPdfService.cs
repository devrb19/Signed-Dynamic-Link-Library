using HtmlToPdf.Core.Models;

namespace HtmlToPdf.Core.Contracts
{
    public interface IHtmlToPdfService
    {
        PdfDocumentResult Convert(HtmlToPdfRequest request);
    }
}