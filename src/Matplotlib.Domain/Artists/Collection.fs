namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// Abstract base for a collection of primitives sharing a single style, drawn
/// together for efficiency.
/// </summary>
/// <remarks>Ported from <c>matplotlib.collections.Collection</c>.</remarks>
[<AbstractClass>]
type Collection() =
    inherit Artist()

    /// <summary>The data-space bounding box used for autoscaling, if any.</summary>
    abstract member DataBounds: unit -> BBox option

/// <summary>Helpers shared by collections.</summary>
[<RequireQualifiedAccess>]
module internal CollectionBounds =

    let ofPoints (points: Point2D seq) : BBox option =
        let pts = points |> Seq.toArray

        if pts.Length = 0 then
            None
        else
            let xs = pts |> Array.map (fun p -> p.X)
            let ys = pts |> Array.map (fun p -> p.Y)
            Some(BBox.fromExtents (Array.min xs) (Array.min ys) (Array.max xs) (Array.max ys))

/// <summary>A collection of polylines sharing one style.</summary>
/// <remarks>Ported from <c>matplotlib.collections.LineCollection</c>.</remarks>
type LineCollection(segments: Point2D[] list) as this =
    inherit Collection()

    do this.ZOrder <- 2.0

    member val Segments = segments with get, set
    member val Color = Color.black with get, set
    member val LineWidth = 1.5 with get, set

    override this.Draw(renderer: IRenderer) =
        if this.Visible then
            let gc =
                { GraphicsContext.Default with
                    StrokeColor = this.Color
                    LineWidth = this.LineWidth * renderer.Dpi / 72.0
                    CapStyle = "round"
                }

            for seg in this.Segments do
                if seg.Length >= 2 then
                    let pts = seg |> Array.map this.Transform.Transform
                    renderer.DrawPath(gc, Path.polyline pts, None)

    override this.DataBounds() = CollectionBounds.ofPoints (this.Segments |> Seq.collect id)

/// <summary>A collection of filled polygons sharing one style.</summary>
/// <remarks>Ported from <c>matplotlib.collections.PolyCollection</c>.</remarks>
type PolyCollection(polygons: Point2D[] list) as this =
    inherit Collection()

    do this.ZOrder <- 1.0

    member val Polygons = polygons with get, set
    member val FaceColor = Color.fromHex "#1f77b4" with get, set
    member val EdgeColor: Color option = None with get, set
    member val LineWidth = 1.0 with get, set

    override this.Draw(renderer: IRenderer) =
        if this.Visible then
            let stroke =
                match this.EdgeColor with
                | Some c -> c
                | None -> Color.none

            let gc =
                { GraphicsContext.Default with
                    StrokeColor = stroke
                    LineWidth = this.LineWidth * renderer.Dpi / 72.0
                    JoinStyle = "miter"
                }

            for poly in this.Polygons do
                if poly.Length >= 2 then
                    let pts = poly |> Array.map this.Transform.Transform
                    renderer.DrawPath(gc, Path.polygon pts, Some this.FaceColor)

    override this.DataBounds() = CollectionBounds.ofPoints (this.Polygons |> Seq.collect id)
