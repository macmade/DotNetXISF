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
/// The broad category an <see cref="XISFPropertyType"/> belongs to, which
/// determines how the property's value is represented and parsed.
/// </summary>
public enum XISFPropertyCategory
{
    /// <summary>
    /// A boolean, integer or floating-point scalar, carried in the <c>value</c>
    /// attribute.
    /// </summary>
    Scalar,

    /// <summary>
    /// A complex number, carried in the <c>value</c> attribute as
    /// <c>(real,imaginary)</c>.
    /// </summary>
    Complex,

    /// <summary>A character string, carried as the element's character content.</summary>
    String,

    /// <summary>A date/time instant, carried in the <c>value</c> attribute.</summary>
    TimePoint,

    /// <summary>A homogeneous vector (or <c>ByteArray</c>), carried in a data block.</summary>
    Vector,

    /// <summary>A homogeneous matrix, carried in a data block.</summary>
    Matrix,
}
