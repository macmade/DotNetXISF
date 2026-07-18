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
/// Unit tests for <see cref="XISFFile"/>.
/// </summary>
/// <remarks>
/// At this stage the type carries only the preamble constants fixed by the XISF
/// standard; this guard test pins their exact values so a later edit cannot
/// silently change the wire-level contract the rest of the parser depends on.
/// </remarks>
public class XISFFileTests
{
    /// <summary>
    /// The preamble constants hold the exact values the XISF standard fixes: the
    /// <c>XISF0100</c> signature, a 16-byte preamble, and the PixInsight XISF
    /// namespace URI.
    /// </summary>
    [ Fact ]
    public void PreambleConstantsHaveTheirExpectedValues()
    {
        Assert.Equal( "XISF0100",                       XISFFile.Signature );
        Assert.Equal( 16,                               XISFFile.PreambleSize );
        Assert.Equal( "http://www.pixinsight.com/xisf", XISFFile.Namespace );
    }
}
