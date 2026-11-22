// PoC: extract tables and chart data from PDF reports.
// Azure AI Document Intelligence pulls text + tables; GPT-4o (vision) reads chart images into JSON.
// Packages: Azure.AI.DocumentIntelligence 1.x, Azure.AI.OpenAI 2.x, Azure.Identity.
using System.Text.Json;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

public record ChartSeries(string Name, Dictionary<string, decimal> Points);
public record ChartData(string Title, string XAxis, string YAxis, List<ChartSeries> Series);

public sealed class PdfReportExtractor(Uri docIntelEndpoint, Uri openAiEndpoint)
{
    private static readonly DefaultAzureCredential Cred = new();
    private readonly DocumentIntelligenceClient _docs = new(docIntelEndpoint, Cred);
    private readonly ChatClient _chat = new AzureOpenAIClient(openAiEndpoint, Cred).GetChatClient("gpt-4o");

    // 1. Tables and text: layout model
    public async Task<IReadOnlyList<DocumentTable>> ExtractTablesAsync(Uri pdfUrl)
    {
        Operation<AnalyzeResult> op = await _docs.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", pdfUrl);
        return op.Value.Tables;   // each table: RowCount, ColumnCount, Cells (RowIndex, ColumnIndex, Content)
    }

    // 2. Charts: send the rendered page/figure image to a vision model and ask for strict JSON
    public async Task<ChartData?> ExtractChartAsync(BinaryData chartPng)
    {
        List<ChatMessage> messages =
        [
            new SystemChatMessage("""
                You convert chart images from financial reports into JSON.
                Return only JSON: {"title":"","xAxis":"","yAxis":"","series":[{"name":"","points":{"<x label>": <number>}}]}
                Read values from labels when present; if a value must be estimated from the axis, still return a number.
                Never invent series that are not in the image.
                """),
            new UserChatMessage(ChatMessageContentPart.CreateImagePart(chartPng, "image/png"))
        ];
        var options = new ChatCompletionOptions { Temperature = 0, ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
        ChatCompletion result = await _chat.CompleteChatAsync(messages, options);
        return JsonSerializer.Deserialize<ChartData>(result.Content[0].Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // 3. Trust but verify: cross-check chart totals against the extracted table before using the data
    public static bool CrossCheck(ChartData chart, decimal tableTotal, decimal tolerancePct = 1m)
    {
        var chartTotal = chart.Series.SelectMany(s => s.Points.Values).Sum();
        return tableTotal != 0 && Math.Abs(chartTotal - tableTotal) / tableTotal * 100 <= tolerancePct;
    }
}
