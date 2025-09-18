using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Demo.MCP.Server.Tools;

[McpServerToolType]
public static class SystemInfoTool
{
    [McpServerTool, Description("Gets the current system date and time.")]
    public static string GetDateTime([Description("Optional format string (e.g., 'yyyy-MM-dd HH:mm:ss')")] string format = "yyyy-MM-dd HH:mm:ss")
    {
        try
        {
            return DateTime.Now.ToString(format);
        }
        catch (FormatException)
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    [McpServerTool, Description("Gets basic system information.")]
    public static string GetSystemInfo()
    {
        var info = new
        {
            OperatingSystem = Environment.OSVersion.ToString(),
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
            RuntimeVersion = Environment.Version.ToString(),
            CurrentDirectory = Environment.CurrentDirectory
        };

        return JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Gets environment variables.")]
    public static string GetEnvironmentVariables([Description("Optional filter pattern")] string? filter = null)
    {
        var envVars = Environment.GetEnvironmentVariables();
        var filteredVars = new Dictionary<string, object?>();

        foreach (var key in envVars.Keys.Cast<string>().OrderBy(k => k))
        {
            if (string.IsNullOrEmpty(filter) || key.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                filteredVars[key] = envVars[key];
            }
        }

        return JsonSerializer.Serialize(filteredVars, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Generates a random number within the specified range.")]
    public static int GenerateRandomNumber(
        [Description("Minimum value (inclusive)")] int min = 0,
        [Description("Maximum value (exclusive)")] int max = 100)
    {
        if (min >= max)
            throw new ArgumentException("Minimum value must be less than maximum value");

        var random = new Random();
        return random.Next(min, max);
    }

    [McpServerTool, Description("Generates a GUID.")]
    public static string GenerateGuid([Description("Format: N, D, B, P, or X")] string format = "D")
    {
        var guid = Guid.NewGuid();

        return format.ToUpper() switch
        {
            "N" => guid.ToString("N"),
            "D" => guid.ToString("D"),
            "B" => guid.ToString("B"),
            "P" => guid.ToString("P"),
            "X" => guid.ToString("X"),
            _ => guid.ToString("D")
        };
    }
}