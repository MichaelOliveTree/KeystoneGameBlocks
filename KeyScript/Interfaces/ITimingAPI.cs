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

    }
}