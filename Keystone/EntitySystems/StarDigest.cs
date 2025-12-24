//using System;
//using System.Text;
//using keymath.IO;
//using Keystone.Extensions;
//using Keystone.Types;


// TODO: this probably shouldn't be necessary as a simple EntitySystem or ModeledEntitySystem combined with user scripts should be enough to accomodate all types?
//       as opposed to having a special version for Stars which in this case is SciFi Command game specific.

//// 1 - universe creation starts with creation of Digest where we will Register stars
//// 2 - universe creation will add the Digest to the Scene as an IES (Entity System)
//// 3 - StarDigest IES will use scene name + .digest extension for db file by default?
//// 4 - StarDigest IES and all IES in Scene will get added to SceneInfo 
////	   - similar to how viewpoints are saved to SceneInfo?  Viewpoints are saved as child nodes though.  
////		 so how do we save IES array elements to SceneInfo since normally our scenewriter will automatically recurse child nodes of SceneInfo.
//// 	   - i  could add them as a property to SceneInfo where serialization getproperties and setproperties will be reading from scene.cs internal property
////		 - for now let's do that as it's easiest
//// 5 - 
//namespace Keystone.EntitySystem
//{
//	public class StarDigest :  Entities.ModeledEntity , IEntitySystem // IEntitySystem a type of fascade + flyweight pattern?
//	{
//		internal struct SectorInfo
//		{
//		}
//		
//		// TODO: given the following Digest Goals, how do we implement a galaxy of stars.
//		//		 - for now, no octree.
//		//		 - a ModelSequence with a star at each Model?
//		//			- we want to be able to mouse pick and differentiate between each picked Model.		
//		//		- an Entity at each Sector? N draw calls.
//		//			- if an Entity at each Sector, then might as well Cull and scale positions right then and there.
//		//		- we want to be able to Simulate against these stars server side without models/geometry.
//		//		- First priority should be to get camera in good range to see entire universe, and to scale Positions of Stars 
//		//		  to be in close together.
//		//		- what if the digest data itself were scaled?
//		//		- can we generate the digest for the Nav hud only?  Because
//		//			we need a different form of the digest for nav vs editor.
//		//			that is, it needs to be scaled for Nav but not for Editor workspace.
//		//			in terms of MVC, the actual star info and star digest database is one thing (model), but then
//		//			the scaling needs to be different, so that should be done in the View.  When we iterate
//		//			through the records, we need to move the verts or minimesh elements to the scaled position.
//		//			- then we need a Zoom function to zoom in on the select star to take us from galaxy view to system view.
//		//			  how does that work? We alter the mGalaxyScale to be larger as we zoom?
//		//			- lets try with minimesh spheres...
//		//				what do we need to do?
//		//				- let's say we open the digest on disk, load it, then for each record
//		//				instance a minimesh sphere.  For the position we scale it by mGalaxyScale.  
//		//				We use a 1 meter sphere for now.
//		
//		//
//	
//		internal Octree.StaticOctree <SectorInfo> mOctree;
//		
//		// - Digest goals
//		//   0) if each type of digest is really a highly customizable Entity node type who's main goal is efficiency, then
//		//      i suspect they will be mostly EXE side objects.  But i dont want to be forced to script their functionality for
//		//      manipulating their visuals.
//		//	 1) provide an efficient simulation model and interface to many entities using just a single entity
//		//		thus avoiding having to do full simulations or load full visual models and interfaces.
//		//		a) user can query the simplified version and get full information from the database as temporary data.
//		//	 2) if applicable, fascilitate construction of abstract visual representations of those entities 
//		//   3) provide mechanism for owning and disowning control of these entities.  If an entity is registered to a system
//		//      it gives up all responsibility of ITSELF to the system.
//		//		a) As a result, spatial/collision, physical, animation, audial, visual cpu cycles are spared.
//		//   4) The system itself determines when an entity can be released to itself as well as when it must turn itself in.
//		//		- this requires more than a checkin/checkout model.  Seems to me the system itself needs to be in charge of
//		//		leashing/unleashing
//		//   5) updating of visual elements?
//		//		- animation system is the first example of modular entity controllers over visual elements.
//		//      - what about the rendering system approach?  problem is our current system with minimesh walls and floors
//		//        is that yes we have individual walls and floors, and yes our picking is special cased, and yes the models
//		//		  are manually manipulated by the owning entity directly...  and whats wrong with any of that?
//		//        - examining the Interior celledregion way of doing things more closely, it also very specifically is designed
//		//        to be efficient for handling interiors.  We abandon elegance there as far as 1:1 wall to entity and such.
//		//		  - so potentially, we can just manually create the Model and it's vertices...  i think it's the easiest way
//		//        and a good starting point.   
//		//    6) Digests are entities, but their updates are handled differently?
//		//		- stardigest has a visual model so should inherit ModeledEntity?  So long as it implements IEntitySystem though yes?
//		// 	  7) To an extent, thinking about this system which seems to really force greater seperation of Entity and Visual Model 
//		//       has a lot of advantages especially when it comes to efficiency.  The visual and physical model children of the Entity
//		//       are great, but when not needed, just a simple entity record is all you need along with a system to switch from Entity level
//		//       AI/Behavior/Animation to a more abstract simulation model.
//		
//        // 	 8)	an IEntitySystem must be attached to the Root node so that it is actually apart of the scene and gets a SceneNode
//        //      so that EntityAttached occurs which will initiate paging of resources, etc
//        
//        
//        // TODO: what about a DataTable instead?
//		// Using a DataTable would make it easier to have custom Record types.
//		// DataTable sourceTable = new DataTable()
//		// http://madskristensen.net/post/do-more-with-a-datatable-and-improve-performance
//		// http://www.codeproject.com/Tips/665519/Writing-a-DataTable-to-a-CSV-file
//		// TODO: or if each implementation of Record knew how to convert itself to a CSV
//		    // Records allow us to simulate offscreen items without having to load
//    // a full Entity.  However, for this to work, certain aspects of how Entity
//    // DomainObject scripts needs to be considered.  It's obviously ok to not have to 
//    // simulate a lot of graphical fx that a script would trigger in an Entity
//    // but as far as Behavioral logic and things like damage fx, those could use a simpler
//    // type of script that occurs at this sort of "lod" range.  Eg like switching from
//    // a real time simulation to a dice roller battle result instead. Since the user can't see
//    // the battle, why not use a more abstract and simpler battle result computation method?
//
//	        
//	        // ancestors - idea here is so we can know parent stellar system and navigate the digest?
//	        //           - BAD - better idea is to simply put all celestial bodies in the digest including the System, the Stars, Worlds, Moons
//	        //           and by making this derived IEntitySystem very particular to our needs here, these records can also contain enough info
//			//           for us to calculate their positions.  Indeed, we should completely (for now) ignore optimizing this too much and focus on
//			//           0.1 alpha version.			
//	        
//	        //public string[] AncestorID;
//	        //public string[] AncestorTypeName;
//	        //public Transform[] AncestorTranslation;
//	
//	        //
//	        //public object[] Values; 
//	
//	        // for a star, we don't really want to combine full world translation
//	        // because we dont have the precision in meters.  well.  We do want to use
//	        // lightyear positioning.  Or perhaps AUs (1 LY = 63 239.7263 AU)
//	        // - also, shouldn't all Translation values for a DigestRecord be 
//	        // Region/Zone specific
//	        //      - well how can they be for a Star where inherently they are Region specific.
//	        //      However, the answer is simply, so what.  For star digests, we will allow much more
//	        //      tracking of info and allow a domainobject script where we can update each of them.
//	        //      but still hold enough data in the digest to know how to compute that info.
//	        //      - So one step at a time, let's create the digest, store the Zone ID, System ID,
//	        //      translations for Zone and System.
//	        //      In fact, maybe we store the hierarchy to the root as far as ID, TypeName, Translation.
//	        //      for stars, and later we'll add orbital info, mass, and then we can compute actual
//	        //      updated orbits.
//	        //      - if it's a star in a sub-system, then we also need the orbital info for each
//	        //      ancestor, not just the translation.  On the other hand, maybe we don't care
//	        //      about simulating the orbits when there are no users near them?  And at any given
//	        //      moment in time, since our orbits are hierarchical and based on elipses and an epoch
//	        //      we can always find the exact position.  So, here we should NOT compute orbits.
//	        //      We just need the final position in AU.  But we do want ancestors so we can
//	        //      navigate through the digest.  But for now we wont worry about that either.
//	        //      JUST_STARS_IN_AU
//	        //
//	        // - also, shouldn't all digest entries be of the same type and use 
//	        // same DomainObject (so can use the same simplified DigestRecord version of the script)
//	        // - also, how do we deal with moving DigestRecord represented entities that are crossing
//	        // zones and such?  Are we not really complicating things by using DigestRecords 
//	        // and not just having our Digest store array of Entities even if those Entities are
//	        // a simplified version of the Entity? 
//	        //      - these entities don't need SceneNodes for culling
//	        //      - they don't need any models, textures
//	        //      - they maybe DO need DOmainObject Scripts
//	        //      - we would still need to seperately track the ParentID and ParentTypeName i think
//	        //        because an Entity stored in a Digest is not hierarchically attached.
//	        // - HOW DO I do zone crossing if I'm not even loading any SceneNode since spatial location 
//	        // management is largely handled by SceneNodes.
//
//	        // NOTE: No matrix, no physics, no scale and rotations.
//	        // We MUST keep the philosophy behind a digest really abstract with no basis on
//	        // visuals or physics.  
//	        // If we end up having to move Entities across Zones, eg for ships
//	        // then it should be done statistically, not via simulated movement.
//	        // Anytime we do intend to model ACTUAL ship movement, it must use full blown Entity.
//	        // Thus ships in neighboring loaded Zones we will fully update but maybe at much reduced
//	        // Hertz.
//	
//	        
//
//		
//		protected string mDatabasePath; // 
//		protected double mFrequency; 
//		private DigestRecord[] mRecords;
//			
//		
//    // TODO: A future derived version of our Digest could be used to represent entities
//    // that ONLY exist in Digest form.  These can be things like characters which 99.9% of the time
//    // are just records and the player never meets (eg never becomes a crewmember of an npc they 
//    // encounter) but adds depth to the universe.
//    // TODO: I think the ModeledEntity aspect should be seperate.  The visual model should be seperate from a Digest EntitySystem (IEntitySystem)
//		public StarDigest (string id) : base (id)
//		{
//			Serializable = true;
//			Shareable = false;
//
//			// NOTE: id is file relative path off of the Data\\Scenes folder			
//			mDatabasePath = GetDatabasePath(id);
//		}
//		
//
//        internal static string GetDatabasePath(string relativePath)
//        {
//            // TODO: currently when generating universe we are given a temp file and that file points to a full path
//            // and trying to merge that path with this one gives us a problem.  we need a final name and then even if
//            // the universe filename is changed, we still need the digest to be in the same path
//            string path = System.IO.Path.Combine (CoreClient._Core.ScenesPath,  relativePath);
//            return path;
//        }
//        
//		// TODO: records should be private and what we return on a query is what?
//		// quicklook type info? we read/load/generateHTML?
//		public DigestRecord[] Records { get { return mRecords; } }
//		
//		public int RecordCount 
//		{
//			get 
//			{
//				int recordCount = 0;
//            	if (mRecords != null) recordCount = mRecords.Length;
//            	return recordCount;
//			}
//		}
//		
//		public string DatabasePath 
//		{
//			get {return mDatabasePath;}
//		}
//		
//		public double UpdateFrequency 
//		{
//			get {return mFrequency;}
//			set {mFrequency = value;}
//		}
//				
//		#region Resource Members
//		/// <summary>
//        /// 
//        /// </summary>
//        /// <param name="specOnly">True returns the properties without any values assigned</param>
//        /// <returns></returns>
//        public override Settings.PropertySpec[] GetProperties(bool specOnly)
//        {
//            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
//            Settings.PropertySpec[] properties = new Settings.PropertySpec[2 + tmp.Length];
//            tmp.CopyTo(properties, 2);
//
//            properties[0] = new Settings.PropertySpec("databasepath", typeof (string).Name);
//            properties[1] = new Settings.PropertySpec("frequency", mFrequency.GetType().Name);
//
//            if (!specOnly)
//            {
//                properties[0].DefaultValue = mDatabasePath;
//                properties[1].DefaultValue = mFrequency;
//            }
//
//            return properties;
//        }
//
//        public override void SetProperties(Settings.PropertySpec[] properties)
//        {
//            if (properties == null) return;
//            base.SetProperties(properties);
//
//            for (int i = 0; i < properties.Length; i++)
//            {
//                if (properties[i].DefaultValue == null) continue;
//                // use of a switch allows us to pass in all or a few of the propspecs depending
//                // on whether we're loading from xml or changing a single property via server directive
//                switch (properties[i].Name)
//                {
//                    case "databasepath":
//                        mDatabasePath = (string)properties[i].DefaultValue;
//                        break;
//                    case "frequency":
//                        mFrequency = (double)properties[i].DefaultValue;
//                        break;
//                }
//            }
//        } 
//        #endregion
//		
//
//		#region IPageableTVNode Members
//        public override void LoadTVResource()
//        {
//        	try
//        	{
//            	base.LoadTVResource(); // base class will load any script
//       
//            	System.Diagnostics.Debug.WriteLine("StarDugest.LoadTVResource() - Begin Read");
//	
//            	
//            	// load the database which contains all records and allows us to skip paging in every region temporarily to discover the registered entities
//            	this.Read ();
//            		            
//	            System.Diagnostics.Debug.WriteLine("StarDugest.LoadTVResource() - End Read");
//        	}
//            catch (Exception ex)
//            {
//				System.Diagnostics.Debug.WriteLine("StarDigest.LoadTVResource() - ERROR: " + ex.ToString());
//            }
//        }
//        
//		#endregion
//		
//
//		public void Register (Celestial.StellarSystem stellarSystem)
//		{
//			if (stellarSystem.StarCount > 0)
//			{
//				for (int i = 0; i < stellarSystem.StarCount; i++)
//					Register (stellarSystem.Stars[i]);
//			}
//			
//			if (stellarSystem.SubSystemCount > 0)
//				for (int i = 0; i < stellarSystem.SubSystemCount; i++)
//					Register (stellarSystem.Systems[i]);
//			
//		}
//		
//		// TODO: how does register deal with brand new entities that are created by simulation
//		//       at runtime for the first time ever, and need to become part of an EntitySystem?
//		//       how are the visuals handled? Well, we can still dynamically create new meshes if no
//		//       free slots...  definetly nice though when actual server side doesnt have to care.
//		//       -What if we use an event mechanism to generate the meshes somewhere else? doesnt really matter 
//		//       for now... its just another level of indirection (aka kicking the can down the road)
//		//    - clearly one of the issues with managing the visuals  here is dealing with
//		//      updating it.  But i suppose i just need to say "FUCK IT" and get a 0.1 version of this
//		//      system working... including serializing and deserializing
//		
//		public void Register (string id, string parentID, string typeName, string name, Vector3d translation)
//		{
//			ModeledDigestRecord record = new ModeledDigestRecord();
//			record.ID = id;
//			record.ParentID = parentID;
//			record.TypeName = typeName;
//			record.Name = name;
//			record.Translation = translation;
//			mRecords = mRecords.ArrayAppend(record);
//			// TODO: the entity who's id = "id" is not instantiated and so 
//			//       how can we "subscribe" this system to it so that the entity
//			//       knows it's part of a system and that it's simulation is done there?
//		}
//		
//		public void Register (Keystone.Entities.Entity entity)
//		{
//			ModeledDigestRecord record = new ModeledDigestRecord();
//            record.ID = entity.ID; 
//            record.ParentID = entity.Parent.ID;
//            record.TypeName = entity.TypeName;
//            record.Name = entity.Name;
//           // record.RegionOffset = null;
//           // if (entity.Region is Keystone.Portals.Zone)
//           // 	record.RegionOffset = ((Keystone.Portals.Zone)entity.Region).Offset;
//           // record.DigestControlled = true;
//           record.Translation = entity.GlobalTranslation; //  This is globaltranslation of a star we're setting
//           // starColor -> use this to influence the color of the pointsprite?
//            
//			// TODO: we should not do this.  Instead, when we register an entity
//            // we should specify which PropertySpecs array elements are to be tracked in the Digest
//            // and then when registering the entity, store those values (we dont need to store
//            // PropertySpec's specifications for every entity, just values so long as all entities
//            // are of the same type (eg have same domainobject script)
//            //
//            // id
//            // typename
//            // friendly name
//            // Label
//            // color / temperature
//            // system center position
//            // zone offset
//            // we do need orbital info though too, period, eccentricity, current epoch
//            //    or what?  
//            mRecords = mRecords.ArrayAppend(record);
//
//            // TODO: what if instead of subscribing, we kept all this data in a database then we
//            //		 can truly store huge amounts of entities.  We'd still keep a copy of the record
//            //		 here in the Digest?  Maybe not... could always do db query when needing info.
//            //			- we'll have to see as we flesh out the system more.
//            //			- then for simulation, we can simply apply calculations against the database.
//            // TODO: when entity is subscribed, it will automatically register with the entitysystem
//            //       when it is paged out and unregister when it pages back in.
//            //       Question is, do we need to distinguish beteen RemoveChild to page out vs to delete?
//            //       That is something we never fully resolved but we'll kick the can some more
//            entity.Subscribe (this);  // TODO: subscribe is completely unimplemented
//            
//		}
//		
//		public void UnRegister (Keystone.Entities.Entity entity)
//		{
//		}
//
//		
//		public void Activate (Keystone.Entities.Entity entity)
//		{
//		}
//		public void DeActivate(Keystone.Entities.Entity entity)
//		{
//		}
//		
//		
//		
//		
//		// HUD vs Simulation
//		//	- the idea here is that a Digest is part of Simulation, not part of HUD really...
//		//  - Therefore starfield proxy pointsprite model is managed by HUD and can easily be
//		//    updated to reflect camera zoom level.
//		public override void Update(double elapsedSeconds)
//		{			
//			base.Update(elapsedSeconds);
//					
//			// NOTE: Keystone.Elements.EntitySystemModel.Render() is where camera space translation
//			//       of each vertex occurs!  EntitySystemModel is a type of Model that puts 1:1 relationship
//			//       between Vertex and an EntitySystem Record
//						
//		}
//		
//		
//	
//		private const string DELIMIETER = ";";
//		public void Write()
//		{
//			// we want to write the database for now as comma seperate list
//			// later we might switch to binary
////			var writer = new CsvWriter (mDatabasePath);
//            
//			// http://www.codeproject.com/Articles/685310/Simple-and-fast-CSV-library-in-Csharp
//
//			if (System.IO.File.Exists(mDatabasePath))
//				System.IO.File.Delete (mDatabasePath);
//            
//            
//			System.IO.File.Create(mDatabasePath).Close();
//            
//
//			if (mRecords == null || mRecords.Length == 0) return;
//			
//            string[][] output = new string[mRecords.Length][];
//            
//            for (int i = 0; i < mRecords.Length; i++)
//            	output[i] = new [] 
//            				{
//            					mRecords[i].ID, 
//            					mRecords[i].TypeName,
//            					mRecords[i].Name,
//            					mRecords[i].Translation.ToString()
//            				};
//            
//            int length = output.GetLength(0);
//            StringBuilder sb = new StringBuilder();
//            for (int index = 0; index < length; index++)
//            	sb.AppendLine(string.Join(DELIMIETER, output[index]));
//            
//            System.IO.File.AppendAllText(mDatabasePath, sb.ToString());
//            System.Diagnostics.Debug.WriteLine("StarDigest.Write() - Database Write Completed.");
//		}
//		
//		public void Read()
//		{
//			// we want to read the database for now as csv
//			// later we might switch to binary format or even sqlite.. who knows
//			string filename = mDatabasePath; // @"c:\temp\csv.csv";
//			string[] text = System.IO.File.ReadAllLines(filename);
//            
//			if (text == null || text.Length == 0) return;
//			
//			mRecords = new Keystone.Simulation.DigestRecord[text.Length];
//			
//			for (int i = 0; i < text.Length; i++)
//			{
//				mRecords[i] = new ModeledDigestRecord();
//				string[] values = text[i].Split(new string[] {DELIMIETER}, StringSplitOptions.None);
//				
//				mRecords[i].ID = values[0];
//				mRecords[i].TypeName = values[1];
//				mRecords[i].Name = values[2];
//				mRecords[i].Translation = Vector3d.Parse (values[3]);
//			}
//			
//			//var scanner = new RegexCsvScanner(DELIMIETER, text);
//            //var recordHandler = new ConsoleCsvRecordHandler();
//            //var parser = new CsvParser(scanner, recordHandler);
//            //parser.ParseRecords();
//            System.Diagnostics.Debug.WriteLine("StarDigest.Read() - Database Read Completed.");
//		}
//
//		
//        
//// OBSOLETE - We use Mesh3d.AdvancedCollide which has branch for traversing pointsprites in either 3D or 2D screenspace
////        /// <summary>
////        /// Collision offered for faces or individual point sprites with a proxy Entity being offered
////        /// for part of the collision result.
////        /// </summary>
////        /// <param name="start"></param>
////        /// <param name="end"></param>
////        /// <param name="worldMatrix"></param>
////        /// <param name="parameters"></param>
////        /// <returns></returns>
////        public override Keystone.Collision.PickResults Collide(Keystone.Types.Vector3d start, Keystone.Types.Vector3d end, Keystone.Types.Matrix regionMatrix, KeyCommon.Traversal.PickParameters parameters)
////        {
////            // http://www.blitzbasic.com/Community/posts.php?topic=45874
////
////            // http://forum.libcinder.org/topic/fast-object-picking-using-multiple-render-targets
////            // 3D - Collision
////            // 2D Collision implemented above.
////            Keystone.Collision.PickResults pickResult;
////            pickResult = new Keystone.Collision.PickResults();
////
////            Vector3d dir = Vector3d.Normalize(end - start);
////            Ray r = new Ray(start, dir);
////
////            for (int i = 0; i < mRecords.Length; i++)
////            {
////                // model space picking, but i think to do screenspace, we do have to transform every
////                // vertex to camera space
////            //    Vector3d screenPos = _context.Viewport.Project(item.CameraSpacePosition + mRecords[i].Translation);
////
////                Vector3d impactPoint = Vector3d.TransformCoord(mRecords[i].Translation, regionMatrix);
////            //    Vector3d screenPos = _context.Viewport.Project(impactPoint);
////
////                // compare the distance between 2d mouse position with the screenpos
////
////                // if the distance is closer than last, set the new one as pickResult
////                // else continue
////                pickResult.FaceID = i;
////                pickResult.VertexID = i;
////
////                pickResult.SetEntity(mRecords[i].ID, mRecords[i].TypeName);
////                pickResult.HasCollided = true;
////                pickResult.CollidedObjectType = KeyCommon.Traversal.PickAccuracy.Vertex;
////
////                pickResult.ImpactPointLocalSpace = mRecords[i].Translation; // TODO: are records local space?
////                pickResult.ImpactNormal = -dir;
////                
////                // TODO: verify this DistanceSquared computation is correct.  I think it is since it seems correct in Actor3d.cs and Mesh3d.cs where we convert model space back to region space and find distance squared
////                pickResult.DistanceSquared = Vector3d.GetDistance3dSquared(Vector3d.TransformCoord(start, regionMatrix), impactPoint);
////                //     System.Diagnostics.Debug.WriteLine ("Pick distance to cell = "+ _lastPickResult.DistanceSquared.ToString ());
////                pickResult.Matrix = regionMatrix;
////            }
////            // we already have plenty of code to unproject the position of an entity (eg to draw icon)
////            // so we simply need to take the camera relative position for each vertex
////            // and project that to get our 2d point.
////            // then we iterate through all of them and find the ones that intersect mouse
////            // and of those, the one closest to camera
////            // 
////            // then we store that entityID in pickresult
////            // 
////            // TODO: i dont believe our nav is even making use of any picking at all
////
////            return pickResult;
////
////            // normally 
////            //return base.Collide(start, end, worldMatrix, parameters);
////
////            // TODO: hrm... normally we'd pick through the geometry
////            // but in case of our digest, each vertex represents a different underlying
////            // Entity and it's that data we wish to return, not just mesh vertex cuz that
////            // really tells us very little.
////        }
//
//        #region IDisposable Members
//		protected override void DisposeManagedResources()
//		{
//			base.DisposeManagedResources();
//		}
//		#endregion
//	}
//}
