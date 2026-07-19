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
using System.Linq;

namespace DotNetXISF;

/// <summary>
/// A parsed XISF <c>&lt;Metadata&gt;</c> element: the set of unit-level properties that
/// describe an XISF unit.
/// </summary>
/// <remarks>
/// The metadata is serialized as a collection of child <c>&lt;Property&gt;</c> elements
/// whose identifiers use the reserved <c>XISF:</c> namespace prefix (for example
/// <c>XISF:CreationTime</c> and <c>XISF:CreatorApplication</c>). The properties are
/// exposed as ordinary <see cref="XISFProperty"/> values.
/// </remarks>
public readonly struct XISFMetadata : IEquatable< XISFMetadata >
{
    /// <summary>The metadata properties backing field (nullable so a default value is safe).</summary>
    private readonly XISFProperty[]? properties;

    /// <summary>The metadata properties, in document order.</summary>
    /// <remarks>Each read returns a fresh snapshot so the internal state cannot be mutated.</remarks>
    public IReadOnlyList< XISFProperty > Properties => this.properties?.ToArray() ?? [];

    /// <summary>The first metadata property whose identifier matches, or <c>null</c> if none does.</summary>
    /// <param name="id">The property identifier to look up (for example <c>XISF:CreatorApplication</c>).</param>
    /// <returns>The first matching property, or <c>null</c>.</returns>
    public XISFProperty? this[ string id ]
    {
        get
        {
            foreach( XISFProperty property in this.properties ?? [] )
            {
                if( string.Equals( property.Id, id, StringComparison.Ordinal ) )
                {
                    return property;
                }
            }

            return null;
        }
    }

    /// <summary>Parses a <c>&lt;Metadata&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;Metadata&gt;</c> element.</param>
    /// <param name="fileData">The complete file bytes, used to resolve any data-block-backed property values.</param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external data blocks; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">
    /// Any error raised while parsing a child property under strict parsing.
    /// </exception>
    internal XISFMetadata( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        this.properties = XISFProperty.ParseList( element, fileData, baseDirectory, options ).ToArray();
    }

    /// <summary>A single-line, human-readable summary of the metadata.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFMetadata {{ properties: { ( this.properties?.Length ?? 0 ).ToString( CultureInfo.InvariantCulture ) } }}";
    }

    /// <summary>Returns whether this metadata equals another.</summary>
    /// <param name="other">The metadata to compare against.</param>
    /// <returns><c>true</c> if both carry the same properties in the same order.</returns>
    public bool Equals( XISFMetadata other )
    {
        return ( this.properties ?? [] ).SequenceEqual( other.properties ?? [] );
    }

    /// <summary>Returns whether this metadata equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is equal metadata.</returns>
    public override bool Equals( object? obj ) => obj is XISFMetadata other && this.Equals( other );

    /// <summary>A hash code combining every property.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        foreach( XISFProperty property in this.properties ?? [] )
        {
            hash.Add( property );
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns whether two metadata values are equal.</summary>
    /// <param name="left">The first metadata value.</param>
    /// <param name="right">The second metadata value.</param>
    /// <returns><c>true</c> if the metadata values are equal.</returns>
    public static bool operator ==( XISFMetadata left, XISFMetadata right ) => left.Equals( right );

    /// <summary>Returns whether two metadata values are unequal.</summary>
    /// <param name="left">The first metadata value.</param>
    /// <param name="right">The second metadata value.</param>
    /// <returns><c>true</c> if the metadata values are unequal.</returns>
    public static bool operator !=( XISFMetadata left, XISFMetadata right ) => left.Equals( right ) == false;
}
