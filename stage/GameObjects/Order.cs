using System;
using KeyCommon.DatabaseEntities;
using Lidgren.Network;
using Settings;

namespace Game01.GameObjects
{
    public enum TaskType : int
    {
        NONE = 0,
        HELM_ENABLE_MAIN_ENGINES,
        HELM_CHANGE_HEADING,
        HELM_PROCEED_TO_NEXT_WAYPOINT,
        HELM_FULL_STOP
    }

    public enum TaskStatus : int
    {
        CREATED = 0,
        CANCELED,
        FAULTED,
        EXECUTING,
        WAITING_TO_ACTIVATE,
        WAITING_TO_EXECUTE,
        WAITING_FOR_CHILDREN_TO_COMPLETE,
        COMPLETED
    }

    /// <summary>
    /// Orders are a type of GameObject which can be serialized to a database
    /// and kept as a complete history of commands the user has given
    /// to his crew.
    /// </summary>
    public class Order : GameObject
    {
        // TODO: all these properties can be made public
        //       our scripts can access them directly because Game01.dll is 
        //       shared by EXE and scripts only.  Keystone.dll does not need to know
        //       about GameObjects.  Keystone.dll is our game engine/framework, but
        //       GameObject derived types are apart of the "game."
        public long OrderID;
        public string AssignedByID;
        public string AssignedStationID;
        public string StationOperatorID;  // (what if operator changes during long task? maybe only for tasks submitted to the ship's computer (and only so many tasks can be assigned to the main computer)),
        public int Task;
        public string Args;               // comma seperated values

        public DateTime AssignedDateTime;
        public DateTime ExecutionDateTime;
        public DateTime CompletionDateTime;
        public int Priority;
        public int Status;                // can be cancelled
        public int Resolution;

        // (notes can be added by crew member who for instance opposes the task but obeys the order anyways)
        // - how can orders per station be serialized one after the other rather than simultaneously?
        // - for instance, some helm maneuvers would use multiple axial thrusters at same time.but maneuvers are not orders...hrm...
        // helm holds state and maneuvers are executable instructions.
        public string Notes1;
        public string Notes2;


        public Order(int type) : base(type)
        { }

        //public Order(string ownerID) : base(ownerID)
        //{ }

        // public virtual void Execute()
        // {}


        #region IRemotableType Members
        public override void Read(NetBuffer buffer)
        {
            base.Read(buffer);

            AssignedDateTime = DateTime.FromBinary(buffer.ReadInt64());
            AssignedDateTime = DateTime.SpecifyKind(AssignedDateTime, DateTimeKind.Utc);
            AssignedByID = buffer.ReadString();
            AssignedStationID = buffer.ReadString();
            Priority = buffer.ReadInt32();
            Task = buffer.ReadInt32();
            Args = buffer.ReadString();
            Notes1 = buffer.ReadString();
        }

        public override void Write(NetBuffer buffer)
        {
            base.Write(buffer);

            buffer.Write(AssignedDateTime.ToBinary());
            buffer.Write(AssignedByID);
            buffer.Write(AssignedStationID);
            buffer.Write(Priority);
            buffer.Write(Task);
            buffer.Write(Args);
            buffer.Write(Notes1);
        }
        #endregion
    }
}
