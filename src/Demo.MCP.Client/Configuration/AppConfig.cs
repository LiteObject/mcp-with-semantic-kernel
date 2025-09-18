using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace McpClientDemo.Configuration;

public class AppConfig
{
    public OpenAIConfig OpenAI { get; set; } = new();

    public Dictionary<string, McpServerConfig> McpServers { get; set; } = new();

    public LoggingConfig Logging { get; set; } = new();

    public string Environment { get; set; } = "Development";

    public void Validate()
    {
        // Only validate OpenAI if API key is provided
        if (!string.IsNullOrWhiteSpace(OpenAI?.ApiKey))
        {
            OpenAI?.Validate();
        }

        if (!McpServers.Any())
            throw new InvalidOperationException("At least one MCP server must be configured");

        foreach (var server in McpServers.Values)
        {
            server.Validate();
        }

        Logging?.Validate();
    }

    public static AppConfig Load()
    {
        var environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        // Get the directory where the executable is located
        var baseDirectory = AppContext.BaseDirectory;
        
        // Find the project directory by searching for appsettings.json
        string? projectDirectory = null;
        var searchDirectory = new DirectoryInfo(baseDirectory);
        
        // Search up the directory tree for appsettings.json
        while (searchDirectory != null && projectDirectory == null)
        {
            var configFile = Path.Combine(searchDirectory.FullName, "appsettings.json");
            if (File.Exists(configFile))
            {
                projectDirectory = searchDirectory.FullName;
                break;
            }
            searchDirectory = searchDirectory.Parent;
        }
        
        // If not found by searching up, try the common dotnet run pattern
        if (projectDirectory == null)
        {
            // For dotnet run, the base directory is usually bin/Debug/net9.0
            // Navigate up to find the project directory
            var possibleProjectDir = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
            if (File.Exists(Path.Combine(possibleProjectDir, "appsettings.json")))
            {
                projectDirectory = possibleProjectDir;
            }
        }
        
        // Fallback to current directory
        if (projectDirectory == null)
        {
            projectDirectory = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(projectDirectory, "appsettings.json")))
            {
                throw new FileNotFoundException($"Could not find appsettings.json in any expected location. Searched in: {baseDirectory} and up the directory tree, and in {projectDirectory}");
            }
        }
        
        Console.WriteLine($"Base directory: {baseDirectory}");
        Console.WriteLine($"Project directory: {projectDirectory}");
        Console.WriteLine($"Environment: {environment}");
        
        var settingsPath = Path.Combine(projectDirectory, "appsettings.json");
        Console.WriteLine($"Looking for appsettings.json at: {settingsPath}");
        Console.WriteLine($"File exists: {File.Exists(settingsPath)}");
        
        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var config = new AppConfig();
        configuration.Bind(config);

        // Debug: Check if servers were loaded
        Console.WriteLine($"Loaded {config.McpServers.Count} MCP servers");
        foreach (var server in config.McpServers)
        {
            Console.WriteLine($"  Server: {server.Key} - {server.Value.Name} (Enabled: {server.Value.Enabled})");
        }

        // Validate configuration
        config.Validate();

        return config;
    }
}

public class LoggingConfig
{
    public string LogLevel { get; set; } = "Information";

    public bool EnableFileLogging { get; set; } = true;

    public string LogFilePath { get; set; } = "logs/app.log";

    public bool EnableConsoleLogging { get; set; } = true;

    public bool EnableStructuredLogging { get; set; } = true;

    public void Validate()
    {
        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
        if (!validLogLevels.Contains(LogLevel))
            throw new InvalidOperationException($"LogLevel must be one of: {string.Join(", ", validLogLevels)}");

        if (EnableFileLogging && string.IsNullOrWhiteSpace(LogFilePath))
            throw new InvalidOperationException("LogFilePath is required when file logging is enabled");
    }
}