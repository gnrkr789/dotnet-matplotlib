# Skills.md — Porting playbook for dotnet-matplotlib (F#)

Concrete, repeatable recipes for porting Matplotlib to native **F#** on .NET 10.
Pair this with `CLAUDE.md` (the engineering contract), `docs/ROADMAP.md` (what to
port next), and `LINTING.md` (style gates).

## Skill 0 — Locate the reference

Upstream lives at `example/matplotlib/lib/matplotlib/`. Useful entry points:

| You want… | Read upstream |
|-----------|---------------|
| Figure container | `figure.py` (`FigureBase`, `Figure`) |
| The Axes workhorse | `axes/_axes.py`, `axes/_base.py` |
| Axis/ticks/labels | `axis.py` (`Axis`, `XAxis`, `YAxis`, `Tick`) |
| Lines/markers | `lines.py` (`Line2D`), `markers.py` |
| Text | `text.py` |
| Patches (Rectangle, …) | `patches.py` |
| Coordinate transforms | `transforms.py` |
| Scales (linear/log/…) | `scale.py` |
| Tick locators/formatters | `ticker.py` (`MaxNLocator`, `ScalarFormatter`) |
| Colors / colormaps | `colors.py`, `_color_data.py`, `_cm*.py` |
| Backends / renderer API | `backend_bases.py` (`RendererBase`, `GraphicsContextBase`) |
| Defaults | `mpl-data/matplotlibrc`, `rcsetup.py` |
| pyplot facade | `pyplot.py` |

Never edit upstream. Treat it as a spec.

## Skill 1 — Port a value object (struct record)

```fsharp
/// <remarks>Ported from matplotlib.transforms.Bbox.</remarks>
[<Struct>]
type BBox =
    { X0: float; Y0: float; X1: float; Y1: float }
    member this.Width = this.X1 - this.X0          // get-only members are fine
```

- Mirror Matplotlib semantics exactly, e.g. a `BBox` may be "un-sorted"
  (`X0 > X1` ⇒ inverted axis) so width/height are signed while `XMin`/`XMax` are
  orientation independent.
- **FS0052 trap:** calling a *custom* instance member on a struct **literal** copies
  the value (error under warnings-as-errors). Bind to a `let` first:
  `let b: BBox = {…} in b.Width`. Pure module functions sidestep this.
- Add a companion `[<RequireQualifiedAccess>] module` for constructors/helpers
  (`BBox.fromBounds`, `BBox.unit`).

## Skill 2 — Port a transform

`transforms.py` is the heart of layout. The data→pixels pipeline:

```
transData = transLimits + transAxes        // Matplotlib's '+' = "apply left, then right"
```

- `transLimits` = `BBoxTransform(dataBox, unitSquare)`.
- `transAxes`  = `BBoxTransform(unitSquare, axesPixelBox)`.
- Implement `ITransform` (`Transform: Point2D -> Point2D`, `Inverted: unit -> ITransform`).
  Provide `Affine2D` (matrix `[[A C E][B D F][0 0 1]]`, matching
  `Affine2DBase.get_matrix`), `IdentityTransform`, `CompositeTransform`,
  `BlendedTransform`, `BBoxTransform`. Compose with `Transforms.compose`.
- Verify against known mappings in tests (unit square → pixel box, inverse round-trips).

## Skill 3 — Port an Artist (OOP in F#)

```fsharp
[<AbstractClass>]
type Artist() =
    member val Visible = true with get, set
    member val ZOrder = 0.0 with get, set
    member val Transform: ITransform = IdentityTransform.Instance :> ITransform with get, set
    abstract member Draw: renderer: IRenderer -> unit
```

- Use `type Foo(args) as this = inherit Artist()` and a `do this.ZOrder <- …`
  block to set Matplotlib's per-type default zorder (Line2D 2, Spine 2.5, Text 3).
- An Artist computes display coordinates via its `Transform` and draws through
  `IRenderer` — it must never know the concrete backend.

## Skill 4 — Port a tick locator / formatter

`ticker.py`. `MaxNLocator` is the default linear-axis locator (`AutoLocator` =
`steps = [1,2,2.5,5,10]`). Port `scale_range`, `_staircase`, `_Edge_integer`
(`le`/`ge`/`closeto`) and `_raw_ticks` faithfully; `tick_values` calls
`nonsingular` first. `ScalarFormatter` picks decimals from the tick spacing and
pads consistently. Match outputs against Matplotlib for canonical ranges
(`0..1 → 0,.2,…,1`; `0..100 → 0,20,…,100`) in tests.

## Skill 5 — Add a drawing primitive to the renderer (port)

`IRenderer` is the boundary, kept minimal and path-centric (mirrors
`RendererBase`):

```fsharp
type IRenderer =
    abstract member CanvasSizePx: Size
    abstract member Dpi: float
    abstract member DrawPath: gc: GraphicsContext * path: Path * fill: Color option -> unit
    abstract member DrawText: gc:GraphicsContext * x:float * y:float * text:string *
                              font:FontProperties * angleDegrees:float * hAlign:HAlign * vAlign:VAlign -> unit
    abstract member MeasureText: text: string * font: FontProperties -> Size
```

Prefer expressing artists/markers as `Path`s so every backend gets them for free.
Line widths/marker sizes are in **points**; convert with `dpi/72.0`.

## Skill 6 — Expose via the Pyplot facade

`pyplot.py` is a stateful wrapper over the OO API. `Pyplot` holds the current
figure/axes; `Plot`, `Scatter`, `Title`, `XLabel`, `Legend`, `Savefig` delegate
to the current `Axes`/`Figure`. Accept Matplotlib-style strings at the facade
edge (`color = "C0"`, `marker = "o"`) and resolve them (`ColorResolver`,
`Styles.parseLineStyle/parseMarker`); forward optionals with F#'s `?arg = opt`.
The OO API (`Figure()`, `fig.AddSubplot()`) must stay usable without the facade.

## Skill 7 — Test like Matplotlib (xUnit + F#)

- **Unit**: transforms, locators, color parsing, scale math — assert exact
  numbers within a tight tolerance (`assertClose`, 1e-9).
- **Golden**: render a known figure to SVG and assert on stable substrings
  (`<path`, a color hex, element counts). Avoid brittle full-file diffs until the
  SVG writer is frozen.
- Remember the FS0052 trap in tests too (bind struct literals before member access).

## Skill 8 — Definition of done

See `CLAUDE.md` §7. In short: cited F# domain code + backend path + facade +
tests, with `dotnet build`, `dotnet test`, and `dotnet fantomas --check` all
green, code complying with `fsharplint.json`, and `README`/`PORTING.md` updated.
