namespace Matplotlib

open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
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

    /// <summary>Show the legend on the current axes.</summary>
    member this.Legend() = this.EnsureAxes().ShowLegend <- true

    /// <summary>Toggle grid lines on the current axes.</summary>
    member this.Grid(?visible: bool) = this.EnsureAxes().Grid(defaultArg visible true)

    /// <summary>Render the current figure to an SVG string.</summary>
    member this.ToSvg() : string = FigureCanvas(this.CurrentFigure()).RenderToSvg()

    /// <summary>Save the current figure to an SVG file (Matplotlib's <c>plt.savefig</c>).</summary>
    member this.Savefig(path: string) = FigureCanvas(this.CurrentFigure()).SaveSvg path

    /// <summary>A fresh, independent pyplot state.</summary>
    static member Instance = Pyplot()
