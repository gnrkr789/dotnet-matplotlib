namespace Matplotlib.Tests

open Xunit
open Matplotlib

module Phase5PlotsTests =

    [<Fact>]
    let ``quiver renders arrows`` () =
        let plt = Plt()
        plt.Quiver([| 0.0; 1.0 |], [| 0.0; 0.0 |], [| 1.0; 0.0 |], [| 0.0; 1.0 |])
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``hist2d produces an image covering the data range`` () =
        let plt = Plt()
        let xs = [| 0.0; 1.0; 2.0; 3.0; 1.0; 2.0 |]
        let ys = [| 0.0; 1.0; 2.0; 3.0; 2.0; 1.0 |]
        plt.Hist2d(xs, ys, bins = 4) |> ignore
        let ax = plt.CurrentAxes()
        assertClose 0.0 ax.XLim.Lower
        assertClose 3.0 ax.XLim.Upper
        Assert.Contains("<", plt.ToSvg())

    [<Fact>]
    let ``boxplot autoscale includes an outlier`` () =
        let plt = Plt()
        plt.Boxplot([| [| 1.0; 2.0; 3.0; 4.0; 5.0; 100.0 |]; [| 2.0; 3.0; 4.0 |] |])
        Assert.Contains("<path", plt.ToSvg())
        Assert.True(plt.CurrentAxes().YLim.Upper >= 100.0)

    [<Fact>]
    let ``violinplot renders a density polygon`` () =
        let plt = Plt()
        plt.Violinplot([| [| 1.0; 2.0; 2.0; 3.0; 3.0; 3.0; 4.0; 5.0 |] |])
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``streamplot traces streamlines through a uniform field`` () =
        let plt = Plt()
        let x = [| 0.0; 1.0; 2.0; 3.0 |]
        let y = [| 0.0; 1.0; 2.0; 3.0 |]
        let u = Array2D.init 4 4 (fun _ _ -> 1.0)
        let v = Array2D.init 4 4 (fun _ _ -> 0.0)
        plt.Streamplot(x, y, u, v, density = 1)
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``hist bins values into counts with edge-aligned bars`` () =
        let plt = Plt()
        let heights, edges = plt.Hist([| 0.0; 0.0; 1.0; 2.0; 2.0; 2.0 |], bins = 3, range = (0.0, 3.0))
        Assert.Equal<float[]>([| 2.0; 1.0; 3.0 |], heights)
        Assert.Equal(4, edges.Length)
        assertClose 0.0 edges[0]
        assertClose 3.0 edges[3]
        Assert.Equal(3, plt.CurrentAxes().Patches.Count)

    [<Fact>]
    let ``hist density normalizes to unit area`` () =
        let plt = Plt()

        let heights, edges = plt.Hist([| 0.0; 1.0; 2.0; 3.0 |], bins = 2, range = (0.0, 4.0), density = true)

        let binWidth = edges[1] - edges[0]
        assertClose 1.0 (heights |> Array.sumBy (fun h -> h * binWidth))

    [<Fact>]
    let ``stackplot stacks areas cumulatively`` () =
        let plt = Plt()
        let x = [| 0.0; 1.0; 2.0 |]
        let ys = [| [| 1.0; 1.0; 1.0 |]; [| 2.0; 2.0; 2.0 |] |]
        let polys = plt.Stackplot(x, ys)
        Assert.Equal(2, polys.Length)
        // total stacked height is 1 + 2 = 3, so the y range reaches it
        Assert.True(plt.CurrentAxes().YLim.Upper >= 3.0)

    [<Fact>]
    let ``vlines and hlines add one segment each`` () =
        let plt = Plt()
        plt.Vlines([| 0.0; 1.0 |], [| 0.0; 0.0 |], [| 1.0; 2.0 |], color = "C0")
        plt.Hlines([| 0.5 |], [| 0.0 |], [| 2.0 |], color = "C1")
        Assert.Equal(3, plt.CurrentAxes().Lines.Count)

    [<Fact>]
    let ``pie creates one wedge per value and hides the axis frame`` () =
        let plt = Plt()
        let wedges = plt.Pie([| 1.0; 2.0; 3.0 |], labels = [| "a"; "b"; "c" |])
        Assert.Equal(3, wedges.Length)
        let ax = plt.CurrentAxes()
        Assert.False ax.XTicksVisible
        Assert.False ax.YTicksVisible
        Assert.False ax.SpineTop

    [<Fact>]
    let ``Equal aspect is honored and still renders`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 0.0 |]) |> ignore
        plt.CurrentAxes().SetAspect "equal"
        Assert.Equal("equal", plt.CurrentAxes().Aspect)
        Assert.Contains("<path", plt.ToSvg())

    [<Fact>]
    let ``streamplot RK4 conserves radius on a circular field`` () =
        let plt = Plt()
        let n = 21
        let xs = Array.init n (fun i -> -2.0 + 4.0 * float i / float (n - 1))
        let ys = xs
        // solid-body rotation u = -y, v = x -> exact circular trajectories
        let u = Array2D.init n n (fun r _ -> -ys[r])
        let v = Array2D.init n n (fun _ c -> xs[c])
        plt.Streamplot(xs, ys, u, v, density = 1)
        let ax = plt.CurrentAxes()
        Assert.True(ax.Lines.Count > 0)
        // every streamline point stays on its circle; forward Euler would drift outward
        for line in ax.Lines do
            let r0 = sqrt (line.XData[0] ** 2.0 + line.YData[0] ** 2.0)

            if r0 > 0.3 && r0 < 1.6 then
                for i in 0 .. line.XData.Length - 1 do
                    let r = sqrt (line.XData[i] ** 2.0 + line.YData[i] ** 2.0)
                    Assert.True(abs (r - r0) < 0.1 * r0 + 0.08, $"radius drifted {r0} -> {r}")

    [<Fact>]
    let ``streamplot density mask spaces non-overlapping streamlines`` () =
        let plt = Plt()
        let n = 20
        let xs = Array.init n (fun i -> float i)
        let ys = xs
        let u = Array2D.init n n (fun _ _ -> 1.0) // uniform rightward field
        let v = Array2D.init n n (fun _ _ -> 0.0)
        plt.Streamplot(xs, ys, u, v, density = 1)
        let ax = plt.CurrentAxes()
        Assert.True(ax.Lines.Count >= 3)
        let startYs = ax.Lines |> Seq.map (fun l -> l.YData[0]) |> Seq.toArray |> Array.sort
        // lines are spread across the domain and separated (no two pile into one row band)
        Assert.True(Array.max startYs - Array.min startYs > float n * 0.4)
        let minGap = Array.pairwise startYs |> Array.map (fun (a, b) -> b - a) |> Array.min
        Assert.True(minGap > 0.1)
