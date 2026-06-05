# ROADMAP — Matplotlib parity, module by module

Tracks the path to a faithful, full port. Ordered by dependency and value.
Status: ✅ done · 🚧 in progress · ⬜ planned.

## Phase 1 — Core OO engine (MVP line plot → SVG)
- ✅ Primitives: `Point2D`, `Size`, `BBox`, `Interval`, `Color`, `ColorResolver`
- ✅ Transforms: `ITransform`, `Affine2D`, `Identity`, `Composite`, `Blended`, `BBoxTransform`
- ✅ Scales: `LinearScale`
- ✅ Ticking: `MaxNLocator`, `ScalarFormatter`
- ✅ Artists: `Artist`, `Line2D` (+ markers), `Text`, `Spine` (ticks drawn by `Axes`)
- ✅ Containers: `Figure`, `Axes`, `Axis` (X/Y)
- ✅ Style: `RcParams`, `PropertyCycler` (tab10), `LineStyle`, `MarkerStyle`
- ✅ Rendering port: `IRenderer`, `GraphicsContext`, path model
- ✅ Backend: `SvgRenderer`, `FigureCanvas`
- ✅ Facade: `Pyplot` (`Plot`, `Title`, `XLabel`, `YLabel`, `Legend`, `Savefig`)

## Phase 2 — Plot types & styling breadth (complete)
- ✅ `scatter`, `bar`/`barh` (sticky baseline), `fill_between`, `step`, `errorbar`, `stem`
- ✅ Patches: `Rectangle`, `Circle`, `Polygon`, `PathPatch`; `LineCollection`, `PolyCollection`
- ✅ Markers: full set (circle, point, square, diamond, thin-diamond, triangles ▲▼◀▶, pentagon, hexagon, star, +, x, |, _)
- ✅ Legend: box, `loc` (9 positions), auto-`best`; `text`, `Annotation` (basic arrow)
- 🚧 Grid ✅; minor ticks ✅; tick params (direction ✅, rest ⬜); spine visibility ✅
- ✅ `subplots`, `GridSpec`, `tight_layout`, `constrained_layout` (approx)

## Phase 3 — Scales, color, data domains (complete)
- ✅ `LogScale`, `SymlogScale`, `LogitScale` (decade / symlog / logit locators)
- ✅ Date axis (`DateFormatter`, `plot_date` via OADate) + `FixedLocator`/`FixedFormatter`
- ✅ Categorical axis (`set_xcategories`, string→index)
- 🚧 Colormaps: `viridis` ✅ (+ gray/jet/hot), `Normalize` ✅, `colorbar` ✅; more `_cm*` maps ⬜
- ✅ `imshow`, `pcolormesh`, `contour` (marching squares), `contourf` (banded)

## Phase 4 — Backends & fidelity
- 🚧 Interactive GUI window (opt-in, Windows: WinForms + GDI+): `Matplotlib.Gui`
  (`GdiRenderer`, `PlotWindow`, `plt.Show()`) — live on-screen raster + resize re-layout
- 🚧 Raster backend → PNG:
  - opt-in GDI+ export ✅ (`Raster.savePng` / `plt.SavePng`, Windows, with text)
  - pure-managed cross-platform rasterizer + PNG encoder ✅
    (`Matplotlib.Backends.Raster`, `FigureCanvas.RenderToPng`/`SavePng`; even-odd
    fill, thick strokes, supersampled AA) — managed text & dashes await TrueType ⬜
- ⬜ Font metrics (AFM/TrueType) for exact text layout (enables managed raster text)
- ⬜ Mathtext subset; hatching; clipping; alpha compositing
- ⬜ PDF backend

## Phase 5 — Ecosystem parity
- ⬜ `3D` (mplot3d), `streamplot`, `quiver`, `hist2d`, `boxplot`, `violinplot`
- ⬜ Style sheets, `rcParams` file parsing
- ⬜ Animation API

Each item ships as a vertical slice per `Skills.md` Skill 8 (definition of done).
