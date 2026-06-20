# dotnet-matplotlib

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/DotnetMatplotlib.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/DotnetMatplotlib/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A **native .NET 10** port of [Matplotlib](https://matplotlib.org/) — the de-facto
2D plotting library for Python — rebuilt in idiomatic **F#** following
**Object-Oriented** and **Domain-Driven Design** principles.

> A faithful port of Matplotlib's plotting model
> (`Figure` / `Axes` / `Artist` / `Transform` / `Backend`) with a familiar
> `plt`-style facade, producing publication-quality output with **zero native
> dependencies** — pure-managed SVG, PNG and PDF backends.

## Gallery

<table>
  <tr>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/imshow.png" alt="imshow + colorbar" width="420"><br><sub><code>imshow</code> + <code>colorbar</code> (viridis)</sub></td>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/contour.png" alt="contour" width="420"><br><sub><code>contour</code> (marching squares)</sub></td>
  </tr>
  <tr>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/annotate.png" alt="annotate" width="420"><br><sub><code>annotate</code> + <code>text</code></sub></td>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/collections.png" alt="collections" width="420"><br><sub>line <code>collections</code></sub></td>
  </tr>
  <tr>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/scatter_colormap.png" alt="scatter with colormap" width="420"><br><sub><code>scatter</code> (colormapped <code>c</code>, sized <code>s</code>)</sub></td>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/pie.png" alt="pie" width="420"><br><sub><code>pie</code> (<code>set_aspect('equal')</code>)</sub></td>
  </tr>
  <tr>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/hist.png" alt="hist" width="420"><br><sub><code>hist</code></sub></td>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/stackplot.png" alt="stackplot" width="420"><br><sub><code>stackplot</code></sub></td>
  </tr>
  <tr>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/table.png" alt="table" width="420"><br><sub><code>table</code></sub></td>
    <td align="center"><img src="https://raw.githubusercontent.com/gnrkr789/dotnet-matplotlib/main/docs/gallery/report.png" alt="multi-panel report" width="420"><br><sub>multi-panel <code>Report</code></sub></td>
  </tr>
</table>

## Install

```bash
dotnet add package DotnetMatplotlib
```

```fsharp
open Matplotlib

let plt = Plt()
plt.Plot([| 1.0; 2.0; 3.0; 4.0 |], [| 1.0; 4.0; 9.0; 16.0 |], color = "C0", label = "y = x^2")
|> ignore
plt.Title "Hello, dotnet-matplotlib"
plt.XLabel "x"
plt.YLabel "y"
plt.Legend()
plt.Savefig "hello.svg"
```

## Output formats

`Savefig` chooses the format from the file extension. SVG, **PNG** and **PDF** are
all pure-managed and cross-platform (zero native dependencies):

```fsharp
plt.Savefig "plot.svg"   // vector SVG
plt.Savefig "plot.png"   // raster PNG (software rasterizer, anti-aliased)
plt.Savefig "plot.pdf"   // vector PDF
```

Text uses TrueType fonts discovered from the system. To select a font, set the
default family before plotting — the equivalent of Matplotlib's
`rcParams["font.family"]`:

```fsharp
plt.FontFamily <- "serif"
```

Animations are written as looping GIFs:

```fsharp
// factory builds the Figure for each frame index
plt.SaveGif("wave.gif", 30, (fun i -> buildFrame i), fps = 20)
```

## Interactive window

Figures can also be shown in a live window — the equivalent of Matplotlib's
`plt.show()`. This is an **opt-in, Windows-only** backend (WinForms + GDI+) that
lives in `Matplotlib.Gui`, so the default path stays free of any UI dependency:

```fsharp
open Matplotlib
open Matplotlib.Gui   // adds plt.Show()

let plt = Plt()
plt.Plot([| 0.0; 1.0; 2.0; 3.0 |], [| 0.0; 1.0; 4.0; 9.0 |], color = "C0") |> ignore
plt.Title "Hello, window"
plt.Show()            // opens a window and blocks until it is closed; resizes re-layout
```

## Notebooks

In a .NET Interactive / Polyglot / Jupyter notebook, figures render inline as SVG:

```fsharp
#r "nuget: DotnetMatplotlib.Interactive"
open Matplotlib
Matplotlib.Interactive.Notebook.register ()   // once per session

let plt = Plt()
plt.Plot([| 0.0; 1.0; 2.0 |], [| 0.0; 1.0; 4.0 |], color = "C0") |> ignore
plt   // the cell value renders as an inline SVG plot
```

## Browser (Blazor WebAssembly)

Because the library is pure-managed, it runs in the browser via .NET WebAssembly
— no JavaScript charting library and no native dependencies. The standalone
[Blazor WASM demo](samples/BlazorDemo) renders plots as inline SVG client-side:

```bash
dotnet run --project samples/BlazorDemo
```

## DataFrames

Plot directly from a `Microsoft.Data.Analysis.DataFrame` (pandas `.plot()` style):

```bash
dotnet add package DotnetMatplotlib.DataFrame
```

```fsharp
open Matplotlib.DataFrame   // adds df.PlotLine / PlotScatter / PlotBar / PlotHist

df.PlotLine("x", "y").Savefig "line.png"
df.PlotHist("value", 20).Savefig "hist.svg"
```

Each method plots the named column(s) and returns the `Plt`, so you can add a
title or choose the output format. The same extension methods work from C#.

## Reports (server-side, deterministic)

`DotnetMatplotlib.Reports` composes multi-panel reports — a grid of titled charts
under a report title — and renders them to SVG / PNG / PDF. It is built for
server-side and regulated / cloud-native use:

- **Pure-managed, zero native dependencies** — it builds and runs on Linux (the
  CI builds and tests it there) and needs no UI or native graphics components.
  SVG and PDF output need no system fonts.
- **Deterministic output** — the same input renders **byte-for-byte identical**
  vector output, so a report can be checksummed (`report.Sha256()`) for audit
  trails, change detection and compliance.

```bash
dotnet add package DotnetMatplotlib.Reports
```

```fsharp
open Matplotlib.Reports

let report =
    Report("Q4 2026 Performance")
        .AddLine("Revenue ($K)", months, revenue)
        .AddBar("Units by region", regions, units)
        .AddScatter("Price vs demand", price, demand)

report.Save "q4.pdf"               // deterministic PDF (also .png / .svg)
let fingerprint = report.Sha256()  // stable SHA-256 for audit
```

## AI agents (MCP)

`DotnetMatplotlib.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io)
server that lets AI agents create plots with this library. Install it as a .NET
tool and point your MCP client at the `matplotlib-mcp` command:

```bash
dotnet tool install -g DotnetMatplotlib.Mcp
```

```json
{
  "mcpServers": {
    "matplotlib": { "command": "matplotlib-mcp" }
  }
}
```

It exposes `plot_line`, `scatter`, `bar` and `heatmap` tools that render to a
PNG / SVG / PDF file (chosen by the output extension) and return the saved path.

## Features

- Plots: `plot`, `scatter` (colormapped `c`, per-point `s`), `bar`/`barh`, `hist`, `pie`, `stackplot`, `fill_between`/`fill_betweenx`, `step`, `stem`, `errorbar` (with `capsize`), `vlines`/`hlines`
- Axis control: `set_aspect('equal')`, `axis('off')`
- Statistics & fields: `hist2d`, `boxplot`, `violinplot`, `quiver`, `streamplot`
- Images: `imshow`, `pcolormesh` with colormaps (`viridis`, `gray`, `jet`, `hot`) and `colorbar`
- Contours: `contour` (marching squares), `contourf`
- Patches & line/poly collections, hatching, data `table`s, the full marker set
- Legends (including automatic `best` placement), text & annotations
- Subplots with `tight_layout` / `constrained_layout`
- Scales: linear / log / symlog / logit; categorical & date axes
- 3D: `plot3D`, `scatter3D`, `plot_wireframe`
- Style sheets and `rcParams` parsing (`ggplot`, `dark_background`, …)
- Backends: SVG, PNG and PDF (pure-managed), an interactive window (Windows), and animated GIF
- Integrations: a deterministic server-side **reporting engine**, `Microsoft.Data.Analysis` DataFrames, .NET Interactive / Jupyter notebooks, an MCP server for AI agents, and Blazor WebAssembly (runs in the browser)

See [PORTING.md](PORTING.md) for the parity log.

## Roadmap

A few areas are still being refined toward fuller Matplotlib parity (tracked in
[PORTING.md](PORTING.md) under *Known deviations / TODO*, and marked with `TODO`
comments in the source):

- **`ScalarFormatter` scientific notation / shared offset** — very large or small
  tick values currently render in full; the common `×10ⁿ` offset label is planned.
- **Real glyph-advance text metrics** — text sizing in layout (`tight_layout`,
  label / legend placement) uses a fixed-width approximation; switching to measured
  TrueType advances is in progress.
- **Non-zero winding raster fill** — the software rasterizer fills with the even-odd
  rule, whereas Matplotlib/Agg use non-zero winding (affects self-intersecting and
  holey fills).

Contributions welcome.

## Building

Requires the **.NET 10 SDK**.

```bash
dotnet build                  # whole solution (warnings are errors)
dotnet test                   # all tests
dotnet run --project samples/Gallery -- out   # render the sample gallery to ./out
```

## License

`dotnet-matplotlib` is released under the **MIT** license — see [LICENSE](LICENSE).

## Citation

Hunter, J. D. (2007). Matplotlib: A 2D graphics environment. *Computing in
Science & Engineering*, 9(3), 90–95. https://doi.org/10.1109/MCSE.2007.55
