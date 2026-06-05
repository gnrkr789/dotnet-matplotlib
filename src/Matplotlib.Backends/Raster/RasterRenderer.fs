namespace Matplotlib.Backends.Raster

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// A pure-managed, cross-platform raster <see cref="IRenderer"/>. Draws paths
/// (with bezier flattening), fills and strokes into a supersampled
/// <see cref="RasterImage"/>; the caller box-downsamples the surface to obtain
/// anti-aliasing. Coordinates are logical display pixels (bottom-left origin);
/// the renderer flips Y and scales by the supersample factor internally.
/// </summary>
/// <remarks>
/// The Agg-equivalent of <c>matplotlib.backends.backend_agg</c>, minus text:
/// glyph rasterization awaits the TrueType font slice, so <c>DrawText</c> is a
/// no-op here (text still measures via the same heuristic as the SVG backend).
/// Dash patterns are not yet honored (strokes render solid).
/// </remarks>
type RasterRenderer(image: RasterImage, sizePx: Size, dpi: float, scale: int) =

    let fscale = float scale
    let logicalHeight = sizePx.Height
    let bezierSteps = 24

    /// <summary>Logical display point → supersampled, Y-flipped device point.</summary>
    let toDevice (p: Point2D) = (p.X * fscale, (logicalHeight - p.Y) * fscale)

    /// <summary>Split a path into subpaths (flattening curves), with a closed flag.</summary>
    let flatten (path: Path) : struct (ResizeArray<float * float> * bool)[] =
        let result = ResizeArray<struct (ResizeArray<float * float> * bool)>()
        let mutable current = ResizeArray<float * float>()
        let mutable closed = false
        let mutable last = (0.0, 0.0)

        let flush () =
            if current.Count > 0 then
                result.Add(struct (current, closed))
                current <- ResizeArray<float * float>()
                closed <- false

        for cmd in path.Commands do
            match cmd with
            | MoveTo p ->
                flush ()
                let pt = toDevice p
                current.Add pt
                last <- pt
            | LineTo p ->
                let pt = toDevice p
                current.Add pt
                last <- pt
            | CurveTo(c1, c2, e) ->
                let x0, y0 = last
                let x1, y1 = toDevice c1
                let x2, y2 = toDevice c2
                let x3, y3 = toDevice e

                for s in 1..bezierSteps do
                    let t = float s / float bezierSteps
                    let mt = 1.0 - t
                    let a = mt * mt * mt
                    let b = 3.0 * mt * mt * t
                    let c = 3.0 * mt * t * t
                    let d = t * t * t
                    current.Add(a * x0 + b * x1 + c * x2 + d * x3, a * y0 + b * y1 + c * y2 + d * y3)

                last <- (x3, y3)
            | ClosePath ->
                closed <- true
                flush ()

        flush ()
        result.ToArray()

    interface IRenderer with

        member _.CanvasSizePx = sizePx

        member _.Dpi = dpi

        member _.DrawPath(gc: GraphicsContext, path: Path, fill: Color option) =
            if not path.Commands.IsEmpty then
                let subpaths = flatten path

                match fill with
                | Some c when not c.IsTransparent ->
                    for struct (pts, _) in subpaths do
                        if pts.Count >= 3 then
                            image.FillPolygon(pts.ToArray(), c)
                | _ -> ()

                let stroke = gc.StrokeColor

                if not stroke.IsTransparent && gc.LineWidth > 0.0 then
                    let w = gc.LineWidth * fscale

                    for struct (pts, isClosed) in subpaths do
                        let arr =
                            if isClosed && pts.Count >= 2 then
                                Array.append (pts.ToArray()) [| pts[0] |]
                            else
                                pts.ToArray()

                        image.StrokePolyline(arr, w, stroke)

        member _.DrawText
            (
                _gc: GraphicsContext,
                _x: float,
                _y: float,
                _text: string,
                _font: FontProperties,
                _angleDegrees: float,
                _hAlign: HAlign,
                _vAlign: VAlign
            ) =
            // Text rasterization awaits the TrueType glyph slice; intentionally a no-op.
            ()

        member _.MeasureText(text: string, font: FontProperties) : Size =
            // Same heuristic as the SVG backend so layout matches across backends.
            let emPx = font.Size * dpi / 72.0

            {
                Width = float text.Length * 0.6 * emPx
                Height = emPx
            }
