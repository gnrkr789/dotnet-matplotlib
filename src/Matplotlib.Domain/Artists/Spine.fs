namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Rendering

/// <summary>
/// A line bordering the data area of an Axes (left/right/top/bottom). Its
/// endpoints are expressed in the artist's coordinate space (typically axes
/// fractions via <c>transAxes</c>).
/// </summary>
/// <remarks>Ported from <c>matplotlib.spines.Spine</c> (line subset).</remarks>
type Spine(start: Point2D, finish: Point2D) as this =
    inherit Artist()

    do this.ZOrder <- 2.5

    /// <summary>The start endpoint.</summary>
    member val Start = start with get, set

    /// <summary>The end endpoint.</summary>
    member val End = finish with get, set

    /// <summary>Spine color.</summary>
    member val Color = Color.black with get, set

    /// <summary>Spine line width in points.</summary>
    member val LineWidth = 0.8 with get, set

    override this.Draw(renderer: IRenderer) =
        if this.Visible then
            let a = this.Transform.Transform this.Start
            let b = this.Transform.Transform this.End

            let gc =
                { GraphicsContext.Default with
                    StrokeColor = this.Color
                    LineWidth = this.LineWidth * renderer.Dpi / 72.0
                }

            renderer.DrawPath(gc, Path.polyline [ a; b ], None)
