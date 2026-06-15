namespace Matplotlib.Tests

open Xunit
open Matplotlib.Domain.Primitives

module ColorTests =

    [<Fact>]
    let ``Hex parses to bytes`` () =
        let c = Color.fromHex "#1f77b4"
        Assert.Equal(31, c.R255)
        Assert.Equal(119, c.G255)
        Assert.Equal(180, c.B255)

    [<Fact>]
    let ``Short hex expands each nibble`` () =
        let c = Color.fromHex "#abc"
        Assert.Equal(0xaa, c.R255)
        Assert.Equal(0xbb, c.G255)
        Assert.Equal(0xcc, c.B255)

    [<Fact>]
    let ``Round trip to hex`` () = Assert.Equal("#1f77b4", (Color.fromHex "#1F77B4").ToHex())

    [<Fact>]
    let ``Resolver maps base color`` () =
        let c = ColorResolver.Default.Resolve "r"
        assertClose 1.0 c.R
        assertClose 0.0 c.G
        assertClose 0.0 c.B

    [<Fact>]
    let ``Resolver maps property cycle reference C0 to tab10`` () =
        let c = ColorResolver.Default.Resolve "C0"
        Assert.Equal("#1f77b4", c.ToHex())

    [<Fact>]
    let ``Resolver treats lowercase c as cyan, not a cycle reference`` () =
        // 'c' is the cyan base color; 'c0' is not a valid matplotlib spec.
        let cyan = ColorResolver.Default.Resolve "c"
        assertClose 0.0 cyan.R
        assertClose 0.75 cyan.G
        assertClose 0.75 cyan.B
        let ok, _ = ColorResolver.Default.TryResolve "c0"
        Assert.False ok

    [<Fact>]
    let ``Resolver maps tableau and css4 names`` () =
        Assert.Equal("#1f77b4", (ColorResolver.Default.Resolve "tab:blue").ToHex())
        Assert.Equal("#008000", (ColorResolver.Default.Resolve "green").ToHex())

    [<Fact>]
    let ``Resolver maps grayscale float string`` () =
        let c = ColorResolver.Default.Resolve "0.5"
        assertClose 0.5 c.R
        assertClose 0.5 c.G
        assertClose 0.5 c.B

    [<Fact>]
    let ``Resolver maps none to transparent`` () = Assert.True((ColorResolver.Default.Resolve "none").IsTransparent)

    [<Fact>]
    let ``Css4 table has 148 entries`` () = Assert.Equal(148, ColorData.css4Colors.Count)
