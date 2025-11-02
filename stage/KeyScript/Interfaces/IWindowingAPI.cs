using System;
using Keystone.Types;
using KeyScript.Rules;
using KeyScript.Routes;

namespace KeyScript.Interfaces
{
    public interface IWindowingAPI
    {
        string CreateWindow(string parentID);
        string CreateButton(string containerID);
        string CreateLabel (string containerID);
        string CreateTextbox(string containerID);
        
        string GetControlContainer(string controlID);
        
        string GetTopMost(string sceneID);
        string[] GetWindows();
        string[] GetControls(string windowID);
        // since we support creating and wiring our GUI entirely from script, we need to support all functionality nrcessary to do this.
        
        void AddEventHandler(string windowID);
        void RemoveEventHandler(string windowID);
        DialogResult ShowDialog(string owner = null);
        
      
        
        
        
        
        
        
        
        
        
        
        #region Workspaces
        void Workspace_SetTool (string workspaceName, string toolName, string toolTargetEntityID, object toolValue);
        string Workspace_GetActiveName();
        #endregion

        #region HUD 
        #endregion

    }
}