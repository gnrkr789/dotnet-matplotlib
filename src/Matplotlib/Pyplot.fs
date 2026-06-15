namespace Matplotlib

open System
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
    let mutable currentAxes3D: Axes3D option = None
    let mutable rcParams = RcParams.Default

    member private this.EnsureAxes() : Axes =
        match currentAxes with
        | Some ax -> ax
        | None ->
            let fig = this.Figure()
            let ax = fig.AddSubplot()
            currentAxes <- Some ax
            ax

    /// <summary>
    /// The default font family applied to figures created afterward (Matplotlib's
    /// <c>rcParams["font.family"]</c>). Set this before plotting; e.g.
    /// <c>plt.FontFamily &lt;- "맑은 고딕"</c> to render Korean text. Generic names
    /// (<c>"sans-serif"</c>, <c>"serif"</c>, <c>"monospace"</c>) are mapped to a
    /// concrete font by each backend.
    /// </summary>
    member _.FontFamily
        with get () = rcParams.FontFamily
        and set (name: string) = rcParams <- { rcParams with FontFamily = name }

    /// <summary>Apply a built-in style sheet to subsequent figures (Matplotlib's <c>plt.style.use</c>).</summary>
    member _.UseStyle(name: string) = rcParams <- StyleSheet.byName name rcParams

    /// <summary>Apply rcParams overrides from a matplotlibrc-style text block.</summary>
    member _.UseStyleText(text: string) = rcParams <- StyleSheet.parseText text rcParams

    /// <summary>Apply rcParams overrides from a matplotlibrc-style file.</summary>
    member _.UseStyleFile(path: string) = rcParams <- StyleSheet.parseText (System.IO.File.ReadAllText path) rcParams

    /// <summary>The available built-in style names.</summary>
    member _.AvailableStyles = StyleSheet.names

    /// <summary>Create a new current figure (Matplotlib's <c>plt.figure</c>).</summary>
    member _.Figure(?width: float, ?height: float, ?dpi: float) : Figure =
        let fig = Figure(rcParams)

        match width, height with
        | Some w, Some h -> fig.SizeInches <- { Width = w; Height = h }
        | _ -> ()

        dpi |> Option.iter (fun d -> fig.Dpi <- d)
        currentFigure <- Some fig
        currentAxes <- None
        currentAxes3D <- None
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
        (
            xs: float[],
            ys: float[],
            ?color: string,
            ?marker: string,
            ?s: float,
            ?label: string,
            ?c: float[],
            ?cmap: string,
            ?vmin: float,
            ?vmax: float,
            ?sizes: float[]
        ) : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun col -> ColorResolver.Default.Resolve col)
        let markerOpt = marker |> Option.map Styles.parseMarker

        ax.Scatter(
            xs,
            ys,
            ?color = colorOpt,
            ?marker = markerOpt,
            ?s = s,
            ?label = label,
            ?c = c,
            ?cmap = cmap,
            ?vmin = vmin,
            ?vmax = vmax,
            ?sizes = sizes
        )

    /// <summary>Draw a vertical bar chart (Matplotlib's <c>plt.bar</c>).</summary>
    member this.Bar
        (
            x: float[],
            height: float[],
            ?width: float,
            ?bottom: float[],
            ?color: string,
            ?label: string,
            ?hatch: string,
            ?yerr: float[],
            ?capsize: float
        ) : Rectangle[] =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)

        let rects =
            ax.Bar(
                x,
                height,
                ?width = width,
                ?bottom = bottom,
                ?color = colorOpt,
                ?label = label,
                ?yerr = yerr,
                ?capsize = capsize
            )

        hatch |> Option.iter (fun h -> rects |> Array.iter (fun r -> r.Hatch <- Some h))
        rects

    /// <summary>Bar chart over named categories (Matplotlib's <c>plt.bar</c> with string labels).</summary>
    member this.Bar(categories: string[], heights: float[], ?color: string, ?label: string) : Rectangle[] =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let rects = ax.Bar(Array.init categories.Length float, heights, ?color = colorOpt, ?label = label)
        ax.SetXCategories categories
        rects

    /// <summary>Draw a horizontal bar chart (Matplotlib's <c>plt.barh</c>).</summary>
    member this.BarH
        (y: float[], width: float[], ?height: float, ?left: float[], ?color: string, ?label: string)
        : Rectangle[] =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.BarH(y, width, ?height = height, ?left = left, ?color = colorOpt, ?label = label)

    /// <summary>Draw a histogram (Matplotlib's <c>plt.hist</c>).</summary>
    member this.Hist
        (x: float[], ?bins: int, ?range: float * float, ?density: bool, ?color: string, ?label: string)
        : float[] * float[] =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.Hist(x, ?bins = bins, ?range = range, ?density = density, ?color = colorOpt, ?label = label)

    /// <summary>Fill the area between two curves (Matplotlib's <c>plt.fill_between</c>).</summary>
    member this.FillBetween
        (x: float[], y1: float[], ?y2: float[], ?color: string, ?alpha: float, ?label: string)
        : Polygon =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.FillBetween(x, y1, ?y2 = y2, ?color = colorOpt, ?alpha = alpha, ?label = label)

    /// <summary>Fill the area between two vertical curves (Matplotlib's <c>plt.fill_betweenx</c>).</summary>
    member this.FillBetweenx
        (y: float[], x1: float[], ?x2: float[], ?color: string, ?alpha: float, ?label: string)
        : Polygon =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.FillBetweenx(y, x1, ?x2 = x2, ?color = colorOpt, ?alpha = alpha, ?label = label)

    /// <summary>Draw stacked filled areas (Matplotlib's <c>plt.stackplot</c>).</summary>
    member this.Stackplot(x: float[], ys: float[][], ?labels: string[]) : Polygon[] =
        this.EnsureAxes().Stackplot(x, ys, ?labels = labels)

    /// <summary>Draw vertical line segments (Matplotlib's <c>plt.vlines</c>).</summary>
    member this.Vlines(x: float[], ymin: float[], ymax: float[], ?color: string, ?label: string) =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.Vlines(x, ymin, ymax, ?color = colorOpt, ?label = label)

    /// <summary>Draw horizontal line segments (Matplotlib's <c>plt.hlines</c>).</summary>
    member this.Hlines(y: float[], xmin: float[], xmax: float[], ?color: string, ?label: string) =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.Hlines(y, xmin, xmax, ?color = colorOpt, ?label = label)

    /// <summary>Draw a pie chart (Matplotlib's <c>plt.pie</c>). Use a square figure for a round pie.</summary>
    member this.Pie(values: float[], ?labels: string[], ?startAngle: float) : Polygon[] =
        this.EnsureAxes().Pie(values, ?labels = labels, ?startAngle = startAngle)

    /// <summary>Configure the axis (Matplotlib's <c>plt.axis</c>): "off", "equal", "auto", or "on".</summary>
    member this.Axis(mode: string) =
        let ax = this.EnsureAxes()

        match mode with
        | "off" -> ax.SetAxisOff()
        | "equal" -> ax.SetAspect "equal"
        | "auto" -> ax.SetAspect "auto"
        | "on" ->
            ax.XTicksVisible <- true
            ax.YTicksVisible <- true
        | other -> failwith $"Unknown axis mode '{other}'."

    /// <summary>Horizontal reference line at <paramref name="y"/> (Matplotlib's <c>plt.axhline</c>).</summary>
    member this.AxHLine(y: float, ?xmin: float, ?xmax: float, ?color: string) =
        let ax = this.EnsureAxes()
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.AxHLine(y, ?xmin = xmin, ?xmax = xmax, ?color = c)

    /// <summary>Vertical reference line at <paramref name="x"/> (Matplotlib's <c>plt.axvline</c>).</summary>
    member this.AxVLine(x: float, ?ymin: float, ?ymax: float, ?color: string) =
        let ax = this.EnsureAxes()
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.AxVLine(x, ?ymin = ymin, ?ymax = ymax, ?color = c)

    /// <summary>Shade a horizontal band between <paramref name="ymin"/>/<paramref name="ymax"/> (Matplotlib's <c>plt.axhspan</c>).</summary>
    member this.AxHSpan(ymin: float, ymax: float, ?color: string, ?alpha: float) =
        let ax = this.EnsureAxes()
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.AxHSpan(ymin, ymax, ?color = c, ?alpha = alpha)

    /// <summary>Shade a vertical band between <paramref name="xmin"/>/<paramref name="xmax"/> (Matplotlib's <c>plt.axvspan</c>).</summary>
    member this.AxVSpan(xmin: float, xmax: float, ?color: string, ?alpha: float) =
        let ax = this.EnsureAxes()
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.AxVSpan(xmin, xmax, ?color = c, ?alpha = alpha)

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
        (
            x: float[],
            y: float[],
            ?yerr: float[],
            ?xerr: float[],
            ?color: string,
            ?marker: string,
            ?capsize: float,
            ?label: string
        ) : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let markerOpt = marker |> Option.map Styles.parseMarker

        ax.Errorbar(
            x,
            y,
            ?yerr = yerr,
            ?xerr = xerr,
            ?color = colorOpt,
            ?marker = markerOpt,
            ?capsize = capsize,
            ?label = label
        )

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

    /// <summary>Add a colorbar for an image (Matplotlib's <c>plt.colorbar</c>).</summary>
    member this.Colorbar(image: AxesImage, ?ax: Axes) = this.CurrentFigure().Colorbar(image, ?ax = ax)

    /// <summary>Add a colorbar for a colormapped artist such as a <c>scatter</c> (Matplotlib's <c>plt.colorbar</c>).</summary>
    member this.Colorbar(mappable: Line2D, ?ax: Axes) = this.CurrentFigure().Colorbar(mappable, ?ax = ax)

    /// <summary>Create a twin axes sharing the x-axis (Matplotlib's <c>plt.twinx</c>); it becomes the current axes.</summary>
    member this.TwinX() : Axes =
        let parent = this.EnsureAxes()
        let twin = this.CurrentFigure().AddTwinX parent
        currentAxes <- Some twin
        twin

    /// <summary>Draw a quad mesh of a 2D array (Matplotlib's <c>plt.pcolormesh</c>).</summary>
    member this.Pcolormesh(data: float[,], ?cmap: string, ?vmin: float, ?vmax: float) : AxesImage =
        this.EnsureAxes().Pcolormesh(data, ?cmap = cmap, ?vmin = vmin, ?vmax = vmax)

    /// <summary>Draw contour lines of a 2D field (Matplotlib's <c>plt.contour</c>).</summary>
    member this.Contour(data: float[,], ?levels: float[], ?cmap: string) : float[] =
        this.EnsureAxes().Contour(data, ?levels = levels, ?cmap = cmap)

    /// <summary>Draw filled contour bands of a 2D field (Matplotlib's <c>plt.contourf</c>).</summary>
    member this.Contourf(data: float[,], ?levels: float[], ?cmap: string) : float[] =
        this.EnsureAxes().Contourf(data, ?levels = levels, ?cmap = cmap)

    /// <summary>Draw a field of arrows (Matplotlib's <c>plt.quiver</c>).</summary>
    member this.Quiver(x: float[], y: float[], u: float[], v: float[], ?scale: float, ?color: string) : unit =
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        this.EnsureAxes().Quiver(x, y, u, v, ?scale = scale, ?color = colorOpt)

    /// <summary>Draw a 2D histogram image (Matplotlib's <c>plt.hist2d</c>).</summary>
    member this.Hist2d(x: float[], y: float[], ?bins: int, ?cmap: string) : AxesImage =
        this.EnsureAxes().Hist2d(x, y, ?bins = bins, ?cmap = cmap)

    /// <summary>Draw box-and-whisker plots (Matplotlib's <c>plt.boxplot</c>).</summary>
    member this.Boxplot(data: float[][], ?positions: float[], ?width: float) : unit =
        this.EnsureAxes().Boxplot(data, ?positions = positions, ?width = width)

    /// <summary>Draw violin plots (Matplotlib's <c>plt.violinplot</c>).</summary>
    member this.Violinplot(data: float[][], ?positions: float[], ?width: float) : unit =
        this.EnsureAxes().Violinplot(data, ?positions = positions, ?width = width)

    /// <summary>Draw streamlines of a vector field (Matplotlib's <c>plt.streamplot</c>).</summary>
    member this.Streamplot(x: float[], y: float[], u: float[,], v: float[,], ?density: int, ?color: string) : unit =
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        this.EnsureAxes().Streamplot(x, y, u, v, ?density = density, ?color = colorOpt)

    /// <summary>Add a 3D axes to the current figure (Matplotlib's <c>add_subplot(projection='3d')</c>).</summary>
    member this.Axes3D() : Axes3D =
        let ax = this.CurrentFigure().AddAxes3D()
        currentAxes3D <- Some ax
        ax

    member private this.Ensure3D() : Axes3D =
        match currentAxes3D with
        | Some a -> a
        | None -> this.Axes3D()

    /// <summary>Plot a 3D line (Matplotlib's <c>plt.plot</c> on a 3D axes).</summary>
    member this.Plot3D(xs: float[], ys: float[], zs: float[], ?color: string) : Axes3D =
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let ax = this.Ensure3D()
        ax.Plot3D(xs, ys, zs, ?color = c)
        ax

    /// <summary>Scatter 3D points (Matplotlib's <c>plt.scatter</c> on a 3D axes).</summary>
    member this.Scatter3D(xs: float[], ys: float[], zs: float[], ?color: string) : Axes3D =
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let ax = this.Ensure3D()
        ax.Scatter3D(xs, ys, zs, ?color = c)
        ax

    /// <summary>Draw a 3D wireframe surface (Matplotlib's <c>plot_wireframe</c>).</summary>
    member this.PlotWireframe(x: float[], y: float[], z: float[,], ?color: string) : Axes3D =
        let c = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        let ax = this.Ensure3D()
        ax.PlotWireframe(x, y, z, ?color = c)
        ax

    /// <summary>Plot y versus dates with a date-formatted x axis (Matplotlib's <c>plt.plot_date</c>).</summary>
    member this.PlotDate(dates: DateTime[], ys: float[], ?format: string, ?color: string, ?label: string) : Line2D =
        let ax = this.EnsureAxes()
        let colorOpt = color |> Option.map (fun s -> ColorResolver.Default.Resolve s)
        ax.PlotDate(dates, ys, ?format = format, ?color = colorOpt, ?label = label)

    /// <summary>Label the x axis with categories at integer positions.</summary>
    member this.XCategories(categories: string[]) = this.EnsureAxes().SetXCategories categories

    /// <summary>Set the x tick positions, optionally with labels (Matplotlib's <c>plt.xticks</c>).</summary>
    member this.XTicks(positions: float[], ?labels: string[]) =
        let ax = this.EnsureAxes()

        match labels with
        | Some l -> ax.SetXTickLabels(positions, l)
        | None -> ax.SetXTicks positions

    /// <summary>Set the y tick positions, optionally with labels (Matplotlib's <c>plt.yticks</c>).</summary>
    member this.YTicks(positions: float[], ?labels: string[]) =
        let ax = this.EnsureAxes()

        match labels with
        | Some l -> ax.SetYTickLabels(positions, l)
        | None -> ax.SetYTicks positions

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

    /// <summary>
    /// Save the current figure, choosing the format from the file extension
    /// (Matplotlib's <c>plt.savefig</c>): <c>.png</c> uses the pure-managed raster
    /// backend, anything else writes SVG.
    /// </summary>
    member this.Savefig(path: string) =
        let canvas = FigureCanvas(this.CurrentFigure())
        let ext (e: string) = path.EndsWith(e, System.StringComparison.OrdinalIgnoreCase)

        if ext ".png" then canvas.SavePng path
        elif ext ".pdf" then canvas.SavePdf path
        else canvas.SaveSvg path

    /// <summary>
    /// Render <paramref name="frameCount"/> figures and save them as a looping
    /// animated GIF (Matplotlib's <c>FuncAnimation(...).save(gif)</c>).
    /// <paramref name="factory"/> builds the figure for each frame index.
    /// </summary>
    member _.SaveGif(path: string, frameCount: int, factory: int -> Figure, ?fps: int, ?scale: int) =
        Animation(frameCount, factory).SaveGif(path, ?fps = fps, ?scale = scale)

    /// <summary>A fresh, independent pyplot state.</summary>
    static member Instance = Pyplot()
