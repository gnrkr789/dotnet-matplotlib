namespace Matplotlib.Tests

open Xunit
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Transforms

module TransformTests =

    [<Fact>]
    let ``Affine translation then scale composes correctly`` () =
        let translate = Affine2D.translation 1.0 2.0
        let scale = Affine2D.scaling 2.0 3.0
        // apply translate first, then scale
        let combined = translate.AndThen scale :> ITransform
        let p = combined.Transform { X = 1.0; Y = 1.0 }
        // (1,1) -> translate (2,3) -> scale (4,9)
        assertClose 4.0 p.X
        assertClose 9.0 p.Y

    [<Fact>]
    let ``Affine rotation by 90 degrees maps x axis to y axis`` () =
        let r = Affine2D.rotationDegrees 90.0 :> ITransform
        let p = r.Transform { X = 1.0; Y = 0.0 }
        assertCloseTol 1e-9 0.0 p.X
        assertCloseTol 1e-9 1.0 p.Y

    [<Fact>]
    let ``Affine inverse round-trips`` () =
        let a = Affine2D(2.0, 0.5, -0.3, 1.5, 4.0, -2.0)
        let inv = a.InvertedAffine()
        let combined = (a :> ITransform)
        let p = { X = 3.0; Y = -1.0 }
        let back = (inv :> ITransform).Transform(combined.Transform p)
        assertClose p.X back.X
        assertClose p.Y back.Y

    [<Fact>]
    let ``BBoxTransform maps unit square onto target box`` () =
        let t = BBoxTransform(BBox.unit, BBox.fromExtents 0.0 0.0 100.0 200.0) :> ITransform
        let mid = t.Transform { X = 0.5; Y = 0.5 }
        assertClose 50.0 mid.X
        assertClose 100.0 mid.Y
        let corner = t.Transform { X = 1.0; Y = 1.0 }
        assertClose 100.0 corner.X
        assertClose 200.0 corner.Y

    [<Fact>]
    let ``Data to display pipeline places points in axes box`` () =
        // axes occupying pixels [80,440]x[60,360] (y-up), data limits [0,10]x[0,100]
        let axesBox = BBox.fromExtents 80.0 60.0 440.0 360.0
        let dataBox = BBox.fromExtents 0.0 0.0 10.0 100.0

        let transData =
            Transforms.compose
                (BBoxTransform(dataBox, BBox.unit) :> ITransform)
                (BBoxTransform(BBox.unit, axesBox) :> ITransform)

        let origin = transData.Transform { X = 0.0; Y = 0.0 }
        assertClose 80.0 origin.X
        assertClose 60.0 origin.Y
        let top = transData.Transform { X = 10.0; Y = 100.0 }
        assertClose 440.0 top.X
        assertClose 360.0 top.Y
