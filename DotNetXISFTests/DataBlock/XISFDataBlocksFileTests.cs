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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFDataBlocksFile"/>: resolving a data block by its
/// block index element identifier, and rejecting a bad signature, an unknown id, a
/// truncated node, a cyclic index and an out-of-bounds block range.
/// </summary>
public class XISFDataBlocksFileTests
{
    /// <summary>A block is resolved by its index id from a well-formed data blocks file.</summary>
    [ Fact ]
    public void ResolvesBlockById()
    {
        byte[]                 payload = { 0x11, 0x22, 0x33, 0x44 };
        ReadOnlyMemory< byte > file    = TestUtilities.DataBlocksFile( 0x2A, payload );

        Assert.Equal( payload, XISFDataBlocksFile.Block( 0x2A, file ).ToArray() );
    }

    /// <summary>A file without the <c>XISB0100</c> signature is rejected.</summary>
    [ Fact ]
    public void RejectsBadSignature()
    {
        ReadOnlyMemory< byte > file = new byte[ 32 ];

        Assert.Throws< XISFException >( () => XISFDataBlocksFile.Block( 0x2A, file ) );
    }

    /// <summary>An identifier not present in the index is rejected.</summary>
    [ Fact ]
    public void RejectsUnknownId()
    {
        ReadOnlyMemory< byte > file = TestUtilities.DataBlocksFile( 0x2A, new byte[] { 0x11 } );

        Assert.Throws< XISFException >( () => XISFDataBlocksFile.Block( 0x99, file ) );
    }

    /// <summary>A node claiming an element that the file does not contain is rejected.</summary>
    [ Fact ]
    public void RejectsTruncatedElement()
    {
        List< byte > bytes = new List< byte >();

        bytes.AddRange( Encoding.ASCII.GetBytes( "XISB0100" ) );
        bytes.AddRange( new byte[ 8 ] );      // reserved
        bytes.AddRange( Le32( 1 ) );          // element count = 1
        bytes.AddRange( new byte[ 4 ] );      // reserved
        bytes.AddRange( Le64( 0 ) );          // next node = none

        // The node claims one element, but no element bytes follow.
        Assert.Throws< XISFException >( () => XISFDataBlocksFile.Block( 0x2A, ( ReadOnlyMemory< byte > )bytes.ToArray() ) );
    }

    /// <summary>A block index whose node chain forms a cycle is rejected.</summary>
    [ Fact ]
    public void DetectsCyclicIndex()
    {
        List< byte > bytes = new List< byte >();

        bytes.AddRange( Encoding.ASCII.GetBytes( "XISB0100" ) );
        bytes.AddRange( new byte[ 8 ] );      // reserved

        // First node (position 16): no elements, next node points to position 32.
        bytes.AddRange( Le32( 0 ) );          // element count = 0
        bytes.AddRange( new byte[ 4 ] );      // reserved
        bytes.AddRange( Le64( 32 ) );         // next node = 32

        // Second node (position 32): no elements, next node points back to 16.
        bytes.AddRange( Le32( 0 ) );          // element count = 0
        bytes.AddRange( new byte[ 4 ] );      // reserved
        bytes.AddRange( Le64( 16 ) );         // next node = 16 (cycle)

        Assert.Throws< XISFException >( () => XISFDataBlocksFile.Block( 0x2A, ( ReadOnlyMemory< byte > )bytes.ToArray() ) );
    }

    /// <summary>A block index element whose block range falls outside the file is rejected.</summary>
    [ Fact ]
    public void RejectsOutOfBoundsBlock()
    {
        List< byte > bytes = new List< byte >();

        bytes.AddRange( Encoding.ASCII.GetBytes( "XISB0100" ) );
        bytes.AddRange( new byte[ 8 ] );      // reserved
        bytes.AddRange( Le32( 1 ) );          // element count = 1
        bytes.AddRange( new byte[ 4 ] );      // reserved
        bytes.AddRange( Le64( 0 ) );          // next node = none
        bytes.AddRange( Le64( 0x2A ) );       // unique id
        bytes.AddRange( Le64( 1000 ) );       // block position (past the end)
        bytes.AddRange( Le64( 4 ) );          // block length
        bytes.AddRange( Le64( 0 ) );          // uncompressed length
        bytes.AddRange( new byte[ 8 ] );      // reserved

        Assert.Throws< XISFException >( () => XISFDataBlocksFile.Block( 0x2A, ( ReadOnlyMemory< byte > )bytes.ToArray() ) );
    }

    /// <summary>Encodes a 32-bit unsigned integer as little-endian bytes.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The four little-endian bytes.</returns>
    private static byte[] Le32( uint value )
    {
        byte[] bytes = new byte[ 4 ];

        BinaryPrimitives.WriteUInt32LittleEndian( bytes, value );

        return bytes;
    }

    /// <summary>Encodes a 64-bit unsigned integer as little-endian bytes.</summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The eight little-endian bytes.</returns>
    private static byte[] Le64( ulong value )
    {
        byte[] bytes = new byte[ 8 ];

        BinaryPrimitives.WriteUInt64LittleEndian( bytes, value );

        return bytes;
    }
}
