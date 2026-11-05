using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.Agents.Runtime.Core;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo9.Orchestration;

#pragma warning disable SKEXP0110

internal class ContextGroupChatManagerActor:
    OrchestrationActor,
    IHandle<ContextGroupChatMessages.InputTask>,
    IHandle<ContextGroupChatMessages.Group>
{
    public const string DefaultDescription = "Orchestrates a team of agents to accomplish a defined task.";

    private readonly AgentType _orchestrationType;
    private readonly ContextGroupChatManager _manager;
    private readonly ChatHistory _chat;
    private readonly GroupChatTeam _team;

    public ContextGroupChatManagerActor(AgentId id, IAgentRuntime runtime, OrchestrationContext context, ContextGroupChatManager manager, GroupChatTeam team, AgentType orchestrationType, ILogger? logger = null)
        : base(id, runtime, context, DefaultDescription, logger)
    {
        _chat = [];
        _manager = manager;
        _orchestrationType = orchestrationType;
        _team = team;
    }
    
        /// <inheritdoc/>
    public async ValueTask HandleAsync(ContextGroupChatMessages.InputTask item, MessageContext messageContext)
    {
        //this.Logger.LogChatManagerInit(this.Id);

        _chat.AddRange(item.Messages);

        await PublishMessageAsync(item.Messages.AsGroupMessage(), this.Context.Topic).ConfigureAwait(false);

        await ManageAsync(messageContext).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask HandleAsync(ContextGroupChatMessages.Group item, MessageContext messageContext)
    {
        //this.Logger.LogChatManagerInvoke(this.Id);

        _chat.AddRange(item.Messages);

        await ManageAsync(messageContext).ConfigureAwait(false);
    }

    private async ValueTask ManageAsync(MessageContext messageContext)
    {
        // if (this._manager.InteractiveCallback != null)
        // {
        //     GroupChatManagerResult<bool> inputResult = await this._manager.ShouldRequestUserInput(this._chat, messageContext.CancellationToken).ConfigureAwait(false);
        //     this.Logger.LogChatManagerInput(this.Id, inputResult.Value, inputResult.Reason);
        //     if (inputResult.Value)
        //     {
        //         ChatMessageContent input = await this._manager.InteractiveCallback.Invoke().ConfigureAwait(false);
        //         this.Logger.LogChatManagerUserInput(this.Id, input.Content);
        //         this._chat.Add(input);
        //         await this.PublishMessageAsync(input.AsGroupMessage(), this.Context.Topic).ConfigureAwait(false);
        //     }
        // }

        var terminateResult = await _manager.ShouldTerminate(_chat, _team, messageContext.CancellationToken).ConfigureAwait(false);
        //this.Logger.LogChatManagerTerminate(this.Id, terminateResult.Value, terminateResult.Reason);
        if (terminateResult.Value)
        {
            GroupChatManagerResult<string> filterResult = await _manager.FilterResults(_chat, messageContext.CancellationToken).ConfigureAwait(false);
            //this.Logger.LogChatManagerResult(this.Id, filterResult.Value, filterResult.Reason);
            await PublishMessageAsync(filterResult.Value.AsResultMessage(), _orchestrationType, messageContext.CancellationToken).ConfigureAwait(false);
            return;
        }

        GroupChatManagerResult<string> selectionResult = await _manager.SelectNextAgent(_chat, _team, messageContext.CancellationToken).ConfigureAwait(false);
        var nextAgent = _team[selectionResult.Value];
        
        var contextResult = await _manager.PrepareContextForNextAgent(_chat, nextAgent.Type, nextAgent.Description, messageContext.CancellationToken).ConfigureAwait(false);
        await PublishMessageAsync(new ContextGroupChatMessages.Group() { Messages = [new ChatMessageContent(AuthorRole.User, contextResult.Value)] }, this.Context.Topic).ConfigureAwait(false);
        //this.Logger.LogChatManagerSelect(this.Id, selectionType);
        await PublishMessageAsync(new ContextGroupChatMessages.Speak(), nextAgent.Type, messageContext.CancellationToken).ConfigureAwait(false);
    }
    
}
#pragma warning restore SKEXP0110