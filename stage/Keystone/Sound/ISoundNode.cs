using Keystone.Types;
using Microsoft.DirectX.DirectSound;

namespace Keystone.Sound
{
    public interface ISoundNode
    {
        Vector3d Position { get; set; }
        float MinDistance { get; set; }
        float MaxDistance { get; set; }

//        source.pitch = Random.Range (lowPitchRange,highPitchRange);
//        float hitVol = coll.relativeVelocity.magnitude * velToVol;
//        if (coll.relativeVelocity.magnitude < velocityClipSplit)
//            source.PlayOneShot(crashSoft,hitVol);
//        else 
//            source.PlayOneShot(crashHard,hitVol);
        
        void Play(int priority, BufferPlayFlags flags);
        // EmitSound( string soundName, number soundLevel=75, number pitchPercent=100, number volume=1, number channel=CHAN_AUTO ) 
        
//         string soundName
//The name of the sound to be played.
//
//number soundLevel=75
//A modifier for the distance this sound will reach, acceptable range is 0 to 511. 100 means no adjustment to the level. See SNDLVL_ Enums
//
//number pitchPercent=100
//The pitch applied to the sound. The acceptable range is from 0 to 255. 100 means the pitch is not changed.
//
//number volume=1
//The volume, from 0 to 1.
//
//number channel=CHAN_AUTO
//The sound channel , see CHAN_ Enums
//NOTE: For weapons this is instead set to CHAN_WEAPON.
//
//         	
    }
}