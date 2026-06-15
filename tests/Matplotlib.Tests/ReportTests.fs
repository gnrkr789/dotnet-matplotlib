namespace Matplotlib.Tests

open Xunit
open Matplotlib.Reports

module ReportTests =

    let private sample () =
        Report("Q4 2026 Performance")
            .AddLine("Revenue ($K)", [| 1.0; 2.0; 3.0; 4.0 |], [| 10.0; 14.0; 13.0; 18.0 |])
            .AddBar("Units by region", [| "NA"; "EU"; "APAC" |], [| 120.0; 90.0; 150.0 |])
            .AddScatter("Price vs demand", [| 1.0; 2.0; 3.0; 4.0; 5.0 |], [| 5.0; 4.0; 4.5; 3.0; 3.2 |])

    [<Fact>]
    let ``report renders SVG with panels and the report title`` () =
        let svg = sample().RenderSvg()
        Assert.Contains("<svg", svg)
        Assert.Contains("Revenue", svg)
        Assert.Contains("Q4 2026 Performance", svg)

    [<Fact>]
    let ``pdf rendering is deterministic (byte-identical across renders)`` () =
        let a = sample().RenderPdf()
        let b = sample().RenderPdf()
        Assert.True(a.Length > 1000)
        Assert.Equal<byte[]>(a, b)

    [<Fact>]
    let ``sha256 is a stable 64-char hex fingerprint`` () =
        let h1 = sample().Sha256()
        let h2 = sample().Sha256()
        Assert.Equal(64, h1.Length)
        Assert.Equal(h1, h2)
