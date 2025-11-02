using Settings;
using System;
using System.Collections.Generic;


namespace Game01.Rules
{
    public static class Combat
    {
        // for components
        // type   eg: station, weapon
        // subtype  eg: tactical, helm, beam, particle, cannon, railgun
        // users[] default = ANY \\ these are the assigned users, not currently in use users. First Officer can reassign users \\ TODO: SHOULD UUSERS REFER TO ACTUALY CHARACTER NAMES FOR EASE OF DEBUGGING AND PLAYER UTILIZATION?
        // currentusers[] - \\ for bunks, this would contain two active slots. For most components, this array lenght should be 0 or 1
        // securitylevel
        // priority \\ for stations, this determines which has authority if its not broken/disabled/destroyed. Think of it like zorder

        // for crew and passengers
        // class
        // rank
        // primary station
        // primary role

        // 1- update test vehicle prefabs with names for turret, laser, tactical operator
        //  - relying on node.Name is not a good way to diffrentiate between components.  I should have a customproperty "component" and if its null, we skip it 
        //  - either "component" or "type.  For crew, type can be rank/position or maybe class like Marine
        //      - wouldn't we need a "subtype" too?  for instance "type" = "weapon" and subtype = "beam"
        //      - what about "users[]" for components like bunks?
        //          - medical beds need to allow "any"
        //          - doors can use the users[] property to determine which rooms are accessible by which crew members.  The first officer's script should assign all crew to accommodations
        //              - maybe doors also have a "securitylevel" to allow sensior officers to enter doors that are not explicitly assigned to them as "users"
        //      - current_user
        // - also what about "primary, secondary, tertiary" ?
        // - we may want to put various components into buckets based on "type" to make searches faster
        //        - buet lets not concern outselves with premature optimization
        // 2- disable behavior tree from ai controlled vehicle
        // 3- have ai ship target a random point within range of the weapon and fire laser for x time (we'll worry about energy consumption later) 
        //      this requires a beam entity to be spawned.  the beam's script only causes the beam to vibrate.  Maybe the laser Entity is always attached and is only made visible\enabled when fired
        //      similarly, kinetic bullet Entities can be added that are invisible and when fired detach and move towards target?  this way each weapon can cache it's own bullets.  The bullets will know which weapon they were fired from and reset when they fly out of range
        //      same for particle weapons or turbo lasers that fire with a RoF > 1 
        // 4- each beam or bullet has a lifetime which can be computed as weaponMaxRange / projectTileSpeed
        // todo: this function could perhaps use a lot of parameters
        // 5-if the projectile speed > -1 (instantaneous) then our RoF needs to be managedd with timeElapsed values in the script.  In fact, the weapon script (not the beam or projectile) can manage the lifetime of the projectiles that it emits


        public static bool RangedAttack(PropertySpec[] station, PropertySpec[] @operator, PropertySpec[] weapon, PropertySpec[] target, double distanceToTarget)
        {
            bool hit = true;

            return hit;
        }

        public static Keystone.Types.Vector3d GetHitLocation(out string hitEntityID)
        {

            Keystone.Types.Vector3d impactPoint = Keystone.Types.Vector3d.Zero();

            // todo: should this also return an entityID for what was hit
            hitEntityID = null;

            // thie Vector3d hit location is only useful for determining where to spawn a particle system / explosion

            // todo: once this hitlocation is determined, we can for some cases like explosive productID, determine what can consume this product
            return impactPoint;
        }


        public static int GetDamage(PropertySpec[] weapon, PropertySpec[] target, double range)
        {
            // todo: what is the PD for the target

            // todo: non hard coded damage
            int damage = 50;
            // todo: armor passive defense 
            // todo: i think the script should create the struct and fill it out including the productID
            // todo: damageType and do we have potentially multiple components or vehicles or Entities receiving damage?
            // todo: does this return an array of structs
            // todo: how do we damage interioir components or crew?
            return damage;
        }

        private static object GetValue (PropertySpec[] properties, string propertyName)
        {
            for (int i = 0; i < properties.Length; i++)
                if (properties[i].Name == propertyName)
                    return properties[i].DefaultValue;

            return null;
        }
    }
}
 