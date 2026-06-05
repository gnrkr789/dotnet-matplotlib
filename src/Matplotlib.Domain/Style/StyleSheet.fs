namespace Matplotlib.Domain.Style

open System.Globalization
open Matplotlib.Domain.Primitives

/// <summary>
/// Parses Matplotlib-style <c>rcParams</c> text (and named built-in style sheets)
/// into <see cref="RcParams"/> overrides.
/// </summary>
/// <remarks>
/// Ported in spirit from <c>matplotlib.rcsetup</c> / <c>matplotlib.style</c>.
/// Recognizes a practical subset of keys (figure, axes, lines, font, ticks,
/// grid, text). Unknown keys are ignored, as Matplotlib does for unset params.
/// </remarks>
[<RequireQualifiedAccess>]
module StyleSheet =

    let private inv = CultureInfo.InvariantCulture

    let private pf (s: string) =
        match System.Double.TryParse(s.Trim(), NumberStyles.Float, inv) with
        | true, v -> Some v
        | _ -> None

    let private col (s: string) =
        try
            Some(ColorResolver.Default.Resolve(s.Trim()))
        with _ ->
            None

    /// <summary>Apply a single <c>key: value</c> override to the parameters.</summary>
    let applyEntry (rc: RcParams) (key: string) (value: string) : RcParams =
        let withFloat f = pf value |> Option.map f |> Option.defaultValue rc
        let withColor f = col value |> Option.map f |> Option.defaultValue rc

        match key with
        | "figure.dpi" -> withFloat (fun v -> { rc with FigureDpi = v })
        | "figure.figsize" ->
            let parts = value.Split(',')

            if parts.Length = 2 then
                match pf parts[0], pf parts[1] with
                | Some w, Some h ->
                    { rc with
                        FigureSizeInches = { Width = w; Height = h }
                    }
                | _ -> rc
            else
                rc
        | "figure.facecolor" -> withColor (fun c -> { rc with FigureFaceColor = c })
        | "axes.facecolor" -> withColor (fun c -> { rc with AxesFaceColor = c })
        | "axes.edgecolor" -> withColor (fun c -> { rc with AxesEdgeColor = c })
        | "axes.linewidth" -> withFloat (fun v -> { rc with AxesLineWidth = v })
        | "axes.labelcolor" -> withColor (fun c -> { rc with AxesLabelColor = c })
        | "axes.labelsize" -> withFloat (fun v -> { rc with AxesLabelSize = v })
        | "axes.titlesize" -> withFloat (fun v -> { rc with AxesTitleSize = v })
        | "lines.linewidth" -> withFloat (fun v -> { rc with LinesLineWidth = v })
        | "font.size" -> withFloat (fun v -> { rc with FontSize = v })
        | "font.family" -> { rc with FontFamily = value.Trim() }
        | "xtick.color"
        | "ytick.color" -> withColor (fun c -> { rc with TickColor = c })
        | "xtick.labelsize"
        | "ytick.labelsize" -> withFloat (fun v -> { rc with TickLabelSize = v })
        | "grid.color" -> withColor (fun c -> { rc with GridColor = c })
        | "grid.linewidth" -> withFloat (fun v -> { rc with GridLineWidth = v })
        | "text.color" -> withColor (fun c -> { rc with TextColor = c })
        | _ -> rc

    /// <summary>Apply a sequence of <c>key: value</c> lines (comments with <c>#</c> allowed).</summary>
    let apply (lines: string seq) (rc: RcParams) : RcParams =
        lines
        |> Seq.map (fun l -> if l.Contains '#' then l.Substring(0, l.IndexOf '#') else l)
        |> Seq.choose (fun l ->
            let i = l.IndexOf ':'

            if i > 0 then
                Some(l.Substring(0, i).Trim(), l.Substring(i + 1).Trim())
            else
                None)
        |> Seq.fold (fun acc (k, v) -> applyEntry acc k v) rc

    /// <summary>Parse a full rcParams/matplotlibrc text block.</summary>
    let parseText (text: string) (rc: RcParams) : RcParams = apply (text.Replace("\r\n", "\n").Split('\n')) rc

    let private styles =
        dict
            [
                "ggplot",
                [
                    "axes.facecolor: #E5E5E5"
                    "figure.facecolor: white"
                    "grid.color: white"
                    "axes.edgecolor: white"
                    "lines.linewidth: 2.0"
                ]
                "dark_background",
                [
                    "figure.facecolor: black"
                    "axes.facecolor: black"
                    "axes.edgecolor: white"
                    "axes.labelcolor: white"
                    "text.color: white"
                    "xtick.color: white"
                    "grid.color: #555555"
                ]
                "grayscale", [ "axes.facecolor: #eeeeee"; "grid.color: #cccccc"; "figure.facecolor: white" ]
                "seaborn",
                [
                    "axes.facecolor: #EAEAF2"
                    "grid.color: white"
                    "figure.facecolor: white"
                    "axes.edgecolor: white"
                ]
            ]

    /// <summary>Apply a built-in named style (no-op for an unknown name).</summary>
    let byName (name: string) (rc: RcParams) : RcParams =
        match styles.TryGetValue name with
        | true, lines -> apply lines rc
        | _ -> rc

    /// <summary>The available built-in style names.</summary>
    let names = styles.Keys |> Seq.toList
