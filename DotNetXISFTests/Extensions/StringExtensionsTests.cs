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

using System.Text;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="StringExtensions"/>.
/// </summary>
/// <remarks>
/// Covers the textual header helpers: identifier validation and the
/// whitespace-tolerant hexadecimal and base64 decoders, including their rejection
/// of malformed input.
/// </remarks>
public class StringExtensionsTests
{
    /// <summary>
    /// <see cref="StringExtensions.XisfHexDecodedData"/> decodes lowercase and
    /// uppercase hexadecimal, ignores embedded whitespace, and maps the empty
    /// string to empty data.
    /// </summary>
    [ Fact ]
    public void HexDecodedData()
    {
        byte[] hello = Encoding.UTF8.GetBytes( "Hello" );

        Assert.Equal( hello, "48656c6c6f".XisfHexDecodedData().ToArray() );
        Assert.Equal( hello, "48656C6C6F".XisfHexDecodedData().ToArray() );
        Assert.Equal( hello, "48 65\n6c\t6c6f".XisfHexDecodedData().ToArray() );
        Assert.Empty( "".XisfHexDecodedData().ToArray() );
    }

    /// <summary>
    /// <see cref="StringExtensions.XisfHexDecodedData"/> rejects an odd digit count
    /// and non-hexadecimal characters.
    /// </summary>
    [ Fact ]
    public void HexDecodedDataRejectsMalformed()
    {
        Assert.Throws< XISFException >( () => "abc".XisfHexDecodedData() );
        Assert.Throws< XISFException >( () => "zz".XisfHexDecodedData() );
        Assert.Throws< XISFException >( () => "4g".XisfHexDecodedData() );
    }

    /// <summary>
    /// <see cref="StringExtensions.XisfBase64DecodedData"/> decodes base64 and
    /// ignores the whitespace used to wrap it across lines.
    /// </summary>
    [ Fact ]
    public void Base64DecodedData()
    {
        byte[] hello = Encoding.UTF8.GetBytes( "Hello" );

        Assert.Equal( hello, "SGVsbG8=".XisfBase64DecodedData().ToArray() );
        Assert.Equal( hello, "SGVs\nbG8=".XisfBase64DecodedData().ToArray() );
    }

    /// <summary>
    /// <see cref="StringExtensions.XisfBase64DecodedData"/> rejects a string that is
    /// not valid base64.
    /// </summary>
    [ Fact ]
    public void Base64DecodedDataRejectsMalformed()
    {
        Assert.Throws< XISFException >( () => "A".XisfBase64DecodedData() );
    }

    /// <summary>
    /// <see cref="StringExtensions.IsValidXisfIdentifier"/> accepts the
    /// <c>[_a-zA-Z][_a-zA-Z0-9]*</c> grammar and rejects everything else.
    /// </summary>
    [ Fact ]
    public void IsValidXisfIdentifier()
    {
        Assert.True( "foo".IsValidXisfIdentifier() );
        Assert.True( "_bar9".IsValidXisfIdentifier() );
        Assert.True( "Good_Name1".IsValidXisfIdentifier() );
        Assert.True( "_".IsValidXisfIdentifier() );

        Assert.False( "9bad".IsValidXisfIdentifier() );
        Assert.False( "".IsValidXisfIdentifier() );
        Assert.False( "has space".IsValidXisfIdentifier() );
        Assert.False( "dash-name".IsValidXisfIdentifier() );
    }
}
