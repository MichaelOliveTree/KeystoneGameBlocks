#define CACHE_VERTICES
#define USE_STRUCT // instead of classes for Quaternion and Matrix
#define USE_MEMORY_T
#define SPATIAL_SEARCH

using System.Diagnostics;
using System;
//using System.Memory;
using System.Reflection; // used for "MethodBase" type in Profiler
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


// NOTE: The primary purpose of this is to demonstrate the use of Memory<T> 
//             to increase performance by updating Entities using a data processing model
//             in order to take advantage of cache coherency instead of 
//             the typical Entity.Update() model which does not.  
// NOTE:  We will also be able to experiment with writing DETERMINISTIC code and 
//              being able to STEP forward and BACKWARDS through the simulation and
//              ultimately even being able to REPLAY a "recording" of the simulation  or parts
//             of it.
namespace HelloBoids
{
	// https://vscode.dev/github/MichaelOliveTree/KeystoneGameBlocks
    public class EntryClass
    {
        public static HelloBoids.ComponentStoreCollection mCStoreCol = new HelloBoids.ComponentStoreCollection();
        public static uint NUM_ENTRIES = 768;
        public static uint NUM_ITERATIONS = 500;
        public static double WIDTH = 800d;
        public static double HEIGHT = 600d;

        public static Profiler CodeProfiler;

        //private static object mTestSpan;
        // private Span<T> mTest;
		private static bool mIsRunning;
		private static object mSyncLock;
		
        public static void Main()
        {
			mSyncLock = new object();
			mIsRunning = true;
			
			System.Threading.Thread renderThread = new System.Threading.Thread(RenderLoop);
			Console.WriteLine("Render thread created.");
			
			System.Threading.Thread animationThread = new System.Threading.Thread(AnimationLoop);
			Console.WriteLine("Animation thread created.");
			
            System.Threading.Thread gameThread = new System.Threading.Thread(GameLoop);
			Console.WriteLine("Game loop thread created.");
			string output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);
			
        	// Set as background so the application can exit when the main thread ends
			// TODO: this may not be necessary if I just set the exit condition to the known
			// number of iterations that will be performed so the sentinel "mIsRunning" can be
			// set to = false;
        	gameThread.IsBackground = true; 
        	gameThread.Start();
			
			// LOOP USING CLASSES
			Console.WriteLine("");
			Console.WriteLine("Class Performance Test started in a separate thread.");
			Console.WriteLine ("");
			
			// The main thread waits for user input to stop the application
			while (true)
			{
				if (Console.ReadKey(true).Key == ConsoleKey.Q)
				{
					lock (mSyncLock)
					{
						mIsRunning = false;
					}
					break; // Exit the main thread's loop
				}
			}

			// Ensure the game thread has time to stop gracefully (optional for background threads)
			gameThread.Join();
			
			Console.WriteLine("Class Performance Test completed.");
			///////////////////////////////////////////////////////////////////////////////////////////////
			
			
			// LOOP USING MEMORY<T>
			gameThread = new System.Threading.Thread(GameLoop);
        	// Set as background so the application can exit when the main thread ends
			// TODO: this may not be necessary if I just set the exit condition to the known
			// number of iterations that will be performed so the sentinel "mIsRunning" can be
			// set to = false;
        	gameThread.IsBackground = true; 
        	gameThread.Start();
			
			Console.WriteLine("Memory<T> Performance Test started in a separate thread.");
			Console.WriteLine ("");
			
			// The main thread waits for user input to stop the application
			while (true)
			{
				if (Console.ReadKey(true).Key == ConsoleKey.Q)
				{
					lock (mSyncLock)
					{
						mIsRunning = false;
					}
					break; // Exit the main thread's loop
				}
			}

			gameThread.Join();
			
			Console.WriteLine("Memory<T> Performance Test completed.");
		}

	
		// simulation updates
		private static void GameLoop()
		{
			
			/*
			Stopwatch stopwatch = Stopwatch.StartNew();
			double lastTime = stopwatch.Elapsed.TotalMilliseconds;

			while (true)
			{
				bool runningStatus;
				lock (_lockObject)
				{
					runningStatus = _isRunning;
				}

				if (!runningStatus)
					break; // Exit the game loop

				// Calculate delta time (time since last frame)
				double currentTime = stopwatch.Elapsed.TotalMilliseconds;
				double deltaTime = currentTime - lastTime;
				lastTime = currentTime;

				// Update and Render operations
				Update(deltaTime);
				Render();

				// Simple throttling to prevent maxing out the CPU (adjust as needed)
				Thread.Sleep(15); 
			}
			*/
			
			
			
			bool useOctree = false;
            string MODE = "Memory<T>";
            bool structs = false;

			long mCurrentFrame = 0;
			
			double targetRateFramesPerSecond = 60d;
			double step = 1d / targetRateFramesPerSecond; // aka dt or "deltaTime"

            // step == 1 results targetRateFramesPerSecond of 0.0333 seconds
            // 33.33 milliseconds per frame

			
#if SPATIAL_SEARCH
            useOctree = true;
#endif
            // how do we profile in other classes if the profiler is not static in Core?
            CodeProfiler = new Profiler();
            CodeProfiler.ProfilerEnabled = true;
            CodeProfiler.ShowFramesPerSecond = true;
            CodeProfiler.CategorizeByTypename = true;
            CodeProfiler.Verbose = true;
            // CodeProfiler.FullyQualifiedTypename = true

            // NOTE: since profiler is global, we register these vars once no matter how many viewports are open
            // Registers profiles before we use them
            int categoryIndex = 0;

            // mouse/keyboard	  
            //categoryIndex ++;
            //string ioCategory = categoryIndex.ToString() + " - I/O";		
            //CodeProfiler.Register("View Behavior", ioCategory);	        

            // networking
            //categoryIndex ++;
            //string networkingCategory = categoryIndex.ToString() + " - Networking";
            //CodeProfiler.Register("TBD", networkingCategory);

            // physics
            //categoryIndex ++;
            //string phsyicsCategory = categoryIndex.ToString() + " - Physics";
            //CodeProfiler.Register("Gravity", phsyicsCategory);
            //CodeProfiler.Register("Production", phsyicsCategory);
            //CodeProfiler.Register("Consumption", phsyicsCategory);

            // processing
            categoryIndex++;
            string scriptingCategory = null;// categoryIndex.ToString() + " - Process";
            CodeProfiler.Register("AssignSpan", scriptingCategory);
            CodeProfiler.Register("Process", scriptingCategory);
            CodeProfiler.Register("GetNeighbors", scriptingCategory);

            /*	
            // culling  
            categoryIndex ++;
            string cullingCategory = categoryIndex.ToString() + " - Culling";
            CodeProfiler.Register("Bucket Insertion", cullingCategory);	
            CodeProfiler.Register("Bucket Creation", cullingCategory);	
            CodeProfiler.Register("GetCameraSpaceBox", cullingCategory);
            CodeProfiler.Register("IntersectTest", cullingCategory);	
            CodeProfiler.Register ("Model Selection", cullingCategory);
            CodeProfiler.Register("VisibleItem Creation", cullingCategory);
            CodeProfiler.Register("Add Model To PVS", cullingCategory);	
            */


            BoidSimulation bSim = new BoidSimulation((int)NUM_ENTRIES, WIDTH, HEIGHT, useOctree);
            // Note: the larger the various distance values below,
            // the more cpu cycles needed. Tweak these values
            // to find a good balance between performance and
            // simulation/behavior quality
            bSim.SeparationDistance = 20;
            bSim.SeparationFactor = 0.5;
            bSim.AlignmentDistance = 20;
            bSim.AlignmentFactor = 0.2;
            bSim.CohesionDistance = 25;
            bSim.CohesionFactor = 0.1;
            bSim.MaxSpeed = 5;
            bSim.TurnFactor = 0.1; // For boundary avoidance


            CodeProfiler.StartLoop();

            string output = "Hello Boids - " + Utils.GetTimeString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);


#if USE_MEMORY_T == false
            MODE = "Classes";
#endif
            output = "MODE = " + MODE;
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "USE OCTREE == " + (bSim.Octree != null).ToString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

#if USE_STRUCT
            structs = true;
#endif
            output = "USE STRUCTS INSTEAD OF CLASSES FOR Quaternion and Matrix == " + structs.ToString();
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);
            output = "";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            Stopwatch stopWatch = new Stopwatch();

            // NOTE: the entire UserDataStore get's passed to our various "data processors."
            //mUserDataStore = new UserDataStore();

            // TODO: checkout of UserData needs to occur when the Entity is created?
            //       Not all Entities need it though? Hrm.  
            //UserData data = mUserDataStore.CheckOut(entityID);
            //entity.Data = data;

            object parameters = new object();

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
            output = "PERFORMANCE TEST RUNNING - " + NUM_ENTRIES.ToString() + " boids @ " + NUM_ITERATIONS.ToString() + " iterations.";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "Please Wait...";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            output = "";
            Console.WriteLine(output);
            Debug.WriteLine(output);



#if USE_MEMORY_T == false
            // TEST CLASSES
            // =====================

            stopWatch.Start();
			
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
			
            for (int i = 0; i < NUM_ITERATIONS; i++)
                using (EntryClass.CodeProfiler.HookUp("Process"))
                {
                    bSim.Update(step, bSim.Boids, bSim.SeparationDistance, bSim.SeparationFactor, bSim.AlignmentDistance, bSim.AlignmentFactor, bSim.CohesionDistance, bSim.CohesionFactor, bSim.MaxSpeed, bSim.TurnFactor);
                	mCurrentFrame++;
				}

            stopWatch.Stop();

            TimeSpan timeSpan = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTimeString = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds,
                timeSpan.Milliseconds / 10);
#else

            // TEST MEMORY<T>
            // ====================

            BoidSimulation.Store = EntryClass.mCStoreCol.CheckOut<Transform.Transform_Struct>(NUM_ENTRIES);
            //ComponentStore<Transform.Transform_Struct> store = EntryClass.mCStoreCol.CheckOut<Transform.Transform_Struct>(NUM_ENTRIES);
            //Span<Transform.Transform_Struct> mem = store.Span;

			mCurrentFrame = 0;
            stopWatch.Reset();
            stopWatch.Start();

            //mTestSpan = store.Span;
            //Span<Transform.Transform_Struct> castTest = (Span<Transform.Transform_Struct>) mTestSpan;

            for (int i = 0; i < NUM_ITERATIONS; i++)
                using (EntryClass.CodeProfiler.HookUp("Process"))
                {
                    //bSim.ProcessStep(step, NUM_ENTRIES, BoidSimulation.Store, bSim.SeparationDistance, bSim.SeparationFactor, bSim.AlignmentDistance, bSim.AlignmentFactor, bSim.CohesionDistance, bSim.CohesionFactor, bSim.MaxSpeed, bSim.TurnFactor);
                    bSim.ProcessStep(step, NUM_ENTRIES, bSim.SeparationDistance, bSim.SeparationFactor, bSim.AlignmentDistance, bSim.AlignmentFactor, bSim.CohesionDistance, bSim.CohesionFactor, bSim.MaxSpeed, bSim.TurnFactor);
					mCurrentFrame++;
                    // p.Invoke(store, parameters, r);
                }
			
            stopWatch.Stop();
            TimeSpan timeSpan = stopWatch.Elapsed;

            string elapsedTimeString = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                 timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds,
                 timeSpan.Milliseconds / 10);

#endif
            CodeProfiler.EndLoop();

            // regardless of the NUM_ITERATIONS used, the FPS should typically all be very close 
            // if the NUM_ENTRIES stays the same
            double fps = NUM_ITERATIONS / timeSpan.TotalSeconds;

            output = "PERFORMANCE TEST COMPLETED - " + Utils.GetTimeString();
            System.Diagnostics.Debug.WriteLine(output);
            Console.WriteLine(output);

            // LOG THE RESULTS of both runs - classes and memory<T> 
            // =====================
            output = "RunTime Mode: " + MODE + " - " + elapsedTimeString;
            Console.WriteLine(output);
            Debug.WriteLine(output);
            output = "FPS = " + fps.ToString();
            Console.WriteLine(output);
            Debug.WriteLine(output);


            CodeProfiler.OutputToConsole();


            output = "____________________________________________";
            Console.WriteLine(output);
            Debug.WriteLine(output);
            output = "";
            Console.WriteLine(output);
            Debug.WriteLine(output);

            // Validate() is only used to make sure the compiler doesn't take a huge shortcut
            // and not run the test code at all because none of the TestClass or TestStruct 
            // members are ever used.
            //     Validate(store, classes);


            //DataProcessors.Processor p = TestIntrinsicProcessor;

            //mIntrinsicProcessors = new KeyCommon.Processors.DataProcessors();
            //mIntrinsicProcessors.Add("STEER", p);
            // then -> p[i].Invoke(store, parameters, r);

            //mRulesProcessors = game.RulesProcessors;

            //Update(0.01f);
            // TODO:implement a https://en.wikipedia.org/wiki/K-d_tree
            //       so that each boid only needs to check against close boids and not all
            // TODO: 

            Console.WriteLine("Goodbye Boids!");
			mIsRunning = false;
		}
		
		private static void UpdateLoop()
		{
			// TODO: Insert game logic (physics, AI, input processing from a queue)
        	// Use lock when accessing shared data
		}
		
		private static void RenderLoop()
		{
			// TODO: Insert console drawing logic
			// Use lock when writing to the console to avoid conflicts
			lock (mSyncLock)
			{
				Console.SetCursorPosition(0, 1);
				Console.WriteLine($"Game running. Time: {DateTime.Now}");
			}
		}
		
		
		private static void AnimationLoop()
		{
			
		}
    }


	
    // TODO: THE SAMPLE FROM GITHUB https://github.com/swharden/Csharp-Data-Visualization/blob/main/website/content/simulations/boids/index.md
    // and simply uses System.Drawing to draw the boids.  I will want to just use a simple 3d pyramid type boid .obj instead.
    //https://github.com/swharden/Csharp-Data-Visualization/blob/main/website/content/simulations/boids/index.md
    public class BoidSimulation
    {
        public List<Boid> Boids { get; set; }
        public int Seed { get; set; } = 123;

        public double SeparationDistance { get; set; } = 20;
        public double SeparationFactor { get; set; } = 0.5;
        public double AlignmentDistance { get; set; } = 50;
        public double AlignmentFactor { get; set; } = 0.2;
        public double CohesionDistance { get; set; } = 50;
        public double CohesionFactor { get; set; } = 0.1;
        public double MaxSpeed { get; set; } = 5;
        public double TurnFactor { get; set; } = 0.1; // For boundary avoidance

        public OctreeOctant Octree { get; }

#if USE_MEMORY_T
        public static ComponentStore<Transform.Transform_Struct> Store;
#endif

        public BoidSimulation(int numBoids, double width, double height, bool useOctree = false)
        {
            Boids = new List<Boid>(); //NOTE: we do not preallocate the list here

            if (useOctree)
            {
                Vector3d min, max;
                min.x = width - width * 0.5d;
                min.y = height - height * 0.5d;
                min.z = 0d;
                max.x = width + width * 0.5d;
                max.y = height + height * 0.5d;
                max.z = 0d;

                OctreeOctant parent = null; // root has no parent
                BoundingBox box = new BoundingBox(min, max);
				uint octreeMaxDepth = 8;
			 	OctreeOctant.MaxDepth = octreeMaxDepth;
                Octree = new OctreeOctant(0, 0, box, parent);
            }

            //System.Numerics.BigInteger bint = 0;
            decimal bint = 0;

            Random rand = new Random(Seed);
            for (int i = 0; i < numBoids; i++)
            {
                // fragment the memory to account for fact that our
                // Boid objects wont be instantiated contiguously like this in production code
                const int FRAGMENTED_OBJ_SIZE = 128;

                // todo: the above doesn't make a diff, but perhaps
                // if i added dummy objects into the array instead..?
                object[] tmp = MemoryFragmenter.CreateAndFreeObjects(FRAGMENTED_OBJ_SIZE);
                for (int j = 0; j < tmp.Length; j++)
                    bint += tmp[j].GetHashCode();


                const int NUM_TO_PIN = 0;
                if (NUM_TO_PIN > 0)
                    MemoryFragmenter.Fragment(NUM_TO_PIN, 512, NUM_TO_PIN / 2, 128);

                double posX = rand.NextDouble() * width;
                double posY = rand.NextDouble() * height;
                double vX = (rand.NextDouble() - 0.5d) * 2d;
                double vY = (rand.NextDouble() - 0.5d) * 2d;


                Boid b = new Boid(i, posX, posY, vX, vY);
                Boids.Add(b);

                if (useOctree)
                {
                    Octree.Add((EntityNode)b);
                }

                if (NUM_TO_PIN > 0)
                    MemoryFragmenter.Cleanup();


            }
            Console.WriteLine(numBoids + " Boids Created.  Big Hash = " + bint.ToString());
        }

        // large amounts of data 
        // Boid.FindNeighbors(allBoids, largestDistance, i, ff);

        private List<int> GetNeighbors(Boid currentBoid, double largestDistance, double largestDistanceSquared)
        {
            List<int> neighbors;

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

            // SPATIAL SEARCH
#if (SPATIAL_SEARCH)
            {
                BoundingBox searchArea = new BoundingBox(currentBoid.Translation, largestDistance);

                Func<EntityNode, EntityNode, bool> match = (neighbor, current) =>
                {
                    if (neighbor == current) return false;
                    // TODO: WE MUST CACHE span<T> and not access neighbor.Translation and current.Translation... we need to directly
                    //        access the indices of the Span<T> here... otherwise its TOO SLOW
                    if (Vector3d.GetDistance3dSquared(neighbor.Translation, current.Translation) <= largestDistanceSquared) return true;
                    return false;
                };

                List<EntityNode> found = currentBoid.SpatialNode.Query(currentBoid, true, searchArea, match);
                if (found == null || found.Count == 0) return null;

                neighbors = new List<int>(found.Count);
                for (int j = 0; j < found.Count; j++)
                {
                    neighbors.Add(found[j].Index);
                }
            }
#else      // NEIGHBOR SEARCH USING DISTANCE CHECK                       
            {
                
	#if USE_MEMORY_T
					//neighbors = Boid.FindNeighbors(store, numBoids, largestDistance, currentIndex, findNeighborsFunc);
					neighbors = Boid.FindNeighbors(this.Boids, largestDistanceSquared, currentBoid.Index, ff);
	#else
		#if USE_MEMORY_T == false
						List<Boid> found = Boid.FindNeighbors(this.Boids, largestDistance, currentBoid.Index, ff);

						if (found == null || found.Count == 0) return null;
						neighbors = new List<int>(found.Count);
						for (int j = 0; j < found.Count; j++)
						{
							neighbors.Add(found[j].Index);
						}
		#else // DO NOT USE A NEIGHBOR FINDING FUNCTION, JUST BRUTE FORCE ALL BOIDS
						{

							// WARNING: iterating through ALL boids
							// for each CURRENT boid is O(n^2) and is too expensive
							// neighors = allBoids;
						}
		#endif
	#endif
#endif
            // END NEIGHBOR SEARCH
            return neighbors;
        }


#if USE_MEMORY_T == false
        public void Update(double elapsedSeconds, List<Boid> allBoids, double separationDistance, double separationFactor, double alignmentDistance, double alignmentFactor, double cohesionDistance, double cohesionFactor, double maxSpeed, double turnFactor)
        {
            double largestDistance = System.Math.Max(this.SeparationDistance, this.AlignmentDistance);
            largestDistance = System.Math.Max(largestDistance, this.CohesionDistance);
            double largestDistanceSquared = largestDistance * largestDistance;

            for (int i = 0; i < Boids.Count; i++)
            {
                List<int> found;
                List<Boid> neighbors;

                using (EntryClass.CodeProfiler.HookUp("GetNeighbors"))
                {
                    // WARNING: here we pass in entire list of
                    // boids to each boid, which is super slow until we have spatial
                    // partitioning
                    found = GetNeighbors(Boids[i], largestDistance, largestDistanceSquared);

                    if (found == null || found.Count == 0) continue;
                    neighbors = new List<Boid>(found.Count);
                    for (int j = 0; j < found.Count; j++)
                    {
                        neighbors.Add(Boids[found[j]]);
                    }
                }

                // Apply Rules
                //var (sepX, sepY) = Boid.Separate(elapsedSeconds, allBoids, allBoids[i], separationDistance, separationFactor);
                var (sepX, sepY) = Boid.Separate(elapsedSeconds, allBoids, allBoids[i], separationDistance, separationFactor, neighbors);

                //var (alignX, alignY) = Boid.Align(elapsedSeconds, allBoids, allBoids[i], alignmentDistance, alignmentFactor);
                var (alignX, alignY) = Boid.Align(elapsedSeconds, allBoids, allBoids[i], alignmentDistance, alignmentFactor, neighbors);

                //var (cohX, cohY) = Boid.Cohese(elapsedSeconds, allBoids, allBoids[i], cohesionDistance, cohesionFactor);
                var (cohX, cohY) = Boid.Cohese(elapsedSeconds, allBoids, allBoids[i], cohesionDistance, cohesionFactor, neighbors);

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
                allBoids[i].Translation += v;
                allBoids[i].Velocity = v;

                allBoids[i].SpatialNode.OnEntityNode_Moved(allBoids[i]);

                //Console.WriteLine("Separate = " + sepX.ToString() + ", " + sepY.ToString());
                //Console.WriteLine("Separate TEMP = " + tempX.ToString() + ", " + tempY.ToString());

                //Console.WriteLine("Alignment = " + alignX.ToString() + ", " + alignY.ToString());
                //Console.WriteLine("Cohesion = " + cohX.ToString() + ", " + cohY.ToString());
                //Console.WriteLine("Velocity = " + mVelocity.ToString());
                //Console.WriteLine("Speed = " + speed.ToString());
                //Console.WriteLine("Translation = " + mTranslation.ToString());


                // Apply boundary rules (wrap around)
                // (You'd need to define boundary dimensions here)
                // If X > maxX, X = minX, etc.
            }
        }
#else
        //public void ProcessStep(double elapsedSeconds, uint numBoids, ComponentStore<Transform.Transform_Struct> store, double separationDistance, double separationFactor, double alignmentDistance, double alignmentFactor, double cohesionDistance, double cohesionFactor, double maxSpeed, double turnFactor)
        public void ProcessStep(double elapsedSeconds, uint numBoids, double separationDistance, double separationFactor, double alignmentDistance, double alignmentFactor, double cohesionDistance, double cohesionFactor, double maxSpeed, double turnFactor)
        {
            double seperatationDistanceSquare = separationDistance * separationDistance;
            double alignmentDistanceSquared = alignmentDistance * alignmentDistance;
            double cohesionDistanceSquared = cohesionDistance * cohesionDistance;
            Span<Transform.Transform_Struct> mem;

            using (EntryClass.CodeProfiler.HookUp("AssignSpan"))
            {
                // note: passing the store and the need to the span to the stack is slow.
                // keep this in mind when developing data procesding funtions.  you dont want
                // to have to pass thay big bock of memory around. 
                // HOWEVER, using the following line mem = Store.Span is faster than using Store.Span[i].#### everywhere!
                mem = Store.Span;
                //Span<Transform.Transform_Struct> mem = store.Span;
            }

            List<int> neighbors = null;
            double largestDistance = System.Math.Max(this.SeparationDistance, this.AlignmentDistance);
            largestDistance = System.Math.Max(largestDistance, this.CohesionDistance);
            double largestDistanceSquared = largestDistance * largestDistance;
            // TODO: 
            // 1) MAKE SURE ALL RESULTS ARE COMPLETELY DETERMINISTIC NO MATTER WHICH PATH IS CHOSEN
            // 2) BOID needs to UPDATE its position within the OCTREE when it moves!
            for (int i = 0; i < numBoids; i++)
            {
                //try
                //{
                int currentIndex = i;
                using (EntryClass.CodeProfiler.HookUp("GetNeighbors"))
                    neighbors = GetNeighbors(Boids[currentIndex], largestDistance, largestDistanceSquared);

                if (neighbors == null || neighbors.Count == 0) continue;
                int nCount = neighbors.Count;
                //}
                // Apply Rules
                //var (sepX, sepY) = Boid.Separate(elapsedSeconds, Boids, Boids[i], SeparationDistance, SeparationFactor);
                //var (sepX, sepY) = Boid.Separate(elapsedSeconds, mem, numBoids, i, SeparationDistance, SeparationFactor);
                //var (sepX, sepY) = Boid.Separate(elapsedSeconds, store, numBoids, i, SeparationDistance, SeparationFactor, neighbors);

                //var (alignX, alignY) = Boid.Align(elapsedSeconds, Boids, Boids[i], AlignmentDistance, AlignmentFactor);
                //var (alignX, alignY) = Boid.Align(elapsedSeconds, mem, numBoids, i, AlignmentDistance, AlignmentFactor);
                //var (alignX, alignY) = Boid.Align(elapsedSeconds, store, numBoids, i, AlignmentDistance, AlignmentFactor, neighbors);

                //var (cohX, cohY) = Boid.Cohese(elapsedSeconds, Boids, Boids[i], CohesionDistance, CohesionFactor);
                //var (cohX, cohY) = Boid.Cohese(elapsedSeconds, mem, numBoids, i, CohesionDistance, CohesionFactor);

                //var (cohX, cohY) = Boid.Cohese(elapsedSeconds, mem, numBoids, i, CohesionDistance, CohesionFactor, coheseFunc);
                //var (cohX, cohY) = Boid.Cohese(elapsedSeconds, store, numBoids, i, CohesionDistance, CohesionFactor, neighbors);

                Vector3d sep;
                Vector3d steer;
                steer.x = 0d;
                steer.y = 0d;
                steer.z = 0d;

                // Iterate through precomputed nearISH neihbors 
                if (nCount > 0)
                {
                    for (int j = 0; j < nCount; j++)
                    {
                        //if (j == currentIndex) continue; // <- this check will already have been performed in building of neighbors list
                        double distanceSquared = Vector3d.GetDistance3dSquared(mem[currentIndex].Translation, mem[neighbors[j]].Translation);

                        if (distanceSquared < seperatationDistanceSquare)
                        {
                            if (distanceSquared > 0d) // Hypnotron Dec.4.2025 - required divide by 0 check
                            {
                                // TODO: are these two results equal?
                                //steer += (mem[currentIndex].Translation - mem[neighbors[j]].Translation) / distance ;
                                steer += (mem[currentIndex].Translation - mem[neighbors[j]].Translation) / System.Math.Sqrt(distanceSquared);
                            }
                        }
                    }

                    steer *= separationFactor;
                }

                sep = steer;

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
                        double distanceSquared = Vector3d.GetDistance3dSquared(mem[currentIndex].Translation, mem[neighbors[j]].Translation);

                        if (distanceSquared < alignmentDistanceSquared)
                        {
                            neighborsVelocity += mem[neighbors[j]].Velocity;
                            foundCount++;
                        }
                    }
                }
                if (foundCount == 0)
                    align = new Vector3d(0, 0, 0);
                else
                {
                    neighborsVelocity /= foundCount;
                    align = (neighborsVelocity - mem[currentIndex].Velocity) * alignmentFactor;
                }

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
                        neighborsAvgCenter += mem[neighbors[j]].Translation;

                    neighborsAvgCenter /= nCount;
                    coh = (neighborsAvgCenter - mem[currentIndex].Translation) * cohesionFactor;
                }

                // Sum forces
                Vector3d v = sep + align + coh;

                // Apply speed limits
                double speed = v.Length;
                if (speed > maxSpeed && maxSpeed != 0)
                {
                    v = (v / speed) * maxSpeed;
                }

                // ... (Optional: Implement min speed if needed)

                // Update position
                //v *= elapsedSeconds;

                mem[currentIndex].Translation += v;
                mem[currentIndex].Velocity = v;

                // todo: all these octree update should probably
                // occur last all at once... in fsct i believe 
                // this does occur in our main src branch because
                // we use a method named FinalizeMovement()

                Boids[currentIndex].SpatialNode.OnEntityNode_Moved(Boids[currentIndex]);

                // Apply boundary rules (wrap around)
                // (You'd need to define boundary dimensions here)
                // If X > maxX, X = minX, etc.

                // if (i == 64)
                //    Console.WriteLine("Boid Translation = " + mem[i].Translation.ToString());
                //}
                //catch (Exception ex)
                //{
                //    StackTrace st = new StackTrace(ex, true); // true to include file info
                //    StackFrame frame = st.GetFrame(0); // Get the top-most frame (where exception occurred)
                //    string fileName = frame.GetFileName();
                //     int lineNumber = frame.GetFileLineNumber();

                // Iterate over the frames extracting the information you need
                //    StackFrame[] frames = st.GetFrames();
                //    foreach (StackFrame f in frames)
                //    {
                //        Console.WriteLine("{0}:{1}({2},{3})", f.GetFileName(), f.GetMethod().Name, f.GetFileLineNumber(), f.GetFileColumnNumber());
                //    }

                //    Console.WriteLine($"Error in file: {fileName}, Line: {lineNumber}");
                //    Console.WriteLine(ex.InnerException.Message);
                //}
            }
        }
#endif
    }
	
		
	
    public class EntityNode : Transform
    {
        protected int mIndex;
        protected BoundingBox _box;
        protected OctreeOctant _octant;

        public BoundingBox BoundingBox
        {
            get { return _box; }
        }

        public EntityNode(int index, double x, double y, double xV, double yV)

        {
            mIndex = index;

        }

        public OctreeOctant SpatialNode
        {

            get { return _octant; }
            set { _octant = value; }
        }

        public int Index { get { return mIndex; } }
    }

	
		
    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN BOIDS
    //https://github.com/swharden/Csharp-Data-Visualization/blob/main/website/content/simulations/boids/index.md
    public class Boid : EntityNode
    {

        public Boid(int index, double x, double y, double xV, double yV)
            : base(index, x, y, xV, yV)
        {
            Vector3d t;
            t.x = x;
            t.y = y;
            t.z = 0.0d;
            Translation = t;

            Vector3d v;
            v.x = xV;
            v.y = yV;
            v.z = 0.0d;
            Velocity = v;

            _box = new BoundingBox(t, 2.0d);
        }

        private static double GetDistance(Boid b1, Boid b2)
        {
            return Vector3d.GetDistance3d(b1.Translation, b2.Translation);
        }

        // Rule 1: Separation
        // Classes version that iterates through all boids
        public static (double xVel, double yVel) Separate(double elapsedSeconds, List<Boid> boids, Boid current, double separationDistance, double separationFactor)
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
        public static (double xVel, double yVel) Separate(double elapsedSeconds, List<Boid> boids, Boid current, double separationDistance, double separationFactor, List<Boid> neighbors)
        {
            Vector3d steer;
            steer.x = 0d;
            steer.y = 0d;
            steer.z = 0d;

            // WARNING: iterating through ALL boids
            // for each CURRENT boid is O(n^2) and is too 
            // expensive
            // Iterate through other boids
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
        public static (double xVel, double yVel) Align(double elapsedSeconds, List<Boid> boids, Boid current, double alignmentDistance, double alignmentFactor)
        {
            // WARNING: LinkQ .Where iterates through ALL boids
            // for each CURRENT boid and is O(n^2) and is too 
            // expensive
            var neighbors = boids.Where(x => x != current && GetDistance(current, x) < alignmentDistance);
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
        public static (double xVel, double yVel) Align(double elapsedSeconds, List<Boid> boids, Boid current, double alignmentDistance, double alignmentFactor, List<Boid> preNeighbors)
        {
            // WARNING: LinkQ .Where iterates through ALL boids
            // for each CURRENT boid and is O(n^2) and is too 
            // expensive
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
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, List<Boid> boids, Boid current, double cohesionDistance, double cohesionFactor)
        {
            var neighbors = boids.Where(x => x != current && GetDistance(current, x) < cohesionDistance);
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
        public static (double xVel, double yVel) Cohese(double elapsedSeconds, List<Boid> boids, Boid current, double cohesionDistance, double cohesionFactor, List<Boid> preNeighbors)
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

        public static List<int> FindNeighbors(ComponentStore<Transform.Transform_Struct> store, uint numBoids, double distance, int currentIndex, Func<int, int, double, Transform.Transform_Struct, Transform.Transform_Struct, bool> condition)
        {
            //Span<Transform.Transform_Struct> mem = store.Span;
            Span<Transform.Transform_Struct> mem = BoidSimulation.Store.Span;

            List<int> neighbors = new List<int>();

            for (int i = 0; i < numBoids; i++)
            {
                if (condition(i, currentIndex, distance, mem[i], mem[currentIndex]))
                    neighbors.Add(i);

            }
            return neighbors;
        }

        public static List<int> FindNeighbors(List<Boid> allBoids, double distance, int currentIndex, Func<Boid, Boid, double, bool> condition)
        {
            List<int> neighbors = new List<int>();

            for (int i = 0; i < allBoids.Count; i++)
            {
                if (condition(allBoids[i], allBoids[currentIndex], distance))
                    neighbors.Add(i);

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
    }
		
		
		
		
		
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

        public static double GetDistance3d(Vector3d v, Vector3d v2)
        {
            return Math.Sqrt(GetDistance3dSquared(v, v2));
        }
        public static double GetLength(double x, double y, double z)
        {
            return Math.Sqrt(GetLengthSquared(x, y, z));
        }
        public static double GetLength(Vector3d v)
        {
            return Math.Sqrt(GetLengthSquared(v));
        }

        public static double GetLengthSquared(double x, double y, double z)
        {
            return (x * x) + (y * y) + (z * z);
        }
        public static double GetLengthSquared(Vector3d v)
        {
            return GetLengthSquared(v.x, v.y, v.z);
        }
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
 3
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
    }

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
        }*/
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

        //TODO: I really should make these regular NON static methods
        public static Vector3d[,] GetQuadFaceVertices(BoundingBox box)
        {
            Vector3d[,] vertices = new Vector3d[6, 4];
            // NOTE: for AABB the first subscript 0 to 5 indices correspond with 
            //the CUBEMAP_FACE enumeration such that
            // face 0 is the PositiveX = 0
            // face 1 is the NegativeX 
            // face 2 is the PositiveY 
            // face 3 is the NegativeY
            // face 4 is the PositiveZ
            // face 5 is the NegativeZ

            // the top quad (PositiveY)
            vertices[2, 0] = new Vector3d(box.Min.x, box.Max.y, box.Min.z);
            vertices[2, 1] = new Vector3d(box.Max.x, box.Max.y, box.Min.z);
            vertices[2, 2] = new Vector3d(box.Min.x, box.Max.y, box.Max.z);
            vertices[2, 3] = new Vector3d(box.Max.x, box.Max.y, box.Max.z);

            // the bottom quad (NegativeY)
            vertices[3, 0] = new Vector3d(box.Min.x, box.Min.y, box.Min.z);
            vertices[3, 1] = new Vector3d(box.Max.x, box.Min.y, box.Min.z);
            vertices[3, 2] = new Vector3d(box.Min.x, box.Min.y, box.Max.z);
            vertices[3, 3] = new Vector3d(box.Max.x, box.Min.y, box.Max.z);


            // the side quads consist of existing top and bottom vertices 
            // PostiveX
            vertices[0, 0] = vertices[3, 1];
            vertices[0, 1] = vertices[3, 3];
            vertices[0, 2] = vertices[2, 3];
            vertices[0, 3] = vertices[2, 1];

            // NegativeX
            vertices[1, 0] = vertices[3, 2];
            vertices[1, 1] = vertices[3, 0];
            vertices[1, 2] = vertices[2, 0];
            vertices[1, 3] = vertices[2, 2];

            // PositiveZ
            vertices[4, 0] = vertices[3, 3];
            vertices[4, 1] = vertices[3, 2];
            vertices[4, 2] = vertices[2, 2];
            vertices[4, 3] = vertices[2, 3];

            // NegativeZ
            vertices[5, 0] = vertices[3, 0];
            vertices[5, 1] = vertices[3, 1];
            vertices[5, 2] = vertices[2, 1];
            vertices[5, 3] = vertices[2, 0];
            return vertices;
        }

        public static Vector3d[] GetVertices(BoundingBox box)
        {
            //Console.WriteLine("Get Vertices");
            Vector3d[] vertices = new Vector3d[8];

            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THIS 
            // 6 ___ 7
            // |    |
            // 4 ___ 5
            //  \    \
            //   2 ___ 3
            //   |    |
            //   0 ___ 1
            // is our layout

            vertices[0].x = box.Min.x;
            vertices[0].y = box.Min.y;
            vertices[0].z = box.Min.z;
            vertices[1].x = box.Max.x;
            vertices[1].y = box.Min.y;
            vertices[1].z = box.Min.z;
            vertices[2].x = box.Min.x;
            vertices[2].y = box.Min.y;
            vertices[2].z = box.Max.z;
            vertices[3].x = box.Max.x;
            vertices[3].y = box.Min.y;
            vertices[3].z = box.Max.z;
            vertices[4].x = box.Min.x;
            vertices[4].y = box.Max.y;
            vertices[4].z = box.Min.z;
            vertices[5].x = box.Max.x;
            vertices[5].y = box.Max.y;
            vertices[5].z = box.Min.z;
            vertices[6].x = box.Min.x;
            vertices[6].y = box.Max.y;
            vertices[6].z = box.Max.z;
            vertices[7].x = box.Max.x;
            vertices[7].y = box.Max.y;
            vertices[7].z = box.Max.z;
            return vertices;
        }

        /*
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
                    // |    |
                    // 4 ___ 5
                    //  \    \
                    //   2 ___ 3
                    //   |    |
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

                    // the side faces
                    tris[2] = new Triangle(v[0], v[4], v[1]); // front
                    tris[3] = new Triangle(v[1], v[4], v[5]);

                    tris[4] = new Triangle(v[2], v[6], v[0]); // left
                    tris[5] = new Triangle(v[2], v[4], v[0]);

                    tris[6] = new Triangle(v[3], v[6], v[2]); // back
                    tris[7] = new Triangle(v[3], v[7], v[6]);

                    tris[8] = new Triangle(v[1], v[7], v[3]);
                    tris[9] = new Triangle(v[7], v[1], v[5]); // right
                    return tris;
                }

                public static Polygon[] GetPolyFaces(BoundingBox box)
                {
                    // NOTE: Default DirectX winding order is CLOCKWISE vertices for
                    // front (outward) facing.  XNA also uses clockwise for front facing.
                    // THUS 
                    // 6 ___ 7
                    // |    |
                    // 4 ___ 5
                    //  \    \
                    //   2 ___ 3
                    //   |    |
                    //   0 ___ 1
                    // is our layout      

                    Polygon[] polys = new Polygon[6];
                    Vector3d[] v = box.Vertices;

                    // bottom face
                    polys[0] = new Polygon(v[0], v[1], v[3], v[2]);

                    // top face
                    polys[5] = new Polygon(v[4], v[6], v[7], v[5]);

                    // the side faces
                    polys[1] = new Polygon(v[0], v[2], v[6], v[4]); // left 
                    polys[2] = new Polygon(v[1], v[5], v[7], v[3]); // right
                    polys[3] = new Polygon(v[0], v[4], v[5], v[1]); // front
                    polys[4] = new Polygon(v[3], v[7], v[6], v[2]); // back

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
        */

		public override bool Equals (object bb)
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

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // END PRIMITIVES


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
        //public static BoundingBox WorldBox;
        public static uint MaxDepth;
        public static uint SplitThreshHold;

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

        private int _depth;
        private int _index;   // index is specific to each depth and contains x,y,z offset at that depth and is useful for finding neighbors (which we may never do and just always move EntityNodes by re-inserting starting at root)
        private const int MAX_CHILD_COUNT = 8;

        private BoundingBox mBox;
        private OctreeOctant mParent;
        private OctreeOctant[] mChildOctants;

        // TODO: switch to linked list?
        private List<EntityNode> mEntityNodesCollection;


        public OctreeOctant(int index, int depth, BoundingBox box, OctreeOctant parent)
            : this()
        {
            _index = index;
            _depth = depth;
            mBox = box;
            mParent = parent;
            //System.Diagnostics.Debug.WriteLine("OctreeOctant() -- Created at index " + index.ToString());
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

        public int Index
        {
            get { return _index; }
        }

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

        internal Vector3d Radius
        {
            get
            {
                
				Vector3d radius;
                radius.x = mBox.Width * 0.5d;
                radius.y = mBox.Height * 0.5d;
                radius.z = mBox.Depth* 0.5d;
                return radius;
            }
        }

        internal int Depth
        {
            get { return _depth; }
        }

        public OctreeOctant[] Children
        {
            get { return mChildOctants; }
        }

        #region ISpatialNode
        public bool Visible { get; set; }

        public EntityNode[] EntityNodes
        {
            get
            {
                if (mEntityNodesCollection == null) return null;
                return mEntityNodesCollection.ToArray();
            }
        }

        public void Add(EntityNode entityNode, bool forceRoot)
        {
            if (forceRoot)
            {
                this.AddEntityNodeToCollection((EntityNode)entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - "  + entityNode.Entity.TypeName + " Forced into Root");
            }
            else
            {
                this.Add(entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName);
            }
        }

        private void AddEntityNodeToCollection(EntityNode entityNode)
        {
            if (mEntityNodesCollection == null)
                mEntityNodesCollection = new List<EntityNode>();

            entityNode.SpatialNode = this;
            mEntityNodesCollection.Add(entityNode);
        }

        public void Add(EntityNode entityNode)
        {
            System.Diagnostics.Debug.Assert(this.BoundingBox != null, "OctreeOctant.Add() - BoundingBox is null.");
#if DEBUG
            // only support square octree octants for performance
//            // TODO: we are going to see if this "performance" concern is no longer valid.  non square octrees are useful 
//            System.Diagnostics.Debug.Assert(this.BoundingBox.Max.x - this.BoundingBox.Min.x ==
//                this.BoundingBox.Max.y - this.BoundingBox.Min.y &&
//            this.BoundingBox.Max.x - this.BoundingBox.Min.x == 
//            this.BoundingBox.Max.z - this.BoundingBox.Min.z);
#endif

            int count;

            if (mEntityNodesCollection == null)
                count = 0;
            else
                count = mEntityNodesCollection.Count;

            //    NOTE: We specifically use ">=" for the depth comparison so that we
            //          can set the maximumDepth depth to 0 if we want a tree with
            //          no depth.
            if (count >= OctreeOctant.SplitThreshHold || _depth >= OctreeOctant.MaxDepth)
            {
                // add to this octant immmediately
                // Non Recursive Add
                this.AddEntityNodeToCollection((EntityNode)entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName);
                return;
            }

            // note: we intentionally compute a radius without taking into account hypotenuse.
            // note: if allowiing non square octants, we take the smallest octant radius and we'll compare that against largest radius of entity being inserted
            double octantRadius = this.BoundingBox.Max.x - this.BoundingBox.Min.x;
            octantRadius = Math.Min(this.BoundingBox.Max.y - this.BoundingBox.Min.y, octantRadius);
            octantRadius = Math.Min(this.BoundingBox.Max.z - this.BoundingBox.Min.z, octantRadius);
            octantRadius *= 0.5d;

            double childOctantRadius = octantRadius * 0.5d;
            double entityRadius = entityNode.BoundingBox.Radius;

            // attempt to insert using tightbox, but we must cull with loose box
            if (entityRadius > childOctantRadius)
            {
                // this entity won't fit in any children of this octant 
                // so what about the parent octant?
                if (entityRadius > octantRadius)
                {
                    // wont fit, can we try to move up to a parent?
                    if (this.IsRoot == false)
                    {
                        // Recurse UPWARDS
                        mParent.Add(entityNode);
                        return;
                    }
                }
                // Non Recursive Add because we're still here, 
                // so it either fits or we're at root and there's
                // no other place to put it
                this.AddEntityNodeToCollection(entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName);
                return;
            }

            // can't go further, add entitynode here
            if (this.Split() == false)
            {
                this.AddEntityNodeToCollection(entityNode);
                return;
            }

            Vector3d octantCenter = this.BoundingBox.Center;
            Vector3d entityCenter = entityNode.BoundingBox.Center; // TODO: is the entityNode box initialized at this point?
            int code = 0;
            if (entityCenter.x > octantCenter.x)
                code |= 1;
            if (entityCenter.y > octantCenter.y)
                code |= 2;
            if (entityCenter.z > octantCenter.z)
                code |= 4;

            for (int i = 0; i < MAX_CHILD_COUNT; i++)
            {
                // if this bitflag cobmination is not set
                if (code != i) continue;

                Vector3d offset = OctreeOctant.BoundsOffsetTable[i] * octantRadius;
                Vector3d center = octantCenter + offset;

                BoundingBox childOctantBox = new BoundingBox(center, (float)childOctantRadius);

                if (mChildOctants[i] == null)
                    mChildOctants[i] =
                        new OctreeOctant(0, _depth + 1, childOctantBox, this);

                // Recursive Add() until max depth is reached or the entity's radius > octant's loose radius
                mChildOctants[i].Add(entityNode);
            }
        }

        public void RemoveEntityNode(EntityNode entityNode)
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


        public void OnEntityNode_Moved(EntityNode entityNode)
        {
            // is the entity still in this bounds?
            // we dont have to test the radius of the entityNode because
            // we already know it fits.
            //    System.Console.WriteLine("m");
            if (mBox.Contains(entityNode.BoundingBox.Center)) return;

            // inform the parent that the entity in this octant no longer fits
            // NOTE: we do not add/remove the entityNode here.  The parent must do it
            // so that we don't trigger collapse of all 8 of it's children before parent can 
            // have a chance to fit it into one of its other 7 children
            if (this.IsRoot == false)
                mParent.Move(this, entityNode); // calls on Parent
        }

        private void Move(OctreeOctant childOctant, EntityNode entityNode)
        {
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

            OctreeOctant newOctant = this;
            Vector3d entityCenter = entityNode.BoundingBox.Center;
            while (newOctant.Parent != null)
            {
                if (newOctant.BoundingBox.Contains(entityCenter))
                    break;

                newOctant = newOctant.Parent;
            }

            System.Console.WriteLine("Moved to new octant");
            newOctant.Add(entityNode);// Add must always occur before Remove() because we dont want to collapse branches before we've had a chance to determine if the child will move there!
            childOctant.OnEntityNode_Removed(entityNode);
        }

        public void OnEntityNode_Resized(EntityNode entityNode)
        {
            // does this entityNode still fit in this octant?
            // we must test against entire box since this entity may now be too big to fit
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


        internal void OnEntityNode_Removed(EntityNode entityNode)
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

        private void OnChildOctant_Empty(OctreeOctant childOctant)
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

        private bool Split()
        {
            // cannot split because we're at max depth
            if (_depth == OctreeOctant.MaxDepth)
                return false;

            // we are already split
            if (this.mChildOctants != null)
                return true;

            // initialize the array but do not instance or assign an Octant
            this.mChildOctants = new OctreeOctant[8];
            return true;
        }

        // TODO: I should implement an overloaded version of Query that traverses for Entities that lay within a bounding box or sphere with potential also of matching a "match" predicate.
        //       Multi-threading of the query would be ideal.
        /// <summary>
        /// Looks for Regions/Entities that are in descendant RegionNodes or EntityNodes 
        /// that match the specified predicate.
        /// </summary>
        /// <param name="recurse"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public virtual List<EntityNode> Query(EntityNode refEnt, bool recurse, BoundingBox searchArea, Func<EntityNode, EntityNode, bool> match)
        {
            if (match == null) throw new ArgumentNullException("SceneNode.Query() - match cannot be null.");

            if (!this.mBox.Intersects(searchArea))
                return null;
            //Console.WriteLine ("Query B");

            List<EntityNode> results = new List<EntityNode>();

            if (mEntityNodesCollection != null)
                for (int i = 0; i < mEntityNodesCollection.Count; i++)
                {
                    if (match(mEntityNodesCollection[i], refEnt))
                        results.Add(mEntityNodesCollection[i]);
                }

            if (recurse)
            {
                if (mChildOctants != null)
                {
                    // NOTE: We recurse the child OctreeOctants, not EntityNodes
                    for (int j = 0; j < mChildOctants.Length; j++)
                    {
                        List<EntityNode> nestedResults = mChildOctants[j].Query(refEnt, recurse, searchArea, match);
                        if (nestedResults != null)
                            results.AddRange(nestedResults);

                    }
                }
            }

            if (results.Count == 0) return null;
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

        // different in translation between current and previous
        protected Vector3d mTranslationDelta;

#if USE_MEMORY_T
		public Memory<Transform_Struct> mMemStore; // This var must be accessible to any DATAPROCESSOR if USE_MEMORY<T> == TRUE

        //[StructLayout(LayoutKind.Sequential)]
        public struct Transform_Struct
        {
            //public string EntityID;

            public Vector3d Velocity;


            //public Vector3d Pivot;

            public Vector3d Translation;
            //public Vector3d DerivedTranslation;
            //public Vector3d GlobalTranslation;

            public Vector3d Scale;
            //public Vector3d DerivedScale;
            //public Vector3d GlobalScale;

            public Quaternion Rotation;
            //public Quaternion DerivedRotation;
            //public Quaternion GlobalRotation;

            //public Matrix RegionMatrix;

        }

        

#endif

        protected Transform()
        {

#if USE_MEMORY_T

            ComponentStore<Transform_Struct> store = EntryClass.mCStoreCol.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES); // Repository.StoresCollection.CheckOut<Transform_Struct>(EntryClass.NUM_ENTRIES);
            mMemStore = store.CheckOut();
            //initialize the memory store

            // todo do we need destuuctor for Repository.CheckIn mMemstore?


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
                return mVelocity;
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
				return mMemStore.Span[0].Translation; 
			}
            set 
			{ 
				mSpanAccessTest = value; 
				mMemStore.Span[0].Translation = value; 
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
            get { return mMemStore.Span[0].Scale; }
            set { mMemStore.Span[0].Scale = value; }
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
            get { return mMemStore.Span[0].Rotation; }
            set { mMemStore.Span[0].Rotation = value; }
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


        #endregion */ // UPDATES
#endif

        #region Disposable members
#if USE_MEMORY_T
        public void Dispose()
        { }

        public void DisposeManagedResources()
        {
            // todo
            //store.CheckIn();
            //BoidSimulation.mCStoreCol.CheckIn<Transform_Struct>(typeof(Transform_Struct), mMemStore);
        }
#endif

        #endregion
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // END NODES

		
		



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
		//
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
		
		

    ////////////////////////////////////////////////////////////////////////////////////////////////
    // BEGIN MEMORY STORES

    /// <summary>
    /// ComponentStoreCollection allows for the CheckIn() and CheckOut() of 
    /// ComponentStore<T> which is a wrapper around the System.Memory.Memory<T> 
    /// class.  
    /// This StoreCollection object will host ComponentStores<T> for both 
    /// Intrinsic and UserComponents
    /// </summary>
    public class ComponentStoreCollection
    {
        private Dictionary<Type, object> mUserComponentsCollection;

        public ComponentStoreCollection()
        {
            mUserComponentsCollection = new Dictionary<Type, object>();
        }

        public ComponentStore<T> CheckOut<T>(uint size = 64)
        {
            object value;
            bool success = mUserComponentsCollection.TryGetValue(typeof(T), out value);

            if (success)
                return (ComponentStore<T>)value; // throw new Exception("ComponentStoreCollection.CheckOut() - Dictionary Key Already Exists.");

            ComponentStore<T> store = new ComponentStore<T>(size);

            mUserComponentsCollection.Add(typeof(T), store);
            return store;
        }

        public void CheckIn<T>(T type, object store)
        {
            if (store == null) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - Dictionary is NULL.");

            object value;
            bool success = mUserComponentsCollection.TryGetValue(type.GetType(), out value);

            if (!success) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - ComponentStore for Type '" + typeof(T).Name + " ' is NULL.");

            mUserComponentsCollection.Remove(type.GetType());
            //value.Dispose();
        }
    } // ComponentStoreCollection.cs

    ///<summary>
    /// Components are essentially data stores for Intrinsic or User game objects.
    /// They are always stored as struct within contiguous Memory<T> for
    /// fast processing of their data.
    ///</summary>
    public class ComponentStore<T>
    {
        private uint STARTING_SIZE = 64;
        private const uint MIN_SIZE = 64;
        private const uint MAX_SIZE = 1024;
        private uint EXPAND_INCREMENT = MIN_SIZE; // expand by this amount when needed.  if 0, it will double the size of Components
        private object mSync;
        private Dictionary<string, bool[]> mViews;
        private Stack<int> mAvailableForCheckOut;

        private int mLastCheckOutIndex = -1;

        private Memory<T> Components;
        private bool[] InUse;


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
        public ComponentStore() : this(64)
        {
        }

        public ComponentStore(uint size)
        {
            STARTING_SIZE = size;
            mSync = new object();
            mAvailableForCheckOut = new Stack<int>();

            for (int i = (int)STARTING_SIZE; i >= 0; i--)
                mAvailableForCheckOut.Push(i);

            Components = new T[STARTING_SIZE];
            InUse = new bool[STARTING_SIZE];
        }

        public uint Size { get { return (uint)Components.Length; } }

        public Span<T> Span { get { return Components.Span; } }

        public void RemoveView(string viewName)
        {
            if (mViews == null) throw new Exception("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");
            bool[] view;
            if (!mViews.TryGetValue(viewName, out view)) throw new Exception("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");

            mViews.Remove(viewName);
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
            if (Components.Equals(default(T)))
            {
                Components = new T[STARTING_SIZE];
                InUse = new bool[STARTING_SIZE];
                mAvailableForCheckOut = new Stack<int>();

                for (int i = (int)STARTING_SIZE; i >= 0; i--)
                    if (!InUse[i])
                        mAvailableForCheckOut.Push(i);

                return;
            }

            int newSize = (int)(Components.Length + EXPAND_INCREMENT);
            if (EXPAND_INCREMENT == 0)
                newSize = Components.Length * 2;

            T[] data = new T[newSize];
            //Components.Span[0].CopyTo(data.AsSpan());

            // hack - copy components to temporary array first since i can't get 
            // MemoryExtensin.CopyTo() working at the moment
            T[] tmp = Components.ToArray();
            tmp.CopyTo(data, 0);

            //MemoryExtensions.CopyTo<T>(Components.ToArray(), data);

            Components = new Memory<T>(data);

            bool[] newInUse = new bool[newSize];
            InUse.CopyTo(newInUse, 0);
            InUse = newInUse;

            // create a new mAvailableForCheckOut stack using the new InUse[] array
            Stack<int> tmpStack = new Stack<int>(newSize);
            for (int i = (int)STARTING_SIZE; i >= 0; i--)
                if (!InUse[i])
                    tmpStack.Push(i);

            mAvailableForCheckOut = tmpStack;

            ExpandViews(newSize);
        }

        private void ExpandViews(int newSize)
        {
            if (mViews == null) throw new Exception("ComponentsStore.ExpandViews() - Views Collection is NULL.");

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

        // GameAPI will need commands for checking in/out via our Entity script initializations, 
        // the types made here in our ComponentStore
        // So for instance, if "EnergyWeapon.cs" on Initialize()
        // will register "Weapon" and "EnergyWeapon" interfaces.
        // Recall that Initialize() is only called ONCE PER SCRIPT whereas Initialize_Entity
        // is called per Entity that is using that script.
        // Initialize_Entity() will then call CheckOut(typeof(Weapon)) and CheckOut(typeOf(EnergyWeapon))
        // to get direct memory access to the Memory<T> where variables associated with those interfaces
        // will get stored.
        public Memory<T> CheckOut() // aka: MemoryPool<T>.Rent() 
        {
            lock (mSync)
            {
                const int HOW_MANY = 1;

                if (Components.Equals(null))
                    Expand();

                // using stack<int> of available indices
                if (mAvailableForCheckOut.Count > 0)
                {
                    int i = mAvailableForCheckOut.Pop();
                    InUse[i] = true;
                    return Components.Slice(i, HOW_MANY);
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
                return CheckOut();
            }
        }

		public ReadOnlySpan<T> Copy ()
		{
			lock (mSync)
            {
				ReadOnlySpan<T> result = Components.Span;
				return result;
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

                        mAvailableForCheckOut.Push(i);
                        return;

                        // todo: Components.Span[i] = default(T);    
                    }
            }
        }
    } // ComponentStore.cs

		
		
		
		
		
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

            if (ProfilerEnabled)
            {
                foreach (string Name in mProfiles.Keys)
                {
                    Profile prof = mProfiles[Name];

                    System.Console.WriteLine(Name + prof.Categorized + " " + prof.ElapsedSeconds);
                    // display indented each non-categorized element under the catch-all "Non-Categorized" header
                    if (!(prof.ElapsedSeconds == 0) && !prof.Categorized)
                    {
                        double Ratio = prof.ElapsedSeconds / mTotalElapsedTime;
                        string Format = "{0} = {1:P} ({2:0.00} seconds)";
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

        public ProfileHook(IProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("ProfileHook.ctor() - 'profile' argument cannot be null.");
            mHookedProfile = profile;

            mStopwatch = new Stopwatch();
            mStopwatch.Start();
        }

        private bool disposedValue = false;

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

		public static string GetTimeString()
        {
            // NOTE: When running on Online Compiler, this time will be the time of the SERVER 
            //       running the online compiler and NOT your local PC.
            return DateTime.Now.ToString();
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
    */
}
