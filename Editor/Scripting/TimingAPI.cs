using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;
using Keystone.CSG;

namespace KeyEdit.Scripting
{
	/// <summary>
	/// Description of TimingAPI.
	/// </summary>
	public class TiminmgAPI : ITimingAPI
	{
		public double GetElapsedSeconds(string sceneID)
		{
			Keystone.Scene.Scene scene = AppMain._core.SceneManager.GetScene (sceneID);
			return scene.Simulation.GameTime.ElapsedSeconds;
		}
		
		public double GetTotalElapsedSeconds(string sceneID)
		{
			Keystone.Scene.Scene scene = AppMain._core.SceneManager.GetScene (sceneID);
			return scene.Simulation.GameTime.TotalElapsedSeconds;	
		}
		
		 public double GetJulianDay (string sceneID)
		 {
 			Keystone.Scene.Scene scene = AppMain._core.SceneManager.GetScene (sceneID);
			return scene.Simulation.GameTime.JulianDay;	
		 }
		 
		 /// <summary>
         /// Equivalent to gameSecondsPerRealLifeSecond.  
         /// eg. 60 gameSeconds per real life second means 
         /// every real life minute results in one hour of game time passing
         /// </summary>
 		 public double GetTimeScaling (string sceneID)
		 {
 			Keystone.Scene.Scene scene = AppMain._core.SceneManager.GetScene (sceneID);
			return scene.Simulation.GameTime.Scale;	
		 }
		 

		// NOTE All Intgerval timers registered are TICKed/Updated each frame from within AppMain.mGameTime
		//      ... those will exist AppMain.mGameTime.IntervalTimers in code Keystone.Timers.IntervalTimers.cs
		public void Interval_Register(string nodeID, string name, float intervalInSeconds, bool activateImmediately = true, bool repeating = false)
		{
			AppMain.mGameTime.IntervalTimers.Register (nodeID, name, intervalInSeconds, activateImmediately, repeating, 0);
		}

		public void Interval_Reset (string nodeID, string name)
		{
			AppMain.mGameTime.IntervalTimers.Reset (nodeID, name);
		}

        public void Interval_UnRegister(string nodeID, string name)
		{
			AppMain.mGameTime.IntervalTimers.UnRegister (nodeID, name);
		}

		public bool Interval_IsReady(string nodeID, string name)
		{
			return AppMain.mGameTime.IntervalTimers.IsReady (nodeID, name);
		}

		public bool Interval_IsActive(string nodeID, string name)
		{
			return AppMain.mGameTime.IntervalTimers.IsActive (nodeID, name);
		}
        #endregion
    }
}