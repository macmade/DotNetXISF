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
/// Confirms that every public type provides a readable string form: the
/// spec-valued enums describe as their spec token, and the structured types
/// describe as a readable, non-empty summary.
/// </summary>
/// <remarks>
/// This suite grows as later milestones add types; here it covers the format
/// enums (whose spec token is their <c>SpecToken()</c>) and the geometry
/// summary.
/// </remarks>
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
}
