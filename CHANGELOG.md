# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/goswinr/Fesh.Rhino/compare/0.31.0...HEAD
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

<!-- use to get tag dates:
<!-- use to get tag dates:
git log --tags --simplify-by-decoration --pretty="format:%ci %d"
-->
