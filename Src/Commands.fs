namespace Seff.Rhino

open Rhino
open System
open System.Windows
open Seff.Util
open Seff
open Rhino.Commands

//the Command Singelton classes:

module State =     
    let mutable ShownOnce = false // having this as static member on LoadEditor fails to evaluate !! not sure why.

type LoadEditor () = 
    inherit Commands.Command()    
    static member val Instance = LoadEditor() 
    
    static member DoLoad() = 
        if isNull Sync.window then // set up window on first run            
            rh.print  " * Seff Editor Window cant be shown, the Plugin is not properly loadad. try restarting Rhino."
            Commands.Result.Failure

        else            
            Sync.window.Show()
            Sync.window.Visibility <- Visibility.Visible
            if Sync.window.WindowState = WindowState.Minimized then Sync.window.WindowState <- WindowState.Normal             
            State.ShownOnce <- true
            Commands.Result.Success

    override this.EnglishName = "Seff" //The command name as it appears on the Rhino command line.
           
    override this.RunCommand (doc, mode)  = 
        LoadEditor.DoLoad()


    (*
    type LoadFsi () = 
        inherit Commands.Command()    
        static member val Instance = LoadFsi() 
                
        override this.EnglishName = "SeffLoadFsi" //The command name as it appears on the Rhino command line.
                       
        override this.RunCommand (doc, mode)  =
            if isNull Sync.window then // set up window on first run            
                rh.print  " * Seff Editor Window cant be shown, the Plugin is not properly loadad . please restart Rhino."
                Commands.Result.Failure            
            else            
                rh.print  "loading Fsi ..."
                Fsi.Initalize()
                rh.print  "Fsi loaded."
                Commands.Result.Success
                *)


type RunCurrentScript () = 
    inherit Commands.Command()    
    static member val Instance = RunCurrentScript() 
    
    override this.EnglishName = "SeffRunCurrentScript"
           
    override this.RunCommand (doc, mode)  =
        
        if isNull Sync.window then // set up window on first run            
            rh.print  "*Seff Editor Window cant be shown, the Plugin is not properly loadad . please restart Rhino."
            Commands.Result.Failure
        else
            if not State.ShownOnce then 
                LoadEditor.DoLoad()  
                // it needs to be shown once. otherwise Seff.Commands.RunAllText below fails to find any text in Editor
            else           
                match Sync.window.Visibility with
                | Visibility.Visible | Visibility.Collapsed ->                
                    rh.print2  "*Seff is running " SeffPlugin.Seff.Tabs.Current.FormatedFileName
                    SeffPlugin.PrintOnceAfterEval <- "*Seff is done!"
                    let cmd = SeffPlugin.Seff.Commands.RunAllText //TODO or trigger directly via agent post to distinguish triggers from commandline and seff ui?
                    
                    // to start running the script after the command has actually completed = making it modeless so manual undo stack works
                    async{
                        do! Async.Sleep 50 // wait till command actually completes. so that RhinoDoc.ActiveDoc.BeginUndoRecord does not return 0 
                        let k = ref 0
                        while Command.InCommand() && !k < 40 do // wait up to 2 sec more ?  
                            incr k 
                            do! Async.Sleep 50                          
                        do! Async.SwitchToContext Sync.syncContext
                        cmd.cmd.Execute(null)} // the argumnent can be any obj, its ignored}
                    |> Async.Start  

                    //rh.print  "*Seff, ran current script." //prints immedeatly in async mode
                    Commands.Result.Success

        
                |Visibility.Hidden -> 
                    Sync.window.Visibility <- Visibility.Visible                
                    match MessageBox.Show("Run Script from current Tab?", "Run Script from current Tab?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) with
                    | MessageBoxResult.Yes -> this.RunCommand (doc, mode) 
                    | _ -> Commands.Result.Failure
                
                
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