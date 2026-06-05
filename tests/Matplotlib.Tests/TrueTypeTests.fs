namespace Matplotlib.Tests

open Xunit
open Matplotlib.Domain.Text
open Matplotlib.Backends.Text

module TrueTypeTests =

    // These assertions only run if a system font resolves (true on Windows and on
    // the Linux CI runners, which ship DejaVu); otherwise there is nothing to test.
    [<Fact>]
    let ``Resolved font yields sane metrics and glyph outlines`` () =
        match FontManager.Default.Resolve "sans-serif" with
        | None -> ()
        | Some font ->
            Assert.True(font.UnitsPerEm > 0)
            Assert.True(font.Ascent > 0.0)
            Assert.True(font.Descent < 0.0)
            Assert.True(font.Advance(int 'A') > 0.0)
            Assert.True(font.Advance(int ' ') > 0.0)

            let outline = font.Outline(int 'H')
            Assert.NotEmpty outline

            // every contour is a real polygon
            for contour in outline do
                Assert.True(contour.Length >= 3)

            // 'H' spans a meaningful fraction of the em square
            let ys = outline |> Array.collect (Array.map snd)
            Assert.True(Array.max ys - Array.min ys > float font.UnitsPerEm * 0.3)
