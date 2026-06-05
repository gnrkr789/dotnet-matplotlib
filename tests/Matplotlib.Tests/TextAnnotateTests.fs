namespace Matplotlib.Tests

open Xunit
open Matplotlib

module TextAnnotateTests =

    [<Fact>]
    let ``Text renders at a data position`` () =
        let plt = Pyplot()
        plt.Text(0.5, 0.5, "hello", color = "C0") |> ignore
        let svg = plt.ToSvg()
        Assert.Contains("<text", svg)
        Assert.Contains("hello", svg)

    [<Fact>]
    let ``Text does not affect autoscale`` () =
        let plt = Pyplot()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |], color = "C0") |> ignore
        plt.Text(100.0, 100.0, "far away") |> ignore
        // the far-away text must not expand the data limits
        Assert.True(plt.CurrentAxes().XLim.Upper < 2.0)
        Assert.True(plt.CurrentAxes().YLim.Upper < 2.0)

    [<Fact>]
    let ``Annotate with an arrow draws a connector and the text`` () =
        let plt = Pyplot()
        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 0.0 |], color = "C0") |> ignore

        plt.Annotate("peak", (1.0, 1.0), xytext = (1.4, 0.8), arrow = true, color = "C3")
        |> ignore

        let svg = plt.ToSvg()
        Assert.Contains("peak", svg)
        Assert.Contains("<path", svg)

    [<Fact>]
    let ``Annotate without arrow still places the text`` () =
        let plt = Pyplot()
        plt.Annotate("label", (0.5, 0.5)) |> ignore
        Assert.Contains("label", plt.ToSvg())
