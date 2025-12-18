using System;
using Keystone.CSG;
using Keystone.Types;
using KeyScript.Rules;
using KeyScript.Routes;

namespace KeyScript.Interfaces
{
    public interface IGameAPI
    {
        
        #region Component Storage and Processing
        // Intrinsic Components
        //int RegisterIntrinsicComponentsStore<T>(string name, Memory<T> data);
        // registering of intrinsic component instances could be done for the user?
        int RegisterComponentInstance<T> (string entityID, T instance);     
        
        // User Defined Components (eg. see Game01.Components.UserComponents.cs)
        int RegisterUserComponentsStore<T>(string name, Memory<T> data);
        int RegisterUserComponentInstance<T> (string entityID, T instance);
        
        
        
        
        // we require all Processors to reside in "user_functions_processors.css" 
        int RegisterProcessor<T> (string name, KeyCommon.Processors.DataProcessor<T>); // this is just to create it, not Run it
        
        
        #endregion
        
        #region Paths
        string Path_GetDataPath();
        string Path_GetModsPath();
        string Path_GetModName();
        #endregion

        #region Actions
        void PerformRangedAttack(string stationID, string weaponID, string targetID);
        #endregion

        #region Workspaces
        void Workspace_SetTool (string workspaceName, string toolName, string toolTargetEntityID, object toolValue);
        string Workspace_GetActiveName();
        #endregion

        #region HUD 
        #endregion

    }
}