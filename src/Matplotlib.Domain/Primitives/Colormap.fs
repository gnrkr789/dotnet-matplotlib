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
            let x = Math.Clamp(t, 0.0, 1.0) * float (n - 1)
            let i0 = int (floor x)
            let i1 = min (i0 + 1) (n - 1)
            let f = x - float i0
            let a = lut[i0]
            let b = lut[i1]

            {
                R = a.R + (b.R - a.R) * f
                G = a.G + (b.G - a.G) * f
                B = a.B + (b.B - a.B) * f
                A = 1.0
            }

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
    let jet =
        buildLut
            [
                0.0, (0.0, 0.0, 0.5)
                0.125, (0.0, 0.0, 1.0)
                0.375, (0.0, 1.0, 1.0)
                0.625, (1.0, 1.0, 0.0)
                0.875, (1.0, 0.0, 0.0)
                1.0, (0.5, 0.0, 0.0)
            ]
        |> fun lut -> Colormap("jet", lut)

    /// <summary>The "hot" black-red-yellow-white colormap.</summary>
    let hot =
        buildLut
            [
                0.0, (0.0, 0.0, 0.0)
                0.365, (1.0, 0.0, 0.0)
                0.746, (1.0, 1.0, 0.0)
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
