using System;
using System.IO;
using Portable.Xaml;

namespace A3sist.Shared.Utils
{
    public static class XamlHelper
    {
        /// <summary>
        /// Converts a XAML string to a simple HTML wrapper.
        /// </summary>
        public static string ConvertXamlToHtml(string xaml)
        {
            if (string.IsNullOrEmpty(xaml))
                return string.Empty;

            try
            {
                // Parse XAML into an object
                var reader = new StringReader(xaml);
                var xamlObject = XamlServices.Load(reader);

                // Convert to HTML (simplified example)
                return $"<div class=\"xaml-content\">{xaml}</div>";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting XAML to HTML: {ex.Message}");
                return $"<div class=\"xaml-error\">Error displaying content: {ex.Message}</div>";
            }
        }

        /// <summary>
        /// Converts a simple HTML string to XAML FlowDocument wrapper.
        /// </summary>
        public static string ConvertHtmlToXaml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                // Wrap HTML content in a FlowDocument
                return $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{html}</FlowDocument>";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting HTML to XAML: {ex.Message}");
                return $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>Error displaying content: {ex.Message}</Paragraph></FlowDocument>";
            }
        }

        /// <summary>
        /// Returns a simple XAML resource string.
        /// </summary>
        public static string GetXamlResource(string resourceName)
        {
            // Example placeholder resource
            return $@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Paragraph>Resource: {resourceName}</Paragraph>
</FlowDocument>";
        }
    }
}
