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
		 
		 public void Interval_Reset (string nodeID, string name)
		 {
		     
		 }
        #endregion
    }
}