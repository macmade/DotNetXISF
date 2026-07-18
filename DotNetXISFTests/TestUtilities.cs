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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DotNetXISF;

namespace DotNetXISFTests;

/// <summary>
/// Shared fixtures and monolithic-file construction helpers for the test suite,
/// together with the self-tests that verify those helpers.
/// </summary>
/// <remarks>
/// Synthetic files are produced as <see cref="ReadOnlyMemory{ Byte }"/> - the
/// model's data type - laid out exactly as a real monolithic XISF file: a 16-byte
/// binary preamble, the UTF-8 XML header, then any attached data-block bytes.
/// </remarks>
public class TestUtilities
{
    /// <summary>
    /// The default XML header used by the synthetic monolithic-file builder: a
    /// minimal, well-formed and namespaced <c>xisf</c> root with no children.
    /// </summary>
    public const string DefaultHeaderXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><xisf version=\"1.0\" xmlns=\"http://www.pixinsight.com/xisf\"/>";

    /// <summary>
    /// The sample <c>.xisf</c> files used as parsing fixtures.
    /// </summary>
    /// <remarks>
    /// The heavy fixtures live in the <c>Test Files</c> directory at the repository
    /// root, outside any project, so they are not copied to the build output. They
    /// are located relative to this source file's compile-time path (captured via
    /// <see cref="CallerFilePathAttribute"/>): a test assembly only ever runs from
    /// its own checkout, so the path stays valid at run time. Returned sorted by
    /// file name.
    /// </remarks>
    public static IReadOnlyList< string > TestFiles => ResolveTestFiles();

    /// <summary>
    /// Resolves <see cref="TestFiles"/> relative to this source file's location.
    /// </summary>
    /// <remarks>
    /// This file sits at the test-project root, one directory below the repository
    /// root, so two <see cref="Path.GetDirectoryName(string)"/> steps reach the
    /// repository root and the sibling <c>Test Files</c> directory.
    /// </remarks>
    /// <param name="sourceFilePath">
    /// The compile-time path of this source file, supplied automatically by
    /// <see cref="CallerFilePathAttribute"/>; it is not passed explicitly.
    /// </param>
    /// <returns>The sample files, sorted by file name; empty when none are found.</returns>
    private static IReadOnlyList< string > ResolveTestFiles( [ CallerFilePath ] string sourceFilePath = "" )
    {
        string? testsDirectory = Path.GetDirectoryName( sourceFilePath );
        string? repositoryRoot = testsDirectory is null ? null : Path.GetDirectoryName( testsDirectory );

        if( repositoryRoot is null )
        {
            return [];
        }

        string root = Path.Combine( repositoryRoot, "Test Files" );

        if( Directory.Exists( root ) == false )
        {
            return [];
        }

        return Directory.EnumerateFiles( root, "*", SearchOption.AllDirectories )
            .Where( path => Path.GetExtension( path ) == ".xisf" )
            .OrderBy( path => Path.GetFileName( path ), StringComparer.Ordinal )
            .ToList();
    }

    /// <summary>
    /// Builds a synthetic monolithic-file byte stream.
    /// </summary>
    /// <remarks>
    /// Assembles the 16-byte binary preamble - the signature, the little-endian
    /// <c>UInt32</c> header-length field, and the reserved field - followed by the
    /// UTF-8 XML header and any attached data-block bytes, exactly as a real
    /// monolithic XISF file is laid out. Each preamble field is overridable so
    /// rejection paths can be exercised.
    /// </remarks>
    /// <param name="signature">The signature to write (overridable to test rejection).</param>
    /// <param name="headerLength">
    /// The header-length field to write; when <c>null</c>, the actual UTF-8 byte
    /// count of <paramref name="xml"/> is used (overridable to test rejection).
    /// </param>
    /// <param name="reserved">The reserved preamble field to write (overridable to test rejection).</param>
    /// <param name="xml">The XML header text; when <c>null</c>, <see cref="DefaultHeaderXml"/> is used.</param>
    /// <param name="attachment">
    /// The bytes to append after the header, at file offsets that
    /// <c>attachment(position, size)</c> locations can reference; when <c>null</c>,
    /// nothing is appended.
    /// </param>
    /// <returns>The assembled monolithic-file bytes.</returns>
    public static ReadOnlyMemory< byte > MonolithicFile
    (
        string                  signature    = XISFFile.Signature,
        uint?                   headerLength = null,
        uint                    reserved     = 0,
        string?                 xml          = null,
        ReadOnlyMemory< byte >? attachment   = null
    )
    {
        byte[] xmlData       = Encoding.UTF8.GetBytes( xml ?? DefaultHeaderXml );
        uint   length        = headerLength ?? ( uint )xmlData.Length;
        byte[] lengthBytes   = new byte[ 4 ];
        byte[] reservedBytes = new byte[ 4 ];

        BinaryPrimitives.WriteUInt32LittleEndian( lengthBytes, length );
        BinaryPrimitives.WriteUInt32LittleEndian( reservedBytes, reserved );

        List< byte > data = new List< byte >();

        data.AddRange( Encoding.UTF8.GetBytes( signature ) );
        data.AddRange( lengthBytes );
        data.AddRange( reservedBytes );
        data.AddRange( xmlData );

        if( attachment is ReadOnlyMemory< byte > payload )
        {
            data.AddRange( payload.ToArray() );
        }

        return data.ToArray();
    }

    /// <summary>
    /// Removes a temporary file created by a test, ignoring any failure.
    /// </summary>
    /// <remarks>
    /// Kept <c>internal</c> rather than <c>public</c>: a public <c>void</c> method on
    /// this test class would be mistaken for an unmarked test by the xUnit analyzer
    /// (<c>xUnit1013</c>). It is reachable from every test in the assembly.
    /// </remarks>
    /// <param name="path">The path of the temporary file to remove.</param>
    internal static void RemoveTemporaryFile( string path )
    {
        try
        {
            File.Delete( path );
        }
        catch( Exception )
        {
            // Cleanup is best-effort: a deletion failure must not mask the test's
            // own result.
        }
    }

    /// <summary>
    /// The set of sample files discovered at the repository root is non-empty.
    /// </summary>
    [ Fact ]
    public void HasTestFiles()
    {
        Assert.NotEmpty( TestUtilities.TestFiles );
    }

    /// <summary>
    /// Every discovered sample file has the <c>.xisf</c> extension.
    /// </summary>
    [ Fact ]
    public void TestFilesAreXisf()
    {
        Assert.All( TestUtilities.TestFiles, path => Assert.Equal( ".xisf", Path.GetExtension( path ) ) );
    }

    /// <summary>
    /// The builder lays out a valid preamble - signature, little-endian header
    /// length and zero reserved field - immediately followed by the XML header.
    /// </summary>
    [ Fact ]
    public void MonolithicFileHasPreambleAndHeader()
    {
        string                 xml      = "<?xml version=\"1.0\"?><xisf version=\"1.0\"/>";
        byte[]                 xmlBytes = Encoding.UTF8.GetBytes( xml );
        ReadOnlyMemory< byte > data     = TestUtilities.MonolithicFile( xml: xml );

        Assert.Equal( XISFFile.PreambleSize + xmlBytes.Length, data.Length );
        Assert.True( data.MatchesAscii( XISFFile.Signature, 0 ) );
        Assert.Equal( ( uint )xmlBytes.Length, data.LittleEndianInteger< uint >( 8 ) );
        Assert.Equal( 0u, data.LittleEndianInteger< uint >( 12 ) );
        Assert.Equal( xmlBytes, data.Slice( XISFFile.PreambleSize, xmlBytes.Length ).ToArray() );
    }

    /// <summary>
    /// The builder appends the attachment bytes after the header, at the tail of
    /// the file.
    /// </summary>
    [ Fact ]
    public void MonolithicFileAppendsAttachment()
    {
        string                 xml        = "<?xml version=\"1.0\"?><xisf version=\"1.0\"/>";
        int                    xmlLength  = Encoding.UTF8.GetByteCount( xml );
        ReadOnlyMemory< byte > attachment = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        ReadOnlyMemory< byte > data       = TestUtilities.MonolithicFile( xml: xml, attachment: attachment );

        Assert.Equal( XISFFile.PreambleSize + xmlLength + attachment.Length, data.Length );
        Assert.Equal( attachment.ToArray(), data.Slice( data.Length - attachment.Length, attachment.Length ).ToArray() );
    }
}
