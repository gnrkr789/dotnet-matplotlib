namespace Matplotlib.Tests

open System.Text
open Xunit
open Matplotlib
open Matplotlib.Backends

module PdfTests =

    [<Fact>]
    let ``FigureCanvas renders a figure to a structurally valid PDF`` () =
        let plt = Pyplot()

        plt.Plot([| 0.0; 1.0; 2.0; 3.0 |], [| 0.0; 1.0; 4.0; 9.0 |], color = "C0", label = "y")
        |> ignore

        plt.Title "pdf demo"

        plt.FillBetween([| 0.0; 1.0; 2.0; 3.0 |], [| 0.0; 1.0; 4.0; 9.0 |], color = "C0", alpha = 0.3)
        |> ignore

        plt.Legend()
        let pdf = FigureCanvas(plt.CurrentFigure()).RenderToPdf()
        let text = Encoding.ASCII.GetString pdf

        Assert.StartsWith("%PDF-1.", text)
        Assert.Contains("/Type /Catalog", text)
        Assert.Contains("/Type /Page", text)
        Assert.Contains("/BaseFont /Helvetica", text)
        Assert.Contains("startxref", text)
        Assert.EndsWith("%%EOF\n", text)
        // alpha fill should have produced an ExtGState
        Assert.Contains("/ExtGState", text)
        // the title text is emitted as a PDF text-show operator
        Assert.Contains("(pdf demo) Tj", text)
