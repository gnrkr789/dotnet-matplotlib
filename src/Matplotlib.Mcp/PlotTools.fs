namespace Matplotlib.Mcp

open System
open System.ComponentModel
open System.IO
open ModelContextProtocol.Server
open Matplotlib

/// <summary>
/// MCP tools that let an AI agent create plots with the matplotlib-style API and
/// save them to a file (PNG / SVG / PDF, chosen by the output extension).
/// </summary>
[<McpServerToolType>]
type PlotTools() =

    static member private save
        (plt: Plt)
        (title: string)
        (xlabel: string)
        (ylabel: string)
        (output: string)
        : string =
        if not (String.IsNullOrWhiteSpace title) then
            plt.Title title

        if not (String.IsNullOrWhiteSpace xlabel) then
            plt.XLabel xlabel

        if not (String.IsNullOrWhiteSpace ylabel) then
            plt.YLabel ylabel

        let path =
            if String.IsNullOrWhiteSpace output then
                "plot.png"
            else
                output

        plt.Savefig path
        $"Saved plot to {Path.GetFullPath path}"

    [<McpServerTool>]
    [<Description("Plot a line chart of y versus x and save it. The image format is chosen by the output extension: .png (raster), .svg or .pdf (vector).")>]
    static member PlotLine
        (
            [<Description("X values")>] x: float[],
            [<Description("Y values, same length as x")>] y: float[],
            [<Description("Output file path, e.g. plot.png")>] output: string,
            [<Description("Chart title (empty for none)")>] title: string,
            [<Description("X axis label (empty for none)")>] xlabel: string,
            [<Description("Y axis label (empty for none)")>] ylabel: string,
            [<Description("Line color such as C0, red or #1f77b4 (empty for default)")>] color: string
        ) : string =
        let plt = Plt()

        if String.IsNullOrWhiteSpace color then
            plt.Plot(x, y) |> ignore
        else
            plt.Plot(x, y, color = color) |> ignore

        PlotTools.save plt title xlabel ylabel output

    [<McpServerTool>]
    [<Description("Draw a scatter plot of the points (x, y) and save it.")>]
    static member Scatter
        (
            [<Description("X values")>] x: float[],
            [<Description("Y values, same length as x")>] y: float[],
            [<Description("Output file path, e.g. scatter.png")>] output: string,
            [<Description("Chart title (empty for none)")>] title: string,
            [<Description("Marker color such as C1 or blue (empty for default)")>] color: string
        ) : string =
        let plt = Plt()

        if String.IsNullOrWhiteSpace color then
            plt.Scatter(x, y) |> ignore
        else
            plt.Scatter(x, y, color = color) |> ignore

        PlotTools.save plt title "" "" output

    [<McpServerTool>]
    [<Description("Draw a vertical bar chart over named categories and save it.")>]
    static member Bar
        (
            [<Description("Category labels")>] labels: string[],
            [<Description("Bar heights, same length as labels")>] values: float[],
            [<Description("Output file path, e.g. bars.png")>] output: string,
            [<Description("Chart title (empty for none)")>] title: string
        ) : string =
        let plt = Plt()
        plt.Bar(labels, values) |> ignore
        PlotTools.save plt title "" "" output

    [<McpServerTool>]
    [<Description("Render a 2D array as a colormapped heatmap (imshow) with a colorbar and save it.")>]
    static member Heatmap
        (
            [<Description("2D data as an array of rows")>] data: float[][],
            [<Description("Output file path, e.g. heatmap.png")>] output: string,
            [<Description("Colormap name: viridis, gray, jet or hot (empty for viridis)")>] cmap: string,
            [<Description("Chart title (empty for none)")>] title: string
        ) : string =
        let rows = if isNull data then 0 else data.Length
        let cols = if rows = 0 then 0 else data[0].Length
        let grid = Array2D.init rows cols (fun i j -> data[i][j])
        let plt = Plt()

        let image =
            if String.IsNullOrWhiteSpace cmap then
                plt.Imshow grid
            else
                plt.Imshow(grid, cmap = cmap)

        plt.Colorbar image |> ignore
        PlotTools.save plt title "" "" output
