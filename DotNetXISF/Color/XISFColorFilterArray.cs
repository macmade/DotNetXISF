/*******************************************************************************
 * The MIT License (MIT)
 *
 * Copyright (c) 2026, Jean-David Gadina - www.xs-labs.com
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the Software), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetXISF;

/// <summary>
/// A parsed XISF <c>&lt;ColorFilterArray&gt;</c> element: the color filter array (CFA),
/// such as a Bayer filter, of a mosaiced two-dimensional image.
/// </summary>
/// <remarks>
/// The CFA is described by a <see cref="Pattern"/> string whose characters, read from top
/// to bottom and left to right, give the color of each element of a
/// <see cref="Width"/> × <see cref="Height"/> matrix.
/// </remarks>
public readonly struct XISFColorFilterArray : IEquatable< XISFColorFilterArray >
{
    /// <summary>A single element of a color filter array pattern.</summary>
    public enum Element
    {
        /// <summary>A nonexistent or undefined CFA element (<c>0</c>).</summary>
        None,

        /// <summary>A red filter element (<c>R</c>).</summary>
        Red,

        /// <summary>A green filter element (<c>G</c>).</summary>
        Green,

        /// <summary>A blue filter element (<c>B</c>).</summary>
        Blue,

        /// <summary>A white or panchromatic filter element (<c>W</c>).</summary>
        White,

        /// <summary>A cyan filter element (<c>C</c>).</summary>
        Cyan,

        /// <summary>A magenta filter element (<c>M</c>).</summary>
        Magenta,

        /// <summary>A yellow filter element (<c>Y</c>).</summary>
        Yellow,
    }

    /// <summary>The CFA pattern backing field (nullable so a default value is safe).</summary>
    private readonly string? pattern;

    /// <summary>The raw CFA pattern string, ordered top to bottom and left to right.</summary>
    public string Pattern => this.pattern ?? "";

    /// <summary>The width, in pixels, of the CFA matrix (greater than zero).</summary>
    public long Width { get; }

    /// <summary>The height, in pixels, of the CFA matrix (greater than zero).</summary>
    public long Height { get; }

    /// <summary>The optional, human-readable name identifying the CFA type or model.</summary>
    public string? Name { get; }

    /// <summary>The pattern decoded into typed elements.</summary>
    /// <remarks>
    /// A character that is not a valid <see cref="Element"/> is dropped; under strict parsing
    /// every character is validated at construction, so this then has exactly
    /// <see cref="Width"/> × <see cref="Height"/> entries. Each read returns a fresh list.
    /// </remarks>
    public IReadOnlyList< Element > Elements
    {
        get
        {
            List< Element > elements = new List< Element >( this.Pattern.Length );

            foreach( char character in this.Pattern )
            {
                if( XISFColorFilterArrayElementExtensions.FromCharacter( character ) is Element element )
                {
                    elements.Add( element );
                }
            }

            return elements;
        }
    }

    /// <summary>Parses a <c>&lt;ColorFilterArray&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;ColorFilterArray&gt;</c> element.</param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing the <c>pattern</c> characters must
    /// all be valid, the dimensions must be greater than zero, and the pattern length must
    /// equal <c>width × height</c>; <see cref="XISFParsingOptions.AllowSpecDeviations"/> skips
    /// those checks.
    /// </param>
    /// <exception cref="XISFException">
    /// A mandatory attribute is missing or invalid, or a strict validation check fails
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    internal XISFColorFilterArray( XISFElement element, XISFParsingOptions options )
    {
        IReadOnlyDictionary< string, string > attributes = element.Attributes;
        bool                                  lenient    = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );

        if( attributes.TryGetValue( "pattern", out string? pattern ) == false )
        {
            throw XISFException.InvalidElement( "ColorFilterArray is missing a 'pattern' attribute" );
        }

        long width  = Dimension( attributes, "width",  lenient );
        long height = Dimension( attributes, "height", lenient );

        if( lenient == false )
        {
            foreach( char character in pattern )
            {
                if( XISFColorFilterArrayElementExtensions.FromCharacter( character ) is null )
                {
                    throw XISFException.InvalidElement( $"ColorFilterArray pattern contains an invalid character: '{ character }'" );
                }
            }

            // The dimensions are both greater than zero here. Guarding each against the
            // pattern length before multiplying keeps the product from overflowing under the
            // project's checked arithmetic for a hostile file: when both fit, the product
            // cannot exceed the pattern length squared, which is within a long.
            long product = width <= pattern.Length && height <= pattern.Length ? width * height : long.MaxValue;

            if( product != pattern.Length )
            {
                throw XISFException.InvalidElement( $"ColorFilterArray pattern length ({ pattern.Length.ToString( CultureInfo.InvariantCulture ) }) does not match width ({ width.ToString( CultureInfo.InvariantCulture ) }) × height ({ height.ToString( CultureInfo.InvariantCulture ) })" );
            }
        }

        this.pattern = pattern;
        this.Width   = width;
        this.Height  = height;
        this.Name    = attributes.GetValueOrDefault( "name" );
    }

    /// <summary>A single-line, human-readable summary of the color filter array.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFColorFilterArray {{ pattern: { this.Pattern }, width: { this.Width.ToString( CultureInfo.InvariantCulture ) }, height: { this.Height.ToString( CultureInfo.InvariantCulture ) }, name: { this.Name ?? "<nil>" } }}";
    }

    /// <summary>Returns whether this color filter array equals another.</summary>
    /// <param name="other">The color filter array to compare against.</param>
    /// <returns><c>true</c> if the pattern, dimensions and name are all equal.</returns>
    public bool Equals( XISFColorFilterArray other )
    {
        return string.Equals( this.Pattern, other.Pattern, StringComparison.Ordinal )
            && this.Width == other.Width
            && this.Height == other.Height
            && string.Equals( this.Name, other.Name, StringComparison.Ordinal );
    }

    /// <summary>Returns whether this color filter array equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal color filter array.</returns>
    public override bool Equals( object? obj ) => obj is XISFColorFilterArray other && this.Equals( other );

    /// <summary>A hash code combining the pattern, dimensions and name.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine( this.Pattern, this.Width, this.Height, this.Name );

    /// <summary>Returns whether two color filter arrays are equal.</summary>
    /// <param name="left">The first color filter array.</param>
    /// <param name="right">The second color filter array.</param>
    /// <returns><c>true</c> if the color filter arrays are equal.</returns>
    public static bool operator ==( XISFColorFilterArray left, XISFColorFilterArray right ) => left.Equals( right );

    /// <summary>Returns whether two color filter arrays are unequal.</summary>
    /// <param name="left">The first color filter array.</param>
    /// <param name="right">The second color filter array.</param>
    /// <returns><c>true</c> if the color filter arrays are unequal.</returns>
    public static bool operator !=( XISFColorFilterArray left, XISFColorFilterArray right ) => left.Equals( right ) == false;

    /// <summary>Parses a mandatory CFA dimension attribute.</summary>
    /// <remarks>Parsed as a 64-bit integer to match the format's dimension width.</remarks>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="name">The attribute name (<c>width</c> or <c>height</c>).</param>
    /// <param name="lenient">Whether the greater-than-zero constraint is relaxed.</param>
    /// <returns>The parsed dimension.</returns>
    /// <exception cref="XISFException">
    /// The attribute is missing, not an integer, or (when not lenient) not greater than zero
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static long Dimension( IReadOnlyDictionary< string, string > attributes, string name, bool lenient )
    {
        if( attributes.TryGetValue( name, out string? raw ) == false || long.TryParse( raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value ) == false )
        {
            throw XISFException.InvalidElement( $"ColorFilterArray has a missing or invalid '{ name }' attribute: '{ attributes.GetValueOrDefault( name ) ?? "" }'" );
        }

        if( value <= 0 && lenient == false )
        {
            throw XISFException.InvalidElement( $"ColorFilterArray '{ name }' must be greater than zero, found { value.ToString( CultureInfo.InvariantCulture ) }" );
        }

        return value;
    }
}

/// <summary>Character parsing and formatting for <see cref="XISFColorFilterArray.Element"/>.</summary>
public static class XISFColorFilterArrayElementExtensions
{
    /// <summary>Parses a CFA pattern character into its typed element.</summary>
    /// <param name="character">The pattern character.</param>
    /// <returns>The matching element, or <c>null</c> when the character is not a known element.</returns>
    public static XISFColorFilterArray.Element? FromCharacter( char character )
    {
        return character switch
        {
            '0' => XISFColorFilterArray.Element.None,
            'R' => XISFColorFilterArray.Element.Red,
            'G' => XISFColorFilterArray.Element.Green,
            'B' => XISFColorFilterArray.Element.Blue,
            'W' => XISFColorFilterArray.Element.White,
            'C' => XISFColorFilterArray.Element.Cyan,
            'M' => XISFColorFilterArray.Element.Magenta,
            'Y' => XISFColorFilterArray.Element.Yellow,
            _   => null,
        };
    }

    /// <summary>The pattern character for a CFA element.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The ASCII pattern character.</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined element (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static char Character( this XISFColorFilterArray.Element element )
    {
        return element switch
        {
            XISFColorFilterArray.Element.None    => '0',
            XISFColorFilterArray.Element.Red     => 'R',
            XISFColorFilterArray.Element.Green   => 'G',
            XISFColorFilterArray.Element.Blue    => 'B',
            XISFColorFilterArray.Element.White   => 'W',
            XISFColorFilterArray.Element.Cyan    => 'C',
            XISFColorFilterArray.Element.Magenta => 'M',
            XISFColorFilterArray.Element.Yellow  => 'Y',
            _                                    => throw XISFException.InvalidElement( "Unknown color filter array element" ),
        };
    }
}
