using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaFontFamily = System.Windows.Media.FontFamily;

namespace AIHub.Services;

public static partial class ExecutorMarkdownDocumentBuilder
{
    public static FlowDocument Build(string markdown)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(28),
            FontFamily = new MediaFontFamily("Segoe UI"),
            FontSize = 15,
            LineHeight = 23,
            Foreground = new SolidColorBrush(MediaColor.FromRgb(229, 234, 244))
        };
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var paragraphLines = new List<string>();
        List? activeList = null;
        var activeListOrdered = false;
        var inCodeBlock = false;
        var codeLines = new List<string>();

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0)
            {
                return;
            }

            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            AppendInlines(paragraph, string.Join(" ", paragraphLines));
            document.Blocks.Add(paragraph);
            paragraphLines.Clear();
        }

        void FlushList()
        {
            if (activeList is null)
            {
                return;
            }

            document.Blocks.Add(activeList);
            activeList = null;
        }

        void FlushCode()
        {
            if (codeLines.Count == 0)
            {
                return;
            }

            document.Blocks.Add(new Paragraph(new Run(string.Join(Environment.NewLine, codeLines)))
            {
                FontFamily = new MediaFontFamily("Consolas"),
                FontSize = 13,
                LineHeight = 20,
                Background = new SolidColorBrush(MediaColor.FromRgb(10, 18, 31)),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 4, 0, 14)
            });
            codeLines.Clear();
        }

        foreach (var sourceLine in lines)
        {
            var line = sourceLine.TrimEnd();
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                FlushList();
                if (inCodeBlock)
                {
                    FlushCode();
                }

                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                codeLines.Add(sourceLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            var heading = HeadingRegex().Match(line);
            if (heading.Success)
            {
                FlushParagraph();
                FlushList();
                var level = heading.Groups[1].Value.Length;
                var paragraph = new Paragraph
                {
                    FontSize = level switch
                    {
                        1 => 30,
                        2 => 23,
                        _ => 18
                    },
                    FontWeight = FontWeights.SemiBold,
                    Foreground = MediaBrushes.White,
                    Margin = new Thickness(0, level == 1 ? 0 : 12, 0, 10)
                };
                AppendInlines(paragraph, heading.Groups[2].Value.Trim());
                document.Blocks.Add(paragraph);
                continue;
            }

            var bullet = BulletRegex().Match(line);
            var numbered = NumberedRegex().Match(line);
            if (bullet.Success || numbered.Success)
            {
                FlushParagraph();
                var ordered = numbered.Success;
                if (activeList is null || activeListOrdered != ordered)
                {
                    FlushList();
                    activeListOrdered = ordered;
                    activeList = new List
                    {
                        MarkerStyle = ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                        Margin = new Thickness(18, 0, 0, 12)
                    };
                }

                var text = (ordered ? numbered : bullet).Groups[1].Value.Trim();
                var paragraph = new Paragraph { Margin = new Thickness(0, 2, 0, 2) };
                AppendInlines(paragraph, text);
                activeList.ListItems.Add(new ListItem(paragraph));
                continue;
            }

            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                FlushParagraph();
                FlushList();
                var paragraph = new Paragraph
                {
                    Margin = new Thickness(12, 4, 0, 14),
                    Padding = new Thickness(12, 8, 12, 8),
                    Background = new SolidColorBrush(MediaColor.FromRgb(29, 45, 69)),
                    Foreground = new SolidColorBrush(MediaColor.FromRgb(190, 204, 229))
                };
                AppendInlines(paragraph, line[2..].Trim());
                document.Blocks.Add(paragraph);
                continue;
            }

            if (line is "---" or "***")
            {
                FlushParagraph();
                FlushList();
                document.Blocks.Add(new BlockUIContainer(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(MediaColor.FromRgb(52, 68, 94)),
                    Margin = new Thickness(0, 8, 0, 14)
                }));
                continue;
            }

            FlushList();
            paragraphLines.Add(line.Trim());
        }

        FlushParagraph();
        FlushList();
        if (inCodeBlock)
        {
            FlushCode();
        }

        return document;
    }

    private static void AppendInlines(Paragraph paragraph, string text)
    {
        var offset = 0;
        foreach (Match match in InlineRegex().Matches(text))
        {
            if (match.Index > offset)
            {
                paragraph.Inlines.Add(new Run(text[offset..match.Index]));
            }

            var token = match.Value;
            if (token.StartsWith("**", StringComparison.Ordinal))
            {
                paragraph.Inlines.Add(new Run(token[2..^2]) { FontWeight = FontWeights.SemiBold });
            }
            else
            {
                paragraph.Inlines.Add(new Run(token[1..^1])
                {
                    FontFamily = new MediaFontFamily("Consolas"),
                    Background = new SolidColorBrush(MediaColor.FromRgb(20, 31, 48))
                });
            }

            offset = match.Index + match.Length;
        }

        if (offset < text.Length)
        {
            paragraph.Inlines.Add(new Run(text[offset..]));
        }
    }

    [GeneratedRegex(@"^(#{1,3})\s+(.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\s*[-*]\s+(.+)$")]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"^\s*\d+[.)]\s+(.+)$")]
    private static partial Regex NumberedRegex();

    [GeneratedRegex(@"\*\*[^*\r\n]+\*\*|`[^`\r\n]+`")]
    private static partial Regex InlineRegex();
}
