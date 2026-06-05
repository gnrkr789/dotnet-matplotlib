namespace Matplotlib.Backends.Svg

open System.Globalization
open System.Text
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// A pure-managed renderer that serializes draw calls to an SVG document.
/// </summary>
/// <remarks>
/// Ported from <c>matplotlib.backends.backend_svg</c>. Has no native
/// dependencies. Display coordinates use a bottom-left origin (Matplotlib
/// convention); this renderer flips the Y axis when writing SVG, whose origin
/// is top-left.
/// </remarks>
type SvgRenderer(sizePx: Size, dpi: float) =

    let body = StringBuilder()
    let height = sizePx.Height
    let pt2px = dpi / 72.0

    /// <summary>Format a number compactly with invariant culture.</summary>
    let num (v: float) = v.ToString("0.###", CultureInfo.InvariantCulture)

    /// <summary>Flip a display-space Y (bottom-left origin) to SVG Y (top-left).</summary>
    let flip (y: float) = height - y

    let escape (text: string) =
        text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")

    let pathData (path: Path) : string =
        let sb = StringBuilder()
        let pt (p: Point2D) = $"{num p.X} {num (flip p.Y)}"

        for cmd in path.Commands do
            match cmd with
            | MoveTo p -> sb.Append("M ").Append(pt p).Append(' ') |> ignore
            | LineTo p -> sb.Append("L ").Append(pt p).Append(' ') |> ignore
            | CurveTo(c1, c2, e) ->
                sb
                    .Append("C ")
                    .Append(pt c1)
                    .Append(' ')
                    .Append(pt c2)
                    .Append(' ')
                    .Append(pt e)
                    .Append(' ')
                |> ignore
            | ClosePath -> sb.Append("Z ") |> ignore

        sb.ToString().Trim()

    let anchorOf (ha: HAlign) =
        match ha with
        | HLeft -> "start"
        | HCenter -> "middle"
        | HRight -> "end"

    let baselineOf (va: VAlign) =
        match va with
        | VTop -> "text-before-edge"
        | VCenter -> "central"
        | VBottom -> "text-after-edge"
        | VBaseline -> "alphabetic"

    /// <summary>The accumulated SVG document.</summary>
    member _.GetSvg() : string =
        let header =
            StringBuilder()
                .Append("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>\n")
                .Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" ")
                .Append($"width=\"{num sizePx.Width}\" height=\"{num sizePx.Height}\" ")
                .Append($"viewBox=\"0 0 {num sizePx.Width} {num sizePx.Height}\">\n")

        header.Append(body.ToString()).Append("</svg>\n").ToString()

    interface IRenderer with

        member _.CanvasSizePx = sizePx

        member _.Dpi = dpi

        member _.DrawPath(gc: GraphicsContext, path: Path, fill: Color option) =
            if not path.Commands.IsEmpty then
                let fillAttr =
                    match fill with
                    | Some c when not c.IsTransparent -> $"fill=\"{c.ToHex()}\" fill-opacity=\"{num c.A}\""
                    | _ -> "fill=\"none\""

                let stroke = gc.StrokeColor

                let strokeAttr =
                    if stroke.IsTransparent || gc.LineWidth <= 0.0 then
                        "stroke=\"none\""
                    else
                        $"stroke=\"{stroke.ToHex()}\" stroke-opacity=\"{num stroke.A}\" stroke-width=\"{num gc.LineWidth}\""

                let dashAttr =
                    match gc.DashPattern with
                    | Some pattern when pattern.Length > 0 ->
                        let joined = pattern |> Array.map num |> String.concat ","
                        $" stroke-dasharray=\"{joined}\""
                    | _ -> ""

                body
                    .Append($"<path {fillAttr} {strokeAttr} stroke-linecap=\"{gc.CapStyle}\" ")
                    .Append($"stroke-linejoin=\"{gc.JoinStyle}\"{dashAttr} d=\"{pathData path}\" />\n")
                |> ignore

        member _.DrawText
            (
                gc: GraphicsContext,
                x: float,
                y: float,
                text: string,
                font: FontProperties,
                angleDegrees: float,
                hAlign: HAlign,
                vAlign: VAlign
            ) =
            let sizePx = font.Size * pt2px
            let yf = flip y

            let weight =
                match font.Weight with
                | Bold -> " font-weight=\"bold\""
                | Normal -> ""

            let slant =
                match font.Slant with
                | Italic -> " font-style=\"italic\""
                | Upright -> ""

            let rotate =
                if angleDegrees <> 0.0 then
                    $" transform=\"rotate({num (-angleDegrees)} {num x} {num yf})\""
                else
                    ""

            body
                .Append($"<text x=\"{num x}\" y=\"{num yf}\" ")
                .Append($"font-family=\"{escape font.Family}\" font-size=\"{num sizePx}\"{weight}{slant} ")
                .Append($"fill=\"{gc.StrokeColor.ToHex()}\" fill-opacity=\"{num gc.StrokeColor.A}\" ")
                .Append($"text-anchor=\"{anchorOf hAlign}\" dominant-baseline=\"{baselineOf vAlign}\"{rotate}>")
                .Append(escape text)
                .Append("</text>\n")
            |> ignore

        member _.MeasureText(text: string, font: FontProperties) : Size =
            let sizePx = font.Size * pt2px

            {
                Width = float text.Length * 0.6 * sizePx
                Height = sizePx
            }
