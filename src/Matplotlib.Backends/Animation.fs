namespace Matplotlib.Backends

open System.IO
open Matplotlib.Domain
open Matplotlib.Backends.Raster

/// <summary>
/// Builds an animation by rendering a sequence of figures and encoding them as a
/// looping animated GIF.
/// </summary>
/// <remarks>
/// The counterpart of <c>matplotlib.animation.FuncAnimation</c> + a GIF writer:
/// <paramref name="frameFactory"/> produces the <see cref="Figure"/> for each
/// frame index <c>0 .. frameCount-1</c>. All frames must share the same pixel
/// size (the first frame's size is used).
/// </remarks>
type Animation(frameCount: int, frameFactory: int -> Figure) =

    /// <summary>Render all frames and encode a looping animated GIF.</summary>
    member _.RenderGif(?fps: int, ?scale: int) : byte[] =
        let fps = defaultArg fps 20
        let s = defaultArg scale 2

        let frames =
            [
                for i in 0 .. frameCount - 1 -> FigureCanvas(frameFactory i).RenderToRgba(scale = s)
            ]

        match frames with
        | [] -> GifEncoder.encode 1 1 [] (100 / max 1 fps)
        | (w, h, _) :: _ ->
            let buffers = frames |> List.map (fun (_, _, d) -> d)
            GifEncoder.encode w h buffers (100 / max 1 fps)

    /// <summary>Render the animation and write it to a GIF file.</summary>
    member this.SaveGif(path: string, ?fps: int, ?scale: int) =
        let directory = Path.GetDirectoryName(Path.GetFullPath path)

        if not (Directory.Exists directory) then
            Directory.CreateDirectory directory |> ignore

        File.WriteAllBytes(path, this.RenderGif(?fps = fps, ?scale = scale))
