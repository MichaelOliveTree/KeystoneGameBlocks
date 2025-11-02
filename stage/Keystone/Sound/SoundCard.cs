using System;
using Microsoft.DirectX.DirectSound;

namespace Keystone.Sound
{
	internal class SoundCard : IDisposable
    {
        private Device _device;

        public SoundCard(IntPtr hwnd, Guid GUID, CooperativeLevel level)
        {
            _device = new Device(GUID);
            _device.SetCooperativeLevel(hwnd, level);
        }

        ~SoundCard()
        {
        }

        public SoundCard(IntPtr hwnd, CooperativeLevel level)
        {
            _device = new Device();
            _device.SetCooperativeLevel(hwnd, level);
        }

        //TODO: can this be changed on the fly?
        public Speakers Speakers
        {
            set { _device.SpeakerConfig = value; }
        }

        public Device Device
        {
            get { return _device; }
        }

        private bool RestoreBuffer()
        {
            return true;
        }
        
        #region IDisposable Members
        public void Dispose()
        {
            _device.Dispose();
        }
        #endregion
    }
}