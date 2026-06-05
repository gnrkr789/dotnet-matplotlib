namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Style

module StepErrorbarTests =

    [<Fact>]
    let ``Bars stick to their baseline (no bottom margin)`` () =
        let ax = Axes()
        ax.Bar([| 0.0; 1.0; 2.0 |], [| 1.0; 2.0; 3.0 |]) |> ignore
        // baseline 0 is sticky -> lower limit is exactly 0
        assertClose 0.0 ax.YLim.Lower
        // top still gets a margin
        Assert.True(ax.YLim.Upper > 3.0)

    [<Fact>]
    let ``A plain line still gets margins on both sides`` () =
        let ax = Axes()
        ax.Plot([| 0.0; 1.0 |], [| 0.0; 10.0 |]) |> ignore
        Assert.True(ax.YLim.Lower < 0.0)
        Assert.True(ax.YLim.Upper > 10.0)

    [<Fact>]
    let ``Step pre expands points with the step at the left`` () =
        let ax = Axes()
        let line = ax.Step([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 0.0 |], where = Pre)
        Assert.Equal<float[]>([| 0.0; 0.0; 1.0; 1.0; 2.0 |], line.XData)
        Assert.Equal<float[]>([| 0.0; 1.0; 1.0; 0.0; 0.0 |], line.YData)

    [<Fact>]
    let ``Step post expands points with the step at the right`` () =
        let ax = Axes()
        let line = ax.Step([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 0.0 |], where = Post)
        Assert.Equal<float[]>([| 0.0; 1.0; 1.0; 2.0; 2.0 |], line.XData)
        Assert.Equal<float[]>([| 0.0; 0.0; 1.0; 1.0; 0.0 |], line.YData)

    [<Fact>]
    let ``Errorbar adds the main line plus one segment per y error`` () =
        let ax = Axes()
        ax.Errorbar([| 0.0; 1.0 |], [| 0.0; 0.0 |], yerr = [| 1.0; 1.0 |]) |> ignore
        // main line + 2 vertical error segments
        Assert.Equal(3, ax.Lines.Count)
        // error extents drive the y limits
        Assert.True(ax.YLim.Lower < -1.0)
        Assert.True(ax.YLim.Upper > 1.0)

    [<Fact>]
    let ``Pyplot step and errorbar render to SVG`` () =
        let plt = Pyplot()

        plt.Step([| 0.0; 1.0; 2.0; 3.0 |], [| 1.0; 3.0; 2.0; 4.0 |], where = "mid", color = "C0")
        |> ignore

        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)

        let plt2 = Pyplot()

        plt2.Errorbar([| 0.0; 1.0; 2.0 |], [| 1.0; 2.0; 1.5 |], yerr = [| 0.2; 0.3; 0.25 |], color = "C1")
        |> ignore

        Assert.Contains("<path", plt2.ToSvg())
