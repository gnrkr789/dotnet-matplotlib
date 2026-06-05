namespace Matplotlib.Domain.Rendering

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style

/// <summary>
/// The rendering boundary (port) that artists draw onto. Concrete backends
/// (SVG, raster, …) implement this interface in the infrastructure layer.
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.backend_bases.RendererBase</c>. Coordinates passed
/// to the renderer are in display pixels with the origin at the bottom-left
/// (Matplotlib convention); a backend flips the Y axis on output if needed.
/// </remarks>
type IRenderer =

    /// <summary>The size of the drawing surface in pixels.</summary>
    abstract member CanvasSizePx: Size

    /// <summary>Dots per inch of the surface, used to convert points → pixels.</summary>
    abstract member Dpi: float

    /// <summary>Stroke (and optionally fill) a path.</summary>
    abstract member DrawPath: gc: GraphicsContext * path: Path * fill: Color option -> unit

    /// <summary>Draw a single line of text anchored at <c>(x, y)</c>.</summary>
    abstract member DrawText:
        gc: GraphicsContext *
        x: float *
        y: float *
        text: string *
        font: FontProperties *
        angleDegrees: float *
        hAlign: HAlign *
        vAlign: VAlign ->
            unit

    /// <summary>Estimate the rendered size of a text string.</summary>
    abstract member MeasureText: text: string * font: FontProperties -> Size
