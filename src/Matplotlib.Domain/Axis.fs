namespace Matplotlib.Domain

open Matplotlib.Domain.Ticking
open Matplotlib.Domain.Scales

/// <summary>Which of the two Cartesian axes an <see cref="Axis"/> represents.</summary>
type AxisOrientation =
    | XAxis
    | YAxis

/// <summary>
/// One of the X or Y axes of an <see cref="Axes"/>: owns the scale, axis label
/// and grid flag. Tick locating/formatting is driven by the owning Axes using
/// the axis scale.
/// </summary>
/// <remarks>Ported from <c>matplotlib.axis.Axis</c> / <c>XAxis</c> / <c>YAxis</c>.</remarks>
type Axis(orientation: AxisOrientation) =

    /// <summary>Whether this is the X or Y axis.</summary>
    member _.Orientation = orientation

    /// <summary>The scale (linear by default).</summary>
    member val Scale: IScale = LinearScale() :> IScale with get, set

    /// <summary>The axis label text.</summary>
    member val Label = "" with get, set

    /// <summary>Whether to draw grid lines for this axis.</summary>
    member val ShowGrid = false with get, set

    /// <summary>Optional tick locator override (falls back to the scale's locator).</summary>
    member val MajorLocator: ITickLocator option = None with get, set

    /// <summary>Optional tick formatter override (falls back to the scale's formatter).</summary>
    member val MajorFormatter: ITickFormatter option = None with get, set
