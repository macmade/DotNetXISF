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
/// Unit tests for <see cref="XISFThumbnail"/>: parsing a nested image and enforcing the
/// XISF thumbnail restrictions (sample format, color space, dimensionality, no bounds and
/// no forbidden child elements) under strict parsing.
/// </summary>
public class XISFThumbnailTests
{
    /// <summary>Parses a thumbnail from an XML fragment.</summary>
    /// <param name="xml">The <c>&lt;Thumbnail&gt;</c> XML fragment.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed thumbnail.</returns>
    private static XISFThumbnail Thumbnail( string xml, XISFParsingOptions options = XISFParsingOptions.Strict )
    {
        return new XISFThumbnail( XISFXmlParser.Parse( xml ), ReadOnlyMemory< byte >.Empty, null, options );
    }

    /// <summary>An RGB UInt8 thumbnail parses its backing image and pixel bytes.</summary>
    [ Fact ]
    public void ParsesRgbUInt8Thumbnail()
    {
        XISFThumbnail thumbnail = Thumbnail( "<Thumbnail geometry=\"1:1:3\" sampleFormat=\"UInt8\" colorSpace=\"RGB\" location=\"inline:hex\">010203</Thumbnail>" );

        Assert.Equal( new long[] { 1, 1 }, thumbnail.Image.Geometry.Dimensions );
        Assert.Equal( XISFSampleFormat.UInt8, thumbnail.Image.SampleFormat );
        Assert.Equal( XISFColorSpace.Rgb, thumbnail.Image.ColorSpace );
        Assert.Equal( new byte[] { 0x01, 0x02, 0x03 }, thumbnail.Data.ToArray() );
    }

    /// <summary>A non-UInt8/UInt16 sample format is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsUnsupportedSampleFormatWhenStrict()
    {
        // Float32 is not a permitted thumbnail sample format.
        Assert.Throws< XISFException >( () => Thumbnail( "<Thumbnail geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"0:1\" location=\"inline:hex\">0000803f</Thumbnail>" ) );
    }

    /// <summary>A thumbnail declaring bounds is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsBoundsWhenStrict()
    {
        Assert.Throws< XISFException >( () => Thumbnail( "<Thumbnail geometry=\"1:1:1\" sampleFormat=\"UInt8\" bounds=\"0:1\" location=\"inline:hex\">01</Thumbnail>" ) );
    }

    /// <summary>A thumbnail containing a color filter array is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsChildColorFilterArrayWhenStrict()
    {
        Assert.Throws< XISFException >( () => Thumbnail( "<Thumbnail geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01<ColorFilterArray pattern=\"RG\" width=\"2\" height=\"1\"/></Thumbnail>" ) );
    }

    /// <summary>A thumbnail containing a nested thumbnail is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsNestedThumbnailWhenStrict()
    {
        Assert.Throws< XISFException >( () => Thumbnail( "<Thumbnail geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01<Thumbnail geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">02</Thumbnail></Thumbnail>" ) );
    }

    /// <summary>The thumbnail restrictions are relaxed under lenient parsing.</summary>
    [ Fact ]
    public void ToleratesRestrictedThumbnailWhenLenient()
    {
        // Float32 with bounds would violate the strict restrictions, but lenient parsing accepts it.
        XISFThumbnail thumbnail = Thumbnail( "<Thumbnail geometry=\"1:1:1\" sampleFormat=\"Float32\" bounds=\"0:1\" location=\"inline:hex\">0000803f</Thumbnail>", XISFParsingOptions.Lenient );

        Assert.Equal( XISFSampleFormat.Float32, thumbnail.Image.SampleFormat );
    }

    /// <summary>The description summarizes the thumbnail.</summary>
    [ Fact ]
    public void Description()
    {
        XISFThumbnail thumbnail = Thumbnail( "<Thumbnail geometry=\"1:1:3\" sampleFormat=\"UInt8\" colorSpace=\"RGB\" location=\"inline:hex\">010203</Thumbnail>" );

        Assert.False( string.IsNullOrEmpty( thumbnail.ToString() ) );
        Assert.Contains( "XISFThumbnail {", thumbnail.ToString() );
    }
}
