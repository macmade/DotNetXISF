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
/// A parsed XISF (Extensible Image Serialization Format) monolithic file.
/// </summary>
/// <remarks>
/// A monolithic XISF file begins with a 16-byte binary preamble - the 8-byte
/// <c>XISF0100</c> signature, a little-endian <c>UInt32</c> giving the length of
/// the XML header, and a 4-byte reserved field - followed by the UTF-8 XML header
/// and, after it, the attached binary data blocks. This type reads and validates
/// the preamble and the XML header, and exposes the parsed images, properties,
/// embedded FITS keywords and metadata.
/// <para>
/// This is a reference type holding parsed file state. It composes lazily-decoding
/// blocks whose results cache on first read, so even concurrent reads of a
/// fully-parsed file race: it is not thread-safe.
/// </para>
/// <para>
/// At this stage the type holds only the preamble constants fixed by the XISF
/// standard; its parsing, validation and accessors are added by the file layer.
/// </para>
/// </remarks>
public class XISFFile
{
    /// <summary>
    /// The 8-byte ASCII signature that opens every monolithic XISF file.
    /// </summary>
    public const string Signature = "XISF0100";

    /// <summary>
    /// The size, in bytes, of the binary preamble: the 8-byte signature, the
    /// 4-byte little-endian header-length field, and the 4-byte reserved field.
    /// The XML header begins immediately after, at this offset.
    /// </summary>
    public const int PreambleSize = 16;

    /// <summary>
    /// The XML namespace declared by the root <c>xisf</c> element.
    /// </summary>
    public const string Namespace = "http://www.pixinsight.com/xisf";
}
