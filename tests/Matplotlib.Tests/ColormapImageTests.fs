namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives

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
        let plt = Pyplot()
        let data = array2D [ for i in 0..7 -> [ for j in 0..7 -> float (i + j) ] ]
        plt.Imshow(data, cmap = "viridis") |> ignore
        let svg = plt.ToSvg()
        // one filled cell per data point (plus backgrounds)
        Assert.True((svg.Split("fill=\"#").Length - 1) > 60)
        // the minimum value maps to viridis[0] = #440154
        Assert.Contains("#440154", svg)
