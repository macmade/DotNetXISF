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
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFException"/> and <see cref="XISFErrorKind"/>.
/// </summary>
/// <remarks>
/// Covers the one static factory per error case: each carries the expected
/// <see cref="XISFErrorKind"/> discriminator and produces a non-empty,
/// prefixed message embedding its payload, which the compiler cannot check.
/// </remarks>
public class XISFExceptionTests
{
    /// <summary>The shared prefix every XISF error message carries.</summary>
    private const string Prefix = "XISF Error: ";

    /// <summary>
    /// One representative payload reused across the reason-carrying factories.
    /// </summary>
    private const string Reason = "This is a test";

    /// <summary>
    /// One representative file path reused across the path-carrying factories.
    /// </summary>
    private const string Path = "/foo/bar.xisf";

    /// <summary>
    /// Every factory produces an exception whose <see cref="XISFException.Kind"/>
    /// matches the case and whose message is exactly the shared prefix followed by
    /// the case's label and its payload.
    /// </summary>
    [ Fact ]
    public void EachFactoryProducesItsKindAndMessage()
    {
        ( XISFException Exception, XISFErrorKind Kind, string Message )[] cases =
        [
            ( XISFException.InvalidFileUrl( Path ),         XISFErrorKind.InvalidFileUrl,      $"Invalid file URL: { Path }" ),
            ( XISFException.CannotReadFile( Path ),         XISFErrorKind.CannotReadFile,      $"Cannot read file: { Path }" ),
            ( XISFException.InvalidSignature( Reason ),     XISFErrorKind.InvalidSignature,    $"Invalid signature: { Reason }" ),
            ( XISFException.InvalidHeaderLength( Reason ),  XISFErrorKind.InvalidHeaderLength, $"Invalid header length: { Reason }" ),
            ( XISFException.MalformedXml( Reason ),         XISFErrorKind.MalformedXml,        $"Malformed XML: { Reason }" ),
            ( XISFException.InvalidElement( Reason ),       XISFErrorKind.InvalidElement,      $"Invalid element: { Reason }" ),
            ( XISFException.DataBlockError( Reason ),       XISFErrorKind.DataBlockError,      $"Data block error: { Reason }" ),
            ( XISFException.DecompressionError( Reason ),   XISFErrorKind.DecompressionError,  $"Decompression error: { Reason }" ),
            ( XISFException.ChecksumMismatch( Reason ),     XISFErrorKind.ChecksumMismatch,    $"Checksum mismatch: { Reason }" ),
            ( XISFException.DataError( Reason ),            XISFErrorKind.DataError,           $"Data error: { Reason }" ),
            ( XISFException.Unsupported( Reason ),          XISFErrorKind.Unsupported,         $"Unsupported: { Reason }" ),
        ];

        foreach( ( XISFException exception, XISFErrorKind kind, string message ) in cases )
        {
            Assert.Equal( kind, exception.Kind );
            Assert.False( string.IsNullOrEmpty( exception.Message ) );
            Assert.StartsWith( Prefix, exception.Message, StringComparison.Ordinal );
            Assert.Equal( Prefix + message, exception.Message );
        }
    }

    /// <summary>
    /// An <see cref="XISFException"/> is an <see cref="Exception"/>, so it can be
    /// thrown and caught as one.
    /// </summary>
    [ Fact ]
    public void IsAnException()
    {
        Assert.IsAssignableFrom< Exception >( XISFException.DataError( Reason ) );
    }
}
