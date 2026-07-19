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
using System.Linq;

namespace DotNetXISF;

/// <summary>
/// An XISF <c>&lt;FITSKeyword&gt;</c>: an embedded FITS header card.
/// </summary>
/// <remarks>
/// XISF carries legacy FITS keywords verbatim, so the value is kept as the raw
/// FITS-formatted string rather than a typed value. A keyword has an up-to-8-character
/// <see cref="Name"/>, an optional <see cref="Value"/> (empty for <c>HISTORY</c>/
/// <c>COMMENT</c> cards), and an optional <see cref="Comment"/>.
/// </remarks>
public readonly struct XISFFitsKeyword : IEquatable< XISFFitsKeyword >
{
    /// <summary>The keyword name backing field (nullable so a default value is safe).</summary>
    private readonly string? name;

    /// <summary>The FITS keyword name (up to 8 characters).</summary>
    public string Name => this.name ?? "";

    /// <summary>
    /// The raw FITS value string, or <c>null</c> when the keyword carries no value (for
    /// example <c>HISTORY</c> or <c>COMMENT</c>).
    /// </summary>
    public string? Value { get; }

    /// <summary>The keyword's optional comment.</summary>
    public string? Comment { get; }

    /// <summary>Parses a FITS keyword from a <c>&lt;FITSKeyword&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;FITSKeyword&gt;</c> element.</param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing the name must be at most 8
    /// characters drawn from the FITS keyword character set;
    /// <see cref="XISFParsingOptions.AllowSpecDeviations"/> relaxes that check.
    /// </param>
    /// <exception cref="XISFException">
    /// The <c>name</c> attribute is missing or, under strict parsing, invalid
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    internal XISFFitsKeyword( XISFElement element, XISFParsingOptions options )
    {
        IReadOnlyDictionary< string, string > attributes = element.Attributes;

        if( attributes.TryGetValue( "name", out string? name ) == false || name.Length == 0 )
        {
            throw XISFException.InvalidElement( "FITSKeyword is missing a 'name' attribute" );
        }

        if( options.HasFlag( XISFParsingOptions.AllowSpecDeviations ) == false && IsValidName( name ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid FITS keyword name: '{ name }'" );
        }

        string? value = attributes.GetValueOrDefault( "value" );

        this.name    = name;
        this.Value   = string.IsNullOrEmpty( value ) ? null : value;
        this.Comment = attributes.GetValueOrDefault( "comment" );
    }

    /// <summary>A single-line, human-readable summary of the keyword.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFFitsKeyword {{ name: { this.Name }, value: { this.Value ?? "<nil>" }, comment: { this.Comment ?? "<nil>" } }}";
    }

    /// <summary>Returns whether this keyword equals another.</summary>
    /// <param name="other">The keyword to compare against.</param>
    /// <returns><c>true</c> if the name, value and comment are all equal.</returns>
    public bool Equals( XISFFitsKeyword other )
    {
        return string.Equals( this.Name, other.Name, StringComparison.Ordinal )
            && string.Equals( this.Value, other.Value, StringComparison.Ordinal )
            && string.Equals( this.Comment, other.Comment, StringComparison.Ordinal );
    }

    /// <summary>Returns whether this keyword equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal keyword.</returns>
    public override bool Equals( object? obj ) => obj is XISFFitsKeyword other && this.Equals( other );

    /// <summary>A hash code combining the name, value and comment.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine( this.Name, this.Value, this.Comment );

    /// <summary>Returns whether two keywords are equal.</summary>
    /// <param name="left">The first keyword.</param>
    /// <param name="right">The second keyword.</param>
    /// <returns><c>true</c> if the keywords are equal.</returns>
    public static bool operator ==( XISFFitsKeyword left, XISFFitsKeyword right ) => left.Equals( right );

    /// <summary>Returns whether two keywords are unequal.</summary>
    /// <param name="left">The first keyword.</param>
    /// <param name="right">The second keyword.</param>
    /// <returns><c>true</c> if the keywords are unequal.</returns>
    public static bool operator !=( XISFFitsKeyword left, XISFFitsKeyword right ) => left.Equals( right ) == false;

    /// <summary>Returns whether a name is a valid FITS keyword name under strict parsing.</summary>
    /// <param name="name">The name to validate.</param>
    /// <returns><c>true</c> if the name is at most 8 allowed characters.</returns>
    private static bool IsValidName( string name )
    {
        return name.Length <= 8 && name.All( IsAllowedNameCharacter );
    }

    /// <summary>Returns whether a character is permitted in a FITS keyword name.</summary>
    /// <remarks>The permitted set is the uppercase letters, digits, the hyphen and the underscore.</remarks>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if the character is permitted.</returns>
    private static bool IsAllowedNameCharacter( char character )
    {
        return ( character >= 'A' && character <= 'Z' )
            || ( character >= '0' && character <= '9' )
            || character == '_'
            || character == '-';
    }
}
