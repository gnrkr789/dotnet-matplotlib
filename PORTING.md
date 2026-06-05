# PORTING.md — Changes relative to Matplotlib

This document records the porting decisions and parity status relative to the
reference Matplotlib implementation.

## Nature of the port

`dotnet-matplotlib` is an **independent native re-implementation** of Matplotlib
in F# / .NET 10. No Matplotlib Python source code is copied or distributed.
Behavior, the object model, default styling, numeric constants, and color tables
are reproduced so that output matches Matplotlib's. The reference implementation
(`example/matplotlib`, Matplotlib `v3.11.0rc2`) is used only as a specification
and is not part of this repository (it is git-ignored).

## High-level changes from Matplotlib

- **Language/runtime**: Python/NumPy → **F# / .NET 10**. NumPy arrays become F#
  arrays / immutable `[<Struct>]` value records. Code style is enforced with
  Fantomas; FSharpLint is the configured lint rule set (see `LINTING.md`).
- **Rendering**: Matplotlib's C++ Agg default raster backend → a pure-managed
  **SVG** default backend (zero native dependencies). An Agg-equivalent raster
  backend is on the roadmap behind the same `IRenderer` port.
- **API shape**: snake_case Python API → PascalCase F# API. The `pyplot`
  module becomes the `Pyplot` facade class; keyword arguments become F# optional
  parameters (`?arg`). Value objects are records; the artist/transform hierarchy
  uses F# classes + interfaces; pure algorithms live in modules.
- **Architecture**: organized under Domain-Driven Design layers
  (`Domain` / `Backends` / facade) instead of a flat Python package, but the
  Figure/Axes/Artist/Transform/Backend object model is preserved 1:1.

## Parity log

| Date | Sprint | Ported from (matplotlib) | dotnet-matplotlib | Notes |
|------|--------|--------------------------|-------------------|-------|
| 2026-06-05 | 1 | `transforms.Bbox`, `Affine2D`, `BboxTransform`, blended/composite | `Domain/Transforms/*`, `Primitives/BBox` | core transform stack |
| 2026-06-05 | 1 | `_color_data.BASE/TABLEAU/CSS4`, `colors.to_rgba` | `Domain/Primitives/Color`, `ColorResolver` | color parsing |
| 2026-06-05 | 1 | `ticker.MaxNLocator`, `ScalarFormatter` | `Domain/Ticking/*` | linear-axis ticks |
| 2026-06-05 | 1 | `lines.Line2D`, `text.Text`, `spines.Spine`, `axis.Tick` | `Domain/Artists/*` | core artists |
| 2026-06-05 | 1 | `figure.Figure`, `axes._base/_axes`, `axis.XAxis/YAxis` | `Domain/Figure,Axes,Axis` | containers |
| 2026-06-05 | 1 | `backend_bases.RendererBase`, `backend_svg` | `Rendering/IRenderer`, `Backends/Svg/SvgRenderer` | SVG output |
| 2026-06-05 | 1 | `pyplot` | `Matplotlib/Pyplot` | stateful facade |
| 2026-06-05 | 2 | `patches.Patch/Rectangle/Polygon/Circle` | `Domain/Artists/Patch` | filled shapes (zorder 1) |
| 2026-06-05 | 2 | `axes.Axes.bar/barh/fill_between` | `Domain/Axes` + `Matplotlib/Pyplot` | bar charts & filled regions |
| 2026-06-05 | 2 | `figure.Figure.subplots`, `GridSpec` | `Domain/Figure.Subplots` + `Pyplot.Subplots` | subplot grid layout |
| 2026-06-05 | 3 | `Artist.sticky_edges` autoscale handling | `Domain/Axes` (`marginExpandSticky`) | bars stick to their baseline |
| 2026-06-05 | 3 | `axes.Axes.step` (pre/post/mid drawstyles) | `Domain/Axes.Step` + `Pyplot.Step` | step plots |
| 2026-06-05 | 3 | `axes.Axes.errorbar` | `Domain/Axes.Errorbar` + `Pyplot.Errorbar` | x/y error bars |
| 2026-06-05 | 4 | `axes.Axes.stem` | `Domain/Axes.Stem` + `Pyplot.Stem` | stem plots |
| 2026-06-05 | 4 | `ticker.AutoMinorLocator` | `Domain/Axes` (`minorTicks`) | minor ticks (4/5 subdivisions) |
| 2026-06-05 | 4 | `Axes.tick_params` (direction), `spines[*].set_visible` | `Domain/Axes` (`TickParams`, `SetSpineVisible`) | tick direction in/out/inout, spine visibility |
| 2026-06-05 | 5 | `markers.py` marker set | `Domain/Artists/Line2D` (`MarkerPaths`) | triangles, pentagon, hexagon, star, thin-diamond, vline, hline |
| 2026-06-05 | 5 | legend `loc` placement | `Domain/Axes` (`LegendLoc`) + `Pyplot.Legend` | 9 standard legend locations |
| 2026-06-05 | 6 | `axes.Axes.text`, `annotate` | `Domain/Axes` (`Text`, `Annotate`, overlays) | data-space text & basic annotations |
| 2026-06-05 | 7 | legend `loc='best'` | `Domain/Axes` (`BestLegendLoc`) | least-overlap placement |
| 2026-06-05 | 7 | `patches.PathPatch`, `collections.LineCollection/PolyCollection` | `Domain/Artists/{Patch,Collection}` | path patch & bulk collections |
| 2026-06-05 | 7 | `figure.tight_layout` | `Domain/Figure.TightLayout` + `Pyplot.TightLayout` | margin auto-fit (approx) |
| 2026-06-05 | 8 | `constrained_layout` | `Domain/Figure.ConstrainedLayout` + `Pyplot` | per-subplot decoration-aware grid layout (approx) |
| 2026-06-06 | 9 | `scale.LogScale`, `ticker.LogLocator/LogFormatter` | `Domain/Scales/Scale`, `Transforms.FunctionalTransform` | base-10 log scale, decade ticks + log minor ticks, `set_xscale/yscale` |
| 2026-06-06 | 10 | `colors.Colormap/Normalize`, `_cm_listed` viridis, `image.AxesImage` | `Domain/Primitives/{Colormap,ColormapData}`, `Artists/Image` | viridis (256-LUT) + gray/jet/hot, linear Normalize, `imshow` |

Known deviations (to refine in later sprints):
- `errorbar` draws the error lines only; caps (`capsize`) are not yet rendered
  (Matplotlib's default `errorbar.capsize` is also `0`, i.e. no caps).
- `fill_between` autoscaling uses simple 5% margins (Matplotlib does not apply
  sticky edges to `fill_between` by default either).
- `imshow` rasterizes each cell as an SVG rectangle (no interpolation / PNG
  embedding yet); fine for modest arrays, heavier for very large ones.

(Append a row per slice.)
