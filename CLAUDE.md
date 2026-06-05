# CLAUDE.md — Engineering guide for dotnet-matplotlib

This file is the operating contract for any agent (or human) working in this
repository. Read it fully before making changes.

## 1. Mission

Produce a **native, idiomatic .NET 10** port of **Matplotlib**, written in **F#**,
faithful to Matplotlib's behavior and public-API semantics ("100% port" as the
north star), built with **OOP** and **Domain-Driven Design**. Output must be
publication-quality and reproducible.

The canonical reference source is the upstream clone at
`example/matplotlib/` (Matplotlib `v3.11.0rc2`, Python). It is **git-ignored**
and is **reference only** — never edit it, never commit it.

## 2. Golden rules

1. **F# by default.** All library, test, and sample code is F# (`.fs`). Use F#'s
   OOP features (classes, interfaces, abstract members, inheritance) for the
   artist/transform hierarchy that Matplotlib's model depends on; use immutable
   records / `[<Struct>]` value types for value objects; use modules for pure
   functions and algorithms.
2. **Faithful, not a transliteration.** Port Matplotlib's *behavior, math, and
   object model*, expressed in idiomatic F#. Reproduce numeric constants,
   default `rcParams`, color tables, tick algorithms, and transform math
   exactly. Cite the source: add an XML-doc `<remarks>Ported from matplotlib
   <module>.<symbol>.</remarks>` so parity is auditable, and log notable
   changes in `PORTING.md`.
3. **Domain stays pure.** `Matplotlib.Domain` has **no** rendering, file, or
   platform dependencies. Rendering happens only through the `IRenderer` port.
   Backends live in `Matplotlib.Backends`.
4. **Zero native dependencies by default.** The default backend is a
   pure-managed SVG writer. Any native/raster backend must be opt-in and live
   behind `IRenderer`.
5. **Lint & format are gates.** Code must pass `dotnet fantomas --check` and
   comply with `fsharplint.json` (see `LINTING.md`). The build runs with
   `TreatWarningsAsErrors` — keep it clean.
6. **Tests are not optional.** Every domain algorithm gets unit tests; every
   backend change gets a golden/serialization test. Keep `dotnet test` green.
7. **Agile cadence.** Work in small vertical slices (a feature that runs
   end-to-end: domain → backend → facade → test → sample). Update the task list
   and the status table in `README.md` each slice.

## 3. Architecture (DDD layering)

```
Matplotlib.Domain (core F# library, no deps)
  Primitives/   Point2D, Size, Interval, BBox  (struct records)
                Color, ColorData, ColorResolver
  Transforms/   ITransform, Affine2D, IdentityTransform, CompositeTransform,
                BlendedTransform, BBoxTransform   (data→axes→figure→display)
  Ticking/      ITickLocator, MaxNLocator; ITickFormatter, ScalarFormatter
  Scales/       IScale, LinearScale  (+ Log/Symlog later)
  Style/        FontProperties, LineStyle, MarkerStyle, PropertyCycler, RcParams
  Rendering/    Path, GraphicsContext, IRenderer (PORT)   <-- abstraction only
  Artists/      Artist (abstract), Line2D, Text, Spine
  Axis.fs, Axes.fs, Figure.fs

Matplotlib.Backends (infrastructure)  ->  references Domain
  Svg/SvgRenderer  (: IRenderer, pure-managed)
  FigureCanvas

Matplotlib (application/facade)       ->  references Domain + Backends
  Pyplot   stateful pyplot-like API

tests/Matplotlib.Tests (xUnit, F#)
samples/Gallery (F# console)
```

Dependency direction is strictly inward: `Matplotlib` → `Backends` → `Domain`.
`Domain` depends on nothing but FSharp.Core / BCL.

### Key domain model (mirrors Matplotlib)

- **Artist** — abstract base for anything drawable; holds `Visible`, `ZOrder`,
  `Transform` and an abstract `Draw(renderer)`. Subclasses: `Line2D`, `Text`,
  `Spine`, (future `Patch`, `Collection`, `Image`).
- **Figure** — top-level container of `Axes`; owns size (inches) & dpi.
- **Axes** — the workhorse: data limits, `XAxis`/`YAxis`, spines, plotted lines,
  title, legend; computes the transform stack and draws itself.
- **Axis** — one of X/Y; owns scale, grid flag and (via the scale) locator/formatter.
- **Transform** — composable coordinate mapping. The data→display pipeline is
  `transData = transLimits + transAxes` (BBox→unit→axes-pixels), origin
  bottom-left; the SVG backend flips Y on write.

## 4. F# conventions (aligned with fsharplint.json)

- Types/members/modules/union-cases/record-fields: **PascalCase**. Interfaces:
  `I`-prefixed PascalCase. Parameters/locals: **camelCase**.
- Value objects: `[<Struct>]` records (`Point2D`, `Size`, `Interval`, `BBox`).
  NOTE: calling a *custom* instance member on a struct **rvalue/literal** triggers
  `FS0052` under warnings-as-errors — bind it to a `let` first. `Color` is a
  reference record (it has many custom members).
- Pure helpers go in `[<RequireQualifiedAccess>] module`s; keep functions small
  (FSharpLint caps function length at 120 lines).
- Angles in **degrees** at API edges (as Matplotlib); radians internally.
- Units: figure size in **inches**, dpi default **100**, 1 point = 1/72 inch.
  Defaults (from `matplotlibrc`): figsize `6.4 x 4.8`, white figure/axes face,
  black edges, `lines.linewidth 1.5`, `font.size 10`, prop_cycle = `tab10`.

## 5. Workflow for a slice

1. Pick the next item from `docs/ROADMAP.md` / task list.
2. Read the upstream reference under `example/matplotlib/lib/matplotlib/…`.
3. Implement in `Domain` (pure) with `<remarks>` citing the source. Remember F#
   file compile order — add new files to the `.fsproj` in dependency order.
4. Extend a backend path if it needs new drawing primitives.
5. Expose via `Pyplot` if user-facing.
6. Write tests (unit for math, golden/string-contains for SVG).
7. `dotnet fantomas src tests samples`, then `dotnet build` (warnings = errors)
   and `dotnet test` must pass.
8. Add a runnable example under `samples/` if user-facing.
9. Update `README.md` status table + `PORTING.md`.

## 6. Commands

```bash
dotnet tool restore
dotnet build                                   # warnings are errors
dotnet test
dotnet fantomas --check src tests samples      # format gate
dotnet run --project samples/Gallery -- out    # render gallery to ./out
```

## 7. Definition of done (per slice)

- [ ] F# domain code with `<remarks>` citing the upstream symbol.
- [ ] Backend path (if new primitive) implemented in `SvgRenderer`.
- [ ] Facade method (if user-facing).
- [ ] Unit + golden tests; `dotnet build` & `dotnet test` green.
- [ ] `dotnet fantomas --check` green; code complies with `fsharplint.json`.
- [ ] Sample (if user-facing) + `README.md` status + `PORTING.md` updated.

## 8. Do / Don't

- ✅ Reproduce Matplotlib numbers exactly; add the source citation.
- ✅ Keep `Domain` free of `System.Drawing`, Skia, files, threads.
- ✅ Keep slices small and shippable; keep the tree formatted.
- ❌ Don't edit or commit anything under `example/`.
- ❌ Don't add a native dependency to the default path.
- ❌ Don't leave the build red, warnings unaddressed, or files unformatted.

See `Skills.md` for the concrete porting playbook and recipes, and `LINTING.md`
for the lint/format setup.
