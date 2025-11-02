using System;
using Keystone.Types;
using Settings;

namespace Game01.GameObjects
{
    public struct PathPoint
    {
        public Vector3d Position;
        // Radius allows us to control how close an agent needs to be to the Position before it registers as having arrived at this PathPoint.
        // TODO: why not just a generic Vector3d array and then define the radius in the AIBlackboardData?
        public float Radius;
    }

    // TODO: i dont think we need a struct for this.  Let's just store the individual members in our Entity.CustomData accessible to the scripts via GetAIBlackboardData
    public class Path //: KeyCommon.DatabaseEntities.GameObject - we dont need to save every path and pathpoints for every update, for every crew member on board, for each ship, right? we don't need that level of granularity in our record storing - even though we do want to store all "orders" and "tasks" and the start/stop times of those tasks
    {
        public string RegionID; // eg. Zone, Region, Interior
        public int CreationTime; // TODO: these don't really need a struct.  They can just be stored in the AIBlackboardData
        public int CurrentIndex;
        public PathPoint[] Points; // don't need to store Tile indices or floor levels do we?
    }

    // TODO: the problem with GameObjects is they should be stored in CustomProperties or the AIBlackboard, but then, they dont really need a struct... just individual members stored in the AIBlackBoard Entity.CustomData

    // TODO: a NavPoint is a GameObject, but it's not an Order or OrderRequest. 
    //       It's like Helm which is a GameObject used to maneuver our ship.
    //       how to modify this?
    public class NavPoint : KeyCommon.DatabaseEntities.GameObject
    {
        public int RowID;       // index of this NavPoint in the Entity's custom property NavPoint[] array.
        public string RegionID; // Region of Target Entity or Position (what if the target leaves the system?)
        public string TargetID; // can be either a celestial body or another vessel
        public Vector3d OrbitalRadius;
        public float OrbitalInclination;
        public float OrbitalEccentricity;
        public float WarpFator; // todo: for sublight speeds a seperate G's or Velocity variable may be needed.  Acceleration and deceleration will look to that variable to determine what to do.
        public Vector3d Position;
        // I don't remember why i had Path here as a sub-array.  Might just be better to have array of NavPoint's all with same info
        // in terms of RegionID
        //public Vector3d[] Path;  // Path[] is not serialized.  It is computed at runtime if it does not exist.
        //public int PathCreationTime; // so we can re-calc if it's too old for dynamic pathing
        
        public NavPoint(long id) : base(id)
        {
        }

        public NavPoint(string ownerID, string regionID, Vector3d position)
            : base(ownerID)
        {
            RegionID = regionID;
            Position = position;
        }

        #region GameObject methods // TODO: GetProperties and SetProperties isn't needed here because we only need to serialize over the wire for loopback or client/server.  Instead, we just need to inherit IRemotableType from Game01.GameObjects.GameObject
        //public override PropertySpec[] GetProperties(bool specOnly)
        //{
        //    Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
        //    Settings.PropertySpec[] properties = new Settings.PropertySpec[2 + tmp.Length];
        //    tmp.CopyTo(properties, 2);

        //    properties[0] = new Settings.PropertySpec("region", typeof(string).Name);
        //    properties[1] = new Settings.PropertySpec("position", typeof(Vector3d).Name);

        //    if (!specOnly)
        //    {
        //        properties[0].DefaultValue = RegionID;
        //        properties[1].DefaultValue = Position;
        //    }

        //    return properties;
        //}

        //public override void SetProperties(PropertySpec[] properties)
        //{
        //    if (properties == null) return;
        //    base.SetProperties(properties);

        //    for (int i = 0; i < properties.Length; i++)
        //    {
        //        // use of a switch allows us to pass in all or a few of the propspecs depending
        //        // on whether we're loading from xml or changing a single property via server directive
        //        switch (properties[i].Name)
        //        {
        //            case "region":
        //                RegionID = (string)properties[i].DefaultValue;
        //                break;
        //            case "position":
        //                Position = (Vector3d)properties[i].DefaultValue;
        //                break;
        //        }
        //    }
        //}
        #endregion

    }
}
