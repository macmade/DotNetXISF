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
using System.Linq;
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Confirms that every public type provides a readable string form: the
/// spec-valued enums describe as their spec token, the structured types describe
/// as a readable, non-empty summary, and a synthetic unit exercising the full
/// complement of ancillary elements confirms each aggregate type describes.
/// </summary>
public class DescriptionsTests
{
    /// <summary>
    /// Every byte-order value has a non-empty spec token, and little-endian's is
    /// the lowercase spec string.
    /// </summary>
    [ Fact ]
    public void ByteOrderSpecTokens()
    {
        foreach( XISFByteOrder value in Enum.GetValues< XISFByteOrder >() )
        {
            Assert.False( string.IsNullOrEmpty( value.SpecToken() ) );
        }

        Assert.Equal( "little", XISFByteOrder.Little.SpecToken() );
        Assert.Equal( "big",    XISFByteOrder.Big.SpecToken() );
    }

    /// <summary>
    /// Every color-space value has a non-empty spec token, and RGB's is the
    /// uppercase spec string.
    /// </summary>
    [ Fact ]
    public void ColorSpaceSpecTokens()
    {
        foreach( XISFColorSpace value in Enum.GetValues< XISFColorSpace >() )
        {
            Assert.False( string.IsNullOrEmpty( value.SpecToken() ) );
        }

        Assert.Equal( "Gray",   XISFColorSpace.Gray.SpecToken() );
        Assert.Equal( "RGB",    XISFColorSpace.Rgb.SpecToken() );
        Assert.Equal( "CIELab", XISFColorSpace.CieLab.SpecToken() );
    }

    /// <summary>
    /// Every pixel-storage value has a non-empty spec token, and planar's is its
    /// spec string.
    /// </summary>
    [ Fact ]
    public void PixelStorageSpecTokens()
    {
        foreach( XISFPixelStorage value in Enum.GetValues< XISFPixelStorage >() )
        {
            Assert.False( string.IsNullOrEmpty( value.SpecToken() ) );
        }

        Assert.Equal( "Planar", XISFPixelStorage.Planar.SpecToken() );
        Assert.Equal( "Normal", XISFPixelStorage.Normal.SpecToken() );
    }

    /// <summary>
    /// Every sample-format value has a non-empty spec token, and Float32's is its
    /// spec string.
    /// </summary>
    [ Fact ]
    public void SampleFormatSpecTokens()
    {
        foreach( XISFSampleFormat value in Enum.GetValues< XISFSampleFormat >() )
        {
            Assert.False( string.IsNullOrEmpty( value.SpecToken() ) );
        }

        Assert.Equal( "Float32", XISFSampleFormat.Float32.SpecToken() );
    }

    /// <summary>
    /// Every property-type value has a non-empty spec token, and the byte-vector
    /// token round-trips.
    /// </summary>
    [ Fact ]
    public void PropertyTypeSpecTokens()
    {
        foreach( XISFPropertyType value in Enum.GetValues< XISFPropertyType >() )
        {
            Assert.False( string.IsNullOrEmpty( value.SpecToken() ) );
        }

        Assert.Equal( "UI8Vector", XISFPropertyType.UI8Vector.SpecToken() );
    }

    /// <summary>
    /// A geometry describes as its <c>d1:...:dN:channels</c> attribute form.
    /// </summary>
    [ Fact ]
    public void GeometryDescription()
    {
        Assert.Equal( "2159:3839:3", new XISFGeometry( "2159:3839:3" ).ToString() );
        Assert.Equal( "4:4:4:1",     new XISFGeometry( "4:4:4:1" ).ToString() );
    }

    /// <summary>A value describes as <c>Kind(payload)</c> for each of the eight kinds.</summary>
    [ Fact ]
    public void ValueDescriptions()
    {
        Assert.Equal( "Boolean(true)",           XISFValue.Boolean( true ).ToString() );
        Assert.Equal( "Boolean(false)",          XISFValue.Boolean( false ).ToString() );
        Assert.Equal( "Integer(5)",              XISFValue.Integer( 5 ).ToString() );
        Assert.Equal( "Integer(-42)",            XISFValue.Integer( -42 ).ToString() );
        Assert.Equal( "Unsigned Integer(65535)", XISFValue.UnsignedInteger( 65535 ).ToString() );
        Assert.Equal( "Float(1.5)",              XISFValue.Float( 1.5 ).ToString() );
        Assert.Equal( "Complex(1, 2)",           XISFValue.Complex( 1, 2 ).ToString() );
        Assert.Equal( "String(\"hi\")",          XISFValue.String( "hi" ).ToString() );
        Assert.Equal( "Data(3 bytes)",           XISFValue.Data( new byte[] { 0x01, 0x02, 0x03 } ).ToString() );

        // A time point describes in round-trip ISO 8601 form.
        Assert.Equal( "Time Point(2021-01-02T03:04:05.0000000+00:00)", XISFValue.TimePoint( new DateTimeOffset( 2021, 1, 2, 3, 4, 5, TimeSpan.Zero ) ).ToString() );
    }

    /// <summary>
    /// Value descriptions format numbers invariantly, regardless of the current
    /// culture (a comma-decimal culture must not change the output).
    /// </summary>
    [ Fact ]
    public void ValueDescriptionIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            Assert.Equal( "Float(1.5)",         XISFValue.Float( 1.5 ).ToString() );
            Assert.Equal( "Complex(1.5, -2.5)", XISFValue.Complex( 1.5, -2.5 ).ToString() );
            Assert.Equal( "Time Point(2021-01-02T03:04:05.0000000+00:00)", XISFValue.TimePoint( new DateTimeOffset( 2021, 1, 2, 3, 4, 5, TimeSpan.Zero ) ).ToString() );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    /// <summary>A data-block location describes as its <c>location</c> attribute form.</summary>
    [ Fact ]
    public void DataBlockLocationDescription()
    {
        Assert.Equal( "inline:base64",                XISFDataBlockLocation.Inline( XISFDataBlockLocation.Encoding.Base64 ).ToString() );
        Assert.Equal( "inline:hex",                   XISFDataBlockLocation.Inline( XISFDataBlockLocation.Encoding.Hex ).ToString() );
        Assert.Equal( "embedded",                     XISFDataBlockLocation.Embedded().ToString() );
        Assert.Equal( "attachment:4570:1428362",      XISFDataBlockLocation.Attachment( 4570, 1428362 ).ToString() );
        Assert.Equal( "path(/data/x.bin)",            XISFDataBlockLocation.AbsolutePath( "/data/x.bin", null ).ToString() );
        Assert.Equal( "path(/data/x.bin):5",          XISFDataBlockLocation.AbsolutePath( "/data/x.bin", 5 ).ToString() );
        Assert.Equal( "path(@header_dir/rel/x.bin)",  XISFDataBlockLocation.HeaderRelativePath( "rel/x.bin", null ).ToString() );
    }

    /// <summary>A data block describes with its location and codec/checksum state.</summary>
    [ Fact ]
    public void DataBlockDescription()
    {
        XISFElement   element = XISFXmlParser.Parse( "<Image location=\"inline:base64\">SGVsbG8=</Image>" );
        XISFDataBlock block   = new XISFDataBlock( element, ReadOnlyMemory< byte >.Empty, null, XISFParsingOptions.Strict );
        string        text    = block.ToString();

        Assert.Contains( "inline:base64",     text, StringComparison.Ordinal );
        Assert.Contains( "compression: none", text, StringComparison.Ordinal );
        Assert.Contains( "checksum: none",    text, StringComparison.Ordinal );
    }

    /// <summary>The checksum and compression types describe non-emptily.</summary>
    [ Fact ]
    public void ChecksumAndCompressionDescriptions()
    {
        Assert.False( string.IsNullOrEmpty( new XISFChecksum( "sha-1:0123456789abcdef0123456789abcdef01234567" ).ToString() ) );
        Assert.False( string.IsNullOrEmpty( new XISFCompression( "zlib:1000" ).ToString() ) );
    }

    /// <summary>The color types built from an element describe non-emptily.</summary>
    [ Fact ]
    public void ColorTypesWithElementInitDescriptions()
    {
        XISFRgbWorkingSpace rgbWorkingSpace = new XISFRgbWorkingSpace(
            XISFXmlParser.Parse( "<RGBWorkingSpace x=\"0.64:0.3:0.15\" y=\"0.33:0.6:0.06\" Y=\"0.2126:0.7152:0.0722\" gamma=\"2.2\"/>" ),
            XISFParsingOptions.Strict
        );

        XISFColorFilterArray colorFilterArray = new XISFColorFilterArray(
            XISFXmlParser.Parse( "<ColorFilterArray pattern=\"RGGB\" width=\"2\" height=\"2\"/>" ),
            XISFParsingOptions.Strict
        );

        Assert.False( string.IsNullOrEmpty( rgbWorkingSpace.ToString() ) );
        Assert.False( string.IsNullOrEmpty( colorFilterArray.ToString() ) );
    }

    /// <summary>
    /// A synthetic monolithic unit exercising the full complement of describable
    /// public types: an image carrying every ancillary element, plus unit-level
    /// metadata.
    /// </summary>
    /// <returns>The parsed rich file.</returns>
    private static XISFFile RichFile()
    {
        const string pixels = "0102030405060708090a0b0c"; // 2 x 2 x 3 UInt8 = 12 bytes
        string       xml    =
            "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\">"
            + "<Image geometry=\"2:2:3\" sampleFormat=\"UInt8\" colorSpace=\"RGB\" location=\"inline:hex\">" + pixels
            + "<Resolution horizontal=\"72\" vertical=\"72\" unit=\"inch\"/>"
            + "<DisplayFunction m=\"0.5:0.5:0.5:0.5\" s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\"/>"
            + "<RGBWorkingSpace x=\"0.64:0.3:0.15\" y=\"0.33:0.6:0.06\" Y=\"0.2126:0.7152:0.0722\" gamma=\"2.2\"/>"
            + "<ColorFilterArray pattern=\"RGGB\" width=\"2\" height=\"2\"/>"
            + "<ICCProfile location=\"inline:base64\">AAAA</ICCProfile>"
            + "<Thumbnail geometry=\"2:2:3\" sampleFormat=\"UInt8\" colorSpace=\"RGB\" location=\"inline:hex\">" + pixels + "</Thumbnail>"
            + "<Property id=\"Image:Prop\" type=\"Int32\" value=\"7\"/>"
            + "<FITSKeyword name=\"EXPTIME\" value=\"30.0\" comment=\"exp\"/>"
            + "</Image>"
            + "<Metadata><Property id=\"XISF:CreatorApplication\" type=\"String\">PixInsight</Property></Metadata>"
            + "</xisf>";

        return new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );
    }

    /// <summary>
    /// Every aggregate type parsed from the rich file - the file, the image, each
    /// ancillary element, the first property and keyword, and the metadata -
    /// describes non-emptily.
    /// </summary>
    [ Fact ]
    public void AggregateTypeDescriptions()
    {
        XISFFile  file  = RichFile();
        XISFImage image = file.Images.First();

        Assert.False( string.IsNullOrEmpty( file.ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.ToString() ) );

        Assert.NotNull( image.Resolution );
        Assert.NotNull( image.DisplayFunction );
        Assert.NotNull( image.RgbWorkingSpace );
        Assert.NotNull( image.ColorFilterArray );
        Assert.NotNull( image.IccProfile );
        Assert.NotNull( image.Thumbnail );

        Assert.False( string.IsNullOrEmpty( image.Resolution.Value.ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.DisplayFunction.Value.ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.RgbWorkingSpace.Value.ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.ColorFilterArray.Value.ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.IccProfile.ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.Thumbnail.ToString() ) );

        Assert.NotEmpty( image.Properties );
        Assert.NotEmpty( image.Keywords );
        Assert.False( string.IsNullOrEmpty( image.Properties.First().ToString() ) );
        Assert.False( string.IsNullOrEmpty( image.Keywords.First().ToString() ) );

        Assert.NotNull( file.Metadata );
        Assert.False( string.IsNullOrEmpty( file.Metadata.Value.ToString() ) );
    }
}
