namespace Seff.Rhino

open Rhino
open System
open Seff.Model
open Seff
open Rhino.Commands

//the Command Singleton classes:

module State = 
    let mutable ShownOnce = false // having this as static member on LoadEditor fails to evaluate !! not sure why.

[<CommandStyle(Style.ScriptRunner)>] // so that RhinoApp.RunScript ( = rs.Command) can be used. https://developer.rhino3d.com/guides/rhinocommon/run-rhino-command-from-plugin/
type LoadEditor () = 
    inherit Commands.Command()
    static member val Instance = LoadEditor()

    static member DoLoad() = 
        if isNull Sync.editorWindow then // set up window on first run
            RhinoAppWriteLine.print  " * Seff Editor Window cant be shown, the Plugin is not properly loaded. try restarting Rhino."
            Commands.Result.Failure

        else
            Sync.editorWindow.Show()
            Sync.editorWindow.Visibility <- Windows.Visibility.Visible
            if Sync.editorWindow.WindowState = Windows.WindowState.Minimized then Sync.editorWindow.WindowState <- Windows.WindowState.Normal
            State.ShownOnce <- true
            Commands.Result.Success

    override this.EnglishName = "Seff" //The command name as it appears on the Rhino command line.

    override this.RunCommand (doc, mode)  = 
        LoadEditor.DoLoad()

[<CommandStyle(Style.ScriptRunner)>] // so that RhinoApp.RunScript ( = rs.Command) can be used. https://developer.rhino3d.com/guides/rhinocommon/run-rhino-command-from-plugin/
type RunCurrentScript () = 
    inherit Commands.Command()
    static member val Instance = RunCurrentScript()

    override this.EnglishName = "SeffRunCurrentScript"

    override this.RunCommand (doc, mode)  : Result = 

        if isNull Sync.editorWindow then // set up window on first run
            RhinoAppWriteLine.print  "*Seff Editor Window cant be shown, the Plugin is not properly loaded. Please restart Rhino."
            Commands.Result.Failure
        else
            if not State.ShownOnce then
                LoadEditor.DoLoad()
                // it needs to be shown once. otherwise Seff.Commands.RunAllText below fails to find any text in Editor
            else                
                let seff = SeffPlugin.Seff
                match Sync.editorWindow.Visibility with
                | Windows.Visibility.Visible | Windows.Visibility.Collapsed ->
                    RhinoAppWriteLine.print2  "*Seff is running: " seff.Tabs.Current.FormattedFileName                    

                    // to start running the script after the command has actually completed, making it mode-less, so manual undo stack works
                    async{
                        do! Async.Sleep 30 // wait till command SeffRunCurrentScript actually completes. so that RhinoDoc.ActiveDoc.BeginUndoRecord does not return 0
                        let k = ref 0
                        while Command.InCommand() && !k < 20 do // wait up to 1.5 sec more ?
                            incr k
                            do! Async.Sleep 50
                        do! Async.SwitchToContext Sync.syncContext
                        if Command.InCommand() then
                            seff.Log.PrintfnAppErrorMsg "Can't Run Current Seff Script because another Rhino Command is active"
                            RhinoAppWriteLine.print "Can't Run Current Seff Script because another Rhino Command is active"
                        else
                            let ed = seff.Tabs.Current.Editor                            
                            match Sync.editorWindow.WindowState with // if editor is not visible print results to rhino command line too.
                            | Windows.WindowState.Normal
                            | Windows.WindowState.Maximized    -> seff.Tabs.Fsi.Evaluate {editor=ed; amount=All; logger = None               ; scriptName = ed.FilePath.FileName }
                            | Windows.WindowState.Minimized |_ -> seff.Tabs.Fsi.Evaluate {editor=ed; amount=All; logger = SeffPlugin.RhWriter; scriptName = ed.FilePath.FileName }
                            
                                
                    }
                    |> Async.Start

                    //rh.print  "*Seff, ran current script." //prints immediately in async mode
                    Commands.Result.Success


                |Windows.Visibility.Hidden ->
                    Sync.editorWindow.Visibility <- Windows.Visibility.Visible
                    let cmd = seff.Commands.RunAllText //TODO or trigger directly via agent post to distinguish triggers from commandline and seff ui?
                    match Windows.MessageBox.Show("Run Script from current Tab?", "Run Script from current Tab?", Windows.MessageBoxButton.YesNo, Windows.MessageBoxImage.Question, Windows.MessageBoxResult.Yes) with
                    | Windows.MessageBoxResult.Yes -> this.RunCommand (doc, mode)
                    | _ -> Commands.Result.Failure

                | _ -> Commands.Result.Failure // only needed to make F# compiler happy
        

//TODO mouse focus: https://discourse.mcneel.com/t/can-rhinocommon-be-used-with-wpf/12/7
(*
a python script loaded from a file as command:

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
