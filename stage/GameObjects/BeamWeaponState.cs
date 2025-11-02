using System;
using System.Collections.Generic;
using Keystone.Types;

namespace Game01.GameObjects
{
    // todo: some of the "state" is useful to see when inspecting the weapon's stats and abilities and limitations.
    //       but having these members grouped under a "state" object makes grabbing the data we need in the script easier and faster 
    //       since we don't have to grab each stat seperately
    public class BeamWeaponState
    {
        private string mVehicleID;
        private string mEntityID; //
        private string mTacticalStationID;
        private string mOperatorID; // operator can be a computer running targetting AI. Can also be null.

        // todo: i think i will just only allow modders to design weapons.  
        //       in v1.0 users can only select from a list of weapons when\if they refit their defalt ship.

        // todo: i think the question is about GUI and property grids or SourceGrid.
        //      - custom properties allow us to define what is available at mod-time or arcade time or both.
        //          - then additionally we have private run-time variables that are only available to the entity script or behaviortree such as AI data.
        // todo: there is some run-time state that needs to be serialized because when we resume play, we want the starting state to be the same.
        // todo: also recall that our client/server design was to be based on tasks/orders that then get carried out by the crew or computer systems of the Vehicle. How do we ensure deterministic results?
        //       - for starters, the update Hz needs to be identical.
        // todo: what about the sql db that stores tasks/orders?  Wouldn't this balloon out of control?  We need maybe to limit tasks/orders to those of the command staff/bridge crew only.
        //  - i think originally i was thinking client/server would be sychronized just by the orders/tasks given along with the time\tick the command was issued and perhaps when it was consumed by the crew or ship's onboard computer.
        //  - todo: we could restrict saving to being not in red-alert, and perhaps only when docked at a station.

        // lets start from the top
        //
        // - create a simple helm station
        //      - add a helm.css script file to it where upon it can retrieve the engine IDs and thruster IDs and cache them upon initialization.
        //      - assign friendly name targets for engines and thrusters
        // - add a single crewmen to operate that station
        // - assign the helmsperson a command\task\order to navigate to a particular planet (at a particular speed) in the Sol system (eg Mars)
        // - add the order to the task database
        // - 
    }
}
