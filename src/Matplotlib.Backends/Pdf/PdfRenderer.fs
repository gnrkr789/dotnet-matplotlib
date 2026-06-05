namespace Matplotlib.Backends.Pdf

open System.Collections.Generic
open System.Globalization
open System.Text
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// A pure-managed PDF 1.4 renderer: serializes draw calls to a single-page PDF
/// content stream. Paths map to PDF path operators; text uses the standard
/// (non-embedded) Helvetica font. Zero native dependencies.
/// </summary>
/// <remarks>
/// Ported in spirit from <c>matplotlib.backends.backend_pdf</c>. PDF user space,
/// like Matplotlib's display space, has a bottom-left origin and y-up, so no Y
/// flip is needed. Display pixels are scaled to PDF points (1/72 inch) by
/// <c>72/dpi</c>. Alpha is emitted via <c>ExtGState</c> objects.
/// </remarks>
type PdfRenderer(sizePx: Size, dpi: float) =

    let body = StringBuilder()
    let pt = 72.0 / dpi // display px -> PDF points
    let widthPts = sizePx.Width * pt
    let heightPts = sizePx.Height * pt
    let alphas = Dictionary<float, string>()

    let inv = CultureInfo.InvariantCulture
    let num (v: float) = v.ToString("0.###", inv)
    let sx (v: float) = v * pt

    /// <summary>Name of an ExtGState for a given alpha (creating it on demand).</summary>
    let alphaState (a: float) =
        match alphas.TryGetValue a with
        | true, name -> name
        | _ ->
            let name = $"GSa{alphas.Count}"
            alphas[a] <- name
            name

    let escape (text: string) = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")

    /// <summary>Emit path-construction operators for a path (scaled to points).</summary>
    let emitPath (path: Path) =
        for cmd in path.Commands do
            match cmd with
            | MoveTo p -> body.Append($"{num (sx p.X)} {num (sx p.Y)} m\n") |> ignore
            | LineTo p -> body.Append($"{num (sx p.X)} {num (sx p.Y)} l\n") |> ignore
            | CurveTo(c1, c2, e) ->
                body.Append(
                    $"{num (sx c1.X)} {num (sx c1.Y)} {num (sx c2.X)} {num (sx c2.Y)} {num (sx e.X)} {num (sx e.Y)} c\n"
                )
                |> ignore
            | ClosePath -> body.Append("h\n") |> ignore

    member _.Width = widthPts
    member _.Height = heightPts

    interface IRenderer with

        member _.CanvasSizePx = sizePx

        member _.Dpi = dpi

        member _.DrawPath(gc: GraphicsContext, path: Path, fill: Color option) =
            if not path.Commands.IsEmpty then
                body.Append("q\n") |> ignore

                let hasFill =
                    match fill with
                    | Some c when not c.IsTransparent ->
                        body.Append($"{num c.R} {num c.G} {num c.B} rg\n") |> ignore

                        if c.A < 1.0 then
                            body.Append($"/{alphaState c.A} gs\n") |> ignore

                        true
                    | _ -> false

                let stroke = gc.StrokeColor
                let hasStroke = not stroke.IsTransparent && gc.LineWidth > 0.0

                if hasStroke then
                    body.Append($"{num stroke.R} {num stroke.G} {num stroke.B} RG\n") |> ignore
                    body.Append($"{num (sx gc.LineWidth)} w\n") |> ignore

                    let cap =
                        match gc.CapStyle with
                        | "round" -> 1
                        | "projecting"
                        | "square" -> 2
                        | _ -> 0

                    let join =
                        match gc.JoinStyle with
                        | "miter" -> 0
                        | "bevel" -> 2
                        | _ -> 1

                    body.Append($"{cap} J {join} j\n") |> ignore

                    match gc.DashPattern with
                    | Some dash when dash.Length > 0 ->
                        let joined = dash |> Array.map (fun d -> num (sx d)) |> String.concat " "
                        body.Append($"[{joined}] 0 d\n") |> ignore
                    | _ -> ()

                emitPath path

                let painter =
                    match hasFill, hasStroke with
                    | true, true -> "B"
                    | true, false -> "f"
                    | false, true -> "S"
                    | false, false -> "n"

                body.Append(painter).Append('\n').Append("Q\n") |> ignore

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
            if not (System.String.IsNullOrEmpty text) then
                let size = font.Size // points
                let widthApprox = float text.Length * 0.6 * size

                let dx =
                    match hAlign with
                    | HLeft -> 0.0
                    | HCenter -> -widthApprox / 2.0
                    | HRight -> -widthApprox

                let dy =
                    match vAlign with
                    | VBaseline -> 0.0
                    | VTop -> -0.8 * size
                    | VCenter -> -0.3 * size
                    | VBottom -> 0.2 * size

                let c = gc.StrokeColor
                let rad = angleDegrees * System.Math.PI / 180.0
                let cosA = cos rad
                let sinA = sin rad
                // place the (dx,dy)-shifted origin in text space, rotated about the anchor
                let ox = sx x + (dx * cosA - dy * sinA)
                let oy = sx y + (dx * sinA + dy * cosA)

                body.Append("q\n").Append($"{num c.R} {num c.G} {num c.B} rg\n") |> ignore

                if c.A < 1.0 then
                    body.Append($"/{alphaState c.A} gs\n") |> ignore

                body.Append("BT\n").Append($"/F1 {num size} Tf\n") |> ignore

                body
                    .Append($"{num cosA} {num sinA} {num -sinA} {num cosA} {num ox} {num oy} Tm\n")
                    .Append($"({escape text}) Tj\n")
                    .Append("ET\nQ\n")
                |> ignore

        member _.MeasureText(text: string, font: FontProperties) : Size =
            let emPx = font.Size * dpi / 72.0

            {
                Width = float text.Length * 0.6 * emPx
                Height = emPx
            }

        member _.PushClip(clip: BBox) =
            body
                .Append("q\n")
                .Append($"{num (sx clip.X0)} {num (sx clip.Y0)} {num (sx clip.Width)} {num (sx clip.Height)} re W n\n")
            |> ignore

        member _.PopClip() = body.Append("Q\n") |> ignore

    /// <summary>Assemble the full PDF document bytes.</summary>
    member _.GetPdf() : byte[] =
        let content = body.ToString()

        let contentBytes = Encoding.ASCII.GetBytes content

        // Object 1 Catalog, 2 Pages, 3 Page, 4 Contents, 5 Font, then ExtGStates.
        let gsObjects = alphas |> Seq.sortBy (fun kv -> kv.Value) |> Seq.toList
        let firstGsObj = 6

        let extGStateDict =
            if gsObjects.IsEmpty then
                ""
            else
                let entries =
                    gsObjects
                    |> List.mapi (fun i kv -> $"/{kv.Value} {firstGsObj + i} 0 R")
                    |> String.concat " "

                $" /ExtGState << {entries} >>"

        let objects = ResizeArray<string>()
        objects.Add "<< /Type /Catalog /Pages 2 0 R >>"
        objects.Add "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"

        objects.Add(
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {num widthPts} {num heightPts}] "
            + $"/Resources << /Font << /F1 5 0 R >>{extGStateDict} >> /Contents 4 0 R >>"
        )

        objects.Add $"<< /Length {contentBytes.Length} >>\nstream\n{content}endstream"
        objects.Add "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"

        for kv in gsObjects do
            objects.Add $"<< /Type /ExtGState /ca {num kv.Key} /CA {num kv.Key} >>"

        // Serialize with an xref table.
        let sb = StringBuilder()
        sb.Append("%PDF-1.4\n") |> ignore
        let offsets = ResizeArray<int>()

        for i in 0 .. objects.Count - 1 do
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()))
            sb.Append($"{i + 1} 0 obj\n").Append(objects[i]).Append("\nendobj\n") |> ignore

        let xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString())
        sb.Append($"xref\n0 {objects.Count + 1}\n") |> ignore
        sb.Append("0000000000 65535 f \n") |> ignore

        for off in offsets do
            sb.Append(off.ToString("D10")).Append(" 00000 n \n") |> ignore

        sb.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\n") |> ignore
        sb.Append($"startxref\n{xrefOffset}\n") |> ignore
        sb.Append("%%EOF\n") |> ignore // plain literal: keep both percent signs
        Encoding.ASCII.GetBytes(sb.ToString())
