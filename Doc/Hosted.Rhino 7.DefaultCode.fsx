#if COMPILED
namespace ???
#endif

#r "nuget: Rhino.Scripting.Extra,0.0.5"
#r @"C:\Program Files\Rhino 7\System\RhinoCommon.dll"

open System
open FsEx
open Rhino
open Rhino.Geometry
type rs = Rhino.Scripting

rs.DisableRedraw()







print "*Done!"