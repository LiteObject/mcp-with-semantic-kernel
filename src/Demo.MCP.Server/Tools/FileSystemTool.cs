using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Demo.MCP.Server.Tools;

[McpServerToolType]
public static class FileSystemTool
{
    [McpServerTool, Description("Checks if a file exists at the specified path.")]
    public static bool FileExists([Description("File path to check")] string filePath)
    {
        return File.Exists(filePath);
    }

    [McpServerTool, Description("Gets the current working directory.")]
    public static string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    [McpServerTool, Description("Lists files in a directory with optional pattern filter.")]
    public static string[] ListFiles(
        [Description("Directory path")] string directoryPath,
        [Description("Search pattern (e.g., *.txt)")] string pattern = "*")
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        return Directory.GetFiles(directoryPath, pattern)
                       .Select(Path.GetFileName)
                       .ToArray()!;
    }
}