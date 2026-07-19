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

using System.Security.Cryptography;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFChecksum"/>: attribute parsing and aliases,
/// digest computation and matching/verification, and the SHA-3 availability gate.
/// </summary>
public class XISFChecksumTests
{
    /// <summary>The reference payload (<c>DE AD BE EF</c>).</summary>
    private static readonly byte[] Payload = { 0xDE, 0xAD, 0xBE, 0xEF };

    /// <summary>The reference payload's SHA-1 digest (computed by python <c>hashlib</c>).</summary>
    private const string Sha1Hex = "d78f8bb992a56a597f6c7a1fb918bb78271367eb";

    /// <summary>The reference payload's SHA-256 digest (computed by python <c>hashlib</c>).</summary>
    internal const string Sha256Hex = "5f78c33274e43fa9de5659265c1d917e25c03722dcb0b8d27db8d5feaa813953";

    /// <summary>The reference payload's SHA-512 digest (computed by python <c>hashlib</c>).</summary>
    private const string Sha512Hex = "1284b2d521535196f22175d5f558104220a6ad7680e78b49fa6f20e57ea7b185d71ec1edb137e70eba528dedb141f5d2f8bb53149d262932b27cf41fed96aa7f";

    /// <summary>The reference payload's SHA3-256 digest (computed by python <c>hashlib</c>).</summary>
    private const string Sha3256Hex = "352b82608dad6c7ac3dd665bc2666e5d97803cb13f23a1109e2105e93f42c448";

    /// <summary>The reference payload's SHA3-512 digest (computed by python <c>hashlib</c>).</summary>
    private const string Sha3512Hex = "16f4abfb7f079d757a24cf6a12a4ee2c28041cee3fa68cb7a50aa95e33aa87d5ada97274d4dc548499eb23da351b1b3ab7c5a04376f94cab4fe705dc0d171bef";

    /// <summary>A <c>checksum</c> attribute parses into its algorithm and digest.</summary>
    [ Fact ]
    public void ParsesAlgorithmAndDigest()
    {
        XISFChecksum checksum = new XISFChecksum( $"sha-256:{ Sha256Hex }" );

        Assert.Equal( XISFChecksum.Algorithm.Sha256, checksum.ChecksumAlgorithm );
        Assert.Equal( Sha256Hex, checksum.Digest );
    }

    /// <summary>Every hyphenated and unhyphenated algorithm spelling is accepted.</summary>
    [ Fact ]
    public void AcceptsAlgorithmAliases()
    {
        Assert.Equal( XISFChecksum.Algorithm.Sha1,     new XISFChecksum( "sha1:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha1,     new XISFChecksum( "sha-1:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha256,   new XISFChecksum( "sha256:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha256,   new XISFChecksum( "sha-256:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha512,   new XISFChecksum( "sha512:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha512,   new XISFChecksum( "sha-512:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha3_256, new XISFChecksum( "sha3-256:00" ).ChecksumAlgorithm );
        Assert.Equal( XISFChecksum.Algorithm.Sha3_512, new XISFChecksum( "sha3-512:00" ).ChecksumAlgorithm );
    }

    /// <summary>An unrecognized algorithm is rejected.</summary>
    [ Fact ]
    public void RejectsUnknownAlgorithm()
    {
        Assert.Throws< XISFException >( () => new XISFChecksum( "md5:00" ) );
        Assert.Throws< XISFException >( () => new XISFChecksum( "sha3-384:00" ) );
    }

    /// <summary>A malformed attribute or an empty digest is rejected.</summary>
    [ Fact ]
    public void RejectsMalformedAttribute()
    {
        Assert.Throws< XISFException >( () => new XISFChecksum( "sha-256" ) );
        Assert.Throws< XISFException >( () => new XISFChecksum( "" ) );
        Assert.Throws< XISFException >( () => new XISFChecksum( ":abc" ) );
        Assert.Throws< XISFException >( () => new XISFChecksum( "sha-256:" ) );
    }

    /// <summary>A SHA-1 checksum matches the digest of the reference payload.</summary>
    [ Fact ]
    public void MatchesSha1()
    {
        Assert.True( new XISFChecksum( $"sha-1:{ Sha1Hex }" ).Matches( Payload ) );
    }

    /// <summary>A SHA-256 checksum matches the digest of the reference payload.</summary>
    [ Fact ]
    public void MatchesSha256()
    {
        Assert.True( new XISFChecksum( $"sha-256:{ Sha256Hex }" ).Matches( Payload ) );
    }

    /// <summary>A SHA-512 checksum matches the digest of the reference payload.</summary>
    [ Fact ]
    public void MatchesSha512()
    {
        Assert.True( new XISFChecksum( $"sha-512:{ Sha512Hex }" ).Matches( Payload ) );
    }

    /// <summary>
    /// A SHA-3 checksum matches where the platform provides SHA-3; where it does
    /// not, verification fails cleanly with an unsupported error rather than passing.
    /// </summary>
    [ Fact ]
    public void MatchesSha3()
    {
        if( SHA3_256.IsSupported && SHA3_512.IsSupported )
        {
            Assert.True( new XISFChecksum( $"sha3-256:{ Sha3256Hex }" ).Matches( Payload ) );
            Assert.True( new XISFChecksum( $"sha3-512:{ Sha3512Hex }" ).Matches( Payload ) );
        }
        else
        {
            Assert.Throws< XISFException >( () => new XISFChecksum( $"sha3-256:{ Sha3256Hex }" ).Matches( Payload ) );
            Assert.Throws< XISFException >( () => new XISFChecksum( $"sha3-512:{ Sha3512Hex }" ).Matches( Payload ) );
        }
    }

    /// <summary>A checksum with the wrong digest does not match.</summary>
    [ Fact ]
    public void DoesNotMatchWrongDigest()
    {
        XISFChecksum checksum = new XISFChecksum( "sha-256:0000000000000000000000000000000000000000000000000000000000000000" );

        Assert.False( checksum.Matches( Payload ) );
    }

    /// <summary>Digest matching is case-insensitive over the declared hex string.</summary>
    [ Fact ]
    public void MatchesUppercaseDigest()
    {
        Assert.True( new XISFChecksum( $"sha-1:{ Sha1Hex.ToUpperInvariant() }" ).Matches( Payload ) );
    }

    /// <summary>Verification throws on a digest mismatch.</summary>
    [ Fact ]
    public void VerifyThrowsOnMismatch()
    {
        XISFChecksum checksum = new XISFChecksum( "sha-1:0000000000000000000000000000000000000000" );

        Assert.Throws< XISFException >( () => checksum.Verify( Payload ) );
    }

    /// <summary>Verification succeeds silently on a digest match.</summary>
    [ Fact ]
    public void VerifySucceedsOnMatch()
    {
        new XISFChecksum( $"sha-1:{ Sha1Hex }" ).Verify( Payload );
    }

    /// <summary>The description is a single-line, human-readable summary.</summary>
    [ Fact ]
    public void Description()
    {
        Assert.Equal( $"XISFChecksum {{ sha-256: { Sha256Hex } }}", new XISFChecksum( $"sha-256:{ Sha256Hex }" ).ToString() );
    }

    /// <summary>Two checksums are equal when their algorithm and digest agree.</summary>
    [ Fact ]
    public void EqualityComparesAlgorithmAndDigest()
    {
        XISFChecksum a = new XISFChecksum( "sha-256:aa" );
        XISFChecksum b = new XISFChecksum( "sha256:aa" );
        XISFChecksum c = new XISFChecksum( "sha-256:bb" );

        Assert.Equal( a, b );
        Assert.Equal( a.GetHashCode(), b.GetHashCode() );
        Assert.NotEqual( a, c );
    }

    /// <summary>
    /// A default-constructed checksum (which C# always permits for a struct) has an
    /// empty digest and none of its members throw.
    /// </summary>
    [ Fact ]
    public void DefaultChecksumIsSafe()
    {
        XISFChecksum checksum = default;

        Assert.Equal( "", checksum.Digest );
        Assert.False( checksum.Matches( Payload ) );
        Assert.Equal( "XISFChecksum { sha-1:  }", checksum.ToString() );
        Assert.Equal( checksum, default( XISFChecksum ) );
        Assert.Equal( checksum.GetHashCode(), default( XISFChecksum ).GetHashCode() );
    }
}
