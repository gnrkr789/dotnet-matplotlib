namespace Matplotlib.Tests

open Xunit
open Verso.Abstractions
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Verso

module VersoTests =

    let private formatter = MatplotlibFormatter() :> IDataFormatter

    // CanFormat / FormatAsync ignore the context for SVG output, so a null is fine here.
    let private noCtx = Unchecked.defaultof<IFormatterContext>

    [<Fact>]
    let ``formatter advertises Figure and Plt as supported types`` () =
        Assert.Contains(typeof<Figure>, formatter.SupportedTypes)
        Assert.Contains(typeof<Plt>, formatter.SupportedTypes)

    [<Fact>]
    let ``FormatAsync renders a Figure as image/svg+xml`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |], color = "C0") |> ignore
        let fig = plt.CurrentFigure()
        Assert.True(formatter.CanFormat(fig, noCtx))
        let out = formatter.FormatAsync(fig, noCtx).Result
        Assert.Equal("image/svg+xml", out.MimeType)
        Assert.Contains("<svg", out.Content)
        Assert.False out.IsError

    [<Fact>]
    let ``FormatAsync renders a Plt facade as SVG`` () =
        let plt = Plt()
        plt.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        Assert.True(formatter.CanFormat(plt, noCtx))
        let out = formatter.FormatAsync(plt, noCtx).Result
        Assert.Equal("image/svg+xml", out.MimeType)
        Assert.Contains("<svg", out.Content)

    [<Fact>]
    let ``formatter does not claim unrelated values`` () =
        Assert.False(formatter.CanFormat("just a string", noCtx))

    [<Fact>]
    let ``class is marked as a Verso extension and has identity`` () =
        let attrs =
            typeof<MatplotlibFormatter>.GetCustomAttributes(typeof<VersoExtensionAttribute>, false)

        Assert.NotEmpty attrs
        let ext = MatplotlibFormatter() :> IExtension
        Assert.False(System.String.IsNullOrWhiteSpace ext.ExtensionId)
