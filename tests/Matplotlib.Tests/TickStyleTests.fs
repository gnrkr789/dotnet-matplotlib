namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style

module TickStyleTests =

    let private countPaths (svg: string) = (svg.Split("<path").Length) - 1

    [<Fact>]
    let ``Auto minor locator splits a 0.2 major step into four`` () =
        let majors = [| 0.0; 0.2; 0.4; 0.6; 0.8; 1.0 |]
        let minors = AxesLayout.minorTicks majors { Lower = 0.0; Upper = 1.0 }
        // step 0.05 -> 21 positions in [0,1], minus the 6 majors = 15 minors
        Assert.Equal(15, minors.Length)
        Assert.Contains(minors, fun v -> abs (v - 0.1) < 1e-9)
        Assert.DoesNotContain(minors, fun v -> abs (v - 0.2) < 1e-9)

    [<Fact>]
    let ``Auto minor locator splits a unit major step into five`` () =
        let majors = [| 0.0; 1.0; 2.0; 3.0 |]
        let minors = AxesLayout.minorTicks majors { Lower = 0.0; Upper = 3.0 }
        // step 0.2 -> minors at 0.2,0.4,... excluding integers
        Assert.Contains(minors, fun v -> abs (v - 0.2) < 1e-9)
        Assert.Contains(minors, fun v -> abs (v - 0.8) < 1e-9)
        Assert.DoesNotContain(minors, fun v -> abs (v - 1.0) < 1e-9)

    [<Fact>]
    let ``set_xticks and set_yticklabels override the locator and formatter`` () =
        let ax = Axes()
        ax.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 2.0 |]) |> ignore
        ax.SetXTicks [| 0.0; 1.0; 2.0 |]
        ax.SetYTickLabels([| 0.0; 1.0 |], [| "lo"; "hi" |])
        Assert.True ax.XAxis.MajorLocator.IsSome
        Assert.True ax.YAxis.MajorLocator.IsSome
        Assert.True ax.YAxis.MajorFormatter.IsSome

    [<Fact>]
    let ``Stem builds stems, a baseline and a marker line`` () =
        let ax = Axes()
        let markerLine = ax.Stem([| 0.0; 1.0; 2.0 |], [| 1.0; 2.0; 3.0 |])
        // 3 stems + 1 baseline + 1 marker line
        Assert.Equal(5, ax.Lines.Count)
        Assert.Equal(LineStyle.NoLine, markerLine.LineStyle)
        Assert.Equal(MarkerStyle.Circle, markerLine.Marker)

    [<Fact>]
    let ``Tick params sets the direction`` () =
        let ax = Axes()
        ax.TickParams(direction = "inout")
        Assert.Equal("inout", ax.TickDirection)

    [<Fact>]
    let ``Spine visibility can be toggled`` () =
        let ax = Axes()
        ax.SetSpineVisible("top", false)
        ax.SetSpineVisible("right", false)
        Assert.False(ax.SpineTop)
        Assert.False(ax.SpineRight)
        Assert.True(ax.SpineBottom)

    [<Fact>]
    let ``Enabling minor ticks adds tick marks to the SVG`` () =
        let plt = Pyplot()
        plt.Plot([| 0.0; 1.0; 2.0; 3.0 |], [| 0.0; 1.0; 4.0; 9.0 |]) |> ignore
        let before = countPaths (plt.ToSvg())
        plt.MinorTicks()
        let after = countPaths (plt.ToSvg())
        Assert.True(after > before, $"expected more paths after enabling minor ticks ({before} -> {after})")

    [<Fact>]
    let ``Pyplot stem renders markers to SVG`` () =
        let plt = Pyplot()

        plt.Stem([| 0.0; 1.0; 2.0; 3.0 |], [| 1.0; 3.0; 2.0; 4.0 |], color = "C0", label = "stem")
        |> ignore

        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)
        Assert.Contains("#1f77b4", svg)
