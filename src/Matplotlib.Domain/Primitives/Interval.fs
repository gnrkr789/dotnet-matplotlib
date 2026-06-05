namespace Matplotlib.Domain.Primitives

/// <summary>
/// A 1D interval <c>[Lower, Upper]</c> used for axis data/view limits. The
/// interval may be inverted (<c>Lower &gt; Upper</c>) to represent a reversed
/// axis, mirroring Matplotlib's view-interval semantics.
/// </summary>
[<Struct>]
type Interval =
    {
        Lower: float
        Upper: float
    }

    /// <summary>Smaller endpoint regardless of orientation.</summary>
    member this.Min = min this.Lower this.Upper

    /// <summary>Larger endpoint regardless of orientation.</summary>
    member this.Max = max this.Lower this.Upper

    /// <summary>Signed span <c>Upper - Lower</c> (negative when inverted).</summary>
    member this.Span = this.Upper - this.Lower

    /// <summary>True when <c>Lower &gt; Upper</c>.</summary>
    member this.IsInverted = this.Lower > this.Upper

    /// <summary>True when the interval has zero or non-finite extent.</summary>
    member this.IsDegenerate =
        let s = this.Span
        not (abs s > 0.0) || System.Double.IsNaN s || System.Double.IsInfinity s

    /// <summary>The smallest interval containing both this and <paramref name="other"/>.</summary>
    member this.UnionExtent(other: Interval) =
        {
            Lower = min this.Min other.Min
            Upper = max this.Max other.Max
        }

    override this.ToString() = $"[{this.Lower}, {this.Upper}]"

/// <summary>Helpers for constructing and adjusting <see cref="Interval"/> values.</summary>
[<RequireQualifiedAccess>]
module Interval =

    /// <summary>Create an interval from its lower and upper endpoints.</summary>
    let create (lower: float) (upper: float) : Interval = { Lower = lower; Upper = upper }

    /// <summary>Expand a degenerate interval to a small finite one around its value.</summary>
    let expanded (absDelta: float) (interval: Interval) : Interval =
        if not interval.IsDegenerate then
            interval
        else
            let v =
                if System.Double.IsFinite interval.Lower then
                    interval.Lower
                else
                    0.0

            {
                Lower = v - absDelta
                Upper = v + absDelta
            }
