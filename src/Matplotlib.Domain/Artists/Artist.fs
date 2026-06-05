namespace Matplotlib.Domain.Artists

open Matplotlib.Domain.Transforms
open Matplotlib.Domain.Rendering

/// <summary>
/// Abstract base class for everything that can be drawn onto a renderer.
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.artist.Artist</c>. Holds visibility, draw order
/// (<c>zorder</c>) and the transform mapping the artist's natural coordinates
/// to display pixels.
/// </remarks>
[<AbstractClass>]
type Artist() =

    /// <summary>Whether the artist is drawn.</summary>
    member val Visible = true with get, set

    /// <summary>Draw order; higher values are drawn on top.</summary>
    member val ZOrder = 0.0 with get, set

    /// <summary>The transform mapping the artist's coordinates to display pixels.</summary>
    member val Transform: ITransform = IdentityTransform.Instance :> ITransform with get, set

    /// <summary>Render the artist onto <paramref name="renderer"/>.</summary>
    abstract member Draw: renderer: IRenderer -> unit
