module Matplotlib.Samples.Gallery.Program

open System
open System.IO
open Matplotlib
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Artists

let private renderSine (outDir: string) =
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
    let categories = [| 0.0; 1.0; 2.0; 3.0; 4.0 |]
    let values = [| 5.0; 9.0; 3.0; 7.0; 6.0 |]
    let errors = [| 0.8; 1.2; 0.5; 0.9; 0.7 |]

    plt.Bar(categories, values, color = "C0", label = "count", yerr = errors, capsize = 5.0)
    |> ignore

    plt.Title "dotnet-matplotlib: bar (yerr)"
    plt.XLabel "category"
    plt.YLabel "count"
    plt.Legend()
    let path = Path.Combine(outDir, "bar.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderFillBetween (outDir: string) =
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
    let xs = [| 1.0; 2.0; 3.0; 4.0; 5.0 |]
    let ys = xs |> Array.map (fun x -> x * 1.5 + 1.0)
    let yerr = [| 0.5; 0.8; 0.4; 0.9; 0.6 |]

    plt.Errorbar(xs, ys, yerr = yerr, color = "C3", marker = "o", capsize = 4.0, label = "measured")
    |> ignore

    plt.Title "dotnet-matplotlib: errorbar (capsize)"
    plt.XLabel "x"
    plt.YLabel "y"
    plt.Legend()
    let path = Path.Combine(outDir, "errorbar.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderStem (outDir: string) =
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
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
    let plt = Plt()
    let n = 40

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
    let plt = Plt()
    let n = 60

    let data =
        Array2D.init n n (fun i j ->
            let x = float j / float n * 6.0 - 3.0
            let y = float i / float n * 6.0 - 3.0

            exp (-((x - 1.0) ** 2.0 + y ** 2.0))
            + 0.8 * exp (-((x + 1.3) ** 2.0 + (y + 0.8) ** 2.0) / 0.6))

    plt.Contour(data, cmap = "viridis") |> ignore
    plt.Title "dotnet-matplotlib: contour"
    plt.XLabel "column"
    plt.YLabel "row"
    let path = Path.Combine(outDir, "contour.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderContourf (outDir: string) =
    let plt = Plt()
    let n = 40

    let data =
        Array2D.init n n (fun i j ->
            let x = float j / float n * 6.0 - 3.0
            let y = float i / float n * 6.0 - 3.0

            exp (-((x - 1.0) ** 2.0 + y ** 2.0))
            + 0.8 * exp (-((x + 1.3) ** 2.0 + (y + 0.8) ** 2.0) / 0.6))

    plt.Contourf(data, cmap = "viridis") |> ignore
    plt.Title "dotnet-matplotlib: contourf (filled bands)"
    plt.XLabel "column"
    plt.YLabel "row"
    let path = Path.Combine(outDir, "contourf.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderCategoricalBar (outDir: string) =
    let plt = Plt()

    plt.Bar([| "alpha"; "beta"; "gamma"; "delta" |], [| 4.0; 7.0; 3.0; 6.0 |], color = "C0")
    |> ignore

    plt.Title "dotnet-matplotlib: categorical bar"
    plt.YLabel "value"
    let path = Path.Combine(outDir, "category.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderDates (outDir: string) =
    let plt = Plt()
    let dates = [| for d in 0..36 -> DateTime(2024, 1, 1).AddDays(float d * 10.0) |]
    let ys = dates |> Array.mapi (fun i _ -> sin (float i / 3.0) + float i * 0.1)
    plt.PlotDate(dates, ys, color = "C1", label = "series") |> ignore
    plt.Title "dotnet-matplotlib: dates"
    plt.XLabel "date"
    plt.Legend()
    plt.TightLayout()
    let path = Path.Combine(outDir, "dates.svg")
    plt.Savefig path
    printfn "wrote %s" path

let private renderScatterColormap (outDir: string) =
    let plt = Plt()
    let rng = Random 7
    let n = 70
    let xs = [| for _ in 1..n -> rng.NextDouble() * 10.0 |]
    let ys = [| for _ in 1..n -> rng.NextDouble() * 10.0 |]
    // color by x + y, and vary the marker AREA per point (a bubble chart)
    let cvals = Array.map2 (+) xs ys
    let sizes = [| for _ in 1..n -> 30.0 + rng.NextDouble() * 500.0 |]
    let sc = plt.Scatter(xs, ys, c = cvals, cmap = "viridis", sizes = sizes)
    plt.Colorbar sc |> ignore
    plt.Title "dotnet-matplotlib: scatter (c + cmap, sized)"
    plt.XLabel "x"
    plt.YLabel "y"
    let path = Path.Combine(outDir, "scatter_colormap.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderHist (outDir: string) =
    let plt = Plt()
    let rng = Random 11
    // sum of 12 uniforms ~ N(0,1) (central limit), shifted/scaled
    let sample () = (Array.init 12 (fun _ -> rng.NextDouble()) |> Array.sum) - 6.0
    let data = [| for _ in 1..2000 -> sample () * 1.5 + 5.0 |]
    plt.Hist(data, bins = 30, color = "C0", label = "samples") |> ignore
    plt.Title "dotnet-matplotlib: hist"
    plt.XLabel "value"
    plt.YLabel "count"
    plt.Legend()
    let path = Path.Combine(outDir, "hist.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderFillBetweenx (outDir: string) =
    let plt = Plt()
    let ys = [| for i in 0..60 -> float i / 6.0 |]
    let x1 = ys |> Array.map sin
    let x2 = ys |> Array.map (fun y -> sin y - 1.0)

    plt.FillBetweenx(ys, x1, x2 = x2, color = "C2", alpha = 0.4, label = "band")
    |> ignore

    plt.Plot(x1, ys, color = "C2") |> ignore
    plt.Title "dotnet-matplotlib: fill_betweenx"
    plt.XLabel "x"
    plt.YLabel "y"
    plt.Legend()
    let path = Path.Combine(outDir, "fill_betweenx.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderColormaps (outDir: string) =
    let plt = Plt()
    let fig, axes = plt.Subplots(nrows = 3, ncols = 1)
    let gradient = Array2D.init 16 256 (fun _ j -> float j)

    [| "viridis"; "jet"; "hot" |]
    |> Array.iteri (fun i name ->
        axes[i, 0].Imshow(gradient, cmap = name) |> ignore
        axes[i, 0].SetTitle name)

    fig.ConstrainedLayout()
    let path = Path.Combine(outDir, "colormaps.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderStackplot (outDir: string) =
    let plt = Plt()
    let x = [| for i in 0..40 -> float i / 4.0 |]

    let ys =
        [|
            x |> Array.map (fun t -> 1.0 + 0.5 * sin t)
            x |> Array.map (fun t -> 1.5 + 0.5 * cos (t * 0.7))
            x |> Array.map (fun t -> 1.0 + 0.4 * sin (t * 1.3 + 1.0))
        |]

    plt.Stackplot(x, ys, labels = [| "a"; "b"; "c" |]) |> ignore
    plt.Title "dotnet-matplotlib: stackplot"
    plt.XLabel "t"
    plt.YLabel "stacked"
    plt.Legend()
    let path = Path.Combine(outDir, "stackplot.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderVlines (outDir: string) =
    let plt = Plt()
    let xs = [| for i in 0..30 -> float i / 3.0 |]
    let ys = xs |> Array.map (fun x -> exp (-x / 5.0) * cos x)
    plt.Vlines(xs, Array.zeroCreate xs.Length, ys, color = "C0", label = "vlines")
    plt.Hlines([| 0.0 |], [| 0.0 |], [| 10.0 |], color = "C3", label = "baseline")
    plt.Scatter(xs, ys, color = "C0", marker = "o", s = 18.0) |> ignore
    plt.Title "dotnet-matplotlib: vlines / hlines"
    plt.XLabel "t"
    plt.Legend()
    let path = Path.Combine(outDir, "vlines.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderPie (outDir: string) =
    let plt = Plt()
    plt.Figure(width = 5.0, height = 5.0) |> ignore // square figure -> round pie

    plt.Pie([| 35.0; 25.0; 20.0; 15.0; 5.0 |], labels = [| "A"; "B"; "C"; "D"; "E" |])
    |> ignore

    plt.Title "dotnet-matplotlib: pie"
    plt.Legend()
    let path = Path.Combine(outDir, "pie.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderCustomTicks (outDir: string) =
    let plt = Plt()
    let xs = [| for i in 0..100 -> float i / 100.0 * 2.0 * Math.PI |]
    let ys = xs |> Array.map sin
    plt.Plot(xs, ys, color = "C0", label = "sin(x)") |> ignore

    plt.XTicks(
        [| 0.0; Math.PI / 2.0; Math.PI; 3.0 * Math.PI / 2.0; 2.0 * Math.PI |],
        labels = [| "0"; "pi/2"; "pi"; "3pi/2"; "2pi" |]
    )

    plt.YTicks([| -1.0; 0.0; 1.0 |], labels = [| "min"; "zero"; "max" |])
    plt.Title "dotnet-matplotlib: custom ticks"
    plt.XLabel "x"
    plt.Legend()
    let path = Path.Combine(outDir, "custom_ticks.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderRefLines (outDir: string) =
    let plt = Plt()
    let xs = [| for i in 0..120 -> float i / 120.0 * 10.0 |]
    let ys = xs |> Array.map (fun x -> sin x + 0.1 * x)
    plt.AxHSpan(0.5, 1.5, color = "C1", alpha = 0.2) // highlighted band (backdrop)
    plt.Plot(xs, ys, color = "C0", label = "signal") |> ignore
    plt.AxHLine(1.0, color = "C3") // threshold
    plt.AxVLine(5.0, color = "C2") // event marker
    plt.Title "dotnet-matplotlib: axhline / axvline / axhspan"
    plt.XLabel "x"
    plt.YLabel "y"
    plt.Legend()
    let path = Path.Combine(outDir, "reflines.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderTwinx (outDir: string) =
    let plt = Plt()
    let xs = [| for i in 0..50 -> float i / 5.0 |]
    plt.Plot(xs, xs |> Array.map sin, color = "C0") |> ignore
    plt.XLabel "x"
    plt.YLabel "sin(x)  (left)"
    let ax2 = plt.TwinX()

    ax2.Plot(xs, xs |> Array.map (fun x -> 100.0 * exp (x / 5.0)), color = ColorResolver.Default.Resolve "C1")
    |> ignore

    ax2.SetYLabel "100·exp(x/5)  (right)"
    plt.Title "dotnet-matplotlib: twinx"
    let path = Path.Combine(outDir, "twinx.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderStreamplot (outDir: string) =
    let plt = Plt()
    let n = 25
    let xs = Array.init n (fun i -> -3.0 + 6.0 * float i / float (n - 1))
    let ys = xs
    // a swirl (rotation + gentle outward spiral) — RK4 keeps the streamlines smooth
    let u = Array2D.init n n (fun r c -> -ys[r] + 0.3 * xs[c])
    let v = Array2D.init n n (fun r c -> xs[c] + 0.3 * ys[r])
    plt.Streamplot(xs, ys, u, v, density = 2)
    plt.Title "dotnet-matplotlib: streamplot (RK4)"
    plt.XLabel "x"
    plt.YLabel "y"
    let path = Path.Combine(outDir, "streamplot.png")
    plt.Savefig path
    printfn "wrote %s" path

let private renderTable (outDir: string) =
    let plt = Plt()
    let headers = [| "Region"; "Q3"; "Q4"; "YoY" |]

    let rows =
        [|
            [| "NA"; "120"; "138"; "+15%" |]
            [| "EU"; "90"; "96"; "+7%" |]
            [| "APAC"; "150"; "171"; "+14%" |]
            [| "LATAM"; "60"; "66"; "+10%" |]
        |]

    plt.Table(rows, colLabels = headers)
    plt.Title "dotnet-matplotlib: table"
    let path = Path.Combine(outDir, "table.png")
    plt.Savefig path
    printfn "wrote %s" path

[<EntryPoint>]
let main argv =
    let outDir = if argv.Length > 0 then argv[0] else "out"
    Directory.CreateDirectory outDir |> ignore
    renderContour outDir
    renderCategoricalBar outDir
    renderDates outDir
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
    renderScatterColormap outDir
    renderHist outDir
    renderFillBetweenx outDir
    renderColormaps outDir
    renderStackplot outDir
    renderVlines outDir
    renderPie outDir
    renderCustomTicks outDir
    renderRefLines outDir
    renderTwinx outDir
    renderStreamplot outDir
    renderContourf outDir
    renderTable outDir
    0
