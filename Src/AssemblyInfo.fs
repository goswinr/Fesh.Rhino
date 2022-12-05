namespace Seff.Rhino.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Rhino.PlugIns

// IMPORTANT:
// Without this Guid Rhino does not remember the plugin after restart, setting <ProjectGuid> in the new SDK fsproj file does not to work.
[<assembly: Guid("01dab273-99ae-4760-8695-3f29f4887831")>] //Don't change it !! its used in Rhino.Scripting.dll via reflection to find the hosting editor.


// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[<assembly: ComVisible(false)>] 


//[<assembly: PlugInDescription(DescriptionType.Address, "Park Avenue North\r\nSeattle, WA 98103")>]
[<assembly: PlugInDescription(DescriptionType.Country, "Austria")>]
[<assembly: PlugInDescription(DescriptionType.Email, "goswin@rothenthal.com")>]
//[<assembly: PlugInDescription(DescriptionType.Phone, "206-545-6877")>]
//[<assembly: PlugInDescription(DescriptionType.Fax, "206-545-7321")>]
[<assembly: PlugInDescription(DescriptionType.Organization, "Studio Rothenthal")>]
//[<assembly: PlugInDescription(DescriptionType.UpdateUrl, "https://github.com/mcneel/RhinoPbrMaterial")>]
[<assembly: PlugInDescription(DescriptionType.WebSite, "http://seff.io/")>]
// Icons should be Windows .ico files and contain 32-bit images in the following sizes: 16, 24, 32, 48, and 256.
// This is a Rhino 6-only description.
[<assembly: PlugInDescription(DescriptionType.Icon, "Seff.Rhino.EmbeddedResources.logo.ico")>] // TODO path ok ?


// done in new SDK fsproj file:
//[<assembly: AssemblyTitle("Seff.Rhino")>]
//[<assembly: AssemblyDescription("Seff | FSharp Scripting Editor for Rhino")>]
//[<assembly: AssemblyConfiguration("")>]
//[<assembly: AssemblyCompany("Seff.io")>]
//[<assembly: AssemblyProduct("Seff.Rhino")>]
//[<assembly: AssemblyCopyright("© Copyright Goswin Rothenthal 2020")>]
//[<assembly: AssemblyTrademark("")>]
//[<assembly: AssemblyCulture("en-US")>]
//[<assembly: AssemblyVersion("0.1.*")>] // You can specify all the values or you can default the Build and Revision Numbers  by using the '*' as shown below:
//[<assembly: AssemblyFileVersion("0.1.1.0")>]

do ()
