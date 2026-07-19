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

using System.Collections.Generic;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Unit tests for <see cref="XISFXmlParser"/> and the <see cref="XISFElement"/>
/// tree it produces: element and attribute extraction, nested children,
/// namespace resolution to local names, and rejection of malformed or unsafe XML.
/// </summary>
public class XISFXmlParserTests
{
    /// <summary>
    /// An element, its attributes, its children and their text content are all
    /// captured.
    /// </summary>
    [ Fact ]
    public void ParsesElementTree()
    {
        XISFElement root = XISFXmlParser.Parse( "<root a=\"1\" b=\"2\"><child>hello</child><child/></root>" );

        Assert.Equal( "root", root.Name );
        Assert.Equal( "1",    root.Attributes[ "a" ] );
        Assert.Equal( "2",    root.Attributes[ "b" ] );
        Assert.Equal( 2,      root.Children.Count );
        Assert.Equal( 2,      root.ChildrenNamed( "child" ).Count );
        Assert.Equal( "hello", root.Children[ 0 ].TrimmedContent );
    }

    /// <summary>
    /// Nested elements are captured as a tree, deepest last.
    /// </summary>
    [ Fact ]
    public void ParsesNestedChildren()
    {
        XISFElement root = XISFXmlParser.Parse( "<a><b><c/></b></a>" );

        Assert.Single( root.Children );

        XISFElement b = root.Children[ 0 ];

        Assert.Single( b.Children );

        XISFElement c = b.Children[ 0 ];

        Assert.Equal( "a", root.Name );
        Assert.Equal( "b", b.Name );
        Assert.Equal( "c", c.Name );
    }

    /// <summary>
    /// A default-namespaced document resolves to local element names, with the
    /// namespace exposed separately, and its <c>xmlns</c> declaration is not
    /// surfaced as a regular attribute.
    /// </summary>
    [ Fact ]
    public void ResolvesDefaultNamespaceToLocalNames()
    {
        XISFElement root = XISFXmlParser.Parse( "<xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"><Image/></xisf>" );

        Assert.Equal( "xisf", root.Name );
        Assert.Equal( "http://www.pixinsight.com/xisf", root.NamespaceUri );
        Assert.Equal( "Image", root.Children[ 0 ].Name );

        Assert.Equal( "1.0", root.Attributes[ "version" ] );
        Assert.False( root.Attributes.ContainsKey( "xmlns" ) );
    }

    /// <summary>
    /// A prefixed-namespace document resolves to local element names, dropping the
    /// prefix, with the namespace exposed separately.
    /// </summary>
    [ Fact ]
    public void ResolvesPrefixedNamespaceToLocalNames()
    {
        XISFElement root = XISFXmlParser.Parse( "<x:xisf version=\"1.0\" xmlns:x=\"http://www.pixinsight.com/xisf\"><x:Image/></x:xisf>" );

        Assert.Equal( "xisf", root.Name );
        Assert.Equal( "http://www.pixinsight.com/xisf", root.NamespaceUri );
        Assert.Equal( "Image", root.Children[ 0 ].Name );
    }

    /// <summary>
    /// An element in no namespace reports a null namespace.
    /// </summary>
    [ Fact ]
    public void ReportsNoNamespaceWhenAbsent()
    {
        XISFElement root = XISFXmlParser.Parse( "<xisf version=\"1.0\"/>" );

        Assert.Equal( "xisf", root.Name );
        Assert.Null( root.NamespaceUri );
    }

    /// <summary>
    /// Malformed or empty XML is rejected.
    /// </summary>
    [ Fact ]
    public void RejectsMalformedXml()
    {
        Assert.Throws< XISFException >( () => XISFXmlParser.Parse( "<root><unclosed></root>" ) );
        Assert.Throws< XISFException >( () => XISFXmlParser.Parse( "not xml at all <<<" ) );
        Assert.Throws< XISFException >( () => XISFXmlParser.Parse( "" ) );
    }

    /// <summary>
    /// A document declaring a DTD is rejected, so the parser never resolves a
    /// document type definition or its entities.
    /// </summary>
    [ Fact ]
    public void RejectsDocumentTypeDefinition()
    {
        string xml = "<!DOCTYPE xisf [ <!ENTITY x \"y\"> ]><xisf version=\"1.0\"/>";

        Assert.Throws< XISFException >( () => XISFXmlParser.Parse( xml ) );
    }

    /// <summary>
    /// The <see cref="XISFElement.Children"/> and <see cref="XISFElement.Attributes"/>
    /// accessors return fresh snapshots, so mutating a returned collection (even via a
    /// downcast) does not alter the element's own state.
    /// </summary>
    [ Fact ]
    public void ChildrenAndAttributesAreSnapshots()
    {
        XISFElement root = XISFXmlParser.Parse( "<root a=\"1\"><child/></root>" );

        ( ( List< XISFElement > )root.Children ).Add( root );
        ( ( Dictionary< string, string > )root.Attributes )[ "b" ] = "2";

        Assert.Single( root.Children );
        Assert.False( root.Attributes.ContainsKey( "b" ) );
    }
}
