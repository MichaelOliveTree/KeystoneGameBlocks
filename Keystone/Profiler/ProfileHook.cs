using System;
using System.Diagnostics;

namespace Keystone.Profiler
{
	/// <summary>
    /// Description of ProfileHook.
    /// </summary>
    internal class ProfileHook : IProfileHook
    {

        private IProfile mHookedProfile;
        private Stopwatch mStopwatch;

        public ProfileHook(IProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("ProfileHook.ctor() - 'profile' argument cannot be null.");
            mHookedProfile = profile;

            mStopwatch = new Stopwatch();
            mStopwatch.Start();
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                mStopwatch.Stop();
                mHookedProfile.Update(mStopwatch.Elapsed.TotalSeconds);
                mHookedProfile = null;
                //mStopwatch.Dispose();
            }
            this.disposedValue = true;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
