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
  arrays / immutable `[<Struct>]` value records.
- **Rendering**: Matplotlib's C++ Agg default raster backend → a pure-managed
  **SVG** default backend (zero native dependencies), plus pure-managed PNG
  (software rasterizer) and PDF backends behind the same `IRenderer` port.
- **API shape**: snake_case Python API → PascalCase F# API. The `pyplot`
  module becomes the `Plt` facade class; keyword arguments become F# optional
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
| 2026-06-05 | 1 | `pyplot` | `Matplotlib/Plt` | stateful facade |
| 2026-06-05 | 2 | `patches.Patch/Rectangle/Polygon/Circle` | `Domain/Artists/Patch` | filled shapes (zorder 1) |
| 2026-06-05 | 2 | `axes.Axes.bar/barh/fill_between` | `Domain/Axes` + `Matplotlib/Plt` | bar charts & filled regions |
| 2026-06-05 | 2 | `figure.Figure.subplots`, `GridSpec` | `Domain/Figure.Subplots` + `Plt.Subplots` | subplot grid layout |
| 2026-06-05 | 3 | `Artist.sticky_edges` autoscale handling | `Domain/Axes` (`marginExpandSticky`) | bars stick to their baseline |
| 2026-06-05 | 3 | `axes.Axes.step` (pre/post/mid drawstyles) | `Domain/Axes.Step` + `Plt.Step` | step plots |
| 2026-06-05 | 3 | `axes.Axes.errorbar` | `Domain/Axes.Errorbar` + `Plt.Errorbar` | x/y error bars |
| 2026-06-05 | 4 | `axes.Axes.stem` | `Domain/Axes.Stem` + `Plt.Stem` | stem plots |
| 2026-06-05 | 4 | `ticker.AutoMinorLocator` | `Domain/Axes` (`minorTicks`) | minor ticks (4/5 subdivisions) |
| 2026-06-05 | 4 | `Axes.tick_params` (direction), `spines[*].set_visible` | `Domain/Axes` (`TickParams`, `SetSpineVisible`) | tick direction in/out/inout, spine visibility |
| 2026-06-05 | 5 | `markers.py` marker set | `Domain/Artists/Line2D` (`MarkerPaths`) | triangles, pentagon, hexagon, star, thin-diamond, vline, hline |
| 2026-06-05 | 5 | legend `loc` placement | `Domain/Axes` (`LegendLoc`) + `Plt.Legend` | 9 standard legend locations |
| 2026-06-05 | 6 | `axes.Axes.text`, `annotate` | `Domain/Axes` (`Text`, `Annotate`, overlays) | data-space text & basic annotations |
| 2026-06-05 | 7 | legend `loc='best'` | `Domain/Axes` (`BestLegendLoc`) | least-overlap placement |
| 2026-06-05 | 7 | `patches.PathPatch`, `collections.LineCollection/PolyCollection` | `Domain/Artists/{Patch,Collection}` | path patch & bulk collections |
| 2026-06-05 | 7 | `figure.tight_layout` | `Domain/Figure.TightLayout` + `Plt.TightLayout` | margin auto-fit (approx) |
| 2026-06-05 | 8 | `constrained_layout` | `Domain/Figure.ConstrainedLayout` + `Plt` | per-subplot decoration-aware grid layout (approx) |
| 2026-06-06 | 9 | `scale.LogScale`, `ticker.LogLocator/LogFormatter` | `Domain/Scales/Scale`, `Transforms.FunctionalTransform` | base-10 log scale, decade ticks + log minor ticks, `set_xscale/yscale` |
| 2026-06-06 | 10 | `colors.Colormap/Normalize`, `_cm_listed` viridis, `image.AxesImage` | `Domain/Primitives/{Colormap,ColormapData}`, `Artists/Image` | viridis (256-LUT) + gray/jet/hot, linear Normalize, `imshow` |
| 2026-06-06 | 11 | `figure.colorbar` | `Domain/Figure.Colorbar` + `Plt.Colorbar` | gradient axes + value scale on the right; Axes gains XTicksVisible / YTickSide |
| 2026-06-06 | 12 | `axes.pcolormesh`, `axes.contour` | `Domain/Artists/Image` (cell edges), `Domain/Axes` (`marchingSquares`) | quad mesh (origin lower) + iso-line contours |
| 2026-06-06 | 13 | `scale.SymmetricalLogScale`, `scale.LogitScale` | `Domain/Scales/Scale` | symlog (linear near 0, log beyond) + logit scales, locators |
| 2026-06-06 | 14 | `ticker.FixedLocator/FixedFormatter`, `dates.DateFormatter`, `category` | `Domain/Ticking`, `Domain/Axis` (overrides), `Domain/Axes` | categorical axis (`SetXCategories`) + date axis (`PlotDate`, OADate); per-axis locator/formatter overrides |
| 2026-06-06 | 15 | `axes.contourf` | `Domain/Axes.Contourf` + `Plt.Contourf` | banded filled contours (cell quantization) |
| 2026-06-06 | 16 | `pyplot.show`, interactive figure window | `Matplotlib.Gui` (`GdiRenderer`, `PlotWindow`, `Plt.Show`) | opt-in Windows GDI+/WinForms window; live resize re-layout (not on the default zero-dependency path) |
| 2026-06-06 | 16 | `rcParams["font.family"]` | `RcParams.FontFamily`, `Plt.FontFamily` | configurable default font family for all text; honored by SVG + GDI backends |
| 2026-06-06 | 17 | `FigureCanvasAgg.print_png` (raster → PNG) | `Matplotlib.Gui` (`Raster.toBitmap`/`savePng`, `Plt.SavePng`) | opt-in GDI+ raster export to PNG (Windows); reuses `GdiRenderer`. (A pure-managed cross-platform rasterizer follows in sprint 18.) |
| 2026-06-06 | 18 | `backend_agg` + PNG output (pure-managed) | `Matplotlib.Backends.Raster` (`PngEncoder`, `RasterImage`, `RasterRenderer`); `FigureCanvas.RenderToPng`/`SavePng` | cross-platform PNG with zero native deps: managed PNG encoder (`ZLibStream` + CRC-32), even-odd polygon fill, thick-line stroke (round joins), supersampled anti-aliasing |
| 2026-06-06 | 19 | `font_manager` + FreeType glyph loading (TrueType) | `Domain/Text/TrueTypeFont` (pure parser), `Backends/Text/FontManager` (I/O) | pure-managed TTF reader (cmap 4/12, simple + composite glyphs, quadratic flattening); raster backend now renders anti-aliased text via glyph outlines; `Plt.Savefig` writes PNG for `.png` paths |
| 2026-06-06 | 20 | `backend_pdf` | `Matplotlib.Backends.Pdf.PdfRenderer`, `FigureCanvas.RenderToPdf`/`SavePdf` | pure-managed single-page PDF 1.4 writer: path operators, standard Helvetica text, alpha via `ExtGState`, xref table; `Plt.Savefig` writes PDF for `.pdf` paths |
| 2026-06-06 | 21 | `Artist.set_clip_box`, `patches.Patch.set_hatch`, `matplotlib.hatch` | `IRenderer.PushClip`/`PopClip` (SVG/raster/PDF/GDI), `Domain/Axes` (data clipped to the axes box), `Hatching` + `Patch.Hatch`, `Plt.Bar(?hatch)` | plotted data is clipped to the axes box; patches render hatch patterns (`/ \ | - + x`); alpha compositing already honored via per-color alpha |
| 2026-06-06 | 22 | `axes.quiver/hist2d/boxplot/violinplot/streamplot` | `Domain/Axes` + `Matplotlib/Plt` | Phase 5 plot types: arrow fields, 2D histograms (image), box-and-whisker (quartiles/whiskers/fliers), violins (Gaussian-KDE, Silverman bandwidth), streamlines (bilinear sampling, arc-length integration) |
| 2026-06-06 | 23 | `matplotlib.style` / `rcsetup` (matplotlibrc) | `Domain/Style/StyleSheet`, `Plt.UseStyle`/`UseStyleText`/`UseStyleFile` | rcParams text parser (subset of figure/axes/lines/font/tick/grid keys) + built-in styles (`ggplot`, `dark_background`, `grayscale`, `seaborn`) |
| 2026-06-06 | 24 | `mpl_toolkits.mplot3d.Axes3D` | `Domain/Axes3D`, `Figure.AddAxes3D`, `Plt.Axes3D`/`Plot3D`/`Scatter3D`/`PlotWireframe` | 3D axes with orthographic elev/azim projection, unit-cube normalization + auto-fit, reference cube frame, line/scatter/wireframe, title & axis labels |
| 2026-06-06 | 25 | `matplotlib.animation.FuncAnimation` + GIF writer | `Backends.Raster.GifEncoder`, `Backends.Animation`, `Plt.SaveGif` | pure-managed animated GIF89a encoder (variable-width LZW, fixed 8-8-4 palette, looping) over raster frames rendered per frame index; `FigureCanvas.RenderToRgba` |
| 2026-06-06 | 26 | `.NET Interactive` notebook formatters | `Matplotlib.Interactive.Notebook.register` (package `DotnetMatplotlib.Interactive`) | inline SVG rendering of `Figure`/`Plt` in Polyglot/Jupyter notebooks via `Microsoft.DotNet.Interactive.Formatting` |
| 2026-06-06 | 27 | Model Context Protocol server (new capability) | `Matplotlib.Mcp` (.NET tool `DotnetMatplotlib.Mcp`, command `matplotlib-mcp`) | MCP server exposing `plot_line`/`scatter`/`bar`/`heatmap` tools that render to PNG/SVG/PDF for AI agents (`ModelContextProtocol` SDK, stdio transport) |
| 2026-06-06 | 28 | Browser rendering (Blazor WebAssembly) | `samples/BlazorDemo` | the pure-managed library runs in the browser via .NET WASM, rendering plots as inline SVG with no JS/native deps; implicit FSharp.Core floor lowered to 8.0.400 to avoid consumer NU1605 downgrades |
| 2026-06-07 | 29 | `pandas.DataFrame.plot` | `Matplotlib.DataFrame` (package `DotnetMatplotlib.DataFrame`) | `Microsoft.Data.Analysis.DataFrame` extension methods `PlotLine`/`PlotScatter`/`PlotBar`/`PlotHist` returning a `Plt`; usable from C# and F# |
| 2026-06-07 | 30 | server-side reporting (new capability) | `Matplotlib/Report` (in the core `DotnetMatplotlib` package) | composable multi-panel `Report` → SVG/PNG/PDF; pure-managed with zero native dependencies, deterministic byte-reproducible vector output, and a `Sha256()` fingerprint for audit/compliance. Originally a separate `DotnetMatplotlib.Reports` package, since folded into core (no extra dependencies) |
| 2026-06-15 | 30 | accuracy corrections (`colors.Colormap.__call__`, `_cm._jet_data`/`_hot_data`, `ticker.AutoMinorLocator`, mpl2014 contour saddles, `mlab.GaussianKDE`, `axes.scatter` `s`, `markers._set_star`, `colors.to_rgba` `CN`) | `Primitives/{Colormap,ColorResolver}`, `Domain/Axes` (`minorTicks`, `marchingSquares`, `Violinplot`, `Scatter`), `Artists/Line2D` | colormap sampling now matches matplotlib's quantized `int(t·N)` flat lookup (no neighbour blending); `jet`/`hot` rebuilt from the exact per-channel `_jet_data`/`_hot_data` segments; `AutoMinorLocator` subdivisions use `round(mantissa) ∈ {1,5,10}`; contour saddle cells (5/10) disambiguated by the cell-centre mean; violin KDE uses Scott's factor with sample variance (ddof = 1) over a 100-point grid; `scatter` `s` is now a points² area (diameter = √s, default 36); star marker inner-radius ratio 0.381966; only the uppercase `CN` cycle form resolves to the property cycle (`'c'` stays cyan) |
| 2026-06-15 | 31 | completing partial plot ports (`axes.scatter` `c`/`cmap`/array-`s`, `axes.hist`, `axes.errorbar` `capsize`, `axes.fill_betweenx`) | `Domain/Axes` (`Scatter`, `Hist`, `Errorbar`, `FillBetweenx`), `Artists/Line2D` (per-point `MarkerColors`/`MarkerSizes`), `Matplotlib/Plt`, `samples/Gallery` | `scatter` maps a numeric `c` array through a colormap + `Normalize` (default viridis) to per-point colors and accepts a per-point `sizes` array (areas → √ diameters); 1-D `hist` (equal-width bins, `range`, `density`, edge-aligned bars on a sticky baseline) returning heights + edges; `errorbar` renders caps as `_`/`|` markers sized by `capsize`; `fill_betweenx` mirrors `fill_between`; Gallery renders demos for each (`scatter_colormap`/`hist`/`fill_betweenx`/`colormaps`/`errorbar` PNGs) |
| 2026-06-15 | 32 | new chart types & axis control (`axes.stackplot`, `vlines`/`hlines`, `pie`, `set_aspect('equal')`, `axis('off')`) | `Domain/Axes` (`Stackplot`, `Vlines`, `Hlines`, `Pie`, `SetAspect`, `SetAxisOff`, `YTicksVisible`, equal-aspect in `BuildContext`), `Matplotlib/Plt` (`Axis`), `samples/Gallery` | stacked cumulative areas; vertical/horizontal line segments; `pie` wedges (flattened polygon arcs) with white separators; `set_aspect('equal')` shrinks the axes box to equal pixels-per-(scaled)-unit (adjustable='box', default `auto` unchanged), so `pie` is round; `axis('off')` hides ticks + spines; Gallery `stackplot`/`vlines`/`pie` PNGs |
| 2026-06-15 | 33 | `axes.bar` `yerr`, `set_xticks`/`set_yticks`(+labels), and a `FixedFormatter` correctness fix | `Domain/Axes` (`Bar`, `SetXTicks`/`SetYTicks`/`SetYTickLabels`), `Ticking/Formatters` (`LabeledTicksFormatter`), `Matplotlib/Plt` (`Bar`, `XTicks`/`YTicks`), `samples/Gallery` | `bar` draws black `yerr` error bars with `capsize` caps; explicit tick positions/labels on both axes; **fix:** the existing `FixedFormatter` indexed labels by `round(value)` (correct only for 0..n-1 categories) — arbitrary `set_xticks` positions now use `LabeledTicksFormatter`, which matches each tick value to its label (robust to order/in-view filtering). Found via the Gallery `custom_ticks`/`bar` PNGs |

| 2026-06-15 | 34 | reference lines/spans (`axhline`/`axvline`/`axhspan`/`axvspan`) | `Domain/Axes` (`AxHLine`/`AxVLine`/`AxHSpan`/`AxVSpan`, `DrawRefLines`/`DrawRefSpans`, autoscale injection), `Matplotlib/Plt`, `samples/Gallery` | full-span reference lines and shaded bands using a manual blended transform (data coordinate on one axis via `TransData`, axes-fraction on the other via `TransAxes`); the data-axis value participates in autoscale; spans render behind the data, lines on top; Gallery `reflines` PNG |
| 2026-06-15 | 35 | generalized `figure.colorbar` for any ScalarMappable (e.g. a colormapped `scatter`) | `Domain/Figure` (`AddColorbar` helper + `Colorbar(AxesImage)`/`Colorbar(Line2D)` overloads), `Artists/Line2D` (`ScalarMappable`), `Domain/Axes` (`Scatter` stores its mapping), `Matplotlib/Plt`, `samples/Gallery` | `scatter` with a numeric `c` now records its `(Colormap, Normalize)` so `plt.colorbar(sc)` draws the value scale, just like for images; Gallery `scatter_colormap` PNG gains a colorbar |
| 2026-06-15 | 36 | `axes.twinx` (shared x, independent right-hand y) | `Domain/Axes` (`SharedXFrom`, shared-x in `BuildContext`), `Domain/Figure` (`AddTwinX`), `Matplotlib/Plt` (`TwinX`), `samples/Gallery` | a twin overlay axes borrows the source axes' x range/scale at render time (`SharedXFrom`), draws a transparent background and only its right y-spine/ticks, and keeps an independent y scale; Gallery `twinx` PNG |
| 2026-06-15 | 37 | `streamplot` rewritten: Euler → RK4 + density occupancy mask | `Domain/Axes` (`Streamplot`), `samples/Gallery` | streamlines integrate the unit-speed field with classic 4th-order Runge–Kutta (forward + backward from each seed), so trajectories stay on curved/closed field lines (a circular field keeps constant radius, verified). A density occupancy mask claims the cells each line passes through and stops a line that enters another's territory or closes a loop; too-short trajectories release their cells — giving evenly spaced, non-overlapping lines (verified on a uniform field). Gallery `streamplot` PNG |
| 2026-06-15 | 38 | `contourf` filled bands (cell quantization → iso-band polygons) | `Domain/Axes` (`Contourf`), `samples/Gallery` | each grid cell is clipped to every band it overlaps with Sutherland–Hodgman against the two band-edge levels, emitting filled polygons whose boundaries follow the contour lines (verified: a band edge lands on the interpolated contour, not a grid line). Replaces the blocky per-cell image; now uses node coordinates `[0,cols-1]×[0,rows-1]` matching `contour`; Gallery `contourf` PNG |
| 2026-06-28 | 39 | Verso notebook formatters (`IDataFormatter`) | `Matplotlib.Interactive/Verso` (`MatplotlibFormatter`, package `DotnetMatplotlib.Interactive`) | inline SVG rendering of `Figure`/`Plt` in [Verso](https://versonotebooks.com/) notebooks: a `[VersoExtension]`-marked `IDataFormatter` returns `image/svg+xml`, auto-discovered by the host (no registration call). Ships in the existing `DotnetMatplotlib.Interactive` package alongside the .NET Interactive formatters, since Verso is its successor ([#1](https://github.com/gnrkr789/dotnet-matplotlib/issues/1)) |
| 2026-07-28 | 40 | tick-label correctness (`ticker.ScalarFormatter` sigfig search, `figure.tight_layout`/`constrained_layout` decoration sizing) | `Ticking/Formatters` (`ScalarFormatter`), `Domain/Axes` (`MajorTicksFor`, `LeftTickLabels`, `AxesLayout.tickLabelWidth`), `Domain/Figure` (`TightLayout`, `ConstrainedLayout`) | **fix:** the decimal-count search compared against a tolerance floored at an absolute `1e-6`, so every tick below that rounded to `"0"` (a whole `1e-7` axis read `0 0 0 0 0 0`) and close ticks on a large offset lost their decimals; precision now derives from the tick *span*, like matplotlib's `sigfigs` loop. **fix:** both layout estimators reserved room for a hard-coded 4-character tick label, so a wider one overflowed the canvas and calling them left *less* room than the default margins (a `1e9` axis went from −7px to −36px of overhang); they now size the margin from the labels the axes will actually draw, via a renderer-free locator/formatter pass (`MajorTicksFor`), skipping right-side y labels (colorbar / `twinx`). Width still uses the font-independent `0.6·em` per character so vector output stays byte-reproducible |

Known deviations / TODO (in progress; marked with `TODO` comments in the source):
- **`ScalarFormatter` scientific notation / shared offset** — very large or small
  tick values render in full (e.g. `1000000000`); Matplotlib factors out a
  common `×10ⁿ` offset and labels the scaled values. Decimal notation runs out
  below `1e-15`, where distinct ticks start sharing a label — the offset form is
  what fixes that. *(TODO: `Ticking/Formatters`.)*
- **Real glyph-advance text metrics** — `MeasureText` uses a fixed `0.6·em` per
  character; Matplotlib measures exact TrueType advances. The figure layout
  estimators (`tight_layout`/`constrained_layout`) measure the real tick-label
  *text* but deliberately keep the same font-independent per-character width, so
  vector output stays byte-reproducible across machines (`Report.Sha256`).
  Switching `MeasureText` itself to measured advances therefore needs a decision
  about which of the two the layout should follow.
  *(TODO: `Backends/.../MeasureText`.)*
- **Non-zero winding raster fill** — the software rasterizer fills with the
  even-odd rule per sub-path; Matplotlib/Agg use non-zero winding across the whole
  path (affects self-intersecting and holey fills). *(TODO: `Backends/Raster/RasterImage`.)*
- `errorbar` renders caps when `capsize > 0` (drawn as `_`/`|` markers, like
  Matplotlib); the default `errorbar.capsize` is `0`, i.e. no caps.
- `fill_between` autoscaling uses simple 5% margins (Matplotlib does not apply
  sticky edges to `fill_between` by default either).
- `imshow` rasterizes each cell as an SVG rectangle (no interpolation / PNG
  embedding yet); fine for modest arrays, heavier for very large ones.

(Append a row per slice.)
