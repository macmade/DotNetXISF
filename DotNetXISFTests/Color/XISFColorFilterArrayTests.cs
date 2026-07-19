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

using System.Globalization;
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFColorFilterArray"/>: parsing the pattern, dimensions
/// and name, decoding the pattern into typed elements, and the strict validation of
/// pattern characters, dimensions and pattern length.
/// </summary>
public class XISFColorFilterArrayTests
{
    /// <summary>Parses a color filter array from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;ColorFilterArray&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed color filter array.</returns>
    private static XISFColorFilterArray ColorFilterArray( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFColorFilterArray( XISFXmlParser.Parse( xml ), options );
    }

    /// <summary>The pattern, dimensions, name and decoded elements are parsed.</summary>
    [ Fact ]
    public void ParsesPatternWidthHeightAndName()
    {
        XISFColorFilterArray cfa = ColorFilterArray( "<ColorFilterArray pattern=\"GRBG\" width=\"2\" height=\"2\" name=\"GRBG Bayer Filter\"/>" );

        Assert.Equal( "GRBG", cfa.Pattern );
        Assert.Equal( 2L, cfa.Width );
        Assert.Equal( 2L, cfa.Height );
        Assert.Equal( "GRBG Bayer Filter", cfa.Name );
        Assert.Equal( new[] { XISFColorFilterArray.Element.Green, XISFColorFilterArray.Element.Red, XISFColorFilterArray.Element.Blue, XISFColorFilterArray.Element.Green }, cfa.Elements );
    }

    /// <summary>Every defined pattern element character decodes to its typed element.</summary>
    [ Fact ]
    public void ParsesAllPatternElements()
    {
        XISFColorFilterArray cfa = ColorFilterArray( "<ColorFilterArray pattern=\"0RGBWCMY\" width=\"8\" height=\"1\"/>" );

        Assert.Null( cfa.Name );
        Assert.Equal(
            new[]
            {
                XISFColorFilterArray.Element.None,
                XISFColorFilterArray.Element.Red,
                XISFColorFilterArray.Element.Green,
                XISFColorFilterArray.Element.Blue,
                XISFColorFilterArray.Element.White,
                XISFColorFilterArray.Element.Cyan,
                XISFColorFilterArray.Element.Magenta,
                XISFColorFilterArray.Element.Yellow,
            },
            cfa.Elements );
    }

    /// <summary>A missing mandatory attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingMandatoryAttributes()
    {
        Assert.Throws< XISFException >( () => ColorFilterArray( "<ColorFilterArray width=\"2\" height=\"2\"/>" ) );
        Assert.Throws< XISFException >( () => ColorFilterArray( "<ColorFilterArray pattern=\"GRBG\" height=\"2\"/>" ) );
    }

    /// <summary>An invalid pattern character is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsInvalidPatternCharacterWhenStrict()
    {
        Assert.Throws< XISFException >( () => ColorFilterArray( "<ColorFilterArray pattern=\"GRXG\" width=\"2\" height=\"2\"/>" ) );
    }

    /// <summary>A pattern length not matching width × height is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsPatternLengthMismatchWhenStrict()
    {
        // 3 pattern characters but width × height = 4.
        Assert.Throws< XISFException >( () => ColorFilterArray( "<ColorFilterArray pattern=\"GRB\" width=\"2\" height=\"2\"/>" ) );
    }

    /// <summary>Non-positive dimensions are rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsNonPositiveDimensionsWhenStrict()
    {
        Assert.Throws< XISFException >( () => ColorFilterArray( "<ColorFilterArray pattern=\"\" width=\"0\" height=\"0\"/>" ) );
    }

    /// <summary>Invalid characters and a length mismatch are tolerated under lenient parsing.</summary>
    [ Fact ]
    public void ToleratesInvalidPatternWhenLenient()
    {
        XISFColorFilterArray cfa = ColorFilterArray( "<ColorFilterArray pattern=\"GRX\" width=\"2\" height=\"2\"/>", XISFParsingOptions.Lenient );

        Assert.Equal( "GRX", cfa.Pattern );

        // The unknown 'X' character is dropped from the decoded elements.
        Assert.Equal( new[] { XISFColorFilterArray.Element.Green, XISFColorFilterArray.Element.Red }, cfa.Elements );
    }

    /// <summary>The description summarizes the color filter array.</summary>
    [ Fact ]
    public void Description()
    {
        XISFColorFilterArray cfa = ColorFilterArray( "<ColorFilterArray pattern=\"RGGB\" width=\"2\" height=\"2\"/>" );

        Assert.Equal( "XISFColorFilterArray { pattern: RGGB, width: 2, height: 2, name: <nil> }", cfa.ToString() );
    }

    /// <summary>A default-constructed color filter array is safe to use.</summary>
    [ Fact ]
    public void DefaultColorFilterArrayIsSafe()
    {
        XISFColorFilterArray cfa = default;

        Assert.Equal( "", cfa.Pattern );
        Assert.Empty( cfa.Elements );
        Assert.False( string.IsNullOrEmpty( cfa.ToString() ) );
    }

    /// <summary>Dimension parsing is culture-invariant.</summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFColorFilterArray cfa = ColorFilterArray( "<ColorFilterArray pattern=\"GRBG\" width=\"2\" height=\"2\"/>" );

            Assert.Equal( 2L, cfa.Width );
            Assert.Equal( 2L, cfa.Height );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
