using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;
using Keystone.CSG;

namespace KeyEdit.Scripting
{
	/// <summary>
	/// Description of GameAPI.
	/// </summary>
	public class GameAPI : IGameAPI
	{

        #region Component Storage and Processing
        // Intrinsic Components
        //int RegisterIntrinsicComponentsStore<T>(string name, Memory<T> data);
        // registering of intrinsic component instances could be done for the user?
        //int IGameAPI.RegisterComponentInstance<T> (string entityID, T instance)
        //{
        //
        //}
        
        // OBSOLETE - When a user script attempts to register a user component instance,
        //            any ComponentStore<T> will be created by the Repository.StoresCollection
        //
        // User Defined Components (eg. see Game01.Components.UserComponents.cs)
        //int IGameAPI.RegisterUserComponentsStore<T>(string name, Memory<T> data)
        //{
        //    
        //}
        
        int IGameAPI.RegisterUserComponentInstance<T> (string entityID, T instance)
        {
            Keystone.Entities.Entity e = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
          
            const int DEFAULT_STORE_SIZE = 1024;
            
             // does this require the UserData already exists? yes, if that is where we store the mem
             KeyCommon.Data.ComponentStore<T> store = Repository.StoresCollection.CheckOut<T>(DEFAULT_STORE_SIZE);
             
             Memory<T> mem = store.CheckOut();
             
             System.Diagnostics.Debug.Assert (e.UserData != null);
             
             e.UserData.Add(typeof(T).ToString(), mem);
             
             
             
        }
        
        
        // we require all Processors to reside in "user_functions_processors.css" 
        // NOTE: Intrinsic Processors could all be created outside of script or within a specific
        //       script loaded by Keystone.dll at runtime if allowed.  Either way
        //       we don't need a "RegisterIntrinsicProcessor<T>()" here
        
        // "intrinsic_functions_processors.css"
        // static void Steering()
        // static void BoidFlockingBehavior()
        // static void Wander()
        // static void Newtonian()
        // static void InterpolatedAnimations()
        
        int RegisterProcessor<T> (string name, KeyCommon.Processors.DataProcessor.Processor<Memory<T>> proc) // this is just to create it, not Run it. todo 
        {
        
            Repository.RulesProcessors.Add(name, proc)
            
            
            
            DataProcessors.Processor p = TestIntrinsicProcessor;
        
        //mIntrinsicProcessors = new KeyCommon.Processors.DataProcessors();
        //mIntrinsicProcessors.Add("STEER", p);
        // then -> p[i].Invoke(store, parameters, r);
            
            
            
        }
        
        #endregion
        
        
		#region Paths
        public string Path_GetDataPath()
        {
            return AppMain.DATA_PATH;
        }
        public string Path_GetModsPath()
        {
            return AppMain.MOD_PATH;
        }
        public string Path_GetModName()
        {
            return AppMain.ModName;
        }
        #endregion

        #region Game Actions
        /// <summary>
        ///  Perform vehicular ranged attack.
        /// </summary>
        /// <param name="stationID"></param>
        /// <param name="weaponID"></param>
        /// <param name="targetID"></param>
        void IGameAPI.PerformRangedAttack(string stationID, string weaponID, string targetID)
        {
            Game01.Messages.PerformRangedAttack rangedAttack = new Game01.Messages.PerformRangedAttack();
            rangedAttack.StationID = stationID;
            rangedAttack.WeaponID = weaponID; // todo: what if we have multiple weapons linked or in the same turret?
            rangedAttack.TargetID = targetID;

            // todo: what if we want to have multiple weapons in one command? or should we just force multiple commands to be sent?


            AppMain.mNetClient.SendMessage(rangedAttack);

            // todo: server processing needs to check that its a valid sensor contact


            // todo: when we receive messages, we want to rely heavily on simple ChangeProperties() type of messages.


            // todo: the station operator should gain XP based on performance of the attack

            // we wait for server response which will include all results including any "maalf" or damage to one or more entityIDs.  We will then activate any renderables and audio, and apply the changed properties included in the server message to client side entities.
        }
        #endregion

        #region Workspaces
        public string Workspace_GetActiveName()
        {
            FormMain form = (FormMain)AppMain.Form;
            return form.WorkspaceManager.CurrentWorkspace.Name;
        }

        public void Workspace_SetTool (string workspaceName, string toolName, string toolTargetEntityID, object toolValue)
 		 {
 		 	Keystone.Workspaces.IWorkspace ws = AppMain.Form.WorkspaceManager.GetWorkspace (workspaceName);
 		 	
 		 	// if this is not a 3d workspace, this is an inappropriate ws since non 3D workspaces (eg. CodeEditor, BehaviorTreeEditor) don't have Tools
 		 	if (ws is Keystone.Workspaces.IWorkspace3D == false) return;
 		 	
 		 	Keystone.Workspaces.IWorkspace3D workspace = (Keystone.Workspaces.IWorkspace3D)ws;
 		 	
 		 	if (workspace == null) return;
 		 	
 		 	Keystone.EditTools.Tool tool = null;
 		 	
 		 	switch (toolName)
 		 	{
 		 		case "waypoint_placer":
 		 			Keystone.Entities.Entity vehicle = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (toolTargetEntityID);
 		 			tool = new KeyEdit.Workspaces.Tools.NavPointPlacer(AppMain.mNetClient);
 		 			break;
	 			case "unit_action":
 		 			{
 		 				// TODO: UnitActionTool should also be a "Selection" tool when we left mouse click over a different target.
 		 				Keystone.Entities.Entity unit = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (toolTargetEntityID);
 		 				tool = new KeyEdit.Workspaces.Tools.UnitActionTool(AppMain.mNetClient, unit);
 		 				
 		 			
	 		 			switch ((string)toolValue)
	 		 			{
	 		 				case "move_to_location":
	 		 				default:
	 		 					// we need a new mouse cursor to show we are in move to mode
	 		 					
	 		 					
	 		 					break;
	 		 			}
 		 			break;
 		 			}
 		 		case "selection_tool":
 		 		default:
 		 			tool = new KeyEdit.Workspaces.Tools.SelectionTool (AppMain.mNetClient);
 		 			break;
 		 	}
 		 	
 		 	workspace.CurrentTool = tool;
 		 }

        #endregion

        #region GUI


        #endregion

        #region HUD 

        #endregion
    }
}