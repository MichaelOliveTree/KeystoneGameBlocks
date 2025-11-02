using System;
using Microsoft.DirectX.DirectSound;
using VorbisDotNet;
using Keystone.Elements;

namespace Keystone.Sound
{
    public class MusicClip // : Node, IPageableTVNode

    {
    	// TODO: VorbisDotNet seem LGPL! Ugh.  No 3d spatial music... but we can use
    	// using Microsoft.DirectX.AudioVideoPlayback.dll
    	// just to play music files.
    	// TODO: http://content.gpwiki.org/index.php/DirectX:DirectSound:Tutorials:VBNET:DX9:Playing_Music  
        // TODO: maybe switch to Ogg Sharp?!
        // http://oggsharp.codeplex.com/
        private VorbisBuffer oggBuffer;
        private Device _device;
        private string _file;
        private bool _streaming;


        public MusicClip(Device device, string file, bool streaming)
        {
            if (device == null || string.IsNullOrEmpty(file)) throw new ArgumentNullException();

            _file = file;
            _device = device;
            _streaming = streaming;

            oggBuffer = new VorbisBuffer(file, device, streaming, VorbisCaps.None, DSoundHelper.Guid3DAlgorithmHrtfFull);
        }

        // oggvorbis secondary buffers cannot be cloned since the SecondaryBuffer (InternalBuffer) is read only.
        //  but we can mimick the functionality 
        public MusicClip(MusicClip music)
            	:  this(music._device, music._file, music._streaming)
        {
        }

        ~MusicClip()
        {
            //Buffer3D.Dispose();
            //oggBuffer.__dtor();
            //oggBuffer.Dispose();

            // VorbisDotNet.VorbisStreamManager.Terminate();
        }

        public SecondaryBuffer Buffer
        {
            get { return oggBuffer.InternalBuffer; }
        }

        // NOTE: GetBuffer3D requires that the VorbisBuffer have been constructed with any one of the DSoundHelper.Guid3D**** enums.  
        // I'm not sure why this is so since it works with Default and the NoVirtualization as well as the High and Low algorithms
        // but without that parameter, GetBuffer3D will always return an unintialized object;
        public Buffer3D Buffer3D
        {
            get { return oggBuffer.GetBuffer3D(); }
        }

        public void Play(int priority, BufferPlayFlags flags)
        {
            oggBuffer.Play((flags & BufferPlayFlags.Looping) == BufferPlayFlags.Looping);
        }
    }
}