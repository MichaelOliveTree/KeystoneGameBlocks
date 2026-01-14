using System;


namespace KeyCommon.Simulation
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entityID"></param>
    /// <param name="production"></param>
    /// <param name="elapsedSeconds"></param>
    /// <returns>Consumption Result array so that they can be sent to other players</returns>
    public delegate Consumption[] Consumption_Delegate(string entityID, Production production, double elapsedSeconds);

    public enum PropertyOperation : byte
    {
        Replace = 0,
        Add,        // typically for adding an array element
        Remove,     // typically for removing an array element
        Union,      // merge two arrays with no duplicates
        Increment,  // for numeric propertyspec values to add the propertySpec value to the existing value within the Entity
        Decrement,
        Additive_Multiply,
        Additive_Divide
    }

    // consumption is more charged with the algorithm for computing how much consumption
    // of the particular product the Entity will use.  This includes everything from 
    // consuming damage or gravity to consuming electricity, water or fuel.
    // It will take into account modifiers such as "stealth" to determine consumption if any. 
    // For instance, a "microwaves" consumption could result in 0 consumption if the distance between
    // producer and consumer is too great or there is an applicable "stealth" modifier
    
    //  It will also take into account modifiers from the crew operator at a station for example.
    
    // TODO: should our Consumption_Delegate return "ConsumptionResult" so that these changes
    // can be sent to other players over the network?
    
    // details information about how much this device will consume.  This is returned
    // when Consumption delegate is invoked in a script for a particular entity.
    // todo: maybe we should think of this as ConsumptionResults and host all the changes that need to be applied to the target entities
    //       so we could include an array of PropertySpec and corresponding nodeIDs
    public struct Consumption // todo: rename this to ConsumptionResult
    {
        // Consumption here is really PRODUCT CONSUMPTION RESULT struct that gets filled so that
        // other players in the networked game can receive the "results" of 
        // having consumed a product
        public string ConsumerID; // the entity that is consuming a product
        public string ProducerID; // todo: should production and consumption be handled server side and the Consumption "result" passed to the client? Simulation.cs should maybe be run on loopback server.
        public uint ProductID;     // todo: i think the productID can be different than what the consumption handler is passed in. For instance, "heat" can be passed in and result in "damage" to be applied to the consumer
       // public object UnitValue; // obsolete - we use PropertySpec[] now with intrinsic types. // the Simulation EXE will know how to deal with UnitValue basedon ProductID.  This could also be "damage." 

           
        public string TargetID; // NOTE: this does mean that an entity performing consumption can change properties of other nodes and not just itself. Typically though, its only for entities within a single ship hierarchy from Exterior to Interior components
        public PropertyOperation[] Operations;
        public Settings.PropertySpec[] Properties; // todo: what about HelmState and TacticalState properties? Well, "tacticalstate" and "helmstate" are properties in the ship.css and they are serializable over the wire.
        // todo: do we need to be able to send this over the wire with NetBuffer Read and Write?
        // todo: we should probably need to know whether the property values are meant to replace, increment, or decrement the existing value.  "store" is a good example. If we're multithreaded, we might need to lock each node before we apply changes
        //       I could include an array of int[] operation; that is same length and specifiy 0=replace, 1=increment and 2= decrement, 3 = add array element, 4 = remove array element
        // todo: maybe instead of seperate objects like HelmState and NavPoints we just use regular custompropertyspec for each member.  This will make it easier for ConsumptionResult handling without keystone.dll needing to know anything about those custom types.
        // todo: well first, lets just use PropertySpec with intrinsic types.  
    }
}
