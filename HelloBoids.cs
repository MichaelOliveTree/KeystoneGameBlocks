#define DEBUG
#define CACHE_VERTICES
#define USE_STRUCT 		// instead of classes for Quaternion and Matrix
#define USE_MEMORY_T  	// 500 FPS Memory<T>, 530 FPS Classes -> 2000 iterations @ 758 BOIDS  2800 FPS for Parallel.For with Memory<T> 
                        // HOWEVER, this is with OctreeOcant.OnEntityMove() not being called during the parallel loop.
						// once we added a lock() or semaphore to the re-entrant OctreeOcant.Add() and OctreeOctant.OnEntityMove(), the FPS 
						// went down to 465 FPS from 2800 FPS.  The "OnEntityMoved()" needs to be made much faster.
						
#define CONCURRENT_TIMERS
#define SPATIAL_SEARCH       // this define enables adding to Octree 
#define SPATIAL_MOVE_UPDATES // this define enables update of the Octree as moving of Entities occurs (430fps WITHOUT vs 150fps WITH)

//#define DEBUG_OUTPUT

using System.Collections;
using System.ComponentModel;

//using System.Collections.Generic; //used by UITypeEditor


using System.Runtime.CompilerServices; // needed for using "[MethodImpl(MethodImplOptions.AggressiveInlining)]"
using System.Diagnostics;
using System; 
// using System.Memory;   // not needed for online compilers running latest .net version
using System.Reflection; // used for "MethodBase" type in Profiler
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
// using System.Numerics;
// using System.Runtime.Intrinsics; // for SIMD enabled code
using Microsoft.CodeAnalysis;
using Microsoft.CSharp;
using System.IO;



// @LT Gaming
// @ObsidianAnt
// @EnterElysium
// 
// The Art of Moebius (French artist with amazing scifi landscapes
// re: proc-gen of Moons in Elite Dangerous, "the composition of the regolith and atmosphere tends to match that of the host planet they orbit, just as our moon is similar to Earth)

// "Starship EVO"           <-- like the simple geometry, materials with no textures
// "Fallen Frontier"
// "C-Beams"
// "D.O.R.F RTS"
// "SAD:Frontier"           <-- newtonian
// "Children of Dead Earth" <-- newtonian + n-body gravitation, ship building, space combat


// NOTES: On Indices and GUIDS
//   
//       that also contained UserTypeID
//       Actually, we can store the UserInterfaceStruct flags in a uint bitflag, we just
//       use that to determine  if we should call GetUserStruct<> for the various structs.
//       Enum.HasFlag() used to be slow in older versions of .net, but direct bitwise
//       operations has always been very fast so we should use them so we know whether
//       a specific type of user struct exists before calling GetUseStruct(type);

// Entity.mUserTypeID is required. mUserTypeID does NOT belong in Component or LivingEntity
// 
// KGB DOES need a GUID primarily because of saved prefabs that can be created by and shared amongst all players.
// 
// TODO: the problem with KGB is GUID needs to be kept in the saved XML, but index refers to where its
//       stored in these ComponentStore<T>   so what do we do here?  I dont think we can guarantee
//       the order of Entities within these arrays across server and clients.  These
//       structs must always be local machine ONLY.  

// A HASH of EntityIDs could yeild an Integer that we can use for sorting them within a List<>
// This then needs to constantly update whenever Entities are Added/Removed from the Scene...
// Also for MMO, this needs to be managed for each "ZONE" 
// https://discussions.unity.com/t/staticentityidrange-for-simple-fast-scene-loading-and-external-entity-refs/725631/7

// if we use an unsigned long for our entity IDs that gives us 18446744073709551615 
// if we allow up to max uint for number of Entities in a given game that is 4,294,967,295
// that allows for 4,294,967,295 unique games containing 4,294,967,295 unique Entities.
// or more games if the max number of Entities in any is less.
// For reference, Counterstrike is said to have total number of matches in the TENS of BILLIONS
// since 1999 to 2026
// BUT THERE IS A MAJOR PROBLEM WITH THESE ENTITY RANGES FOR A GAME LIKE SCIFICOMMAND where 
// anyone can create prefabs.... they would always need to grab an unique INT from a SERVER
// to ensure uniqueness across the prefabs of all other creators, including those assets made for the official released version of ScifiCommand.
// So, GUID is  better...  
// We probably must HASH the GUID and use that as a way to sort Entities in our List<EntityNode>


//
// // https://erikmcclure.com/blog/multithreading-problems-in-game-design/

	/*
	Updating game entities in parallel while maintaining determinism requires strict control over update ordering and data access, typically achieved using an Entity-Component-System (ECS) architecture with a job system or double-buffering. Determinism ensures that given the same initial state and inputs, the simulation produces identical results every time, regardless of the machine or number of CPU cores used.Reddit
	ReddiHere are the key approaches to achieve parallel, deterministic updates:

	1. Structured Parallel ECS (Job System)
	Modern ECS frameworks (like Unity DOTS) allow running systems in parallel while maintaining order through dependency tracking. 

	System Dependencies: Use [UpdateBefore] and [UpdateAfter] attributes to define a strict order of execution for systems.
	Job Scheduling: Use ScheduleParallel() for systems that do not conflict, which automatically splits work across cores while maintaining deterministic ordering of data processing.
	Avoid Non-Determinism: Do not use Run() in a way that allows arbitrary thread scheduling. Ensure that if systems depend on each other, they are synchronized using Dependency.Complete(). 

	2. Double-Buffering (Read-Only Input, Write-Only Output)
	To avoid race conditions, systems should read from the current state and write changes to a "next state" buffer. 

	Process: Par	allelize reading entity data (Component A, B) to calculate results.
	Deferred Mutation: Write new component data (Component C) to a separate buffer.
	Swap: After all systems finish, swap the read and write buffers.
	Result: All entities update based on the same snapshot of the previous frame, eliminating dependency on thread execution order. 

	3. Deterministic Ordering and Sorting
	If entities are updated in parallel, the order of modification must not matter, or it must be explicitly enforced. 

	Sort Entities: If the outcome depends on which entity updates first, sort entities by a fixed ID before processing.
	Avoid Hash Maps: Avoid data structures where iteration order changes, as this can break determinism between different machine architectures. 

	4. Deterministic Simulation Techniques
	Fixed Timestep: Run the simulation logic on a fixed cadence (FixedUpdate in Unity, for example), separate from the rendering framerate.
		Floating Point Constraints: Ensure that floating-point calculations are identical across platforms (e.g., using fixed point math or forcing strict IEEE 754 compliance).
	Deterministic RNG: Use a seeded random number generator. Ensure it is called in the same order every frame. 

	Key Considerations for Parallelism
	Data Layout: Use contiguous memory (Arrays/NativeContainers) for component data to allow parallel access without locking.
	Job System Hazards: Ensure that parallel jobs do not write to the same memory location. Use NativeParallelHashMap or NativeArray with strict index management to ensure safe parallel writes. 
	*/	

// By the way, this is what Media Molecule does in Dreams. The Trackmania racing games do this as well, to verify runs and make sure people aren’t cheating. Even their 3d physics engine is fully deterministic! very cool stuff.

//	Notes:

//  You need to make sure entities are always updated in the same order. This means deterministic O(1) datastructures like pools are your friend.
// If you use random numbers then you need to make sure the seeds match at the start of every tick as well. You can probably get by storing only one seed along with the first
// The stored replay gets invalidated once you change your gameplay logic, so this method is generally useful for debugging only.

//https://jakubtomsu.github.io/posts/fixed_timestep_without_interpolation/ < -- i cant easily do a memcpy to copy the gamestate like this.... storing animation states for us is much more difficult.
//                                                                              well, perhaps we only just need to copy the previous animation's "weight"
// https://www.rfleury.com/p/main-loops-refresh-rates-and-determinism
// 1 - simulaton thread - outputs state
// 2 - user thread (drawing here including animation updating)
// 3 - input gathering thread

// https://www.youtube.com/watch?v=fdAOPHgW7qM <-- frame rate independance for animation... render tick method?

// https://www.youtube.com/watch?v=72y2EC5fkcE
// TODO: deterministic
//       fixed step
//       - track each frame 'long currentFrame'
//       
//       ability to "step" play backwards and forwards
//       animation state decoupling for interpolation
//  ___________________________________________________
//  TODO: Procedural Generation Focus
//        - seeds and determinism and such


// 2 - Test determinism of spawning with a parallel.For() loop and using the ThreadedRandom.cs

// 3 - Instead of just one buffer accessed through ComponentStore<>.Span, create two buffers 
//     ComponentStore.ReadOnlySpan  and ComponentStore.WriteOnlySpan, ComponentStore.SwapBuffers()
//     for double buffering.  This means we can multithread the updates and not worry about the state
//     of each EntityNode changing until all parallel threads are complete and then we can SwapBuffers.

// 4 - Replay system with single step FWD and REVS functions

// 5 - procedural generation of a Colony : IEntitySystem -> both proc generation with seeds/THreaded<Random> and updates

// 6 - Status Effect System - for both attributes (eg +2 morale to subordinates), spells, weapon uses (eg lasers), and items
//     For KeystoneGameBlocks, the idea is to keep the code modified in the same way as our DataProcessor system.
//
//     https://www.gamedev.net/forums/topic/692150-status-effects-buffs-debuffs-in-an-ecs-architecture/
//     https://www.gamedev.net/forums/topic/719143-stucked-on-creating-statuseffects/
//
//     https://www.reddit.com/r/gamedev/comments/50rrcs/code_design_for_an_ability_status_effect_system/
//     https://github.com/Improx/ModifiedValues
//     https://stackoverflow.com/questions/2197966/designing-a-clean-flexible-way-for-a-character-to-cast-different-spells-in-a-r
//     https://gamedev.stackexchange.com/questions/147873/creating-a-robust-item-system
//     https://medium.com/@kryzarel/character-stats-attributes-in-unity-pt-1-70f90ade9788

// 6 - SIMD code

// 7 - OCTREE 				
//     - octree Add and Moves should probably Enqueue and then get applied all at once
//       in fact i believe this does occur in our main KeystoneGameBlocks src branch because
//       we use a method named FinalizeMovement()



// FIXES Feb.8.2026
//   - started adding code for Laser fire damage effects processing 
//   - Added destructor and IDisposable Dispose() to ComponentStoreCollection.cs and ComponentStore.cs and BoidSimulation.cs
//   - Added destructor to Transform.cs for freeing up the memory of Memory<T>... i think this still needs work to keep the Memory<T> blocks packed correctly.
//   - Fixed Semaphore.Wait(-1) which means wait indefinetely for ComponentStoreCollection.CheckOut() and ComponentStore.CheckOut()

	
// TODO: THE SAMPLE FROM GITHUB https://github.com/swharden/Csharp-Data-Visualization/blob/main/website/content/simulations/boids/index.md
// and simply uses System.Drawing to draw the boids.  I will want to just use a simple 3d pyramid type boid .obj instead.
// https://github.com/swharden/Csharp-Data-Visualization/blob/main/website/content/simulations/boids/index.md

// NOTE: The primary purpose of this is to demonstrate the use of Memory<T>
// via ComponentStore.cs (ComponentStore.ReadOnlySpan and ComponentStore.WriteOnlySpan)
// to increase performance by updating Entities using a data-oriented processing model 
// in order to take advantage of cache coherency instead of the typical Entity.Update()
// model which does not.
// NOTE: We will also be able to experiment with writing DETERMINISTIC code and
// being able to STEP forward and BACKWARDS through the simulation and
// ultimately even being able to REPLAY a "recording" of the simulation or parts
// of it.
namespace HelloBoids
{
    // https://vscode.dev/github/MichaelOliveTree/KeystoneGameBlocks
    public class EntryClass
    {
		private static string MODE = MODE = "Memory<T>"; // Classes or Memory<T> and is set at RunTime but defaults to Memory<T> unless #define Memory_T is commented out		

        // NOTE: cube shaped otrees are MUCH faster than non cubed octreesbecause they are easier to keep balanced when inserting entityNodes
        public static double WIDTH = 800d;
        public static double HEIGHT = 800d;
        public static double DEPTH = 800d;
		public static double BOID_SIZE = 2d;             // since this is 2D, we need a size for the Octree's Z depth 
		public static uint NUM_ENTRIES = 768;
        public static uint NUM_ITERATIONS = 400;
        public static double MAX_RUNTIME_SECONDS = 5.5;
		
		// Note: the larger the various distance values below,
        // the more cpu cycles needed. Tweak these values
        // to find a good balance between performance and
        // simulation/behavior quality
		//public static double MAX_SEARCH_DISTANCE = 35d;
		public static double SEPERATION_DISTANCE = 25.0d;
		public static double ALIGNMENT_DISTANCE = 15.5d;
		public static double COHESION_DISTANCE = 12.5d;
		
		public static double SEPARATION_FACTOR = 0.5d;
		public static double ALIGNMENT_FACTOR = 0.2d;
		public static double COHESION_FACTOR = 0.1d;
		public static double TURN_FACTOR = 0.1d; // For boundary avoidance
		public static double MAX_SPEED = 5d;
				
		private static bool useOctree = false;
		private static uint OctreeMaxDepth = 12;         // NOTE: this is ignored if Octree.EnforceMaxDepth == false in which case the splitthreshHold and radius of the entity being added is the main determinant
		private static uint OctreeSplitThreshold = 8;
		
		public static HelloBoids.UserDataStore mUserDataStore;
        public static HelloBoids.ComponentStoreCollection mCStoreCol;
        public static BoidSimulation bSim;
		
		
        public static double step;
        private static double mTotalRuntime;
        public static long mCurrentFrame;
		
        private static bool mMainLoopIsRunning;
		private static bool mGameLoopIsRunning;
		 
        private static object mMainLoopLock; 
		private static object mGameLoopLock; 
		private static object mRenderLoopLock;
			
		// Debugging Aids
		// fragment the memory to account for fact that our
        // Boid objects wont be instantiated contiguously like this in production code
        public const int FRAGMENTED_OBJ_SIZE = 128;
		public const int NUM_TO_PIN = 0;
		
		public static Profiler CodeProfiler;
        public static string output;
		public static string mSimulationOutputFile;
		public static string OUTPUT_FILENAME = "hello_output.txt";

		

        public static void Main()
        {			
            MODE = "Memory<T>";
            bool structs = false;

#if USE_MEMORY_T == false
            MODE = "Classes";
#else
	#if USE_STRUCT
            structs = true;
   #endif
#endif
	
	   
#if SPATIAL_SEARCH
            useOctree = true;
#endif


            CodeProfiler = new Profiler();
            CodeProfiler.ProfilerEnabled = true;
            CodeProfiler.ShowFramesPerSecond = true;
            CodeProfiler.CategorizeByTypename = true;
            CodeProfiler.Verbose = true;
            // CodeProfiler.FullyQualifiedTypename = true

            // NOTE: since profiler is global, we register these vars once no matter how many viewports are open
            // Registers profiles before we use them
            int categoryIndex = 0;

            // processing
            categoryIndex++;
            string dataProcessing = "Data Processing";
           
            CodeProfiler.Register("AssignSpan", dataProcessing);
            CodeProfiler.Register("Update() - Process Frame", dataProcessing);
            CodeProfiler.Register("GetNeighbors", dataProcessing);
            CodeProfiler.Register("FlockingRules", dataProcessing);
            CodeProfiler.Register("GetDistanceSquared", dataProcessing);

			CodeProfiler.Register("GetSearchArea", dataProcessing);
            CodeProfiler.Register("IntersectsSearchArea", dataProcessing);					

			// output some information about this program and the settings for this Performance Test
            output = "Hello Boids - " + Utils.GetTimeString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);

			output = "Remote Computer Has " + Environment.ProcessorCount.ToString() + " processors.";
            Console.WriteLine(output);
            Debug.WriteLine(output);
			
			output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);
			
			
            output = "MODE = " + MODE;
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "USE OCTREE == " + useOctree.ToString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "USE STRUCTS INSTEAD OF CLASSES FOR Quaternion and Matrix == " + structs.ToString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);


            System.Threading.Thread renderThread = new System.Threading.Thread(RenderLoop);
            Console.WriteLine("Main() - Render thread created.");

            System.Threading.Thread animationThread = new System.Threading.Thread(AnimationLoop);
            Console.WriteLine("Main() - Animation thread created.");

            System.Threading.Thread gameThread = new System.Threading.Thread(GameLoop);
            Console.WriteLine("Main() - Game loop thread created.");
            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);
			

            mSimulationOutputFile = Utils.CreateFile(OUTPUT_FILENAME);
            
            // Set as background so the application can exit when the main thread ends
            // TODO: this may not be necessary if I just set the exit condition to the known
            // number of iterations that will be performed so the sentinel "mIsRunning" can be
            // set to = false;
            gameThread.IsBackground = true;
            gameThread.Start();

            Console.WriteLine("");
            Console.WriteLine("Main() - Performance Test #1 STARTED in Game thread.");
            Console.WriteLine("");

			
			// This main thread waits for user input to stop the application OR for the gameLoop
			// to finish
			mMainLoopLock = new object();
			mMainLoopIsRunning = true;
            while (mMainLoopIsRunning)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    lock (mMainLoopLock)
                    {
                        mMainLoopIsRunning = false;
                    }
                    break; // Exit the main thread's loop
                }
            }
			
			//Console.WriteLine("Waiting to Join Main Thread... Loop #1");

            // Ensure the game thread has time to stop gracefully (optional for background threads)
            gameThread.Join();

            Console.WriteLine("Main() - Game Loop #1 COMPLETED.");
            
            
            ///////////////////////////////////////////////////////////////////////////////////////////////

            // Reset Game Thread and start GameLoop again for Test #2                      
			
            Console.WriteLine("");
			output = "____________________________________________";
			Console.WriteLine(output);
			Console.WriteLine("Main() - Performance Test #2 STARTED in Game thread.");
            Console.WriteLine("");

            gameThread = new System.Threading.Thread(GameLoop);
            // Set as background so the application can exit when the main thread ends
            // TODO: this may not be necessary if I just set the exit condition to the known
            // number of iterations that will be performed so the sentinel "mIsRunning" can be
            // set to = false;
            gameThread.IsBackground = true;
            gameThread.Start();


            // This main thread waits for user input to stop the application OR for the gameLoop
			// to finish
			mMainLoopIsRunning = true;
            while (mMainLoopIsRunning)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Q)
                {
                    lock (mMainLoopLock)
                    {
                        mMainLoopIsRunning = false;
                    }
                    break; // Exit the main thread's loop
                }
            }

            gameThread.Join();

            Console.WriteLine("Main() - Game Loop #2 COMPLETED.");
			
			Console.WriteLine("");
			output = "____________________________________________";
			Console.WriteLine(output);
            output = "Main() - Goodbye Boids! - " + Utils.GetTimeString();
            Console.WriteLine(output);
        }


        // simulation updates
        private static void GameLoop()
        {
			output = "____________________________________________";
			Console.WriteLine(output);
            Debug.WriteLine(output);
			
            output = "GameLoop() - Entered. Test RUNNING - " + NUM_ENTRIES.ToString() + " boids @ " + NUM_ITERATIONS.ToString() + " iterations.";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "GameLoop() - Please Wait...";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            // NOTE: the entire UserDataStore get's passed to our various "data processors."
            //mUserDataStore = new UserDataStore();

            // TODO: checkout of UserData needs to occur when the Entity is created?
            //       Not all Entities need it though? Hrm.  
            //UserData data = mUserDataStore.CheckOut(entityID);
            //entity.Data = data;


            // WARM UP the code so that the loops are JIT properly
            // =====================
            /* System.Diagnostics.Debug.WriteLine("WARM-UP - RUNNING.");
             Console.WriteLine("WARM-UP - RUNNING.");
             TestClasses(classes, r);
             Processor<TestStruct> p = TestIntrinsicProcessor;
             p.Invoke(store, parameters, r);
             System.Diagnostics.Debug.WriteLine("WARM-UP - COMPLETED.");
             Console.WriteLine("WARM-UP - COMPLETED.");
             */
			
			
			mGameLoopLock = new object();
           	
			mCStoreCol = new HelloBoids.ComponentStoreCollection();
			mUserDataStore = new HelloBoids.UserDataStore();
			
           	bSim = new BoidSimulation((int)NUM_ENTRIES, WIDTH, HEIGHT, DEPTH, useOctree);
			
			
            CodeProfiler.StartLoop();
            Stopwatch sw = Stopwatch.StartNew();
            double lastElapsedTime = sw.Elapsed.TotalSeconds;
			GameTime gt = new GameTime();
			double targetFrameRatePerSecond = 60d;
            step = 1d / targetFrameRatePerSecond; // aka dt or "deltaTime"

            // 100FPS uses a step of 1 / 100d == 0.01 seconds 
            // or 10.00 milliseconds per framer
            //
            // 60FPS uses a step of 1 / 60d == 0.0166666666666667 seconds 
            // or 16.66 milliseconds per framer
            //
            // 30FPS uses a step of 1 / 30d == 0.0333333333333333 seconds
            // or 33.33 milliseconds per frame
			
			mCurrentFrame = 0;
            mTotalRuntime = 0;
			mGameLoopIsRunning = true;
			
            while (true)
            {
                bool runningStatus;
                lock (mGameLoopLock)
                {
                    runningStatus = mGameLoopIsRunning;
                }

                if (!runningStatus)
                    break; // Exit the game loop

                // Calculate delta time (time since last frame)
                double totalElapsedSeconds = sw.Elapsed.TotalSeconds;
                double elapsedSeconds = totalElapsedSeconds - lastElapsedTime;
                lastElapsedTime = totalElapsedSeconds;

                //HACK - make the elapsedSeconds always equal to fixed step
                elapsedSeconds = step;
				TimeSpan ts = TimeSpan.FromSeconds(elapsedSeconds);
				gt.Update(ts);

                // Update and Render operations
                Update(gt);
                //Render();

                mCurrentFrame++;
                mTotalRuntime += elapsedSeconds;

                //Console.WriteLine("TOTAL = " + totalElapsedSeconds.ToString() + " Frame Time = " + lastElapsedTime.ToString() + "  Runtime elapsed == " + mTotalRuntime.ToString() + " of " + MAX_RUNTIME_SECONDS.ToString() + " seconds.");
               // if (mTotalRuntime >= MAX_RUNTIME_SECONDS)
               if (mCurrentFrame >= NUM_ITERATIONS)
                    mGameLoopIsRunning = false;

                // Simple throttling to prevent maxing out the CPU (adjust as needed)
                //System.Threading.Thread.Sleep(15);
            } // end While loop
			
			lock (mMainLoopLock)
			{
				mMainLoopIsRunning = false;
			}
			
            CodeProfiler.EndLoop();

            TimeSpan timeSpan = sw.Elapsed;

            // Format and  the TimeSpan value.
            string elapsedTimeString = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds,
                timeSpan.Milliseconds / 10);


 
            output = "GameLoop() - Performance Test COMPLETED - " + Utils.GetTimeString();
            System.Diagnostics.Debug.WriteLine(output);
            Console.WriteLine(output);


            Console.WriteLine("");
            output = "GameLoop() - Clearing Resources (" + MODE + ")";
            Console.WriteLine(output);
            Debug.WriteLine(output);
			
			output = "____________________________________________";
			Console.WriteLine(output);
            Debug.WriteLine(output);
			
			
			// Dispose Simulation and ComponentStores
			bSim.Dispose();
            bSim = null;
            
        	mCStoreCol.Dispose();
        	mCStoreCol = null;
			
			output = "____________________________________________";
			Console.WriteLine(output);
            Debug.WriteLine(output);
			
			
			// regardless of the NUM_ITERATIONS used, the FPS should typically all be very close 
            // if the NUM_ENTRIES stays the same
            double fps = mCurrentFrame / timeSpan.TotalSeconds;
			
            // LOG THE RESULTS
            // =====================
            output = "GameLoop() - RunTime Mode: " + MODE + " - " + elapsedTimeString;
            Console.WriteLine(output);
            Debug.WriteLine(output);
            output = "GameLoop() - FPS = " + fps.ToString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "GameLoop() - Remote Computer Has " + Environment.ProcessorCount.ToString() + " processors.";
            Console.WriteLine(output);
            Debug.WriteLine(output);
			
			output = "____________________________________________";

            Console.WriteLine("");
            output = "GameLoop() - Begin Profiler Output (" + MODE + ")";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            CodeProfiler.OutputToConsole();

            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);
            output = "";
            Console.WriteLine(output);
            Debug.WriteLine(output);

					
		#if DEBUG_OUTPUT
			string text = Utils.ReadAllText(EntryClass.mSimulationOutputFile);
		
			Console.WriteLine ("GameLoop() - DEBUG OUTPUT - Translations and Velocities");
			const int BufferSize = 256;
			using (var fileStream = System.IO.File.OpenRead(EntryClass.mSimulationOutputFile))
			{
				Console.WriteLine ("GameLoop() - Debug Output - Filestream null == " + fileStream == null);
				
  				using (var streamReader = new System.IO.StreamReader(fileStream, System.Text.Encoding.UTF8, true, BufferSize)) 
				{
    				string line;
    				while ((line = streamReader.ReadLine()) != null)
    				{
      					// Process line
						Console.WriteLine(line);
    				}
  				}
			}
		#else
			Console.WriteLine ("GameLoop() - DEBUG OUTPUT #DEFINE DISABLED");
		#endif
			
            // Validate() is only used to make sure the compiler doesn't take a huge shortcut
            // and not run the test code at all because none of the TestClass or TestStruct 
            // members are ever used.
            //     Validate(store, classes);

            // TODO:implement a https://en.wikipedia.org/wiki/K-d_tree

			
        }

        private static void Update(GameTime gt)
        {
            using (EntryClass.CodeProfiler.HookUp("Update() - Process Frame"))
            {
                bSim.Update(gt);
            }	
        }

        private static void UpdateLoop()
        {
            // TODO: Insert game logic (physics, AI, input processing from a queue)
            // Use lock when accessing shared data
        }

				
        private static void RenderLoop()
        {
			mRenderLoopLock = new object();
			
            // TODO: Insert console drawing logic
            // Use lock when writing to the console to avoid conflicts
            lock (mRenderLoopLock)
            {
                //Console.SetCursorPosition(0, 1);
                // Console.WriteLine($"Game running. Time: {DateTime.Now}");
            }
        }

        private static void AnimationLoop()
        {

        }
    }


    public class BoidSimulation : IDisposable
    {
#if USE_MEMORY_T
        public DataProcessorsStore mDataProcessor;
#endif

		
		
		
        public List<EntityNode> Boids { get; set; }
					// NOTE: The statistics do not exist within each Droid and so we can keep them
			//       when a Droid is Destroyed and then Respawned and the Statistics can continue
			//       to accumulate with the newly spawned replacement for that Droid assuming its using
			//       the same ID/Profile which is how I envision a screensaver type auto-play game would work.
		public List<Statistics> Statistics {get; set;}
		
		
		private System.Collections.Concurrent.ConcurrentDictionary<int, List<Tuple<int, double>>> mNeighbors = new System.Collections.Concurrent.ConcurrentDictionary<int, List<Tuple<int, double>>>();
		internal System.Collections.Concurrent.ConcurrentDictionary<int, ComponentStore<Production>> mProduction;
        internal System.Collections.Concurrent.ConcurrentDictionary<int, ComponentStore<Consumption>> mConsumption;
		
		
        public Seeds Seeds { get; set; }
						 
		//public ThreadedRandom mTHRandom;
		
        private double SeparationDistance;
        private double SeparationFactor ;
        private double AlignmentDistance;
        private double AlignmentFactor;
        private double CohesionDistance;
        private double CohesionFactor;
        private double MaxSpeed;
        private double TurnFactor; // For boundary avoidance

        public OctreeOctant Octree { get; }
        public static IntervalTimers mIntervalTimers;


#if USE_MEMORY_T
        public static ComponentStore<Transform.Transform_Struct> Store;
#endif

		public static DamageSystem mDamageSystem = new DamageSystem();
		public static DamageOverTimeSystem mDamageOverTimeSystem = new DamageOverTimeSystem();
		public static HealthSystem mHealthSystem = new HealthSystem();
		public static SkillModificationSystem mSkillModificationSystem = new SkillModificationSystem();
		public static SkillSystem mSkillSystem = new SkillSystem();
		
		private const CONFIGURATION HumanOperatorConfiguration = CONFIGURATION.Transform | CONFIGURATION.RigidBody |  CONFIGURATION.LifeForm | CONFIGURATION.Sentient | CONFIGURATION.Intelligent | CONFIGURATION.SelfPropelled;
		private const CONFIGURATION BoidConfiguration = CONFIGURATION.Transform | CONFIGURATION.RigidBody | CONFIGURATION.LifeForm | CONFIGURATION.SelfPropelled;
		private const CONFIGURATION OpticalSensorConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing | CONFIGURATION.Sensor;
		private const CONFIGURATION WingsConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing;
		private const CONFIGURATION LaserConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing | CONFIGURATION.Weapon | CONFIGURATION.Laser;
		private const CONFIGURATION TacticalStationConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing | CONFIGURATION.TacticalStation;
		private const CONFIGURATION BatteryConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerProducing;
			
			
		
		public const int OPTICAL_SENSOR_OFFSET = 1;
		public const int WINGS_OFFSET = 2;
		public const int LASER_OFFSET = 3;
		public const int TACTICAL_STATION_OFFSET = 4;
		public const int BATTERY_OFFSET = 5;
		public const int HUMAN_OPERATOR_OFFSET = 6;
		
		public static SimulationEventManager mSimEventManager;
		
		private object mLock = new object();
		
			
		
		
        public BoidSimulation(int numBoids, double width, double height, double depth, bool useOctree = false)
        {
            Boids = new List<EntityNode>(); //NOTE: we do not preallocate the list here
			Seeds = new Seeds(123);
	
						
			SeparationDistance = EntryClass.SEPERATION_DISTANCE;
        	SeparationFactor = EntryClass.SEPARATION_FACTOR;
        	AlignmentDistance = EntryClass.ALIGNMENT_DISTANCE;
        	AlignmentFactor = EntryClass.ALIGNMENT_FACTOR;
        	CohesionDistance = EntryClass.COHESION_DISTANCE;
        	CohesionFactor = EntryClass.COHESION_FACTOR;
       		MaxSpeed = EntryClass.MAX_SPEED;
        	TurnFactor = EntryClass.TURN_FACTOR; // For boundary avoidance
	
			// NOTE: mLimitedProduction may not be necessary as we now track the NumUses for any given Production and if
			//       p.NumUses == 0, then we remove that production at the end of UpdateProduction();
			//mLimitedProduction = new System.Collections.Concurrent.ConcurrentDictionary<uint, List<Production>>();
			//mProduction = new System.Collections.Concurrent.ConcurrentDictionary<uint, List<Production>>();
        	//mConsumption  = new System.Collections.Concurrent.ConcurrentDictionary<uint, List<Consumption>>();
			mProduction = new System.Collections.Concurrent.ConcurrentDictionary<int, ComponentStore<Production>>();
        	mConsumption  = new System.Collections.Concurrent.ConcurrentDictionary<int, ComponentStore<Consumption>>();
			mSimEventManager = new SimulationEventManager(EntryClass.mUserDataStore);
			
#if USE_MEMORY_T

            mDataProcessor = new DataProcessorsStore(EntryClass.mCStoreCol);
#endif
            mIntervalTimers = new IntervalTimers();
            		
            if (useOctree)
            {
				//Console.WriteLine ("Width = " + width.ToString() + " Height = " + height.ToString());
                Vector3d min, max;
                min.x = -width * 0.5d;
                min.y = -height * 0.5d;
                min.z = -depth * 0.5d;
                max.x = min.x + width;
                max.y = min.y + height;
                max.z = min.z + depth; 

                OctreeOctant parent = null; // root has no parent
                BoundingBox box = new BoundingBox(min, max);
         
               // OctreeOctant.MaxDepth = EntryClass.OctreeMaxDepth;
				//OctreeOctant.SplitThreshHold = EntryClass.OctreeSplitThreshold;
                Octree = new OctreeOctant(0, 0, box, parent);
            }

#if USE_MEMORY_T

            // add data processors
            DataProcessorsStore.Processor<LifeForm> lifeCycleBehavior = DoLifeCycle;
            mDataProcessor.Add("LIFECYCLE", lifeCycleBehavior);

			
			DataProcessorsStore.Processor<Transform.Transform_Struct> opticalSensorsDetect = ProcessOpticalSensors;
            mDataProcessor.Add("OPTICAL_SENSING", opticalSensorsDetect);	
			
            DataProcessorsStore.Processor<Transform.Transform_Struct> flockingBehavior = DoFlocking;
            mDataProcessor.Add("FLOCKING", flockingBehavior);
	
			DataProcessorsStore.Processor<Consumption> powerConsumption = ProcessPowerConsumption;
            mDataProcessor.Add("POWER_CONSUMPTION", powerConsumption);
			
			//DataProcessorsStore.Processor<BoidSimulation.ImpalingDamage> lasersBehavior = DoWeaponTest;
            //mDataProcessor.Add("LASERS", lasersBehavior);
	
			//DataProcessorsStore.Processor<BoidSimulation.ImpalingDamage> laserImpalingDamageBehavior = DoImpalingDamage;
            //mDataProcessor.Add("LASER_IMPALING_DAMAGE", laserImpalingDamageBehavior);
			
#endif

            // SPAWN INITIAL SET OF BOIDS UP TO EntryClass.NUM_ENTRIES
			//System.Numerics.BigInteger bint = 0;
            decimal bint = 0;

			System.Diagnostics.Debug.Assert(EntryClass.NUM_ENTRIES == numBoids);
	
			
			Console.WriteLine("BoidSimulation.ctor() - Preparing to Spawn " + numBoids + " with SEED == " + this.Seeds.Master.ToString());
			
			//NOTE: List<> (which stores our Boids and EntityNode) is not threadsafe and so for .Add() we must prefill it with 
			// null items so we can use direct assignment (eg Boids[i] = b;  rather than Boids.Add(b); when spawning them
			// NOTE: either of the below two lines of code will work to fill the list to the desired amount with nulls
			const int ENTITIES_PER_DROID = 7;
			int numElements = numBoids * ENTITIES_PER_DROID;

			Boids = new List<EntityNode>(new EntityNode[numElements]);
			//Boids = Enumerable.Repeat<EntityNode>(null, numElements).ToList();
						// NOTE: The statistics do not exist within each Droid and so we can keep them
			//       when a Droid is Destroyed and then Respawned and the Statistics can continue
			//       to accumulate with the newly spawned replacement for that Droid assuming its using
			//       the same ID/Profile which is how I envision a screensaver type auto-play game would work.
			string PREFIX = "stats_";
			Statistics = new List<Statistics>(new Statistics[numElements]);
			

			// Spawn the Boids using Parallel.For() and optional memory fragmenting
			System.Threading.Tasks.Parallel.For(0, numBoids, i=>
            //for (int i = 0; i < numBoids; i++)
            {
				Random mTHRandom = ThreadedRandom.Instance; //(this.Seeds.Master);
				
                // todo: the above doesn't make a diff, but perhaps
                // if i added dummy objects into the array instead..?
                object[] tmp = MemoryFragmenter.CreateAndFreeObjects(EntryClass.FRAGMENTED_OBJ_SIZE);
                for (int j = 0; j < tmp.Length; j++)
                    bint += tmp[j].GetHashCode();

     			if(EntryClass.NUM_TO_PIN > 0)
                    MemoryFragmenter.Fragment(EntryClass.NUM_TO_PIN, 512, EntryClass.NUM_TO_PIN / 2, 128);

				// spawn will add to the Octree 
				Tuple<Boid, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode> result = Spawn(mTHRandom, (int)i * ENTITIES_PER_DROID, width, height, depth);
				int arrayIndex = (int)i * ENTITIES_PER_DROID;
                
				Boids[arrayIndex]                           = result.Item1; // NOTE: must use direct assignment after having pre-initialize List<> since List<> is not threadsafe
				Boids[arrayIndex + OPTICAL_SENSOR_OFFSET]   = result.Item2;
				Boids[arrayIndex + WINGS_OFFSET]            = result.Item3;
				Boids[arrayIndex + LASER_OFFSET]            = result.Item4;
				Boids[arrayIndex + TACTICAL_STATION_OFFSET] = result.Item5;
				Boids[arrayIndex + BATTERY_OFFSET]          = result.Item6;
				Boids[arrayIndex + HUMAN_OPERATOR_OFFSET]   = result.Item7;
				
				
				Statistics[arrayIndex] = new Statistics (PREFIX + arrayIndex.ToString());
				
				//Boids.Add(b); // <-- will not work here as List<> is not threadsafe
				//Console.WriteLine("i == " + i.ToString()); 
								  
                if (EntryClass.NUM_TO_PIN > 0)
                    MemoryFragmenter.Cleanup();
            });
	
            Console.WriteLine("BoidSimulation.ctor() - " + numBoids + " Boids Created. " + (numBoids == Boids.Count).ToString() + "  Big Hash = " + bint.ToString());
        }
        
        ~BoidSimulation()
        {
            Dispose();
        }
		
		#region SensorContacts and Target manipulation belongs in SCRIPTS ULTIMATELY		
		        
		public int[] GetOwner(int[] entityArrayIndices)
		{
			EntityNode[] owners = new EntityNode[entityArrayIndices.Length];
			int[] indices = new int[entityArrayIndices.Length];
			
			for (int i = 0; i < owners.Length; i++)
			{
				owners[i] = GetOwner(entityArrayIndices[i]);
				if (owners[i] != null)
					indices[i] = owners[i].EntityArrayIndex;
				else
					indices[i] = entityArrayIndices[i]; // the entity is already the overall Vehicle and has no "owner."
			}
			
			return indices;
		}
		
		
		//NOTE: This implementation of GetOwner() is a hack. In KGB we have nested Child entities and finding the "owner"
		//      is just a matter of recursing upwards through the tree until the Starship/Container is found.
		public EntityNode GetOwner (int entityArrayIndex)
		{
			uint config = (uint)Boids[entityArrayIndex].Configuration;
			EntityNode owner; 
			int index = entityArrayIndex;
			
			if (config == (uint)TacticalStationConfiguration)
			{
				index -= BoidSimulation.TACTICAL_STATION_OFFSET;
			}
			else if (config == (uint)LaserConfiguration)
			{
				index -= BoidSimulation.LASER_OFFSET;
			}
			else if (config == (uint)WingsConfiguration)
			{
				index -= BoidSimulation.WINGS_OFFSET;
			}
			else if (config == (uint)BatteryConfiguration)
			{
				index -= BoidSimulation.BATTERY_OFFSET;
			}
			else if (config == (uint)OpticalSensorConfiguration)
			{
				index = index - BoidSimulation.OPTICAL_SENSOR_OFFSET; ;
			}
			else if (config == (uint)BoidConfiguration)
			{
				return null;
			}
			
			owner = Boids[index];
			return owner;
		}
		
		
		public EntityNode GetOwner (EntityNode entity)
		{
			return GetOwner(entity.EntityArrayIndex);
		}
		
		public EntityNode GetEntity (int entityArrayIndex)
		{
			return Boids[entityArrayIndex];
		}
		
		public HitPoints[] GetHitPoints(int[] entityArrayIndices)
		{
			HitPoints[] hitpoints = new HitPoints[entityArrayIndices.Length];
			
			for (int i = 0; i <  hitpoints.Length; i++)
				hitpoints[i] = GetHitPoints(entityArrayIndices[i]);
			
			return hitpoints;
		}
		
		public HitPoints GetHitPoints(int entityArrayIndex)
		{
			EntityNode e = Boids[entityArrayIndex];
			int index;
			
			if ((e.Configuration & (uint)CONFIGURATION.LifeForm) != 0)
			{
				Memory<LifeForm> lf = (Memory<LifeForm>)e.GetUserStruct(typeof(LifeForm), out index);
				return lf.Span[0].HitPoints;
			}
			else
			{
				System.Diagnostics.Debug.Assert((e.Configuration & (uint)CONFIGURATION.Component) != 0, "GetHitPoints() - Unexpected Entity CONFIGURATION");
				Memory<Component> comp = (Memory<Component>)e.GetUserStruct(typeof(Component), out index);
				return comp.Span[0].HitPoints;
			}
		}
		
			// NOTE: This is horribly inefficient because it just iterates t hrough all EntityNodes to find the
			//       one "Sensor" that has the expected EntityKey that starts with "sensor_" and otherwise has same number part as this Droid's mID
		public EntityNode[] GetSensors(int entityArrayIndex)
		{
			if (EntryClass.bSim.Boids == null) return null;
			
		
			//int numPartOfKeyBOID = int.Parse(EntryClass.bSim.Boids[entityArrayIndex].EntityKey.Split("_")[1]);
			int numPartOfKeyEYES =  entityArrayIndex + BoidSimulation.OPTICAL_SENSOR_OFFSET; // int.Parse(EntryClass.bSim.Boids[entityArrayIndex + 1].EntityKey.Split("_")[1]);
			
			string sensorKeyForThisBoid = "sensor_" + numPartOfKeyEYES.ToString();

			System.Diagnostics.Debug.Assert(sensorKeyForThisBoid == EntryClass.bSim.Boids[numPartOfKeyEYES].EntityKey);
			return new EntityNode[] {EntryClass.bSim.Boids[numPartOfKeyEYES]};
			
			// NOTE: the below isn't necessary.  
			// NOTE: previously when this loop was failing it was because the Key I was searching for "sensor_###" 
			//       could NEVER possibly exist because the entityArrayIndex for the associated sensor to a given boid is
			//       always entityArrayIndex + 1.  There is a sensor_111 for example, but never a sensor_110 that is just 1 less.
			//       They always are in increments of 2, same with the boid's indexArrays too.
			List<EntityNode> found = new List<EntityNode>();
			for (int i = 0; i < EntryClass.bSim.Boids.Count; i++)
			{
				if (EntryClass.bSim.Boids[i] == null) continue; 
				Console.WriteLine("looping -- sensor key == " + EntryClass.bSim.Boids[i].EntityKey);
				if (EntryClass.bSim.Boids[i].EntityKey == sensorKeyForThisBoid)
				{
					found.Add(EntryClass.bSim.Boids[i]);
					System.Diagnostics.Debug.Assert (EntryClass.bSim.Boids[i] == EntryClass.bSim.Boids[numPartOfKeyEYES]);
				}
			}
			
			Console.WriteLine("GetSensors() - Call Complete.  # of Sensors found == " + found.Count.ToString());
			if (found.Count == 0) return null;
			
			return found.ToArray();
		}

		public EntityNode[] GetWeapons(int entityArrayIndex)
		{
			if (EntryClass.bSim.Boids == null) return null;
						
			int numPartOfKey =  entityArrayIndex + BoidSimulation.LASER_OFFSET; 
			string keyForThisBoid = "laser_" + numPartOfKey.ToString();

			System.Diagnostics.Debug.Assert(keyForThisBoid == EntryClass.bSim.Boids[numPartOfKey].EntityKey);
			return new EntityNode[] {EntryClass.bSim.Boids[numPartOfKey]};
		}
		
		public EntityNode[] GetTacticalStations(int entityArrayIndex)
		{
			if (EntryClass.bSim.Boids == null) return null;
			
			int numPartOfKey =  entityArrayIndex + BoidSimulation.TACTICAL_STATION_OFFSET; 
			string keyForThisBoid = "tacticalstation_" + numPartOfKey.ToString();

			System.Diagnostics.Debug.Assert(keyForThisBoid == EntryClass.bSim.Boids[numPartOfKey].EntityKey);
			return new EntityNode[] {EntryClass.bSim.Boids[numPartOfKey]};
			
			// NOTE: the below isn't necessary.  
			// NOTE: previously when this loop was failing it was because the Key I was searching for "sensor_###" 
			//       could NEVER possibly exist because the entityArrayIndex for the associated sensor to a given boid is
			//       always entityArrayIndex + 1.  There is a sensor_111 for example, but never a sensor_110 that is just 1 less.
			//       They always are in increments of 2, same with the boid's indexArrays too.
			List<EntityNode> found = new List<EntityNode>();
			for (int i = 0; i < EntryClass.bSim.Boids.Count; i++)
			{
				if (EntryClass.bSim.Boids[i] == null) continue; 
				Console.WriteLine("looping -- tactical station key == " + EntryClass.bSim.Boids[i].EntityKey);
				if (EntryClass.bSim.Boids[i].EntityKey == keyForThisBoid)
				{
					found.Add(EntryClass.bSim.Boids[i]);
					System.Diagnostics.Debug.Assert (EntryClass.bSim.Boids[i] == EntryClass.bSim.Boids[numPartOfKey]);
				}
			}
			
			Console.WriteLine("GetTacticalStations() - Call Complete.  # of Tactical Stations found == " + found.Count.ToString());
			if (found.Count == 0) return null;
			
			return found.ToArray();
		}
		
		public EntityNode[] GetTacticalStationOperators(int entityArrayIndex)
		{
			if (EntryClass.bSim.Boids == null) return null;
			
			int numPartOfKey =  entityArrayIndex + BoidSimulation.HUMAN_OPERATOR_OFFSET; 
			string keyForThisBoid = "human_operator_" + numPartOfKey.ToString();

			System.Diagnostics.Debug.Assert(keyForThisBoid == EntryClass.bSim.Boids[numPartOfKey].EntityKey);
			return new EntityNode[] {EntryClass.bSim.Boids[numPartOfKey]};
		}
		
	#endregion
			
		public Tuple<Boid, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode> Spawn(Random rand, int arrayIndex, double width, double height, double depth)
		{
			Tuple<Boid, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode> result;
				
			// TODO: TEMP HACK - THE JSON Serialize and Deserialize of a PropertySpec[] DOES WORK!  
			//Builder builder = new Builder();
			//builder.ToString();
			//Environment.Exit(0);
			
			//Console.WriteLine ("Spawn() - Boid Spawn BEGIN at array index == " + arrayIndex.ToString());
			string exLine = "Spawn 0";
						
			double posX = rand.NextDouble() * width;
            double posY = rand.NextDouble() * height;
            double posZ= rand.NextDouble() * depth;
            
            double vX = (rand.NextDouble() - 0.5d) * 2d;
            double vY = (rand.NextDouble() - 0.5d) * 2d;

			string entityKey = "boid_" + arrayIndex.ToString(); // prefix with "boid_" to not duplicate with "sensor_"
			
            Boid b = null;
			try
			{
				b = new Boid(entityKey, arrayIndex, posX, posY, posZ, vX, vY);
				b.Configuration = (uint)BoidConfiguration;
				// NOTE: since each Droid will have an "Operator" and "TacticalStation" merged into it's blackboarddata,
				//       all we really need to do is stick to a naming convention like "operator_#####"  and "tactical_#####" 
				//       when adding those Keys.
				
				// todo: generate Droids with some variance for age, size, and speed

				string factionColor = "Red";
				factionColor = (rand.NextDouble() >= 0.5d) ? "Red" : "Blue";
				b.BlackBoardData.SetString("faction", factionColor);

				//EntryClass.mUserDataStore[entityKey].SetString("faction", factionColor);
				System.Diagnostics.Debug.Assert(b.BlackBoardData == EntryClass.mUserDataStore[entityKey], "Spawn() -- UserData objects do not match.");
			}
			catch (Exception ex)
			{
				Console.WriteLine (exLine + " " + ex.Message);
			}
			
			
					
			// todo: create a "cooldown" interval that is based on the droid's size	
	
			// TODO: Add to Spawn()
			//
			// OnEntityAttached(EntityNode e)
			//       {
			
			
			
			
			// TIMERS
			////////////////////////

			exLine = "Spawn 2";
			try
			{
				mIntervalTimers.Register(entityKey, "droid_spawn", 0.14d);
			}
			catch (Exception ex)
			{
				Console.WriteLine(exLine + " " + ex.Message);
			}
			
			
			// private const CONFIGURATION BoidConfiguration = CONFIGURATION.Transform | CONFIGURATION.RigidBody | CONFIGURATION.Sentient | CONFIGURATION.SelfPropelled;
			
			// NOTE: Do NOT use the allTransforms method below
			//ComponentStore<Transform.Transform_Struct> storeTransform = EntryClass.mCStoreCol.CheckOut<Transform.Transform_Struct>(EntryClass.NUM_ENTRIES); 
            //int transformIndex = -1;
			//Memory<Transform.Transform_Struct> memAllTransforms = storeTransform.CheckOut(out transformIndex);
			//memAllTransforms.Span[transformIndex].Configuration = BoidConfiguration;
			// b.AddUserStruct(typeof(Transform.Transform_Struct), memAllTransforms, transformIndex);
			
			////////////////////////////////////////////////////////////////////////////////////////////////////
			// TRANSFORM STRUCT - NOTE: We do not need to b.AddUserStruct() because the Transform_Struct is added by default by 'class Transform'
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)b.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = BoidConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026		
		
			// LIFE FORM
			ComponentStore<LifeForm> storeLivingEntity = EntryClass.mCStoreCol.CheckOut<LifeForm>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
            int livingEntityID = -1;
            Memory<LifeForm> memLivingEnt = storeLivingEntity.CheckOut(out livingEntityID);
			b.AddUserStruct(typeof(LifeForm), memLivingEnt, livingEntityID);
			
			storeLivingEntity.Span[livingEntityID].Age = 1;
			storeLivingEntity.Span[livingEntityID].HitPoints = new HitPoints(){ Base = 250, Current = 250};
			storeLivingEntity.Span[livingEntityID].Configuration = BoidConfiguration;
			
			
			// ARMOR: this may require an array of checkOutIndices based on how many layers as determined from 
			//       component.ArmorLayersCount
			ComponentStore<ArmorLayer> storeArmorLayers = EntryClass.mCStoreCol.CheckOut<ArmorLayer>(EntryClass.NUM_ENTRIES); 
            int checkOutIndex = -1;
            Memory<ArmorLayer> memArmor = storeArmorLayers.CheckOut(out checkOutIndex);
			b.AddUserStruct(typeof(Armor), memArmor, checkOutIndex);
			
			
			////////////////////////////////////////////////////////////////////////////////////////////////////
			// EYES
			exLine = "Spawn() - CreateOpticalSensors 1";
			EntityNode eyes = null;
			try
			{
				eyes = CreateOpticalSensor(arrayIndex + OPTICAL_SENSOR_OFFSET);
			}
			catch (Exception ex)
			{
				Console.WriteLine(exLine + " " + ex.Message);
			}
						
			////////////////////////////////////////////////////////////////////////////////////////////////////
			// WINGS need power to fly
			EntityNode wings = CreateWings(arrayIndex  + WINGS_OFFSET);
			
			// ////////////////////////////////////////////////////////////////////////////////////////////////////
			// Laser
			EntityNode laser = CreateLaser(arrayIndex + LASER_OFFSET);
			
			
			////////////////////////////////////////////////////////////////////////////////////////////////////
			// TACTICAL STATION
			EntityNode tacticalStation = CreateTacticalStation(arrayIndex + TACTICAL_STATION_OFFSET);
						
	
			//			AddProduction(e)
			//	        AddConsumption(e);
			//       }
			
			// TODO: finish creating the optical sensors for our Droids
			// TODO: I think we need to remove all struct creation from within Boid or EntityNode because we are unable
			//       to manage the Index values properly that way.

			
			
			////////////////////////////////////////////////////////////////////////////////////////////////////
			// BATTERY to power Eyes, Wings, Laser and TacticalStation
			EntityNode battery = CreateBattery(arrayIndex + BATTERY_OFFSET);
			
			////////////////////////////////////////////////////////////////////////////////////////////////////
			// HUMAN OPERATOR for the tactical station
			EntityNode humanOperator = CreateHumanOperator(arrayIndex + HUMAN_OPERATOR_OFFSET);
			
						
			Vector3d pos = new Vector3d(posX, posY, posZ);
			BoundingBox box = new BoundingBox (pos, 1);
			
			eyes.Translation = pos;
			eyes.BoundingBox = box; // HACK -direct BoundingBox assignment.  I need a BoundingBox set to insert into Octree since we dont have any geometry to auto compute one for us. 
			eyes.Configuration |= (uint)CONFIGURATION.PowerUsing; // eyes are sensors so use power
			
			wings.Translation = pos;
			wings.BoundingBox = box; // HACK
			wings.Configuration |= (uint)CONFIGURATION.PowerUsing; // wings flap so use power
			
			laser.Translation = pos;
			laser.BoundingBox = box;// HACK
			laser.Configuration |= (uint)CONFIGURATION.PowerUsing; // lasers obviously use power
			
			tacticalStation.Translation = pos;
			tacticalStation.BoundingBox = box;// HACK
			tacticalStation.Configuration |= (uint)CONFIGURATION.PowerUsing; // stations have fancy computer screens that use power
			
			battery.Translation = pos;
			battery.BoundingBox = box;// HACK
			battery.Configuration = (uint)CONFIGURATION.PowerProducing;
			
			humanOperator.Translation = pos;
			humanOperator.BoundingBox = box;// HACK
			humanOperator.Configuration = (uint)HumanOperatorConfiguration;
			
		    if (this.Octree != null)
            {
           		Octree.Add((EntityNode)b);
				// NOTE: in KGB these Entities would be children of the parent node Boid and then
				//       the Boid would get added to the Octree and then any child entities would get
				//       recursively added to the Octree automatically.
				Octree.Add((EntityNode)eyes);
				Octree.Add((EntityNode)wings);
				Octree.Add((EntityNode)laser);
				Octree.Add((EntityNode)tacticalStation);
				Octree.Add((EntityNode)battery);
				Octree.Add((EntityNode)humanOperator);
            }

			result = 
					new Tuple<Boid, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode, EntityNode>(b, eyes, wings, laser, tacticalStation, battery, humanOperator);
			return result;
		}
		
		// todo: typically creation of structs and production and consumption would be handled in an Entity script - eg eventually for KGB it might be  \\data\\mods\\caesar\\scripts_entities\\sensor_radar.css
		private EntityNode CreateOpticalSensor(int arrayIndex)
		{
			// TODO: the problem we are having with 'index' right now is that every EntityNode
			//       creates a Transform_Struct which is sized initially to EntryClass.NUM_ENTRIES and I do not think
			//       it can handle expansions properly OR when Boid's create the various structs it needs (eg LivingEntity) that then
			//       do not correspond index wise necessarily to their transform struct's spanIndex 
			// SO we need a more robust solution to handling these indices and for finding these index
			// values
			string exLine =  "CreateOpticalSensor 1";
			string entityKey = "sensor_" + arrayIndex.ToString(); // prefix with "sensor_" to not duplicate with "boid_".  It turns out this is technically not necessary because every arrayIndex is always unique... duh!
			EntityNode opticalSensor = new EntityNode(entityKey, arrayIndex, 0, 0, 0, 0, 0); // OpticalSensor is the Droid's 'eyes'
			opticalSensor.Configuration = (uint)OpticalSensorConfiguration;
			
			//CONFIGURATION OpticalSensorConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing | CONFIGURATION.Sensor;
	
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)opticalSensor.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = OpticalSensorConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026

			ComponentStore<Component> storeComp = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int compInternalIndex = -1;
			Memory<Component> memComp = storeComp.CheckOut(out compInternalIndex);
			opticalSensor.AddUserStruct(typeof(Component), memComp, compInternalIndex);

			storeComp.Span[compInternalIndex].Configuration = OpticalSensorConfiguration;
			storeComp.Span[compInternalIndex].EntityArrayIndex = arrayIndex;

			// powerconsumer struct
			ComponentStore<PowerConsumer> storePowerConsumer = EntryClass.mCStoreCol.CheckOut<PowerConsumer>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int powerConsumerInternalIndex = -1;
			Memory<PowerConsumer> memPowerConsumer = storePowerConsumer.CheckOut(out powerConsumerInternalIndex);
			opticalSensor.AddUserStruct(typeof(PowerConsumer), memPowerConsumer, powerConsumerInternalIndex);
			storePowerConsumer.Span[powerConsumerInternalIndex].Configuration = WingsConfiguration;
			storePowerConsumer.Span[powerConsumerInternalIndex].EntityArrayIndex = arrayIndex;
			
			
			int sensorInternalIndex = -1;
			exLine = "CreateOpticalSensor 4";
			try
			{
				ComponentStore<Sensor> storeSensor = EntryClass.mCStoreCol.CheckOut<Sensor>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
				Memory<Sensor> memSensor = storeSensor.CheckOut(out sensorInternalIndex);
				opticalSensor.AddUserStruct(typeof(Sensor), memSensor, sensorInternalIndex);
				
				storeSensor.Span[sensorInternalIndex].Configuration = OpticalSensorConfiguration;
				storeSensor.Span[sensorInternalIndex].EntityArrayIndex = arrayIndex;
				//storeSensor.Span[sensorInternalIndex].InternalComponentIndex = -1; // TODO:  this should be from "Component" struct not sensorInternalIndex;
				exLine = "CreateOpticalSensor 5";
				storeSensor.Span[sensorInternalIndex].RangeSquared = Utils.GetMax(this.SeparationDistance, this.AlignmentDistance, this.CohesionDistance);
				exLine = "CreateOpticalSensor 6";
				storeSensor.Span[sensorInternalIndex].RangeSquared *= storeSensor.Span[sensorInternalIndex].RangeSquared; // square it
				//storeSensor.Span[sensorInternalIndex].ScanRating = 2000; // <-- this is a computed stat based on TL and Power, that generally ranges from 10 - 40+  (google "gurps vehicles 2nd edition radar scan rating")
				exLine = "CreateOpticalSensor 7";
			}
			catch (Exception ex)
			{
				Console.WriteLine(exLine + " " + ex.Message);
			}
			
			// each Droid can Produce a 'PRODUCT.OpticalReflection' 
			Production p;
			p.ProducerEntityArrayIndex = opticalSensor.EntityArrayIndex;
			p.ProducerEntityInternalIndex = opticalSensor.GetUserStructIndex(typeof(Transform.Transform_Struct));
			//p.Consumers = null; <-- same as DistributionList?  what if we only are using a different DistributionMode that requires a search?
			p.ProductID = 	(int)PRODUCTS.OpticalReflection;
			p.Breaker = true;
			
			p.Value = 1;
			p.Store = -1; // this should be diminished by the range of the sensor 
			
			p.StartTime = Utils.NowTicks();
			p.Duration = -1;
			p.NumUses = -1;
			p.CooldownBetweenUses = 0;
			
			// TODO: the distribution list for PRODUCT.OpticalReflection is ignored for now.  We just use
			//       adjacents I think to determine who we will distribute too
			p.DistributionMode = PRODUCT_DISTRIBUTION_TYPE.List;
			p.Consumers = null; //new int[] {checkOutIndex};
			p.SearchReferenceEntity  = null;

						
			// TODO: the distribution list for PRODUCT.OpticalReflection is ignored for now.  We just use
			//       adjacents I think to determine who we will distribute too
			// each Droid can Consume a 'PRODUCT.OpticalReflection' 
			Consumption c;
			c.ConsumerEntityArrayIndex = opticalSensor.EntityArrayIndex;
			c.ConsumerInternalIndex = sensorInternalIndex;
			c.ProductID = (int)PRODUCTS.OpticalReflection;
			c.Breaker = true;
			c.Value =  null;
			c.Amount = 1;
			c.Operations = null;
			
			RegisterProduction(opticalSensor, p);
			RegisterConsumption(opticalSensor, c);
			
			
			// each OpticalSensor CONSUMES PRODUCT.ElectricalPower from our Battery (a Producer)
			c.ConsumerEntityArrayIndex = opticalSensor.EntityArrayIndex;
			c.ConsumerInternalIndex = transformIndex;
			c.ProductID = (int)PRODUCTS.ElectricalPower;
			c.Breaker = true;
			c.Value =  2;  // 10 kW/h
			c.Amount = 1;
			c.Operations = null;
			
			RegisterConsumption(opticalSensor, c);
			
			return opticalSensor;
		}
		
		private EntityNode CreateWings(int arrayIndex)
		{
			string exLine = "CreateWings 1";
			string entityKey = "wings_" + arrayIndex.ToString(); // prefix with "laser_" to not duplicate with "boid_".  It turns out this is technically not necessary because every arrayIndex is always unique... duh!			
			
			EntityNode wings = new EntityNode(entityKey, arrayIndex, 0, 0, 0, 0, 0); 
			wings.Configuration = (uint)WingsConfiguration;
			
			//CONFIGURATION WingsConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing; // <- CONFIGURATION.Propulsion
			
			// transform struct
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)wings.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = WingsConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026

			// component struct
			ComponentStore<Component> storeComp = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int compInternalIndex = -1;
			Memory<Component> memComp = storeComp.CheckOut(out compInternalIndex);
			wings.AddUserStruct(typeof(Component), memComp, compInternalIndex);
			storeComp.Span[compInternalIndex].Configuration = WingsConfiguration;
			storeComp.Span[compInternalIndex].EntityArrayIndex = arrayIndex;

			// powerconsumer struct
			ComponentStore<PowerConsumer> storeWings = EntryClass.mCStoreCol.CheckOut<PowerConsumer>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int powerConsumerInternalIndex = -1;
			Memory<PowerConsumer> memWings = storeWings.CheckOut(out powerConsumerInternalIndex);
			wings.AddUserStruct(typeof(PowerConsumer), memWings, powerConsumerInternalIndex);
			storeWings.Span[powerConsumerInternalIndex].Configuration = WingsConfiguration;
			storeWings.Span[powerConsumerInternalIndex].EntityArrayIndex = arrayIndex;
			
			// each Wing CONSUMES PRODUCT.ElectricalPower from our Battery (a Producer)
			Consumption c;
			c.ConsumerEntityArrayIndex = wings.EntityArrayIndex;
			c.ConsumerInternalIndex = transformIndex;
			c.ProductID = (int)PRODUCTS.ElectricalPower;
			c.Breaker = true;
			c.Value =  5;  // 5 kW/h
			c.Amount = 1;
			c.Operations = null;
			
			RegisterConsumption(wings, c);
			
			return wings;
		}
		
		
		private EntityNode CreateLaser(int arrayIndex)
		{
			string exLine = "CreateLaser 1";
			string entityKey = "laser_" + arrayIndex.ToString(); // prefix with "laser_" to not duplicate with "boid_".  It turns out this is technically not necessary because every arrayIndex is always unique... duh!			
			
			EntityNode laser = new EntityNode(entityKey, arrayIndex, 0, 0, 0, 0, 0); 
			laser.Configuration = (uint)LaserConfiguration;
			
			mIntervalTimers.Register(entityKey, "droid_canfire", 0.00d);
			mIntervalTimers.Register(entityKey, "droid_isfiring", 0.06d);
						
			//CONFIGURATION LaserConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing | CONFIGURATION.Weapon | CONFIGURATION.Laser;
			
			// transform struct
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)laser.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = LaserConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026
			
			// component struct
			ComponentStore<Component> storeComp = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
            int checkOutIndex = -1;
            Memory<Component> memCmp = storeComp.CheckOut(out checkOutIndex);
			laser.AddUserStruct(typeof(Component), memCmp, checkOutIndex);
			storeComp.Span[checkOutIndex].Configuration = LaserConfiguration;
			storeComp.Span[checkOutIndex].EntityArrayIndex = laser.EntityArrayIndex;
			storeComp.Span[checkOutIndex].Level = 1;
			//storeComp.Span[checkOutIndex].Quality = 1.0f;  // a coefficient with 1.0f being finely crafted and 0.0 being barely MacGuyvered together and may only last one shot
			storeComp.Span[checkOutIndex].Ruggedized = true;
			//storeComp.Span[checkOutIndex].HitPoints = 100;
			//storeComp.Span[checkOutIndex].DR = 20;  // todo: if we use complex armor, is DR (damage resistance) used?
			//storeComp.Span[checkOutIndex].Cost = 10d;
			//storeComp.Span[checkOutIndex].Weight = 2.5d;
			//storeComp.Span[checkOutIndex].SurfaceArea = 1d;
			//storeComp.Span[checkOutIndex].Volume = 0.2d;

			// powerconsumer struct
			ComponentStore<PowerConsumer> storePowerConsumer = EntryClass.mCStoreCol.CheckOut<PowerConsumer>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int powerConsumerInternalIndex = -1;
			Memory<PowerConsumer> memPowerConsumer = storePowerConsumer.CheckOut(out powerConsumerInternalIndex);
			laser.AddUserStruct(typeof(PowerConsumer), memPowerConsumer, powerConsumerInternalIndex);
			storePowerConsumer.Span[powerConsumerInternalIndex].Configuration = WingsConfiguration;
			storePowerConsumer.Span[powerConsumerInternalIndex].EntityArrayIndex = arrayIndex;
			
			// weapon struct
			ComponentStore<Weapon> storeWeapon = EntryClass.mCStoreCol.CheckOut<Weapon>(EntryClass.NUM_ENTRIES);
            checkOutIndex = -1;
            Memory<Weapon> memWep = storeWeapon.CheckOut(out checkOutIndex);
			laser.AddUserStruct(typeof(Weapon), memWep, checkOutIndex);	
			storeWeapon.Span[checkOutIndex].EntityArrayIndex = laser.EntityArrayIndex;
			storeWeapon.Span[checkOutIndex].Configuration = LaserConfiguration;
			storeWeapon.Span[checkOutIndex].Reliable = true;
			storeWeapon.Span[checkOutIndex].Compact = true;
			
			storeWeapon.Span[checkOutIndex].Accuracy = 10;
			storeWeapon.Span[checkOutIndex].SnapShot = 2;
			
			storeWeapon.Span[checkOutIndex].Malfunction = 0.2f; // todo: this may be not needed if Malfunction is calculated at runtime to include Damage to the weapon when firing) // range 0.0 - 1.0, Malfunction with 1.0 being maximum meaning it would malfunction every time and 0.0f never.

			
			storeWeapon.Span[checkOutIndex].NumShots = 3;  // todo: Needs to be like HitPoints with .Base and .Current   eg. how many bullets are in a magazine 
			storeWeapon.Span[checkOutIndex].ReloadCoolDown = 3.0f; //
			storeWeapon.Span[checkOutIndex].CoolDown = 0.3f; // This is Rate-of-Fire.  This is the cooldown between when this weapon can be fired again.  It is RoF and perhaps CyclicRate too ultimately. Any "ANIMATION" of the weapon firing should last less than the time of this cooldown!

			
			// todo: PowerReqt should be part of PowerUsing struct?
//			storeWeapon.Span[checkOutIndex].PowerReqt = 0.0f;


			// TODO: these are like "internal" items and can be used if another power source is no longer connected
//			string PowerCellType;  // TOOD: Need an ENUM
//			int PowerCellQuantity;
//			double PowerCellWeight;
			
			// https://panoptesv.com/RPGs/Equipment/Weapons/BeamWeapons.php?HR=0
//			storeWeapon.Span[checkOutIndex].TypeDamage = DAMAGE_TYPE.Burning;     // TOOD: Need an ENUM
			storeWeapon.Span[checkOutIndex].AverageDamage = 32;       
//			double KEDamage = 3.0d;
			storeWeapon.Span[checkOutIndex].FallOffStart = 25; 
//			double VacuumFallOffStart;
			storeWeapon.Span[checkOutIndex].Range = 10;   
			storeWeapon.Span[checkOutIndex].RangeSquared = storeWeapon.Span[checkOutIndex].Range * storeWeapon.Span[checkOutIndex].Range;
//			public double MaxRange2;
//			public double VacuumMaxRange;
//			public double VacuumMaxRange2;
    

			
			// Laser struct
			ComponentStore<Laser_Struct> storeLasers = EntryClass.mCStoreCol.CheckOut<Laser_Struct>(EntryClass.NUM_ENTRIES); 
            checkOutIndex = -1;
            Memory<Laser_Struct>memLaser = storeLasers.CheckOut(out checkOutIndex);
            laser.AddUserStruct(typeof(Laser_Struct), memLaser, checkOutIndex);
			storeLasers.Span[checkOutIndex].EntityArrayIndex = laser.EntityArrayIndex;
			storeLasers.Span[checkOutIndex].Configuration = LaserConfiguration;
			storeLasers.Span[checkOutIndex].Type = 1;     
			storeLasers.Span[checkOutIndex].EnergyDrill = false;
			storeLasers.Span[checkOutIndex].FTL = true;
			storeLasers.Span[checkOutIndex].BeamOutput = 10f; // kW
			storeLasers.Span[checkOutIndex].CyclicRate = 1;
					
			// each Laser CONSUMES PRODUCT.ElectricalPower from our Battery (a Producer)
			Consumption c;
			c.ConsumerEntityArrayIndex = laser.EntityArrayIndex;
			c.ConsumerInternalIndex = transformIndex;
			c.ProductID = (int)PRODUCTS.ElectricalPower;
			c.Breaker = true;
			c.Value =  25;  // 10 kW/h
			c.Amount = 1;
			c.Operations = null;
			
			RegisterConsumption(laser, c);
			
			return laser;
		}
		
		private EntityNode CreateTacticalStation(int arrayIndex)
		{
			string exLine = "CreateStation 1";
			string entityKey = "tacticalstation_" + arrayIndex.ToString(); // prefix with "laser_" to not duplicate with "boid_".  It turns out this is technically not necessary because every arrayIndex is always unique... duh!			
			
			EntityNode station = new EntityNode(entityKey, arrayIndex, 0, 0, 0, 0, 0); 
			station.Configuration = (uint)TacticalStationConfiguration;
			
			//CONFIGURATION TacticalStationConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerUsing | CONFIGURATION.TacticalStation;

			// transform struct
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)station.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = TacticalStationConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026
			
			// component struct
			ComponentStore<Component> storeComp = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int compInternalIndex = -1;
			Memory<Component> memComp = storeComp.CheckOut(out compInternalIndex);
			station.AddUserStruct(typeof(Component), memComp, compInternalIndex);
			storeComp.Span[compInternalIndex].Configuration = TacticalStationConfiguration;
			storeComp.Span[compInternalIndex].EntityArrayIndex = arrayIndex;

		
			// powerconsumer struct
			ComponentStore<PowerConsumer> storePowerConsumer = EntryClass.mCStoreCol.CheckOut<PowerConsumer>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int powerConsumerInternalIndex = -1;
			Memory<PowerConsumer> memWings = storePowerConsumer.CheckOut(out powerConsumerInternalIndex);
			station.AddUserStruct(typeof(PowerConsumer), memWings, powerConsumerInternalIndex);
			storePowerConsumer.Span[powerConsumerInternalIndex].Configuration = TacticalStationConfiguration;
			storePowerConsumer.Span[powerConsumerInternalIndex].EntityArrayIndex = arrayIndex;
			
			
			// tactical station
			ComponentStore<TacticalStation> storeTacticalStation = EntryClass.mCStoreCol.CheckOut<TacticalStation>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
            int checkOutIndex = -1;
            Memory<TacticalStation> memTact = storeTacticalStation.CheckOut(out checkOutIndex);
			station.AddUserStruct(typeof(TacticalStation), memTact, checkOutIndex);

			storeTacticalStation.Span[checkOutIndex].Configuration = TacticalStationConfiguration;
			storeTacticalStation.Span[checkOutIndex].EntityArrayIndex = arrayIndex; // we use EntityArrayIndex and not SpanIndex because we want to use it to find the Boid element in the EntryClass.bSim.Boids[index] List
			storeTacticalStation.Span[checkOutIndex].HistoryCount = 1;
			storeTacticalStation.Span[checkOutIndex].CooldownBetweenActions = 3.0f;
			storeTacticalStation.Span[checkOutIndex].MaxActions = 2;
			storeTacticalStation.Span[checkOutIndex].NumActions = 0;
			storeTacticalStation.Span[checkOutIndex].Actions = null;
			storeTacticalStation.Span[checkOutIndex].Contacts = null;
			storeTacticalStation.Span[checkOutIndex].ContactsHistory = null;
			storeTacticalStation.Span[checkOutIndex].Targets = null;

			// add a targetingSkill requirement to this TacticalStation
			Skill targetingSkill;
			targetingSkill.SkillType = SKILLS.Targeting;
			targetingSkill.Level = 2;     			// the level of this skill
			targetingSkill.Production = null;
			//targetingSkill.Modifiers = null;
			targetingSkill.BaseValue = 2;
			targetingSkill.EffectiveValue = 0; // todo: this should be a Getter perhaps and not a public variable
			// add the modifier(s) to this skill.  Recall that modifiers behave just like any other type of PRODUCTION and must be registered as PRODUCTION 
			// at the appropriate time (eg On USE of the Skill, or on EQUIP of an Item, etc.)
			
			// NOTE: This station will be CONSUMING TargetingSkilLModifer and NOT producing any.  The operator will be PRODUCING
			//targetingSkill.AddProduction(livingEntityID, PRODUCTS.TargetingSkillModifier, 1, true, -1);

			// TODO: This MUST go to the TacticalStation, NOT HERE
			// add the skill to the DROID as if it was being added to a CREW STATION which for HelloBoids.cs we are not modeling for now... but KGB and SciFiCommand does.
			station.Skills.Add(targetingSkill.SkillType, targetingSkill);
			
			// each Station CONSUMES PRODUCT.ElectricalPower from our Batter (a Producer)
			Consumption c;
			c.ConsumerEntityArrayIndex = station.EntityArrayIndex;
			c.ConsumerInternalIndex = transformIndex;
			c.ProductID = (int)PRODUCTS.ElectricalPower;
			c.Breaker = true;
			c.Value =  1;   
			c.Amount = 10; // 10 kW/h
			c.Operations = null;
			
			RegisterConsumption(station, c);
			
			
			// each Station can Consume a TargetingSkillModifier as if it had a TACTICAL CREW STATION from an Operator
			c.ConsumerEntityArrayIndex = station.EntityArrayIndex;
			c.ConsumerInternalIndex = transformIndex;
			c.ProductID = (int)PRODUCTS.TargetingSkillModifier;
			c.Breaker = true;   // for SkillModifiers, this is just whether the Skill Modifier is currently enabled or not.
			c.Value =  null;
			c.Amount = 1;
			c.Operations = null;

			
			RegisterConsumption(station, c);
			
			return station;
		}
		
		private EntityNode CreateBattery (int arrayIndex)
		{
			string exLine = "CreateBattery 1";
			string entityKey = "battery_" + arrayIndex.ToString(); // prefix with "laser_" to not duplicate with "boid_".  It turns out this is technically not necessary because every arrayIndex is always unique... duh!			
		
			EntityNode battery = new EntityNode(entityKey, arrayIndex, 0, 0, 0, 0, 0); 
			battery.Configuration = (uint)BatteryConfiguration;
			
			//CONFIGURATION BatteryConfiguration = CONFIGURATION.Transform | CONFIGURATION.Component | CONFIGURATION.PowerProducer;
			
			// transform struct
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)battery.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = BatteryConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026

			// component struct
			ComponentStore<Component> storeComp = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			int compInternalIndex = -1;
			Memory<Component> memComp = storeComp.CheckOut(out compInternalIndex);
			battery.AddUserStruct(typeof(Component), memComp, compInternalIndex);
			storeComp.Span[compInternalIndex].Configuration = BatteryConfiguration;
			storeComp.Span[compInternalIndex].EntityArrayIndex = arrayIndex;

			storeComp.Span[compInternalIndex].Configuration = BatteryConfiguration;
			storeComp.Span[compInternalIndex].EntityArrayIndex = battery.EntityArrayIndex;
			storeComp.Span[compInternalIndex].Level = 1;
			//storeComp.Span[compInternalIndex].Quality = 1.0f;  // a coefficient with 1.0f being finely crafted and 0.0 being barely MacGuyvered together and may only last one shot
			storeComp.Span[compInternalIndex].Ruggedized = true;
			//storeComp.Span[compInternalIndex].HitPoints = 100;
			//storeComp.Span[compInternalIndex].DR = 20;  // todo: if we use complex armor, is DR (damage resistance) used?
			//storeComp.Span[compInternalIndex].Cost = 10d;
			//storeComp.Span[compInternalIndex].Weight = 2.5d;
			//storeComp.Span[compInternalIndex].SurfaceArea = 1d;
			//storeComp.Span[compInternalIndex].Volume = 0.2d;
			
			
			// powerProducer struct
			double BATTERY_POWER_STARTING_AMOUNT = 1000d;
			ComponentStore<PowerProducer> storePowerProducers = EntryClass.mCStoreCol.CheckOut<PowerProducer>(EntryClass.NUM_ENTRIES); 
			int powerProducerInternalIndex = -1;
			Memory<PowerProducer> memPowerProducer = storePowerProducers.CheckOut(out powerProducerInternalIndex);
			battery.AddUserStruct(typeof(PowerProducer), memPowerProducer, powerProducerInternalIndex);
			storePowerProducers.Span[powerProducerInternalIndex].Configuration = BatteryConfiguration;
			storePowerProducers.Span[powerProducerInternalIndex].EntityArrayIndex = arrayIndex;
			// todo: the .Store amount must be computed from it's Capacity and Output or Output and Duration
			//       but for now just hardcode the value with a constant
			storePowerProducers.Span[powerProducerInternalIndex].Output = 0;
			storePowerProducers.Span[powerProducerInternalIndex].Capacity = BATTERY_POWER_STARTING_AMOUNT;
			storePowerProducers.Span[powerProducerInternalIndex].Duration = 120f; // seconds
			storePowerProducers.Span[powerProducerInternalIndex].Store = BATTERY_POWER_STARTING_AMOUNT;
			storePowerProducers.Span[powerProducerInternalIndex].Breaker = true;
			
			
			string buildScriptRelativePath = "\\scripts_build\\battery_builder.css";
			Builder build = new Builder(buildScriptRelativePath);
			build.Calculate(battery);
	
			
			
			
			// each Battery can Produce a PRODUCTS.ElectricalPower
			Production p;
			p.ProducerEntityArrayIndex = battery.EntityArrayIndex;
			p.ProducerEntityInternalIndex = powerProducerInternalIndex;
			p.ProductID = 	(int)PRODUCTS.ElectricalPower;
			p.Breaker = true;
			
			p.Value = 1;  // the UNIT value... in this case it's a DOUBLE
			p.Store = 0; // a Battery can produce as much as it can discharge supply all of it's Consumers until it runs out of Energy  
			
			p.StartTime =  -1; // Utils.NowTicks();
			p.Duration = -1;
			p.NumUses = -1; // This Battery can be used until it's Capacity = 0
			p.CooldownBetweenUses = -1;
			
			// TODO: the distribution list for PRODUCT.OpticalReflection is ignored for now.  We just use
			//       adjacents I think to determine who we will distribute too
			p.DistributionMode = PRODUCT_DISTRIBUTION_TYPE.BoundingBox;
			p.Consumers = null; //new int[] {checkOutIndex}; <-- see a few lines down where we add wingsConsumptionListIndex, eyesConsumptionListIndex, laserConsumptionListIndex, tacticalConsumptionListIndex, 
			p.SearchReferenceEntity = battery; // TODO: we should do a test to see if storing a reference is better than just an index into the Boids[] List<>
			//Console.WriteLine ("CreateBattery() - SearchReferenceEntity is SET AND VALID == " + (p.SearchReferenceEntity!= null).ToString());

			// Wings, Eyes, Lasers, TacticalStation all CONSUME ElectricalPower
			// TODO: these indices should probably be indices into PowerConsumer struct, NOT EntityArrayIndex into List<Boids>
			int boidArrayIndex = arrayIndex - BATTERY_OFFSET;
			int wingsArrayIndex = boidArrayIndex + WINGS_OFFSET;
			int eyesArrayIndex = boidArrayIndex + OPTICAL_SENSOR_OFFSET;
			int laserArrayIndex = boidArrayIndex + LASER_OFFSET;
			int tacticalArrayIndex = boidArrayIndex + TACTICAL_STATION_OFFSET;
			
			int wingsConsumptionListIndex = GetConsumerIndex (p.ProductID, wingsArrayIndex);
			int eyesConsumptionListIndex = GetConsumerIndex (p.ProductID, eyesArrayIndex);
			int laserConsumptionListIndex = GetConsumerIndex (p.ProductID, laserArrayIndex);
			int tacticalConsumptionListIndex = GetConsumerIndex (p.ProductID, tacticalArrayIndex);
			
			// NOTE: when Entities are added/removed from the Simulation at runtime, these indices may change IF we try to do any
			//       type of management that packs the Memory<T> to not have "empty" or "disabled" records strewn throughout 
			//       and that results in new indices being given to some existing records when those records are moved to fill in
			//       the spots that have been "removed."  THUS, if we do allow that, these Distribution Lists will constantly need to be
			//       updated in mProduction.
			//       There's another problem as well... the ConsumptionListIndex is not easily available during Ship EngineeringStation's manual changing of 
			//       a distributionList.  The DISPLAY would simply need to do a conversion of the List<Consumption> index to the EntityArrayIndex and vice-versa
			p.Consumers = new int[] {wingsConsumptionListIndex, eyesConsumptionListIndex, laserConsumptionListIndex, tacticalConsumptionListIndex};
			
				
			RegisterProduction(battery, p);
			
			return battery;
		}
		
		private EntityNode CreateHumanOperator(int arrayIndex)
		{
			string exLine = "CreateHumanOperator 1";
			string entityKey = "human_operator_" + arrayIndex.ToString(); // prefix with "laser_" to not duplicate with "boid_".  It turns out this is technically not necessary because every arrayIndex is always unique... duh!			
			
			EntityNode humanOperator = new EntityNode(entityKey, arrayIndex, 0, 0, 0, 0, 0); 
			humanOperator.Configuration = (uint)HumanOperatorConfiguration;
			
			int transformIndex;
			Memory<Transform.Transform_Struct> transform = (Memory<Transform.Transform_Struct>)humanOperator.GetUserStruct(typeof(Transform.Transform_Struct), out transformIndex); 
			transform.Span[0].Configuration = HumanOperatorConfiguration; //<-- critical to set this.  I dont like this design where forgtting such things is possible.  March.31.2026
			transform.Span[0].EntityArrayIndex = arrayIndex; // <--  critical to set this.  I dont like this design where forgetting such things is possible. March.31.2026		
		
			// LIVING ENTITY
			ComponentStore<LifeForm> storeLivingEntity = EntryClass.mCStoreCol.CheckOut<LifeForm>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Component>(EntryClass.NUM_ENTRIES);
            int lfID = -1;
            Memory<LifeForm> memLivingEnt = storeLivingEntity.CheckOut(out lfID);
			humanOperator.AddUserStruct(typeof(LifeForm), memLivingEnt, lfID);
			
			storeLivingEntity.Span[lfID].Age = 1;
			storeLivingEntity.Span[lfID].HitPoints =  new HitPoints(){ Base = 100, Current = 100};
			storeLivingEntity.Span[lfID].Configuration = HumanOperatorConfiguration;
			
			BoundingBox box = new BoundingBox (Vector3d.Zero(), 1);
			storeLivingEntity.Span[lfID].Armor = new Armor(box);
					
			// Armor
			//		
			//Console.WriteLine ("CREATING DEFENSE == " + memLivingEnt.Span[0].Armor.Defense.ToString());
			
			Skill targetingSkill;
			targetingSkill.SkillType = SKILLS.Targeting;
			targetingSkill.Level = 3;     			// the level of this skill
			targetingSkill.Production = null;
			//targetingSkill.Modifiers = null;
			targetingSkill.BaseValue = 1;
			targetingSkill.EffectiveValue = 0;
			
			// add the modifier(s) to this skill.  Recall that modifiers behave just like any other type of PRODUCTION and must be registered as PRODUCTION 
			// at the appropriate time (eg On USE of the Skill, or on EQUIP of an Item, etc.)
			targetingSkill.AddProduction(lfID, PRODUCTS.TargetingSkillModifier, 1, true, -1);


			// add the skill to the DROID as if it was being added to an OPERATOR for a CREW STATION which for HelloBoids.cs we are not modeling for now... but KGB and SciFiCommand does.
			humanOperator.Skills.Add(targetingSkill.SkillType, targetingSkill);
			
			// each Operator can Produce a TargetingSkillModifier
			Production p;
			p.ProducerEntityArrayIndex = humanOperator.EntityArrayIndex;
			p.ProducerEntityInternalIndex = lfID;
			p.ProductID = 	(int)targetingSkill.Production[0].Product;  // TargetingSkillModifier
			p.Breaker = true;
			
			p.Value = targetingSkill.Production[0];  // ??
			p.Store = targetingSkill.Production[0].Amount; // ??
			
			p.StartTime = Utils.NowTicks();
			p.Duration = -1;
			p.NumUses = -1; // The targetingSkillModifier is used as long as the Operator remains (note: if operator levels up, this modifier should change too yes?)
			p.CooldownBetweenUses = 0;
			
			p.DistributionMode = PRODUCT_DISTRIBUTION_TYPE.List;

			p.SearchReferenceEntity  = null;
						
			int stationArrayIndex = arrayIndex - HUMAN_OPERATOR_OFFSET + TACTICAL_STATION_OFFSET;
			int stationConsumerListIndex = GetConsumerIndex (p.ProductID, stationArrayIndex);
			
			p.Consumers = new int[] {stationConsumerListIndex};
			
			
			RegisterProduction(humanOperator, p);
					
			return humanOperator;
		}
		
		public Armor CreateArmor(BoundingBox bbox, uint numFaces = 6, uint numLayers = 1)
		{
			Armor result = new Armor (bbox, numFaces, numLayers);

			return result;
		}
	
		
		
		private int GetConsumerIndex (int productID, int entityArrayIndex)
		{
			//List<Consumption> consumption = mConsumption[productID];
			ComponentStore<Consumption> consumption = mConsumption[productID];
			if (consumption == null || consumption.Count == 0) return -1;

			Predicate<Consumption> match = c => c.ConsumerEntityArrayIndex == entityArrayIndex ;
			
			return consumption.FindIndex(match);
		}
		
		private void Destroy(EntityNode entity)
		{
			int lastIndex = this.Boids.Count - 1;
	
			// TODO:
			// OnEntityDetached(EntityNode e)
			//       {
			//			RemoveProduction(e)
			//	        RemoveConsumption(e);
			//       }

			// remove from Octree
			this.Octree.OnEntityNode_Removed(entity);
			
			// remove from Boids[] list
			// TODO: do we need to update all the indices to keep our Memory<T> packed?
			//       one method is to always move the last indexed entity into the slot where 
			//       an Entity was removed, update its entity.Index, and then change the count 
			//       of the Memory<T> store to previousCount - 1;
			// TODO: we need to release all Memory<T> used by Transform_Struct and Living_Entity structs.
		#if MEMORY_T
			this.Boids[entity.EntityArayIndex].Dispose(); // 	<-- store.CheckIn(Boids[i].mMemStore_LivingEntity); occurs here correct?
		#endif
			this.Boids[entity.EntityArrayIndex] = null;
			this.Boids[entity.EntityArrayIndex] = this.Boids[lastIndex];
			this.Boids[entity.EntityArrayIndex].EntityArrayIndex = lastIndex;
	
			this.Boids.RemoveAt(lastIndex); // todo: this wont result in a List copy to a new List will it?

#if MEMORY_T
			Console.WriteLine("Destroy() == Completed on index " + entity.SpanIndexLE.ToString());
#endif
		}

        /// <summary>
        /// Update simulation using either Data Oriented Technique or Object Oriented Technique
		/// </summary>
        public void Update(GameTime gt)
        {
			double elapsedSeconds = gt.ElapsedSeconds; 
            mIntervalTimers.Update(elapsedSeconds);

			int linePos = 0;
			
			//Console.WriteLine("Update() - ELAPSED SECONDS == " + gt.TotalElapsedSeconds.ToString());
			
			try
			{
				// TODO: I should probably just add a setting for whether we are doing CLASSES or MEMORYT so that
				//       we can run both Tests in one run.
	#if USE_MEMORY_T == false
				// TEST CLASSES (Object Oriented Technique)
				// =====================
				this.UpdateClasses(gt); 
				
				/*
								   this.Boids,
								   this.SeparationDistance,
								   this.SeparationFactor,
								   this.AlignmentDistance,
								   this.AlignmentFactor,
								   this.CohesionDistance,
								   this.CohesionFactor,
								   this.MaxSpeed,
								   this.TurnFactor);
				*/
	#else

				// TEST MEMORY<T> (Data Oriented Technique)
				// ====================
				
				try
				{
					// CLEAR
									
					mDamageSystem.Clear();
					mDamageOverTimeSystem.Clear();
					
					
					// production that occurs every frame
					UpdateProduction(gt);
					linePos = 1;
					// NOTE: mLimitedProduction may not be necessary as we now track the NumUses for any given Production and if
			//       p.NumUses == 0, then we remove that production at the end of UpdateProduction();
					// unlike normal production, limited production only occurs for .NumUses which typically is just 1 use
					// that would occur for example when an x-ray laser is fired and XRAYS are produced just for the initial 
					// impact of the laser against the target as opposed to occuring every frame indefinetely.
					//UpdateLimitedProduction(gt);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Update() - UpdateProduction() - " + ex.Message);
				}
				

				
				try
				{
					// NOTE: ProcessOpticalSensors() is added as a mDataProcessor which means our mNeighbors<> Dictionary
					//       will not be initialized during this first call to Do_Tactical_Logic()!
					Do_Tactical_Logic (Seeds.Master, elapsedSeconds, gt);
					linePos = 2;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Update() - Do_Droid_Logic() " + ex.Message);
				}
				
				ComponentStore<LifeForm> livingEntityStore = null;
				livingEntityStore = EntryClass.mCStoreCol.CheckOut<LifeForm>(0);
				
				//  modifications before damage?  I think this is probably the way to
				try
				{
					mSkillModificationSystem.Process(livingEntityStore, null, Seeds.Master, gt);
					linePos = 3;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Update() - Skill Modification System " + ex.Message);
				}
				
				//Console.WriteLine("Update() - Preparing to Update Damage System ");
				try
				{
					mDamageSystem.Process(livingEntityStore, null, Seeds.Master, gt);
					linePos = 4;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Update() - Damage System " + ex.Message);
				}
				
				try
				{
					mDamageOverTimeSystem.Process(livingEntityStore, null, Seeds.Master, gt);
					linePos = 5;
				}
				catch (Exception ex)
				{
					Console.WriteLine("Update() - Damage Over Time System " + ex.Message);
				}
				
								
				try
				{
					
					// LIFECYCLE
					// OPTICAL_SENSING <- creation of mNeighbors<> adjacency 
					// FLOCKING
					mDataProcessor.Update(gt, Boids.ToArray());
					linePos = 6;
					// mProductionProcessor.Update(gt, Boids.ToArray());
					// POWER_PRODUCTION
					// POWER_CONSUMPTION
					
					
				}
				catch (Exception ex)
				{
					Console.WriteLine("Update() - mDataProcessor.Update() - LINE #=" + linePos.ToString() + " " + ex.Message);
				}				
	#endif
			}
			catch (AggregateException ae)
        	{
				foreach (var innerEx in ae.InnerExceptions)
				{
					// Get line number from the stack trace's top frame for the exception with source file information
					// BAH, line numbers wont print when using the online compilers like www.dotnetfiddle.net
					// "Because the .NET Fiddle environment is designed for quick online execution and not full application
					// deployment with debug symbols, you generally have to trace the error by the method names provided 
					// in the stack trace, or by adding your own print statements (Console.WriteLine) to narrow down the 
					// exact location of the error."
					
    				int linenumber = (new StackTrace(innerEx, true)).GetFrame(0).GetFileLineNumber();
					Console.WriteLine(innerEx.TargetSite.ToString());
						
					Console.WriteLine($"Update() - Caught inner exception: {innerEx.Message}" + " Line Number: " + linenumber);
					Console.WriteLine($"Update() - Stack Trace: {innerEx.StackTrace}");
					
					// You can add specific handling logic here based on exception type
					if (innerEx is UnauthorizedAccessException)
					{
						// Handle access denied case
					}
					else if (innerEx is ArgumentException)
					{
						// Handle invalid argument case
					}
				}
			}
			catch (Exception ex)
			{
				// General exception handler (less common with async/await)
				Console.WriteLine($"Update() - A general exception occurred: {ex.Message}");
				//Console.WriteLine(ex.Message.)
			}
		}
		
		/// <summary>
		/// Run the simulation using Update method for data stored in CLASSES  in a typical way, as opposed to Memory<T> based storage which allows us to use (D)ata (O)riented processing of Entities
		/// </summary>
        //public void UpdateClasses(GameTime gt, List<Boid> allBoids, double separationDistance, double separationFactor, double alignmentDistance, double alignmentFactor, 
		//						  double cohesionDistance, double cohesionFactor, double maxSpeed, double turnFactor)
		
		public void UpdateClasses(GameTime gt)
		{
									  
            //////////////////////////////////////////////////////////////////
            // Life Cycle
            //////////////////////////////////////////////////////////////////
			/*
			sting entityKey = currentBoid.EntityKey;
            bool spawnReady = mIntervalTimers.IsReady(entityKey, "droid_spawn");
            if (spawnReady)
            {
				
                //Console.WriteLine("Spawn Ready == " + spawnReady.ToString());
                mIntervalTimers.Reset(entityKey, "droid_spawn");
            }
			*/
			
            //////////////////////////////////////////////////////////////////
            // Flocking
            //////////////////////////////////////////////////////////////////		
			
			int count = Boids.Count;
            System.Threading.Tasks.Parallel.For(0, count, i => 
            //for (int i = 0; i < Boids.Count; i++)
            {
				if (Boids[(int)i] is Boid == false) return;
				
				
                List<int> found;
                List<Boid> neighbors;

				double separationDistance = EntryClass.SEPERATION_DISTANCE;
				double alignmentDistance = EntryClass.ALIGNMENT_DISTANCE;
				double cohesionDistance = EntryClass.COHESION_DISTANCE;

				// more parameters
				double separationFactor = EntryClass.SEPARATION_FACTOR;
				double alignmentFactor = EntryClass.ALIGNMENT_FACTOR;
				double cohesionFactor = EntryClass.COHESION_FACTOR;
				double turnFactor =  EntryClass.TURN_FACTOR; // For boundary avoidance
				double maxSpeed = EntryClass.MAX_SPEED;
				
				double seperatationDistanceSquare = separationDistance * separationDistance;
				double alignmentDistanceSquared = alignmentDistance * alignmentDistance;
				double cohesionDistanceSquared = cohesionDistance * cohesionDistance;
				
				double elapsedSeconds = gt.ElapsedSeconds;
            	double largestDistance = Utils.GetMax(this.SeparationDistance, this.AlignmentDistance, this.CohesionDistance);
            	double largestDistanceSquared = largestDistance * largestDistance;
            
				
                using (EntryClass.CodeProfiler.HookUp("GetNeighbors"))
                {
                    // WARNING: here we pass in entire list of
                    // boids to each boid, which is super slow until we have spatial
                    // partitioning
                    found = GetNeighbors((Boid)Boids[i], largestDistance, largestDistanceSquared);

                    if (found == null || found.Count == 0) 
                   	{ 
					   //Console.WriteLine("UpdateClasses() - Found Count == 0");
						neighbors = null;
					}
                    else
					{
						//Console.WriteLine("UpdateClasses() - Found Count == 0");

						neighbors = new List<Boid>(found.Count);
						for (int j = 0; j < found.Count; j++)
						{
							neighbors.Add((Boid)Boids[found[j]]);
						}
					}
				} // end Using "GetNeighbors"
               

				//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
				// Apply Flocking Rules
				//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                using (EntryClass.CodeProfiler.HookUp("FlockingRules"))
               	{
					// Apply Rules
					//var (sepX, sepY) = Boid.Separate(elapsedSeconds, Boids, Boids[i], separationDistance, separationFactor);
					double sepX = 0d; double sepY = 0d;
					if (neighbors != null)
					 (sepX, sepY) = Boid.Separate(elapsedSeconds, Boids, (Boid)Boids[(int)i], separationDistance, separationFactor, neighbors);

					//var (alignX, alignY) = Boid.Align(elapsedSeconds, Boids, Boids[i], alignmentDistance, alignmentFactor);
					double alignX = 0d; double alignY = 0d;
					if (neighbors != null)
						(alignX, alignY) = Boid.Align(elapsedSeconds, Boids, (Boid)Boids[(int)i], alignmentDistance, alignmentFactor, neighbors);

					//var (cohX, cohY) = Boid.Cohese(elapsedSeconds, Boids, Boids[i], cohesionDistance, cohesionFactor);
					double cohX = 0d; double cohY = 0d;

					if (neighbors != null)
						(cohX, cohY) = Boid.Cohese(elapsedSeconds, Boids, (Boid)Boids[(int)i], cohesionDistance, cohesionFactor, neighbors);

					// Sum forces
					Vector3d v;
					v.x = sepX + alignX + cohX;
					v.y = sepY + alignY + cohY;
					v.z = 0.0d;

					// Apply speed limits
					double speed = v.Length;

					if (speed > maxSpeed && maxSpeed != 0)
					{
						v = (v / speed) * maxSpeed;
					}

					// ... (Optional: Implement min speed if needed)

					// Update position
					v *= elapsedSeconds;
					Boids[(int)i].Velocity += v;
					Boids[(int)i].Translation += Boids[i].Velocity;
					
			#if SPATIAL_MOVE_UPDATES // this define needs to remain FALSE because currently Octree is NOT THREAD SAFE
					Boids[(int)i].SpatialNode.OnEntityNode_Moved(Boids[(int)i]);
			#endif

			#if DEBUG_OUTPUT
					const string SEPERATOR = "|";
					Utils.AppendText(EntryClass.mSimulationOutputFile, Boids[i].Translation.ToString() + SEPERATOR + Boids[i].Velocity.ToString());
			#endif
					
					// Apply boundary rules (wrap around)
					// (You'd need to define boundary dimensions here)
					// If X > maxX, X = minX, etc.

				} // end for loop
			});  // end using "FlockingRules"   NOTE: Close Parenthesis and Semicolon here is needed after close curly brace only when using Parallel.For()
        } 

#if USE_MEMORY_T
		
	
		
		/// <summary>
		/// Called by UpdateClasses() regardless of whether Octree is used or not.
		/// Called by Memory<T> ONLY if Octree is NOT used.  Otherwise it uses non-recursive Octree code within the DoFlocking() method.
		/// </summary>
        private List<int> GetNeighbors(Boid currentBoid, double largestDistance, double largestDistanceSquared)
        {
            List<int> neighbors = null;

#if USE_MEMORY_T
            /*   Func<int, int, double, Transform.Transform_Struct, Transform.Transform_Struct, bool> findNeighborsFunc = (index, referenceIndex, distanceSquared, boid, referenceBoid) =>
             {
                 if (index == referenceIndex)
                     return false;

                 if (Vector3d.GetDistance3dSquared(boid.Translation, referenceBoid.Translation) < distanceSquared)
                     return true;

                 return false;
        };*/

            Func<Transform, Transform, double, bool> ff = (boid, referenceBoid, distanceSquared) =>
            {
                if (boid == referenceBoid)
                    return false;

                if (Vector3d.GetDistance3dSquared(boid.Translation, referenceBoid.Translation) < distanceSquared)
                    return true;

                return false;
            };
#else
            Func<Transform, Transform, double, bool> ff = (boid, referenceBoid, distanceSquared) =>
            {
                if (boid == referenceBoid)
                    return false;

                if (Vector3d.GetDistance3dSquared(boid.Translation, referenceBoid.Translation) < distanceSquared)
                    return true;

                return false;
            };
#endif

            // SPATIAL SEARCH ///////////////////////////////////////////////////////////////////////////////////
#if SPATIAL_SEARCH
            //BoundingBox searchArea = new BoundingBox(currentBoid.Translation, largestDistance * 0.5d);
			BoundingSphere searchSphere = new BoundingSphere(currentBoid.Translation, largestDistance * 0.5d);
            Func<EntityNode, EntityNode, bool> match = (neighbor, current) =>
            {
                if (neighbor == current) return false;
                // TODO: WE MUST CACHE span<T> and not access neighbor.Translation and current.Translation... we need to directly
                 //        access the indices of the Span<T> here... otherwise its TOO SLOW
                if (Vector3d.GetDistance3dSquared(neighbor.Translation, current.Translation) <= largestDistanceSquared) return true;
                return false;
            };

#if USE_MEMORY_T
			Console.WriteLine ("GetNeighbors - Memory<T> Using SpatialQueryLocal()");
            ComponentStore<Transform.Transform_Struct> store = EntryClass.mCStoreCol.CheckOut<Transform.Transform_Struct>(0);
			int currentInternalIndex = currentBoid.GetUserStructIndex(typeof(Transform.Transform_Struct));
            List<EntityNode> found = SpatialQueryLocal(store.Span, currentBoid.SpatialNode, currentInternalIndex, largestDistanceSquared, true, searchSphere);

#else
            List<EntityNode> found = this.Octree.Query(currentBoid, true, searchArea, match);
#endif

            if (found == null || found.Count == 0) return null;
			//Console.WriteLine("nc = " + found.Count.ToString());

            neighbors = new List<int>(found.Count);
            for (int j = 0; j < found.Count; j++)
            {
                neighbors.Add(found[j].EntityArrayIndex);
            }

#else      // NON-SPATIAL ASSISTED DISTANCE CHECK  ///////////////////////////////////////////////////////////////////////////////////                     
			// Console.WriteLine ("GetNeighbors - Memory<T> NON SPATIAL LINQ CALL TO Boid.FindNeighbors() being used.");

#if USE_MEMORY_T
			//neighbors = Boid.FindNeighbors(store, numBoids, largestDistance, currentIndex, findNeighborsFunc);
			try 
			{
				neighbors = Boid.FindNeighbors(this.Boids, largestDistanceSquared, currentBoid.SpanIndex, ff);
			}
			catch (Exception ex)
			{
				Console.WriteLine ("bSim.GetNeighbors() - Memory<T> - attempted to call Boid.FindNeighbors() - " + ex.Message);	
			}
#else
#if USE_MEMORY_T == false
            List<Boid> found = Boid.FindNeighbors(this.Boids, largestDistance, currentBoid.SpanIndex, ff);

            if (found == null || found.Count == 0) return null;
            neighbors = new List<int>(found.Count);
            for (int j = 0; j < found.Count; j++)
            {
                neighbors.Add(found[j].Index);
            }
#else // DO NOT USE A NEIGHBOR FINDING FUNCTION, JUST BRUTE FORCE ALL BOIDS ///////////////////////////////////////////////////////////////////////////////////                     
						
	  // WARNING: iterating through ALL boids
	  // for each CURRENT boid is O(n^2) and is too expensive
	  //	neighors = allBoids;
						
#endif
#endif
#endif
            // END NEIGHBOR SEARCH
            return neighbors;
        }


		
		private List<EntityNode> FindNearestTarget (EntityNode source, List<Tuple<int, double>> neighbors, out double[] distances)
		{
			distances = null;
			if (neighbors == null || neighbors.Count == 0) return null;
			
			EntityNode[] tmp = new EntityNode[neighbors.Count];
			distances = new double[neighbors.Count];
					
			ComponentStore<Transform.Transform_Struct> allTransforms = EntryClass.mCStoreCol.CheckOut<Transform.Transform_Struct>(0);
	
			for (int i = 0; i < neighbors.Count; i++)
			{
				int arrayIndex = allTransforms.Span[neighbors[i].Item1].EntityArrayIndex;
				EntityNode currentTarget = Boids[arrayIndex];
				distances[i] = Vector3d.GetDistance3dSquared(source.Translation, currentTarget.Translation); // allTransforms.Span[neighbors[i].Item1].Translation);
				tmp[i] = currentTarget;
				System.Diagnostics.Debug.Assert(source != currentTarget, "FindNearestTarget() - Target cannot be same as the Current Source Droid!");
			}

			// Sort 'the keys double[]' (distances) and rearrange associated data 'EntityNode[]' (results) accordingly
			Array.Sort(distances, tmp);

			return new List<EntityNode>(tmp);
		}
		
		///<summary>
		/// This is the target that the operator (either crew member or computer) of a Targeting Crew Station
		/// will be attempting to fire upon.  
		/// Return value is a List of Tuples containing the EntityNode and Distance to that Entity
		/// </summary>
		private List<Tuple<EntityNode, double>> FindNearestTarget (EntityNode source, double maxDistance)
		{
			BoundingBox searchArea = new BoundingBox (source.SpatialNode.BoundingBox.Center, maxDistance * 0.5d);
			double maxDistanceSquared = maxDistance * maxDistance;
			
			Func<EntityNode, EntityNode, Tuple<bool, double>> match = (current, neighbor) =>            {
                
				if (current == neighbor) return new Tuple<bool, double>(false, -1);
                double distanceSquared = Vector3d.GetDistance3dSquared(neighbor.Translation, current.Translation);
				if (distanceSquared <= maxDistanceSquared) return new Tuple<bool, double>(true, distanceSquared);
                return new Tuple<bool, double>(false, -1);
            };
			
			List<Tuple<EntityNode, double>> found  = this.Octree.Query(source, true, searchArea, match);
			if (found == null) return null;
			
			//Console.WriteLine("FindNearestTarget found count == " + found.Count.ToString());
			return found;		
		}
		
		///<summary>
		/// This is the target that the operator (either crew member or computer) of a Targeting Crew Station
		/// will be attempting to fire upon.  
		/// Return value is a List of Tuples containing the EntityNode and Distance to that Entity
		/// </summary>
		private List<Tuple<EntityNode, double>> FindNearestTarget (EntityNode source, BoundingBox searchArea, Func<EntityNode, EntityNode, Tuple<bool, double>> match = null)
		{
			double maxDistanceSquared = searchArea.RadiusSquared; 
			
			if (match == null)
			{
				match = (current, neighbor) =>            {
                
				if (current == neighbor) return new Tuple<bool, double>(false, -1);
                double distanceSquared = Vector3d.GetDistance3dSquared(neighbor.Translation, current.Translation);
				if (distanceSquared <= maxDistanceSquared) return new Tuple<bool, double>(true, distanceSquared);
                return new Tuple<bool, double>(false, -1);
            	};
			}
			List<Tuple<EntityNode, double>> found  = this.Octree.Query(source, true, searchArea, match);
			if (found == null) return null;
			
			//Console.WriteLine("FindNearestTarget found count == " + found.Count.ToString());
			return found;		
		}
		
		
#if USE_MEMORY_T
        private List<EntityNode> SpatialQueryLocal(Span<Transform.Transform_Struct> memSpan, OctreeOctant refSpatialNode, int refIndex, double distance, bool recurse, BoundingSphere searchSphere)
        {
            if (refSpatialNode == null) throw new ArgumentNullException("SpatialQueryLocal() - reference Entity cannot be null.");
            //if (!refSpatialNode.BoundingBox.Intersects(searchArea)) return null; // early exit
			if (refSpatialNode.BoundingSphere.Intersects(searchSphere) == IntersectResult.OUTSIDE) return null; // early exit
	
            List<EntityNode> results = new List<EntityNode>();

            // ITERATIVE DEPTH-FIRST TRAVERSAL
            Stack<OctreeOctant> stack = new Stack<OctreeOctant>();
            stack.Push(refSpatialNode);

            while (stack.Count > 0)
            {
                OctreeOctant current = stack.Pop();

                if (current.EntityNodes != null)
                {
                    for (int i = 0; i < current.EntityNodes.Length; i++)
                    {
						int childInternalIndex = current.EntityNodes[i].GetUserStructIndex(typeof(Transform.Transform_Struct));
						
                        if (childInternalIndex == refIndex) continue;
                        // TODO: WE MUST CACHE span<T> and not access neighbor.Translation and current.Translation... we need to directly
                        //        access the indices of the Span<T> here... otherwise its TOO SLOW
                        double calc = Vector3d.GetDistance3dSquared(memSpan[childInternalIndex].Translation, memSpan[refIndex].Translation);
                        //System.Diagnostics.Debug.WriteLine("Calculated distance = " + calc.ToString());
                        if (calc <= distance)
                            results.Add(current.EntityNodes[i]);
                    }
                }

                if (current.Children != null)
                {
                    for (int i = 0; i < current.Children.Length; i++)
                        // NOTE: Each OctreeOctant's BoundingBox needs to be in World Space.
						if (current.Children[i].BoundingSphere.Intersects(searchSphere) != IntersectResult.OUTSIDE)
                        //if (current.Children[i].BoundingBox.Intersects(searchArea))
                            stack.Push(current.Children[i]);
                }
            }



            /*
            // RECURSIVE DEPTH-FIRST TRAVERSAL
            // NOTE: Each OctreeOctant's BoundingBox needs to be in World Space.
            if (!refSpatialNode.BoundingBox.Intersects(searchArea))
                return null;

            // compare the distance of all Entities within this Octant
            if ( refSpatialNode.EntityNodes != null)
                for (int i = 0; i < refSpatialNode.EntityNodes.Length; i++)
                {
                    if (refSpatialNode.EntityNodes[i].SpanIndex == refIndex) continue;
                    // TODO: WE MUST CACHE span<T> and not access neighbor.Translation and current.Translation... we need to directly
                    //        access the indices of the Span<T> here... otherwise its TOO SLOW
                    if (Vector3d.GetDistance3dSquared(memSpan[refSpatialNode.EntityNodes[i].SpanIndex].Translation, memSpan[refIndex].Translation) <= distance) 
                        results.Add(refSpatialNode.EntityNodes[i]);
                }

            if (recurse)
            {
                if (refSpatialNode.Children != null)
                {
                    for (int j = 0; j < refSpatialNode.Children.Length; j++)
                    {						
                        List<EntityNode> nestedResults = SpatialQueryLocal(memSpan, refSpatialNode.Children[j], refIndex, distance, recurse, searchArea);
                        if (nestedResults != null)
                            results.AddRange(nestedResults);
                    }
                }
            }
            */

            if (results.Count == 0) return null;
            return results;
        }
#endif
    
			
		
		// https://github.com/MonoGame/MonoGame/blob/db9e544dfb3f1c1e8bfc2ea08fec31c1c17a9033/MonoGame.Framework/Game.cs#L539
        private void DoLifeCycle(ComponentStore<LifeForm> store, object[] parameters, int seed, GameTime gt)
        {
			ComponentStore<LifeForm> testLEComp = EntryClass.mCStoreCol.CheckOut<LifeForm>(0);
			//Console.WriteLine("DoLifeCycle() - Stores are the same == " + (store == testLEComp).ToString());
			
			// TODO: until both paths use DoLifeCycle(), this will throw off deterministism for Memory<T> path
    		return;
    
			Span<LifeForm> livingEntitySpan = store.Span;
	
			int recordCount = (int)store.Count;
			
			// todo: maxAge and minAge need to be set in Parameters
	        double maxAge = 0.9d;
            double minAge = 0.3d;
			int numDestroyed = 0;
	
            for (int i = 0; i < recordCount; i++)
			{
				// NOTE: this timerID is taken from the LivingEntity struct's spanIndex
				int index = livingEntitySpan[i].EntityArrayIndex;
				string entityKey = Boids[index].EntityKey; 
				
				bool spawnReady = mIntervalTimers.IsReady(entityKey, "droid_spawn");
            	if (spawnReady)
            	{
               		// Console.WriteLine("Spawn Ready == " + spawnReady.ToString());
                	mIntervalTimers.Reset(entityKey, "droid_spawn");
            	}
				
				// todo: i think we need to check to see if this record is for
				//       an Entity that is enabled
				double age = gt.ElapsedSeconds - livingEntitySpan[i].CreationDateTime;// Utils.GetAge(memSpan[i].CreationDateTime);
				livingEntitySpan[i].Age = age;
				if (age >= maxAge)
				{
					// TODO: there is a bug here in CheckIn and Destroy()... we are not managing the entity.Index and entity.SpanIndex properly
					/*

					Destroy(Boids[i]);
					numDestroyed++;
					*/
				}
			}
         
			// spawn new ones up to max spawn number per frame
			double width = (double)parameters[0];
			double height = (double)parameters[1];
			double depth = (double)parameters[2];
	
			int numToCreate = numDestroyed;
			for (int i = 0; i < numToCreate; i++)
			{
				// todo: i think we need to check to see if this record is for
				//       an Entity that is enabled
				double age = gt.TotalElapsedSeconds - livingEntitySpan[i].CreationDateTime;
				Random mTHRandom = ThreadedRandom.Instance;  //(this.Seeds.Master);
				
				Spawn(mTHRandom, i, width, height, depth);
			}
        }
		

		///<summary>
		/// The Droid's Eyes are treated as Optical Sensors and are processed to find the adjacent Droids to each other Droids based on their sight distance.
		/// This means that each Droid will find all Droids that are within it's "optical range."
        /// This will be the initial set of "neighbors" that a Droid is influenced by before the finer
        /// influences of seperation, alignment and cohesion rules.
		/// Incidentally, moving this processing out into a seperated dedicated processor results in a significant boost in FPS compared to when it was
		/// apart of DoFlocking().  We moved it out seperately because we need the adjacency info for doing Combat logic such as which Droid a particular
		/// Droid can "see" and thus target with a laser.  
		///</summary>
        private void ProcessOpticalSensors(ComponentStore<Transform.Transform_Struct> transformStructStore, object[] parameters, int seed, GameTime gt)
        {
            mNeighbors.Clear();

            OctreeOctant root = this.Octree;

			//Console.WriteLine("ProcessOpticalSensors() - parameters count == " + parameters.Length.ToString());
			
			// NOTE: these values derived from passed in parameters
			double separationDistance = (double)parameters[0];
			double alignmentDistance = (double)parameters[1];
			double cohesionDistance = (double)parameters[2];
			
			// more parameters
			double separationFactor = (double)parameters[3];
            double alignmentFactor = (double)parameters[4];
            double cohesionFactor = (double)parameters[5];
			double turnFactor = (double)parameters[6]; // For boundary avoidance
			double maxSpeed = (double)parameters[7];
            
            //Console.WriteLine("ProcessOpticalSensors() - parameters count OK");
            double largestDistance = Utils.GetMax(separationDistance, alignmentDistance, cohesionDistance);
			
            double seperatationDistanceSquare = separationDistance * separationDistance;
            double alignmentDistanceSquared = alignmentDistance * alignmentDistance;
            double cohesionDistanceSquared = cohesionDistance * cohesionDistance;
		   
            
            double largestDistanceSquared = largestDistance * largestDistance;
			
			
			// TODO: do we need a BaseEntity Struct that  just contains the EntityArrayIndex, UserTypeID and Configuration?
			
			
			int recordCount = (int)transformStructStore.Count;
			
			//Console.WriteLine("ProcessOpticalSensors() - TransformStore's Record count == " + recordCount.ToString());
			//Console.WriteLine("ProcessOpticalSensors() - Largest Distance Squared == " + largestDistanceSquared.ToString());
            System.Threading.Tasks.Parallel.For(0, recordCount, i =>
			//for (int i = 0; i < recordCount; i++) // TODO: this needs to use the store.ComponentCount since the memSpan may have empty records at positions >= store.ComponentCount
            {
				// NOTE: inside of the Parallel.For(), Span<T> cannot be passed in
				//      because the code inside the Paralle.For() is treated as a Lambda
				Span<Transform.Transform_Struct> allTransforms = transformStructStore.Span;

				// NOTE: we iterate through Boid's ONLY (Enitites.Configuraton == BoidConfiguration) because we are interested in THEIR location not those of any other Entity configurations.
				// NOTE: problem with the BOOLEAN version of this Configuration test is, we want to test for Boid configuration and ONLY Boid configuration
				//       and not another Configuration such as HumanOperatorConfiguration which CONTAINS all of BoidConfiguration  but LOGICALLY OR's "|" CONFIGURATION.Sentient as well 
				//       and so it WILL pass the BOOLEAN version of this test.  Thus solution is a DIRECT == compare.  Duh!
				if (allTransforms[(int)i].Configuration != BoidConfiguration)
				//if ((allTransforms[(int)i].Configuration & BoidConfiguration) != BoidConfiguration) 
				{
					//Console.WriteLine("Transform_Struct.Configuration == " + memSpan[(int)i].Configuration.ToString());
					return;
				}
				
				//EntityNode currentBoid = ; // Boids[(int)i];
				int currentEntityArrayIndex = allTransforms[(int)i].EntityArrayIndex;
				System.Diagnostics.Debug.Assert(Boids[allTransforms[(int)i].EntityArrayIndex] is Boid);
				
				
				//int currentInternalTransformIndex = memSpan[i].InternalTransformIndex; // currentBoid.GetUserStructIndex(typeof(Transform.Transform_Struct));
				//System.Diagnostics.Debug.Assert (i == currentInternalTransformIndex, "ProcessOpticalSensors() - These indices should match now but wont once we destroy/spawn new Droids. ");
				
				
		//		arrayIndex = memSpan[currentInternalIndex];
				
		//		System.Diagnostics.Debug.Assert (arrayIndex == currentInternalIndex);
				
				// add a List<Tuple> to our mNeighbors Dictionary<> that will hold any adjacents for this current Droid
		
				// NOTE: we currently use the entityArrayIndex as key into mNeighbors but the Tuples<> use <int == internalIndex> for referencing the adjacent/neighbor
				//       We might want to just use the internalIndex for the main mNeighbors dictionary key too.
				mNeighbors.TryAdd(currentEntityArrayIndex, new List<Tuple<int, double>>(4));

				
		#if SPATIAL_SEARCH == false

			   if (i > Boids.Count - 1)
				   Console.WriteLine("ProcessOpticalScanners() - Out of range 'arrayIndex' == " + currentEntityArrayIndex.ToString() + " but count == " + Boids.Count.ToString());

				mNeighbors[currentEntityArrayIndex] =   GetNeighbors(Boids[currentEntityArrayIndex], largestDistance, largestDistanceSquared);
		#endif
				
			
		#if SPATIAL_SEARCH
				Stack<OctreeOctant> stack = new Stack<OctreeOctant>(32);
            	
				
				// INLINING of "GetNeighbors" in order to avoid have to load the memSpan onto the stack for each iteration
				
				// Vector3d currentBoidTranslation = memSpan[i].Translation;
								
				// WARNING:  The first line that uses currentBoid.Translation is 100x SLOWER than the version using CLASSES (eg for "Classes" version comment out #define USE_MEMORY_T
				//           The second line that uses memSpan[i].Translation is 100x FASTER than the version using CLASSES (WHAT ON EARTH?
				//           I believe it is because the cache evicts the span<T> data and has to re-load it every iteration (eg memSpan.Length)
				// UPDATE:   Above is likely wrong.  One problem is memSpan[i].Translation was always 0,0,0 and so the search box was often
				//           never intersecting with the box of the currentB's spatial node SpatialNode.BoundingBox

				//       BoundingBox searchArea = new BoundingBox(currentBoid.Translation, radius);
				//       System.Console.WriteLine("Translation CLASS = " + currentBoid.Translation.ToString());
				//BoundingBox searchArea = new BoundingBox(Boids[i].Translation, radius);
				using (EntryClass.CodeProfiler.HookUp("GetNeighbors"))
				{
					BoundingBox searchArea;
					double searchRadius = largestDistance * 0.5d;
                    searchArea = new BoundingBox(allTransforms[(int)i].Translation, searchRadius);
					//BoundingBox searchArea = new BoundingBox(currentBoidTranslation, radius);
					//System.Console.WriteLine("ProcessOpticalSensors() - Translation = " + allTransforms[(int)i].Translation.ToString() + " Search Radius = " + searchRadius.ToString());
                    //System.Console.WriteLine ("Search radius == " + searchRadius.ToString());
						
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // INLINED VERSION OF "ITERATIVE DEPTH-FIRST" TRAVERSAL OF OCTREE TO FIND NEIGHBORING BOIDS OF THE CURRENT ONE
					// NOTE: We use this inline version that uses a stack<> to avoid recursion because having to load the span<T> onto
					//       the stack for every function call that needs it, slows this "flocking" update code BIG TIME (eg by ~100x slower)
	
                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    stack.Clear();
					stack.Push(root); 
					
					while (stack.Count > 0)
                    {
						OctreeOctant currentOctant = stack.Pop();
						if (currentOctant.BoundingBox.Intersects(searchArea))
						{
							// TODO: the following call to currentOcant.EntityNodes needs to be thread safe... the EntityNodes assigned can move within this ProcessOpticalScanners()  method
							EntityNode[] ents = currentOctant.EntityNodes;
							if (ents != null)
							{
								//Console.WriteLine("ProcessOpticalSensors() - Entities within octant == " + ents.Length.ToString());
								//
								//for (int j = 0; j < ents.Length; j++)
								//{
								//	double d =  Vector3d.GetDistance3d(Boids[allTransforms[(int)i].EntityArrayIndex].Translation, ents[j].Translation);
								//	Console.WriteLine("ProcessOpticalSensors() - ent " + j.ToString() + " = '" + ents[j].EntityKey + " DISTANCE == " + d.ToString());
								//	
								//}
								
								for (int j = 0; j < ents.Length; j++)
								{
									EntityNode potentialNeighbor = ents[j];
									if (Boids[currentEntityArrayIndex] == potentialNeighbor) continue;
									System.Diagnostics.Debug.Assert (potentialNeighbor.Configuration != 0, "ProcessOpticalSensors() - CONFIGURATION for Entity '" + potentialNeighbor.EntityKey + "' is set to 'None' and is likely an ERROR.");
									if (potentialNeighbor.Configuration != (uint)BoidConfiguration) continue;
																		
									int potentialInternalTransformIndex = potentialNeighbor.GetUserStructIndex(typeof(Transform.Transform_Struct));
									int potentialArrayIndex = allTransforms[potentialInternalTransformIndex].EntityArrayIndex;

									// if (currentOctant.EntityNodes[j].SpanIndex == currentBoid.SpanIndex) continue; 
									//using (EntryClass.CodeProfiler.HookUp("IntersectsSearchArea"))
									if (!potentialNeighbor.BoundingBox.Intersects(searchArea)) 
										continue;

									double distanceToNeighboringBoidSquared;
									// TODO: if i stored the SpanIndex in the Octree instead of the EntityNode perhaps that would help?
									//using (EntryClass.CodeProfiler.HookUp("GetDistanceSquared"))
									distanceToNeighboringBoidSquared = Vector3d.GetDistance3dSquared(allTransforms[potentialInternalTransformIndex].Translation, allTransforms[(int)i].Translation);
									//distanceToNeighboringBoidSquared = Vector3d.GetDistance3dSquared(allTransforms[potentialNeighbor.SpanIndex].Translation, currentBoidTranslation);

									//using (EntryClass.CodeProfiler.HookUp("GetDistanceSquared"))
									//   distanceToNeighboringBoidSquared = Vector3d.GetDistance3dSquared(currentOctant.EntityNodes[j].Translation, currentBoid.Translation);

									//Console.WriteLine("Calculated distanceSquared to neighboring boid = " + distanceToNeighboringBoidSquared.ToString());
									if (distanceToNeighboringBoidSquared <= largestDistanceSquared)
										// NOTE: we do in fact key the main dictionary<> with an EntityArrayIndex, but the found Tuple contains the internalTransformStruct's Index.
										//       for now this is ok
										mNeighbors[currentEntityArrayIndex].Add(new Tuple<int, double>(potentialInternalTransformIndex, distanceToNeighboringBoidSquared));

             					}  // end for ents[]        
							}
						
							OctreeOctant[] childOctants = currentOctant.Children;
							if (childOctants != null)
							{
								//Console.WriteLine("ProcessOpticalScanners() - CLength == " + childOctants.Length.ToString());
								for (int j = 0; j < childOctants.Length; j++)
								{
									// NOTE: Each OctreeOctant's BoundingBox needs to be in World Space.
									bool intersects = false;
									//using (EntryClass.CodeProfiler.HookUp("IntersectsSearchArea"))
									{
										if (childOctants[j] == null)
											continue;

										intersects = childOctants[j].BoundingBox.Intersects(searchArea);
									}
									if (intersects)
									{
										stack.Push(childOctants[j]);
									}
									//Console.WriteLine("ProcessOpticalScanners() - Stack count == " + stack.Count.ToString());
								} // end for childOctants[]

								//Console.WriteLine("ProcessOpticalScanners() - Stack count == " + stack.Count.ToString());
							}
						} // end if currentOctant intersects searchArea test
					} // end While loop
				} // end Using (GetNeighbors)
		#endif // SPATIAL_SEARCH
			});
        
			//Console.WriteLine("ProcessOpticalSensors() - COMPLETE ");
        }
		
		
        private void DoFlocking(ComponentStore<Transform.Transform_Struct> store, object[] parameters, int seed, GameTime gt)
        {
			double elapsedSeconds = gt.ElapsedSeconds;
			
			// NOTE: store MUST be of the type Transform_Struct as the neighbor's tuples use .Item1 to hold that InternalTransformIndex and NOT the EntityArrayIndex
			int recordCount = (int)store.Count;

			
			//Console.WriteLine ("Span and Store Size Agree == " + (store.Span.Length == store.Size).ToString());
			
            //using (EntryClass.CodeProfiler.HookUp("AssignSpan"))
            //{
            // note: passing the store and the need to the span to the stack is slow.
            // keep this in mind when developing data procesding funtions.  you dont want
            // to have to pass thay big bock of memory around. 
            // HOWEVER, using the following line mem = Store.Span is faster than using Store.Span[i].#### everywhere!
            
            //}
			
			//Console.WriteLine("parameters count == " + parameters.Length.ToString());
			// NOTE: these values derived from passed in parameters
			double separationDistance = (double)parameters[0];
			double alignmentDistance = (double)parameters[1];
			double cohesionDistance = (double)parameters[2];
			
			// more parameters
			double separationFactor = (double)parameters[3];
            double alignmentFactor = (double)parameters[4];
            double cohesionFactor = (double)parameters[5];
			double turnFactor = (double)parameters[6]; // For boundary avoidance
			double maxSpeed = (double)parameters[7];
			
            double seperatationDistanceSquare = separationDistance * separationDistance;
            double alignmentDistanceSquared = alignmentDistance * alignmentDistance;
            double cohesionDistanceSquared = cohesionDistance * cohesionDistance;

			
			System.Threading.Tasks.Parallel.For(0, recordCount, i =>
			//for (int i = 0; i < memSpan.Length; i++) // TODO: this needs to use the store.ComponentCount since the memSpan may have empty records at positions >= store.ComponentCount
            {
				// NOTE: inside of the Parallel.For(), Span<T> cannot be passed in
				//      because the code inside the Paralle.For() is treated as a Lambda
				Span<Transform.Transform_Struct> memSpan = store.Span;
				
				int currentBoidArrayIndex = memSpan[(int)i].EntityArrayIndex;
				EntityNode currentBoid = Boids[currentBoidArrayIndex];
				int currentInternalTransformIndex = currentBoid.GetUserStructIndex(typeof(Transform.Transform_Struct));
				List<Tuple<int, double>>neighbors;
				bool r = mNeighbors.TryGetValue(currentBoidArrayIndex, out neighbors); 
				
				// NOTE: the bool result of TryGetValue() will be false if .TryGetValue() call is not synchronized.
				//       Checking value and count of neighbors is more reliable.
				if (neighbors == null || neighbors.Count == 0) return;
                int nCount = neighbors.Count;
				
				//Console.WriteLine("DoFlocking() - Neighbors count == " + nCount.ToString());
				
								  
				// DEBUG TEST - note: Item1 refers to the InternalTransformIndex for the transform_Struct so it needs to be in range of THAT specific struct
				for (int z = 0; z < nCount; z++)
					if (neighbors[z].Item1 > recordCount  - 1)
						Console.WriteLine("DoFlocking() - Neighbor value is OUT OF RANGE " + neighbors[z].ToString());
				
				// END TEST
				
				 //if (i == 8)
				//	Console.WriteLine("DoFlocking() - #954 - Neighbor Count = " + nCount.ToString());
				
				
				//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
				// Apply Flocking Rules
				//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //using (EntryClass.CodeProfiler.HookUp("FlockingRules"))
               	{
                    // SEPARATION
                    Vector3d sep;
                    Vector3d steer;
                    steer.x = 0d;
                    steer.y = 0d;
                    steer.z = 0d;

                    if (nCount > 0)
                    {
                        for (int j = 0; j < nCount; j++)
                        {
                            //if (j == currentIndex) continue; // <- this check will already have been performed in building of neighbors list
                            double distanceSquared = Vector3d.GetDistance3dSquared(memSpan[(int)i].Translation, memSpan[neighbors[j].Item1].Translation);
							//double distanceSquared = Vector3d.GetDistance3dSquared(currentBoidTranslation, memSpan[neighbors[j]].Translation);
							
                            if (distanceSquared < seperatationDistanceSquare)
                            {
                                if (distanceSquared > 0d) // Hypnotron Dec.4.2025 - required divide by 0 check
                                {
                                    // TODO: are these two results equal?
                                    steer += (memSpan[(int)i].Translation - memSpan[neighbors[j].Item1].Translation) / separationDistance ;
									//steer += (currentBoidTranslation - memSpan[neighbors[j]].Translation) / separationDistance ;
                                }
                            }
                        }

                        steer *= separationFactor;
                    }

                    sep = steer;

                    // ALIGNMENT
                    Vector3d align;
                    Vector3d neighborsVelocity;
                    neighborsVelocity.x = 0;
                    neighborsVelocity.y = 0;
                    neighborsVelocity.z = 0;

                    int foundCount = 0;
                    if (nCount > 0)
                    {
                        for (int j = 0; j < neighbors.Count; j++)
                        {
                            //if (j == currentIndex) continue; // <- this check will already have been performed in building of neighbors list
                            double distanceSquared = Vector3d.GetDistance3dSquared(memSpan[(int)i].Translation, memSpan[neighbors[j].Item1].Translation);
							//double distanceSquared = Vector3d.GetDistance3dSquared(currentBoidTranslation, memSpan[neighbors[j]].Translation);
							
                            if (distanceSquared < alignmentDistanceSquared)
                            {
                                neighborsVelocity += memSpan[neighbors[j].Item1].Velocity;
                                foundCount++;
                            }
                        }
                    }

                    if (foundCount == 0)
                        align = new Vector3d(0, 0, 0);
                    else
                    {
                        neighborsVelocity /= foundCount;
                        align = (neighborsVelocity - memSpan[(int)i].Velocity) * alignmentFactor;
                    }

                    // COHESION
                    Vector3d coh;
                    coh.x = 0;
                    coh.y = 0;
                    coh.z = 0.0d;

                    Vector3d neighborsAvgCenter;
                    neighborsAvgCenter.x = 0;
                    neighborsAvgCenter.y = 0;
                    neighborsAvgCenter.z = 0;

                    if (nCount > 0)
                    {
                        for (int j = 0; j < nCount; j++)
                            //if (j == currentIndex) continue; // <- this check will already have been performed in building of neighbors list
                            neighborsAvgCenter += memSpan[neighbors[j].Item1].Translation;

                        neighborsAvgCenter /= nCount;
                        coh = (neighborsAvgCenter - memSpan[(int)i].Translation) * cohesionFactor;
						//coh = (neighborsAvgCenter - currentBoidTranslation) * cohesionFactor;
                    }

                    // SUM FORCES
                    Vector3d v = sep + align + coh;

                    // Apply speed limits
                    double speed = v.Length;
                    if (speed > maxSpeed && maxSpeed != 0)
                    {
                        v = (v / speed) * maxSpeed;
                    }

                    // ... (Optional: Implement min speed if needed)

                    // Update position
                    v *= elapsedSeconds;

                    memSpan[(int)i].Velocity += v;
					memSpan[(int)i].Translation += memSpan[(int)i].Velocity;
                } // end profiler FlockingRules

				
				//Console.WriteLine("DoFlocking() - OnEntityNode_Moved()");
		#if SPATIAL_MOVE_UPDATES // this define needs to remain FALSE because currently Octree is NOT THREAD SAFE
//              // making this thread safe is going to be a problem if we also want to maintain performance
				// i could maybe only add locks to depth = 1 and not any further.
				currentBoid.SpatialNode.OnEntityNode_Moved(currentBoid);
				//Console.WriteLine("DoFlocking() - Moved Completed...");
		#endif	
                // Apply boundary rules (wrap around)
                // (You'd need to define boundary dimensions here)
                // If X > maxX, X = minX, etc.
			
				//if (i == 8)
				//	Console.WriteLine("DoFlocking() - #954 - " + i.ToString());
				//Console.WriteLine(i.ToString());
			}); // end parallel.for
			
		#if DEBUG_OUTPUT
			Span<Transform.Transform_Struct> memSpan = store.Span;
			for (int i = 0; i < memSpan.Length; i++)
			{
				const string SEPERATOR = "|";
				Utils.AppendText(EntryClass.mSimulationOutputFile, memSpan[i].Translation.ToString() + SEPERATOR + memSpan[i].Velocity.ToString());	
			}	
		#endif	
        }
#endif
        		
		
	
		private static System.Threading.SemaphoreSlim mSort = new System.Threading.SemaphoreSlim(1);
		
		/// <summary>
		/// This is mostly just creating 'SensorContact' from "neighbors" .... based on policies
		/// </summary>
		private void CreateContactListFromAdjacents()
		{
			if (mNeighbors.Count == 0) return;
			//Console.WriteLine("DoContactListSorting() - STARTING");
			
			ComponentStore<TacticalStation> allTacticalStations  = EntryClass.mCStoreCol.CheckOut<TacticalStation>(0);
			int recordCount = (int)allTacticalStations.Count;

            System.Threading.Tasks.Parallel.For(0, recordCount, i => 		
			{
				// NOTE: problem with the BOOLEAN version of this Configuration test is, we want to test for Boid configuration and ONLY Boid configuration
				//       and not another Configuration such as HumanOperatorConfiguration which CONTAINS all of BoidConfiguration  but LOGICALLY OR's "|" CONFIGURATION.Sentient as well 
				//       and so it WILL pass the BOOLEAN version of this test.  Thus solution is a DIRECT == compare.  Duh!
				if (allTacticalStations.Span[(int)i].Configuration != TacticalStationConfiguration)
				//if ((allTacticalStations.Span[(int)i].Configuration & BoidConfiguration) != BoidConfiguration)
				{
					//Console.WriteLine("configuration = " + allTransforms.Span[(int)i].Configuration.ToString());
					return;
				}	
				
				int currentStationArrayIndex = allTacticalStations.Span[(int)i].EntityArrayIndex; // current.EntityArrayIndex; //  current.GetUserStructIndex(typeof(Transform.Transform_Struct));
				//System.Diagnostics.Debug.Assert( (int)i == currentArrayIndex, "DoContactListSorting() - array index does not match...");
				// the adjacnets that are stored in neighbors from the overall mNeighbors is very much stores Area of Interest for each Droid
				// but we will only send them things that their sensors can detect (and "eyes" are treated as optical sensors)
				//Console.WriteLine ("DoContactListSorting() - Key for current == " + Boids[currentArrayIndex].EntityKey);
				
				// TODO: Should we be iterating over the 'TacticalStation' struct's and NOT the Boids array? and then getting the SensorContacts from it?
				//       we could skip any TacticalStation that is not designated as PRIMARY TacticalStation
				
				EntityNode currentStation = Boids[currentStationArrayIndex]; // <-- if we can get the Sensors without having to get the current Boid... hmm...
				System.Diagnostics.Debug.Assert(currentStation.EntityKey.Contains("tactical"), "ProcessOpticalSensors() - Entity is NOT a TacticalStation.");
				
				int currentBoidArrayIndex = currentStation.EntityArrayIndex - TACTICAL_STATION_OFFSET;
				Boid currentBoid = (Boid)Boids[currentBoidArrayIndex];
				
				//Console.WriteLine ("2");
				EntityNode[] sensorEntities = GetSensors(currentBoidArrayIndex); // todo: we currently do  not have EntityNode allowing adding of child nodes.  This is needed next.
				
				int sensorsCount = 0;
				if (sensorEntities != null) sensorsCount = sensorEntities.Length;
				//Console.WriteLine("CreateContactListFromAdjacents() - Sensor Count == " + sensorsCount);
				if (sensorEntities == null) return; 
				
				//Console.WriteLine ("4");
				
				// grab the neighbors/adjacents for this Droid.  The returned parameter List<Tuple<int, double>> tells us which Droid (int) index was detected and the (double) distance to it  
				List<Tuple<int, double>> neighbors = null;
				
				//Console.WriteLine("CreateContactListFromAdjacents() - Looking for Neighbors at Array Index  == " + currentArrayIndex.ToString());
				//foreach (int key in mNeighbors.Keys)
				//	Console.WriteLine ("Key == " + key.ToString());
				
				bool success = mNeighbors.TryGetValue(currentBoidArrayIndex, out neighbors);
								
				//Console.WriteLine("DoContactListSorting() - Found '" + neighbors.Count.ToString() + "' Adjacents for Droid @ Array Index == '" + currentArrayIndex.ToString() + "' ");
				List<SensorContact> contacts = new List<SensorContact>();
				
				//Console.WriteLine("CreateContactListFromAdjacents - 1");
				
				// iterate through all the potential "contacts"
				for (int j = 0; j < neighbors.Count; j++)
				{			
					contacts.Clear();
					ComponentStore<Transform.Transform_Struct> allTransforms  = EntryClass.mCStoreCol.CheckOut<Transform.Transform_Struct>(0);
					
					double distanceSquared = neighbors[(int)j].Item2;
					int potentialContactsInternalTransformIndex = neighbors[(int)j].Item1; 
					int potentialContactsEntityArrayIndex = allTransforms.Span[potentialContactsInternalTransformIndex].EntityArrayIndex;
			  
					//Console.WriteLine("CreateContactListFromAdjacents - 2");
					// Iterate through all the Sensors the current Droid is using to see which ones might
					// detect this potential contact.  This is why a "SensorContact" may already exist
					// in the List<SensorContact> 'contacts'  because multiple Sensors on _the_same_ship_
					// might detect this adjacent 'contact.'
					for (int k = 0; k < sensorEntities.Length; k++)
					{
						int sensorStructIndex = -1;
						Memory<Sensor> sensorStruct = (Memory<Sensor>)sensorEntities[k].GetUserStruct(typeof(Sensor), out sensorStructIndex);
						int sensorArrayIndex = sensorStruct.Span[0].EntityArrayIndex;
						
						double sensorRangeSquared = sensorStruct.Span[0].RangeSquared;
						
						//Console.WriteLine("CreateContactListFromAdjacents() - Range = " +  sensorRangeSquared.ToString() + " Distance to Contact ==  " + Math.Sqrt(distanceSquared).ToString());
						
						if (sensorRangeSquared >= distanceSquared)
						{
							SensorContact c;

							// if another sensor on this same vehicle has detected this potential contact already, append it's Sensor index
							// to the list of SensorIndices for this contact so we know all sensors that detected it.
							Predicate<SensorContact> contactExists = contact => contact.ContactEntityArrayIndex == potentialContactsEntityArrayIndex;
							c = contacts.Find(contactExists);

							if (!c.Equals(default(SensorContact)))
							{
								//Console.WriteLine("CreateContactListFromAdjacents() - sensor contact name == " + c.Name);
								if (c.SensorsIndices == null) 
									c.SensorsIndices = Utils.ArrayAppend<int>(c.SensorsIndices,  sensorArrayIndex); // sensorStructIndex);
								else
									c.SensorsIndices.Append(sensorArrayIndex); // sensorStructIndex);

								//Console.WriteLine("DoContactListSorting() - Appending SensorContact of Droid at Array Index = '" + c.ContactEntityArrayIndex.ToString() + "' detected by the Sensor at Array Index = '" + sensorArrayIndex.ToString() + "'");
							}
							else // contact has not yet already been detected by another Sensor within this same ship during this loop through all sensors on this same ship
							{
								c = new SensorContact();

								//Console.WriteLine("DoContactListSorting() - Creating NEW SensorContact of Droid at Array Index = '" + contactsEntityArrayIndex.ToString() + "' detected by the Sensor at Array Index = '" + sensorArrayIndex.ToString() + "'");
								Boid bb = null;
								try 
								{
									bb =  (Boid)this.Boids[potentialContactsEntityArrayIndex];
								}
								catch (Exception ex)
								{
									Console.WriteLine("DoContactListSorting() - ERROR: Boid contact at Array Index == " + c.ContactEntityArrayIndex.ToString() + " not found. " + ex.Message);
								}

								//Console.WriteLine ("9");
								//int sensorContactInternalTransformIndex = bb.GetUserStructIndex(typeof(Transform.Transform_Struct));
								// contact details are needed to find the correct SensorContact to potentially merge with an existing SensorContact for this detected Entity
								// NOTE: HelloBoids should only have one element within its SensorsIndices
								//       because each Droid only has one Sensor ('Optical Sensor' == eyes)
								c.ContactEntityArrayIndex = potentialContactsEntityArrayIndex; // index within the Boid[] array of the detected Droid
								c.Index = (int)i;
								c.Name =  "boid_" + potentialContactsEntityArrayIndex.ToString(); // verified name of ship eg. UEN Pegasus "Galactica Class Battlestar"
								c.RegistryNumber = c.Name;
								c.Type = SensorContact.TYPE.Drone;
								c.ContactStatus = Target.STATUS.Unknown;
								c.FriendOrFoe = SensorContact.FoF.Unknown;
								c.SensorsIndices = Utils.ArrayAppend<int>(c.SensorsIndices, sensorArrayIndex); //sensorStructIndex);
								
								// telemetry
								SensorContact.ContactTelemetry t;
								t.Radius = (float)bb.BoundingBox.Radius;    // how might size be spoofed?
								t.Position = bb.Translation;
								t.Velocity = bb.Velocity;
								t.DistanceSquared = distanceSquared;
								t.Heading = 0;
								t.TimeAcquired = Utils.NowTicks(); // todo: this needs to eventually just be gt.Ticks <-- which must come from 'gametime fixedstep' and not 'real-time'
								t.TimeLast = t.TimeAcquired;

								c.Add(t);			
								contacts.Add(c);
								//Console.WriteLine("DoContactListSorting() - Added NEW SensorContact of Droid at Array Index = '" + c.ContactEntityArrayIndex.ToString() + "' detected by the Sensor at Array Index = '" + sensorArrayIndex.ToString() + "'");
							}
						} // end sensor range check
					} // end for SensorsCount
				} // end for neihbors Count
				

				// add all of the SensorContacts to the current TacticalStation, and it will be responsible for
				// properly merging these SensorContacts with existing ones so as to maintain
				// proper SensorContact histories for all detected Entities.
				if (contacts != null)
					currentStation.Add(contacts); 
			});
			
			//Console.WriteLine("DoContactListSorting() - COMPLETED.");
		}
		
		/// <summary>
		/// Seed might typically be Seeds.Local_Droid_Tactical_Logic + mCurrentFrame;
		/// </summary>
		private void Do_Tactical_Logic(int seed, double maxDistance, GameTime gt)
		{
			//Console.WriteLine("Do_Tactical_Logic() - BEGIN ");
			//ThreadedRandom random = new ThreadedRandom(seed);
			
			
			
			
			// todo: we could pass in an array of store to our Processor functions... rather than just one.
			//       but it would have to be an array of object[] like parameters and we'd have to cast them
			// OR, our various processors can just grab the Stores that are needed.  There's no need really to 
			// grab the stores outside of the processor functions only to just pass them there...  
	
 			
			 // Sensor scan 
			 //  - spatial searches using Search Radius to find adjacents/neibhors
			             
             //  Sensor Scans
			 //    - atmospheric composition
			 //    - geological - minerals
			 //    - archaeological (ground penetrating radars and such)
			 //
			 //    - biological life analysis
			 //    - specific racial signatures 
			 //    - specific person signatures (much slower if the search area is not very limited)
			 //    - specific atoms, molecules
			 //    - specific energy signatures
			 //    
			 
             //    - AreaOfInterest 
			
			// https://forum.paradoxplaza.com/forum/threads/the-truth-is-out-there-an-aurora-4x-c-forum-game-version-1-13.1492866/page-11
			
             // Crew/NPC movement (steering)
             //   linear acceleration / decelaration
			 // Ship movement - Gravitation / N-Body
             // Ship movement - Newtonian Physics
             // Ship movement - Steering
			 // Ship movement - Lerping to a destination over a specific time period
             //   SEE https://github.com/vazgriz/PID_Controller
			 //     - MIT License
			 //     - specifically has a sample for controlling a Turret
			 //     - https://github.com/vazgriz/PID_Controller/blob/master/Assets/Scripts/Turret.cs
			 //     - Also see stage\\projects\\waypointfollower.txt
			
			 // Turret aiming - PID controllers
             // laser / particle cannons - movement
             // missiles - PID controllers again

             // particle Systems
             // motion fields
             // 

			 // Collisions - could benefit from sharing Adjacents / Neighbors from Sensor Scans or vice-versa
             // collisions (BoundingBox.Min, BoundingBox.Max, and Sphere.Center and Sphere.Radius need to be in a Memory<T> struct)
             //
			
             // Animations (LODs used to prevent animations when too far away?)
             //   - interpolation Animations
             //   - spritesheets, atlas texture animations
             // 
             
			 // 
			 // 
             //    - storing data on interior Walls for fast iteration of mouse picking
             //    walls and floors and ceilings.  <-- This is mostly for when our view is such that
             //    we cannot first determine the closest edge and use that to find any wall on that edge
             //    For instance, imagine a camera that is more like a FPS view or a bullet or laser hits a Walls

             //    - storing data on interior Walls and Floors and Ceilings "damage"


        	//Console.WriteLine("Do_Tactical_Logic() - DoDeviceReadyStatus()");
			DoDeviceReadyStatus();
						
			
			//Console.WriteLine("Do_Tactical_Logic() - DoStationCanActStatus()");
			DoStationCanActStatus();
			//Console.WriteLine("Do_Tactical_Logic() - continuing Do_Droid_Logic()");
			
			
			
			DoEnableDisableSensors();
			
			
			
			//Console.WriteLine("Do_Tactical_Logic() - CreateContactListFromAdjacents()");
			CreateContactListFromAdjacents(); // based on policies
			
			
			//Console.WriteLine("Do_Tactical_Logic() - DoTargetPrioritization()");
			DoTargetPrioritization();
			
			
			// todo: if we had a list of all weapons for every ship to pass all at once
			//       as well as all targets for each ship to pass all at once, we could run this
			//       processor in a single call from here...
			//Console.WriteLine("Do_Tactical_Logic() - DoWeaponFitnessScores()");
			DoWeaponFitnessScores(null, null);
			
			
			//Console.WriteLine("Do_Tactical_Logic() - DoWeaponsCanFire()");
			DoWeaponsCanFire();
			
			//ComponentStore<LifeForm> allLivingEntities = EntryClass.mCStoreCol.CheckOut<LifeForm>(0);
			//ComponentStore<Component> allComponents  = EntryClass.mCStoreCol.CheckOut<Component>(0);
			//ComponentStore<TacticalStation> allTacticalStations  = EntryClass.mCStoreCol.CheckOut<TacticalStation>(0);
						
			//Console.WriteLine("Do_Tactical_Logic() - preparing for loop()");
			int recordCount = Boids.Count;
            System.Threading.Tasks.Parallel.For(0, recordCount, i => 				
			//for (int i = 0; i < Boids.Count; i++)
            {
				if (Boids[(int)i] is Boid == false) return;
				
				Random random = ThreadedRandom.Instance;
				Boid attacker = (Boid)Boids[(int)i];
				
				// NOTE: Transform_Struct will  host indices for Boids, OpticalSensors and TacticalStations
				int currentInternalIndex = attacker.GetUserStructIndex(typeof(Transform.Transform_Struct));
				int attackerArrayIndex = attacker.EntityArrayIndex;
				System.Diagnostics.Debug.Assert (attackerArrayIndex == i, "Do_Droid_Logic() - i and attackerArrayIndex do not match.");
				
				// get a reference to the Station and determine if it "CanAct()"
				EntityNode[] operators = GetTacticalStationOperators(attackerArrayIndex);
				EntityNode[] tacticalStationEnts = GetTacticalStations(attackerArrayIndex);
				if (operators == null || tacticalStationEnts == null || operators.Length == 0 || tacticalStationEnts.Length == 0) return;

				int operatorEntityArrayIndex = operators[0].EntityArrayIndex;  
				int operatorIndex;
				Memory<LifeForm> operatorStruct = (Memory<LifeForm>) operators[0].GetUserStruct(typeof(LifeForm), out operatorIndex);

				int stationArrayIndex = tacticalStationEnts[0].EntityArrayIndex;  
				int tacticalIndex;
				Memory<TacticalStation> tacticalStationStruct = (Memory<TacticalStation>) tacticalStationEnts[0].GetUserStruct(typeof(TacticalStation), out tacticalIndex);

				string errorReason = null;
				if (tacticalStationStruct.Span[0].CanAct(out errorReason)) return;
				//Console.WriteLine("Do_Tactical_Logic() - Station CanAct() == TRUE");		
				
				// NOTE: The EXE will render Sensor Contact info as necessary.
				//       The client EXE will have access to those types and the UI elements using them and can update
				//       those relevant UI elements as necessary
				
				
                //  - are we in a state of COMBAT?
				//		- direct orders?
				//      - any Contacts in list marked as FOF.Foe + FOF.Hostile as opposed to just FOF.Foe (note: stale contacts are still treated as available in case of need to persue)
				//      	- FOF.Withdrawing may be ignored for example if ROE says we don't persue in this circumstance including disabled ships and unarmed ships like freighters
				
				EntityNode[] weapons = GetWeapons(attackerArrayIndex);	
				int weaponArrayIndex = weapons[0].EntityArrayIndex; // todo: hack -  we know all droids have one weapon but this will fail otherwise
				int weaponIndex;
				Memory<Weapon>weaponStruct = (Memory<Weapon>) weapons[0].GetUserStruct(typeof(Weapon), out weaponIndex);
				int componentIndex;
				Memory<Component>componentStructForWeaponEntity = (Memory<Component>)weapons[0].GetUserStruct(typeof(Component), out componentIndex);
				
				bool canFire = weaponStruct.Span[0].CanFire(out errorReason);
				
				//Console.WriteLine("Do_Tactical_Logic() - Weapon CanFire() == " + canFire.ToString());		
				if (canFire) // TODO: Establish CANFIRE PER WEAPON
           	 	{  

					string weaponKey = weapons[0].EntityKey;
					bool suspend = false;
					mIntervalTimers.Reset(weaponKey, "droid_canfire", suspend);
					
					List<Boid> targets = null;
					double[] distances = null;				
					List<Tuple<int, double>> neighbors = null;

					try
					{
						bool success = mNeighbors.TryGetValue(attackerArrayIndex, out neighbors);
						if (!success) 
						{
							//System.Diagnostics.Debug.Assert(mNeighbors.Count > 0, "Do_Tactical_Logic() - ASSERTION FAILED - Check that optical Sensors[] list is being filled via Spawn().");
							//Console.WriteLine("Do_Tactical_Logic() -  No neighbors exist in mNeighbors! This usually occurs during the very first frame since Droid Logic occurs before ProcessOpticalSensing()");
							return;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Do_Tactical_Logic() -  Attacker Droid Array Index '" + attackerArrayIndex.ToString() + "' does not exist. " + ex.Message);
					}
				
					
					
					// TEMP HACK
					// THE FOLLOWING BLOCK SHOULD GO INTO HasHitOccurred()
					// BEGIN BLOCK ------------------
					//List<EntityNode> tmp = FindNearestTarget(currentBoid, MAX_SEARCH_DISTANCE); // TODO: Hopefully this FindNearestTarget() can be optimized.... spatial searches even with Octree is slow.
					
					// This overloaded version of FindNearestTarget() returns the sorted list of neighbors from closest to furthest along with their distances to the current droid
					List<EntityNode> tmp = FindNearestTarget(attacker, neighbors, out distances);
					if (tmp == null || tmp.Count == 0)
						return;     // NOTE: for parallel.For we use "return"
						// continue; // NOTE: for regular for() loop we use "continue"

					targets = tmp.OfType<Boid>().ToList();
					//Console.WriteLine("Do_Tactical_Logic() - Attacker Droid @ Array Index '" + attackerArrayIndex.ToString() + "' Found " + targets.Count.ToString() + " targets.");
					//      NOTE - 
					// END BLOCK ------------------
					
					
					
					// WE HAVE A TARGET AND A WEAPON THAT CAN FIRE
					try
					{
						// todo: fix.  for now we wont iterate all targets, just the most near one
						Boid currentTarget = targets[0];
						double distanceToTargetSquared = distances[0];
						
						// NOTE: TacticalStation.CanHit() returns true if a hit WILL RESULT from the fired shot
						//       even if the HIT is not the expected location on a Target or even on the correct Target!
						//       Otherwise it is a total MISS.  We log the hit/miss EVENT either way... typically as a 
						//       COMBAT ACTION INITIATED and a COMBAT ACTION RESULT.  There can be multiple COMBAT ACTION RESULTS
						//       for instance if a mine field is laid, and some time later, a ship/craft is impacted by it... potentially
						//       years later!
						HIT[] hits;
						
						if (HitHasOccurred((EntityNode)attacker, currentTarget, distanceToTargetSquared, weapons[0], gt, random, out hits))
						{
							ProcessHits(hits, operatorEntityArrayIndex, stationArrayIndex, attackerArrayIndex, weaponArrayIndex, gt, random);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine ("Do_Tactical_Logic() - ERROR - " + ex.Message);
					}
				}
			});
			
			// see Keystone.Game01.Messages.   public class AttackResults since
			// we need results going over the network
		}
		
		
		/// <summary>
		/// based on policies
		/// </summary>
		private void DoTargetPrioritization()
		{
			//Console.WriteLine("DoTargetPrioritization");
			int count = Boids.Count;
            System.Threading.Tasks.Parallel.For(0, count, i => 		
			{
				if (Boids[i] is Boid == false) return;
				
				Boid current = (Boid)Boids[i];
				
				//int stationID = GetTacticalStations(i)[0]; 
				EntityNode tacticalStation = GetTacticalStations(i)[0]; //(EntityNode)Boids[stationID];
				
				//Console.WriteLine("DoTargetPrioritization - for TacticalStatin '" + tacticalStation.EntityKey + "'");
				
				List<SensorContact> contacts = tacticalStation.GetSensorContacts();
				if (contacts == null || contacts.Count == 0) return;
								
				//List<Target> targets = tacticalStation.GetTargets();
				tacticalStation.ClearTargets();
								
				for (int j = 0; j < contacts.Count; j++)
				{
					// entityKey will usually be the ID of the target Entity (aka Droid or Ship).  But not always.  Sometimes it may be our own ship.  It depends on the specific rule.			
					string targetKey = "boid_" + contacts[j].ContactEntityArrayIndex.ToString();
					string currentKey = "boid_" + i.ToString();
					
					Policy roePolicy = new Policy();
					Query q = new Query(EntryClass.mUserDataStore);

					Rule r = new Rule("ROE - Friendly Fire", "Earth Alliance Directive 209 states Captains must not fire on Friendly forces.");

					// Condition 1 == in Spawn() we randomly assign each Boid to either 'Red' or 'Blue' factions.
					string name = "Never fire on Same Faction";
					string description = "Never fire on any Droid that is a member of our Faction.";
					 
					Condition.EVAL_TYPE eval = Condition.EVAL_TYPE.NOT_EQUALS;
					string operandLeft = "faction";
					string operandRight = "faction";  
						
					Condition condition = new Condition(name, description, currentKey, targetKey, eval, operandLeft, operandRight);
											
					r.Add(condition);

					// Condition 2 == This Entity is not currently fighting us or one of our Allies in the arena
					eval = Condition.EVAL_TYPE.EQUALS;
					operandRight = "false";
					
					object[] delegateArgs = new object[]{currentKey, targetKey};
					condition = new Condition(name, description, targetKey, currentKey, eval, IsCombatant, operandRight, delegateArgs);
					r.Add(condition);
					q.Add(r);
					roePolicy.Add(q);
			
					SensorContact currentContact = contacts[j];
					
					//Console.WriteLine("DoTargetPrioritization - PRE- roePolicy.Execute()" );
					if (roePolicy.Execute())
					{
						// Targets are those SensorContacts that friendly forces will potentially fire upon.
						// Whereas SensorContacts is all contacts regardless of FoF status.
						Target t = new Target();
						t = current.GetTarget(currentContact.ContactEntityArrayIndex);
						if (t.Equals(default(Target)))
						{

						}
						else 
						{
							t.TargetedBy = Utils.ArrayAppend(t.TargetedBy, (int)i);       // other Ships/Vehciles/Entities, ground radars, factions, etc that are targeting this Target
						}
						t.EntityArrayIndex = currentContact.ContactEntityArrayIndex;
						t.WeaponsAssigned = null;
						t.Status = Target.STATUS.Active;
						t.CrewStatus = Target.CREWSTATUS.Alive;
						t.Hitpoints = 20;        // Boids[c.ContactIndex].Hitpoints; // max hitpoints of target... should a Sensor be able to know this exact number?  It's really just a game thing and maybe we should just use visual observations of condition of ship instead
						t.CurrentHitPoints = 18; // Boids[c.ContactIndex].CurrentHP ; // used to determine % damage of Target

						tacticalStation.Add(t);
						Console.WriteLine("DoTargetPrioritization() - Rules of Engagement POLICY PASSED. Target added.");
						
					}
					else
					{
						Console.WriteLine("DoTargetPrioritization() - Rules of Engagement POLICY FAILED.");
					}
				}
			});
			
			
			// a carrier with very few fighters remaining might be a low tactical threat
			// but high strategic threat... 
			// if a carrier is a primary mission objective that should increase its priority when scoring
			
			// a ship that is a primary target, but is not fleeing, can perhaps be scored lower since there will be time
			// to target it later if there are more dangerous threats to deal with first.
			// if a primary target is attempting to escape, the ETA that it will reach an escape trajectory should be used
			// to wieght it's prioritization score
			
			// NPC non-jobs 
			// - read
			// - study for promotion
			// - train for promotion
			// - play cards, board games
			// - socialize at cantina
			// - meditate, spirtual seeking/studying
			// - network with the crew
			// - listen to music
			// - play music/instrument
			// - art (painting, sculpting, writing poetry, 
			// - excercise, yoga, batleth*,  
			// - sparring
			// - theater (performances, orchestras, bands, etc)
			// - nap/sleep
			// Console.WriteLine("End target prioritization...");
		}
		
		/// <summary>
		/// Loop through all Components and set the Runtime flags that determine if this component/device is ready for use
		/// NOTE: Using Data Oriented Processing takes some getting used to if you are more familiar with OOP where you iterate
		/// through all Entities and update every aspect of that Entity all in once swoop before moving on to the next.
		/// Here you will see, we update each Entity piecemeal, but we perform all the same piecemeal updates to each Entity
		/// in one loop which is VERY cache friendly and yields supperior performance over the typical OOP method.
		/// </summary>
		private void DoDeviceReadyStatus()
		{
			return;
			// TODO: fix indices and such
			
			ComponentStore<LifeForm> allLivingEntities = EntryClass.mCStoreCol.CheckOut<LifeForm>(0);
			ComponentStore<Component> allComponents  = EntryClass.mCStoreCol.CheckOut<Component>(0);
			ComponentStore<TacticalStation> allTacticalStations  = EntryClass.mCStoreCol.CheckOut<TacticalStation>(0);
				
			// TODO: Do these .Is****  functions need to be setting mRuntimeFlags?
			int count = (int)allTacticalStations.Count;
            System.Threading.Tasks.Parallel.For(0, count, i => 		
			{
				
				Boid droid = (Boid)EntryClass.bSim.Boids[allComponents.Span[(int)i].EntityArrayIndex];
				
				string errorReason;
				if (allComponents.Span[(int)i].DoIsOperatorStatusCheckOK(out errorReason))
				{
					//int livingEntIndex;
					//Memory<LivingEntity> livingEntity = (Memory<LivingEntity>) droid.GetUserStruct(typeof(LivingEntity), out livingEntIndex); //"HelloBoids.LivingEntity"); //();

					if (allComponents.Span[(int)i].DoIsPowered(out errorReason))
					{
						if (allComponents.Span[(int)i].DoIsHealthyEnough(out errorReason))
						{
						}
					}
				}
			});			
		}
		
		private void DoStationCanActStatus()
		{
			ComponentStore<LifeForm> allLivingEntities = EntryClass.mCStoreCol.CheckOut<LifeForm>(0);
			ComponentStore<Component> allComponents  = EntryClass.mCStoreCol.CheckOut<Component>(0);
			ComponentStore<TacticalStation> allTacticalStations  = EntryClass.mCStoreCol.CheckOut<TacticalStation>(0);
			
			int recordCount = (int)allTacticalStations.Count;
            System.Threading.Tasks.Parallel.For(0, recordCount, i => 		
			{
				string errorReason;
				if (allTacticalStations.Span[i].CanAct(out errorReason))
				{
					// TODO: Do these .Is****  functions need to be setting mRuntimeFlags?
				
				}
			});
		}
		
		
		/// <summary>
		/// based on policies
		/// </summary>
		private void DoEnableDisableSensors()
		{
			// TODO: this comment doesnt belong here, but for now remember
			// HELM station would be influenced by Orders, Mission and Posture for example
			// if ordered to defend another ship, helm would try to maneuver such that this ship
			// is physically located between the ship-to-defend and a threat
			
			
		}
		
		/// <summary>
		/// A callback function for a Rule 'Condition.'
		/// NOTE: If we need to use a callback function, there is no need to evaluate
		///       a left and right 'operand' because it can all be done here using
		///       the passed in object[] args.
		/// </summary>
		private bool IsCombatant(object[] args)
		{
			// todo: we need both the key of the tacticalstation (currently just the current Droid)
			//       and the potential target contact key and index
			//Console.WriteLine("IsCombatant() - Begin Parse Keys");
			string currentKey = (string)args[0];
			string[] sp = currentKey.Split("_");
			int currentEntityArrayIndex = int.Parse(sp[1]);
			//Console.WriteLine("IsCombatant() - Parsed Current index == " + currentEntityArrayIndex.ToString());
			
			string targetKey = (string)args[1];
			sp = targetKey.Split("_");
			int targetEntiyArrayIndex = int.Parse(sp[1]);
			//Console.WriteLine("IsCombatant() - Parsed Target index == " + targetEntiyArrayIndex.ToString());
				
			Boid B = (Boid)Boids[currentEntityArrayIndex];
			
			EntityNode tactical = GetTacticalStations(currentEntityArrayIndex)[0];    
			// UserData data = tactical.BlackBoardData; // station operator
					
			
			// the tacticalStation will have it's list of Contacts and Targets 
			List<SensorContact> contacts = tactical.GetSensorContacts();
			int count = 0;
			if (contacts != null) 
				count = contacts.Count;
			
			//Console.WriteLine("IsCombatant() - tacticalStation '" + tactical.EntityKey + "' has '" + count + "' contacts.");
			
			for (int i = 0; i < count; i++)
				if (contacts[i].ContactEntityArrayIndex == targetEntiyArrayIndex)
				{
				}
			
			
			
			return false;
		}
			
		/// <summary>
		/// based on policies
		/// </summary>	
		private double[] DoWeaponFitnessScores(EntityNode ship, EntityNode target)
		{
			//Console.WriteLine("DoWeaponFitnessScores()");
			// NOTE: weapon fitness scores of friendlies can be combined into one table to 
			// determine how to coordinate firepower on various ships during combat
			
			// todo: we should estimate min, average, and max damage that each weapon might have against a particular target
			
			if (ship == null || target == null) 
			{
				//Console.WriteLine("DoWeaponFitnessScores() - paramters 'ship' or 'target' is null.");
				return null;
			}
			
			// the different structs used for a "Laser" component 
			//todo: i should pass the array of Memory<T> and the indices arrays
			int componentIndex;
			Memory<Component> component = (Memory<Component>)ship.GetUserStruct(typeof(Component), out componentIndex);
			int wepIndex;
			Memory<Weapon> wep = (Memory<Weapon>)ship.GetUserStruct(typeof(Weapon), out wepIndex);
			int laserIndex; 
			Memory<Laser_Struct> laser = (Memory<Laser_Struct>)ship.GetUserStruct(typeof(Laser_Struct), out laserIndex);
			
			// todo: we need just all the weapons from this particular ship.  
			// ComponetStore<Weapon> would contain ALL for ALL ships
			ComponentStore<Weapon> allWeapons = (ComponentStore<Weapon>)EntryClass.mCStoreCol.CheckOut<Weapon>(0);
			
			// return just the ones for this ship... maybe add a new function and not just GetView()
			Memory<Weapon> allWeaponsForThisShip = null; // allWeapons.GetView(ship.SpanIndex); 

			uint numRules = 3;
			uint numWeapons = (uint)allWeaponsForThisShip.Span.Length;
			double[] scores =  new double[numWeapons];
			double[] weights = new double[numRules];
				
			weights[0] = 2;
			weights[1] = 5;
			weights[2] = 0;
			
			for (int i = 0; i < numWeapons; i++)
			{
				// todo:  is the weapon available? does it need to aim at target? has it been doing so already? time for turret to rotate towards target
				if (allWeaponsForThisShip.Span[0].CoolDown == 0)  // if coolDown != 0 then the fitness score should just be 0?
				{
					scores[i] = 0;
				}
				else
				{
					scores[i] = (allWeaponsForThisShip.Span[0].AverageDamage * weights[0]) * (laser.Span[0].PowerReqt * weights[1]);
				}
			}
			
			return scores;
		}
		
		
		private void DoWeaponsCanFire()
		{
			ComponentStore<Component> allComponents  = EntryClass.mCStoreCol.CheckOut<Component>(0);
			ComponentStore<Weapon> allWeapons  = EntryClass.mCStoreCol.CheckOut<Weapon>(0);
			
			// NOTE: we really want to avoid having to reference a Droid from the array as it 
			//       impacts our cache coherency
			//EntityNode ent = (EntityNode)EntryClass.bSim.Boids[droidIndex];
			int recordCount = (int)allWeapons.Count;
            System.Threading.Tasks.Parallel.For(0, recordCount, i => 		
			{
				string errorReason;
				// TODO: timerID must consistantly use same struct LifeForm id or something else
				EntityNode boid = Boids[allWeapons.Span[(int)i].EntityArrayIndex - BoidSimulation.LASER_OFFSET];
				string entityKey = boid.EntityKey;
				EntityNode weapon = Boids[allWeapons.Span[(int)i].EntityArrayIndex];
				string weaponKey = weapon.EntityKey;
				//Console.WriteLine ("DoWeaponsCanFire() - Weapon Entity Key = " + weaponKey);
				
				bool canFire = false;
				
				uint USER_RUNTIME_FLAG_1 = 1 << 0;
				uint USER_RUNTIME_FLAG_2 = 1 << 1;
				uint USER_RUNTIME_FLAG_3 = 1 << 2;
				uint USER_RUNTIME_FLAG_4 = 1 << 3;
				uint USER_RUNTIME_FLAG_5 = 1 << 4;
				uint USER_RUNTIME_FLAG_6 = 1 << 5;
				uint USER_RUNTIME_FLAG_7 = 1 << 6;
				uint USER_RUNTIME_FLAG_8 = 1 << 7;
				
				uint USER_STRUCT_FLAG_1 = 1 << 0;
				uint USER_STRUCT_FLAG_2 = 1 << 1;
				uint USER_STRUCT_FLAG_3 = 1 << 2;
				uint USER_STRUCT_FLAG_4 = 1 << 3;
				uint USER_STRUCT_FLAG_5 = 1 << 4;
				uint USER_STRUCT_FLAG_6 = 1 << 5;
				uint USER_STRUCT_FLAG_7= 1 << 6;
				uint USER_STRUCT_FLAG_8 = 1 << 7;
				
				bool flagValue = canFire;
				
				int componentIndex;
				Memory<Component> compStruct = (Memory<Component>)weapon.GetUserStruct(typeof(Component), out componentIndex);
				compStruct.Span[0].SetUserStructFlag(USER_STRUCT_FLAG_1, flagValue);
				bool hasStruct = compStruct.Span[0].GetUserStructFlag(USER_STRUCT_FLAG_1);
				compStruct.Span[0].SetUserRuntimeFlag(USER_RUNTIME_FLAG_1, flagValue);
				bool hasRuntimeFlag = compStruct.Span[0].GetUserRuntimeFlag(USER_RUNTIME_FLAG_1);
				
				int weaponIndex;
				Memory<Weapon> weaponStruct = (Memory<Weapon>)weapon.GetUserStruct(typeof(Weapon), out weaponIndex);
				
				//weaponStruct.Span[0].SetUserStructFlag(USER_STRUCT_FLAG_1, flagValue);
				//bool hasStruct = weaponStruct.Span[0].GetUserStructFlag(USER_STRUCT_FLAG_1);
				//weaponStruct.Span[0].SetUserRuntimeFlag(USER_RUNTIME_FLAG_1, flagValue);
				//bool hasRuntimeFlag = weaponStruct.Span[0].GetUserRuntimeFlag(USER_RUNTIME_FLAG_1);
				
				try
				{
					// todo: is it better to use mIntervalTimers here than to implement checks elsewhere?
					canFire = mIntervalTimers.IsReady(weaponKey, "droid_canfire");
					//Console.WriteLine("DoWeaponsCanFire() - Droid " + weaponKey + " Can Fire = " + canFire.ToString());
					if (canFire)
					{	
						//Console.WriteLine("DoWeaponsCanFire() - Droid " + weaponKey + " FIRING!!!");
						// set the runtime flag
						//bool suspend = true;  // we do not want this timer to start over until we start it again. <-- Wait, why?  Is this not just a cooldown?
                		//mIntervalTimers.Reset(weaponKey, "droid_canfire", suspend);
						mIntervalTimers.Reset(weaponKey, "droid_canfire");
					}
					
					// set the GAME SPECIFIC runtime flag
					// the runtime flags can only be in Entity or in Component.  It should not be in the various structs
					// themselves, because it needs to affect ALL structs and we dont want to manage a copy of those across
					// every flag OBVIOUSLY.
					
				}
				catch (Exception ex)
				{
					Console.WriteLine("DoWeaponsCanFire() - droid_canfire " + weaponKey + " key does not exist");
				}
				
				// TODO: Do these .Is****  functions need to be setting mRuntimeFlags?
			});
		}
		
		public struct HIT 
		{
			public EntityNode Attacker;
			public EntityNode Target;
			public EntityNode Owner;            // if an assembly, component or operator, Owner is the Starship or Droid that is hosting them.
			public EntityNode WeaponUsed;
			public Vector3d Location;           // the impact point in worldspace on the targeted ship/droid/component/operator/world/colony/etc
			public double DistanceSquared;
		}
		
		
		// NOTE: This only applies for FTL weapons... "CanHit()" must be different for Missiles, Kinetic Energy Weapons and Particle Weapons that are slower than light
		public bool HitHasOccurred(EntityNode attacker, EntityNode target, double distanceToTargetSquared, EntityNode weaponEntity, GameTime gt, Random rand, out HIT[] hits)
		{
			bool result = false;
			
			//TODO: find the actual target that was hit... we may be aiming for an assembly or component and may hit something different, such as a different Component or Operator or even a different Starship or Droid or NOTHING
			Vector3d start = attacker.Translation;   // todo: if hierarchical and if this is the Weapon and not the Droid, it should be .DerivedTranslation
			Vector3d targetLoc = target.Translation; // TODO: if hierarchical, this should be .DerivedTranslation
			
			hits = new HIT[1];
			hits[0].Target = target;
			hits[0].Owner = target;  //  target is same as owner for now since target is a Boid and not the Operator or Station or Laser or Battery or Wings
			hits[0].WeaponUsed = weaponEntity; // how does this work if its an explosion or fire or radiation volume?
			hits[0].Location = targetLoc;    // the impact point
			hits[0].DistanceSquared = Vector3d.GetDistance3dSquared(start, targetLoc);
			
			
			
			// todo: for tactical station, the logic for determining hit+damage should rely on the crew station.css script and not the operator.  Instead, we just grab bonuses or minuses from the operator crew member.
			//  - time to get a lock
			//  - bonus for time 
			//  - bonus for damage
			//  and remember, it's the tactical station that keeps track of all the weapons available and the targets (including friendlies)
			
				
			// stealth
			
			// target last acquisition - previous aquisition makes it easier to re-aquire
			
			// sensorLockOfTargetTimeElapsed (aka durationOfSensorAquistion) // how much time has this  target been tracked by sensors already
			
			
			
			// target distance			

			// operator skill  
			
			
			// operator Health
			
			
			// target evasive
			
			
			// target deployed counter measures within X time (time * fallOff aka call it 'attenuation')
					
			
			result = true;
			return result;
		}
		
		
		public void ProcessHits(HIT[] hits, int operatorEntityArrayIndex, int stationEntityArrayIndex, int attackingShipEntityArrayIndex, int weaponEntityArrayIndex, GameTime gt, Random random)
		{
			
			for (int currentHitIndex = 0;  currentHitIndex < hits.Length; currentHitIndex++)
			{
				EntityNode attacker = hits[currentHitIndex].Owner;
				EntityNode currentTarget = hits[currentHitIndex].Target;
				
				// NOTE: hit.Owner is always the containing Starship/Container/Vehicle/Building
				EntryClass.mUserDataStore[attacker.EntityKey].IncrementInteger("shotsfired");
				//Console.WriteLine("ProcessHits() - Attacker Droid @ Array Index '" + currentArrayIndex.ToString() + "' firing shot # " + EntryClass.mUserDataStore[attacker.EntityKey].IncrementInteger("shotsfired").ToString() + " on Droid @ Array Index '" + currentTarget.EntityArrayIndex.ToString() + "'");

				
				// TODO: QUEUE ANIMATION TO FIRE THIS WEAPON 
				// Publish a CombatEventRecord containing the 'FireAt' Attempt
				int actionID = (int)ACTIONS.FiringAt;
				CombatEventRecord r; //= new CombatEventRecord();
				r = default(CombatEventRecord);
				r.ActionID = actionID;

				//Console.WriteLine ("ProcessHits() - Publishing FiringAt 1");	
				r.Time = gt.TotalElapsedSeconds;
				r.OfficerArrayIndex = operatorEntityArrayIndex;    // Attacking vessel's acting Tactical Officer
				r.StationArrayIndex = stationEntityArrayIndex;     // Attacking vessel Tactical Station
				r.ShipArrayIndex = attackingShipEntityArrayIndex;       // Attacking vessel
				r.WeaponArrayIndex = weaponEntityArrayIndex;       // Attacking vessel's weapon used

				r.TargetArrayIndices = new int[]{currentTarget.EntityArrayIndex};
				//Console.WriteLine ("ProcessHits() - Publishing FiringAt 1b");	
				r.TargetOwnerArrayIndices = GetOwner(r.TargetArrayIndices);
				//Console.WriteLine ("ProcessHits() - Publishing FiringAt 2");	
				r.HitPoints = GetHitPoints(r.TargetArrayIndices);
				//Console.WriteLine ("ProcessHits() - Publishing FiringAt 3");	
				r.Damage = null;

				mSimEventManager.PublishEvent(attacker, actionID, r);

				// NOTE: here we assume the Fire() occurs immediately using a lightspeed laser and the damage is instantaneous 
				//       and does not need any travel time to reach the currentTarget
				object[] damages = null;

				try 
				{
					// todo: change parameter attacker to tacticalStation?
					// todo: randomly choose between 
					// battery, opticalsensors, wings, laser, overall droid, tacticalstation or operator
					EntityNode stationOperator = Boids[operatorEntityArrayIndex];
					EntityNode specificSubTarget = hits[currentHitIndex].Target;
					EntityNode weaponEntity = Boids[weaponEntityArrayIndex];
					
					int componentIndex;
					int weaponIndex;
					Memory<Component> componentStructForWeaponEntity = (Memory<Component>)weaponEntity.GetUserStruct(typeof(Component), out componentIndex);
					Memory<Weapon> weaponStruct = (Memory<Weapon>)weaponEntity.GetUserStruct(typeof(Weapon), out weaponIndex);
					
					bool critMalfunctionHasOccurred;
					
					// NOTE: if damages occurs, there can be multiple TYPES of damages in the return damages[] because a single target 
					//       may for example receive kinetic damage AND on-going fire damage, and/or other damages.
					damages = CalculateDamage(stationOperator, specificSubTarget, hits[currentHitIndex].DistanceSquared, componentStructForWeaponEntity, weaponStruct, gt, random, out critMalfunctionHasOccurred); // <-- returns 1 or more Products (eg Damage eg: impaling damage and/or DamageOverTime eg fire damage until fire is extinguished)

					//TODO: IF 0 Damage occurs because the Target was able to resist the attack with armor or passive defenses
					//      the result of damage should return 0 and not NULL or anything because resisting an attack is valid information to know in an event log
					//
					//int dCount = 0;
					//if (damages != null)
					//	dCount = damages.Length;
					//
					// Console.WriteLine("ProcessHits() - Damages Produced = " + dCount.ToString());
				}
				catch(Exception ex)
				{
					Console.WriteLine ("ProcessHits() - CaculateDamage ERROR - " + ex.Message);	
				}

				if (damages != null)
				{
					int[] damageAmounts = new int[damages.Length];

					for (int j = 0; j < damages.Length; j++)
					{
						if (damages[j] is DamageSystem.Damage)
						{
							mDamageSystem.Add((DamageSystem.Damage)damages[j]);
							damageAmounts[j] = ((DamageSystem.Damage)damages[j]).Amount;
						}
						else if (damages[j] is DamageOverTimeSystem.DamageOverTime)
						{
							mDamageOverTimeSystem.Add ((DamageOverTimeSystem.DamageOverTime)damages[j]);
							damageAmounts[j] = ((DamageOverTimeSystem.DamageOverTime)damages[j]).Amount;

						}
						else 
							throw new Exception("ProcessHits() - Unexpected Damge type. " + damages[j].GetType().Name);
					}

					
				
					
					
					// Console.WriteLine ("ProcessHits() - Publishing HIT RESULTS - 1");	
					// Publish a CombatEventRecord containing the Hit Results
					actionID = (int)ACTIONS.TargetHit;

					r = default(CombatEventRecord);
					r.ActionID = actionID;
					r.Time = gt.TotalElapsedSeconds;
					r.OfficerArrayIndex = operatorEntityArrayIndex;      // Attacking vessel's acting Tactical Officer
					r.StationArrayIndex = stationEntityArrayIndex;       // Attacking vessel Tactical Station
					r.ShipArrayIndex = attackingShipEntityArrayIndex;    // Attacking vessel

					r.WeaponArrayIndex = weaponEntityArrayIndex;         // Attacking vessel's weapon used
					r.TargetArrayIndices = new int[]{currentTarget.EntityArrayIndex};
					r.TargetOwnerArrayIndices = GetOwner(r.TargetArrayIndices);	
					r.HitPoints = GetHitPoints(r.TargetArrayIndices);
					//Console.WriteLine ("ProcessHits() - Publishing HIT RESULTS - 2");	
					r.Damage = damageAmounts;
					mSimEventManager.PublishEvent(attacker, actionID, r);
				}
			}
		}
		
		/// <summary>
		/// The resulting damage types and amounts (and duration for damage that can be applied overtime)
		/// that occur on this successful hit.
		/// </summary>
        private object[] CalculateDamage(EntityNode attackerOperator, EntityNode target, double distanceSquared, Memory<Component> componentStruct, Memory<Weapon> weaponStruct, GameTime gt, Random rand, out bool criticalMalfunctionHasOccurred)
        {
			// 1 - [DONE] - Calc Malfunction
			// 2 - Distance effect on Damage (laser attenuation/falloff) 
			//     - for kinetic say a ballistic projectile or catapult bolt in atmosphere... depends on atmosphere and perhaps gravity too \
			//     - for laser, inverse square law 'intensity = intensity * ( 1 / d^2)' where for instance double the distance == 1/4 the intensity of the beam
			//      if (distance > falloffStart) 
			//		{
    		//			float damageReduction = (distance - falloffStart) * damageDropPerUnit;
    		//			currentDamage = Math.Max(minDamage, baseDamage - damageReduction);
			//		}
		
			// 3 - Recursive / Cascading / Chain-Reaction Damage
				// - PRODUCTION & CONSUMPTION should be used for propgating things like Fire and Radiation right?
			//		- its kind of like cellular automata though isn't it? if the 
			
				// TODO: so for chained / recursive / cascading damage, where should we initiate that?
					// We do know for an Explosion, an explosion ENtity can be retreived from an ObjectPool
					// and then added to the Scene.  That Entity can be flagged as a MissionObject perhaps?
					// 
					// In KGB for Interiors, we can use our TileMaps and search x distance away using floodfill
					// In space and in HelloBoids, we can use 
					//  a) a bigger sub-set of the adjacents rather than one target to HasHitOccurred()... include the desired target along with some adjacents within X range of the Target perhaps.
					//  b) we still need this sub-set for our Sensor detection where ships can mask their signatures somewhat by flying in formation in a column (from 
					//     the target's point-of-view) towards the the target.
					//  c) or we just re-search over again with Octree to find new adjacents... or
					//     again, we can use the SensorContact data...
			
					// EntityPool for things like Explosions, RadiationFields, etc
			
			// 4 - variances for spawned Droid Size
			// 5 - randomness of skills of operators
			// 6 - armor of the Droid randomness based on the size of the Droid
			
			// 7 - armor option for Operators
			// 8 - destruction of Droids upon lose of hitpoints
			// 9 - double buffering of Data
			// 10 - finish Statistics and Policies
			// 11 - class Builder 
			
			
			// https://panoptesv.com/RPGs/Equipment/Weapons/BeamWeapons.php?HR=0
			// https://gamedev.stackexchange.com/questions/148961/how-to-design-a-damage-formula-in-an-rpg-which-keeps-weapons-with-different-atta
			
			
			//Console.WriteLine("CalculateDamage() - Begin.");
			System.Diagnostics.Debug.Assert (attackerOperator.Configuration == (uint)HumanOperatorConfiguration, "CalculateDamage() - AttackerOperator is of incorrect CONFIGURATION.");
										 
			// TODO: I think we want to have all relevant data on attacker and target
			// for instance
			// TacticalStation used
			// Operator of Tactical Station
			// time of event
			// Target Vehicle
			//  specific sub-location of target aimed at
			//  specific sub-location of target hit
			
			
			// note: this will be different if a MINE or AREA EFFECT damage occurs
			// and there are multiple targets and multiple sub-locations on the target(s) that are damaged.
	
			// todo: so TacticalStation stores the stats correct?
			//       Well, we have dedicated EntryClass.Statistics now
	
			//string factionColor = "Red";
			//factionColor = (rand.NextDouble() >= 0.5d) ? "Red" : "Blue";
			//b.BlackBoardData.SetString("faction", factionColor);
				
			
			/*
			Production laserDamage;
			laserDamage.Amount = 5;
			laserDamage.DistributionList = null;
			laserDamage.EntityID = droid.Index;
			laserDamage.Location = Vector3d.Zero();
			laserDamage.ProductID = (int)PRODUCTS.MicrowaveDamage;
			laserDamage.SearchPrimitive = null;
			laserDamage.Value = 1;
			
			result[0] = laserDamage;
			*/
			
			criticalMalfunctionHasOccurred = false;
			bool malfunction = CalculateMalfunction(componentStruct, weaponStruct, rand, out criticalMalfunctionHasOccurred);
			
			if (criticalMalfunctionHasOccurred)
			{
				// the weapon has failed in a critical way.  Damage may occur to the operator (if the weapon is handheld or loaders are nearby)
				// or it may cause damage to any assemblies or components near it.
				
				return null;
			}
		
			
			
			// weapon %power of maxpower being used vs weapon output
			
			
			
			
			// weapon Hitpoints - damage percent to weapon determines if increased malfunction and decreased accuracy
			
			
			
			int lfIndex = -1;
			Memory<LifeForm> targetLF = (Memory<LifeForm>)attackerOperator.GetUserStruct(typeof(LifeForm), out lfIndex);

			
			// todo: the weapon's actual damage needs to be a result along a bell curve of the average Damage
			// https://gamedev.stackexchange.com/questions/198751/how-to-calculate-player-damage-in-a-game
			// https://gamedev.stackexchange.com/questions/154920/browser-rpg-fight-calculation-formula/154927#154927  <- one user's opinion on why the 'luck' mechanic shouldn't be used
			double damageAmount = weaponStruct.Span[0].AverageDamage;
			double variancePercentage = 0.10; // 10%
			double damageAmountWithVariance = Utils.RandomWithVariance(rand, damageAmount, variancePercentage);
			
			// distance based falloff
			// - kinetic energy in atmosphere
			// - lasers in atmosphere
			// - lasers in vacuum.
			double fallOffSquared = weaponStruct.Span[0].FallOffStart * weaponStruct.Span[0].FallOffStart;
			if (distanceSquared > fallOffSquared) 
			{
				damageAmountWithVariance *= 0.5d; // initial drop is half of the damageAmountWithVariance (we might want to compute this prior to factorinig in variance)
    			
				double damageDropPerUnit = 0.5d; // keep in mind we are using distances SQUARED so we may need to half these values 
				damageAmountWithVariance = (distanceSquared - fallOffSquared) * damageDropPerUnit;
    			
			}
			
			
		
				
			
			
			// critChance is variable based on operator skill, factor is tweakable.
			// the higher the "factor" and "critChance" (exponent), the smaller the resulting
			// Pow() expression will result which will make rand.NextDouble() increasingly
			// MORE LIKELY to be a higher value thus resuling in a CRITICAL HIT.
			double critChance = 2.0d; // EXPONENT - todo: this should be based on the weapon and the skill of the operator
			double factor = 1.25d; // factor of 1 or less will result in there NEVER being a critical hit
			
			// 0% at luck = 0 and approaches 100% as luck goes to infinity.
			bool isCriticalHit = rand.NextDouble() > System.Math.Pow(factor, -critChance); // rand.NextDouble() will be in range [0.0, 1.0]
			// Math.Pow(2.71, -2) == 1/2.71^2  ==  1/7.344 == 0.13616371099249738
			
			
			double critMultiplier = 2;
			if (isCriticalHit)
				damageAmountWithVariance *= critMultiplier;
			
		
			// target Armor  //targetFL.Span[0].Armor.Armor[side].
			int defense = targetLF.Span[0].Armor.AverageDR;
			// if the defense is higher than the damage, then 0 damage gets through.  Math.Max() will prevent any "negative" damage in that case.
			double finalDamageAmount = Math.Max(0, damageAmountWithVariance - defense); 
									
			// NOTE: Damage amount generated. 
			Console.WriteLine ("CalculateDamage() - Result == " + finalDamageAmount.ToString() + " (Target Average Defense == " + defense.ToString() + " Weapon Attack Value == " + damageAmountWithVariance.ToString() + " Critical Hit == " + isCriticalHit.ToString() + ")");
			double time = gt.TotalElapsedSeconds;
	
						
			// ------------------------------------------
			object[] result = new object[2];
			
			DamageSystem.Damage d;
			d.AttackerOperatorEntityArrayIndex = attackerOperator.EntityArrayIndex;
			d.WeaponUsedEntityArrayIndex = weaponStruct.Span[0].EntityArrayIndex;
			d.TargetEntityArrayIndex = target.EntityArrayIndex;
			d.Amount = (int)finalDamageAmount;
			d.TimeOfAttack = time;
			result[0] = d;
			
		
			// TODO: what if we were to make and add multiple DamageSystem.Damage records instead
			//       and execute them no earlier than their "TimeOfAttack?"  This way we wouldn't 
			//       need a seperate System for the two,we would just need to only execute them
			//       when the "TimeOfAttack was <= gt.GetTime();
			//       
			DamageOverTimeSystem.DamageOverTime dot;
			dot.AttackerOperatorEntityArrayIndex = attackerOperator.EntityArrayIndex;
			dot.WeaponUsedEntityArrayIndex = weaponStruct.Span[0].EntityArrayIndex;
			dot.TargetEntityArrayIndex = target.EntityArrayIndex;
			dot.Amount = 1;  // weaponStruct.BeamOutput;
			dot.TimeOfAttack = time;
			dot.Duration = 0.05f;
			result[1] = dot;
			
			//see Keystone.Game01.Messages.   public class AttackResults since
			// we need results going over the network
			return result;
        }

        private bool CalculateMalfunction (Memory<Component> componentStruct, Memory<Weapon> weaponStruct, Random rand, out bool criticalMalfunctionHasOccurred)
		{
			/*
			In GURPS Vehicles 2nd Edition, weapon malfunction (Malf) rates are primarily determined 
			by the weapon's Tech Level (TL) and construction quality, with most standard weapons 
			having a Malf of 16 or 17. A failure (roll > skill) triggers a Malf check if the roll 
			also exceeds the weapon's Malf number (e.g., 17 or 18).
			
			Standard Reliability: Most modern 
			(TL7-8) weapons have a Malf of 17, meaning a malfunction occurs on a 17 or 18.
			
			High Reliability: Very reliable weapons (e.g., some TL8) might only malfunction on a 18, 
			or only during a critical failure.Poor Conditions: Lack of maintenance, mud, or water can 
			reduce a weapon's Malf number (e.g., to 15 or 16), making jams more frequent.
			
			Quality Modifiers: Fine-quality weapons can improve reliability by 1 or more, while cheap
			weapons may see it reduced.Vehicle-Mounted Weapon Malf: Generally, vehicle weapons use 
			these same standard Malf numbers, often interpreted as needing a 17+ or 18 to fail on a 
			sustained fire roll, especially for high-volume weapons like autocannons.A malfunction 
			usually requires a "Ready" maneuver to clear (stoppage) or more severe repairs for a 
			broken weapon
			*/
			
			criticalMalfunctionHasOccurred = false;
			
			// our LEVELs will be floating points and allow for 0.1, 0.2, ... 2.5...etc -> 10.0
			const double MAX_LEVEL = 10d;
			// malfChance is variable based on weapon craftsmanship, factor is tweakable.
			// the higher the "factor" and "malfChance" (exponent), the smaller the resulting
			// Pow() expression result which will make rand.NextDouble() increasingly
			// MORE LIKELY to be a higher value thus resuling in a MALFUNCTION.
			double weaponQualityCoefficient = componentStruct.Span[0].MaterialQuality;   
			double weaponDamageCoefficient = componentStruct.Span[0].HitPoints.Base > 0 ? componentStruct.Span[0].HitPoints.Current / componentStruct.Span[0].HitPoints.Base : 0;
			double weaponLevelCoefficient = componentStruct.Span[0].Level / MAX_LEVEL;
			double weaponCraftsmanshipCoefficient = componentStruct.Span[0].Craftsmanship;
			
			// multiply all coefficients together
			double combined = weaponQualityCoefficient * weaponDamageCoefficient * weaponLevelCoefficient * weaponCraftsmanshipCoefficient;
						
			
			// range [0.001 - 1.0]  the greater the value, the more likely like a malfunction will occur 
			double malfChance = 1.0 - weaponQualityCoefficient;  // EXPONENT - todo: this should be based on the weapon Level and craftsmenship of the of the Weapon
			
			// range [0.01 - 1.00]
			double factor = 0.9d; // the lower the value, the greater the chance of a malfunction

			bool malfunctionOccurred = rand.NextDouble() > System.Math.Pow(factor, -malfChance); // rand.NextDouble() should always be in range [0.0, 1.0]
			// Math.Pow(factor, -malfChance) == Math.Pow(0.9 -(.25)) == 1 / (0.9^0.25)  ==  1 / 0.97400374642529676442270619639968 ==  1.0266900960803409723972392556152

			if (malfunctionOccurred)
			{
				double criticalMalfunctionThreshold = 0.91d;
				double variancePercentage = 0.05; // 5%
				criticalMalfunctionThreshold = Utils.RandomWithVariance(rand, criticalMalfunctionThreshold, variancePercentage);
				
				if (rand.NextDouble() > criticalMalfunctionThreshold)
					criticalMalfunctionHasOccurred = true;
			}			

			return malfunctionOccurred;
		}
		
#region Consumption and Production
		
	
		private void ProcessPowerConsumption(ComponentStore<Consumption> consumptionStore, object[] parameters, int seed, GameTime gt)
		{
			uint consumptionCount = consumptionStore.Count;
			uint productID = (uint)parameters[0];
			
			ComponentStore<Production> production = (ComponentStore<Production>)parameters[1];	
			uint productionCount = production.Count;
			
			//Console.WriteLine("ProcessPowerConsumption() - Producing ProductID == '" + ((PRODUCTS)productID).ToString() + "  Production Count == " + productionCount + " Consumption Count == " + consumptionCount);
			
			// LOOP THROUGH ALL COMPONENTS THAT ARE PRODUCING PRODUCTS.ElectricalPower
			System.Threading.Tasks.Parallel.For(0, productionCount, i =>
			//for (int i = 0; i < productionCount; i++)
			{
				Span<Consumption> allConsumptions = consumptionStore.Span;
				
				if (production.Span[(int)i].Breaker == false) return;
				
				// NOTE: by using production.Span[i], we never have to COPY the struct 
				
				//Console.WriteLine("ProcessPowerConsumption() - Entity '" + Boids[currentProduction.ProducerEntityArrayIndex].EntityKey + "' Producing '" + ((PRODUCTS)productID).ToString() );
			    
				
				int[] distributionList = production.Span[(int)i].Consumers;
				if (production.Span[(int)i].DistributionMode != PRODUCT_DISTRIBUTION_TYPE.List && production.Span[(int)i].DistributionMode != PRODUCT_DISTRIBUTION_TYPE.SingleItem)
				{
					
					if (production.Span[(int)i].DistributionMode == PRODUCT_DISTRIBUTION_TYPE.Region)
					{
						// find PowerConsumers that are inside of this entire Region (eg onboard the Starship)
						throw new NotImplementedException("ProcessPowerConsumption() - DistributionMode '" + production.Span[(int)i].DistributionMode.ToString() + "' NOT YET SUPPORTED.");
					}
					else if (production.Span[(int)i].DistributionMode == PRODUCT_DISTRIBUTION_TYPE.Zone)
					{
						// find PowerConsumers that are inside of this entire Zone (eg inside the current star system)
						throw new NotImplementedException("ProcessPowerConsumption() - DistributionMode '" + production.Span[(int)i].DistributionMode.ToString() + "' NOT YET SUPPORTED.");
					}
					else if (production.Span[(int)i].DistributionMode == PRODUCT_DISTRIBUTION_TYPE.BoundingBox)
					{
						// find PowerConsumers within this bound volume
						//Console.WriteLine ("SearchReferenceEntity is SET AND VALID == " + (production.Span[(int)i].SearchReferenceEntity != null).ToString());
						BoundingBox searchBox = (BoundingBox)((EntityNode)production.Span[(int)i].SearchReferenceEntity).BoundingBox;
						searchBox = new BoundingBox(searchBox.Center, Utils.GetMax(EntryClass.bSim.SeparationDistance, EntryClass.bSim.AlignmentDistance, EntryClass.bSim.CohesionDistance));
						
						double maxDistanceSquared = 1;//searchBox.RadiusSquared;
						
						Func<EntityNode, EntityNode, Tuple<bool, double>> match = (current, neighbor) =>  {
							if (current == neighbor || !neighbor.HasConfiguration((uint)CONFIGURATION.PowerUsing)) return new Tuple<bool, double>(false, -1);
                			//if (current == neighbor) return new Tuple<bool, double>(false, -1);
							double distanceSquared = Vector3d.GetDistance3dSquared(neighbor.Translation, current.Translation);
							if (distanceSquared <= maxDistanceSquared) return new Tuple<bool, double>(true, distanceSquared);
                			return new Tuple<bool, double>(false, -1);
           					 };  
							
						List<Tuple<EntityNode, double>> found = EntryClass.bSim.FindNearestTarget((EntityNode)production.Span[(int)i].SearchReferenceEntity, searchBox, match);
						
						if (found == null) return;
						Console.WriteLine("ProcessPowerConsumption() - Found count == " + found.Count.ToString());
						
						distributionList = new int[found.Count];
						for (int h = 0; h < distributionList.Length; h++)
						{
							// NOTE: This needs to be the Memory<T> index, not the Entity Array Index
							EntityNode e = found[h].Item1;
							int indexConsumer;
							Memory<PowerConsumer> consumerEntityStruct = (Memory<PowerConsumer>)e.GetUserStruct(typeof(PowerConsumer), out indexConsumer);
							distributionList[h] = indexConsumer;
						}
						
						Console.WriteLine("ProcessPowerConsumption() - Battery using SearchBox...");
					}
					else if (production.Span[(int)i].DistributionMode == PRODUCT_DISTRIBUTION_TYPE.BoundingSphere)
					{
						//BoundingSphere sphere =  (BoundingSphere)((EntityNode)production.Span[(int)i].SearchReferenceEntity).BoundingSphere;
						throw new NotImplementedException("ProcessPowerConsumption() - DistributionMode '" + production.Span[(int)i].DistributionMode.ToString() + "' NOT YET SUPPORTED.");
					}
					else if (production.Span[(int)i].DistributionMode == PRODUCT_DISTRIBUTION_TYPE.BoundingCone)
					{
						//BoundingCone cone = (BoundingCone)production.Span[(int)i].SearchPrimitive;
						throw new NotImplementedException("ProcessPowerConsumption() - DistributionMode '" + production.Span[(int)i].DistributionMode.ToString() + "' NOT YET SUPPORTED.");
					}
					else if (production.Span[(int)i].DistributionMode == PRODUCT_DISTRIBUTION_TYPE.PlanedHull)
					{
						//PlanedHull hull = (PlanedHull)production.Span[(int)i].SearchPrimitive;
						throw new NotImplementedException("ProcessPowerConsumption() - DistributionMode '" + production.Span[(int)i].DistributionMode.ToString() + "' NOT YET SUPPORTED.");
					}					
				}
				
				if (distributionList ==  null || distributionList.Length == 0) return; // use 'return' keyword if using parallel.For

				// NOTE: we are always guarnateed that any distributionList items already exist as registered
				//       Consumption for this ProductID... so we don't have to verify consumers list contains 
				//       all items within the distributionList
				//List<Consumption> consumers = mConsumption[productID];
				//if (consumers == null) continue;
				// if (!consumers.Contains(distributionList)) return;

				EntityNode producerEntity =  Boids[production.Span[(int)i].ProducerEntityArrayIndex];
				int indexProducer;
				Memory<PowerProducer> producerEntityStruct = (Memory<PowerProducer>)producerEntity.GetUserStruct(typeof(PowerProducer), out indexProducer);
				
				// Update the Producer Entity's Struct's Output, Capacity, and Duration based on Level, Damage, Efficiency
				// that may have changed since last Tick()

				//producerEntityStruct.Span[0].Level ;
				//producerEntityStruct.Span[0].Output;
				//producerEntityStruct.Span[0].Capacity;
				//producerEntityStruct.Span[0].Duration;
				//producerEntityStruct.Span[0].MaxInput;
				//producerEntityStruct.Span[0].Store = newValue;
				
				
				// fill the Production with all of the 'Output' generated and stored for this tick() and remove it from the producing Entity
				production.Span[(int)i].Store = producerEntityStruct.Span[0].Store;
				producerEntityStruct.Span[0].Store = 0;
					
				try
				{
					// LOOP THROUGH ALL CONSUMERS OF THIS CURRENT PRODUCER'S PRODUCTS.ElectricalPower
					// (NOTE: Parallelizing the INNER LOOP is typically not recommended because
					// each time the OUTER loop completes, it has to recreate all the threads
					// for the INNER LOOP.
					// Maybe this is OK for our use case since we are essentially running different
					// processing code for different PRODUCTS.  For instance, distributing PRODUCTS.ElectricalPower
					// is not the same as applying PRODUCTS.Morale or PRODUCTS.FatigueRecovery and so if we
					// were to parallelize the OUTER loop, those loops might finish in dramatically different
					// lengths of time because the work can be totally different.)
					for (int j = 0; j < distributionList.Length; j++)
					{
						// TODO: above we are iterating, but really what we want I think is to 
						//       have the Memory<Consumption<ProductID>> and the list of which elements are
						//       Then there is ZERO looping and we simply pass ProcessConsumption (currentProduction, mem);
						//       Where we have a Dictionary<> of functions that will process that change just like we do
						//       for things like FLOCKING, OPTICAL_SENSORS, LIFECYCLE
						//       We do not need all these "DamageSystem" and "HealthSystem" and such that contain a special struct for "records"
						//       as such... we just need a LIST or ARRAY like our distributinList[] that says which
						//       Memory<T> records to modify in both the Memory<Production> and Memory<Consumption>
						//       and we will probably optimize those by having Memory<Production<ProductID>> and Memory<Consumption<ProductID>>
						//       And again, each of these will contain a functin to use to handle Production and Consumption... just like 
						//       we do with OPTICAL_SENSORS and LIFECYCLE  so we just do a simple "key"  look up of the Processor function
						//       based on the ProductID.
						
						if (allConsumptions[distributionList[j]].Equals(default(Consumption)))
						{
							Console.WriteLine("ProcessPowerConsumption() - Consumption '" + distributionList[j] + "'  is not registered.");
							return;
						}

						try
						{							
							EntityNode consumerEntity = Boids[allConsumptions[distributionList[j]].ConsumerEntityArrayIndex];
							if (producerEntity == null || consumerEntity == null) return;
							
							int indexConsumer;
							Memory<PowerConsumer> consumerEntityStruct = (Memory<PowerConsumer>)consumerEntity.GetUserStruct(typeof(PowerConsumer), out indexConsumer);
							
							//todo: update the consumption[distributionList[j]] and consumerEntityStruct
							//      requirements based on any damage and thus changes to efficiency and such since last Tick()
							// todo: that should be done in DamageSystem right?						
							double diff = production.Span[(int)i].Store - allConsumptions[distributionList[j]].Amount;
							if (diff >= 0)
							{	
								production.Span[(int)i].Store -= allConsumptions[distributionList[j]].Amount;
								
								// todo:  flag the consumerEntityStruct's runtime flag "CanAct"
								
								// todo:
								// NOTE: if we need to Spawn something like say a RadiationCloud because
								// this reactor has been damaged enough to warrant it (though this would likely
								// occur within DamageProcessor and not here...) then we do things like we normally
								// do within KGB... we create a NetMessage and send it and then it gets queued 
								// and eventually the result gets returned and handled by main thread.  
								// TODO: we should also allow for spawning of many prefabs within a single NetMessage
								// Unity does something similar using the "Entity Command Buffer (ECB)" but here KGB
								// just uses one approach for ALL spawn scenarios including inside these 
								// DataOrientedProcessors... the Network Message "KeyCommon.Messages.Node_Create_Request.cs" 
								// (which can be accessible via function that handles the creation of the message packet for us)
								// because we always use loopback anyway and modify the Scene on main thread each time.
								// No special code required for us with KGB! \o/  Elegant.
								// TODO: we may add code to pool (OBJECT POOL) some Entity prefabs more easily at the start of a scene.
								
								// NOTE: In the case of a RadiationCloud, we should consider that the owner of this cloud
								//       will be the Reactor, and the RadiationCloud will own it's "Production" of
								//       PRODUCTS.Radiation and will be responsible for Registering that Production.
								//       So maybe this helps us with the idea that Entities tend to 'own' only one
								//       type of PRODUCT?  Maybe not... i mean... even a rocket plume produces
								//       PRODUCTS.Thrust and PRODUCTS.HeatSignature, PRODUCTS.HeatVolume, PRODUCTS.LightSignature
								//       (note: its always better to seperate out a product like "Heat" into the actual distinct
								//       functional roles they play... HeatSignature and HeatVolume 
								
							}
							else if (production.Span[(int)i].Store - consumerEntityStruct.Span[0].MinimumPower >=0) // allConsumptions[distributionList[j]].Amount)
							{
								// there is not enough for full amount, can we meet the minimum power reqt?
								production.Span[(int)i].Store -= consumerEntityStruct.Span[0].MinimumPower;
								
								// todo: if there is no more power in the production.Span[i].Store, just break from the loop of consumers of this particular producing Entity
								if (production.Span[(int)i].Store == 0) break;
								
								// 
							}
							consumerEntityStruct.Span[0].PowerRequirement = 10; // per tick or per-use if "Continuous == false:
							consumerEntityStruct.Span[0].MinimumPower = 8;
							consumerEntityStruct.Span[0].Breaker = true;
							consumerEntityStruct.Span[0].Continuous = true; // whether this component always consumes power when operating
							consumerEntityStruct.Span[0].PerformanceSetting = 1.0f; // can run at reduced power, but with reduced performance (eg sensor will have lower range)
							consumerEntityStruct.Span[0].Priority = 0;  // determines if there's insufficient power production, which consumers get higher priority to be powered during runtime 
							
							// runtime
							consumerEntityStruct.Span[0].BreakerCycleDuration = 0;
							consumerEntityStruct.Span[0].TimeStarted = 0;
							consumerEntityStruct.Span[0].Duration = -1;
							consumerEntityStruct.Span[0]. Looping = true; // Repeating
							consumerEntityStruct.Span[0].CooldownDuration = 0; 
							consumerEntityStruct.Span[0].InCoolDown = false;
							
							
							
							//Console.WriteLine("ProcessPowerConsumption() - producer.Output == " + producer.Span[0].Output.ToString());						
							
						}
						catch (Exception ex)
						{
							Console.WriteLine("ProcessPowerConsumption() - ERROR: Production Entity = " + Boids[production.Span[(int)i].ProducerEntityArrayIndex].EntityKey + " Consumer Entity = " + Boids[allConsumptions[distributionList[j]].ConsumerEntityArrayIndex].EntityKey + " " + ex.Message);
						}
					} // end for of consumer distribution list
					
					
					// assign the Store value of the Entity's "Store" to that of this Production
					producerEntityStruct.Span[0].Store = production.Span[(int)i].Store;
					//Console.WriteLine("ProcessPowerConsumption() - Production Entity '" + Boids[production.Span[(int)i].ProducerEntityArrayIndex].EntityKey + "' Store Amount = " + producerEntityStruct.Span[0].Store.ToString());
				}
				catch (Exception ex)
				{
					Console.WriteLine("ProcessPowerConsumption() - " + ex.Message);
				}

			}); // end for of current ComponentStore<Production> // NOTE: ');' parens + semicolon required at end if using parallel.for
		}
		
		private void UpdateProduction(GameTime gt)
        {
            int productID = (int)PRODUCTS.TargetingSkillModifier;
			
			
			/*
			foreach (KeyValuePair<uint, List<Production>> entry in mProduction)
			{	
				productID = entry.Key;
				List<Production> production = entry.Value;
				
				//Console.WriteLine("UpdateProduction() - Running production for " + ((PRODUCTS)productID).ToString());
				
				// March.10.2026 - Production now always occurs automatically without every needing to call Script.OnUpdate()
				//                 because ALL Production and Consumption must be REGISTERED by the Scripts.  In the future
				//                 we can always support dynamic insertion of PRODUCTION during a call to Script.OnUpdate() but
				//                 this should not be used regularly because we do not want to have to force Script.OnUpdate() to
				//                 be called everyframe since we've switched to using a DATA ORIENTED PROCESSING MODEL.
				for (int i = 0; i < production.Count; i++)
				{
					Production currentProduction = production[i];
					
					List<Consumption> consumers = mConsumption[productID];
					if (consumers == null) continue;

					int[] distributionList = currentProduction.DistributionList;
					if (distributionList ==  null || distributionList.Length == 0) continue; // return if using parallel.For

					try
					{
						// Parallelizing the INNER LOOP is typically not recommended because
						// each time the OUTER loop completes, it has to recreate all the threads
						// for the INNER LOOP.
						// Maybe this is OK for our use case since we are essentially running different
						// processing code for different PRODUCTS.  For instance, distributing PRODUCTS.ElectricalPower
						// is not the same as applying PRODUCTS.Morale or PRODUCTS.FatigueRecovery and so if we
						// were to parallelize the OUTER loop, those loops might finish in dramatically different
						// lengths of time because the work can be totally different.
						for (int j = 0; j < distributionList.Length; j++)
						{
							// TODO: above we are iterating, but really what we want I think is to 
							//       have the Memory<Consumption<ProductID>> and the list of which elements are
							//       Then there is ZERO looping and we simply pass ProcessConsumption (currentProduction, mem);
							//       Where we have a Dictionary<> of functions that will process that change just like we do
							//       for things like FLOCKING, OPTICAL_SENSORS, LIFECYCLE
							//       We do not need all these "DamageSystem" and "HealthSystem" and such that contain a special struct for "records"
							//       as such... we just need a LIST or ARRAY like our distributinList[] that says which
							//       Memory<T> records to modify in both the Memory<Production> and Memory<Consumption>
							//       and we will probably optimize those by having Memory<Production<ProductID>> and Memory<Consumption<ProductID>>
							//       And again, each of these will contain a functin to use to handle Production and Consumption... just like 
							//       we do with OPTICAL_SENSORS and LIFECYCLE  so we just do a simple "key"  look up of the Processor function
							//       based on the ProductID.
							//       
							Consumption currentConsumption = consumers[distributionList[j]];
							if (currentConsumption.Equals(default(Consumption)))
							{
								Console.WriteLine("UpdateProduction() - Consumption '" + distributionList[j] + "'  is not registered.");
								continue;
							}
							
							try
							{							
								EntityNode consumerEntity =  Boids[currentConsumption.ConsumerEntityArrayIndex];
								if (consumerEntity == null) continue;
								
								ProcessConsumption(currentProduction, currentConsumption);
							}
							catch (Exception ex)
							{
								Console.WriteLine("UpdateProduction() - ERROR: " + ex.Message);
							}
						} // end for of consumer distribution list
					}
					catch (Exception ex)
					{
						Console.WriteLine("UpdateProduction() - " + ex.Message);
					}

				} // end for of current List<production>
				
				//Console.WriteLine ("UpdateProduction()  - ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++COMPLETED.");
				foreach (Production p in production)
				{	
					if (p.NumUses == 0)
						production.Remove(p);
					
				}
				
			} // end foreach of each mProduction<> dictionary
			
			foreach (KeyValuePair<uint, List<Production>> entry in mProduction)
			{	
				productID = entry.Key;
				List<Production> production = entry.Value;
				if (production != null && production.Count == 0)
			    	mProduction.TryRemove(entry.Key, out production);
			}
			
			//Console.WriteLine ("UpdateProduction()  - ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++COMPLETED.");
			
			*/
        }

		private void ProcessConsumption(Production production, Consumption consumption)
		{
			
			// todo: The distributionList currently contains the index to the List<Consumption> 
			//       but maybe it should also contain the Index to the EntityArrayIndex?
			//       in other words,  make distributionList a List<Tuple<uint, uint>>


			// verify all Entities in the distribution list match an Entity in the registered consumers of this productID.
			// NOTE: Just because a consumer is consuming the same ProductID, does NOT mean it's consuming the production of 
			//       the current sourceEntity.  Consider a reactor that produces POWER, it may only power weapons and the engines
			//       and a second Reactor or Auxillary power source provides energy for things like computers, sensors, etc even 
			//       though it's the same PRODUCT ID.
			// NOTE:  THIS DOES MEAN THAT IF AN ENTITY IS A CONSUMER, BUT DOES NOT REGISTER YET IS ADDED SOMEHOW TO A PRODUCERS
			//        DISTRIBUTIONLIST or USES SOME OTHER DISTRIBUTION METHOD/FILTER, IT _MUST_NOT_GET_PROCESSED 
			//        because again, there will be no details on how this Consumption should be applied to the Producer.
			//        e.g do we drain 10kW from the Battery or do we drain 2kW.

			//currentConsumer.ConsumerEntityArrayIndex = 0;
			//currentConsumer.ProductID = 0;
			//currentConsumer.Amount = 0;
			//currentConsumer.Value = 0;
			//currentConsumer.ConsumerInternalIndex = 0;
			//currentConsumer.Operations = null;
							
					// CONSIDER ProcessOpticalSensors()... we are essentially initiating the PRODUCTION
					// of optical emission by taking all of the optical  producers (each Boid inside Boids[] array)
					// and then doing a spatial search for all other Droids in range of that emission, we 
					// create a OPTICAL_SIGNATURE that takes the form of a "contact" item and is transmitted
					// back to the original emitter Droid's "eye" which  is an optical sensor and currently is stored in
					// Dictionary<> mNeighbors;
			
			int productID = production.ProductID;				
			switch (productID)
			{
				case (int)PRODUCTS.ElectricalPower:
					//mPowerConsumptionSystem.Add (production, consumption);
					break;
				default:
					break;
			}
								
			// PRODUCTION such as PRODUCTS.ElectricalPower or PRODUCTS.OpticalReflection
			if (production.Value is SkillModifier == false)
			{										
				//Console.WriteLine("UpdateProduction() - ENTITY '" + Boids[production.ProducerEntityArrayIndex].EntityKey + "' PRODUCING -> " + ((PRODUCTS)production.ProductID).ToString() + " to '" + consumerEntity.EntityKey + "'");


			}
			// SKILL MODIFICATION
			else 
			{
				SkillModifier modifier = (SkillModifier)production.Value;

				if (modifier.Enabled)
				{
					if (modifier.NumUses > 0 || modifier.NumUses == -1 )
					{
						System.Diagnostics.Debug.Assert(consumption.ProductID == productID);          // todo: i think the productID can be different than what the consumption handler is passed in. For instance, "heat" can be passed in and result in "damage" to be applied to the consumer.  Actually, I think we've modified this so that "PRODUCTS.HeatSignature" and "Products.HeatDamage" are two seperate products that may or may not both be consumed by any given Consumer.
						consumption.Value = modifier;
						consumption.Amount = modifier.Amount; // obsolete - maybe not? <- MichaelOliveTree Feb.25.2026 - OLD -> we use PropertySpec[] now with intrinsic types. // the Simulation EXE will know how to deal with UnitValue basedon ProductID.  This could also be "damage." 

						//Console.WriteLine("UpdateProduction() - ENTITY '" + Boids[producerEntityArrayIndex].EntityKey + "' MODIFYING SKILL -> " + ((PRODUCTS)production.ProductID).ToString() + " to '" + consumerEntity.EntityKey + "'");

						// TODO: in the loop of all production, there can be multiple Producers modifying this
						//       currentConsumer.  I think we should be able to track all of them... but that
						//       could maybe be done in the "ModificationSystem" 

						// TODO: based on the ProductID, we need to find the correct property to modify
						// and to also deduct that amount from a Producer if it's production amount is not infinite
						// TODO: also, when deducting production from a Producer, we need to sychronize thread access to it?
						//consumption.

						mSkillModificationSystem.Add (consumption);
						if (modifier.NumUses > 0)
							modifier.NumUses--;
					}
					else if (modifier.NumUses == 0)
					{
						modifier.Enabled = false;
					}
				}
			}
		}
		
		public struct SkillSystem
		{
			public struct ModificationResult
			{
				public int EntityIndex;
				public int TargetIndex;
				public SKILLS SkillType;
				public int Amount;
			}
						
			public void Apply(ComponentStore<LifeForm> store, object[] parameters, int seed, GameTime gt)
			{
				// NOTE: the store used here must refer to the actual memStore the Droid uses
				//       to store it's data or else there is no way to update that Droid...Duh!
				//       This is OK though!  We just need to know that although all the RECORDS
				//       will be used in List<DamageResult>records, NOT ALL of the SPAN records
				//       will be used.  No problem.  We just use memSpan[records[i].EntityIndex] 
				//       to know which ones to use
				//       
				if (store == null) return;
				Span<LifeForm> memSpan = store.Span;
				List<ModificationResult> records = (List<ModificationResult>)parameters[0];					
				
				if (records != null)
				{
					for (int i = 0; i < records.Count; i++)
					{
						LifeForm e = (LifeForm)memSpan[records[i].TargetIndex]; // todo: this should be the target to which the Modification should be applied
						// = records[i].Amount;
						
						SKILLS s = records[i].SkillType;
						//e.Skills[s].AddExternalModifier(records[i].Amount);
						
												
						// e.Skills[(int)m.SkillToTarget].Bonuses += m.Bonus;
						
					 	// so lets say we have a TargetingSkillModifier every frame so long as an Operator
						// is using the "Droid/aka TacticalCrewStation."  Or does it get added just 
						// ONCE until after the Tactical Crew Station is "USED" by the operator and only when
						// "UN-USED" does the bonuses get cleared.  
						
						// This would entail adding the PRODUCTION but NOT Registering() it.
						
						
						
					}
				}
			}
		}
		
		
		// todo: might exist in Game01.Rules.Processors
		public struct SkillModificationSystem
		{
			List<Consumption> mRecords;
			List<SkillSystem.ModificationResult> mSkillModResults;
			
			public SkillModificationSystem()
			{
				mRecords = new List<Consumption>();
				mSkillModResults = new List<SkillSystem.ModificationResult>();
			}
			
			public void Add (Consumption d)
			{
				mRecords.Add (d);
				//Console.WriteLine ("SkillModificationSystem.Add() - Record count == " + mRecords.Count.ToString());
			}
			
			public void Clear()
			{
				mRecords.Clear();
				mSkillModResults.Clear();
			}
					
			public void Process(ComponentStore<LifeForm> store, object[] parameters, int seed, GameTime gt)
			{
				if (store == null) return;
				Span<LifeForm> memSpan = store.Span;
				
				Clear();
				
				if (mRecords != null)
				{
					mSkillModResults.Clear();
					
					for (int i = 0; i < mRecords.Count; i++)
					{
						int amount = mRecords[i].Amount;
						mSkillModResults.Add (new SkillSystem.ModificationResult() {TargetIndex = mRecords[i].ConsumerEntityArrayIndex, Amount = amount});	
					
					}
				
					// use the same LivingEntityStore as the one passed in, for applying Skill changes to the Droid
					BoidSimulation.mSkillSystem.Apply(store, new object[] {mSkillModResults}, seed, gt);
				}
			}
		}
		
		
		public enum ACTIONS : int
		{
			None = 0,
			FiringAt,
			TargetHit,
			DeployMine,
			DeployProbe
		}
		
		public interface ISimulationEventManager
		{
			void Subscribe();
			void UnSubscribe();

			void Notify();
			void PublishEvent(EntityNode owner, int actionID, ISimEventRecord r);
			
		}

		public interface ISimEventRecord
		{
			public int ActionID {get; set;}
			public double Time {get; set;}
		}
		
		public struct CombatEventRecord : ISimEventRecord
		{
			public int ActionID {get; set;}
			public double Time {get; set;}
			
			public int OfficerArrayIndex;    // Attacking vessel's acting Tactical Officer
			public int StationArrayIndex;    // Attacking vessel Tactical Station
			public int ShipArrayIndex;       // Attacking vessel
			
			public int WeaponArrayIndex;     // Attacking vessel's weapon used
			
			// TODO: a problem here is, what if we wanted to query for all Ships that have been attacked within X timeframe... here
			//       the targets are in an array and some are components (not owners) and it would be extremely slow to search for these.
			//       I think at the very least, all componets and assemblies need to track something like "int[] TargetOwnersArrayIndices" 
			public int[] TargetArrayIndices;  // the lowest resolution Components or parts of one or more Targets. A missile for example might cause damage to multiple targets (i.e splash damage or proximity damage)
			public int[] TargetOwnerArrayIndices; // if the owner and target are the same, then the overall "hull" was targeted/hit.
			
			// NOTE: The following fields may not be necessary for all ActionID types.  For now we'll just keep them in this one struct til we learn more about the different record types we'll need and how we'll be storing them
			public int[] Damage;             // amount of damage inflicted during this event
			public HitPoints[] HitPoints;    // Target operator(s), component(s), assembly(s) or ship(s) hitpoint at the end of this event 
		}
		
		
		
	
		
		/// <summary>
		/// This class should be a concrete implementation of Keystone.ISimulationEventManager that 
		/// resides in Game01.dll for tracking all of the simulation specific
		/// (game-play) events that occur during runtime.
		///
		/// This class serves three purposes:
		/// 1) It logs simulation events
		/// 2) It serves as a 'history' STORE of ALL simulation events
		/// 3) It notifies subscribers of the EventManager of the various events that happen at runtime
		///    to which they've subscribed.  For instance, the GUI can subscribe to various events.
		/// </summary>
		public class SimulationEventManager : ISimulationEventManager
		{
			// https://softwareengineering.stackexchange.com/questions/401800/c-design-question-about-a-specific-game-combat-implementation-with-a-event-sys
			// OUR ENTITIES do support custom EVENTS... but those are more for property value changes
			// and animation events...  This class is for high-level SIMULATION events like FIRING upon
			// another vessel, a vessel being IMPACTED by a mine...
			// Do we want to track every SECURITY event such as who accessed what doors and at what time?
			// Which crew member or passenger passed by which point in this ship at what time...etc? YES ultimately...
			//
			// todo: this class should strive to work well with UserObjectStore for AI blackboard data.
			// TODO: Thus, every event should probably be stored into buckets differentiated by entityKey
			//       This means we do want to have calls like GetOwner(operatorID) or GetOwner(stationID)
			//       or GetOwner(assemblyID)  to ultimately return the VEHICLE ID (aka DROID ID).
			//       Otherwise there are too many EntityKeys potentially?  Hmm...
			//       Consider if we want to find out if an encountered ship has previously fired upon a 
			//       friendly ship, if we search for all instances of the SHIP (as owner) to which ANY 
			//       of it's OPERATORS at ANY STATION using ANY WEAPON attacked a friendly craft, it should
			//       return those relevant events.  It would be too difficult to search by OPERATOR and TIME
			//       because the OPERATOR has to have served on the ship in question during the time a friendly
			//       ship of ours was attacked.
			public UserDataStore mUserDataStore;
			
			public SimulationEventManager(UserDataStore dataStore)
			{
				
				mUserDataStore = dataStore;
				
			}
			
		#region ISimulationEventManager members
			public void Subscribe()
			{
			}
			public void UnSubscribe()
			{
			}
			
			public void Notify()
			{
				// notify all subscribers (observers) of those events
				// that occurred since the previous Notify() and to which
				// they are specifically subscribed
				
			}
			
			public void PublishEvent(EntityNode owner, int actionID, ISimEventRecord r)
			{
				
			}
		#endregion
				
			//public event EventHandler<CombatEventArgs> CombatLog;

			public void TakeDamage(string attacker, string target, int damage)
			{
				// Invoke event with variable arguments
				//OnCombatLog(new CombatEventArgs("{0} dealt {1} damage to {2}!", attacker, damage, target));
			}

			//protected virtual void OnCombatLog(CombatEventArgs e)
			//{
			//	CombatLog?.Invoke(this, e);
			//}
		}
		
		
		
		public struct HealthSystem
		{
			public struct DamageResult
			{
				public int TargetEntityArrayIndex;
				public int Amount;
			}
			

			public void Apply(ComponentStore<LifeForm> store, object[] parameters, int seed, GameTime gt)
			{
				// NOTE: the store used here must refer to the actual memStore the Droid uses
				//       to store it's data or else there is no way to update that Droid...Duh!
				//       This is OK though!  We just need to know that although all the RECORDS
				//       will be used in List<DamageResult>records, NOT ALL of the SPAN records
				//       will be used.  No problem.  We just use memSpan[records[i].EntityIndex] 
				//       to know which ones to use
				//       
				if (store == null) return;
				Span<LifeForm> memSpan = store.Span;
				List<DamageResult> records = (List<DamageResult>)parameters[0];					
				
				if (records != null)
				{
					for (int i = 0; i < records.Count; i++)
					{
						int spanIndex;
						Memory<LifeForm> lf  = (Memory<LifeForm>)EntryClass.bSim.Boids[records[i].TargetEntityArrayIndex].GetUserStruct(typeof(LifeForm), out spanIndex);
						//LifeForm lf = (LifeForm)memSpan[records[i].EntityIndex];
						HitPoints prev = lf.Span[0].HitPoints;
						lf.Span[0].HitPoints.Current -= records[i].Amount;
						Console.WriteLine ("HealthSystem.Apply() -  Entity '" + EntryClass.bSim.Boids[records[i].TargetEntityArrayIndex].EntityKey + " Hitpoints: '" + lf.Span[0].HitPoints.ToString() + "' Previously was: '" + prev.ToString() + "'");
						
					}
				}
			}
		}
		
		//see Keystone.Game01.Messages.   public class AttackResults since
		// we need results going over the network
		public struct DamageSystem
		{
			public struct Damage
			{
				public double TimeOfAttack;
				public int AttackerOperatorEntityArrayIndex; // always the Operator
				public int WeaponUsedEntityArrayIndex;
				public int TargetEntityArrayIndex;           // always the specific Component or Assembly that received damage
				public int Amount;
			}
			
			System.Collections.Concurrent.ConcurrentQueue<Damage> mRecords;
			List<HealthSystem.DamageResult> mDamageResults;
			
			public DamageSystem()
			{
				mRecords = new System.Collections.Concurrent.ConcurrentQueue<Damage>();
				mDamageResults = new List<HealthSystem.DamageResult>();
			}
			
			public void Add (Damage d)
			{
				try
				{
					mRecords.Enqueue(d);
				}
				catch (Exception ex)
				{
					Console.WriteLine("DamageSystem.Add() - Record Count == " + mRecords.Count.ToString());
				}
			}
			
			public void Clear()
			{
				mRecords.Clear();
				mDamageResults.Clear();
			}
					
			public void Process(ComponentStore<LifeForm> store, object[] parameters, int seed, GameTime gt)
			{
				if (store == null) return;
				Span<LifeForm> memSpan = store.Span;
				
				if (mRecords != null)
				{
					mDamageResults.Clear();
					
					while (mRecords.Count > 0)
					{
						Damage d;
						bool result = mRecords.TryDequeue(out d);
						
						int amount = d.Amount;
						mDamageResults.Add (new HealthSystem.DamageResult() {TargetEntityArrayIndex = d.TargetEntityArrayIndex, Amount = amount});
						Console.WriteLine ("DamageSystem.Add() - Damage of '" + amount.ToString() + "'  being applied to '" + EntryClass.bSim.Boids[d.TargetEntityArrayIndex].EntityKey);
					}
					
					// use the same <LifeForm>store as the one passed in, for applying health changes to the Droid
					BoidSimulation.mHealthSystem.Apply(store, new object[] {mDamageResults}, seed, gt);
				}
			}
		}
		
		//see Keystone.Game01.Messages.   public class AttackResults since
		// we need results going over the network
		public struct DamageOverTimeSystem
		{
			public struct DamageOverTime
			{
				public double TimeOfAttack;
				public int AttackerOperatorEntityArrayIndex; // always the Operator
				public int WeaponUsedEntityArrayIndex;
				public int TargetEntityArrayIndex;           // always the specific Component or Assembly that received damage
				public int Amount;
				public double Duration;
			}
			
			System.Collections.Concurrent.ConcurrentQueue<DamageOverTime> mRecords;
			List<HealthSystem.DamageResult> mDamageResults;
			
			
			public DamageOverTimeSystem()
			{
				mRecords = new System.Collections.Concurrent.ConcurrentQueue<DamageOverTime>();
				mDamageResults = new List<HealthSystem.DamageResult>();
			}
			
			public void Add (DamageOverTime d)
			{
				try
				{
					mRecords.Enqueue(d);
					//Console.WriteLine ("DamageOverTimeSystem.Add() - Record count == " + mRecords.Count.ToString());
				}
				catch (Exception ex)
				{
					Console.WriteLine("DamageOverTimeSystem.Add() - Record Count == " + mRecords.Count.ToString() + " " + ex.Message);
				}
			}
					
			public void Clear()
			{
				mRecords.Clear();
				mDamageResults.Clear();
			}
			
			/// <summary>
			/// FireDamage for example, can last for several seconds and so any one particular FireDamage record is
			/// not removed from the ComponentStore<> until it's expired
			/// </summary>
			public void Process(ComponentStore<LifeForm> store, object[] parameters, int seed, GameTime gt)
			{
				if (store == null) return;
				Span<LifeForm> memSpan = store.Span;
							
				if (mRecords != null)
				{
					mDamageResults.Clear();
					
					while (mRecords.Count > 0)
					{
						DamageOverTime d;
						bool result = mRecords.TryDequeue(out d);
						// TODO: what if our damageOverTime was just adding more instances of the Damage struct to the
						//       queue and then waiting for the TimeOfAttack to match before executing any of them?
						//      This seems better than having a seperate DamageOverTime struct from Damage struct...
						
						int amount = d.Amount;
						mDamageResults.Add (new HealthSystem.DamageResult() {TargetEntityArrayIndex = d.TargetEntityArrayIndex, Amount = amount});
						Console.WriteLine ("DamageOverTimeSystem.Add() - Damage of '" + amount.ToString() + "'  being applied to '" + EntryClass.bSim.Boids[d.TargetEntityArrayIndex].EntityKey);
					}
					
					// use the same <LifeForm> store as the one passed in, for applying health changes to the Droid
					BoidSimulation.mHealthSystem.Apply(store, new object[]{ mDamageResults }, seed, gt);
				}
			}
			
            // NOTE: in KeystoneGameBlocks we would then potentially send the result to the clients if this is processing on the server
            // FormMainBase.SendNetMessage(msg)
		}
		
		public void RegisterProduction (EntityNode entity, Production[] production)
		{
			if (production != null)
				for (int i = 0; i < production.Length; i++)
					 RegisterProduction(entity, production[i]);
		}

		private static System.Threading.SemaphoreSlim mProductionSemaphore = new System.Threading.SemaphoreSlim(1);
		private static System.Threading.SemaphoreSlim mConsumptionSemaphore = new System.Threading.SemaphoreSlim(1);
       	
		public void RegisterProduction(EntityNode entity, Production p)
        {
		    try
			{
				mProductionSemaphore.Wait(-1);
				int productID = p.ProductID; 
				//Console.WriteLine ("RegisterProduction()  - productID == " + productID.ToString());
				
				// NOTE: mLimitedProduction may not be necessary as we now track the NumUses for any given Production and if
				//       p.NumUses == 0, then we remove that production at the end of UpdateProduction();
				//if (limited)
				//{
				//	List<Production> production = mLimitedProduction.GetOrAdd(productID, (key) => new List<Production>());
				//	mLimitedProduction[productID].Add(p);
				//}
				//else
				//{
	            	//List<Production> production = mProduction.GetOrAdd(productID, (key) =>  new List<Production>());
            		ComponentStore<Production> production = mProduction.GetOrAdd (productID, (key) =>  EntryClass.mCStoreCol.CheckOut<Production>(EntryClass.NUM_ENTRIES, (int)p.ProductID));
					Predicate<Production> productionForThisEntityAndProductAlreadyExists = x => x.ProductID == p.ProductID && x.ProducerEntityArrayIndex == p.ProducerEntityArrayIndex;
					Production search = production.Find(productionForThisEntityAndProductAlreadyExists);
				
					if (search.Equals(default(Production)))
					{
						int index;
						Memory<Production> mem = production.CheckOut(out index);
						mem.Span[0] = p;
						//Console.WriteLine("RegisterProduction() - PRODUCTION '" + ((PRODUCTS)productID).ToString() + "' REGISTERED>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
						
					}
				
				//}	
			}
			finally
			{
				mProductionSemaphore.Release();
			}
            // todo: ideally this ISimulation implementation should be in the EXE because we need to know the game specific productIDs and what they refer to
            // todo: how and where is the Hz for each productID defined?  Perhaps its just the job of this Simulation implementation which should be implemented in the EXE, not Keystone.dll
        }

        public void RegisterConsumption(EntityNode entity, Consumption c)
        {
			try
			{
				mConsumptionSemaphore.Wait(-1);
				int productID = c.ProductID;
				//Console.WriteLine ("RegisterConsumption()  - productID == " + productID.ToString());
            	//List<Consumption> consumption = mConsumption.GetOrAdd (productID, (key) =>  new List<Consumption>());
				ComponentStore<Consumption> consumption = mConsumption.GetOrAdd (productID, (key) =>  EntryClass.mCStoreCol.CheckOut<Consumption>(EntryClass.NUM_ENTRIES, (int)c.ProductID));
				Predicate<Consumption> consumptionForThisEntityAndProductAlreadyExists = x => x.ProductID == c.ProductID && x.ConsumerEntityArrayIndex == c.ConsumerEntityArrayIndex;
				Consumption search = consumption.Find(consumptionForThisEntityAndProductAlreadyExists);
				
				if (search.Equals(default(Consumption)))
				{
					int index;
					Memory<Consumption> mem = consumption.CheckOut(out index);
					mem.Span[0] = c;
				}
				else 
					Console.WriteLine("RegisterConsumption() - Consumption '" + ((PRODUCTS)c.ProductID).ToString() + " for Entity " + c.ConsumerEntityArrayIndex + "' already exists.");
			}
			finally
			{
				mConsumptionSemaphore.Release();
			}
            // todo: ideally this ISimulation implementation should be in the EXE because we need to know the game specific productIDs and what they refer to
            // todo: how and where is the Hz for each productID defined?  Perhaps its just the job of this Simulation implementation which should be implemented in the EXE, not Keystone.dll
        }
        
		public void UnRegisterProduction(uint productID, EntityNode entity)
        {
			/*
			int found = -1;
			for (int i = 0; i < mProduction.Count; i++)
				if (mProduction[productID][i].ProducerEntityArrayIndex == entity.EntityArrayIndex)
				{
					found = (int)i;
					break;
				}

            mProduction[productID].Remove(mProduction[productID][(int)found]);
			*/
			
			
			
        }

        // TODO: when an Entity is detached from the Scene, it should be removed as a Consumer
        public void UnRegisterConsumption(uint productID, EntityNode entity)
        {
			/*
			int found = -1;
			for (int i = 0; i < mConsumption.Count; i++)
				if (mConsumption[productID][i].ConsumerEntityArrayIndex == entity.EntityArrayIndex)
				{
					found = (int)i;
					break;
				}

            mConsumption[productID].Remove(mConsumption[productID][(int)found]);
			
			
			
			ComponentStore<Consumption> consumption;
			bool foundf = mConsumption.TryGetValue(productID, out consumption);
			
			Predicate<Consumption> consumptionForThisEntityAndProductAlreadyExists = x => x.ProductID == c.ProductID && x.ConsumerEntityArrayIndex == c.ConsumerEntityArrayIndex;
			Consumption search = consumption.Find(consumptionForThisEntityAndProductAlreadyExists);

			if ()
			{
				int index;
				Memory<Consumption> mem = consumption.CheckOut(out index);
				mem.Span[0] = c;
			}
			else 
				Console.WriteLine("RegisterConsumption() - Consumption '" + ((PRODUCTS)c.ProductID).ToString() + " for Entity " + c.ConsumerEntityArrayIndex + "' already exists.");
			*/
			
        }

        // TODO: when an Entity is detached from the Scene, it should be removed as a Producer
        public void UnRegisterProducer(uint productID, EntityNode entity)
        {
            //mProducers[productID].Remove(entity);
        }

        // TODO: when an Entity is detached from the Scene, it should be removed as a Consumer
        public void UnRegisterConsumer(uint productID, EntityNode entity)
        {
            //mConsumers[productID].Remove(entity);
        }

		
		#endregion


        bool mIsDisposed;
        public void Dispose()
        {
            if (!mIsDisposed)
            {
                Console.WriteLine("BoidSimulation.~dtor() - Detroying all boids");
            
				if (this.Boids != null)
					for (int i = 0; i < this.Boids.Count; i++)
					{
						// todo: do we need to remove from SpatialNode here or should
						//       the BoidSimulation do that?  I think the Simulation should on notification
						//       that it's been removed from the Scene which is how we will do it in KeystoneGameBlocks.
				#if MEMORY_T
						this.Boids[i].Dispose();
				#endif
						this.Boids[i] = null;
					}

					mIsDisposed = true;
			}
        }
	}
	
		
    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN BOIDS
	// The Boid class is influenced by the Boid code from the following GitHub repository.  Primarily, the 
	// seperation, cohesion and alignment functions have been copied.
    //https://github.com/swharden/Csharp-Data-Visualization/blob/main/website/content/simulations/boids/index.md
	// 
    public class Boid : EntityNode
    {
        private const double BOID_WIDTH = 2.0d;
        
        public Boid(string entityID, int index, double x, double y, double z,  double xV, double yV)
            : base(entityID, index, x, y, z, xV, yV)
        {
				
#if USE_MEMORY_T
			OnInitializeEntity();
				
#endif
	
            Vector3d v;
            v.x = xV;
            v.y = yV;
            v.z = 0.0d;
            Velocity = v;

			// bounding box in World Space which is probably not what we want for KGB Entity but only for KGB EntityNode (which is derived from SceneNode and used for hierarchical bbox structure)
            _box = new BoundingBox(Translation,  BOID_WIDTH);
        }
	
			
	#region ShouldBeInEntityScript_NotHERE_ButCantRunScriptsFromWebCSharpCompiler
		private void OnInitializeEntity()
		{
			// assignment can be done via User Interface just using a propertyName and TypeName from a PropertySpec and does not need to know any game specific info including the MODs various types of UserStructs or how to process them
			// -------------------------------
			// Entity.SetCustomPropertyValue
			// Entity.GetCustomPropertyValue
	
			// assignment can be done via the scripts themselves since they are aware of the properties of their MOD using just the single records Memory<T>
			// -------------------------------
			// object = (CastHere>Entity.GetUserStruct(T type)
			// object.Span[0].PropertyName = 1234;
			// or return object.Span[0].PropertyName;
			
			// assignment can be done via our DataProcessors using the FULL Memory<T> 
			// or object = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES);
			// -------------------------------
			// for (int i = 0; i < object.RecordCount; i++
			// {
			//     string name = object.Span[i].PropertyName 
			// }
			
		
			
			PropertyBag bag = new PropertyBag();
			bag.SetValue += OnSetLaserStructValue;
			bag.GetValue += OnGetLaserStructValue;
				
			// TODO: the problem here is, only a GUI can trigger the events for SetValue and GetValue...  
			//       and that is fine since it does represent editing say the Laser build properties 
			//       and then being able to grab them all from bag.Properties if we need to.
			PropertySpec spec = new PropertySpec("entityIndex", typeof(int).Name, "indices", "this is the span index", (object)0, (string)null, (string)null);
			bag.Properties.Add(spec);
			
			PropertySpecEventArgs e = new PropertySpecEventArgs(spec, 9999);
			bag.OnSetValue (e);
				
				
			// TODO: the CustomProperties should be mostly for GUI... the structs is where the values
			//       for that underlying GUI is STORED.  So there's just a couple of questions
			//       1 - do we use multiple structs or do we use one struct with all possible options except perhaps Armor options
			//          - if we use multiple structs, the indices will all be different in the different Memory<T> records.  
			//       Also, this will always happen anyway when iterating that records representing most Weapons will NOT be firing anyways... 
			//      VERY GOOD ARTICLE ON Span<T> below.  
			//	https://nishanc.medium.com/an-introduction-to-writing-high-performance-c-using-span-t-struct-b859862a84e4
				
			// WE KNOW what the FOREIGN KEY WOULD BE for say Component.Index, Component.ArmorIndex, Component.WeaponIndex, Component.LaserIndex
			//                                               each Armor, Weapon, Laser, etc, will have a reference to the Component.Index which
			//                                               will yield Entity.Index if necessary.  
			// SO NOW THE QUESTION IS, we need a way to grab these memoryStores perhaps in the Processors WITHOUT having to pass them in
			// at all!!! Let each Processor know which ones to grab I think.
			// 

				
			// if ()
			// 
				
			/*
				-- To String
				using System;
				using System.Text;

				// Example with a byte array (UTF-8 encoding for "Hello")
				byte[] buffer = { 72, 101, 108, 108, 111, 32, 87, 111, 114, 108, 100 };
				ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(buffer);

				// Slice the span
				ReadOnlySpan<byte> slice = span.Slice(0, 5); // Corresponds to "Hello"

				// Convert the slice to a string using the correct encoding
				string result = Encoding.UTF8.GetString(slice);

				Console.WriteLine(result); // Output: Hello


				-- To Int
				using System;
				using System.Runtime.InteropServices;

				byte[] buffer = new byte[] { 25, 0, 0, 0, 100, 101, 102, 103 };
				ReadOnlySpan<byte> slice = buffer.AsSpan().Slice(0, 4);

				// Reinterpret the bytes as a ReadOnlySpan<int>
				// This assumes the bytes are already in the correct endianness for the current system.
				// If not, you need to use BinaryPrimitives.ReverseEndianness to correct it.
				int result = MemoryMarshal.Read<int>(slice); 

				Console.WriteLine(result);
				*/
		}

		// public delegate void PropertySpecEventHandler(object sender, PropertySpecEventArgs e);
		public void OnGetLaserStructValue(object sender, PropertySpecEventArgs e)
		{
			//Console.WriteLine("OnGetLaserStructValue " + e.Value.ToString());
			
			// TODO: this MUST _GET_ the values from the relevant Script's struct  eg wep = (Weapon)GetUserStruct(type);
			switch (e.Property.Name)
			{
				case "entityIndex":
					//Console.WriteLine ("Getter() - entityIndex currently set to " + e.Value.ToString());
					break;
				default:
					break;
			}
			
			// Have the property bag raise an event to get the current value
			// of the property.

			//PropertySpecEventArgs e = new PropertySpecEventArgs(item, null);
			//bag.OnGetValue(e);
			//return e.Value;
		}
		
		public void OnSetLaserStructValue(object sender, PropertySpecEventArgs e)
		{
			//Console.WriteLine("OnSetLaserStructValue " + e.Value.ToString());
			
			// TODO: this MUST _SET_ the values TO the relevant Script's struct  eg wep = (Weapon)GetUserStruct(type);
			switch (e.Property.Name)
			{
				case "entityIndex":
					//Console.WriteLine ("Setter() - entityIndex changing too " + e.Value.ToString());
					break;
				default:
					break;
			}
			// Have the property bag raise an event to set the current value
			// of the property.

			//PropertySpecEventArgs e = new PropertySpecEventArgs(item, value);
			//bag.OnSetValue(e);
		}
		
		private void OnSetLaserStructValue(PropertySpecEventArgs e)
		{
			
		}
		
		private void OnGetLaserStructValue(PropertySpecEventArgs e)
		{
			
		}
		
		
	#endregion // ShouldBeInEntityScript_NotHERE_ButCantRunScriptsFromWebCSharpCompiler
		
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetDistance(Boid b1, Boid b2)
        {
            return Vector3d.GetDistance3d(b1.Translation, b2.Translation);
        }

        // Rule 1: Separation
        // Classes version that iterates through all boids
        public static (double xVel, double yVel) Separate(double elapsedSeconds, List<EntityNode> boids, Boid current, double separationDistance, double separationFactor)
        {
            Vector3d steer;
            steer.x = 0d;
            steer.y = 0d;
            steer.z = 0d;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // Iterate through other boids
            foreach (Boid other in boids)
            {
                if (other == current) continue;

                double distance = GetDistance(current, other);

                if (distance < separationDistance)
                {
                    if (distance > 0d) // Hypnotron Dec.4.2025 - required divide by 0 check
                    {
                        steer += (current.Translation - other.Translation) / distance;
                    }
                }
            }

            return (steer.x * separationFactor, steer.y * separationFactor);
        }

        // Rule 1: Separation
        // Classes with PRECOMPUTED NEIGHBORS version
        public static (double xVel, double yVel) Separate(double elapsedSeconds, List<EntityNode> boids, Boid current, double separationDistance, double separationFactor, List<Boid> neighbors)
        {
            Vector3d steer;
            steer.x = 0d;
            steer.y = 0d;
            steer.z = 0d;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // Iterate through other boids
if (neighbors!= null)
            foreach (Boid other in neighbors)
            {
                //if (other == current) continue;

                double distance = GetDistance(current, other);

                if (distance < separationDistance)
                {
                    if (distance > 0d) // Hypnotron Dec.4.2025 - required divide by 0 check
                    {
                        steer += (current.Translation - other.Translation) / distance;
                    }
                }
            }

            return (steer.x * separationFactor, steer.y * separationFactor);
        }

#if USE_MEMORY_T
        // Rule 1: Separation
        // Memory<T> Standard version is by far the fastest
        public static (double xVel, double yVel) Separate(double elapsedSeconds, Span<Transform.Transform_Struct> mem, uint numBoids, int currentIndex, double separationDistance, double separationFactor)
        {
            Vector3d steer;
            steer.x = 0d;
            steer.y = 0d;
            steer.z = 0d;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // Iterate through other boids
            for (int i = 0; i < numBoids; i++)
            {
                if (i == currentIndex) continue;

                double distance = Vector3d.GetDistance3d(mem[currentIndex].Translation, mem[i].Translation);

                if (distance < separationDistance)
                {
                    if (distance > 0d) // Hypnotron Dec.4.2025 - required divide by 0 check
                    {
                        //steerX += (current.Translation.x - other.Translation.x) / distance; // Move away
                        //steerY += (current.Translation.y - other.Translation.y) / distance;

                        steer += (mem[currentIndex].Translation - mem[i].Translation) / distance;
                    }
                }
            }

            return (steer.x * separationFactor, steer.y * separationFactor);
        }

        // Rule 1: Separation
        // Memory<T> PRECOMPUTED NEIGHBORS version
        public static (double xVel, double yVel) Separate(double elapsedSeconds, ComponentStore<Transform.Transform_Struct> store, uint numBoids, int currentIndex, double separationDistance, double separationFactor, List<int> neighbors)
        {
            Span<Transform.Transform_Struct> mem = store.Span;

            Vector3d steer;
            steer.x = 0d;
            steer.y = 0d;
            steer.z = 0d;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // Iterate through precomputed nearISH neihbors
			if (neighbors!= null)
				for (int i = 0; i < neighbors.Count; i++)
				{
					//if (i == currentIndex) continue;
					double distance = Vector3d.GetDistance3d(mem[currentIndex].Translation, mem[neighbors[i]].Translation);

					if (distance < separationDistance)
					{
						if (distance > 0d) // Hypnotron Dec.4.2025 - required divide by 0 check
						{
							steer += (mem[currentIndex].Translation - mem[neighbors[i]].Translation) / distance;
						}
					}
				}

            return (steer.x * separationFactor, steer.y * separationFactor);
        }
#endif

        // Rule 2: Alignment
        // LinkQ version
        public static (double xVel, double yVel) Align(double elapsedSeconds, List<EntityNode> boids, Boid current, double alignmentDistance, double alignmentFactor)
        {
            // WARNING: LinkQ .Where iterates through ALL boids
            // for each CURRENT boid and is O(n^2) and is too 
            // expensive
            var neighbors = boids.Where(x => x != current && GetDistance(current, (Boid)x) < alignmentDistance);
            if (!neighbors.Any())
            {
                return (0, 0); // No neighbors to align with
            }

            double meanXvel = neighbors.Sum(x => x.Velocity.x) / neighbors.Count();
            double meanYvel = neighbors.Sum(x => x.Velocity.y) / neighbors.Count();

            return ((meanXvel - current.Velocity.x) * alignmentFactor, (meanYvel - current.Velocity.y) * alignmentFactor);
        }

        // Rule 2: Alignment
        // LinkQ with PRECOMPUTED NEIGHBORS version
        public static (double xVel, double yVel) Align(double elapsedSeconds, List<EntityNode> boids, Boid current, double alignmentDistance, double alignmentFactor, List<Boid> preNeighbors)
        {
            // WARNING: LinkQ .Where iterates through ALL boids
            // for each CURRENT boid and is O(n^2) and is too 
            // expensive
if (preNeighbors== null)
return (0,0);

            var neighbors = preNeighbors.Where(x => x != current && GetDistance(current, x) < alignmentDistance);
            if (!neighbors.Any())
            {
                return (0, 0); // No neighbors to align with
            }

            double meanXvel = neighbors.Sum(x => x.Velocity.x) / neighbors.Count();
            double meanYvel = neighbors.Sum(x => x.Velocity.y) / neighbors.Count();

            return ((meanXvel - current.Velocity.x) * alignmentFactor, (meanYvel - current.Velocity.y) * alignmentFactor);
        }


#if USE_MEMORY_T
        // Rule 2: Alignment
        // Memory<T> Standard version is by far the fastest
        public static (double xVel, double yVel) Align(double elapsedSeconds, Span<Transform.Transform_Struct> mem, uint numBoids, int currentIndex, double alignmentDistance, double alignmentFactor)
        {
            Vector3d neighborsVelocity;
            neighborsVelocity.x = 0;
            neighborsVelocity.y = 0;
            neighborsVelocity.z = 0;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // sum neighbors'velocity
            int count = 0;
            for (int i = 0; i < numBoids; i++)
            {
                if (i == currentIndex) continue;

                double distance = Vector3d.GetDistance3d(mem[currentIndex].Translation, mem[i].Translation);

                if (distance < alignmentDistance)
                {
                    neighborsVelocity += mem[i].Velocity;
                    count++;
                }
            }

            if (count == 0) return (0, 0);
            neighborsVelocity /= count;

            Vector3d result = (neighborsVelocity - mem[currentIndex].Velocity) * alignmentFactor;
            return (result.x, result.y);
        }

        // Rule 2: Alignment
        // Memory<T> PRECOMPUTED NEIGHBORS version
        public static (double xVel, double yVel) Align(double elapsedSeconds, ComponentStore<Transform.Transform_Struct> store, uint numBoids, int currentIndex, double alignmentDistance, double alignmentFactor, List<int> neighbors)
        {
            Span<Transform.Transform_Struct> mem = store.Span;

            Vector3d neighborsVelocity;
            neighborsVelocity.x = 0;
            neighborsVelocity.y = 0;
            neighborsVelocity.z = 0;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // sum neighbors'velocity
            int count = 0;
            for (int i = 0; i < neighbors.Count; i++)
            {
                //if (i == currentIndex) continue;

                double distance = Vector3d.GetDistance3d(mem[currentIndex].Translation, mem[neighbors[i]].Translation);

                if (distance < alignmentDistance)
                {
                    neighborsVelocity += mem[neighbors[i]].Velocity;
                    count++;
                }
            }

            if (count == 0) return (0, 0);
            neighborsVelocity /= count;

            Vector3d result = (neighborsVelocity - mem[currentIndex].Velocity) * alignmentFactor;
            return (result.x, result.y);
        }
#endif

        // Rule 3: Cohesion 
        // LinkQ version
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, List<EntityNode> boids, Boid current, double cohesionDistance, double cohesionFactor)
        {
            var neighbors = boids.Where(x => x != current && GetDistance(current, (Boid)x) < cohesionDistance);
            if (!neighbors.Any())
            {
                return (0, 0); // No neighbors to cohese with
            }

            double centerX = neighbors.Average(x => x.Translation.x);
            double centerY = neighbors.Average(x => x.Translation.y);

            Vector3d center;
            center.x = centerX;
            center.y = centerY;
            center.z = 0.0d;
            Vector3d result = (center - current.Translation) * cohesionFactor; //(centerX - current.X) * cohesionFactor, (centerY - current.Y) * cohesionFactor);
            return (result.x, result.y);
        }

        // Rule 3: Cohesion 
        // LinkQ with PRECOMPUTED NEIGHBORS version
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, List<EntityNode> boids, Boid current, double cohesionDistance, double cohesionFactor, List<Boid> preNeighbors)
        {
            var neighbors = preNeighbors.Where(x => x != current && GetDistance(current, x) < cohesionDistance);
            if (!neighbors.Any())
            {
                return (0, 0); // No neighbors to cohese with
            }

            double centerX = neighbors.Average(x => x.Translation.x);
            double centerY = neighbors.Average(x => x.Translation.y);

            Vector3d center;
            center.x = centerX;
            center.y = centerY;
            center.z = 0.0d;
            Vector3d result = (center - current.Translation) * cohesionFactor; //(centerX - current.X) * cohesionFactor, (centerY - current.Y) * cohesionFactor);
            return (result.x, result.y);
        }


#if USE_MEMORY_T
        // Rule 3: Cohesion 
        // Memory<T> with Func<> delegate version
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, ComponentStore<Transform.Transform_Struct> store, uint numBoids, int currentIndex, double cohesionDistance, double cohesionFactor, Func<int, int, double, Transform.Transform_Struct, Transform.Transform_Struct, bool> coheseFunc)
        {
            List<int> neighbors = FindNeighbors(store, numBoids, cohesionDistance, currentIndex, coheseFunc);
            Span<Transform.Transform_Struct> mem = store.Span;

            if (neighbors == null || neighbors.Count == 0) return (0, 0);

            Vector3d neighborsAvgCenter;
            neighborsAvgCenter.x = 0;
            neighborsAvgCenter.y = 0;
            neighborsAvgCenter.z = 0;

            for (int i = 0; i < neighbors.Count; i++)
                neighborsAvgCenter += mem[neighbors[i]].Translation;

            neighborsAvgCenter /= neighbors.Count;
            Vector3d result = (neighborsAvgCenter - mem[currentIndex].Translation) * cohesionFactor;

            return (result.x, result.y);
        }

        // Rule 3: Cohesion 
        // Memory<T> Standard version is by far the fastest
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, Span<Transform.Transform_Struct> mem, uint numBoids, int currentIndex, double cohesionDistance, double cohesionFactor)
        {
            Vector3d neighborsAvgCenter;
            neighborsAvgCenter.x = 0;
            neighborsAvgCenter.y = 0;
            neighborsAvgCenter.z = 0;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            int count = 0;
            for (int i = 0; i < numBoids; i++)
            {
                if (i == currentIndex) continue;

                double distance = Vector3d.GetDistance3d(mem[currentIndex].Translation, mem[i].Translation);
                if (distance < cohesionDistance)
                {
                    neighborsAvgCenter += mem[i].Translation;
                    count++;
                }
            }

            if (count == 0) return (0, 0);
            neighborsAvgCenter /= count;

            Vector3d result = (neighborsAvgCenter - mem[currentIndex].Translation) * cohesionFactor;

            return (result.x, result.y);
        }

        // Rule 3: Cohesion 
        // Memory<T> PRECOMPUTED NEIGHBORS version
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, ComponentStore<Transform.Transform_Struct> store, uint numBoids, int currentIndex, double cohesionDistance, double cohesionFactor, List<int> neighbors)
        {
            Span<Transform.Transform_Struct> mem = store.Span;

            Vector3d neighborsAvgCenter;
            neighborsAvgCenter.x = 0;
            neighborsAvgCenter.y = 0;
            neighborsAvgCenter.z = 0;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            int count = 0;
            for (int i = 0; i < neighbors.Count; i++)
            {
                // note: precomputed neighbors will hsve eliminated the commented out line below
                // if (neighbors[i] == currentIndex) continue;

                double distance = Vector3d.GetDistance3d(mem[currentIndex].Translation, mem[neighbors[i]].Translation);
                if (distance < cohesionDistance)
                {
                    neighborsAvgCenter += mem[neighbors[i]].Translation;
                    count++;
                }
            }

            if (count == 0) return (0, 0);
            neighborsAvgCenter /= count;

            Vector3d result = (neighborsAvgCenter - mem[currentIndex].Translation) * cohesionFactor;

            return (result.x, result.y);
        }
		
		
		//////////////////////////////////////////////////////
        public static List<int> FindNeighbors(ComponentStore<Transform.Transform_Struct> store, uint numBoids, double distance, int currentIndex, Func<int, int, double, Transform.Transform_Struct, Transform.Transform_Struct, bool> condition)
        {
            Span<Transform.Transform_Struct> mem = store.Span;

            List<int> neighbors = new List<int>();

			if (currentIndex < 0 || currentIndex > store.Span.Length)
				Console.WriteLine("Boid.FindNeighbors() - Index '" + currentIndex + "' out of range ");
			
			try 
			{
            	for (int i = 0; i < numBoids; i++)
            	{
                	if (condition(i, currentIndex, distance, mem[i], mem[currentIndex]))
                    	neighbors.Add(i);

            	}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Boid.FindNeighbors() - Index '" + currentIndex + "' out of range ");
			}
            return neighbors;
        }

        public static List<int> FindNeighbors(List<Boid> allBoids, double distance, int currentIndex, Func<Boid, Boid, double, bool> condition)
        {
            List<int> neighbors = new List<int>();
			int debug = 0;
			
			try
			{
				for (int i = 0; i < allBoids.Count; i++)
				{
					debug = i;
					if (i > allBoids.Count - 1) continue;
					if (currentIndex > allBoids.Count - 1) continue;
					if (condition(allBoids[i], allBoids[currentIndex], distance))
						neighbors.Add(i);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Boid.FindNeighbors() - Generic Find 'i iterator == " + debug.ToString() + "current boid index that has adjacents == " + currentIndex.ToString() + " total boids count == " + allBoids.Count.ToString());	
			}
            return neighbors;
        }
#else

        public static List<Boid> FindNeighbors(List<Boid> allBoids, double distance, int currentIndex, Func<Boid, Boid, double, bool> condition)
        {
            List<Boid> neighbors = new List<Boid>();

            for (int i = 0; i < allBoids.Count; i++)
            {
                if (condition(allBoids[i], allBoids[currentIndex], distance))
                    neighbors.Add(allBoids[i]);

            }
            return neighbors;
        }
#endif
		#region IDisposable
		public override void DisposeManagedResources()
        {
           if (!mIsDisposed)
           {
			   base.Dispose();
			   
			   foreach (Type t in mUserStructs.Keys)
			   {
				   //object store = EntryClass.mCStoreCol.CheckOut<t.BaseType>(0);
				   
				   //object store = EntryClass.mCStoreCol.CheckOut<LivingEntity>(EntryClass.NUM_ENTRIES); 
				   //store.CheckIn(memT);
			   }
			   
			   // TODO: we need a common interface for these 
			   //IComponentStore perhaps
			   /*
			    ComponentStore<LivingEntity> storeLE = EntryClass.mCStoreCol.CheckOut<LivingEntity>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
			    storeLE.CheckIn(mMemStore_LivingEntity);
			   
			    ComponentStore<TacticalStation> storeTactical = EntryClass.mCStoreCol.CheckOut<TacticalStation>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
			    storeTactical.CheckIn(mMemStore_TacticalStation);
			   
				ComponentStore<Component> storeComponent = EntryClass.mCStoreCol.CheckOut<Component>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
			    storeComponent.CheckIn(mMemStore_Component);
				
			    ComponentStore<Weapon> storeWeapon = EntryClass.mCStoreCol.CheckOut<Weapon>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
			    storeWeapon.CheckIn(mMemStore_Weapon);
				
				ComponentStore<Laser_Struct> storeLaser = EntryClass.mCStoreCol.CheckOut<Laser_Struct>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
			    storeLaser.CheckIn(mMemStore_Laser);
				
			    ComponentStore<ArmorLayer> storeArmorLayer = EntryClass.mCStoreCol.CheckOut<ArmorLayer>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
			    storeArmorLayer.CheckIn(mMemStore_ArmorLayers);
				*/
			   
				
				//Console.WriteLine ("Transform.cs.DisposeManagedResources() - Checked In Living_Entity struct");
			    mIsDisposed = true;
		   }   
        }
	#endregion
    }

    // to set flags use  Parent.ChangeState = ChangeStates.Moved | ChangeStates.Rotated | ChangeStates.Scaled
    // to check the status of a flag you would use a property "ChangeStates node.ChangeState" and then
    // the statement if (node.ChangeState & ChangeStates.Translated) == ChangeStates.Translated
    [Flags]
    public enum ChangeStates : int
    {
        // IMPORTANT: I thought about using some persistant flags here but that is a mistake.
        // ChangeStates should ONLY include those states which are only important at runtime.
        None = 0,
        ChildNodeAdded = 1 << 1,
        ChildNodeRemoved = 1 << 2,
        GeometryAdded = 1 << 3 | ChildNodeAdded, // WARNING! MUST be careful with combining '|' other flags here like GeometryAdded = 1 << 3 | ChildNodeAdded | BoundingBox_Dirty because when you clear the flag for ChildNodeAdded it will also clear the BoundingBox_Dirty when you dont intend to
        GeometryRemoved = 1 << 4 | ChildNodeRemoved, // TODO: In fact I should ban the practice altogether 

        ViewpointAdded = 1 << 5 | ChildNodeAdded,
        ViewpointRemoved = 1 << 6 | ChildNodeRemoved,


        BoundingBoxDirty = 1 << 7,
        BoundingBox_TranslatedOnly = 1 << 8,
        MatrixDirty = 1 << 9,
        RegionMatrixDirty = 1 << 10,
        GlobalMatrixDirty = 1 << 11,
        Translated = 1 << 12,
        Rotated = 1 << 13,
        Scaled = 1 << 14,

        // NOTE: we don't combine flags because when we DisableChangeStates, it also disables ALL the relevant bits in the flag
        // Translated = 1 << 12 | BoundingBox_TranslatedOnly | MatrixDirty,
        // Rotated = 1 << 13 | BoundingBoxDirty  | MatrixDirty,
        // Scaled = 1 << 14 | BoundingBoxDirty | MatrixDirty,
        KeyFrameUpdated = 1 << 15,  // AnimationUpdated same thing
        AppearanceNodeChanged = 1 << 28, // MaterialNodeChanged, TextureNodeChanged
        AppearanceParameterChanged = 1 << 16,  // when Appearance parameters are changed 
        ShaderParameterValuesChanged = 1 << 17,
        ShaderFXLoaded = 1 << 18,
        ShaderFXUnloaded = 1 << 19,
        EntityScriptLoaded = 1 << 20,    // scripts paged in or paged out (NOT just added/removed)
        DomainScriptUnloaded = 1 << 21,
        BehaviorScriptLoaded = 1 << 22,
        BehaviorScriptUnloaded = 1 << 23,
        TargetChanged = 1 << 24,  // typically used by animations which are directly parented to Entity so Entity will be only receiver
        EventHandlerChanged = 1 << 25,

        EntityMoved = 1 << 26,  // required only for scene listener
        EntityResized = 1 << 27, // required only for scene listener
        PhysicsNodeAdded = 1 << 28,
        PhysicsNodeRemoved = 1 << 29,
        All = int.MaxValue
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN NODES
    public class EntityNode : Transform
    {
        protected string mID; // entityKey
		protected int mArrayIndex; // index within a List<Boids> or List<Sensors> etc.
		
        protected BoundingBox _box;
        protected OctreeOctant _octant;
		
		protected UserData mUserData;
		public Dictionary<SKILLS, Skill> Skills;
		
		
        public EntityNode(string entityKey, int arrayIndex, double x, double y, double z, double xV, double yV) 
			: base (arrayIndex, x, y, z, xV, yV)
        {
            mArrayIndex = arrayIndex;				
			mID = entityKey;
			mUserData = EntryClass.mUserDataStore.CheckOut(mID);
				
			Skills = new Dictionary<SKILLS, Skill>();		
        }
		
		public string EntityKey { get {return mID;}}
		
        public int EntityArrayIndex { get { return mArrayIndex; } set {mArrayIndex = value;}}
		
		public int UserTypeID { get {return mUserData.UserTypeID; } set { mUserData.UserTypeID = value;}}
		
		public UserData BlackBoardData { get {return mUserData;} set {mUserData = value;} }
		
        public BoundingBox BoundingBox
        {
            get { return _box; }
			//TEMP HACK allow Set here since we have no meshes to compute automatically
			set { _box = value;}
        }

		public OctreeOctant SpatialNode
        {

            get { return _octant; }
            set { _octant = value; }
        }

	#region Custom Properties
		PropertySpec[] mCustomProperties;
		public void SetCustomProperties(PropertySpec[] buildSpecificProperties)
		{
			mCustomProperties = buildSpecificProperties;
		}
		
		public PropertySpec[] GetCustomProperties()
		{
			return mCustomProperties;
		}
	#endregion

				
	#region PLACE_THIS_CODE_IN_SCRIPT_FOR_TACTICAL_STATION
		private List<Target> mTargets;
		public List<Target> GetTargets()
		{
			return mTargets;
		}
		
		public void Add (Target t)
		{
			if (mTargets == null) mTargets = new List<Target>();
			
			// if the target already exists, replace it with current data?
			int found = -1;
			for (int i = 0; i < mTargets.Count; i++)
				if (mTargets[i].EntityArrayIndex == t.EntityArrayIndex)
				{
					found = i;
					break;
				}
			
			if (found == -1)
				mTargets.Add(t);
			else
				mTargets[found] = t;
		}
		
		public void Add (Target[] t)
		{
			if (t == null || t.Length == 0) return;
			
			for (int i = 0; i < t.Length; i++)
				Add(t[i]);
		}
		
		public void ClearTargets()
		{
			if (mTargets != null)
				mTargets.Clear();
		}
		
		public Target GetTarget (int entityArrayIndex)
		{
			if (mTargets == null || mTargets.Count == 0) return default(Target);
			
			for (int i = 0; i < mTargets.Count; i++)
				if (mTargets[i].EntityArrayIndex == entityArrayIndex)
					return mTargets[i];
			
			return default(Target);
		}
		        
		private List<SensorContact> mSensorContacts;
		public List<SensorContact> GetSensorContacts()
		{
			return mSensorContacts;
		}
		
		public void Add (SensorContact c)
		{
			
			if (mSensorContacts == null) mSensorContacts = new List<SensorContact>();
			//Console.WriteLine("EntityNode.Add(SensorContact) - 222 SensorContact added to Entity '" + mID + "'. Total Contacts Count == " + mSensorContacts.Count.ToString());

			int found = -1;
			for (int i = 0; i < mSensorContacts.Count; i++)
				if (mSensorContacts[i].Name == c.Name)
				{
					found = i;
					break;
				}
			
			//Console.WriteLine("EntityNode.Add(SensorContact) - 333 SensorContact added to Entity '" + mID + "'. Total Contacts Count == " + mSensorContacts.Count.ToString());
			if (found == -1) 
				mSensorContacts.Add (c);
			else 
				mSensorContacts[found].Add(c.Telemetry);
			
			//Console.WriteLine("EntityNode.Add(SensorContact) - 444 SensorContact added to Entity '" + mID + "'. Total Contacts Count == " + mSensorContacts.Count.ToString());
		}
		
		public void Add (List<SensorContact> contacts)
		{
			if (contacts == null) return;
			for (int i = 0; i < contacts.Count; i++)
				Add(contacts[i]);
		}
	#endregion
		
		
		
		
		
	#region IDisposable
		public override void DisposeManagedResources()
        {
           if (!mIsDisposed)
           {
			   base.Dispose();
			   
			   // todo: verify this.Index should not be this.ID (a string) in KGB Entity.cs since
			   //       maintaining the "Index" within a ComponentStore<> will be needlessly complicated
			   EntryClass.mUserDataStore.CheckIn(this.mID, mUserData);
			   mIsDisposed = true;
		   }   
        }
	#endregion
		
    }



    // Transform node type. Entities and Models inherit this type.
    // NOTE: The global variables are almost exclusively as they relate to Zones.
    //       Otherwise the mDerived* vars are our worldspace variables.  Adjacent Zones
    //       get oriented with respect to the camera's Region/Zone. This means
    //       only globalTranslation is used and globalscale and globalrotation are Identity.
    //
    // TODO: should we derive a PhysicalTransform node for PhysicsBodies?
    // that can host our mOldTransform and mPreviousStepTransform vars?
    public class Transform
#if USE_MEMORY_T
            : IDisposable
#endif
    {
        private ChangeStates mChangeStates = ChangeStates.None;

        // TODO: currently inheritScale and inheritRotation are treated as
        // bools since a bool can potentially take up just 1 bit instead of 32
        // so no need to merge these into a single 32bit flag.
        //private const int INHERIT_ROTATION = 1 << 0;
        //private const int INHERIT_SCALE = 1 << 1;

        private bool mInheritScale;
        private bool mInheritRotation;

        public int AttachedToBoneID;

        protected Vector3d mPivot;
        protected Vector3d mPreviousTranslation;

#if USE_MEMORY_T == false
        // local scale, translation and rotation
        protected Vector3d mScale, mTranslation;
        protected Quaternion mRotation;

        // region centric translation, scale, and rotation 
        protected Quaternion mDerivedRotation;
        protected Vector3d mDerivedTranslation;
        protected Vector3d mDerivedScale;

        // global scale, rotation and translation (note: translation includes zone translations)
        protected Vector3d mGlobalScale, mGlobalTranslation;
        protected Quaternion mGlobalRotation;

        // cached matrix will automatically include derived versions if enabled
        protected Matrix mMatrix; // RegionMatrix
        protected Matrix mLocalMatrix;
        protected Matrix mGlobalMatrix;
#endif

        // difference in translation between current and previous
        protected Vector3d mTranslationDelta;

		protected Dictionary<Type, Tuple<int, object>> mUserStructs;
		//protected Dictionary<Type, int> mUserStructIndices;
				
#if USE_MEMORY_T
        public Memory<Transform_Struct> mMemStore_Transform; // This var must be accessible to any DATAPROCESSOR if USE_MEMORY<T> == TRUE
		
	
				
        //[StructLayout(LayoutKind.Sequential)]  // NOTE: "ideal" total struct size for L1 cache row purposes is 64 bytes.
        public struct Transform_Struct
        {
			public int EntityArrayIndex;
			public CONFIGURATION Configuration;
			
			//public int EntityArrayIndex;
			//public int InternalTransformIndex;
			
            //public string EntityID;
            public Vector3d Velocity;            // 24 bytes

            //public Vector3d Pivot;

            public Vector3d Translation;          // 24 bytes
            //public Vector3d DerivedTranslation;
            //public Vector3d GlobalTranslation;

            public Vector3d Scale;                // 24 bytes
            //public Vector3d DerivedScale;
            //public Vector3d GlobalScale;

            public Quaternion Rotation;           // 32 bytes
            //public Quaternion DerivedRotation;
            //public Quaternion GlobalRotation;

            //public Matrix RegionMatrix;
        }
#endif


        protected Transform()
        {
#if USE_MEMORY_T
            ComponentStore<Transform_Struct> store = EntryClass.mCStoreCol.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES * 2); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
            int index = -1;
            mMemStore_Transform = store.CheckOut(out index);
			AddUserStruct(typeof(Transform.Transform_Struct), mMemStore_Transform, index);
	
			//mMemStore_Transform.Span[0].InternalTransformIndex = index; // <-- Important to do this here. Eventually we need to be able to modify these when/if our Memory<T> records are ordered differently at runtime
	
#else
            mMatrix = Matrix.Identity();
            mScale.x = 1;
            mScale.y = 1;
            mScale.z = 1;
            mTranslation.x = 0;
            mTranslation.y = 0;
            mTranslation.z = 0;
            mRotation = new Quaternion();
            //_rotation.X = 0;
            //_rotation.Y = 0;
            //_rotation.Z = 0;
            //_rotation.W = 1;
            mPivot.x = 0;
            mPivot.y = 0;
            mPivot.z = 0;
#endif

            AttachedToBoneID = -1;

            //SetChangeFlags(Enums.ChangeStates.Translated |
            //               Enums.ChangeStates.Scaled |
            //               Enums.ChangeStates.Rotated |
            //               Enums.ChangeStates.MatrixDirty |
            //               Enums.ChangeStates.RegionMatrixDirty |
            //               Enums.ChangeStates.GlobalMatrixDirty | 
            //              Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);

            // by default we inherit rotations however
            // stellar system components like stars, planets, moons, asteroids do not
            // Currently what we do is in our ProceduralHelper.cs is to manually
            // set these two flags to false.
            mInheritRotation = true;

            // In a hierarchical scene, Transform derived nodes should always inherit scale.  The variable
            // is available however if for certain elements such as GUI Widgets, HUD root elements, etc
            // where we always want independant scaling.  But for things like Engine nacelles, we want
            // them to inherit scale of Vehicle they are attached to.
            mInheritScale = true;
            // If we don't intend for a scale set on an Entity to pass to a child Entity, then we should 
            // set that scale on the Parent entity's child Model instead. 
            //  Entity         <-- don't set scale here
            //		|___Model   <-- set scale on Model instead
            //		|__Entity   <-- child Entity will now not inherit scale
            //			|__Model

            //Shareable = false; // Transform nodes and derived can never be shared.
        }
				
		protected Transform (int arrayIndex, double x, double y, double z, double xV, double yV) : this()
		{
			#if USE_MEMORY_T
				Vector3d translation = new Vector3d(x, y, z);
				mMemStore_Transform.Span[0].Velocity = new Vector3d(xV, yV, 0d);
				mSpanAccessTest = translation;
				mMemStore_Transform.Span[0].Translation = mSpanAccessTest;// translation;
				mMemStore_Transform.Span[0].EntityArrayIndex = arrayIndex;
			
			#else
				mMatrix = Matrix.Identity();
				mScale.x = 1;
				mScale.y = 1;
				mScale.z = 1;
				mTranslation.x = x;
				mTranslation.y = y;
				mTranslation.z = z;
				Translation = mTranslation;
				mRotation = new Quaternion();
				//_rotation.X = 0;
				//_rotation.Y = 0;
				//_rotation.Z = 0;
				//_rotation.W = 1;
				mPivot.x = 0;
				mPivot.y = 0;
				mPivot.z = 0;
	
				mVelocity.x = xV;
				mVelocity.y = yV;
			#endif
		}

		private uint mConfiguration;
				
		public bool HasConfiguration (uint configuration)
		{
			return (mConfiguration & configuration) != 0;
		}
			
		public uint Configuration { get {return mConfiguration; } set {mConfiguration = value; }}
				
		/*
		/// <summary>
		/// The typeAsKey is the T part of the Memory<T> we pass in, and NOT the Typeof(Memory<T>)
		/// </summary>
		public void AddUserStruct(object memStore, int indexWithinMemStore)
		{
			// NOTE: We pass in the Memory<T> and not just the Typeof(T)
			//       So we need to get the internal type used by the Memory<T>
			
			string genericTypeName = memStore.GetType().FullName;
			// our Memory<T>'s will look as follows:
			// 'System.Memory`1[[HelloBoids.Laser_Struct, nkj43iat.exe, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]'
			Console.WriteLine("Transform.AddUserStruct() - Typename = '" + genericTypeName + "'");
			
			// if we want to parse out just the first string
			int start = genericTypeName.IndexOf("[[") + 2;
			int end = genericTypeName.IndexOf(",");
											  
		    genericTypeName = genericTypeName.Substring(start, end - start);
			
			start = genericTypeName.IndexOf("+");
			end =
			
			// For Memory<T> just use the above, the below is  NOT what we want
        	// Remove the generic arity part (e.g., "`1")
        	//genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
			Console.WriteLine("Transform.AddUserStruct() - Typename = '" + genericTypeName + "'");
			
			AddUserStruct (typeAsKey, memStore, indexWithinMemStore);
		}
		*/
		
		/// <summary>
		/// The typeAsKey is the T part of the Memory<T> we pass in, and NOT the Typeof(Memory<T>)
		/// </summary>
		public void AddUserStruct(Type t, object memStore, int indexWithinMemStore)
		{
			if (mUserStructs == null) mUserStructs = new Dictionary<Type, Tuple<int, object>>();
			
			//Console.WriteLine ("EntityNode.AddUserStruct() - Adding User Struct '" + typename + "'");
			Tuple<int, object> tup = new Tuple<int, object>(indexWithinMemStore, memStore);
			mUserStructs.Add(t, tup); //memStore);
			
			//Console.WriteLine ("Transform.AddUserStruct() - INTERNAL = " + tup.Item1.ToString());
		}
			
		public Dictionary<Type, Tuple<int, object>> GetUserStructs()
		{
			return mUserStructs;
		}
				
		public object GetUserStruct(Type t, out int index)
		{
			index = -1;
			//Console.WriteLine ("EntityNode.GetUserStruct '" + typename + "'");
			if (mUserStructs == null) return null;
			
			Tuple<int, object> result;
			if (mUserStructs.TryGetValue(t, out result))
			{
				index = result.Item1;
				return result.Item2;
			}	
			return null;
		}
		
		public int GetUserStructIndex(Type t)
		{
			int index = -1;
			//Console.WriteLine ("Transform.GetUserStruct '" + t.Name + "'");
			if (mUserStructs == null) return index;
			
			
			Tuple<int, object> result;
			if (mUserStructs.TryGetValue(t, out result))
			{
				index = result.Item1;
			}
			//else 
			//{
				//Console.WriteLine ("Transform.GetUserStruct() - FAILED TO FIND '" + t.Name + "'  UserStruct.Count ==  " + mUserStructs.Count.ToString());
				//List<Type> keyList = new List<Type>(this.mUserStructs.Keys);
				//Console.WriteLine ("Transform.GetUserStruct() -  Position 0 == " + keyList[0].ToString());
			
			//}
			return index;
		}
		
        #region ResourceBase members

        /*/// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[10 + tmp.Length];
            tmp.CopyTo(properties, 10);

        properties[0] = new Settings.PropertySpec("inheritscale", mInheritScale.GetType().Name);
            properties[1] = new Settings.PropertySpec("inheritrotation", mInheritRotation.GetType().Name);

            properties[2] = new Settings.PropertySpec("position", mTranslation.GetType().Name);
            properties[3] = new Settings.PropertySpec("scale", mScale.GetType().Name);
            properties[4] = new Settings.PropertySpec("rotation", mRotation.GetType().Name);

            properties[5] = new Settings.PropertySpec("velocity", mVelocity.GetType().Name);
            properties[6] = new Settings.PropertySpec("acceleration", mAcceleration.GetType().Name);
            properties[7] = new Settings.PropertySpec("force", mForce.GetType().Name);
            properties[8] = new Settings.PropertySpec("angularforce", mAngularForce.GetType().Name);
            properties[9] = new Settings.PropertySpec("angularvelocity", mAngularVelocity.GetType().Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = mInheritScale;
                properties[1].DefaultValue = mInheritRotation;


                properties[2].DefaultValue = mTranslation;
                properties[3].DefaultValue = mScale;
                properties[4].DefaultValue = mRotation;


                properties[5].DefaultValue = mVelocity;
                properties[6].DefaultValue = mAcceleration;
                properties[7].DefaultValue = mForce;
                properties[8].DefaultValue = mAngularForce;
                properties[9].DefaultValue = mAngularVelocity;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].DefaultValue == null) continue;
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "inheritscale":
                        InheritScale = (bool)properties[i].DefaultValue;
                        break;
                    case "inheritrotation":
                        InheritRotation = (bool)properties[i].DefaultValue;
                        break;



                    case "position":
                        Translation = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "scale":
                        Scale = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "rotation":
                        Rotation = (Quaternion)properties[i].DefaultValue;
                        break;


            // Physics will be moved to Entity.PhysicsBody which will implement IPhysicsBody (as will RigidBody.cs)
                    case "velocity":
                        mVelocity = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "acceleration":
                        mAcceleration = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "force":
                        mForce = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "angularforce":
                        mAngularForce = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "angularvelocity":
                        mAngularVelocity = (Vector3d)properties[i].DefaultValue;
                        break;
                }
            }

            // NOTE: the following flags are set in the property Settors
    //            SetChangeFlags(Enums.ChangeStates.BoundingBoxDirty |
    //                Enums.ChangeStates.GlobalMatrixDirty |
    //                Enums.ChangeStates.MatrixDirty |
    //                Enums.ChangeStates.RegionMatrixDirty, Enums.ChangeSource.Self);
        }
        */
        #endregion



        /// <summary>
        /// In a hierarchical scene, Transform derived nodes shoudl always inherit scale.  The variable
        /// is available however if for certain elements such as GUI Widgets, HUD root elements, etc
        /// where we always want independant scaling.
        /// </summary>
        public bool InheritScale
        {
            get { return mInheritScale; }
            set
            {
                mInheritScale = value;
                /*SetChangeFlags(
                    Enums.ChangeStates.MatrixDirty | 
                    Enums.ChangeStates.RegionMatrixDirty |
                    Enums.ChangeStates.GlobalMatrixDirty | 
                    Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
                */
            }
        }

        public bool InheritRotation
        {
            get { return mInheritRotation; }
            set
            {
                mInheritRotation = value;
                /*SetChangeFlags(
                    Enums.ChangeStates.MatrixDirty |
                    Enums.ChangeStates.RegionMatrixDirty |
                    Enums.ChangeStates.GlobalMatrixDirty |
                    Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
                */
            }
        }



        #region PHYSICS - MOVES ALL THIS TO Entity.PhysicsBody which implements IPhysicsBody

        // TODO: Dynamic physics items aren't related to Transform, but... how should we track our physics?
        // do we use the traditional PhysicsBody composite object?
        // These could maybe be moved to the new RigidBody class we've added (July.17.2019)
        // TODO: but if these exist in the RigidBody node, how do we apply results to the Entity itself?
        //  SHouldn't the RigidBody apply only the velocities (angular and linear)?  The RigidBody as needed
        //  could grab any Transform info it needs from the Entity.
        // Upon polling the events each physics step, we can update the relevant velocities on the Entity?
        protected Vector3d mPreviousStepTranslation;
        protected Vector3d mPreviousStepScale;
        protected Quaternion mPreviousStepRotation;

        protected Vector3d mVelocity = Vector3d.Zero();
        protected Vector3d mAcceleration = Vector3d.Zero();
        protected Vector3d mForce = Vector3d.Zero();

        protected Vector3d mAngularVelocity = Vector3d.Zero();
        protected Vector3d mAngularAcceleration = Vector3d.Zero();
        protected Vector3d mAngularForce = Vector3d.Zero();

        public virtual Vector3d Force
        {
            get { return mForce; }
            set { mForce = value; }
        }

        public virtual Vector3d Acceleration
        {
            get { return mAcceleration; }
            set { mAcceleration = value; }
        }

        // NOTE: Velocity may be overriden by SteerableEntity.cs 
        public virtual Vector3d Velocity
        {
            get
            {
				#if USE_MEMORY_T
					return mMemStore_Transform.Span[0].Velocity;
				#else
                	return mVelocity;
				#endif
            }
            set
            {
                mVelocity = value;
            }
        }

        /// <summary>
        /// torque
        /// </summary>
        public virtual Vector3d AngularForce
        {
            get { return mAngularForce; }
            set { mAngularForce = value; }
        }

        public virtual Vector3d AngularAcceleration
        {
            get { return mAngularAcceleration; }
            set { mAngularAcceleration = value; }
        }

        public virtual Vector3d AngularVelocity
        {
            get { return mAngularVelocity; }
            set { mAngularVelocity = value; }
        }
        #endregion



        /// <summary>
        /// Previous translation from previous frame
        /// </summary>
        public Vector3d PreviousTranslation { get { return mPreviousTranslation; } }

        // TODO: don't all these previous step versions end up needing
        //       to compute a matrix for rendering ?  because
        // well, we need to use this in model.Render() to compute the matrix
        // to set on the geometry.  We compute the interpolated value and render 
        // with that.
        public Vector3d LatestStepTranslation
        {
            get { return mPreviousStepTranslation; }
            set { mPreviousStepTranslation = value; }
        }

        public Vector3d LatestStepScale
        {
            get { return mPreviousStepScale; }
            set { mPreviousStepScale = value; }
        }

        public Quaternion LatestStepRotation
        {
            get { return mPreviousStepRotation; }
            set { mPreviousStepRotation = value; }
        }


#if USE_MEMORY_T
        // variables stored in contiguous array of structs via Memory<T>
        //public string GetEntityID { get { return mMemStore.Span[0].EntityID; } }

        /*public Vector3d Pivot
        {
            get { return mMemStore.Span[0].Pivot; }
            set { mMemStore.Span[0].Pivot = value; }
        }*/
        Vector3d mSpanAccessTest;
        public Vector3d Translation
        {
            get
            {
                // https://www.codemag.com/Article/2207031/Writing-High-Performance-Code-Using-SpanT-and-MemoryT-in-C
                return mSpanAccessTest; // NOTE: <-- this line is much faster than returning the Translation from the below line!  
                                        // / What we want to do is cache/grab the entire Span[0] once for this Entity/Boid and then directly just modify IT and not this accessor!!!
                                        //
                return mMemStore_Transform.Span[0].Translation;
            }
            set
            {
                mSpanAccessTest = value;
                mMemStore_Transform.Span[0].Translation = value;
            }
        }
        /*
        public Vector3d DerivedTranslation
        {
            get { return mMemStore.Span[0].DerivedTranslation; }
        }
        public Vector3d GlobalTranslation
        {
            get { return mMemStore.Span[0].GlobalTranslation; }
            set { mMemStore.Span[0].GlobalTranslation = value; }
        }*/

        public Vector3d Scale
        {
            get { return mMemStore_Transform.Span[0].Scale; }
            set { mMemStore_Transform.Span[0].Scale = value; }
        }
        /*
        public Vector3d DerivedScale
        {
            get { return mMemStore.Span[0].DerivedScale; }
        }
        public Vector3d GlobalScale
        {
            get { return mMemStore.Span[0].GlobalScale; }
        }*/
        public Quaternion Rotation
        {
            get { return mMemStore_Transform.Span[0].Rotation; }
            set { mMemStore_Transform.Span[0].Rotation = value; }
        }
        /*
        public Quaternion DerivedRotation
        {
            get { return mMemStore.Span[0].DerivedRotation; }
        }
        public Quaternion GlobalRotation
        {
            get { return mMemStore.Span[0].GlobalRotation; }
        }*/

#else
        /// <summary>
        /// Local Space Position
        /// </summary>
        public virtual Vector3d Translation
        {
            get { return mTranslation; }
            set
            {
                //            	if (this is Model && ((Model)this).Geometry is MinimeshGeometry && value == Vector3d.Zero())
                //            		System.Diagnostics.Debug.WriteLine ("err");
                //            	
                mTranslationDelta = value - mTranslation;
                // May.16.2017 - even if mTranslationDelta equals Vector3d.Zero() we can't "return". We need to SetChangeFlags
                //               or the Viewpoint used by ViewpointController will jitter.  Maybe it's because we need
                //               mPreviousStepTranslation to update.  Eitherway, the following line must remain commented out.
                //if (mTranslationDelta.Equals(Vector3d.Zero())) return;

                mTranslation = value;
                // TODO: arg, this previoussteptranslation crap oct.9.2014 temp hack as we implement steering 
                // behaviors again with Dynamic flag to true.  We need to solve this long term where modifying 
                // Translation through script or API or plugin will also update the previousStep if 
                // Dynamic == true, or when enabling Dynamic, it initializes previousStepTranslation to Translation
                mPreviousStepTranslation = Translation;

                //if (this is Entities.Entity && ((Entities.Entity)this).Name == "helm")
                //    System.Diagnostics.Debug.WriteLine("helm translation because it had EntityAttributes.Dynamic set");

                //SetChangeFlags(
                //	Enums.ChangeStates.Translated |
                //    Enums.ChangeStates.MatrixDirty | 
                //    Enums.ChangeStates.RegionMatrixDirty |
                //   Enums.ChangeStates.GlobalMatrixDirty | 
                //   Enums.ChangeStates.BoundingBox_TranslatedOnly, Enums.ChangeSource.Self);
            }
        }

        public virtual Vector3d Scale
        {
            get { return mScale; }
            set
            {
#if DEBUG
            if (value == Vector3d.Zero()) throw new ArgumentOutOfRangeException("Transform.Scale cannot be 0,0,0");
#endif
                if (value == mScale) return; // some thigns have their scale altered all the time such as for percentage screenspace scaling and if the scale value doesnt change, no need to alter

                //if (this is Entities.Entity && ((Entities.Entity)this).Name == "helm")
                //    System.Diagnostics.Debug.WriteLine("helm scale err");

                mScale = value;
                //SetChangeFlags(
                //     Enums.ChangeStates.Scaled |
                //    Enums.ChangeStates.MatrixDirty |
                //    Enums.ChangeStates.RegionMatrixDirty |
                //   Enums.ChangeStates.GlobalMatrixDirty |
                //   Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
            }
        }

        /// <summary>
        /// Local Space Rotation
        /// </summary>
        public virtual Quaternion Rotation
        {
            get { return mRotation; }
            set
            {
                if (value.IsNullOrEmpty()) return;
                if (value.Equals(mRotation)) return; // some things have their rotaton altered all the time but never actually change, no need to set change flags here
                mRotation = value;


                //  SetChangeFlags(
                // 	Enums.ChangeStates.Rotated |
                //    Enums.ChangeStates.MatrixDirty |
                //    Enums.ChangeStates.RegionMatrixDirty |
                //   Enums.ChangeStates.GlobalMatrixDirty |
                //   Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
            }
        }

        public Vector3d Pivot
        {
            get { return mPivot; }
            set
            {
                mPivot = value;
                if (value == mPivot) return;
                //   SetChangeFlags(
                //       Enums.ChangeStates.MatrixDirty |
                //      Enums.ChangeStates.RegionMatrixDirty |
                //      Enums.ChangeStates.GlobalMatrixDirty |
                //      Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
            }
        }

        public Vector3d DerivedTranslation
        {
            get
            {
                // NOTE: translation can be altered by parent scale as well as translation from parent or self
                if ((mChangeStates & (ChangeStates.Translated | ChangeStates.Scaled)) != 0)
                    UpdateRegional();

                return mDerivedTranslation;
            }
        }

        public Vector3d DerivedScale
        {
            get
            {
                if ((mChangeStates & ChangeStates.Scaled) == ChangeStates.Scaled)
                    UpdateRegional();

                return mDerivedScale;
            }
        }

        public Quaternion DerivedRotation
        {
            get
            {
                if ((mChangeStates & ChangeStates.Rotated) == ChangeStates.Rotated)
                    UpdateRegional();
                return mDerivedRotation;
            }
        }

        public virtual Vector3d GlobalTranslation
        {
            get
            {
                // global translation is dirty whenever a) this node translates b) it's parent node translates c) it's parent node's Global translation has changed,
                // but we dont always need to know the most up to date value 
                // so maybe we need more flags for these so we can clear Translated flag after local update, but still know that
                // global still needs to be updated (is dirty) if we should try to grab it's value
                // global values aren't requested very often.  We try to do most calcs in Region space.  I think mostly it's camera and Regions which use these
                if ((mChangeStates & ChangeStates.GlobalMatrixDirty) == ChangeStates.GlobalMatrixDirty)
                    UpdateGlobal();

                Vector3d result = GlobalMatrix.GetTranslation();
                //System.Diagnostics.Debug.Assert (result == mGlobalTranslation);

                return mGlobalTranslation;
            }
            set
            {
                mGlobalTranslation = value;
                /*
                if (mParents == null || mParents[0] == null)
                {
                    // there is no parent so GlobalTranslation is same as local
                    Translation = mGlobalTranslation; // calling public property setter instead of private var will trigger appropriate SetChangeFlags
                    return;
                }

                Transform parent = (Transform)mParents[0];

                // we want to transform coordinate from (src) global to (dest) local identity space
                Matrix source2dest = Matrix.Inverse (parent.GlobalMatrix); // Matrix.Source2Dest(parent.GlobalMatrix, Matrix.Identity());
                Matrix locallyTransformedMatrix = Matrix.Multiply4x4(source2dest, Matrix.CreateTranslation (value));
                Vector3d result = locallyTransformedMatrix.GetTranslation();

                // TODO: for Zones this is wrong.  Not even sure for other Entity types because we dont use it much but my recollection
                //       is that it is also wrong when trying to place entities in multi-zone region with asset placement tool.
                Translation = result; // calling public property setter instead of private var will trigger appropriate SetChangeFlags
                */
            }
        }

        public Vector3d GlobalScale
        {
            get
            {
                // global scale is dirty whenever a) this node re-scales b) it's parent node's scales c) it's parent node's Global scale has changed
                // so maybe we need more flags for these?
                // global values aren't requested very often.  We try to do most calcs in Region space.  I think mostly it's camera and Regions which use these
                if ((mChangeStates & ChangeStates.GlobalMatrixDirty) == ChangeStates.GlobalMatrixDirty)
                    UpdateGlobal();

                return mGlobalScale;
            }
        }

        public Quaternion GlobalRotation
        {
            get
            {
                // global rotation is dirty whenever a) this node rotates b) it's parent node's rotated c) it's parent node's Global rotation has changed
                // so maybe we need more flags for these?
                // global values aren't requested very often.  We try to do most calcs in Region space.  I think mostly it's camera and Regions which use these
                if ((mChangeStates & ChangeStates.GlobalMatrixDirty) == ChangeStates.GlobalMatrixDirty)
                    UpdateGlobal();
                return mGlobalRotation;
            }
        }


        /// <summary>
        /// Local Matrix is nearly obsolete because what we primarily store are LOCAL translation, 
        /// scale, and orientation quaternion. 
        /// 
        /// This is only used by MoveTool/RotateTool/ScaleTool and ScaleDrawer _all_ for
        /// EditableMesh which is edited in modelspace and not in the coordinate system of the current 
        /// Region
        ///
        ///  Local matrix is cached primarily so that we can properly compare differences in
        /// translation to the position elements already in the matrix when translation is the only thing
        /// that has changed.
        /// Even setting a local matrix just result in the diffferent vector components being created. 
        /// 
        /// Local Matrix is always relative to the parent.
        /// </summary>
        public Matrix LocalMatrix
        {
            get
            {
                // this override of the get{} performs a lazy update of the WorldMatrix
                // if it's dirty.  It's exactly like what happens with the getter on BoundingBox()
                // When trying to access the RelativeMatrix, if the position, scale, translation
                // has changed for this Model, the appropriate flags will get set and we must
                // compute  a new one.
                if (mLocalMatrix.IsNullOrEmpty() || (mChangeStates & ChangeStates.MatrixDirty) == ChangeStates.MatrixDirty)
                {
                    // update local matrix
                    Matrix tmat = Matrix.CreateTranslation(mTranslation);
                    Matrix smat = Matrix.CreateScaling(mScale);
                    Matrix rmat = new Matrix(mRotation);
                    //Matrix Rx = Matrix.RotationX(_rotation.x * Utilities.MathHelper.DEGREES_TO_RADIANS);
                    //Matrix Ry = Matrix.RotationY(_rotation.y * Utilities.MathHelper.DEGREES_TO_RADIANS);
                    //Matrix Rz = Matrix.RotationZ(_rotation.z * Utilities.MathHelper.DEGREES_TO_RADIANS);
                    // The order these rotations are performed to match TV3D is: Yaw(y), Pitch(x), then Roll (z). 
                    //_localMatrix = S*Ry*Rx*Rz*T;
                    //                Usually, its;
                    //// scale * rotation * translation
                    ////But if you want the object to rotate (orbit) around a certain point, then:
                    ////scale* translationToCertainPoint * rotation * translationToObjectPosition
                    ////_localMatrix = smat * RotationMatrix * tmat;
                    if (mPivot == Vector3d.Zero())
                        mLocalMatrix = smat * rmat * tmat;
                    else
                    {
                        Matrix offsetMat = Matrix.CreateTranslation(mPivot);
                        Matrix negativeOffsetMat = Matrix.CreateTranslation(-mPivot);
                        mLocalMatrix = smat * offsetMat * rmat * negativeOffsetMat * tmat;
                    }

                    // DisableChangeFlags(Enums.ChangeStates.MatrixDirty);
                }
                return mLocalMatrix;
            }
        }

        //// RegionMatrix is an entity's transform in relation to the Region it's in.  
        //// Since we only render in Region space with camera space offset, this makes our RegionMatrix
        //// akin to our WorldMatrix since this is the resulting value we plug into the d3d device
        //// To render across Regions, we still use this RegionMatrix however we compute a transform
        //// for the camera view to transform an entity that lies in one region, to be relative to the
        //// current camera's region.
        //private Matrix result;
        public virtual Matrix RegionMatrix
        {
            get
            {
                if ((mChangeStates & ChangeStates.RegionMatrixDirty) != 0)
                {
                    UpdateRegional();

                    Matrix tmat = Matrix.CreateTranslation(mDerivedTranslation);
                    Matrix smat = Matrix.CreateScaling(mDerivedScale);
                    Matrix rmat = new Matrix(mDerivedRotation);

                    //Matrix Rx = Matrix.RotationX(_rotation.x * Utilities.MathHelper.DEGREES_TO_RADIANS);
                    //Matrix Ry = Matrix.RotationY(_rotation.y * Utilities.MathHelper.DEGREES_TO_RADIANS);
                    //Matrix Rz = Matrix.RotationZ(_rotation.z * Utilities.MathHelper.DEGREES_TO_RADIANS);
                    // The order these rotations are performed to match TV3D is: Yaw(y), Pitch(x), then Roll (z). 
                    //_matrix = S*Ry*Rx*Rz*T;
                    //                Usually, its;
                    //// scale * rotation * translation
                    ////But if you want the object to rotate (orbit) around a certain point, then:
                    ////scale* translationToCertainPoint * rotation * translationToObjectPosition
                    ////_matrix = smat * RotationMatrix * tmat;


                    // NOTE: smat * rmat * tmat is evaluated as (smat * rmat) * tmat
                    // http://msdn.microsoft.com/en-us/library/ms173145.aspx
                    // When two or more operators that have the same precedence are present in an 
                    // expression, they are evaluated based on associativity. Left-associative 
                    // operators are evaluated in order from left to right. For example, 
                    // x * y / z is evaluated as (x * y) / z. Right-associative operators are 
                    // evaluated in order from right to left. For example, the assignment operator
                    // is right associative. 
                    //          _matrix = smat * rmat * tmat;


                    if (mPivot == Vector3d.Zero())
                        mMatrix = smat * rmat * tmat;
                    else
                    {
                        Matrix offsetMat = Matrix.CreateTranslation(mPivot);
                        Matrix negativeOffsetMat = Matrix.CreateTranslation(-mPivot);
                        mMatrix = smat * offsetMat * rmat * negativeOffsetMat * tmat;
                    }

                    //DisableChangeFlags(Enums.ChangeStates.RegionMatrixDirty);
                }
                return mMatrix;
            }
        }


        public virtual Matrix GlobalMatrix
        {
            get
            {
                if (mGlobalMatrix.IsNullOrEmpty() || (mChangeStates & ChangeStates.GlobalMatrixDirty) != 0)
                {
                    UpdateGlobal();
                    Matrix tmat = Matrix.CreateTranslation(mGlobalTranslation);
                    Matrix smat = Matrix.CreateScaling(mGlobalScale);
                    Matrix rmat = new Matrix(mGlobalRotation);
                    mGlobalMatrix = smat * rmat * tmat;
                    //DisableChangeFlags(Enums.ChangeStates.GlobalMatrixDirty);
                }

                return mGlobalMatrix;
            }
        }
#endif

        /// <summary>
        /// This is a translation amount to apply to the current world view position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="skipBoundsCheck" ></param>
        public void Translate(double deltaX, double deltaY, double deltaZ, bool skipBoundsCheck)
        {
            // TODO: Rather than just have option to restrict a viewpoint via it's Region's bounds
            // we should be able to restrict Viewpoints here (or also via an Entity script to be called
            // upon Translate...?)  
            // The idea is that we can create say a security cam viewpoint that can rotate, but not translate
            // Or restrict a Viewpoint with a bounding volume (sphere or box) for editing interior
            // celledregion of a vehicle.  
            Vector3d delta;
            delta.x = deltaX;
            delta.y = deltaY;
            delta.z = deltaZ;
            //System.Diagnostics.Debug.WriteLine(delta.ToString());
            Translation = Translation + delta;
        }


        /// <summary>
        /// This is a translation amount to apply to the current camera position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void deltaZ(double deltaX, double deltaY, double deltaZ)
        {
            Translate(deltaX, deltaY, deltaZ, false);
        }

        public void Translate(Vector3d delta)
        {
            Translate(delta.x, delta.y, delta.z, false);
        }

        public void Translate(Vector3d delta, bool skipBoundsCheck)
        {
            Translate(delta.x, delta.y, delta.z, skipBoundsCheck);
        }

        public void SetRotation(double yawDegrees, double pitchDegrees, double rollDegrees)
        {
            Rotation = new Quaternion(yawDegrees * 57.2958d, //Utilities.MathHelper.DEGREES_TO_RADIANS, 
                                        pitchDegrees * 57.2958d, //Utilities.MathHelper.DEGREES_TO_RADIANS,
                                        rollDegrees * 57.2958);//Utilities.MathHelper.DEGREES_TO_RADIANS);
        }

#if USE_MEMORY_T == false
        #region UPDATES
        private void UpdateGlobal()
        {
            /*
            // there is no parent                
            if (mParents == null || mParents[0] == null)
            {
                if (this is Portals.Zone)
                    mGlobalTranslation = ((Portals.Zone)this).ZoneTranslation;
                else
                    mGlobalTranslation = mTranslation;

                mGlobalRotation = mRotation;
                mGlobalScale = mScale;
                return;
            }

            Transform mParent = (Transform)mParents[0];

            // Update orientation             
            Quaternion parentOrientation = mParent.GlobalRotation;
            if (mInheritRotation)
            {
                // Combine orientation with that of parent     
                if (AttachedToBoneID >= 0)
                {
                    // TODO: no way to just get the goddamn rotation... grr...
                    //((Keystone.Entities.BonedEntity)_parents[0])._actor._actor.getbone. GetBoneMatrix(AttachedToBoneID, true);
                    //mGlobalRotation = parentOrientation * boneRotation * _rotation;
                }
                else
                    mGlobalRotation = parentOrientation * mRotation;
            }
            else
            {
                // No inheritence                 
                mGlobalRotation = mRotation;
            }
            // Update scale             
            Vector3d parentScale = mParent.GlobalScale;
            if (mInheritScale)
            {
                // Scale own position by parent scale, NB just combine                 
                // as equivalent axes, no shearing                 
                mGlobalScale = parentScale * mScale;
            }
            else
            {
                // No inheritence                 
                mGlobalScale = mScale;
            }
            if (mInheritScale)
            {
                // Change position vector based on parent's orientation & scale             
                if (this is Portals.Zone)
                    mGlobalTranslation = parentOrientation * (parentScale * ((Portals.Zone)this).ZoneTranslation);
                else
                    mGlobalTranslation = parentOrientation * (parentScale * mTranslation);
            }
            else
            {
                // Change position vector based on parent's orientation & scale             
                if (this is Portals.Zone)
                    mGlobalTranslation = ((Portals.Zone)this).ZoneTranslation;
                else
                    mGlobalTranslation = mTranslation;
            }


            // Add altered position vector to parents 
            mGlobalTranslation += mParent.GlobalTranslation;
            */
        }

        private void UpdateRegional()
        {
            /*
            // DisableChangeFlags(
            //     Enums.ChangeStates.Translated |
            //     Enums.ChangeStates.Scaled |
            //     Enums.ChangeStates.Rotated);

                if (mParents == null || mParents[0] == null || this is Portals.Region)
                {
                    // Region node's derived matrix is always identity.  
                    // _rotation, _translation and _scale are all guaranteed to be default starting values.
                    mDerivedRotation = mRotation;
                    mDerivedTranslation = mTranslation;
                    mDerivedScale = mScale;
                    return;
                }


                Transform parentTransform = (Transform)mParents[0];

                // Update orientation             
                Quaternion parentOrientation = parentTransform.DerivedRotation;
                if (mInheritRotation)
                {
                    if (AttachedToBoneID >= 0)
                    {
                        // TODO: no way to just get the goddamn rotation... grr...
                        //((Keystone.Entities.BonedEntity)_parents[0])._actor._actor.getbone. GetBoneMatrix(AttachedToBoneID, true);
                        //mDerivedRotation = parentOrientation * boneRotation * _rotation;
                        throw new NotImplementedException();
                    }
                    else
                    {
                        // Combine orientation with that of parent                 
                        mDerivedRotation = parentOrientation * mRotation;
                    }
                }
                else
                {
                    // No rotation inheritence                 
                    mDerivedRotation = mRotation;
                }
                // Update scale             
                Vector3d parentScale = parentTransform.DerivedScale;
                if (mInheritScale)
                {
                    // Scale own position by parent scale, NB just combine                 
                    // as equivalent axes, no shearing                 
                    mDerivedScale = parentScale * mScale;
                }
                else
                {
                    // No inheritence                 
                    mDerivedScale = mScale;
                }

                if (mInheritScale)
                    // Change position vector based on parent's orientation & scale                
                    mDerivedTranslation = parentOrientation * (parentScale * mTranslation);
                // reverse the parameters to the * operator so second overload op version is used 
                //mDerivedTranslation = (parentScale * mTranslation) * parentOrientation;
                else
                    mDerivedTranslation = mTranslation;

                // Add altered position vector to parents             
                mDerivedTranslation += parentTransform.DerivedTranslation;


                if (mTranslation.x == double.NaN)
                    System.Diagnostics.Debug.WriteLine("Transform.Update() - NaN");
            */
        }


        #endregion  // UPDATES
#endif

        #region Disposable members
#if USE_MEMORY_T
		protected bool mIsDisposed;
        public virtual void Dispose()
        {
            DisposeManagedResources();
		}

        public virtual void DisposeManagedResources()
        {
           if (!mIsDisposed)
           {
                ComponentStore<Transform_Struct> store = EntryClass.mCStoreCol.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
				store.CheckIn(mMemStore_Transform);
            	//SpanIndex ;
            	//Console.WriteLine ("Transform.cs.DisposeManagedResources() - Checked In Transform_Struct");
			    mIsDisposed = true;
		   }
        }
#endif

        #endregion
    }
////////////////////////////////////////////////////////////////////////////////////////////////
// END NODES
////////////////////////////////////////////////////////////////////////////////////////////////	

	
	
////////////////////////////////////////////////////////////////////////////////////////////////
#region Policy Rules
////////////////////////////////////////////////////////////////////////////////////////////////	
			
		// POLICIES AND RULES 
		// todo: the ai captain needs a "mission" or "objectives" for each mission
		// ordinance Rules
		// ROE example: see HelloConditions.cs
			
		// http://www.gamasutra.com/view/news/198377/Video_Valves_system_for_creating_AIdriven_dynamic_dialog.php   <- now on Youtube @ https://www.youtube.com/watch?v=tAbBID3N64A
		// http://www.valvesoftware.com/publications/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf
		// NOTE: in Valve's Zombie game, for the npc voice logic, they share
		//       all of this knowledge in a single knowledge base rather than allowing
		//       each to have it's own in a fragmented way and it makes running through
		//       them sequentially to find voice responses that match a search much faster and easier.
		//       Valve's Left 4 Dead voice logic is very much a flat database but generated by flattening
		//		 a scenegraph style directed acyclic graph (DAG))	
		
			/* 
			https://stackoverflow.com/questions/31879609/flattening-a-graph
			https://deephaven.io/core/docs/conceptual/dag/
			https://github.com/madelson/Traverse
			//	http://www.gamasutra.com/blogs/GuyHasson/20120706/173705/Story_Design_Tips_Better_NPC_Interaction_Part_II.php
		//  -> Flattening a DAG ->   https://medium.com/@chipzt/directed-acyclic-graphs-dags-8d479ed14967
		
			"Flattening" a Directed Acyclic Graph (DAG) in C# is typically achieved using a topological sort algorithm. 
			This process results in a linear ordering of all nodes such that for every directed edge from node A to 
			node B, A appears before B in the list. This linear sequence is the "flattened" representation of the DAG, 
			often used for task scheduling and dependency resolution. 
			
			Implementation Concepts in C#
			
			To flatten a DAG in C#, you would generally follow these steps:
			Represent the DAG: Define a class for the nodes and a way to store the edges (e.g., an adjacency list or a 
			dictionary where keys are nodes and values are lists of their children).
			Implement Topological Sort: Use an algorithm like Kahn's algorithm or a depth-first search (DFS) based 
			approach to generate a topological ordering.

			Iterate and Collect: The result of the topological sort is your flattened list of nodes. 

			Example C# Code Snippet (Conceptual)
			A common approach for topological sort uses DFS: 


		public class Node<T>
		{
			public T Value { get; set; }
			public List<Node<T>> Dependencies { get; set; } = new List<Node<T>>();
		}

		public static List<Node<T>> TopologicalSort<T>(List<Node<T>> nodes)
		{
			var sortedList = new List<Node<T>>();
			var visited = new HashSet<Node<T>>();
			var recursionStack = new HashSet<Node<T>>();

			foreach (var node in nodes)
			{
				if (!visited.Contains(node))
				{
					SortUtil(node, visited, recursionStack, sortedList);
				}
			}
			// Result needs to be reversed if using DFS post-order traversal
			sortedList.Reverse(); 
			return sortedList;
		}

		private static void SortUtil<T>(Node<T> node, HashSet<Node<T>> visited, HashSet<Node<T>> recursionStack, List<Node<T>> sortedList)
		{
			visited.Add(node);
			recursionStack.Add(node); // Used for cycle detection (crucial for DAG validation)

			foreach (var dependency in node.Dependencies)
			{
				if (!visited.Contains(dependency))
				{
					SortUtil(dependency, visited, recursionStack, sortedList);
				}
				else if (recursionStack.Contains(dependency))
				{
					// Cycle detected - the graph is NOT a DAG and cannot be flattened this way
					throw new Exception("Graph contains a cycle!"); 
				}
			}

			recursionStack.Remove(node);
			sortedList.Add(node);
		}

		Note: A true "flattening" into a simple linear list is only possible if the graph is, in fact, a DAG (meaning it has no cycles). 
		If a cycle is present, the process cannot terminate in a finite order, and an exception should be thrown. 

		For a complete, working example or to use a library that handles graph operations, you might explore graph libraries for C# or
		refer to examples on platforms like Stack Overflow.  https://stackoverflow.com/questions/31879609/flattening-a-graph
		*/
			
			
			
		//	- The trick is how the KEY for each flattened path is created and then used when building the query string!!!		
		//	http://www.gamasutra.com/blogs/GuyHasson/20120706/173705/Story_Design_Tips_Better_NPC_Interaction_Part_II.php
		//  -> Flattening a DAG ->   https://medium.com/@chipzt/directed-acyclic-graphs-dags-8d479ed14967
			
		//			- sort rules alphabetically.  Why?
		//				- well this way when running the comparisons of the QUERIES against the CONDITIONS of each rule,
		//			as we iterate through each QUERY "key" we don't have to re-start an iteration at the beginning of every CONDITION "key" 
		//			because we know they are in same alphabetical order as the QUERIES collection.  For instance:
		//			QUERY: A:100, B:50, C:true, F:false
		//			RULE1:
		//          	CONDITIONS: A:<=500 && A: >=0 
		//              CONDITIONS: C:<=True && >=True
		//				- in the above, we start to iterate through the 4 query tuples and for each naivly we iterate each CONDITION
		//                but instead, when we find a matching condition, we don't need to start over.  We can resume because we know that
		//                the CONDITIONS are sorted the same way so when testing QUERY part B, we can resume iteration of CONDITION and next
		//				  CONDITION will be C: so we know B doesn't exist (else the iteration cursor would have been moved back to beginning).		
		//
		//          TODO: currently our normal propertybag stores it's data as DefaultValue and does not actually hook back to a
		//			      collection of objects.  It should actually store to same object store so that the data can also be read
		//                directly through the object store and not through the entity.  Recall that originally, the point of using the PropertySpec's
		//                was to get propertybag GUI rendering for free via propertygrid control.
		//
		//			- hash buckets for different regions and/or other basic buckets similarly to what we do when we cull
		//			- store pointers to the value we want to compare rather than have to query that game data
		//			- sort by decreasing # of criteria (as we do with TileMap auto-tile rules)
		//			- represent every comparision as a >= x >= b  
		//				eg.   return (10 >= ptrCharacterXHitpoints && ptrCharacterXHitpoints >= 100);    
		//
		//		 So in a way, what we want there is a Blackboard class that can
		//       manage all that for us, and then when we first initialize a behavior
		//       on an Entity, it will grab a blackboard blob from the allocator and
		//       assign it to the Enity.Knowledge
		//       - and since the dialogue tree structure is essentially a flattened DAG (like a scenegraph)
		//		 which seems to take on a Rules Engine like functionality because it becomes serial
		//       test and not a branching test.
		//       - thus, each "record" has an owner and can be referenced and read/written to
		//       from the UserDataStore.  There is a question of whether this data should remain in 
		//       DB form.. perhaps cached for recent access.  Well, i think it must be cached or else
		//       way too slow for the type of use we do.  Do we CheckIn/CheckOut data blobs?  We could do some
		//       really fast computations I think and threaded, on an in memory "blackboard" where each blackboard 
		//       can be defined and hold all of same record types (eg all stars, all worlds, all npcs) so that
		//       manipulation of their data is... well... its all very functional style and not OO.
		//		 - THE CACHE COHERENCY BECOMES EXCELLENT.
		//		   - perhaps each derived blackboard itself becomes a data manipulator that knows how to read/write it's data
		//	       and then the sqlite or whatever storage occurs as generic using array of field definitions
		//			- Being able to define custom blackboards is nice because we now have fixed size fields
		//
		//		 - is the UserDataStore a global like Pager and Repository?
		//		- maybe each Blackboard gets instantiated EXE side and so we get StarData : UserData 
		//      that gets used for all stars and which we can write custom data manipulation against
		//		- we could even read/write to it like we do with Packets... and perhaps even use unsafe code for even greater performance
		// (See E:\dev\_projects\_XNA\Mercury Particle Engine\ProjectMercury.WindowsEmitters\Emitter.cs.Update() method)
		// but one thing it does which i think defeats the purpose somewhat perhaps is it creates a fixed pointer to the particle array rather than allocating it as pointer from start.  having to "fix" it seems like enough overhead to nullify any performance advantages
		
		//  - Production productID and Consumers can be stored here as well.  Do we still want to use scripts for these entities? or
		//    would scripts assigned to each data store type be more efficient?
		//  - for economic simulation this could be very fast
		//	- AI simulation may be more needed case for a single player 1.0 game release
		//		- blackboard data can store Area_Of_Interest data generated from other pre- calculations 
		//  - for NPC simulation this can be very fast too when running out behavior tree against this data
		//    and eventually we probably stop simulating Entity AI in Entity.Update() and move it to an Update() 
		//    of simulation that will iterate through npcs by iterating through the blackboard data (limiting iterating 
		//    to X count that fit into an alotted timeslice using threading as available and as needed)
		
		//		- IN OTHER WORDS, by iterating through the array of UserData to perform entity updates, can we properly update
		//    these variables with appropriate functions and have the update reflected in the Entity itself?  For example, lets say
		//    we have 50 entities that are doing wander steering behavior... can we run a singular script that operates on blackboard user data
		//    to update all 50 of those entities?  rather than 50 calls to entity.update() and 50 script calls.
		//          - if the scripts each entity uses can be one of the ways we sort entities when updating their data, then we can easily
		//          update all entities using a particular script.... similar to how we do renders of sorted entities
		//			- if our scene update() loop added entities to be updated in sorted buckets... but for now this is jsut brainstorming idea, since it could slow us down
		//  Feb.6.2026 -> Regarding the above question which is Data-Oriented processing model, YES WE CAN.  This is what i have been in fact implementing for last few months
		//                
		//
		
		// TODO: google cache coherency as it relates to flat databases 
		//			- and .net c#
		// TODO: isn't BehaviorContext.Knowledge already associated with Entity?  And shouldn't this data replace Entity CustomProperties? and Rename var from Knowledge to Entity.CustomData
                                   //       and is now stored in sqlite where our scene representation which uses xml is seperate from the entity custom data which is db stored.  Our EntityAPI for
                                   //       getting custom data can now also use methods with type safety.  Further we no longer have to care about custom data being serialized to xml and perhaps this
                                   //       speeds up our ability to save scene when we are editing maps as well as saving game state
        							// TODO: however, will this type of CustomProperties now no longer be easily editable in a PropertyGrid and if not, is that ok?
        							//      we're using custom html interfaces now anyway right?
        							//      we must start with _just_ custom properties for now but actually just RenderingContext 
        							// TODO: also what about shaders?  right now those use custom properties for shader params/vars and should not be stored in a db!
        							// TODO: actually volume, surfacearea,cost,weight for all celestial bodes is already being used as custom properties!
        							//       So question is, how do we connect those to a datastore?
        							//		 - well just as we use GetProperties() SetProperties() where a single reader/writer of xml store is operating
        							//       we can do same for UserData.   We can convert to GetProperties and SetProperties() and we can also
        							//       use other methods of iterating thru the list of custom data. For now, let's just focus on Viewpoint for Chase
        					
			// is there a way to track the data for an individual Entity via an Index into array of records and to have this record
			// index maintained during lifetime? indices can be checked in / checked out

			// locally, we dont really need to use entityID as part of a record key either, locally we can use just an Index
			// and perhaps a lookup value... but i think in short term, we should continue to focus on just Viewpoint and Chase cam
			// and if that goes well, Stars and see about how it works with LoadTVResource() and restoring DB via a LoadCustomData()
	
	// Directives
	// Treaties
	// RulesOfEngagement
	// Orders
	// Objectives
	// OrdianceUsePolicy
	// EnergyAllocationPolicy
	
	
	// combat specific assessmemnts
	// ----
	// Readyness, CapacityToAct;
	// CapabilityAssessment;
	// OutcomeAssessment;
	public class ExecutiveDirectives
	{
		// Keystone.Simulation.Missions.Mission
		// Keystone.Simulaton.Missions.MissionData
		// Keystone.Simulation.Missions.Objective

	//	public Mission Mission;
	//	public Orders Orders;

		// Game01.GameObjects.ExecutiveDirectives.RulesOfEngagement
		public struct RulesOfEngagement
		{
			public bool FireOnFreighters; // usually always false
			public bool RetreatRatherThanFightIfPossible;
				//      - never fire first except during wartime
				//		- diplomacy first unless state of war
				//      - never fire on disabled ships or otherwise  non-threats
				//		- pre-emptive policy
				//		- disable priority
				//			- shields
				//			- weapons
				//			- engines
				//		- proportiality / proportional response
				//		- nuclear weapons only to deter opposing nuclear threat only (some ships may have a mission of always staying hidden and running silent and nuclear deterences in case of an attack on homeworld and homeworld is destroyed, the retaliatory strike option will still exist to carry out its mission
				//		- 

		}
	}


	
	public class Policy
	{
		// eg: A Policy contains a list of Queries that represent testing for a 
		//     related series of conditions.
		//     For instance, a subset of the Rules of Engagement (RoE) policy says "Do Not Fire On Friendlies" 
		//     needs to check for the following:
		//     - Is the target a member of "Membership_Earth_Alliance"
		//     - Is the target a member of "Colonial_Expeditionary_Fleet"
		//     - Has the target fired upon us or an ally, and thus, is in breach of this policy itself?
		
		// Let's say the question is, Can this Target vessel be fired upon?  
		// we want to build this query up as a type of Policy for When can a vessel be fired upon?
		
		private List<Query> mQueries;
		private string mErrorReason;
		
		
		public Policy()
		{
			mQueries = new List<Query>();
		}
		
		public Query[] Queries {get {if (mQueries == null) return null; return mQueries.ToArray();}}
		
		
		public void Add(Query q)
		{
			if (mQueries == null) mQueries = new List<Query>();
			mQueries.Add(q);
		}
		
		public bool Execute ()
		{
			if (mQueries == null || mQueries.Count == 0) return true;
			
			for (int i = 0; i < mQueries.Count; i++)
			{
				UserDataStore context = mQueries[i].Context;
				if (!mQueries[i].Execute()) return false;
			}
			
			return true;
		}
	}
	
	
	public class Query 
	{
		private UserDataStore mContext;
		private Rule[] mRules;
		
		// see line 2535 for useage 
		public Query(UserDataStore uds)
		{
			if (uds == null) throw new ArgumentNullException("Query.ctor() - UserDataStore parameter cannot be null.");
			mContext = uds;
		}
		
		public UserDataStore Context {get {return mContext;}}
		
		public Rule[] Rules { get {return mRules;}}
		
		public void Add(Rule r)
		{
			// ArrayAppend() is using Keystone namespace but actually is in KeyStandardLibrary
			// Keystone.Extensions.ArrayExtensions.	
			mRules = Utils.ArrayAppend<Rule>(mRules, r);
		}
		
		public bool Execute ()
		{
			if (mRules == null || mRules.Length == 0) return true;
			
			//Console.WriteLine("Executing rules");
			for (int i = 0; i < mRules.Length; i++)
				if (!mRules[i].Evaluate(mContext)) return false;
			
			return true;
		}
	}

	
	/// <summary>
	/// Rules should be sorted from highest number of Conditions to lowest so that we always test against highest number first so we can potentially early-exit
	/// <summary>
	public class Rule
	{
		private string mConcept;
		private string mDescription;
		private Condition[] mConditions;
		//public Response Response;
		//public Remember Remember;
		//public Trigger Trigger;
		public string ErrorReason;
		
		public Rule (string concept, string description)
		{
			mConcept = concept;
			mDescription = description;
		}
		
		public void Add(Condition c)
		{
			// ArrayAppend() is using Keystone namespace but actually is in KeyStandardLibrary
			// Keystone.Extensions.ArrayExtensions.	
			mConditions = Utils.ArrayAppend<Condition>(mConditions, c);
		}

		public void Remove (Condition c)
		{
			//mConditions = Utils.ArrayRemove<Condition>(mConditions, c);
		}
		
		public bool Evaluate(UserDataStore context)
		{
			if (mConditions == null || mConditions.Length == 0) return true;
			
			//Console.WriteLine("Condition.Evaluate() - Conditions Count == " + mConditions.Length.ToString());
			for (int i = 0; i < mConditions.Length; i++)
			{
				string left = null;
				string right = null;
				System.Diagnostics.Debug.Assert(mConditions[i] != null, "Condition.Evaluate() - Condition is NULL");
				//Console.WriteLine("Condition.Evaluate() - Condition Has Delegate == " + mConditions[i].LeftOperandIsDelegate.ToString());
				
				if (mConditions[i].LeftOperandIsDelegate)
				{
					// the LEFT operand delegate to invoke.  The RIGHT operand is what we want to compare it against 
					bool result = mConditions[i].OperandLeftDelegate(mConditions[i].DelegateArgs);
					left = result.ToString().ToUpper();
					right =  mConditions[i].OperandRight.ToUpper(); // NOTE: We do not need anything more than a "true" or "false" for the rightOperand.  We DO NOT NEED A DICTIONARY KEY BECAUSE WE COULD SOLVE FOR THAT WITHIN THE DELEGATE 
					System.Diagnostics.Debug.Assert(right == "FALSE" || right == "TRUE", "Evaluate() - When using a Delegate, a CONDITION must always evaluate against TRUE or FALSE.");
					//Console.WriteLine("Condition.Evaluate() - LEFT IS A DELEGATE --> LEFT == " + left + " RIGHT == " + right);
				}
				else
				{	
					// left is the KVP to look up.  right is what we want to compare it against 
					System.Diagnostics.Debug.Assert (context != null, "Context is not null.");
					left = context[mConditions[i].LeftEntityKey].GetString(mConditions[i].OperandLeft);
					right = context[mConditions[i].RightEntityKey].GetString(mConditions[i].OperandRight);  
					//Console.WriteLine("Condition.Evaluate() - LEFT ENTITY '" + mConditions[i].LeftEntityKey + "' KEY == " + left + " RIGHT ENTITY '" + mConditions[i].RightEntityKey + "' KEY == " + right);
				}
				
				switch (mConditions[i].mEvalType)
				{
					case Condition.EVAL_TYPE.EQUALS:
						//Console.WriteLine("Condition.Evaluate() - EQUALS TEST");
						if (left != right) return false; // todo: ErrorReason = 
						break;

					case Condition.EVAL_TYPE.NOT_EQUALS:
						//Console.WriteLine("Condition.Evaluate() - NOT EQUALS TEST");
						if (left == right) return false; // todo: ErrorReason = 
						break;

					case Condition.EVAL_TYPE.LESS_THAN:
						if (MicroEx.Evaluate(left + " >= " + right)) return false; // todo: ErrorReason = 
						break;
						//return OperandLeft < OperandRight;

					case Condition.EVAL_TYPE.GREATER_THAN:
						if (MicroEx.Evaluate(left + " <= " + right)) return false; // todo: ErrorReason = 
						break;
						//return OperandLeft > OperandRight;

					default:
						throw new ArgumentOutOfRangeException("Condition.Evaluate() - Unexpected evalType '" + mConditions[i].mEvalType.ToString() + "'");
				}
			}
			return true;
		}
	}
	
	
	public class Condition
	{
		public enum EVAL_TYPE : int
		{
			EQUALS = 0,
			NOT_EQUALS = 1,
			LESS_THAN = 2,
			GREATER_THAN = 3
		}
		
		public string Name;
		public string Description;
		
		public bool LeftOperandIsDelegate;
		// there's generally no reason for BOTH the left and right operands to be a delegate.  
		// The left will be our delegate and the right will be the operand we want to compare the result of the delegate to
		public Func<object[], bool> OperandLeftDelegate; 
		public object[] DelegateArgs;
		
		// The 'key' into our UserDataStore context that will return the 'value' we want for the left operand
		public string OperandLeft;
		// The 'key' into our UserDataStore context that will return the 'value' we want for the right operand
		public string OperandRight;
		public EVAL_TYPE mEvalType;
		
		public string LeftEntityKey;
		public string RightEntityKey;
		
		
		public Condition (string name, string description, string leftEntityKey, string rightEntityKey, EVAL_TYPE eval, string operandLeft, string operandRight)
		{
			Name = name;
			Description = description;
			OperandLeft = operandLeft;
			OperandRight = operandRight;
			mEvalType = eval;
			LeftEntityKey = leftEntityKey; // our UserDataStore holds a Dictionary<string, UserData> with the string 'key' being the EntityID the UserData belongs too. 
			RightEntityKey = rightEntityKey;
			LeftOperandIsDelegate = false;
		}
		
		public Condition (string name, string description, string leftEntityKey, string rightEntityKey, EVAL_TYPE eval, Func<object[], bool> operandLeft, string operandRight, object[] delegateArgs)
		{
			Name = name;
			Description = description;

			DelegateArgs = delegateArgs;
			OperandLeftDelegate = operandLeft;
			OperandRight = operandRight;
			mEvalType = eval;
			LeftEntityKey = leftEntityKey;     // our UserDataStore holds a Dictionary<string, UserData> with the string 'key' being the EntityID the UserData belongs too. 
			RightEntityKey = rightEntityKey;   // 
			LeftOperandIsDelegate = true;
			//Console.WriteLine("Condition.ctor() - left operand is delegate");
		}
	}
	
	
	
	public class Statistics
	{
		private UserDataStore mContext;
		private UserData mBlackboardData;
		Dictionary<string, int> mCounters = new Dictionary<string, int>();
		
		// https://redis.io/docs/latest/develop/get-started/data-store/
		// HSET bike:1 model Deimos brand Ergonom type 'Enduro bikes' price 4972
		
		// so this is a key that would be made up of 4 keyvalue pairs.  Each kvp is delimited by colons
		// each key and value is delimited by a space.
		
		// bike 1:      model Deimos:brand Ergonom:type 'Enduro bikes':price 4972
		
		
		
		
		// hmm... the first is a QUANTITY also though... im not sure how this works
		// > HGET bike:1 model
		// "Deimos"
		
		// IStatistics stats = (IStatistics)EntityNode.UserData.Get(this.ID, "stats");
		
		// string action = "defeated" ;
		// string key = action + "," + "Droid_123";
		
		// Increment (this.ID, key)
		
		// how would you sum totals... sure we can
		// parse each string for the "defeated" text, but that seems slow and annoying...
		// Parsing 768 KVPs for each Droid seems an extremly slow operation...  
		
		
		
		// EntityID->stats-> "kvp...."
		// attacked 
		// defeated Droid123 3:Droid345 1:Droid989 1  <-- Dictionary<string, Dictionary<string, int>> 
		// defeated_by
		// attacked_by
		// faction memberships
		// crew members
		
		/*
		For a dogfighting space sim leaderboard, track Win/Loss Ratio, Kill/Death Ratio (KDR), and Score per Minute (SPM) as primary performance indicators. Include specialized metrics like Total Damage Dealt, Accuracy Percentage, Objective Points, and Target Lock Time to reward skilled flying, high-damage loadouts, and objective-oriented gameplay over just raw kills. 
Reddit
Reddit
 +2
Key Leaderboard Categories
Core Combat Stats:
KDR: Measures pure lethality.
Win/Loss Ratio: Highlights team players who focus on victory.
Score per Minute (SPM): Measures efficiency and consistent engagement.
Skill-Based Stats:
Accuracy %: Shots landed vs. fired.
Total Damage Dealt: Rewards damage over just last-hitting for kills.
Average Damage per Kill: Distinguishes snipers from finishers.
Tactical Stats:
Target Lock Time: Measures fast target acquisition.
Objective Points: Rewards time spent on objectives (e.g., node capturing, flag carrying).
Most Dangerous Enemy: Tracks against whom the player has the highest win rate.
Ship Performance (Contextual):
Time Spent in Speed Class (SCM): Measures maneuvering skill.
Missile Efficiency: Hits vs. launched missiles. 
Reddit
Reddit
 +2
Why These Matter
According to a discussion on Reddit, Win/Loss and SPM are often better indicators of true skill and teamwork than raw kills, especially in objective-focused gameplay. For dogfights, damage dealing and accuracy are often more indicative of pilot skill, as discussed in Reddit. 
	*/
		
		// A) We want to accomplish two things
		//    1) We want to track for EACH droid, how many of every OTHER droid, we've defeated and with what weapon and what operator at the Station
		//    2) We need fast access to these Stats and that these stats should remain in memory after a DROID is defeated so that it can be respawned
		//       and continue to create stats.
		//    3) We DO NOT NEED TO CREATE A LOG of ALL EVENTS FOR THIS....  WE MAY EVENTUALLY, BUT PURPOSE OF THIS IS ONLY TO CREATE COMBAT STATS TRACKING
		
		// EntityStats[]  <-- same index as the EntityArrayIndex
		//                <-- uses Memory<T> underneath and a UserDataStore
		//                <-- 
		//
		// 
		// 
		//  
		
		
		
		public Statistics(string key)
		{
			mBlackboardData = EntryClass.mUserDataStore.CheckOut(key);
			
			// lets say a Ship is detected and we want to check if it has fired upon any friendly 
			// recently.  Friendly means any Ship that is of the same "faction."
			// 
			// List<Targets> = Statistics[targetEntityArrayIndex].GetList("targets");
			// for (int i = 0; i < targets.Length; i++)
			// {
			//    // TODO: the call to AttackedBy() should be a record that contains the DATE, TIME, LOCATION and other details of that attack such as what weapon was used and what was hit or targeted.
			//             and whether a HIT or MISS was recorded.
			//    string[] attackedByEntitiesArrayIndices = targets[i].AttackedBy();
			//	  for (int j = 0; j < attackedByEntitiesArrayIndices.Length; j++)
			//        if (Boids[attackedByEntitiesArrayIndices[j]].GetMembership("Red"))
			//			  return true;
			// }
		}
		
		public UserData BlackboardData {get { return mBlackboardData; } }
		
		
		// https://stackoverflow.com/questions/74811627/whats-the-best-way-to-store-a-finite-number-of-stats
		// http://blog.ndepend.com/faster-dictionary-in-c/
		// https://codesignal.com/learn/courses/revision-of-csharp-dictionaries-and-their-use-in-practice/lessons/data-aggregation-using-dictionaries-in-csharp
		// https://codesignal.com/learn/courses/hashing-dictionaries-and-collections-in-csharp/lessons/advanced-dictionary-operations-in-csharp
		public void Increment(string entityKey, KeyValuePair<string, string>[] keys)
		{
			if (keys == null || keys.Length == 0) return;
			
			int count = keys.Length;
			
			string VP_DELIM = " ";
			string KVP_DELIM = ":";
			
			string combinedKey = null;
			for (int i = 0; i < count; i++)
			{
				if (!string.IsNullOrEmpty(combinedKey)) combinedKey += KVP_DELIM;
				
				combinedKey += keys[i].Key + VP_DELIM + keys[i].Value;
			}
			
			Increment(combinedKey);
		}
		
		// todo: this should probably go in UserData with the above overload being the only one
		//       that exists because it has the responsibility of combinging the kvps into one key
		public void Increment(string key)
		{
			if (mCounters.ContainsKey(key)) 
			{
                mCounters[key]++;
			}
			else // it doesn't currently exist, so we add it
			{
				mCounters[key] = 1;
			}
		}
	}
////////////////////////////////////////////////////////////////////////////////////////////////
#endregion   //Rules, Queries, Policies, Conditions
////////////////////////////////////////////////////////////////////////////////////////////////

	
	
////////////////////////////////////////////////////////////////////////////////////////////////
#region PRODUCTION AND CONSUMPTION //  NOTE: These all belong in Game01.dll
	////////////////////////////////////////////////////////////////////////////////////////////////
	public enum PRODUCTS : int
	{
		None = 0,

		ElectricalPower,

		// Fuels


		// Emissions and Signatures
		OpticalReflection,    // aka: VisibleLightReflection,  camoflauge can reduce this "reflection" 
		MicrowaveReflection,
		MicrowaveEmission,
		
		// Damage Types
		MicrowaveDamage = 1024,
		FireDamage,
		PlasmaFireDamage,
		VaccumDamage,
		RadiationDamage,
		PressureDamage,   // eg too deep underwater or within a Gas Giant's atmosphere

		CommandBoost = 2048,
		MoraleBoost,   // like all modifiers, this too can actually be either negative or positive
		Fatigue,
		
		
		// Skill Modifiers
		TacticalOperationsSkillModifier = 4096,
		TargetingSkillModifier,

		Haggling
	}


	
	public enum SKILLS : int
	{
		HelmOperations,
		TacticalOperations,
		Piloting,
		Targeting,
		Engineering,
		SensorOperations,

		Command,
		Morale
	}

	public enum PRODUCT_DISTRIBUTION_TYPE
	{
		SingleItem = 0,
		List,
		Region,
		Zone,
		BoundingSphere,
		BoundingBox,
		BoundingCone,
		PlanedHull
	}
	
	
	
	// TODO: Im not sure I need seperate Consumption and PowerConsumer 
	//                            and    Production  and PowerProducer
	//       Mostly I think, its so we can have a general struct for ALL 
	//       sorts of Production... not just electrical power.
	//       If we need production of Morale, Fatigue, Heat, Gamma Radiation, etc
	//       then all of these can use the same struct... 
		
		
	/*
	Electromigration: The gradual movement of metal atoms in a semiconductor caused by electric current, leading to open or short circuits.
	Thermal Cycling/Fatigue: Damage caused by expansion and contraction due to heat, leading to cracked solder joints.
	Capacitor Degradation: Electrolytic capacitors "drying out" or leaking, a very common failure mode.
	Corrosion: Oxidation of connectors and traces caused by moisture and air.
	Oxidation: The loss of electrons in metal components, causing degradation.
	Planned Obsolescence: A strategic design choice for a product to become unusable within a set time. 

	How Electronics Wear Out:
	Mechanical Wear: Parts that physically move, such as fans, hard drives, or button contacts, degrade due to friction.
	Chemical Degradation: Capacitors can lose their charge capacity or break down, and battery performance fades over time.
	Thermal Stress: Excessive heat can cause brittle fractures, warping of boards, or damage to components. 

	Common Indicators of Wear:
	Performance Reduction: Reduced battery life and slower performance.
	Cosmetic Issues: Corrosion of metal parts, loosening of connectors, or damage to components.
	Intermittent Failures: Components failing sporadically before complete breakdown. 

	Relevant Concepts:
	Bathtub Curve: Describes the likelihood of failures over time, which are high at the beginning ("infant mortality") and high at the end ("wearout period").
	Software Rot: The gradual decline of software usability over time without modification. 
	*/

		
	public struct Production
    {
		public int ProductID;
		
		// todo: should i have a frequency or Hz?  Gravitation would be at Physics frequency, but other's should be 1 hz or every 1000 ms
		// production is not serialized to XML because they are created by the scripts in code
		public int ProducerEntityArrayIndex;
		public int ProducerEntityInternalIndex;  
		 // In turn, the Producing Entity needs an index to this Production's index... and in fact, a list of all the Production indices it has registered.
		
		// cached list of Consumers indices from ComponentStore<Consumer> we're sending ProductID to.  
		// For DistributionMode.List or .Item this array does NOT need to be recomputed as it would if
		// a Search() was required to find Consumers within a certain range or volume each frame.
		// Those are typically more used for things like Heat and Radiation from an Explosion prefab
		// and NOT for ElectricalPower production for instance.
		public int[] Consumers;  
		//public int[] DistributionList;  // <-- same as Consumers
		public bool Breaker;
		
		public object Value;  // for Thrust, this would be a 'Vector3d'.  For damage, an 'int', for electrical power a 'double',  for radar echos, UnitValue is a Vector3d position, for SkillModifiers, it can contain a SkillModifier struct.
		public double Store; // infinite = -1, else number of unit's that are available to be consumed by Consumers this Production will be distributed too
		
		public double Duration;
		public double StartTime;
		public int NumUses; // a radiation producing bomb may only produce damage for 3 turns before it dissipates.
		public float CooldownBetweenUses;

		
		// SingleItem, List, Region, Zone, BoundingSphere, BoundingBox, BoundingCone,  PlanedHull
		public PRODUCT_DISTRIBUTION_TYPE DistributionMode; 
		// public Func<Production, string, bool> DistributionFilterFunc; // accepts Production and an EntityID and returns true if the test is passed
		// used when DistributionType is List.  Contains id of entities consuming this product.  
		// No searches (spatial or otherwise) reqt. "power links" and other "links" are good examples of their use.

		// can then use SearchReferenceEntity.BoundingBox, .BoundingSphere, BoundingCone with DistributionMode is a spatial search of some kind.
		public object SearchReferenceEntity;   // since this struct is stored in ComponentStore<Production> we do NOT have to worry about boxing when casting the Memory<T> to a struct
		//[obsolete] public Vector3d Position; // Position of Primitive where this production occurred (eg. explosion, heat signature, etc)?  A radiation bomb or any explosion will need it's Position(Location) set so we can determine the falloff/attenuation of the damage, 
		//[obsolete] public Vector3d Size;        // taken from SearchReferenceEntity.BoundingBox/Sphere
		//[obsolete] public Vector3d Velocity;    // taken from SearchReferenceEntity.Velocity 
		//[obsolete] public Vector3d Acceleration;
		
		
		// PrimitiveTransformBehavior - none (stay the same), expand (positive scaling), contract (negative scaling), translate, rotate
		// PrimitiveStrengthBehavior - fade, intensify
		
		// BehaviorGradient/Attenuation/Falloff
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
		public int ProductID;     // todo: i think the productID can be different than what the consumption handler is passed in. For instance, "heat" can be passed in and result in "damage" to be applied to the consumer
		
		// Consumption here is really PRODUCT CONSUMPTION RESULT struct that gets filled so that
		// other players in the networked game can receive the "results" of 
		// having consumed a product
		// NOTE: There is no reference to which Enity provided the PRODUCTION... is this ok?  If during a Data Oriented Processor
		//       pass multiple Producers contributed to the Conumption result, then we would need an array of Producers...
		//       I think it's best to not worry about this yet.  This is NOT really ConsumptionResult, it's Consumption 
		//       "Value" and "Amount" settings for how much to receive from a Producer, assuming the producer can supply the entire amount.
		public int ConsumerEntityArrayIndex; // in KGB this would be the Entity.ID or GUID of the Consuming Entity
		public int ConsumerInternalIndex; // the index into the Memory<T> of the entity that is consuming a product.
		                                  // In turn, the Consuming Entity needs an index to this Consumption's index... and in fact, a list of all the Consumption indices it has registered.
		
		// public int[] Producers;  // list of Producers indices from ComponentStore<Producer> we're receiving ProductID from.
			
		public bool Breaker;
		
		public object Value;  // for Thrust, this would be a 'Vector3d'.  For damage, an 'int', for electrical power a 'double', 
		public int Amount; // the number of Units to draw from the relevant Producer.  The Simulation EXE will know how to deal with UnitValue based on ProductID.  This could also be "damage." 
		
		//public string TargetID; // NOTE: this does mean that an entity performing consumption can change properties of other nodes and not just itself. Typically though, its only for entities within a single ship hierarchy from Exterior to Interior components
		public PropertyOperation[] Operations; // <-- operation is how to apply the Value and Amount... eg... do we add, subtract, multiply, divide... etc
		
		
		
		// PROPERTIES is probably obsolete because the DataProcessors we assign will know exactly what properties to edit.  
		//   These DataProcessors can be assigned to methods in Scripts or methods in Game01.dll 
		
		
//       public Settings.PropertySpec[] Properties; // todo: what about HelmState and TacticalState properties? Well, "tacticalstate" and "helmstate" are properties in the ship.css and they are serializable over the wire.
		// todo: do we need to be able to send this over the wire with NetBuffer Read and Write?
		// todo: we should probably need to know whether the property values are meant to replace, increment, or decrement the existing value.  "store" is a good example. If we're multithreaded, we might need to lock each node before we apply changes
		//       I could include an array of int[] operation; that is same length and specifiy 0=replace, 1=increment and 2= decrement, 3 = add array element, 4 = remove array element
		// todo: maybe instead of seperate objects like HelmState and NavPoints we just use regular custompropertyspec for each member.  This will make it easier for ConsumptionResult handling without keystone.dll needing to know anything about those custom types.
		// todo: well first, lets just use PropertySpec with intrinsic types.  
		
	}

	
		
	// a producer that if providing continuous electrical power, IF it registers a Production item
	// it must replenish that item at the beginning of every tick correct?
		
	// an item that is "used" and creates a Production like an LightEmission from a laser
	// then, that Production is not continuous and only needs to be enabled/disabled... does 
	// it need a refeence to the Laser?  Or does the "production" have all the data it needs
	// maybe it has a reference to the PowerProducer?
	// What about a LightEmission?  That clearly has no dependancy to the Laser except maybe
	// the distance between the laser and the target it hits.
	//
	
		
		public enum ModificationEffect // equivalent to distributionMode for Production
		{
			Individual,
			List,
			Party,
			Area,
			Region,
			Faction
		}

	
	public struct SkillModifier
	{ 
		public int ProducerEntityArrayIndex; 
		public PRODUCTS Product;  // modifiers are a type of PRODUCT for instance PRODUCT.MoraleBoost
		public SKILLS SkillToTarget;      // the skill that will be affected eg Skill.Morale
		public bool Enabled;
		public int Amount;         // can be negative or positive e.g  +1 Morale
		public int NumUses;
		public float CooldownBetweenUses;
	}
	
	// TODO: an Operator that has for example a targeting skill, (see struct LivingEntity)
	//       will "PRODUCE" a bonus for that crew station every update.  It does not require
	//       an "Update()" function within a script, it only needs the type of PRODUCTION defined
	//       and registered via the Scripting API.  
	public struct Skill
	{
		public SKILLS SkillType;
		public int Level;     			// the level of this skill
		public int BaseValue; 
		public int EffectiveValue;
		
		// These are modifiers that this Skill struct naturally PRODUCES 
		// (as in PRODUCTION and as in, modifiers that are built in to this specific Skill). 
		// These are NOT external modifiers that are added to this Skill!
	//	public SkillModifier[] Modifiers; 
		public SkillModifier[] Production;
		
		
		//public int Value()
		//{
		//	int result = BaseValue;
			
			//if (Modifiers != null)
		//}
		
		/*
		public void AddModifier(int producerIndex, PRODUCTS product, int amount, bool enabled = true, int numUses = -1)
		{
			SkillModifier m;
			m.EntityIndex = producerIndex;
			m.SkillToTarget = SkillType;
			m.Enabled = enabled;
			m.Product = product;
			m.Amount = amount;
			m.NumUses = numUses;
			
			AddModifier(m);
		}
		
		public void AddModifier(SkillModifier modifier)
		{
			int length = 0;
			
			if (Modifiers == null)
				Modifiers = new SkillModifier[1];
			else
			{
				length = Modifiers.Length;
				SkillModifier[] tmp = Modifiers;
				Modifiers.CopyTo(tmp, 0);
				
				Modifiers = new SkillModifier[length];
			}
			
			Modifiers[length] = modifier;
		}
		*/
		
		public void AddProduction(int producerEntityArrayIndex, PRODUCTS product, int amount, bool enabled = true, int numUses = -1)
		{
			SkillModifier m;
			m.ProducerEntityArrayIndex = producerEntityArrayIndex;
			m.SkillToTarget = SkillType;
			m.Enabled = enabled;
			m.Product = product;
			m.Amount = amount;
			m.NumUses = numUses;
			m.CooldownBetweenUses = 1; // 1 second
			AddProduction(m);
		}
		
		public void AddProduction(SkillModifier modifier)
		{
			int length = 0;
			
			if (Production == null)
				Production = new SkillModifier[1];
			else
			{
				length = Production.Length;
				SkillModifier[] tmp = Production;
				Production.CopyTo(tmp, 0);
				
				Production = new SkillModifier[length];
			}
			
			Production[length] = modifier;
		}
	}
	#endregion // PRODUCTION AND CONSUMPTION
	////////////////////////////////////////////////////////////////////////////////////////////////
	
	
	
	
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// STRUCTS AND IENTITYSYSTEMS
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
	// https://boristhebrave.github.io/DeBroglie/
    // https://github.com/BorisTheBrave/DeBroglie
    // LibNoise
    // IEntitySystem proc gen

	// NOTE: game specific structs and enums may only need to exist in Game01.dll or any future GameNN.dll
    public struct NFT_Address // Locator 
    {
        public string NestedGUIDs;
        public string NestedTypeNames; // Vehicle.?12.&111 // aka vehicle.floor.area
        public string NestedNames;     // eg Enterprise.Deck12.Cabin111
    }

    public struct NFT_State
    {
        public bool Loaded;

    }

    public struct NFT_Description
    {
        public string Name;
        public string Description;
        public string Attributes; // kvp traits
        public string Image;      // thumbnail preview

    }

    public struct NFT_Info
    {
        public string GUID;
        public string Owner;
        public string Creator;
        public NFT_Address Location;
        public NFT_Description Description;
        public NFT_State State;
        // public Entity_Taxonomy Taxonomy;     // ProcGen_ItemType enum can be loaded from a modder's file 
    }

	
#region Game01.GameObjects
	
	// similar to Advantages and Disadvantages
	public enum StrengthAndWeaknesses
	{
		PanicsUnderPressure,
		GreatUnderPressure
	}

	public enum ActionType : int
	{
		Target,
		FireAt,
		Ram,
		DeployCounterMeasure,
		DeployMine,
		DeployProbe
	}

	public class UnitedEarthCode
	{

		// SpecialOrder1 (as in SpecialDirective)


	}

	public struct CrewMemberServiceRecord
	{

	}

	
	public struct Membership
	{
		int OrganizationID;  // will lead to organizationType, Name, Description, etc.
		double JoinDate;
		double LeaveDate;

		public long GenerateJoinDate (int age)
		{
			// based on the age of the character, compute a "join date" that seems believable
			// and is consistant also with the age of the Organization or Faction.

			return 0;
		}

		public bool HasMembership(string organizationID, int organizationType)
		{
			return false;
		}
	}

		
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


	/// <summary>
	/// A SensorContact is a PRODUCT that is produced by a Sensor upon receiving
	/// and detectinig an emission of the same PRODUCT of that Sensor.
	/// eg. a Product.OpticalEmission received by a binocular set of "Eyes" sensors
	/// will result in the "production" of a SensorContact of that Entity that emitted the
	/// Product.OpticalEmission and in turn that SensorContact will be consumed by the TacticalStation
	/// or Droid
	/// </summary>
	public struct SensorContact // NOTE: our Droids have one optical sensor... a single binocular system comprised of two eyes
	{
		// see game "Highfleet" for it's exterior ship component placement interface
		public enum FoF // Friend or Foe
		{
			Friend = 0,
			Foe = 1 << 0, 
			Unknown = 1 << 2
		}
		
		public enum TYPE
		{
			Unknown,
			Drone,
			Asteroid,
			Debris,
			Mine,
			Missile,
			Fighter,
			Bomber,
			Frigate,
			Transport,
			Destroyer,
			Corvette,
			Carrier,
			Satellite,
			OrbitalPlatform,
			GroundRadar	
		}
		
		/*  decided to go with SensorContact.Radius instead 
		public enum SIZE
		{
			VerySmall = 0,      // Drone, Mine
			Small,              // X-Wing, Tie Fighter
			Medium,             // Mellenium Falcon
			Large,              // 
			VeryLarge,          // USS Enterprise
			Huge,               // Super StarDestroyer
			Enormous            // Death Star
		}
		*/
		
				
		public int Index;           // Index of this Contact within the Memory<T> contacts? Or, the Index of the TactialStation or Sensor that detected this Contact?
		
		public int ContactEntityArrayIndex;       // EntityIndex
		public string Name;            // verified name of ship eg. UEN Pegasus "Galactica Class Battlestar"
		public string RegistryNumber;  
		public FoF FriendOrFoe;        // Friend, Foe, Unknown
		
		public TYPE Type;              // unknown, drone, mine, missile, satelite, carrier, asteroid, frigate, etc.
		
		public Target.STATUS ContactStatus;  // withdrawing, combat ineffective, disabled, etc
		
		
		public struct ContactTelemetry
		{
			public double TimeLast; // this can be used to determine how stale a Sensor contact is.  gt.Time - LastDetectionTime == 0 then this is current.  Otherwise it's stale and we should add the previous telemetry to "History"
			public double TimeAcquired;
				
			 // how might Radius be spoofed?  Also, if two or more ships are in very close formation, can this result in the hiding of one or more
			// of those ships via emergent behavior?  I think we would need to explicitly program this...  it would depend on the closeness of 
			// the ships, whether they had transponders for IFF, the Level of the Sensor that made the detection(s), and perhaps the skill of the Operator
			public float Radius;             
			public Vector3d Position;
			public Vector3d Velocity;
			public double DistanceSquared;     // range to target
			public float Heading;       // NOTE: Bearing is the direction to fly to get somewhere specific see Google AI Overview notes below
		

			public double GetStaleAmount(GameTime gt)
			{
				// TODO: NowTicks should actually be set to the frequency of the updates for Sensors.
				//       and NEVER "real-time" NowTicks as used below.  Any "NowTicks()" should result from a DateTimeNow 
				//       that is manually incremented by the Fixed Time Step.
				double lag = gt.TotalElapsedSeconds - TimeLast;
				
				// CURRENT
				if (lag == 0)
				{
					
				}
				
				
				return 0;
				
			}
		}
		
		public ContactTelemetry[] Telemetry;
		
		// NOTE: It is assumed the ContactTelemetry being Added is for the exact
		//       same SensorContact as the existing ones in the Telemetry[] 
		public void Add(ContactTelemetry t)
		{
			Telemetry = Utils.ArrayAppend (Telemetry, t);
		}
		
		public void Add(ContactTelemetry[] t)
		{
			if (t != null && t.Length > 0)
				for (int i = 0; i < t.Length; i++)
					Add(t[i]);
		}
		
		public void Clear()
		{
			Telemetry = null;
		}
		
		
				
		public int[] SensorsIndices;   // the sensorIDs that have all acquired this target
		public int[] SensorsTypes;     // the UserTypeIDs of Sensors corresponding to the SensorsIndices
		
		
		
		/* Target Bearing is the angular direction from your current position to a target (often relative to North or your bow),
		while Target Heading is the direction the target itself is moving or pointing. Bearings tell you where to look, whereas headings indicate the target's trajectory. 
		
		Key Differences:
		Bearing (Direction to Target): The angle from your position to the target, often measured in degrees from True North (True Bearing) or clockwise from your bow (Relative Bearing).
		Heading (Direction of Travel): The direction your vessel, aircraft, or the target is facing.
		Context: In navigation, a bearing helps locate an object (e.g., "bearing 090" is East), while a heading is your current course (e.g., "heading 180" is South). 

		Application Example:
		If you are facing North (Heading) and a target is to your right, the relative bearing is 
		(East). If you turn East to follow it, your new heading is, but the bearing to the target changes as you close the distance. 
		*/


	}
	
	
	public struct Target
	{
		[Flags]
		public enum STATUS : int
		{
			Unknown = 0,           // a good tactical officer will flag a status of Unknown if not sure why it appears Disabled, rather than report it as Disabled when it could be playing possum waiting to draw your ship in
			Withdrawing = 1 << 0,
			Disabled    = 1 << 1,
			EnginesDisabled = 1 << 2,
			WeaponsDisabled = 1 << 3,
			ShieldsDisabled = 1 << 4,
			Active          = 1 << 5,
			NonCombatant     = 1 << 6,           // eg civilian, medical
			CombatIneffective     = 1 << 7,      // eg out of ammunition and/or power
			Neutral           = 1 << 8,
			Suspect           = 1 << 9,         // TODO: some of these need to move to FOF
			Hostile         = 1 << 10,
			Derelict        = 1 << 11
		}
		
		public enum CREWSTATUS
		{
			Unknown,
			Alive,
			Dead,
			LightlyDepleted,
			ModeratelyDepleted,
			HeavilyDepleted
		}
		
		public int EntityArrayIndex;
		public int[] WeaponsAssigned;
		public int[] TargetedBy;      // other Ships/Vehciles/Entities, ground radars, factions, etc that are targeting this Target
		public STATUS Status;
		public CREWSTATUS CrewStatus;
		public int Hitpoints;         // max hitpoints of target... should a Sensor be able to know this exact number?  It's really just a game thing and maybe we should just use visual observations of condition of ship instead
		public int CurrentHitPoints;  // used to determine % damage of Target
	}

	
	
    public struct DeltaInfo
    {
        public int ID;
        public object Properties;
        public int ReferenceStateID;

    }

				
	public enum DAMAGE_TYPE
	{
		Impaling,
		Burning				
	}
		
		// material quality
	public enum QUALITY_
	{
		Cheap = 0,
		BelowAverage,
		Average,
		Fine,
		VeryFine				
	}
#endregion  // Game01.GameObjects


////////////////////////////////////////////////////////////////////////////////////////////////
#region USER STRUCTS
////////////////////////////////////////////////////////////////////////////////////////////////
	[Flags]
	public enum CONFIGURATION : uint
	{
		None               =  1 << 0,
		Transform          =  1 << 1,
		RigidBody          =  1 << 2,
		LifeForm           =  1 << 3,
		Sentient           =  1 << 4,    // something that is 'AWARE' and can 'FEEL' and 'PERCEIVE' and has statistics like 'Age', 'Hitpoints' and such and survival Skills
		Intelligent        = 1 << 5,     // a SENTIENT that can recognize 'TRUTH' and operates on more than 'INSTINCT'  Can signify anything from an Android to a Human to an alien Xenomorph.  Characters can have Memberships and Skills other than Survival related
		
		SelfPropelled      = 1 << 6,    // can move under it's own power such as a Human or a Droid
		Container          = 1 << 7,  // Vehicles, Buildings that can have Components, Sentient's and even other Containers within it.
		Assembly           = 1 << 8,   // includes things attached to the EXTERIOR of Containers like Turrets, Pods, Superstructures, Towers, Masts
		Component          = 1 << 9,  // interior items of a building or vehicle that contain basic stats like Weight, Volume, Surface Area, Cost, and can be Armored
			// Useable
			Sensor         = 1 << 10,     // 
			Station        = 1 << 11,     // a type of Component that allows commands to be issued to various Crew and Components
			HelmStation    = 1 << 12,
			TacticalStation  = 1 << 13,
			EngineeringStation = 1 << 14,
			PowerProducing  = 1 << 15,
			PowerUsing     = 1 << 16,
			FuelGenerator  = 1 << 17,
			FuelUsing      = 1 << 18,
			Weapon         = 1 << 19,
			Laser          = 1 << 20
	}
	
	[Flags]
	public enum USER_RUNTIME_FLAGS : uint
	{
		IsPowered =          0,
		IsFueled =            1 << 0,
		IsHealthyEnough =    1 << 1, 
		OperatorHasSkills =  1 << 2, 
		IsOperatorStatusOK = 1 << 3,
		IsInUse =            1 << 4,  // aka isFiring for weapons)
		CanAct =             1 << 5,  // (for Stations, can an additional Action be performed at this Station... depends on TL of the Station),
		CanUse =             1 << 6,  // for Weapons this can be thought of as CanFire
		IsReloading =        1 << 7, 
		IsUnJamming =        1 << 8 // denotes a quick fix in the field requiring less than 1 minute to resolve (isFixingMinorMalfunction), 
	}
	
	public struct HitPoints // TODO: This may be renamed to "RPGStat" or something in the future and used for all stats that are modifiable (in one way or another.. eg an item buf or from damage taken)
	{
		public int Base;
		public int Current;
		
		public override string ToString()
		{
			return "HP: " + Current.ToString() + "/" + Base.ToString();
		}
	}
		
	//[StructLayout(LayoutKind.Sequential)]  // NOTE: "ideal" total struct size for L1 cache row purposes is 64 bytes.
	public struct LifeForm
	{
		public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		
		public string FullName;
		
		// These will serve as Station Operators for now
		public double CreationDateTime;
		public double Age;            // technically, this probably doesnt need to be stored... we only need the CreationDate?  // assign using Utils.GetAge() and find Age via 'age = Utils.GetAge(entity.CreationDate);'
		public double MaxAge;
		
		public HitPoints HitPoints; 
		public Armor Armor;
		
		public Membership[] Memberships;
		public Skill[] Skills;
		
		// LivingEntity vs Component both have this mRuntimeFlags but they are unique to each interface because typically LivingEntity and Component structs DO NOT exist within the same Entity.
		// - this could conceivably change in the future if for instance a Cyborg or Robot was also a "Character" that was needed the LivingEntity struct.
		public uint mRuntimeFlags;

	
		public double GetAge(double currentTime)
		{
			return currentTime - CreationDateTime;
		}
	}
			
		
	// NOTE: Production and Consumption belong in Entity, not in Component. 
    //public Production[] Production;   // eg. even a painting on a wall can produce +0.2 aesthic bonus to morale or happiness to crew
	//public Consumption[] Consumption; // eg. all components can consume damage.  
	public struct Component  // aka: "Useable Component"
    {
		public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		
        public string FullName;
		
		public uint Level; // technological level. 
		
        public float MaterialQuality; // cheap vs very fine materials (eg poorly refined steel vs damascus steel)
        public float Craftsmanship;   // how well the item is put together or manufactured (often taking into account the skill level of the maker)
        public bool Ruggedized;
		public bool Repairable; 
		
		/// <summary>
		/// Number of Human (as opposed to software/AI) Operators Required (if 0 then RequiresOperator {get { return NumOperatorsRequired > 0;}}
		///	      
		/// NOTE: if this is a medical bed 1 or 2 might be required.  For instance, the First "operator" is the patient and the Second "operator" is the Medical Professional.  
		///       The second operator isnt always necessary depending on what the first "operator" is doing... if recovering for instance, no second operator is needed.
		///</summary>
		public int NumOperatorsRequired; 
		
		/// <summary>
		/// The required skills an Operator must have to use this Component
		/// </summary>
		public Skill[] Skills;
		
		
		// 'Defense' is Armor (Armor Faces with Armor Layers and DR and PD)
		// TODO: i think these simply need to be part of the Component 
		// https://www.google.com/search?q=memory%3CT%3E+and+span%3CT%3E+from+a+struct+with+nested+structs&rlz=1C1GCPF_enUS1162US1162&oq=memory%3CT%3E+and+span%3CT%3E+from+a+struct+with+nested+structs&gs_lcrp=EgZjaHJvbWUyBggAEEUYOdIBCTExMDEzajBqMagCALACAA&sourceid=chrome&ie=UTF-8
        public Armor Armor; 
        public InternalStructure Internals; 	
		
        // stats
        public HitPoints HitPoints; 
        public float Cost;
        public float Weight;
        public float Volume;
        public float SurfaceArea;

        // runtime
		public int[] OperatorIDs;
		// LivingEntity vs Component both have this mRuntimeFlags but they are unique to each interface because typically LivingEntity and Component structs DO NOT exist within the same Entity.
		// - this could conceivably change in the future if for instance a Cyborg or Robot was also a "Character" that was needed the LivingEntity struct. 
		public uint mUserRuntimeFlags;
		public uint mUserStructFlags;
		
		public double StartTime; // when "Use" began
		public double Duration;  // if the "Use" is of a set Duration, track how long that Duration is... for instance, a sleep duration might be 6 hours of gameTime
		
		// todo: these bools would go into runtime stats as bitflags
		// along with isPowered, isFueld, isHealthyEnough, hasSkills, isOperatorStatusOK, isInUse(aka isFiring for weapons), canAct (for tacticalStations),
		// isReloading, isUnJamming (isFixingMalfunction), 
        public bool InUse;
		public bool Looping; // Repeating
		public double CooldownDuration; 
		
		
        public delegate void OnCreate();  // or OnAddedToScene()
        public delegate void OnDestroy(); // or OnRemovedFromScene()
		public delegate void OnUseStarted();
		public delegate void OnUseEnded();

		public void Use(string entityID)
		{
 		}
		
		public void SetUserStructFlag(uint flag, bool value)
		{
			mUserStructFlags |= flag;
		}
		
		public bool	GetUserStructFlag(uint flag)
		{
			return (flag & mUserStructFlags) != 0;	
		}
		
		public void SetUserRuntimeFlag(uint flag, bool value)
		{
			mUserRuntimeFlags |= flag;
		}
		
		public bool GetUserRuntimeFlag(uint flag)
		{
			return (flag & mUserRuntimeFlags) != 0;	
		}
							
		public bool DoIsPowered(out string errorReason)
		{
			errorReason = null;
			bool result = true;
			
			
			return result;
		}
		
		public bool DoIsFueled(out string errorReason)
		{
			errorReason = null;
			bool result = true;
			
			
			return result;
		}
		
		public bool DoIsHealthyEnough(out string errorReason)
		{
			errorReason = null;
			bool result = true;
			
			
			return result;
		}
		
		/// <summary>
		/// Verify if the Component Requires an Operator(s) and whether the Operator(s)
		/// have the required Skills to use this Component
		/// </summary>
		public bool DoIsOperatorStatusCheckOK(out string errorReason)
		{
			const float DAMAGE_PERCENT_THRESHOLD = 0.33f;
		
			errorReason = null;
			
			ComponentStore<LifeForm> allLivingEntities = EntryClass.mCStoreCol.CheckOut<LifeForm>(0);
			ComponentStore<Component> allComponents  = EntryClass.mCStoreCol.CheckOut<Component>(0);
			ComponentStore<TacticalStation> allTacticalStations  = EntryClass.mCStoreCol.CheckOut<TacticalStation>(0);
						
			// NOTE: if this station requires an AI operator at the very least, then NumOperators will be == 1.
			//       And the operator ID will point to another Component (eg a Computer running some tpe of software...eg Targeting Software)
			if (this.NumOperatorsRequired > 0)
			{
				// is the operatorID for a component and is it's UserTypeID a Computer running Software that can control this Component?
				
				if (this.OperatorIDs != null && this.OperatorIDs.Length >= this.NumOperatorsRequired)
				{
					// operator(s) is(are) healthy
					for (int i = 0; i < this.OperatorIDs.Length; i++)
					{
						int index = this.OperatorIDs[i];
						
						float percentage = allLivingEntities.Span[index].HitPoints.Current / allLivingEntities.Span[index].HitPoints.Base;
						if (percentage <= DAMAGE_PERCENT_THRESHOLD)
						{
							errorReason = "Operator '" + allLivingEntities.Span[index].FullName + "' is not Healthy enough to operate this Component.";
							return false;
						}
						
						
						if (this.Skills != null)
						{
							string name = allLivingEntities.Span[index].FullName;
							
							// operator has necessary skills to use this Component\Station
							Skill[] operatorSkills = allLivingEntities.Span[index].Skills;
							if (operatorSkills == null)
							{
								errorReason = "Operator '" + name + "' does not have the required skills to operate this Component";
								return false;
							}

							int totalSkillCount = 0;
							
							for (int j = 0; j < this.Skills.Length; j++)
							{
								for (int k = 0; k < operatorSkills.Length; k++)
								{
									if (operatorSkills[k].SkillType == this.Skills[j].SkillType)
									{
										if (operatorSkills[k].Level < this.Skills[j].Level)
										{
											
											int level = this.Skills[j].Level;
											string skillname = this.Skills[j].SkillType.ToString();

											errorReason = $"Operator {name}, does not have the required skill level {level} for the skill {skillname}.";
											return false;
										}
										else 
											totalSkillCount++;
									}
								}
							}
							
							if (totalSkillCount < this.Skills.Length)
							{
								errorReason = $"Operator {name}, does not have the required skills or skill levels for all skills required to use this Component.";
								return false;
							}
						}
					}
				}
			}
		
			return true;
		}	
		
		public bool IsInUse 
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsInUse) == (uint)USER_RUNTIME_FLAGS.IsInUse;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsInUse;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsInUse;
			}
		}
		
		public bool CanAct 
		{
			get {return true;} // {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.CanAct) == (uint)USER_RUNTIME_FLAGS.CanAct;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.CanAct;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.CanAct;
			}
		}
		
		public bool CanUse
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.CanUse) == (uint)USER_RUNTIME_FLAGS.CanUse;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.CanUse;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.CanUse;
			}
		}
		
		public bool IsPowered 
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsPowered) == (uint)USER_RUNTIME_FLAGS.IsPowered;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsPowered;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsPowered;
			}
		}
		
		public bool IsFueled 
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsFueled) == (uint)USER_RUNTIME_FLAGS.IsFueled;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsFueled;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsFueled;
			}
		}
		
		public bool IsHealthyEnough // component is healthy enough and can function
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsHealthyEnough) == (uint)USER_RUNTIME_FLAGS.IsHealthyEnough;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsHealthyEnough;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsHealthyEnough;
			}
		}
				
		public bool IsOperatorStatusOK // operator is healthy enough
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsOperatorStatusOK) == (uint)USER_RUNTIME_FLAGS.IsOperatorStatusOK;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsOperatorStatusOK;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsOperatorStatusOK;
			}
		}
		
		public bool OperatorHasSkills 
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.OperatorHasSkills) == (uint)USER_RUNTIME_FLAGS.OperatorHasSkills;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.OperatorHasSkills;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.OperatorHasSkills;
			}
		}
		
		public bool IsReloading 
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsReloading) == (uint)USER_RUNTIME_FLAGS.IsReloading;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsReloading;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsReloading;
			}
		}
		
		
		public bool IsUnJamming // aka fixing a minor malfuction that can be resolved in under 60 seconds
		{
			get {return (mUserRuntimeFlags & (uint)USER_RUNTIME_FLAGS.IsUnJamming) == (uint)USER_RUNTIME_FLAGS.IsUnJamming;}
			set 
			{
				if (value)
                	mUserRuntimeFlags |= (uint)USER_RUNTIME_FLAGS.IsUnJamming;
                else
                    mUserRuntimeFlags &= ~(uint)USER_RUNTIME_FLAGS.IsUnJamming;
			}
		}
	}
	
	
	// See Game01.Components
	public struct PowerProducer
	{
        public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		
		public bool Breaker;      // NOTE: we do not use node.Enabled because that is seperate (for rendering AND updating) from a Component running it's production simulation or not.
        
        //Definition: 1 kWh == 1 kW of power sustained for 1 hour.
        //    Usage Example: A 2,500-watt clothes dryer used for 2 hours consumes 5 kWh (2.5kW x 2  hours).
        //Average Consumption: The average U.S. household consumes approximately 899 kWh per month, or about 30 kWh per day.
        
		
        public double Output;    // kWh    
		public double Capacity;
		public double Duration;  
		public double MaxInput; // for a Battery, this is for recharging
		
		public double Store;
		
		// todo:efficiency should have a max value and a current value that can never exceed the max value.  Efficiency ranges from 0.0 - 1.0 
        public float Efficiency; // at same throttle, increased efficiency will produce more
//                                 // as the machine wears out between mainteneance efficiency
//                                 // will drop.  It is also possible to increase efficiency

		
        public float Throttle;  // aka: Regulator.  This value typically 0.0 - 1.0 but can exceed 1.0 with potential risk
//                                // of damaging the machine (is Damage a customProperty in Entity?)
			
		// TODO: the 'Component struct' should probably have a WearAndTear value that limits the Max Hitpoints that can be recovered perhaps
		//       unless the Component is repaired/refurbished/torndown+cleaned+reassembled/etc.
		
		// the following are PowerProducer runtime STATS that belong in 'struct PowerProducer'
		public double PowerDraw; //- combined power drawn from all Consumers (cannot exceed Output)
		public double PowerAvailable; // = Math.Min(Capacity, Output - PowerDraw);
		public double PowerIn; // = combined power INPUT from all Producers (cannot exceed MaxInput)  Should this only exist in PowerConsumer?
		
		
		// The index into the ComponentStore<Production> internal Memory<Production>
		// From there we can get an array of Consumers.
		// NOTE: Entities can produce more than one Production.  For instance,
		// a Reactor may normally just produce ElectricalPower but if it is damaged
		// it may also start Producing Heat and Radiation.  This is why we need
		// a seperate 'struct Production' for storing Production and why we cannot
		// just use this struct (PowerProducer) as the Production.  
		// NOTE: We use a lookup into the ComponentStore<Production> instead of storing 
		// the Production's themselves because modifying them here would NOT modify them
		// in the ComponentStore<Production> unless instead we stored the Memory<T> for each
		// and that is not necessary.
		public int[] Production;
		
		private const int ELECTRICAL = 0;
		private const int HEAT       = 1;
		private const int RADIATION  = 2;
		

		
	}
	
		
	// See Game01.Components
	public struct PowerConsumer  
    {
		// todo: just as Consumer struct has an index to this List<Boids> entry
		//       this PowerConsumer should have an index to the Memory<Consumption> it registered 
		//       Also, I think a Consumption should always have all the vars it needs to update
		//       both the PowerConsumer's vars and the Producers based on it's PowerDraw amount.
		//       
		
        public int EntityArrayIndex;    // Guid.NewGuid().ToString() results in a 36 character string.
		public CONFIGURATION Configuration; 
        
		public bool Breaker;  // NOTE: we do not use node.Enabled because that is seperate (for rendering AND updating) from a Component running it's production simulation or not.
        public double PowerRequirement;// per tick or per-use if "Continuous == false:
        public double MinimumPower;
		
		public bool Continuous; // whether this component always consumes power when operating, or only when it is "Used" such as a Laser firing for a fixed duration
		public float PerformanceSetting;  // 0.0 - 1.0.  We can get rid of HasVariablePerformance if PerformanceSetting >= 0 and <= 1.0
		public bool HasVariablePerformance {get {return (PerformanceSetting >= 0.0f && PerformanceSetting <= 1.0f); }} // can run at reduced power, but with reduced performance (eg sensor will have lower range)
        
		public int Priority;  // determines if there's insufficient power production, which consumers get higher priority to be powered during runtime 
		
		
        // runtime
        public float BreakerCycleDuration;
		
		public double TimeStarted;
		public double Duration;
		
		public bool Looping; // Repeating
		public double CooldownDuration; 
		public bool InCoolDown;
    }
	
		
	

	public struct TacticalStation
	{
		public class StationAction
		{
			public double TimeStarted;     // time this action started
			public double Duration;         // time to complete this action
			public int ActionID;         // eg Fire at Target, Lay Mines, Deploy Counter-measures
		}

		public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		//public int UserTypeID;
		
		// NOTE: The "GetLastAction() is simply the Action at index == 0

		// Queue is First In First Out
		public System.Collections.Generic.List<StationAction> Actions;
		public float CooldownBetweenActions;  //todo: maybe this is CurrentAction.Duration where "CanAct" = (NumActions < Actions.Count - 1 && elapsed >= CurrentAction.Duration)  the minimum amount of time since the previous action before the next action can take place e.g 4.5 seconds and represents the time it takes to carry out that previous Action and to be ready to carry out the next
		                                       // or it might be the Math.Max(thisAction, prevAction) since a previious action might take less time to complete so we will be able to act when it completes first.
		public int HistoryCount; 
		public int NumActions;
		public int MaxActions;        // based on operator's max ability to handle so many simultaneously, tacticalstation TL, tacticalStation damage, and ability to perform that many actions in the first place (eg having enough weapons to use )

		public System.Collections.Generic.Queue<List<SensorContact>> ContactsHistory;
		public List<SensorContact> Contacts;
		public List<Target> Targets;


		public void AddContact(SensorContact c)
		{
		}

		public void RemoveContact()
		{
			
		}

		public void ClearContacts()
		{
			
		}

		public void AddTarget(Target t)
		{
			
		}
		
		public void Add (StationAction a)
		{
			if (Actions == null) Actions = new List<StationAction>();
			Actions.Add(a);
		}
		
		public void Remove (StationAction a)
		{
			Actions.Remove(a);
			if (Actions.Count == 0)
				Actions = null;
		}

		// todo: Actions that have completed need to be removed from a list?
		///<summary>
		/// Determines if an Action can be assigned to this Component based on existing Actions
		/// and skill of Operator.
		/// </summary>
		/// <remarks>
		/// Example of StationActions are as follows:
		/// 1 - moving a turret to aim at a specific target and fire when ready
		/// 2 - prioritizing targets
		/// 3 - assigning targets to weapons
		/// 4 - monitoring guided missiles to ensure they are on course, otherwise they may need to be given a destruct order to avoid hazards to friendly ships (including your own)
		/// </remarks>
		public bool CanAct(out string errorReason)
		{
			EntityNode station = EntryClass.bSim.Boids[this.EntityArrayIndex]; // an actual Boid but for now, we think of it as dedicated Station Component Entity
			errorReason = null;
			bool result = true;

		#if DEBUG
			int componentIndex;
			Memory<Component> cmp = (Memory<Component>) station.GetUserStruct(typeof(Component), out componentIndex); //"HelloBoids.Component"); // );
			System.Diagnostics.Debug.Assert (station.EntityArrayIndex == cmp.Span[0].EntityArrayIndex);
		#endif
				
				
			if (!cmp.IsEmpty)
			{
				string name = cmp.Span[0].FullName;
				int max = MaxActions;
				int diff = NumActions;
				if (diff <= 0)
				{
					errorReason = $"Station {name}, is already performing the maximum {max} number of simultaneous actions allowed based on the Level of this Station, it's current condition and the skill and condition of it's currrent Operator.";
					return false;
				}
				
				// check the cooldowns (if a slot is available, then doesn't this mean the cooldown has expired?  
				// once a cooldown expires, the action is removed from the list of current actions correct?
				
				List<int>toRemove = new List<int>();
				int pos = 0;
				foreach (var item in Actions)
				{
					double elapsed = Utils.GetAge(item.TimeStarted);
					bool hasElapsed = elapsed > item.Duration;
					if (hasElapsed)
					{
						toRemove.Add(pos);
					}
					pos++;
					
				}

				if (toRemove.Count > 0)
					for (int i = 0; i < toRemove.Count; i++)
						Actions.RemoveAt(toRemove[i]);
				
			}
			return result;
		}

		public int GetMaxActionCount()
		{
			int result = 0;

			result = Actions.Count;

			// station powered? (assuming it requires power to function)
			// station TL
			// station Damage (damage = CurrentHitPoints / Hitpoints
			// operator Skill + Bonuses (can limit max actions as well) =
			// opertor Health  // the max time between actions may also slow down as a result of an injured operator
			
			return result;
		}

		public int GetOperatorAssignments(int operatorIndex)
		{
			int result = 0;


			return result;
		}

		// for this specific sensor
		public int GetMaxTargets()
		{
			int result = 0;

			return result;
		}
	}
		
	
	/// Sensors are Consumers of things like MicrowaveSignature, SoundSignature, etc
	/// but in the case of Active Sensors, like an active Radar and Ladar, they can PRODUCE
	/// MicrowaveEmissions, LaserEmissions, etc when they are being USED.
	public struct Sensor
	{
		public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		//public int InternalComponentIndex;   // <-- may not be necessary.  Just grab from Entity?		
		// public int UserTypeID; // <-- may not be necessary
		
		
		//"Sensory Instruments and Electronics must be placed in Periscope, Body, Superstructure, Pod, equipment Pod, Turret, Popturret, Arm, Wing, Open Mount, Leg or Module."

		// optical
		//    eyes
		//    telescopes
		//    periscopes
		//    magnifyers
		
		// radar/ladar
			public bool NoTargeting;
			public string SearchOption;
			public bool FTL;   // if FTL, range is in light-seconds
			public double RangeSquared;
			public int ScanRating;  // <-- this is a computed stat based on TL and Power, that generally ranges from 10 - 40+  (google "gurps vehicles 2nd edition radar scan rating")
		
			// types using Radar and Ladar
		    	// case Radar
            	// case NavigationalRadar <-- uses NoTargeting = true
            	// case AntiCollisionRadar
		    	// case Ladar
            	// case AESA
            	// case HiResImagingRadar
		 		// case LowResImagingRadar

		// scientific sensors
		//  	Case LowResPlanetarySurveyArray   // can have options for using Microwaves, Ultrasound if in atmosphere or water, Radar, etc?
    	//       case MedResPlanetarySurveyArray
        //       case HighResPlanetarySurveyArray
		
		// following used by "Other types of scanners"
			// float Range As Single
			// long ScanRating As Long
			
			// Types of "other"
			//    Thermal, Passive IR
			//     PassiveInfrared
    		//     Thermograph
   			//    PassiveRadar
    		//     PESA
		
			// Case Geophone
			// Case MAD
     		// Case MultiScanner
            // Case ChemScanner
            // Case RadScanner
            // Case BioScanner
            // Case GravScanner
		
		
		// sonar
			// bool DepthFinding As Boolean
			// bool DippingSonar As Boolean
			// bool TowedArray As Boolean
			// bool NoTargeting As Boolean
			// float Range As Single
			// long ScanRating As Long
			
		// sound
			// long Level 
		
		
	}
	
   // Laser:Weapon:Component

	// In \\KeystoneGameBlocks\\ see \\game01\\Components\\Weapon
	public struct Weapon 
    {
		public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		
        // kinetic energy type weapons build parameters 
        public float Bore;
        public int BarrelLength;
        public bool Reliable;
		public bool Compact;
		
        // stats
        
        public int NumShots;      // Should be like HitPoints... HitPoints.Base and HitPoints.Current  so NumShots.Base and NumShots.Current.  Make sure NumShots can accommodate the RoF
		public float CoolDown;    // This is the Rate-of-Fire (RoF) expressed as a cooldown value.  For instance, a RoF = 1/5 means once shot per 5 turns (eg 1 per every 5 seconds == 5 second cooldown) RoF = 1 means one shot per one second = 1 second cooldown.  
		public float ReloadCoolDown; // This occurs after NumShots.Current reaches 0.
		
		public double Range;
		public double RangeSquared; // for convenience rather than having to recompute for comparison against the distance of a target Entity
		//			public double MaxRange2;
		//			public double VacuumMaxRange;
		//			public double VacuumMaxRange2;
        
        public float Accuracy;      // based on type of weapon (revolvers -> rifles -> lasers) 
		public int SnapShot;        // this is a penalty for Accuracy when not aiming but needing to try to hit a target as quickly as possible.  It's based on the weapons bulk,design and size.
		                            // For our purposes, this would be a statistic and probalby modify our [0.0 - 1.0f] Accuracy stat
        
		
		// 0.0 - 1.0f coefficient for tendancy to malfunction. MaterialQuality and Craftsmanship have impact
        public float Malfunction;    // 0 to Malfunction with 1.0 being maximum meaning it would malfunction every time and 0.0f never.
		                             // Malfunction is determined from Level, Craftsmenship, MaterialQuality and currentHitpoints
				

		public DAMAGE_TYPE DamageType;
        public int AverageDamage; // amount of damage it can inflict
        //   Kinetic Energy (KE) damage in GURPS—or more accurately, calculating damage based on muzzle energy or impact velocity—is primarily needed to bridge the gap between abstract gameplay mechanics and realistic, simulation-heavy ballistics. While the GURPS Basic Set provides simplified damage values for common weapons, a formal KE system is needed to:Standardize Weapon Stats: It ensures that damage across different guns, especially experimental or high-TL weapons, is mathematically consistent rather than based on guesswork.Accurately Model Armor Penetration: Penetration in reality scales with KE divided by the cross-sectional area of the projectile. A formal system allows for precise calculation of how a bullet interacts with DR (Damage Resistance).Bridge TL Gaps: It allows for realistic conversions between different technological levels (TL), ensuring a TL7 rifle feels correctly powered compared to a TL9 railgun, based on actual energy output.Why a Specific KE System is UsedThe need for this arises because simply scaling damage linearly with velocity does not work.Consistency: The GURPS 4th Edition Basic Set allows for varied wounding modifiers based on caliber (e.g., \(pi-\), \(pi\), \(pi+\), \(pi++\)).Realism over Fiat: Instead of a writer guessing that a gun does \(2d+2\), developers or GMs use projectile velocity and mass to calculate KE and map that to a realistic GURPS damage die.Collisions: KE calculation is vital for determining damage in massive impacts, such as vehicle crashes or huge monsters falling, which is not easily covered by standard weapon stats.Summary of UtilityHigh-Tech Campaigns: Essential for balancing modern and futuristic firearms (High-Tech, Ultra-Tech).Detailed Simulation: Used by GMs who want armor penetration to follow physical laws rather than abstract tables.Vehicular Combat: Used to calculate damage from collisions (e.g., GURPS Vehicles 2nd Ed).In short, while not needed for cinematic games, a formal Kinetic Energy formula is needed to keep damage realistic and consistent when dealing with high-velocity weapons or physics-heavy scenarios.
		//public double KEDamage; (Crushing or Impaling damage formula specifically =  KEDamage = Damage * Velocity * Acceleration * Weight
		
		public int FallOffStart; // distance in meters at which the damage inflicted begins to be reduced
		// public double VacuumFallOffStart;    
		
		
		
		
        // runtime flags
        //public bool IsFiring; // todo: for weapons this is Component.isInUse, 
        public bool IsReloading;
        public bool IsUnJamming; // represents fix of minor malfunction... does not require a "repair"
        //public bool IsPowered;
        //public bool IsHealthy;
        
        // nested weapon.  
        //public Weapon SecondaryWeapon;
		
		
		public bool CanFire(out string errorReason)
		{
			EntityNode weapoon = EntryClass.bSim.Boids[this.EntityArrayIndex]; 
			errorReason = null;
			bool result = true;

			return true;
		}
    }
	
	
	/*
	ref struct ComponentLaserStruct
	{
		public ComponentStruct[] Components;
		public WeaponStruct[] Weapons;
		public LaserStruct[] Lasers;
		public Armor[] Armor;
		//public ComponentLaserStruct[] Records;
	}
	*/
	
	
	public struct Laser_Struct
	{
		public int EntityArrayIndex;
		public CONFIGURATION Configuration;
		//public int UserTypeID;
		//public int WeaponIndex;
		
		// beam specific
		public int Type;       // type is really just about what types of Damage(s) (ProductID(s)) it results in such as Paralysis, Crushing, Burning, Impaling
		
		public bool EnergyDrill;
		public bool FTL;

		
		public float BeamOutput;    // kJ - kiloJoules -  what is the difference between this and kW of power... is it the convsion rate of the input power to the output power?
		public float CyclicRate;    //   Expressed as a cooldown value.  The maximum possible firing rate of the weapon without considering overheating or ammunition capacity. Often, RoF and CyclicRate are the same, but CyclicRate is theoretical maximum given mechanics of the weapon
		
		public double PowerReqt;    // todo: this might be part of PoweredComponent struct.  Depends on whether we just put it into Component struct because PoweredComponent struct may not have many fields to hold... but then again, that is not entirely bad is it as long as we are good at only doing updates to one struct at a time.  For instance, applying / distributing power to all Powered structs in one swoop


		// TODO: these are like "internal" items and can be used if another power source is no longer connected
		//			public string PowerCellType;  // TOOD: Need an ENUM
		//			public int PowerCellQuantity;
		//			public double PowerCellWeight;


	}


	
	
	public struct Armor
    {
	
		
        public const int MAX_ARMOR_LAYERS = 5;
        public const int NUM_ARMOR_FACES = 6; //4 = front, back, left, right.  6 adds 'top' and 'back'.
        public ArmorFace[] Faces;
		
		public double SurfaceArea 
		{
			get
			{
				double result = 0;
				for (int i = 0; i < Faces.Length; i++)
					result += Faces[i].SurfaceArea;
				
				return result;
			}
		}
		

		public double Cost;
		public double Weight;
		
		// average DR of all layers on all Faces
		public int AverageDR 
		{
			get
			{
				if (Faces == null || Faces.Length == 0) return 0;
				
				int result = 0;
				for (int i = 0; i < Faces.Length; i++)
					// recall that the DR per Face is the combined DR of all Layers.DR of that Face
					result += Faces[i].DR;
				
				// average for each Face is totaly DRs of all Layers divided by the number of Faces
				return result / Faces.Length;
			}
		}
		
		//public int Defense;           // shortcut overall Passive Defense // Passive Defense is a type of defense that requires no active trying to defeat an attack against it
		
		// can be init with 5 or 6 sides, with each side having arbitrary number of layers with NO MINIMUM either... so one or more sides can be completely UN-ARMORED
		public Armor(BoundingBox box, uint numFaces = 6, uint numLayers = 1)
		{
			if (numFaces != NUM_ARMOR_FACES) throw new ArgumentOutOfRangeException();
			
			Faces = new ArmorFace[numFaces];
			
			for (int i = 0; i < numFaces; i++)
			{
				double surfaceArea = GetArmorFaceSurfaceArea ((BoundingBox.BOX_FACES)i, box);
				
				Faces[i] = new ArmorFace(box, i);
				Faces[i].SurfaceAttributes = ArmorFace.SURFACE_ATTRIBUTES.None;
				    
				Faces[i].Defense = 50; // passive defense... 
				Faces[i].Layers = new ArmorLayer[numLayers];
					for (int j = 0; j < numLayers; j++)
					{
						int DR = 50;
						string material = "iron";
						float quality = 0.5f;  // 0.1 is very poor/cheap,  0.5 is average quality, 0.9 is Space-Grade, 1.0 is Advanced-Spec
						double cost = GetArmorCost (DR, Faces[i].SurfaceArea, material, quality); // 100;
						double weight = GetArmorWeight(DR, Faces[i].SurfaceArea, material, quality); // 2000
						
						ArmorLayer layer;
						layer.Cost = cost;
						layer.Weight = weight;
						layer.DR = DR;
						layer.Material = material; // type of material should be enum (wood, metal, non-rigid, ablative, fireproof-ablative, composite, laminate
						layer.Quality = quality; //  a coefficient with 1.0 being the highest possible quality material
						
						Faces[i].Layers[j] = layer;		
					}
			}
		}
		
		public double GetArmorFaceSurfaceArea (BoundingBox.BOX_FACES side, BoundingBox box)
		{
			double result = 0;
			
			
			// TODO: make sure our ArmorFace[] array indices match those of our BoundingBox
			//       WARNING: See BoundingBox.GetQuadFaceVertices()  because i think
			//       there we grab the vertices for each face and we are using different
			//       indices for each Face.  They need to be the same.
			// Face0 (the end that points +z from camera) = FRONT = WIDTH * HEIGHT
			// Face1 (the end that points -z toward camera) = BACK = WIDTH * HEIGHT
			// Face2 = (the end that points +x to right of camera) = RIGHT = DEPTH * HEIGHT
			// Face3 = (the end that points -x to left of camera) = LEFT = DEPTH * HEIGHT
			// Face4 = top +y = TOP = WIDTH * DEPTH
			// Face5 = bottom -y = BOTTOM = WIDTH * DEPTH
			
			BoundingBox.BOX_FACES eSide = (BoundingBox.BOX_FACES)side;
			
			switch (eSide)
			{
				case BoundingBox.BOX_FACES.RIGHT:
				case BoundingBox.BOX_FACES.LEFT:
					result = box.Height * box.Depth;
					break;
				case BoundingBox.BOX_FACES.TOP:
				case BoundingBox.BOX_FACES.BOTTOM:
					result = box.Width * box.Depth;
					break;
				case BoundingBox.BOX_FACES.FRONT: // <--NOTE: "FRONT" (+z) denotes facing INTO the camera.  So if you place an Actor into the scene, the eyes of that actor will be facing away from you and into the Camera unless you apply a 180 y axis rotation in the assetplacementtgool logic
				case BoundingBox.BOX_FACES.BACK:
					result = box.Width * box.Height;
					break;
			}
				
			
			// and for any given one side surfaceArea = LW or surfaceArea = LH or surfaceArea = WH 
			// or a 
			// volume in cubic meters where surfaceArea = 6 x (cube root of (volume))^2 
			
			return result;
		}
		
		/// <summary>
		/// surfaceAreaCubicMeters
		/// </summary>
		public double GetArmorWeight (int damageResistance, double surfaceArea, string material, float quality)
		{
			double result = 0;

			switch (material)
			{
				/*  https://www.quora.com/What-is-the-cost-of-one-cubic-meter-of-low-carbon-steel
				I don’t know of any steel plant that could cast 1 cubic metre in a single block
				(and I know the industry very well). However, if the 1 cubic metre were made up 
				of 4 slabs, each 250mm (1/4 meter) thick, then you could have a “block” 1 cubic metre thick. 
				This block would weigh 7,850kg = 7.85 tonnes. The current price of slabs – Brazil,
				FOB port – is $US490 to $US505 per tonne, so your 1 cubic metre would cost 
				approximately $US3,905.
				*/
					
				/*  https://www.quora.com/What-is-the-cost-of-one-cubic-meter-of-low-carbon-steel
				Price of one cubic meter of low‑carbon steel varies by grade, form, market and region. As of mid‑2024 typical ranges and how to compute cost:

				Key inputs

				Density (approximate): 7,850 kg/m³ (commonly used value for mild/low‑carbon steels).
				Price basis: usually quoted in currency per tonne (metric ton = 1,000 kg).
				Conversion: 1 m³ ≈ 7.85 tonnes, so multiply price per tonne by 7.85.
				Typical market price examples (approximate, mid‑2024 observations)

				Commodity mild/low‑carbon steel (rolled coil, bulk domestic): US$600–1,000 per tonne → US$4,700–7,850 per m³.
				Structural/plate mild steel (common commercial grades): US$700–1,200 per tonne → US$5,500–9,420 per m³.
				Low‑carbon specialty or alloyed variants: higher; US$1,200–2,000+ per tonne → US$9,420–15,700+ per m³.
				How to get an exact current cost

				Identify the exact grade and product form (sheet, plate, ingot, billet) — processing and yield affect price.
				Check spot prices on commodity services (Metal Bulletin, Fastmarkets, Platts) or regional steel distributors.
				Convert by multiplying quoted price per tonne by 7.85 to get per‑m³ cost.
				Add applicable extras: freight, customs/duties, cutting/processing, taxes, and volume discounts.
				Example calculation

				If supplier quote = US$850/tonne for mild steel: 850 × 7.85 = US$6,672.50 per m³ (plus logistics and processing).
				Regional note

				Prices fluctuate with scrap/iron ore markets, energy costs and local trade policy; use local supplier quotes for procurement decisions.
				If a precise, up‑to‑date number is required for budgeting, obtain current per‑tonne quotes from local mills or distributors and apply the 7.85 multiplier.
				*/
					
				case "iron":
					// DR = 2.75DR per 1mm of IRON thickness.  1 meter by 1 meter by 1mm thick iron plate = 7.85 kilograms
					//
					//      So we can let the user type in the thickness of the armor and we can compute the DR ourselves
					//      and frankly just forget about using DR at all.  We'll only deal in "thickness".. well... 
					//      the reason for a 'DR' is so that we can roughly compare (conceptually) different material types... like... 
					//      1mm thick of IRON == 1 meter thick of CARDBOARD == 2.75DR
					//
					result = 500d * surfaceArea * damageResistance;
					break;
				default:
					break;
			}

			return result;
		}

		public double GetArmorCost (int damageResistance, double surfaceArea, string material, float quality)
		{
			double result = 0;

			switch (material)
			{
				case "iron":
					double thickness = damageResistance / 2.75d;
					double pricePerKG = 0.70d; // 3.00 per kg for high-carbon/alloyed
					// so we'd like quality (0.0 - 1.0) to map linearly from 0.70 scrap to 3.00 highcarbon/alloyed  
					result = pricePerKG * GetArmorWeight (damageResistance, surfaceArea, material, quality);
					break;
				default:
					break;
			}

			return result;
		}
    }
    
    public struct ArmorFace
    {
		[Flags]
		public enum SURFACE_ATTRIBUTES : byte
		{
			None = 0,                // 0
    		RAP = 1 << 0,            // 1
    		Electrified = 1 << 1,    // 2
    		ThermalCoating = 1 << 2, // 4
    		RadShielding = 1 << 3,   // 8
			ReflectiveCoating = 1 << 4, // 16 // todo: there are multiple types of ReflectiveCoating right? 
    		All = RAP | Electrified | ThermalCoating | RadShielding | ReflectiveCoating
		}
		
        //public bool RAP;  // reactive armor plate
        //public bool Electrified;
        //public bool ThermalCoating;
        //public bool RadShielding;
        //public string ReflectiveCoating;  // todo: what types are there? see gvd // todo:  need enums or perhaps a coefficient value instead AND THE GUI can interpet this coefficient into a string if desired
		public SURFACE_ATTRIBUTES SurfaceAttributes;
		
		private int mFaceIndex; // from BoundingBox
		private BoundingBox mBox;
		public ArmorFace(BoundingBox box, int faceIndex)
		{
			mBox = box;
			mFaceIndex = faceIndex; // the faceIndex of the BoundingBox (see BoundingBox.GetFaceVertices())
			
			Vertices = BoundingBox.GetQuadFaceVertices(mBox);
		}
		
		public ArmorLayer[] Layers;
		
		/* 
			Common armor slopes, particularly in armored fighting vehicle (AFV) design, typically range from 30 to over 80 degrees back from the vertical to increase the effective line-of-sight thickness and improve deflection chances. The most iconic design is the 60-degree slope, which doubles the effective thickness of the armour compared to its nominal thickness. 
			Common Armor Slope Angles (from Vertical)
            	60 Degrees: The standard "optimal" slope for WWII-era vehicles, such as the T-34's glacis plate, which provides 2x the effective thickness ( 45mm / cos(60 degrees) = 90mm  ).
				45–55 Degrees: Frequently used on intermediate armored vehicle designs, such as the Panzer V Panther (approx. 55°), which offers a roughly effectiveness multiplier to the line-of-sight thickness, depending on the research source.
            	75–82+ Degrees: Highly sloped, nearly horizontal angles found on "pike nose" designs (IS-3) or the front glacis of modern main battle tanks like the M1 Abrams, which can reach 80+ degrees, making the armor extremely effective against horizontal fire
		*/
		
		public byte Slope;
		       
		
	     
		// Armor DR and Passive Defense is additional to component DR, specialized defensive material added to the component to increase its protection (e.g., bolted-on steel plates, Kevlar blankets, or composite ceramic armor).
         		//    See Google AI Overview in Game01.Components.Armor.cs 
		// Defense Resistance - natural protection provided by the material and structure of the vehicle
		// component itself (e.g., the 1-inch thick steel hull, the aluminum skin of an aircraft, or 
		// the glass of a windshield).
		public int DR
		{
			get
			{
				if (Layers == null || Layers.Length == 0) return 0;
				int result = 0;
				for (int i = 0; i < Layers.Length; i++)
					result += Layers[i].DR;
				
				return result;
			}
		}      						
        
		public int Defense;  // Passive Defense - see Google AI Overview in Game01.Components.Armor.cs Definition: PD acts as a bonus to the vehicle's evasion roll (Active Defense). Component PD is used when a specific part (like a turret, rotor, or sensor array) is targeted rather than the vehicle as a whole.
 
		public Vector3d[,] Vertices;

		public double Width 
		{
			get 
			{
				double result = 0d;
				//Vector3d min = Vector3d.Min(Vertices[0], Vertices[1]);
				//min = Vector3d.Min(min, Vertices[2]);
				
				return result;
			}
		}        

		public double Height
		{
			get 
			{
				double result = 0d;
				return result;
			}
		}        

        public double SurfaceArea 
		{
			get 
			{
				double result = 0;
				// todo: to compute the surface area of each face, we should pass in a 
				// box primitive where surfaceArea = 2 * (WH + DH + WD)  
				double D = mBox.Depth;
				double W = mBox.Width;
				double H = mBox.Height;

				// the face indices match those of BoundingBox.GetQuadFaceVerices()
				// This also matches enums for TV3D CUBEMAP faces.
				// 0: Positive X (Right)1: Negative X (Left)2: Positive Y (Top)3: Negative Y (Bottom)4: Positive Z (Front)5: Negative Z (Back)
					
				switch (mFaceIndex)
				{
					case 0: // RIGHT (+x)
					case 1: // LEFT (-x)
						result = W * D;
						break;
					
					case 2: // TOP (+y)
					case 3: // BOTTOM (-y)
						result = H * D;
						break;
						
					case 4: // FRONT (+z) <--NOTE: "FRONT" (+z) denotes facing INTO the camera.  So if you place an Actor into the scene, the eyes of that actor will be facing away from you and into the Camera unless you apply a 180 y axis rotation in the assetplacementtgool logic
					case 5: // BACK (-z)
						result = W*H;
						break;
				}
			
				return result;
			}
		}
		
        public double Weight 
		{
			get
			{
				if (Layers == null || Layers.Length == 0) return 0;
				double result = 0;
				for (int i = 0; i < Layers.Length; i++)
					result += Layers[i].Weight;
				
				return result;
			}
		}
		
        public double Cost
		{
			get
			{
				if (Layers == null || Layers.Length == 0) return 0;
				double result = 0;
				for (int i = 0; i < Layers.Length; i++)
					result += Layers[i].Cost;
				
				return result;
			}
		}
		
		public bool RAP 
		{
			get {return (SurfaceAttributes & SURFACE_ATTRIBUTES.RAP) == SURFACE_ATTRIBUTES.RAP;}
			set 
			{
				if (value)
                	SurfaceAttributes |= SURFACE_ATTRIBUTES.RAP;
                else
                    SurfaceAttributes &= ~SURFACE_ATTRIBUTES.RAP;
			}
		}
		
		public bool Electrified 
		{
			get {return (SurfaceAttributes & SURFACE_ATTRIBUTES.Electrified) == SURFACE_ATTRIBUTES.Electrified;}
			set 
			{
				if (value)
                	SurfaceAttributes |= SURFACE_ATTRIBUTES.Electrified;
                else
                    SurfaceAttributes &= ~SURFACE_ATTRIBUTES.Electrified;
			}
		}
		
		public bool ThermalCoating 
		{
			get {return (SurfaceAttributes & SURFACE_ATTRIBUTES.ThermalCoating) == SURFACE_ATTRIBUTES.ThermalCoating;}
			set 
			{
				if (value)
                	SurfaceAttributes |= SURFACE_ATTRIBUTES.ThermalCoating;
                else
                    SurfaceAttributes &= ~SURFACE_ATTRIBUTES.ThermalCoating;
			}
		}
		
		public bool RadShielding 
		{
			get {return (SurfaceAttributes & SURFACE_ATTRIBUTES.RadShielding) == SURFACE_ATTRIBUTES.RadShielding;}
			set 
			{
				if (value)
                	SurfaceAttributes |= SURFACE_ATTRIBUTES.RadShielding;
                else
                    SurfaceAttributes &= ~SURFACE_ATTRIBUTES.RadShielding;
			}
		}
		
		public bool ReflectiveCoating 
		{
			get {return (SurfaceAttributes & SURFACE_ATTRIBUTES.ReflectiveCoating) == SURFACE_ATTRIBUTES.ReflectiveCoating;}
			set 
			{
				if (value)
                	SurfaceAttributes |= SURFACE_ATTRIBUTES.ReflectiveCoating;
                else
                    SurfaceAttributes &= ~SURFACE_ATTRIBUTES.ReflectiveCoating;
			}
		}
    }
    
	
    public struct ArmorLayer
    {
        public string Material;   // material type e.g metal // TODO: need enums
        public float Quality;    
        public double Weight;
        public double Cost; 
		public int DR;
    }
	

    public struct InternalStructure
    {
		[Flags]
		public enum STRUCTURE_ATTRIBUTES : byte
		{
			None = 0,                // 0
    		Robotic = 1 << 0,            // 1
    		Biomechanical = 1 << 1,    // 2
    		Responsive = 1 << 2, // 4
    		LivingMetal = 1 << 3,   // 8
    		All = Robotic | Biomechanical | Responsive | LivingMetal
		}
		
		public STRUCTURE_ATTRIBUTES StructureAttributes;
        public int MaterialType; // wood, metal, composite
        public float Strength;  // frame strength
        			
        public byte SlopeLeft; // note: slope uses constants to represent 0, 30 or 60
        public byte SlopeRight;
        public byte SlopeFront;
        public byte SlopeBack;
        
        // todo: is this correct place to have streamlining?  It would have to be set individually for each subassembly?
        public string StreamLining; // todo:  need enums or perhaps a coefficient value instead AND THE GUI can interpet this coefficient into a string if desired
        // NOTE: hitpoints I think is fine for inanimate objects,
        //       but not good for living things. 
        //       https://www.youtube.com/watch?v=sMWMB9bjFGo
        public HitPoints HitPoints;		
		
		public bool Robotic 
		{
			get {return (StructureAttributes & STRUCTURE_ATTRIBUTES.Robotic) == STRUCTURE_ATTRIBUTES.Robotic;}
			set 
			{
				if (value)
                	StructureAttributes |= STRUCTURE_ATTRIBUTES.Robotic;
                else
                    StructureAttributes &= ~STRUCTURE_ATTRIBUTES.Robotic;
			}
		}
		
		public bool Biomechanical 
		{
			get {return (StructureAttributes & STRUCTURE_ATTRIBUTES.Biomechanical) == STRUCTURE_ATTRIBUTES.Biomechanical;}
			set 
			{
				if (value)
                	StructureAttributes |= STRUCTURE_ATTRIBUTES.Biomechanical;
                else
                    StructureAttributes &= ~STRUCTURE_ATTRIBUTES.Biomechanical;
			}
		}
		
		public bool Responsive 
		{
			get {return (StructureAttributes & STRUCTURE_ATTRIBUTES.Responsive) == STRUCTURE_ATTRIBUTES.Responsive;}
			set 
			{
				if (value)
                	StructureAttributes |= STRUCTURE_ATTRIBUTES.Responsive;
                else
                    StructureAttributes &= ~STRUCTURE_ATTRIBUTES.Responsive;
			}
		}
		
		public bool LivingMetal 
		{
			get {return (StructureAttributes & STRUCTURE_ATTRIBUTES.LivingMetal) == STRUCTURE_ATTRIBUTES.LivingMetal;}
			set 
			{
				if (value)
                	StructureAttributes |= STRUCTURE_ATTRIBUTES.LivingMetal;
                else
                    StructureAttributes &= ~STRUCTURE_ATTRIBUTES.LivingMetal;
			}
		}
    }

	#endregion // USER STRUCTS 

    public interface IEntitySystem
    {
        // 1) an IEntitySystem of type "City{World.Country.Province.County}" might include many different types of child IEntitySystem within it.
        //    eg. University, Factory, Arthouses, Houses of Worship, Acadamies, Mines, Lodges, Farms, Museums, Research Fascilities, Heavy Idustries, Parks
        //        Parks, etc.
        //        - These IEntitySystems are very much like the Simulation of a Vehicle and it's part... each uses "Production and Consumption" that can be
        //        received by the Simulation.cs in a very consistant/agnosic way.
        //        - people with various "skills" can be "produced" from Academies... not just minerals, crops, or commodities.
        //        - these Systems also CONSUME from "Stores"... how do we assign Stores and make them available to something like a "City?"
        // 2) Stores - food, supplies, medicines, clothing, energy
        // 3) Do we need to support rendering Proxies here (2D and 3D?)
		public struct EntitySystemUpdateContext
    	{
        	// see SelectionNode or Elements.SwitchNode for help
			
			
    	}
	
        int Seed { get; }
        int EntityCount { get; }
        bool MultithreadingEnabled { get; set; }

        // TODO: perhaps grab the max count from a configuration file
        int MaxEntityCount { get; set; }

        void GenerateSystem();
        // todo: need delegates for handling the Generate()
        // todo: need delegate for Create() of single IProcGeneratedItem


        // libnoise uses this to find a value on a texture
        object GetValue(double x, double y, double z);
        IProcGeneratedItem GetItem(string address);
        IProcGeneratedItem GetItem(int index);
        IProcGeneratedItem GetItem(string guid, int seed);

        void Update(double elapsedSeconds, EntitySystemUpdateContext context);
        void Read();
        void Write();
    }

	

    public abstract class EntitySystemBase : EntityNode, IEntitySystem
    {
        public delegate IProcGeneratedItem CreateEntityHandler(int seed, string path);
        public delegate void GenerateSystemHandler(int seed);
        public delegate void UpdateHandler(double elapsedSeconds, IEntitySystem.EntitySystemUpdateContext context);

        // private variables
        protected string mPath;
        protected int mMaxEntityCount;
        protected bool mMultithreadingEnabled;

        protected int mTickID; // incremented everytime Update() is called.  NOTE: Update() is not necessarily called once per frame.
        protected int mSeed;

        protected UpdateHandler[] mUpdateHandlers;

        // properties
        public int Seed { get { return mSeed; } }
        public int TickID { get { return mTickID; } }
        public int EntityCount { get { return 0; } }
        public bool MultithreadingEnabled { get { return mMultithreadingEnabled; } set { mMultithreadingEnabled = value; } }

        public int MaxEntityCount { get; set; }

		protected EntitySystemBase(string guid) : base(guid, guid.GetHashCode(), 0, 0, 0, 0, 0)
		{
			
		}
		
        public virtual void Update(double elapsedSeconds, IEntitySystem.EntitySystemUpdateContext context)
        {
            // select from mUpdateHandlers based on context... its essentially like update LOD where the
            // update simulation can be simpler when this IEntitySystem is far away or has no players near it...
        }

        public virtual void GenerateSystem()
        {
        }
        // todo: need delegates for handling the Generate()
        // todo: need delegate for Create() of single IProcGeneratedItem


        // libnoise uses this to find a value on a texture
        public virtual object GetValue(double x, double y, double z)
        {
            return null;
        }

        public virtual IProcGeneratedItem GetItem(string address)
        {
            return null;
        }
        public virtual IProcGeneratedItem GetItem(int index)
        {
            return null;
        }

        public virtual IProcGeneratedItem GetItem(string guid, int seed)
        {
            return null;
        }

        public virtual void Read()
        {
        }

        public virtual void Write()
        {
        }

    }

    public class City : EntitySystemBase
    {

        // City specific structs
        private struct Terrain
        {
            public bool Mountainous;
            public bool Landlocked;
            //public Resource[] Resources;			
        }

        private struct Environment
        {
            public float Pollution; // coefficient
            public float WildLife; // diversity coefficient 

        }


        private struct Government
        {
            public int Type;  // 

        }

        private struct Infrastructure
        {
            public bool Highways;
            public bool SeaPorts;
            public bool Airport;
            public bool Railroads;
            public int HousingUnits;

        }

        private struct Economy
        {
            public int Credits;
            //public Store[] Stores;
            //public Resources[] ResourcesRealized;
            //public Resources[] ResourcesUnrealized;
            //public Product[] Commodities;

        }
		
        /*Factories_Light; // produce finished goods
        Factories_Medium;
        Factories_Heavy; 
        Factories_SuperHeavy;
        PowerPlants;
        Mines;
        Farms;
        Fisheries;

        Universities;
        Academies;
        */


        // City specific variables
        private int mOwnerID; // eg FactionID
        private Economy mEconomy;
        private Queue<EntityNode> mBuildQueue;

        private City[] mConnections; // migration, tourism,  trade


        public City(string guid) : base(guid)
        {
        }


    }

    public class Population : EntitySystemBase
    {

        public Population(string guid) : base(guid)
        {
        }


        public override IProcGeneratedItem GetItem(string address)
        {

            return null;
        }

        public override object GetValue(double x, double y, double z)
        {
            throw new NotImplementedException();
        }
        public override IProcGeneratedItem GetItem(int index)
        {
            return null;
        }

        /// <summary>
        /// From the GUID, we can lookup the address (hierarchical region) and then the seed used to generate this Entity
        /// and a "changedState" save file that contains all changed data that is different from the
        /// Entity that is initially created from the seed.
        /// </summary>
        public override IProcGeneratedItem GetItem(string guid, int seed)
        {
            // todo: note the guid must always be assigned if it's being generated from a seed because
            //       a GUID cannot be generated from a seed.  It is always going to be a new GUID if we call guid = System.Guid.NewGuid()

            return null;
        }

        //public IProcGeneratedItem[] GetItems (Rectangle bounds)
        //{
        // todo: note the guid must always be assigned if it's being generated from a seed because
        //       a GUID cannot be generated from a seed.  It is always going to be a new GUID if we call guid = System.Guid.NewGuid()

        //	return null;
        //}

        public override void GenerateSystem()
        {
        }

        public virtual ProcGeneratedItem Create()
        {
            return null;

        }

    }


    public class BoidFactory : Population
    {
        public BoidFactory(string guid) : base(guid)
        {
        }

        public override void Update(double elapsedSeconds, IEntitySystem.EntitySystemUpdateContext context)
        {

        }
    }


    // TODO: these interfaces should support LayeredProcGen chunks
    //       Wave Function Collapse
    //       LibNoise style texture generation, but with more control over generating "chunks"
    //       that connect to each other as opposed to a giant texture that then has to be stiched
    //       together after the fact
    public interface IProcGeneratedItem
    {
        int Seed { get; }
        //public Settings.PropertySpec[] Deltas {get ;}

    }

    public abstract class ProcGeneratedItem : IProcGeneratedItem
    {
        public int mSeed;

        public int Seed { get { return mSeed; } }
    }

    // SEE EntityNode above!  EntityNode is equivalent to Keystone.Entities.Entity
    //public class Entity : ProcGeneratedItem
    //{
    //  public struct LivingEntity
    //  {
    //     // see class Boid "public struct LivingEntity"
    //
    //  }
    //	public Entity (string guid)
    //	{
    //	}
    //}

    public class TerrainChunk : ProcGeneratedItem
    {

    }

    public class ProceduralTexture : IProcGeneratedItem
    {
        public int mSeed;

        public int Seed { get { return mSeed; } }
    }
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





    #region Builder implementation
	// See KeystoneGameBlocks/Game01/Builders
	public struct Builder
    {
		public CONFIGURATION Configuration {get; set;}
		public int UserTypeID {get; set;}
		
		private string mBuildScriptRelativeResourcePath;
		private Dictionary<string, object> mBuildSpecificPropertyValues;
				
		private object mBuildScript;
		private object mComponetScript;
		
		private EntityNode mComponent;
		public EntityNode Component { get {return mComponent;}}
		
		
        public string BuildPersistString {get;}
        private bool mPropertyChanged;
        private bool mBuildChanged;
        private bool mBuildScriptInitialized;
		
		
		public Builder (string buildScriptRelativeResourcePath)
		{
			if (string.IsNullOrEmpty(buildScriptRelativeResourcePath)) throw new ArgumentOutOfRangeException("Builder.ctor() - Build Script relative path cannot be null.");
			mBuildScriptRelativeResourcePath = buildScriptRelativeResourcePath;

			// TODO: Load this Build Script.  This script CAN be shared because the Values of the 
			//       Build properties are stored inm the Component
			//mBuildScript = Repository.Create("Builder", mBuildScriptRelativeResourcePath);
		}
		
		
		//public CSScript BuildScript
		//{
		//	get { return mBuildScript;}
		//}
		
		
		//public CSScript ComponentScript
		//{
		//	get { return if (Component == null) return null; return Component.ComponentScript;}
		//}
		
		
		public void SetProperties(PropertySpec[] properties)
		{
			mPropertyChanged = true;
			mBuildChanged = true;
		}
		
		
		public PropertySpec[] GetProperties(out Dictionary<string, object> buildSpecificPropertyValues)
		{
			// TEMP: These are hard-coded 'build specific' properties for a Battery Power Producer 
			
			
			PropertySpec[] properties;
			
			// based on the CONFIGURATION, retreive the Properties for the various structs used by this CONFIGURATION
			
			
			// Capacity (Watt hours / kJ)
			// Output (aka Max Discharge Rate)
			// Duration (max duration in seconds at Max Discharge Rate)
			// MaxInput (for a Battery, this is maximum Input for recharging purposes)
			
			uint level = 1;
			double capacity = 2000;
			
			double output = 100;
			double duration = 25d;
			double maxInput = 33d;
			double efficiency = 0.92d;
			double throttle = 1.0d;	
			
			buildSpecificPropertyValues = new Dictionary<string, object>();
			buildSpecificPropertyValues.Add("Level", level);
			buildSpecificPropertyValues.Add("Breaker", true);
			buildSpecificPropertyValues.Add("Capacity", capacity);
			buildSpecificPropertyValues.Add("Output", output);
			buildSpecificPropertyValues.Add("Duration", duration);
			buildSpecificPropertyValues.Add("MaxInput", maxInput);
			
			buildSpecificPropertyValues.Add("Efficiency", efficiency);
			buildSpecificPropertyValues.Add("Throttle", throttle);
			
			// todo: we also need to take into account 'Component struct'
			//    - craftsmanship, 
			//    - materials quality
			//    - Ruggedized
			//    - Wear&Tear (Power or Duty Cycles\Takeoff+Land Cycles\etc) 
			//       NOTE: Some stats would need to be FIXED once a design is FINISHED because repairs should never allow for improved Armor or change in Weight, Volume, Surface Area.
			//             So, really it's CAPACITY and or DURATION that needs to be modified when efficiency and/or throttle changes
			// 
			//    - Hitpoints - CurrentHP (damage)
			//    - DR / PassiveDefense
			
			PropertySpec[] buildSpecificProperties = GetProperties_PowerProducer();

			
			// For NON-battery Power Producers like gas generators, reactors, etc
			// FuelType
			// FuelConsumptionRate

			
			return buildSpecificProperties;
		}
				
		private PropertySpec[] GetProperties_Component()
		{
			uint level = 1;
			string fullname = "Battery";
			float craftsmanship = 0.5f;
			float materialQuality = 0.5f;
			bool ruggedized = false;
			bool repairable = true;
				
			double weight = 0;
			double cost = 0;
			double volume = 0;
			double surfaceArea = 0;
			
			PropertySpec[] componentCustomProperties = new PropertySpec[] 
			{
				//public int EntityArrayIndex;
				//public CONFIGURATION Configuration;
				new PropertySpec ("Level", typeof(uint).Name, "component", 1),
				new PropertySpec("Name", typeof(string).Name, "component", (object)fullname),

				
				new PropertySpec ("Material Quality", typeof(float).Name, "component", craftsmanship),
				new PropertySpec ("Craftsmanship", typeof(float).Name, "component", materialQuality),
				new PropertySpec ("Ruggedized", typeof(bool).Name, "component", ruggedized),
				new PropertySpec ("Repairable", typeof(bool).Name, "component", repairable),
			
				/// <summary>
				/// Number of Human (as opposed to software/AI) Operators Required (if 0 then RequiresOperator {get { return NumOperatorsRequired > 0;}}
				///	      
				/// NOTE: if this is a medical bed 1 or 2 might be required.  For instance, the First "operator" is the patient and the Second "operator" is the Medical Professional.  
				///       The second operator isnt always necessary depending on what the first "operator" is doing... if recovering for instance, no second operator is needed.
				///</summary>
				new PropertySpec ("Number of Operators Required", typeof(int).Name, "component", 0),
				
				/// <summary>
				/// The required skills an Operator must have to use this Component
				/// </summary>
//				public Skill[] Skills;

//			    public ExternalArmor Defense; 
//				public InternalStructure Internals; 	

				// stats
				new PropertySpec("Cost", typeof(double).Name, "component", cost),
				new PropertySpec("Weight", typeof(double).Name, "component", weight),
				new PropertySpec("Volume", typeof(double).Name, "component", volume),
				new PropertySpec("SurfaceArea", typeof(double).Name, "component", surfaceArea) // recharging takes significantly longer than discharging at lower technology levels
					
				/*	
				// runtime
					// what about state?  like , waiting for Operator to arrive?
					public int[] OperatorIDs;
					// LivingEntity vs Component both have this mRuntimeFlags but they are unique to each interface because typically LivingEntity and Component structs DO NOT exist within the same Entity.
					// - this could conceivably change in the future if for instance a Cyborg or Robot was also a "Character" that was needed the LivingEntity struct. 
					public uint mUserRuntimeFlags;
					public uint mUserStructFlags;

					public int CurrentHP; // HitPoints - Damage == CurrentHP;


					public float StartTime; // when "Use" began
					public float Duration;  // if the "Use" is of a set Duration, track how long that Duration is... for instance, a sleep duration might be 6 hours of gameTime

					// todo: these bools would go into runtime stats as bitflags
					// along with isPowered, isFueld, isHealthyEnough, hasSkills, isOperatorStatusOK, isInUse(aka isFiring for weapons), canAct (for tacticalStations),
					// isReloading, isUnJamming (isFixingMalfunction), 
					public bool InUse;
					public bool Looping; // Repeating
					public float CooldownDuration; 
				*/
				
				
				
			};
		
			return componentCustomProperties;
		}
		
		private PropertySpec[] GetProperties_PowerProducer()
		{
			
			double capacity = 1000d;
			double output = 100;
			double duration = 0d;
			double maxInput = 50d;
			double efficiency = 0.7d;
			double throttle = .75d;
			
			PropertySpec[] buildSpecificProperties = new PropertySpec[]
			{
				new PropertySpec("Capacity", typeof(double).Name, "build", capacity),
				new PropertySpec("Output", typeof(double).Name, "build", output),
				new PropertySpec("Duration", typeof(double).Name, "build", duration),
				new PropertySpec("MaxInput", typeof(double).Name, "build", maxInput), // recharging takes significantly longer than discharging at lower technology levels
				new PropertySpec("Efficiency", typeof(double).Name, "build", efficiency),
				new PropertySpec("Throttle", typeof(double).Name, "build", throttle)
			};

			return buildSpecificProperties;
		}
		
		
		private PropertySpec[] GetProperties_PowerConsumer()
		{
			bool breaker =            true;  // NOTE: we do not use node.Enabled because that is seperate (for rendering AND updating) from a Component running it's production simulation or not.
			double powerRequirement = 100d;// per tick or per-use if "Continuous == false:
			double minimumPower = 90d;
			bool continuous = true; // whether this component always consumes power when operating, or only when it is "Used" such as a Laser firing for a fixed duration
			bool looping = false; // Repeating
			float performanceSetting = 1.0f;  // 0.0 - 1.0.  We can get rid of HasVariablePerformance if PerformanceSetting >= 0 and <= 1.0
			//bool HasVariablePerformance {get {return (PerformanceSetting >= 0.0f && PerformanceSetting <= 1.0f); }} // can run at reduced power, but with reduced performance (eg sensor will have lower range)
			float duration = 2.0f;          // the length of time that one "Use" takes
			float cooldownDuration = 1.0f;  // the required downtime after the Duration of the previous "use", for the next "use" to be able to occur	
			int priority = 1;  // determines if there's insufficient power production, which consumers get higher priority to be powered during runtime 
			float efficiency = .75f;
			
			PropertySpec[] buildSpecificProperties = new PropertySpec[]
			{	
				new PropertySpec("Breaker", typeof(bool).Name, "runtime", breaker),
				new PropertySpec("PowerRequirement", typeof(double).Name, "build", powerRequirement),
				new PropertySpec("MinimumPower", typeof(double).Name, "build", minimumPower),
				new PropertySpec("Continuous", typeof(bool).Name, "build", continuous),
				new PropertySpec("Looping", typeof(bool).Name, "build", looping),
				new PropertySpec("PerformanceSetting", typeof(double).Name, "build", performanceSetting), // recharging takes significantly longer than discharging at lower technology levels
				new PropertySpec("Efficiency", typeof(double).Name, "build", efficiency),
				new PropertySpec("CooldownDuration", typeof(double).Name, "build", cooldownDuration),
				new PropertySpec("Duration", typeof(double).Name, "build", duration),				
				new PropertySpec("Priority", typeof(double).Name, "build", priority)
			};

			return buildSpecificProperties;
		}
		
		
		private PropertySpec[] GetProperties_Armor()
		{
			PropertySpec[] buildSpecificProperties = new PropertySpec[]
			{
				//new PropertySpec("Level", typeof(uint).Name, "build", level),
				//new PropertySpec("Breaker", typeof(bool).Name, "runtime", breaker),
				//new PropertySpec("Capacity", typeof(double).Name, "build", capacity),
			//	new PropertySpec("Output", typeof(double).Name, "build", output),
				//new PropertySpec("Duration", typeof(double).Name, "build", duration),
				//new PropertySpec("MaxInput", typeof(double).Name, "build", maxInput), // recharging takes significantly longer than discharging at lower technology levels
				//new PropertySpec("Efficiency", typeof(double).Name, "build", duration),
				//new PropertySpec("Throttle", typeof(double).Name, "build", maxInput)		
			};
			
			return buildSpecificProperties;
		}
		
		public void Calculate(EntityNode component)
		{
			if (component == null) throw new ArgumentNullException();
			mComponent = component;
			
			// GET BUILD SPECIFIC PROPERTIES FROM OUR INSTANCED BUILD SCRIPT... for now its just hardcoded
			// SPECIFIC TO OUR 'BATTERY' PowerProducer 
			// --------------------------------------------------------------------------------------------
			Dictionary<string, object> buildSpecificPropertyValues = mBuildSpecificPropertyValues;
			// the following line should be getting these from the BuildObjectScript
			PropertySpec[] buildSpecificProperties = GetProperties(out buildSpecificPropertyValues);
			
			// retrieve some of the Component's properties such as Level to help compute
			// the build stats. Or should "level" exist only in the Build stats?
			//PropertySpec[] componentProperties = Component.GetCustomProperties(true);
			uint level = (uint)buildSpecificPropertyValues["Level"];
			bool breaker = (bool)buildSpecificPropertyValues["Breaker"];
			double capacity = (double)buildSpecificPropertyValues["Capacity"];
			double output = (double)buildSpecificPropertyValues["Output"];
			double duration = (double)buildSpecificPropertyValues["Duration"];
			double maxInput = (double)buildSpecificPropertyValues["MaxInput"];
			//double efficiency = (double)buildSpecificPropertyValues["Efficiency"];
			//double throttle = (double)buildSpecificPropertyValues["Throttle"];
			
			// todo: we also need to take into account 'Component struct'
			//    - craftsmanship, 
			//    - materials quality
			//    - Hitpoints - CurrentHP (damage)
			//    - Wear&Tear (Power or Duty Cycles\Takeoff+Land Cycles\etc) 
			//       NOTE: Some stats would need to be FIXED once a design is FINISHED because repairs should never allow for improved Armor or change in Weight, Volume, Surface Area.
			//             So, really it's CAPACITY and or DURATION that needs to be modified when efficiency and/or throttle changes
			// 
         
			
			// ASSIGN the build props and values to a set of PropertySpec and to it's Entity
			buildSpecificProperties = new PropertySpec[] 
			{
				new PropertySpec("Level", typeof(uint).Name, "build", level),
				new PropertySpec("Capacity", typeof(double).Name, "build", capacity),
				new PropertySpec("Output", typeof(double).Name, "build", output),
				new PropertySpec("Duration", typeof(double).Name, "build", duration),
				new PropertySpec("MaxInput", typeof(double).Name, "build", maxInput) 
			};

			

			// GET COMPONENT SPECIFIC PROPERTIES FROM OUR INSTANCED CLIENT ENTITY SCRIPT... for now its just hardcoded
			// --------------------------------------------------------------------------------------------
			double cost = 0d;
			double weight = 0d;
			double volume = 0d;
			double surfaceArea = 0d;
						
			// compute stats for cost, weight, volume, surface area, 
			cost = level * 10d;
			weight = level * 1d;
			volume = level * 1d;
			surfaceArea = level * 0.25d;
			
			// assign the computed values to a set of PropertySpec and to it's Entity
			PropertySpec[] componentCustomProperties = new PropertySpec[] 
			{
				new PropertySpec("Cost", typeof(double).Name, "component", cost),
				new PropertySpec("Weight", typeof(double).Name, "component", weight),
				new PropertySpec("Volume", typeof(double).Name, "component", volume),
				new PropertySpec("SurfaceArea", typeof(double).Name, "component", surfaceArea) // recharging takes significantly longer than discharging at lower technology levels
			};
			
			
			Dictionary<string, object> componentCustomPropertyValues = new Dictionary<string, object>();
			componentCustomPropertyValues.Add("Cost", cost);
			componentCustomPropertyValues.Add("Weight", weight);
			componentCustomPropertyValues.Add("Volume", volume);
			componentCustomPropertyValues.Add("SurfaceArea", surfaceArea);
			
			// NOTE: this should result in the Memory<T> records being updated for the 'Battery' component
			//       and specifically it's 'struct PowerProducer' internally within Entity... that means the
			//       CustomProperties must know which interfaces to use for each property
			mComponent.SetCustomProperties(buildSpecificProperties);
			mComponent.SetCustomProperties(componentCustomProperties);
		}
		
		
        public override string ToString()
        {
            // NOTE: we only need to write out the build parameters and from that we can
            //       reconstitute the full entity

			// 1 - Memory<T> represents how the data is STORED in memory, in structs from which we can
			//     store in contiguous memory
			// 2 - So, a Laser will be made up of 3 "structs" like Component, Weapon and Laser for storing the data
			//    and these structs will co-exist in our UserData object keyed by their typename
			//    The Defense and InternalStructure too can be keyed this way and assigned later... with ArmorLayers being
			//    somewhat special case because there are currently no maximum allowable limits

			Dictionary<string, object> buildSpecificPropertyValues = mBuildSpecificPropertyValues;
			PropertySpec[] buildSpecificProperties = GetProperties(out buildSpecificPropertyValues);
			
			// JSon == javascript object notation
			var options = new System.Text.Json.JsonSerializerOptions 
			{ 
    			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull    |
					                     System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
				WriteIndented = true
			};
			
			string jsonString = System.Text.Json.JsonSerializer.Serialize(buildSpecificProperties, options);
			Console.WriteLine("Builder.ToString() - SERIALIZE = " + jsonString);
			
			//Being Compression + Base65 encoding
			string compressedBase64 = Convert.ToBase64String(Utils.CompressWithBrotli(System.Text.Encoding.UTF8.GetBytes(jsonString)));
			string decompressedBase64 = System.Text.Encoding.UTF8.GetString(Utils.DecompressWithBrotli(Convert.FromBase64String(compressedBase64)));
			jsonString = decompressedBase64;
			// End Base64 decoding and Decompression
			
			buildSpecificProperties = System.Text.Json.JsonSerializer.Deserialize<PropertySpec[]>(jsonString);
			
			Console.WriteLine("Builder.ToString() - DESERIALIZE count = " + buildSpecificProperties.Length.ToString());
			Console.WriteLine("Build.ToString() - COMPLETED.");
            return jsonString;
		}
#endregion
	}
	



	////////////////////////////////////////////////////////////////////////////////////////////////
    // DATA PROCESSORS, USER DATA STORE and COMPONENT STORES 
#if USE_MEMORY_T
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

        /// // An array oof Memory<object> to hold different types
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
        public delegate void Processor<T>(ComponentStore<T> store, object[] parameters, int seed, GameTime gt);

        private ComponentStoreCollection mComponentStoreCollection;

        // TODO: there are some types of data processing where an Entity is always added... such as 
        //       currently when movement/flocking is computed because a "STEER" acceleration/force PRODUCTION
        //       is required every frame.
        //       HOWEVER, there are plenty of cases where an Entity would only be added if production was
        //       occuring such as a CHAIR producing +morale or -fatigue or +health but only when an
        //       OPERATOR was USING it.  


        //private Keystone.Scene.Scene mScene;
        //private ComponentStore<T>[] mStores;

        /// <summary>
        /// Memory<T>[] contains arrays of data for each interface needed by the DataProcessor
        /// </summary>
        // private DataProcessor<IScene scene, Memory<T> data, object parameters> mDataProcessors;
        // we will need to cast the 'object' param to the appropriate DataProcessor 
        private Dictionary<string, object> mProcessors;


        public DataProcessorsStore(ComponentStoreCollection col)
        {
            mComponentStoreCollection = col;
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
        public void Add<T>(string name, Processor<T> proc)
        {
            mProcessors.Add(name, proc);

            // this class probably needs to reside in Core.cs where it gets
            // called by Simulation.DataProcessor.Update(); followed by
            // i();
            //
            // API needs call to add DataProcessor instances to this class

        }


        public void Update(GameTime gt, EntityNode[] entities)
        {
            foreach (string key in mProcessors.Keys)
            {
				try
				{
                var func = mProcessors[key];
				int seed = 0;
			
                object[] args = GetParameters(key);
				//Console.WriteLine("Processor.Update() - Key == " + key);
				
				// cast processors of type 'object' to the appropriate type we need for this processor (based on the name of it's key)
				// note: we could probably check it's GetType() instead... but not necessary for now
				switch (key)
				{
					case "OPTICAL_SENSING":
						Processor<Transform.Transform_Struct> opticalSensing = (Processor<Transform.Transform_Struct>)func;
                        ComponentStore<Transform.Transform_Struct> storeForOptical = mComponentStoreCollection.CheckOut<Transform.Transform_Struct>(0);
  		                opticalSensing.Invoke(storeForOptical, args, seed, gt);
						break;
					case "FLOCKING":
						Processor<Transform.Transform_Struct> flocking = (Processor<Transform.Transform_Struct>)func;
                        ComponentStore<Transform.Transform_Struct> store0 = mComponentStoreCollection.CheckOut<Transform.Transform_Struct>(0);
  		                flocking.Invoke(store0, args, seed, gt);
						break;
					case "LIFECYCLE":
						Processor<LifeForm> life = (Processor<LifeForm>)func;
				   		ComponentStore<LifeForm> store1 = mComponentStoreCollection.CheckOut<LifeForm>(0);
 						life.Invoke(store1, args, seed, gt);
						break;
					case "LASERS":
						Processor<Laser_Struct> lazer = (Processor<Laser_Struct>)func;
				   		ComponentStore<Laser_Struct> storeLasers = mComponentStoreCollection.CheckOut<Laser_Struct>(0);
 						lazer.Invoke(storeLasers, args, seed, gt);
						break;
					case "POWER_CONSUMPTION": 
						uint productID = (uint)PRODUCTS.ElectricalPower;
						Processor<Consumption> powerConsumption = (Processor<Consumption>)func;
						ComponentStore<Consumption> storePowerConsumption = mComponentStoreCollection.CheckOut<Consumption>(0, (int)productID);
						powerConsumption.Invoke(storePowerConsumption, args, seed, gt);
						break;

						
						
					//case "LASER_IMPALING_DAMAGE":
					//	Processor<BoidSimulation.ImpalingDamage> laserImpalingDamage = (Processor<BoidSimulation.ImpalingDamage>)func;
				    //	ComponentStore<BoidSimulation.ImpalingDamage> storeLaserImpalingDamage = mComponentStoreCollection.CheckOut<BoidSimulation.ImpalingDamage>(0);
 					//  laserImpalingDamage.Invoke(storeLaserImpalingDamage, args, seed, gt);
					//	break;
					default:
						throw new NotImplementedException("DataProcessorsStore() - Update() - key '" + key + "' not supported.");
				}
				}
				catch (Exception ex)
				{
					Console.WriteLine("DataProcessorsStore() - Update() - ERROR with processing at key '" + key + "' - " + ex.Message);
				}
				
                

                // NOTE: For intrinsic interfaces at least, we need to set changeFlags on the Entities
                // for mIsDirty updates to Matrices and BoundingBox.

                // TODO: SetChangeFlags() must be called... so i think we need a delegate/function pointer to be
                // stored in our Memory<T> for that interface.  Or we need to iterate at end through all active Entities that
                // changeFlags flags = ChangeFlags.BoundingBoxDirty | ChangeFlags.TranslationDirty | ChangeFlags.MatriDirty
                // were modified and call Entity[i].SetChangeFlags(flags)
            }
        }


        private object[] GetParameters(string key)
        {
            object[] result = null;

            // all parameters are tracked in KeyCommon.UserData
            // TODO: temporary switch to grab the correct parameters from KeyCommon.UserData.
            switch (key)
            {
				case "LIFECYCLE": 
				    result = new object[3];
				    result[0] = EntryClass.HEIGHT;
				    result[1] = EntryClass.WIDTH;
				    result[2] =
				    EntryClass.DEPTH;
                    break;
				case "OPTICAL_SENSING":
					result = new object[8];
					result[0] = EntryClass.SEPERATION_DISTANCE;
					result[1] = EntryClass.ALIGNMENT_DISTANCE;
					result[2] = EntryClass.COHESION_DISTANCE;
					result[3] = EntryClass.SEPARATION_FACTOR;
					result[4] = EntryClass.ALIGNMENT_FACTOR;
					result[5] = EntryClass.COHESION_FACTOR;
					result[6] = EntryClass.TURN_FACTOR; // For boundary avoidance
					result[7] = EntryClass.MAX_SPEED;
                    break;
                case "FLOCKING":
					result = new object[8];
					result[0] = EntryClass.SEPERATION_DISTANCE;
					result[1] = EntryClass.ALIGNMENT_DISTANCE;
					result[2] = EntryClass.COHESION_DISTANCE;
					result[3] = EntryClass.SEPARATION_FACTOR;
					result[4] = EntryClass.ALIGNMENT_FACTOR;
					result[5] = EntryClass.COHESION_FACTOR;
					result[6] = EntryClass.TURN_FACTOR; // For boundary avoidance
					result[7] = EntryClass.MAX_SPEED;
                    break;
					
                case "STEER":
                    break;
                case "COLLIDE":
                    break;
				case "LASERS":
					break;
				case "LASER_IMPALING_DAMAGE":
					break;
					
				case "POWER_CONSUMPTION": 
					result = new object[2];
					result[0] = (uint)PRODUCTS.ElectricalPower;
					result[1] = EntryClass.bSim.mProduction[(int)PRODUCTS.ElectricalPower];  // dictionary key into mProduction[key] returns a ComponentStore<Production>
					
					break;
					
                default:
                    throw new NotImplementedException("DataProcessors.GetParameters() - No store for key '" + key + "'");
            }

            return result;

        }
    } // DataProcessorsStore

	
    /// <summary>
    /// ComponentStoreCollection allows for the CheckIn() and CheckOut() of 
    /// ComponentStore<T> which is a wrapper around the System.Memory.Memory<T> 
    /// class.  
    /// This StoreCollection object will host ComponentStores<T> for both 
    /// Intrinsic and UserComponents
    /// </summary>
    public class ComponentStoreCollection : IDisposable
    {
        //private System.Collections.Concurrent.ConcurrentDictionary<Type, object> mUserComponentsCollection;
		
		private System.Collections.Concurrent.ConcurrentDictionary<int, object> mUserComponentsCollection;
		private static System.Threading.SemaphoreSlim mSlim = new System.Threading.SemaphoreSlim(1);
				
		
        public ComponentStoreCollection()
        {
            //mUserComponentsCollection = new System.Collections.Concurrent.ConcurrentDictionary<Type, object>();
        	mUserComponentsCollection = new System.Collections.Concurrent.ConcurrentDictionary<int, object>();
		}
		
		// todo: i would need to pass in a userTypeID and perhaps classification, category, and configuration
        public ComponentStore<T> CheckOut<T>(uint size = 64, int productID = -1)
        {
			try 
			{
				mSlim.Wait(-1); // wait parameter is in milliseconds to Wait, BUT -1 means wait indefinetely
				// April.5.2026 - switched to "int" Dictionary<> key, away from 'Type' a a key.
				// Google AI Overview says that the hashcode calculation for a Type is more expensive than an
				// integer which is vertually 0 since the returned value is the integer value itself.
				int hashCode = GetHashCode<T>(productID);
				
				ComponentStore<T> store = (ComponentStore<T>) mUserComponentsCollection.GetOrAdd(hashCode, result =>  new ComponentStore<T>(size));
				
				// Feb.13.2026 - switched to ConcurrentDictionary<>
				//ComponentStore<T> store = (ComponentStore<T>) mUserComponentsCollection.GetOrAdd(typeof(T), result =>  new ComponentStore<T>(size));
								
				//object value;
				//bool success = mUserComponentsCollection.TryGetValue(typeof(T), out value);
				//if (success)
				//    return (ComponentStore<T>)value; // throw new Exception("ComponentStoreCollection.CheckOut() - Dictionary Key Already Exists.");

				//mUserComponentsCollection.Add(typeof(T), store);
				return store;
			}
			finally
			{
				mSlim.Release();
				//Console.WriteLine ("ComponentStore.CheckOut() - Completed " + typeof(T).ToString());
			}
        }
				
        public void CheckIn<T>(object store, int productID = -1)
        {
			try 
			{
				mSlim.Wait(-1);  // wait parameter is in milliseconds to Wait, BUT -1 means wait indefinetely
				
				object existing;
				
				int hashCode = GetHashCode<T>(productID);
				
				bool result = mUserComponentsCollection.TryRemove(hashCode, out existing);

				//System.Diagnotistics.Debug.Assert (result == true && existing == store, "ComponentStoreCollection.CheckIn()  - Dictionary item does not exist.");

				/*
				if (store == null) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - Dictionary is NULL.");

				object value;
				bool success = mUserComponentsCollection.TryGetValue(type.GetType(), out value);

				if (!success) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - ComponentStore for Type '" + typeof(T).Name + " ' is NULL.");

				mUserComponentsCollection.Remove(type.GetType());
				//value.Dispose();
				*/
			}
			finally
			{
				mSlim.Release();
			}
        }
        
		private int GetHashCode<T>(int productID = -1)
		{
			int hashCode = typeof(T).GetHashCode();
				
			if (productID != -1)
				hashCode = HashCode.Combine(hashCode, productID);  // <-- preferred method it seems...
			// hashCode = (hashCode, productID).GetHashCode();  // <-- below method supposedly uses no heap allocations
			
			return hashCode;
			
		}
		
        bool mIsDisposed;
        
        public void Dispose()
        {
            if (!mIsDisposed)
            {
                 
				foreach(object obj in mUserComponentsCollection.Values)
				{
					((IDisposable)obj).Dispose();
				}

				mIsDisposed= true;
				mUserComponentsCollection=null;
				Console.WriteLine("ComponentStoreCollection.~dtor() - " + this.GetType().ToString() + " Disposed.");
            }
            
        }
    } // ComponentStoreCollection.cs

	
    ///<summary>
    /// Components are essentially data stores for Intrinsic or User game objects.
    /// They are always stored as struct within contiguous Memory<T> for
    /// fast processing of their data.
    ///</summary>
    public class ComponentStore<T> :IDisposable
    {
        private uint STARTING_SIZE = 64; // todo: rename _SIZE to _COUNT to make it clear this is number of records not size in bytes
        private const uint MIN_SIZE = 64;
        private const uint MAX_SIZE = 4096; // number of Records  (eg records of Transform_Struct), not bytes
        private uint EXPAND_INCREMENT = MIN_SIZE; // expand by this amount when needed.  if 0, it will double the size of Components
        
		private bool mDoubleBufferEnabled = false;
		
		private uint mRecordCount = 0;  // should equal (Size - mAvailableForCheckOut.Count)
		
		// NOTE: there is no System.Collections.Concurrent.ConcurrentList<>
		private Memory<T> Components;
		private Memory<T> DoubleBuffer;
		
		private Stack<int> mAvailableForCheckOut;
		private bool[] InUse;       

        private object mSync;
		private static System.Threading.SemaphoreSlim mSlim = new System.Threading.SemaphoreSlim(1);
		
        private Dictionary<string, bool[]> mViews;
		
        /*Span<T> in C# is a value type that provides a safe and efficient way to work with 
        contiguous regions of memory, whether that memory is managed (like an array on the 
        heap), unmanaged, or allocated on the stack. Despite being a value type, Span<T> 
        does not change the underlying memory itself; rather, it provides a view into that 
        memory, allowing you to read from or write to it directly.

        Here's how it works: 

        View, Not Ownership:

        Span<T> does not own the memory it points to. It's essentially a lightweight 
        structure containing a reference (or pointer) to the start of a memory region 
        and a length. When you create a Span<T> from an array, for instance, it 
        doesn't copy the array data; it simply creates a view that allows you to 
        access a portion of that existing array.

        Direct Access:

        Because Span<T> holds a reference to the underlying memory, any modifications 
        made through the Span<T> directly affect that original memory. For example, 
        if you have an array myArray and create a Span<int> mySpan = myArray;, then 
        mySpan[0] = 10; will change the value of myArray[0] in the original array.

        No Memory Allocation (for the data):

        When you create a Span<T>, you are not allocating new memory for the data 
        itself. You are only allocating the Span<T> struct on the stack, which is a 
        very small and efficient operation. This is a key reason for Span<T>'s 
        performance benefits, as it avoids heap allocations and associated garbage 
        collection overhead.

        Immutability of the Span (not the data):

        While Span<T> allows you to modify the underlying data, the Span<T> itself 
        is immutable in terms of its range. You cannot change the starting address 
        or the length of an existing Span<T> instance. If you need a different 
        view of the same or another memory region, you create a new Span<T> 
        instance (e.g., through slicing).

        In essence, Span<T> provides a highly efficient and safe mechanism to 
        interact with existing memory buffers without incurring the costs of copying 
        data or managing memory ownership. It acts as a direct conduit to the 
        underlying data, allowing for in-place modifications when desired.
        */
        public ComponentStore(bool doubleBufferEnabled = false) : this(64, doubleBufferEnabled)
        {
        }

        public ComponentStore(uint size, bool doubleBufferEnabled = false)
        {
            STARTING_SIZE = size;
			mDoubleBufferEnabled = doubleBufferEnabled;
			
            mSync = new object();
						
			mAvailableForCheckOut = new Stack<int>();
			
			Expand();
            
			//long totalAllocated = Utils.GetTotalAllocatedBytes(false);
			//Console.WriteLine("ComponentStore.ctor() - " + totalAllocated.ToString() + " allocated.");
			
			long totalUsed = Utils.GetUsedMemory(false);
			//Console.WriteLine("ComponentStore.ctor() - " + Utils.SizeSuffix(totalUsed) + " used.");

			Console.WriteLine( "ComponentStore.ctor() - Type == '" + (typeof(T)).ToString() + " Starting capacity == " + Capacity.ToString());
        }

		private uint mCapacity;
		
		/// <summary>
		/// The maximum number of records this Store can hold before it needs to be expanded.
		/// </summary>
        public uint Capacity { get { return mCapacity; } }

		/// <summary>
		/// The currrent number of records this Store is holding.  This number
		/// cannot exceed the 'Capacity' value.
		/// </summary>
		public uint Count { 
			get 
			{ 
				try
				{
					//mSlim.Wait(-1); // NOTE: This needs to be synchronized because if access from outside, for say, determining the number of iterations of a loop
					                // then this may fail the below Debug.Assert() without sychronization
					
					int availableCount = 0;
					if (mAvailableForCheckOut != null)
						availableCount = mAvailableForCheckOut.Count;

					int  tmp = (int)mCapacity - availableCount;
					//Console.WriteLine("ComponentStore.Count - Capcity (" + Capacity.ToString() + ") - Available(" + availableCount.ToString() + ") == " + tmp.ToString());
					string output = "ComponentStore.Count - RecordCount (" + mRecordCount.ToString() + ") == Capacity (" + mCapacity.ToString() + ") - AvailableCount (" + availableCount.ToString() + ")";
					//Console.WriteLine(output);
			//		System.Diagnostics.Debug.Assert (mRecordCount == tmp, output);
					return mRecordCount;
				}
				finally
				{
					//mSlim.Release();
				}
			}
		}
		
        public Span<T> Span { get { return Components.Span; } }
        
		public Span<T> SpanReadWrite {get {return DoubleBuffer.Span;}}
		
		public bool DoubleBufferEnabled {get {return mDoubleBufferEnabled;}}
		
        public ReadOnlySpan<T> Copy()
        {
            lock (mSync)
            {
                ReadOnlySpan<T> result = Components.Span;
                return result;
            }
        }
		
        // GameAPI will need commands for checking in/out via our Entity script initializations, 
        // the types made here in our ComponentStore
        // So for instance, if "EnergyWeapon.cs" on Initialize()
        // will register "Weapon" and "EnergyWeapon" interfaces.
        // Recall that Initialize() is only called ONCE PER SCRIPT whereas Initialize_Entity
        // is called per Entity that is using that script.
        // Initialize_Entity() will then call CheckOut(typeof(Weapon)) and CheckOut(typeOf(EnergyWeapon))
        // to get direct memory access to the Memory<T> where variables associated with those interfaces
        // will get stored.
		/// <summary>
		/// This CheckOut() call currently retreives only a single record from the Components Memory<T> 
		/// and returns it as a new Memory<T> that points to that single record
		/// </summary>
        public Memory<T> CheckOut(out int index) // aka: MemoryPool<T>.Rent() 
        {
            //lock (mSync)
			try
			{
				mSlim.Wait(-1);  // wait parameter is in milliseconds to Wait, BUT -1 means wait indefinetely
				{
					const int HOW_MANY = 1;
					index = -1;
					try
					{
						try
						{
							if (Components.Equals(null))
								Expand();
						}
						catch (Exception ex)
						{
							//Console.WriteLine("ComponentStore.CheckOut() - line 1" + ex.Message);
						}
						
						// using stack<int> of available indices
						if (mAvailableForCheckOut.Count > 0)
						{
							mRecordCount++;
							int i = mAvailableForCheckOut.Pop();
							
							//uint tmp = Count;
							try
							{
								InUse[i] = true;
							}
							catch (Exception ex)
							{
								//Console.WriteLine("ComponentStore.CheckOut() - i == " + i + " InUse[i] == " + InUse[i] + " - " + ex.Message);
							}
							
							index = i;
							return Components.Slice(index, HOW_MANY);
						}

						// NOTE: we start searching from mLastCheckOutIndex + 1 otherwise
						//       finding an available slot is very slow.  This works great
						//       but when we also start to CheckIn() items, we need to maintain
						//       a list of those as well.  
						//       In fact, all we need is to initially create a stack<> of available
						//       generated by adding initially all indices from bottom to top so that
						//       we grab from the top first.  Then any item's that are "CheckIn" get 
						//       their indices added back to the stack.
						//for (int i = mLastCheckOutIndex + 1; i < Components.Length; i++)
						//    if (!InUse[i])
						//    {
						//        InUse[i] = true;
						//        mLastCheckOutIndex = i;
						//        return Components.Slice(i, HOW_MANY);    
						//    }

						// if still here, we need to expand first
						Expand();
						if (mAvailableForCheckOut.Count > 0)
						{
							mRecordCount++;
							int i = mAvailableForCheckOut.Pop();
							
							//uint tmp = Count;
							try
							{
								InUse[i] = true;
							}
							catch (Exception ex)
							{
								//Console.WriteLine("ComponentStore.CheckOut() - i == " + i + " InUse[i] == " + InUse[i] + " - " + ex.Message);
							}
							
							index = i;
							return Components.Slice(index, HOW_MANY);
						}
						else 
						{
							Console.WriteLine("CheckOut() - THIS SHOULD NOT HAPPEN.");
						}
						return null;
						//return CheckOut(out index);
					}
					catch (Exception ex)
					{
						//Console.WriteLine("ComponentStore.Checkout()" + ex.Message);
						return null;
					}
				}
			}
			finally
			{
				mSlim.Release();
			}
        }

        public void CheckIn(Memory<T> mem)
        {
            lock (mSync)
            {
                // find the index of this mem being checked In
                for (int i = 0; i < Components.Length; i++)
                    if (!InUse[i] && (mem.Equals(this.Components.Slice(i, 1))))
                    {
                        InUse[i] = false;
						
						//CheckIn(); 
						
                        mAvailableForCheckOut.Push(i);
						mRecordCount--;
                        return;
                        // todo: Components.Span[i] = default(T);    
                    }
            }
        }
		
		
		
		public void RemoveView(string viewName)
        {
            if (mViews == null) throw new Exception("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");
            bool[] view;
            if (!mViews.TryGetValue(viewName, out view)) throw new Exception("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");

            mViews.Remove(viewName);
        }

		public int FindIndex (Predicate<T> match)
		{
			try 
			{
				mSlim.Wait(-1);
				
				if (mRecordCount == 0) return -1;

				for (int i = 0; i < mRecordCount; i++)
					if (match(this.Span[i]))
						return i;

				return -1;
			}
			finally
			{
				mSlim.Release();
			}
		}
		
		public T Find (Predicate<T> match)
		{
			try 
			{
				mSlim.Wait(-1);
				
				T found = default(T);
				if (mRecordCount == 0) return found;

				for (int i = 0; i < mRecordCount; i++)
					if (match(this.Span[i]))
						return this.Span[i];
								
				return found;
				
			}
			finally
			{
				mSlim.Release();
			}
		}
		
		public List<T> FindAll (Predicate<T> match)
		{
			try 
			{
				mSlim.Wait(-1);
				
				if (mRecordCount == 0) return null;
				List<T> found = new List<T>();

				for (int i = 0; i < mRecordCount; i++)
					if (match(this.Span[i]))
						found.Add( this.Span[i]);

				return found;
			}
			finally
			{
				mSlim.Release();
			}
		}
		
		
        public void CreateView(string viewName)
        {
            if (mViews == null)
                mViews = new Dictionary<string, bool[]>();

            bool[] v;
            if (mViews.TryGetValue(viewName, out v)) throw new Exception("ComponentStore.CreateView() - A View with name '" + viewName + "' already exists.");

            // By default, all indices start off as enabled
            bool[] indices = new bool[Components.Length];
            for (int i = 0; i < Components.Length; i++)
                indices[i] = true;

            mViews.Add(viewName, indices);
            //mViews[viewName] = indices;
        }

        public void AddIndicesToView(string viewName, int enabledIndex)
        {
            AddIndicesToView(viewName, new int[] { enabledIndex });
        }

        public void AddIndicesToView(string viewName, int[] enabledIndices)
        {
            bool[] v;
            if (!mViews.TryGetValue(viewName, out v)) throw new Exception("ComponentStore.AddIndicesToView() - A View with name '" + viewName + "' does NOT exist.");
            bool[] results = mViews[viewName];

            int length = Components.Length;

            // enable all indices specified in the enabledIndices argument
            for (int i = 0; i < enabledIndices.Length; i++)
                if (enabledIndices[i] < length)
                    results[enabledIndices[i]] = true;

            mViews[viewName] = results;
            //mViews[viewName] = Helpers.ArrayExtensions.ArrayAppendRange(mViews[mViewName], enabledIndices);
        }

        public void RemoveIndicesFromView(string viewName, int[] disabledIndices)
        {
            bool[] v;
            if (!mViews.TryGetValue(viewName, out v)) throw new Exception("ComponentStore.AddIndicesToView() - A View with name '" + viewName + "' does NOT exist.");
            bool[] results = mViews[viewName];

            int length = Components.Length;

            // disable all indices not specified in the enabledIndices argument
            for (int i = 0; i < disabledIndices.Length; i++)
                if (disabledIndices[i] < length)
                    results[disabledIndices[i]] = true;

            mViews[viewName] = results;
        }

        /// <summary>
        /// Returns a list of indices indicating which elemements in Memory<T> 
        /// exist in a View with the name "viewName"
        public bool[] GetView(string viewName)
        {
            bool[] results;
            bool success = mViews.TryGetValue(viewName, out results);
            if (success) return results;

            throw new Exception("ComponentStore.GetView() - ERROR: View '" + viewName + "' not found.");
        }

        // TODO: script initialization will grab/checkout the arrayElements it needs
        //       script destructors need to checkin / dispose all array arrayElements
        private void Expand()
        {
			Console.WriteLine("ComponentStore.Expand() - Current Capacity == " + Capacity.ToString() + " for type '" + typeof(T).Name + "'" );
            if (InUse == null)
            {
                Components = new T[STARTING_SIZE];
                InUse = new bool[STARTING_SIZE];
                mAvailableForCheckOut = new Stack<int>();
				mRecordCount = 0;
				mCapacity = (uint)Components.Length;
				
				// this is a stack which is Last In, First Out so we want to
				// have the lowest indices at the top of the stack (last)
				// and the large indices at the bottom (first)
                for (int i = (int)STARTING_SIZE - 1; i >= 0; i--)
	                mAvailableForCheckOut.Push(i);

				uint abc = STARTING_SIZE;
				Console.WriteLine("Expand() - " + typeof(T).Name + " " +  abc.ToString());
                return;
            }

            int newSize = (int)(Capacity + EXPAND_INCREMENT);
            //if (EXPAND_INCREMENT == 0)
                newSize = (int)Capacity * 2;

            T[] data = new T[newSize];
            //Components.Span[0].CopyTo(data.AsSpan());

            // hack - copy components to temporary array first since i can't get 
            // MemoryExtensin.CopyTo() working at the moment
            T[] tmp = Components.ToArray();
            tmp.CopyTo(data, 0);

            //MemoryExtensions.CopyTo<T>(Components.ToArray(), data);

            Components = new Memory<T>(data);
			mCapacity = (uint)Components.Length;
			
			//Console.WriteLine("ComponentStore.Expand() - New Capacity == " + Capacity.ToString() + " for type '" + Components.GetType().Name + "'" );
			
            bool[] newInUse = new bool[newSize];
            InUse.CopyTo(newInUse, 0);
            InUse = newInUse;

            // create a new mAvailableForCheckOut stack using the new InUse[] array
			//recall: this is a stack which is Last In, First Out so we want to
			// have the lowest indices at the top of the stack (last)
			// and the large indices at the bottom (first)
            Stack<int> tmpStack = new Stack<int>(newSize);
            for (int i = (int)newSize - 1; i >= 0; i--)
			{
	        	System.Diagnostics.Debug.Assert (InUse.Length == newSize, "InUse Length == " + InUse.Length.ToString() + " newSize == " + newSize.ToString());
				if (!InUse[i])
					tmpStack.Push(i);
			}
			
            mAvailableForCheckOut = tmpStack;
            ExpandViews(newSize);
        }

        private void ExpandViews(int newSize)
        {
            if (mViews == null) 
			{
				return; // NOTE: most likely this is not an error, we just aren't using any views
			}
			
            foreach (var key in mViews.Keys)
            {
                bool[] indices = mViews[key];

                bool[] newInUse = new bool[newSize];
                indices.CopyTo(newInUse, 0);

                int diff = newSize - indices.Length;
                // if it's decreased in size no need to assign true or false
                if (diff <= 0) return;

                for (int i = indices.Length - 1; i < newSize; i++)
                    indices[i] = true;

                // assign the new expanded view
                mViews[key] = indices;
            }
        }
        
        bool mIsDisposed;
        
        public void Dispose()
        {
            if (!mIsDisposed)
            {
				for(int i = 0; i <  Components.Length;i++)
				{
					//ComponentStore<T> store = (ComponentStore<T>)
					CheckIn (Components.Slice(i, 1));
				}

				mIsDisposed = true;
				Components = null; 
				
				Console.WriteLine("ComponentStore.~dtor() - " + this.GetType().ToString() + " Disposed.");
            }
        }

    } // ComponentStore.cs


#endif



#region USER DATASTORE AND USERDATA 
	/// <summary>
	/// Stores ALL UserData objects for all loaded Entities.  
	/// This is necessary so that our DataProcessors can grab the appropriate
	/// parameters required for a DataProcessor delegate, for all Entities/Components
	/// that are being processed.
	/// March.22.2026 - UserDataStore can be used for our overall CONTEXT that will allow us to access
	/// custom data for Entities as well as data for Policies like Rules of Engagement, Directives, etc.
	/// We simply need the EntityID and Key or the PolicyName and Key.  The underlying data within each
	/// Entity's 'UserData' instance, can be stored in Lists<T> for our various types of data as it is now,
	/// OR we can implement a Memory<byte> mBuffer;  that holds most data including arrays for numeric types
	/// We simply need a Dictionary<key, Tuple<start, type, length>> and we can maybe use the Types we have
	/// for KeyCommon.Helpers.ExtensionMethods  though that entire class should be moved and renamed to 
	/// something like KeyCommon.IO.DataHelper.  It has methods for reading/writing our common Keystone.Types
	/// to/from XML, to/from our PropertySpecs, and to/from our Lidgren.NetBuffer, and we'd add functionality
	/// for read/write to/from our Memory<byte> 
	/// </summary>
	public class UserDataStore : IDisposable
	{
		private System.Collections.Concurrent.ConcurrentDictionary<string, UserData> mUserDataCollection; // Dictionary<string, UserData> mUserDataCollection;
		
		
		
		public UserDataStore()
		{
		    //mUserDataCollection = new Dictionary<string, UserData>();
			mUserDataCollection = new System.Collections.Concurrent.ConcurrentDictionary<string, UserData>();
		}

		public UserData this[string entityID]
		{
			get 
			{				
				UserData d;
				bool success = mUserDataCollection.TryGetValue(entityID, out d);
				if (success) return d;
				
				return null;
			}
		}
		
		
		// TODO: currently Entity.BlackBoardData is being assigned externally to entityID
		//       which is just fine, but now we need to grab it from KeyCommon.Data.UserDataStore.CheckOut(entityID);
		// TODO: We also need to make sure when an Entity is detached from the Scene, CheckIn(entity.ID, entity.BlackBoardData) is called.
		// August.18.2025 - WWG -  this change is being made because we need to be able to pass all BlackBoardData for all Entities 
		//                         so that rules processors for Memory<T> will have access to that BlackBoardData which can contain
		//                         parameters required by the various rules processors in order to adequately process the data for each Entity 
		//                         given the current rule being ran.
		public UserData CheckOut(string entityID)
		{
		    bool success = mUserDataCollection.ContainsKey(entityID);		    
		    if (success) throw new Exception ("UserDataStore.ctor() - Dictionary Key '" + entityID + "' Already Exists.");
		    
		    UserData data = new UserData();
		    
		    mUserDataCollection.TryAdd(entityID, data);
		    return data;
		}
		
		public void CheckIn (string entityID, UserData data)
		{
		    if (string.IsNullOrEmpty(entityID) || data == null) throw new ArgumentOutOfRangeException();
		    
		    UserData value;
		    bool success = mUserDataCollection.TryGetValue(entityID, out value);
		    
		    if (value != data) throw new ArgumentOutOfRangeException();
		    
		    bool tryResult = mUserDataCollection.TryRemove(entityID, out data);
			if (!tryResult) throw new ArgumentOutOfRangeException("UserDataStore.CheckIn() - Key " + entityID + "' does not exist.");
		    data.Dispose();
		}
		
		#region Disposable members
		protected bool mIsDisposed;
        public void Dispose()
        {
            DisposeManagedResources();
		}

        public void DisposeManagedResources()
        {
           if (!mIsDisposed)
           {
                // TODO: Iterate through and dispose all contained UserData in collectiopns
			   throw new NotImplementedException("UserStore.Dispose() - ");
			   
				//Console.WriteLine ("UserData.cs.DisposeManagedResources() - ...");

			   mIsDisposed = true;
		   }
        }

        #endregion
			
	}
	

	// http://www.gamasutra.com/view/news/38977/InDepth_Behavior_Tree_Entrails.php
	// An agent's blackboard aggregates all agent specific game world knowledge. 
	// It's the only data immediate action functions are allowed to access to keep
	// cache misses at bay. A blackboard data structure might just be a C struct with
	// fields like used by Halo 2 or a key-value dictionary. It's favorable if the
	// blackboard can be stored as a data blob that's easily kept or streamed into 
	// local memory/cache.
	// ---
	// WWG Notes - August.20.2025: see KeyCommon.Data.BinaryBlob.cs
	// ---
	// https://social.technet.microsoft.com/wiki/contents/articles/13461.blackboard-design-pattern-a-practical-example-radar-defense-system.aspx
	// Blackboard is a design pattern that also requires it be threadsafe.	Blackboard
	// is great for sharing knowledge.
	// http://www.codeproject.com/Articles/451326/Type-safe-blackboard-property-bag
	public class UserData 
	{
		// http://www.gamasutra.com/view/news/198377/Video_Valves_system_for_creating_AIdriven_dynamic_dialog.php
		// http://www.valvesoftware.com/publications/2012/GDC2012_Ruskin_Elan_DynamicDialog.pdf
		// NOTE: in Valve's Zombie game, for the npc voice logic, they share
		//       all of this knowledge in a single knowledge base rather than allowing
		//       each to have it's own in a fragmented way and it makes running through
		//       them sequentially to find voice responses that match a search much faster and easier.
		//       Valve's Left 4 Dead voice logic is very much a flat database but generated by flattening
		//		 a scenegraph style directed acyclic graph (DAG))	
		//		 - The trick is how the KEY for each flattened path is created and then used when building the query string!!!		
		//		http://www.gamasutra.com/blogs/GuyHasson/20120706/173705/Story_Design_Tips_Better_NPC_Interaction_Part_II.php
		//			- sort rules alphabetically.  Why?
		//				- well this way when running the comparisons of the QUERIES against the CONDITIONS of each rule,
		//			as we iterate through each QUERY "key" we don't have to re-start an iteration at the beginning of every CONDITION "key" 
		//			because we know they are in same alphabetical order as the QUERIES collection.  For instance:
		//			QUERY: A:100, B:50, C:true, F:false
		//			RULE1:
		//          	CONDITIONS: A:<=500 && A: >=0 
		//              CONDITIONS: C:<=True && >=True
		//				- in the above, we start to iterate through the 4 query tuples and for each naivly we iterate each CONDITION
		//                but instead, when we find a matching condition, we don't need to start over.  We can resume because we know that
		//                the CONDITIONS are sorted the same way so when testing QUERY part B, we can resume iteration of CONDITION and next
		//				  CONDITION will be C: so we know B doesn't exist (else the iteration cursor would have been moved back to beginning).		
		//
		//          TODO: currently our normal propertybag stores it's data as DefaultValue and does not actually hook back to a
		//			      collection of objects.  It should actually store to same object store so that the data can also be read
		//                directly through the object store and not through the entity.  Recall that originally, the point of using the PropertySpec's
		//                was to get propertybag GUI rendering for free via propertygrid control.
		//
		//			- hash buckets for different regions and/or other basic buckets similarly to what we do when we cull
		//			- store pointers to the value we want to compare rather than have to query that game data
		//			- sort by decreasing # of criteria (as we do with TileMap auto-tile rules)
		//			- represent every comparision as a >= x >= b  
		//				eg.   return (10 >= ptrCharacterXHitpoints && ptrCharacterXHitpoints >= 100);    
		//
		//		So in a way, what we want there is a Blackboard class that can
		//       manage all that for us, and then when we first initialize a behavior
		//       on an Entity, it will grab a blackboard blob from the allocator and
		//       assign it to the Enity.Knowledge
		//       - and since the dialogue tree structure is essentially a flattened DAG (like a scenegraph)
		//		 which seems to take on a Rules Engine like functionality because it becomes serial
		//       test and not a branching test.
		//       -thus, each "record" has an owner and can be referenced and read/written to
		//       from the UserDataStore.  There is a question of whether this data should remain in 
		//       DB form.. perhaps cached for recent access.  Well, i think it must be cached or else
		//       way too slow for the type of use we do.  Do we CheckIn/CheckOut data blobs?  We could do some
		//       really fast computations I think and threaded, on an in memory "blackboard" where each blackboard 
		//       can be defined and hold all of same record types (eg all stars, all worlds, all npcs) so that
		//       manipulation of their data is... well... its all very functional style and not OO.
		//		 - THE CACHE COHERENCY BECOMES EXCELLENT.
		//		   - perhaps each derived blackboard itself becomes a data manipulator that knows how to read/write it's data
		//	       and then the sqlite or whatever storage occurs as generic using array of field definitions
		//			- Being able to define custom blackboards is nice because we now have fixed size fields
		//
		//		 - is the UserDataStore a global like Pager and Repository?
		//		- maybe each Blackboard gets instantiated EXE side and so we get StarData : UserData 
		//      that gets used for all stars and which we can write custom data manipulation against
		//		- we could even read/write to it like we do with Packets... and perhaps even use unsafe code for even greater performance
		// (See E:\dev\_projects\_XNA\Mercury Particle Engine\ProjectMercury.WindowsEmitters\Emitter.cs.Update() method)
		// but one thing it does which i think defeats the purpose somewhat perhaps is it creates a fixed pointer to the particle array rather than allocating it as pointer from start.  having to "fix" it seems like enough overhead to nullify any performance advantages
		
		//  - Production productID and Consumers can be stored here as well.  Do we still want to use scripts for these entities? or
		//    would scripts assigned to each data store type be more efficient?
		//  - for economic simulation this could be very fast
		//	- AI simulation may be more needed case for a single player 1.0 game release
		//		- blackboard data can store Area_Of_Interest data generated from other pre- calculations 
		//  - for NPC simulation this can be very fast too when running out behavior tree against this data
		//    and eventually we probably stop simulating Entity AI in Entity.Update() and move it to an Update() 
		//    of simulation that will iterate through npcs by iterating through the blackboard data (limiting iterating 
		//    to X count that fit into an alotted timeslice using threading as available and as needed)
		
		//		- IN OTHER WORDS, by iterating through the array of UserData to perform entity updates, can we properly update
		//    these variables with appropriate functions and have the update reflected in the Entity itself?  For example, lets say
		//    we have 50 entities that are doing wander steering behavior... can we run a singular script that operates on blackboard user data
		//    to update all 50 of those entities?  rather than 50 calls to entity.update() and 50 script calls.
		//          - if the scripts each entity uses can be one of the ways we sort entities when updating their data, then we can easily
		//          update all entities using a particular script.... similar to how we do renders of sorted entities
		//			- if our scene update() loop added entities to be updated in sorted buckets... but for now this is jsut brainstorming idea, since it could slow us down
		//
		
		// TODO: google cache coherency as it relates to flat databases 
		//			- and .net c#
		// TODO: isn't BehaviorContext.Knowledge already associated with Entity?  And shouldn't this data replace Entity CustomProperties? and Rename var from Knowledge to Entity.CustomData
                                   //       and is now stored in sqlite where our scene representation which uses xml is seperate from the entity custom data which is db stored.  Our EntityAPI for
                                   //       getting custom data can now also use methods with type safety.  Further we no longer have to care about custom data being serialized to xml and perhaps this
                                   //       speeds up our ability to save scene when we are editing maps as well as saving game state
        							// TODO: however, will this type of CustomProperties now no longer be easily editable in a PropertyGrid and if not, is that ok?
        							//      we're using custom html interfaces now anyway right?
        							//      we must start with _just_ custom properties for now but actually just RenderingContext 
        							// TODO: also what about shaders?  right now those use custom properties for shader params/vars and should not be stored in a db!
        							// TODO: actually volume, surfacearea,cost,weight for all celestial bodes is already being used as custom properties!
        							//       So question is, how do we connect those to a datastore?
        							//		 - well just as we use GetProperties() SetProperties() where a single reader/writer of xml store is operating
        							//       we can do same for UserData.   We can convert to GetProperties and SetProperties() and we can also
        							//       use other methods of iterating thru the list of custom data. For now, let's just focus on Viewpoint for Chase
        					
		// is there a way to track the data for an individual Entity via an Index into array of records and to have this record
		// index maintained during lifetime? indices can be checked in / checked out

		// locally, we dont really need to use entityID as part of a record key either, locally we can use just an Index
		// and perhaps a lookup value... but i think in short term, we should continue to focus on just Viewpoint and Chase cam
		// and if that goes well, Stars and see about how it works with LoadTVResource() and restoring DB via a LoadCustomData()
        							
		bool Initialized = false;      // first run? if knowledge is not initialized, then we should select initialization node first
                                       // TODO: is it useful to store these by type?  so bools, timestamps, vector3d, strings, ints, etc?
                                       //	System.Collections.Hashtable BehaviorState;
                                       //	System.Collections.Hashtable AxisState;
                                       //	System.Collections.Hashtable TimeStamps;  // when a target was seen, when received damage, when ally died, 
                                       // Stimulii <-- not sure... is this like timestamps where we learn if we've just consumed explosive damage from an explosive producer?


        // TODO: I could/should just use a Template here!
        // TODO: if everything was an object and I just used the "GetInteger()" for example, as helper method to do a cast for me since i know the type represented by each key value
        //       then perhaps i could jsut avoid all of these dictionaries? perhaps at least, have dictionaries that are now key'ed into buckets
        //       by entityID and/or by regionID and then entityID.	The point is though
        //       by storing them in a Dictionary as object, I can query the value by maintaining a reference to that object in a Rule
        //       so that when running these rules, i dont have to perform the lookup in the collection for the value.  I just have to do a cast.	
        //private int ID; // ID should (but not required) to be unique amongst all Entities and combined with an iterator count, can be used for deterministic random seeds.
        // https://www.gamedeveloper.com/programming/a-primer-on-repeatable-random-numbers
        //private int mCounter; // every traversal of the behavior tree increments this value by 1 and potentially every decision made during traversal where a Random number is needed, can increment this counter.	
		private Dictionary <string, object> Objects;
		private System.Collections.Concurrent.ConcurrentDictionary<string, object[]> ObjectsArray;
		private Dictionary <string, string> Strings;
        private Dictionary <string, string[]> StringArray;
		private Dictionary <string, bool> Bools;
		private Dictionary <string, int> Integers;
		private Dictionary <string, float> Floats;
		private Dictionary <string, double> Doubles;
		//private Dictionary <string, System.Drawing.Point> Points;
		private Dictionary <string, Vector3d> Vectors;
		private Dictionary <string, Vector3d[]> Vector3dArrays;
		private Dictionary <string, Quaternion> Quaternions;
		//private Dictionary <string, Color> Colors;

        // https://github.com/wuyuntao/BehaveAsSakura/tree/master
        // TODO: if we enforced all fields first, then we could do a fixed layout
        //       but if that's the case, we might as well just use struct{}
        //       However, also it could be better if the key for all of these
        //       is tied to the Entity so that we have key = entity.ID + ":" + name
        //       and this way we can use a single set of Dictionaries (or in the future, arrays)
        //       to store everything.  The problem is with arrays, we could use a lookup for the entity ID
        //       to find the index for the record, then use sub-index for the specific field
        // TODO: Collections field could be used perhaps to store nested Data?
        private Dictionary <string, UserData> Collections;

		protected int mUserTypeID;   // can be defined by game##.dll or by an enum that is generated into a compiled binary at runtime
		
				
        /// <summary>
        /// UseData.ctor() uses the access modifier "internal" because an instance
        /// must be obtained via GameAPI which will result in a call to 
        /// UserDataStore.CheckOut() which will provide an index.
        /// Our Viewpoint BehaviorTree is one exception currently that calls 
        /// UserDataStore.CheckOut() that does not originate from a script call 
        /// to GameAPI.
        /// </summary>
        internal UserData()
        {
			mUserTypeID = -1;
        }

		internal UserData Clone ()
		{
			UserData copy = new UserData ();

            // TODO: a single array of object would consume less memory
            //       and cloning it would not require we maintain the code whenever we add
            //       a new generic Dictionary type.
            // 
            //  and then why not then use "custom properties" or something?
            //  our PropertyTable is a type of blackboard too... but its main
            //  feature is that it allows for use with a propertyGrid
            //  We could maybe streamline it... but it uses just flat array instead of
            //  dictionary.  
            //  Also, even our "custom properties" could use same array as our regular properties
            //  only we could add them to a category of "custom" properties instead
            //  so that when serializing we can skip them

            // our IEntityAPI can still use special accessors for get/set so that caller in script
            // does not have to specify a category, but actually i dont think thats necessary.  they are
            // only keyed by property name, not name and category.
            // 
            // Also, we can still do database storage easily using a DataObject wrapper around our Properties.
            // or well maybe scrach that, since our normal properties are linked to intrinsic property fields 
            // in those Entities like _translation and _scale and _orientation, but our Behavior nodes can still
            // access those as blackboard knowledge...

            // so i think our 'UserData' interface should merge with "CustomProperties" in the short term 
            // and be cloneable.  at the least instead of spec.DefaultValue, we should be using actual
            // UserData[key]  to store the value.
            throw new NotImplementedException();
            return null;
		}
		
		public int UserTypeID 
		{ 
			get {return mUserTypeID; } 
			set { mUserTypeID = value;}
		}
		

        // TODO: our "Entity.BlackboardData" will contain an array of objects that each script
        //       for that Entity will assign and therefore know how to cast each array element.
        //       So we can have bbData = new object[2];
        //       bbData[0] = new ComponentStore<EnergyWeapon>();
        //       bbData[1] = new UserData;  // <-- this is the AI data which can be a struct also and which the Entity's script will know what is assigned to this index
        public object[] GetObjectArray(string name)
        {
            return ObjectsArray[name];
        }
        
        public void SetObject(string name, object[] value)
        {
            if (ObjectsArray == null)
                ObjectsArray = new System.Collections.Concurrent.ConcurrentDictionary<string, object[]>();

            if (ObjectsArray.ContainsKey(name))
                ObjectsArray[name] = value;
            else
                ObjectsArray.TryAdd(name, value);
        }
        
        public object GetObject(string name)
        {
            return Objects[name];
        }
        
        public void SetObject(string name, object value)
        {
            if (Objects == null)
                Objects = new Dictionary<string, object>();

            if (Objects.ContainsKey(name))
                Objects[name] = value;
            else
                Objects.Add(name, value);
        }

        public string GetString (string name)
		{
			return Strings[name];
		}
		
		public void SetString (string name, string value)
		{
			if (Strings == null)
					Strings = new Dictionary<string, string>();
			
			if (Strings.ContainsKey(name))
				Strings [name] = value;
			else   				
				Strings.Add (name, value);
		}

        public string[] GetStringArray(string name)
        {
            return StringArray[name];
        }

        public void SetStringArray(string name, string[] value)
        {
            if (StringArray == null) StringArray = new Dictionary<string, string[]>();
            StringArray[name] = value;
        }

		public bool GetBool (string name)
		{
			return Bools[name];
		}
		
		public void SetBool (string name, bool value)
		{
			if (Bools == null)
					Bools = new Dictionary<string, bool> ();
			
			if (Bools.ContainsKey(name))
				Bools [name] = value;
			else   				
				Bools.Add (name, value);
		}
		
		public int GetInteger (string name)
		{
			return Integers[name];
		}
		
		public void SetInteger (string name, int value)
		{
			if (Integers == null)
				Integers = new Dictionary<string, int> ();
			
			if (Integers.ContainsKey(name))
				Integers [name] = value;
			else   				
				Integers.Add (name, value);
		}
		
		public void IncrementInteger (string name)
		{
			if (Integers == null)
				Integers = new Dictionary<string, int> ();
			
			if (Integers.ContainsKey(name))
				Integers [name] += 1;
			else   				
				Integers.Add (name, 1); // it doesnt exist would mean it's 0, so increment it  to 1, yes?
		}
		
		public void DecrementInteger (string name)
		{
			if (Integers == null)
				Integers = new Dictionary<string, int> ();
			
			if (Integers.ContainsKey(name))
				Integers [name] -= 1;
			else   				
				Integers.Add (name, -1); // it doesnt exist would mean it's 0, so decrement it  to -1, yes?
		}
			
		public double GetDouble (string name)
		{
			return Doubles[name];
		}
		
		public void SetDouble (string name, double value)
		{
			if (Doubles == null)
					Doubles = new Dictionary<string, double> ();
			
			if (Doubles.ContainsKey(name))
				Doubles [name] = value;
			else
				Doubles.Add (name, value);
		}
		
		public float GetFloat (string name)
		{
			return Floats[name];
		}
		
		public void SetFloat (string name, float value)
		{
			if (Floats == null)
					Floats = new Dictionary<string, float> ();
			
			if (Floats.ContainsKey(name))
				Floats [name] = value;
			else
				Floats.Add (name, value);
		}
		  
		/*
		public System.Drawing.Point GetPoint (string name)
		{
			return Points [name];
		}
		public void SetPoint (string name, System.Drawing.Point value)
		{
			if (Points == null)
				Points = new Dictionary<string, System.Drawing.Point> ();
				
			if (Points.ContainsKey(name))
				Points [name] = value;
			else
				Points.Add (name, value);

		}
		*/
		
		public Vector3d GetVector (string name)
		{
			return Vectors [name];
		}
		
		public void SetVector (string name, Vector3d value)
		{
			if (Vectors == null)
				Vectors = new Dictionary<string, Vector3d> ();
				
			if (Vectors.ContainsKey(name))
				Vectors [name] = value;
			else
				Vectors.Add (name, value);
		}

		public Vector3d[] GetVector3dArray (string name)
		{
			return Vector3dArrays [name];
		}
		
		public void SetVector3dArray (string name, Vector3d[] value)
		{
			if (Vector3dArrays == null)
				Vector3dArrays = new Dictionary<string, Vector3d[]> ();
				
			if (Vector3dArrays.ContainsKey(name))
				Vector3dArrays [name] = value;
			else
				Vector3dArrays.Add (name, value);
		}
		
		
		public Quaternion GetQuaternion (string name)
		{
			return Quaternions [name];
		}
		
		public void SetQuaternion (string name, Quaternion value)
		{
			if (Quaternions == null)
				Quaternions = new Dictionary<string, Quaternion> ();
				
			if (Quaternions.ContainsKey(name))
				Quaternions [name] = value;
			else
				Quaternions.Add (name, value);
		}
		
		
		
	#region Disposable members
		bool mIsDisposed;
        public void Dispose()
        {
            DisposeManagedResources();
		}

        public void DisposeManagedResources()
        {
           if (!mIsDisposed)
           {
                
				//Console.WriteLine ("UserData.cs.DisposeManagedResources() - ...");

			   mIsDisposed = true;
		   }
        }
        #endregion
	}	
#endregion // USERDATA STORE and USERDATA 



    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN OCTREE 

    // http://www.flipcode.com/archives/Octree_Implementation.shtml
    /// <summary>
    /// A dynamic + loose octree implementation. 
    /// Dynamic = children are only added up to the depth that is first deepest enough to accomodate the bounds of the items being inserted into the tree.
    /// </summary>
    public class OctreeOctant //: ISpatialNode //, ITraversable, IBoundVolume
    {
        #region Static variables
        public static uint MaxDepth = 7;            // try ~7 - 9
        public static uint SplitThreshHold = 8; // try ~8 - 15

        private static Vector3d[] BoundsOffsetTable = new Vector3d[]
        {
                new Vector3d(-0.5, -0.5, -0.5),
                new Vector3d(+0.5, -0.5, -0.5),
                new Vector3d(-0.5, +0.5, -0.5),
                new Vector3d(+0.5, +0.5, -0.5),
                new Vector3d(-0.5, -0.5, +0.5),
                new Vector3d(+0.5, -0.5, +0.5),
                new Vector3d(-0.5, +0.5, +0.5),
                new Vector3d(+0.5, +0.5, +0.5)
        };

        #endregion
			
        private bool mEnforceMaxDepth = false;
        private int _depth;
        private double mOctantRadius;
        private int mIndex;   // index is specific to each depth and contains x,y,z offset at that depth and is useful for finding neighbors (which we may never do and just always move EntityNodes by re-inserting starting at root)
        private const int MAX_CHILD_COUNT = 8;

        private BoundingBox mBox;
		private BoundingSphere mSphere;
		
        private OctreeOctant mParent;
        private OctreeOctant[] mChildOctants;	
		
        // TODO: switch to linked list?
        private List<EntityNode> mEntityNodesCollection;

		private static System.Threading.SemaphoreSlim mSemaphoreSlim = new System.Threading.SemaphoreSlim(1);
		private object mAddLock;
		
        public OctreeOctant(int index, int depth, BoundingBox box, OctreeOctant parent)
            : this()
        {
            mIndex = index;
            _depth = depth;
            mBox = box;
			mSphere = new BoundingSphere(mBox.Center, mBox.Radius);
				
            mParent = parent;
            
            mOctantRadius = this.BoundingBox.Max.x - this.BoundingBox.Min.x;
            mOctantRadius = Math.Min(this.BoundingBox.Max.y - this.BoundingBox.Min.y, mOctantRadius);
			mOctantRadius = Math.Min(this.BoundingBox.Max.z - this.BoundingBox.Min.z, mOctantRadius);
			mOctantRadius *= 0.5d;

			mAddLock =  new object();
				
            //System.Diagnostics.Debug.WriteLine("OctreeOctant.ctor() -- Created at index " + index.ToString());
        }

        public OctreeOctant()
        {
            Visible = true;
        }

        ~OctreeOctant()
        {
        }

        /*
                #region ITraversable Members
                public object Traverse(ITraverser target, object data)
                {
                    return target.Apply(this, data);
                }
                #endregion
        */

        private bool IsRoot { get { return mParent == null; } }

        private OctreeOctant Parent { get { return mParent; } set { mParent = value; } }

        public bool IsLeaf { get { return mChildOctants == null; } }

		internal Vector3d Radius
        {
            get
            {
                Vector3d radius;
                radius.x = mBox.Width * 0.5d;
                radius.y = mBox.Height * 0.5d;
                radius.z = mBox.Depth * 0.5d;
                return radius;
            }
        }

        internal int Depth
        {
            get { return _depth; }
        }
        
        public double MaxRadius
        {
            get 
            { 
                return mOctantRadius;
            }
        }

        public int Index
        {
            get { return mIndex; }
        }

		public string Address 
		{
			get 
			{
				const string SEPERATOR = ",";
				string result = this.mIndex.ToString();
				OctreeOctant parent = mParent;
				
				while (mParent != null)
				{
					result = mParent.mIndex + SEPERATOR + result;
					mParent = mParent.mParent;
				}
				
				return result;
			}
		}
		
        public OctreeOctant[] Children
        {
            get { return mChildOctants; }
        }
		
		/*
        internal int[] LocalIndexToVector(int index)
        {

            // divide the index by  2 ^ depth
            // 

            int[] v = new int[3];
            if ((index & 1) > 0) v[0] = 1;
            else v[0] = -1;

            if ((index & 2) > 0) v[1] = 1;
            else v[1] = -1;

            if ((index & 4) > 0) v[2] = 1;
            else v[2] = -1;

            return v;
        }

        internal int LocalVectorToIndex(int[] v)
        {
            int index = 0;

            if (v[0] >= 0) index |= 1;
            if (v[1] >= 0) index |= 2;
            if (v[2] >= 0) index |= 4;

            return index;
        }
		*/


        #region ISpatialNode
        public bool Visible { get; set; }

        public EntityNode[] EntityNodes
        {
            get
            {
                try
                {
                    mSemaphoreSlim.Wait(-1);
                
					if (mEntityNodesCollection == null) return null;
					
					return mEntityNodesCollection.ToArray();
                 }
                 finally
                 {
                     mSemaphoreSlim.Release();
                 }
            }
        }

        public void Add(EntityNode entityNode, bool forceRoot = false)
        {
			//lock(mAddLock)
     		try
			{
				mSemaphoreSlim.Wait(-1); // -1 waits indefinetly, otherwise parameter represents maximum milliseconds to wait
				{
					if (forceRoot)
					{
						this.AddEntityNodeToCollection((EntityNode)entityNode);
						//System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName + " at Address " + this.Address + " Forced into Root");
					}
					else
					{
						// TODO: we might have optional param to EnQueue this Add() rather than try to Add it immediately
						this.Add(entityNode);
					}
				}
			}
			finally
			{
				mSemaphoreSlim.Release();
			}
        }

        private void Add(EntityNode entityNode)
        {
			//lock(mAddLock)
			try
			{
				//mSemaphoreSlim.Wait(-1); // -1 waits indefinetly, otherwise parameter represents maximum milliseconds to wait
				//System.Diagnostics.Debug.Assert(this.BoundingBox != null, "OctreeOctant.Add() - BoundingBox is null.");
	#if DEBUG
				// only support square octree octants for performance
	//            // TODO: we are going to see if this "performance" concern is no longer valid.  non square octrees are useful 
	//            System.Diagnostics.Debug.Assert(this.BoundingBox.Max.x - this.BoundingBox.Min.x ==
	//                this.BoundingBox.Max.y - this.BoundingBox.Min.y &&
	//            this.BoundingBox.Max.x - this.BoundingBox.Min.x == 
	//            this.BoundingBox.Max.z - this.BoundingBox.Min.z);
	#endif


				// https://stackoverflow.com/questions/4324703/should-an-octree-be-rebuilt-every-frame

				// https://www.gamedev.net/articles/programming/general-and-gameplay-programming/introduction-to-octrees-r3529/
				//     1 - this one from gamedev.net has a good idea of creating a list of Entities (objects) that are to be added
				//       to or moved within, the Octree during a particular frame, and then to update them all at once after 
				//       this list of Entities/Objects to add/move is completed.
				//     2 - it also  has an idea for using a lifespan test to see if an empty octant should be deleted rather than
				//         to delete it immediately upon becoming empty of any EntityNodes/Objects.  This way if say a stream\burst
				//         of bullets are moving in the same direction, one bullet will leave an octant, but closely followed by another which may soon need
				//         the octant previously occupied by the earlier bullet.
				//			a) the code can also be found here -> https://www.wobblyduckstudios.com/Octrees.php
				//				- https://www.wobblyduckstudios.com/Code/OctTree.cs
				//              - https://www.wobblyduckstudios.com/Code/IntersectionRecord.cs
				//				- https://www.wobblyduckstudios.com/Code/Physical.cs    <-- sort of an Entity class with Physics properties like acceleration, max acceleration, velocity, etc
				// 
				//
				// TODO: // TODO: https://daeken.dev/a-stupidly-simple-fast-octree-traversal-for-ray-intersection

				int count = 0;

				if (mEntityNodesCollection != null)
					count = mEntityNodesCollection.Count;

				//    NOTE: We specifically use ">=" for the depth comparison so that we
				//          can set the maximumDepth depth to 0 if we want a tree with
				//          no depth.
				if (mEnforceMaxDepth && _depth >= OctreeOctant.MaxDepth)
				{
					// add to this octant immmediately Non Recursively
					this.AddEntityNodeToCollection((EntityNode)entityNode);
					return;
				}

				if (entityNode.BoundingBox.Radius <= 0) throw new Exception("OctreeOctant.Add() - Entity BoundingBox invalid.");
				double entityRadius = entityNode.BoundingBox.Radius;

				// note: we intentionally compute a radius without taking into account hypotenuse.
				// note: if allowiing non square octants, we take the smallest octant radius and we'll compare that against largest radius of entity being inserted
				double octantRadius = this.MaxRadius;
				double childOctantRadius = octantRadius * 0.5d;

				// if the entityRadius is greater than that of any child nodes, try adding it here
				// or recurse UPWARDS (note: because we cull using loose bounding box, it is ok
				// to add when it's radius is less than or equal to the octant's radius because that alone guarantees
				// entire entity's box will fit within loose bounds which is used during culling
				if (entityRadius > childOctantRadius)
				{
					//Console.WriteLine("OctreeOctant.Add() - insert testing at depth " + _depth.ToString());
					// this entity won't fit in any children of this octant 
					// so what about the parent octant?
					if (entityRadius > octantRadius)
					{
						// wont fit, can we try to move up to a parent?
						if (this.IsRoot == false)
						{
							// Recurse UPWARDS
							//Console.WriteLine("OctreeOctant.Add() - insert UPWARDs");
							mParent.Add(entityNode);
							return;
						}
					}
					// Non Recursive Add because we're still here, 
					// so it either fits or we're at root and there's
					// no other place to put it
					this.AddEntityNodeToCollection(entityNode);
					return;
				}

				// can't go further, add entitynode here
				if (this.Split() == false)
				{
					this.AddEntityNodeToCollection(entityNode);
					return;
				}
				
				
				//  BEGIN PATH #1 which should be functionally equivalent to PATH #2
				// TODO: surely a FOR LOOP here is not needed?  We just
				//       need: if (code < MAX_)
				//      int i = code;
				//      // then leave the rest of the code the same... 
				
				int bestFitChildOctantIndex = GetBestFitOctant(this.BoundingBox.Center, entityNode.BoundingBox.Center);
				System.Diagnostics.Debug.Assert (bestFitChildOctantIndex >= 0 && bestFitChildOctantIndex <= MAX_CHILD_COUNT, "OctreeOctant.Add() - Octant code/index out of range.");
				
				Vector3d offset = OctreeOctant.BoundsOffsetTable[bestFitChildOctantIndex] * octantRadius;
				Vector3d childOctantCenter = this.BoundingBox.Center + offset;

				BoundingBox childOctantBox = new BoundingBox(childOctantCenter, (float)childOctantRadius);

				if (mChildOctants[bestFitChildOctantIndex] == null)
					mChildOctants[bestFitChildOctantIndex] =
					new OctreeOctant(bestFitChildOctantIndex, _depth + 1, childOctantBox, this);

				// Recursive Add() until max depth is reached or the entity's radius > octant's loose radius
				mChildOctants[bestFitChildOctantIndex].Add(entityNode);
				
				/* // BEGIN PATH #2 which should be functionally equivalent to PATH #1
				for (int i = 0; i < MAX_CHILD_COUNT; i++)
				{
					// the bitflag combination created above MUST ALWAYS evaluate to
					// values of 0 thru 7 which represents the 8 octants
					if (code != i) continue;

					Vector3d offset = OctreeOctant.BoundsOffsetTable[i] * octantRadius;
					Vector3d center = octantCenter + offset;

					BoundingBox childOctantBox = new BoundingBox(center, (float)childOctantRadius);

					if (mChildOctants[i] == null)
						mChildOctants[i] =
							new OctreeOctant(i, _depth + 1, childOctantBox, this);

					// Recursive Add() until max depth is reached or the entity's radius > octant's loose radius
					mChildOctants[i].Add(entityNode);
				}
				*/ // END PATH #2
				

				// Remove nodes that already exist 
				if (mEntityNodesCollection != null)
				{
					EntityNode[] toReAdd = mEntityNodesCollection.ToArray();
					mEntityNodesCollection = null;
					for (int i = 0; i < toReAdd.Length; i++)
					{
						Add(toReAdd[i]);
					}
				}
			}
			finally
			{
				//mSemaphoreSlim.Release();
			}
   		}

		private int GetBestFitOctant(Vector3d parentOctantCenter, Vector3d entityCenter)
		{
			int code = 0;
			if (entityCenter.x > parentOctantCenter.x)
				code |= 1;
			if (entityCenter.y > parentOctantCenter.y)
				code |= 2;

			// TODO: if this is a 2D Octree which should just use a quadtree of course... then we should skip the following entityCenter.z test
			if (entityCenter.z >= parentOctantCenter.z)
				code |= 4;
			
			return code;
		}
		
		
		private void AddEntityNodeToCollection(EntityNode entityNode)
        {
            if (mEntityNodesCollection == null)
                mEntityNodesCollection = new List<EntityNode>();

            entityNode.SpatialNode = this;
            mEntityNodesCollection.Add(entityNode);
           
           //Console.WriteLine(_depth.ToString());           
           // Console.WriteLine("Added at Depth == " + _depth.ToString() +  "total ent count = " + mEntityNodesCollection.Count.ToString());
        }

		
        public void RemoveEntityNode (EntityNode entityNode)
        {
            // NOTE: The reason for this function as opposed to just using OnEntityNode_Removed()
            // is that when a node is moving, then we directly call OnEntityNode_Removed() instead
            // so that the .SpatialNode = null can occur before we call OnEntityNode_Removed() 
            // and yet so we dont have to make the OnEntityNode_Removed() before we .Add to the new
            // destination.  This is important to avoid collapsing of empty branches before we've
            // had a chance to find the correct new parent.
            entityNode.SpatialNode = null;
            OnEntityNode_Removed(entityNode);
        }
           
		// Split() is called by this.Add()
        private bool Split()
        {	
            // cannot split because we're at max depth
            if (mEnforceMaxDepth && _depth == OctreeOctant.MaxDepth)
			{
                //Console.WriteLine("Max Depth reached ... " + _depth.ToString());
				return false;
			}
			
            // we are already split
            if (this.mChildOctants != null)
            {
                return true;
            }
		
			if (this.mEntityNodesCollection != null)
			{
				// we only meed to split if splitThreshold reached (NOTE: if we made it here, mEnforceMaxDepth must be false)
				// otherwise this represents deepest available octant on this branch
				if (this.mEntityNodesCollection.Count >= OctreeOctant.SplitThreshHold)
				{
					// initialize the array but do not instance or assign an Octant
					this.mChildOctants = new OctreeOctant[8];
					
					// NOTE: the existing Entities which must now be tested to see if they can fit in the deepest Octant
					//       which may now be one of the mChildOctants we just instanced above, is done at the bottom of
					//       private void Add(EntityNode node)
					return true;
				}
        	}
			return false;
		}

        public void OnEntityNode_Moved(EntityNode entityNode)
        {
			//lock(mAddLock)
			try
			{
				
				mSemaphoreSlim.Wait(-1); // -1 waits indefinetly, otherwise parameter represents maximum milliseconds to wait

				// is the entity still in this bounds?
				// we dont have to test the radius of the entityNode because
				// we already know it fits.
				if (mBox.Contains(entityNode.BoundingBox.Center)) return;

				// inform the parent that the entity in this octant no longer fits
				// NOTE: we do not add/remove the entityNode here.  The parent must do it
				// so that we don't trigger collapse of all 8 of it's children before parent can 
				// have a chance to fit it into one of its other 7 children
				if (this.IsRoot == false)
				{
					mParent.Move(this, entityNode); // calls updward to Parent
				}
				
			}
			finally
			{
				mSemaphoreSlim.Release();
			}
        }

		///<summary>
		/// PreviousOctant should always be a child of the OctreeOctant executing this method
		/// </summary>
        private void Move(OctreeOctant previousOctant, EntityNode entityNode)
        {
			#if DEBUG
				bool found = false;
				for (int i = 0; i < mChildOctants.Length; i++)
					if (mChildOctants[i] == previousOctant)
					{
						found = true;
						break;
					}
			
			if (!found) throw new Exception("OctreeOctant.Move() - Invalid previousOctant.");
			#endif
			
			
			//System.Diagnostics.Debug.WriteLine ("OctreeOctant.Move() - " + entityNode.Entity.TypeName);
			// NOTE: Here we clear the .SpatialNode first but we must not call OnEntityNode_Removed()
			//       until AFTER .Add() is called.
			entityNode.SpatialNode = null;

			// we cannot simply attempt to add to this parent because
			// if the entityNode has moved beyond this parent's own bounds
			// our fast Add() (which avoids having to do a Box.Contains() call 
			// will not be able to determine this and will simply force insert
			// the entityNode into itself.

			// so we can easily avoid that by recursing til we find the first parent
			// that contains the entityNode.. and provided the entityNode has not changed size
			// (particularly has not gotten larger) we are guaranteed that the parent octant
			// is large enought to contain it if the entityNode's center is with in it.
		
						
			OctreeOctant newOctant = previousOctant;
			if (previousOctant.mParent != null)
				newOctant = previousOctant.mParent;
			
			Vector3d entityCenter = entityNode.BoundingBox.Center;
			while (newOctant.Parent != null)
			{
				if (newOctant.BoundingBox.Contains(entityCenter))
					break;

				newOctant = newOctant.Parent;
			}

			newOctant.Add(entityNode);    // Add must always occur before Remove() because we dont want to collapse branches before we've had a chance to determine if the child will move there!
			previousOctant.OnEntityNode_Removed(entityNode);
		}

        internal void OnEntityNode_Removed(EntityNode entityNode)
        {
			//lock (mAddLock)
			try
			{
				//mSemaphoreSlim.Wait(-1); // -1 waits indefinetly, otherwise parameter represents maximum milliseconds to wait
				{
					// remove the entityNode
					if (mEntityNodesCollection == null) return;
					mEntityNodesCollection.Remove(entityNode);

					// can we collapse this octant?
					if (mEntityNodesCollection.Count == 0)
					{
						mEntityNodesCollection = null;
						// must now notify the parent that this octant can be destroyed
						// TODO: nov.27.2012 - i think when an entityNode is added to octree root node, there is no parent
						//       so how do we prevent this?  should i just return if null?  will do for now
						// TODO: however i also think part of the problem this seems to keep being called for non moving
						//       think like a manually place directional light is our physics update
						if (mParent == null) return;
						mParent.OnChildOctant_Empty(this);
					}
				}
			}
			finally
			{
				//mSemaphoreSlim.Release();
			}
        }

		public void OnEntityNode_Resized(EntityNode entityNode)
        {
            // does this entityNode still fit in this octant?
            // we must test against entire box since this entity may now be too big to fit
			// TODO: is this call correct? ".Contains()" must check that all corners are fully contained correct? not just intersecting?
            if (mBox.Contains(entityNode.BoundingBox)) return;

            if (this.IsRoot == false)
                mParent.Resize(this, entityNode);
        }

        private void Resize(OctreeOctant childOctant, EntityNode entityNode)
        {
            //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Resize() - " + entityNode.Entity.TypeName);
            entityNode.SpatialNode = null;

            // if the entity itself has resized, we cannot do the quick .Contains(point)
            // and instead must do .Contains(box) to see if this entity still fits within this octant
            OctreeOctant newOctant = this;
            BoundingBox box = entityNode.BoundingBox;

            while (newOctant.Parent != null)
            {
                if (newOctant.BoundingBox.Contains(box))
                    break;

                newOctant = newOctant.Parent;
            }

            // once we've found a parent that fully contains the box, we can do an a normal
            // Add() to recurse downwards again.
            // Add must always occur before Remove() because we dont want to collapse branches 
            // before we've had a chance to determine if the child will move there!
            newOctant.Add(entityNode);
            childOctant.OnEntityNode_Removed(entityNode);
        }
		
        private void OnChildOctant_Empty(OctreeOctant childOctant)
        {
			//lock (mAddLock)
			{
				int nullCount = 0;
				for (int i = 0; i < mChildOctants.Length; i++)
					if (mChildOctants[i] == childOctant)
					{
						mChildOctants[i].Parent = null; // or mChildOctants[i].Dispose() ?
						mChildOctants[i] = null;
						nullCount++;
					}
					else if (mChildOctants[i] == null)
						nullCount++;

				// if all child octants are null, we can delete the entire child array
				// and potentially it's parents too
				if (nullCount == MAX_CHILD_COUNT)
				{
					mChildOctants = null;
					if (IsRoot == false && mEntityNodesCollection == null)
						mParent.OnChildOctant_Empty(this); // recurse upwards
				}
			}
        }
        #endregion

        //public void Add(EntityNode element)
        //{
        //    int x = 0;
        //    int y = 0;
        //    int z = 0;
        //    int depth = FindIdealInsertion(element.Position, element.BoundingBox.Radius, ref x, ref y, ref z);

        //    OctreeOctant foundOctant = FindBestFittingOctant(x, y, z, depth);
        //    System.Diagnostics.Debug.WriteLine(string.Format("OctreeOctant.Add () - Adding entityNode to node {0} at {1},{2},{3} depth {4}", foundOctant.Index, x,y,z,depth ));
        //    foundOctant.AddEntityNode((EntityNode)element);

        //    if (foundOctant == null) throw new Exception();
        //}

        //private int FindIdealInsertion(Vector3d objectPosition, double objectRadius, ref int x, ref int y, ref int z)
        //{
        //    // TODO: if we enforce cubic octree, we dont need a box, just a diameter
        //    // and this shoudl be desireable because insertions is complicated if we need to test 3 axis 
        //    // radius
        //    if (objectRadius < 0)
        //    {
        //        System.Diagnostics.Debug.WriteLine("OctreeOctant.FindIdealInsertion() - Entity has negative radius.");
        //        return 0;
        //    }

        //    double octantDiameter = OctreeOctant.WorldBox.Diameter;
        //    int depth = 0;
        //    double k = .5;


        //    // iterate downwards in depth until the octant's loose radius is finally smaller than
        //    // the object's radius
        //    while(depth <= OctreeOctant.MaxDepth)
        //    {
        //        octantDiameter /= 2;
        //        depth++;

        //        if(octantDiameter * (1- k ) / 2 < objectRadius)
        //            break;
        //    }

        //    //we're off by one
        //    depth--;
        //    octantDiameter *= 2;

        //    //get the x,y,z index of the node at this level in the tree
        //    x = (int) (objectPosition.x / octantDiameter);
        //    y = (int) (objectPosition.y / octantDiameter);
        //    z = (int) (objectPosition.z / octantDiameter);


        //    return depth ;
        //}

        //private OctreeOctant FindBestFittingOctant(int x, int y, int z, int depth)
        //{
        //    OctreeOctant octant = RootOctant;
        //    BoundingBox box = OctreeOctant.WorldBox;

        //    for (int currentDepth = 0; currentDepth != depth; ++currentDepth)
        //    {
        //        if (!octant.Split())  // if the octant cannot be split, this is as far as we can go
        //            return octant;
        //        else
        //        {
        //            /*
        //                We can find the exact child without any comparisons
        //                For example, we're looking for an octant at depth 2 with x,y,z = (2,1,3)
        //                This will be a child of the octant at depth 1 with x,y,z = (1,0,1)

        //                We take the convention that childOctants are layed out as:

        //                local index                1D index
        //                [(0,1,0) (1,1,0)]        [2 3]
        //                [(0,0,0) (1,0,0)]        [0 1]
        //                                    =
        //                [(0,1,0) (1,1,0)]        [6 7]
        //                [(0,0,0) (1,0,0)]        [4 5]

        //                To find the local index of an octant in the frame of it's direct parent, 
        //                we have to divide the index by two.
        //                To find the local index of an octant in the frame of it's  parent x times up,
        //                we have to divide the index by 2^x

        //            */
        //            //this generates the local index of the child octant at (currentDepth - 1)
        //            int currentDepthX = x >> (depth - (currentDepth + 1));
        //            int currentDepthY = y >> (depth - (currentDepth + 1));
        //            int currentDepthZ = z >> (depth - (currentDepth + 1));
        //            int globalIndex = currentDepthX + currentDepthY << 1 + currentDepthZ << 2;

        //            int localIndex = LocalVectorToIndex(new int[] { currentDepthX, currentDepthY, currentDepthZ});

        //            if (octant.Children[localIndex] == null) 
        //            {
        //                // create a box with half the diameter and offset to the parent's center
        //                // according to it's octant index
        //                Vector3d offset = OctreeOctant.BoundsOffsetTable[localIndex] * box.Radius;
        //                Vector3d center = box.Center + offset;
        //                double radius = box.Radius / 2;

        //                box = new BoundingBox(center, (float)radius);

        //                octant.Children[localIndex] =
        //                    new OctreeOctant(globalIndex, currentDepth + 1, box);
        //            }
        //            octant = octant.Children[localIndex];
        //        }
        //    }

        //    //if we make it here, we're at the minimum depth. and we found our octant
        //    return octant;
        //}

        // TODO: I should implement an overloaded version of Query that traverses for Entities that lay within a bounding box or sphere with potential also of matching a "match" predicate.
        //       Multi-threading of the query would be ideal.
        /// <summary>
        /// Looks for Regions/Entities that are in descendant RegionNodes or EntityNodes 
        /// that match the specified predicate.
        /// </summary>
        /// <param name="recurse"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public virtual List<Tuple<EntityNode, double>> Query(EntityNode refEnt, bool recurse, BoundingBox searchArea, Func<EntityNode, EntityNode, Tuple<bool, double>> match) // todo: maybe use Tuple<bool, object> 
        {
            if (match == null) throw new ArgumentNullException("SceneNode.Query() - match cannot be null.");

            if (!this.mBox.Intersects(searchArea))
                return null;
            //Console.WriteLine ("Query B");

            List<Tuple<EntityNode, double>> results = new List<Tuple<EntityNode, double>>();

            if (mEntityNodesCollection != null)
                for (int i = 0; i < mEntityNodesCollection.Count; i++)
                {
					Tuple<bool, double> r = match(mEntityNodesCollection[i], refEnt);
                    if (r.Item1)
                        results.Add(new Tuple<EntityNode, double> (mEntityNodesCollection[i], r.Item2));
                }
//if (!recurse)
 //  Console.WriteLine("%%");
//recurse = true;

            if (recurse)
            {
                if (mChildOctants != null)
                {
                    // NOTE: We recurse the child OctreeOctants, not EntityNodes
                    for (int j = 0; j < mChildOctants.Length; j++)
                    {
						if (mChildOctants[j] == null) continue;
						
                        List<Tuple<EntityNode, double>> nestedResults = mChildOctants[j].Query(refEnt, recurse, searchArea, match);
                        if (nestedResults != null)
                            results.AddRange(nestedResults);

                    }
                }
            }

            if (results.Count == 0)  
                return null;
          
            return results;
        }

		/// <summary>
        /// Looks for Regions/Entities that are in descendant RegionNodes or EntityNodes 
        /// that match the specified predicate.
        /// </summary>
        /// <param name="recurse"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public virtual List<Tuple<EntityNode, double>> Query(EntityNode refEnt, bool recurse, BoundingSphere searchSphere, Func<EntityNode, EntityNode, Tuple<bool, double>> match) // todo: maybe use Tuple<bool, object> 
        {
            if (match == null) throw new ArgumentNullException("SceneNode.Query() - match cannot be null.");

            if (this.BoundingSphere.Intersects(searchSphere) == IntersectResult.OUTSIDE)
                return null;
            //Console.WriteLine ("Query B");

            List<Tuple<EntityNode, double>> results = new List<Tuple<EntityNode, double>>();

            if (mEntityNodesCollection != null)
                for (int i = 0; i < mEntityNodesCollection.Count; i++)
                {
					Tuple<bool, double> r = match(mEntityNodesCollection[i], refEnt);
                    if (r.Item1)
                        results.Add(new Tuple<EntityNode, double> (mEntityNodesCollection[i], r.Item2));
                }
//if (!recurse)
 //  Console.WriteLine("%%");
//recurse = true;

            if (recurse)
            {
                if (mChildOctants != null)
                {
                    // NOTE: We recurse the child OctreeOctants, not EntityNodes
                    for (int j = 0; j < mChildOctants.Length; j++)
                    {
						if (mChildOctants[j] == null) continue;
						
                        List<Tuple<EntityNode, double>> nestedResults = mChildOctants[j].Query(refEnt, recurse, searchSphere, match);
                        if (nestedResults != null)
                            results.AddRange(nestedResults);

                    }
                }
            }

            if (results.Count == 0)  
                return null;
          
            return results;
        }

        #region IBoundVolume Members
        /// <summary>
        /// Public bbox used for culling tests
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                return mBox;
            }
        }
		
		public BoundingSphere BoundingSphere
		{
			get 
			{
				return mSphere;
			}
		}

		
        /*
        public BoundingSphere BoundingSphere
        {
            get { return new BoundingSphere(mBox); } // TODO: compute center from x,y,z index, then return sphere new BoundingSphere(center, _radius); }
        }
        */

        public bool BoundVolumeIsDirty
        {
            // octree bounds are fixed.
            get { return false; }
        }


        protected void UpdateBoundVolume()
        {

        }
        #endregion
    }
    ////////////////////////////////////////////////////////////////////////////////////////////////
    // END OCTREE




    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN TYPES

    // This attribute is not required at least for the PropertyGrid using PropertyBags
    // because you can specify the converter to use for each PropertySpec item in the bag.
    // But it is needed for KeyPluginEntityEdit.Animations for modifying keyframe values in the plugin GUI interface
    //[TypeConverter(typeof(Keystone.TypeConverters.Vector3dConverter))]
    public struct Vector3d
    {
        public double x;
        public double y;
        public double z;

        /*public static Vector3d Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            if (values == null || values.Length != 3) throw new ArgumentException();
            Vector3d results;
            results.x = double.Parse(values[0]);
            results.y = double.Parse(values[1]);
            results.z = double.Parse(values[2]);
            return results;
        }

        public Vector3d(string delimitedString)
        {
            Vector3d parse = Vector3d.Parse(delimitedString);
            this.x = parse.x;
            this.y = parse.y;
            this.z = parse.z;
        }

        public static Vector3d[] ParseArray(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);
            if (values == null || values.Length < 3 || values.Length % 3 != 0) throw new ArgumentException();

            int arraySize = values.Length / 3;
            Vector3d[] results = new Vector3d[arraySize];

            int j = 0;
            for (int i = 0; i < results.Length; i++)
            {
                results[i].x = double.Parse(values[j]); j++;
                results[i].y = double.Parse(values[j]); j++;
                results[i].z = double.Parse(values[j]); j++;
            }
            return results;
        }*/

        public Vector3d(double Vx, double Vy, double Vz)
        {
            x = Vx;
            y = Vy;
            z = Vz;
        }

        public Vector3d(Quaternion axisAngle)
        {

            double angleRadians = 0; // this value will be lost, only the axis is kept
            Vector3d result = axisAngle.GetAxisAngle(ref angleRadians);
            x = result.x;
            y = result.y;
            z = result.z;
        }

        public static Vector3d Zero()
        {
            Vector3d v;
            v.x = v.y = v.z = 0d;
            return v;
        }

        public static Vector3d MaxValue
        {
            get
            {
                Vector3d v;
                v.x = v.y = v.z = double.MaxValue;
                return v;
            }
        }

        public static Vector3d Up()
        {
            Vector3d v;
            v.x = 0d;
            v.y = 1d;
            v.z = 0d;
            return v;
        }

        public static Vector3d Right()
        {
            Vector3d v;
            v.x = 1d;
            v.y = 0d;
            v.z = 0d;
            return v;
        }

        public static Vector3d Forward()
        {
            Vector3d v;
            v.x = 0d;
            v.y = 0d;
            v.z = 1d;
            return v;
        }

        public double Length
        {
            get { return GetLength(this); }
        }

        public double LengthSquared()
        {
            return Vector3d.GetLengthSquared(this);
        }

        public void ZeroVector()
        {
            x = y = z = 0d;
        }

        public static void OrthoNormalize(ref Vector3d normal, ref Vector3d tangent)
        {
            normal = Normalize(normal);
            Vector3d proj = Scale(DotProduct(tangent, normal));
            tangent = tangent - proj;
            tangent = Normalize(tangent);
        }

        public double Normalize()
        {
            double l = Length;
            if (l == 0) return 0d; //  new Vector3d(0, 0, 0);
            double inverse = 1.0d / l;
            x *= inverse;
            y *= inverse;
            z *= inverse;
            return l;
        }
        public static Vector3d Normalize(Vector3d vec)
        {
            double dummy;
            return Normalize(vec, out dummy);
        }

        public static Vector3d Normalize(Vector3d vec, out double length)
        {
            double t = vec.Normalize();
            length = t;
            return vec;
        }

        public static Vector3d[] TransformNormalArray(Vector3d[] v, Matrix m)
        {
            Vector3d[] result = new Vector3d[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = TransformNormal(v[i], m);

            return result;
        }

        public static Vector3d[] TransformCoordArray(Vector3d[] v, Matrix m)
        {
            Vector3d[] result = new Vector3d[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = TransformCoord(v[i], m);

            return result;
        }
        /// <summary>
        /// 3x3 matrix transform but assumes the vector is a normal and so only
        /// scaling and rotation will be applied, not translation
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector3d TransformNormal(Vector3d v, Matrix m)
        {
            Vector3d result;
            if (m.IsNullOrEmpty())
            {
                result.x = v.x;
                result.y = v.y;
                result.z = v.z;
                return result;
            }
            result.x = (v.x * m.M11) + (v.y * m.M21) + (v.z * m.M31);
            result.y = (v.x * m.M12) + (v.y * m.M22) + (v.z * m.M32);
            result.z = (v.x * m.M13) + (v.y * m.M23) + (v.z * m.M33);
            return result;
        }

        public static Vector3d TransformNormal(Vector3d v, Quaternion q)
        {
            if (q.Equals(Quaternion.Identity()) || q.IsNullOrEmpty() || v.IsNullOrEmpty())
                return v;

            return TransformNormal(v, Quaternion.ToMatrix(q));
        }

        /// <summary>
        /// 3x4 matrix transform.  This is not intended to be used with a 4x4 matrix such as a projection matrix
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector3d TransformCoord(Vector3d v, Matrix m)
        {
            if (m.Equals(Matrix.Identity()) || m.IsNullOrEmpty() || v.IsNullOrEmpty())
                return v;

            Vector3d result;
            result.x = (v.x * m.M11) + (v.y * m.M21) + (v.z * m.M31) + m.M41;
            result.y = (v.x * m.M12) + (v.y * m.M22) + (v.z * m.M32) + m.M42;
            result.z = (v.x * m.M13) + (v.y * m.M23) + (v.z * m.M33) + m.M43;
            return result;
        }

        public static Vector3d TransformCoord(Vector3d v, double M11, double M12, double M13,
                                                                double M21, double M22, double M23,
                                                                double M31, double M32, double M33,
                                                                double M41, double M42, double M43)
        {
            Vector3d result;
            result.x = (v.x * M11) + (v.y * M21) + (v.z * M31) + M41;
            result.y = (v.x * M12) + (v.y * M22) + (v.z * M32) + M42;
            result.z = (v.x * M13) + (v.y * M23) + (v.z * M33) + M43;
            return result;
        }

        public static Vector3d TransformCoord(Vector3d v, Quaternion q)
        {

            if (q.IsNullOrEmpty() || v.IsNullOrEmpty())
                return v;

            return TransformCoord(v, Quaternion.ToMatrix(q));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDistance3d(Vector3d v, Vector3d v2)
        {
            return Math.Sqrt(GetDistance3dSquared(v, v2));
        }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetLength(double x, double y, double z)
        {
            return Math.Sqrt(GetLengthSquared(x, y, z));
        }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetLength(Vector3d v)
        {
            return Math.Sqrt(GetLengthSquared(v));
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetLengthSquared(double x, double y, double z)
        {
            return (x * x) + (y * y) + (z * z);
        }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetLengthSquared(Vector3d v)
        {
            return GetLengthSquared(v.x, v.y, v.z);
        }
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double GetDistance3dSquared(Vector3d v1, Vector3d v2)
        {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;
            double dz = v1.z - v2.z;
            return GetLengthSquared(dx, dy, dz);
        }

        /// <summary>
        /// Returns angle between two vectors in radians.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double AngleBetweenVectors(Vector3d v1, Vector3d v2)
        {
            double dot = DotProduct(v1, v2);
            double vectorsMagnitude = v1.Length * v2.Length;
            double angleRadians = Math.Acos(dot / vectorsMagnitude);

#if DEBUG
        //if (v1 == Vector3d.Up())
            //   System.Diagnostics.Debug.WriteLine("Determining if v2 is parallel to Up vector");
#endif
            if (double.IsNaN(angleRadians))
                return 0;
            else
                return angleRadians;
        }

        public static bool AreParallel(Vector3d v1, Vector3d v2, double epsilon)
        {
            return AngleBetweenVectors(v1, v2) <= epsilon;
        }

        /// <summary>
        /// Flips a vector.  Note: To avoid confusion, I've deleted the Vector3d.Inverse() function altogether
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3d Negate(Vector3d v)
        {
            Vector3d result;
            result.x = -v.x;
            result.y = -v.y;
            result.z = -v.z;
            return result;
        }

        /// <summary>
        /// dot productive is commutative (i.e.  v1 dot v2 == v2 dot v1)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double DotProduct(Vector3d v1, Vector3d v2)
        {
            return (v1.x * v2.x + v1.y * v2.y + v1.z * v2.z);
        }

        /// <summary>
        /// cross product is NOT commutative (ie.. v1 cross v2 != v2 cross v1)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector3d CrossProduct(Vector3d v1, Vector3d v2)
        {
            Vector3d vResult;
            vResult.x = v1.y * v2.z - v1.z * v2.y;
            vResult.y = v1.z * v2.x - v1.x * v2.z;
            vResult.z = v1.x * v2.y - v1.y * v2.x;
            return vResult;
        }

        public static Vector3d Subtract(Vector3d v1, Vector3d v2)
        {
            Vector3d result;
            result.x = v1.x - v2.x;
            result.y = v1.y - v2.y;
            result.z = v1.z - v2.z;
            return result;
        }

        public static Vector3d Add(Vector3d v1, Vector3d v2)
        {
            Vector3d result;
            result.x = v1.x + v2.x;
            result.y = v1.y + v2.y;
            result.z = v1.z + v2.z;
            return result;
        }

        public static Vector3d Scale(Vector3d v1, double scale)
        {
            Vector3d result;
            result.x = v1.x * scale;
            result.y = v1.y * scale;
            result.z = v1.z * scale;
            return result;
        }

        public static Vector3d Scale(double scale)
        {
            Vector3d result;
            result.x = scale;
            result.y = scale;
            result.z = scale;
            return result;
        }

        // clamp the vector's magnitude (length) to the limit length
        public static Vector3d Limit(Vector3d vec, double limit)
        {
            if (vec.Length > limit)
                return Normalize(vec) * limit;

            return vec;
        }


        // yes you can do spherical interpolation between two vectors
        // http://keithmaggio.wordpress.com/2011/02/15/math-magician-lerp-slerp-and-nlerp/
        public static Vector3d Slerp(Vector3d start, Vector3d end, float weight)
        {
            throw new System.NotImplementedException("uncomment Utitlities.MathHelper.Clamp after Boids tests");
            // Dot product - the cosine of the angle between 2 vectors.
            double dot = Vector3d.DotProduct(start, end);
            // Clamp it to be in the range of Acos()
            //            Utilities.MathHelper.Clamp(dot, -1.0f, 1.0f);
            // Acos(dot) returns the angle between start and end,
            // And multiplying that by percent returns the angle between
            // start and the final result.
            double theta = Math.Acos(dot) * weight;
            Vector3d RelativeVec = end - start * dot;
            RelativeVec.Normalize();     // Orthonormal basis
                                         // The final result.
            return ((start * Math.Cos(theta)) + (RelativeVec * Math.Sin(theta)));
        }

        // http://keithmaggio.wordpress.com/2011/02/15/math-magician-lerp-slerp-and-nlerp/
        // Nlerp: Nlerp is our solution to Slerp�s computational cost. Nlerp also handles 
        // rotation and is much less computationally expensive, however it, too has it�s drawbacks.
        // Both travel a torque-minimal path, but Nlerp is commutative where Slerp is not, and 
        // Nlerp aslo does not maintain a constant velocity, which, in some cases, may be a 
        // desired effect. Implementing Nlerp in place of some Slerp calls may produce the same 
        // effect and even save on some FPS. However, with every optimization, using this improperly
        // may cause undesired effects. Nlerp should be used more, but it doesn�t mean cut out Slerp 
        // all together. Nlerp is very easy, too. Just normalize the result from Lerp()!
        public static Vector3d NLerp(Vector3d start, Vector3d end, double weight)
        {
            Vector3d result = Lerp(start, end, weight);
            result.Normalize();
            return result;
        }

        /// <summary>
        /// TODO: In my actor waypoint following, I should be using Lerp and computing a weight based on the total elapsed
        /// to get to my end point and this way regardless of frame rate or even alt_tab where tons of time has passed, It'll not overshoot
        /// although, if there are linked waypoints, we would need to subtract remaining time to get the actor to move to the next 
        /// i'll have to double check my code.  Also when alt_tabbed the simulation should keep going, just rendering should stop.  Or
        /// if i do pause the simualtion, on re-start the elapsed values should pause and recalibrate on resume as well.
        /// note: also in my spline following, rather than use waypoints and going from point to point, i could just based on elapsed, 
        /// input the value and get the new postion on th espline dynamically and never worry about overshooting/undershooting anything
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="weight">typically 0 - 1.0 but if outside the bounds, results in under or overshoot along that path e.g. 2.0 would be twice as far as the end from the start. 
        /// Typically you could compute weight as i / N  where i is the current itterator count and N is the max number of itterations
        /// so 0 to 20, step 2 would be  2 / 20  and that would result in weight falling in the range of 0.0 to 1.0</param>
        /// <returns></returns>
        public static Vector3d Lerp(Vector3d start, Vector3d end, double weight)
        {
            return (start * (1.0d - weight)) + (end * weight);
        }

        private static Vector3d Lerp(Vector3d start, Vector3d end, double step, double maxSteps)
        {
            return Lerp(start, end, step / maxSteps);
        }

        /// <summary>
        /// Accelerates from start and slows down towards end.
        /// http://sol.gfxile.net/interpolation/
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static Vector3d LerpSmoothStep(Vector3d start, Vector3d end, double step, double maxSteps)
        {
            return Lerp(start, end, SmoothStep(step / maxSteps));
        }

        public static Vector3d LerpSmoothStep(Vector3d start, Vector3d end, double weight)
        {
            return Lerp(start, end, SmoothStep(weight));
        }

        public static Vector3d LerpSmoothAcceleration(Vector3d start, Vector3d end, double weight)
        {
            return Lerp(start, end, SmoothAcceleration(weight));
        }

        public static Vector3d LerpSmoothDeceleration(Vector3d start, Vector3d end, double weight)
        {
            return Lerp(start, end, SmoothDeceleration(weight));
        }

        /// <summary>
        /// Adds acceleration and deceleration to the interpolation
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double SmoothStep(double weight)
        {
            return (weight * weight * (3d - 2d * weight));
        }

        /// <summary>
        /// Adds acceleration but no deceleration
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double SmoothAcceleration(double weight)
        {
            return weight * weight;
        }

        /// <summary>
        /// Adds deceleration but no acceleration
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double SmoothDeceleration(double weight)
        {
            return 1d - (1d - weight) * (1d - weight) * (1d - weight);
        }

        /// <summary>
        /// One rather handy algorithm, especially when you don't necessarily
        /// know how the target will behave in the future (such as a camera
        /// tracking the player's character), is to apply weighted average
        /// to the value.
        /// where 'weight' is the current value, w is the value towards which
        /// we want to move, and N is the slowdown factor. The higher N, the
        /// slower 'weight' approaches w.
        /// http://sol.gfxile.net/interpolation/
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double WeightedAverage(double weight)
        {
            // TODO: return ((weight* (N - 1)) + w) / N; 
            return 0;
        }

        public Vector3d ProjectOnToPlane(Vector3d planeNormal)
        {
            Vector3d result = this;

            double sqrMag = Vector3d.DotProduct(planeNormal, planeNormal);
            if (sqrMag > double.Epsilon)
            {
                double dot = Vector3d.DotProduct(this, planeNormal);
                result.x = this.x - planeNormal.x * dot / sqrMag;
                result.y = this.y - planeNormal.y * dot / sqrMag;
                result.z = this.z - planeNormal.z * dot / sqrMag;
            }

            return result;
        }

        public static Vector3d FromTV3DVector(Vector3d v)
        {
            Vector3d result;
            result.x = v.x;
            result.y = v.y;
            result.z = v.z;
            return result;
        }

        public static Vector3d operator -(Vector3d v1)
        {
            return Negate(v1);
        }

        public static Vector3d operator -(Vector3d v1, Vector3d v2)
        {
            return Subtract(v1, v2);
        }

        public static Vector3d operator +(Vector3d v1, Vector3d v2)
        {
            return Add(v1, v2);
        }

        // Multiplying a quaternion q with a vector v applies the q-rotation to v
        public static Vector3d operator *(Vector3d vec, Quaternion quat)
        {
            // http://content.gpwiki.org/index.php/OpenGL%3aTutorials%3aUsing_Quaternions_to_represent_rotation#Rotating_vectors
            Vector3d vn = Vector3d.Normalize(vec);

            Quaternion vecQuat = new Quaternion(vn.x, vn.y, vn.z, 0.0d);
            Quaternion resultQuat = vecQuat * Quaternion.Conjugate(quat);

            resultQuat = quat * resultQuat;

            return new Vector3d(resultQuat.X, resultQuat.Y, resultQuat.Z);
        }

        public static Vector3d operator *(Vector3d v1, Vector3d v2)
        {
            Vector3d result;
            result.x = v1.x * v2.x;
            result.y = v1.y * v2.y;
            result.z = v1.z * v2.z;

            return result;
        }

        public static Vector3d operator *(Vector3d v1, double value)
        {
            return Scale(v1, value);
        }
        public static Vector3d operator *(double value, Vector3d v1)
        {
            return Scale(v1, value);
        }
        public static Vector3d operator /(Vector3d v1, double value)
        {
            if (value == 0) return Vector3d.Zero();

            Vector3d result;
            result.x = v1.x / value;
            result.y = v1.y / value;
            result.z = v1.z / value;

            return result;
        }

        // March.11.2024 - this was only ever used by Ray.css for finding the inverse direction vector.
        //                The problem was invesrse_direction = new Vector3d(1d / v.x, 1d/v.y, 1d/.vz) 
        //                is not the same as what we get when calling this overloaded operator because
        //                we are assigning 0 inappropriately.
        //public static Vector3d operator /(double value, Vector3d v1)
        //{

        //	Vector3d result;

        //    // July.10.2012 to avoid divide by zero use ternary ?: to assign 0 or 1 / v 
        //	result.x = (v1.x == 0d) ? 0d : value / v1.x;
        //	result.y = (v1.y == 0d) ? 0d : value / v1.y;
        //	result.z = (v1.z == 0d) ? 0d : value / v1.z;

        //	return result;
        //}

        public static bool operator ==(Vector3d v1, Vector3d v2)
        {
            return (v1.x == v2.x && v1.y == v2.y && v1.z == v2.z);
        }

        public static bool operator !=(Vector3d v1, Vector3d v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Vector3d))
                return false;

            return (this == (Vector3d)obj);
        }

        public bool Equals(Vector3d v)
        {
            return this.x == v.x && this.y == v.y && this.z == v.z;
        }

        public bool IsNullOrEmpty()
        {
            return (x == 0 && y == 0 && z == 0);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            string delimiter = ",";// keymath.ParseHelper.English.XMLAttributeDelimiter;
            return string.Format("{0}{1}{2}{3}{4}", x, delimiter,
                                                        y, delimiter,
                                                        z);
        }

        /*  public static string ToString(Vector3d[] vecArray)
            {

                if (vecArray == null || vecArray.Length == 0) return null;

                string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
                string result = string.Empty;
                System.Text.StringBuilder sb = new System.Text.StringBuilder(result);

                for (int i = 0; i < vecArray.Length; i++)
                {
                    sb.Append(vecArray[i].ToString());
                    if (i != vecArray.Length - 1)
                        // append delimiter. NOTE: same delimiter is used even between vectors and not just their elements
                        sb.Append(delimiter);
                }
                result = sb.ToString();
                return result;
            }
            */

        // TODO: i think the thing to do is move this out from here and into the PropertyBags
        //#region ICustomTypeDescriptor Members
        //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        //{
        //    bool filtering = (attributes != null && attributes.Length > 0);
        //    PropertyDescriptorCollection props;

        //    // Create the property collection and filter
        //    props = new PropertyDescriptorCollection(null);
        //    foreach (PropertyDescriptor prop in
        //        TypeDescriptor.GetProperties(
        //        this, attributes, true))
        //    {
        //        props.Add(prop);
        //    }

        //    // add public fields to property description collection
        //    FieldInfo[] allFields = this.GetType().GetFields();
        //    foreach (FieldInfo field in this.GetType().GetFields())
        //    {
        //        // at this point we wind up adding a value type
        //        // and there's no way to call the value changed handler for the current instance
        //        // that's why we should be changing the entire Vector3d for the PropertySpec
        //        // and it's the PropertySpec's SetValue that should be firing
        //        FieldPropertyDescriptor fieldDesc =
        //            new FieldPropertyDescriptor(ref this, field);
        //        //if (!filtering ||
        //        //    fieldDesc.Attributes.Contains(attributes))

        //        fieldDesc.AddValueChanged(this, PropertyGridFieldChanged);
        //        props.Add(fieldDesc);
        //    }

        //    return props;
        //}

        //// i think this instance of the vector doesn't get called either... it's a value type
        //internal void PropertyGridFieldChanged(object sender, EventArgs e)
        //{
        //    this.x = 0;
        //    this.y = 0;
        //    this.z = 0;
        //}

        //AttributeCollection ICustomTypeDescriptor.GetAttributes()
        //{
        //    //throw new NotImplementedException();
        //    return null;
        //}

        //string ICustomTypeDescriptor.GetClassName()
        //{
        //    throw new NotImplementedException();
        //}

        //string ICustomTypeDescriptor.GetComponentName()
        //{
        //    throw new NotImplementedException();
        //}

        //TypeConverter ICustomTypeDescriptor.GetConverter()
        //{
        //    throw new NotImplementedException();
        //}

        //EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        //{
        //    throw new NotImplementedException();
        //}

        //PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        //{
        //    //throw new NotImplementedException();
        //    return null;
        //}

        //object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        //{
        //    throw new NotImplementedException();
        //}

        //EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        //{
        //    throw new NotImplementedException();
        //}

        //EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        //{
        //    throw new NotImplementedException();
        //}

        //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        //{
        //    throw new NotImplementedException();
        //    //return GetProperties(null);
        //}

        //object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        //{
        //    // TODO: here I should be returning the PropertySpec
        //    return this;
        //}

        //#endregion
    } // end class Vector3d
		

    //public class FieldPropertyDescriptor : PropertyDescriptor
    //{
    //    private Vector3d mTarget;
    //    FieldInfo fieldInfo;
    //    public override Type ComponentType { get { return fieldInfo.DeclaringType; } }
    //    public override bool IsReadOnly { get { return false; } }            
    //    public override Type PropertyType { get { return fieldInfo.FieldType; } }            

    //    //public FieldPropertyDescriptor(FieldInfo fieldInfo) : base(fieldInfo.Name, 
    //    //    (Attribute[])fieldInfo.GetCustomAttributes(true)) 
    //    //{ 
    //    //    this.fieldInfo = fieldInfo; 
    //    //}

    //    public FieldPropertyDescriptor(ref Vector3d target, FieldInfo fieldInfo)
    //        : base(fieldInfo.Name,
    //        (Attribute[])fieldInfo.GetCustomAttributes(typeof(Attribute), true))
    //    {
    //        this.fieldInfo = fieldInfo;
    //        mTarget = target;
    //    }


    //    public override bool CanResetValue(object component) { return false; }            
    //    public override object GetValue(object component) 
    //    {
    //        object value = fieldInfo.GetValue(component);
    //        return fieldInfo.GetValue(component);


    //        //Type type = value.GetType();
    //        //Type converterType = typeof(MyConverter<,>).MakeGenericType(type, typeof(FieldsToProperties));
    //        //ConversionDelegate dlg = delegate(object o)
    //        //{
    //        //    return new FieldsToProperties(o);
    //        //};

    //        //return converterType.InvokeMember(
    //        //"Convert",
    //        //BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
    //        //null,
    //        //Activator.CreateInstance(converterType),
    //        //new object[] { value, dlg });


    //    }            
    //    public override void ResetValue(object component) { }    


    //    public override void SetValue(object component, object value) 
    //    {

    //        Vector3d v = (Vector3d)component;


    //        if (this.Name == "x")
    //        {
    //            mTarget.x = (double)value;
    //        }
    //        else if (this.Name == "y")
    //        {
    //            mTarget.y = (double)value;
    //        }
    //        else if (this.Name == "z")
    //        {
    //            mTarget.z = (double)value;
    //        }

    //        fieldInfo.SetValue(mTarget, value);
    //        OnValueChanged(mTarget, EventArgs.Empty);
    //        OnValueChanged(this, EventArgs.Empty);
    //    }            
    //    public override bool ShouldSerializeValue(object component) { return true; }            
    //    public override int GetHashCode() { return fieldInfo.GetHashCode(); }            
    //    public override bool Equals(object obj) 
    //    { 
    //        if (obj == null)                  return false; 
    //        if (GetType() != obj.GetType()) 
    //            return false; return (obj as FieldPropertyDescriptor).fieldInfo.Equals(fieldInfo); 
    //    }


    //    delegate object ConversionDelegate(object a);
    //    class MyConverter<From, To>
    //    {
    //        public object Convert(object src, ConversionDelegate dlg)
    //        {
    //            if (src.GetType().IsArray)
    //            {
    //                From[] a = (From[])src;
    //                To[] b = new To[a.Length];
    //                for (int i = 0; i < a.Length; i++)
    //                {
    //                    b[i] = (To)dlg(a[i]);
    //                }
    //                return b;
    //            }
    //            else
    //            {
    //                return dlg((From)src);
    //            }
    //        }
    //    }

    //    //public override object GetValue(object component)
    //    //{
    //    //    object value = fieldInfo.GetValue(component);

    //    //    Type type = value.GetType();
    //    //    bool isArray = type.IsArray;
    //    //    if (isArray) type = type.GetElementType();


    //    //    //if (type == typeof(IntPtr))
    //    //    //{
    //    //    //    return (new MyConverter<IntPtr, MemoryAddress>()).Convert(value, delegate(object o)
    //    //    //    {
    //    //    //        IntPtr ip = (IntPtr)o;
    //    //    //        return new MemoryAddress(ip);
    //    //    //    });
    //    //    //}

    //    //    if (type.DeclaringType == typeof(Native))
    //    //    {
    //    //        //if (type == typeof(Native.UNICODE_STRING))
    //    //        //{I'm
    //    //        //    return (new MyConverter<Native.UNICODE_STRING, string>()).Convert(value, delegate(object o)
    //    //        //    {
    //    //        //        Native.UNICODE_STRING us = (Native.UNICODE_STRING)o;
    //    //        //        return RemoteReader.ReadStringUni(MainForm.Doc.ProcessHandle, us.Buffer, us.Length);
    //    //        //    });
    //    //        //}

    //    //        //if (type == typeof(Native.HANDLE))
    //    //        //{
    //    //        //    return (new MyConverter<Native.HANDLE, string>()).Convert(value, delegate(object o)
    //    //        //    {
    //    //        //        Native.HANDLE h = (Native.HANDLE)o;
    //    //        //        return "0x" + h.Handle.ToString("X8");
    //    //        //    });
    //    //        //}

    //    //        if (!type.IsEnum)
    //    //        {
    //    //            Type converterType = typeof(MyConverter<,>).MakeGenericType(type, typeof(FieldsToProperties));
    //    //            ConversionDelegate dlg = delegate(object o)
    //    //            {
    //    //                return new FieldsToProperties(o);
    //    //            };

    //    //            return converterType.InvokeMember(
    //    //              "Convert",
    //    //              BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
    //    //              null,
    //    //              Activator.CreateInstance(converterType),
    //    //              new object[] { value, dlg });


    //    //            //return new FieldsToProperties(value);
    //    //        }
    //    //    }

    //    //    return value;
    //    //}
    //}


    //public class FieldsToProperties : ICustomTypeDescriptor
    //{
    //    #region Private fields

    //    private object _target;

    //    #endregion
    //    #region Construction

    //    public FieldsToProperties(object target)
    //    {
    //        if (target == null) throw new ArgumentNullException("target");
    //        _target = target;
    //    }

    //    #endregion
    //    #region Object overrides

    //    public override string ToString()
    //    {
    //        return string.Format("({0})", _target.GetType().Name);
    //    }

    //    #endregion
    //    #region ICustomTypeDescriptor Members

    //    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
    //    {
    //        // Properties belong to the target object
    //        return _target;
    //    }

    //    AttributeCollection ICustomTypeDescriptor.GetAttributes()
    //    {
    //        // Gets the attributes of the target object
    //        return TypeDescriptor.GetAttributes(this, true);
    //    }

    //    string ICustomTypeDescriptor.GetClassName()
    //    {
    //        // Gets the class name of the target object
    //        return TypeDescriptor.GetClassName(this, true);
    //    }

    //    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
    //    {
    //        return ((ICustomTypeDescriptor)this).GetProperties(null);
    //    }

    //    private class FilterCache
    //    {
    //        public Attribute[] Attributes;
    //        public PropertyDescriptorCollection FilteredProperties;

    //        public FilterCache(Attribute[] att, PropertyDescriptorCollection props)
    //        {
    //            Attributes = att;
    //            FilteredProperties = props;
    //        }

    //        public bool IsValid(Attribute[] other)
    //        {
    //            if (other == null || Attributes == null) return false;

    //            if (Attributes.Length != other.Length) return false;

    //            for (int i = 0; i < other.Length; i++)
    //            {
    //                if (!Attributes[i].Match(other[i])) return false;
    //            }

    //            return true;
    //        }
    //    }

    //    private PropertyDescriptorCollection _propCache;
    //    private FilterCache _filterCache;

    //    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    //    {
    //        bool filtering = (attributes != null && attributes.Length > 0);
    //        PropertyDescriptorCollection props = _propCache;
    //        FilterCache cache = _filterCache;

    //        // Use a cached version if possible
    //        if (filtering && cache != null && cache.IsValid(attributes))
    //            return cache.FilteredProperties;

    //        if (!filtering && props != null)
    //            return props;

    //        // Create the property collection and filter
    //        props = new PropertyDescriptorCollection(null);
    //        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this, attributes, true))
    //        {
    //            props.Add(prop);
    //        }

    //        foreach (FieldInfo field in _target.GetType().GetFields())
    //        {
    //            FieldPropertyDescriptor fieldDesc = new FieldPropertyDescriptor(field., field);
    //            if (!filtering || fieldDesc.Attributes.Contains(attributes))
    //                props.Add(fieldDesc);
    //        }

    //        // Store the computed properties
    //        if (filtering)
    //        {
    //            cache = new FilterCache(attributes, props);
    //            _filterCache = cache;
    //        }
    //        else
    //        {
    //            _propCache = props;
    //        }

    //        return props;
    //    }

    //    string ICustomTypeDescriptor.GetComponentName()
    //    {
    //        return null;
    //    }

    //    TypeConverter ICustomTypeDescriptor.GetConverter()
    //    {
    //        return null;
    //    }

    //    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
    //    {
    //        return null;
    //    }

    //    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
    //    {
    //        return null;
    //    }

    //    object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
    //    {
    //        return null;
    //    }


    //    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
    //    {
    //        return null;
    //    }

    //    EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
    //    {
    //        return null;
    //    }

    //    #endregion
    //}

#if USE_STRUCT
    public struct Quaternion
#else
    public class Quaternion
#endif
    {
        private double[] _quat;  // subscripts 0 = x, 1=y, 2=z, 3=w

        /*public static Quaternion Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            if (values == null || values.Length != 4) throw new ArgumentException();
            return new Quaternion(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2]), double.Parse(values[3]));
        }

        public static Quaternion[] ParseArray(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            if (values == null || values.Length < 4 || values.Length % 4 != 0) throw new ArgumentException();

            int arraySize = values.Length / 4;
            Quaternion[] results = new Quaternion[arraySize];

            int j = 0;
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = new Quaternion();
                results[i]._quat[0] = double.Parse(values[j]); j++;
                results[i]._quat[1] = double.Parse(values[j]); j++;
                results[i]._quat[2] = double.Parse(values[j]); j++;
                results[i]._quat[3] = double.Parse(values[j]); j++;
            }
            return results;
        }*/

        //Quaternions add a fourth element to the [ x, y, z] values that define a vector, 
        //resulting in arbitrary 4D vectors. However, the following illustrates how each 
        //element of a unit quaternion relates to an axis-angle rotation (where q 
        //represents a unit quaternion (x, y, z, w), axis is normalized, and theta is the
        //desired CCW rotation about the axis):

        //q.x = sin(theta/2) * axis.x
        //q.y = sin(theta/2) * axis.y
        //q.z = sin(theta/2) * axis.z
        //q.w = cos(theta/2)
        public Quaternion(bool identity)
        {
            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat = new double[4];
            if (identity)
                _quat[3] = 1.0; // for no rotation, w = 1 typically
                                // else it's "Empty"
        }


        // http://msdn.microsoft.com/en-us/library/bb205417(VS.85).aspx
        // YawPitchRoll correctly implemented from John Ratcliff's Code Suppository http://codesuppository.blogspot.com/
        // note: he indicates an error with the one in d3d
        /// <summary>
        /// Yaw pitch roll represents Y, X, Z and NOT X,Y,Z 
        /// </summary>
        /// <remarks>This is broken.  Build a Matrix rotationMatrix instead</remarks>
        /// <param name="radianYaw">the Y axis in radians</param>
        /// <param name="radianPitch">the X axis in radians</param>
        /// <param name="radianRoll">the Z axis in radians</param>
        public Quaternion(double radianYaw, double radianPitch, double radianRoll) : this()
        {
            // TODO: This overload is broken. If i use RotationMatrix and create Quaternion from that
            //       it works.  But below is broken.

            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat = new double[4];
            //double c1 = Math.Cos(radianYaw / 2);
            //double s1 = Math.Sin(radianYaw / 2);
            //double c2 = Math.Cos(radianPitch / 2);
            //double s2 = Math.Sin(radianPitch / 2);
            //double c3 = Math.Cos(radianRoll / 2);
            //double s3 = Math.Sin(radianRoll / 2);
            //double c1c2 = c1 * c2;
            //double s1s2 = s1 * s2;
            //_quat[3] = c1c2 * c3 - s1s2 * s3;
            //_quat[0] = c1c2 * s3 + s1s2 * c3;
            //_quat[1] = s1 * c2 * c3 + c1 * s2 * s3;
            //_quat[2] = c1 * s2 * c3 - s1 * c2 * s3;
            ////_quat[3] = c1c2 * c3 - s1s2 * s3;
            ////_quat[2] = c1c2 * s3 + s1s2 * c3;
            ////_quat[1] = s1 * c2 * c3 + c1 * s2 * s3;
            ////_quat[0] = c1 * s2 * c3 - s1 * c2 * s3;

            double sinY, cosY, sinP, cosP, sinR, cosR;
            double halfYaw = 0.5d * radianYaw;
            sinY = Math.Sin(halfYaw);
            cosY = Math.Cos(halfYaw);

            double halfPitch = 0.5d * radianPitch;
            sinP = Math.Sin(halfPitch);
            cosP = Math.Cos(halfPitch);

            double halfRoll = 0.5d * radianRoll;
            sinR = Math.Sin(halfRoll);
            cosR = Math.Cos(halfRoll);

            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat[0] = cosY * sinP * cosR + sinY * cosP * sinR;
            _quat[1] = sinY * cosP * cosR - cosY * sinP * sinR;
            _quat[2] = cosY * cosP * sinR - sinY * sinP * cosR;
            _quat[3] = cosY * cosP * cosR + sinY * sinP * sinR;

        }

        /// <summary>
        /// Creates a quaternion with the passed in component values. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <remarks> X,Y,Z do are not components of an axis vector.  
        /// If you wish to create a quaternion from radian axis vector values
        /// you should use the constructor that accepts a Vector3d axis and double angleRadians</remarks>
        public Quaternion(double x, double y, double z, double w) : this()
        {
            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat[0] = x;
            _quat[1] = y;
            _quat[2] = z;
            _quat[3] = w;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis">Unit vector representing the axis of rotation</param>
        /// <param name="angleRadians"></param>
        public Quaternion(Vector3d axis, double angleRadians) : this()
        {
            // xna's version
            axis.Normalize();
            double halfAngle = angleRadians * 0.5d;
            double sineHalfAngle = Math.Sin(halfAngle);

            _quat[0] = axis.x * sineHalfAngle;                   // x
            _quat[1] = axis.y * sineHalfAngle;                   // y
            _quat[2] = axis.z * sineHalfAngle;                   // z
            _quat[3] = Math.Cos(halfAngle);        // w

            // http://www.codeproject.com/KB/graphics/YLScsDrawing3d.aspx
            //double length = axis.Length;
            //const double epsilon = 0.0001;
            //if (length > epsilon)
            //{
            //    double halfAngle = angleRadians * 0.5d;
            //    double sineHalfAngle = Math.Sin(halfAngle);

            //     // divide by length to normalize first since axis must be unit vector
            //    _quat[0] = axis.x / length * sineHalfAngle;                   // x
            //    _quat[1] = axis.y / length * sineHalfAngle;                   // y
            //    _quat[2] = axis.z / length * sineHalfAngle;                   // z
            //    _quat[3] = Math.Cos(halfAngle);        // w
            //}
            //else
            //{
            //    _quat[0] = 0;                   // x
            //    _quat[1] = 0;                   // y
            //    _quat[2] = 0;                   // z
            //    _quat[3] = 1;
            //}
        }


        // TODO: must read this and verify my algos are optimized
        // http://www.edn.com/archives/1995/030295/05df3.htm
        // http://www.gamedev.net/topic/613595-quaternion-lookrotationlookat-up/
        // http://code.google.com/p/slimdx/source/browse/branches/lite/SlimMath/Quaternion.cs
        public Quaternion(Matrix rotationMatrix) : this()
        {
            /** construct Quaternion from a rotation matrix expressed as a triple
            of vectors, each one a row of the matrix.
            Code adapted from Shoemake's paper "Quaternions".
            */
            double tr, s, sinv;
            tr = rotationMatrix.M11 + rotationMatrix.M22 + rotationMatrix.M33;
            if (tr >= 0.0)
            {
                s = Math.Sqrt(tr + 1);
                sinv = 0.5 / s;
                _quat[0] = (rotationMatrix.M32 - rotationMatrix.M23) * sinv;
                _quat[1] = (rotationMatrix.M13 - rotationMatrix.M31) * sinv;
                _quat[2] = (rotationMatrix.M21 - rotationMatrix.M12) * sinv;
                _quat[3] = s * 0.5;
            }
            else if (rotationMatrix.M11 > rotationMatrix.M22 && rotationMatrix.M11 > rotationMatrix.M33)
            {
                s = Math.Sqrt(rotationMatrix.M11 - (rotationMatrix.M22 + rotationMatrix.M33) + 1);
                sinv = 0.5 / s;
                _quat[0] = s * 0.5;
                _quat[1] = (rotationMatrix.M12 + rotationMatrix.M21) * sinv;
                _quat[2] = (rotationMatrix.M31 + rotationMatrix.M13) * sinv;
                _quat[3] = (rotationMatrix.M32 - rotationMatrix.M23) * sinv;
            }
            else if (rotationMatrix.M22 > rotationMatrix.M33)
            {
                s = Math.Sqrt(rotationMatrix.M22 - (rotationMatrix.M33 + rotationMatrix.M11) + 1);
                sinv = 0.5 / s;
                _quat[0] = (rotationMatrix.M12 + rotationMatrix.M21) * sinv;
                _quat[1] = s * 0.5;
                _quat[2] = (rotationMatrix.M23 + rotationMatrix.M32) * sinv;
                _quat[3] = (rotationMatrix.M13 - rotationMatrix.M31) * sinv;
            }
            else
            {
                s = Math.Sqrt(rotationMatrix.M33 - (rotationMatrix.M11 + rotationMatrix.M22) + 1);
                sinv = 0.5 / s;
                _quat[0] = (rotationMatrix.M31 + rotationMatrix.M13) * sinv;
                _quat[1] = (rotationMatrix.M23 + rotationMatrix.M32) * sinv;
                _quat[2] = s * 0.5;
                _quat[3] = (rotationMatrix.M21 - rotationMatrix.M12) * sinv;
            }
        }

        /// <summary>
        /// LookAt Quaternion
        /// </summary>
        /// <param name="forward">The LookAt direction</param>
        /// <param name="up"></param>
        public Quaternion(Vector3d forward, Vector3d up) : this()
        {
            //forward = Vector3d.Normalize(forward);
            //up = Vector3d.Normalize(up);
            Vector3d.OrthoNormalize(ref forward, ref up);
            Vector3d right = Vector3d.CrossProduct(up, forward);

            _quat[3] = Math.Sqrt(1.0d + right.x + up.y + forward.z) * 0.5d;
            double w4_recip = 1.0d / (4.0d * _quat[3]);
            _quat[0] = (up.z - forward.y) * w4_recip;
            _quat[1] = (forward.x - right.z) * w4_recip;
            _quat[2] = (right.y - up.x) * w4_recip;
        }

        public void GetAxisAngle(out Vector3d axis, out double angle)
        {
            // http://content.gpwiki.org/index.php/OpenGL%3aTutorials%3aUsing_Quaternions_to_represent_rotation#Rotating_vectors
            double scale = Math.Sqrt(_quat[0] * _quat[0] + _quat[1] * _quat[1] + _quat[2] * _quat[2]);
            axis.x = _quat[0] / scale;
            axis.y = _quat[1] / scale;
            axis.z = _quat[2] / scale;

            angle = Math.Acos(_quat[3]) * 2.0d;
        }

        // http://www.euclideanspace.com/maths/algebra/vectors/lookat/index.htm
        //public static double LookAt(Vector3d target, Vector3d position, Vector3d eye, Vector3d up)
        // {
        // TODO: finish converting this function
        //// turn vectors into unit vectors 
        //n1 = (current - eye).norm();
        //n2 = (target - eye).norm();  
        //d = sfvec3f.dot(n1,n2); 
        //// if no noticable rotation is available return zero rotation
        //// this way we avoid Cross product artifacts 
        //if( d > 0.9998 ) return new sfquat( 0, 0, 1, 0 ); 
        //// in this case there are 2 lines on the same axis 
        //if(d < -0.9998){ 
        //    n1 = n1.Rotx( 0.5f ); 
        //    // there are an infinite number of normals 
        //    // in this case. Anyone of these normals will be 
        //    // a valid rotation (180 degrees). so rotate the curr axis by 0.5 radians this way we get one of these normals 
        //} 
        //sfvec3f axis = n1;
        //axis.cross(n2);
        //sfquat pointToTarget= new sfquat(1.0 + d,axis.x,axis.y,axis.z); 
        //pointToTarget.norm();
        //// now twist around the target vector, so that the 'up' vector points along the z axis
        //sfmatrix projectionMatrix=new sfmatrix();
        //double a = pointToTarget.x;
        //double b = pointToTarget.y;
        //double c = pointToTarget.z;
        //projectionMatrix.m00 = b*b+c*c;
        //projectionMatrix.m01 = -a*b;
        //projectionMatrix.m02 = -a*c;
        //projectionMatrix.m10 = -b*a;
        //projectionMatrix.m11 = a*a+c*c;
        //projectionMatrix.m12 = -b*c;
        //projectionMatrix.m20 = -c*a;
        //projectionMatrix.m21 = -c*b;
        //projectionMatrix.m22 = a*a+b*b;
        //sfvec3f upProjected = projectionMatrix.transform(up);
        //sfvec3f yaxisProjected = projectionMatrix.transform(new sfvec(0,1,0);
        //d = sfvec3f.dot(upProjected,yaxisProjected);
        //// so the axis of twist is n2 and the angle is arcos(d)
        ////convert this to quat as follows   
        //double s=Math.sqrt(1.0 - d*d);
        //sfquat twist=new sfquat(d,n2*s,n2*s,n2*s);
        //return sfquat.mul(pointToTarget,twist);

        //}


        public bool IsNan()
        {
            return double.IsNaN(_quat[0]) ||
                    double.IsNaN(_quat[1]) ||
                    double.IsNaN(_quat[2]) ||
                    double.IsNaN(_quat[3]);

            // NOTE: IEEEE 754 says the following will still return false. We must use .IsNaN() method 
            //	return  _quat[0] == double.NaN || 
            //			_quat[1] == double.NaN || 
            //			_quat[2] == double.NaN || 
            //			_quat[3] == double.NaN;
        }

        // http://answers.unity3d.com/questions/35541/problem-finding-relative-rotation-from-one-quatern.html
        public static Quaternion CreateRelativeRotation(Quaternion a, Quaternion b)
        {
            // TODO: Jan.31.2014 - never been tested
            Quaternion relative = Quaternion.Inverse(a) * b;
            return relative;
        }

        public static Quaternion CreateRotationTo(Vector3d source, Vector3d dest, Vector3d up)
        {
            Matrix m = Matrix.CreateLookAt(source, dest, up);
            // return new Quaternion (m);

            double w = Math.Sqrt(1.0d + m.M11 + m.M22 + m.M33) * 0.5d;
            double w4_recip = 1.0d / (4.0d * w);
            double x = (m.M32 - m.M23) * w4_recip;
            double y = (m.M13 - m.M31) * w4_recip;
            double z = (m.M21 - m.M12) * w4_recip;

            // TODO: here if the start and end rotations are nearly the same, it'll produce NaN and in that case
            // we should return a rotation of 0,0,0,1 

            Quaternion result = new Quaternion(x, y, z, w);

#if DEBUG
        if (result.IsNan())
            result = new Quaternion();
#endif

            return result;
        }

        // http://stackoverflow.com/questions/12435671/quaternion-lookat-function

        /// <summary>
        /// Creates a rotation quaternion that will orient the source entity to
        /// face a destination coordinate.  This is in effect a "LookAt" for entities.
        /// </summary>
        /// <param name="source">Normalized destination coordinate</param>
        /// <param name="dest">Normalized destination coordinate</param>
        /// <param name="up">Normalized up vector</param>
        /// <returns></returns>
        //         public static Quaternion GetRotationTo(Vector3d source, Vector3d dest, Vector3d up)
        //         {
        //             double dot = Vector3d.DotProduct(source, dest);
        //
        //             if (Math.Abs(dot - -1.0d) < 0.000001d)
        //             {
        //                 // vector source and dest point exactly in the opposite direction, 
        //                 // so it is a 180 degrees turn around the up-axis
        //                 return new Quaternion(up, Utilities.MathHelper.DEGREES_TO_RADIANS  * 180.0d);
        //             }
        //             if (Math.Abs(dot - 1.0d) < 0.000001d)
        //             {
        //                 // vector source and dest point exactly in the same direction
        //                 // so we return the identity quaternion
        //                 return Quaternion.Identity();
        //             }
        //
        //             double rotAngle = Math.Acos(dot);
        //             // TODO: why isn't the rotation axis the dir?  
        //             Vector3d rotAxis = Vector3d.CrossProduct(source, dest);
        //             rotAxis = Vector3d.Normalize(rotAxis);
        //             return new Quaternion(rotAxis, rotAngle);
        //          
        //         }

        // note: below seems similar as above but with different code. untested.
        // however what is interesting is the special cases it makes for directly forward or
        // directly backwards facing quats where it special cases the handling.
        // http://gamedev.stackexchange.com/questions/15070/orienting-a-model-to-face-a-target
        //public static Quaternion GetRotation(Vector3 source, Vector3 dest, Vector3 up)
        //{
        //    float dot = Vector3.Dot(source, dest);

        //    if (Math.Abs(dot - (-1.0f)) < 0.000001f)
        //    {
        //        // vector a and b point exactly in the opposite direction, 
        //        // so it is a 180 degrees turn around the up-axis
        //        return new Quaternion(up, MathHelper.ToRadians(180.0f));
        //    }
        //    if (Math.Abs(dot - (1.0f)) < 0.000001f)
        //    {
        //        // vector a and b point exactly in the same direction
        //        // so we return the identity quaternion
        //        return Quaternion.Identity;
        //    }

        //    float rotAngle = (float)Math.Acos(dot);
        //    Vector3 rotAxis = Vector3.Cross(source, dest);
        //    rotAxis = Vector3.Normalize(rotAxis);
        //    return Quaternion.CreateFromAxisAngle(rotAxis, rotAngle);
        //}

        /// <summary>
        /// The multiplication Identity Quaternion (the addition identity quaternion which we don't use is 0 (0,0,0)
        /// </summary>
        /// <returns></returns>
        public static Quaternion Identity()
        {
            Quaternion q = new Quaternion();
            q._quat[0] = 0;
            q._quat[1] = 0;
            q._quat[2] = 0;
            q._quat[3] = 1;

            return q;
        }

        public bool IsNullOrEmpty()
        {
            return (_quat[0] == 0 && _quat[1] == 0 && _quat[2] == 0 && _quat[3] == 0);
        }

        public Vector3d Up()
        {
            double xx = _quat[0] * _quat[0];
            double zz = _quat[2] * _quat[2];
            double xy = _quat[0] * _quat[1];
            double yz = _quat[1] * _quat[2];
            double wx = _quat[3] * _quat[0];
            double wz = _quat[3] * _quat[2];

            Vector3d result;
            result.x = 2.0 * (xy + wz);
            result.y = 1.0 - 2.0 * (xx + zz);
            result.z = 2.0 * (yz - wx);

            return result;
        }

        public Vector3d Forward()
        {
            double xx = _quat[0] * _quat[0];
            double yz = _quat[1] * _quat[2];
            double wx = _quat[3] * _quat[0];

            double xz = _quat[0] * _quat[2];
            double yy = _quat[1] * _quat[1];
            double wy = _quat[3] * _quat[1];

            Vector3d result;
            result.x = 2.0 * (xz + wy);
            result.y = 2.0 * (yz - wx);
            result.z = 1.0d - 2.0 * (xx + yy);

            return result;
        }

        public double X
        {
            get { return _quat[0]; }
            set { _quat[0] = value; }
        }

        public double Y
        {
            get { return _quat[1]; }
            set { _quat[1] = value; }
        }

        public double Z
        {
            get { return _quat[2]; }
            set { _quat[2] = value; }
        }

        public double W
        {
            get { return _quat[3]; }
            set { _quat[3] = value; }
        }

        public double Length
        {
            get { return Math.Sqrt(_quat[0] * _quat[0] + _quat[1] * _quat[1] + _quat[2] * _quat[2] + _quat[3] * _quat[3]); }
        }

        public static Quaternion Normalize(Quaternion quat)
        {
            double invLength = 1d / quat.Length;

            return new Quaternion(quat.X * invLength, quat.Y * invLength, quat.Z * invLength, quat.W * invLength);
        }

        // https://github.com/ehsan/ogre/blob/master/OgreMain/src/OgreQuaternion.cpp
        public static Quaternion Inverse(Quaternion quat)
        {
            double mag = quat.W * quat.W + quat.X * quat.X + quat.Y * quat.Y + quat.Z * quat.Z;
            if (mag > 0.0d)
            {
                double magInvNorm = 1.0d / mag;
                return new Quaternion(-quat.X * magInvNorm, -quat.Y * magInvNorm, -quat.Z * magInvNorm, quat.W * magInvNorm);
            }
            else
            {
                // return an invalid result to flag the error
                return new Quaternion(0d, 0d, 0d, 0d);
            }

        }

        /// <summary>
        /// Given a quaternion (x, y, z, w), this method returns the quaternion (-x, -y, -z, w). 
        /// </summary>
        /// <param name="quat"></param>
        /// <returns>Unliked in Negate(), here the W component is the only compponent not negated</returns>
        public static Quaternion Conjugate(Quaternion quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        /// <summary>
        /// Negates all components of the quaternion.
        /// </summary>
        /// <param name="quat"></param>
        /// <returns></returns>
        public static Quaternion Negate(Quaternion quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, -quat.W);
        }

        public static Quaternion Scale(Quaternion q1, double scale)
        {
            Quaternion result = new Quaternion
            {
                X = (scale * q1.X),
                Y = (scale * q1.Y),
                Z = (scale * q1.Z),
                W = (scale * q1.W)
            };
            return result;
        }

        // xna concatenate
        public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
        {
            double x = value2.X;
            double y = value2.Y;
            double z = value2.Z;
            double w = value2.W;
            double x2 = value1.X;
            double y2 = value1.Y;
            double z2 = value1.Z;
            double w2 = value1.W;
            double num = y * z2 - z * y2;
            double num2 = z * x2 - x * z2;
            double num3 = x * y2 - y * x2;
            double num4 = x * x2 + y * y2 + z * z2;
            Quaternion result = new Quaternion();
            result.X = x * w2 + x2 * w + num;
            result.Y = y * w2 + y2 * w + num2;
            result.Z = z * w2 + z2 * w + num3;
            result.W = w * w2 - num4;
            return result;
        }

        public static Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            Quaternion result = new Quaternion
            {
                X = (q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y),
                Y = (q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X),
                Z = (q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W),
                W = (q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z)
            };

            return result;
        }

        //public static Quaternion Multiply(Quaternion value1, Quaternion value2)
        //{
        //    Quaternion result = new Quaternion();
        //    double single8 = value1.X;
        //    double single7 = value1.Y;
        //    double single6 = value1.Z;
        //    double single5 = value1.W;
        //    double single4 = value2.X;
        //    double single3 = value2.Y;
        //    double single2 = value2.Z;
        //    double single1 = value2.W;
        //    double single12 = (single7 * single2) - (single6 * single3);
        //    double single11 = (single6 * single4) - (single8 * single2);
        //    double single10 = (single8 * single3) - (single7 * single4);
        //    double single9 = ((single8 * single4) + (single7 * single3)) + (single6 * single2);
        //    result.X = ((single8 * single1) + (single4 * single5)) + single12;
        //    result.Y = ((single7 * single1) + (single3 * single5)) + single11;
        //    result.Z = ((single6 * single1) + (single2 * single5)) + single10;
        //    result.W = (single5 * single1) - single9;

        //    return result;
        //}

        //public static Vector3d  Multiply(Quaternion q, Vector3d v)
        //{
        //    Vector3d tmp = Vector3d.Normalize (v);
        //    Quaternion tmpQuat = new Quaternion (tmp.x, tmp.y, tmp.z, 0D);

        //    Quaternion result = tmpQuat * Quaternion.Conjugate (q);
        //    result = q * result;

        //    return new Vector3d (result.X , result.Y , result.Z , result.W );

        //}

        public static double DotProduct(Quaternion q1, Quaternion q2)
        {
            return q1.W * q2.W + q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z;

        }

        public static Quaternion RotateTowards(Quaternion from, Quaternion to, double maxDegreesDelta)
        {
            double angle = Quaternion.Angle(from, to);
            if (angle == 0.0d) return to;
            return Slerp(from, to, Math.Min(1.0d, maxDegreesDelta / angle));
        }

        public static double Angle(Quaternion a, Quaternion b)
        {
            double dot = Math.Min(Math.Abs(DotProduct(a, b)), 1.0F);
            return IsEqualUsingDot(dot) ? 0.0d : Math.Acos(dot) * 2.0d * 57.2958d; //Utilities.MathHelper.RADIANS_TO_DEGREES;
        }

        // Is the dot product of two quaternions within tolerance for them to be considered equal?
        private static bool IsEqualUsingDot(double dot)
        {
            // Returns false in the presence of NaN values.
            return dot > 1.0d - double.Epsilon;
        }


        public Vector3d GetAxisAngle(ref double angleRadians)
        {
            Vector3d axis;
            angleRadians = 2.0 * Math.Acos(_quat[3]);
            double sinHalfAngle = Math.Sin(angleRadians * .5d);
            if (sinHalfAngle != 0) // check for divide by zero 
            {
                axis.x = _quat[0] / sinHalfAngle;
                axis.y = _quat[1] / sinHalfAngle;
                axis.z = _quat[2] / sinHalfAngle;

                // TODO: i think according to this gamedev, the result asis should be normalized
                // http://www.gamedev.net/topic/310603-quaternion-to-axis-angle-and-back/
            }
            else
            {
                axis.x = 0;
                axis.y = 0;
                axis.z = 1;
            }

            return axis;
        }

        // http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/index.htm
        public Vector3d GetEulerAngles(bool degrees)
        {
            Vector3d angles;
            double sqw = _quat[3] * _quat[3];
            double sqx = _quat[0] * _quat[0];
            double sqy = _quat[1] * _quat[1];
            double sqz = _quat[2] * _quat[2];
            double unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            double test = _quat[0] * _quat[1] + _quat[2] * _quat[3];

            if (test > 0.499 * unit)
            { // singularity at north pole
                angles.y = 2 * Math.Atan2(_quat[0], _quat[3]);
                angles.x = Math.PI / 2;
                angles.z = 0;
            }
            else if (test < -0.499 * unit)
            { // singularity at south pole
                angles.y = -2 * Math.Atan2(_quat[0], _quat[3]);
                angles.x = -Math.PI / 2;
                angles.z = 0;
            }
            else
            {
                angles.y = Math.Atan2(2 * _quat[1] * _quat[3] - 2 * _quat[0] * _quat[2], sqx - sqy - sqz + sqw);
                angles.x = Math.Asin(2 * test / unit);
                angles.z = Math.Atan2(2 * _quat[0] * _quat[3] - 2 * _quat[1] * _quat[2], -sqx + sqy - sqz + sqw);
            }

            // make negative rotations, positive rotations
            // angles = Utilities.MathHelper.WrapAngleRadians(angles);

            if (degrees)
            {
                angles.x *= 57.295779513d; // Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.y *= 57.295779513d; //Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.z *= 57.295779513d; // Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
            }
            return angles;
        }

        /// <summary>
        /// TODO: this is buggy and does not work correctly in all cases.  converting quat
        /// to euler should be avoided i think.
        /// Returns euler angle representation of the quat in radians
        /// http://forums.create.msdn.com/forums/p/4574/62520.aspx //<-- TODO: this site has alternatives starting at ed022's 17th post
        /// </summary>
        /// <returns></returns>
        public Vector3d GetEulerAnglesOLD(bool degrees)
        {
            Vector3d angles;
            const double case1 = Math.PI / 2.0d;
            const double case2 = -Math.PI / 2.0d;
            // quat must be normalized
            angles.z = Math.Atan2(2.0d * (_quat[0] * _quat[1] +
                _quat[3] * _quat[2]),
                (_quat[3] * _quat[3] +
                _quat[0] * _quat[0] -
                _quat[1] * _quat[1] -
                _quat[2] * _quat[2]));
            double sine = -2.0d * (_quat[0] * _quat[2] - _quat[3] * _quat[1]);

            if (sine >= 1d)     //cases where value is 1 or -1 cause NAN
                angles.y = case1;
            else if (sine <= -1d)
                angles.y = case2;
            else
                angles.y = Math.Asin(sine);

            angles.x = Math.Atan2(2.0d * (_quat[3] * _quat[0] + _quat[1] * _quat[2]), (_quat[3] * _quat[3] - _quat[0] * _quat[0] - _quat[1] * _quat[1] + _quat[2] * _quat[2]));

            if (degrees)
            {
                angles.x *= 57.2958d; //Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.y *= 57.2958;// Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.z *= 57.2958; //Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
            }
            return angles;
        }

        //public static Vector3d ToEulerAngles(Quaternion q)
        //{
        //    // Store the Euler angles in radians
        //    Vector3d pitchYawRoll = new Vector3d();

        //    double sqx = q.X * q.X;
        //    double sqy = q.Y * q.Y;
        //    double sqz = q.Z * q.Z;
        //    double sqw = q.W * q.W;

        //    // If quaternion is normalised the unit is one, otherwise it is the correction factor
        //    double unit = sqx + sqy + sqz + sqw;

        //    double test = q.X * q.Y + q.Z * q.W;
        //    //double test = q.X * q.Z - q.W * q.Y;

        //    if (test > 0.4999f * unit)                              // 0.4999f OR 0.5f - EPSILON
        //    {
        //        // Singularity at north pole
        //        pitchYawRoll.y = 2f * (float)Math.Atan2(q.X, q.W);  // Yaw
        //        pitchYawRoll.x = PIOVER2;                           // Pitch
        //        pitchYawRoll.z = 0f;                                // Roll
        //        return pitchYawRoll;
        //    }
        //    else if (test < -0.4999f * unit)                        // -0.4999f OR -0.5f + EPSILON
        //    {
        //        // Singularity at south pole
        //        pitchYawRoll.y = -2f * (float)Math.Atan2(q.X, q.W); // Yaw
        //        pitchYawRoll.x = -PIOVER2;                          // Pitch
        //        pitchYawRoll.z = 0f;                                // Roll
        //        return pitchYawRoll;
        //    }
        //    else
        //    {
        //        pitchYawRoll.y = (float)Math.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);       // Yaw
        //        pitchYawRoll.x = (float)Math.Asin(2f * test / unit);                                              // Pitch
        //        pitchYawRoll.z = (float)Math.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);      // Roll

        //        //pitchYawRoll.Y = (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz + sqw));      // Yaw 
        //        //pitchYawRoll.X = (float)Math.Asin(2f * (q.X * q.Z - q.W * q.Y));                                // Pitch 
        //        //pitchYawRoll.Z = (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz));      // Roll 
        //    }

        //    return pitchYawRoll;
        //}

        public byte GetComponentYRotationIndex()
        {
            Vector3d anglesRadians = GetEulerAngles(false);
            int snapLimit = 90; // for v1.0 we dont allow 45 degree increments.  Only 90

            double angleDegrees = anglesRadians.y * 57.2958d; //Utilities.MathHelper.RADIANS_TO_DEGREES;

            if (angleDegrees < 0.0)
                angleDegrees += 360;
            else if (angleDegrees > 360)
                angleDegrees = angleDegrees % 360;

            // snap to 90 degree increments
            int snapped = ((int)Math.Round(angleDegrees / snapLimit)) * snapLimit;

            byte result = (byte)(64 * snapped / snapLimit);
            return result;
            // 0 = 0 or 360 degrees
            // 32 = 45 degrees // not used
            // 64 = 90 degrees
            // 96 = 135 degrees // not used
            // 128 = 180 degrees
            // 160 = 225 degrees // not used
            // 192 = 270 degrees
            // 224 = 315 degrees // not used
        }

        // TODO: Slerp2 is being used in some places and Slerp in others.  I think all
        //       should use Slerp2		
        // Slerp - spherical interpolation of quaternions
        public static Quaternion Slerp(Quaternion start, Quaternion end, double t)
        {
            // NOTE: start and end must be normalized rotations
            double costheta = Quaternion.DotProduct(start, end);
            const double epsilon = 0.001d;
            double sclp, sclq;
            //// decide if one of the quaternions is backwards
            //double a = (x - quat2.x) * (x - quat2.x) + (y - quat2.y) * (y - quat2.y) + (z - quat2.z) * (z - quat2.z) + (r - quat2.r) * (r - quat2.r);
            //double b = (x + quat2.x) * (x + quat2.x) + (y + quat2.y) * (y + quat2.y) + (z + quat2.z) * (z + quat2.z) + (r + quat2.r) * (r + quat2.r);
            //if (a > b)
            //{
            //    //quato.Negate();
            //   costheta = -costheta;
            //   end.x *= -1;   // Reverse all signs
            //   end.y *= -1;
            //   end.z  *= -1;
            //   end.w  *= -1;
            //}

            // http://www.cs.wisc.edu/graphics/Courses/cs-838-1999/Students/thorek/final/Quatern.cpp
            // Make sure the two quaternions are not exactly opposite? (within a little slop).

            if (1.0d - costheta > epsilon)
            {
                // Standard case (slerp)
                double omega = Math.Acos(costheta);
                double sinom = Math.Sin(omega);
                sclp = Math.Sin((1.0d - t) * omega) / sinom;
                sclq = Math.Sin(t * omega) / sinom;
            }
            else
            {
                // very close. linear interpolation will be faster
                sclp = 1.0d - t;
                sclq = t;
            }

            return new Quaternion(sclp * start.X + sclq * end.X,
                                    sclp * start.Y + sclq * end.Y,
                                    sclp * start.Z + sclq * end.Z,
                                    sclp * start.W + sclq * end.W);

            // TODO: i never properly finished this function or tested it
            // Still here? Then the quaternions are nearly opposite so to avoid a divided by zero error
            // Calculate a perpendicular quaternion and slerp that direction
            sclp = Math.Sin((1.0d - t) * Math.PI);
            sclq = Math.Sin(t * Math.PI);
            return new Quaternion(
                sclp * start.W + sclq * end.Z,
                sclp * start.X + sclq * -end.Y,
                sclp * start.Y + sclq * end.X,
                sclp * start.Z + sclq * -end.W);

        }

        // xna slerp
        public static Quaternion Slerp2(Quaternion start, Quaternion end, double t)
        {
            double opposite;
            double inverse;
            double dot = DotProduct(start, end);
            const double epsilon = .001d;
            if (Math.Abs(dot) > 1.0d - epsilon)
            {
                inverse = 1.0d - t;
                opposite = t * Math.Sign(dot);
            }
            else
            {
                double acos = Math.Acos(Math.Abs(dot));
                double invSin = (1.0d / Math.Sin(acos));
                inverse = Math.Sin((1.0d - t) * acos) * invSin;
                opposite = Math.Sin(t * acos) * invSin * Math.Sign(dot);
            }

            return new Quaternion((inverse * start.X) + (opposite * end.X),
                    (inverse * start.Y) + (opposite * end.Y),
                    (inverse * start.Z) + (opposite * end.Z),
                    (inverse * start.W) + (opposite * end.W));
        }

        // xna lerp
        public static Quaternion Lerp(Quaternion start, Quaternion end, double amount)
        {
            double num = 1f - amount;
            Quaternion result = new Quaternion();
            double num2 = start.X * end.X + start.Y * end.Y + start.Z * end.Z + start.W * end.W;
            if (num2 >= 0f)
            {
                result.X = num * start.X + amount * end.X;
                result.Y = num * start.Y + amount * end.Y;
                result.Z = num * start.Z + amount * end.Z;
                result.W = num * start.W + amount * end.W;
            }
            else
            {
                result.X = num * start.X - amount * end.X;
                result.Y = num * start.Y - amount * end.Y;
                result.Z = num * start.Z - amount * end.Z;
                result.W = num * start.W - amount * end.W;
            }

            double num3 = result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W;
            double num4 = 1d / Math.Sqrt(num3);
            result.X *= num4;
            result.Y *= num4;
            result.Z *= num4;
            result.W *= num4;
            return result;
        }

        // -------------------------------------------------------------
        // SLERP: Spherical Linear Interpolation
        // Step from q1 to q2, 0=<t=<1
        // SLERP(q1,q2,0) = q1
        // SLERP(q1,q2,1) = q2
        // -------------------------------------------------------------
        //Quat SLERP(Quat q1, Quat q2, float t)
        //{
        //    Quat result = new Quat();
        //    float[] to1 = new float[4];
        //    float omega, cos_omega, sin_omega, scale0, scale1;

        //    // calc cosine
        //    cos_omega = q1.r * q2.r + q1.x * q2.x + q1.y * q2.y + q1.z * q2.z;

        //    // adjust signs (if necessary)
        //    if (cos_omega < 0.0)
        //    {
        //        cos_omega = -cos_omega;
        //        to1[0] = -q2.r;
        //        to1[1] = -q2.x;
        //        to1[2] = -q2.y;
        //        to1[3] = -q2.z;

        //    }
        //    else
        //    {
        //        to1[0] = q2.r;
        //        to1[1] = q2.x;
        //        to1[2] = q2.y;
        //        to1[3] = q2.z;
        //    }


        //    // calculate coefficients

        //    if ((1.0 - cos_omega) > 0.01)
        //    {
        //        // standard case (slerp)
        //        omega = (float)Math.acos(cos_omega);
        //        sin_omega = sin(omega);
        //        scale0 = sin((1.0 - t) * omega) / sin_omega;
        //        scale1 = sin(t * omega) / sin_omega;


        //    }
        //    else
        //    {
        //        // "from" and "to" Quats are very close 
        //        //  ... so we can do a linear interpolation
        //        scale0 = 1.0 - t;
        //        scale1 = t;
        //    }
        //    // calculate final values
        //    result.r = scale0 * q1.r + scale1 * to1[0];
        //    result.x = scale0 * q1.x + scale1 * to1[1];
        //    result.y = scale0 * q1.y + scale1 * to1[2];
        //    result.z = scale0 * q1.z + scale1 * to1[3];

        //    return result;
        //}


        // http://physicsforgames.blogspot.com/2010/02/quaternions.html
        //        How to Integrate a Quaternion:
        //
        //Updating the dynamical state of a rigid body is referred to as integration. If you represent the orientation of this body with a quaternion, you will need to know how to update it. This is done with the following quaternion formula.
        //
        //q' = Δq q
        //
        //We calculate Δq using a 3D vector ω whose magnitude represents the angular velocity, and whose direction represents the axis of the angular velocity. We also use the time step Δt over which the velocity should be applied. Δq is still a rotation quaternion, and has the same form involving sines and cosines of a half angle. We use the angular velocity and time step to construct a vector θ, whose magnitude is the half angle, and whose direction is the axis.
        //
        //θ = ωΔt/2
        //
        //Note: I've included the factor of 1/2, which shows up inside the trig functions of the rotation quaternion. Expressing the rotation quaternion in terms of this vector you have
        //
        //Δq = ( cos(θ), (θ/|θ|) sin(θ) )
        //
        //This works well, however this formula becomes numerically unstable as |θ| approaches zero. If we can detect that |θ| is small, we can safely use the Taylor series expansion of the sin and cos functions. The "low angle" version of this formula is
        //
        //Δq = (1 - |θ|2/2, θ - θ|θ|2/6)
        //
        //We use the first 3 terms of the Taylor series expansion, so we should ensure that the fourth term is less than machine precision before we use the "low angle" version. The fourth term of the expansion is
        //
        //|θ|4/24 < ε
        //
        //Here is a sample function for integrating a quaternion with a given angular velocity and time step
        //
        //Quat QuatIntegrate(const Quat& q, const Vector& omega, float deltaT) { Quat deltaQ; Vector theta = VecScale(omega, deltaT * 0.5f); float thetaMagSq = VecMagnitudeSq(theta); float s; if(thetaMagSq * thetaMagSq / 24.0f < MACHINE_SMALL_FLOAT) { deltaQ.w = 1.0f - thetaMagSq / 2.0f; s = 1.0f - thetaMagSq / 6.0f; } else { float thetaMag = sqrt(thetaMagSq); deltaQ.w = cos(thetaMag); s = sin(thetaMag) / thetaMag; } deltaQ.x = theta.x * s; deltaQ.y = theta.y * s; deltaQ.z = theta.z * s; return QuatMultiply(deltaQ, q); }
        //


        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);
        }

        //public static Vector3d operator *(Quaternion q, Vector3d v)
        //{
        //    // nVidia SDK implementation
        //    Vector3d uv, uuv;

        //    Vector3d qvec;
        //    qvec.x = q.X;
        //    qvec.y = q.Y;
        //    qvec.z = q.Z;                 
        //    uv = Vector3d.CrossProduct(qvec,v);                
        //    uuv = Vector3d.CrossProduct(qvec, uv);                 
        //    uv *= (2.0 * q.W);                 
        //    uuv *= 2.0;                  
        //    return v + uv + uuv; 
        //}

        // http://www.java-gaming.org/index.php?PHPSESSID=dkpq2dfr89eks0atgndch2cjm3&topic=25517.msg220313#msg220313
        public static Vector3d operator *(Quaternion q, Vector3d v)
        {
            double k0 = q.W * q.W - 0.5;
            double k1;
            double rx, ry, rz;

            // k1 = Q.V  
            k1 = v.x * q.X;
            k1 += v.y * q.Y;
            k1 += v.z * q.Z;

            // (qq-1/2)V+(Q.V)Q  
            rx = v.x * k0 + q.X * k1;
            ry = v.y * k0 + q.Y * k1;
            rz = v.z * k0 + q.Z * k1;

            // (Q.V)Q+(qq-1/2)V+q(QxV)  
            rx += q.W * (q.Y * v.z - q.Z * v.y);
            ry += q.W * (q.Z * v.x - q.X * v.z);
            rz += q.W * (q.X * v.y - q.Y * v.x);

            //  2((Q.V)Q+(qq-1/2)V+q(QxV))  
            rx += rx;
            ry += ry;
            rz += rz;

            return new Vector3d(rx, ry, rz);
        }

        public static Quaternion operator *(Quaternion q1, double scale)
        {
            return Scale(q1, scale);
        }

        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return Multiply(q1, q2);
        }

        public static Matrix ToMatrix(Quaternion quat)
        {
            return new Matrix(quat);
        }

        //public static Quaternion operator /(Quaternion q, double scale)
        //{

        //}

        public override bool Equals(object obj)
        {
            if (obj is Quaternion == false)
                return false;

            Quaternion arg = (Quaternion)obj;
            return _quat[0] == arg._quat[0] &&
                _quat[1] == arg._quat[1] &&
                _quat[2] == arg._quat[2] &&
                _quat[3] == arg._quat[3];
        }

        public override int GetHashCode()
        {
            // throw new NotImplementedException();
            return 0;
        }

        /*public override string ToString()
        {
            string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
            return string.Format("{0}{1}{2}{3}{4}{5}{6}", _quat[0], delimiter,
                                                    _quat[1], delimiter,
                                                    _quat[2], delimiter,
                                                    _quat[3]);
        }

        public static string ToString(Quaternion[] quatArray)
        {

            if (quatArray == null || quatArray.Length == 0) return null;

            char[] delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string result = string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(result);

            for (int i = 0; i < quatArray.Length; i++)
            {
                sb.Append(quatArray[i].ToString());
                if (i != quatArray.Length - 1)
                    // append delimiter. NOTE: same delimiter is used even between quaternions and not just their elements
                    sb.Append(keymath.ParseHelper.English.XMLAttributeDelimiter); // note: we use a single delimiter here not the char[] since that holds multiple delims
            }
            result = sb.ToString();

            return result;
        }
        */
    }

    // http://msdn.microsoft.com/en-au/library/bb206269%28VS.85%29.aspx
#if USE_STRUCT
    public struct Matrix
#else
    public class Matrix
#endif
    {
        private double[,] _mat;
        private double _determinant;

        //* The zero-based row-column position:
        //      o _m00, _m01, _m02, _m03
        //      o _m10, _m11, _m12, _m13
        //      o _m20, _m21, _m22, _m23
        //      o _m30, _m31, _m32, _m33
        //* The one-based row-column position:
        //      o _11, _12, _13, _14
        //      o _21, _22, _23, _24
        //      o _31, _32, _33, _34
        //      o _41, _42, _43, _44

        //A matrix can also be accessed using array access notation, which is a zero-based set of indices. 
        //Each index is inside of square brackets. A 4x4 matrix is accessed with the following indices:
        //* [0][0], [0][1], [0][2], [0][3]
        //* [1][0], [1][1], [1][2], [1][3]
        //* [2][0], [2][1], [2][2], [2][3]
        //* [3][0], [3][1], [3][2], [3][3]
        public Matrix(bool identity)
        {
            _mat = new double[4, 4];
            _determinant = 0d;
            if (identity)
            {
                _mat[0, 0] = 1.0d;
                _mat[1, 1] = 1.0d;
                _mat[2, 2] = 1.0d;
                _mat[3, 3] = 1.0d;
            }
        }

        /// <summary>
        /// Matrix from orientation quaternion.
        /// </summary>
        /// <param name="quat">Unit quaternion</param>
        public Matrix(Quaternion quat) : this()
        {
            //Matrix matrix = Matrix.Identity(); // new Matrix(); //

            double xx = quat.X * quat.X;
            double yy = quat.Y * quat.Y;
            double zz = quat.Z * quat.Z;
            double xy = quat.X * quat.Y;
            double xz = quat.X * quat.Z;
            double yz = quat.Y * quat.Z;
            double wx = quat.W * quat.X;
            double wy = quat.W * quat.Y;
            double wz = quat.W * quat.Z;

            _mat[0, 0] = 1.0 - 2.0 * (yy + zz);
            _mat[1, 0] = 2.0 * (xy - wz);
            _mat[2, 0] = 2.0 * (xz + wy);

            _mat[0, 1] = 2.0 * (xy + wz);
            _mat[1, 1] = 1.0 - 2.0 * (xx + zz);
            _mat[2, 1] = 2.0 * (yz - wx);

            _mat[0, 2] = 2.0 * (xz - wy);
            _mat[1, 2] = 2.0 * (yz + wx);
            _mat[2, 2] = 1.0 - 2.0 * (xx + yy);

            _mat[3, 0] = _mat[3, 1] = _mat[3, 2] = 0.0d;
            _mat[0, 3] = _mat[1, 3] = _mat[2, 3] = 0.0d;
            _mat[3, 3] = 1.0d;



            //
            //            double single9 = quat.X * quat.X;
            //            double single8 = quat.Y * quat.Y;
            //            double single7 = quat.Z * quat.Z;
            //            double single6 = quat.X * quat.Y;
            //            double single5 = quat.Z * quat.W;
            //            double single4 = quat.Z * quat.X;
            //            double single3 = quat.Y * quat.W;
            //            double single2 = quat.Y * quat.Z;
            //            double single1 = quat.X * quat.W;
            //            _mat[0, 0] = 1.0 - (2.0 * (single8 + single7));
            //            _mat[0, 1] = 2.0 * (single6 + single5);
            //            _mat[0, 2] = 2.0 * (single4 - single3);
            //            _mat[0, 3] = 0.0;
            //            _mat[1, 0] = 2.0 * (single6 - single5);
            //            _mat[1, 1] = 1.0 - (2.0 * (single7 + single9));
            //            _mat[1, 2] = 2.0 * (single2 + single1);
            //            _mat[1, 3] = 0.0;
            //            _mat[2, 0] = 2.0 * (single4 + single3);
            //            _mat[2, 1] = 2.0 * (single2 - single1);
            //            _mat[2, 2] = 1.0 - (2.0 * (single8 + single9));
            //            _mat[2, 3] = 0.0;
            //            _mat[3, 0] = 0.0;
            //            _mat[3, 1] = 0.0;
            //            _mat[3, 2]= 0.0;
            //            _mat[3, 3] = 1.0;
        }

        public Matrix(double m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24,
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44) : this()
        {
            _mat[0, 0] = m11;
            _mat[0, 1] = m12;
            _mat[0, 2] = m13;
            _mat[0, 3] = m14;
            _mat[1, 0] = m21;
            _mat[1, 1] = m22;
            _mat[1, 2] = m23;
            _mat[1, 3] = m24;
            _mat[2, 0] = m31;
            _mat[2, 1] = m32;
            _mat[2, 2] = m33;
            _mat[2, 3] = m34;
            _mat[3, 0] = m41;
            _mat[3, 1] = m42;
            _mat[3, 2] = m43;
            _mat[3, 3] = m44;
        }
        public Matrix(Matrix m) : this()
        {
            _mat[0, 0] = m.M11;
            _mat[0, 1] = m.M12;
            _mat[0, 2] = m.M13;
            _mat[0, 3] = m.M14;
            _mat[1, 0] = m.M21;
            _mat[1, 1] = m.M22;
            _mat[1, 2] = m.M23;
            _mat[1, 3] = m.M24;
            _mat[2, 0] = m.M31;
            _mat[2, 1] = m.M32;
            _mat[2, 2] = m.M33;
            _mat[2, 3] = m.M34;
            _mat[3, 0] = m.M41;
            _mat[3, 1] = m.M42;
            _mat[3, 2] = m.M43;
            _mat[3, 3] = m.M44;
        }

        public double Determinant { get { return _determinant; } }
        public double M11
        {
            get { return _mat[0, 0]; }
            set { _mat[0, 0] = value; }
        }

        public double M12
        {
            get { return _mat[0, 1]; }
            set { _mat[0, 1] = value; }
        }

        public double M13
        {
            get { return _mat[0, 2]; }
            set { _mat[0, 2] = value; }
        }

        public double M14
        {
            get { return _mat[0, 3]; }
            set { _mat[0, 3] = value; }
        }

        public double M21
        {
            get { return _mat[1, 0]; }
            set { _mat[1, 0] = value; }
        }

        public double M22
        {
            get { return _mat[1, 1]; }
            set { _mat[1, 1] = value; }
        }

        public double M23
        {
            get { return _mat[1, 2]; }
            set { _mat[1, 2] = value; }
        }

        public double M24
        {
            get { return _mat[1, 3]; }
            set { _mat[1, 3] = value; }
        }

        public double M31
        {
            get { return _mat[2, 0]; }
            set { _mat[2, 0] = value; }
        }

        public double M32
        {
            get { return _mat[2, 1]; }
            set { _mat[2, 1] = value; }
        }

        public double M33
        {
            get { return _mat[2, 2]; }
            set { _mat[2, 2] = value; }
        }

        public double M34
        {
            get { return _mat[2, 3]; }
            set { _mat[2, 3] = value; }
        }

        public double M41
        {
            get { return _mat[3, 0]; }
            set { _mat[3, 0] = value; }
        }

        public double M42
        {
            get { return _mat[3, 1]; }
            set { _mat[3, 1] = value; }
        }

        public double M43
        {
            get { return _mat[3, 2]; }
            set { _mat[3, 2] = value; }
        }

        public double M44
        {
            get { return _mat[3, 3]; }
            set { _mat[3, 3] = value; }
        }

        //http://www.euclideanspace.com/maths/algebra/matrix/orthogonal/index.htm
        public Vector3d Right
        {
            get
            {
                Vector3d result;
                result.x = _mat[0, 0];
                result.y = _mat[1, 0];
                result.z = _mat[2, 0];
                return result;
            }
        }
        public Vector3d Up
        {
            get
            {
                Vector3d result;
                result.x = _mat[0, 1];
                result.y = _mat[1, 1];
                result.z = _mat[2, 1];
                return result;
            }
        }
        public Vector3d Backward
        {
            get
            {
                Vector3d result;
                result.x = _mat[0, 2];
                result.y = _mat[1, 2];
                result.z = _mat[2, 2];
                return result;
            }
        }

        public Vector3d GetTranslation()
        {
            Vector3d result;
            result.x = _mat[3, 0];
            result.y = _mat[3, 1];
            result.z = _mat[3, 2];
            return result;
        }

        public Vector3d GetScale()
        {
            Vector3d result;
            result.x = _mat[0, 0];
            result.y = _mat[1, 1];
            result.z = _mat[2, 2];
            return result;
        }

        public void SetTranslation(Vector3d translation)
        {
            _mat[3, 0] = translation.x;
            _mat[3, 1] = translation.y;
            _mat[3, 2] = translation.z;
        }

        public static Matrix Identity()
        {
            Matrix m = new Matrix(true);
            m._mat[0, 0] = 1.0f;
            m._mat[0, 1] = 0.0f;
            m._mat[0, 2] = 0.0f;
            m._mat[0, 3] = 0.0f;

            m._mat[1, 0] = 0.0f;
            m._mat[1, 1] = 1.0f;
            m._mat[1, 2] = 0.0f;
            m._mat[1, 3] = 0.0f;

            m._mat[2, 0] = 0.0f;
            m._mat[2, 1] = 0.0f;
            m._mat[2, 2] = 1.0f;
            m._mat[2, 3] = 0.0f;

            m._mat[3, 0] = 0.0f;
            m._mat[3, 1] = 0.0f;
            m._mat[3, 2] = 0.0f;
            m._mat[3, 3] = 1.0f;

            return m;
        }

        /// <summary>
        /// Creates a Translation Matrix that is first initialized to Identity.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix CreateTranslation(Vector3d v)
        {
            Matrix result = Identity();
            result.SetTranslation(v);
            return result;
        }

        public static Matrix CreateScaling(double x, double y, double z)
        {
            Matrix result = Identity();
            result._mat[0, 0] = x;
            result._mat[1, 1] = y;
            result._mat[2, 2] = z;
            return result;
        }

        public static Matrix CreateScaling(Vector3d v)
        {
            return CreateScaling(v.x, v.y, v.z);
        }


        // NOTE: The following offset rotation is not really useful because instead we are computing
        // the RegoinMatrix already taking into account a .Pivot value.
        // http://www.ogre3d.org/forums/viewtopic.php?f=5&t=11088&start=25
        // http://stackoverflow.com/questions/8747870/xna-rotation-over-given-vector <-- answer is translation offset + axis rotation 
        // http://stackoverflow.com/questions/8791845/xna-rotate-a-bone-with-an-offset-translation
        public static Matrix Rotation(Vector3d axis, double angleRadians, Vector3d offset)
        {
            return Matrix.CreateTranslation(offset) * Matrix.CreateRotation(axis, angleRadians);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotationAxis">Unit vector</param>
        /// <param name="angleRadians"></param>
        /// <returns></returns>
        public static Matrix CreateRotation(Vector3d rotationAxis, double angleRadians)
        {
            //http://tools.devshed.com/c/a/Web-Development/Part-Three-Rotation-About-an-Arbitrary-Axis/
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            double invcos = 1.0d - cos;

            Matrix m = new Matrix();

            m.M11 = cos + rotationAxis.x * rotationAxis.x * invcos;
            m.M12 = rotationAxis.x * rotationAxis.y * invcos + (rotationAxis.z * sin);
            m.M13 = rotationAxis.x * rotationAxis.z * invcos - (rotationAxis.y * sin);
            m.M14 = 0;

            m.M21 = rotationAxis.x * rotationAxis.y * invcos - (rotationAxis.z * sin);
            m.M22 = cos + rotationAxis.y * rotationAxis.y * invcos;
            m.M23 = rotationAxis.y * rotationAxis.z * invcos + (rotationAxis.x * sin);
            m.M24 = 0;

            m.M31 = rotationAxis.x * rotationAxis.z * invcos + (rotationAxis.y * sin);
            m.M32 = rotationAxis.y * rotationAxis.z * invcos - (rotationAxis.x * sin);
            m.M33 = cos + rotationAxis.z * rotationAxis.z * invcos;
            m.M34 = 0;

            m.M41 = 0;
            m.M42 = 0;
            m.M43 = 0;
            m.M44 = 1;

            // below is extracted from xna's RotationFromAxis and it seems to produce same results
            // strange because using a 0,0,1 axis with 0 angleRadians is resulting in no rotation (identity) at all!
            // wtf?  

            //double single5 = rotationAxis.x;
            //double single4 = rotationAxis.y;
            //double single3 = rotationAxis.z;
            //double single2 = Math.Sin(angleRadians);
            //double single1 = Math.Cos(angleRadians);
            //double single11 = single5 * single5;
            //double single10 = single4 * single4;
            //double single9 = single3 * single3;
            //double single8 = single5 * single4;
            //double single7 = single5 * single3;
            //double single6 = single4 * single3;
            //m.M11 = single11 + (single1 * (1.00 - single11));
            //m.M12 = (single8 - (single1 * single8)) + (single2 * single3);
            //m.M13 = (single7 - (single1 * single7)) - (single2 * single4);
            //m.M14 = 0.00;
            //m.M21 = (single8 - (single1 * single8)) - (single2 * single3);
            //m.M22 = single10 + (single1 * (1.00 - single10));
            //m.M23 = (single6 - (single1 * single6)) + (single2 * single5);
            //m.M24 = 0.00;
            //m.M31 = (single7 - (single1 * single7)) + (single2 * single4);
            //m.M32 = (single6 - (single1 * single6)) - (single2 * single5);
            //m.M33 = single9 + (single1 * (1.00 - single9));
            //m.M34 = 0.00;
            //m.M41 = 0.00;
            //m.M42 = 0.00;
            //m.M43 = 0.00;
            //m.M44 = 1.00;
            return m;
        }

        // left handed x rotation matrix
        public static Matrix CreateRotationX(double angleRadians)
        {
            // Assuming the angle is in radians. 
            double cos_x = Math.Cos(angleRadians);
            double sin_x = Math.Sin(angleRadians);
            Matrix tmp = Matrix.Identity();
            tmp.M11 = 1.0D;
            tmp.M12 = 0.0D;
            tmp.M13 = 0.0D;
            tmp.M14 = 0;
            tmp.M21 = 0.0D;
            tmp.M22 = cos_x;
            tmp.M23 = sin_x;
            tmp.M24 = 0;
            tmp.M31 = 0.0D;
            tmp.M32 = -sin_x;
            tmp.M33 = cos_x;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1D;
            return tmp;
        }

        public static Matrix CreateRotationY(double angleRadians)
        {
            // Assuming the angle is in radians.
            double c = Math.Cos(angleRadians);
            double s = Math.Sin(angleRadians);
            Matrix tmp = Matrix.Identity();
            tmp.M11 = c;
            tmp.M12 = 0.0D;
            tmp.M13 = -s;
            tmp.M14 = 0;
            tmp.M21 = 0.0D;
            tmp.M22 = 1;
            tmp.M23 = 0.0D;
            tmp.M24 = 0;
            tmp.M31 = s;
            tmp.M32 = 0.0D;
            tmp.M33 = c;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1D;
            return tmp;
        }

        public static Matrix CreateRotationZ(double angleRadians)
        {
            // Assuming the angle is in radians. 
            double c = Math.Cos(angleRadians);
            double s = Math.Sin(angleRadians);
            Matrix tmp = Matrix.Identity();
            tmp.M11 = c;
            tmp.M12 = s;
            tmp.M13 = 0.0;
            tmp.M14 = 0;
            tmp.M21 = -s;
            tmp.M22 = c;
            tmp.M23 = 0.0;
            tmp.M24 = 0;
            tmp.M31 = 0.0;
            tmp.M32 = 0.0;
            tmp.M33 = 1.0;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1;
            return tmp;
        }

        // http://stackoverflow.com/questions/349050/calculating-a-lookat-matrix
        // this is a left handed view matrix.  to use as a rotation for a model, take it's inverse.
        public static Matrix CreateLookAt(Vector3d position, Vector3d target, Vector3d up)
        {
            Matrix matrix1 = new Matrix();
            // TODO: negation hack! for some reason we have to negate Vector3d.Normalize(position - target) or the rotation is off by 180 degrees
            // TODO: hopefully this is not something quirky with TV View matrix which is all we use this for so far.
            //       But eventually when we try to get one ship to rotate to another, we'll see if that is reversed and then we'll know
            Vector3d forward = -Vector3d.Normalize(position - target); // ZAXIS
                                                                       // orthonormalize (aka up and forward are orthogonal and normalized)
            Vector3d newUp = Vector3d.Normalize(Vector3d.CrossProduct(up, forward)); // XAXIS
                                                                                     //Vector3d right = Vector3d.CrossProduct(forward, newUp); // YAXIS
            Vector3d right = Vector3d.Normalize(Vector3d.CrossProduct(forward, newUp)); // YAXIS // Normalize here not necessary?
            matrix1.M11 = newUp.x;   // XAXIS
            matrix1.M12 = right.x;   // YAXIS
            matrix1.M13 = forward.x; // ZAXIS
            matrix1.M14 = 0.00d;
            matrix1.M21 = newUp.y;   // XAXIS
            matrix1.M22 = right.y;   // YAXIS
            matrix1.M23 = forward.y; // ZAXIS
            matrix1.M24 = 0.00d;
            matrix1.M31 = newUp.z;   // XAXIS
            matrix1.M32 = right.z;   // YAXIS
            matrix1.M33 = forward.z; // ZAXIS
            matrix1.M34 = 0.00d;
            //matrix1.M41 = -Vector3d.DotProduct(newUp, position);
            //matrix1.M42 = -Vector3d.DotProduct(right, position);
            //matrix1.M43 = -Vector3d.DotProduct(forward, position);
            matrix1.M44 = 1.00d;
            return matrix1;
        }

        public static Matrix PerspectiveFOVLH(double near, double far, double fovRadians, int viewportWidth, int viewportHeight, ref double aspectRatio)
        {
            Matrix proj = new Matrix();

            double cot = 1d / Math.Tan(fovRadians * 0.5d);

            aspectRatio = (double)viewportWidth / (double)viewportHeight; // floating point divide

            proj.M11 = cot / aspectRatio;
            proj.M22 = cot;
            proj.M33 = far / (far - near);
            proj.M34 = 1d;
            // Hypno - May.2.2012 - I switched the bottom line around from the commented one to the current
            // I havent noticed a difference yet but i've not tested much.  I need to verify which is correct.
            proj.M43 = -near * far / (far - near);
            // proj.M43 = -(far * near / (far - near));
            return proj;


            //double num = 1d / Math.Tan(fovRadians * 0.5d);
            //double m = num / aspectRatio;
            //Matrix result = new Matrix ();
            //result.M11 = m;
            //result.M12 = result.M13 = result.M14 = 0d;
            //result.M22 = num;
            //result.M21 = result.M23 = result.M24 = 0d;
            //result.M31 = result.M32 = 0d;
            //result.M33 = far / (near - far);
            //result.M34 = -1d;         
            //result.M41 = result.M42 = result.M44 = 0d;
            //result.M43 = near * far / (near - far);


            //result.M33 = far / (far - near);
            //result.M34 = 1d;
            //result.M43 = -near * far / (far - near);
            //return result;
        }

        /// <summary>
        /// http://www.codeguru.com/Cpp/misc/misc/math/article.php/c10123__2/    <-- deriving projection matrices
        /// src : http://www.ogre3d.org/forums/viewtopic.php?f=2&t=26244&start=0
        /// Then you can use it like this. The ctrl.Width & scale are the width and height of your window/viewport in pixels. This is needed to keep a correct aspect ratio.
        /// float scale = 0.5f; // Your scale here.
        ///Matrix4 p = this.BuildScaledOrthoMatrix(ctrl.Width  / scale / -2.0f,
        ///                                        ctrl.Width  / scale /  2.0f,
        ///                                        ctrl.Height / scale / -2.0f,
        ///                                        ctrl.Height / scale /  2.0f, 0, 1000);
        ///m_camera.SetCustomProjectionMatrix(true, p);
        ///You can also pan simply by moving the position of the plane :
        ///Matrix p = this.ScaledOrthoMatrix(ctrl.Width  / scale / -2.0f + tx,
        ///                                ctrl.Width  / scale /  2.0f + tx,
        ///                                ctrl.Height / scale / -2.0f + ty,
        ///                                ctrl.Height / scale /  2.0f + ty, 0, 1000);
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="top"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public static Matrix ScaledOrthoMatrix(double left, double right, double top, double bottom, double near, double far)
        {
            // March.6.2011 - This projection matrix is verified correct for all ortho views
            double invw = 1 / (right - left);
            double invh = 1 / (bottom - top);
            double invd = 1 / (far - near);

            Matrix proj = new Matrix();  // Matrix.Zero
            proj._mat[0, 0] = 2 * invw;
            //proj._mat[0, 3] = -(right + left) * invw;  // for offcenter matrices
            proj._mat[1, 1] = 2 * invh;
            //proj._mat[1, 3] = -(top + bottom) * invh; // for offcenter matrices
            proj._mat[2, 2] = invd;
            proj._mat[2, 3] = -near * invd;
            proj._mat[3, 3] = 1;
            return proj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scale">1.0 equals a field of view of 45 (i think???)</param>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public static Matrix ScaledOrthoMatrix(double scale, double viewportWidth, double viewportHeight, double near, double far)
        {
            double right = viewportWidth * scale / 2;
            double left = -right;
            double bottom = viewportHeight * scale / 2;
            double top = -bottom;

            return ScaledOrthoMatrix(left, right, top, bottom, near, far);
        }

        public static Matrix CreateBillboardRotationMatrix(Vector3d cameraUp, Vector3d cameraLook)
        {
            Matrix rotationMatrix = new Matrix(); // Types.Matrix.Identity();

            Vector3d r = Vector3d.CrossProduct(cameraUp, cameraLook);
            r.Normalize();
            rotationMatrix.M11 = r.x;
            rotationMatrix.M12 = r.y;
            rotationMatrix.M13 = r.z;
            rotationMatrix.M21 = cameraUp.x;
            rotationMatrix.M22 = cameraUp.y;
            rotationMatrix.M23 = cameraUp.z;
            rotationMatrix.M31 = cameraLook.x;
            rotationMatrix.M32 = cameraLook.y;
            rotationMatrix.M33 = cameraLook.z;
            // we dont include position in this rotation matrix
            // caller should do rotationMatrix.SetTranslation(vec) if they wish
            // to make this a final world matrix and not just a rotation matrix
            rotationMatrix.M44 = 1.0;

            return rotationMatrix;
        }

        //<Hypnotron> <Toaster> cause if you look at them head on its just flat which is to be expected  <-- i think i missed tha tline the first time somehow
        //<Hypnotron> i see what you're saying now tho.. maybe they special case the rendering when its headed directly at the camera (+/- some variance)
        //<Hypnotron> and do as you say, add a cap billboard to the end /me shrugs
        //<Hypnotron> or maybe they do nothing
        //<Aeon> do what I do, put a split right in the middle, when the angle to the camera is so that it's staring down the middle, inflate the split a little so it shows up
        //<Aeon> so it's 4 triangles instead of two
        //<Aeon> instead of a plane like [/]  make it like [X] and get that center point, duplicate underneath, then you can move them up and down the planes Z plane to give depth when looking down the plane
        //<Hypnotron> interesting
        //<Hypnotron> like opening an umbrella...
        //<Hypnotron> somewhat
        //<Hypnotron> cracking it open anyway
        // note: using 8 triangles and not just sandwhiching 2 x 2 triangle quads looks better at all angles... in case wondering why not just use two quads instead of 4.


        /// <summary>
        /// Creates a world matrix in camera space NOT a local space matrix.  NOTE that the billboard position is in camera space position or world position if cameraPosition is in world space.
        /// In otherwords, there is no need to multiply this returned matrix with a derivedRotation.
        /// </summary>
        /// <param name="up"></param>
        /// <param name="billboardPosition"></param>
        /// <param name="cameraPosition"></param>
        /// <returns></returns>
        public static Matrix CreateAxialBillboardRotationMatrix(Vector3d up, Vector3d billboardPosition, Vector3d cameraPosition)
        {
            // https://www.flipcode.com/archives/Billboarding-Excerpt_From_iReal-Time_Renderingi_2E.shtml
            // https://forum.unity.com/threads/billboard-script-flat-spherical-arbitrary-axis-aligned.539481/

            // https://gamedev.stackexchange.com/questions/188636/cylindrical-billboarding-around-an-arbitrary-axis-in-geometry-shader
            Vector3d look = Vector3d.Normalize(billboardPosition - cameraPosition);

            Vector3d right = Vector3d.Normalize(Vector3d.CrossProduct(up, look));

            // March.11.2024 - up is actually our axis and should not be recomputed. This fixes the issue with billboard not appearing to point towards it target at certain angles
            //           up = Vector3d.Normalize(Vector3d.CrossProduct(look, right));

            Matrix rotationMatrix = new Matrix(); // Types.Matrix.Identity();
            rotationMatrix.M11 = right.x;
            rotationMatrix.M12 = right.y;
            rotationMatrix.M13 = right.z;
            rotationMatrix.M21 = up.x;
            rotationMatrix.M22 = up.y;
            rotationMatrix.M23 = up.z;
            rotationMatrix.M31 = look.x;
            // rotationMatrix.M32 = look.z; //? apparently this z and y following are not transposed by accident
            // rotationMatrix.M33 = look.y;
            rotationMatrix.M32 = look.y;  //? i dunno, y and z seems to work with also with no visual difference at runtime
            rotationMatrix.M33 = look.z;

            rotationMatrix.M44 = 1.0;
            return rotationMatrix;
        }



        public static Matrix CreateAxialBillboardRotationMatrix(Matrix rotationMatrix, Vector3d billboardPosition,
                                                        Vector3d cameraPosition)
        {
            return
                CreateAxialBillboardRotationMatrix(new Vector3d(rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23), billboardPosition, cameraPosition);
        }


        ////For a square matrix A, the inverse is written A-1. When A is multiplied by A-1 the result is the identity matrix I. 
        //// Non-square matrices do not have inverses.
        //// http://www.mathwords.com/i/inverse_of_a_matrix.htm
        //// Note: Not all square matrices have inverses. A square matrix which has an inverse is called invertible or nonsingular,
        //// and a square matrix without an inverse is called noninvertible or singular.
        //public static Matrix Inverse(Matrix m)
        //{
        //    // the following seems to work with physics collision whereas the simple InverseView() transpose does not.  That one
        //    // seems to work ok for perspective view picking

        //    // WARNING: The xna code works with picking best
        //    // for some reason the following loop does not!

        //  double e;
        //  Matrix m1 = new Matrix(m);

        //  for (int k = 0; k < 4; ++k)
        //  {
        //      e = m1._mat[k, k];
        //      m1._mat[k, k] = 1.0d;
        //      if (e == 0.0) { System.Diagnostics.Debug.WriteLine("Matrix.Inverse() - Inversion error.");  return m1; }// TODO: returning seems to work ok'ish.  throwing exception is lame throw new Exception("Matrix inversion error");
        //      for (int j = 0; j < 4; ++j)
        //           m1._mat[k, j] = m1._mat[k, j] / e;
        //      for (int i = 0; i < 4; ++i)
        //      {
        //          if (i != k)
        //          {
        //              e = m1._mat[i, k];
        //              m1._mat[i, k] = 0.0d;
        //              for (int j = 0; j < 4; ++j)
        //                  m1._mat[i, j] = m1._mat[i, j] - e * m1._mat[k, j];
        //          }
        //      }
        //  }
        //
        //  Matrix tmp = m * m1;
        //  //System.Diagnostics.Debug.Assert (tmp.Equals (Matrix.Identity())); 
        //  return m1;
        //
        //}

        //        private static Matrix InverseTV3D (Matrix m)
        //        {
        //
        //            // TVMatrixInverse() works for picking and culling and everything but
        //            // 1) we don't want the MTV3D65 dependancy in keystone.dll or keymath.dll since server shouldn't require windows and DX
        //            // 2) the loss of precision when using single floating point precision matrices are bad for space sim
        //            MTV3D65.TV_3DMATRIX tvmat = Keystone.Types. Matrix.ToTV3DMatrix(m);
        //            MTV3D65.TV_3DMATRIX inv = new MTV3D65.TV_3DMATRIX();
        //            float det = 0;
        //
        //            CoreClient._Core.Maths.TVMatrixInverse(ref inv, ref det, tvmat);
        //            return new Matrix(inv);
        //        }

        private static Matrix InvertSlimDX(Matrix value)
        {
            double b0 = (value.M31 * value.M42) - (value.M32 * value.M41);
            double b1 = (value.M31 * value.M43) - (value.M33 * value.M41);
            double b2 = (value.M34 * value.M41) - (value.M31 * value.M44);
            double b3 = (value.M32 * value.M43) - (value.M33 * value.M42);
            double b4 = (value.M34 * value.M42) - (value.M32 * value.M44);
            double b5 = (value.M33 * value.M44) - (value.M34 * value.M43);

            double d11 = value.M22 * b5 + value.M23 * b4 + value.M24 * b3;
            double d12 = value.M21 * b5 + value.M23 * b2 + value.M24 * b1;
            double d13 = value.M21 * -b4 + value.M22 * b2 + value.M24 * b0;
            double d14 = value.M21 * b3 + value.M22 * -b1 + value.M23 * b0;

            double det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13 - value.M14 * d14;
            if (Math.Abs(det) <= 00.0000001d) // the epsilon used here could fail if a very large model is scaled down sufficiently much. For now this value works.
            {

                return new Matrix(); ;
            }

            det = 1d / det;

            double a0 = (value.M11 * value.M22) - (value.M12 * value.M21);
            double a1 = (value.M11 * value.M23) - (value.M13 * value.M21);
            double a2 = (value.M14 * value.M21) - (value.M11 * value.M24);
            double a3 = (value.M12 * value.M23) - (value.M13 * value.M22);
            double a4 = (value.M14 * value.M22) - (value.M12 * value.M24);
            double a5 = (value.M13 * value.M24) - (value.M14 * value.M23);

            double d21 = value.M12 * b5 + value.M13 * b4 + value.M14 * b3;
            double d22 = value.M11 * b5 + value.M13 * b2 + value.M14 * b1;
            double d23 = value.M11 * -b4 + value.M12 * b2 + value.M14 * b0;
            double d24 = value.M11 * b3 + value.M12 * -b1 + value.M13 * b0;

            double d31 = value.M42 * a5 + value.M43 * a4 + value.M44 * a3;
            double d32 = value.M41 * a5 + value.M43 * a2 + value.M44 * a1;
            double d33 = value.M41 * -a4 + value.M42 * a2 + value.M44 * a0;
            double d34 = value.M41 * a3 + value.M42 * -a1 + value.M43 * a0;

            double d41 = value.M32 * a5 + value.M33 * a4 + value.M34 * a3;
            double d42 = value.M31 * a5 + value.M33 * a2 + value.M34 * a1;
            double d43 = value.M31 * -a4 + value.M32 * a2 + value.M34 * a0;
            double d44 = value.M31 * a3 + value.M32 * -a1 + value.M33 * a0;

            Matrix result = Matrix.Identity();
            result.M11 = +d11 * det; result.M12 = -d21 * det; result.M13 = +d31 * det; result.M14 = -d41 * det;
            result.M21 = -d12 * det; result.M22 = +d22 * det; result.M23 = -d32 * det; result.M24 = +d42 * det;
            result.M31 = +d13 * det; result.M32 = -d23 * det; result.M33 = +d33 * det; result.M34 = -d43 * det;
            result.M41 = -d14 * det; result.M42 = +d24 * det; result.M43 = -d34 * det; result.M44 = +d44 * det;

            return result;
        }

        // 4x4 Matrix Inverse
        public static Matrix Inverse(Matrix m)
        {
            // return InverseTV3D (m);
            return InvertSlimDX(m);

            //double e;
            //Matrix m1 = new Matrix(m);

            //for (int k = 0; k < 4; ++k)
            //{
            //    e = m1._mat[k, k];
            //    m1._mat[k, k] = 1.0d;
            //    if (e == 0.0) { System.Diagnostics.Debug.WriteLine("Matrix.Inverse() - Inversion error."); return m1; }// TODO: returning seems to work ok'ish.  throwing exception is lame throw new Exception("Matrix inversion error");
            //    for (int j = 0; j < 4; ++j)
            //        m1._mat[k, j] = m1._mat[k, j] / e;
            //    for (int i = 0; i < 4; ++i)
            //    {
            //        if (i != k)
            //        {
            //            e = m1._mat[i, k];
            //            m1._mat[i, k] = 0.0d;
            //            for (int j = 0; j < 4; ++j)
            //                m1._mat[i, j] = m1._mat[i, j] - e * m1._mat[k, j];
            //        }
            //    }
            //}

            //Matrix tmp = m * m1;
            ////System.Diagnostics.Debug.Assert (tmp.Equals (Matrix.Identity())); 
            //return m1;

            // WARNING: Below works with picking really well, above does not!
            Matrix result = new Matrix();
            double single5 = m.M11;
            double single4 = m.M12;
            double single3 = m.M13;
            double single2 = m.M14;
            double single9 = m.M21;
            double single8 = m.M22;
            double single7 = m.M23;
            double single6 = m.M24;
            double single17 = m.M31;
            double single16 = m.M32;
            double single15 = m.M33;
            double single14 = m.M34;
            double single13 = m.M41;
            double single12 = m.M42;
            double single11 = m.M43;
            double single10 = m.M44;
            double single23 = (single15 * single10) - (single14 * single11);
            double single22 = (single16 * single10) - (single14 * single12);
            double single21 = (single16 * single11) - (single15 * single12);
            double single20 = (single17 * single10) - (single14 * single13);
            double single19 = (single17 * single11) - (single15 * single13);
            double single18 = (single17 * single12) - (single16 * single13);
            double single39 = ((single8 * single23) - (single7 * single22)) + (single6 * single21);
            double single38 = -(((single9 * single23) - (single7 * single20)) + (single6 * single19));
            double single37 = ((single9 * single22) - (single8 * single20)) + (single6 * single18);
            double single36 = -(((single9 * single21) - (single8 * single19)) + (single7 * single18));
            double single1 = 1.00d / ((((single5 * single39) + (single4 * single38)) + (single3 * single37)) + (single2 * single36));
            result.M11 = single39 * single1;
            result.M21 = single38 * single1;
            result.M31 = single37 * single1;
            result.M41 = single36 * single1;
            result.M12 = -(((single4 * single23) - (single3 * single22)) + (single2 * single21)) * single1;
            result.M22 = (((single5 * single23) - (single3 * single20)) + (single2 * single19)) * single1;
            result.M32 = -(((single5 * single22) - (single4 * single20)) + (single2 * single18)) * single1;
            result.M42 = (((single5 * single21) - (single4 * single19)) + (single3 * single18)) * single1;
            double single35 = (single7 * single10) - (single6 * single11);
            double single34 = (single8 * single10) - (single6 * single12);
            double single33 = (single8 * single11) - (single7 * single12);
            double single32 = (single9 * single10) - (single6 * single13);
            double single31 = (single9 * single11) - (single7 * single13);
            double single30 = (single9 * single12) - (single8 * single13);
            result.M13 = (((single4 * single35) - (single3 * single34)) + (single2 * single33)) * single1;
            result.M23 = -(((single5 * single35) - (single3 * single32)) + (single2 * single31)) * single1;
            result.M33 = (((single5 * single34) - (single4 * single32)) + (single2 * single30)) * single1;
            result.M43 = -(((single5 * single33) - (single4 * single31)) + (single3 * single30)) * single1;
            double single29 = (single7 * single14) - (single6 * single15);
            double single28 = (single8 * single14) - (single6 * single16);
            double single27 = (single8 * single15) - (single7 * single16);
            double single26 = (single9 * single14) - (single6 * single17);
            double single25 = (single9 * single15) - (single7 * single17);
            double single24 = (single9 * single16) - (single8 * single17);
            result.M14 = -(((single4 * single29) - (single3 * single28)) + (single2 * single27)) * single1;
            result.M24 = (((single5 * single29) - (single3 * single26)) + (single2 * single25)) * single1;
            result.M34 = -(((single5 * single28) - (single4 * single26)) + (single2 * single24)) * single1;
            result.M44 = (((single5 * single27) - (single4 * single25)) + (single3 * single24)) * single1;
            return result;

        }


        // a simple inverse to work with View matrix and is used by Picking
        // http://www.gamedev.net/community/forums/topic.asp?topic_id=288155
        public static Matrix InverseView(Matrix m)
        {
            Matrix R = new Matrix(m);
            R.M41 = 0;
            R.M42 = 0;
            R.M43 = 0;
            R.M44 = 1;

            Matrix T = Matrix.Identity();
            T.M41 = m.M41;
            T.M42 = m.M42;
            T.M43 = m.M43;
            T.M44 = m.M44;

            //System.Diagnostics.Trace.Assert(m.Equals(Matrix.Multiply( R, T)));

            Matrix TInv = new Matrix(T);
            // negate the translation of T
            TInv.M41 = -T.M41;
            TInv.M42 = -T.M42;
            TInv.M43 = -T.M43;
            TInv.M44 = -T.M44; // TODO: have tried with this line commented out and not and no real difference.

            // inverse of rotation only is it's transpose
            Matrix RInv = Matrix.Transpose(R);

            return Matrix.Multiply(TInv, RInv);
        }

        // swap rows with columns
        public static Matrix Transpose(Matrix m)
        {
            Matrix transposed = new Matrix();
            transposed._mat[0, 0] = m._mat[0, 0];
            transposed._mat[0, 1] = m._mat[1, 0];
            transposed._mat[0, 2] = m._mat[2, 0];
            transposed._mat[0, 3] = m._mat[3, 0];

            transposed._mat[1, 0] = m._mat[0, 1];
            transposed._mat[1, 1] = m._mat[1, 1];
            transposed._mat[1, 2] = m._mat[2, 1];
            transposed._mat[1, 3] = m._mat[3, 1];

            transposed._mat[2, 0] = m._mat[0, 2];
            transposed._mat[2, 1] = m._mat[1, 2];
            transposed._mat[2, 2] = m._mat[2, 2];
            transposed._mat[2, 3] = m._mat[3, 2];

            transposed._mat[3, 0] = m._mat[0, 3];
            transposed._mat[3, 1] = m._mat[1, 3];
            transposed._mat[3, 2] = m._mat[2, 3];
            transposed._mat[3, 3] = m._mat[3, 3];
            return transposed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">the coordinate system that is we start from</param>
        /// <param name="dest">the destination coordinate system we want to transform the source to</param>
        /// <returns></returns>
        public static Matrix Source2Dest(Matrix source, Matrix dest)
        {
            Matrix root2dest = dest;
            Matrix source2root = Matrix.Inverse(source);  // NOTE: fails on 4x4 matrix such as view
            Matrix source2dest = root2dest * source2root;

#if DEBUG
        // TODO: verify that we can first subtract the relative difference in positions from both matrices to cancel those out before we 
        //       start matrix multiplication?  <-- Feb.4.2014 - i think the below actually proves this does work, however if there is no difference
        //       then there's no need to do it because it's a bit more expensive
        // get the difference in translation between src and dst matrices
        // verify the computed source2dest now has a translation that is equal to -difference
        // within some tolerance and that if we were to subtract out that difference first, then
        // our results would be more precise  
    
        Vector3d srcTranslation = source.GetTranslation();
        Vector3d dstTranslation= dest.GetTranslation ();
        Vector3d diff = dstTranslation - srcTranslation;
        Vector3d s2dTranslation = source2dest.GetTranslation();
//		System.Diagnostics.Debug.Assert (s2dTranslation.Equals (diff));
#endif
            return source2dest;
        }

        // TODO: looks like Sylvain has finally added TVMath.TVEulerAnglesFromMatrix(Rot, Matx)
        // if i have any problems with this, i can try switching to tv's version
        public static void Decompose(Matrix mx, out Vector3d outPosition, out Vector3d outRotation,
                                        out Vector3d outScale)
        {
            Vector3d positionResult;
            positionResult.x = mx.M41;
            positionResult.y = mx.M42;
            positionResult.z = mx.M43;

            Vector3d scaleResult;
            scaleResult.x = mx.M11;
            scaleResult.y = mx.M22;
            scaleResult.z = mx.M33;

            outPosition = positionResult;
            outScale = scaleResult;

            outRotation = DecomposeRollPitchYawZXYMatrix(mx);
        }

        //TODO: Read this article
        // http://www.robertblum.com/articles/2005/02/14/decomposing-matrices#comments
        // that suggest the following isnt really the best way to do this.  We should implement
        // the other as well.
        /// <summary>
        /// Decomposes a RotationMatrix to yaw, pitch and roll.
        /// Freeware Code taken from  Mike Pelton's blog.
        /// http://blogs.msdn.com/mikepelton/archive/2004/10/29/249501.aspx
        /// </summary>
        /// <param name="mx"></param>
        public static Vector3d DecomposeRollPitchYawZXYMatrix(Matrix mx)
        {
            double toDegrees = 57.295779513d; //Utilities.MathHelper.RADIANS_TO_DEGREES;
            double xPitch, yYaw, zRoll;
            xPitch = Math.Asin(-mx.M32) * toDegrees; // TODO: the toDegrees... is consistatnt with code above were we convert degrees to radians, but with JigLib, it seems we need to leave them in radians so it seems i have some issue where im mixng the two improperly... :/
            double threshold = 0.001; // Hardcoded constant - burn him, he's a witch
            double test = Math.Cos(xPitch);

            if (test > threshold)
            {
                zRoll = Math.Atan2(mx.M12, mx.M22) * toDegrees;
                yYaw = Math.Atan2(mx.M31, mx.M33) * toDegrees;
            }
            else
            {
                zRoll = Math.Atan2(-mx.M21, mx.M11) * toDegrees;
                //"This being maths there are gotcha's - when the cosine of the pitch angle gets small, 
                //(for a pitch of 90-ish degrees, say) numerically things go bananas, so you can take an arbitrary
                //decision about the yaw angle (here I've set it to zero) and deduce a roll. This is okay except
                //where numerical consistency is important (flying jet fighters. guiding satellites and so on) where 
                //you can't just swizzle your object in space to make the sums work. If you're looking for the "proper" 
                //way to do this, I can recommend a piece I stumbled on called The Right Way to Calculate Stuff by Don Hatch."
                // - Mike Pelton 
                yYaw = 0.0d;
            }

            Vector3d result;
            result.x = xPitch;
            result.y = yYaw;
            result.z = zRoll;
            return result;
        }

        // Rotation Arc
        // Reference, from Stan Melax in Game Gems I
        //  Quaternion q;
        //  vector3 c = CrossProduct(v0,v1);
        //  float   d = DotProduct(v0,v1);
        //  float   s = (float)sqrt((1+d)*2);
        //  q.x = c.x / s;
        //  q.y = c.y / s;
        //  q.z = c.z / s;
        //  q.w = s /2.0f;
        //  return q;
        public static Quaternion RotationArc(Vector3d v0, Vector3d v1)
        {
            Vector3d cross = Vector3d.CrossProduct(v0, v1);
            double d = Vector3d.DotProduct(v0, v1);
            double s = Math.Sqrt((1 + d) * 2);
            double recip = 1.0d / s;

            Vector3d res = cross * recip;
            return new Quaternion(res.x, res.y, res.z, s * 0.5d);
        }

        public static Matrix Add(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m1.M11 + m2.M11;
            result._mat[0, 1] = m1.M12 + m2.M12;
            result._mat[0, 2] = m1.M13 + m2.M13;
            result._mat[0, 3] = m1.M14 + m2.M14;
            result._mat[1, 0] = m1.M21 + m2.M21;
            result._mat[1, 1] = m1.M22 + m2.M22;
            result._mat[1, 2] = m1.M23 + m2.M23;
            result._mat[1, 3] = m1.M24 + m2.M24;
            result._mat[2, 0] = m1.M31 + m2.M31;
            result._mat[2, 1] = m1.M32 + m2.M32;
            result._mat[2, 2] = m1.M33 + m2.M33;
            result._mat[2, 3] = m1.M34 + m2.M34;
            result._mat[3, 0] = m1.M41 + m2.M41;
            result._mat[3, 1] = m1.M42 + m2.M42;
            result._mat[3, 2] = m1.M43 + m2.M43;
            result._mat[3, 3] = m1.M44 + m2.M44;
            return result;
        }

        public static Matrix Subtract(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m1.M11 - m2.M11;
            result._mat[0, 1] = m1.M12 - m2.M12;
            result._mat[0, 2] = m1.M13 - m2.M13;
            result._mat[0, 3] = m1.M14 - m2.M14;
            result._mat[1, 0] = m1.M21 - m2.M21;
            result._mat[1, 1] = m1.M22 - m2.M22;
            result._mat[1, 2] = m1.M23 - m2.M23;
            result._mat[1, 3] = m1.M24 - m2.M24;
            result._mat[2, 0] = m1.M31 - m2.M31;
            result._mat[2, 1] = m1.M32 - m2.M32;
            result._mat[2, 2] = m1.M33 - m2.M33;
            result._mat[2, 3] = m1.M34 - m2.M34;
            result._mat[3, 0] = m1.M41 - m2.M41;
            result._mat[3, 1] = m1.M42 - m2.M42;
            result._mat[3, 2] = m1.M43 - m2.M43;
            result._mat[3, 3] = m1.M44 - m2.M44;
            return result;
        }

        /// <summary>
        /// Short cut multiplication for matrices that have 0,0,0,1 in final column.  Cannot be used with perspective matrix for instance
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            return Multiply4x4(m1, m2);

            //Matrix result = new Matrix();
            //result._mat[0, 0] = (m1.M11*m2.M11) + (m1.M12*m2.M21) + (m1.M13*m2.M31);
            //result._mat[0, 1] = (m1.M11*m2.M12) + (m1.M12*m2.M22) + (m1.M13*m2.M32);
            //result._mat[0, 2] = (m1.M11*m2.M13) + (m1.M12*m2.M23) + (m1.M13*m2.M33);
            //result._mat[0, 3] = 0.0;

            //result._mat[1, 0] = (m1.M21*m2.M11) + (m1.M22*m2.M21) + (m1.M23*m2.M31);
            //result._mat[1, 1] = (m1.M21*m2.M12) + (m1.M22*m2.M22) + (m1.M23*m2.M32);
            //result._mat[1, 2] = (m1.M21*m2.M13) + (m1.M22*m2.M23) + (m1.M23*m2.M33);
            //result._mat[1, 3] = 0.0;

            //result._mat[2, 0] = (m1.M31*m2.M11) + (m1.M32*m2.M21) + (m1.M33*m2.M31);
            //result._mat[2, 1] = (m1.M31*m2.M12) + (m1.M32*m2.M22) + (m1.M33*m2.M32);
            //result._mat[2, 2] = (m1.M31*m2.M13) + (m1.M32*m2.M23) + (m1.M33*m2.M33);
            //result._mat[2, 3] = 0.0;

            //result._mat[3, 0] = (m1.M41*m2.M11) + (m1.M42*m2.M21) + (m1.M43*m2.M31) + m2.M41;
            //result._mat[3, 1] = (m1.M41*m2.M12) + (m1.M42*m2.M22) + (m1.M43*m2.M32) + m2.M42;
            //result._mat[3, 2] = (m1.M41*m2.M13) + (m1.M42*m2.M23) + (m1.M43*m2.M33) + m2.M43;
            //result._mat[3, 3] = 1.0;
            //return result;
        }

        /// <summary>
        /// Scalar multiplication is easy. You just take a regular number (called a "scalar") and multiply it on every entry in the matrix.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Multiply(Matrix m, double scalar)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m.M11 * scalar;
            result._mat[0, 1] = m.M12 * scalar;
            result._mat[0, 2] = m.M13 * scalar;
            result._mat[0, 3] = m.M14 * scalar;

            result._mat[1, 0] = m.M21 * scalar;
            result._mat[1, 1] = m.M22 * scalar;
            result._mat[1, 2] = m.M23 * scalar;
            result._mat[1, 3] = m.M24 * scalar;

            result._mat[2, 0] = m.M31 * scalar;
            result._mat[2, 1] = m.M32 * scalar;
            result._mat[2, 2] = m.M33 * scalar;
            result._mat[2, 3] = m.M34 * scalar;

            result._mat[3, 0] = m.M41 * scalar;
            result._mat[3, 1] = m.M42 * scalar;
            result._mat[3, 2] = m.M43 * scalar;
            result._mat[3, 3] = m.M44 * scalar;
            return result;
        }

        ///// <summary>
        ///// Full 4x4 multiplication for 4x4 matrices such as the projection matrix
        ///// </summary>
        ///// <param name="m1"></param>
        ///// <param name="m2"></param>
        ///// <returns></returns>
        //public static Matrix Multiply4x4(Matrix m1, Matrix m2)
        //{
        //    Matrix result = new Matrix();
        //    for (int i = 0; i < 4; i++) // rows
        //    {
        //        for (int j = 0; j < 4; j++) // columns
        //        {
        //            double value = 0;
        //            for (int k = 0; k < 4; k++) 
        //            {
        //                value += m1._mat[i, k] * m2._mat[k, j];
        //            }
        //            result._mat[i, j] = value;  // [row , column]
        //        }
        //    }
        //    return result;
        //}
        public static Matrix Multiply4x4(Matrix matrix1, Matrix matrix2)
        {
            Matrix result = new Matrix();
            result.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
            result.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
            result.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
            result.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
            result.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
            result.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
            result.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
            result.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
            result.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
            result.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
            result.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
            result.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
            result.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
            result.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
            result.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
            result.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
            return result;
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            return Matrix.Add(m1, m2);
        }
        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            return Matrix.Subtract(m1, m2);
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            return Matrix.Multiply(m1, m2);
        }

        public static Matrix operator *(Matrix m, double scalar)
        {
            return Matrix.Multiply(m, scalar);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Matrix == false) return false;

            return this.Equals((Matrix)obj);
        }

        public bool Equals(Matrix m)
        {
            return
                _mat[0, 0] == m.M11 &&
                _mat[0, 1] == m.M12 &&
                _mat[0, 2] == m.M13 &&
                _mat[0, 3] == m.M14 &&

                _mat[1, 0] == m.M21 &&
                _mat[1, 1] == m.M22 &&
                _mat[1, 2] == m.M23 &&
                _mat[1, 3] == m.M24 &&

                _mat[2, 0] == m.M31 &&
                _mat[2, 1] == m.M32 &&
                _mat[2, 2] == m.M33 &&
                _mat[2, 3] == m.M34 &&

                _mat[3, 0] == m.M41 &&
                _mat[3, 1] == m.M42 &&
                _mat[3, 2] == m.M43 &&
                _mat[3, 3] == m.M44;
        }

        public bool IsNullOrEmpty()
        {
            return _mat[0, 0] == 0d &&
                _mat[0, 1] == 0d &&
                _mat[0, 2] == 0d &&
                _mat[0, 3] == 0d &&

                _mat[1, 0] == 0d &&
                _mat[1, 1] == 0d &&
                _mat[1, 2] == 0d &&
                _mat[1, 3] == 0d &&

                _mat[2, 0] == 0d &&
                _mat[2, 1] == 0d &&
                _mat[2, 2] == 0d &&
                _mat[2, 3] == 0d &&

                _mat[3, 0] == 0d &&
                _mat[3, 1] == 0d &&
                _mat[3, 2] == 0d &&
                _mat[3, 3] == 0d;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
            return base.GetHashCode();
        }

        //// src http://www.idevgames.com/forum/archive/index.php/t-10866.html
        ////    >>Original post by jyk
        ////    >>Here's another option to consider. You can get the same interpolation as you would with quaternion slerp (albeit at greater expense) by finding the matrix that rotates from A to B, extracting the axis and angle from this matrix, scaling the angle, and then recomposing the matrix from the axis and angle and multiplying with A.
        ////    >>I'm at work now, but if you're interested in this method I can post details later (or perhaps someone else will in the meantime).


        ////Linear interpolation (Vectors, scalars):
        ////delta = b - a;
        ////// alpha = [0..1]
        ////c = a + delta*alpha

        ////3x3 rotation matrix form (right-hand element evaluated first):

        ////delta = b * transpose(a) // transpose(a) followed by b.
        ////delta.getAxisAngle(axis,deltaAngle)
        ////// alpha = [0..1]
        ////c = axisAngleToMatrix(axis,deltaAngle*alpha) * a

        //public void interpolate(Matrix a, Matrix b, float alpha, Matrix c) 
        //{
        //  Matrix delta = b ^ !a; // ^ = matrix product, ! = matrix transpose.
        //  Vector3d axis;
        //    float deltaAngle;
        //  delta.AxisAngle(ref axis, ref deltaAngle);
        //  Matrix rm = Matrix.Rotation(axis,deltaAngle*alpha);
        //  c = rm ^ a;
        //} // interpolate

        //        public Matrix AxisAngle(Vector3d axis, float deltaAngle)
        //        {

        //        }


        // void Matrix::setInverseTranslation( const float *translation )
        //{
        //    m_matrix[12] = -translation[0];
        //    m_matrix[13] = -translation[1];
        //    m_matrix[14] = -translation[2];
        //}

        //void Matrix::setRotationDegrees( const float *angles )
        //{
        //    float vec[3];
        //    vec[0] = ( float )( angles[0]*180.0/PI );
        //    vec[1] = ( float )( angles[1]*180.0/PI );
        //    vec[2] = ( float )( angles[2]*180.0/PI );
        //    setRotationRadians( vec );
        //}

        //void Matrix::setInverseRotationDegrees( const float *angles )
        //{
        //    float vec[3];
        //    vec[0] = ( float )( angles[0]*180.0/PI );
        //    vec[1] = ( float )( angles[1]*180.0/PI );
        //    vec[2] = ( float )( angles[2]*180.0/PI );
        //    setInverseRotationRadians( vec );
        //}

        //void Matrix::setRotationRadians( const float *angles )
        //{
        //    double cr = cos( angles[0] );
        //    double sr = sin( angles[0] );
        //    double cp = cos( angles[1] );
        //    double sp = sin( angles[1] );
        //    double cy = cos( angles[2] );
        //    double sy = sin( angles[2] );

        //    m_matrix[0] = ( float )( cp*cy );
        //    m_matrix[1] = ( float )( cp*sy );
        //    m_matrix[2] = ( float )( -sp );

        //    double srsp = sr*sp;
        //    double crsp = cr*sp;

        //    m_matrix[4] = ( float )( srsp*cy-cr*sy );
        //    m_matrix[5] = ( float )( srsp*sy+cr*cy );
        //    m_matrix[6] = ( float )( sr*cp );

        //    m_matrix[8] = ( float )( crsp*cy+sr*sy );
        //    m_matrix[9] = ( float )( crsp*sy-sr*cy );
        //    m_matrix[10] = ( float )( cr*cp );
        //}

        //void Matrix::setInverseRotationRadians( const float *angles )
        //{
        //    double cr = cos( angles[0] );
        //    double sr = sin( angles[0] );
        //    double cp = cos( angles[1] );
        //    double sp = sin( angles[1] );
        //    double cy = cos( angles[2] );
        //    double sy = sin( angles[2] );

        //    m_matrix[0] = ( float )( cp*cy );
        //    m_matrix[4] = ( float )( cp*sy );
        //    m_matrix[8] = ( float )( -sp );

        //    double srsp = sr*sp;
        //    double crsp = cr*sp;

        //    m_matrix[1] = ( float )( srsp*cy-cr*sy );
        //    m_matrix[5] = ( float )( srsp*sy+cr*cy );
        //    m_matrix[9] = ( float )( sr*cp );

        //    m_matrix[2] = ( float )( crsp*cy+sr*sy );
        //    m_matrix[6] = ( float )( crsp*sy-sr*cy );
        //    m_matrix[10] = ( float )( cr*cp );
        //}

        //void Matrix::setRotationQuaternion( const Quaternion& quat )
        //{
        //    m_matrix[0] = ( float )( 1.0 - 2.0*quat[1]*quat[1] - 2.0*quat[2]*quat[2] );
        //    m_matrix[1] = ( float )( 2.0*quat[0]*quat[1] + 2.0*quat[3]*quat[2] );
        //    m_matrix[2] = ( float )( 2.0*quat[0]*quat[2] - 2.0*quat[3]*quat[1] );

        //    m_matrix[4] = ( float )( 2.0*quat[0]*quat[1] - 2.0*quat[3]*quat[2] );
        //    m_matrix[5] = ( float )( 1.0 - 2.0*quat[0]*quat[0] - 2.0*quat[2]*quat[2] );
        //    m_matrix[6] = ( float )( 2.0*quat[1]*quat[2] + 2.0*quat[3]*quat[0] );

        //    m_matrix[8] = ( float )( 2.0*quat[0]*quat[2] + 2.0*quat[3]*quat[1] );
        //    m_matrix[9] = ( float )( 2.0*quat[1]*quat[2] - 2.0*quat[3]*quat[0] );
        //    m_matrix[10] = ( float )( 1.0 - 2.0*quat[0]*quat[0] - 2.0*quat[1]*quat[1] );
        //}
    }



    ////////////////////////////////////////////////////////////////////////////////////////////////
    // END TYPES


    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN PRIMITIVES

    public struct BoundingBox
    {
        private static Vector3d MIN_INIT = new Vector3d(float.MaxValue * .5f, float.MaxValue * .5f, float.MaxValue * .5f);
        private static Vector3d MAX_INIT = new Vector3d(float.MinValue * .5f, float.MinValue * .5f, float.MinValue * .5f);

        //private Vector3d[] _parameters;
        private Vector3d _min;
        private Vector3d _max;

		public enum BOX_FACES
		{
			RIGHT = 0, // +x
			LEFT = 1,  // -x
			TOP = 2,   // +y
			BOTTOM = 3,// -y
			FRONT = 4,  // +z // FRONT (+z) <--NOTE: "FRONT" (+z) denotes facing INTO the camera.  So if you place an Actor into the scene, the eyes of that actor will be facing away from you and into the Camera unless you apply a 180 y axis rotation in the assetplacementtgool logic
			BACK = 5  // -z
		}
		
        public static BoundingBox Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = new char[','];// keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            Vector3d min, max;
            min.x = double.Parse(values[0]);
            min.y = double.Parse(values[1]);
            min.z = double.Parse(values[2]);

            max.x = double.Parse(values[3]);
            max.y = double.Parse(values[4]);
            max.z = double.Parse(values[5]);

            return new BoundingBox(min, max);
        }

        public static BoundingBox Initialized()
        {
            BoundingBox box;
            box._min = MIN_INIT;
            box._max = MAX_INIT;

            return box;
        }

        public bool IsNullOrEmpty()
        {
            return (_min.x == 0d &&
                    _min.y == 0d &&
                    _min.z == 0d &&
                    _max.x == 0d &&
                    _max.y == 0d &&
                        _max.z == 0d);
        }

        /*
                public static BoundingBox FromBoundingRect(BoundingRect rect)
                {
                    Vector3d min, max;
                    min.x = rect.Min.x;
                    min.y = float.MinValue;
                    min.z = rect.Min.y;

                    max.x = rect.Max.x;
                    max.y = float.MaxValue;
                    max.z = rect.Max.y;
                    BoundingBox result = new BoundingBox(min, max);

                    return result;
                }
        */
        public override string ToString()
        {
            string delimiter = ","; // keymath.ParseHelper.English.XMLAttributeDelimiter;
            string s = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", Min.x, delimiter,
                                                                Min.y, delimiter,
                                                                Min.z, delimiter,
                                                                Max.x, delimiter,
                                                                Max.y, delimiter,
                                                                Max.z);
            return s;
        }

        public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Vector3d min, max;
            min.x = minX;
            min.y = minY;
            min.z = minZ;
            max.x = maxX;
            max.y = maxY;
            max.z = maxZ;

            _min = min;
            _max = max;
        }

        public BoundingBox(Vector3d min, Vector3d max)
        {
            // TODO: assert if width/height/depth of this box is > double.MaxValue 
            // TODO: assert if box is inversed such that any component of max is smaller than any component of max
            _min = min;
            _max = max;
        }


        // construct a square bounding box who's ceter is at "position" and who's
        // center points on each face are "radius" distance from the center.
        // This type of box will always full encompass a sphere of the same radius.  
        public BoundingBox(Vector3d position, double radius)
            :
                this(position.x - radius, position.y - radius, position.z - radius,
                        position.x + radius, position.y + radius, position.z + radius)
        {
        }

        // construct a square bounding box who's ceter is at "position" and who's
        // center points on each face are "radius" distance from the center.
        // This type of box will always full encompass a sphere of the same radius.  
        public BoundingBox(Vector3d position, float radius)
            :
                this(position.x - radius, position.y - radius, position.z - radius,
                        position.x + radius, position.y + radius, position.z + radius)
        {
        }
        // we use * .5f because if we try to take width of these it returns infinity since double overlows
        //        public BoundingBox()
        //            : this(MIN_INIT, MAX_INIT)
        //        {
        //        }

        //http://www.truevision3d.com/forums/tv3d_sdk_65/why_not_mesh_group_bounding_boxes-t17758.0.html
        // jviper's boundingbox for mesh groups
        //Function GetBoundingBox(MshTV as TVMesh,intGroup as integer,Transformed as boolean) as Box3D
        //    dim TVIO as TVInternalObjects
        //    dim TmpMesh as Microsoft.DirectX.Direct3D.Mesh
        //    dim Attr() as Mircrosoft.DirectX.Direct3D.AttributeRange
        //    dim Vec as Vector3d
        //    dim Ret as Box3D

        //    TmpMsh = New Microsoft.DirectX.Direct3D.Mesh(TVIO.GetD3DMesh(MshTV.GetIndex))
        //    Attr = TmpMsh.GetAttributeTable()
        //    Ret.Min=new Vector3d(single.maxvalue,single.maxvalue,single.maxvalue)   
        //    Ret.Max=new Vector3d(single.minvalue,single.minvalue,single.minvalue)   
        //    For i as integer=Attr(intGroup).VertexStart to Attr(intGroup).VertexStart+Attr(intGroup).VertexCount-1
        //        mshTV.GetVertex(i,Vec.x,Vec.y,Vec.z,0,0,0,0,0,0,0,0)
        //        if Transformed then vec=tvvec3transformcoord(vec,mshTV.GetMatrix)
        //        Ret.Min.x = min(Ret.Min.x,Vec.x)
        //        Ret.Min.y = min(Ret.Min.y,Vec.y)
        //        Ret.Min.z = min(Ret.Min.z,Vec.z)
        //        Ret.Max.x = max(Ret.Max.x,Vec.x)
        //        Ret.Max.y = max(Ret.Max.y,Vec.y)
        //        Ret.Max.z = max(Ret.Max.z,Vec.z)
        //    Next i
        //    Return Ret
        //End Function


        public Vector3d[] Vertices
        {
            get
            {
                return GetVertices(this);
            }
        }

        public Vector3d Min
        {
            get { return _min; }
            set
            {
                _min = value;
            }
        }

        public Vector3d Max
        {
            get { return _max; }
            set
            {
                _max = value;
            }
        }

        public double Height
        {
            get { return Max.y - Min.y; }
        }

        public double Width
        {
            get { return Max.x - Min.x; }
        }

        public double Depth
        {
            get { return Max.z - Min.z; }
        }

        /// <summary>
        /// True Radius which takes into account half the diagonal length from one corner to it's opposite.
        /// </summary>
        public double Radius
        {
            get
            {
                return Diameter * .5;
            }
        }

        public double RadiusSquared
        {
            get
            {
                double radius = Diameter * .5;
                return radius * radius;
            }
        }

        /// <summary>
        /// True diameter which takes into account the diagonal length from one corner to it's opposite.
        /// TODO: this is not actually diameter which is a diagonal line, this is the max axis length
        /// </summary>
        public double Diameter
        {
            get
            {
                double axisLength = _max.x - _min.x;
                axisLength = Math.Max(axisLength, _max.y - _min.y);
                return Math.Max(axisLength, _max.z - _min.z);


                //return (Max - Min).Length;
            }
        }

        public Vector3d Center
        {
            get
            {
                Vector3d result;
                result.x = Min.x + (Width * 0.5d);
                result.y = Min.y + (Height * 0.5d);
                result.z = Min.z + (Depth * 0.5d);
                return result;
            }
        }

        public void Translate(double translationX, double translationY, double translationZ)
        {
            _min.x += translationX;
            _min.y += translationY;
            _min.z += translationZ;

            _max.x += translationX;
            _max.y += translationY;
            _max.z += translationZ;
        }

        public void Translate(Vector3d translation)
        {
            Min += translation;
            Max += translation;
        }

        public void Scale(Vector3d scale)
        {
            Min *= scale;
            Max *= scale;
        }

        public static BoundingBox Scale(BoundingBox box, Vector3d scale)
        {
            Vector3d min = box.Min * scale;
            Vector3d max = box.Max * scale;
            return new BoundingBox(min, max);
        }

        public static BoundingBox Transform1(BoundingBox box, Matrix m)
        {
            // If we're empty, then bail


            // Start with the translation portion
            Vector3d min, max;
            min = max = new Vector3d(m.M41, m.M42, m.M43);

            // Examine each of the 9 matrix elements
            // and compute the new AABB

            if (m.M11 > 0.0d)
            {
                min.x += m.M11 * box.Min.x; max.x += m.M11 * box.Max.x;
            }
            else
            {
                min.x += m.M11 * box.Max.x; max.x += m.M11 * box.Min.x;
            }

            if (m.M12 > 0.0d)
            {
                min.y += m.M12 * box.Min.x; max.y += m.M12 * box.Max.x;
            }
            else
            {
                min.y += m.M12 * box.Max.x; max.y += m.M12 * box.Min.x;
            }

            if (m.M13 > 0.0d)
            {
                min.z += m.M13 * box.Min.x; max.z += m.M13 * box.Max.x;
            }
            else
            {
                min.z += m.M13 * box.Max.x; max.z += m.M13 * box.Min.x;
            }

            if (m.M21 > 0.0d)
            {
                min.x += m.M21 * box.Min.y; max.x += m.M21 * box.Max.y;
            }
            else
            {
                min.x += m.M21 * box.Max.y; max.x += m.M21 * box.Min.y;
            }

            if (m.M22 > 0.0d)
            {
                min.y += m.M22 * box.Min.y; max.y += m.M22 * box.Max.y;
            }
            else
            {
                min.y += m.M22 * box.Max.y; max.y += m.M22 * box.Min.y;
            }

            if (m.M23 > 0.0d)
            {
                min.z += m.M23 * box.Min.y; max.z += m.M23 * box.Max.y;
            }
            else
            {
                min.z += m.M23 * box.Max.y; max.z += m.M23 * box.Min.y;
            }

            if (m.M31 > 0.0d)
            {
                min.x += m.M31 * box.Min.z; max.x += m.M31 * box.Max.z;
            }
            else
            {
                min.x += m.M31 * box.Max.z; max.x += m.M31 * box.Min.z;
            }

            if (m.M32 > 0.0d)
            {
                min.y += m.M32 * box.Min.z; max.y += m.M32 * box.Max.z;
            }
            else
            {
                min.y += m.M32 * box.Max.z; max.y += m.M32 * box.Min.z;
            }

            if (m.M33 > 0.0d)
            {
                min.z += m.M33 * box.Min.z; max.z += m.M33 * box.Max.z;
            }
            else
            {
                min.z += m.M33 * box.Max.z; max.z += m.M33 * box.Min.z;
            }

            return new BoundingBox(min, max);
        }
        // This should only be used when the origin of the box and the origin of the mesh are the same.
        // In other words, if the mesh's center is in the center of the mesh.  Remember that often times
        // actors and other meshes will have their center.Y at the foot of the mesh and rotation occur
        // about that position.
        // A faster looking method simply takes the extents and the center 
        // transforms the center and then creates a new box 
        public static BoundingBox Transform2(BoundingBox src, Matrix xform)
        {
            // get center and transform
            Vector3d c = (src.Min + src.Max) * 0.5f;
            c = Vector3d.TransformCoord(c, xform);

            // get extent and transform
            Vector3d e = (src.Max - src.Min) * 0.5f;
            Matrix m = new Matrix();

            // working just with scaling and rotation
            m.M11 = Math.Abs(xform.M11);
            m.M12 = Math.Abs(xform.M12);
            m.M13 = Math.Abs(xform.M13);
            m.M14 = 0.0f;

            m.M21 = Math.Abs(xform.M21);
            m.M22 = Math.Abs(xform.M22);
            m.M23 = Math.Abs(xform.M23);
            m.M24 = 0.0f;

            m.M31 = Math.Abs(xform.M31);
            m.M32 = Math.Abs(xform.M32);
            m.M33 = Math.Abs(xform.M33);
            m.M34 = 0.0f;

            m.M41 = 0.0f;
            m.M42 = 0.0f;
            m.M43 = 0.0f;
            m.M44 = 1.0f;

            // use transform normal to 
            e = Vector3d.TransformNormal(e, m);

            // convert back to bounding box representation
            return new BoundingBox(c - e, c + e);
        }

        public static BoundingBox Transform(BoundingBox box, Matrix matrix)
        {
            // do not attempt to transform a box that is not initialized.  Instead
            // return the original box
            if (box.Min == MIN_INIT && box.Max == MAX_INIT)
                return box;
            // when transforming a local box to world, you cannot (unfortunately) simply
            // transform the min and max coords.  You have to transform all 8 and then take the min,max of those.
            //Vector3d worldMax = new Vector3d(double.MinValue , double.MinValue , double.MinValue  );
            //Vector3d worldMin = new Vector3d(double.MaxValue, double.MaxValue, double.MaxValue);
            Vector3d worldMax, worldMin;
            worldMax.x = double.MinValue;
            worldMax.y = double.MinValue;
            worldMax.z = double.MinValue;
            worldMin.x = double.MaxValue;
            worldMin.y = double.MaxValue;
            worldMin.z = double.MaxValue;

            Vector3d v2;

            Vector3d[] verts = box.Vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                v2 = Vector3d.TransformCoord(verts[i], matrix);
                worldMax.x = Math.Max(worldMax.x, v2.x);
                worldMax.y = Math.Max(worldMax.y, v2.y);
                worldMax.z = Math.Max(worldMax.z, v2.z);
                worldMin.x = Math.Min(worldMin.x, v2.x);
                worldMin.y = Math.Min(worldMin.y, v2.y);
                worldMin.z = Math.Min(worldMin.z, v2.z);
            }

            //          // TODO: http://dev.theomader.com/transform-bounding-boxes/ ?
            //          var xa = m.Right * boundingBox.Min.X;
            //		    var xb = m.Right * boundingBox.Max.X;
            //		 
            //		    var ya = m.Up * boundingBox.Min.Y;
            //		    var yb = m.Up * boundingBox.Max.Y;
            //		 
            //		    var za = m.Backward * boundingBox.Min.Z;
            //		    var zb = m.Backward * boundingBox.Max.Z;
            //		 
            //		    return new BoundingBox(
            //		        Vector3.Min(xa, xb) + Vector3.Min(ya, yb) + Vector3.Min(za, zb) + m.Translation,
            //		        Vector3.Max(xa, xb) + Vector3.Max(ya, yb) + Vector3.Max(za, zb) + m.Translation

            return new BoundingBox(worldMin, worldMax);
        }


        /*  public bool Intersects(Ray r, double t0, double t1, out double distance1, out double distance2)
            {
                distance1 = distance2 = 0;

                // https://people.csail.mit.edu/amy/papers/box-jgt.pdf
                Vector3d[] parameters = new Vector3d[2];
                parameters[0] = _min;
                parameters[1] = _max;
                double tXmin = (parameters[r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
                double tXmax = (parameters[1 - r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
                double tymin = (parameters[r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;
                double tymax = (parameters[1 - r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;

                // TODO: is there an issue in this method of failing to collide when the t0 and t1 are both inside the 
                // min/max of the bounding box such that there is no collision with any plane?  In that way, the box "contains"
                // the ray but never intersects it.

                //         distance1 = tXmin;
                //         distance2 = tXmax;

                if ((tXmin > tymax) || (tymin > tXmax)) return false;
                // consolidate min/max into txmin and txmax respectively
                if (tymin > tXmin)
                    tXmin = tymin;
                if (tymax < tXmax)
                    tXmax = tymax;

                double tzmin = (parameters[r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;
                double tzmax = (parameters[1 - r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;

                if ((tXmin > tzmax) || (tzmin > tXmax)) return false;
                // consolidate min/max into txmin and txmax respectively
                if (tzmin > tXmin)
                    tXmin = tzmin;
                if (tzmax < tXmax)
                    tXmax = tzmax;

                // The code from this lesson returns intersections with the box which are in front or behind 
                // the origin of the ray. For instance, if the ray's origin is inside the box (like in the 
                // image on the right), there will be two intersections: one in front of the ray and one behind.
                // We know that an intersection is "behind" the origin of the ray when the value for t is negative.
                // When t is positive, the intersection is in front of the origin of the ray. If your algorithm
                // is not interested in intersections for values of t lower than 0, then you will have to carefully
                // deal with these cases when you return from the ray-box intersection box (as it is often a source 
                // of bugs).

                distance1 = tXmin;
                distance2 = tXmax;

                // if -1 for t0 and t1, no min/max range testing wanted
                if (t0 == -1d || t1 == -1d) return true;

                // return true if any part of collision segmewnt overlaps the min/max range
                return ((tXmin < t1) && (tXmax > t0));
            }

            /// <summary>Ray-box intersection using IEEE numerical properties to ensure 
            ///  that the test is both robust and efficient, as described in:
            /// 
            ///       Amy Williams, Steve Barrus, R. Keith Morley, and Peter Shirley
            ///       "An Efficient and Robust Ray-Box Intersection Algorithm"
            ///       Journal of graphics tools, 10(1):49-54, 2005
            ///        
            /// t0 and t1 accept a valid intersection interval.  In this way
            /// you can ignore positive hits that are too close or too far away
            /// from the desired area you're testing. (e.g. in a game with an avatar
            /// testing only the length by which the player traveled since the last frame
            /// is good enough for t1 and perhaps t0 being 0 or very close to it .001.
            /// Same principle works for collision of bullets and particle lasers between frames.
            /// </summary>
            /// <param name="r"></param>
            /// <param name="t0">Start interval</param>
            /// <param name="t1">End interval</param>
            /// <returns></returns>
            public bool Intersects(Ray r, double t0, double t1)
            {
                //https://tavianator.com/2011/ray_box.html
                // http://www.scratchapixel.com/lessons/3d-basic-lessons/lesson-7-intersecting-simple-shapes/ray-box-intersection/
                Vector3d[] parameters = new Vector3d[2];
                parameters[0] = _min;
                parameters[1] = _max;
                double tXmin = (parameters[r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
                double tXmax = (parameters[1 - r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
                double tymin = (parameters[r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;
                double tymax = (parameters[1 - r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;

                // TODO: is there an issue in this method of failing to collide when the t0 and t1 are both inside the 
                // min/max of the bounding box such that there is no collision with any plane?  In that way, the box "contains"
                // the ray but never intersects it.


                if ((tXmin > tymax) || (tymin > tXmax)) return false;
                // consolidate min/max into txmin and txmax respectively
                if (tymin > tXmin)
                    tXmin = tymin;
                if (tymax < tXmax)
                    tXmax = tymax;

                double tzmin = (parameters[r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;
                double tzmax = (parameters[1 - r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;

                if ((tXmin > tzmax) || (tzmin > tXmax)) return false;
                // consolidate min/max into txmin and txmax respectively
                if (tzmin > tXmin)
                    tXmin = tzmin;
                if (tzmax < tXmax)
                    tXmax = tzmax;

                // The code from this lesson returns intersections with the box which are in front or behind 
                // the origin of the ray. For instance, if the ray's origin is inside the box (like in the 
                // image on the right), there will be two intersections: one in front of the ray and one behind.
                // We know that an intersection is "behind" the origin of the ray when the value for t is negative.
                // When t is positive, the intersection is in front of the origin of the ray. If your algorithm
                // is not interested in intersections for values of t lower than 0, then you will have to carefully
                // deal with these cases when you return from the ray-box intersection box (as it is often a source 
                // of bugs).

                // if -1 for t0 and t1, no min/max range testing wanted
                if (t0 == -1d || t1 == -1d) return true;

                // return true if any part of collision segmewnt overlaps the min/max range
                return ((tXmin < t1) && (tXmax > t0));
            }

            // also a good article on various collision detections
            // http://www.harveycartel.org/metanet/tutorials/tutorialA.html
            // a simple collision response so that two colliding boxes dont penetrate
            // A this point we've already determined that the boxes intersect...

            //float dist[4];

            //dist[0] = box1.max.x - box2.min.x;
            //dist[1] = box2.max.x - box1.min.x;
            //dist[2] = box1.max.y - box2.min.y;
            //dist[3] = box2.max.y - box1.min.y;

            //size_t direction = std::distance(dist, std::min_element(dist, dist + 4));

            //switch (direction) {
            //    case 0: /* Move box1 along -x by dist[0] */ // break;
                                                              //    case 1: /* Move box1 along +x by dist[1] */ break;
                                                              //    case 2: /* Move box1 along -y by dist[2] */ break;
                                                              //    case 3: /* Move box1 along +y by dist[3] */ break;
                                                              //}



        //returns if the passed inbox is contained in whole or in part with the existing box
        //NOTE: We must test both boxes against each other because if one box totally encompasses the other
        // then none of its corners will be in the bounds of the other, but the opposite WILL be true.
        // sadly, the worst case represents a maximum of 24 tests but we can usually bail out early 
        //TODO: I think technically, if the boxes arent necessarily exact but have all 4 corners on the same
        // planes as two sides each then it might not register as being "contained" since its more accurately "on" the other.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(BoundingBox box)
        {

            bool result = (this.Min.x < box.Max.x) && (this.Max.x > box.Min.x) &&
                (this.Min.y < box.Max.y) && (this.Max.y > box.Min.y) &&
                (this.Min.z < box.Max.z) && (this.Max.z > box.Min.z);

            return result;

            Vector3d[] points = box.Vertices;
            // if any of the corners of the target box are contained in the src box, return true.
            for (int i = 0; i < points.Length; i++)
            {
                if (Contains(points[i]))
                    return true;
            }

            points = Vertices;
            for (int i = 0; i < box.Vertices.Length; i++)
            {
                if (box.Contains(points[i]))
                    return true;
            }

            // or in the unlikely event these boxes are identicle
            return box.Equals(this);
        }

        /// <summary>
        /// Returns true if the box passed in is entirely contained in the bounds of this box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool Contains(BoundingBox box)
        {
            //Console.WriteLine("Contains");
            return Contains(box.Vertices);

        }

        // returns true if _all_ points are contained
        public bool Contains(Vector3d[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (!Contains(points[i])) return false;
            }
            return true;
        }

        public bool Contains(Vector3d point)
        {
            return (point.x >= Min.x && point.x <= Max.x &&
                    point.y >= Min.y && point.y <= Max.y &&
                    point.z >= Min.z && point.z <= Max.z);
        }

        public bool Contains(double pointX, double pointY, double pointZ)
        {
            return (pointX >= Min.x && pointX <= Max.x &&
                    pointY >= Min.y && pointY <= Max.y &&
                    pointZ >= Min.z && pointZ <= Max.z);
        }

        ///// <summary>
        ///// performs intersection testing based on the separating axis theorem. As soon as a separating axis is found, the function returns.
        ///// </summary>
        ///// <param name="box"></param>
        ///// <returns></returns>
        //public bool Intersects2 (BoundingBox box)
        //{

        //    // A = normals of the faces that touch the minimum vector
        //    // B = normals of the faces that touch the maximum vector
        //   // Vector3d[]    CA =  A[3];
        //    Vector3d      CB = B[3];
        //    Vector3d      T (CB - CA);

        //    double         rA;
        //    double         rB;
        //    double         rT;
        //    Vector3d        L;

        //    for (int i = 0; i < 3; ++i)
        //    {
        //        L = A[i];

        //        rA = Math.Abs(A[0].VDot(L)) + Math.Abs(A[1].VDot(L)) + Math.Abs(A[2].VDot(L));
        //        rB = Math.Abs(B[0].VDot(L)) + Math.Abs(B[1].VDot(L)) + Math.Abs(B[2].VDot(L));
        //        rT = Math.Abs(T.VDot(L));

        //        if (rT > rA + rB)
        //            return false;

        //        L = B[i];
        //        rA = Math.Abs(A[0].VDot(L)) + Math.Abs(A[1].VDot(L)) + Math.Abs(A[2].VDot(L));
        //        rB = Math.Abs(B[0].VDot(L)) + Math.Abs(B[1].VDot(L)) + Math.Abs(B[2].VDot(L));
        //        rT = Math.Abs(T.VDot(L));

        //        if (rT > rA + rB)
        //            return false;
        //    }

        //    // and now for the cross product axes
        //    for (int i = 0; i < 3; ++i)
        //        for (int j = 0; j < 3; ++j)
        //        {
        //            L = A[i].VCross(B[j]);
        //            rA = Math.Abs(A[0].VDot(L)) + Math.Abs(A[1].VDot(L)) + Math.Abs(A[2].VDot(L));
        //            rB = Math.Abs(B[0].VDot(L)) + Math.Abs(B[1].VDot(L)) + Math.Abs(B[2].VDot(L));
        //            rT = Math.Abs(T.VDot(L));

        //            if (rT > rA + rB)
        //                return false;
        //        }
        //    return true;
        //}


        public void Reset()
        {
            _min = MIN_INIT;
            _max = MAX_INIT;
        }

        public void Resize(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            _min.x = minX;
            _min.y = minY;
            _min.z = minZ;
            _max.x = maxX;
            _max.y = maxY;
            _max.z = maxZ;
        }

        /// <summary>
        /// Combines the dimensions of the parameter box to this existing box.
        /// </summary>
        /// <param name="target"></param>
        public void Combine(BoundingBox target)
        {
            Vector3d min, max;
            min.x = Math.Min(_min.x, target._min.x);
            min.y = Math.Min(_min.y, target._min.y);
            min.z = Math.Min(_min.z, target._min.z);

            max.x = Math.Max(_max.x, target._max.x);
            max.y = Math.Max(_max.y, target._max.y);
            max.z = Math.Max(_max.z, target._max.z);

            _min = min;
            _max = max;
        }

        //When combining bounding volumes, an uninitialized box will have 0,0,0 for both min & max vectors
        //This is usually unintional so make sure your bounding boxes are initialized with proper min/max vectors.
        public static BoundingBox Combine(BoundingBox b1, BoundingBox b2)
        {
            // TODO: I need a version of Combine that is NOT static and which will simply
            //       increase the size of the existing
            Vector3d min, max;
            min.x = Math.Min(b1.Min.x, b2.Min.x);
            min.y = Math.Min(b1.Min.y, b2.Min.y);
            min.z = Math.Min(b1.Min.z, b2.Min.z);

            max.x = Math.Max(b1.Max.x, b2.Max.x);
            max.y = Math.Max(b1.Max.y, b2.Max.y);
            max.z = Math.Max(b1.Max.z, b2.Max.z);
            return new BoundingBox(min, max);
        }

		
        ///<summary>
        /// Returns all the Vertices of each QuadFace of the BoundingBox.
        /// IMPORTANT: ArmorFace indices match those this BoundingBox.GetQuadFaceVerices()
		// Both also matches enums for TV3D CUBEMAP faces.
		// 0: Positive X (Right)1: Negative X (Left)2: Positive Y (Top)3: Negative Y (Bottom)4: Positive Z (Front)5: Negative Z (Back)
        /// </summary>
        public static Vector3d[,] GetQuadFaceVertices(BoundingBox box)
        {
            Vector3d[,] vertices = new Vector3d[6, 4];
            // NOTE: for AABB the first subscript 0 to 5 indices correspond with 
            //the CUBEMAP_FACE enumeration such that
            // face 0 is the PositiveX (RIGHT)
            // face 1 is the NegativeX (LEFT)
            // face 2 is the PositiveY  (TOP)
            // face 3 is the NegativeY (BOTTOM)
            // face 4 is the PositiveZ (BACK)
            // face 5 is the NegativeZ (FRONT)

            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THUS 
            // 6 ___ 7
            // |    |         TOP - as seen from OUTSIDE the box looking down at it
            // 4 ___ 5
            //     
            //   2 ___ 3
            //   |    |        BOTTOM - as seen from INSIDE the box looking down at it.  (NOTE: 0, 1, 3, 2 is CLOCKWISE _IF_ looking from OUTSIDE of the box (eg underneath it looking up at it)
            //   0 ___ 1
            // is our layout     

            // TOP quad (PositiveY)
            vertices[(int)BOX_FACES.TOP, 0] = new Vector3d(box.Min.x, box.Max.y, box.Min.z);  // v4
            vertices[(int)BOX_FACES.TOP, 1] = new Vector3d(box.Max.x, box.Max.y, box.Min.z);  // v5
            vertices[(int)BOX_FACES.TOP, 2] = new Vector3d(box.Min.x, box.Max.y, box.Max.z);  // v6
            vertices[(int)BOX_FACES.TOP, 3] = new Vector3d(box.Max.x, box.Max.y, box.Max.z);  // v7

            // BOTTOM quad (NegativeY)
            vertices[(int)BOX_FACES.BOTTOM, 0] = new Vector3d(box.Min.x, box.Min.y, box.Min.z);   // v0
            vertices[(int)BOX_FACES.BOTTOM, 1] = new Vector3d(box.Max.x, box.Min.y, box.Min.z);   // v1
            vertices[(int)BOX_FACES.BOTTOM, 2] = new Vector3d(box.Min.x, box.Min.y, box.Max.z);   // v2
            vertices[(int)BOX_FACES.BOTTOM, 3] = new Vector3d(box.Max.x, box.Min.y, box.Max.z);   // v3

			// --
            // the other 4 quads use a combination of the previous top and bottom vertices 
            
            // PostiveX (RIGHT) 
            vertices[(int)BOX_FACES.RIGHT, 0] = vertices[(int)BOX_FACES.BOTTOM, 1];   // v1
            vertices[(int)BOX_FACES.RIGHT, 1] = vertices[(int)BOX_FACES.BOTTOM, 3];   // v3
            vertices[(int)BOX_FACES.RIGHT, 2] = vertices[(int)BOX_FACES.TOP, 3];   // v7
            vertices[(int)BOX_FACES.RIGHT, 3] = vertices[(int)BOX_FACES.TOP, 1];   // v5

            // NegativeX (LEFT)
            vertices[(int)BOX_FACES.LEFT, 0] = vertices[(int)BOX_FACES.BOTTOM, 2];   // v2
            vertices[(int)BOX_FACES.LEFT, 1] = vertices[(int)BOX_FACES.BOTTOM, 0];   // v0
            vertices[(int)BOX_FACES.LEFT, 2] = vertices[(int)BOX_FACES.TOP, 0];   // v4
            vertices[(int)BOX_FACES.LEFT, 3] = vertices[(int)BOX_FACES.TOP, 2];   // v6

            // PositiveZ (FRONT)
            vertices[(int)BOX_FACES.FRONT, 0] = vertices[(int)BOX_FACES.BOTTOM, 3];   // v3
            vertices[(int)BOX_FACES.FRONT, 1] = vertices[(int)BOX_FACES.BOTTOM, 2];   // v2
            vertices[(int)BOX_FACES.FRONT, 2] = vertices[(int)BOX_FACES.TOP, 2];   // v6
            vertices[(int)BOX_FACES.FRONT, 3] = vertices[(int)BOX_FACES.TOP, 3];   // v7

            // NegativeZ (BACK)
            vertices[(int)BOX_FACES.BACK, 0] = vertices[(int)BOX_FACES.BOTTOM, 0];   // v0
            vertices[(int)BOX_FACES.BACK, 1] = vertices[(int)BOX_FACES.BOTTOM, 1];   // v1
            vertices[(int)BOX_FACES.BACK, 2] = vertices[(int)BOX_FACES.TOP, 1];   // v5
            vertices[(int)BOX_FACES.BACK, 3] = vertices[(int)BOX_FACES.TOP, 0];   // v4
			
            return vertices;
        }

        ///<summary>
        /// If we only need the vertices of the BoundingBox, use this method.
        /// </summary>
        public static Vector3d[] GetVertices(BoundingBox box)
        {
           //Console.WriteLine("Get Vertices");
            Vector3d[] vertices = new Vector3d[8];

            
            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THUS 
            // 6 ___ 7
            // |    |         TOP - as seen from OUTSIDE the box looking down at it
            // 4 ___ 5
            //     
            //   2 ___ 3
            //   |    |        BOTTOM - as seen from INSIDE the box looking down at it.  (NOTE: 0, 1, 3, 2 is CLOCKWISE _IF_ looking from OUTSIDE of the box (eg underneath it looking up at it)
            //   0 ___ 1
            // is our layout   
            
            // BOTTOM v0
            vertices[0].x = box.Min.x;
            vertices[0].y = box.Min.y;
            vertices[0].z = box.Min.z;

            // BOTTOM v1
            vertices[1].x = box.Max.x;
            vertices[1].y = box.Min.y;
            vertices[1].z = box.Min.z;
            
            // BOTTOM v2
            vertices[2].x = box.Min.x;
            vertices[2].y = box.Min.y;
            vertices[2].z = box.Max.z;
            
            // BOTTOM v3
            vertices[3].x = box.Max.x;
            vertices[3].y = box.Min.y;
            vertices[3].z = box.Max.z;

            // ------------------------
            // TOP v4
            vertices[4].x = box.Min.x;
            vertices[4].y = box.Max.y;
            vertices[4].z = box.Min.z;
            
            // TOP v5
            vertices[5].x = box.Max.x;
            vertices[5].y = box.Max.y;
            vertices[5].z = box.Min.z;
            
            // TOP v6
            vertices[6].x = box.Min.x;
            vertices[6].y = box.Max.y;
            vertices[6].z = box.Max.z;
            
            // TOP v7
            vertices[7].x = box.Max.x;
            vertices[7].y = box.Max.y;
            vertices[7].z = box.Max.z;
            return vertices;
        }

        
        /// <summary>
        /// Constructs the 12 edges of the bouding box
        /// </summary>
        public static Line3d[] GetEdges(BoundingBox box)
        {
            Vector3d[] vertices = box.Vertices;
            Line3d[] edges = new Line3d[12];
            // X-aligned lines on both sides, both heights
            edges[0] = new Line3d(vertices[0], vertices[1]);
            edges[1] = new Line3d(vertices[2], vertices[3]);
            edges[2] = new Line3d(vertices[4], vertices[5]);
            edges[3] = new Line3d(vertices[6], vertices[7]);

            // Y-aligned lines at each corner
            edges[4] = new Line3d(vertices[0], vertices[4]);
            edges[5] = new Line3d(vertices[2], vertices[6]);
            edges[6] = new Line3d(vertices[1], vertices[5]);
            edges[7] = new Line3d(vertices[3], vertices[7]);

            // Z-aligned lines on both sides, both heights
            edges[8] = new Line3d(vertices[0], vertices[2]);
            edges[9] = new Line3d(vertices[1], vertices[3]);
            edges[10] = new Line3d(vertices[4], vertices[6]);
            edges[11] = new Line3d(vertices[5], vertices[7]);

            return edges;
        }

        public static Triangle[] GetTriangleFaces(BoundingBox box)
        {
            // construct 12 triangles from our bounding box vertices.  
            
            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THUS 
            // 6 ___ 7
            // |    |         TOP - as seen from OUTSIDE the box looking down at it
            // 4 ___ 5
            //     
            //   2 ___ 3
            //   |    |        BOTTOM - as seen from INSIDE the box looking down at it.  (NOTE: 0, 1, 3, 2 is CLOCKWISE _IF_ looking from OUTSIDE of the box (eg underneath it looking up at it)
            //   0 ___ 1
            // is our layout   
            Triangle[] tris = new Triangle[12];
            Vector3d[] v = box.Vertices;

            // bottom 2 faces
            tris[0] = new Triangle(v[0], v[1], v[3]);
            tris[1] = new Triangle(v[0], v[3], v[2]);

            // top 2 faces
            tris[10] = new Triangle(v[4], v[6], v[7]);
            tris[11] = new Triangle(v[4], v[7], v[5]);

            // right 2 faces
            tris[8] = new Triangle(v[1], v[7], v[3]);
            tris[9] = new Triangle(v[7], v[1], v[5]); 

            // left 2 faces
            tris[4] = new Triangle(v[2], v[6], v[0]); 
            tris[5] = new Triangle(v[2], v[4], v[0]);

            // FRONT 2 faces (+z, facing into the camera.  In fact if this were not so, the RIGHT 2 faces above would not be correct (but they are correct)
            tris[6] = new Triangle(v[3], v[6], v[2]); 
            tris[7] = new Triangle(v[3], v[7], v[6]);

            // back 2 faces (-z , closest to camera)
            tris[2] = new Triangle(v[0], v[4], v[1]); 
            tris[3] = new Triangle(v[1], v[4], v[5]);

            return tris;
        }

        public static Polygon[] GetPolyFaces(BoundingBox box)
        {
            
            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THUS 
            // 6 ___ 7
            // |    |         TOP - as seen from OUTSIDE the box looking down at it
            // 4 ___ 5
            //     
            //   2 ___ 3
            //   |    |        BOTTOM - as seen from INSIDE the box looking down at it.  (NOTE: 0, 1, 3, 2 is CLOCKWISE _IF_ looking from OUTSIDE of the box (eg underneath it looking up at it)
            //   0 ___ 1
            // is our layout   
			
            Polygon[] polys = new Polygon[6];
            Vector3d[] v = box.Vertices;

            // bottom face
            polys[(int)BOX_FACES.BOTTOM] = new Polygon(v[0], v[1], v[3], v[2]);

            // top face
            polys[(int)BOX_FACES.TOP] = new Polygon(v[4], v[6], v[7], v[5]);

            // the side faces
            polys[(int)BOX_FACES.LEFT] = new Polygon(v[0], v[2], v[6], v[4]); // left 
            polys[(int)BOX_FACES.RIGHT] = new Polygon(v[1], v[5], v[7], v[3]); // right
			
			polys[(int)BOX_FACES.FRONT] = new Polygon(v[3], v[7], v[6], v[2]); // front // FRONT (+z) <--NOTE: "FRONT" (+z) denotes facing INTO the camera.  So if you place an Actor into the scene, the eyes of that actor will be facing away from you and into the Camera unless you apply a 180 y axis rotation in the assetplacementtgool logic
            polys[(int)BOX_FACES.BACK] = new Polygon(v[0], v[4], v[5], v[1]); // back // -z, face closest to the camera.
            
            return polys;
        }


		// one good thing is this code can be used for our imposter code too
		// find the minimum and maximum distance needed to enclose that box on the supplied axis.
		public static void GetProjectedDistances(BoundingBox box, Vector3d OnVector, out double NearDistance,
												 out double FarDistance)
		{
			double FarAssociatedNear = double.MinValue;
			NearDistance = double.MaxValue;
			FarDistance = double.MinValue;
			const double DEPTH_BIAS = 1f;

			Line3d[] edges = GetEdges(box);
			Trace.Assert(edges.Length == 12);
			foreach (Line3d e in edges)
			{
				// the projected offset gives us the seperation from this edge vertex and the supplied axis vector
				double ProjectedOffset = Vector3d.DotProduct(e.Point[0], OnVector);
				// subtract the unscaled direction vector from our supplied axis and compute the dot product
				// this dot product gives us the scalar projection of this direction vector onto our OnVector
				double ProjectedVector =
					Vector3d.DotProduct(e.Point[1] - e.Point[0], OnVector);

				double CurrentNear = ProjectedOffset;
				if (ProjectedVector < 0)
					CurrentNear += ProjectedVector;

				NearDistance = Math.Min(NearDistance, CurrentNear);
				double CurrentFar = ProjectedVector * Math.Sign(ProjectedVector);

				if (CurrentNear + CurrentFar > FarAssociatedNear + FarDistance)
				{
					FarAssociatedNear = CurrentNear;
					FarDistance = CurrentFar;
				}
			}

			FarDistance += FarAssociatedNear - NearDistance;
			FarDistance += DEPTH_BIAS;
			NearDistance -= DEPTH_BIAS;
		}
        
        public override bool Equals(object bb)
        {
            if (bb is BoundingBox == false) return false;

            return this == (BoundingBox)bb;
        }

        public override int GetHashCode()
        {
            return this.Min.GetHashCode() + this.Max.GetHashCode();
        }

        // Equality operator. Returns dbNull if either operand is dbNull, 
        // otherwise returns dbTrue or dbFalse:
        public static bool operator ==(BoundingBox a, BoundingBox b)
        {
            if (a.Min == b.Min && a.Max == b.Max) return true;
            return false;
        }

        // Inequality operator. Returns dbNull if either operand is
        // dbNull, otherwise returns dbTrue or dbFalse:
        public static bool operator !=(BoundingBox a, BoundingBox b)
        {
            if (a.Min != b.Min || a.Max != b.Max) return true;
            return false;
        }
    }


	public enum IntersectResult
    	{
		OUTSIDE = 0,
		INTERSECT,
		// unlike partially visible, these are good for quadtree node culling to eliminate testing children.  sicne by definition, if a parent is fully visible (or not visible) than so are its children.
		INSIDE
		}
		
    public class BoundingSphere
    {
        private Vector3d _center;
        private double _radius;

        /// <summary>
        /// Calculates a bounding sphere to encompass a bounding box.
        /// </summary>
        /// <remarks>
        /// It takes into account the diagonal extents of the box, not just the max axis length
        /// </remarks>
        /// <param name="box"></param>
        public BoundingSphere(BoundingBox box) : this (box.Center, (box.Max - box.Min).Length / 2d)
        {
        }
        
        public BoundingSphere(BoundingSphere sphere) : this (sphere._center, sphere._radius)
        {
        }
        
        public BoundingSphere(Vector3d center, double radius)
        {
            _center = center;
            _radius = radius;
        }

        public BoundingSphere (double centerX, double centerY, double centerZ, double radius)
        {
            _center.x = centerX;
            _center.y = centerY;
            _center.z = centerZ;
            _radius = radius;
        }

        // Sphere to sphere can be a faster test, but results in much greater overdraw because the sphere is HUGE to make 
        // sure it doesnt cull things that are visible.  This is best used as a preliminary stage cull.
        public BoundingSphere(float nearPlane, float farPlane, float fovRadians,
                              Vector3d cameraPosition, Vector3d lookAt)
        {
            double diameter = farPlane - nearPlane;
            double sngRadius = diameter * 0.5;
            double farPlaneHeight = diameter * System.Math.Tan(fovRadians * 0.5);

            // with an aspect ratio of 1, our width = height
            double farPlaneWidth = farPlaneHeight;

            //TODO: once we have the radius, we dont actually have to update it unless the far/near/fov changes
            // but we'll still always need to update the center based on the LookAt
            Vector3d center;
            center.x = 0;
            center.y = 0;
            center.z = nearPlane + sngRadius;

            Vector3d farCorner;
            farCorner.x = farPlaneWidth;
            farCorner.y = farPlaneHeight;
            farCorner.z = diameter;

            // the frustum sphere radius becomes the length of this vector
            _radius = (farCorner - center).Length;

            // TODO: Below is actually the only things that need to be updated every frame.  So to optimize
            // this seperate out the FrustumSphereInitialization from the FrustumSphereUpdate.  Only re-init
            // if for some reason the near/far/fov changes.
            // calculate the center of the sphere    
            // note in TV3d the lookAt is actually the point in world coordinates of where we are looking
            // to get the real direction vector, substract it from the camera position
            Vector3d dir = Vector3d.Normalize(lookAt - cameraPosition);
            dir *= _radius;
            _center = cameraPosition + dir;
        }

        public double Radius
        {
            get { return _radius; }
        }

        public Vector3d Center
        {
        	get { return _center; } set {_center = value;} 
        }
        
        public void Scale (double scale)
        {
        	_radius *= scale;
        }

        public BoundingSphere Transform(Matrix matrix)
        {
           return Transform(this, matrix);
        }

        public static BoundingSphere Transform(BoundingSphere sphere, Matrix matrix)
        {
            Vector3d pointOnSurface; 
            pointOnSurface.x = sphere._radius;
            pointOnSurface.y = 0;
            pointOnSurface.z = 0;
            pointOnSurface += sphere._center; 

            Vector3d center = Vector3d.TransformCoord(sphere._center, matrix);
            pointOnSurface = Vector3d.TransformCoord (pointOnSurface, matrix);

            double radius = (pointOnSurface - center).Length;

            return new BoundingSphere(center, radius);
        }


        // if the distance between the sphere center is less than radius it contains the point
        public bool Contains(Vector3d point)
        {
            Vector3d v = _center - point;
            double distance = v.LengthSquared();
            return distance < _radius;
        }

        /// <summary>
        /// Returns whether the targetSphere is fully contained by this sphere instance.
        /// </summary>
        /// <param name="targetSphere"></param>
        /// <returns>True if this sphere instance fully contains the target sphere.  False otherwise.</returns>
        public bool Contains(BoundingSphere targetSphere)
        {
            Vector3d v = _center - targetSphere.Center;
            double distance = v.LengthSquared();
            // similar to intersect only instead of sumRadiiSquared, its just the radius squared of the source sphere
            // for small meshes being tested against a frustum sphere this results in much less "intersect" false positives
            // however for large meshes, this will ignore the ones that dont fully fit within the source (e.g. frustum) sphere.
            return distance < _radius * _radius && _radius > targetSphere.Radius;
        }

        
        // ----------------------------------------------------------------------
        // Name  : CheckPointInTriangle()
        // Input : point - point we wish to check for inclusion
        //         sO - Origin of sphere
        //         sR - radius of sphere 
        // Notes : 
        // Return: TRUE if point is in sphere, FALSE if not.
        // -----------------------------------------------------------------------  
        //private bool CheckPointInSphere(Vector3d point, Vector3d sO, double sR)
        //{
        //    double d = (point - sO).Length;

        //    if (d <= sR) return true;
        //    return false;
        //}

		
        /// <summary>
        /// Sphere 2 Sphere intersection.
        /// </summary>
        /// <param name="targetSphere"></param>
        /// <returns></returns>
        public IntersectResult Intersects(BoundingSphere targetSphere)
        {
            Vector3d v = _center - targetSphere.Center;
            double distance = v.LengthSquared();

            // if the distance between the centers is less than the radius of this instance
            // _and_ the radius of this instance is larger than the target, this instance fully 
            // contains the target
            if (distance < _radius * _radius && _radius > targetSphere.Radius)
                return IntersectResult.INSIDE;

            // if the distance between the centers is less than the sum of 
            // the radii then the two spheres intersect
            double RadiiSum = _radius + targetSphere.Radius;
            double RadiiSumSquared = RadiiSum * RadiiSum;
            if (distance < RadiiSumSquared)
                return IntersectResult.INTERSECT;

            return IntersectResult.OUTSIDE;

        }

		/*
        /// <summary>
        /// Ray 2 Sphere intersection.
        /// </summary>
        /// <param name="ray">ray</param>
        /// <param name="i1">first intersection distance</param>
        /// <param name="i2">second intersection distance</param>
        /// <returns>true if intersection is found, false otherwise.</returns>
        public bool Intersects (Ray  ray,  ref double  i1, ref double i2)
        {
            Vector3d p = ray.Origin - _center;
            double b = -Vector3d.DotProduct(p, ray.Direction);
            double c = Vector3d.DotProduct(p, p) + _radius * _radius;
            double det = b * b - c;
	        
            if (det < 0) return false;
        	
	        det = System.Math.Sqrt(det);
	        
	        // because this is polynomial, 2 possible solutions -> +/-
	        i1 = b - det;
	        i2 = b + det;
	        // intersecting with ray?
	        
	        // if i2 is less than 0, the collision occurred in the ray's negative direction?
	        if (i2 < 0) return false;
	        
	        // if i1 is less than 0, the collission occurred at i2?
	        if(i1 < 0) i1 = 0;
	        return true;
        }

        // TODO: make sure above version works before deleting this... and verify number of operations is optimal
        // ----------------------------------------------------------------------
        // Name  : intersectRaySphere()
        // Input : rO - origin of ray in world space
        //         rV - vector describing direction of ray in world space
        //         sO - Origin of sphere 
        //         sR - radius of sphere
        // Notes : Normalized directional vectors expected
        // Return: distance to sphere in world units, -1 if no intersection.
        // -----------------------------------------------------------------------  
        //private double intersectRaySphere(Vector3d rO, Vector3d rV, Vector3d sO, double sR)
        //{
        //    Vector3d Q = sO - rO;

        //    double c = Q.Length;
        //    double v = Vector3d.DotProduct(Q, rV);
        //    double d = sR * sR - (c * c - v * v);

        //    // If there was no intersection, return -1
        //    if (d < 0.0) return (-1.0f);

        //    // Return the distance to the [first] intersecting point
        //    return (double)(v - Math.Sqrt(d));
        //}
        
        // TODO: followinig is from
        // http://wiki.cgsociety.org/index.php/Ray_Sphere_Intersection
        // and may be less efficient but may have better precision when ray origin is far
        public bool Intersects(Ray ray, ref double t)
        {
        	// NOTE: This assumes that the sphere is at origin and that the ray is in modelspace, but if
        	// not then we have to move the ray to modelspace.  Obviously both ray and sphere must be in same space.
        	ray = new Ray ( ray.Origin - _center, ray.Direction);
        	
            //Compute A, B and C coefficients
            double a = Vector3d.DotProduct(ray.Direction, ray.Direction);
            double b = 2 * Vector3d.DotProduct(ray.Direction, ray.Origin);
            double c = Vector3d.DotProduct(ray.Origin, ray.Origin) - (_radius * _radius);

            //Find discriminant
            double disc = b * b - 4 * a * c;
            
            // if discriminant is negative there are no real roots, so return 
            // false as ray misses sphere
            if (disc < 0)
                return false;

            // compute q as described above
            double distSqrt = System.Math.Sqrt(disc);
            double q;
            if (b < 0)
                q = (-b - distSqrt) / 2.0;
            else
                q = (-b + distSqrt) /2.0;

            // compute t0 and t1
            double t0 = q / a;
            double t1 = c / q;

            // make sure t0 is smaller than t1
            if (t0 > t1)
            {
                // if t0 is bigger than t1 swap them around
                double temp = t0;
                t0 = t1;
                t1 = temp;
            }

            // if t1 is less than zero, the object is in the ray's negative direction
            // and consequently the ray misses the sphere
            if (t1 < 0)
                return false;

            // if t0 is less than zero, the intersection point is at t1
            if (t0 < 0)
            {
                t = t1;
                return true;
            }
            // else the intersection point is at t0
            else
            {
                t = t0;
                return true;
            }
        }
		*/
		
        public struct SweepResult
        {
            public bool Intersection; // true if there is some intersection(including an initial intersection)
            public float? T;
            public int? FaceIndex;
            public Vector3d? Point;
            public Vector3d? Normal;
            public float? PenetrationDepth;
        }


        //// TODO: need to add sweep tests to BoundingBox and Lines too
        //// http://therealdblack.wordpress.com/ 
        //// http://therealdblack.wordpress.com/category/sweep-tests/   - xna blog posts about various sweep tests
        //public static bool Sweep(BoundingSphere sweepSphere, BoundingSphere otherSphere, Vector3d direction, out SweepResult sweepResult)
        //{

        //    //like a sphere-point sweep with a sphere the size of the sum of the radii
        //    sweepResult = new SweepResult(null);

        //    BoundingSphere infSphere = new BoundingSphere(sweepSphere.Center, sweepSphere.Radius + otherSphere.Radius);

        //    SweepResult infSweepResult; //Inflated sphere result

        //    bool infResult = SweepSpherePoint(infSphere, otherSphere.Center, direction, out infSweepResult);

        //    sweepResult.T = infSweepResult.T;

        //    if (infSweepResult.T != null)
        //    {
        //        sweepResult.Point = infSweepResult.Point + infSweepResult.Normal * otherSphere.Radius;
        //        sweepResult.Normal = infSweepResult.Normal;
        //    }

        //    return sweepResult.Intersection = infResult;
        //}


        //public static bool SweepSpherePoint(BoundingSphere sweepSphere, Vector3d pt, Vector3d direction, out SweepResult sweepResult)
        //{
        //    //sweep point against sphere along -direction
        //    sweepResult = new SweepResult(null);

        //    if (direction.Length < DirectionEpsilon)
        //    {
        //        //zero direction, is the point initially touching the sphere?
        //        return sweepResult.Intersection = Intersects(sweepSphere, pt);
        //    }

        //    Vector3d P = pt - sweepSphere.Center;

        //    double PdotV = Vector3d.DotProduct(P, -direction);
        //    double PdotP = Vector3d.DotProduct(pt, pt);

        //    double a = Vector3d.DotProduct(direction, direction);
        //    double b = 2.0d * PdotV;
        //    double c = PdotP - sweepSphere.Radius * sweepSphere.Radius;

        //    double t0, t1;

        //    if (!Utilities.MathHelper.SolveQuadratic(a, b, c, out t0, out t1))
        //    {
        //        return sweepResult.Intersection = false;
        //    }

        //    Utilities.MathHelper.Sort(ref t0, ref t1);

        //    if ((t1 < 0.0f) || (t0 > 1.0f))
        //    {
        //        return sweepResult.Intersection = false;
        //    }

        //    if (t0 < 0.0f)
        //    {
        //        return sweepResult.Intersection = true;
        //    }

        //    Vector3d sphereHitCen0 = sweepSphere.Center + direction * t0;

        //    sweepResult.T = t0;
        //    sweepResult.Point = pt;
        //    sweepResult.Normal = Vector3d.Normalize(sphereHitCen0 - pt);

        //    return sweepResult.Intersection = true;
        //}


    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // END PRIMITIVES


    public class ConvexHull // TODO: should this implement IPageableTVResource? and should we just save our hulls in .obj?
    {
        protected Vector3d[] _vertices;  // point cloud
        protected Triangle[] _triangles; // faces

        public ConvexHull(string filepath)
        {
            // just a data file of verts?
        }

		/*
        //Pre-processes the input point cloud by converting it to a unit-normal cube. 
        //Duplicate vertices are removed based on a normalized tolerance
        // level (i.e. 0.1 means collapse vertices within 1/10th the width/breadth/depth of any side. 
        //This is extremely useful in eliminating slivers. When cleaning up �duplicates and/or nearby neighbors� 
        //it also keeps the one which is �furthest away� from the centroid of the volume.
        public static ConvexHull GetStanHull(TVMesh m)
        {
           
            MHull hullLib = new MHull();
            int count;
            float[] verts;

            if (m == null) throw new ArgumentNullException();

            count = m.GetVertexCount();
            verts = new float[count * 3];
            float ny = 0, nx = 0, nz = 0, tu1 = 0, tu2 = 0, tv1 = 0, tv2 = 0;
            int color = 0;

            for (int i = 0; i < count; i++)
            {
                m.GetVertex(i, ref verts[i * 3], ref verts[i * 3 + 1], ref verts[i * 3 + 2], ref ny, ref nx, ref nz, ref tu1,
                            ref tv1,
                            ref tu2, ref tv2, ref color);
            }

            //4096, 8192, 
            System.Diagnostics.Stopwatch watch = new Stopwatch() ;
            watch.Reset() ;
            watch.Start();
            MHullResult res = hullLib.CreateConvexHull(true, false, false, count, verts, 12, 0.001f, 8192, 8192, 0.01F);
            watch.Stop();
            Trace.Assert(res.Triangles);
            Trace.Assert(res.GetIndices().Length / 3 == res.FaceCount,
                         "Import.GetStanHull() -- Invalid face count " + res.GetVertices().Length);
            Trace.WriteLine("Convex hull created in " + watch.Elapsed + "seconds, with = " + res.Count + " vertices in " + res.FaceCount + " triangles.");

            return new ConvexHull(res.GetVertices(), res.GetIndices());

        }

        /// <summary>
        /// Constructor assumes Mesh is already a convex hull as opposed to concave
        /// </summary>
        /// <param name="obj"></param>
        public ConvexHull(TVMesh obj)
        {
            int count = obj.GetTriangleCount();
            _triangles = new Triangle[count];

            int index1, index2, index3;
            int group = 0;
            index1 = index2 = index3 = 0;

            for (int i = 0; i < count; i++)
            {
                obj.GetTriangleInfo(i, ref index1, ref index2, ref index3, ref group);
                _triangles[i] = Helpers.TVTypeConverter.FromTVMeshIndexedFace(obj, index1, index2, index3);
            }
        }
		*/

        public ConvexHull(float[] Vertices, uint[] indices)
        {
            Trace.Assert(Vertices.Length%3 == 0);
            int length = Vertices.Length/3;

            Vector3d[] tmp = new Vector3d[Vertices.Length];
            for (int i = 0; i < length; i++)
            {
                tmp[i] = new Vector3d(Vertices[i*3], Vertices[i*3 + 1], Vertices[i*3 + 2]);
            }

            _vertices = new Vector3d[indices.Length];
            for (int i = 0; i < indices.Length; i ++)
            {
                _vertices[i] = tmp[indices[i]];
            }

            _triangles = new Triangle[indices.Length/3];
            for (int i = 0; i < indices.Length/3; i ++)
            {
                _triangles[i] = new Triangle(_vertices[i*3], _vertices[i*3 + 1], _vertices[i*3 + 2]);
            }
        }

        /// <summary>
        /// Called from CollisionTest.collideWithWorld() to create a new hull in world coords.  This should be cashed
        /// until the entity using it has changed. NOTE: Generally, we only need to test a hull after bounding volume so this
        /// elminates all but the potential hits.  This way the "per frame" update is minimal since 99% of
        /// everything will be eliminated by the bounding volume test.
        /// </summary>
        /// <param name="triangles"></param>
        /// <param name="matrix"></param>
        public ConvexHull (Triangle[] triangles, Matrix matrix)
        {
            // takes an existing set of triangles, transforms them to world coords or any other matrix
            _triangles = new Triangle[triangles.Length];

            for (int i = 0; i < triangles.Length; i++)
            {

                Vector3d p1, p2, p3;
                p1 = Vector3d.TransformCoord(triangles[i].Points[0], matrix);
                p2 = Vector3d.TransformCoord(triangles[i].Points[1], matrix);
                p3 = Vector3d.TransformCoord(triangles[i].Points[2], matrix);
                _triangles[i] = new Triangle(p1, p2, p3);
            }
        }

        // a convex hull created from a bounding box
        public ConvexHull(BoundingBox box)
        {
            _triangles = BoundingBox.GetTriangleFaces(box);
            _vertices = box.Vertices;
        }

        // a convex hull created from an array of triangles
        public ConvexHull(Triangle[] tris)
        {
        }

        // a convex hull created from an array of vertices 
        public ConvexHull(Vector3d[] verts)
        {
            // if (verts.Length % 3 > 0) throw new ArgumentOutOfRangeException();
            _vertices = verts;
        }

        public ConvexHull(Triangle[] tris, Vector3d referencePoint, double scale)
        {
            // get the triangles for the bounding box

            // determine which ones are NOT facing the camera and extrude
            // the verts... hrm, but we have to match them still with the regular verts of the box 

            // would be easier using hte quad faces.  Start by getting the quad faces, computing hte normals
            // and finding which ones are forward facing.  Then extrude the ones not facing and skip the ones
            // already extruded.

            // 
        }

        // TODO: Ideally we should use the Box and its vertex normals to first determine which are the "back"
        // vertices which need to be extruded.  You're supposed to use vertex normals, but since our vertices are
        // shared by 3 faces of the box, we'd actually need to use face normals which is normally wrong but probably ok
        // in this case since we can "always" only use a box type shadow volume.  
        // a convex hull created from an array of contour vertices and extruded from the reference position by scale
        public ConvexHull(Vector3d[] verts, Vector3d referencePoint, double scale)
        {
            List<Vector3d> extrudedVerts = new List<Vector3d>();

            for (int i = 0; i < verts.Length; i++)
            {
                //    // NOTE: we can either store just the camera position to have fewer corner tests against
                //    // the frustum, or we can add the src poitns on the contour as well as the extruded points.

                //    // extrude the vertex by the scale (for shadowVolume its usually the light falloff range)
                //    Vector3d ex, tmp;
                //    Vector3d dir = Core._CoreClient.Maths.VSubtract(verts[i], referencePoint);
                //    ex = Core._CoreClient.Maths.VNormalize(dir);
                //    tmp = Core._CoreClient.Maths.VScale(ex, scale);
                //    ex = Core._CoreClient.Maths.VAdd(verts[i], tmp);

                //    bool add = true;
                //    foreach (Vector3d v in extrudedVerts)
                //    {
                //        // TODO: i can probably check for the redundant vertex prior to computing the extruded version
                //        if (v.Equals(verts[i]))
                //        {
                //            add = false;
                //            break;
                //        }
                //    }
                //    if (add)
                //    {
                //        // here we add both the original and extruded 
                //        // TODO: what we should be doing is taking the bounding volume
                //        // and simply MOVING the verts that are facing away and extrude those.
                extrudedVerts.Add(verts[i]);
                //        extrudedVerts.Add(ex);
                //    } 
            }
            _vertices = extrudedVerts.ToArray();
        }

        public Triangle[] Triangles
        {
            get { return _triangles; }
        }

        public Vector3d[] Vertices
        {
            get { return _vertices; }
        }

#if DEBUG
        //// debug visual aids.
        //public void Draw(TV_3DMATRIX mat, CONST_TV_COLORKEY color)
        //{
        //    // TODO: temporarily commented out til i fix this massive overhaul
        //    Vector3d[] tmp = new Vector3d[_vertices.Length];
        //    for (int i = 0; i < _vertices.Length; i++)
        //    {
        //        tmp[i] = Vector3d.TransformCoord(_vertices[i], Helpers.TVTypeConverter.FromTVMatrix(mat));
        //    }
        //    DebugDraw.DrawHull(tmp, color);
        //    for (int i = 0; i < _vertices.Length; i += 3)
        //    {
        //        DebugDraw.DrawNormalVector(tmp[i], tmp[i + 1], tmp[i + 2], 3);
        //    }
        //}

        //public void Draw(CONST_TV_COLORKEY color)
        //{
        //    DebugDraw.DrawHull(_vertices, color);
        //    //for (int i = 0; i < _vertices.Length; i += 3)
        //    //{
        //    //    DebugDraw.DrawNormalVector(_vertices[i], _vertices[i + 1], _vertices[i + 2], 3);
        //    //}
        //}
#endif
    }

public abstract class PlanedFrustum
    {
        protected Plane[] _planes;
        protected bool _testAllPoints = false;
        protected bool[] _enabledPlanes;

        public Plane[] Planes
        {
            get { return _planes; }
        }

        public bool[] EnabledPlanes
        {
            get { return _enabledPlanes;}
            set {_enabledPlanes = value;}
        }

        public bool TestAllPoints
        {
            get { return _testAllPoints; }
            set { _testAllPoints = value; }
        }


        //public void Translate(Vector3d translation)
        //{
        //    if (translation.IsNullOrEmpty()) return;
        //    foreach (Plane p in _planes)
        //    {
        //        p.Translate(translation);
        //    }
        //}

        // TODO: for OBB see this URL
        // http://www.gamedev.net/community/forums/topic.asp?topic_id=539116
        // frustum planes tested against mesh box
        public IntersectResult Intersects(BoundingBox box)
        {
            return Intersects(box.Vertices);
        }

        // TODO: I had pasted the following because it seemed advocated, but its far less flexible
        // then my current Intersects (Vector3d[] vertices) 
        //public bool IsBBoxVisible(BoundingBox b)
        //{

        //    if ((Math.Abs(b.Mid.x - Pos.x) < b.Size.x) &&
        //        (Math.Abs(b.Mid.y - Pos.y) < b.Size.y) &&
        //        (Math.Abs(b.Mid.z - Pos.z) < b.Size.z))
        //        return true;

        //    for (int i = 0; i < 6; i++)
        //    {
        //        float m = b.Mid.x * planes[i].x + b.Mid.y * planes[i].y + b.Mid.z * planes[i].z + planes[i].w;
        //        float n = b.Size.x * absPlanes[i].x + b.Size.y * absPlanes[i].y + b.Size.z * absPlanes[i].z;
        //        if (m > n) return false;
        //    }

        //    return true;
        //}

        // if all corners of the bounding box are inside every plane of the frustum, this item is FULLY visible
        // NOTE: When this method is called by the OcclusionFrustum.IsVisible rather than VIewFrustum.IsVisible
        // it treats true returnd from here as being NOT visible since its fully within the bounds of the frustum.
        // (i.e. opposite of how ViewFrustum interprets true)
        public IntersectResult Intersects(Vector3d[] vertices)
        {
           
            int totalIn = 0;
            for (int j = 0; j < _planes.Length; j++)
            {
                if (!_enabledPlanes[j]) // if the plane is NOT enabled, then the vertex is assumed inside
                {
                    totalIn ++;
                    continue;
                }

                int cornersIn = 0;
                for (int i = 0; i < vertices.Length; i++)
                {
                    double distance = Plane.DistanceToPlane(vertices[i], _planes[j]);

                    if (distance >= 0.0)
                    {
                        cornersIn++;
                    }
                }
                
                // if all corners are behind any single plane, this item is fully NOT visible.
                if (cornersIn == vertices.Length)
                    totalIn++;
                else if (cornersIn == 0)
                    return IntersectResult.OUTSIDE;
            }
            return totalIn == _planes.Length ? IntersectResult.INSIDE : IntersectResult.INTERSECT;
        }

        // frustum planes tested against mesh sphere. NOTE: I think my 
        // frustum planes normals are reversed (they are pointing outwards) and so
        // we're testing > instead of < as seen in so mahy online samples
        public IntersectResult Intersects(BoundingSphere sphere)
        {
            for (int i = 0; i < _planes.Length; i++)
            {
                if (!_enabledPlanes[i]) 
                {
                    continue;
                }

                double distance = Plane.DistanceToPlane(sphere.Center, _planes[i]);

                // if the distance from the center of the sphere to any single plane
                // is < -sphere.radius we are definetly outside
                if (distance < -sphere.Radius)
                    return IntersectResult.OUTSIDE; // Sphere is completely outside and thus not visible

                // if the distance from the center of the sphere to the plane is within +-radius the sphere intersects
                // which means this mesh POTENTIALLY is visible since bounding sphere isnt the best fit around the mesh
                // but we'll assume its good enough and Return early
                if (Math.Abs(distance) < sphere.Radius)
                    return IntersectResult.INTERSECT;
            }
            return IntersectResult.INSIDE;
        }

#if DEBUG
        public abstract void Draw();
#endif
        // TODO: why IsVisible vs Intersects overloads?
        // or else why arent Intersects private members?
       // public abstract bool IsVisible(Geometry mesh);

        public abstract bool IsVisible(BoundingBox box);

        public abstract bool IsVisible(BoundingSphere sphere);
    }

	public class OcclusionFrustum : PlanedFrustum
    {
        private Vector3d _lastCameraPos;
        public ConvexHull Hull;
        public List<Line3d> _edges;
        private List<Triangle> _facingTris;

        private bool _isDirty;
                     // NOTE: This is just for debugging in order to create a occlusion frustum and render it in one spot

        // without it updating every frame.  This way we can render it once and then walk around it to confirm its ok.  
        // otherwise normally, we must update the frustum everytime the camera moves and so its always "dirty"
        // As far as actually moving the occluders themselves, we rebuild the entire object instance via constructor below.
        // in other words, dont move occluders because rebuilding them is slower than updating them.

        public OcclusionFrustum(ConvexHull hull)
        {

            // TODO: actually im using incorrect vars here
            // First, for an occluder there's only a "planes" hull type.
            // For VIewFrustum there is "sphere, cone and planes.
            // For either occluder or viewfrustum
            // - sphereFrustum can test mesh Sphere or mesh AABB
            // - coneFrustum can only test mesh Spheres
            // - planes can test mesh Spheres or mesh AABB

            // further, you can run Sphere, Cone and Planes in a cascading way.
            // for instance, first test the mesh against Sphere.  If its INTERSECT
            // then go on to test Cone or Planes.  

            if (hull == null) throw new ArgumentNullException();
            Hull = hull;

            _facingTris = new List<Triangle>();
            _edges = new List<Line3d>();
            _isDirty = true;
        }

        public bool IsDirty
        {
            get { return _isDirty; }
        }

       // public override bool IsVisible(Geometry mesh)
       // {
       //     bool result = IsVisible(mesh.BoundingBox);
		//
        //    return result;
       // }


        public bool IsVisible(Vector3d[] vertices)
        {
            IntersectResult result = Intersects((vertices));

            //return (result == IntersectResult.INSIDE || result == IntersectResult.INTERSECT );
            return result != IntersectResult.OUTSIDE;
        }

        public override bool IsVisible(BoundingBox box)
        {
            // NOTE: for occluders, we return the opposite of the normal visiblity test
            return (Intersects(box) != IntersectResult.INSIDE);
        }

        public override bool IsVisible(BoundingSphere sphere)
        {
            // NOTE: for occluders, we return the opposite of the normal visiblity test
            return (Intersects(sphere) != IntersectResult.INSIDE);
        }

        public void Update(Vector3d camPos, bool contourPlanesOnly, bool forceUpdate)
        {
            if (!_lastCameraPos.Equals(camPos) || forceUpdate)
            {
                _edges.Clear();
                _facingTris.Clear();
                _lastCameraPos = camPos;

                // This implementation based on the psuedo code from the following article.
                //http://www.gamasutra.com/features/20020717/bacik_03.htm
                //To build contours from a convex hull, we use a simple algorithm utilizing the fact
                // that each edge in a convex hull connects exactly two faces. The algorithm is this:

                // 1. Iterate through all polygons, and detect whether a polygon faces the viewer. 
                // (To detect whether a polygon faces the viewer, use the dot product of the polygon�s
                // normal and direction to any of the polygon�s vertices. When this is less than 0, 
                // the polygon faces the viewer.)

                for (int i = 0; i < Hull.Triangles.Length; i++)
                {
                    Vector3d dir = Hull.Triangles[i].Points[0] - camPos;
                    // TODO: hope this is correct.  Down the line if there's any problems switch from using
                    // triangle face normal to a vertex normal of any point on the triangle
                    if (Vector3d.DotProduct(dir, Hull.Triangles[i].Normal) < 0)
                    {
                        _facingTris.Add(Hull.Triangles[i]);
                    }
                }

                // 2. If the polygon faces viewer, do the following for all its edges: If the edge is already 
                // in the edge list, remove the edge from the list since this means its an interior edge.
                // Otherwise, add the edge into the list.

                foreach (Triangle tri in _facingTris)
                {
                    Line3d e = new Line3d(tri.Points[0], tri.Points[1]);
                    UpdateEdges(e);
                    e = new Line3d(tri.Points[1], tri.Points[2]);
                    UpdateEdges(e);
                    e = new Line3d(tri.Points[2], tri.Points[0]);
                    UpdateEdges(e);
                }

                // After this, we should have collected all the edges forming the occluder�s contour, as seen
                // from the viewer�s position. Once you�ve got it, it�s time to build the occlusion frustum itself,
                // as shown in Figure 7 (note that this figure shows a 2D view of the situation). The frustum is a 
                // set of planes defining a volume being occluded. The property of this occlusion volume is that any 
                // point lying behind all planes of this volume is inside of the volume, and thus is occluded. 
                // So in order to define an occlusion volume, we just need a set of planes forming the occlusion volume.

                //Looking closer, we can see that the frustum is made of all of the occluder�s polygons facing the
                // viewer, and from new planes made of edges and the viewer�s position. So we will do the following:

                //1. Add planes of all facing polygons of the occluder.
                int count;
                if (contourPlanesOnly)
                    count = _edges.Count;
                else count = _facingTris.Count + _edges.Count;

                _planes = new Plane[count];
                if (!contourPlanesOnly)
                {
                    for (int i = 0; i < _facingTris.Count; i++)
                    {
                        // TODO: As an optimization, should combine the planes of facing triangles that lay within the same plane themselves.
                        // (see paragraph comments below)
                        _planes[i] = new Plane(_facingTris[i]);
                        // we flip the normal so that the planes face inward 
                        // this way it matches the ViewFrustum planes creation so they can all
                        // share the same Intersect methods.
                        _planes[i].Negate();
                    }
                }

                //2. Construct planes from the two points of each edge and the view-er�s position.
                for (int i = 0; i < _edges.Count; i++)
                {
                    int j;
                    if (!contourPlanesOnly)
                        j = _facingTris.Count + i;
                    else j = i;

                    _planes[j] = new Plane(_edges[i].Point[0], _edges[i].Point[1], camPos);
                    _planes[j].Negate();
                }

                //If you�ve gotten this far and it�s all working for you, there�s one useful optimization to implement 
                // at this point. It lies in minimizing the number of facing planes (which will speed up intersection 
                // detection). You may achieve this by collapsing all the facing planes into a single plane, with a 
                // normal made of the weighted sum of all the facing planes. Each participating normal is weighted by 
                // the area of its polygon. Finally, the length of the computed normal is made unit-length. The d part 
                // of this plane is computed using the farthest contour point. Occlusion testing will work well without 
                // this optimization, but implementing it will speed up further computations without loss of accuracy. 

                // TODO: another optimization during the cullling process is to skip occlusion tests for volumes that 
                // do not take up alot of room?  (though for things like castles that are far away, would we want to use
                // occlusion for thigns inside the walls or LOD at that point and just not render things inside because they
                // are too far away?
            }
            _isDirty = false;
        }

        public Vector3d[] EdgeVertices()
        {
            Vector3d[] verts = new Vector3d[_edges.Count*2];
            int k = 0;
            for (int i = 0; i < _edges.Count; i++)
            {
                verts[k] = _edges[i].Point[0];
                verts[k + 1] = _edges[i].Point[1];
                k += 2;
            }
            return verts;
        }

        private void UpdateEdges(Line3d e)
        {
            foreach (Line3d edge in _edges)
            {
                if (edge == e)
                {
                    _edges.Remove(edge);
                    return;
                }
            }
            _edges.Add(e);
        }

		
#if DEBUG
        // debug visual aids.
        public override void Draw()
        {
            /*
			const double NORMAL_LENGTH = 10;
            if ((Planes == null) || (Planes.Length == 0)) return;
            foreach (Plane p in Planes)
            {
                // draw  lines connecting all the vertices.  
                CoreClient._CoreClient.Screen2D.Draw_Line3D((float)p.Points[0].x, (float)p.Points[0].y, (float)p.Points[0].z,
                                                (float) p.Points[1].x,
                                                (float) p.Points[1].y, (float) p.Points[1].z);
                CoreClient._CoreClient.Screen2D.Draw_Line3D((float)p.Points[1].x, (float)p.Points[1].y, (float)p.Points[1].z,
                                                (float) p.Points[2].x,
                                                (float) p.Points[2].y, (float) p.Points[2].z);
                CoreClient._CoreClient.Screen2D.Draw_Line3D((float)p.Points[2].x, (float)p.Points[2].y, (float)p.Points[2].z,
                                                (float) p.Points[0].x,
                                                (float) p.Points[0].y, (float) p.Points[0].z);

            //    DebugDraw.DrawLine(Triangle.getCenter(p.Points[0], p.Points[1], p.Points[2]), p.Normal, NORMAL_LENGTH);
            }
			*/
        }
#endif
    }
	
	
	// This wrapper has one constructor that allows us to
    // retain the points used to create the plane from the 2nd and 3rd constructors.
    // NOTE: yeah its not that great because _points is null for all other cases...
    // in the future it might be possible to initialize 3 other points on the planes for all cases....
    // but for me right now, that's not useful so i wont bother...
    public class Plane // faster as a class than a struct
    {
        private Vector3d[] _points;
        private Vector3d _normal;
        private double _distance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <param name="normal">Expects a unit normal</param>
        public Plane(Vector3d point, Vector3d normal)
        {
            _points = null;
            _normal = normal; // normal must be unit normal
            _distance = point.Length;
            //_distance = Vector3d.DotProduct(normal, point);
            //if (_distance == 0.0)
            //    System.Diagnostics.Debug.WriteLine("problem here");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="normal">Expects a unit normal</param>
        /// <param name="distance">distance to the origin</param>
        public Plane(Vector3d normal, double distance) : this(normal.x, normal.y, normal.z, distance)
        {
        }

        /// <summary>
        /// Excepts a unit normal's components and distance from the origin.
        /// </summary>
        /// <param name="normalX"></param>
        /// <param name="normalY"></param>
        /// <param name="normalZ"></param>
        /// <param name="distance">distance to the origin</param>
        public Plane(double normalX, double normalY, double normalZ, double distance)
        {
            _distance = distance;
            _normal.x = normalX;
            _normal.y = normalY;
            _normal.z = normalZ;
            _points = null;
            
        }

        // our retained points version of the constructor
        public Plane(Vector3d p1, Vector3d p2, Vector3d p3)
        {
            _points = new Vector3d[3];
            _points[0] = p1;
            _points[1] = p2;
            _points[2] = p3;

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // TODO: is the p1 - p2 and p1 - p3 correct order?  somehow i broke my culling and i have the
            // scaleculler visibility test forcing return true always
            // %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
            Vector3d edge1 = p2 - p1;
            Vector3d edge2 = p3 - p1 ;
            _normal = Vector3d.CrossProduct(edge1, edge2);
            // TODO: should we normalize the normal?
            _normal.Normalize(); 
            _distance = -Vector3d.DotProduct( _normal, p1);


            //// slim dx 
            //double x1 = p2.x - p1.x;
            //double y1 = p2.y - p1.y;
            //double z1 = p2.z - p1.z;
            //double x2 = p3.x - p1.x;
            //double y2 = p3.y - p1.y;
            //double z2 = p3.z - p1.z;
            //double yz = (y1 * z2) - (z1 * y2);
            //double xz = (z1 * x2) - (x1 * z2);
            //double xy = (x1 * y2) - (y1 * x2);
            //double invPyth = 1.0d / (Math.Sqrt((yz * yz) + (xz * xz) + (xy * xy)));
            //_normal.x = yz * invPyth;
            //_normal.y = xz * invPyth;
            //_normal.z = xy * invPyth;
            //_distance = -((_normal.x * p1.x) + (_normal.y * p1.y) + (_normal.z * p1.z)); 
        }

        public Plane(Triangle tri)
            : this(tri.Points[0], tri.Points[1], tri.Points[2])
        {
        }

        public Vector3d[] Points
        {
            get { return _points; }
        }

        public Vector3d Normal
        {
             get { return _normal; } 
        }

        public void Translate(Vector3d translation)
        {
            _distance -=  Vector3d.DotProduct(Normal, translation);
            if (_points != null)
                for (int i = 0; i < _points.Length; i++)
                    _points[i] -= translation;

        }

        /// <summary>
        /// Plane must already be normalized before transforming it.
        /// </summary>
        /// <param name="matrix"></param>
        public void Transform (Matrix matrix)
        {
            Matrix matrix1 = Matrix.Inverse(matrix);

            double single4 = this._normal.x;
            double single3 = this._normal.y;
            double single2 = this._normal.z;
            double single1 = this._distance;
         
            _normal.x = (((single4 * matrix1.M11) + (single3 * matrix1.M12)) + (single2 * matrix1.M13)) + (single1 * matrix1.M14);
            _normal.y = (((single4 * matrix1.M21) + (single3 * matrix1.M22)) + (single2 * matrix1.M23)) + (single1 * matrix1.M24);
            _normal.z = (((single4 * matrix1.M31) + (single3 * matrix1.M32)) + (single2 * matrix1.M33)) + (single1 * matrix1.M34);
            _distance = (((single4 * matrix1.M41) + (single3 * matrix1.M42)) + (single2 * matrix1.M43)) + (single1 * matrix1.M44);

        }

        public void Negate()
        {
            _normal = Vector3d.Negate(_normal);
            // note; distance is never negated.  Distance is an absolute value and the normal gives us the direction.
        }

        public double Distance //distance from the origin
        {
            get { return _distance; } 
        }

        public Vector3d Origin
        {
            // NOTE: _normal * -_distance seems correct. It works for gizmo and waypoint placer
            //       where we create plane from a normal and distance and it works for picking
            //       celledregion grid squares where that plane is created using 3 points.
            get { return _normal * -_distance; }
        }
        
        public double DistanceToCoordinate (Vector3d coord)
        {
            return DistanceToPlane(coord, this);
        }

        public static double DistanceToPlane(Vector3d coord, Plane plane)
        {
            return Vector3d.DotProduct(coord, plane.Normal) + plane.Distance;
        }

        public static Plane Normalize(Plane plane)
        {
            double mag;
            Vector3d normal = Vector3d.Normalize(plane.Normal, out mag);
            return new Plane(normal.x, normal.y, normal.z, plane.Distance / mag);
        }

        #region Broken and Obsolete.  New implementation far superior
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="r"></param>
        ///// <param name="p"></param>
        ///// <param name="distance"></param>
        ///// <returns>Oct.22.2010 verified the main (non Points[] version) is equivalent to the XNA counterpart.</returns>
        //public static bool Intersects(Ray r, Plane p, ref double distance)
        //{
        //    const double epsilon = double.Epsilon; // .0001d; // strictly speaking this can be 0 but trying to add margin for floating point precision issues
        //    double d = -p.Distance; // -Vector3d.DotProduct(p.Normal, p.Points[0]); 
        //    if (p.Points != null)
        //    {
        //        //TODO: i commented the below distance calc in favor of just p.Distance 
        //        // because of debugging ScalingManipulator dragging the scaling tabs.  Verify culling, occlusion creation, portals, etc all ok.
        //        // it may be as simple as changing the sign
        //        double d2 = -Vector3d.DotProduct(p.Normal, p.Points[0]);
        //        double dist = System.Math.Abs(d - d2);
        //        System.Diagnostics.Trace.Assert(dist < .01);
        //    }
        //    double denom = Vector3d.DotProduct(p.Normal, r.Direction);
        //    double numer = d - Vector3d.DotProduct(p.Normal, r.Origin);

        //    if (denom <= epsilon) // normal is orthogonal to vector, cant intersect
        //        return false;

        //    distance = -numer / denom;
        //    return true;

        //}
#endregion 

        /// <summary>
        /// Finds the line that defines an intersection of two planes 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="intersection"></param>
        /// <returns></returns>
        public bool Intersects(Plane plane, ref Line3d intersection)
        {
            throw new NotImplementedException();
            return false;
        }

        public bool Intersects(Ray r, double rayScale, ref double distance)
        {
            Vector3d intersectionPoint;
            intersectionPoint.x = 0;
            intersectionPoint.y = 0;
            intersectionPoint.z = 0;
            return Intersects(r, this, rayScale, ref distance, ref intersectionPoint);
        }

        public bool Intersects(Ray r, double rayScale, ref double distance, ref Vector3d intersectionPoint)
        {
            return Intersects(r, this, rayScale, ref distance, ref intersectionPoint);
        }
        
        /// <summary>
        /// Finds intersection between ray and plane if it exists.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="p"></param>
        /// <param name="distance"></param>
        /// <param name="intersectionPoint"></param>
        /// <remarks>
        /// Copyright 2001, softSurfer (www.softsurfer.com)
        /// This code may be freely used and modified for any purpose
        /// providing that this copyright notice is included with it.
        /// SoftSurfer makes no warranty for this code, and cannot be held
        /// liable for any real or imagined damage resulting from its use.
        /// Users of this code must verify correctness for their application.
        /// http://www.softsurfer.com/Archive/algorithm_0104/algorithm_0104B.htm
        /// http://code.google.com/p/slimmath/source/browse/trunk/SlimMath/Collision.cs
        /// </remarks>
        /// <returns></returns>
        public static bool Intersects(Ray r, Plane p, double rayScale, ref double distance, ref Vector3d intersectionPoint)
        {
            bool result = false;

            Vector3d rayDestination = r.Origin + (r.Direction * rayScale);
            Vector3d u = rayDestination - r.Origin;
            Vector3d w = r.Origin - p.Origin; // TODO: make sure this still works.
                                              // used to be p.Points[0] but now i compute p.Origin
                                              // and im not 100% sure that the p.Origin is caled properly
                                              // or whether p.Points[0] can be replaced with origin point (but i dont see why not)

            double D = Vector3d.DotProduct(p.Normal, u);
            double N = -Vector3d.DotProduct(p.Normal, w);

            // segment is parallel to plane 
            if (System.Math.Abs(D) < double.Epsilon) 
            {
                if (N == 0)                     // segment lies in plane
                    result = true;              
                else
                    result = false;                   // no intersection
            }
            // they are not parallel compute intersect param
            else  
            {
                const double zeroEpsilon = double.Epsilon;
                
                double sI = N / D;
                if (sI < 0 || sI > 1) // on wrong side of face no intersection
                    result = false;                       
                else
                {
                    // compute segment intersect point
                    Vector3d scaledDirection = sI * u;
                    intersectionPoint = r.Origin + scaledDirection; 
                    distance = scaledDirection.Length;
                    result = true;
                }
            }
            return result;
        }

        public static Vector3d IntersectionPoint(Ray r, double distance)
        {
            return r.Origin + (r.Direction*distance);
        }

        /// <summary>
        /// Utility function that returns the origin plane whose normal is perpendicular to the 
        /// vectors of the specified axes if multiple axes are specified, or the plane
        /// whose normal is the unit vector of the specified axis if a single axis is specified
        /// </summary>
        /// <param name="axis">The axes for which to retrieve the corresponding plane</param>
        /// <returns>The origin plane that corresponds to the specified axes</returns>
        public static Plane GetPlane(Vector3d origin, AxisFlags axis)
        {
            Vector3d normal;
            normal.x = 0;
            normal.y = 0;
            normal.z = 0;

            switch (axis)
            {
                case AxisFlags.X:
                case AxisFlags.Y | AxisFlags.Z:
                    normal.x = 1;
                    break;

                case AxisFlags.Y:
                case AxisFlags.X | AxisFlags.Z:
                    normal.y = 1;
                    break;

                case AxisFlags.Z:
                case AxisFlags.X | AxisFlags.Y:
                    normal.z = 1;
                    break;
            }

            return new Plane(origin, normal);
        }

        public static Plane GetPlane(AxisFlags axis)
        {
            return GetPlane(Vector3d.Zero(), axis);
        }
    }
	
	public class Triangle : Polygon
    {
        private const double LOCAL_EPSILON = 0.000001d; // triangle intersection fudge factor

        // tri_face structure // TODO: needs to be a more universal u,v distance structure for not just triangles
        public struct TRI_FACE
        {
            public double U;
            public double V;
            public double Distance;
        }

        public Triangle(Vector3d p1, Vector3d p2, Vector3d p3) : base (new Vector3d[]{p1, p2,p3})
        {
        }

        public Vector3d Center
        {
            get { return getCenter(Points[0], Points[1], Points[2]); }
        }

        public static Vector3d getCenter(Vector3d v1, Vector3d v2, Vector3d v3)
        {
            return new Vector3d((v1.x + v2.x + v3.x)/3, (v1.y + v2.y + v3.y)/3, (v1.z + v2.z + v3.z)/3);
        }

        
        /// <summary>
        /// Tomas M�ller's RayTri collision test.
        /// usage - itterate through triangles passing the verts
        /// and depending on whether we want first contact exit or to build an entire list of hits
        /// we continue itterating.
        /// we can also cache the previous frame's results and if our test parameters are the same
        /// we can try to test those first, else we start back at beginning.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="vert0"></param>
        /// <param name="vert1"></param>
        /// <param name="vert2"></param>
        /// <param name="backfaceCulling">skip backface triangles</param>
        /// <param name="hitResult"></param>
        /// <returns></returns>
        public static bool Intersects(Ray r, Vector3d vert0, Vector3d vert1, Vector3d vert2, bool backfaceCulling, ref TRI_FACE hitResult )
        {
            // Find vectors for two edges sharing vert0
            Vector3d edge1 = vert1 - vert0; // vert0 - vert1;
            Vector3d edge2 = vert2 - vert0; // vert0 - vert2;

            // Begin calculating determinant - also used to calculate U parameter
            Vector3d pvec = Vector3d.CrossProduct(r.Direction, edge2);

            // If determinant is near zero, ray lies in plane of triangle
            double det = Vector3d.DotProduct(edge1, pvec);
            double OneOverDet;

            if (backfaceCulling)  // only test frontward facing triangles
            {
                if (det < LOCAL_EPSILON)
                    return false;
                // From here, det is > 0. So we can use integer cmp.

                // Calculate distance from vert0 to ray origin
                Vector3d tvec =  r.Origin - vert0;

                // Calculate barycentric U parameter and test bounds
                hitResult.U = Vector3d.DotProduct(tvec, pvec);

                if ((hitResult.U < 0.0f) || (hitResult.U > det))
                    return false;

                // Prepare to test V parameter
                Vector3d qvec = Vector3d.CrossProduct(tvec, edge1);

                // Calculate barrycentric V parameter and test bounds
                hitResult.V = Vector3d.DotProduct(r.Direction, qvec);
                if ((hitResult.V < 0.0f) || (hitResult.U + hitResult.V > det))
                    return false;

                // Calculate t, scale parameters, ray intersects triangle
                hitResult.Distance = Vector3d.DotProduct(edge2, qvec);
                // Det > 0 so we can early exit here
                // Intersection point is valid if distance is positive (else it can just be a face behind the orig point)
                if (hitResult.Distance < 0.0f) return false;
                // here in Moeller's code he includes in the if (mStabbedFace.mU + mStabbedFace.mV > det) 
                // Else go on
                OneOverDet = 1.0f / det;
                hitResult.Distance *= OneOverDet;
                hitResult.U *= OneOverDet;
                hitResult.V *= OneOverDet;
            }
            else
            {
                // the non-culling branch
                if (det > -LOCAL_EPSILON && det < LOCAL_EPSILON)
                    return false;

                // Calculate distance from vert0 to ray origin
                Vector3d tvec = r.Origin - vert0;
                OneOverDet = 1.0f / det;
                // Calculate U parameter and test bounds
                hitResult.U = (Vector3d.DotProduct(tvec, pvec)) * OneOverDet;

                if ((hitResult.U < 0.0f) || (hitResult.U > 1.0f))
                    return false;

                // prepare to test V parameter
                Vector3d qvec = Vector3d.CrossProduct(tvec, edge1);

                // Calculate V parameter and test bounds
                hitResult.V = (Vector3d.DotProduct(r.Direction, qvec)) * OneOverDet;
                if ((hitResult.V < 0.0f) || (hitResult.U + hitResult.V > 1.0f))
                    return false;

                // Calculate t, ray intersects triangle
                hitResult.Distance = (Vector3d.DotProduct(edge2, qvec)) * OneOverDet;
                // Intersection point is valid if distance is positive (else it can just be a face behind the orig point)
                if (hitResult.Distance < 0.0f)
                    return false;
            }
            return true;
        }

        // ----------------------------------------------------------------------
        // Name  : CheckPointInTriangle()
        // Input : point - point we wish to check for inclusion
        //         a - first vertex in triangle
        //         b - second vertex in triangle 
        //         c - third vertex in triangle
        // Notes : Triangle should be defined in clockwise order a,b,c
        // Return: TRUE if point is in triangle, FALSE if not.
        // -----------------------------------------------------------------------  
        public static bool CheckPointInTriangle(Vector3d point, Vector3d a, Vector3d b, Vector3d c)
        {
            double total_angles = 0.0f;
            double epsilon = 0.005;
            // make the 3 vectors
            Vector3d v1 = point - a;
            Vector3d v2 = point - b;
            Vector3d v3 = point - c;

            v1 = Vector3d.Normalize(v1);
            v2 = Vector3d.Normalize(v2);
            v3 = Vector3d.Normalize(v3);

            total_angles += Math.Acos(Vector3d.DotProduct(v1, v2));
            total_angles += Math.Acos(Vector3d.DotProduct(v2, v3));
            total_angles += Math.Acos(Vector3d.DotProduct(v3, v1));

            if (Math.Abs(total_angles - 2 * Math.PI) <= epsilon)
                return (true);

            return (false);
        }

        // ----------------------------------------------------------------------
        // Name  : closestPointOnTriangle()
        // Input : a - first vertex in triangle
        //         b - second vertex in triangle 
        //         c - third vertex in triangle
        //         p - point we wish to find closest point on triangle from 
        // Notes : 
        // Return: closest point on line triangle edge
        // -----------------------------------------------------------------------  
        public static Vector3d ClosestPointOnTriangle(Vector3d a, Vector3d b, Vector3d c, Vector3d p)
        {
            Vector3d Rab = Line3d.ClosestPointOnLine(a, b, p);
            Vector3d Rbc = Line3d.ClosestPointOnLine(b, c, p);
            Vector3d Rca = Line3d.ClosestPointOnLine(c, a, p);

            double dAB = (p - Rab).Length;
            double dBC = (p - Rbc).Length;
            double dCA = (p - Rca).Length;


            double min = dAB;
            Vector3d result = Rab;

            if (dBC < min)
            {
                min = dBC;
                result = Rbc;
            }

            if (dCA < min)
                result = Rca;

            return (result);
        }
    }
	
	public class Polygon
    {
        protected int[] _indices;
        protected Vector3d[] _points;
        protected Vector3d _normal;

        public Polygon(Vector3d[] points)
        {
            if (points.Length < 3) throw new ArgumentException();

            _points = points;

            _normal = FaceNormal(_points[0], _points[1], _points[2]);

            // bad triangles where any two points are the same could result in exception?
            if (_points[0].Equals(_points[1]) || _points[0].Equals(_points[2]) || _points[1].Equals(_points[2]))
            {
                //throw new ArgumentException("Triangle cannot have two identicle vertices.");
            }
            _indices = null;
        }

		public Polygon(Vector3d a, Vector3d b, Vector3d c, Vector3d d)
        {
            _points = new Vector3d[4];
            _points[0] = a;
            _points[1] = b;
            _points[2] = c;
            _points[3] = d;
            
            _normal = FaceNormal(_points[0], _points[1], _points[2]);

            // bad triangles where any two points are the same could result in exception?
            if (_points[0].Equals(_points[1]) || _points[0].Equals(_points[2]) || _points[1].Equals(_points[2]))
            {
                //throw new ArgumentException("Triangle cannot have two identicle vertices.");
            }
            _indices = null;
        }
        
        public Vector3d Normal
        {
            get { return _normal; }
        }

        public int[] Indices
        {
            get { return _indices; }
        }

        public Vector3d[] Points
        {
            get { return _points; }
        }

        public Plane GetPlane()
        {
            return new Plane(_points[0], _points[1], _points[2]);
        }

        public static Vector3d FaceNormal(Vector3d v1, Vector3d v2, Vector3d v3)
        {
            //1. The two edges chosen must not be parallel, i.e. the angle between the edges must not be 0 or 180 degrees. 
            //   The normal will be more accurate if the angle between the lines is closer to 90 degrees. 
            //2. The length of the edges must be non-zero and the normal will be more accurate if the length is high compared 
            //   with the accuracy of the coordinates. 
            //3. If the angle is concave then the direction of the normal needs to be reversed.

            Vector3d a, b;

            a = v1 - v2;
            b = v2 - v3;
            return Vector3d.Normalize(Vector3d.CrossProduct(a, b));
        }

        public Polygon Transform(Matrix transform)
        {
            return Transform(this, transform);
        }

        public static Polygon Transform(Polygon p, Matrix transform)
        {
            Vector3d[] points = Vector3d.TransformCoordArray(p.Points, transform);

            Polygon result = new Polygon(points);
            return result;

        }

        public bool Intersects (Vector3d start, Vector3d end, bool skipBackFaces, out Vector3d intersectionPoint)
        {
        	return Polygon.Intersects (this._points, start, end, skipBackFaces , out intersectionPoint);
        }
        
        public bool Intersects(Ray r, double rayScale, bool skipBackFaces, out Vector3d intersectionPoint)
        {
        	return Polygon.Intersects (r, rayScale, this._points, skipBackFaces, out intersectionPoint);
        }
        
        public static bool Intersects(Polygon poly, Vector3d start, Vector3d end, bool skipBackFaces, out Vector3d intersectionPoint)
        {
            return Polygon.Intersects(poly.Points, start, end, skipBackFaces, out intersectionPoint);
        }

        public static bool Intersects(Vector3d[] points, Vector3d start, Vector3d end, bool skipBackFaces, out Vector3d intersectionPoint)
        {
            Vector3d dir = Vector3d.Normalize(end - start);
            Ray r = new Ray(start, dir);
            return Intersects(r, dir.Length, points, skipBackFaces, out intersectionPoint);
        }



        public static bool Intersects(Ray r, double rayScale, Vector3d[] points, bool skipBackFaces, out Vector3d intersectionPoint)
        {
            if (points.Length < 3) throw new ArgumentException();

            double distance = 0;
            Vector3d intersectPoint = Vector3d.Zero();
            intersectionPoint = intersectPoint;

            //if (skipBackFaces)
            //{
            //    // Find vectors for two edges sharing vert0
            //    Vector3d edge1 = points[1] - points[0];
            //    Vector3d edge2 = points[points.Length - 1] - points[0];

            //    // Begin calculating determinant - also used to calculate U parameter
            //    Vector3d pvec = Vector3d.CrossProduct(r.direction, edge2);

            //    // If determinant is near zero, ray lies in plane of triangle
            //    double det = Vector3d.DotProduct(edge1, pvec);
            //    double OneOverDet = 1.0f / det;
            //    // Calculate distance from vert0 to ray origin
            //    Vector3d tvec = r.origin - points[0];

            //    // Calculate U parameter and test bounds
            //    double U = Vector3d.DotProduct(tvec, pvec);

            //    if ((U < 0.0f) || (U > det))
            //        return false;
            //}

            Plane p = new Plane(points[0], points[1], points[points.Length - 1]);
            //if (r.Origin == points[0] || r.Origin == points[1] || r.Origin == points[points.Length - 1])
            //{
            //    // TODO: I believe this code is ok.  I added it July.1.2011 to catch case where
            //    // if any of the points that define the plane is the same as the ray  origin
            //    // then we should short circuit and return true with the intersection point equal to
            //    // the ray origin and obviously distance == 0
            //    // TODO: actually this doesnt work at all.  I itterate through all cells so this 
            //    // gets evaluated and returns true every time when it hits the first cell!
            //    intersectPoint = r.Origin;
            //    return true;
            //}
            //p = new Plane(points[0], points[1], points[2]);
            //if (skipBackFaces)
            //{
            //    if( Plane.DistanceToPlane(r.Origin, p) < 0)
            //        return false;
            //}

            bool result = (p.Intersects(r, rayScale, ref distance, ref intersectPoint));
            intersectionPoint = intersectPoint;
            if (!result) return false;

            // now that we have an intersection point, find if that point is in the set of points that make up the poly
            return ContainsPoint(intersectPoint, points);
        }

        public bool ContainsPoint(Vector3d rayPlaneIntersectionPoint)
        {
            return ContainsPoint(rayPlaneIntersectionPoint, _points);
        }

        public bool ContainsPoints(Vector3d[] points)
        {
            if (points == null || points.Length == 0) return false;

            for (int i = 0; i < points.Length; i++)
                if (ContainsPoint(points[i]) == false) return false;

            return true;
        }

        // Tests if a point on the plane of a polygon is within the actual bounds of the point
        // http://www.realtimerendering.com/intersections.html
        public static bool ContainsPoint(Vector3d rayPlaneIntersectionPoint, Vector3d[] polygonVertices)
        {
            const double MATCH_FACTOR = 0.99; // Used to cover up the error in floating point
            double angle = 0.0; // Initialize the angle

            for (int i = 0; i < polygonVertices.Length; i++) // Go in a circle to each vertex and get the angle between
            {
                Vector3d vA = polygonVertices[i] - rayPlaneIntersectionPoint;
                    // Subtract the intersection point from thecurrent vertex
                Vector3d vB = polygonVertices[(i + 1) % polygonVertices.Length] - rayPlaneIntersectionPoint;
                    // Subtract the point from the next vertex
                angle += Vector3d.AngleBetweenVectors(vA, vB);
                    // Find the angle between the 2 vectors and add them all up as we go along
            }
          
            if (angle >= (MATCH_FACTOR*(2.0*Math.PI))) // If the angle is greater than 2 PI, (360 degrees)
                return true; // The point is inside of the polygon

            return false; // If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }
    }
	public struct Line3d
    {
        private Vector3d[] _p;


        public Line3d(Vector3d v1, Vector3d v2)  :
            this(v1.x, v1.y, v1.z, v2.x, v2.y, v2.z)
        {

        }

        public Line3d(double x1, double y1, double z1, double x2, double y2, double z2) 
        {
            _p = new Vector3d[2];
            _p[0].x = x1;
            _p[0].y = y1;
            _p[0].z = z1;

            _p[1].x = x2;
            _p[1].y = y2;
            _p[1].z = z2;       
        }
        public Vector3d[] Point
        {
            get { return _p; }
        }

        public void SetEndPoints(Vector3d start, Vector3d end)
        {
            if (_p == null) throw new Exception("Line3D.SetEndPoints() - Line3d not initialized.");

            _p[0] = start;
            _p[1] = end;
        }

        // Overloaded the == operator in EDGE to return as true any two edges that have same endpoints regardless of order.
        // i.e.  AB=AB && AB = BA
        public static bool operator ==(Line3d e1, Line3d e2)
        {
            return (e1.Point[0].x == e2.Point[0].x
                    && e1.Point[0].y == e2.Point[0].y
                    && e1.Point[0].z == e2.Point[0].z
                    && e1.Point[1].x == e2.Point[1].x
                    && e1.Point[1].y == e2.Point[1].y
                    && e1.Point[1].z == e2.Point[1].z)
                   ||
                   (e1.Point[0].x == e2.Point[1].x
                    && e1.Point[0].y == e2.Point[1].y
                    && e1.Point[0].z == e2.Point[1].z
                    && e1.Point[1].x == e2.Point[0].x
                    && e1.Point[1].y == e2.Point[0].y
                    && e1.Point[1].z == e2.Point[0].z);
        }

        public static bool operator !=(Line3d e1, Line3d e2)
        {
            return !(e1 == e2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Line3d)
                return this == (Line3d) obj;
            else
                return base.Equals(obj);
        }

        public Vector3d ClosestPoint(Vector3d p)
        {
            return ClosestPointOnLine(this._p[0], this._p[1], p);
        }

        public double DistanceSquared (Vector3d p, out Vector3d closestPoint)
        {
            return DistanceSquared(this._p[0], this._p[1], p, out closestPoint);
        }

        public double Distance (Vector3d p, out Vector3d closestPoint)
        {
            return System.Math.Sqrt( DistanceSquared(p, out closestPoint));
        }

        public static Vector3d Center (Vector3d start, Vector3d end)
        {
            return end + ((start - end) / 2);
        }
        // ----------------------------------------------------------------------
        // Name  : closestPointOnLine()
        // Input : a - first end of line segment
        //         b - second end of line segment
        //         p - point we wish to find closest point on line from 
        // Notes : Helper function for closestPointOnTriangle()
        // Return: closest point on line segment
        // -----------------------------------------------------------------------  
        /// <summary>
        /// Returns a point on the line that is closest to point P. Thus this does not just return a or b, but any point in between that 
        /// will result in a perpendicular line from p to the line segment
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector3d ClosestPointOnLine(Vector3d a, Vector3d b, Vector3d p)
        {
            if (a == b) return a; // zero length line segment

            // Determine t (the length of the vector from �a� to �p�)
            Vector3d c = p - a;
            Vector3d V = b - a;

            double d = V.Length;

            V = Vector3d.Normalize(V);
            double t = Vector3d.DotProduct(V, c);
            
            // Check to see if �t� is beyond the extents of the line segment
            if (t < 0.0f) return (a);
            if (t > d) return (b);
            
            // Return the point between �a� and �b�
            //set length of V to t. V is normalized so this is easy
            V.x = V.x * t;
            V.y = V.y * t;
            V.z = V.z * t;

            return (a + V);
        }

        public static Vector3d ClosestPointOnAxis (Vector3d a, Vector3d b, Vector3d p)
        {
            if (a == b) return a; // zero length line segment

            // Determine t (the length of the vector from �a� to �p�)
            Vector3d c = p - a;
            Vector3d axis = b - a;

            axis = Vector3d.Normalize(axis);
            double t = Vector3d.DotProduct(axis, c);

            // scale axis 
            axis *= t; 

            return (a + axis);
        }


        public static double Distance(Line3d line, Vector3d p, out Vector3d closestPoint)
        {
            return System.Math.Sqrt(DistanceSquared(line, p, out closestPoint));
        }
        public static double DistanceSquared(Line3d line, Vector3d p, out Vector3d closestPoint)
        {
            return DistanceSquared(line._p[0], line._p[1], p, out closestPoint);
        }

        public static double DistanceSquared (Vector3d a, Vector3d b, Vector3d p, out Vector3d closestPoint)
        {
            closestPoint = ClosestPointOnLine(a, b, p);
            return (p - closestPoint).LengthSquared();
        }

        public static double Distance(Vector3d a, Vector3d b, Vector3d p, out Vector3d closestPoint)
        {
            return System.Math.Sqrt(DistanceSquared(a, b, p, out closestPoint));
        }

      //      Bresenham's Line Algorithm - VB XNA style
        // http://ilovevb.net/Web/blogs/vbxna/archive/2008/04/15/bresenham-s-line-algorithm-vb-xna-style.aspx
     
      // 1:  Imports Microsoft.Xna.Framework
      // 2:  Imports System.Math
      // 3:   
      // 4:  Public Class Utils
      // 5:      ''' <summary>
      // 6:      ''' This function uses Bresenham's Line Algorithm to find the 
      // 7:      ''' most direct path between two points on a 2D grid and stores 
      // 8:      ''' all the points in a list.
      // 9:      ''' </summary>
      //10:      ''' <param name="StartPosition">Starting X,Y coordinates</param>
      //11:      ''' <param name="EndPosition">Starting X,Y coordinates</param>
      //12:      ''' <returns>List(Of Vector2)</returns>
      //13:      ''' <remarks></remarks>
      //14:      Public Function DeterminePath(ByVal StartPosition As Vector2, _
      //15:                     ByVal EndPosition As Vector2) As List(Of Vector2)

      //16:          Dim myPoint As Vector2 = StartPosition ' current point
      //17:          Dim myPath As New List(Of Vector2) ' collection of path points
      //18:   
      //19:          ' Get the difference between 2 points
      //20:          Dim deltaX As Integer = EndPosition.X - StartPosition.X
      //21:          Dim deltaY As Integer = EndPosition.Y - StartPosition.Y
      //22:          Dim leftover As Integer
      //23:   
      //24:          ' Figure out direction based on the +/- value of the deltas
      //25:          Dim dirX As Integer = IIf(deltaX < 0, -1, 1)
      //26:          Dim dirY As Integer = IIf(deltaY < 0, -1, 1)
      //27:   
      //28:          ' Get absolute, we'll decide whether to add/subtract later 
      //29:          deltaX = Abs(deltaX)
      //30:          deltaY = Abs(deltaY)
      //31:   
      //32:          ' Uncomment this to add the first point to the path (list)
      //33:          ' myPath.Add(myPoint)
      //34:   
      //35:          ' iterate through whichever axis is longest
      //36:          If deltaX > deltaY Then
      //37:              leftover = (deltaY * 2) - deltaX

      //38:              While myPoint.X <> EndPosition.X
      //39:                  If leftover >= 0 Then
      //40:                      myPoint.Y = myPoint.Y + dirY
      //41:                      leftover = leftover - deltaX
      //42:                  End If

      //43:                  myPoint.X = myPoint.X + dirX
      //44:                  leftover = leftover + deltaY
      //45:                  myPath.Add(myPoint)
      //46:              End While
      //47:          Else

      //48:              leftover = (deltaX * 2) - deltaY

      //49:              While myPoint.Y <> EndPosition.Y
      //50:                  If leftover >= 0 Then
      //51:                      myPoint.X = myPoint.X + dirX
      //52:                      leftover = leftover - deltaY
      //53:                  End If
      //54:                  myPoint.Y = myPoint.Y + dirY
      //55:                  leftover = leftover + deltaX
      //56:                  myPath.Add(myPoint)
      //57:              End While
      //58:          End If

      //59:   
      //60:          Return myPath
      //61:      End Function
      //62:  End Class
    }
	
	///
    /// Ray class, for use with the optimized ray-box intersection test
    /// described in:
    ///
    ///      Amy Williams, Steve Barrus, R. Keith Morley, and Peter Shirley
    ///      "An Efficient and Robust Ray-Box Intersection Algorithm"
    ///      Journal of graphics tools, 10(1):49-54, 2005
    /// 
    /// http://www.realtimerendering.com/intersections.html
    public class Ray
    {
        public Vector3d Origin;
        public Vector3d Direction;
        
        // these two members are only used by BoundingBox.Intersects() - i could compute them there... it's only
        // 1 divide and 3 compares
        public Vector3d InverseDirection;
        public int[] Sign = new int[3];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="dir">Expects a normalized direction</param>
        public Ray(Vector3d orig, Vector3d dir)
        {
            Origin = orig;
            Direction = Vector3d.Normalize (dir);

            //InverseDirection = 1d / Direction ; // <-- this produces the wrong result.  And this is the only place we ever called that op_Divide overloaded function.
            // March.11.2024 - NOTE: we don't want to prevent divide by 0 here
            //                 as we did in the operator overloaded op_Divide.
            //                 This breaks the BoundingBox.Inversects(r) test
            InverseDirection.x = 1d / Direction.x;
            InverseDirection.y = 1d / Direction.y;
            InverseDirection.z = 1d / Direction.z;

            Sign[0] = (InverseDirection.x < 0d) ? 1 : 0;
            Sign[1] = (InverseDirection.y < 0d) ? 1 : 0;
            Sign[2] = (InverseDirection.z < 0d) ? 1 : 0;
        }

        // clone
        public Ray(Ray r)
        {
            Origin = r.Origin;
            Direction = r.Direction;
            Sign[0] = r.Sign[0];
            Sign[1] = r.Sign[1];
            Sign[2] = r.Sign[2];
        }

        /// <summary>
        ///  typically used to return a ray that is transformed to modelspace
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public Ray Transform(Matrix m)
        {
            Vector3d newOrig, newDir;
                       
            // Transform ray origin and direction by matrix
            newOrig = Vector3d.TransformCoord(Origin, m);
            newDir = Vector3d.TransformNormal(Direction, m);
            return new Ray (newOrig, Vector3d.Normalize(newDir));
        }
    }

	
	/// <summary>
    /// Defines the current axes on which a manipulator is operating
    /// </summary>
    [Flags]
    public enum AxisFlags : int
    {
        None = 0,

        X = (0x1 << 0),
        Y = (0x1 << 1),
        Z = (0x1 << 2),

        XY = X | Y,
        YX = Y | X,
        XZ = X | Z,
        ZX = Z | X,
        YZ = Y | Z,
        ZY = Z | Y,

        XYZ = X | Y | Z,

        All = XYZ
    }

    /// <summary>
    /// Defines the vector space in which a manipulator is operating
    /// </summary>
    public enum VectorSpace
    {
        World,              // Manipulating with world space basis vectors
        Local               // Manipulating with local space basis vectors
    }

    public class Axis
    {

        /// <summary>
        /// Utility function that returns the unit axis in Vector3 format that corresponds to the 
        /// specified axes, oriented based on the vector space of the manipulator
        /// </summary>
        /// <param name="axis">The axes for which to retrieve the corresponding unit axis</param>
        /// <returns>The unit axis that corresponds to the specified axes</returns>
        public static Vector3d GetUnitAxis(Quaternion targetRotation, AxisFlags axes, VectorSpace vectorSpace)
        {
            Vector3d unit;
            unit.x = 0;
            unit.y = 0;
            unit.z = 0;

            // note these are NOT if / else blocks.  Execution falls through and each successive flag can
            // potentially be true when multiple axis are ORd together
            if ((axes & AxisFlags.X) == AxisFlags.X)
                unit.x += 1;
            if ((axes & AxisFlags.Y) == AxisFlags.Y)
                unit.y += 1;
            if ((axes & AxisFlags.Z) == AxisFlags.Z)
                unit.z += 1;

            if (unit.x != 0 || unit.y != 0 || unit.z != 0)
                unit.Normalize();

            // in local vector space, rotate the axis with the transform's
            // rotation component, otherwise return the axis in its default
            // form for world vector space
            unit = (vectorSpace == VectorSpace.Local)
                ? Vector3d.TransformNormal(unit, targetRotation)
                : unit;

            return unit;
        }
    }
	
	
	// NOTE: GameTime does not utilize any Windows Timer.  The "elapsedSeconds" is passed in from 
	//       an instance of Keystone.Timers.Timer.cs from within the gameloop in AppMain.cs
	
	// GameTime is the time elapsed in "game" time which CAN be scaled to go FastForward or Backward.
	//          GameTime mostly differentiates between REAL-LIFE-TIME where there is no PAUSING
	//          Real-Life-Time-Total-Elapsed is the seconds from when the game started.  It can never be affected by PAUSE or SCALING.
	// 
    // GetSimulatedTime() - SimulatedTime.ElapsedSeconds() is computed as GameTime's ElapsedSeconds * GAME_TIME_FACTOR  E.g. 1 minute GameTime with a TIME_FACTOR = 1000 = 1000 minutes in game time * any scaling as well.
	
    public class GameTime 
    {        
        public IntervalTimers IntervalTimers;
		
		private TimeSpan mTotalElapsedSeconds;
		private TimeSpan mElapsedSeconds;  
		private TimeSpan mStartOffset; // instead of our stopwatch starting at 0, advance it by x amount .eg mStarOffset = TimeSpan.FromMinutes(5); then  TimeSpan totalTime = mStopwatch.Elapsed + mStarOffset;
		private double mElapsedGameTimeSeconds;

        private bool mIsPaused;
        private float _timeScaling;                    // used for FFWD and REVERSE time speed ups and slow downs
        private float mGameSecondsPerEachRealSecond;  // eg. 60 gameSeconds for every real life second means every real life minute results in one hour of game time passing
        
       
        
        
		//private long mTicksAtStart;
		//private long mTicks;
		private float _julianDay;

		private TimeSpan? START_OFFSET = default(TimeSpan);
		
        // TODO: use Stopwatch here!!!  

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeScaling">minimum value must be >0.0 unless we want to support reverse time.</param>
        public GameTime(float timeScaling = 1.0f, TimeSpan? startOffset = null)
        {
        	// TODO: what if 0.0 == paused/stopped
            if (timeScaling <= 0f) throw new ArgumentOutOfRangeException("GameTime.ctor() - timeScaling must be greater than 0.");
            _timeScaling = timeScaling;
            
			//_time = new DateTime();
			//mTicksAtStart = _time.Ticks;
			
			START_OFFSET = startOffset;
			
            IntervalTimers = new IntervalTimers();

            // http://stackoverflow.com/questions/5248827/convert-datetime-to-julian-date-in-c-sharp-tooadate-safe

			// TODO: Fix below
       //     int a = (14 - _time.Month) /12;
       //     int y = 1975 + 4800 - a;
       //     int m = _time.Month + 12 * a - 3;
       //     _julianDay = _time.DayOfYear + (153 * m + 2) / 5 + y * 365 + y / 4 - y / 100 + y / 400 - 32045;
            _julianDay -= 2442414;
            _julianDay -= 1f / 24f;
        }

        public GameTime() : this (1.0f)
        {
        }
        
        /// <summary>
        /// Equivalent to gameSecondsPerRealLifeSecond.  
        /// eg. 60 gameSeconds per real life second means 
        /// every real life minute results in one hour of game time passing
        /// </summary>
        public float Scale {get {return _timeScaling;} set{_timeScaling = value;}}
        

		/// <summary>
		/// Returns the elapsed seconds for the most recent frame.
		/// </summary>
        public double ElapsedSeconds
        {
            get
            {
                // TODO: TV's AccurateTimeElapsed() fixes issues im having with my own GameTime management.
                //       I need to fix my own system, but for now this works.  
                //double elapsedSeconds = (double)CoreClient._CoreClient.Engine.AccurateTimeElapsed();
                //elapsedSeconds /= 1000d;
                //return elapsedSeconds;
              return mElapsedSeconds.Seconds; 
			}
        }
        
        /// <summary>
        /// Elapsed game time in seconds
        /// </summary>
		public double ElapsedGameTime
		{
			get {return mElapsedGameTimeSeconds; }
		}
	
        public double TotalElapsedSeconds
        {
        	get 
			{ 	
				return mTotalElapsedSeconds.Seconds;
			}
        }
        
        public double JulianDay // total number of days including fractional days 
        {
        	get 
        	{
        		return _julianDay; // TODO: FIX THIS + _time.TimeOfDay.TotalDays;
        	}
        }

		public void Update (TimeSpan ts)
		{
			if (_timeScaling == 0.0f) return; 
        	
            mElapsedSeconds.Add(ts);
							
			mTotalElapsedSeconds += mElapsedSeconds;
			TimeSpan totalTime = mTotalElapsedSeconds + mStartOffset;
								
            mElapsedGameTimeSeconds = mElapsedSeconds.Seconds * _timeScaling;
		
            IntervalTimers.Update(ts.Seconds);
		}
		
		
        public void Update(double elapsedSeconds)
        {
        	this.Update(TimeSpan.FromSeconds(elapsedSeconds));
        }
    }
	
	

    public class IntervalTimers
    {			
        public delegate string IntervalCompleted(string nodeID, string name);
        //private List<TimePeriod> mTimePeriods;
        private Dictionary<string, TimePeriod> mKeyedTimePeriods;
		private System.Collections.Concurrent.ConcurrentDictionary<string, TimePeriod> mIntervals;
		
		
        // NOTE: Using a class for TimePeriod instead of a struct allows us to easily
        //       increment timePeriod.Elapsed and decrement timePeriod.RepeatsRemaining without
        //       having to update this timePeriod within the Dictionary.
        private class TimePeriod
        {
            public string OwnerID;
            public string Name;

            // milliseconds
            public double Duration; // the duration in Seconds this Period will last before completed ("IsReady")
            public double Elapsed;  // get's incremented each frame by elapsedSeconds and compared against Duration
            public bool Repeating;  // todo: im not sure this is useful because if we find that a TimePeriod has elapsed, then the next Elapsed may need to have the remainder added to it if we're just going to automatically repeat and not wait for a handler to process the current elapsed Interval and then start the next Interval if it wants to...
            public int RepeatCount;
            public int RepeatsRemaining;
            private bool mIsActive;
            // there should be no need to modify the Elapsed when resuming 
            // because we do not store the starting TickCount, we just 
            // track the elapsed duration
            public bool IsPaused;

            public bool DeActivateAfterCompleted;

            /// notifies the caller that the Interval with the specified "Name" has completed.
            public IntervalCompleted IntervalCompletedCB;


            public bool IsReady 
			{ 
				get 
				{ 
					return Elapsed >= Duration; } 
			}

            ///<summary>
            /// Rather than delete a Timer, sometimes we just want to 
            /// set IsActive=false and we will skip updates to it.
            ///</summary>
            public bool IsActive
            {
                get { return mIsActive; }
                set
                {
                    mIsActive = !mIsActive; // toggle the state
                    Elapsed = 0;            // always reset the Elapsed to 0
                }
            }
        }

		/// ctor()
		public IntervalTimers()
		{
		#if CONCURRENT_TIMERS
			if (mIntervals == null) mIntervals = new System.Collections.Concurrent.ConcurrentDictionary<string, TimePeriod>();
		#endif
		}

		

        public void Register(string nodeID, string name, double durationInSeconds, bool activateImmediately = true, bool repeating = false, int repeatCount = 0)
        {
            TimePeriod tp = new TimePeriod();

            tp.OwnerID = nodeID;
            tp.Name = name;
            tp.Duration = durationInSeconds;
            tp.Elapsed = 0d;
            tp.Repeating = repeating;
            tp.RepeatCount = repeatCount;
            tp.RepeatsRemaining = repeatCount;

            tp.IsPaused = false;
            tp.DeActivateAfterCompleted = false;
            tp.IntervalCompletedCB = null;

            tp.IsActive = activateImmediately;

            string key = GetKey(nodeID, name);
	
			//Console.WriteLine ("Register " + key + " IS PAUSED == " + tp.IsPaused.ToString());
			
#if CONCURRENT_TIMERS
			if (!mIntervals.TryAdd(key, tp))
				throw new Exception("IntervalTimers.Register() - FAILED" );
#else
            if (mKeyedTimePeriods == null) mKeyedTimePeriods = new Dictionary<string, TimePeriod>();
            mKeyedTimePeriods.Add(key, tp);
#endif   
		}

        public void UnRegister(string nodeID, string name)
        {
            // TODO: remove this period from the dictionary
            if (mKeyedTimePeriods == null)
            {
                Console.WriteLine("IntervalTimers.UnRegister() - " + nodeID + " using name " + name + " does not exist.");
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);

            if (success)
                mKeyedTimePeriods.Remove(key);
            else
                Console.WriteLine("IntervalTimers.UnRegister() - " + nodeID + " using name " + name + " does not exist.");

        }

        ///<summary>
        /// Unregisters all Intervals registered for a specific nodeID
        ///</summary>
        public void Interval_UnRegisterAll(string nodeID)
        {

        }


        //public TimePeriod[] GetAllTimeIntervals (string nodeID)
        //{
        //    // for this to work, we must test for existance of "nodeID" at the start of every key in the dictionary 
        //    return null;
        //}

        public void Reset(string nodeID, string name, bool suspend = false)
        {
#if CONCURRENT_TIMERS
			string key = GetKey(nodeID, name);
            TimePeriod tp;
			
			if (!mIntervals.TryGetValue(key, out tp))
				throw new Exception();
			
			tp.Elapsed = 0d;
#else
	
	
            if (mKeyedTimePeriods == null)
            {
                Console.WriteLine("IntervalTimers.Reset() - " + nodeID + " using name " + name + " does not exist.");
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);

            if (success)
                tp.Elapsed = 0d;
            else
                Console.WriteLine("IntervalTimers.Reset() - " + nodeID + " using name " + name + " does not exist.");
#endif
        
			tp.IsPaused = suspend;
			//Console.WriteLine ("Reset " + key + " IS PAUSED == " + tp.IsPaused.ToString());
		}

        public bool IsReady(string nodeID, string name)
        {
	#if CONCURRENT_TIMERS
			string key = GetKey(nodeID, name);
            TimePeriod tp;
			
			bool success = mIntervals.TryGetValue(key, out tp);
			
			//Console.WriteLine("IntervalTimers.Success = " + key + " = " + success.ToString() + " TP.Duration " + tp.Duration.ToString());
	#else
            if (mKeyedTimePeriods == null)
            {
                //Console.WriteLine("GameTime.IsReady() - " + nodeID + " using name " + name + " does not exist.");
                return false;
            }

            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);
	#endif
            if (success)
            {
                bool result = tp.IsPaused == false && tp.IsActive && (tp.Elapsed >= tp.Duration);
				//Console.WriteLine ("IntervalTimers. IS PAUSED == " + tp.IsPaused.ToString());
                //Console.WriteLine("IntervalTimers.IsReady() - " +  key + "RESULT == " + result.ToString() + " --> NOT PAUSED == " + (!tp.IsPaused).ToString() + " ACTIVE == " + tp.IsActive.ToString() + " COOLDOWN OVER == " + (tp.Elapsed >= tp.Duration).ToString());
                return result;
            }
            return false;
        }

        public bool IsActive(string nodeID, string name)
        {
			
	#if CONCURRENT_TIMERS
			string key = GetKey(nodeID, name);
            TimePeriod tp;
			
			bool success = mIntervals.TryGetValue(key, out tp);
				
			tp.Elapsed = 0d;
	#else
		
            if (mKeyedTimePeriods == null)
            {
                //Console.WriteLine("IntervalTimers.IsActive() - " + nodeID + " using interval named '" + name + "', does not exist.");
                //using HelloBoids.Transform;
                return false;
            }
            string key = GetKey(nodeID, name);
            TimePeriod tp;
            bool success = mKeyedTimePeriods.TryGetValue(key, out tp);
			//Console.WriteLine("IntervalTimers.IsActive() - " + success.ToString()  + " " + tp.Elapsed.ToString() + " " + nodeID + " using interval named '" + name + "', does not exist.");
			
	#endif
		
            if (success) return tp.IsActive;

            return false;
        }

        private string GetKey(string nodeID, string name)
        {
            return nodeID + "_" + name;
        }

        public void Update(double elapsedSeconds)
        {
			
	#if CONCURRENT_TIMERS
			if (mIntervals == null || mIntervals.Count == 0) return;
			
			foreach (TimePeriod period in mIntervals.Values)
	#else
		
            if (mKeyedTimePeriods == null || mKeyedTimePeriods.Count == 0) return;

            foreach (TimePeriod period in mKeyedTimePeriods.Values)
	#endif
            {
                if (!period.IsActive || period.IsPaused) continue;
                period.Elapsed += elapsedSeconds;
				//Console.WriteLine("IntervalTimers.IsReady() - " + period.OwnerID + " using name '" + period.Name + "' Elapsed = " + period.Elapsed.ToString());
				
                if (period.Elapsed >= period.Duration)
                {
                    period.IntervalCompletedCB?.Invoke(period.OwnerID, period.Name);

                    if (period.Repeating)
                    {
                        double spillOver = period.Elapsed - period.Duration;
                        period.Elapsed = spillOver;
                        period.RepeatsRemaining--;

                        // return before deactivation or removing the timePeriod
                        if (period.RepeatsRemaining > 0)
                            return;
                    }

                    // deactivate or remove this TimePeriod 
                    if (period.DeActivateAfterCompleted)
                        period.IsActive = false;
                    //else
                    //    todo: cant unregister it before caller can
                    //     check if IsReady== true !!
                    //     unless a delegate or event is raised

                    //    UnRegister(period.OwnerID, period.Name);

                }
            }
        }
    }

    /// <summary>
    /// Zaknafein's TV3D Profiler.  TODO: I should modify this to be more general purpose to include my debug text as well.
    /// </summary>
    public class Profiler
    {
        /// <summary>
        /// Enables or disables all profiler functions
        /// </summary>
        /// <remarks>Enabled by default to prevent an exception on first display</remarks>
        public bool ProfilerEnabled = true;
        /// <summary>
        /// Verbosity will make the profiler print the number of recorded loops and the unprofiled time
        /// </summary>
        public bool Verbose = true;

        public bool ShowFramesPerSecond = true;
        /// <summary>
        /// Enables or disables the fully qualified typename instead of "short" typename
        /// when using the parameterless hook. i.e. ProjectName.ClassName.MethodName
        /// </summary>
        public bool FullyQualifiedTypename;
        /// <summary>
        /// Should the profiler categorize the reflected profiles by their typename?
        /// </summary>
        public bool CategorizeByTypename;

        /// <summary>
        /// The profile for profiler text displaying
        /// </summary>
        private const string PROFILER_DISPLAY_PROFILE = "Profiler Display";
        /// <summary>
        /// The category for debugging information, can be used out of the profiler (hence the public)
        /// </summary>
        public const string DEBUGGING_CATEGORY = "Debugging";

        /// <summary>
        /// The sorted dictionary (hash-table) of all registered profiles
        /// </summary>
        private SortedList<string, Profile> mProfiles;
        /// <summary>
        /// The profiles ordered by category, in another sorted dictionary
        /// </summary>
        private SortedList<string, SortedList<string, Profile>> mCategories;
        /// <summary>
        /// Whether the profiler uses category.  This value is deduced at run-time and is not defined by user.
        /// </summary>
        private bool mUsesCategories;

        private double mTotalElapsedTime;
        //private double mLastTotalElapsedTime;
        private int mLoopCount;
        private int mFramesPerSecond;
        //private long mStartTime;
        private double UPDATE_INTERVAL = 1.0; // 1 second
        private Stopwatch mStopwatch;

        private bool mDisplayIsUpdated = false;
        object syncLock = new object();


        public Profiler()
        {

            mProfiles = new SortedList<string, Profile>();
            mCategories = new SortedList<string, SortedList<string, Profile>>();

            mDebugText = new List<Profiler.DebugText>();

            Register(PROFILER_DISPLAY_PROFILE, DEBUGGING_CATEGORY);
        }

        /// <summary>
        /// Registers a profile
        /// </summary>
        /// <param name="Name">The name of the profile, which is used for hooking it up</param>
        /// <param name="Category">The optional category</param>
        /// <remarks>
        /// No need to test if the profiler is enabled before calling it,
        /// the test it made inside every method of the profiler.
        /// </remarks>
        public void Register(string Name, string Category)
        {
            Profile P = new Profile(Name, Category);
            mProfiles.Add(Name, P);
            if (Category != null)
            {
                mUsesCategories = true;
                if (!mCategories.ContainsKey(Category))
                    mCategories.Add(Category, new SortedList<string, Profile>());
                mCategories[Category].Add(Name, P);
            }
        }

        /// <summary>
        /// Unregisters a profile
        /// </summary>
        /// <param name="Name">The profile's name</param>
        /// <remarks>Unregistering a non-existing profile will throw an exception</remarks>
        public void Unregister(string Name)
        {
            mProfiles.Remove(Name);
            if (mUsesCategories)
            {
                foreach (string Category in mCategories.Keys)
                {
                    if (mCategories[Category].ContainsKey(Name))
                        mCategories[Category].Remove(Name);
                }
            }
        }

        /// <summary>
        /// Marks the start of the profiling loop
        /// </summary>
        public void StartLoop()
        {
            if (ProfilerEnabled)
            {
                //mStartTime = Time.Counter;
                mStopwatch = new Stopwatch();
                mStopwatch.Start();
                mLoopCount++;
            }
        }

        /// <summary>
        /// Marks the end of the profiling loop
        /// </summary>
        public void EndLoop()
        {
            if (ProfilerEnabled)
            {
                mStopwatch.Stop();
                // accumulate mTotalElapsedTime for UPDATE_INTERVAL 
                //mTotalElapsedTime += (float)(Time.Counter - mStartTime) / Time.Frequency;
                //mLastTotalElapsedTime = mTotalElapsedTime;
                mTotalElapsedTime += mStopwatch.Elapsed.TotalSeconds;// Time.ElapsedSeconds(mStartTime); // Hypnotron Feb.12.2015 - added conversion to milliseconds since seconds and milliseconds  

                // ...and will accumulate the timers for 50 frames
                // This could be made with an elapsed time calculation,
                // to accumulate a full second for example
                if (mTotalElapsedTime >= UPDATE_INTERVAL)
                {
                    //ResetTimers();
                }
            }
        }

        /// <summary>
        /// Resets the accumulation timers
        /// </summary>
        private void ResetTimers()
        {
            mDisplayIsUpdated = true;
            //mLastTotalElapsedTime = mTotalElapsedTime;
            mTotalElapsedTime = 0;
            mLoopCount = 0;
            mFramesPerSecond = mLoopCount;

            lock (syncLock)
            {
                foreach (IProfile profile in mProfiles.Values)
                {
                    profile.ResetTimer();
                }
            }
        }

        /// <summary>
        /// Hooks a profile (starts its timer)
        /// </summary>
        /// <param name="Name">The name of the profile to hook up</param>
        /// <returns>An IDisposable instance (see remarks)</returns>
        /// <remarks>
        /// This function can (and should, when possible) be used with a Using declaration
        /// </remarks>
        public IProfileHook HookUp(string Name)
        {
            if (ProfilerEnabled)
            {
                // TODO: this should be made thread safe since when trying to
                //       hookup multiple times in threaded procedure, our timings will
                //       be wrong since the increments arent atmoic operations
                try
                {
                    IProfile profile = null;
                    lock (syncLock)
                    {
                        profile = mProfiles[Name];
                    }
                    IProfileHook hook = new ProfileHook(profile);
                    return hook;
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Hooks a new or existing profile for the caller's method
        /// </summary>
        /// <returns>An IDisposable instance (see remarks)</returns>
        /// <remarks>
        /// This function can (and should, when possible) be used with a Using declaration.
        /// Uses reflection to find the last stack frame, and generate the profile name from that.
        /// </remarks>
        public IProfileHook HookUp()
        {
            if (ProfilerEnabled)
            {
                MethodBase CallerMethod = (new StackTrace()).GetFrame(1).GetMethod();

                string CallerType;
                if (FullyQualifiedTypename)
                {
                    CallerType = CallerMethod.DeclaringType.FullName;
                }
                else
                {
                    CallerType = CallerMethod.DeclaringType.Name;
                }

                string CallerID = string.Format("{0}.{1}", CallerType, CallerMethod.Name);

                if (!mProfiles.ContainsKey(CallerID))
                {
                    if (CategorizeByTypename)
                        Register(CallerID, CallerType);
                    else
                        Register(CallerID, "");
                }

                return new ProfileHook(mProfiles[CallerID]);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Displays the profiles with their accumulated time and statistics
        /// </summary>
        /// <param name="Offset">The 2D offset to which the text should be printed</param>
        /// <returns>The additional Y offset that the profiler engendered</returns>
        /// <remarks>
        /// This function does NOT force a Text2D.Action_BeginText nor EndText.
        /// It should be enclosed within those at the caller side.
        /// </remarks>
        public int Display(int offsetX, int offsetY, TextOutputHandler AddDebugText)
        {
            if (ProfilerEnabled)
            {
                if (mDisplayIsUpdated == false)
                {
                    foreach (DebugText item in mDebugText)
                        AddDebugText(item.Text, item.OffsetX, item.OffsetY, item.Color);

                    return 0;
                }
                else
                {
                    mDebugText.Clear();

                    int additionalYOffset = 0;
                    double totalProfiledTime = 0;
                    int colorWhite = Utils.RGBA(0.8f, 0.8f, 0.8f, 0.8f);

                    string displayText = null;

                    //HookUp(PROFILER_DISPLAY_PROFILE);

                    if (ShowFramesPerSecond)
                    {
                        displayText = string.Format("{0} FPS", mLoopCount / mLoopCount);
                        AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                        additionalYOffset += 14 + 7;
                    }

                    // CATEGORIZED
                    if (mUsesCategories)
                    {
                        foreach (string Category in mCategories.Keys)
                        {
                            double CategoryTime = 0;

                            // display main Category Header
                            displayText = string.Format("{0} :", Category);
                            AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                            additionalYOffset += 14;

                            // display indented each element under this category
                            foreach (Profile Prof in mCategories[Category].Values)
                            {
                                //if (!(Prof.ElapsedTime == 0)) // skip profile if no elapsed time
                                //{
                                CategoryTime += Prof.ElapsedSeconds;

                                float timeRatio = (float)(Prof.ElapsedSeconds / mTotalElapsedTime);
                                displayText = string.Format(" {0} = {1:P} ({2:0.00} ms)", Prof.Name, timeRatio, Prof.ElapsedMilliseconds / mLoopCount);
                                AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(timeRatio, 1 - timeRatio, 0, 0.8f));

                                totalProfiledTime += Prof.ElapsedSeconds;

                                additionalYOffset += 14;
                                //}
                            }

                            // total stats for this category
                            float CategoryRatio = (float)(CategoryTime / mTotalElapsedTime);
                            displayText = string.Format("Totaling {0:P} ({1:0.00} ms)", CategoryRatio, CategoryTime * 1000f / mLoopCount);
                            AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(CategoryRatio, 1 - CategoryRatio, 0, 0.8f));
                            additionalYOffset += 14;
                            additionalYOffset += 7;
                        }

                        // display main Non-Categorized Header - these are simply Hooked profiles where user didn't enter a category name
                        displayText = "Non-Categorized :";
                        AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                        additionalYOffset += 14;
                    }

                    // NON-CATEGORIZED
                    double NonCategorisedTime = 0;
                    foreach (string Name in mProfiles.Keys)
                    {
                        Profile Prof = mProfiles[Name];

                        // display indented each non-categorized element under the catch-all "Non-Categorized" header
                        if (!(Prof.ElapsedSeconds == 0) && !Prof.Categorized)
                        {
                            float Ratio = (float)(Prof.ElapsedSeconds / mTotalElapsedTime);
                            string Format = "{0} = {1:P} ({2:0.00} ms)";
                            if (Verbose)
                                Format = " " + Format;

                            displayText = string.Format(Format, Name, Ratio, Prof.ElapsedMilliseconds / mLoopCount);
                            AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(Ratio, 1 - Ratio, 0, 0.8f));

                            totalProfiledTime += Prof.ElapsedSeconds;
                            NonCategorisedTime += Prof.ElapsedSeconds;

                            additionalYOffset += 14;
                        }
                    }

                    // total stats for Non-Categorized entries
                    float nonCategorizedTimeRatio = (float)(NonCategorisedTime / mTotalElapsedTime);
                    displayText = string.Format("Totaling {0:P} ({1:0.00} ms)", NonCategorisedTime / mTotalElapsedTime, NonCategorisedTime * 1000f / mLoopCount);
                    AddText(displayText, offsetX, offsetY + additionalYOffset, Utils.RGBA(nonCategorizedTimeRatio, 1 - nonCategorizedTimeRatio, 0, 0.8f));

                    additionalYOffset += 14 + 7;


                    // NON PROFILED - Header    	
                    if (Verbose)
                    {
                        double NonProfiledTime = mTotalElapsedTime - totalProfiledTime;
                        double NonProfiledTimeRatio = NonProfiledTime / mTotalElapsedTime;
                        displayText = string.Format("{0} = {1:P} ({2:0.000} ms)", "Non-Profiled", NonProfiledTimeRatio, NonProfiledTime * 1000f / mLoopCount);
                        AddText(displayText, offsetX, offsetY + additionalYOffset, colorWhite);
                    }

                    //Release(PROFILER_DISPLAY_PROFILE);

                    foreach (DebugText item in mDebugText)
                        AddDebugText(item.Text, item.OffsetX, item.OffsetY, item.Color);

                    mLoopCount = 0;
                    mDisplayIsUpdated = false;
                    return additionalYOffset;
                }
            }
            else
            {
                return 0;
            }
        }

        public void OutputToConsole()
        {
            double totalProfiledTime = 0;
            double NonCategorisedTime = 0;
            const string PREFIX_CATEGORIZED = "   @";
            const string PREFIX_NON_CATEGORIZED = "    @";

            if (ProfilerEnabled)
            {
                foreach (string Name in mProfiles.Keys)
                {
                    Profile prof = mProfiles[Name];

                    string category = PREFIX_CATEGORIZED + prof.Category + " - ";

                    // display indented each non-categorized element under the catch-all "Non-Categorized" header
                    if (!(prof.ElapsedSeconds == 0))
                    {
                        string prefix = category;
                        if (!prof.Categorized)
                            prefix = PREFIX_NON_CATEGORIZED;

                        double Ratio = prof.ElapsedSeconds / mTotalElapsedTime;
                        string Format = prefix + "{0} = {1:P} ({2:0.00} seconds)";
                        if (Verbose)
                            Format = " " + Format;

                        string displayText = string.Format(Format, Name, Ratio, prof.ElapsedSeconds);
                        //AddText(displayText, offsetX, offsetY + additionalYOffset, RGBA(Ratio, 1 - Ratio, 0, 0.8f));

                        totalProfiledTime += prof.ElapsedSeconds;
                        NonCategorisedTime += prof.ElapsedSeconds;

                        System.Console.WriteLine(displayText);

                        //additionalYOffset += 14;
                    }
                }
            }
        }

        public delegate void TextOutputHandler(string text, int offsetX, int offsetY, int color);

        private List<DebugText> mDebugText;
        private struct DebugText
        {
            public string Text;
            public int OffsetX;
            public int OffsetY;
            public int Color;
        }

        private void AddText(string text, int offsetX, int offsetY, int color)
        {
            DebugText item;
            item.Text = text;
            item.OffsetX = offsetX;
            item.OffsetY = offsetY;
            item.Color = color;

            mDebugText.Add(item);
        }

        /// <summary>
        /// Releases the hook on a profile
        /// </summary>
        /// <param name="Name">The name of the profile</param>
        /// <remarks>
        /// This method is only necessary when a profile has been hooked up without Using
        /// </remarks>
        public void Release(string Name)
        {
            if (ProfilerEnabled)
            {
                throw new NotImplementedException("Because of problem with nested profile calls, we moved timer functions to Hook and out of Profile.  We should always use using{} so that Hook can be used and so nested call timing is accurate.");
                // mProfiles[Name].StopTimer();
            }
        }
    }

    /// <summary>
    /// Description of IProfileHook.
    /// </summary>
    public interface IProfileHook : IDisposable
    {

    }

    /// <summary>
    /// Description of ProfileHook.
    /// </summary>
    internal class ProfileHook : IProfileHook
    {
        private IProfile mHookedProfile;
        private Stopwatch mStopwatch;
		private bool disposedValue = false;
		
		public ProfileHook(IProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("ProfileHook.ctor() - 'profile' argument cannot be null.");
            mHookedProfile = profile;

            mStopwatch = new Stopwatch();
            mStopwatch.Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                mStopwatch.Stop();
                mHookedProfile.Update(mStopwatch.Elapsed.TotalSeconds);
                mHookedProfile = null;
                //mStopwatch.Dispose();
            }
            this.disposedValue = true;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Description of IProfile.
    /// </summary>
    public interface IProfile
    {
        void Update(double elapsed);
        void ResetTimer();
    }

    /// <summary>
    /// Description of Profile.
    /// </summary>
    internal class Profile : IProfile
    {
        private string ProfileName;
        private string ProfileCategory;

        //   private long StartTime;
        private double TotalTime;
        private double LastTotal;
        // private bool HookedUp; // TODO: HookedUp var is never used. might be useful for re-entrant sync, but so far it's not used

        public Profile(string Name)
        {
            this.ProfileName = Name;
        }
        public Profile(string Name, string Category)
        {
            this.ProfileName = Name;
            this.ProfileCategory = Category;
        }

        public void Update(double elapsed)
        {
            LastTotal = TotalTime;
            TotalTime += elapsed;
        }

        public void ResetTimer()
        {
            LastTotal = TotalTime;
            TotalTime = 0;
        }

        // we cache the last elapsed since we will only update the display every x interval.
        // thus, our display isn't erratic
        public double ElapsedSeconds
        {
            get { return LastTotal; }
        }

        public double ElapsedMilliseconds { get { return LastTotal * 1000d; } }

        public bool Categorized
        {
            get { return ProfileCategory != null; }
        }

        public string Category
        {
            get { return ProfileCategory; }
        }

        public string Name
        {
            get { return ProfileName; }
        }
    }



    public class MemoryFragmenter
    {
        private static List<GCHandle> _pinnedHandles = new List<GCHandle>();
        private static List<byte[]> _allocatedObjects = new List<byte[]>();

        public static void Fragment(int numToPin, int pinSize, int numToFree, int freeSize)
        {
            Console.WriteLine("Simulating memory fragmentation...");

            // Allocate and pin some objects
            for (int i = 0; i < numToPin; i++)
            {
                byte[] data = new byte[pinSize];
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                _pinnedHandles.Add(handle);
                _allocatedObjects.Add(data); // Keep a reference to prevent immediate collection
            }

            Console.WriteLine("Initial objects allocated and pinned.");

            // Release some unpinned objects to create "holes"
            // We'll simulate this by allocating and then letting some go out of scope
            for (int i = 0; i < numToFree; i++)
            {
                byte[] temp = new byte[freeSize]; // Allocate temporarily
                                                  // No pinning for these, they can be collected
            }
            Console.WriteLine("Temporary objects created and potentially collected.");

            // Force a garbage collection to compact and reveal fragmentation
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Console.WriteLine("Garbage collection performed. Fragmentation might be present.");

            /*  // Now, try to allocate a large contiguous block
                try
                {
                    byte[] largeBlock = new byte[1024 * 1024 * 5]; // 5MB
                    Console.WriteLine("Successfully allocated a large contiguous block.");
                }
                catch (OutOfMemoryException)
                {
                    Console.WriteLine("Failed to allocate a large contiguous block due to fragmentation.");
                }
            */
        }

        public static void Cleanup()
        {
            // Clean up pinned handles
            foreach (var handle in _pinnedHandles)
            {
                handle.Free();
            }

            _pinnedHandles.Clear();
            _allocatedObjects.Clear(); // Allow all objects to be collected
            GC.Collect();
            Console.WriteLine("Cleanup complete.");
        }
        
        public static object[] CreateAndFreeObjects(int LargeObjectSize)
        {
            object[] tmp = new object[3];

            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = new byte[LargeObjectSize];

            // Allocate some large objects within this method's scope


            // These objects become eligible for GC once the method exits, 
            // creating "holes" in the LOH over time.
            return tmp;
        }
    }


	#region Random Number Generation
	///<summary>
	/// code by Alessandro D'Andria
	/// https://stackoverflow.com/questions/19270507/correct-way-to-use-random-in-multithread-application
	/// </summary>
	public class ThreadedRandom
	{
		static int mSeed = Environment.TickCount;

		public ThreadedRandom(int seed)
		{
			mSeed = seed;
		}
		
		/*
		// NOTE: the use of the "ThreadLocal<>" generic  provides a thread-local Random instance , meaning each thread that accesses the variable mRandom, gets an independently initialized copy of the variable.
		// This mechanism ensures data isolation between threads, eliminating the need for synchronization and thus improving performance and simplifying concurrent programming. 
		private readonly System.Threading.ThreadLocal<Random> mTLRandom =
			new System.Threading.ThreadLocal<Random>(() => new Random(System.Threading.Interlocked.Increment(ref mSeed)));

		public int NextInt()
		{
			// this could be confusing, but understand we reference "Value" because this is the Random var from the ThreadLocal<Random> variable named mTLRandom
			return mTLRandom.Value.Next();
		}

		public double NextDouble()
		{
			return mTLRandom.Value.NextDouble();
		} 
		*/
			
			
		
		// https://codeblog.jonskeet.uk/2009/11/04/revisiting-randomness/
		private static readonly Random globalRandom = new Random();
		private static readonly object globalLock = new object();

		private static readonly System.Threading.ThreadLocal<Random> mTLRandom = new System.Threading.ThreadLocal<Random>(NewRandom);

		public static Random NewRandom()
		{
			lock (globalLock)
			{
				return new Random(globalRandom.Next());
			}
		}

		public static Random Instance { get { return mTLRandom.Value; } }

		public static int Next()
		{
			return Instance.Next();
		}
		
		public double NextDouble()
		{
			return Instance.NextDouble();
		} 
	}
	#endregion

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN MEMORY STORES

	public class Seeds
	{
		private int mMaster;
		private int mLocal_DroidLogic;
		//public int Local_D
		
		public Seeds (int seed)
		{
			mMaster = seed;
			mLocal_DroidLogic = mMaster + 1;
			
		}
		
		public int Master {get {return mMaster;}}
		public int Local_Droid_Logic {get {return mLocal_DroidLogic;}}
		
		
	}
	
    public class Utils
    {
        static uint frame_count = 0;
        static long last_fps_time = -1;

        static long last_frame_time = -1;

        public int GetFrequency()
        {
            if (last_frame_time < 0)
            {
                last_frame_time = DateTime.Now.Ticks;
                last_fps_time = last_frame_time;
            }
            long now = DateTime.Now.Ticks;
            long dt = now - last_frame_time;
            last_frame_time = now;

            int dt_fps = (int)(now - last_fps_time);
            if (dt_fps > 1)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("{0} fps", frame_count / dt_fps));
                frame_count = 0;
                last_fps_time = DateTime.Now.Ticks;
            }
            ++frame_count;
            return dt_fps;
        }

		public static long GetUsedMemory(bool forceFullCollection)
		{
			long usedMemoryWithGC = GC.GetTotalMemory(forceFullCollection);
			return usedMemoryWithGC;
		}
		
		public static long GetTotalAllocatedBytes(bool precise = false)
		{
			long results = GetTotalAllocatedBytes(precise );
			return results;
		}
		
			
		#region FileIO
		public static void AppendText(string fullPath, string text)
		{
			bool append = true;
			using (System.IO.TextWriter tw = new System.IO.StreamWriter(fullPath, append))
				tw.WriteLine(text, System.Text.Encoding.UTF8);
		}
		
		/// <summary>
		/// .NETFiddle and other online compilers local stores 
		/// this will get the path our .NETFiddle app is running on at the remote server
		/// </summary>
		public static string GetPath (string fileName)
		{
			string fullPath = System.IO.Directory.GetCurrentDirectory();
			fullPath = System.IO.Path.Combine(fullPath, fileName);
			
			return fullPath;
		}
		
		/// <summary>
		/// .NETFiddle and other online compilers local stores 
		/// this will get the path our .NETFiddle app is running on at the remote server
		/// </summary>
		public static string CreateFile(string fileName)
		{
			//string p = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			//p = System.IO.Directory.GetCurrentDirectory();
			//p += "/Downloads";

			string fullPath = GetPath(fileName);

			// create a file in the path our app is compiled too
			System.IO.FileStream stream = System.IO.File.Create (fullPath);
			
			// verify it exists
			bool exists = System.IO.File.Exists(fullPath);
			Console.WriteLine(fullPath + "  File Created = "  + exists.ToString() );
		
			stream.Close();
			
			return fullPath;
		}
	
		// TODO: past all the data we want to load into the below and then pass in the path to write it
		public static void WriteFile(string path, string textToWrite)
		{
			System.IO.File.WriteAllText(path, textToWrite, System.Text.Encoding.UTF8);
		}

		public static string ReadAllText(string path)
		{
			return System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
		}
		#endregion
		
		
		// TODO: these Now.Ticks should be based on GAME TIME and never REAL-TIME.
		//       So for instance, if we advance a frame by a FIXED TIME STEP of 0.025 then
		//       the tickCounter should advance by that much which is what we do
		//       in GameTime.Ticks() except its not a static method
		public static long NowTicks()
		{
			return DateTime.Now.Ticks;
		}
		
		// https://github.com/MonoGame/MonoGame/blob/db9e544dfb3f1c1e8bfc2ea08fec31c1c17a9033/MonoGame.Framework/Game.cs#L539
		public static long GetAge (long startingTicks)
		{
			long diff = NowTicks() - startingTicks;
			Console.WriteLine("Show long As TimeSpan : {0}", new TimeSpan(diff));
			return diff;
		}
		
		public static double GetAge (double startingOLEAutomationDate)
		{
			double diff = DateTime.Now.ToOADate() - startingOLEAutomationDate;
			
			Console.WriteLine("Show Age using DateTime As TimeSpan : {0}", DateTime.FromOADate(diff));
			return diff;
		}
		
        public static string GetTimeString()
        {
            // NOTE: When running on Online Compiler, this time will be the time of the SERVER 
            //       running the online compiler and NOT your local PC.
            return NowTicks().ToString();
        }

		private static readonly string[] SizeSuffixes = 
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		
		/// <summary>
		/// code by stackoverflow user "JLRishe"
		/// https://stackoverflow.com/users/1945651/jlrishe
		/// </summary>
		public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
		{
			if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
			if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); } 
			if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

			// mag is 0 for bytes, 1 for KB, 2, for MB, etc.
			int mag = (int)Math.Log(value, 1024);

			// 1L << (mag * 10) == 2 ^ (10 * mag) 
			// [i.e. the number of bytes in the unit corresponding to mag]
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			// make adjustment when the value is large enough that
			// it would round up to 1000 or more
			if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
			{
				mag += 1;
				adjustedSize /= 1024;
			}

			return string.Format("{0:n" + decimalPlaces + "} {1}", 
				adjustedSize, 
				SizeSuffixes[mag]);
			
			/* alternate code version by same author "JLRishe"
			// https://stackoverflow.com/users/1945651/jlrishe
			// "And here's the original implementation I suggested, which may be marginally slower, but a bit easier to follow:"
			if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); } 

			int i = 0;
			decimal dValue = (decimal)value;
			while (Math.Round(dValue, decimalPlaces) >= 1000)
			{
				dValue /= 1024;
				i++;
			}

			return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);

			// Useage -> Console.WriteLine(SizeSuffix(100005000L));
			*/
		}
		
        public static int RGBA(float r, float g, float b, float a)
        {
            int A = (int)(255 * a);
            int R = (int)(255 * r);
            int G = (int)(255 * g);
            int B = (int)(255 * b);

            A = A << 24;
            R = R << 16;
            G = G << 8;

            return A | R | G | B;
        }
				
		public static double GetMax(double value1, double value2, double value3)
		{
			double result = System.Math.Max(value1, value2);
			result = System.Math.Max(result, value3);
			return result;
		}
		
		public static double RandomWithVariance(Random rand, double baseValue, double variancePercentage)
		{
			double min = baseValue * (1 - variancePercentage);
			double max = baseValue * (1 + variancePercentage);
			double damageAmountWithVariance = rand.NextDouble() * (max - min) + min;
			
			return damageAmountWithVariance;
		}
		
		public static byte[] CompressWithBrotli(byte[] inputBytes)
		{
			using var outputStream = new MemoryStream();
			using (var brotliStream = new System.IO.Compression.BrotliStream(outputStream, System.IO.Compression.CompressionLevel.Optimal))
			{
				brotliStream.Write(inputBytes, 0, inputBytes.Length);
			}
			return outputStream.ToArray();
		}
		
		public static byte[] DecompressWithBrotli(byte[] compressedData)
		{
			using var inputStream = new MemoryStream(compressedData);
			using var outputStream = new MemoryStream();
			
			using (var brotliStream = new System.IO.Compression.BrotliStream(inputStream, System.IO.Compression.CompressionMode.Decompress))
			{
				brotliStream.CopyTo(outputStream);
			}
			return  outputStream.ToArray();
			
			
		}
		
		// ArrayExtensions from KeystoneStandardLibrary.Extensions.ArrayExtensions
		/// <summary>
        /// Grow an array by one element and append new element there.
        /// </summary>
        /// <remarks>
        /// This method RETURNS the new array and does NOT change the array passed in.
        /// </remarks>
        public static T[] ArrayAppend<T>(T[] array, T element) // public static T[] ArrayAppend<T>(this T[] array, T element)
        {
            T[] tmp;
            if (array == null)
            {
                tmp = new T[1];
                tmp[0] = element;
            }
            else
            {
                tmp = new T[array.Length + 1];
                array.CopyTo(tmp, 0);
                tmp[array.Length] = element;
            }

            return tmp;
        }
    }



    /*
    // Dec.12.2025 - removed dependancy to Win32 APIs QueryPerformanceCounter and QueryPerformanceFrequency
    //               and using collection of stopwatch instead
    /// <summary>
    /// Time management
    /// </summary>
    /// <remarks>The Profiler module uses this module to calculate the elapsed times</remarks>
    public class Time
    {
        static Dictionary<string, Stopwatch> timers = new Dictionary<string, Stopwatch>();

        static void StartTaskTimer(string taskName)
        {
            if (!timers.ContainsKey(taskName))
            {
                timers[taskName] = new Stopwatch();
            }
            timers[taskName].Start();
        }

        static void StopTaskTimer(string taskName)
        {
            if (timers.ContainsKey(taskName))
            {
                timers[taskName].Stop();
                Console.WriteLine($"Task '{taskName}' elapsed time: {timers[taskName].Elapsed}");
            }
        }

        public static double ElapsedSeconds(string taskName)
        {
            return timers[taskName].Elapsed.TotalSeconds;

        }
    */


    /*
    // The performance counter API has the best precision
    [DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    [DllImport("Kernel32.dll")]
    private static extern bool QueryPerformanceFrequency(out long lpFrequency);

    public static long Counter
    {
        get
        {
            long R;
            QueryPerformanceCounter(out R);
            return R;
        }
    }

    private static long mFrequency = 0L;
    public static long Frequency
    {
        get 
        {
            // Caches the frequency since it doesn't change
            if (mFrequency == 0L)
                QueryPerformanceFrequency(out mFrequency);
            return mFrequency;
        }
    }

    public static double ElapsedSeconds (long startCounter)
    {
        return (Counter - startCounter) *  (1D / (double)Time.Frequency);

    }
    }
    */
	
	/********************************************************************
 *
 *  PropertyBag.cs
 *  --------------
 *  Copyright (C) 2002  Tony Allowatt
 *  Last Update: 12/14/2002
 * 
 *  THE SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS", WITHOUT WARRANTY
 *  OF ANY KIND, EXPRESS OR IMPLIED. IN NO EVENT SHALL THE AUTHOR BE
 *  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OF THIS
 *  SOFTWARE.
 * 
 *  Public types defined in this file:
 *  ----------------------------------
 *  namespace Flobbster.Windows.Forms
 *     class PropertySpec
 *     class PropertySpecEventArgs
 *     delegate PropertySpecEventHandler
 *     class PropertyBag
 *        class PropertyBag.PropertySpecCollection
 *     class PropertyTable
 *
 ********************************************************************/
	/// <summary>
	/// Represents a collection of custom properties that can be selected into a
	/// PropertyGrid to provide functionality beyond that of the simple reflection
	/// normally used to query an object's properties.
	/// </summary>
	public class PropertyBag : ICustomTypeDescriptor
	{

        protected string defaultProperty;
        protected PropertySpecCollection mSpecs; // TODO: this whole thing can likely be replaced by List<PropertySpec>

		#region private PropertySpecDescriptor class definition
		private class PropertySpecDescriptor : PropertyDescriptor
		{
			private PropertyBag bag;
			private PropertySpec item;

			public PropertySpecDescriptor(PropertySpec item, PropertyBag bag, string name, Attribute[] attrs) :
				base(name, attrs)
			{
				this.bag = bag;
				this.item = item;
			}

			public override Type ComponentType
			{
				get { return item.GetType(); }
			}

			public override bool IsReadOnly
			{
				get { return (Attributes.Matches(ReadOnlyAttribute.Yes)); }
			}

            public override Type PropertyType
            {

                get
                {
                    try
                    {
                        return Type.GetType(item.TypeName);
                    }

                    catch
                    {
                        Trace.WriteLine("Type '" + item.TypeName + "' unsupported." );
                        throw new ArgumentNullException("Type '" + item.TypeName + "' unsupported.");
                    }
                }
            }

			public override bool CanResetValue(object component)
			{
				if(item.DefaultValue == null)
					return false;
				else
					return !this.GetValue(component).Equals(item.DefaultValue);
			}

			public override object GetValue(object component)
			{
				// Have the property bag raise an event to get the current value
				// of the property.

				PropertySpecEventArgs e = new PropertySpecEventArgs(item, null);
				bag.OnGetValue(e);
				return e.Value;
			}

			public override void ResetValue(object component)
			{
				SetValue(component, item.DefaultValue);
			}

			public override void SetValue(object component, object value)
			{
				// Have the property bag raise an event to set the current value
				// of the property.

				PropertySpecEventArgs e = new PropertySpecEventArgs(item, value);
				bag.OnSetValue(e);
			}

			public override bool ShouldSerializeValue(object component)
			{
				object val = this.GetValue(component);

				if(item.DefaultValue == null || val == null)
					return false;
				else
					return !val.Equals(item.DefaultValue);
			}
		}
		#endregion // end private PropertySpecDescriptor nested class


		/// <summary>
		/// Initializes a new instance of the PropertyBag class.
		/// </summary>
		public PropertyBag()
		{
			defaultProperty = null;
			mSpecs = new PropertySpecCollection();
		}

		/// <summary>
		/// Gets or sets the name of the default property in the collection.
		/// </summary>
		public string DefaultProperty
		{
			get { return defaultProperty; }
			set { defaultProperty = value; }
		}

		/// <summary>
		/// Gets the collection of properties contained within this PropertyBag.
		/// </summary>
		public PropertySpecCollection Properties
		{
			get { return mSpecs; }
		}

		/// <summary>
		/// Occurs when a PropertyGrid requests the value of a property.
		/// </summary>
		public event PropertySpecEventHandler GetValue;

		/// <summary>
		/// Occurs when the user changes the value of a property in a PropertyGrid.
		/// </summary>
		public event PropertySpecEventHandler SetValue;

		/// <summary>
		/// Raises the GetValue event.
		/// </summary>
		/// <param name="e">A PropertySpecEventArgs that contains the event data.</param>
		public virtual void OnGetValue(PropertySpecEventArgs e)
		{
			// Feb.28.2026 - made public from protected - MichaelOliveTree
			if(GetValue != null)
				GetValue(this, e);
		}

		/// <summary>
		/// Raises the SetValue event.
		/// </summary>
		/// <param name="e">A PropertySpecEventArgs that contains the event data.</param>
		public virtual void OnSetValue(PropertySpecEventArgs e)
		{
			// Feb.28.2026 - made public from protected - MichaelOliveTree
			if(SetValue != null)
				SetValue(this, e);
		}

		#region ICustomTypeDescriptor explicit interface definitions
		// Most of the functions required by the ICustomTypeDescriptor are
		// merely pssed on to the default TypeDescriptor for this type,
		// which will do something appropriate.  The exceptions are noted
		// below.
		AttributeCollection ICustomTypeDescriptor.GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}

		string ICustomTypeDescriptor.GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}

		string ICustomTypeDescriptor.GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		TypeConverter ICustomTypeDescriptor.GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
		{
			// This function searches the property list for the property
			// with the same name as the DefaultProperty specified, and
			// returns a property descriptor for it.  If no property is
			// found that matches DefaultProperty, a null reference is
			// returned instead.

			PropertySpec propertySpec = null;
			if(defaultProperty != null)
			{
				int index = mSpecs.IndexOf(defaultProperty);
				propertySpec = mSpecs[index];
			}

			if(propertySpec != null)
				return new PropertySpecDescriptor(propertySpec, this, propertySpec.DisplayName , null);
			else
				return null;
		}

		object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
		{
			return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
		}

		PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
		{
			// Rather than passing this function on to the default TypeDescriptor,
			// which would return the actual properties of PropertyBag, I construct
			// a list here that contains property descriptors for the elements of the
			// Properties list in the bag.

			ArrayList props = new ArrayList();

			foreach(PropertySpec property in mSpecs)
			{
				ArrayList attrs = new ArrayList();

				// If a category, description, editor, or type converter are specified
				// in the PropertySpec, create attributes to define that relationship.
				if(property.Category != null)
					attrs.Add(new CategoryAttribute(property.Category));

				if(property.Description != null)
					attrs.Add(new DescriptionAttribute(property.Description));

				//if(property.EditorTypeName != null)
				//	attrs.Add(new EditorAttribute(property.EditorTypeName, typeof(UITypeEditor)));

				if(property.ConverterTypeName != null)
					attrs.Add(new TypeConverterAttribute(property.ConverterTypeName));

				// dec.24.2013 - Hypno - using System.Attributes in this way is unnecessary
				// Additionally, append the custom attributes associated with the
				// PropertySpec, if any.
				//if(property.Attributes != null)
				//	attrs.AddRange(property.Attributes);

				Attribute[] attrArray = (Attribute[])attrs.ToArray(typeof(Attribute));

				// Create a new property descriptor for the property item, and add
				// it to the list.
				PropertySpecDescriptor pd = new PropertySpecDescriptor(property,
					this, property.DisplayName, attrArray);
				props.Add(pd);
			}

			// Convert the list of PropertyDescriptors to a collection that the
			// ICustomTypeDescriptor can use, and return it.
			PropertyDescriptor[] propArray = (PropertyDescriptor[])props.ToArray(
				typeof(PropertyDescriptor));
			return new PropertyDescriptorCollection(propArray);
		}

		object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}
		#endregion
		
		#region PropertySpecCollection class definition
		/// <summary>
		/// Encapsulates a collection of PropertySpec objects.
		/// </summary>
		[Serializable]
		public class PropertySpecCollection : IList
		{
			private List<PropertySpec> _innerArray;
			
			/// <summary>
			/// Initializes a new instance of the PropertySpecCollection class.
			/// </summary>
			public PropertySpecCollection()
			{
				_innerArray = new List<PropertySpec>();
			}

			/// <summary>
			/// Gets the number of elements in the PropertySpecCollection.
			/// </summary>
			/// <value>
			/// The number of elements contained in the PropertySpecCollection.
			/// </value>
			public int Count
			{
				get { return _innerArray.Count; }
			}

			/// <summary>
			/// Gets a value indicating whether the PropertySpecCollection has a fixed size.
			/// </summary>
			/// <value>
			/// true if the PropertySpecCollection has a fixed size; otherwise, false.
			/// </value>
			public bool IsFixedSize
			{
				get { return false; }
			}
			
			/// <summary>
			/// Gets a value indicating whether the PropertySpecCollection is read-only.
			/// </summary>
			public bool IsReadOnly
			{
				get { return false; }
			}

			/// <summary>
			/// Gets a value indicating whether access to the collection is synchronized (thread-safe).
			/// </summary>
			/// <value>
			/// true if access to the PropertySpecCollection is synchronized (thread-safe); otherwise, false.
			/// </value>
			public bool IsSynchronized
			{
				get { return false; }
			}

			/// <summary>
			/// Gets an object that can be used to synchronize access to the collection.
			/// </summary>
			/// <value>
			/// An object that can be used to synchronize access to the collection.
			/// </value>
			object ICollection.SyncRoot
			{
				get { return null; }
			}

			/// <summary>
			/// Gets or sets the element at the specified index.
			/// In C#, this property is the indexer for the PropertySpecCollection class.
			/// </summary>
			/// <param name="index">The zero-based index of the element to get or set.</param>
			/// <value>
			/// The element at the specified index.
			/// </value>
			public PropertySpec this[int index]
			{
				get { return _innerArray[index]; }
				set { _innerArray[index] = value; }
			}

            ///// <summary>
            ///// Gets or sets the element at the specified index.
            ///// In C#, this property is the indexer for the PropertySpecCollection class.
            ///// </summary>
            ///// <param name="key">The hash key of the element to get or set.</param>
            ///// <value>
            ///// The element at the specified index.
            ///// </value>
            //public PropertySpec this[string key]
            //{
            //    get { return _innerArray[key]; }
            //    set 
            //    { 
            //        int index = IndexOf (key)
            //        _innerArray[key] = value; 
            //    }
            //}

			/// <summary>
			/// Adds a PropertySpec to the end of the PropertySpecCollection.
			/// </summary>
			/// <param name="value">The PropertySpec to be added to the end of the PropertySpecCollection.</param>
			/// <returns>The PropertySpecCollection index at which the value has been added.</returns>
			public int Add(PropertySpec value)
			{
				_innerArray.Add(value);
                return _innerArray.Count - 1;
			}

			/// <summary>
			/// Adds the elements of an array of PropertySpec objects to the end of the PropertySpecCollection.
			/// </summary>
			/// <param name="array">The PropertySpec array whose elements should be added to the end of the
			/// PropertySpecCollection.</param>
			public void AddRange(PropertySpec[] array)
			{
				_innerArray.AddRange(array);
			}

			/// <summary>
			/// Removes all elements from the PropertySpecCollection.
			/// </summary>
			public void Clear()
			{
				_innerArray.Clear();
			}

			/// <summary>
			/// Determines whether a PropertySpec is in the PropertySpecCollection.
			/// </summary>
			/// <param name="item">The PropertySpec to locate in the PropertySpecCollection. The element to locate
			/// can be a null reference (Nothing in Visual Basic).</param>
			/// <returns>true if item is found in the PropertySpecCollection; otherwise, false.</returns>
			public bool Contains(PropertySpec item)
			{
				return _innerArray.Contains(item);
			}

			/// <summary>
			/// Determines whether a PropertySpec with the specified name is in the PropertySpecCollection.
			/// </summary>
			/// <param name="name">The name of the PropertySpec to locate in the PropertySpecCollection.</param>
			/// <returns>true if item is found in the PropertySpecCollection; otherwise, false.</returns>
			public bool Contains(string name)
			{
				foreach(PropertySpec spec in _innerArray)
					if(spec.Name == name)
						return true;

				return false;
			}

			/// <summary>
			/// Copies the entire PropertySpecCollection to a compatible one-dimensional Array, starting at the
			/// beginning of the target array.
			/// </summary>
			/// <param name="array">The one-dimensional Array that is the destination of the elements copied
			/// from PropertySpecCollection. The Array must have zero-based indexing.</param>
			public void CopyTo(PropertySpec[] array)
			{
				_innerArray.CopyTo(array);
			}

			/// <summary>
			/// Copies the PropertySpecCollection or a portion of it to a one-dimensional array.
			/// </summary>
			/// <param name="array">The one-dimensional Array that is the destination of the elements copied
			/// from the collection.</param>
			/// <param name="index">The zero-based index in array at which copying begins.</param>
			public void CopyTo(PropertySpec[] array, int index)
			{
				_innerArray.CopyTo(array, index);
			}

			/// <summary>
			/// Returns an enumerator that can iterate through the PropertySpecCollection.
			/// </summary>
			/// <returns>An IEnumerator for the entire PropertySpecCollection.</returns>
			public IEnumerator GetEnumerator()
			{
				return _innerArray.GetEnumerator();
			}

			/// <summary>
			/// Searches for the specified PropertySpec and returns the zero-based index of the first
			/// occurrence within the entire PropertySpecCollection.
			/// </summary>
			/// <param name="value">The PropertySpec to locate in the PropertySpecCollection.</param>
			/// <returns>The zero-based index of the first occurrence of value within the entire PropertySpecCollection,
			/// if found; otherwise, -1.</returns>
			public int IndexOf(PropertySpec value)
			{
				return _innerArray.IndexOf(value);
			}

			/// <summary>
			/// Searches for the PropertySpec with the specified name and returns the zero-based index of
			/// the first occurrence within the entire PropertySpecCollection.
			/// </summary>
			/// <param name="name">The name of the PropertySpec to locate in the PropertySpecCollection.</param>
			/// <returns>The zero-based index of the first occurrence of value within the entire PropertySpecCollection,
			/// if found; otherwise, -1.</returns>
			public int IndexOf(string name)
			{
				int i = 0;

				foreach(PropertySpec spec in _innerArray)
				{
					if(spec.Name == name)
						return i;

					i++;
				}

				return -1;
			}

			/// <summary>
			/// Inserts a PropertySpec object into the PropertySpecCollection at the specified index.
			/// </summary>
			/// <param name="index">The zero-based index at which value should be inserted.</param>
			/// <param name="value">The PropertySpec to insert.</param>
			public void Insert(int index, PropertySpec value)
			{
				_innerArray.Insert(index, value);
			}

			/// <summary>
			/// Removes the first occurrence of a specific object from the PropertySpecCollection.
			/// </summary>
			/// <param name="obj">The PropertySpec to remove from the PropertySpecCollection.</param>
			public void Remove(PropertySpec obj)
			{
				_innerArray.Remove(obj);
			}

			/// <summary>
			/// Removes the property with the specified name from the PropertySpecCollection.
			/// </summary>
			/// <param name="name">The name of the PropertySpec to remove from the PropertySpecCollection.</param>
			public void Remove(string name)
			{
				int index = IndexOf(name);
				RemoveAt(index);
			}

			/// <summary>
			/// Removes the object at the specified index of the PropertySpecCollection.
			/// </summary>
			/// <param name="index">The zero-based index of the element to remove.</param>
			public void RemoveAt(int index)
			{
				_innerArray.RemoveAt(index);
			}

			/// <summary>
			/// Copies the elements of the PropertySpecCollection to a new PropertySpec array.
			/// </summary>
			/// <returns>A PropertySpec array containing copies of the elements of the PropertySpecCollection.</returns>
			public PropertySpec[] ToArray()
			{
				return _innerArray.ToArray();
			}

			#region Explicit interface implementations for ICollection and IList
			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			void ICollection.CopyTo(Array array, int index)
			{
				CopyTo((PropertySpec[])array, index);
			}

			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			int IList.Add(object value)
			{
				return Add((PropertySpec)value);
			}

			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			bool IList.Contains(object obj)
			{
				return Contains((PropertySpec)obj);
			}

			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			object IList.this[int index]
			{
				get
				{
					return ((PropertySpecCollection)this)[index];
				}
				set
				{
					((PropertySpecCollection)this)[index] = (PropertySpec)value;
				}
			}

			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			int IList.IndexOf(object obj)
			{
				return IndexOf((PropertySpec)obj);
			}

			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			void IList.Insert(int index, object value)
			{
				Insert(index, (PropertySpec)value);
			}

			/// <summary>
			/// This member supports the .NET Framework infrastructure and is not intended to be used directly from your code.
			/// </summary>
			void IList.Remove(object value)
			{
				Remove((PropertySpec)value);
			}
			#endregion
		}
		#endregion

	}
	
/********************************************************************
 *
 *  PropertyBag.cs
 *  --------------
 *  Copyright (C) 2002  Tony Allowatt
 *  Last Update: 12/14/2002
 * 
 *  THE SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS", WITHOUT WARRANTY
 *  OF ANY KIND, EXPRESS OR IMPLIED. IN NO EVENT SHALL THE AUTHOR BE
 *  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OF THIS
 *  SOFTWARE.
 * 
 *  Public types defined in this file:
 *  ----------------------------------
 *  namespace Flobbster.Windows.Forms
 *     class PropertySpec
 *     class PropertySpecEventArgs
 *     delegate PropertySpecEventHandler
 *     class PropertyBag
 *        class PropertyBag.PropertySpecCollection
 *     class PropertyTable
 *
 ********************************************************************/
	/// <summary>
    /// Provides data for the GetValue and SetValue events of the PropertyBag class.
    /// </summary>
    public class PropertySpecEventArgs : EventArgs
    {
        private PropertySpec property;
        private object val;

        /// <summary>
        /// Initializes a new instance of the PropertySpecEventArgs class.
        /// </summary>
        /// <param name="property">The PropertySpec that represents the property whose
        /// value is being requested or set.</param>
        /// <param name="val">The current value of the property.</param>
        public PropertySpecEventArgs(PropertySpec property, object val)
        {
            this.property = property;
            this.val = val;
        }

        /// <summary>
        /// Gets the PropertySpec that represents the property whose value is being
        /// requested or set.
        /// </summary>
        public PropertySpec Property
        {
            get { return property; }
        }

        /// <summary>
        /// Gets or sets the current value of the property.
        /// </summary>
        public object Value
        {
            get { return val; }
            set { val = value; }
        }
    }
	
    /// <summary>
    /// Represents the method that will handle the GetValue and SetValue events of the
    /// PropertyBag class.
    /// </summary>
    public delegate void PropertySpecEventHandler(object sender, PropertySpecEventArgs e);
    
    /// <summary>
    /// Represents a single property in a PropertySpec.
    /// </summary>
    public class PropertySpec
    {
        private PropertyFlags mAttributeFlags;
        private string category; // public vars, private vars, game properties, build properties
        private object defaultValue;
        private string description;
        private string editor; // eg Texture browser, Material
        private string name;
        private string type; // the "type" does store the AssemblyQualifiedName
        //private string assemblyQualifiedName;
        private string typeConverter;
        private string displayName;

        
        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The fully qualified name of the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The fully qualified name of the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, string type, string category, string description, object defaultValue,
            string editor, string typeConverter)
        {
        	
            this.name = name;
            this.displayName = name;
            this.type = type;
            this.category = category;
            if (string.IsNullOrEmpty (description) == false)
	            this.description = description;
    
            this.defaultValue = defaultValue;

            if (string.IsNullOrEmpty (editor) == false)
	            this.editor = editor;
            
            if (string.IsNullOrEmpty (typeConverter) == false)
            	this.typeConverter = typeConverter;
        
            // default attribute flags
            IsSerializable = true;
            IsReadOnly = false;
            IsBrowsable = true; 
        }
        
        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        public PropertySpec(string name, string type, string category, string description, object defaultValue)
        	:
            this(name, type, category, description, defaultValue, "", "")
        {

        }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        public PropertySpec() 
            : 
            this("", "", null, null, null) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        public PropertySpec(string name, string type) 
            : 
            this(name, type, null, null, null) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        public PropertySpec(string name, Type type)
            :
            this(name, type.AssemblyQualifiedName, null, null, null) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        public PropertySpec(string name, string type, string category) 
            : 
            this(name, type, category, null, null) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category"></param>
        public PropertySpec(string name, Type type, string category)
            :
            this(name, type.AssemblyQualifiedName, category, null, null) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        public PropertySpec(string name, string type, string category, string description)
            :
            this(name, type, category, description, null) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        public PropertySpec(string name, Type type, string category, string description)
            :
            this(name, type.AssemblyQualifiedName, category, description, null) 
        { }
        
        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        public PropertySpec(string name, string type, object defaultValue)
            :
            this(name, type, null, null, defaultValue) 
        { }
        
        public PropertySpec(string name, string type, string category, object defaultValue)
            :
            this(name, type, category, null, defaultValue)
        { }

        public PropertySpec(string name, Type type, string category, object defaultValue)
            :
            this(name, type.AssemblyQualifiedName, category, null, defaultValue) 
        { }


        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        public PropertySpec(string name, Type type, string category, string description, object defaultValue)
            :
            this(name, type.AssemblyQualifiedName, category, description, defaultValue) 
        { }

        public PropertySpec(string name, Type type, string category, object defaultValue, Type typeConverter)
            :
            this(name, type.AssemblyQualifiedName, category, null, defaultValue) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The fully qualified name of the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The fully qualified name of the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, Type type, string category, string description, object defaultValue,
            string editor, string typeConverter)
            :
            this(name, type.AssemblyQualifiedName, category, description, defaultValue, editor, typeConverter) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The Type that represents the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The fully qualified name of the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, string type, string category, string description, object defaultValue,
            Type editor, string typeConverter)
            :
            this(name, type, category, description, defaultValue, editor.AssemblyQualifiedName,
            typeConverter) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The Type that represents the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The fully qualified name of the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, Type type, string category, string description, object defaultValue,
            Type editor, string typeConverter)
            :
            this(name, type.AssemblyQualifiedName, category, description, defaultValue,
            editor.AssemblyQualifiedName, typeConverter) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The fully qualified name of the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The Type that represents the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, string type, string category, string description, object defaultValue,
            string editor, Type typeConverter)
            :
            this(name, type, category, description, defaultValue, editor, typeConverter.AssemblyQualifiedName) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The fully qualified name of the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The Type that represents the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, Type type, string category, string description, object defaultValue,
            string editor, Type typeConverter)
            :
            this(name, type.AssemblyQualifiedName, category, description, defaultValue, editor,
            typeConverter.AssemblyQualifiedName)
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">The fully qualified name of the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The Type that represents the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The Type that represents the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, string type, string category, string description, object defaultValue,
            Type editor, Type typeConverter)
            :
            this(name, type, category, description, defaultValue, editor.AssemblyQualifiedName,
            typeConverter.AssemblyQualifiedName) 
        { }

        /// <summary>
        /// Initializes a new instance of the PropertySpec class.
        /// </summary>
        /// <param name="name">The name of the property displayed in the property grid.</param>
        /// <param name="type">A Type that represents the type of the property.</param>
        /// <param name="category">The category under which the property is displayed in the
        /// property grid.</param>
        /// <param name="description">A string that is displayed in the help area of the
        /// property grid.</param>
        /// <param name="defaultValue">The default value of the property, or null if there is
        /// no default value.</param>
        /// <param name="editor">The Type that represents the type of the editor for this
        /// property.  This type must derive from UITypeEditor.</param>
        /// <param name="typeConverter">The Type that represents the type of the type
        /// converter for this property.  This type must derive from TypeConverter.</param>
        public PropertySpec(string name, Type type, string category, string description, object defaultValue,
            Type editor, Type typeConverter)
            :
            this(name, type.AssemblyQualifiedName, category, description, defaultValue,
            editor.AssemblyQualifiedName, typeConverter.AssemblyQualifiedName) 
        { }


            [Flags]
        enum PropertyFlags : uint
        {
        	None = 0,
        	ReadOnly = 1 << 0,
        	Serializable =1 << 1,
        	Browsable = 1 << 2
        }
        
		// bit 0 = readonly
		// bit 1 = serializable
		// bit 2 = browsable
        public bool IsReadOnly 
        {
        	get{ return (mAttributeFlags & PropertyFlags.ReadOnly) != 0;}
        	set 
        	{
        		if (value)
        			mAttributeFlags |= PropertyFlags.ReadOnly;
        		else
	        		mAttributeFlags &= ~PropertyFlags.ReadOnly;
        	}
        }
        
        public bool IsSerializable
        {
        	get{ return (mAttributeFlags & PropertyFlags.Serializable) != 0;}
        	set 
        	{
        		if (value)
        			mAttributeFlags |= PropertyFlags.Serializable;
        		else
	        		mAttributeFlags &= ~PropertyFlags.Serializable;
        	}
        }
                
        public bool IsBrowsable 
        {
        	get{ return (mAttributeFlags & PropertyFlags.Browsable) != 0;}
        	set 
        	{
        		if (value)
        			mAttributeFlags |= PropertyFlags.Browsable;
        		else
	        		mAttributeFlags &= ~PropertyFlags.Browsable;
        	}
        }
        
        public string DisplayName
        {
            get { return (string.IsNullOrEmpty(displayName) ? name : displayName); }
            set { displayName = value; }
        }
        /// <summary>
        /// Gets or sets the category name of this property.
        /// </summary>
        public string Category
        {
            get { return category; }
            set { category = value; }
        }

        /// <summary>
        /// Gets or sets the fully qualified name of the type converter
        /// type for this property.
        /// </summary>
        public string ConverterTypeName
        {
            get { return typeConverter; }
            set { typeConverter = value; }
        }

        /// <summary>
        /// Gets or sets the default value of this property.
        /// </summary>
        public object DefaultValue
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }

        /// <summary>
        /// Gets or sets the help text description of this property.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Gets or sets the fully qualified name of the editor type for
        /// this property.
        /// </summary>
        public string EditorTypeName
        {
            get { return editor; }
            set { editor = value; }
        }

        /// <summary>
        /// Gets or sets the name of this property.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Gets or sets the fully qualfied name of the type of this
        /// property.
        /// </summary>
        public string TypeName
        {
            get { return type; }
            set { type = value; }
        }
    }

	
	
	/*
	// MicroExpressionEvaluator (class MicroEx) is a neat little library!  Very fast and Very compact and easy to use with web compilers like DotNetFiddle since its just one
			// completely self contained class with no depenedancies that i can just paste into this single .cs script!
			// https://github.com/webermania/MicroExpressionEvaluator
			string logicalExpression = "false != true";
			//bool result = MicroEx.Evaluate(logicalExpression);
			//Console.WriteLine ("Do_Droid_Logic() - MicroEx.Evaluate() - '" + logicalExpression + "' " + result.ToString());

			// TODO: this is just referencing the ONE stationOperator memory<T> not allLivingEntities so we use [0] not [i]
			logicalExpression = 1.ToString() + " < " + 2.ToString();
			bool result = MicroEx.Evaluate(logicalExpression);
			//Console.WriteLine ("Do_Droid_Logic() - MicroEx.Evaluate() - '" + logicalExpression + "' " + result.ToString());
	*/	
	
	// https://github.com/webermania/MicroExpressionEvaluator
	// Apache 2.0 license  // todo: include
	public static class MicroEx
    {
        public static StringComparison StringComparison { get; set; } = StringComparison.Ordinal;

        /// <summary>
        /// interprets and evaluates logic expressions represented as string
        /// </summary>
        /// <param name="expression">Input expression such as "(\"text123\" == \"text123\") && (7 <= 8)"</param>
        /// <returns>returns evaluation success as bool or throws Exception with clear a Error message
        public static bool Evaluate(string expression)
        {
            return string.IsNullOrWhiteSpace(expression)
                ? throw new Exception("Invalid input! Empty expression.")
                : SimplifyAndSolveExpression(expression);
        }

        private static bool ContainsAnyOperators(string expr)
        {
            return new[] { "||", "&&", "!=", "==", "<=", ">=", "<", ">" }.Any(expr.Contains);
        }

        /// <summary>
        ///     Solves nested groups from the ((((inside)))) out
        /// </summary>
        private static bool SimplifyAndSolveExpression(string expr)
        {
            bool containsOpenBracket = expr.Contains('(');
            bool containsCloseBracket = expr.Contains(')');

            if (containsOpenBracket && !containsCloseBracket)
            {
                throw new Exception($"Invalid input:'{expr}'! ) expected.");
            }

            if (!containsOpenBracket && containsCloseBracket)
            {
                throw new Exception($"Invalid input:'{expr}'! ( expected.");
            }

            if (containsOpenBracket)
            {
                string[] potentialGroups = expr.Split(new string[] { "(" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string potentialGroupX in potentialGroups)
                {
                    if (potentialGroupX.StartsWith(")") && potentialGroupX.Length > 1)
                    {
                        throw new Exception($"Invalid input:'{potentialGroupX}'! ( expected.");
                    }

                    if (potentialGroupX.StartsWith(")"))
                    {
                        throw new Exception("Invalid input! Group has no value ().");
                    }

                    int nextOpenBracket = potentialGroupX.IndexOf('(');
                    int nextCloseBracket = potentialGroupX.IndexOf(')');

                    if (nextCloseBracket == -1 || (nextOpenBracket != -1 && nextOpenBracket < nextCloseBracket))
                    {
                        continue;
                    }

                    // found a (group) that has (no (deeper nested) group)
                    string subExpr = potentialGroupX.Substring(0, nextCloseBracket);
                    bool subResult = SplitAndValidateLogicalOperators(subExpr);

                    string simplifiedExpr = expr.Replace($"({subExpr})", subResult.ToString().ToLower());
                    return SimplifyAndSolveExpression(simplifiedExpr);
                }
            }

            return SplitAndValidateLogicalOperators(expr);
        }

        /// <summary>
        ///     Fast and simple way to split into only two pieces
        /// </summary>
        /// <param name="source">string to be split</param>
        /// <param name="separator">what to split by</param>
        /// <returns>string array of result(s)</returns>
        private static string[] SplitOnce(string source, string separator)
        {
            if (string.IsNullOrEmpty(source))
            {
                return Array.Empty<string>();
            }

            if (string.IsNullOrEmpty(separator))
            {
                return new string[] { source.Trim() };
            }

            int position = source.IndexOf(separator, StringComparison.Ordinal);
            if (position == -1)
            {
                return new string[] { source.Trim() };
            }

            string before = source.Substring(0, position);
            string after = source.Substring(position + separator.Length);

            return new string[] { before.Trim(), after.Trim() };
        }

        /// <summary>
        ///     Splits the problem respecting the correct operator precedence.
        ///     (Testsed against C# implementation.)
        /// </summary>
        private static bool SplitAndValidateLogicalOperators(string expr)
        {
            // ||
            string[] op1 = SplitOnce(expr, "||");
            if (op1.Length > 1)
            {
                return op1.Any(SimplifyAndSolveExpression);
            }

            // &&
            string[] op2 = SplitOnce(expr, "&&");
            if (op2.Length > 1)
            {
                return op2.All(SimplifyAndSolveExpression);
            }

            // !=
            string[] op3 = SplitOnce(expr, "!=");
            if (op3.Length > 1)
            {
                bool part1GoesDeeper = ContainsAnyOperators(op3[0]);
                bool part2GoesDeeper = ContainsAnyOperators(op3[1]);

                if (part1GoesDeeper || part2GoesDeeper)
                {
                    bool resultPart1 = part1GoesDeeper ? SimplifyAndSolveExpression(op3[0]) : ValidateBool(op3[0]);
                    bool resultPart2 = part2GoesDeeper ? SimplifyAndSolveExpression(op3[1]) : ValidateBool(op3[1]);

                    return resultPart1 != resultPart2;
                }

                return !ValidateEquality(op3[0], op3[1]);
            }

            // ==
            string[] op4 = SplitOnce(expr, "==");
            if (op4.Length > 1)
            {
                bool part1GoesDeeper = ContainsAnyOperators(op4[0]);
                bool part2GoesDeeper = ContainsAnyOperators(op4[1]);

                if (part1GoesDeeper || part2GoesDeeper)
                {
                    bool resultPart1 = part1GoesDeeper ? SimplifyAndSolveExpression(op4[0]) : ValidateBool(op4[0]);
                    bool resultPart2 = part2GoesDeeper ? SimplifyAndSolveExpression(op4[1]) : ValidateBool(op4[1]);

                    return resultPart1 == resultPart2;
                }

                return ValidateEquality(op4[0], op4[1]);
            }

            // >=
            string[] op5 = SplitOnce(expr, ">=");
            if (op5.Length > 1)
            {
                bool part1GoesDeeper = ContainsAnyOperators(op5[0]);
                bool part2GoesDeeper = ContainsAnyOperators(op5[1]);

                return part1GoesDeeper || part2GoesDeeper
                    ? throw new Exception(
                        $"Invalid input:'{expr}'! Operator '>=' cannot be applied to operands of type 'bool' and 'object'.")
                    : Convert.ToDecimal(op5[0]) >= Convert.ToDecimal(op5[1]);
            }

            // <=
            string[] op6 = SplitOnce(expr, "<=");
            if (op6.Length > 1)
            {
                bool part1GoesDeeper = ContainsAnyOperators(op6[0]);
                bool part2GoesDeeper = ContainsAnyOperators(op6[1]);

                return part1GoesDeeper || part2GoesDeeper
                    ? throw new Exception(
                        $"Invalid input:'{expr}'! Operator '<=' cannot be applied to operands of type 'bool' and 'unknown object'.")
                    : Convert.ToDecimal(op6[0]) <= Convert.ToDecimal(op6[1]);
            }

            // >
            string[] op7 = SplitOnce(expr, ">");
            if (op7.Length > 1)
            {
                bool part1GoesDeeper = ContainsAnyOperators(op7[0]);
                bool part2GoesDeeper = ContainsAnyOperators(op7[1]);

                return part1GoesDeeper || part2GoesDeeper
                    ? throw new Exception(
                        $"Invalid input:'{expr}'! Operator '>' cannot be applied to operands of type 'bool' and 'unknown object'.")
                    : Convert.ToDecimal(op7[0]) > Convert.ToDecimal(op7[1]);
            }

            // <
            string[] op8 = SplitOnce(expr, "<");
            if (op8.Length > 1)
            {
                bool part1GoesDeeper = ContainsAnyOperators(op8[0]);
                bool part2GoesDeeper = ContainsAnyOperators(op8[1]);

                return part1GoesDeeper || part2GoesDeeper
                    ? throw new Exception(
                        $"Invalid input:'{expr}'! Operator '<' cannot be applied to operands of type 'bool' and 'unknown object'.")
                    : Convert.ToDecimal(op8[0]) < Convert.ToDecimal(op8[1]);
            }

            return ValidateBool(expr);
        }

        private static bool ValidateBool(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                throw new ArgumentException("Input string is null or empty.");
            }

            val = val.Trim();

            if (val.Length == 0)
            {
                throw new ArgumentException("Input string is only whitespace.");
            }

            if (val[0] == '!')
            {
                return !ValidateBool(val.Substring(1));
            }

            // Using StringComparison.OrdinalIgnoreCase for case-insensitive comparison
            return val.Equals("true", StringComparison.OrdinalIgnoreCase) || (val.Equals("false", StringComparison.OrdinalIgnoreCase)
                    ? false
                    : throw new ArgumentException($"String '{val}' was not recognized as a valid Boolean."));
        }

        private static bool ValidateEquality(string val1, string val2)
        {
            val1 = val1.Trim();
            val2 = val2.Trim();

            bool val1HasStringFlag = val1.StartsWith("\"") && val1.EndsWith("\"");
            bool val2HasStringFlag = val2.StartsWith("\"") && val2.EndsWith("\"");

            if (val1HasStringFlag != val2HasStringFlag)
            {
                throw new Exception(
                    $"Invalid input i1:'{val1}' i2:'{val1}'! Operator cannot be applied to operands of type 'string' and 'unknown object'.");
            }

            if (val1HasStringFlag)
            {
                return val1.Equals(val2, StringComparison);
            }

            bool val1IsDec = decimal.TryParse(val1, out decimal val1Dec);
            bool val2IsDec = decimal.TryParse(val2, out decimal val2Dec);

            return val1IsDec != val2IsDec
                ? throw new Exception(
                    $"Invalid input i1:'{val1}' i2:'{val1}'! Operator cannot be applied to operands of type 'decimal' and 'unknown object'.")
                : val1IsDec ? val1Dec == val2Dec : ValidateBool(val1) == ValidateBool(val2);
        }
    }

/*
	// NOTE: The below code I only put through an initial pass of removing Unity3d specific attributes, variable types, and such...
	//       There's still some more that needs to be fixed, but
#region PID Controller including one for a Turret https://github.com/vazgriz/PID_Controller/blob/master/Assets/Scripts/Horizontal.cs
	
	public abstract class Controller  {
		public abstract PIDController GetController();
		public abstract void SetTarget(int index);
		public abstract float Power { get; set; }
	}


	public class SinMover  {
		float amplitude;
		float frequency;

		new Transform transform;
		Vector3d startPosition;

		void Start() {
			transform = GetComponent<Transform>();
			startPosition = transform.position;
		}

		void Update() {
			transform.position = startPosition + new Vector3d(Math.Sin(Time.time * frequency) * amplitude, 0, 0);
		}
	}



	public class PIDController {
		public enum DerivativeMeasurement {
			Velocity,
			ErrorRateOfChange
		}

		//PID coefficients
		public float proportionalGain;
		public float integralGain;
		public float derivativeGain;

		public float outputMin = -1;
		public float outputMax = 1;
		public float integralSaturation;
		public DerivativeMeasurement derivativeMeasurement;

		public float valueLast;
		public float errorLast;
		public float integrationStored;
		public float velocity;  //only used for the info display
		public bool derivativeInitialized;

		public void Reset() {
			derivativeInitialized = false;
		}

		public float Update(float dt, float currentValue, float targetValue) {
			if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));

			float error = targetValue - currentValue;

			//calculate P term
			float P = proportionalGain * error;

			
			//calculate I term
			integrationStored = Math.Clamp(integrationStored + (error * dt), -integralSaturation, integralSaturation);
			float I = integralGain * integrationStored;

			//calculate both D terms
			float errorRateOfChange = (error - errorLast) / dt;
			errorLast = error;

			float valueRateOfChange = (currentValue - valueLast) / dt;
			valueLast = currentValue;
			velocity = valueRateOfChange;

			//choose D term to use
			float deriveMeasure = 0;

			if (derivativeInitialized) {
				if (derivativeMeasurement == DerivativeMeasurement.Velocity) {
					deriveMeasure = -valueRateOfChange;
				} else {
					deriveMeasure = errorRateOfChange;
				}
			} else {
				derivativeInitialized = true;
			}

			float D = derivativeGain * deriveMeasure;

			float result = P + I + D;

			return Math.Clamp(result, outputMin, outputMax);
		}

		float AngleDifference(float a, float b) {
			return (a - b + 540) % 360 - 180;   //calculate modular difference, and remap to [-180, 180]
		}

		public float UpdateAngle(float dt, float currentAngle, float targetAngle) {
			if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));
			float error = AngleDifference(targetAngle, currentAngle);

			//calculate P term
			float P = proportionalGain * error;

			//calculate I term
			integrationStored = Math.Clamp(integrationStored + (error * dt), -integralSaturation, integralSaturation);
			float I = integralGain * integrationStored;

			//calculate both D terms
			float errorRateOfChange = AngleDifference(error, errorLast) / dt;
			errorLast = error;

			float valueRateOfChange = AngleDifference(currentAngle, valueLast) / dt;
			valueLast = currentAngle;
			velocity = valueRateOfChange;

			//choose D term to use
			float deriveMeasure = 0;

			if (derivativeInitialized) {
				if (derivativeMeasurement == DerivativeMeasurement.Velocity) {
					deriveMeasure = -valueRateOfChange;
				} else {
					deriveMeasure = errorRateOfChange;
				}
			} else {
				derivativeInitialized = true;
			}

			float D = derivativeGain * deriveMeasure;

			float result = P + I + D;

			return Math.Clamp(result, outputMin, outputMax);
		}
	}

	public class Turret : Controller {

		PIDController controller;
		float power;
		Transform target;

		new Rigidbody rigidbody;

		public override float Power {
			get {
				return power;
			}
			set {
				power = value;
			}
		}

		void Start() {
			rigidbody = GetComponent<Rigidbody>();
		}

		public override PIDController GetController() {
			return controller;
		}

		public override void SetTarget(int index) {
		}

		void FixedUpdate() {
			var targetPosition = target.position;
			targetPosition.y = rigidbody.position.y;    //ignore difference in Y
			var targetDir = (targetPosition - rigidbody.position).normalized;
			var forwardDir = rigidbody.rotation * Vector3d.Forward();

			var currentAngle = Vector3d.SignedAngle(Vector3d.Forward(), forwardDir, Vector3d.Up());
			var targetAngle = Vector3d.SignedAngle(Vector3d.Forward(), targetDir, Vector3d.Up());

			float input = controller.UpdateAngle(Time.fixedDeltaTime, currentAngle, targetAngle);
			rigidbody.AddTorque(new Vector3d(0, input * power, 0));
		}
	}


	public class Horizontal : Controller {

		PIDController controller;
		float power;
		Transform[] targets;
		GameObject flameRight;
		GameObject flameLeft;
		float flameSize;

		new Rigidbody rigidbody;
		List<Vector3d> targetPositions;
		Vector3d targetPosition;

		public override float Power {
			get {
				return power;
			}
			set {
				power = value;
			}
		}

		void Start() {
			rigidbody = GetComponent<Rigidbody>();

			targetPositions = new List<Vector3d>();
			foreach (var target in targets) {
				targetPositions.Add(target.position);
			}
		}

		public override PIDController GetController() {
			return controller;
		}

		public override void SetTarget(int index) {
			targetPosition = targetPositions[index];
		}

		void SetScale(GameObject go, float scale) {
			scale = Math.Clamp(scale, 0, 1);

			if (scale < 0.1f) {
				go.SetActive(false);
			} else {
				go.SetActive(true);
				go.GetComponent<Transform>().localScale = new Vector3d(scale, scale, scale) * flameSize;
			}
		}

		void FixedUpdate() {
			float throttle = controller.Update(Time.fixedDeltaTime, rigidbody.position.x, targetPosition.x);
			rigidbody.AddForce(new Vector3d(throttle * power, 0, 0));

			SetScale(flameRight, -throttle);
			SetScale(flameLeft, throttle);
		}
	}

	public class Vertical : Controller {
		PIDController controller;
		float power;
		Transform[] targets;
		GameObject flame;
		float flameSize;

		new Rigidbody rigidbody;
		List<Vector3d> targetPositions;
		Vector3d targetPosition;

		public override float Power {
			get {
				return power;
			}
			set {
				power = value;
			}
		}

		void Start() {
			rigidbody = GetComponent<Rigidbody>();

			targetPositions = new List<Vector3d>();
			foreach (var target in targets) {
				targetPositions.Add(target.position);
			}

			SetTarget(0);
		}

		public override PIDController GetController() {
			return controller;
		}

		public override void SetTarget(int index) {
			targetPosition = targetPositions[index];
		}

		void SetScale(GameObject go, float scale) {
			scale = Mathf.Clamp(scale, 0, 1);

			if (scale < 0.1f) {
				go.SetActive(false);
			} else {
				go.SetActive(true);
				go.GetComponent<Transform>().localScale = new Vector3d(scale, scale, scale) * flameSize;
			}
		}

		void FixedUpdate() {
			float throttle = controller.Update(Time.fixedDeltaTime, rigidbody.position.y, targetPosition.y);
			rigidbody.AddForce(new Vector3d(0, throttle * power, 0));

			SetScale(flame, throttle);
		}
	}
   #endregion  */

}