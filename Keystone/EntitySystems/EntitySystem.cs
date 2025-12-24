public namespace Keystone.EntitySystems
{

  public EntitySystem : IEntitySystem
  {
      private string ID {get; }
  		private string TypeName {get;}
  		
  		// TODO: not all systems need to ever store anything... i dont think this should be in the interface
  		// Based on file extension, database loading strategy changes.
  		private string DatabasePath {get;}
  		private double UpdateFrequency {get; set;}
  		
  		private DigestRecord[] Records {get;}
  
  		// TODO: a digest must be able to re-store serialized records and then to
  		//       match those with subscribers that are brought in.  Further, simulation dones in the iEntitySystem
  		//       should then be used to update those actual entities.
  		//       So subscription is something that occurs and adds a record if necessary.  But when paging out, the entity
  		//       is not unsubscribing, it is just allowing the IES to take over simulating that object.
  		//
  		// records should be as lightweight as possible, potentially records may exist as really simple pointers to storage (db or the xml)
  		//
  		// TODO: a physics system in many ways is similar in that it may simulate planet orbits and such in a more low frequency way
  		//       until entity live instance is loaded and not digested version... 


      public EntitySystem (string id) : Entity
        base(id)
      {
      
      }

      
  		// this call assigns the IEntitySystem ID to the entity
  		public void Register (Keystone.Entities.Entity entity);
  		public void UnRegister (Keystone.Entities.Entity entity);
  
  		public void Activate (Keystone.Entities.Entity entity);
  		public void DeActivate(Keystone.Entities.Entity entity);

      public Entity[] SelectEntity(SelectionMode pass, double distance)
        {
        	using (CoreClient._CoreClient.Profiler.HookUp ("Entity System - Entity Selection"))
        	{
	           throw new NotImplementedException("EntitySystem.cs - 'SelectEntity()' not yet implemented");
        	}
        }

        // selects single model at specified index
        public Entity SelectModel (uint index)
        {
        	using (CoreClient._CoreClient.Profiler.HookUp ("Entity System - Entity Selection"))
        	{
	           throw new NotImplementedException("EntitySystem.cs - 'SelectEntity()' not yet implemented");
        	}
        }
        
  		public void Update (double elapsedSeconds);
  		
  		// TODO: not all systems need to ever store anything... i dont think this should be in the interface
  		public void Write();
  		public void Read();
  }

}
