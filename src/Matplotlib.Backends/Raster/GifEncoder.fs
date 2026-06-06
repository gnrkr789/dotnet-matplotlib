namespace Matplotlib.Backends.Raster

open System.Collections.Generic

/// <summary>
/// A pure-managed animated GIF89a encoder with LZW compression and a fixed
/// 8-8-4 RGB palette (256 colors). Zero native dependencies.
/// </summary>
/// <remarks>
/// Used by the animation API to turn rendered raster frames into a looping GIF
/// (Matplotlib writes GIFs via Pillow/ImageMagick; here the bytes are produced
/// directly). Frames are quantized to a uniform color cube.
/// </remarks>
[<RequireQualifiedAccess>]
module GifEncoder =

    /// <summary>The fixed 8x8x4 RGB palette (256 entries, 3 bytes each).</summary>
    let private palette =
        let p = Array.zeroCreate<byte> (256 * 3)

        for ri in 0..7 do
            for gi in 0..7 do
                for bi in 0..3 do
                    let idx = ri * 32 + gi * 4 + bi
                    p[idx * 3] <- byte (ri * 255 / 7)
                    p[idx * 3 + 1] <- byte (gi * 255 / 7)
                    p[idx * 3 + 2] <- byte (bi * 255 / 3)

        p

    /// <summary>Quantize an RGBA buffer to palette indices (alpha ignored).</summary>
    let private quantize (rgba: byte[]) : byte[] =
        let n = rgba.Length / 4
        let idx = Array.zeroCreate<byte> n

        for i in 0 .. n - 1 do
            let r = int rgba[i * 4]
            let g = int rgba[i * 4 + 1]
            let b = int rgba[i * 4 + 2]
            idx[i] <- byte ((r * 7 / 255) * 32 + (g * 7 / 255) * 4 + (b * 3 / 255))

        idx

    /// <summary>GIF variable-width LZW compression of one frame's indices.</summary>
    let private lzw (indices: byte[]) : byte[] =
        let minCodeSize = 8
        let clearCode = 1 <<< minCodeSize
        let endCode = clearCode + 1
        let out = ResizeArray<byte>()
        let mutable bitBuffer = 0
        let mutable bitCount = 0

        let emit (code: int) (width: int) =
            bitBuffer <- bitBuffer ||| (code <<< bitCount)
            bitCount <- bitCount + width

            while bitCount >= 8 do
                out.Add(byte (bitBuffer &&& 0xFF))
                bitBuffer <- bitBuffer >>> 8
                bitCount <- bitCount - 8

        let dict = Dictionary<int, int>()
        let mutable codeWidth = minCodeSize + 1
        let mutable nextCode = endCode + 1

        emit clearCode codeWidth

        if indices.Length > 0 then
            let mutable cur = int indices[0]

            for i in 1 .. indices.Length - 1 do
                let c = int indices[i]
                let key = (cur <<< 8) ||| c

                match dict.TryGetValue key with
                | true, code -> cur <- code
                | _ ->
                    emit cur codeWidth

                    if nextCode = 4096 then
                        // dictionary full: reset (decoders mirror this on the Clear code)
                        emit clearCode codeWidth
                        dict.Clear()
                        nextCode <- endCode + 1
                        codeWidth <- minCodeSize + 1
                    else
                        // widen BEFORE assigning the boundary code, to stay in lockstep
                        // with the decoder (GIF/omggif convention)
                        if nextCode >= (1 <<< codeWidth) && codeWidth < 12 then
                            codeWidth <- codeWidth + 1

                        dict[key] <- nextCode
                        nextCode <- nextCode + 1

                    cur <- c

            emit cur codeWidth

        emit endCode codeWidth

        if bitCount > 0 then
            out.Add(byte (bitBuffer &&& 0xFF))

        out.ToArray()

    let private le16 (v: int) = [| byte (v &&& 0xFF); byte ((v >>> 8) &&& 0xFF) |]

    /// <summary>Append LZW data as GIF sub-blocks (≤255 bytes each, 0-terminated).</summary>
    let private writeSubBlocks (out: ResizeArray<byte>) (data: byte[]) =
        let mutable pos = 0

        while pos < data.Length do
            let len = min 255 (data.Length - pos)
            out.Add(byte len)
            out.AddRange(data[pos .. pos + len - 1])
            pos <- pos + len

        out.Add 0uy

    /// <summary>
    /// Encode RGBA frames (each <c>w*h*4</c> bytes, top-row first) into a looping
    /// animated GIF. <paramref name="delayCs"/> is the per-frame delay in 1/100 s.
    /// </summary>
    let encode (w: int) (h: int) (frames: byte[] list) (delayCs: int) : byte[] =
        let out = ResizeArray<byte>()
        out.AddRange("GIF89a"B)
        // logical screen descriptor
        out.AddRange(le16 w)
        out.AddRange(le16 h)
        out.Add 0xF7uy // global color table, 256 entries (2^(7+1))
        out.Add 0uy // background color index
        out.Add 0uy // pixel aspect ratio
        out.AddRange palette
        // NETSCAPE2.0 looping extension (loop forever)
        out.AddRange [| 0x21uy; 0xFFuy; 0x0Buy |]
        out.AddRange("NETSCAPE2.0"B)
        out.AddRange [| 0x03uy; 0x01uy; 0x00uy; 0x00uy; 0x00uy |]

        for frame in frames do
            // graphic control extension (delay)
            out.AddRange [| 0x21uy; 0xF9uy; 0x04uy; 0x00uy |]
            out.AddRange(le16 (max 2 delayCs))
            out.AddRange [| 0x00uy; 0x00uy |]
            // image descriptor
            out.Add 0x2Cuy
            out.AddRange(le16 0)
            out.AddRange(le16 0)
            out.AddRange(le16 w)
            out.AddRange(le16 h)
            out.Add 0uy // no local color table
            // LZW image data
            out.Add 8uy // minimum code size
            writeSubBlocks out (lzw (quantize frame))

        out.Add 0x3Buy // trailer
        out.ToArray()
