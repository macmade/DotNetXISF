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

namespace DotNetXISF;

/// <summary>
/// Reads XISF data blocks files (<c>.xisb</c>) to resolve a data block by its
/// block index element identifier.
/// </summary>
/// <remarks>
/// A data blocks file begins with an 8-byte <c>XISB0100</c> signature and an
/// 8-byte reserved field, followed by a singly-linked list of <em>block index
/// nodes</em>. Each node stores a 32-bit element count, a 4-byte reserved field, a
/// 64-bit position of the next node (zero when it is the last), and then that many
/// 40-byte <em>block index elements</em>. Each element carries a 64-bit unique
/// identifier, the block's 64-bit position and length, a 64-bit uncompressed
/// length, and an 8-byte reserved field. All integers are little-endian.
/// <para>
/// This type is internal infrastructure used by <c>XISFDataBlock</c> to resolve
/// <c>url(...):index-id</c> and <c>path(...):index-id</c> locations.
/// </para>
/// </remarks>
internal static class XISFDataBlocksFile
{
    /// <summary>The 8-byte ASCII signature that opens every XISF data blocks file.</summary>
    internal const string Signature = "XISB0100";

    /// <summary>
    /// The size, in bytes, of the fixed header (signature plus reserved field)
    /// preceding the first block index node.
    /// </summary>
    internal const int HeaderSize = 16;

    /// <summary>The size, in bytes, of a single block index element.</summary>
    internal const int ElementSize = 40;

    /// <summary>The number of leading bytes of a block index element that carry its id, position and length.</summary>
    private const int ElementFieldsSize = 24;

    /// <summary>
    /// Resolves the bytes of the data block identified by <paramref name="indexId"/>
    /// in a data blocks file.
    /// </summary>
    /// <param name="indexId">The unique identifier of the block index element to locate.</param>
    /// <param name="data">The complete contents of the data blocks file.</param>
    /// <returns>The raw (still as-stored) bytes of the located block.</returns>
    /// <exception cref="XISFException">
    /// The signature is invalid, the block index is malformed or truncated, the
    /// identifier is not found, or the located block range is out of bounds
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    internal static ReadOnlyMemory< byte > Block( ulong indexId, ReadOnlyMemory< byte > data )
    {
        if( data.MatchesAscii( Signature, 0 ) == false )
        {
            throw XISFException.DataBlockError( $"External file is not an XISF data blocks file (missing the { Signature } signature)" );
        }

        if( FindElement( indexId, data ) is not Element element )
        {
            throw XISFException.DataBlockError( $"No block index element with id 0x{ indexId.ToString( "x", CultureInfo.InvariantCulture ) } in the XISF data blocks file" );
        }

        if( element.Position > int.MaxValue || element.Length > int.MaxValue )
        {
            throw OutOfBounds( indexId, element );
        }

        try
        {
            return data.Bytes( ( int )element.Position, ( int )element.Length );
        }
        catch( XISFException )
        {
            throw OutOfBounds( indexId, element );
        }
    }

    /// <summary>A resolved block index element: the position and length of a data block.</summary>
    private readonly struct Element
    {
        /// <summary>The byte position of the block, from the start of the file.</summary>
        public ulong Position { get; }

        /// <summary>The length of the block, in bytes.</summary>
        public ulong Length { get; }

        /// <summary>Creates a resolved element.</summary>
        /// <param name="position">The block's byte position.</param>
        /// <param name="length">The block's length, in bytes.</param>
        public Element( ulong position, ulong length )
        {
            this.Position = position;
            this.Length   = length;
        }
    }

    /// <summary>
    /// Walks the singly-linked list of block index nodes to find the element with a
    /// given identifier.
    /// </summary>
    /// <param name="indexId">The unique identifier to find.</param>
    /// <param name="data">The complete contents of the data blocks file.</param>
    /// <returns>The matching element, or <c>null</c> if the identifier is not found.</returns>
    /// <exception cref="XISFException">
    /// A node extends past the end of the file or the node chain is cyclic
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static Element? FindElement( ulong indexId, ReadOnlyMemory< byte > data )
    {
        ulong            nodePosition = HeaderSize;
        HashSet< ulong > visited      = new HashSet< ulong >();

        while( nodePosition != 0 )
        {
            if( visited.Add( nodePosition ) == false )
            {
                throw XISFException.DataBlockError( "Cyclic block index in the XISF data blocks file" );
            }

            if( nodePosition > ( ulong )data.Length )
            {
                throw XISFException.DataBlockError( $"Truncated block index node at position { nodePosition.ToString( CultureInfo.InvariantCulture ) }" );
            }

            int nodeStart = ( int )nodePosition;

            if( ( long )nodeStart + HeaderSize > data.Length )
            {
                throw XISFException.DataBlockError( $"Truncated block index node at position { nodePosition.ToString( CultureInfo.InvariantCulture ) }" );
            }

            uint  count = data.LittleEndianInteger< uint >( nodeStart );
            ulong next  = data.LittleEndianInteger< ulong >( nodeStart + 8 );

            if( ElementInNode( indexId, count, ( long )nodeStart + HeaderSize, data ) is Element found )
            {
                return found;
            }

            nodePosition = next;
        }

        return null;
    }

    /// <summary>Scans the elements of a single block index node for a matching identifier.</summary>
    /// <param name="indexId">The unique identifier to find.</param>
    /// <param name="count">The number of elements in the node.</param>
    /// <param name="start">The byte position of the first element.</param>
    /// <param name="data">The complete contents of the data blocks file.</param>
    /// <returns>The matching element, or <c>null</c> if not found in this node.</returns>
    /// <exception cref="XISFException">
    /// An element extends past the end of the file
    /// (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    private static Element? ElementInNode( ulong indexId, uint count, long start, ReadOnlyMemory< byte > data )
    {
        for( long index = 0; index < count; index += 1 )
        {
            long elementStart = start + index * ElementSize;

            if( elementStart < 0 || elementStart + ElementFieldsSize > data.Length )
            {
                throw XISFException.DataBlockError( $"Truncated block index element at position { elementStart.ToString( CultureInfo.InvariantCulture ) }" );
            }

            int   fieldStart = ( int )elementStart;
            ulong identifier = data.LittleEndianInteger< ulong >( fieldStart );
            ulong position   = data.LittleEndianInteger< ulong >( fieldStart + 8 );
            ulong length     = data.LittleEndianInteger< ulong >( fieldStart + 16 );

            if( identifier == indexId )
            {
                return new Element( position, length );
            }
        }

        return null;
    }

    /// <summary>Creates the out-of-bounds error for a located block whose range falls outside the file.</summary>
    /// <param name="indexId">The block index element's identifier.</param>
    /// <param name="element">The located element.</param>
    /// <returns>The created exception.</returns>
    private static XISFException OutOfBounds( ulong indexId, Element element )
    {
        return XISFException.DataBlockError( $"Block index element 0x{ indexId.ToString( "x", CultureInfo.InvariantCulture ) } points to an out-of-bounds range (position { element.Position.ToString( CultureInfo.InvariantCulture ) }, length { element.Length.ToString( CultureInfo.InvariantCulture ) })" );
    }
}
