using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;

namespace KeyEdit.Scripting
{
    public class AudioFXAPI : IAudioFXAPI
    {
    	public void RegisterMusicFile (string resourcePath)
    	{
    		string path = System.IO.Path.Combine (AppMain.MOD_PATH , resourcePath);
    		//AppMain._core.AudioManager.CreateMusic (path); 
    		
  		
    	}
    	
    	public void RegisterSoundClip (string name, string resourcePath)
    	{
    		string path = System.IO.Path.Combine (AppMain._core.ModsPath, resourcePath);
    		
            Keystone.Sound.AudioClip3D clip = AppMain._core.AudioManager.CreateSoundNode(name, path);  
    	}
    	
    	void IAudioFXAPI.Sound_Play(string entityID, string soundName)
    	{
    		Keystone.Sound.AudioClip3D clip = (Keystone.Sound.AudioClip3D)Keystone.Resource.Repository.Get (soundName);
    		Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
    		
    		// TODO: this translation must be in camera space since our listener is fixed at origin right?
    		clip.Play (entity.Translation);
    	}
    	
        void IAudioFXAPI.Sound_Play(string entityID, int soundID)
        {
        	// TODO: 3D soundclips must be relative to camera position (aka origin since we use cameraspace fixed at origin)
        	
            // if entity or sound is invalid, return

            // 
        }
        
        void IAudioFXAPI.Music_Play (int musicID)
        {
        }
    }

}
