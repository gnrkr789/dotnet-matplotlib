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

## Phase 3 — Scales, color, data domains
- ⬜ `LogScale`, `SymlogScale`, `LogitScale`; log locators/formatters
- ⬜ Date axis (`dates.py`) + locators/formatters
- ⬜ Categorical axis (`category.py`)
- ⬜ Colormaps (`_cm*.py`: viridis, plasma, …), `Normalize`, `colorbar`
- ⬜ `imshow`, `pcolormesh`, `contour`/`contourf`

## Phase 4 — Backends & fidelity
- ⬜ Raster backend (Agg-equivalent, pure-managed or opt-in native) → PNG
- ⬜ Font metrics (AFM/TrueType) for exact text layout
- ⬜ Mathtext subset; hatching; clipping; alpha compositing
- ⬜ PDF backend

## Phase 5 — Ecosystem parity
- ⬜ `3D` (mplot3d), `streamplot`, `quiver`, `hist2d`, `boxplot`, `violinplot`
- ⬜ Style sheets, `rcParams` file parsing
- ⬜ Animation API

Each item ships as a vertical slice per `Skills.md` Skill 8 (definition of done).
