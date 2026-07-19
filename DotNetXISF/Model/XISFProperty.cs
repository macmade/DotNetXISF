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
using System.Text;

namespace DotNetXISF;

/// <summary>
/// An XISF <c>&lt;Property&gt;</c>: a typed, identified piece of metadata.
/// </summary>
/// <remarks>
/// A property has a hierarchical, colon-separated identifier (for example
/// <c>Observation:Time:Start</c>), a declared <see cref="XISFPropertyType"/>, a typed
/// <see cref="XISFValue"/>, and optional <see cref="Comment"/> and <see cref="Format"/>
/// attributes. Scalar, complex and time-point values are carried in the element's
/// <c>value</c> attribute; a string value is the element's character content (or, when a
/// <c>location</c> is present, a data block decoded as UTF-8). Vector, matrix and
/// <c>ByteArray</c> values are carried in a data block and exposed as opaque bytes, with
/// their shape in <see cref="Length"/> / <see cref="Rows"/> / <see cref="Columns"/>.
/// </remarks>
public readonly struct XISFProperty : IEquatable< XISFProperty >
{
    /// <summary>A strict UTF-8 codec that throws rather than substituting on invalid bytes.</summary>
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding( false, true );

    /// <summary>The property's identifier backing field (nullable so a default value is safe).</summary>
    private readonly string? id;

    /// <summary>The property's hierarchical identifier (colon-separated components).</summary>
    public string Id => this.id ?? "";

    /// <summary>The property's declared type.</summary>
    public XISFPropertyType Type { get; }

    /// <summary>The property's typed value.</summary>
    public XISFValue Value { get; }

    /// <summary>The property's optional comment.</summary>
    public string? Comment { get; }

    /// <summary>The property's optional format specifier.</summary>
    public string? Format { get; }

    /// <summary>The element count of a vector- or <c>ByteArray</c>-typed value, or <c>null</c> otherwise.</summary>
    public long? Length { get; }

    /// <summary>The row count of a matrix-typed value, or <c>null</c> otherwise.</summary>
    public long? Rows { get; }

    /// <summary>The column count of a matrix-typed value, or <c>null</c> otherwise.</summary>
    public long? Columns { get; }

    /// <summary>Parses a property from a <c>&lt;Property&gt;</c> element.</summary>
    /// <remarks>
    /// Handles the value-attribute types (scalar, complex, time point), string values
    /// (inline content or a UTF-8 data block), and the data-block-backed vector, matrix and
    /// <c>ByteArray</c> types (exposed as opaque bytes).
    /// </remarks>
    /// <param name="element">The <c>&lt;Property&gt;</c> element.</param>
    /// <param name="fileData">
    /// The complete file bytes, used to resolve an <c>attachment</c> data block for a
    /// vector/matrix/<c>ByteArray</c>/data-block string value.
    /// </param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external data blocks; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing the <c>id</c> must be a valid
    /// colon-separated identifier and the dimension attributes must be present;
    /// <see cref="XISFParsingOptions.AllowSpecDeviations"/> relaxes those checks.
    /// </param>
    /// <exception cref="XISFException">
    /// A required attribute is missing, the type is unknown, the <c>id</c> is invalid, or the
    /// value cannot be parsed (<see cref="XISFErrorKind.InvalidElement"/>); or any error
    /// raised while resolving a data block.
    /// </exception>
    internal XISFProperty( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        IReadOnlyDictionary< string, string > attributes = element.Attributes;

        if( attributes.TryGetValue( "id", out string? id ) == false || id.Length == 0 )
        {
            throw XISFException.InvalidElement( "Property is missing an 'id' attribute" );
        }

        bool lenient = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );

        if( lenient == false && IsValidIdentifier( id ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid property id: '{ id }'" );
        }

        if( attributes.TryGetValue( "type", out string? typeString ) == false )
        {
            throw XISFException.InvalidElement( $"Property '{ id }' is missing a 'type' attribute" );
        }

        if( XISFPropertyTypeExtensions.FromSpecToken( typeString ) is not XISFPropertyType type )
        {
            throw XISFException.InvalidElement( $"Property '{ id }' has unknown type '{ typeString }'" );
        }

        this.id      = id;
        this.Type    = type;
        this.Comment = attributes.GetValueOrDefault( "comment" );
        this.Format  = attributes.GetValueOrDefault( "format" );

        switch( type.Category() )
        {
            case XISFPropertyCategory.Scalar or XISFPropertyCategory.Complex or XISFPropertyCategory.TimePoint:

                if( attributes.TryGetValue( "value", out string? raw ) == false )
                {
                    throw XISFException.InvalidElement( $"Property '{ id }' of type { type.SpecToken() } is missing a 'value' attribute" );
                }

                this.Value   = XISFValue.FromAttribute( raw, type );
                this.Length  = null;
                this.Rows    = null;
                this.Columns = null;

                break;

            case XISFPropertyCategory.String:

                this.Length  = null;
                this.Rows    = null;
                this.Columns = null;
                this.Value   = ParseStringValue( element, attributes, fileData, baseDirectory, options, id );

                break;

            case XISFPropertyCategory.Vector:

                this.Length  = Dimension( attributes, "length", id, required: lenient == false );
                this.Rows    = null;
                this.Columns = null;
                this.Value   = XISFValue.Data( new XISFDataBlock( element, fileData, baseDirectory, options ).Data );

                break;

            case XISFPropertyCategory.Matrix:

                this.Length  = null;
                this.Rows    = Dimension( attributes, "rows", id, required: lenient == false );
                this.Columns = Dimension( attributes, "columns", id, required: lenient == false );
                this.Value   = XISFValue.Data( new XISFDataBlock( element, fileData, baseDirectory, options ).Data );

                break;

            default:

                throw XISFException.InvalidElement( $"Property '{ id }' has an unsupported type category" );
        }
    }

    /// <summary>A single-line, human-readable summary of the property.</summary>
    /// <remarks>
    /// The <c>value</c> portion is rendered through <see cref="XISFValue"/>'s own string
    /// form.
    /// </remarks>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFProperty {{ id: { this.Id }, type: { this.Type.SpecToken() }, kind: { this.Value.Kind.Description() }, value: { this.Value }, comment: { this.Comment ?? "<nil>" }, format: { this.Format ?? "<nil>" } }}";
    }

    /// <summary>Returns whether this property equals another.</summary>
    /// <param name="other">The property to compare against.</param>
    /// <returns><c>true</c> if every field is equal.</returns>
    public bool Equals( XISFProperty other )
    {
        return string.Equals( this.Id, other.Id, StringComparison.Ordinal )
            && this.Type == other.Type
            && this.Value.Equals( other.Value )
            && string.Equals( this.Comment, other.Comment, StringComparison.Ordinal )
            && string.Equals( this.Format, other.Format, StringComparison.Ordinal )
            && this.Length == other.Length
            && this.Rows == other.Rows
            && this.Columns == other.Columns;
    }

    /// <summary>Returns whether this property equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal property.</returns>
    public override bool Equals( object? obj ) => obj is XISFProperty other && this.Equals( other );

    /// <summary>A hash code combining every field.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        hash.Add( this.Id, StringComparer.Ordinal );
        hash.Add( this.Type );
        hash.Add( this.Value );
        hash.Add( this.Comment, StringComparer.Ordinal );
        hash.Add( this.Format, StringComparer.Ordinal );
        hash.Add( this.Length );
        hash.Add( this.Rows );
        hash.Add( this.Columns );

        return hash.ToHashCode();
    }

    /// <summary>Returns whether two properties are equal.</summary>
    /// <param name="left">The first property.</param>
    /// <param name="right">The second property.</param>
    /// <returns><c>true</c> if the properties are equal.</returns>
    public static bool operator ==( XISFProperty left, XISFProperty right ) => left.Equals( right );

    /// <summary>Returns whether two properties are unequal.</summary>
    /// <param name="left">The first property.</param>
    /// <param name="right">The second property.</param>
    /// <returns><c>true</c> if the properties are unequal.</returns>
    public static bool operator !=( XISFProperty left, XISFProperty right ) => left.Equals( right ) == false;

    /// <summary>Parses the direct-child <c>&lt;Property&gt;</c> elements of an element.</summary>
    /// <remarks>
    /// All property types are parsed, including the data-block-backed vector, matrix and
    /// <c>ByteArray</c> values. Under strict parsing a property with a missing or unknown
    /// type — or one that otherwise fails to parse — is an error;
    /// <see cref="XISFParsingOptions.AllowSpecDeviations"/> skips it instead, so a single
    /// malformed property does not fail the whole unit.
    /// </remarks>
    /// <param name="element">The element whose <c>&lt;Property&gt;</c> children to parse.</param>
    /// <param name="fileData">The complete file bytes, used to resolve data-block-backed property values.</param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external data blocks; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed properties, in document order.</returns>
    /// <exception cref="XISFException">
    /// Any error raised while parsing a property, under strict parsing.
    /// </exception>
    internal static IReadOnlyList< XISFProperty > ParseList( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        bool                 lenient    = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );
        List< XISFProperty > properties = new List< XISFProperty >();

        foreach( XISFElement child in element.ChildrenNamed( "Property" ) )
        {
            string? typeString = child.Attributes.GetValueOrDefault( "type" );

            if( typeString is null || XISFPropertyTypeExtensions.FromSpecToken( typeString ) is null )
            {
                if( lenient )
                {
                    continue;
                }

                throw XISFException.InvalidElement( $"Property has a missing or unknown type: '{ typeString ?? "" }'" );
            }

            try
            {
                properties.Add( new XISFProperty( child, fileData, baseDirectory, options ) );
            }
            catch( XISFException )
            {
                if( lenient )
                {
                    continue;
                }

                throw;
            }
        }

        return properties;
    }

    /// <summary>Parses a string property's value from inline content or a UTF-8 data block.</summary>
    /// <param name="element">The <c>&lt;Property&gt;</c> element.</param>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="fileData">The complete file bytes, used to resolve a data-block value.</param>
    /// <param name="baseDirectory">The header file's directory for <c>@header_dir</c> resolution, or <c>null</c>.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <param name="id">The property identifier, for error reporting.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="XISFException">
    /// The data block cannot be resolved, or its bytes are not valid UTF-8
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseStringValue( XISFElement element, IReadOnlyDictionary< string, string > attributes, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options, string id )
    {
        if( attributes.ContainsKey( "location" ) == false )
        {
            return XISFValue.String( element.Content );
        }

        ReadOnlyMemory< byte > bytes = new XISFDataBlock( element, fileData, baseDirectory, options ).Data;

        try
        {
            return XISFValue.String( StrictUtf8.GetString( bytes.Span ) );
        }
        catch( DecoderFallbackException )
        {
            throw XISFException.InvalidElement( $"Property '{ id }' String data block is not valid UTF-8" );
        }
    }

    /// <summary>Parses a non-negative integer dimension attribute (<c>length</c>/<c>rows</c>/<c>columns</c>).</summary>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="name">The attribute name to read.</param>
    /// <param name="id">The property identifier, for error reporting.</param>
    /// <param name="required">Whether a missing attribute is an error.</param>
    /// <returns>The parsed dimension, or <c>null</c> if absent and not required.</returns>
    /// <remarks>
    /// Parsed as a 64-bit integer to match the format's dimension width, consistent with
    /// <see cref="XISFGeometry"/>.
    /// </remarks>
    /// <exception cref="XISFException">
    /// The attribute is required and missing, or present but not a non-negative integer
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static long? Dimension( IReadOnlyDictionary< string, string > attributes, string name, string id, bool required )
    {
        if( attributes.TryGetValue( name, out string? raw ) == false )
        {
            if( required )
            {
                throw XISFException.InvalidElement( $"Property '{ id }' is missing the '{ name }' attribute" );
            }

            return null;
        }

        if( long.TryParse( raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value ) == false || value < 0 )
        {
            throw XISFException.InvalidElement( $"Property '{ id }' has an invalid '{ name }' attribute: '{ raw }'" );
        }

        return value;
    }

    /// <summary>Returns whether a string is a valid XISF property identifier.</summary>
    /// <remarks>
    /// A property identifier is a non-empty, colon-separated sequence of simple identifiers,
    /// each matching <c>[_a-zA-Z][_a-zA-Z0-9]*</c> (for example
    /// <c>Instrument:Telescope:FocalLength</c>).
    /// </remarks>
    /// <param name="id">The identifier to validate.</param>
    /// <returns><c>true</c> if <paramref name="id"/> is a valid property identifier.</returns>
    private static bool IsValidIdentifier( string id )
    {
        string[] components = id.Split( ':' );

        return components.Length > 0 && components.All( component => component.IsValidXisfIdentifier() );
    }
}
