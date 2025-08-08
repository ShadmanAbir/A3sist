using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeAssist.Shared.Utils
{
    public class MarkdownParser
    {
        public static string ParseMarkdownToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            // Convert headers
            markdown = Regex.Replace(markdown, @"^# (.*$)", "<h1>$1</h1>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^## (.*$)", "<h2>$1</h2>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"^### (.*$)", "<h3>$1</h3>", RegexOptions.Multiline);

            // Convert bold and italic
            markdown = Regex.Replace(markdown, @"\*\*(.*?)\*\*", "<strong>$1</strong>");
            markdown = Regex.Replace(markdown, @"\*(.*?)\*", "<em>$1</em>");

            // Convert links
            markdown = Regex.Replace(markdown, @"\[(.*?)\]\((.*?)\)", "<a href=\"$2\">$1</a>");

            // Convert code blocks
            markdown = Regex.Replace(markdown, @"```(.*?)```", "<pre><code>$1</code></pre>", RegexOptions.Singleline);

            // Convert inline code
            markdown = Regex.Replace(markdown, @"`(.*?)`", "<code>$1</code>");

            // Convert lists
            markdown = Regex.Replace(markdown, @"^\* (.*$)", "<li>$1</li>", RegexOptions.Multiline);
            markdown = Regex.Replace(markdown, @"<li>(.*?)</li>", "<ul><li>$1</li></ul>", RegexOptions.Singleline);

            // Convert paragraphs
            markdown = Regex.Replace(markdown, @"^(?!<[a-z]).+$", "<p>$0</p>", RegexOptions.Multiline);

            return markdown;
        }

        public static Dictionary<string, string> ParseMarkdownHeaders(string markdown)
        {
            var headers = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(markdown))
                return headers;

            // Extract headers
            var headerMatches = Regex.Matches(markdown, @"^(#{1,6}) (.*$)", RegexOptions.Multiline);

            foreach (Match match in headerMatches)
            {
                var level = match.Groups[1].Value.Length;
                var text = match.Groups[2].Value;
                var id = text.ToLower().Replace(" ", "-").Replace(".", "");

                headers[id] = $"<h{level} id=\"{id}\">{text}</h{level}>";
            }

            return headers;
        }
    }
}