namespace Matplotlib.Domain

open System
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Transforms
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering
open Matplotlib.Domain.Artists

/// <summary>Layout/numeric helpers used while drawing an <c>Axes</c>.</summary>
[<RequireQualifiedAccess>]
module internal AxesLayout =

    /// <summary>Clamp an integer to <c>[lo, hi]</c>.</summary>
    let clampInt (lo: int) (hi: int) (v: int) = max lo (min hi v)

    /// <summary>True if <paramref name="v"/> lies within the (orientation-free) interval.</summary>
    let inView (interval: Interval) (v: float) =
        let a = interval.Min
        let b = interval.Max
        let eps = (b - a) * 1e-9
        v >= a - eps && v <= b + eps

    /// <summary>Expand a data range by Matplotlib's default 5% margin.</summary>
    let marginExpand (lo: float) (hi: float) : Interval =
        let range = hi - lo

        if range = 0.0 then
            let d = if lo = 0.0 then 0.5 else abs lo * 0.05
            { Lower = lo - d; Upper = hi + d }
        else
            {
                Lower = lo - 0.05 * range
                Upper = hi + 0.05 * range
            }

    /// <summary>Number of tick bins for an axis of the given pixel length.</summary>
    let tickBins (lengthPx: float) (labelSizePts: float) (factor: float) (pt2px: float) : int =
        let sizePx = labelSizePts * factor * pt2px

        let raw =
            if sizePx > 0.0 then
                int (Math.Round(lengthPx / sizePx))
            else
                9

        clampInt 1 9 raw

/// <summary>Immutable context describing where/how an Axes is being drawn.</summary>
[<NoComparison; NoEquality>]
type internal AxesDrawContext =
    {
        Renderer: IRenderer
        Box: BBox
        TransAxes: ITransform
        TransData: ITransform
        Pt2Px: float
        XTicks: float[]
        XLabels: string[]
        YTicks: float[]
        YLabels: string[]
    }

/// <summary>
/// A single plotting region: owns data limits, the X/Y axes, the plotted lines,
/// title and legend, and knows how to render itself onto an <see cref="IRenderer"/>.
/// </summary>
/// <remarks>Ported from <c>matplotlib.axes.Axes</c> / <c>_AxesBase</c>.</remarks>
type Axes(rc: RcParams) =

    let lines = ResizeArray<Line2D>()
    let cycler = PropertyCycler.CreateDefault()

    /// <summary>Create an Axes with the default <c>rcParams</c>.</summary>
    new() = Axes(RcParams.Default)

    /// <summary>The active rcParams snapshot.</summary>
    member _.Rc = rc

    /// <summary>Axes position within the figure, in figure fractions.</summary>
    member val Position = BBox.fromExtents rc.SubplotLeft rc.SubplotBottom rc.SubplotRight rc.SubplotTop with get, set

    /// <summary>Background (face) color of the data area.</summary>
    member val FaceColor = rc.AxesFaceColor with get, set

    /// <summary>The X axis.</summary>
    member val XAxis = Axis(XAxis) with get

    /// <summary>The Y axis.</summary>
    member val YAxis = Axis(YAxis) with get

    /// <summary>The current X view limits.</summary>
    member val XLim = { Lower = 0.0; Upper = 1.0 } with get, set

    /// <summary>The current Y view limits.</summary>
    member val YLim = { Lower = 0.0; Upper = 1.0 } with get, set

    member val private XLimAuto = true with get, set
    member val private YLimAuto = true with get, set

    /// <summary>The Axes title.</summary>
    member val Title = "" with get, set

    /// <summary>Whether to draw the legend.</summary>
    member val ShowLegend = false with get, set

    /// <summary>The plotted lines.</summary>
    member _.Lines = lines

    /// <summary>Plot y versus x as a connected line (Matplotlib's <c>plot</c>).</summary>
    member this.Plot
        (
            xs: float[],
            ys: float[],
            ?color: Color,
            ?lineStyle: LineStyle,
            ?marker: MarkerStyle,
            ?lineWidth: float,
            ?label: string
        ) : Line2D =
        let line = Line2D(xs, ys)
        line.Color <- defaultArg color (cycler.Next())
        line.LineStyle <- defaultArg lineStyle Solid
        line.Marker <- defaultArg marker NoMarker
        line.LineWidth <- defaultArg lineWidth rc.LinesLineWidth
        line.Label <- defaultArg label ""
        lines.Add line
        this.Autoscale()
        line

    /// <summary>Draw a scatter of markers (Matplotlib's <c>scatter</c>).</summary>
    member this.Scatter
        (xs: float[], ys: float[], ?color: Color, ?marker: MarkerStyle, ?markerSize: float, ?label: string)
        : Line2D =
        let line = Line2D(xs, ys)
        line.LineStyle <- NoLine
        line.Color <- defaultArg color (cycler.Next())
        line.Marker <- defaultArg marker Circle
        line.MarkerSize <- defaultArg markerSize 6.0
        line.Label <- defaultArg label ""
        lines.Add line
        this.Autoscale()
        line

    /// <summary>Set the X view limits explicitly (disables x autoscale).</summary>
    member this.SetXLim(lower: float, upper: float) =
        this.XLim <- { Lower = lower; Upper = upper }
        this.XLimAuto <- false

    /// <summary>Set the Y view limits explicitly (disables y autoscale).</summary>
    member this.SetYLim(lower: float, upper: float) =
        this.YLim <- { Lower = lower; Upper = upper }
        this.YLimAuto <- false

    /// <summary>Set the Axes title.</summary>
    member this.SetTitle(title: string) = this.Title <- title

    /// <summary>Set the X axis label.</summary>
    member this.SetXLabel(label: string) = this.XAxis.Label <- label

    /// <summary>Set the Y axis label.</summary>
    member this.SetYLabel(label: string) = this.YAxis.Label <- label

    /// <summary>Enable grid lines on both axes.</summary>
    member this.Grid(visible: bool) =
        this.XAxis.ShowGrid <- visible
        this.YAxis.ShowGrid <- visible

    member private this.Autoscale() =
        let finite = Array.filter Double.IsFinite
        let xs = lines |> Seq.collect (fun l -> l.XData) |> Seq.toArray |> finite
        let ys = lines |> Seq.collect (fun l -> l.YData) |> Seq.toArray |> finite

        if this.XLimAuto && xs.Length > 0 then
            this.XLim <- AxesLayout.marginExpand (Array.min xs) (Array.max xs)

        if this.YLimAuto && ys.Length > 0 then
            this.YLim <- AxesLayout.marginExpand (Array.min ys) (Array.max ys)

    member private this.BuildContext(renderer: IRenderer) : AxesDrawContext =
        let canvas = renderer.CanvasSizePx
        let pos = this.Position

        let box =
            BBox.fromExtents
                (pos.X0 * canvas.Width)
                (pos.Y0 * canvas.Height)
                (pos.X1 * canvas.Width)
                (pos.Y1 * canvas.Height)

        let transAxes = BBoxTransform(BBox.unit, box) :> ITransform

        let dataBox = BBox.fromExtents this.XLim.Lower this.YLim.Lower this.XLim.Upper this.YLim.Upper

        let transLimits = BBoxTransform(dataBox, BBox.unit) :> ITransform
        let transData = Transforms.compose transLimits transAxes
        let pt2px = renderer.Dpi / 72.0
        let nbinsX = AxesLayout.tickBins (abs box.Width) rc.TickLabelSize 3.0 pt2px
        let nbinsY = AxesLayout.tickBins (abs box.Height) rc.TickLabelSize 2.0 pt2px

        let xTicks =
            (this.XAxis.Scale.CreateLocator nbinsX).TickValues this.XLim
            |> Array.filter (AxesLayout.inView this.XLim)

        let yTicks =
            (this.YAxis.Scale.CreateLocator nbinsY).TickValues this.YLim
            |> Array.filter (AxesLayout.inView this.YLim)

        {
            Renderer = renderer
            Box = box
            TransAxes = transAxes
            TransData = transData
            Pt2Px = pt2px
            XTicks = xTicks
            XLabels = (this.XAxis.Scale.CreateFormatter()).FormatTicks xTicks
            YTicks = yTicks
            YLabels = (this.YAxis.Scale.CreateFormatter()).FormatTicks yTicks
        }

    member private this.DrawBackground(ctx: AxesDrawContext) =
        let b = ctx.Box

        let gc =
            { GraphicsContext.Default with
                StrokeColor = Color.none
                LineWidth = 0.0
            }

        let corners = [ b.P0; { X = b.X1; Y = b.Y0 }; b.P1; { X = b.X0; Y = b.Y1 } ]
        ctx.Renderer.DrawPath(gc, Path.polygon corners, Some this.FaceColor)

    member private this.DrawGrid(ctx: AxesDrawContext) =
        let b = ctx.Box

        let gc =
            { GraphicsContext.Default with
                StrokeColor = rc.GridColor
                LineWidth = rc.GridLineWidth * ctx.Pt2Px
            }

        if this.XAxis.ShowGrid then
            for tv in ctx.XTicks do
                let x = (ctx.TransData.Transform { X = tv; Y = this.YLim.Lower }).X
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x; Y = b.Y0 }; { X = x; Y = b.Y1 } ], None)

        if this.YAxis.ShowGrid then
            for tv in ctx.YTicks do
                let y = (ctx.TransData.Transform { X = this.XLim.Lower; Y = tv }).Y
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = b.X0; Y = y }; { X = b.X1; Y = y } ], None)

    member private this.DrawData(ctx: AxesDrawContext) =
        for line in lines do
            line.Transform <- ctx.TransData
            line.Draw ctx.Renderer

    member private this.DrawSpines(ctx: AxesDrawContext) =
        let edges =
            [
                { X = 0.0; Y = 0.0 }, { X = 1.0; Y = 0.0 }
                { X = 0.0; Y = 1.0 }, { X = 1.0; Y = 1.0 }
                { X = 0.0; Y = 0.0 }, { X = 0.0; Y = 1.0 }
                { X = 1.0; Y = 0.0 }, { X = 1.0; Y = 1.0 }
            ]

        for (a, b) in edges do
            let spine = Spine(a, b)
            spine.Transform <- ctx.TransAxes
            spine.Color <- rc.AxesEdgeColor
            spine.LineWidth <- rc.AxesLineWidth
            spine.Draw ctx.Renderer

    member private _.MakeTickLabel(x: float, y: float, text: string, ha: HAlign, va: VAlign) : Text =
        let t = Text(x, y, text)
        t.Transform <- IdentityTransform.Instance

        t.Font <-
            { FontProperties.Default with
                Size = rc.TickLabelSize
            }

        t.Color <- rc.TickColor
        t.HAlign <- ha
        t.VAlign <- va
        t

    member private this.DrawTicks(ctx: AxesDrawContext) =
        let b = ctx.Box
        let len = rc.TickMajorSize * ctx.Pt2Px
        let pad = rc.TickPad * ctx.Pt2Px

        let gc =
            { GraphicsContext.Default with
                StrokeColor = rc.TickColor
                LineWidth = rc.TickMajorWidth * ctx.Pt2Px
            }

        Array.iter2
            (fun tv lab ->
                let x = (ctx.TransData.Transform { X = tv; Y = this.YLim.Lower }).X
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x; Y = b.Y0 }; { X = x; Y = b.Y0 - len } ], None)
                (this.MakeTickLabel(x, b.Y0 - len - pad, lab, HCenter, VTop)).Draw ctx.Renderer)
            ctx.XTicks
            ctx.XLabels

        Array.iter2
            (fun tv lab ->
                let y = (ctx.TransData.Transform { X = this.XLim.Lower; Y = tv }).Y
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = b.X0; Y = y }; { X = b.X0 - len; Y = y } ], None)
                (this.MakeTickLabel(b.X0 - len - pad, y, lab, HRight, VCenter)).Draw ctx.Renderer)
            ctx.YTicks
            ctx.YLabels

    member private this.DrawAxisLabelsAndTitle(ctx: AxesDrawContext) =
        let b = ctx.Box
        let len = rc.TickMajorSize * ctx.Pt2Px
        let pad = rc.TickPad * ctx.Pt2Px

        let tickFont =
            { FontProperties.Default with
                Size = rc.TickLabelSize
            }

        let labelFont =
            { FontProperties.Default with
                Size = rc.AxesLabelSize
            }

        if this.XAxis.Label <> "" then
            let y = b.Y0 - len - pad - rc.TickLabelSize * ctx.Pt2Px - rc.AxesLabelPad * ctx.Pt2Px

            let t = Text(b.CenterX, y, this.XAxis.Label)
            t.Transform <- IdentityTransform.Instance
            t.Font <- labelFont
            t.Color <- rc.AxesLabelColor
            t.HAlign <- HCenter
            t.VAlign <- VTop
            t.Draw ctx.Renderer

        if this.YAxis.Label <> "" then
            let widest =
                if ctx.YLabels.Length = 0 then
                    0.0
                else
                    ctx.YLabels
                    |> Array.map (fun s -> (ctx.Renderer.MeasureText(s, tickFont)).Width)
                    |> Array.max

            let x = b.X0 - len - pad - widest - rc.AxesLabelPad * ctx.Pt2Px
            let t = Text(x, b.CenterY, this.YAxis.Label)
            t.Transform <- IdentityTransform.Instance
            t.Font <- labelFont
            t.Color <- rc.AxesLabelColor
            t.Rotation <- 90.0
            t.HAlign <- HCenter
            t.VAlign <- VBottom
            t.Draw ctx.Renderer

        if this.Title <> "" then
            let t = Text(b.CenterX, b.Y1 + rc.AxesTitlePad * ctx.Pt2Px, this.Title)
            t.Transform <- IdentityTransform.Instance

            t.Font <-
                { FontProperties.Default with
                    Size = rc.AxesTitleSize
                }

            t.Color <- rc.TextColor
            t.HAlign <- HCenter
            t.VAlign <- VBottom
            t.Draw ctx.Renderer

    member private this.DrawLegend(ctx: AxesDrawContext) =
        let entries = lines |> Seq.filter (fun l -> l.Label <> "") |> Seq.toList

        if this.ShowLegend && not entries.IsEmpty then
            let b = ctx.Box

            let font =
                { FontProperties.Default with
                    Size = rc.FontSize
                }

            let lineH = rc.FontSize * 1.4 * ctx.Pt2Px
            let pad = 0.4 * rc.FontSize * ctx.Pt2Px
            let sample = 2.0 * rc.FontSize * ctx.Pt2Px
            let gap = 0.5 * rc.FontSize * ctx.Pt2Px

            let textW =
                entries
                |> List.map (fun l -> (ctx.Renderer.MeasureText(l.Label, font)).Width)
                |> List.max

            let boxW = pad + sample + gap + textW + pad
            let boxH = pad + lineH * float entries.Length + pad
            let x1 = b.X1 - 0.01 * b.Width - pad
            let y1 = b.Y1 - 0.01 * abs b.Height - pad
            let x0 = x1 - boxW
            let y0 = y1 - boxH

            let frameGc =
                { GraphicsContext.Default with
                    StrokeColor = Color.fromHex "#cccccc"
                    LineWidth = ctx.Pt2Px
                }

            let corners =
                [
                    { X = x0; Y = y0 }
                    { X = x1; Y = y0 }
                    { X = x1; Y = y1 }
                    { X = x0; Y = y1 }
                ]

            ctx.Renderer.DrawPath(frameGc, Path.polygon corners, Some(Color.white.WithAlpha 0.9))

            entries
            |> List.iteri (fun i line ->
                let cy = y1 - pad - lineH * (float i + 0.5)
                let sx0 = x0 + pad

                let sampleGc =
                    { GraphicsContext.Default with
                        StrokeColor = line.Color
                        LineWidth = line.LineWidth * ctx.Pt2Px
                    }

                ctx.Renderer.DrawPath(
                    sampleGc,
                    Path.polyline [ { X = sx0; Y = cy }; { X = sx0 + sample; Y = cy } ],
                    None
                )

                let t = Text(sx0 + sample + gap, cy, line.Label)
                t.Transform <- IdentityTransform.Instance
                t.Font <- font
                t.Color <- rc.TextColor
                t.HAlign <- HLeft
                t.VAlign <- VCenter
                t.Draw ctx.Renderer)

    /// <summary>Render the Axes and all its content onto <paramref name="renderer"/>.</summary>
    member this.Draw(renderer: IRenderer) =
        let ctx = this.BuildContext renderer
        this.DrawBackground ctx
        this.DrawGrid ctx
        this.DrawData ctx
        this.DrawSpines ctx
        this.DrawTicks ctx
        this.DrawAxisLabelsAndTitle ctx
        this.DrawLegend ctx
