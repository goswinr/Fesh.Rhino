namespace Seff.Rhino

open Rhino
open System
open System.Windows

//the Command Singelton classes:

type LoadEditor () = 
    inherit Commands.Command()    
    static member val Instance = LoadEditor() 
    
    override this.EnglishName = "Seff" //The command name as it appears on the Rhino command line.
           
    override this.RunCommand (doc, mode)  =
        if isNull SeffPlugin.Instance.Window then // set up window on first run
            rh.print  "*loading Seff - Fsharp Scripting Editor Window..."
            let win = 
                Seff.App.runEditorHostedWithUndo(
                    RhinoApp.MainWindowHandle(),
                    "Rhino",
                    (fun () -> RhinoDoc.ActiveDoc.BeginUndoRecord "FsiSession" ) , // https://github.com/mcneel/rhinocommon/blob/57c3967e33d18205efbe6a14db488319c276cbee/dotnet/rhino/rhinosdkdoc.cs#L857
                    (fun i  -> RhinoDoc.ActiveDoc.EndUndoRecord(i) |> ignore )
                    )        

            SeffPlugin.Instance.Window <- win
            win.Closing.Add (fun e -> win.Visibility <- Visibility.Hidden ; e.Cancel <- true)
            Seff.Config.fileDefaultCode <- "#r @\"C:\Program Files\Rhinoceros 5 (64-bit)\System\RhinoCommon.dll\"\r\n" + Seff.Config.fileDefaultCode + "\r\nopen Rhino\r\n"
            //Seff.Config.codeToAppendEvaluations <- "\r\nRhino.RhinoDoc.ActiveDoc.Views.Redraw()"
            win.Show()
            rh.print  "*Seff Editor Window loaded."
            Commands.Result.Success
        else
            SeffPlugin.Instance.Window.Visibility <- Visibility.Visible
            Commands.Result.Success


        //TODO add rhino closing event that closes the window and so checks for unsaved script files before rhino closes

type RunCurrentScript () = 
    inherit Commands.Command()    
    static member val Instance = RunCurrentScript() 
    
    override this.EnglishName = "SeffRunCurrentScript"
           
    override this.RunCommand (doc, mode)  =
        
        match SeffPlugin.Instance.Window.Visibility with
        | Visibility.Visible | Visibility.Collapsed -> 
            match Seff.Tab.current with 
            | Some t -> 
                rh.print  "*Seff, running the current script.."
                let _,_,cmd,_ = Seff.Commands.RunAllText
                cmd.Execute(null) // the argumnent can be any obj, its ignored
                rh.print  "*Seff, ran current script." // this non-modal ? print another msg when completed
                Commands.Result.Success
            |None -> 
                rh.print "There is no active document in Seff editor"
                Commands.Result.Failure
        
        |Visibility.Hidden -> 
            SeffPlugin.Instance.Window.Visibility <- Visibility.Visible
            match Seff.Tab.current with 
            | Some t -> 
                match MessageBox.Show("Run Script from current Tab?", "Run Script from current Tab?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) with
                | MessageBoxResult.Yes -> this.RunCommand (doc, mode) 
                | _ -> Commands.Result.Failure
            |None -> 
                rh.print "There is no active Document in Seff editor"
                Commands.Result.Failure
                
        | _ -> Commands.Result.Failure // only needed to make F# compiler happy

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