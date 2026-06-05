namespace Matplotlib

open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering
open Matplotlib.Domain.Artists
open Matplotlib.Backends

/// <summary>
/// A stateful, MATLAB-style plotting facade over the object-oriented API,
/// tracking a "current figure" and "current axes".
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.pyplot</c>. The object-oriented API
/// (<c>Figure</c>, <c>Axes</c>) remains fully usable without this facade.
/// </remarks>
type Pyplot() =

    let mutable currentFigure: Figure option = None
    let mutable currentAxes: Axes option = None

    member private this.EnsureAxes() : Axes =
        match currentAxes with
        | Some ax -> ax
        | None ->
            let fig = this.Figure()
            let ax = fig.AddSubplot()
            currentAxes <- Some ax
            ax

    /// <summary>Create a new current figure (Matplotlib's <c>plt.figure</c>).</summary>
    member _.Figure(?width: float, ?height: float, ?dpi: float) : Figure =
        let fig = Figure()

        match width, height with
        | Some w, Some h -> fig.SizeInches <- { Width = w; Height = h }
        | _ -> ()

        dpi |> Option.iter (fun d -> fig.Dpi <- d)
        currentFigure <- Some fig
        currentAxes <- None
        fig

    /// <summary>The current figure, creating one if necessary.</summary>
    member this.CurrentFigure() : Figure =
        match currentFigure with
        | Some f -> f
        | None -> this.Figure()

    /// <summary>The current axes, creating figure/axes if necessary.</summary>
    member this.CurrentAxes() : Axes = this.EnsureAxes()

    /// <summary>Plot y versus x as a line (Matplotlib's <c>plt.plot</c>).</summary>
    member this.Plot
        (
            xs: float[],
            ys: float[],
            ?color: string,
            ?lineStyle: string,
            ?marker: string,
            ?lineWidth: float,
            ?label: string
        ) : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let lineStyleOpt = lineStyle |> Option.map Styles.parseLineStyle
        let markerOpt = marker |> Option.map Styles.parseMarker

        ax.Plot(
            xs,
            ys,
            ?color = colorOpt,
            ?lineStyle = lineStyleOpt,
            ?marker = markerOpt,
            ?lineWidth = lineWidth,
            ?label = label
        )

    /// <summary>Draw a scatter of markers (Matplotlib's <c>plt.scatter</c>).</summary>
    member this.Scatter
        (xs: float[], ys: float[], ?color: string, ?marker: string, ?markerSize: float, ?label: string)
        : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let markerOpt = marker |> Option.map Styles.parseMarker
        ax.Scatter(xs, ys, ?color = colorOpt, ?marker = markerOpt, ?markerSize = markerSize, ?label = label)

    /// <summary>Draw a vertical bar chart (Matplotlib's <c>plt.bar</c>).</summary>
    member this.Bar
        (x: float[], height: float[], ?width: float, ?bottom: float[], ?color: string, ?label: string)
        : Rectangle[] =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.Bar(x, height, ?width = width, ?bottom = bottom, ?color = colorOpt, ?label = label)

    /// <summary>Draw a horizontal bar chart (Matplotlib's <c>plt.barh</c>).</summary>
    member this.BarH
        (y: float[], width: float[], ?height: float, ?left: float[], ?color: string, ?label: string)
        : Rectangle[] =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.BarH(y, width, ?height = height, ?left = left, ?color = colorOpt, ?label = label)

    /// <summary>Fill the area between two curves (Matplotlib's <c>plt.fill_between</c>).</summary>
    member this.FillBetween
        (x: float[], y1: float[], ?y2: float[], ?color: string, ?alpha: float, ?label: string)
        : Polygon =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.FillBetween(x, y1, ?y2 = y2, ?color = colorOpt, ?alpha = alpha, ?label = label)

    /// <summary>Draw a step plot (Matplotlib's <c>plt.step</c>).</summary>
    member this.Step
        (x: float[], y: float[], ?where: string, ?color: string, ?lineStyle: string, ?label: string)
        : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let whereOpt = where |> Option.map Styles.parseStepWhere
        let lineStyleOpt = lineStyle |> Option.map Styles.parseLineStyle
        ax.Step(x, y, ?where = whereOpt, ?color = colorOpt, ?lineStyle = lineStyleOpt, ?label = label)

    /// <summary>Draw a line with error bars (Matplotlib's <c>plt.errorbar</c>).</summary>
    member this.Errorbar
        (x: float[], y: float[], ?yerr: float[], ?xerr: float[], ?color: string, ?marker: string, ?label: string)
        : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let markerOpt = marker |> Option.map Styles.parseMarker
        ax.Errorbar(x, y, ?yerr = yerr, ?xerr = xerr, ?color = colorOpt, ?marker = markerOpt, ?label = label)

    /// <summary>Draw a stem plot (Matplotlib's <c>plt.stem</c>).</summary>
    member this.Stem(x: float[], y: float[], ?bottom: float, ?color: string, ?label: string) : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.Stem(x, y, ?bottom = bottom, ?color = colorOpt, ?label = label)

    /// <summary>Enable (or disable) minor ticks (Matplotlib's <c>plt.minorticks_on</c>).</summary>
    member this.MinorTicks(?on: bool) =
        let ax = this.EnsureAxes()

        if defaultArg on true then
            ax.MinorTicksOn()
        else
            ax.MinorTicksOff()

    /// <summary>Adjust tick parameters on the current axes (direction subset).</summary>
    member this.TickParams(?direction: string) = this.EnsureAxes().TickParams(?direction = direction)

    /// <summary>Show or hide a spine by side (<c>top/bottom/left/right</c>).</summary>
    member this.SpineVisible(side: string, visible: bool) = this.EnsureAxes().SetSpineVisible(side, visible)

    /// <summary>Create a grid of subplots (Matplotlib's <c>plt.subplots</c>).</summary>
    member this.Subplots(?nrows: int, ?ncols: int, ?width: float, ?height: float, ?dpi: float) : Figure * Axes[,] =
        let fig = this.Figure(?width = width, ?height = height, ?dpi = dpi)
        let axes = fig.Subplots(defaultArg nrows 1, defaultArg ncols 1)
        currentAxes <- Some axes[0, 0]
        fig, axes

    /// <summary>Set the title of the current axes.</summary>
    member this.Title(text: string) = this.EnsureAxes().SetTitle text

    /// <summary>Set the x-axis label of the current axes.</summary>
    member this.XLabel(text: string) = this.EnsureAxes().SetXLabel text

    /// <summary>Set the y-axis label of the current axes.</summary>
    member this.YLabel(text: string) = this.EnsureAxes().SetYLabel text

    /// <summary>Set the x view limits of the current axes.</summary>
    member this.XLim(lower: float, upper: float) = this.EnsureAxes().SetXLim(lower, upper)

    /// <summary>Set the y view limits of the current axes.</summary>
    member this.YLim(lower: float, upper: float) = this.EnsureAxes().SetYLim(lower, upper)

    /// <summary>Display a 2D array as a colormapped image (Matplotlib's <c>plt.imshow</c>).</summary>
    member this.Imshow(data: float[,], ?cmap: string, ?vmin: float, ?vmax: float) : AxesImage =
        this.EnsureAxes().Imshow(data, ?cmap = cmap, ?vmin = vmin, ?vmax = vmax)

    /// <summary>Set the x-axis scale (<c>"linear"</c> / <c>"log"</c>).</summary>
    member this.XScale(name: string) = this.EnsureAxes().SetXScale name

    /// <summary>Set the y-axis scale (<c>"linear"</c> / <c>"log"</c>).</summary>
    member this.YScale(name: string) = this.EnsureAxes().SetYScale name

    /// <summary>Show the legend on the current axes, optionally at a location.</summary>
    member this.Legend(?loc: string) =
        let locOpt = loc |> Option.map Styles.parseLegendLoc
        this.EnsureAxes().Legend(?loc = locOpt)

    /// <summary>Add text at a data-space position (Matplotlib's <c>plt.text</c>).</summary>
    member this.Text
        (
            x: float,
            y: float,
            content: string,
            ?color: string,
            ?fontSize: float,
            ?rotation: float,
            ?ha: string,
            ?va: string
        ) : Text =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun c -> ColorResolver.Default.Resolve c)

        let haOpt =
            ha
            |> Option.map (function
                | "left" -> HLeft
                | "center" -> HCenter
                | "right" -> HRight
                | o -> failwith $"Unknown horizontal alignment '{o}'.")

        let vaOpt =
            va
            |> Option.map (function
                | "top" -> VTop
                | "center" -> VCenter
                | "bottom" -> VBottom
                | "baseline" -> VBaseline
                | o -> failwith $"Unknown vertical alignment '{o}'.")

        ax.Text(
            x,
            y,
            content,
            ?color = colorOpt,
            ?fontSize = fontSize,
            ?rotation = rotation,
            ?hAlign = haOpt,
            ?vAlign = vaOpt
        )

    /// <summary>Annotate a point with text and an optional arrow (Matplotlib's <c>plt.annotate</c>).</summary>
    member this.Annotate
        (content: string, xy: float * float, ?xytext: float * float, ?arrow: bool, ?color: string)
        : Text =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun c -> ColorResolver.Default.Resolve c)
        let toPoint (a: float, b: float) : Point2D = { X = a; Y = b }
        let xytextOpt = xytext |> Option.map toPoint
        ax.Annotate(content, toPoint xy, ?xytext = xytextOpt, ?arrow = arrow, ?color = colorOpt)

    /// <summary>Toggle grid lines on the current axes.</summary>
    member this.Grid(?visible: bool) = this.EnsureAxes().Grid(defaultArg visible true)

    /// <summary>Tighten margins so labels/titles fit (Matplotlib's <c>plt.tight_layout</c>).</summary>
    member this.TightLayout(?pad: float) = this.CurrentFigure().TightLayout(?pad = pad)

    /// <summary>Lay out subplots reserving per-subplot decoration space (<c>constrained_layout</c>).</summary>
    member this.ConstrainedLayout(?pad: float, ?wPad: float, ?hPad: float) =
        this.CurrentFigure().ConstrainedLayout(?pad = pad, ?wPad = wPad, ?hPad = hPad)

    /// <summary>Render the current figure to an SVG string.</summary>
    member this.ToSvg() : string = FigureCanvas(this.CurrentFigure()).RenderToSvg()

    /// <summary>Save the current figure to an SVG file (Matplotlib's <c>plt.savefig</c>).</summary>
    member this.Savefig(path: string) = FigureCanvas(this.CurrentFigure()).SaveSvg path

    /// <summary>A fresh, independent pyplot state.</summary>
    static member Instance = Pyplot()
