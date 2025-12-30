using System.Collections.Generic;
using System.Memory;

namespace KeyCommon.Data
{
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
    }
    
}