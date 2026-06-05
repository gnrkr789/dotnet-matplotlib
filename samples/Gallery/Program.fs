module Matplotlib.Samples.Gallery.Program

open System
open System.IO
open Matplotlib

let private renderSine (outDir: string) =
    let plt = Pyplot()
    let xs = [| for i in 0..60 -> float i / 6.0 |]
    let ys = xs |> Array.map sin
    plt.Plot(xs, ys, color = "C0", label = "sin(x)") |> ignore
    plt.Title "dotnet-matplotlib: sine"
    plt.XLabel "x"
    plt.YLabel "sin(x)"
    plt.Grid()
    plt.Legend()
    let path = Path.Combine(outDir, "line_sine.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderScatter (outDir: string) =
    let plt = Pyplot()
    let rng = Random 42
    let xs = [| for _ in 1..40 -> rng.NextDouble() * 10.0 |]
    let ys = xs |> Array.map (fun x -> 2.0 * x + 3.0 + (rng.NextDouble() - 0.5) * 6.0)

    plt.Scatter(xs, ys, color = "tab:red", marker = "o", label = "samples")
    |> ignore

    plt.Title "dotnet-matplotlib: scatter"
    plt.XLabel "x"
    plt.YLabel "y"
    plt.Legend()
    let path = Path.Combine(outDir, "scatter.svg")
    plt.Savefig path
    printfn "wrote %s" path

[<EntryPoint>]
let main argv =
    let outDir = if argv.Length > 0 then argv[0] else "out"
    Directory.CreateDirectory outDir |> ignore
    renderSine outDir
    renderScatter outDir
    0
