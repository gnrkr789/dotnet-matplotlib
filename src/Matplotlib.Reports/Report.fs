namespace Matplotlib.Reports

open System
open System.IO
open System.Security.Cryptography
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Rendering
open Matplotlib.Backends

/// <summary>
/// A composable, multi-panel report built on dotnet-matplotlib. Lays out a grid
/// of titled chart panels under a report title and renders to SVG / PNG / PDF.
/// </summary>
/// <remarks>
/// Designed for server-side and regulated/cloud-native use: it is pure-managed
/// (no native dependencies, so it runs in AWS Lambda / Azure Functions / Linux
/// containers / Native AOT where native graphics stacks fail) and its vector
/// output is <b>deterministic</b> — the same input renders byte-for-byte
/// identical bytes, so a report can be checksummed for audit/compliance
/// (<see cref="Sha256"/>). Fluent and usable from C# and F#.
/// </remarks>
type Report(title: string) =

    let panels = ResizeArray<string * (Axes -> unit)>()

    /// <summary>The report title.</summary>
    member _.Title = title

    /// <summary>Add a panel drawn by a custom action on its <see cref="Axes"/>.</summary>
    member this.AddChart(panelTitle: string, draw: Action<Axes>) : Report =
        panels.Add(panelTitle, (fun ax -> draw.Invoke ax))
        this

    /// <summary>Add a line-chart panel.</summary>
    member this.AddLine(panelTitle: string, xs: float[], ys: float[]) : Report =
        this.AddChart(panelTitle, Action<Axes>(fun ax -> ax.Plot(xs, ys) |> ignore))

    /// <summary>Add a scatter panel.</summary>
    member this.AddScatter(panelTitle: string, xs: float[], ys: float[]) : Report =
        this.AddChart(panelTitle, Action<Axes>(fun ax -> ax.Scatter(xs, ys) |> ignore))

    /// <summary>Add a bar-chart panel over named categories.</summary>
    member this.AddBar(panelTitle: string, categories: string[], values: float[]) : Report =
        this.AddChart(
            panelTitle,
            Action<Axes>(fun ax ->
                ax.Bar(Array.init categories.Length float, values) |> ignore
                ax.SetXCategories categories)
        )

    /// <summary>Compose the report into a single <see cref="Figure"/>.</summary>
    member _.RenderFigure() : Figure =
        let n = panels.Count
        let cols = max 1 (int (ceil (sqrt (float (max 1 n)))))
        let rows = max 1 (int (ceil (float (max 1 n) / float cols)))
        let fig = Figure()

        fig.SizeInches <-
            {
                Width = float cols * 4.6
                Height = float rows * 3.3 + 0.7
            }

        let grid = fig.Subplots(rows, cols)
        let mutable i = 0

        for r in 0 .. rows - 1 do
            for c in 0 .. cols - 1 do
                let ax = grid[r, c]

                if i < n then
                    let panelTitle, draw = panels[i]
                    draw ax

                    if not (String.IsNullOrEmpty panelTitle) then
                        ax.SetTitle panelTitle
                else
                    ax.SetAxisOff() // hide unused cells

                i <- i + 1

        // report title as a banner across the top margin
        if not (String.IsNullOrEmpty title) then
            let banner = fig.AddAxes(BBox.fromExtents 0.0 0.94 1.0 1.0)
            banner.SetAxisOff()
            banner.SetXLim(0.0, 1.0)
            banner.SetYLim(0.0, 1.0)
            banner.Text(0.5, 0.5, title, fontSize = 16.0, hAlign = HCenter, vAlign = VCenter)
            |> ignore

        fig

    /// <summary>Render the report to an SVG document.</summary>
    member this.RenderSvg() : string = FigureCanvas(this.RenderFigure()).RenderToSvg()

    /// <summary>Render the report to PDF bytes (deterministic vector output).</summary>
    member this.RenderPdf() : byte[] = FigureCanvas(this.RenderFigure()).RenderToPdf()

    /// <summary>Render the report to PNG bytes.</summary>
    member this.RenderPng() : byte[] = FigureCanvas(this.RenderFigure()).RenderToPng()

    /// <summary>Save the report, choosing the format from the file extension (.svg/.png/.pdf).</summary>
    member this.Save(path: string) : unit =
        let canvas = FigureCanvas(this.RenderFigure())
        let ext (e: string) = path.EndsWith(e, StringComparison.OrdinalIgnoreCase)

        if ext ".pdf" then canvas.SavePdf path
        elif ext ".png" then canvas.SavePng path
        else canvas.SaveSvg path

    /// <summary>
    /// SHA-256 (lowercase hex) of the deterministic PDF rendering — a stable
    /// fingerprint of the report for audit trails and change detection.
    /// </summary>
    member this.Sha256() : string =
        use sha = SHA256.Create()
        sha.ComputeHash(this.RenderPdf()) |> Array.map (fun b -> b.ToString("x2")) |> String.concat ""
