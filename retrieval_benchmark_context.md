# Retrieval Strategy Benchmarking Framework — Context & Decisions

## Purpose of this document

This document captures all design decisions, rationale, and architectural choices made during the planning of a hackathon project that compares retrieval strategies for making research documents accessible to LLMs. It is meant to be used as context in follow-up conversations so work can continue without losing prior reasoning.

---

## 1. Project goal

The business has various use-cases that involve making document collections accessible to LLMs — research repositories, knowledge bases, internal documentation, etc. Each use-case may have different corpus characteristics, query patterns, and performance requirements. There is no one-size-fits-all retrieval strategy, and the literature confirms that the optimal approach depends on query type, corpus size, vocabulary mismatch, and other factors.

The goal of this project is to build a **reusable benchmarking tool** that, given a corpus and a set of representative queries, can evaluate and compare different retrieval strategies head-to-head. For any new use-case where we're not sure which strategy to follow, we can run this tool against the specific corpus and query patterns to make a data-driven decision rather than guessing.

The first application of this tool is a set of research documents on SharePoint that need to be queryable for various operations (topic discovery, fact extraction, synthesis, etc.). But the tool itself is use-case agnostic — it can be repointed at any document collection with a new eval dataset.

---

## 2. Key decision: Evaluation-first approach

We decided to start from the evaluation side rather than the retrieval side. The reasoning:

- Setting up retrieval strategies is relatively straightforward (Azure AI Search gives you most options out of the box). The hard part is objectively measuring which one "works better."
- A proper eval harness requires a ground truth dataset of questions with expected relevant documents. Without it, comparisons are subjective.
- The eval dataset and framework have lasting value — retrieval strategies can be swapped in later, but the evaluation infrastructure captures institutional knowledge about what "good retrieval" looks like for our specific corpus.

---

## 3. Query use case classes

We defined five classes of queries that represent realistic user interactions. Different retrieval strategies are expected to excel at different classes. Evaluation metrics are computed per class, never aggregated into a single score.

| Class | Example | Key challenge |
|---|---|---|
| **Topic Discovery** | "What research have we done on customer churn?" | High recall needed; missed docs are worse than irrelevant ones |
| **Fact Extraction** | "What sample size did the 2024 churn study use?" | Needs the right chunk, not just the right document |
| **Multi-Doc Synthesis** | "How do our churn findings compare across industries?" | Requires multiple relevant docs surfaced and reasoned across |
| **Metadata / Structured** | "What did the data science team publish last quarter?" | Filtering problem, not a semantic one |
| **Absence / Negative** | "Have we done any research on X?" (answer: no) | System must recognize that nothing relevant exists |

Classes 4 and 5 are the ones most teams skip but where real users get the most frustrated. A system that can't filter by team or can't say "we haven't researched that" is annoying in practice.

---

## 4. Retrieval strategies — evolution of choices

### Initial list (6 approaches)
We started with six: BM25 keyword, vector search, Azure hybrid + semantic ranker, summary catalog + full doc injection, agentic retrieval, GraphRAG.

### First cut (5 approaches)
Dropped pure BM25 and pure vector as standalone strategies. The research is clear that they lose to hybrid in almost every scenario, and they're already components inside hybrid search. No point testing them separately just to confirm what's known.

### Reframing after GraphRAG analysis
Reading the GraphRAG documentation revealed overlap between our proposed approaches and GraphRAG's built-in query modes:

- GraphRAG's **Basic Search** overlaps with vector search (but is weaker than Azure hybrid + semantic ranker)
- GraphRAG's **Global Search** overlaps conceptually with the Summary Catalog approach (both use precomputed summaries)
- GraphRAG has **no** metadata filtering capability
- GraphRAG has **no** query decomposition / agentic behavior

### Final architecture (3 core + 1 future layer)

1. **Azure AI Search** — hybrid + semantic ranker, with and without metadata filters. One backend, two modes. The metadata filtering is a capability toggle, not a separate system.

2. **GraphRAG** — with appropriate query mode per situation (Global for discovery/synthesis, Local for fact extraction, Drift for entity exploration, Basic as baseline). One indexing pipeline, multiple query modes exposed as separate backend implementations.

3. **Agentic Retrieval** (future) — query decomposition and iterative search. A decorator that wraps any other `IRetrievalBackend`. Not implemented during the hackathon but the architecture is designed to accommodate it.

**What was dropped**: The Summary Catalog approach was deprioritized because GraphRAG's Global Search covers similar ground (precomputed summaries, searched at query time). However, it remains a cheap experiment worth revisiting — the "do we even need chunking" baseline where you just stuff full documents into the context window.

### Why this is the right set

- Two core indexing pipelines (Azure AI Search, GraphRAG) represent fundamentally different philosophies: traditional search index vs knowledge graph
- Metadata filtering tests structured queries that neither pure semantic search nor GraphRAG can handle
- The agentic layer tests intelligent query planning on top of any retrieval engine
- Each approach has genuine uncertainty about its performance on our corpus — we're not testing anything we already know the answer to

---

## 5. Research findings that shaped decisions

Key findings from the literature survey:

- **Hybrid recall improvement**: +15-30% over single methods, but a poorly tuned hybrid can score lower than dense-only. This justifies the eval framework.
- **Reranking**: Consistently the single biggest quality gain. Best accuracy-to-latency tradeoff. More complex cascade pipelines often introduce noise without benefit.
- **Long context vs RAG**: LC generally outperforms chunk-based RAG for QA, but RAG wins on citation accuracy and cost. Summarization-based retrieval performs comparably to LC. For bounded corpora, full-doc injection is a legitimate contender.
- **Data representation > algorithm complexity**: How you structure and represent your data (metadata, summaries, tags) can matter more than the retrieval algorithm itself.
- **Hybrid benefit varies by domain**: Improvement is greatest when vocabulary mismatch between queries and documents is high (likely our case with research documents).
- **GraphRAG strengths**: Excels at connecting disparate information and holistic summarization over large collections — exactly where baseline RAG struggles.
- **GraphRAG is Python-only**: No .NET library exists. There's an open GitHub issue requesting .NET support. This means GraphRAG requires a Python sidecar service with an HTTP API.

---

## 6. Architecture

### Solution structure

```
RetrievalBenchmark/
├── src/
│   ├── RetrievalBenchmark.Core/          # Contracts and models — zero external dependencies
│   ├── RetrievalBenchmark.Evaluation/    # Eval runner + metrics — depends on Core only
│   ├── RetrievalBenchmark.Backends/      # All backend implementations — depends on Core + SDKs
│   └── RetrievalBenchmark.Cli/           # Console app entry point — depends on all
├── data/
│   ├── corpus/
│   │   └── manifest.json                 # Canonical document set with stable IDs
│   └── eval/
│       └── dataset.json                  # Queries with ground truth per class
└── reports/                              # JSON output, one file per run
```

### Core types (RetrievalBenchmark.Core)

```csharp
public interface IRetrievalBackend
{
    string Name { get; }
    Task<RetrievalResult> RetrieveAsync(RetrievalRequest request);
    Task<CorpusStatus> GetCorpusStatusAsync();
}

public record RetrievalRequest(
    string Query,
    int TopK = 10,
    Dictionary<string, string>? Filters = null
);

public record RetrievalResult(
    List<RetrievedDocument> Documents,
    TimeSpan Latency,
    decimal EstimatedCost,
    Dictionary<string, object>? Metadata = null
);

public record RetrievedDocument(
    string DocId,
    double Score,
    string? ChunkText = null
);

public record CorpusStatus(
    int DocCount,
    List<string> DocIds,
    string ManifestHash
);

public enum QueryClass
{
    TopicDiscovery,
    FactExtraction,
    MultiDocSynthesis,
    MetadataStructured,
    AbsenceNegative
}

public record EvalDataset(List<EvalQuery> Queries);

public record EvalQuery(
    string QueryId,
    string Text,
    QueryClass Class,
    List<string> RelevantDocIds,
    List<string>? IrrelevantDocIds = null
);

public record CorpusManifest(List<CorpusDocument> Documents);

public record CorpusDocument(
    string DocId,
    string Title,
    string FilePath,
    string Checksum,
    Dictionary<string, string> Metadata  // author, date, team, etc.
);
```

### Evaluation types (RetrievalBenchmark.Evaluation)

- **EvalRunner** — takes one `IRetrievalBackend` and an `EvalDataset`. Validates corpus consistency, runs all queries, computes per-query metrics, aggregates by query class, returns structured `EvalReport`.
- **MetricsCalculator** — computes Precision@k, Recall@k, MRR given returned doc IDs vs ground truth. Pure functions, no dependencies, easily unit-testable.
- **CorpusValidator** — compares a `CorpusManifest` against a backend's `CorpusStatus`. Asserts doc count and doc IDs match. Fails fast before running any queries.
- **ReportWriter** — serializes `EvalReport` to JSON. One file per run, named by backend + timestamp.

### Backend hierarchy (RetrievalBenchmark.Backends)

Key design decision: **No shared QueryMode enum.** The core contract doesn't know about backend-specific modes. Instead, each backend family uses an abstract base class that shares implementation logic, and each concrete class represents one fully-configured variant. The evaluator gets `IEnumerable<IRetrievalBackend>` and loops through all registered backends — it never needs to know what kind of backend it's talking to.

**Azure AI Search family:**

```csharp
public abstract class AzureSearchBackendBase : IRetrievalBackend
{
    public abstract string Name { get; }
    protected abstract AzureSearchMode SearchMode { get; }
    protected abstract bool ApplyMetadataFilters { get; }

    public async Task<RetrievalResult> RetrieveAsync(RetrievalRequest request)
    {
        // Shared Azure SDK logic, uses SearchMode and ApplyMetadataFilters
    }

    public async Task<CorpusStatus> GetCorpusStatusAsync()
    {
        // Shared corpus status check against Azure index
    }
}

public enum AzureSearchMode { Keyword, Vector, Hybrid, HybridSemantic }

public class AzureHybridSemanticBackend : AzureSearchBackendBase
{
    public override string Name => "Azure-Hybrid-Semantic";
    protected override AzureSearchMode SearchMode => AzureSearchMode.HybridSemantic;
    protected override bool ApplyMetadataFilters => false;
}

public class AzureHybridFilteredBackend : AzureSearchBackendBase
{
    public override string Name => "Azure-Hybrid-Filtered";
    protected override AzureSearchMode SearchMode => AzureSearchMode.HybridSemantic;
    protected override bool ApplyMetadataFilters => true;
}
```

**GraphRAG family:**

```csharp
public abstract class GraphRAGBackendBase : IRetrievalBackend
{
    public abstract string Name { get; }
    protected abstract string SearchMode { get; }  // "global", "local", "drift", "basic"

    public async Task<RetrievalResult> RetrieveAsync(RetrievalRequest request)
    {
        // Shared HTTP call to Python GraphRAG API with this.SearchMode
    }

    public async Task<CorpusStatus> GetCorpusStatusAsync()
    {
        // Call Python API status endpoint
    }
}

public class GraphRAGGlobalBackend : GraphRAGBackendBase
{
    public override string Name => "GraphRAG-Global";
    protected override string SearchMode => "global";
}

public class GraphRAGLocalBackend : GraphRAGBackendBase
{
    public override string Name => "GraphRAG-Local";
    protected override string SearchMode => "local";
}

public class GraphRAGDriftBackend : GraphRAGBackendBase
{
    public override string Name => "GraphRAG-Drift";
    protected override string SearchMode => "drift";
}
```

**Future agentic layer (decorator pattern):**

```csharp
public class AgenticBackend : IRetrievalBackend
{
    private readonly IRetrievalBackend _innerBackend;

    public AgenticBackend(IRetrievalBackend innerBackend)
    {
        _innerBackend = innerBackend;
    }

    // Wraps inner backend with query decomposition + iterative search
    // Can be composed with ANY backend:
    //   new AgenticBackend(azureHybridBackend)
    //   new AgenticBackend(graphRAGGlobalBackend)
}
```

**HTTP escape hatch:**

```csharp
public class HttpRetrievalBackend : IRetrievalBackend
{
    // Generic implementation calling any external API conforming to the retrieval contract
    // Constructor takes a name and base URL
    // Useful for backends implemented in other languages
}
```

### DI registration (Cli)

```csharp
// Azure backends
services.AddSingleton<IRetrievalBackend, AzureHybridSemanticBackend>();
services.AddSingleton<IRetrievalBackend, AzureHybridFilteredBackend>();

// GraphRAG backends
services.AddSingleton<IRetrievalBackend, GraphRAGGlobalBackend>();
services.AddSingleton<IRetrievalBackend, GraphRAGLocalBackend>();
services.AddSingleton<IRetrievalBackend, GraphRAGDriftBackend>();

// Future: agentic on top of Azure
services.AddSingleton<IRetrievalBackend>(sp =>
    new AgenticBackend(sp.GetRequiredService<AzureHybridSemanticBackend>()));
```

The evaluator receives `IEnumerable<IRetrievalBackend>`, loops through all, and each backend is a self-contained, fully-configured unit.

### Canonical corpus

A manifest file defines the ground truth document set. Every backend must ingest this exact set. The eval harness calls `GetCorpusStatusAsync()` on each backend before running queries and asserts it matches the manifest (doc count, doc IDs).

```json
{
  "documents": [
    {
      "doc_id": "doc-001",
      "title": "Customer Churn Analysis 2024",
      "file_path": "documents/doc-001.pdf",
      "checksum": "sha256:abc...",
      "metadata": {
        "author": "Data Science Team",
        "date": "2024-03-15",
        "team": "DS"
      }
    }
  ]
}
```

### Evaluation dataset

40-50 queries across five classes (8-10 per class), with human-judged relevant document IDs. References documents by stable corpus IDs, never by chunk IDs or index-specific identifiers.

```json
{
  "queries": [
    {
      "query_id": "td-001",
      "text": "What research have we done on customer churn?",
      "class": "TopicDiscovery",
      "relevant_doc_ids": ["doc-001", "doc-012"],
      "irrelevant_doc_ids": []
    }
  ]
}
```

### Metrics

- **Precision@k** — of the top k results returned, how many are relevant?
- **Recall@k** — of all relevant documents, how many appear in the top k?
- **MRR (Mean Reciprocal Rank)** — how high is the first relevant result ranked?
- **Latency** — end-to-end response time per query
- **Estimated cost per query** — token usage and API calls, tracked via `EstimatedCost` on `RetrievalResult`

Metrics are reported per query class per backend. The report is a structured JSON file, one per run, designed to be consumed by a comparison dashboard later.

### Cost tracking

`EstimatedCost` is a field on `RetrievalResult` (not on `RetrievedDocument`), because cost is per retrieval operation, not per document returned. A single Azure AI Search call returns 10 documents but costs one API call. A GraphRAG query costs one LLM call regardless of result count. Agentic retrieval sums up all inner calls. Each backend is responsible for estimating its own cost.

---

## 7. GraphRAG specifics

GraphRAG is Python-only (Microsoft's library). It requires a separate Python service (FastAPI/Flask) exposing an HTTP API that the .NET `GraphRAGBackendBase` calls.

GraphRAG query modes and their intended use:
- **Global Search** — scans community summaries for holistic questions. Maps to Topic Discovery and Multi-Doc Synthesis.
- **Local Search** — starts from a specific entity, fans out to neighbors. Maps to Fact Extraction.
- **DRIFT Search** — like Local but with community context. A middle ground.
- **Basic Search** — standard vector search, their baseline RAG fallback.

Each mode is exposed as a separate concrete backend class so the evaluator tests each one independently.

---

## 8. Expected performance hypotheses

| Query class | Expected best | Strong contender | Likely weak |
|---|---|---|---|
| Topic Discovery | Azure Hybrid / GraphRAG Global | — | — |
| Fact Extraction | Azure Hybrid | GraphRAG Local | — |
| Multi-Doc Synthesis | GraphRAG Global | Agentic (future) | Azure Hybrid |
| Metadata / Structured | Azure Hybrid Filtered | — | Everything else |
| Absence Queries | GraphRAG Global | — | Azure Hybrid |

These are hypotheses to be validated by the eval framework.

---

## 9. Hackathon build order

1. Core project — all types, no logic, just models and interfaces
2. Evaluation project — MetricsCalculator first (unit-testable), then EvalRunner, CorpusValidator, ReportWriter
3. One Azure backend (AzureHybridSemanticBackend) — prove the pipeline end to end
4. Eval dataset — even 10 queries (2 per class) to validate the system works
5. CLI that ties it all together
6. Second Azure backend (AzureHybridFilteredBackend) — quick win, reuses base class
7. GraphRAG Python wrapper + GraphRAGGlobalBackend — if time allows
8. More GraphRAG modes + more eval queries — stretch

---

## 10. Open items for future sessions

- Implement the actual solution code
- Create the GraphRAG Python API wrapper (FastAPI service)
- Build the eval dataset with real queries against the actual SharePoint corpus
- Design the report comparison dashboard / viewer
- Implement the agentic retrieval decorator
- Consider whether the Summary Catalog approach (full doc injection, no chunking) is worth adding back as a cheap baseline
