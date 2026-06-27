# dotnet-matplotlib

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/DotnetMatplotlib.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/DotnetMatplotlib/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/gnrkr789/dotnet-matplotlib/blob/main/LICENSE)

A **native .NET 10** port of [Matplotlib](https://matplotlib.org/) — the de-facto
2D plotting library for Python — rebuilt in idiomatic **F#** following
**Object-Oriented** and **Domain-Driven Design** principles.

> A faithful port of Matplotlib's plotting model
> (`Figure` / `Axes` / `Artist` / `Transform` / `Backend`) with a familiar
> `plt`-style facade, producing publication-quality output with **zero native
> dependencies** — pure-managed SVG, PNG and PDF backends.

A screenshot gallery is available on the
[project page](https://github.com/gnrkr789/dotnet-matplotlib).

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

## Notebooks

`DotnetMatplotlib.Interactive` renders figures inline as SVG in **Verso** and **.NET
Interactive** notebooks. In [Verso](https://versonotebooks.com/) (the actively-developed
successor to .NET Interactive) the formatter is discovered automatically — just add the
package as an extension. For legacy .NET Interactive / Polyglot, add
`#r "nuget: DotnetMatplotlib.Interactive"` and call
`Matplotlib.Interactive.Notebook.register ()` once.

## DataFrames

`DotnetMatplotlib.DataFrame` adds pandas-style plotting extensions to
`Microsoft.Data.Analysis.DataFrame`: `df.PlotLine("x","y")`, `PlotScatter`,
`PlotBar`, `PlotHist` — each returns a `Plt`.

## Reports (server-side, deterministic)

The core package includes a `Report` type (`open Matplotlib.Reports`) that composes
multi-panel SVG/PNG/PDF reports for server-side use: pure-managed with zero native
dependencies (builds and tests on Linux; SVG and PDF need no system fonts) and
**byte-reproducible** output that can be checksummed (`report.Sha256()`) for audit and
compliance.

## AI agents (MCP)

`DotnetMatplotlib.Mcp` is a Model Context Protocol server (install as a .NET tool:
`dotnet tool install -g DotnetMatplotlib.Mcp`, command `matplotlib-mcp`) that lets
AI agents create line / scatter / bar / heatmap plots saved as PNG / SVG / PDF.

## Features

- Plots: `plot`, `scatter` (colormapped `c`, per-point `s`), `bar`/`barh`, `hist`, `pie`, `stackplot`, `fill_between`/`fill_betweenx`, `step`, `stem`, `errorbar` (with `capsize`), `vlines`/`hlines`
- Statistics & fields: `hist2d`, `boxplot`, `violinplot`, `quiver`, `streamplot`
- Axis control: `set_aspect('equal')`, `axis('off')`
- Images: `imshow`, `pcolormesh` with colormaps (`viridis`, `gray`, `jet`, `hot`) and `colorbar`
- Contours: `contour` (marching squares), `contourf`
- Patches & line/poly collections, hatching, the full marker set
- Legends (including automatic `best` placement), text & annotations
- Subplots with `tight_layout` / `constrained_layout`
- Scales: linear / log / symlog / logit; categorical & date axes
- 3D: `plot3D`, `scatter3D`, `plot_wireframe`
- Style sheets and `rcParams` parsing (`ggplot`, `dark_background`, …)
- Backends: SVG, PNG and PDF (pure-managed), an interactive window (Windows), and animated GIF

## License

`dotnet-matplotlib` is released under the **MIT** license.

## Citation

Hunter, J. D. (2007). Matplotlib: A 2D graphics environment. *Computing in
Science & Engineering*, 9(3), 90–95. https://doi.org/10.1109/MCSE.2007.55
