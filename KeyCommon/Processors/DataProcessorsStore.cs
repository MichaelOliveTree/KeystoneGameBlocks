namespace KeyCommon.Processors
{
    public class DataProcessorsStore
    {
        /// <summary>
        /// Memory<T> contains arrays of data for each interface needed by the DataProcessor
        ///
        /// PROBLEM: an array of Memory<object>[] that contains different value type structs
        ///           representing the various Component interfaces, will have to be boxed/unboxed
        ///          thus affecting performance.
        /// example:
        /// Memory<int> intMemory = new int[] { 1, 2, 3 };
        /// Memory<string> stringMemory = new string[] { "hello", "world" };

        /// // An array of Memory<object> to hold different types
        /// Memory<object>[] memories = new Memory<object>[2]; 
        /// memories[0] = intMemory.AsMemory().Cast<object>(); // Explicit cast needed
        /// memories[1] = stringMemory.AsMemory().Cast<object>();
        ///
        /// To avoid the problem, we do not use Memory<object> but only structs that represents
        /// such as Transform.Transform_Struct  or Transform.RigidBody_Struct, etc.
        /// </summary>
        
        // movement {steering, newtonian movement, interpolation animations, collisions}
        // sound, morale boosts (from the Captain himself for instance if nearby), 
        // energy, damage of various kinds, etc.
        public delegate void Processor<T>(ComponentStore<T> store, object parameters, int seed, GameTime gt);
        
        // TODO: there are some types of data processing where an Entity is always added... such as 
        //       currently when movement/flocking is computed because a "STEER" acceleration/force PRODUCTION
        //       is required every frame.
        //       HOWEVER, there are plenty of cases where an Entity would only be added if production was
        //       occuring such as a CHAIR producing +morale or -fatigue or +health but only when an
        //       OPERATOR was USING it.  


        private Keystone.Scene.Scene mScene; 
        private ComponentStore<T>[] mStores;
        
        /// <summary>
        /// Memory<T>[] contains arrays of data for each interface needed by the DataProcessor
        /// </summary>
        // private DataProcessor<IScene scene, Memory<T> data, object parameters> mDataProcessors;
        // we will need to cast the 'object' param to the appropriate DataProcessor 
        private Dictionary<string, object> mProcessors;
        

        public DataProcessors (Scene scene, ComponentStore<object>[] stores)
        {
            if (scene == null) throw new ArgumentNullException("DataProcessor.ctor() - scene cannot be NULL.");
            mScene = scene;
            mStores = stores;
            
            mProcessors = new Dictionary<string, object>();
        }
        

        // 1 - we need to know which Memory<T>[] interfaces the mDataProcessor[i] requires
        // 2 - we need to know how to determine which parameters are needed to be passed such as "Hz" or "targetDestination"
        // 3 - 
        // 4 - I think the only way to do this without performance problems of boxing/unboxing of Memory<T> 
        //     is to have the DataProcessor instance of Keystone that runs first in Simulation.cs know exactly which
        //     interfaces it requires, and then for Game01.dll game.Update() is to require it also know which
        //     Memory<T> types and parameters it needs to send for each DataProcessor.  
        //     - In other words, Keystone.dll KNOWS what processors and interfaces it has access to and to which it cannot
        //       run DataProcesssors for, and Game01.dll game.Update() is EXE specific and it too knows which Memory<T> 
        //       types it can handle.
        //       - For the different interfaces for example, I can use a bitflag and then find the correct ones by comparing those
        //         bitflag values
        public void Add (string name, Processor<Memory<T>> proc)
        {
            mProcessors.Add (name, proc);
            
            // this class probably needs to reside in Core.cs where it gets
            // called by Simulation.DataProcessor.Update(); followed by
            // Simulation.GameDataProcessor.Update();
            //
            // API needs call to add DataProcessor instances to this class
            
        }
        
        public void Update(Scene scene, Entities[] entities, GameTime gt)
        {
            for (int i = 0; i < mProcessors.Count; i++)
            {
                // TODO: make sure IScene implements Entities[] ActiveEntities
                //object[] interfaces = new object[2];
                // todo: i dont think the following works because there's apparently not a good way 
                //       to cast a Memory<object> to a type of Memory<T>
                //interfaces[0] = mStores[0];
                //interfaces[1] = mStores[1];
                string key = mProcessors.Keys[i]; // eg "STEER" or "COLLIDE"
                mProcessors[i].Invoke (GetComponentStore(key), mScene, GetParameters(key), gt);
                
                // NOTE: For intrinsic interfaces at least, we need to set changeFlags on the Entities
                // for mIsDirty updates to Matrices and BoundingBox.
                
                // TODO: SetChangeFlags() must be called... so i think we need a delegate/function pointer to be
                // stored in our Memory<T> for that interface.  Or we need to iterate at end through all active Entities that
                // changeFlags flags = ChangeFlags.BoundingBoxDirty | ChangeFlags.TranslationDirty | ChangeFlags.MatriDirty
                // were modified and call Entity[i].SetChangeFlags(flags)
            }
        }
        
        private Memory<T> GetStore(string key)
        {
            // TODO: temporary switch to find the correct DataStore containing our Memory<T>
            switch (key)
            {
                case "STEER":
                    break;
                case "COLLIDE":
                    break;
                default:
                    throw new NotImplementedException("DataProcessors.GetStore() - No store for key '" + key + "'" );
            }
        }
        
        
        private object GetParameters(string key)
        {
            // all parameters are tracked in KeyCommon.UserData
            // TODO: temporary switch to grab the correct parameters from KeyCommon.UserData.
            switch (key)
            {
                case "STEER":
                    break;
                case "COLLIDE":
                    break;
                default:
                    throw new NotImplementedException("DataProcessors.GetParameters() - No store for key '" + key + "'" );
            }
            
        }
        
        
        // Action and Action<T>:
        // For methods that perform an action and do not return a value. Useful for processing data that doesn't require a returned result, like logging or side effects.

        // Func<TResult> and Func<T, TResult>:
        // For methods that perform an operation and return a value. Ideal for data transformations, filtering, and calculations.

        // TInput and TOutput is the same as T1 and T2.  They are both just different Generic types T
        public List<TResult> ProcessData<TInput, TResult>(List<T> data, ProcessItem<TInput, TResult> processor)
        {
            List<TResult> processedResults = new List<TResult>();
            foreach (TInput item in data)
            {
                processedResults.Add(processor(item));
            }
            return processedResults;
        }
        
        public List<TResult> ProcessData<TInput, TResult>(Memory<T> data, ProcessItem<Memory<T>, TResult> processor)
        {
            List<TResult> processedResults = new List<TResult>();
            foreach (TInput item in data)
            {
                processedResults.Add(processor(item));
            }
            return processedResults;
        }
        
        //TODO: below would be in a Script and the delegate to that function
        //      would be passed during script.Initialize();
        
        // todo: what exactly are we "processing" for this sensor scan?  
        //       we do know for steering behaviors, eactly what algorithm to use for each crew 
        //       memory element.
        // This isn't a big deal. There is no difference iterating over these memory<T> arrayElements
        // and iterating over Entity except we just need to know what memory<T> to use in order to 
        // have access to the fields we want.  
        // For sensors, we may want 
        private void ProcessSensorScan(Memory<T> memory, SensorScanHandler handler)
        {
            handler?.Invoke(memory);
            
            // 1) Iterate over the structs using a for loop
            System.Diagnostics.Debug.WriteLine("Iterating with for loop:");
            for (int i = 0; i < memory.Length; i++)
            {
                MyStruct currentStruct = memory.Span[i];
                
                // what other data might this handler need? it depends on what exactly the SensorScanHandler
                // is doing.  Is it mearly checking to see what other emission productions are being detected
                // so it can then pass that info over to the contacts list of the sensor
            
                handler(currentStruct); // handler(memory.Span[i]);
                
                System.Diagnostics.Debug.WriteLine($"Value1: {currentStruct.Value1}, Value2: {currentStruct.Value2}");
                
                
                // THE ABOVE WONT UPDATE THE STRUCT WITHIN THE MEMORY<T> object
                // Structs and Value Semantics: Structs in C# are value types. 
                // When a struct is accessed from a Span<T> or Memory<T>, a 
                // copy of that struct is used, not a reference to the original 
                // instance. If the copied struct is modified, the changes won't 
                // be reflected in the original Memory<T> unless the modified 
                // struct is explicitly assigned back to the Memory<T> at the 
                // specific index
                
                
                // BUT THE BELOW WILL
                
                int sliceLength = 1;
                
                // a slice will result in a new Memory<Weapon> object with a span of 0 to sliceLength
                Memory<Weapon> singleWeapon = Weapons.Slice(i, sliceLength); // A Memory<T> representing the element at index 32
                singleWeapon = Weapons.Span[i];
                
                
                // assigning a modified weapon to the singleWeapon.
                int damageTaken = 5;
                Weapon modifiedWeapon;
                modifiedWeapon = singleWeapon.Span[0];
                modifiedWeapon.CurrentHP -= damageTaken;
        
                // two ways to modify the data in the original Memory<Weapons[]>
                singleWeapon.Span[0] = modifiedWeapon;        // 1) modifying the shared element
                Weapons.Span[i] = modifiedWeapon;  // 2) modifying the struct at the specified index directly
                
            }
    
            //System.Diagnostics.Debug.WriteLine("\nIterating with foreach loop (using Span<T>):");
            // 2) Iterate over the structs using a foreach loop (requires getting Span<T>)
            //foreach (var currentStruct in memory.Span) 
            //{
            //    System.Diagnostics.Debug.WriteLine($"Value1: {currentStruct.Value1}, Value2: {currentStruct.Value2}");
            //}
        
        
            System.Diagnostics.Debug.WriteLine($"Memory<T> processed");
        }
        
        /// <summary>
        /// We pass in gameTime so we have access to the realtime elapsed
        /// as well as the simulated Time elapsed.
        /// </summar>
        public void ProcessData<TInput, TResult>(Keystone.Simulation.GameTime gameTime. List<TInput> data, ProcessItem<TInput, TResult> processor) 
        {
            
            List<TResult> processedResults = new List<TResult>();
            foreach (TInput item in data)
            {
                processedResults.Add(processor(item));
            }
            return processedResults;
    
            
            // Store for position, scale, rotation, matrices, need to be in 
            // Keystone or KeyCommon
            
            // Store for physics state also needs to be in Keystone or KeyCommon.
            
            
            // IEntitySystems.Update()
            
            //
            // movement of crew (steering)
            //   linear acceleration / decelaration
            //   newtonian ship movement
            //   movement of ships via Steering 
            //
            // physics Update
            //   N-Body
            // laser bolts
            // missiles
            
            // particle Systems
            // motion fields
            // 
            
            // collisions (BoundingBox.Min, BoundingBox.Max, and Sphere.Center and Sphere.Radius need to be in a Memory<T> struct)
            //
            
            // Animations 
            //   - interpolation Animations
            //   - spritesheets
            // 
            // 
            // game specific
            //    - power drain
            //    - fuel drain
            //    - OnFire
            //    - InRadiation
            //    - UnderWater/InVaccuum
            //
            //    - applying accumulated damage
            //    -   ""         "" damage
            //    -   ""         "" bufs
            //    -   ""         "" debufs
            // 
            //    - sensor scan (lambda)
            
            //    - planetary scan
            //    - AreaOfInterest 
            //    // - storing data on interior Walls for fast iteration of mouse picking
            //    // walls and floors and ceilings.  <-- This is mostly for when our view is such that
            //    // we cannot first determine the closest edge and use that to find any wall on that edge
            //    // For instance, imagine a camera that is more like a FPS view or a bullet or laser hits a Walls
            //    // 
            //    - storing data on interior Walls and Floors and Ceilings "damage"
            
            
            
            
        }
    }
}