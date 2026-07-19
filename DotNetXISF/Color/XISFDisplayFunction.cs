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
/// A parsed XISF <c>&lt;DisplayFunction&gt;</c> element: the parameters of a display
/// function (DF) associated with an image.
/// </summary>
/// <remarks>
/// A display function has five parameter vectors — midtones balance, shadows clipping,
/// highlights clipping, shadows dynamic range expansion, and highlights dynamic range
/// expansion — each carrying one value per image component (red/gray, green, blue, and
/// lightness). When no <c>DisplayFunction</c> element is associated with an image, the
/// default is the identity function.
/// </remarks>
public readonly struct XISFDisplayFunction : IEquatable< XISFDisplayFunction >
{
    /// <summary>One value of a display-function parameter vector for each image component.</summary>
    public readonly struct Components : IEquatable< Components >
    {
        /// <summary>The value for the red/gray component.</summary>
        public double Rk { get; }

        /// <summary>The value for the green component.</summary>
        public double G { get; }

        /// <summary>The value for the blue component.</summary>
        public double B { get; }

        /// <summary>The value for the lightness component.</summary>
        public double L { get; }

        /// <summary>Creates a per-component parameter vector.</summary>
        /// <param name="rk">The value for the red/gray component.</param>
        /// <param name="g">The value for the green component.</param>
        /// <param name="b">The value for the blue component.</param>
        /// <param name="l">The value for the lightness component.</param>
        public Components( double rk, double g, double b, double l )
        {
            this.Rk = rk;
            this.G  = g;
            this.B  = b;
            this.L  = l;
        }

        /// <summary>A single-line, human-readable summary of the per-component values.</summary>
        /// <returns>The formatted summary.</returns>
        public override string ToString()
        {
            return $"( rk: { this.Rk.ToString( CultureInfo.InvariantCulture ) }, g: { this.G.ToString( CultureInfo.InvariantCulture ) }, b: { this.B.ToString( CultureInfo.InvariantCulture ) }, l: { this.L.ToString( CultureInfo.InvariantCulture ) } )";
        }

        /// <summary>Returns whether this vector equals another.</summary>
        /// <param name="other">The vector to compare against.</param>
        /// <returns><c>true</c> if all four components are equal.</returns>
        public bool Equals( Components other ) => this.Rk == other.Rk && this.G == other.G && this.B == other.B && this.L == other.L;

        /// <summary>Returns whether this vector equals another object.</summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an equal vector.</returns>
        public override bool Equals( object? obj ) => obj is Components other && this.Equals( other );

        /// <summary>A hash code combining the four components.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => HashCode.Combine( this.Rk, this.G, this.B, this.L );

        /// <summary>Returns whether two vectors are equal.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns><c>true</c> if the vectors are equal.</returns>
        public static bool operator ==( Components left, Components right ) => left.Equals( right );

        /// <summary>Returns whether two vectors are unequal.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns><c>true</c> if the vectors are unequal.</returns>
        public static bool operator !=( Components left, Components right ) => left.Equals( right ) == false;
    }

    /// <summary>The midtones balance parameters (the <c>m</c> attribute).</summary>
    public Components MidtonesBalance { get; }

    /// <summary>The shadows clipping-point parameters (the <c>s</c> attribute).</summary>
    public Components ShadowsClipping { get; }

    /// <summary>The highlights clipping-point parameters (the <c>h</c> attribute).</summary>
    public Components HighlightsClipping { get; }

    /// <summary>The shadows dynamic-range expansion parameters (the <c>l</c> attribute).</summary>
    public Components ShadowsExpansion { get; }

    /// <summary>The highlights dynamic-range expansion parameters (the <c>r</c> attribute).</summary>
    public Components HighlightsExpansion { get; }

    /// <summary>The optional, human-readable name identifying the display function.</summary>
    public string? Name { get; }

    /// <summary>Parses a <c>&lt;DisplayFunction&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;DisplayFunction&gt;</c> element.</param>
    /// <param name="options">
    /// The parsing options to apply. (No strict-only validation is currently applied beyond
    /// the presence and shape of the mandatory attributes.)
    /// </param>
    /// <exception cref="XISFException">
    /// A mandatory attribute is missing or is not four colon-separated real numbers
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    internal XISFDisplayFunction( XISFElement element, XISFParsingOptions options )
    {
        _ = options;

        IReadOnlyDictionary< string, string > attributes = element.Attributes;

        this.MidtonesBalance     = ParseComponents( attributes, "m" );
        this.ShadowsClipping     = ParseComponents( attributes, "s" );
        this.HighlightsClipping  = ParseComponents( attributes, "h" );
        this.ShadowsExpansion    = ParseComponents( attributes, "l" );
        this.HighlightsExpansion = ParseComponents( attributes, "r" );
        this.Name                = attributes.GetValueOrDefault( "name" );
    }

    /// <summary>A single-line, human-readable summary of the display function.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFDisplayFunction {{ m: { this.MidtonesBalance }, s: { this.ShadowsClipping }, h: { this.HighlightsClipping }, l: { this.ShadowsExpansion }, r: { this.HighlightsExpansion }, name: { this.Name ?? "<nil>" } }}";
    }

    /// <summary>Returns whether this display function equals another.</summary>
    /// <param name="other">The display function to compare against.</param>
    /// <returns><c>true</c> if every parameter vector and the name are equal.</returns>
    public bool Equals( XISFDisplayFunction other )
    {
        return this.MidtonesBalance == other.MidtonesBalance
            && this.ShadowsClipping == other.ShadowsClipping
            && this.HighlightsClipping == other.HighlightsClipping
            && this.ShadowsExpansion == other.ShadowsExpansion
            && this.HighlightsExpansion == other.HighlightsExpansion
            && string.Equals( this.Name, other.Name, StringComparison.Ordinal );
    }

    /// <summary>Returns whether this display function equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal display function.</returns>
    public override bool Equals( object? obj ) => obj is XISFDisplayFunction other && this.Equals( other );

    /// <summary>A hash code combining every parameter vector and the name.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        hash.Add( this.MidtonesBalance );
        hash.Add( this.ShadowsClipping );
        hash.Add( this.HighlightsClipping );
        hash.Add( this.ShadowsExpansion );
        hash.Add( this.HighlightsExpansion );
        hash.Add( this.Name, StringComparer.Ordinal );

        return hash.ToHashCode();
    }

    /// <summary>Returns whether two display functions are equal.</summary>
    /// <param name="left">The first display function.</param>
    /// <param name="right">The second display function.</param>
    /// <returns><c>true</c> if the display functions are equal.</returns>
    public static bool operator ==( XISFDisplayFunction left, XISFDisplayFunction right ) => left.Equals( right );

    /// <summary>Returns whether two display functions are unequal.</summary>
    /// <param name="left">The first display function.</param>
    /// <param name="right">The second display function.</param>
    /// <returns><c>true</c> if the display functions are unequal.</returns>
    public static bool operator !=( XISFDisplayFunction left, XISFDisplayFunction right ) => left.Equals( right ) == false;

    /// <summary>Parses a <c>rk:g:b:l</c> parameter-vector attribute into <see cref="Components"/>.</summary>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="name">The attribute name (<c>m</c>, <c>s</c>, <c>h</c>, <c>l</c> or <c>r</c>).</param>
    /// <returns>The parsed per-component values.</returns>
    /// <exception cref="XISFException">
    /// The attribute is missing or is not four colon-separated real numbers
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static Components ParseComponents( IReadOnlyDictionary< string, string > attributes, string name )
    {
        if( attributes.TryGetValue( name, out string? raw ) == false )
        {
            throw XISFException.InvalidElement( $"DisplayFunction is missing the '{ name }' attribute" );
        }

        string[] parts = raw.Split( ':' );

        if( parts.Length != 4
            || double.TryParse( parts[ 0 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double rk ) == false
            || double.TryParse( parts[ 1 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double g ) == false
            || double.TryParse( parts[ 2 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double b ) == false
            || double.TryParse( parts[ 3 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double l ) == false )
        {
            throw XISFException.InvalidElement( $"DisplayFunction '{ name }' is not four colon-separated numbers: '{ raw }'" );
        }

        return new Components( rk, g, b, l );
    }
}
