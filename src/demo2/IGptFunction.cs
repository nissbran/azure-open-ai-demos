using Azure.AI.OpenAI;

namespace Demo2;

public interface IGptFunction
{
    FunctionDefinition GetFunctionDefinition();
}