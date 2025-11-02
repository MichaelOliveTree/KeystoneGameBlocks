using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX.AudioVideoPlayback;
using Microsoft.DirectX.Direct3D;

namespace Keystone.Sound
{
    public class Movie // movie rendered to a billboard
    {
        private Microsoft.DirectX.AudioVideoPlayback.Video m_video;
        private Device m_device;
        private VertexBuffer m_vertices;
        private IntPtr m_Handle;
        private Rectangle m_bounds;
        private Pool _pool;

        public Movie(IntPtr Handle, Rectangle Bounds, string Filename)
        {
            m_Handle = Handle;
            m_bounds = Bounds;

            if (!File.Exists(Filename))
            {
                MessageBox.Show("Movie file not found.", "AudioDX Error", MessageBoxButtons.OK,
                                MessageBoxIcon.Exclamation);
                return;
            } //error out of the file isnt found

            m_video = new Microsoft.DirectX.AudioVideoPlayback.Video(Filename);
            m_video.TextureReadyToRender += new TextureRenderEventHandler(m_video_TextureReadyToRender);
            m_video.Disposing += new EventHandler(m_video_Disposing);

            InitGraphics();
        }

        private void m_video_Disposing(object sender, EventArgs e)
        {
            while (true) ;
        }

        private void InitGraphics()
        {
            PresentParameters pres = new PresentParameters();
            pres.Windowed = true;
            pres.SwapEffect = SwapEffect.Discard;

            // TODO: note i switched this from Pool.Default to Pool.Managed but never tested it.  If things tstart to explode change it back to Default
            // but remember that we'll have to use Core.RegisterOnNotifyDeviceReset()
            _pool = Pool.Managed;
            m_device = new Device(0, DeviceType.Hardware, m_Handle, CreateFlags.SoftwareVertexProcessing, pres);
            m_vertices = CreateVertexBuffer(m_device);
        }

        protected VertexBuffer CreateVertexBuffer(Device device)
        {
            VertexBuffer buf =
                new VertexBuffer(typeof (CustomVertex.TransformedTextured), 4, device, 0,
                                 CustomVertex.TransformedTextured.Format, _pool);

            PopulateVertexBuffer(buf);

            return buf;
        } //end for virtex

        protected void PopulateVertexBuffer(VertexBuffer vertices)
        {
            CustomVertex.TransformedTextured[] verts = (CustomVertex.TransformedTextured[]) vertices.Lock(0, 0);

            int i = 0;
            verts[i++] = new CustomVertex.TransformedTextured(10, 10, 0.5f, 1, 0, 0);
            //.PositionTextured( -1.0F, 0, 0, 0, 0 );
            verts[i++] = new CustomVertex.TransformedTextured(m_bounds.Width - 10, 10, 0.5f, 1, 1, 0);
            //.PositionTextured( 1.0F, 0, 0, 1, 0 );
            verts[i++] = new CustomVertex.TransformedTextured(10, m_bounds.Height - 10, 0.5f, 1, 0, 1);
            //.PositionTextured( -1.0F, -1.0f, 0, 0, 1 );
            verts[i++] = new CustomVertex.TransformedTextured(m_bounds.Width - 10, m_bounds.Height - 10, 0.5f, 1, 1, 1);
            //.PositionTextured( 1.0F, 1.0f, 0, 1, 1 );

            vertices.Unlock();
        } //end Virtex Buffer

        private void m_video_TextureReadyToRender(object sender, TextureRenderEventArgs e)
        {
            m_device.Clear(ClearFlags.Target, SystemColors.Control, 1.0F, 0);
            m_device.BeginScene();
            m_device.SetTexture(0, e.Texture);
            m_device.VertexFormat = CustomVertex.TransformedTextured.Format;

            m_device.SetStreamSource(0, m_vertices, 0);
            m_device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

            m_device.EndScene();
            m_device.Present();
        } //Do Drawing here

        public void Play()
        {
            if (m_video != null)
            {
                if (m_video.State == StateFlags.Paused)
                    m_video.Play();
                else
                    m_video.RenderToTexture(m_device);
            } //check that video is initialized
        } //play Embedded

        public void PlayExtern()
        {
            m_video.Play();
        } //play on external player

        public void Pause()
        {
            m_video.Pause();
        } //works on both

        public void Stop()
        {
            m_video.Stop();
        } //stop playback

        public void StopExtern()
        {
            m_video.Stop();
        } //stop external playback

        public Microsoft.DirectX.AudioVideoPlayback.Video Video
        {
            get { return m_video; }
        }
    }
}