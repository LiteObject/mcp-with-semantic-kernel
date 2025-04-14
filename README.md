# Integrating Model Context Protocol (MCP) Tools with Semantic Kernel: A Step-by-Step Guide

This repository demonstrates how to integrate Model Context Protocol (MCP) tools with Microsoft Semantic Kernel, enabling seamless interaction between AI models and external data sources or tools. By following this guide, you'll learn how to connect to an MCP server, convert MCP tools into Semantic Kernel functions, and leverage large language models (LLMs) for function calling—all within a reusable and extensible framework.

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

## Prerequisites

Before diving into the code, ensure you have the following:
- **.NET SDK** (version 8.0 or later recommended).
- A valid **OpenAI API key** (or another LLM provider compatible with Semantic Kernel).
- The **ModelContextProtocol** NuGet package.
- Basic familiarity with C# and Semantic Kernel concepts.
- (Optional) An MCP server to test with, such as the example ["Everything" MCP server](https://mcp.so/server/server-everything) for demo purposes.

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

3. Configure your OpenAI API key (or other LLM credentials) using environment variables or user secrets:

    ```bash
    dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
    dotnet user-secrets set "OpenAI:ChatModelId" "gpt-4o"
    ```
    
### Step 2: Connect to an MCP Server

The project includes code to connect to an MCP server using the `ModelContextProtocol` package. The MCP client retrieves available tools from the server, which can then be used by Semantic Kernel.

Example code (see `mcp-with-semantic-kernel/src/Program.cs`):

```csharp
using ModelContextProtocol;

var mcpConfig = new McpServerConfig
{
    Id = "everything",
    Name = "Everything",
    TransportType = TransportTypes.Sse,
    Location = "http://localhost:8931"
};

var mcpClient = await McpClientFactory.CreateAsync(mcpConfig);
var tools = await mcpClient.ListToolsAsync();
```

This snippet establishes a connection to an MCP server (e.g., the "Everything" demo server) and fetches its available tools.