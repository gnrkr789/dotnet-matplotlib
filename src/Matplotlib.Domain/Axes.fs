namespace Matplotlib.Domain

open System
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Transforms
open Matplotlib.Domain.Scales
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

    /// <summary>
    /// Expand a data range by the default margin, but keep a "sticky" edge
    /// (e.g. a bar baseline) exactly at the limit with no margin on that side.
    /// </summary>
    /// <remarks>Ported from Matplotlib's <c>Artist.sticky_edges</c> autoscale handling.</remarks>
    let marginExpandSticky (sticky: float seq) (lo: float) (hi: float) : Interval =
        let range = hi - lo

        if range = 0.0 then
            marginExpand lo hi
        else
            let isSticky (v: float) = sticky |> Seq.exists (fun s -> abs (s - v) <= 1e-9 * (1.0 + abs v))

            {
                Lower = if isSticky lo then lo else lo - 0.05 * range
                Upper = if isSticky hi then hi else hi + 0.05 * range
            }

    /// <summary>Expand <c>(x, y)</c> into the vertices of a step plot.</summary>
    /// <remarks>Ported from Matplotlib's stepped drawstyles (pre/post/mid).</remarks>
    let stepPoints (where: StepWhere) (x: float[]) (y: float[]) : float[] * float[] =
        if x.Length = 0 then
            [||], [||]
        else
            let xs = ResizeArray<float>()
            let ys = ResizeArray<float>()
            xs.Add x[0]
            ys.Add y[0]

            match where with
            | Pre ->
                for i in 1 .. x.Length - 1 do
                    xs.Add x[i - 1]
                    ys.Add y[i]
                    xs.Add x[i]
                    ys.Add y[i]
            | Post ->
                for i in 1 .. x.Length - 1 do
                    xs.Add x[i]
                    ys.Add y[i - 1]
                    xs.Add x[i]
                    ys.Add y[i]
            | Mid ->
                for i in 1 .. x.Length - 1 do
                    let m = (x[i - 1] + x[i]) / 2.0
                    xs.Add m
                    ys.Add y[i - 1]
                    xs.Add m
                    ys.Add y[i]

                xs.Add x[x.Length - 1]
                ys.Add y[x.Length - 1]

            xs.ToArray(), ys.ToArray()

    /// <summary>Floored modulo (result has the sign of the divisor, like Python's <c>%</c>).</summary>
    let private flooredMod (x: float) (m: float) = ((x % m) + m) % m

    /// <summary>
    /// Minor tick locations between the given major ticks within the view.
    /// </summary>
    /// <remarks>
    /// Ported from <c>matplotlib.ticker.AutoMinorLocator</c>: the major interval
    /// is split into 5 sub-intervals when its mantissa is 1/2.5/5/10, else 4.
    /// </remarks>
    let minorTicks (majors: float[]) (view: Interval) : float[] =
        if majors.Length < 2 then
            [||]
        else
            let majorStep = abs (majors[1] - majors[0])

            if majorStep <= 0.0 then
                [||]
            else
                let mantissa = 10.0 ** flooredMod (log10 majorStep) 1.0

                let ndivs =
                    if [ 1.0; 2.5; 5.0; 10.0 ] |> List.exists (fun v -> abs (mantissa - v) < 1e-6) then
                        5
                    else
                        4

                let minorStep = majorStep / float ndivs
                let lo = view.Min
                let hi = view.Max
                let t0 = majors[0]
                let kStart = int (floor ((lo - t0) / minorStep)) - 1
                let kEnd = int (ceil ((hi - t0) / minorStep)) + 1

                [ for k in kStart..kEnd -> t0 + float k * minorStep ]
                |> List.filter (fun v -> v >= lo - 1e-9 && v <= hi + 1e-9)
                |> List.filter (fun v -> not (majors |> Array.exists (fun m -> abs (m - v) < minorStep * 1e-6)))
                |> List.toArray

    /// <summary>Minor tick locations for a log axis (2..9 within each decade).</summary>
    /// <remarks>Ported from <c>matplotlib.ticker.LogLocator</c> minor sub-ticks.</remarks>
    let logMinorTicks (view: Interval) : float[] =
        let lo = max view.Min 1e-300
        let hi = max view.Max (lo * 10.0)
        let loExp = int (floor (log10 lo))
        let hiExp = int (ceil (log10 hi))

        [
            for k in loExp..hiExp do
                for d in 2..9 do
                    let v = float d * (10.0 ** float k)

                    if v >= lo - 1e-12 && v <= hi + 1e-12 then
                        yield v
        ]
        |> List.toArray

    /// <summary>Autoscale limits in log space (margins applied to the exponents).</summary>
    let logLimits (vals: float[]) : Interval =
        let pos = vals |> Array.filter (fun v -> v > 0.0)

        if pos.Length = 0 then
            { Lower = 1.0; Upper = 10.0 }
        else
            let llo = log10 (Array.min pos)
            let lhi = log10 (Array.max pos)
            let m = 0.05 * (if lhi = llo then 1.0 else lhi - llo)

            {
                Lower = 10.0 ** (llo - m)
                Upper = 10.0 ** (lhi + m)
            }

    /// <summary>
    /// The two coordinates of a tick mark along the axis-normal direction,
    /// given the spine <paramref name="baseline"/>, tick <paramref name="length"/>
    /// (px) and direction (<c>in</c> / <c>out</c> / <c>inout</c>).
    /// </summary>
    let tickEndpoints (baseline: float) (length: float) (direction: string) : float * float =
        match direction with
        | "in" -> baseline, baseline + length
        | "inout" -> baseline - length, baseline + length
        | _ -> baseline, baseline - length

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
        XView: Interval
        YView: Interval
        XTicks: float[]
        XLabels: string[]
        YTicks: float[]
        YLabels: string[]
        XMinor: float[]
        YMinor: float[]
    }

/// <summary>
/// A single plotting region: owns data limits, the X/Y axes, the plotted lines,
/// title and legend, and knows how to render itself onto an <see cref="IRenderer"/>.
/// </summary>
/// <remarks>Ported from <c>matplotlib.axes.Axes</c> / <c>_AxesBase</c>.</remarks>
type Axes(rc: RcParams) =

    let lines = ResizeArray<Line2D>()
    let patches = ResizeArray<Patch>()
    let collections = ResizeArray<Collection>()
    let images = ResizeArray<AxesImage>()
    let overlays = ResizeArray<Artist>()
    let stickyX = ResizeArray<float>()
    let stickyY = ResizeArray<float>()
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

    /// <summary>Where the legend is placed within the Axes.</summary>
    member val LegendLoc = Best with get, set

    /// <summary>Whether minor ticks are drawn.</summary>
    member val MinorTicksEnabled = false with get, set

    /// <summary>Tick direction: <c>out</c> (default), <c>in</c> or <c>inout</c>.</summary>
    member val TickDirection = "out" with get, set

    /// <summary>Whether the top spine is drawn.</summary>
    member val SpineTop = true with get, set

    /// <summary>Whether the bottom spine is drawn.</summary>
    member val SpineBottom = true with get, set

    /// <summary>Whether the left spine is drawn.</summary>
    member val SpineLeft = true with get, set

    /// <summary>Whether the right spine is drawn.</summary>
    member val SpineRight = true with get, set

    /// <summary>The plotted lines.</summary>
    member _.Lines = lines

    /// <summary>The plotted patches (bars, filled regions, shapes).</summary>
    member _.Patches = patches

    /// <summary>The drawn collections (bulk line/polygon sets).</summary>
    member _.Collections = collections

    /// <summary>The displayed images.</summary>
    member _.Images = images

    /// <summary>Display a 2D array as a colormapped image (Matplotlib's <c>imshow</c>).</summary>
    member this.Imshow(data: float[,], ?cmap: string, ?vmin: float, ?vmax: float) : AxesImage =
        let rows = Array2D.length1 data
        let cols = Array2D.length2 data

        let flat =
            [|
                for i in 0 .. rows - 1 do
                    for j in 0 .. cols - 1 -> data[i, j]
            |]

        let lo = defaultArg vmin (Array.min flat)
        let hi = defaultArg vmax (Array.max flat)
        let colormap = Colormap.byName (defaultArg cmap "viridis")
        let image = AxesImage(data, colormap, Normalize(lo, hi))
        images.Add image
        // origin 'upper': row 0 at top, so the y-axis is inverted.
        this.SetXLim(-0.5, float cols - 0.5)
        this.SetYLim(float rows - 0.5, -0.5)
        image

    /// <summary>Add a collection and rescale to include it (Matplotlib's <c>add_collection</c>).</summary>
    member this.AddCollection(collection: Collection) : Collection =
        collections.Add collection
        this.Autoscale()
        collection

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
        line.Marker <- defaultArg marker MarkerStyle.Circle
        line.MarkerSize <- defaultArg markerSize 6.0
        line.Label <- defaultArg label ""
        lines.Add line
        this.Autoscale()
        line

    /// <summary>Draw a vertical bar chart (Matplotlib's <c>bar</c>, center-aligned).</summary>
    member this.Bar
        (x: float[], height: float[], ?width: float, ?bottom: float[], ?color: Color, ?label: string)
        : Rectangle[] =
        let w = defaultArg width 0.8
        let bottoms = defaultArg bottom (Array.zeroCreate x.Length)
        let faceColor = defaultArg color (cycler.Next())
        let lbl = defaultArg label ""

        let rects =
            Array.init x.Length (fun i ->
                let rect = Rectangle(x[i] - w / 2.0, bottoms[i], w, height[i])
                rect.FaceColor <- faceColor
                rect.Label <- if i = 0 then lbl else ""
                rect)

        for rect in rects do
            patches.Add rect

        for bm in bottoms do
            stickyY.Add bm // bars stick to their baseline (no y margin there)

        this.Autoscale()
        rects

    /// <summary>Draw a horizontal bar chart (Matplotlib's <c>barh</c>, center-aligned).</summary>
    member this.BarH
        (y: float[], width: float[], ?height: float, ?left: float[], ?color: Color, ?label: string)
        : Rectangle[] =
        let h = defaultArg height 0.8
        let lefts = defaultArg left (Array.zeroCreate y.Length)
        let faceColor = defaultArg color (cycler.Next())
        let lbl = defaultArg label ""

        let rects =
            Array.init y.Length (fun i ->
                let rect = Rectangle(lefts[i], y[i] - h / 2.0, width[i], h)
                rect.FaceColor <- faceColor
                rect.Label <- if i = 0 then lbl else ""
                rect)

        for rect in rects do
            patches.Add rect

        for l in lefts do
            stickyX.Add l // horizontal bars stick to their baseline (no x margin there)

        this.Autoscale()
        rects

    /// <summary>Fill the area between two curves (Matplotlib's <c>fill_between</c>).</summary>
    member this.FillBetween
        (x: float[], y1: float[], ?y2: float[], ?color: Color, ?alpha: float, ?label: string)
        : Polygon =
        let lower = defaultArg y2 (Array.zeroCreate x.Length)
        let faceColor = (defaultArg color (cycler.Next())).WithAlpha(defaultArg alpha 1.0)

        let forward = Array.init x.Length (fun i -> { X = x[i]; Y = y1[i] })

        let backward =
            Array.init x.Length (fun i ->
                {
                    X = x[x.Length - 1 - i]
                    Y = lower[x.Length - 1 - i]
                })

        let polygon = Polygon(Array.append forward backward)
        polygon.FaceColor <- faceColor
        polygon.Label <- defaultArg label ""
        patches.Add polygon
        this.Autoscale()
        polygon

    /// <summary>Draw a step plot (Matplotlib's <c>step</c>, default <c>where = pre</c>).</summary>
    member this.Step
        (x: float[], y: float[], ?where: StepWhere, ?color: Color, ?lineStyle: LineStyle, ?label: string)
        : Line2D =
        let sx, sy = AxesLayout.stepPoints (defaultArg where Pre) x y
        this.Plot(sx, sy, ?color = color, ?lineStyle = lineStyle, ?label = label)

    /// <summary>Draw a line with x and/or y error bars (Matplotlib's <c>errorbar</c>).</summary>
    member this.Errorbar
        (
            x: float[],
            y: float[],
            ?yerr: float[],
            ?xerr: float[],
            ?color: Color,
            ?marker: MarkerStyle,
            ?lineStyle: LineStyle,
            ?label: string
        ) : Line2D =
        let col = defaultArg color (cycler.Next())
        let main = Line2D(x, y)
        main.Color <- col
        main.LineStyle <- defaultArg lineStyle Solid
        main.Marker <- defaultArg marker NoMarker
        main.LineWidth <- rc.LinesLineWidth
        main.Label <- defaultArg label ""
        lines.Add main

        let addBar (xs: float[]) (ys: float[]) =
            let bar = Line2D(xs, ys)
            bar.Color <- col
            bar.LineWidth <- rc.LinesLineWidth
            lines.Add bar

        match yerr with
        | Some err ->
            for i in 0 .. x.Length - 1 do
                addBar [| x[i]; x[i] |] [| y[i] - err[i]; y[i] + err[i] |]
        | None -> ()

        match xerr with
        | Some err ->
            for i in 0 .. x.Length - 1 do
                addBar [| x[i] - err[i]; x[i] + err[i] |] [| y[i]; y[i] |]
        | None -> ()

        this.Autoscale()
        main

    /// <summary>Draw a stem plot: vertical stems from a baseline to each point.</summary>
    /// <remarks>Ported from <c>matplotlib.axes.Axes.stem</c>.</remarks>
    member this.Stem(x: float[], y: float[], ?bottom: float, ?color: Color, ?label: string) : Line2D =
        let col = defaultArg color (cycler.Next())
        let bot = defaultArg bottom 0.0

        for i in 0 .. x.Length - 1 do
            let stem = Line2D([| x[i]; x[i] |], [| bot; y[i] |])
            stem.Color <- col
            stem.LineWidth <- rc.LinesLineWidth
            lines.Add stem

        if x.Length > 0 then
            let baseLine = Line2D([| Array.min x; Array.max x |], [| bot; bot |])
            baseLine.Color <- col
            baseLine.LineWidth <- rc.AxesLineWidth
            lines.Add baseLine

        let markerLine = Line2D(x, y)
        markerLine.LineStyle <- NoLine
        markerLine.Marker <- MarkerStyle.Circle
        markerLine.Color <- col
        markerLine.Label <- defaultArg label ""
        lines.Add markerLine
        this.Autoscale()
        markerLine

    /// <summary>Add text at a data-space position (Matplotlib's <c>Axes.text</c>).</summary>
    member _.Text
        (
            x: float,
            y: float,
            content: string,
            ?color: Color,
            ?fontSize: float,
            ?rotation: float,
            ?hAlign: HAlign,
            ?vAlign: VAlign
        ) : Text =
        let t = Text(x, y, content)
        t.Color <- defaultArg color rc.TextColor

        t.Font <-
            { FontProperties.Default with
                Size = defaultArg fontSize rc.FontSize
            }

        t.Rotation <- defaultArg rotation 0.0
        t.HAlign <- defaultArg hAlign HLeft
        t.VAlign <- defaultArg vAlign VBaseline
        overlays.Add t
        t

    /// <summary>Annotate the point <paramref name="xy"/> with text, optional arrow.</summary>
    /// <remarks>Ported from <c>matplotlib.axes.Axes.annotate</c> (basic connector).</remarks>
    member _.Annotate(content: string, xy: Point2D, ?xytext: Point2D, ?arrow: bool, ?color: Color) : Text =
        let textPos = defaultArg xytext xy
        let col = defaultArg color rc.TextColor

        if defaultArg arrow false then
            let connector = Line2D([| textPos.X; xy.X |], [| textPos.Y; xy.Y |])
            connector.Color <- col
            connector.LineWidth <- rc.LinesLineWidth
            overlays.Add connector

        let t = Text(textPos.X, textPos.Y, content)
        t.Color <- col

        t.Font <-
            { FontProperties.Default with
                Size = rc.FontSize
            }

        t.HAlign <- HLeft
        t.VAlign <- VBaseline
        overlays.Add t
        t

    /// <summary>Enable minor ticks on both axes (Matplotlib's <c>minorticks_on</c>).</summary>
    member this.MinorTicksOn() = this.MinorTicksEnabled <- true

    /// <summary>Disable minor ticks on both axes (Matplotlib's <c>minorticks_off</c>).</summary>
    member this.MinorTicksOff() = this.MinorTicksEnabled <- false

    /// <summary>Adjust tick appearance (Matplotlib's <c>tick_params</c>, direction subset).</summary>
    member this.TickParams(?direction: string) = direction |> Option.iter (fun d -> this.TickDirection <- d)

    /// <summary>Show or hide one spine by side (<c>top/bottom/left/right</c>).</summary>
    member this.SetSpineVisible(side: string, visible: bool) =
        match side with
        | "top" -> this.SpineTop <- visible
        | "bottom" -> this.SpineBottom <- visible
        | "left" -> this.SpineLeft <- visible
        | "right" -> this.SpineRight <- visible
        | other -> failwith $"Unknown spine '{other}'."

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

    /// <summary>Set the X axis scale by name (<c>"linear"</c> / <c>"log"</c>).</summary>
    member this.SetXScale(name: string) =
        this.XAxis.Scale <- Scale.byName name
        this.Autoscale()

    /// <summary>Set the Y axis scale by name (<c>"linear"</c> / <c>"log"</c>).</summary>
    member this.SetYScale(name: string) =
        this.YAxis.Scale <- Scale.byName name
        this.Autoscale()

    /// <summary>Enable grid lines on both axes.</summary>
    member this.Grid(visible: bool) =
        this.XAxis.ShowGrid <- visible
        this.YAxis.ShowGrid <- visible

    /// <summary>Show the legend, optionally at a specific location.</summary>
    member this.Legend(?loc: LegendLoc) =
        this.ShowLegend <- true
        loc |> Option.iter (fun l -> this.LegendLoc <- l)

    member private this.Autoscale() =
        let finite = Array.filter Double.IsFinite

        let bounds =
            Seq.append
                (patches |> Seq.choose (fun p -> p.DataBounds()))
                (collections |> Seq.choose (fun c -> c.DataBounds()))
            |> Seq.toArray

        let boundsXs = bounds |> Array.collect (fun b -> [| b.XMin; b.XMax |])
        let boundsYs = bounds |> Array.collect (fun b -> [| b.YMin; b.YMax |])
        let lineXs = lines |> Seq.collect (fun l -> l.XData) |> Seq.toArray
        let lineYs = lines |> Seq.collect (fun l -> l.YData) |> Seq.toArray
        let xs = Array.append lineXs boundsXs |> finite
        let ys = Array.append lineYs boundsYs |> finite

        if this.XLimAuto && xs.Length > 0 then
            this.XLim <-
                if this.XAxis.Scale.Name = "log" then
                    AxesLayout.logLimits xs
                else
                    AxesLayout.marginExpandSticky stickyX (Array.min xs) (Array.max xs)

        if this.YLimAuto && ys.Length > 0 then
            this.YLim <-
                if this.YAxis.Scale.Name = "log" then
                    AxesLayout.logLimits ys
                else
                    AxesLayout.marginExpandSticky stickyY (Array.min ys) (Array.max ys)

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
        let xScale = this.XAxis.Scale
        let yScale = this.YAxis.Scale
        let xView = xScale.ClampLimits this.XLim
        let yView = yScale.ClampLimits this.YLim

        let transScale =
            FunctionalTransform(xScale.TransformValue, yScale.TransformValue, xScale.InverseValue, yScale.InverseValue)
            :> ITransform

        let scaledBox =
            BBox.fromExtents
                (xScale.TransformValue xView.Lower)
                (yScale.TransformValue yView.Lower)
                (xScale.TransformValue xView.Upper)
                (yScale.TransformValue yView.Upper)

        let transLimits = BBoxTransform(scaledBox, BBox.unit) :> ITransform
        let transData = Transforms.compose (Transforms.compose transScale transLimits) transAxes
        let pt2px = renderer.Dpi / 72.0
        let nbinsX = AxesLayout.tickBins (abs box.Width) rc.TickLabelSize 3.0 pt2px
        let nbinsY = AxesLayout.tickBins (abs box.Height) rc.TickLabelSize 2.0 pt2px

        let xTicks =
            (xScale.CreateLocator nbinsX).TickValues xView
            |> Array.filter (AxesLayout.inView xView)

        let yTicks =
            (yScale.CreateLocator nbinsY).TickValues yView
            |> Array.filter (AxesLayout.inView yView)

        let minorOf (scale: IScale) (ticks: float[]) (view: Interval) =
            if not this.MinorTicksEnabled then [||]
            elif scale.Name = "log" then AxesLayout.logMinorTicks view
            else AxesLayout.minorTicks ticks view

        {
            Renderer = renderer
            Box = box
            TransAxes = transAxes
            TransData = transData
            Pt2Px = pt2px
            XView = xView
            YView = yView
            XTicks = xTicks
            XLabels = (xScale.CreateFormatter()).FormatTicks xTicks
            YTicks = yTicks
            YLabels = (yScale.CreateFormatter()).FormatTicks yTicks
            XMinor = minorOf xScale xTicks xView
            YMinor = minorOf yScale yTicks yView
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
                let x = (ctx.TransData.Transform { X = tv; Y = ctx.YView.Lower }).X
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x; Y = b.Y0 }; { X = x; Y = b.Y1 } ], None)

        if this.YAxis.ShowGrid then
            for tv in ctx.YTicks do
                let y = (ctx.TransData.Transform { X = ctx.XView.Lower; Y = tv }).Y
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = b.X0; Y = y }; { X = b.X1; Y = y } ], None)

    member private this.DrawData(ctx: AxesDrawContext) =
        // Images (zorder 0) sit beneath everything else.
        for image in images do
            image.Transform <- ctx.TransData
            image.Draw ctx.Renderer

        // Patches (zorder 1) are drawn beneath lines (zorder 2).
        for patch in patches do
            patch.Transform <- ctx.TransData
            patch.Draw ctx.Renderer

        for collection in collections do
            collection.Transform <- ctx.TransData
            collection.Draw ctx.Renderer

        for line in lines do
            line.Transform <- ctx.TransData
            line.Draw ctx.Renderer

    member private this.DrawTexts(ctx: AxesDrawContext) =
        for artist in overlays do
            artist.Transform <- ctx.TransData
            artist.Draw ctx.Renderer

    member private this.DrawSpines(ctx: AxesDrawContext) =
        let edges =
            [
                this.SpineBottom, ({ X = 0.0; Y = 0.0 }, { X = 1.0; Y = 0.0 })
                this.SpineTop, ({ X = 0.0; Y = 1.0 }, { X = 1.0; Y = 1.0 })
                this.SpineLeft, ({ X = 0.0; Y = 0.0 }, { X = 0.0; Y = 1.0 })
                this.SpineRight, ({ X = 1.0; Y = 0.0 }, { X = 1.0; Y = 1.0 })
            ]

        for (visible, (a, b)) in edges do
            if visible then
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
        let dir = this.TickDirection
        let labelOff = if dir = "in" then pad else len + pad

        let gc =
            { GraphicsContext.Default with
                StrokeColor = rc.TickColor
                LineWidth = rc.TickMajorWidth * ctx.Pt2Px
            }

        Array.iter2
            (fun tv lab ->
                let x = (ctx.TransData.Transform { X = tv; Y = ctx.YView.Lower }).X
                let y0, y1 = AxesLayout.tickEndpoints b.Y0 len dir
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x; Y = y0 }; { X = x; Y = y1 } ], None)
                (this.MakeTickLabel(x, b.Y0 - labelOff, lab, HCenter, VTop)).Draw ctx.Renderer)
            ctx.XTicks
            ctx.XLabels

        Array.iter2
            (fun tv lab ->
                let y = (ctx.TransData.Transform { X = ctx.XView.Lower; Y = tv }).Y
                let x0, x1 = AxesLayout.tickEndpoints b.X0 len dir
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x0; Y = y }; { X = x1; Y = y } ], None)
                (this.MakeTickLabel(b.X0 - labelOff, y, lab, HRight, VCenter)).Draw ctx.Renderer)
            ctx.YTicks
            ctx.YLabels

    member private this.DrawMinorTicks(ctx: AxesDrawContext) =
        if this.MinorTicksEnabled then
            let b = ctx.Box
            let len = rc.TickMinorSize * ctx.Pt2Px
            let dir = this.TickDirection

            let gc =
                { GraphicsContext.Default with
                    StrokeColor = rc.TickColor
                    LineWidth = rc.TickMinorWidth * ctx.Pt2Px
                }

            for tv in ctx.XMinor do
                let x = (ctx.TransData.Transform { X = tv; Y = ctx.YView.Lower }).X
                let y0, y1 = AxesLayout.tickEndpoints b.Y0 len dir
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x; Y = y0 }; { X = x; Y = y1 } ], None)

            for tv in ctx.YMinor do
                let y = (ctx.TransData.Transform { X = ctx.XView.Lower; Y = tv }).Y
                let x0, x1 = AxesLayout.tickEndpoints b.X0 len dir
                ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x0; Y = y }; { X = x1; Y = y } ], None)

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

    /// <summary>Lower-left x and top y of the legend box for a concrete location.</summary>
    member private _.LegendBoxPosition
        (loc: LegendLoc, boxW: float, boxH: float, b: BBox, inset: float)
        : float * float =
        let x0 =
            match loc with
            | UpperLeft
            | LowerLeft
            | CenterLeft -> b.XMin + inset
            | UpperCenter
            | LowerCenter
            | Center -> b.CenterX - boxW / 2.0
            | Best
            | UpperRight
            | LowerRight
            | CenterRight -> b.XMax - inset - boxW

        let y1 =
            match loc with
            | Best
            | UpperLeft
            | UpperRight
            | UpperCenter -> b.YMax - inset
            | CenterLeft
            | CenterRight
            | Center -> b.CenterY + boxH / 2.0
            | LowerLeft
            | LowerRight
            | LowerCenter -> b.YMin + inset + boxH

        x0, y1

    /// <summary>Choose the location whose legend box overlaps the fewest data points.</summary>
    member private this.BestLegendLoc(ctx: AxesDrawContext, boxW: float, boxH: float, inset: float) : LegendLoc =
        let b = ctx.Box

        let pts =
            seq {
                for line in lines do
                    for i in 0 .. line.XData.Length - 1 do
                        yield ctx.TransData.Transform { X = line.XData[i]; Y = line.YData[i] }

                for patch in patches do
                    match patch.DataBounds() with
                    | Some db ->
                        yield ctx.TransData.Transform { X = db.XMin; Y = db.YMin }
                        yield ctx.TransData.Transform { X = db.XMax; Y = db.YMax }
                        yield ctx.TransData.Transform { X = db.XMin; Y = db.YMax }
                        yield ctx.TransData.Transform { X = db.XMax; Y = db.YMin }
                    | None -> ()
            }
            |> Seq.toArray

        let candidates =
            [
                UpperRight
                UpperLeft
                LowerLeft
                LowerRight
                CenterRight
                CenterLeft
                LowerCenter
                UpperCenter
                Center
            ]

        let overlap loc =
            let x0, y1 = this.LegendBoxPosition(loc, boxW, boxH, b, inset)
            let x1 = x0 + boxW
            let y0 = y1 - boxH

            pts
            |> Array.filter (fun p -> p.X >= x0 && p.X <= x1 && p.Y >= y0 && p.Y <= y1)
            |> Array.length

        candidates |> List.minBy overlap

    member private this.DrawLegend(ctx: AxesDrawContext) =
        // A legend entry is (label, color, lineWidth option). Some => line sample,
        // None => filled patch swatch.
        let lineEntries =
            lines
            |> Seq.filter (fun l -> l.Label <> "")
            |> Seq.map (fun l -> l.Label, l.Color, Some l.LineWidth)

        let patchEntries =
            patches
            |> Seq.filter (fun p -> p.Label <> "")
            |> Seq.map (fun p -> p.Label, p.FaceColor, None)

        let entries = Seq.append lineEntries patchEntries |> Seq.toList

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
            let swatchH = 0.7 * rc.FontSize * ctx.Pt2Px

            let textW =
                entries
                |> List.map (fun (label, _, _) -> (ctx.Renderer.MeasureText(label, font)).Width)
                |> List.max

            let boxW = pad + sample + gap + textW + pad
            let boxH = pad + lineH * float entries.Length + pad
            let inset = 0.5 * rc.FontSize * ctx.Pt2Px

            let resolvedLoc =
                match this.LegendLoc with
                | Best -> this.BestLegendLoc(ctx, boxW, boxH, inset)
                | l -> l

            let x0, y1 = this.LegendBoxPosition(resolvedLoc, boxW, boxH, b, inset)
            let x1 = x0 + boxW
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
            |> List.iteri (fun i (label, color, lineWidth) ->
                let cy = y1 - pad - lineH * (float i + 0.5)
                let sx0 = x0 + pad

                match lineWidth with
                | Some lw ->
                    let sampleGc =
                        { GraphicsContext.Default with
                            StrokeColor = color
                            LineWidth = lw * ctx.Pt2Px
                        }

                    ctx.Renderer.DrawPath(
                        sampleGc,
                        Path.polyline [ { X = sx0; Y = cy }; { X = sx0 + sample; Y = cy } ],
                        None
                    )
                | None ->
                    let swatch =
                        [
                            { X = sx0; Y = cy - swatchH / 2.0 }
                            {
                                X = sx0 + sample
                                Y = cy - swatchH / 2.0
                            }
                            {
                                X = sx0 + sample
                                Y = cy + swatchH / 2.0
                            }
                            { X = sx0; Y = cy + swatchH / 2.0 }
                        ]

                    let swatchGc =
                        { GraphicsContext.Default with
                            StrokeColor = Color.none
                            LineWidth = 0.0
                        }

                    ctx.Renderer.DrawPath(swatchGc, Path.polygon swatch, Some color)

                let t = Text(sx0 + sample + gap, cy, label)
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
        this.DrawMinorTicks ctx
        this.DrawTicks ctx
        this.DrawAxisLabelsAndTitle ctx
        this.DrawTexts ctx
        this.DrawLegend ctx
