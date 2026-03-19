using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Demo11;

/// <summary>
/// Middleware that sanitizes SSE (Server-Sent Events) responses by removing null values from JSON payloads.
/// This is needed because the AG-UI TypeScript client's Zod schema expects optional fields to be omitted,
/// not set to null. The Microsoft Agent Framework sends null for optional fields like parentMessageId.
/// https://github.com/CopilotKit/CopilotKit/issues/2788
/// https://github.com/microsoft/agent-framework/issues/2637
/// </summary>
public sealed class SseNullSanitizingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SseNullSanitizingMiddleware> _logger;

    public SseNullSanitizingMiddleware(RequestDelegate next, ILogger<SseNullSanitizingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request accepts SSE
        var acceptHeader = context.Request.Headers.Accept.ToString();
        if (!acceptHeader.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Wrap the response body with our sanitizing stream
        var originalBodyStream = context.Response.Body;
        await using var sanitizingStream = new SseNullSanitizingStream(originalBodyStream, _logger);
        context.Response.Body = sanitizingStream;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}

/// <summary>
/// A stream wrapper that intercepts SSE writes and removes null values from JSON data lines.
/// </summary>
internal sealed class SseNullSanitizingStream : Stream
{
    private readonly Stream _innerStream;
    private readonly ILogger _logger;
    private string _buffer = string.Empty;

    public SseNullSanitizingStream(Stream innerStream, ILogger logger)
    {
        _innerStream = innerStream;
        _logger = logger;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _innerStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var chunk = Encoding.UTF8.GetString(buffer, offset, count);
        _buffer += chunk;

        // Process complete lines (SSE events end with \n\n or \n)
        await ProcessBufferAsync(cancellationToken);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var chunk = Encoding.UTF8.GetString(buffer.Span);
        _buffer += chunk;

        await ProcessBufferAsync(cancellationToken);
    }

    private async Task ProcessBufferAsync(CancellationToken cancellationToken)
    {
        // Process complete lines while keeping incomplete ones in the buffer
        while (true)
        {
            var newlineIndex = _buffer.IndexOf('\n');
            if (newlineIndex == -1)
                break;

            var line = _buffer[..(newlineIndex + 1)];
            _buffer = _buffer[(newlineIndex + 1)..];

            var sanitizedLine = SanitizeLine(line);
            var bytes = Encoding.UTF8.GetBytes(sanitizedLine);
            await _innerStream.WriteAsync(bytes, cancellationToken);
        }
    }

    private string SanitizeLine(string line)
    {
        // Check if this is a data line
        if (!line.StartsWith("data: ", StringComparison.Ordinal))
            return line;

        var jsonPart = line[6..].TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(jsonPart) || jsonPart == "[DONE]")
            return line;

        try
        {
            var jsonNode = JsonNode.Parse(jsonPart);
            if (jsonNode != null)
            {
                RemoveNullValues(jsonNode);
                var sanitizedJson = jsonNode.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                // Preserve the original line ending
                var lineEnding = line.EndsWith("\r\n") ? "\r\n" : line.EndsWith("\n") ? "\n" : "";
                return $"data: {sanitizedJson}{lineEnding}";
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse SSE JSON: {Json}", jsonPart);
        }

        return line;
    }

    private static void RemoveNullValues(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var keysToRemove = new List<string>();

            foreach (var property in obj)
            {
                if (property.Value is null)
                {
                    keysToRemove.Add(property.Key);
                }
                else
                {
                    RemoveNullValues(property.Value);
                }
            }

            foreach (var key in keysToRemove)
            {
                obj.Remove(key);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    RemoveNullValues(item);
                }
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        // Flush any remaining buffer content
        if (!string.IsNullOrEmpty(_buffer))
        {
            var sanitizedLine = SanitizeLine(_buffer);
            var bytes = Encoding.UTF8.GetBytes(sanitizedLine);
            await _innerStream.WriteAsync(bytes);
        }

        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !string.IsNullOrEmpty(_buffer))
        {
            var sanitizedLine = SanitizeLine(_buffer);
            var bytes = Encoding.UTF8.GetBytes(sanitizedLine);
            _innerStream.Write(bytes);
        }

        base.Dispose(disposing);
    }
}