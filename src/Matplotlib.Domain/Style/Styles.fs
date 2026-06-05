namespace Matplotlib.Domain.Style

open Matplotlib.Domain.Primitives

/// <summary>Font weight (subset of Matplotlib's font weights).</summary>
type FontWeight =
    | Normal
    | Bold

/// <summary>Font slant (Matplotlib's font <c>style</c>).</summary>
type FontSlant =
    | Upright
    | Italic

/// <summary>
/// Properties controlling text rendering.
/// </summary>
/// <remarks>Ported from <c>matplotlib.font_manager.FontProperties</c> (subset).</remarks>
type FontProperties =
    {
        Family: string
        Size: float
        Weight: FontWeight
        Slant: FontSlant
    }

    /// <summary>The default font (sans-serif, 10pt, normal).</summary>
    static member Default =
        {
            Family = "sans-serif"
            Size = 10.0
            Weight = Normal
            Slant = Upright
        }

/// <summary>
/// Line styles and their on/off dash patterns in points (unscaled, as in
/// <c>rcParams['lines.*_pattern']</c>).
/// </summary>
/// <remarks>Ported from <c>matplotlib.lines</c> line-style handling.</remarks>
type LineStyle =
    | Solid
    | Dashed
    | DashDot
    | Dotted
    | NoLine

    /// <summary>The unscaled dash pattern in points, or None for solid/no line.</summary>
    member this.DashPattern: float list option =
        match this with
        | Solid -> None
        | Dashed -> Some [ 3.7; 1.6 ]
        | DashDot -> Some [ 6.4; 1.6; 1.0; 1.6 ]
        | Dotted -> Some [ 1.0; 1.65 ]
        | NoLine -> None

/// <summary>Marker shapes (subset of Matplotlib's marker set).</summary>
/// <remarks>Ported from <c>matplotlib.markers</c>.</remarks>
type MarkerStyle =
    | NoMarker
    | Circle
    | Point
    | Square
    | Diamond
    | TriangleUp
    | Plus
    | Cross

/// <summary>Where the step occurs relative to the data points in a step plot.</summary>
/// <remarks>Ported from Matplotlib's <c>step</c> <c>where</c> argument / drawstyle.</remarks>
type StepWhere =
    | Pre
    | Post
    | Mid

/// <summary>Parsers from Matplotlib's short string codes to style values.</summary>
[<RequireQualifiedAccess>]
module Styles =

    /// <summary>Parse a Matplotlib line-style code (<c>-</c>, <c>--</c>, <c>-.</c>, <c>:</c>).</summary>
    let parseLineStyle (code: string) : LineStyle =
        match code with
        | "-"
        | "solid" -> Solid
        | "--"
        | "dashed" -> Dashed
        | "-."
        | "dashdot" -> DashDot
        | ":"
        | "dotted" -> Dotted
        | ""
        | "None"
        | "none" -> NoLine
        | other -> failwith $"Unknown line style '{other}'."

    /// <summary>Parse a Matplotlib marker code (<c>o . s D ^ + x</c>).</summary>
    let parseMarker (code: string) : MarkerStyle =
        match code with
        | ""
        | "None"
        | "none" -> NoMarker
        | "o" -> Circle
        | "." -> Point
        | "s" -> Square
        | "D" -> Diamond
        | "^" -> TriangleUp
        | "+" -> Plus
        | "x" -> Cross
        | other -> failwith $"Unknown marker '{other}'."

    /// <summary>Parse a Matplotlib step <c>where</c> code (<c>pre</c>, <c>post</c>, <c>mid</c>).</summary>
    let parseStepWhere (code: string) : StepWhere =
        match code with
        | "pre" -> Pre
        | "post" -> Post
        | "mid" -> Mid
        | other -> failwith $"Unknown step where '{other}'."

/// <summary>
/// Cycles through the property-cycle colors (Matplotlib's <c>axes.prop_cycle</c>).
/// </summary>
/// <remarks>Ported from <c>matplotlib.rcsetup.cycler</c> default (<c>tab10</c>).</remarks>
type PropertyCycler(colors: Color list) =

    let arr = colors |> List.toArray
    let mutable index = 0

    /// <summary>The colors in cycle order.</summary>
    member _.Colors = arr

    /// <summary>Return the next color, advancing the cycle.</summary>
    member _.Next() : Color =
        let c = arr[index % arr.Length]
        index <- index + 1
        c

    /// <summary>Reset the cycle back to its first color.</summary>
    member _.Reset() = index <- 0

    /// <summary>A cycler over the default tab10 palette.</summary>
    static member CreateDefault() = PropertyCycler(ColorData.tab10 |> List.map Color.fromHex)

/// <summary>
/// The subset of Matplotlib's <c>rcParams</c> used by the current renderer,
/// with values taken from the default <c>matplotlibrc</c>.
/// </summary>
type RcParams =
    {
        FigureSizeInches: Size
        FigureDpi: float
        FigureFaceColor: Color
        AxesFaceColor: Color
        AxesEdgeColor: Color
        AxesLineWidth: float
        LinesLineWidth: float
        FontSize: float
        TickMajorSize: float
        TickMajorWidth: float
        TickLabelSize: float
        TickPad: float
        AxesTitleSize: float
        AxesTitlePad: float
        AxesLabelSize: float
        AxesLabelPad: float
        AxesLabelColor: Color
        TickColor: Color
        TextColor: Color
        GridColor: Color
        GridLineWidth: float
        SubplotLeft: float
        SubplotRight: float
        SubplotBottom: float
        SubplotTop: float
    }

    /// <summary>The Matplotlib default parameters (from <c>mpl-data/matplotlibrc</c>).</summary>
    static member Default =
        {
            FigureSizeInches = { Width = 6.4; Height = 4.8 }
            FigureDpi = 100.0
            FigureFaceColor = Color.white
            AxesFaceColor = Color.white
            AxesEdgeColor = Color.black
            AxesLineWidth = 0.8
            LinesLineWidth = 1.5
            FontSize = 10.0
            TickMajorSize = 3.5
            TickMajorWidth = 0.8
            TickLabelSize = 10.0
            TickPad = 3.5
            AxesTitleSize = 12.0
            AxesTitlePad = 6.0
            AxesLabelSize = 10.0
            AxesLabelPad = 4.0
            AxesLabelColor = Color.black
            TickColor = Color.black
            TextColor = Color.black
            GridColor = Color.fromHex "#b0b0b0"
            GridLineWidth = 0.8
            SubplotLeft = 0.125
            SubplotRight = 0.9
            SubplotBottom = 0.11
            SubplotTop = 0.88
        }
