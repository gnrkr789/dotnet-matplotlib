namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Reports

module TableTests =

    [<Fact>]
    let ``table renders header and cell text into SVG`` () =
        let plt = Pyplot()
        plt.Table([| [| "a"; "1" |]; [| "b"; "2" |] |], colLabels = [| "name"; "val" |])
        let svg = plt.ToSvg()
        Assert.Contains("name", svg) // header label
        Assert.Contains("<path", svg) // cell rectangles

    [<Fact>]
    let ``report with a table panel renders`` () =
        let report =
            Report("with table")
                .AddLine("trend", [| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |])
                .AddTable("summary", [| "k"; "v" |], [| [| "x"; "10" |]; [| "y"; "20" |] |])

        let svg = report.RenderSvg()
        Assert.Contains("summary", svg)
        Assert.Contains("<path", svg)
