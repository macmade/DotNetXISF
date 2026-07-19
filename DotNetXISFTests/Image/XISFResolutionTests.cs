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
/// Unit tests for <see cref="XISFResolution"/>: parsing the horizontal/vertical values
/// and unit, the default-to-inch behavior, and the strict/lenient positivity and
/// unknown-unit handling.
/// </summary>
public class XISFResolutionTests
{
    /// <summary>Parses a resolution from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;Resolution&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed resolution.</returns>
    private static XISFResolution Resolution( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFResolution( XISFXmlParser.Parse( xml ), options );
    }

    /// <summary>The horizontal/vertical values and unit are parsed.</summary>
    [ Fact ]
    public void ParsesHorizontalVerticalAndUnit()
    {
        XISFResolution resolution = Resolution( "<Resolution horizontal=\"120\" vertical=\"96\" unit=\"cm\"/>" );

        Assert.Equal( 120.0, resolution.Horizontal );
        Assert.Equal( 96.0, resolution.Vertical );
        Assert.Equal( XISFResolution.Unit.Centimeter, resolution.ResolutionUnit );
    }

    /// <summary>An absent <c>unit</c> attribute defaults to inches.</summary>
    [ Fact ]
    public void DefaultsToInchWhenUnitAbsent()
    {
        Assert.Equal( XISFResolution.Unit.Inch, Resolution( "<Resolution horizontal=\"72\" vertical=\"72\"/>" ).ResolutionUnit );
    }

    /// <summary>A missing horizontal or vertical value is rejected.</summary>
    [ Fact ]
    public void RejectsMissingMandatoryAttributes()
    {
        Assert.Throws< XISFException >( () => Resolution( "<Resolution vertical=\"72\"/>" ) );
        Assert.Throws< XISFException >( () => Resolution( "<Resolution horizontal=\"72\"/>" ) );
    }

    /// <summary>A non-positive value or unknown unit is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsNonPositiveOrUnknownUnitWhenStrict()
    {
        Assert.Throws< XISFException >( () => Resolution( "<Resolution horizontal=\"0\" vertical=\"72\"/>" ) );
        Assert.Throws< XISFException >( () => Resolution( "<Resolution horizontal=\"72\" vertical=\"72\" unit=\"meters\"/>" ) );
    }

    /// <summary>An unknown unit falls back to inches under lenient parsing.</summary>
    [ Fact ]
    public void ToleratesUnknownUnitWhenLenient()
    {
        Assert.Equal( XISFResolution.Unit.Inch, Resolution( "<Resolution horizontal=\"72\" vertical=\"72\" unit=\"meters\"/>", XISFParsingOptions.Lenient ).ResolutionUnit );
    }

    /// <summary>The description summarizes the resolution.</summary>
    [ Fact ]
    public void Description()
    {
        Assert.False( string.IsNullOrEmpty( Resolution( "<Resolution horizontal=\"72\" vertical=\"72\" unit=\"inch\"/>" ).ToString() ) );
    }

    /// <summary>A default-constructed resolution is safe to use.</summary>
    [ Fact ]
    public void DefaultResolutionIsSafe()
    {
        XISFResolution resolution = default;

        Assert.Equal( XISFResolution.Unit.Inch, resolution.ResolutionUnit );
        Assert.False( string.IsNullOrEmpty( resolution.ToString() ) );
        Assert.Equal( resolution, resolution );
    }

    /// <summary>Numeric parsing is culture-invariant.</summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFResolution resolution = Resolution( "<Resolution horizontal=\"120.5\" vertical=\"96.25\" unit=\"cm\"/>" );

            Assert.Equal( 120.5, resolution.Horizontal );
            Assert.Equal( 96.25, resolution.Vertical );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
