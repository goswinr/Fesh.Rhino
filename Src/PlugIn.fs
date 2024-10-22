﻿namespace Fesh.Rhino // Don't change name  its used in Rhino.Scripting.dll via reflection

open Rhino
open System
open Fesh
open System.Windows

//open System.Drawing // fot net 7


module Sync =  //Don't change name its used in Rhino.Scripting.dll via reflection
    let syncContext = Threading.SynchronizationContext.Current  // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable hideEditor = null: Action // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable showEditor = null: Action // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable isEditorVisible = null: Func<bool> // Don't change name  its used in Rhino.Scripting.dll via reflection

    let mutable editorWindow = null: Windows.Window // Not used via reflection


module RhinoAppWriteLine =
    let print txt  = RhinoApp.WriteLine txt  ; RhinoApp.Wait()
    let print2 txt1 txt2 = RhinoApp.WriteLine (txt1+txt2); RhinoApp.Wait()

module State =
    let mutable ShownOnce = false // having this as static member on LoadEditor fails to evaluate !! not sure why.



module internal App =
    let showEditor() =
        if isNull Sync.editorWindow then // set up window on first run
            RhinoAppWriteLine.print  " * Fesh Editor Window cant be shown, the Plugin is not properly loaded. try restarting Rhino."
            Commands.Result.Failure
        else
            Sync.editorWindow.Show()
            Sync.editorWindow.Visibility <- Windows.Visibility.Visible
            if Sync.editorWindow.WindowState = Windows.WindowState.Minimized then Sync.editorWindow.WindowState <- Windows.WindowState.Normal
            State.ShownOnce <- true
            Commands.Result.Success


module internal Util =

    // Fesh wil add this before:
    // "// This is your default code for new files,"
    // "// you can change it by going to the menu: File -> Edit Template File"
    // "// The default code is saved at at " + filePath0
    let defaultCode =
        [|
        """#r "C:/Program Files/Rhino 8/System/RhinoCommon.dll" """
        """#r "nuget:Rhino.Scripting.Fsharp"  """
        ""
        """open System"""
        """open Rhino.Scripting"""
        """open Rhino.Scripting.Fsharp //recommended for F# """
        """open FsEx // part of Rhino.Scripting"""
        ""
        """type rs = RhinoScriptSyntax"""
        ""
        """// use the rs object to call RhinoScript functions like in python"""
        """let crv = rs.GetObject("Select a curve",  rs.Filter.Curve)"""
        ""
        |]
        |> String.concat Environment.NewLine

    let requestedFsCoreVersion = "8.0.400"

    // insert just before the last </runtime> tag in Rhino.exe.config
    let bindingRedirect(version:string) = $"""
        <!-- binding redirect added automatically by Rhino.Fesh plugin: -->
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
            <dependentAssembly>
                <assemblyIdentity name="FSharp.Core" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
                <bindingRedirect oldVersion="0.0.0.0-{version}" newVersion="{version}" />
            </dependentAssembly>
        </assemblyBinding>
    """


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

    static member val UndoRecordSerial = 0u with get,set

    static member val Fesh = Unchecked.defaultof<Fesh> with get,set

    static member BeforeEval () =
            async{
                do! Async.SwitchToContext Sync.syncContext
                lastDoc <- RhinoDoc.ActiveDoc
                FeshPlugin.UndoRecordSerial <- RhinoDoc.ActiveDoc.BeginUndoRecord "F# script run by Fesh.Rhino"
                } |> Async.StartImmediate

    static member AfterEval (showWin) : unit =
        async{
            do! Async.SwitchToContext Sync.syncContext
            //if FeshPlugin.UndoRecordSerial <> 0u then

            if lastDoc = RhinoDoc.ActiveDoc then // it might have changed during script run
                if not <| RhinoDoc.ActiveDoc.EndUndoRecord(FeshPlugin.UndoRecordSerial) then
                    RhinoAppWriteLine.print " * Fesh.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord"
                    eprintfn " * Fesh.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord(FeshPlugin.UndoRecordSerial:%d)" FeshPlugin.UndoRecordSerial

            RhinoDoc.ActiveDoc.Views.RedrawEnabled <- true
            RhinoDoc.ActiveDoc.Views.Redraw()
            if showWin && not (isNull Sync.showEditor) then Sync.showEditor.Invoke() //because it might crash during UI interaction where it is hidden
            } |> Async.StartImmediate


    override this.OnLoad(refErrs) : PlugIns.LoadReturnCode =
        AssemblyInfo.track()
        let assemblies = AppDomain.CurrentDomain.GetAssemblies()

        // let loadedFsCoreVersion = assemblies |> Seq.tryFind (fun a -> a.GetName().Name = "Fsharp.Core") |> Option.map (fun a -> a.GetName().Version.ToString() )

        if not Runtime.HostUtils.RunningOnWindows then
            let errMsg = " * The Fesh.Rhino Scripting-Editor-For-F# PlugIn only works on Windows, not Mac. It depends on the WPF framework "
            refErrs <- errMsg
            RhinoAppWriteLine.print errMsg
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
                    "A RhinoCommon target for .NET 7 or 8 is not available yet."
                    "It might crash with .NET 7 or 8."
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
        //     RhinoAppWriteLine.print errMsg
        //     PlugIns.LoadReturnCode.ErrorShowDialog

        else
            RhinoAppWriteLine.print  "loading Fesh.Rhino Plugin ..."
            try
                let canRun () = not <| Rhino.Commands.Command.InCommand()
                let host = "Rhino"
                let hostData : Fesh.Config.HostedStartUpData = {
                    hostName = host
                    mainWindowHandel = RhinoApp.MainWindowHandle()
                    fsiCanRun = canRun
                    defaultCode = Some Util.defaultCode
                    // Add the Icon at the top left of the window and in the status bar, musst be called  after loading window.
                    // Media/LogoCursorTr.ico with Build action : "Resource"
                    // (for the exe file icon in explorer use <Win32Resource>Media\logo.res</Win32Resource>  in fsproj )
                    logo = Some (Uri("pack://application:,,,/Fesh.Rhino;component/Media/logo.ico"))
                    }

                let fesh = Fesh.App.createEditorForHosting( hostData )
                FeshPlugin.Fesh <- fesh
                Sync.showEditor <- new Action(fun () -> fesh.Window.Show())
                Sync.hideEditor <- new Action(fun () -> fesh.Window.Hide())
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

                Sync.editorWindow <- (fesh.Window :> Windows.Window)

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
                fesh.Fsi.OnRuntimeError.Add ( fun e -> FeshPlugin.AfterEval(true))  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
                fesh.Fsi.OnCanceled.Add     ( fun m -> FeshPlugin.AfterEval(true))  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
                fesh.Fsi.OnCompletedOk.Add  ( fun m -> FeshPlugin.AfterEval(false)) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin = false because might be running in background mode from rhino command line

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
                        RhinoAppWriteLine.print  "* Fesh.Rhino Plugin added the command alias 'fr' for 'FeshRunCurrentScript'"

                // Reinitialize Rhino.Scripting just in case it is loaded already in the current AppDomain by another plugin.
                // This is needed to have showEditor() and hideEditor() actions for Fesh setup correctly.
                assemblies
                |> Seq.tryFind (fun a -> a.GetName().Name = "Rhino.Scripting")
                |> Option.iter (fun rsAss ->
                    try
                        let rhinoSyncModule = rsAss.GetType("Rhino.RhinoSync")
                        let init = rhinoSyncModule.GetProperty("initialize").GetValue(rsAss) :?> Action
                        init.Invoke()
                        RhinoAppWriteLine.print "Rhino.Scripting.RhinoSync re-initialized."
                    with e ->
                        RhinoAppWriteLine.print (sprintf "* Fesh.Rhino Plugin Rhino.Scripting.Initialize() failed with %A" e)
                    )

                RhinoAppWriteLine.print  ("Fesh."+host + " plugin loaded.")
                App.showEditor() |> ignore
                PlugIns.LoadReturnCode.Success
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
                RhinoAppWriteLine.print e.Message
                RhinoAppWriteLine.print errMsg
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
