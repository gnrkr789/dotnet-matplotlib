module Matplotlib.Mcp.Program

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

/// <summary>
/// Entry point for the DotnetMatplotlib MCP server. Speaks the Model Context
/// Protocol over stdio, exposing the plotting tools in <see cref="PlotTools"/>.
/// </summary>
[<EntryPoint>]
let main argv =
    let builder = Host.CreateApplicationBuilder argv

    // MCP uses stdout for the protocol; all logging must go to stderr.
    builder.Logging.AddConsole(fun o -> o.LogToStandardErrorThreshold <- LogLevel.Trace)
    |> ignore

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly()
    |> ignore

    builder.Build().Run()
    0
