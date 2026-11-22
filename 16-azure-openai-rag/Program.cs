// RAG pipeline: chunk -> embed -> index -> retrieve top-k -> grounded answer with citations.
// Runs fully offline with simple local stand-ins, so the flow can be studied without an Azure subscription.
// azure/AzureAdapters.cs swaps in Azure OpenAI embeddings/chat and Azure AI Search behind the same interfaces.
using Rag;

var docs = new Dictionary<string, string>
{
    ["wire-policy.md"] = "Outgoing international wires above $10,000 require a second approver. Wires are processed until 4 PM Eastern on business days.",
    ["kyc-policy.md"] = "Customer identity must be re-verified every 24 months for high-risk accounts. Sanctions screening runs on every new party.",
    ["fees.md"] = "Premium accounts pay no wire fees. Standard accounts pay a 1% wire fee with a $5 minimum.",
};

IEmbedder embedder = new HashingEmbedder();
IVectorStore store = new InMemoryVectorStore();
IChatModel llm = new ExtractiveChatModel();

// 1) Ingest: chunk + embed + index
foreach (var (source, text) in docs)
    foreach (var (chunk, i) in Chunker.BySentence(text).Select((c, i) => (c, i)))
        store.Upsert(new Chunk($"{source}#{i}", source, chunk, embedder.Embed(chunk)));

// 2) Ask
foreach (var question in new[] { "Who must approve a large international wire?", "What fee does a standard account pay for wires?" })
{
    var hits = store.Search(embedder.Embed(question), k: 2);
    var answer = llm.Answer(question, hits);
    Console.WriteLine($"Q: {question}\nA: {answer}\n   sources: {string.Join(", ", hits.Select(h => h.Id))}\n");
}

namespace Rag
{
    public record Chunk(string Id, string Source, string Text, float[] Vector);

    public interface IEmbedder { float[] Embed(string text); }
    public interface IVectorStore { void Upsert(Chunk c); IReadOnlyList<Chunk> Search(float[] query, int k); }
    public interface IChatModel { string Answer(string question, IReadOnlyList<Chunk> context); }

    public static class Chunker
    {
        // Real systems: ~300-800 tokens per chunk with overlap; sentences keep this demo readable
        public static IEnumerable<string> BySentence(string text) =>
            text.Split(". ", StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimEnd('.') + ".");
    }

    // Stand-in for an embedding model: hashed bag of words, L2-normalised
    public class HashingEmbedder(int dims = 256) : IEmbedder
    {
        static readonly HashSet<string> Stop = ["a", "the", "for", "of", "to", "is", "are", "on", "with", "per", "what", "who", "must", "does", "pay"];
        public float[] Embed(string text)
        {
            var v = new float[dims];
            foreach (var w in text.ToLowerInvariant().Split(new[] { ' ', ',', '.', '?', '$' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Stop.Contains(w)) continue;
                var stem = w.TrimEnd('s');
                v[(int)((uint)StringComparer.Ordinal.GetHashCode(stem) % (uint)dims)] += 1f;
            }
            var norm = MathF.Sqrt(v.Sum(x => x * x));
            return norm == 0 ? v : v.Select(x => x / norm).ToArray();
        }
    }

    public class InMemoryVectorStore : IVectorStore
    {
        private readonly List<Chunk> _chunks = new();
        public void Upsert(Chunk c) { _chunks.RemoveAll(x => x.Id == c.Id); _chunks.Add(c); }
        public IReadOnlyList<Chunk> Search(float[] q, int k) =>
            _chunks.OrderByDescending(c => Cosine(q, c.Vector)).Take(k).ToList();
        static float Cosine(float[] a, float[] b) { float s = 0; for (int i = 0; i < a.Length; i++) s += a[i] * b[i]; return s; }
    }

    // Stand-in for the LLM: answers only from the best retrieved chunk (grounding) and cites it
    public class ExtractiveChatModel : IChatModel
    {
        public string Answer(string question, IReadOnlyList<Chunk> context) =>
            context.Count == 0 ? "I don't know based on the provided documents."
                               : $"{context[0].Text} [{context[0].Source}]";
    }
}
