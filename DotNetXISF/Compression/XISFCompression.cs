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
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using K4os.Compression.LZ4;
using ZstdSharp;

namespace DotNetXISF;

/// <summary>
/// The compression applied to a data block, parsed from its <c>compression</c>
/// (and optional <c>subblocks</c>) attribute.
/// </summary>
/// <remarks>
/// An XISF <c>compression</c> attribute has the form
/// <c>&lt;codec&gt;[+sh]:&lt;uncompressed-size&gt;[:&lt;item-size&gt;]</c>, where
/// <c>+sh</c> marks byte-shuffled data and <c>&lt;item-size&gt;</c> is the shuffle
/// granularity. The optional <c>subblocks</c> attribute (<c>c1,u1:c2,u2:…</c>)
/// splits the stream into independently-compressed sub-blocks.
/// <para>
/// <see cref="Decompress"/> reverses the whole pipeline: it decodes each sub-block
/// (or the single stream), concatenates the results, validates the total length
/// against <see cref="UncompressedSize"/>, and reverses any byte-shuffling.
/// </para>
/// <para>
/// The <c>zlib</c> codec is decoded through the base class library
/// (<see cref="ZLibStream"/>, which consumes the RFC 1950 wrapper directly);
/// <c>lz4</c>/<c>lz4hc</c> and <c>zstd</c> are decoded through the managed
/// <c>K4os.Compression.LZ4</c> and <c>ZstdSharp.Port</c> packages, the two XISF
/// codecs the base class library does not provide.
/// </para>
/// </remarks>
public readonly struct XISFCompression : IEquatable< XISFCompression >
{
    /// <summary>A supported compression codec.</summary>
    public enum Codec
    {
        /// <summary>The zlib codec (RFC 1950 zlib-wrapped DEFLATE).</summary>
        Zlib,

        /// <summary>The LZ4 codec (raw LZ4 block format).</summary>
        Lz4,

        /// <summary>The LZ4-HC codec; its streams are decoded by the same LZ4 decoder.</summary>
        Lz4Hc,

        /// <summary>The Zstandard codec.</summary>
        Zstd,
    }

    /// <summary>One independently-compressed sub-block of a split-compression stream.</summary>
    public readonly struct Subblock : IEquatable< Subblock >
    {
        /// <summary>The size, in bytes, of this sub-block's compressed data.</summary>
        public int CompressedSize { get; }

        /// <summary>The size, in bytes, this sub-block decompresses to.</summary>
        public int UncompressedSize { get; }

        /// <summary>Creates a sub-block descriptor.</summary>
        /// <param name="compressedSize">The compressed size, in bytes.</param>
        /// <param name="uncompressedSize">The decompressed size, in bytes.</param>
        public Subblock( int compressedSize, int uncompressedSize )
        {
            this.CompressedSize   = compressedSize;
            this.UncompressedSize = uncompressedSize;
        }

        /// <summary>Returns whether this sub-block equals another.</summary>
        /// <param name="other">The sub-block to compare against.</param>
        /// <returns><c>true</c> if the sub-blocks are equal.</returns>
        public bool Equals( Subblock other ) => this.CompressedSize == other.CompressedSize && this.UncompressedSize == other.UncompressedSize;

        /// <summary>Returns whether this sub-block equals another object.</summary>
        /// <param name="obj">The object to compare against.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an equal sub-block.</returns>
        public override bool Equals( object? obj ) => obj is Subblock other && this.Equals( other );

        /// <summary>A hash code combining the compressed and uncompressed sizes.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => HashCode.Combine( this.CompressedSize, this.UncompressedSize );

        /// <summary>Returns whether two sub-blocks are equal.</summary>
        /// <param name="left">The first sub-block.</param>
        /// <param name="right">The second sub-block.</param>
        /// <returns><c>true</c> if the sub-blocks are equal.</returns>
        public static bool operator ==( Subblock left, Subblock right ) => left.Equals( right );

        /// <summary>Returns whether two sub-blocks are unequal.</summary>
        /// <param name="left">The first sub-block.</param>
        /// <param name="right">The second sub-block.</param>
        /// <returns><c>true</c> if the sub-blocks are unequal.</returns>
        public static bool operator !=( Subblock left, Subblock right ) => left.Equals( right ) == false;
    }

    /// <summary>The split-compression sub-blocks backing store, or <c>null</c> for a single stream.</summary>
    private readonly Subblock[]? subblocks;

    /// <summary>The codec used to compress the block.</summary>
    public Codec CompressionCodec { get; }

    /// <summary>The total size, in bytes, of the fully decompressed data.</summary>
    public int UncompressedSize { get; }

    /// <summary>The byte-shuffling item size, or <c>null</c> if the data is not shuffled.</summary>
    public int? ItemSize { get; }

    /// <summary>Whether the data is byte-shuffled.</summary>
    public bool UsesByteShuffling => this.ItemSize != null;

    /// <summary>
    /// The split-compression sub-blocks, in order, or <c>null</c> for a single
    /// compressed stream.
    /// </summary>
    /// <remarks>
    /// Each read returns a fresh snapshot, so the internal store can never be mutated
    /// through the returned list.
    /// </remarks>
    public IReadOnlyList< Subblock >? Subblocks => this.subblocks?.ToArray();

    /// <summary>Parses a <c>compression</c> attribute (and an optional <c>subblocks</c> attribute).</summary>
    /// <param name="attribute">
    /// The raw <c>compression</c> attribute value, of the form
    /// <c>&lt;codec&gt;[+sh]:&lt;uncompressed-size&gt;[:&lt;item-size&gt;]</c>.
    /// </param>
    /// <param name="subblocksAttribute">The raw <c>subblocks</c> attribute value, or <c>null</c>.</param>
    /// <exception cref="XISFException">
    /// The attribute is malformed or names an unknown codec
    /// (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    public XISFCompression( string attribute, string? subblocksAttribute = null )
    {
        string[] parts = attribute.Split( ':' );

        if( parts.Length < 2 )
        {
            throw XISFException.DecompressionError( $"Malformed compression attribute: '{ attribute }'" );
        }

        string rawCodecName   = parts[ 0 ];
        bool   isByteShuffled = rawCodecName.EndsWith( "+sh", StringComparison.Ordinal );
        string codecName      = isByteShuffled ? rawCodecName[ ..^3 ] : rawCodecName;

        Codec? codec = XISFCompressionCodecExtensions.FromName( codecName );

        if( codec.HasValue == false )
        {
            throw XISFException.DecompressionError( $"Unknown compression codec: '{ codecName }'" );
        }

        if( int.TryParse( parts[ 1 ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int uncompressedSize ) == false || uncompressedSize < 0 )
        {
            throw XISFException.DecompressionError( $"Invalid uncompressed size in compression attribute: '{ attribute }'" );
        }

        if( isByteShuffled )
        {
            if( parts.Length != 3 || int.TryParse( parts[ 2 ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int itemSize ) == false || itemSize <= 0 )
            {
                throw XISFException.DecompressionError( $"Byte-shuffled compression requires a positive item size: '{ attribute }'" );
            }

            this.ItemSize = itemSize;
        }
        else
        {
            if( parts.Length != 2 )
            {
                throw XISFException.DecompressionError( $"Unexpected components in compression attribute: '{ attribute }'" );
            }

            this.ItemSize = null;
        }

        this.CompressionCodec = codec.Value;
        this.UncompressedSize = uncompressedSize;
        this.subblocks        = subblocksAttribute == null ? null : ParseSubblocks( subblocksAttribute );
    }

    /// <summary>Decompresses a block's raw (as-stored) bytes into its final bytes.</summary>
    /// <remarks>
    /// Decodes each sub-block (or the single stream), concatenates them, validates
    /// the total length against <see cref="UncompressedSize"/>, and reverses any
    /// byte-shuffling.
    /// </remarks>
    /// <param name="input">The raw, as-stored compressed bytes.</param>
    /// <returns>The fully decompressed and un-shuffled bytes.</returns>
    /// <exception cref="XISFException">
    /// Decompression fails or the decompressed length does not match
    /// <see cref="UncompressedSize"/> (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    public ReadOnlyMemory< byte > Decompress( ReadOnlyMemory< byte > input )
    {
        ReadOnlyMemory< byte > decompressed = this.subblocks != null
            ? this.DecompressSubblocks( input, this.subblocks )
            : this.Decode( input, this.UncompressedSize );

        if( decompressed.Length != this.UncompressedSize )
        {
            throw XISFException.DecompressionError( $"Decompressed size { decompressed.Length.ToString( CultureInfo.InvariantCulture ) } does not match the declared size { this.UncompressedSize.ToString( CultureInfo.InvariantCulture ) }" );
        }

        if( this.ItemSize is int itemSize && itemSize > 1 )
        {
            return ByteUnshuffle( decompressed, itemSize );
        }

        return decompressed;
    }

    /// <summary>A single-line, human-readable summary of the compression.</summary>
    /// <returns>The summary string.</returns>
    public override string ToString()
    {
        string shuffle = this.ItemSize is int itemSize
            ? $"+sh:{ this.UncompressedSize.ToString( CultureInfo.InvariantCulture ) }:{ itemSize.ToString( CultureInfo.InvariantCulture ) }"
            : $":{ this.UncompressedSize.ToString( CultureInfo.InvariantCulture ) }";

        string subblockText = this.subblocks != null
            ? $", subblocks: { this.subblocks.Length.ToString( CultureInfo.InvariantCulture ) }"
            : "";

        return $"XISFCompression {{ { this.CompressionCodec.Name() }{ shuffle }{ subblockText } }}";
    }

    /// <summary>Returns whether this compression equals another.</summary>
    /// <param name="other">The compression to compare against.</param>
    /// <returns><c>true</c> if the codec, size, shuffle item size and sub-blocks all agree.</returns>
    public bool Equals( XISFCompression other )
    {
        return this.CompressionCodec == other.CompressionCodec
            && this.UncompressedSize == other.UncompressedSize
            && this.ItemSize == other.ItemSize
            && SubblocksEqual( this.subblocks, other.subblocks );
    }

    /// <summary>Returns whether this compression equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal compression.</returns>
    public override bool Equals( object? obj ) => obj is XISFCompression other && this.Equals( other );

    /// <summary>A hash code combining the codec, size, shuffle item size and sub-blocks.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        hash.Add( this.CompressionCodec );
        hash.Add( this.UncompressedSize );
        hash.Add( this.ItemSize );

        if( this.subblocks != null )
        {
            foreach( Subblock subblock in this.subblocks )
            {
                hash.Add( subblock );
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns whether two compressions are equal.</summary>
    /// <param name="left">The first compression.</param>
    /// <param name="right">The second compression.</param>
    /// <returns><c>true</c> if the compressions are equal.</returns>
    public static bool operator ==( XISFCompression left, XISFCompression right ) => left.Equals( right );

    /// <summary>Returns whether two compressions are unequal.</summary>
    /// <param name="left">The first compression.</param>
    /// <param name="right">The second compression.</param>
    /// <returns><c>true</c> if the compressions are unequal.</returns>
    public static bool operator !=( XISFCompression left, XISFCompression right ) => left.Equals( right ) == false;

    /// <summary>Parses a <c>subblocks</c> attribute (<c>c1,u1:c2,u2:…</c>).</summary>
    /// <param name="attribute">The raw <c>subblocks</c> attribute value.</param>
    /// <returns>The parsed sub-blocks, in order.</returns>
    /// <exception cref="XISFException">
    /// The attribute is empty or malformed (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    private static Subblock[] ParseSubblocks( string attribute )
    {
        if( attribute.Length == 0 )
        {
            throw XISFException.DecompressionError( "Empty subblocks attribute" );
        }

        string[]   pairs  = attribute.Split( ':' );
        Subblock[] result = new Subblock[ pairs.Length ];

        for( int index = 0; index < pairs.Length; index += 1 )
        {
            string[] numbers = pairs[ index ].Split( ',' );

            if( numbers.Length != 2
                || int.TryParse( numbers[ 0 ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int compressedSize ) == false || compressedSize < 0
                || int.TryParse( numbers[ 1 ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int uncompressedSize ) == false || uncompressedSize < 0 )
            {
                throw XISFException.DecompressionError( $"Malformed subblocks attribute: '{ attribute }'" );
            }

            result[ index ] = new Subblock( compressedSize, uncompressedSize );
        }

        return result;
    }

    /// <summary>Decompresses a split-compression stream, sub-block by sub-block.</summary>
    /// <param name="input">The concatenated compressed sub-block streams.</param>
    /// <param name="subblocks">The sub-block descriptors, in order.</param>
    /// <returns>The concatenated decompressed bytes.</returns>
    /// <exception cref="XISFException">
    /// A sub-block extends past the input, its total size overflows a buffer, or it
    /// fails to decompress to its declared size
    /// (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    private ReadOnlyMemory< byte > DecompressSubblocks( ReadOnlyMemory< byte > input, Subblock[] subblocks )
    {
        long total = subblocks.Aggregate( 0L, ( sum, subblock ) => sum + subblock.UncompressedSize );

        if( total > int.MaxValue )
        {
            throw XISFException.DecompressionError( "The total sub-block size exceeds the maximum buffer size" );
        }

        byte[] result    = new byte[ total ];
        long   inOffset  = 0;
        int    outOffset = 0;

        foreach( Subblock subblock in subblocks )
        {
            long end = inOffset + subblock.CompressedSize;

            if( end > input.Length )
            {
                throw XISFException.DecompressionError( "Sub-block extends past the end of the compressed data" );
            }

            ReadOnlyMemory< byte > slice   = input.Slice( ( int )inOffset, subblock.CompressedSize );
            ReadOnlyMemory< byte > decoded = this.Decode( slice, subblock.UncompressedSize );

            decoded.Span.CopyTo( result.AsSpan( outOffset ) );

            inOffset   = end;
            outOffset += decoded.Length;
        }

        return result;
    }

    /// <summary>Decodes a single compressed stream of a known decompressed size.</summary>
    /// <param name="input">The compressed bytes.</param>
    /// <param name="expectedSize">The expected decompressed size, in bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    /// <exception cref="XISFException">Decoding fails (<see cref="XISFErrorKind.DecompressionError"/>).</exception>
    private ReadOnlyMemory< byte > Decode( ReadOnlyMemory< byte > input, int expectedSize )
    {
        return this.CompressionCodec switch
        {
            Codec.Zlib               => InflateZlib( input, expectedSize ),
            Codec.Lz4 or Codec.Lz4Hc => DecodeLz4( input, expectedSize ),
            Codec.Zstd               => InflateZstd( input, expectedSize ),
            _                        => throw XISFException.DecompressionError( "Unknown compression codec" ),
        };
    }

    /// <summary>Decompresses an RFC 1950 zlib stream into a buffer of the expected size.</summary>
    /// <param name="input">The zlib-wrapped bytes.</param>
    /// <param name="expectedSize">The expected decompressed size, in bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    /// <exception cref="XISFException">The stream is corrupt or shorter than expected (<see cref="XISFErrorKind.DecompressionError"/>).</exception>
    private static ReadOnlyMemory< byte > InflateZlib( ReadOnlyMemory< byte > input, int expectedSize )
    {
        if( expectedSize <= 0 )
        {
            return ReadOnlyMemory< byte >.Empty;
        }

        byte[] output = new byte[ expectedSize ];

        try
        {
            using MemoryStream source = AsReadOnlyStream( input );
            using ZLibStream   stream = new ZLibStream( source, CompressionMode.Decompress );

            stream.ReadExactly( output );
        }
        catch( Exception exception ) when( exception is InvalidDataException or EndOfStreamException )
        {
            throw XISFException.DecompressionError( $"zlib decompression failed: { exception.Message }" );
        }

        return output;
    }

    /// <summary>Decodes a raw LZ4 block into a buffer of the expected size.</summary>
    /// <param name="input">The LZ4-compressed bytes.</param>
    /// <param name="expectedSize">The expected decompressed size, in bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    /// <exception cref="XISFException">
    /// The stream is empty, or the decoder writes a number of bytes other than
    /// <paramref name="expectedSize"/> (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    private static ReadOnlyMemory< byte > DecodeLz4( ReadOnlyMemory< byte > input, int expectedSize )
    {
        if( expectedSize <= 0 )
        {
            return ReadOnlyMemory< byte >.Empty;
        }

        if( input.Length == 0 )
        {
            throw XISFException.DecompressionError( "Cannot decompress an empty stream" );
        }

        byte[] output  = new byte[ expectedSize ];
        int    decoded = LZ4Codec.Decode( input.Span, output );

        if( decoded != expectedSize )
        {
            throw XISFException.DecompressionError( $"LZ4 decompression produced { decoded.ToString( CultureInfo.InvariantCulture ) } bytes, expected { expectedSize.ToString( CultureInfo.InvariantCulture ) }" );
        }

        return output;
    }

    /// <summary>Decompresses a Zstandard stream into a buffer of the expected size.</summary>
    /// <remarks>
    /// The decode is bounded to a destination of <paramref name="expectedSize"/>
    /// bytes, so a frame that would expand beyond it fails rather than allocating an
    /// unbounded buffer.
    /// </remarks>
    /// <param name="input">The zstd-compressed bytes.</param>
    /// <param name="expectedSize">The expected decompressed size, in bytes.</param>
    /// <returns>The decompressed bytes.</returns>
    /// <exception cref="XISFException">
    /// The stream is empty, the decoder reports an error, or the decompressed length
    /// differs from <paramref name="expectedSize"/>
    /// (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    private static ReadOnlyMemory< byte > InflateZstd( ReadOnlyMemory< byte > input, int expectedSize )
    {
        if( expectedSize <= 0 )
        {
            return ReadOnlyMemory< byte >.Empty;
        }

        if( input.Length == 0 )
        {
            throw XISFException.DecompressionError( "Cannot decompress an empty zstd stream" );
        }

        byte[] output = new byte[ expectedSize ];
        int    written;

        try
        {
            using Decompressor decompressor = new Decompressor();

            written = decompressor.Unwrap( input.Span, output );
        }
        catch( ZstdException exception )
        {
            throw XISFException.DecompressionError( $"zstd decompression failed: { exception.Message }" );
        }

        if( written != expectedSize )
        {
            throw XISFException.DecompressionError( $"zstd decompression produced { written.ToString( CultureInfo.InvariantCulture ) } bytes, expected { expectedSize.ToString( CultureInfo.InvariantCulture ) }" );
        }

        return output;
    }

    /// <summary>Reverses byte-shuffling for the given item size.</summary>
    /// <remarks>
    /// This is the inverse of the de-interleaving applied before compression: the
    /// planar byte stream is re-interleaved back into items of <paramref name="itemSize"/>
    /// bytes. Any trailing bytes that do not fill a whole item are left unchanged.
    /// </remarks>
    /// <param name="data">The shuffled (decompressed) bytes.</param>
    /// <param name="itemSize">The shuffle item size, in bytes.</param>
    /// <returns>The un-shuffled bytes.</returns>
    private static ReadOnlyMemory< byte > ByteUnshuffle( ReadOnlyMemory< byte > data, int itemSize )
    {
        if( itemSize <= 1 )
        {
            return data;
        }

        ReadOnlySpan< byte > bytes = data.Span;
        int                  count = bytes.Length;
        int                  items = count / itemSize;
        int                  main  = items * itemSize;

        byte[] result = new byte[ count ];

        for( int index = 0; index < main; index += 1 )
        {
            result[ index ] = bytes[ ( index % itemSize ) * items + ( index / itemSize ) ];
        }

        bytes[ main.. ].CopyTo( result.AsSpan( main ) );

        return result;
    }

    /// <summary>
    /// Wraps a read-only memory as a non-writable stream, without copying when the
    /// memory is backed by an array.
    /// </summary>
    /// <param name="memory">The memory to wrap.</param>
    /// <returns>A stream over the memory's bytes.</returns>
    private static MemoryStream AsReadOnlyStream( ReadOnlyMemory< byte > memory )
    {
        if( MemoryMarshal.TryGetArray( memory, out ArraySegment< byte > segment ) && segment.Array != null )
        {
            return new MemoryStream( segment.Array, segment.Offset, segment.Count, writable: false );
        }

        return new MemoryStream( memory.ToArray(), writable: false );
    }

    /// <summary>Returns whether two sub-block arrays are equal by content, treating <c>null</c> as a distinct value.</summary>
    /// <param name="left">The first array, or <c>null</c>.</param>
    /// <param name="right">The second array, or <c>null</c>.</param>
    /// <returns><c>true</c> if both are <c>null</c> or both have the same sub-blocks in order.</returns>
    private static bool SubblocksEqual( Subblock[]? left, Subblock[]? right )
    {
        if( left == null || right == null )
        {
            return left == null && right == null;
        }

        return left.AsSpan().SequenceEqual( right );
    }
}

/// <summary>
/// Spec-token parsing and formatting for <see cref="XISFCompression.Codec"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with <see cref="XISFCompression"/> as a single logical unit.
/// </remarks>
public static class XISFCompressionCodecExtensions
{
    /// <summary>Parses a codec from its XISF spec token.</summary>
    /// <remarks>The token is matched exactly (case-sensitive, ordinal).</remarks>
    /// <param name="name">The codec token (for example <c>lz4hc</c>).</param>
    /// <returns>The matching codec, or <c>null</c> when the token is not a known codec.</returns>
    public static XISFCompression.Codec? FromName( string name )
    {
        return name switch
        {
            "zlib"  => XISFCompression.Codec.Zlib,
            "lz4"   => XISFCompression.Codec.Lz4,
            "lz4hc" => XISFCompression.Codec.Lz4Hc,
            "zstd"  => XISFCompression.Codec.Zstd,
            _       => null,
        };
    }

    /// <summary>The XISF spec token for a codec.</summary>
    /// <param name="codec">The codec.</param>
    /// <returns>The spec token (for example <c>lz4hc</c>).</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined codec (<see cref="XISFErrorKind.DecompressionError"/>).
    /// </exception>
    public static string Name( this XISFCompression.Codec codec )
    {
        return codec switch
        {
            XISFCompression.Codec.Zlib  => "zlib",
            XISFCompression.Codec.Lz4   => "lz4",
            XISFCompression.Codec.Lz4Hc => "lz4hc",
            XISFCompression.Codec.Zstd  => "zstd",
            _                           => throw XISFException.DecompressionError( "Unknown compression codec" ),
        };
    }
}
