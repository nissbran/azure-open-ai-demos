using Microsoft.Extensions.AI;

namespace MaintenanceAgent.Tools;

public class WebIQMcpTool : HostedMcpServerTool
{
    public WebIQMcpTool(IConfiguration configuration) : base(
        "WebIQ",
        new Uri("https://api.microsoft.ai/v3/mcp"))
    {
        ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire;
        Headers = new Dictionary<string, string>
        {
            { "x-apikey", configuration["WebIQ:ApiKey"] ?? throw new InvalidOperationException("WebIQ ApiKey configuration is missing.") }
        };
    }
}