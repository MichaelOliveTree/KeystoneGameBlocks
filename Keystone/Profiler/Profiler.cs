using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MTV3D65;

namespace Keystone.Profiler
{
	/// <summary>
    /// Zaknafein's TV3D Profiler.  TODO: I should modify this to be more general purpose to include my debug text as well.
    /// </summary>
    public class Profiler
    {
        /// <summary>
        /// Enables or disables all profiler functions
        /// </summary>
        /// <remarks>Enabled by default to prevent an exception on first display</remarks>
        public bool ProfilerEnabled = true;
        /// <summary>
        /// Verbosity will make the profiler print the number of recorded loops and the unprofiled time
        /// </summary>
        public bool Verbose = true;

        public bool ShowFramesPerSecond = true;
        /// <summary>
        /// Enables or disables the fully qualified typename instead of "short" typename
        /// when using the parameterless hook. i.e. ProjectName.ClassName.MethodName
        /// </summary>
        public bool FullyQualifiedTypename;
        /// <summary>
        /// Should the profiler categorize the reflected profiles by their typename?
        /// </summary>
        public bool CategorizeByTypename;

        /// <summary>
        /// The profile for profiler text displaying
        /// </summary>
        private const string PROFILER_DISPLAY_PROFILE = "Profiler Display";
        /// <summary>
        /// The category for debugging information, can be used out of the profiler (hence the public)
        /// </summary>
        public const string DEBUGGING_CATEGORY = "Debugging";

        /// <summary>
        /// The sorted dictionary (hash-table) of all registered profiles
        /// </summary>
        private SortedList<string, Profile> mProfiles;
        /// <summary>
        /// The profiles ordered by category, in another sorted dictionary
        /// </summary>
        private SortedList<string, SortedList<string, Profile>> mCategories;
        /// <summary>
        /// Whether the profiler uses category.  This value is deduced at run-time and is not defined by user.
        /// </summary>
        private bool mUsesCategories;

        private double mTotalElapsedTime;
        //private double mLastTotalElapsedTime;
        private int mLoopCount;
        private int mFramesPerSecond;
        //private long mStartTime;
        private double UPDATE_INTERVAL = 1.0; // 1 second
        private Stopwatch mStopwatch;

        private bool mDisplayIsUpdated = false;
        object syncLock = new object();


        public Profiler()
        {

            mProfiles = new SortedList<string, Profile>();
            mCategories = new SortedList<string, SortedList<string, Profile>>();

            mDebugText = new List<Profiler.DebugText>();

            Register(PROFILER_DISPLAY_PROFILE, DEBUGGING_CATEGORY);
        }

        /// <summary>
        /// Registers a profile
        /// </summary>
        /// <param name="Name">The name of the profile, which is used for hooking it up</param>
        /// <param name="Category">The optional category</param>
        /// <remarks>
        /// No need to test if the profiler is enabled before calling it,
        /// the test it made inside every method of the profiler.
        /// </remarks>
        public void Register(string Name, string Category)
        {
            Profile P = new Profile(Name, Category);
            mProfiles.Add(Name, P);
            if (Category != null)
            {
                mUsesCategories = true;
                if (!mCategories.ContainsKey(Category))
                    mCategories.Add(Category, new SortedList<string, Profile>());
                mCategories[Category].Add(Name, P);
            }
        }

        /// <summary>
        /// Unregisters a profile
        /// </summary>
        /// <param name="Name">The profile's name</param>
        /// <remarks>Unregistering a non-existing profile will throw an exception</remarks>
        public void Unregister(string Name)
        {
            mProfiles.Remove(Name);
            if (mUsesCategories)
            {
                foreach (string Category in mCategories.Keys)
                {
                    if (mCategories[Category].ContainsKey(Name))
                        mCategories[Category].Remove(Name);
                }
            }
        }

        /// <summary>
        /// Marks the start of the profiling loop
        /// </summary>
        public void StartLoop()
        {
            if (ProfilerEnabled)
            {
                //mStartTime = Time.Counter;
                mStopwatch = new Stopwatch();
                mStopwatch.Start();
                mLoopCount++;
            }
        }

        /// <summary>
        /// Marks the end of the profiling loop
        /// </summary>
        public void EndLoop()
        {
            if (ProfilerEnabled)
            {
                mStopwatch.Stop();
                // accumulate mTotalElapsedTime for UPDATE_INTERVAL 
                //mTotalElapsedTime += (float)(Time.Counter - mStartTime) / Time.Frequency;
                //mLastTotalElapsedTime = mTotalElapsedTime;
                mTotalElapsedTime += mStopwatch.Elapsed.TotalSeconds;// Time.ElapsedSeconds(mStartTime); // Hypnotron Feb.12.2015 - added conversion to milliseconds since seconds and milliseconds  


                // ...and will accumulate the timers for 50 frames
                // This could be made with an elapsed time calculation,
                // to accumulate a full second for example
                if (mTotalElapsedTime >= UPDATE_INTERVAL)
                {
                    //ResetTimers();
                }
            }
        }

        /// <summary>
        /// Resets the accumulation timers
        /// </summary>
        private void ResetTimers()
        {
            mDisplayIsUpdated = true;
            //mLastTotalElapsedTime = mTotalElapsedTime;
            mTotalElapsedTime = 0;
            mLoopCount = 0;
            mFramesPerSecond = mLoopCount;

            lock (syncLock)
            {
                foreach (IProfile profile in mProfiles.Values)
                {
                    profile.ResetTimer();
                }
            }
        }

        /// <summary>
        /// Hooks a profile (starts its timer)
        /// </summary>
        /// <param name="Name">The name of the profile to hook up</param>
        /// <returns>An IDisposable instance (see remarks)</returns>
        /// <remarks>
        /// This function can (and should, when possible) be used with a Using declaration
        /// </remarks>
        public IProfileHook HookUp(string Name)
        {
            if (ProfilerEnabled)
            {
                // TODO: this should be made thread safe since when trying to
                //       hookup multiple times in threaded procedure, our timings will
                //       be wrong since the increments arent atmoic operations
                try
                {
                    IProfile profile = null;
                    lock (syncLock)
                    {
                        profile = mProfiles[Name];
                    }
                    IProfileHook hook = new ProfileHook(profile);
                    return hook;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Hooks a new or existing profile for the caller's method
        /// </summary>
        /// <returns>An IDisposable instance (see remarks)</returns>
        /// <remarks>
        /// This function can (and should, when possible) be used with a Using declaration.
        /// Uses reflection to find the last stack frame, and generate the profile name from that.
        /// </remarks>
        public IProfileHook HookUp()
        {
            if (ProfilerEnabled)
            {
                MethodBase CallerMethod = (new StackTrace()).GetFrame(1).GetMethod();

                string CallerType;
                if (FullyQualifiedTypename)
                {
                    CallerType = CallerMethod.DeclaringType.FullName;
                }
                else
                {
                    CallerType = CallerMethod.DeclaringType.Name;
                }

                string CallerID = string.Format("{0}.{1}", CallerType, CallerMethod.Name);

                if (!mProfiles.ContainsKey(CallerID))
                {
                    if (CategorizeByTypename)
                        Register(CallerID, CallerType);
                    else
                        Register(CallerID, "");
                }

                return new ProfileHook(mProfiles[CallerID]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Displays the profiles with their accumulated time and statistics
        /// </summary>
        /// <param name="Offset">The 2D offset to which the text should be printed</param>
        /// <returns>The additional Y offset that the profiler engendered</returns>
        /// <remarks>
        /// This function does NOT force a Text2D.Action_BeginText nor EndText.
        /// It should be enclosed within those at the caller side.
        /// </remarks>
        public int Display(int offsetX, int offsetY, TextOutputHandler AddDebugText)
        {
            if (ProfilerEnabled)
            {
                if (mDisplayIsUpdated == false)
                {
                    foreach (DebugText item in mDebugText)
                        AddDebugText(item.Text, item.OffsetX, item.OffsetY, item.Color);

                    return 0;
                }
                else
                {
                    mDebugText.Clear();

                    int additionalYOffset = 0;
                    double totalProfiledTime = 0;
                    int colorWhite = Utils.RGBA(0.8f, 0.8f, 0.8f, 0.8f);

                    string displayText = null;

                    //HookUp(PROFILER_DISPLAY_PROFILE);

                    if (ShowFramesPerSecond)
                    {
                        displayText = string.Format("{0} FPS", mLoopCount / mLoopCount);
                        AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                        additionalYOffset += 14 + 7;
                    }

                    // CATEGORIZED
                    if (mUsesCategories)
                    {
                        foreach (string Category in mCategories.Keys)
                        {
                            double CategoryTime = 0;

                            // display main Category Header
                            displayText = string.Format("{0} :", Category);
                            AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                            additionalYOffset += 14;

                            // display indented each element under this category
                            foreach (Profile Prof in mCategories[Category].Values)
                            {
                                //if (!(Prof.ElapsedTime == 0)) // skip profile if no elapsed time
                                //{
                                CategoryTime += Prof.ElapsedSeconds;

                                float timeRatio = (float)(Prof.ElapsedSeconds / mTotalElapsedTime);
                                displayText = string.Format(" {0} = {1:P} ({2:0.00} ms)", Prof.Name, timeRatio, Prof.ElapsedMilliseconds / mLoopCount);
                                AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(timeRatio, 1 - timeRatio, 0, 0.8f));

                                totalProfiledTime += Prof.ElapsedSeconds;

                                additionalYOffset += 14;
                                //}
                            }

                            // total stats for this category
                            float CategoryRatio = (float)(CategoryTime / mTotalElapsedTime);
                            displayText = string.Format("Totaling {0:P} ({1:0.00} ms)", CategoryRatio, CategoryTime * 1000f / mLoopCount);
                            AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(CategoryRatio, 1 - CategoryRatio, 0, 0.8f));
                            additionalYOffset += 14;
                            additionalYOffset += 7;
                        }

                        // display main Non-Categorized Header - these are simply Hooked profiles where user didn't enter a category name
                        displayText = "Non-Categorized :";
                        AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                        additionalYOffset += 14;
                    }

                    // NON-CATEGORIZED
                    double NonCategorisedTime = 0;
                    foreach (string Name in mProfiles.Keys)
                    {
                        Profile Prof = mProfiles[Name];

                        // display indented each non-categorized element under the catch-all "Non-Categorized" header
                        if (!(Prof.ElapsedSeconds == 0) && !Prof.Categorized)
                        {
                            float Ratio = (float)(Prof.ElapsedSeconds / mTotalElapsedTime);
                            string Format = "{0} = {1:P} ({2:0.00} ms)";
                            if (Verbose)
                                Format = " " + Format;

                            displayText = string.Format(Format, Name, Ratio, Prof.ElapsedMilliseconds / mLoopCount);
                            AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(Ratio, 1 - Ratio, 0, 0.8f));

                            totalProfiledTime += Prof.ElapsedSeconds;
                            NonCategorisedTime += Prof.ElapsedSeconds;

                            additionalYOffset += 14;
                        }
                    }

                    // total stats for Non-Categorized entries
                    float nonCategorizedTimeRatio = (float)(NonCategorisedTime / mTotalElapsedTime);
                    displayText = string.Format("Totaling {0:P} ({1:0.00} ms)", NonCategorisedTime / mTotalElapsedTime, NonCategorisedTime * 1000f / mLoopCount);
                    AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(nonCategorizedTimeRatio, 1 - nonCategorizedTimeRatio, 0, 0.8f));

                    additionalYOffset += 14 + 7;


                    // NON PROFILED - Header    	
                    if (Verbose)
                    {
                        double NonProfiledTime = mTotalElapsedTime - totalProfiledTime;
                        double NonProfiledTimeRatio = NonProfiledTime / mTotalElapsedTime;
                        displayText = string.Format("{0} = {1:P} ({2:0.000} ms)", "Non-Profiled", NonProfiledTimeRatio, NonProfiledTime * 1000f / mLoopCount);
                        AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                    }

                    //Release(PROFILER_DISPLAY_PROFILE);

                    foreach (DebugText item in mDebugText)
                        AddDebugText(item.Text, item.OffsetX, item.OffsetY, item.Color);

                    mLoopCount = 0;
                    mDisplayIsUpdated = false;
                    return additionalYOffset;
                }
            }
            else
            {
                return 0;
            }
        }

        public void OutputToConsole()
        {
            double totalProfiledTime = 0;
            double NonCategorisedTime = 0;

            if (ProfilerEnabled)
            {
                foreach (string Name in mProfiles.Keys)
                {
                    Profile prof = mProfiles[Name];

                    System.Console.WriteLine(Name + prof.Categorized + " " + prof.ElapsedSeconds);
                    // display indented each non-categorized element under the catch-all "Non-Categorized" header
                    if (!(prof.ElapsedSeconds == 0) && !prof.Categorized)
                    {
                        double Ratio = prof.ElapsedSeconds / mTotalElapsedTime;
                        string Format = "{0} = {1:P} ({2:0.00} seconds)";
                        if (Verbose)
                            Format = " " + Format;

                        string displayText = string.Format(Format, Name, Ratio, prof.ElapsedSeconds);
                        //AddText(displayText, offsetX, offsetY + additionalYOffset, RGBA(Ratio, 1 - Ratio, 0, 0.8f));

                        totalProfiledTime += prof.ElapsedSeconds;
                        NonCategorisedTime += prof.ElapsedSeconds;

                        System.Console.WriteLine(displayText);

                        //additionalYOffset += 14;
                    }
                }
            }
        }

        public delegate void TextOutputHandler(string text, int offsetX, int offsetY, int color);

        private List<DebugText> mDebugText;
        private struct DebugText
        {
            public string Text;
            public int OffsetX;
            public int OffsetY;
            public int Color;
        }

        private void AddText(string text, int offsetX, int offsetY, int color)
        {
            DebugText item;
            item.Text = text;
            item.OffsetX = offsetX;
            item.OffsetY = offsetY;
            item.Color = color;

            mDebugText.Add(item);
        }

		public static string GetTimeString()
        {
            // NOTE: When running on Online Compiler, this time will be the time of the SERVER 
            //       running the online compiler and NOT your local PC.
            return DateTime.Now.ToString();
        }

        /// <summary>
        /// Releases the hook on a profile
        /// </summary>
        /// <param name="Name">The name of the profile</param>
        /// <remarks>
        /// This method is only necessary when a profile has been hooked up without Using
        /// </remarks>
        public void Release(string Name)
        {
            if (ProfilerEnabled)
            {
                throw new NotImplementedException("Because of problem with nested profile calls, we moved timer functions to Hook and out of Profile.  We should always use using{} so that Hook can be used and so nested call timing is accurate.");
                // mProfiles[Name].StopTimer();
            }
        }
    }
	    
}
