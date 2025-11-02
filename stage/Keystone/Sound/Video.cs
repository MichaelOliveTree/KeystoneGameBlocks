using System;
using System.Windows.Forms;

namespace Keystone.Sound
{
    public class Video
    {
        private Microsoft.DirectX.AudioVideoPlayback.Video _video;

        /// <summary>
        /// opens a video from an avi file
        /// and plays the first frame inside the panel
        /// </summary>
        public void OpenVideo(Form videoPanel, string filename)
        {
            // open the video
            // remember the original dimensions of the panel
            int height = videoPanel.Height;
            int width = videoPanel.Width;
            // dispose of the old video to clean up resources
            if (_video != null)
            {
                _video.Dispose();
            }

            _video = new Microsoft.DirectX.AudioVideoPlayback.Video(filename);
            _video.Owner = videoPanel;
            // resize to fit in the panel
            videoPanel.Width = width;
            videoPanel.Height = height;
            // play the first frame of the video so we can identify it
            _video.Play();
            _video.Pause();
        }


        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (_video != null)
            {
                _video.Play();
            }
        }
    }
}