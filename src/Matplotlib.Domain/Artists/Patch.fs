namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Rendering

/// <summary>
/// Generates hatch line segments clipped to a polygon outline, for the standard
/// Matplotlib hatch characters (<c>/ \ | - + x</c>).
/// </summary>
/// <remarks>Ported in spirit from <c>matplotlib.hatch</c>.</remarks>
[<RequireQualifiedAccess>]
module Hatching =

    let private angleOf (c: char) : float =
        match c with
        | '/' -> System.Math.PI / 4.0
        | '\\' -> -System.Math.PI / 4.0
        | '|' -> System.Math.PI / 2.0
        | '-' -> 0.0
        | _ -> nan

    /// <summary>Expand compound patterns and map to distinct line angles (radians).</summary>
    let private anglesOf (pattern: string) : float list =
        pattern
        |> Seq.collect (fun c ->
            match c with
            | '+' -> "-|" :> char seq
            | 'x' -> "/\\"
            | other -> Seq.singleton other)
        |> Seq.choose (fun c ->
            let a = angleOf c
            if System.Double.IsNaN a then None else Some a)
        |> Seq.distinct
        |> Seq.toList

    let private rot (a: float) (p: Point2D) : Point2D =
        let c = cos a
        let s = sin a

        {
            X = p.X * c - p.Y * s
            Y = p.X * s + p.Y * c
        }

    /// <summary>Hatch line segments (display coords) filling <paramref name="outline"/>.</summary>
    let segments (outline: Point2D[]) (pattern: string) (spacing: float) : (Point2D * Point2D) list =
        if outline.Length < 3 || spacing <= 0.0 then
            []
        else
            let result = ResizeArray<Point2D * Point2D>()
            let n = outline.Length

            for angle in anglesOf pattern do
                // rotate so this hatch family is horizontal, scan, then rotate back
                let r = outline |> Array.map (rot -angle)
                let ys = r |> Array.map (fun p -> p.Y)
                let mutable yc = ceil (Array.min ys / spacing) * spacing
                let yMax = Array.max ys

                while yc <= yMax do
                    let xs = ResizeArray<float>()

                    for i in 0 .. n - 1 do
                        let a = r[i]
                        let b = r[(i + 1) % n]

                        if (a.Y <= yc && b.Y > yc) || (b.Y <= yc && a.Y > yc) then
                            xs.Add(a.X + (yc - a.Y) / (b.Y - a.Y) * (b.X - a.X))

                    xs.Sort()
                    let mutable k = 0

                    while k + 1 < xs.Count do
                        result.Add(rot angle { X = xs[k]; Y = yc }, rot angle { X = xs[k + 1]; Y = yc })
                        k <- k + 2

                    yc <- yc + spacing

            List.ofSeq result

/// <summary>
/// Abstract base for 2D filled shapes with a face color and an optional edge.
/// The shape is expressed in the artist's (data) coordinate space and mapped to
/// display pixels by its <c>Transform</c> at draw time.
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.patches.Patch</c> (default <c>zorder = 1</c>, no
/// edge drawn unless an edge color is set, matching <c>patch.force_edgecolor</c>
/// being False).
/// </remarks>
[<AbstractClass>]
type Patch() as this =
    inherit Artist()

    do this.ZOrder <- 1.0

    /// <summary>Fill color (defaults to the first cycle color, C0).</summary>
    member val FaceColor: Color = Color.fromHex "#1f77b4" with get, set

    /// <summary>Edge color; <c>None</c> draws no edge (Matplotlib's default).</summary>
    member val EdgeColor: Color option = None with get, set

    /// <summary>Edge width in points.</summary>
    member val LineWidth = 1.0 with get, set

    /// <summary>Whether the shape is filled.</summary>
    member val Fill = true with get, set

    /// <summary>Hatch pattern (<c>/ \ | - + x</c>), or <c>None</c> for no hatch.</summary>
    member val Hatch: string option = None with get, set

    /// <summary>Legend label.</summary>
    member val Label = "" with get, set

    /// <summary>The shape outline in the artist's (data) coordinate space.</summary>
    abstract member BuildPath: unit -> Path

    /// <summary>The data-space bounding box used for autoscaling, if any.</summary>
    abstract member DataBounds: unit -> BBox option

    override this.Draw(renderer: IRenderer) =
        if this.Visible then
            let path = Path.map this.Transform.Transform (this.BuildPath())

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

            let fill = if this.Fill then Some this.FaceColor else None
            renderer.DrawPath(gc, path, fill)

            match this.Hatch with
            | Some pattern when pattern <> "" ->
                let outline = Path.vertices path |> List.toArray

                let hatchColor =
                    match this.EdgeColor with
                    | Some c -> c
                    | None -> Color.black

                let gcHatch =
                    { GraphicsContext.Default with
                        StrokeColor = hatchColor
                        LineWidth = 0.5 * renderer.Dpi / 72.0
                    }

                let spacing = 6.0 * renderer.Dpi / 72.0

                for (p0, p1) in Hatching.segments outline pattern spacing do
                    renderer.DrawPath(gcHatch, Path.polyline [ p0; p1 ], None)
            | _ -> ()

/// <summary>An axis-aligned rectangle anchored at its lower-left corner.</summary>
/// <remarks>Ported from <c>matplotlib.patches.Rectangle</c>.</remarks>
type Rectangle(x: float, y: float, width: float, height: float) =
    inherit Patch()

    member val X = x with get, set
    member val Y = y with get, set
    member val Width = width with get, set
    member val Height = height with get, set

    override this.BuildPath() =
        Path.polygon
            [
                { X = this.X; Y = this.Y }
                { X = this.X + this.Width; Y = this.Y }
                {
                    X = this.X + this.Width
                    Y = this.Y + this.Height
                }
                { X = this.X; Y = this.Y + this.Height }
            ]

    override this.DataBounds() = Some(BBox.fromBounds this.X this.Y this.Width this.Height)

/// <summary>A general polygon through a sequence of vertices.</summary>
/// <remarks>Ported from <c>matplotlib.patches.Polygon</c>.</remarks>
type Polygon(points: Point2D[], ?closed: bool) =
    inherit Patch()

    let isClosed = defaultArg closed true

    member val Points = points with get, set

    member _.Closed = isClosed

    override this.BuildPath() =
        if isClosed then
            Path.polygon this.Points
        else
            Path.polyline this.Points

    override this.DataBounds() =
        if this.Points.Length = 0 then
            None
        else
            let xs = this.Points |> Array.map (fun p -> p.X)
            let ys = this.Points |> Array.map (fun p -> p.Y)
            Some(BBox.fromExtents (Array.min xs) (Array.min ys) (Array.max xs) (Array.max ys))

/// <summary>A patch defined by an arbitrary path (data coordinates).</summary>
/// <remarks>Ported from <c>matplotlib.patches.PathPatch</c>.</remarks>
type PathPatch(pathData: Path) =
    inherit Patch()

    member val PathData = pathData with get, set

    override this.BuildPath() = this.PathData

    override this.DataBounds() =
        match Path.vertices this.PathData with
        | [] -> None
        | verts ->
            let xs = verts |> List.map (fun pt -> pt.X)
            let ys = verts |> List.map (fun pt -> pt.Y)
            Some(BBox.fromExtents (List.min xs) (List.min ys) (List.max xs) (List.max ys))

/// <summary>A circle of a given radius about a center (data coordinates).</summary>
/// <remarks>Ported from <c>matplotlib.patches.Circle</c>.</remarks>
type Circle(center: Point2D, radius: float) =
    inherit Patch()

    member val Center = center with get, set
    member val Radius = radius with get, set

    override this.BuildPath() = MarkerPaths.circle this.Center this.Radius

    override this.DataBounds() =
        let c = this.Center
        let r = this.Radius
        Some(BBox.fromExtents (c.X - r) (c.Y - r) (c.X + r) (c.Y + r))
