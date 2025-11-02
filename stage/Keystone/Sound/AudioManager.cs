using System;
using System.Diagnostics;
using Keystone.Types;
using Microsoft.DirectX.DirectSound;
using MTV3D65;
using VorbisDotNet;
using Buffer=Microsoft.DirectX.DirectSound.Buffer;

namespace Keystone.Sound
{
    // TODO: Crap, debating on making this a static class.  The idea would be so events wouldnt need a reference
    // but why not?  Events will be done by the Simulation which will have access to this class directly. 
    // TODO: AudioManager is accessible to AudioFXAPI via AppMain._core.AudioManager 
    //
    // Audio System is initialized here.
    //
    //
    // SoundEvents will play AudioClips and MusicClips directly.  If a buffer
    // needs to play multiple times, it will duplicate the buffer as necessary
    // and use a cache to expire sounds that haven't been played in a long time.
    //
    //
    /// <summary>
    /// Terrain types can make footstep sounds.  The idea is that you can associate a SoundMap  
    /// to terrain or a SoundMaterial to say a ladder or a duct and then during traversal, if the avatar
    /// is moving and making foot contact with this material, the sound is initiated or continued if it's already looping.
    /// So alot of that would tie into the player's state (e.g. IsUsingItem) and/or it's collision (collding with ground, colliding with duct)
    /// </summary>
    public class AudioManager : IDisposable
    {
        private IntPtr _handle;
        private bool _initialized;
        private bool _enabled;
        private Listener _listener;
        private SoundCard _soundCard;
        private WaveFormat mFormat;


        // The devices
        private Capture applicationCapture = null;
        private DevicesCollection devicesCollection = null;
        private CaptureDevicesCollection captureCollection = null;

        private string[] _soundCardsDescriptions;
        private Guid _currentSoundCard; //guid
        private string _currentSpeaker;


        public AudioManager()
        {
            // Retrieve the DirectSound Devices and Capture devices.
            devicesCollection = new DevicesCollection();
            _soundCardsDescriptions = new string[devicesCollection.Count];
            for (int i = 0; i < devicesCollection.Count; i++)
            {
                _soundCardsDescriptions[i] = devicesCollection[i].Description;
            }
            // capture would only be required if we did network voice
            captureCollection = new CaptureDevicesCollection();
        }

        /// <summary>
        /// Initialized by CoreClient.
        /// </summary>
        /// <param name="hwnd"></param>
        public void Initialize(IntPtr hwnd)
        {
            try
            {
                if (IntPtr.Zero == hwnd) throw new ArgumentNullException();

                _handle = hwnd;

                //default format
                WaveFormat format = new WaveFormat();
                format.FormatTag = WaveFormatTag.Pcm; //wav must be PCM
                format.Channels = 2; // 2 channels for stereo, 1 for mono
                format.SamplesPerSecond = 22050;
                format.BitsPerSample = 16;
                // Block alignment, in bytes. The block alignment is the minimum atomic unit of data.
                format.BlockAlign = (short) (format.BitsPerSample / 8 * format.Channels);
                format.AverageBytesPerSecond = format.SamplesPerSecond * format.BlockAlign;
                mFormat = format;

                Initialize();

                // for streaming ogg buffers, we must initialize the VorbisStreamManager which controls the updating of the buffer with forward data
                // otherwise you have to respond to events yourself to update the buffers and we dont want that.
                VorbisStreamManager.Initialize(true);
            }
            catch
            {
                Trace.WriteLine("Failed to initiliaze sound system.");
                _initialized = false;
            }
        }

        private void Initialize()
        {
            // create the device
            if (Guid.Empty == _currentSoundCard)
                _soundCard = new SoundCard(_handle, CooperativeLevel.Priority);
            else
                _soundCard = new SoundCard(_handle, _currentSoundCard, CooperativeLevel.Priority);


            // setup a primary buffer, set its format, and create the listener
            // NOTE: Secondary buffers will only need to be recreated if the _soundCard object (and its device) are re-inited.
            //       We are ok with changing the primary buffer format and listener (i believe) without effecting the secondary buffers.
            BufferDescription desc = new BufferDescription();
            desc.PrimaryBuffer = true;
            desc.Control3D = true;
            // apparently none of the below can be set on the listener's buffer.  Only the secondary
            // desc.ControlVolume = true;
            // desc.ControlFrequency = true;
            // desc.ControlPan = false;
            // desc.GlobalFocus = true; 
            // desc.Mute3DAtMaximumDistance = true;
            // desc.Guid3DAlgorithm = DSoundHelper.Guid3DAlgorithmHrtfFull; // High quality algorithms

            //BufferCaps caps = new BufferCaps( );
            //desc.Flags = caps.PrimaryBuffer | caps.Control3D;
            //DSoundHelper.

            Buffer primaryBuffer = new Buffer(desc, _soundCard.Device);
            primaryBuffer.Format = mFormat; //TODO: can this be changed on the fly?

            // TODO: shouldn't listener be part of a ListenerTraverser object? this traverser can find 3d sound nodes placed for ambient sound loops.
            //       perhaps it's attached/parented to the camera?
            _listener = new Listener(primaryBuffer, 1, 1, 1);
//            _listener.Translation = new TV_3DVECTOR(300, 45, 300);

            // when creating a new speaker object, all the speaker types are set to false and we need only set the one
            // we want to true.  If we were to get a reference to the existing speaker config, and wanted to change it and then set it back
            // we'd have to remember to set the previous speaker type = false first.  So the easiest method is to just create a new speaker object.
            // TODO: temp, uncomment. but right now after launch, this isnt getting set from propertybag
            Speakers s = GetSpeaker("Stereo Speakers"); // (_currentSpeaker);
            _soundCard.Speakers = s;

            _initialized = true;
        }


        public string[] AvailableSpeakers
        {
            get
            {
                return
                    new string[] {"Mono", "Stereo Speakers", "Stereo Headphones", "Dolby 4.0", "Dolby 5.1", "Dolby 7.1"};
            }
        }

        public string CurrentSpeaker
        {
            set
            {
                if (AvailableSpeakers != null && AvailableSpeakers.Length > 0 && _currentSpeaker != value)
                {
                    for (int i = 0; i < AvailableSpeakers.Length; i++)
                    {
                        if (AvailableSpeakers[i] == value)
                        {
                            _currentSpeaker = value;
                            _initialized = false;
                            return;
                        }
                    }
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        private Speakers GetSpeaker(string name)
        {
            Speakers tmp = new Speakers();
            switch (name)
            {
                case "Mono":
                    tmp.Mono = true;
                    break;

                case "Stereo Speakers":
                    tmp.Stereo = true;
                    break;

                case "Stereo Headphones":
                    tmp.Headphone = true;
                    break;
                case "Dolby 4.0":
                    tmp.Quad = true;
                    break;

                case "Dolby 5.1":
                    tmp.FivePointOne = true;
                    break;
                case "Dolby 7.1":
                    tmp.SevenPointOne = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return tmp;
        }

        public string[] AvailableSoundCards
        {
            get { return _soundCardsDescriptions; }
        }

        public Guid CurrentSoundCard
        {
            get { return _currentSoundCard; }
            set
            {
                if (devicesCollection != null && devicesCollection.Count > 0 && _currentSoundCard != value)
                {
                    for (int i = 0; i < devicesCollection.Count; i ++)
                    {
                        if (devicesCollection[i].DriverGuid == value)
                        {
                            _currentSoundCard = value;
                            _initialized = false;
                            return;
                        }
                    }
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        public Listener Listener
        {
            get { return _listener; }
        }

        internal SoundCard SoundCard
        {
            get { return _soundCard; }
        }


        public WaveFormat Format
        {
            get { return mFormat; }
            set
            {
                // TODO: format if changed must re-set the primary buffer and reset the listener!
                // note: we'll need to add an internal method to reset the listener since we cant
                // destroy it or clients using it will lose it.
                mFormat = value;
                _initialized = false;
            }
        }

        public bool Enable
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        //private bool RestoreBuffer()
        //{

        //}

        public void PlaySound()
        {
            if (!_enabled) return;
            if (!_initialized) Initialize(); //attempt once
            if (!_initialized) return;
        }

        public void PlaySound(Vector3d position)
        {
            if (!_enabled) return;
            if (!_initialized) Initialize(); //attempt once
            if (!_initialized) return;
        }

        public AudioClip3D CreateSoundNode(string name, string file)
        {
        	string id = Resource.Repository.GetNewName (typeof(SoundNode2D));
        	AudioClip3D tmp = new AudioClip3D (name, file, _soundCard.Device);
            
            return tmp;
        }
        
                        
        #region IDisposable Members
        public void Dispose()
        {
            VorbisStreamManager.Terminate();
            _soundCard.Dispose();
        }
        #endregion
    }
}