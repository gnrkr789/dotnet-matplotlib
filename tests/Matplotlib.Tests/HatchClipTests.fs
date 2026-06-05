namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Artists

module HatchClipTests =

    [<Fact>]
    let ``Hatch fills a square with clipped parallel lines`` () =
        let square =
            [|
                { X = 0.0; Y = 0.0 }
                { X = 10.0; Y = 0.0 }
                { X = 10.0; Y = 10.0 }
                { X = 0.0; Y = 10.0 }
            |]

        let segs = Hatching.segments square "/" 2.0
        Assert.NotEmpty segs
        // every segment endpoint lies within the square (clipped to the outline)
        for (a, b) in segs do
            for p in [ a; b ] do
                Assert.InRange(p.X, -0.001, 10.001)
                Assert.InRange(p.Y, -0.001, 10.001)

    [<Fact>]
    let ``Cross hatch produces more lines than a single direction`` () =
        let square =
            [|
                { X = 0.0; Y = 0.0 }
                { X = 10.0; Y = 0.0 }
                { X = 10.0; Y = 10.0 }
                { X = 0.0; Y = 10.0 }
            |]

        let single = Hatching.segments square "/" 2.0
        let cross = Hatching.segments square "x" 2.0
        Assert.True(cross.Length > single.Length)

    [<Fact>]
    let ``Unknown hatch characters yield no segments`` () =
        let tri = [| { X = 0.0; Y = 0.0 }; { X = 4.0; Y = 0.0 }; { X = 0.0; Y = 4.0 } |]
        Assert.Empty(Hatching.segments tri "o" 1.0)

    [<Fact>]
    let ``A hatched bar renders extra strokes into the SVG`` () =
        let svg (hatch: string option) =
            let plt = Pyplot()
            plt.Bar([| 0.0; 1.0; 2.0 |], [| 3.0; 5.0; 2.0 |], ?hatch = hatch) |> ignore
            plt.ToSvg()

        Assert.True((svg (Some "x")).Length > (svg None).Length)
