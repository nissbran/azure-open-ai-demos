using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Demo9.Orchestration;

#pragma warning disable SKEXP0110
public abstract class ContextGroupChatManager
{
    private int _invocationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextGroupChatManager"/> class.
    /// </summary>
    protected ContextGroupChatManager() { }

    /// <summary>
    /// Gets the number of times the group chat manager has been invoked.
    /// </summary>
    public int InvocationCount => _invocationCount;

    /// <summary>
    /// Gets or sets the maximum number of invocations allowed for the group chat manager.
    /// </summary>
    public int MaximumInvocationCount { get; init; } = int.MaxValue;

    /// <summary>
    /// Filters the results of the group chat based on the provided chat history.
    /// </summary>
    /// <param name="history">The chat history to filter.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="GroupChatManagerResult{TValue}"/> containing the filtered result as a string.</returns>
    public abstract ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects the next agent to participate in the group chat based on the provided chat history and team.
    /// </summary>
    /// <param name="history">The chat history to consider.</param>
    /// <param name="team">The group of agents participating in the chat.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="GroupChatManagerResult{TValue}"/> containing the identifier of the next agent as a string.</returns>
    public abstract ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepares the context for the next agent based on the provided chat history.
    /// </summary>
    /// <param name="history">The chat history to consider.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <param name="nextAgent">The name of the next agent.</param>
    /// <param name="nextAgentDescription">The agent description of the next agent</param>
    /// <returns>A <see cref="GroupChatManagerResult{TValue}"/> Containing context for the next agent to use in the question</returns>
    public abstract ValueTask<GroupChatManagerResult<string>> PrepareContextForNextAgent(ChatHistory history, string nextAgent, string nextAgentDescription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the group chat should be terminated based on the provided chat history and invocation count.
    /// </summary>
    /// <param name="history">The chat history to consider.</param>
    /// <param name="team">The group of agents participating in the chat.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="GroupChatManagerResult{TValue}"/> indicating whether the chat should be terminated.</returns>
    public virtual ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _invocationCount);

        var resultValue = false;
        var reason = "Maximum number of invocations has not been reached.";
        if (InvocationCount > MaximumInvocationCount)
        {
            resultValue = true;
            reason = "Maximum number of invocations reached.";
        }

        GroupChatManagerResult<bool> result = new(resultValue) { Reason = reason };

        return ValueTask.FromResult(result);
    }
}
#pragma warning restore SKEXP0110