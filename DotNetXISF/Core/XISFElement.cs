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
using System.Linq;
using System.Text;

namespace DotNetXISF;

/// <summary>
/// A node in the parsed XML-header element tree.
/// </summary>
/// <remarks>
/// This is the lightweight, namespace-aware representation that
/// <see cref="XISFXmlParser"/> produces from the raw XML header and that the
/// higher-level model types (properties, keywords, images) are built from. It is
/// internal infrastructure and not part of the public API.
/// <para>
/// Element and attribute names are the <em>local</em> names (any namespace prefix
/// is resolved away), and the element's namespace, if any, is available separately
/// as <see cref="NamespaceUri"/>. Names are compared case-sensitively (ordinal),
/// as the XML specification requires.
/// </para>
/// </remarks>
internal sealed class XISFElement
{
    /// <summary>The mutable backing store of <see cref="Children"/>.</summary>
    private readonly List< XISFElement > children = new List< XISFElement >();

    /// <summary>The accumulated character content, appended piece by piece.</summary>
    private readonly StringBuilder content = new StringBuilder();

    /// <summary>The backing store of <see cref="Attributes"/>, keyed by local name (ordinal).</summary>
    private readonly IReadOnlyDictionary< string, string > attributes;

    /// <summary>The element's local name, with any namespace prefix resolved away.</summary>
    public string Name { get; }

    /// <summary>
    /// The URI of the element's namespace, or <c>null</c> if the element is in no
    /// namespace.
    /// </summary>
    public string? NamespaceUri { get; }

    /// <summary>The element's attributes, keyed by local name (ordinal comparison).</summary>
    /// <remarks>
    /// Each read returns a fresh snapshot, so the element's internal store can never
    /// be mutated through the returned dictionary.
    /// </remarks>
    public IReadOnlyDictionary< string, string > Attributes => new Dictionary< string, string >( this.attributes, StringComparer.Ordinal );

    /// <summary>The element's child elements, in document order.</summary>
    /// <remarks>
    /// Each read returns a fresh snapshot, so the element's internal store can never
    /// be mutated through the returned list.
    /// </remarks>
    public IReadOnlyList< XISFElement > Children => this.children.ToList();

    /// <summary>
    /// The element's accumulated character content.
    /// </summary>
    /// <remarks>
    /// Character data may be reported in several pieces while parsing; each piece is
    /// appended verbatim. Use <see cref="TrimmedContent"/> for the whitespace-trimmed
    /// value.
    /// </remarks>
    public string Content => this.content.ToString();

    /// <summary>
    /// The element's character content with leading and trailing whitespace and
    /// newlines removed.
    /// </summary>
    public string TrimmedContent => this.Content.Trim();

    /// <summary>Creates an element node.</summary>
    /// <param name="name">The element's local name.</param>
    /// <param name="namespaceUri">The element's namespace URI, or <c>null</c> for none.</param>
    /// <param name="attributes">The element's attributes, keyed by local name.</param>
    public XISFElement( string name, string? namespaceUri, IReadOnlyDictionary< string, string > attributes )
    {
        this.Name         = name;
        this.NamespaceUri = namespaceUri;
        this.attributes   = attributes;
    }

    /// <summary>Appends a child element, in document order.</summary>
    /// <param name="child">The child element to append.</param>
    public void AppendChild( XISFElement child )
    {
        this.children.Add( child );
    }

    /// <summary>Appends a piece of character content.</summary>
    /// <param name="text">The character data to append verbatim.</param>
    public void AppendContent( string text )
    {
        this.content.Append( text );
    }

    /// <summary>Returns the direct child elements with a given local name.</summary>
    /// <param name="name">The local name to match, compared case-sensitively.</param>
    /// <returns>The matching direct children, in document order.</returns>
    public IReadOnlyList< XISFElement > ChildrenNamed( string name )
    {
        return this.children.Where( child => child.Name == name ).ToList();
    }
}
