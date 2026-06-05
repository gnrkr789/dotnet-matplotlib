namespace Matplotlib.Domain.Transforms

open Matplotlib.Domain.Primitives

/// <summary>The transform that maps every point to itself.</summary>
/// <remarks>Ported from <c>matplotlib.transforms.IdentityTransform</c>.</remarks>
type IdentityTransform private () =

    static let instance = IdentityTransform()

    /// <summary>The shared identity instance.</summary>
    static member Instance = instance

    interface ITransform with
        member _.Transform(point: Point2D) = point
        member this.Inverted() = this :> ITransform

/// <summary>
/// The composition of two transforms: applies <c>First</c> then <c>Second</c>.
/// </summary>
/// <remarks>Ported from <c>matplotlib.transforms.CompositeGenericTransform</c>.</remarks>
type CompositeTransform(first: ITransform, second: ITransform) =

    do
        if isNull (box first) then
            nullArg (nameof first)

        if isNull (box second) then
            nullArg (nameof second)

    member _.First = first

    member _.Second = second

    interface ITransform with
        member _.Transform(point: Point2D) = second.Transform(first.Transform point)

        member _.Inverted() = CompositeTransform(second.Inverted(), first.Inverted()) :> ITransform

/// <summary>
/// A separable transform mapping the x coordinate through one transform and the
/// y coordinate through another. Used for "blended" coordinate systems such as
/// (data-x, axes-y).
/// </summary>
/// <remarks>Ported from <c>matplotlib.transforms.BlendedGenericTransform</c>.</remarks>
type BlendedTransform(xTransform: ITransform, yTransform: ITransform) =

    do
        if isNull (box xTransform) then
            nullArg (nameof xTransform)

        if isNull (box yTransform) then
            nullArg (nameof yTransform)

    member _.XTransform = xTransform

    member _.YTransform = yTransform

    interface ITransform with
        member _.Transform(point: Point2D) =
            {
                X = (xTransform.Transform point).X
                Y = (yTransform.Transform point).Y
            }

        member _.Inverted() = BlendedTransform(xTransform.Inverted(), yTransform.Inverted()) :> ITransform

/// <summary>
/// Linearly maps points contained in the <c>From</c> bounding box onto the
/// <c>To</c> bounding box. This is the workhorse behind the data→axes
/// (<c>transLimits</c>) and axes→figure (<c>transAxes</c>) steps.
/// </summary>
/// <remarks>Ported from <c>matplotlib.transforms.BboxTransform</c>.</remarks>
type BBoxTransform(fromBox: BBox, toBox: BBox) =

    let scaleX =
        if fromBox.Width = 0.0 then
            0.0
        else
            toBox.Width / fromBox.Width

    let scaleY =
        if fromBox.Height = 0.0 then
            0.0
        else
            toBox.Height / fromBox.Height

    member _.From = fromBox

    member _.To = toBox

    /// <summary>The equivalent axis-aligned scale + translate affine.</summary>
    member _.ToAffine() : Affine2D =
        Affine2D(scaleX, 0.0, 0.0, scaleY, toBox.X0 - fromBox.X0 * scaleX, toBox.Y0 - fromBox.Y0 * scaleY)

    interface ITransform with
        member _.Transform(point: Point2D) =
            {
                X = toBox.X0 + (point.X - fromBox.X0) * scaleX
                Y = toBox.Y0 + (point.Y - fromBox.Y0) * scaleY
            }

        member _.Inverted() = BBoxTransform(toBox, fromBox) :> ITransform

/// <summary>
/// A separable transform applying an arbitrary scalar function to x and to y
/// (e.g. <c>log10</c> for a log scale). Used as the <c>transScale</c> step.
/// </summary>
/// <remarks>Ported from the per-axis scale transforms in <c>matplotlib.scale</c>.</remarks>
type FunctionalTransform
    (forwardX: float -> float, forwardY: float -> float, inverseX: float -> float, inverseY: float -> float) =

    interface ITransform with
        member _.Transform(point: Point2D) =
            {
                X = forwardX point.X
                Y = forwardY point.Y
            }

        member _.Inverted() = FunctionalTransform(inverseX, inverseY, forwardX, forwardY) :> ITransform

/// <summary>Composition helpers for transforms.</summary>
[<RequireQualifiedAccess>]
module Transforms =

    /// <summary>
    /// Compose two transforms: apply <paramref name="first"/>, then
    /// <paramref name="second"/>. Mirrors Matplotlib's <c>first + second</c>.
    /// </summary>
    let compose (first: ITransform) (second: ITransform) : ITransform = CompositeTransform(first, second) :> ITransform

    /// <summary>Blend an x transform and a y transform into a separable transform.</summary>
    let blend (xTransform: ITransform) (yTransform: ITransform) : ITransform =
        BlendedTransform(xTransform, yTransform) :> ITransform
