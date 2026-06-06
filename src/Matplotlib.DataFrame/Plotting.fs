namespace Matplotlib.DataFrame

open System
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.Data.Analysis
open Matplotlib

/// <summary>
/// Plotting extension methods for <see cref="Microsoft.Data.Analysis.DataFrame"/>,
/// in the spirit of pandas' <c>DataFrame.plot</c>. Each method plots the named
/// column(s) and returns the <see cref="Pyplot"/> so the caller can add a title,
/// save the figure, etc. Usable from C# (extension methods) and F#.
/// </summary>
[<Extension>]
type DataFrameExtensions =

    /// <summary>Read a numeric column as floats (nulls become NaN).</summary>
    static member private floats (df: DataFrame) (name: string) : float[] =
        df.Columns[name]
        |> Seq.cast<obj>
        |> Seq.map (fun v ->
            if isNull v then
                nan
            else
                Convert.ToDouble(v, Globalization.CultureInfo.InvariantCulture))
        |> Seq.toArray

    /// <summary>Read a column as strings (nulls become empty).</summary>
    static member private strings (df: DataFrame) (name: string) : string[] =
        df.Columns[name]
        |> Seq.cast<obj>
        |> Seq.map (fun v -> if isNull v then "" else string v)
        |> Seq.toArray

    /// <summary>Plot column <paramref name="y"/> versus column <paramref name="x"/> as a line.</summary>
    [<Extension>]
    static member PlotLine
        (df: DataFrame, x: string, y: string, [<Optional; DefaultParameterValue("")>] color: string)
        : Pyplot =
        let plt = Pyplot()
        let xs = DataFrameExtensions.floats df x
        let ys = DataFrameExtensions.floats df y

        if String.IsNullOrEmpty color then
            plt.Plot(xs, ys, label = y) |> ignore
        else
            plt.Plot(xs, ys, color = color, label = y) |> ignore

        plt.XLabel x
        plt.YLabel y
        plt

    /// <summary>Scatter column <paramref name="y"/> against column <paramref name="x"/>.</summary>
    [<Extension>]
    static member PlotScatter
        (df: DataFrame, x: string, y: string, [<Optional; DefaultParameterValue("")>] color: string)
        : Pyplot =
        let plt = Pyplot()
        let xs = DataFrameExtensions.floats df x
        let ys = DataFrameExtensions.floats df y

        if String.IsNullOrEmpty color then
            plt.Scatter(xs, ys) |> ignore
        else
            plt.Scatter(xs, ys, color = color) |> ignore

        plt.XLabel x
        plt.YLabel y
        plt

    /// <summary>Bar chart of a value column over a (string) category column.</summary>
    [<Extension>]
    static member PlotBar
        (df: DataFrame, category: string, value: string, [<Optional; DefaultParameterValue("")>] color: string) : Pyplot =
        let plt = Pyplot()
        let cats = DataFrameExtensions.strings df category
        let vals = DataFrameExtensions.floats df value

        if String.IsNullOrEmpty color then
            plt.Bar(cats, vals) |> ignore
        else
            plt.Bar(cats, vals, color = color) |> ignore

        plt.YLabel value
        plt

    /// <summary>Histogram of a numeric column using <paramref name="bins"/> equal-width bins.</summary>
    [<Extension>]
    static member PlotHist(df: DataFrame, column: string, [<Optional; DefaultParameterValue(10)>] bins: int) : Pyplot =
        let plt = Pyplot()
        let data = DataFrameExtensions.floats df column |> Array.filter Double.IsFinite

        if data.Length > 0 && bins > 0 then
            let lo = Array.min data
            let hi = Array.max data
            let w = let raw = (hi - lo) / float bins in if raw <= 0.0 then 1.0 else raw
            let counts = Array.zeroCreate<float> bins

            for v in data do
                let idx = min (bins - 1) (max 0 (int ((v - lo) / w)))
                counts[idx] <- counts[idx] + 1.0

            let centers = Array.init bins (fun i -> lo + (float i + 0.5) * w)
            plt.Bar(centers, counts, width = w) |> ignore

        plt.XLabel column
        plt.YLabel "count"
        plt
