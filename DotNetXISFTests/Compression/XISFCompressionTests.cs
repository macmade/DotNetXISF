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
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFCompression"/>: attribute and sub-block parsing,
/// and decompression round-trips against genuine XISF-format compressed fixtures
/// produced by real third-party encoders (so these prove interoperability, not
/// merely an encode/decode round-trip).
/// </summary>
public class XISFCompressionTests
{
    /// <summary>The reference text payload, hex-encoded (132 bytes).</summary>
    internal const string TextHex = "5849534620636f6d7072657373696f6e20726f756e642d7472697020746573743a2074686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f6720303132333435363738392074686520717569636b2062726f776e20666f78206a756d7073206f76657220746865206c617a7920646f67";

    /// <summary>The reference payload compressed with python <c>zlib</c> (RFC 1950).</summary>
    internal const string ZlibTextHex = "789c8bf00c765348cecf2d284a2d2ececccf5328ca2fcd4bd12d29ca2c5028492d2eb15228c94855282ccd4cce56482aca2fcf5348cbaf50c82acd2d2856c82f4b2d024be72456552aa4e4a72b18181a199b989a995b5892a20d0044f02eba";

    /// <summary>The 32-byte shuffle payload zlib-compressed after forward-shuffling at item size 2.</summary>
    private const string ZlibSh2Hex = "789c63606261e3e0e2e1131012119390929163646665e7e4e6e5171416159794969507001a4801f1";

    /// <summary>The 32-byte shuffle payload zlib-compressed after forward-shuffling at item size 4.</summary>
    private const string ZlibSh4Hex = "789c6360e1e0111091906164e5e4151495946562e3e2131293926366e7e61716979607001c3801f1";

    /// <summary>The reference payload compressed as a raw LZ4 block.</summary>
    private const string Lz4TextHex = "f1315849534620636f6d7072657373696f6e20726f756e642d7472697020746573743a2074686520717569636b2062726f776e20666f78206a756d7073206f7665721f00f1046c617a7920646f67203031323334353637383918000f37000f507920646f67";

    /// <summary>The reference payload compressed as a raw LZ4-HC block.</summary>
    private const string Lz4HcTextHex = "f1315849534620636f6d7072657373696f6e20726f756e642d7472697020746573743a2074686520717569636b2062726f776e20666f78206a756d7073206f7665721f00ff046c617a7920646f672030313233343536373839370014507920646f67";

    /// <summary>The 32-byte shuffle payload LZ4-compressed after forward-shuffling at item size 2.</summary>
    private const string Lz4Sh2Hex = "f01100020406080a0c0e10121416181a1c1e01030507090b0d0f11131517191b1d1f";

    /// <summary>The reference payload zlib-compressed as two independently-compressed sub-blocks.</summary>
    private const string SubblocksHex = "789c05c1c10d80300805d055fe022ee000269ebd78b662c4a68040d5f17d6f9d9709459b3945b00a5cbbec433a1b922247e449b83b978acdf5151cfae1eacd02fa90237f3080186d789ccb4855c849acaa5448c94f573030343236313533b7b05428c94855282ccd4cce56482aca2fcf5348cbaf50c82acd2d2856c82f4b2d024bc3b40100c82f164e";

    /// <summary>The reference payload compressed with the upstream zstd CLI (v1.5.7).</summary>
    private const string ZstdTextHex = "28b52ffd0458f5020034055849534620636f6d7072657373696f6e20726f756e642d7472697020746573743a2074686520717569636b2062726f776e20666f78206a756d7073206f7665726c617a7920646f6720303132333435363738390200d3ca0440082a0a4ebdb5a9";

    /// <summary>The 32-byte shuffle payload zstd-compressed after forward-shuffling at item size 2.</summary>
    private const string ZstdSh2Hex = "28b52ffd045801010000020406080a0c0e10121416181a1c1e01030507090b0d0f11131517191b1d1f2df1f06c";

    /// <summary>The 32-byte shuffle payload: the bytes 0 through 31.</summary>
    private static readonly byte[] ShufflePayload = Enumerable.Range( 0, 32 ).Select( value => ( byte )value ).ToArray();

    /// <summary>Hex-decodes a fixture string into its bytes.</summary>
    /// <param name="hex">The hexadecimal fixture.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] Decode( string hex ) => hex.XisfHexDecodedData().ToArray();

    /// <summary>A plain codec parses its codec and uncompressed size, with no shuffle or sub-blocks.</summary>
    [ Fact ]
    public void ParsesPlainCodec()
    {
        XISFCompression compression = new XISFCompression( "zlib:6220800" );

        Assert.Equal( XISFCompression.Codec.Zlib, compression.CompressionCodec );
        Assert.Equal( 6220800, compression.UncompressedSize );
        Assert.Null( compression.ItemSize );
        Assert.Null( compression.Subblocks );
    }

    /// <summary>A shuffled codec parses its item size.</summary>
    [ Fact ]
    public void ParsesShuffledCodec()
    {
        XISFCompression compression = new XISFCompression( "zlib+sh:6220800:2" );

        Assert.Equal( XISFCompression.Codec.Zlib, compression.CompressionCodec );
        Assert.Equal< int? >( 2, compression.ItemSize );
        Assert.True( compression.UsesByteShuffling );
    }

    /// <summary>The LZ4 and LZ4-HC codec spellings, plain and shuffled, are parsed.</summary>
    [ Fact ]
    public void ParsesLz4Variants()
    {
        Assert.Equal( XISFCompression.Codec.Lz4,   new XISFCompression( "lz4:10" ).CompressionCodec );
        Assert.Equal( XISFCompression.Codec.Lz4Hc, new XISFCompression( "lz4hc:10" ).CompressionCodec );
        Assert.Equal< int? >( 4, new XISFCompression( "lz4+sh:10:4" ).ItemSize );
        Assert.Equal( XISFCompression.Codec.Lz4Hc, new XISFCompression( "lz4hc+sh:10:4" ).CompressionCodec );
    }

    /// <summary>A sub-blocks attribute parses into its compressed/uncompressed size pairs, in order.</summary>
    [ Fact ]
    public void ParsesSubblocks()
    {
        XISFCompression compression = new XISFCompression( "zlib:132", "72,66:65,66" );

        IReadOnlyList< XISFCompression.Subblock >? subblocks = compression.Subblocks;

        Assert.NotNull( subblocks );
        Assert.Equal( 2, subblocks.Count );
        Assert.Equal( 72, subblocks[ 0 ].CompressedSize );
        Assert.Equal( 66, subblocks[ 0 ].UncompressedSize );
        Assert.Equal( 65, subblocks[ 1 ].CompressedSize );
        Assert.Equal( 66, subblocks[ 1 ].UncompressedSize );
    }

    /// <summary>An unknown codec is rejected.</summary>
    [ Fact ]
    public void RejectsUnknownCodec()
    {
        Assert.Throws< XISFException >( () => new XISFCompression( "lzw:10" ) );
    }

    /// <summary>The zstd codec spelling, plain and shuffled, is parsed.</summary>
    [ Fact ]
    public void ParsesZstdCodec()
    {
        Assert.Equal( XISFCompression.Codec.Zstd, new XISFCompression( "zstd:10" ).CompressionCodec );
        Assert.Equal( XISFCompression.Codec.Zstd, new XISFCompression( "zstd+sh:10:2" ).CompressionCodec );
        Assert.Equal< int? >( 2, new XISFCompression( "zstd+sh:10:2" ).ItemSize );
    }

    /// <summary>Malformed compression attributes are rejected.</summary>
    [ Fact ]
    public void RejectsMalformedAttributes()
    {
        Assert.Throws< XISFException >( () => new XISFCompression( "zlib" ) );
        Assert.Throws< XISFException >( () => new XISFCompression( "zlib:" ) );
        Assert.Throws< XISFException >( () => new XISFCompression( "zlib:abc" ) );
        Assert.Throws< XISFException >( () => new XISFCompression( "zlib+sh:10" ) );
        Assert.Throws< XISFException >( () => new XISFCompression( "zlib+sh:10:abc" ) );
        Assert.Throws< XISFException >( () => new XISFCompression( "" ) );
    }

    /// <summary>A zlib stream decompresses to the original payload.</summary>
    [ Fact ]
    public void DecompressesZlib()
    {
        Assert.Equal( Decode( TextHex ), new XISFCompression( "zlib:132" ).Decompress( Decode( ZlibTextHex ) ).ToArray() );
    }

    /// <summary>A raw LZ4 block decompresses to the original payload.</summary>
    [ Fact ]
    public void DecompressesLz4()
    {
        Assert.Equal( Decode( TextHex ), new XISFCompression( "lz4:132" ).Decompress( Decode( Lz4TextHex ) ).ToArray() );
    }

    /// <summary>A raw LZ4-HC block decompresses to the original payload through the same decoder.</summary>
    [ Fact ]
    public void DecompressesLz4Hc()
    {
        Assert.Equal( Decode( TextHex ), new XISFCompression( "lz4hc:132" ).Decompress( Decode( Lz4HcTextHex ) ).ToArray() );
    }

    /// <summary>A zstd stream decompresses to the original payload.</summary>
    [ Fact ]
    public void DecompressesZstd()
    {
        Assert.Equal( Decode( TextHex ), new XISFCompression( "zstd:132" ).Decompress( Decode( ZstdTextHex ) ).ToArray() );
    }

    /// <summary>A byte-shuffled zstd stream decompresses and un-shuffles to the shuffle payload.</summary>
    [ Fact ]
    public void DecompressesZstdWithByteShuffling()
    {
        Assert.Equal( ShufflePayload, new XISFCompression( "zstd+sh:32:2" ).Decompress( Decode( ZstdSh2Hex ) ).ToArray() );
    }

    /// <summary>A byte-shuffled zlib stream un-shuffles at item size 2.</summary>
    [ Fact ]
    public void DecompressesZlibWithByteShufflingItemSize2()
    {
        Assert.Equal( ShufflePayload, new XISFCompression( "zlib+sh:32:2" ).Decompress( Decode( ZlibSh2Hex ) ).ToArray() );
    }

    /// <summary>A byte-shuffled zlib stream un-shuffles at item size 4.</summary>
    [ Fact ]
    public void DecompressesZlibWithByteShufflingItemSize4()
    {
        Assert.Equal( ShufflePayload, new XISFCompression( "zlib+sh:32:4" ).Decompress( Decode( ZlibSh4Hex ) ).ToArray() );
    }

    /// <summary>A byte-shuffled LZ4 block un-shuffles at item size 2.</summary>
    [ Fact ]
    public void DecompressesLz4WithByteShuffling()
    {
        Assert.Equal( ShufflePayload, new XISFCompression( "lz4+sh:32:2" ).Decompress( Decode( Lz4Sh2Hex ) ).ToArray() );
    }

    /// <summary>A split-compression stream decompresses sub-block by sub-block into the original payload.</summary>
    [ Fact ]
    public void DecompressesSubblocks()
    {
        Assert.Equal( Decode( TextHex ), new XISFCompression( "zlib:132", "72,66:65,66" ).Decompress( Decode( SubblocksHex ) ).ToArray() );
    }

    /// <summary>A short or corrupt stream fails to decompress.</summary>
    [ Fact ]
    public void ThrowsOnShortOrCorruptStream()
    {
        XISFCompression compression = new XISFCompression( "zlib:132" );
        byte[]          garbage     = { 0x78, 0x9C, 0x01, 0x02, 0x03 };

        Assert.Throws< XISFException >( () => compression.Decompress( garbage ) );
    }

    /// <summary>A decompressed size that differs from the declared size is rejected.</summary>
    [ Fact ]
    public void ThrowsWhenDecompressedSizeDiffersFromDeclared()
    {
        XISFCompression compression = new XISFCompression( "zlib:200" );

        Assert.Throws< XISFException >( () => compression.Decompress( Decode( ZlibTextHex ) ) );
    }

    /// <summary>The description is a single-line, human-readable summary.</summary>
    [ Fact ]
    public void Description()
    {
        Assert.Equal( "XISFCompression { zlib:6220800 }", new XISFCompression( "zlib:6220800" ).ToString() );
        Assert.Equal( "XISFCompression { zlib+sh:6220800:2 }", new XISFCompression( "zlib+sh:6220800:2" ).ToString() );
        Assert.Equal( "XISFCompression { zlib:132, subblocks: 2 }", new XISFCompression( "zlib:132", "72,66:65,66" ).ToString() );
    }

    /// <summary>Two compressions are equal when their codec, size, shuffle and sub-blocks agree.</summary>
    [ Fact ]
    public void EqualityComparesAllComponents()
    {
        XISFCompression a = new XISFCompression( "zlib+sh:100:2" );
        XISFCompression b = new XISFCompression( "zlib+sh:100:2" );
        XISFCompression c = new XISFCompression( "zlib+sh:100:4" );

        Assert.Equal( a, b );
        Assert.Equal( a.GetHashCode(), b.GetHashCode() );
        Assert.NotEqual( a, c );
        Assert.NotEqual( new XISFCompression( "zlib:132" ), new XISFCompression( "zlib:132", "72,66:65,66" ) );
    }

    /// <summary>
    /// A default-constructed compression (which C# always permits for a struct) is
    /// safe: it decompresses an empty input to empty and none of its members throw.
    /// </summary>
    [ Fact ]
    public void DefaultCompressionIsSafe()
    {
        XISFCompression compression = default;

        Assert.Equal( XISFCompression.Codec.Zlib, compression.CompressionCodec );
        Assert.Equal( 0, compression.UncompressedSize );
        Assert.Null( compression.ItemSize );
        Assert.Null( compression.Subblocks );
        Assert.False( compression.UsesByteShuffling );
        Assert.Empty( compression.Decompress( ReadOnlyMemory< byte >.Empty ).ToArray() );
        Assert.Equal( "XISFCompression { zlib:0 }", compression.ToString() );
        Assert.Equal( compression, default( XISFCompression ) );
    }

    /// <summary>
    /// Attribute parsing and the description are culture-invariant: a large size
    /// parses and formats identically under a culture whose number format differs
    /// from the invariant culture (a thousands separator would break it).
    /// </summary>
    [ Fact ]
    public void FormattingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFCompression compression = new XISFCompression( "zlib+sh:6220800:2" );

            Assert.Equal( 6220800, compression.UncompressedSize );
            Assert.Equal< int? >( 2, compression.ItemSize );
            Assert.Equal( "XISFCompression { zlib+sh:6220800:2 }", compression.ToString() );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
