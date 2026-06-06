# Security Policy

## Supported Versions

`dotnet-matplotlib` is pre-1.0. Only the latest published `0.0.x` release
receives security updates.

| Version          | Supported          |
| ---------------- | ------------------ |
| 0.0.x (latest)   | :white_check_mark: |
| older 0.0.x      | :x:                |

## Reporting a Vulnerability

Please report security issues **privately** — do not open a public issue or pull
request for an undisclosed vulnerability.

- Preferred: open a private report through GitHub Security Advisories
  ("**Security**" tab → "**Report a vulnerability**"), or directly at
  https://github.com/gnrkr789/dotnet-matplotlib/security/advisories/new

When reporting, please include the affected version, a description of the issue,
and a minimal way to reproduce it (input data, code, or a sample file).

**What to expect**

- An acknowledgement, typically within a few days.
- If accepted: a fix is prepared and shipped in a new `0.0.x` release, and a
  security advisory is published crediting the reporter (unless anonymity is
  requested).
- If declined: an explanation of why it is considered out of scope.

## Scope notes

`dotnet-matplotlib` is a pure-managed library with zero native dependencies, so
the most relevant concerns are:

- parsing untrusted input — notably **TrueType font files** loaded for text
  rendering, and 2D arrays passed to the plotting API;
- resource exhaustion when rendering very large figures or animations.

It does not perform network access or execute external processes.
