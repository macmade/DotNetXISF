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
using System.Globalization;
using System.Linq;

namespace DotNetXISF;

/// <summary>
/// A parsed XISF <c>&lt;Image&gt;</c> element: its typed geometry and format metadata,
/// nested properties and FITS keywords, and its pixel data as opaque bytes.
/// </summary>
/// <remarks>
/// <para>
/// Following the opaque-bytes design, pixel samples are exposed as the fully decoded
/// (decompressed and un-shuffled) <see cref="Data"/> plus the metadata needed to interpret
/// them: <see cref="Geometry"/>, <see cref="SampleFormat"/>, <see cref="ColorSpace"/>,
/// <see cref="PixelStorage"/>, <see cref="ByteOrder"/> and <see cref="Bounds"/>.
/// Interpretation is left to the consumer.
/// </para>
/// <para>
/// The pixel data is decoded lazily on first access to <see cref="Data"/> (via the backing
/// data block), so this is a reference type and, like the data block, not thread-safe.
/// </para>
/// </remarks>
public sealed class XISFImage
{
    /// <summary>The image geometry: spatial dimensions and channel count.</summary>
    public XISFGeometry Geometry { get; }

    /// <summary>The pixel sample format.</summary>
    public XISFSampleFormat SampleFormat { get; }

    /// <summary>The color space (defaults to <see cref="XISFColorSpace.Gray"/> when unspecified).</summary>
    public XISFColorSpace ColorSpace { get; }

    /// <summary>The pixel storage model (defaults to <see cref="XISFPixelStorage.Planar"/> when unspecified).</summary>
    public XISFPixelStorage PixelStorage { get; }

    /// <summary>The byte order of multi-byte samples (defaults to little-endian).</summary>
    public XISFByteOrder ByteOrder { get; }

    /// <summary>
    /// The representable sample range (<c>Low</c>..<c>High</c>), required for floating-point
    /// formats and <c>null</c> otherwise (integers have an implicit range; complex is undefined).
    /// </summary>
    public ( double Low, double High )? Bounds { get; }

    /// <summary>The optional image type (for example <c>Light</c>, <c>Bias</c>, <c>Dark</c>).</summary>
    public string? ImageType { get; }

    /// <summary>The optional image orientation.</summary>
    public string? Orientation { get; }

    /// <summary>The optional image identifier.</summary>
    public string? Id { get; }

    /// <summary>The optional image UUID.</summary>
    public string? Uuid { get; }

    /// <summary>The parsed nested properties.</summary>
    private XISFProperty[] StoredProperties { get; }

    /// <summary>The parsed nested FITS keywords.</summary>
    private XISFFitsKeyword[] StoredKeywords { get; }

    /// <summary>The image's nested properties, in document order.</summary>
    /// <remarks>Each read returns a fresh snapshot so the internal state cannot be mutated.</remarks>
    public IReadOnlyList< XISFProperty > Properties => this.StoredProperties.ToArray();

    /// <summary>The image's nested FITS keywords, in document order.</summary>
    /// <remarks>Each read returns a fresh snapshot so the internal state cannot be mutated.</remarks>
    public IReadOnlyList< XISFFitsKeyword > Keywords => this.StoredKeywords.ToArray();

    /// <summary>The image's embedded ICC color profile, or <c>null</c> if none is associated.</summary>
    public XISFIccProfile? IccProfile { get; }

    /// <summary>
    /// The image's RGB working space, or <c>null</c> if none is associated (the default working
    /// space is then sRGB).
    /// </summary>
    public XISFRgbWorkingSpace? RgbWorkingSpace { get; }

    /// <summary>
    /// The image's display function, or <c>null</c> if none is associated (the default is then
    /// the identity function).
    /// </summary>
    public XISFDisplayFunction? DisplayFunction { get; }

    /// <summary>The image's color filter array, or <c>null</c> if the image is not mosaiced.</summary>
    public XISFColorFilterArray? ColorFilterArray { get; }

    /// <summary>
    /// The image's display resolution, or <c>null</c> if none is associated (the default is then
    /// 72 pixels per inch).
    /// </summary>
    public XISFResolution? Resolution { get; }

    /// <summary>The image's thumbnail, or <c>null</c> if none is associated.</summary>
    public XISFThumbnail? Thumbnail { get; }

    /// <summary>The backing pixel data block.</summary>
    private XISFDataBlock DataBlock { get; }

    /// <summary>
    /// The image's pixel bytes: fully decoded (decompressed and un-shuffled), exposed opaquely.
    /// Computed lazily on first access and cached.
    /// </summary>
    /// <exception cref="XISFException">
    /// Any error raised while resolving or decoding the pixel data block (decompression failure,
    /// checksum mismatch).
    /// </exception>
    public ReadOnlyMemory< byte > Data => this.DataBlock.Data;

    /// <summary>Parses an <c>&lt;Image&gt;</c> element.</summary>
    /// <param name="element">The <c>&lt;Image&gt;</c> element.</param>
    /// <param name="fileData">The complete file bytes, used to resolve an <c>attachment</c> pixel data block by its absolute offset.</param>
    /// <param name="baseDirectory">
    /// The directory of the XISF header file, used to resolve <c>@header_dir</c> relative
    /// external data blocks; <c>null</c> when the unit was opened from raw data.
    /// </param>
    /// <param name="options">
    /// The parsing options to apply. Under strict parsing the expected pixel byte count must
    /// match the geometry and sample format, floating-point images must declare <c>bounds</c>,
    /// and unknown enumerated values are errors;
    /// <see cref="XISFParsingOptions.AllowSpecDeviations"/> relaxes these.
    /// </param>
    /// <exception cref="XISFException">
    /// A required attribute is missing or invalid, or a validation check fails
    /// (<see cref="XISFErrorKind.InvalidElement"/>); or any error raised while resolving the
    /// pixel data block.
    /// </exception>
    internal XISFImage( XISFElement element, ReadOnlyMemory< byte > fileData, string? baseDirectory, XISFParsingOptions options )
    {
        IReadOnlyDictionary< string, string > attributes = element.Attributes;

        if( attributes.TryGetValue( "geometry", out string? geometryString ) == false )
        {
            throw XISFException.InvalidElement( "Image is missing a 'geometry' attribute" );
        }

        XISFGeometry geometry = new XISFGeometry( geometryString );

        if( attributes.TryGetValue( "sampleFormat", out string? sampleFormatString ) == false || XISFSampleFormatExtensions.FromSpecToken( sampleFormatString ) is not XISFSampleFormat sampleFormat )
        {
            throw XISFException.InvalidElement( $"Image has a missing or unknown 'sampleFormat': '{ attributes.GetValueOrDefault( "sampleFormat" ) ?? "" }'" );
        }

        bool lenient = options.HasFlag( XISFParsingOptions.AllowSpecDeviations );

        ( double Low, double High )? bounds = null;

        if( attributes.TryGetValue( "bounds", out string? boundsRaw ) )
        {
            bounds = ParseBounds( boundsRaw );
        }

        XISFDataBlock dataBlock = new XISFDataBlock( element, fileData, baseDirectory, options );

        if( sampleFormat.IsFloatingPoint() && bounds is null && lenient == false )
        {
            throw XISFException.InvalidElement( $"Floating-point image of format { sampleFormat.SpecToken() } is missing the required 'bounds' attribute" );
        }

        // The uncompressed size is unknown for an uncompressed external block (it would require
        // reading the external resource), so validate only when it is known without resolving the
        // block. The expected size is computed here, where it is used, so it is not evaluated for
        // an external block whose size is unknown.
        if( dataBlock.UncompressedSize is int actualSize && lenient == false )
        {
            long expectedSize = geometry.SampleCount * sampleFormat.BytesPerSample();

            if( actualSize != expectedSize )
            {
                throw XISFException.InvalidElement( $"Image pixel data is { actualSize.ToString( CultureInfo.InvariantCulture ) } bytes but geometry { geometryString } and format { sampleFormat.SpecToken() } require { expectedSize.ToString( CultureInfo.InvariantCulture ) }" );
            }
        }

        this.Geometry     = geometry;
        this.SampleFormat = sampleFormat;
        this.ColorSpace   = EnumeratedValue( attributes, "colorSpace",   XISFColorSpaceExtensions.Default,   XISFColorSpaceExtensions.FromSpecToken,   lenient );
        this.PixelStorage = EnumeratedValue( attributes, "pixelStorage", XISFPixelStorageExtensions.Default, XISFPixelStorageExtensions.FromSpecToken, lenient );
        this.ByteOrder    = EnumeratedValue( attributes, "byteOrder",    XISFByteOrderExtensions.Default,    XISFByteOrderExtensions.FromSpecToken,    lenient );
        this.Bounds       = bounds;
        this.ImageType    = attributes.GetValueOrDefault( "imageType" );
        this.Orientation  = attributes.GetValueOrDefault( "orientation" );
        this.Id           = attributes.GetValueOrDefault( "id" );
        this.Uuid         = attributes.GetValueOrDefault( "uuid" );
        this.DataBlock    = dataBlock;

        this.StoredProperties = XISFProperty.ParseList( element, fileData, baseDirectory, options ).ToArray();
        this.StoredKeywords   = ParseKeywords( element, options );

        this.IccProfile       = OptionalReferenceChild( element, "ICCProfile",       lenient, child => new XISFIccProfile( child, fileData, baseDirectory, options ) );
        this.RgbWorkingSpace  = OptionalValueChild( element, "RGBWorkingSpace",  lenient, child => new XISFRgbWorkingSpace( child, options ) );
        this.DisplayFunction  = OptionalValueChild( element, "DisplayFunction",  lenient, child => new XISFDisplayFunction( child, options ) );
        this.ColorFilterArray = OptionalValueChild( element, "ColorFilterArray", lenient, child => new XISFColorFilterArray( child, options ) );
        this.Resolution       = OptionalValueChild( element, "Resolution",       lenient, child => new XISFResolution( child, options ) );
        this.Thumbnail        = OptionalReferenceChild( element, "Thumbnail",        lenient, child => new XISFThumbnail( child, fileData, baseDirectory, options ) );
    }

    /// <summary>A single-line, human-readable summary of the image.</summary>
    /// <returns>The formatted summary.</returns>
    public override string ToString()
    {
        string dimensions = string.Join( "x", this.Geometry.Dimensions.Select( value => value.ToString( CultureInfo.InvariantCulture ) ) );

        return $"XISFImage {{ geometry: { dimensions }:{ this.Geometry.ChannelCount.ToString( CultureInfo.InvariantCulture ) }, sampleFormat: { this.SampleFormat.SpecToken() }, colorSpace: { this.ColorSpace.SpecToken() }, pixelStorage: { this.PixelStorage.SpecToken() }, byteOrder: { this.ByteOrder.SpecToken() } }}";
    }

    /// <summary>Parses the direct-child <c>&lt;FITSKeyword&gt;</c> elements.</summary>
    /// <remarks>
    /// Unlike the properties, a malformed keyword is always fatal (the name-length and charset
    /// checks are themselves relaxed under lenient parsing), matching the source.
    /// </remarks>
    /// <param name="element">The <c>&lt;Image&gt;</c> element.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>The parsed keywords, in document order.</returns>
    /// <exception cref="XISFException">A keyword fails to parse (<see cref="XISFErrorKind.InvalidElement"/>).</exception>
    private static XISFFitsKeyword[] ParseKeywords( XISFElement element, XISFParsingOptions options )
    {
        IReadOnlyList< XISFElement > children = element.ChildrenNamed( "FITSKeyword" );
        List< XISFFitsKeyword >      keywords = new List< XISFFitsKeyword >( children.Count );

        foreach( XISFElement child in children )
        {
            keywords.Add( new XISFFitsKeyword( child, options ) );
        }

        return keywords.ToArray();
    }

    /// <summary>Parses a <c>bounds</c> attribute of the form <c>low:high</c>.</summary>
    /// <param name="raw">The raw <c>bounds</c> attribute value.</param>
    /// <returns>The parsed low/high range.</returns>
    /// <exception cref="XISFException">
    /// The attribute is not two <c>low:high</c> reals with <c>low &lt;= high</c>
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static ( double Low, double High ) ParseBounds( string raw )
    {
        string[] parts = raw.Split( ':' );

        // Reject with the source's positive predicate: a valid range requires low <= high. This
        // is written as its negation (rather than low > high) so that a NaN component — for which
        // neither comparison holds — is rejected, matching the source.
        if( parts.Length != 2
            || double.TryParse( parts[ 0 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double low ) == false
            || double.TryParse( parts[ 1 ], NumberStyles.Float, CultureInfo.InvariantCulture, out double high ) == false
            || ( low <= high ) == false )
        {
            throw XISFException.InvalidElement( $"Invalid bounds attribute: '{ raw }'" );
        }

        return ( low, high );
    }

    /// <summary>Reads a token-valued enumerated attribute, applying a default when absent.</summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="attributes">The element's attributes.</param>
    /// <param name="name">The attribute name.</param>
    /// <param name="defaultValue">The value to use when the attribute is absent.</param>
    /// <param name="fromToken">The token parser for <typeparamref name="T"/>.</param>
    /// <param name="lenient">
    /// Whether an unknown value falls back to the default instead of being an error.
    /// </param>
    /// <returns>The parsed value, or the default.</returns>
    /// <exception cref="XISFException">
    /// The value is present but unknown and strict parsing is in effect
    /// (<see cref="XISFErrorKind.InvalidElement"/>).
    /// </exception>
    private static T EnumeratedValue< T >( IReadOnlyDictionary< string, string > attributes, string name, T defaultValue, Func< string, T? > fromToken, bool lenient )
        where T : struct
    {
        if( attributes.TryGetValue( name, out string? raw ) == false )
        {
            return defaultValue;
        }

        if( fromToken( raw ) is T value )
        {
            return value;
        }

        if( lenient )
        {
            return defaultValue;
        }

        throw XISFException.InvalidElement( $"Invalid '{ name }' attribute: '{ raw }'" );
    }

    /// <summary>Returns the first child element with the given name, or <c>null</c>.</summary>
    /// <param name="element">The element whose children to search.</param>
    /// <param name="name">The local name of the child to find.</param>
    /// <returns>The first matching child, or <c>null</c>.</returns>
    private static XISFElement? FirstChild( XISFElement element, string name )
    {
        IReadOnlyList< XISFElement > children = element.ChildrenNamed( name );

        return children.Count == 0 ? null : children[ 0 ];
    }

    /// <summary>Parses an optional value-typed child metadata element, tolerating a malformed one under lenient parsing.</summary>
    /// <typeparam name="T">The value type to parse.</typeparam>
    /// <param name="element">The element whose children to search.</param>
    /// <param name="name">The local name of the child element to parse.</param>
    /// <param name="lenient">Whether a child that fails to parse is dropped instead of propagating the error.</param>
    /// <param name="parse">The function that parses the first matching child element.</param>
    /// <returns>The parsed value, <c>null</c> if no matching child exists, or <c>null</c> if parsing failed under lenient parsing.</returns>
    /// <exception cref="XISFException">Any error raised by <paramref name="parse"/> under strict parsing.</exception>
    private static T? OptionalValueChild< T >( XISFElement element, string name, bool lenient, Func< XISFElement, T > parse )
        where T : struct
    {
        if( FirstChild( element, name ) is not XISFElement child )
        {
            return null;
        }

        try
        {
            return parse( child );
        }
        catch( XISFException )
        {
            if( lenient )
            {
                return null;
            }

            throw;
        }
    }

    /// <summary>Parses an optional reference-typed child metadata element, tolerating a malformed one under lenient parsing.</summary>
    /// <typeparam name="T">The reference type to parse.</typeparam>
    /// <param name="element">The element whose children to search.</param>
    /// <param name="name">The local name of the child element to parse.</param>
    /// <param name="lenient">Whether a child that fails to parse is dropped instead of propagating the error.</param>
    /// <param name="parse">The function that parses the first matching child element.</param>
    /// <returns>The parsed value, <c>null</c> if no matching child exists, or <c>null</c> if parsing failed under lenient parsing.</returns>
    /// <exception cref="XISFException">Any error raised by <paramref name="parse"/> under strict parsing.</exception>
    private static T? OptionalReferenceChild< T >( XISFElement element, string name, bool lenient, Func< XISFElement, T > parse )
        where T : class
    {
        if( FirstChild( element, name ) is not XISFElement child )
        {
            return null;
        }

        try
        {
            return parse( child );
        }
        catch( XISFException )
        {
            if( lenient )
            {
                return null;
            }

            throw;
        }
    }
}
