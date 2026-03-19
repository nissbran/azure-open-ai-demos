using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Demo11;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ProverbsAgentFactory")]
internal sealed class SharedStateAgent : DelegatingAIAgent
{
    //public SharedStateAgent(AIAgent innerAgent, JsonSerializerOptions jsonSerializerOptions)
    //    : base(innerAgent)
    //{
    //    _jsonSerializerOptions = jsonSerializerOptions;
    //}

    public SharedStateAgent(IChatClient chatClient) : base(
        chatClient.AsAIAgent(new ChatClientAgentOptions
            {
                Name = "SharedStateAgent",
                ChatOptions = new ChatOptions
                {
                    Instructions = "Testing agent"
                },
            })
            .AsBuilder()
            .Build())
    {

    }

    //public override Task<AgentResponseUpdate> RunAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    //{
    //    return RunCoreStreamingAsync(messages, session, options, cancellationToken).ToAgentRunResponseAsync(cancellationToken);
    //}

    protected override Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        return RunCoreStreamingAsync(messages, session, options, cancellationToken).ToAgentResponseAsync(cancellationToken);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages, 
        AgentSession? session = null, 
        AgentRunOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        //if (options is not ChatClientAgentRunOptions { ChatOptions.AdditionalProperties: { } properties } chatRunOptions ||
        //    !properties.TryGetValue("ag_ui_state", out JsonElement state))
        //{
        //    await foreach (var update in InnerAgent.RunStreamingAsync(messages, thread, options, cancellationToken).ConfigureAwait(false))
        //    {
        //        yield return update;
        //    }
        //    yield break;
        //}

        if (options is not null)
            Console.WriteLine("SharedStateAgent: Detected state in options:" + JsonSerializer.Serialize(options));

        //var firstRunOptions = new ChatClientAgentRunOptions
        //{
        //    ChatOptions = chatRunOptions.ChatOptions.Clone(),
        //    AllowBackgroundResponses = chatRunOptions.AllowBackgroundResponses,
        //     ContinuationToken = chatRunOptions.ContinuationToken,
        //    ChatClientFactory = chatRunOptions.ChatClientFactory,
        //};

        // Configure JSON schema response format for structured state output
        //firstRunOptions.ChatOptions.ResponseFormat = ChatResponseFormat.ForJsonSchema<ProverbsStateSnapshot>(
        //    schemaName: "ProverbsStateSnapshot",
        //    schemaDescription: "A response containing the current list of proverbs");

        //ChatMessage stateUpdateMessage = new(
        //    ChatRole.System,
        //    [
        //        new TextContent("Here is the current state in JSON format:"),
        //        new TextContent(state.GetRawText()),
        //        new TextContent("The new state is:")
        //    ]);

        //var firstRunMessages = messages.Append(stateUpdateMessage);

        //var allUpdates = new List<AgentResponseUpdate>();
        //await foreach (var update in InnerAgent.RunStreamingAsync(messages, session, cancellationToken: cancellationToken).ConfigureAwait(false))
        //{
        //    allUpdates.Add(update);

        //    // Yield all non-text updates (tool calls, etc.)
        //    bool hasNonTextContent = update.Contents.Any(c => c is not TextContent);
        //    if (hasNonTextContent)
        //    {
        //        yield return update;
        //    }
        //}

        //var response = allUpdates.ToAgentResponse();

        //if (response.TryDeserialize(_jsonSerializerOptions, out JsonElement stateSnapshot))
        //{
        //    byte[] stateBytes = JsonSerializer.SerializeToUtf8Bytes(
        //        stateSnapshot,
        //        _jsonSerializerOptions.GetTypeInfo(typeof(JsonElement)));
        //    yield return new AgentRunResponseUpdate
        //    {
        //        Contents = [new DataContent(stateBytes, "application/json")]
        //    };
        //}
        //else
        //{
        //    yield break;
        //}



        //var secondRunMessages = messages.Concat(response.Messages).Append(
        //    new ChatMessage(
        //        ChatRole.System,
        //        [new TextContent("Please provide a concise summary of the state changes in at most two sentences.")]));

        await foreach (var update in InnerAgent.RunStreamingAsync(messages, session, options, cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }
}