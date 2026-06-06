namespace Matplotlib.Backends

open System.IO
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Backends.Svg
open Matplotlib.Backends.Raster
open Matplotlib.Backends.Pdf

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

    /// <summary>
    /// Render the figure to a downsampled RGBA buffer with the pure-managed raster
    /// backend. <paramref name="scale"/> is the supersampling factor for
    /// anti-aliasing (default 3). Returns <c>(width, height, rgba)</c>.
    /// </summary>
    member _.RenderToRgba(?scale: int) : int * int * byte[] =
        let s = max 1 (defaultArg scale 3)
        let px = figure.PixelSize
        let w = max 1 (int (System.Math.Round px.Width))
        let h = max 1 (int (System.Math.Round px.Height))
        let surface = RasterImage(w * s, h * s)

        let logical: Size = { Width = float w; Height = float h }

        let renderer = RasterRenderer(surface, logical, figure.Dpi, s)
        figure.Draw renderer
        let final = surface.Downsample s
        (final.Width, final.Height, final.Data)

    /// <summary>Render the figure to PNG bytes with the pure-managed raster backend.</summary>
    member this.RenderToPng(?scale: int) : byte[] =
        let (w, h, rgba) = this.RenderToRgba(?scale = scale)
        PngEncoder.encode w h rgba

    /// <summary>Render the figure and write it to a PNG file (pure-managed).</summary>
    member this.SavePng(path: string, ?scale: int) =
        let directory = Path.GetDirectoryName(Path.GetFullPath path)

        if not (Directory.Exists directory) then
            Directory.CreateDirectory directory |> ignore

        File.WriteAllBytes(path, this.RenderToPng(?scale = scale))

    /// <summary>Render the figure to PDF bytes (pure-managed vector backend).</summary>
    member _.RenderToPdf() : byte[] =
        let renderer = PdfRenderer(figure.PixelSize, figure.Dpi)
        figure.Draw renderer
        renderer.GetPdf()

    /// <summary>Render the figure and write it to a PDF file.</summary>
    member this.SavePdf(path: string) =
        let directory = Path.GetDirectoryName(Path.GetFullPath path)

        if not (Directory.Exists directory) then
            Directory.CreateDirectory directory |> ignore

        File.WriteAllBytes(path, this.RenderToPdf())
