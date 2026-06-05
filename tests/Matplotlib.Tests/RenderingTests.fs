namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Backends

module RenderingTests =

    [<Fact>]
    let ``Pyplot renders a line plot to SVG`` () =
        let plt = Pyplot()

        plt.Plot([| 1.0; 2.0; 3.0; 4.0 |], [| 1.0; 4.0; 9.0; 16.0 |], color = "C0", label = "y = x^2")
        |> ignore

        plt.Title "demo"
        plt.XLabel "x"
        plt.YLabel "y"
        plt.Legend()
        let svg = plt.ToSvg()
        Assert.Contains("<svg", svg)
        Assert.Contains("</svg>", svg)
        Assert.Contains("<path", svg)
        Assert.Contains("<text", svg)
        // the line color C0 = #1f77b4 should appear as a stroke
        Assert.Contains("#1f77b4", svg)
        // title text present
        Assert.Contains("demo", svg)

    [<Fact>]
    let ``FontFamily setting flows into rendered text`` () =
        let plt = Pyplot()
        plt.FontFamily <- "맑은 고딕"
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        plt.Title "제목"
        let svg = plt.ToSvg()
        Assert.Contains("font-family=\"맑은 고딕\"", svg)
        // and never falls back to the generic family for the configured text
        Assert.DoesNotContain("font-family=\"sans-serif\"", svg)

    [<Fact>]
    let ``Default font family is sans-serif`` () =
        let plt = Pyplot()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        plt.Title "demo"
        Assert.Contains("font-family=\"sans-serif\"", plt.ToSvg())

    [<Fact>]
    let ``Figure pixel size honors dpi`` () =
        let fig = Figure()
        fig.SizeInches <- { Width = 6.4; Height = 4.8 }
        fig.Dpi <- 100.0
        assertClose 640.0 fig.PixelSize.Width
        assertClose 480.0 fig.PixelSize.Height

    [<Fact>]
    let ``Canvas reports configured size and dpi`` () =
        let fig = Figure()
        let ax = fig.AddSubplot()
        ax.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        let svg = FigureCanvas(fig).RenderToSvg()
        Assert.Contains("width=\"640\"", svg)
        Assert.Contains("height=\"480\"", svg)

    [<Fact>]
    let ``Object oriented API works without the facade`` () =
        let fig = Figure()
        let ax = fig.AddSubplot()
        ax.SetXLim(0.0, 10.0)
        ax.SetYLim(0.0, 100.0)
        ax.Plot([| 0.0; 5.0; 10.0 |], [| 0.0; 25.0; 100.0 |]) |> ignore
        let svg = FigureCanvas(fig).RenderToSvg()
        Assert.Contains("<svg", svg)
        Assert.True(svg.Length > 200)
