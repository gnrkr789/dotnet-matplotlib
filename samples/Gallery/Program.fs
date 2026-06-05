module Matplotlib.Samples.Gallery.Program

open System
open System.IO
open Matplotlib
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Artists

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

let private renderMarkers (outDir: string) =
    let plt = Pyplot()
    let xs = [| for i in 0..9 -> float i |]
    let series = [ "o", "C0"; "s", "C1"; "^", "C2"; "*", "C3"; "D", "C4"; "p", "C5" ]

    series
    |> List.iteri (fun i (marker, color) ->
        let ys = xs |> Array.map (fun x -> x + float (i * 2))
        plt.Scatter(xs, ys, color = color, marker = marker, label = marker) |> ignore)

    plt.Title "dotnet-matplotlib: markers"
    plt.XLabel "x"
    plt.Legend "upper left"
    let path = Path.Combine(outDir, "markers.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderAnnotate (outDir: string) =
    let plt = Pyplot()
    let xs = [| for i in 0..60 -> float i / 6.0 |]
    let ys = xs |> Array.map sin
    plt.Plot(xs, ys, color = "C0", label = "sin(x)") |> ignore

    plt.Annotate("first peak", (1.5708, 1.0), xytext = (3.2, 0.7), arrow = true, color = "C3")
    |> ignore

    plt.Text(7.0, -0.85, "sine wave", color = "C0") |> ignore
    plt.Title "dotnet-matplotlib: annotate"
    plt.XLabel "x"
    plt.Legend "lower left"
    let path = Path.Combine(outDir, "annotate.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderCollections (outDir: string) =
    let plt = Pyplot()
    let ax = plt.CurrentAxes()
    let xs = [| for i in 0..60 -> float i / 6.0 |]

    let segments = [ for k in 0..4 -> xs |> Array.map (fun x -> { X = x; Y = sin x + float k }) ]

    let lc = LineCollection(segments)
    lc.Color <- ColorResolver.Default.Resolve "C0"
    lc.LineWidth <- 1.2
    ax.AddCollection lc |> ignore
    plt.Title "dotnet-matplotlib: LineCollection"
    plt.XLabel "x"
    plt.YLabel "sin(x) + k"
    plt.TightLayout()
    let path = Path.Combine(outDir, "collections.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderSubplots (outDir: string) =
    let plt = Pyplot()
    let fig, axes = plt.Subplots(nrows = 2, ncols = 2)
    let xs = [| for i in 0..40 -> float i / 4.0 |]
    axes[0, 0].Plot(xs, xs |> Array.map sin) |> ignore
    axes[0, 0].SetTitle "sin"
    axes[0, 1].Plot(xs, xs |> Array.map cos) |> ignore
    axes[0, 1].SetTitle "cos"
    axes[1, 0].Bar([| 0.0; 1.0; 2.0; 3.0 |], [| 3.0; 1.0; 4.0; 2.0 |]) |> ignore
    axes[1, 0].SetXLabel "x"
    axes[1, 0].SetYLabel "y"

    axes[1, 1]
        .Scatter(xs, xs |> Array.map (fun x -> sin x * cos x), marker = MarkerStyle.Circle)
    |> ignore

    axes[1, 1].SetTitle "sin·cos"
    fig.ConstrainedLayout()
    let path = Path.Combine(outDir, "subplots.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderLogScale (outDir: string) =
    let plt = Pyplot()
    let xs = [| for i in 0..40 -> 10.0 ** (float i / 10.0) |]
    let ys = xs |> Array.map (fun x -> x ** 1.5)
    plt.Plot(xs, ys, color = "C0", marker = "o", label = "y = x^1.5") |> ignore
    plt.XScale "log"
    plt.YScale "log"
    plt.Title "dotnet-matplotlib: log-log"
    plt.XLabel "x"
    plt.YLabel "y"
    plt.Grid()
    plt.Legend()
    plt.TightLayout()
    let path = Path.Combine(outDir, "loglog.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderImshow (outDir: string) =
    let plt = Pyplot()
    let n = 60

    let data =
        Array2D.init n n (fun i j ->
            let x = float j / float n * 6.0
            let y = float i / float n * 6.0
            sin x * cos y)

    let img = plt.Imshow(data, cmap = "viridis")
    plt.Colorbar img |> ignore
    plt.Title "dotnet-matplotlib: imshow (viridis)"
    plt.XLabel "column"
    plt.YLabel "row"
    let path = Path.Combine(outDir, "imshow.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderContour (outDir: string) =
    let plt = Pyplot()
    let n = 80

    let data =
        Array2D.init n n (fun i j ->
            let x = float j / float n * 6.0 - 3.0
            let y = float i / float n * 6.0 - 3.0
            sin (x * x + y * y))

    plt.Contour(data, cmap = "viridis") |> ignore
    plt.Title "dotnet-matplotlib: contour"
    plt.XLabel "column"
    plt.YLabel "row"
    let path = Path.Combine(outDir, "contour.svg")
    plt.Savefig path
    printfn "wrote %s" path

[<EntryPoint>]
let main argv =
    let outDir = if argv.Length > 0 then argv[0] else "out"
    Directory.CreateDirectory outDir |> ignore
    renderContour outDir
    renderSine outDir
    renderScatter outDir
    renderBar outDir
    renderFillBetween outDir
    renderStep outDir
    renderErrorbar outDir
    renderStem outDir
    renderMarkers outDir
    renderAnnotate outDir
    renderCollections outDir
    renderSubplots outDir
    renderLogScale outDir
    renderImshow outDir
    0
