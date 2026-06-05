# Linting & formatting

This repository is written in **F#** and enforces two style/quality gates,
configured as repo tools in [`.config/dotnet-tools.json`](.config/dotnet-tools.json):

| Tool | Purpose | Config | Status here |
|------|---------|--------|-------------|
| **FSharpLint** | Lint rules (naming, conventions, hints, complexity) | [`fsharplint.json`](fsharplint.json) | configured; runner blocked on .NET 10 (see below) |
| **Fantomas** | Canonical F# auto-formatter | [`.editorconfig`](.editorconfig) | ✅ enforced (`--check` is green) |

Restore the tools once:

```bash
dotnet tool restore
```

## Formatting (Fantomas) — enforced

```bash
dotnet fantomas src tests samples           # format in place
dotnet fantomas --check src tests samples   # CI gate: non-zero if any file is unformatted
```

The whole tree currently passes `fantomas --check`.

## Linting (FSharpLint)

FSharpLint is the project's required lint rule set. The configuration in
`fsharplint.json` enables naming rules (PascalCase types/members/modules,
`I`-prefixed interfaces, camelCase parameters), line-length and size limits,
and a set of refactoring hints.

```bash
dotnet dotnet-fsharplint lint --lint-config fsharplint.json --file-type project <project.fsproj>
```

### Known limitation on the .NET 10 SDK

FSharpLint `0.24.2` (latest) bundles an `Ionide.ProjInfo` / MSBuild that cannot
yet *crack* projects under the .NET 8/10 SDKs in every environment — it fails with:

```
Could not load file or assembly 'System.Runtime, Version=10.0.0.0 …'
   at Ionide.ProjInfo.ProjectLoader.loadProject(...)
```

This is an upstream tool-vs-SDK gap, not an issue with this codebase. Until a
.NET 10-compatible FSharpLint ships, lint it from a machine/SDK combination
where project cracking works, or via the **net8.0 lint shim**:

```bash
# Pinned to the .NET 8 SDK via build/lint/global.json; links every library .fs file.
cd build/lint
DOTNET_ROLL_FORWARD=LatestMajor dotnet dotnet-fsharplint lint \
    --lint-config ../../fsharplint.json --file-type project LintShim.fsproj
```

The F# sources in this repo are written to comply with `fsharplint.json`
regardless. See [CLAUDE.md](CLAUDE.md) §Definition of done.
