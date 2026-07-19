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
/// A parsed XISF <c>&lt;Resolution&gt;</c> element: the display resolution associated
/// with an image.
/// </summary>
/// <remarks>
/// Resolution defines how many pixels are represented per unit of surface on a display
/// medium, measured either in pixels per inch or per centimeter.
/// </remarks>
public readonly struct XISFResolution : IEquatable< XISFResolution >
{
    /// <summary>The unit of length used to express a resolution.</summary>
    public enum Unit
    {
        /// <summary>Resolution measured in pixels per inch (the default).</summary>
        Inch,

        /// <summary>Resolution measured in pixels per centimeter.</summary>
        Centimeter,
    }

    /// <summary>The horizontal (X-axis) resolution, in pixels per <see cref="ResolutionUnit"/>.</summary>
    public double Horizontal { get; }

    /// <summary>The vertical (Y-axis) resolution, in pixels per <see cref="ResolutionUnit"/>.</summary>
    public double Vertical { get; }

    /// <summary>The unit the resolution values are expressed in (defaults to inches).</summary>
    public Unit ResolutionUnit { get; }

    /// <summary>Parses a <c>&lt;Resolution&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;Resolution&gt;</c> element.</param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing both resolution values must be
    /// present and greater than zero and the <c>unit</c>, if present, must be a known value;
    /// <see cref="XISFParsingOptions.AllowSpecDeviations"/> relaxes the positivity check and
    /// falls back to the default unit for an unknown value.
    /// </param>
    /// <exception cref="XISFException">
    /// A mandatory attribute is missing or not a number, or a strict validation check fails
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    internal XISFResolution( XISFElement element, XISFParsingOptions options )
    {
        IReadOnlyDictionary< string, string > attributes = element.Attributes;
        bool                                  lenient    = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );

        this.Horizontal = Value( attributes, "horizontal", lenient );
        this.Vertical   = Value( attributes, "vertical",   lenient );

        if( attributes.TryGetValue( "unit", out string? raw ) == false )
        {
            this.ResolutionUnit = Unit.Inch;

            return;
        }

        if( XISFResolutionUnitExtensions.FromToken( raw ) is Unit unit )
        {
            this.ResolutionUnit = unit;

            return;
        }

        if( lenient )
        {
            this.ResolutionUnit = Unit.Inch;

            return;
        }

        throw XISFException.InvalidElement( $"Resolution has an unknown 'unit' attribute: '{ raw }'" );
    }

    /// <summary>A single-line, human-readable summary of the resolution.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFResolution {{ horizontal: { this.Horizontal.ToString( CultureInfo.InvariantCulture ) }, vertical: { this.Vertical.ToString( CultureInfo.InvariantCulture ) }, unit: { this.ResolutionUnit.Token() } }}";
    }

    /// <summary>Returns whether this resolution equals another.</summary>
    /// <param name="other">The resolution to compare against.</param>
    /// <returns><c>true</c> if the values and unit are equal.</returns>
    public bool Equals( XISFResolution other )
    {
        return this.Horizontal == other.Horizontal && this.Vertical == other.Vertical && this.ResolutionUnit == other.ResolutionUnit;
    }

    /// <summary>Returns whether this resolution equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal resolution.</returns>
    public override bool Equals( object? obj ) => obj is XISFResolution other && this.Equals( other );

    /// <summary>A hash code combining the values and unit.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine( this.Horizontal, this.Vertical, this.ResolutionUnit );

    /// <summary>Returns whether two resolutions are equal.</summary>
    /// <param name="left">The first resolution.</param>
    /// <param name="right">The second resolution.</param>
    /// <returns><c>true</c> if the resolutions are equal.</returns>
    public static bool operator ==( XISFResolution left, XISFResolution right ) => left.Equals( right );

    /// <summary>Returns whether two resolutions are unequal.</summary>
    /// <param name="left">The first resolution.</param>
    /// <param name="right">The second resolution.</param>
    /// <returns><c>true</c> if the resolutions are unequal.</returns>
    public static bool operator !=( XISFResolution left, XISFResolution right ) => left.Equals( right ) == false;

    /// <summary>Parses a mandatory, strictly-positive resolution value attribute.</summary>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="name">The attribute name (<c>horizontal</c> or <c>vertical</c>).</param>
    /// <param name="lenient">Whether the positivity constraint is relaxed.</param>
    /// <returns>The parsed resolution value.</returns>
    /// <exception cref="XISFException">
    /// The attribute is missing, not a number, or (when not lenient) not greater than zero
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static double Value( IReadOnlyDictionary< string, string > attributes, string name, bool lenient )
    {
        if( attributes.TryGetValue( name, out string? raw ) == false || double.TryParse( raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value ) == false )
        {
            throw XISFException.InvalidElement( $"Resolution has a missing or invalid '{ name }' attribute: '{ attributes.GetValueOrDefault( name ) ?? "" }'" );
        }

        if( value <= 0 && lenient == false )
        {
            throw XISFException.InvalidElement( $"Resolution '{ name }' must be greater than zero, found { value.ToString( CultureInfo.InvariantCulture ) }" );
        }

        return value;
    }
}

/// <summary>Token parsing and formatting for <see cref="XISFResolution.Unit"/>.</summary>
public static class XISFResolutionUnitExtensions
{
    /// <summary>The default unit when the <c>unit</c> attribute is absent (inches).</summary>
    public const XISFResolution.Unit Default = XISFResolution.Unit.Inch;

    /// <summary>Parses a resolution-unit token.</summary>
    /// <param name="token">The unit token, matched exactly (case-sensitive).</param>
    /// <returns>The matching unit, or <c>null</c> when the token is not a known unit.</returns>
    public static XISFResolution.Unit? FromToken( string token )
    {
        return token switch
        {
            "inch" => XISFResolution.Unit.Inch,
            "cm"   => XISFResolution.Unit.Centimeter,
            _      => null,
        };
    }

    /// <summary>The spec token for a resolution unit.</summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The spec token (<c>inch</c> or <c>cm</c>).</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined unit (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static string Token( this XISFResolution.Unit unit )
    {
        return unit switch
        {
            XISFResolution.Unit.Inch       => "inch",
            XISFResolution.Unit.Centimeter => "cm",
            _                              => throw XISFException.InvalidElement( "Unknown resolution unit" ),
        };
    }
}
