using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.Generic;

// TODO: TimerAPI will reference this Timer which will be hosted by Core.cs

namespace Keystone.Timers
{
    public class Timer : TimerBase
    {
        public delegate string IntervalCompleted (string nodeID, string name);
        
        private struct TimePeriod
        {
            public string OwnerID;
            public string Name;
            
            // milliseconds
            public int Duration; // the duration in milliseconds this Period will last before completed ("IsReady")
            public int Elapsed;  // get's incremented each frame by elapsedMilliseconds and compared against Duration
            public bool Repeating;
            public int RepeatCount;
            public int RepeatsRemaining;
            
            public bool DeActivateAfterCompleted;
            
            public bool IsReady { get { return Elapsed >= Duration; }
            
            ///<summary>
            /// Rather than delete a Timer, sometimes we just want to 
            /// set IsActive=false and we will skip updates to it.
            ///</summary>
            public bool IsActive 
            {
                get { return mIsActive;}
                set 
                {
                    mIsActive = !mIsActive; // toggle the state
                    Elapsed = 0;            // always reset the Elapsed to 0
                }
            } 
            
            // there should be no need to modify the Elapsed when resuming 
            // because we do not store the starting TickCount, we just 
            // track the elapsed duration
            public bool IsPaused; 
            
            /// notifies the caller that the Interval with the specified "Name" has completed.
            public IntervalCompleted IntervalCompletedCB;
            
        }
        
        private Timer mTimer;
        private List<TimePeriod> mTimePeriods;
        private Dictionary<string, TimePeriod> mKeyedTimePeriods;
        
        public void Interval_Register(string nodeID, string name, int durationMilliseconds, optional bool repeating = false, int repeatCount = 0)
        {
            TimePeriod tp;
            tp.OwnerID = nodeID;
            tp.Name = name;
            
            tp.Duration = durationMilliseconds;
            tp.Elapsed = 0;
            tp.Repeating = repeating;
            tp.RepeatCount = repeatCount;
            tp.RepeatsRemaining = tp.RepeatCount;
            
            tp.IsPaused = false;
            tp.IsActive = true;
            
            tp.DeActivateAfterCompleted = false;
            
            // todo: add it to the dictionary
            string key = GetKey(nodeID, name);
            if (mKeyedTimePeriods == null) mKeyedTimePeriods = new Dictionary<string, TimePeriod>();
            mKeyedTimePeriods.Add (key, tp);
        }
        
        public void Interval_UnRegister (string nodeID, string name)
        {
            // TODO: remove this period from the dictionary
            
        }
        
        ///<summary>
        /// Unregisters all Intervals registered for a specific nodeID
        ///</summary>
        public void Interval_UnRegisterAll (string nodeID)
        {
            
        }
        
        public TimePeriod[] GetAllTimeIntervals (string nodeID)
        {
            // for this to work, we must test for existance of "nodeID" at the start of every key in the dictionary 
            
        }
        
        public bool IsReady (string nodeID, string name)
        {
            if (mKeyedTimePeriods == null) 
            {
                System.Diagnostics.Debug.WriteLine ("GameTime.IsReady() - " + nodeID + " using name " + name + " does not exist.");
                return false;
            }
            string nodeID = GetKey (nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue (key, out tp);
            
            if (success) return tp.IsReady;
                
            return false;
        }
        
        private string GetKey (string nodeID, string name)
        {
            return nodeID + "_" name;
        }
        
        public void Update(int elapsedMilliseconds)
        {
            for (int i = 0; i < periods.Length; i++)
            {
                if (!periods[i].IsActive || periods[i].IsPaused) continue;
                periods[i].Elapsed += elapsedMilliseconds;
                
                if (periods[i].Elapsed >= periods[i].Duration)
                {
                    periods[i].IntervalCompletedCB?.Invoke(periods[i].OwnerID, periods[i].Name);
                    
                    if (periods[i].Repeating) 
                    {
                        int spillOver = periods[i].Elapsed - periods[i].Duration;
                        periods[i].Elapsed = spillOver;
                        periods[i].RepeatsRemaining--;
                        
                        // return before deactivation or removing the timePeriod
                        if (periods[i].RepeatsRemaining > 0)
                            return;
                    }

                    // deactivate or remove this TimePeriod 
                    if (DeActivateAfterCompleted)
                        periods[i].IsActive = false;
                    else
                        Interval_UnRegister (periods[i].OwnerID, periods[i].Name);
                    
                }
            }
        }
    }
    
    public sealed class TimerBase : IDisposable
    {
        [SuppressUnmanagedCodeSecurity(), DllImport("winmm.dll")]
        private static extern int timeBeginPeriod(int uPeriod);

        [SuppressUnmanagedCodeSecurity(), DllImport("winmm.dll")]
        private static extern int timeEndPeriod(int uPeriod);

        //Private _disposed As Boolean = False
        private static Timer mInstance = null;
        private static readonly object mLock = new object();

        private Timer()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>timeBeginPeriod increases the fidelity of the windows timer from 20ms to 1ms.</remarks>
        public static Timer Instance
        {
            get
            {
                lock (mLock)
                {
                    if (mInstance == null)
                    {
                        mInstance = new Timer();
                        timeBeginPeriod(1);
                    }
                    return mInstance;
                }
            }
        }

        public static bool PeriodExpired(int startPeriod, int periodLength)
        {
            return Math.Abs(Environment.TickCount - startPeriod) > periodLength;
        }

        public static int ElapsedMilliseconds(int startPeriod, out int tickCount)
        {
        	tickCount = Environment.TickCount;
            return Math.Abs(tickCount - startPeriod);
        }

   
        public int Time()
        {
            return Environment.TickCount;
        }
        
        #region IDisposable Members
        private static bool _disposed = false;

        public void Dispose()
        {
            timeEndPeriod(1);
            _disposed = true;
        }

        public void finalize()
        {
            if (!_disposed) Dispose();
        }
        #endregion
    }
}