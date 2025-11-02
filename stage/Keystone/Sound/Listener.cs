using System;
using Keystone.Types;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using MTV3D65;
using Buffer=Microsoft.DirectX.DirectSound.Buffer;

namespace Keystone.Sound
{
    public class Listener
    {
        private Listener3D _listener;
        //private Listener3DSettings _listenerSettings;

        public Listener(Buffer primarybuffer, float distanceFactor, float dopplerFactor, float rolloffFactor)
        {
            if (primarybuffer.Caps.Control3D == false) throw new ArgumentException();

            _listener = new Listener3D(primarybuffer);
            _listener.Deferred = true; // Don't remix after every setting change          
            // The distance factor is a global setting that controls the way vectors are interpreted.
            // It is the number of meters per vector unit. By default it has a value of 1.0. 
            // If you set this number to 2.0, for example, a sound at (1,0,0) will be assumed by 
            // DirectSound to be two meters from a listener at (0,0,0).
            _listener.DistanceFactor = distanceFactor;
            // How pronounced the Doppler shift is (default 1) 
            _listener.DopplerFactor = dopplerFactor; // valid range is 0 to 10
            // How quickly sounds fade with distance (default 1)

            // The rolloff factor controls how quickly sounds get quieter as they move away. 
            // If you set this value to zero (the smallest allowed), sounds will remain the 
            // same volume no matter how far away they are. At 10.0 (the largest allowable value),
            // sounds will very quickly drop in volume as they move away. This is useful, for example,
            // if you wanted to model a mosquito: in your ear it’s loud, but only a few meters away you can’t hear it at all.
            _listener.RolloffFactor = rolloffFactor; // valid range 0.0 to 10.0f  
            _listener.CommitDeferredSettings();
        }

        public Vector3d Position
        {
            get { return new Vector3d(_listener.Position.X, _listener.Position.Y, _listener.Position.Z); }
            set
            {
                Vector3 newPos = new Vector3((float) value.x, (float) value.y, (float) value.z);
                if (_listener.Position == newPos) return;

                Listener3DOrientation o = new Listener3DOrientation();
                o.Front = new Vector3(1, 0, 0);
                o.Top = new Vector3(0, 1, 0);
                _listener.Orientation = o;
                _listener.Position = newPos;
                _listener.Velocity = o.Front;
                _listener.CommitDeferredSettings();
            }
        }

        public Listener3DOrientation Orientation
        {
            get { return _listener.Orientation; }
            set { _listener.Orientation = value; }
        }

        public float DopplerFactor
        {
            get { return _listener.DopplerFactor; }
            set { _listener.DopplerFactor = value; }
        }
    }
}