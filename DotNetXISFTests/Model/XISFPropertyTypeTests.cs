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
/// Unit tests for <see cref="XISFPropertyType"/> and its spec-token parsing and
/// category classification.
/// </summary>
public class XISFPropertyTypeTests
{
    /// <summary>
    /// The scalar, complex, string and time-point spec tokens parse to their
    /// property types.
    /// </summary>
    [ Fact ]
    public void ScalarSpecTokens()
    {
        Assert.Equal( XISFPropertyType.Boolean,   XISFPropertyTypeExtensions.FromSpecToken( "Boolean" ) );
        Assert.Equal( XISFPropertyType.Int8,      XISFPropertyTypeExtensions.FromSpecToken( "Int8" ) );
        Assert.Equal( XISFPropertyType.UInt8,     XISFPropertyTypeExtensions.FromSpecToken( "UInt8" ) );
        Assert.Equal( XISFPropertyType.Int64,     XISFPropertyTypeExtensions.FromSpecToken( "Int64" ) );
        Assert.Equal( XISFPropertyType.UInt64,    XISFPropertyTypeExtensions.FromSpecToken( "UInt64" ) );
        Assert.Equal( XISFPropertyType.Float32,   XISFPropertyTypeExtensions.FromSpecToken( "Float32" ) );
        Assert.Equal( XISFPropertyType.Float64,   XISFPropertyTypeExtensions.FromSpecToken( "Float64" ) );
        Assert.Equal( XISFPropertyType.Complex32, XISFPropertyTypeExtensions.FromSpecToken( "Complex32" ) );
        Assert.Equal( XISFPropertyType.Complex64, XISFPropertyTypeExtensions.FromSpecToken( "Complex64" ) );
        Assert.Equal( XISFPropertyType.String,    XISFPropertyTypeExtensions.FromSpecToken( "String" ) );
        Assert.Equal( XISFPropertyType.TimePoint, XISFPropertyTypeExtensions.FromSpecToken( "TimePoint" ) );
    }

    /// <summary>
    /// The vector, matrix and byte-array spec tokens parse to their property
    /// types.
    /// </summary>
    [ Fact ]
    public void VectorAndMatrixSpecTokens()
    {
        Assert.Equal( XISFPropertyType.ByteArray,  XISFPropertyTypeExtensions.FromSpecToken( "ByteArray" ) );
        Assert.Equal( XISFPropertyType.I8Vector,   XISFPropertyTypeExtensions.FromSpecToken( "I8Vector" ) );
        Assert.Equal( XISFPropertyType.UI8Vector,  XISFPropertyTypeExtensions.FromSpecToken( "UI8Vector" ) );
        Assert.Equal( XISFPropertyType.UI16Vector, XISFPropertyTypeExtensions.FromSpecToken( "UI16Vector" ) );
        Assert.Equal( XISFPropertyType.F64Vector,  XISFPropertyTypeExtensions.FromSpecToken( "F64Vector" ) );
        Assert.Equal( XISFPropertyType.C64Vector,  XISFPropertyTypeExtensions.FromSpecToken( "C64Vector" ) );
        Assert.Equal( XISFPropertyType.I32Matrix,  XISFPropertyTypeExtensions.FromSpecToken( "I32Matrix" ) );
        Assert.Equal( XISFPropertyType.F32Matrix,  XISFPropertyTypeExtensions.FromSpecToken( "F32Matrix" ) );
        Assert.Equal( XISFPropertyType.C64Matrix,  XISFPropertyTypeExtensions.FromSpecToken( "C64Matrix" ) );
    }

    /// <summary>
    /// Unknown, non-1.0, numeric and wrong-case tokens do not parse.
    /// </summary>
    [ Fact ]
    public void RejectsUnknownSpecTokens()
    {
        // Complex128 / C128 / 128-bit scalars do not exist in XISF 1.0.
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "Complex128" ) );
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "C128Vector" ) );
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "Int128" ) );
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "Float128" ) );
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "boolean" ) );
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "Nonsense" ) );

        // A numeric string must not be accepted as an enum ordinal.
        Assert.Null( XISFPropertyTypeExtensions.FromSpecToken( "1" ) );
    }

    /// <summary>
    /// Each property type reports the category that determines how its value is
    /// represented and parsed.
    /// </summary>
    [ Fact ]
    public void Category()
    {
        Assert.Equal( XISFPropertyCategory.Scalar,    XISFPropertyType.Boolean.Category() );
        Assert.Equal( XISFPropertyCategory.Scalar,    XISFPropertyType.Int32.Category() );
        Assert.Equal( XISFPropertyCategory.Scalar,    XISFPropertyType.Float64.Category() );
        Assert.Equal( XISFPropertyCategory.Complex,   XISFPropertyType.Complex32.Category() );
        Assert.Equal( XISFPropertyCategory.String,    XISFPropertyType.String.Category() );
        Assert.Equal( XISFPropertyCategory.TimePoint, XISFPropertyType.TimePoint.Category() );
        Assert.Equal( XISFPropertyCategory.Vector,    XISFPropertyType.ByteArray.Category() );
        Assert.Equal( XISFPropertyCategory.Vector,    XISFPropertyType.UI8Vector.Category() );
        Assert.Equal( XISFPropertyCategory.Matrix,    XISFPropertyType.F32Matrix.Category() );
    }
}
