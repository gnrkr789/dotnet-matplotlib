namespace Matplotlib.Tests

open Xunit
open Matplotlib.Domain

/// <summary>
/// Regression tests for outer-margin sizing in <c>TightLayout</c> and
/// <c>ConstrainedLayout</c>. Both used to reserve room for a fixed four-character
/// tick label, so a wider one (e.g. <c>1000000000</c>) was pushed off the canvas —
/// and calling either method left *less* room than the untouched default margins.
/// </summary>
module LayoutMarginTests =

    /// <summary>
    /// Distance in pixels from the canvas edge to the left edge of the widest Y
    /// tick label, positioned exactly as <c>DrawTicks</c> positions it:
    /// right-anchored one tick length + pad outside the axes box. Negative means
    /// the label is drawn off-canvas.
    /// </summary>
    let private widestLabelLeftEdge (fig: Figure) (ax: Axes) : float =
        let figPx = fig.PixelSize
        let pt2px = fig.Dpi / 72.0
        let labelOff = (fig.Rc.TickMajorSize + fig.Rc.TickPad) * pt2px

        let width =
            ax.LeftTickLabels(figPx, fig.Dpi)
            |> AxesLayout.tickLabelWidth fig.Rc.TickLabelSize pt2px

        ax.Position.X0 * figPx.Width - labelOff - width

    /// <summary>A figure whose Y tick labels scale with <paramref name="magnitude"/>.</summary>
    let private figureAt (magnitude: float) : Figure * Axes =
        let fig = Figure()
        let ax = fig.AddSubplot()

        ax.Plot(
            [| 0.0; 1.0; 2.0; 3.0 |],
            [| magnitude; 2.0 * magnitude; 1.5 * magnitude; 3.0 * magnitude |]
        )
        |> ignore

        fig, ax

    /// <summary>Magnitudes producing 3- to 12-character tick labels.</summary>
    let private magnitudes = [ 1.0e2; 1.0e4; 1.0e6; 1.0e8; 1.0e9; 1.0e11 ]

    [<Fact>]
    let ``TightLayout keeps a ten-character tick label on the canvas`` () =
        // 1e9 ticks label as "1000000000"; this used to land 36px off-canvas.
        let fig, ax = figureAt 1.0e9
        fig.TightLayout()
        let edge = widestLabelLeftEdge fig ax
        Assert.True(edge >= 0.0, $"widest Y tick label starts {edge}px from the canvas edge")

    [<Fact>]
    let ``TightLayout keeps tick labels on the canvas at every magnitude`` () =
        for magnitude in magnitudes do
            let fig, ax = figureAt magnitude
            fig.TightLayout()
            let edge = widestLabelLeftEdge fig ax
            let widest = ax.LeftTickLabels(fig.PixelSize, fig.Dpi) |> Array.maxBy String.length

            Assert.True(edge >= 0.0, $"label \"{widest}\" starts {edge}px from the canvas edge")

    [<Fact>]
    let ``TightLayout reserves more room for wider tick labels`` () =
        // The defect the fixed 4-character constant caused: the reserved margin
        // did not respond to label width at all.
        let marginFor (magnitude: float) =
            let fig, ax = figureAt magnitude
            fig.TightLayout()
            ax.Position.X0

        let narrow = marginFor 1.0e2 // "100"
        let wide = marginFor 1.0e9 // "1000000000"
        Assert.True(wide > narrow, $"margin did not grow with label width: {narrow} -> {wide}")

    [<Fact>]
    let ``ConstrainedLayout keeps a ten-character tick label on the canvas`` () =
        let fig, ax = figureAt 1.0e9
        fig.ConstrainedLayout()
        let edge = widestLabelLeftEdge fig ax
        Assert.True(edge >= 0.0, $"widest Y tick label starts {edge}px from the canvas edge")

    [<Fact>]
    let ``Right-side tick labels do not widen the left margin`` () =
        // A twinx overlay (like a colorbar) labels its Y axis on the right, so its
        // labels must not be charged to the left margin.
        let fig, ax = figureAt 1.0e2
        fig.TightLayout()
        let withoutTwin = ax.Position.X0

        let fig2, ax2 = figureAt 1.0e2
        let twin = fig2.AddTwinX ax2
        twin.Plot([| 0.0; 3.0 |], [| 1.0e9; 3.0e9 |]) |> ignore
        fig2.TightLayout()

        Assert.Empty(twin.LeftTickLabels(fig2.PixelSize, fig2.Dpi))
        assertCloseTol 1e-9 withoutTwin ax2.Position.X0

    [<Fact>]
    let ``Hidden ticks reserve no label room`` () =
        let fig, ax = figureAt 1.0e9
        ax.SetAxisOff()
        Assert.Empty(ax.LeftTickLabels(fig.PixelSize, fig.Dpi))
