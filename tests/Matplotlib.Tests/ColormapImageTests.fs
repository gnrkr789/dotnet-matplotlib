namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Backends

module ColormapImageTests =

    [<Fact>]
    let ``Viridis endpoints match the ported lookup table`` () =
        let c0 = Colormap.viridis.Apply 0.0
        assertCloseTol 1e-6 0.267004 c0.R
        assertCloseTol 1e-6 0.004874 c0.G
        assertCloseTol 1e-6 0.329415 c0.B
        let c1 = Colormap.viridis.Apply 1.0
        assertCloseTol 1e-6 0.993248 c1.R
        assertCloseTol 1e-6 0.143936 c1.B

    [<Fact>]
    let ``Colormap lookup is quantized (no interpolation between LUT entries)`` () =
        // matplotlib indexes the LUT with int(t * N); Apply must return that exact
        // entry, not a blend of neighbours.
        let c = Colormap.viridis
        Assert.Equal(c.Lut[0], c.Apply 0.0)
        Assert.Equal(c.Lut[128], c.Apply 0.5)
        Assert.Equal(c.Lut[255], c.Apply 1.0)

    [<Fact>]
    let ``Hot colormap starts at matplotlib's dark red`` () =
        let c = Colormap.hot.Apply 0.0
        assertCloseTol 1e-6 0.0416 c.R
        assertCloseTol 1e-6 0.0 c.G
        assertCloseTol 1e-6 0.0 c.B

    [<Fact>]
    let ``Jet colormap uses matplotlib's per-channel breakpoints`` () =
        // Endpoints: jet(0) = (0, 0, 0.5), jet(1) = (0.5, 0, 0).
        let lo = Colormap.jet.Apply 0.0
        assertCloseTol 1e-6 0.0 lo.R
        assertCloseTol 1e-6 0.5 lo.B
        let hi = Colormap.jet.Apply 1.0
        assertCloseTol 1e-6 0.5 hi.R
        assertCloseTol 1e-6 0.0 hi.B
        // Green ramps down over [0.64, 0.91]; distinct from the old shared-node jet.
        let g = Colormap.jet.Apply 0.75
        assertCloseTol 1e-2 0.5817 g.G

    [<Fact>]
    let ``Scatter c maps values through a colormap`` () =
        let ax = Axes()

        let line = ax.Scatter([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 2.0 |], c = [| 0.0; 0.5; 1.0 |], cmap = "viridis")

        Assert.True line.MarkerColors.IsSome
        let colors = line.MarkerColors.Value
        Assert.Equal(3, colors.Length)
        // c is normalized over [0, 1]; the endpoints hit viridis[0] and viridis[255].
        Assert.Equal(Colormap.viridis.Apply 0.0, colors[0])
        Assert.Equal(Colormap.viridis.Apply 1.0, colors[2])
        Assert.NotEqual(colors[0], colors[1])

    [<Fact>]
    let ``Normalize maps and clamps to [0,1]`` () =
        let n = Normalize(0.0, 10.0)
        assertClose 0.5 (n.Normalize 5.0)
        assertClose 0.0 (n.Normalize -5.0)
        assertClose 1.0 (n.Normalize 15.0)

    [<Fact>]
    let ``Colormaps resolve by name`` () =
        Assert.Equal("viridis", (Colormap.byName "viridis").Name)
        Assert.Equal("gray", (Colormap.byName "grey").Name)
        Assert.Equal("jet", (Colormap.byName "jet").Name)
        Assert.Equal("hot", (Colormap.byName "hot").Name)

    [<Fact>]
    let ``Imshow sets the extent with an inverted (origin-upper) y axis`` () =
        let ax = Axes()
        let data = array2D [ [ 0.0; 1.0 ]; [ 2.0; 3.0 ] ]
        ax.Imshow data |> ignore
        Assert.Equal(1, ax.Images.Count)
        assertClose -0.5 ax.XLim.Lower
        assertClose 1.5 ax.XLim.Upper
        // origin upper -> y axis inverted (row 0 at the top)
        assertClose 1.5 ax.YLim.Lower
        assertClose -0.5 ax.YLim.Upper

    [<Fact>]
    let ``Imshow renders viridis-colored cells to SVG`` () =
        let plt = Plt()
        let data = array2D [ for i in 0..7 -> [ for j in 0..7 -> float (i + j) ] ]
        plt.Imshow(data, cmap = "viridis") |> ignore
        let svg = plt.ToSvg()
        // one filled cell per data point (plus backgrounds)
        Assert.True((svg.Split("fill=\"#").Length - 1) > 60)
        // the minimum value maps to viridis[0] = #440154
        Assert.Contains("#440154", svg)

    [<Fact>]
    let ``Colorbar adds a gradient axes and shrinks the parent`` () =
        let fig = Figure()
        let ax = fig.AddSubplot()
        let data = array2D [ [ 0.0; 1.0 ]; [ 2.0; 3.0 ] ]
        let img = ax.Imshow data
        let before = fig.Axes.Count
        let cax = fig.Colorbar img
        Assert.Equal(before + 1, fig.Axes.Count)
        Assert.Equal(256, cax.Patches.Count)
        Assert.False(cax.XTicksVisible)
        Assert.Equal("right", cax.YTickSide)
        Assert.True(ax.Position.X1 < 0.9)
        Assert.Contains("#440154", FigureCanvas(fig).RenderToSvg())

    [<Fact>]
    let ``Plt colorbar renders value ticks`` () =
        let plt = Plt()
        let data = array2D [ for i in 0..3 -> [ for j in 0..3 -> float (i + j) ] ]
        let img = plt.Imshow data
        plt.Colorbar img |> ignore
        Assert.Contains("<text", plt.ToSvg())

    [<Fact>]
    let ``Colorbar works for a colormapped scatter`` () =
        let plt = Plt()

        let sc = plt.Scatter([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 2.0 |], c = [| 0.0; 5.0; 10.0 |], cmap = "viridis")

        Assert.True sc.ScalarMappable.IsSome
        let before = plt.CurrentFigure().Axes.Count
        plt.Colorbar sc |> ignore
        // a colorbar axes was added with the scatter's 0..10 value scale
        Assert.Equal(before + 1, plt.CurrentFigure().Axes.Count)
        Assert.Contains("<text", plt.ToSvg())
