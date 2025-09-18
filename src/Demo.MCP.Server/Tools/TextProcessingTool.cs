using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace Demo.MCP.Server.Tools;

[McpServerToolType]
public static class TextProcessingTool
{
    [McpServerTool, Description("Converts text to uppercase.")]
    public static string ToUpperCase([Description("Text to convert")] string text)
    {
        return text?.ToUpper() ?? string.Empty;
    }

    [McpServerTool, Description("Converts text to lowercase.")]
    public static string ToLowerCase([Description("Text to convert")] string text)
    {
        return text?.ToLower() ?? string.Empty;
    }

    [McpServerTool, Description("Reverses the characters in a string.")]
    public static string ReverseString([Description("Text to reverse")] string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return new string(text.Reverse().ToArray());
    }

    [McpServerTool, Description("Counts words, characters, and lines in text.")]
    public static string CountTextStats([Description("Text to analyze")] string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return JsonSerializer.Serialize(new { Words = 0, Characters = 0, Lines = 0 },
                new JsonSerializerOptions { WriteIndented = true });
        }

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        var stats = new
        {
            Words = words.Length,
            Characters = text.Length,
            CharactersNoSpaces = text.Count(c => !char.IsWhiteSpace(c)),
            Lines = lines.Length
        };

        return JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Removes extra whitespace from text.")]
    public static string TrimWhitespace([Description("Text to clean")] string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Replace multiple whitespace characters with single space
        var result = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return result.Trim();
    }

    [McpServerTool, Description("Encodes text to Base64.")]
    public static string EncodeBase64([Description("Text to encode")] string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    [McpServerTool, Description("Decodes text from Base64.")]
    public static string DecodeBase64([Description("Base64 text to decode")] string base64Text)
    {
        try
        {
            if (string.IsNullOrEmpty(base64Text))
                return string.Empty;

            var bytes = Convert.FromBase64String(base64Text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid Base64 string");
        }
    }

    [McpServerTool, Description("Extracts words that match a pattern using regex.")]
    public static string ExtractPattern(
        [Description("Text to search")] string text,
        [Description("Regex pattern to match")] string pattern)
    {
        try
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
                return "[]";

            var regex = new System.Text.RegularExpressions.Regex(pattern);
            var matches = regex.Matches(text);

            var results = matches.Cast<System.Text.RegularExpressions.Match>()
                                 .Select(m => m.Value)
                                 .ToArray();

            return JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid regex pattern: {ex.Message}");
        }
    }
}