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
/// Unit tests for <see cref="XISFSampleFormat"/> parsing and its per-format
/// properties.
/// </summary>
public class XISFSampleFormatTests
{
    /// <summary>
    /// Each spec token parses to its sample format; an unknown token does not.
    /// </summary>
    [ Fact ]
    public void SpecTokensMatchSpec()
    {
        Assert.Equal( XISFSampleFormat.UInt8,     XISFSampleFormatExtensions.FromSpecToken( "UInt8" ) );
        Assert.Equal( XISFSampleFormat.UInt16,    XISFSampleFormatExtensions.FromSpecToken( "UInt16" ) );
        Assert.Equal( XISFSampleFormat.UInt32,    XISFSampleFormatExtensions.FromSpecToken( "UInt32" ) );
        Assert.Equal( XISFSampleFormat.UInt64,    XISFSampleFormatExtensions.FromSpecToken( "UInt64" ) );
        Assert.Equal( XISFSampleFormat.Float32,   XISFSampleFormatExtensions.FromSpecToken( "Float32" ) );
        Assert.Equal( XISFSampleFormat.Float64,   XISFSampleFormatExtensions.FromSpecToken( "Float64" ) );
        Assert.Equal( XISFSampleFormat.Complex32, XISFSampleFormatExtensions.FromSpecToken( "Complex32" ) );
        Assert.Equal( XISFSampleFormat.Complex64, XISFSampleFormatExtensions.FromSpecToken( "Complex64" ) );
        Assert.Null( XISFSampleFormatExtensions.FromSpecToken( "Float128" ) );
    }

    /// <summary>
    /// Each format reports its sample size in bytes, counting both floating-point
    /// components of a complex sample.
    /// </summary>
    [ Fact ]
    public void BytesPerSample()
    {
        Assert.Equal( 1,  XISFSampleFormat.UInt8.BytesPerSample() );
        Assert.Equal( 2,  XISFSampleFormat.UInt16.BytesPerSample() );
        Assert.Equal( 4,  XISFSampleFormat.UInt32.BytesPerSample() );
        Assert.Equal( 8,  XISFSampleFormat.UInt64.BytesPerSample() );
        Assert.Equal( 4,  XISFSampleFormat.Float32.BytesPerSample() );
        Assert.Equal( 8,  XISFSampleFormat.Float64.BytesPerSample() );
        Assert.Equal( 8,  XISFSampleFormat.Complex32.BytesPerSample() );
        Assert.Equal( 16, XISFSampleFormat.Complex64.BytesPerSample() );
    }

    /// <summary>
    /// The floating-point and complex classifications identify the real-float and
    /// complex formats.
    /// </summary>
    [ Fact ]
    public void FloatingPointAndComplexClassification()
    {
        Assert.True( XISFSampleFormat.Float32.IsFloatingPoint() );
        Assert.True( XISFSampleFormat.Float64.IsFloatingPoint() );
        Assert.False( XISFSampleFormat.UInt8.IsFloatingPoint() );
        Assert.False( XISFSampleFormat.Complex32.IsFloatingPoint() );

        Assert.True( XISFSampleFormat.Complex32.IsComplex() );
        Assert.True( XISFSampleFormat.Complex64.IsComplex() );
        Assert.False( XISFSampleFormat.Float32.IsComplex() );
    }
}
