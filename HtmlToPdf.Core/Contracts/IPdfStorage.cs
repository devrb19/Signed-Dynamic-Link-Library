namespace HtmlToPdf.Core.Contracts
{
    public interface IPdfStorage
    {
        string Save(byte[] pdfBytes);

        byte[] Read(string documentId);

        bool Delete(string documentId);
    }
}