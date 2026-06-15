namespace Matplotlib.Domain.Primitives

open System

/// <summary>
/// Linearly maps data values onto <c>[0, 1]</c> for colormapping.
/// </summary>
/// <remarks>Ported from <c>matplotlib.colors.Normalize</c> (linear).</remarks>
type Normalize(vmin: float, vmax: float) =

    /// <summary>Lower data limit mapped to 0.</summary>
    member _.VMin = vmin

    /// <summary>Upper data limit mapped to 1.</summary>
    member _.VMax = vmax

    /// <summary>Map a value to <c>[0, 1]</c> (clamped).</summary>
    member _.Normalize(value: float) : float =
        if vmax = vmin then
            0.0
        else
            Math.Clamp((value - vmin) / (vmax - vmin), 0.0, 1.0)

/// <summary>
/// A colormap: maps <c>t ∈ [0, 1]</c> to a color by interpolating a lookup table.
/// </summary>
/// <remarks>Ported from <c>matplotlib.colors.Colormap</c> / <c>ListedColormap</c>.</remarks>
type Colormap(name: string, lut: Color[]) =

    /// <summary>The colormap name.</summary>
    member _.Name = name

    /// <summary>The lookup table.</summary>
    member _.Lut = lut

    /// <summary>Sample the colormap at <paramref name="t"/> (clamped to [0, 1]).</summary>
    member _.Apply(t: float) : Color =
        let n = lut.Length

        if n = 0 then
            Color.black
        else
            // matplotlib quantizes to N discrete entries: index = int(t * N),
            // with t = 1.0 mapping to the last entry (a flat lookup, not a blend).
            let i = min (n - 1) (int (Math.Clamp(t, 0.0, 1.0) * float n))
            lut[i]

/// <summary>The built-in colormaps.</summary>
[<RequireQualifiedAccess>]
module Colormap =

    let private fromFlat (name: string) (data: float[]) =
        let n = data.Length / 3

        Colormap(
            name,
            Array.init n (fun i ->
                {
                    R = data[3 * i]
                    G = data[3 * i + 1]
                    B = data[3 * i + 2]
                    A = 1.0
                })
        )

    let private buildLut (stops: (float * (float * float * float)) list) : Color[] =
        let arr = List.toArray stops

        Array.init 256 (fun i ->
            let t = float i / 255.0
            let mutable k = 0

            while k < arr.Length - 1 && fst arr[k + 1] < t do
                k <- k + 1

            let t0, (r0, g0, b0) = arr[k]
            let t1, (r1, g1, b1) = arr[min (k + 1) (arr.Length - 1)]
            let f = if t1 = t0 then 0.0 else (t - t0) / (t1 - t0)

            {
                R = r0 + (r1 - r0) * f
                G = g0 + (g1 - g0) * f
                B = b0 + (b1 - b0) * f
                A = 1.0
            })

    /// <summary>
    /// Builds a 256-entry lookup table from independent per-channel breakpoint
    /// lists, mirroring matplotlib's <c>LinearSegmentedColormap</c> segmentdata
    /// (each channel carries its own node positions).
    /// </summary>
    let private buildLutSegmented
        (red: (float * float) list)
        (green: (float * float) list)
        (blue: (float * float) list)
        : Color[] =
        let channel (stops: (float * float) list) =
            let arr = List.toArray stops

            fun (t: float) ->
                let mutable k = 0

                while k < arr.Length - 1 && fst arr[k + 1] < t do
                    k <- k + 1

                let x0, y0 = arr[k]
                let x1, y1 = arr[min (k + 1) (arr.Length - 1)]

                if x1 = x0 then
                    y0
                else
                    y0 + (y1 - y0) * (t - x0) / (x1 - x0)

        let r, g, b = channel red, channel green, channel blue

        Array.init 256 (fun i ->
            let t = float i / 255.0
            { R = r t; G = g t; B = b t; A = 1.0 })

    /// <summary>The default perceptually-uniform colormap.</summary>
    let viridis = fromFlat "viridis" ColormapData.viridis

    /// <summary>A linear grayscale colormap.</summary>
    let gray =
        Colormap(
            "gray",
            Array.init 256 (fun i ->
                let v = float i / 255.0
                { R = v; G = v; B = v; A = 1.0 })
        )

    /// <summary>The classic "jet" rainbow colormap.</summary>
    /// <remarks>Ported from matplotlib's <c>_jet_data</c> (independent per-channel segments).</remarks>
    let jet =
        buildLutSegmented
            [ 0.0, 0.0; 0.35, 0.0; 0.66, 1.0; 0.89, 1.0; 1.0, 0.5 ] // red
            [ 0.0, 0.0; 0.125, 0.0; 0.375, 1.0; 0.64, 1.0; 0.91, 0.0; 1.0, 0.0 ] // green
            [ 0.0, 0.5; 0.11, 1.0; 0.34, 1.0; 0.65, 0.0; 1.0, 0.0 ] // blue
        |> fun lut -> Colormap("jet", lut)

    /// <summary>The "hot" black-red-yellow-white colormap.</summary>
    let hot =
        buildLut
            [
                0.0, (0.0416, 0.0, 0.0)
                0.365079, (1.0, 0.0, 0.0)
                0.746032, (1.0, 1.0, 0.0)
                1.0, (1.0, 1.0, 1.0)
            ]
        |> fun lut -> Colormap("hot", lut)

    /// <summary>Look up a built-in colormap by name.</summary>
    let byName (name: string) : Colormap =
        match name with
        | "viridis" -> viridis
        | "gray"
        | "grey" -> gray
        | "jet" -> jet
        | "hot" -> hot
        | other -> failwith $"Unknown colormap '{other}'."
