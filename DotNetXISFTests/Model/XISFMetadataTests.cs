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
/// Unit tests for <see cref="XISFMetadata"/>: exposing the child <c>&lt;Property&gt;</c>
/// elements, the identifier subscript, and the strict/lenient handling of a malformed
/// child property.
/// </summary>
public class XISFMetadataTests
{
    /// <summary>Parses a metadata element from an XML fragment, with no attached file bytes.</summary>
    /// <param name="xml">The <c>&lt;Metadata&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed metadata.</returns>
    private static XISFMetadata Metadata( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFMetadata( XISFXmlParser.Parse( xml ), ReadOnlyMemory< byte >.Empty, null, options );
    }

    /// <summary>The child properties are exposed and reachable through the identifier subscript.</summary>
    [ Fact ]
    public void ParsesChildProperties()
    {
        string       xml      = "<Metadata><Property id=\"XISF:CreatorApplication\" type=\"String\">PixInsight</Property><Property id=\"XISF:CompressionLevel\" type=\"Int32\" value=\"75\"/></Metadata>";
        XISFMetadata metadata = Metadata( xml );

        Assert.Equal( 2, metadata.Properties.Count );
        Assert.Equal( XISFValue.String( "PixInsight" ), metadata[ "XISF:CreatorApplication" ]?.Value );
        Assert.Equal( XISFValue.Integer( 75 ), metadata[ "XISF:CompressionLevel" ]?.Value );
        Assert.Null( metadata[ "XISF:Missing" ] );
    }

    /// <summary>A metadata element with no properties exposes an empty collection.</summary>
    [ Fact ]
    public void ExposesEmptyPropertiesWhenNonePresent()
    {
        Assert.Empty( Metadata( "<Metadata/>" ).Properties );
    }

    /// <summary>A malformed child property is dropped under lenient parsing.</summary>
    [ Fact ]
    public void DropsMalformedPropertyWhenLenient()
    {
        // First child has an unknown type (dropped at the type gate); second child has a
        // known type but no value (dropped when its construction throws). Only the valid
        // third property survives.
        string xml = "<Metadata>"
            + "<Property id=\"a\" type=\"BadType\" value=\"1\"/>"
            + "<Property id=\"b\" type=\"Int32\"/>"
            + "<Property id=\"XISF:Good\" type=\"Int32\" value=\"5\"/>"
            + "</Metadata>";

        XISFMetadata metadata = Metadata( xml, XISFParsingOptions.Lenient );

        Assert.Single( metadata.Properties );
        Assert.Equal( "XISF:Good", metadata.Properties[ 0 ].Id );
    }

    /// <summary>A malformed child property is fatal under strict parsing.</summary>
    [ Fact ]
    public void ThrowsOnMalformedPropertyWhenStrict()
    {
        Assert.Throws< XISFException >( () => Metadata( "<Metadata><Property id=\"a\" type=\"BadType\" value=\"1\"/></Metadata>" ) );
    }

    /// <summary>The description reports the property count.</summary>
    [ Fact ]
    public void Description()
    {
        string xml = "<Metadata><Property id=\"XISF:CreatorApplication\" type=\"String\">PixInsight</Property><Property id=\"XISF:CompressionLevel\" type=\"Int32\" value=\"75\"/></Metadata>";

        Assert.Equal( "XISFMetadata { properties: 2 }", Metadata( xml ).ToString() );
    }

    /// <summary>A default-constructed metadata value is safe to use.</summary>
    [ Fact ]
    public void DefaultMetadataIsSafe()
    {
        XISFMetadata metadata = default;

        Assert.Empty( metadata.Properties );
        Assert.Null( metadata[ "x" ] );
        Assert.Equal( "XISFMetadata { properties: 0 }", metadata.ToString() );
    }
}
