namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain.Style

module MarkerLegendTests =

    [<Fact>]
    let ``parseMarker covers the extended marker set`` () =
        Assert.Equal(TriangleDown, Styles.parseMarker "v")
        Assert.Equal(TriangleLeft, Styles.parseMarker "<")
        Assert.Equal(TriangleRight, Styles.parseMarker ">")
        Assert.Equal(Pentagon, Styles.parseMarker "p")
        Assert.Equal(Star, Styles.parseMarker "*")
        Assert.Equal(Hexagon, Styles.parseMarker "h")
        Assert.Equal(Hexagon, Styles.parseMarker "H")
        Assert.Equal(ThinDiamond, Styles.parseMarker "d")
        Assert.Equal(VLine, Styles.parseMarker "|")
        Assert.Equal(HLine, Styles.parseMarker "_")

    [<Fact>]
    let ``Scatter with a star marker renders filled paths`` () =
        let plt = Plt()

        plt.Scatter([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 0.5 |], color = "C0", marker = "*")
        |> ignore

        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)
        Assert.Contains("#1f77b4", svg)

    [<Fact>]
    let ``Scatter s is a points-squared area (diameter = sqrt s)`` () =
        let plt = Plt()
        // default s = 36 -> marker diameter 6 points
        let def = plt.Scatter([| 0.0 |], [| 0.0 |])
        assertClose 6.0 def.MarkerSize
        // s = 144 -> diameter sqrt(144) = 12 points
        let big = plt.Scatter([| 0.0 |], [| 0.0 |], s = 144.0)
        assertClose 12.0 big.MarkerSize

    [<Fact>]
    let ``Scatter sizes gives per-point diameters (sqrt of the area)`` () =
        let plt = Plt()
        let line = plt.Scatter([| 0.0; 1.0 |], [| 0.0; 1.0 |], sizes = [| 36.0; 144.0 |])
        Assert.True line.MarkerSizes.IsSome
        let ds = line.MarkerSizes.Value
        assertClose 6.0 ds[0]
        assertClose 12.0 ds[1]

    [<Fact>]
    let ``parseLegendLoc covers the standard locations`` () =
        Assert.Equal(Best, Styles.parseLegendLoc "best")
        Assert.Equal(UpperRight, Styles.parseLegendLoc "upper right")
        Assert.Equal(LowerLeft, Styles.parseLegendLoc "lower left")
        Assert.Equal(CenterRight, Styles.parseLegendLoc "center right")
        Assert.Equal(Center, Styles.parseLegendLoc "center")

    [<Fact>]
    let ``Legend loc is applied to the current axes and still renders`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |], color = "C0", label = "a") |> ignore
        plt.Legend "lower left"
        Assert.Equal(LowerLeft, plt.CurrentAxes().LegendLoc)
        Assert.True(plt.CurrentAxes().ShowLegend)
        Assert.Contains("<text", plt.ToSvg())
