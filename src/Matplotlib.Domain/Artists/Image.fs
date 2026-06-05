namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Rendering

/// <summary>
/// An image / quad mesh displaying a 2D scalar array, colormapped through a
/// <see cref="Normalize"/> + <see cref="Colormap"/>. Each cell <c>(i, j)</c> is
/// drawn as a filled quad spanning <c>[xEdges[j], xEdges[j+1]] ×
/// [yEdges[i], yEdges[i+1]]</c> in data coordinates.
/// </summary>
/// <remarks>Ported from <c>matplotlib.image.AxesImage</c> / <c>QuadMesh</c>.</remarks>
type AxesImage(data: float[,], colormap: Colormap, norm: Normalize, xEdges: float[], yEdges: float[]) as this =
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
            let gc =
                { GraphicsContext.Default with
                    StrokeColor = Color.none
                    LineWidth = 0.0
                }

            for i in 0 .. this.Rows - 1 do
                for j in 0 .. this.Cols - 1 do
                    let color = this.Colormap.Apply(this.Norm.Normalize data[i, j])

                    let corners =
                        [
                            { X = xEdges[j]; Y = yEdges[i] }
                            { X = xEdges[j + 1]; Y = yEdges[i] }
                            { X = xEdges[j + 1]; Y = yEdges[i + 1] }
                            { X = xEdges[j]; Y = yEdges[i + 1] }
                        ]
                        |> List.map this.Transform.Transform

                    renderer.DrawPath(gc, Path.polygon corners, Some color)
