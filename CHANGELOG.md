# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.1] - 2026-07-06

### Added
- Package documentation under `Documentation~/` (manual, getting started, architecture, hooks, function components, diffing, API reference) with Japanese translations under `Documentation~/ja/`.
- XML documentation comments (`///`) on all public APIs in `Lib/`.
- `AGENTS.md`, `CLAUDE.md`, and `llms.txt` for coding agents.
- This changelog.
- `license` field (MIT) in `package.json`.

### Changed
- README: added Documentation section; package table now lists all four packages (including `com.uzimaru.veauty-uitoolkit`).

## [0.5.0] - 2026-07-01

### Added
- Function components: `FunctionComponentNode`, `FunctionComponents.Create/Keyed`, and the `FunctionComponent` delegates.
- Hooks API: `Hooks.UseState` / `UseRef` / `UseEffect`, `State<T>`, `Ref<T>`, backed by `HookRuntime` (`Resolve`, `CommitEffects`, `Dispose`).
- `[Component]` attribute with an ILPostProcessor (`CodeGen/Veauty.CodeGen.dll`, source in `SourceGenerator~/`) that transparently wraps annotated static methods in `FunctionComponents.Create()` at compile time.
- Host lifecycle nodes (`IHostLifecycle<T>`, `HostLifecycleNode<T>/<T,U>`, `HostLifecycleKeyedNode<T>/<T,U>`) as the composable replacement for `Widget<T>`.
- `IVTreeWrapper` unwrapping in diff and hook resolution.
- Package dependency on `com.unity.nuget.mono-cecil`.
- English `README.md` and Japanese `README.ja.md`.

### Changed
- Minimum Unity version raised to 6000.5 (Unity 6).
- Unsupported tree types now throw `ArgumentException` including the actual type name instead of a generic `Exception`.

### Removed
- `Widget<T>` (including `Init` / `AfterRenderKids` / `Destroy` on nodes); use host lifecycle nodes or function components instead.

### Fixed
- Diff redraws when tags differ regardless of component type, and when a widget's rendered tags differ.
- Structural equality of core diff results and patch types.

## [0.4.0] - 2021-07-15

### Added
- `Equals` / `GetHashCode` implementations for VTree and patch types.

### Changed
- `Node` constructors accept `IEnumerable` child arguments; `GetKids` related APIs reworked.

## [0.3.2] - 2021-07-12

### Fixed
- Constructor argument handling for nodes.

## [0.3.1] - 2021-07-11

### Changed
- Variable-length (`params`) arguments for node kids.

## [0.3.0] - 2021-07-08

### Changed
- Removed Unity class dependencies from the core (pure C# assembly).

### Added
- Test files for the core.

## [0.2.0] - 2020-06-16

### Changed
- Moved `IPatch` implementations into the `Veauty.Patch` namespace.

## [0.1.2] - 2020-05-17

### Fixed
- `NodeType` handling.

## [0.1.1] - 2020-05-15

### Changed
- Relaxed `TypedNode` generic constraints.

## [0.1.0] - 2020-05-15

### Added
- Initial release: VTree types (`Node`, `KeyedNode`, `Widget`), attribute system, diff algorithm and patch types.
- Rendering split into a separate package (`com.uzimaru.veauty-gameobject`).
