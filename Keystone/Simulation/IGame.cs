using Game01.GameObjects;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using Game01.MemoryStore;

namespace Keystone.Simulation
{
    public interface IGame
    {
        public int Seed {get;}
        // TODO: need to modify "Host"
        // to explicitly be a per game object where each game on a multiple game server, uses a seperate Host since
        // authentication requires different accounts and we'd use different listening ports whcih means we'd use an array
        // of Lidgren.Network.NetServer[] mNetServer.
        // but for now this is not too important since for alpha we just have one game per server
        public Host Host { get ; }
        public long UserID { get ; }
        public bool Registered {get;}
        public string Name {get;}     // the name of this game instance that will appear on a MasterServer browser
        public string Password {get;} // password to join the game, not the account password
        public string Map {get;}

        public ComponentStoreCollection ComponentsStoreCollection {get;}
        public KeyCommon.Processors.DataProcessors DataProcessors {get;}
        
        public IGame[] GamesWithinTheGame {get;} // minimgames potentially
        
        public GameSummary GetSummary();
        
        public void UserMessageReceived(Lidgren.Network.NetConnectionBase connection, Lidgren.Network.NetBuffer buffer);

        public void Update(Keystone.Simulation.GameTime gameTime);


        public NetChannel Channel {get;}
        public void Read(NetBuffer buffer);
        public void Write(NetBuffer buffer);

    }
}