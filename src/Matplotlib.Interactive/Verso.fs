namespace Matplotlib.Verso

open System
open System.Collections.Generic
open System.Reflection
open System.Runtime.Loader
open System.Threading.Tasks
open Verso.Abstractions

/// <summary>
/// Verso notebook integration: renders a Matplotlib <c>Plt</c> or <c>Figure</c>
/// returned from a cell as inline SVG (<c>image/svg+xml</c>).
/// </summary>
/// <remarks>
/// Ships in the <c>DotnetMatplotlib.Interactive</c> package and is discovered
/// automatically via <c>[VersoExtension]</c>. Verso loads each extension into its own
/// collectible <c>AssemblyLoadContext</c> and shares only <c>Verso.Abstractions</c> with
/// the host, so the <c>Plt</c>/<c>Figure</c> a notebook kernel produces are *different
/// CLR types* from the ones this assembly references. The formatter therefore matches
/// values by type name and renders through reflection rather than a direct cast, so it
/// works across the load-context boundary.
/// </remarks>
[<VersoExtension; Sealed>]
type MatplotlibFormatter() =

    // The host pre-filters with `SupportedTypes.Any(t -> t.IsInstanceOfType(value))`.
    // Across the load-context boundary only the shared `System.Object` matches every
    // value, so we advertise that and let CanFormat do the real, name-based gating.
    static let supportedTypes = [| typeof<obj> |] :> IReadOnlyList<Type>

    static let isRenderable (value: obj) : bool =
        not (isNull value)
        && (match value.GetType().FullName with
            | "Matplotlib.Plt"
            | "Matplotlib.Domain.Figure" -> true
            | _ -> false)

    /// Render a value to SVG via reflection, so its type may come from any load context.
    static let tryRenderSvg (value: obj) : string option =
        try
            let vt = value.GetType()
            let toSvg = vt.GetMethod("ToSvg", Type.EmptyTypes)

            if not (isNull toSvg) && toSvg.ReturnType = typeof<string> then
                // A `Plt` (and anything exposing `string ToSvg()`) renders itself.
                Some(toSvg.Invoke(value, null) :?> string)
            elif vt.FullName = "Matplotlib.Domain.Figure" then
                // A bare `Figure` renders through `FigureCanvas`, loaded from the value's
                // own context so the constructed canvas accepts it.
                let alc =
                    match AssemblyLoadContext.GetLoadContext vt.Assembly with
                    | null -> AssemblyLoadContext.Default
                    | ctx -> ctx

                let backends = alc.LoadFromAssemblyName(AssemblyName "Matplotlib.Backends")
                let canvasType = backends.GetType "Matplotlib.Backends.FigureCanvas"
                let canvas = Activator.CreateInstance(canvasType, [| value |])
                Some(canvasType.GetMethod("RenderToSvg", Type.EmptyTypes).Invoke(canvas, null) :?> string)
            else
                None
        with _ ->
            None

    interface IDataFormatter with
        member _.SupportedTypes = supportedTypes

        // High enough to win over the generic object-tree fallback for our types.
        member _.Priority = 100

        member _.CanFormat(value: obj, _context: IFormatterContext) = isRenderable value

        member _.FormatAsync(value: obj, _context: IFormatterContext) : Task<CellOutput> =
            match tryRenderSvg value with
            | Some svg -> Task.FromResult(CellOutput.Svg svg)
            | None -> Task.FromResult(CellOutput.Error "dotnet-matplotlib: could not render the value to SVG.")

    interface IExtension with
        member _.ExtensionId = "com.dotnetmatplotlib.verso"
        member _.Name = "dotnet-matplotlib"
        member _.Version = "1.0.0"
        member _.Author = "Jun Tae Kim"
        member _.Description = "Renders Matplotlib figures (Plt and Figure) as inline SVG in Verso notebooks."
        member _.OnLoadedAsync(_context: IExtensionHostContext) : Task = Task.CompletedTask
        member _.OnUnloadedAsync() : Task = Task.CompletedTask
