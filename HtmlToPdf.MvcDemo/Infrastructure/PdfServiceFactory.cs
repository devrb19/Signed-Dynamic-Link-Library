
using System;
using System.Configuration;
using System.IO;
using System.Web.Hosting;

using HtmlToPdf.Core.Contracts;
using HtmlToPdf.Core.Services;

namespace HtmlToPdf.MvcDemo.Infrastructure
{
    public static class PdfServiceFactory
    {
        public static IHtmlToPdfService Create(
            out IPdfStorage storage)
        {
            string edgePath = GetRequiredSetting(
                "HtmlToPdf.EdgeExecutablePath");

            string storagePath = ResolvePath(
                GetRequiredSetting("HtmlToPdf.StoragePath"));

            string tempPath = ResolvePath(
                GetRequiredSetting("HtmlToPdf.TempPath"));

            string timeoutValue = GetRequiredSetting(
                "HtmlToPdf.TimeoutMilliseconds");

            int timeout;

            if (!int.TryParse(timeoutValue, out timeout) ||
                timeout <= 0)
            {
                throw new ConfigurationErrorsException(
                    "Invalid HTML-to-PDF timeout configuration.");
            }

            storage = new FileSystemPdfStorage(storagePath);

            return new HtmlToPdfService(
                storage,
                edgePath,
                tempPath,
                timeout);
        }

        private static string GetRequiredSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(
                    "Missing application setting: " + key);
            }

            return value.Trim();
        }

        private static string ResolvePath(string configuredPath)
        {
            if (configuredPath.StartsWith(
                "~/",
                StringComparison.Ordinal))
            {
                string mappedPath =
                    HostingEnvironment.MapPath(configuredPath);

                if (string.IsNullOrWhiteSpace(mappedPath))
                {
                    throw new ConfigurationErrorsException(
                        "Could not resolve application path: " +
                        configuredPath);
                }

                return Path.GetFullPath(mappedPath);
            }

            string expandedPath =
                Environment.ExpandEnvironmentVariables(
                    configuredPath);

            return Path.GetFullPath(expandedPath);
        }
    }
}
