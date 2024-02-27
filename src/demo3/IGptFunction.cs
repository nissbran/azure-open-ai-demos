using Azure.AI.OpenAI.Assistants;

namespace Demo3;

public interface IGptFunction
{
    FunctionToolDefinition GetFunctionDefinition();
    string GetFunctionName();
}