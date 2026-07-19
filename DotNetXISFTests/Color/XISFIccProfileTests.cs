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
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFIccProfile"/>: exposing the opaque profile bytes of
/// inline and attachment data blocks, and rejecting a missing location.
/// </summary>
public class XISFIccProfileTests
{
    /// <summary>Parses an ICC profile from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;ICCProfile&gt;</c> XML fragment.</param>
    /// <param name="fileData">The complete file bytes, for an attachment location.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed ICC profile.</returns>
    private static XISFIccProfile IccProfile( string xml, ReadOnlyMemory< byte > fileData = default, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFIccProfile( XISFXmlParser.Parse( xml ), fileData, null, options );
    }

    /// <summary>An inline ICC profile exposes its decoded bytes.</summary>
    [ Fact ]
    public void ExposesInlineProfileBytes()
    {
        XISFIccProfile icc = IccProfile( "<ICCProfile location=\"inline:hex\">deadbeef</ICCProfile>" );

        Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, icc.Data.ToArray() );
    }

    /// <summary>An attachment ICC profile resolves its bytes from the file.</summary>
    [ Fact ]
    public void ResolvesAttachmentProfileBytes()
    {
        ReadOnlyMemory< byte > fileData = new byte[] { 0x00, 0x00, 0x11, 0x22, 0x33, 0x44 };
        XISFIccProfile         icc      = IccProfile( "<ICCProfile location=\"attachment:2:4\"/>", fileData );

        Assert.Equal( new byte[] { 0x11, 0x22, 0x33, 0x44 }, icc.Data.ToArray() );
    }

    /// <summary>An ICC profile without a location is rejected.</summary>
    [ Fact ]
    public void RejectsMissingLocation()
    {
        Assert.Throws< XISFException >( () => IccProfile( "<ICCProfile/>" ) );
    }

    /// <summary>The description summarizes the ICC profile without reading its bytes.</summary>
    [ Fact ]
    public void Description()
    {
        XISFIccProfile icc = IccProfile( "<ICCProfile location=\"inline:hex\">deadbeef</ICCProfile>" );

        Assert.Equal( "XISFIccProfile { location: inline:hex }", icc.ToString() );
    }
}
