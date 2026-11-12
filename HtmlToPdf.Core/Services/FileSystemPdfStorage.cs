using System;
using System.IO;
using HtmlToPdf.Core.Contracts;

namespace HtmlToPdf.Core.Services
{
    public class FileSystemPdfStorage : IPdfStorage
    {
        private readonly string _rootPath;

        public FileSystemPdfStorage(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException(
                    "Storage folder path is required.",
                    nameof(rootPath));
            }

            _rootPath = Path.GetFullPath(rootPath);

            Directory.CreateDirectory(_rootPath);
        }

        public string Save(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                throw new ArgumentException(
                    "PDF content cannot be empty.",
                    nameof(pdfBytes));
            }

            string documentId = Guid.NewGuid().ToString("N");

            string filePath = GetPdfPath(documentId);

            File.WriteAllBytes(filePath, pdfBytes);

            return documentId;
        }

        public byte[] Read(string documentId)
        {
            string filePath = GetPdfPath(documentId);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The requested PDF document was not found.");
            }

            return File.ReadAllBytes(filePath);
        }

        public bool Delete(string documentId)
        {
            string filePath = GetPdfPath(documentId);

            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);

            return true;
        }

        private string GetPdfPath(string documentId)
        {
            Guid parsedId;

            if (!Guid.TryParseExact(documentId, "N", out parsedId))
            {
                throw new ArgumentException(
                    "Invalid document ID.",
                    nameof(documentId));
            }

            return Path.Combine(
                _rootPath,
                parsedId.ToString("N") + ".pdf");
        }
    }
}