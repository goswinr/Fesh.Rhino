# Fesh.Rhino

 This repo contains a <a href="https://www.autodesk.com/products/revit/overview" target="_blank">Rhino</a> plugin to host <a href="https://github.com/goswinr/Fesh" target="_blank">Fesh</a>. Fesh is a fsharp scripting editor based on <a href="https://github.com/goswinr/AvalonEditB" target="_blank">AvalonEdit</a>. The editor supports the latest features of F# 8.0 via <a href="https://www.nuget.org/packages/FSharp.Compiler.Service/43.8.300" target="_blank">FCS 430.0.0</a>. It has semantic syntax highlighting, auto completion and typ info tooltips. The output window supports colored text.


Untill Rhinocommon is avaailable for .NET 7 this plugin only supports .NET Framework 4.8

![](Docs/screen1.png)
The example script in the root folder generates the axes for cladding of the Louvre Abu Dhabi.
As shown in my talk at <a href="https://www.youtube.com/watch?v=ZY-bvZZZZnE" target="_blank">FSharpConf 2016</a>



## Getting started

you will always ned ar reference to the RhinoCommon.dll


```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" // or where ever you have it
```

If you are used to doing Rhino Scripting with PythonI recommend using the [Rhino.Scripting nuget package](https://nuget.org/packages/Rhino.Scripting) to have the same 900 functions available that tt.


```fsharp
#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll"
#r "nuget:Rhino.Scripting,  0.7.0"

open System
open Rhino.Scripting

type rs = RhinoScriptSyntax


let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)
```

### License
[MIT](https://github.com/goswinr/Fesh.Rhino/blob/main/LICENSE)

