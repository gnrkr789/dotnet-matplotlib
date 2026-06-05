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
        // y range covers baseline 0 to tallest bar 3 (plus margins)
        Assert.True(ax.YLim.Lower < 0.0)
        Assert.True(ax.YLim.Upper > 3.0)

    [<Fact>]
    let ``Pyplot bar renders filled rectangles to SVG`` () =
        let plt = Pyplot()

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
