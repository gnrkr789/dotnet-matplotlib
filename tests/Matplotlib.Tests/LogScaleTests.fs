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
    let ``Plt log-log plot renders with decade labels`` () =
        let plt = Plt()

        plt.Plot([| 1.0; 10.0; 100.0; 1000.0 |], [| 1.0; 100.0; 10.0; 1000.0 |])
        |> ignore

        plt.XScale "log"
        plt.YScale "log"
        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)
        Assert.Contains(">100<", svg)

    [<Fact>]
    let ``Symlog scale is symmetric and round-trips`` () =
        let scale = SymlogScale() :> IScale
        Assert.Equal("symlog", scale.Name)
        assertClose 0.0 (scale.TransformValue 0.0)
        assertCloseTol 1e-9 123.4 (scale.InverseValue(scale.TransformValue 123.4))
        assertCloseTol 1e-9 -0.5 (scale.InverseValue(scale.TransformValue -0.5))
        assertCloseTol 1e-12 (-(scale.TransformValue 50.0)) (scale.TransformValue -50.0)

    [<Fact>]
    let ``Logit scale maps 0.5 to 0 and round-trips`` () =
        let scale = LogitScale() :> IScale
        Assert.Equal("logit", scale.Name)
        assertClose 0.0 (scale.TransformValue 0.5)
        assertClose 0.5 (scale.InverseValue 0.0)
        assertCloseTol 1e-9 0.73 (scale.InverseValue(scale.TransformValue 0.73))

    [<Fact>]
    let ``Scale factory resolves symlog and logit`` () =
        Assert.Equal("symlog", (Scale.byName "symlog").Name)
        Assert.Equal("logit", (Scale.byName "logit").Name)

    [<Fact>]
    let ``Symlog axis renders across zero`` () =
        let plt = Plt()

        plt.Plot([| -100.0; -1.0; 0.0; 1.0; 100.0 |], [| -100.0; -1.0; 0.0; 1.0; 100.0 |])
        |> ignore

        plt.YScale "symlog"
        plt.XScale "symlog"
        Assert.Contains("<path", plt.ToSvg())
