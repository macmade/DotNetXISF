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
/// Unit tests for <see cref="XISFDisplayFunction"/>: parsing the five per-component
/// parameter vectors and the optional name, and rejecting a missing attribute or a
/// wrong component count.
/// </summary>
public class XISFDisplayFunctionTests
{
    /// <summary>Parses a display function from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;DisplayFunction&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed display function.</returns>
    private static XISFDisplayFunction DisplayFunction( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFDisplayFunction( XISFXmlParser.Parse( xml ), options );
    }

    /// <summary>All five component vectors and the name are parsed.</summary>
    [ Fact ]
    public void ParsesAllComponentVectorsAndName()
    {
        XISFDisplayFunction displayFunction = DisplayFunction( "<DisplayFunction m=\"0.5:0.5:0.5:0.5\" s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\" name=\"AutoStretch\"/>" );

        Assert.Equal( new XISFDisplayFunction.Components( 0.5, 0.5, 0.5, 0.5 ), displayFunction.MidtonesBalance );
        Assert.Equal( new XISFDisplayFunction.Components( 0, 0, 0, 0 ), displayFunction.ShadowsClipping );
        Assert.Equal( new XISFDisplayFunction.Components( 1, 1, 1, 1 ), displayFunction.HighlightsClipping );
        Assert.Equal( new XISFDisplayFunction.Components( 0, 0, 0, 0 ), displayFunction.ShadowsExpansion );
        Assert.Equal( new XISFDisplayFunction.Components( 1, 1, 1, 1 ), displayFunction.HighlightsExpansion );
        Assert.Equal( "AutoStretch", displayFunction.Name );
    }

    /// <summary>A missing mandatory attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingMandatoryAttributes()
    {
        Assert.Throws< XISFException >( () => DisplayFunction( "<DisplayFunction s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\"/>" ) );
    }

    /// <summary>A component vector with the wrong number of components is rejected.</summary>
    [ Fact ]
    public void RejectsWrongComponentCount()
    {
        // m has only three components instead of four.
        Assert.Throws< XISFException >( () => DisplayFunction( "<DisplayFunction m=\"0.5:0.5:0.5\" s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\"/>" ) );
    }

    /// <summary>The description summarizes the display function.</summary>
    [ Fact ]
    public void Description()
    {
        XISFDisplayFunction displayFunction = DisplayFunction( "<DisplayFunction m=\"0.5:0.5:0.5:0.5\" s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\"/>" );

        Assert.False( string.IsNullOrEmpty( displayFunction.ToString() ) );
    }

    /// <summary>A default-constructed display function is safe to use.</summary>
    [ Fact ]
    public void DefaultDisplayFunctionIsSafe()
    {
        XISFDisplayFunction displayFunction = default;

        Assert.False( string.IsNullOrEmpty( displayFunction.ToString() ) );
        Assert.Equal( displayFunction, displayFunction );
    }

    /// <summary>Numeric parsing is culture-invariant.</summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFDisplayFunction displayFunction = DisplayFunction( "<DisplayFunction m=\"0.25:0.5:0.75:1.5\" s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\"/>" );

            Assert.Equal( new XISFDisplayFunction.Components( 0.25, 0.5, 0.75, 1.5 ), displayFunction.MidtonesBalance );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
