namespace Matplotlib.Tests

open Xunit
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Ticking

module TickingTests =

    let private ticksIn (lo: float) (hi: float) : float[] =
        let locator = TickLocator.linearAuto 9

        locator.TickValues { Lower = lo; Upper = hi }
        |> Array.filter (fun v -> v >= lo - 1e-9 && v <= hi + 1e-9)

    [<Fact>]
    let ``Auto locator on 0..1 gives steps of 0.2`` () =
        let ticks = ticksIn 0.0 1.0

        Assert.Equal<float[]>(
            [| 0.0; 0.2; 0.4; 0.6; 0.8; 1.0 |],
            ticks |> Array.map (fun v -> System.Math.Round(v, 10))
        )

    [<Fact>]
    let ``Auto locator on 0..10 gives even integers`` () =
        let ticks = ticksIn 0.0 10.0

        Assert.Equal<float[]>(
            [| 0.0; 2.0; 4.0; 6.0; 8.0; 10.0 |],
            ticks |> Array.map (fun v -> System.Math.Round(v, 10))
        )

    [<Fact>]
    let ``Auto locator on 0..100 gives steps of 20`` () =
        let ticks = ticksIn 0.0 100.0

        Assert.Equal<float[]>(
            [| 0.0; 20.0; 40.0; 60.0; 80.0; 100.0 |],
            ticks |> Array.map (fun v -> System.Math.Round(v, 10))
        )

    [<Fact>]
    let ``Scalar formatter chooses one decimal for 0.5 spacing`` () =
        let labels = TickFormatter.scalar.FormatTicks [| 0.0; 0.5; 1.0 |]
        Assert.Equal<string[]>([| "0.0"; "0.5"; "1.0" |], labels)

    [<Fact>]
    let ``Scalar formatter renders integers without decimals`` () =
        let labels = TickFormatter.scalar.FormatTicks [| 0.0; 2.0; 4.0 |]
        Assert.Equal<string[]>([| "0"; "2"; "4" |], labels)

    [<Fact>]
    let ``Scalar formatter pads to required precision`` () =
        let labels = TickFormatter.scalar.FormatTicks [| 0.0; 0.25; 0.5 |]
        Assert.Equal<string[]>([| "0.00"; "0.25"; "0.50" |], labels)

    // The precision search used to compare against a tolerance floored at an
    // absolute 1e-6, which made every tick below that round to a bare "0" and
    // dropped the decimals separating close ticks sitting on a large offset.

    [<Fact>]
    let ``Scalar formatter keeps sub-microscopic ticks distinct`` () =
        let labels = TickFormatter.scalar.FormatTicks [| 1e-7; 2e-7; 3e-7 |]
        Assert.Equal<string[]>([| "0.0000001"; "0.0000002"; "0.0000003" |], labels)

    [<Fact>]
    let ``Scalar formatter labels stay distinct down to 1e-15`` () =
        for exponent in 1..15 do
            let step = 10.0 ** float -exponent
            let labels = TickFormatter.scalar.FormatTicks [| step; 2.0 * step; 3.0 * step |]
            let distinct = labels |> Array.distinct
            let rendered = String.concat ", " distinct
            Assert.True(distinct.Length = 3, $"ticks at 1e-{exponent} collapsed to {rendered}")

    [<Fact>]
    let ``Scalar formatter keeps decimals for close ticks at a large offset`` () =
        let labels = TickFormatter.scalar.FormatTicks [| 1000000.0; 1000000.5; 1000001.0 |]
        Assert.Equal<string[]>([| "1000000.0"; "1000000.5"; "1000001.0" |], labels)

    [<Fact>]
    let ``Scalar formatter precision follows the tick span not the magnitude`` () =
        // Same span (1.0), wildly different magnitudes -> same decimal count.
        let near = TickFormatter.scalar.FormatTicks [| 0.0; 0.5; 1.0 |]
        let far = TickFormatter.scalar.FormatTicks [| 1.0e9; 1.0e9 + 0.5; 1.0e9 + 1.0 |]
        Assert.Equal(1, near[0].Length - near[0].IndexOf '.' - 1)
        Assert.Equal(1, far[0].Length - far[0].IndexOf '.' - 1)

    [<Fact>]
    let ``Scalar formatter handles single and constant tick sets`` () =
        Assert.Equal<string[]>([||], TickFormatter.scalar.FormatTicks [||])
        Assert.Equal<string[]>([| "5" |], TickFormatter.scalar.FormatTicks [| 5.0 |])
        Assert.Equal<string[]>([| "0"; "0" |], TickFormatter.scalar.FormatTicks [| 0.0; 0.0 |])
