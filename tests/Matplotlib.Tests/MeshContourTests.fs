namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain

module MeshContourTests =

    [<Fact>]
    let ``Marching squares finds the iso-line of a ramp`` () =
        // z increases with the row index; the 1.5 contour is a horizontal line at y = 1.5
        let z = array2D [ [ 0.0; 0.0; 0.0 ]; [ 1.0; 1.0; 1.0 ]; [ 2.0; 2.0; 2.0 ] ]
        let segments = AxesLayout.marchingSquares z 1.5
        Assert.Equal(2, segments.Length)

        for (a, b) in segments do
            assertCloseTol 1e-9 1.5 a.Y
            assertCloseTol 1e-9 1.5 b.Y

    [<Fact>]
    let ``Marching squares resolves a saddle with the cell-centre value`` () =
        // A case-5 saddle with a high centre (mean = 1.0 > 0.5): each contour
        // segment must wrap a LOW corner (bottom-right or top-left), not a high one.
        let z = array2D [ [ 2.0; 0.0 ]; [ 0.0; 2.0 ] ]
        let segments = AxesLayout.marchingSquares z 0.5
        Assert.Equal(2, segments.Length)

        let nearLowCorner x y =
            let d ax ay = (x - ax) ** 2.0 + (y - ay) ** 2.0
            // closer to bottom-right (1,0) / top-left (0,1) than to the high corners
            min (d 1.0 0.0) (d 0.0 1.0) < min (d 0.0 0.0) (d 1.0 1.0)

        for (a, b) in segments do
            Assert.True(nearLowCorner ((a.X + b.X) / 2.0) ((a.Y + b.Y) / 2.0))

    [<Fact>]
    let ``Pcolormesh uses origin-lower integer cell edges`` () =
        let plt = Pyplot()
        let data = array2D [ for i in 0..3 -> [ for j in 0..3 -> float (i + j) ] ]
        plt.Pcolormesh data |> ignore
        assertClose 0.0 (plt.CurrentAxes().XLim.Lower)
        assertClose 4.0 (plt.CurrentAxes().XLim.Upper)
        Assert.True((plt.ToSvg().Split("fill=\"#").Length - 1) > 12)

    [<Fact>]
    let ``Contour produces line collections that render`` () =
        let plt = Pyplot()
        let n = 24

        let data =
            Array2D.init n n (fun i j ->
                let x = float j / float n * 6.0
                let y = float i / float n * 6.0
                sin x * cos y)

        plt.Contour data |> ignore
        Assert.True(plt.CurrentAxes().Collections.Count > 0)
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``Contourf fills the field with banded colors`` () =
        let plt = Pyplot()
        let data = array2D [ for i in 0..9 -> [ for j in 0..9 -> float (i + j) ] ]
        let levels = plt.Contourf(data, cmap = "viridis")
        Assert.True(levels.Length > 2)
        Assert.True(plt.CurrentAxes().Patches.Count > 0)
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``Contourf bands follow the contour, not the cell grid`` () =
        let ax = Axes()
        let z = array2D [ [ 0.0; 0.0 ]; [ 2.0; 2.0 ] ] // z rises with the row index
        ax.Contourf(z, levels = [| 0.0; 1.0; 2.0 |]) |> ignore
        // two bands -> two filled polygons whose shared edge is the z = 1 contour at y = 0.5
        Assert.True(ax.Patches.Count >= 2)

        let touchesHalf =
            ax.Patches
            |> Seq.exists (fun p ->
                match p.DataBounds() with
                | Some b -> abs (b.YMin - 0.5) < 1e-9 || abs (b.YMax - 0.5) < 1e-9
                | None -> false)

        Assert.True(touchesHalf, "expected a band boundary interpolated at y = 0.5")
