using System;
using System.Collections.Generic;

namespace KeyCommon.Data
{
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
}