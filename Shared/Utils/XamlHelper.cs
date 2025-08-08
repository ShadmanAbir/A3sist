using System;
using System.IO;
using System.Windows.Markup;
using System.Xml;

namespace CodeAssist.Shared.Utils
{
    public static class XamlHelper
    {
        public static string ConvertXamlToHtml(string xaml)
        {
            if (string.IsNullOrEmpty(xaml))
                return string.Empty;

            try
            {
                // Parse the XAML
                var reader = new StringReader(xaml);
                var xmlReader = XmlReader.Create(reader);
                var xamlObject = XamlReader.Load(xmlReader);

                // Convert to HTML (simplified example)
                // In a real implementation, you would use a proper XAML to HTML converter
                return $"<div class=\"xaml-content\">{xaml}</div>";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting XAML to HTML: {ex.Message}");
                return $"<div class=\"xaml-error\">Error displaying content: {ex.Message}</div>";
            }
        }

        public static string ConvertHtmlToXaml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
                // Convert HTML to XAML (simplified example)
                // In a real implementation, you would use a proper HTML to XAML converter
                return $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{html}</FlowDocument>";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting HTML to XAML: {ex.Message}");
                return $"<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Paragraph>Error displaying content: {ex.Message}</Paragraph></FlowDocument>";
            }
        }

        public static string GetXamlResource(string resourceName)
        {
            // In a real implementation, this would load XAML resources from embedded resources
            // For this example, we'll return a simple XAML string
            return $@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Paragraph>Resource: {resourceName}</Paragraph>
</FlowDocument>";
        }
    }
}