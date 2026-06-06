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
  **SVG** default backend (zero native dependencies), plus pure-managed PNG
  (software rasterizer) and PDF backends behind the same `IRenderer` port.
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
| 2026-06-06 | 11 | `figure.colorbar` | `Domain/Figure.Colorbar` + `Pyplot.Colorbar` | gradient axes + value scale on the right; Axes gains XTicksVisible / YTickSide |
| 2026-06-06 | 12 | `axes.pcolormesh`, `axes.contour` | `Domain/Artists/Image` (cell edges), `Domain/Axes` (`marchingSquares`) | quad mesh (origin lower) + iso-line contours |
| 2026-06-06 | 13 | `scale.SymmetricalLogScale`, `scale.LogitScale` | `Domain/Scales/Scale` | symlog (linear near 0, log beyond) + logit scales, locators |
| 2026-06-06 | 14 | `ticker.FixedLocator/FixedFormatter`, `dates.DateFormatter`, `category` | `Domain/Ticking`, `Domain/Axis` (overrides), `Domain/Axes` | categorical axis (`SetXCategories`) + date axis (`PlotDate`, OADate); per-axis locator/formatter overrides |
| 2026-06-06 | 15 | `axes.contourf` | `Domain/Axes.Contourf` + `Pyplot.Contourf` | banded filled contours (cell quantization) |
| 2026-06-06 | 16 | `pyplot.show`, interactive figure window | `Matplotlib.Gui` (`GdiRenderer`, `PlotWindow`, `Pyplot.Show`) | opt-in Windows GDI+/WinForms window; live resize re-layout (not on the default zero-dependency path) |
| 2026-06-06 | 16 | `rcParams["font.family"]` | `RcParams.FontFamily`, `Pyplot.FontFamily` | configurable default font family for all text; honored by SVG + GDI backends |
| 2026-06-06 | 17 | `FigureCanvasAgg.print_png` (raster → PNG) | `Matplotlib.Gui` (`Raster.toBitmap`/`savePng`, `Pyplot.SavePng`) | opt-in GDI+ raster export to PNG (Windows); reuses `GdiRenderer`. (A pure-managed cross-platform rasterizer follows in sprint 18.) |
| 2026-06-06 | 18 | `backend_agg` + PNG output (pure-managed) | `Matplotlib.Backends.Raster` (`PngEncoder`, `RasterImage`, `RasterRenderer`); `FigureCanvas.RenderToPng`/`SavePng` | cross-platform PNG with zero native deps: managed PNG encoder (`ZLibStream` + CRC-32), even-odd polygon fill, thick-line stroke (round joins), supersampled anti-aliasing |
| 2026-06-06 | 19 | `font_manager` + FreeType glyph loading (TrueType) | `Domain/Text/TrueTypeFont` (pure parser), `Backends/Text/FontManager` (I/O) | pure-managed TTF reader (cmap 4/12, simple + composite glyphs, quadratic flattening); raster backend now renders anti-aliased text via glyph outlines; `Pyplot.Savefig` writes PNG for `.png` paths |
| 2026-06-06 | 20 | `backend_pdf` | `Matplotlib.Backends.Pdf.PdfRenderer`, `FigureCanvas.RenderToPdf`/`SavePdf` | pure-managed single-page PDF 1.4 writer: path operators, standard Helvetica text, alpha via `ExtGState`, xref table; `Pyplot.Savefig` writes PDF for `.pdf` paths |
| 2026-06-06 | 21 | `Artist.set_clip_box`, `patches.Patch.set_hatch`, `matplotlib.hatch` | `IRenderer.PushClip`/`PopClip` (SVG/raster/PDF/GDI), `Domain/Axes` (data clipped to the axes box), `Hatching` + `Patch.Hatch`, `Pyplot.Bar(?hatch)` | plotted data is clipped to the axes box; patches render hatch patterns (`/ \ | - + x`); alpha compositing already honored via per-color alpha |
| 2026-06-06 | 22 | `axes.quiver/hist2d/boxplot/violinplot/streamplot` | `Domain/Axes` + `Matplotlib/Pyplot` | Phase 5 plot types: arrow fields, 2D histograms (image), box-and-whisker (quartiles/whiskers/fliers), violins (Gaussian-KDE, Silverman bandwidth), streamlines (bilinear sampling, arc-length integration) |
| 2026-06-06 | 23 | `matplotlib.style` / `rcsetup` (matplotlibrc) | `Domain/Style/StyleSheet`, `Pyplot.UseStyle`/`UseStyleText`/`UseStyleFile` | rcParams text parser (subset of figure/axes/lines/font/tick/grid keys) + built-in styles (`ggplot`, `dark_background`, `grayscale`, `seaborn`) |
| 2026-06-06 | 24 | `mpl_toolkits.mplot3d.Axes3D` | `Domain/Axes3D`, `Figure.AddAxes3D`, `Pyplot.Axes3D`/`Plot3D`/`Scatter3D`/`PlotWireframe` | 3D axes with orthographic elev/azim projection, unit-cube normalization + auto-fit, reference cube frame, line/scatter/wireframe, title & axis labels |
| 2026-06-06 | 25 | `matplotlib.animation.FuncAnimation` + GIF writer | `Backends.Raster.GifEncoder`, `Backends.Animation`, `Pyplot.SaveGif` | pure-managed animated GIF89a encoder (variable-width LZW, fixed 8-8-4 palette, looping) over raster frames rendered per frame index; `FigureCanvas.RenderToRgba` |
| 2026-06-06 | 26 | `.NET Interactive` notebook formatters | `Matplotlib.Interactive.Notebook.register` (package `DotnetMatplotlib.Interactive`) | inline SVG rendering of `Figure`/`Pyplot` in Polyglot/Jupyter notebooks via `Microsoft.DotNet.Interactive.Formatting` |
| 2026-06-06 | 27 | Model Context Protocol server (new capability) | `Matplotlib.Mcp` (.NET tool `DotnetMatplotlib.Mcp`, command `matplotlib-mcp`) | MCP server exposing `plot_line`/`scatter`/`bar`/`heatmap` tools that render to PNG/SVG/PDF for AI agents (`ModelContextProtocol` SDK, stdio transport) |

Known deviations (to refine in later sprints):
- `errorbar` draws the error lines only; caps (`capsize`) are not yet rendered
  (Matplotlib's default `errorbar.capsize` is also `0`, i.e. no caps).
- `fill_between` autoscaling uses simple 5% margins (Matplotlib does not apply
  sticky edges to `fill_between` by default either).
- `imshow` rasterizes each cell as an SVG rectangle (no interpolation / PNG
  embedding yet); fine for modest arrays, heavier for very large ones.

(Append a row per slice.)
