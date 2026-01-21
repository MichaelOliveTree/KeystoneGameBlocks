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
        private float _timeScaling;                    // 0.0 == paused.  0.5 == half speed slow motion.  1.0 == fullspeed.  2.0 = 2x speed, etc.  used for FFWD and REVERSE time speed ups and slow downs
        private float mGameSecondsPerEachRealSecond;  // eg. 60 gameSeconds for every real life second means every real life minute results in one hour of game time passing
        
        private double _totalElapsed; // total elapsed time since the first update
        private double _elapsedSeconds;
        private double mElapsedGameTimeSeconds;
        private long mTicks;
		private float _julianDay;

        private bool mInitialized;
        private double mFixedStep;

        // TODO: use Stopwatch here!!!  

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeScaling">minimum value must be >0.0 unless we want to support reverse time.</param>
        public GameTime(float fixedFrequencyInSeconds = 60d, float timeScaling = 1.0f)
        {
            if (timeScaling < 0f) throw new ArgumentOutOfRangeException("GameTime.ctor() - REVERSE time is not supported.. yet?");
            _timeScaling = timeScaling;
            
            _time = new DateTime(2006, 3, 30, 10, 30, 30, 30);
            
            mFixedStep = 1d / (double)fixedFrequencyInSeconds;

            IntervalTimers = new IntervalTimers();


            // http://stackoverflow.com/questions/5248827/convert-datetime-to-julian-date-in-c-sharp-tooadate-safe

            int a = (14 - _time.Month) /12;
            int y = 1975 + 4800 - a;
            int m = _time.Month + 12 * a - 3;
            _julianDay = _time.DayOfYear + (153 * m + 2) / 5 + y * 365 + y / 4 - y / 100 + y / 400 - 32045;
            _julianDay -= 2442414;
            _julianDay -= 1f / 24f;
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

        public void Initialize()
        {
            mInitialized = true;
            _totalElapsed = 0.0d;
            mElapsedGameTimeSeconds = 0.0d;
        }

        /// <summary>
        /// Advances the overall elapsedSeconds tracking variables.
        /// Updates IntervalTimers.
        /// Updates overall time in "Game world" values (eg 1 second == 60 seconds game world time)
        /// Returns the elapsedSeconds for this frame 
        /// </summary>
        public double Update()
        {
            if (!mInitialized)
            { 
                Console.WriteLine ("GameTime.Update() - GameTime not initialized.");
                return;
            }
        	if (_timeScaling == 0.0f) return; 
        	
            // TODO: the fixed step needs to be set here
            

            _elapsedSeconds = mFixedStep * _timeScaling;
            _totalElapsed += _elapsedSeconds;
            mElapsedGameTimeSeconds = _totalElapsed * mGameSecondsPerEachRealSecond;

            double elapsedMilliseconds = _totalElapsed * 1000d;
            _time = _time.Add(new TimeSpan(0, 0, 0, 0, (int)elapsedMilliseconds));

            // todo: are we in replay mode?  Ticks shouldn't be advanced here... 
            // revisit this
            //todo: should we be grabbing this from a running mStopWatch?
            //       
            mTicks = _time.Ticks; 


            IntervalTimers.Update(_elapsedSeconds);

            return _elapsedSeconds;
        }
    }
}