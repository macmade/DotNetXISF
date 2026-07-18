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
/// Options controlling how strictly XISF data is parsed and validated.
/// </summary>
/// <remarks>
/// The individual flags toggle independent behaviors; the <see cref="Strict"/>
/// and <see cref="Lenient"/> presets bundle sensible defaults.
/// <see cref="Strict"/> validates as much as possible (including data-block
/// checksums) and rejects any spec deviation, while <see cref="Lenient"/>
/// tolerates the technically-noncompliant constructs found in real-world files.
/// Both presets leave external/distributed data-block resolution disabled; it
/// must be opted into explicitly for security. The raw bitmask is the enum's
/// underlying value.
/// </remarks>
[ Flags ]
public enum XISFParsingOptions
{
    /// <summary>No options; the empty set.</summary>
    None = 0,

    /// <summary>
    /// Verify a data block's declared checksum against its computed digest,
    /// throwing an <see cref="XISFErrorKind.ChecksumMismatch"/> error on a
    /// mismatch. When unset, declared checksums are ignored.
    /// </summary>
    VerifyChecksums = 1 << 0,

    /// <summary>
    /// Allow resolving data blocks whose location refers to an external or
    /// distributed file (<c>url(...)</c> / <c>path(...)</c>). Disabled by default
    /// because resolving such locations reads files outside the parsed document.
    /// </summary>
    AllowExternalLocations = 1 << 1,

    /// <summary>
    /// Tolerate technically-noncompliant input that deviates from the XISF
    /// specification instead of rejecting it. When unset, such deviations are
    /// treated as errors.
    /// </summary>
    AllowSpecDeviations = 1 << 2,

    /// <summary>
    /// Spec-faithful parsing: verifies data-block checksums and rejects any input
    /// the XISF specification forbids.
    /// </summary>
    Strict = VerifyChecksums,

    /// <summary>
    /// Real-world-friendly parsing: tolerates the noncompliant constructs found in
    /// many existing XISF files and does not require checksum verification.
    /// </summary>
    Lenient = AllowSpecDeviations,
}
