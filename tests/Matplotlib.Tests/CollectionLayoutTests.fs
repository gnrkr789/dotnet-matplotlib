namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering
open Matplotlib.Domain.Artists
open Matplotlib.Backends

module CollectionLayoutTests =

    [<Fact>]
    let ``PathPatch bounds span its vertices`` () =
        let path = Path.polygon [ { X = 0.0; Y = 0.0 }; { X = 2.0; Y = 0.0 }; { X = 1.0; Y = 3.0 } ]
        let patch = PathPatch(path)
        let b = patch.DataBounds().Value
        assertClose 0.0 b.XMin
        assertClose 2.0 b.XMax
        assertClose 0.0 b.YMin
        assertClose 3.0 b.YMax

    [<Fact>]
    let ``LineCollection autoscales and renders`` () =
        let fig = Figure()
        let ax = fig.AddSubplot()

        let lc =
            LineCollection(
                [
                    [| { X = 0.0; Y = 0.0 }; { X = 1.0; Y = 1.0 } |]
                    [| { X = 1.0; Y = 0.0 }; { X = 2.0; Y = 2.0 } |]
                ]
            )

        ax.AddCollection lc |> ignore
        Assert.Equal(1, ax.Collections.Count)
        let b = lc.DataBounds().Value
        assertClose 0.0 b.XMin
        assertClose 2.0 b.XMax
        assertClose 2.0 b.YMax
        Assert.Contains("<path", FigureCanvas(fig).RenderToSvg())

    [<Fact>]
    let ``PolyCollection renders filled polygons`` () =
        let fig = Figure()
        let ax = fig.AddSubplot()

        let pc = PolyCollection([ [| { X = 0.0; Y = 0.0 }; { X = 1.0; Y = 0.0 }; { X = 0.5; Y = 1.0 } |] ])

        pc.FaceColor <- Color.fromHex "#1f77b4"
        ax.AddCollection pc |> ignore
        let svg = FigureCanvas(fig).RenderToSvg()
        Assert.Contains("fill=\"#1f77b4\"", svg)

    [<Fact>]
    let ``Default legend location is Best`` () =
        let ax = Axes()
        Assert.Equal(LegendLoc.Best, ax.LegendLoc)

    [<Fact>]
    let ``Best legend still renders the label`` () =
        let plt = Pyplot()

        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 2.0 |], color = "C0", label = "series")
        |> ignore

        plt.Legend()
        Assert.Contains("series", plt.ToSvg())

    [<Fact>]
    let ``Tight layout expands the axes to use more of the figure`` () =
        let plt = Pyplot()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |], color = "C0") |> ignore
        plt.XLabel "x"
        plt.YLabel "y"
        plt.Title "t"
        plt.TightLayout()
        let ax = plt.CurrentAxes()
        Assert.True(ax.Position.X1 > 0.9)
        Assert.True(ax.Position.Y1 > 0.88)
