namespace Matplotlib.Domain.Rendering

open Matplotlib.Domain.Primitives

/// <summary>A single path-drawing command in display (pixel) coordinates.</summary>
/// <remarks>Ported from the vertex/code model of <c>matplotlib.path.Path</c>.</remarks>
type PathCommand =
    | MoveTo of Point2D
    | LineTo of Point2D
    | CurveTo of control1: Point2D * control2: Point2D * endPoint: Point2D
    | ClosePath

/// <summary>An ordered sequence of <see cref="PathCommand"/>s.</summary>
type Path =
    {
        Commands: PathCommand list
    }

    /// <summary>An empty path.</summary>
    static member Empty = { Commands = [] }

/// <summary>Construction helpers for <see cref="Path"/>.</summary>
[<RequireQualifiedAccess>]
module Path =

    /// <summary>Build an open polyline path through the given points.</summary>
    let polyline (points: Point2D seq) : Path =
        let pts = points |> Seq.toList

        match pts with
        | [] -> Path.Empty
        | head :: tail ->
            {
                Commands = MoveTo head :: List.map LineTo tail
            }

    /// <summary>Build a closed polygon path through the given points.</summary>
    let polygon (points: Point2D seq) : Path =
        match polyline points with
        | p when p.Commands.IsEmpty -> p
        | p ->
            {
                Commands = p.Commands @ [ ClosePath ]
            }

    /// <summary>The anchor vertices of a path (segment ends; curve end points).</summary>
    let vertices (path: Path) : Point2D list =
        path.Commands
        |> List.choose (fun cmd ->
            match cmd with
            | MoveTo p
            | LineTo p -> Some p
            | CurveTo(_, _, e) -> Some e
            | ClosePath -> None)

    /// <summary>Apply a point mapping to every vertex of a path (e.g. a transform).</summary>
    let map (f: Point2D -> Point2D) (path: Path) : Path =
        let mapCmd cmd =
            match cmd with
            | MoveTo p -> MoveTo(f p)
            | LineTo p -> LineTo(f p)
            | CurveTo(c1, c2, e) -> CurveTo(f c1, f c2, f e)
            | ClosePath -> ClosePath

        {
            Commands = path.Commands |> List.map mapCmd
        }

/// <summary>Horizontal text anchoring.</summary>
type HAlign =
    | HLeft
    | HCenter
    | HRight

/// <summary>Vertical text anchoring.</summary>
type VAlign =
    | VTop
    | VCenter
    | VBottom
    | VBaseline

/// <summary>
/// The set of graphics-state parameters a renderer applies to a draw call:
/// stroke, fill, width (pixels), dashing (pixels), alpha and line styling.
/// </summary>
/// <remarks>Ported from <c>matplotlib.backend_bases.GraphicsContextBase</c>.</remarks>
type GraphicsContext =
    {
        StrokeColor: Color
        LineWidth: float
        DashPattern: float[] option
        FillColor: Color option
        Alpha: float
        CapStyle: string
        JoinStyle: string
    }

    /// <summary>A default context: 1px solid black stroke, no fill.</summary>
    static member Default =
        {
            StrokeColor = Color.black
            LineWidth = 1.0
            DashPattern = None
            FillColor = None
            Alpha = 1.0
            CapStyle = "butt"
            JoinStyle = "round"
        }
