//#define TVPhysics
//#define GRAVITATION;
//#define USE_PROCESSORS;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Portals;
using Keystone.Resource;
using Keystone.Physics;
using Keystone.Physics.BroadPhases;
using Keystone;
using Keystone.Types;
using KeyCommon.DatabaseEntities;
using KeyCommon.Flags;
using KeyCommon.Data;
using KeyCommon.Processors;
using Keystone.Scene;
using Keystone.Simulation;
using System.IO;

namespace KeyEdit
{
    internal class Interest  // area of interest
    {
        // what if we were to come up with an area of interest that actually
        // used AREAS (like our spatial nodes) to track interest...
        // The idea being that 10 ships in the same area could then simply share 
        // that same Area's interest data.  
        // Thus, the area knows it's interest and that interest is shareable by those 
        // that are in the area
        // And what if we could come up with some novel hierarchical AoI data structure?
        // Something like spare octree grid
 
    }



    /// <summary>
    /// Simulation is mostly about physics and movement whereas the Game object
    /// is about scoring and time limits and objectives and such.  Game is a business object 
    /// Similarly a KeyCommon.Entities.Player tracks player scores, kills and such and is not
    /// related to a Scene Entity "PlayerCharacter" or "NPC" etc   except that a KeyCommon.Entities.Player
    /// will have a link to it's corresponding controllable player entities (ship + avatar)
    /// </summary>
    public class Simulation : Keystone.Simulation.ISimulation
    {

        private Game mGame;
        
        private Scene mScene;
        
        private Dictionary<uint, List<Entity>> mProduction;

        private Keystone.Simulation.Missions.Mission mCurrentMission;

        private Timers.GameTime mGameTime;
        
        // phsyics timing vars
        private const int MAX_STEPS = 20; // TODO: with timeScaling, this value must increase if we don't want to lose precision/stability of orbits
        private bool FIXED_FREQUENCY_PHYSICS = false;
        private uint _physicsHertzInTimesPerSecond = 100;
        private double _fixedTimeStep = 0.1d; 
        private double _elapsedSecondsPhysics;
        
        //private Collision.CollisionTest _collision;
        private float _gravity;
        private bool _isPaused;
        private bool _isRunning;
        
        
        //private SimTime _time;  ISimSystem[] mSystems; //includes GameTime and DayNightCycle
                
        private bool _collisionEnable = false;
        protected bool _disposed;

        //private JigLibX.Physics.PhysicsSystem _physicsSystem;
        private Keystone.Traversers.EntityQuery mEntityQuery;
        private Jitter.World mPhysicsWorld;
        private Jitter.Collision.CollisionSystem mCollisionSystem;


        // TODO: some variable values should be passed in after being read from INI file
        public Simulation(float gravity, Game game)
        {
            if (game == null) Debug.WriteLine("Simulation.Ctor() - WARNING: Game argument == null");
            mGame = game;
            
            mEntityQuery = new Keystone.Traversers.EntityQuery();

            _gravity = gravity;
            
            _physicsHertzInTimesPerSecond = 100;
            _fixedTimeStep = 1.0d / _physicsHertzInTimesPerSecond;
            
            
            // NOTE: When Entities are Created, the Intrinsic datatypes from
            // Keystone.Data.IntrinsicData  are assigned to Stores.  So here
            // all we need to do is wire up the Processors 
            WireIntrinsicProcessors();
            
            
        }


#region Intrinsic Processors
        private static void WireIntrinsicProcessors()
        {
           Repository.IntrinsicProcessors.Add ("STEER", IntrinsicProcessor_Steer);
           
        }
        
        private static void IntrinsicProcessor_Steer(Scene scene, ComponentStore<T> store, object parameters, GameTime gt)
        {
            Entities[] entities = scene.ActiveEntities;
            Span<T> mem = store.Span;
            
            for (int i = 0; i < store.Size; i++)
            {
                
            }
        }
        
        private static void ReleaseIntrinsicProcessors()
        {
            // todo: 
            Repository.IntrinsicProcessors.Clear();
        }
#endregion


        #region ISimulation Members
        public Keystone.Scene.Scene Scene { get { return mScene; } set { mScene = value; } }
        
        public Game Game { get { return mGame; } }
        
        /// <summary>
        /// Simulation can only have one current mission at a time.
        /// </summary>
        public Keystone.Simulation.Missions.Mission CurrentMission { get { return mCurrentMission; } set { mCurrentMission = value; } }

        public Keystone.Timers.GameTime GameTime {get {return mGameTime;}}
        
        public uint PhysicsHertzInTimesPerSecond
        {
            get { return _physicsHertzInTimesPerSecond; }
            set
            {
                if (value == 0) throw new Exception("physics hertz should typically be 30 - 100");
                _physicsHertzInTimesPerSecond = value;
            }
        }

        public bool Paused
        {
            get { return _isPaused; }
            // TODO: when unpausing, how do we resume without having elapsed some huge value?
            // TODO: i think somehow it must 
            // maybe we could set the elapsed time to always == 0 for the physics when paused
            // this way we can still animate some things while paused... but still
            // other things we would not want animate
            set { _isPaused = value; }
        }

        public bool CollisionEnabled
        {
            get { return _collisionEnable; }
            set { _collisionEnable = value; }
        }

        public bool Running
        {
            get { return _isRunning; }
            set { _isRunning = value; }
        }

        int mNPCMaterial;
        int mElevatorMaterial = -1;
        private void InitializePhysicsEngine()
        {
            // TODO: Switch to Jitter. Actually, im going to compare TV's Newton physics and Jitter and see which is the best fit.
            // READ http://software.intel.com/en-us/blogs/2009/06/24/highlights-and-challenges-during-ghostbusters-development-part-2/
            // for ideas on parallelizing physics down the road

#region Jitter
            // https://github.com/mattleibow/jitterphysics/wiki/Tutorial-1:-World-Creation-&-Raycasting
            mCollisionSystem = new Jitter.Collision.CollisionSystemSAP();
            mCollisionSystem.CollisionDetected += OnCollisionDetected;
            
            //mCollisionSystem.PassedBroadphase

            // TODO: we don't need to use the World system if we don't want dynamics and just collisions right?
            mPhysicsWorld = new Jitter.World(mCollisionSystem);
            
            // todo: our "pause" game needs to suspend physics updates as well.
            // todo: when adding a BoxCollider to an Entityin the Scene, it should default size to Entity.BoundingBox
            // todo: maybe make Jitter a global resource allocator like TVMaterailFactory?
            // todo: do these nodes need to be Ipageable?  Or do we wait til the entire Entity is added to Simulation and then find the physics/colliders and add them to the Simulation?
            //       We DO NOT want them added to the Simulation when we are just loading via AssetPlacementTool so that seems to indicate we'd do it after the Entity has been added to the Simulation. 
            //       TODO: what do we do when they are added to the Simulation and we want to change a property?  How do we get those changes updated in the Physics Simulation?
            //
            // (do we get seperate notifications for child Entities added from a compound Prefab? I think we do.
            // todo: update our Create() method to create nodes of the types we need.
            // tood: update ChildSetter() to support the new nodes we need.  Our other traversers can ignore these nodes. (culler and drawer)
            //
            // These are non-shareable right?  Well, only if we have to keep the transform values in them, otherwise collider shapes should be shareable across prefabs such as crew that all use same CapsuleCollider
            // Let's make them non-shareable for now until we know more.  But these are the nodes we need.  We just need to make sure we can substitute Jitter for TVPhysics so we can performance test.
            // 
            // TODO: TVPhysics seems to have colliders as children of the RigidBody node since to create a collider, you need to pass in the index of the RigidBody it's attached to.
            // I think Unity3d is the same.  The collider's move with the RigidBody.  So my Collider implementations can inherit from Node and RigidBody can implement IGroup or inherit Group node.

            //RigidBody : Group // can contaim multiple colliders for Compound Colliders <-- TODO: actually, what ijust did is place colliders under Entity along with RigidBody.
            //                  // they should easily be able to get references to each other since they share the same parent Entity.
            //CapsuleCollider : Node
            //BoxCollider : Node
            //SphereCollider : Node

            // after i run simulation.Update() and update entity scripts so that movement occurs for our npc crew, i can update the Physics and see if there was a collision.



            //tvphysics.
            //tvphysics.Simulate ()
            //int physicsMaterial = tvphysics.CreateMaterialGroup(name);
            //tvphysics.SetMaterialInteractionCollision(physicsMaterial, physicsMaterial2, true);
            //tvphysics.TestCollision ()
            
            //tvphysics.SetBodyPosition() // will updating TVPhysics result in the Meshes matrices being updated when we don't want them to?  We want the Entity's matrices to be updated and so would we have to 
            //tvphysics.GetBodyMatrix() each frame for every entity? for tvphysics, only for each collision event perhaps?
            //tvphysics.

            //int eventID = tvphysics.PollEvents();
            // while (eventID >= 0)
            // {
            //      TV_EVENT_PHYSICSCOLLISION coll = pEngine.GetEventCollisionDesc(eventID); 
            //    
            //      tvphysics.GetEventType();      
            //      tvphysics.AdvancedCollision(tvGlobals.Vector3(ballx, bally, ballz), tvGlobals.Vector3(ballx, bally - 10, ballz),out mColResult);
            //      // do interesting stuff here
            //
            //      // poll next event if any
            //      eventID = tvphysics.PollEvents();
            // 
            // }

#endregion

#region JigLibX
            //_physicsSystem = new PhysicsSystem();
            //_physicsSystem.SetGravity(0, gravity, 0);

            //_physicsSystem.CollisionSystem = new JigLibX.Collision.CollisionSystemSAP();
            //_physicsSystem.EnableFreezing = true;
            //_physicsSystem.SolverType = PhysicsSystem.Solver.Normal;
            //_physicsSystem.CollisionSystem.UseSweepTests = true;

            //_physicsSystem.NumCollisionIterations = 10; // 8;
            //_physicsSystem.NumContactIterations = 10; // 8;
            //_physicsSystem.NumPenetrationRelaxtionTimesteps = 15
#endregion
        }

        // TODO: what if physics object's properties change? Ideally, they should not change at run-time unless
        //       it is editing of the prefab and we are assigning values such as mass, trigger, center, size, radius, etc
        public void RegisterPhysicsObject(Entity entity)
        {
            if (entity.RigidBody != null)
            {

#if TVPhysics
                if (CoreClient._CoreClient.Physics == null)
                {

                    CoreClient._CoreClient.Physics = new MTV3D65.TVPhysics();
                    CoreClient._CoreClient.Physics.Initialize();
                    CoreClient._CoreClient.Physics.SetMultiThreadSolvePerIsland(true);
                    CoreClient._CoreClient.Physics.RenderDebugInfo(true);
                }

                int bodyID = CoreClient._CoreClient.Physics.CreateBody(1.0f);
                entity.RigidBody.TVIndex = bodyID; // non serializable internal property
                CoreClient._CoreClient.Physics.SetBodyTag(bodyID, entity.ID);


                if (entity.BoxCollider != null)
                {
                    Vector3d halfSize = entity.BoxCollider.Size / 2d;
                    MTV3D65.TV_3DVECTOR min, max;
                    min.x = (float)-halfSize.x;
                    min.y = (float)-halfSize.y;
                    min.z = (float)-halfSize.z;
                    max.x = (float)halfSize.x;
                    max.y = 2f; // (float)halfSize.y;
                    max.z = (float)halfSize.z;
                    CoreClient._CoreClient.Physics.AddBox(bodyID, min, max);
                    CoreClient._CoreClient.Physics.SetBodyMovable(bodyID, entity.RigidBody.Static);
                    CoreClient._CoreClient.Physics.SetBodyCollidable(bodyID, true);
                    // physics events are not enabled by default so we need to enable them in the material
                    if (mElevatorMaterial == -1)
                    {
                        mElevatorMaterial = CoreClient._CoreClient.Physics.CreateMaterialGroup();
                        
                    }
                    //CoreClient._CoreClient.Physics.SetMaterialInteractionEvents(pMatFloor, pMatBox, true, true, false);
                    CoreClient._CoreClient.Physics.SetBodyMaterialGroup(bodyID, mElevatorMaterial);
                }
                else if (entity.SphereCollider != null)
                {
                    MTV3D65.TV_3DVECTOR center = Helpers.TVTypeConverter.ToTVVector(entity.SphereCollider.Center);
                    CoreClient._CoreClient.Physics.AddSphere(bodyID, center, (float)entity.SphereCollider.Radius);
                }
                else if (entity.CapsuleCollider != null)
                {
                    MTV3D65.TV_3DVECTOR center = Helpers.TVTypeConverter.ToTVVector(entity.CapsuleCollider.Center);
                    CoreClient._CoreClient.Physics.AddCapsule(bodyID, center, (float)entity.CapsuleCollider.Radius, (float)entity.CapsuleCollider.Height);
                    CoreClient._CoreClient.Physics.SetBodyMovable(bodyID, true);
                    CoreClient._CoreClient.Physics.SetBodyCollidable(bodyID, true);

                    if (mNPCMaterial == -1)
                    {
                        mNPCMaterial = CoreClient._CoreClient.Physics.CreateMaterialGroup();
                        CoreClient._CoreClient.Physics.SetMaterialInteractionContinuousCollision(mNPCMaterial, mElevatorMaterial, true);
                        CoreClient._CoreClient.Physics.SetMaterialInteractionCollision(mNPCMaterial, mElevatorMaterial, true);
                        CoreClient._CoreClient.Physics.SetMaterialInteractionEvents(mNPCMaterial, mElevatorMaterial, true, true, false);
                    }
                    CoreClient._CoreClient.Physics.SetBodyMaterialGroup(bodyID, mNPCMaterial);
                }
                else
                {
                    entity.RigidBody.TVIndex = -1;
                    CoreClient._CoreClient.Physics.DestroyBody(bodyID);
                    return;
                }

#else
 
                if (mPhysicsWorld == null)
                    InitializePhysicsEngine();

                Jitter.Collision.Shapes.Shape shape = null;

                if (entity.BoxCollider != null)
                {
                    Vector3d size = entity.BoxCollider.Size;
                    shape = new Jitter.Collision.Shapes.BoxShape((float)size.x, (float)size.y, (float)size.z);
                    shape.Tag = entity.BoxCollider;
                    entity.BoxCollider.Shape = shape;
                }
                else if (entity.SphereCollider != null)
                {
                    double radius = entity.SphereCollider.Radius;
                    shape = new Jitter.Collision.Shapes.SphereShape((float)radius);
                    shape.Tag = entity.SphereCollider;
                    entity.SphereCollider.Shape = shape;
                }
                else if (entity.CapsuleCollider != null)
                {
                    double radius = entity.CapsuleCollider.Radius;
                    double length = entity.CapsuleCollider.Height;
                    shape = new Jitter.Collision.Shapes.CapsuleShape((float)length, (float)radius);
                    shape.Tag = entity.CapsuleCollider;
                    entity.CapsuleCollider.Shape = shape;
                }

                // TODO: should we return a bool so we know if Entity needs to maintain the 
                //       changestates flag or is it not necessary since when the 
                //       shape node is added, it creates a new flag
                if (shape == null) return;

                // TODO: verify that a rigid body component for this entity doesn't already exist!  Otherwise we should remove it from physics engine and recreate using new collider
                
                Jitter.Dynamics.RigidBody body = new Jitter.Dynamics.RigidBody(shape);
                body.Tag = entity;

                body.IsStatic = entity.RigidBody.Static;
                System.Diagnostics.Debug.Assert(entity.RigidBody.Mass > 0d, "mass of 0 not allowed. throws exception.");
                body.Mass = (float)entity.RigidBody.Mass; 
                entity.RigidBody.Body = body;
                
                //Jitter.Dynamics.Material material = new Jitter.Dynamics.Material();
                //body.Material.Restitution = 0;
                //body.Material.KineticFriction = 0;
                //body.Material.StaticFriction = 0;
                // TODO: how come passing in IBroadphaseEntity to mCollisionSystem still returns 2 rigid bodies in the OnCollisionDetected() event handler?
                //       what if i don't want to use rigid bodies?  Does it just create those rigid bodies for us in the handler?
                //       And IBroadphaseEntity only seems to have a bounding box member.  Surely that's not good enough for collision detection?
                Jitter.Collision.IBroadphaseEntity broadPhaseEntity;

                mCollisionSystem.AddEntity(body);
                // adding a body to the world automatically adds it to the collision system. what happens if it gets added twice?
                //mPhysicsWorld.AddBody (body);
#endif
            }
        }

        public void UnRegisterPhysicsObject(Entity entity)
        {
#if TVPhysics
            CoreClient._CoreClient.Physics.DestroyBody(entity.RigidBody.TVIndex);
#else
            // NOTE: removing body from world also removes it from collision system.
            Jitter.Dynamics.RigidBody body = entity.RigidBody.Body;
            mPhysicsWorld.RemoveBody(body);
#endif
        }

        public void UnRegisterProducer(uint productID, Entity entity)
        {
            mProduction[productID].Remove(entity);
        }

        public void RegisterProducer(uint productID, Entity entity)
        {
            if (mProduction == null) mProduction = new Dictionary<uint, List<Entity>>();
            List<Entity> producers;
            bool exists = mProduction.TryGetValue(productID, out producers);
            if (!exists)
                mProduction[productID] = new List<Entity>();

            mProduction[productID].Add(entity);

            // todo: ideally this ISimulation implementation should be in the EXE because we need to know the game specific productIDs and what they refer to
            // todo: how and where is the Hz for each productID defined?  Perhaps its just the job of this Simulation implementation which should be implemented in the EXE, not Keystone.dll
        }

        Keystone.Vehicles.Vehicle mSpawnedVehicle;
        FileStream mFileStream;
        bool mSpawnInProgress = false;
        bool mSpawnCompleted = false;
        bool mTransferInProgress = false;
        string mTransferGuid = null;
        int mTotalBytesSent = 0;
        int mFragmentIndex = 0;

        /// <summary>
        /// Loopback Server Simulation update
        /// </summary>
        /// <param name="gameTime"></param>
        /// <returns></returns>
        private double UpdateLoopback(Keystone.Simulation.GameTime gameTime)
        {
            // are we transfering a stream or file? if so continue until finished
            // server needs to instantiate an Entity db and client and save it as temporary file, unload it (since its just loopback), then send it
            // client needs to load it with the existing node IDs.  Also need to indicate the parent node ID if mFileTransfer.mParentID > -1;
            
            if (this.Scene.Enable && this.Running)
            {
                Entity[] serverEntities = mScene.ServerEntities;
                if (serverEntities != null)
                    for (int i = 0; i < serverEntities.Length; i++)
                    {
                        serverEntities[i].Update(gameTime.ElapsedSeconds);
                    }
                

                // todo: if there is no mission but ArcadeEnabled = true, perhaps we can still
                // do things.  Afterall, some Entities like spawnpoints can exist as Scene objects and not missionObjects.
                if (mCurrentMission == null || !mCurrentMission.Enable) return 0;
                
                // determine if mission success or failed criteria have been reached

                // NOTE: mCurrentMission.Update() must occur even if the player controlled Entity is not yet spawned 
                //       because the mCurrentMission itself might be the source of the spawning of the player controlled Entity!
                Keystone.Simulation.Missions.Mission.MissionState state = mCurrentMission.Update(gameTime.ElapsedSeconds);
                if (state == Keystone.Simulation.Missions.Mission.MissionState.Completed)
                {
                    KeyCommon.Messages.MissionResult result = new KeyCommon.Messages.MissionResult();
                    result.Success = true;
                    AppMain.Form.ServerSendMessage(AppMain._core.Settings.settingRead("network", "username"), result);
                }
                else if (state == Keystone.Simulation.Missions.Mission.MissionState.Failed)
                {
                    KeyCommon.Messages.MissionResult result = new KeyCommon.Messages.MissionResult();
                    result.Success = false;
                    AppMain.Form.ServerSendMessage(AppMain._core.Settings.settingRead("network", "username"), result);
                }

                if (AppMain.mPlayerControlledEntity == null) return 0;


                // for now, lets just focus on spawning around the client's vehicle
                // find spawn points if any, near player vehicle
                // determine if we've already spawned for a particular spawnPoint 
                // TEMP: - below is to just spawn a vehicle in the absense of any spawnpoints existing.
                // HOWEVER, we will also support any Entity including Vehicles as being placed in the scene and serialized/deserialized
                //      - but the state for those Entities may not be resumeable... they will alway load back at the same point.
                //      This is good for games where we just want to be able to load a scene with a default empty mission that is playable.

                bool tempHackSpawnVehicle = false;
                if (tempHackSpawnVehicle && mSpawnedVehicle == null && !mSpawnCompleted && !mSpawnInProgress) // todo: the Mission.cs which i think should belong to Simulation.cs should determine what AI vehicles exist, but for now to test spawning, we'll just hard code a single AI controlled vehicle near the moon
                {
                    // todo: do we need to perform this search every frame? or at some fixed frequency. The users vehicle is moving pretty fast so...
                    Region currentRegion = AppMain.mPlayerControlledEntity.Region;

                    //Predicate<Keystone.Elements.Node> match = (n) =>
                    //{
                    //    if (n is SpawnPoint) return true;
                    //// todo: test distance between mLocalVehicle and SpawnPoint based on mLocalVehicle's sensor range

                    //return false;
                    //};

                    //Node[] results = currentRegion.Query(true, match);


                    //for (int i = 0; i < results.Length; i++)
                    //{
                    //    // todo: mCurrentMission should place spawn points if applicable
                    //    // todo: shouldn't SpawnPoint just be an Entity with a spawn script? If so, how do we know its a spawnpoint?
                    //    SpawnPoint sp = (SpawnPoint)results[i];
                    //    // sp.
                    //    // send spawn message to client
                    //    AppMain.Form.ServerSendMessage(AppMain._core.Settings.settingRead("network", "username"), msg);
                    //}


                    // todo: i dont think i need a specific relativePath since i know this is intended to be spawned from a temp file
                    // that the client creates when downloading the stream.  So this is actually more like Transfer_File_And_Spawn() and
                    // we just need to know the parentID to attach it to.
                    // todo: do i need to know if this file belongs in the SCENES\\ SAVES\\ or MODS_PATH\\
                    // todo: its important that the mRelativePath along with other non file data, fits in at or below MESSAGE_MAX_HEADER_SIZE
                    // todo: this may need to be a temp file since we dont want to overwrite the prefab, we just want the server generated node.IDs
                    // load the entity, create Node.IDs and save to a temporary file and use that to get the data
                    bool generateIDs = true;
                    bool recurse = true;
                    bool delayResourceLoading = true; // we are just generating IDs and dont need to load any resources
                    string prefabRelativePath = "caesar\\entities\\vehicles\\test_ai.kgbentity";
                    string fullPath = System.IO.Path.Combine(AppMain.MOD_PATH, prefabRelativePath);
                    string[] nodeIDsToUse = null;
                    // NOTE: we load the vehicle in UpdateLoopback() because we want the server to establish the entire hierarchyy of node IDs INCLUDING CREW MEMBERS
                    //       and that the client will then import without cloning.  Creatting a full prefab file is easier than trying to send a bunch of NodeStates.
                    //       For this to work, we must send the user the relative path of the prefab we're sending.
                    //
                    // NOTE: when the client is told to Spawn a prefab comprised of a large Entity hierarchy such as a Vehcile or Container complete with crew, furniture and components,
                    //       that client willl know the relative path and already have the appropriate \\mod installed.
                    //       So we don't have to transmit the .interior database.  The client should know how to handle  copying and assigning a Container's Interior.
                    bool arcadeEnabled = AppMain._core.ArcadeEnabled;
                    AppMain._core.ArcadeEnabled = false;
                    mSpawnedVehicle = (Keystone.Vehicles.Vehicle)Keystone.ImportLib.Load(fullPath, generateIDs, recurse, delayResourceLoading, nodeIDsToUse);
                    mSpawnedVehicle.SRC = null;
                    AppMain._core.ArcadeEnabled = arcadeEnabled; // TODO:l I think this ArcadeEnabled hack was supposed to be so the LoadEntity would know how to create the fullpath (eg either \\saves\\ or \\modspath\\ 
                    mSpawnedVehicle.SetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.PlayerControlled, false);
                    
                    mSpawnedVehicle.SetEntityAttributesValue((int)EntityAttributes.MissionObject, true);

                    string newDataPath = AppMain._core.Scene_Name + "\\"  + mSpawnedVehicle.ID + ".interior";
                    mSpawnedVehicle.Interior.SetProperty("datapath", typeof(string), newDataPath);

                    // todo: none of the above settings occur because we load from the fullpath instead of a temp path that contains the node.IDs we want the client to use
                    //       Same goes for the translation

                    // translation - todo: this should be handled by the mission.css script
                    Predicate<Node> match = (n) => { if (n is Keystone.Celestial.World && n.Name == "Earth") return true; return false; };

                    Node moon = this.Scene.Root.FindDescendant(match);
                    Keystone.Celestial.World w = (Keystone.Celestial.World)moon;

                    if (w != null)
                    {
                        double worldRadius = w.Radius;
                        double orbitalRadius = worldRadius * 2;
                        Vector3d vector = Vector3d.Normalize(w.RegionMatrix.GetTranslation());
                        Vector3d translation = w.RegionMatrix.GetTranslation() + (vector * -orbitalRadius);
                        mSpawnedVehicle.Translation = AppMain.mPlayerControlledEntity.Translation; //translation;
                        mSpawnedVehicle.Translation = new Vector3d(mSpawnedVehicle.Translation.x + 550, mSpawnedVehicle.Translation.y, mSpawnedVehicle.Translation.z);              
                        // https://stackoverflow.com/questions/14845273/initial-velocity-vector-for-circular-orbit
                        // todo: initial orbital velocity
                    }

                    string tempFile = Path.GetTempFileName();
                    Keystone.IO.XMLDatabaseCompressed entityDB = new Keystone.IO.XMLDatabaseCompressed();
                    entityDB.Create(SceneType.Prefab, tempFile, mSpawnedVehicle.TypeName);

                    // todo: should i just force inline all Nodes?  we dont need to save incrementally, only when the user clicks "save" or quits the simulation. The current state is in memory.
                    entityDB.WriteSychronous(mSpawnedVehicle, true, false, false); // we dont incrementdecrement because unlike generating a scene or universe, the target node already exists in our scene and we want to keep it
                    entityDB.SaveAllChanges();
                    entityDB.Dispose();

                    // todo: Write(mSpanwedVehicle, tempFile);
                    ZoneRoot zr = (ZoneRoot)this.Scene.Root;
                    string earthZoneID = zr.GetZoneName(0, 0, 0);

                    //// NOTE: when the client is told to Spawn a prefab comprised of a large Entity hierarchy such as a Vehcile or Container complete with crew, furniture and components,
                    //       that client willl know the relative path and already have the appropriate \\mod installed.
                    //       So we don't have to transmit the .interior database.  The client should know how to handle  copying and assigning a Container's Interior.
                    KeyCommon.Messages.Transfer_Entity_File begin = new KeyCommon.Messages.Transfer_Entity_File();
                   
                    begin.mParentID = earthZoneID;// this.Scene.Root.Children[3].ID;
                    begin.mRootNodeTypename = mSpawnedVehicle.TypeName;
                    begin.mRootNodeID = mSpawnedVehicle.ID;
                    begin.mPrefabRelativePath = prefabRelativePath; // this allows us to copy the .interior file if mRootNodeTypename == Vehicle or Container
                    begin.mNewRelativeDataPath = newDataPath;
                    begin.FileLength = mFileStream.Length;

                    // todo: verify client and server are using maxuserpayloadsize from Settings and for send and recv buffers. Perhaps even the server should tell the client what size buffers to use
                    long remaining = mFileStream.Length;
                    int sendBytes = (remaining > AppMain._core.MaxFragmentSize ? AppMain._core.MaxFragmentSize : (int)remaining);
                    
                    begin.Data = new byte[sendBytes];

                    mFileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    int result = mFileStream.Read(begin.Data, 0, begin.DataSize); 
                    
                    mTotalBytesSent = sendBytes;

                    if (remaining - sendBytes > 0)
                    {
                        begin.IsFragmented = true;
                        begin.Guid = mTransferGuid = System.Guid.NewGuid().ToString();
                        begin.FragmentIndex = mFragmentIndex = 0;
                        mTransferInProgress = true;
                        mSpawnInProgress = true;
                    }
                    else
                    {
                        mTransferInProgress = false;
                        mSpawnInProgress = false;
                    }

                    // todo: if the file fits entirely in a single Transfer_File command, inc/dec the spawnedEntity
                    AppMain.Form.ServerSendMessage(AppMain._core.Settings.settingRead("network", "username"), begin);

                }
                else if (mTransferInProgress)
                {
                    KeyCommon.Messages.Transfer_Entity_File transfer = new KeyCommon.Messages.Transfer_Entity_File();
                    transfer.Guid = mTransferGuid; // we use the same Guid for each fragment
                    transfer.IsFragmented = true;
                    mFragmentIndex++;
                    transfer.FragmentIndex = mFragmentIndex;

                    long remaining = mFileStream.Length - mTotalBytesSent;
                    int sendBytes = (remaining > AppMain._core.MaxFragmentSize ? AppMain._core.MaxFragmentSize : (int)remaining);

                    transfer.Data = new byte[sendBytes];
                    
                    int result = mFileStream.Read(transfer.Data, 0, transfer.DataSize);
                    
                    mTotalBytesSent += sendBytes;
                    if (sendBytes > 0)
                        AppMain.Form.ServerSendMessage(AppMain._core.Settings.settingRead("network", "username"), transfer);

                    remaining = mFileStream.Length - mTotalBytesSent;
                    if (remaining == 0)
                    {
                        mFileStream.Close();
                        mFileStream.Dispose();
                        mFileStream = null;
                        mTransferInProgress = false;
                        mSpawnInProgress = false;
                        Repository.IncrementRef(mSpawnedVehicle);
                        Repository.DecrementRef(mSpawnedVehicle);
                        mSpawnCompleted = true;
                        mTransferGuid = null;
                        mTotalBytesSent = 0;
                        mFragmentIndex = 0;
                    }
                }

                // clean up spawned objects that are destoryed or are no longer in range
                // find all vehicle's that are AIControlled
                // verify all AIControlled vehicles are Serializable == false?
                // todo: do we save bonedEntities (aka crew) into the Scene?
            }
            return 0;
        }
        
        int mCurrentNPC = 0;
        // https://docs.unity3d.com/Manual/ExecutionOrder.html
        // TODO: MPJ June.20.2011 - Each Region could have it's own Simulation!  
        // http://www.gamedev.net/topic/525261-questions-on-zoning/  <-- Eve Online partitioning
        // this will also make it easy to have different physics models for different region types
        // eg. indoors on ships vs exterior of ship vs on land interior or exterior
        // For now however we wont worry about it... wait til we just get more gameplay working
        public double Update(Keystone.Simulation.GameTime gameTime)
        {

            if (AppMain.mLoopbackServer != null)
                UpdateLoopback(gameTime);


            // TODO: should entity.Update() occur in fixed step too like physics? some parts should definetly i think such as Consumption and Production
        	mGameTime  = gameTime;
            // TODO: errors here with null entities is usually because we've added
            // the entity outside of the commandprocessor (eg. hud added entities with no
            // use of proper sychronization methods.)  

#if USE_PROCESSORS


        // BENCHMARK - Simulation.IntrinsicDataProcessors can test 
        //             1000 simple ships that use Steer.Wander() and Collision testing and 1) test
        //             runs for 10 minutes using DataProcessors and ComponentStore<T>
        //             and 2) another test runs using OOP Entity.Update() 
        //             and 3) last test runs using OOP but with seperate Entity.Update() for
        //             Steer.Wander and Collision detection
        // NOTE: parallelism should occur within the processing of the Memory<T> structs with the
        //       assigned Data/Rule Processor.  
          Repository.IntrinsicProcessors.Update(mScene, mScene.ActiveEntities, gameTime);
          
          // TODO: create intrinsic structs to store
          // enum IntrinsicDataTypes
          // {
          //     Transform = 1;
          //     Bounds = 2;
          //     Physics = 3;
          // }

            // each struct should contain a field for holding the index to a UserData object in a UserDataStore
            // todo: currently the structs are stored in a dictionary using a string key not an int index... 
            //       for near term, lets just store a fixed byte[] field in our Memory<T> representing the 
            //       GUID id of every entity.
          
          Repository.IntrinsicProcessors.RulesProcessors.Update(mScene, mScene.ActiveEntities, gameTime);
          
          // Updates to Game Logic including newly added high performance updates to MemoryStores. August.15.2025
          // todo: August.21.2025 - I don't think we need a mGame.Update() because Game01.dll for example is
          // just a library and mostly only contains rules and gameObjects.
          // But we perform the actual processing here in the EXE
          mGame.Update(mGameTime.ElapsedSeconds); // <-- NOTE: THis is mGame NOT mGameTIME


          mScene.FinalizeEntityMovement(gameTime.Ticks);
#else 
            Entity[] activeEntities = mScene.ActiveEntities;
            if (activeEntities != null)
            {
                // simple round robin processing of all active entities (excluding viewpoints)
                // TODO: to use this const, we must still cache the elapsed time for entities that are skipped or they will slow down!!!
                //       For movement and animations this is unacceptable, but perhaps for other tasks, it's ok?
                const int NUM_NPC_PER_FRAME = 10;

                // NOTE: AI is processed seperately from entity.Update() so that we can implement simple schedule of 10 agents per frame.
                // see last entry on this thread -> https://gamedev.stackexchange.com/questions/32813/how-does-dwarf-fortress-keep-track-of-so-many-entities-without-losing-performanc
                // TODO: what we could do is flag when ActiveEntities changes from frame to frame so we can make sure we're not missing currentNPC when iterating our processing.
                // TODO: in order to get away with udating a small number of NPCs per frame, we MUST ACCUMULATE the elapsedTime for those that don't get updated or else they start moving slower since when they are updated, they've missed elapsedSeconds for multiple frames
                //       So this means i need a corresponding array for mCurrentNPC that mapes to the activeEntities that were skipped.  But what if hte order of activeEntities changes between frames?
                for (int i = 0; i < activeEntities.Length; i++) // NUM_NPC_PER_FRAME; i++)
                {
                    if (mCurrentNPC >= activeEntities.Length)
                    {
                        mCurrentNPC = 0;
                        break;
                    }
                    if (activeEntities[i] is Viewpoint) // mCurrentNPC] is Viewpoint)
                    {
                        //mCurrentNPC++;
                        continue;
                    }

                }

                // todo: spawnpoints should exist but they should be initiated by loopback i think
                
                // NOTE: the following for() loop we only look to update "helm" but this seems strange.  
                //       The only reason to do this is to ensure helm updates prior to other
                //       Entities, however a better way to solve this would be to use an "Update Priority" value.
                for (int i = 0; i < activeEntities.Length; i++)
                    if (activeEntities[i].Name == "helm")
                        activeEntities[i].Update(gameTime.ElapsedSeconds);
                        
                // UPDATE - Simulated logic\scripts of entities via calling all Entity.Update()
                for (int i = 0; i < activeEntities.Length; i++)
                {
                    Entity entity = activeEntities[i];

                    // TODO: i think we should just entity.Enable = false; when disabled and not remove it from it's parent because
                    //       Mission's and MissionObjective's will try to read the EntityAttributes.Destroyed and potentially wont be able to
                    //       because it will have already been removed.
                    // TODO: So maybe Mission's should be responsible for handling removal from Scene??? Or maybe perhaps a long enough time out
                    //       where the Simulation can garbagecollect() them after say SOME_FIXED_GC_TIME 

                    // scripts can set the "destroyed" flag so that the Simulation knows when it's ok to remove those entitites (actually i think now we're just going to enable=false them)
                    // TODO: if this is the player's ship, then the viewpoint targeted to it will fail during the update at the end of this Update() 
                    // method... specifically when activeEntities[i].UpdateAI() is called.
                    //       DO WE NEED TO DO ANYTHING WITH THE viewpoint that has focusEntityID set to this destroyed Entity?
                    if (entity.GetEntityAttributesValue((uint)EntityAttributes.Destroyed))
                    {
                        if (entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.PlayerControlled))
                        {
                            // TODO: ideally i should change the viewpoint behavior to be free cam?
                            ((Workspaces.WorkspaceBase3D)AppMain.Form.WorkspaceManager.CurrentWorkspace).ViewportControls[0].Viewport.Context.Viewpoint.BlackboardData.SetString("focus_entity_id", null);
                            System.Diagnostics.Debug.Assert(AppMain.mPlayerControlledEntity == entity);
                            AppMain.mPlayerControlledEntity = null;
                        }

                        // NOTE: we do not remove the destroyed Entity immediately.  We just disable it since
                        // other contexts such as Missions may need to access the Entity before it is removed.
                        entity.Enable = false;

                        //entity.Parent.RemoveChild(entity);
                        //System.Diagnostics.Debug.Assert(entity.RefCount == 0, "Simulation.Update() - Destroyed Entity refcount == " + entity.RefCount.ToString());
                        continue;
                    }

                    if (entity.Enable)
                    {
                        if (entity is Viewpoint) continue; // May.16.2017 - viewpoints should always be updated via ViewpointController right? TODO: wait, April.4.2019 -> i think vew updgraded Viewpoints to use BehaviorTrees.  TODO: if game is paused, the viewpoint needs to pause too and not resume with some massive amount of movement accrued during the pause
                        
                        // todo: why did i add this? was it temporary to avoid helm script from running? 
                        // Actually, i specifically check for "helm" higher up in THIS very Update() method.  Why?
                        // Is it to ensure helm always updates before everything else?
                        
                        if (entity.Name == "helm") continue; 
                        // Entity.Update() 
                        //  - updates AnimationControllers, HOWEVER actor.Render(true) occurs during scene.Update()
                        //  - Entity.Script.OnUpdate() for updating game logic 
                        //    like NON FORCE production & consumption <-- this should be fixed step i think!
                        //  - if Steering is updated here, then it cannot benefit from
                        //    fixed step physics!

                        // NOTE: entity.Update() will set the PreviousTranslation to the current Translation prior to 
                        // calling any entity.scripts that might modify the current Translation.
                        entity.Update(gameTime.ElapsedSeconds);
                    }
                }
            }
            
            
            // UPDATE - NON force production. We may decide to lower the Hz here.
            // TODO: confused, why isn't production and consumption operating at a fixed frequency?  You dont get same results
            // across clients if you don't do this
            UpdateProduction(gameTime.ElapsedSeconds);
            
            
            // updates force production & consumption, physics if fixedStep
			// adds elapsed to remainder seconds from prev UpdatePhysics()
            double fixedTimeStepRatio 
                = UpdatePhysics(gameTime.ElapsedSeconds);
 

			UpdateCollisions();
			
            // FinalizeEntityMovement() to apply position changes to nodes (done AFTER physics response)
            // TODO: but what if a node has reached bounds?  ideally CollisionDetection manager would be the
            // one enforcing the bounds?  hrm...  the main bounds are world bounds, not zone
            // or interior region bounds since crossing those bounds simply transfers to the
            // adjacent region... right?  any special cases where this is not so?
            // TODO: verify that FinalizeEntityMovement() applies the changes to the node and that
            //       collision response and physics response and boundary checking/limiting are all done before we get here.
            mScene.FinalizeEntityMovement(gameTime.Ticks);

            // TODO: Area of Interest management - where? how? no idea why i put this comment here...
            //       do need to think about AoI for 2.0, but i think 1.0 no multiplayer means little need
            //       for AoI?
            // for all entities that are registered for area of interest management,
            // update them round robin style
            // NOTE: The stale interest management results are retained across frames
            // for use in network group cast of data to interested parties, for refined
            // searches as a starting point upon which to run filters.
            // So typically we'd store interests by Vehicle...  we could potentially take some
            // short cuts with fleets that are flying close together and can share interest info

            // NOTE: AI Update occurs after force production
            if (activeEntities != null)
                for (int i = 0; i < activeEntities.Length; i++)
                {
                    Entity entity = activeEntities[i]; 
                    if (entity is Viewpoint) continue;
                    if (entity.Behavior != null)
                    {
                        entity.UpdateAI(gameTime.ElapsedSeconds);
                    }
                }

            // IMPORTANT: Filter out the Viewpoints only, then process Viewpoint AI every frame AFTER we've updated all other non-Viewpoint Entities. 
            // This way our Chase cam will be using the target entity's latest position
            if (activeEntities != null)
                for (int i = 0; i < activeEntities.Length; i++)
                {
                    // todo: need to test that viewpoint follows properly when it's focus entity has traversed a zone boundary and viewpoint has yet to do that.
                    if (activeEntities[i] is Viewpoint && activeEntities[i].Enable)
                    {
                        activeEntities[i].UpdateAI(gameTime.ElapsedSeconds);
                    }
                }
#endif // USE_PROCESSORS


            mEntityQuery.Clear();
            
            // we update the basic area of interest for max range entities of all once every second
            // (for starters)

            return fixedTimeStepRatio;
        }


        private void UpdateForceProduction(double elapsedSeconds)
        {
            // TODO: what do we do if the Entity that has registerd force production is destroyed?  Do
            //       we rely on that Entity's script to deliver 0 force?  Or do we test that Entity's state here
            //       and if Entity.Suspended or even Entity.Enabled == false or Entity.Asleep or something skip it?

            // divide the 1Hz producers up by type and update all elements of a particular type at once
            // then all the next, etc... 
            // so if i have 30 categories and 30 fps, i can do 1 category each frame.
            // This will make an ok 1.0 round robin implementation.
            // I might even be able to assign them the specific timeslice % or range across
            // 1 full second at which to update on... and potentially spreading each
            // category out over time within that slice range.

            Entity[] producers = GetProducers(0); // 0 == thrust, 1 angular_thrust, 2==gravity defined in the script UserConstants.css
            UpdateForceProduction(producers, 0, elapsedSeconds);
            producers = GetProducers(1);
            UpdateForceProduction(producers, 1, elapsedSeconds);

#if GRAVITATION
            // todo: gravitation. Celestial body scripts needs to update Force production to use userProduction 
            producers = GetProducers(2);
            UpdateForceProduction(producers, elapsedSeconds);
#endif
        }

        private void UpdateForceProduction(Entity[] producers, uint productID, double elapsedSeconds)
        {           
            if (producers == null) return;
            
            for (int i = 0; i < producers.Length; i++)
            {
            	Entity producer = producers[i];
                if (producer.Script == null) continue; // should never happen! All production is defined in DomainObject scripts. KeyCommon.Simulation.Production[] production = 

                // 1) Produce Forces
                KeyCommon.Simulation.Production[] production =
                    producer.Script.UserProduction[productID](producer.ID, elapsedSeconds);

                if (production == null) continue;

                for (int j = 0; j < production.Length; j++)
                {
                    Entity sourceEntity = mScene.GetEntity(production[j].SourceEntityID);
                    if (sourceEntity == null) continue;
                    // 2) Determine consumer distrubtion - find all valid consumers that match the terms of this production result
                    List<Entity> consumers = FindConsumers(mScene, sourceEntity, production[j]);
                    if (consumers == null) continue;

                    // 3) Consume - pass the production to the consumption handler
                    for (int k = 0; k < consumers.Count; k++)
                    {
                        // if this is a force consumer (eg thrust or gravity) then we should cache
                        // the existing values as PreviousStep____ 
                        
                        // if this is a force consumer (eg thrust or gravity), we will 
                        // the resulting translations/rotations/scales will have been done 
                        // and be stored in mTranslation/mScale/mRotation

                        // for culling and visiblity we want to use the mPreviousStep____
                        // but for physics and collisions and spatial logic like pathing
                        // we want to use the current.
                        KeyCommon.Simulation.Consumption_Delegate handler = consumers[k].Script.Consumers[production[j].ProductID];
                        KeyCommon.Simulation.Consumption[] consumptionResult = handler(consumers[k].ID, production[j], elapsedSeconds);
                        if (consumptionResult == null) continue;


                        for (int l = 0; l < consumptionResult.Length; l++)
                        {
                            // NOTE: the main diffference between force production and user production is the call to Node_ChangeProperty for intrinsic vars versu  ChangeCustomCustomProperty for custom Entity properties
                            // change property using the "operation" defined in the consumptionResult.  INCREMENT(accumulate) for instance will add acceleration force or angular force (torque)
                            if (consumptionResult[l].Properties == null) continue;
                            KeyCommon.Messages.Node_ChangeProperty changeProperty = new KeyCommon.Messages.Node_ChangeProperty(consumptionResult[l].TargetID);
                            changeProperty.Add(consumptionResult[l].Properties[0]);

                            for (int c = 0; c < consumptionResult[l].Properties.Length; c++)
                            {
                                changeProperty.Add(consumptionResult[l].Properties[c], consumptionResult[l].Operations[c]);
                            }
                            // todo: for client exe, we do need changes in acceleration and velocity. eventually we need to have simple integration on both client and server.
                            //       client should simply use existing velocity and acceleration to integrate, whilst server uses most up to date values.
                            //       Server should also periodically send translation and client should work to get to this exact server defined position
                            AppMain.mNetClient.SendMessage(changeProperty);
                        }
                    }
                }
            }
        }


       	private void UpdateProduction(double elapsedSeconds)
        {
            uint productID = 8; // 3 == power in UserConstants, 8 == microwaves
            Entity[] producers = GetProducers(productID);
            if (producers == null) return;
            // todo: i need to handle each bucket.  currently we are only updating production for "power"

            // TODO: maybe we only want to produce at a certain frequency, with exception of COLLISION filters?
            //       and maybe we also want to thread and
            //       maybe we also want to do only x many per interval
            //       or put another way, load balance scheduling 
            
            // %windir%\microsoft.net\framework\v3.5\msbuild /m /p:Configuration=Release /p:DefineConstants="NET35" /nologo /t:Build 
            
#if NET40
            
            //System.Threading.Tasks.Parallel.For ();
            // http://stackoverflow.com/questions/10846550/disappointing-performance-with-parallel-for
#endif
            
            for (int i = 0; i < producers.Length; i++)
            {
            	Entity producer = producers[i];
                if (producer.Script == null) continue; // should never happen!
                // TODO: the act of "consumption" should add to the contacts and be sent over the wire.
                // So there is a distinction between what production/consumption is predicted client side
                // and what is done server side with results transmitted to clients.
                // TODO: how do we ensure that some production is only done server side
                //       if not running in loopback?  The "search" feature especially
                //       here which could use a lamda, 
                // 1) Produce User Products
                // todo: outside this loop, we need to iterate through other types of production and at potentially different Hz
                KeyCommon.Simulation.Production[] production = 
                   producer.Script.UserProduction[productID](producer.ID, elapsedSeconds);

                // if production is null, consumers attached to this cannot be updated
                // and so on their next "tick" they will be starved for the product 
                if (production == null) continue;

                for (int j = 0; j < production.Length; j++)
                {
                    Entity sourceEntity = mScene.GetEntity(production[j].SourceEntityID);
                    if (sourceEntity == null) continue;
                    // 2) Determine consumer distrubtion - find all valid consumers that match the terms of this production result
                    // TODO: FindConsumers is very slow.  I must not run that simulation each period for Zones that are beyond a certain range from player.
                    // TODO:  Verify that we are in fact running Production for entities in every zone we load.  That is a bug.
                    List<Entity> consumers = FindConsumers(mScene, sourceEntity, production[j]);
                    if (consumers == null) continue;

                    // System.Diagnostics.Debug.WriteLine("Simulation.UpdateProduction() - " + consumers.Count + " found.");
                    // 3) Consume - pass the production to the consumption handler
                    for (int k = 0; k < consumers.Count; k++)
                    {
                        // invoke appropriate delegate
                        KeyCommon.Simulation.Consumption[] consumptionResult = consumers[k].Script.Consumers[production[j].ProductID](consumers[k].ID, production[j], elapsedSeconds);
                        if (consumptionResult == null) continue;

                        // send a command to change relevant properties based on the consumptionResult.
                        // todo: question is, how would we do someth`ing like an "Explosion" fx when health == 0 after changes to the property?
                        // well perhaps our script performs a Rule check when the value is changed and initiates the explosion.
                        // todo: well i think we can still use SensorContacts, HelmState and NavPoints, but they need to be able to serialize over the wire to go through loopback.
                        // todo: i think we do want to be able to store for saves purposes some custom gameobject types.
                        // NOTE: we pass in "operations" to perform so that we have ability to things like increment/decrement/add/remove/etc.    
                        for (int b = 0; b < consumptionResult.Length; b++)
                        {
                            if (consumptionResult[b].Properties == null) continue;

                            KeyCommon.Messages.Entity_ChangeCustomPropertyValue changeProperties = new KeyCommon.Messages.Entity_ChangeCustomPropertyValue();
                            changeProperties.EntityID = (consumptionResult[b].TargetID);
                            for (int c = 0; c < consumptionResult[b].Properties.Length; c++)
                            {
                                changeProperties.Add(consumptionResult[b].Properties[c], consumptionResult[b].Operations[c]);
                            }
                            AppMain.mNetClient.SendMessage(changeProperties);
                        }
                    }
                }
            }
        }

        // http://answers.unity3d.com/questions/10993/whats-the-difference-between-update-and-fixedupdat.html
        private double UpdatePhysics(double elapsedSeconds)
        {

            //if (_singleStep == true && keyState.IsKeyDown(Keys.Space) == false) 
            //{
            //    // don't intergrate so we can step at will
            //}
            //else
            //{
            _elapsedSecondsPhysics += elapsedSeconds; // add elapsed to remaining _elapsedPhysics from previous frame

            // http://gafferongames.com/game-physics/fix-your-timestep/
            // http://blog.allanbishop.com/box-2d-2-1a-tutorial-part-10-fixed-time-step/
            if (FIXED_FREQUENCY_PHYSICS)
            {
                int steps = (int)Math.Floor(_elapsedSecondsPhysics / _fixedTimeStep);
                if (steps > 0)
                {
                    _elapsedSecondsPhysics -= steps * _fixedTimeStep;
                }

                // TODO: the fixedTimeStepAccumulatorRatio is to be used with rendering
                //       to get the best interpolated physical position for rendering
                double fixedTimestepAccumulatorRatio = _elapsedSecondsPhysics / _fixedTimeStep;

                // clamping will cause potentially unsycrhonization but it's a safeguard
                // to prevent the physics from spiraling behind and bogging down the entire game.
                int stepsClamped = Math.Min(steps, MAX_STEPS);
                //System.Diagnostics.Debug.WriteLine("Steps clamed == " + stepsClamped.ToString());
                for (int i = 0; i < stepsClamped; ++i)
                {
                    // TODO: also what about networking interpolation?  is that automatically done here
                    //       so long as our script is running?

                    //ResetSmoothStates(); // NOTE: in the Box2d implementation
                    //                     // he sets body.SmoothPosition, body.PreviousPosition all equal to
                    // the body.Position since Update/Integrate will change body.Position
                    // but potentially the callbacks during each Step will look to read body.SmoothPosition
                    // which will be stale.  But what i wonder is, what condition would require
                    // a callback to use the .SmoothPosition and not just the
                    // body.Position?  And if .SmoothPosition is always the same...
                    // well the only time it is not the same is after the final step
                    // and maybe that is why... but it seems to me then
                    // that callback should use body.PreviousPosition.  I do
                    // see why a callback might want to know the previousPosition
                    // though.  
                    // 
                    UpdateForceProduction(_fixedTimeStep);

                    // apply forces that were accumulated during production & consumption of physical forces
                    DoPhysics(_fixedTimeStep);

                    // process collisions after application of forces and computing new translation & orientation
#if TVPhysics
                    if (CoreClient._CoreClient.Physics != null)
                    {
                        //CoreClient._CoreClient.Physics.SimulateFixedStep((float)elapsedSeconds, 1f / (float)_physicsHertzInTimesPerSecond);
                        CoreClient._CoreClient.Physics.Simulate((float)elapsedSeconds);

                        int numEvents = CoreClient._CoreClient.Physics.PollEvents();
                        for (int i = 0; i < numEvents; i++)
                        {
                            MTV3D65.TV_EVENT_PHYSICSCOLLISION collision = CoreClient._CoreClient.Physics.GetEventCollisionDesc(i);
                            System.Diagnostics.Debug.WriteLine("Collision detected.");
                        }
                    }
#else
                    #region Jitter Physics
                    if (mPhysicsWorld != null)
                        mPhysicsWorld.Step((float)_fixedTimeStep, true); // (1f / (float)_physicsHertzInTimesPerSecond, true);
                    //mPhysicsWorld.CollisionSystem.PassedBroadphase       
                    #endregion

#endif

                    //_physicsSystem.Integrate(_fixedTimeStep);

                }

                SmoothSteps(fixedTimestepAccumulatorRatio);

                return fixedTimestepAccumulatorRatio;
            }
            else
            {
                UpdateForceProduction(elapsedSeconds);
                DoPhysics(elapsedSeconds);
                SmoothSteps(elapsedSeconds);

                return 0;
            }
        }

        // simple linear velocity and angular velocity to find new Translation and Rotation
        private void DoPhysics(double elapsedSeconds)
        {
            Vector3d translation, acceleration, velocity;

            Entity[] activeEntities = mScene.ActiveEntities;
            if (activeEntities == null) return;

            for (int i = 0; i < activeEntities.Length; i++)
            {
                Entity entity = activeEntities[i];

                if (entity != null &&  // HACK: null is possible if item is paged out after being added to activeEntities and before it's removed
                    entity is Viewpoint == false &&
                    entity.Enable &&
                    entity.GetEntityAttributesValue((uint)EntityAttributes.Dynamic) &&
                    entity.GetEntityAttributesValue((uint)EntityAttributes.Awake))
                //&& mScene.mActiveEntities[i] is Proxy == false)
                {

                    if (!entity.Force.Equals(Vector3d.Zero()) || entity.Velocity != Vector3d.Zero())
                    {
                        //                    entity.Translation = entity.LatestStepTranslation;
                        translation = entity.LatestStepTranslation;
                        acceleration = entity.Acceleration;
                        velocity = entity.Velocity;


                        // LeapFrogVerlet for linear velocity
                        // - finally this will compute the new acceleration and translation based on accumulated forces (eg thrust and gravity) this step
                        // todo: acceleration can stop, but the velocity should remain constant after that and instead the Vehicle just completely stops

                        Keystone.Physics.Newtonian.LeapFrogVerlet(entity.Force, ref translation, ref acceleration, ref velocity, elapsedSeconds, 0f);
                        entity.Acceleration = acceleration;
                        entity.LatestStepTranslation = translation;
                        entity.Velocity = velocity;
                        entity.Force = Vector3d.Zero(); // <-- zero all forces afterwards is correct yes? or done in SmoothSteps?
                    }

                    // RK4
                    //                    KeyCommon.Simulation.NBodyPhysics.State state;
                    //                    state.Position = entity.Translation;
                    //                    state.Velocity = entity.Velocity;
                    //                    KeyCommon.Simulation.NBodyPhysics.Integrate (ref state, 0, (float) elapsedSeconds);
                    //                    entity.Translation = state.Position;
                    //                    entity.Velocity = state.Velocity;
                    //	                  entity.Force = Vector3d.Zero(); // <-- zero all forces afterwards is correct yes? 
                    if (entity.AngularForce == Vector3d.Zero() && entity.AngularVelocity == Vector3d.Zero()) return;

                    entity.AngularVelocity += entity.AngularForce * elapsedSeconds;
                    Quaternion rotation = Keystone.Physics.Newtonian.RotationAfterAngularVelocity(entity.AngularVelocity, entity.DerivedRotation, elapsedSeconds);
                    // Apply the rotation
                    AppMain.mScriptingHost.EntityAPI.SetRotationRegionSpace(entity.ID, rotation);

                    entity.AngularForce = Vector3d.Zero();

                }
            }
        }

        // "take the last state and the state before that as the two states (interpolation). 
        // Then we use the ratio on this. Of course, this means the renderer will always be 
        // 1 frame behind the physics but this should not be a problem. "
        private void SmoothSteps(double fixedTimestepAccumulatorRatio)
        {

            double oneMinusRatio = 1.0 - fixedTimestepAccumulatorRatio;

            Entity[] activeEntities = mScene.ActiveEntities;
            if (activeEntities == null) return;

            // NOTE: camera viewpoint is always updated per frame and not fixed freqency even when FIXED_FREQUENCY_PHYSICS = true. Not sure if i need to change that.
            for (int i = 0; i < activeEntities.Length; i++)
            {
                Entity entity = activeEntities[i];

                if (entity is Viewpoint == false &&
                    entity.Enable &&
                    entity.GetEntityAttributesValue((uint)EntityAttributes.Dynamic) &&
                    entity.GetEntityAttributesValue((uint)EntityAttributes.Awake))
                //&& mScene.mActiveEntities[i] is Proxy == false)
                {

                    if (entity.Force.Equals(Vector3d.Zero()) && entity.Acceleration.Equals(Vector3d.Zero()) && entity.Velocity.Equals(Vector3d.Zero())) continue;

                    if (FIXED_FREQUENCY_PHYSICS)
                    {
                        entity.Translation =
                            fixedTimestepAccumulatorRatio * entity.LatestStepTranslation +
                            oneMinusRatio * entity.Translation;
                    }
                    else
                        entity.Translation = entity.LatestStepTranslation;



                    //entity.Acceleration = Vector3d.Zero(); // acceleration;
                    entity.Force = Vector3d.Zero();

                }
            }
        }

        private Entity[] GetProducers(uint productID)
        {
            if (mProduction == null) return null;
            List<Entity> results;
            mProduction.TryGetValue(productID, out results);

            if (results == null) return null;

            return results.ToArray();
        }

        private List<Entity> FindConsumers(Keystone.Scene.Scene scene, Entity sourceEntity, KeyCommon.Simulation.Production production)
        {
            // TODO: re sensor contacts, see notes in Vehicle.cs
            // TODO: these "Distribution Type" is I think a weakness.  I think in the case of a
            //       emission that can be detected by opposing players sensors, distribution then
            //       requires specific logic that is specific for a particular sensor / emission pair.
            //       The "detection" logic is in a sense, the consumption of that sensor.  But that
            //       detection should be done server side and then the result returned to relevant clients.
            //       This way clients dont get to test if detection is made and have to be trusted to
            //       ignore information about source and direction of that detection if detection fails.
            //
            // TODO: could use a lambda instead of this 
            //       select case
            bool temp = false;
          
                // test using the scripted filter function, if this entity meets the criteria
                // to receive this production to consume
                //bool pass = production.DistributionFilter(production, target.ID);
                
                // TODO: i think since the distribution filter is a scripted function and because
                // scripts cannot instance entities and must go through the API, then
                // that distribution filter in turn will need to make EntityAPI calls

                // So i want to do a filter for sensor contact where we have say
                // a omnidirectional radar picking up a "radar emission" product,
                // with configurable range via custom property
                // that has a bonus depending on the skill of the sensor operator
                // (the notion being that running a sensor requires being able to screen out false positives
                // the fact that this equipment is not perfect, the fact that tuning sensors is 
                // something that must be done continuously, 
                // So the range of a sensor is not the whole story.  
                //    - the other issue is thta previous sensor contact gives a bonus to 
                //      getting picked up on subsequent radar ticks.  
                //      - how does this look in script?  You have to query
                //      the parent vehicle entity's "contact" property and i think a "contact"
                //      being a Simulation.Contact (like Simulation.Product) that is a custom property in Vehicle
                //      we can grab the contact object directly.
                // struct Contact
                // {
                //      string ContactID;
                //      string[] Sensors; // the sensors that have detected this contact.  ships can have many sensors and of various types, but we only track one contact instance right?
                //      int Time;         // the time this Contact struct was created and thus we know the elapsed time this contact has been tracked.  The longer it's detected the better the odds of maintaining the contact detection on subsequent sensor ticks
                //      Vector3d Position;
                //      Orientation Heading; 
                //      Vector3d Acceleration;
                // }
                //
                // - radar emission production
                // - simulation runs updateproduction
                //   - UpdateProduction() runs a Query using the DistributionFilter function and DistributionType
                //     to determine the Consumers.
                // - Consumer list is sent to clients for production types that are performed server side only
                // - Consumer creates or updates a "Contact" object and adds it to the 
                //   Vehicle.CustomProperties["contacts"] property
                
            
// TODO: ConsumptionResults for distibutiontypes that are the same (and same location)
//       for multiple entities should be cached and saved to avoid re-computing?
            switch (production.DistributionMode)
            {
                case KeyCommon.Simulation.DistributionType.Self :
                    return new List<Entity> { sourceEntity};

                case KeyCommon.Simulation.DistributionType.Parent:
                    return new List<Entity> { sourceEntity.Parent};

                case KeyCommon.Simulation.DistributionType.Container :
                    //string vehicleID = sourceEntity.Container.ID; 
                    return new List<Entity> { sourceEntity.Container}; // scene.GetEntity(vehicleID) };

                case KeyCommon.Simulation.DistributionType.List: // "power links" populate this list and require DistributionType to be of type List 
                                                                 // but the problem at the moment is that these productions 
                    string[] entityIDs = production.DistributionList;
                    List<Entity> entities = new List<Entity> (entityIDs.Length);
                    for (int i = 0; i < entities.Count; i++)
                    	entities.Add (scene.GetEntity(entityIDs[i]));

                    return entities;

                case KeyCommon.Simulation.DistributionType.Region: 

                    if (sourceEntity.Region == null) return null;

                    Predicate<Entity> match;

                    // TODO: can we speed this up for spatial queries like collision where we can include a bbox, rect, or sphere? or even tilemap cell?
                    if (production.DistributionFilterFunc != null)
                    {
                        match = e=>
                        {
                            if (e.Script == null) return false;
                            if (e.Script.Consumers == null) return false;
                            if (e.Script.Consumers.ContainsKey(production.ProductID) == false) return false;
                            return production.DistributionFilterFunc(production, e.ID);
                        };
                    }
                    else 
                    {
                        match = e =>
                        {
                            if (e.Script == null) return false;
                            if (e.Script.Consumers == null) return false;
                            if (e.Script.Consumers.ContainsKey(production.ProductID) == false) return false;
                            return true;
                        };
                    }

                    return sourceEntity.Region.RegionNode.Query(true, match);
                    // TODO: above query fails on Navigation tab planet view
                    return null;

                case KeyCommon.Simulation.DistributionType.Primitive: 
                    return null;

                    // TODO: proximity based collisions as well?
                    
                case KeyCommon.Simulation.DistributionType.Collision: // seperate enum types for CollisionRay, CollisionSphere, CollisionBox, CollisionCone?
                    // and many of these types of production, we probably want to seperate from "product" production and put it more on par with "force" production
                    // since collision usually means explosion or something we're trying to do and those should be done at higher hz than what we might end up doing
                    // for goods production.
					// no consumers yet. we will skip until / if a collision response occurs for this production                     
                    return null;
                    
                default:
                    return null;
            }
        }

        private void UpdateCollisions()
        {
            // TODO: Scene.FinalizeEntityMovement() -> Scene.EntityMoved() occurs and manages sceneNode test
            //       for switching Regions... but it does not (yet?) do portal traversal testing.

            // TODO: when to do portal crossing tests that result in Entity switching zones?
            //       - TODO: does this not mean perhaps that a portal should have a geometry model
            //       in the scene and that the EntityNode that hosts the Portal should perhaps just be
            //       like any typical EntityNode and not a special "PortalNode" ??
            //  	  - it's probably why Portal.cs is derived from Entity at least... but the problem still is
            //   	  i think maybe it should have an actual geometry that exists in the scene and which can be debug drawn as well as
            //   	  collided with and which doesn't require any special SceneNode handlers such as Scene.EntityMoved() ->EntityNode.OnEntityMoved().  
            //        Indeed, I don't think EntityNode should be handling region changes at all because it doesn't have all of the information...
            //        Simulation & Entities & Region.cs should create events when an entity has crossed a "physical" boundary
            //        and only then should Scene and EntityNode or RegionNode should manage the modification/reassignment of SceneNodes and OctreeOctants and such.

            // TODO: when to do collision tests? Collisons need to be able to send events to 
            //       game about when collisions occur so the game specific logic can react
            //       ... where collision can result in emission of a product?
            //      - in TV's newton system, collision events are collected and then you can iterate
            //      through them

            // TODO: we can find the octree nodes that a particular entity has gone through maybe through a ray traverser
            //       of the scene graph.  We use that to create list of 

            // TODO: I think rather than worry about jiglibx, or jitter or 
            // bulletsharp wrapper http://www.youtube.com/watch?v=FypEARSFd44
            // or http://henge3d.codeplex.com/
            // we should simply do simply sphere 2 sphere and try to use our spatial graph to assist our broadphase
            // - we can use a ray through the octree to retreive which active nodes it has passed through between frames
            //   and use that to build potential list of bodies

            // 1) When an entity has moved and we start to update it's position, we add it to collision system for testing
            // 2) After all are inserted into the collision system, we run Update() on the collision system
            //		- collision system can be run in parallel by region or even by octrees  as far as finding
            //      and culling and pairing down potentially interacting / colliding objects quickly and efficiently.
            // 3) events are generated and we are able to retreive them and process them in parallel.
            // 4) how do these events work with our Production system?  Like if we want to have an explosive shockwave
            //    emitted, then detect what it collides with, then allow the hit devices to consume that damage.
            //    - In that sense, the collision response is like our distribution filter for finding consumers.
            //    - so in a way, some "producers" if their distribution filter = COLLISION_RESPONSE then
            //    we skip that production until it's triggered by response.

            //    // Physics psuedo
            //    //int eventCount = Engine.Core.Physics.PollEvents();
            //    //for (int i = 0; i < eventCount; i++)
            //    //{
            //    //    type = Physics.GetEventType(i);
            //    //    collision = Physics.GetEventCollisionDesc(i);
            //    //    // get the object associated with the collision and fire an event
            //    //    Entity myEntity;
            //    //    int index = collision.iBody1;
            //    //    myEntity = GetEntityFromIndex(index);// 1st entity involved
            //    //    myEntity.PhysicsResponse(collision); // handle response using details of the collision
            //    //    index = collision.iBody2;
            //    //    myEntity = GetEntityFromIndex(index);// 2nd entity involved
            //    //    myEntity.PhysicsResponse(collision); // handle response using details of the collision

            //    //     collision object members
            //    //      collision.fForce
            //    //      collision.fNormalSpeed 
            //    //      collision.fTangentSpeed 
            //    //      collision.iBody1 
            //    //      collision.iBody2 
            //    //      collision.iMaterial1 
            //    //      collision.iMaterial2 
            //    //      collision.vNormal   
            //    //      collision.vPosition 
            if (mCollisionSystem != null)
                mCollisionSystem.Detect(true);
        }

        private void OnCollisionDetected(Jitter.Dynamics.RigidBody body1, Jitter.Dynamics.RigidBody body2, Jitter.LinearMath.JVector point1, Jitter.LinearMath.JVector point2, Jitter.LinearMath.JVector normal, float depth)
        {
            Entity entity1 = (Entity)body1.Tag;
            Entity entity2 = (Entity)body2.Tag;

            // point1; 
            // point2;
            // body1.Position;
            // body2.Position;

            // internal method calls (todo: do we need to pass in the Jitter RigidBody Position and point1, point2 and depth? I think the positions are only needed if we're relying on Jitter physics to dynamically adjust the positions.
            //                              here we are only looking for trigger enter/exit events
            entity1.OnCollisionDetected(entity2);
            entity2.OnCollisionDetected(entity1);

            // System.Diagnostics.Debug.WriteLine("Simulation.OnCollisionDetected() - Depth = " + depth.ToString());
        }

        // //       http://www.grav-sim.com/simulation.html

        // the idea of registering production is we dont have to iterate through a bunch of
        // entities that do no produce anything.
        // the idea is also that for something like a wall or floor, that is 99.9% static
        // we dont have to Update() it for no reason either.  Instead we only have to 
        // call Entity.Produce() for those items which have production and return the array of
        // products and then iterate through them and distribute them according to the 
        // distribution channel specified be it a primitve shape where it's emitted to all
        // entities in that area, or a list of subscribers.
        // Rather than registeration which is clunky however, id like to be able to query
        // a flag on the entity instead to see if any production or consumption handlers exist
        // and for what types.
        //private void Transmit(double elapsedSeconds)
        //{
        //    if (mTransmitters == null || mTransmitters.Count == 0) return;

        //    // TODO: i think when a certain receiver leaves a region and enters a region
        //    // those events need to be forwarded to the Transmitters that are listening
        //    // for those events.  This is of course a way of dividing up areas of interest.
        //    // Transmitters can then get all receivers within a particular region easily...
        //    // and without having to constantly re-poll.  
        //    // But the question then is, what object is hosting these receivers (and transmitters)
        //    // I think most sensibly, it's Simulation itself but with dictionary by zone
        //    // for transmitters and receivers as opposed to one simple Transmitters by 
        //    // TODO: But via Zone.Children we know the first level children... and by Zone.SceneNode.Children
        //    // we can get all entities of that region via their SceneNode parents.
        //    // TODO: So in fact, Zone's / Region's should trigger our leave/enter events

        //    if (mTransmitters.ContainsKey(GRAVITY_TRANSMITTER))
        //        // TODO: although it's not necessarily relevant for gravity transmitters
        //        // for other types we should test if the entity to which the transmitter is connected
        //        // is enabled and how much damage and then invoke the script for that transmitter
        //        // to update... or something.  I mean, the main point is, this is not a generic
        //        // implementation where i call a TransmitGravity function that is hardcoded here
        //        // and where potentially i can have any kind of transmitters.
        //        TransmitGravity(elapsedSeconds);

        //}

        // NOTE: our star pulls on ship, but does not pull on planet.  The planet instead uses elliptical animation
        // So we will need to verify that the pull of the star on a vehicle will still sychronize well enough
        // with planet that is just animating around the star and not actually orbiting it.

        //    //    if (_collisionEnable)
        //    //    {
        //    //        oldpos.y = p.Center.y;
        //    //        // create a collision info structure to track the collision status
        //    //        TCollisionPacket collision =
        //    //            new TCollisionPacket(oldpos, velocity, p.Height / 2, p.Height / 2, p.Height / 2);

        //    //        newPos = collideWithWorld(collision.ScaledPosition, collision.ScaledVelocity, ref collision);
        //    //        newPos =
        //    //            new Vector3d(newPos.x * collision.eRadius.x, newPos.y * collision.eRadius.y,
        //    //                            newPos.z * collision.eRadius.z);
        //    //    }
        //    //    //newPos = _player.Translation; 
        //    //    //newPos = _player.Center;
        //    //    //newPos.y += 5;
        //    //    //newPos = Slide(oldpos, newPos); // this doesnt work so well because the position is at his feet
        //    //    //adjust player for collision with terrain
        //    //    terrainHeightAt = 35; // terrain.GetHeight(newPos.x, newPos.z);
        //    //    //Vector3d start = p.Center;
        //    //    //start.y -= p.Height/2 - 5f;
        //    //    //PickResults[] result =  _scene.Pick(start, Engine.Core.Maths.VNormalize( p.Center - p.Translation) );
        //    //    //if (result != null && result.Length > 0)
        //    //    //    terrainHeightAt = result[0].Result.vCollisionImpact.y;
        //    //    //else
        //    //    //    terrainHeightAt = oldpos.y;

        //    //    //terrainHeightAt += p.Height;
        //    //    //p.Translation =
        //    //    //    new Vector3d(newPos.x, terrainHeightAt, newPos.z);
        //    //p.Translation = newPos;



        //    // p.Translation = Collision(p.Translation);

        //    // finally update the player's or npc's animation
        //    // TODO: we could technically cull first before doing this...
        //    // and then instead of having to call p.Actor.Update(elapsed) we can do a proper
        //    // traversal.  Remove the "update" within BonedModel
        //    // and then do proper Traversal node handling.  Then for LOD we can proper select the child 

        //    // im not even certain that animation updates to actor should be done in either cull or traversal...
        //    // i do know i dont want it in "render" because with multiple passes there could be several and normally
        //    // you always want the update dont ONCE and on or before the first pass so the most up to date is used in all  passes
        //    // that frame.  Hrm... during "CUll" you're going to be updating things like BoundingVolumes on lazy
        //    // updating and at this same time you're going to select the child LOD's (or could)
        //    // NOTE: When building a "state" list you're simply pushing the nodes you visit onto the stack
        //    // and not popping them off afterwards.  Then in the Draw traversal we can actually traverse it
        //    // identically except instead of itterating children, we itterate the stack.  I suppose we could just have a
        //    // simple State object wrapper that contains the node as well as the node's relation to the previous node.
        //    // this way we know when to pop nodes off the stack (e.g appearances for instance that must be known )

        //// TODO: all of the following should be in a script.  And if our
        //// PhysicsBody is always in region specific coords then we need to convert
        //// them to entity relative instead (sword attached to player ragdoll hand)
        //if (entity.PhysicsBody != null)
        //{
        //    if (entity.PhysicsBody.IsActive)
        //    {
        //        float x, y, z;
        //        entity.PhysicsBody.GetPosition(out x, out y, out z);

        //        entity.Translation = new Vector3d(x, y, z);
        //        float m11, m12, m13, m21, m22, m23, m31, m32, m33;
        //        entity.PhysicsBody.GetOrientation(out m11, out m12, out m13,
        //                                                                                          out m21, out m22, out m23,
        //                                                                                           out m31, out m32, out m33);
        //        // TODO: further, RotationMatrix results in the Matrix being recomputed even if the rotation never changed
        //        // so we dont want to set this for nothing every frame just for the hell of it

        //        // TODO: this shouldnt have to be done, the Physics engine should be updating these values directly for the entity
        //        // or perhap, entity.RotationMatrix should simply be grabbing and setting entity.PhysicsBody.RotationMatrix()
        //        entity.RotationMatrix = new Matrix(m11, m12, m13, 0, m21, m22, m23, 0, m31, m32, m33, 0, 0, 0, 0, 1);
        //    }
        //}

        //}

        // http://stackoverflow.com/questions/19308282/smooth-collision-wall-sliding
        //        private Vector3 previewMove(Vector3 amount)
        //		{
        //		    // Create a rotate matrix
        //		    Matrix rotate = Matrix.CreateRotationY(CameraRotation.Y);
        //		    // Create a movement vector
        //		    Vector3 movement = new Vector3(amount.X, amount.Y, amount.Z);
        //		    movement = Vector3.Transform(movement, rotate);
        //		    // Return the value of camera position + movement vector
        //		
        //		    return CameraPosition + new Vector3(
        //		        Collision.CheckCollision(CameraPosition + new Vector3(movement.X, 0, 0)) ? 0 : movement.X,
        //		        Collision.CheckCollision(CameraPosition + new Vector3(0, movement.Y, 0)) ? 0 : movement.Y,
        //		        Collision.CheckCollision(CameraPosition + new Vector3(0, 0, movement.Z)) ? 0 : movement.Z);
        //		
        //		}
        //
        //http://stackoverflow.com/questions/2343789/c-sharp-xna-optimizing-collision-detection <-- sweep and prune post seems to advocate me just integrating jibligx or something.
        //

        // the below code seems to use the same type of fixed frequency integration we do above but seems to add a variable
        // for passing to the Vector3.Lerp function.  In any case primarily i want to ensure that my code is correct.
        // http://code.google.com/p/slimdx/source/browse/tags/Nov%2008/samples/Games/Asteroids/Particles/ParticleEmitter.cs?r=798
        //    public void Update(GameTime gameTime, Vector3 newPosition)
        //    {
        //        if (gameTime.ElapsedGameTime > 0.0f && !float.IsInfinity(timeBetweenParticles))
        //        {
        //            Vector3 velocity = (newPosition - previousPosition) / gameTime.ElapsedGameTime;

        //            float totalTime = timeLeftOver + gameTime.ElapsedGameTime;
        //            float currentTime = -timeLeftOver;

        //            while (totalTime > timeBetweenParticles)
        //            {
        //                currentTime += timeBetweenParticles;
        //                totalTime -= timeBetweenParticles;

        //                Vector3 position = Vector3.Lerp(previousPosition, newPosition, currentTime / gameTime.ElapsedGameTime);

        //                particleSystem.SpawnParticle(position, velocity);
        //            }

        //            timeLeftOver = totalTime;
        //        }

        //        previousPosition = newPosition;
        //    }
        //}


        public void LoadMission(string sceneName, string missionName)
        {
            bool hasMission = !string.IsNullOrEmpty(missionName);
            Keystone.Simulation.Missions.Mission mission = null;

            string missionsPath = System.IO.Path.Combine(sceneName, "missions");
            missionsPath = Path.Combine(missionsPath, missionName);
            missionsPath = Path.Combine(AppMain.SCENES_PATH, missionsPath);
            // verify this mission exists for this campaign and that the user is authorized to play it
            // eg. by virtue of having completed previous missions
            bool authorized = true; // TODO: check authorizatioon
            if (hasMission && authorized)
            {
                // load the mission and find this users spawnpoint
                mission = new Keystone.Simulation.Missions.Mission();
                mission.Load(missionsPath);

                // TODO: I THINK YES WE CAN LOAD THE MISSION DATA, but we can't begin to spawn Entities or
                //       assign and cache target's until the relevant Regions are fully paged in. So this probably
                //       means we need to wait for OnRegionChildrenPageComplete()  In fact, maybe we can proceed to 
                //       spawn and assign target's for just those regions that are paged in, and then delay execution
                //       or Tick() of the Mission if _all_ relevant Region's required for it are paged in.  So do we
                //       need to list the array of regions needed for that Mission?


                // get this player's spawnpoint's location regionID for the startingRegionID
                // todo: wait, consider if this is a 2 player PvP multiplayer mission, how do we know
                // from the mission data file, which player is at which spawnpoint? we can't use ur.UserName
                // because the mission data file is generic to accomodate any players.  However, for single player 
                // missions, there should only be one player spawnpoint and that spawnpoints regionID should be
                // the one we return. If there are more than one, then maybe we need to take into account
                // the player's faction (unless it co-op and every player is of the same faction)
                // TODO: What about if this is resuming from previous missions? The faction shouldn't change
                // and all players resuing together should spawn from the correct locations
                string factionName = null;
                //startingRegionID = mission.GetStartingRegion(factionName);

                // todo: i need to have persistance between missions

                mCurrentMission = mission;
                EnableMission(true);
            }
        }

        // If the client is using loopbck, this function is called either when toggling the "Play Mission" button
        // in the Editor ViewportControl or after having loaded a Mission even if it was the loopback server that told us
        // which mission to load.
        // If _NOT_ using loopback (eg. we're connected to a real multiplayer game server) then the client will never load a 
        // mission so we need to assign AppMain._core.ArcadeEnabled after having joined a game server that is running a Mission
        // as opposed to a server that is offering remote + group Scene editing capabilities.
        public void EnableMission(bool enable)
        {
            if (enable)
            {
                if (mCurrentMission != null)
                    mCurrentMission.Enable = true;
                else
                {
                    Keystone.Simulation.Missions.Mission mission = new Keystone.Simulation.Missions.Mission();
                    // TEMP HACK - a "null mission objective" to test arcade play mode with no real objectives, just free roaming play
                    Keystone.Simulation.Missions.Objective nullObjective = new Keystone.Simulation.Missions.Objective(Keystone.Simulation.Missions.Objective.ObjectiveType.None);
                    mission.Add(nullObjective);

                    // TEMP HACK - just add our test mission for player to destroy one ai ship
                    //Keystone.Simulation.Missions.Objective destroy = new Keystone.Simulation.Missions.Objective(Keystone.Simulation.Missions.Objective.ObjectiveType.Destroy);
                    //mission.Add(destroy);
                    
                    mCurrentMission = mission;
                    mCurrentMission.Enable = true;
                }
            }
            else
                if (mCurrentMission != null)
                    mCurrentMission.Enable = false;


            if (AppMain._core.SceneManager.Scenes != null && AppMain._core.SceneManager.Scenes.Length >= 1)
                AppMain._core.ArcadeEnabled = mCurrentMission != null && mCurrentMission.Enable;
                    
        }

        public void AddPlayer(Player player)
        {
            //mPlayers.Add(player.mID, player);

            //// send the player, the list of all other registered players
            //SendPlayerList(player);

            //// send the player the map spec
            //KeyCommon.MapSpec mapspec;
            //mapspec.mWidth = mGame.mWidth;
            //mapspec.mHeight = mGame.mHeight;
            //mapspec.Tiles = mGame.mTiles;

            //Lidgren.Network.NetConnectionBase connection  = (Lidgren.Network.NetConnectionBase)player.Tag;
            //// send the mapspec.  When the client receives this, they know they are now successfully connected to the server.
            //// NOTE: even when resuming a game, we can send a clientSpec that just has the width/height and no tiles
            //// since the client can refer to cached tiles.  If a client discovers for some reason it does not have a cached tile
            //// that the server thinks it should, the client can request it and the server will determine if it's allowed.
            //connection.SendMessage(mapspec, NetChannel.ReliableUnordered);

            //// send any new units and or cities 
            ////   - do we wait for user to request them?  If so, then we're potentially re-sending everything everytime the user re-connects
            ////     because we'd be trusting users to only request what they specifically don't already have
            ////     
        }

        private void SendPlayerList(Player player)
        {
            //    ' get list of all of the registerd players
            //    Dim p() As ClientServerCommon.Player
            //    p = DirectCast(mHost.mGamesStorageContext.RetreiveList(GetType(ClientServerCommon.Player).Name, "game_id", DbType.String, Game.mID), ClientServerCommon.Player())

            //    ' we endeavor to make sure that we only need to send the entire user list to any particular user ONCE
            //    ' and from there out, only join/parts of individuals.  Thus we sychronize access to adding/removing of mUsers
            //    Dim userList As New ClientServerCommon.UserList()
            //    userList.Scope = ClientServerCommon.Scope.Game ' game indicates it's only sent to all users in this SimHost.Simulation and not all players on any game on this GameServer
            //    ' add the user to the top of the list so that the client will be able to get their server assigned UserID first as opposed to last
            //    ' which is what would happen if we added it in the forloop below
            //    userList.AddUser(player.mName, player.mID)

            //    If (p IsNot Nothing) Then
            //        If (p.Length > 0) Then
            //            For Each item In p
            //                ' dont add the joined user to the user list twice. They were already added as first
            //                If item.mName <> player.mName Then
            //                    userList.AddUser(item.mName, item.mID)
            //                End If
            //            Next

            //            ' notify new user of the existing users
            //            Dim connection As Lidgren.Network.NetConnectionBase = DirectCast(player.Tag, Lidgren.Network.NetConnectionBase)
            //            Console.WriteLine("Sending userlist to user " + connection.ToString())
            //            connection.SendMessage(userList, userList.Channel)
            //        End If
            //    End If
        }

        public void RemovePlayer(Player player)
        {
            //    // TODO: i dont think we want to actually remove players, we just want to set their status to offline and then update the db
            //    mPlayers.Remove(player.mID);
            //    player.mOnline = False;
            //    mHost.mGamesStorageContext.Store(player);


            //    Dim userLeft As New ClientServerCommon.UserStatusChanged(ClientServerCommon.Enumerations.UserLeft);
            //    userLeft.UserName = player.mName;
            //    userLeft.UserID = player.mID;

            //    // notify all users
            //    Dim connections(mPlayers.Count - 1) As Lidgren.Network.NetConnectionBase;
            //    Dim i As Integer = 0;
            //    foreach KeyCommon.Player p in mPlayers.Values
            //    {
            //        connections(i) = DirectCast(p.Tag, NetConnectionBase);
            //        i += 1;
            //    }
            //    // see note above the Broadcast() method in NetServer.cs for why here we must use Groupcast and not Broadcast()
            //    mHost.mServer.Groupcast(userLeft, connections)
        }

        
#endregion

#region jitter collision callbacks

#endregion

#region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // pass true if managed resources should be disposed. Otherwise false.
        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    DisposeManagedResources();
                    _disposed = true;
                }
                DisposeUnmanagedResources();
            }
        }

        protected virtual void DisposeManagedResources()
        {
            try
            {
                _isRunning = false;
                
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Simulation.DisposeManagedResources() - " + ex.Message);
            }
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }

        protected void CheckDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException("scene is disposed.");
        }

        protected bool IsDisposed
        {
            get { return _disposed; }
        }
#endregion
    }
}