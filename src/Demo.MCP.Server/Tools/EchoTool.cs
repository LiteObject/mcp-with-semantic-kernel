using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Demo.MCP.Server.Tools;

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Returns a greeting message with the provided name.")]
    public static string Echo([Description("The name to include after hello.")] string name)
    {
        return "hello " + name;
    }
}