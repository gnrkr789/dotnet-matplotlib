namespace Matplotlib.Tests

open Xunit
open Matplotlib
open Matplotlib.Domain.Primitives
open Matplotlib.Domain.Style

module StyleSheetTests =

    [<Fact>]
    let ``rcParams text overrides parse into fields`` () =
        let rc =
            StyleSheet.parseText
                "# a comment\nfigure.dpi: 150\nlines.linewidth: 3.5\nfont.family: serif\n"
                RcParams.Default

        assertClose 150.0 rc.FigureDpi
        assertClose 3.5 rc.LinesLineWidth
        Assert.Equal("serif", rc.FontFamily)

    [<Fact>]
    let ``figsize parses a comma pair`` () =
        let rc = StyleSheet.parseText "figure.figsize: 8, 5" RcParams.Default
        assertClose 8.0 rc.FigureSizeInches.Width
        assertClose 5.0 rc.FigureSizeInches.Height

    [<Fact>]
    let ``dark_background style sets dark colors`` () =
        let rc = StyleSheet.byName "dark_background" RcParams.Default
        Assert.True(rc.FigureFaceColor.R < 0.1 && rc.FigureFaceColor.G < 0.1)
        // text becomes light
        Assert.True(rc.TextColor.R > 0.9)

    [<Fact>]
    let ``unknown style is a no-op`` () =
        let rc = StyleSheet.byName "does-not-exist" RcParams.Default
        Assert.Equal(RcParams.Default.FigureFaceColor, rc.FigureFaceColor)

    [<Fact>]
    let ``Pyplot UseStyle affects the created figure`` () =
        let plt = Pyplot()
        plt.UseStyle "dark_background"
        let fig = plt.Figure()
        Assert.True(fig.FaceColor.R < 0.1)
        Assert.Contains("dark_background", plt.AvailableStyles)
