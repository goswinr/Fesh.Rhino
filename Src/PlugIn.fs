namespace Fesh.Rhino // Don't change name  its used in Rhino.Scripting.dll via reflection

open Rhino
open System
open Fesh
open System.Windows
open System.Net.Http

module RhCmdLn =
    let print txt  =
        RhinoApp.Write txt
        // Console.Write txt
        RhinoApp.Wait()

    let printn txt =
        RhinoApp.WriteLine txt
        // Console.WriteLine txt
        RhinoApp.Wait()

module Sync =  //Don't change name its used in Rhino.Scripting.dll via reflection
    let syncContext = System.Threading.SynchronizationContext.Current  // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable hideEditor = Action(fun() -> ())  // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable showEditor = Action(fun() -> ()) // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable isEditorVisible = new Func<bool>(fun () -> false) // Don't change name  its used in Rhino.Scripting.dll via reflection

    /// Red green blue text
    let mutable printFeshLogColor  = // Don't change name  its used in Rhino.Scripting.dll via reflection
        new Action<int,int,int,string> (fun r g b s -> RhCmdLn.print s)

    /// Red green blue text
    let mutable printnFeshLogColor  = // Don't change name  its used in Rhino.Scripting.dll via reflection
        new Action<int,int,int,string> (fun r g b s -> RhCmdLn.printn s)

    let mutable clearFeshLog = Action(fun() -> ()) // Don't change name  its used in Rhino.Scripting.dll via reflection


module internal FeshApp =

    let mutable wasShownOnce = false // having this as static member on LoadEditor fails to evaluate !! not sure why.

    let mutable editorWindow = null: Windows.Window // Not used via reflection

    let showEditorWindow(win: Windows.Window option) =
        match win with
        | Some w -> editorWindow <- w
        | None   -> ()

        if isNull editorWindow then // sets up window on first run
            RhCmdLn.printn  " * Fesh Editor Window cant be shown, the Plugin is not properly loaded. try restarting Rhino."
            Commands.Result.Failure
        else
            editorWindow.Show()
            editorWindow.Visibility <- Windows.Visibility.Visible
            if editorWindow.WindowState = Windows.WindowState.Minimized then editorWindow.WindowState <- Windows.WindowState.Normal
            wasShownOnce <- true
            Commands.Result.Success

    type Dummy = class end

    let checkForNewRelease(fesh: Fesh.Fesh) =
        async {
            try
                use client = new HttpClient()
                client.DefaultRequestHeaders.UserAgent.ParseAdd "Fesh.Rhino"
                let! response = client.GetStringAsync "https://api.github.com/repos/goswinr/Fesh.Rhino/tags" |> Async.AwaitTask
                let version = response |> Fesh.Util.Str.between "\"name\":\"" "\""
                match version with
                | None -> fesh.Log.PrintfnInfoMsg "Could not get latest version tag from https://github.com/goswinr/Fesh.Rhino/tags "
                | Some v ->
                    let cv = Reflection.Assembly.GetAssembly(typeof<Dummy>).GetName().Version.ToString()
                    let cv = if cv.EndsWith(".0") then cv[..^2] else cv
                    if v = cv then
                        fesh.Log.PrintfnInfoMsg $"You are using the latest version of Fesh for Rhino: {cv}"
                    else
                        // let url = response |> Fesh.Util.Str.between "\"url\":\"" "\""
                        // match url with
                        // | None -> fesh.Log.PrintfnInfoMsg "Could get version tag commit url from https://github.com/goswinr/Fesh.Rhino/tags "
                        // | Some url ->
                        //     let! comm = client.GetStringAsync(url) |> Async.AwaitTask
                        //     let date  = comm |> Fesh.Util.Str.between "\"date \":\"" "\""
                        //     match date with
                        //     | None -> fesh.Log.PrintfnInfoMsg $"Could not get tag commit date from {url}"
                        //     | Some date ->
                        //         match DateTime.TryParse(date) with
                        //         | false, _ -> fesh.Log.PrintfnInfoMsg $"Could not parse tag commit date from {date}"
                        //         | true, d ->
                        //             let days = (DateTime.Now - d).Days
                        //             if days > 2 then
                        //                 fesh.Log.PrintfnAppErrorMsg $"A newer version of Fesh is available: {v} , you are using {cv}"
                        //                 fesh.Log.PrintfnAppErrorMsg  "Please visit https://www.food4rhino.com/en/app/fesh"
                        //                 fesh.Log.PrintfnAppErrorMsg $"Or use the Rhino command 'PackageManager' to update Fesh. There you can also enable auto-updates."
                        //             else
                        fesh.Log.PrintfnAppErrorMsg $"A newer version of Fesh is available: {v} , you are using {cv}"
                        fesh.Log.PrintfnAppErrorMsg  "If you have auto-updates configured in the Rhino PackageManager"
                        fesh.Log.PrintfnAppErrorMsg  "it will be installed automatically after restarting Rhino."
                        fesh.Log.PrintfnAppErrorMsg  "Use the Rhino command 'PackageManager' and there the 'Installed' tab to check your settings."
                        fesh.Log.PrintfnAppErrorMsg  "Alternatively you can download it from https://www.food4rhino.com/en/app/fesh"

            with _ ->
                fesh.Log.PrintfnInfoMsg "Could not check for updates on https://api.github.com/repos/goswinr/Fesh.Rhino/tags .\r\nAre you offline?"
        }
        |> Async.Start


module internal Util =

    /// The folder of Rhino.exe, e.g. "C:/Program Files/Rhino 8/System"
    /// The RhinoCommon.dll in here is the .NET Framework build, even when Rhino is running on .NET Core.
    let rhinoSystemFolder =
        RhinoApp.GetExecutableDirectory().FullName.Replace("\\", "/")

    /// The folder of the RhinoCommon.dll that is loaded in this process.
    /// On .NET Framework that is the same as rhinoSystemFolder.
    /// On .NET Core it is its 'netcore' subfolder, e.g. "C:/Program Files/Rhino 8/System/netcore"
    /// Referencing the .NET Framework build on .NET Core fails to resolve System.Drawing.Bitmap as soon as the Rhino namespace is opened.
    /// see https://discourse.mcneel.com/t/system-drawing-bitmap-in-assembly-system-drawing/223199/2
    let rhinoCommonFolder =
        let loc = typeof<RhinoApp>.Assembly.Location
        if String.IsNullOrEmpty loc then rhinoSystemFolder // should never happen, RhinoCommon is always loaded from a file
        else IO.Path.GetDirectoryName(loc).Replace("\\", "/")

    /// The folders to resolve #r "RhinoCommon.dll" and others without a full path.
    /// rhinoCommonFolder comes first so that it wins over the .NET Framework build in rhinoSystemFolder.
    /// rhinoSystemFolder is still needed for the dlls that are shared by both runtimes, like Eto.dll
    let libFolders =
        [|
        rhinoCommonFolder
        rhinoSystemFolder
        |] |> Array.distinct

    // Fesh wil add this before:
    // "// This is your default code for new files,"
    // "// you can change it by going to the menu: File -> Edit Template File"
    // "// The default code is saved at at " + filePath0
    let defaultCode =
        [|
        // $"""#I "{rhinoCommonFolder}" """
        """#r "RhinoCommon.dll"  """
        """#r "nuget: Rhino.Scripting.FSharp" """
        """#r "nuget: ResizeArrayT" // optional"""
        ""
        """open System"""
        """open ResizeArrayT"""
        """open Rhino"""
        """open Rhino.Scripting"""
        """open Rhino.Scripting.FSharp"""
        ""
        """type rs = RhinoScriptSyntax """
        ""
        """// use the static members on 'rs' to call RhinoScript functions like in python. e.g.:"""
        """let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)"""
        ""
        "// press F5 to run the script"
        ""
        |]
        |> String.concat Environment.NewLine


module internal RhinoScripting =

    /// Rhino.Scripting remembers an Esc press even when no script is running, e.g. to deselect objects.
    /// Then rs.EscapeTest() in the next script would raise right away.
    /// So this clears that flag before each script, via reflection, because Fesh.Rhino does not reference Rhino.Scripting.
    /// The flag is the static property 'EscapePressed' of the internal class 'Rhino.Scripting.State', it exists since at least Rhino.Scripting 0.8.
    /// There can be several versions of Rhino.Scripting loaded, e.g. after resetting FSI, so this clears it in all of them.
    let resetEscapePressed () =
        for a in AppDomain.CurrentDomain.GetAssemblies() do
            if a.GetName().Name = "Rhino.Scripting" then
                try
                    match a.GetType "Rhino.Scripting.State" with
                    | null -> () // a very old Rhino.Scripting
                    | state ->
                        let flags = Reflection.BindingFlags.Public ||| Reflection.BindingFlags.NonPublic ||| Reflection.BindingFlags.Static
                        match state.GetProperty("EscapePressed", flags) with
                        | null -> ()
                        | prop -> prop.SetValue(null, false)
                with e ->
                    RhCmdLn.printn (sprintf "* Fesh.Rhino Plugin resetting Rhino.Scripting.State.EscapePressed failed with %A" e)


#if NETCOREAPP
/// On .NET Core Fesh runs FSI with --multiemit+, so FSI loads one assembly per evaluation, all named 'FSI-ASSEMBLY'
/// ('FSI-ASSEMBLY-MULTI' since FSharp.Compiler.Service 43.12, used by the net10 build), with versions that start again in each session. Later evaluations reference the earlier ones by that name.
/// FSI resolves them in its own AssemblyResolve handler, but Rhino's handler comes first and matches only the simple name:
/// it returns the first FSI-ASSEMBLY of the process. Then a value of an earlier evaluation fails with a TypeLoadException,
/// or after a reset silently is the value of an old session.
/// This handler goes first and resolves by full name among the assemblies of the current Fesh session.
/// (On .NET Framework Fesh uses --multiemit-, one assembly for all evaluations.)
module internal FsiAssemblyResolver =
    open System.Reflection
    open FSharp.Compiler.Interactive.Shell

    let mutable private getSession: unit -> FsiEvaluationSession option = fun () -> None

    let private resolve (args: ResolveEventArgs) : Assembly =
        // 'FSI-ASSEMBLY,' or 'FSI-ASSEMBLY-MULTI,', the full name is compared below
        if not (args.Name.StartsWith("FSI-ASSEMBLY", StringComparison.Ordinal)) then
            null
        else
            match getSession () with
            | None -> null
            | Some session ->
                let assemblies = session.DynamicAssemblies
                let fromThisSession =
                    match args.RequestingAssembly with
                    | null -> true
                    | requester -> assemblies |> Array.exists (fun a -> obj.ReferenceEquals(a, requester))
                if fromThisSession then assemblies |> Array.tryFind (fun a -> a.FullName = args.Name) |> Option.toObj
                else null // e.g. from an FSI session of another plugin

    let private handler = ResolveEventHandler(fun _ args -> resolve args)

    /// Puts the handler first in AppDomain.AssemblyResolve, before Rhino's.
    /// The event has no public way to insert at the front, so this sets its backing field in AssemblyLoadContext.
    let install (session: unit -> FsiEvaluationSession option) =
        getSession <- session
        match typeof<Runtime.Loader.AssemblyLoadContext>.GetField("AssemblyResolve", BindingFlags.NonPublic ||| BindingFlags.Static) with
        | null -> failwith "the field AssemblyLoadContext.AssemblyResolve was not found"
        | field ->
            lock handler (fun () ->
                let others = Delegate.Remove(field.GetValue null :?> Delegate, handler)
                field.SetValue(null, Delegate.Combine(handler, others)))
#endif


// the Plugin  and Commands Singletons:
// Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
// class. DO NOT create instances of this class yourself. It is the
// responsibility of Rhino to create an instance of this class.
// do not use "private" keyword on (singleton) constructor
type FeshPlugin () =
    inherit PlugIns.PlugIn()

    static let mutable lastDoc = RhinoDoc.ActiveDoc

    static member val RhWriter : IO.TextWriter option = Some <| new Rhino.RhinoApp.CommandLineTextWriter()

    //PlugIns.PlugInType.Utility how to set this ?

    static member val Instance = FeshPlugin() // singleton pattern needed for Rhino. http://stackoverflow.com/questions/2691565/how-to-implement-singleton-pattern-syntax

    static member val UndoRecordSerial: Option<uint32> = None with get,set

    static member val Fesh = Unchecked.defaultof<Fesh> with get,set

    static member BeforeEval () =
        // Not inside the async below, that is posted to the UI thread and might only run once the script is running already.
        RhinoScripting.resetEscapePressed()
        async{
            do! Async.SwitchToContext Sync.syncContext
            lastDoc <- RhinoDoc.ActiveDoc
            FeshPlugin.UndoRecordSerial <-
                if isNull lastDoc then None
                else
                    let serial = lastDoc.BeginUndoRecord "F# script run by Fesh.Rhino"
                    if serial = 0u then None else Some serial
        }
        |> Async.StartImmediate // fails :Async.RunSynchronously

    static member AfterEval (showWin) : unit =
        async{
            do! Async.SwitchToContext Sync.syncContext
            let doc = RhinoDoc.ActiveDoc
            let undoRecord = FeshPlugin.UndoRecordSerial
            FeshPlugin.UndoRecordSerial <- None // consume once, even if the script changed documents

            if not (isNull doc) then
                match undoRecord with
                | Some serial when lastDoc = doc ->
                    if not <| doc.EndUndoRecord(serial) then
                        RhCmdLn.printn " * Fesh.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord"
                        eprintfn " * Fesh.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord(FeshPlugin.UndoRecordSerial:%d)" serial
                | _ -> ()

                doc.Views.RedrawEnabled <- true
                doc.Views.Redraw()
            if showWin then Sync.showEditor.Invoke() //Action //because it might crash during UI interaction where it is hidden
        }
        |> Async.StartImmediate // fails :Async.RunSynchronously



    member this.WhenLoading(refErrs:byref<string>): PlugIns.LoadReturnCode  =
        RhCmdLn.printn  "loading Fesh.Rhino Plugin ..."
        try
            let canRun () = not <| Rhino.Commands.Command.InCommand()
            let feshHost =
                #if DEBUG
                    "RhinoDebug"
                #else
                    "Rhino"
                #endif

            let hostData : Fesh.Config.HostedStartUpData = {
                hostName = feshHost
                mainWindowHandel = RhinoApp.MainWindowHandle()
                fsiCanRun = canRun
                defaultCode = Some Util.defaultCode
                // Add the Icon at the top left of the window and in the status bar, musst be called  after loading window.
                // Media/LogoCursorTr.ico with Build action : "Resource"
                // (for the exe file icon in explorer use <Win32Resource>Media\logo.res</Win32Resource>  in fsproj )
                logo = Some (Uri "pack://application:,,,/Fesh.Rhino;component/Media/logo.ico")
                hostAssembly = Some (Reflection.Assembly.GetAssembly typeof<FeshPlugin>)
                canRunAsync = true // FSI can run async, so that it does not block the UI thread.
                libFolders = Util.libFolders // so that #r "RhinoCommon.dll" resolves without a full path
                }

            let fesh:Fesh = Fesh.App.createEditorForHosting hostData
            FeshPlugin.Fesh <- fesh

            #if NETCOREAPP
            try FsiAssemblyResolver.install (fun () -> fesh.Fsi.Session)
            with e -> RhCmdLn.printn $" * Fesh.Rhino | values of earlier evaluations might not be found, setting up the resolver for FSI assemblies failed: {e.Message}"
            #endif

            fesh.Window.Loaded.Add (fun _ ->

                Sync.showEditor      <- Action(fun () -> fesh.Window.Show())
                Sync.hideEditor      <- Action(fun () -> fesh.Window.Hide())
                Sync.isEditorVisible <- new Func<bool>(fun () ->
                    // originally : fesh.Window.Visibility = Windows.Visibility.Visible but
                    // this might also show invisible if at the time of calling another window is covering rhino.
                    // then going back to rhino the ui prompt might not be visible because the window would be in front again.
                    // so we have to check if it is minimized too:
                    fesh.Window.Visibility = Windows.Visibility.Visible
                    &&
                    match fesh.Window.WindowState with
                    | Windows.WindowState.Minimized                                     -> false
                    | Windows.WindowState.Normal  | Windows.WindowState.Maximized | _   -> true
                    )

                Sync.printFeshLogColor  <- new Action<int,int,int,string> (fun r g b s -> fesh.Log.AvalonLog.AppendWithColor(r,g,b,s))
                Sync.printnFeshLogColor <- new Action<int,int,int,string> (fun r g b s -> fesh.Log.AvalonLog.AppendLineWithColor(r,g,b,s))
                Sync.clearFeshLog       <- Action(fun () -> fesh.Log.AvalonLog.Clear())

                async {
                    // Reinitialize Rhino.Scripting just in case it is loaded already in the current AppDomain by another plugin.
                    // This is needed to have showEditor() and hideEditor() actions for Fesh setup correctly.
                    // let assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    // let loadedFsCoreVersion = assemblies |> Seq.tryFind (fun a -> a.GetName().Name = "Fsharp.Core") |> Option.map (fun a -> a.GetName().Version.ToString() )
                    let assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    assemblies
                    |> Seq.tryFind (fun a -> a.GetName().Name = "Rhino.Scripting")
                    |> Option.iter (fun rsAss ->
                        try
                            // 'initialize' is a private static field of the class RhinoSync,
                            // which is in the namespace Rhino.Scripting since Rhino.Scripting 0.7, and in Rhino before.
                            let rhinoSync =
                                match rsAss.GetType "Rhino.Scripting.RhinoSync" with
                                | null -> rsAss.GetType("Rhino.RhinoSync", true)
                                | t -> t
                            let flags = Reflection.BindingFlags.NonPublic ||| Reflection.BindingFlags.Static
                            let init = rhinoSync.GetField("initialize", flags).GetValue null :?> Action
                            init.Invoke()
                            RhCmdLn.printn "Rhino.Scripting.RhinoSync re-initialized."
                        with e ->
                            RhCmdLn.printn (sprintf "* Fesh.Rhino Plugin Rhino.Scripting.Initialize() failed with %A" e)
                        )
                    } |> Async.Start
                )

            // Could be used to keep everything alive: But then you would be asked twice to save unsaved files. On Closing Fesh and closing Rhino.
            fesh.Window.Closing.Add (fun e ->
                if not e.Cancel then // closing might be already cancelled in Fesh.fs as a result of asking to save unsaved files.
                    // even if closing is not canceled, don't close, just hide window
                    fesh.Window.Visibility <- Windows.Visibility.Hidden
                    // Closed does not fire when closing is canceled below.
                    // Keep temporary hides during script UI interaction separate from closing the editor.
                    Console.SetOut   FeshPlugin.RhWriter.Value
                    Console.SetError FeshPlugin.RhWriter.Value
                    e.Cancel <- true
                    )

            fesh.Window.StateChanged.Add (fun e ->
                match fesh.Fsi.State with
                | Ready ->
                    // if the window is hidden log error messages to rhino command line, but not when window is shown
                    // this is also set in FeshRunCurrentScript Command
                    match fesh.Window.WindowState with
                    | Windows.WindowState.Normal    | Windows.WindowState.Maximized    -> fesh.Log.AdditionalLogger <- None
                    | Windows.WindowState.Minimized | _                                -> fesh.Log.AdditionalLogger <- FeshPlugin.RhWriter

                | Initializing | NotLoaded | Evaluating | Compiling -> ()   // don't change while running
                )


            // Redirect Console output to Fesh log when Fesh window gets
            // Because otherwise it would go to Rhino command line because Rhino Python editor redirects Console.Out and Console.Error there.
            fesh.Window.GotFocus.Add(fun _ ->
                let l = fesh.Log
                Console.SetOut   l.TextWriterConsoleOut
                Console.SetError l.TextWriterConsoleError
                )

            fesh.Fsi.OnCompiling.Add    ( fun m -> FeshPlugin.BeforeEval())    // https://github.com/mcneel/rhinocommon/blob/57c3967e33d18205efbe6a14db488319c276cbee/dotnet/rhino/rhinosdkdoc.cs#L857
            fesh.Fsi.OnRuntimeError.Add ( fun e -> FeshPlugin.AfterEval true)  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
            fesh.Fsi.OnFsiEvalError.Add ( fun e -> FeshPlugin.AfterEval true)  // parser errors can be returned as diagnostics without a runtime exception
            fesh.Fsi.OnCanceled.Add     ( fun m -> FeshPlugin.AfterEval true)  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
            fesh.Fsi.OnCompletedOk.Add  ( fun m -> FeshPlugin.AfterEval false) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin = false because might be running in background mode from rhino command line

            //RhinoDoc.CloseDocument.Add (fun e -> fesh.Fsi.CancelIfAsync() ) // don't do that !! Allow rs.Command to open new files when called async.

            // TODO make sure that the referenced RhinoCommen is the same one as running, e.g ther might be Rhino 8 and Rhino 9 WIP in use.
            // fesh.Fsi.OnCompiling.Add ( fun m ->
            //     let tx = m.editor.

            RhinoApp.Closing.Add (fun _ ->
                fesh.Tabs.AskForFileSavingToKnowIfClosingWindowIsOk() |> ignore // to save unsaved files, canceling of closing not possible here, save dialog will show after rhino is closed
                fesh.Fsi.AskIfCancellingIsOk() |> ignore
                fesh.Fsi.CancelIfAsync()   //sync eval gets canceled anyway
                )

            // A first Dummy attachment in sync mode to prevent access violation exception if first access is in async mode from Rhino.Scripting dll
            // Don't abort on esc, only on ctrl+break or Rhino.Scripting.EscapeTest()
            RhinoApp.EscapeKeyPressed.Add(ignore)

            // Add an Alias too if not taken already:
            if not <| ApplicationSettings.CommandAliasList.IsAlias "fr" then
                if ApplicationSettings.CommandAliasList.Add("fr","FeshRunCurrentScript")then
                    RhCmdLn.printn  "* Fesh.Rhino Plugin added the command alias 'fr' for 'FeshRunCurrentScript'"


            // only now load and show the window:

            RhCmdLn.printn  ("Fesh."+feshHost + " plugin loaded.")
            match FeshApp.showEditorWindow(Some fesh.Window) with
            | Commands.Result.Success ->
                FeshApp.checkForNewRelease fesh
                PlugIns.LoadReturnCode.Success
            | _   ->
                PlugIns.LoadReturnCode.ErrorShowDialog
        with
        | e ->
            let errMsg =
                [|
                "Fesh.Rhino Plugin failed to load."
                "Try to restart Rhino! That is often enough to make it work!"
                "If you still have problems,"
                "and you have other plugins loaded that are using older versions of 'Fsharp.Core'"
                "try to unload or disable them."
                |] |> String.concat Environment.NewLine
            refErrs <- errMsg
            RhCmdLn.printn e.Message
            RhCmdLn.printn errMsg
            PlugIns.LoadReturnCode.ErrorShowDialog




    override this.OnLoad(refErrs) : PlugIns.LoadReturnCode =
        AssemblyInfo.track()

        if not Runtime.HostUtils.RunningOnWindows then
            let errMsg = " * The Fesh.Rhino Scripting-Editor-For-F# PlugIn only works on Windows, not Mac.\r\nIt depends on the WPF framework "
            refErrs <- errMsg
            RhCmdLn.printn errMsg
            PlugIns.LoadReturnCode.ErrorShowDialog
        else

            let frameworkDescription = Runtime.InteropServices.RuntimeInformation.FrameworkDescription

        #if NET10
            if Environment.Version.Major < 10 then // .NET Framework has major version 4 here
                MessageBox.Show(
                    [|
                        $"The loaded Fesh.Rhino Plugin is compiled for .NET 10 but Rhino is running on {frameworkDescription}"
                        "You can use the Rhino Command 'SetDotNetRuntime' to change Rhino's runtime to .NET 10"
                    |] |> String.concat Environment.NewLine,
                    "Fesh.Rhino Plugin | .NET 10 needed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning)
                |> ignore
                PlugIns.LoadReturnCode.ErrorNoDialog
        #else
        #if NET8
            if frameworkDescription.StartsWith ".NET Framework" then
                MessageBox.Show(
                    [|
                        $"The loaded Fesh.Rhino Plugin is compiled for .NET core 8.0 but Rhino is running on {frameworkDescription}"
                        "You can use the Rhino Command 'SetDotNetRuntime' to change Rhino's runtime to .NET core 8.0"
                    |] |> String.concat Environment.NewLine,
                    "Fesh.Rhino Plugin | .NETcore 8.0 needed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning)
                |> ignore
                PlugIns.LoadReturnCode.ErrorNoDialog
            elif frameworkDescription.StartsWith ".NET 7" then
                MessageBox.Show(
                    [|
                        $"The loaded Fesh.Rhino Plugin is compiled for .NET core 8.0 but Rhino is running on {frameworkDescription}"
                        "You can use the Rhino Command 'SetDotNetRuntime' to change Rhino's runtime to .NET core 8.0"
                    |] |> String.concat Environment.NewLine,
                    "Fesh.Rhino Plugin | .NETcore 8.0 needed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning)
                |> ignore
                PlugIns.LoadReturnCode.ErrorNoDialog
        #else
            if not <| frameworkDescription.StartsWith ".NET Framework" then
                // Command: SetDotNetRuntime
                // Currently running in .NET 7.0.7
                // Select .NET Runtime ( Runtime=NETFramework  NetCoreVersion=v7 ): Runtime
                // Runtime <NETFramework> ( NETCore  NETFramework ): NETFramework
                // Select .NET Runtime ( Runtime=NETFramework  NetCoreVersion=v7 )
                MessageBox.Show(
                    [|
                        $"Rhino is running on {frameworkDescription}"
                        "But only .NET Framework 4.8 and .NET core 8.0 are supported by the Fesh.Rhino Plugin."
                        "You can use the Rhino Command 'SetDotNetRuntime' to change Rhino's .NET runtime."
                        ".NET core 8.0 is recommended."
                    |] |> String.concat Environment.NewLine,
                    "Fesh.Rhino Plugin | Choose another .NET runtime",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning)
                |> ignore
                PlugIns.LoadReturnCode.ErrorNoDialog
        #endif
        #endif

            else
                // proceed with loading
                this.WhenLoading(&refErrs)




    //override this.LoadAtStartup = true //obsolete? load FSI already at Rhino startup ??

    // You can override methods here to change the plug-in behavior on
    // loading and shut down, add options pages to the Rhino _Option command
    // and maintain plug-in wide options in a document.
    //override this.CreateCommands() = //to add script files as custom commands
        // https://discourse.mcneel.com/t/how-to-create-commands-after-plugin-load/47833

        //base.CreateCommands()
        // then call base.RegisterCommand()
        // or ? Rhino.Runtime.HostUtils.RegisterDynamicCommand(feshPlugin,command)

        //for file in commandsFiles do
        // let cmd `= create instance of command derived class
        //    HostUtils.RegisterDynamicCommand(this,cmd)

        (*
                protected override void CreateCommands()
        {
          base.CreateCommands();
          var resource_names = Assembly.GetManifestResourceNames();
          foreach (var name in resource_names)
          {
            if (!name.EndsWith(".py", StringComparison.InvariantCulture))
              continue;
            var start = name.LastIndexOf(".", name.Length - ".py".Length - 1, StringComparison.CurrentCultureIgnoreCase) + 1;
            var english_name = name.Substring(start, name.Length - ".py".Length - start);
            Rhino.Commands.Command cmd = english_name.StartsWith("Test", StringComparison.InvariantCulture) ?
              new PythonTestCommand(english_name, name) :
              new PythonCommand(english_name, name);
            Rhino.Runtime.HostUtils.RegisterDynamicCommand(this, cmd);
          }
        }
        *)
