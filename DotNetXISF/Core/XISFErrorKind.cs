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

namespace DotNetXISF;

/// <summary>
/// Identifies which kind of error an <see cref="XISFException"/> represents.
/// </summary>
/// <remarks>
/// A discriminator a consumer can branch on to identify which error a caught
/// <see cref="XISFException"/> represents, without matching on the message text.
/// </remarks>
public enum XISFErrorKind
{
    /// <summary>
    /// The provided path does not point to a readable file (for example it is
    /// missing or refers to a directory).
    /// </summary>
    InvalidFileUrl,

    /// <summary>The file at the given path exists but its contents could not be read.</summary>
    CannotReadFile,

    /// <summary>
    /// The monolithic-file binary preamble is invalid - either the signature is
    /// not the expected <c>XISF0100</c> marker, or the reserved field is non-zero.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// The XML-header length field is invalid (for example zero, or extending past
    /// the end of the file).
    /// </summary>
    InvalidHeaderLength,

    /// <summary>The XML header could not be parsed as well-formed XML.</summary>
    MalformedXml,

    /// <summary>
    /// An XML element or attribute is missing, malformed, or carries an invalid
    /// value.
    /// </summary>
    InvalidElement,

    /// <summary>A data block's bytes could not be resolved from its declared location.</summary>
    DataBlockError,

    /// <summary>A compressed data block could not be decompressed.</summary>
    DecompressionError,

    /// <summary>A data block's computed digest does not match its declared checksum.</summary>
    ChecksumMismatch,

    /// <summary>A low-level data operation failed.</summary>
    DataError,

    /// <summary>
    /// A requested feature or algorithm is not supported by this build or on this
    /// operating-system version (for example a checksum algorithm that has no
    /// available implementation).
    /// </summary>
    Unsupported,
}
