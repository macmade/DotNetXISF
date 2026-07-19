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
using System.Threading;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFValue"/>: its typed accessors, kind
/// discrimination, equality and hashing (including the <c>NaN</c>-equal rule),
/// and its invariant-culture attribute parsing of every value-attribute type.
/// </summary>
public class XISFValueTests
{
    /// <summary>
    /// A typed accessor returns the payload when the value's kind matches.
    /// </summary>
    [ Fact ]
    public void AccessorReturnsPayloadForMatchingCase()
    {
        Assert.Equal< bool?   >( true, XISFValue.Boolean( true ).AsBoolean );
        Assert.Equal< long?   >( 42,   XISFValue.Integer( 42 ).AsInteger );
        Assert.Equal< ulong?  >( 42,   XISFValue.UnsignedInteger( 42 ).AsUnsignedInteger );
        Assert.Equal< double? >( 42.5, XISFValue.Float( 42.5 ).AsFloat );
        Assert.Equal( "hi", XISFValue.String( "hi" ).AsString );

        ( double Real, double Imaginary )? complex = XISFValue.Complex( 1.5, -2.5 ).AsComplex;

        Assert.NotNull( complex );
        Assert.Equal( 1.5,  complex.Value.Real );
        Assert.Equal( -2.5, complex.Value.Imaginary );

        DateTimeOffset timePoint = new DateTimeOffset( 2021, 1, 2, 3, 4, 5, TimeSpan.Zero );

        Assert.Equal< DateTimeOffset? >( timePoint, XISFValue.TimePoint( timePoint ).AsTimePoint );
    }

    /// <summary>
    /// A typed accessor returns <c>null</c> when the value's kind does not match.
    /// </summary>
    [ Fact ]
    public void AccessorReturnsNullForNonMatchingCase()
    {
        Assert.Null( XISFValue.Integer( 42 ).AsBoolean );
        Assert.Null( XISFValue.Integer( 42 ).AsUnsignedInteger );
        Assert.Null( XISFValue.Integer( 42 ).AsFloat );
        Assert.Null( XISFValue.String( "hi" ).AsInteger );
        Assert.Null( XISFValue.Float( 1 ).AsComplex );
        Assert.Null( XISFValue.Boolean( true ).AsTimePoint );
        Assert.Null( XISFValue.Integer( 42 ).AsData );
    }

    /// <summary>
    /// The <see cref="XISFValue.Kind"/> discriminator matches the value's case.
    /// </summary>
    [ Fact ]
    public void KindDerivesFromCase()
    {
        Assert.Equal( XISFValueKind.Boolean,         XISFValue.Boolean( true ).Kind );
        Assert.Equal( XISFValueKind.Integer,         XISFValue.Integer( 1 ).Kind );
        Assert.Equal( XISFValueKind.UnsignedInteger, XISFValue.UnsignedInteger( 1 ).Kind );
        Assert.Equal( XISFValueKind.Float,           XISFValue.Float( 1 ).Kind );
        Assert.Equal( XISFValueKind.Complex,         XISFValue.Complex( 0, 0 ).Kind );
        Assert.Equal( XISFValueKind.String,          XISFValue.String( "x" ).Kind );
        Assert.Equal( XISFValueKind.TimePoint,       XISFValue.TimePoint( DateTimeOffset.UnixEpoch ).Kind );
        Assert.Equal( XISFValueKind.Data,            XISFValue.Data( new byte[] { 1, 2 } ).Kind );
    }

    /// <summary>
    /// Each kind describes as a human-readable name.
    /// </summary>
    [ Fact ]
    public void KindDescription()
    {
        Assert.Equal( "Boolean",          XISFValueKind.Boolean.Description() );
        Assert.Equal( "Integer",          XISFValueKind.Integer.Description() );
        Assert.Equal( "Unsigned Integer", XISFValueKind.UnsignedInteger.Description() );
        Assert.Equal( "Float",            XISFValueKind.Float.Description() );
        Assert.Equal( "Complex",          XISFValueKind.Complex.Description() );
        Assert.Equal( "String",           XISFValueKind.String.Description() );
        Assert.Equal( "Time Point",       XISFValueKind.TimePoint.Description() );
        Assert.Equal( "Data",             XISFValueKind.Data.Description() );
    }

    /// <summary>
    /// Equality compares matching payloads, treats <c>NaN</c> as equal to
    /// <c>NaN</c> (both for floats and for either complex component), and never
    /// equates differing kinds.
    /// </summary>
    [ Fact ]
    public void EqualityAndNaN()
    {
        Assert.Equal( XISFValue.Integer( 42 ), XISFValue.Integer( 42 ) );
        Assert.NotEqual( XISFValue.Integer( 42 ), XISFValue.Integer( 43 ) );
        Assert.NotEqual( XISFValue.Integer( 42 ), XISFValue.UnsignedInteger( 42 ) );
        Assert.Equal( XISFValue.Float( double.NaN ), XISFValue.Float( double.NaN ) );
        Assert.Equal( XISFValue.Float( 1.5 ), XISFValue.Float( 1.5 ) );
        Assert.Equal( XISFValue.Complex( double.NaN, 1 ), XISFValue.Complex( double.NaN, 1 ) );
        Assert.NotEqual( XISFValue.Complex( 1, 2 ), XISFValue.Complex( 1, 3 ) );

        Assert.True( XISFValue.Integer( 42 )  == XISFValue.Integer( 42 ) );
        Assert.True( XISFValue.Integer( 42 )  != XISFValue.Integer( 43 ) );
    }

    /// <summary>
    /// Data values compare by their byte contents, independent of the backing
    /// buffer, and hash consistently.
    /// </summary>
    [ Fact ]
    public void DataEqualityIsByContent()
    {
        XISFValue a = XISFValue.Data( new byte[] { 1, 2, 3 } );
        XISFValue b = XISFValue.Data( new byte[] { 1, 2, 3 } );
        XISFValue c = XISFValue.Data( new byte[] { 1, 2, 4 } );

        Assert.Equal( a, b );
        Assert.Equal( a.GetHashCode(), b.GetHashCode() );
        Assert.NotEqual( a, c );

        byte[] bytes = { 9, 8, 7 };

        Assert.Equal( bytes, XISFValue.Data( bytes ).AsData?.ToArray() );
    }

    /// <summary>
    /// Distinct values hash to distinct buckets, and two equal <c>NaN</c> floats
    /// hash alike.
    /// </summary>
    [ Fact ]
    public void DistinctValuesHashDistinctly()
    {
        XISFValue[] values =
        {
            XISFValue.Boolean( true ),
            XISFValue.Integer( 1 ),
            XISFValue.UnsignedInteger( 1 ),
            XISFValue.Float( 1 ),
            XISFValue.Complex( 1, 1 ),
            XISFValue.String( "x" ),
        };

        HashSet< int > hashes = new HashSet< int >();

        foreach( XISFValue value in values )
        {
            hashes.Add( value.GetHashCode() );
        }

        Assert.Equal( values.Length, hashes.Count );
        Assert.Equal( XISFValue.Float( double.NaN ).GetHashCode(), XISFValue.Float( double.NaN ).GetHashCode() );
    }

    /// <summary>
    /// A default-constructed value (which C# always permits for a struct) is a
    /// safe boolean <c>false</c>, and none of its members throw.
    /// </summary>
    [ Fact ]
    public void DefaultValueIsSafe()
    {
        XISFValue value = default;

        Assert.Equal( XISFValueKind.Boolean, value.Kind );
        Assert.Equal< bool? >( false, value.AsBoolean );
        Assert.Null( value.AsInteger );
        Assert.Null( value.AsString );
        Assert.Null( value.AsData );
        Assert.Equal( value, default( XISFValue ) );
        Assert.Equal( value.GetHashCode(), default( XISFValue ).GetHashCode() );
    }

    /// <summary>
    /// Booleans parse from <c>1</c>/<c>true</c> and <c>0</c>/<c>false</c>,
    /// case-insensitively for the words; anything else is rejected.
    /// </summary>
    [ Fact ]
    public void ParsesBoolean()
    {
        Assert.Equal( XISFValue.Boolean( true ),  XISFValue.FromAttribute( "1",     XISFPropertyType.Boolean ) );
        Assert.Equal( XISFValue.Boolean( false ), XISFValue.FromAttribute( "0",     XISFPropertyType.Boolean ) );
        Assert.Equal( XISFValue.Boolean( true ),  XISFValue.FromAttribute( "true",  XISFPropertyType.Boolean ) );
        Assert.Equal( XISFValue.Boolean( false ), XISFValue.FromAttribute( "False", XISFPropertyType.Boolean ) );

        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "maybe", XISFPropertyType.Boolean ) );
    }

    /// <summary>
    /// Signed integers parse per declared width and reject overflow and non-digits.
    /// </summary>
    [ Fact ]
    public void ParsesSignedIntegers()
    {
        Assert.Equal( XISFValue.Integer( 42 ), XISFValue.FromAttribute( "42", XISFPropertyType.Int32 ) );
        Assert.Equal( XISFValue.Integer( -5 ), XISFValue.FromAttribute( "-5", XISFPropertyType.Int8 ) );
        Assert.Equal( XISFValue.Integer( 9223372036854775807 ), XISFValue.FromAttribute( "9223372036854775807", XISFPropertyType.Int64 ) );

        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "300", XISFPropertyType.Int8 ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "abc", XISFPropertyType.Int32 ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "99999999999999999999", XISFPropertyType.Int64 ) );
    }

    /// <summary>
    /// Unsigned integers parse per declared width and reject negatives and overflow.
    /// </summary>
    [ Fact ]
    public void ParsesUnsignedIntegers()
    {
        Assert.Equal( XISFValue.UnsignedInteger( 200 ), XISFValue.FromAttribute( "200", XISFPropertyType.UInt8 ) );
        Assert.Equal( XISFValue.UnsignedInteger( 18446744073709551615 ), XISFValue.FromAttribute( "18446744073709551615", XISFPropertyType.UInt64 ) );

        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "-1",  XISFPropertyType.UInt8 ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "300", XISFPropertyType.UInt8 ) );
    }

    /// <summary>
    /// Floats parse from decimal and exponential notation, accept <c>nan</c>, and
    /// reject non-numeric text.
    /// </summary>
    [ Fact ]
    public void ParsesFloats()
    {
        Assert.Equal( XISFValue.Float( 1.5 ),    XISFValue.FromAttribute( "1.5",   XISFPropertyType.Float64 ) );
        Assert.Equal( XISFValue.Float( 1000.0 ), XISFValue.FromAttribute( "1.0e3", XISFPropertyType.Float32 ) );

        XISFValue nan = XISFValue.FromAttribute( "nan", XISFPropertyType.Float64 );

        Assert.True( nan.AsFloat.HasValue && double.IsNaN( nan.AsFloat.Value ) );

        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "x", XISFPropertyType.Float64 ) );
    }

    /// <summary>
    /// Complex values parse from the <c>(real,imaginary)</c> form (with optional
    /// interior whitespace) and reject anything malformed.
    /// </summary>
    [ Fact ]
    public void ParsesComplex()
    {
        Assert.Equal( XISFValue.Complex( 1.5, -2.5 ), XISFValue.FromAttribute( "(1.5,-2.5)", XISFPropertyType.Complex64 ) );
        Assert.Equal( XISFValue.Complex( 1, 2 ),      XISFValue.FromAttribute( "( 1 , 2 )",  XISFPropertyType.Complex32 ) );

        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "(1.5)",   XISFPropertyType.Complex64 ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "1.5,2.5", XISFPropertyType.Complex64 ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "(a,b)",   XISFPropertyType.Complex64 ) );
    }

    /// <summary>
    /// Time points parse from ISO 8601 internet date-time, with or without
    /// fractional seconds, and reject non-dates.
    /// </summary>
    [ Fact ]
    public void ParsesTimePoint()
    {
        XISFValue value = XISFValue.FromAttribute( "2021-01-02T03:04:05Z", XISFPropertyType.TimePoint );

        Assert.NotNull( value.AsTimePoint );

        DateTimeOffset date = value.AsTimePoint.Value.ToUniversalTime();

        Assert.Equal( 2021, date.Year );
        Assert.Equal( 1,    date.Month );
        Assert.Equal( 2,    date.Day );
        Assert.Equal( 3,    date.Hour );
        Assert.Equal( 4,    date.Minute );
        Assert.Equal( 5,    date.Second );

        Assert.NotNull( XISFValue.FromAttribute( "2021-01-02T03:04:05.500Z", XISFPropertyType.TimePoint ).AsTimePoint );
        Assert.NotNull( XISFValue.FromAttribute( "2021-01-02T03:04:05+02:00", XISFPropertyType.TimePoint ).AsTimePoint );

        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "not a date", XISFPropertyType.TimePoint ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "2021-01-02",  XISFPropertyType.TimePoint ) );

        // A zone designator is required, so the parsed instant is deterministic and
        // independent of the host time zone.
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "2021-01-02T03:04:05", XISFPropertyType.TimePoint ) );
    }

    /// <summary>
    /// Types not carried in a <c>value</c> attribute (strings, byte arrays,
    /// vectors and matrices) are rejected.
    /// </summary>
    [ Fact ]
    public void RejectsNonValueAttributeTypes()
    {
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "x",   XISFPropertyType.String ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "1 2", XISFPropertyType.UI8Vector ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "1 2", XISFPropertyType.F32Matrix ) );
        Assert.Throws< XISFException >( () => XISFValue.FromAttribute( "00",  XISFPropertyType.ByteArray ) );
    }

    /// <summary>
    /// Parsing is culture-invariant: the same float, complex and time-point strings
    /// parse identically under a culture whose number and date formats differ from
    /// the invariant culture.
    /// </summary>
    [ Fact ]
    public void ParsingIsCultureInvariant()
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo( "fr-FR" );

            Assert.Equal( XISFValue.Float( 1.5 ),         XISFValue.FromAttribute( "1.5",       XISFPropertyType.Float64 ) );
            Assert.Equal( XISFValue.Complex( 1.5, -2.5 ), XISFValue.FromAttribute( "(1.5,-2.5)", XISFPropertyType.Complex64 ) );

            DateTimeOffset expected = new DateTimeOffset( 2021, 1, 2, 3, 4, 5, TimeSpan.Zero );
            XISFValue      parsed   = XISFValue.FromAttribute( "2021-01-02T03:04:05Z", XISFPropertyType.TimePoint );

            Assert.Equal< DateTimeOffset? >( expected, parsed.AsTimePoint );
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
