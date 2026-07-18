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

using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFParsingOptions"/>.
/// </summary>
/// <remarks>
/// A guard test over a hand-assigned bit set and its two composite presets, which
/// the compiler cannot check for a wrong shift, a bit collision or a missing
/// preset member.
/// </remarks>
public class XISFParsingOptionsTests
{
    /// <summary>
    /// Each flag has its expected, distinct bit value (and the empty set is zero),
    /// asserted against independent literals so a wrong shift is caught.
    /// </summary>
    [ Fact ]
    public void EachFlagHasItsExpectedBitValue()
    {
        Assert.Equal( 0, ( int )XISFParsingOptions.None );
        Assert.Equal( 1, ( int )XISFParsingOptions.VerifyChecksums );
        Assert.Equal( 2, ( int )XISFParsingOptions.AllowExternalLocations );
        Assert.Equal( 4, ( int )XISFParsingOptions.AllowSpecDeviations );
    }

    /// <summary>
    /// The strict preset is exactly checksum verification: it verifies checksums
    /// and enables neither spec-deviation tolerance nor external-location
    /// resolution.
    /// </summary>
    [ Fact ]
    public void StrictPresetVerifiesChecksumsOnly()
    {
        XISFParsingOptions options = XISFParsingOptions.Strict;

        Assert.True( options.HasFlag( XISFParsingOptions.VerifyChecksums ) );
        Assert.False( options.HasFlag( XISFParsingOptions.AllowSpecDeviations ) );
        Assert.False( options.HasFlag( XISFParsingOptions.AllowExternalLocations ) );
    }

    /// <summary>
    /// The lenient preset is exactly spec-deviation tolerance: it tolerates
    /// deviations and enables neither checksum verification nor external-location
    /// resolution.
    /// </summary>
    [ Fact ]
    public void LenientPresetToleratesDeviationsOnly()
    {
        XISFParsingOptions options = XISFParsingOptions.Lenient;

        Assert.True( options.HasFlag( XISFParsingOptions.AllowSpecDeviations ) );
        Assert.False( options.HasFlag( XISFParsingOptions.VerifyChecksums ) );
        Assert.False( options.HasFlag( XISFParsingOptions.AllowExternalLocations ) );
    }

    /// <summary>
    /// Both presets leave external/distributed data-block resolution disabled; it
    /// must be opted into explicitly for security.
    /// </summary>
    [ Fact ]
    public void ExternalLocationsAreOffInBothPresets()
    {
        Assert.False( XISFParsingOptions.Strict.HasFlag( XISFParsingOptions.AllowExternalLocations ) );
        Assert.False( XISFParsingOptions.Lenient.HasFlag( XISFParsingOptions.AllowExternalLocations ) );
    }

    /// <summary>
    /// A value reconstructed from a flag's raw integer round-trips back to the same
    /// flag.
    /// </summary>
    [ Fact ]
    public void RawValueRoundTrips()
    {
        XISFParsingOptions options = ( XISFParsingOptions )( int )XISFParsingOptions.VerifyChecksums;

        Assert.True( options.HasFlag( XISFParsingOptions.VerifyChecksums ) );
    }
}
