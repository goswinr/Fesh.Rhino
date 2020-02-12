﻿namespace Seff.Rhino

open Rhino
open System
open System.Windows
open Seff.Util
open Seff

//the Command Singelton classes:

type LoadEditor () = 
    inherit Commands.Command()    
    static member val Instance = LoadEditor() 
    
    override this.EnglishName = "Seff" //The command name as it appears on the Rhino command line.
           
    override this.RunCommand (doc, mode)  =
        if isNull Sync.window then // set up window on first run
            rh.print  "*loading Seff - Fsharp Scripting Editor Window..."            
            let win = Seff.App.runEditorHosted(  RhinoApp.MainWindowHandle(), "Rhino" )
            Sync.window <- win

            //win.Closing.Add (fun e ->         // not needed ???
            //    match Fsi.askAndCancel() with
            //    |Fsi.States.Evaluating -> e.Cancel <- true
            //    |Fsi.States.Ready -> 
            //        win.Visibility <- Visibility.Hidden 
            //        e.Cancel <- true) // i think user would rather expect full closing ? 
            
            //win.Closed.Add (fun _ -> Sync.window <- null) // cant be restarted then ??

            
            win.Show()
            rh.print  "*Seff Editor Window loaded."
            Commands.Result.Success

        else            
            if Sync.window.WindowState = WindowState.Minimized then Sync.window.WindowState <- WindowState.Normal 
            Sync.window.Visibility <- Visibility.Visible
            Sync.window.Show()
            Commands.Result.Success



type RunCurrentScript () = 
    inherit Commands.Command()    
    static member val Instance = RunCurrentScript() 
    
    override this.EnglishName = "SeffRunCurrentScript"
           
    override this.RunCommand (doc, mode)  =
        
        match Sync.window.Visibility with
        | Visibility.Visible | Visibility.Collapsed -> 
            match Seff.Tab.current with 
            | Some t -> 
                rh.print  "*Seff, running the current script.."
                let _,_,cmd,_ = Seff.Commands.RunAllText //TODO or trigger directly via agent post to distinguish triggers from commandline and seff ui?
                cmd.Execute(null) // the argumnent can be any obj, its ignored
                rh.print  "*Seff, ran current script." // this non-modal ? print another msg when completed
                Commands.Result.Success
            |None -> 
                rh.print "There is no active script file in Seff editor"
                Commands.Result.Failure
        
        |Visibility.Hidden -> 
            Sync.window.Visibility <- Visibility.Visible
            match Seff.Tab.current with 
            | Some t -> 
                match MessageBox.Show("Run Script from current Tab?", "Run Script from current Tab?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) with
                | MessageBoxResult.Yes -> this.RunCommand (doc, mode) 
                | _ -> Commands.Result.Failure
            |None -> 
                rh.print "There is no active script file in Seff editor"
                Commands.Result.Failure
                
        | _ -> Commands.Result.Failure // only needed to make F# compiler happy



// type Redraw () =                            // why ??
//     inherit Commands.Command()    
//     static member val Instance = Redraw() 
//             
//     override this.EnglishName = "Redraw" //The command name as it appears on the Rhino command line.
//                    
//     override this.RunCommand (doc, mode)  =        
//         RhinoDoc.ActiveDoc.Views.RedrawEnabled <- true
//         RhinoDoc.ActiveDoc.Views.Redraw()
//         Commands.Result.Success


//TODO mouse focus: https://discourse.mcneel.com/t/can-rhinocommon-be-used-with-wpf/12/7
(*
a python script loadad from a file as command:

[CommandStyle(Style.ScriptRunner)]
  class PythonCommand : Command
  {
    readonly string m_english_name;
    readonly Guid m_id;
    readonly string m_resource_name;

    public PythonCommand(string englishName, string resourceName)
    {
      m_english_name = englishName;
      m_id = Guid.NewGuid();
      m_resource_name = resourceName;
    }

    public override Guid Id { get { return m_id; } }

    public override string EnglishName { get { return m_english_name; } }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      var script = Rhino.Runtime.PythonScript.Create();
      script.ScriptContextDoc = doc;
      script.ScriptContextCommand = this;
      var resource_stream = typeof(PythonTestCommand).Assembly.GetManifestResourceStream(m_resource_name);
      if (resource_stream == null)
        return Result.Failure;
      var stream = new System.IO.StreamReader(resource_stream);
      string s = stream.ReadToEnd();
      stream.Close();
      return script.ExecuteScript(s) ? Result.Success:Result.Failure;
    }
  }
  *)