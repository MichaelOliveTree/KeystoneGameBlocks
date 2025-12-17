#define EDIT 
#define USE_THREADED_GAME_LOOP
#define SPACE_SIM
//#define DEBUG_CONSOLE
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Keystone.Celestial;
using Keystone.IO;
using Keystone.Portals;
using Keystone.Resource;
using Keystone.Scene;
using Keystone.Types;
using Settings;

namespace KeyEdit
{
    public static class AppMain
    {
        // based on Tom Miller's managed render loop
        // https://blogs.msdn.com/tmiller/archive/2005/05/05/415008.aspx
        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern bool PeekMessage(out Message msg, IntPtr hWnd, 
                                            uint messageFilterMin, 
                                            uint messageFilterMax,
                                            uint flags);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool TranslateMessage(ref Message msg);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern Int32 DispatchMessage(ref Message msg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);



                
        private const uint PM_NOREMOVE = 0x0000; // Messages are not removed from the queue after processing by PeekMessage.
        private const uint PM_REMOVE = 0x0001; //Messages are removed from the queue after processing by PeekMessage.
        private const uint PM_NOYIELD = 0x0002; // Prevents the system from releasing any thread that is waiting for the caller to go idle (see WaitForInputIdle).
                                                // Combine this value with either PM_NOREMOVE or PM_REMOVE
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SETFOCUS = 0x0007;
        private const int WM_KILLFOCUS = 0x0008;
        private const int WA_INACTIVE = 0; // Deactivated
        private const int WA_ACTIVE = 1; // Activated by some method other than a mouse click (for example, by a call to the SetActiveWindow function or by use of the keyboard interface to select the window).
        private const int WA_CLICKACTIVE = 2; // Activated by a mouse click.

        private const int WM_QUIT = 0x0012;
        private const int WM_KEYDOWN = 0x0100; 
        private const int WM_KEYUP = 0x0101; 
        private const int WM_CHAR  = 0x0102 ;
        private const int WM_COMMAND = 0x0111;
        private const int VK_SPACE = 0x020 ;
        private const int VK_RETURN = 0x01C ;


        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        [StructLayout(LayoutKind.Sequential)]
        private struct Message
        {
            public IntPtr hWnd;
            public int msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }


#if USE_THREADED_GAME_LOOP
        private static Thread mGameThread;
        private static bool mRunning = false;
#endif 
        private static FormMain _form; // TODO: Inherit our window from Core and init Core with a window object
        private static FormPreview  mPreviewForm; 
        
        public static KeyPlugins.PluginServices PluginService;
        public static Keystone.CoreClient _core;
        public static KeyEdit.Network.InternetClient mKeyEditInternetClient; // instantiated from KeyEdit.FormMain.OnLoad()
        public static Keystone.Network.LoopbackServer mKGBLoopbackServer;    // instantiated from KeyEdit.FormMain.OnLoad()
        public static Scripting.ScriptingHost mScriptingHost;
        public static Keystone.Entities.Entity mPlayerControlledEntity;
        public static string mPlayerControlledEntityID;
        
        public static string HOST_ADDRESS = "127.0.0.1"; //localhost
        public static int HOST_PORT = 2023;

        private static Initialization _ini;
        private static string _appDataPath;
        private static string _configFile, _configPath;
        public static string mLoopBackServerDatabaseFullPath;

        public static string STARTUP_PATH = Application.StartupPath;
        public static string BASE_PATH = STARTUP_PATH + "\\..\\..\\..\\.."; // using x86 need extra \\.., using AnyCPU, dont need
        public static string DATA_PATH = BASE_PATH + "\\data"; 
        public static string MOD_PATH = DATA_PATH + "\\mods"; 
        public static string SCENES_PATH = DATA_PATH + "\\scenes\\";
        public static string CURRENT_SCENE_NAME; 
        public static string SAVES_PATH;

        public static string ModName = "caesar"; // "common.zip"; // TODO: remove hardcoded ModName

        public static string COMPANY_NAME = "SciFiCommand";
        public static string CONFIG_FILENAME = "KGBClient.config";
        public static string CONFIG_DEFAULTS_FILENAME = "KGBClient_defaults.config";


        public static readonly string OPEN_SCENE_FILTER = "Scene Files|SceneInfo.xml";
        public static readonly string OPEN_SAVE_FILTER = "Saved Campaigns|save.db";
        private static Keystone.Simulation.GameTime mGameTime;
        	
        public const bool EMPTY_UNIVERSE = false;
        public const uint REGIONS_ACROSS = 1;
        public const uint REGIONS_DEEP = 1;
        public const uint REGIONS_HIGH = 1;

        public const float CELL_WIDTH = 2.5f; 
        public const float CELL_DEPTH = 2.5f; 
        public const uint INTERIOR_TILE_COUNT_X = 16; // number of tiles in a cell along the x axis
        public const uint INTERIOR_TILE_COUNT_Z = 16; // number of tiles in a cell along the z axis
        public const float INTERIOR_TILE_WIDTH = CELL_WIDTH / (float)INTERIOR_TILE_COUNT_X;
        public const float INTERIOR_TILE_DEPTH = CELL_DEPTH / (float)INTERIOR_TILE_COUNT_Z;
        // ideally, our diameter should be just large enough to contain the radius of any world's oribt so that
        // they don't have to be switched between zones.  So if we must have smaller orbits to have smaller zones 
        // we will... if we have to.
        public const float MAX_FLOORPLAN_SIZE = 500f; //5kx5k meters is absolute max 
        public const float SIMPLE_SCENE_DIAMETER = 74799000000000f;
        public const float REGION_DIAMETER = 74799000000000f; // 74.799 trillion meters = 74,799,000,000,000 meters = 500 AU;
        // NOTE: It takes an octree depth of 20 to get a cell diameter of 142k meters.
        // NOTE The largest known star  http://en.wikipedia.org/wiki/VY_Canis_Majoris    is  a red hypergiant with a radius of 1800–2,100 AU!  My star sectors
        // simply cannot contain such a star.  Pluto's distance if 39.5AU so this star would gobble up every planet in our solar system.
  
   //     public const float SIMPLE_SCENE_DIAMETER = 10000;
   //     public const float REGION_DIAMETER = 10000; 
        //public const float REGION_DIAMETER = 5900000000000; // ~39.5 AU which is pluto's distance
        //public const float GRAVITY = -9.8f;  // TODO: not used here yet but only cuz its too annoying at the moment to move it here from SceneBase
        public const float FARPLANE_LARGE = 10000000000f;
        public const float NEARPLANE_LARGE = 100000.0f;

        // 10000000000000 meters (10 trillion meters) =  66.8458134 AU
        // 100000000000000 meters (100 trillion meters) 668.458134 AU (our sector max size is 500 AU)
        public const double MAX_VISIBLE_DISTANCE= 100000000000000; 

#if LOGZ
        public const float FARPLANE = 10000000000f;
        public const float NEARPLANE = 1f;
#else
        public const float FARPLANE = 100000f; // is used for space scenes, 40000f for land
        public const float NEARPLANE = 1.0f;
#endif
        public const float ORTHO_ZOOM = 100f;
        public const float FOV = 45f;

        
		#if SPACE_SIM
        public const double MINIMUM_PICK_DISTANCE = .1D, MAXIMUM_PICK_DISTANCE = 1000000000000000; // = 1+fifteen zeros = 6,684.58712 AU  //double.MaxValue; // 10000f;
		#else
        public const double MINIMUM_PICK_DISTANCE = .01D, MAXIMUM_PICK_DISTANCE = 1000000; 
		#endif
		
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // NOTE: The primary purpose of the property bag is to fully decouple the get/set of Scene node's values (properties)
            // from the GUI editing/display of those settings.  Recall that the PropertyGrid Control directly edits the object that
            // it's referencing thus one would normally have say OctreeNode referenced directly by the Grid and thus editing the grid
            // would directly edit the OctreeNode.  That is bad and it makes an elegant multithreaded renderer impossible.  Since however we
            // use a proxy object (the PropertyBag) to store and define the properties of an object, we can then "submit" those changes
            // to the intended object any way we please.
            // Thus, since we have these property bags for that purpose, we might as well use them here for INI as well.
            // TODO: Wait I'm confused.  If we for instance click on a ComplexModel in the scene, how do we populate the PropertyBag's values
            // to show the current values of that scene object including it's child nodes?
            // Well i guess the point is, since the Update() thread runs in the main thread, then any Reads against the scene will be done once
            // and then since changes to the PropertBag will only get committed at the end of hte Update() loop, it will only write to the scene once
            // as opposed to being able to directly edit a scene object would be random locks to the scene.
#if EDIT
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

			DATA_PATH = System.IO.Path.GetFullPath (BASE_PATH + "\\data");
 			MOD_PATH = DATA_PATH + "\\mods";

            _appDataPath = Initialization.CreateApplicationDataFolder(COMPANY_NAME);
            _configPath = System.IO.Path.Combine(_appDataPath, "config");
            SAVES_PATH = System.IO.Path.Combine(_appDataPath, "saves\\");
            mLoopBackServerDatabaseFullPath = System.IO.Path.Combine(_appDataPath, "server\\");

            System.IO.Directory.CreateDirectory(SAVES_PATH);
            System.IO.Directory.CreateDirectory(_configPath);
            System.IO.Directory.CreateDirectory(mLoopBackServerDatabaseFullPath);
            mLoopBackServerDatabaseFullPath = System.IO.Path.Combine(mLoopBackServerDatabaseFullPath, "server.db");
            
            _configFile = Initialization.GetConfigFilePath(_configPath, CONFIG_FILENAME, Properties.Resources.KGBClient);
            

            PropertyBagCollection bags;
            if (!Initialize(false, out bags))
                return;

            // the following line will usually throw a "module not found" exception if the proper version of DirectX is not installed.
            try
            {
                bool multithreadingEnabled = true;
                _core = new Keystone.CoreClient(_ini, BASE_PATH, DATA_PATH, MOD_PATH, SAVES_PATH, multithreadingEnabled);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                MessageBox.Show("A required module was not found. Do you have the latest DirectX Runtime Installed?  If not download and install it free from www.microsoft.com/directx");
                Trace.WriteLine(ex.Message);
                return;
            }

            if (!ApplySettings(bags))
                return;


#if EDIT  // create the editor
            _form = new FormMain(_core);
#else // client 
            _form = new FormClient (_core, _ini);
                      
#endif  


			
            PluginService = new PluginHost.EditorHost(((FormMainBase)_form).CommandCompleted);
            //Call the find plugins routine, to search in our Plugins Folder
            PluginService.FindPlugins(AppMain.BASE_PATH + @"\Plugins", AppMain.MOD_PATH, AppMain.ModName);

            Scripting.DatabaseAPI databaseAPI = new Scripting.DatabaseAPI();
            Scripting.GameAPI gameAPI = new KeyEdit.Scripting.GameAPI ();
            Scripting.GraphicsAPI graphicsAPI = new KeyEdit.Scripting.GraphicsAPI();
            Scripting.EntityAPI entityAPI = new KeyEdit.Scripting.EntityAPI();
            Scripting.PhysicsAPI physicsAPI = new Scripting.PhysicsAPI();
            Scripting.AIAPI  aiAPI = new KeyEdit.Scripting.AIAPI();
            Scripting.VisualFXAPI visualAPI = new KeyEdit.Scripting.VisualFXAPI();
            Scripting.AudioFXAPI audioAPI = new KeyEdit.Scripting.AudioFXAPI();
            Scripting.AnimationAPI animationAPI = new KeyEdit.Scripting.AnimationAPI();
            AppMain.mScriptingHost  = new Scripting.ScriptingHost(databaseAPI,
                                                                  gameAPI,
                                                                  graphicsAPI,
                                                                  entityAPI, 
                                                                  physicsAPI,
                                                                  aiAPI,
                                                                  visualAPI, 
                                                                  animationAPI,
                                                                  audioAPI);
            //  NOTE: Since the vars used by the BaseScript are all static, all user scripts derived from BaseScript
            // will have access to these initialized static variables
            KeyScript.DefaultScript.Initialize(mScriptingHost);


            // Timing
            float gameSecondsPerRealLifeSecond = 1.0f; // 60.0f * 5.0f; //60 gameSeconds per real life second means every real life minute results in one hour of game time passing

            // TODO: gameTime needs to be accessible to animations and scripts
            mGameTime = new Keystone.Simulation.GameTime(gameSecondsPerRealLifeSecond);

#if USE_THREADED_GAME_LOOP
             // being able to run the win32 GUI and the simulation in a seperate thread is useful.
             // The good thing is our main GUI thread will barely be using any cycles most of the time.
             mGameThread = new Thread(new ThreadStart(MultiThreadedGameLoop));
             mGameThread.Priority = ThreadPriority.Normal;
             mGameThread.SetApartmentState (ApartmentState.STA);
             mRunning = true;
             mGameThread.Start();

             // here we want to start up the FormMain but we don't want for it to
             // wire up the OnApplicationIdle.
             // NOTE: since all gui changes to the scene go through command processor
             // our app should already be very thread safe for this.
             // We only need some global vars for when we need to completely "pause" or
             // shut down
#endif
            
            
            
             // NOTE: we dont override the mainform's Closing event.  The mainform will shutdown the engine since it also
             // init's the engine to use the document form's handle
             // Application.Run(_form); // once Application.Run is called execution will not continue beyond this line in this function
           
             GUILoop();
        }

        public static bool ApplicationHasFocus = false;

        internal static FormMain Form{ get {return _form;}}
        
        internal static FormPreview PreviewForm { get {return mPreviewForm;} set {mPreviewForm = value;}}
        
        public static string ConfigFolderPath { get { return _configPath; } }

        public static string ConfigFilePath { get { return _configFile; } }

        internal static void SetTimeScale (float scale)
        {
        	mGameTime.Scale = scale;
        }
        
        private static void GUILoop()
        {
        	System.Diagnostics.Debug.WriteLine("AppMain.GUILoop() - ENTERING.");
        	System.Diagnostics.Debug.WriteLine("AppMain.GUILoop() - Begin Show Form.");
        	_form.Show();
            Message msg;

            System.Diagnostics.Debug.WriteLine("AppMain.GUILoop() - ENTERING message pump.");
            while (mRunning)
            {
                // http://www.gamedev.net/topic/296990-getmessage-vs-peekmessage/
                while (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                //if (PeekMessage(ref msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    if (msg.msg == WM_QUIT) break;
                    else if (msg.msg == WM_CHAR)
                    {
                        // the keyboard will store up to 256 characters 
                        // and the observer of the keyboard will be able to
                        // decide if it wants to respond to these or the dinput keyboard.
                        // NOTE: WM_CHAR messages require non exclusive keyboard mode
                        _core.Keyboard.OnWindowsCharReceived((char)msg.wParam);
                    }
                    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms646373(v=vs.85).aspx
                    // http://blogs.msdn.com/b/oldnewthing/archive/2008/05/23/8535427.aspx
                    TranslateMessage(ref msg); 
                    DispatchMessage(ref msg);
                }

                // Render a frame during idle time (no messages are waiting)
                if (_core.IsInitialized)
                {
#if USE_THREADED_GAME_LOOP

#else
                    SingleThreadedGameLoop();
#endif
                }
                else
                {
                    // clear the input
                    ClearInput();
                    Thread.Sleep(0);
                }
            }

            System.Diagnostics.Debug.WriteLine("AppMain.GUILoop() - EXITED message pump.");
            

#if USE_THREADED_GAME_LOOP
            // stop the game thread
            mRunning = false;
            // wait for the game thread to stop if it's running.. the game thread
            System.Threading.Thread.Sleep(1000);
#endif
        	System.Diagnostics.Debug.WriteLine("AppMain.GUILoop() - EXITING.");
        }


        public static int LowWord(int number)
        { 
            return number & 0x0000FFFF; 
        }

#if USE_THREADED_GAME_LOOP

static int mTimerStartPeriod;
        private static void MultiThreadedGameLoop()
        {
        	
            System.Diagnostics.Debug.WriteLine("AppMain.MultiThreadedGameLoop() - Entering Multithreaded Game Loop.");
            
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            
            while (mRunning)
            {
                // the Engine.AccurateTimeElapsed is updated only at each RenderToScreen/RenderTootherHwnd.
				// It takes the time between the last RenderToScreen and the current one.
            	// TODO: I think i need seperate .AccurateTimeElapsed for each viewport and accumulate the results?
            	// http://www.truevision3d.com/forums/tv3d_sdk_65/accumlative_accuratetimeelapsed-t20127.0.html
            	// TODO: using stopWatch helps but the elapsedTime seems to be running faster than it should.  No idea why
            	double elapsedSeconds = stopWatch.ElapsedMilliseconds / 1000d; // _core.Engine.AccurateTimeElapsed() / 1000d;
            	stopWatch.Reset();
                stopWatch.Start();
                        
            	if (_form.IsHandleCreated)
                {
                    // mKGBLoopbackServer is instantiated and assigned in FormMain.cs.OnLoad()
                    if (mKGBLoopbackServer != null)
                        mKGBLoopbackServer.Update();

                    // mKeyEditInternetClient is KeyEdit.Network.InternetClient.cs and not Lidgren.Network.NetClientBase.cs
                    // mKeyEditInternetClient is instantiated and assigned in FormMain.cs.OnLoad()
                    // mKeyEditInternetClient hosts a Lidgren.NetUDPClient for Authentication, Lobby and GameServer connections and command processing
                    // as well as a state machine to manage transitioning between those 3 connections.
                    if (mKeyEditInternetClient != null)
                    {
                        // mKGBInternetClient processes Authentication, Lobby and GameClient command processing.
                        // mKGBInternetClient.Update results in trigger of FormMain.Commands.UserMessageReceived()
                        mKeyEditInternetClient.Update();

                        // NOTE: For dedciated game thread, we do NOT process completed commands here
                        ((FormMainBase)_form).ProcessCompletedCommandQueue();
                    }

                    if (_core.SceneManager == null || _core.SceneManager.Scenes == null || _core.SceneManager.Scenes.Length == 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    // june.13.2013 - MPJ - TODO: CheckInput() and loopback and netclient
                    //                        perhaps should be moved to AppIdle main gui thread
                    //                        since it wont affect threadpool threads from running in background
                    //                        and ProcessCompletedCOmmandQueue should be moved here instead
                    //                        where we can apply results of the completed commands to the scene
                    //                        
                    CheckInput();

                    // simulation update is for updating positions of traveling things
                    // and keeping game state up to date.  But as far as 
                    // updating animations for moving things i'm not sure.  Ideally i wouldnt want to
                    // but i can forsee problems whereby the animation of an actor or particle system
                    // if too much time lapses between updates, where the actor or particle system cant cope
                    // and something screws up?  But ideally, i would want Scene.Update to actually just be
                    // scene Culler... and to flag the Visible path as it goes.  This means that it will update
                    //bounding volumes for things that have moved or scaled during the _simulation.Update() as needed.(i.e. lazy update)
                    // and for items that just move, we dont need to recompute the local box just apply the translation
                    // to the matrix 
                    if (ApplicationHasFocus)
                    {
                    	

    	                // If paused, we want to send elapsedSeconds == 0 to scene.Update()
                		// This will suspend animations during scene.Update(0)
// TODO:           		if (scene.Simulation.Paused)
//                    		elapsedSeconds = 0;
                
                    	mGameTime.Update (elapsedSeconds);
                     	
                        
                    	// CULL & RENDER
                        _core.SceneManager.Update(mGameTime);


                    }
                    else
                    {
                        // clear the input since we did not call CheckInput();
                        ForceKeyReleases();
                        ClearInput();

                        Thread.Sleep(10);
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine("AppMain.MultiThreadedGameLoop() - EXITING Multithreaded Game Loop.");
        }
#else



		
		
        // TODO: should this be on a seperate thread?
        // We wont use a dedicated render thread though, but we want a seperate
        // thread of the game loop and the normal win32 forms message pump.
        // This way all of those should update regardless of modal forms and such.
        private static void SingleThreadedGameLoop()
        {
            // TODO: I'm not properly updating the _updateElapsed so for now,since i'm still just runing single threaded, keep using regular render time elapsed
            mElapsedMilliseconds = (int)_core.Engine.AccurateTimeElapsed(); //_core.Engine.TimeElapsed(); //_core.Timer.Elapsed(_updateElapsed);
            bool hasFocus = _form.Visible && _form.WindowState != FormWindowState.Minimized; //&& GetFocus() == _form.Handle;
            //hasFocus = true;

            if (mKGBLoopbackServer != null)
                mKGBLoopbackServer.Update();

            if (mKeyEditInternetClient != null)
            {
                // mKeyEditInternetClient contains Lidgren.NetUDPClient for Authentication, Lobby and GameServer
                mKeyEditInternetClient.Update();

                // if threaded, we want to process these on the main gui thread
                // but we don't want to wait for the gui thread to complete these because
                // if we do, we'll still be blocked by anything that blocks the GUI.  
                // So we need for the main gui to process completed commands in it's appidle 
                // 
                ((FormMainBase)_form).ProcessCompletedCommandQueue();
            }

            if (_core.SceneManager == null || _core.SceneManager.Scenes == null || _core.SceneManager.Scenes.Length == 0)
            {
                Thread.Sleep(100);
                return;
            }
            
            CheckInput();

            // simulation update is for updating positions of traveling things
            // and keeping game state up to date.  But as far as 
            // updating animations for moving things i'm not sure.  Ideally i wouldnt want to
            // but i can forsee problems whereby the animation of an actor or particle system
            // if too much time lapses between updates, where the actor or particle system cant cope
            // and something screws up?  But ideally, i would want Scene.Update to actually just be
            // scene Culler... and to flag the Visible path as it goes.  This means that it will update
            //bounding volumes for things that have moved or scaled during the _simulation.Update() as needed.(i.e. lazy update)
            // and for items that just move, we dont need to recompute the local box just apply the translation
            // to the matrix 
            if (hasFocus)
            {
                float renderelapsedMilliseconds = _core.Engine.AccurateTimeElapsed(); 
                              
                _core.SceneManager.Update(renderelapsedMilliseconds);
            }
            else
            {
                // clear the input since we did not call CheckInput();
                ClearInput();
                Thread.Sleep(10);
            }
        }
#endif

        /// <summary>
        /// Only needs to be called if Mouse.Update and Keyboard.Update are not called
        /// via CheckInput()
        /// </summary>
        private static void ClearInput()
        {
            if (_core.Mouse != null) _core.Mouse.Clear();
            if (_core.Keyboard != null) _core.Keyboard.Clear();
        }

        private static void ForceKeyReleases()
        {
            if (_core.Mouse != null) _core.Mouse.ForceKeyReleases();
            if (_core.Keyboard != null) _core.Keyboard.ForceKeyReleases();
        }

        private static void CheckInput()
        {

            // TODO: can i grab the WM_CHAR here first?

            _core.Mouse.Update();
            _core.Keyboard.Update();
        }


        private static bool Initialize(bool showControlPanel, out PropertyBagCollection bags)
        {
#if DEBUG_CONSOLE
            _console = DebugConsole.Instance;
            _console.Init(true, true);
#endif

            bags = null;

            #region Obsolete TV BetaKey Code but useful perhaps for User's RegKey or Login

            //try
            //{
            //    _ini = LoadSettings("license.config", "");
            //    // pass "" for default license so we can launch control panel if necesseceary
            //    bags = new PropertyBagCollection(_ini);
            //    Core =
            //        new Core.Core((string) bags.Properties["betaname"], (string) bags.Properties["betakey"], BASE_PATH);
            //}
            //catch
            //{
            //    try
            //    {
            //        // the license file does not exist.  So we launch the control panel  
            //        _ini = LoadSettings("", "license_default.config");
            //        bags = new PropertyBagCollection(_ini);
            //        if (!GetUserSettings("license.config", bags)) return false;
            //        Core =
            //            new Core.Core((string) bags.Properties["betaname"], (string) bags.Properties["betakey"],
            //                          BASE_PATH);
            //    }
            //    catch
            //    {
            //        //TODO: add log entry 
            //        return false;
            //    }
            //}

            #endregion

           
            // now try to load the settings
            try
            {
                // before loading the control panel, we need to create a property bag collection from the ini file settings
                _ini = LoadSettings(_configFile);
                bags = new PropertyBagCollection(_ini);
                
                if (showControlPanel) 
                    return GetUserSettings(_configFile, bags, ref _ini);

                return true;
            }
            catch
            {
                //TODO: add log entry 
                return false;
            }
        }

        /// <summary>
        /// Display the pre-launch app config control panel
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="bags"></param>
        /// <param name="ini"></param>
        /// <returns></returns>
        private static bool GetUserSettings(string filename, PropertyBagCollection bags, ref Initialization ini)
        {
            try
            {
                ControlPanel cp = new ControlPanel(bags);
                cp.ShowDialog();
                if (cp.DialogResult != DialogResult.OK)
                    return true; // return without saving changes

                // save the property grid changes to the user file and fill the ini ref with any of the changes
                SaveSettings(bags, filename, ref ini);
                return true;
            }
            catch (Exception ex)
            {
                //TODO: add log entry 
                Debug.WriteLine("AppMain:GetUserSettings() - " + ex.Message);
                return false;
            }
        }


        private static void SaveSettings(PropertyBagCollection bags, string filename, ref Initialization ini)
        {
            ini = bags.ToInitializationObject();
            Initialization.Save(filename, ini);
            _core.Settings = ini; // core settings needs to point to new ini
        }

        private static Initialization LoadSettings(string configfilename)
        {
            Initialization ini = null;
            try
            {
                // attempt to load settings from the users personal config settings
                ini = Initialization.Load(configfilename);
            }
            catch (Exception ex)
            {
            }
            
            if (ini == null) throw new ArgumentNullException();
            return ini;
        }



        private static bool ApplySettings(PropertyBagCollection bagCollection)
        {
            // at this point we "could" pass the control panel a reference to the System controller so that it can directly
            // send the changed settings to the engine via System.RunCommand.  NOTE: once "launch" is clicked, these values are no
            // longer updated in the cp.InitializationFile
            // instance the System controller so that we can start processing the cvars


            string bindsFilePath = Settings.Initialization.GetConfigFilePath(_configPath, "binds_system.config", Properties.Resources.binds_system);     
            _core.SystemIOController = new Keystone.Controllers.SystemController(_core, bindsFilePath);

            // apply the settings
            foreach (PropertySpec spec in bagCollection.Properties)
            {
                // some commands like fullscreen have more args than just true/false.
                // we need to pass resolution, colordepth, a window handle for windowed, etc.
                Debug.WriteLine("AppMain.ApplySettings() - cvar - " + spec.Name + " " + spec.DefaultValue.ToString());
                ((Keystone.Controllers.SystemController)_core.SystemIOController).RunCommand(spec.Name + " " +
                                                                              spec.DefaultValue.ToString());
            }


            // obsolete after switching PropertyBagCollection to inherit from PropertyTable which itself inherits from PropertyBag
            //foreach (string cvar in bagCollection.Properties)
            //{
            //    if (bagCollection.PropertyBags[cvar] != null)
            //    {
            //        foreach (PropertySpec  spec in bagCollection.PropertyBags[cvar].Properties)
            //        {
            //            // some commands like fullscreen have more args than just true/false.
            //            // we need to pass resolution, colordepth, a window handle for windowed, etc.
            //            Debug.WriteLine("cvar - " + spec.Name + " " + spec.DefaultValue.ToString ());
            //            ((Keystone.Controllers.SystemController)_core.SystemIOController).RunCommand(spec.Name + " " +
            //                                                                          spec.DefaultValue.ToString());
            //        }
            //    }
            //}
            return true;
        }
    }
}