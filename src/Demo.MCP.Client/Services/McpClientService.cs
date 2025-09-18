using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

namespace McpClientDemo.Services;

public interface IMcpClientService : IDisposable
{
    Task<IList<McpClientTool>> ListToolsAsync(string serverId, CancellationToken cancellationToken = default);
    Task<object> CallToolAsync(string serverId, string toolName, Dictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    Task<bool> ConnectAsync(string serverId, CancellationToken cancellationToken = default);
    Task DisconnectAsync(string serverId);
    Task DisconnectAllAsync();
}

public class McpClientService : IMcpClientService, IDisposable
{
    private readonly ILogger<McpClientService> _logger;
    private readonly Dictionary<string, Configuration.McpServerConfig> _serverConfigs;
    private readonly Dictionary<string, IMcpClient> _clients = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public McpClientService(ILogger<McpClientService> logger, Dictionary<string, Configuration.McpServerConfig> serverConfigs)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serverConfigs = serverConfigs ?? throw new ArgumentNullException(nameof(serverConfigs));
    }

    public async Task<bool> ConnectAsync(string serverId, CancellationToken cancellationToken = default)
    {
        if (!_serverConfigs.TryGetValue(serverId, out var config))
        {
            _logger.LogError("Server configuration not found for ID: {ServerId}", serverId);
            return false;
        }

        if (!config.Enabled)
        {
            _logger.LogWarning("Server {ServerId} is disabled", serverId);
            return false;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_clients.ContainsKey(serverId))
            {
                _logger.LogDebug("Client for server {ServerId} already exists", serverId);
                return true;
            }

            _logger.LogInformation("Connecting to MCP server {ServerName} ({ServerId})", config.Name, serverId);

            var client = await CreateClientWithRetryAsync(config, cancellationToken);
            if (client != null)
            {
                _clients[serverId] = client;
                _logger.LogInformation("Successfully connected to MCP server {ServerName}", config.Name);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MCP server {ServerName} ({ServerId})", config.Name, serverId);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<IMcpClient?> CreateClientWithRetryAsync(Configuration.McpServerConfig config, CancellationToken cancellationToken)
    {
        var maxRetries = config.MaxRetries;
        var baseDelay = TimeSpan.FromSeconds(1);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Attempting to connect to {ServerName} (attempt {Attempt}/{MaxRetries})",
                    config.Name, attempt + 1, maxRetries + 1);

                var transport = CreateTransport(config);
                using var timeoutCts = new CancellationTokenSource(config.ConnectionTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var client = await McpClientFactory.CreateAsync(transport);

                // Test the connection by listing tools
                var tools = await client.ListToolsAsync();

                return client;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Connection attempt cancelled for {ServerName}", config.Name);
                throw;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Connection attempt {Attempt} failed for {ServerName}. Retrying in {Delay}ms",
                    attempt + 1, config.Name, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All connection attempts failed for {ServerName}", config.Name);
                throw;
            }
        }

        return null;
    }

    private IClientTransport CreateTransport(Configuration.McpServerConfig config)
    {
        return config.TransportType switch
        {
            Configuration.TransportType.Stdio => new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = config.Name,
                    Command = config.Command,
                    Arguments = ResolveArguments(config.Arguments),
                    WorkingDirectory = GetRepositoryRoot()
                }),
            Configuration.TransportType.Sse => throw new NotImplementedException("SSE transport not yet implemented"),
            Configuration.TransportType.Http => throw new NotImplementedException("HTTP transport not yet implemented"),
            _ => throw new ArgumentException($"Unsupported transport type: {config.TransportType}")
        };
    }

    private string[] ResolveArguments(string[] arguments)
    {
        // Resolve any relative paths in arguments to be relative to repository root
        return arguments.Select(arg =>
        {
            // If it's a project path, ensure it's relative to repository root
            if (arg.StartsWith("../") || arg.StartsWith("..\\"))
            {
                // Convert relative path to absolute, then back to relative from repo root
                var repoRoot = GetRepositoryRoot();
                var currentDir = Path.GetDirectoryName(AppContext.BaseDirectory);
                var absolutePath = Path.GetFullPath(Path.Combine(currentDir ?? "", arg));
                return Path.GetRelativePath(repoRoot, absolutePath);
            }
            return arg;
        }).ToArray();
    }

    private string GetRepositoryRoot()
    {
        // Start from the current directory and search upward for the solution file
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        
        while (currentDir != null)
        {
            // Look for .sln file or .git directory
            if (currentDir.GetFiles("*.sln").Any() || 
                currentDir.GetDirectories(".git").Any())
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        
        // Fallback to current directory
        return Directory.GetCurrentDirectory();
    }

    private static List<string> FindSimilarToolNames(string inputTool, List<string> availableTools)
    {
        var suggestions = new List<string>();
        var inputLower = inputTool.ToLowerInvariant();

        // First, look for exact case-insensitive matches
        var exactMatch = availableTools.FirstOrDefault(t => t.ToLowerInvariant() == inputLower);
        if (exactMatch != null && exactMatch != inputTool)
        {
            suggestions.Add(exactMatch);
        }

        // Then look for tools that start with the input (case-insensitive)
        var startsWith = availableTools
            .Where(t => t.ToLowerInvariant().StartsWith(inputLower) && t.ToLowerInvariant() != inputLower)
            .Take(3)
            .ToList();
        suggestions.AddRange(startsWith);

        // Finally, look for tools that contain the input (case-insensitive)
        if (suggestions.Count < 3)
        {
            var contains = availableTools
                .Where(t => t.ToLowerInvariant().Contains(inputLower) && 
                           !suggestions.Contains(t) && 
                           t.ToLowerInvariant() != inputLower)
                .Take(3 - suggestions.Count)
                .ToList();
            suggestions.AddRange(contains);
        }

        return suggestions.Distinct().Take(3).ToList();
    }

    public async Task<IList<McpClientTool>> ListToolsAsync(string serverId, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(serverId, cancellationToken);
        if (client == null)
        {
            _logger.LogError("Client not available for server {ServerId}", serverId);
            return Array.Empty<McpClientTool>();
        }

        try
        {
            _logger.LogDebug("Listing tools for server {ServerId}", serverId);
            var tools = await client.ListToolsAsync();
            _logger.LogInformation("Found {ToolCount} tools for server {ServerId}", tools.Count, serverId);
            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tools for server {ServerId}", serverId);
            throw;
        }
    }

    public async Task<object> CallToolAsync(
        string serverId,
        string toolName,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(serverId, cancellationToken);
        if (client == null)
        {
            throw new InvalidOperationException($"Client not available for server {serverId}");
        }

        try
        {
            _logger.LogInformation("Calling tool {ToolName} on server {ServerId} with parameters: {Parameters}",
                toolName, serverId, System.Text.Json.JsonSerializer.Serialize(parameters));

            var result = await client.CallToolAsync(toolName, parameters, null);

            _logger.LogInformation("Tool {ToolName} executed successfully on server {ServerId}", toolName, serverId);
            return result;
        }
        catch (ModelContextProtocol.McpException ex) when (ex.Message.Contains("Unknown tool"))
        {
            _logger.LogError(ex, "Failed to call tool {ToolName} on server {ServerId}", toolName, serverId);
            
            // Try to provide helpful suggestions for similar tool names
            try
            {
                var availableTools = await client.ListToolsAsync();
                var suggestions = FindSimilarToolNames(toolName, availableTools.Select(t => t.Name).ToList());
                
                if (suggestions.Any())
                {
                    var suggestionText = string.Join(", ", suggestions);
                    throw new InvalidOperationException($"Unknown tool '{toolName}'. Did you mean: {suggestionText}? (Tool names are case-sensitive)", ex);
                }
                else
                {
                    throw new InvalidOperationException($"Unknown tool '{toolName}'. Tool names are case-sensitive. Use 'list {serverId}' to see available tools.", ex);
                }
            }
            catch (Exception suggestionEx) when (!(suggestionEx is InvalidOperationException))
            {
                _logger.LogWarning(suggestionEx, "Could not generate tool suggestions for {ToolName}", toolName);
                throw new InvalidOperationException($"Unknown tool '{toolName}'. Tool names are case-sensitive. Use 'list {serverId}' to see available tools.", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call tool {ToolName} on server {ServerId}", toolName, serverId);
            throw;
        }
    }

    private async Task<IMcpClient?> GetClientAsync(string serverId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_clients.TryGetValue(serverId, out var client))
            {
                return client;
            }

            // Try to connect if not already connected
            var connected = await ConnectAsync(serverId, cancellationToken);
            return connected && _clients.TryGetValue(serverId, out client) ? client : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DisconnectAsync(string serverId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_clients.TryGetValue(serverId, out var client))
            {
                _logger.LogInformation("Disconnecting from server {ServerId}", serverId);
                await client.DisposeAsync();
                _clients.Remove(serverId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from server {ServerId}", serverId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DisconnectAllAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var disconnectTasks = _clients.Keys.Select(serverId => DisconnectClient(serverId)).ToArray();
            await Task.WhenAll(disconnectTasks);
            _clients.Clear();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task DisconnectClient(string serverId)
    {
        try
        {
            if (_clients.TryGetValue(serverId, out var client))
            {
                await client.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting client {ServerId}", serverId);
        }
    }

    public void Dispose()
    {
        DisconnectAllAsync().GetAwaiter().GetResult();
        _semaphore.Dispose();
    }
}