namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Artists

module PatchTests =

    [<Fact>]
    let ``Rectangle data bounds and path`` () =
        let rect = Rectangle(1.0, 2.0, 3.0, 4.0)
        let b = rect.DataBounds().Value
        assertClose 1.0 b.X0
        assertClose 2.0 b.Y0
        assertClose 4.0 b.X1
        assertClose 6.0 b.Y1
        // polygon: MoveTo + 3 LineTo + ClosePath
        Assert.Equal(5, rect.BuildPath().Commands.Length)

    [<Fact>]
    let ``Polygon data bounds span its vertices`` () =
        let poly = Polygon([| { X = 0.0; Y = 0.0 }; { X = 2.0; Y = 5.0 }; { X = -1.0; Y = 3.0 } |])

        let b = poly.DataBounds().Value
        assertClose -1.0 b.XMin
        assertClose 2.0 b.XMax
        assertClose 0.0 b.YMin
        assertClose 5.0 b.YMax

    [<Fact>]
    let ``Bar creates one patch per value and autoscales over them`` () =
        let ax = Axes()
        ax.Bar([| 0.0; 1.0; 2.0 |], [| 1.0; 2.0; 3.0 |]) |> ignore
        Assert.Equal(3, ax.Patches.Count)
        // baseline 0 is sticky (no bottom margin); the top gets a margin
        assertClose 0.0 ax.YLim.Lower
        Assert.True(ax.YLim.Upper > 3.0)

    [<Fact>]
    let ``Plt bar renders filled rectangles to SVG`` () =
        let plt = Plt()

        plt.Bar([| 0.0; 1.0; 2.0 |], [| 1.0; 2.0; 3.0 |], color = "C0", label = "values")
        |> ignore

        plt.Legend()
        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)
        // bars are filled with C0 = #1f77b4
        Assert.Contains("fill=\"#1f77b4\"", svg)

    [<Fact>]
    let ``FillBetween produces a polygon spanning both curves`` () =
        let ax = Axes()
        let poly = ax.FillBetween([| 0.0; 1.0; 2.0 |], [| 1.0; 3.0; 2.0 |])
        Assert.Equal(1, ax.Patches.Count)
        let b = poly.DataBounds().Value
        assertClose 0.0 b.XMin
        assertClose 2.0 b.XMax
        // baseline 0 included as the lower edge
        assertClose 0.0 b.YMin
        assertClose 3.0 b.YMax

    [<Fact>]
    let ``FillBetweenx produces a polygon spanning both curves`` () =
        let ax = Axes()
        let poly = ax.FillBetweenx([| 0.0; 1.0; 2.0 |], [| 1.0; 3.0; 2.0 |])
        Assert.Equal(1, ax.Patches.Count)
        let b = poly.DataBounds().Value
        // baseline x = 0 is the left edge; x1 spans up to 3
        assertClose 0.0 b.XMin
        assertClose 3.0 b.XMax
        assertClose 0.0 b.YMin
        assertClose 2.0 b.YMax

    [<Fact>]
    let ``axhline includes its y in the y view and renders`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        plt.AxHLine(5.0, color = "C3")
        Assert.True(plt.CurrentAxes().YLim.Upper >= 5.0)
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``axvline includes its x in the x view`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        plt.AxVLine 5.0
        Assert.True(plt.CurrentAxes().XLim.Upper >= 5.0)

    [<Fact>]
    let ``axhspan shades a band and widens the y view`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        plt.AxHSpan(2.0, 4.0, color = "C2", alpha = 0.3)
        Assert.True(plt.CurrentAxes().YLim.Upper >= 4.0)
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``twinx shares x and gives an independent right-side y axis`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 10.0 |], [| 0.0; 1.0 |], color = "C0") |> ignore
        let parent = plt.CurrentAxes()
        let ax2 = plt.TwinX()
        ax2.Plot([| 0.0; 10.0 |], [| 0.0; 1000.0 |]) |> ignore
        Assert.Equal("right", ax2.YTickSide)
        Assert.False ax2.XTicksVisible
        Assert.True ax2.SharedXFrom.IsSome
        // independent y scales: parent ~[0,1], twin ~[0,1000]
        Assert.True(ax2.YLim.Upper >= 1000.0)
        Assert.True(parent.YLim.Upper < 2.0)
        Assert.Contains("<path", plt.ToSvg())
