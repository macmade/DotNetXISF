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
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for the external/distributed resolution of <see cref="XISFDataBlock"/>:
/// absolute and header-relative <c>path(...)</c> whole-file and <c>.xisb</c>-indexed
/// blocks, the opt-in gating, and the missing-file / no-base-directory / remote-URL
/// rejections.
/// </summary>
public class XISFExternalDataBlockTests
{
    /// <summary>The parsing options that permit external-location resolution.</summary>
    private const XISFParsingOptions External = XISFParsingOptions.AllowExternalLocations;

    /// <summary>Builds a data block from a <c>location</c> attribute.</summary>
    /// <param name="location">The <c>location</c> attribute value.</param>
    /// <param name="baseDirectory">The header file's directory for <c>@header_dir</c> resolution, or <c>null</c>.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The constructed data block.</returns>
    private static XISFDataBlock Block( string location, string? baseDirectory, XISFParsingOptions options )
    {
        return new XISFDataBlock( XISFXmlParser.Parse( $"<Image location=\"{ location }\"/>" ), ReadOnlyMemory< byte >.Empty, baseDirectory, options );
    }

    /// <summary>Returns a unique temporary file path (not created) with the given extension.</summary>
    /// <param name="extension">The file extension, including the leading dot.</param>
    /// <returns>The temporary file path.</returns>
    private static string TemporaryFile( string extension ) => Path.Combine( Path.GetTempPath(), $"DotNetXISF-{ Guid.NewGuid() }{ extension }" );

    /// <summary>An absolute <c>path(...)</c> block reads the whole external file.</summary>
    [ Fact ]
    public void ResolvesAbsolutePathWholeFile()
    {
        string file = TemporaryFile( ".bin" );

        try
        {
            File.WriteAllBytes( file, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF } );

            XISFDataBlock block = Block( $"path({ file })", null, External );

            Assert.Equal( new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, block.Data.ToArray() );
        }
        finally
        {
            TestUtilities.RemoveTemporaryFile( file );
        }
    }

    /// <summary>A header-relative <c>path(@header_dir/...)</c> block reads the whole external file.</summary>
    [ Fact ]
    public void ResolvesHeaderRelativePathWholeFile()
    {
        string directory = Path.GetTempPath();
        string name      = $"DotNetXISF-{ Guid.NewGuid() }.bin";
        string file      = Path.Combine( directory, name );

        try
        {
            File.WriteAllBytes( file, new byte[] { 0x01, 0x02, 0x03 } );

            XISFDataBlock block = Block( $"path(@header_dir/{ name })", directory, External );

            Assert.Equal( new byte[] { 0x01, 0x02, 0x03 }, block.Data.ToArray() );
        }
        finally
        {
            TestUtilities.RemoveTemporaryFile( file );
        }
    }

    /// <summary>An indexed <c>path(...):id</c> block is located through the <c>.xisb</c> block index.</summary>
    [ Fact ]
    public void ResolvesDataBlocksFileByIndexId()
    {
        string directory = Path.GetTempPath();
        string name      = $"DotNetXISF-{ Guid.NewGuid() }.xisb";
        string file      = Path.Combine( directory, name );

        try
        {
            File.WriteAllBytes( file, TestUtilities.DataBlocksFile( 0x2A, new byte[] { 0x11, 0x22, 0x33, 0x44 } ).ToArray() );

            XISFDataBlock block = Block( $"path(@header_dir/{ name }):0x2a", directory, External );

            Assert.Equal( new byte[] { 0x11, 0x22, 0x33, 0x44 }, block.Data.ToArray() );
        }
        finally
        {
            TestUtilities.RemoveTemporaryFile( file );
        }
    }

    /// <summary>An index id absent from the <c>.xisb</c> block index is rejected.</summary>
    [ Fact ]
    public void RejectsUnknownIndexId()
    {
        string directory = Path.GetTempPath();
        string name      = $"DotNetXISF-{ Guid.NewGuid() }.xisb";
        string file      = Path.Combine( directory, name );

        try
        {
            File.WriteAllBytes( file, TestUtilities.DataBlocksFile( 0x2A, new byte[] { 0x11 } ).ToArray() );

            XISFDataBlock block = Block( $"path(@header_dir/{ name }):0x99", directory, External );

            Assert.Throws< XISFException >( () => block.Data );
        }
        finally
        {
            TestUtilities.RemoveTemporaryFile( file );
        }
    }

    /// <summary>External resolution is refused unless the option is enabled.</summary>
    [ Fact ]
    public void RejectsExternalResolutionWhenDisabled()
    {
        string file = TemporaryFile( ".bin" );

        try
        {
            File.WriteAllBytes( file, new byte[] { 0x01 } );

            // Constructing the block succeeds (external resolution is lazy); reading it
            // fails because the option is not enabled.
            XISFDataBlock block = Block( $"path({ file })", null, XISFParsingOptions.Strict );

            Assert.Throws< XISFException >( () => block.Data );
        }
        finally
        {
            TestUtilities.RemoveTemporaryFile( file );
        }
    }

    /// <summary>A header-relative location whose file does not exist is rejected.</summary>
    [ Fact ]
    public void RejectsMissingExternalFile()
    {
        string name = $"DotNetXISF-{ Guid.NewGuid() }.bin";

        XISFDataBlock block = Block( $"path(@header_dir/{ name })", Path.GetTempPath(), External );

        Assert.Throws< XISFException >( () => block.Data );
    }

    /// <summary>A header-relative location without a base directory is rejected.</summary>
    [ Fact ]
    public void RejectsHeaderRelativeWithoutBaseDirectory()
    {
        XISFDataBlock block = Block( "path(@header_dir/block.bin)", null, External );

        Assert.Throws< XISFException >( () => block.Data );
    }

    /// <summary>A remote (non-file) <c>url(...)</c> location is rejected.</summary>
    [ Fact ]
    public void RejectsRemoteUrl()
    {
        XISFDataBlock block = Block( "url(http://example.com/block.bin)", null, External );

        Assert.Throws< XISFException >( () => block.Data );
    }

    /// <summary>An uncompressed external block reports an unknown decoded size without reading.</summary>
    [ Fact ]
    public void UncompressedSizeUnknownForExternalUncompressedBlock()
    {
        XISFDataBlock block = Block( "path(/data/x.bin)", null, External );

        Assert.Null( block.UncompressedSize );
    }
}
