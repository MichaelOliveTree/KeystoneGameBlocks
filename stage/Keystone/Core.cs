using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Amib.Threading;
using Keystone.Utilities;
using Keystone.Commands;
using Keystone.Resource;
using Keystone.Scene;
using Keystone.Simulation;
using Keystone.Timers;


namespace Keystone
{
    public class Core : ICore
    {
        protected static Core _internalCore;
        internal static Core _Core
        {
            get { return _internalCore; }
            set { _internalCore = value; }
        }

        internal Keystone.TileMap.MapLayerGrid MapGrid;
        
        // core base
        public static readonly string ATTRIB_ID = "id";
        public static readonly string ATTRIB_REF = "ref"; // another document table (eg Models.xml, Entities.xml, Zones.xml)
        public static readonly string ATTRIB_SRC = "src"; // a link to a saved Node such as prefab in mod db (eg .KGBEntity prefab)

        protected Settings.Initialization _ini;
        protected bool _isInitialized;
        protected string _basePath;
        protected string _dataPath;
        protected string _modsPath;
        protected string _savesPath;
        protected string _relativeLayerDataPath = @"zones\layers";

        public int MESSAGE_MAX_HEADER_SIZE = 256; // this value is just a guestimation really.  It basically represents the combined sizes of all property values in a command message except for mData[] or NodeIDsToUse[] and so is used to determine whether we need to use command fragment processing for a particular command (eg KeyCommon.Messages.Transfer_Entity_File)
        public int MaxFragmentSize; 

        public enum SceneMode
        {
            EditScene,
            EditMission,
            Build, // build is for refitting exterior and interior of vehicle by player.  Simulation must be previous SceneMode.  This suspends simulation. After Build mode is finished, we must revert back to Simulation mode
            Simulation
        }


        public struct SceneState
        {
            // while in floor plan edit mode, you cannot change the primary perspective from top down x/z orthographic  projection
            // you actually can leave floor plan edit mode, but you will then no longer be able to modify walls, floors or ceilings. (except for placing components like doors and windows)
            public SceneType Type;
            public SceneMode Mode;
            public string FileName;
            public string Name;
            public bool IsUntitled;
            public bool Changed;
        }

        protected SceneState _primarySceneState;



        protected bool _usingDegrees;
        protected bool mScriptsEnabled = true;
        protected bool mSimulationEnabled = false;
        
        protected SceneManagerBase _sceneManager;
        protected SmartThreadPool _threadPool;
        protected int _pagerConcurrency;
        protected CommandProcessor _commandProcessor;
        //protected CmdConsole.Console _console;

        private KeyScript.Host.Loader mScriptLoader;

        // TODO: singleton pattern
        public Core(Settings.Initialization ini, string basePath, string dataPath, string modsPath, string savesPath)
        {
            try
            {
                if (ini == null) throw new ArgumentNullException();

                _basePath = basePath;
                // basepath is not the exe path.  its the root folder of your application since exe can exist
                // in an appfolder\bin directory
                _dataPath = dataPath;
                _modsPath = modsPath;
                _savesPath = savesPath;
                
                _ini = ini;
              
                _usingDegrees = true;
               
                CRC32.Init();
                _Core = this;

                string modName = "caesar"; // TODO: name of mod should be passed in
                string scriptFolder =  System.IO.Path.Combine(_modsPath, System.IO.Path.Combine (modName, "scripts"));

                mScriptLoader = new KeyScript.Host.Loader(scriptFolder);
                //_console = new Keystone.CmdConsole.Console();

                _primarySceneState.Type = SceneType.SingleRegion;
                _primarySceneState.Mode = SceneMode.EditScene;
                _primarySceneState.Name = "";
                _primarySceneState.FileName = "";
                _primarySceneState.Changed = false;
                _primarySceneState.IsUntitled = true;

                Initialize();

                Trace.WriteLine("Engine Initialized.");
            }
            catch
            {
                Trace.WriteLine("Core failed to initialize;");
            }
        }

        public virtual void Initialize()
        {
            if (_isInitialized ) return;

            // TODO: we should use seperate threadpools for
            // - background paging in/out tvresources 
            // - for IO to a given xmldb (note: we can have seperate xmldb's if multiple scenes are open)
            // - for in the future our parrallel loops 
            // but it should be noted that having > 1 for our background thread results in poor usage of the Repository to find duplicates
            // TODO: actually the below is somewhat obsolete, i believe i now use a seperate Group to do paging and yet another sperate
            // Group to do other things
            Amib.Threading.STPStartInfo info = new STPStartInfo();
            info.ThreadPriority = System.Threading.ThreadPriority.Lowest;
            info.MaxWorkerThreads = 6; // we should be able to have 4 threads here so long as Pager and SceneReader and SceneWriter's use a seperate ThreadPoolGroup with max threads = 1
            _threadPool = new SmartThreadPool(info); 
            _pagerConcurrency = 1;
            _commandProcessor = new CommandProcessor(_threadPool);

            if (File.Exists(Path.Combine(_basePath, @"debugfile.txt")))
                File.Delete(Path.Combine(_basePath, @"debugfile.txt"));


            MaxFragmentSize = Settings.settingReadInteger("lidgren", "receivebuffersize") - MESSAGE_MAX_HEADER_SIZE;
            _isInitialized = true;
        }

        public bool ScriptsEnabled { get { return mScriptsEnabled;  } set { mScriptsEnabled = value; } }
        public SmartThreadPool ThreadPool
        {
            get { return _threadPool; }
        }
        
        public int PagerConurrency
        {
        	get {return _pagerConcurrency;}
        }

        public KeyScript.Host.Loader ScriptLoader { get { return mScriptLoader; } }
        

        public CommandProcessor CommandProcessor
        {
            get { return _commandProcessor; }
        }
        
        public SceneManagerBase SceneManager
        {
            get { return _sceneManager; }
            set { _sceneManager = value; }
        }

        //public CmdConsole.Console Console { get { return _console; } }

        public bool UsingAngleSystemDegrees
        {
            get { return _usingDegrees; }
        }

        private int mSeed;
        public int Seed
        {
            get { return mSeed; }
            set { mSeed = value; }
        }

        public bool ArcadeEnabled
        {
            get { return mSimulationEnabled; }
            set { mSimulationEnabled = value; }
        }

        public Settings.Initialization Settings
        {
            get { return _ini; }
            set 
            {
                if (value == null) throw new ArgumentNullException("Core.Settings - ERROR: Ini cannot be null.");
                _ini = value; 
            }
        }


        public string DataPath
        {
            get { return _dataPath; }
        }

        public string AppPath
        {
            get { return _basePath; }
        }

        public string ModsPath
        {
            get { return _modsPath; }
        }

        public string SavesPath
        {
            get { return _savesPath; }
        }

        public string ScenesPath 
        {
        	get { return System.IO.Path.Combine (DataPath, "scenes");}
        }

        public string CurrentSaveName
        {
            get { return _primarySceneState.Name; }
            set { _primarySceneState.Name = value; }
        }
        // TODO: _currentSceneName here needs to be available to parts of the app trying to build
        //       relative paths, but sometimes we need to build the paths before the Scene object
        //       itself exists so we can't query the Scene yet... hack is to just set it here
        public string Scene_Name {get {return _primarySceneState.Name;} set { _primarySceneState.Name = value;}}

        public string Scene_FileName { get { return _primarySceneState.FileName; } set { _primarySceneState.FileName = value; } }

        public bool Scene_Changed { get { return _primarySceneState.Changed; } set { _primarySceneState.Changed = value; } }

        public bool Scene_IsUntitled { get { return _primarySceneState.IsUntitled; } set { _primarySceneState.IsUntitled = value; } }

        public Scene.SceneType Scene_Type { get { return _primarySceneState.Type; } set { _primarySceneState.Type = value; } }

        public SceneMode Scene_Mode { get { return _primarySceneState.Mode; } set { _primarySceneState.Mode = value; } }

        public SceneState SceneInfo { get { return _primarySceneState; } }

        public string RelativeLayerDataPath
		{
			get {return _relativeLayerDataPath;}
		}

        public static string FullNodePath(string relativePath)
        {
            if (_Core.ArcadeEnabled && relativePath.Contains(_Core._primarySceneState.Name))
                return Path.Combine(_Core._savesPath, relativePath);

            return System.IO.Path.Combine(Core._Core.ModsPath, relativePath);
        }

        public bool IsInitialized
        {
            get { return _isInitialized; }
        }
        
        public string GetNewName(Type type)
        {
            return Repository.GetNewName(type);
        }

        public void Shutdown()
        {
            _ini.Save();
            Terminate();
        }

        protected virtual void Terminate()
        {
            _isInitialized = false;
            
            _commandProcessor = null;

                 
            if (_sceneManager != null)
                _sceneManager.Dispose();
    
            _sceneManager = null;
            //_threadPool.Shutdown(); // TODO: if shutting down quickly after having added something new to scene, xmldb might have outstanding threads going
            //System.Threading.Thread.Sleep(1500);
            //_threadPool.Dispose(); // don't stop threadpool until after _sceneManager and all scenes are done
            System.Diagnostics.Debug.WriteLine("=============================BEGIN REPOSITORY DUMP");
            System.Diagnostics.Debug.WriteLine("Repository item count = " + Repository.Items.Length.ToString());
            if (Repository.Items.Length > 0)
                for (int i = 0; i < Repository.Items.Length; i ++)
                    System.Diagnostics.Debug.WriteLine (string.Format("Item[{0}] Type={1} ID={2}", i, Repository.Items[i].TypeName, Repository.Items[i].ID));
           
            System.Diagnostics.Debug.WriteLine("=============================END REPOSITORY DUMP");
            Debug.Assert(Repository.Items.Length == 0, "Repository.Terminate() - " + Repository.Items.Length.ToString() + " not removed.");
            // CoreClient._CoreClient.Engine.ReleaseAll();
        }
    }
}