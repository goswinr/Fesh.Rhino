![Logo](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/logo128.png)
# Fesh.Rhino

This repo contains a [Rhino](https://www.rhino3d.com/) plugin to host [Fesh](https://github.com/goswinr/Fesh).
Fesh is an F# scripting editor based on [AvalonEdit](https://github.com/goswinr/AvalonEditB) for Windows.
The editor supports the latest features of F# 8.0 via [Fsharp Compiler Service](https://www.nuget.org/packages/FSharp.Compiler.Service).
It has semantic syntax highlighting, auto completion and type info tooltips.
The output window supports colored text.

Until [RhinoCommon](https://www.nuget.org/packages/rhinocommon#supportedframeworks-body-tab) is targeted properly for .NET 7 this plugin only supports .NET Framework 4.8 .
In Rhino use the command [SetDotNetRuntime](https://www.rhino3d.com/en/docs/guides/netcore/) to switch between .NET Framework and .NET Core.


The example script [LouvreAbuDhabi.fsx](https://github.com/goswinr/Fesh.Rhino/blob/main/LouvreAbuDhabi.fsx) in the root folder generates the axes for cladding of the Louvre Abu Dhabi.
As shown in my talk at [FSharpConf 2016](https://www.youtube.com/watch?v=ZY-bvZZZZnE):

![Screenshot](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/screen1.png)

## Set Up
After downloading from [Releases](https://github.com/goswinr/Fesh.Rhino/releases) or compiling via `dotnet build`.
Drag and drop the file `Fesh.rhp` into Rhino.
Then launch the editor with the command `Fesh`.

## Coding
You will always need to add a reference to RhinoCommon.dll


```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" // or where ever you have it
```

If you are used to doing Rhino Scripting with Python I recommend using the [Rhino.Scripting nuget package](https://nuget.org/packages/Rhino.Scripting) to have the same 900 functions available.


```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll"
#r "nuget:Rhino.Scripting.Fsharp, 0.8.1"

open System
open Rhino.Scripting
open Rhino.Scripting.Fsharp //recommended for F#
open FsEx // part of Rhino.Scripting

type rs = RhinoScriptSyntax

// use the rs object to call RhinoScript functions like in python
let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)
```

## Running
Run your script by presssing `F5` key.
Like in Visual Studio you can also just evaluate the selected text by pressing `Alt` + `Enter` keys.
See the `FSI` menu for more options.

### License
[MIT](https://github.com/goswinr/Fesh.Rhino/blob/main/LICENSE)

