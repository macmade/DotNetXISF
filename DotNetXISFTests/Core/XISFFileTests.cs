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
using System.IO;
using System.Linq;
using System.Text;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFFile"/>: preamble and XML-header validation, the
/// strict/lenient reserved-field and version handling, the path and byte-buffer
/// entry points, the parsed properties/keywords/images/metadata exposure, and the
/// real-fixture integration parses against the repository-root sample files.
/// </summary>
public class XISFFileTests
{
    /// <summary>
    /// The preamble constants hold the exact values the XISF standard fixes: the
    /// <c>XISF0100</c> signature, a 16-byte preamble, and the PixInsight XISF
    /// namespace URI.
    /// </summary>
    [ Fact ]
    public void PreambleConstantsHaveTheirExpectedValues()
    {
        Assert.Equal( "XISF0100",                       XISFFile.Signature );
        Assert.Equal( 16,                               XISFFile.PreambleSize );
        Assert.Equal( "http://www.pixinsight.com/xisf", XISFFile.Namespace );
    }

    // MARK: - Preamble & header validation

    /// <summary>A well-formed preamble and header parse, exposing the verbatim XML.</summary>
    [ Fact ]
    public void ParsesValidPreamble()
    {
        string   xml  = "<?xml version=\"1.0\"?><xisf version=\"1.0\"/>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Equal( xml, file.HeaderXml );
    }

    /// <summary>Bytes attached after the header do not affect header parsing.</summary>
    [ Fact ]
    public void ParsesHeaderRegardlessOfAttachment()
    {
        string                 xml        = "<?xml version=\"1.0\"?><xisf version=\"1.0\"/>";
        ReadOnlyMemory< byte > attachment = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        XISFFile               file       = new XISFFile( TestUtilities.MonolithicFile( xml: xml, attachment: attachment ), XISFParsingOptions.Strict );

        Assert.Equal( xml, file.HeaderXml );
    }

    /// <summary>A file whose signature is not <c>XISF0100</c> is rejected.</summary>
    [ Fact ]
    public void RejectsBadSignature()
    {
        ReadOnlyMemory< byte > raw = TestUtilities.MonolithicFile( signature: "XISF0101" );

        Assert.Throws< XISFException >( () => new XISFFile( raw, XISFParsingOptions.Strict ) );
    }

    /// <summary>Empty data is rejected.</summary>
    [ Fact ]
    public void RejectsEmptyData()
    {
        Assert.Throws< XISFException >( () => new XISFFile( ReadOnlyMemory< byte >.Empty, XISFParsingOptions.Strict ) );
    }

    /// <summary>A file shorter than the signature or the 16-byte preamble is rejected.</summary>
    [ Fact ]
    public void RejectsTruncatedFile()
    {
        Assert.Throws< XISFException >( () => new XISFFile( Encoding.ASCII.GetBytes( "XISF" ),     XISFParsingOptions.Strict ) );
        Assert.Throws< XISFException >( () => new XISFFile( Encoding.ASCII.GetBytes( "XISF0100" ), XISFParsingOptions.Strict ) );
    }

    /// <summary>A non-zero reserved preamble field is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsNonZeroReservedWhenStrict()
    {
        ReadOnlyMemory< byte > raw = TestUtilities.MonolithicFile( reserved: 1 );

        Assert.Throws< XISFException >( () => new XISFFile( raw, XISFParsingOptions.Strict ) );
    }

    /// <summary>A non-zero reserved preamble field is tolerated under lenient parsing.</summary>
    [ Fact ]
    public void ToleratesNonZeroReservedWhenLenient()
    {
        ReadOnlyMemory< byte > raw  = TestUtilities.MonolithicFile( reserved: 1 );
        XISFFile               file = new XISFFile( raw, XISFParsingOptions.Lenient );

        Assert.False( string.IsNullOrEmpty( file.HeaderXml ) );
    }

    /// <summary>A header-length field that extends past the end of the file is rejected.</summary>
    [ Fact ]
    public void RejectsHeaderLengthPastEndOfFile()
    {
        ReadOnlyMemory< byte > raw = TestUtilities.MonolithicFile( headerLength: 100_000 );

        Assert.Throws< XISFException >( () => new XISFFile( raw, XISFParsingOptions.Strict ) );
    }

    /// <summary>A zero header-length field is rejected.</summary>
    [ Fact ]
    public void RejectsZeroHeaderLength()
    {
        ReadOnlyMemory< byte > raw = TestUtilities.MonolithicFile( headerLength: 0 );

        Assert.Throws< XISFException >( () => new XISFFile( raw, XISFParsingOptions.Strict ) );
    }

    // MARK: - Path entry point

    /// <summary>An XISF file is read and parsed from a file path.</summary>
    [ Fact ]
    public void ReadsFromPath()
    {
        string path = Path.Combine( Path.GetTempPath(), $"DotNetXISF-{ Guid.NewGuid() }.xisf" );

        try
        {
            File.WriteAllBytes( path, TestUtilities.MonolithicFile().ToArray() );

            XISFFile file = new XISFFile( path, XISFParsingOptions.Strict );

            Assert.False( string.IsNullOrEmpty( file.HeaderXml ) );
        }
        finally
        {
            TestUtilities.RemoveTemporaryFile( path );
        }
    }

    /// <summary>A path that does not exist is rejected.</summary>
    [ Fact ]
    public void RejectsMissingPath()
    {
        string path = Path.Combine( Path.GetTempPath(), $"DotNetXISF-missing-{ Guid.NewGuid() }.xisf" );

        Assert.Throws< XISFException >( () => new XISFFile( path, XISFParsingOptions.Strict ) );
    }

    /// <summary>
    /// A <c>path(@header_dir/...)</c> image whose pixels live in an adjacent external
    /// file resolves relative to the header file's directory.
    /// </summary>
    [ Fact ]
    public void ResolvesHeaderRelativeExternalImageFromPath()
    {
        string directory = Path.Combine( Path.GetTempPath(), $"DotNetXISF-{ Guid.NewGuid() }" );

        Directory.CreateDirectory( directory );

        try
        {
            string headerPath = Path.Combine( directory, "image.xisf" );
            string pixelsPath  = Path.Combine( directory, "pixels.bin" );
            string xml         = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"path(@header_dir/pixels.bin)\"/></xisf>";

            File.WriteAllBytes( headerPath, TestUtilities.MonolithicFile( xml: xml ).ToArray() );
            File.WriteAllBytes( pixelsPath, new byte[] { 0x01, 0x02, 0x03, 0x04 } );

            XISFFile file = new XISFFile( headerPath, XISFParsingOptions.AllowExternalLocations );

            Assert.Equal( new byte[] { 0x01, 0x02, 0x03, 0x04 }, file.Images.First().Data.ToArray() );
        }
        finally
        {
            RemoveTemporaryDirectory( directory );
        }
    }

    /// <summary>
    /// An external image opens (external resolution is lazy) but reading its pixels
    /// fails when external resolution is disabled by default.
    /// </summary>
    [ Fact ]
    public void RejectsExternalImageWhenResolutionDisabled()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"path(/nonexistent/pixels.bin)\"/></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Throws< XISFException >( () => file.Images.First().Data );
    }

    // MARK: - Parsed content

    /// <summary>The local names of the root's direct children are exposed in document order.</summary>
    [ Fact ]
    public void ExposesTopLevelElementNames()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Image geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01</Image><Property id=\"a\" type=\"Int32\" value=\"1\"/><FITSKeyword name=\"A\" value=\"1\"/></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Equal( new[] { "Image", "Property", "FITSKeyword" }, file.HeaderElementNames );
    }

    /// <summary>Top-level properties and keywords are exposed and looked up by id/name.</summary>
    [ Fact ]
    public void ExposesPropertiesAndKeywords()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Property id=\"A\" type=\"Int32\" value=\"1\"/><Property id=\"B\" type=\"String\">hi</Property><FITSKeyword name=\"EXPTIME\" value=\"10\" comment=\"exp\"/><FITSKeyword name=\"HISTORY\" value=\"\" comment=\"x\"/></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Equal( 2, file.Properties.Count );
        Assert.Equal( XISFValue.Integer( 1 ),  file[ "A" ]?.Value );
        Assert.Equal( XISFValue.String( "hi" ), file[ "B" ]?.Value );
        Assert.Null( file[ "missing" ] );
        Assert.Equal( 2, file.Keywords.Count );
        Assert.Single( file.KeywordsNamed( "EXPTIME" ) );
        Assert.Equal( "10", file.KeywordsNamed( "EXPTIME" ).First().Value );
    }

    /// <summary>Vector/matrix properties are resolved through the data-block pipeline.</summary>
    [ Fact ]
    public void ParsesDataBlockBackedProperties()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Property id=\"V\" type=\"UI8Vector\" length=\"3\" location=\"inline:base64\">AAEC</Property><Property id=\"S\" type=\"Int32\" value=\"5\"/></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Equal( 2, file.Properties.Count );
        Assert.Equal( XISFValue.Integer( 5 ), file[ "S" ]?.Value );
        Assert.Equal( XISFPropertyType.UI8Vector, file[ "V" ]?.Type );
        Assert.Equal( 3L, file[ "V" ]?.Length );
        Assert.Equal( XISFValue.Data( new byte[] { 0x00, 0x01, 0x02 } ), file[ "V" ]?.Value );
    }

    /// <summary>A single image is exposed and its pixels decode.</summary>
    [ Fact ]
    public void ExposesImages()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Image geometry=\"2:2:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01020304</Image></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Single( file.Images );
        Assert.Equal( XISFSampleFormat.UInt8, file.Images.First().SampleFormat );
        Assert.Equal( new byte[] { 0x01, 0x02, 0x03, 0x04 }, file.Images.First().Data.ToArray() );
    }

    /// <summary>Multiple images are exposed in document order.</summary>
    [ Fact ]
    public void ExposesMultipleImages()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Image geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01</Image><Image geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">02</Image></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Equal( 2, file.Images.Count );
    }

    /// <summary>A unit-level <c>Metadata</c> element is parsed and exposed.</summary>
    [ Fact ]
    public void ExposesUnitLevelMetadata()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Metadata><Property id=\"XISF:CreatorApplication\" type=\"String\">PixInsight</Property></Metadata></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.NotNull( file.Metadata );
        Assert.Single( file.Metadata.Value.Properties );
        Assert.Equal( XISFValue.String( "PixInsight" ), file.Metadata.Value[ "XISF:CreatorApplication" ]?.Value );
    }

    /// <summary>A header without a <c>Metadata</c> element yields <c>null</c> metadata.</summary>
    [ Fact ]
    public void HasNullMetadataWhenAbsent()
    {
        string   xml  = "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"/>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Null( file.Metadata );
    }

    // MARK: - Root validation

    /// <summary>A root element other than <c>xisf</c> is rejected.</summary>
    [ Fact ]
    public void RejectsWrongRoot()
    {
        string xml = "<notxisf version=\"1.0\"/>";

        Assert.Throws< XISFException >( () => new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict ) );
    }

    /// <summary>A missing <c>version</c> attribute is rejected under strict parsing.</summary>
    [ Fact ]
    public void RejectsMissingVersionWhenStrict()
    {
        string xml = "<xisf xmlns=\"http://www.pixinsight.com/xisf\"/>";

        Assert.Throws< XISFException >( () => new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict ) );
    }

    /// <summary>A missing <c>version</c> attribute is tolerated under lenient parsing.</summary>
    [ Fact ]
    public void ToleratesMissingVersionWhenLenient()
    {
        string   xml  = "<xisf xmlns=\"http://www.pixinsight.com/xisf\"/>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Lenient );

        Assert.Empty( file.HeaderElementNames );
    }

    /// <summary>A root <c>xisf</c> element in a foreign namespace is rejected.</summary>
    [ Fact ]
    public void RejectsForeignNamespace()
    {
        string xml = "<xisf version=\"1.0\" xmlns=\"http://example.com/other\"/>";

        Assert.Throws< XISFException >( () => new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict ) );
    }

    /// <summary>A header that omits the namespace declaration parses.</summary>
    [ Fact ]
    public void ParsesNonNamespacedHeader()
    {
        string   xml  = "<xisf version=\"1.0\"><Image geometry=\"1:1:1\" sampleFormat=\"UInt8\" location=\"inline:hex\">01</Image></xisf>";
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict );

        Assert.Equal( new[] { "Image" }, file.HeaderElementNames );
    }

    /// <summary>A not-well-formed XML header is rejected.</summary>
    [ Fact ]
    public void RejectsMalformedHeaderXml()
    {
        string xml = "<xisf version=\"1.0\"><unclosed></xisf>";

        Assert.Throws< XISFException >( () => new XISFFile( TestUtilities.MonolithicFile( xml: xml ), XISFParsingOptions.Strict ) );
    }

    /// <summary>The description is a non-empty, readable summary.</summary>
    [ Fact ]
    public void HasReadableDescription()
    {
        XISFFile file = new XISFFile( TestUtilities.MonolithicFile(), XISFParsingOptions.Strict );

        Assert.False( string.IsNullOrEmpty( file.ToString() ) );
        Assert.Contains( "XISFFile", file.ToString(), StringComparison.Ordinal );
    }

    // MARK: - Real-fixture integration tests

    /// <summary>The plain autocrop fixture: an RGB integration image plus a Gray crop mask.</summary>
    private static string? AutocropFixture => TestUtilities.TestFiles.FirstOrDefault( path => Path.GetFileName( path ).Contains( "autocrop.xisf", StringComparison.Ordinal ) );

    /// <summary>
    /// The corrected fixture: a single RGB image carrying a thumbnail, ICC profile,
    /// resolution and display function.
    /// </summary>
    private static string? CorrectedFixture => TestUtilities.TestFiles.FirstOrDefault( path => Path.GetFileName( path ).Contains( "corrected.xisf", StringComparison.Ordinal ) );

    /// <summary>Every sample file parses under lenient options.</summary>
    [ Fact ]
    public void ParsesAllTestFiles()
    {
        Assert.NotEmpty( TestUtilities.TestFiles );

        foreach( string path in TestUtilities.TestFiles )
        {
            _ = new XISFFile( path, XISFParsingOptions.Lenient );
        }
    }

    /// <summary>Every sample file parses under strict options.</summary>
    [ Fact ]
    public void ParsesAllTestFilesStrictly()
    {
        Assert.NotEmpty( TestUtilities.TestFiles );

        foreach( string path in TestUtilities.TestFiles )
        {
            _ = new XISFFile( path, XISFParsingOptions.Strict );
        }
    }

    /// <summary>Every image in every sample file declares a self-consistent geometry.</summary>
    [ Fact ]
    public void EveryImageDeclaresConsistentGeometry()
    {
        foreach( string path in TestUtilities.TestFiles )
        {
            XISFFile file = new XISFFile( path, XISFParsingOptions.Lenient );

            foreach( XISFImage image in file.Images )
            {
                Assert.NotEmpty( image.Geometry.Dimensions );
                Assert.True( image.Geometry.ChannelCount >= 1 );
                Assert.Equal( image.Geometry.PixelCount * image.Geometry.ChannelCount, image.Geometry.SampleCount );
                Assert.True( image.SampleFormat.BytesPerSample() >= 1 );
            }
        }
    }

    /// <summary>The autocrop fixture's two images carry the expected typed metadata.</summary>
    [ Fact ]
    public void ParsesAutocropFixture()
    {
        string? path = AutocropFixture;

        Assert.NotNull( path );

        XISFFile file = new XISFFile( path, XISFParsingOptions.Lenient );

        Assert.Equal( 2, file.Images.Count );

        XISFImage integration = file.Images[ 0 ];
        XISFImage mask        = file.Images[ 1 ];

        Assert.Equal( "integration_autocrop",      integration.Id );
        Assert.Equal( new long[] { 578, 1547 },    integration.Geometry.Dimensions );
        Assert.Equal( 3L,                          integration.Geometry.ChannelCount );
        Assert.Equal( XISFSampleFormat.Float32,    integration.SampleFormat );
        Assert.Equal( XISFColorSpace.Rgb,          integration.ColorSpace );
        Assert.Equal( XISFByteOrder.Little,        integration.ByteOrder );
        Assert.NotNull( integration.Bounds );
        Assert.Equal( ( 0.0, 1.0 ),                integration.Bounds.Value );
        Assert.Equal( 28,                          integration.Keywords.Count );
        Assert.Equal( 89,                          integration.Properties.Count );

        Assert.Equal( "crop_mask",                 mask.Id );
        Assert.Equal( new long[] { 1080, 1920 },   mask.Geometry.Dimensions );
        Assert.Equal( 1L,                          mask.Geometry.ChannelCount );
        Assert.Equal( XISFSampleFormat.Float32,    mask.SampleFormat );
        Assert.Equal( XISFColorSpace.Gray,         mask.ColorSpace );

        // A scalar property and a FITS keyword parsed from the real header.
        Assert.Equal( "LP",  integration.Properties.First( property => property.Id == "Instrument:Filter:Name" ).Value.AsString );
        Assert.Equal( 2.9,   integration.Properties.First( property => property.Id == "Instrument:Sensor:XPixelSize" ).Value.AsFloat );
        Assert.Equal( "'LP'", integration.Keywords.First( keyword => keyword.Name == "FILTER" ).Value );

        // The unit-level metadata element is present.
        Assert.NotNull( file.Metadata );
        Assert.NotNull( file.Metadata.Value[ "XISF:CreatorApplication" ]?.Value.AsString );
    }

    /// <summary>The autocrop fixture's decoded pixel bytes match geometry x bytes-per-sample.</summary>
    [ Fact ]
    public void ParsesAutocropFixturePixelData()
    {
        string? path = AutocropFixture;

        Assert.NotNull( path );

        XISFFile  file        = new XISFFile( path, XISFParsingOptions.Lenient );
        XISFImage integration = file.Images.First();
        long      expected    = integration.Geometry.SampleCount * integration.SampleFormat.BytesPerSample();

        Assert.Equal( expected, ( long )integration.Data.Length );
        Assert.Equal( 578L * 1547 * 3 * 4, expected );
    }

    /// <summary>The corrected fixture's single image carries the full complement of ancillary elements.</summary>
    [ Fact ]
    public void ParsesCorrectedFixture()
    {
        string? path = CorrectedFixture;

        Assert.NotNull( path );

        XISFFile file = new XISFFile( path, XISFParsingOptions.Lenient );

        Assert.Single( file.Images );

        XISFImage image = file.Images.First();

        Assert.Equal( "drizzle_integration",     image.Id );
        Assert.Equal( new long[] { 1080, 1920 }, image.Geometry.Dimensions );
        Assert.Equal( 3L,                        image.Geometry.ChannelCount );
        Assert.Equal( XISFSampleFormat.Float32,  image.SampleFormat );
        Assert.Equal( XISFColorSpace.Rgb,        image.ColorSpace );
        Assert.NotNull( image.Bounds );
        Assert.Equal( ( 0.0, 1.0 ),              image.Bounds.Value );
        Assert.Equal( 27,                        image.Keywords.Count );

        Assert.NotNull( image.Thumbnail );
        Assert.NotNull( image.IccProfile );
        Assert.NotNull( image.Resolution );
        Assert.NotNull( image.DisplayFunction );

        Assert.Equal( 72.0,                       image.Resolution.Value.Horizontal );
        Assert.Equal( 72.0,                       image.Resolution.Value.Vertical );
        Assert.Equal( XISFResolution.Unit.Inch,   image.Resolution.Value.ResolutionUnit );

        // The thumbnail is a small 8-bit RGB image.
        XISFThumbnail? thumbnail = image.Thumbnail;

        Assert.NotNull( thumbnail );
        Assert.Equal( new long[] { 225, 400 },  thumbnail.Image.Geometry.Dimensions );
        Assert.Equal( 3L,                       thumbnail.Image.Geometry.ChannelCount );
        Assert.Equal( XISFSampleFormat.UInt8,   thumbnail.Image.SampleFormat );
        Assert.Equal( XISFColorSpace.Rgb,       thumbnail.Image.ColorSpace );

        Assert.NotNull( file.Metadata );
    }

    /// <summary>The corrected fixture's inline ICC profile and attached thumbnail decode to bytes.</summary>
    [ Fact ]
    public void DecodesCorrectedFixtureAncillaryData()
    {
        string? path = CorrectedFixture;

        Assert.NotNull( path );

        XISFFile  file  = new XISFFile( path, XISFParsingOptions.Lenient );
        XISFImage image = file.Images.First();

        XISFIccProfile? iccProfile = image.IccProfile;
        XISFThumbnail?  thumbnail  = image.Thumbnail;

        Assert.NotNull( iccProfile );
        Assert.NotNull( thumbnail );
        Assert.False( iccProfile.Data.IsEmpty );
        Assert.Equal( 225 * 400 * 3, thumbnail.Data.Length );
    }

    /// <summary>
    /// Removes a temporary directory created by a test and all of its contents,
    /// ignoring any failure (cleanup is best-effort).
    /// </summary>
    /// <param name="path">The path of the temporary directory to remove.</param>
    private static void RemoveTemporaryDirectory( string path )
    {
        try
        {
            Directory.Delete( path, true );
        }
        catch( Exception )
        {
            // Cleanup is best-effort: a deletion failure must not mask the test's
            // own result.
        }
    }
}
