using System;
using KeyScript.Interfaces;
using System.Collections.Generic;


namespace KeyEdit.Scripting
{
    public class DatabaseAPI : IDatabaseAPI
    {
        public void CreateWaypointRecord(string entityID, Game01.GameObjects.NavPoint navpoint)
        {
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);
            KeyEdit.Database.AppDatabaseHelper.CreateWaypointRecord(entity, navpoint);
        }

        public Game01.GameObjects.NavPoint[] GetWaypointRecords(string entityID)
        {
            return KeyEdit.Database.AppDatabaseHelper.GetNavPoints(entityID);
        }
    }
}
