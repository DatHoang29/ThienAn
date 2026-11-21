using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Module.VideoWall.WPF.Interaction;

/// <summary>
/// Description: Bộ tiện ích tô màu cú pháp XML (syntax highlighting) hiển thị trên RichTextBox WPF
/// Created date: 13/09/2026
/// </summary>
public static partial class XmlSyntaxHighlighter
{
    private static readonly SolidColorBrush BracketBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)));
    private static readonly SolidColorBrush TagNameBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)));
    private static readonly SolidColorBrush AttrNameBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)));
    private static readonly SolidColorBrush AttrValueBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xB4, 0x53, 0x09)));
    private static readonly SolidColorBrush CommentBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)));
    private static readonly SolidColorBrush SuccessBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)));
    private static readonly SolidColorBrush ErrorBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)));
    private static readonly SolidColorBrush NumberBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7)));
    private static readonly SolidColorBrush DefaultTextBrush = FreezeBrush(new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B)));

    private static readonly HashSet<string> SuccessValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "normal", "true", "ok", "success", "linked", "islinked", "isdecoding", "online"
    };

    private static readonly HashSet<string> ErrorValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "notconnected", "abnormal", "false", "invalidoperation", "badparameters",
        "devicenotexist", "devicebusy", "methodnotallowed", "badxmlformat",
        "notsupport", "error", "failed", "offline", "unlinked"
    };

    private static SolidColorBrush FreezeBrush(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }

    [GeneratedRegex(@"(<!--[\s\S]*?-->|<\?[\s\S]*?\?>|<[/]?[a-zA-Z0-9_:\-]+(?:\s+[^<>]+)?[/]?>|[^<]+)", RegexOptions.Compiled)]
    private static partial Regex XmlTokenRegex();

    [GeneratedRegex(@"([a-zA-Z0-9_:\-]+)\s*=\s*""([^""]*)""", RegexOptions.Compiled)]
    private static partial Regex AttributeRegex();

    [GeneratedRegex(@"(""(?:\\.|[^""\\])*""\s*:|""(?:\\.|[^""\\])*""|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|true|false|null|[{}\[\]:,]|[\s]+|[^""{}\[\]:,\s]+)", RegexOptions.Compiled)]
    private static partial Regex JsonTokenRegex();

    public static FlowDocument BuildDocument(string? text)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(8, 6, 8, 6),
            FontFamily = new FontFamily("Consolas, 'Cascadia Code', 'Courier New', monospace"),
            FontSize = 12.0,
            LineHeight = 18.0
        };

        if (string.IsNullOrWhiteSpace(text))
            return doc;

        var trimmed = text.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            return BuildJsonDocument(doc, text);
        }

        return BuildXmlDocument(doc, text);
    }

    private static FlowDocument BuildXmlDocument(FlowDocument doc, string xml)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0) };
        var tokens = XmlTokenRegex().Matches(xml);

        foreach (Match token in tokens)
        {
            var text = token.Value;
            if (string.IsNullOrEmpty(text))
                continue;

            if (text.StartsWith("<!--") && text.EndsWith("-->"))
            {
                paragraph.Inlines.Add(new Run(text)
                {
                    Foreground = CommentBrush,
                    FontStyle = FontStyles.Italic
                });
            }
            else if (text.StartsWith("<?") && text.EndsWith("?>"))
            {
                paragraph.Inlines.Add(new Run(text)
                {
                    Foreground = CommentBrush,
                    FontStyle = FontStyles.Italic
                });
            }
            else if (text.StartsWith('<') && text.EndsWith('>'))
            {
                AppendTagInlines(paragraph, text);
            }
            else
            {
                AppendTextInlines(paragraph, text);
            }
        }

        doc.Blocks.Add(paragraph);
        return doc;
    }

    private static FlowDocument BuildJsonDocument(FlowDocument doc, string json)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0) };
        var tokens = JsonTokenRegex().Matches(json);

        foreach (Match token in tokens)
        {
            var val = token.Value;
            if (string.IsNullOrEmpty(val))
                continue;

            if (val.EndsWith(':') && val.StartsWith('"'))
            {
                var keyName = val[..^1].Trim();
                paragraph.Inlines.Add(new Run(keyName) { Foreground = AttrNameBrush, FontWeight = FontWeights.SemiBold });
                paragraph.Inlines.Add(new Run(":") { Foreground = BracketBrush });
            }
            else if (val.StartsWith('"') && val.EndsWith('"') && val.Length >= 2)
            {
                paragraph.Inlines.Add(new Run(val) { Foreground = AttrValueBrush });
            }
            else if (val is "true" or "false" or "null")
            {
                var brush = val == "true" ? SuccessBrush : ErrorBrush;
                paragraph.Inlines.Add(new Run(val) { Foreground = brush, FontWeight = FontWeights.Bold });
            }
            else if (double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                paragraph.Inlines.Add(new Run(val) { Foreground = NumberBrush });
            }
            else if (val is "{" or "}" or "[" or "]" or "," or ":")
            {
                paragraph.Inlines.Add(new Run(val) { Foreground = BracketBrush, FontWeight = FontWeights.Bold });
            }
            else
            {
                paragraph.Inlines.Add(new Run(val) { Foreground = DefaultTextBrush });
            }
        }

        doc.Blocks.Add(paragraph);
        return doc;
    }

    private static void AppendTagInlines(Paragraph paragraph, string tag)
    {
        var isClosing = tag.StartsWith("</");
        var isSelfClosing = tag.EndsWith("/>");

        var content = tag;
        if (isClosing)
        {
            paragraph.Inlines.Add(new Run("</") { Foreground = BracketBrush, FontWeight = FontWeights.Bold });
            var tagName = content[2..^1].Trim();
            paragraph.Inlines.Add(new Run(tagName) { Foreground = TagNameBrush, FontWeight = FontWeights.Bold });
            paragraph.Inlines.Add(new Run(">") { Foreground = BracketBrush, FontWeight = FontWeights.Bold });
            return;
        }

        paragraph.Inlines.Add(new Run("<") { Foreground = BracketBrush, FontWeight = FontWeights.Bold });
        var inner = isSelfClosing ? tag[1..^2] : tag[1..^1];

        var firstSpace = inner.IndexOf(' ');
        if (firstSpace < 0)
        {
            paragraph.Inlines.Add(new Run(inner) { Foreground = TagNameBrush, FontWeight = FontWeights.Bold });
        }
        else
        {
            var tagName = inner[..firstSpace];
            paragraph.Inlines.Add(new Run(tagName) { Foreground = TagNameBrush, FontWeight = FontWeights.Bold });

            var attrsString = inner[firstSpace..];
            var attrMatches = AttributeRegex().Matches(attrsString);
            var lastIdx = 0;

            foreach (Match match in attrMatches)
            {
                if (match.Index > lastIdx)
                {
                    paragraph.Inlines.Add(new Run(attrsString[lastIdx..match.Index]) { Foreground = DefaultTextBrush });
                }

                var attrName = match.Groups[1].Value;
                var attrVal = match.Groups[2].Value;

                paragraph.Inlines.Add(new Run(attrName) { Foreground = AttrNameBrush });
                paragraph.Inlines.Add(new Run("=\"") { Foreground = BracketBrush });
                paragraph.Inlines.Add(new Run(attrVal) { Foreground = AttrValueBrush });
                paragraph.Inlines.Add(new Run("\"") { Foreground = BracketBrush });

                lastIdx = match.Index + match.Length;
            }

            if (lastIdx < attrsString.Length)
            {
                paragraph.Inlines.Add(new Run(attrsString[lastIdx..]) { Foreground = DefaultTextBrush });
            }
        }

        if (isSelfClosing)
        {
            paragraph.Inlines.Add(new Run("/>") { Foreground = BracketBrush, FontWeight = FontWeights.Bold });
        }
        else
        {
            paragraph.Inlines.Add(new Run(">") { Foreground = BracketBrush, FontWeight = FontWeights.Bold });
        }
    }

    private static void AppendTextInlines(Paragraph paragraph, string text)
    {
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            paragraph.Inlines.Add(new Run(text) { Foreground = DefaultTextBrush });
            return;
        }

        var leadingSpaces = text[..text.IndexOf(trimmed, StringComparison.Ordinal)];
        var trailingSpaces = text[(text.IndexOf(trimmed, StringComparison.Ordinal) + trimmed.Length)..];

        if (!string.IsNullOrEmpty(leadingSpaces))
            paragraph.Inlines.Add(new Run(leadingSpaces));

        if (SuccessValues.Contains(trimmed))
        {
            paragraph.Inlines.Add(new Run(trimmed)
            {
                Foreground = SuccessBrush,
                FontWeight = FontWeights.Bold
            });
        }
        else if (ErrorValues.Contains(trimmed))
        {
            paragraph.Inlines.Add(new Run(trimmed)
            {
                Foreground = ErrorBrush,
                FontWeight = FontWeights.Bold
            });
        }
        else
        {
            paragraph.Inlines.Add(new Run(trimmed)
            {
                Foreground = DefaultTextBrush,
                FontWeight = FontWeights.Medium
            });
        }

        if (!string.IsNullOrEmpty(trailingSpaces))
            paragraph.Inlines.Add(new Run(trailingSpaces));
    }
}

/// <summary>
/// Description: Attached Behavior hỗ trợ ràng buộc dữ liệu chuỗi XML và tự động tô màu cú pháp trên RichTextBox
/// Created date: 13/09/2026
/// </summary>
public static class XmlHighlightingBehavior
{
    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(XmlHighlightingBehavior), new PropertyMetadata(false));

    public static readonly DependencyProperty XmlTextProperty =
        DependencyProperty.RegisterAttached(
            "XmlText",
            typeof(string),
            typeof(XmlHighlightingBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnXmlTextChanged));

    public static string? GetXmlText(DependencyObject obj) => (string?)obj.GetValue(XmlTextProperty);
    public static void SetXmlText(DependencyObject obj, string? value) => obj.SetValue(XmlTextProperty, value);

    private static void OnXmlTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox richTextBox)
            return;

        if ((bool)richTextBox.GetValue(IsUpdatingProperty))
            return;

        var newText = e.NewValue as string;
        var currentText = GetTextFromDocument(richTextBox.Document);

        if (string.Equals(newText?.Trim(), currentText?.Trim(), StringComparison.Ordinal))
            return;

        richTextBox.SetValue(IsUpdatingProperty, true);
        try
        {
            richTextBox.Document = XmlSyntaxHighlighter.BuildDocument(newText);
        }
        finally
        {
            richTextBox.SetValue(IsUpdatingProperty, false);
        }
    }

    public static readonly DependencyProperty EnableTwoWayProperty =
        DependencyProperty.RegisterAttached(
            "EnableTwoWay",
            typeof(bool),
            typeof(XmlHighlightingBehavior),
            new PropertyMetadata(false, OnEnableTwoWayChanged));

    public static bool GetEnableTwoWay(DependencyObject obj) => (bool)obj.GetValue(EnableTwoWayProperty);
    public static void SetEnableTwoWay(DependencyObject obj, bool value) => obj.SetValue(EnableTwoWayProperty, value);

    private static void OnEnableTwoWayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox rtb)
            return;

        rtb.TextChanged -= OnRichTextBoxTextChanged;
        if ((bool)e.NewValue)
        {
            rtb.TextChanged += OnRichTextBoxTextChanged;
        }
    }

    private static void OnRichTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not RichTextBox rtb)
            return;

        if ((bool)rtb.GetValue(IsUpdatingProperty))
            return;

        var text = GetTextFromDocument(rtb.Document);
        rtb.SetValue(IsUpdatingProperty, true);
        try
        {
            SetXmlText(rtb, text);
        }
        finally
        {
            rtb.SetValue(IsUpdatingProperty, false);
        }
    }

    public static string GetTextFromDocument(FlowDocument? doc)
    {
        if (doc == null)
            return string.Empty;
        var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
        return textRange.Text.TrimEnd('\r', '\n');
    }
}
