# Port SwiftXISF to C# / .NET 10

Port SwiftXISF, an XISF file parser written in Swift, to idiomatic C# targeting .NET 10.

The original Swift repository is checked out at: `../SwiftXISF`

The current directory contains:

- An empty .NET solution
- A C# class library project
- An xUnit test project

## Primary goals

1. Port the complete SwiftXISF implementation to C#.
2. Port all unit tests to xUnit.
3. Preserve the original public API and class structure where practical.
4. Preserve the original behavior and parsing logic unless a difference is required by C# or .NET.
5. Produce an idiomatic, maintainable C# library rather than a mechanical line-by-line translation.
6. Leave the solution compiling with all tests passing.
7. Update the README after the implementation is complete.

## Porting guidelines

Keep names, responsibilities, and relationships between types reasonably consistent with the Swift project so that users familiar with SwiftXISF can recognize the C# API.

However, do not attempt to reproduce Swift-specific language patterns when they would result in unnatural C#. Prefer established C# and .NET conventions.

Match normal .NET naming conventions but match formatting conventions from SwiftXISF.

You may add extensions to standard .NET types where appropriate to match the Swift codebase and the Swift standard library.

Use exceptions for error handling, as in SwiftXISF.

All parsing and formatting that is part of the XISF format must be deterministic and independent of the machine’s current culture. Use explicit culture and comparison rules where relevant.

Exact textual output does not need to match where the difference is merely a normal language-runtime convention.  
For example, Swift may represent a floating-point value as "42.0" in a string, whereas the default C# representation is "42". Such differences are acceptable unless the text is part of a specified serialization format, an XISF requirement, or behavior explicitly tested by the original project.  
You may adapt the unit tests in such cases.

You will be working on macOS, but .NET 10 is installed, so you can compile and run tests.

## Notes

A similar port was made to port SwiftFITS to C# / .NET 10:

- `../SwiftFITS`
- `../DotNetFITS`

As SwiftXISF was originally based on SwiftFITS, make sure to inspect the .NET port: `DotNetFITS`.  
The two .NET ports should be consistent with each other, just as the Swift versions are.

You can find the original port plan in `../DotNetFITS/Docs/agent-plans/completed/2026-07-16-dotnet-port/dotnet-port-plan.html`.  
Make sure to read this document and apply the same principles, conventions, patterns, etc. This should answer a lot of your questions and guide your implementation.