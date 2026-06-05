# dotnet-matplotlib

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/DotnetMatplotlib.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/DotnetMatplotlib/)

A **native .NET 10** port of [Matplotlib](https://matplotlib.org/) — the de-facto
2D plotting library for Python — rebuilt in idiomatic **F#** following
**Object-Oriented** and **Domain-Driven Design** principles.

> Goal: faithful, 100% behavioral port of Matplotlib's plotting model
> (`Figure` / `Axes` / `Artist` / `Transform` / `Backend`) with a familiar
> `pyplot`-style facade, producing publication-quality output with **zero native
> dependencies** (pure-managed SVG backend; raster/Agg backend on the roadmap).

```fsharp
open Matplotlib

let plt = Pyplot()
plt.Plot([| 1.0; 2.0; 3.0; 4.0 |], [| 1.0; 4.0; 9.0; 16.0 |], color = "C0", label = "y = x^2")
|> ignore
plt.Title "Hello, dotnet-matplotlib"
plt.XLabel "x"
plt.YLabel "y"
plt.Legend()
plt.Savefig "hello.svg"
```

## Building

Requires the **.NET 10 SDK**.

```bash
dotnet tool restore           # FSharpLint + Fantomas (one-time)
dotnet build                  # whole solution (warnings are errors)
dotnet test                   # all tests
dotnet run --project samples/Gallery -- out   # render the sample gallery to ./out
```

Style is enforced with **Fantomas**, and **FSharpLint** is the configured lint
rule set — see [LINTING.md](LINTING.md).

## Architecture

`dotnet-matplotlib` mirrors Matplotlib's layered object model under a DDD project
structure. F# is functional-first but fully supports the OOP model Matplotlib
relies on (classes, interfaces, inheritance) — used for the artist hierarchy —
while value objects are immutable records and algorithms live in modules.

```
src/
  Matplotlib.Domain/    # Pure F# domain: Figure, Axes, Artist, Transform,
                        #   Scale, Ticker, Color — no I/O, no rendering deps.
                        #   Defines IRenderer (port) that artists draw onto.
  Matplotlib.Backends/  # Infrastructure: concrete IRenderer implementations
                        #   (SvgRenderer) + FigureCanvas (output adapters).
  Matplotlib/           # Application/facade: the stateful `Pyplot` API.
tests/
  Matplotlib.Tests/     # xUnit unit & golden-file tests.
samples/Gallery/        # Runnable example gallery.
```

See [CLAUDE.md](CLAUDE.md) for the full engineering guide and
[Skills.md](Skills.md) for the porting playbook.

## License

`dotnet-matplotlib` is released under the **BSD-3-Clause** license — see
[LICENSE](LICENSE).

## Citation

Hunter, J. D. (2007). Matplotlib: A 2D graphics environment. *Computing in
Science & Engineering*, 9(3), 90–95. https://doi.org/10.1109/MCSE.2007.55
