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
/// The color space of an XISF image, from its <c>colorSpace</c> attribute.
/// </summary>
/// <remarks>
/// The default (<see cref="Gray"/>) is the enum's zero value, so an image with no
/// <c>colorSpace</c> attribute defaults correctly.
/// </remarks>
public enum XISFColorSpace
{
    /// <summary>A single-channel grayscale image (the default).</summary>
    Gray,

    /// <summary>A three-channel RGB image.</summary>
    Rgb,

    /// <summary>A three-channel CIE L*a*b* image.</summary>
    CieLab,
}

/// <summary>
/// Spec-token parsing/formatting and the default for <see cref="XISFColorSpace"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with the enum as a single logical unit.
/// </remarks>
public static class XISFColorSpaceExtensions
{
    /// <summary>The default color space when the attribute is absent (<c>Gray</c>).</summary>
    public const XISFColorSpace Default = XISFColorSpace.Gray;

    /// <summary>
    /// Parses a color-space spec token to its <see cref="XISFColorSpace"/>.
    /// </summary>
    /// <param name="token">The spec string (for example <c>RGB</c>).</param>
    /// <returns>
    /// The matching color space, or <c>null</c> when the token is not a known XISF
    /// 1.0 color space.
    /// </returns>
    public static XISFColorSpace? FromSpecToken( string token )
    {
        foreach( XISFColorSpace value in Enum.GetValues< XISFColorSpace >() )
        {
            if( string.Equals( value.SpecToken(), token, StringComparison.Ordinal ) )
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// The spec token for a color space (the string carried in the
    /// <c>colorSpace</c> attribute).
    /// </summary>
    /// <param name="value">The color space.</param>
    /// <returns>The spec token.</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined color space
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static string SpecToken( this XISFColorSpace value )
    {
        return value switch
        {
            XISFColorSpace.Gray   => "Gray",
            XISFColorSpace.Rgb    => "RGB",
            XISFColorSpace.CieLab => "CIELab",
            _                     => throw XISFException.InvalidElement( "Unknown color space" ),
        };
    }
}
