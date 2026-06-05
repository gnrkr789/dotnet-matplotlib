namespace Matplotlib.Domain.Artists

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

    /// <summary>Marker edge width in points.</summary>
    member val MarkerEdgeWidth = 1.0 with get, set

    /// <summary>Legend label.</summary>
    member val Label = "" with get, set

    member private this.DisplayPoints() : Point2D[] =
        Array.map2 (fun x y -> this.Transform.Transform { X = x; Y = y }) this.XData this.YData

    member private this.DrawMarkers(renderer: IRenderer, points: Point2D[]) =
        let dpiScale = renderer.Dpi / 72.0
        let r = this.MarkerSize / 2.0 * dpiScale
        let face = defaultArg this.MarkerFaceColor this.Color
        let edge = defaultArg this.MarkerEdgeColor this.Color
        let edgeWidthPx = this.MarkerEdgeWidth * dpiScale

        let strokeGc =
            { GraphicsContext.Default with
                StrokeColor = edge
                LineWidth = edgeWidthPx
            }

        for pt in points do
            match this.Marker with
            | NoMarker -> ()
            | Circle -> renderer.DrawPath(strokeGc, MarkerPaths.circle pt r, Some face)
            | Point -> renderer.DrawPath(strokeGc, MarkerPaths.circle pt (r / 2.0), Some face)
            | Square -> renderer.DrawPath(strokeGc, MarkerPaths.square pt r, Some face)
            | Diamond -> renderer.DrawPath(strokeGc, MarkerPaths.diamond pt r, Some face)
            | TriangleUp -> renderer.DrawPath(strokeGc, MarkerPaths.triangleUp pt r, Some face)
            | Plus -> renderer.DrawPath(strokeGc, MarkerPaths.plus pt r, None)
            | Cross -> renderer.DrawPath(strokeGc, MarkerPaths.cross pt r, None)

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
