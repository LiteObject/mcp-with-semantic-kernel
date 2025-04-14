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