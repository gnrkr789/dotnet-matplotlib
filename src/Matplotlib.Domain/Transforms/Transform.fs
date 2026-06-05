namespace Matplotlib.Domain.Transforms

open Matplotlib.Domain.Primitives

/// <summary>
/// A coordinate transformation mapping points from one space to another
/// (e.g. data → axes → figure → display pixels).
/// </summary>
/// <remarks>Ported from the <c>Transform</c> hierarchy in <c>matplotlib.transforms</c>.</remarks>
type ITransform =

    /// <summary>Map a single point into the target space.</summary>
    abstract member Transform: point: Point2D -> Point2D

    /// <summary>Return the inverse transformation.</summary>
    abstract member Inverted: unit -> ITransform

/// <summary>Convenience helpers over <see cref="ITransform"/>.</summary>
[<RequireQualifiedAccess>]
module Transform =

    /// <summary>Transform every point, returning a new array.</summary>
    let transformAll (transform: ITransform) (points: Point2D seq) : Point2D[] =
        points |> Seq.map transform.Transform |> Seq.toArray
