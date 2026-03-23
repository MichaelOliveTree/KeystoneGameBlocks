using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections;


namespace Keystone.Helpers
{

    // TODO: do we use this anywhere?  There is a Collections.Generic.Concurrent.ConcurrentDictionary<>  so we really don't need this
    public class ThreadSafeDictionary <TKey, TValue>
    {

        //This is the internal dictionary that we are wrapping
        Dictionary<TKey, TValue> mDictionary;

        #region IDictionary members
        [NonSerialized]
        ReaderWriterLockSlim dictionaryLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public ThreadSafeDictionary()
        {
            mDictionary = new Dictionary<TKey, TValue>();
        }

        public virtual bool Remove(TKey key)
        {
            dictionaryLock.EnterWriteLock();
            try
            {
                return this.mDictionary.Remove(key);
            }
            finally
            {
                dictionaryLock.ExitWriteLock();
            }
        }



        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            dictionaryLock.EnterReadLock();
            try
            {
                return this.mDictionary.TryGetValue(key, out value);
            }
            finally
            {
                dictionaryLock.ExitReadLock();
            }
        }


        public virtual TValue this[TKey key]
        {
            get
            {
                dictionaryLock.EnterReadLock();
                try
                {
                    return this.mDictionary[key];
                }
                finally
                {
                    dictionaryLock.ExitReadLock();
                }
            }
            set
            {
                dictionaryLock.EnterWriteLock();
                try
                {
                    this.mDictionary[key] = value;
                }
                finally
                {
                    dictionaryLock.ExitWriteLock();
                }
            }
        }


        public virtual ICollection Keys
        {
            get
            {
                dictionaryLock.EnterReadLock();
                try
                {
                    return new List<TKey>(this.mDictionary.Keys);
                }
                finally
                {
                    dictionaryLock.ExitReadLock();
                }
            }
        }


        public virtual ICollection Values
        {
            get
            {
                dictionaryLock.EnterReadLock();
                try
                {
                    return new List<TValue>(this.mDictionary.Values);
                }
                finally
                {
                    dictionaryLock.ExitReadLock();
                }
            }
        }


        public virtual void Clear()
        {
            dictionaryLock.EnterWriteLock();
            try
            {
                this.mDictionary.Clear();
            }
            finally
            {
                dictionaryLock.ExitWriteLock();
            }
        }


        public virtual int Count
        {
            get
            {
                dictionaryLock.EnterReadLock();
                try
                {
                    return this.mDictionary.Count;
                }
                finally
                {
                    dictionaryLock.ExitReadLock();
                }
            }
        }


        public virtual bool ContainsKey(TKey key)
        {
            dictionaryLock.EnterReadLock();
            try
            {
                return this.mDictionary.ContainsKey(key);
            }
            finally
            {
                dictionaryLock.ExitReadLock();
            }

        }



        public virtual void Add(TKey key, TValue value)
        {
            dictionaryLock.EnterWriteLock();
            try
            {
                this.mDictionary.Add(key, value);
            }
            finally
            {
                dictionaryLock.ExitWriteLock();
            }
        }




        public virtual bool IsReadOnly
        {
            get
            {
                return false; //this.dict.is;
            }
        }


        public virtual IEnumerator GetEnumerator()
        {
            throw new NotSupportedException("Cannot enumerate a threadsafe dictionary. Instead, enumerate the keys or values collection");
        }
        #endregion
        }
}
