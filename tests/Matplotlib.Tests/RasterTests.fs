namespace Matplotlib.Tests

open System.IO
open System.IO.Compression
open System.Text
open Xunit
open Matplotlib
open Matplotlib.Domain.Primitives
open Matplotlib.Backends
open Matplotlib.Backends.Raster

module RasterTests =

    let private pngSignature = [| 137uy; 80uy; 78uy; 71uy; 13uy; 10uy; 26uy; 10uy |]

    let private readBE32 (b: byte[]) (o: int) =
        (int b[o] <<< 24)
        ||| (int b[o + 1] <<< 16)
        ||| (int b[o + 2] <<< 8)
        ||| int b[o + 3]

    /// Minimal PNG reader (8-bit RGBA, filter 0) used to verify the encoder.
    let private decodePng (png: byte[]) : int * int * byte[] =
        let w = readBE32 png 16
        let h = readBE32 png 20
        let idat = ResizeArray<byte>()
        let mutable pos = 8
        let mutable stop = false

        while not stop && pos + 8 <= png.Length do
            let len = readBE32 png pos
            let typ = Encoding.ASCII.GetString(png, pos + 4, 4)
            let dataStart = pos + 8

            if typ = "IDAT" then
                idat.AddRange(png[dataStart .. dataStart + len - 1])

            if typ = "IEND" then
                stop <- true

            pos <- dataStart + len + 4

        use ms = new MemoryStream(idat.ToArray())
        use z = new ZLibStream(ms, CompressionMode.Decompress)
        use outMs = new MemoryStream()
        z.CopyTo outMs
        let raw = outMs.ToArray()
        let stride = w * 4
        let rgba = Array.zeroCreate<byte> (stride * h)

        for y in 0 .. h - 1 do
            // skip the per-row "None" filter byte
            Array.blit raw (y * (stride + 1) + 1) rgba (y * stride) stride

        (w, h, rgba)

    [<Fact>]
    let ``PngEncoder produces a valid signature and round-trips pixels`` () =
        // 2x2: red, green / blue, semi-transparent white
        let src =
            [|
                255uy
                0uy
                0uy
                255uy
                0uy
                255uy
                0uy
                255uy
                0uy
                0uy
                255uy
                255uy
                255uy
                255uy
                255uy
                128uy
            |]

        let png = PngEncoder.encode 2 2 src
        Assert.Equal<byte[]>(pngSignature, png[0..7])
        let (w, h, rgba) = decodePng png
        Assert.Equal(2, w)
        Assert.Equal(2, h)
        Assert.Equal<byte[]>(src, rgba)

    [<Fact>]
    let ``FillPolygon fills the interior and leaves the outside clear`` () =
        let img = RasterImage(10, 10)
        img.FillPolygon([| (2.0, 2.0); (8.0, 2.0); (8.0, 8.0); (2.0, 8.0) |], Color.rgb 1.0 0.0 0.0)
        let at x y = (y * 10 + x) * 4
        // interior is opaque red
        Assert.Equal(255uy, img.Data[at 5 5])
        Assert.Equal(0uy, img.Data[at 5 5 + 1])
        Assert.Equal(255uy, img.Data[at 5 5 + 3])
        // a far corner is untouched (transparent)
        Assert.Equal(0uy, img.Data[at 0 0 + 3])

    [<Fact>]
    let ``Downsample box-averages the supersampled pixels`` () =
        let img = RasterImage(2, 2)
        // one of four sub-pixels opaque red
        img.SetOver(0, 0, 255uy, 0uy, 0uy, 255uy)
        let small = img.Downsample 2
        Assert.Equal(1, small.Width)
        Assert.Equal(1, small.Height)
        Assert.Equal(byte (255 / 4), small.Data[0])
        Assert.Equal(byte (255 / 4), small.Data[3])

    [<Fact>]
    let ``FigureCanvas renders a figure to a valid PNG`` () =
        let plt = Pyplot()

        plt.Plot([| 0.0; 1.0; 2.0; 3.0 |], [| 0.0; 1.0; 4.0; 9.0 |], color = "C0")
        |> ignore

        let png = FigureCanvas(plt.CurrentFigure()).RenderToPng(scale = 2)
        Assert.Equal<byte[]>(pngSignature, png[0..7])
        let (w, h, rgba) = decodePng png
        Assert.Equal(640, w)
        Assert.Equal(480, h)
        // the top-left corner is the white figure background
        Assert.Equal(255uy, rgba[0])
        Assert.Equal(255uy, rgba[3])
        // and the plotted line means not every pixel is white
        let hasInk =
            seq { 0 .. (w * h - 1) }
            |> Seq.exists (fun p -> rgba[p * 4] <> 255uy || rgba[p * 4 + 1] <> 255uy || rgba[p * 4 + 2] <> 255uy)

        Assert.True hasInk
