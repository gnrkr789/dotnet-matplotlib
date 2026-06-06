namespace Matplotlib.Tests

open System.IO
open System.Text
open Xunit
open Matplotlib
open Matplotlib.Domain
open Matplotlib.Backends
open Matplotlib.Backends.Raster

module AnimationTests =

    [<Fact>]
    let ``GifEncoder writes a valid GIF89a header and trailer`` () =
        let frame = Array.create (2 * 2 * 4) 255uy // white 2x2
        let gif = GifEncoder.encode 2 2 [ frame; frame ] 5
        Assert.Equal("GIF89a", Encoding.ASCII.GetString(gif, 0, 6))
        Assert.Equal(0x3Buy, gif[gif.Length - 1])
        Assert.True(gif.Length > 780) // includes the 768-byte global color table

    [<Fact>]
    let ``Animation renders a multi-frame gif`` () =
        let factory i =
            let fig = Figure()
            let ax = fig.AddSubplot()
            ax.Scatter([| float i |], [| float i |]) |> ignore
            ax.SetXLim(0.0, 5.0)
            ax.SetYLim(0.0, 5.0)
            fig

        let gif = Animation(5, factory).RenderGif(fps = 10, scale = 1)
        Assert.Equal("GIF89a", Encoding.ASCII.GetString(gif, 0, 6))
        Assert.Equal(0x3Buy, gif[gif.Length - 1])
        Assert.True(gif.Length > 2000)

    [<Fact>]
    let ``Pyplot SaveGif writes a gif file`` () =
        let plt = Pyplot()
        let path = Path.Combine(Path.GetTempPath(), "mpltest_anim.gif")

        plt.SaveGif(
            path,
            3,
            (fun i ->
                let fig = Figure()
                let ax = fig.AddSubplot()
                ax.Plot([| 0.0; 1.0 |], [| 0.0; float i |]) |> ignore
                fig),
            fps = 10,
            scale = 1
        )

        Assert.True(File.Exists path)
        let bytes = File.ReadAllBytes path
        Assert.Equal("GIF89a", Encoding.ASCII.GetString(bytes, 0, 6))
        File.Delete path
