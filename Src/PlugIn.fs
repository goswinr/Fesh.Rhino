namespace Fesh.Rhino // Don't change name  its used in Rhino.Scripting.dll via reflection

open Rhino
open System
open Fesh
open System.Windows
open System.Net.Http

//open System.Drawing // fot net 7


module RhCmdAndConsole =
    let print txt  =
        RhinoApp.Write txt
        Console.Write txt
        RhinoApp.Wait()

    let printn txt =
        RhinoApp.WriteLine txt
        Console.WriteLine txt
        RhinoApp.Wait()

module Sync =  //Don't change name its used in Rhino.Scripting.dll via reflection
    let syncContext = System.Threading.SynchronizationContext.Current  // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable hideEditor = Action<unit>(fun () ->())  // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable showEditor = Action<unit>(fun () ->())// Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable isEditorVisible = new Func<bool>(fun () -> false) // Don't change name  its used in Rhino.Scripting.dll via reflection

    let mutable editorWindow = null: Windows.Window // Not used via reflection

    /// Red green blue text
    let mutable printFeshLogColor  = // Don't change name  its used in Rhino.Scripting.dll via reflection
        new Action<int,int,int,string> (fun r g b s -> RhCmdAndConsole.print s)

    /// Red green blue text
    let mutable printnFeshLogColor  = // Don't change name  its used in Rhino.Scripting.dll via reflection
        new Action<int,int,int,string> (fun r g b s -> RhCmdAndConsole.printn s)

    let mutable clearFeshLog = // Don't change name  its used in Rhino.Scripting.dll via reflection
        Action<unit>(fun () ->())



module State =
    let mutable ShownOnce = false // having this as static member on LoadEditor fails to evaluate !! not sure why.


module internal FeshApp =

    let showEditor() =
        if isNull Sync.editorWindow then // sets up window on first run
            RhCmdAndConsole.printn  " * Fesh Editor Window cant be shown, the Plugin is not properly loaded. try restarting Rhino."
            Commands.Result.Failure
        else
            Sync.editorWindow.Show()
            Sync.editorWindow.Visibility <- Windows.Visibility.Visible
            if Sync.editorWindow.WindowState = Windows.WindowState.Minimized then Sync.editorWindow.WindowState <- Windows.WindowState.Normal
            State.ShownOnce <- true
            Commands.Result.Success

    type Dummy = class end


    let checkForNewRelease(fesh: Fesh.Fesh) =
        async {
            try
                use client = new HttpClient()
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Fesh.Rhino")
                let! response = client.GetStringAsync("https://api.github.com/repos/goswinr/Fesh.Rhino/tags") |> Async.AwaitTask
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

    // Fesh wil add this before:
    // "// This is your default code for new files,"
    // "// you can change it by going to the menu: File -> Edit Template File"
    // "// The default code is saved at at " + filePath0
    let defaultCode =
        [|
        """#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" """
        """#r "nuget:Rhino.Scripting.FSharp" """
        ""
        """open System"""
        """open Rhino.Scripting"""
        """open Rhino.Scripting.FSharp //recommended for F# """
        ""
        """type rs = RhinoScriptSyntax"""
        ""
        """// use the rs object to call RhinoScript functions like in python"""
        """let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)"""
        ""
        |]
        |> String.concat Environment.NewLine

    // let requestedFsCoreVersion = "8.0.400"

    // // insert just before the last </runtime> tag in Rhino.exe.config
    // let bindingRedirect(version:string) = $"""
    //     <!-- binding redirect added automatically by Rhino.Fesh plugin: -->
    //     <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    //         <dependentAssembly>
    //             <assemblyIdentity name="FSharp.Core" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
    //             <bindingRedirect oldVersion="0.0.0.0-{version}" newVersion="{version}" />
    //         </dependentAssembly>
    //     </assemblyBinding>
    // """


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
        async{
            do! Async.SwitchToContext Sync.syncContext
            lastDoc <- RhinoDoc.ActiveDoc
            FeshPlugin.UndoRecordSerial <- Some (RhinoDoc.ActiveDoc.BeginUndoRecord "F# script run by Fesh.Rhino")
        }
        |> Async.StartImmediate // fails :Async.RunSynchronously

    static member AfterEval (showWin) : unit =
        async{
            do! Async.SwitchToContext Sync.syncContext
            //if FeshPlugin.UndoRecordSerial <> 0u then

            if lastDoc = RhinoDoc.ActiveDoc then // it might have changed during script run
                match FeshPlugin.UndoRecordSerial with
                | None -> ()
                | Some serial ->
                    FeshPlugin.UndoRecordSerial <- None // so a record is only eneded once
                    if not <| RhinoDoc.ActiveDoc.EndUndoRecord(serial) then
                        RhCmdAndConsole.printn " * Fesh.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord"
                        eprintfn " * Fesh.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord(FeshPlugin.UndoRecordSerial:%d)" serial


            RhinoDoc.ActiveDoc.Views.RedrawEnabled <- true
            RhinoDoc.ActiveDoc.Views.Redraw()
            if showWin && not (isNull Sync.showEditor) then Sync.showEditor.Invoke() //because it might crash during UI interaction where it is hidden
        }
        |> Async.StartImmediate // fails :Async.RunSynchronously


    override this.OnLoad(refErrs) : PlugIns.LoadReturnCode =
        AssemblyInfo.track()
        let assemblies = AppDomain.CurrentDomain.GetAssemblies()

        // let loadedFsCoreVersion = assemblies |> Seq.tryFind (fun a -> a.GetName().Name = "Fsharp.Core") |> Option.map (fun a -> a.GetName().Version.ToString() )

        if not Runtime.HostUtils.RunningOnWindows then
            let errMsg = " * The Fesh.Rhino Scripting-Editor-For-F# PlugIn only works on Windows, not Mac.\r\nIt depends on the WPF framework "
            refErrs <- errMsg
            RhCmdAndConsole.printn errMsg
            PlugIns.LoadReturnCode.ErrorShowDialog

        elif not <| Runtime.InteropServices.RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework") then

            // Command: SetDotNetRuntime
            // Currently running in .NET 7.0.7
            // Select .NET Runtime ( Runtime=NETFramework  NetCoreVersion=v7 ): Runtime
            // Runtime <NETFramework> ( NETCore  NETFramework ): NETFramework
            // Select .NET Runtime ( Runtime=NETFramework  NetCoreVersion=v7 )
            MessageBox.Show(
                [|
                    "The Fesh.Rhino Plugin currently only works well with.NET Framework."
                    "A RhinoCommon nuget targeting .NET 7  is not available yet."
                    "It might crash with .NET 7"
                    "Please use the Rhino Command 'SetDotNetRuntime' to change to .NET Framework."   |] |> String.concat Environment.NewLine,
                "Fesh.Rhino Plugin | .NET Framework needed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning)
            |> ignore
            PlugIns.LoadReturnCode.ErrorNoDialog

        // elif loadedFsCoreVersion.IsSome && Util.requestedFsCoreVersion <> loadedFsCoreVersion.Value then // another version of Fsharp.Core is loaded
        //     let errMsg =
        //         $"The Fesh.Rhino Plugin needs Fsharp.Core version {Util.requestedFsCoreVersion}, but found version " + loadedFsCoreVersion.Value +
        //         "\r\nYou might have already another plugin loaded using an older version of Fsharp.Core." +
        //         "\r\nPlease unload the other plugin or update it to use Fsharp.Core version {requestedFsCoreVersion}." +
        //         "\r\nOr add a binding redirect to Rhino.exe.config. see:" +
        //         "\r\nhttps://github.com/goswinr/Fesh.Rhino/issues/2"
        //     refErrs <- errMsg
        //     RhinoAppWriteLine.printn errMsg
        //     PlugIns.LoadReturnCode.ErrorShowDialog

        else
            RhCmdAndConsole.printn  "loading Fesh.Rhino Plugin ..."
            try
                let canRun () = not <| Rhino.Commands.Command.InCommand()
                let host =
                    #if DEBUG
                        "RhinoDebug"
                    #else
                        "Rhino" //The command name as it appears on the Rhino command line.
                    #endif

                let hostData : Fesh.Config.HostedStartUpData = {
                    hostName = host
                    mainWindowHandel = RhinoApp.MainWindowHandle()
                    fsiCanRun = canRun
                    defaultCode = Some Util.defaultCode
                    // Add the Icon at the top left of the window and in the status bar, musst be called  after loading window.
                    // Media/LogoCursorTr.ico with Build action : "Resource"
                    // (for the exe file icon in explorer use <Win32Resource>Media\logo.res</Win32Resource>  in fsproj )
                    logo = Some (Uri "pack://application:,,,/Fesh.Rhino;component/Media/logo.ico")
                    hostAssembly = Some (Reflection.Assembly.GetAssembly typeof<FeshPlugin>)
                    canRunAsync = true // FSI can run async, so that it does not block the UI thread.
                    }

                let fesh:Fesh = Fesh.App.createEditorForHosting hostData
                FeshPlugin.Fesh <- fesh
                Sync.showEditor <- new Action<unit>(fun () -> fesh.Window.Show())
                Sync.hideEditor <- new Action<unit>(fun () -> fesh.Window.Hide())
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

                Sync.editorWindow       <- fesh.Window :> Windows.Window
                Sync.printFeshLogColor  <- new Action<int,int,int,string> (fun r g b s -> fesh.Log.AvalonLog.AppendWithColor(r,g,b,s))
                Sync.printnFeshLogColor <- new Action<int,int,int,string> (fun r g b s -> fesh.Log.AvalonLog.AppendLineWithColor(r,g,b,s))
                Sync.clearFeshLog       <- new Action<unit>(fun () ->fesh.Log.AvalonLog.Clear())

                // Could be used to keep everything alive: But then you would be asked twice to save unsaved files. On Closing Fesh and closing Rhino.
                fesh.Window.Closing.Add (fun e ->
                    if not e.Cancel then // closing might be already cancelled in Fesh.fs as a result of asking to save unsaved files.
                        // even if closing is not canceled, don't close, just hide window
                        fesh.Window.Visibility <- Windows.Visibility.Hidden
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


                fesh.Fsi.OnCompiling.Add    ( fun m -> FeshPlugin.BeforeEval())     // https://github.com/mcneel/rhinocommon/blob/57c3967e33d18205efbe6a14db488319c276cbee/dotnet/rhino/rhinosdkdoc.cs#L857
                fesh.Fsi.OnRuntimeError.Add ( fun e -> FeshPlugin.AfterEval true)  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
                fesh.Fsi.OnCanceled.Add     ( fun m -> FeshPlugin.AfterEval true)  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
                fesh.Fsi.OnCompletedOk.Add  ( fun m -> FeshPlugin.AfterEval false) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin = false because might be running in background mode from rhino command line

                //RhinoDoc.CloseDocument.Add (fun e -> fesh.Fsi.CancelIfAsync() ) // don't do that !! Allow rs.Command to open new files when called async.

                RhinoApp.Closing.Add (fun _ ->
                    fesh.Tabs.AskForFileSavingToKnowIfClosingWindowIsOk() |> ignore // to save unsaved files, canceling of closing not possible here, save dialog will show after rhino is closed
                    fesh.Fsi.AskIfCancellingIsOk() |> ignore
                    fesh.Fsi.CancelIfAsync()   //sync eval gets canceled anyway
                    )

                // Dummy attachment in sync mode  to prevent access violation exception if first access is in async mode
                // Don't abort on esc, only on ctrl+break or Rhino.Scripting.EscapeTest()
                RhinoApp.EscapeKeyPressed.Add(ignore)

                // Add an Alias too if not taken already:
                if not <| ApplicationSettings.CommandAliasList.IsAlias("fr") then
                    if ApplicationSettings.CommandAliasList.Add("fr","FeshRunCurrentScript")then
                        RhCmdAndConsole.printn  "* Fesh.Rhino Plugin added the command alias 'fr' for 'FeshRunCurrentScript'"

                // Reinitialize Rhino.Scripting just in case it is loaded already in the current AppDomain by another plugin.
                // This is needed to have showEditor() and hideEditor() actions for Fesh setup correctly.
                assemblies
                |> Seq.tryFind (fun a -> a.GetName().Name = "Rhino.Scripting")
                |> Option.iter (fun rsAss ->
                    try
                        let rhinoSyncModule = rsAss.GetType("Rhino.RhinoSync")
                        let init = rhinoSyncModule.GetProperty("initialize").GetValue rsAss :?> Action
                        init.Invoke()
                        RhCmdAndConsole.printn "Rhino.Scripting.RhinoSync re-initialized."
                    with e ->
                        RhCmdAndConsole.printn (sprintf "* Fesh.Rhino Plugin Rhino.Scripting.Initialize() failed with %A" e)
                    )

                RhCmdAndConsole.printn  ("Fesh."+host + " plugin loaded.")
                match FeshApp.showEditor() with
                | Commands.Result.Success ->
                    FeshApp.checkForNewRelease fesh
                    PlugIns.LoadReturnCode.Success
                | _                       -> PlugIns.LoadReturnCode.ErrorShowDialog
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
                RhCmdAndConsole.printn e.Message
                RhCmdAndConsole.printn errMsg
                PlugIns.LoadReturnCode.ErrorShowDialog




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
