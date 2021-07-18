namespace Seff.Rhino //Don't change name  its used in Rhino.Scripting.dll via reflection

open Rhino
open System
open System.Windows
open Rhino.Runtime
open Seff
open Seff.Model
open Seff.Config


module Sync =  //Don't change name  its used in Rhino.Scripting.dll via reflection                                                 
    let syncContext = Threading.SynchronizationContext.Current  //Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable window = null : Window                          //Don't change name  its used in Rhino.Scripting.dll via reflection


module RhinoAppWriteLine = 
    let print a    = RhinoApp.WriteLine a    ; RhinoApp.Wait()
    let print2 a b = RhinoApp.WriteLine (a+b); RhinoApp.Wait()   
    

module Debugging = 
    open RhinoAppWriteLine

    let printAssemblyInfo (plug:PlugIns.PlugIn) =             
        let rec getAllFiles dir pattern = 
            seq { yield! IO.Directory.EnumerateFiles(dir, pattern)//(patter = "*.pdf")
                  for d in IO.Directory.EnumerateDirectories(dir) do
                        yield! getAllFiles d pattern }     
        
        // FSI assembly binding: https://github.com/Microsoft/visualfsharp/issues/3600#issuecomment-330378022
        for s in Runtime.HostUtils.GetAssemblySearchPaths() do                    
            print2 "*Searched: " s
            for file in getAllFiles s "*.dll" do                        
                if file.ToUpper().Contains("FSHARP") then 
                    print2 "*Dll found: " file             

        let assem = plug.Assembly
        if isNull assem then print "***cannot get pulgin assembly loaction"
        else                 
            print2 "*plugin loaded from: " assem.Location
                    
            let ra = Runtime.HostUtils.GetRhinoDotNetAssembly()
            if isNull ra then print "***cannot load Runtime.HostUtils.GetRhinoDotNetAssembly"
            else print2 "*GetRhinoDotNetAssembly loaded from:" ra.Location
                    
            let folder = IO.Path.GetDirectoryName(assem.Location)
            let fc = Reflection.Assembly.LoadFile(IO.Path.Combine(folder,"FSharp.Core.dll"))  
            let fcs =  Reflection.Assembly.LoadFile(IO.Path.Combine(folder,"FSharp.Compiler.Service.dll"))

            if isNull fc then print "***cannot load Fsarp.Core"
            else print2 "*Fsharp.Core loaded from:" fc.Location

            if isNull fcs then print "***cannot load Fsarp.Compiler.Service"
            else print2 "*Fsharp.Compiler.Service loaded from:" fcs.Location
        
        print "++AppDomain.CurrentDomain.GetAssemblies():"
        AppDomain.CurrentDomain.GetAssemblies()
        |> Seq.sortBy string
        |> Seq.iter (sprintf "%A" >> print)

// the Plugin  and Commands Singeltons:
// Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
// class. DO NOT create instances of this class yourself. It is the
// responsibility of Rhino to create an instance of this class.
// do not use "private" keyword on (singelton) constructor
type SeffPlugin () =  
    inherit PlugIns.PlugIn()
    
    static let mutable lastDoc = RhinoDoc.ActiveDoc 

    //PlugIns.PlugInType.Utility how to set this ?

    static member val Instance = SeffPlugin() // singelton pattern neded for Rhino. http://stackoverflow.com/questions/2691565/how-to-implement-singleton-pattern-syntax
    
    static member val UndoRecordSerial = 0u with get,set
        
    static member val Seff = Unchecked.defaultof<Seff> with get,set

    static member BeforeEval () = 
           async{
               do! Async.SwitchToContext Sync.syncContext
               lastDoc <- RhinoDoc.ActiveDoc
               SeffPlugin.UndoRecordSerial <- RhinoDoc.ActiveDoc.BeginUndoRecord "FsiSession" 
               } |> Async.StartImmediate

    static member AfterEval (showWin) = 
        async{
            do! Async.SwitchToContext Sync.syncContext
            //if SeffPlugin.UndoRecordSerial <> 0u then
            
            if lastDoc = RhinoDoc.ActiveDoc then // it might have changed during script run
                if not <| RhinoDoc.ActiveDoc.EndUndoRecord(SeffPlugin.UndoRecordSerial) then
                    eprintfn "failed to set RhinoDoc.ActiveDoc.EndUndoRecord(SeffPlugin.UndoRecordSerial:%d)" SeffPlugin.UndoRecordSerial        
            
            RhinoDoc.ActiveDoc.Views.RedrawEnabled <- true
            RhinoDoc.ActiveDoc.Views.Redraw()
            if showWin then Sync.window.Show() //because it might crash during UI interaction where it is hidden
            } |> Async.StartImmediate


    //member this.Folder = IO.Path.GetDirectoryName(this.Assembly.Location) // for debug only

    static member val PrintOnceAfterEval = "" with get,set // to be ablke to print after SeffRunCurrentScript command 

    override this.OnLoad refErrs =         
        if not Runtime.HostUtils.RunningOnWindows then 
            RhinoAppWriteLine.print " * Seff | Scripting Editor For FSharp PlugIn only works on Windows. It needs the WPF framework "
            PlugIns.LoadReturnCode.ErrorNoDialog
        else    
            RhinoAppWriteLine.print  "* loading Seff.Rhino Plugin ..."           
            let canRun () = not <| Rhino.Commands.Command.InCommand()
            #if RHINO6
            let host = "Rhino 6"
            #else
            let host = "Rhino 7"
            #endif
            let hostData = { 
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
            Sync.window <- (seff.Window :> Window)

            seff.Window.Closing.Add (fun e ->         
                
                match seff.Fsi.AskAndCancel() with
                |Evaluating -> e.Cancel <- true // no closing
                |Ready | Initalizing | NotLoaded -> 
                    seff.Window.Visibility <- Visibility.Hidden 
                    //TODO add option to menu to actually close, not just hide ??
                    e.Cancel <- true) // i think user would rather expect full closing ? 
            
            //win.Closed.Add (fun _ -> Sync.window <- null) // TODO, it seems it cant be restarted then.

            seff.Fsi.OnStarted.Add      ( fun m -> SeffPlugin.BeforeEval())   // https://github.com/mcneel/rhinocommon/blob/57c3967e33d18205efbe6a14db488319c276cbee/dotnet/rhino/rhinosdkdoc.cs#L857
            seff.Fsi.OnRuntimeError.Add ( fun e -> SeffPlugin.AfterEval(true))  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction wher it is hidden
            seff.Fsi.OnCanceled.Add     ( fun m -> SeffPlugin.AfterEval(true))  // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction wher it is hidden  
            seff.Fsi.OnCompletedOk.Add  ( fun m -> SeffPlugin.AfterEval(false)) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin = false because might be running in background mode from rhino command line
            seff.Fsi.OnIsReady.Add      ( fun m -> if SeffPlugin.PrintOnceAfterEval <> "" then RhinoApp.WriteLine SeffPlugin.PrintOnceAfterEval ; SeffPlugin.PrintOnceAfterEval <- "")  
           
            // TODO done by seff anyway?? 
            
            
            RhinoDoc.CloseDocument.Add (fun e -> seff.Fsi.CancelIfAsync() ) //during sync eval closing doc should not be possible anyway??
            RhinoApp.Closing.Add (fun _ -> 
                seff.Tabs.AskIfClosingWindowIsOk() |> ignore // to save unsaved files, canceling of closing not possible here, save dialog will show after rhino is closed
                seff.Fsi.AskIfCancellingIsOk() |> ignore
                seff.Fsi.CancelIfAsync()   //sync eval gets canceled anyway
                )
            
            
            //Dummy attachment in sync mode  to prevent access violation exception if first access is in async mode  
            //Dont abort on esc, only on ctrl+break or RhinoScriptSyntax.EscapeTest() 
            RhinoApp.EscapeKeyPressed.Add ( fun e -> ()) 
            
            // add Alias too :
            if not <|  ApplicationSettings.CommandAliasList.IsAlias("sr") then 
                if ApplicationSettings.CommandAliasList.Add("sr","SeffRunCurrentScript")then 
                    RhinoAppWriteLine.print  "* Seff.Rhino Plugin added the command alias 'sr' for 'SeffRunCurrentScript'"

            //Debugging.printAssemblyInfo(this)
            
            RhinoAppWriteLine.print  ("Seff."+host + " Plugin loaded.")
            PlugIns.LoadReturnCode.Success
    
    
    //override this.LoadAtStartup = true //obsolete??//Seff.Fsi.agent.Post Seff.Fsi.AgentMessage.Done // load FSI already at Rhino startup ??
    
    // You can override methods here to change the plug-in behavior on
    // loading and shut down, add options pages to the Rhino _Option command
    // and mantain plug-in wide options in a document.
    override this.CreateCommands() = //to add script files as custom commands
        // https://discourse.mcneel.com/t/how-to-create-commands-after-plugin-load/47833    
        
        base.CreateCommands()
        // then call base.RegisterCommand()
        // or ? Rhino.Runtime.HostUtils.RegisterDynamicCommand(seffPlugin,command)

        //for file in commandsfiles do
        // let cmd `= creat instance of command derived class
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
    