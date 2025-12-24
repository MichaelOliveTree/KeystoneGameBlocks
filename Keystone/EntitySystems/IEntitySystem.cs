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
	
    // Records allow us to simulate offscreen items without having to load
    // a full Entity.  However, for this to work, certain aspects of how Entity
    // DomainObject scripts needs to be considered.  It's obviously ok to not have to 
    // simulate a lot of graphical fx that a script would trigger in an Entity
    // but as far as Behavioral logic and things like damage fx, those could use a simpler
    // type of script that occurs at this sort of "lod" range.  Eg like switching from
    // a real time simulation to a dice roller battle result instead. Since the user can't see
    // the battle, why not use a more abstract and simpler battle result computation method?
    public interface DigestRecord 
    {
    	string ID {get; set;}
    	string ParentID {get;set;}
        string TypeName {get; set;}
        string Name {get; set;} // friendly
        Keystone.Types.Vector3d Translation {get; set;} 
        Keystone.Types.Vector3d GlobalTranslation {get;} // TODO: GlobalTranslation must take into account scale if "InheritScale"
    }
    
    public struct ModeledDigestRecord : Keystone.Simulation.DigestRecord
    {
    	public string ID {get; set;}
    	public string ParentID {get;set;}
        public string TypeName {get; set;}
        public string Name {get; set;} // friendly
        public Keystone.Types.Vector3d Translation {get; set;} 
        
        // TODO: i think each record should have it's global manually updated by the Digest if parent entity has transformed.  So we should
        //       override PropogateChangeFlags in Digest
        public Keystone.Types.Vector3d GlobalTranslation {get {return Translation;}} // TODO: GlobalTranslation must take into account parent scale if "InheritScale"
    }

	public interface IEntitySystemSubscriber
	{
		string[] Keys {get; set;}
		IEntitySystem[] Systems { get;}
		
		void Subscribe (IEntitySystem system);
	}
}
