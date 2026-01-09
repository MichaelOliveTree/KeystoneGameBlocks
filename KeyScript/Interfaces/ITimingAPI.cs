using System;
using Keystone.Types;


namespace KeyScript.Interfaces
{
    public interface ITimingAPI
    {
        
        

        double GetElapsedSeconds(string sceneID);
        double GetTotalElapsedSeconds(string sceneID);

        double GetJulianDay (string sceneID);
        double GetTimeScaling (string sceneID);


        void Interval_Register(string entityID, string name, float intervalInSeconds, bool activateImmediately = true, bool repeating = false); 
		void Interval_Reset (string nodeID, string name);
        void Interval_UnRegister(string nodeID, string name);
		bool Interval_IsReady(string nodeID, string name);
        bool Interval_IsActive(string nodeID, string name);



    }
}