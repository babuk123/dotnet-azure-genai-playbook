// Real tool-calling loop with Azure OpenAI (package: Azure.AI.OpenAI 2.x, Azure.Identity).
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

namespace Agents.Azure;

public sealed class ToolCallingAgent(Uri endpoint, Func<string, string, string> invokeTool)
{
    private readonly ChatClient _chat = new AzureOpenAIClient(endpoint, new DefaultAzureCredential()).GetChatClient("gpt-4o");

    private static readonly ChatTool GetPositions = ChatTool.CreateFunctionTool(
        "get_positions", "Read positions for an account",
        BinaryData.FromString("""{"type":"object","properties":{"account":{"type":"string"}},"required":["account"]}"""));

    private static readonly ChatTool GetDrawdown = ChatTool.CreateFunctionTool(
        "get_drawdown", "Max drawdown percent for an account",
        BinaryData.FromString("""{"type":"object","properties":{"account":{"type":"string"}},"required":["account"]}"""));

    public async Task<string> RunAsync(string goal, int maxSteps = 6)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage("You draft risk reports for finance analysts. Use tools for every number. Never invent figures."),
            new UserChatMessage(goal)
        ];
        var options = new ChatCompletionOptions { Tools = { GetPositions, GetDrawdown }, Temperature = 0 };

        for (int step = 0; step < maxSteps; step++)
        {
            ChatCompletion completion = await _chat.CompleteChatAsync(messages, options);
            if (completion.FinishReason != ChatFinishReason.ToolCalls)
                return completion.Content[0].Text;          // draft goes to human review, not straight to clients

            messages.Add(new AssistantChatMessage(completion));
            foreach (ChatToolCall call in completion.ToolCalls)
                messages.Add(new ToolChatMessage(call.Id, invokeTool(call.FunctionName, call.FunctionArguments.ToString())));
        }
        throw new InvalidOperationException("Step limit reached");
    }
}
