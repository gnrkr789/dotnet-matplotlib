namespace Matplotlib.Tests

open Xunit
open Matplotlib.Domain.Primitives

module PrimitiveTests =

    [<Fact>]
    let ``BBox width and height are signed`` () =
        let b = BBox.fromExtents 1.0 2.0 4.0 8.0
        assertClose 3.0 b.Width
        assertClose 6.0 b.Height
        assertClose 1.0 b.XMin
        assertClose 4.0 b.XMax

    [<Fact>]
    let ``Inverted BBox keeps min/max orientation independent`` () =
        let b = BBox.fromExtents 4.0 8.0 1.0 2.0
        assertClose -3.0 b.Width
        assertClose 1.0 b.XMin
        assertClose 4.0 b.XMax

    [<Fact>]
    let ``Interval detects degeneracy`` () =
        let degenerate: Interval = { Lower = 1.0; Upper = 1.0 }
        let proper: Interval = { Lower = 0.0; Upper = 1.0 }
        Assert.True degenerate.IsDegenerate
        Assert.False proper.IsDegenerate

    [<Fact>]
    let ``Point arithmetic`` () =
        let p = { X = 1.0; Y = 2.0 } + { X = 3.0; Y = 4.0 }
        assertClose 4.0 p.X
        assertClose 6.0 p.Y
        let q: Point2D = { X = 3.0; Y = 4.0 }
        assertClose 5.0 q.Length
