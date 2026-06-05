namespace Matplotlib.Domain.Transforms

open System
open Matplotlib.Domain.Primitives

/// <summary>
/// An immutable 2D affine transformation, stored as the homogeneous matrix
/// <c>[[A C E], [B D F], [0 0 1]]</c> so that
/// <c>x' = A·x + C·y + E</c> and <c>y' = B·x + D·y + F</c>.
/// </summary>
/// <remarks>Ported from <c>matplotlib.transforms.Affine2D</c> / <c>Affine2DBase</c>.</remarks>
type Affine2D(a: float, b: float, c: float, d: float, e: float, f: float) =

    /// <summary>Matrix entry A (x scale / cos component).</summary>
    member _.A = a

    member _.B = b

    member _.C = c

    member _.D = d

    member _.E = e

    member _.F = f

    /// <summary>Determinant of the linear (2×2) part.</summary>
    member _.Determinant = a * d - b * c

    /// <summary>
    /// Matrix product <c>this · other</c> (acts as: apply <paramref name="other"/>
    /// first, then this, on column vectors).
    /// </summary>
    member _.MatMul(other: Affine2D) : Affine2D =
        if isNull (box other) then
            nullArg (nameof other)

        Affine2D(
            a * other.A + c * other.B,
            b * other.A + d * other.B,
            a * other.C + c * other.D,
            b * other.C + d * other.D,
            a * other.E + c * other.F + e,
            b * other.E + d * other.F + f
        )

    /// <summary>Apply this transform first, then <paramref name="next"/>.</summary>
    member this.AndThen(next: Affine2D) : Affine2D =
        if isNull (box next) then
            nullArg (nameof next)

        next.MatMul this

    /// <summary>The inverse affine transform (throws if singular).</summary>
    member this.InvertedAffine() : Affine2D =
        let det = this.Determinant

        if abs det < 1e-300 then
            invalidOp "Affine transform is singular and cannot be inverted."

        let invDet = 1.0 / det
        let na = d * invDet
        let nb = -b * invDet
        let nc = -c * invDet
        let nd = a * invDet
        let ne = -(na * e + nc * f)
        let nf = -(nb * e + nd * f)
        Affine2D(na, nb, nc, nd, ne, nf)

    interface ITransform with
        member _.Transform(p: Point2D) =
            {
                X = a * p.X + c * p.Y + e
                Y = b * p.X + d * p.Y + f
            }

        member this.Inverted() = this.InvertedAffine() :> ITransform

    override _.ToString() = $"Affine2D[[{a} {c} {e}] [{b} {d} {f}]]"

/// <summary>Factory functions for common affine transforms.</summary>
[<RequireQualifiedAccess>]
module Affine2D =

    /// <summary>The identity transform.</summary>
    let identity = Affine2D(1.0, 0.0, 0.0, 1.0, 0.0, 0.0)

    /// <summary>A pure translation by <c>(tx, ty)</c>.</summary>
    let translation (tx: float) (ty: float) = Affine2D(1.0, 0.0, 0.0, 1.0, tx, ty)

    /// <summary>A pure scaling by <c>(sx, sy)</c>.</summary>
    let scaling (sx: float) (sy: float) = Affine2D(sx, 0.0, 0.0, sy, 0.0, 0.0)

    /// <summary>A rotation about the origin, counter-clockwise, in degrees.</summary>
    let rotationDegrees (degrees: float) =
        let rad = degrees * Math.PI / 180.0
        let cos = Math.Cos rad
        let sin = Math.Sin rad
        Affine2D(cos, sin, -sin, cos, 0.0, 0.0)
