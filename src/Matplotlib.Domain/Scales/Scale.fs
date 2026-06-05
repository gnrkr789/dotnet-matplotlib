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

/// <summary>Places symlog ticks: zero, ±linthresh and ±decades beyond it.</summary>
/// <remarks>Ported from <c>matplotlib.ticker.SymmetricalLogLocator</c> (decade subset).</remarks>
type SymlogLocator(linthresh: float) =

    interface ITickLocator with
        member _.TickValues(view: Interval) : float[] =
            let hi = max (abs view.Min) (abs view.Max)

            let maxExp =
                if hi > linthresh then
                    int (ceil (log10 (hi / linthresh)))
                else
                    0

            let decades =
                [
                    for k in 0..maxExp do
                        let v = linthresh * (10.0 ** float k)
                        yield v
                        yield -v
                ]

            (0.0 :: decades)
            |> List.filter (fun v -> v >= view.Min - 1e-12 && v <= view.Max + 1e-12)
            |> List.sort
            |> List.toArray

/// <summary>
/// A symmetric log scale: linear within <c>±linthresh</c>, logarithmic beyond.
/// </summary>
/// <remarks>Ported from <c>matplotlib.scale.SymmetricalLogScale</c> (base 10).</remarks>
type SymlogScale(?linthresh: float, ?linscale: float) =

    let lt = defaultArg linthresh 1.0
    let ls = defaultArg linscale 1.0
    let baseN = 10.0
    let linscaleAdj = ls / (1.0 - 1.0 / baseN)

    interface IScale with
        member _.Name = "symlog"

        member _.TransformValue(value: float) =
            let a = abs value

            if a <= lt then
                value * linscaleAdj
            else
                float (sign value) * lt * (linscaleAdj + log (a / lt) / log baseN)

        member _.InverseValue(value: float) =
            let a = abs value

            if a <= lt * linscaleAdj then
                value / linscaleAdj
            else
                float (sign value) * lt * (baseN ** (a / lt - linscaleAdj))

        member _.ClampLimits(view: Interval) = view
        member _.CreateLocator(_nbins: int) = SymlogLocator(lt) :> ITickLocator
        member _.CreateFormatter() = ScalarFormatter() :> ITickFormatter

/// <summary>Places logit ticks at the usual 0.01..0.99 probabilities.</summary>
/// <remarks>Ported from <c>matplotlib.ticker.LogitLocator</c> (fixed subset).</remarks>
type LogitLocator() =

    let candidates = [| 0.01; 0.05; 0.1; 0.2; 0.3; 0.5; 0.7; 0.8; 0.9; 0.95; 0.99 |]

    interface ITickLocator with
        member _.TickValues(view: Interval) : float[] =
            candidates
            |> Array.filter (fun v -> v >= view.Min - 1e-12 && v <= view.Max + 1e-12)

/// <summary>A logit scale mapping <c>(0, 1)</c> via <c>log(x / (1 - x))</c>.</summary>
/// <remarks>Ported from <c>matplotlib.scale.LogitScale</c>.</remarks>
type LogitScale() =

    let eps = 1e-7

    interface IScale with
        member _.Name = "logit"

        member _.TransformValue(value: float) =
            let x = max eps (min (1.0 - eps) value)
            log (x / (1.0 - x))

        member _.InverseValue(value: float) = 1.0 / (1.0 + exp (-value))

        member _.ClampLimits(view: Interval) =
            let lower = if view.Lower <= 0.0 then 1e-3 else view.Lower
            let upper = if view.Upper >= 1.0 then 1.0 - 1e-3 else view.Upper
            { Lower = lower; Upper = upper }

        member _.CreateLocator(_nbins: int) = LogitLocator() :> ITickLocator
        member _.CreateFormatter() = ScalarFormatter() :> ITickFormatter

/// <summary>Factory for scales by Matplotlib name.</summary>
[<RequireQualifiedAccess>]
module Scale =

    /// <summary>Create a scale by name (<c>linear</c>, <c>log</c>, <c>symlog</c>, <c>logit</c>).</summary>
    let byName (name: string) : IScale =
        match name with
        | "linear" -> LinearScale() :> IScale
        | "log" -> LogScale() :> IScale
        | "symlog" -> SymlogScale() :> IScale
        | "logit" -> LogitScale() :> IScale
        | other -> failwith $"Unknown scale '{other}'."
