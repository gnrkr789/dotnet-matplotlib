module Matplotlib.Samples.GuiDemo.Program

open System
open Matplotlib
open Matplotlib.Gui

/// <summary>
/// A tiny app that builds a figure and opens it in an interactive window,
/// exactly as one would with Matplotlib's <c>plt.show()</c>.
/// </summary>
[<EntryPoint; STAThread>]
let main argv =
    let plt = Plt()

    // Set the default font to Malgun Gothic so Korean labels render correctly.
    // (Must be set before the figure/axes are created, like matplotlib's rcParams.)
    plt.FontFamily <- "맑은 고딕"

    let xs = [| for i in 0..400 -> float i / 400.0 * 4.0 * Math.PI |]
    let sine = xs |> Array.map sin
    let cosine = xs |> Array.map cos
    let damped = Array.map2 (fun x s -> s * exp (-x / 10.0)) xs sine

    plt.Plot(xs, sine, color = "C0", label = "사인 sin x") |> ignore

    plt.Plot(xs, cosine, color = "C1", lineStyle = "--", label = "코사인 cos x")
    |> ignore

    plt.Plot(xs, damped, color = "C2", label = "감쇠 sin x · e^(-x/10)") |> ignore

    plt.FillBetween(xs, damped, color = "C2", alpha = 0.15) |> ignore

    plt.Title "dotnet-matplotlib — 인터랙티브 창 (plt.Show())"
    plt.XLabel "x 축"
    plt.YLabel "y 축"
    plt.Grid true
    plt.Legend(loc = "upper right")

    match argv with
    | [| "png"; path |] ->
        // Headless raster export, like plt.savefig("out.png").
        plt.SavePng path
    | _ ->
        // Blocks until the user closes the window, like Matplotlib's plt.show().
        plt.Show()

    0
