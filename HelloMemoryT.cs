namespace HelloMemoryT
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
	
    public static class AppMain
    {
  		//UserDataStore mUserDataStore;
      private static ComponentStoreCollection mStoresCollection;
      public static uint DEFAULT_STORE_SIZE = 1024 * 400;

  		public delegate void Processor<T>(ComponentStore<T> store, object parameters, Random r);
  		
  		public static int RS_LEN = 7;
  		public static int RS_COUNT = 10;
  		
  		
  		public class TestClass
  		{
    		  // 
    		  // assigned (CheckOut()) by Repository on creation of this node.
    		  // reclaimed (CheckIn()) by Repository on recount==0
    		  // For NON-INTRINSIC memory stores, this is more of a problem.
    		  // We can't necessarily rely on a script for each Entity to do this 
    		  // because for a Viewpoint or say we are dynamically creating a bunch
    		  // of BonedActors and we want to assign Character GameObjects to them 
    		  // that exist as UserTypes generated via code in client EXE and NOT in 
    		  // an Entity's script, then how do we control CheckOut() and CheckIn() 
    		  // for those cases?
    		  // Because consider, it is our Entities' scripts that define these 
    		  // user types that only the EXE, Game##.DLL and Scripts are aware of.
    		  // Keystone.dll and KeyCommon.dll and KeyStandardLibrary.dll do not 
    		  // care about any of those things.
    		  // 
    		  // Whatever is creating a custom data interface like a Character 
    		  // or a EnergyWeapon that gets assigned to an Entity, those items
    		  // need to be "Registered" which gets them added to a ComponentStoreCollection
    		  // by their "Type" and returns a Memory<T> for that Entity to use in it's
    		  // Entity.UserData.AddMemory(Type, mem);
    		  
    		  
    		  internal Memory<TestStruct> mMemStore;
    		  
    			public TestClass (string[] randomStrings)
    			{
    				RStrings = randomStrings;
    			}
    			
    			// local variables
    			public string EntityID;
    			public string[] RStrings;
    			public float X;
    			public float Y;
    			public float Z;
    			
    			// variables stored in contiguous array of structs via Memory<T>
    			public string GetEntityID { get {return mMemStore.Span[0].EntityID;}}
          public string[] GetRStrings { get {return mMemStore.Span[0].RStrings;}}
          public float GetX { get {return mMemStore.Span[0].X;}}
          public float GetY { get {return mMemStore.Span[0].Y;}}
          public float GetZ { get {return mMemStore.Span[0].Z;}}
          
          		
      		public static TestClass Create(Random random)
      		{
        			string[] s = new string[RS_COUNT];
        			for (int i = 0; i < RS_COUNT; i++)
        			{
        				s[i] = GenerateRandomString(RS_LEN, random);
        			}
        			return new TestClass(s);
      		}
		  }
  		
		  public struct TestStruct
		  {
    			public string EntityID;
    			public string[] RStrings;
    			public float X;
    			public float Y;
    			public float Z;
		  }
      
      
      [STAThread]
      private static void Main()
      {
            
			  Stopwatch stopWatch = new Stopwatch();
        				
        mStoresCollection = new ComponentStoreCollection();
        //ComponentStore<Keystone.Data.AdvancedEntityData>> store = mStores.CheckOut<Keystone.Data.AdvancedEntityData>();
        ComponentStore<TestStruct> store = mStoresCollection.CheckOut<TestStruct>(DEFAULT_STORE_SIZE);
	
	
  			// Initialize our classes and Memory<T> structs with values
  			Random r = new Random();
  			
  			int storeSize = (int)store.Size;
  			Console.WriteLine("size = " + storeSize.ToString());
        int entityCount = storeSize; // this is only for the event that EntityCount is < the size of the entire ComponentStore which should usually be true
  			
  			TestClass[] classes = new TestClass[entityCount];
  			
  			//Memory<TestStruct> mem = store.Components.Slice(0, store.Components.Length);
        //Span<TestStruct> memSpan = store.Components.Span; <-- TODO: we should not allow access to "Components" directly, but just to the Span()
  			
        for (int i = 0; i < entityCount; i++)
        {
    				classes[i] = TestClass.Create(r);
    				//classes[i].mMemStore = store.Components.Slice(i, 1);
    				Memory<TestStruct> mem = store.CheckOut();
    				
    				classes[i].mMemStore = mem; //store.CheckOut();
    				
    				// the idea behind creating these strings is to help ensure
    				// that the TestClass array is not populated by instances that 
    				// are contiguous.
    				string[] memStrings = new string[RS_COUNT];
    				for (int j = 0; j < RS_COUNT; j++)
    					memStrings [j] = GenerateRandomString(RS_LEN, r); 
  				
  				
  				  mem.Span[0].RStrings = memStrings;
  				  //memSpan[i].RStrings = memStrings;
  				  classes[i].RStrings = memStrings;
  				
    				string guid = new System.Guid().ToString();
    				classes[i].EntityID = 	guid;
    				mem.Span[0].EntityID = guid;
    				//memSpan[i].EntityID  = guid;
  				  System.Diagnostics.Debug.Assert (mem.Span[i].EntityID == classes[i].GetEntityID);
  				  
  				  float x = r.Next();
    				classes[i].X = x;
    				mem.Span[0].X = x;
    				//memSpan[i].X  = x;
    				System.Diagnostics.Debug.Assert (mem.Span[i].X == classes[i].GetX);
    				
    				float y = r.Next();
    				classes[i].Y = y;
    				mem.Span[0].Y = y;
    				//memSpan[i].Y  = y;
    				System.Diagnostics.Debug.Assert (mem.Span[i].Y == classes[i].GetY);
    				
    				float z = r.Next();
    				classes[i].Z = z;
    				mem.Span[0].Z = z;
    				//memSpan[i].Z  = z;
            System.Diagnostics.Debug.Assert (mem.Span[i].Z == classes[i].GetZ);
          
        }
  			
              
          // NOTE: the entire UserDataStore get's passed to our various "data processors."
          //mUserDataStore = new UserDataStore();
          
          // TODO: checkout of UserData needs to occur when the Entity is created?
          //       Not all Entities need it though? Hrm.  
          //UserData data = mUserDataStore.CheckOut(entityID);
         //entity.Data = data;
          
  			object parameters = new object();
			
        	// WARM UP the code so that the loops are JIT properly
			  // =====================
				System.Diagnostics.Debug.WriteLine("WARM-UP - RUNNING.");
  			Console.WriteLine("WARM-UP - RUNNING.");
  			TestClasses(classes, r);
  			Processor<TestStruct> p = TestIntrinsicProcessor;
  			p.Invoke(store, parameters, r);
  				System.Diagnostics.Debug.WriteLine("WARM-UP - COMPLETED.");
  			Console.WriteLine("WARM-UP - COMPLETED.");
  			
  			
  			System.Diagnostics.Debug.WriteLine("PERFORMANCE - RUNNING.");
  			Console.WriteLine("PERFORMANCE - RUNNING");
  			
  			// TEST CLASSES
  			// =====================
  			stopWatch.Start();
  			TestClasses(classes, r);
  			stopWatch.Stop();
			
      	TimeSpan timeSpan = stopWatch.Elapsed;

      	// Format and display the TimeSpan value.
      	string elapsedTimeClasses = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
          	timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds,
          	timeSpan.Milliseconds / 10);
      		
						
  			// TEST MEMORY<T>
  			// =====================
  			stopWatch.Reset();
        stopWatch.Start();
  			
  			p.Invoke(store, parameters, r);
  			
  			stopWatch.Stop();
  			
  			TimeSpan prev = timeSpan;
  			
  			timeSpan = stopWatch.Elapsed;
          	string elapsedTimeMemoryT = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
              	timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds,
              	timeSpan.Milliseconds / 10);
          	
        double diff = (prev - timeSpan) / prev * 100;
        
        System.Diagnostics.Debug.WriteLine("PERFORMANCE - COMPLETED.");
  			Console.WriteLine("PERFORMANCE - COMPLETED");
			
  			// LOG THE RESULTS of both runs - classes and memory<T> 
  			// =====================
  			Console.WriteLine("RunTime CLASSES: " + elapsedTimeClasses);
  			System.Diagnostics.Debug.WriteLine("RunTime CLASSES: " + elapsedTimeClasses);
  			
  			Console.WriteLine("RunTime MEMORY<T>: " + elapsedTimeMemoryT);
  			System.Diagnostics.Debug.WriteLine("RunTime MEMORY<T>: " + elapsedTimeMemoryT);
  			
  			System.Diagnostics.Debug.WriteLine("--------------------------");
  			
  			
  			string DIFF_TEXT = "faster";
  			if (diff < 0) DIFF_TEXT = "slower";
  			
  			System.Console.WriteLine("Memory<T> is " + diff.ToString() + DIFF_TEXT + " than using Getter and Setter of Classes.");
  			System.Diagnostics.Debug.WriteLine("Memory<T> is " + diff.ToString() + "% " + DIFF_TEXT + " than using Getter and Setter of Classes.");
  			
  			// Validate() is only used to make sure the compiler doesn't take a huge shortcut
  			// and not run the test code at all because none of the TestClass or TestStruct 
  			// members are ever used.
  			Validate(store, classes);
  			
			
        //DataProcessors.Processor p = TestIntrinsicProcessor;
        
        //mIntrinsicProcessors = new KeyCommon.Processors.DataProcessors();
        //mIntrinsicProcessors.Add("STEER", p);
        // then -> p[i].Invoke(store, parameters, r);
	
        //mRulesProcessors = game.RulesProcessors;

        //Update(0.01f);
			
			
    }

		
		  public static string GenerateRandomString(int length, Random random)
  	  {
  			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  			System.Text.StringBuilder sb = new System.Text.StringBuilder(length);
  
  			for (int i = 0; i < length; i++)
  			{
  				sb.Append(chars[random.Next(chars.Length)]);
  			}
  
  			return sb.ToString();
		}
		
  		private static void Validate(ComponentStore<TestStruct> store, TestClass[] classes)
  		{
  		  
    			Span<TestStruct> ts = store.Span;
    			decimal classesTotal = 0;
    			decimal memoryT_Total = 0;
    			//System.Numerics.BigInteger classesTotal = 0;
    			//System.Numerics.BigInteger memoryT_Total = 0;
    			
    			for (int i = 0; i < classes.Length; i++)
          {
              classesTotal += (decimal)(classes[i].X + classes[i].Y + classes[i].Z);
    				  memoryT_Total += (decimal)(ts[i].X + ts[i].Y + ts[i].Z);
    				  
    				  //classesTotal += (System.Numerics.BigInteger)(classes[i].X + classes[i].Y + classes[i].Z);
    				  //memoryT_Total += (System.Numerics.BigInteger)(ts[i].X + ts[i].Y + ts[i].Z);
    				  
      				//Console.WriteLine("TestIntrinsicProcessor() - CLASSES -> X = '" + classes[i].X.ToString() + "' Y = '" + classes[i].Y.ToString() + "' Z = '" + classes[i].Z.ToString() + "'" );
      				//Console.WriteLine("TestIntrinsicProcessor() - MEMORY<T> -> X = '" + ts[i].X.ToString() + "' Y = '" + ts[i].Y.ToString() + "' Z = '" + ts[i].Z.ToString() + "'" );
      				
    			}
    			
    			Console.WriteLine("TestIntrinsicProcessor() - CLASSES TOTAL  = " + classesTotal.ToString());
      		Console.WriteLine("TestIntrinsicProcessor() - MEMORY<T> TOTAL = " + memoryT_Total.ToString());
      				
  		}
  		
  		private static float FRACTION = 0.0135f;
  		private static void TestClasses (TestClass[] classes, Random r)
  		{
  		  	System.Diagnostics.Debug.WriteLine("TestClasses() - RUNNING.");
    			Console.WriteLine("TestClasses() - RUNNING.");
    			for (int i = 0; i < classes.Length; i++)
          {
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - EntityID = '" + classes[i].EntityID + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - EntityID = '" + classes[i].EntityID + "'");
      				
      				int rIndex = r.Next (0, classes.Length);
      				
      				classes[i].X *= 2;
      				classes[i].X *= classes[rIndex].X * FRACTION;
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - X = '" + classes[i].X.ToString() + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - X = '" + classes[i].X.ToString() + "'");
      				rIndex = r.Next (0, classes.Length);
      				classes[i].Y *= 2;
      				classes[i].Y *= classes[rIndex].Y * FRACTION;
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - Y = '" + classes[i].Y.ToString() + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - Y = '" + classes[i].Y.ToString() + "'");
      				rIndex = r.Next (0, classes.Length);			
      				classes[i].Z *= 2;
      				classes[i].Z *= classes[rIndex].Z * FRACTION;
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - Z = '" + classes[i].Z.ToString() + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - Z = '" + classes[i].Z.ToString() + "'");
    			}
    			System.Diagnostics.Debug.WriteLine("TestClasses() - COMPLETED.");
    			Console.WriteLine("TestClasses() - COMPLETED.");
  		}
  
  		private static void TestIntrinsicProcessor(ComponentStore<TestStruct> store, object parameters, Random r)
      //private static void TestIntrinsicProcessor(Scene scene, ComponentStore<TestStruct> store, object parameters, GameTime gt)
      {
    			System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - RUNNING.");
    			Console.WriteLine("TestIntrinsicProcessor() - RUNNING.");
          //Entities[] entities = scene.ActiveEntities;
          //Memory<TestStruct> mem = store.Components.Slice(0, store.Components.Length);
          Span<TestStruct> ts = store.Span;
    	
          for (int i = 0; i < (int)store.Size; i++)
          {
      	      int rIndex = r.Next (0, (int)store.Size);
      				
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - EntityID = Mem[i]'" + mem.Span[i].EntityID + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - EntityID = Span[i]'" + ts[i].EntityID + "'");
      				
      				
      				ts[i].X *= 2f;
      				ts[i].X *= ts[rIndex].X * FRACTION;
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - X = Mem[i]'" + mem.Span[i].X.ToString() + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - X = Span[i]'" + ts[i].X.ToString() + "'");
      				rIndex = r.Next (0, (int)store.Size);
      				ts[i].Y *= 2f;
      				ts[i].Y *= ts[rIndex].Y * FRACTION;
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - Y = Mem[i]'" + mem.Span[i].Y.ToString() + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - Y = Span[i]'" + ts[i].Y.ToString() + "'");
      				rIndex = r.Next (0, (int)store.Size);
      				ts[i].Z *= 2f;
      				ts[i].Z *= ts[rIndex].Z * FRACTION;
      				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - Z = Mem[i]'" + mem.Span[i].Z.ToString() + "'");
      				//Console.WriteLine("TestIntrinsicProcessor() - Z = Span[i]'" + ts[i].Z.ToString() + "'");
      				
      				// Verify editing the span above is also modifying the underlying memory
      				//System.Diagnostics.Debug.Assert(ts[i].Equals(mem.Span[i]));
          }
    			
      			System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - COMPLETED.");
      			Console.WriteLine("TestIntrinsicProcessor() - COMPLETED.");
        }
          
      public static void Update() // Update(GameTime gameTime)
      {

          //mIntrinsicProcessors.Update(mScene, mScene.ActiveEntities, gameTime);
          
          // TODO: create intrinsic structs to store
          // enum IntrinsicDataTypes
          // {
          //     Transform = 1;
          //     Bounds = 2;
          //     Physics = 3;
          // }

          // each struct should contain a field for holding the index to a UserData object in a UserDataStore
          // todo: currently the structs are stored in a dictionary using a string key not an int index... 
          //       for near term, lets just store a fixed byte[] field in our Memory<T> representing the 
          //       GUID id of every entity.
        
        //mRulesProcessors.Update(mScene, mScene.ActiveEntities, gameTime);
  
      }
    }
    

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
    		    
    		    if (success) throw new Exception ("ComponentStoreCollection.CheckOut() - Dictionary Key Already Exists.");
    		    
    		    ComponentStore<T> store = new ComponentStore<T>(size);
    		    
    		    mUserComponentsCollection.Add (typeof(T), store);
    		    return store;
    		}
    		
    		public void CheckIn<T> (T type, object store)
    		{
    		    if (store == null) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - Dictionary is NULL.");
    		    
    		    object value;
    		    bool success = mUserComponentsCollection.TryGetValue(type.GetType(), out value);
    		    
    		    if (!success) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - ComponentStore for Type '" + typeof(T).Name + " ' is NULL.");
    		    
    		    mUserComponentsCollection.Remove (type.GetType());
    		    //value.Dispose();
    		}
	  } // ComponentStoreCollection.cs
    
    ///<summar>
    /// Components are essentially data stores for Intrinsic or User game objects.
    /// They are always stored as struct within contiguous Memory<T> for
    /// fast processing of their data.
    ///</summary>
    public class ComponentStore<T> 
    {
        private uint STARTING_SIZE = 64;
        private const uint MIN_SIZE = 64;
        private const uint MAX_SIZE = 1024 * 1000;
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
        
        public uint Size {get {return (uint)Components.Length;}}
        
        public Span<T> Span {get {return Components.Span;}}
        
        public void RemoveView (string viewName)
        {
            if (mViews == null) throw new Exception ("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");
			      bool[] view;
            if (!mViews.TryGetValue(viewName, out view)) throw new Exception ("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");
            
            mViews.Remove(viewName);
        }
        
        public void CreateView (string viewName)
        {
            if (mViews == null) 
                mViews = new Dictionary<string, bool[]>();
            
			      bool[] v;
            if (mViews.TryGetValue(viewName, out v)) throw new Exception ("ComponentStore.CreateView() - A View with name '" + viewName + "' already exists.");
            
            // By default, all indices start off as enabled
            bool[] indices = new bool[Components.Length];
            for (int i = 0; i < Components.Length; i++)
                indices[i] = true;
              
            mViews.Add (viewName, indices);  
            //mViews[viewName] = indices;
        }
        
        public void AddIndicesToView(string viewName, int enabledIndex)
        {
            AddIndicesToView (viewName, new int[]{enabledIndex});
        }
        
        public void AddIndicesToView (string viewName, int[] enabledIndices)
        {
			      bool[] v;
            if (!mViews.TryGetValue(viewName, out v)) throw new Exception ("ComponentStore.AddIndicesToView() - A View with name '" + viewName + "' does NOT exist.");
            bool[] results = mViews[viewName];
            
            int length = Components.Length;
            
            // enable all indices specified in the enabledIndices argument
            for (int i = 0; i < enabledIndices.Length; i++)
                if (enabledIndices[i] < length)
                    results[enabledIndices[i]] = true;
             
            mViews[viewName] = results;       
            //mViews[viewName] = Helpers.ArrayExtensions.ArrayAppendRange(mViews[mViewName], enabledIndices);
        }
        
        public void RemoveIndicesFromView (string viewName, int[] disabledIndices)
        {
			      bool[] v;
            if (!mViews.TryGetValue(viewName, out v)) throw new Exception ("ComponentStore.AddIndicesToView() - A View with name '" + viewName + "' does NOT exist.");
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
        public bool[] GetView (string viewName)
        {
            bool[] results;
            bool success = mViews.TryGetValue(viewName, out results);
            if (success) return results;
            
            throw new Exception ("ComponentStore.GetView() - ERROR: View '" + viewName + "' not found.");
        }
        
        // TODO: script initialization will grab/checkout the arrayElements it needs
        //       script destructors need to checkin / dispose all array arrayElements
        private void Expand ()
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
			      
            ExpandViews (newSize);
        }
        
        private void ExpandViews (int newSize)
        {
            if (mViews ==  null) throw new Exception ("ComponentsStore.ExpandViews() - Views Collection is NULL.");
            
            foreach (var key in mViews.Keys)
            {
                bool[] indices = mViews[key];
                
                bool[] newInUse = new bool[newSize];
                indices.CopyTo(newInUse, 0);
            
                int diff = newSize -  indices.Length;
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
            lock(mSync)
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
    

    /*public class DataProcessors
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
        /// </summary>
        
        // movement {steering, newtonian movement, interpolation animations, collisions}
        public delegate void Processor<Memory<T>>(Memory<T> data, IScene scene, object parameters, GameTime gameTime)
        
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
	*/
} // namespace