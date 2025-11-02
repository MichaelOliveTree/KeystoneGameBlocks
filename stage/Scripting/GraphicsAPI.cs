using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;

namespace KeyEdit.Scripting
{
    public class GraphicsAPI : IGraphicsAPI
    {

        #region IGraphicsAPI Members
        bool IGraphicsAPI.IsVisible (string contextID, string entityID)
        {
        	if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();
        	
        	Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
        	return true;
        }
        
        bool IGraphicsAPI.IsOccluded (string contextID, string entityID, string modelID, Vector3d cameraSpacePosition)
        {
        	// TODO: for our sun, what we could do is, create a frustum that is just big enough
        	//       to look at the sun, then cull the scene using cone shaped frustum
        	//       then a) if any other Entity is in view, then it must be occluded because nothing is further than the sun
        	//            b) do hardware occ query with the results of the cone shaped frustum cull and determine
        	//               what percentage of occlusion there if possible so we can control how much the lens flares 
        	//               fade out and/or cut on/off.
        	
//        	//Setup our Hardware Occlusion Quirres camera
//        	TVCamera camera = AppMain._core.CameraFactory.CreateCamera ();
//        	
//        	camera.OccQuery_Init(MaxNumberoQueries);
//
//            //Create our Hardware Occlusion Quirres Render Surface
//            //Create a render surface thats 50% of our screen size.
//            MTV3D65.TVRenderSurface occlusionRS = CEngine.Scene.CreateRenderSurfaceEx(1, 1, CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_A8R8G8B8, true, true, .5f, "Occlusion");
//            occlusionRS.SetNewCamera(Camera.Cam);
            
            // Then later, we can use our occluder in an FXOccluder of some  kind
            //  RenderBeforeClear()
            //  {
            //    //Set color writes to false(Speeds things up)
            //    AppMain._core.Scene.SetColorWriteEnable (false, false, false, false);
            
			//    //Render Hardware Occlusion Queries Surface
            //    occlusionRS.StartRender(true);
            
            //    // Render All Meshes first to the occlusionRS (but using designated occluders would be better)
            //    renderCB.Invoke(occludersOnly)
            //
            //    //Check Z buffer for Meshes
            //    for (int i = 0; i < TotalMeshesRenderedCount; i++)
            //    {
            //        //Get each mesh's bounding box
            //        MeshesInFrustum[i].GetBoundingBox(ref MeshBoundsMin, ref MeshBoundsMax);
            
            //        //begin performing occlusion query and note that for each "i" 
            //        //of the Begin/End iteration, a OccQuery_GetData() will become available
            //        camera.OccQuery_Begin(i);

            //        //Draw mesh's bounding box solid to see if any part of it's box
            //        // is occluded by a mesh with a different i index (so we don't self occlude).
            //        camera.OccQuery_DrawBox(MeshBoundsMin, MeshBoundsMax);

            //        camera.OccQuery_End();
            //    }
            //    occlusionRS.EndRender();
                        
            //    //Set color writes back to enabled
            //    AppMain._core.Scene.SetColorWriteEnable (true, true, true, true);

            //    //Get Number of Queries
            //    for (int i = 0; i < TotalMeshesRenderedCount; i++)
            //    {
            //        MeshQueryData[i] = Camera.Cam.OccQuery_GetData(i, true);
            //    }
            //}
            
            // then in Render()
            // {
            //    //Render Meshes that are not occluded
            //    for (int i = 0; i < TotalMeshesRenderedCount; i++)
            //   {
            //        //If mesh Is visable then render!
            //        if (MeshQueryData[i] != 0)
            //        {
            //            MeshesInFrustum[i].Render();
            //            MeshesRenderedCount++;
            //        }
            //    }
            // }
        	return false;
        }
        
        Vector3d IGraphicsAPI.Project(string contextID, Vector3d v)
        {
        	
            if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            Vector3d result = vp.Project(v, vp.Context.Camera.View, vp.Context.Camera.Projection, Matrix.Identity());
            return result;
        }

        void IGraphicsAPI.SetBackColor (string contextID, Vector3d color) 
        {
        	if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();

        	Keystone.Types.Color c;
        	c.r = (float)color.x;
        	c.g = (float)color.y;
        	c.b = (float)color.z;
        	c.a = 1.0f;
        	
        	((IGraphicsAPI)this).SetBackColor (contextID, c);
        	
        }
        
        void IGraphicsAPI.SetBackColor (string contextID, Keystone.Types.Color color)
        {
        	if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            vp.BackColor = color;
        }
                
        Vector3d IGraphicsAPI.GetCameraPosition(string contextID)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false) 
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            return vp.Context.Position;
        }

        Vector3d IGraphicsAPI.GetCameraLook(string contextID)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            return vp.Context.LookAt;
        }

        int IGraphicsAPI.GetViewportWidth(string contextID)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            return vp.Width;
        }

        int IGraphicsAPI.GetViewportHeight(string contextID)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false)
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            return vp.Height;
        }
        #endregion
    }

}
