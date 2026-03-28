# Claude Code instructions

## Architecture reference

Full design decisions and rationale are in [`retrieval_benchmark_context.md`](retrieval_benchmark_context.md). Read it before making architectural decisions.

## Project conventions

### Test projects
- Named `{Project}.Tests` (e.g. `RetrievalBenchmark.Evaluation.Tests`)
- Live in `src/` alongside the project they test, not in a separate `tests/` directory
- Use xUnit

### Stubbing methods
- Non-trivial method bodies get `throw new NotImplementedException()`
- Trivial implementations (field assignment, simple delegation) can be implemented immediately
- "Trivial" means: no design decisions, no edge cases, nothing debatable

### C# style
- File-scoped namespaces (`namespace Foo.Bar;`)
- Primary constructors where applicable
- `HashSet<string>` for membership lookups, `List<string>` when order matters

### JSON
- Data files (`manifest.json`, `dataset.json`) use `snake_case` property names
- Deserialize with `JsonNamingPolicy.SnakeCaseLower` in Evaluation/Cli
- Never add `[JsonPropertyName]` attributes to Core types

### Core project
- Zero NuGet dependencies — no package references, BCL only
- Contains contracts and models only, no logic

## Build order

1. ~~Solution scaffold~~ ✓
2. ~~Core project~~ ✓
3. ~~Evaluation project~~ ✓
4. Azure backend (`AzureHybridSemanticBackend`) — prove the pipeline end to end
5. CLI — wire up DI, load dataset, run evaluator, write reports
6. Second Azure backend (`AzureHybridFilteredBackend`)
7. GraphRAG Python wrapper + `GraphRAGGlobalBackend`
8. More GraphRAG modes (stretch)
