namespace Matplotlib.Domain.Ticking

open System
open Matplotlib.Domain.Primitives

/// <summary>
/// Decides where ticks are placed on an axis given its view interval.
/// </summary>
/// <remarks>Ported from the <c>Locator</c> hierarchy in <c>matplotlib.ticker</c>.</remarks>
type ITickLocator =

    /// <summary>Return the tick locations spanning the given view interval.</summary>
    abstract member TickValues: view: Interval -> float[]

/// <summary>Numeric helpers shared by the tick locators.</summary>
[<RequireQualifiedAccess>]
module internal TickMath =

    /// <summary>Ported from <c>matplotlib.ticker.scale_range</c>.</summary>
    let scaleRange (vmin: float) (vmax: float) (n: int) (threshold: float) : float * float =
        let dv = abs (vmax - vmin)
        let meanv = (vmax + vmin) / 2.0

        let offset =
            if dv = 0.0 then
                0.0
            elif abs meanv / dv < threshold then
                0.0
            else
                float (sign meanv) * (10.0 ** floor (log10 (abs meanv)))

        let scale = 10.0 ** floor (log10 (dv / float n))
        scale, offset

    /// <summary>Ported from <c>matplotlib.transforms.nonsingular</c>.</summary>
    let nonsingular (vmin: float) (vmax: float) (expander: float) (tiny: float) : float * float =
        let mutable lo = vmin
        let mutable hi = vmax

        if lo > hi then
            let t = lo
            lo <- hi
            hi <- t

        let maxabs = max (abs lo) (abs hi)

        if maxabs < 1e-300 then
            lo <- -expander
            hi <- expander
        elif (hi - lo) <= maxabs * tiny then
            if lo = 0.0 && hi = 0.0 then
                lo <- -expander
                hi <- expander
            else
                lo <- lo - expander * abs lo
                hi <- hi + expander * abs hi

        lo, hi

    /// <summary>Ported from <c>matplotlib.ticker.MaxNLocator._staircase</c>.</summary>
    let staircase (steps: float[]) : float[] =
        Array.concat
            [
                steps[.. steps.Length - 2] |> Array.map (fun s -> 0.1 * s)
                steps
                [| 10.0 * steps[1] |]
            ]

    /// <summary>Ensure a steps array begins with 1 and ends with 10.</summary>
    let validateSteps (steps: float[]) : float[] =
        let withOne =
            if steps[0] <> 1.0 then
                Array.append [| 1.0 |] steps
            else
                steps

        if withOne[withOne.Length - 1] <> 10.0 then
            Array.append withOne [| 10.0 |]
        else
            withOne

/// <summary>Ported from <c>matplotlib.ticker._Edge_integer</c>.</summary>
type internal EdgeInteger(step: float, offset: float) =

    let offs = abs offset

    member _.CloseTo(ms: float, edge: float) : bool =
        let tol =
            if offs > 0.0 then
                let digits = log10 (offs / step)
                min 0.4999 (max 1e-10 (10.0 ** (digits - 12.0)))
            else
                1e-10

        abs (ms - edge) < tol

    /// <summary>Largest n such that n*step &lt;= x.</summary>
    member this.Le(x: float) : float =
        let d = floor (x / step)
        let m = x - d * step
        if this.CloseTo(m / step, 1.0) then d + 1.0 else d

    /// <summary>Smallest n such that n*step &gt;= x.</summary>
    member this.Ge(x: float) : float =
        let d = floor (x / step)
        let m = x - d * step
        if this.CloseTo(m / step, 0.0) then d else d + 1.0

/// <summary>
/// Places evenly spaced ticks with a cap on the total number of ticks, choosing
/// "nice" multiples from the configured steps.
/// </summary>
/// <remarks>Ported from <c>matplotlib.ticker.MaxNLocator</c>.</remarks>
type MaxNLocator(?nbins: int, ?steps: float[], ?integer: bool, ?minNTicks: int) =

    let stepsArr =
        match steps with
        | Some s -> TickMath.validateSteps s
        | None -> [| 1.0; 1.5; 2.0; 2.5; 3.0; 4.0; 5.0; 6.0; 8.0; 10.0 |]

    let extended = TickMath.staircase stepsArr
    let nb = defaultArg nbins 10
    let integ = defaultArg integer false
    let minTicks = max 1 (defaultArg minNTicks 2)

    /// <summary>The maximum number of intervals (one fewer than max ticks).</summary>
    member _.Nbins = nb

    /// <summary>Ported from <c>MaxNLocator._raw_ticks</c>.</summary>
    member _.RawTicks(vmin: float, vmax: float) : float[] =
        let scale, offset = TickMath.scaleRange vmin vmax nb 100.0
        let vminS = vmin - offset
        let vmaxS = vmax - offset
        let scaled = extended |> Array.map (fun s -> s * scale)

        let steps' =
            if integ then
                scaled |> Array.filter (fun s -> s < 1.0 || abs (s - Math.Round s) < 0.001)
            else
                scaled

        let rawStep = (vmaxS - vminS) / float nb
        let large = steps' |> Array.map (fun s -> s >= rawStep)

        let istep =
            match Array.tryFindIndex id large with
            | Some idx -> idx
            | None -> steps'.Length - 1

        let mutable ticks: float[] = [||]
        let mutable found = false
        let mutable k = istep

        while not found && k >= 0 do
            let mutable step = steps'[k]

            if integ && (floor vmaxS - ceil vminS >= float (minTicks - 1)) then
                step <- max 1.0 step

            let bestVmin = floor (vminS / step) * step
            let edge = EdgeInteger(step, offset)
            let low = edge.Le(vminS - bestVmin)
            let high = edge.Ge(vmaxS - bestVmin)
            let count = int (high - low) + 1
            let t = Array.init (max 0 count) (fun i -> (low + float i) * step + bestVmin)
            ticks <- t
            let nticks = t |> Array.filter (fun v -> v <= vmaxS && v >= vminS) |> Array.length

            if nticks >= minTicks then
                found <- true

            k <- k - 1

        ticks |> Array.map (fun v -> v + offset)

    interface ITickLocator with
        member this.TickValues(view: Interval) : float[] =
            let vmin, vmax = TickMath.nonsingular view.Lower view.Upper 1e-13 1e-14
            this.RawTicks(vmin, vmax)

/// <summary>Factory functions for tick locators.</summary>
[<RequireQualifiedAccess>]
module TickLocator =

    /// <summary>
    /// The default linear-axis locator: <c>MaxNLocator</c> with
    /// <c>steps = [1, 2, 2.5, 5, 10]</c> (Matplotlib's <c>AutoLocator</c>).
    /// </summary>
    let linearAuto (nbins: int) : ITickLocator =
        MaxNLocator(nbins = nbins, steps = [| 1.0; 2.0; 2.5; 5.0; 10.0 |]) :> ITickLocator
