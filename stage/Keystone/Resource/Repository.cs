using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

using Keystone.Appearance;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.IO;
using Keystone.Portals;

namespace Keystone.Resource
{
    // UPDATE
    // I may be going back against what I originally intended in the "OLD" description.
    // This Repository is for all scene nodes that are being managed by KeyStone. 
    // RefCounting is done on node.AddChild() or when the user does node.IncrementRefCount() hete.
    //
    // Thus node.RemoveChild() will result in node.DecrementRefCount() here.
    // When the refcount hits 0, on that event, the node is removed from the Repository.
    // NOTE: When a node is created, its added to the repository but its ref count = 0 until its added to a node that is already connevted to the scene.   It does
    // not get deleted because deletes occur on the DecrementRefcount event and when the resulting count  = 0. 
    // This event clearly doesnt trigger on object creation.
    //
    // Users cannot manually delete objects.  They can RemoveChild() or explicilty Decrement the ref count and if that hits 0
    // will result in Remove() from the cache which will expliciltiy call Dispose() on that object and any unmanaged TV resource
    // will get destroyed there e.g. mesh.Destroy()  , particle.Destroy() , particle.DestroyEmitter(), light.Destroy(), textureFactory.DeleteTexture()... etc Note: to destroy a node thst is createf bur never connected to a scene, call IncrementRefCount to raise it's refcount to 1, then immediatly call DecrementRefcount to set it bsck to 0 which will trigger destroy code path.
    // 
    // So, Repository does NOT represent objects in the scene per se, it represents all objects being
    // managed by KeyStone.
    public class Repository
    {
    	// http://stackoverflow.com/questions/781189/how-to-lock-on-an-integer-in-c
    	private class Synchronizer <T>
    	{
    		
		    private Dictionary<T, SyncLock> mLocks;
		    private object mLock;
		
		    public Synchronizer() 
		    {
		        mLocks = new Dictionary<T, SyncLock>();
		        mLock = new object();
		    }
		

		    public SyncLock Lock (T key)
		    {

	        	// note: we lock dictionary access because .NET 3 dictionaries
	        	// are not thread safe even for reads.  
	        	// .NET 4 provides ConcurrentDictionary however.
	            lock (mLock) 
	            {
	                SyncLock result;
	                if (mLocks.TryGetValue(key, out result))
	                    return result;
	
	                result = new SyncLock(key, this);
	                mLocks.Add (key, result);
	                return result;
	            }
		        
		    }
		    
		    public void Unlock (SyncLock theLock)
		    {
		    	lock (mLock) // Feb.19.2014 - this lock seems required because removing from collection while trying to .add above in Lock() call will
		    				 // cause an IndexOutofRangeException - Index was outside the bounds of the array.
		    	{
	            	#if DEBUG
		    		if (mLocks.ContainsKey(theLock.Value) == false) 
		    			return; // let's see if ignoring helps since it appears we are disposing twice which calls unlock twice - throw new Exception("Repository.Unlock() - lock does not exist.");
	                #endif
	                
	            	mLocks.Remove (theLock.Value);
		    	}
		    }
		    
		    /// <summary>
		    /// Represents a lock for the Synchronizer class.
		    /// </summary>
		    public class SyncLock
		        : IDisposable
		    {
		
		        /// <summary>
		        /// This class should only be instantiated from the Synchronizer class.
		        /// </summary>
		        /// <param name="value"></param>
		        /// <param name="sync"></param>
		        internal SyncLock(T value, Synchronizer<T> sync)
		        {
		            Value = value;
		            Sync = sync;
		        }
		
		        /// <summary>
		        /// Makes sure the lock is removed.
		        /// </summary>
		        public void Dispose()
		        {
		            Sync.Unlock(this);
		        }
		
		        /// <summary>
		        /// Gets the value that this lock is based on.
		        /// </summary>
		        public T Value { get; private set; }
		
		        /// <summary>
		        /// Gets the synchronizer this lock was created from.
		        /// </summary>
		        private Synchronizer<T> Sync { get; set; }
		
		    }
		}
    	
    	  private static Synchronizer <string> mSychronizer = new Synchronizer<string> ();
    	
        internal static ConcurrentDictionary<string, IResource> mCache = new ConcurrentDictionary<string, IResource>();
        //		internal static Entities.Entity mRecentEntity;
		          
        private static KeyCommon.Data.UserDataStore mUserDataStore = new KeyCommon.Data.UserDataStore();
        
        // Note:Because Keystone.dll will know nothing about user types , the processor methods as well as gettors trying to casr usee types in the usetdata, should be in game01.dll
        // hosts intrinsic or user defined components stores
        private static KeyCommon.Data.ComponentStoreCollection mStoresCollection = new KeyCommon.Data.UserDataStore();
        
        private static KeyCommon.Processors.DataProcessors mIntrinsicProcessors = new KeyCommon.Processors.DataProcessors();
        
        private static KeyCommon.Processors.DataProcessors mRulesProcessors = new KeyCommon.Processors.DataProcessors();
        
        public static StoresCollection {get {return mStoresCollection;}}
        
        public static DataProcessors IntrinsicProcessors { get {return mIntrinsicProcessors;}}
        
        // user rules procesdor methods should reside in game##.dll  because the user types in the ComponentStores will only be kmown to it, the exe, and the scripts. Even if we dynamically loaded the user types into keystone.dll, the processing methods would again, exist in gsme##.dll, tye exe, or the scripts.  
        // in fact, i think we should enforce that the processibg scripts exist in gsme##.dll and not an entity script as the timingbof script loading for entities isnt reliable.
        
        public static DataProcessors RulesProcessors { get { return mUserRulesProcessors;}}
        
        public static UserDataStore UserDataStore { get {return mUserDataStore;} }
        
        internal static IResource[] Items
        {
            get
            {
                lock(mCache)
                {
                    IResource[] items = new IResource[mCache.Count];
                    mCache.Values.CopyTo( items,0);
                    return items;
                }
            }
        }
        
        // TODO: need to manage a seperate global keycache
        public static string GetNewName(Type type)
        {
            return GetNewName(type.Name);
        }

        public static string GetNewName(string typeName)
        {
            // TODO: according to MSDN there is a "very low probablilty" that guids will class.  TODO: when a map is finalized, i should run a test routine to make sure all guids are unique
            return System.Guid.NewGuid().ToString();

//            string key = typeName;
//            string tmp;
//            int i = 0;
//            do
//            {
//                i++;
//                tmp = key + i;
//            } while (_cache.ContainsKey(tmp));
//
//            return tmp;
        }

        public static IResource GetByName(string name)
        {
            foreach (Keystone.Resource.IResource resource in Resource.Repository.mCache.Values)
            {
                if (resource is Elements.Node)
                {
                    Elements.Node node = (Elements.Node)resource;
                    if (node.Name == name)
                    {
                        return node;
                    }
                }
            }
            return null;
        }
        // TODO: it'd be nice if we had a seperate _cache for entities so that "Get" 
        //       doesn't have to look for all the other various node types. This would make it
        //       faster for scripts.  Or if we implemented a seperate smaller cache for recent queries
        //       from IEntityAPI.
        // TODO: add a performance counter here on .Get() to see the impact especially as the _cache size grows
        public static IResource Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            // Get() is used extensively by EntitAPI scripts!  
            // We must be able to work with unlocked "Get" or we'll
            // have too many race conditions.  Also, we need to
            // ensure that an Entity is fully loaded before we allow
            // Simulation.cs to start calling Update() on an entity's Script.
            //using (mSychronizer.Lock (id))
            //{
            	IResource node = null;
            	
            	// determine if our most recent entity is the node we are looking for
            	// TODO: Get calls from multiple threads changes the mRecentEntity 
            	//       which is a module level var and no good.
//            	if (mRecentEntity != null && mRecentEntity.ID == id) 
//            		return mRecentEntity;
            	
            	mCache.TryGetValue (id, out node); 
            	
            	// unassign our most recent entity that we cache for performance reasons
//            	if (node != null && node is Entity)
//            		mRecentEntity = (Entity)node;
            	
	            return node;
            //}
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceName"></param>
        /// <remarks>
        /// WARNING: Dangerous method especially if we are using "id" as designations
        /// to other nodes like Animations and such.  Changing the id may break the
        /// reference and we may forget to update them to use the new id.
        /// </remarks>
        public static void RenameResource(IResource resource, string newName)
        {
            using (var syncLock = mSychronizer.Lock (resource.ID))
            {
            	lock (syncLock)
            	{
	                if (!mCache.ContainsKey(resource.ID))
	                {
	                    return;
	                	//throw new Exception("Repository.RenameResource() -- node '" + resource.GetType().Name + "' with id = '" + resource.ID.ToString() +
	                    //                                   "' does not exist.");
	                }

                    System.Diagnostics.Debug.Assert(resource.RefCount == 1); // NOTE we do not support renaming a resource if not in EDIT mode of a simple prefab. Renaming is only allowed during simple prefab editing and not at game runtime.

                    if (resource is Keystone.Appearance.Layer)
                    {
                        Node tmp = (Node)resource;
                        tmp.SetProperty("id", typeof(string), (object)newName);
                        System.Diagnostics.Debug.Assert (((Keystone.Appearance.Texture)resource).ResourcePath == newName);
                    }
                    else if (resource is Geometry)
                    {
                        // ParticleSystem is typically the only resource we would want to rename because it's the only type of Geometry 
                        // that allow us to modify the "Groups" via adding removing Emitters and Attractors.
                        // It's the type of resource where we need to constantly do a lot of tweaking to get good results
                        ((Geometry)resource).ResourcePath = newName;
                        ((Geometry)resource).SetProperty("id", typeof(string), (object)newName);
                        System.Diagnostics.Debug.Assert(((Geometry)resource).ResourcePath == newName);
                    }
                    else if (resource is Keystone.Shaders.Shader)
                    {
                        ((Keystone.Shaders.Shader)resource).ResourcePath = newName;
                        ((Keystone.Shaders.Shader)resource).SetProperty("id", typeof(string), (object)newName);
                        System.Diagnostics.Debug.Assert(((Keystone.Shaders.Shader)resource).ResourcePath == newName);
                    }
                    else throw new NotImplementedException("Repository.RenameResource() - node of type '" + resource.TypeName + "' not supported.");

	                // NOTE: here we directly remove from collection instead of calling Repository method.
	                // This avoids any changes to RefCount of the renamed node.
	                IResource value;
	                bool resultRemove = mCache.TryRemove (resource.ID, out value);
	                // TODO: the resource.ID is never updated! That's bad for IPageable's like textures
	                // SEE FormMain.Commands.Worker_ModImportGeometryAsEntity() where we clone the node, de-parent existing and re-parent with clone.
	                // But for now, NodeState.cs still uses this and I'm not sure if that is buggy because of the resource.ID never being updated
	                bool resultAdd = mCache.TryAdd (newName, resource);
	                if (resultAdd == false)
	                {
	                	// HACK - Feb.14.2016 - force removal of existing Node with same name:
	                	// is it because there is a NodeState node in the cache that is already stored at that "id"?
	                	// It appears so.  NodeState should be removed after deserialization shouldn't it but is not?
	                	IResource tmp;
	                	resultRemove = mCache.TryRemove (newName, out tmp);
	                	System.Diagnostics.Debug.Assert (tmp as NodeState != null);
	                	resultAdd = mCache.TryAdd (newName, resource);
	                	if (resultAdd == false)
	                	{
	                		Debug.WriteLine ("Repository.RenameResource() - ugh");
	                	}
	                }
	                //_cache.Remove(resource.ID
	                // _cache.Add(newName, resource);
            	}
            }
        }

		
        /// <summary>
        /// Called Server Side where "id" is generated.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Node Create(string typeName)
        {
            string id = Resource.Repository.GetNewName(typeName);
            return Create(id, typeName);
        }


        /// <summary>
        /// Single Create method allows us to lock the cache here rather than within the Repository.Get() 
        /// and Repository.Add() calls, so that we can hold a single lock during both .Get and .Add to avoid
        /// race condition inbetween those calls.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Node Create(string id, string typeName)
        {
            if (string.IsNullOrEmpty(id)) 
            	throw new ArgumentNullException("Repository.Create() - id cannot be null.");


            // TODO: we have calls to "Get" in KeyEdit and other areas and those should all be moved here
            //       too so that we can control the locks so that a pure .Get() with no expectation of .Create() will
            //       result in a lock/unlock whereas direct Repository.Get does no locking at all so that we can encompass
            // TODO: here where are we actually "locking" the mSychronizer.Lock object that is returned?
            using (var syncLock = mSychronizer.Lock (id))
            {
            	lock (syncLock)
            	{
		            Node node = null; 
		            if (mCache.ContainsKey(id))
		            {

		            	node = (Node)mCache[id];
                        
		            	if (node.TypeName != typeName)
		                    throw new Exception("Repository.Create() - This should not occur");
		
		                //System.Diagnostics.Debug.WriteLine("Repository.Create() - '"+ typeName + "' with id '" + id + "' retrieved from Repository.");
		                return node;
		            }
		
	
		            int parameterCount;
		            string[] parameterTypeNames;
		            // Generic Types
		            if (IsGeneric(typeName, out parameterCount, out parameterTypeNames))
		            {
		                Type genericType = Type.GetType(typeName); 
		                string[] constructorArgs = new string[]{id};
		
		                object genericInstance = Activator.CreateInstance(genericType, constructorArgs);
		                if (genericInstance == null || (genericInstance is Node == false)) throw new Exception();
		
		                node = (Node)genericInstance;
		
	    	            // TODO: what does the typeName look like for a generic type?
			            // say InterpolatedAnimation <Vector3d>
			            //  - upon creation of that type, based on the type we would know
			            //    which static interpolation function to assign.
			            //    - then we would deserialize the target and targetProperty
			            //      and use a static lambda based on that (that does mean that the lambda
			            //      must be from a fixed list)
		
		            
		                //// Keystone.Animation.InterpolatorAnimation.Create (
		                //Keystone.Animation.InterpolatorAnimation<Keystone.Types.Vector3d> test = new Keystone.Animation.InterpolatorAnimation<Keystone.Types.Vector3d>("test");
		                //System.Diagnostics.Debug.WriteLine(test.TypeName);
		
		                //Type t = test.GetType();
		                //System.Diagnostics.Debug.WriteLine(t.Name);
		                //System.Diagnostics.Debug.WriteLine(t.FullName);
		                //// the "`" character tells you it's a generic type
		                //// the number that follows tells you how many arguments it has 
		                //// thus you can use that to recreate the exact generic type in the factory.
		
		                //if (t.IsGenericType)
		                //{
		                //    Type g = t.GetGenericTypeDefinition();
		                //    System.Diagnostics.Debug.WriteLine(g.Name);
		                //    System.Diagnostics.Debug.WriteLine(g.FullName);
		
		                //    System.Diagnostics.Debug.WriteLine(t.IsGenericType.ToString());
		                //    Type[] typeArguments = t.GetGenericArguments();
		
		                //    System.Diagnostics.Debug.WriteLine(typeArguments[0].Name);
		                //}
		
		            }
		            else
		            {
		                // This and in ChildSetter() it blows with these near identicle switch{}
		                //  ugh.  i would much rather just have some parse code tied into 
		                switch (typeName)
		                {
		                    case "NodeState":
		                        node = new IO.NodeState(id);
		                        break;
		                    // TODO: This is a flaw, NO EntityBase derived type should need
		                    // a static .Create method because EntityBase derived classes cant
		                    // be shared! and the whole point of .Create() is for other nodes
		                    // to first check repository.get(id) to see if it can be shared
		                    // rather than reloaded.
		                    case "Occluder":
								break;
		                    case "SceneInfo":
		                        node = new Keystone.Scene.SceneInfo(id);
		                        break;
		                    case "Viewpoint":
		                        node = new Keystone.Entities.Viewpoint(id);
		                        break;
		                    case "Root":
		                        node = new Portals.Root(id);
		                        break;
		                    case "ZoneRoot":
		                        node = new Portals.ZoneRoot(id);
		                        break;
		                    case "Region":
		                        node = new Portals.Region(id);
		                        break;
		                    case "Zone":
		                        node = new Portals.Zone(id);
		                        break;
		                    case "Interior": // an interior structure such as deckplan for starship or space station
		                        node = new Portals.Interior(id);
		                        break;
	                       case "Structure": // an exterior structure such as terrain sandbox for away team missions
		                        node = new TileMap.Structure(id);
		                        break;
                            // TODO: the following celestial types should NOT have their own
                            // entity types. They should simply be scripted and use custom domain object properties
                            case "Background3D":
                                node = new Background3D(id);
                                break;
                            case "Star":
		                        node = new Celestial.Star(id);
		                        break;
		                    case "StellarSystem":
		                        node = new Celestial.StellarSystem(id);
		                        break;
		                    case "World":
		                        node = new Celestial.World(id);
		                        break;
                            case "DefaultEntity":
                                node = new DefaultEntity(id);
                                break;
		                    case "ModeledEntity":
		                    case "DynamicEntity":
		                        node = new ModeledEntity(id);
		                        break;
		                    case "Player":
		                    case "NPC":
		                    case "BonedEntity":
		                        node = new BonedEntity(id);
		                        break;
	
						   // NOTE: Not entirely relevant for proxies.  Proxies do get recycled and need static Create() as a result
						   //       however, by definition, proxies are generated dynamically at runtime and are never serialized to xml
						   //       and so never need to be deserialized.					   
	                       // case "Proxy3D":
	                       // case "Proxy2D":
	                       // case "Proxy2DControl":
		                        
		                    case "Vehicle":
		                        node = new Vehicles.Vehicle(id);
		                        break;
		                    case "Light":
		                    case "DirectionalLight":
		                        node = new Lights.DirectionalLight(id);
		                        break;
		                    case "SpotLight":
		                        node = new Lights.SpotLight(id);
		                        break;
		                    case "PointLight":
		                        node = new Lights.PointLight(id);
		                        break;
		
		                    // DOMAIN OBJECT
		                    case "DomainObject":
		                        node = new DomainObjects.DomainObject(id);
		                        //System.Diagnostics.Debug.Assert (false, "Factory.Create() - DomainObject should now be loaded by Entity directly from resourcePath."); // TODO: still need to get rid of hard coded for proceduralhelper.cs MakeDomainObject() and such
		                        break;

                            // PHYSICS
                            case "RigidBody":
                                node = new Physics.RigidBody(id);
                                break;
                            case "BoxCollider":
                                node = new Physics.Colliders.BoxCollider(id);
                                break;
                            case "SphereCollider":
                                node = new Physics.Colliders.SphereCollider(id);
                                break;
                            case "CapsuleCollider":
                                node = new Physics.Colliders.CapsuleCollider(id);
                                break;

		                    // MODELS
		                    case "Model":
		                        node = new Model(id);
		                        break;
		                    case "EntitySystemModel":
		                        node = new EntitySystemModel(id);
		                        break;
		                    //case "BonedModel": // boned model is typ eof LODModel so make sure this case is above LODModel
		                    //    node = BonedModel.Create(id);
		                    // break;
		                    //case "InstancedModel":
		                    //    node = InstancedModel.Create(id);
		                    // break;
		                    case "ModelSelector":
		                        node = new ModelSelector(id);
		                       // throw new Exception ("Repository.Create() - ModelSelector is an abstract node.");
		                        break;
		                    case "ModelSwitch":
		                        node = new ModelSwitch(id);
		                        break;
		                    case "ModelSequence":
		                        node = new ModelSequence(id);
		                        break;
		
		                    // APPEARANCE
		                    case "Appearance":
		                    	throw new NotImplementedException ();
		                    case "SplatAppearance":
		                         node = new SplatAppearance(id);
		                        break;
		                    case "DefaultAppearance":
		                        node = new DefaultAppearance(id);
		                        // TEMP HACK SHADOWMAPPING - but ultimately i think the shaders we use by default will simply be autoassigned here and
    							// if shadowmapping is enabled, we can switch the techniques or so.  Only for the default shader though.
    							// since special shaders for sky or water etc we treat differently
    							// TODO: for menu items, this shader is being assigned and it should not... so this HACK is not a permanent fix.
                                // TODO: can we just use "tvdefault" in LoadTVResource for appearance if shaderpath is null or empty?
                                // TODO: ideally, i would use my own default shader (or a vehicle specific shader) to treat star pointlights as Dir lights.
                                // TODO: can we add option for how to treat pointlights as a #define?  And can we
                                //       then make that rendering context specific so that we can compile a new version of the shader with the correct
                                //       #defines enabled?  Or should we make a star's light into a directional light and then update it's direction
                                //       vector prior to rendering each item?  I think this is the best method.  
                     			string shaderPath = "tvdefault";// @"caesar\shaders\PSSM\pssm.fx";      
		                        ((DefaultAppearance)node).ResourcePath = shaderPath;  			
		            			// NOTE: there is no need to set defines since Model.UpdateShader() 
		            			// will get called after the shader is added to the Appearance.
        			            // END TEMP HACK SHADOWMAPPING
		                        break;
		                    case "GroupAttribute":
		                        node = new GroupAttribute(id); // GroupAttributes/Appearances are never shared
		                        break;
		
		                    // TEXTURE LAYERS
		                    case "TextureCycle":
		                        node = TextureCycle.Create(id);
		                        break;
	                       case "SplatAlpha":
		                        node = new SplatAlpha (id);
		                        break;
		                    case "Diffuse": // TODO: layers are obsolete now that we've gotten rid of fixed function and texturemod info are now shader params
		                        node = new Diffuse(id);
		                        break;
		                    case "Specular":
		                        node = new Specular(id);
		                        break;
		                    case "NormalMap":
		                        node = new NormalMap(id);
		                        break;
		                    case "Emissive":
		                        node = new Emissive(id);
		                        break;
		                    case "VolumeTexture":
		                        node = new VolumeTexture(id);
		                        break;
		                    case "DUDVTexture":
		                        node = new DUDVTexture(id);
		                        break;
		                    case "CubeMap":
		                        node = new CubeMap (id);
		                        break;
		                    // MATERIAL IS RESOURCE
		                    case "Material":
		                        node = new Material(id);

		                        break;
		                    // TEXTURE AND TEXTUREATLAS IS RESOURCE
		                    case "Texture":
		                        node = new Texture(id);
		                        break;
	                       case "TextureAtlas":
		                        node = new TextureAtlas (id);
		                        break;
		                    // SHADERS
		                    case "ProceduralShader":
		                    //    // note: the shader resource descriptor gets read from file
		                    //    //string tempNewName = Resource.Repository.GetNewName(typeof(Shaders.ProceduralShader));
		                    //    node = new Shaders.ProceduralShader(id, id);
		                    //    break;
		                    case "Generic":
		                    case "Shader":
		                        node = new Shaders.Shader(id);
		                        break;

                            // Footprint
                            case "CellFootprint":
                                node = new CellFootprint(id);
                                break;
                            // GEOMETRY
                            case "Actor3d":
		                        node = new Actor3d(id);
		                        break;
		                    case "Mesh3d":
		                        node = new Mesh3d (id);
		                        break;
	                        case "Terrain":
		                        node = new Terrain (id);
		                        break;
	                       case "InstancedGeometry":
		                        node = new InstancedGeometry(id);
		                        break;
		                    case "InstancedBillboard":
		                        node = new InstancedBillboard (id);
		                        break;
	                        case "MinimeshGeometry":
		                        node = new MinimeshGeometry (id);
		                        break;
		                    //case "Minimesh2":
		                    //    node = Minimesh.Create(id);
		                    //    break;
		                   case "BillboardText":
		                        node = new BillboardText (id);
		                        break;
	                       case "TexturedQuad2D":
		                        node = new TexturedQuad2D (id);
		                        break;
		                    case "Billboard":
		                        node = Billboard.Create(id);
		                        break;
		                    case "BillboardChain":
		                        throw new NotImplementedException("Repository.Create() - " + typeName);
		                    case "ParticleSystem":
		                        node = ParticleSystem.Create(id);
                                break;
		                    case "Emitter":
		                        node = new Keystone.Elements.Emitter(id);
		                        break;
		                    case "Attractor":
		                        throw new NotImplementedException("Repository.Create() - " + typeName);
	
		                    // ANIMATIONS
		                    case "Animation":
		                        node = new Animation.Animation(id);
		                        break;
		                    case "BonedAnimation":
		                        node = Animation.BonedAnimation.Create(id);
		                        break;
		                    case "EllipticalAnimation":
		                        node = new Animation.EllipticalAnimation(id);
		                        break;
                            case "TextureAnimation":
                                node = Animation.TextureAnimation.Create(id);
                                break;
		                    case "KeyframeInterpolator_rotation":
		                        node = new Animation.KeyframeInterpolator<Keystone.Types.Quaternion>(id, "rotation");
		                        // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        Keystone.Types.Quaternion[] quats = new Keystone.Types.Quaternion[2];
		                        quats[0] = new Keystone.Types.Quaternion();
		                        quats[1] = new Keystone.Types.Quaternion();
		                        node.SetProperty ("keyframes" , typeof (Keystone.Types.Quaternion), quats);
		                        break;
		                 case "KeyframeInterpolator_Material_specular_power":
		                    // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        float[] power = new float[2];
		                        power[0] = 0f;
		                        power[1] = 0f;
		                        node.SetProperty ("keyframes" , typeof (float), power);
		                        break;
		                  case "KeyframeInterpolator_Material_opacity":
		                     // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        Keystone.Types.Vector3d[] scale = new Keystone.Types.Vector3d[2];
		                        scale[0] = Keystone.Types.Vector3d.Zero();
		                        scale[1] = Keystone.Types.Vector3d.Zero();
		                        node.SetProperty ("keyframes" , typeof (Keystone.Types.Vector3d), scale);
		                        break;
		                    case "KeyframeInterpolator_Light_diffuse":
		                       // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        Keystone.Types.Vector3d[] scale = new Keystone.Types.Vector3d[2];
		                        scale[0] = Keystone.Types.Vector3d.Zero();
		                        scale[1] = Keystone.Types.Vector3d.Zero();
		                        node.SetProperty ("keyframes" , typeof (Keystone.Types.Vector3d), scale);
		                        break;
		                    case
		                   ambient
		                   Emissive
		                   Specular
		                   "KeyframeInterpolator_Material_diffuse":
		                   ...TODO
		                       // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        Keystone.Types.Color[] color = new Keystone.Types.Color[2];
		                        color[0] = Keystone.Types.Color.Black();
		                      color[1] = Keystone.Types.Color.Black();
		                        node.SetProperty ("keyframes" , typeof (Keystone.Types.Color), color);
		                        break;
		                    case "KeyframeInterpolator_translation":
		                        node = new Animation.KeyframeInterpolator<Keystone.Types.Vector3d>(id, "translation");
		                        // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        Keystone.Types.Vector3d[] translation = new Keystone.Types.Vector3d[2];
		                        translation[0] = Keystone.Types.Vector3d.Zero();
		                        translation[1] = Keystone.Types.Vector3d.Zero();
		                        node.SetProperty ("keyframes" , typeof (Keystone.Types.Vector3d), translation);
		                        break;
		                    case "KeyframeInterpolator_scale":
		                        node = new Animation.KeyframeInterpolator<Keystone.Types.Vector3d>(id, "scale");
		                        // interpolator needs minimum 2 keyframes otherwise delete the interpolator animation clip
		                        Keystone.Types.Vector3d[] scale = new Keystone.Types.Vector3d[2];
		                        scale[0] = Keystone.Types.Vector3d.Zero();
		                        scale[1] = Keystone.Types.Vector3d.Zero();
		                        node.SetProperty ("keyframes" , typeof (Keystone.Types.Vector3d), scale);
		                        break;
		                    
		                    // BEHAVIOR TREE NODES (recall, we use a BehaviorTree for our Viewpoint controller logic, but those should have SERIALIZEABLE = false)
	   	                    case "Selector":
			                    node = new Behavior.Composites.Selector (id);
			                    break;
		                    case "Sequence":
		                        node = new Behavior.Composites.Sequence(id);
		                        break;
		                    case "Script":
		                        node = new Keystone.Behavior.Actions.Script(id);
		                        break;
	                       case "Action":
		                        node = new Keystone.Behavior.Actions.Action (id);
								break;	
		                    default:
		                        throw new NotImplementedException("Repository:Create() - Unsupported node type '" + typeName + "'");
		                }
		            }
		
		
		            // When an Entity is CREATED (regardless of whether it's yet added to the Scene)
		            // assign IntrinsicDataStores to the Entity.
		            // NOTE: these resources are reclaimed here in Repository.DecrementRef() when the Entity is DISPOSED
		            if (node is Entity)
		            {
		                RentMemoryStores(node as Entity);
		            }
		            
		        
            
            
            
		            System.Diagnostics.Debug.Assert (node.ID == id, "Repository.Create() - IDs MUST MATCH."); //verify no funky unsafe issue with id assignment
		            System.Diagnostics.Debug.Assert (mCache.ContainsKey (id), "Repository.Create() - ID should now exist in Repository Cache after Create() call.");
		            // TODO: above assert somehow is failing.  Is it possible somehow that inbetween the node being added to the repository it is then being removed?
		            return node;
            	}
            }
        }

        public static void Destroy(IResource node)
        {
        	using (var syncLock = mSychronizer.Lock (node.ID))
            {
        		lock (syncLock)
        		{
        			// for IPageableTVNode set PageStatus == Unloading
        			throw new NotImplementedException("Repository.Destroy() - ERROR: Method not implemented.");
        		}
        	}
        }

        private void RentMemoryStores(Entity e)
        {
            int DEFAULT_STORE_SIZE = 1024;
            
            KeyCommon.Data.UserData data = mUserDataStore.CheckOut();
            
            ComponentStore<Keystone.Data.IntrinsicDataStores.AdvancedEntityData> store = 
              mStoresCollection.CheckOut<Keystone.Data.IntrinsicDataStores.AdvancedEntityData>(DEFAULT_STORE_SIZE);
              
            // grab an available memory record and assign it to the entity
            System.Memory.Memory<Keystone.Data.IntrinsicDataStores.AdvancedEntityData> mem = store.CheckOut();
            e.IntrinsicData = mem;
            e.BlackboardData = data;
        }
        
        private void ReclaimMemoryStores (Entity e)
        {
              // Reclaim the intrinsic data storage and UserData objects
              mUserDataStore.CheckIn (e.BlackboardData, e.ID);
              e.BlackboardData = null;
              
              // todo: we are just checking in the single Memory<T> record of this entity within
              ////       a ComponentStore of type <Keystone.Data.IntrinsicDataStores.AdvancedEntityData>
              mStoresCollection.CheckIn<Keystone.Data.IntrinsicDataStores.AdvancedEntityData>(e.IntrinsicData);
              e.IntrinsicData = null;
        }
        
        public static void IncrementRef(IResource child)
        {
        	using (var syncLock = mSychronizer.Lock (child.ID))
            {
        		lock (syncLock)
        		{
		            if (!mCache.ContainsKey(child.ID))
		            {
                        //return; // HACK - dec.17.2022 - nodes of type SceneInfo are leaking because they are not getting decremented and removed from the Repository
		                throw new Exception("Repository.IncrementRef() -- node '" + child.GetType().Name + "' with id = '" + child.ID +
		                                    "' does not exist.");
		            }

		            // note: Wait, the reason we increment even if parent is not ultimately connected to root
		            // is because we still want to know when a node is shared.  It may very well be that
		            // a node can be shared in multiple places that are still not connected to the scene.  We want that
		            // to be reflected in the refcount.  
		            // So our real concern is how to notify the scene that a node has been added to the Scene and needs to be
		            // added to the tree.
		            mCache[child.ID].RefCount++; // viewpoint issue somehow related to zones instead of single region when switching workspaces
		                                                    // i think maybe the viewpoint is removed from scene when we only intend to suspend it when switching workspaces
		                                                    // then when we try to go back to it, its no longer in repository
		            
		            //Debug.WriteLine(node.GetType().Name + " '" + node.Key + "' ref count = " + node.RefCount);
		
		            QueuePageableResource ((Node)child);
		            
		            
//		            if (child is Shaders.Shader)
//		            {
//        	        	// TODO: if vp and context aren't initialized, this will fail.  What i should do is
//			        	//       have an event raised in all Context's that are loaded whenever a shader is added/removed
//			        	//       from Repository
//			        	
//			        	if (CoreClient._CoreClient.Viewports != null)
//			        	{
//			        		Cameras.Viewport[] viewports = new Keystone.Cameras.Viewport[CoreClient._CoreClient.Viewports.Count];
//			        		CoreClient._CoreClient.Viewports.Values.CopyTo (viewports, 0);
//				        	foreach (Keystone.Cameras.Viewport vp in viewports)
//			        			vp.Context.ParallelSplitShadowMapping.AddShader ((Shaders.Shader)child);
//			        	}
//		            }
        		}
        	}
        }
        
        /// <summary>
        /// If a node has a recount == 0 after it's decremented, it gets removed from the scene.  
        /// NOTE: We do NOT / SHOULD  NOT recurse child nodes and decrementref them.  
        ///  see Node.cs.RemoveParent for more details on how child nodes are recursively removed from parents
        ///  only when their own parent count has dropped to 0.  
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public static void DecrementRef(IResource node)
        {
            using (var syncLock = mSychronizer.Lock (node.ID))
            {
            	lock (syncLock)
            	{
		            if (mCache.ContainsKey(node.ID))
		            {
		                IResource tmp = mCache[node.ID];
		                System.Diagnostics.Debug.Assert(tmp == node, "Repository.DecrementRef() - ID's '" + node.ID + "' match but nodes are not the same reference!");
		                node.RefCount--;
                        //Debug.WriteLine(node.GetType().Name + " '" + node.Key + "' ref count = " + node.RefCount);

                    if (node.RefCount == 0)
		                {
		                    if (node is Entity)
		                    {
		                        ReclaimMemoryStores(node as Entity)
		                    }
		                    //if (node is Celestial.World)
		                        // TODO: i think if our script is running after having just loaded
		                        // a celled region with assetplacement tool and then we place an instance
		                        // and then unload the tool while the script that is setting collapsed stat4es
		                        // of floors and setting the UVs to fit the correct sub-texture in atlas, then
		                        // removing the celledRegion now will result in the script failing.  One thing 
		                        // would be to suspend scripts when loading assetplacment version of an entity
		                        // as well as the preview version
		                    //    Debug.WriteLine("Repository.DecrementRef() = 0 '" + node.TypeName + "'");
		                    //System.Diagnostics.Debug.WriteLine ("Repository.DecrementRef() - Item ID " + node.ID + " BEING REMOVED FROM cache.");
	                		//mCache.Remove(node.ID);
	                		IResource value;
	                		bool result = mCache.TryRemove (node.ID, out value);
	                		System.Diagnostics.Debug.Assert (result == true, "Repository.DecrementRef() - ERROR: Cache should always contain the node we are trying to remove at this point.");
	                		
	                		// unassign our most recent entity that we cache for performance reasons
//	                		if (value != null && mRecentEntity != null && mRecentEntity.ID == node.ID)
//	                			mRecentEntity = null;
	                		
	                		System.Diagnostics.Debug.Assert (mCache.ContainsKey (node.ID) == false);
	                		node.Dispose();
		                }
		            }
		            //else throw new ArgumentException(string.Format("Repository.DecrementRef() -- Node '{0}' of type '{1}' doesn't exist.", child.ID, child.TypeName));
            	}
            }
        }
        
        /// <summary>
        /// Called by Keystone.Elements.Node  constructor()   Nowhere else.  
        /// NOTE: When a Node is constructed, IPageable.LoadTVResource() is not called.
        /// LoadTVResource() is only called when the object is added to another node.
        /// </summary>
        /// <remarks>
        /// We DO NOT increment a reference count on Add because our philosophy is to ONLY increment
        /// when items are added to a scene or too another entity.
        /// </remarks>
        /// <param name="key"></param>
        /// <param name="item"></param>
        internal static void Add(IResource item)
        {
            // TODO: I suspect this isn't needed as it occurs on Node creation which occurs through the .Create() call which uses the sychronizer
            //       but it shouldnt hurt because that same thread context will have this lock
            using (var syncLock = mSychronizer.Lock(item.ID))
            {
                lock (syncLock)
                { 
                Trace.Assert(!mCache.ContainsKey(item.ID));
                //System.Diagnostics.Debug.WriteLine ("Repository.Add() - Item ID " + item.ID + " being added to cache.");
	            mCache.TryAdd (item.ID, item);
	            //_cache.Add(item.ID, item);
	            System.Diagnostics.Debug.Assert (mCache.ContainsKey (item.ID));
	            System.Diagnostics.Debug.Assert (item.RefCount == 0);
	            
        	    }
            }
        }

        // external callers trying to remove should first IncrementRef() then DrecrementRef() the resource
        // Then if the res.RefCount == 0 it will unload, otherwise IT WILL NOT (by design since that res is still in use)
        private static void Remove(IResource res)
        {
        	throw new Exception ("Repository.Remove() - This is not supported.  Use IncrementRef/DecrementRef cycle to ensure res.RefCount == 0");
        }
        
        private static bool IsGeneric(string typename)
        {
            return typename.Contains("`");
        }

        private static bool IsGeneric(string typename, out int parameterCount, out string[] parameterTypeNames)
        {
            // the "`" character tells you it's a generic type
            // the number that follows tells you how many arguments it has 
            // thus you can use that to recreate the exact generic type in the factory.
            int start = typename.IndexOf("`");
            bool result = start > -1;
            parameterCount = 0;
            parameterTypeNames = null;
            if (result)
            {
                start += 1;
                int length = 1; // typename.Length - start;
                parameterCount = int.Parse(typename.Substring(start, length));
            }
            return result;
        }
        
        private static void QueuePageableResource (Node node)
        {

        	// TODO: all of this is causing a ton of problems as it relates to loading
        	//       things like HUD models and even CelledRegion interior nodes.  I'm not sure why yet
        	//       but the interior db is not loading 
            bool isConnected = node.IsConnectedToScene ();
//            if (isConnected)
//            	System.Diagnostics.Debug.WriteLine (node.TypeName + " is connected.");
//            else 
//            	System.Diagnostics.Debug.WriteLine (node.TypeName + " NOT connected.");
            
            if (isConnected == false) return;
            

        	if (node is IPageableTVNode)
        		QueuePageableResource ((IPageableTVNode)node);
        	
//          // recursion only occurs in scene.EntityAttached()
//        	// TODO: seems to fail to queue models added from HUD
//        	// recurse children if applicable. note: this can fail to work properly if we 
//        	// are recurisvely loading children in SceneReader (eg we forgot to set context.RecursivelyLoadChildren == false)
//        	// when we do not intend to. 
//        	if (node is IGroup)
//        	{
//        		IGroup g = (IGroup)node;
//        		if (g.Children !=null)
//        		{
//        			for (int i = 0; i < g.Children.Length; i++)
//        			{
//        				// note: we have to recurse child entities because if they were previously unconnected to scene
//        				// by virtue of being attached to a unconnected Root node, then every single hierarchy of entity 
//        				// under that root will be disconnected and their resources and child resources will never get
//        				// paged in.
//        				//if (g.Children[i] is Entities.Entity) continue; // do not recurse child entities <-- WRONG! we must recurse
//        				QueuePageableResource (g.Children[i]);
//        			}
//        		}
//        	}	
            
        }
        
        private static void QueuePageableResource (IPageableTVNode node)
        {

        	// if this node has just connected, which it may have since we know its refcount was just increased
        	// then we need to recurse this node down to all non child Entities
        	
    		if (!(node.TVResourceIsLoaded)) // || node.PageStatus != PageableNodeStatus.Loaded) // TODO: i should add in this || line so if either IsLoaded or status = Loaded it skips?
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
        	
        // TODO:  Good place to add some memory tracking on both Add/Remove and on IncrementRef / DecrementRef perhaps...
        //long StopBytes = 0;
        //public int GetSize(object foo)
        //{
        //    // do this prior to instantiating the object
        //    long StartBytes = System.GC.GetTotalMemory(true);

        //    // do this after instantiating the object
        //    StopBytes = System.GC.GetTotalMemory(true);
        //    GC.KeepAlive(foo); // This ensure a reference to object keeps object in memory

        //    Trace.WriteLine("Size is " + ((long)(StopBytes - StartBytes)).ToString());
        //}


    }
}