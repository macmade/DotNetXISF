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
using System.Threading;

namespace DotNetXISF;

/// <summary>
/// A resolved XISF data block: the bytes backing an image, a thumbnail, an ICC
/// profile, or a vector/matrix property value.
/// </summary>
/// <remarks>
/// <para>
/// For in-file locations (inline, embedded, attachment) the block's <em>raw</em>
/// (as-stored, possibly compressed) bytes are decoded eagerly during construction.
/// For external/distributed locations (<c>url(...)</c> / <c>path(...)</c>) resolution
/// is deferred: the external file is read only when <see cref="RawBytes"/> (or
/// <see cref="Data"/>) is first accessed, so opening a distributed unit does not read
/// resources that are never used. Both are exposed via <see cref="RawBytes"/>. The
/// fully decoded bytes — decompressed and byte-unshuffled per the block's
/// <see cref="Compression"/> — are available via <see cref="Data"/>, computed lazily
/// and cached (success and failure) on first access.
/// </para>
/// <para>
/// This is a reference type so <see cref="RawBytes"/> and <see cref="Data"/> can be
/// computed once and cached. Because that caching mutates on read, it is not
/// thread-safe: a block must not be read concurrently from multiple threads.
/// </para>
/// </remarks>
public sealed class XISFDataBlock
{
    /// <summary>Where the block's bytes are stored.</summary>
    public XISFDataBlockLocation Location { get; }

    /// <summary>The block's compression, or <c>null</c> if it is uncompressed.</summary>
    public XISFCompression? Compression { get; }

    /// <summary>The block's checksum, or <c>null</c> if it declares none.</summary>
    /// <remarks>
    /// When the parsing options request checksum verification, <see cref="Data"/>
    /// verifies this against <see cref="RawBytes"/> (the stored bytes) on first access.
    /// </remarks>
    public XISFChecksum? Checksum { get; }

    /// <summary>
    /// The raw <c>byteOrder</c> attribute, or <c>null</c> if unspecified. Interpreted
    /// when images are parsed.
    /// </summary>
    public string? RawByteOrder { get; }

    /// <summary>
    /// The parsing options applied, used to decide whether <see cref="Data"/> verifies
    /// the block's <see cref="Checksum"/> and whether external locations may be resolved.
    /// </summary>
    private XISFParsingOptions Options { get; }

    /// <summary>
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external locations, or <c>null</c> when the unit was opened from raw data (in which
    /// case relative external locations cannot be resolved).
    /// </summary>
    private string? BaseDirectory { get; }

    /// <summary>
    /// The eagerly-resolved in-file bytes (inline, embedded or attachment), or <c>null</c>
    /// for an external location whose bytes are resolved lazily.
    /// </summary>
    private ReadOnlyMemory< byte >? InFileBytes { get; }

    /// <summary>
    /// The block's raw (as-stored, possibly compressed) bytes: the in-file bytes, or — for
    /// an external location — the bytes read from the external resource, computed lazily and
    /// cached (success and failure) on first access.
    /// </summary>
    private Lazy< ReadOnlyMemory< byte > > StoredBytes { get; }

    /// <summary>
    /// The block's fully decoded bytes, computed lazily and cached (success and failure) on
    /// first access.
    /// </summary>
    private Lazy< ReadOnlyMemory< byte > > DecodedBytes { get; }

    /// <summary>
    /// The block's raw, as-stored bytes: decoded from the inline/embedded encoding, sliced
    /// from the attached region, or read from the external resource. These bytes are still
    /// compressed if the block declares a <see cref="Compression"/>.
    /// </summary>
    /// <remarks>
    /// For an external location this reads the external file on first access; the result,
    /// success or failure, is cached and not recomputed.
    /// </remarks>
    /// <exception cref="XISFException">
    /// An external location cannot be resolved — resolution disabled, missing base directory,
    /// unreadable or remote file, or a bad data-blocks-file index
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    public ReadOnlyMemory< byte > RawBytes => this.StoredBytes.Value;

    /// <summary>
    /// The size, in bytes, the block decodes to — its <see cref="Compression"/>'s declared
    /// uncompressed size, or the in-file byte count if the block is uncompressed.
    /// </summary>
    /// <remarks>
    /// This is <c>null</c> only for an uncompressed <em>external</em> block, whose size is
    /// not known without reading the external resource. It is always known without
    /// decompressing, so callers can validate an expected size cheaply.
    /// </remarks>
    public int? UncompressedSize => this.Compression?.UncompressedSize ?? this.InFileBytes?.Length;

    /// <summary>
    /// The block's fully decoded bytes: decompressed and byte-unshuffled per its
    /// <see cref="Compression"/>, or <see cref="RawBytes"/> if the block is uncompressed.
    /// </summary>
    /// <remarks>Computed once on first access and cached (success and failure).</remarks>
    /// <exception cref="XISFException">
    /// An external location cannot be resolved (<see cref="XISFErrorKind.DataBlockError"/>),
    /// checksum verification is enabled and fails
    /// (<see cref="XISFErrorKind.ChecksumMismatch"/>), or decompression fails
    /// (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    public ReadOnlyMemory< byte > Data => this.DecodedBytes.Value;

    /// <summary>Resolves a data block from an element that declares a <c>location</c>.</summary>
    /// <remarks>
    /// In-file locations (inline, embedded, attachment) are decoded eagerly; external
    /// locations (<c>url(...)</c> / <c>path(...)</c>) are validated here but their bytes are
    /// read lazily on first access to <see cref="RawBytes"/> / <see cref="Data"/>.
    /// </remarks>
    /// <param name="element">
    /// The element carrying the data-block attributes (and, for an embedded block, the
    /// <c>&lt;Data&gt;</c> child).
    /// </param>
    /// <param name="fileData">
    /// The complete file bytes, used to slice an <c>attachment</c> block by its absolute offset.
    /// </param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external locations; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">
    /// The <c>location</c> is missing or malformed, an embedded block lacks a valid
    /// <c>&lt;Data&gt;</c> child, or an attachment range is out of bounds
    /// (<see cref="XISFErrorKind.DataBlockError"/>); or inline/embedded content is not valid
    /// base64 or hexadecimal (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    internal XISFDataBlock( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        if( element.Attributes.TryGetValue( "location", out string? locationString ) == false )
        {
            throw XISFException.DataBlockError( "Data block is missing a 'location' attribute" );
        }

        XISFDataBlockLocation location = XISFDataBlockLocation.FromAttribute( locationString );

        this.Location      = location;
        this.BaseDirectory = baseDirectory;
        this.Options       = options;

        // The element that carries the data-block attributes: for an embedded block it is
        // the <Data> child, otherwise the element itself.
        XISFElement compressionSource;

        switch( location.LocationKind )
        {
            case XISFDataBlockLocation.Kind.Inline:

                this.InFileBytes  = Decode( element.Content, location.InlineEncoding ?? XISFDataBlockLocation.Encoding.Base64 );
                compressionSource = element;

                break;

            case XISFDataBlockLocation.Kind.Embedded:

                compressionSource = ReadEmbeddedData( element, out ReadOnlyMemory< byte > embeddedBytes );
                this.InFileBytes  = embeddedBytes;

                break;

            case XISFDataBlockLocation.Kind.Attachment:

                this.InFileBytes  = SliceAttachment( location, fileData );
                compressionSource = element;

                break;

            default:

                // External bytes are resolved lazily on first access.
                this.InFileBytes  = null;
                compressionSource = element;

                break;
        }

        IReadOnlyDictionary< string, string > sourceAttributes  = compressionSource.Attributes;
        IReadOnlyDictionary< string, string > elementAttributes = element.Attributes;

        string? Attribute( string name ) => sourceAttributes.GetValueOrDefault( name ) ?? elementAttributes.GetValueOrDefault( name );

        string? compressionAttribute = Attribute( "compression" );
        string? subblocksAttribute   = Attribute( "subblocks" );
        string? checksumAttribute    = Attribute( "checksum" );

        this.Compression  = compressionAttribute is null ? null : new XISFCompression( compressionAttribute, subblocksAttribute );
        this.RawByteOrder = Attribute( "byteOrder" );
        this.Checksum     = ParseChecksum( checksumAttribute, options );

        this.StoredBytes  = new Lazy< ReadOnlyMemory< byte > >( this.ResolveStoredBytes, LazyThreadSafetyMode.None );
        this.DecodedBytes = new Lazy< ReadOnlyMemory< byte > >( this.DecodeStoredBytes, LazyThreadSafetyMode.None );
    }

    /// <summary>A single-line, human-readable summary of the block.</summary>
    /// <remarks>
    /// Reports the location, compression and checksum without reading the block's bytes (so
    /// it never triggers external resolution or decompression); the decoded size is shown
    /// when it is known cheaply.
    /// </remarks>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        string compression = this.Compression?.ToString() ?? "none";
        string checksum    = this.Checksum?.ToString() ?? "none";
        string size        = this.UncompressedSize is int value ? value.ToString( CultureInfo.InvariantCulture ) : "unknown";

        return $"XISFDataBlock {{ location: { this.Location }, compression: { compression }, checksum: { checksum }, uncompressedSize: { size } }}";
    }

    /// <summary>Reads and decodes an embedded block's <c>&lt;Data&gt;</c> child.</summary>
    /// <param name="element">The element declaring the <c>embedded</c> location.</param>
    /// <param name="bytes">On return, the decoded child bytes.</param>
    /// <returns>The <c>&lt;Data&gt;</c> child element, which carries the compression attributes.</returns>
    /// <exception cref="XISFException">
    /// The <c>&lt;Data&gt;</c> child is missing or its <c>encoding</c> attribute is missing or
    /// invalid (<see cref="XISFErrorKind.DataBlockError"/>), or its content is not valid for
    /// the encoding (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    private static XISFElement ReadEmbeddedData( XISFElement element, out ReadOnlyMemory< byte > bytes )
    {
        IReadOnlyList< XISFElement > dataElements = element.ChildrenNamed( "Data" );

        if( dataElements.Count == 0 )
        {
            throw XISFException.DataBlockError( "Embedded data block is missing its <Data> child element" );
        }

        XISFElement dataElement = dataElements[ 0 ];

        if( dataElement.Attributes.TryGetValue( "encoding", out string? encodingString ) == false || XISFDataBlockLocation.EncodingFromName( encodingString ) is not XISFDataBlockLocation.Encoding encoding )
        {
            throw XISFException.DataBlockError( "Embedded <Data> element has a missing or invalid 'encoding' attribute" );
        }

        bytes = Decode( dataElement.Content, encoding );

        return dataElement;
    }

    /// <summary>Slices an attachment block's bytes from the whole-file buffer.</summary>
    /// <param name="location">The attachment location carrying the absolute position and size.</param>
    /// <param name="fileData">The complete file bytes.</param>
    /// <returns>The sliced block bytes (a view over <paramref name="fileData"/>).</returns>
    /// <exception cref="XISFException">
    /// The attachment range extends past the end of the file
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static ReadOnlyMemory< byte > SliceAttachment( XISFDataBlockLocation location, ReadOnlyMemory< byte > fileData )
    {
        ( long position, long size ) = location.AttachmentRange ?? ( 0, 0 );

        // The position and size are non-negative (validated when the location is parsed).
        // Comparing each against the file length before adding keeps the arithmetic from
        // overflowing under the project's checked-arithmetic setting for a hostile file.
        if( position > fileData.Length || size > fileData.Length - position )
        {
            throw XISFException.DataBlockError( $"Attachment range (position { position.ToString( CultureInfo.InvariantCulture ) }, size { size.ToString( CultureInfo.InvariantCulture ) }) is out of bounds for the { fileData.Length.ToString( CultureInfo.InvariantCulture ) }-byte file" );
        }

        return fileData.Bytes( ( int )position, ( int )size );
    }

    /// <summary>Parses the optional <c>checksum</c> attribute.</summary>
    /// <remarks>
    /// A checksum that fails to parse only matters when verification is requested; otherwise
    /// it is ignored rather than failing the parse.
    /// </remarks>
    /// <param name="attribute">The <c>checksum</c> attribute value, or <c>null</c> if absent.</param>
    /// <param name="options">The parsing options.</param>
    /// <returns>The parsed checksum, or <c>null</c> if absent (or unparseable while not verifying).</returns>
    /// <exception cref="XISFException">
    /// The checksum is malformed and verification is enabled
    /// (<see cref="XISFErrorKind.InvalidElement"/> / <see cref="XISFErrorKind.Unsupported"/>).
    /// </exception>
    private static XISFChecksum? ParseChecksum( string? attribute, XISFParsingOptions options )
    {
        if( attribute is null )
        {
            return null;
        }

        if( options.HasFlag( XISFParsingOptions.VerifyChecksums ) )
        {
            return new XISFChecksum( attribute );
        }

        try
        {
            return new XISFChecksum( attribute );
        }
        catch( XISFException )
        {
            return null;
        }
    }

    /// <summary>Resolves the block's raw, as-stored bytes.</summary>
    /// <remarks>
    /// Returns the eagerly-decoded in-file bytes, or — for an external location — reads the
    /// external resource, gated by <see cref="XISFParsingOptions.AllowExternalLocations"/>.
    /// </remarks>
    /// <returns>The raw, as-stored bytes.</returns>
    /// <exception cref="XISFException">
    /// External resolution is disabled, a <c>@header_dir</c> location has no base directory,
    /// the resource is remote or unreadable, or a data-blocks-file index lookup fails
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private ReadOnlyMemory< byte > ResolveStoredBytes()
    {
        if( this.InFileBytes is ReadOnlyMemory< byte > inFileBytes )
        {
            return inFileBytes;
        }

        if( this.Options.HasFlag( XISFParsingOptions.AllowExternalLocations ) == false )
        {
            throw XISFException.DataBlockError( $"External/distributed data-block resolution is disabled; enable XISFParsingOptions.AllowExternalLocations to read '{ this.Location }'" );
        }

        switch( this.Location.LocationKind )
        {
            case XISFDataBlockLocation.Kind.Url when this.Location.ExternalUrl is Uri url:

                return ReadExternalUrl( url, this.Location.IndexId );

            case XISFDataBlockLocation.Kind.AbsolutePath when this.Location.Path is string absolutePath:

                return ReadExternalFile( absolutePath, this.Location.IndexId );

            case XISFDataBlockLocation.Kind.HeaderRelativePath when this.Location.Path is string relativePath:

                if( this.BaseDirectory is not string baseDirectory )
                {
                    throw XISFException.DataBlockError( "A '@header_dir' relative data-block location requires opening the file from a URL, not from raw data" );
                }

                return ReadExternalFile( Path.Combine( baseDirectory, relativePath ), this.Location.IndexId );

            default:

                // Unreachable: an in-file location always has in-file bytes, and every external
                // case carries its resource.
                throw XISFException.DataBlockError( "Internal error: in-file data block has no bytes" );
        }
    }

    /// <summary>Reads an external data block from a URL, which must be a local file URL.</summary>
    /// <param name="url">The external URL. Remote (non-file) URLs are not supported.</param>
    /// <param name="indexId">The optional data-blocks-file block index identifier.</param>
    /// <returns>The raw bytes of the external block.</returns>
    /// <exception cref="XISFException">
    /// The URL is remote, the file cannot be read, or the index lookup fails
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static ReadOnlyMemory< byte > ReadExternalUrl( Uri url, ulong? indexId )
    {
        if( url.IsFile == false )
        {
            throw XISFException.DataBlockError( $"Remote external data blocks are not supported: { url.AbsoluteUri }" );
        }

        return ReadExternalFile( url.LocalPath, indexId );
    }

    /// <summary>Reads an external data block from a local file path.</summary>
    /// <remarks>
    /// When <paramref name="indexId"/> is <c>null</c> the block is the whole file; otherwise
    /// the file is an XISF data blocks file and the block is located through its block index.
    /// </remarks>
    /// <param name="path">The local file path of the external resource.</param>
    /// <param name="indexId">The optional data-blocks-file block index identifier.</param>
    /// <returns>The raw bytes of the external block.</returns>
    /// <exception cref="XISFException">
    /// The file cannot be read or the index lookup fails
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static ReadOnlyMemory< byte > ReadExternalFile( string path, ulong? indexId )
    {
        byte[] data;

        try
        {
            data = File.ReadAllBytes( path );
        }
        catch( Exception exception ) when ( exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException )
        {
            throw XISFException.DataBlockError( $"Cannot read external data-block file: { path }" );
        }

        if( indexId is ulong id )
        {
            return XISFDataBlocksFile.Block( id, data );
        }

        return data;
    }

    /// <summary>
    /// Decodes and (when required) verifies and decompresses the block's stored bytes.
    /// </summary>
    /// <remarks>
    /// The checksum covers the stored (as-on-disk, still-compressed) bytes, so it is verified
    /// before decompression, and only when requested.
    /// </remarks>
    /// <returns>The fully decoded bytes.</returns>
    /// <exception cref="XISFException">
    /// An external location cannot be resolved (<see cref="XISFErrorKind.DataBlockError"/>),
    /// checksum verification fails (<see cref="XISFErrorKind.ChecksumMismatch"/>), or
    /// decompression fails (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    private ReadOnlyMemory< byte > DecodeStoredBytes()
    {
        ReadOnlyMemory< byte > rawBytes = this.RawBytes;

        if( this.Options.HasFlag( XISFParsingOptions.VerifyChecksums ) && this.Checksum is XISFChecksum checksum )
        {
            checksum.Verify( rawBytes );
        }

        if( this.Compression is XISFCompression compression )
        {
            return compression.Decompress( rawBytes );
        }

        return rawBytes;
    }

    /// <summary>Decodes inline/embedded text into bytes.</summary>
    /// <param name="text">The encoded character content.</param>
    /// <param name="encoding">The declared encoding.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="XISFException">
    /// The text is not valid for the encoding (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    private static ReadOnlyMemory< byte > Decode( string text, XISFDataBlockLocation.Encoding encoding )
    {
        return encoding switch
        {
            XISFDataBlockLocation.Encoding.Base64 => text.XisfBase64DecodedData(),
            XISFDataBlockLocation.Encoding.Hex    => text.XisfHexDecodedData(),
            _                                     => throw XISFException.DataError( "Unknown data-block encoding" ),
        };
    }
}
