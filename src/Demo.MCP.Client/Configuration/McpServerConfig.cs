using System.ComponentModel.DataAnnotations;

namespace McpClientDemo.Configuration;

public class McpServerConfig
{
    public const string SectionName = "McpServers";

    [Required(ErrorMessage = "Server ID is required")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Server Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transport Type is required")]
    public TransportType TransportType { get; set; } = TransportType.Stdio;

    public string Command { get; set; } = string.Empty;

    public string[] Arguments { get; set; } = Array.Empty<string>();

    public string WorkingDirectory { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxRetries { get; set; } = 3;

    public bool Enabled { get; set; } = true;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException("MCP Server ID must be configured");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("MCP Server Name must be configured");

        switch (TransportType)
        {
            case TransportType.Stdio:
                if (string.IsNullOrWhiteSpace(Command))
                    throw new InvalidOperationException("Command is required for Stdio transport");
                break;

            case TransportType.Sse:
            case TransportType.Http:
                if (string.IsNullOrWhiteSpace(Location))
                    throw new InvalidOperationException("Location is required for SSE/HTTP transport");
                if (!Uri.TryCreate(Location, UriKind.Absolute, out _))
                    throw new InvalidOperationException("Location must be a valid URI");
                break;
        }

        if (ConnectionTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException("ConnectionTimeout must be positive");

        if (MaxRetries < 0)
            throw new InvalidOperationException("MaxRetries must be non-negative");
    }
}

public enum TransportType
{
    Stdio,
    Sse,
    Http
}