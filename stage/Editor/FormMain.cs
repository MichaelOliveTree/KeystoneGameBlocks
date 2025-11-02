using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Keystone.Devices;
using Keystone.Scene;


namespace KeyEdit
{
    public partial class FormMain : FormMainBase 
    {
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SETFOCUS = 0x0007;
        private const int WM_KILLFOCUS = 0x0008;
        private const int WA_INACTIVE = 0; // Deactivated
        private const int WA_ACTIVE = 1; // Activated by some method other than a mouse click (for example, by a call to the SetActiveWindow function or by use of the keyboard interface to select the window).
        private const int WA_CLICKACTIVE = 2; // Activated by a mouse click.

        private const string ALL_GEOMETRY_FILE_FILTER = "x files (*.x)|*.x| tvm files (*.tvm)|*.tvm| tva files (*.tva)|*.tva|obj files (*.obj)|*.obj|all supported files (*.x; *.tvm;*.obj)|*.x; *.tvm;*.obj|All files (*.*)|*.*";
        private const string ACTOR_GEOMETRY_FILE_FILTER = "x files (*.x)|*.x| tva files (*.tva)|*.tva| All files (*.*)|*.*";
        private const string STATIC_GEOMETRY_FILE_FILTER = "x files (*.x)|*.x| tvm files (*.tvm)|*.tvm|obj files (*.obj)|*.obj|all supported files (*.x; *.tvm;*.obj)|*.x; *.tvm;*.obj|All files (*.*)|*.*";
        private const string EDITABLE_MESH_FILE_FILTER = "obj files (*.obj)|*.obj";
        private const int DEFAULT_FILTER_INDEX = 4;
        private const string TEXTURE_FILE_FILTER = "bmp files (*.bmp)|*.bmp|png files (*.png)|*.png|dds files (*.dds)|*.dds|jpg files (*.jpg)|*.jpg |gif files (*.gif)|*.gif |tga files (*.tga)|*.tga|All supported files (*.bmp; *.png;*.dds; *.jpg; *.gif; *.tga)|*.bmp; *.png;*.dds;*.jpg;*.gif;*.tga|All files (*.*)|*.*";
        private const int TEXTURE_FILTER_INDEX = 7;

        private const string EDITOR_LAYOUT_FILENAME = "layouts\\editor.layout";
        private const string EDITOR_DEFINITION_FILENAME = "layouts\\editor.definition";

        private const string TITLE_SFC = "SciFi Command";
        private const string TITLE_KGB = "Keystone Game Blocks (KGB)";

        public FormMain() : base() 
        {
            InitializeComponent();
            ribbonControl = ribbon;
        }

        public FormMain(Keystone.CoreClient core) : base (core)
        {
            InitializeComponent();
            ribbonControl = ribbon;

            //CheckForIllegalCrossThreadCalls = false;

        }

        // MainForm_Load is called on Application.Run(_window) and AFTER the Form's constructor.
        protected override void OnLoad(EventArgs e)
        {
        	MRU_Initialize();
        	
            if (!DesignMode)
            {
                

                // we'll use the form's handle to init our graphics, but we'll only render to secondary viewports.  
                // This means that our ViewManager doesnt even have to retain the array of viewports and do some juggling act.
                // Instead, we just need to remember that cameras have viewports assigned to them, and each Scene instance
                // has Cameras added to them.  So only the active scene being rendered uses only the cameras added to it.
                // So our Intro screen can use an entirely seperate Scene with it's own camera and viewport assigned to it.
                IntPtr handle = this.Handle;
                GraphicsDevice graphics = new GraphicsDevice(handle);
                System.Diagnostics.Debug.WriteLine("FormMain.OnLoad() - Primary Graphics handle == " + handle.ToString());
                graphics.SwitchWindow();
                AppMain._core.Initialize(graphics);


                // TODO: since the loopback server and the mNetClient are outside of Keystone.dll
                // how do we get Keystone.DLL simulation for instance send a message on behalf
                // of the user if it doesn't have access to the network?
                // - a) we can generate an event that then the app.exe must respond to
                // - b) we assign a variable in CoreClient for the keystone.dll to gain access to mNetClient
                //
                // loopback server is used whenever we do NOT connect to an external server.  Loopback server is default
                // TODO: a call to ConnectToServer should ideally start loopback or swap to any other if a current server is already set
                if (!System.IO.File.Exists(AppMain.mLoopBackServerDatabaseFullPath))
                {
                    CreateLoopbackServerDatabase();

                    // create our default user record
                    // TODO: i DO NOT think i should have two different User objects, one in AppDatabaseHelper and the other in KeyCommmon.DatabaseEntities.
                    string userName = AppMain._core.Settings.settingRead("network", "username");
                    CreateUserRecord(userName);
                }

                AppMain.mLoopbackServer = new Keystone.Network.LoopbackServer(AppMain.HOST_ADDRESS, AppMain.HOST_PORT, AppMain.BASE_PATH);
                AppMain.mLoopbackServer.UserMessageReceivedLoopbackHandler = UserMessageReceivedLoopback;
                

                // create the client and connect it to the loopback server
                AppMain.mNetClient = new KeyEdit.Network.LoopBackClient(_core.Settings, null);
                AppMain.mNetClient.ConnectTo(AppMain.HOST_ADDRESS, AppMain.HOST_PORT);
                AppMain.mNetClient.UserMessageReceivedHandler = UserMessageReceived;
                AppMain.mNetClient.UserMessageSendingHandler = UserMessageSending;
                // scene manager to hold each of our scenes
                ClientSceneManager sm = new ClientSceneManager(_core);

                string layoutFile = Settings.Initialization.GetConfigFilePath(AppMain.ConfigFolderPath, EDITOR_LAYOUT_FILENAME, Properties.Resources.editor_layout);
                string definitionFile = Settings.Initialization.GetConfigFilePath(AppMain.ConfigFolderPath, EDITOR_DEFINITION_FILENAME, Properties.Resources.editor_definition);
                mWorkspaceManager = new KeyEdit.Workspaces.WorkspaceManager(this, dotNetBarManager, _core.Settings, definitionFile, layoutFile, OnDocumentTabClosing);

                //dotNetBarManager.

                base.OnLoad(e);
            }
        }

        #region View Document Tabs
        private void buttonCodeEditor_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            Scene scene = ws.Scene;

            mWorkspaceManager.Add(new Workspaces.CodeEditorWorkspace ("Script Editor"), scene);
        }


        private void buttonBehaviorTree_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            Scene scene = ws.Scene;

            mWorkspaceManager.Add(new Workspaces.BehaviorTreeEditorWorkspace("Behavior Tree Editor"), scene);
        }

        private void buttonComponentEditor_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase editorWS = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            if (editorWS.SelectedEntity == null || editorWS.SelectedEntity.Entity == null)
            {
                MessageBox.Show("You must first select a component to edit by LEFT MOUSE clicking on it with the mouse.", "Component not selected.");
                return;
            }
            // TODO: wouldn't it be better if when selecting the component editor
            //       a "Component Edit" button became active?  well for now let's
            //       This way we don't ahve to 
            Scene scene = editorWS.Scene;

            KeyEdit.Workspaces.ComponentDesignWorkspace componentDesignWorkspace =
                new KeyEdit.Workspaces.ComponentDesignWorkspace("Component Editor", editorWS.SelectedEntity.Entity, AppMain.INTERIOR_TILE_WIDTH, AppMain.INTERIOR_TILE_DEPTH);
            mWorkspaceManager.Add(componentDesignWorkspace, scene);

            componentDesignWorkspace.Configure(mWorkspaceManager, scene);
            mWorkspaceManager.ChangeWorkspace(componentDesignWorkspace.Name, false);


        }

        private void buttonHardpointEditor_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase hardpointEditorWS = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            if (hardpointEditorWS.SelectedEntity == null || hardpointEditorWS.SelectedEntity.Entity == null)
            {
                MessageBox.Show("You must first select a Vehicle to edit by LEFT MOUSE clicking on it with the mouse.", "Vehicle not selected.");
                return;
            }

            Scene scene = hardpointEditorWS.Scene;

            KeyEdit.Workspaces.HardpointEditorWorkspace hardpointsEditorWorkspace =
                new KeyEdit.Workspaces.HardpointEditorWorkspace("Hardpoints Editor", (Keystone.Vehicles.Vehicle)hardpointEditorWS.SelectedEntity.Entity);
            mWorkspaceManager.Add(hardpointsEditorWorkspace, scene);
        }

        private void buttonFloorplanEditor_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);

            if (ws.SelectedEntity == null || ws.SelectedEntity.Entity is Keystone.Vehicles.Vehicle == false)
            {
                MessageBox.Show("You must first select a Vehicle who's floorplan you wish to view by LEFT MOUSE clicking on it with the mouse.", "Vehicle not selected.");
                return;
            }

            Scene scene = ws.Scene;
            KeyEdit.Workspaces.FloorPlanDesignWorkspace floorPlanWorkspace =
                new KeyEdit.Workspaces.FloorPlanDesignWorkspace("Floorplan Editor", (Keystone.Vehicles.Vehicle)ws.SelectedEntity.Entity);
            mWorkspaceManager.Add(floorPlanWorkspace, scene);

            floorPlanWorkspace.Configure(mWorkspaceManager, scene);
            mWorkspaceManager.ChangeWorkspace(floorPlanWorkspace.Name, false);
        }

        private void buttonTextureEditor_Click(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                //ClientScene scene = (ClientScene)mWorkSpace.Scene;
                // our new model is that each Document requires it's own workspace
                // always add a new ProceduralTextureWorkspace unless it's the exact same node
                // TODO: maybe we can prompt user for a new name for the procedural texture?
                mWorkspaceManager.Add(new Workspaces.ProceduralTextureWorkspace("Procedural Texture"), null);
            }
        }

        private void buttonTactical_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            //        	if (AppMain.mLocalVehicle == null)
            //        		return;

            // TODO: if the TacticalWorkspace is already active, make it the active tab.


            // the local player's Vehicle must be instantiated and assigned to AppMain.mLocalVehicle
            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            //
            //            if (ws.Selected == null || !(ws.Selected.Entity is Keystone.Vehicles.Vehicle))
            //            {
            //                MessageBox.Show("You must first select a Vehicle.", "Vehicle not selected.");
            //                return;
            //            }

            Scene scene = ws.Scene;
            // TODO: selected entity should not have to be the vehicle!!
            // TODO: if no vehicle assigned to the main workspace, cannot open TacticalWorkspace.
            //       simply pop-up a message box explaining.
            //            Keystone.Vehicles.Vehicle vehicle = (Keystone.Vehicles.Vehicle)ws.Selected.Entity;
            KeyEdit.Workspaces.TacticalWorkspace tacticalWorkspace =
                new KeyEdit.Workspaces.TacticalWorkspace("Tactical");

            mWorkspaceManager.Add(tacticalWorkspace, scene);
        }


        private void buttonNavigation_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            // Sept.30.2016 - player vehicle does not have to be set to view Starmap
            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            //
            //            if (ws.Selected == null || !(ws.Selected.Entity is Keystone.Vehicles.Vehicle))
            //            {
            //                MessageBox.Show("You must first select a Vehicle.", "Vehicle not selected.");
            //                return;
            //            }

            // TODO: if the NavWorkspace is already active, make it the active tab.

            Scene scene = ws.Scene;
            // TODO: selected entity should not have to be the vehicle!!
            // TODO: if no vehicle assigned to the main workspace, cannot open NavWorkspace.
            //       simply pop-up a message box explaining.
            //            Keystone.Vehicles.Vehicle vehicle = (Keystone.Vehicles.Vehicle)ws.Selected.Entity;
            KeyEdit.Workspaces.NavWorkspace navWorkspace =
                new KeyEdit.Workspaces.NavWorkspace("Navigation");

            mWorkspaceManager.Add(navWorkspace, scene);
        }

        private void buttonShipSystems_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            Scene scene = ws.Scene;
            mWorkspaceManager.Add(new Workspaces.ShipSystemsWorkspace("Systems Management"), scene); // TODO: must associate with a vehicle yes?

        }

        private void buttonAdministration_Click(object sender, EventArgs e)
        {
            if (mWorkspaceManager == null || mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME) == null)
                return;

            Workspaces.WorkspaceBase ws = (Workspaces.WorkspaceBase)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            Scene scene = ws.Scene;

            mWorkspaceManager.Add(new Workspaces.CrewManagementWorkspace("Crew Management"), scene); // TODO: must associate with a vehicle yes?
        }


        #endregion

        //        protected override void WndProc(ref Message m)
        //        {
        //
        //        	
        //            if (m.Msg == WM_SETFOCUS)
        //            {
        //            	
        //                AppMain.ApplicationHasFocus = true;
        //                System.Diagnostics.Debug.WriteLine("AppMain.AppLoop() - GAINED FOCUS");
        //            }
        //            else if (m.Msg == WM_KILLFOCUS)
        //            {
        //                AppMain.ApplicationHasFocus = false;
        //                System.Diagnostics.Debug.WriteLine("AppMain.AppLoop() - LOST FOCUS");
        //            }
        //            else if (m.Msg == WM_ACTIVATE)
        //            {
        //                if (AppMain.LowWord((int)m.WParam) != WA_INACTIVE)
        //                {
        //                    AppMain.ApplicationHasFocus = true;
        //                    System.Diagnostics.Debug.WriteLine("AppMain.AppLoop() - GAINED FOCUS");
        //                }
        //                else
        //                {
        //                    AppMain.ApplicationHasFocus = false;
        //                    System.Diagnostics.Debug.WriteLine("AppMain.AppLoop() - LOST FOCUS");
        //                }
        //            }
        //
        //            base.WndProc(ref m);
        //        }

        protected override void OnMove(EventArgs e)
		{
			base.OnMove(e);
			if (mWorkspaceManager != null)
				mWorkspaceManager.Resize();
		} 
		protected override void OnActivated(EventArgs e)
		{
			
			base.OnActivated(e);
			AppMain.ApplicationHasFocus = true;
			System.Diagnostics.Debug.WriteLine("FormMain.OnActivated() - GAINED FOCUS");
		} 
		
		protected override void OnDeactivate(EventArgs e)
		{
			//base.OnDeactivate(e);
			//AppMain.ApplicationHasFocus = false;
			System.Diagnostics.Debug.WriteLine("FormMain.OnDeactivate() - LOST FOCUS");
		} 

        
        
        void ButtonOptionsClick(object sender, EventArgs e)
        {
        	buttonSettings_Click (sender, e);
        }
        
        /// <summary>
        /// Edit Network, Graphics, Audio, Control, Misc settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSettings_Click(object sender, EventArgs e)
        {
            PropertyBagCollection bags = new PropertyBagCollection(_core.Settings);

            ControlPanel cp = new ControlPanel(bags);
            cp.ShowDialog();
            if (cp.DialogResult != DialogResult.OK)
                return; // return without saving changes

            Settings.Initialization ini = bags.ToInitializationObject();
            Settings.Initialization.Save(_core.Settings.FilePath, ini);
            
            // update the settings object in _core
            _core.Settings = ini;
        }
        
        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Close(); // see FormMainBase.cs OnClosing() {}
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Close(); // see FormMainBase.cs OnClosing() {}
        }

        


        private void buttonItemResume_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            // TODO: its probably easiest to rename this to Edit Scene and then add a Resume Campaign buttton that opens from the \\saves path
            // also we can add a "Create" menu for new prefab and static scene design and then use "New" for campaigns
            // TODO: we can disable menu items on release of v1.0.  only v2.0 will allow for moding and editing new components
            openFile.InitialDirectory = AppMain.SAVES_PATH;
            openFile.Filter = AppMain.OPEN_SAVE_FILTER;
            openFile.FilterIndex = 1;
            openFile.RestoreDirectory = true;

            if (openFile.ShowDialog(this) == DialogResult.OK)
            {
                string folderPath = System.IO.Path.GetDirectoryName(openFile.FileName);

                // strip the machine specific path so we just have an agnostic relative folder path
                string relativeFolderPath = GetRelativePath(folderPath, AppMain.SAVES_PATH);
                ResumeSimulation(relativeFolderPath);
                this.Text = TITLE_SFC + " - " + relativeFolderPath;
            }
        }

        /// <summary>
        /// Strip the user machine specific path so we just have an agnostic relative folder path
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="pathToStrip"></param>
        /// <returns></returns>
        private string GetRelativePath (string fullPath, string pathToStrip)
        {
        	Uri fullURiPath = new Uri(fullPath, UriKind.Absolute);
			Uri relRoot = new Uri(pathToStrip, UriKind.Absolute);

			string relPath = relRoot.MakeRelativeUri(fullURiPath).ToString();
			relPath = Uri.UnescapeDataString(relPath);
			return relPath;
        }
        
        System.Collections.Generic.List <string> mMRUList = new System.Collections.Generic.List<string>();
        const int MAX_MRU_LENGTH = 10;
        
      //  void OpenScene (string sceneName)
      //  {
      //      this.Text = TITLE + " - " + sceneName;

      //      // TODO: Open should send a command to open?
      //      // How does htis work exactly?  I mean, what is open?  is it Resuming?
      //      // Well, in one form it's open for editing an unfinished scene...
      //      // normally for multiplayer, you would never open, you'd connect...
      //      AppMain.CURRENT_SCENE_NAME = sceneName;
      //      string relativefilePath = System.IO.Path.Combine (sceneName, "SceneInfo.xml");
      //  	if (!System.IO.File.Exists (System.IO.Path.Combine (AppMain.SCENES_PATH, relativefilePath))) 
      //  	{
      //  		MessageBox.Show ("Scene not found.", "Scene not found.");
      //  		return;
      //  	}
        	
      //  	// TODO: if a scene is already loaded, query user to load new simulation or re-load if existing?
    		//if (_core.SceneManager.Scenes != null && _core.SceneManager.Scenes[0] != null)
    		//{
    		//	// TODO: this should use the "open" scene code path
      //  		if (!QuerySave ())
      //  		{
      //  		 	return;
      //  		}	        	
    		//}
        	
    		//MRU_AddFolder (sceneName);

      //      //KeyCommon.Messages.Scene_Load message = new KeyCommon.Messages.Scene_Load();
      //      //message.FolderName = relativeFolderPath;
      //      //message.UnloadPreviousScene = true;

      //      KeyCommon.Messages.Scene_Load_Request message = new KeyCommon.Messages.Scene_Load_Request();
      //      message.FolderName = sceneName;
      //      // TODO: how do i handle simple scenes where we've not created a game database?
      //      //       - i could just create a database and not use it if it's not needed.
      //      //       Or when specifying the scene folder, server can query the SceneInfo and know
      //      //       what type of scene it is and whether it uses a db.

      //      // TODO: Scene_Load() I think we want to be a command sent from server to client
      //      //       and we want perhaps a Scene_Load_Request() instead.  The assumption here
      //      //       is that the user is resuming a saved simulation and not joining for first time.
      //      SendNetMessage(message);
      //  }

        private void ResumeSimulation(string simulationName)
        {
            AppMain.CURRENT_SCENE_NAME = simulationName;
            string relativefilePath = System.IO.Path.Combine(simulationName, "save.db");
            if (!System.IO.File.Exists(System.IO.Path.Combine(AppMain.SAVES_PATH, relativefilePath)))
            {
                MessageBox.Show("Simulation not found.", "Simulation not found.");
                return;
            }

            // TODO: if a scene is already loaded, query user to load new simulation or re-load if existing?
           // if (_core.SceneManager.Scenes != null && _core.SceneManager.Scenes[0] != null)
            //{
            //    // TODO: this should use the "open" scene code path
            //    if (!QuerySave())
            //    {
            //        return;
            //    }
            //}

            MRU_AddFolder(simulationName);
            

            KeyCommon.Messages.Simulation_Join_Request message = new KeyCommon.Messages.Simulation_Join_Request();
            message.FolderName = simulationName;

            // todo: i need to get the actual real SceneType from the SceneInfo
            _core.Scene_Type = SceneType.MultiReginSpaceStarsAndWorlds;
            _core.Scene_Mode = Keystone.Core.SceneMode.Simulation;

            SendNetMessage(message);
        }

        private void buttonItemNewSingleRegion_Click(object sender, EventArgs e)
        {
            //_core.Scene_Type = SceneType.SimpleComposition;
            
        	FormNewScene newScene = new FormNewScene();
            newScene.ShowDialog(this);
         
            if (newScene.DialogResult == DialogResult.OK )
            {
            	// NOTE: we now are no longer using zip file to save all the xmldb xml files within.
            	//       we are now using just unique folder name based on modname.
            	string modName = newScene.SceneName.ToLower();
 	
            	// TODO: querySceneUnload should be if existing scene is loaded and we want to load a new one
            	//       - it should have nothing to do with testing if we need to Save() existing first.
            	//       But before we even get here, we need to verify overwrite!  
            	string folderPath;
            	if (NewFileNameValidityTest(modName, out folderPath))
            	{
		            if (QuerySceneUnload())
		            {
                        _core.Scene_FileName = folderPath; // note: assign after QuerySceneUnload
                        _core.Scene_Type = SceneType.SingleRegion;
                        _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;

                        float diameter = newScene.Diameter;

		                KeyCommon.Messages.Scene_New message = new KeyCommon.Messages.Scene_New(modName, diameter, diameter, diameter);
		
		                SendNetMessage(message);
		                
		                MRU_AddFolder (message.FolderName);
		            }
            	}
            }
        }
                
        private void buttonItemNewFloorPlan_Click(object sender, EventArgs e)
        {
            // TODO: floorplan should require a mesh or modeled entity to which to use
            // as basis for the floorplan generation.
            // so for this i think it should force selection from assetbrowser...
            // via a "browse" button on 
            // This is for when the new floor plan is created as sole scene.
            // Altneratively, we should be able to select an existing entity and right click it
            // and generate a floorplan for it too.  But 
            // Then when clicking on an exterior entity that has a floorplan, show tab on ribbonbar
            // to show interior view
            // Maybe have in "General" tab a "Add Interior" which will then add the Interior
            // plugin tab that allows us to configure our decks as far as heights and numbers
            // 

            // Floorplan view or Ship interior view always requires a seperate 3d viewport.
            // There is no "new" scene on the server, however, there is a shared representation
            // of this interior with the main scene.
            // 
            // However, that's for runtime interior.  For just a raw ship design, this entiy
            // only exists on the server as a non scene entity and thus can be used for checking
            // construction rules, but not for being an active game object.
            // On client side, the 3d is just a visual aid for designin the ship.  Otherwise
            // designing involves sending of commands just like anything to chanage any property or 
            // add new child nodes, etc.
            // 
            // So client side, we do need multiple scenes!
            // Our runtime arcade interior view will also be restricted to a similar
            // floorplan style viewport.  This is a good way to do it as we can restrict 
            // camera and allow for special viewport toolbar controls for the current deck.
            // 
            //   
            // TODO: ok, here we potentially want to change the grid style
            // change the snap behavior
            // fix the camera to "floorplan view" which is a 3d top down perspective view
            //_core.Scene_Type = SceneType.FloorPlan;

            // AppMain.MAX_FLOORPLAN_SIZE;
            
            float cellWidth = 1.0f;
            float cellHeight = 1.0f;
            float cellDepth = 1.0f;

            FormNewVehicle newVeh = new FormNewVehicle(cellWidth, cellHeight, cellDepth);
            DialogResult dialogResult = newVeh.ShowDialog(this);
            if (dialogResult == DialogResult.OK)
            {
            	throw new NotImplementedException ("TODO: we no longer allow scenes that are untitled. need to update");
                string folderName = "untitled_floorplan";

                _core.Scene_Type = SceneType.SingleRegion;
                _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;

                // TODO: no need for queryunload?  allowed to edit multiple
                KeyCommon.Messages.Floorplan_New message = new KeyCommon.Messages.Floorplan_New(folderName, cellWidth, cellDepth, cellHeight, newVeh.CellCountX, newVeh.CellCountY, newVeh.CellCountZ);

                SendNetMessage(message);
                
                MRU_AddFolder (message.FolderName);
            }
        }
        
        /// <summary>
        /// Conceptually, an outdoor scene is similar to a multi-zone scene
        /// except that each zone has a terrain patch and the gravity simulation is different.
        /// For instance, terrain scenes on flat levels (non spherical world simulation) uses a 
        /// down vector of 0,-1,0.
        /// For terrain levels, assets being placed will snap to ground/floor by default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItemNewOutdoorSceneClick(object sender, EventArgs e)
        {
            _core.Scene_Type = SceneType.MultiRegionTerrainLandscape;
            _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;

            FormNewTerrainScene newTerrainScene = new FormNewTerrainScene();
            newTerrainScene.ShowDialog(this);
         
            if (newTerrainScene.DialogResult == DialogResult.OK )
            {
            	string folderPath;
            	if (NewFileNameValidityTest(newTerrainScene.SceneName, out folderPath))
            	{
	                if (QuerySceneUnload())
	                {
	                    uint acrossX = newTerrainScene.RegionsAcross;
	                    uint acrossY = newTerrainScene.RegionsHigh;
	                    uint acrossZ = newTerrainScene.RegionsDeep;
	
	                    KeyCommon.Messages.Scene_NewTerrain.CreationMode creationMode = 
	                    	 KeyCommon.Messages.Scene_NewTerrain.CreationMode.MultiZone;
	                    
	                    if (newTerrainScene.CreateEmptyTerrain)
	                    	creationMode |= KeyCommon.Messages.Scene_NewTerrain.CreationMode.DefaultTerrain;

	                    // // (for 128 levels, -64 is lowest but is always unbreakable ground that cannot be dug through and so -63 is lowest modifiable. 63 is highest since 0 is first positive value level)
	                    int maximumFloor = (int)(newTerrainScene.TerrainTileCountY / 2 - 1);

	                    int minimumFloor = -maximumFloor;
	                    uint octreeDepth = newTerrainScene.OctreeDepth;
	                    KeyCommon.Messages.Scene_NewTerrain message =
	                        new KeyCommon.Messages.Scene_NewTerrain(
	                            newTerrainScene.SceneName, 
	                            acrossX, acrossY, acrossZ,
	                            newTerrainScene.TileSizeX,
	                            newTerrainScene.TileSizeY,
	                            newTerrainScene.TileSizeZ,
	                            newTerrainScene.RegionResolution,
	                            newTerrainScene.RegionResolution,
	                            newTerrainScene.RegionResolution,
	                            newTerrainScene.TerrainTileCountY,
	                            minimumFloor,
	                            maximumFloor,
	                            creationMode, 
	                            octreeDepth,
	                            newTerrainScene.SerializeEmptyZones);
	
	                    SendNetMessage(message);
	                    
	                    MRU_AddFolder (message.FolderName);
	                }
            	}
            }
        }
        
        /// <summary>
        /// New campaign indicates we are creating and joining a new game and not editing a new Scene.
        /// The diference is that with Scene's you don't create a game state save database.
        /// Furthermore, this represents client exe game specific knowledge.  EXE is open source but our DLLs are closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItemNewCampaignClick (object sender, EventArgs e)
        {
            // no universe EditMode for now. Just a single region scene
            //_core.Scene_Type = SceneType.SimpleComposition;
        	
        	// settings var shared by all pages.  
        	// - is using Ini best way to go here?  Or should we be storing
        	//   this data in a shared SceneInfo object? Or maybe the Ini
        	//  is never stored to disk just shared between the pages and
        	//  then here, we take the data and store it into the SceneInfo
        	// - first thing we need to do is just take the ship path
        	//	 store it, generate universe, assign the ship to a viewpoint
        	//   use that and load it around a rocky planet in orbit
        	//   - we'll use hardcoded values for now, for the universe generation
        	// - todo: why are there two viewpoints being created in the universe?
        	// - todo: why not use the existing multi-region scene and have that
        	//   generate a starting viewpoint and load hardcoded vehicle
        	
        	
        	Settings.Initialization ini = new Settings.Initialization();
        	
        	FormCampaignNew newCampaignScene = new FormCampaignNew (ini);
        	newCampaignScene.WizardCompleted += NewCampaign_WizardCompleted;
        	newCampaignScene.Text = "New Campaign";

            newCampaignScene.WizardPages.Add(0, new FormCampaignCaptainSelect(ini));
        	newCampaignScene.WizardPages.Add (1, new FormCampaignShipSelect(ini));
        	newCampaignScene.WizardPages.Add (2, new FormCampaignGalaxy(ini));
        	newCampaignScene.WizardPages.Add (3, new FormCampaignAliens(ini)); // or more generically, "opponents"
        	       	
        	
        	newCampaignScene.LoadWizard ();
        	newCampaignScene.ShowDialog (this);
        	
        	if (newCampaignScene.DialogResult == DialogResult.OK)
        	{
                throw new NotImplementedException();

          //      // should we first have user generate a new scene, then from wizard select the scene they wish to start a new campaign with?
          //      // they should perhaps be able to select between small, large, huge.
          //      // perhaps what we could do is when the user clicks "next" on the wizard is to generate the Scene then? 

          //      // generate a .save file.  What do we name the save file? Do we take the Scene's "folder name" and then just append that to our savePath? eg. "Campaign1.dat"
          //      // i think in The Sims, you can't save older gameplay, you always resume where you left off with a particular game.
          //      // I could just use a "Save As" and user just enters a unique filename (not path).
          //      string savePath = AppMain.SAVES_PATH;

          //      // throw up a progress bar while we generate the world and database and load the Vehicle.
          //      // the vehicle will have been selected from the campaign wizard 

          //      // todo: this actually needs to load first mission of selected campaign.
          //      //       todo: if no missions exist, we should not load. we need to find the first mission
        		//// load saved scene of Sol system and player's Vehicle
        		//string filepath = _core.ScenesPath + "";
        		//string folderPath = System.IO.Path.GetDirectoryName (filepath);

          //      _core.Scene_Type = SceneType.MultiReginSpaceStarsAndWorlds;
          //      _core.Scene_Mode = Keystone.Core.SceneMode.Simulation;


            // TODO: we should use SimulationJoinRequest and not create a Scene_Load() message
            //SimulationJoinRequest()
          //      KeyCommon.Messages.Scene_Load message = new KeyCommon.Messages.Scene_Load();
          //      message.FolderName = folderPath;
          //      message.UnloadPreviousScene = true;

          //      SendNetMessage(message);        		
        	
          //      MRU_AddFolder (message.FolderName);
        	}
        }
        
        void NewCampaign_WizardCompleted ()
        {
        	System.Diagnostics.Debug.WriteLine ("FormMain.NewCampaign_WizardCompleted()");
        }
        
                
        // used for celestial galaxy.  Perhaps use the Control Panel option dialog instead with a different set of options
        private void buttonItemNewMultiRegionScene_Click(object sender, EventArgs e)
        {
            //_core.Scene_Type = SceneType.Universe;
            FormNewGalaxy newGalaxy = new FormNewGalaxy();
            newGalaxy.ShowDialog(this);
         
            if (newGalaxy.DialogResult == DialogResult.OK )
            {
            	string folderPath;
            	if (NewFileNameValidityTest(newGalaxy.SceneName, out folderPath))
            	{
	                if (QuerySceneUnload())
	                {
                        _core.Scene_Type = SceneType.MultiReginSpaceStarsAndWorlds;
                        _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;

                        uint acrossX = newGalaxy.RegionsAcross;
	                    uint acrossY = newGalaxy.RegionsHigh;
	                    uint acrossZ = newGalaxy.RegionsDeep;
	                    float diameter = newGalaxy.RegionDiameter;
	                    float density = newGalaxy.Density;
                        string relativeVehiclePath = "caesar\\entities\\vehicles\\test3.kgbentity"; 
                        relativeVehiclePath = "caesar\\entities\\vehicles\\yorktown.kgbentity";

                        uint octreeDepth = 0;
	                    KeyCommon.Messages.Scene_NewUniverse message =
	                        new KeyCommon.Messages.Scene_NewUniverse(newGalaxy.RandomSeed , 
	                            newGalaxy.SceneName,
	                            acrossX, acrossY, acrossZ,
	                            diameter, diameter, diameter,
	                            newGalaxy.CreationMode, 
	                            density, octreeDepth,
	                            newGalaxy.SerializeEmptyZones,
	                            newGalaxy.CreateStarDigest,
                                relativeVehiclePath);


                        SendNetMessage(message);
	                    
	                    MRU_AddFolder (newGalaxy.SceneName);
	                }
            	}
            }
        }

        /// <summary>
        /// Open is for offline opening of scenes for editing purpose, but
        /// also will include single player.
        /// For networked games, Network-->Server-->Join is used to join a scene.
        /// Network scenes can be opened offline via Open though, but are in Edit mode
        /// and not Play mode.  
        /// Thus whether a scene is opened in the client for Read/Write or just Read is important.
        /// </summary>
        /// <remarks>
        /// Since this is client side only, we still use loopback method to open the scene?
        /// This makes sense because remote editing, the remote edit server needs to know what
        /// scene we're opening and wish to edit.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            // TODO: its probably easiest to rename this to Edit Scene and then add a Resume Campaign buttton that opens from the \\saves path
            // also we can add a "Create" menu for new prefab and static scene design and then use "New" for campaigns
            // TODO: we can disable menu items on release of v1.0.  only v2.0 will allow for moding and editing new components
            openFile.InitialDirectory = AppMain.SCENES_PATH;
            openFile.Filter = AppMain.OPEN_SCENE_FILTER;
            openFile.FilterIndex = 1;
            openFile.RestoreDirectory = true;

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                string folderPath = System.IO.Path.GetDirectoryName(openFile.FileName);

                // strip the machine specific path so we just have an agnostic relative folder path
                string relativeFolderPath = GetRelativePath(folderPath, AppMain.SCENES_PATH);

                KeyCommon.Messages.Simulation_Join_Request message = new KeyCommon.Messages.Simulation_Join_Request();
                message.FolderName = relativeFolderPath;

                // todo: i need to get the actual real SceneType from the SceneInfo
                _core.Scene_Type = SceneType.SingleRegion;
                _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;

                SendNetMessage(message);

                //OpenScene(relativeFolderPath);
            }
        }

        private void buttonItemEditSceneClick(object sender, EventArgs e)
        {
            _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;
        }

        private void buttonItemEditPrefabClick(object sender, EventArgs e)
        {
            _core.Scene_Mode = Keystone.Core.SceneMode.EditScene;
        }

        private void buttonItemEditMissionClick(object sender, EventArgs e)
        {
            // todo: bring up the campaign browser dialog so a campaign mission can be selected

            // todo: does this allow us to edit the scene or just the missions?
            //       perhaps the scene must already be fixed/locked from changes and we can only edit the mission from this point?
            //       what happens when we try to edit the underlying scene after missions have already been created for it?

            // todo: we should still be able to add mission objects to the interior of a ship or a terrain prefab located on a world
            _core.Scene_Mode = Keystone.Core.SceneMode.EditMission;
        }

        private void buttonItemLoadMissionClick(object sender, EventArgs e)
        {
            string scenesPath = AppMain.SCENES_PATH;

            FormCampaignMissionBrowser missionBrowser = new FormCampaignMissionBrowser();
            missionBrowser.CampaignsPath = scenesPath;

            DialogResult result = missionBrowser.ShowDialog();

            if (result == DialogResult.OK)
            {
                string campaignFolderName = missionBrowser.SelectedCampaign;
                string missionPath = missionBrowser.SelectedMission;

                string username = AppMain._core.Settings.settingRead("network", "username");
                string password = AppMain._core.Settings.settingRead("network", "password");

                // todo: we need to get the SceneType from the SceneInfo
                _core.Scene_Mode = Keystone.Core.SceneMode.Simulation;

                SimulationJoinRequest(campaignFolderName, username, password, "", false);

                // NOTE: Below is unnecessary.
                //       Client never initiates call to OpenScene() or send a Scene_Load().  
                //       Instead, Scene_Load command comes from the server.

                //KeyCommon.Messages.Scene_Load message = new KeyCommon.Messages.Scene_Load();
                //message.FolderName = campaignFolderPath;
                //message.UnloadPreviousScene = true;

                //SendNetMessage(message);

                MRU_AddFolder(campaignFolderName);
                //OpenScene(campaignFolderPath);

                // todo: upon callback that the starting region has completed loading, load the mission objects such as spawn points
                // todo: for spawnpoints that trigger "on start", the server will activate them and send the spawned vehicles to the client?
            }

        }

        private bool NewFileNameValidityTest(string modName, out string fullPath)
        {
        	string folderPath = System.IO.Path.Combine (AppMain.SCENES_PATH, modName);
        	fullPath = folderPath;
        	
        	if (System.IO.Directory.Exists (folderPath))
        	{
        		// overwrite?
        		if (MessageBox.Show ("Folder already exists. Do you wish to delete all existing files?", "Overwrite existing?", MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
        		{
        			return false;
        		}
        		
        		// we need to delete all existing files then recreate folder
                // TODO: if our game.db is in use by say an external db editor, this will throw an exception
        		System.IO.Directory.Delete (folderPath, true);
        	}

    		// create the folder 
			System.IO.Directory.CreateDirectory (folderPath);
        	return true;
        }
        
        
        // TODO: save should not even be necessary since 
        //       we no longer use temporary files and "untitled" and we
        //       dont allow changing names, and we always save incrementally
        //       or as changes are made to the scene.
        private void buttonItemSave_Click(object sender, EventArgs e)
        {

            _core.Scene_Changed = true;  
            QuerySave();




        }


        public void ShowPreview (Keystone.Entities.Entity entity, string archiveFullPath, string archiveEntry)
        {
        	if (this.InvokeRequired)
        	{
        		this.Invoke((System.Windows.Forms.MethodInvoker) delegate
	            { 
    	        	ShowPreview (entity, archiveFullPath, archiveEntry);
    	         });
        	}
        	else 
        	{
				AppMain.PreviewForm.ModPath = archiveFullPath;
            	AppMain.PreviewForm.EntryPath = archiveEntry;
	            AppMain.PreviewForm.TargetEntity = entity;  
               	AppMain.PreviewForm.Show(this);
        	}
        }
        
        private void buttonCloseScene_Click(object sender, EventArgs e)
        {
            // TODO: perhaps toggles to "Disconnect" and then closes the scene / disconnects from server
            // and returns to our empty screen
            KeyEdit.Workspaces.EditorWorkspace ws = (KeyEdit.Workspaces.EditorWorkspace)mWorkspaceManager.GetWorkspace(PRIMARY_WORKSPACE_NAME);
            OnDocumentTabClosing(ws.Document);
        }

        #region Dotnet Document Bar Events
        private bool OnDocumentTabClosing(System.Windows.Forms.Control control)
        {
            bool cancelClosing = true;

            if (control is KeyEdit.Controls.ViewportsDocument)
            {
                DialogResult result = MessageBox.Show(this, "Closing this 3D Viewport will end the simulation.  Are you sure you wish to close?", "End simulation?", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    cancelClosing = !QuerySceneUnload();
                    if (cancelClosing == false)
                        // remove the editor workspace
                        mWorkspaceManager.Remove(PRIMARY_WORKSPACE_NAME);
                }
                else
                    cancelClosing = true;
            }
            else
            {
                mWorkspaceManager.Remove(control.Name);
                cancelClosing = false;
            }
            //else if (control == mCodeEditDocument)
            //{

            //    mCodeEditDocument = null;
            //    cancelClosing = false;
            //}
            //else if (control == mFloorplanEditDocument)
            //{
            //    cancelClosing = false;
            //}

            return cancelClosing;
        }
        #endregion


        private void buttonItemUndo_Click(object sender, EventArgs e)
        {
            UnDo();
        }

        private void buttonItemRedo_Click(object sender, EventArgs e)
        {
            ReDo();
        }

        private void buttonCut_Click(object sender, EventArgs e)
        {

        }

        private void buttonCopy_Click(object sender, EventArgs e)
        {

        }

        private void buttonItemPaste_Click(object sender, EventArgs e)
        {

        }


        //        private void clearUndoHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        //        {

        //        }

        //        private void clearClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        //        {

        //        }

        // TODO: Why not send delete key on to the IOController.cs (eg EditorController.cs) and have it's
        // input processor handle this?
        // http://stackoverflow.com/questions/2386695/disadvantage-of-setting-form-keypreview-true
        //        protected override void OnKeyDown(KeyEventArgs e)
        //        {
        //            base.OnKeyDown(e);
        //            // OBSOLETE - EditorController.cs binds the delete key
        //            // this is because our DX Input is done system wide and does not go through the form exclusively
        //
        //            // see AppMain.ClearInput() for hint on how ApplicationHasFocus is used to stop/start input processing
        //        }


        #region Plugin Notification and QuickLook Panel 
        // user directly clicks to edit the script from the plugin's list of scriptable events
        private void Plugin_OnEditResource(object sender, EventArgs e)
        {
            KeyPlugins.EditResourceEventArgs arg = (KeyPlugins.EditResourceEventArgs)e;
            KeyCommon.IO.ResourceDescriptor descriptor = new KeyCommon.IO.ResourceDescriptor(arg.ResourcePath);

            try
            {
                string ext = descriptor.Extension;
                switch (ext)
                {
                    case ".FX":
                    case ".CSS":
                        // is the edit code workspace active? should we always create a new one
                        // or replace current edited code with this?  I think we should always
                        // create a new one.
                        mWorkspaceManager.Add (new Workspaces.CodeEditorWorkspace(descriptor.ToString()), null); // null ok because scene is irrelevant for CodeEditor?
                        break;

                    // TODO: launch image editor app
                    case ".DDS":
                    case ".PNG":
                    case ".BMP":
                    case ".JPG":
                    case ".GIF":
                    case ".TGA":
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine("Unsupported resource type " + ext.ToUpper());
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error editing resource " + arg.ResourcePath);
            }

        }


        #endregion
        private void buttonStartSimulation_Click(object sender, EventArgs e)
        {
            if (AppMain._core.IsInitialized == false ||
                AppMain._core.SceneManager == null ||
                AppMain._core.SceneManager.Scenes == null ||
                AppMain._core.SceneManager.Scenes.Length == 0) return;

            // start simulation switches us from Edit mode to Simulation mode
            // - must switch from edit view to simulation view
            // - must find a vehicle to attach to
            // - if no vehicle, show message and abort indiccating user should create one
            //
            // - vehicle must be accessible to the current user/player (access rights)
            // - switch plugin profile to simulation
            // - select vehicle initially to load default vehicle plugin
            // 
            // - hide various EDIT tabs while simulating
            // - show various tabs for SIMULATING
            // 
            //if ()

        }

        // TODO: Our plugin i think should be able to call these methods
        // directly from EditorHost.IOController which will then directly
        // call the associated interpreted function that a keybind would call.
        // In fact, we should expand that function processor to allow args directly
        //private void buttonIncreaseVelocity_Click(object sender, EventArgs e)
        //{
        //    ((VehicleController)_core.CurrentIOController).ProcessCommand("velocity increase 1000");
        //}

        //private void buttonDecreaseVelocity_Click(object sender, EventArgs e)
        //{
        //    ((VehicleController)_core.CurrentIOController).ProcessCommand("velocity decrease 1000");
        //}

        //private void buttonStop_Click(object sender, EventArgs e)
        //{
        //    ((VehicleController)_core.CurrentIOController).ProcessCommand("velocity stop");
        //}


       

        #region Quick Test Physics Demos
        private void OnPhysicsDemoSelected(object sender, EventArgs e)
        {
            string name = ((DevComponents.DotNetBar.BaseItem)sender).Name;
            Trace.WriteLine("Physics Demo - " + name);

            // here we can bring up a dialog perhaps to 
            switch (name)
            {
                case "buttonWallDemo":
                    break;

                case "buttonIncomingDemo":
                    break;

                case "buttonSpheresDemo":
                    break;

                case "buttonCatapultDemo":
                    break;

                case "buttonPendulumDemo":
                    break;

                case "buttonPlinkoDemo":
                    break;
                case "buttonVehicleDemo":
                    break;
                case "buttonSandboxDemo":
                    break;
                default:
                    throw new NotImplementedException("Toolbox tool not implemented");
            }

            //if (_core.CurrentIOController is KeyEdit.Workspaces.Controllers.EditorController)
            //{
                // TODO: need to convert physics demos to prefabs and then just load using path
                //ICommand2 command = new ImportPhysicsDemo(PhysicsDemo.LotsOfSpheres, .....);
                //command.BeginExecute(CommandCompleted);
            //}
        }
        #endregion

        //#region drag and drop - MOVED TO EditorWorkspace.cs
        //private KeyPlugins.DragDropContext mDragContext;
        //private bool mDraggingItem;
        //private const int DRAG_START_DELAY = 150; // milliseconds
        //private Stopwatch mDragDelayStopWatch;
        //private void QueryDrag(object sender, EventArgs e)
        //{
        //    // drag and drop sites
        //    // http://blogs.techrepublic.com.com/howdoi/?p=148
        //    // http://stackoverflow.com/questions/495666/c-drag-drop-from-listbox-to-treeview
        //    // http://www.codeproject.com/KB/cs/DragDropImage.aspx

        //    if (!mDraggingItem)
        //    {
        //        if (mDragDelayStopWatch == null)
        //        {
        //            mDragDelayStopWatch = new Stopwatch();
        //            mDragDelayStopWatch.Start();
        //            return;
        //        }
        //        // if the time since the mouse down occurred and the current mouse move >= START_DRAG_INTERVAL then
        //        //  we initiate drag operation

        //        if (mDragDelayStopWatch.ElapsedMilliseconds >= DRAG_START_DELAY)
        //        {
        //            mDraggingItem = true;
        //            mDragContext = new KeyPlugins.DragDropContext();
        //            mDragDelayStopWatch = null;
        //            if (sender is TreeView && ((TreeView)sender).SelectedNode != null)
        //            {
        //                mDragContext.EntityID = ((TreeView)sender).SelectedNode.Name;
        //                ((TreeView)sender).DoDragDrop(mDragContext, DragDropEffects.Move);
        //            }
        //            else if (sender is Control)
        //                ((Control)sender).DoDragDrop(mDragContext, DragDropEffects.Copy);
        //            else if (sender is ButtonItem)
        //            {
        //                mDragContext.RelativeZipFilePath = AppMain.ModName;
        //                mDragContext.ResourcePath = ((ButtonItem)sender).Name;
        //                DoDragDrop(mDragContext, DragDropEffects.Copy);
        //            }
        //            else
        //                DoDragDrop(mDragContext, DragDropEffects.Copy);
        //        } 
        //    }
        //}
        
        //private void EndDrag()
        //{
        //    mDraggingItem = false;
        //    mDragDelayStopWatch = null;
        //    mDragContext = null;
        //}

        //protected override void OnMouseUp(MouseEventArgs e)
        //{
        //    EndDrag();
        //    base.OnMouseUp(e);
        //}
        //#endregion



        private void menuItemAbout_Click(object sender, EventArgs e)
        {
            FormAbout formAbout = new FormAbout();
            formAbout.ShowDialog(this);
        }

        private void buttonCredits_Click(object sender, EventArgs e)
        {

        }

        private void buttonCommunityForums_Click(object sender, EventArgs e)
        {

        }

        private void buttonHowToPlay_Click(object sender, EventArgs e)
        {

        }


        private void buttonItemNew_Click(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// Load "most recently used" list of simulations 
        /// </summary>
        void MRU_Initialize()
        {
            // MRU list is maintained in config file
            if (_core.Settings == null) return;

            string dirPath = _core.ScenesPath;
            string[] directories = System.IO.Directory.GetDirectories(dirPath);
            if (directories == null) return;


            System.Diagnostics.Debug.WriteLine("FormMain.MRU_Initialize() - Initializing MRU.");
            // clear MRU list
            System.Collections.Generic.List<DevComponents.DotNetBar.ButtonItem> items = new System.Collections.Generic.List<DevComponents.DotNetBar.ButtonItem>();
            foreach (DevComponents.DotNetBar.BaseItem item in menuFileMRU.SubItems)
            {
                if (item as DevComponents.DotNetBar.ButtonItem == null)
                    continue;

                item.Click -= MRU_Button_Clicked;
                items.Add(item as DevComponents.DotNetBar.ButtonItem);
            }

            menuFileMRU.SubItems.RemoveRange(items.ToArray());

            // get list of 10 MRU simulations from Settings
            for (int i = 1; i <= MAX_MRU_LENGTH; i++)
            {
                string file = _core.Settings.settingRead("mru", "mru" + i);
                // exit after first empty string found in MRU list
                if (string.IsNullOrEmpty(file))
                    break;


                DevComponents.DotNetBar.BaseItem button = new DevComponents.DotNetBar.ButtonItem();
                button.Click += MRU_Button_Clicked;
                button.Text = "&" + i + ". " + file;
                button.Tag = file;
                mMRUList.Add((string)button.Tag);
                this.menuFileMRU.SubItems.Add(button);
            }

            //        	for (int i = 0; i < directories.Length; i++)
            //        	{
            //        		DevComponents.DotNetBar.BaseItem button = new DevComponents.DotNetBar.ButtonItem();
            //        		button.Click += MRU_Button_Clicked;
            //        		button.Text = "&" + (i + 1) + ". " + directories[i];
            //        		button.Tag = directories[i];
            //        		this.menuFileMRU.SubItems.Add (button);
            //        	
            //        	}
        }


        void MRU_Button_Clicked(object sender, EventArgs args)
        {
            //System.Diagnostics.Debug.WriteLine("FormMain.MRU_Button_Clicked() - Loading MRU selection.");
            //DevComponents.DotNetBar.ButtonItem button = sender as DevComponents.DotNetBar.ButtonItem;
            //if (button == null) return;

            //string folderName = button.Tag as string;
            //if (string.IsNullOrEmpty(folderName)) return;
            //if (!System.IO.Directory.Exists(Path.Combine(AppMain.SCENES_PATH, folderName))) return;

            // TODO: this needs to use simjoinrequest
            //string relativeFolderPath = folderName;
            //OpenScene(relativeFolderPath);
        }

        /// <summary>
        /// Add folder to MRU list rearranging as necessary
        /// </summary>
        /// <param name="relativeFolderPath"></param>
        void MRU_AddFolder(string relativeFolderPath)
        {
            MRU_RemoveFolderInternal(relativeFolderPath);

            mMRUList.Insert(0, relativeFolderPath);

            // If we have too many items, remove the last one.
            if (mMRUList.Count > MAX_MRU_LENGTH) mMRUList.RemoveAt(MAX_MRU_LENGTH);

            // update the saved settings configuration file
            MRU_Save();

            // rebuild the GUI 
            MRU_Initialize();
        }

        void MRU_RemoveFolder(string folderPath)
        {
            MRU_RemoveFolderInternal(folderPath);

            // update the saved settings configuration file
            MRU_Save();

            // rebuild the GUI
            MRU_Initialize();
        }

        void MRU_Save()
        {
            for (int i = 1; i <= MAX_MRU_LENGTH; i++)
            {
                string relativeFolder = null;
                if (i > mMRUList.Count)
                    relativeFolder = "";
                else
                    relativeFolder = mMRUList[i - 1];

                string folderName = null;
                if (string.IsNullOrEmpty(relativeFolder))
                    folderName = "";
                else
                    folderName = Path.GetFileName(relativeFolder);

                _core.Settings.settingWrite("mru", "mru" + i, folderName);
            }
            _core.Settings.Save();
        }

        void MRU_RemoveFolderInternal(string relativeFolderPath)
        {
            // Remove all occurrences from the list.
            for (int i = mMRUList.Count - 1; i >= 0; i--)
            {
                if (mMRUList[i] == relativeFolderPath)
                    mMRUList.RemoveAt(i);
            }
        }

    }
}
