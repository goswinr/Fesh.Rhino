![Logo](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/logo128.png)
# Fesh.Rhino

 This repo contains a Rhino plugin to host [Fesh](https://github.com/goswinr/Fesh). Fesh is a fsharp scripting editor based on [AvalonEdit](https://github.com/goswinr/AvalonEditB).
 The editor supports the latest features of F# 8.0 via [Fsharp Compiler Service](https://www.nuget.org/packages/FSharp.Compiler.Service)</a>.
 It has semantic syntax highlighting, auto completion and typ info tooltips. The output window supports colored text.

Until Rhinocommon is available for .NET 7 this plugin only supports .NET Framework 4.8


The example script `LouvreAbuDhabi.fsx` in the root folder generates the axes for cladding of the Louvre Abu Dhabi.
As shown in my talk at [FSharpConf 2016](https://www.youtube.com/watch?v=ZY-bvZZZZnE)

![Screenshot](https://raw.githubusercontent.com/goswinr/Fesh.Rhino/main/Media/screen1.png)

## Getting started

you will always need to add a reference to RhinoCommon.dll


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

### License
[MIT](https://github.com/goswinr/Fesh.Rhino/blob/main/LICENSE)

