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
/// Unit tests for <see cref="XISFDataBlockLocation"/>: parsing of the in-file
/// (<c>inline</c>/<c>embedded</c>/<c>attachment</c>) and external
/// (<c>url(...)</c>/<c>path(...)</c>) location forms, external detection, and
/// rejection of malformed values.
/// </summary>
public class XISFDataBlockLocationTests
{
    /// <summary>An inline location parses its encoding.</summary>
    [ Fact ]
    public void ParsesInline()
    {
        Assert.Equal( XISFDataBlockLocation.Inline( XISFDataBlockLocation.Encoding.Base64 ), XISFDataBlockLocation.FromAttribute( "inline:base64" ) );
        Assert.Equal( XISFDataBlockLocation.Inline( XISFDataBlockLocation.Encoding.Hex ),    XISFDataBlockLocation.FromAttribute( "inline:hex" ) );
    }

    /// <summary>An embedded location parses with no further components.</summary>
    [ Fact ]
    public void ParsesEmbedded()
    {
        Assert.Equal( XISFDataBlockLocation.Embedded(), XISFDataBlockLocation.FromAttribute( "embedded" ) );
    }

    /// <summary>An attachment location parses its absolute position and size.</summary>
    [ Fact ]
    public void ParsesAttachment()
    {
        Assert.Equal( XISFDataBlockLocation.Attachment( 4570, 1428362 ), XISFDataBlockLocation.FromAttribute( "attachment:4570:1428362" ) );
    }

    /// <summary>Malformed in-file locations are rejected.</summary>
    [ Fact ]
    public void RejectsMalformedLocations()
    {
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "inline" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "inline:base32" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "attachment:100" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "attachment:abc:def" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "bogus" ) );
    }

    /// <summary>A URL location parses its URL and optional hex index id.</summary>
    [ Fact ]
    public void ParsesUrlLocations()
    {
        Assert.Equal( XISFDataBlockLocation.Url( new Uri( "http://example.com/f.bin" ), null ), XISFDataBlockLocation.FromAttribute( "url(http://example.com/f.bin)" ) );
        Assert.Equal( XISFDataBlockLocation.Url( new Uri( "file:///data/huge.xisb" ), 0x7a73526b ), XISFDataBlockLocation.FromAttribute( "url(file:///data/huge.xisb):0x7a73526b" ) );
    }

    /// <summary>An absolute-path location parses its path and optional decimal index id.</summary>
    [ Fact ]
    public void ParsesAbsolutePathLocations()
    {
        Assert.Equal( XISFDataBlockLocation.AbsolutePath( "/data/x.dat", null ),  XISFDataBlockLocation.FromAttribute( "path(/data/x.dat)" ) );
        Assert.Equal( XISFDataBlockLocation.AbsolutePath( "/data/x.xisb", 42 ),   XISFDataBlockLocation.FromAttribute( "path(/data/x.xisb):42" ) );
    }

    /// <summary>A header-relative-path location strips the <c>@header_dir/</c> prefix.</summary>
    [ Fact ]
    public void ParsesHeaderRelativePathLocations()
    {
        Assert.Equal( XISFDataBlockLocation.HeaderRelativePath( "blocks.xisb", 0x4d37 ), XISFDataBlockLocation.FromAttribute( "path(@header_dir/blocks.xisb):0x4d37" ) );
        Assert.Equal( XISFDataBlockLocation.HeaderRelativePath( "sub/f.dat", null ),      XISFDataBlockLocation.FromAttribute( "path(@header_dir/sub/f.dat)" ) );
    }

    /// <summary>Literal parentheses inside a path are captured; the closing parenthesis is the last one.</summary>
    [ Fact ]
    public void ParsesParenthesesWithinResource()
    {
        Assert.Equal( XISFDataBlockLocation.AbsolutePath( "/Documents/description(draft).txt", null ), XISFDataBlockLocation.FromAttribute( "path(/Documents/description(draft).txt)" ) );
    }

    /// <summary>Only the external location forms report as external.</summary>
    [ Fact ]
    public void ReportsExternalLocationsAsExternal()
    {
        Assert.True( XISFDataBlockLocation.FromAttribute( "path(/x.dat)" ).IsExternal );
        Assert.False( XISFDataBlockLocation.FromAttribute( "inline:hex" ).IsExternal );
        Assert.False( XISFDataBlockLocation.FromAttribute( "attachment:0:4" ).IsExternal );
    }

    /// <summary>Malformed external locations are rejected.</summary>
    [ Fact ]
    public void RejectsMalformedExternalLocations()
    {
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "url(no-close" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "path(/x.dat):zz" ) );
        Assert.Throws< XISFException >( () => XISFDataBlockLocation.FromAttribute( "path(/x.dat)trailing" ) );
    }

    /// <summary>The typed accessors expose the payload of the matching kind and null otherwise.</summary>
    [ Fact ]
    public void AccessorsExposePayloads()
    {
        XISFDataBlockLocation attachment = XISFDataBlockLocation.FromAttribute( "attachment:10:20" );

        Assert.Equal( XISFDataBlockLocation.Kind.Attachment, attachment.LocationKind );
        Assert.Equal( ( 10L, 20L ), attachment.AttachmentRange );
        Assert.Null( attachment.ExternalUrl );

        XISFDataBlockLocation path = XISFDataBlockLocation.FromAttribute( "path(@header_dir/f.dat):7" );

        Assert.Equal( "f.dat", path.Path );
        Assert.Equal< ulong? >( 7, path.IndexId );
        Assert.Null( path.InlineEncoding );
    }

    /// <summary>Each location describes as its <c>location</c> attribute form.</summary>
    [ Fact ]
    public void Description()
    {
        Assert.Equal( "inline:base64", XISFDataBlockLocation.FromAttribute( "inline:base64" ).ToString() );
        Assert.Equal( "embedded", XISFDataBlockLocation.FromAttribute( "embedded" ).ToString() );
        Assert.Equal( "attachment:4570:1428362", XISFDataBlockLocation.FromAttribute( "attachment:4570:1428362" ).ToString() );
        Assert.Equal( "path(@header_dir/blocks.xisb):19767", XISFDataBlockLocation.FromAttribute( "path(@header_dir/blocks.xisb):0x4d37" ).ToString() );
        Assert.Equal( "path(/data/x.dat)", XISFDataBlockLocation.FromAttribute( "path(/data/x.dat)" ).ToString() );
    }

    /// <summary>
    /// A default-constructed location (which C# always permits for a struct) is a
    /// safe inline base64 location and none of its members throw.
    /// </summary>
    [ Fact ]
    public void DefaultLocationIsSafe()
    {
        XISFDataBlockLocation location = default;

        Assert.Equal( XISFDataBlockLocation.Kind.Inline, location.LocationKind );
        Assert.Equal< XISFDataBlockLocation.Encoding? >( XISFDataBlockLocation.Encoding.Base64, location.InlineEncoding );
        Assert.False( location.IsExternal );
        Assert.Null( location.AttachmentRange );
        Assert.Equal( "inline:base64", location.ToString() );
        Assert.Equal( location, default( XISFDataBlockLocation ) );
    }

    /// <summary>
    /// Attachment offsets and index ids parse and format identically under a culture
    /// whose number format differs from the invariant culture (a thousands separator
    /// would break the round-trip).
    /// </summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            Assert.Equal( XISFDataBlockLocation.Attachment( 4570, 1428362 ), XISFDataBlockLocation.FromAttribute( "attachment:4570:1428362" ) );
            Assert.Equal( "attachment:4570:1428362", XISFDataBlockLocation.FromAttribute( "attachment:4570:1428362" ).ToString() );

            Assert.Equal( XISFDataBlockLocation.AbsolutePath( "/x.xisb", 1000000 ), XISFDataBlockLocation.FromAttribute( "path(/x.xisb):1000000" ) );
            Assert.Equal( "path(/x.xisb):1000000", XISFDataBlockLocation.FromAttribute( "path(/x.xisb):1000000" ).ToString() );

            Assert.Equal( XISFDataBlockLocation.AbsolutePath( "/y.xisb", 0x4d37 ), XISFDataBlockLocation.FromAttribute( "path(/y.xisb):0x4d37" ) );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
