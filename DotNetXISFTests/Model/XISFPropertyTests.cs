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
using System.Globalization;
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFProperty"/>: parsing scalar, complex, time-point,
/// string and data-block-backed (vector/matrix/byte-array) values, the strict/lenient
/// identifier validation, and the required-attribute rejections.
/// </summary>
public class XISFPropertyTests
{
    /// <summary>Parses a property from an XML fragment, with no attached file bytes.</summary>
    /// <param name="xml">The <c>&lt;Property&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed property.</returns>
    private static XISFProperty Property( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFProperty( XISFXmlParser.Parse( xml ), ReadOnlyMemory< byte >.Empty, null, options );
    }

    /// <summary>A scalar property carries its typed value and optional attributes.</summary>
    [ Fact ]
    public void ParsesScalarProperty()
    {
        XISFProperty property = Property( "<Property id=\"Observation:Time:Start\" type=\"Int32\" value=\"42\" comment=\"a comment\" format=\"%d\"/>" );

        Assert.Equal( "Observation:Time:Start", property.Id );
        Assert.Equal( XISFPropertyType.Int32, property.Type );
        Assert.Equal( XISFValue.Integer( 42 ), property.Value );
        Assert.Equal( "a comment", property.Comment );
        Assert.Equal( "%d", property.Format );
    }

    /// <summary>A string property with no location uses its character content.</summary>
    [ Fact ]
    public void ParsesInlineStringProperty()
    {
        XISFProperty property = Property( "<Property id=\"Title\" type=\"String\">Hello, world</Property>" );

        Assert.Equal( XISFPropertyType.String, property.Type );
        Assert.Equal( XISFValue.String( "Hello, world" ), property.Value );
    }

    /// <summary>Boolean, float, complex and unsigned-integer scalar values parse correctly.</summary>
    [ Fact ]
    public void ParsesScalarKinds()
    {
        Assert.Equal( XISFValue.Boolean( true ),          Property( "<Property id=\"a\" type=\"Boolean\" value=\"true\"/>" ).Value );
        Assert.Equal( XISFValue.Float( 1.5 ),             Property( "<Property id=\"a\" type=\"Float64\" value=\"1.5\"/>" ).Value );
        Assert.Equal( XISFValue.Complex( 1, 2 ),          Property( "<Property id=\"a\" type=\"Complex64\" value=\"(1,2)\"/>" ).Value );
        Assert.Equal( XISFValue.UnsignedInteger( 65535 ), Property( "<Property id=\"a\" type=\"UInt16\" value=\"65535\"/>" ).Value );
    }

    /// <summary>Absent <c>comment</c> and <c>format</c> attributes default to <c>null</c>.</summary>
    [ Fact ]
    public void OptionalCommentAndFormatDefaultToNull()
    {
        XISFProperty property = Property( "<Property id=\"a\" type=\"Int32\" value=\"1\"/>" );

        Assert.Null( property.Comment );
        Assert.Null( property.Format );
    }

    /// <summary>A property without an <c>id</c> attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingId()
    {
        Assert.Throws< XISFException >( () => Property( "<Property type=\"Int32\" value=\"1\"/>" ) );
    }

    /// <summary>A property without a <c>type</c> attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingType()
    {
        Assert.Throws< XISFException >( () => Property( "<Property id=\"a\" value=\"1\"/>" ) );
    }

    /// <summary>A property with an unknown type token is rejected.</summary>
    [ Fact ]
    public void RejectsUnknownType()
    {
        Assert.Throws< XISFException >( () => Property( "<Property id=\"a\" type=\"Complex128\" value=\"(1,2)\"/>" ) );
    }

    /// <summary>A scalar property without a <c>value</c> attribute is rejected.</summary>
    [ Fact ]
    public void RejectsMissingValueForScalar()
    {
        Assert.Throws< XISFException >( () => Property( "<Property id=\"a\" type=\"Int32\"/>" ) );
    }

    /// <summary>An invalid identifier is rejected under strict parsing but kept under lenient.</summary>
    [ Fact ]
    public void ValidatesIdAccordingToOptions()
    {
        Assert.Throws< XISFException >( () => Property( "<Property id=\"9bad\" type=\"Int32\" value=\"1\"/>" ) );
        Assert.Equal( "9bad", Property( "<Property id=\"9bad\" type=\"Int32\" value=\"1\"/>", XISFParsingOptions.Lenient ).Id );
    }

    /// <summary>A vector property exposes its element count and opaque decoded bytes.</summary>
    [ Fact ]
    public void ParsesVectorProperty()
    {
        // Two little-endian Float32 samples (1.0, 2.0): 8 raw bytes.
        XISFProperty property = Property( "<Property id=\"v\" type=\"F32Vector\" length=\"2\" location=\"inline:hex\">0000803f00000040</Property>" );

        Assert.Equal( XISFPropertyType.F32Vector, property.Type );
        Assert.Equal( 2L, property.Length );
        Assert.Equal( XISFValue.Data( new byte[] { 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x40 } ), property.Value );
    }

    /// <summary>A byte-array property exposes its length and opaque bytes.</summary>
    [ Fact ]
    public void ParsesByteArrayProperty()
    {
        XISFProperty property = Property( "<Property id=\"b\" type=\"ByteArray\" length=\"4\" location=\"inline:hex\">deadbeef</Property>" );

        Assert.Equal( XISFPropertyType.ByteArray, property.Type );
        Assert.Equal( 4L, property.Length );
        Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, property.Value.AsData?.ToArray() );
    }

    /// <summary>A matrix property exposes its row/column counts and opaque bytes.</summary>
    [ Fact ]
    public void ParsesMatrixProperty()
    {
        XISFProperty property = Property( "<Property id=\"m\" type=\"UI8Matrix\" rows=\"2\" columns=\"2\" location=\"inline:hex\">01020304</Property>" );

        Assert.Equal( XISFPropertyType.UI8Matrix, property.Type );
        Assert.Equal( 2L, property.Rows );
        Assert.Equal( 2L, property.Columns );
        Assert.Equal( XISFValue.Data( new byte[] { 0x01, 0x02, 0x03, 0x04 } ), property.Value );
    }

    /// <summary>A string value stored in a data block is decoded as UTF-8.</summary>
    [ Fact ]
    public void ParsesStringPropertyCarriedInDataBlock()
    {
        XISFProperty property = Property( "<Property id=\"s\" type=\"String\" location=\"inline:hex\">48656c6c6f</Property>" );

        Assert.Equal( XISFValue.String( "Hello" ), property.Value );
    }

    /// <summary>A string data block that is not valid UTF-8 is rejected.</summary>
    [ Fact ]
    public void RejectsInvalidUtf8StringDataBlock()
    {
        Assert.Throws< XISFException >( () => Property( "<Property id=\"s\" type=\"String\" location=\"inline:hex\">ff</Property>" ) );
    }

    /// <summary>The description summarizes the property's identity, type and kind.</summary>
    [ Fact ]
    public void Description()
    {
        XISFProperty property = Property( "<Property id=\"a\" type=\"Int32\" value=\"42\" comment=\"c\" format=\"%d\"/>" );

        Assert.Contains( "XISFProperty {", property.ToString() );
        Assert.Contains( "id: a, type: Int32, kind: Integer", property.ToString() );
    }

    /// <summary>A default-constructed property is safe to use.</summary>
    [ Fact ]
    public void DefaultPropertyIsSafe()
    {
        XISFProperty property = default;

        Assert.Equal( "", property.Id );
        Assert.False( string.IsNullOrEmpty( property.ToString() ) );
        Assert.Equal( property, property );
        Assert.Equal( property.GetHashCode(), property.GetHashCode() );
    }

    /// <summary>Numeric parsing is culture-invariant (float value and integer dimensions).</summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            Assert.Equal( XISFValue.Float( 1.5 ), Property( "<Property id=\"a\" type=\"Float64\" value=\"1.5\"/>" ).Value );
            Assert.Equal( 2L, Property( "<Property id=\"v\" type=\"F32Vector\" length=\"2\" location=\"inline:hex\">0000803f00000040</Property>" ).Length );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
