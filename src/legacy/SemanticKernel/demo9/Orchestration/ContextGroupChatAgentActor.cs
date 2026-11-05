using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;

namespace Demo9.Orchestration;

#pragma warning disable SKEXP0110
internal class ContextGroupChatAgentActor:
    AgentActor,
    IHandle<ContextGroupChatMessages.Group>,
    IHandle<ContextGroupChatMessages.Reset>,
    IHandle<ContextGroupChatMessages.Speak>
{
    
    private readonly List<ChatMessageContent> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextGroupChatAgentActor"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="runtime">The runtime associated with the agent.</param>
    /// <param name="context">The orchestration context.</param>
    /// <param name="agent">An <see cref="Agent"/>.</param>
    /// <param name="logger">The logger to use for the actor</param>
    public ContextGroupChatAgentActor(AgentId id, IAgentRuntime runtime, OrchestrationContext context, Agent agent, ILogger<ContextGroupChatAgentActor>? logger = null)

        : base(id, runtime, context, agent, logger)
    {
        _cache = [];
    }

    /// <inheritdoc/>
    public ValueTask HandleAsync(ContextGroupChatMessages.Group item, MessageContext messageContext)
    {
        _cache.AddRange(item.Messages);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ContextGroupChatMessages.Reset item, MessageContext messageContext)
    {
        await DeleteThreadAsync(messageContext.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ContextGroupChatMessages.Speak item, MessageContext messageContext)
    {
        //this.Logger.LogChatAgentInvoke(this.Id);
        
        Logger.LogDebug("{AgentId} handling Speak with {MessageCount} messages in cache", Id, _cache.Count);
        Logger.LogDebug("Messages in cache:");
        foreach (var msg in _cache)
        {   
            Logger.LogDebug(" - {Role}: {Content}", msg.Role, msg.Content);
        }

        ChatMessageContent response = await this.InvokeAsync(this._cache, messageContext.CancellationToken).ConfigureAwait(false);

        //this.Logger.LogChatAgentResult(this.Id, response.Content);

        _cache.Clear();
        await PublishMessageAsync(response.AsGroupMessage(), this.Context.Topic).ConfigureAwait(false);
    }
}
#pragma warning restore SKEXP0110