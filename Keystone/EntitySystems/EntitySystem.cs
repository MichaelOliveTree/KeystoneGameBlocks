public namespace Keystone.EntitySystems
{

  public EntitySystem : Entity, IEntitySystem
  {
  		private int mSeed;
		private List<Int> mModifiedEntities; // some Entities in the System may have special modifications that must be set after they are restored via procedural generation
		
  		
  		// TODO: some digests may only need to re-store those records that have been modified and which
		//       have been "observed" (as in Quantum Mechanics) such that they must now appear in an area
		//       where player previously observed them.

		
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
	
	    public int Seed {get;}
  		
  		// TODO: not all systems need to ever store anything... i dont think this should be in the interface
  		// Based on file extension, database loading strategy changes.
  		public string DatabasePath {get;}
  		  		
  		public DigestRecord[] Records {get;}

		public double UpdateFrequency {get; set;}

		// TODO: I think if an Entity belongs to an EntitySystem it should have that set
		//       as an Entity.Attribute eg.  'Entity.Attributes.EntitySystemMemeber' flag

		// TODO: i think we need to store the path to each Entity prefab for each member?
		//       this will probably be part of a digest "record?"
		
		// this call assigns the IEntitySystem ID to the entity
  		public void Register (Keystone.Entities.Entity entity);
  		public void UnRegister (Keystone.Entities.Entity entity);

  		// 
  		public void Activate (Keystone.Entities.Entity entity);
  		public void DeActivate(Keystone.Entities.Entity entity);


		// TODO: we need to implement similar system for LOD where we can "Select" based on a rule that the user can define
      	public Entity[] SelectEntity(SelectionMode pass, double distance)
        {
        	using (CoreClient._CoreClient.Profiler.HookUp ("Entity System - Entity Selection"))
        	{
	           throw new NotImplementedException("EntitySystem.cs - 'SelectEntity()' not yet implemented");
        	}
        }

        // selects single Entity at specified index
        public Entity SelectEntity (uint index)
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
