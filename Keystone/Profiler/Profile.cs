using System;
using System.Diagnostics;

namespace Keystone.Profiler
{
	/// <summary>
    /// Description of Profile.
    /// </summary>
    internal class Profile : IProfile
    {
        private string ProfileName;
        private string ProfileCategory;

        //   private long StartTime;
        private double TotalTime;
        private double LastTotal;
        // private bool HookedUp; // TODO: HookedUp var is never used. might be useful for re-entrant sync, but so far it's not used


        public Profile(string Name)
        {
            this.ProfileName = Name;
        }
        public Profile(string Name, string Category)
        {
            this.ProfileName = Name;
            this.ProfileCategory = Category;
        }


        public void Update(double elapsed)
        {
            LastTotal = TotalTime;
            TotalTime += elapsed;
        }

        public void ResetTimer()
        {
            LastTotal = TotalTime;
            TotalTime = 0;
        }

        // we cache the last elapsed since we will only update the display every x interval.
        // thus, our display isn't erratic
        public double ElapsedSeconds
        {
            get { return LastTotal; }
        }

        public double ElapsedMilliseconds { get { return LastTotal * 1000d; } }

        public bool Categorized
        {
            get { return ProfileCategory != null; }
        }

        public string Category
        {
            get { return ProfileCategory; }
        }

        public string Name
        {
            get { return ProfileName; }
        }
    }
}
