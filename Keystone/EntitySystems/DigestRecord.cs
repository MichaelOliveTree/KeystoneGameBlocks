public namespace Keystone.EntitySystems
{
    /// Records allow us to simulate offscreen items without having to load
    /// a full Entity.  However, for this to work, certain aspects of how Entity
    /// DomainObject scripts needs to be considered.  It's obviously ok to not have to 
    /// simulate a lot of graphical fx that a script would trigger in an Entity
    /// but as far as Behavioral logic and things like damage fx, those could use a simpler
    /// type of script that occurs at this sort of "lod" range.  Eg like switching from
    /// a real time simulation to a dice roller battle result instead. Since the user can't see
    /// the battle, why not use a more abstract and simpler battle result computation methods
    public interface DigestRecord
    {

      	string ID {get; set;}
    	  string ParentID {get;set;}
        string TypeName {get; set;}
        string Name {get; set;} // friendly
        Keystone.Types.Vector3d Translation {get; set;} 
        Keystone.Types.Vector3d GlobalTranslation {get;} // TODO: GlobalTranslation must take into account scale if "InheritScale"
    }

    public struct ModeledDigestRecord : Keystone.EntitySystems.DigestRecord
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
  }

}
