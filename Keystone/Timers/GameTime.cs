using System;

namespace Keystone.Timers
{
	// NOTE: GameTime does not utilize any Windows Timer.  The "elapsedSeconds" is passed in from 
	//       an instance of Keystone.Timers.Timer.cs from within the gameloop in AppMain.cs
	
    // simulated game time. e.g. 1 minute real time with a TIME_FACTOR = 1000 = 1000 minutes in game time
    public class GameTime 
    {        
        public IntervalTimers IntervalTimers;

        private DateTime _time;
        private double mInitialTimeAtStartup;
        private bool mIsPaused;
        private float _timeScaling;                    // used for FFWD and REVERSE time speed ups and slow downs
        private float mGameSecondsPerEachRealSecond;  // eg. 60 gameSeconds for every real life second means every real life minute results in one hour of game time passing
        
        private double _totalElapsed; // total elapsed time since the first update
        private double _elapsedSeconds;
        private double mElapsedGameTimeSeconds;
        private long mTicks;
		private float _julianDay;

        // TODO: use Stopwatch here!!!  

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeScaling">minimum value must be >0.0 unless we want to support reverse time.</param>
        public GameTime(float timeScaling)
        {
        	// TODO: what if 0.0 == paused/stopped
            if (timeScaling <= 0f) throw new ArgumentOutOfRangeException("GameTime.ctor() - timeScaling must be greater than 0.");
            _timeScaling = timeScaling;
            
            _time = new DateTime(2006, 3, 30, 10, 30, 30, 30);
            
            IntervalTimers = new IntervalTimers();


            // http://stackoverflow.com/questions/5248827/convert-datetime-to-julian-date-in-c-sharp-tooadate-safe

            int a = (14 - _time.Month) /12;
            int y = 1975 + 4800 - a;
            int m = _time.Month + 12 * a - 3;
            _julianDay = _time.DayOfYear + (153 * m + 2) / 5 + y * 365 + y / 4 - y / 100 + y / 400 - 32045;
            _julianDay -= 2442414;
            _julianDay -= 1f / 24f;
        }

        private GameTime() : this (1.0f)
        {
        }
        
        public DateTime Time {get {return _time;}}
        
        /// <summary>
        /// Equivalent to gameSecondsPerRealLifeSecond.  
        /// eg. 60 gameSeconds per real life second means 
        /// every real life minute results in one hour of game time passing
        /// </summary>
        public float Scale {get {return _timeScaling;} set{_timeScaling = value;}}
        

        public long Ticks 
        {
        	get {return mTicks;}
        }
        
        public double ElapsedSeconds
        {
            get
            {
                // TODO: TV's AccurateTimeElapsed() fixes issues im having with my own GameTime management.
                //       I need to fix my own system, but for now this works.  
                double elapsedSeconds = (double)CoreClient._CoreClient.Engine.AccurateTimeElapsed();
                elapsedSeconds /= 1000d;
                return elapsedSeconds;
            } // return _elapsedSeconds; }
        }
        
        /// <summary>
        /// Elapsed game time in seconds
        /// </summary>
		public double ElapsedGameTime
		{
			get {return mElapsedGameTimeSeconds; }
		}
	
        public double TotalElapsedSeconds
        {
        	get { return _totalElapsed; }
        }
        
        public double JulianDay // total number of days including fractional days 
        {
        	get 
        	{
        		return _julianDay + _time.TimeOfDay.TotalDays;
        	}
        }

        public void Update(double elapsedSeconds)
        {
        	if (_timeScaling == 0.0f) return; 
        	
            _elapsedSeconds = elapsedSeconds * _timeScaling;
            _totalElapsed += _elapsedSeconds;
            mElapsedGameTimeSeconds = _elapsedSeconds * _timeScaling;
            double elapsedMilliseconds = mElapsedGameTimeSeconds * 1000d;
            _time = _time.Add(new TimeSpan(0, 0, 0, 0, (int)elapsedMilliseconds));
            mTicks = _time.Ticks; 


            IntervalTimers.Update(elapsedSeconds);
        }
    }
}