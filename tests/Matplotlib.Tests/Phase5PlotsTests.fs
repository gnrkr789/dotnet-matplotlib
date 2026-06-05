namespace Matplotlib.Tests

open Xunit
open Matplotlib

module Phase5PlotsTests =

    [<Fact>]
    let ``quiver renders arrows`` () =
        let plt = Pyplot()
        plt.Quiver([| 0.0; 1.0 |], [| 0.0; 0.0 |], [| 1.0; 0.0 |], [| 0.0; 1.0 |])
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``hist2d produces an image covering the data range`` () =
        let plt = Pyplot()
        let xs = [| 0.0; 1.0; 2.0; 3.0; 1.0; 2.0 |]
        let ys = [| 0.0; 1.0; 2.0; 3.0; 2.0; 1.0 |]
        plt.Hist2d(xs, ys, bins = 4) |> ignore
        let ax = plt.CurrentAxes()
        assertClose 0.0 ax.XLim.Lower
        assertClose 3.0 ax.XLim.Upper
        Assert.Contains("<", plt.ToSvg())

    [<Fact>]
    let ``boxplot autoscale includes an outlier`` () =
        let plt = Pyplot()
        plt.Boxplot([| [| 1.0; 2.0; 3.0; 4.0; 5.0; 100.0 |]; [| 2.0; 3.0; 4.0 |] |])
        Assert.Contains("<path", plt.ToSvg())
        Assert.True(plt.CurrentAxes().YLim.Upper >= 100.0)

    [<Fact>]
    let ``violinplot renders a density polygon`` () =
        let plt = Pyplot()
        plt.Violinplot([| [| 1.0; 2.0; 2.0; 3.0; 3.0; 3.0; 4.0; 5.0 |] |])
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``streamplot traces streamlines through a uniform field`` () =
        let plt = Pyplot()
        let x = [| 0.0; 1.0; 2.0; 3.0 |]
        let y = [| 0.0; 1.0; 2.0; 3.0 |]
        let u = Array2D.init 4 4 (fun _ _ -> 1.0)
        let v = Array2D.init 4 4 (fun _ _ -> 0.0)
        plt.Streamplot(x, y, u, v, density = 1)
        Assert.Contains("<path", plt.ToSvg())
