namespace Matplotlib.Interactive

open System.IO
open Microsoft.DotNet.Interactive.Formatting
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Backends

/// <summary>
/// .NET Interactive / Polyglot / Jupyter notebook integration: registers HTML
/// formatters so a <see cref="Figure"/> or <see cref="Pyplot"/> returned from a
/// cell renders inline as SVG.
/// </summary>
/// <remarks>
/// Mirrors the formatter-registration pattern used by other .NET plotting
/// libraries in notebooks. Call <see cref="register"/> once per session.
/// </remarks>
[<RequireQualifiedAccess>]
module Notebook =

    let private htmlMime = "text/html"
    let mutable private registered = false

    /// <summary>
    /// Register inline SVG formatters for <see cref="Figure"/> and
    /// <see cref="Pyplot"/> with .NET Interactive. Idempotent.
    /// </summary>
    let register () : unit =
        if not registered then
            Formatter.Register(
                typeof<Figure>,
                (fun (value: obj) (writer: TextWriter) -> writer.Write(FigureCanvas(value :?> Figure).RenderToSvg())),
                htmlMime
            )

            Formatter.Register(
                typeof<Pyplot>,
                (fun (value: obj) (writer: TextWriter) -> writer.Write((value :?> Pyplot).ToSvg())),
                htmlMime
            )

            registered <- true
