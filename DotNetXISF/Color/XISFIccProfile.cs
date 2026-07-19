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
/// A parsed XISF <c>&lt;ICCProfile&gt;</c> element: an embedded ICC color profile.
/// </summary>
/// <remarks>
/// <para>
/// An ICC profile is serialized as an XISF data block that stores the profile structure
/// unaltered, so its bytes are exposed opaquely via <see cref="Data"/>. Per the ICC
/// specification the profile data is always big-endian; <c>ICCProfile</c> elements
/// therefore never carry a <c>byteOrder</c> attribute, and interpretation of the bytes is
/// left to the consumer.
/// </para>
/// <para>
/// The profile bytes are decoded lazily on first access to <see cref="Data"/> (via the
/// backing data block), so this is a reference type and, like the data block, not
/// thread-safe.
/// </para>
/// </remarks>
public sealed class XISFIccProfile
{
    /// <summary>The backing data block holding the profile bytes.</summary>
    private XISFDataBlock DataBlock { get; }

    /// <summary>
    /// The raw ICC profile bytes: fully decoded (decompressed if the block was compressed),
    /// exposed opaquely. Computed lazily on first access.
    /// </summary>
    /// <exception cref="XISFException">
    /// Any error raised while resolving or decoding the data block (decompression failure,
    /// checksum mismatch).
    /// </exception>
    public ReadOnlyMemory< byte > Data => this.DataBlock.Data;

    /// <summary>Parses an <c>&lt;ICCProfile&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;ICCProfile&gt;</c> element, which must declare a data-block <c>location</c>.</param>
    /// <param name="fileData">The complete file bytes, used to resolve an <c>attachment</c> data block by its absolute offset.</param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external data blocks; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">The parsing options to apply.</param>
    /// <exception cref="XISFException">
    /// The <c>location</c> is missing or malformed, or any error raised while resolving the
    /// block (<see cref="XISFErrorKind.DataBlockError"/>).
    /// </exception>
    internal XISFIccProfile( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        this.DataBlock = new XISFDataBlock( element, fileData, baseDirectory, options );
    }

    /// <summary>A single-line, human-readable summary of the ICC profile.</summary>
    /// <remarks>Reports the location without reading the profile bytes.</remarks>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        return $"XISFIccProfile {{ location: { this.DataBlock.Location } }}";
    }
}
