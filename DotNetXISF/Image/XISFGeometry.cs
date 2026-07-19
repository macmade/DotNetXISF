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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotNetXISF;

/// <summary>
/// The geometry of an XISF image, parsed from its <c>geometry</c> attribute.
/// </summary>
/// <remarks>
/// The attribute is a colon-separated list where the <strong>last</strong> value
/// is the channel count and the values before it are the image's spatial
/// dimensions, most-significant first - for a 2D image,
/// <c>width:height:channels</c>. At least one spatial dimension and the channel
/// count are required. The dimensions, channel count and the derived
/// <see cref="PixelCount"/> and <see cref="SampleCount"/> are all 64-bit, so their
/// products cannot overflow for any realistic image.
/// </remarks>
public readonly struct XISFGeometry : IEquatable< XISFGeometry >
{
    /// <summary>The backing store of <see cref="Dimensions"/>.</summary>
    /// <remarks>
    /// Nullable because C# permits a default-constructed struct, on which this
    /// field is <c>null</c>; <see cref="Dimensions"/> coalesces that to an empty
    /// list so the derived members never dereference null.
    /// </remarks>
    private readonly long[]? dimensions;

    /// <summary>
    /// The spatial dimensions, most-significant first (for a 2D image, width then
    /// height). Always at least one, all strictly positive, for a parsed geometry;
    /// empty for a default-constructed one.
    /// </summary>
    public IReadOnlyList< long > Dimensions => this.dimensions ?? Array.Empty< long >();

    /// <summary>The number of channels (strictly positive).</summary>
    public long ChannelCount { get; }

    /// <summary>The number of pixels: the product of the spatial <see cref="Dimensions"/>.</summary>
    public long PixelCount => this.Dimensions.Aggregate( 1L, ( product, dimension ) => product * dimension );

    /// <summary>The total number of samples: <see cref="PixelCount"/> times <see cref="ChannelCount"/>.</summary>
    public long SampleCount => this.PixelCount * this.ChannelCount;

    /// <summary>
    /// Parses a <c>geometry</c> attribute of the form
    /// <c>d1:d2:…:dN:channelCount</c>.
    /// </summary>
    /// <param name="attribute">The raw <c>geometry</c> attribute value.</param>
    /// <exception cref="XISFException">
    /// There are fewer than two components, or any component is not a strictly
    /// positive integer (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public XISFGeometry( string attribute )
    {
        string[] components = attribute.Split( ':' );

        if( components.Length < 2 )
        {
            throw XISFException.InvalidElement( $"Geometry must have at least one dimension and a channel count: '{ attribute }'" );
        }

        long[] values = new long[ components.Length ];

        for( int index = 0; index < components.Length; index += 1 )
        {
            if( long.TryParse( components[ index ], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value ) == false || value <= 0 )
            {
                throw XISFException.InvalidElement( $"Invalid geometry component '{ components[ index ] }' in '{ attribute }'" );
            }

            values[ index ] = value;
        }

        this.dimensions   = values[ ..^1 ];
        this.ChannelCount = values[ ^1 ];
    }

    /// <summary>
    /// The <c>geometry</c> attribute form: the dimensions and channel count joined
    /// with colons (for example <c>2159:3839:3</c>).
    /// </summary>
    /// <returns>The formatted attribute string.</returns>
    public override string ToString()
    {
        IEnumerable< long > values = this.Dimensions.Append( this.ChannelCount );

        return string.Join( ":", values.Select( value => value.ToString( CultureInfo.InvariantCulture ) ) );
    }

    /// <summary>
    /// Returns whether this geometry equals another: the same channel count and the
    /// same spatial dimensions in order.
    /// </summary>
    /// <param name="other">The geometry to compare against.</param>
    /// <returns><c>true</c> if the geometries are equal.</returns>
    public bool Equals( XISFGeometry other )
    {
        return this.ChannelCount == other.ChannelCount && this.Dimensions.SequenceEqual( other.Dimensions );
    }

    /// <summary>
    /// Returns whether this geometry equals another object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal geometry.</returns>
    public override bool Equals( object? obj ) => obj is XISFGeometry other && this.Equals( other );

    /// <summary>
    /// A hash code combining the channel count and the spatial dimensions.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        HashCode hash = new HashCode();

        hash.Add( this.ChannelCount );

        foreach( long dimension in this.Dimensions )
        {
            hash.Add( dimension );
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns whether two geometries are equal.</summary>
    /// <param name="left">The first geometry.</param>
    /// <param name="right">The second geometry.</param>
    /// <returns><c>true</c> if the geometries are equal.</returns>
    public static bool operator ==( XISFGeometry left, XISFGeometry right ) => left.Equals( right );

    /// <summary>Returns whether two geometries are unequal.</summary>
    /// <param name="left">The first geometry.</param>
    /// <param name="right">The second geometry.</param>
    /// <returns><c>true</c> if the geometries are unequal.</returns>
    public static bool operator !=( XISFGeometry left, XISFGeometry right ) => left.Equals( right ) == false;
}
