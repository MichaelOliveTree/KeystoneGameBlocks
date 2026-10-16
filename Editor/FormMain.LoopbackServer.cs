using System;
using System.Diagnostics;
using System.IO;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Types;
using Keystone.Appearance;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Scene;
using Lidgren.Network;
using Keystone.Celestial;
using Keystone.Traversers;
using Keystone.Workspaces;
using KeyCommon.Messages;
using Settings;
using System.Data.SQLite;

namespace KeyEdit
{
    // FormMain.LoopbackServer
    partial class FormMain : FormMainBase
    {
        private bool mIsLoopback = true;

        private void UserMessageReceivedGameSpecificLoopback(NetConnectionBase connection, int commandID, NetChannel channel, NetBuffer buffer)
        {
            Game01.Enums.UserMessage command = (Game01.Enums.UserMessage)commandID;
            MessageBase msg;

            switch (command)
            {
                // This is specifically space combat and nto NPC vs NPC combat
                case Game01.Enums.UserMessage.Game_PerformRangedAttack:
                     msg = new Game01.Messages.PerformRangedAttack();
                     msg.Read(buffer);

                    // todo: perhaps prefix with Action_RangedAttack 
                    Game01.Messages.PerformRangedAttack perform = (Game01.Messages.PerformRangedAttack)msg;
                    

                    Entity weapon = (Entity)Repository.Get(perform.WeaponID);
                    Container vehicle = weapon.Container;
                    Entity target = (Entity)Repository.Get(perform.TargetID);
                    Entity station = (Entity)Repository.Get(perform.StationID);
                  
                    // todo: is the attack request valid? have we exceeded cooldown? do we have energy & ammo?
                    PropertySpec[] weaponProperties = weapon.GetCustomProperties(false);
                    PropertySpec[] stationProperties = station.GetCustomProperties(false); // todo: station can have set "continuous fire" where if aimed and hasnt exceeded cooldown, will fire at next available opportunity

                   
                    // todo: perhaps prefix with Response_Attack()
                    Game01.Messages.AttackResults response = new Game01.Messages.AttackResults();
                    response.Hit = false;
                    if (target != null)
                    {
                        PropertySpec[] targetProperties = target.GetCustomProperties(false);

                        const double MAX_DISTANCE = 60000d;
                        double distanceToTarget = (weapon.DerivedTranslation - target.DerivedTranslation).Length;
                        response.DistanceToTarget = MAX_DISTANCE;
                        string operatorName = (string)station.GetCustomPropertyValue("operator");
                        string operatorID = AppMain.mScriptingHost.EntityAPI.FindDescendantByName(vehicle.Interior.ID, operatorName);
                        Entity stationOperator = (Entity)Repository.Get(operatorID);

                        if (stationOperator != null)
                        {
                            PropertySpec[] operatorProperties = stationOperator.GetCustomProperties(false);


                            // NOTE: game01 has no concept of Nodes or Entities.  We must pass all the required variables to Rules.Combat.RangedAttack() 
                            // well, instead of passing in all internal components, i can return a dice roll result that tells us what type of internal component got hit.  Then we also return as an out parameter, the index of the component that got hit? hrm
                            response.Hit = Game01.Rules.Combat.RangedAttack(stationProperties, operatorProperties, weaponProperties, targetProperties, distanceToTarget); // weaponStats, targetStats, operatorStats, distanceToTarget, isTargetEvasive

                            if (response.Hit)
                            {

                                bool isCriticalHit; // todo: this should be a property of the response
                                Predicate<Node> match = (e) =>
                                {
                                    if (e is Entity) return true;

                                    return false;
                                };

                                // todo: we need to know if target's PD succeeds. Thats what Combat.GetDamage() does
                                Node[] potentialExteriorTargets = target.Query(false, match);
                                Node[] potentialInteriorTargets = ((Container)target).Interior.Query(true, match);

                                // TODO: How does this affect production/consumption?  Consumption could make calls to GameAPI.QueryConsumption(entityID, product) for resolution.
                                //       That seems to bypass somewhat our novel system for handling production, but still, it does allow us to define products and consumers and have each consumer notified when there is production (even if its heat and cosmic rays damage from a Star's radiation) they could be affected.
                                // todo: evven sighting and detection needs to be performed as an action because it does require capability of the sensor and the operator
                                int targetIndex = 0;
                                Entity selected = (Entity)potentialExteriorTargets[targetIndex];
                                int damage = Game01.Rules.Combat.GetDamage(weaponProperties, selected.GetCustomProperties(false), distanceToTarget);
                                // what if we have an array of damaged Entities including crew?
                                // todo: if there was explosive damage type, then there might be more "selected" to iterate through to find damage for each
                                response.AddResult(target.ID, damage);
                                response.WeaponID = weapon.ID;
                                response.DistanceToTarget = distanceToTarget;
                                // todo: when the Damage is great enough, then how do we signal to do some FX like an explosion? 
                            }
                        }
                    }
                    // todo: the trickiest part is, how do we determine which (if any) interior components and crew gets damaged?
                    // todo: we could randomly pick any interior damage.  This is how RPG combat works.  We need to stop thinking in terms of
                    //       physical accuracy and rely on RPG based determinations.

                    // store results in response
                    // todo: if (hit) perhaps we can send a bunch of propertychange messages


                    connection.SendMessage(response, NetChannel.ReliableInOrder1);
                    break;
                default:
                    break;
            }
        }

        private void UserMessageReceivedLoopback(Lidgren.Network.NetConnectionBase connection, int commandID, NetChannel channel, NetBuffer buffer)
        {
            // TODO: This sequence number is not what i think it is!  It is for
            // assemblies fragmented messages!  Thus for unfragmented messages this value
            // will always wind up being 0.  A way to fix this app side without modifying lidgren
            // is to put a 4byte guid as our sequence number that server can add to the messages
            // it sends.


            // since this is a loopback server we're just going to send the command back to the user?
            // Well some commands that's appropriate, but not for all?  Hrm... Like NewScene we do need to
            // send back.
            if (commandID > (int)Enumerations.UserMessages)
            {
                // pass handling off to Game01.dll
                this.UserMessageReceivedGameSpecificLoopback(connection, commandID, channel, buffer);
                return;
            }

            KeyCommon.Messages.Enumerations command = (KeyCommon.Messages.Enumerations)commandID;
            // based on the command received, wire up the command worker function and completion functions

            KeyCommon.Messages.MessageBase msg ;


            // i think the only time we need to send the client a response is if the command has failed.
            // the client will keep a queue of guaranteed commands sent and ordered if necessary and
            // will know how to undo the command if it failed and thus get back in sync???
            switch (command)
            {
                case KeyCommon.Messages.Enumerations.DeleteFileFromArchive:
                    msg = new KeyCommon.Messages.Archive_DeleteEntry();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.AddFolderToArchive:
                    msg = new KeyCommon.Messages.Archive_AddFolder();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.AddFileToArchive:
                    msg = new Archive_AddFiles();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Geometry_Add:
                    msg = new KeyCommon.Messages.Geometry_Add();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.AddGeometryToArchive:
                    msg = new KeyCommon.Messages.Archive_AddGeometry();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;

                //case KeyCommon.Messages.Enumerations.LoadScene:
                //    msg = new KeyCommon.Messages.Scene_Load();
                //    msg.Read(buffer);
                //    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                //    break;
                case KeyCommon.Messages.Enumerations.NewScene:
                    // TODO: I must send id of root for client to create - Actually
                    //       for empty new scene we use the filename for the root node
                    msg = new KeyCommon.Messages.Scene_New();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.NewFloorplan:
                    // TODO: I must send id of interior region and container for client to create 
                    msg = new KeyCommon.Messages.Floorplan_New();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.NewUniverse:
                    {
                        string id = Repository.GetNewName(typeof(object));
                        msg = new KeyCommon.Messages.Scene_NewUniverse();
                        msg.Read(buffer);

                        Scene_NewUniverse newUniverse = (Scene_NewUniverse)msg;

                        CreateSaveDirectory(newUniverse.FolderName);
                        CreateSceneDirectory(newUniverse.FolderName);

                        try
                        {
                            // TODO: this .CreateSave() call is saving user records to the save.db in the \\saves\\ path and not a seperate db for the Campaign Scene. 

                            // just like in The Sims, you can't save a bunch of states.  The game is an active running simulation\campaign. You can have multiple campaigns though.
                            Database.AppDatabaseHelper.CreateSave(newUniverse.FolderName, "save.db");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("FormMain.LoopbackServer.UserMessageReceived() - ERROR: Database - " + ex.Message);
                        }

                        _core.Seed = newUniverse.RandomSeed;
                        GenerateNewUniverse(newUniverse);

                        bool enableHack = false;
                        if (enableHack)
                        {
                            // todo: Save of Interior does not need to occur on any change.  We have the changes in Memory.  We only need to save when user clicks "save" or when closing the floorplan tab and then we should prompt user to "save changes?".

                            // todo: add a starfield to the generated scene
                            //if (newUniverse.CreateStarField) - i think we should always add a starfield and then add menu option to hide it
                            //{
                            //}
                            // todo: add a nebula style starbox?

                            // TODO: BEGIN TEMP - below block is not needed.  We now use spawnpoints that are either
                            // placed into the Scene during EDIT mode or are loaded during Mission loading during ARCADE
                            Database.AppDatabaseHelper.WorldRecord worldRecord = Database.AppDatabaseHelper.GetWorldRecordByName("Earth");
                            double worldRadius = worldRecord.Radius;
                            double orbitalRadius = worldRadius * 4d; // 7000000;
                            Vector3d vector = Vector3d.Normalize(worldRecord.Translation);
                            Vector3d translation = worldRecord.Translation + (vector * -orbitalRadius);
                            // https://stackoverflow.com/questions/14845273/initial-velocity-vector-for-circular-orbit

                            string prefabVehicleFullPath = Path.Combine(AppMain.MOD_PATH, newUniverse.VehiclePrefabRelativePath); // todo: rename WhiclePath to VehiclePrefabPath and add a VehicleSavePath for alt

                            // NOTE: since this is loopback, we can delay resource loading.  The RegisterChild() that occurs in Interior.AddChild() is not necessary as 
                            //       the client will load resources and properly register interior children
                            bool delayResourceLoading = true;
                            bool generateIDs = true; // generate a unique ID for all non-shareable nodes
                            string[] nodeIDsToUse = null; // this should be null because we are generating here on the server and using clone = true
                            Keystone.Vehicles.Vehicle vehicle = (Keystone.Vehicles.Vehicle)LoadEntity(prefabVehicleFullPath, newUniverse.VehiclePrefabRelativePath, generateIDs, true, delayResourceLoading, nodeIDsToUse, translation);
                            vehicle.SetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.PlayerControlled, true);
                            vehicle.SRC = null; // null here is critical or SceneReader will go into an infinite loop. This instructs it that it is not referencing a nested prefab



                            // todo: the mScene is not initialized yet. argh  I could perhaps make them static methods since they dont require being attached to the Scene
                            // todo: this needs to specify a SAVE_LOCATION enum
                            Scene.CopyInteriorDataFile(vehicle, newUniverse.VehiclePrefabRelativePath);
                            string interiorID = vehicle.Interior.ID;
                            Scene.WriteEntity(vehicle, false);


                            //// TEMP TEST OF WRITE CONTAINER
                            //// todo: can i use a single file and still recycle resource nodes? In testing, the single vehicle file is a 4kb smaller however...  and it seems faster to read/write but i should time it to be sure.
                            ////       I can still add a compression option when transfer_stream is initiated.
                            //// todo: test that the single file kgbentity can be loaded instead of the prefab version... i think i will need to add it to a zip under vehicles.xml
                            //SceneWriter writer = new SceneWriter(null, AppMain._core.ThreadPool, null, null);
                            //Stream stream;
                            //writer.WriteSychronous(vehicle, true, out stream);

                            //FileStream fs = new FileStream(AppMain.SAVES_PATH + "\\Campaign001\\test2.kgbentity", FileMode.Create);
                            //stream.Seek(0, SeekOrigin.Begin);
                            //stream.CopyTo(fs);

                            //fs.Flush();
                            //fs.Close();
                            //fs.Dispose();

                            //stream.Close();
                            //stream.Dispose();
                            // todo: after increment/decrement, test loading the vehicle here to see if the Node hierarchy is the same 
                            //       SceneReader actually checks the Repository for existing instances so that behavior must remain the same.  


                            // todo: do a test of loading all 100 crew from single file versions of the "database"
                            // IMPORTANT: our prefab db does serve one good purpose, it holds the common name preview.png
                            // END TEMP HACK

                            string userName = _core.Settings.settingRead("network", "username");
                            mStartingRegionID = worldRecord.RegionID;
                            // TODO: we need to do better saving state and cleaning up all nodes in Repository when shutting down.
                            Database.AppDatabaseHelper.CreateVehicleRecord(userName, vehicle, mStartingRegionID, mStartingRegionID, AppMain.CURRENT_SCENE_NAME + "\\" + vehicle.ID + ".kgbentity", Database.AppDatabaseHelper.GetConnection());

                            // on interior region paged in, we search for NPCs in a vehicle.ID and retrieve them and spawn them to the client
                            int crewCount = 100;
                            GenerateCrew(interiorID, crewCount, _core.Seed);

                            // NOTE: for a remote server, we would be adding the vehicle to the scene under the appropriate parent, but with loopback, the client maintains the scene
                            //       Here we only create the entity.kgbentity files which the client can receive and then add to the correct parent
                            Repository.IncrementRef(vehicle);
                            Repository.DecrementRef(vehicle);
                            // TEMP - END HACK
                        }

                        _core.CurrentSaveName = AppMain.CURRENT_SCENE_NAME;
                        // _core.ArcadeEnabled = true;  // TODO: i think ArcadeEnabled=true here is wrong because here we are only generating not "playing"
                        //                                 ArcadeEnabled = true should only be true when a mission has been loaded which never happens here

                        // TODO:  i think this is enough for NewUniverse gen.  We may need to load enemy AI vehicles too though.
                        //        It's when the client sends Join_Request that we should return the regionID and vehicle id that the user can then grab the path from the vehicle record.
                        //        In an actual remote server implementation, we need to send the xml .kgbentity for vehicles.

                        // TODO: what about AI vehicles? do we generate those here now too? Like in Begin, do we allow user to specify the number and type of AI opponents? Then we can spawn them in procedural locations
                        // TODO: I think this is correct.  We are generating a procedural GAME campaign and not just the universe scene.



                        // TODO: Then we page only relevant Zones and Entities to client including all interior ship Entities.  Client doesn't deal with scene database directly.  Hrm?  Fast enough over loopback but slow during runtime except its background spawning and the iniitial
                        //       spawning we can use a splash loading dialogue.  This is ok for a space sim, but for future other KGB based games, streaming the entire relevant map to a user is expensive even if it's idealistic.
                        // TODO: serverside (espeically multiplayer) the entire Universe can be kept in memory and no graphics need to be loaded.. just scripts.

                        // TODO: how do we sycnchronise GUIDs for stars and planets and such if only the client is generating the universe?
                        //       - if all "entities" are spawned by the server, then we would send the guid for each entity to the client and then the client doesn't actually read from disk, but from a stream and then use the supplied guids from the server?
                        //         Would this be too slow?

                        // TODO: we need to create a new GUID for a vehicle and pass that to the client so that it can save the prefab and interior.db to the \\saves path

                        // TODO: Normally when connecting to a real network server, the universe would
                        //       generate on the server, and the client would get piecemeal data based on
                        //       what information about the universe is generally knowable versus must be
                        //       obtained through scanning or finding/obtaining computer databases from other races.
                        //       SO HOW DO WE STRUCURE PIECEMEAL data passing to client?
                        //       - client requests data for galactic map?
                        //       - client requests data on it's crew state? (since a default crew is first loaded)
                        //          - 1 call to get crew base records
                        //              - subsequent call for each individual crew member we are looking up (think dumb terminal?)
                        //       - client requests data on ship components.
                        //       - I do think client initiated requests is the simplest way to go here. 
                        //          - if this is the case, we can do these as needed.  First would be getting star records.
                        //


                        // TODO: Client would not normally create a DB table.  But the server could
                        //       handle it and then that DB is shared between client and server.  Client
                        //       is doing read only queries where as server does read/write
                        //       SO this means our procedural generation should be done server side.
                        //       And upon completion, send command to client that it's done.
                        //       WAIT!  Client does need to create the same procedural universe because
                        //       the universe gen also creates the Geometry.  What if we could 
                        //       make the geometry done client side where server uses geometry-less (and appearance-less)
                        //       scene.  Also what if instead of giant xml db, we store just the Entity's 
                        //       prefab locations and we generate the prefabs client side during procedural generation
                        //       and store that path in DB rather than in xmld  This is something i think
                        //       we need to do sooner or later and so should just do it now...
                        //       - I think we could generate all necessary tables here though.
                        //       and the client can do the same?  Ugh.  We don't went to send an entire 
                        //       db to the client.  We want the client to only get info for what it's sensors scan or
                        //       it's crew perceive.

                        // TODO: what if we needed to create multiple vehicles for multiple users?
                        //      1) - or if a user connects and does not currently have a Vehicle,
                        //			We select a starting homeworld for them
                        //			We select a starting position for their Vehicle
                        //			- we assign to "Player" table, the playerID and vehicle_path
                        //      2) - or if user connects and is RESUMING play
                        //			We query the DB for their Vehicle's record ID
                        //			We load that vehicle
                        //
                        // send seperate command to Create_Node for Vehicle
                        //KeyCommon.Messages.Enumerations.Node_Create;


                        //                		//		- or test for a IsSpawned flag in the save db.
                        //                		//      - but now we need to start thinking about save game state
                        //                		//		- Viewpoint in SceneInfo needs to start at our ship spawn point.
                        //                		//			- Viewpoint is PER CLIENT not scene wide. SERVER DOES NOT CARE ABOUT VIEWPOINTS!
                        //                		//			- Viewpoint is attached to Vehicle or attaches itself to Vehicle on Vehicle Scene.EntityAttached().
                        //                		//				- pager then pages in regions based on Viewpoint's region
                        //                		//		- will the server to client commands serialize on the client's background thread so that we definetly load the vehicle AFTER the scene is loaded?
                        //                		//		
                        //                		//   - do we need to generate spawn points then during universe gen?  Perhaps GEOsynchronous orbit positions.
                        //                		//	 - 6) HOW DO WE RESUME SAVED GAME?!  What is the process? User's ship must be assigned to database
                        //						//   - this would be true on initial universe generation too?  Or Vehicle assignment done as second command made by client?
                        //						//	   Imagine multiple clients connecting and requesting to assign vehicles on Game Server start?

                        //                	}

                        // do we wait for user command to "Join_Request"? and then server sends
                        // a Scene_Load command and a Simulation_Join command.
                        // What about when user select an existing scene?  Do they
                        // send a Join_Request in that instance too?  I think so.

                        connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    }
                    break;
                case KeyCommon.Messages.Enumerations.NewTerrainScene:
                    // TODO: I must send id of root for client to create and for any default terrains and waters
                    msg = new KeyCommon.Messages.Scene_NewTerrain();
                    msg.Read(buffer);

                    Scene_NewTerrain newTerrainScene = (Scene_NewTerrain)msg;

                    CreateSaveDirectory(newTerrainScene.FolderName);
                    CreateSceneDirectory(newTerrainScene.FolderName);

                    try
                    {
                        // just like in The Sims, you can't save a bunch of states.  The game is an active running simulation\campaign. You can have multiple campaigns though.
                        Database.AppDatabaseHelper.CreateSave(newTerrainScene.FolderName, "save.db");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("FormMain.LoopbackServer.UserMessageReceived() - ERROR: Database - " + ex.Message);
                    }
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                //case KeyCommon.Messages.Enumerations.LoadSceneRequest:
                //    msg = new KeyCommon.Messages.Scene_Load_Request();
                //    msg.Read(buffer);

                //    Scene_Load_Request request = (Scene_Load_Request)msg;
                //    // TODO: validate the user via database

                //    // determine the simulation type (single region, multi-region land, multi-zone space sim)

                //    // determine starting Region ID
                //    // if this is a single region scene, use root.ID

                //    // else look up starting region in database 

                //    request.Approved = true;
                //    Scene_Load loadApproved = new Scene_Load();
                //    loadApproved.FolderName = request.FolderName;
                //    loadApproved.StartingRegionID = mStartingRegionID;
                //    connection.SendMessage(loadApproved, NetChannel.ReliableInOrder1);
                //    break;
                case KeyCommon.Messages.Enumerations.Simulation_Join_Request:
                    {
                        msg = new KeyCommon.Messages.Simulation_Join_Request();
                        msg.Read(buffer);

                        Simulation_Join_Request simJoinRequest = (Simulation_Join_Request)msg;
                        UserRecord ur = GetUserRecord(simJoinRequest.UserName);
                        connection.Tag = ur;

                        
                        // todo: we do potentially need to load a specified mission
                        // to determine a startingRegionID for player controlled vehicles that are spawned
                        // by the mission

                        XMLDatabase mXMLDB = new XMLDatabase();
                        string fullFolderPath = Path.Combine(AppMain.SCENES_PATH, simJoinRequest.FolderName);
                        
                        SceneInfo sceneInfo = mXMLDB.Open(fullFolderPath, true);

                        Viewpoint[] viewpoints = sceneInfo.Viewpoints;
                        string startingRegionID = null;

                        // grab the viewpoints from the SceneInfo and find an appropriate one and grab the startingRegionID
                        if (sceneInfo.FirstNodeTypeName == typeof(Keystone.Portals.ZoneRoot).Name)
                        {
                            // todo: we can't assume this always works because if there are more than one player, there will be multiple viewpoints
                            startingRegionID = viewpoints[1].StartingRegionID;
                        }
                        else startingRegionID = viewpoints[0].StartingRegionID;
                        
                        // todo: remove VehicleID from Simulation_Join 

                        Simulation_Join simJoinApproved = new Simulation_Join();
                        // simJoinApproved.Approved = true;
                   //     simJoinApproved.VehicleID = vr.ID;
                        simJoinApproved.FolderName = simJoinRequest.FolderName;
                        //simJoinApproved.CampaignName = simJoinRequest.CampaignName;
                        simJoinApproved.MissionName = simJoinRequest.MissionName;

                        //     simJoinApproved.VehicleSaveRelativePath = vr.RelativeEntitySavePath;
                        simJoinApproved.RegionID = startingRegionID; // vr.RegionID;
                   //     simJoinApproved.Translation = vr.Translation;

                        _core.CurrentSaveName = simJoinApproved.FolderName;
                        

                        connection.SendMessage(simJoinApproved, NetChannel.ReliableInOrder1);
                    break;
                    }

                case KeyCommon.Messages.Enumerations.Simulation_Leave:
                    {
                        msg = new KeyCommon.Messages.Simulation_Leave();
                        msg.Read( buffer);

                        mScene.Simulation.Running = false;

                        Simulation_Leave leave = (Simulation_Leave)msg;
                        System.Diagnostics.Debug.Assert(leave.UserName == ((UserRecord)connection.Tag).UserName);
                        Database.AppDatabaseHelper.VehicleRecord vr = Database.AppDatabaseHelper.GetVehicleRecord(leave.UserName);
                        Container container = (Keystone.Vehicles.Vehicle)Repository.Get(vr.ID);
                        Database.AppDatabaseHelper.UpdateVehicleRecord(container.ID, container.Region.ID, container.Translation);

                        // vehicle should already be selected to the FloorplanWorkspace when in Simulation mode.  We shouldn't have to click the vehicle object in the scene explorer or 3d viewport

                        // save this user's state
                        // todo: verify changes to internal structure are saved to the interior database
                        container.SRC = null; // todo: during simulation this should already be null when loading the entity but it's not and is causing the Write to use NodeState objects (which then results in a infinite loop on trying to read())
                        Scene.WriteEntity(container, false); // todo: this is saving empty BonedEntity elements to the Interior.xml.  NOTE: this doesnt occur during galaxy generation because the npcs are never added to the Vehicle.Interior before the Zones are saved.
                        // I could just remove all the npc's from the Interior prior to writing the Vehicle container.
                        // but really, we should not be saving empty xml elements.

                        string[] characterIDs;
                        string[] types;
                        container.Interior.GetChildIDs(null, out characterIDs, out types);
                        System.Collections.Generic.List<BonedEntity> bonedEnts = new System.Collections.Generic.List<BonedEntity>();
                        for (int i = 0; i < types.Length; i++)
                        {
                            if (types[i] == typeof(BonedEntity).Name)
                                bonedEnts.Add((BonedEntity)Repository.Get(characterIDs[i]));
                        }

                        if (bonedEnts.Count > 0)
                        {
                            string[] ids = new string[bonedEnts.Count];
                            Vector3d[] translations = new Vector3d[bonedEnts.Count];

                            for (int i = 0; i < translations.Length; i++)
                            {
                                translations[i] = bonedEnts[i].Translation;
                                ids[i] = bonedEnts[i].ID;
                            }
                            // NOTE: for now i only need to update the translations and not relativePath or anything
                            Database.AppDatabaseHelper.UpdateCharacterRecords(ids, translations);
                            
                            // TODO: do i need to remove all BonedEntities from the Scene before we save it?  Currently
                            //       I am saving Interior to the scenes\\ path and not just to the saves\\ path. 
                            for (int i = 0; i < bonedEnts.Count; i++)
                                Scene.WriteEntity(bonedEnts[i], true);

                            mScene.XMLDB.SaveAllChanges();
                            System.Windows.Forms.MessageBox.Show("Save completed.", "Save completed.", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly, false);
                        }
                        // this includes both the save.db and the scene xml (at least the dynamic entities like vehicle and crew)
                        // this is complicated because components like reactors and engines have dynamic properties that need to be saved too yet they are stored in custom properties within the scene xml and not as seperate prefabs
                        // should we just go all the way and make every component on the ship a .kgbentity under the \\saves\\ path?
                        // well we could still just save all Interior.kgbengith components inside the Interior xml since it's all saved in one go and we dont have to save what is stored in memory until we quit
                        // although we need to remember that in real multiplayer we'll need to broadcast state changes to other players that can see them such as durinmg boarding parties
                        // notify any other users that a player has left?

                        // if this is the last player in the simulation, we can save all states for ai vehicles and crew and shut down the entire sim and scene
                        break;
                    }
                case KeyCommon.Messages.Enumerations.Simulation_GenerateCharacters:
                    {
                        msg = new KeyCommon.Messages.Simulation_GenerateCharacters();
                        msg.Read(buffer);
                        Simulation_GenerateCharacters generateCharacters = (Simulation_GenerateCharacters)msg;

                        GenerateCrew(generateCharacters.ParentID, generateCharacters.Quantity, AppMain._core.Seed);

                        // spawn them on the client
                        Database.AppDatabaseHelper.CharacterRecord[] characterRecord = Database.AppDatabaseHelper.GetCharacterRecords(generateCharacters.ParentID);
                        Vector3d[] characterPositions = new Vector3d[characterRecord.Length];

                        // establish initial positions for each NPC. It is required that the Vehicle.Interior be loaded
                        characterPositions = PositionCrew(generateCharacters.ParentID, characterRecord.Length);

                        SpawnCharacters(generateCharacters.ParentID, characterRecord, characterPositions, connection);
                    }
                    break;
                case KeyCommon.Messages.Enumerations.RegionPageComplete:
                    msg = new KeyCommon.Messages.RegionPageComplete();
                    msg.Read(buffer);
                    RegionPageComplete regionComplete = (RegionPageComplete)msg;

                    if (connection.Tag == null) break;
                    UserRecord user = (UserRecord)connection.Tag;

                    // initiate spawning of dynamic entities to client
                    // find dynamic Vehicle's in the user's Region
                    string regionID = regionComplete.RegionID;
                    Keystone.Portals.Region region = (Keystone.Portals.Region)Repository.Get(regionID);


                    // TODO: here we need to check the mission object for mission objects that need to be loaded into the scene
                    //       Additionally, we may need to check for what existing entities are now in range of current players
                    //       (NOTE: in multiplayer, players may exist in different regions) and tell those players to spawn 
                    //       those Entities that are now in detection range.
                    Keystone.Simulation.Missions.Mission currentMission = _core.SceneManager.Scenes[0].Simulation.CurrentMission;

                    // I BELIEVE THE BELOW IS OBSOLETE - we need to refer to the currentMission to determine if anything is to be spawne.d

                    //// todo: do i need to get the vehicleID?  I just need to find all vehicles in db that are within the paged in RegionID
                    //Database.AppDatabaseHelper.VehicleRecord vehicleRecord = Database.AppDatabaseHelper.GetVehicleRecord(user.UserName);

                    //// todo: the below method of spawning i think is obsolete.  We are working towards using spawnpoints
                    ////       to spawn players and AI vehicles.

                    //// todo: when a region is paged out for a particular client,  we may need to let the mission object instance know
                    ////       which mission objects are no longer active in the client's scene. not sure how this works on server side
                    ////       because i think we awlays wanted for mission processing and updates to occur in loopback/server but for
                    ////       real server, the entire universe stays in memory and never gets paged out (or does it?)
                    //if (regionID == vehicleRecord.RegionID)
                    //{
                    //    // the only thing different about a spawn and a prefab_load is that spawn might require us to send the .kdgentity to the client whereas a prefab already exists on the client.
                    //    // I seriously need to consider how we should transfer the entity data... 1 of two options 1) send it as a file 2) send it as nested hierarchy of IRemoteableType and then save it to the appropriate current_save path
                    //    // but a vehicle.kgbentity for instance and its celldb could be very large.  This means we need to add a fileTransfer layer in the EXE to nmanage the concatenation of received file data packets. WE DO NOT WANT TO MODIFY LIDGREN ITSELF
                    //    Simulation_Spawn spawn = new Simulation_Spawn();
                    //    spawn.EntitySaveRelativePath = vehicleRecord.RelativeEntitySavePath;
                    //    spawn.Translation = vehicleRecord.Translation;
                    //    spawn.ParentID = regionID; // vehicleRecord.ParentID;
                    //    spawn.CellDBData = null;
                    //    //spawn.EntityFileData = null;

                    //    //Prefab_Load spawn = new Prefab_Load();

                    //    connection.SendMessage(spawn, NetChannel.ReliableInOrder1);
                    //}
                    //else if (region is Keystone.Portals.Interior)
                    //{
                        
                        
                    //    Database.AppDatabaseHelper.CharacterRecord[] characterRecord = Database.AppDatabaseHelper.GetCharacterRecords(region.ID);
                    //    Vector3d[] characterPositions = new Vector3d[characterRecord.Length];
                        
                    //    if (CrewPositionsNeedInitialization(characterRecord)) // todo: temp need to test for first run
                    //    {
                    //        // establish initial positions for each NPC 
                    //         characterPositions = PositionCrew(region.ID, characterRecord.Length);
                    //    }
                    //    else
                    //    {
                    //        // use existing saved positions
                    //        for (int i =0; i < characterRecord.Length; i++)
                    //            characterPositions[i] = characterRecord[i].Translation;
                    //    }

                    //    // if the interior has no Area connectivity, characterPositions will be null so don't spawn any crew
                    //    if (characterRecord != null && characterPositions != null)
                    //        SpawnCharacters(region.ID, characterRecord, characterPositions, connection);

                    //}
                    break;
                case KeyCommon.Messages.Enumerations.PrefabSave:
                    msg = new KeyCommon.Messages.Prefab_Save();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.PrefabLoad:
                    msg = new KeyCommon.Messages.Prefab_Load();
                    msg.Read(buffer);


                    // IMPORTANT: For loopback generating IDs is not necessary because the Nodes 
                    // that exist in the Client are obviously shared here.  Thus we
                    // wrap the following code in #if DEBUG block for testing and verification.
                    // For a remote server, the Nodes generated by the Server must always
                    // match those created on the client (Including the Node IDs.)
                    // Even when in EDIT mode, a client cannot send for example
                    // ChangeProperty commands if the NodeID for the Node they want
                    // to edit is not the same as that on the Server.
#if DEBUG
                    if (mIsLoopback)
                    {
                        Prefab_Load load = (Prefab_Load)msg;
                        // load the Entity and grab the NodeIDs for non-shareable Nodes that were generated
                        bool generateIDs = true;
                        
                        string fullPath = System.IO.Path.Combine(_core.ModsPath, load.EntryPath);
                        Entity entity = LoadEntity(fullPath, load.EntryPath, generateIDs, true, true, null, load.Position);
                        System.Collections.Generic.Queue<string> IDs = new System.Collections.Generic.Queue<string>();
                        GetNonShareableNodeIDs(entity, ref IDs);
                        string[] generatedIDs = IDs.ToArray();

                        // for loopback we do not want to keep this in memory once we've grabbed the nodeIDStoUse
                        Repository.IncrementRef(entity);
                        Repository.DecrementRef(entity);


                        // TODO: we need to make sure the non-shareable nodes are removed from the Repository
                        // in order for the client to crete Nodes that use the generatedIDs we created here

                        // if necessary (eg. NodeIDsToUse size is too large), generte multiple command fragments 
                        string guid = System.Guid.NewGuid().ToString();
                        int lengthOfSingleID = guid.Length;
                        int maxSize = _core.MaxFragmentSize;
                        int IDsPerPacket = maxSize / lengthOfSingleID;

                        if (IDsPerPacket < generatedIDs.Length)
                        {
                            int offset = 0;
                            for (int i = 0; i < IDsPerPacket; i++)
                            {
                                int size = generatedIDs.Length - offset >= IDsPerPacket ? IDsPerPacket : generatedIDs.Length - offset;
                                string[] idsForThisPacket = new string[size];
                                for (int j = 0; j < size; j++)
                                    idsForThisPacket[j] = generatedIDs[j + offset];

                                // assign the NodeIDs to the Prefab_Load command before passing each fragment to the client
                                load.NodeIDsToUse = idsForThisPacket;
                                connection.SendMessage(load, NetChannel.ReliableInOrder1);

                                offset += size;
                            }
                        }
                        else
                        {
                            // assign the NodeIDs to the Prefab_Load command before passing it to the client
                            load.NodeIDsToUse = generatedIDs;
                            connection.SendMessage(load, NetChannel.ReliableInOrder1);
                        }
                    }
#else
                    System.Diagnostics.Debug.Assert (mIsLoopback == true);
                    // NOTE: if this is NOT loopback, then we do need to load the prefab and add it to the server's scene
                    // and generate the node IDs and pass them to relevant clients
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
#endif
                    break;

                case KeyCommon.Messages.Enumerations.InsertPrefab_Interior:
                    // TODO: Here we should validate the placement so the client doesn't have to. server is authoritative
                    msg = new KeyCommon.Messages.Prefab_Insert_Into_Interior();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.InsertPrefab_Structure:
                    msg = new KeyCommon.Messages.Prefab_Insert_Into_Structure();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.PlaceEntity_EdgeSegment:
                    msg = new KeyCommon.Messages.Place_Entity_In_EdgeSegment();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.CelledRegion_PaintCell :
                    msg = new KeyCommon.Messages.PaintCellOperation();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.TileMapStructure_PaintCell:
                    msg = new KeyCommon.Messages.TileMapStructure_PaintCell();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                // TODO: this wall placement message i believe is obsolete because
                // InsertPrefab_CelledRegion covers walls, floors and regular components
                case KeyCommon.Messages.Enumerations.PlaceWall_CelledRegion:
                    msg = new KeyCommon.Messages.Place_Wall_Into_CelledRegion();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Terrain_Paint:
                    msg = new KeyCommon.Messages.Terrain_Paint();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Entity_Move:
                    msg = new Entity_Move();
                    msg.Read(buffer);

                    // todo: we should be clear here that this is multiple entity alignment and not just
                    //       behavioral or AI or physics resulting movement.  we should probably just rename the command to be clear
                    // also, we don't cross Region boundaries.  The re-position occurs within same parent region. (eg Interior)
                    Entity_Move move = (Entity_Move)msg;
                    if (ValidateEntityPosition(move))
                        connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.GeometrySave:
                    msg = new KeyCommon.Messages.Geometry_Save();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Node_Remove:
                    msg = new KeyCommon.Messages.Node_Remove();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Node_Create:
                    System.Diagnostics.Trace.Assert(false, "FormMain.LoopbackServer.UserMessageReceivedLoopback() - ERROR: This switch case should be unnecessary in LoopbackSErver and instead should use the Node_Create_Request case.");
                    //msg = new KeyCommon.Messages.Node_Create();
                    //msg.Read(buffer);

                    //// TODO: Node_Request_Create and Node_Request_Add should be same command
                    //// both require a NodeType otherwise we have to guess based on resource type
                    //// and for some texture types thats not always possible.
                    //string parent = ((KeyCommon.Messages.Node_Create)msg).ParentID;
                    //string nodeType = ((KeyCommon.Messages.Node_Create)msg).NodeType;

                    //// use the factory to create the node type
                    //string id = Keystone.Resource.Repository.GetNewName(typeof(object));
                    //// TODO: if this is an animation, how do i provide an index to indicate
                    //// it's child index
                    //Elements.Node node = Resource.Repository.Create(id, nodeType);

                    //// now since this is loopback and we share the same Repository, the above Factory.Create
                    //// already create's this node in the Repository!  So we'll add this created node
                    //// to the scene (TODO: here in this MessageProc is it threadsafe to access
                    //// scenegraph?

                    //// our return message will be the "Node_Create" but our client will first verify
                    //// that the requested cretae node doesnt already exist.  IF it does, it will 
                    //// not create it but just update the properties.
                    //Elements.Node parentNode = (Elements.Node)Keystone.Resource.Repository.Get(parent);
                    //if (!(parentNode is Keystone.Elements.IGroup)) throw new Exception ("Command is invalid, i should test validity first");

                    //// NOTE: WE do NOT use SuperSetter to addChild the node here because
                    //// This is loopback and we want the loopback to play nice w/ the client code so that
                    //// the client code never has to be modified to work with loopback or remote
                    //// and in case of Remote, the client would be forced to use SuperSetter so 
                    //// if we did it here in loopback, it would make the child already added 
                    //// and the client code handler for Node_Create would break

                    //// BUG: HOwever regarding the above, there is a problem if we try to serialize a
                    ////      created node that also has child created nodes.  Those will encounter the
                    ////      the problem of the client trying to SuperSetter them to parents which already
                    ////      have them.  And we can't dereference all the children recursively because 
                    ////      these nodes may be in multiple use and we dont want to undo them because they're
                    ////      already in use by the scene!  
                    ////      TODO: There has to be an elegant solution to this... this is not a huge problem
                    ////            to solve.  The simplest solution may be to look at the Node_Create()
                    ////            reply (not just the Node_Create_Request())
                    //// 
                    //// TODO: Undo/Redo must also be considered for loopback
                    ////  - actually undo/redo should always be solely client/side.
                    ////    as far as tracking changes that can be done/undone.
                    ////    Though i'm not 100% sure how we manage shared changes except to
                    ////    Only allow a person to "check out" one region at a time.
                    ////    But also allow perhaps people to be notified when a region they
                    ////    subscribed for notifiations and perhaps scheduled next available time
                    ////    for, is available and reserved for them to edit.

                    //// create a Node_Create message for the response 
                    //// TODO: Node-Create_Request requires a reply of Node_Create,
                    ////       what on earth is this doing?  I think this switch case is redundant
                    //// and probably forgot to be removed from loopback. 
                    //KeyCommon.Messages.Node_Create create = new KeyCommon.Messages.Node_Create(node, parent);
                    //connection.SendMessage(create, NetChannel.ReliableInOrder1);
                    break;


                case KeyCommon.Messages.Enumerations.Node_Create_Request:  // REQUEST
                    msg = new Node_Create_Request();
                    msg.Read(buffer);

                    string p2 = ((Node_Create_Request)msg).ParentID;
                    string res = ((Node_Create_Request)msg).ResourcePath;
                    string nodeTypeName2 = ((Node_Create_Request)msg).NodeTypename;

                    string[] descendantIDs = null;
                    string[] descendantTypes = null;
                    string[] descendantParents = null;

                    // if this is not an IPageableTVResource, then res will be null
                    // TODO: maybe better to add in Node_Create_Request an explicit flag to signify this
                    // rather than relying on null or empty state of the resourcepath?

                    Node node2 = null;
                    // Load as a Resource
                    if (string.IsNullOrEmpty(res)) // TODO: rather than test res != "", this should test typename and ignore res if the typename is not a res type
                    {  
                        
                        if (nodeTypeName2 != "DefaultAppearance[]")
                        {
                            node2 = Keystone.Resource.Repository.Create(nodeTypeName2);
                            // apply the properties specified by user.  NOTE: rules will be
                            // (should be) checked here
                            node2.SetProperties(((Node_Create_Request)msg).Properties);
                        }
                        else
                        {
                            // May.11.2017 - The following I think is used only by our EntityPlugin
                            // specifically the Model tab where we want to create GroupAttributes for every
                            // group in a Mesh.  This functionality occurs automatically when we import
                            // Geometry as an Entity or Vehicle/Container, but it does not occur
                            // when we for instance, go to the plugin Model tab and add a ModelSequence
                            // node and wish to attach Models with Geometry under it and then to 
                            // create the associated GroupAttributes under a DefaultAppearance node.
                            node2 = Keystone.Resource.Repository.Create("DefaultAppearance");

                            // find the Mesh3d or Actor3d
                            IGroup parentGroupNode = (IGroup)Repository.Get(p2);
                            int found = -1;
                            if (parentGroupNode.ChildCount > 0)
                            {
                                for (int i = 0; i < parentGroupNode.ChildCount; i++)
                                {
                                    if (parentGroupNode.Children[i] is Mesh3d ||
                                        parentGroupNode.Children[i] is Actor3d)
                                    {
                                        found = i;
                                        break;
                                    }
                                }


                                if (found >= 0)
                                {
                                    Geometry g = (Geometry)parentGroupNode.Children[found];
                                    if (g.GroupCount > 0)
                                    {
                                        SuperSetter setter = new SuperSetter(node2);
                                        Node[] groupAttributes = new Node[g.GroupCount];
                                        descendantIDs = new string[g.GroupCount];
                                        descendantTypes = new string[g.GroupCount];
                                        descendantParents = new string[g.GroupCount];
                                        for (int i = 0; i < g.GroupCount; i++)
                                        {

                                            groupAttributes[i] = Keystone.Resource.Repository.Create("GroupAttribute");
                                            descendantIDs[i] = groupAttributes[i].ID;
                                            descendantTypes[i] = groupAttributes[i].TypeName;
                                            descendantParents[i] = node2.ID;

                                            // obsolete: Feb.9.2013 if we SuperSetter apply here, then we should not
                                            // apply client side.  that of course is lame because
                                            // we shouldnt ever change the client code to accommodate
                                            // different servers.  we should change the servers if
                                            // it's loopback to work with the client considering this fact
                                            //setter.Apply(groupAttributes[i]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else // Load as explicity type with specified property values provided by user
                    {
                        // this is a resource type, however some types 
                        // like texture paths or shader paths require the ProceduralShader
                        // or Layer node be created to host it.  The client can request to directly
                        // create those, however when directly requesting a Shader or Texture
                        // we automatically respond by creating all necessary nodes.
                        //if (nodeType2 == typeof(ProceduralShader).Name)
                        //{
                        //    // create the procedural shader 
                        //    node2 = Keystone.Resource.Repository.Create(nodeType2);
                        //    // assign the resourcetarget so that the client will deserialize it and
                        //    // be able to compile a local shader using client side graphics settings
                        //    ((ProceduralShader)node2).ResourcePath = res;
                        //} else
                        if (nodeTypeName2 == typeof(Texture).Name)
                        {
                            // for TextureCycle, creating texture directly is ok without a Layer
                            // since the TextureCycle is our Layer
                            Node p = (Node)Repository.Get(p2);
                            System.Diagnostics.Debug.Assert(p is TextureCycle);
                            node2 = Keystone.Resource.Repository.Create(nodeTypeName2);
                            ((Texture)node2).ResourcePath = res;
                        }
                        else if (nodeTypeName2 == typeof(Material).Name)
                        {
                            Node parent2 = (Node)Repository.Get(p2);
                            node2 = Repository.Create(res, nodeTypeName2);
                            

                        }
                        else
                        {
                            // res is used as ID so loopback server does not need to generate it
                            node2 = Keystone.Resource.Repository.Create(res, nodeTypeName2);
                        }

                        // apply the properties specified by user.  NOTE: rules will be
                        // (should be) checked here
                        node2.SetProperties(((Node_Create_Request)msg).Properties);
                    }

                    // now in the case of remote server, here'd we'd also add the node to the scene
                    // right?  before then sending command to all relevant users to create this node>?

                    // create a Node_Create message for the response 
                    // NOTE: All Properties for starting node and all descendants are serialized in the commands .Write(NetBuffer buffer)
                    Keystone.Messages.Node_Create create2 = new Keystone.Messages.Node_Create(node2, p2);
                    if (descendantIDs != null && descendantIDs.Length > 0)
                    {
                        for (int i = 0; i < descendantIDs.Length; i++)
                        {
                            create2.AddDescendant(descendantIDs[i], descendantTypes[i], descendantParents[i]);
                        }
                    }
                    connection.SendMessage(create2, NetChannel.ReliableInOrder1);
                    break;
                    // OBSOLETE - lights now added with Node_Create_Request like any other entity
                //case KeyCommon.Messages.Enumerations.AddLight:
                //    msg = new KeyCommon.Messages.Scene_LoadLight();
                //    msg.Read(buffer);
                //    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                //    break;
                //case KeyCommon.Messages.Enumerations.AddNode: // add/import node
                //    Debug.WriteLine("LoopbackServer.MessageProc() - Command 0.");

                //    // Core._Core.SceneManager.ActiveScene 
                //    break;
                case KeyCommon.Messages.Enumerations.Node_MoveChildOrder :
                    msg = new Node_MoveChildOrder();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Node_InsertUnderNew_Request:
                    msg = new Node_InsertUnderNew_Request();
                    msg.Read(buffer);

                    string parent = ((Node_InsertUnderNew_Request)msg).ParentID;
                    string nodeType = ((Node_InsertUnderNew_Request)msg).NodeType;

                    // use the factory to create the node type
                    string rootID = Repository.GetNewName(typeof(object));
                    string previous = ((Node_InsertUnderNew_Request)msg).NodeID;

                    // create a node insert response
                    KeyCommon.Messages.Node_InsertUnderNew insert = new KeyCommon.Messages.Node_InsertUnderNew(parent, rootID, nodeType, previous);
                    connection.SendMessage(insert, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Node_RenameResource:
                    msg = new KeyCommon.Messages.Node_RenameResource();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Node_ReplaceResource:
                    msg = new KeyCommon.Messages.Node_ReplaceResource();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Node_ChangeParent:
                    msg = new KeyCommon.Messages.Node_ChangeParent();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.NodeChangeState:
                    msg = new KeyCommon.Messages.Node_ChangeProperty();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Geometry_CreateGroup:
                    msg = new KeyCommon.Messages.Geometry_CreateGroup();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Geometry_RemoveGroup:
                    msg = new KeyCommon.Messages.Geometry_RemoveGroup();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Geometry_ChangeProperty:
                    msg = new KeyCommon.Messages.Geometrty_ChangeGroupProperty();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Entity_GetCustomProperties:
                    msg = new KeyCommon.Messages.Entity_GetCustomProperties();
                    msg.Read(buffer);

                    KeyCommon.Messages.Entity_GetCustomProperties getRequest = (KeyCommon.Messages.Entity_GetCustomProperties)msg;

                    // TODO: is user authorized to retreive this information about this entity?

                    // get the entity from repository
                    Node node3 = (Node)Keystone.Resource.Repository.Get(getRequest.EntityID);
                    if (node3 == null) throw new Exception();
                    bool loadedFromDB = node3 == null;
                    // if null, get from xmldb
                    if (loadedFromDB)
                    {
                        XMLDatabase xmldb = new XMLDatabase();
                        SceneInfo info = xmldb.Open(getRequest.SceneName, true);
                        bool clone = false;
                        bool recurse = false;
                        bool delayLoading = true;
                        // note: custom properties are guaranteed to be loaded with the entity
                        // because the entity's script is always loaded on the entity's own LoadTVResource() call.

                        // TODO: why are we attempting to read this from our xml database?  
                        // is it because we want the properties potentially and dont mind that the
                        // values are all default?
                        // I think it's because our GUI needs to do this where potentially the object isnt instanced
                        // but must lay in our mod db somewhere and all we care about is populating the
                        // interface drop downs adn what not.
                        node3 = (Node)xmldb.ReadSynchronous(getRequest.EntityTypeName, getRequest.EntityID, recurse, clone, null, delayLoading, false);
                        Keystone.IO.PagerBase.LoadTVResource (node3, recurse);

                        //// find the DomainObject - OBSOLETE - scripts are now loaded on Entity.LoadTVResource()
                        //Keystone.Elements.Node[] children = xmldb.ReadSynchronous(getRequest.EntityTypeName, getRequest.EntityID,
                        //           false, false, true); // we delay loading in general because we can still manually cause our DomainObject to load
                        
                        //if (children != null)
                        //{
                        //    for (int i = 0; i < children.Length; i++)
                        //        if (children[i] is DomainObject)
                        //        {
                        //            SuperSetter setter = new SuperSetter(node3);
                        //            setter.Apply(children[i]);
                        //            ((IPageableTVNode)children[i]).LoadTVResource();

                        //            // TODO: this is problematic, ideally we also want to know 
                        //            //       info about it's children... what other child Entities are there?
                        //            //       I think "Entities" would be the limit then clicking those entities
                        //            //       can give us more detail about just those entities.

                        //            //xmldb.WriteSychronous 
                        //            break;
                        //        }
                        //}
                        // remove all temporary node instances from repository

                    }

                    // return just the properties.  There is an implicit expectation that if a client
                    // can request custom properties for an entity, then that entity must exist 
                    // client side either in db or in repository.  However, how much information
                    // they actually have is not clear because it can vary depending on how
                    // well their sensors are for instance.  But in any case, we do not need to
                    // send a "CreateNode" response because the client has to already have it.
                    // Instead we only need to send the cusotmproperties
                    PropertySpec[] customProperties = ((Entity)node3).GetCustomProperties(false);
                    Entity_SetCustomProperties setCustom = new Entity_SetCustomProperties(customProperties);
                    setCustom.EntityID = node3.ID;
                    setCustom.EntityTypeName = node3.TypeName;
                    if (loadedFromDB)
                    {
                        Repository.IncrementRef(node3);
                        Repository.DecrementRef(node3);
                    }
                    connection.SendMessage(setCustom, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Entity_ChangeCustomPropertyValue: // changes to nodes or the scene are to be viewed as requests
                    msg = new KeyCommon.Messages.Entity_ChangeCustomPropertyValue(); // thus we must validate the request then return comfirmation or fail along with fail codes if applicable
                    // a message that contains multipel commands in a single request must have all commands succeed or else all fail.
                    msg.Read(buffer);

                    KeyCommon.Messages.Entity_ChangeCustomPropertyValue change = (KeyCommon.Messages.Entity_ChangeCustomPropertyValue)msg;
                    Node node4 = (Node)Keystone.Resource.Repository.Get(change.EntityID);


                    // TODO: the actual apply of these properties to the node
                    // should not occur in threaded workers, only in single threaded ocmmandcompleted?
                    // I think that's probably true because with multiple worker threads we'd have to sychronize
                    // changing of properties.
                    int[] errorCodes = null;
                    //if (node4 is Entity)
                    //    ((Entity)node4).SetCustomPropertyValues(change.CustomProperties, true, true, out errorCodes);
                    if (node4 is GroupAttribute)
                        ((GroupAttribute)node4).SetShaderParameterValues(change.CustomProperties);
                    //else
                    //    throw new Exception("LoopbackServer.MessageProc() - Unexpected type '" + node4.TypeName + "'");

                    long uuid = change.UUID;

                    // if success, send a success response
                    if (errorCodes == null || errorCodes.Length == 0)
                    {
                        msg = new KeyCommon.Messages.CommandSuccess(uuid);
                    }
                    else
                    {
                        // else send fail and error codes
                        msg = new KeyCommon.Messages.CommandFail(uuid);
                    }


                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Geometry_ResetTransform:
                    msg = new KeyCommon.Messages.Geometry_ResetTransform();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.NodeChangeFlag:
                    msg = new KeyCommon.Messages.Node_ChangeFlag();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.GameObject_Create_Request :
                    msg = new KeyCommon.Messages.GameObject_Create_Request();
                    msg.Read(buffer);

                    string typeName = ((KeyCommon.Messages.GameObject_Create_Request)msg).GameObjectType;
                    string owner = ((KeyCommon.Messages.GameObject_Create_Request)msg).OwnerID;
                    Settings.PropertySpec[] properties = ((KeyCommon.Messages.GameObject_Create_Request)msg).Properties;

                // todo: we dont want Game to have to refer to Repository.Get().  Instead we want to pass in all parameters it needs to compute results of applying game rules.
                //       Most properties that need to be used for rules results are customProperties that may contain types that are defined in Game01.
                //       I probably need a GameAPI since currently EXEs do have access to Keystone.dll and NetClient and LoopbackServer.  
                //       Well certain game userTypes might need to be accessible to scripts since they can be stored in custom properties
                //    AppMain.Game.UserMessageReceivedLoopback();

                    // verify this ownerID is good
                    Entity ownerEntity = (Entity)Repository.Get(owner);

                    // NOTE: we create game object here server side so the GUID is same across all clients
                    // create a new game object of this type, add it to the owner (eg vehicle)
                    KeyCommon.DatabaseEntities.GameObject gameObject = Game01.GameObjects.Factory.Create(typeName);
                    gameObject.SetProperties(properties);
                    // create a GameObject_Create message for the response 
                    GameObject_Create createGO = new GameObject_Create(ownerEntity.ID, gameObject);
                    
                    connection.SendMessage(createGO, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.GameObject_ChangeProperties:
                    msg = new KeyCommon.Messages.GameObject_ChangeProperties();
                    msg.Read(buffer);
                    connection.SendMessage(msg, NetChannel.ReliableInOrder1);
                    break;
                case KeyCommon.Messages.Enumerations.Task_Create_Request:
                    msg = new Game01.Messages.Task_Create_Request();
                    msg.Read(buffer);

                    Game01.Messages.Task_Create_Request taskCreateRequest = (Game01.Messages.Task_Create_Request)msg;
                    ProcessTaskRequest(taskCreateRequest.Request, connection);
                    break;
                default:
                    Debug.WriteLine("FormMain.LoopbackServer.MessageProc() - Unexpected game command from User.");
                    break;
            }
        }

        #region database functions
        private struct UserRecord
        {
            public string UserName;
        }

        private SQLiteConnection GetConnection()
        {
            SQLiteConnection connect = new SQLiteConnection(@"Data Source=" + AppMain.mLoopBackServerDatabaseFullPath + ";Version=3;");
            connect.Open();
            return connect;
        }

        private void CreateLoopbackServerDatabase()
        {
            System.Data.SQLite.SQLiteConnection connect = GetConnection();

            //  users (does this include AI controlled vehicles?)
            string sql = "create table users (username varchar(16))";
            System.Data.SQLite.SQLiteCommand command = new System.Data.SQLite.SQLiteCommand(sql, connect);
            command.ExecuteNonQuery();
            connect.Close();
        }

        private void CreateUserRecord(string userName)
        {
            using (SQLiteConnection connect = GetConnection())
            {
                SQLiteCommand insertSQL = new SQLiteCommand("INSERT INTO [users] ([username]) VALUES (@username)", connect);
                insertSQL.Parameters.Add(new SQLiteParameter(@"username", userName));

                try
                {
                    insertSQL.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception("FormMainLoopbackServer.CreateUserRecord(): ERROR - " + ex.Message);
                }
            }
        }
        private UserRecord GetUserRecord(string name)
        {
            using (SQLiteConnection connect = GetConnection())
            {
                using (SQLiteCommand fmd = connect.CreateCommand())
                {
                    fmd.CommandText = @"SELECT username FROM users";
                    fmd.CommandType = System.Data.CommandType.Text;

                    SQLiteDataReader r = fmd.ExecuteReader();

                    r.Read();

                    UserRecord record;
                    record.UserName = Convert.ToString(r["username"]);
                    //record.RegionID = Convert.ToString(r["regionid"]);
                    return record;
                }
            }

            throw new Exception("FormMainLoopbackServer.GetUserRecord() - Record not found.");
        }

        #endregion

        private void GetNonShareableNodeIDs(Node node, ref System.Collections.Generic.Queue<string> ids)
        {
            if (node == null) return;
            // WARNING: Child order (indeed the order of the entire hierachy is IMORTANT! 
            //          The SceneReader reads in depth-first traversal.
            //          i suppose if for some reason the user wants to re-order child node, its ok as long as its done after initial load and 
            //          validation.  However, if the server and client re-save the entity, the IDs on next load wont match unless the server 
            //          also performs the same re-order prior to any savce.

            // WARNING: Interior floor and ceiling visuals are NOT serialized so we skip the Interior.Model (which is actually a ModelSequence)
            //          entirely.  For now the test is node.Serializable, but we should have a dedicated flag for that indicating the ID
            //          is already guaranteed to be sychronized because they are based on the Interior.ID + naming convention. And potentially,
            //          the server wont even need to generate that ModelSequence and it's sub-tree because it's all just client side visuals.
            if (node.Shareable == false && node.Serializable == true)
            {
                System.Diagnostics.Debug.WriteLine("Enqueing ID " + node.ID + " for typename = " + node.TypeName + " at index = " + (ids.Count - 1).ToString());
                ids.Enqueue(node.ID);
            }

            // recursive depth-first traversal 
            if (node is IGroup && node.Serializable == true)
            {
                IGroup group = (IGroup)node;
                for (int i = 0; i < group.ChildCount; i++)
                {
                    GetNonShareableNodeIDs(group.Children[i], ref ids);
                }
            }
        }

       

        // todo: maybe just be even more specific ValidateInteriorEntityPosition since this is only designed for moving static Entities/Components that have footprint data
        private bool ValidateEntityPosition(Entity_Move move)
        {
            bool result = true;
            Keystone.Portals.Interior parent = (Keystone.Portals.Interior)Repository.Get(move.mParentID);
            Entity[] children = new Entity[move.mTargetIDs.Length];
            Vector3d[] previousPositions = new Vector3d[children.Length];

            for (int i = 0; i < children.Length; i++)
            {
                children[i] = (Entity)Repository.Get(move.mTargetIDs[i]);
                previousPositions[i] = children[i].Translation;
                System.Diagnostics.Debug.Assert(children[i].Parent == parent);
            }

            // todo: do we need to remove then validate then re-add the node? maybe we can temporarily clear the footprint... but we'd need to clear all of them and then if cancel restore them.
            for (int i = 0; i < children.Length; i++)
            {
                Repository.IncrementRef(children[i]);
                parent.RemoveChild(children[i]);
            }


            for (int i = 0; i < children.Length; i++)
            {
                System.Diagnostics.Debug.Assert(children[i].Footprint != null);
                int[,] footprint = children[i].Footprint.Data;
                int[,] destFootprint;

                if (!parent.IsChildPlaceableWithinInterior(footprint, move.Positions[i], children[i].Rotation, out destFootprint))
                {
                    result = false;
                    break;
                }
            }

            // based on result, we add the child back with its original position or the new position
            for (int i = 0; i < children.Length; i++)
            {
                if (result)
                    children[i].Translation = move.Positions[i];
                // setter isn't necessary in this case because parent is already cast as Interior
                parent.AddChild(children[i]);
                Repository.DecrementRef(children[i]);
            }
            

            return result;
        }

        // todo: this spawncharacters() is invalid because the Node.IDs are not going to be the same across server and client.  It just doesn't matter for loopback but it should still be fixed
        private void SpawnCharacters(string parentID, Database.AppDatabaseHelper.CharacterRecord[] characterRecord, Vector3d[] characterPositions, NetConnectionBase connection)
        {
            // TODO: should serializable == false so that they dont get saved to the \\scenes\\ xmldb?
            // todo: in fact, we can do it during Spawn creation message server side

            if (characterRecord == null || characterRecord.Length == 0) return;
            for (int i = 0; i < characterRecord.Length; i++)
            {
                Simulation_Spawn spawn = new Simulation_Spawn();
                spawn.EntitySaveRelativePath = characterRecord[i].RelativeEntitySavePath;
                spawn.Translation = characterPositions[i];
                spawn.ParentID = parentID;
                spawn.CellDBData = null;
                //spawn.EntityFileData = null;

                connection.SendMessage(spawn, NetChannel.ReliableInOrder1);
            }
        }

        

        /// <summary>
        /// Process a Task REQUEST and if validated, return an ORDER create message so the client can
        /// execute the validated task.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="connection"></param>
        private void ProcessTaskRequest(Game01.Messages.OrderRequest request, NetConnectionBase connection)
        {
            //// store the request in a server side client requests table
            //// What is the purpose of storing both requests and commands in tables in the db?
            //// Client side i know that our scripts may need to retreive current tasks... but we don't want to
            //// to that per frame.  We need to cache them in the Entity custom properties.
            //// Hrm.  

            //// validate the request, create a command for the "dumb" client to execute
            //switch (request.Task)
            //{
            //    default:
            //        break;
            //}



            //// then client side, we have a seperate table for client side "approved by server" orders/tasks 
            //long recordID = 0;

            //// do we create maneuvers here so we can perform them and know where the client ship should be at any point in time?
            //// in other words, we can prevent cheating
            //// And what about our Production/Consumption paradigm?  That is all done through client side scripts
            //// do we server side, just worry about making sure the "results" of these operations are in sync with
            //// server side calculations including things like damage in case client tries to inflate damage and speed and such?
            //// If we were a real multiplayer server and running production/consumption scripts like the client, how do we
            //// validate certain state and keep synchronization with server side script results?  Well first of all,
            //// in a real multiplayer server, we are running scripts, testing for conditions such as "destroyed" or
            //// "firing", or "thrusting" and then sending updates to all clients.  So the mechanism to validate
            //// is one of validating that an order can be performed, when it's performed, and any results.


            //// How we handle this determines whether we keep trying to keep the game open for multiplayer in v2 or we just
            //// focus on single player only and worry about fixing multiplayer for v2 when the time comes.

            //// FOR NOW, forget validation and such.  Let's get our ship oriented towards the next waypoint.
            //// - create server table for orders and add this new order to it
            //// - create maneuvers that are required to orient the ship
            //// - there are 2 types of maneuvers we can implement
            ////   - a physical / thrust based axial 
            ////      - for this we have to calculate how long to thrust to reach halfway point
            ////        then thrust in opposite direction to stop at desired orientation
            ////      - for this we use a Helm gameobject to store Euler rotations since we can compute the euler halfway point easily.
            ////   - a quaternion slerp based axial maneuver
            //Game01.GameObjects.Order task = new Game01.GameObjects.Order(recordID);
            //task.AssignedByID = request.AssignedByID;
            //task.AssignedDateTime = request.AssignedDateTime;
            //task.Task = request.Task;
            //task.Status = 0;
            //task.Args = request.Args;
            //task.Notes1 = request.Notes1;

            ////task.Owner = request.Owner;

            //// if validated add the task to the server side task table

            //Database.AppDatabaseHelper.CreateTaskRecordServer(task, Database.AppDatabaseHelper.GetConnection());


            //Game01.Messages.Task_Create msg = new Game01.Messages.Task_Create();
            //msg.Order = task;
            //// respond with a Task_Create (as opposed to Task_Create_Request)
            //connection.SendMessage(msg, NetChannel.ReliableInOrder1);
        }


        ///<summary>
        /// Behaves as if this loopback server were actually a remote multiplayer server with many 
        /// connected clients.  It looks for the specified user connection and sends the msg specified
        /// in the parameters.
        ///</summary>
        ///<remarks> 
        /// See FormMainBase.SendNetMessage() for the common client network message routine.
        /// Internally it calls AppMain.mNetworkClient.SendMessage()
        ///</remarks>
        public void ServerSendMessage(string user, KeyCommon.Messages.MessageBase msg)
        {
            // NOTE: If the message we're sending exceeds the client's receive buffer size, the client will gracefully disconnect.
            // NOTE: This size includes the message header info that precedes every MessageBase based message.
            // NOTE: This prevents a bad client from taking down the server.

            NetConnectionBase connection = null;
            UserRecord ur = GetUserRecord(user);

            for (int i = 0; i < AppMain.mLoopbackServer.Server.Connections.Count; i++)
                if (((UserRecord)AppMain.mLoopbackServer.Server.Connections[i].Tag).UserName == user)
                {
                    connection = AppMain.mLoopbackServer.Server.Connections[i];
                    break;
                }

            if (connection != null)
                connection.SendMessage(msg, NetChannel.ReliableInOrder1);
            else throw new Exception("LoopbackServer.SendMessage() - Connection for user '" + user + "' not found.");

        }

    }
}
