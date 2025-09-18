using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using McpClientDemo.Configuration;
using McpClientDemo.Services;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using System.Text.Json;

namespace McpClientDemo;

class Program
{
    private static ILogger<Program>? _logger;
    private static IMcpClientService? _mcpService;

    static async Task Main(string[] args)
    {
        try
        {
            // Load and validate configuration
            var appConfig = AppConfig.Load();
            Console.WriteLine($"Configuration loaded for environment: {appConfig.Environment}");

            // Setup logging
            using var loggerFactory = LoggingService.CreateLoggerFactory(appConfig.Logging);
            _logger = loggerFactory.CreateLogger<Program>();

            // Configure global exception handling
            LoggingService.ConfigureGlobalExceptionHandling(_logger);

            _logger.LogInformation("Starting MCP Client Demo Application");
            _logger.LogInformation("Configured MCP servers: {ServerCount}", appConfig.McpServers.Count);

            // Create MCP client service
            _mcpService = new McpClientService(
                loggerFactory.CreateLogger<McpClientService>(),
                appConfig.McpServers);

            // Connect to enabled MCP servers
            var enabledServers = appConfig.McpServers.Where(kvp => kvp.Value.Enabled).ToList();
            var connectionTasks = enabledServers.Select(async server =>
            {
                try
                {
                    var connected = await _mcpService.ConnectAsync(server.Key);
                    if (connected)
                    {
                        _logger.LogInformation("Successfully connected to {ServerName}", server.Value.Name);
                        return (server.Key, true);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to connect to {ServerName}", server.Value.Name);
                        return (server.Key, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to {ServerName}", server.Value.Name);
                    return (server.Key, false);
                }
            });

            var connectionResults = await Task.WhenAll(connectionTasks);
            var connectedServers = connectionResults.Where(r => r.Item2).Select(r => r.Item1).ToList();

            if (!connectedServers.Any())
            {
                _logger.LogError("No MCP servers could be connected. Exiting.");
                return;
            }

            _logger.LogInformation("Connected to {ConnectedCount} out of {TotalCount} servers",
                connectedServers.Count, enabledServers.Count);

            // Demonstrate MCP functionality
            await DemonstrateToolListing(connectedServers);
            await DemonstrateToolExecution(connectedServers);

            // Setup Semantic Kernel integration (if OpenAI is configured)
            if (!string.IsNullOrEmpty(appConfig.OpenAI.ApiKey))
            {
                await DemonstrateSemanticKernelIntegration(appConfig, connectedServers);
            }
            else
            {
                _logger.LogWarning("OpenAI API key not configured. Skipping Semantic Kernel integration.");
            }

            // Interactive demo loop
            await RunInteractiveDemo(connectedServers);
        }
        catch (Exception ex)
        {
            if (_logger != null)
            {
                _logger.LogCritical(ex, "Application terminated unexpectedly");
            }
            else
            {
                Console.WriteLine($"Critical error: {ex.Message}");
            }
        }
        finally
        {
            if (_mcpService != null)
            {
                await _mcpService.DisconnectAllAsync();
                _mcpService.Dispose();
            }

            _logger?.LogInformation("Application shutdown complete");
        }
    }

    private static async Task DemonstrateToolListing(List<string> connectedServers)
    {
        _logger!.LogInformation("=== Demonstrating Tool Listing ===");

        foreach (var serverId in connectedServers)
        {
            try
            {
                var tools = await _mcpService!.ListToolsAsync(serverId);
                _logger!.LogInformation("Server {ServerId} has {ToolCount} tools:", serverId, tools.Count);

                foreach (var tool in tools)
                {
                    _logger!.LogInformation("  - {ToolName}: {ToolDescription}", tool.Name, tool.Description);
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Failed to list tools for server {ServerId}", serverId);
            }
        }
    }

    private static async Task DemonstrateToolExecution(List<string> connectedServers)
    {
        _logger!.LogInformation("=== Demonstrating Tool Execution ===");

        foreach (var serverId in connectedServers)
        {
            try
            {
                var tools = await _mcpService!.ListToolsAsync(serverId);
                var echoTool = tools.FirstOrDefault(t => t.Name.Contains("echo", StringComparison.OrdinalIgnoreCase));

                if (echoTool != null)
                {
                    _logger!.LogInformation("Executing echo tool on server {ServerId}", serverId);

                    var result = await _mcpService.CallToolAsync(
                        serverId,
                        echoTool.Name,
                        new Dictionary<string, object?> { ["name"] = "Enhanced MCP Client" });

                    // Handle dynamic result type
                    if (result is { } resultObj && resultObj.GetType().GetProperty("Content")?.GetValue(resultObj) is IEnumerable<object> content)
                    {
                        var textContent = content.FirstOrDefault(c => 
                        {
                            var typeProperty = c.GetType().GetProperty("Type")?.GetValue(c)?.ToString();
                            return typeProperty == "text";
                        });
                        
                        if (textContent != null)
                        {
                            var textProperty = textContent.GetType().GetProperty("Text")?.GetValue(textContent)?.ToString();
                            _logger!.LogInformation("Echo result: {Result}", textProperty);
                        }
                    }
                }
                else
                {
                    _logger!.LogInformation("No echo tool found on server {ServerId}", serverId);
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Failed to execute tool on server {ServerId}", serverId);
            }
        }
    }

    private static async Task DemonstrateSemanticKernelIntegration(AppConfig config, List<string> connectedServers)
    {
        _logger!.LogInformation("=== Demonstrating Semantic Kernel Integration ===");

        try
        {
            var builder = Kernel.CreateBuilder();
            builder.Services.AddOpenAIChatCompletion(config.OpenAI.ChatModelId, config.OpenAI.ApiKey);
            var kernel = builder.Build();

            // Convert MCP tools to Semantic Kernel functions
            foreach (var serverId in connectedServers)
            {
                var tools = await _mcpService!.ListToolsAsync(serverId);

                // Create kernel functions from MCP tools
                var kernelFunctions = tools.Select(tool =>
                    KernelFunctionFactory.CreateFromMethod(
                        async (string input) =>
                        {
                            var parameters = new Dictionary<string, object?> { ["input"] = input };
                            var result = await _mcpService.CallToolAsync(serverId, tool.Name, parameters);
                            
                            // Handle dynamic result type
                            if (result is { } resultObj && resultObj.GetType().GetProperty("Content")?.GetValue(resultObj) is IEnumerable<object> content)
                            {
                                var textContent = content.FirstOrDefault(c => 
                                {
                                    var typeProperty = c.GetType().GetProperty("Type")?.GetValue(c)?.ToString();
                                    return typeProperty == "text";
                                });
                                
                                if (textContent != null)
                                {
                                    return textContent.GetType().GetProperty("Text")?.GetValue(textContent)?.ToString() ?? "";
                                }
                            }
                            return "";
                        },
                        tool.Name,
                        tool.Description ?? $"Tool from {serverId} server"));

                kernel.Plugins.AddFromFunctions($"McpTools_{serverId}", kernelFunctions);
            }

            _logger!.LogInformation("Semantic Kernel configured with {PluginCount} MCP plugins", connectedServers.Count);
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Failed to setup Semantic Kernel integration");
        }
    }

    private static async Task RunInteractiveDemo(List<string> connectedServers)
    {
        _logger!.LogInformation("=== Interactive Demo ===");
        _logger!.LogInformation("Available commands:");
        _logger!.LogInformation("  list <serverId> - List tools for a server");
        _logger!.LogInformation("  call <serverId> <toolName> <parameters> - Call a tool");
        _logger!.LogInformation("  servers - List connected servers");
        _logger!.LogInformation("  exit - Quit the application");

        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "exit")
            {
                break;
            }

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();

            try
            {
                switch (command)
                {
                    case "servers":
                        Console.WriteLine($"Connected servers: {string.Join(", ", connectedServers)}");
                        break;

                    case "list":
                        if (parts.Length > 1)
                        {
                            await ListToolsCommand(parts[1]);
                        }
                        else
                        {
                            Console.WriteLine("Usage: list <serverId>");
                        }
                        break;

                    case "call":
                        if (parts.Length >= 3)
                        {
                            await CallToolCommand(parts[1], parts[2], parts.Skip(3).ToArray());
                        }
                        else
                        {
                            Console.WriteLine("Usage: call <serverId> <toolName> [parameters...]");
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error executing command: {Command}", command);
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static async Task ListToolsCommand(string serverId)
    {
        var tools = await _mcpService!.ListToolsAsync(serverId);
        Console.WriteLine($"Tools for server {serverId}:");
        foreach (var tool in tools)
        {
            Console.WriteLine($"  {tool.Name}: {tool.Description}");
        }
    }

    private static async Task CallToolCommand(string serverId, string toolName, string[] parameterParts)
    {
        var parameters = new Dictionary<string, object?>();

        if (parameterParts.Length == 0)
        {
            // No parameters provided
        }
        else
        {
            // Join all parameter parts back together to handle JSON objects that were split by spaces
            var paramString = string.Join(" ", parameterParts);
            
            // Try to parse as JSON first
            if (paramString.Trim().StartsWith("{") && paramString.Trim().EndsWith("}"))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(paramString);
                    foreach (var property in jsonDoc.RootElement.EnumerateObject())
                    {
                        parameters[property.Name] = property.Value.ValueKind switch
                        {
                            JsonValueKind.String => property.Value.GetString(),
                            JsonValueKind.Number => property.Value.TryGetInt32(out var intVal) ? intVal : property.Value.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => property.Value.ToString()
                        };
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, fall back to simple parameter parsing
                    Console.WriteLine("Warning: Invalid JSON format, falling back to simple parameter parsing");
                    ParseSimpleParameters(parameterParts, parameters);
                }
            }
            else
            {
                // Use simple parameter parsing for non-JSON input
                ParseSimpleParameters(parameterParts, parameters);
            }
        }

        var result = await _mcpService!.CallToolAsync(serverId, toolName, parameters);
        
        // Handle dynamic result type
        if (result is { } resultObj && resultObj.GetType().GetProperty("Content")?.GetValue(resultObj) is IEnumerable<object> content)
        {
            var textContent = content.FirstOrDefault(c => 
            {
                var typeProperty = c.GetType().GetProperty("Type")?.GetValue(c)?.ToString();
                return typeProperty == "text";
            });
            
            if (textContent != null)
            {
                var textProperty = textContent.GetType().GetProperty("Text")?.GetValue(textContent)?.ToString();
                Console.WriteLine($"Result: {textProperty}");
            }
            else
            {
                Console.WriteLine("No text content in result");
            }
        }
        else
        {
            Console.WriteLine("No text content in result");
        }
    }

    private static void ParseSimpleParameters(string[] parameterParts, Dictionary<string, object?> parameters)
    {
        // Simple parameter parsing (key=value format)
        foreach (var part in parameterParts)
        {
            if (part.Contains('='))
            {
                var kvp = part.Split('=', 2);
                parameters[kvp[0]] = kvp[1];
            }
            else
            {
                parameters["input"] = part;
            }
        }
    }
}