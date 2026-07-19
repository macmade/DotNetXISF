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
/// Unit tests for <see cref="XISFRgbWorkingSpace"/>: parsing the chromaticity/luminance
/// triplets, the numeric and sRGB gamma, the optional name, and the mandatory-attribute
/// and strict-validation rejections.
/// </summary>
public class XISFRgbWorkingSpaceTests
{
    /// <summary>Parses an RGB working space from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;RGBWorkingSpace&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed working space.</returns>
    private static XISFRgbWorkingSpace WorkingSpace( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFRgbWorkingSpace( XISFXmlParser.Parse( xml ), options );
    }

    /// <summary>The primaries, luminance, numeric gamma and name are parsed.</summary>
    [ Fact ]
    public void ParsesPrimariesLuminanceGammaAndName()
    {
        XISFRgbWorkingSpace workingSpace = WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"2.2\" name=\"Adobe RGB (1998)\"/>" );

        Assert.Equal( new XISFRgbWorkingSpace.Primaries( 0.64, 0.30, 0.15 ), workingSpace.X );
        Assert.Equal( new XISFRgbWorkingSpace.Primaries( 0.33, 0.60, 0.06 ), workingSpace.Y );
        Assert.Equal( new XISFRgbWorkingSpace.Primaries( 0.22, 0.71, 0.06 ), workingSpace.Luminance );
        Assert.Equal( XISFRgbWorkingSpace.Gamma.Exponent( 2.2 ), workingSpace.GammaFunction );
        Assert.Equal( "Adobe RGB (1998)", workingSpace.Name );
    }

    /// <summary>An <c>sRGB</c> gamma is recognized case-insensitively.</summary>
    [ Fact ]
    public void ParsesSrgbGammaCaseInsensitively()
    {
        XISFRgbWorkingSpace workingSpace = WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"sRGB\"/>" );

        Assert.Equal( XISFRgbWorkingSpace.Gamma.Srgb, workingSpace.GammaFunction );
        Assert.Null( workingSpace.Name );
    }

    /// <summary>A missing mandatory attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingMandatoryAttributes()
    {
        Assert.Throws< XISFException >( () => WorkingSpace( "<RGBWorkingSpace y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"2.2\"/>" ) );
        Assert.Throws< XISFException >( () => WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\"/>" ) );
    }

    /// <summary>A malformed triplet or non-positive numeric gamma is rejected.</summary>
    [ Fact ]
    public void RejectsMalformedTripletOrGamma()
    {
        Assert.Throws< XISFException >( () => WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"2.2\"/>" ) );
        Assert.Throws< XISFException >( () => WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"0\"/>" ) );
    }

    /// <summary>The description summarizes the working space.</summary>
    [ Fact ]
    public void Description()
    {
        XISFRgbWorkingSpace workingSpace = WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"2.2\"/>" );

        Assert.False( string.IsNullOrEmpty( workingSpace.ToString() ) );
    }

    /// <summary>A default-constructed working space is safe to use.</summary>
    [ Fact ]
    public void DefaultWorkingSpaceIsSafe()
    {
        XISFRgbWorkingSpace workingSpace = default;

        Assert.False( string.IsNullOrEmpty( workingSpace.ToString() ) );
        Assert.False( workingSpace.GammaFunction.IsSrgb );
        Assert.Equal( workingSpace, workingSpace );
    }

    /// <summary>Numeric parsing is culture-invariant.</summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFRgbWorkingSpace workingSpace = WorkingSpace( "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"2.2\"/>" );

            Assert.Equal( new XISFRgbWorkingSpace.Primaries( 0.64, 0.30, 0.15 ), workingSpace.X );
            Assert.Equal( XISFRgbWorkingSpace.Gamma.Exponent( 2.2 ), workingSpace.GammaFunction );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
