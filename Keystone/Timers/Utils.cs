using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Keystone.Profiler
{
    internal class Utils
    {
        static uint frame_count = 0;
        static long last_fps_time = -1;

        static long last_frame_time = -1;

        public int GetFrequency()
        {
            if (last_frame_time < 0)
            {
                last_frame_time = DateTime.Now.Ticks;
                last_fps_time = last_frame_time;
            }
            long now = DateTime.Now.Ticks;
            long dt = now - last_frame_time;
            last_frame_time = now;

            int dt_fps = (int)(now - last_fps_time);
            if (dt_fps > 1)
            {
                System.Diagnostics.Debug.WriteLine (string.Format("{0} fps", frame_count / dt_fps));
                frame_count = 0;
                last_fps_time = DateTime.Now.Ticks;
            }
            ++frame_count;
            return dt_fps;
        }
    }
    

}
