namespace Matplotlib.Domain.Ticking

open System
open System.Globalization

/// <summary>
/// Turns tick values into display strings.
/// </summary>
/// <remarks>Ported from the <c>Formatter</c> hierarchy in <c>matplotlib.ticker</c>.</remarks>
type ITickFormatter =

    /// <summary>
    /// Format a full set of tick locations. The whole set is provided because
    /// the chosen precision depends on the spacing between ticks.
    /// </summary>
    abstract member FormatTicks: locs: float[] -> string[]

/// <summary>
/// Formats tick values as plain decimal numbers, choosing the number of decimal
/// places from the precision required by the tick set and padding consistently.
/// </summary>
/// <remarks>
/// A faithful subset of <c>matplotlib.ticker.ScalarFormatter</c> covering the
/// common non-scientific, no-offset case used by linear axes.
/// </remarks>
// TODO(roadmap): scientific notation + a shared "×10ⁿ" offset label for very
// large/small values (matplotlib's axes.formatter.limits powerlimits). Needs an
// OffsetText surface on the formatter + axis rendering. See PORTING.md
// (Known deviations / TODO).
type ScalarFormatter() =

    /// <summary>Math.Round's maximum supported decimal count.</summary>
    let maxDecimals = 15

    /// <summary>
    /// Smallest number of decimals that keeps <paramref name="v"/> distinguishable
    /// at a scale of <paramref name="tol"/>.
    /// </summary>
    let decimalsFor (tol: float) (v: float) : int =
        let mutable d = 0
        let mutable searching = true

        while searching && d < maxDecimals do
            if abs (v - Math.Round(v, d)) <= tol then
                searching <- false
            else
                d <- d + 1

        d

    /// <summary>
    /// The scale the precision is judged against: the tick span, falling back to
    /// the largest magnitude for a single (or constant) tick.
    /// </summary>
    let toleranceScale (locs: float[]) : float =
        let finite = locs |> Array.filter Double.IsFinite

        if finite.Length = 0 then
            1.0
        else
            let span = Array.max finite - Array.min finite

            if span > 0.0 then
                span
            else
                let magnitude = finite |> Array.map abs |> Array.max
                if magnitude > 0.0 then magnitude else 1.0

    interface ITickFormatter with
        member _.FormatTicks(locs: float[]) : string[] =
            if locs.Length = 0 then
                [||]
            else
                // Precision follows the tick *span*, like matplotlib's sigfig search.
                // An absolute tolerance floor instead collapses every value below it
                // to "0" (a whole 1e-7 axis) and drops the decimals that separate
                // closely-spaced ticks at a large offset (1000000.5 -> "1000000").
                let tol = 1e-6 * toleranceScale locs
                let decimals = locs |> Array.map (decimalsFor tol) |> Array.max

                locs
                |> Array.map (fun v ->
                    let rounded = Math.Round(v, decimals) + 0.0
                    rounded.ToString("F" + string decimals, CultureInfo.InvariantCulture))

/// <summary>Labels ticks from a fixed list, indexed by the (rounded) tick position.</summary>
/// <remarks>Ported from <c>matplotlib.ticker.FixedFormatter</c> (used for 0..n-1 categories).</remarks>
type FixedFormatter(labels: string[]) =

    interface ITickFormatter with
        member _.FormatTicks(locs: float[]) : string[] =
            locs
            |> Array.map (fun v ->
                let i = int (Math.Round v)
                if i >= 0 && i < labels.Length then labels[i] else "")

/// <summary>
/// Labels ticks by matching each tick value to a known position (within tolerance),
/// so arbitrary <c>set_xticks</c>/<c>set_yticks</c> positions get the right label
/// regardless of order or which ticks fall in view.
/// </summary>
type LabeledTicksFormatter(positions: float[], labels: string[]) =

    interface ITickFormatter with
        member _.FormatTicks(locs: float[]) : string[] =
            locs
            |> Array.map (fun v ->
                match positions |> Array.tryFindIndex (fun p -> abs (p - v) <= 1e-9 + 1e-9 * abs p) with
                | Some i when i < labels.Length -> labels[i]
                | _ -> "")

/// <summary>Formats tick values (OLE Automation date numbers) as dates.</summary>
/// <remarks>Ported from <c>matplotlib.dates.DateFormatter</c> (numeric-day formatting).</remarks>
type DateFormatter(format: string) =

    interface ITickFormatter with
        member _.FormatTicks(locs: float[]) : string[] =
            locs
            |> Array.map (fun v ->
                try
                    DateTime.FromOADate(v).ToString(format, CultureInfo.InvariantCulture)
                with _ ->
                    "")

/// <summary>Factory functions for tick formatters.</summary>
[<RequireQualifiedAccess>]
module TickFormatter =

    /// <summary>The default scalar (decimal) formatter for linear axes.</summary>
    let scalar: ITickFormatter = ScalarFormatter() :> ITickFormatter
