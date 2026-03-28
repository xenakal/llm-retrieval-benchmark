# llm-retrieval-benchmark

Benchmarking framework for evaluating LLM retrieval strategies across Azure AI Search (hybrid + semantic ranking), GraphRAG (global, local, drift), and custom backends. Measures precision, recall, and MRR per query class to surface how different retrieval approaches perform on different types of questions.

## Why

Different retrieval strategies excel at different things. A hybrid semantic search that scores well on fact extraction may miss documents on topic discovery. Rather than guessing which approach fits a new use case, this tool lets you run a data-driven comparison against your specific corpus and query patterns.

## Query classes

Metrics are computed per class — never aggregated into a single score that hides meaningful differences.

| Class | Example |
|---|---|
| **Topic Discovery** | "What research have we done on customer churn?" |
| **Fact Extraction** | "What sample size did the 2024 churn study use?" |
| **Multi-Doc Synthesis** | "How do our churn findings compare across industries?" |
| **Metadata / Structured** | "What did the data science team publish last quarter?" |
| **Absence / Negative** | "Have we done any research on X?" (answer: no) |

## Backends

| Backend | Strategy |
|---|---|
| `AzureHybridSemantic` | Hybrid search (BM25 + vector) with semantic reranking |
| `AzureHybridFiltered` | Hybrid search with metadata filters, no semantic reranking |
| `GraphRAGGlobal` | GraphRAG global search via Python sidecar |
| `GraphRAGLocal` | GraphRAG local search via Python sidecar |
| `HttpRetrieval` | Generic HTTP wrapper for any conforming retrieval API |

## Project structure

```
src/
  RetrievalBenchmark.Core/          # Contracts and models
  RetrievalBenchmark.Evaluation/    # Metrics, eval runner, report writer
  RetrievalBenchmark.Backends/      # Backend implementations
  RetrievalBenchmark.Cli/           # Entry point
data/
  corpus/manifest.json              # Canonical document set
  eval/dataset.json                 # Queries with ground truth
reports/                            # JSON output, one file per run
```

## Running

```bash
dotnet run --project src/RetrievalBenchmark.Cli -- \
  --manifest data/corpus/manifest.json \
  --dataset data/eval/dataset.json \
  --output reports/
```

## Output

Each run produces a self-contained JSON report per backend in `reports/`. Reports include per-class precision@k, recall@k, MRR, average latency, and per-query detail so results can be audited or visualized later.
