using Microsoft.Extensions.Hosting;

// TODO: Configure DI, register IRetrievalBackend implementations, run EvalRunner.
var host = Host.CreateDefaultBuilder(args).Build();
await host.RunAsync();
