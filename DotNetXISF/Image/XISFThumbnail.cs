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
/// A parsed XISF <c>&lt;Thumbnail&gt;</c> element: a small, representative version of an image.
/// </summary>
/// <remarks>
/// <para>
/// Other than its tag name, a thumbnail is an ordinary XISF image, so it is parsed through and
/// exposed as an <see cref="XISFImage"/> (available as <see cref="Image"/>). The XISF
/// specification additionally restricts a thumbnail to a two-dimensional, <c>UInt8</c> or
/// <c>UInt16</c>, grayscale or RGB image with no <c>bounds</c> attribute and no child
/// <c>ColorFilterArray</c> or nested <c>Thumbnail</c>; these restrictions are enforced under
/// strict parsing and relaxed under <see cref="XISFParsingOptions.AllowSpecDeviations"/>.
/// </para>
/// <para>
/// Because the backing image decodes its pixel bytes lazily, this is a reference type and not
/// thread-safe.
/// </para>
/// </remarks>
public sealed class XISFThumbnail
{
    /// <summary>The thumbnail as an ordinary parsed image.</summary>
    public XISFImage Image { get; }

    /// <summary>
    /// The thumbnail's pixel bytes: fully decoded (decompressed and un-shuffled), exposed
    /// opaquely. Computed lazily on first access.
    /// </summary>
    /// <exception cref="XISFException">
    /// Any error raised while resolving or decoding the pixel data block.
    /// </exception>
    public ReadOnlyMemory< byte > Data => this.Image.Data;

    /// <summary>Parses a <c>&lt;Thumbnail&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;Thumbnail&gt;</c> element.</param>
    /// <param name="fileData">The complete file bytes, used to resolve an <c>attachment</c> pixel data block by its absolute offset.</param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external data blocks; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing the thumbnail restrictions are
    /// enforced; <see cref="XISFParsingOptions.AllowSpecDeviations"/> relaxes them.
    /// </param>
    /// <exception cref="XISFException">
    /// A thumbnail restriction is violated under strict parsing, or any error raised while
    /// parsing the image (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    internal XISFThumbnail( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        XISFImage image = new XISFImage( element, fileData, baseDirectory, options );

        if( options.HasFlag( XISFParsingOptions.AllowSpecDeviations ) == false )
        {
            Validate( image, element );
        }

        this.Image = image;
    }

    /// <summary>A single-line, human-readable summary of the thumbnail.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFThumbnail {{ { this.Image } }}";
    }

    /// <summary>Enforces the XISF thumbnail restrictions on a parsed image.</summary>
    /// <param name="image">The parsed thumbnail image.</param>
    /// <param name="element">The originating <c>&lt;Thumbnail&gt;</c> element (used to detect forbidden child elements).</param>
    /// <exception cref="XISFException">Any restriction is violated (<see cref="XISFErrorKind.InvalidElement"/>).</exception>
    private static void Validate( XISFImage image, XISFElement element )
    {
        if( image.SampleFormat is not ( XISFSampleFormat.UInt8 or XISFSampleFormat.UInt16 ) )
        {
            throw XISFException.InvalidElement( $"Thumbnail sample format must be UInt8 or UInt16, found { image.SampleFormat.SpecToken() }" );
        }

        if( image.ColorSpace is not ( XISFColorSpace.Gray or XISFColorSpace.Rgb ) )
        {
            throw XISFException.InvalidElement( $"Thumbnail color space must be Gray or RGB, found { image.ColorSpace.SpecToken() }" );
        }

        if( image.Geometry.Dimensions.Count != 2 )
        {
            throw XISFException.InvalidElement( "Thumbnail must be a two-dimensional image" );
        }

        if( image.Bounds is not null )
        {
            throw XISFException.InvalidElement( "Thumbnail must not define a 'bounds' attribute" );
        }

        if( element.ChildrenNamed( "ColorFilterArray" ).Count != 0 )
        {
            throw XISFException.InvalidElement( "Thumbnail must not contain a ColorFilterArray element" );
        }

        if( element.ChildrenNamed( "Thumbnail" ).Count != 0 )
        {
            throw XISFException.InvalidElement( "Thumbnail must not contain a nested Thumbnail element" );
        }
    }
}
