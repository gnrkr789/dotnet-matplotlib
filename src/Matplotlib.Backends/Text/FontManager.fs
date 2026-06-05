namespace Matplotlib.Backends.Text

open System
open System.IO
open System.Collections.Generic
open Matplotlib.Domain.Text

/// <summary>
/// Resolves font-family names to parsed <see cref="TrueTypeFont"/>s by searching
/// the platform's font directories, with caching. The infrastructure (I/O) side
/// of font handling; the parser itself is pure and lives in the domain.
/// </summary>
/// <remarks>Loosely mirrors <c>matplotlib.font_manager.FontManager</c>.</remarks>
type FontManager() =

    let cache = Dictionary<string, TrueTypeFont option>()

    let fontDirs =
        [
            Environment.GetFolderPath Environment.SpecialFolder.Fonts // Windows: C:\Windows\Fonts
            "/usr/share/fonts"
            "/usr/local/share/fonts"
            Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".fonts")
            "/System/Library/Fonts"
            "/Library/Fonts"
            Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, "Library/Fonts")
        ]
        |> List.filter (fun d -> not (String.IsNullOrEmpty d))

    let allTtf =
        lazy
            (fontDirs
             |> List.collect (fun d ->
                 try
                     if Directory.Exists d then
                         Directory.EnumerateFiles(d, "*.ttf", SearchOption.AllDirectories) |> List.ofSeq
                     else
                         []
                 with _ ->
                     []))

    /// <summary>Ordered candidate file-name fragments for a family (with fallbacks).</summary>
    let candidatesFor (family: string) : string list =
        let f = family.ToLowerInvariant()
        let sans = [ "arial"; "dejavusans"; "liberationsans"; "helvetica"; "segoeui"; "verdana" ]
        let serif = [ "times"; "dejavuserif"; "liberationserif"; "georgia" ]
        let mono = [ "consola"; "dejavusansmono"; "liberationmono"; "cour" ]
        let korean = [ "malgun"; "gulim"; "batang"; "nanumgothic"; "notosanscjk" ]

        let primary =
            if f.Contains "맑은" || f.Contains "malgun" || f.Contains "gothic" then
                korean
            elif f.Contains "mono" then
                mono
            elif f.Contains "serif" && not (f.Contains "sans") then
                serif
            elif f = "sans-serif" || f = "sans" then
                sans
            else
                [ f.Replace(" ", "") ]

        // de-duplicate while keeping order, then append sans as a last resort
        (primary @ sans) |> List.distinct

    /// <summary>Resolve a family name to a parsed font, or <c>None</c> if unavailable.</summary>
    member _.Resolve(family: string) : TrueTypeFont option =
        match cache.TryGetValue family with
        | true, v -> v
        | _ ->
            let files = allTtf.Value
            let norm (p: string) = Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Replace(" ", "")

            let found =
                candidatesFor family
                |> List.tryPick (fun c ->
                    files
                    |> List.tryFind (fun fp -> norm fp = c)
                    |> Option.orElseWith (fun () -> files |> List.tryFind (fun fp -> (norm fp).Contains c)))

            let font =
                found
                |> Option.bind (fun fp ->
                    try
                        Some(TrueTypeFont(File.ReadAllBytes fp))
                    with _ ->
                        None)

            cache[family] <- font
            font

    /// <summary>A shared, process-wide font manager.</summary>
    static member val Default = FontManager()
