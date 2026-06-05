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
