namespace Matplotlib.Backends.Raster

open System
open System.IO
open System.IO.Compression
open System.Text

/// <summary>
/// A pure-managed PNG encoder: 8-bit truecolor + alpha (color type 6), no
/// interlacing, "None" row filter, IDAT compressed with the BCL
/// <see cref="System.IO.Compression.ZLibStream"/>. Zero native dependencies.
/// </summary>
/// <remarks>
/// Implements the subset of the PNG spec needed by the raster backend
/// (Matplotlib renders PNG via Agg + libpng; here the bytes are produced
/// directly). CRC-32 uses the standard PNG polynomial 0xEDB88320.
/// </remarks>
[<RequireQualifiedAccess>]
module PngEncoder =

    let private signature = [| 137uy; 80uy; 78uy; 71uy; 13uy; 10uy; 26uy; 10uy |]

    let private crcTable =
        Array.init 256 (fun n ->
            let mutable c = uint32 n

            for _ in 0..7 do
                c <-
                    if c &&& 1u <> 0u then
                        0xEDB88320u ^^^ (c >>> 1)
                    else
                        c >>> 1

            c)

    let private crc32 (bytes: byte[]) : uint32 =
        let mutable c = 0xFFFFFFFFu

        for b in bytes do
            c <- crcTable[int ((c ^^^ uint32 b) &&& 0xFFu)] ^^^ (c >>> 8)

        c ^^^ 0xFFFFFFFFu

    /// <summary>Big-endian 4-byte encoding of an unsigned 32-bit value.</summary>
    let private be32 (v: uint32) : byte[] = [| byte (v >>> 24); byte (v >>> 16); byte (v >>> 8); byte v |]

    /// <summary>A length-prefixed, CRC-suffixed PNG chunk.</summary>
    let private chunk (name: string) (data: byte[]) : byte[] =
        let typeBytes = Encoding.ASCII.GetBytes name
        let crc = crc32 (Array.append typeBytes data)
        Array.concat [ be32 (uint32 data.Length); typeBytes; data; be32 crc ]

    let private ihdr (w: int) (h: int) : byte[] =
        // bit depth 8, color type 6 (RGBA), compression 0, filter 0, interlace 0
        Array.concat [ be32 (uint32 w); be32 (uint32 h); [| 8uy; 6uy; 0uy; 0uy; 0uy |] ]

    let private deflate (raw: byte[]) : byte[] =
        use ms = new MemoryStream()
        let z = new ZLibStream(ms, CompressionLevel.Optimal, true)
        z.Write(raw, 0, raw.Length)
        z.Dispose() // finishes the zlib stream (writes the Adler-32 trailer)
        ms.ToArray()

    /// <summary>
    /// Encode a top-row-first, row-major RGBA buffer (<c>w*h*4</c> bytes) as PNG.
    /// </summary>
    let encode (w: int) (h: int) (rgba: byte[]) : byte[] =
        let stride = w * 4
        // Prefix every scanline with a "None" (0) filter byte.
        let raw = Array.zeroCreate<byte> ((stride + 1) * h)

        for y in 0 .. h - 1 do
            Array.blit rgba (y * stride) raw (y * (stride + 1) + 1) stride

        Array.concat
            [
                signature
                chunk "IHDR" (ihdr w h)
                chunk "IDAT" (deflate raw)
                chunk "IEND" [||]
            ]
