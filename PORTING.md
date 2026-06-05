# PORTING.md — Changes relative to Matplotlib

This document satisfies clause 3 of the Matplotlib license (a brief summary of
changes made to Matplotlib) and records the porting decisions and parity status.

## Nature of the port

`dotnet-matplotlib` is an **independent native re-implementation** of Matplotlib
in C# / .NET 10. No Matplotlib Python source code is copied or distributed.
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

(Append a row per slice.)
