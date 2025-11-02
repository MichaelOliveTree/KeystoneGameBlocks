using System;
using System.Collections.Generic;
using KeyCommon.DatabaseEntities;

namespace Game01.GameObjects
{
    public enum TableStatus
    {
        Created,
        Closed,
        ParameterChanged
    }

    public class Table : GameObject
    {
        public string Name;
        private List<string> mUsers;
        public List<GameConfigParameter> Settings;
        private Dictionary<string, bool> mReadyStatus;

        public Table(int type) : base (type)
        {
            Settings = new List<GameConfigParameter>();
            mReadyStatus = new Dictionary<string, bool>();
            mUsers = new List<string>();
        }

        public void AddUser(string name)
        {
            if (mUsers == null) mUsers = new List<string>();

            System.Diagnostics.Debug.Assert(!mUsers.Contains(name));

            if (!mUsers.Contains(name))
                mUsers.Add(name);
        }

        public string[] UserIDs
        {
            get
            {
                string[] results = new string[mUsers.Count];
                mUsers.CopyTo(results, 0);
                return results;
            }
        }
        public void RemoveUser(string name)
        {
            System.Diagnostics.Debug.Assert(mUsers.Contains(name));
            mUsers.Remove(name);
            mReadyStatus.Remove(name);
        }


        public bool IsFull
        {
            get { return false; } //TODO: check usercount against me.Settings["max_users"]
        }
       

        /// <summary>
        /// Returns true if all users at the table are ready
        /// </summary>
        public bool IsReady
        {
            get
            {
                foreach (bool val in mReadyStatus.Values)
                    if (!val) return false;

                return true;
            }
        }

        public void SetUserReadyStatus(string name, bool isReady)
        {
            mReadyStatus[name] = isReady;
        }

        public bool ContainsUser(string name)
        {
            return mUsers.Contains(name);
        }

        #region IRemotableType members
        public virtual void Read(Lidgren.Network.NetBuffer buffer)
        {
            base.Read(buffer);
            //mID = buffer.ReadInt64();
        }

        public virtual void Write(Lidgren.Network.NetBuffer buffer)
        {
            base.Write(buffer);
            //buffer.Write(mID);
        }
        #endregion
    }
}
