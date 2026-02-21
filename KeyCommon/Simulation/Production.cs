using System;

namespace KeyCommon.Simulation
{
    /// // TODO: this delegate has to be modified to look like our DataProcessors as in 
    /// // KeyCommon.Processors -> public delegate void Processor<T>(ComponentStore<T> store, object parameters, int seed, GameTime gt);
    /// // because we are using a Data Oriented processing model that will accept all of the entities that will produce a particular productIDs.
    /// TODO: we have a bit more thinking to do here because we know that for some components, we want to produce multiple things like
    /// MicrowaveEmission and MicrowaveDamage.    I think to do this, the Entity via its script will just register seperatelyh for BOTH types of production
    /// and then the handlers will determine how much emission and damage is produced by this particular component.
    public delegate Production[] Production_Delegate(string entityID, double elapsedSeconds);

    // this represents the total production during this tick.
    // it could also use -1 for infinite production such as a gravity production.
    // The production then also describes it's distribution/transport model as either
    // List of directly attached consumers or an emission volume which any consumer of the type
    // can consume until the production's amount is depleted
    public struct Production
    {
        // todo: should i have a frequency or Hz?  Gravitation would be at Physics frequency, but other's should be 1 hz or every 1000 ms
    	// production is not serialized to XML because they are created by the scripts in code
        public string SourceEntityID;
        public uint ProductID;
        public Keystone.Types.Vector3d Location; // location where this production is occurring (eg. explosion, heat signature, etc)
        public object UnitValue;  // eg. for thrust this contains double, for radar echos, UnitValue is a Vector3d position
        public int UnitCount; // infitie = -1, else number of unit's 
        public DistributionType DistributionMode; 
        public Func<Production, string, bool> DistributionFilterFunc; // accepts Production and an EntityID and returns true if the test is passed
        // used when DistributionType is List.  Contains id of entities consuming this product.  
        // No searches (spatial or otherwise) reqt. "power links" and other "links" are good examples of their use.
        public string[] DistributionList;  
        public object SearchPrimitive;   // used with DistributionMode is a spatial search of some kind.
        
        //public float Efficiency;      // obsolete - efficiency is grabbed from custom property in script
        //public float Regulator;        // obsolete - throttle is grabbed from custom property in script
        //public bool Breaker;          // obsolete - enabled is grabbed from custom property of a "Machine" in script
     

//        public Production (string serializedString)
//        {
//        	const string delimiter = ",";
//        	string[] split = serializedString.Split (new string[] {delimiter}, StringSplitOptions.None);
//        	
//        	SourceEntityID = split[0];
//        	ProductID = uint.Parse (split[1]);
//        	UnitValue = split[2];
//        }
        
        // TODO: is tostring() and constructor from serialize string unnecessary?
        // production isn't something we need to serialize as it's constructed from the script
        // however it is stored in a CustomProperty 
        public override string ToString()
		{
        	
        	const string delimiter = ",";
        	
        	// string result = string.Join (delimiter, SourceEntityID, ProductID, UnitValue, UnitCount, DistributionMode, DistributionFilterFunc, SearchPrimitive, DistributionList);
        	
        	
        	// i dont think we can use comma delimiters here 
        	// customvalues = '0,0,0;1;0.5; ???? ; // where ???? is our struct which can contain comma delimited values for a single property, plus semicolons between properties
        	// // TODO: what if there was a way to store the custom attributes in with the intrinsic attributes and get them automatically parsed into the custom properties?
			// // we would no longer need a seperate persist string        	
			return string.Format("[{0},{1},{2},{3},{4},{5},{6},{7}]", 
        	                     SourceEntityID, ProductID, UnitValue, UnitCount, DistributionMode, DistributionFilterFunc, SearchPrimitive, DistributionList);
		}
 
        // if we could store all custom property values in a seperate child node that would solve the problem 
        // can we switch to using a subnode for custom 
        
    }
}

//    // let's think hypothetically about a fission reactor (fusion wont produce radiation)
//    // it produces heat (which can feed into life support)
//    // it produces electricity
//    // it produce radiation
//    // Each of the above "Productions" would be a seperate Production 
//    // struct in the Machine's array of Production.
//    // double supply = Production.Produce(elapsed);  returns the amount produced
//    // Now this supply is either distributed or stored.  
//    // If storage space runs out, depending on the type it either is simply lost
//    // or the machine must shut down.
//    // - HOW TO MANAGE SUBSCRIPTIONS & DISTRIBUTIONS? 
//    //
//    // i wonder if Production should be in Common or something?  I'm torn because
//    // this is somewhat game stuff and not engine.  But on the other hand
//    // this is really more of a Supplier/Consumer pattern
//    public struct Production
//    {
//        public delegate double ProductionUpdate(string domainObjectID, double elapsedMilliseconds, float rate, float throttle);
//        public UInt32 ProductID;
//        public float Rate;    // amount of units consumed per second

//        // confused on some of these vars because where does the machine/entity pass in
//        // vars used for the computation, and which exist here?  I think one good argument
//        // to keep them here is that a machine that produces/consumes multiple things
//        // may have seperate throttle values and efficiency values and even different enable/disable
//        // states
//        // But why not have some of these custom properties in the Entity then?
//        public bool Enabled;
//        public float Efficiency; // at same throttle, increased efficiency will produce more
//                                 // as the machine wears out between mainteneance efficiency
//                                 // will drop.  It is also possible to increase efficiency
                                 
//        public float Throttle;  // value typically 0 -1.0 but can exceed 1.0 with potential risk
//                                // of damaging the machine (is Damage a customProperty in Entity?)
//        private ProductionUpdate UpdateHandler;  // within here we can use the Rate
//                                                           // but also use different formulas 
//                                                           // (linear/curved/exponential/etc)
//                                                           // to compute production rate based on
//                                                           // things like throttle, damage of the
//                                                           // owning entity machine.
//        private List<Consumption> mConsumers;


//        public Production(UInt32 productID, float rate, ProductionUpdate handler)
//        {
//            ProductID = productID;
//            Rate = rate;
//            UpdateHandler = handler;
//            Enabled = true;
//            Throttle = 1.0f;
//            Efficiency = 1.0f;
//            mConsumers = new List<Consumption>();
//        }

//        public void Subscribe(Consumption consumer)
//        {
//            if (mConsumers == null) mConsumers = new List<Consumption>();
//        }

//        public void UnSubscribe(Consumption consumer)
//        {
//            if (mConsumers == null) throw new ArgumentOutOfRangeException();
//            if (!mConsumers.Contains(consumer)) throw new ArgumentOutOfRangeException();
//            mConsumers.Remove(consumer);
//        }

//        public double Produce(string entityID, string domainObjectID, double elapsedMilliseconds)
//        {
//            if (!Enabled && Throttle == 0) return 0d;
//            System.Diagnostics.Debug.Assert(Throttle >= 0);

//            if (UpdateHandler == null)
//            {
//                return LinearConsumption(elapsedMilliseconds, Rate, Throttle);
            
//            }

//            // TODO: rather than return Producers we should now distribute
//            // the supply based on our subscribers and based on priority of those subscribers
//            // 
//            return UpdateHandler (domainObjectID, elapsedMilliseconds, Rate, Throttle);

//        }

//        private double LinearConsumption(double elapsedMilliseconds, float rate, float throttle)
//        {
//            // simple built in linear consumption is default
//            double ratePerSecond = elapsedMilliseconds / 1000d;

//            return ratePerSecond * throttle * Efficiency;
//        }

//        //protected ~Production()
//        //{ 
//        //    // if this production is destroyed, we must notify subscribers yes?

//        //    // we must also de-list as a produce provider in the overall Vehicle's
//        //    // list of producers.  

//        //    // This is necessary as a way for consumers to find producers and for the user
//        //    // to then be able to assign consumers to providers.  There is one thing though
//        //    // how does automatic mapping of consumers to providers occur?  Like for instance
//        //    // a hole breaks into a wall, air now moves from the neighboring compartment and the
//        //    // characters who were suffocating can now consume that air and are rescued.
//        //    //
//        //    // So how does this sort of diffusion match with direct assignment of consumers to
//        //    // producers?  And how do we implement conduits and pipelines if the user is
//        //    // directly connecting rather than laying down the conduits/routes/links/pipelines/vents
//        //    // 
//        //    // I guess to an extent, the user can subscribe parts of a "grid" to producers
//        //    // and then for every grid part they can define max flow that the grid conduit
//        //    // can carry... then consumers are connected to this grid?

//        //    // or maybe more of a sim city approach where you paint the grid onto the cells
//        //    // and if a tile has a reactor that neighbors a tile with a grid, then that reactor
//        //    // automatically is connected to that grid.  


//        //}

//    }

