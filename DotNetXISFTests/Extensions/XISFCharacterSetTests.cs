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

using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFCharacterSet"/>.
/// </summary>
/// <remarks>
/// Pins the two membership predicates behind XISF identifier validation: the
/// start set (a letter or underscore) and the body set (the start set plus the
/// digits).
/// </remarks>
public class XISFCharacterSetTests
{
    /// <summary>
    /// The identifier-start set contains ASCII letters and the underscore, but not
    /// digits or spaces.
    /// </summary>
    [ Fact ]
    public void IdentifierStart()
    {
        Assert.True( XISFCharacterSet.IsIdentifierStart( 'a' ) );
        Assert.True( XISFCharacterSet.IsIdentifierStart( 'Z' ) );
        Assert.True( XISFCharacterSet.IsIdentifierStart( '_' ) );
        Assert.False( XISFCharacterSet.IsIdentifierStart( '9' ) );
        Assert.False( XISFCharacterSet.IsIdentifierStart( ' ' ) );
    }

    /// <summary>
    /// The identifier-body set contains ASCII letters, the underscore and digits,
    /// but not the hyphen or a space.
    /// </summary>
    [ Fact ]
    public void IdentifierBody()
    {
        Assert.True( XISFCharacterSet.IsIdentifierBody( 'a' ) );
        Assert.True( XISFCharacterSet.IsIdentifierBody( 'Z' ) );
        Assert.True( XISFCharacterSet.IsIdentifierBody( '_' ) );
        Assert.True( XISFCharacterSet.IsIdentifierBody( '9' ) );
        Assert.False( XISFCharacterSet.IsIdentifierBody( '-' ) );
        Assert.False( XISFCharacterSet.IsIdentifierBody( ' ' ) );
    }
}
