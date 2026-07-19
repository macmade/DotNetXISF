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
/// Unit tests for <see cref="XISFFitsKeyword"/>: parsing an embedded FITS header card,
/// the empty-value-to-<c>null</c> rule, and the strict/lenient name length and charset
/// validation.
/// </summary>
public class XISFFitsKeywordTests
{
    /// <summary>Parses a keyword from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;FITSKeyword&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed keyword.</returns>
    private static XISFFitsKeyword Keyword( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFFitsKeyword( XISFXmlParser.Parse( xml ), options );
    }

    /// <summary>A keyword carries its name, value and comment.</summary>
    [ Fact ]
    public void ParsesKeyword()
    {
        XISFFitsKeyword keyword = Keyword( "<FITSKeyword name=\"EXPTIME\" value=\"10.0\" comment=\"exposure time\"/>" );

        Assert.Equal( "EXPTIME", keyword.Name );
        Assert.Equal( "10.0", keyword.Value );
        Assert.Equal( "exposure time", keyword.Comment );
    }

    /// <summary>An empty <c>value</c> attribute (as on <c>HISTORY</c>/<c>COMMENT</c>) becomes <c>null</c>.</summary>
    [ Fact ]
    public void EmptyValueBecomesNull()
    {
        XISFFitsKeyword keyword = Keyword( "<FITSKeyword name=\"HISTORY\" value=\"\" comment=\"processed\"/>" );

        Assert.Equal( "HISTORY", keyword.Name );
        Assert.Null( keyword.Value );
        Assert.Equal( "processed", keyword.Comment );
    }

    /// <summary>A keyword without a <c>name</c> attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingName()
    {
        Assert.Throws< XISFException >( () => Keyword( "<FITSKeyword value=\"x\"/>" ) );
    }

    /// <summary>A name longer than 8 characters is rejected under strict parsing but kept under lenient.</summary>
    [ Fact ]
    public void ValidatesNameLengthAccordingToOptions()
    {
        XISFElement element = XISFXmlParser.Parse( "<FITSKeyword name=\"TOOLONGNAME\" value=\"1\"/>" );

        Assert.Throws< XISFException >( () => new XISFFitsKeyword( element, XISFParsingOptions.Strict ) );
        Assert.Equal( "TOOLONGNAME", new XISFFitsKeyword( element, XISFParsingOptions.Lenient ).Name );
    }

    /// <summary>A name with disallowed characters is rejected under strict parsing but kept under lenient.</summary>
    [ Fact ]
    public void ValidatesNameCharsetAccordingToOptions()
    {
        XISFElement element = XISFXmlParser.Parse( "<FITSKeyword name=\"lower\" value=\"1\"/>" );

        Assert.Throws< XISFException >( () => new XISFFitsKeyword( element, XISFParsingOptions.Strict ) );
        Assert.Equal( "lower", new XISFFitsKeyword( element, XISFParsingOptions.Lenient ).Name );
    }

    /// <summary>The description summarizes the keyword.</summary>
    [ Fact ]
    public void Description()
    {
        XISFFitsKeyword keyword = Keyword( "<FITSKeyword name=\"EXPTIME\" value=\"10.0\" comment=\"exposure time\"/>" );

        Assert.Equal( "XISFFitsKeyword { name: EXPTIME, value: 10.0, comment: exposure time }", keyword.ToString() );
    }

    /// <summary>A default-constructed keyword is safe to use.</summary>
    [ Fact ]
    public void DefaultKeywordIsSafe()
    {
        XISFFitsKeyword keyword = default;

        Assert.Equal( "", keyword.Name );
        Assert.Null( keyword.Value );
        Assert.Equal( "XISFFitsKeyword { name: , value: <nil>, comment: <nil> }", keyword.ToString() );
    }
}
