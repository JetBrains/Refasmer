Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Fixed
- [#38: Internal types in public API break `omit-non-api-members=true`](https://github.com/JetBrains/Refasmer/issues/38).
- Fix regression since v2.0.0: public compiler-generated types are no longer ommitted from the refasmed assemblies.

## [2.0.0] - 2024-11-20
### Changed
- **(Breaking change!)** A new mandatory parameter `--omit-non-api-types` (pass either `true` or `false`).

  This parameter determines when to preserve or omit private nested types in value types. We have made it mandatory since the previous changes in the value type behavior might've been caused confusion in cases when the user wanted to remove _all_ the private API from an assembly.

  `--omit-non-api-types false` closely corresponds to the behavior from 1.0.33; `--omit-non-api-types true` corresponds to the behavior from 1.0.32.

  Refasmer now does its best to preserve some of the value type semantics even with `--omit-non-api-types true`: it will emit synthetic private field in a value type even if it has removed all the fields before. Note that this change might not preserve the type blittability and still affect the compilation. 

## [1.0.33] - 2023-05-11
### Fixed
- [#18](https://github.com/JetBrains/Refasmer/issues/18): always import all members of the value types, since they may affect the compilation behavior.
- [#20: Bad signature is produced for function pointers](https://github.com/JetBrains/Refasmer/issues/20).

### Added
- Support .NET 7 as the host runtime.

## [1.0.32] - 2023-02-01
### Changed
- Migrate from the obsolete SHA1 API.

## [1.0.31] - 2023-01-27
### Fixed
- [#14](https://github.com/JetBrains/Refasmer/issues/14): choose the system runtime library with the greatest version if there are several referenced. Previously, such situation was fatal.

## [1.0.30] - 2022-05-26
### Added
- Support .NET 6 as a host runtime.
- Enable roll forward = major in the CLI tool.

## [1.0.29] - 2021-01-21
### Changed
- Minor tracing logging update.

## [1.0.28] - 2022-01-21
### Added
- Preserve any attributes from `System.Runtime.CompilerServices` and `System.Diagnostic.CodeAnalysis` in the processed assemblies. 

## [1.0.27] - 2022-01-19
### Changed
- Improve looking for the system runtime library, include `netstandard` and `System.Private.CoreLib`. 

## [1.0.26] - 2022-01-18
### Changed
- Find the `ReferenceAssemblyAttribute` in a referenced version of the system runtime library if possible.

## [1.0.25] - 2022-01-17
### Added
- Globbing support in arguments.

## [1.0.24] - 2021-12-09
### Fixed
- Void method signature for the `ReferenceAssemblyAttribute` was emitted incorrectly. 

## [1.0.23] - 2021-11-29
### Added
- `--noattr` switch to omit the reference assembly attribute from the resulting assembly.

## [1.0.22] - 2021-11-09
### Fixed
- Fix the signature write for the `NotImplementedException` type.

## [1.0.21] - 2021-11-09
### Fixed
- Fix the signature check for the `NotImplementedException` type.

## [1.0.20] - 2021-11-08
### Changed
- `--publiconly` switch is renamed to `--public`.

### Added
- `--internals` switch to import both public and internal types.
- `--all` switch to import types of any visibility.

### Fixed
- Always import the `<Module>` type.
- Do not mock the interface methods in the mock mode.

## [1.0.19] - 2021-09-29
### Added
- An option to create "mock assemblies" — not marked as a reference assemblies, but throwing `System.NotImplementedException` from any method.

## [1.0.18] - 2021-08-05
### Changed
- Force the resulting assembly to target `AnyCPU`.

## [1.0.17] - 2021-08-03 
No notable changes.

## [1.0.16] - 2021-08-03
### Fixed
- Fix input files being never closed (up until finalizer) which would previously cause a `System.IO.IOException: The process cannot access the file because it is being used by another process`.

### Removed
- Remove a dependency on the Microsoft.Extensions.Logging package.

### Added
- `?` switch to activate help.
- `--publiconly` switch to drop non-public types even if the `InternalsVisibleTo` attribute is present.

## [1.0.15] - 2021-08-03
### Changed
- Switch from `net461` to `netstandard2.0`.
- `JetBrains.Refasmer.NetStandardSubstitution` is now `internal`.

## [1.0.14] - 2021-08-03
No changes.

## [1.0.13] - 2021-08-03
### Fixed
- [#3](https://github.com/JetBrains/Refasmer/issues/3): document the (missing) wildcard support.

### Added
- Support .NET 5 as host runtime.
- Add the `ReferenceAssemblyAttribute` to the generated assemblies.

## [1.0.12] - 2020-09-24
### Changed
- Minor package metadata update.

## [1.0.11] - 2020-09-24
### Changed
- Show help when called with no arguments.

## [1.0.9] - 2020-09-24
### Added
- An icon for the package. 

## [1.0.0.8] - 2020-08-26
### Changed
- Update the package metadata; description for the CLI tool.

## [1.0.0.7] - 2020-08-26
### Added
- The new JetBrains.Refasmer.CliTool package with the application packages as a dotnet tool.

## [1.0.0.5] - 2020-08-26
No notable changes. 

## [1.0.0.4] - 2020-08-25
### Changed
- Set the package license correctly.

## [1.0.0.3] - 2020-08-25
No notable changes.

## [1.0.0.2] - 2020-08-25
Release the initial version in form of a .NET executable and a NuGet package.

[1.0.0.2]: https://github.com/JetBrains/Refasmer/releases/tag/1.0.0.2
[1.0.0.3]: https://github.com/JetBrains/Refasmer/compare/1.0.0.2...1.0.0.3
[1.0.0.4]: https://github.com/JetBrains/Refasmer/compare/1.0.0.3...1.0.0.4
[1.0.0.5]: https://github.com/JetBrains/Refasmer/compare/1.0.0.4...1.0.0.5
[1.0.0.7]: https://github.com/JetBrains/Refasmer/compare/1.0.0.5...1.0.0.7
[1.0.0.8]: https://github.com/JetBrains/Refasmer/compare/1.0.0.7...1.0.0.8
[1.0.9]: https://github.com/JetBrains/Refasmer/compare/1.0.0.8...1.0.9
[1.0.11]: https://github.com/JetBrains/Refasmer/compare/1.0.9...1.0.11
[1.0.12]: https://github.com/JetBrains/Refasmer/compare/1.0.11...1.0.12
[1.0.13]: https://github.com/JetBrains/Refasmer/compare/1.0.12...1.0.13
[1.0.14]: https://github.com/JetBrains/Refasmer/compare/1.0.13...1.0.14
[1.0.15]: https://github.com/JetBrains/Refasmer/compare/1.0.14...1.0.15
[1.0.16]: https://github.com/JetBrains/Refasmer/compare/1.0.15...1.0.16
[1.0.17]: https://github.com/JetBrains/Refasmer/compare/1.0.16...1.0.17
[1.0.18]: https://github.com/JetBrains/Refasmer/compare/1.0.17...1.0.18
[1.0.19]: https://github.com/JetBrains/Refasmer/compare/1.0.18...1.0.19
[1.0.20]: https://github.com/JetBrains/Refasmer/compare/1.0.19...1.0.20
[1.0.21]: https://github.com/JetBrains/Refasmer/compare/1.0.20...1.0.21
[1.0.22]: https://github.com/JetBrains/Refasmer/compare/1.0.21...1.0.22
[1.0.23]: https://github.com/JetBrains/Refasmer/compare/1.0.22...1.0.23
[1.0.24]: https://github.com/JetBrains/Refasmer/compare/1.0.23...1.0.24
[1.0.25]: https://github.com/JetBrains/Refasmer/compare/1.0.24...1.0.25
[1.0.26]: https://github.com/JetBrains/Refasmer/compare/1.0.25...1.0.26
[1.0.27]: https://github.com/JetBrains/Refasmer/compare/1.0.26...1.0.27
[1.0.28]: https://github.com/JetBrains/Refasmer/compare/1.0.27...1.0.28
[1.0.29]: https://github.com/JetBrains/Refasmer/compare/1.0.28...1.0.29
[1.0.30]: https://github.com/JetBrains/Refasmer/compare/1.0.29...1.0.30
[1.0.31]: https://github.com/JetBrains/Refasmer/compare/1.0.30...1.0.31
[1.0.32]: https://github.com/JetBrains/Refasmer/compare/1.0.31...1.0.32
[1.0.33]: https://github.com/JetBrains/Refasmer/compare/1.0.32...1.0.33
[2.0.0]: https://github.com/JetBrains/Refasmer/compare/1.0.33...v2.0.0
[Unreleased]: https://github.com/JetBrains/Refasmer/compare/v2.0.0...HEAD
