namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// A single run of text drawn at a position, with alignment and rotation.
/// </summary>
/// <remarks>Ported from <c>matplotlib.text.Text</c> (single-line subset).</remarks>
type Text(x: float, y: float, content: string) as this =
    inherit Artist()

    do this.ZOrder <- 3.0

    /// <summary>X position in the artist's coordinate space.</summary>
    member val X = x with get, set

    /// <summary>Y position in the artist's coordinate space.</summary>
    member val Y = y with get, set

    /// <summary>The text content.</summary>
    member val Content = content with get, set

    /// <summary>Text color.</summary>
    member val Color = Color.black with get, set

    /// <summary>Font properties.</summary>
    member val Font = FontProperties.Default with get, set

    /// <summary>Rotation in degrees (counter-clockwise).</summary>
    member val Rotation = 0.0 with get, set

    /// <summary>Horizontal anchoring of the text relative to its position.</summary>
    member val HAlign = HLeft with get, set

    /// <summary>Vertical anchoring of the text relative to its position.</summary>
    member val VAlign = VBaseline with get, set

    override this.Draw(renderer: IRenderer) =
        if this.Visible && not (System.String.IsNullOrEmpty this.Content) then
            let pos = this.Transform.Transform { X = this.X; Y = this.Y }

            let gc =
                { GraphicsContext.Default with
                    StrokeColor = this.Color
                }

            renderer.DrawText(gc, pos.X, pos.Y, this.Content, this.Font, this.Rotation, this.HAlign, this.VAlign)
