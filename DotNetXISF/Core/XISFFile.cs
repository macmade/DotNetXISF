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
using System.IO;
using System.Linq;
using System.Text;

namespace DotNetXISF;

/// <summary>
/// A parsed XISF (Extensible Image Serialization Format) monolithic file.
/// </summary>
/// <remarks>
/// A monolithic XISF file begins with a 16-byte binary preamble - the 8-byte
/// <c>XISF0100</c> signature, a little-endian <c>UInt32</c> giving the length of
/// the XML header, and a 4-byte reserved field - followed by the UTF-8 XML header
/// and, after it, the attached binary data blocks. This type reads and validates
/// the preamble and the XML header, then exposes the parsed images, properties,
/// embedded FITS keywords and metadata.
/// <para>
/// The file's bytes are not held in a separate whole-file buffer: parsing happens
/// during construction, and the bytes that later stages need - for
/// <c>attachment(position, size)</c> data-block locations, whose positions are
/// absolute offsets from the start of the file - are captured into the per-block
/// model objects then, as cheap slices of the original bytes.
/// </para>
/// <para>
/// <see cref="XISFParsingOptions"/> controls how strictly the preamble and header
/// are validated; for example a non-zero reserved field is rejected unless
/// <see cref="XISFParsingOptions.AllowSpecDeviations"/> is set.
/// </para>
/// <para>
/// This is a reference type holding parsed file state. It composes lazily-decoding
/// blocks whose results cache on first read, so even concurrent reads of a
/// fully-parsed file race: it is not thread-safe.
/// </para>
/// </remarks>
public class XISFFile
{
    /// <summary>
    /// The 8-byte ASCII signature that opens every monolithic XISF file.
    /// </summary>
    public const string Signature = "XISF0100";

    /// <summary>
    /// The size, in bytes, of the binary preamble: the 8-byte signature, the
    /// 4-byte little-endian header-length field, and the 4-byte reserved field.
    /// The XML header begins immediately after, at this offset.
    /// </summary>
    public const int PreambleSize = 16;

    /// <summary>
    /// The XML namespace declared by the root <c>xisf</c> element.
    /// </summary>
    public const string Namespace = "http://www.pixinsight.com/xisf";

    /// <summary>
    /// A strict UTF-8 decoder that throws on invalid bytes, so a header that is not
    /// valid UTF-8 is rejected rather than silently substituted.
    /// </summary>
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding( encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true );

    /// <summary>
    /// The raw UTF-8 XML header, as a string.
    /// </summary>
    /// <remarks>This is the verbatim header text; the parsed element tree is <see cref="Root"/>.</remarks>
    public string HeaderXml { get; }

    /// <summary>The root <c>xisf</c> element of the parsed XML header.</summary>
    internal XISFElement Root { get; }

    /// <summary>The parsed top-level properties backing store.</summary>
    private XISFProperty[] StoredProperties { get; }

    /// <summary>The parsed top-level FITS keywords backing store.</summary>
    private XISFFitsKeyword[] StoredKeywords { get; }

    /// <summary>The parsed images backing store.</summary>
    private XISFImage[] StoredImages { get; }

    /// <summary>
    /// The local names of the root's direct child elements, in document order (for
    /// example <c>Image</c>, <c>Property</c>, <c>FITSKeyword</c>).
    /// </summary>
    public IReadOnlyList< string > HeaderElementNames => this.Root.Children.Select( child => child.Name ).ToArray();

    /// <summary>
    /// The unit-level (top-level) properties, in document order, including
    /// data-block-backed vector, matrix and <c>ByteArray</c> values.
    /// </summary>
    /// <remarks>Each read returns a fresh snapshot so the internal state cannot be mutated.</remarks>
    public IReadOnlyList< XISFProperty > Properties => this.StoredProperties.ToArray();

    /// <summary>The unit-level (top-level) embedded FITS keywords, in document order.</summary>
    /// <remarks>Each read returns a fresh snapshot so the internal state cannot be mutated.</remarks>
    public IReadOnlyList< XISFFitsKeyword > Keywords => this.StoredKeywords.ToArray();

    /// <summary>The images contained in the unit, in document order.</summary>
    /// <remarks>Each read returns a fresh snapshot so the internal state cannot be mutated.</remarks>
    public IReadOnlyList< XISFImage > Images => this.StoredImages.ToArray();

    /// <summary>
    /// The unit-level metadata, or <c>null</c> if the header declares no
    /// <c>Metadata</c> element.
    /// </summary>
    public XISFMetadata? Metadata { get; }

    /// <summary>The first property whose identifier matches, or <c>null</c> if none does.</summary>
    /// <param name="id">The property identifier to look up.</param>
    /// <returns>The first matching property, or <c>null</c>.</returns>
    public XISFProperty? this[ string id ]
    {
        get
        {
            foreach( XISFProperty property in this.StoredProperties )
            {
                if( string.Equals( property.Id, id, StringComparison.Ordinal ) )
                {
                    return property;
                }
            }

            return null;
        }
    }

    /// <summary>The embedded FITS keywords with a given name, in document order.</summary>
    /// <remarks>
    /// A name may appear more than once (notably <c>HISTORY</c> and <c>COMMENT</c>),
    /// so every match is returned. Named <c>KeywordsNamed</c> rather than an overload
    /// of <see cref="Keywords"/> because a member cannot share the name of the
    /// property.
    /// </remarks>
    /// <param name="name">The keyword name to look up.</param>
    /// <returns>The matching keywords, in document order.</returns>
    public IReadOnlyList< XISFFitsKeyword > KeywordsNamed( string name )
    {
        return this.StoredKeywords.Where( keyword => string.Equals( keyword.Name, name, StringComparison.Ordinal ) ).ToArray();
    }

    /// <summary>Reads and parses an XISF file from a file-system path.</summary>
    /// <param name="path">The location of the file to read.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">
    /// <see cref="XISFErrorKind.InvalidFileUrl"/> if the path is missing or a
    /// directory, <see cref="XISFErrorKind.CannotReadFile"/> if the contents cannot
    /// be read, or any error raised while parsing the data.
    /// </exception>
    public XISFFile( string path, XISFParsingOptions options )
        : this( ReadFile( path ), Path.GetDirectoryName( path ), options )
    {
    }

    /// <summary>Parses an XISF file from raw bytes.</summary>
    /// <remarks>
    /// Because there is no originating file, <c>@header_dir</c> relative external
    /// data-block locations cannot be resolved; accessing such a block throws.
    /// </remarks>
    /// <param name="data">The complete file contents.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">
    /// <see cref="XISFErrorKind.DataError"/> if the data is empty,
    /// <see cref="XISFErrorKind.InvalidSignature"/> if the signature or reserved
    /// field is invalid, <see cref="XISFErrorKind.InvalidHeaderLength"/> if the
    /// header-length field is zero or extends past the end of the file,
    /// <see cref="XISFErrorKind.MalformedXml"/> if the header bytes are not valid
    /// UTF-8 or not well-formed XML, or <see cref="XISFErrorKind.InvalidElement"/>
    /// if the root element is not a valid <c>xisf</c> element.
    /// </exception>
    public XISFFile( ReadOnlyMemory< byte > data, XISFParsingOptions options )
        : this( data, null, options )
    {
    }

    /// <summary>
    /// Parses an XISF file from raw bytes, with an optional base directory for
    /// resolving <c>@header_dir</c> relative external data-block locations.
    /// </summary>
    /// <param name="data">The complete file contents.</param>
    /// <param name="baseDirectory">
    /// The directory of the header file, or <c>null</c> when opened from raw data.
    /// </param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">The same errors as <see cref="XISFFile(ReadOnlyMemory{byte}, XISFParsingOptions)"/>.</exception>
    internal XISFFile( ReadOnlyMemory< byte > data, string? baseDirectory, XISFParsingOptions options )
    {
        bool lenient = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );

        if( data.IsEmpty )
        {
            throw XISFException.DataError( "Data is empty" );
        }

        if( data.MatchesAscii( Signature, 0 ) == false )
        {
            throw XISFException.InvalidSignature( $"File does not start with the { Signature } signature" );
        }

        if( data.Length < PreambleSize )
        {
            throw XISFException.InvalidHeaderLength( $"File is smaller than the { PreambleSize.ToString( CultureInfo.InvariantCulture ) }-byte preamble" );
        }

        uint reserved = data.LittleEndianInteger< uint >( 12 );

        if( reserved != 0 && lenient == false )
        {
            throw XISFException.InvalidSignature( $"Reserved preamble field at offset 12 is not zero ({ reserved.ToString( CultureInfo.InvariantCulture ) })" );
        }

        uint headerLength = data.LittleEndianInteger< uint >( 8 );

        if( headerLength == 0 )
        {
            throw XISFException.InvalidHeaderLength( "Header length is zero" );
        }

        // Widened to 64-bit so the sum cannot overflow under the project's checked
        // arithmetic for a pathologically large declared length.
        if( ( long )PreambleSize + headerLength > data.Length )
        {
            throw XISFException.InvalidHeaderLength( $"XML header ({ headerLength.ToString( CultureInfo.InvariantCulture ) } bytes) extends past the end of the { data.Length.ToString( CultureInfo.InvariantCulture ) }-byte file" );
        }

        // The guard above proved the header fits within the file, so its length is at
        // most data.Length - PreambleSize and the narrowing to int is safe.
        ReadOnlyMemory< byte > headerData = data.Bytes( PreambleSize, ( int )headerLength );
        string                 headerXml;

        try
        {
            headerXml = StrictUtf8.GetString( headerData.Span );
        }
        catch( DecoderFallbackException )
        {
            throw XISFException.MalformedXml( "XML header is not valid UTF-8" );
        }

        XISFElement root = XISFXmlParser.Parse( headerXml );

        ValidateRoot( root, options );

        this.HeaderXml        = headerXml;
        this.Root             = root;
        this.StoredProperties = XISFProperty.ParseList( root, data, baseDirectory, options ).ToArray();
        this.StoredKeywords   = root.ChildrenNamed( "FITSKeyword" ).Select( element => new XISFFitsKeyword( element, options ) ).ToArray();
        this.StoredImages     = root.ChildrenNamed( "Image" ).Select( element => new XISFImage( element, data, baseDirectory, options ) ).ToArray();
        this.Metadata         = ParseMetadata( root, data, baseDirectory, options );
    }

    /// <summary>Reads a file's bytes, classifying a read failure into an XISF error.</summary>
    /// <remarks>
    /// The classification happens only after the read is attempted, so there is no
    /// time-of-check/time-of-use gap: a missing path or a directory is an invalid
    /// URL, anything else is an unreadable file.
    /// </remarks>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The complete file contents.</returns>
    /// <exception cref="XISFException">
    /// <see cref="XISFErrorKind.InvalidFileUrl"/> if the path is missing or a
    /// directory; otherwise <see cref="XISFErrorKind.CannotReadFile"/>.
    /// </exception>
    private static ReadOnlyMemory< byte > ReadFile( string path )
    {
        try
        {
            return File.ReadAllBytes( path );
        }
        catch( Exception exception ) when( exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException )
        {
            if( File.Exists( path ) == false || Directory.Exists( path ) )
            {
                throw XISFException.InvalidFileUrl( path );
            }

            throw XISFException.CannotReadFile( path );
        }
    }

    /// <summary>Parses the unit-level <c>Metadata</c> element, if present.</summary>
    /// <remarks>
    /// The <c>Metadata</c> element is optional: a header without one yields
    /// <c>null</c>. Under <see cref="XISFParsingOptions.AllowSpecDeviations"/> a
    /// malformed <c>Metadata</c> element is dropped (returns <c>null</c>) rather than
    /// failing the whole unit.
    /// </remarks>
    /// <param name="root">The root <c>xisf</c> element.</param>
    /// <param name="fileData">The complete file bytes, used to resolve data-block-backed metadata property values.</param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c>
    /// relative external data blocks; <c>null</c> when opened from raw data.
    /// </param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed metadata, or <c>null</c> if none is present (or a malformed one was dropped under lenient parsing).</returns>
    /// <exception cref="XISFException">Any error raised while parsing the metadata under strict parsing.</exception>
    private static XISFMetadata? ParseMetadata( XISFElement root, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        XISFElement? element = root.ChildrenNamed( "Metadata" ).FirstOrDefault();

        if( element is null )
        {
            return null;
        }

        try
        {
            return new XISFMetadata( element, fileData, baseDirectory, options );
        }
        catch( XISFException )
        {
            if( options.HasFlag( XISFParsingOptions.AllowSpecDeviations ) )
            {
                return null;
            }

            throw;
        }
    }

    /// <summary>Validates the root element of a parsed XISF header.</summary>
    /// <remarks>
    /// The root must be an <c>xisf</c> element in the XISF namespace (or in no
    /// namespace, tolerating headers that omit the declaration); an element in any
    /// other namespace is rejected. The <c>version</c> attribute must be <c>1.0</c>
    /// unless <see cref="XISFParsingOptions.AllowSpecDeviations"/> is set, which
    /// tolerates a missing or different version.
    /// </remarks>
    /// <param name="root">The root element to validate.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">
    /// <see cref="XISFErrorKind.InvalidElement"/> if the root is not a valid
    /// <c>xisf</c> element.
    /// </exception>
    private static void ValidateRoot( XISFElement root, XISFParsingOptions options )
    {
        if( string.Equals( root.Name, "xisf", StringComparison.Ordinal ) == false )
        {
            throw XISFException.InvalidElement( $"Expected root element 'xisf' but found '{ root.Name }'" );
        }

        if( root.NamespaceUri is { Length: > 0 } namespaceUri && string.Equals( namespaceUri, Namespace, StringComparison.Ordinal ) == false )
        {
            throw XISFException.InvalidElement( $"Root element 'xisf' is in an unexpected namespace: { namespaceUri }" );
        }

        if( options.HasFlag( XISFParsingOptions.AllowSpecDeviations ) )
        {
            return;
        }

        root.Attributes.TryGetValue( "version", out string? version );

        if( string.Equals( version, "1.0", StringComparison.Ordinal ) == false )
        {
            string found = version is null ? "no version attribute" : $"'{ version }'";

            throw XISFException.InvalidElement( $"Expected XISF version '1.0' but found { found }" );
        }
    }

    /// <summary>A multi-line, human-readable summary of the file.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $$"""
        XISFFile
        {
            Header: {{ this.HeaderXml.Length.ToString( CultureInfo.InvariantCulture ) }} characters
        }
        """;
    }
}
