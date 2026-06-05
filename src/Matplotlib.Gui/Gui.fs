namespace Matplotlib.Gui

open System.Windows.Forms
open Matplotlib
open Matplotlib.Domain

/// <summary>
/// Interactive on-screen display of figures, mirroring <c>matplotlib.pyplot.show</c>.
/// </summary>
/// <remarks>
/// Opt-in, Windows-only (WinForms + GDI+). The default SVG backend stays free of
/// any native/UI dependency; this module is only referenced by code that wants a
/// live window.
/// </remarks>
[<RequireQualifiedAccess>]
module Gui =

    let mutable private appInitialized = false

    let private ensureInit () =
        if not appInitialized then
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault false
            appInitialized <- true

    /// <summary>
    /// Open a window displaying the figure and block until it is closed
    /// (Matplotlib's blocking <c>plt.show()</c>).
    /// </summary>
    let show (figure: Figure) : unit =
        ensureInit ()
        use form = new PlotWindow(figure)
        Application.Run form

/// <summary>Adds an interactive <c>Show</c> to the <see cref="Pyplot"/> facade.</summary>
[<AutoOpen>]
module PyplotGuiExtensions =

    type Pyplot with

        /// <summary>
        /// Display the current figure in an interactive window and block until it
        /// is closed (Matplotlib's <c>plt.show()</c>).
        /// </summary>
        member this.Show() : unit = Gui.show (this.CurrentFigure())
