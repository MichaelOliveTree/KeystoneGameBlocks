using System;
using Keystone.CSG;
using Keystone.Types;
using KeyScript.Rules;
using KeyScript.Routes;

namespace KeyScript.Interfaces
{
    public interface IGameAPI
    {
        
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