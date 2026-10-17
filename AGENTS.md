# AGENTS.md

Fesh.Rhino is a Rhino plugin (`Fesh.Rhino.rhp`) that hosts the [Fesh](https://github.com/goswinr/Fesh) F# scripting editor inside Rhino on Windows. The editor, F# Interactive and FSharp.Compiler.Service all come from the `Fesh` NuGet package. This repo is the glue: it loads the editor into Rhino, connects it to Rhino's UI thread, undo stack and command line, and packages it for Rhino's package manager (Yak). It is Windows only, because Fesh uses WPF.

Editor features (UI, FSI, completions, themes) belong in the Fesh repo, not here. It is often checked out next to this one as `../Fesh`.

## Commands

```powershell
dotnet build                      # Debug, all three targets: net48, net8.0-windows, net10.0-windows
dotnet build -f net8.0-windows    # faster: one target only
```

CI runs `.github/actions/package/action.yml` from both `build.yml` (every push and PR) and `release.yml`. It publishes each target, then builds the .yak packages into `bin/Release`:

```powershell
dotnet publish --self-contained --configuration Release --framework net48
dotnet publish --self-contained --configuration Release --framework net8.0-windows
dotnet publish --self-contained --configuration Release --framework net10.0-windows
```

`--self-contained` does not bundle a .NET runtime here, because this is a library without a RuntimeIdentifier. Rhino brings its own runtime. `PublishTrimmed` fails with IL1034, so it stays off.

If the build fails because `Fesh.Rhino.rhp` is locked, Rhino has the plugin loaded. Ask the user to close Rhino. Don't kill it.

## Testing

There are no tests. To verify a change:

1. Build all three targets. Code under `#if NET8`, `#if NET10` or `#if NETCOREAPP` compiles only in some of them.
2. Behavior can only be checked inside Rhino, so ask the user. They drag `bin/Debug/<tfm>/Fesh.Rhino.rhp` into Rhino and run `FeshDebug`. The build must match Rhino's runtime (see the table below). Rhino's `SetDotNetRuntime` command switches Rhino 8 between .NET Framework and .NET 8. A build for the wrong runtime shows a message box from `OnLoad` and does not load.

Debug and Release builds differ on purpose:

- Debug registers the command `FeshDebug` instead of `Fesh`, and passes the host name `RhinoDebug` to Fesh, so its settings go to `%APPDATA%\Fesh.RhinoDebug\Settings` instead of `%APPDATA%\Fesh.Rhino\Settings`.
- Debug has no assembly `Guid`, so Rhino forgets the plugin after a restart.

Rhino keeps the folders of all registered plugins in its assembly search path, also an old registration of a Debug build, e.g. of `bin/Debug/net48` from when Debug had a Guid. The net48 build has FCS 43.12 and FSharp.Core 10, higher versions than the net8 build, so Rhino hands them to the net8 build or to other F# plugins like Caddis. If FCS or FSharp.Core versions look wrong in Rhino, look for such registrations in Rhino's Options under Plug-ins. If an empty file `RhinoAssemblyResolveLog.txt` exists on the Desktop when Rhino starts, Rhino's resolver logs each request it gets and the assembly it returned. Requests that `PluginFolderResolver` answers never reach it.

`LouvreAbuDhabi.fsx` is an example script for users. It is not compiled.

## Layout

`Fesh.Rhino.fsproj` compiles three files, in this order:

- `Src/AssemblyInfo.fs`: the plugin Guid and the Rhino plugin descriptions
- `Src/PlugIn.fs`: the `FeshPlugin` singleton and its helper modules. `OnLoad` checks that Rhino's runtime matches the build. `WhenLoading` creates the editor with `Fesh.App.createEditorForHosting` and wires up all events.
- `Src/Commands.fs`: the Rhino commands `Fesh` and `FeshRunCurrentScript`. On load, the plugin also adds the alias `fr` for `FeshRunCurrentScript` if it is free. Both commands use `Style.ScriptRunner`, so scripts can call `RhinoApp.RunScript` (`rs.Command`).

`Util.libFolders` in `PlugIn.fs` are the folders where `#r "RhinoCommon.dll"` resolves without a path. The folder of the loaded RhinoCommon must come first: on .NET it is `System/netcore`, and the .NET Framework build in `System` fails to resolve `System.Drawing.Bitmap`. `Util.defaultCode` is the template for new script files.

## How the plugin works with Rhino

- **UI thread:** `Sync.syncContext` is captured when `PlugIn.fs` initializes on Rhino's UI thread. FSI callbacks that touch `RhinoDoc` or the views switch to it with `Async.SwitchToContext Sync.syncContext`. Scripts run on the UI thread by default, or on a background thread if the user turns on async mode.
- **Script runs:** `fesh.Fsi.OnCompiling` calls `FeshPlugin.BeforeEval`, which clears Rhino.Scripting's Esc flag and begins an undo record, so the user can undo a whole script in one step. Every way a run ends (`OnCompletedOk`, `OnRuntimeError`, `OnFsiEvalError`, `OnCanceled`) calls `FeshPlugin.AfterEval`, which ends the undo record and turns `Views.RedrawEnabled` back on. If you add another way for a run to end, it must call `AfterEval` too, or Rhino's views stay frozen.
- **`FeshRunCurrentScript`** waits until its own Rhino command has finished before it evaluates. Otherwise `BeginUndoRecord` returns 0.
- **Window:** closing the editor only hides it (`Closing` is cancelled), so the FSI session survives. `Closed` never fires. `Console.Out` and `Console.Error` go to the Fesh log while its window has focus, and back to the Rhino command line when it is closed. While the window is minimized, the log is also written to the Rhino command line.
- **Rhino's assembly resolver on .NET:** Rhino's `AssemblyResolve` handler matches only the simple name. It returns a loaded assembly of any version, or else the highest version it finds in the folders of all registered plugins, never the folder of the assembly that asks. The two handlers below must run before it.
- **Own dependencies on .NET:** `OnLoad` installs `PluginFolderResolver` on the `Resolving` event of the plugin's `AssemblyLoadContext`. .NET raises that event before `AppDomain.AssemblyResolve`, so it runs before Rhino's handler without reflection. It serves the DLLs of the plugin folder: Fesh, FSharp.Compiler.Service, AvalonEditB and the others. The event doesn't say which assembly asks, so it serves only requests for the same or a lower version, with the same public key token. A request for a higher version, e.g. another plugin's for FSharp.Core 10, stays Rhino's. So do the DLLs that Rhino or .NET have too, like Newtonsoft.Json and NuGet.\*. Without it the net8 build could get FCS 43.12 from another plugin and fail with a `MissingMethodException` for `FSharpChecker.Create`. It must be installed before Fesh or FCS loads, so don't use their types in module-level values of `PlugIn.fs` or in static fields of `FeshPlugin` (`static member val`): just JIT-compiling the initialization of the file would load them. FSharp.Core loads before any plugin code runs, when Rhino reads the assembly attributes, so `PluginFolderResolver.conflicts` can only report a wrong one, in the Rhino command line and the Fesh log.
- **FSI assemblies on .NET:** Fesh runs FSI with `--multiemit+`, so each evaluation is a separate dynamic assembly named `FSI-ASSEMBLY`, or `FSI-ASSEMBLY-MULTI` since FSharp.Compiler.Service 43.12 (the net10 build). Rhino's handler would return the wrong one, so `FsiAssemblyResolver` resolves them by full name among the dynamic assemblies of the current session. It needs `RequestingAssembly`, to ignore the FSI sessions of other plugins, so it can't use `Resolving`. Instead it goes in front of Rhino's handler by setting the private static `AssemblyResolve` field of `AssemblyLoadContext`. If a Fesh update changes FCS, check that the name prefix still matches.

## Reflection contract with Rhino.Scripting

[Rhino.Scripting](https://github.com/goswinr/Rhino.Scripting) is a separate library. This repo doesn't reference it. The two find each other via reflection, in both directions, so a rename breaks them at runtime, not at compile time. Code marked `Don't change name ... used via reflection` is part of this contract.

Rhino.Scripting reads from Fesh.Rhino:

- the Release assembly Guid `01dab273-99ae-4760-8695-3f29f4887831` in `Src/AssemblyInfo.fs`, to detect the hosting editor
- the namespace `Fesh.Rhino` and the module `Sync`, with `syncContext`, `showEditor`, `hideEditor`, `isEditorVisible`, `printFeshLogColor`, `printnFeshLogColor` and `clearFeshLog`. Keep their names and delegate types.

Fesh.Rhino reads from Rhino.Scripting:

- the private static field `initialize` of `Rhino.Scripting.RhinoSync` (`Rhino.RhinoSync` before Rhino.Scripting 0.7). Fesh.Rhino invokes it when the window loads, so that a Rhino.Scripting that another plugin loaded first picks up the `Sync` hooks.
- the static property `EscapePressed` of the internal class `Rhino.Scripting.State`, cleared before each run, in every loaded copy of Rhino.Scripting (there can be several after an FSI reset)

These lookups must tolerate older Rhino.Scripting versions in which the type or member is missing.

## Target frameworks and dependencies

| Target            | Runs in                          | `Fesh` package  | RhinoCommon reference |
| ----------------- | -------------------------------- | --------------- | --------------------- |
| `net48`           | Rhino 8 on .NET Framework        | `x.y.z-net472`  | 7.0                   |
| `net8.0-windows`  | Rhino 8.19 or later on .NET 8    | `x.y.z-net8`    | 8.19                  |
| `net10.0-windows` | Rhino 9                          | `x.y.z-net10`   | 9.0 beta              |

- Each target has its own conditional `ItemGroup` in `Fesh.Rhino.fsproj`. To update Fesh, change its version in all three and add a changelog entry that links to the Fesh changelog, like the existing ones.
- `DisableImplicitFSharpCoreReference` is set, so the `Fesh` package decides the FSharp.Core and FSharp.Compiler.Service versions, not the SDK. Don't add a FSharp.Core reference.
- RhinoCommon is a compile-time reference only (`ExcludeAssets="runtime"`). Rhino provides it at runtime. Keep its version in line with what Rhino.Scripting and Rhino.Scripting.FSharp reference.
- `netstandard.xml` (and on net48 also `netstandard.dll`) is copied to the output, so that FCS can show tooltips.
- `CopyLocalLockFileAssemblies` copies the package DLLs to the build output on net8 and net10 too, as on net48 and in `dotnet publish`. So a Debug build has its own FSharp.Core and FCS next to the .rhp for `PluginFolderResolver`. That's why `Microsoft.SourceLink.GitHub` excludes `runtime` assets, otherwise its `System.IO.Hashing.dll` would be copied too.
- The fsproj defines `NET8` and `NET10`. `NETCOREAPP` is defined by the SDK for both .NET targets.
- `LangVersion` is `preview`, for `^` end indexing.

## Packaging and releases

Each version is published as two Yak distributions, built by `.github/actions/package/action.yml`:

- `fesh-x.y.z-rh8-win.yak` with `net48` and `net8.0-windows`
- `fesh-x.y.z-rh9-win.yak` with `net10.0-windows`

`yak build` names the file after the highest RhinoCommon reference, e.g. `rh8_19`. The action renames it to drop the minor version, so that all Rhino 8 versions get it. Yak offers an `rhN` distribution only to Rhino with the same major version. Don't go back to `any-win`, which would offer the Rhino 8 package to Rhino 7 and 9 too.

- `Src/yak.yml` is the Yak manifest. The build copies it to `bin/<Configuration>/manifest.yml`, and `Media/logo128.png` to `logo.png`, outside the framework folders. Its `version: 0.0.0` is a placeholder. The real version comes from `bin/ChangelogVersion.txt`, which a target in the fsproj writes after each build.
- `yak.exe` is downloaded at a pinned version and checked against a pinned SHA256. Update both together.
- `release.yml` runs on a pushed `x.y.z` tag. It fails if the tag differs from the version in CHANGELOG.md, then pushes both .yak files. Only the maintainer creates tags.
- GitHub Actions are pinned by commit SHA, with the version in a comment. Dependabot updates them, but not the NuGet packages.

## CHANGELOG.md and versions

The version of the plugin and the Yak packages comes from `CHANGELOG.md`, through `Ionide.KeepAChangelog.Tasks`. The fsproj has no `<Version>`.

- Add user-facing changes under `## [Unreleased]`, in `### Added`, `### Changed` or `### Fixed`, worded like the existing entries.
- Write every bullet on a single line, with no wrapped lines and no nested bullets. The parser crashes on them.
- Don't mix line endings in the file (it is CRLF in the working tree). Mixed line endings fail the build with IKC0002.
- Run `dotnet build` after editing the changelog.

## Conventions

- All text files use CRLF line endings. Some have a UTF-8 BOM. Keep both as they are.
