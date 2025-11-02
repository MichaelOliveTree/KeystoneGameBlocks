using System;
using Keystone.Entities;
using KeyCommon.DatabaseEntities;

namespace Keystone.Simulation
{
    public interface ISimulation : IDisposable
    {
        Scene.Scene Scene { get; set; }
        GameTime GameTime { get; }
        IGame Game { get; }

        DataProcessors IntrinsicProcessors { get;}
        DataProcessors RulesProcessors { get;}


        /// <summary>
        /// Simulation can only have one current mission at a time.
        /// </summary>
        Missions.Mission CurrentMission {get; set;}

        uint PhysicsHertzInTimesPerSecond {get ; set;}
        bool Running { get; set; }
        bool Paused { get; set; }
        bool CollisionEnabled { get; set; }

        void LoadMission(string sceneName, string missionName);
        void EnableMission(bool enable);
        double Update(Keystone.Simulation.GameTime gameTime);
        //PlayerCharacter CurrentTarget { get; set; }

        void RegisterPhysicsObject(Entity entity);
        void UnRegisterPhysicsObject(Entity entity);

        void UnRegisterProducer(uint productID, Entity entity);
        void RegisterProducer(uint productID, Entity entity);

		
        void AddPlayer(Player p);
        void RemovePlayer(Player p);

        void UserMessageReceived(Lidgren.Network.NetConnectionBase connection, Lidgren.Network.NetBuffer buffer);
        double Update(Keystone.Simulation.GameTime gameTime);

    }
}