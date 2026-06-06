# Contributing to dotnet-matplotlib

Thanks for your interest in improving **dotnet-matplotlib**! Contributions of all
kinds are welcome — bug reports, feature requests, documentation, and code.

## Getting started

You need the **.NET 10 SDK**.

```bash
dotnet tool restore     # one-time: restores Fantomas (and the lint tooling)
dotnet build            # builds the whole solution (warnings are errors)
dotnet test             # runs the test suite
```

Render the sample gallery to `./out` to eyeball changes:

```bash
dotnet run --project samples/Gallery -- out
```

## Project layout

The code follows a Domain-Driven Design layering, with dependencies pointing
strictly inward:

- `src/Matplotlib.Domain` — the pure plotting model (no rendering, files or
  platform dependencies): primitives, transforms, scales, ticking, artists,
  `Figure` / `Axes` / `Axes3D`, and the `IRenderer` port.
- `src/Matplotlib.Backends` — concrete `IRenderer` implementations (SVG, the
  pure-managed PNG/raster backend, PDF), font loading and the GIF/animation writer.
- `src/Matplotlib` — the stateful `Pyplot` facade.
- `src/Matplotlib.Gui` — an opt-in, Windows-only interactive window + GDI backend.
- `tests/Matplotlib.Tests` — xUnit tests.

## Before opening a pull request

1. **Format**: `dotnet fantomas src tests samples` (CI enforces `--check`).
2. **Build**: `dotnet build` must be clean — warnings are treated as errors.
3. **Test**: `dotnet test` must be green. Add tests for new behavior — unit tests
   for domain math, and render/serialization checks for backend changes.
4. Keep changes focused and the public API close to Matplotlib's where it maps
   cleanly. New domain files must be added to the `.fsproj` in dependency order
   (F# compiles files top to bottom).

## Commit messages

Write clear, imperative commit messages (e.g. "Add log-scale minor ticks").
Group related changes into a single commit where it makes sense.

## License

By contributing, you agree that your contributions are licensed under the
project's [MIT License](LICENSE).
