namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Rendering

/// <summary>
/// An image displaying a 2D scalar array, colormapped through a
/// <see cref="Normalize"/> + <see cref="Colormap"/>. Each cell is drawn as a
/// filled rectangle in data coordinates (origin upper: row 0 at the top).
/// </summary>
/// <remarks>Ported from <c>matplotlib.image.AxesImage</c> (rect rasterization).</remarks>
type AxesImage(data: float[,], colormap: Colormap, norm: Normalize) as this =
    inherit Artist()

    do this.ZOrder <- 0.0

    /// <summary>Number of image rows.</summary>
    member _.Rows = Array2D.length1 data

    /// <summary>Number of image columns.</summary>
    member _.Cols = Array2D.length2 data

    /// <summary>The underlying scalar data.</summary>
    member _.Data = data

    /// <summary>The colormap.</summary>
    member val Colormap = colormap with get, set

    /// <summary>The value normalizer.</summary>
    member val Norm = norm with get, set

    override this.Draw(renderer: IRenderer) =
        if this.Visible then
            let rows = this.Rows
            let cols = this.Cols

            let gc =
                { GraphicsContext.Default with
                    StrokeColor = Color.none
                    LineWidth = 0.0
                }

            for i in 0 .. rows - 1 do
                for j in 0 .. cols - 1 do
                    let color = this.Colormap.Apply(this.Norm.Normalize data[i, j])

                    let corners =
                        [
                            { X = float j - 0.5; Y = float i - 0.5 }
                            { X = float j + 0.5; Y = float i - 0.5 }
                            { X = float j + 0.5; Y = float i + 0.5 }
                            { X = float j - 0.5; Y = float i + 0.5 }
                        ]
                        |> List.map this.Transform.Transform

                    renderer.DrawPath(gc, Path.polygon corners, Some color)
