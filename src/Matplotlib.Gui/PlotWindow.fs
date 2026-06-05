namespace Matplotlib.Gui

open System
open System.Windows.Forms
open Matplotlib.Domain
open Matplotlib.Domain.Primitives

/// <summary>
/// A top-level window that renders a <see cref="Figure"/> with the GDI+ backend,
/// the on-screen counterpart of Matplotlib's interactive figure window.
/// </summary>
/// <remarks>
/// Repaints the figure on every paint and on resize, feeding the renderer the
/// current client size so the figure re-lays-out to fill the window (Axes are
/// positioned as a fraction of the canvas).
/// </remarks>
type PlotWindow(figure: Figure) as this =
    inherit Form()

    do
        let px = figure.PixelSize
        this.Text <- "Figure 1 — dotnet-matplotlib"
        this.ClientSize <- System.Drawing.Size(int (Math.Round px.Width), int (Math.Round px.Height))
        this.BackColor <- System.Drawing.Color.White
        this.StartPosition <- FormStartPosition.CenterScreen
        this.DoubleBuffered <- true
        this.MinimumSize <- System.Drawing.Size(160, 120)

    /// <summary>The figure background fills the surface, so skip the default clear.</summary>
    override _.OnPaintBackground(_e: PaintEventArgs) = ()

    override this.OnPaint(e: PaintEventArgs) =
        let cs = this.ClientSize

        let sizePx: Size =
            {
                Width = float cs.Width
                Height = float cs.Height
            }

        let renderer = GdiRenderer(e.Graphics, sizePx, figure.Dpi)
        figure.Draw renderer

    override this.OnResize(e: EventArgs) =
        base.OnResize e
        this.Invalidate()
