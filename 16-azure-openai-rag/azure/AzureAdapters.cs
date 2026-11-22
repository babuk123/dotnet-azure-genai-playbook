// Real implementations of the same interfaces with Azure OpenAI and Azure AI Search.
// Packages: Azure.AI.OpenAI (2.x), Azure.Search.Documents (11.6+), Azure.Identity.
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Rag.Azure;

public sealed class AzureRag(Uri openAiEndpoint, Uri searchEndpoint, string indexName)
{
    private static readonly DefaultAzureCredential Credential = new();   // managed identity, no keys
    private readonly AzureOpenAIClient _openAi = new(openAiEndpoint, Credential);
    private readonly SearchClient _search = new(searchEndpoint, indexName, Credential);

    public async Task<float[]> EmbedAsync(string text)
    {
        EmbeddingClient client = _openAi.GetEmbeddingClient("text-embedding-3-small");
        OpenAIEmbedding embedding = await client.GenerateEmbeddingAsync(text);
        return embedding.ToFloats().ToArray();
    }

    // Hybrid search: keyword + vector, re-ranked by the semantic ranker
    public async Task<List<SearchChunk>> RetrieveAsync(string question, int k = 5)
    {
        var options = new SearchOptions
        {
            Size = k,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions { SemanticConfigurationName = "default" },
            VectorSearch = new VectorSearchOptions
            {
                Queries = { new VectorizedQuery(await EmbedAsync(question)) { KNearestNeighborsCount = k, Fields = { "contentVector" } } }
            },
            Filter = "securityGroup eq 'wealth-ops'"   // security trimming: only documents the user may see
        };
        var results = await _search.SearchAsync<SearchChunk>(question, options);
        var hits = new List<SearchChunk>();
        await foreach (var r in results.Value.GetResultsAsync()) hits.Add(r.Document);
        return hits;
    }

    public async Task<string> AnswerAsync(string question)
    {
        var hits = await RetrieveAsync(question);
        var context = string.Join("\n\n", hits.Select(h => $"[{h.Source}]\n{h.Content}"));

        ChatClient chat = _openAi.GetChatClient("gpt-4o");
        List<ChatMessage> messages =
        [
            new SystemChatMessage("""
                You answer questions for bank operations staff using ONLY the sources below.
                Cite sources in [brackets]. If the answer is not in the sources, say you don't know.
                """),
            new UserChatMessage($"Sources:\n{context}\n\nQuestion: {question}")
        ];
        ChatCompletion completion = await chat.CompleteChatAsync(messages, new ChatCompletionOptions { Temperature = 0 });
        return completion.Content[0].Text;
    }
}

public record SearchChunk(string Id, string Source, string Content);
