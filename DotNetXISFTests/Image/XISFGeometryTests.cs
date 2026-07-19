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

using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFGeometry"/> parsing and its derived counts.
/// </summary>
public class XISFGeometryTests
{
    /// <summary>
    /// A 2D geometry parses its width, height and channel count and derives the
    /// pixel and sample counts.
    /// </summary>
    [ Fact ]
    public void Parses2DGeometry()
    {
        XISFGeometry geometry = new XISFGeometry( "100:200:3" );

        Assert.Equal( new[] { 100L, 200L }, geometry.Dimensions );
        Assert.Equal( 3L, geometry.ChannelCount );
        Assert.Equal( 20000L, geometry.PixelCount );
        Assert.Equal( 60000L, geometry.SampleCount );
    }

    /// <summary>
    /// A single-channel geometry derives its sample count from a single channel.
    /// </summary>
    [ Fact ]
    public void ParsesSingleChannel()
    {
        XISFGeometry geometry = new XISFGeometry( "512:512:1" );

        Assert.Equal( new[] { 512L, 512L }, geometry.Dimensions );
        Assert.Equal( 1L, geometry.ChannelCount );
        Assert.Equal( 262144L, geometry.SampleCount );
    }

    /// <summary>
    /// An N-dimensional geometry parses every spatial dimension before the channel
    /// count.
    /// </summary>
    [ Fact ]
    public void ParsesNDimensionalGeometry()
    {
        XISFGeometry geometry = new XISFGeometry( "10:20:30:2" );

        Assert.Equal( new[] { 10L, 20L, 30L }, geometry.Dimensions );
        Assert.Equal( 2L, geometry.ChannelCount );
        Assert.Equal( 6000L, geometry.PixelCount );
        Assert.Equal( 12000L, geometry.SampleCount );
    }

    /// <summary>
    /// A minimal geometry has a single spatial dimension and a channel count.
    /// </summary>
    [ Fact ]
    public void ParsesMinimalGeometry()
    {
        XISFGeometry geometry = new XISFGeometry( "5:3" );

        Assert.Equal( new[] { 5L }, geometry.Dimensions );
        Assert.Equal( 3L, geometry.ChannelCount );
        Assert.Equal( 15L, geometry.SampleCount );
    }

    /// <summary>
    /// A default-constructed geometry (which C# always permits for a struct)
    /// exposes an empty dimension list and does not throw from any of its derived
    /// members.
    /// </summary>
    [ Fact ]
    public void DefaultGeometryIsSafe()
    {
        XISFGeometry geometry = default;

        Assert.Empty( geometry.Dimensions );
        Assert.Equal( 0L, geometry.ChannelCount );
        Assert.Equal( 0L, geometry.SampleCount );
        Assert.Equal( "0", geometry.ToString() );
        Assert.Equal( geometry, default( XISFGeometry ) );
        Assert.Equal( geometry.GetHashCode(), default( XISFGeometry ).GetHashCode() );
    }

    /// <summary>
    /// A geometry with too few components, or any non-positive or non-integer
    /// component, is rejected.
    /// </summary>
    [ Fact ]
    public void RejectsMalformedGeometry()
    {
        Assert.Throws< XISFException >( () => new XISFGeometry( "100" ) );
        Assert.Throws< XISFException >( () => new XISFGeometry( "" ) );
        Assert.Throws< XISFException >( () => new XISFGeometry( "100:0:3" ) );
        Assert.Throws< XISFException >( () => new XISFGeometry( "100:200:0" ) );
        Assert.Throws< XISFException >( () => new XISFGeometry( "100:-5:3" ) );
        Assert.Throws< XISFException >( () => new XISFGeometry( "a:b:c" ) );
    }
}
