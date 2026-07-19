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
/// Unit tests for <see cref="XISFByteOrder"/> parsing and its default.
/// </summary>
public class XISFByteOrderTests
{
    /// <summary>
    /// Each spec token parses to its byte order; an unknown token does not.
    /// </summary>
    [ Fact ]
    public void SpecTokensMatchSpec()
    {
        Assert.Equal( XISFByteOrder.Big,    XISFByteOrderExtensions.FromSpecToken( "big" ) );
        Assert.Equal( XISFByteOrder.Little, XISFByteOrderExtensions.FromSpecToken( "little" ) );
        Assert.Null( XISFByteOrderExtensions.FromSpecToken( "native" ) );
    }

    /// <summary>
    /// The default byte order when the attribute is absent is little-endian, and it
    /// is the enum's zero value.
    /// </summary>
    [ Fact ]
    public void DefaultIsLittleEndian()
    {
        Assert.Equal( XISFByteOrder.Little, XISFByteOrderExtensions.Default );
        Assert.Equal( XISFByteOrder.Little, default( XISFByteOrder ) );
    }
}
