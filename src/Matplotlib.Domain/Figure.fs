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
    let mutable subplotGrid: Axes[,] option = None

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

        let grid =
            Array2D.init nrows ncols (fun i j ->
                let l = left + float j * (cellW + sepW)
                let t = top - float i * (cellH + sepH)
                let ax = Axes(rc)
                ax.Position <- BBox.fromExtents l (t - cellH) (l + cellW) t
                axesList.Add ax
                ax)

        subplotGrid <- Some grid
        grid

    /// <summary>
    /// Adjust the outer margins so axis labels, tick labels and titles are not
    /// clipped, repositioning every Axes within the new envelope.
    /// </summary>
    /// <remarks>
    /// Ported (approximated) from <c>matplotlib.figure.Figure.tight_layout</c>:
    /// margins are estimated from font sizes and the presence of labels/title,
    /// then each Axes is remapped linearly from the default subplot envelope.
    /// </remarks>
    member this.TightLayout(?pad: float) =
        let figPx = this.PixelSize
        let pt2px = this.Dpi / 72.0
        let padPx = defaultArg pad 1.08 * rc.FontSize * pt2px * 0.5
        let anyTitle = axesList |> Seq.exists (fun a -> a.Title <> "")
        let anyXLabel = axesList |> Seq.exists (fun a -> a.XAxis.Label <> "")
        let anyYLabel = axesList |> Seq.exists (fun a -> a.YAxis.Label <> "")
        let tickRoom = rc.TickMajorSize * pt2px + rc.TickPad * pt2px
        let labelH = rc.AxesLabelSize * pt2px
        let xlabelRoom = if anyXLabel then rc.AxesLabelPad * pt2px + labelH else 0.0
        let ylabelRoom = if anyYLabel then rc.AxesLabelPad * pt2px + labelH else 0.0

        let titleRoom =
            if anyTitle then
                rc.AxesTitlePad * pt2px + rc.AxesTitleSize * pt2px
            else
                0.0

        let bottomPx = tickRoom + rc.TickLabelSize * pt2px + xlabelRoom + padPx
        let leftPx = tickRoom + 4.0 * 0.6 * rc.TickLabelSize * pt2px + ylabelRoom + padPx

        let newL = leftPx / figPx.Width
        let newR = 1.0 - padPx / figPx.Width
        let newB = bottomPx / figPx.Height
        let newT = 1.0 - (titleRoom + padPx) / figPx.Height

        let remap v oldA oldB newA newB =
            if oldB = oldA then
                newA
            else
                newA + (v - oldA) / (oldB - oldA) * (newB - newA)

        for ax in axesList do
            let pos = ax.Position

            ax.Position <-
                BBox.fromExtents
                    (remap pos.X0 rc.SubplotLeft rc.SubplotRight newL newR)
                    (remap pos.Y0 rc.SubplotBottom rc.SubplotTop newB newT)
                    (remap pos.X1 rc.SubplotLeft rc.SubplotRight newL newR)
                    (remap pos.Y1 rc.SubplotBottom rc.SubplotTop newB newT)

    /// <summary>
    /// Lay out subplots so each one's decorations (labels, ticks, title) fit
    /// inside its grid cell and adjacent subplots do not overlap.
    /// </summary>
    /// <remarks>
    /// Ported (approximated) from <c>matplotlib</c>'s <c>constrained_layout</c>:
    /// unlike <see cref="TightLayout"/> it reserves space per-subplot inside each
    /// grid cell rather than remapping a single outer envelope.
    /// </remarks>
    member this.ConstrainedLayout(?pad: float, ?wPad: float, ?hPad: float) =
        let figPx = this.PixelSize
        let pt2px = this.Dpi / 72.0
        let outer = defaultArg pad 1.08 * rc.FontSize * pt2px * 0.5
        let wp = defaultArg wPad 1.0 * rc.FontSize * pt2px * 0.5
        let hp = defaultArg hPad 1.0 * rc.FontSize * pt2px * 0.5

        let deco (ax: Axes) =
            let tickRoom = rc.TickMajorSize * pt2px + rc.TickPad * pt2px
            let labelH = rc.AxesLabelSize * pt2px

            let l =
                tickRoom
                + 4.0 * 0.6 * rc.TickLabelSize * pt2px
                + (if ax.YAxis.Label <> "" then
                       rc.AxesLabelPad * pt2px + labelH
                   else
                       0.0)

            let b =
                tickRoom
                + rc.TickLabelSize * pt2px
                + (if ax.XAxis.Label <> "" then
                       rc.AxesLabelPad * pt2px + labelH
                   else
                       0.0)

            let t =
                if ax.Title <> "" then
                    rc.AxesTitlePad * pt2px + rc.AxesTitleSize * pt2px
                else
                    0.0

            l, b, t

        let place (ax: Axes) (cellL: float) (cellB: float) (cellR: float) (cellT: float) =
            let l, b, t = deco ax

            ax.Position <-
                BBox.fromExtents (cellL + l / figPx.Width) (cellB + b / figPx.Height) cellR (cellT - t / figPx.Height)

        let gl = outer / figPx.Width
        let gr = 1.0 - outer / figPx.Width
        let gb = outer / figPx.Height
        let gt = 1.0 - outer / figPx.Height

        match subplotGrid with
        | Some grid ->
            let nrows = Array2D.length1 grid
            let ncols = Array2D.length2 grid
            let wpf = wp / figPx.Width
            let hpf = hp / figPx.Height
            let cellW = (gr - gl - wpf * float (ncols - 1)) / float ncols
            let cellH = (gt - gb - hpf * float (nrows - 1)) / float nrows

            for i in 0 .. nrows - 1 do
                for j in 0 .. ncols - 1 do
                    let cellL = gl + float j * (cellW + wpf)
                    let cellT = gt - float i * (cellH + hpf)
                    place grid[i, j] cellL (cellT - cellH) (cellL + cellW) cellT
        | None ->
            if axesList.Count = 1 then
                place axesList[0] gl gb gr gt

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
