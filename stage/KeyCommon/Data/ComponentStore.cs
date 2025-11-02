using System.Collections.Generic;
using System.Memory;

namespace KeyCommon.Data
{
    public class ComponentStore<T> : Store
    {
        private const uint STARTING_SIZE = 64;
        private const uint MAX_SIZE = 1024 * 1000;
        private uint EXPAND_INCREMENT = STARTING_SIZE; // expand by this amount when needed.  if 0, it will double the size of Components
        private object mSync;
        
        
        public Memory<T> Components;
        private bool[] InUse;

        public ComponentStore<T>()
        {
            mSync = new object();
            Components = new T[STARTING_SIZE].ToMemory(); 
            InUse = new bool[STARTING_SIZE];
        }
        
        private Dictionary<string, bool[]> mViews;
        public void RemoveView (string viewName)
        {
            if (mViews == null) throw new Exception ("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");
            if (!mViews.Exists(viewName)) throw new Exception ("ComponentStore.RemoveView() - A View with name '" + viewName + "' NOT FOUND.");
            
            mViews.Remove(viewName);
        }
        
        public void CreateView (string viewName)
        {
            if (mViews == null) 
                mViews = new Dictionary<string, bool[]>();
            
            if (mViews.Exists(viewName)) throw new Exception ("ComponentStore.CreateView() - A View with name '" + viewName + "' already exists.");
            
            // By default, all indices start off as enabled
            bool[] indices = new int[Components.Length];
            for (int i = 0; i < Components.Length; i++)
                indices[i] = true;
              
            mViews.Add (viewName);  
            mViews[viewName] = indices;
        }
        
        public void AddIndicesToView(string viewName, int enabledIndex)
        {
            AddIndicesToView (viewName, new int[]{index});
        }
        
        public void AddIndicesToView (string viewName, int[] enabledIndices)
        {
            if (!mViews.Exists(viewName)) throw new Exception ("ComponentStore.AddIndicesToView() - A View with name '" + viewName + "' does NOT exist.");
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
            if (!mViews.Exists(viewName)) throw new Exception ("ComponentStore.AddIndicesToView() - A View with name '" + viewName + "' does NOT exist.");
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
            if (Components == null) 
            {
                Components = new T[STARTING_SIZE].ToMemory();
                bool[] newInUse = new bool[STARTING_SIZE];
                return;
            }
            
            int newSize = Components.Length + EXPAND_INCREMENT;
            if (EXPAND_INCREMENT == 0)
                newSize = Components.Length * 2;
                
            T[] data = new T[newSize];
            Components.Span[0].CopyTo(data.AsSpan());
            
            Components = new Memory<T>(data);
            
            bool[] newInUse = new bool[newSize];
            InUse.CopyTo(newInUse, 0);
            
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
        public T CheckOut() // aka: MemoryPool<T>.Rent() 
        {
            lock(mSync)
            {
                const int HOW_MANY = 1;
                
                if (Components == null) Expand();
                
                for (int i = 0; i < Components.Length; i++)
                    if (!InUse[i])
                    {
                        InUse[i] = true;
                        return Components.Slice(i, HOW_MANY);    
                    }
                
                // if still here, we need to expand first
                Expand();
                return CheckOut();
            }
        }
        
        public void CheckIn()
        {
            lock (mSync)
            {
                for (int i = 0; i < Components.Length; i++)
                    if (!InUse[i])
                    {
                        InUse[i] = false;
                        Components.Span[i] = default(T));    
                    }
            }
        }
        
    }
    
}