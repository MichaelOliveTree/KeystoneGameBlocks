using System;
using Keystone.Entities;
using Keystone.Elements;
using Keystone.Controllers;
using KeyCommon.Flags;

namespace Keystone.Entities
{
    //http://www.simswiki.info/wiki.php?title=TS3PR/Sims3GameplayObjects
    // http://geck.bethsoft.com/index.php/Weapons
    //http://www.simswiki.info/wiki.php?title=TS3PR/Sims3GameplayObjects

    // the flagship (Vehicle) is the only direct user controllable vehicle a player can control.  The key distinction
    // as far as implementation goes is IControllableVehicle which will configure this vehicle to respond to INPUT keyboard and mouse
    // or COMMANDS and for IControllableVehicle to interpret those commands and deal with sending those commands to the network
    // but for now, we're just going to have a simple command processor
    //
    // TODO: above is a bit daft i think.  There's no need for player vs regular Vehicle.
    //       user input would create commands and not control the vehicle directly!  This way
    //       commands are always sent via network as per usual.
    //
    // TODO: Vehicle should be good enough.  Why not just use a flag to differentiate
    // between player and NPCs
    
    
    // http://www.simswiki.info/wiki.php?title=TS3PR/Sims3GameplayObjects
    public class Vehicle : Container
    {
        // TODO: CustomProperties suggestions
        // Owner - userID
        // Maintenance History - spacedock on date x,x,x,x [overhaul engines, refueld reactor, hull fractures treatment]
		// Access\Command codes - or are access codes more about the mainframe computer on the ship?
        // DockStatus = Undocked, Docked
        // LandStatus = Landed, Not Landed
        // DesignStatus = Designed, Undesigned, UnderConstruction
        // Simulation = power simulation during design mode
        //		- automatic while in design mode, power use is always simulated power use?
		// 		- on/off breakers are always simulated when not docked
		//		- ship can use space station's externally provided power so long as space station/stardock is generating enough power.
		//
		// 
		
        // RE: Contacts, see notes at Hud.cs Pick()
        //               see notes at Simulation.cs.UpdateForceProduction()
        //      - contacts in a way represents not necessarily visual items from cull pass
        //      they are game elements that are sent to the renderer for rendering.  This makes
        //      them part of a "Hud" and thus picking them reflects picking a visual representation
        //      back to the underlying "Contact" item.
        // Collections?  If you consider how these sensors and propulsion and such are
        // "ticked" during simulation, the collection isnt really required for that, however
        // when it comes to listing things, these collections are necessary but perhaps just
        // helper functions for that?
        // and then, we just need "Contacts" directly on Vehicle.
        // - Sensors.Contacts
        // - Propulsion.
        // - 
        // Can contacts be "scripted" somehow?  I mean i think we need to examine "contacts" front to back
        // - A "contact" list could be a property of "Vehicle" where the type is
        //   a collection of Contacts which are described in our common script
        // - Then a "contact" can easily be manipulated in the Vehicle script.  The "Vehicle" script
        //   however would need a way to process this incoming message from the server that a contact
        //   would found.  This is similar to how we use propulsion as Product/Consumer
        //      1) Enemy and Friendly and unknown vehicles emit an "emission" product of some kind.
        //      1b) Filter of the emission occurs server side from script so that only 
        //         consumers that should "detect" a contact will consume that emission.
        //      1c) Then skill test of sensors/sensor operator needs to be done.
        //      1d) so scripting of this filer in user_functions.css is what we need to do.
        //      2) player vehicle receives the emission and consumes it.   
        //         - consuming means adding to Vehicle.Contact list.  Or updating that Contact list
        //         wth the current information about that contact and also registering with Hud and such.
        //         I'm not sure how registering with Hud.cs occurs though.  I think our Hud.cs should be
        //         written in client.exe and be written knowing that a Vehicle contains .Contacts and that
        //         when in Navigation mode, there is a Vehicle that is the players that is assumed to be
        //         active and focus of that Navigation.  
        //         - but wait, surely that must be done server side because if a client is given this information
        //         there is no way to ensure that the client will properly ignore those
        //         emissions that fail the detection test.
        //       2b) Consumption of the emission by a sensor should then update any contact list and
        //           register for rendering somehow. I think the contact list if it's a scripted object
        //           perhaps managed by the Vehicle script actually (contact list is a custom property)
        //           must register for the contact to be rendered on a hud.  I think that must involve
        //           the Hud.cs just knowing that there is a "contacts" custom property in Vehicle 
        //           
        //      3) This makes me wonder if instead, a "sense" product is emitted, and consumed
        //          by a client, that may or may not emit a "detection" result.
        //        
        //      
        // RE: Contacts, see notes at Simulation.cs.UpdateForceProduction()
        //          - a sensor contact that is a ship that was added to the scene because initially
        //            detected, could track it's lastDetection time and thus when it becomes too stale
        //            can be removed.  So in this case, that it's in the scenegraph is ok because it's a
        //            proxy and may not contain full details AND because it's current position is subject
        //            to update info by server.
        //           - normally a contact discovery would be done independant of graphics culling.
        //            but once discovered, its added to scene and the Vehicle.Sensors.Contacts
        //            will then manage when those contacts are stale and removed.  
        //           - contacts i think will then need to be resolved soon...
        //            Assuming the simulation will notify the client of contact update info, users
        //            can add some preliminary info about that contact to the db.  But it will be
        //            varying degrees of accurate.
        //              - but some contacts i think we may know its a ship, but not which one
        //                so adding to main db is not correct.  instead we'd want more of a tempary
        //                contacts db.  In fact, i think our "dynamic" db is now appearing to be 
        //                a very seperate thing.  for dynamics we can have historical plot info
        //                like stocks, and current dbs for contacts made within last x interval.
        //            - this way also full vehicles of other players will also exist outside the main db
        //              sending over the zipped xmldb of a single vehicle should be pretty tiny if there
        //              are no mesh/geometry/textures and just xml.
        //protected const double METERS_PER_MILLISECOND = .001;

        public Vehicle(string id) : 
            base(id) 
        {
            mEntityFlags |= EntityAttributes.ContainerExterior;
            // Vehicles and other controllable objects should not be serializable.
            // They are tracked and loaded into the scene dynamically from the db.
            // May.10.2017 - Serializable = false makes it impossible to import as Vehicle (Container)! So 
            Serializable = true;
        }

        public Vehicle(string id, Geometry exteriorGeometry)
            : this (id)
        {
            if (exteriorGeometry == null) throw new ArgumentNullException();
            AddChild(exteriorGeometry);

            //TODO: a script file should define hardpoints
            // on the vehicle.  Unless we use the script to point
            // to another text file that then lists and defines the hardpoints.
            // The idea is that pointing to a seperate file means you can use a modeling tool
            // to output the hardpoint data and never touch the script itself.
        }


        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }


        // TODO: i think this Vehicle class is obsolete.  Just an Entity should be fine? 
        // We certainly dont need this Update() function anymore because we use Behavior trees now.
        // this should Update logic and Animation... and with physics im just not sure yet how we integrate whether physics must move us
        // or whether we can tell it to only do collision and response but not general movement updates... 
        //public override void Update(float elapsedMilliseconds)
        //{
        //Vector3d perFrameMoveVelocity = new Vector3d();
        //double secondsElapsed = elapsedMilliseconds * METERS_PER_MILLISECOND; // elapsed is passed in as millisoncds.  Convert to seconds

        //// get the accumulation of all thrust from all firing thrusters
        //// TODO: get all thrusters
        //// itterate thru them combining the thrust
        //Vector3d totalVelocity = new Vector3d();
        //perFrameMoveVelocity = totalVelocity;
        //perFrameMoveVelocity *= secondsElapsed;

        //// TODO: to rotate the ship, a thruster should be designated as having the effect
        //// of rotating the ship about it's bounding center (which will say is center of mass as well)
        //// and 
        //double rotationRadians = Utilities.MathHelper.DegreesToRadians((double)rotationDegrees);
        //Vector3d pos = Translation;
        //// TODO: we should be tracking our position independantly?  The actor's position shoulb be strictly for rendering?  Or should they?  I mean, a BonedModel is instanced. But i could see not wanting to have to touch the Scene tree since it might be in seperate thread so local tracking isbette
        //pos.z += (double)(Math.Cos(rotationRadians) * perFrameMoveVelocity) +
        //         (double)(Math.Cos(rotationRadians + Utilities.MathHelper.PiOver2) * perFrameStrafeVelocity);
        //pos.x += (double)(Math.Sin(rotationRadians) * perFrameMoveVelocity) +
        //         (double)(Math.Sin(rotationRadians + Utilities.MathHelper.PiOver2) * perFrameStrafeVelocity);

        //// TODO: for threaded, here we need to Queue this command
        ////  Commands.SetPosition setPosition = new SetPosition(pos.x, pos.y, pos.z);
        ////  Simulation.QueueCommand(setPosition);
        //// or
        //// Simulation.QueueCommand(new SetMove(pos, rotationDegrees, scale));
        //Rotation = new Vector3d(0, rotationDegrees, 0);
        //Translation = pos;

        //// update the animation and skinning
        //base.Update(this, elapsed);
        //// NotifyChildEntities(); // TODO: i dont think this is necessary if we are updating Translation or Rotation or Matrix here
        //}
    }
}
