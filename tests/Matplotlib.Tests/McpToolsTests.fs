namespace Matplotlib.Tests

open System.IO
open Xunit
open Matplotlib.Mcp

module McpToolsTests =

    [<Fact>]
    let ``plot_line tool writes a PNG and reports the path`` () =
        let path = Path.Combine(Path.GetTempPath(), "mpltest_mcp_line.png")

        if File.Exists path then
            File.Delete path

        let result = PlotTools.PlotLine([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |], path, "t", "x", "y", "C0")
        Assert.Contains("Saved plot to", result)
        Assert.True(File.Exists path)
        let header = (File.ReadAllBytes path)[0..3]
        Assert.Equal<byte[]>([| 137uy; 80uy; 78uy; 71uy |], header) // PNG signature
        File.Delete path

    [<Fact>]
    let ``bar tool writes an SVG`` () =
        let path = Path.Combine(Path.GetTempPath(), "mpltest_mcp_bar.svg")
        PlotTools.Bar([| "a"; "b"; "c" |], [| 1.0; 2.0; 3.0 |], path, "bars") |> ignore
        Assert.True(File.Exists path)
        Assert.Contains("<svg", File.ReadAllText path)
        File.Delete path

    [<Fact>]
    let ``heatmap tool writes a file`` () =
        let path = Path.Combine(Path.GetTempPath(), "mpltest_mcp_heat.svg")
        let data = [| [| 0.0; 1.0 |]; [| 2.0; 3.0 |] |]
        PlotTools.Heatmap(data, path, "viridis", "h") |> ignore
        Assert.True(File.Exists path)
        File.Delete path
