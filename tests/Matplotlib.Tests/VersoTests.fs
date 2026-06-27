namespace Matplotlib.Tests

open System
open Xunit
open Verso.Abstractions
open Verso.Testing.Stubs
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Verso

module VersoTests =

    let private formatter = MatplotlibFormatter() :> IDataFormatter

    // The real context type the Verso host passes to formatters (from Verso.Testing).
    let private ctx = StubFormatterContext() :> IFormatterContext

    [<Fact>]
    let ``formatter advertises Figure and Plt as supported types`` () =
        Assert.Contains(typeof<Figure>, formatter.SupportedTypes)
        Assert.Contains(typeof<Plt>, formatter.SupportedTypes)

    [<Fact>]
    let ``FormatAsync renders a Figure as image/svg+xml`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |], color = "C0") |> ignore
        let fig = plt.CurrentFigure()
        Assert.True(formatter.CanFormat(fig, ctx))
        let out = formatter.FormatAsync(fig, ctx).Result
        Assert.Equal("image/svg+xml", out.MimeType)
        Assert.Contains("<svg", out.Content)
        Assert.False out.IsError

    [<Fact>]
    let ``FormatAsync renders a Plt facade as SVG`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        Assert.True(formatter.CanFormat(plt, ctx))
        let out = formatter.FormatAsync(plt, ctx).Result
        Assert.Equal("image/svg+xml", out.MimeType)
        Assert.Contains("<svg", out.Content)

    [<Fact>]
    let ``formatter does not claim unrelated values`` () =
        Assert.False(formatter.CanFormat("just a string", ctx))

    /// Replicates the Verso host's discovery pipeline against our shipped assembly:
    /// scan for `[VersoExtension]` types implementing `IDataFormatter`, construct via
    /// `Activator.CreateInstance` (as the host does), then format a real figure. This
    /// proves the integration path end-to-end short of launching the host process.
    [<Fact>]
    let ``host discovery path finds, constructs and runs the formatter`` () =
        let assembly = typeof<MatplotlibFormatter>.Assembly

        let discovered =
            assembly.GetTypes()
            |> Array.filter (fun t ->
                t.GetCustomAttributes(typeof<VersoExtensionAttribute>, false).Length > 0
                && typeof<IDataFormatter>.IsAssignableFrom t
                && not t.IsAbstract)

        Assert.Contains(typeof<MatplotlibFormatter>, discovered)

        // The host instantiates discovered extensions with their parameterless ctor.
        let instance = Activator.CreateInstance(typeof<MatplotlibFormatter>) :?> IDataFormatter

        let plt = Plt()
        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |], color = "C0") |> ignore

        Assert.True(instance.CanFormat(plt, ctx))
        let out = instance.FormatAsync(plt, ctx).Result
        Assert.Equal("image/svg+xml", out.MimeType)
        Assert.Contains("<svg", out.Content)

        // Identity the host surfaces in its extension list must be non-empty.
        let ext = instance :> IExtension
        Assert.False(String.IsNullOrWhiteSpace ext.ExtensionId)
