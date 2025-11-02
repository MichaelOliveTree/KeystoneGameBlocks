using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace Game01.GameObjects
{
    public class TacticalState : GameObject 
    {
        SensorContact[] mContacts;

        public TacticalState(string ownerID, int type) : base(type)
        {

        }

        public SensorContact[] Contacts  
        {
            get { return mContacts; }
            set { mContacts = value; }
        } 

        public override void Read(NetBuffer buffer)
        {
            base.Read(buffer);
        }

        public override void Write(NetBuffer buffer)
        {
            base.Write(buffer);
        }
    }
}
