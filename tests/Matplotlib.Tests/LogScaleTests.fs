namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Ticking
open Matplotlib.Domain.Scales

module LogScaleTests =

    [<Fact>]
    let ``Log locator places ticks at decades`` () =
        let locator = LogLocator() :> ITickLocator
        let ticks = locator.TickValues { Lower = 1.0; Upper = 1000.0 }
        Assert.Equal<float[]>([| 1.0; 10.0; 100.0; 1000.0 |], ticks)

    [<Fact>]
    let ``Log formatter renders decades plainly`` () =
        let formatter = LogFormatter() :> ITickFormatter
        Assert.Equal<string[]>([| "1"; "10"; "100"; "0.1" |], formatter.FormatTicks [| 1.0; 10.0; 100.0; 0.1 |])

    [<Fact>]
    let ``Log scale transforms values via log10`` () =
        let scale = LogScale() :> IScale
        Assert.Equal("log", scale.Name)
        assertClose 2.0 (scale.TransformValue 100.0)
        assertClose 100.0 (scale.InverseValue 2.0)

    [<Fact>]
    let ``Setting a log scale autoscales into positive log-space limits`` () =
        let ax = Axes()

        ax.Plot([| 1.0; 10.0; 100.0; 1000.0 |], [| 1.0; 10.0; 100.0; 1000.0 |])
        |> ignore

        ax.SetYScale "log"
        Assert.True(ax.YLim.Lower > 0.0)
        Assert.True(ax.YLim.Lower < 1.0)
        Assert.True(ax.YLim.Upper > 1000.0)

    [<Fact>]
    let ``Pyplot log-log plot renders with decade labels`` () =
        let plt = Pyplot()

        plt.Plot([| 1.0; 10.0; 100.0; 1000.0 |], [| 1.0; 100.0; 10.0; 1000.0 |])
        |> ignore

        plt.XScale "log"
        plt.YScale "log"
        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)
        Assert.Contains(">100<", svg)
