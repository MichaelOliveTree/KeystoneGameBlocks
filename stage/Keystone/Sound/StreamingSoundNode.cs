using System;
using System.Diagnostics;
using Keystone.Types;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;

namespace Keystone.Sound
{
    public class StreamingSoundNode : ISoundNode
    {
        private MusicClip music;
        private Buffer3D buffer3d;

        public StreamingSoundNode(MusicClip oggstream)
        {
            // verify the buffer description in the audio clip has appropriate buffer description for SPatialSound interoperability
            if (oggstream == null) throw new ArgumentNullException();

            music = oggstream;
            buffer3d = oggstream.Buffer3D;
            buffer3d.Mode = Mode3D.HeadRelative; // Enable possibility to move sound's source and listener positons

            try
            {
                if (music.Buffer.NotVirtualized)
                    Trace.WriteLine("The 3D virtualization algorithm requested is not supported under this " +
                                    "operating system.  It is available only on Windows 2000, Windows ME, and Windows 98 with WDM " +
                                    "drivers and beyond. This buffer was created without virtualization.");
            }
            catch (ArgumentException)
            {
                // Check to see if it was a stereo buffer that threw the exception.
                Trace.WriteLine("Wave file must be mono for 3D control.");
                return;
            }
            catch
            {
                // Unknown error, but not a critical failure, so just update the status
                Trace.WriteLine("Unknown, non critical failure.");
                return;
            }
        }

        #region ISoundNode Members

        public Vector3d Position
        {
            get { return new Vector3d(buffer3d.Position.X, buffer3d.Position.Y, buffer3d.Position.Z); }
            set { buffer3d.Position = new Vector3((float) value.x, (float) value.y, (float) value.z); }
        }

        public float MinDistance
        {
            get { return buffer3d.MinDistance; }
            set { buffer3d.MinDistance = value; }
        }

        public float MaxDistance
        {
            get { return buffer3d.MaxDistance; }
            set { buffer3d.MaxDistance = value; }
        }

        public void Play(int priority, BufferPlayFlags flags)
        {
            // NOTE: It's important to not call music.Buffer.Play() directly.  OggVorbis buffer needs different handling so go through it.
            music.Play(priority, flags);
        }

        #endregion
    }
}