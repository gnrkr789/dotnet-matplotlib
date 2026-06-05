namespace Matplotlib.Domain.Primitives

/// <summary>
/// An axis-aligned bounding box defined by two corner points
/// <c>(X0, Y0)</c> and <c>(X1, Y1)</c>.
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.transforms.Bbox</c>. As in Matplotlib the box may
/// be "un-sorted": <c>X0 &gt; X1</c> or <c>Y0 &gt; Y1</c> represents an inverted
/// axis, so <c>Width</c>/<c>Height</c> are signed while <c>XMin</c>/<c>XMax</c>
/// are orientation independent.
/// </remarks>
[<Struct>]
type BBox =
    {
        X0: float
        Y0: float
        X1: float
        Y1: float
    }

    member this.P0: Point2D = { X = this.X0; Y = this.Y0 }

    member this.P1: Point2D = { X = this.X1; Y = this.Y1 }

    /// <summary>Signed width <c>X1 - X0</c>.</summary>
    member this.Width = this.X1 - this.X0

    /// <summary>Signed height <c>Y1 - Y0</c>.</summary>
    member this.Height = this.Y1 - this.Y0

    member this.XMin = min this.X0 this.X1

    member this.XMax = max this.X0 this.X1

    member this.YMin = min this.Y0 this.Y1

    member this.YMax = max this.Y0 this.Y1

    member this.CenterX = 0.5 * (this.X0 + this.X1)

    member this.CenterY = 0.5 * (this.Y0 + this.Y1)

    /// <summary>True if the point lies within the (orientation-normalized) box.</summary>
    member this.Contains(p: Point2D) = p.X >= this.XMin && p.X <= this.XMax && p.Y >= this.YMin && p.Y <= this.YMax

    /// <summary>Smallest box containing both this and <paramref name="other"/>.</summary>
    member this.Union(other: BBox) =
        {
            X0 = min this.XMin other.XMin
            Y0 = min this.YMin other.YMin
            X1 = max this.XMax other.XMax
            Y1 = max this.YMax other.YMax
        }

    override this.ToString() = $"BBox[({this.X0}, {this.Y0}) -> ({this.X1}, {this.Y1})]"

/// <summary>Helpers for constructing <see cref="BBox"/> values.</summary>
[<RequireQualifiedAccess>]
module BBox =

    /// <summary>The unit square <c>[[0,0],[1,1]]</c>.</summary>
    let unit: BBox =
        {
            X0 = 0.0
            Y0 = 0.0
            X1 = 1.0
            Y1 = 1.0
        }

    /// <summary>Construct from two arbitrary corner points.</summary>
    let fromExtents (x0: float) (y0: float) (x1: float) (y1: float) : BBox = { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1 }

    /// <summary>Construct from a lower-left corner plus (signed) width and height.</summary>
    let fromBounds (x0: float) (y0: float) (width: float) (height: float) : BBox =
        {
            X0 = x0
            Y0 = y0
            X1 = x0 + width
            Y1 = y0 + height
        }

    /// <summary>Grow the box on every side by the given fraction of its size.</summary>
    let expanded (padX: float) (padY: float) (box: BBox) : BBox =
        let dx = padX * box.Width
        let dy = padY * box.Height

        {
            X0 = box.X0 - dx
            Y0 = box.Y0 - dy
            X1 = box.X1 + dx
            Y1 = box.Y1 + dy
        }
