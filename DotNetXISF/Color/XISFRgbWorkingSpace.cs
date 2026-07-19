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
/// A parsed XISF <c>&lt;RGBWorkingSpace&gt;</c> element: the parameters of a
/// colorimetrically-defined RGB working color space (RGBWS).
/// </summary>
/// <remarks>
/// All parameters are relative to the standard D50 reference white. When no
/// <c>RGBWorkingSpace</c> element is associated with an image, the default working space is
/// sRGB.
/// </remarks>
public readonly struct XISFRgbWorkingSpace : IEquatable< XISFRgbWorkingSpace >
{
    /// <summary>A parameter value for each of the three RGB primaries.</summary>
    public readonly struct Primaries : IEquatable< Primaries >
    {
        /// <summary>The value for the red primary.</summary>
        public double Red { get; }

        /// <summary>The value for the green primary.</summary>
        public double Green { get; }

        /// <summary>The value for the blue primary.</summary>
        public double Blue { get; }

        /// <summary>Creates a set of per-primary values.</summary>
        /// <param name="red">The value for the red primary.</param>
        /// <param name="green">The value for the green primary.</param>
        /// <param name="blue">The value for the blue primary.</param>
        public Primaries( double red, double green, double blue )
        {
            this.Red   = red;
            this.Green = green;
            this.Blue  = blue;
        }

        /// <summary>A single-line, human-readable summary of the per-primary values.</summary>
        /// <returns>The formatted summary.</returns>
        public override string ToString()
        {
            return $"( red: { this.Red.ToString( CultureInfo.InvariantCulture ) }, green: { this.Green.ToString( CultureInfo.InvariantCulture ) }, blue: { this.Blue.ToString( CultureInfo.InvariantCulture ) } )";
        }

        /// <summary>Returns whether this triplet equals another.</summary>
        /// <param name="other">The triplet to compare against.</param>
        /// <returns><c>true</c> if all three values are equal.</returns>
        public bool Equals( Primaries other ) => this.Red == other.Red && this.Green == other.Green && this.Blue == other.Blue;

        /// <summary>Returns whether this triplet equals another object.</summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an equal triplet.</returns>
        public override bool Equals( object? obj ) => obj is Primaries other && this.Equals( other );

        /// <summary>A hash code combining the three values.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => HashCode.Combine( this.Red, this.Green, this.Blue );

        /// <summary>Returns whether two triplets are equal.</summary>
        /// <param name="left">The first triplet.</param>
        /// <param name="right">The second triplet.</param>
        /// <returns><c>true</c> if the triplets are equal.</returns>
        public static bool operator ==( Primaries left, Primaries right ) => left.Equals( right );

        /// <summary>Returns whether two triplets are unequal.</summary>
        /// <param name="left">The first triplet.</param>
        /// <param name="right">The second triplet.</param>
        /// <returns><c>true</c> if the triplets are unequal.</returns>
        public static bool operator !=( Primaries left, Primaries right ) => left.Equals( right ) == false;
    }

    /// <summary>
    /// The gamma of an RGB working space: either a fixed exponent or the sRGB gamma function.
    /// </summary>
    public readonly struct Gamma : IEquatable< Gamma >
    {
        /// <summary>The fixed gamma exponent (meaningful only when not the sRGB function).</summary>
        private readonly double exponent;

        /// <summary>Whether this is the sRGB gamma function rather than a fixed exponent.</summary>
        private readonly bool srgb;

        /// <summary>Initializes a gamma with the given payload.</summary>
        /// <param name="exponent">The fixed gamma exponent.</param>
        /// <param name="srgb">Whether this is the sRGB gamma function.</param>
        private Gamma( double exponent, bool srgb )
        {
            this.exponent = exponent;
            this.srgb     = srgb;
        }

        /// <summary>Creates a fixed-exponent gamma.</summary>
        /// <param name="value">The gamma exponent (greater than zero).</param>
        /// <returns>The created gamma.</returns>
        public static Gamma Exponent( double value ) => new Gamma( value, false );

        /// <summary>The sRGB gamma function (rather than a fixed exponent).</summary>
        public static Gamma Srgb { get; } = new Gamma( 0, true );

        /// <summary>Whether this is the sRGB gamma function.</summary>
        public bool IsSrgb => this.srgb;

        /// <summary>The fixed gamma exponent, or <c>null</c> if this is the sRGB gamma function.</summary>
        public double? ExponentValue => this.srgb ? null : this.exponent;

        /// <summary>A single-line, human-readable summary of the gamma.</summary>
        /// <returns>The formatted summary.</returns>
        public override string ToString()
        {
            return this.srgb ? "sRGB" : $"exponent({ this.exponent.ToString( CultureInfo.InvariantCulture ) })";
        }

        /// <summary>Returns whether this gamma equals another.</summary>
        /// <param name="other">The gamma to compare against.</param>
        /// <returns><c>true</c> if both are the sRGB function, or both are the same exponent.</returns>
        public bool Equals( Gamma other ) => this.srgb == other.srgb && ( this.srgb || this.exponent == other.exponent );

        /// <summary>Returns whether this gamma equals another object.</summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an equal gamma.</returns>
        public override bool Equals( object? obj ) => obj is Gamma other && this.Equals( other );

        /// <summary>A hash code consistent with equality.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => this.srgb ? HashCode.Combine( true ) : HashCode.Combine( false, this.exponent );

        /// <summary>Returns whether two gamma values are equal.</summary>
        /// <param name="left">The first gamma.</param>
        /// <param name="right">The second gamma.</param>
        /// <returns><c>true</c> if the gamma values are equal.</returns>
        public static bool operator ==( Gamma left, Gamma right ) => left.Equals( right );

        /// <summary>Returns whether two gamma values are unequal.</summary>
        /// <param name="left">The first gamma.</param>
        /// <param name="right">The second gamma.</param>
        /// <returns><c>true</c> if the gamma values are unequal.</returns>
        public static bool operator !=( Gamma left, Gamma right ) => left.Equals( right ) == false;
    }

    /// <summary>The gamma exponent, or the sRGB gamma function.</summary>
    public Gamma GammaFunction { get; }

    /// <summary>The chromaticity <c>x</c> coordinates of the red, green and blue primaries.</summary>
    public Primaries X { get; }

    /// <summary>The chromaticity <c>y</c> coordinates of the red, green and blue primaries.</summary>
    public Primaries Y { get; }

    /// <summary>The luminance coefficients (<c>Y</c>) of the red, green and blue primaries.</summary>
    public Primaries Luminance { get; }

    /// <summary>The optional, human-readable name identifying the working space.</summary>
    public string? Name { get; }

    /// <summary>Parses an <c>&lt;RGBWorkingSpace&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;RGBWorkingSpace&gt;</c> element.</param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing a numeric <c>gamma</c> must be
    /// greater than zero; <see cref="XISFParsingOptions.AllowSpecDeviations"/> relaxes that
    /// check.
    /// </param>
    /// <exception cref="XISFException">
    /// A mandatory attribute is missing or malformed, or a strict validation check fails
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    internal XISFRgbWorkingSpace( XISFElement element, XISFParsingOptions options )
    {
        IReadOnlyDictionary< string, string > attributes = element.Attributes;
        bool                                  lenient    = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );

        this.X             = ParsePrimaries( attributes, "x" );
        this.Y             = ParsePrimaries( attributes, "y" );
        this.Luminance     = ParsePrimaries( attributes, "Y" );
        this.GammaFunction = ParseGamma( attributes, lenient );
        this.Name          = attributes.GetValueOrDefault( "name" );
    }

    /// <summary>A single-line, human-readable summary of the working space.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFRgbWorkingSpace {{ gamma: { this.GammaFunction }, x: { this.X }, y: { this.Y }, luminance: { this.Luminance }, name: { this.Name ?? "<nil>" } }}";
    }

    /// <summary>Returns whether this working space equals another.</summary>
    /// <param name="other">The working space to compare against.</param>
    /// <returns><c>true</c> if every field is equal.</returns>
    public bool Equals( XISFRgbWorkingSpace other )
    {
        return this.GammaFunction == other.GammaFunction
            && this.X == other.X
            && this.Y == other.Y
            && this.Luminance == other.Luminance
            && string.Equals( this.Name, other.Name, StringComparison.Ordinal );
    }

    /// <summary>Returns whether this working space equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal working space.</returns>
    public override bool Equals( object? obj ) => obj is XISFRgbWorkingSpace other && this.Equals( other );

    /// <summary>A hash code combining every field.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine( this.GammaFunction, this.X, this.Y, this.Luminance, this.Name );

    /// <summary>Returns whether two working spaces are equal.</summary>
    /// <param name="left">The first working space.</param>
    /// <param name="right">The second working space.</param>
    /// <returns><c>true</c> if the working spaces are equal.</returns>
    public static bool operator ==( XISFRgbWorkingSpace left, XISFRgbWorkingSpace right ) => left.Equals( right );

    /// <summary>Returns whether two working spaces are unequal.</summary>
    /// <param name="left">The first working space.</param>
    /// <param name="right">The second working space.</param>
    /// <returns><c>true</c> if the working spaces are unequal.</returns>
    public static bool operator !=( XISFRgbWorkingSpace left, XISFRgbWorkingSpace right ) => left.Equals( right ) == false;

    /// <summary>Parses a <c>red:green:blue</c> triplet attribute into <see cref="Primaries"/>.</summary>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="name">The attribute name (<c>x</c>, <c>y</c> or <c>Y</c>).</param>
    /// <returns>The parsed per-primary values.</returns>
    /// <exception cref="XISFException">
    /// The attribute is missing or is not three colon-separated real numbers
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static Primaries ParsePrimaries( IReadOnlyDictionary< string, string > attributes, string name )
    {
        if( attributes.TryGetValue( name, out string? raw ) == false )
        {
            throw XISFException.InvalidElement( $"RGBWorkingSpace is missing the '{ name }' attribute" );
        }

        string[] parts = raw.Split( ':' );

        if( parts.Length != 3
            || double.TryParse( parts[ 0 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double red ) == false
            || double.TryParse( parts[ 1 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double green ) == false
            || double.TryParse( parts[ 2 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double blue ) == false )
        {
            throw XISFException.InvalidElement( $"RGBWorkingSpace '{ name }' is not three colon-separated numbers: '{ raw }'" );
        }

        return new Primaries( red, green, blue );
    }

    /// <summary>Parses the <c>gamma</c> attribute.</summary>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="lenient">Whether the greater-than-zero constraint on a numeric gamma is relaxed.</param>
    /// <returns>The parsed gamma.</returns>
    /// <exception cref="XISFException">
    /// The attribute is missing, is neither <c>sRGB</c> nor a number, or (when not lenient) is
    /// a number that is not greater than zero (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static Gamma ParseGamma( IReadOnlyDictionary< string, string > attributes, bool lenient )
    {
        if( attributes.TryGetValue( "gamma", out string? raw ) == false )
        {
            throw XISFException.InvalidElement( "RGBWorkingSpace is missing the 'gamma' attribute" );
        }

        if( string.Equals( raw, "srgb", StringComparison.OrdinalIgnoreCase ) )
        {
            return Gamma.Srgb;
        }

        if( double.TryParse( raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value ) == false )
        {
            throw XISFException.InvalidElement( $"RGBWorkingSpace 'gamma' is neither 'sRGB' nor a number: '{ raw }'" );
        }

        if( value <= 0 && lenient == false )
        {
            throw XISFException.InvalidElement( $"RGBWorkingSpace 'gamma' exponent must be greater than zero, found { value.ToString( CultureInfo.InvariantCulture ) }" );
        }

        return Gamma.Exponent( value );
    }
}
