using System;
using Keystone.Extensions;
using Keystone.Types;
using KeyCommon.DatabaseEntities;

namespace Game01.GameObjects
{
    public struct EmissionValue
    {
        public float Strength;
        public int Time; // local game elapsed time, filled by api function
        public Vector3d Position;
    }

    // obsolete? not sure yet though ive combined direction and amount as single vector3d in engine.css
    public struct ThrusterValue
    {
        public ThrusterDirection Direction; // will apply to either translation or rotation
        public double Amount;
    }

    public enum ThrusterDirection
    {
        TranslatePositiveX,
        TranslateNegativeX,
        TranslatePositiveY,
        TranslateNegativeY,
        TranslatePositiveZ,
        TranslateNegativeZ,
        RotatePositiveX,
        RotateNegativeX,
        RotatePositiveY,
        RotateNegativeY,
        RotatePositiveZ,
        RotateNegativeZ
    }

    // todo: this should perhaps just be a struct and not a GameObject
    // we should not be updating SensorContacts every frame.  It's too much.  We need to be able to update various stations (eg tactical station) at fixed frequencies like 1hz but still interpolate contact positions based on last position and velocity
    public struct SensorContact 
    {
        // todo: if we do want to plot movement, we could just do it in the relevant HUDs.
        //public struct Plot
        //{
        //    public int Time;         // the time each position echo was received
        //    public Vector3d Position; // position echo (acceleration and heading can be determined from the history)
        //    public string[] Sensors; // the sensors that have detected this contact.  ships can have many sensors and of various types, but we only track one contact instance right?
        //}
        //public Plot[] History;

        public string ContactID;   // what about an enum for ContactType {HeavyCruiser, Frigate, Freighter, Unknown, etc}
        public Vector3d Position;
        public Vector3d Velocity;
        public int IFF;            // identify friend or foe. This could be an Enum FRIEND, FOE, UNKNOWN
        public bool IsTarget;
        public bool IsGhost;       // occurs when the contact moves out of sensor range, or has used evasive maneuvers sufficiently, or has cloaked
        public double Age;
        public double GhostAge;
        public int Priority;
        // https://goblinshenchman.wordpress.com/
        // https://goblinshenchman.wordpress.com/2019/07/06/monster-mark-off-the-mark-quantifying-monster-threat-levels/
        public int ThreatLevel; // this is basically like an EotL threat level rating based on it's firepower and defenses.  This value is alway relative to the player's ship's level

        //public void AddPlot(int time, Vector3d position)
        //{
        //    //Plot p;
        //    //p.Time = time;
        //    //p.Position = position;
        //    //p.Sensors = null;

        //    //History = History.ArrayAppend(p);
        //    Position = position;
        //}

        public override bool Equals(object obj)
        {
            if (!(obj is SensorContact)) return false;

            if (this.ContactID == ((SensorContact)obj).ContactID) return true;
            return false;
        }
    }
}
