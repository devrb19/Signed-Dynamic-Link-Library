
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

using HtmlToPdf.Core.Contracts;
using HtmlToPdf.Core.Models;

namespace HtmlToPdf.Core.Services
{
    public class HtmlToPdfService : IHtmlToPdfService
    {
        private readonly IPdfStorage _storage;
        private readonly string _edgeExePath;
        private readonly string _tempRootPath;
        private readonly int _timeoutMilliseconds;

        public HtmlToPdfService(
            IPdfStorage storage,
            string edgeExePath,
            string tempRootPath,
            int timeoutMilliseconds)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            if (string.IsNullOrWhiteSpace(edgeExePath))
                throw new ArgumentException(
                    "Edge executable path is required.");

            if (!File.Exists(edgeExePath))
                throw new FileNotFoundException(
                    "Microsoft Edge executable was not found.",
                    edgeExePath);

            if (string.IsNullOrWhiteSpace(tempRootPath))
                throw new ArgumentException(
                    "Temporary folder path is required.");

            if (timeoutMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutMilliseconds));

            _storage = storage;
            _edgeExePath = Path.GetFullPath(edgeExePath);
            _tempRootPath = Path.GetFullPath(tempRootPath);
            _timeoutMilliseconds = timeoutMilliseconds;

            Directory.CreateDirectory(_tempRootPath);
        }

        public PdfDocumentResult Convert(HtmlToPdfRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            bool hasUrl =
                !string.IsNullOrWhiteSpace(request.Url);

            bool hasHtml =
                !string.IsNullOrWhiteSpace(request.Html);

            if (hasUrl == hasHtml)
            {
                throw new ArgumentException(
                    "Provide either a URL or raw HTML, but not both.");
            }

            string inputAddress;

            if (hasUrl)
            {
                Uri sourceUri = ValidateHttpUrl(request.Url);
                inputAddress = sourceUri.AbsoluteUri;
            }
            else
            {
                inputAddress = null;
            }

            string fileName = GetSafeFileName(request.FileName);

            string workingDirectory = Path.Combine(
                _tempRootPath,
                Guid.NewGuid().ToString("N"));

            string profilePath = Path.Combine(
                workingDirectory,
                "EdgeProfile");

            string pdfPath = Path.Combine(
                workingDirectory,
                "output.pdf");

            try
            {
                Directory.CreateDirectory(workingDirectory);
                Directory.CreateDirectory(profilePath);

                // Raw HTML is written to a temporary HTML file.
                if (hasHtml)
                {
                    string htmlContent = request.Html;

                    if (!string.IsNullOrWhiteSpace(request.BaseUrl))
                    {
                        htmlContent = ApplyBaseUrl(
                            htmlContent,
                            request.BaseUrl);
                    }

                    string htmlPath = Path.Combine(
                        workingDirectory,
                        "input.html");

                    File.WriteAllText(
                        htmlPath,
                        htmlContent,
                        new UTF8Encoding(false));

                    inputAddress = new Uri(htmlPath).AbsoluteUri;
                }

                // Generate PDF using Microsoft Edge.
                RunEdge(
                    inputAddress,
                    pdfPath,
                    profilePath,
                    workingDirectory);

                if (!File.Exists(pdfPath))
                {
                    throw new InvalidOperationException(
                        "Edge did not generate the PDF file.");
                }

                byte[] pdfBytes = File.ReadAllBytes(pdfPath);

                ValidatePdf(pdfBytes);

                // Save PDF through the storage abstraction.
                string documentId = _storage.Save(pdfBytes);

                return new PdfDocumentResult
                {
                    DocumentId = documentId,
                    FileName = fileName,
                    ContentType = "application/pdf",
                    Length = pdfBytes.LongLength,
                    CreatedUtc = DateTime.UtcNow
                };
            }
            finally
            {
                TryDeleteDirectory(workingDirectory);
            }
        }

        private Uri ValidateHttpUrl(string value)
        {
            Uri uri;

            if (!Uri.TryCreate(
                value.Trim(),
                UriKind.Absolute,
                out uri))
            {
                throw new ArgumentException(
                    "A valid absolute URL is required.");
            }

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException(
                    "Only HTTP and HTTPS URLs are supported.");
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new ArgumentException(
                    "URLs containing embedded credentials are not supported.");
            }

            return uri;
        }

        private string ApplyBaseUrl(string html, string baseUrl)
        {
            Uri baseUri = ValidateHttpUrl(baseUrl);

            string baseElement =
                "<base href=\"" +
                WebUtility.HtmlEncode(baseUri.AbsoluteUri) +
                "\" />";

            int headEnd = html.IndexOf(
                "</head>",
                StringComparison.OrdinalIgnoreCase);

            if (headEnd >= 0)
                return html.Insert(headEnd, baseElement);

            int bodyStart = html.IndexOf(
                "<body",
                StringComparison.OrdinalIgnoreCase);

            if (bodyStart >= 0)
            {
                return html.Insert(
                    bodyStart,
                    "<head>" + baseElement + "</head>");
            }

            int htmlStart = html.IndexOf(
                "<html",
                StringComparison.OrdinalIgnoreCase);

            if (htmlStart >= 0)
            {
                int htmlTagEnd = html.IndexOf('>', htmlStart);

                if (htmlTagEnd >= 0)
                {
                    return html.Insert(
                        htmlTagEnd + 1,
                        "<head>" + baseElement + "</head>");
                }
            }

            return "<!DOCTYPE html><html><head>" +
                   baseElement +
                   "</head><body>" +
                   html +
                   "</body></html>";
        }

        private void RunEdge(
            string inputAddress,
            string pdfPath,
            string profilePath,
            string workingDirectory)
        {
            string arguments =
                "--headless=new " +
                "--disable-gpu " +
                "--no-first-run " +
                "--no-default-browser-check " +
                "--disable-extensions " +
                "--user-data-dir=" + QuoteArgument(profilePath) + " " +
                "--no-pdf-header-footer " +
                "--print-to-pdf=" + QuoteArgument(pdfPath) + " " +
                QuoteArgument(inputAddress);

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _edgeExePath,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                if (!process.Start())
                {
                    throw new InvalidOperationException(
                        "Microsoft Edge could not be started.");
                }

                if (!process.WaitForExit(_timeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch
                    {
                        // Preserve the timeout error.
                    }

                    throw new TimeoutException(
                        "HTML-to-PDF conversion exceeded the configured timeout.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "Microsoft Edge exited with code " +
                        process.ExitCode + ".");
                }
            }
        }

        private static string QuoteArgument(string value)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append('"');

            int backslashCount = 0;

            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append('"');
                    backslashCount = 0;
                    continue;
                }

                builder.Append('\\', backslashCount);
                backslashCount = 0;
                builder.Append(character);
            }

            builder.Append('\\', backslashCount * 2);
            builder.Append('"');

            return builder.ToString();
        }

        private static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "document.pdf";

            fileName = Path.GetFileName(fileName.Trim());

            foreach (char invalidCharacter in
                Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '_');
            }

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "document.pdf";

            if (!fileName.EndsWith(
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".pdf";
            }

            return fileName;
        }

        private static void ValidatePdf(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length < 5)
            {
                throw new InvalidOperationException(
                    "The generated PDF is empty or invalid.");
            }

            string signature = Encoding.ASCII.GetString(
                pdfBytes,
                0,
                5);

            if (signature != "%PDF-")
            {
                throw new InvalidOperationException(
                    "The generated file is not a valid PDF.");
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
            }
            catch
            {
                // Temporary cleanup must not hide the
                // original conversion result or error.
            }
        }
    }
}
