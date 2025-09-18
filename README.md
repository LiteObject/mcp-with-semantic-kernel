# Integrating Model Context Protocol (MCP) Tools with Semantic Kernel: A Step-by-Step Guide

This repository demonstrates how to integrate Model Context Protocol (MCP) tools with Microsoft Semantic Kernel, enabling seamless interaction between AI models and external data sources or tools. By following this guide, you'll learn how to connect to an MCP server, convert MCP tools into Semantic Kernel functions, and leverage large language models (LLMs) for function calling—all within a reusable and extensible framework.

## Quick Start - Running the Demo

### Prerequisites
- **.NET SDK** (version 9.0 or later)
- **Node.js and npm** (required only for external MCP servers like GitHub and Everything servers)
- A valid **OpenAI API key** (optional - only needed for Semantic Kernel integration)

> **Note**: Node.js is needed because the demo uses external MCP servers (GitHub, Everything) that are published as Node.js packages. You can run the demo with only the local .NET MCP server if you prefer to avoid Node.js dependencies.

### 1. Clone and Build
```bash
git clone https://github.com/LiteObject/mcp-with-semantic-kernel.git
cd mcp-with-semantic-kernel
dotnet restore
dotnet build
```

### 2. Configure (Optional)
Set up your OpenAI API key for Semantic Kernel integration (this is optional):
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here" --project src/Demo.MCP.Client
```

### 3. Run the MCP Client Demo
```bash
dotnet run --project src/Demo.MCP.Client
```

The application will:
1. **Automatically connect to enabled MCP servers** (configured in appsettings.json)
2. **List available tools** from each connected server (23 tools from local server)
3. **Demonstrate tool execution** with sample calls (Echo tool test)
4. **Start interactive mode** where you can test commands

### 4. Quick Test with Local Server Only
For the simplest setup that doesn't require Node.js, the demo is pre-configured to use only the local .NET MCP server. Just run:

```bash
# The client (already configured to use local server)
dotnet run --project src/Demo.MCP.Client
```

The local server includes 23 built-in tools for testing.

>No need for a separate server - it starts automatically!

### 5. Interactive Commands
Once the client is running, try these commands:
```bash
> servers                           # List connected servers  
> list local                        # List tools for local server
> call local Add {"a": 5, "b": 3}   # Test calculator tool (note: case-sensitive "Add")
> call local Echo {"name": "World"} # Test echo tool (note: case-sensitive "Echo")
> call local GetDateTime {}         # Get current time (no parameters needed)
> exit                              # Quit the application
```

**Important Notes:**
- Tool names are **case-sensitive** (use "Add", not "add")
- Parameters must be valid JSON format
- Use the exact parameter names as defined in the tool (e.g., "a" and "b" for Add tool)

### 6. Enable External Servers (Optional)
To test with GitHub and Everything servers, edit `src/Demo.MCP.Client/appsettings.json`:
```json
{
  "McpServers": {
    "github": { "Enabled": true },
    "everything": { "Enabled": true },
    "local": { "Enabled": true }
  }
}
```

Then restart the client. This requires Node.js and internet access.

## What is Model Context Protocol (MCP)?

The **Model Context Protocol (MCP)** is an open-standard protocol designed to standardize how applications provide context to AI models. It acts as a universal connector, allowing LLMs to interact with diverse data sources (e.g., APIs, databases, or services) in a consistent way. Think of MCP as a bridge that enhances AI interoperability, flexibility, and contextual understanding.

In this project, we use MCP to expose tools that Semantic Kernel can consume, enabling AI-driven workflows with real-world applications like automation, data retrieval, or system integration.

## Why Use Semantic Kernel with MCP? 

**Microsoft Semantic Kernel** is a powerful SDK that simplifies building AI agents and orchestrating complex workflows. By integrating MCP tools, you can:

- Extend Semantic Kernel with external capabilities via MCP servers.
- Enable LLMs to call functions dynamically based on user prompts.
- Promote interoperability between AI models and non-Semantic Kernel applications.
- Simplify development with a standardized protocol for tool integration.

This repository provides a practical example of how to combine these technologies, complete with sample code to get you started.

## Project Structure

```
src/
├── Demo.MCP.Client/              # MCP client application  
│   ├── Configuration/            # Robust configuration system with validation
│   │   ├── AppConfig.cs         # Main configuration class with smart loading
│   │   ├── McpServerConfig.cs   # MCP server configuration with transport types
│   │   └── OpenAIConfig.cs      # OpenAI/Semantic Kernel configuration
│   ├── Services/                # Business logic and MCP integration
│   │   ├── LoggingService.cs    # Structured logging with Serilog
│   │   └── McpClientService.cs  # MCP client with retry logic and error handling
│   ├── Program.cs               # Main application with interactive demo
│   └── appsettings.json         # Configuration for servers, logging, and OpenAI
└── Demo.MCP.Server/             # Local MCP server (.NET implementation)
    ├── Tools/                   # 23 example MCP tools organized by category
    │   ├── CalculatorTool.cs    # Math operations (Add, Subtract, Multiply, etc.)
    │   ├── EchoTool.cs          # Simple echo functionality for testing
    │   ├── FileSystemTool.cs    # File operations (FileExists, ListFiles, etc.)
    │   ├── SystemInfoTool.cs    # System information and environment tools  
    │   └── TextProcessingTool.cs # Text manipulation (Base64, regex, etc.)
    └── Program.cs               # MCP server using stdio transport
```

## Detailed Prerequisites

Before diving into the code, ensure you have the following:
- **.NET SDK** (version 9.0 or later - uses preview version).
- **Node.js and npm** (only required for external MCP servers - GitHub and Everything servers are Node.js packages)
- A valid **OpenAI API key** (optional - only for Semantic Kernel integration).
- The **ModelContextProtocol** NuGet package (version 0.1.0-preview.8).
- Basic familiarity with C# and Semantic Kernel concepts.

### Running Without Node.js
The demo is pre-configured to work without Node.js by using only the local .NET MCP server:
1. Local server starts automatically when the client runs
2. External servers are disabled by default in `appsettings.json`:
```json
{
  "McpServers": {
    "github": { "Enabled": false },
    "everything": { "Enabled": false },
    "local": { "Enabled": true }
  }
}
```

This provides 23 built-in tools for comprehensive testing without external dependencies.

## Step-by-Step Guide

This section walks you through the process of integrating MCP tools with Semantic Kernel, as implemented in this repository.

### Step 1: Set Up Your Project

1. Clone this repository:

    ```bash
    git clone https://github.com/LiteObject/mcp-with-semantic-kernel.git
    cd mcp-with-semantic-kernel
    ```

2. Restore dependencies:
    
    ```bash
    dotnet restore
    ```

3. Configure your OpenAI API key (optional - only for Semantic Kernel integration):

    ```bash
    dotnet user-secrets set "OpenAI:ApiKey" "your-api-key" --project src/Demo.MCP.Client
    dotnet user-secrets set "OpenAI:ChatModelId" "gpt-4o" --project src/Demo.MCP.Client
    ```
    
### Step 2: Connect to an MCP Server

The project includes code to connect to an MCP server using the `ModelContextProtocol` package. The MCP client retrieves available tools from the server, which can then be used by Semantic Kernel.

Example code (see `src/Demo.MCP.Client/Program.cs`):

```csharp
using ModelContextProtocol;

var mcpConfig = new McpServerConfig
{
    Id = "everything",
    Name = "Everything",
    TransportType = TransportType.Stdio,
    Command = "npx",
    Arguments = ["-y", "@modelcontextprotocol/server-everything"]
};

var mcpClient = await McpClientFactory.CreateAsync(mcpConfig);
var tools = await mcpClient.ListToolsAsync();
```

This snippet establishes a connection to an MCP server (e.g., the "Everything" demo server) and fetches its available tools.

## Configuration

The application uses a sophisticated configuration system with validation and smart path resolution:

### Automatic MCP Server Management
The client automatically starts and manages the local MCP server using relative paths:

```json
{
  "McpServers": {
    "local": {
      "Id": "local",
      "Name": "Local Demo Server", 
      "TransportType": "Stdio",
      "Command": "dotnet",
      "Arguments": ["run", "--project", "src/Demo.MCP.Server"],
      "ConnectionTimeout": "00:00:30",
      "MaxRetries": 3,
      "Enabled": true
    }
  }
}
```

### Smart Path Resolution
The application includes intelligent path resolution that:
- Automatically finds the repository root
- Resolves relative paths at runtime  
- Works regardless of where you run the application
- Is portable across different machines and users

### Available MCP Servers
- **Local Server**: .NET MCP server with 23 built-in tools (enabled by default)
- **GitHub Server**: Provides GitHub API access (disabled by default)
- **Everything Server**: Demo server with various tools (disabled by default)

### Configuration Features
- **Hierarchical loading**: appsettings.json, environment-specific files, user secrets
- **Validation**: Comprehensive validation with descriptive error messages
- **Logging configuration**: Structured logging with Serilog, file and console output
- **Retry logic**: Configurable connection timeouts and retry attempts

## Transport Options for MCP Communication

MCP supports different transport mechanisms for client-server communication. Understanding these options helps you choose the right configuration for your use case.

### Available Transport Types

#### 1. **Stdio (Standard Input/Output)**
The default and most common transport for local MCP servers.

**How it works:** The client starts the server as a subprocess and communicates through stdin/stdout pipes.

**Configuration example:**
```json
{
  "TransportType": "Stdio",
  "Command": "dotnet",
  "Arguments": ["run", "--project", "src/Demo.MCP.Server"]
}
```

**Use cases:**
- Local development and testing
- Embedded servers within applications
- Scenarios where the client controls the server lifecycle

**Advantages:**
- Simple setup, no network configuration needed
- Secure (no network exposure)
- Client automatically manages server lifecycle

**Limitations:**
- Server must be on the same machine as client
- One client per server instance

#### 2. **HTTP/HTTPS** (Future Support)
Network-based transport for remote MCP servers.

**How it works:** Client connects to a server exposed via HTTP endpoint.

**Potential configuration:**
```json
{
  "TransportType": "Http",
  "Endpoint": "https://api.example.com/mcp"
}
```

**Use cases:**
- Cloud-hosted MCP servers
- Multi-client scenarios
- Microservices architecture

**Advantages:**
- Remote server access
- Multiple clients can connect to one server
- Standard web infrastructure (load balancers, proxies, etc.)

**Limitations:**
- Requires network setup and security considerations
- Higher latency than local transports

#### 3. **WebSocket** (Potential Future)
Real-time bidirectional communication.

**Use cases:**
- Real-time updates from server to client
- Long-running connections
- Push notifications

#### 4. **Named Pipes** (Platform-specific)
Inter-process communication on the same machine.

**Use cases:**
- High-performance local communication
- Windows services integration

### Choosing the Right Transport

| Scenario | Recommended Transport | Why |
|----------|----------------------|-----|
| Local development | Stdio | Simple, no configuration needed |
| Production server | HTTP/HTTPS | Scalable, standard infrastructure |
| Real-time features | WebSocket | Bidirectional communication |
| Windows service | Named Pipes | Native Windows IPC |

### Current Implementation
This demo uses **Stdio transport** for all servers:
- **Local server**: Started as subprocess via `dotnet run`
- **GitHub server**: Started via `npx` command
- **Everything server**: Started via `npx` command

The Stdio transport was chosen for simplicity and security, as it doesn't expose any network endpoints and works consistently across platforms.

## Features

### Enhanced Architecture
- **Robust Configuration Management**: Hierarchical configuration with validation and smart path resolution
- **Advanced Error Handling**: Comprehensive retry logic with exponential backoff
- **Structured Logging**: Serilog integration with file and console output
- **Interactive CLI**: Rich command-line interface for testing and exploration

### Core MCP Features (No API Key Required)
- **Multi-server support** - Connect to multiple MCP servers simultaneously  
- **Automatic server management** - Local server starts automatically with smart path resolution
- **Tool discovery** - List available tools from each server (23 tools from local server)
- **Tool execution** - Call MCP tools with parameters and see results
- **Interactive CLI** - Command-line interface for testing and exploration
- **Error handling** - Robust error handling with retry logic and detailed logging
- **Comprehensive logging** - Structured logging for debugging and monitoring

### Advanced Features (OpenAI API Key Required)
- **Semantic Kernel integration** - Convert MCP tools to SK functions automatically
- **AI-powered tool orchestration** - Let AI decide which tools to use based on context
- **Natural language queries** - Ask questions in plain English and get AI-driven responses

### Local MCP Server Tools (23 Available)
**Calculator Tools:**
- **Add** - Adds two numbers (parameters: `a`, `b`)
- **Subtract** - Subtracts second from first (parameters: `a`, `b`) 
- **Multiply** - Multiplies two numbers (parameters: `a`, `b`)
- **Divide** - Divides first by second (parameters: `a`, `b`)
- **Power** - Calculates power (parameters: `baseNumber`, `exponent`)
- **SquareRoot** - Square root calculation (parameters: `number`)

**System Information:**  
- **GetSystemInfo** - Hardware and OS information (no parameters)
- **GetEnvironmentVariables** - Environment variable access (no parameters)
- **GetCurrentDirectory** - Working directory information (no parameters)
- **GetDateTime** - Current system date and time (no parameters)
- **GenerateGuid** - GUID generation (no parameters)

**File System Operations:**
- **FileExists** - Check if files exist (parameters: `filePath`)
- **ListFiles** - Directory listing with pattern filtering (parameters: `directoryPath`, `pattern`)

**Text Processing:**
- **ToUpperCase** - Case conversion to upper (parameters: `text`)
- **ToLowerCase** - Case conversion to lower (parameters: `text`)
- **TrimWhitespace** - Whitespace removal (parameters: `text`)
- **ReverseString** - String reversal (parameters: `text`)
- **CountTextStats** - Word, character, and line counting (parameters: `text`)
- **ExtractPattern** - Regex pattern extraction (parameters: `text`, `pattern`)
- **EncodeBase64** - Base64 encoding (parameters: `text`)
- **DecodeBase64** - Base64 decoding (parameters: `encodedText`)

**Utility Tools:**
- **Echo** - Simple echo for testing connectivity (parameters: `name`)
- **GenerateRandomNumber** - Random number generation (parameters: `min`, `max`)

## Troubleshooting

### Common Issues
1. **"Unknown tool 'add'" or similar errors**
   - Tool names are **case-sensitive** - use "Add" not "add"
   - Use exact tool names as shown in `list local` command
   - Check parameter names match the tool definition exactly

2. **"No MCP servers could be connected"**
   - Check that the local server project builds successfully: `dotnet build src/Demo.MCP.Server`
   - Verify .NET SDK version 9.0+ is installed
   - For external servers: ensure Node.js and npm are installed and `npx` is available

3. **Parameter parsing errors in interactive mode**
   - Ensure JSON parameters are properly formatted: `{"a": 5, "b": 3}`
   - Use double quotes around parameter names and string values
   - Check parameter names match those expected by the tool (use `list local` to see tool descriptions)

4. **"OpenAI API key not configured"** 
   - This is a warning, not an error - the core MCP functionality works without it
   - Only affects Semantic Kernel integration features
   - All 23 local tools work without an API key

5. **Build errors**
   - Run `dotnet restore` to ensure all packages are installed
   - Check that .NET 9.0+ SDK is installed (the project uses preview features)
   - Verify all project references are restored

6. **Path resolution issues**
   - The application automatically finds the repository root
   - Works regardless of where you run the command from
   - Check that the solution file (.sln) exists in the repository root

### Correct Command Examples
```bash
# Correct (case-sensitive tool names)
> call local Add {"a": 5, "b": 3}
> call local Echo {"name": "World"}
> call local SquareRoot {"number": 16}

# Incorrect (wrong case)  
> call local add {"a": 5, "b": 3}        # Error: Unknown tool 'add'
> call local echo {"name": "World"}      # Error: Unknown tool 'echo'
```

### Debug Mode
Run with detailed logging to see internal operations:
```bash
dotnet run --project src/Demo.MCP.Client --configuration Debug
```

### Testing Individual Components
Test the local MCP server independently:
```bash
# Test server build
dotnet build src/Demo.MCP.Server

# Test client build  
dotnet build src/Demo.MCP.Client
```

---
## Links
- [MCP Servers](https://github.com/modelcontextprotocol/servers)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)