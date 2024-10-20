![Logo](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/logo128.png)
# Fesh.Rhino

![code size](https://img.shields.io/github/languages/code-size/goswinr/Fesh.Rhino.svg)
[![license](https://img.shields.io/github/license/goswinr/Fesh.Rhino)](LICENSE)

Fesh.Rhino is an F# scripting editor hosted inside [Rhino3D](https://www.rhino3d.com/) on Windows. It is based on [Fesh](https://github.com/goswinr/Fesh).
It has semantic syntax highlighting, auto completion, type info tooltips and more.
The output window supports colored text.

Until [RhinoCommon](https://www.nuget.org/packages/rhinocommon#supportedframeworks-body-tab) is targeted properly for .NET 7 this plugin only supports .NET Framework 4.8.
In Rhino use the command [SetDotNetRuntime](https://www.rhino3d.com/en/docs/guides/netcore/) to switch between .NET Framework and .NET Core.


The example script [LouvreAbuDhabi.fsx](https://github.com/goswinr/Fesh.Rhino/blob/main/LouvreAbuDhabi.fsx) in the root folder generates the axes for cladding of the Louvre Abu Dhabi.
As shown in my talk at [FSharpConf 2016](https://www.youtube.com/watch?v=ZY-bvZZZZnE):

![Screenshot](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/screen1.png)

## Set Up
After downloading from [Releases](https://github.com/goswinr/Fesh.Rhino/releases) or compiling via `dotnet build`.
Drag and drop the file `Fesh.rhp` into Rhino.
Then launch the editor with the command `Fesh`.

The editor might not load properly if you have already another plug-in loaded that uses an older version of Fsharp.Core.
See this [issue](https://github.com/goswinr/Fesh.Rhino/issues/2.)

## Get Started Coding
All you need is to add a reference to RhinoCommon.dll:

```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" // adapt path if needed
open Rhino
```

If you are used to doing Rhino Scripting with Python I recommend using the [Rhino.Scripting](https://github.com/goswinr/Rhino.Scripting) to have the same 900 functions available.
In addition I recommend the [Rhino.Scripting.Fsharp](https://github.com/goswinr/Rhino.Scripting.Fsharp) package.
It provides useful extensions and curried functions for piping and partial application.


```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll"
#r "nuget:Rhino.Scripting.Fsharp, 0.8.0" // includes Rhino.Scripting and FsEx

open System
open Rhino.
open Rhino.Scripting
open Rhino.Scripting.Fsharp // for curried functions
open FsEx // for extensions to Fsharp Collections

type rs = RhinoScriptSyntax

// use the rs object to call RhinoScript functions like in python
let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)
```

## Running
Run your script by presssing `F5` key.
Like in Visual Studio you can also just evaluate the selected text by pressing `Alt` + `Enter` keys.
See the `FSI` menu for more options.

## Blocking the UI thread ?
You can choose to run the scripts in Synchronous on the UI thread or Asynchronous on a background thread.
Synchronous mode is the default. Your UI will be blocked while the script is running.
But the interaction with Rhino is safer.

![async mode](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/async.png)

While the main Rhino Document is officially not thread safe,
modifying the Rhino Document from a background thread is actually OK as long as there is only one thread doing it.
The main reason to use this editor in Async mode is to keep the Rhino UI and the Fesh UI responsive while doing long running operations.

The [Rhino.Scripting](https://github.com/goswinr/Rhino.Scripting) library can be used from any thread.
If running async it will automatically marshal all calls that affect the UI to the main Rhino UI thread and wait for switching back till completion on UI thread.


### License
[MIT](https://github.com/goswinr/Fesh.Rhino/blob/main/LICENSE)


## Changelog
`0.13.0`
- Fix crashes of Rhino in case of assembly version conflicts
- Updated to Fesh 0.13.0


`0.12.0`
- Synchonous mode is now the default
- Updated to Fesh 0.12.0

`0.11.1`
- first public release

