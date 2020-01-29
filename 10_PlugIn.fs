﻿namespace Seff.Rhino

open Rhino
open System
open System.Windows
open Rhino.Runtime
open Seff
open Seff.Fsi

module rh = 
    let print a    = RhinoApp.WriteLine a    ; RhinoApp.Wait()
    let print2 a b = RhinoApp.WriteLine (a+b); RhinoApp.Wait()   
    
module Sync = //Don't change name  its used in Rhino.Scripting.dll via reflection
    let syncContext = Threading.SynchronizationContext.Current //Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable window = null : Window //Don't change name  its used in Rhino.Scripting.dll via reflection
    let mutable isCancelRequested = false // don't chnage ! accessed via reflection in RhinoscriptSyntax assembly

module Debugging = 
    open rh

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

// the Plugin  and Commands Singeltons:
// Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
// class. DO NOT create instances of this class yourself. It is the
// responsibility of Rhino to create an instance of this class.
// do not use "private" keyword on (singelton) constructor
type SeffPlugin () =  

    inherit PlugIns.PlugIn()
    
    //PlugIns.PlugInType.Utility how to set this ?

    static member val Instance = SeffPlugin() // singelton pattern neded for Rhino. http://stackoverflow.com/questions/2691565/how-to-implement-singleton-pattern-syntax
    
    static member val UndoRecordSerial = 0u with get,set
    
    static member PostEval (showWin) = 
        RhinoDoc.ActiveDoc.EndUndoRecord(SeffPlugin.UndoRecordSerial) |> ignore
        async{
            do! Async.SwitchToContext Sync.syncContext
            RhinoDoc.ActiveDoc.Views.RedrawEnabled <- true
            RhinoDoc.ActiveDoc.Views.Redraw()
            if showWin then Sync.window.Show() //because it might crash during UI interaction wher it is hidden
            } |> Async.StartImmediate


    //member this.Folder = IO.Path.GetDirectoryName(this.Assembly.Location) // for debug only

    // You can override methods here to change the plug-in behavior on
    // loading and shut down, add options pages to the Rhino _Option command
    // and mantain plug-in wide options in a document.

        
    override this.CreateCommands() = //to add script files as custom commands
        base.CreateCommands()
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
    
    

    override this.OnLoad refErrs =         
        if not Runtime.HostUtils.RunningOnWindows then 
            rh.print "Seff FSharp Scripting Editor PlugIn only works on Windows. It needs the WPF framework "
            PlugIns.LoadReturnCode.ErrorNoDialog
        else
            
            Fsi.Events.Canceled.Add ( fun m -> Sync.isCancelRequested <-false) //cancel not needed  anymore
            
            Fsi.Events.Started.Add      ( fun m -> SeffPlugin.UndoRecordSerial <- RhinoDoc.ActiveDoc.BeginUndoRecord "FsiSession" )   // https://github.com/mcneel/rhinocommon/blob/57c3967e33d18205efbe6a14db488319c276cbee/dotnet/rhino/rhinosdkdoc.cs#L857
            Fsi.Events.RuntimeError.Add ( fun e -> SeffPlugin.PostEval(true)) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction wher it is hidden
            Fsi.Events.Canceled.Add     ( fun m -> SeffPlugin.PostEval(true)) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin because it might crash during UI interaction wher it is hidden  
            Fsi.Events.Completed.Add    ( fun m -> SeffPlugin.PostEval(false)) // to unsure UI does not stay frozen if RedrawEnabled is false //showWin = false because might be running in background mode from rhino command line
              
            
            RhinoApp.Closing.Add (fun (e:EventArgs) -> 
                Seff.FileDialogs.closeWindow() |> ignore) // to save unsaved files
            
            RhinoDoc.CloseDocument.Add (fun (e:DocumentEventArgs) -> Fsi.cancelIfAsync())
            
            RhinoApp.EscapeKeyPressed.Add ( fun (e:EventArgs) -> 
                    
                    if not <| Input.RhinoGet.InGet(RhinoDoc.ActiveDoc) then // TODO blocking in async ??
                        Sync.isCancelRequested <-true
                        Fsi.cancelIfAsync()
                    ) 
                    //don't block event completion by doing some debug logging
                    //match Fsi.state with
                    //|Ready ->()
                    //|Evaluating -> 
                        //match Fsi.mode with
                        //|Async ->   Fsi.cancel()  // abort Thread // rh.print "* Seff.Rhino: 'Esc' was pressed. Canceled currently running FSharp Interacitve Script."
                        //|Sync ->    () //don't block event completion by doing some debug logging // do nothing wait for Escape test event attched in Rhinscriotsyntax
                        //)
            
            /// add Alias too :
            if not <|  ApplicationSettings.CommandAliasList.IsAlias("sr") then 
                if ApplicationSettings.CommandAliasList.Add("sr","SeffRunCurrentScript")then 
                    rh.print  "*Seff.Rhino Plugin added the comand alias 'sr' for 'SeffRunCurrentScript'"

            //Debugging.printAssemblyInfo(this)
            
            rh.print  "*Seff.Rhino Plugin loaded..."
            PlugIns.LoadReturnCode.Success
    
    
    //override this.LoadAtStartup = true //obsolete??//Seff.Fsi.agent.Post Seff.Fsi.AgentMessage.Done // load FSI already at Rhino startup ??
    
