using System;
using System.ClientModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using OpenAI.Chat;
using Serilog;

#pragma warning disable SKEXP0001

namespace Demo6;

public class ChatWithAgentFrameworkService
{
    private const string SystemMessage = "You are a helpful assistant that helps find information about starships and vehicles in Star Wars.";
    private readonly Uri _mcpServerUri;

    private McpClient _mcpClient;
    private readonly ChatClient _chatClient;
    private AIAgent _agent;
    private AgentSession _agentSession;

    public ChatWithAgentFrameworkService(IConfiguration configuration)
    {
        var model = configuration["AzureOpenAI:ChatModel"] ?? throw new ArgumentNullException(nameof(configuration), "ChatModel configuration is missing.");
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey configuration is missing.");
        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException(nameof(configuration), "Endpoint configuration is missing.");

        var mcpBaseUrl = configuration["McpServer:BaseUrl"] ?? throw new ArgumentNullException(nameof(configuration), "McpServer:Uri configuration is missing.");
        
        _mcpServerUri = new Uri(mcpBaseUrl);
        _chatClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new ApiKeyCredential(apiKey))
            .GetChatClient(model);
    }

    public async Task StartNewSessionAsync()
    {
        Log.Verbose("Starting new session");

        // We can customize a shared HttpClient with a custom handler if desired
        var sharedHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };
        var httpClient = new HttpClient(sharedHandler);

        var consoleLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });
        var transport = new HttpClientTransport(new()
        {
            Endpoint = new Uri("https://apim-demo342231.azure-api.net/graph/mcp"),
            Name = "ProtectedMcpClient",
            OAuth = new ClientOAuthOptions
            {
                ClientId = "7f570956-9e4f-427b-9559-849123d4219e",
                RedirectUri = new Uri("http://localhost:8080/auth/callback"),
                AuthorizationRedirectDelegate = HandleAuthorizationUrlAsync
            }
        }, httpClient, consoleLoggerFactory);

        try
        {
            _mcpClient = await McpClient.CreateAsync(transport);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to create MCP client");
            throw;
        }

        Log.Verbose("Creating new MCP client");
        
        var tools = await _mcpClient.ListToolsAsync().ConfigureAwait(false);

        Log.Verbose("Found {Count} tools", tools.Count);

        foreach (var tool in tools)
        {
            Log.Verbose("Tool: {Name} - {Description}", tool.Name, tool.Description);
        }
        _agent = _chatClient.AsAIAgent(SystemMessage, tools: [.. tools.Cast<AITool>()]);
        _agentSession = await _agent.CreateSessionAsync();
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            var response = await _agent.RunAsync(message, _agentSession);

            return response.Messages.LastOrDefault()?.Text;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute ");
            return "I'm sorry, I can't do that right now.";
        }
    }

    /// <summary>
    /// Handles the OAuth authorization URL by starting a local HTTP server and opening a browser.
    /// This implementation demonstrates how SDK consumers can provide their own authorization flow.
    /// </summary>
    /// <param name="authorizationUrl">The authorization URL to open in the browser.</param>
    /// <param name="redirectUri">The redirect URI where the authorization code will be sent.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests. The default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>The authorization code extracted from the callback, or null if the operation failed.</returns>
    private static async Task<string?> HandleAuthorizationUrlAsync(Uri authorizationUrl, Uri redirectUri, CancellationToken cancellationToken)
    {
        Log.Information("Starting OAuth authorization flow...");
        Log.Information("Opening browser to: {AuthorizationUrl}", authorizationUrl);

        var listenerPrefix = redirectUri.GetLeftPart(UriPartial.Authority);
        if (!listenerPrefix.EndsWith("/")) listenerPrefix += "/";

        using var listener = new HttpListener();
        listener.Prefixes.Add(listenerPrefix);

        try
        {
            listener.Start();
            Log.Information("Listening for OAuth callback on: {ListenerPrefix}", listenerPrefix);

            OpenBrowser(authorizationUrl);

            var context = await listener.GetContextAsync();
            var query = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? string.Empty);
            var code = query["code"];
            var error = query["error"];

            string responseHtml = "<html><body><h1>Authentication complete</h1><p>You can close this window now.</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();

            if (!string.IsNullOrEmpty(error))
            {
                Log.Error("Auth error: {Error}", error);
                return null;
            }

            if (string.IsNullOrEmpty(code))
            {
                Log.Error("No authorization code received");
                return null;
            }

            Log.Information("Authorization code received successfully.");
            return code;
        }
        catch (Exception ex)
        {
            Log.Error("Error getting auth code: {Message}", ex.Message);
            return null;
        }
        finally
        {
            if (listener.IsListening) listener.Stop();
        }
    }

    /// <summary>
    /// Opens the specified URL in the default browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    private static void OpenBrowser(Uri url)
    {
        // Validate the URI scheme - only allow safe protocols
        if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
        {
            Console.WriteLine($"Error: Only HTTP and HTTPS URLs are allowed.");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url.ToString(),
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening browser: {ex.Message}");
            Console.WriteLine($"Please manually open this URL: {url}");
        }
    }
}