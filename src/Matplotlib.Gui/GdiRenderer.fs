namespace Matplotlib.Gui

open System.Drawing
open System.Drawing.Drawing2D
open Matplotlib.Domain.Rendering
open Matplotlib.Domain.Style
// Opened last so its Color / Size / Point2D shadow the System.Drawing types.
open Matplotlib.Domain.Primitives

/// <summary>
/// An <see cref="IRenderer"/> implementation that draws onto a GDI+
/// <see cref="System.Drawing.Graphics"/> surface, for on-screen (window) display.
/// </summary>
/// <remarks>
/// The raster counterpart of <c>Matplotlib.Backends.Svg.SvgRenderer</c>. It is an
/// opt-in, Windows-only backend (GDI+ via <c>System.Drawing</c>) and is never on
/// the default, zero-native-dependency path. Display coordinates use a
/// bottom-left origin (Matplotlib convention); this renderer flips Y on write,
/// since GDI+'s origin is top-left. Text is measured with the same heuristic as
/// the SVG backend so layout matches across backends.
/// </remarks>
type GdiRenderer(graphics: Graphics, sizePx: Size, dpi: float) =

    let height = sizePx.Height
    let pt2px = dpi / 72.0
    let clipStates = System.Collections.Generic.Stack<GraphicsState>()

    /// <summary>Flip a display-space Y (bottom-left origin) to GDI Y (top-left).</summary>
    let flip (y: float) = height - y

    do
        graphics.SmoothingMode <- SmoothingMode.AntiAlias
        graphics.PixelOffsetMode <- PixelOffsetMode.HighQuality
        graphics.TextRenderingHint <- System.Drawing.Text.TextRenderingHint.AntiAliasGridFit

    let toGdi (c: Color) = System.Drawing.Color.FromArgb(c.A255, c.R255, c.G255, c.B255)

    let toPointF (p: Point2D) = PointF(float32 p.X, float32 (flip p.Y))

    /// <summary>Map a matplotlib font family name onto an installed system font.</summary>
    let fontFamily (family: string) =
        match family with
        | "sans-serif" -> "Arial"
        | "serif" -> "Times New Roman"
        | "monospace" -> "Consolas"
        | "맑은 고딕" -> "Malgun Gothic" // localized name -> GDI family name
        | other -> other

    let buildPath (path: Path) : GraphicsPath =
        let gp = new GraphicsPath()
        let mutable current = PointF(0.0f, 0.0f)

        for cmd in path.Commands do
            match cmd with
            | MoveTo p ->
                current <- toPointF p
                gp.StartFigure()
            | LineTo p ->
                let next = toPointF p
                gp.AddLine(current, next)
                current <- next
            | CurveTo(c1, c2, e) ->
                let p1 = toPointF c1
                let p2 = toPointF c2
                let pe = toPointF e
                gp.AddBezier(current, p1, p2, pe)
                current <- pe
            | ClosePath -> gp.CloseFigure()

        gp

    interface IRenderer with

        member _.CanvasSizePx = sizePx

        member _.Dpi = dpi

        member _.DrawPath(gc: GraphicsContext, path: Path, fill: Color option) =
            if not path.Commands.IsEmpty then
                use gp = buildPath path

                match fill with
                | Some c when not c.IsTransparent ->
                    use brush = new SolidBrush(toGdi c)
                    graphics.FillPath(brush, gp)
                | _ -> ()

                let stroke = gc.StrokeColor

                if not stroke.IsTransparent && gc.LineWidth > 0.0 then
                    use pen = new Pen(toGdi stroke, float32 gc.LineWidth)

                    pen.StartCap <-
                        match gc.CapStyle with
                        | "round" -> LineCap.Round
                        | "projecting"
                        | "square" -> LineCap.Square
                        | _ -> LineCap.Flat

                    pen.EndCap <- pen.StartCap

                    pen.LineJoin <-
                        match gc.JoinStyle with
                        | "miter" -> LineJoin.Miter
                        | "bevel" -> LineJoin.Bevel
                        | _ -> LineJoin.Round

                    match gc.DashPattern with
                    | Some pattern when pattern.Length > 0 ->
                        // GDI+ expresses dash lengths as multiples of the pen width.
                        pen.DashPattern <- pattern |> Array.map (fun v -> max 0.1f (float32 (v / gc.LineWidth)))
                    | _ -> ()

                    graphics.DrawPath(pen, gp)

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
                let emPx = float32 (font.Size * pt2px)

                let style =
                    (match font.Weight with
                     | Bold -> FontStyle.Bold
                     | Normal -> FontStyle.Regular)
                    ||| (match font.Slant with
                         | Italic -> FontStyle.Italic
                         | Upright -> FontStyle.Regular)

                use gdiFont = new Font(fontFamily font.Family, emPx, style, GraphicsUnit.Pixel)
                use brush = new SolidBrush(toGdi gc.StrokeColor)
                use fmt = new StringFormat(StringFormat.GenericTypographic)
                fmt.FormatFlags <- fmt.FormatFlags ||| StringFormatFlags.NoWrap ||| StringFormatFlags.NoClip

                fmt.Alignment <-
                    match hAlign with
                    | HLeft -> StringAlignment.Near
                    | HCenter -> StringAlignment.Center
                    | HRight -> StringAlignment.Far

                fmt.LineAlignment <-
                    match vAlign with
                    | VTop -> StringAlignment.Near
                    | VCenter -> StringAlignment.Center
                    | VBottom
                    | VBaseline -> StringAlignment.Far

                let state = graphics.Save()
                graphics.TranslateTransform(float32 x, float32 (flip y))

                if angleDegrees <> 0.0 then
                    graphics.RotateTransform(float32 -angleDegrees)

                graphics.DrawString(text, gdiFont, brush, PointF(0.0f, 0.0f), fmt)
                graphics.Restore state

        member _.MeasureText(text: string, font: FontProperties) : Size =
            // Match the SVG backend's heuristic so cross-backend layout is identical.
            let emPx = font.Size * pt2px

            {
                Width = float text.Length * 0.6 * emPx
                Height = emPx
            }

        member _.PushClip(clip: BBox) =
            clipStates.Push(graphics.Save())
            let y = flip clip.Y1
            graphics.IntersectClip(RectangleF(float32 clip.X0, float32 y, float32 clip.Width, float32 clip.Height))

        member _.PopClip() =
            if clipStates.Count > 0 then
                graphics.Restore(clipStates.Pop())
