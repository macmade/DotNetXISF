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
/// The pixel storage model of an XISF image, from its <c>pixelStorage</c>
/// attribute.
/// </summary>
/// <remarks>
/// <c>Planar</c> stores each channel contiguously (channel-major); <c>Normal</c>
/// stores samples interleaved per pixel (pixel-major). Pixel bytes are exposed
/// opaquely, so this is metadata the consumer uses to interpret them. The default
/// (<see cref="Planar"/>) is the enum's zero value.
/// </remarks>
public enum XISFPixelStorage
{
    /// <summary>Channel-contiguous (planar) storage (the default).</summary>
    Planar,

    /// <summary>Pixel-interleaved (normal) storage.</summary>
    Normal,
}

/// <summary>
/// Spec-token parsing/formatting and the default for
/// <see cref="XISFPixelStorage"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with the enum as a single logical unit.
/// </remarks>
public static class XISFPixelStorageExtensions
{
    /// <summary>The default pixel storage when the attribute is absent (<c>Planar</c>).</summary>
    public const XISFPixelStorage Default = XISFPixelStorage.Planar;

    /// <summary>
    /// Parses a pixel-storage spec token to its <see cref="XISFPixelStorage"/>.
    /// </summary>
    /// <param name="token">The spec string (<c>Planar</c> or <c>Normal</c>).</param>
    /// <returns>
    /// The matching pixel-storage model, or <c>null</c> when the token is not a
    /// known XISF 1.0 pixel-storage model.
    /// </returns>
    public static XISFPixelStorage? FromSpecToken( string token )
    {
        foreach( XISFPixelStorage value in Enum.GetValues< XISFPixelStorage >() )
        {
            if( string.Equals( value.SpecToken(), token, StringComparison.Ordinal ) )
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// The spec token for a pixel-storage model (the string carried in the
    /// <c>pixelStorage</c> attribute).
    /// </summary>
    /// <param name="value">The pixel-storage model.</param>
    /// <returns>The spec token, which is the member's name.</returns>
    public static string SpecToken( this XISFPixelStorage value ) => value.ToString();
}
