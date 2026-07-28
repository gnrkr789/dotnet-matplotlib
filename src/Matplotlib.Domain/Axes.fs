namespace Matplotlib.Domain

open System
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Transforms
open Matplotlib.Domain.Ticking
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
    /// is split into 5 sub-intervals when round(mantissa) is 1/5/10, else 4.
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

                // matplotlib's AutoMinorLocator: 5 sub-intervals when round(mantissa)
                // is 1, 5 or 10, else 4 (round-half-to-even, as in numpy).
                let ndivs =
                    match round mantissa with
                    | 1.0
                    | 5.0
                    | 10.0 -> 5
                    | _ -> 4

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
    /// Iso-line segments of a 2D field at the given level, in data coordinates
    /// (x = column, y = row).
    /// </summary>
    /// <remarks>Ported from the marching-squares contour generator behind <c>contour</c>.</remarks>
    let marchingSquares (z: float[,]) (level: float) : (Point2D * Point2D) list =
        let rows = Array2D.length1 z
        let cols = Array2D.length2 z
        let result = ResizeArray<Point2D * Point2D>()

        let interp v0 (p0: Point2D) v1 (p1: Point2D) =
            let t = if v1 = v0 then 0.5 else (level - v0) / (v1 - v0)

            {
                X = p0.X + (p1.X - p0.X) * t
                Y = p0.Y + (p1.Y - p0.Y) * t
            }

        for i in 0 .. rows - 2 do
            for j in 0 .. cols - 2 do
                let blV, brV, trV, tlV = z[i, j], z[i, j + 1], z[i + 1, j + 1], z[i + 1, j]
                let blP = { X = float j; Y = float i }
                let brP = { X = float (j + 1); Y = float i }
                let trP = { X = float (j + 1); Y = float (i + 1) }
                let tlP = { X = float j; Y = float (i + 1) }
                let bit v = if v >= level then 1 else 0
                let case = bit blV + 2 * bit brV + 4 * bit trV + 8 * bit tlV
                let pB = interp blV blP brV brP
                let pR = interp brV brP trV trP
                let pT = interp trV trP tlV tlP
                let pL = interp tlV tlP blV blP

                match case with
                | 1
                | 14 -> result.Add(pL, pB)
                | 2
                | 13 -> result.Add(pB, pR)
                | 3
                | 12 -> result.Add(pL, pR)
                | 4
                | 11 -> result.Add(pR, pT)
                | 6
                | 9 -> result.Add(pB, pT)
                | 7
                | 8 -> result.Add(pL, pT)
                | 5 ->
                    // Saddle: disambiguate with the cell-centre value (mean of the
                    // four corners), as matplotlib's mpl2014 contour generator does.
                    // A high centre encloses the two low corners; a low centre the high.
                    let center = (blV + brV + trV + tlV) / 4.0

                    if center >= level then
                        result.Add(pB, pR)
                        result.Add(pT, pL)
                    else
                        result.Add(pL, pB)
                        result.Add(pR, pT)
                | 10 ->
                    let center = (blV + brV + trV + tlV) / 4.0

                    if center >= level then
                        result.Add(pL, pB)
                        result.Add(pR, pT)
                    else
                        result.Add(pB, pR)
                        result.Add(pT, pL)
                | _ -> ()

        List.ofSeq result

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

    /// <summary>
    /// Width in pixels of the widest of <paramref name="labels"/>, for reserving
    /// margin space.
    /// </summary>
    /// <remarks>
    /// Uses the same fixed <c>0.6 em</c> per character as every backend's
    /// <c>MeasureText</c> rather than measured glyph advances: the figure layout
    /// must not depend on which fonts a machine has installed, or the vector
    /// output would stop being byte-reproducible (see <c>Report.Sha256</c>).
    /// </remarks>
    let tickLabelWidth (labelSizePts: float) (pt2px: float) (labels: string seq) : float =
        let longest = labels |> Seq.fold (fun acc (s: string) -> max acc s.Length) 0
        float longest * 0.6 * labelSizePts * pt2px

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
    // reference lines/spans drawn with a blended transform (data on one axis,
    // axes-fraction on the other): axhline / axvline / axhspan / axvspan.
    let refHLines = ResizeArray<float * float * float * Color>() // y, xminFrac, xmaxFrac, color
    let refVLines = ResizeArray<float * float * float * Color>() // x, yminFrac, ymaxFrac, color
    let refHSpans = ResizeArray<float * float * Color>() // ymin, ymax, color (full width)
    let refVSpans = ResizeArray<float * float * Color>() // xmin, xmax, color (full height)
    let cycler = PropertyCycler.CreateDefault()

    /// <summary>Create an Axes with the default <c>rcParams</c>.</summary>
    new() = Axes(RcParams.Default)

    /// <summary>The active rcParams snapshot.</summary>
    member _.Rc = rc

    /// <summary>Axes position within the figure, in figure fractions.</summary>
    member val Position = BBox.fromExtents rc.SubplotLeft rc.SubplotBottom rc.SubplotRight rc.SubplotTop with get, set

    /// <summary>Background (face) color of the data area.</summary>
    member val FaceColor = rc.AxesFaceColor with get, set

    /// <summary>When set (a <c>twinx</c> overlay), this axes draws against the source axes' shared x range/scale.</summary>
    member val SharedXFrom: Axes option = None with get, set

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

    /// <summary>Whether x-axis ticks and tick labels are drawn.</summary>
    member val XTicksVisible = true with get, set

    /// <summary>Whether y-axis ticks and tick labels are drawn.</summary>
    member val YTicksVisible = true with get, set

    /// <summary>Data aspect: <c>auto</c> (default) or <c>equal</c> (one data unit spans equal pixels on both axes).</summary>
    member val Aspect = "auto" with get, set

    /// <summary>Which side the y ticks/labels are on (<c>left</c> default, or <c>right</c>).</summary>
    member val YTickSide = "left" with get, set

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

    member private _.DataRange(data: float[,], vmin: float option, vmax: float option) : float * float =
        let rows = Array2D.length1 data
        let cols = Array2D.length2 data

        let flat =
            [|
                for i in 0 .. rows - 1 do
                    for j in 0 .. cols - 1 -> data[i, j]
            |]

        defaultArg vmin (Array.min flat), defaultArg vmax (Array.max flat)

    /// <summary>Display a 2D array as a colormapped image (Matplotlib's <c>imshow</c>).</summary>
    member this.Imshow(data: float[,], ?cmap: string, ?vmin: float, ?vmax: float) : AxesImage =
        let rows = Array2D.length1 data
        let cols = Array2D.length2 data
        let lo, hi = this.DataRange(data, vmin, vmax)
        let colormap = Colormap.byName (defaultArg cmap "viridis")
        let xEdges = Array.init (cols + 1) (fun j -> float j - 0.5)
        let yEdges = Array.init (rows + 1) (fun i -> float i - 0.5)
        let image = AxesImage(data, colormap, Normalize(lo, hi), xEdges, yEdges)
        images.Add image
        // origin 'upper': row 0 at top, so the y-axis is inverted.
        this.SetXLim(-0.5, float cols - 0.5)
        this.SetYLim(float rows - 0.5, -0.5)
        image

    /// <summary>Draw a quadrilateral mesh of a 2D array (Matplotlib's <c>pcolormesh</c>, origin lower).</summary>
    member this.Pcolormesh(data: float[,], ?cmap: string, ?vmin: float, ?vmax: float) : AxesImage =
        let rows = Array2D.length1 data
        let cols = Array2D.length2 data
        let lo, hi = this.DataRange(data, vmin, vmax)
        let colormap = Colormap.byName (defaultArg cmap "viridis")

        let mesh =
            AxesImage(data, colormap, Normalize(lo, hi), Array.init (cols + 1) float, Array.init (rows + 1) float)

        images.Add mesh
        this.SetXLim(0.0, float cols)
        this.SetYLim(0.0, float rows)
        mesh

    /// <summary>Draw contour lines of a 2D field (Matplotlib's <c>contour</c>).</summary>
    member this.Contour(data: float[,], ?levels: float[], ?cmap: string) : float[] =
        let lo, hi = this.DataRange(data, None, None)
        let levs = defaultArg levels [| for k in 1..6 -> lo + float k / 7.0 * (hi - lo) |]
        let colormap = Colormap.byName (defaultArg cmap "viridis")
        let norm = Normalize(lo, hi)

        for level in levs do
            let segments = AxesLayout.marchingSquares data level |> List.map (fun (a, b) -> [| a; b |])

            if not segments.IsEmpty then
                let collection = LineCollection segments
                collection.Color <- colormap.Apply(norm.Normalize level)
                collection.LineWidth <- 1.0
                collections.Add collection

        this.SetXLim(0.0, float (Array2D.length2 data - 1))
        this.SetYLim(0.0, float (Array2D.length1 data - 1))
        levs

    /// <summary>Draw filled contour bands of a 2D field (Matplotlib's <c>contourf</c>).</summary>
    /// <remarks>Approximated by colouring each cell with its level band's color.</remarks>
    member this.Contourf(data: float[,], ?levels: float[], ?cmap: string) : float[] =
        let lo, hi = this.DataRange(data, None, None)
        let boundaries = defaultArg levels [| for k in 0..10 -> lo + float k / 10.0 * (hi - lo) |]
        let rows = Array2D.length1 data
        let cols = Array2D.length2 data
        let colormap = Colormap.byName (defaultArg cmap "viridis")
        let norm = Normalize(lo, hi)

        // Sutherland–Hodgman clip: keep the part of a (x,y,z) polygon on one side of
        // the plane z = level (the band edge), interpolating z linearly along edges.
        let clip (level: float) (keepBelow: bool) (poly: (float * float * float)[]) : (float * float * float)[] =
            if poly.Length = 0 then
                [||]
            else
                let inside (_, _, z) = if keepBelow then z <= level else z >= level
                let result = ResizeArray<float * float * float>()

                for i in 0 .. poly.Length - 1 do
                    let cur = poly[i]
                    let nxt = poly[(i + 1) % poly.Length]
                    let (cx, cy, cz) = cur
                    let (nx2, ny2, nz) = nxt
                    let curIn = inside cur

                    if curIn then
                        result.Add cur

                    if curIn <> inside nxt then
                        let t = if nz = cz then 0.0 else (level - cz) / (nz - cz)
                        result.Add(cx + t * (nx2 - cx), cy + t * (ny2 - cy), level)

                result.ToArray()

        // each grid cell is clipped to every band it overlaps, producing iso-bands
        // whose boundaries follow the contour lines (not the cell grid).
        for i in 0 .. rows - 2 do
            for j in 0 .. cols - 2 do
                let corners =
                    [|
                        float j, float i, data[i, j]
                        float (j + 1), float i, data[i, j + 1]
                        float (j + 1), float (i + 1), data[i + 1, j + 1]
                        float j, float (i + 1), data[i + 1, j]
                    |]

                let zs = corners |> Array.map (fun (_, _, z) -> z)
                let cellMin, cellMax = Array.min zs, Array.max zs

                for k in 0 .. boundaries.Length - 2 do
                    let a, b = boundaries[k], boundaries[k + 1]

                    if cellMax >= a && cellMin <= b then
                        let band = clip b true (clip a false corners)

                        if band.Length >= 3 then
                            let poly = Polygon(band |> Array.map (fun (x, y, _) -> { X = x; Y = y }))
                            poly.FaceColor <- colormap.Apply(norm.Normalize((a + b) / 2.0))
                            patches.Add poly

        this.SetXLim(0.0, float (cols - 1))
        this.SetYLim(0.0, float (rows - 1))
        boundaries

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
    /// <param name="s">Marker area in points squared (Matplotlib/MATLAB <c>s</c>/<c>sz</c>; default 36).</param>
    /// <param name="c">Per-point values mapped to colors through <paramref name="cmap"/>.</param>
    member this.Scatter
        (
            xs: float[],
            ys: float[],
            ?color: Color,
            ?marker: MarkerStyle,
            ?s: float,
            ?label: string,
            ?c: float[],
            ?cmap: string,
            ?vmin: float,
            ?vmax: float,
            ?sizes: float[]
        ) : Line2D =
        let line = Line2D(xs, ys)
        line.LineStyle <- NoLine
        line.Color <- defaultArg color (cycler.Next())
        line.Marker <- defaultArg marker MarkerStyle.Circle
        // `s` is an AREA in points^2 (default 36), so the marker diameter is sqrt(s).
        line.MarkerSize <- sqrt (defaultArg s 36.0)
        line.Label <- defaultArg label ""

        // Per-point sizes (an array of areas in points^2) -> per-point diameters.
        match sizes with
        | Some areas when areas.Length > 0 -> line.MarkerSizes <- Some(areas |> Array.map sqrt)
        | _ -> ()

        // A numeric `c` array is mapped through a colormap + Normalize (default viridis).
        match c with
        | Some values when values.Length > 0 ->
            let lo = defaultArg vmin (Array.min values)
            let hi = defaultArg vmax (Array.max values)
            let colormap = Colormap.byName (defaultArg cmap "viridis")
            let norm = Normalize(lo, hi)
            line.MarkerColors <- Some(values |> Array.map (fun v -> colormap.Apply(norm.Normalize v)))
            line.ScalarMappable <- Some(colormap, norm) // lets plt.colorbar(sc) read the mapping
        | _ -> ()

        lines.Add line
        this.Autoscale()
        line

    /// <summary>Draw a vertical bar chart (Matplotlib's <c>bar</c>, center-aligned).</summary>
    member this.Bar
        (
            x: float[],
            height: float[],
            ?width: float,
            ?bottom: float[],
            ?color: Color,
            ?label: string,
            ?yerr: float[],
            ?capsize: float
        ) : Rectangle[] =
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

        // y error bars at the bar tops (Matplotlib draws these in black by default)
        match yerr with
        | Some err ->
            let cap = defaultArg capsize 0.0
            let tops = Array.init x.Length (fun i -> bottoms[i] + height[i])

            for i in 0 .. x.Length - 1 do
                let bar = Line2D([| x[i]; x[i] |], [| tops[i] - err[i]; tops[i] + err[i] |])
                bar.Color <- Color.black
                bar.LineWidth <- rc.LinesLineWidth
                lines.Add bar

            if cap > 0.0 then
                let caps =
                    Line2D(
                        Array.append x x,
                        Array.append
                            (Array.init x.Length (fun i -> tops[i] - err[i]))
                            (Array.init x.Length (fun i -> tops[i] + err[i]))
                    )

                caps.LineStyle <- NoLine
                caps.Marker <- MarkerStyle.HLine
                caps.MarkerSize <- cap
                caps.Color <- Color.black
                caps.MarkerEdgeColor <- Some Color.black
                lines.Add caps
        | None -> ()

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

    /// <summary>Draw a histogram of <paramref name="x"/> (Matplotlib's <c>hist</c>, <c>histtype='bar'</c>).</summary>
    /// <returns>The bar heights (counts, or densities) and the bin edges.</returns>
    member this.Hist
        (x: float[], ?bins: int, ?range: float * float, ?density: bool, ?color: Color, ?label: string)
        : float[] * float[] =
        let nbins = max 1 (defaultArg bins 10)

        let lo, hi =
            match range with
            | Some(a, b) -> a, b
            | None when x.Length > 0 -> Array.min x, Array.max x
            | None -> 0.0, 1.0

        // Guard a degenerate range so the bin width stays positive.
        let lo, hi = if hi > lo then lo, hi else lo, lo + 1.0
        let binWidth = (hi - lo) / float nbins
        let edges = Array.init (nbins + 1) (fun i -> lo + float i * binWidth)
        let counts = Array.zeroCreate nbins

        for v in x do
            // Values land in [edge[i], edge[i+1]); the last bin is closed at hi (like numpy).
            if v >= lo && v <= hi then
                let idx = min (nbins - 1) (int ((v - lo) / binWidth))
                counts[idx] <- counts[idx] + 1.0

        // density=True normalizes so the total area is 1: count / (N * binWidth).
        let heights =
            if defaultArg density false then
                let n = float x.Length
                counts |> Array.map (fun cnt -> if n > 0.0 then cnt / (n * binWidth) else 0.0)
            else
                counts

        let faceColor = defaultArg color (cycler.Next())
        let lbl = defaultArg label ""

        let rects =
            Array.init nbins (fun i ->
                let rect = Rectangle(edges[i], 0.0, binWidth, heights[i])
                rect.FaceColor <- faceColor
                rect.Label <- if i = 0 then lbl else ""
                rect)

        for rect in rects do
            patches.Add rect

        stickyY.Add 0.0 // the bars rest on the baseline (no y margin there)
        this.Autoscale()
        heights, edges

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

    /// <summary>Fill the area between two vertical curves (Matplotlib's <c>fill_betweenx</c>).</summary>
    member this.FillBetweenx
        (y: float[], x1: float[], ?x2: float[], ?color: Color, ?alpha: float, ?label: string)
        : Polygon =
        let left = defaultArg x2 (Array.zeroCreate y.Length)
        let faceColor = (defaultArg color (cycler.Next())).WithAlpha(defaultArg alpha 1.0)

        let forward = Array.init y.Length (fun i -> { X = x1[i]; Y = y[i] })

        let backward =
            Array.init y.Length (fun i ->
                {
                    X = left[y.Length - 1 - i]
                    Y = y[y.Length - 1 - i]
                })

        let polygon = Polygon(Array.append forward backward)
        polygon.FaceColor <- faceColor
        polygon.Label <- defaultArg label ""
        patches.Add polygon
        this.Autoscale()
        polygon

    /// <summary>Draw stacked filled areas (Matplotlib's <c>stackplot</c>).</summary>
    member this.Stackplot(x: float[], ys: float[][], ?colors: Color[], ?labels: string[]) : Polygon[] =
        let n = x.Length
        let cols = defaultArg colors [||]
        let lbls = defaultArg labels [||]
        let baseline = Array.zeroCreate n // running cumulative top of the stack
        let polys = ResizeArray<Polygon>()

        ys
        |> Array.iteri (fun k y ->
            let top = Array.init n (fun i -> baseline[i] + y[i])
            let forward = Array.init n (fun i -> { X = x[i]; Y = top[i] })

            let backward =
                Array.init n (fun i ->
                    {
                        X = x[n - 1 - i]
                        Y = baseline[n - 1 - i]
                    })

            let poly = Polygon(Array.append forward backward)
            poly.FaceColor <- if k < cols.Length then cols[k] else cycler.Next()
            poly.Label <- if k < lbls.Length then lbls[k] else ""
            patches.Add poly
            polys.Add poly
            Array.blit top 0 baseline 0 n)

        this.Autoscale()
        polys.ToArray()

    /// <summary>Draw vertical line segments (Matplotlib's <c>vlines</c>).</summary>
    member this.Vlines(x: float[], ymin: float[], ymax: float[], ?color: Color, ?label: string) : unit =
        let col = defaultArg color (cycler.Next())

        for i in 0 .. x.Length - 1 do
            let seg = Line2D([| x[i]; x[i] |], [| ymin[i]; ymax[i] |])
            seg.Color <- col
            seg.LineWidth <- rc.LinesLineWidth
            seg.Label <- if i = 0 then defaultArg label "" else ""
            lines.Add seg

        this.Autoscale()

    /// <summary>Draw horizontal line segments (Matplotlib's <c>hlines</c>).</summary>
    member this.Hlines(y: float[], xmin: float[], xmax: float[], ?color: Color, ?label: string) : unit =
        let col = defaultArg color (cycler.Next())

        for i in 0 .. y.Length - 1 do
            let seg = Line2D([| xmin[i]; xmax[i] |], [| y[i]; y[i] |])
            seg.Color <- col
            seg.LineWidth <- rc.LinesLineWidth
            seg.Label <- if i = 0 then defaultArg label "" else ""
            lines.Add seg

        this.Autoscale()

    /// <summary>Draw a pie chart of <paramref name="values"/> (Matplotlib's <c>pie</c>).</summary>
    member this.Pie(values: float[], ?labels: string[], ?colors: Color[], ?startAngle: float) : Polygon[] =
        let total = Array.sum values
        let cols = defaultArg colors [||]
        let lbls = defaultArg labels [||]
        let wedges = ResizeArray<Polygon>()
        let mutable theta0 = (defaultArg startAngle 0.0) * Math.PI / 180.0

        values
        |> Array.iteri (fun k v ->
            let frac = if total > 0.0 then v / total else 0.0
            let theta1 = theta0 + 2.0 * Math.PI * frac
            // flatten the arc to ~5-degree segments
            let steps = max 2 (int (ceil (abs (theta1 - theta0) / (Math.PI / 36.0))))

            let arc =
                [|
                    for s in 0..steps ->
                        let a = theta0 + (theta1 - theta0) * float s / float steps
                        { X = cos a; Y = sin a }
                |]

            let wedge = Polygon(Array.append [| { X = 0.0; Y = 0.0 } |] arc)
            wedge.FaceColor <- if k < cols.Length then cols[k] else cycler.Next()
            wedge.EdgeColor <- Some(Color.rgb 1.0 1.0 1.0)
            wedge.Label <- if k < lbls.Length then lbls[k] else ""
            patches.Add wedge
            wedges.Add wedge
            theta0 <- theta1)

        // the pie lives in the unit circle; equal aspect keeps it round, and the
        // axes frame and ticks are dropped.
        this.SetXLim(-1.3, 1.3)
        this.SetYLim(-1.3, 1.3)
        this.SetAspect "equal"
        this.SetAxisOff()
        wedges.ToArray()

    /// <summary>Render a grid of text cells filling the axes (Matplotlib's <c>table</c>).</summary>
    /// <remarks>An optional header row (<paramref name="colLabels"/>) is shaded; the axes frame and ticks are dropped.</remarks>
    member this.Table(cellText: string[][], ?colLabels: string[]) : unit =
        let nrows = cellText.Length
        let ncols = if nrows = 0 then 0 else cellText[0].Length
        let headerRows = if colLabels.IsSome then 1 else 0
        let totalRows = nrows + headerRows

        this.SetAxisOff()
        this.SetXLim(0.0, 1.0)
        this.SetYLim(0.0, 1.0)

        if totalRows > 0 && ncols > 0 then
            let rowH = 1.0 / float totalRows
            let colW = 1.0 / float ncols
            let edge = Color.fromHex "#cccccc"
            let headerFill = Color.fromHex "#dddddd"

            let cell (rowIndex: int) (c: int) (text: string) (isHeader: bool) =
                let x = float c * colW
                let y = 1.0 - float (rowIndex + 1) * rowH
                let rect = Rectangle(x, y, colW, rowH)
                rect.EdgeColor <- Some edge
                rect.Fill <- isHeader

                if isHeader then
                    rect.FaceColor <- headerFill

                patches.Add rect

                this.Text(x + colW / 2.0, y + rowH / 2.0, text, fontSize = rc.FontSize * 0.9, hAlign = HCenter, vAlign = VCenter)
                |> ignore

            match colLabels with
            | Some labels ->
                for c in 0 .. ncols - 1 do
                    cell 0 c (if c < labels.Length then labels[c] else "") true
            | None -> ()

            for r in 0 .. nrows - 1 do
                let row = cellText[r]

                for c in 0 .. ncols - 1 do
                    cell (r + headerRows) c (if c < row.Length then row[c] else "") false

    /// <summary>Draw a horizontal reference line at <paramref name="y"/> spanning the axes (Matplotlib's <c>axhline</c>).</summary>
    member this.AxHLine(y: float, ?xmin: float, ?xmax: float, ?color: Color) =
        refHLines.Add(y, defaultArg xmin 0.0, defaultArg xmax 1.0, defaultArg color (cycler.Next()))
        this.Autoscale()

    /// <summary>Draw a vertical reference line at <paramref name="x"/> spanning the axes (Matplotlib's <c>axvline</c>).</summary>
    member this.AxVLine(x: float, ?ymin: float, ?ymax: float, ?color: Color) =
        refVLines.Add(x, defaultArg ymin 0.0, defaultArg ymax 1.0, defaultArg color (cycler.Next()))
        this.Autoscale()

    /// <summary>Shade a full-width horizontal band between <paramref name="ymin"/>/<paramref name="ymax"/> (Matplotlib's <c>axhspan</c>).</summary>
    member this.AxHSpan(ymin: float, ymax: float, ?color: Color, ?alpha: float) =
        refHSpans.Add(ymin, ymax, (defaultArg color (cycler.Next())).WithAlpha(defaultArg alpha 0.5))
        this.Autoscale()

    /// <summary>Shade a full-height vertical band between <paramref name="xmin"/>/<paramref name="xmax"/> (Matplotlib's <c>axvspan</c>).</summary>
    member this.AxVSpan(xmin: float, xmax: float, ?color: Color, ?alpha: float) =
        refVSpans.Add(xmin, xmax, (defaultArg color (cycler.Next())).WithAlpha(defaultArg alpha 0.5))
        this.Autoscale()

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
            ?capsize: float,
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

        // capsize is the cap length in points; drawn as '_'/'|' markers (which are
        // sized in points at draw time), as matplotlib does. Default 0 -> no caps.
        let cap = defaultArg capsize 0.0

        let addCaps (markerStyle: MarkerStyle) (cxs: float[]) (cys: float[]) =
            if cap > 0.0 then
                let caps = Line2D(cxs, cys)
                caps.Color <- col
                caps.LineStyle <- NoLine
                caps.Marker <- markerStyle
                caps.MarkerSize <- cap
                caps.MarkerEdgeColor <- Some col
                caps.MarkerEdgeWidth <- rc.LinesLineWidth
                lines.Add caps

        match yerr with
        | Some err ->
            for i in 0 .. x.Length - 1 do
                addBar [| x[i]; x[i] |] [| y[i] - err[i]; y[i] + err[i] |]

            addCaps
                MarkerStyle.HLine
                (Array.append x x)
                (Array.append
                    (Array.init x.Length (fun i -> y[i] - err[i]))
                    (Array.init x.Length (fun i -> y[i] + err[i])))
        | None -> ()

        match xerr with
        | Some err ->
            for i in 0 .. x.Length - 1 do
                addBar [| x[i] - err[i]; x[i] + err[i] |] [| y[i]; y[i] |]

            addCaps
                MarkerStyle.VLine
                (Array.append
                    (Array.init x.Length (fun i -> x[i] - err[i]))
                    (Array.init x.Length (fun i -> x[i] + err[i])))
                (Array.append y y)
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

    /// <summary>Draw a field of arrows from <c>(x,y)</c> with components <c>(u,v)</c> (Matplotlib's <c>quiver</c>).</summary>
    member this.Quiver(x: float[], y: float[], u: float[], v: float[], ?scale: float, ?color: Color) : unit =
        let s = defaultArg scale 1.0
        let col = defaultArg color (cycler.Next())

        for i in 0 .. x.Length - 1 do
            let x0, y0 = x[i], y[i]
            let x1, y1 = x0 + u[i] * s, y0 + v[i] * s
            let shaft = Line2D([| x0; x1 |], [| y0; y1 |])
            shaft.Color <- col
            shaft.LineWidth <- rc.LinesLineWidth
            lines.Add shaft
            let dx, dy = x1 - x0, y1 - y0
            let len = sqrt (dx * dx + dy * dy)

            if len > 1e-12 then
                let ang = atan2 dy dx
                let hl, hw = 0.3 * len, 0.16 * len
                let bx, by = x1 - hl * cos ang, y1 - hl * sin ang

                let head =
                    Polygon(
                        [|
                            { X = x1; Y = y1 }
                            {
                                X = bx - hw * sin ang
                                Y = by + hw * cos ang
                            }
                            {
                                X = bx + hw * sin ang
                                Y = by - hw * cos ang
                            }
                        |]
                    )

                head.FaceColor <- col
                head.EdgeColor <- Some col
                patches.Add head

        this.Autoscale()

    /// <summary>Draw a 2D histogram as a colormapped image (Matplotlib's <c>hist2d</c>).</summary>
    member this.Hist2d(x: float[], y: float[], ?bins: int, ?cmap: string) : AxesImage =
        let nb = defaultArg bins 10
        let xmin, xmax = Array.min x, Array.max x
        let ymin, ymax = Array.min y, Array.max y
        let xw = (xmax - xmin) / float nb
        let yw = (ymax - ymin) / float nb
        let counts = Array2D.zeroCreate nb nb

        for k in 0 .. x.Length - 1 do
            let cx =
                if xw <= 0.0 then
                    0
                else
                    min (nb - 1) (max 0 (int ((x[k] - xmin) / xw)))

            let cy =
                if yw <= 0.0 then
                    0
                else
                    min (nb - 1) (max 0 (int ((y[k] - ymin) / yw)))

            counts[cy, cx] <- counts[cy, cx] + 1.0

        let mutable hi = 1.0
        Array2D.iter (fun c -> hi <- max hi c) counts
        let colormap = Colormap.byName (defaultArg cmap "viridis")
        let xEdges = Array.init (nb + 1) (fun j -> xmin + float j * xw)
        let yEdges = Array.init (nb + 1) (fun i -> ymin + float i * yw)
        let image = AxesImage(counts, colormap, Normalize(0.0, hi), xEdges, yEdges)
        images.Add image
        this.SetXLim(xmin, xmax)
        this.SetYLim(ymin, ymax)
        image

    /// <summary>Draw box-and-whisker plots of each dataset (Matplotlib's <c>boxplot</c>).</summary>
    member this.Boxplot(data: float[][], ?positions: float[], ?width: float) : unit =
        let w = defaultArg width 0.5
        let pos = defaultArg positions (Array.init data.Length (fun i -> float (i + 1)))
        let boxCol = Color.fromHex "#1f77b4"
        let medCol = Color.fromHex "#ff7f0e"

        let quantile (s: float[]) (q: float) =
            match s.Length with
            | 0 -> 0.0
            | 1 -> s[0]
            | n ->
                let p = q * float (n - 1)
                let lo = int (floor p)
                let hi = min (n - 1) (lo + 1)
                let f = p - float lo
                s[lo] * (1.0 - f) + s[hi] * f

        let addLine (xs: float[]) (ys: float[]) =
            let l = Line2D(xs, ys)
            l.Color <- boxCol
            lines.Add l

        for i in 0 .. data.Length - 1 do
            let s = Array.sort data[i]

            if s.Length > 0 then
                let p = pos[i]
                let q1 = quantile s 0.25
                let med = quantile s 0.5
                let q3 = quantile s 0.75
                let iqr = q3 - q1

                let whisLo =
                    s
                    |> Array.filter (fun v -> v >= q1 - 1.5 * iqr)
                    |> Array.tryHead
                    |> Option.defaultValue q1

                let whisHi =
                    s
                    |> Array.filter (fun v -> v <= q3 + 1.5 * iqr)
                    |> Array.tryLast
                    |> Option.defaultValue q3

                let box = Rectangle(p - w / 2.0, q1, w, iqr)
                box.EdgeColor <- Some boxCol
                box.Fill <- false
                patches.Add box

                let medLine = Line2D([| p - w / 2.0; p + w / 2.0 |], [| med; med |])
                medLine.Color <- medCol
                lines.Add medLine

                addLine [| p; p |] [| q3; whisHi |] // upper whisker
                addLine [| p; p |] [| q1; whisLo |] // lower whisker
                addLine [| p - w / 4.0; p + w / 4.0 |] [| whisHi; whisHi |] // upper cap
                addLine [| p - w / 4.0; p + w / 4.0 |] [| whisLo; whisLo |] // lower cap

                let outs = s |> Array.filter (fun v -> v < q1 - 1.5 * iqr || v > q3 + 1.5 * iqr)

                if outs.Length > 0 then
                    let fliers = Line2D(Array.create outs.Length p, outs)
                    fliers.LineStyle <- NoLine
                    fliers.Marker <- MarkerStyle.Circle
                    fliers.MarkerSize <- 4.0
                    fliers.Color <- boxCol
                    lines.Add fliers

        this.Autoscale()

    /// <summary>Draw violin plots (kernel-density estimates) of each dataset (Matplotlib's <c>violinplot</c>).</summary>
    member this.Violinplot(data: float[][], ?positions: float[], ?width: float) : unit =
        let w = defaultArg width 0.5
        let pos = defaultArg positions (Array.init data.Length (fun i -> float (i + 1)))
        let col = Color.fromHex "#1f77b4"
        let normalPdf z = exp (-0.5 * z * z) / sqrt (2.0 * System.Math.PI)

        for i in 0 .. data.Length - 1 do
            let d = data[i]

            if d.Length > 1 then
                let n = float d.Length
                let mean = Array.average d
                // Sample standard deviation (ddof = 1), matching numpy's
                // np.cov(bias=False) used by matplotlib's GaussianKDE.
                let std = sqrt (Array.sumBy (fun v -> (v - mean) * (v - mean)) d / (n - 1.0))
                // Scott's factor (GaussianKDE's default): bandwidth = std * n^(-1/(d+4)), d = 1.
                let h = if std <= 0.0 then 1.0 else std * (n ** -0.2)
                let lo, hi = Array.min d, Array.max d
                let grid = 100
                let ys = Array.init grid (fun k -> lo + (hi - lo) * float k / float (grid - 1))

                let dens =
                    ys
                    |> Array.map (fun y -> (d |> Array.sumBy (fun xj -> normalPdf ((y - xj) / h))) / (n * h))

                let maxD = Array.max dens
                let scale = if maxD <= 0.0 then 0.0 else (w / 2.0) / maxD
                let p = pos[i]
                let right = Array.map2 (fun y de -> { X = p + de * scale; Y = y }) ys dens

                let left = Array.map2 (fun y de -> { X = p - de * scale; Y = y }) ys dens |> Array.rev

                let poly = Polygon(Array.append right left)
                poly.FaceColor <- col.WithAlpha 0.5
                poly.EdgeColor <- Some col
                patches.Add poly

        this.Autoscale()

    /// <summary>Draw streamlines of a vector field on a grid (Matplotlib's <c>streamplot</c>).</summary>
    /// <remarks>RK4 integration (forward + backward) with a density occupancy mask for evenly spaced, non-overlapping lines; bilinear field sampling.</remarks>
    member this.Streamplot(x: float[], y: float[], u: float[,], v: float[,], ?density: int, ?color: Color) : unit =
        let col = defaultArg color (cycler.Next())
        let nx, ny = x.Length, y.Length
        let x0, x1 = x[0], x[nx - 1]
        let y0, y1 = y[0], y[ny - 1]
        let inDomain px py = px >= x0 && px <= x1 && py >= y0 && py <= y1

        let interp (grid: float[,]) px py =
            let fc = (px - x0) / (x1 - x0) * float (nx - 1)
            let fr = (py - y0) / (y1 - y0) * float (ny - 1)
            let c0 = min (nx - 2) (max 0 (int fc))
            let r0 = min (ny - 2) (max 0 (int fr))
            let tc = fc - float c0
            let tr = fr - float r0

            (grid[r0, c0] * (1.0 - tc) + grid[r0, c0 + 1] * tc) * (1.0 - tr)
            + (grid[r0 + 1, c0] * (1.0 - tc) + grid[r0 + 1, c0 + 1] * tc) * tr

        // unit-speed (normalized) velocity at a point, or None off-domain / at a stagnation point
        let field px py =
            if not (inDomain px py) then
                None
            else
                let uu = interp u px py
                let vv = interp v px py
                let sp = sqrt (uu * uu + vv * vv)
                if sp < 1e-9 then None else Some(uu / sp, vv / sp)

        let dt = (x1 - x0) / float (nx - 1) * 0.5
        let dens = max 1 (defaultArg density 1)
        let res = max 8 (12 * dens) // occupancy-mask resolution (coarser -> longer lines)
        let mask = Array2D.zeroCreate res res // 0 = free, else the claiming trajectory id
        let maxSteps = 1000

        let maskOf px py =
            let mc = min (res - 1) (max 0 (int ((px - x0) / (x1 - x0) * float res)))
            let mr = min (res - 1) (max 0 (int ((py - y0) / (y1 - y0) * float res)))
            mr, mc

        // one classic-RK4 step in direction sgn (+1 forward / -1 backward) on the
        // unit-speed field; None if it leaves the domain or hits a stagnation point.
        let rk4 (sgn: float) px py =
            match field px py with
            | None -> None
            | Some(k1x, k1y) ->
                let h = sgn * dt

                match field (px + 0.5 * h * k1x) (py + 0.5 * h * k1y) with
                | None -> None
                | Some(k2x, k2y) ->
                    match field (px + 0.5 * h * k2x) (py + 0.5 * h * k2y) with
                    | None -> None
                    | Some(k3x, k3y) ->
                        match field (px + h * k3x) (py + h * k3y) with
                        | None -> None
                        | Some(k4x, k4y) ->
                            let nxp = px + h / 6.0 * (k1x + 2.0 * k2x + 2.0 * k3x + k4x)
                            let nyp = py + h / 6.0 * (k1y + 2.0 * k2y + 2.0 * k3y + k4y)
                            if inDomain nxp nyp then Some(nxp, nyp) else None

        // integrate from the seed in direction sgn, claiming mask cells for `tid`; stops on
        // leaving the domain, entering another streamline's cell, or closing a loop.
        let traceDir (tid: int) (claimed: ResizeArray<int * int>) (sgn: float) (sx: float) (sy: float) =
            let pts = ResizeArray<float * float>()
            let seedCell = maskOf sx sy
            let mutable px, py = sx, sy
            let mutable cell = seedCell
            let mutable leftSeed = false
            let mutable alive = true
            let mutable k = 0

            while alive && k < maxSteps do
                match rk4 sgn px py with
                | None -> alive <- false
                | Some(nxp, nyp) ->
                    let nc = maskOf nxp nyp

                    if nc = cell then
                        px <- nxp
                        py <- nyp
                        pts.Add(px, py)
                    elif nc = seedCell && leftSeed then
                        alive <- false // closed orbit -> stop after one loop
                    else
                        let (mr, mc) = nc

                        if mask[mr, mc] <> 0 && mask[mr, mc] <> tid then
                            alive <- false // ran into another streamline's territory
                        else
                            mask[mr, mc] <- tid
                            claimed.Add(mr, mc)
                            cell <- nc
                            leftSeed <- true
                            px <- nxp
                            py <- nyp
                            pts.Add(px, py)

                k <- k + 1

            pts

        let mutable tid = 0

        // seed from each free mask cell; a trajectory claims the cells it passes through, so
        // later seeds in occupied cells are skipped -> evenly spaced, non-overlapping lines.
        for smr in 0 .. res - 1 do
            for smc in 0 .. res - 1 do
                if mask[smr, smc] = 0 then
                    tid <- tid + 1
                    let sx = x0 + (float smc + 0.5) / float res * (x1 - x0)
                    let sy = y0 + (float smr + 0.5) / float res * (y1 - y0)
                    let claimed = ResizeArray<int * int>()
                    mask[smr, smc] <- tid
                    claimed.Add(smr, smc)
                    let bwd = traceDir tid claimed -1.0 sx sy
                    let fwd = traceDir tid claimed 1.0 sx sy
                    let xs2 = ResizeArray<float>()
                    let ys2 = ResizeArray<float>()

                    for i in bwd.Count - 1 .. -1 .. 0 do
                        xs2.Add(fst bwd[i])
                        ys2.Add(snd bwd[i])

                    xs2.Add sx
                    ys2.Add sy

                    for i in 0 .. fwd.Count - 1 do
                        xs2.Add(fst fwd[i])
                        ys2.Add(snd fwd[i])

                    if xs2.Count > 10 then
                        let line = Line2D(xs2.ToArray(), ys2.ToArray())
                        line.Color <- col
                        lines.Add line
                    else
                        // too short to keep -> free its cells so the space can be re-used
                        for (r, c) in claimed do
                            mask[r, c] <- 0

        this.SetXLim(x0, x1)
        this.SetYLim(y0, y1)

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
                Family = rc.FontFamily
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
                Family = rc.FontFamily
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

    /// <summary>Hide all ticks, tick labels and spines (Matplotlib's <c>axis('off')</c>).</summary>
    member this.SetAxisOff() =
        this.XTicksVisible <- false
        this.YTicksVisible <- false
        this.SpineTop <- false
        this.SpineBottom <- false
        this.SpineLeft <- false
        this.SpineRight <- false

    /// <summary>Set the data aspect ratio (Matplotlib's <c>set_aspect</c>: <c>"equal"</c> or <c>"auto"</c>).</summary>
    member this.SetAspect(aspect: string) = this.Aspect <- aspect

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

    /// <summary>Fix the X tick positions and labels (Matplotlib's <c>set_xticks</c>+labels).</summary>
    member this.SetXTickLabels(positions: float[], labels: string[]) =
        this.XAxis.MajorLocator <- Some(FixedLocator positions :> ITickLocator)
        this.XAxis.MajorFormatter <- Some(LabeledTicksFormatter(positions, labels) :> ITickFormatter)

    /// <summary>Fix the X tick positions, keeping the default labels (Matplotlib's <c>set_xticks</c>).</summary>
    member this.SetXTicks(positions: float[]) = this.XAxis.MajorLocator <- Some(FixedLocator positions :> ITickLocator)

    /// <summary>Fix the Y tick positions, keeping the default labels (Matplotlib's <c>set_yticks</c>).</summary>
    member this.SetYTicks(positions: float[]) = this.YAxis.MajorLocator <- Some(FixedLocator positions :> ITickLocator)

    /// <summary>Fix the Y tick positions and labels (Matplotlib's <c>set_yticks</c>+labels).</summary>
    member this.SetYTickLabels(positions: float[], labels: string[]) =
        this.YAxis.MajorLocator <- Some(FixedLocator positions :> ITickLocator)
        this.YAxis.MajorFormatter <- Some(LabeledTicksFormatter(positions, labels) :> ITickFormatter)

    /// <summary>Label the X axis with categories at integer positions 0..n-1.</summary>
    /// <remarks>Ported from <c>matplotlib.category</c> (string-to-index mapping).</remarks>
    member this.SetXCategories(categories: string[]) =
        this.SetXTickLabels(Array.init categories.Length float, categories)
        this.SetXLim(-0.5, float categories.Length - 0.5)

    /// <summary>Plot y versus dates, formatting the x axis as dates.</summary>
    /// <remarks>Ported from <c>matplotlib.axes.Axes.plot_date</c> (OADate numbering).</remarks>
    member this.PlotDate(dates: DateTime[], ys: float[], ?format: string, ?color: Color, ?label: string) : Line2D =
        let xs = dates |> Array.map (fun d -> d.ToOADate())
        let line = this.Plot(xs, ys, ?color = color, ?label = label)
        this.XAxis.MajorFormatter <- Some(DateFormatter(defaultArg format "yyyy-MM-dd") :> ITickFormatter)
        line

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

        // reference lines/spans contribute only on their data axis (the other axis
        // is in axes-fraction coordinates and must not drive autoscale).
        let refXs =
            Seq.append
                (refVLines |> Seq.map (fun (x, _, _, _) -> x))
                (refVSpans |> Seq.collect (fun (a, b, _) -> [ a; b ]))
            |> Seq.toArray

        let refYs =
            Seq.append
                (refHLines |> Seq.map (fun (y, _, _, _) -> y))
                (refHSpans |> Seq.collect (fun (a, b, _) -> [ a; b ]))
            |> Seq.toArray

        let xs = Array.concat [ lineXs; boundsXs; refXs ] |> finite
        let ys = Array.concat [ lineYs; boundsYs; refYs ] |> finite

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

    /// <summary>The scales and clamped view intervals this Axes draws with.</summary>
    member private this.ScaleView() : IScale * IScale * Interval * Interval =
        // a twinx overlay borrows the source axes' x range and scale (shared x).
        let xSource = defaultArg this.SharedXFrom this
        let xScale = xSource.XAxis.Scale
        let yScale = this.YAxis.Scale
        xScale, yScale, xScale.ClampLimits xSource.XLim, yScale.ClampLimits this.YLim

    /// <summary>The pixel box this Axes occupies on a canvas of the given size.</summary>
    member private this.DrawBox(canvas: Size) : BBox =
        let pos = this.Position

        let fullBox =
            BBox.fromExtents
                (pos.X0 * canvas.Width)
                (pos.Y0 * canvas.Height)
                (pos.X1 * canvas.Width)
                (pos.Y1 * canvas.Height)

        let xScale, yScale, xView, yView = this.ScaleView()
        let xr = abs (xScale.TransformValue xView.Upper - xScale.TransformValue xView.Lower)
        let yr = abs (yScale.TransformValue yView.Upper - yScale.TransformValue yView.Lower)

        // aspect='equal': shrink the axes box so one (scaled) data unit spans the
        // same number of pixels on both axes (adjustable='box'), centred in place.
        if this.Aspect = "equal" && xr > 0.0 && yr > 0.0 then
            let s = min (abs fullBox.Width / xr) (abs fullBox.Height / yr)
            let w = s * xr
            let h = s * yr
            let cx = (fullBox.X0 + fullBox.X1) / 2.0
            let cy = (fullBox.Y0 + fullBox.Y1) / 2.0
            BBox.fromExtents (cx - w / 2.0) (cy - h / 2.0) (cx + w / 2.0) (cy + h / 2.0)
        else
            fullBox

    /// <summary>
    /// The major tick values and labels this Axes would draw on a canvas of the
    /// given size: the locator + formatter pass, without needing a renderer, so
    /// the figure layout can size its margins before anything is drawn.
    /// </summary>
    member internal this.MajorTicksFor(canvas: Size, dpi: float) : float[] * string[] * float[] * string[] =
        let box = this.DrawBox canvas
        let xScale, yScale, xView, yView = this.ScaleView()
        let pt2px = dpi / 72.0
        let nbinsX = AxesLayout.tickBins (abs box.Width) rc.TickLabelSize 3.0 pt2px
        let nbinsY = AxesLayout.tickBins (abs box.Height) rc.TickLabelSize 2.0 pt2px
        let xLocator = defaultArg this.XAxis.MajorLocator (xScale.CreateLocator nbinsX)
        let yLocator = defaultArg this.YAxis.MajorLocator (yScale.CreateLocator nbinsY)
        let xFormatter = defaultArg this.XAxis.MajorFormatter (xScale.CreateFormatter())
        let yFormatter = defaultArg this.YAxis.MajorFormatter (yScale.CreateFormatter())
        let xTicks = xLocator.TickValues xView |> Array.filter (AxesLayout.inView xView)
        let yTicks = yLocator.TickValues yView |> Array.filter (AxesLayout.inView yView)
        xTicks, xFormatter.FormatTicks xTicks, yTicks, yFormatter.FormatTicks yTicks

    /// <summary>
    /// The Y tick labels drawn to the *left* of this Axes — what the outer margin
    /// has to accommodate. Empty when the ticks are hidden or sit on the right
    /// (a colorbar or twinx overlay).
    /// </summary>
    member internal this.LeftTickLabels(canvas: Size, dpi: float) : string[] =
        if this.YTicksVisible && this.YTickSide <> "right" then
            let _, _, _, yLabels = this.MajorTicksFor(canvas, dpi)
            yLabels
        else
            [||]

    member private this.BuildContext(renderer: IRenderer) : AxesDrawContext =
        let canvas = renderer.CanvasSizePx
        let box = this.DrawBox canvas
        let xScale, yScale, xView, yView = this.ScaleView()
        let sxLo, sxHi = xScale.TransformValue xView.Lower, xScale.TransformValue xView.Upper
        let syLo, syHi = yScale.TransformValue yView.Lower, yScale.TransformValue yView.Upper

        let transAxes = BBoxTransform(BBox.unit, box) :> ITransform

        let transScale =
            FunctionalTransform(xScale.TransformValue, yScale.TransformValue, xScale.InverseValue, yScale.InverseValue)
            :> ITransform

        let scaledBox = BBox.fromExtents sxLo syLo sxHi syHi

        let transLimits = BBoxTransform(scaledBox, BBox.unit) :> ITransform
        let transData = Transforms.compose (Transforms.compose transScale transLimits) transAxes
        let pt2px = renderer.Dpi / 72.0
        let xTicks, xLabels, yTicks, yLabels = this.MajorTicksFor(canvas, renderer.Dpi)

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
            XLabels = xLabels
            YTicks = yTicks
            YLabels = yLabels
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

    member private this.DrawRefSpans(ctx: AxesDrawContext) =
        let b = ctx.Box

        let fillRect x0 y0 x1 y1 (c: Color) =
            let gc =
                { GraphicsContext.Default with
                    StrokeColor = c
                }

            ctx.Renderer.DrawPath(
                gc,
                Path.polygon
                    [
                        { X = x0; Y = y0 }
                        { X = x1; Y = y0 }
                        { X = x1; Y = y1 }
                        { X = x0; Y = y1 }
                    ],
                Some c
            )

        for (ymin, ymax, c) in refHSpans do
            let y0 = (ctx.TransData.Transform { X = 0.0; Y = ymin }).Y
            let y1 = (ctx.TransData.Transform { X = 0.0; Y = ymax }).Y
            fillRect b.X0 y0 b.X1 y1 c

        for (xmin, xmax, c) in refVSpans do
            let x0 = (ctx.TransData.Transform { X = xmin; Y = 0.0 }).X
            let x1 = (ctx.TransData.Transform { X = xmax; Y = 0.0 }).X
            fillRect x0 b.Y0 x1 b.Y1 c

    member private this.DrawRefLines(ctx: AxesDrawContext) =
        let lineGc (c: Color) =
            { GraphicsContext.Default with
                StrokeColor = c
                LineWidth = rc.LinesLineWidth * ctx.Pt2Px
            }

        for (y, xminF, xmaxF, c) in refHLines do
            let yp = (ctx.TransData.Transform { X = 0.0; Y = y }).Y
            let x0 = (ctx.TransAxes.Transform { X = xminF; Y = 0.0 }).X
            let x1 = (ctx.TransAxes.Transform { X = xmaxF; Y = 0.0 }).X
            ctx.Renderer.DrawPath(lineGc c, Path.polyline [ { X = x0; Y = yp }; { X = x1; Y = yp } ], None)

        for (x, yminF, ymaxF, c) in refVLines do
            let xp = (ctx.TransData.Transform { X = x; Y = 0.0 }).X
            let y0 = (ctx.TransAxes.Transform { X = 0.0; Y = yminF }).Y
            let y1 = (ctx.TransAxes.Transform { X = 0.0; Y = ymaxF }).Y
            ctx.Renderer.DrawPath(lineGc c, Path.polyline [ { X = xp; Y = y0 }; { X = xp; Y = y1 } ], None)

    member private this.DrawData(ctx: AxesDrawContext) =
        // reference spans (axhspan/axvspan) form a backdrop behind the data.
        this.DrawRefSpans ctx

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

        // reference lines (axhline/axvline) are drawn on top of the data.
        this.DrawRefLines ctx

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
                Family = rc.FontFamily
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

        if this.XTicksVisible then
            Array.iter2
                (fun tv lab ->
                    let x = (ctx.TransData.Transform { X = tv; Y = ctx.YView.Lower }).X
                    let y0, y1 = AxesLayout.tickEndpoints b.Y0 len dir
                    ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x; Y = y0 }; { X = x; Y = y1 } ], None)
                    (this.MakeTickLabel(x, b.Y0 - labelOff, lab, HCenter, VTop)).Draw ctx.Renderer)
                ctx.XTicks
                ctx.XLabels

        let onRight = this.YTickSide = "right"
        let yBase = if onRight then b.X1 else b.X0
        // mirror the tick direction for a right-side axis
        let yDir =
            if not onRight then
                dir
            else
                match dir with
                | "in" -> "out"
                | "out" -> "in"
                | other -> other

        if this.YTicksVisible then
            Array.iter2
                (fun tv lab ->
                    let y = (ctx.TransData.Transform { X = ctx.XView.Lower; Y = tv }).Y
                    let x0, x1 = AxesLayout.tickEndpoints yBase len yDir

                    ctx.Renderer.DrawPath(gc, Path.polyline [ { X = x0; Y = y }; { X = x1; Y = y } ], None)

                    let labelX = if onRight then b.X1 + labelOff else b.X0 - labelOff
                    let labelHa = if onRight then HLeft else HRight
                    (this.MakeTickLabel(labelX, y, lab, labelHa, VCenter)).Draw ctx.Renderer)
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
                Family = rc.FontFamily
                Size = rc.TickLabelSize
            }

        let labelFont =
            { FontProperties.Default with
                Family = rc.FontFamily
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
                    Family = rc.FontFamily
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
                    Family = rc.FontFamily
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
        // plotted data is clipped to the axes box (Matplotlib's default)
        renderer.PushClip ctx.Box
        this.DrawData ctx
        renderer.PopClip()
        this.DrawSpines ctx
        this.DrawMinorTicks ctx
        this.DrawTicks ctx
        this.DrawAxisLabelsAndTitle ctx
        this.DrawTexts ctx
        this.DrawLegend ctx
