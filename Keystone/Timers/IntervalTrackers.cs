using System;

namespace Keystone.Timers
{
	
    public class Intervals
    {
        public Dictionary<string, StopWatch> mIntervals;

        public void Add (string name, double intervalInSeconds, bool activate = true)
        {
            if (mIntervals == null) mIntervals = new Dictionary<string, StopWatch>();

            if (mIntervals.Contains(name)) throw new Exception();

            StopWatch sw = new StopWatch (intervalInSeconds);
            if (activate) sw.Start();

            mIntervals.Add (name, sw);
        }

        public void Remove (string name)
        {
            if (mIntervals == null) throw new Exception();

            if (!mIntervals.Contains(name)) throw new Exception();

            mIntervals[name].Stop();
            mIntervals.Remove(name);
        }

        public void Reset (string name)
        {
            if (mIntervals == null) throw new Exception();

            if (!mIntervals.Contains(name)) throw new Exception();

            mIntervals[name].Reset();
            mIntervals.Remove(name);
        }



    }

}