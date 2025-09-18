using System.ComponentModel.DataAnnotations;

namespace McpClientDemo.Configuration;

public class OpenAIConfig
{
    public const string SectionName = "OpenAI";

    [Required(ErrorMessage = "OpenAI API Key is required")]
    public string ApiKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chat Model ID is required")]
    public string ChatModelId { get; set; } = "gpt-4o";

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public int MaxRetries { get; set; } = 3;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    public void Validate()
    {
        // Only validate if API key is provided (making OpenAI optional)
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            if (string.IsNullOrWhiteSpace(ChatModelId))
                throw new InvalidOperationException("OpenAI Chat Model ID must be configured when API key is provided");

            if (MaxRetries < 0)
                throw new InvalidOperationException("MaxRetries must be non-negative");

            if (Timeout <= TimeSpan.Zero)
                throw new InvalidOperationException("Timeout must be positive");
        }
    }
}