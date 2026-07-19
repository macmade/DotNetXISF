DotNetXISF
==========

[![Build Status](https://img.shields.io/github/actions/workflow/status/macmade/DotNetXISF/ci-win.yaml?label=Windows&logo=dotnet)](https://github.com/macmade/DotNetXISF/actions/workflows/ci-win.yaml)
[![NuGet](https://img.shields.io/nuget/v/DotNetXISF.svg?logo=nuget)](https://www.nuget.org/packages/DotNetXISF)
[![Issues](http://img.shields.io/github/issues/macmade/DotNetXISF.svg?logo=github)](https://github.com/macmade/DotNetXISF/issues)
![Status](https://img.shields.io/badge/status-active-brightgreen.svg?logo=git)
![License](https://img.shields.io/badge/license-mit-brightgreen.svg?logo=open-source-initiative)  
[![Contact](https://img.shields.io/badge/follow-@macmade-blue.svg?logo=twitter&style=social)](https://twitter.com/macmade)
[![Sponsor](https://img.shields.io/badge/sponsor-macmade-pink.svg?logo=github-sponsors&style=social)](https://github.com/sponsors/macmade)

### About

XISF Image Library for C# / .NET.

This library provides a simple interface to read [XISF](https://pixinsight.com/xisf/) (Extensible
Image Serialization Format) files in C#, based on the
[XISF 1.0 specification](https://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html). XISF is
the native image format of [PixInsight](https://pixinsight.com/).

A single `XISFFile` entry point, opened from a file path or a `ReadOnlyMemory<byte>` buffer, exposes
the file's images, properties, embedded FITS keywords and metadata. Pixel data is surfaced as fully
decoded (decompressed and un-shuffled) *opaque bytes plus typed geometry and format metadata* —
interpretation of the samples is left to the consumer.

### Status

DotNetXISF is **read-only**: it parses existing monolithic XISF files into their header/data
structure. Write and serialization (XISF authoring) support is not implemented and is not currently
planned.

### Features

- **Monolithic files**: reads and validates the 16-byte binary preamble (`XISF0100` signature,
  little-endian header length, reserved field) and the UTF-8 XML header.
- **Images**: multiple images per file, each exposing typed `Geometry`, `SampleFormat`,
  `ColorSpace`, `PixelStorage`, `ByteOrder` and `Bounds`, plus its fully decoded pixel bytes.
- **Properties & FITS keywords**: typed scalar, complex, time-point and string properties, plus
  vector / matrix / `ByteArray` values backed by data blocks, and embedded FITS keywords.
- **Data blocks**: `inline` (base64 / hex), `embedded` (`<Data>` child), and `attachment` (in-file)
  locations, plus opt-in external / distributed `url(...)` / `path(...)` locations and the `.xisb`
  distributed block index.
- **Compression**: `zlib`, `lz4`, `lz4hc` and `zstd`, all with optional byte-shuffling (`+sh`) and
  split sub-blocks.
- **Checksums**: opt-in verification of `sha-1`, `sha-256` and `sha-512` (and `sha3-256` /
  `sha3-512` where the platform provides them) data-block digests.
- **Color & ancillary metadata**: unit-level `Metadata`, per-image ICC profiles, RGB working space,
  display function, color filter array, resolution and thumbnails.
- **Strict vs. lenient**: `XISFParsingOptions` toggles spec-faithful validation against real-world
  tolerance, and gates checksum verification and external-location resolution.

### Conformance & Limitations

DotNetXISF targets the base [XISF 1.0 specification](https://pixinsight.com/doc/docs/XISF-1.0-spec/XISF-1.0-spec.html).
The following properties are intentional, not latent surprises:

- **Read-only**: there is no XISF authoring or serialization API.
- **Opaque pixel data**: samples are exposed as the fully decoded raw bytes plus typed metadata; the
  library does not decode them into typed sample arrays. The consumer interprets the bytes using
  `SampleFormat`, `ByteOrder`, `Geometry` and `PixelStorage`.
- **External / distributed data blocks are opt-in**: `url(...)` / `path(...)` locations are resolved
  only when `XISFParsingOptions.AllowExternalLocations` is set (off in both `Strict` and `Lenient`),
  because resolving them reads files outside the parsed document. Resolution is lazy: a unit
  referencing external blocks still opens, and only *accessing* such a block requires the option.
  Only local `file://` URLs and both `path(...)` forms are supported — **remote (network) URLs are
  not fetched**.
- **SHA-3 checksums require platform support**: `sha3-256` / `sha3-512` verification is available
  only where the operating system provides SHA-3; where it does not, requesting it yields a clean
  "unsupported" error rather than silently passing. `sha-1` / `sha-256` / `sha-512` are always
  available.
- **`Reference` / `uid` association is not implemented**: ancillary elements (ICC profile, display
  function, and so on) are parsed only as direct children of their `<Image>` (and `Metadata` as a
  direct child of the root). Root-level elements shared across images via `<Reference>` are not
  resolved.
- **`Metadata` is treated as optional**: the specification makes the unit `<Metadata>` element
  mandatory, but DotNetXISF exposes it as an optional (`null` when absent) rather than rejecting
  files that omit it.
- **Tables are out of scope**: `Structure` / `Table` elements are not parsed.
- **Strict vs. lenient**: `Strict` verifies data-block checksums and rejects input the spec forbids
  (a non-zero reserved field, an out-of-range value, a missing `version="1.0"`, a float image
  without `bounds`, an invalid identifier, and so on), while `Lenient` tolerates common real-world
  deviations (a non-zero reserved field, a missing / mismatched version, unknown enumerated values
  falling back to their defaults, and a declared-size mismatch) and does not force checksum
  verification.
- **Culture-invariant**: all numeric and date parsing and formatting is performed with the invariant
  culture, so a file parses and describes identically regardless of the host's locale.
- **Not thread-safe**: `XISFFile`, `XISFImage`, `XISFDataBlock`, `XISFIccProfile` and
  `XISFThumbnail` decode and cache their bytes lazily on read, so instances must not be shared across
  threads without external synchronization.

### Requirements

DotNetXISF is written in pure C# and targets **.NET 10**. Two of the four XISF compression codecs
(`lz4` / `lz4hc` and `zstd`) have no equivalent in the .NET base class library, so the library takes
two managed NuGet dependencies for them —
[K4os.Compression.LZ4](https://www.nuget.org/packages/K4os.Compression.LZ4) and
[ZstdSharp.Port](https://www.nuget.org/packages/ZstdSharp.Port). Both are pure-managed (no native
binaries), MIT-licensed and cross-platform; `zlib` uses the built-in `System.IO.Compression`. The
library is continuously built and tested on Windows (see the CI badge above) in both Debug and Release
configurations. Nothing platform-specific is used, so the library runs anywhere .NET 10 does.

### Installation

DotNetXISF is available on [NuGet](https://www.nuget.org/packages/DotNetXISF). Add it to your project
with the .NET CLI:

```bash
dotnet add package DotNetXISF
```

Or add a `<PackageReference>` to your project file:

```xml
<PackageReference Include="DotNetXISF" Version="1.0.0" />
```

### Building

The solution (`DotNetXISF.slnx`) contains the `DotNetXISF` class library and the `DotNetXISFTests`
xUnit test suite:

```bash
dotnet build DotNetXISF.slnx -c Release
dotnet test  DotNetXISF.slnx -c Release
```

The two managed compression packages are restored automatically; no additional setup is required.

### Example Usage

```csharp
using System;
using DotNetXISF;

try
{
    XISFFile file = new XISFFile( "/path/to/file.xisf", XISFParsingOptions.Lenient );

    foreach( XISFImage image in file.Images )
    {
        Console.WriteLine( $"Image { image.Id ?? "<unnamed>" }: { image.Geometry }, { image.SampleFormat.SpecToken() }, { image.ColorSpace.SpecToken() }" );

        // Fully decoded (decompressed and un-shuffled) opaque pixel bytes; interpret them
        // using the image's typed geometry, sample format, byte order and pixel storage.
        ReadOnlyMemory< byte > pixels = image.Data;

        Console.WriteLine( $"  { pixels.Length } bytes of pixel data" );
    }

    // Unit-level properties and embedded FITS keywords.
    foreach( XISFProperty property in file.Properties )
    {
        Console.WriteLine( property );
    }

    if( file.Metadata is XISFMetadata metadata )
    {
        Console.WriteLine( metadata[ "XISF:CreatorApplication" ]?.Value.AsString ?? "<unknown creator>" );
    }
}
catch( XISFException exception )
{
    Console.WriteLine( exception.Message );
}
```

License
-------

Project is released under the terms of the MIT License.

Repository Infos
----------------

    Owner:          Jean-David Gadina - XS-Labs
    Web:            www.xs-labs.com
    Blog:           www.noxeos.com
    Twitter:        @macmade
    GitHub:         github.com/macmade
    LinkedIn:       ch.linkedin.com/in/macmade/
    StackOverflow:  stackoverflow.com/users/182676/macmade
