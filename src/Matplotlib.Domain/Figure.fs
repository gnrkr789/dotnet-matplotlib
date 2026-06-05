namespace Matplotlib.Domain

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// The top-level container holding one or more <see cref="Axes"/>. Owns the
/// figure size (inches), resolution (dpi) and background color.
/// </summary>
/// <remarks>Ported from <c>matplotlib.figure.Figure</c>.</remarks>
type Figure(rc: RcParams) =

    let axesList = ResizeArray<Axes>()

    /// <summary>Create a figure with the default <c>rcParams</c>.</summary>
    new() = Figure(RcParams.Default)

    /// <summary>The active rcParams snapshot.</summary>
    member _.Rc = rc

    /// <summary>Figure size in inches.</summary>
    member val SizeInches = rc.FigureSizeInches with get, set

    /// <summary>Resolution in dots per inch.</summary>
    member val Dpi = rc.FigureDpi with get, set

    /// <summary>Figure background color.</summary>
    member val FaceColor = rc.FigureFaceColor with get, set

    /// <summary>The Axes contained in this figure.</summary>
    member _.Axes = axesList

    /// <summary>The figure size in device pixels (<c>inches × dpi</c>).</summary>
    member this.PixelSize: Size =
        {
            Width = this.SizeInches.Width * this.Dpi
            Height = this.SizeInches.Height * this.Dpi
        }

    /// <summary>Add an Axes at the given figure-fraction position.</summary>
    member _.AddAxes(position: BBox) : Axes =
        let ax = Axes(rc)
        ax.Position <- position
        axesList.Add ax
        ax

    /// <summary>Add an Axes filling the default subplot area.</summary>
    member _.AddSubplot() : Axes =
        let ax = Axes(rc)
        axesList.Add ax
        ax

    /// <summary>
    /// Create a grid of <paramref name="nrows"/>×<paramref name="ncols"/> Axes,
    /// indexed <c>[row, col]</c> with row 0 at the top.
    /// </summary>
    /// <remarks>
    /// Ported from <c>matplotlib.figure.Figure.subplots</c> / <c>GridSpec</c>.
    /// <paramref name="wspace"/>/<paramref name="hspace"/> are the inter-cell
    /// spacing as a fraction of the average cell size (Matplotlib default 0.2).
    /// </remarks>
    member _.Subplots(nrows: int, ncols: int, ?wspace: float, ?hspace: float) : Axes[,] =
        let ws = defaultArg wspace 0.2
        let hs = defaultArg hspace 0.2
        let left, right = rc.SubplotLeft, rc.SubplotRight
        let bottom, top = rc.SubplotBottom, rc.SubplotTop
        let cellW = (right - left) / (float ncols + ws * float (ncols - 1))
        let cellH = (top - bottom) / (float nrows + hs * float (nrows - 1))
        let sepW = ws * cellW
        let sepH = hs * cellH

        Array2D.init nrows ncols (fun i j ->
            let l = left + float j * (cellW + sepW)
            let t = top - float i * (cellH + sepH)
            let ax = Axes(rc)
            ax.Position <- BBox.fromExtents l (t - cellH) (l + cellW) t
            axesList.Add ax
            ax)

    /// <summary>Render the figure background and every Axes onto the renderer.</summary>
    member this.Draw(renderer: IRenderer) =
        let canvas = renderer.CanvasSizePx

        let gc =
            { GraphicsContext.Default with
                StrokeColor = Color.none
                LineWidth = 0.0
            }

        let corners =
            [
                { X = 0.0; Y = 0.0 }
                { X = canvas.Width; Y = 0.0 }
                { X = canvas.Width; Y = canvas.Height }
                { X = 0.0; Y = canvas.Height }
            ]

        renderer.DrawPath(gc, Path.polygon corners, Some this.FaceColor)

        for ax in axesList do
            ax.Draw renderer
