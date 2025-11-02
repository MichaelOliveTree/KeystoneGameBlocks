using System;
using Lidgren.Network;

namespace Game01.GameObjects
{
    public abstract class GameObject : IRemotableType
    {
        protected int mType;
        // todo: GameObject's should be scriptable


        protected GameObject(int type)
        {
            mType = type;
        }

        #region IRemotableType members
        public NetChannel Channel
        {
            get
            {
                return NetChannel.ReliableInOrder1;
            }
        }

        public int Type
        {
            get
            {
                return mType;
            }
        }

        public virtual void Read(NetBuffer buffer)
        {
            mType = buffer.ReadInt32();
        }

        public virtual void Write(NetBuffer buffer)
        {
            buffer.Write(mType);
        }
        #endregion 
    }
}
