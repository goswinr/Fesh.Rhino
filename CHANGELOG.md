# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Fixed
- On .NET 10 (Rhino 9) values defined in earlier evaluations could still fail or come from an old session: FSharp.Compiler.Service 43.12 names the assemblies of evaluations `FSI-ASSEMBLY-MULTI` instead of `FSI-ASSEMBLY`, which the resolver of 0.34.0 did not match.
- `rs.EscapeTest()` of Rhino.Scripting raised right at the start of a script if Esc had been pressed in Rhino before, e.g. to deselect objects. Fesh.Rhino now clears that Esc state before each script.

## [0.34.0] - 2026-10-05
### Changed
- Update Fesh to 0.34.0 for all target frameworks.

### Fixed
- On .NET 8 and 10, values defined in earlier evaluations failed with a `TypeLoadException`, and after a reset could silently be those of an old session. With `--multiemit+` each evaluation is an assembly named `FSI-ASSEMBLY`, and Rhino's assembly resolver returned the first one of the process. Fesh.Rhino now resolves them first, from the current session.
- Re-initializing Rhino.Scripting when it was loaded before Fesh.Rhino failed: it looked for a public property `initialize` of `Rhino.RhinoSync`, but `initialize` is a private static field, and since Rhino.Scripting 0.7 the class is `Rhino.Scripting.RhinoSync`.

## [0.33.5] - 2026-10-04
### Changed
- Update Fesh to 0.33.5 for all target frameworks.
- Update SourceLink to 10.0.401 to remove the vulnerable Microsoft.Build.Tasks.Git dependency.

### Fixed
- End the script undo record when FSI reports an evaluation error without a runtime exception; ignore undo records that failed to start and clear consumed records even after a document change.
- Restore lookup of shared Rhino assemblies such as Eto.dll while keeping the loaded RhinoCommon assembly folder first.
- Restore console output to Rhino when closing the editor, which hides the window instead of raising its Closed event.

## [0.33.4] - 2026-10-02
### Fixed
- On .NET Core `#r "RhinoCommon.dll"` now resolves to the .NET Core build in the `System/netcore` folder instead of the .NET Framework build next to Rhino.exe. This fixes the error about resolving `System.Drawing.Bitmap` when opening the `Rhino` namespace.

## [0.33.3] - 2026-10-02
### Added
- Build for .NET 10 for Rhino 9, using Fesh 0.33.2-net10. It is published as a separate `rh9-win` distribution of the yak package.

### Changed
- The yak package with the .NET 8 and .NET Framework 4.8 builds is now published as `rh8-win` instead of `any-win`, so it is only offered in Rhino 8.

## [0.33.2] - 2026-10-01
### Changed
- Update to [Fesh 0.33.2](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0332)
- The Rhino System folder is searched for assemblies referenced via `#r`, so `#r "RhinoCommon.dll"` works without a full path
- Files next to the script can be referenced by name or relative path in `#r` and `#load`

## [0.32.3] - 2026-05-23
### Changed
- Update to [Fesh 0.32.3](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0323)

## [0.32.2] - 2026-05-10
### Changed
- Update to [Fesh 0.32.2](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0322)

## [0.32.0] - 2026-01-28
### Fixed
- Include netstandard.xml in output, to show better tooltips in net10

## [0.31.1] - 2026-01-17
### Changed
- Try fix Duplicate printing to Rhino command line.

## [0.31.0] - 2025-12-19
### Changed
- Update to [Fesh 0.31.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0310)


## [0.30.1] - 2025-12-19
### Changed
- Update to [Fesh 0.30.1](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0301)
- warn on if rhino .is in net7 runtime.


## [0.29.4] - 2025-12-17
### Changed
- Try again to fix issue with FSharp.Core not loading in Rhino net8 runtime by using `dotnet publish --framework net8.0-windows --self-contained`


## [0.29.3] - 2025-12-14
### Changed
- Update to [Fesh 0.29.3](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0293) to try fix issue with FSharp.Core loading in Rhino net8 runtime

## [0.29.0] - 2025-12-14
### Changed
- Update to [Fesh 0.29.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0290)
- build for net8 and net48


## [0.28.1] - 2025-10-05
### Changed
- Update to [Fesh 0.28.1](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0281)


## [0.28.0] - 2025-06-13
### Changed
- Update to [Fesh 0.28.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0280)

## [0.27.4] - 2025-05-25
### Changed
- try build for net7 again

## [0.27.3] - 2025-05-25
### Changed
- try build for net7

## [0.27.2] - 2025-05-25
### Changed
- expose AvalonLog for use via Reflection as F# functions

## [0.27.1] - 2025-05-25
### Changed
- expose AvalonLog for use via Reflection

## [0.27.0] - 2025-05-24
### Added
- Update to [Fesh 0.27.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0270)

## [0.26.3] - 2025-04-14
### Added
- Update to [Fesh 0.26.3](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0263)

## [0.26.1] - 2025-03-17
### Added
- Update to [Fesh 0.26.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0260)

## [0.25.1] - 2025-03-22
### Changed
- Revert async mode in before and after eval hooks from 0.24.0

## [0.25.0] - 2025-03-19
### Added
- Update to [Fesh 0.25.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0250)

## [0.24.0] - 2025-03-17
### Added
- Update to [Fesh 0.24.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0241)

## [0.23.0] - 2025-02-16
### Fixed
- include FSharp.Core.xml

## [0.22.0] - 2025-02-15
### Changed
- Update to [Fesh 0.22.0](https://github.com/goswinr/Fesh/blob/main/CHANGELOG.md#0220)

## [0.20.0] - 2025-01-20
### Changed
- Updated to Fesh 0.20.0

## [0.19.0] - 2025-01-13
### Changed
- Updated to Fesh 0.19.0

## [0.16.3] - 2024-12-15
### Added
- Messages about Auto-Updates

## [0.16.2] - 2024-12-15
### Added
- Yak build version check

## [0.16.1] - 2024-12-14
### Changed
- Updated to Fesh 0.16.0

## [0.13.0] - 2024-10-20
### Fixed
- Fix crashes of Rhino in case of assembly version conflicts
- Updated to Fesh 0.13.0
- Synchronous mode is now the default

## [0.11.1] - 2024-10-07
### Fixed
- first public release

[Unreleased]: https://github.com/goswinr/Fesh.Rhino/compare/0.33.4...HEAD
[0.33.4]: https://github.com/goswinr/Fesh.Rhino/compare/0.33.3...0.33.4
[0.33.3]: https://github.com/goswinr/Fesh.Rhino/compare/0.33.2...0.33.3
[0.33.2]: https://github.com/goswinr/Fesh.Rhino/compare/0.32.3...0.33.2
[0.32.3]: https://github.com/goswinr/Fesh.Rhino/compare/0.32.2...0.32.3
[0.32.2]: https://github.com/goswinr/Fesh.Rhino/compare/0.31.1...0.32.2
[0.31.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.31.0...0.31.1
[0.31.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.30.1...0.31.0
[0.30.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.29.4...0.30.1
[0.29.4]: https://github.com/goswinr/Fesh.Rhino/compare/0.29.3...0.29.4
[0.29.3]: https://github.com/goswinr/Fesh.Rhino/compare/0.29.0...0.29.3
[0.29.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.28.1...0.29.0
[0.28.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.28.0...0.28.1
[0.28.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.27.4...0.28.0
[0.27.4]: https://github.com/goswinr/Fesh.Rhino/compare/0.27.3...0.27.4
[0.27.3]: https://github.com/goswinr/Fesh.Rhino/compare/0.27.2...0.27.3
[0.27.2]: https://github.com/goswinr/Fesh.Rhino/compare/0.27.1...0.27.2
[0.27.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.27.0...0.27.1
[0.27.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.26.3...0.27.0
[0.26.3]: https://github.com/goswinr/Fesh.Rhino/compare/0.26.1...0.26.3
[0.26.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.25.1...0.26.1
[0.25.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.25.0...0.25.1
[0.25.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.24.0...0.25.0
[0.24.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.23.0...0.24.0
[0.23.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.22.0...0.23.0
[0.22.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.20.0...0.22.0
[0.20.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.19.0...0.20.0
[0.19.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.16.3...0.19.0
[0.16.3]: https://github.com/goswinr/Fesh.Rhino/compare/0.16.2...0.16.3
[0.16.2]: https://github.com/goswinr/Fesh.Rhino/compare/0.16.1...0.16.2
[0.16.1]: https://github.com/goswinr/Fesh.Rhino/compare/0.13.0...0.16.1
[0.13.0]: https://github.com/goswinr/Fesh.Rhino/compare/0.11.1...0.13.0
[0.11.1]: https://github.com/goswinr/Fesh.Rhino/releases/tag/0.11.1
