open System

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open BitcoinHistoricalPriceMcp.Mcp

[<EntryPoint>]
let main argv =
    let builder = Host.CreateApplicationBuilder(argv)

    // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
    builder.Logging.AddConsole(fun o -> o.LogToStandardErrorThreshold <- LogLevel.Trace) |> ignore

    // Add the MCP services: the transport to use (stdio) and the tools to register.
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<HistoricalBitcoinDataMcp>() 
        //.WithToolsFromAssembly();
        |> ignore

    // Run the host (synchronously block until shutdown).
    builder.Build().RunAsync() |> System.Threading.Tasks.Task.WaitAny |> ignore
    0


    // https://gist.github.com/Thorium/46d683453517ca0567f035e78d758449 has a different approach