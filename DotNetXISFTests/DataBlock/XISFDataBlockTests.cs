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
using System.Linq;
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for the in-file resolution of <see cref="XISFDataBlock"/>: inline,
/// embedded and attachment locations, the captured compression/checksum/byte-order
/// attributes, lazy decoding and checksum verification.
/// </summary>
public class XISFDataBlockTests
{
    /// <summary>Parses an XML fragment into the element carrying the data-block attributes.</summary>
    /// <param name="xml">The XML fragment.</param>
    /// <returns>The parsed root element.</returns>
    private static XISFElement Element( string xml ) => XISFXmlParser.Parse( xml );

    /// <summary>An <c>inline:base64</c> block decodes its character content.</summary>
    [ Fact ]
    public void ResolvesInlineBase64()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"inline:base64\">SGVsbG8=</Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Equal( "Hello"u8.ToArray(), block.RawBytes.ToArray() );
        Assert.Equal( XISFDataBlockLocation.Inline( XISFDataBlockLocation.Encoding.Base64 ), block.Location );
    }

    /// <summary>An <c>inline:hex</c> block decodes its character content.</summary>
    [ Fact ]
    public void ResolvesInlineHex()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"inline:hex\">48656c6c6f</Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Equal( "Hello"u8.ToArray(), block.RawBytes.ToArray() );
    }

    /// <summary>An <c>embedded</c> block decodes its <c>&lt;Data&gt;</c> child's content.</summary>
    [ Fact ]
    public void ResolvesEmbeddedBase64()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"embedded\"><Data encoding=\"base64\">SGVsbG8=</Data></Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Equal( "Hello"u8.ToArray(), block.RawBytes.ToArray() );
        Assert.Equal( XISFDataBlockLocation.Embedded(), block.Location );
    }

    /// <summary>An <c>embedded</c> block without a <c>&lt;Data&gt;</c> child is rejected.</summary>
    [ Fact ]
    public void RejectsEmbeddedWithoutDataChild()
    {
        Assert.Throws< XISFException >( () => new XISFDataBlock( Element( "<Image location=\"embedded\"/>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict ) );
    }

    /// <summary>An <c>embedded</c> <c>&lt;Data&gt;</c> child with an unknown encoding is rejected.</summary>
    [ Fact ]
    public void RejectsEmbeddedWithInvalidEncoding()
    {
        Assert.Throws< XISFException >( () => new XISFDataBlock( Element( "<Image location=\"embedded\"><Data encoding=\"base32\">xx</Data></Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict ) );
    }

    /// <summary>An <c>attachment</c> block slices the file bytes at its absolute offset.</summary>
    [ Fact ]
    public void ResolvesAttachmentSlice()
    {
        ReadOnlyMemory< byte > fileData = Enumerable.Range( 0, 16 ).Select( value => ( byte )value ).ToArray();
        XISFDataBlock          block    = new XISFDataBlock( Element( "<Image location=\"attachment:4:3\"/>" ), fileData, null, XISFParsingOptions.Strict );

        Assert.Equal( new byte[] { 0x04, 0x05, 0x06 }, block.RawBytes.ToArray() );
        Assert.Equal( XISFDataBlockLocation.Attachment( 4, 3 ), block.Location );
    }

    /// <summary>An <c>attachment</c> range extending past the file is rejected.</summary>
    [ Fact ]
    public void RejectsAttachmentOutOfRange()
    {
        ReadOnlyMemory< byte > fileData = Enumerable.Range( 0, 16 ).Select( value => ( byte )value ).ToArray();

        Assert.Throws< XISFException >( () => new XISFDataBlock( Element( "<Image location=\"attachment:10:20\"/>" ), fileData, null, XISFParsingOptions.Strict ) );
    }

    /// <summary>An element without a <c>location</c> attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingLocation()
    {
        Assert.Throws< XISFException >( () => new XISFDataBlock( Element( "<Image/>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict ) );
    }

    /// <summary>The block captures its raw compression, checksum and byte-order attributes.</summary>
    [ Fact ]
    public void CapturesRawAttributes()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"attachment:0:4\" compression=\"zlib:8\" checksum=\"sha-1:abcdef\" byteOrder=\"little\"/>" ), new byte[] { 1, 2, 3, 4 }, null, XISFParsingOptions.Strict );

        Assert.NotNull( block.Compression );
        Assert.Equal( XISFCompression.Codec.Zlib, block.Compression.Value.CompressionCodec );
        Assert.Equal( 8, block.Compression.Value.UncompressedSize );
        Assert.NotNull( block.Checksum );
        Assert.Equal( XISFChecksum.Algorithm.Sha1, block.Checksum.Value.ChecksumAlgorithm );
        Assert.Equal( "abcdef", block.Checksum.Value.Digest );
        Assert.Equal( "little", block.RawByteOrder );
    }

    /// <summary>An embedded block reads its compression from the <c>&lt;Data&gt;</c> child.</summary>
    [ Fact ]
    public void CapturesEmbeddedCompressionFromDataChild()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"embedded\"><Data encoding=\"base64\" compression=\"zlib:5\">SGVsbG8=</Data></Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.NotNull( block.Compression );
        Assert.Equal( 5, block.Compression.Value.UncompressedSize );
    }

    /// <summary>Accessing <c>Data</c> decompresses a compressed block.</summary>
    [ Fact ]
    public void DataDecompressesCompressedBlock()
    {
        string                 xml      = $"<Image location=\"inline:hex\" compression=\"zlib:132\">{ XISFCompressionTests.ZlibTextHex }</Image>";
        XISFDataBlock          block    = new XISFDataBlock( Element( xml ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );
        ReadOnlyMemory< byte > original = XISFCompressionTests.TextHex.XisfHexDecodedData();

        Assert.Equal( original.ToArray(), block.Data.ToArray() );
        Assert.NotNull( block.Compression );
        Assert.Equal( XISFCompression.Codec.Zlib, block.Compression.Value.CompressionCodec );
    }

    /// <summary>An uncompressed block's <c>Data</c> equals its raw bytes.</summary>
    [ Fact ]
    public void DataEqualsRawBytesWhenUncompressed()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"inline:hex\">deadbeef</Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Null( block.Compression );
        Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, block.Data.ToArray() );
    }

    /// <summary>A corrupt compressed stream throws on <c>Data</c> access.</summary>
    [ Fact ]
    public void DataThrowsOnCorruptCompressedStream()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"inline:hex\" compression=\"zlib:132\">789c010203</Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Throws< XISFException >( () => block.Data );
    }

    /// <summary>A matching checksum is verified on <c>Data</c> access when verification is enabled.</summary>
    [ Fact ]
    public void VerifiesChecksumOnDataAccessWhenEnabled()
    {
        string        xml   = $"<Image location=\"inline:hex\" checksum=\"sha-256:{ XISFChecksumTests.Sha256Hex }\">deadbeef</Image>";
        XISFDataBlock block = new XISFDataBlock( Element( xml ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.NotNull( block.Checksum );
        Assert.Equal( XISFChecksum.Algorithm.Sha256, block.Checksum.Value.ChecksumAlgorithm );
        Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, block.Data.ToArray() );
    }

    /// <summary>A mismatching checksum throws on <c>Data</c> access when verification is enabled.</summary>
    [ Fact ]
    public void ThrowsOnChecksumMismatchWhenEnabled()
    {
        string        xml   = "<Image location=\"inline:hex\" checksum=\"sha-256:0000000000000000000000000000000000000000000000000000000000000000\">deadbeef</Image>";
        XISFDataBlock block = new XISFDataBlock( Element( xml ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Throws< XISFException >( () => block.Data );
    }

    /// <summary>A mismatching checksum is skipped when verification is disabled.</summary>
    [ Fact ]
    public void SkipsChecksumWhenVerificationDisabled()
    {
        string        xml   = "<Image location=\"inline:hex\" checksum=\"sha-256:0000000000000000000000000000000000000000000000000000000000000000\">deadbeef</Image>";
        XISFDataBlock block = new XISFDataBlock( Element( xml ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Lenient );

        Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, block.Data.ToArray() );
    }

    /// <summary>The checksum is read from the <c>&lt;Data&gt;</c> child, not the parent.</summary>
    [ Fact ]
    public void VerifiesEmbeddedChecksumFromDataChild()
    {
        // A mismatching checksum on the <Data> child must be consulted (and fail),
        // proving the checksum is read from the child rather than the checksum-less
        // parent.
        string        xml   = "<Image location=\"embedded\"><Data encoding=\"hex\" checksum=\"sha-256:0000000000000000000000000000000000000000000000000000000000000000\">deadbeef</Data></Image>";
        XISFDataBlock block = new XISFDataBlock( Element( xml ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.NotNull( block.Checksum );
        Assert.Equal( XISFChecksum.Algorithm.Sha256, block.Checksum.Value.ChecksumAlgorithm );
        Assert.Throws< XISFException >( () => block.Data );
    }

    /// <summary>The description summarizes the block without reading its bytes.</summary>
    [ Fact ]
    public void Description()
    {
        XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"inline:hex\">deadbeef</Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

        Assert.Equal( "XISFDataBlock { location: inline:hex, compression: none, checksum: none, uncompressedSize: 4 }", block.ToString() );
    }

    /// <summary>The description formats the decoded size culture-invariantly.</summary>
    [ Fact ]
    public void DescriptionIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFDataBlock block = new XISFDataBlock( Element( "<Image location=\"inline:hex\" compression=\"zlib:6220800\">deadbeef</Image>" ), ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );

            Assert.Equal( "XISFDataBlock { location: inline:hex, compression: XISFCompression { zlib:6220800 }, checksum: none, uncompressedSize: 6220800 }", block.ToString() );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
