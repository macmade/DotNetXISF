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
using System.IO;
using System.Xml;

namespace DotNetXISF;

/// <summary>
/// Builds an <see cref="XISFElement"/> tree from the raw XML header text.
/// </summary>
/// <remarks>
/// A thin, namespace-aware wrapper over <see cref="XmlReader"/>. Namespace
/// processing is enabled, so prefixed and default-namespaced documents both
/// resolve to local element names with the namespace exposed separately, as the
/// XML specification requires. The reader is safe by construction: document type
/// definitions are prohibited (<see cref="DtdProcessing.Prohibit"/>) and no
/// <see cref="System.Xml.XmlResolver"/> is provided, so the parser never resolves
/// a DTD or reads any external entity.
/// <para>
/// This is internal infrastructure and not part of the public API.
/// </para>
/// </remarks>
internal static class XISFXmlParser
{
    /// <summary>The namespace URI under which XML reports its <c>xmlns</c> declarations.</summary>
    private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    /// <summary>Parses XML header text into an element tree.</summary>
    /// <param name="xml">The XML header text.</param>
    /// <returns>The root element of the parsed tree.</returns>
    /// <exception cref="XISFException">
    /// The text is not well-formed XML, declares a DTD, or contains no root element
    /// (<see cref="XISFErrorKind.MalformedXml"/>).
    /// </exception>
    public static XISFElement Parse( string xml )
    {
        XmlReaderSettings settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver   = null,
        };

        XISFElement?          root  = null;
        Stack< XISFElement >  stack = new Stack< XISFElement >();

        try
        {
            using StringReader stringReader = new StringReader( xml );
            using XmlReader    reader       = XmlReader.Create( stringReader, settings );

            while( reader.Read() )
            {
                switch( reader.NodeType )
                {
                    case XmlNodeType.Element:

                        XISFElement element = ReadElement( reader );

                        if( stack.Count > 0 )
                        {
                            stack.Peek().AppendChild( element );
                        }
                        else if( root == null )
                        {
                            root = element;
                        }

                        if( reader.IsEmptyElement == false )
                        {
                            stack.Push( element );
                        }

                        break;

                    case XmlNodeType.EndElement:

                        stack.Pop();

                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:

                        if( stack.Count > 0 )
                        {
                            stack.Peek().AppendContent( reader.Value );
                        }

                        break;

                    default:

                        break;
                }
            }
        }
        catch( XmlException exception )
        {
            throw XISFException.MalformedXml( exception.Message );
        }

        if( root == null )
        {
            throw XISFException.MalformedXml( "The XML header contains no root element" );
        }

        return root;
    }

    /// <summary>
    /// Reads the element node the reader is currently positioned on into an
    /// <see cref="XISFElement"/>, capturing its local name, namespace and attributes.
    /// </summary>
    /// <remarks>
    /// The element's <c>xmlns</c> declarations are namespace machinery, not content,
    /// so they are skipped rather than surfaced as attributes.
    /// </remarks>
    /// <param name="reader">The reader, positioned on an element node.</param>
    /// <returns>The element node, without its children or content.</returns>
    private static XISFElement ReadElement( XmlReader reader )
    {
        string  name         = reader.LocalName;
        string? namespaceUri = string.IsNullOrEmpty( reader.NamespaceURI ) ? null : reader.NamespaceURI;

        Dictionary< string, string > attributes = new Dictionary< string, string >( StringComparer.Ordinal );

        if( reader.MoveToFirstAttribute() )
        {
            do
            {
                if( string.Equals( reader.NamespaceURI, XmlnsNamespace, StringComparison.Ordinal ) )
                {
                    continue;
                }

                attributes[ reader.LocalName ] = reader.Value;
            }
            while( reader.MoveToNextAttribute() );

            reader.MoveToElement();
        }

        return new XISFElement( name, namespaceUri, attributes );
    }
}
