using OpenAI.Chat;

namespace Demo2;

public interface IGptFunction
{
    ChatTool GetToolDefinition();
}