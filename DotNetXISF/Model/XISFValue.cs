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
using System.Numerics;

namespace DotNetXISF;

/// <summary>
/// The typed value of an XISF property.
/// </summary>
/// <remarks>
/// This models the scalar value categories - boolean, signed and unsigned
/// integers, floating-point, complex, string and time-point - plus the opaque
/// decoded bytes (<see cref="XISFValueKind.Data"/>) of a vector-, matrix- or
/// <c>ByteArray</c>-typed value carried in a data block.
/// <para>
/// A lean value type: a single 8-byte numeric slot reinterprets the bits of the
/// <c>bool</c>/<see cref="long"/>/<see cref="ulong"/>/<see cref="double"/>
/// payloads (and a complex value's real component), a second slot holds a complex
/// value's imaginary component, and dedicated slots hold the string, time-point
/// and byte payloads. A <see cref="XISFValueKind"/> discriminator selects the
/// active case. The default value (<c>default(XISFValue)</c>) is a boolean
/// <c>false</c>.
/// </para>
/// <para>
/// Equality treats two floating-point payloads of <c>NaN</c> as equal, departing
/// from IEEE 754, so comparing or diffing headers does not report a spurious
/// change; this applies to <see cref="XISFValueKind.Float"/> and to either
/// component of a <see cref="XISFValueKind.Complex"/> value.
/// <see cref="GetHashCode"/> is kept consistent, hashing every <c>NaN</c> to one
/// constant so equal-<c>NaN</c> values share a bucket. Byte payloads compare and
/// hash by content.
/// </para>
/// </remarks>
public readonly struct XISFValue : IEquatable< XISFValue >
{
    /// <summary>The active case discriminator.</summary>
    private readonly XISFValueKind kind;

    /// <summary>
    /// The primary numeric payload slot, reinterpreted per <see cref="kind"/>: a
    /// boolean (0 or 1), a <see cref="long"/>, the bits of a <see cref="ulong"/>,
    /// or the bit pattern of a <see cref="double"/> (a float, or a complex value's
    /// real component). Unused for the string, time-point and data kinds.
    /// </summary>
    private readonly long numeric;

    /// <summary>
    /// The secondary numeric payload slot: the bit pattern of a complex value's
    /// imaginary component. Unused for every other kind.
    /// </summary>
    private readonly long numericSecondary;

    /// <summary>
    /// The string payload for the <see cref="XISFValueKind.String"/> kind;
    /// <c>null</c> otherwise.
    /// </summary>
    private readonly string? text;

    /// <summary>
    /// The time-point payload for the <see cref="XISFValueKind.TimePoint"/> kind;
    /// the default otherwise.
    /// </summary>
    private readonly DateTimeOffset timePoint;

    /// <summary>
    /// The opaque bytes payload for the <see cref="XISFValueKind.Data"/> kind;
    /// empty otherwise.
    /// </summary>
    private readonly ReadOnlyMemory< byte > data;

    /// <summary>
    /// The date/time formats accepted for a time-point value: the ISO 8601 internet
    /// date-time, with or without fractional seconds, and with either the <c>Z</c>
    /// zone designator or a numeric offset. A zone designator is <strong>required</strong>,
    /// so the parsed instant is independent of the host time zone.
    /// </summary>
    private static readonly string[] TimePointFormats =
    [
        "yyyy-MM-dd'T'HH:mm:ss'Z'",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
    ];

    /// <summary>
    /// Initializes a value with the given kind and payload slots.
    /// </summary>
    /// <param name="kind">The active case discriminator.</param>
    /// <param name="numeric">The primary numeric payload slot.</param>
    /// <param name="numericSecondary">The secondary numeric payload slot.</param>
    /// <param name="text">The string payload, or <c>null</c>.</param>
    /// <param name="timePoint">The time-point payload.</param>
    /// <param name="data">The opaque bytes payload.</param>
    private XISFValue( XISFValueKind kind, long numeric, long numericSecondary, string? text, DateTimeOffset timePoint, ReadOnlyMemory< byte > data )
    {
        this.kind             = kind;
        this.numeric          = numeric;
        this.numericSecondary = numericSecondary;
        this.text             = text;
        this.timePoint        = timePoint;
        this.data             = data;
    }

    /// <summary>Creates a boolean value.</summary>
    /// <param name="value">The boolean payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue Boolean( bool value ) => new XISFValue( XISFValueKind.Boolean, value ? 1L : 0L, 0L, null, default, default );

    /// <summary>Creates a signed-integer value (any width, widened to <see cref="long"/>).</summary>
    /// <param name="value">The signed-integer payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue Integer( long value ) => new XISFValue( XISFValueKind.Integer, value, 0L, null, default, default );

    /// <summary>Creates an unsigned-integer value (any width, widened to <see cref="ulong"/>).</summary>
    /// <param name="value">The unsigned-integer payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue UnsignedInteger( ulong value ) => new XISFValue( XISFValueKind.UnsignedInteger, unchecked( ( long )value ), 0L, null, default, default );

    /// <summary>Creates a floating-point value.</summary>
    /// <param name="value">The floating-point payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue Float( double value ) => new XISFValue( XISFValueKind.Float, BitConverter.DoubleToInt64Bits( value ), 0L, null, default, default );

    /// <summary>Creates a complex value with the given real and imaginary components.</summary>
    /// <param name="real">The real component.</param>
    /// <param name="imaginary">The imaginary component.</param>
    /// <returns>The created value.</returns>
    public static XISFValue Complex( double real, double imaginary ) => new XISFValue( XISFValueKind.Complex, BitConverter.DoubleToInt64Bits( real ), BitConverter.DoubleToInt64Bits( imaginary ), null, default, default );

    /// <summary>Creates a character-string value.</summary>
    /// <param name="value">The string payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue String( string value ) => new XISFValue( XISFValueKind.String, 0L, 0L, value, default, default );

    /// <summary>Creates a date/time value.</summary>
    /// <param name="value">The time-point payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue TimePoint( DateTimeOffset value ) => new XISFValue( XISFValueKind.TimePoint, 0L, 0L, null, value, default );

    /// <summary>
    /// Creates an opaque-bytes value: the decoded bytes of a vector-, matrix- or
    /// <c>ByteArray</c>-typed value carried in a data block.
    /// </summary>
    /// <param name="value">The opaque bytes payload.</param>
    /// <returns>The created value.</returns>
    public static XISFValue Data( ReadOnlyMemory< byte > value ) => new XISFValue( XISFValueKind.Data, 0L, 0L, null, default, value );

    /// <summary>The <see cref="XISFValueKind"/> matching this value's case.</summary>
    public XISFValueKind Kind => this.kind;

    /// <summary>The boolean payload, or <c>null</c> if this is not a boolean value.</summary>
    public bool? AsBoolean => this.kind == XISFValueKind.Boolean ? this.numeric != 0L : null;

    /// <summary>The signed-integer payload, or <c>null</c> if this is not a signed-integer value.</summary>
    public long? AsInteger => this.kind == XISFValueKind.Integer ? this.numeric : null;

    /// <summary>The unsigned-integer payload, or <c>null</c> if this is not an unsigned-integer value.</summary>
    public ulong? AsUnsignedInteger => this.kind == XISFValueKind.UnsignedInteger ? unchecked( ( ulong )this.numeric ) : null;

    /// <summary>The floating-point payload, or <c>null</c> if this is not a float value.</summary>
    public double? AsFloat => this.kind == XISFValueKind.Float ? BitConverter.Int64BitsToDouble( this.numeric ) : null;

    /// <summary>
    /// The complex payload's real and imaginary components, or <c>null</c> if this
    /// is not a complex value.
    /// </summary>
    public ( double Real, double Imaginary )? AsComplex => this.kind == XISFValueKind.Complex ? ( BitConverter.Int64BitsToDouble( this.numeric ), BitConverter.Int64BitsToDouble( this.numericSecondary ) ) : null;

    /// <summary>The string payload, or <c>null</c> if this is not a string value.</summary>
    public string? AsString => this.kind == XISFValueKind.String ? this.text : null;

    /// <summary>The time-point payload, or <c>null</c> if this is not a time-point value.</summary>
    public DateTimeOffset? AsTimePoint => this.kind == XISFValueKind.TimePoint ? this.timePoint : null;

    /// <summary>The opaque bytes payload, or <c>null</c> if this is not a data value.</summary>
    /// <remarks>
    /// The active branch is typed as the nullable form on purpose: a bare
    /// <c>null</c> would otherwise bind to the implicit <c>byte[]</c>-to-
    /// <see cref="ReadOnlyMemory{T}"/> conversion and yield an empty buffer rather
    /// than a genuine <c>null</c>.
    /// </remarks>
    public ReadOnlyMemory< byte >? AsData => this.kind == XISFValueKind.Data ? ( ReadOnlyMemory< byte >? )this.data : null;

    /// <summary>Returns whether two values are equal.</summary>
    /// <param name="lhs">A value to compare.</param>
    /// <param name="rhs">Another value to compare.</param>
    /// <returns><c>true</c> if the two values are equal.</returns>
    public static bool operator ==( XISFValue lhs, XISFValue rhs ) => lhs.Equals( rhs );

    /// <summary>Returns whether two values are not equal.</summary>
    /// <param name="lhs">A value to compare.</param>
    /// <param name="rhs">Another value to compare.</param>
    /// <returns><c>true</c> if the two values are not equal.</returns>
    public static bool operator !=( XISFValue lhs, XISFValue rhs ) => lhs.Equals( rhs ) == false;

    /// <summary>Returns whether this value equals another.</summary>
    /// <remarks>
    /// Matching kinds compare their payloads, except two floating-point payloads of
    /// <c>NaN</c> are treated as equal (unlike IEEE 754), both for a float and for
    /// either component of a complex value. Byte payloads compare by content.
    /// Differing kinds are never equal.
    /// </remarks>
    /// <param name="other">The value to compare with.</param>
    /// <returns><c>true</c> if the two values are equal.</returns>
    public bool Equals( XISFValue other )
    {
        if( this.kind != other.kind )
        {
            return false;
        }

        return this.kind switch
        {
            XISFValueKind.Boolean         => this.numeric == other.numeric,
            XISFValueKind.Integer         => this.numeric == other.numeric,
            XISFValueKind.UnsignedInteger => this.numeric == other.numeric,
            XISFValueKind.Float           => FloatEquals( BitConverter.Int64BitsToDouble( this.numeric ), BitConverter.Int64BitsToDouble( other.numeric ) ),
            XISFValueKind.Complex         => FloatEquals( BitConverter.Int64BitsToDouble( this.numeric ), BitConverter.Int64BitsToDouble( other.numeric ) )
                                             && FloatEquals( BitConverter.Int64BitsToDouble( this.numericSecondary ), BitConverter.Int64BitsToDouble( other.numericSecondary ) ),
            XISFValueKind.String          => this.text == other.text,
            XISFValueKind.TimePoint       => this.timePoint == other.timePoint,
            XISFValueKind.Data            => this.data.Span.SequenceEqual( other.data.Span ),
            _                             => false,
        };
    }

    /// <summary>Returns whether this value equals another object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal <see cref="XISFValue"/>.</returns>
    public override bool Equals( object? obj ) => obj is XISFValue other && this.Equals( other );

    /// <summary>Returns a hash code consistent with <see cref="Equals(XISFValue)"/>.</summary>
    /// <remarks>
    /// Each kind mixes in its discriminator before its payload. Because equality
    /// treats any two <c>NaN</c> floats as equal, every <c>NaN</c> is hashed to a
    /// single constant so equal-<c>NaN</c> values share a bucket. Byte payloads hash
    /// by content.
    /// </remarks>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        switch( this.kind )
        {
            case XISFValueKind.Boolean:
            case XISFValueKind.Integer:
            case XISFValueKind.UnsignedInteger:

                return HashCode.Combine( this.kind, this.numeric );

            case XISFValueKind.Float:

                return HashCode.Combine( this.kind, NormalizeNaN( BitConverter.Int64BitsToDouble( this.numeric ) ) );

            case XISFValueKind.Complex:

                return HashCode.Combine( this.kind, NormalizeNaN( BitConverter.Int64BitsToDouble( this.numeric ) ), NormalizeNaN( BitConverter.Int64BitsToDouble( this.numericSecondary ) ) );

            case XISFValueKind.String:

                return HashCode.Combine( this.kind, this.text );

            case XISFValueKind.TimePoint:

                return HashCode.Combine( this.kind, this.timePoint );

            case XISFValueKind.Data:

                HashCode hash = new HashCode();

                hash.Add( this.kind );
                hash.AddBytes( this.data.Span );

                return hash.ToHashCode();

            default:

                return 0;
        }
    }

    /// <summary>A readable summary of the value: its kind and its payload.</summary>
    /// <remarks>
    /// The form is <c>Kind(payload)</c> - for example <c>Integer(5)</c>,
    /// <c>Float(1.5)</c>, <c>Complex(1, 2)</c>, <c>String("hi")</c>,
    /// <c>Time Point(2026-07-19T00:00:00.0000000+00:00)</c> or <c>Data(12 bytes)</c>.
    /// A byte payload is summarized by its length rather than dumped, so a large
    /// vector, matrix or byte-array value describes cheaply. Every number is
    /// formatted with <see cref="CultureInfo.InvariantCulture"/>. Like any
    /// <see cref="object.ToString"/>, this never throws: the eight kinds are all
    /// handled, and the unreachable fallback returns the kind's name rather than
    /// throwing.
    /// </remarks>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        // Copied to a local because a local function in a struct cannot access 'this'.
        XISFValueKind kind = this.kind;

        string Describe( string payload ) => $"{ kind.Description() }({ payload })";

        return this.kind switch
        {
            XISFValueKind.Boolean         => Describe( this.numeric != 0L ? "true" : "false" ),
            XISFValueKind.Integer         => Describe( this.numeric.ToString( CultureInfo.InvariantCulture ) ),
            XISFValueKind.UnsignedInteger => Describe( unchecked( ( ulong )this.numeric ).ToString( CultureInfo.InvariantCulture ) ),
            XISFValueKind.Float           => Describe( BitConverter.Int64BitsToDouble( this.numeric ).ToString( CultureInfo.InvariantCulture ) ),
            XISFValueKind.Complex         => Describe( $"{ BitConverter.Int64BitsToDouble( this.numeric ).ToString( CultureInfo.InvariantCulture ) }, { BitConverter.Int64BitsToDouble( this.numericSecondary ).ToString( CultureInfo.InvariantCulture ) }" ),
            XISFValueKind.String          => Describe( $"\"{ this.text ?? "" }\"" ),
            XISFValueKind.TimePoint       => Describe( this.timePoint.ToString( "o", CultureInfo.InvariantCulture ) ),
            XISFValueKind.Data            => Describe( $"{ this.data.Length.ToString( CultureInfo.InvariantCulture ) } bytes" ),
            _                             => this.kind.ToString(),
        };
    }

    /// <summary>
    /// Parses a value from a <c>&lt;Property&gt;</c> element's <c>value</c>
    /// attribute string, for a value-attribute type.
    /// </summary>
    /// <remarks>
    /// Handles the types XISF carries in the <c>value</c> attribute: booleans,
    /// integers (range-checked against the declared width), floating-point, complex
    /// (<c>(real,imaginary)</c>), and time points (ISO 8601). String values (carried
    /// as element content) and vector/matrix values (carried in data blocks) are not
    /// value-attribute types and are rejected. All numeric and date parsing is
    /// invariant-culture.
    /// </remarks>
    /// <param name="attribute">
    /// The raw <c>value</c> attribute string. Surrounding whitespace is ignored.
    /// </param>
    /// <param name="type">The declared property type.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a valid value for <paramref name="type"/>, or
    /// <paramref name="type"/> is not carried in a <c>value</c> attribute
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static XISFValue FromAttribute( string attribute, XISFPropertyType type )
    {
        string trimmed = attribute.Trim();

        return type switch
        {
            XISFPropertyType.Boolean   => ParseBoolean( trimmed, type ),
            XISFPropertyType.Int8      => ParseSignedInteger< sbyte >( trimmed, type ),
            XISFPropertyType.Int16     => ParseSignedInteger< short >( trimmed, type ),
            XISFPropertyType.Int32     => ParseSignedInteger< int >( trimmed, type ),
            XISFPropertyType.Int64     => ParseSignedInteger< long >( trimmed, type ),
            XISFPropertyType.UInt8     => ParseUnsignedInteger< byte >( trimmed, type ),
            XISFPropertyType.UInt16    => ParseUnsignedInteger< ushort >( trimmed, type ),
            XISFPropertyType.UInt32    => ParseUnsignedInteger< uint >( trimmed, type ),
            XISFPropertyType.UInt64    => ParseUnsignedInteger< ulong >( trimmed, type ),
            XISFPropertyType.Float32   => ParseFloat( trimmed, type ),
            XISFPropertyType.Float64   => ParseFloat( trimmed, type ),
            XISFPropertyType.Complex32 => ParseComplex( trimmed, type ),
            XISFPropertyType.Complex64 => ParseComplex( trimmed, type ),
            XISFPropertyType.TimePoint => ParseTimePoint( trimmed, type ),
            _                          => throw XISFException.InvalidElement( $"Type { type.SpecToken() } is not represented by a value attribute" ),
        };
    }

    /// <summary>Returns whether two floats are equal under the <c>NaN</c>-equal rule.</summary>
    /// <param name="a">A float.</param>
    /// <param name="b">Another float.</param>
    /// <returns><c>true</c> if equal, treating <c>NaN</c> as equal to <c>NaN</c>.</returns>
    private static bool FloatEquals( double a, double b ) => a == b || ( double.IsNaN( a ) && double.IsNaN( b ) );

    /// <summary>
    /// Collapses every <c>NaN</c> to the one canonical <c>NaN</c> so that equal
    /// (under the <c>NaN</c>-equal rule) floats hash alike.
    /// </summary>
    /// <param name="value">The float to normalize.</param>
    /// <returns>The canonical <c>NaN</c> if <paramref name="value"/> is any <c>NaN</c>, otherwise <paramref name="value"/>.</returns>
    private static double NormalizeNaN( double value ) => double.IsNaN( value ) ? double.NaN : value;

    /// <summary>Parses a boolean value.</summary>
    /// <remarks>Accepts <c>1</c>/<c>true</c> and <c>0</c>/<c>false</c>, case-insensitively for the words.</remarks>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="type">The declared property type, for error reporting.</param>
    /// <returns>The parsed boolean value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a recognized boolean literal
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseBoolean( string value, XISFPropertyType type )
    {
        if( value == "1" || string.Equals( value, "true", StringComparison.OrdinalIgnoreCase ) )
        {
            return Boolean( true );
        }

        if( value == "0" || string.Equals( value, "false", StringComparison.OrdinalIgnoreCase ) )
        {
            return Boolean( false );
        }

        throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
    }

    /// <summary>Parses a signed integer of the given fixed width, widened to <see cref="long"/>.</summary>
    /// <typeparam name="T">The fixed-width signed integer type whose range the value must fit.</typeparam>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="type">The declared property type, for error reporting.</param>
    /// <returns>The parsed signed-integer value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a valid integer or does not fit the type's range
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseSignedInteger< T >( string value, XISFPropertyType type ) where T : struct, INumberBase< T >
    {
        if( T.TryParse( value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out T parsed ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        return Integer( long.CreateChecked( parsed ) );
    }

    /// <summary>Parses an unsigned integer of the given fixed width, widened to <see cref="ulong"/>.</summary>
    /// <typeparam name="T">The fixed-width unsigned integer type whose range the value must fit.</typeparam>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="type">The declared property type, for error reporting.</param>
    /// <returns>The parsed unsigned-integer value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a valid unsigned integer or does not fit the type's range
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseUnsignedInteger< T >( string value, XISFPropertyType type ) where T : struct, INumberBase< T >
    {
        if( T.TryParse( value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out T parsed ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        return UnsignedInteger( ulong.CreateChecked( parsed ) );
    }

    /// <summary>Parses a floating-point value.</summary>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="type">The declared property type, for error reporting.</param>
    /// <returns>The parsed floating-point value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a valid floating-point literal
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseFloat( string value, XISFPropertyType type )
    {
        if( double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        return Float( parsed );
    }

    /// <summary>Parses a complex value of the form <c>(real,imaginary)</c>.</summary>
    /// <remarks>The value is split on its first comma; each component is trimmed and parsed invariantly.</remarks>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="type">The declared property type, for error reporting.</param>
    /// <returns>The parsed complex value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a valid <c>(real,imaginary)</c> literal
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseComplex( string value, XISFPropertyType type )
    {
        if( value.StartsWith( '(' ) == false || value.EndsWith( ')' ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        string inner = value[ 1..^1 ];
        int    comma = inner.IndexOf( ',' );

        if( comma < 0 )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        string realText      = inner[ ..comma ].Trim();
        string imaginaryText = inner[ ( comma + 1 ).. ].Trim();

        if( double.TryParse( realText, NumberStyles.Float, CultureInfo.InvariantCulture, out double real ) == false
            || double.TryParse( imaginaryText, NumberStyles.Float, CultureInfo.InvariantCulture, out double imaginary ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        return Complex( real, imaginary );
    }

    /// <summary>Parses a time-point value as an ISO 8601 internet date/time.</summary>
    /// <remarks>
    /// The XISF specification leaves the representation of time points
    /// implementation-defined; PixInsight emits ISO 8601, so both the
    /// fractional-seconds and whole-second internet date-time forms are accepted. A
    /// zone designator (<c>Z</c> or a numeric offset) is required; a zone-less
    /// string is rejected, so the parsed instant is deterministic and independent of
    /// the host time zone.
    /// </remarks>
    /// <param name="value">The trimmed value string.</param>
    /// <param name="type">The declared property type, for error reporting.</param>
    /// <returns>The parsed time-point value.</returns>
    /// <exception cref="XISFException">
    /// The string is not a recognized ISO 8601 date/time
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static XISFValue ParseTimePoint( string value, XISFPropertyType type )
    {
        if( DateTimeOffset.TryParseExact( value, TimePointFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid { type.SpecToken() } value: '{ value }'" );
        }

        return TimePoint( parsed );
    }
}
