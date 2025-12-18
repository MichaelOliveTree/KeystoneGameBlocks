using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;

namespace KeyEdit.Scripting
{
    public class VisualFXAPI : IVisualFXAPI
    {
        #region IVisualFXAPI Members
        #region Draw 2D Textures
        int IVisualFXAPI.RGBA (float r, float g, float b, float a)
        {
        	return AppMain._core.Globals.RGBA (r, g, b, a);
        }
        
        void IVisualFXAPI.DrawQuad(string contextID, float left, float top, float right, float bottom, int color1)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false) 
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            vp.Context.DrawQuad(left, top, right, bottom, color1);
        }
        
        void IVisualFXAPI.DrawTexturedQuad(string contextID, string textureID, float x, float y, float width, float height, float angleRadians, bool alphaBlend)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false) 
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            vp.Context.DrawTexturedQuad(textureID, x, y, width, height, angleRadians, alphaBlend);
        }

        void IVisualFXAPI.DrawTexturedQuad(string contextID, string textureID, float x, float y, float width, float height, float angleRadians, int color1, bool alphaBlend)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false) 
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            vp.Context.DrawTexturedQuad(textureID, x, y, width, height, angleRadians, alphaBlend, color1);
        }
        
        void IVisualFXAPI.DrawTexturedQuad(string contextID, string textureID, float x, float y, float width, float height, float angleRadians, int color1, int color2, bool alphaBlend)
        {
            if (AppMain._core.Viewports.ContainsKey(contextID) == false) 
                throw new ArgumentOutOfRangeException();

            Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
            vp.Context.DrawTexturedQuad(textureID, x, y, width, height, angleRadians, alphaBlend, color1, color2);
        }
                
        //void IVisualFX.DrawTexturedQuad(string contextID, string textureID, float angle, float left, float top, float right, float bottom, bool alphaBlend)
        //{
        //    Keystone.Cameras.Viewport vp = AppMain._core.Viewports[contextID];
        //    vp.Context.DrawTexturedQuad(textureID, angle, left, top, right, bottom, alphaBlend);
        //}
        #endregion
        
        #region Heavy Particle Systems
        int IVisualFXAPI.ParticleEffect_Register(string name, string parentID)
        {
        	// TODO: this code should create a server command similar to how Entities are created so that the particle effect is owned by the server.
        	//       how do we get this guid sychronously from server?  Can the client supply it to the server which then shares it with the other clients?
        	//       we could simply rely on naming convention and as modders, we know the convention is same across all scripts.
        	//       Then we can retrieve by name from Repository.  But then again, we want every instance to be unshared... right?  This is what makes
        	//       the naming convention rather useless... 
        	//       Is our current implementation more or less "ok"?  Did i already get it right the first time?  The main problem is how it's tied
        	//       to root... and what that means for rendering and simulating across all zones.  This is why I think the main change we need to make is
        	//       having Particles[] sorted by Zone buckets.  Most of the Zones will be empty (not contain any particles), but if need be, we can keep
        	//       all the zones array active in memory (right?)
        	
        	// 0 - explosion
        	// 1 - laser
        	// 2 - bullet
        	// 4 - plasma
        	// 5 - rocket
        	// 6 - huge explosion tactical nuke
        	// 7 - spark
        	Keystone.EntitySystems.HeavyParticleSystemManager lasers = (Keystone.EntitySystems.HeavyParticleSystemManager)AppMain._core.SceneManager.FXProviders[(int)Keystone.FX.FX_SEMANTICS.FX_CAMERA_BUBBLE];
        
        	// TODO: here the effect is parented to a Region/Zone not Root unless Root is all there is
        	
//        	int effectID = lasers.CreateEffect (name, AppMain._core.SceneManager.Scenes[0].Root);
			int effectID = lasers.CreateEffect (name, parentID);
			return effectID;
        }
        
        int IVisualFXAPI.ParticleEmitter_Register(int effectID, int emitterType, int maxParticles, float lifeSpan, float interval, int quantityReleased, string texturePath)
        {
        	Keystone.EntitySystems.HeavyParticleSystemManager mgr = (Keystone.EntitySystems.HeavyParticleSystemManager)AppMain._core.SceneManager.FXProviders[(int)Keystone.FX.FX_SEMANTICS.FX_CAMERA_BUBBLE];
        	
        	// TODO: does the IEntitySystem get added to Root?  perhaps for now yes...
        	int emitterID = mgr.CreateEmitter (effectID, emitterType, maxParticles, lifeSpan, interval, quantityReleased, texturePath);
        	return emitterID;
        }
        
        void IVisualFXAPI.ParticleEmitter_SetParameter (int effectID, int emitterID, string parameterName, object value)
        {
        	Keystone.EntitySystems.HeavyParticleSystemManager mgr = (Keystone.EntitySystems.HeavyParticleSystemManager)AppMain._core.SceneManager.FXProviders[(int)Keystone.FX.FX_SEMANTICS.FX_CAMERA_BUBBLE];
        	mgr.SetParticleEmitterParameter(effectID, emitterID, parameterName, value);
        }
        
        void IVisualFXAPI.ParticleEmitter_SetModifier(int effectID, int emitterID, string modifierName, object startValue, object endValue)
        {
        	Keystone.EntitySystems.HeavyParticleSystemManager mgr = (Keystone.EntitySystems.HeavyParticleSystemManager)AppMain._core.SceneManager.FXProviders[(int)Keystone.FX.FX_SEMANTICS.FX_CAMERA_BUBBLE];
        	mgr.SetParticleEmitterModifierr (effectID, emitterID, modifierName, startValue, endValue);
        }
        
        void IVisualFXAPI.ParticleEffect_Trigger(int effectID, string entityID, Vector3d triggerPosition, Vector3d triggerVelocity)
        {
        	Keystone.EntitySystems.HeavyParticleSystemManager mgr = (Keystone.EntitySystems.HeavyParticleSystemManager)AppMain._core.SceneManager.FXProviders[(int)Keystone.FX.FX_SEMANTICS.FX_CAMERA_BUBBLE];
        	Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
        	
        	double gameTime = entity.Scene.Simulation.GameTime.TotalElapsedSeconds;
        	Vector3d globalPosition = triggerPosition;
        	
        	mgr.TriggerEffect (effectID, entity, gameTime, globalPosition, triggerVelocity);
        }
        #endregion

        #region Particle System Members
        Dictionary<string, Keystone.Entities.ModeledEntity> mParticleSystems;
        void IVisualFXAPI.RegisterParticleSystem(string scriptID, string name, string prefabRelativePath)
        {
            if (mParticleSystems == null) mParticleSystems = new Dictionary<string, Keystone.Entities.ModeledEntity>();

            if (mParticleSystems.ContainsKey(name)) return;

            string fullPath = System.IO.Path.Combine(AppMain.MOD_PATH, prefabRelativePath);
            Keystone.Entities.ModeledEntity ps = (Keystone.Entities.ModeledEntity)Keystone.ImportLib.Load(fullPath, false, true, false, null);
            ps.Serializable = false;

            mParticleSystems.Add(name, ps);

            // todo: should we include ability to specifiy MAX_POOL_SIZE?

            // todo: perhaps we can concat the scriptID and the name to create a unique key into the dicitonary hosting all particlesystems. 
            //       - it does seem useful to do this, however, how does that just not end up being different from just ensuring you're assigning a unique key
            //        across all scripts?

            // create the FXParticleSystem if it doesn't already exist
            // the FXParticleSystem manages lifetime of particleSystems and calls Update() and Render()
            //  - actually thats not necessary since im adding the particle prefab to the scene. THe FXParticleSystem only needs to manage the lifetimes and pools

            // name is unique dictionary key per scriptID
        }

        void IVisualFXAPI.SpawnParticleSystem(string scriptID, string name, string parentID, Vector3d relativePosition, Vector3d relativeRotation)
        {
            Keystone.Entities.Entity parent = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(parentID);

            Keystone.Entities.ModeledEntity ps = mParticleSystems[name];
            // todo: 
            parent.AddChild(ps);
        }

        double IVisualFXAPI.GetParticleDuration(string name)
        {
            Keystone.Entities.ModeledEntity ps = mParticleSystems[name];
            Keystone.Elements.ParticleSystem system = (Keystone.Elements.ParticleSystem)ps.Model.Geometry;
            return system.Duration;
        }

        void IVisualFXAPI.StartParticleSystem()
        {
            throw new NotImplementedException();
        }

        void IVisualFXAPI.StopParticleSystem()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Lighting
        void IVisualFXAPI.SetGlobalAmbient (float r, float g, float b)
        {
        	AppMain._core.Light.SetGlobalAmbient (r, g, b);
        }
        #endregion
        
        #region Shader members
        string IVisualFXAPI.GetShaderID (string modelID)
        {
        	Keystone.Elements.Model model = (Keystone.Elements.Model)Keystone.Resource.Repository.Get(modelID);
        	// TODO: not only have I not added any error checking, I'm also assuming that the shader is set on default Appearance and not on a specific Group
            return model.Appearance.Shader.ID;
        }
        
        void IVisualFXAPI.SetShaderParameterString(string shaderID, string parameterName, string value)
        {
        }

        void IVisualFXAPI.SetShaderParameterColor(string shaderID, string parameterName, Keystone.Types.Color value)
        {
        }
        

        void IVisualFXAPI.SetShaderParameterBool(string shaderID, string parameterName, bool value)
        {
        }

        void IVisualFXAPI.SetShaderParameterInteger(string shaderID, string parameterName, int value)
        {
        }

        /// <summary>
        /// Shortcut for the general purpose 
        /// </summary>
        /// <param name="shaderID"></param>
        void IVisualFXAPI.SetShaderParameterFloat(string shaderID, string parameterName, float value)
        {
            // shader parameters are stored in the appearanceGroup node which are unique nodes
            // and NOT shareable like Shader nodes.  Thus get/set of parameters values
            // must occur through there.

            // Then during mesh.Render() is parameters in the mesh match parameters in the
            // appearanceGroup (hashcode) we can skip, otherwise we must send the parameters

            Keystone.Shaders.Shader shader = (Keystone.Shaders.Shader)Keystone.Resource.Repository.Get(shaderID);
            if (shader == null) return; // shader might not be loaded yet
            shader.SetShaderParameterFloat (parameterName, value);
        }

        void IVisualFXAPI.SetShaderParameterFloatArray(string shaderID, string parameterName, float[] value)
        {
            // shader parameters are stored in the appearanceGroup node which are unique nodes
            // and NOT shareable like Shader nodes.  Thus get/set of parameters values
            // must occur through there.

            // Then during mesh.Render() is parameters in the mesh match parameters in the
            // appearanceGroup (hashcode) we can skip, otherwise we must send the parameters

            Keystone.Shaders.Shader shader = (Keystone.Shaders.Shader)Keystone.Resource.Repository.Get(shaderID);
            if (shader == null) return; // shader might not be loaded yet
            shader.SetShaderParameterFloatArray (parameterName, value);
        }
        
        void IVisualFXAPI.SetShaderParameterVector3(string shaderID, string parameterName, Keystone.Types.Vector3d value)
        {
            Keystone.Shaders.Shader shader = (Keystone.Shaders.Shader)Keystone.Resource.Repository.Get(shaderID);
            if (shader == null) return; // shader might not be loaded yet
            if (shader == null)
            {
                System.Diagnostics.Trace.WriteLine("VisualFX.SetShaderParameterVector3() -- Could not find node '" + shaderID + "'");
                return;
            }

            // shader parameters are always assigned immediately.  No need to queue anything
            // NOTE: This call in this method is written by the game developer writing the game front end
            // and not the game player who is free to modify scripts.  The game player is still prevented
            // from directly accessing keystone datatypes.
            shader.SetShaderParameterVector(parameterName, value);
        }

        void IVisualFXAPI.SetShaderParameterVector3(string shaderID, string parameterName, Keystone.Types.Vector3f value)
        {
            Keystone.Shaders.Shader shader = (Keystone.Shaders.Shader)Keystone.Resource.Repository.Get(shaderID);
            if (shader == null) return; // shader might not be loaded yet
            if (shader == null)
            {
                System.Diagnostics.Trace.WriteLine("VisualFX.SetShaderParameterVector3f() -- Could not find node '" + shaderID + "'");
                return;
            }

            // shader parameters are always assigned immediately.  No need to queue anything
            // NOTE: This call in this method is written by the game developer writing the game front end
            // and not the game player who is free to modify scripts.  The game player is still prevented
            // from directly accessing keystone datatypes.
            shader.SetShaderParameterVector(parameterName, value);
        }
                
        void IVisualFXAPI.SetShaderParameterVector2(string shaderID, string parameterName, Keystone.Types.Vector2f value)
        {
            Keystone.Shaders.Shader shader = (Keystone.Shaders.Shader)Keystone.Resource.Repository.Get(shaderID);
            if (shader == null) return; // shader might not be loaded yet
            if (shader == null)
            {
                System.Diagnostics.Trace.WriteLine("VisualFX.SetShaderParameterVector2() -- Could not find node '" + shaderID + "'");
                return;
            }

            // shader parameters are always assigned immediately.  No need to queue anything
            // NOTE: This call in this method is written by the game developer writing the game front end
            // and not the game player who is free to modify scripts.  The game player is still prevented
            // from directly accessing keystone datatypes.
            shader.SetShaderParameterVector2(parameterName, value);
        }
                
        void IVisualFXAPI.SetShaderParameterMatrix(string shaderID, string parameterName, Keystone.Types.Matrix value)
        {
        }

        void IVisualFXAPI.SetShaderParameterTexture(string shaderID, string parameterName, int value)
        {
            // obsolete ? - script will know exactly which texture slot index to use
            //string parameterName = "";
            //switch (textureSlot)
            //{
            //    case 0 - 7:
            //        parameterName = "TEXTURE" + textureSlot.ToString();
            //        break;
            //    default:
            //        return; // invalid texture slot
            //}

        }
        #endregion 
    #endregion
    }
}
