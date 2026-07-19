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
/// The declared type of an XISF property, as carried by a <c>&lt;Property&gt;</c>
/// element's <c>type</c> attribute.
/// </summary>
/// <remarks>
/// XISF 1.0 defines scalar types (<c>Boolean</c>, the signed and unsigned
/// integers, the floats), <c>Complex32</c>/<c>Complex64</c>, <c>String</c>,
/// <c>TimePoint</c>, the homogeneous vector and matrix families, and
/// <c>ByteArray</c>. There are deliberately no 128-bit complex or 128-bit scalar
/// types: XISF 1.0 stops at <c>Complex64</c> / <c>C64*</c>. Each member's name is
/// its exact spec token; parse with
/// <see cref="XISFPropertyTypeExtensions.FromSpecToken(string)"/> and format with
/// <see cref="XISFPropertyTypeExtensions.SpecToken(XISFPropertyType)"/>.
/// </remarks>
public enum XISFPropertyType
{
    /// <summary>A boolean value.</summary>
    Boolean,

    /// <summary>An 8-bit signed integer.</summary>
    Int8,

    /// <summary>An 8-bit unsigned integer.</summary>
    UInt8,

    /// <summary>A 16-bit signed integer.</summary>
    Int16,

    /// <summary>A 16-bit unsigned integer.</summary>
    UInt16,

    /// <summary>A 32-bit signed integer.</summary>
    Int32,

    /// <summary>A 32-bit unsigned integer.</summary>
    UInt32,

    /// <summary>A 64-bit signed integer.</summary>
    Int64,

    /// <summary>A 64-bit unsigned integer.</summary>
    UInt64,

    /// <summary>A 32-bit (single-precision) floating-point value.</summary>
    Float32,

    /// <summary>A 64-bit (double-precision) floating-point value.</summary>
    Float64,

    /// <summary>A complex number with two 32-bit floating-point components.</summary>
    Complex32,

    /// <summary>A complex number with two 64-bit floating-point components.</summary>
    Complex64,

    /// <summary>A character string.</summary>
    String,

    /// <summary>A date/time instant.</summary>
    TimePoint,

    /// <summary>A vector of bytes (equivalent to a <c>UI8Vector</c>).</summary>
    ByteArray,

    /// <summary>A vector of 8-bit signed integers.</summary>
    I8Vector,

    /// <summary>A vector of 8-bit unsigned integers.</summary>
    UI8Vector,

    /// <summary>A vector of 16-bit signed integers.</summary>
    I16Vector,

    /// <summary>A vector of 16-bit unsigned integers.</summary>
    UI16Vector,

    /// <summary>A vector of 32-bit signed integers.</summary>
    I32Vector,

    /// <summary>A vector of 32-bit unsigned integers.</summary>
    UI32Vector,

    /// <summary>A vector of 64-bit signed integers.</summary>
    I64Vector,

    /// <summary>A vector of 64-bit unsigned integers.</summary>
    UI64Vector,

    /// <summary>A vector of 32-bit floating-point values.</summary>
    F32Vector,

    /// <summary>A vector of 64-bit floating-point values.</summary>
    F64Vector,

    /// <summary>A vector of 32-bit-component complex numbers.</summary>
    C32Vector,

    /// <summary>A vector of 64-bit-component complex numbers.</summary>
    C64Vector,

    /// <summary>A matrix of 8-bit signed integers.</summary>
    I8Matrix,

    /// <summary>A matrix of 8-bit unsigned integers.</summary>
    UI8Matrix,

    /// <summary>A matrix of 16-bit signed integers.</summary>
    I16Matrix,

    /// <summary>A matrix of 16-bit unsigned integers.</summary>
    UI16Matrix,

    /// <summary>A matrix of 32-bit signed integers.</summary>
    I32Matrix,

    /// <summary>A matrix of 32-bit unsigned integers.</summary>
    UI32Matrix,

    /// <summary>A matrix of 64-bit signed integers.</summary>
    I64Matrix,

    /// <summary>A matrix of 64-bit unsigned integers.</summary>
    UI64Matrix,

    /// <summary>A matrix of 32-bit floating-point values.</summary>
    F32Matrix,

    /// <summary>A matrix of 64-bit floating-point values.</summary>
    F64Matrix,

    /// <summary>A matrix of 32-bit-component complex numbers.</summary>
    C32Matrix,

    /// <summary>A matrix of 64-bit-component complex numbers.</summary>
    C64Matrix,
}

/// <summary>
/// Spec-token parsing/formatting and category classification for
/// <see cref="XISFPropertyType"/>.
/// </summary>
/// <remarks>
/// These are the enum's behavior, which a C# enum cannot carry directly; they are
/// grouped with the enum as a single logical unit.
/// </remarks>
public static class XISFPropertyTypeExtensions
{
    /// <summary>
    /// Parses a property-type spec token to its <see cref="XISFPropertyType"/>.
    /// </summary>
    /// <remarks>
    /// The comparison is exact (case-sensitive, ordinal); a numeric string or a
    /// wrong-case token does not parse.
    /// </remarks>
    /// <param name="token">The spec type string (for example <c>UI8Vector</c>).</param>
    /// <returns>
    /// The matching property type, or <c>null</c> when the token is not a known
    /// XISF 1.0 type.
    /// </returns>
    public static XISFPropertyType? FromSpecToken( string token )
    {
        foreach( XISFPropertyType value in Enum.GetValues< XISFPropertyType >() )
        {
            if( string.Equals( value.SpecToken(), token, StringComparison.Ordinal ) )
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// The spec token for a property type (the string carried in the <c>type</c>
    /// attribute).
    /// </summary>
    /// <param name="value">The property type.</param>
    /// <returns>The spec token, which is the member's name.</returns>
    public static string SpecToken( this XISFPropertyType value ) => value.ToString();

    /// <summary>
    /// The category a property type belongs to, determining how its value is
    /// represented and parsed.
    /// </summary>
    /// <param name="value">The property type.</param>
    /// <returns>The property category.</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined property type
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static XISFPropertyCategory Category( this XISFPropertyType value )
    {
        return value switch
        {
            XISFPropertyType.Boolean
                or XISFPropertyType.Int8 or XISFPropertyType.UInt8
                or XISFPropertyType.Int16 or XISFPropertyType.UInt16
                or XISFPropertyType.Int32 or XISFPropertyType.UInt32
                or XISFPropertyType.Int64 or XISFPropertyType.UInt64
                or XISFPropertyType.Float32 or XISFPropertyType.Float64 => XISFPropertyCategory.Scalar,

            XISFPropertyType.Complex32 or XISFPropertyType.Complex64 => XISFPropertyCategory.Complex,

            XISFPropertyType.String => XISFPropertyCategory.String,

            XISFPropertyType.TimePoint => XISFPropertyCategory.TimePoint,

            XISFPropertyType.ByteArray
                or XISFPropertyType.I8Vector or XISFPropertyType.UI8Vector
                or XISFPropertyType.I16Vector or XISFPropertyType.UI16Vector
                or XISFPropertyType.I32Vector or XISFPropertyType.UI32Vector
                or XISFPropertyType.I64Vector or XISFPropertyType.UI64Vector
                or XISFPropertyType.F32Vector or XISFPropertyType.F64Vector
                or XISFPropertyType.C32Vector or XISFPropertyType.C64Vector => XISFPropertyCategory.Vector,

            XISFPropertyType.I8Matrix or XISFPropertyType.UI8Matrix
                or XISFPropertyType.I16Matrix or XISFPropertyType.UI16Matrix
                or XISFPropertyType.I32Matrix or XISFPropertyType.UI32Matrix
                or XISFPropertyType.I64Matrix or XISFPropertyType.UI64Matrix
                or XISFPropertyType.F32Matrix or XISFPropertyType.F64Matrix
                or XISFPropertyType.C32Matrix or XISFPropertyType.C64Matrix => XISFPropertyCategory.Matrix,

            _ => throw XISFException.InvalidElement( "Unknown property type" ),
        };
    }
}
