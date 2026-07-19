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
using System.Globalization;

namespace DotNetXISF;

/// <summary>
/// Where a data block's bytes are stored, as declared by a <c>location</c>
/// attribute.
/// </summary>
/// <remarks>
/// This models the three in-file forms (<c>inline</c>, <c>embedded</c>,
/// <c>attachment</c>) and the external/distributed forms (<c>url(...)</c> and
/// <c>path(...)</c>, optionally with a <c>:index-id</c> into an XISF data blocks
/// file). The active case is the <see cref="LocationKind"/>; its payload is read
/// through the matching accessor. A default-constructed value is an
/// <c>inline:base64</c> location.
/// </remarks>
public readonly struct XISFDataBlockLocation : IEquatable< XISFDataBlockLocation >
{
    /// <summary>The active case discriminator of an <see cref="XISFDataBlockLocation"/>.</summary>
    public enum Kind
    {
        /// <summary>The bytes are the element's character content, in a given encoding.</summary>
        Inline,

        /// <summary>The bytes are a child <c>&lt;Data&gt;</c> element's character content.</summary>
        Embedded,

        /// <summary>The bytes are at an absolute offset within the monolithic file.</summary>
        Attachment,

        /// <summary>The bytes are an external resource at a URL.</summary>
        Url,

        /// <summary>The bytes are in a local file at an absolute path.</summary>
        AbsolutePath,

        /// <summary>The bytes are in a local file at a path relative to the header file's directory.</summary>
        HeaderRelativePath,
    }

    /// <summary>The text encoding of inline or embedded data-block bytes.</summary>
    public enum Encoding
    {
        /// <summary>Base64 encoding.</summary>
        Base64,

        /// <summary>Lowercase base16 (hexadecimal) encoding.</summary>
        Hex,
    }

    /// <summary>The header-relative-path marker prefix stripped from a <c>path(...)</c> resource.</summary>
    private const string HeaderDirectoryPrefix = "@header_dir/";

    /// <summary>The active case.</summary>
    private readonly Kind kind;

    /// <summary>The inline encoding (meaningful for <see cref="Kind.Inline"/>).</summary>
    private readonly Encoding encoding;

    /// <summary>The attachment position (meaningful for <see cref="Kind.Attachment"/>).</summary>
    private readonly long position;

    /// <summary>The attachment size (meaningful for <see cref="Kind.Attachment"/>).</summary>
    private readonly long size;

    /// <summary>The external URL (meaningful for <see cref="Kind.Url"/>).</summary>
    private readonly Uri? url;

    /// <summary>The external path (meaningful for <see cref="Kind.AbsolutePath"/>/<see cref="Kind.HeaderRelativePath"/>).</summary>
    private readonly string? path;

    /// <summary>The block index id into a data blocks file, or <c>null</c>.</summary>
    private readonly ulong? indexId;

    /// <summary>Initializes a location with the given case and payload slots.</summary>
    /// <param name="kind">The active case.</param>
    /// <param name="encoding">The inline encoding.</param>
    /// <param name="position">The attachment position.</param>
    /// <param name="size">The attachment size.</param>
    /// <param name="url">The external URL, or <c>null</c>.</param>
    /// <param name="path">The external path, or <c>null</c>.</param>
    /// <param name="indexId">The block index id, or <c>null</c>.</param>
    private XISFDataBlockLocation( Kind kind, Encoding encoding, long position, long size, Uri? url, string? path, ulong? indexId )
    {
        this.kind     = kind;
        this.encoding = encoding;
        this.position = position;
        this.size     = size;
        this.url      = url;
        this.path     = path;
        this.indexId  = indexId;
    }

    /// <summary>Creates an inline location.</summary>
    /// <param name="encoding">The text encoding of the inline bytes.</param>
    /// <returns>The created location.</returns>
    public static XISFDataBlockLocation Inline( Encoding encoding ) => new XISFDataBlockLocation( Kind.Inline, encoding, 0, 0, null, null, null );

    /// <summary>Creates an embedded location.</summary>
    /// <returns>The created location.</returns>
    public static XISFDataBlockLocation Embedded() => new XISFDataBlockLocation( Kind.Embedded, default, 0, 0, null, null, null );

    /// <summary>Creates an attachment location.</summary>
    /// <param name="position">The absolute byte position within the monolithic file.</param>
    /// <param name="size">The block size, in bytes.</param>
    /// <returns>The created location.</returns>
    public static XISFDataBlockLocation Attachment( long position, long size ) => new XISFDataBlockLocation( Kind.Attachment, default, position, size, null, null, null );

    /// <summary>Creates an external URL location.</summary>
    /// <param name="url">The external URL.</param>
    /// <param name="indexId">The block index id, or <c>null</c> for the whole file.</param>
    /// <returns>The created location.</returns>
    public static XISFDataBlockLocation Url( Uri url, ulong? indexId ) => new XISFDataBlockLocation( Kind.Url, default, 0, 0, url, null, indexId );

    /// <summary>Creates an absolute-path location.</summary>
    /// <param name="path">The absolute file path.</param>
    /// <param name="indexId">The block index id, or <c>null</c> for the whole file.</param>
    /// <returns>The created location.</returns>
    public static XISFDataBlockLocation AbsolutePath( string path, ulong? indexId ) => new XISFDataBlockLocation( Kind.AbsolutePath, default, 0, 0, null, path, indexId );

    /// <summary>Creates a header-relative-path location.</summary>
    /// <param name="path">The path relative to the header file's directory (without the <c>@header_dir/</c> prefix).</param>
    /// <param name="indexId">The block index id, or <c>null</c> for the whole file.</param>
    /// <returns>The created location.</returns>
    public static XISFDataBlockLocation HeaderRelativePath( string path, ulong? indexId ) => new XISFDataBlockLocation( Kind.HeaderRelativePath, default, 0, 0, null, path, indexId );

    /// <summary>The active case of this location.</summary>
    public Kind LocationKind => this.kind;

    /// <summary>
    /// Whether the block is stored outside the XISF header file (a <c>url(...)</c> or
    /// <c>path(...)</c> location).
    /// </summary>
    public bool IsExternal => this.kind is Kind.Url or Kind.AbsolutePath or Kind.HeaderRelativePath;

    /// <summary>The inline encoding, or <c>null</c> if this is not an inline location.</summary>
    public Encoding? InlineEncoding => this.kind == Kind.Inline ? this.encoding : null;

    /// <summary>
    /// The attachment position and size, or <c>null</c> if this is not an attachment
    /// location.
    /// </summary>
    public ( long Position, long Size )? AttachmentRange => this.kind == Kind.Attachment ? ( this.position, this.size ) : null;

    /// <summary>The external URL, or <c>null</c> if this is not a URL location.</summary>
    public Uri? ExternalUrl => this.kind == Kind.Url ? this.url : null;

    /// <summary>
    /// The external path, or <c>null</c> if this is not an absolute- or
    /// header-relative-path location. A header-relative path is without its
    /// <c>@header_dir/</c> prefix.
    /// </summary>
    public string? Path => this.kind is Kind.AbsolutePath or Kind.HeaderRelativePath ? this.path : null;

    /// <summary>
    /// The block index id into a data blocks file, or <c>null</c> when the external
    /// location is the whole file (or when the location is in-file).
    /// </summary>
    public ulong? IndexId => this.indexId;

    /// <summary>Parses a <c>location</c> attribute string.</summary>
    /// <remarks>
    /// The value is expected to be already XML-decoded, as it is when read from a
    /// parsed header: any parentheses that were escaped as character or entity
    /// references in the URL or path are literal <c>(</c> / <c>)</c> by the time they
    /// reach this method.
    /// </remarks>
    /// <param name="attribute">The XML-decoded <c>location</c> attribute value.</param>
    /// <returns>The parsed location.</returns>
    /// <exception cref="XISFException">
    /// The location is malformed (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    public static XISFDataBlockLocation FromAttribute( string attribute )
    {
        // External/distributed locations embed colons (and possibly literal
        // parentheses) inside parentheses, so parse them before splitting on ':'.
        if( attribute.StartsWith( "url(", StringComparison.Ordinal ) || attribute.StartsWith( "path(", StringComparison.Ordinal ) )
        {
            return ParseExternal( attribute );
        }

        string[] parts = attribute.Split( ':' );

        switch( parts[ 0 ] )
        {
            case "inline":

                if( parts.Length != 2 || EncodingFromName( parts[ 1 ] ) is not Encoding encoding )
                {
                    throw XISFException.DataBlockError( $"Invalid inline data-block location: '{ attribute }'" );
                }

                return Inline( encoding );

            case "embedded":

                if( parts.Length != 1 )
                {
                    throw XISFException.DataBlockError( $"Invalid embedded data-block location: '{ attribute }'" );
                }

                return Embedded();

            case "attachment":

                if( parts.Length != 3
                    || long.TryParse( parts[ 1 ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long position ) == false || position < 0
                    || long.TryParse( parts[ 2 ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long size ) == false || size < 0 )
                {
                    throw XISFException.DataBlockError( $"Invalid attachment data-block location: '{ attribute }'" );
                }

                return Attachment( position, size );

            default:

                throw XISFException.DataBlockError( $"Invalid data-block location: '{ attribute }'" );
        }
    }

    /// <summary>The <c>location</c> attribute form this location describes.</summary>
    /// <returns>The attribute-form string.</returns>
    public override string ToString()
    {
        return this.kind switch
        {
            Kind.Inline             => $"inline:{ EncodingName( this.encoding ) }",
            Kind.Embedded           => "embedded",
            Kind.Attachment         => $"attachment:{ this.position.ToString( CultureInfo.InvariantCulture ) }:{ this.size.ToString( CultureInfo.InvariantCulture ) }",
            Kind.Url                => $"url({ this.url?.AbsoluteUri ?? "" }){ this.IndexSuffix() }",
            Kind.AbsolutePath       => $"path({ this.path }){ this.IndexSuffix() }",
            Kind.HeaderRelativePath => $"path({ HeaderDirectoryPrefix }{ this.path }){ this.IndexSuffix() }",
            _                       => throw XISFException.DataBlockError( "Unknown data-block location" ),
        };
    }

    /// <summary>Returns whether this location equals another.</summary>
    /// <param name="other">The location to compare against.</param>
    /// <returns><c>true</c> if the locations describe the same case and payload.</returns>
    public bool Equals( XISFDataBlockLocation other )
    {
        if( this.kind != other.kind )
        {
            return false;
        }

        return this.kind switch
        {
            Kind.Inline             => this.encoding == other.encoding,
            Kind.Embedded           => true,
            Kind.Attachment         => this.position == other.position && this.size == other.size,
            Kind.Url                => Equals( this.url, other.url ) && this.indexId == other.indexId,
            Kind.AbsolutePath       => string.Equals( this.path, other.path, StringComparison.Ordinal ) && this.indexId == other.indexId,
            Kind.HeaderRelativePath => string.Equals( this.path, other.path, StringComparison.Ordinal ) && this.indexId == other.indexId,
            _                       => false,
        };
    }

    /// <summary>Returns whether this location equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal location.</returns>
    public override bool Equals( object? obj ) => obj is XISFDataBlockLocation other && this.Equals( other );

    /// <summary>A hash code combining the case and its payload.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        return this.kind switch
        {
            Kind.Inline             => HashCode.Combine( this.kind, this.encoding ),
            Kind.Embedded           => HashCode.Combine( this.kind ),
            Kind.Attachment         => HashCode.Combine( this.kind, this.position, this.size ),
            Kind.Url                => HashCode.Combine( this.kind, this.url, this.indexId ),
            Kind.AbsolutePath       => HashCode.Combine( this.kind, this.path, this.indexId ),
            Kind.HeaderRelativePath => HashCode.Combine( this.kind, this.path, this.indexId ),
            _                       => 0,
        };
    }

    /// <summary>Returns whether two locations are equal.</summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><c>true</c> if the locations are equal.</returns>
    public static bool operator ==( XISFDataBlockLocation left, XISFDataBlockLocation right ) => left.Equals( right );

    /// <summary>Returns whether two locations are unequal.</summary>
    /// <param name="left">The first location.</param>
    /// <param name="right">The second location.</param>
    /// <returns><c>true</c> if the locations are unequal.</returns>
    public static bool operator !=( XISFDataBlockLocation left, XISFDataBlockLocation right ) => left.Equals( right ) == false;

    /// <summary>Parses an external <c>url(...)</c> or <c>path(...)</c> location.</summary>
    /// <remarks>
    /// The resource is everything between the first <c>(</c> and the <em>last</em>
    /// <c>)</c> (so a URL or path may itself contain parentheses), optionally followed
    /// by <c>:index-id</c>. A <c>path(...)</c> beginning with <c>@header_dir/</c> is a
    /// header-relative path; any other <c>path(...)</c> is absolute.
    /// </remarks>
    /// <param name="attribute">The attribute value, which starts with <c>url(</c> or <c>path(</c>.</param>
    /// <returns>The parsed external location.</returns>
    /// <exception cref="XISFException">
    /// The parentheses are unbalanced, a URL is invalid, or the <c>index-id</c> is not
    /// an unsigned integer (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static XISFDataBlockLocation ParseExternal( string attribute )
    {
        bool   isUrl  = attribute.StartsWith( "url(", StringComparison.Ordinal );
        string prefix = isUrl ? "url(" : "path(";
        int    close  = attribute.LastIndexOf( ')' );

        if( close < 0 )
        {
            throw XISFException.DataBlockError( $"External data-block location is missing a closing parenthesis: '{ attribute }'" );
        }

        int open = prefix.Length;

        if( open > close )
        {
            throw XISFException.DataBlockError( $"Malformed external data-block location: '{ attribute }'" );
        }

        string resource = attribute[ open..close ];
        string trailing = attribute[ ( close + 1 ).. ];
        ulong? indexId  = ParseTrailingIndexId( trailing, attribute );

        if( isUrl )
        {
            if( Uri.TryCreate( resource, UriKind.Absolute, out Uri? parsedUrl ) == false )
            {
                throw XISFException.DataBlockError( $"Invalid data-block URL: '{ resource }'" );
            }

            return Url( parsedUrl, indexId );
        }

        if( resource.StartsWith( HeaderDirectoryPrefix, StringComparison.Ordinal ) )
        {
            return HeaderRelativePath( resource[ HeaderDirectoryPrefix.Length.. ], indexId );
        }

        return AbsolutePath( resource, indexId );
    }

    /// <summary>Parses the optional <c>:index-id</c> that may trail an external location.</summary>
    /// <param name="trailing">The substring after the closing parenthesis (empty, or <c>:index-id</c>).</param>
    /// <param name="attribute">The full attribute value, for error reporting.</param>
    /// <returns>The parsed index id, or <c>null</c> if none is present.</returns>
    /// <exception cref="XISFException">
    /// Trailing text is present but is not a valid <c>:index-id</c>
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static ulong? ParseTrailingIndexId( string trailing, string attribute )
    {
        if( trailing.Length == 0 )
        {
            return null;
        }

        if( trailing.StartsWith( ':' ) == false )
        {
            throw XISFException.DataBlockError( $"Unexpected trailing text in external data-block location: '{ attribute }'" );
        }

        string digits = trailing[ 1.. ];
        bool   isHex  = digits.StartsWith( "0x", StringComparison.Ordinal ) || digits.StartsWith( "0X", StringComparison.Ordinal );

        bool parsed = isHex
            ? ulong.TryParse( digits[ 2.. ], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value )
            : ulong.TryParse( digits, NumberStyles.None, CultureInfo.InvariantCulture, out value );

        if( parsed == false )
        {
            throw XISFException.DataBlockError( $"Invalid data-block index-id: '{ digits }'" );
        }

        return value;
    }

    /// <summary>The <c>:index-id</c> suffix (decimal) of an external location, or the empty string.</summary>
    /// <returns>The formatted suffix.</returns>
    private string IndexSuffix() => this.indexId is ulong id ? $":{ id.ToString( CultureInfo.InvariantCulture ) }" : "";

    /// <summary>Parses an inline/embedded encoding token.</summary>
    /// <param name="name">The encoding token, matched exactly (case-sensitive).</param>
    /// <returns>The matching encoding, or <c>null</c> when the token is not a known encoding.</returns>
    private static Encoding? EncodingFromName( string name )
    {
        return name switch
        {
            "base64" => Encoding.Base64,
            "hex"    => Encoding.Hex,
            _        => null,
        };
    }

    /// <summary>The spec token for an encoding.</summary>
    /// <param name="encoding">The encoding.</param>
    /// <returns>The spec token (<c>base64</c> or <c>hex</c>).</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined encoding (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static string EncodingName( Encoding encoding )
    {
        return encoding switch
        {
            Encoding.Base64 => "base64",
            Encoding.Hex    => "hex",
            _               => throw XISFException.DataBlockError( "Unknown data-block encoding" ),
        };
    }
}
