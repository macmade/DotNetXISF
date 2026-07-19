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
using System.Globalization;
using System.Security.Cryptography;

namespace DotNetXISF;

/// <summary>
/// A data block's checksum, parsed from its <c>checksum</c> attribute.
/// </summary>
/// <remarks>
/// An XISF <c>checksum</c> attribute has the form <c>algorithm:digest</c>, where
/// the digest is base16 (hexadecimal). The digest is computed over the block's
/// <em>stored</em> (as-on-disk, still-compressed) bytes, so verification happens
/// before decompression.
/// <para>
/// The SHA-3 algorithms rely on platform support that is only present on recent
/// systems; where it is absent, <see cref="Matches"/> / <see cref="Verify"/> throw
/// an <see cref="XISFErrorKind.Unsupported"/> error rather than silently passing.
/// </para>
/// </remarks>
public readonly struct XISFChecksum : IEquatable< XISFChecksum >
{
    /// <summary>A supported checksum algorithm.</summary>
    public enum Algorithm
    {
        /// <summary>SHA-1 (accepts the <c>sha-1</c> and <c>sha1</c> spellings).</summary>
        Sha1,

        /// <summary>SHA-256 (accepts the <c>sha-256</c> and <c>sha256</c> spellings).</summary>
        Sha256,

        /// <summary>SHA-512 (accepts the <c>sha-512</c> and <c>sha512</c> spellings).</summary>
        Sha512,

        /// <summary>SHA3-256.</summary>
        Sha3_256,

        /// <summary>SHA3-512.</summary>
        Sha3_512,
    }

    /// <summary>
    /// The declared digest backing store, or <c>null</c> for a default-constructed
    /// value; <see cref="Digest"/> coalesces that to the empty string.
    /// </summary>
    private readonly string? digest;

    /// <summary>The checksum algorithm.</summary>
    public Algorithm ChecksumAlgorithm { get; }

    /// <summary>The declared digest, as a base16 (hexadecimal) string.</summary>
    public string Digest => this.digest ?? "";

    /// <summary>Parses a <c>checksum</c> attribute of the form <c>algorithm:digest</c>.</summary>
    /// <param name="attribute">The raw <c>checksum</c> attribute value.</param>
    /// <exception cref="XISFException">
    /// The attribute is malformed or has an empty digest
    /// (<see cref="XISFErrorKind.InvalidElement"/>), or the algorithm is not
    /// recognized (<see cref="XISFErrorKind.Unsupported"/>).
    /// </exception>
    public XISFChecksum( string attribute )
    {
        int separator = attribute.IndexOf( ':' );

        if( separator < 0 )
        {
            throw XISFException.InvalidElement( $"Malformed checksum attribute (expected 'algorithm:digest'): '{ attribute }'" );
        }

        string name         = attribute[ ..separator ];
        string parsedDigest = attribute[ ( separator + 1 ).. ];

        Algorithm? algorithm = XISFChecksumAlgorithmExtensions.FromName( name );

        if( algorithm.HasValue == false )
        {
            throw XISFException.Unsupported( $"Unsupported checksum algorithm: '{ name }'" );
        }

        if( parsedDigest.Length == 0 )
        {
            throw XISFException.InvalidElement( $"Checksum attribute has an empty digest: '{ attribute }'" );
        }

        this.ChecksumAlgorithm = algorithm.Value;
        this.digest            = parsedDigest;
    }

    /// <summary>Returns whether the checksum matches the digest of the given bytes.</summary>
    /// <param name="data">The bytes to hash (a data block's stored bytes).</param>
    /// <returns><c>true</c> if the computed digest equals the declared digest, compared case-insensitively.</returns>
    /// <exception cref="XISFException">
    /// The algorithm has no implementation on the current platform
    /// (<see cref="XISFErrorKind.Unsupported"/>).
    /// </exception>
    public bool Matches( ReadOnlyMemory< byte > data )
    {
        return string.Equals( this.ComputedDigest( data ), this.Digest, StringComparison.OrdinalIgnoreCase );
    }

    /// <summary>Verifies that the checksum matches the digest of the given bytes.</summary>
    /// <param name="data">The bytes to hash (a data block's stored bytes).</param>
    /// <exception cref="XISFException">
    /// The digests differ (<see cref="XISFErrorKind.ChecksumMismatch"/>), or the
    /// algorithm has no implementation on the current platform
    /// (<see cref="XISFErrorKind.Unsupported"/>).
    /// </exception>
    public void Verify( ReadOnlyMemory< byte > data )
    {
        if( this.Matches( data ) == false )
        {
            throw XISFException.ChecksumMismatch( $"{ this.ChecksumAlgorithm.Name() } digest of the { data.Length.ToString( CultureInfo.InvariantCulture ) }-byte data block does not match the declared checksum" );
        }
    }

    /// <summary>Computes the lowercase base16 digest of the given bytes.</summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The digest as a lowercase hexadecimal string.</returns>
    /// <exception cref="XISFException">
    /// A SHA-3 algorithm is requested on a platform that does not provide it
    /// (<see cref="XISFErrorKind.Unsupported"/>).
    /// </exception>
    private string ComputedDigest( ReadOnlyMemory< byte > data )
    {
        ReadOnlySpan< byte > bytes = data.Span;

        byte[] hash = this.ChecksumAlgorithm switch
        {
            Algorithm.Sha1     => SHA1.HashData( bytes ),
            Algorithm.Sha256   => SHA256.HashData( bytes ),
            Algorithm.Sha512   => SHA512.HashData( bytes ),
            Algorithm.Sha3_256 => SHA3_256.IsSupported ? SHA3_256.HashData( bytes ) : throw XISFException.Unsupported( "SHA3-256 checksum verification is not supported on this platform" ),
            Algorithm.Sha3_512 => SHA3_512.IsSupported ? SHA3_512.HashData( bytes ) : throw XISFException.Unsupported( "SHA3-512 checksum verification is not supported on this platform" ),
            _                  => throw XISFException.InvalidElement( "Unknown checksum algorithm" ),
        };

        return Convert.ToHexStringLower( hash );
    }

    /// <summary>A single-line, human-readable summary of the checksum.</summary>
    /// <returns>The summary string.</returns>
    public override string ToString()
    {
        return $"XISFChecksum {{ { this.ChecksumAlgorithm.Name() }: { this.Digest } }}";
    }

    /// <summary>Returns whether this checksum equals another: the same algorithm and digest.</summary>
    /// <param name="other">The checksum to compare against.</param>
    /// <returns><c>true</c> if the checksums are equal.</returns>
    public bool Equals( XISFChecksum other )
    {
        return this.ChecksumAlgorithm == other.ChecksumAlgorithm && string.Equals( this.Digest, other.Digest, StringComparison.Ordinal );
    }

    /// <summary>Returns whether this checksum equals another object.</summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal checksum.</returns>
    public override bool Equals( object? obj ) => obj is XISFChecksum other && this.Equals( other );

    /// <summary>A hash code combining the algorithm and digest.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode() => HashCode.Combine( this.ChecksumAlgorithm, this.Digest );

    /// <summary>Returns whether two checksums are equal.</summary>
    /// <param name="left">The first checksum.</param>
    /// <param name="right">The second checksum.</param>
    /// <returns><c>true</c> if the checksums are equal.</returns>
    public static bool operator ==( XISFChecksum left, XISFChecksum right ) => left.Equals( right );

    /// <summary>Returns whether two checksums are unequal.</summary>
    /// <param name="left">The first checksum.</param>
    /// <param name="right">The second checksum.</param>
    /// <returns><c>true</c> if the checksums are unequal.</returns>
    public static bool operator !=( XISFChecksum left, XISFChecksum right ) => left.Equals( right ) == false;
}

/// <summary>
/// Spec-spelling parsing and formatting for <see cref="XISFChecksum.Algorithm"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with <see cref="XISFChecksum"/> as a single logical unit.
/// </remarks>
public static class XISFChecksumAlgorithmExtensions
{
    /// <summary>Parses an algorithm from its XISF attribute spelling.</summary>
    /// <remarks>The name is matched case-insensitively, accepting the hyphenated and unhyphenated spellings.</remarks>
    /// <param name="name">The algorithm name.</param>
    /// <returns>The matching algorithm, or <c>null</c> when the name is not a recognized algorithm.</returns>
    public static XISFChecksum.Algorithm? FromName( string name )
    {
        return name.ToUpperInvariant() switch
        {
            "SHA-1" or "SHA1"     => XISFChecksum.Algorithm.Sha1,
            "SHA-256" or "SHA256" => XISFChecksum.Algorithm.Sha256,
            "SHA-512" or "SHA512" => XISFChecksum.Algorithm.Sha512,
            "SHA3-256"            => XISFChecksum.Algorithm.Sha3_256,
            "SHA3-512"            => XISFChecksum.Algorithm.Sha3_512,
            _                     => null,
        };
    }

    /// <summary>The canonical XISF spelling of an algorithm.</summary>
    /// <param name="algorithm">The algorithm.</param>
    /// <returns>The canonical spelling (for example <c>sha-256</c>).</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined algorithm (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static string Name( this XISFChecksum.Algorithm algorithm )
    {
        return algorithm switch
        {
            XISFChecksum.Algorithm.Sha1     => "sha-1",
            XISFChecksum.Algorithm.Sha256   => "sha-256",
            XISFChecksum.Algorithm.Sha512   => "sha-512",
            XISFChecksum.Algorithm.Sha3_256 => "sha3-256",
            XISFChecksum.Algorithm.Sha3_512 => "sha3-512",
            _                               => throw XISFException.InvalidElement( "Unknown checksum algorithm" ),
        };
    }
}
