namespace Matplotlib.Tests

open Xunit
open Microsoft.Data.Analysis
open Matplotlib.DataFrame

module DataFrameTests =

    let private numCol (name: string) (vals: float[]) = PrimitiveDataFrameColumn<float>(name, vals) :> DataFrameColumn

    let private strCol (name: string) (vals: string[]) = StringDataFrameColumn(name, vals) :> DataFrameColumn

    [<Fact>]
    let ``PlotLine renders a line from two columns`` () =
        let df = DataFrame([| numCol "x" [| 0.0; 1.0; 2.0; 3.0 |]; numCol "y" [| 0.0; 1.0; 4.0; 9.0 |] |])
        let svg = df.PlotLine("x", "y").ToSvg()
        Assert.Contains("<path", svg)

    [<Fact>]
    let ``PlotScatter renders points and honors color`` () =
        let df = DataFrame([| numCol "a" [| 0.0; 1.0; 2.0 |]; numCol "b" [| 1.0; 0.0; 2.0 |] |])
        let svg = df.PlotScatter("a", "b", "C1").ToSvg()
        Assert.Contains("<path", svg)

    [<Fact>]
    let ``PlotBar renders categorical bars`` () =
        let df = DataFrame([| strCol "cat" [| "a"; "b"; "c" |]; numCol "v" [| 3.0; 5.0; 2.0 |] |])
        let svg = df.PlotBar("cat", "v").ToSvg()
        Assert.Contains("<", svg)

    [<Fact>]
    let ``PlotHist bins a numeric column and autoscales`` () =
        let df = DataFrame([| numCol "vals" [| 1.0; 2.0; 2.0; 3.0; 3.0; 3.0; 4.0; 5.0 |] |])
        let plt = df.PlotHist("vals", 4)
        Assert.Contains("<", plt.ToSvg())
        Assert.True(plt.CurrentAxes().YLim.Upper >= 3.0)
