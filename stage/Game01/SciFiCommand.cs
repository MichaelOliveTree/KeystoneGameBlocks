using Game01.GameObjects;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using Game01.MemoryStore;

namespace Game01
{
    
    // NOTE: Here we inherit GameBase which implements Keystone.Simulation.IGame, but maybe IGame too will just reside in KeyCommon.GameObjects
    public class Game : KeyCommon.GameObjects.GameBase
    {
        
        public Game(int seed) : base(seed)
        {
        }

        // GOOGLE - HASH (GUID) to an int or long?
        
        // RPGs are Comprehensive Abstract Rules-Resolving Systems That Govern Interactions
        //   character<-> character interactions
        //   character<-> world interactions (eg perception checks)
        //   character<-> items<->character-
        //   character<-> items<->world
        
        
        // TODO: This Update() shouldn't be needed.  KeyEdit.Simulation.Update() will
        //       grab the game.DataProcessors(see KeyCommon.GameObjects.GameBase from
        //       which this class derives.) as well as the game.DataProcessors and perform
        //       the loops to process user functions for game rules.
        // 
        public override void Update(Keystone.Simulation.GameTime gameTime)
        {
            
        }
    }
}