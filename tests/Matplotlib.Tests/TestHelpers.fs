namespace Matplotlib.Tests

open Xunit

/// <summary>Shared assertions for the test suite.</summary>
[<AutoOpen>]
module TestHelpers =

    /// <summary>Assert two floats are equal within an absolute tolerance.</summary>
    let assertClose (expected: float) (actual: float) =
        Assert.True(abs (expected - actual) < 1e-9, $"expected {expected}, got {actual}")

    /// <summary>Assert two floats are equal within a caller-supplied tolerance.</summary>
    let assertCloseTol (tol: float) (expected: float) (actual: float) =
        Assert.True(abs (expected - actual) < tol, $"expected {expected}, got {actual} (tol {tol})")
