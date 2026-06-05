namespace Matplotlib.Backends

open System.IO
open Matplotlib.Domain
open Matplotlib.Backends.Svg

/// <summary>
/// Connects a <see cref="Figure"/> to a concrete backend and produces output.
/// </summary>
/// <remarks>Ported from <c>matplotlib.backend_bases.FigureCanvasBase</c> (SVG output).</remarks>
type FigureCanvas(figure: Figure) =

    /// <summary>The figure this canvas renders.</summary>
    member _.Figure = figure

    /// <summary>Render the figure to an SVG document string.</summary>
    member _.RenderToSvg() : string =
        let renderer = SvgRenderer(figure.PixelSize, figure.Dpi)
        figure.Draw renderer
        renderer.GetSvg()

    /// <summary>Render the figure and write it to an SVG file.</summary>
    member this.SaveSvg(path: string) =
        let directory = Path.GetDirectoryName(Path.GetFullPath path)

        if not (Directory.Exists directory) then
            Directory.CreateDirectory directory |> ignore

        File.WriteAllText(path, this.RenderToSvg())
