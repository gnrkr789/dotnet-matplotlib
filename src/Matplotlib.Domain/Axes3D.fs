namespace Matplotlib.Domain

open System
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style
open Matplotlib.Domain.Rendering

/// <summary>
/// A minimal 3D axes (Matplotlib's <c>mpl_toolkits.mplot3d.Axes3D</c>): holds 3D
/// line/scatter series and wireframe surfaces, and renders them with an
/// orthographic projection controlled by elevation and azimuth angles.
/// </summary>
/// <remarks>
/// The projection follows mplot3d's view rotation (azimuth about the vertical,
/// then elevation), without perspective. Data is normalized to a unit cube; the
/// projected scene is uniformly scaled to fit the axes box. A reference cube
/// frame is drawn for depth context.
/// </remarks>
type Axes3D(rc: RcParams) =

    // xs, ys, zs, color, isScatter
    let series = ResizeArray<float[] * float[] * float[] * Color * bool>()
    // x (cols), y (rows), z[rows,cols], color
    let surfaces = ResizeArray<float[] * float[] * float[,] * Color>()
    let cycler = PropertyCycler.CreateDefault()

    new() = Axes3D(RcParams.Default)

    /// <summary>Axes position within the figure, in figure fractions.</summary>
    member val Position = BBox.fromExtents 0.1 0.1 0.9 0.9 with get, set

    /// <summary>Elevation viewing angle in degrees.</summary>
    member val Elev = 30.0 with get, set

    /// <summary>Azimuth viewing angle in degrees.</summary>
    member val Azim = -60.0 with get, set

    member val Title = "" with get, set
    member val XLabel = "" with get, set
    member val YLabel = "" with get, set
    member val ZLabel = "" with get, set

    /// <summary>Plot a 3D line through the given points (Matplotlib's <c>plot3D</c>).</summary>
    member _.Plot3D(xs: float[], ys: float[], zs: float[], ?color: Color) =
        series.Add(xs, ys, zs, defaultArg color (cycler.Next()), false)

    /// <summary>Scatter 3D points (Matplotlib's <c>scatter3D</c>).</summary>
    member _.Scatter3D(xs: float[], ys: float[], zs: float[], ?color: Color) =
        series.Add(xs, ys, zs, defaultArg color (cycler.Next()), true)

    /// <summary>Draw a wireframe surface over a grid (Matplotlib's <c>plot_wireframe</c>).</summary>
    member _.PlotWireframe(x: float[], y: float[], z: float[,], ?color: Color) =
        surfaces.Add(x, y, z, defaultArg color (cycler.Next()))

    member private _.AllPoints() : (float * float * float) seq =
        seq {
            for (xs, ys, zs, _, _) in series do
                for i in 0 .. (min xs.Length (min ys.Length zs.Length)) - 1 -> (xs[i], ys[i], zs[i])

            for (x, y, z, _) in surfaces do
                for r in 0 .. y.Length - 1 do
                    for c in 0 .. x.Length - 1 -> (x[c], y[r], z[r, c])
        }

    member this.Draw(renderer: IRenderer) =
        let pts = this.AllPoints() |> Seq.toArray
        let canvas = renderer.CanvasSizePx
        let pos = this.Position

        let box =
            BBox.fromExtents
                (pos.X0 * canvas.Width)
                (pos.Y0 * canvas.Height)
                (pos.X1 * canvas.Width)
                (pos.Y1 * canvas.Height)

        let range sel =
            if pts.Length = 0 then
                0.0, 1.0
            else
                let vs = pts |> Array.map sel
                Array.min vs, Array.max vs

        let xmin, xmax = range (fun (x, _, _) -> x)
        let ymin, ymax = range (fun (_, y, _) -> y)
        let zmin, zmax = range (fun (_, _, z) -> z)

        let norm v lo hi = if hi <= lo then 0.0 else (v - lo) / (hi - lo) - 0.5

        let a = this.Azim * Math.PI / 180.0
        let e = this.Elev * Math.PI / 180.0
        let cosA, sinA = cos a, sin a
        let cosE, sinE = cos e, sin e

        // (x,y,z) data -> projected screen (sx, sy) before fitting
        let project (x: float) (y: float) (z: float) =
            let nx = norm x xmin xmax
            let ny = norm y ymin ymax
            let nz = norm z zmin zmax
            let x1 = nx * cosA + ny * sinA
            let y1 = -nx * sinA + ny * cosA
            let z2 = -y1 * sinE + nz * cosE
            (x1, z2)

        // reference cube corners
        let corners =
            [|
                for xi in [ xmin; xmax ] do
                    for yi in [ ymin; ymax ] do
                        for zi in [ zmin; zmax ] -> project xi yi zi
            |]

        let projectedData = pts |> Array.map (fun (x, y, z) -> project x y z)
        let allScreen = Array.append corners projectedData

        let fit =
            if allScreen.Length = 0 then
                fun _ -> { X = box.CenterX; Y = box.CenterY }
            else
                let sxs = allScreen |> Array.map fst
                let sys = allScreen |> Array.map snd
                let sxMin, sxMax = Array.min sxs, Array.max sxs
                let syMin, syMax = Array.min sys, Array.max sys
                let pad = 0.14
                let availW = box.Width * (1.0 - 2.0 * pad)
                let availH = box.Height * (1.0 - 2.0 * pad)
                let sw = max 1e-9 (sxMax - sxMin)
                let sh = max 1e-9 (syMax - syMin)
                let scale = min (availW / sw) (availH / sh)
                let cx = (sxMin + sxMax) / 2.0
                let cy = (syMin + syMax) / 2.0

                fun (sx, sy) ->
                    {
                        X = box.CenterX + (sx - cx) * scale
                        Y = box.CenterY + (sy - cy) * scale
                    }

        let toDisplay x y z = fit (project x y z)

        // --- reference cube frame ---
        let cubeGc =
            { GraphicsContext.Default with
                StrokeColor = Color.fromHex "#cccccc"
                LineWidth = 0.8 * renderer.Dpi / 72.0
            }

        let cornerPt xi yi zi = toDisplay xi yi zi
        let xs2 = [| xmin; xmax |]
        let ys2 = [| ymin; ymax |]
        let zs2 = [| zmin; zmax |]

        let edge p0 p1 = renderer.DrawPath(cubeGc, Path.polyline [ p0; p1 ], None)

        for yi in ys2 do
            for zi in zs2 do
                edge (cornerPt xmin yi zi) (cornerPt xmax yi zi)

        for xi in xs2 do
            for zi in zs2 do
                edge (cornerPt xi ymin zi) (cornerPt xi ymax zi)

        for xi in xs2 do
            for yi in ys2 do
                edge (cornerPt xi yi zmin) (cornerPt xi yi zmax)

        // --- wireframe surfaces ---
        for (x, y, z, color) in surfaces do
            let gc =
                { GraphicsContext.Default with
                    StrokeColor = color
                    LineWidth = rc.LinesLineWidth * 0.6 * renderer.Dpi / 72.0
                }

            for r in 0 .. y.Length - 1 do
                let row = [ for c in 0 .. x.Length - 1 -> toDisplay x[c] y[r] z[r, c] ]
                renderer.DrawPath(gc, Path.polyline row, None)

            for c in 0 .. x.Length - 1 do
                let colLine = [ for r in 0 .. y.Length - 1 -> toDisplay x[c] y[r] z[r, c] ]
                renderer.DrawPath(gc, Path.polyline colLine, None)

        // --- line / scatter series ---
        for (xs, ys, zs, color, isScatter) in series do
            let n = min xs.Length (min ys.Length zs.Length)
            let projected = [ for i in 0 .. n - 1 -> toDisplay xs[i] ys[i] zs[i] ]

            if isScatter then
                let gc =
                    { GraphicsContext.Default with
                        StrokeColor = Color.none
                        LineWidth = 0.0
                    }

                let radius = 3.0 * renderer.Dpi / 72.0

                for p in projected do
                    let disk =
                        [
                            for k in 0..11 ->
                                {
                                    X = p.X + radius * cos (float k / 12.0 * 2.0 * Math.PI)
                                    Y = p.Y + radius * sin (float k / 12.0 * 2.0 * Math.PI)
                                }
                        ]

                    renderer.DrawPath(gc, Path.polygon disk, Some color)
            else
                let gc =
                    { GraphicsContext.Default with
                        StrokeColor = color
                        LineWidth = rc.LinesLineWidth * renderer.Dpi / 72.0
                    }

                renderer.DrawPath(gc, Path.polyline projected, None)

        // --- title and axis labels ---
        let textGc =
            { GraphicsContext.Default with
                StrokeColor = rc.TextColor
            }

        let font =
            { FontProperties.Default with
                Family = rc.FontFamily
                Size = rc.FontSize
            }

        if this.Title <> "" then
            let titleFont = { font with Size = rc.AxesTitleSize }
            renderer.DrawText(textGc, box.CenterX, box.Y1, this.Title, titleFont, 0.0, HCenter, VBottom)

        let labelAt (mid: Point2D) (text: string) =
            if text <> "" then
                renderer.DrawText(textGc, mid.X, mid.Y, text, font, 0.0, HCenter, VCenter)

        let midpoint (p0: Point2D) (p1: Point2D) : Point2D =
            {
                X = (p0.X + p1.X) / 2.0
                Y = (p0.Y + p1.Y) / 2.0
            }

        labelAt (midpoint (cornerPt xmin ymin zmin) (cornerPt xmax ymin zmin)) this.XLabel
        labelAt (midpoint (cornerPt xmax ymin zmin) (cornerPt xmax ymax zmin)) this.YLabel
        labelAt (midpoint (cornerPt xmin ymin zmin) (cornerPt xmin ymin zmax)) this.ZLabel
