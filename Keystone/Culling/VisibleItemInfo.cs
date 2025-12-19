using System;
using System.Collections.Generic;
using Keystone.Types;
using Keystone.Lights;
using Keystone.Entities;
using Keystone.Elements;

namespace Keystone.Culling
{
    
    public struct VisibleItem
    {
        /// <summary>
        /// If the node being inserted is closer to the camera than 
        /// the current existing node being evaluated, return 1 (true)
        /// </summary>
        private class LightComparer : System.Collections.Generic.IComparer<SortableLightInfo>
        {
            #region IComparer<LightInfo> Members
            public int Compare(SortableLightInfo light1, SortableLightInfo light2)
            {
                return light1.DistanceToItemSquared < light2.DistanceToItemSquared ? 1 : 0;
            }
            #endregion
        }

        // enum Draw_Geometry, Draw_Collider, Draw_CSG_Stencil
        public Entity Entity;
        public Model Model;
        
        public ushort RenderPriority;
        public Vector3d CameraSpacePosition;
        public double DistanceToCameraSq;

        // NOTE: SingleLinkedLIst is used to speed up sorted insertion of relevant lights
        // TODO: if i add the influential lights and sort them by their tvindex, i can easily create
        // a hash value to uniquely identify that enabled set of lights.  since it's always sorted by
        // id... also during adding of the light, if we have some other limit on how many lights
        // we can elect to drop lights that are too small and then recompute the hash.
        public keymath.DataStructures.SingleLinkedList<SortableLightInfo> InfluentialLights;
        private int _lightsHashCode;
        private LightComparer mLightComparer; 
        public bool Indoor;


        public VisibleItem(Entity entity, Model model, Vector3d cameraSpacePosition)
        {
            Entity = entity;
            Model = model;
            CameraSpacePosition = cameraSpacePosition;
            DistanceToCameraSq = Vector3d.GetLengthSquared(cameraSpacePosition);

            // if forward render, only 8 lights allowed
            // thus we compare and only add the light if it beats out at least the 8th light
            // in terms of it's range/intensity/distance to node or camera (these acceptance criteria can be modified)
            InfluentialLights = new keymath.DataStructures.SingleLinkedList<SortableLightInfo>();
            mLightComparer = new LightComparer();
            _lightsHashCode = -1;
            RenderPriority = entity.RenderPriority;
            Indoor = false;
        }

        public int AppearanceHashCode { get { return Model.Appearance.GetHashCode(); } }

        // http://stackoverflow.com/questions/539311/generate-a-hash-sum-for-several-integers
        public int LightsHashCode { get { return _lightsHashCode;  } }

        public void AddLight(SortableLightInfo info)
        {

            //TODO: Dec.5.2012  
            //      We are finding the right lights, however I think there is some kind of error with
            //      TV setting the proper light into the semantic during multithreading.
            //      UPDATE: Ive re-enabled the single threaded culling and it exhibits same behavior.
            //              But i still think its a matter of tv not passing proper light semantics
            //              to shader
            //      UPDATE: maybe i can just manually pass the proper light index?  that would clear all
            //             this up.
            // TODO: is there a way to cap the linked list count to 8 or 10 lights? 
            InfluentialLights.Add(info, mLightComparer);
          //  System.Diagnostics.Trace.WriteLine("Entity " + Entity.ID +  "- Influential light " + info.LightInfo.Light.TVIndex.ToString ());
           // _lightsHashCode = 
        }

        /// <summary>
        /// This is the PRIMARY DRAW for every 3D Mesh and is called from RegionPVS.Draw(). Perhaps we can move this Draw() to there?
        /// OR we move all the RegionPVS.Draw() functions to here?
        /// In the following code, we first do a check if we need to set any shader parameters for InstancedGeometry type Meshes
        /// Otherwise we just call model.Draw()
        /// </summary>
        public void Draw(RenderingContext context, double elapsedSeconds, FX.FX_SEMANTICS fxsemantics)
        {
        	if (Model.Geometry is InstancedGeometry && InfluentialLights != null && InfluentialLights.Count > 0)
        	{
        		// TEMP HACK: should probably move following lines into Model.Render() and pass InfluentialLights[] to it.
        		//            If there are no influential lights, we still need to handle fullbright/emissive/etc.
        		//
        		// need to supply light direction and light color
        		Model.Appearance.Shader.SetShaderParameterVector("g_DirLightDirection", -InfluentialLights[0].LightInfo.Light.Direction);
                Model.Appearance.Shader.SetShaderParameterVector ("g_DirLightColor", Helpers.TVTypeConverter.ColorToVector3d (InfluentialLights[0].LightInfo.Light.Diffuse));
        	
                float r = 0.0f;
                float g = 0.0f;
                float b = 0.0f;
                
                // IMPORTANT: fog SHOULD NOT BE ENABLED for space scene rendering
                // TODO: should fog be apart of appearance node? that way we can enable per model
                //       perhaps ideally, it's an appearance we can inherit from a Region appearance so space scenes fogenable = false and terrain scenes = true
                //       perhaps fog should be part of an environmental volume node.  Thus when generating a test world
				//      we can add one to each zone, but for the sun day & night cycle, not.
                CoreClient._CoreClient.Atmosphere.Fog_GetColor(ref r, ref g, ref b);
                
                Vector3d fogColor = new Vector3d (r, g, b);
                
                float start = 0.0f;
                float end = 0.0f;
                float density = 0.0f;
                CoreClient._CoreClient.Atmosphere.Fog_GetParameters (ref start, ref end, ref density);
                
                MTV3D65.CONST_TV_FOG fogAlgorithm = MTV3D65.CONST_TV_FOG.TV_FOG_LINEAR;
                MTV3D65.CONST_TV_FOGTYPE fogType = MTV3D65.CONST_TV_FOGTYPE.TV_FOGTYPE_PIXEL;
                
                CoreClient._CoreClient.Atmosphere.Fog_GetType (ref fogAlgorithm, ref fogType);
                
             
                // fog semantics need to be set in shader? 
                Model.Appearance.Shader.SetShaderParameterInteger ("fogType", (int)fogAlgorithm);
				Model.Appearance.Shader.SetShaderParameterVector ("fogColor", fogColor);
              	Model.Appearance.Shader.SetShaderParameterFloat ("fogDensity", density);
				Model.Appearance.Shader.SetShaderParameterFloat ("fogStart", start);
				Model.Appearance.Shader.SetShaderParameterFloat ("fogEnd", end);
//              int fogType 					: FOGTYPE;
//				float3 fogColor 				: FOGCOLOR; 
//				float fogDensity 				: FOGDENSITY;
//				float fogStart 					: FOGSTART;
//				float fogEnd 					: FOGEND;
        	}

            // Primary Draw occurs here.  The above is only for setting up any shader parameters for
            // InstancedGeometry meshes.
            using (CoreClient._CoreClient.Profiler.HookUp("Model Draw"))
                Model.Render(context, Entity, CameraSpacePosition, elapsedSeconds, fxsemantics);
        }
    }

}
