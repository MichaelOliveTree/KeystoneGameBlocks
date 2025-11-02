using System;
using Keystone.Types;

namespace KeyScript.Interfaces
{
    public interface IVisualFXAPI
    {
        #region Direct 2D Drawing
        int RGBA (float r, float g, float b, float a);
        // TODO: is there way to do this without having to register resource files?
        //string RegisterTexturedQuad(string resource);
        //string RegisterFont(string typename, int pointSize);
        void DrawQuad(string contextID, float left, float top, float right, float bottom, int color1);
        void DrawTexturedQuad(string contextID, string textureID, float left, float top, float right, float bottom, float angleRadians, bool alphaBlend);
        void DrawTexturedQuad(string contextID, string textureID, float x, float y, float width, float height, float angleRadians, int color1, bool alphaBlend);
        void DrawTexturedQuad(string contextID, string textureID, float x, float y, float width, float height, float angleRadians, int color1, int color2, bool alphaBlend);
        
        //void DrawTexturedQuad(string contextID, string textureID, float angle, float left, float top, float right, float bottom);
        //void DrawText(int font, string text);
        #endregion

        #region Custom Particle Systems
        int ParticleEffect_Register(string name, string parentID);
        int ParticleEmitter_Register(int effectID, int emitterType, int maxParticles, float lifeSpan, float interval, int quantityReleased, string texturePath);
        void ParticleEmitter_SetParameter (int effectID, int emitterID, string parameterName, object value);
        void ParticleEmitter_SetModifier(int effectID, int emitterID, string modifierName, object startValue, object endValue);
        void ParticleEffect_Trigger (int effectID, string entityID, Vector3d triggerPosition, Vector3d triggerVelocity);
        #endregion

        #region Particle System Functions
        void RegisterParticleSystem(string scriptID, string name, string prefabRelativePath);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="name"></param>
        /// <param name="parentID"></param>
        /// <param name="relativePosition"></param>
        /// <param name="relativeRotation">Impact Norrmal for instance</param>
        void SpawnParticleSystem(string scriptID, string name, string parentID, Vector3d relativePosition, Vector3d relativeRotation);
        double GetParticleDuration(string name);
        void StartParticleSystem();
        void StopParticleSystem();
        #endregion
        
        #region Lighting
        void SetGlobalAmbient (float r, float g, float b);
        #endregion
        
        #region Shader Functions
        string GetShaderID (string modelID);
        	
        // shader parameters are stored in the appearanceGroup node which are unique nodes
        // and NOT shareable like Shader nodes.  Thus get/set of parameters values
        // must occur through there.
		
        // Then during mesh.Render() is parameters in the mesh match parameters in the
        // appearanceGroup (hashcode) we can skip, otherwise we must send the parameters

        void SetShaderParameterString(string shaderID, string parameterName, string value);
        void SetShaderParameterColor(string shaderID, string parameterName, Keystone.Types.Color value);
        void SetShaderParameterBool(string shaderID, string parameterName, bool value);
        void SetShaderParameterInteger(string shaderID, string parameterName, int value);
        
        /// <summary>
        /// Shortcut for shader parameter changes of datatype float
        /// </summary>
        /// <param name="shaderID"></param>
        void SetShaderParameterFloat(string shaderID, string parameterName, float value);
        void SetShaderParameterFloatArray(string shaderID, string parameterName, float[] value);
        void SetShaderParameterVector3(string shaderID, string parameterName, Keystone.Types.Vector3d value);
        void SetShaderParameterVector3(string shaderID, string parameterName, Keystone.Types.Vector3f value);
        void SetShaderParameterVector2(string shaderID, string parameterName, Keystone.Types.Vector2f value);
        void SetShaderParameterMatrix(string shaderID, string parameterName, Keystone.Types.Matrix value);
        void SetShaderParameterTexture(string shaderID, string parameterName, int value);
        #endregion 

    }
}
