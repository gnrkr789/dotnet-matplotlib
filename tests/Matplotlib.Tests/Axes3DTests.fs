namespace Matplotlib.Tests

open System
open Xunit
open Matplotlib

module Axes3DTests =

    [<Fact>]
    let ``plot3D renders a projected line into the SVG`` () =
        let plt = Plt()
        let t = [| for i in 0..100 -> float i / 100.0 * 4.0 * Math.PI |]
        let xs = t |> Array.map cos
        let ys = t |> Array.map sin
        let zs = t |> Array.map (fun v -> v / (4.0 * Math.PI))
        let ax = plt.Plot3D(xs, ys, zs, color = "C0")
        ax.Title <- "helix"
        ax.XLabel <- "x"
        let svg = plt.ToSvg()
        Assert.Contains("<svg", svg)
        Assert.Contains("<path", svg)
        // title text present
        Assert.Contains("helix", svg)

    [<Fact>]
    let ``scatter3D and wireframe render`` () =
        let plt = Plt()

        plt.Scatter3D([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 0.0 |], [| 0.0; 1.0; 2.0 |], color = "C1")
        |> ignore

        let svg = plt.ToSvg()
        Assert.Contains("<path", svg)

        let plt2 = Plt()
        let x = [| 0.0; 1.0; 2.0 |]
        let y = [| 0.0; 1.0; 2.0 |]
        let z = Array2D.init 3 3 (fun r c -> float (r + c))
        plt2.PlotWireframe(x, y, z) |> ignore
        Assert.Contains("<path", plt2.ToSvg())

    [<Fact>]
    let ``a fresh figure resets the 3D axes`` () =
        let plt = Plt()
        plt.Plot3D([| 0.0; 1.0 |], [| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        let a1 = plt.Axes3D()
        plt.Figure() |> ignore
        let a2 = plt.Axes3D()
        Assert.False(Object.ReferenceEquals(a1, a2))
