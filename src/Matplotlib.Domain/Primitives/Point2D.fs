namespace Matplotlib.Domain.Primitives

/// <summary>
/// An immutable 2D point / vector in some coordinate system (data, axes,
/// figure, or display pixels depending on context).
/// </summary>
/// <remarks>
/// Matplotlib represents points as length-2 NumPy arrays; here we use an
/// allocation-free value type.
/// </remarks>
[<Struct>]
type Point2D =
    {
        X: float
        Y: float
    }

    /// <summary>Euclidean length of this vector.</summary>
    member this.Length = sqrt (this.X * this.X + this.Y * this.Y)

    /// <summary>Euclidean distance to <paramref name="other"/>.</summary>
    member this.DistanceTo(other: Point2D) =
        let dx = this.X - other.X
        let dy = this.Y - other.Y
        sqrt (dx * dx + dy * dy)

    /// <summary>The point (0, 0).</summary>
    static member Origin = { X = 0.0; Y = 0.0 }

    static member (+)(a: Point2D, b: Point2D) = { X = a.X + b.X; Y = a.Y + b.Y }

    static member (-)(a: Point2D, b: Point2D) = { X = a.X - b.X; Y = a.Y - b.Y }

    static member (*)(a: Point2D, s: float) = { X = a.X * s; Y = a.Y * s }

    static member (*)(s: float, a: Point2D) = { X = a.X * s; Y = a.Y * s }

    override this.ToString() = $"({this.X}, {this.Y})"

/// <summary>Helpers for constructing <see cref="Point2D"/> values.</summary>
[<RequireQualifiedAccess>]
module Point2D =

    /// <summary>Create a point from x and y components.</summary>
    let create (x: float) (y: float) : Point2D = { X = x; Y = y }
