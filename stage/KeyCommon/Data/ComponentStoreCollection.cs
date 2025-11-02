using System;
using System.Collections.Generic;

namespace KeyCommon.Data
{
    /// <summary>
	/// ComponentStoreCollection allows for the CheckIn() and CheckOut() of 
	/// ComponentStore<T> which is a wrapper around the System.Memory.Memory<T> 
	/// class.  
	/// </summary>
    public class ComponentStoreCollection
    {
		private Dictionary<string, ComponentStore<T>> mUserComponentsCollection;
		
		public ComponentStoreCollection()
		{
		    mUserComponentsCollection = new Dictionary<string, DataStore<T>();
		}
		
		public T CheckOut(Type T)
		{
		    object value;
		    bool success = mUserComponentsCollection.TryGetValue(entityID, out value);
		    
		    if (success) throw new Exception ("ComponentStoreCollection.CheckOut() - Dictionary Key Already Exists.");
		    
		    ComponentStore store = new ComponentStore<T>();
		    
		    mUserComponentsCollection.Add (entityID, data);
		    return data;
		}
		
		public void CheckIn (Type T, object store)
		{
		    if (store == null) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - Dictionary is NULL.");
		    
		    object value;
		    bool success = mUserComponentsCollection.TryGetValue(T, out value);
		    
		    if (object != data) throw new ArgumentOutOfRangeException("ComponentStoreCollection.CheckIn() - ComponentStore for Type '" + typeof(T).Name + " ' is NULL.");
		    
		    mUserComponentsCollection.Remove (T);
		    data.Dispose();
		}
	}
}