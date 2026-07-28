# Changelog

All notable changes to **dotnet-matplotlib** are recorded here. The project follows
[Semantic Versioning](https://semver.org/). While on `0.x` (pre-1.0), minor releases
may contain breaking changes.

## [0.1.0] — 2026-07-28

> **Upgrade note for `Report.Sha256()` users:** this release's tick-label and layout fixes
> change rendered output, so report fingerprints produced by `0.0.9` will not all match
> those from `0.1.0`. Re-baseline stored hashes when upgrading. Only figures with
> sub-`1e-6` ticks or a `TightLayout` / `ConstrainedLayout` call are affected — of the 29
> sample gallery outputs, 24 are byte-identical across the upgrade and 5 changed.

### Fixed

- **Tick labels below `1e-6` no longer all render as `0`.** `ScalarFormatter` picked its
  decimal count with a tolerance floored at an absolute `1e-6`, so every tick on, say, a
  `1e-7` axis rounded to a bare `"0"` and the axis became unreadable. Precision now
  follows the tick *span* (as matplotlib's significant-figure search does), which also
  restores the decimals that separate closely-spaced ticks sitting on a large offset
  (`1000000.0 / 1000000.5 / 1000001.0` used to collapse to `1000000 / 1000000 / 1000001`).
  Labels stay distinct down to `1e-15`; below that, decimal notation runs out and the
  planned `×10ⁿ` offset label is still needed.
- **`TightLayout` / `ConstrainedLayout` no longer push wide tick labels off the canvas.**
  Both reserved room for a fixed four-character tick label, so a wider one (e.g.
  `1000000000`) overflowed — and calling either left *less* room than the untouched
  default margins, making the clipping worse rather than better. They now measure the
  tick labels the axes will actually draw, and ignore labels on a right-side y axis (a
  colorbar or `twinx` overlay) that the left margin does not have to accommodate.

### Added

- **Verso notebook support**, shipped inside the existing `DotnetMatplotlib.Interactive`
  package. A `[VersoExtension]`-marked `IDataFormatter` renders a `Figure` or `Plt`
  returned from a cell as inline SVG (`image/svg+xml`), discovered automatically by the
  Verso host (no registration call). [Verso](https://versonotebooks.com/) is the
  actively-developed successor to the now-deprecated .NET Interactive / Polyglot
  Notebooks ([#1](https://github.com/gnrkr789/dotnet-matplotlib/issues/1)).

### Changed

- **Folded `DotnetMatplotlib.Reports` into the core `DotnetMatplotlib` package.** The
  `Report` type (namespace `Matplotlib.Reports`) needs nothing beyond the core library,
  so it no longer ships as a separate package — trimming the published set to four.
  **Migration:** drop the `DotnetMatplotlib.Reports` package reference; `Report` now
  comes from `DotnetMatplotlib`, and `open Matplotlib.Reports` is unchanged.

## [0.0.9] — 2026-06-20

### Changed — ⚠️ BREAKING

- **Renamed the plotting facade `Pyplot` → `Plt`.** This is a .NET / F# port, so a
  `Py`-prefixed type name read oddly. The stateful facade is now **`Plt`**, matching
  the `plt` alias Matplotlib users already use (`import matplotlib.pyplot as plt`).
  The object-oriented API (`Figure` / `Axes` / `Axes3D`) is unchanged.

  **Migration — replace `Pyplot()` with `Plt()`:**

  ```fsharp
  // before
  let plt = Pyplot()
  // after
  let plt = Plt()
  ```

  ```csharp
  // C#: var plt = new Pyplot();  ->  var plt = new Plt();
  ```

  The rename touches every package that surfaces the facade:
  - `DotnetMatplotlib` — the facade type itself.
  - `DotnetMatplotlib.DataFrame` — the `PlotLine` / `PlotScatter` / `PlotBar` /
    `PlotHist` extension methods now return a `Plt`.
  - `DotnetMatplotlib.Interactive`, `DotnetMatplotlib.Mcp` — internal use updated.
  - `Matplotlib.Gui` (opt-in) — the interactive `Show()` extension now extends `Plt`.

  Only the **name** changed: every member, signature and rendered byte of output is
  identical. References to Matplotlib's own `matplotlib.pyplot` module (in docs and
  doc-comments) are left as-is, since they accurately describe the upstream Python API.

## [0.0.8] — 2026-06-15

### Added

- **`DotnetMatplotlib.Reports`** — a deterministic, dependency-free server-side
  reporting engine: compose multi-panel SVG / PNG / PDF reports with byte-reproducible
  output that can be checksummed (`report.Sha256()`) for audit and compliance.
- **Data tables** — `Axes.Table` / `Plt.Table` / `Report.AddTable`, plus a Gallery sample.

### Removed

- FSharpLint and Fantomas were removed from the repository and CI; in-repo style is no
  longer enforced.

## [0.0.7] — 2026-06-15

### Changed

- Matplotlib parity pass: accuracy fixes, additional plot types, and backend rewrites.

## [0.0.6] — 2026-06-07

### Added

- **`DotnetMatplotlib.DataFrame`** — `Microsoft.Data.Analysis.DataFrame` plotting
  extensions (`PlotLine` / `PlotScatter` / `PlotBar` / `PlotHist`), usable from C# and F#.

## [0.0.5] — 2026-06-06

### Added

- Blazor WebAssembly demo (the library runs in the browser via .NET WASM).

### Fixed

- Lowered the `FSharp.Core` floor to `8.0.400` to avoid an NU1605 downgrade warning for
  consumers.

## [0.0.4] — 2026-06-06

### Added

- **`DotnetMatplotlib.Mcp`** — a Model Context Protocol server (`.NET` tool) that lets
  AI agents create plots with the library.

## [0.0.1] – [0.0.3] — 2026-06-06

- Initial public packages and the NuGet + GitHub Packages publish pipeline; added
  `CONTRIBUTING` and `SECURITY` docs and a gallery-less README for the NuGet page.
