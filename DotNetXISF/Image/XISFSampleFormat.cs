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

namespace DotNetXISF;

/// <summary>
/// The pixel sample format of an XISF image, from its <c>sampleFormat</c>
/// attribute.
/// </summary>
/// <remarks>
/// XISF 1.0 defines eight formats: the unsigned integers, the two floating-point
/// formats, and the two complex formats. Each member's name is its exact spec
/// token.
/// </remarks>
public enum XISFSampleFormat
{
    /// <summary>8-bit unsigned integer samples.</summary>
    UInt8,

    /// <summary>16-bit unsigned integer samples.</summary>
    UInt16,

    /// <summary>32-bit unsigned integer samples.</summary>
    UInt32,

    /// <summary>64-bit unsigned integer samples.</summary>
    UInt64,

    /// <summary>32-bit (single-precision) floating-point samples.</summary>
    Float32,

    /// <summary>64-bit (double-precision) floating-point samples.</summary>
    Float64,

    /// <summary>Complex samples with two 32-bit floating-point components.</summary>
    Complex32,

    /// <summary>Complex samples with two 64-bit floating-point components.</summary>
    Complex64,
}

/// <summary>
/// Spec-token parsing/formatting and per-format properties for
/// <see cref="XISFSampleFormat"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with the enum as a single logical unit.
/// </remarks>
public static class XISFSampleFormatExtensions
{
    /// <summary>
    /// Parses a sample-format spec token to its <see cref="XISFSampleFormat"/>.
    /// </summary>
    /// <param name="token">The spec string (for example <c>Float32</c>).</param>
    /// <returns>
    /// The matching sample format, or <c>null</c> when the token is not a known
    /// XISF 1.0 format.
    /// </returns>
    public static XISFSampleFormat? FromSpecToken( string token )
    {
        foreach( XISFSampleFormat value in Enum.GetValues< XISFSampleFormat >() )
        {
            if( string.Equals( value.SpecToken(), token, StringComparison.Ordinal ) )
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// The spec token for a sample format (the string carried in the
    /// <c>sampleFormat</c> attribute).
    /// </summary>
    /// <param name="value">The sample format.</param>
    /// <returns>The spec token, which is the member's name.</returns>
    public static string SpecToken( this XISFSampleFormat value ) => value.ToString();

    /// <summary>
    /// The size, in bytes, of a single sample in the format.
    /// </summary>
    /// <remarks>
    /// A complex sample is two floating-point components, so <c>Complex32</c> is
    /// 8 bytes and <c>Complex64</c> is 16 bytes.
    /// </remarks>
    /// <param name="value">The sample format.</param>
    /// <returns>The sample size in bytes.</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined sample format
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static int BytesPerSample( this XISFSampleFormat value )
    {
        return value switch
        {
            XISFSampleFormat.UInt8     => 1,
            XISFSampleFormat.UInt16    => 2,
            XISFSampleFormat.UInt32    => 4,
            XISFSampleFormat.UInt64    => 8,
            XISFSampleFormat.Float32   => 4,
            XISFSampleFormat.Float64   => 8,
            XISFSampleFormat.Complex32 => 8,
            XISFSampleFormat.Complex64 => 16,
            _                          => throw XISFException.InvalidElement( "Unknown sample format" ),
        };
    }

    /// <summary>
    /// Returns whether the format is real floating-point (<c>Float32</c> or
    /// <c>Float64</c>).
    /// </summary>
    /// <param name="value">The sample format.</param>
    /// <returns><c>true</c> for a real floating-point format.</returns>
    public static bool IsFloatingPoint( this XISFSampleFormat value )
    {
        return value is XISFSampleFormat.Float32 or XISFSampleFormat.Float64;
    }

    /// <summary>
    /// Returns whether the format is complex (<c>Complex32</c> or
    /// <c>Complex64</c>).
    /// </summary>
    /// <param name="value">The sample format.</param>
    /// <returns><c>true</c> for a complex format.</returns>
    public static bool IsComplex( this XISFSampleFormat value )
    {
        return value is XISFSampleFormat.Complex32 or XISFSampleFormat.Complex64;
    }
}
