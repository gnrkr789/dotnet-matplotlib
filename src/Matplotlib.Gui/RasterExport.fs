namespace Matplotlib.Gui

open System.Drawing
open System.Drawing.Imaging
open Matplotlib.Domain
// Opened last so its Size shadows System.Drawing.Size.
open Matplotlib.Domain.Primitives

/// <summary>
/// Raster (bitmap) export of a <see cref="Figure"/> via the GDI+ backend — the
/// opt-in, Windows-only counterpart of the SVG file writer.
/// </summary>
/// <remarks>
/// The Agg-equivalent raster path of Matplotlib's <c>FigureCanvasAgg.print_png</c>.
/// Reuses <see cref="GdiRenderer"/> to draw onto an in-memory bitmap, so it
/// renders exactly what the interactive window shows (paths, fills and text).
/// </remarks>
[<RequireQualifiedAccess>]
module Raster =

    /// <summary>Render a figure to a 32-bit ARGB bitmap at its pixel size.</summary>
    let toBitmap (figure: Figure) : Bitmap =
        let px = figure.PixelSize
        let w = max 1 (int (System.Math.Round px.Width))
        let h = max 1 (int (System.Math.Round px.Height))
        let bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb)
        bmp.SetResolution(float32 figure.Dpi, float32 figure.Dpi)
        use g = Graphics.FromImage bmp

        let sizePx: Size = { Width = float w; Height = float h }

        let renderer = GdiRenderer(g, sizePx, figure.Dpi)
        figure.Draw renderer
        bmp

    /// <summary>Render a figure and save it as a PNG file (creating directories).</summary>
    let savePng (path: string) (figure: Figure) : unit =
        let directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath path)

        if
            not (System.String.IsNullOrEmpty directory)
            && not (System.IO.Directory.Exists directory)
        then
            System.IO.Directory.CreateDirectory directory |> ignore

        use bmp = toBitmap figure
        bmp.Save(path, ImageFormat.Png)
