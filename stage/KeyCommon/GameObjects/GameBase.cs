using System;
using Lidgren.Network;
using KeyCommon;
using System.Collections.Generic;
using KeyCommon.Commands;

namespace KeyCommon.GameObjects
{
    public struct GameConfigParameter
    {
        public GameConfigParameterType ID;
        public object[] Args;

        public void Read(Lidgren.Network.NetBuffer buffer)
        {
            throw new NotImplementedException();
        }

        public void Write(Lidgren.Network.NetBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }

    public struct GameTurnSchedule
    {
        public GameScheduleType Schedule;
        public bool UseList;
        // if true, does not use Interval but rather the list
        public int Interval;


        public DateTime[] Times;
        //TODO: what to do if the server runs, and the turn takes longer to compute than the interval between turns?
        //public int Duration;
        //can either be a count(e.g. 10 turns a day running on an every 15 minute shcedule..)
        //Public Occurance As Occurance ' e.g. the function of occurance depends on the selected Schedule.
        // for instance, if minutely, then our occurance could  every day, every week, every other day, M,W,F, T

        public void Read(NetBuffer buffer)
        {

            try
            {
                Schedule = (GameScheduleType)buffer.ReadInt32();
                UseList = buffer.ReadBoolean();
                Interval = buffer.ReadInt32();
                int count = buffer.ReadInt32();
                if ((count > 0))
                {
                //ERROR: Not supported in C#: ReDimStatement

                    for (int i = 0; i < count; i++)
                    {
                        Times[i] = new DateTime(buffer.ReadInt64());
                    }
                }

               // Duration = buffer.ReadInt32();
            }
            catch
            {

            }
        //TODO: malformed packet could not be read.
        }

        public void Write(NetBuffer buffer)
        {
            if ((buffer == null)) return;

            try
            {
                buffer.Write((byte)Schedule);
                buffer.Write(UseList);
                buffer.Write(Interval);
                int count = 0;
                if ((Times != null && Times.Length > 0))
                {
                    count = Times.Length;
                }
                buffer.Write(count);
               // buffer.Write(Duration);
            }
            catch
            {

            //TODO: buffer write error
                return;
            }
            return;
        }
    }


    //a "table" and a "game" are now synonymous however
    // a table can be canceled at the registering stage in the lobby
    // WAIT:  Maybe not...maybe Game is Game and GameRegistration is what we called a "Table" before

    // match, campaign
    // registrationRequired?
    // status = registering, playing, ended
    // password
     
    // - two types of games, persistant games that only show up in the list if
    //   registration = false(no registration required, can join as long as open player slots)
    // - non persistant games (matches)

    public class GameBase : GameObject, Keystone.Simulation.IGame
    {
        protected int mSeed;
        //TODO: need to modify "Host"
        // to explicitly be a per game object where each game on a multiple game server, uses a seperate Host since
        // authentication requires different accounts and we'd use different listening ports whcih means we'd use an array
        // of Lidgren.Network.NetServer[] mNetServer.
        // but for now this is not too important since for alpha we just have one game per server
        protected Host mHost;
        protected long mUserID;
        protected bool mRegistered;
        protected string mName;     // friendly name of this game
        protected string mPassword; // password to join the game, not the account password
        protected string mMap;      // scene folder path or name?
        protected IGame[] mGames;   // games within the game such as minigames
        
        protected KeyCommon.Data.ComponentStoreCollection mComponentStoreCollection;
        protected KeyCommon.Processors.DataProcessors mDataProcessors;
        

        public GameBase(int id) : base (id)
        {
            mTick = 0;
            mSeed = seed;
            
            // TODO: i think id here represents a db record index not the typeID of GameServerInfo
            mDataProcessors = new KeyCommon.Processors.DataProcessors();
            
            mComponentStoreCollection = new KeyCommon.Data.ComponentStoreCollection();
        }

        public GameBase(string name) : this((int)KeyCommon.Messages.Enumerations.GameServerInfo)
        {
            mName = name;
        }

        public GameBase(Host host) : this((int)KeyCommon.Messages.Enumerations.GameServerInfo)
        {
            if (host == null) throw new ArgumentNullException();
            mHost = host;
        }

        public GameSummary GetSummary()
        {
            GameSummary sum = new GameSummary();
            sum.Name = mName; // so a lookup can be done by the lobby to find the correct server.  if we pass a PrimaryKey id instead we can more directly query the dictionary of games
            sum.ServerName = mHost.Name;
            sum.PasswordProtected = !string.IsNullOrEmpty(mPassword);
            sum.Map = Map;
            sum.ListenTable = mHost.ListenTable;
            return sum;
        }

#region IGame implementation
        public Host Host { get { return mHost; } }
        public long UserID { get { throw new NotImplementedException(); } }
        public int Seed {get{return mSeed;}}
        public bool Registered {get {return mRegistered;}}
        public string Name {get{return mName;}}     // the name of this game instance that will appear on a MasterServer browser
        public string Password {get {return mPassword;}} // password to join the game, not the account password
        public string Map {get{return mMapName;}}
        public IGame[] GamesWithinTheGame {get {return mGames;} } // minimgames potentially

        public ComponentStoreCollection ComponentsStoreCollection {get {return mComponentStoreCollection;}}
        public KeyCommon.Processors.DataProcessors DataProcessors {get{return mDataProcessors;}}


        public virtual void UserMessageReceived(Lidgren.Network.NetConnectionBase connection, Lidgren.Network.NetBuffer buffer)
        {
            KeyCommon.DatabaseEntities.Player player = (KeyCommon.DatabaseEntities.Player)connection.Tag;

            // if this is a server, the connection will be from the server... so we may very well ignore it...
            // but if this simulation is running on the server, each player will be important.
            // Probably best to make this abstract and have yet again... ClientSimulation, ServerSimulation


        }
        
        public virtual void Update(Keystone.Simulation.GameTime gameTime)
        {
            / this class probably needs to reside in Core.cs where it gets
            // called via Simulation.GameRulesProcessor.Update(); which occurs
            // after Simulation.IntrinsicRulesProcessor.Update()
            //
            // TODO: can be done in parallel?
            // API needs call to add DataProcessor instances to this class
            for (int i = 0; i < mDataProcessors.Count; i++)
            {
                
            }
            
        }
        
        //// temporary concepts
       // public void Tick(long elapsed)
       // {
       //     int startTime =

       //         //update each AI entity for the provided time slice(this is to prevent us from doing nothing but AI updates for some undetermined interval

       //        // do AI stuff

       //       Do while elapsed < mAITimeSlice

       //             entity = mAI.GetNext()
       //            entity.Update()
       //            elapsed = Environment.TickCount() - startTime

       //         loop

       //      update positions of all entities

       //      do other stuff required in the simulation

       //     mGame.mTurn += 1;
       //     mSimulationTick = GetPerformanceCounter - iStartTime  ' how long it took to update the simulation.  We can even take averages over time.  
       // }

       // //   this will be based on how long each SimulationTick Requires to run
       // private void CalcAITimeSlice(ByVal iPercentage As Integer)
       // {
       //     mAITimeSlice = 50;
       // }

#endregion

        // similar to what we do for Plugins and when serializing / deserializing CustomProperties
        private static ushort UserTypeIDFromTypename(string typename)
        {
            switch (typename)
            {
                case "SensorContact[]":
                    return TYPE_SENSOR_CONTACT_ARRAY;
   
                case "NavPoint[]":
                case "Path[]":
                case "UserData":
                case "UserData[]":
                case "Waypoint[]":

                default:
                    return 0;
            }
        }

        private static string UserTypenameFromTypeID(ushort typeID)
        {
            switch (typeID)
            {
                case TYPE_SENSOR_CONTACT_ARRAY: return "SensorContact[]";
                default:
                    return null;
            }
        }

        private static object UserTypeReader(Lidgren.Network.NetBuffer buffer, ushort typeID, out string typeName)
        {
            switch (typeID)
            {
                case TYPE_SENSOR_CONTACT_ARRAY:
                    typeName = UserTypenameFromTypeID(TYPE_SENSOR_CONTACT_ARRAY);
                    int count = buffer.ReadInt32();
                    SensorContact[] contacts = new SensorContact[count];
                    for (int i = 0; i < count; i++)
                    {
                        contacts[i].ContactID = buffer.ReadString();
                        contacts[i].Position.x = buffer.ReadDouble();
                        contacts[i].Position.y = buffer.ReadDouble();
                        contacts[i].Position.z = buffer.ReadDouble();
                        contacts[i].Velocity.x = buffer.ReadDouble();
                        contacts[i].Velocity.y = buffer.ReadDouble();
                        contacts[i].Velocity.z = buffer.ReadDouble();
                        contacts[i].IFF = buffer.ReadInt32();            // identify friend or foe. This could be an Enum FRIEND, FOE, UNKNOWN
                        contacts[i].IsTarget = buffer.ReadBoolean ();
                        contacts[i].IsGhost = buffer.ReadBoolean();       // occurs when the contact moves out of sensor range, or has used evasive maneuvers sufficiently, or has cloaked
                        contacts[i].Age = buffer.ReadInt32 ();
                        contacts[i].GhostAge = buffer.ReadInt32();
                        contacts[i].Priority = buffer.ReadInt32();
                        contacts[i].ThreatLevel = buffer.ReadInt32(); // todo: this should probably be a float
                    }
                    return contacts;
                    break;
                default:
                    typeName = null;
                    return null;
            }
        }

        private static bool UserTypeWriter(Lidgren.Network.NetBuffer buffer, object value, string typeName)
        {
            switch (typeName)
            {
                case "SesnsorContact":
                    break;
                case "SensorContact[]":
                    SensorContact[] contacts = (SensorContact[])value;
                    if (contacts == null || contacts.Length == 0) break;
                    buffer.Write(contacts.Length);
                    for (int i = 0; i < contacts.Length; i++)
                    {
                        buffer.Write(contacts[i].ContactID);
                        buffer.Write(contacts[i].Position.x);
                        buffer.Write(contacts[i].Position.y);
                        buffer.Write(contacts[i].Position.z);
                        buffer.Write(contacts[i].Velocity.x);
                        buffer.Write(contacts[i].Velocity.y);
                        buffer.Write(contacts[i].Velocity.z);
                        buffer.Write(contacts[i].IFF);            // identify friend or foe. This could be an Enum FRIEND, FOE, UNKNOWN
                        buffer.Write(contacts[i].IsTarget);
                        buffer.Write(contacts[i].IsGhost);       // occurs when the contact moves out of sensor range, or has used evasive maneuvers sufficiently, or has cloaked
                        buffer.Write(contacts[i].Age);
                        buffer.Write(contacts[i].GhostAge);
                        buffer.Write(contacts[i].Priority);
                        buffer.Write(contacts[i].ThreatLevel); // todo: this should probably be a float
                    }
                    break;
                default:
                    return false;
                    //break;
            }

            return true;
        }


        private static object MergeArrayElementsForUserTypes(string typeName, object currentValue, object value)
        {
            object result = null;

            ushort typeID = UserTypeIDFromTypename(typeName);
            switch (typeID)
            {

                case TYPE_SENSOR_CONTACT_ARRAY: //we don't want duplicates
                    if (currentValue == null) return value;

                    SensorContact[] existing = (SensorContact[])currentValue;
                    SensorContact[] newValues = (SensorContact[])value;

                    if (newValues == null) return existing;
                    bool found = false;

                    for (int i = 0; i < newValues.Length; i++)
                    {
                        for (int j = 0; j < existing.Length; j++)
                        {
                            if (newValues[i].ContactID == existing[j].ContactID)
                            {
                                //update the existing with newValue contact data
                                existing[j] = newValues[i];

                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            existing = Keystone.Extensions.ArrayExtensions.ArrayAppend(existing, newValues[i]);
                        found = false;
                    }

                    return existing;

                    // NOTE: we dont want a ArrayUnion because we don't want duplicates. We want to update the existing element with the new element with same contactID
                   // result = Keystone.Extensions.ArrayExtensions.ArrayUnion((string[])currentValue, (string[])value);
                    break;

                
                default:
                    throw new NotImplementedException("MergeArrayElementsForUserTypes() - ERROR: unsupported type '" + typeName + "'");
                    break;
            }

            return result;
        }


        public NetChannel Channel
        {
            get { return NetChannel.ReliableUnordered; }
        }


        public override void Read(NetBuffer buffer)
        {
            mID = buffer.ReadInt64();
            mName = buffer.ReadString();

            mHost = new Host();
            //mHost.Read(buffer);


            Map = buffer.ReadString();
            mPassword = buffer.ReadString();  // not the authentication password but a local password so only invited players can play this game

            
        }

        public override void Write(NetBuffer buffer)
        {
            buffer.Write(mID);
            buffer.Write(mName);

           // mHost.Write(buffer);

            buffer.Write(Map);
            buffer.Write(mPassword);
            
        }
    }

    //public class OldGame : GameObject
    //{

    //    public string mName;
    //    public string mServerName;  // if an end user is hosting this game this is their user's name, else it's the name of one our own game servers' account

    //    // not a typeo, this is in fact supposed to be a Long
    //    public string mPassword;
    //    public string mVersion;
    //    public DateTime mStart;
    //    public DateTime mEnd;


    //    public Host mHost;

    //    public GameType mType;
    //    public GameConfigParameter[] mParameters;  // can include time_step and other params? client and server need to run at same frequency
    //    // shouldn't we want to persist this?
    //    public GameTurnSchedule mTurnSchedule;
    //    public GameStatus mStatus;
    //    public GameResolution mResolution;




    //    public OldGame()
    //    {

    //    }


    //    public OldGame(Table table, string password)
    //    {
    //        mPassword = password;

    //        // we use -1 when creating a game from a table since we dont want the Table's id, the table's id is only relevant in the context of the Lobby.  We 
    //        // instead get the game ID from the database itself since game ID's use a SERIAL value so that every single game ever created is unique
    //        // this way we can always reference the game a user was in for admin purposes and log tracking since every game is unique.
    //        // Anyway, using a -1 will instruct the SQLContext.Store() to use the DEFAULT keyword for this type

    //        mName = table.Name;

    //        //mHost.IP = 
    //        //mHost.Port = 
    //        //mHost.UpTime <-- this field should be changed to just .StartTime  for when it connect to the Lobby 
    //        // mHost.UsesNat 
    //        // mType As GameType
    //        // mVersion As String  <-- TODO: this should be moved to the Host yes?  it's the Host's exe version
    //        // 

    //        mStart = DateTime.Now;
    //        mStatus = GameStatus.Registering;
    //        mResolution = GameResolution.Unresolved;

    //        mParameters = table.Settings.ToArray();

    //    }

    //    public void GetSummary()
    //    {

    //    }


    //    public int ID
    //    {
    //        get { return (int)Enumerations.Game; } // TODO: no, not creategame, but should just be a "game" entity 
    //    }

    //    public NetChannel Channel
    //    {
    //        get { return NetChannel.ReliableUnordered; }
    //    }

    //    public override void Read(NetBuffer buffer)
    //    {
    //        mPrimaryKey = buffer.ReadInt64();
    //        mName = buffer.ReadString();
    //        mServerName = buffer.ReadString();
    //        mPassword = buffer.ReadString();
    //        mType = (GameType)buffer.ReadInt32();
    //        mVersion = buffer.ReadString();
    //        mHost = new Host();
    //        mHost.Read(buffer);
    //        // mParameters ' first mParameters.Count
    //        // mTurnSchedule
    //        mStatus = (GameStatus)buffer.ReadByte();
    //        mResolution = (GameResolution)buffer.ReadByte();

    //        mStart = new DateTime(buffer.ReadInt64());

    //        mEnd = new DateTime(buffer.ReadInt64());
    //    }

    //    public override void Write(NetBuffer buffer)
    //    {
    //        buffer.Write(mPrimaryKey);
    //        buffer.Write(mName);
    //        buffer.Write(mServerName);
    //        buffer.Write(mPassword);
    //        buffer.Write((byte)mType);
    //        buffer.Write(mVersion);
    //        mHost.Write(buffer);
    //        // buffer.Write parametersCount
    //        // for each param parameter.Write(buffer)
    //        // mTurnSchedule.Write(buffer
    //        buffer.Write((byte)mStatus);
    //        buffer.Write((byte)mResolution);

    //        buffer.Write(mStart.Ticks);


    //        buffer.Write(mEnd.Ticks);
    //    }
    //}
}