using System;
using System.Collections.Generic;


namespace Game01.Rules.Build
{
    // todo: i think a single static function with all parameters necessary to compute the laser stats is needed 
    public static class BuildLaser // TODO: Should all methodds here be static?
    {
        // todo: for lasers of same class but with better stats, we could use MK I, MK II, etc instead of TL.  But i think the problem here is, if world TL is high, then that should lower the cost of MK 8 lasers.

        // todo: the goal of the Builder is to generate the custom property values for the actual Laser Entity 

        // todo: when user wants to re-edit an existing weapon, how do we get the current build settings

        // todo: i dont want user to have access to build rules or build specific properties.  They should only see final stats.  This means our build workspace should use the rules.  But again, when we want to re-edit an existing weapons, how then do we know the build settings currently in place?
        //  - well, the simplest solution is to just not allow re-editing.  User has to design from scratch.

        // todo: i dont think we want a seperate script for every single beam weapon type.  They are all the same with just differnet sets of data  We can save them as prefabs with different data, but they all still use the same script
        //       - well, maybe damage is calculated differently.  That may warrant a seperate script?  Well, rules for damage should pass through game00.dll.  

        // todo: and how does Production/Consumption fit in here during damage calculations  Well the handlers for production and consumption are in the scripts and those scripts can perhaps make calls to game00.dll rules API
        //  - problem is, whether something is for instance getting splash damage must be rules based an not simply location/proximity based.
        //   well, we can probably still compute a hitlocation derived from rules first
        //  NOTE: i think this is key... if we used actual aiming and player skill, it would still just yield a hitLocation.  So all our rules are doing is determining a hitLocation instead of it being based on physcially accurate aiming
        // NOTE2: production could be damage and a type of damage and we find consumers of that production.  This makes the graphics of the laser or kinetic "bullets" just a visual reference that we render.  We already know if they've hit before they leave the barrel
        // TODO: so this tells me that our lasers and bullets and such can be Entities which are only for rendering purposes.  Scripts would simply control their rendering and viduals and time on screen and fade in/out, vibrations, etc


        // todo: for now let's just modify our ai controlled ship to move in random positions as our target drone.

        // todo: then just create a laser script that just has our main statistics and lets determine what combat looks like.
        //      - because we want combat hit locations and misses and damage computed using Rules and not using realtime aiming

        // todo: start with test vehicle radar sensor breaker == true
        // todo: add right mouse click to target radar sensor icon attack, persuit, intercept, dock, etc

        // todo: is the beam itself a full entity or a sub-model that gets turned on and has an animation attached to it
        //       maybe it's the barrel that gets the sub model and the script... not the beam itself which really is just visualization.  It is the "weapon" (eg barrel) that "fires" and has the cooldown properties and also which determines hit location/miss
        // todo: but for turbo lasers, particle weapons and kinetic projectiles, we need more than just one sub-model because of the Rate of Fire can be more than 1.  They will probably need to be just simple entities with no script or logic of their own.
        // maybe they do have a script which simply tracks the weapon they were fired from and what their collision impact fx look and sound like.

        // todo: i think the EntityAPI should spawn the projectiles whether a laser or not.

        // todo: perhaps its the tactical crew station that "fires" at targets with approrpriate weapon.  Then the game01.rules can be computed
        // todo: tactical probably needs to gather "contacts" from all active sensors.  So we probably need to initializeentity with gathering various sensors by name

        // todo: the behavior tree for the npc crew station operator might not be necessary for player controlled ships, but we do need it for NPC . In otherwrords, we dont want that functionality built into the crew station scripts, but rather the NPC character operator of that station.
        //       maybe for automated tactical station controlled by the ship's computer, a behavior tree is ok?

        // todo: will NPC crew get saved to the test3.kgbentity NPC controlled vehicle prefab?  I can possibly add them when detecting the OnRegionLoadedCompleted()???
        // todo: if we load npc actors, how do we assign them as operators to the crew station


        // todo: for tactical station, the logic for determining hit+damage should rely on the crew station.css script and not the operator.  Instead, we just grab bonuses or minuses from the operator crew member.
        //  - time to get a lock
        //  - bonus for time 
        //  - bonus for damage
        //  and remember, it's the tactical station that keeps track of all the weapons available and the targets (including friendlies)
        // time for turret to rotate towards target


        // todo: the ai captain needs a "mission" or "objectives" for each mission
    }
}
