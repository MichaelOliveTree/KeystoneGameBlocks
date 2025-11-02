using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Keystone.Cameras;
using Keystone.Commands;
using Keystone.Controllers;
using Keystone.Devices;
using Keystone.Entities;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Scene;
using Keystone.Types;
using Lidgren.Network;
using MTV3D65;
using System.Collections.Generic;
using DevComponents.DotNetBar;
using Ionic.Zip;
using KeyEdit.Controls;

namespace KeyEdit
{
    public class FormMainBase : DevComponents.DotNetBar.Office2007RibbonForm
    {

        // ribbonControl MUST be new'd before any derived Form attempts to Initialize()
        public RibbonControl ribbonControl; 


        internal Workspaces.WorkspaceManager mWorkspaceManager;
        protected Keystone.CoreClient _core;
        protected bool m_bSaveLayout = true;

        private System.ComponentModel.IContainer components;
        protected const string PRIMARY_WORKSPACE_NAME = "Main Viewer";
        protected const string TACTICAL_WORKSPACE_NAME = "Tactical";
        
        public delegate void SendNetworkMessage(KeyCommon.Messages.MessageBase message);

        public virtual void SendNetMessage(KeyCommon.Messages.MessageBase message)
        {
            message.SetFlag(KeyCommon.Messages.Flags.SourceIsClient);


            AppMain.mNetClient.SendMessage(message);
        }

        protected FormMainBase() 
        { 
            InitializeComponent(); 
        }

        protected FormMainBase(Keystone.CoreClient core) : base()
        {
            if (core == null) throw new ArgumentNullException();

            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            _core = core;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            // 
            // FormMainBase
            // 
            this.ClientSize = new System.Drawing.Size(604, 529);
            this.Name = "FormMainBase";
            this.ResumeLayout(false);

        }
        
        public Workspaces.WorkspaceManager WorkspaceManager {get {return mWorkspaceManager;}}

        protected override void  OnLoad(EventArgs e)
        {
 	         base.OnLoad(e);

             // rendering will start as soon as a scene is loaded
             // Hook the application's idle event and launch the main form.

             //used in both single threaded gameloop and dedicated

            //Application.Idle += AppMain.OnApplicationIdle;
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            try
            {
                KeyCommon.Messages.Simulation_Leave leave = new KeyCommon.Messages.Simulation_Leave();
                string username = AppMain._core.Settings.settingRead("network", "username");
                leave.UserName = username; // todo: this shouldn't be necessary since the connection.Tag should have a UserRecord assigned to it
                SendNetMessage (leave);

                //if (QuerySceneUnload())
                //{
                //    // TODO: there needs to be an option to just exit the current simulation and keep the main client open so that we can 
                //    //       join a different campaign instead of having to shutdown completely

                //    // send message to server that we want to quit. Server will save user's state and then remove them from the simulation
                //    // this must destroy all HUD widgets and nodes that were created by the workspace
                //    // TODO: server should perhaps respond to a "disconnect" message or "timeout" or something and initiate save state too? 
                //    // server only needs to save the actual Scene and not HUD elements or menu states.
                //    if (m_bSaveLayout)
                //    {
                //        // todo: dispose all workspaces with the primary workspace closed last
                //        mWorkspaceManager.SaveLayout();
                //    }
                //    Hide();
                //    AppMain.PostQuitMessage(0);
                    
                //    AppMain.PluginService.ClosePlugins();

                //    //TODO: unload scripts
                //    // todo: we shouldn't need to shutdown the core if we are only exiting this particular scene or simulation but not closing the entire client.
                //    // todo: we should probably reset certain variables like current_scene_name and current_simulation_name and such
                //    _core.Shutdown(); // shut down and unload scene before the form which our engine is initialized too is removed
                //}
                //else
                    e.Cancel = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }


        // OBSOLETE - when the form is resized, each document tab will get resized and they have
        //            event handler assigned to WorkspaceBase.cs.OnDocumentResized()
        //protected override void OnResizeEnd(EventArgs e)
        //{
        //    base.OnResizeEnd(e);
        //    if (mWorkspaceManager != null) 
        //        mWorkspaceManager.Resize();
        //}
        
        // returns true if user elects to EITHER save or willfully NOT save
        // returns false if user CANCELS
        protected bool QuerySceneUnload()
        {
            // TODO: If we're in a simulation and not just a Scene or Prefab edit mode, the server needs to save state
            if (AppMain._core.SceneManager != null && !string.IsNullOrEmpty (_core.Scene_FileName))
            {
                // does the current scene need to be saved?
                // TODO: .Changed should now be irreelvant since all changes should be done in real-time as they are made
                //       in the editor.
                if (_core.Scene_Changed)
                {
                    DialogResult result = MessageBox.Show(
                        "The current scene has not been saved since it was last modified.  Do you wish to save now?",
                        "Save current scene?", MessageBoxButtons.YesNoCancel);

                    if (result == DialogResult.Cancel) return false;
                    if (result == DialogResult.No)
                    {
                        UnloadScene(false); // unload the existing scene without saving
                    }
                    else
                    {
                        UnloadScene(true);
                        
                    }
                }
                else 
                {
                    // we can unload it safely without saving
                    UnloadScene(false);
                }
            }
            _core.Scene_IsUntitled = false;
            _core.Scene_Changed = false;
            _core.Scene_FileName = "";
            return true;
        }


        protected bool QuerySave()
        {
            // does the current scene need to be saved?
            if (_core.Scene_Changed)
            {
                DialogResult result = MessageBox.Show(
                    "The current scene has not been saved since it was last modified.  Do you wish to save now?",
                    "Save current scene?", MessageBoxButtons.YesNoCancel);

                if (result == DialogResult.Cancel) 
                	return false;
                
                if (result != DialogResult.No)
                {
                	Save();    
                }
            }
            return true;
        }


        protected  void Save()
        {

            // the file menu Save buttons apply to the scene that is referenced by the current view.
            // (eg EditorView, FloorplanEditorView)
            //TODO: maybe save icon should only exist on this workspace's toolbar...
            // or perhaps save always checks all unsaved documents workspaces such as code editor
            // sort of like notepad++ that has save all ands ave for active document
            if (mWorkspaceManager.CurrentWorkspace != null)
                if (mWorkspaceManager.CurrentWorkspace is Workspaces.WorkspaceBase)
                {
                    Workspaces.WorkspaceBase current = (Workspaces.WorkspaceBase)mWorkspaceManager.CurrentWorkspace;

                    if (current.Scene != null)
                    {
                        current.Scene.Save();
                        _core.Scene_Changed = false;
                    }
                }
        }


        protected void UnloadScene(bool save)
        {
            if (save)
            {
                Save();
            }

            _core.SceneManager.Unload(_core.Scene_FileName);
            //ClearDockedWindows();
        }

        protected Queue<Amib.Threading.IWorkItemResult> mCompletedCommands = new Queue<Amib.Threading.IWorkItemResult>();
        protected object mCompletedQueueLock = new object();
        // This is a handler function called by every ICommand and so is executed on the worker thread.
        // Thus we queue each result so that we can process them into the main thread without having each ICommand worker
        // thread interupting at any time (such as while rendering the scene itself).  In other words, we can more deterministiclaly
        // control when we apply completed commands to the scene and it's entities.
        internal void CommandCompleted(Amib.Threading.IWorkItemResult result)
        {
            // lock (sychronize) the queue and add the new item and return.  We'll process the results after we've rendered
            // the scene and before the next update()
            lock (mCompletedQueueLock)
            {
                try
                {
                    Keystone.Commands.Command cmd = (Keystone.Commands.Command)result.State;
                    // TODO: I should actually use a List and then sort the results by the sequenceID (TODO: actually here im wrong about what sequenceID is, its for
                    // reassembling fragmented packets, it is NOT serial id of a packet.  use UUID for that) 
                    // so that on a machine with 4 cores
                    // if we have 2 for concurrency for our threadpool in such situations, then some commands will complete out of order
                    // (if it's allowed to and doesnt rely on other things) which is fine so long as we then process the completed commands
                    // in order
                    if (cmd.Message is KeyCommon.Messages.Scene_New)
                    {

                    }
                    else if (cmd.Message is KeyCommon.Messages.Scene_NewTerrain)
                    {
                    }
                    else if (cmd.Message is KeyCommon.Messages.Scene_NewUniverse)
                    {

                    }
                    else if (cmd.Message is KeyCommon.Messages.Floorplan_New)
                    {
 
                    }
                    
                    // NOTE: We enqueue the full IWorkItemResult because result contains
                    // any information about exceptions thrown during the work item's execution
                    // and thus success/fail information.
                    mCompletedCommands.Enqueue(result);
                }
                catch (Exception ex)
                { }
                finally 
                {
                    AppMain._core.CommandProcessor.RemoveWorkItemResult(result);
                }
            }
        }

        internal virtual void ProcessCompletedCommandQueue() { }
    }
}
