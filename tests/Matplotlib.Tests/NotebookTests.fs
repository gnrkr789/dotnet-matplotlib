namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Interactive
open Microsoft.DotNet.Interactive.Formatting

module NotebookTests =

    [<Fact>]
    let ``register makes Pyplot format as inline SVG html`` () =
        Notebook.register ()
        let plt = Pyplot()
        plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |], color = "C0") |> ignore
        let html = Formatter.ToDisplayString(plt, "text/html")
        Assert.Contains("<svg", html)

    [<Fact>]
    let ``register is idempotent and Figure also renders as SVG`` () =
        Notebook.register ()
        Notebook.register ()
        let fig = Matplotlib.Domain.Figure()
        let ax = fig.AddSubplot()
        ax.Plot([| 0.0; 1.0 |], [| 0.0; 1.0 |]) |> ignore
        let html = Formatter.ToDisplayString(fig, "text/html")
        Assert.Contains("<svg", html)
