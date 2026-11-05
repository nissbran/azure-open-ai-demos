using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Extensions;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo9.Orchestration;

#pragma warning disable SKEXP0110
public class ContextGroupChatOrchestration :
    AgentOrchestration<ChatHistory, string>
{
    private readonly ContextGroupChatManager _manager;
    
    /// <summary>
    /// Transforms the orchestration input into a source input suitable for processing.
    /// </summary>
    public new OrchestrationInputTransform<ChatHistory> InputTransform { get; init; } = DefaultInputTransform;

    private static ValueTask<IEnumerable<ChatMessageContent>> DefaultInputTransform(ChatHistory input, CancellationToken cancellationToken = default) => ValueTask.FromResult(input as IEnumerable<ChatMessageContent> ?? []);
    
    public ContextGroupChatOrchestration(ContextGroupChatManager manager, params Agent[] agents)
        : base(agents)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }
    
    protected override ValueTask StartAsync(IAgentRuntime runtime, TopicId topic, IEnumerable<ChatMessageContent> input, AgentType? entryAgent)
    {
        if (!entryAgent.HasValue)
        {
            throw new ArgumentException("Entry agent is not defined.", nameof(entryAgent));
        }
        return runtime.PublishMessageAsync(input.AsInputTaskMessage(), entryAgent.Value);
    }

    protected override async ValueTask<AgentType?> RegisterOrchestrationAsync(IAgentRuntime runtime, OrchestrationContext context, RegistrationContext registrar, ILogger logger)
    {
        AgentType outputType = await registrar.RegisterResultTypeAsync<ContextGroupChatMessages.Result>(response => [response.Message]).ConfigureAwait(false);

        int agentCount = 0;
        GroupChatTeam team = [];
        foreach (Agent agent in this.Members)
        {
            ++agentCount;
            AgentType agentType = await RegisterAgentAsync(agent, agentCount).ConfigureAwait(false);
            string name = agent.Name ?? agent.Id ?? agentType;
            string? description = agent.Description;

            team[name] = (agentType, description);

            //logger.LogRegisterActor(this.OrchestrationLabel, agentType, "MEMBER", agentCount);

            await runtime.SubscribeAsync(agentType, context.Topic).ConfigureAwait(false);
        }

        AgentType managerType =
            await runtime.RegisterOrchestrationAgentAsync(
                FormatAgentType(context.Topic, "Manager"),
                (agentId, runtime) =>
                {
                    ContextGroupChatManagerActor actor = new(agentId, runtime, context, _manager, team, outputType, context.LoggerFactory.CreateLogger<ContextGroupChatManagerActor>());

                    return ValueTask.FromResult<IHostableAgent>(actor);

                }).ConfigureAwait(false);
        //logger.LogRegisterActor(this.OrchestrationLabel, managerType, "MANAGER");

        await runtime.SubscribeAsync(managerType, context.Topic).ConfigureAwait(false);

        return managerType;

        ValueTask<AgentType> RegisterAgentAsync(Agent agent, int agentCount) =>
            runtime.RegisterOrchestrationAgentAsync(
                FormatAgentType(context.Topic, $"Agent_{agentCount}"),
                (agentId, runtime) =>
                {
                    ContextGroupChatAgentActor actor = new(agentId, runtime, context, agent, context.LoggerFactory.CreateLogger<ContextGroupChatAgentActor>());

                    return ValueTask.FromResult<IHostableAgent>(actor);
                });
    }
}
#pragma warning restore SKEXP0110