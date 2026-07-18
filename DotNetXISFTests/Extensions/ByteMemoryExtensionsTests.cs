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
using System.Text;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="ByteMemoryExtensions"/>.
/// </summary>
/// <remarks>
/// Covers the byte-level readers the binary parser relies on: bounds-checked
/// slicing, little-endian integer decoding, and ASCII-marker matching, including
/// their behavior on a re-based slice of a larger buffer.
/// </remarks>
public class ByteMemoryExtensionsTests
{
    /// <summary>
    /// <see cref="ByteMemoryExtensions.Bytes"/> returns the requested sub-range,
    /// accepts a zero-length range, and rejects out-of-bounds or negative
    /// arguments.
    /// </summary>
    [ Fact ]
    public void BytesAtOffset()
    {
        ReadOnlyMemory< byte > data = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };

        Assert.Equal( new byte[] { 0x10, 0x20 }, data.Bytes( 0, 2 ).ToArray() );
        Assert.Equal( new byte[] { 0x40, 0x50 }, data.Bytes( 3, 2 ).ToArray() );
        Assert.Empty( data.Bytes( 2, 0 ).ToArray() );

        Assert.Throws< XISFException >( () => data.Bytes( 4, 2 ) );
        Assert.Throws< XISFException >( () => data.Bytes( 5, 1 ) );
        Assert.Throws< XISFException >( () => data.Bytes( -1, 1 ) );
        Assert.Throws< XISFException >( () => data.Bytes( 0, -1 ) );
    }

    /// <summary>
    /// <see cref="ByteMemoryExtensions.Bytes"/> reads relative to the slice's own
    /// start, since slicing re-bases the memory to index zero.
    /// </summary>
    [ Fact ]
    public void BytesHandlesReBasedSlice()
    {
        ReadOnlyMemory< byte > full  = CreateSequence( 16 );
        ReadOnlyMemory< byte > slice = full.Slice( 8, 8 );

        Assert.Equal( new byte[] { 0x08, 0x09 }, slice.Bytes( 0, 2 ).ToArray() );
        Assert.Equal( new byte[] { 0x0E, 0x0F }, slice.Bytes( 6, 2 ).ToArray() );

        Assert.Throws< XISFException >( () => slice.Bytes( 7, 2 ) );
    }

    /// <summary>
    /// <see cref="ByteMemoryExtensions.LittleEndianInteger{ T }"/> decodes a
    /// fixed-width integer little-endian at the given offset, and rejects a read
    /// that runs past the end.
    /// </summary>
    [ Fact ]
    public void LittleEndianInteger()
    {
        ReadOnlyMemory< byte > data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF, 0xFF, 0xFF, 0xFF };

        Assert.Equal( 0x0403_0201u, data.LittleEndianInteger< uint >( 0 ) );
        Assert.Equal( ( ushort )0x0201, data.LittleEndianInteger< ushort >( 0 ) );
        Assert.Equal( 0xFFFF_FFFFu, data.LittleEndianInteger< uint >( 4 ) );

        Assert.Throws< XISFException >( () => data.LittleEndianInteger< uint >( 6 ) );
    }

    /// <summary>
    /// <see cref="ByteMemoryExtensions.LittleEndianInteger{ T }"/> reads relative
    /// to a slice's own start.
    /// </summary>
    [ Fact ]
    public void LittleEndianIntegerOnSlice()
    {
        ReadOnlyMemory< byte > full  = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04 };
        ReadOnlyMemory< byte > slice = full.Slice( 4, 4 );

        Assert.Equal( 0x0403_0201u, slice.LittleEndianInteger< uint >( 0 ) );
    }

    /// <summary>
    /// <see cref="ByteMemoryExtensions.MatchesAscii"/> matches the ASCII bytes of a
    /// string at an offset, and rejects a case, content or length mismatch.
    /// </summary>
    [ Fact ]
    public void MatchesAscii()
    {
        ReadOnlyMemory< byte > data = Encoding.UTF8.GetBytes( "XISF0100extra" );

        Assert.True( data.MatchesAscii( "XISF0100", 0 ) );
        Assert.True( data.MatchesAscii( "extra", 8 ) );
        Assert.False( data.MatchesAscii( "xisf0100", 0 ) );
        Assert.False( data.MatchesAscii( "XISF0101", 0 ) );
        Assert.False( data.MatchesAscii( "XISF0100extra!", 0 ) );
    }

    /// <summary>
    /// Builds a memory of <paramref name="count"/> bytes with each byte equal to
    /// its index.
    /// </summary>
    /// <param name="count">The number of bytes to produce.</param>
    /// <returns>The sequential byte memory.</returns>
    private static ReadOnlyMemory< byte > CreateSequence( int count )
    {
        byte[] bytes = new byte[ count ];

        for( int index = 0; index < count; index += 1 )
        {
            bytes[ index ] = ( byte )index;
        }

        return bytes;
    }
}
