namespace Seff.Rhino.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices


// IMPORTANT:
// Without this Guid Rhino does not remeber the plugin after restart, setting <ProjectGuid> in the fsproj file does not seem to work.
[<assembly: Guid("01dab273-99ae-4760-8695-3f29f4887831")>] 
//System.Guid.NewGuid() //for fsi




// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[<assembly: ComVisible(false)>]






// done in fsproj file:
//[<assembly: AssemblyTitle("Etos.Rhino")>]
//[<assembly: AssemblyDescription("FSharp Scriting Editor for Rhino")>]
//[<assembly: AssemblyConfiguration("")>]
//[<assembly: AssemblyCompany("Etos.io")>]
//[<assembly: AssemblyProduct("Etos.Rhino")>]
//[<assembly: AssemblyCopyright("© Copyright Goswin R 2017")>]
//[<assembly: AssemblyTrademark("")>]
//[<assembly: AssemblyCulture("en-US")>]
//[<assembly: AssemblyVersion("0.1.*")>] // You can specify all the values or you can default the Build and Revision Numbers  by using the '*' as shown below:
//[<assembly: AssemblyFileVersion("0.1.1.0")>]

do ()