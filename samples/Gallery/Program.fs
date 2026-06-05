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

let private renderBar (outDir: string) =
    let plt = Pyplot()
    let categories = [| 0.0; 1.0; 2.0; 3.0; 4.0 |]
    let values = [| 5.0; 9.0; 3.0; 7.0; 6.0 |]
    plt.Bar(categories, values, color = "C0", label = "count") |> ignore
    plt.Title "dotnet-matplotlib: bar"
    plt.XLabel "category"
    plt.YLabel "count"
    plt.Legend()
    let path = Path.Combine(outDir, "bar.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderFillBetween (outDir: string) =
    let plt = Pyplot()
    let xs = [| for i in 0..60 -> float i / 6.0 |]
    let ys = xs |> Array.map sin
    plt.FillBetween(xs, ys, color = "C1", alpha = 0.4, label = "sin(x)") |> ignore
    plt.Plot(xs, ys, color = "C1") |> ignore
    plt.Title "dotnet-matplotlib: fill_between"
    plt.XLabel "x"
    plt.Legend()
    let path = Path.Combine(outDir, "fill_between.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderStep (outDir: string) =
    let plt = Pyplot()
    let xs = [| 0.0; 1.0; 2.0; 3.0; 4.0; 5.0 |]
    let ys = [| 1.0; 3.0; 2.0; 4.0; 3.0; 5.0 |]
    plt.Step(xs, ys, where = "mid", color = "C2", label = "level") |> ignore
    plt.Title "dotnet-matplotlib: step"
    plt.XLabel "x"
    plt.Legend()
    let path = Path.Combine(outDir, "step.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderErrorbar (outDir: string) =
    let plt = Pyplot()
    let xs = [| 1.0; 2.0; 3.0; 4.0; 5.0 |]
    let ys = xs |> Array.map (fun x -> x * 1.5 + 1.0)
    let yerr = [| 0.5; 0.8; 0.4; 0.9; 0.6 |]

    plt.Errorbar(xs, ys, yerr = yerr, color = "C3", marker = "o", label = "measured")
    |> ignore

    plt.Title "dotnet-matplotlib: errorbar"
    plt.XLabel "x"
    plt.YLabel "y"
    plt.Legend()
    let path = Path.Combine(outDir, "errorbar.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderStem (outDir: string) =
    let plt = Pyplot()
    let xs = [| for i in 0..20 -> float i / 2.0 |]
    let ys = xs |> Array.map (fun x -> exp (-x / 4.0) * cos x)
    plt.Stem(xs, ys, color = "C4", label = "damped") |> ignore
    plt.Title "dotnet-matplotlib: stem"
    plt.XLabel "t"
    plt.MinorTicks()
    plt.TickParams(direction = "in")
    plt.SpineVisible("top", false)
    plt.SpineVisible("right", false)
    plt.Legend()
    let path = Path.Combine(outDir, "stem.svg")
    plt.Savefig path
    printfn "wrote %s" path

[<EntryPoint>]
let main argv =
    let outDir = if argv.Length > 0 then argv[0] else "out"
    Directory.CreateDirectory outDir |> ignore
    renderSine outDir
    renderScatter outDir
    renderBar outDir
    renderFillBetween outDir
    renderStep outDir
    renderErrorbar outDir
    renderStem outDir
    0
