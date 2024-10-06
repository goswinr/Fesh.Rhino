let pattern = 
    [|
        [|  0,3 ;  -2,2 ;  -3,0 ;  -2,-2 ;  0,-3 ;  2,-2 ;  3,0 ;  2,2 |]
        [|  0,3 ;  2,2 ;  2,4 |]
        [|  2,2 ;  3,0 ;  4,2 |]
        [|  2,4 ;  4,4 ;  3,6 |]
        [|  4,2 ;  6,3 ;  4,4 |]
        [|  2,2 ;  4,2 ;  4,4 ;  2,4 |]
    |]
let map2 f = Array.map <| Array.map f

#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" 
//#r "nuget:Rhino.Scripting.Fsharp, 0.8.1"  
open Rhino.Geometry

let points = pattern |> map2 (fun (u,v) -> Point3d(float u, float v, 0.0)) 
let edges  = 
    [| for es in points do 
        yield [|for i = 0 to es.Length - 2 do yield Line(es.[i] , es.[i+1] )
                yield Line(es.[es.Length-1] , es.[0] ) |] |] // connect last to first 

// draw one
let doc = Rhino.RhinoDoc.ActiveDoc  
edges |> map2 doc.Objects.AddLine |> ignore
doc.Views.Redraw()

// draw grid
let shiftLine (offX, offY) (l:Line)  =  
    Line(   l.FromX + offX, l.FromY + offY , 0.0 ,  
            l.ToX   + offX, l.ToY   + offY , 0.0 )
let uvs = 
    let ext = 6 * 8// step size 6*7, max 19 at 0.013 radians
    [| for u in -ext..6..ext do 
        for v in  -ext..6..ext do
            yield float u, float v |]
            
let edgesShifted =  [| for uv in uvs do yield! edges |> map2 (shiftLine uv) |]

edgesShifted |> map2 doc.Objects.AddLine |> ignore
doc.Views.Redraw()

// draw sphere
let setToSphere (pt:Point3d) =
    let uRad = pt.X * 0.014  //to get angle in Radians
    let vRad = pt.Y * 0.014
    let x = sin uRad / cos uRad
    let y = sin vRad / cos vRad
    // Radius of spheere 
    // inverse length to get scaling factor for this vector:
    let f = 65. / sqrt (x*x + y*y + 1.) 
    Point3d(x*f, y*f, f) 

let setLineToSphere (l:Line) = Line (setToSphere l.From , setToSphere l.To )

edgesShifted |> map2 setLineToSphere|> map2 doc.Objects.AddLine |> ignore
doc.Views.Redraw()

edgesShifted





