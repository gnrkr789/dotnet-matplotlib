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
type ScalarFormatter() =

    let decimalsFor (v: float) : int =
        let mutable d = 0
        let mutable searching = true

        while searching && d < 12 do
            let scaled = v * (10.0 ** float d)

            if abs (scaled - Math.Round scaled) <= 1e-6 * max 1.0 (abs scaled) then
                searching <- false
            else
                d <- d + 1

        d

    interface ITickFormatter with
        member _.FormatTicks(locs: float[]) : string[] =
            if locs.Length = 0 then
                [||]
            else
                let decimals = locs |> Array.map decimalsFor |> Array.max

                locs
                |> Array.map (fun v ->
                    let rounded = Math.Round(v, decimals) + 0.0
                    rounded.ToString("F" + string decimals, CultureInfo.InvariantCulture))

/// <summary>Factory functions for tick formatters.</summary>
[<RequireQualifiedAccess>]
module TickFormatter =

    /// <summary>The default scalar (decimal) formatter for linear axes.</summary>
    let scalar: ITickFormatter = ScalarFormatter() :> ITickFormatter
