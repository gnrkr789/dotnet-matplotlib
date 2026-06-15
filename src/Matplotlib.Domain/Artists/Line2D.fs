namespace Matplotlib.Domain.Artists

open System
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>Builds marker shape paths centered at a display-space point.</summary>
[<RequireQualifiedAccess>]
module internal MarkerPaths =

    [<Literal>]
    let private Kappa = 0.5522847498307936

    let private p x y : Point2D = { X = x; Y = y }

    /// <summary>A circle approximated by four cubic Bézier arcs.</summary>
    let circle (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        let k = Kappa * r

        {
            Commands =
                [
                    MoveTo(p (cx + r) cy)
                    CurveTo(p (cx + r) (cy + k), p (cx + k) (cy + r), p cx (cy + r))
                    CurveTo(p (cx - k) (cy + r), p (cx - r) (cy + k), p (cx - r) cy)
                    CurveTo(p (cx - r) (cy - k), p (cx - k) (cy - r), p cx (cy - r))
                    CurveTo(p (cx + k) (cy - r), p (cx + r) (cy - k), p (cx + r) cy)
                    ClosePath
                ]
        }

    let square (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y

        Path.polygon
            [
                p (cx - r) (cy - r)
                p (cx + r) (cy - r)
                p (cx + r) (cy + r)
                p (cx - r) (cy + r)
            ]

    let diamond (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        Path.polygon [ p cx (cy + r); p (cx + r) cy; p cx (cy - r); p (cx - r) cy ]

    let triangleUp (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        Path.polygon [ p cx (cy + r); p (cx - r) (cy - r); p (cx + r) (cy - r) ]

    /// <summary>The two stroked segments of a plus marker.</summary>
    let plus (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y

        {
            Commands =
                [
                    MoveTo(p (cx - r) cy)
                    LineTo(p (cx + r) cy)
                    MoveTo(p cx (cy - r))
                    LineTo(p cx (cy + r))
                ]
        }

    /// <summary>The two stroked diagonals of an x marker.</summary>
    let cross (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y

        {
            Commands =
                [
                    MoveTo(p (cx - r) (cy - r))
                    LineTo(p (cx + r) (cy + r))
                    MoveTo(p (cx - r) (cy + r))
                    LineTo(p (cx + r) (cy - r))
                ]
        }

    /// <summary>A regular n-gon inscribed in radius <paramref name="r"/>.</summary>
    let private regular (center: Point2D) (r: float) (n: int) (startDeg: float) : Path =
        [
            for k in 0 .. n - 1 ->
                let a = (startDeg + 360.0 * float k / float n) * Math.PI / 180.0
                p (center.X + r * cos a) (center.Y + r * sin a)
        ]
        |> Path.polygon

    /// <summary>An n-pointed star (matplotlib's inner radius ratio 0.381966).</summary>
    let private starShape (center: Point2D) (r: float) (n: int) (startDeg: float) : Path =
        let inner = r * 0.381966

        [
            for k in 0 .. (2 * n - 1) ->
                let rr = if k % 2 = 0 then r else inner
                let a = (startDeg + 360.0 * float k / float (2 * n)) * Math.PI / 180.0
                p (center.X + rr * cos a) (center.Y + rr * sin a)
        ]
        |> Path.polygon

    let pentagon (center: Point2D) (r: float) : Path = regular center r 5 90.0

    let hexagon (center: Point2D) (r: float) : Path = regular center r 6 90.0

    let star (center: Point2D) (r: float) : Path = starShape center r 5 90.0

    let triangleDown (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        Path.polygon [ p cx (cy - r); p (cx - r) (cy + r); p (cx + r) (cy + r) ]

    let triangleLeft (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        Path.polygon [ p (cx - r) cy; p (cx + r) (cy + r); p (cx + r) (cy - r) ]

    let triangleRight (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        Path.polygon [ p (cx + r) cy; p (cx - r) (cy - r); p (cx - r) (cy + r) ]

    let thinDiamond (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y
        Path.polygon [ p cx (cy + r); p (cx + r * 0.6) cy; p cx (cy - r); p (cx - r * 0.6) cy ]

    let vline (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y

        {
            Commands = [ MoveTo(p cx (cy - r)); LineTo(p cx (cy + r)) ]
        }

    let hline (center: Point2D) (r: float) : Path =
        let cx, cy = center.X, center.Y

        {
            Commands = [ MoveTo(p (cx - r) cy); LineTo(p (cx + r) cy) ]
        }

/// <summary>
/// A 2D line, optionally with markers — the artist created by <c>plot</c>.
/// </summary>
/// <remarks>Ported from <c>matplotlib.lines.Line2D</c>.</remarks>
type Line2D(xData: float[], yData: float[]) as this =
    inherit Artist()

    do
        if xData.Length <> yData.Length then
            invalidArg (nameof yData) "x and y data must have the same length."

        this.ZOrder <- 2.0

    /// <summary>X data values.</summary>
    member val XData = xData with get, set

    /// <summary>Y data values.</summary>
    member val YData = yData with get, set

    /// <summary>Line color.</summary>
    member val Color = Color.black with get, set

    /// <summary>Line width in points.</summary>
    member val LineWidth = 1.5 with get, set

    /// <summary>Line style.</summary>
    member val LineStyle = Solid with get, set

    /// <summary>Marker shape.</summary>
    member val Marker = NoMarker with get, set

    /// <summary>Marker size in points.</summary>
    member val MarkerSize = 6.0 with get, set

    /// <summary>Marker fill color (defaults to the line color).</summary>
    member val MarkerFaceColor: Color option = None with get, set

    /// <summary>Marker edge color (defaults to the line color).</summary>
    member val MarkerEdgeColor: Color option = None with get, set

    /// <summary>
    /// Per-point marker colors (Matplotlib's <c>scatter</c> with a mapped <c>c</c>
    /// array); when set, each marker uses its own color for both face and edge.
    /// </summary>
    member val MarkerColors: Color[] option = None with get, set

    /// <summary>
    /// Per-point marker diameters in points (Matplotlib's <c>scatter</c> with an
    /// array <c>s</c>); when set, each marker is sized individually.
    /// </summary>
    member val MarkerSizes: float[] option = None with get, set

    /// <summary>The colormap + normalization that produced this artist's colors (consumed by <c>colorbar</c>).</summary>
    member val ScalarMappable: (Colormap * Normalize) option = None with get, set

    /// <summary>Marker edge width in points.</summary>
    member val MarkerEdgeWidth = 1.0 with get, set

    /// <summary>Legend label.</summary>
    member val Label = "" with get, set

    member private this.DisplayPoints() : Point2D[] =
        Array.map2 (fun x y -> this.Transform.Transform { X = x; Y = y }) this.XData this.YData

    member private this.DrawMarkers(renderer: IRenderer, points: Point2D[]) =
        let dpiScale = renderer.Dpi / 72.0
        let baseR = this.MarkerSize / 2.0 * dpiScale
        let baseFace = defaultArg this.MarkerFaceColor this.Color
        let baseEdge = defaultArg this.MarkerEdgeColor this.Color
        let edgeWidthPx = this.MarkerEdgeWidth * dpiScale

        let drawAt (i: int) (pt: Point2D) =
            // Per-point sizes (scatter with an array s) override the scalar radius.
            let r =
                match this.MarkerSizes with
                | Some ss when ss.Length > 0 -> ss[min i (ss.Length - 1)] / 2.0 * dpiScale
                | _ -> baseR
            // Per-point colors (scatter c+cmap) override face/edge; matplotlib's
            // default for mapped scatters is edgecolors='face'.
            let face, edge =
                match this.MarkerColors with
                | Some cs when cs.Length > 0 ->
                    let c = cs[min i (cs.Length - 1)]
                    c, c
                | _ -> baseFace, baseEdge

            let gc =
                { GraphicsContext.Default with
                    StrokeColor = edge
                    LineWidth = edgeWidthPx
                }

            let fill (path: Path) = renderer.DrawPath(gc, path, Some face)
            let stroke (path: Path) = renderer.DrawPath(gc, path, None)

            match this.Marker with
            | NoMarker -> ()
            | Circle -> fill (MarkerPaths.circle pt r)
            | Point -> fill (MarkerPaths.circle pt (r / 2.0))
            | Square -> fill (MarkerPaths.square pt r)
            | Diamond -> fill (MarkerPaths.diamond pt r)
            | ThinDiamond -> fill (MarkerPaths.thinDiamond pt r)
            | TriangleUp -> fill (MarkerPaths.triangleUp pt r)
            | TriangleDown -> fill (MarkerPaths.triangleDown pt r)
            | TriangleLeft -> fill (MarkerPaths.triangleLeft pt r)
            | TriangleRight -> fill (MarkerPaths.triangleRight pt r)
            | Pentagon -> fill (MarkerPaths.pentagon pt r)
            | Hexagon -> fill (MarkerPaths.hexagon pt r)
            | Star -> fill (MarkerPaths.star pt r)
            | Plus -> stroke (MarkerPaths.plus pt r)
            | Cross -> stroke (MarkerPaths.cross pt r)
            | VLine -> stroke (MarkerPaths.vline pt r)
            | HLine -> stroke (MarkerPaths.hline pt r)

        points |> Array.iteri drawAt

    override this.Draw(renderer: IRenderer) =
        if this.Visible && this.XData.Length > 0 then
            let points = this.DisplayPoints()
            let dpiScale = renderer.Dpi / 72.0

            if this.LineStyle <> NoLine && points.Length >= 2 then
                let lwPx = this.LineWidth * dpiScale

                let dash =
                    this.LineStyle.DashPattern
                    |> Option.map (fun pattern ->
                        pattern |> List.map (fun seg -> seg * this.LineWidth * dpiScale) |> List.toArray)

                let gc =
                    { GraphicsContext.Default with
                        StrokeColor = this.Color
                        LineWidth = lwPx
                        DashPattern = dash
                        CapStyle = "round"
                    }

                renderer.DrawPath(gc, Path.polyline points, None)

            if this.Marker <> NoMarker then
                this.DrawMarkers(renderer, points)
