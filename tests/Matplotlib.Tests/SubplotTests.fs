namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain

module SubplotTests =

    [<Fact>]
    let ``Subplots creates a row-major grid of axes`` () =
        let fig = Figure()
        let axes = fig.Subplots(2, 3)
        Assert.Equal(2, Array2D.length1 axes)
        Assert.Equal(3, Array2D.length2 axes)
        Assert.Equal(6, fig.Axes.Count)

    [<Fact>]
    let ``Subplots positions advance left-to-right, top-to-bottom`` () =
        let fig = Figure()
        let axes = fig.Subplots(2, 3)
        // top-left cell anchored at the default subplot left/top
        assertCloseTol 1e-9 fig.Rc.SubplotLeft axes[0, 0].Position.X0
        assertCloseTol 1e-9 fig.Rc.SubplotTop axes[0, 0].Position.Y1
        // columns increase in x
        Assert.True(axes[0, 2].Position.X0 > axes[0, 1].Position.X0)
        Assert.True(axes[0, 1].Position.X0 > axes[0, 0].Position.X0)
        // row 0 is above row 1
        Assert.True(axes[0, 0].Position.Y1 > axes[1, 0].Position.Y1)

    [<Fact>]
    let ``Pyplot subplots sets the first cell as current axes and renders`` () =
        let plt = Pyplot()
        let _, axes = plt.Subplots(nrows = 1, ncols = 2)
        Assert.Equal(2, Array2D.length2 axes)
        axes[0, 0].Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        axes[0, 1].Bar([| 0.0; 1.0 |], [| 1.0; 2.0 |]) |> ignore
        let svg = plt.ToSvg()
        Assert.Contains("<svg", svg)
        Assert.Contains("</svg>", svg)
