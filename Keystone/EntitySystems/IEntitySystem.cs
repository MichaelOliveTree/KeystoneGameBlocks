using System;

namespace Keystone.EntitySystems
{
	/// <summary>
	/// An Entity System is a special type of Entity which can act as a fascade for 
	/// many different entities.
	/// The EntitySystem node can be added as a child any Region.  It behaves just
	/// like any other entity including it's simulation update however, it can 
	/// implement it's own update Frequency scheme to preserve CPU.
	/// </summary>
	public interface IEntitySystem  // IEntitySystem a type of fascade + flyweight pattern?
	{
		int Seed {get;}
		string ID {get; }
		string TypeName {get;}
		
		// TODO: not all systems need to ever store anything... i dont think this should be in the interface
		// Based on file extension, database loading strategy changes.
		string DatabasePath {get;}
		double UpdateFrequency {get; set;}
		
		DigestRecord[] Records {get;}

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
		
		// this call assigns the IEntitySystem ID to the entity
		void Register (Keystone.Entities.Entity entity);
		void UnRegister (Keystone.Entities.Entity entity);

		void Activate (Keystone.Entities.Entity entity);
		void DeActivate(Keystone.Entities.Entity entity);

		void Update (double elapsedSeconds);
		
		// TODO: not all systems need to ever store anything... i dont think this should be in the interface
		void Write();
		void Read();
	}
	
    
	public interface IEntitySystemSubscriber
	{
		string[] Keys {get; set;}
		IEntitySystem[] Systems { get;}
		
		void Subscribe (IEntitySystem system);
	}
}
