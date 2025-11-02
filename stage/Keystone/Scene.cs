//#define TVPhysics

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Entities;
using Keystone.IO;
using Keystone.Portals;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Elements;
using KeyCommon.DatabaseEntities;
using Keystone.Extensions;
using System.IO;

namespace Keystone.Scene
{
    public class Scene : Group, IDisposable // Feb.13.2014 - SceneBase renamed to Scene.  Also now inherits Group.  It can accept two children - Root and SceneInfo.
    {

        public delegate void EntityAdded(Entity parent, Entity child);
        public delegate void EntityRemoved(Entity parent, Entity child);

        // client notifications
        public EntityAdded mEntityAddedHandler;
        public EntityRemoved mEntityRemovedHandler;
       
        // internal access - required by Simulation.cs
        internal List<Entity> mEntities;
        internal List<Entity> mServerEntities;
        internal List<Entity> mActiveEntities;
        internal List<Entity> mChangedNodes; 

        protected SceneManagerBase mSceneManager;
        protected Simulation.ISimulation mSimulation;

        // a root region that hosts an unbounded Region always exists because its required
        // for portals which need to link to the root region
        protected Root mRoot;

        protected XMLDatabase mXMLDB;
        protected PagerBase mPager;

        protected bool mSceneLoaded = false;

        protected SceneInfo mSceneInfo;

        internal Keystone.AI.Connectivity mConnectivityGraph;
        


        public static Scene CreatePreviewScene (SceneManagerBase sceneManager, string id)
        {
        	Scene scene = new Scene (sceneManager, id, null);
        	scene.mPager = null;
            sceneManager.mScenes.Add(id, scene);
        	return scene;
        }
        
        public static Scene CreateClientScene (SceneManagerBase sceneManager, string folderName, Keystone.Simulation.ISimulation simulation, 
                                               PagerBase.PageCompleteCallback emptyRegionPageCompleteHandler, PagerBase.PageCompleteCallback regionChildrenPageCompleteHandler)
        {
        	Scene scene = new Scene (sceneManager, folderName, simulation);
            sceneManager.mScenes.Add(folderName, scene);

        	scene.mPager = new IO.ClientPager(scene, Core._Core.ThreadPool, Core._Core.PagerConurrency, emptyRegionPageCompleteHandler, regionChildrenPageCompleteHandler);

            string fullPath = Path.Combine(Core._Core.ScenesPath, folderName);
            scene.Open(fullPath);
        	return scene;
        }
        
    	public static Scene CreateServerScene (SceneManagerBase sceneManager, string folderName, Keystone.Simulation.ISimulation simulation,
                                               PagerBase.PageCompleteCallback emptyRegionPageCompleteHandler, PagerBase.PageCompleteCallback regionChildrenPageCompleteHandler)
        {
        	Scene scene = new Scene (sceneManager, folderName, simulation);
            sceneManager.mScenes.Add(folderName, scene);

            scene.mPager = new IO.ServerPager(scene, Core._Core.ThreadPool, Core._Core.PagerConurrency,  emptyRegionPageCompleteHandler, regionChildrenPageCompleteHandler);

            string fullPath = Path.Combine(Core._Core.ScenesPath, folderName);
            scene.Open(fullPath);
        	return scene;
        }
        
    	
        private Scene(string id) : base (id)
        {
        }
        
    	/// <summary>
    	/// Scenes are never instantiated by Repository.Create().  They are always either generated
    	/// or returned as a result of a "CreateClientScene()" static call.
    	/// </summary>
    	/// <param name="sceneManager"></param>
    	/// <param name="id"></param>
    	/// <param name="simulation"></param>
        internal Scene(SceneManagerBase sceneManager, 
    	               string id,
    	               Keystone.Simulation.ISimulation simulation)
        : this(id)
        {
            if (sceneManager == null) throw new ArgumentNullException();
            
            mSceneManager = sceneManager;
  
            // Simulation can be null but it must be assigned before we Deserialize Saved scene. 
            // Preview viewports dont need to load serialized scene.
            mSimulation = simulation;
            mSimulation.Scene = this;
            mServerEntities = new List<Entity>();
            mChangedNodes = new List<Entity>(32);
            mActiveEntities = new List<Entity>();
            
            // hrm... could be interesting if we can inherit the gravity setting of the current node.
            mEntities = new List<Entity>();
        }

        #region ITraverser Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }
        #endregion


        protected void RegionPageInCompleteHandler(Amib.Threading.IWorkItemResult result)
        {
            Keystone.IO.ReadContext rc = (Keystone.IO.ReadContext)result.State;
            string nodeID = rc.NodeID;


        }

        protected virtual void Open(string fullFolderPath)
        {
            try
            {
                
                mXMLDB = new XMLDatabase();
                mSceneInfo = mXMLDB.Open(fullFolderPath, true);
            }
            catch (Exception ex)
            {
                mSceneLoaded = false;
                mSimulation.Running = false;
                Debug.WriteLine("SceneBase.Open() - Error opening scene archive. " + ex.Message);
                return;
            }
        }

        // TODO: can we make all "Load()" path static so that 
        // we can directly deserialize Scene instead of SceneInfo? and assign it's Root and then have it's root automatically
        // deserialized by virtue of it being a child of Scene?  I should fork and do this...
        public void Load()
        {
            try 
            {
                if (mXMLDB == null) throw new Exception("SceneBase.Load() - Scene db must be opened first.");

                mSceneLoaded = true; // set to true prior to attempting to load any data from Resource
                                     // since Repository will not call IPageable:LoadTVResource() if
                                     // the scene is not loaded.

                                     // NOTE: recursing ZoneRoot is perfectly fine because child Zones are never saved as children of the ZoneRoot
                                     //       however, other node types like Viewpoints or IEntitySystem CAN be saved to ZoneRoot and must be restored
                                     //       by recursing.
                                     // TODO: when dynamically editing Zones, I do need to verify that i am indeed NOT saving them by calling
                                     //       SaveNode () on ZoneRoot!  So when Pager AddChilds them, it should never save starting at root
                                     //       only starting at Zone.  Furthermore, no changes to Zone's children should cause propogation
                                     //       up to ZoneRoot that would cause it to Save their either.
                Node result = mXMLDB.ReadSynchronous(mSceneInfo.FirstNodeTypeName, null, true, false, null, true, false);

                mRoot = (Root)result;
      
                // must call this manually on root to get it's RegionNode created so that
                // child entities can then have their scenenode's attached under it
         //       _root.SetChangeFlags(Enums.ChangeStates.EntityAttached, Keystone.Enums.ChangeSource.Self);
                EntityAttached(mRoot, this);
                // TODO: this flag must be canceled, but it's not... 

                // nodes are added to the Repository when they are constructed but only ref count is incremented
                // if it's manually done or added to the scene.  Here we do it manually so that on DecrementRef it
                // will automatically be removed from Repository
                Repository.IncrementRef(mRoot);


                // MapLayerGrid and Connectivity Graph for Structural (land) based Multi-Zone Scenes
                if (this.mRoot is ZoneRoot)
                {
                    ZoneRoot zr = (ZoneRoot)this.mRoot;
                    if (zr.StructureLevelsHigh > 0)
                    {
                        System.Diagnostics.Debug.Assert(zr.RegionsHigh == 1, "TileMap.Structure for zones use levels for 3-D and must be set in a 1-D zone height game world.");

                        mMapGrid = new Keystone.TileMap.MapLayerGrid(zr, (int)zr.RegionsAcross, (int)zr.StructureLevelsHigh, (int)zr.RegionsDeep);
                        Core._Core.MapGrid = mMapGrid;

                        // NOTE: this path only occurs for structural (land) based multi-zone scenes and 
                        // NOT EXTERIOR Space Simulation Zones.  
                        mConnectivityGraph = new Keystone.AI.Connectivity((int)zr.RegionsAcross, (int)zr.RegionsDeep);
                        Core._Core.MapGrid.Attach(mConnectivityGraph);
                    }
                }

            }
            catch (Exception ex)
            {
                mSceneLoaded = false;
                Trace.WriteLine("SceneBase.Load() - Error loading scene from archive. " + ex.Message);
            }
        }

        public void Unload()
        {
            this.DisposeManagedResources();
        }


        /// <summary>
        /// This only saves the filemaps.  If Entities or other nodes are not being saved, it is because
        /// SaveNode(Node node) is not being called from ProcessCommandCompleted() 
        /// when Nodes are added, removed or changed and when AppMain._core.Scene_Mode == Keystone.Core.SceneMode.EditScene
        /// </summary>
        public void Save()
        {
            if (CoreClient._Core.Scene_Mode != Core.SceneMode.EditScene) return;
            mXMLDB.SaveAllChanges();
            Debug.WriteLine("Scene.Save() - COMPLETED.");
        }

        // copies from prefab
        public static void CopyInteriorDataFile(Container container, string prefabRelativePath)
        {
            if (container == null) return;

            string containerID = container.ID;

            string interiorDBRelativePath = prefabRelativePath;
            interiorDBRelativePath = Path.ChangeExtension(interiorDBRelativePath, ".interior");
            // todo: what if the relative interiorDBRelativePath is in the \\Scenes\\ folder and we want to copy to \\saves\\?
            string interiorDBPath = Path.Combine(Core._Core.ModsPath, interiorDBRelativePath);

            if (File.Exists(interiorDBPath))
            {
                // todo: what if we want to copy to the \\scenes\\ path ?
                string savePath = Path.Combine(Core._Core.SavesPath, Core._Core.Scene_Name);
                string destinationInteriorDBPath = Path.Combine(savePath, containerID + ".interior");
                File.Copy(interiorDBPath, destinationInteriorDBPath);
                
                container.Interior.SetProperty("datapath", typeof(string), Core._Core.Scene_Name + "\\" + containerID + ".interior");
            }
        }
 
        // todo: this belongs in Keystone.IO
        // writes to \\saves\\ path, temp\\ path or scenes\\ path but NOT PREFAB PATH
        // NOTE: Containers are never saved to the scene.xml. Because they are dynamic, they are
        // only stored in the \]\saves\\ path and are spawned by the server which looks them up in the save.db by username
        // todo: after moving to Scene.cs add an SAVE_LOCATION enum for the relative path to use \\saves\\  \\modspath\\  or \\temp\\  or \\scenes\\
        // writeEntity and WriteContainer are basically the same code.  Should be merged.
        // also i should add this as a methos in Scene.cs
        public static void WriteEntity(Entity entity, bool incrementDecrement)
        {
            if (entity == null) return;

            string entityID = entity.ID;
            string savePath = Path.Combine(Core._Core.SavesPath, Core._Core.Scene_Name);
            System.Diagnostics.Debug.Assert(entity.SRC == null);
            string fullSavePath = Path.Combine(savePath, entityID + ".kgbentity");

            XMLDatabaseCompressed entityDB = new XMLDatabaseCompressed();
            entityDB.Create(SceneType.Prefab, fullSavePath, entity.TypeName);

            // todo: should i just force inline all Nodes?  we dont need to save incrementally, only when the user clicks "save" or quits the simulation. The current state is in memory.
            entityDB.WriteSychronous(entity, true, incrementDecrement, false); // we dont incrementdecrement because unlike generating a scene or universe, the target node already exists in our scene and we want to keep it
            entityDB.SaveAllChanges();
            entityDB.Dispose();
        }   

        public static void DeleteEntityFile(string entityID)
        {
            string savePath = Path.Combine(Core._Core.SavesPath, Core._Core.Scene_Name);
            string fullSavePath = Path.Combine(savePath, entityID + ".kgbentity");

            System.IO.File.Delete(fullSavePath);
        }

        public SceneManagerBase SceneManager { get { return mSceneManager; } }

        public XMLDatabase XMLDB { get { return mXMLDB; } }

        public Keystone.Simulation.ISimulation Simulation { get { return mSimulation; } }

        public PagerBase Pager { get { return mPager; } }

        public bool PagerEnabled {get; set;}
        
        public Viewpoint[] Viewpoints
        {
            get
            {
                // IMPORTANT: This does not return all created Viewpoints, only the ones in SceneInfo.
                // So all dynamically cloned viewpoints (which always use .Serializable = false) are not returned here.
                // For all created viewpoints, look in Scene.ActiveEntities() but they will be mixed in with
                // all other entities so you need to filter the traversal.  Should I modify this
                // getter to find and return just the Viewpoints filtered out of ActiveEntities?
                // 
                if (mRoot == null) return null;
                return mSceneInfo.Viewpoints;
            }
        }
        
        public Root Root
        {
            get { return mRoot; }
        }

        public bool SceneLoaded
        {
            get { return mSceneLoaded; }
            set { mSceneLoaded = value; }
        }

        public SceneInfo SceneInfo
        {
            get { return mSceneInfo; }
        }

        public IResource GetResource(string id)
        {
            return Repository.Get(id);
        }

        public void DisableAllLights()
        {
            for (int i = 0; i < mEntities.Count; i++)
                if (mEntities[i] is Lights.Light)
                    ((Lights.Light)mEntities[i]).Active = false;
        }

        public Entity[] ServerEntities
        {
            get
            {
                if (mServerEntities == null || mServerEntities.Count == 0) return null;
                return mServerEntities.ToArray();
            }
        }

        public Entity[] ActiveEntities // entities that have change flags set? or what is this again exactly?
        { 
            get 
            { 
            	if (mActiveEntities == null || mActiveEntities.Count == 0) return null;
            	// TODO: I think the following is not thread safe.  If ToArray() is called
            	// just as another entity is added to the mActiveEntities list, an Exception
            	// will occur.  I need to synchronize access to mActiveEntities.
            	return mActiveEntities.ToArray();
            }
        }
        
        public Entity[] GetEntities()
        {
            if (mEntities == null || mEntities.Count == 0) return null;
            return mEntities.ToArray();
        }

        public Entity GetEntity(string id)
        {
            IResource ent = Repository.Get(id);
            if (ent is Entity) return (Entity) ent;

            return null;
        }

        public List<Entity> FindEntities (bool recurse, Predicate<Entities.Entity> match)
        {
        	return this.Root.SceneNode.Query (recurse, match);
        }
        
        // NOTE: im not sure how useful this is since it depends on when these items were added to the Scene
        //  and not where they are encountered in the scene.
        public Entity FindFirstEntityByType(KeyCommon.Flags.EntityAttributes flags)
        {
            IResource[] resources = Repository.Items;

            for (int i = 0; i < resources.Length ; i++)
            	if (resources[i] as Entity != null && (((Entity)resources[i]).Attributes & flags) != 0 )
                    return (Entity) resources[i];

            return null;
        }
        
        private Keystone.TileMap.Structure GetRegionStructure (Region region)
        {
        	Keystone.TileMap.Structure structure = null;
				for (int i = 0; i < region.Children.Length; i++)
					if (region.Children[i] as Keystone.TileMap.Structure != null)
					{
						structure = (Keystone.TileMap.Structure)region.Children[i];
						break;
					}
				
				return structure;
        }
                
        
        
        public virtual void Update(double elapsedSeconds)
        {
            // TODO: should pager on Client only be associated with RenderingContext so that
            // it can be associated with a specific active viewpoint?
            if (mPager != null)
                mPager.Update();  // even server will need to page in scripts.   
        }



        #region INodeListener Members


        /// NOTE: You cannot add Enties directly to the SpatialGraph.  
        /// This is a fundamental concept that must be understood.  Entities represent Hierarchical 
        /// relationships and not spatial ones.  If you don't know which hierarchical relationship
        /// a child entity should have, you can't expect the program to figure it out.  
        /// Therefore, you MUST know in advance which Entity you wish
        /// to add a child entity to and then add that child directly to that parent entity. 
        /// Now, you can pick into the spatial graph to return a desired entity (e.g. a region)
        /// but then you will entity.AddChild(entity) and that parent entity will be responsible for inserting
        /// that child into the SceneManager.  When we first determined we'd be seperating our entities from our
        /// spatial graph, this was all apart of that fundamental design decision of how it _should_ work.  This is
        /// desired behavior even though after taking weeks off coding sometimes, I forget these facts.  But the fact is
        /// remembering this simplifies going foward because the restraints it imposes, points the correct path foward.
        /// Thus, when adding a child entity to a parent, we must pass it the child's relative position to the parent's origin.
        /// This too is by design and we keep forgetting.  Child entities position property is always in relative coords
        /// with respect to the parent.  However, the entity.SceneNode.Translation is always in worldcoordinates. This is why
        /// our SceneNode's position vars will need to use doubles.
        internal void EntityAttached(Entity childEntity, Scene scene)
        {
        	// TODO: what if we did the resource paging from within here?
        	//       this avoids issues of IncrementRef having anything to do with it...
        	//       - we can still if we want to implement a special code to force paging when we just
        	//       load a resource
        	
        	// TODO: can we / should we require that NodeAttached must require that "scene" be not null?
        	//       that before we even allow SceneNodes to be added, scene must not be null.  If it is null
        	//       we return and then rely on waiting for the node to actually be added to a parent that is 
			//		 attached to the scene and then we can recurse and call NodeAttached for all.
        	//       I know that for things like our AssetPlacement and HUD, we might render and short circuit
        	//       the adding to scene by using an immediate mode render directly to PVS and bypassing culler,
        	//       but is that not really a problem?
        	
        	// TODO: when are we loading in DomainObject? if DomainObject is changed, we change resourcestate
        	//       and maybe even entity.Suspended = true.  Maybe we even scene.Detach (thisEntity) this way whenever
        	//       DomainObject is loaded, we can attach it and then that will trigger cascade
        	//       otherwise thisEntity.AddChild(domainObject) occurs and Repositry.IncrementRef will page 
        	
            //if (childEntity.ID == "manipulator")
            //    Trace.WriteLine("SceneBase.NodeAttached - " + childEntity.TypeName + "' " + childEntity.ID + "' attached.");
            //else
            //    Trace.WriteLine("SceneBase.NodeAttached - " + childEntity.TypeName + "' " + childEntity.ID + "' attached.");

            childEntity.Scene = scene;
            
            mEntities.Add(childEntity);
                        
        	if (mEntityAddedHandler != null)
	            // notify calling app so it can do things like add node to treeview
        		// if parent is null, it'll be added to treeview as root node
        	    mEntityAddedHandler.Invoke(childEntity.Parent, childEntity);

            // Notify the Mission that an Entity which may be required for a Mission Objective has activated
            if (mSimulation.CurrentMission != null)
                mSimulation.CurrentMission.EntityActivated(childEntity);

            // when a root is first created when loaded from file, this event is triggered so that
            // it's scenenode can be created and if any child entities have already paged in and added
            // then it's children can have their scenenode's created as well recursively. (since we know
            // that no child entity can have a scenenode (except for root) if it's parent does not
            // also have a scenenode
            //if (childEntity is Root || childEntity is ZoneRoot && childEntity.SceneNode == null)
            if (childEntity as Root != null) // TODO: temp: i commented out above line and replaced with this and added assert for childEntity.SceneNode == null to see if it ever fails.  I dont know what scenario would cause it to fail
            {
            	// TODO: above we test if childEntity.SceneNode == null but under what condition would
            	// that be != null?  do an assert and lets see if it ever happens
            	System.Diagnostics.Debug.Assert (childEntity.SceneNode == null);
                RegionNode rootRN = new RegionNode((Root)childEntity);
            }
			else 
			{
	            // NOTE: All we are doing is Adding SceneNode's here! Nothing else
	            // TODO: is there any scenario in which this .Parent == null is true?  Only root and we test for that already
	            System.Diagnostics.Debug.Assert (childEntity.Parent != null);
	            //if (childEntity.Parent == null) return;
	
            
	            SceneNode parentSN = childEntity.Parent.SceneNode;
	            if (parentSN != null)
	            // test for null because unconnected entity parents will not have a sceneNode so children will be unconnected as well.
	            {
	                SceneNode childSN;
	
	                if (childEntity.SceneNode != null)
	                    childSN = childEntity.SceneNode;
	
	                else if (childEntity is Portals.Interior)
	                    childSN = new CelledRegionNode((Portals.Interior)childEntity);
	                else if (childEntity is Region) // region but not of subclass class Interior which we'll handle a few lines down
	                    // NOTE: I don't need a special OctreeRegionNode for region nodes.
	                    // And I don't need a seperate OctreeEntityNode for entities either
	                    // because EntityNode will simply reference any Octree.Octant if applicable?
	                    //if (((Region)childEntity).IsOctreePartition == false)
	                        childSN = new RegionNode((Region)childEntity);
	                    //else
	                    //    childSN = new Keystone.Octree.OctreeRegionNode((Region)childEntity);
	                else if (childEntity is Portal)
	                    // note: the only use of special PortalNode to host portal entities is for
	                    // unique Apply() traverser method in ITraverser implementations
	                    childSN = new PortalNode((Portal)childEntity);
	                else
	                {
	                    if (parentSN is CelledRegionNode)
	                        childSN = new CellSceneNode(childEntity);
	                    else if (childEntity is Controls.Control2D)
	                        childSN = new GUINode((Controls.Control2D)childEntity);
	                    else 
	                        childSN = new EntityNode(childEntity);
	                }
	
	                if (!parentSN.Children.ArrayContains(childSN))
	                    parentSN.AddChild(childSN);
	            }
			}
            // TODO: here all we really need to do is parse the subscriber guids and register the entity with them.
            // we don't need .Activate(bool)
            // entity is activated in the Simulation so it get's .Updated() every loop. 
            // This has nothing to do with "activating" a widget by adding it to the Scene
            childEntity.Activate(true);
            // childEntity.RestoreSubscriptions();
            //                string[] keys = childEntity.Subscriptions;
            //                if (keys != null)
            //                	for (int i = 0; i < keys.Length; i++)
            //		                mEntitySystems[keys[i]].Register (childEntity);

            if (childEntity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.HasViewpoint) == true)
                ManageGeneratedViewpoints(childEntity, true, true, false);

            // TODO: problem here is, when child resource nodes get added, there is no mechanism to start
            //       queing them... that is why before with IncrementRef it would always occur but the problem was
            //       in detecting when these nodes were attached.  if we could guarantee 
            QueuePageableResource ((Node)childEntity, scene);
                        
            // April.25.2011
            // Each child generates it's own Entity Added event however
            // at the top of this method we first test if that child's parent
            // sceneNode == null and if so, we return. In other words we ignore
            // those child's EntityAdded if it's parent is not attached to the scene.
            // Thus, here we recurse to make sure all children are ultimately connected
            // with their own SceneNode's when the parent is added to the Scene.
            // Our widget control is a good example of where this is needed.
            if (childEntity.Children != null)
            {
                foreach (Node n in childEntity.Children)
                {
                    // note: testing for null SceneNode is causing us to skip child entities
                    // which have NON null SceneNodes but where those SceneNodes have no parents.
                    // Calling NodeAttached() will re-attach them.  But the question is, why are those
                    // SceneNodes not null since detaching should cascade through child nodes and detach
                    // them as well.
                    if (n is Entity) // && ((Entity)n).SceneNode == null)
                        EntityAttached((Entity)n, scene);
                }
            }

            // TODO: use IEntitySystem with a PhysicsEntitySystem with Serializable = false;
            //if (entity is IPhysicsEntity)
            //{
            if ((childEntity is Controls.Control == false) && (childEntity is Region == false) && (childEntity is Lights.Light == false))
            {
                // TODO: instead of above if &&, why not just check for PhysicsBody from start?
               // TODO: temporarily disabled after commenting out jiblibx in move to .net 4.0 which XNA 3.1 has problems with  if (childEntity.PhysicsBody != null)
               // TODO: temporarily disabled after commenting out jiblibx in move to .net 4.0 which XNA 3.1 has problems with      childEntity.PhysicsBody.EnableBody();

                //    Physics.Primitives.CollisionPrimitive primitve;
                //    //if (entity.ID == "floor")
                //        primitve = new Physics.Primitives.BoxPrimitive(entity.PhysicsBody, entity.BoundingBox);
                //    //else
                //    //{
                //    //    primitve = new Physics.Primitives.SpherePrimitive(entity.PhysicsBody, entity.BoundingSphere);
                //    //}
                //    _space.add(entity.PhysicsBody);
                //}
            }
        }

		
        /////////////////////////////////////////
        // Detached
        // NOTE: All we are doing is removing SceneNode's here! Nothing else
        internal void EntityDetached(Entity childEntity)
        {
//           Debug.WriteLine("Scene.EntityDetached() - Entity name = " + childEntity.Name);
//            if (childEntity.ID == "manipulator")
//                Trace.WriteLine("SceneBase.EntityDetached - " + childEntity.TypeName + "' " + childEntity.ID + "' detached.");
//            else
//                Trace.WriteLine("SceneBase.EntityDetached - " + childEntity.TypeName + "' " + childEntity.ID + "' detached.");

            if (mEntityRemovedHandler != null)
                mEntityRemovedHandler.Invoke(childEntity.Parent, childEntity);

            // Notify the Mission that an Entity which may be required for a Mission Objective has activated
            if (mSimulation.CurrentMission != null)
                mSimulation.CurrentMission.EntityDeActivated(childEntity);

            // obsolete - testing for now to see if this really is obsolete
            //if (childEntity is Lights.Light)
            //    _lights.Remove((Lights.Light)childEntity);

            mEntities.Remove(childEntity);

            
            // TODO: i don't really like that an Entity's EntityNode (CelledNode)is child of both
            //       a SceneNode and a ISpaitalNode
            SceneNode parentSN = childEntity.SceneNode.Parent;
 //           if (parentSN != null) // - with HUD switching sometimes the parentSN is null.  I don't know why yet.  We really should always have a 
            	// parentSN because if the child is "attached" to the Scene, only then can it be Detached here.  
	            parentSN.RemoveChild(childEntity.SceneNode);

           
            // flags get set so that entity is DE-activated in the Simulation so that
            // .Update() stops being called every loop. 
            // The .Activate(false) call has nothing to do with "de-activating" a widget by 
            // removing it to the Scene
            childEntity.Activate(false);

            if (childEntity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.HasViewpoint) == true)
                ManageGeneratedViewpoints(childEntity, true, false, true);

            // we recurse because our final best determined design is to only generate
            // the NodeAttached/NodeDetached events when the top most child entity is removed
            // and NOT for each child.  Instead we'll recurse here and directly call EntityDetached
            // for those children.
            if (childEntity.Children != null)
            {
                foreach (Node n in childEntity.Children)
                {
                    Entity ent = n as Entity;
                    if (ent != null && ent.SceneNode != null)
                        EntityDetached(ent);
                }
            }

            // System.Diagnostics.Debug.Assert (childEntity.Scene == this);
            childEntity.Scene = null;

            // disposing childEntity sceneNode will also null childEntity.SceneNode reference
            childEntity.SceneNode.Dispose();
            childEntity.SceneNode = null;
        }


        /////////////////////////////////////////
        // Activated - Occurs after EntityAttached.  
        internal void EntityActivated(Entity entity)
        {
            // todo: it seems Proxies are being Activated and thus probably "EntityAttached" as well.  Do we want this?
            //       If so, then we definetly need to Detach and DeActive them when appropriate.

            // todo: any Entity script is likely not paged in at this point so for Entities that serve as spawnpoints
            //       or Mission Objects that we only want server (eg Loopback) to update(), how do we know this Entity
            //       should be added to a seperate list of mActiveServerEntities?  Keep in mind that some Entities like
            //       ones we designate as spawn points, can be built directly into the scene and not be loaded from a Mission Object.

            if (entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.ServerObject))
            {
                System.Diagnostics.Debug.WriteLine("Scene.EntityActivated() - Server Object added.");
                mServerEntities.Add(entity);
            }
            else if (!mActiveEntities.Contains(entity))
                mActiveEntities.Add(entity);

            // TODO: find and add any physics objects (eg colliders and rigidbodies) to the Physics simulation
            //       and tag them to this entity
            // if (entity.RigidBody == null) return; // even if colliders are present and we don't need Dynamics, there needs to be a RigidBody to do collisions.
            // RigidBody body = entity.RigidBody;
            // NOTE: for Unity3d, it seems if you want compound colliders, you need to add child GameObjects off your main GameObject and then add a single
            // Collider (eg box, sphere, capsule) to each child gameobject.  So this means we should only allow one Collider per Entity instead of an array.
            // Collider[] colliders = entity.Colliders;
            // Collider collider = entity.Collider;
            //mSimulation.
            //Core._Core.Physics
        }

        internal void EntityDeactivated(Entity entity)
        {
            // This method fires when EntityDetached() calls Entity.Activate(false) which then calls Scene.EntityDeactivated(entity)

            System.Diagnostics.Debug.WriteLine("EntityDeactivated() Thread ID: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            if (entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.ServerObject))
            {
                System.Diagnostics.Debug.WriteLine("Scene.EntityDeactivated() - Server Object '" + entity.Name + "' of type '" + entity.TypeName + "' removed.");
                mServerEntities.Remove(entity);
            }
            else if (mActiveEntities.Contains(entity))
            {
                System.Diagnostics.Debug.WriteLine("Scene.EntityDeactivated() - Entity '" + entity.Name + "' of type '" + entity.TypeName + "' deactivated.");
                mActiveEntities.Remove(entity);
            }

            if (mChangedNodes != null)
                mChangedNodes.RemoveAll(e => e.ID == entity.ID);

            // TODO: remove any physics objects from the Physics simulation

        }


        /////////////////////////////////////////
        // Destroyed
        internal void EntityDestroyed(Entity entity)
        {
            // the generic SceneNode method could be called but we don't want it to.
            // what we need instead are more overloads for RegionNode, ZoneNode, 
            throw new NotImplementedException();
            EntityDeactivated(entity);
            //TODO: remove from mActiveEntities
        }

        /////////////////////////////////////////
        // Attached

        // the generic SceneNode method could be called but we don't want it to.
        // what we need instead are more overloads for RegionNode, ZoneNode, 

        // TODO: some types like Players need to be in a seperate _players array?  not sure... 
        // e.g.
        //if (entity is Player)
        //    _players.Add((Player)entity);

        // not sure what to do about "position" because Directional doesnt have one, but to simulate sunlight
        // i think maybe we should use a direction and even an artificial range to allow us to define the area
        // and create a "box" and to maybe help us determine if when indoors when we are able to see light.
        // And recall, even that is maybe not 100% enough because we dont want indoor meshes lit by sunlight
        // it would be light coming through the walls.  So i think we need to turn lights on/off during the render
        // So perhaps categorically, when rendering indoors, turn off the outdoor lights.  Maybe that's the deal.
        //
        // _root.Insert(l);  //_injector.Inject(l); // TODO: adds the light to the octree.  At runtime, the Renderer traversal
        // should manage the indoor / outdoor light stack
        //  _lights.Add(l);
        //l.Enable = true;
        //   _sceneManager._entitySpawned (this, child); // only purpose of this seems for notifications
        // insert is called internally by container entities like Regions when they've added a child entity to them
        // and now need for this child to be represented in the scene.  NOTE: we have a 1:1 relationship with a parent region and it's spatial
        // regionNode representation.  That is why we can recurse backward through the parent Entities 
        // ack - but consider a moon orbiting a planet?  
        // ack2 - what about things being carried by a player that end up straddling a portal?
        // ack3 - what about a tank barrel that is part of an overall tank, and so doesnt have a fixed bound "region" because when the turret 
        //       swivels that bounds changes.  This is part of why the spatial graph is / should be independant of the parentEntity childEntity relationship.
        //       right?  But the tank turret and tracks and body are 3 sub-entities of a "tank" entity that itself has no bounds.  And those three parts
        //       are siblings and child of that entity container object.  Spatially an overall regionNode could be created to represent the overall
        //       entity and it will have 3 entitynode's underneath it.
        // answer?  I think maybe the answer is based on which "region" something is in.  An entity attached as a child (e.g. sword in player's hand)
        //          is ultimately recursed upward til we find the region it's in.  Then in the spatailgraph it's added as a child to the RegionNode
        //          that hosts that Region.  However while recursing backwards, we accumulate the matrices of every parent along the way.

        // TODO: for the very short term, we wont worry about the above until we get multiple zone's loaded, a planet or so in and an empty ship.
        //       with all bounding boxes, lazy dirty updates, and movement across zones and such working.
        //

        // This is only called if an Entity with hasViewpointFlagChanged == true is 1) added or 2)removed or 3)if the hasViewpointFlagChanged has changed
        public void ManageGeneratedViewpoints(Entity hasViewpointEntity, bool hasViewpointFlagChanged, bool entityAdded, bool entityRemoved)
        {

            if (CoreClient._Core.Scene_Mode != Core.SceneMode.EditScene) return;

            // NOTE: Since this method only occurs during EDIT, I don't think we have a case where the Entity changes Zones such
            // that it's associated autogenerated viewport.StartingRegionID would need to be changed

            // HOWEVER - perhaps when a Node crosses Zone's it initiates a Detached and Attached event which then calls this method.
            // I think if we manually re-parent an Entity in the treeview, then yes, this method needs to be called
            //    - TODO: I CAN TEST THIS EASILY

            // 1 - why are there two sceneInfo's in the SceneInfo.xml?
            // 2- why are viewpoints being stored to both?
            // 3 - the SceneInfo.ID points to the full Scene folder path and it should just be relative path

            Viewpoint[] vp = mSceneInfo.Viewpoints;
            bool flagSet = hasViewpointEntity.GetEntityFlagValue("hasviewpoint");

            if (entityRemoved)
            {
                // remove the Viewpoint representing this entity
                if (vp != null && vp.Length > 0)
                {
                    for (int i = 0; i < vp.Length; i++)
                    {
                        // todo: add a bool to Viewpoint indicating if it is AutoGenerated
                        if (vp[i].FocusEntityID == hasViewpointEntity.ID)
                        {
                            System.Diagnostics.Debug.Assert(vp[i].AutoGenerated == true);
                            mSceneInfo.RemoveChild(vp[i]);
                            break;
                        }
                    }
                }
            }
            else if (entityAdded)// if entityAdded
            {
                if (flagSet)
                {
                    bool exists = false;
                    for (int i = 0; i < vp.Length; i++)
                    {
                        if (vp[i].StartingRegionID == hasViewpointEntity.Region.ID && vp[i].FocusEntityID == hasViewpointEntity.ID)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        // add a new autogenerated viewpoint if one for it doesn't already exist
                        string id = Repository.GetNewName(typeof(Viewpoint));
                        Viewpoint v = Viewpoint.Create(id, hasViewpointEntity.Region.ID);
                        v.FocusEntityID = hasViewpointEntity.ID;
                        v.Serializable = true; // MUST BE SERIALIABLE IF ADDING TO SCENEINFO

                        mSceneInfo.AddChild(v);
                    }
                }
                else
                {
                    for (int i = 0; i < vp.Length; i++)
                    {
                        // todo: add a bool to Viewpoint indicating if it is AutoGenerated
                        if (vp[i].StartingRegionID == hasViewpointEntity.Region.ID && vp[i].FocusEntityID == hasViewpointEntity.ID)
                        {
                            System.Diagnostics.Debug.Assert(vp[i].AutoGenerated == true);
                            mSceneInfo.RemoveChild(vp[i]);
                            break;
                        }
                    }
                }
            }
            else if (hasViewpointFlagChanged)
            {
                if (flagSet)
                {
                    bool exists = false;
                    for (int i = 0; i < vp.Length; i++)
                    {
                        if (vp[i].StartingRegionID == hasViewpointEntity.Region.ID && vp[i].FocusEntityID == hasViewpointEntity.ID)
                        {
                            exists = true;
                            break;
                        }
                    }
                    System.Diagnostics.Debug.Assert(!exists);
                    // add the viewpoint which should not already exist
                    string id = Repository.GetNewName(typeof(Viewpoint));
                    Viewpoint v = Viewpoint.Create(id, hasViewpointEntity.Region.ID);
                    v.FocusEntityID = hasViewpointEntity.ID;
                    v.Serializable = true; // MUST BE SERIALIABLE IF ADDING TO SCENEINFO
                    mSceneInfo.AddChild(v);
                }
                else
                {
                    // remove the viewpoint
                    if (mSceneInfo.ChildCount > 0)
                    {
                        if (vp != null && vp.Length > 0)
                        {
                            for (int i = 0; i < vp.Length; i++)
                            {
                                if (vp[i].StartingRegionID == hasViewpointEntity.Region.ID && vp[i].FocusEntityID == hasViewpointEntity.ID)
                                {
                                    mSceneInfo.RemoveChild(vp[i]);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // NOTE: the changes wont commit to disk until Save() is called.
            mXMLDB.Write(mSceneInfo, true, false, null);
        }

        #region Entity Spatial Management
        // Updated
        internal void EntityUpdated(Entity entity)
        {
            if (entity.SceneNode == null)
            {
                // TODO: April.26.2017 - our vehicle is throwing this exception twice initially but then goes away
                //                       looks like a race condition.
                Debug.WriteLine("Scene.EntityUpdate() - entity '" + entity.ID + "' not attached to Scene.");
                //throw new Exception();
            }
            // TODO: should performance test this .Contains()  and maybe see if we can
            //       speed it up by making mChangedNodes a linked list so that insertions are fast
            //       and looking for duplicates is N(1) operation?
            //using (CoreClient._CoreClient.Profiler.HookUp ("EntityChangedFlag"))
            //{
	            if (mChangedNodes.Contains (entity)) return;
	            // for updates, we only want to use the generic SceneNode case
	            mChangedNodes.Add(entity);
            //}
        }
        
        
        // TODO: one important thing to remember is, we want a chance for the Simulation physics
        // collision response to engage before we attempt to reposition EntitySceneNodes
        // TODO: is our physics updates causing this event to occur when it should not?
        public void FinalizeEntityMovement(long ticks)
        {
            // apply queued changes to the entities
            for (int i = 0; i < mChangedNodes.Count; i++)
            {
            	Entity entity = mChangedNodes[i];
                
                if (entity != null) // NOTE: Do not test && entity.Enabled here because that will prevent us from responding to the last detach notice it sends!
                {
                	// if this entity was added twice or more to mChangedNodes then
                	// .LastTick will equal this tick for subsequent attempts to udpate it,
					// so we skip those instances  since we never update more than once
                	if (entity.LastTick == ticks) 
                	{
                		entity.LastTick = ticks;
                		continue;
                	}
                	entity.LastTick = ticks;
                	
                	// no flags set
                    if ((entity.ChangeFlags & Enums.ChangeStates.None) > 0) 
                    	continue;
                	                    
                    // TODO: we can still end up with scenarios where user deletes an item and it still has messages
                    // here that attempt to get processed after it's already detached from scene.
                    // This will cause errors when the node's mSceneNode variable comes up null.
                    // TODO: is this really the oldpos prior to movement? doesnt seem like it.
                    Vector3d oldpos = entity.Translation; // cache position prior to update in case of ....?
                                      
		            // When an entity moves, it will trigger the Listener.NodeMoved(this)
		            // which can handle the logic for moving an entity.
		            //
		            // It seems this is proper and that boundary enforcement should be done by the 
		            // Simulation for Cameras or anything else based on the type of entity/camera
		            // and the current zone/region and the entity's path.
		            // eg. If it's determined the entity has traversed through a portal, then
		            // the Simulation makes that determination and finds the new location.
		            //
		            // So maybe all this "EnforceBoundary" bullshit doesnt belong in any SceneNode/RegionNode/ZoneNode
		            // and is instead logic of the Simulation.
		            // This is probably also best for integrating physics too.
		
		            // THE ORDER HERE IS IMPORTANT!!!!!!!!!!!!
		            if ((entity.ChangeFlags & Enums.ChangeStates.EntityResized) > 0)
		            {
		            	// TODO: Zone and Zone root are getting this message when their child entities move
		                // vital for triggering ISpatialNode rebalancing
		                // TODO: i need to test scenarios where if a child is InheritScaling
		                // and I scale a parent entity that results in child entity, that both entities
		                // get NodeResized() and have a chance to be repositioned within the SpatialTree (octree or quadtree)
		                this.EntityResized(entity); 
		            }
		            if ((entity.ChangeFlags & Enums.ChangeStates.EntityMoved) > 0)
		            {
		                // spatial node moving does not occur during the culling/rendering stages which is ideal.
		                //if (this is Keystone.Lights.PointLight)
		                //    CoreClient._CoreClient.Engine.AddToLog("Entity.NotifyListener.Set() - light notifying Scene.NodeMoved = " + ((IPageableTVNode)this).TVIndex.ToString());
		
                        // TODO: shouldn't we update based on any changes in Entity.Velocity? i.e don't allow scripts to move entities.Translation.  Only allow them to modify the velocity?
		                this.EntityMoved(entity);
		            }                    
                    
                    entity.DisableChangeFlags (Enums.ChangeStates.EntityMoved | Enums.ChangeStates.EntityResized);
                    Vector3d newPos = entity.Translation; // position after update where bounds checking may be required

                    // determine if we've left a trigger area and raise appropriate events in FinalizeMovement()
                    entity.FinalizeMovement();
                }
            }

            mChangedNodes.Clear();
        }


        // TODO: does NodeMoved impact all children of a moved node?  Or does
        // moving of the parent Entity result in the child Entity's "move" which results
        // in that child's EntityNode receiving .OnEntityMoved right here?
        // I should be able to test this with a star and an attached pointlight
        // then move the star and see which EntityNode's get .OnEntityMoved.
        // SEE NOTES IN EntityNode.cs.UpdateBoundVolume() dated Dec.4.2012
        //and SEE NOTES In SceneNode.cs.SetChangeFlags()
        // I think SceneNode only needs BoundingVolume dirty flag, no others.
        // remember that we have listeners trying to track position of receptors and transmitters for instance
        /// <summary>
        /// Called from Entity.NotifyListeners when an Entity has been translated.
        /// </summary>
        /// <param name="entity"></param>
        internal void EntityMoved(Entity entity)
        {
            if (entity is Region) return; // region's don't change their RegionNodes and don't Move().  This NodeMoved() is only called Regions when they are first loaded into the scene
                                        // Debug.WriteLine(node.ID + " " + node.TypeName + " _moved");

            EntityNode entityNode = (EntityNode)entity.SceneNode;

       //     entityNode.SpatialNode.Quadtree.Bounds
            // NOTE: Here is where the spatial node gets a chance at updating it's position in the scene.
            //       This way this SpatialNode update occurs here during Update() and not during Cull()
            entityNode.OnEntityMoved();

#if TVPhysics
            if (CoreClient._CoreClient.Physics != null && node.RigidBody != null)
                CoreClient._CoreClient.Physics.SetBodyPosition(node.RigidBody.TVIndex, (float)node.Translation.x, (float)node.Translation.y, (float)node.Translation.z);
#else
           #region Jitter 
            if (entity.RigidBody != null && entity.RigidBody.Body != null)
            { 
                Jitter.LinearMath.JVector position;
                position.X = (float)entity.Translation.x;
                position.Y = (float)entity.Translation.y;
                position.Z = (float)entity.Translation.z;
                // TODO: should i have node.RigidBody.Position and then allow the RigidBody to update the Jitter Body
                // TODO: do i need to update the Shape position too? Maybe not, the Jitter Body has a reference to the Shape being used.
                entity.RigidBody.Body.Position = position;

                // TODO: we need to finalize the movement within the Entity so as to discover TriggerExit events.

            }
            #endregion
#endif
        }


        internal void EntityResized(Entity entity)
        {
            if (entity is Region) return;

            EntityNode entityNode = (EntityNode)entity.SceneNode ;
            // NOTE: Here is where the spatial node gets a chance at updating it's position in the scene.
            //       This way this SpatialNode update occurs here during Update() and not during Cull().
 
            entityNode.OnEntityResized();
        }
#endregion
#endregion
        
		internal void QueuePageableResource (Node node, Scene scene)
        {
			// - NOTE: by definition, all children off of the AttachedEntity node
			//	 are connected to scene, even if that mScene has not been assigned yet
        	if (node is IPageableTVNode)
        		QueuePageableResource ((IPageableTVNode)node);
        	
        	
        	// TODO: seems to fail to queue models added from HUD
        	// recurse children if applicable. note: this can fail to work properly if we 
        	// are recurisvely loading children in SceneReader (eg we forgot to set context.RecursivelyLoadChildren == false)
        	// when we do not intend to. 
        	if (node is IGroup)
        	{
        		IGroup g = (IGroup)node;
        		if (g.Children !=null)
        		{
        			for (int i = 0; i < g.Children.Length; i++)
        			{
        				// note: we have to recurse child entities because if they were previously unconnected to scene
        				// by virtue of being attached to a unconnected Root node, then every single hierarchy of entity 
        				// under that root will be disconnected and their resources and child resources will never get
        				// paged in.
        				if (g.Children[i] is Entities.Entity) continue; // do not recurse child entities because those will get done on recursive calls to EntityAttached()
        				QueuePageableResource (g.Children[i], scene);
        			}
        		}
        	}	
        }
        
        internal  void QueuePageableResource (IPageableTVNode node)
        {

        	// if this node has just connected, which it may have since we know its refcount was just increased
        	// then we need to recurse this node down to all non child Entities
        	
        	if (!(node.TVResourceIsLoaded) && node.PageStatus != PageableNodeStatus.Loaded && node.PageStatus != PageableNodeStatus.Loading)
            {
                // note: check for scene.PagerEnabled since if we're generating a temporary node such as Universe nodes, we dont want
                // to start paging those in so we can set scene.PagerEnabled = false;
                // TODO: shouldn't the pager be shared by all scenes?  all we'd have to do
                // is add ability to prioritize items added to the pager.  We dont need
                // a seperate pager per scene.  The only thing
                // is during our pager update, it should be done per scene so that
                // the current scene and whether it's a multi-zone or single zone can be
                // taken into account

                // June.17.2013 - Hypno - NOTE: I commented out in SceneReader.cs.ReadEntity()
                // a call to QueuePageableResource() that was redundant, so far seems confirmed to
            	// not be needed.
            	// TODO: would be nice if we could disable paging by Scene.  But what if the node
            	// exists under different parents from different scenes? it should skip paging for the one
            	// but not the other.
            	// what if i dont do any paging when the node is disconnected from an actual Scene?
            	// then when attached to scene finally, we can queue paging for all pageable nodes and
            	// perhaps even prioritize them?
            	// so if that is the case, we only page branches where Entity has been attached
            	// so we'd call from within Scene. 
            	// we can still force paging by explicitly calling .LoadTVResource() such as with
            	// assetplacement tool
            	// if this is an entity, we never do any paging.  That occurs only when the Entity is attached.
            	// if this is not an entity, we only page a) if immediate ancestoral Entity.Scene != null and b) Entity.Scene.PagerEnabled = true
            	// is this ok and not too hackish? will it cover all use cases?
            	//    	            if (scene.PagerEnabled)
            	if (node.PageStatus != PageableNodeStatus.Loading )
            		// TODO: here having a handler would be nice to initialize domain object for instance when it's loaded
            		// because here what we have is entity is loaded but it's script is not prior to it being Attached to scene.
            		// I think before when i had DomainObject do such things on AddChild() it worked better however
            		// it made it slightly more difficult to unregister scripts
            		// TODO: further, node shouldnt be flagged as Active until any script it has is loaded... geometry and appearance we dont care about tho.
            		//       animations neither so long as we can always have current tickcount in animation controller we dont care if the animation itself cannot be seen
                    IO.PagerBase.QueuePageableResourceLoad(node, null);
            	

//		            	// page Entity since all Entity are IPageableTVNode and recurse all child nodes except
//		            	// child Entities... but how do we know if the child Entity has all of it's own children added?
//		            	// because when deserializing we instance node and assign properties and then set to parent
//		            	// and then we go on to children...
//		            	// perhaps when an IPageableTVNode is added anywhere beneath Entity, and resourcestatus = NotLoaded 
//		            	// we respond to propogated change flag by finding the resource and Queueing it.
//		            	// and even if we were to not attach root to scene until all scene IO was done, we'd still have to 
//		            	// deal wth cases where we change a Script, Texture or Mesh at runtime in editor.
//		            	// If all nodes that are .AddChild() and so Repository.IncrementRef, can we test 
//		            	// scene.PagingEnabled there as well as the node's connectivity? non entity nodes we must recurse up to first entity
//		            	// and see if it is connected

            }
            
        }
        	
        // if the zone's have internal "structure" then we will initialize MapGrid
    	internal TileMap.MapLayerGrid mMapGrid;

#region Pathing Helpers
        public Vector3i Tile_FindAdjacent (string regionID, Vector3d start, Vector3d target, out Vector3d destination)
        {
        	destination = Vector3d.Zero();
        	
        	Zone zone = (Zone)Repository.Get (regionID);
    
        	TileMap.Structure sourceStructure = (TileMap.Structure)Group.GetChildofType (zone.Children, "Structure");
        	Vector3i targetTile = sourceStructure.TileLocationFromPoint (target);
        	
        	// breadth search for nearest walkable tile for melee units for now
       // 	sourceStructure.GetAdjacentTileList (targetTile);
        	
        	
        	return targetTile;
        	
        }
        
        public BoundingBox[] PathGetAreaPortals (string regionID)
        {
        	Region r = (Region)Repository.Get (regionID);
        	if (r as Zone != null)
        	{
        		Zone z = (Zone)r;

   				int subscriptX = z.ArraySubscript[0];
				int	subscriptZ = z.ArraySubscript[2];
				
				return PathGetAreaPortals (subscriptX, 0, subscriptZ);
        	}
        	
        	return null;
        }
                
        public BoundingBox[] PathGetAreaPortals (int x, int y, int z)
        {
        	
        	if (mConnectivityGraph == null) return null;
        	
        	Keystone.AI.Connectivity.Graph? result = mConnectivityGraph[x, z];
        	if (result == null) return null;
        	
        	Keystone.AI.Connectivity.Graph graph = result.Value;
        	if (graph.Areas == null || graph.Areas.Length == 0) return null;
        	
        	List<BoundingBox> results = new List<BoundingBox>();
        	
        	if (graph.Portals == null || graph.Portals.Length == 0) return null;
        	
        	// for all areas in this region 
        	for (int i = 0; i < graph.Areas.Length; i++)
        	{
        		Keystone.AI.Connectivity.Area area = graph.Areas[i];
        		
        		if (area.Portals == null || area.Portals.Length == 0) continue;
        		
        		BoundingBox[] portalBoxes = new BoundingBox[area.Portals.Length];
        		
        		// get the portals and append them to overall list of portalBoxes
        		for (int j = 0; j < area.Portals.Length; j++)
        		{
        			// TODO: here if instead of portal = graph.Portals[portalIndex]
        			//       what if it was portal = connectivity.Portals[portalIndex] such that
        			//       we tracked indices for portals over the entire game world globally
        			//       But why?  Well, for zone border portals, the portal can't be owned by just
        			//       the graph of one zone.  So how do we do that?  How do we create zone portals
        			int portalIndex = area.Portals[j];
        			Keystone.AI.Connectivity.AreaPortal portal = graph.Portals[portalIndex];
        			
        			double multiplier = 2.5;
        			int[] modifier = new int[2];
        			modifier[0] = 1;
        			modifier[1] = 1;
        			
        			if (portal.RegionID[0] != portal.RegionID[1])
        			{
        				// TODO: if the Start/End tiles are in different Zones, we are NOT calculating the end point locations correctly!
	        			//       They are currently connecting across the entire zone back to it's beginning instead of the beginning of
	        			//       adjacent zone

	        			const int tileCountX = 32;
	        			// TODO: x and z modifiers need to be different depending on which axis the adjacent extends
	        			//       this can be done by subtracting the zone subscripts from start to end zone
	        			int startX, startZ;
	        			Utilities.MathHelper.UnflattenIndex ((uint)portal.RegionID[0], 3, out startX, out startZ);
	        			int destX, destZ;
	        			Utilities.MathHelper.UnflattenIndex ((uint)portal.RegionID[1], 3, out destX, out destZ);
	        			
	        			modifier[0] += tileCountX * (destX - startX);
	        			modifier[1] += tileCountX * (destZ - startZ);
        		    	
        		    }
        			
        			// TODO: since we only generate portals for source and dest zones, other adjacent zones to the start zone will not
        			//       have portals created.  We need a way to dynamically update them, but for now, lets focus on pathing from
        			//       start to destinatin using an A* that uses areas and portals.
        			
        			// each portal gives us the tile x,y,z of the start and end tiles across both adjoining areas
        			portalBoxes[j] = new BoundingBox (portal.Location[0].X * multiplier, (portal.Location[0].Y - 1) * multiplier, portal.Location[0].Z * multiplier, 
        			                                  (portal.Location[1].X + modifier[0]) * multiplier, portal.Location[1].Y * multiplier, (portal.Location[1].Z + modifier[1]) * multiplier);
        				        			
        			// scale the box so it's half the size of a tile so they resemble doors 
        			//portalBoxes[j].Scale (new Vector3d(0.5, 0.5, 0.5));
        		}

        		results.AddRange (portalBoxes);
        	}
        	
        	return results.ToArray();     	
        }
        
        // HUDs can retreive the bounds and use them to draw Immediate Debugging Boxes to the viewport
        public BoundingBox[] PathGetConnectivityAreaBounds (string regionID)
        {
        	Keystone.Portals.Region r = (Keystone.Portals.Region)Repository.Get (regionID);
        	if (r is Keystone.Portals.Zone)
        	{
        		Keystone.Portals.Zone z = (Keystone.Portals.Zone)r;

   				int subscriptX = z.ArraySubscript[0];
				int	subscriptZ = z.ArraySubscript[2];
				
				return PathGetConnectivityAreaBounds (subscriptX, 0, subscriptZ);
        	}
        	
        	return null;
        }
        
        // HUDs can retreive the bounds and use them to draw Immediate Debugging Boxes to the viewport
        public BoundingBox[] PathGetConnectivityAreaBounds (int x, int y, int z)
        {
        	if (mConnectivityGraph == null) return null;
        	
        	Keystone.AI.Connectivity.Graph? result = mConnectivityGraph[x, z];
        	if (result == null) return null;
        	
        	Keystone.AI.Connectivity.Graph graph = result.Value;
        	if (graph.Areas == null || graph.Areas.Length == 0) return null;
        	
        	BoundingBox[] boxes = new BoundingBox[graph.Areas.Length];
        	
        	for (int i = 0; i < boxes.Length; i++)
        	{
        		
        		// these box min/max values need to be converted from coordinate indices to region space values
        		// and then camera space values
        		// multiplying by 2.5 isn't enough because... our tile maps are 0 - N and i think what we need is to find the StartX, StartY, StartZ 
        		// for this region and add that to the boxes
        		boxes[i] = new BoundingBox(graph.Areas[i].MinX * 2.5, (graph.Areas[i].MinY - 1) * 2.5, graph.Areas[i].MinZ * 2.5,
        		                           (graph.Areas[i].MaxX + 1 ) * 2.5, graph.Areas[i].MaxY * 2.5, (graph.Areas[i].MaxZ + 1 ) * 2.5);
        	}
        	
        	return boxes;
        }

        public Vector3d[] PathFind(Interior interior, Entity npc, Vector3d start, Vector3d end, out string[] components)
        {
            Vector3d[] points = interior.PathFind(npc, start, end, out components);
            return points;
        }

        // TODO: I think this is not the most up to date pathing for Interior. For that we need InteriorPathing.PathFind()
        public Vector3d[] PathFind (Entity entity, string destinationRegionID, Vector3d destination)
        {
        	Vector3d[] results = null;
        	Zone destinationZone = null;
        	if (entity.Region as Zone == null) return null;
        	
        	Zone startZone = destinationZone = (Zone)entity.Region;
		
			// TODO: we should be enforcing that entities translation during movement
			//       cannot tunnel through the floor in addition to world bounds checking?
			//       - when placing entities via assetplacementtool, we should be enforcing
			//       floor values as well.  currently, and not sure why, floating point imprecision perhaps
			//       but we may need an epsilon to ensure that we're not getting these negative .Translation.Y values
			Vector3d start = entity.Translation;

            
			// find destination region if it is different from startRegion
			if (destinationRegionID != startZone.ID)
			{
				destinationZone = (Zone)Repository.Get (destinationRegionID);
				Keystone.TileMap.Structure destStructure = GetRegionStructure (destinationZone);
				Keystone.TileMap.Structure sourceStructure = GetRegionStructure (startZone);
				
				int subscriptX, subscriptZ, destinationSubscriptX, destinationSubscriptZ;
				
				subscriptX = startZone.ArraySubscript[0];
				subscriptZ = startZone.ArraySubscript[2];
				
				destinationSubscriptX = destinationZone.ArraySubscript[0];
				destinationSubscriptZ = destinationZone.ArraySubscript[2];

			
				// find the start and end tiles.  Remember that these tile locations are dependant on 
				// the specific map layer being tested since map layers can have different resolutions.			
				Vector3i startTile = sourceStructure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].TileLocationFromPoint (start);
				Vector3d test = sourceStructure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].PointFromTileLocation (startTile.X, startTile.Y, startTile.Z);
	            // recall that floor 0 by default is underground who's ceiling becomes the "ground" of floor 1 which is first ground level floor.
	            // TODO: if start.y is some miniscule negative value of any kind, rather than returning 0, it will return -1 and then +1 gives startTile.Y = 0 which will be WRONG
	            //       and cause pathing to find since startTile will be underground where there is no Area generated that includes it.
	            double heightValue = start.y;
	            if (Keystone.Utilities.MathHelper.AboutEqual (heightValue, 0))
	            	heightValue = 0;
	            
	            int floor = (int)Math.Floor (heightValue / sourceStructure.FloorHeight) + 1;
	            startTile.Y = sourceStructure.FindLevelIndex (startTile.Y); // floor;
	            
	            Vector3i endTile = sourceStructure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].TileLocationFromPoint (destination);
	            test = sourceStructure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].PointFromTileLocation (endTile.X, endTile.Y, endTile.Z);
	            
	            heightValue = destination.y;
	            if (Keystone.Utilities.MathHelper.AboutEqual (heightValue, 0))
	            	heightValue = 0;
	            // recall that floor 0 by default is underground who's ceiling becomes the "ground" of floor 1 which is first ground level floor.
	            floor =(int) Math.Floor (heightValue / sourceStructure.FloorHeight) + 1; 
	            endTile.Y = sourceStructure.FindLevelIndex (endTile.Y); // floor;
            
            
				// TODO: can the below return a Tuple array of regionID's and start/end vectors which we can use to
				//       build our array of NavPoints?
				AI.BroadPhasePathFindResults pathFindResults = mConnectivityGraph.GraphFind (subscriptX, subscriptZ, startTile,
				                             destinationSubscriptX, destinationSubscriptZ, endTile);
					
				// TODO: with above result, caller is expecting an array of Vector3d but how do we get it to
				//       treat those Vector3d within a specific Zone's coordinate system?  
				//       TODO: we first need to treat entity movement across zones similar to Camera with our Zone RegionNode's bounds checking
				//       so that the entity's zone is changed.
				//			OPTIONS 
				//				1- returned GraphFind can contain only coordinates that are in the Start region's coordinate system
				//				   and so if we cross a zone, we modify the coordinates to now be within the current region's coordinate system
				//				   a - this seems most friendly since when we cross a zone we can easily convert all path values and it makes sending
				//                     a unit over a zone very easy.  We will go with this route first
				
    			// all points will be converted to coordinates relative to start graph's coordinate system
    			if (pathFindResults.FoundNodes != null)
    			{
    				// get the start Zone's offsets 
    				int startOffsetX = subscriptX;
    				int startOffsetY = startZone.ArraySubscript[1];
    				int startOffsetZ = subscriptZ;
    				
    				// during iteration, all coords where .GraphID != startZone.ID we will find relativeOffset different
    				// and multiply by Zone dimensions to find relative coordinates
    				
					System.Collections.Generic.List <Vector3d> points = new System.Collections.Generic.List<Vector3d>();
					for (int i = 0; i < pathFindResults.FoundNodes.Count; i++)
					{
						// Tile Indices to Position Coordinates 
						// TODO: does it matter here that this structure is not the same?  It shouldn't matter so long as the
						//       tileLocation passed in is correct.
						Vector3d coord = sourceStructure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].PointFromTileLocation (pathFindResults.FoundNodes[i].Location.X,
						                                                                                                                pathFindResults.FoundNodes[i].Location.Y,
						                                                                                                                pathFindResults.FoundNodes[i].Location.Z);
						// convert all coords to the coordinate space of the start region
						int graphID = pathFindResults.FoundNodes[i].GraphID;
						int currentOffsetX;
	    				int currentOffsetZ;
	    				Utilities.MathHelper.UnflattenIndex ((uint)graphID, 3, out currentOffsetX, out currentOffsetZ);
						
	    				int diffX = startOffsetX - currentOffsetX;
	    				int diffZ = startOffsetZ - currentOffsetZ;

	    				double zoneWidth = startZone.BoundingBox.Width;
	    				double zoneDepth = startZone.BoundingBox.Depth;
	    				
	    				if (diffX != 0 || diffZ != 0)
	    				{
	    					// compute relative coord from startZone
	    					coord -=
		    					new Vector3d(diffX * zoneWidth,
	    				                     0,
	    				                     diffZ * zoneDepth);
	    				}
	    				
						// add the points
						points.Add (coord);
					}
					
					// append the final destination
					Vector3d offset = startZone.GlobalTranslation - destinationZone.GlobalTranslation;
					destination = destination - offset;
					
					
					points.Add (destination);
					
					results = points.ToArray();
    			}
			}
			else 
			{
			
				Keystone.TileMap.Structure structure = GetRegionStructure (destinationZone);
						
				if (structure == null) return null;
				
				// finds path points only within a single region.  To path across multiple zones we must first
				// find the list of zones we must traverse through and the start/end locations we'll be pathing from within each.
				results = this.PathFind (structure, start, destination);
			}
			
			return results;
        }
        
        
        private Vector3d[] PathFind(Keystone.TileMap.Structure structure, Vector3d start, Vector3d end)
        {
        		
			Zone zone = (Zone)structure.Region;
			
			// NOTE: 1 level with 1 obstacle layer must exist at minimum to perform path find
			uint tileCountX = structure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].TileCountX; 
			uint tileCountZ = structure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].TileCountZ; 
			//Layer_GetDimensions (0, "obstacles", out tileCountX, out tileCountZ);
			
			float tileStartX;
			float tileStopX;
			Portals.ZoneRoot.GetStartStop(out tileStartX, out tileStopX, tileCountX);
			
			float tileStartZ;
			float tileStopZ; 
			Portals.ZoneRoot.GetStartStop(out tileStartZ, out tileStopZ, tileCountZ);
			
			float tileSizeX = structure.mWidth  / tileCountX; 
			float tileSizeZ = structure.mDepth / tileCountZ; 

			// find the start and end tiles.  Remember that these tile locations are dependant on 
			// the specific map layer being tested since map layers can have different resolutions.			
			Vector3i startTile = structure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].TileLocationFromPoint (start);
			// TODO: find the index of the level who's floorLevel hosts start.y
			
            // recall that floor 0 by default is underground who's ceiling becomes the "ground" of floor 1 which is first ground level floor.
            int floor = (int)Math.Floor (start.y / structure.FloorHeight) + 1;
            startTile.Y = floor;
            startTile.Y = structure.FindLevelIndex (start.y);
            
            Vector3i endTile = structure.mLevels[0].mMapLayers[(int)TileMap.LayerType.obstacles].TileLocationFromPoint (end);
            // recall that floor 0 by default is underground who's ceiling becomes the "ground" of floor 1 which is first ground level floor.
            floor =(int) Math.Floor (end.y / structure.FloorHeight) + 1; 
            endTile.Y = floor;
			endTile.Y = structure.FindLevelIndex (end.y);

            // TODO: RESEARCH THE INDEXING OF MAPLAYERS
            //       THERE IS DEFINETLY A CONVERSION ERROR WITH OUR START/ENDTILE.Y components.  
            //       Initially they need to be 0 based but then the resulting found path coords need to be
            //       converted back to non-0 based coordinates.  And currently they are not.
            //       
            
			// TODO: we want for region.PathFind() to be thread safe.  We allow that call to create
			//       the internal grids if we need them.
			// TODO: until we can just directly pass in the mMapLayers[].TileData.Data, we will iterate and copy to 1-D grid[] (1-D grid is faster for pathing)
			int[] grid = Core._Core.MapGrid.CopyData (TileMap.LayerType.obstacles, zone.ArraySubscript[0], zone.ArraySubscript[2]);
			
			int levelCount = structure.mLevels.Length;						
			Keystone.AI.Pathing pathFinder = new Keystone.AI.Pathing(grid, (int)tileCountX, levelCount, (int)tileCountZ);
			
			bool allowDiagonal = true;
			bool allowVertical = true; // TODO: currently there is no way to allow vertical (ascend/descend levels) without also allowing diagonals! must fix that because they are very different concepts
			bool penalizeDiagonalMovement = false;
			bool punishHorizontalChangeDirection = false;
			bool punishVerticalChangeDirection = true;
			bool useTieBreaker = false;
			int heuristicEstimate = 2;
			int searchLimit = 2000;
			Keystone.AI.PathFindParameters parameters = 
				new Keystone.AI.PathFindParameters (Keystone.AI.HeuristicFormula.Manhattan,
                                                   searchLimit,
                                                   heuristicEstimate,
                	                               allowDiagonal,
                                                   allowVertical,
                                                   punishHorizontalChangeDirection,
                                                   punishVerticalChangeDirection,
	                                               useTieBreaker,
	                                               penalizeDiagonalMovement);
			
			
			Keystone.AI.PathFindResults results = pathFinder.Find (startTile, endTile, parameters);
			
			
			if (results.Found == false || results.FoundNodes == null) return null;
			
			Vector3d[] path = new Vector3d[results.FoundNodes.Count];
			
			// the path is returned in reverse order so when we compute coords from locations
			// do it in reverse
			int foundNodeCount = results.FoundNodes.Count;
			for (int i = 0; i < foundNodeCount; i++)
			{
				Keystone.AI.PathFinderNodeFast node = results.FoundNodes[i];
				// the found node coordinates are 0 based array indices.  convert them to region space coordinates	
				// using the specified mLevels[node.Y]				
				path[i] = structure.mLevels[node.Y].mMapLayers[(int)TileMap.LayerType.obstacles].PointFromTileLocation (node.X, node.Y, node.Z);
			}
									
			return path;
        }
        #endregion
#region IDisposable Members
        protected override void DisposeManagedResources()
        {
            if (!mSceneLoaded) return;
            //TODO: fix FileManager.DisableWrite = true;
            mSceneLoaded = false;
            //note: when a node is disposed, it's reference count should be down to 0 at this point
            // because when the ref count reaches 0, there should be no more references to the node
            // unless they are held by the original caller and they shouldnt be.  
            //
            // IMPORTANT: Thus the Repository will call .Dispose() on the object and the 
            //  overriden DisposeManaged/UnmangedResoruces should call an appropriate tv....Destroy() on any TV3D resource
            // thus if there are any lingering references, they will be invalid so it is the Creator's responsibility to manually
            // increment the reference count of this object if they dont want it to be disposed.

            //But there's no way to enforce
            // that.  In normal file save/load this can't happen, but when the user manually imports/creates from a static load procedure
            // then there is no way to guarantee that so we leave those resources loaded.  There's no harm in that when we unload a scene
            // only when we want to shut down the engine completely.

            try
            {
                mSimulation.Dispose();
                RemoveChildren();

                mRoot = null;
                mSceneInfo = null;

                
                // TODO: there maybe some objects that are instanced by the pager but never added to the scene and thus a call to
                // _root.RemoveChildren() has no impact.  So when shutting down we need to lock the pager, cancel any existing jobs,
                // and then dispose of all regions that hasnt been added and that should result in all of them being recursively removed as well.
                mPager.Dispose();
                mPager = null;
                
                if (mConnectivityGraph != null)
                {
	                mMapGrid.Detach (mConnectivityGraph);    
            	    mMapGrid.Dispose();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("SceneBase.DisposeManagedResources() - " + ex.Message);
            }
            // TODO: commenting out because class var _fxProviders ive commented out // Array.Clear(_fxProviders, 0, _fxProviders.Length);
            // TODO: Should these be apart of scene then or apart of our core engine?

        }

#endregion
    }
}