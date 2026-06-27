namespace Matplotlib.Verso

open System
open System.Collections.Generic
open System.Threading.Tasks
open Verso.Abstractions
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Backends

/// <summary>
/// Verso notebook integration: a data formatter that renders a Matplotlib
/// <see cref="Figure"/> or <see cref="Plt"/> returned from a cell as inline SVG
/// (<c>image/svg+xml</c>).
/// </summary>
/// <remarks>
/// Ships inside the <c>DotnetMatplotlib.Interactive</c> package. Verso discovers this
/// formatter automatically via the <c>[VersoExtension]</c> marker once the package is
/// loaded — unlike the .NET Interactive integration (<see cref="T:Matplotlib.Interactive"/>),
/// no explicit registration call is required. Implements <see cref="IDataFormatter"/>
/// (which extends <see cref="IExtension"/>).
/// </remarks>
[<VersoExtension; Sealed>]
type MatplotlibFormatter() =

    // Cached once; the host uses this to pre-filter before calling CanFormat.
    static let supportedTypes = [| typeof<Figure>; typeof<Plt> |] :> IReadOnlyList<Type>

    interface IDataFormatter with
        member _.SupportedTypes = supportedTypes

        // Our own types have no built-in formatter; a high priority keeps the
        // generic fallback formatters from claiming them first.
        member _.Priority = 100

        member _.CanFormat(value: obj, _context: IFormatterContext) = (value :? Figure) || (value :? Plt)

        member _.FormatAsync(value: obj, _context: IFormatterContext) : Task<CellOutput> =
            let svg =
                match value with
                | :? Figure as fig -> FigureCanvas(fig).RenderToSvg()
                | :? Plt as plt -> plt.ToSvg()
                | _ -> "" // unreachable: the host only calls this after CanFormat

            Task.FromResult(CellOutput.Svg svg)

    interface IExtension with
        member _.ExtensionId = "com.dotnetmatplotlib.verso"
        member _.Name = "dotnet-matplotlib"
        member _.Version = "1.0.0"
        member _.Author = "Jun Tae Kim"
        member _.Description = "Renders Matplotlib figures (Figure and Plt) as inline SVG in Verso notebooks."
        member _.OnLoadedAsync(_context: IExtensionHostContext) : Task = Task.CompletedTask
        member _.OnUnloadedAsync() : Task = Task.CompletedTask
