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
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFImage"/>: parsing geometry, format and identity metadata,
/// the enumerated-value defaults and strict/lenient fallback, floating-point bounds, the
/// pixel-size validation, nested properties/keywords and the optional child elements.
/// </summary>
public class XISFImageTests
{
    /// <summary>Parses an image from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;Image&gt;</c> XML fragment.</param>
    /// <param name="fileData">The complete file bytes, for an attachment pixel block.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed image.</returns>
    private static XISFImage Image( string xml, ReadOnlyMemory< byte > fileData = default, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFImage( XISFXmlParser.Parse( xml ), fileData, null, options );
    }

    /// <summary>A grayscale UInt8 image parses its geometry, format and pixel bytes.</summary>
    [ Fact ]
    public void ParsesGrayUInt8Image()
    {
        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" colorSpace=\"Gray\" location=\"inline:hex\">01020304</Image>" );

        Assert.Equal( new long[] { 2, 2 }, image.Geometry.Dimensions );
        Assert.Equal( 1, image.Geometry.ChannelCount );
        Assert.Equal( XISFSampleFormat.UInt8, image.SampleFormat );
        Assert.Equal( XISFColorSpace.Gray, image.ColorSpace );
        Assert.Equal( new byte[] { 0x01, 0x02, 0x03, 0x04 }, image.Data.ToArray() );
    }

    /// <summary>Absent enumerated attributes fall back to their defaults, and bounds is absent.</summary>
    [ Fact ]
    public void AppliesDefaultsWhenAttributesAbsent()
    {
        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01020304</Image>" );

        Assert.Equal( XISFColorSpace.Gray, image.ColorSpace );
        Assert.Equal( XISFPixelStorage.Planar, image.PixelStorage );
        Assert.Equal( XISFByteOrder.Little, image.ByteOrder );
        Assert.Null( image.Bounds );
    }

    /// <summary>Planar and normal pixel storage are distinguished.</summary>
    [ Fact ]
    public void ParsesRgbWithPlanarAndNormalStorage()
    {
        XISFImage planar = Image( "<Image geometry=\"1:1:3\" sampleFormat=\"UInt8\" colorSpace=\"RGB\" pixelStorage=\"Planar\" location=\"inline:hex\">010203</Image>" );
        XISFImage normal = Image( "<Image geometry=\"1:1:3\" sampleFormat=\"UInt8\" colorSpace=\"RGB\" pixelStorage=\"Normal\" location=\"inline:hex\">010203</Image>" );

        Assert.Equal( XISFColorSpace.Rgb, planar.ColorSpace );
        Assert.Equal( XISFPixelStorage.Planar, planar.PixelStorage );
        Assert.Equal( XISFPixelStorage.Normal, normal.PixelStorage );
    }

    /// <summary>A floating-point image parses its bounds and pixel bytes.</summary>
    [ Fact ]
    public void ParsesFloatImageWithBounds()
    {
        XISFImage image = Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" byteOrder=\"little\" bounds=\"0:1\" location=\"inline:hex\">0000803f</Image>" );

        Assert.Equal( XISFSampleFormat.Float32, image.SampleFormat );
        Assert.Equal( XISFByteOrder.Little, image.ByteOrder );
        Assert.NotNull( image.Bounds );
        Assert.Equal( ( 0.0, 1.0 ), image.Bounds.Value );
        Assert.Equal( new byte[] { 0x00, 0x00, 0x80, 0x3F }, image.Data.ToArray() );
    }

    /// <summary>A floating-point image without bounds is rejected under strict parsing.</summary>
    [ Fact ]
    public void RequiresBoundsForFloatingPointWhenStrict()
    {
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" location=\"inline:hex\">0000803f</Image>" ) );
    }

    /// <summary>A floating-point image without bounds is tolerated under lenient parsing.</summary>
    [ Fact ]
    public void ToleratesMissingBoundsForFloatingPointWhenLenient()
    {
        XISFImage image = Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" location=\"inline:hex\">0000803f</Image>", options: XISFParsingOptions.Lenient );

        Assert.Null( image.Bounds );
    }

    /// <summary>A bounds attribute with a non-ordered or NaN range is rejected.</summary>
    [ Fact ]
    public void RejectsMalformedBounds()
    {
        // low greater than high.
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"1:0\" location=\"inline:hex\">0000803f</Image>" ) );

        // A NaN component: neither low <= high nor high <= low holds, so the range is not ordered.
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"NaN:NaN\" location=\"inline:hex\">0000803f</Image>" ) );
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"NaN:1\" location=\"inline:hex\">0000803f</Image>" ) );
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"1:NaN\" location=\"inline:hex\">0000803f</Image>" ) );
    }

    /// <summary>A pixel byte count inconsistent with the geometry is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsPixelSizeMismatchWhenStrict()
    {
        // geometry 2:2:1 UInt8 expects 4 bytes; only 3 are provided.
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">010203</Image>" ) );
    }

    /// <summary>The optional identity attributes are captured.</summary>
    [ Fact ]
    public void ParsesOptionalIdentityAttributes()
    {
        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" id=\"main\" uuid=\"abc-123\" imageType=\"Light\" orientation=\"0\" location=\"inline:hex\">01020304</Image>" );

        Assert.Equal( "main", image.Id );
        Assert.Equal( "abc-123", image.Uuid );
        Assert.Equal( "Light", image.ImageType );
        Assert.Equal( "0", image.Orientation );
    }

    /// <summary>Nested properties and FITS keywords are parsed.</summary>
    [ Fact ]
    public void ParsesNestedPropertiesAndKeywords()
    {
        string    xml   = "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"embedded\"><Data encoding=\"hex\">01020304</Data><Property id=\"P\" type=\"Int32\" value=\"7\"/><FITSKeyword name=\"EXPTIME\" value=\"1.0\" comment=\"exposure\"/></Image>";
        XISFImage image = Image( xml );

        IReadOnlyList< XISFProperty >    properties = image.Properties;
        IReadOnlyList< XISFFitsKeyword > keywords   = image.Keywords;

        Assert.Single( properties );
        Assert.Equal( XISFValue.Integer( 7 ), properties[ 0 ].Value );
        Assert.Single( keywords );
        Assert.Equal( "EXPTIME", keywords[ 0 ].Name );
        Assert.Equal( new byte[] { 0x01, 0x02, 0x03, 0x04 }, image.Data.ToArray() );
    }

    /// <summary>A missing geometry or sample format is rejected.</summary>
    [ Fact ]
    public void RejectsMissingGeometryOrSampleFormat()
    {
        Assert.Throws< XISFException >( () => Image( "<Image sampleFormat=\"UInt8\" location=\"inline:hex\">01</Image>" ) );
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"1:1:1\" location=\"inline:hex\">01</Image>" ) );
    }

    /// <summary>An unknown enumerated value is rejected under strict and falls back under lenient.</summary>
    [ Fact ]
    public void EnumeratedValueFallsBackWhenLenient()
    {
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" colorSpace=\"Bogus\" location=\"inline:hex\">01020304</Image>" ) );

        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" colorSpace=\"Bogus\" location=\"inline:hex\">01020304</Image>", options: XISFParsingOptions.Lenient );

        Assert.Equal( XISFColorSpace.Gray, image.ColorSpace );
    }

    /// <summary>All optional child metadata elements are attached.</summary>
    [ Fact ]
    public void AttachesChildMetadataElements()
    {
        string xml = "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01020304"
            + "<ICCProfile location=\"inline:hex\">deadbeef</ICCProfile>"
            + "<ColorFilterArray pattern=\"GRBG\" width=\"2\" height=\"2\"/>"
            + "<Resolution horizontal=\"72\" vertical=\"72\"/>"
            + "<RGBWorkingSpace x=\"0.64:0.30:0.15\" y=\"0.33:0.60:0.06\" Y=\"0.22:0.71:0.06\" gamma=\"2.2\"/>"
            + "<DisplayFunction m=\"0.5:0.5:0.5:0.5\" s=\"0:0:0:0\" h=\"1:1:1:1\" l=\"0:0:0:0\" r=\"1:1:1:1\"/>"
            + "<Thumbnail geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">05</Thumbnail>"
            + "</Image>";
        XISFImage image = Image( xml );

        Assert.NotNull( image.IccProfile );
        Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, image.IccProfile.Data.ToArray() );
        Assert.Equal( "GRBG", image.ColorFilterArray?.Pattern );
        Assert.Equal( 72.0, image.Resolution?.Horizontal );
        Assert.Equal( XISFRgbWorkingSpace.Gamma.Exponent( 2.2 ), image.RgbWorkingSpace?.GammaFunction );
        Assert.Null( image.DisplayFunction?.Name );
        Assert.NotNull( image.Thumbnail );
        Assert.Equal( new byte[] { 0x05 }, image.Thumbnail.Data.ToArray() );
    }

    /// <summary>Absent optional child metadata elements are null.</summary>
    [ Fact ]
    public void HasNullMetadataElementsWhenAbsent()
    {
        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01020304</Image>" );

        Assert.Null( image.IccProfile );
        Assert.Null( image.ColorFilterArray );
        Assert.Null( image.Resolution );
        Assert.Null( image.RgbWorkingSpace );
        Assert.Null( image.DisplayFunction );
        Assert.Null( image.Thumbnail );
    }

    /// <summary>A malformed child metadata element is fatal under strict parsing.</summary>
    [ Fact ]
    public void RejectsMalformedChildMetadataWhenStrict()
    {
        Assert.Throws< XISFException >( () => Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01020304<Resolution horizontal=\"72\"/></Image>" ) );
    }

    /// <summary>A malformed child metadata element is dropped under lenient parsing.</summary>
    [ Fact ]
    public void DropsMalformedChildMetadataWhenLenient()
    {
        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01020304<Resolution horizontal=\"72\"/></Image>", options: XISFParsingOptions.Lenient );

        Assert.Null( image.Resolution );
    }

    /// <summary>The description summarizes the image geometry and format.</summary>
    [ Fact ]
    public void Description()
    {
        XISFImage image = Image( "<Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" colorSpace=\"Gray\" location=\"inline:hex\">01020304</Image>" );

        Assert.Equal( "XISFImage { geometry: 2x2:1, sampleFormat: UInt8, colorSpace: Gray, pixelStorage: Planar, byteOrder: little }", image.ToString() );
    }

    /// <summary>Bounds parsing is culture-invariant.</summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            XISFImage image = Image( "<Image geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"0.5:1.5\" location=\"inline:hex\">0000803f</Image>" );

            Assert.NotNull( image.Bounds );
            Assert.Equal( ( 0.5, 1.5 ), image.Bounds.Value );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
