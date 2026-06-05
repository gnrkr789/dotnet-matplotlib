namespace Matplotlib.Domain.Primitives

/// <summary>
/// A width/height pair. Used for figure size (inches), canvas size (pixels),
/// and measured text extents.
/// </summary>
[<Struct>]
type Size =
    {
        Width: float
        Height: float
    }

    /// <summary>An empty size (0, 0).</summary>
    static member Empty = { Width = 0.0; Height = 0.0 }

    /// <summary>Scale both dimensions by <paramref name="factor"/>.</summary>
    member this.Scale(factor: float) =
        {
            Width = this.Width * factor
            Height = this.Height * factor
        }

    override this.ToString() = $"{this.Width} x {this.Height}"

/// <summary>Helpers for constructing <see cref="Size"/> values.</summary>
[<RequireQualifiedAccess>]
module Size =

    /// <summary>Create a size from width and height.</summary>
    let create (width: float) (height: float) : Size = { Width = width; Height = height }
