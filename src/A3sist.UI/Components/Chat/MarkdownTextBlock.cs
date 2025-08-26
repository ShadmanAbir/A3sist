using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// A simple markdown text block that supports basic markdown formatting
    /// </summary>
    public class MarkdownTextBlock : TextBlock
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MarkdownTextBlock),
                new PropertyMetadata(string.Empty, OnTextChanged));

        /// <summary>
        /// Gets or sets the markdown text to display
        /// </summary>
        public new string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownTextBlock markdownTextBlock)
            {
                markdownTextBlock.UpdateFormattedText();
            }
        }

        /// <summary>
        /// Updates the formatted text with basic markdown parsing
        /// </summary>
        private void UpdateFormattedText()
        {
            try
            {
                Inlines.Clear();
                
                if (string.IsNullOrEmpty(Text))
                    return;

                ParseAndAddContent(Text);
            }
            catch (Exception ex)
            {
                // Fallback to plain text
                System.Diagnostics.Debug.WriteLine($"Error parsing markdown: {ex.Message}");
                Inlines.Clear();
                Inlines.Add(new Run(Text));
            }
        }

        /// <summary>
        /// Parses the text and adds formatted content
        /// </summary>
        private void ParseAndAddContent(string text)
        {
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Handle code blocks
                if (line.TrimStart().StartsWith("```"))
                {
                    var codeBlock = ParseCodeBlock(lines, ref i);
                    if (codeBlock != null)
                    {
                        Inlines.Add(codeBlock);
                        continue;
                    }
                }
                
                // Parse inline formatting for regular lines
                ParseInlineFormatting(line);
                
                // Add line break if not the last line
                if (i < lines.Length - 1)
                {
                    Inlines.Add(new LineBreak());
                }
            }
        }

        /// <summary>
        /// Parses code blocks
        /// </summary>
        private InlineUIContainer? ParseCodeBlock(string[] lines, ref int currentIndex)
        {
            if (currentIndex >= lines.Length)
                return null;

            var startLine = lines[currentIndex].TrimStart();
            if (!startLine.StartsWith("```"))
                return null;

            var language = startLine.Length > 3 ? startLine.Substring(3).Trim() : "";
            var codeContent = "";
            
            currentIndex++; // Move past opening ```
            
            // Collect code content until closing ```
            while (currentIndex < lines.Length)
            {
                var line = lines[currentIndex];
                if (line.TrimStart().StartsWith("```"))
                {
                    break;
                }
                
                if (codeContent.Length > 0)
                    codeContent += "\n";
                codeContent += line;
                currentIndex++;
            }

            // Create code block
            var codeBlock = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 4)
            };

            var codeText = new TextBlock
            {
                Text = codeContent,
                FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                FontSize = FontSize * 0.9
            };

            codeBlock.Child = codeText;
            
            return new InlineUIContainer(codeBlock);
        }

        /// <summary>
        /// Parses inline formatting like **bold**, *italic*, `code`
        /// </summary>
        private void ParseInlineFormatting(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                Inlines.Add(new Run(""));
                return;
            }

            var remaining = line;
            var position = 0;

            while (position < remaining.Length)
            {
                // Find next formatting marker
                var boldMatch = Regex.Match(remaining.Substring(position), @"\*\*(.+?)\*\*");
                var italicMatch = Regex.Match(remaining.Substring(position), @"\*(.+?)\*");
                var codeMatch = Regex.Match(remaining.Substring(position), @"`(.+?)`");

                var nextMatch = FindEarliestMatch(boldMatch, italicMatch, codeMatch);
                
                if (nextMatch == null)
                {
                    // No more formatting, add remaining text
                    Inlines.Add(new Run(remaining.Substring(position)));
                    break;
                }

                // Add text before the match
                if (nextMatch.Index > 0)
                {
                    Inlines.Add(new Run(remaining.Substring(position, nextMatch.Index)));
                }

                // Add formatted content
                var content = nextMatch.Groups[1].Value;
                Run formattedRun;
                
                if (nextMatch == boldMatch)
                {
                    formattedRun = new Run(content) { FontWeight = FontWeights.Bold };
                }
                else if (nextMatch == italicMatch)
                {
                    formattedRun = new Run(content) { FontStyle = FontStyles.Italic };
                }
                else // code match
                {
                    formattedRun = new Run(content)
                    {
                        FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                        Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                    };
                }
                
                Inlines.Add(formattedRun);
                position += nextMatch.Index + nextMatch.Length;
            }
        }

        /// <summary>
        /// Finds the earliest match among multiple regex matches
        /// </summary>
        private static Match? FindEarliestMatch(params Match[] matches)
        {
            Match? earliest = null;
            
            foreach (var match in matches)
            {
                if (match.Success && (earliest == null || match.Index < earliest.Index))
                {
                    earliest = match;
                }
            }
            
            return earliest?.Success == true ? earliest : null;
        }
    }
}