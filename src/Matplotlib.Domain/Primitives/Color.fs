namespace Matplotlib.Domain.Primitives

open System

/// <summary>
/// An RGBA color with components in the range <c>[0, 1]</c>, matching
/// Matplotlib's float-RGBA convention.
/// </summary>
/// <remarks>Ported from the RGBA tuples used throughout <c>matplotlib.colors</c>.</remarks>
type Color =
    {
        R: float
        G: float
        B: float
        A: float
    }

    /// <summary>True when the color is fully transparent.</summary>
    member this.IsTransparent = this.A <= 0.0

    member private this.ToByte(v: float) =
        int (Math.Round(Math.Clamp(v, 0.0, 1.0) * 255.0, MidpointRounding.AwayFromZero))

    member this.R255 = this.ToByte this.R

    member this.G255 = this.ToByte this.G

    member this.B255 = this.ToByte this.B

    member this.A255 = this.ToByte this.A

    /// <summary>Return the same color with a different alpha.</summary>
    member this.WithAlpha(alpha: float) = { this with A = alpha }

    /// <summary>Lowercase <c>#rrggbb</c> string (no alpha).</summary>
    member this.ToHex() = String.Format("#{0:x2}{1:x2}{2:x2}", this.R255, this.G255, this.B255)

    /// <summary>Lowercase <c>#rrggbbaa</c> string including alpha.</summary>
    member this.ToHexRgba() = String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", this.R255, this.G255, this.B255, this.A255)

    override this.ToString() = if this.A >= 1.0 then this.ToHex() else this.ToHexRgba()

/// <summary>Constructors and parsers for <see cref="Color"/>.</summary>
[<RequireQualifiedAccess>]
module Color =

    /// <summary>Opaque black.</summary>
    let black: Color = { R = 0.0; G = 0.0; B = 0.0; A = 1.0 }

    /// <summary>Opaque white.</summary>
    let white: Color = { R = 1.0; G = 1.0; B = 1.0; A = 1.0 }

    /// <summary>Fully transparent (Matplotlib's <c>'none'</c>).</summary>
    let none: Color = { R = 0.0; G = 0.0; B = 0.0; A = 0.0 }

    /// <summary>Create an opaque color from RGB in <c>[0, 1]</c>.</summary>
    let rgb (r: float) (g: float) (b: float) : Color = { R = r; G = g; B = b; A = 1.0 }

    /// <summary>Create a color from RGBA in <c>[0, 1]</c>.</summary>
    let rgba (r: float) (g: float) (b: float) (a: float) : Color = { R = r; G = g; B = b; A = a }

    /// <summary>Build a color from 8-bit integer channels.</summary>
    let fromBytes (r: int) (g: int) (b: int) (a: int) : Color =
        {
            R = float r / 255.0
            G = float g / 255.0
            B = float b / 255.0
            A = float a / 255.0
        }

    let private hexDigit (c: char) =
        match c with
        | c when c >= '0' && c <= '9' -> int c - int '0'
        | c when c >= 'a' && c <= 'f' -> int c - int 'a' + 10
        | c when c >= 'A' && c <= 'F' -> int c - int 'A' + 10
        | _ -> raise (FormatException $"Invalid hex digit '{c}'.")

    let private hx (hi: char) (lo: char) = (hexDigit hi <<< 4) ||| hexDigit lo

    /// <summary>Parse a <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c> or <c>#rrggbbaa</c> string.</summary>
    let fromHex (hex: string) : Color =
        if isNull hex then
            nullArg (nameof hex)

        let h = if hex.StartsWith '#' then hex.Substring 1 else hex

        match h.Length with
        | 3 -> fromBytes (hx h[0] h[0]) (hx h[1] h[1]) (hx h[2] h[2]) 255
        | 4 -> fromBytes (hx h[0] h[0]) (hx h[1] h[1]) (hx h[2] h[2]) (hx h[3] h[3])
        | 6 -> fromBytes (hx h[0] h[1]) (hx h[2] h[3]) (hx h[4] h[5]) 255
        | 8 -> fromBytes (hx h[0] h[1]) (hx h[2] h[3]) (hx h[4] h[5]) (hx h[6] h[7])
        | _ -> raise (FormatException $"'{hex}' is not a valid hex color.")
