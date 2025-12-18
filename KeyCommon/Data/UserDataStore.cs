using System;
using System.Collections.Generic;

namespace KeyCommon.Data
{
	/// <summary>
	/// Stores ALL UserData objects for all loaded Entities.  
	/// This is necessary so that our DataProcessors can grab the appropriate
	/// parameters required for a DataProcessor delegate, for all Entities/Components
	/// that are being processed.
	/// </summary>
	public class UserDataStore
	{
		private Dictionary<string, UserData> mUserDataCollection;
		
		public UserDataStore()
		{
		    mUserDataCollection = new Dictionary<string, UserData>();
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
		    object value;
		    bool success = mUserDataCollection.TryGetValue(entityID, out value);
		    
		    if (success) throw new Exception ("Dictionary Key Already Exists.");
		    
		    UserData data = new UserData();
		    
		    mUserDataCollection.Add (entityID, data);
		    return data;
		}
		
		public void CheckIn (string entityID, UserData data)
		{
		    if (string.IsNullOrEmpty(entityID) || data == null) throw new ArgumentOutOfRangeException();
		    
		    object value;
		    bool success = mUserDataCollection.TryGetValue(entityID, out value);
		    
		    if (object != data) throw new ArgumentOutOfRangeException();
		    
		    mUserDataCollection.Remove (entityID);
		    data.Dispose();
		}
	}
}