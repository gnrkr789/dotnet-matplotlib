namespace Matplotlib.Domain.Scales

open System
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Ticking

/// <summary>
/// Maps data values to a transformed axis space (e.g. log) and provides the
/// default tick locator/formatter for that space.
/// </summary>
/// <remarks>Ported from the <c>ScaleBase</c> hierarchy in <c>matplotlib.scale</c>.</remarks>
type IScale =

    /// <summary>The registered scale name (e.g. <c>"linear"</c>, <c>"log"</c>).</summary>
    abstract member Name: string

    /// <summary>Map a data value into scaled (axis) space.</summary>
    abstract member TransformValue: value: float -> float

    /// <summary>Map a scaled value back to data space.</summary>
    abstract member InverseValue: value: float -> float

    /// <summary>Constrain a view interval to the scale's valid domain (e.g. positive for log).</summary>
    abstract member ClampLimits: view: Interval -> Interval

    /// <summary>Create the default tick locator for this scale.</summary>
    abstract member CreateLocator: nbins: int -> ITickLocator

    /// <summary>Create the default tick formatter for this scale.</summary>
    abstract member CreateFormatter: unit -> ITickFormatter

/// <summary>The standard linear scale (identity value transform).</summary>
/// <remarks>Ported from <c>matplotlib.scale.LinearScale</c>.</remarks>
type LinearScale() =

    interface IScale with
        member _.Name = "linear"
        member _.TransformValue(value: float) = value
        member _.InverseValue(value: float) = value
        member _.ClampLimits(view: Interval) = view
        member _.CreateLocator(nbins: int) = TickLocator.linearAuto nbins
        member _.CreateFormatter() = ScalarFormatter() :> ITickFormatter

/// <summary>Places ticks at integer powers of ten within the view.</summary>
/// <remarks>Ported from <c>matplotlib.ticker.LogLocator</c> (base 10).</remarks>
type LogLocator() =

    interface ITickLocator with
        member _.TickValues(view: Interval) : float[] =
            let lo = max view.Min 1e-300
            let hi = max view.Max (lo * 10.0)
            let loExp = int (floor (log10 lo))
            let hiExp = int (ceil (log10 hi))
            let numdec = hiExp - loExp
            let stride = max 1 (int (ceil (float numdec / 12.0)))
            [| for k in loExp..stride..hiExp -> 10.0 ** float k |]

/// <summary>Formats decade tick values (plain near 1, scientific far away).</summary>
/// <remarks>Ported from <c>matplotlib.ticker.LogFormatter</c> (decade subset).</remarks>
type LogFormatter() =

    let format (value: float) =
        if value <= 0.0 then
            ""
        else
            let e = int (Math.Round(log10 value))

            if e >= 0 && e <= 4 then
                (pown 10L e).ToString(Globalization.CultureInfo.InvariantCulture)
            elif e < 0 && e >= -4 then
                "0." + String('0', -e - 1) + "1"
            else
                $"1e{e}"

    interface ITickFormatter with
        member _.FormatTicks(locs: float[]) : string[] = locs |> Array.map format

/// <summary>A base-10 logarithmic scale.</summary>
/// <remarks>Ported from <c>matplotlib.scale.LogScale</c> (base 10).</remarks>
type LogScale() =

    interface IScale with
        member _.Name = "log"
        member _.TransformValue(value: float) = if value > 0.0 then log10 value else log10 1e-300
        member _.InverseValue(value: float) = 10.0 ** value

        member _.ClampLimits(view: Interval) =
            let upper = if view.Upper > 0.0 then view.Upper else 10.0
            let lower = if view.Lower > 0.0 then view.Lower else upper / 1000.0
            { Lower = lower; Upper = upper }

        member _.CreateLocator(_nbins: int) = LogLocator() :> ITickLocator
        member _.CreateFormatter() = LogFormatter() :> ITickFormatter

/// <summary>Factory for scales by Matplotlib name.</summary>
[<RequireQualifiedAccess>]
module Scale =

    /// <summary>Create a scale by name (<c>"linear"</c> or <c>"log"</c>).</summary>
    let byName (name: string) : IScale =
        match name with
        | "linear" -> LinearScale() :> IScale
        | "log" -> LogScale() :> IScale
        | other -> failwith $"Unknown scale '{other}'."
