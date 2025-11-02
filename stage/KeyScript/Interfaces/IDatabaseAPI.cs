using System;


namespace KeyScript.Interfaces
{
    public interface IDatabaseAPI
    {
        void CreateWaypointRecord(string entityID, Game01.GameObjects.NavPoint navpoint);
        Game01.GameObjects.NavPoint[] GetWaypointRecords(string entityID);

    }
}
