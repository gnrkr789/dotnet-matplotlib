namespace Matplotlib.Domain.Text

open System

/// <summary>
/// A minimal, pure-managed TrueType/OpenType (<c>glyf</c>-based) font reader:
/// parses the tables needed to map characters to glyph outlines and advance
/// widths, with quadratic contours flattened to polylines (in font units, y-up).
/// </summary>
/// <remarks>
/// Operates on an in-memory font file (no I/O). Supports <c>cmap</c> formats 4
/// and 12, simple and composite glyphs. Ported in spirit from FreeType's glyph
/// loader, which Matplotlib uses via <c>font_manager</c>/Agg for text layout.
/// </remarks>
type TrueTypeFont(data: byte[]) =

    let u8 (o: int) = int data[o]
    let u16 (o: int) = (int data[o] <<< 8) ||| int data[o + 1]

    let i16 (o: int) =
        let v = (int data[o] <<< 8) ||| int data[o + 1]
        if v >= 0x8000 then v - 0x10000 else v

    let u32 (o: int) =
        (uint32 data[o] <<< 24)
        ||| (uint32 data[o + 1] <<< 16)
        ||| (uint32 data[o + 2] <<< 8)
        ||| uint32 data[o + 3]

    // --- table directory ---------------------------------------------------
    let tables =
        let numTables = u16 4
        let mutable map = Map.empty

        for i in 0 .. numTables - 1 do
            let rec' = 12 + i * 16
            let tag = Text.Encoding.ASCII.GetString(data, rec', 4)
            map <- Map.add tag (int (u32 (rec' + 8))) map

        map

    let tableOffset name =
        match Map.tryFind name tables with
        | Some o -> o
        | None -> failwithf "TrueType font is missing the '%s' table." name

    let headOffset = tableOffset "head"
    let unitsPerEm = u16 (headOffset + 18)
    let locFormat = i16 (headOffset + 50) // 0 = short, 1 = long

    let hheaOffset = tableOffset "hhea"
    let ascent = i16 (hheaOffset + 4)
    let descent = i16 (hheaOffset + 6)
    let numHMetrics = u16 (hheaOffset + 34)

    let maxpOffset = tableOffset "maxp"
    let numGlyphs = u16 (maxpOffset + 4)

    let locaOffset = tableOffset "loca"
    let glyfOffset = tableOffset "glyf"
    let hmtxOffset = tableOffset "hmtx"

    /// <summary>Byte offset of glyph <paramref name="g"/> within the glyf table.</summary>
    let glyphLoc (g: int) =
        if locFormat = 0 then
            (u16 (locaOffset + g * 2)) * 2, (u16 (locaOffset + (g + 1) * 2)) * 2
        else
            int (u32 (locaOffset + g * 4)), int (u32 (locaOffset + (g + 1) * 4))

    let advanceWidth (g: int) =
        let idx = if g < numHMetrics then g else numHMetrics - 1
        u16 (hmtxOffset + idx * 4)

    // --- cmap (character -> glyph id) --------------------------------------
    let cmap =
        let baseO = tableOffset "cmap"
        let numSub = u16 (baseO + 2)
        let mutable best = -1

        // Prefer a Unicode subtable (platform 3 enc 1/10, or platform 0).
        for i in 0 .. numSub - 1 do
            let p = baseO + 4 + i * 8
            let plat = u16 p
            let enc = u16 (p + 2)
            let off = int (u32 (p + 4))

            if (plat = 3 && (enc = 1 || enc = 10)) || plat = 0 then
                best <- baseO + off

        if best < 0 && numSub > 0 then
            best <- baseO + int (u32 (baseO + 4 + 4))

        best

    let lookupGlyph (cp: int) : int =
        if cmap < 0 then
            0
        else
            match u16 cmap with
            | 4 ->
                let segX2 = u16 (cmap + 6)
                let segs = segX2 / 2
                let endO = cmap + 14
                let startO = endO + segX2 + 2
                let deltaO = startO + segX2
                let rangeO = deltaO + segX2
                let mutable result = 0
                let mutable i = 0

                while result = 0 && i < segs do
                    if cp <= u16 (endO + i * 2) then
                        let segStart = u16 (startO + i * 2)

                        if cp >= segStart then
                            let idRange = u16 (rangeO + i * 2)

                            if idRange = 0 then
                                result <- (cp + i16 (deltaO + i * 2)) &&& 0xFFFF
                            else
                                let gi = u16 (rangeO + i * 2 + idRange + (cp - segStart) * 2)
                                result <- if gi = 0 then 0 else (gi + i16 (deltaO + i * 2)) &&& 0xFFFF

                        i <- segs // past the matching segment, stop
                    else
                        i <- i + 1

                result
            | 12 ->
                let nGroups = int (u32 (cmap + 12))
                let mutable result = 0
                let mutable i = 0

                while result = 0 && i < nGroups do
                    let g = cmap + 16 + i * 12
                    let startC = int (u32 g)
                    let endC = int (u32 (g + 4))

                    if cp >= startC && cp <= endC then
                        result <- int (u32 (g + 8)) + (cp - startC)

                    i <- i + 1

                result
            | _ -> 0

    let flattenSteps = 8

    /// <summary>Read the contours of a glyph (font units), following composites.</summary>
    let rec glyphContours (g: int) (dx: float) (dy: float) : (float * float)[][] =
        if g < 0 || g >= numGlyphs then
            [||]
        else
            let start, finish = glyphLoc g

            if finish <= start then
                [||] // empty glyph (e.g. space)
            else
                let o = glyfOffset + start
                let numContours = i16 o

                if numContours < 0 then
                    compositeContours (o + 10) dx dy
                else
                    simpleContours o numContours dx dy

    and simpleContours (o: int) (numContours: int) (dx: float) (dy: float) : (float * float)[][] =
        let endPts = Array.init numContours (fun i -> u16 (o + 10 + i * 2))
        let numPoints = if numContours = 0 then 0 else endPts[numContours - 1] + 1
        let insLen = u16 (o + 10 + numContours * 2)
        let mutable p = o + 10 + numContours * 2 + 2 + insLen

        // flags (with repeat)
        let flags = Array.zeroCreate<int> numPoints
        let mutable k = 0

        while k < numPoints do
            let f = u8 p
            p <- p + 1
            flags[k] <- f
            k <- k + 1

            if f &&& 0x08 <> 0 then
                let mutable rep = u8 p
                p <- p + 1

                while rep > 0 && k < numPoints do
                    flags[k] <- f
                    k <- k + 1
                    rep <- rep - 1

        // x coords (delta-encoded)
        let xs = Array.zeroCreate<int> numPoints
        let mutable x = 0

        for i in 0 .. numPoints - 1 do
            let f = flags[i]

            if f &&& 0x02 <> 0 then
                let d = u8 p
                p <- p + 1
                x <- x + (if f &&& 0x10 <> 0 then d else -d)
            elif f &&& 0x10 = 0 then
                x <- x + i16 p
                p <- p + 2

            xs[i] <- x

        // y coords (delta-encoded)
        let ys = Array.zeroCreate<int> numPoints
        let mutable y = 0

        for i in 0 .. numPoints - 1 do
            let f = flags[i]

            if f &&& 0x04 <> 0 then
                let d = u8 p
                p <- p + 1
                y <- y + (if f &&& 0x20 <> 0 then d else -d)
            elif f &&& 0x20 = 0 then
                y <- y + i16 p
                p <- p + 2

            ys[i] <- y

        // build contours
        let contours = ResizeArray<(float * float)[]>()
        let mutable startIdx = 0

        for c in 0 .. numContours - 1 do
            let endIdx = endPts[c]
            let count = endIdx - startIdx + 1

            if count > 0 && startIdx >= 0 && endIdx < numPoints then
                let pt i =
                    let j = startIdx + ((i % count + count) % count)
                    (float xs[j] + dx, float ys[j] + dy, flags[j] &&& 0x01 <> 0)

                contours.Add(buildContour pt count)

            startIdx <- endIdx + 1

        contours.ToArray()

    and buildContour (pt: int -> float * float * bool) (count: int) : (float * float)[] =
        // Insert implied on-curve midpoints between consecutive off-curve points.
        let expanded = ResizeArray<float * float * bool>()

        for i in 0 .. count - 1 do
            let (cx, cy, con) = pt i

            if expanded.Count > 0 then
                let (px, py, pon) = expanded[expanded.Count - 1]

                if not pon && not con then
                    expanded.Add((px + cx) / 2.0, (py + cy) / 2.0, true)

            expanded.Add(cx, cy, con)

        // ensure the contour begins on-curve
        let firstOn = expanded |> Seq.tryFindIndex (fun (_, _, on) -> on)

        match firstOn with
        | None -> [||]
        | Some rot ->
            let m = expanded.Count
            let g i = expanded[(rot + i) % m]
            let out = ResizeArray<float * float>()
            let (x0, y0, _) = g 0
            out.Add(x0, y0)
            let mutable i = 1

            while i <= m do
                let (cx, cy, con) = g i

                if con then
                    out.Add(cx, cy)
                    i <- i + 1
                else
                    // quadratic: control = current off-curve, end = next on-curve
                    let (ex, ey, _) = g (i + 1)
                    let (sx, sy) = out[out.Count - 1]

                    for s in 1..flattenSteps do
                        let t = float s / float flattenSteps
                        let mt = 1.0 - t
                        let bx = mt * mt * sx + 2.0 * mt * t * cx + t * t * ex
                        let by = mt * mt * sy + 2.0 * mt * t * cy + t * t * ey
                        out.Add(bx, by)

                    i <- i + 2

            out.ToArray()

    and compositeContours (o: int) (dx: float) (dy: float) : (float * float)[][] =
        let result = ResizeArray<(float * float)[]>()
        let mutable p = o
        let mutable more = true

        while more do
            let flags = u16 p
            let compGlyph = u16 (p + 2)
            p <- p + 4

            let arg1, arg2 =
                if flags &&& 0x0001 <> 0 then
                    let a = i16 p
                    let b = i16 (p + 2)
                    p <- p + 4
                    a, b
                else
                    let a = int (sbyte data[p])
                    let b = int (sbyte data[p + 1])
                    p <- p + 2
                    a, b

            // skip scale info (we apply translation only; scaling is rare for text)
            if flags &&& 0x0008 <> 0 then
                p <- p + 2
            elif flags &&& 0x0040 <> 0 then
                p <- p + 4
            elif flags &&& 0x0080 <> 0 then
                p <- p + 8

            let ox, oy =
                if flags &&& 0x0002 <> 0 then
                    float arg1, float arg2
                else
                    0.0, 0.0

            for c in glyphContours compGlyph (dx + ox) (dy + oy) do
                result.Add c

            more <- flags &&& 0x0020 <> 0

        result.ToArray()

    /// <summary>Font design units per em (the glyph coordinate scale).</summary>
    member _.UnitsPerEm = unitsPerEm

    /// <summary>Typographic ascent in font units.</summary>
    member _.Ascent = float ascent

    /// <summary>Typographic descent in font units (typically negative).</summary>
    member _.Descent = float descent

    /// <summary>Advance width of a character in font units.</summary>
    member _.Advance(cp: int) : float = float (advanceWidth (lookupGlyph cp))

    /// <summary>
    /// Outline of a character as one or more closed contours of points, in font
    /// units (origin at the baseline, y increasing upward). Returns no contours
    /// for a malformed or empty glyph.
    /// </summary>
    member _.Outline(cp: int) : (float * float)[][] =
        try
            glyphContours (lookupGlyph cp) 0.0 0.0
        with _ ->
            [||]
