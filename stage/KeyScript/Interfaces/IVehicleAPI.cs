using System;

namespace KeyScript.Interfaces
{
    public interface  IVehicleAPI
    {
        // Obsolete?: now using  IEntityAPI.GetComponentsOfType() 
        // because i think these sorts of Vehicle API which starts to get into application specific 
        // concepts should be avoided.  We should keep the API app agnostic.
        // get all components of a particular user component type
        // string[] GetComponentsOfType(string vehicleID, int componentClassID); 

      
        // TODO: why not just allow these to be in our common 
        // EntityNode.Query()  can be used to find any pattern match
        // and using entityID as a VehicleID, we can then easily query
        // the interior region of the Vehicle
            //  Predicate<Entity> match; (see Simulation.cs for Consumers and Producers searches)
  //      string[] GetEngines(string vehicleID);
  //      string[] GetThrusters(string vehicleID);
  //      string[] GetSensors(string vehicleID);
  //      string[] GetWeapons(string vehicleID); // a way to pass a filter?
    }
}
