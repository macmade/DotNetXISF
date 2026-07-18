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
using System.Linq;

namespace DotNetXISF;

/// <summary>
/// Decoding and validation helpers for the textual parts of an XISF header.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Returns whether the string is a valid XISF identifier.
    /// </summary>
    /// <remarks>
    /// XISF identifiers (property and image <c>id</c> attributes, for instance)
    /// match the grammar <c>[_a-zA-Z][_a-zA-Z0-9]*</c>: a non-empty string starting
    /// with an ASCII letter or underscore, followed by letters, digits or
    /// underscores.
    /// </remarks>
    /// <param name="value">The string to validate.</param>
    /// <returns><c>true</c> if <paramref name="value"/> is a valid identifier.</returns>
    public static bool IsValidXisfIdentifier( this string value )
    {
        if( value.Length == 0 || XISFCharacterSet.IsIdentifierStart( value[ 0 ] ) == false )
        {
            return false;
        }

        return value.Skip( 1 ).All( XISFCharacterSet.IsIdentifierBody );
    }

    /// <summary>
    /// Decodes the string as base16 (hexadecimal) bytes.
    /// </summary>
    /// <remarks>
    /// Whitespace is ignored, so hex content wrapped across lines in the XML header
    /// decodes correctly. Although the XISF specification emits lowercase digits,
    /// uppercase digits are also accepted.
    /// </remarks>
    /// <param name="value">The hexadecimal string to decode.</param>
    /// <returns>The decoded bytes; an empty string yields empty data.</returns>
    /// <exception cref="XISFException">
    /// After removing whitespace, the string has an odd number of digits or
    /// contains a character that is not a hexadecimal digit
    /// (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    public static ReadOnlyMemory< byte > XisfHexDecodedData( this string value )
    {
        char[] digits = value.Where( character => char.IsWhiteSpace( character ) == false ).ToArray();

        if( digits.Length % 2 != 0 )
        {
            throw XISFException.DataError( "Hex string has an odd number of digits" );
        }

        byte[] bytes = new byte[ digits.Length / 2 ];

        for( int index = 0; index < digits.Length; index += 2 )
        {
            int high = HexDigitValue( digits[ index ] );
            int low  = HexDigitValue( digits[ index + 1 ] );

            if( high < 0 || low < 0 )
            {
                throw XISFException.DataError( "Invalid hexadecimal digit" );
            }

            bytes[ index / 2 ] = ( byte )( ( high << 4 ) | low );
        }

        return bytes;
    }

    /// <summary>
    /// Decodes the string as base64 bytes.
    /// </summary>
    /// <remarks>
    /// Characters outside the base64 alphabet, including the whitespace used to
    /// wrap base64 content across lines in the XML header, are ignored before
    /// decoding.
    /// </remarks>
    /// <param name="value">The base64 string to decode.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="XISFException">
    /// The string is not valid base64 (<see cref="XISFErrorKind.DataError"/>).
    /// </exception>
    public static ReadOnlyMemory< byte > XisfBase64DecodedData( this string value )
    {
        string filtered = new string( value.Where( IsBase64Character ).ToArray() );

        try
        {
            return Convert.FromBase64String( filtered );
        }
        catch( FormatException )
        {
            throw XISFException.DataError( "Invalid base64 data" );
        }
    }

    /// <summary>
    /// Returns the numeric value of a hexadecimal digit, or <c>-1</c> when the
    /// character is not a hexadecimal digit.
    /// </summary>
    /// <param name="character">The character to interpret.</param>
    /// <returns>The value <c>0</c>-<c>15</c>, or <c>-1</c> when invalid.</returns>
    private static int HexDigitValue( char character )
    {
        if( character >= '0' && character <= '9' )
        {
            return character - '0';
        }

        if( character >= 'a' && character <= 'f' )
        {
            return character - 'a' + 10;
        }

        if( character >= 'A' && character <= 'F' )
        {
            return character - 'A' + 10;
        }

        return -1;
    }

    /// <summary>
    /// Returns whether <paramref name="character"/> belongs to the base64 alphabet,
    /// including the <c>=</c> padding character.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if it is a base64 alphabet or padding character.</returns>
    private static bool IsBase64Character( char character )
    {
        return char.IsAsciiLetterOrDigit( character ) || character == '+' || character == '/' || character == '=';
    }
}
