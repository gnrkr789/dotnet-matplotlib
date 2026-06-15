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
    let ``Errorbar capsize adds a cap-marker line`` () =
        let ax = Axes()

        ax.Errorbar([| 0.0; 1.0 |], [| 0.0; 0.0 |], yerr = [| 1.0; 1.0 |], capsize = 5.0)
        |> ignore
        // main line + 2 vertical error segments + 1 cap-marker line ('_' caps)
        Assert.Equal(4, ax.Lines.Count)
        let caps = ax.Lines[ax.Lines.Count - 1]
        Assert.Equal(MarkerStyle.HLine, caps.Marker)
        Assert.Equal(LineStyle.NoLine, caps.LineStyle)

    [<Fact>]
    let ``Bar with yerr adds error-bar lines on top of the bars`` () =
        let ax = Axes()

        ax.Bar([| 0.0; 1.0 |], [| 2.0; 3.0 |], yerr = [| 0.5; 0.5 |], capsize = 4.0)
        |> ignore

        Assert.Equal(2, ax.Patches.Count) // the two bars
        Assert.Equal(3, ax.Lines.Count) // 2 error segments + 1 cap-marker line
        // the y range expands to include the upper error (3 + 0.5)
        Assert.True(ax.YLim.Upper >= 3.5)

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
