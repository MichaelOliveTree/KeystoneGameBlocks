using System;
using System.Diagnostics;
using System.IO;
using Keystone.Types;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Keystone.Elements;
using Keystone.Traversers;

namespace Keystone.Sound
{
    public class AudioClip3D : Node //, IPageableTVNode
    {
        private System.Collections.Generic.List<SecondaryBuffer> mSoundList;
        private System.Collections.Generic.List < Buffer3D> mBuffer3DList;
        private SecondaryBuffer _buffer;
        private Buffer3D mBuffer3D;
        
        private bool playing = false;
        private string _filename;

        private Device _device;
        private BufferDescription _description;
        private BufferPlayFlags _flags;

        // Secondary buffers hold a single audio stream and must be explicitly created by the application. 
        // Each application must create at least one secondary buffer to store and play sounds. 
        // Each secondary buffer also has a specific waveform format (described in the WaveFormat structure),
        // and only sound data that matches that format can be loaded into that secondary buffer.
        //
        // An application can play sounds of differing formats by creating a separate secondary buffer for 
        // each format and letting the API mix them into a common format in the primary buffer. To mix sounds 
        // in two different secondary buffers, simply play them at the same time and let the API mix them in the 
        // primary buffer. The only limitation to the number of different secondary buffers that can be mixed 
        // is the processing power of the system, but remember that any additional processing required will 
        // also slow down your game. We have not added any AI or physics computations, but we should be careful 
        // with the available processing power.
        // ...
        // The sound in the secondary buffer can be played once or set up to loop. If the sound to be played is
        // short, it can be loaded into the buffer in its entirety (called a static buffer), but longer sounds 
        // must be streamed. It is the responsibility of the application to manage the streaming of the sound to the buffer.

        // http://blogs.msdn.com/coding4fun/archive/2006/11/06/999786.aspx 
        internal AudioClip3D(string name, string file, Device dev) : this(name)
        {
        	if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("AudioClip.ctor()");
            if (string.IsNullOrEmpty(file)) throw new ArgumentNullException("AudioClip.ctor()");
            if (dev == null) throw new ArgumentNullException("AudioClip.ctor()");


            // TODO: is this true about vista and sound processing requiring software mode?
            // "You can still use DirectSound on Vista but (on my PC at least) you need to specify software processing when you create the DirectSound device."
            // https://en.wikipedia.org/wiki/DirectSound
            // seems it should be ok using software processing in the age of multi-core cpus
            
            
            
            // When a buffer is created you have to specify the control options for that buffer using the BufferDescription class. 
            // If you use a property of the buffer without first setting it in the control properties, an exception is thrown. 
            // The control options can be combined by either setting each property to true or combined in the Flag property.

            // Volume is expressed in hundredths of a decibel and ranges from 0 (full volume) to -10,000 (completely silent). 
            // The decibel scale is not linear, so you may reach effective silence well before the volume setting reaches true 
            // silence at -10,000. There is also no way to increase the volume of the sound above the volume it was recorded at,
            // so you have to make sure to record the sound with a high enough volume to at least match the desired maximum volume in the game.

            // Pan is expressed as an integer and ranges from -10,000 (full left) to +10,000 (full right), with 0 being center.

            //The frequency value is expressed in samples per seconds and represents the playback speed of the buffer. 
            // A larger number plays the sound faster and raises the pitch while a smaller number slows the speed down and lowers the pitch. 
            // To reset the sound to its original frequency, simply set the frequency value to 0. The minimum value for frequency is 100 
            // and the maximum value is 200,000.
            // http://blogs.msdn.com/coding4fun/archive/2006/11/06/999786.aspx 

            
            BufferDescription desc = new BufferDescription();
            desc.Control3D = true;
            desc.ControlEffects = false;  // <-- required for cloning. but then does this mean we cannot set effects? TODO: find out.
            desc.ControlVolume = true;
            desc.ControlFrequency = true;
            desc.ControlPan = false; // <-- why does enabling this cause a crash? Even with .Control3D = false it fails
            desc.Guid3DAlgorithm = DSoundHelper.Guid3DAlgorithmHrtfFull;  // Others = DSoundHelper.Guid3DAlgorithmHrtfLight , DSoundHelper.Guid3DAlgorithmDefault 
            //   desc.Mute3DAtMaximumDistance = true;
            desc.GlobalFocus = false;  // Tells the primary buffer to continue play this buffer, even if the app loses focus.
            //desc.LocateInSoftware = true;
            
            
            _description = desc;
            _device = dev;
            _filename = file;
            _buffer = LoadSoundFile(file);
            mBuffer3D = new Buffer3D (_buffer);
            InitializeBuffer3D (mBuffer3D);
            
        }

        private AudioClip3D (string name) : base (name)
        {
        	mSoundList = new System.Collections.Generic.List<SecondaryBuffer>();
        	mBuffer3DList = new System.Collections.Generic.List<Buffer3D>();
        }
        
        
        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }
        

        private SecondaryBuffer LoadSoundFile(string filename)
        {
            if (_buffer != null)
            {
                _buffer.Stop();
                _buffer.SetCurrentPosition(0);
                // TODO: should we dispose _buffer?
            }

            FileStream stream = File.Open(filename, FileMode.Open);
            _description.BufferBytes = (int) stream.Length;
            SecondaryBuffer tmp = new SecondaryBuffer(stream, _description, _device);
            stream.Close();

            if (tmp == null) throw new ArgumentNullException("audioClip");
                        
            if (tmp.Caps.LocateInHardware)
                Trace.WriteLine("AudioClip.LoadSoundFile() - File loaded using hardware mixing.");
            else
            	// Windows Vista, 7 (and beyond) will always use software mixing.
                Trace.WriteLine("AudioClip.LoadSoundFile() - File loaded using software mixing.");

            // verify the buffer description in the audio clip has appropriate buffer description for SPatialSound interoperability
            if (_description.Control3D == false)
                throw new ArgumentException("SoundNode.ctor() - Audioclip buffer must allow 3DControl.");

            Buffer3D mBuffer3d = new Buffer3D(tmp);
            mBuffer3d.Mode = Mode3D.HeadRelative; // Enable possibility to move sound's source and listener positons

            try
            {
                if (tmp.NotVirtualized)
                    Debug.WriteLine("SoundNode.ctor() - The 3D virtualization algorithm requested is not supported under this " +
                                    "operating system.  It is available only on Windows 2000, Windows ME, and Windows 98 with WDM " +
                                    "drivers and beyond. This buffer was created without virtualization.");
            }
            catch (ArgumentException)
            {
                // Check to see if it was a stereo buffer that threw the exception.
                Debug.WriteLine("SoundNode.ctor() - Wave file must be mono for 3D control.");
                return null;
            }
            catch
            {
                // Unknown error, but not a critical failure, so just update the status
                Debug.WriteLine("SoundNode.ctor() - Unknown, non critical failure.");
                return null;
            }

            if (WaveFormatTag.Pcm != (WaveFormatTag.Pcm & _description.Format.FormatTag))
            {
                Trace.WriteLine("SoundNode.ctor() - Wave file must be PCM for 3D control.");
            }
            
            return tmp;
        }
        
        private void InitializeBuffer3D (Buffer3D buffer)
        {
        	buffer.Mode = Mode3D.HeadRelative; // Enable possibility to move sound's source and listener positons
	        // buffer.Mode = Mode3D.Normal;
	
	    	//MinDistance is the distance at which the sound is at full volume. It doesn’t get louder when it gets closer than this
	    	buffer.MinDistance = 5;
	    	//MaxDistance is the distance past which the sound no longer gets quieter. Its default is one billion meters
	    	buffer.MaxDistance = 100;
        }
        
        public void SetFX()
        {
        	//In DirectSound 9 there are – 9 effects: Chorus, Compressor, Distortion, Echo, Flanger, Gargle, Interactive3DLevel2Reverb, ParamEqualizer, WavesReverb.
            // TODO: verify this isnt possible if in the description. ControlEffects = True; ?
            EffectDescription[] fx = new EffectDescription[1];
            fx[0].GuidEffectClass = DSoundHelper.StandardParamEqGuid;
            _buffer.SetEffects(fx);
            ParamEqEffect eqEffect = (ParamEqEffect) _buffer.GetEffects(0);
            EffectsParamEq eqParams = eqEffect.AllParameters;
            // Specific properties!
            eqParams.Bandwidth = 36; // Apply a gain on the highest frequency
            eqParams.Gain = ParamEqEffect.GainMax;
            eqEffect.AllParameters = eqParams;
        }
                
        public BufferDescription Description
        {
            get { return _description; }
        }

        public SecondaryBuffer Buffer
        {
            get { return _buffer; }
        }

        public WaveFormat Format
        {
            get { return _buffer.Format; }
        }

        public BufferCaps Caps
        {
            get { return _buffer.Caps; }
        }

        public bool Playing
        {
            get { return playing; }
            set { playing = value; }
        }

        public int Volume
        {
            get { return _buffer.Volume; }
            set { _buffer.Volume = value; }
        }
        

//        public float MinDistance
//        {
//            get { return mBuffer3d.MinDistance; }
//            set { mBuffer3d.MinDistance = value; }
//        }
//
//        public float MaxDistance
//        {
//            get { return mBuffer3d.MaxDistance; }
//            set { mBuffer3d.MaxDistance = value; }
//        }
        
        public void Play(Vector3d translation)
        {
        	// TODO: the following might be needed if we want sounds to play when the app window loses focus 
        	// BufferDescription bd;
        	// bd.Flags = BufferDescriptionFlags.GlobalFocus
        	// 
        	this.Play (translation, 0, BufferPlayFlags.Default );
        }
                
        public void Play(Vector3d translation, int priority, BufferPlayFlags flags)
        {
            _flags = flags;
            if (RestoreBuffer())
            {
                _buffer = LoadSoundFile(_filename);
                _buffer.SetCurrentPosition(0);
            }
            
            if (_buffer.Status.Playing )
            {
				// get free secondary buffer and 3d buffer
				Buffer3D buffer3d;
        		SecondaryBuffer secondary = GetSecondaryBuffer(priority, flags, out buffer3d);
        	
            
        		Vector3 position;
        		// TODO: is the translation value in camera space coords?
            	position.X = (float)translation.x;
            	position.Y = (float)translation.y;
            	position.Z = (float)translation.z;
            	buffer3d.Position = position; 
	    		secondary.Play (priority, flags);
            }
            else
            {
	            _buffer.Play(priority, flags);
            }
        }

        // Not thread safe
        private SecondaryBuffer GetSecondaryBuffer(int priority, BufferPlayFlags flags, out Buffer3D buffer3d)
        {
        	if (mSoundList == null) 
        	{
        		mSoundList = new System.Collections.Generic.List<SecondaryBuffer>();
        		mBuffer3DList = new System.Collections.Generic.List<Buffer3D>();
        	}
        	
        	// recycle existing buffers
        	for (int i = 0; i < mSoundList.Count; i++)
        		if (mSoundList[i].Status.Playing == false)
        		{
    	    		buffer3d = mBuffer3DList[i];
        			return mSoundList[i];
        		}
        	
        	// create new
    		SecondaryBuffer secondary = _buffer.Clone(_device);
    		buffer3d = new Buffer3D(secondary);
    		
    		mBuffer3DList.Add (buffer3d);
    		mSoundList.Add (secondary);
    		return secondary;
        }
        
        private bool RestoreBuffer()
        {
            if (_buffer.Status.BufferLost)
            {
                while (_buffer.Status.BufferLost)
                {
                    _buffer.Restore();
                }
                return true;
            }
            return false;
        }

        public void Stop()
        {
            _buffer.Stop();
        }

        public EffectsReturnValue[] SetEffects(EffectDescription[] fxdesc)
        {
            return _buffer.SetEffects(fxdesc);
        }

        public object GetEffects(int index)
        {
            return _buffer.GetEffects(index);
        }

        public void SetCurrentPlayPosition(int position)
        {
            _buffer.SetCurrentPosition(position);
        }

        public BufferStatus Status
        {
            get { return _buffer.Status; }
        }

        public int PlayPosition
        {
            get { return _buffer.PlayPosition; }
        }

        public int Pan
        {
            get { return _buffer.Pan; }
            set { _buffer.Pan = value; }
        }

        public int Length
        {
            get { return _buffer.Caps.BufferBytes; }
        }

        public int Duration
        {
            get { return Length/_buffer.Format.AverageBytesPerSecond; }
        }

        private void CalcTime()
        {
            int toth;
            int totm;
            int tots;

            int tot = Length/_buffer.Format.AverageBytesPerSecond;
            toth = tot/3600;
            totm = (tot - (toth*3600))/60;
            tots = tot - (toth*3600) - (totm*60);

            int h;
            int m;
            int s;

            int now = PlayPosition/_buffer.Format.AverageBytesPerSecond;
            h = now/3600;
            m = (now - (h*3600))/60;
            s = now - (h*3600) - (m*60);
        }
        
        #region IDisposable
		protected override void DisposeManagedResources()
		{
			base.DisposeManagedResources();
			
			if (mSoundList != null)
			{
				for (int i =0 ; i < mSoundList.Count; i++)
				{
					mSoundList[i].Dispose();
					mBuffer3DList[i].Dispose();
				}
			}
			
			if (_buffer != null) _buffer.Dispose();
			if (mBuffer3D != null)	mBuffer3D.Dispose();
            
            // TODO: decrement ref of AudioClip?
		} 
		#endregion
      
    }
}