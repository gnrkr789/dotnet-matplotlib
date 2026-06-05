# dotnet-matplotlib

A **native .NET 10** port of [Matplotlib](https://matplotlib.org/) — the de-facto
2D plotting library for Python — rebuilt in idiomatic **F#** following
**Object-Oriented** and **Domain-Driven Design** principles.

> Goal: faithful, 100% behavioral port of Matplotlib's plotting model
> (`Figure` / `Axes` / `Artist` / `Transform` / `Backend`) with a familiar
> `pyplot`-style facade, producing publication-quality output with **zero native
> dependencies** (pure-managed SVG backend; raster/Agg backend on the roadmap).

```fsharp
open Matplotlib

let plt = Pyplot()
plt.Plot([| 1.0; 2.0; 3.0; 4.0 |], [| 1.0; 4.0; 9.0; 16.0 |], color = "C0", label = "y = x^2")
|> ignore
plt.Title "Hello, dotnet-matplotlib"
plt.XLabel "x"
plt.YLabel "y"
plt.Legend()
plt.Savefig "hello.svg"
```

## Status

This project is developed **agile, sprint by sprint**. See [PORTING.md](PORTING.md)
for the porting log and [docs/ROADMAP.md](docs/ROADMAP.md) for the module-by-module
plan tracking Matplotlib parity.

| Layer | Module | Status |
|-------|--------|--------|
| Domain | Primitives (Point2D, Size, BBox, Color) | ✅ Sprint 1 |
| Domain | Transforms (Affine2D, transform stack) | ✅ Sprint 1 |
| Domain | Artists (Line2D, Text, Spine) | ✅ Sprint 1 |
| Domain | Figure / Axes / Axis | ✅ Sprint 1 |
| Domain | Scales & Ticking (Linear, MaxNLocator) | ✅ Sprint 1 |
| Infra | SVG backend | ✅ Sprint 1 |
| App | `Pyplot` facade | ✅ Sprint 1 |
| Domain | Patches (Rectangle, Polygon, Circle) | ✅ Sprint 2 |
| App | `bar` / `barh` / `fill_between` | ✅ Sprint 2 |
| App | `subplots` grid (GridSpec) | ✅ Sprint 2 |
| … | Collections, Images, Colormaps, Log/Date scales, Agg raster backend | 🚧 planned |

## Building

Requires the **.NET 10 SDK**.

```bash
dotnet tool restore           # FSharpLint + Fantomas (one-time)
dotnet build                  # whole solution (warnings are errors)
dotnet test                   # all tests
dotnet run --project samples/Gallery -- out   # render the sample gallery to ./out
```

Style is enforced with **Fantomas**, and **FSharpLint** is the configured lint
rule set — see [LINTING.md](LINTING.md).

## Architecture

`dotnet-matplotlib` mirrors Matplotlib's layered object model under a DDD project
structure. F# is functional-first but fully supports the OOP model Matplotlib
relies on (classes, interfaces, inheritance) — used for the artist hierarchy —
while value objects are immutable records and algorithms live in modules.

```
src/
  Matplotlib.Domain/    # Pure F# domain: Figure, Axes, Artist, Transform,
                        #   Scale, Ticker, Color — no I/O, no rendering deps.
                        #   Defines IRenderer (port) that artists draw onto.
  Matplotlib.Backends/  # Infrastructure: concrete IRenderer implementations
                        #   (SvgRenderer) + FigureCanvas (output adapters).
  Matplotlib/           # Application/facade: the stateful `Pyplot` API.
tests/
  Matplotlib.Tests/     # xUnit unit & golden-file tests.
samples/Gallery/        # Runnable example gallery.
```

See [CLAUDE.md](CLAUDE.md) for the full engineering guide and
[Skills.md](Skills.md) for the porting playbook.

## License & Attribution

`dotnet-matplotlib` is released under the **BSD-3-Clause** license
([LICENSE](LICENSE)). It is an independent re-implementation of Matplotlib; the
Matplotlib copyright notice — *"Copyright (c) 2012- Matplotlib Development Team;
All Rights Reserved"* — is retained in [LICENSE](LICENSE) as required, and the
full Matplotlib (PSF-based) license is available
[upstream](https://github.com/matplotlib/matplotlib/blob/main/LICENSE/LICENSE).
A summary of changes is in [PORTING.md](PORTING.md).

## Citation

If `dotnet-matplotlib` (or Matplotlib, from which it is ported) contributes to a
project that leads to a scientific publication, please cite the original
Matplotlib paper:

> J. D. Hunter, "Matplotlib: A 2D Graphics Environment", *Computing in Science &
> Engineering*, vol. 9, no. 3, pp. 90–95, 2007, doi:10.1109/MCSE.2007.55.

BibTeX:

```bibtex
@Article{Hunter:2007,
  Author    = {Hunter, J. D.},
  Title     = {Matplotlib: A 2D graphics environment},
  Journal   = {Computing in Science \& Engineering},
  Volume    = {9},
  Number    = {3},
  Pages     = {90--95},
  abstract  = {Matplotlib is a 2D graphics package used for Python for
  application development, interactive scripting, and publication-quality
  image generation across user interfaces and operating systems.},
  publisher = {IEEE COMPUTER SOC},
  doi       = {10.1109/MCSE.2007.55},
  year      = 2007
}
```

Matplotlib — Visualization with Python. The Matplotlib Development Team.
<https://matplotlib.org/> · <https://github.com/matplotlib/matplotlib>
