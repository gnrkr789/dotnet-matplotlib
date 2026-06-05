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
