namespace Matplotlib.Tests

open System
open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Ticking

module CategoryDateTests =

    [<Fact>]
    let ``Fixed formatter labels by index`` () =
        let f = FixedFormatter([| "a"; "b"; "c" |]) :> ITickFormatter
        Assert.Equal<string[]>([| "a"; "b"; "c" |], f.FormatTicks [| 0.0; 1.0; 2.0 |])
        Assert.Equal<string[]>([| "b" |], f.FormatTicks [| 1.0 |])

    [<Fact>]
    let ``Labeled-ticks formatter matches arbitrary positions to labels`` () =
        let f = LabeledTicksFormatter([| 0.0; 1.5708; 3.1416 |], [| "0"; "pi/2"; "pi" |]) :> ITickFormatter
        // each label follows its value, independent of order; unknown values are blank
        Assert.Equal<string[]>([| "pi/2"; "0"; "pi" |], f.FormatTicks [| 1.5708; 0.0; 3.1416 |])
        Assert.Equal<string[]>([| ""; "pi" |], f.FormatTicks [| 9.9; 3.1416 |])

    [<Fact>]
    let ``Fixed locator keeps positions within the view`` () =
        let l = FixedLocator([| 0.0; 1.0; 2.0; 3.0 |]) :> ITickLocator
        Assert.Equal<float[]>([| 1.0; 2.0 |], l.TickValues { Lower = 0.5; Upper = 2.5 })

    [<Fact>]
    let ``Date formatter renders OADate numbers as dates`` () =
        let f = DateFormatter("yyyy-MM-dd") :> ITickFormatter
        let oa = DateTime(2020, 1, 15).ToOADate()
        Assert.Equal<string[]>([| "2020-01-15" |], f.FormatTicks [| oa |])

    [<Fact>]
    let ``Set categories fixes positions, labels and limits`` () =
        let ax = Axes()
        ax.Bar([| 0.0; 1.0; 2.0 |], [| 3.0; 1.0; 2.0 |]) |> ignore
        ax.SetXCategories [| "A"; "B"; "C" |]
        assertClose -0.5 ax.XLim.Lower
        assertClose 2.5 ax.XLim.Upper
        Assert.True(ax.XAxis.MajorLocator.IsSome)
        Assert.True(ax.XAxis.MajorFormatter.IsSome)

    [<Fact>]
    let ``Pyplot categorical bar renders category labels`` () =
        let plt = Pyplot()
        plt.Bar([| "red"; "green"; "blue" |], [| 3.0; 7.0; 5.0 |]) |> ignore
        let svg = plt.ToSvg()
        Assert.Contains("green", svg)
        Assert.Contains("<path", svg)

    [<Fact>]
    let ``Pyplot plot_date renders a date axis`` () =
        let plt = Pyplot()
        let dates = [| DateTime(2020, 1, 1); DateTime(2020, 6, 1); DateTime(2021, 1, 1) |]
        plt.PlotDate(dates, [| 1.0; 3.0; 2.0 |]) |> ignore
        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)
        Assert.Contains("<text", svg)
