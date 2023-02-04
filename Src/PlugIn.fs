namespace Seff.Rhino // Don't change name  its used in Rhino.Scripting.dll via reflection

open Rhino
open System
open Seff


module Sync =  //Don't change name  its used in Rhino.Scripting.dll via reflection
    let syncContext = Threading.SynchronizationContext.Current  // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable hideEditor = null: Action // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable showEditor = null: Action // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable isEditorVisible = null: Func<bool> // Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable window = null: Windows.Window // Not used via reflection
    

module RhinoAppWriteLine = 
    let print txt  = RhinoApp.WriteLine txt  ; RhinoApp.Wait()
    let print2 txt1 txt2 = RhinoApp.WriteLine (txt1+txt2); RhinoApp.Wait()

// the Plugin  and Commands Singletons:
// Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
// class. DO NOT create instances of this class yourself. It is the
// responsibility of Rhino to create an instance of this class.
// do not use "private" keyword on (singleton) constructor
type SeffPlugin () = 
    inherit PlugIns.PlugIn()

    static let mutable lastDoc = RhinoDoc.ActiveDoc

    static member val RhWriter : IO.TextWriter option = Some <| new Rhino.RhinoApp.CommandLineTextWriter() 

    //PlugIns.PlugInType.Utility how to set this ?

    static member val Instance = SeffPlugin() // singleton pattern needed for Rhino. http://stackoverflow.com/questions/2691565/how-to-implement-singleton-pattern-syntax

    static member val UndoRecordSerial = 0u with get,set

    static member val Seff = Unchecked.defaultof<Seff> with get,set

    static member BeforeEval () = 
            async{
                do! Async.SwitchToContext Sync.syncContext
                lastDoc <- RhinoDoc.ActiveDoc
                SeffPlugin.UndoRecordSerial <- RhinoDoc.ActiveDoc.BeginUndoRecord "F# script run by Seff.Rhino"
                } |> Async.StartImmediate

    static member AfterEval (showWin) = 
        async{
            do! Async.SwitchToContext Sync.syncContext
            //if SeffPlugin.UndoRecordSerial <> 0u then

            if lastDoc = RhinoDoc.ActiveDoc then // it might have changed during script run
                if not <| RhinoDoc.ActiveDoc.EndUndoRecord(SeffPlugin.UndoRecordSerial) then
                    RhinoAppWriteLine.print " * Seff.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord"
                    eprintfn " * Seff.Rhino | failed to set RhinoDoc.ActiveDoc.EndUndoRecord(SeffPlugin.UndoRecordSerial:%d)" SeffPlugin.UndoRecordSerial

            RhinoDoc.ActiveDoc.Views.RedrawEnabled <- true
            RhinoDoc.ActiveDoc.Views.Redraw()
            if showWin && not (isNull Sync.showEditor) then Sync.showEditor.Invoke() //because it might crash during UI interaction where it is hidden
            } |> Async.StartImmediate


    //static member val PrintOnceAfterEval = "" with get,set // to be able to print after SeffRunCurrentScript command

    override this.OnLoad refErrs = 
        if not Runtime.HostUtils.RunningOnWindows then
            RhinoAppWriteLine.print " * Seff.Rhino  | Scripting Editor For FSharp PlugIn only works on Windows. It needs the WPF framework "
            PlugIns.LoadReturnCode.ErrorNoDialog
        else
            RhinoAppWriteLine.print  "loading Seff.Rhino Plugin ..."
            let canRun () = not <| Rhino.Commands.Command.InCommand() 
            let host = "Rhino"
            let hostData : Seff.Config.HostedStartUpData = {
                hostName = host
                mainWindowHandel = RhinoApp.MainWindowHandle()
                fsiCanRun = canRun
                // Add the Icon at the top left of the window and in the status bar, musst be called  after loading window.
                // Media/LogoCursorTr.ico with Build action : "Resource"
                // (for the exe file icon in explorer use <Win32Resource>Media\logo.res</Win32Resource>  in fsproj )
                logo = Some (Uri("pack://application:,,,/Seff.Rhino;component/Media/logo.ico"))
                }

            let seff = Seff.App.createEditorForHosting( hostData )
            SeffPlugin.Seff <- seff
            Sync.showEditor <- new Action(fun () -> seff.Window.Show())
            Sync.hideEditor <- new Action(fun () -> seff.Window.Hide())
            Sync.isEditorVisible <- new Func<bool>(fun () -> seff.Window.Visibility = Windows.Visibility.Visible)
            Sync.window <- (seff.Window :> Windows.Window)
            

            seff.Window.Closing.Add (fun e ->
                if not e.Cancel then // closing might be already cancelled in Seff.fs in main Seff lib.               
                    // even if closing is not canceled, don't close, just hide window
                    seff.Window.Visibility <- Windows.Visibility.Hidden
                    e.Cancel <- true
                    )

            seff.Window.StateChanged.Add (fun e ->
                match seff.Fsi.State with 
                |FsiState.Ready ->   
                    // if the window is hidden log error messages to rhino command line, but not when window is shown
                    // this is also set in SeffRunCurrentScript Command
                    match seff.Window.WindowState with
                    | Windows.WindowState.Normal
                    | Windows.WindowState.Maximized    -> seff.Log.AdditionalLogger <- None
                    | Windows.WindowState.Minimized |_ -> seff.Log.AdditionalLogger <- SeffPlugin.RhWriter                    
                    
                |Initializing |NotLoaded  |Evaluating -> ()   // don't change while running                    
                )
            

            seff.Fsi.OnStarted.Add      ( fun m -> SeffPlugin.BeforeEval())     // https://github.com/mcneel/rhinocommon/blob/57c3967e33d18205efbe6a14db488319c276cbee/dotnet/rhino/rhinosdkdoc.cs#L857
            seff.Fsi.OnRuntimeError.Add ( fun e -> SeffPlugin.AfterEval(true))  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
            seff.Fsi.OnCanceled.Add     ( fun m -> SeffPlugin.AfterEval(true))  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction where it is hidden
            seff.Fsi.OnCompletedOk.Add  ( fun m -> SeffPlugin.AfterEval(false)) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin = false because might be running in background mode from rhino command line
            
            RhinoDoc.CloseDocument.Add (fun e -> seff.Fsi.CancelIfAsync() ) //during sync eval closing doc should not be possible anyway??
            RhinoApp.Closing.Add (fun _ ->
                seff.Tabs.AskForFileSavingToKnowIfClosingWindowIsOk() |> ignore // to save unsaved files, canceling of closing not possible here, save dialog will show after rhino is closed
                seff.Fsi.AskIfCancellingIsOk() |> ignore
                seff.Fsi.CancelIfAsync()   //sync eval gets canceled anyway
                )


            // Dummy attachment in sync mode  to prevent access violation exception if first access is in async mode
            // Don't abort on esc, only on ctrl+break or Rhino.Scripting.EscapeTest()
            RhinoApp.EscapeKeyPressed.Add ( fun e -> ())

            // add Alias too if not taken already:
            if not <|  ApplicationSettings.CommandAliasList.IsAlias("sr") then
                if ApplicationSettings.CommandAliasList.Add("sr","SeffRunCurrentScript")then
                    RhinoAppWriteLine.print  "* Seff.Rhino Plugin added the command alias 'sr' for 'SeffRunCurrentScript'"

            RhinoAppWriteLine.print  ("Seff."+host + " Plugin loaded.")
            PlugIns.LoadReturnCode.Success


    //override this.LoadAtStartup = true //obsolete? load FSI already at Rhino startup ??

    // You can override methods here to change the plug-in behavior on
    // loading and shut down, add options pages to the Rhino _Option command
    // and maintain plug-in wide options in a document.
    override this.CreateCommands() = //to add script files as custom commands
        // https://discourse.mcneel.com/t/how-to-create-commands-after-plugin-load/47833

        base.CreateCommands()
        // then call base.RegisterCommand()
        // or ? Rhino.Runtime.HostUtils.RegisterDynamicCommand(seffPlugin,command)

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
