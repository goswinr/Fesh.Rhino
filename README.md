![Logo](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/logo128.png)
# Fesh.Rhino

[![build](https://github.com/goswinr/Fesh.Rhino/actions/workflows/build.yml/badge.svg?event=push)](https://github.com/goswinr/Fesh.Rhino/actions/workflows/build.yml)
[![publish yak](https://github.com/goswinr/Fesh.Rhino/actions/workflows/release.yml/badge.svg?event=push)](https://github.com/goswinr/Fesh.Rhino/actions/workflows/release.yml)
[![Check NuGet](https://github.com/goswinr/Fesh.Rhino/actions/workflows/outdatedNuget.yml/badge.svg)](https://github.com/goswinr/Fesh.Rhino/actions/workflows/outdatedNuget.yml)

![latest tag](https://img.shields.io/github/v/tag/goswinr/Fesh.Rhino?label=Latest%20Tag)
![yak version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2Ffesh&query=%24.version&logo=rhinoceros&label=Yak%20Package%20Manager&color=%23a3d6ff)

[![license](https://img.shields.io/github/license/goswinr/Fesh.Rhino)](LICENSE)
![code size](https://img.shields.io/github/languages/code-size/goswinr/Fesh.Rhino.svg)

Fesh.Rhino is an F# scripting editor hosted inside [Rhino3D](https://www.rhino3d.com/) on Windows.<br>
It is based on [Fesh](https://github.com/goswinr/Fesh).<br>
It has semantic syntax highlighting, auto-completion, type info tooltips and more.<br>
The output window supports colored text via [Fesher](https://github.com/goswinr/Fesher).


The example script [LouvreAbuDhabi.fsx](https://github.com/goswinr/Fesh.Rhino/blob/main/LouvreAbuDhabi.fsx) in the root folder generates the axes for cladding of the Louvre Abu Dhabi.<br>
As shown in my talk at [FSharpConf 2016](https://www.youtube.com/watch?v=ZY-bvZZZZnE):

![Screenshot](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/screen1.png)

## .NET Framework or .NET Core?
This plugin supports .NET Framework 4.8.<br>
.NET 7 is supported if your Rhino version is higher than 8.19 (May 2025).<br>
If you installed Fesh via the [Package Manager](https://www.rhino3d.com/features/package-manager/) then the correct framework will be picked automatically.<br>
In Rhino use the command [SetDotNetRuntime](https://www.rhino3d.com/en/docs/guides/netcore/) to switch between .NET Framework and .NET Core.
If you are on the wrong runtime you will get an error message box when trying to load the plugin.

## Installation

### Food for Rhino

Install via Food for Rhino: https://www.food4rhino.com/en/app/fesh<br>
Or from inside Rhino via the `PackageManager` command, then search for `Fesh`.<br>
No admin rights should be needed for this installation.

Then launch the editor with the command `Fesh`.

### Manual Installation
You can also build the plugin from this repository via `dotnet build`.<br>
Then drag and drop the file `Fesh.Rhino.rhp` into Rhino.<br>
Then launch the editor with the command `Fesh`.

## Known Issues
The editor might not load properly if you already have another plugin loaded that uses an older version of `FSharp.Core`.<br>
See this [issue](https://github.com/goswinr/Fesh.Rhino/issues/2).<br>
Please report any issues you encounter.

## Get Started Coding
All you need is to add a reference to RhinoCommon.dll:

```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" // adapt path if needed
open Rhino
```

If you are used to doing Rhino Scripting with Python I recommend using the [Rhino.Scripting](https://github.com/goswinr/Rhino.Scripting) to have the same 900 functions available.<br>
In addition I recommend the [Rhino.Scripting.FSharp](https://github.com/goswinr/Rhino.Scripting.FSharp) package.<br>
It provides useful extensions and curried functions for piping and partial application.


```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll"
#r "nuget:Rhino.Scripting.FSharp" // includes Rhino.Scripting

open System
open Rhino
open Rhino.Scripting
open Rhino.Scripting.FSharp // for curried functions

type rs = RhinoScriptSyntax

// use the rs object to call RhinoScript functions like in Python
let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)
```

## Running
Run your script by pressing `F5` key.<br>
Like in Visual Studio you can also just evaluate the selected text by pressing `Alt` + `Enter` keys.<br>
See the `FSI` menu for more options.

## Blocking the UI thread
You can choose to run the scripts in Synchronous mode on the UI thread or asynchronous mode on a background thread.<br>
Synchronous mode is the default. Your UI will be blocked while the script is running.<br>
But the interaction with Rhino is safer.

![async mode](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/async.png)

While the main Rhino Document is officially not thread-safe,
modifying the Rhino Document from a background thread is actually OK as long as there is only one thread doing it.<br>
The main reason to use this editor in async mode is to keep the Rhino UI and the Fesh UI responsive while doing long-running operations.

The [Rhino.Scripting](https://github.com/goswinr/Rhino.Scripting) library can be used from any thread.<br>
If running async it will automatically marshal all calls that affect the UI to the main Rhino UI thread and wait for switching back until completion on the UI thread.

## Changelog
See [CHANGELOG.md](https://github.com/goswinr/Fesh.Rhino/blob/main/CHANGELOG.md)

## License
[MIT](https://github.com/goswinr/Fesh.Rhino/blob/main/LICENSE)
