using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.Generic;

// TODO: TimerAPI will reference this IntervalTimers.cs which will be hosted by AppMain.mGameTime (see Keystone.Timers.GameTime.cs)
//        TODO: Take a look at MMTimer though because it uses asynchronous timing methods with callbacks and not just polling
namespace Keystone.Timers
{
    
	public class IntervalTimers
    {
        public delegate string IntervalCompleted(string nodeID, string name);
        //private List<TimePeriod> mTimePeriods;
        private Dictionary<string, TimePeriod> mKeyedTimePeriods;

        // NOTE: Using a class for TimePeriod instead of a struct allows us to easily
        //       increment timePeriod.Elapsed and decrement timePeriod.RepeatsRemaining without
        //       having to update this timePeriod within the Dictionary.
        private class TimePeriod
        {
            public string OwnerID;
            public string Name;

            // milliseconds
            public double Duration; // the duration in Seconds this Period will last before completed ("IsReady")
            public double Elapsed;  // get's incremented each frame by elapsedSeconds and compared against Duration
            public bool Repeating;  // todo: im not sure this is useful because if we find that a TimePeriod has elapsed, then the next Elapsed may need to have the remainder added to it if we're just going to automatically repeat and not wait for a handler to process the current elapsed Interval and then start the next Interval if it wants to...
            public int RepeatCount;
            public int RepeatsRemaining;
            private bool mIsActive;
            // there should be no need to modify the Elapsed when resuming 
            // because we do not store the starting TickCount, we just 
            // track the elapsed duration
            public bool IsPaused;

            public bool DeActivateAfterCompleted;

            /// notifies the caller that the Interval with the specified "Name" has completed.
            public IntervalCompleted IntervalCompletedCB;


            public bool IsReady { get { return Elapsed >= Duration; } }

            ///<summary>
            /// Rather than delete a Timer, sometimes we just want to 
            /// set IsActive=false and we will skip updates to it.
            ///</summary>
            public bool IsActive
            {
                get { return mIsActive; }
                set
                {
                    mIsActive = !mIsActive; // toggle the state
                    Elapsed = 0;            // always reset the Elapsed to 0
                }
            }


        }



        public void Register(string nodeID, string name, double durationInSeconds, bool activateImmediately = true, bool repeating = false, int repeatCount = 0)
        {
            TimePeriod tp = new TimePeriod();

            tp.OwnerID = nodeID;
            tp.Name = name;
            tp.Duration = durationInSeconds;
            tp.Elapsed = 0d;
            tp.Repeating = repeating;
            tp.RepeatCount = repeatCount;
            tp.RepeatsRemaining = repeatCount;

            tp.IsPaused = false;
            tp.DeActivateAfterCompleted = false;
            tp.IntervalCompletedCB = null;

            tp.IsActive = activateImmediately;

            string key = GetKey(nodeID, name);
            if (mKeyedTimePeriods == null) mKeyedTimePeriods = new Dictionary<string, TimePeriod>();
            mKeyedTimePeriods.Add(key, tp);
        }

        public void UnRegister(string nodeID, string name)
        {
            // TODO: remove this period from the dictionary
            if (mKeyedTimePeriods == null)
            {
                System.Diagnostics.Debug.WriteLine("GameTime.UnRegister() - " + nodeID + " using name " + name + " does not exist.");
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);

            if (success)
                mKeyedTimePeriods.Remove(key);
            else
                System.Diagnostics.Debug.WriteLine("GameTime.UnRegister() - " + nodeID + " using name " + name + " does not exist.");

        }

        ///<summary>
        /// Unregisters all Intervals registered for a specific nodeID
        ///</summary>
        public void Interval_UnRegisterAll(string nodeID)
        {

        }


        //public TimePeriod[] GetAllTimeIntervals (string nodeID)
        //{
        //    // for this to work, we must test for existance of "nodeID" at the start of every key in the dictionary 
        //    return null;
        //}

        public void Reset(string nodeID, string name)
        {
            if (mKeyedTimePeriods == null)
            {
                System.Diagnostics.Debug.WriteLine("GameTime.Reset() - " + nodeID + " using name " + name + " does not exist.");
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);

            if (success)
                tp.Elapsed = 0d;
            else
                System.Diagnostics.Debug.WriteLine("GameTime.Reset() - " + nodeID + " using name " + name + " does not exist.");

        }

        public bool IsReady(string nodeID, string name)
        {
            if (mKeyedTimePeriods == null)
            {
                //Console.WriteLine("GameTime.IsReady() - " + nodeID + " using name " + name + " does not exist.");
                return false;
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);

            if (success)
            {
                bool result = !tp.IsPaused && tp.IsActive && tp.Elapsed >= tp.Duration;

                //Console.WriteLine("GameTime.IsReady() - " + nodeID + " using name ''" + name + "'' isReady = " + result.ToString());
                return result;
            }

            return false;
        }

        public bool IsActive(string nodeID, string name)
        {
            if (mKeyedTimePeriods == null)
            {
                System.Diagnostics.Debug.WriteLine("GameTime.IsActive() - " + nodeID + " using name " + name + " does not exist.");
                //using HelloBoids.Transform;
                return false;
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);

            if (success) return tp.IsActive;

            return false;
        }

        private string GetKey(string nodeID, string name)
        {
            return nodeID + "_" + name;
        }

        public void Update(double elapsedSeconds)
        {
            if (mKeyedTimePeriods == null || mKeyedTimePeriods.Count == 0) return;

            foreach (TimePeriod period in mKeyedTimePeriods.Values)
            {
                if (!period.IsActive || period.IsPaused) continue;
                period.Elapsed += elapsedSeconds;

                if (period.Elapsed >= period.Duration)
                {
                    period.IntervalCompletedCB?.Invoke(period.OwnerID, period.Name);

                    if (period.Repeating)
                    {
                        double spillOver = period.Elapsed - period.Duration;
                        period.Elapsed = spillOver;
                        period.RepeatsRemaining--;

                        // return before deactivation or removing the timePeriod
                        if (period.RepeatsRemaining > 0)
                            return;
                    }

                    // deactivate or remove this TimePeriod 
                    if (period.DeActivateAfterCompleted)
                        period.IsActive = true;
                    //else
                    //    todo: cant unregister it befire callet can
                    //     check if IsReady== true !!
                    //     unless a delegate or event is raised

                    //    UnRegister(period.OwnerID, period.Name);

                }
            }
        }
    }
    
}