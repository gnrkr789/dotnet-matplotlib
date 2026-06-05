namespace Matplotlib.Backends.Raster

open System
open Matplotlib.Domain.Primitives

/// <summary>
/// A straight-alpha RGBA raster surface with the primitive drawing operations
/// the raster backend needs: source-over compositing, even-odd polygon fill,
/// thick polyline stroking, disk fill, and box downsampling for anti-aliasing.
/// </summary>
/// <remarks>
/// Anti-aliasing is achieved by rendering into a supersampled surface and
/// box-averaging it down (<see cref="Downsample"/>), so the primitives
/// themselves stay simple and exact. Pixel centres are at <c>(x+0.5, y+0.5)</c>.
/// </remarks>
type RasterImage(width: int, height: int) =
    let data = Array.zeroCreate<byte> (width * height * 4)
    let mutable clipX0 = 0
    let mutable clipY0 = 0
    let mutable clipX1 = width - 1
    let mutable clipY1 = height - 1

    /// <summary>Set the pixel clip rectangle (inclusive); drawing outside is ignored.</summary>
    member _.SetClip(x0: int, y0: int, x1: int, y1: int) =
        clipX0 <- max 0 x0
        clipY0 <- max 0 y0
        clipX1 <- min (width - 1) x1
        clipY1 <- min (height - 1) y1

    /// <summary>Reset the clip to the whole image.</summary>
    member _.ResetClip() =
        clipX0 <- 0
        clipY0 <- 0
        clipX1 <- width - 1
        clipY1 <- height - 1

    /// <summary>Image width in pixels.</summary>
    member _.Width = width

    /// <summary>Image height in pixels.</summary>
    member _.Height = height

    /// <summary>The row-major, top-row-first RGBA buffer (<c>w*h*4</c> bytes).</summary>
    member _.Data = data

    /// <summary>Source-over composite of a straight-alpha colour onto one pixel.</summary>
    member _.SetOver(x: int, y: int, r: byte, g: byte, b: byte, a: byte) =
        if x >= clipX0 && x <= clipX1 && y >= clipY0 && y <= clipY1 && a > 0uy then
            let i = (y * width + x) * 4

            if a = 255uy then
                data[i] <- r
                data[i + 1] <- g
                data[i + 2] <- b
                data[i + 3] <- 255uy
            else
                let sa = float a / 255.0
                let da = float data[i + 3] / 255.0
                let inv = 1.0 - sa
                let outA = sa + da * inv

                if outA > 0.0 then
                    let comp (dst: byte) (src: byte) = byte (Math.Round((float src * sa + float dst * da * inv) / outA))

                    data[i] <- comp data[i] r
                    data[i + 1] <- comp data[i + 1] g
                    data[i + 2] <- comp data[i + 2] b
                    data[i + 3] <- byte (Math.Round(outA * 255.0))

    /// <summary>Fill a polygon (even-odd rule) with a solid colour.</summary>
    member this.FillPolygon(pts: (float * float)[], color: Color) =
        if pts.Length >= 3 then
            let r, g, b, a = byte color.R255, byte color.G255, byte color.B255, byte color.A255
            let ys = pts |> Array.map snd
            let yMin = max 0 (int (floor (Array.min ys)))
            let yMax = min (height - 1) (int (ceil (Array.max ys)))
            let n = pts.Length
            let xs = ResizeArray<float>()

            for y in yMin..yMax do
                let yc = float y + 0.5
                xs.Clear()

                for i in 0 .. n - 1 do
                    let x0, y0 = pts[i]
                    let x1, y1 = pts[(i + 1) % n]
                    // half-open edge rule avoids double-counting shared vertices
                    if (y0 <= yc && y1 > yc) || (y1 <= yc && y0 > yc) then
                        xs.Add(x0 + (yc - y0) / (y1 - y0) * (x1 - x0))

                xs.Sort()
                let mutable k = 0

                while k + 1 < xs.Count do
                    let xStart = max 0 (int (ceil (xs[k] - 0.5)))
                    let xEnd = min (width - 1) (int (floor (xs[k + 1] - 0.5)))

                    for x in xStart..xEnd do
                        this.SetOver(x, y, r, g, b, a)

                    k <- k + 2

    /// <summary>
    /// Fill several contours together with a single even-odd rule, so inner
    /// contours cut holes (used for glyph outlines such as "o" or "e").
    /// </summary>
    member this.FillPolygons(contours: (float * float)[][], color: Color) =
        let edges = contours |> Array.filter (fun c -> c.Length >= 2)

        if edges.Length > 0 then
            let r, g, b, a = byte color.R255, byte color.G255, byte color.B255, byte color.A255
            let allY = edges |> Array.collect (Array.map snd)
            let yMin = max 0 (int (floor (Array.min allY)))
            let yMax = min (height - 1) (int (ceil (Array.max allY)))
            let xs = ResizeArray<float>()

            for y in yMin..yMax do
                let yc = float y + 0.5
                xs.Clear()

                for contour in edges do
                    let n = contour.Length

                    for i in 0 .. n - 1 do
                        let x0, y0 = contour[i]
                        let x1, y1 = contour[(i + 1) % n]

                        if (y0 <= yc && y1 > yc) || (y1 <= yc && y0 > yc) then
                            xs.Add(x0 + (yc - y0) / (y1 - y0) * (x1 - x0))

                xs.Sort()
                let mutable k = 0

                while k + 1 < xs.Count do
                    let xStart = max 0 (int (ceil (xs[k] - 0.5)))
                    let xEnd = min (width - 1) (int (floor (xs[k + 1] - 0.5)))

                    for x in xStart..xEnd do
                        this.SetOver(x, y, r, g, b, a)

                    k <- k + 2

    /// <summary>Fill a disk (used for round line joins and markers).</summary>
    member this.FillDisk(cx: float, cy: float, radius: float, color: Color) =
        if radius > 0.0 then
            let r, g, b, a = byte color.R255, byte color.G255, byte color.B255, byte color.A255
            let x0 = max 0 (int (floor (cx - radius)))
            let x1 = min (width - 1) (int (ceil (cx + radius)))
            let y0 = max 0 (int (floor (cy - radius)))
            let y1 = min (height - 1) (int (ceil (cy + radius)))
            let rr = radius * radius

            for y in y0..y1 do
                for x in x0..x1 do
                    let dx = float x + 0.5 - cx
                    let dy = float y + 0.5 - cy

                    if dx * dx + dy * dy <= rr then
                        this.SetOver(x, y, r, g, b, a)

    /// <summary>Stroke a polyline with a given width; round joins, butt ends.</summary>
    member this.StrokePolyline(pts: (float * float)[], lineWidth: float, color: Color) =
        let hw = lineWidth / 2.0

        if pts.Length >= 2 && hw > 0.0 then
            for i in 0 .. pts.Length - 2 do
                let x0, y0 = pts[i]
                let x1, y1 = pts[i + 1]
                let dx = x1 - x0
                let dy = y1 - y0
                let len = sqrt (dx * dx + dy * dy)

                if len > 1e-9 then
                    let nx = -dy / len * hw
                    let ny = dx / len * hw

                    this.FillPolygon(
                        [|
                            (x0 + nx, y0 + ny)
                            (x1 + nx, y1 + ny)
                            (x1 - nx, y1 - ny)
                            (x0 - nx, y0 - ny)
                        |],
                        color
                    )

                // round join at interior vertices
                if i > 0 then
                    this.FillDisk(x0, y0, hw, color)

    /// <summary>Box-average down by an integer factor for anti-aliasing.</summary>
    member _.Downsample(scale: int) : RasterImage =
        if scale <= 1 then
            let copy = RasterImage(width, height)
            Array.blit data 0 copy.Data 0 data.Length
            copy
        else
            let ow = width / scale
            let oh = height / scale
            let outImg = RasterImage(ow, oh)
            let outData = outImg.Data
            let area = scale * scale

            for oy in 0 .. oh - 1 do
                for ox in 0 .. ow - 1 do
                    let mutable rs = 0
                    let mutable gs = 0
                    let mutable bs = 0
                    let mutable acc = 0

                    for sy in 0 .. scale - 1 do
                        for sx in 0 .. scale - 1 do
                            let i = (((oy * scale + sy) * width) + (ox * scale + sx)) * 4
                            rs <- rs + int data[i]
                            gs <- gs + int data[i + 1]
                            bs <- bs + int data[i + 2]
                            acc <- acc + int data[i + 3]

                    let o = (oy * ow + ox) * 4
                    outData[o] <- byte (rs / area)
                    outData[o + 1] <- byte (gs / area)
                    outData[o + 2] <- byte (bs / area)
                    outData[o + 3] <- byte (acc / area)

            outImg
