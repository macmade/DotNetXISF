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
/// The byte order of an XISF image's multi-byte samples, from its
/// <c>byteOrder</c> attribute.
/// </summary>
/// <remarks>
/// Pixel bytes are exposed opaquely, so this is metadata the consumer uses to
/// interpret multi-byte samples. The default (<see cref="Little"/>) is the enum's
/// zero value, matching the XISF 1.0 default and the vast majority of systems on
/// which XISF is deployed.
/// </remarks>
public enum XISFByteOrder
{
    /// <summary>Little-endian, least significant byte first (the default).</summary>
    Little,

    /// <summary>Big-endian, most significant byte first.</summary>
    Big,
}

/// <summary>
/// Spec-token parsing/formatting and the default for <see cref="XISFByteOrder"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with the enum as a single logical unit.
/// </remarks>
public static class XISFByteOrderExtensions
{
    /// <summary>The default byte order when the attribute is absent (<c>little</c>).</summary>
    public const XISFByteOrder Default = XISFByteOrder.Little;

    /// <summary>
    /// Parses a byte-order spec token to its <see cref="XISFByteOrder"/>.
    /// </summary>
    /// <param name="token">The spec string (<c>big</c> or <c>little</c>).</param>
    /// <returns>
    /// The matching byte order, or <c>null</c> when the token is not a known XISF
    /// 1.0 byte order.
    /// </returns>
    public static XISFByteOrder? FromSpecToken( string token )
    {
        foreach( XISFByteOrder value in Enum.GetValues< XISFByteOrder >() )
        {
            if( string.Equals( value.SpecToken(), token, StringComparison.Ordinal ) )
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// The spec token for a byte order (the string carried in the <c>byteOrder</c>
    /// attribute).
    /// </summary>
    /// <param name="value">The byte order.</param>
    /// <returns>The spec token (the lowercase spec string).</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined byte order
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static string SpecToken( this XISFByteOrder value )
    {
        return value switch
        {
            XISFByteOrder.Little => "little",
            XISFByteOrder.Big    => "big",
            _                    => throw XISFException.InvalidElement( "Unknown byte order" ),
        };
    }
}
