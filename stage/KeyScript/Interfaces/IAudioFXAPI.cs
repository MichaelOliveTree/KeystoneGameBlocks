using System;


namespace KeyScript.Interfaces
{
    public interface IAudioFXAPI
    {
        // TODO: is there a better way than register/unregister?
        // 
        void RegisterSoundClip(string name, string relativePath); // wav
        //void RegisterMusicFile(); // ogg
        //void UnRegisterSoundClip();
        //void UnRegisterMusicFile();

        void Sound_Play(string entityID, string soundName);
        void Sound_Play(string entityID, int soundID);
        void Music_Play(int musicID);

    }
}
