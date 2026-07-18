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
/// XISF-identifier character-membership predicates used during parsing.
/// </summary>
/// <remarks>
/// An XISF identifier matches the grammar <c>[_a-zA-Z][_a-zA-Z0-9]*</c>. The two
/// predicates express the allowed start characters (a letter or the underscore)
/// and the allowed body characters (the start set plus the digits), each as a
/// <see cref="char"/> membership test (.NET has no character-set type). Kept
/// <c>internal</c> as a parser-only helper.
/// </remarks>
internal static class XISFCharacterSet
{
    /// <summary>
    /// The characters allowed as the first character of an XISF identifier: the
    /// ASCII letters and the underscore.
    /// </summary>
    private const string IdentifierStartCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_";

    /// <summary>
    /// The characters allowed after the first character of an XISF identifier: the
    /// start characters extended with the ASCII digits.
    /// </summary>
    private const string IdentifierBodyCharacters = IdentifierStartCharacters + "0123456789";

    /// <summary>
    /// Returns whether <paramref name="character"/> may start an XISF identifier.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if it is an ASCII letter or the underscore.</returns>
    internal static bool IsIdentifierStart( char character ) => IdentifierStartCharacters.Contains( character );

    /// <summary>
    /// Returns whether <paramref name="character"/> may appear after the first
    /// character of an XISF identifier.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if it is an ASCII letter, digit or the underscore.</returns>
    internal static bool IsIdentifierBody( char character ) => IdentifierBodyCharacters.Contains( character );
}
