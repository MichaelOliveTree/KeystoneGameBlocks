using System;
using Keystone.CSG;
using Keystone.Types;
using KeyScript.Rules;

namespace KeyScript.Interfaces
{
    public interface IGraphicsAPI
    {
		bool IsVisible (string contextID, string entityID);
		bool IsOccluded (string contextID, string entityID, string modelID, Vector3d cameraSpacePosition);
        Vector3d Project(string contextID, Vector3d v);
        Vector3d GetCameraPosition(string contextID);
        Vector3d GetCameraLook(string contextID);
        int GetViewportWidth(string contextID);
        int GetViewportHeight(string contextID);

        void SetBackColor (string contextID, Vector3d color);
        void SetBackColor (string contextID, Keystone.Types.Color color);
    }
}
