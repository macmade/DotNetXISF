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
/// The exception thrown by DotNetXISF when reading or validating XISF data.
/// </summary>
/// <remarks>
/// A single exception type carrying an <see cref="XISFErrorKind"/> discriminator
/// and constructed through one static factory per error case. Every message is
/// prefixed with <c>XISF Error: </c>.
/// </remarks>
public sealed class XISFException : Exception
{
    /// <summary>The prefix applied to every XISF error message.</summary>
    private const string MessagePrefix = "XISF Error: ";

    /// <summary>The kind of error this exception represents.</summary>
    public XISFErrorKind Kind { get; }

    /// <summary>
    /// Initializes a new instance for the given kind and specific description.
    /// </summary>
    /// <param name="kind">The kind of error.</param>
    /// <param name="description">
    /// The specific description, appended to the shared XISF error prefix.
    /// </param>
    private XISFException( XISFErrorKind kind, string description ) : base( MessagePrefix + description )
    {
        this.Kind = kind;
    }

    /// <summary>Creates an <see cref="XISFErrorKind.InvalidFileUrl"/> error.</summary>
    /// <param name="path">The offending file path.</param>
    /// <returns>The created exception.</returns>
    public static XISFException InvalidFileUrl( string path ) => new XISFException( XISFErrorKind.InvalidFileUrl, $"Invalid file URL: { path }" );

    /// <summary>Creates a <see cref="XISFErrorKind.CannotReadFile"/> error.</summary>
    /// <param name="path">The file that could not be read.</param>
    /// <returns>The created exception.</returns>
    public static XISFException CannotReadFile( string path ) => new XISFException( XISFErrorKind.CannotReadFile, $"Cannot read file: { path }" );

    /// <summary>Creates an <see cref="XISFErrorKind.InvalidSignature"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException InvalidSignature( string reason ) => new XISFException( XISFErrorKind.InvalidSignature, $"Invalid signature: { reason }" );

    /// <summary>Creates an <see cref="XISFErrorKind.InvalidHeaderLength"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException InvalidHeaderLength( string reason ) => new XISFException( XISFErrorKind.InvalidHeaderLength, $"Invalid header length: { reason }" );

    /// <summary>Creates a <see cref="XISFErrorKind.MalformedXml"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException MalformedXml( string reason ) => new XISFException( XISFErrorKind.MalformedXml, $"Malformed XML: { reason }" );

    /// <summary>Creates an <see cref="XISFErrorKind.InvalidElement"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException InvalidElement( string reason ) => new XISFException( XISFErrorKind.InvalidElement, $"Invalid element: { reason }" );

    /// <summary>Creates a <see cref="XISFErrorKind.DataBlockError"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException DataBlockError( string reason ) => new XISFException( XISFErrorKind.DataBlockError, $"Data block error: { reason }" );

    /// <summary>Creates a <see cref="XISFErrorKind.DecompressionError"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException DecompressionError( string reason ) => new XISFException( XISFErrorKind.DecompressionError, $"Decompression error: { reason }" );

    /// <summary>Creates a <see cref="XISFErrorKind.ChecksumMismatch"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException ChecksumMismatch( string reason ) => new XISFException( XISFErrorKind.ChecksumMismatch, $"Checksum mismatch: { reason }" );

    /// <summary>Creates a <see cref="XISFErrorKind.DataError"/> error.</summary>
    /// <param name="reason">A description of the specific problem.</param>
    /// <returns>The created exception.</returns>
    public static XISFException DataError( string reason ) => new XISFException( XISFErrorKind.DataError, $"Data error: { reason }" );

    /// <summary>Creates an <see cref="XISFErrorKind.Unsupported"/> error.</summary>
    /// <param name="reason">A description of the limitation.</param>
    /// <returns>The created exception.</returns>
    public static XISFException Unsupported( string reason ) => new XISFException( XISFErrorKind.Unsupported, $"Unsupported: { reason }" );
}
