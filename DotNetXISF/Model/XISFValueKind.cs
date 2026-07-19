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

namespace DotNetXISF;

/// <summary>
/// The type discriminator of an <see cref="XISFValue"/>, independent of any
/// payload.
/// </summary>
/// <remarks>
/// <see cref="Boolean"/> is the zero value, so a default-constructed
/// <see cref="XISFValue"/> is a boolean value.
/// </remarks>
public enum XISFValueKind
{
    /// <summary>The kind of a boolean value.</summary>
    Boolean,

    /// <summary>The kind of a signed-integer value.</summary>
    Integer,

    /// <summary>The kind of an unsigned-integer value.</summary>
    UnsignedInteger,

    /// <summary>The kind of a floating-point value.</summary>
    Float,

    /// <summary>The kind of a complex value.</summary>
    Complex,

    /// <summary>The kind of a character-string value.</summary>
    String,

    /// <summary>The kind of a date/time value.</summary>
    TimePoint,

    /// <summary>The kind of an opaque-bytes value.</summary>
    Data,
}

/// <summary>
/// Human-readable descriptions for <see cref="XISFValueKind"/>.
/// </summary>
/// <remarks>
/// This is the enum's readable name, which a C# enum cannot carry directly; it is
/// grouped with the enum as a single logical unit.
/// </remarks>
public static class XISFValueKindExtensions
{
    /// <summary>
    /// A human-readable name for a value kind.
    /// </summary>
    /// <param name="value">The value kind.</param>
    /// <returns>The readable name (for example <c>Unsigned Integer</c>).</returns>
    /// <exception cref="XISFException">
    /// The value is not a defined value kind
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    public static string Description( this XISFValueKind value )
    {
        return value switch
        {
            XISFValueKind.Boolean         => "Boolean",
            XISFValueKind.Integer         => "Integer",
            XISFValueKind.UnsignedInteger => "Unsigned Integer",
            XISFValueKind.Float           => "Float",
            XISFValueKind.Complex         => "Complex",
            XISFValueKind.String          => "String",
            XISFValueKind.TimePoint       => "Time Point",
            XISFValueKind.Data            => "Data",
            _                             => throw XISFException.InvalidElement( "Unknown value kind" ),
        };
    }
}
