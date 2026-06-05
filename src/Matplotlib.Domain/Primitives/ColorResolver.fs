namespace Matplotlib.Domain.Primitives

open System
open System.Collections.Generic
open System.Globalization

/// <summary>
/// Resolves Matplotlib color specifications (strings) into <see cref="Color"/>.
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.colors.to_rgba</c>. Supported forms: <c>"none"</c>,
/// single-letter base colors, <c>"C0".."Cn"</c> property-cycle references,
/// <c>"#rgb"/"#rgba"/"#rrggbb"/"#rrggbbaa"</c>, Tableau <c>"tab:*"</c>, CSS4/X11
/// names (case-insensitive), and a grayscale float string in <c>[0, 1]</c>.
/// </remarks>
type ColorResolver(propertyCycle: IReadOnlyList<Color>) =

    let cycle: Color[] =
        if isNull (box propertyCycle) || propertyCycle.Count = 0 then
            ColorData.tab10 |> List.map Color.fromHex |> List.toArray
        else
            Array.ofSeq propertyCycle

    let resolveCore (spec: string) : Color =
        if isNull spec then
            nullArg (nameof spec)

        let s = spec.Trim()

        if s.Length = 0 || String.Equals(s, "none", StringComparison.OrdinalIgnoreCase) then
            Color.none
        elif s.StartsWith '#' then
            Color.fromHex s
        elif
            s.Length >= 2
            && (s[0] = 'C' || s[0] = 'c')
            && (let ok, _ = Int32.TryParse(s.Substring 1, NumberStyles.None, CultureInfo.InvariantCulture)

                ok)
        then
            let idx = Int32.Parse(s.Substring 1, CultureInfo.InvariantCulture)
            cycle[idx % cycle.Length]
        elif s.Length = 1 && ColorData.baseColors.ContainsKey s then
            ColorData.baseColors[s]
        elif ColorData.tableauColors.ContainsKey s then
            Color.fromHex ColorData.tableauColors[s]
        elif ColorData.css4Colors.ContainsKey s then
            Color.fromHex ColorData.css4Colors[s]
        else
            match Double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture) with
            | true, gray when gray >= 0.0 && gray <= 1.0 -> Color.rgb gray gray gray
            | _ -> raise (FormatException $"'{spec}' is not a recognized color specification.")

    /// <summary>A resolver whose <c>C0..Cn</c> map to the default tab10 cycle.</summary>
    static member val Default = ColorResolver(ColorData.tab10 |> List.map Color.fromHex |> List.toArray) with get

    /// <summary>Resolve a color spec, optionally overriding the alpha channel.</summary>
    member _.Resolve(spec: string, ?alpha: float) : Color =
        let c = resolveCore spec

        match alpha with
        | Some a -> c.WithAlpha a
        | None -> c

    /// <summary>Try to resolve a color spec; returns false rather than throwing.</summary>
    member _.TryResolve(spec: string) : bool * Color =
        try
            true, resolveCore spec
        with :? FormatException ->
            false, Unchecked.defaultof<Color>
