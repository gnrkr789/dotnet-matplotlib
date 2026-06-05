namespace Matplotlib.Domain.Scales

open Matplotlib.Domain.Transforms
open Matplotlib.Domain.Ticking

/// <summary>
/// Maps data values to a transformed axis space and provides the default tick
/// locator/formatter for that space.
/// </summary>
/// <remarks>Ported from the <c>ScaleBase</c> hierarchy in <c>matplotlib.scale</c>.</remarks>
type IScale =

    /// <summary>The registered scale name (e.g. <c>"linear"</c>).</summary>
    abstract member Name: string

    /// <summary>The value-space transform (identity for a linear scale).</summary>
    abstract member Transform: ITransform

    /// <summary>Create the default tick locator for this scale.</summary>
    abstract member CreateLocator: nbins: int -> ITickLocator

    /// <summary>Create the default tick formatter for this scale.</summary>
    abstract member CreateFormatter: unit -> ITickFormatter

/// <summary>The standard linear scale (identity transform).</summary>
/// <remarks>Ported from <c>matplotlib.scale.LinearScale</c>.</remarks>
type LinearScale() =

    interface IScale with
        member _.Name = "linear"
        member _.Transform = IdentityTransform.Instance :> ITransform
        member _.CreateLocator(nbins: int) = TickLocator.linearAuto nbins
        member _.CreateFormatter() = ScalarFormatter() :> ITickFormatter
