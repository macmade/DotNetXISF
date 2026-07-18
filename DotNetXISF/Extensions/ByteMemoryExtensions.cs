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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotNetXISF;

/// <summary>
/// Byte-level helpers used to read and validate the binary parts of an XISF
/// monolithic file.
/// </summary>
/// <remarks>
/// Extensions on <see cref="ReadOnlyMemory{ Byte }"/>, a value type wrapping a
/// buffer, offset and length whose slicing is O(1) and shares storage. Every
/// offset is interpreted relative to the memory's own start, so the helpers behave
/// identically on a full buffer and on a slice of one - slicing re-bases the
/// memory to index zero.
/// </remarks>
public static class ByteMemoryExtensions
{
    /// <summary>
    /// Returns a bounds-checked sub-range of the memory.
    /// </summary>
    /// <param name="data">The memory to slice.</param>
    /// <param name="offset">
    /// The number of bytes from the start at which the range begins. Must be
    /// non-negative.
    /// </param>
    /// <param name="count">The number of bytes to return. Must be non-negative.</param>
    /// <returns>
    /// A slice of <paramref name="count"/> bytes beginning at
    /// <paramref name="offset"/>. The slice shares the receiver's storage and is
    /// itself safe to pass back into these helpers.
    /// </returns>
    /// <exception cref="XISFException">
    /// <paramref name="offset"/> or <paramref name="count"/> is negative, or the
    /// requested range extends past the end of the data
    /// (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    public static ReadOnlyMemory< byte > Bytes( this ReadOnlyMemory< byte > data, int offset, int count )
    {
        if( offset < 0 || count < 0 )
        {
            throw XISFException.DataError( "Negative offset or count" );
        }

        // Widened to 64-bit so the end computation cannot overflow for pathological
        // offset/count values before the bounds check runs.
        long end = ( long )offset + count;

        if( end > data.Length )
        {
            throw XISFException.DataError( $"Requested range { offset.ToString( CultureInfo.InvariantCulture ) }..<{ end.ToString( CultureInfo.InvariantCulture ) } is out of bounds for { data.Length.ToString( CultureInfo.InvariantCulture ) } bytes" );
        }

        return data.Slice( offset, count );
    }

    /// <summary>
    /// Reads a little-endian, fixed-width integer from the memory.
    /// </summary>
    /// <remarks>
    /// XISF stores the monolithic-file header-length field and the distributed
    /// block-index fields as little-endian integers; this helper covers those and
    /// any other little-endian field. The number of bytes consumed is the size of
    /// <typeparamref name="T"/>.
    /// </remarks>
    /// <typeparam name="T">The fixed-width integer type to read.</typeparam>
    /// <param name="data">The memory to read from.</param>
    /// <param name="offset">
    /// The number of bytes from the start at which the integer begins.
    /// </param>
    /// <returns>The decoded integer.</returns>
    /// <exception cref="XISFException">
    /// The integer's bytes extend past the end of the data
    /// (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    public static T LittleEndianInteger< T >( this ReadOnlyMemory< byte > data, int offset )
        where T : unmanaged, IBinaryInteger< T >, IMinMaxValue< T >
    {
        ReadOnlyMemory< byte > bytes = data.Bytes( offset, Unsafe.SizeOf< T >() );

        // A type whose minimum value is zero is unsigned; the read fills exactly the
        // type's width, so the value always fits.
        return T.ReadLittleEndian( bytes.Span, isUnsigned: T.IsZero( T.MinValue ) );
    }

    /// <summary>
    /// Returns whether the bytes at a given offset equal the ASCII encoding of a
    /// string.
    /// </summary>
    /// <remarks>
    /// Used to match fixed binary markers such as the <c>XISF0100</c> signature. A
    /// string that is not representable as ASCII never matches.
    /// </remarks>
    /// <param name="data">The memory to inspect.</param>
    /// <param name="value">The ASCII string to compare against.</param>
    /// <param name="offset">
    /// The number of bytes from the start at which to compare.
    /// </param>
    /// <returns>
    /// <c>true</c> if the data contains exactly the ASCII bytes of
    /// <paramref name="value"/> at <paramref name="offset"/>; otherwise <c>false</c>.
    /// </returns>
    public static bool MatchesAscii( this ReadOnlyMemory< byte > data, string value, int offset )
    {
        if( Ascii.IsValid( value ) == false )
        {
            return false;
        }

        byte[] ascii = Encoding.ASCII.GetBytes( value );

        try
        {
            return data.Bytes( offset, ascii.Length ).Span.SequenceEqual( ascii );
        }
        catch( XISFException )
        {
            return false;
        }
    }
}
