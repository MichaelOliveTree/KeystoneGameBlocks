using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using Keystone.Commands;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Types ;
using Keystone.Appearance;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Scene;
using Keystone.Celestial;
using Keystone.Portals;
using Keystone.Traversers;
using Keystone.Workspaces;
using Lidgren.Network;
using System.Collections.Generic;


namespace KeyEdit
{
    partial class FormMain : FormMainBase
    {
		    private Keystone.TileMap.StructureVoxels GenerateVoxelTerrain(int x, int y, int z, double voxelSizeY, int minFloor, int maxFloor, int maxFloorCount, int octreeDepth)
        {
            // NOTE: structure ID MUST be based on zone ID 
            string id = Keystone.TileMap.StructureVoxels.GetStructureID(x, y, z);
            Keystone.TileMap.StructureVoxels voxelStructure = (Keystone.TileMap.StructureVoxels)Repository.Create(id, "StructureVoxels");
            voxelStructure.SetProperty("floorheight", typeof(double), voxelSizeY);
            voxelStructure.SetProperty("minfloor", typeof(int), minFloor);
            voxelStructure.SetProperty("maxfloor", typeof(int), maxFloor);
            voxelStructure.SetProperty("maxfloorcount", typeof(int), maxFloorCount);
            voxelStructure.SetProperty("octreedepth", typeof(uint), octreeDepth);



            // ----------------------------------------------
            // DEBUG TEMP - GENERATE LEVEL VARS
            //int zoneTileWidth = (int)newTerrainScene.RegionResolutionX;
            //int zoneTileDepth = (int)newTerrainScene.RegionResolutionZ;
            //double floorHeight = newTerrainScene.TileSizeY;
            //string persistDimensions = newTerrainScene.RegionDiameterX.ToString() + "," +
            //                           floorHeight.ToString() + "," +
            //                           newTerrainScene.RegionDiameterZ.ToString() + ",";
            string levelsPersistString;
            int numLevels;


            return voxelStructure;
        }


        private Keystone.TileMap.Structure GenerateIsometricTerrain(int x, int y, int z, float regionDiameterX, float regionDiameterZ)
        {
            // Zone will have a visible tile based structure such as floors, walls and ceilings.
            // The structure is regarded as part of the Region and not as seperate entities.
            // This is for performance (rendering and memory consumption) primarily.
            // Since having seperate entities for every Tile in the 

            // NOTE: structure ID MUST be based on zone ID 
            string id = Keystone.TileMap.Structure.GetStructureID(x, y, z);
            Keystone.TileMap.Structure structure = (Keystone.TileMap.Structure)Repository.Create(id, "Structure");
            //structure.SetProperty("floorheight", typeof(double), (double)newTerrainScene.TileSizeY);
            //structure.SetProperty("minfloor", typeof(int), newTerrainScene.MinimumFloor);
            //structure.SetProperty("maxfloor", typeof(int), newTerrainScene.MaximumFloor);
            //structure.SetProperty("maxfloorcount", typeof(int), (int)newTerrainScene.TerrainTileCountY);



            // ----------------------------------------------
            // DEBUG TEMP - GENERATE LEVEL VARS
            int zoneTileWidth = 32; // (int)newTerrainScene.RegionResolutionX;
            int zoneTileDepth = 32; // (int)newTerrainScene.RegionResolutionZ;
            double floorHeight = 2.82983f; // newTerrainScene.TileSizeY;
            string persistDimensions = regionDiameterX.ToString() + "," +
                                       floorHeight.ToString() + "," +
                                       regionDiameterZ.ToString() + ",";
            string levelsPersistString;
            int numLevels;

            // ----------------------------------------------
            // DEBUG TEMP - GENERATE DEFAULT STRUCTURE LEVELS
            // Index 0 = Floor Level -1 = underground
            // Index 1 = Floor Level 0 = above ground
            // Index 2 = Floor Level 1 = air above 
            //	- when placing items upon Level2 (and on top of Level 1) a new air Level gets added
            //    as Level 3, and Level 2 items will write to obstacle layer of Level 1
            // since we deduce filepaths from layer name, all we need is the name of the layer and it's level
            string[] layerNames = new string[] { "obstacles", "layout", "style" };
            const int TERRAIN_SEGMENT_INDEX = 1;
            byte segmentIndex = TERRAIN_SEGMENT_INDEX;

            for (int floorLevel = -1; floorLevel <= 0; floorLevel++)
            {
                // begin temp: create these bitmaps and save to disk.... seed with random values from 0 - N for now
                for (int n = 0; n < layerNames.Length; n++)
                {
                    int initializationValue = 0;
                    if (layerNames[n] == "obstacles")
                    {
                        initializationValue = 0; // segments placed on this LEVEL affect obstacle map of the level BELOW it!
                    }
                    else if (layerNames[n] == "layout")
                    {
                        if (floorLevel == -1)
                            initializationValue = segmentIndex;
                        else
                            initializationValue = 0; // 0 == null empty segment
                    }
                    else if (layerNames[n] == "style") // "style"
                    {
                        // style has to be discovered during autotile
                        initializationValue = -1;
                    }

                    ProceduralHelper.InitializeMapLayerBitmap(layerNames[n], floorLevel, x, z, zoneTileWidth, zoneTileDepth, initializationValue);
                }
            }
            numLevels = 2;
            // format: numLevels, worldDimensions, {floorLevel, numLayers, layerNames{}}
            levelsPersistString = numLevels + "," + persistDimensions + "-1,3,obstacles,layout,style,0,3,obstacles,layout,style";
            //						        	// END TEMP - GENERATE DEFAULT STRUCTURE LEVELS
            // ----------------------------------------------

            //// BEGIN TEMP - GENERATE PROCEDURAL BASED LEVEL DATA
            //// ----------------------------------------------

            //// - for this zone, determine range of floor levels we need to generate based on altitude
            ////   of the terrain. (NOTE: subterranian caverns not generated yet)
            ////   - note: unlike above where each level has same initializationValue, here
            ////     visible levels will have varying initialization values for each x,z tile location based on
            ////     whether terrain exists there or not.
            //int seed = 0;
            //int minFloorLevel, maxFloorLevel;
            //numLevels = ProceduralHelper.GenerateMapLayerBitmap(seed, 
            //                                                    x,  z,
            //                                                    zoneTileWidth, zoneTileDepth, 
            //                                                    structureLevelsHigh, newTerrainScene.MinimumFloor, newTerrainScene.MaximumFloor - 1, 
            //                                                    out minFloorLevel, out maxFloorLevel);

            //levelsPersistString = numLevels + "," + persistDimensions;

            //string delimitedText = null;
            //for (int i = minFloorLevel; i <= maxFloorLevel; i++)
            //{
            //	if (string.IsNullOrEmpty(delimitedText) == false)
            //		delimitedText +=",";

            //	delimitedText += i + ",3,obstacles,layout,style";
            //}

            //levelsPersistString += delimitedText;
            //// ----------------------------------------------
            //// END TEMP - GENERATE PROCEDURAL BASED LEVEL DATA


            // TODO: this persist string after structure.SetProperty ("maplevels"...) is being ignored.  Actually, I think it's being
            //       overwritten by another persist string that is computed because no actual Levels and Layers are created being added to Structure!  
            //       What we're trying to do is generate the save file without having to load the level and serialize the xml.
            //       So the question then is, can we override the overwriting of the persist string so that we do not need to use
            //       the hackish "persistPath" file.
            //       WAIT: The other reason we use persistPath is so we can load MapLayer's in Pager without needing to load in Zones
            //       or their Structures and that way when we do load structures, the AutoTile will work across Zones because the MapLayer
            //       will be loaded already so we can see what types of segments exist in relevant adjacent tiles across zone boundaries.
            structure.SetProperty("maplevels", typeof(string), levelsPersistString); // <- is being overwritten by computed persist string during structure.PersistFloorLevels()

            // We need to update the actual persist file with this string for the current structure because
            // assigning "maplevels" property above is not working.
            string persistPath = Keystone.TileMap.Structure.GetLayersDataPath(x, z);
            System.IO.File.WriteAllText(persistPath, levelsPersistString);

            // hard coded default dirt segment
            string[] modelLookupPaths = new string[] { AppMain.ModName + @"\meshes\terrain\dirt.kgbsegment" };
            // Add a default segment to go with the default floor i've painted in the layout above
            structure.SetProperty("modellookuppaths", typeof(string[]), modelLookupPaths);

            // domain objects (aka entity scripts) are assigned via entity.ResourcePath
            //        	string scriptPath = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\scripts\tile_structure.css";
            //        	structure.ResourcePath = scriptPath;

            return structure;
        }

        private ModeledEntity GenerateTVLandscapeTerrain()
        {
            // TVLandscape based terrain
            // create terrain Entity and add to zone
            // TODO: terrain entity names should be generated by server and sent back to client to use
            string terrainEntityID = Repository.GetNewName(typeof(ModeledEntity));
            ModeledEntity newTerrain = new ModeledEntity(terrainEntityID);
            Model model = new Model(Repository.GetNewName(typeof(Model)));
            string geometryID = Repository.GetNewName(typeof(Terrain));
            Terrain terrainGeometry = (Terrain)Repository.Create(geometryID, "Terrain");
            terrainGeometry.SetProperty("heightmap", typeof(string), null); // empty default terrain 
                                                                            // force loading of terrainGeometry resource since this is already in Worker thread
                                                                            // TODO: i don't think loading of the terrain is necessary here since it actually doesn't need to be rendered here.
                                                                            //       that will occur when this generated terrain scene XML is read in and rebuilt.
                                                                            //terrainGeometry.LoadTVResource ();

            // splatting appearance
            string appearanceID = Repository.GetNewName(typeof(SplatAppearance));
            SplatAppearance appearance = new SplatAppearance(appearanceID);
            // we'll use single group that is same for all chunks
            Material material = Material.Create(Material.DefaultMaterials.matte);
            appearance.AddChild(material);

            // TODO: this path is just a resource path to find textures, it's not a mod path at all
            string path = System.IO.Path.Combine(AppMain._core.ModsPath, "terrain");

            string texturePath1 = System.IO.Path.Combine(path, "grass1.png");
            string texturePath2 = System.IO.Path.Combine(path, "rock 6.png");
            string texturePath3 = System.IO.Path.Combine(path, "dirt 1.png");
            string texturePath4 = System.IO.Path.Combine(path, "snow 1.png");
            string alphaPath = "";     // we will be autogenerating the values contained in our alpha map using our AutoUpdateOpacityMap() method


            Keystone.Appearance.SplatAlpha splatLayer = (SplatAlpha)Keystone.Resource.Repository.Create("SplatAlpha");
            Keystone.Appearance.Texture tex = (Texture)Keystone.Resource.Repository.Create(texturePath1, "Texture");
            tex.TextureType = Texture.TEXTURETYPE.Default;
            splatLayer.AddChild(tex);
            appearance.AddChild(splatLayer);
            //appearance.AddDefine("DIFFUSEMAP", null);

            splatLayer = (SplatAlpha)Keystone.Resource.Repository.Create("SplatAlpha");
            tex = (Texture)Keystone.Resource.Repository.Create(texturePath2, "Texture");
            tex.TextureType = Texture.TEXTURETYPE.Default;
            splatLayer.AddChild(tex);
            appearance.AddChild(splatLayer);

            splatLayer = (SplatAlpha)Keystone.Resource.Repository.Create("SplatAlpha");
            tex = (Texture)Keystone.Resource.Repository.Create(texturePath3, "Texture");
            tex.TextureType = Texture.TEXTURETYPE.Default;
            splatLayer.AddChild(tex);
            appearance.AddChild(splatLayer);

            splatLayer = (SplatAlpha)Keystone.Resource.Repository.Create("SplatAlpha");
            tex = (Texture)Keystone.Resource.Repository.Create(texturePath4, "Texture");
            tex.TextureType = Texture.TEXTURETYPE.Default;
            splatLayer.AddChild(tex);
            appearance.AddChild(splatLayer);


            model.AddChild(appearance);
            model.AddChild(terrainGeometry);
            newTerrain.AddChild(model);


            return newTerrain;
        }
        
        
        // NOTE: Generating a new scene is NOT the same thing as LOADING a scene. For instance, no scene is added to scenemanager when generating
        private void GenerateNewUniverse(KeyCommon.Messages.Scene_NewUniverse newUniverse)
        {
            // TODO: i should be using seperate seed for each stellar system, star, world, asteroid field rather than
            // passing a _random.  This is because we cannot create the individual system, star, world, etc without having
            // a seed for each.  
            // TODO: so i believe to do this is to use the _random created with the first seed, to actually generate a seed to use
            // in the next call.  This way that seed value can be stored, and this way that star,world,etc can be restored from just
            // the initial seed value.
            Keystone.Utilities.XXHash hash = new Keystone.Utilities.XXHash(newUniverse.RandomSeed);
            Random _random = new Random(newUniverse.RandomSeed); // new Random((int)hash.GetHash(new int[] { 0, 0, 0 }));
            // NOTE: we generate all star systems and stars because they're needed for starmap and navigation screen
            StellarSystemGenerator _systemGen = new StellarSystemGenerator(newUniverse.RandomSeed);
            // TODO: generate worlds and moons only as needed. page in and out as required, but i think actually, this is what Zones are for.
            GenerateWorld _worldGen = new GenerateWorld(_random);
            GenerateMoon _moonGen = new GenerateMoon(_random);
            List<StellarSystem> _systems = new List<StellarSystem>();

            uint octreeDepth = uint.Parse(AppMain._core.Settings.settingRead("scene", "octreedepth"));

            try
            {
                // Feb.29.2024 - Disabling the pager does NOT prevent loading of Entity scritps since we need to have Scripts loaded if we want to save CustomProperties
                 ClientPager.Disabled = true;

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                // TODO: we should get the root node name from the returned newUniverse message from server.
                //       Then we can create the child zones ourselves because those are named based off the root's name
                System.Diagnostics.Debug.Assert(System.IO.Directory.Exists(System.IO.Path.Combine(AppMain.SCENES_PATH, newUniverse.FolderName)));
                string sceneName = System.IO.Path.GetFileName(newUniverse.FolderName);
                AppMain.CURRENT_SCENE_NAME = sceneName;

                Keystone.Portals.ZoneRoot root;
                Keystone.Scene.SceneInfo info;
                Keystone.IO.XMLDatabase xmldb;

                // Create new Scene XML DB and Viewpoints
                AppMain._core.SceneManager.CreateNewSceneDatabase(newUniverse.FolderName, sceneName,
                                                                  Keystone.Scene.SceneType.MultiReginSpaceStarsAndWorlds,
                                                                  newUniverse.RegionsAcross, newUniverse.RegionsHigh, newUniverse.RegionsDeep,
                                                                  newUniverse.RegionDiameterX, newUniverse.RegionDiameterY, newUniverse.RegionDiameterZ,
                                                                  newUniverse.SerializeEmptyZones,
                                                                  0,
                                                                  0,
                                                                  out root,
                                                                  out info,
                                                                 out xmldb);

                PagerBase.Disabled = true;

                if (newUniverse.CreateStarDigest) // todo: add checkbox for GenerateStarfield
                {
                    int[] starCount = new int[] { 5000, 1000, 100 };
                    int[] colors = new int[]{Keystone.Utilities.RandomHelper.RandomColor().ToInt32(),
                                     Keystone.Utilities.RandomHelper.RandomColor().ToInt32(),
                                     Keystone.Utilities.RandomHelper.RandomColor().ToInt32()};

                    float variance = 1.0f;
                    //float[] spriteSize = new float[] {500 + (500 * (float)rand.NextDouble()),
                    //            500 + (500 * (float)rand.NextDouble()),
                    //            500 + (500 * (float)rand.NextDouble())};
                    float[] spriteSize = new float[] { 250, 500, 1000 };

                    float radius = 90000;

                    string[] texture = new string[] { @"caesar\Shaders\Planet\stardx7.png", @"caesar\Shaders\Planet\stardx7.png", @"caesar\Shaders\Planet\stardx7.png" }; // star2.dds";
                    string fieldName = "starfield_" + Repository.GetNewName(typeof(Entity)); // "starfield1"  // random name means we should be able to produce more than one

                    Entity field = Keystone.Celestial.ProceduralHelper.CreateRandomStarField(fieldName, texture, radius, starCount, spriteSize, colors);
                    field.Name = "starfield";
                    field.Translation = Vector3d.Zero(); // TODO: isn't this pos irrelevant as it follows camera?

                    root.AddChild(field);
                }

                // create and write to disk the regions we will need to pass to the universe gen
                Keystone.Portals.Zone[,,] regions = null;
                if (info.SerializeEmptyZones)
                    regions = Keystone.Portals.Zone.Create(root, octreeDepth, xmldb);

                // this routine essentially creates a cube shaped galaxy.  It can be made
                // to be more irregular by haing the Z level go to rnd(min) to rnd(maxDiameter)
                // it takes diameter of our galaxy and then begins to plot stars
                // Basically it starts at coordinates 1,1,1 and then goes to 1,1,2 then 1,1,3, etc
                // til it eventually reaches Diameter,Diameter,Diameter
                // so the maximum number of stars we can have in our system is Diameter^3
                // The actual diameter of our galaxy in lightyears is the MinimumSystemSeperation * Diameter.
                // So if the minimum seperation (as set by the user) is 2 light years then
                // our diamter in light years is 2 * Diameter.  So if the diameter is 10 then our cube is
                // actually 20 light years across and can hold at most 1000 (or 10^3) star systems.
                // Incidentally, Generating 1000 star systems as detailed as we are doing will take quite
                // a while, but fortunately its jsut for creating the initial map.

                // //note that the user is setting the minimum distance between systems in LightYears
                //   but we calculate all of our locations in AU so we must convert
                int width = (int)root.RegionsAcross;
                int height = (int)root.RegionsHigh;
                int depth = (int)root.RegionsDeep;

                int totalZoneCount = width * height * depth;
                                
                System.Diagnostics.Debug.WriteLine("-- {0} -- ZONE GENERATION BEGINNING", width * height * depth);
                

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        for (int k = 0; k < depth; k++)
                        {
                            Zone zone = null;
                            if (info.SerializeEmptyZones)
                                zone = regions[i, j, k]; 

                            if (newUniverse.Mode != KeyCommon.Messages.Scene_NewUniverse.CreationMode.Empty)
                            {
                                KeyCommon.Messages.UniverseCreationParams celestialParams = newUniverse.mParams;
                                // systems gets added and written to disk and then the region is unloaded and we move to the next
                                if (totalZoneCount == 1 || ShouldSystemGoHere(celestialParams.ClusterDensity, _random))
                                {
                                    // TODO: for empty Zones, shouldn't we still have a default DirectionalLight? or we could add one when the Zone is paged in.
                                    // if we are NOT serializing empty zones, the current Zone will not have
                                    // been created above and so be NULL here. So we will need to create the zone instance obviously
                                    // BEFORE WE CAN ADD A STELLAR SYSTEM TO IT
                                    if (info.SerializeEmptyZones == false)
                                    {
                                        // NOTE: Here even though client side we are creating the child zone off root
                                        //       we can compute the child's name because it's based off the root Zone's name
                                        string name = root.GetZoneName(i, j, k);

                                        BoundingBox box = root.GetChildZoneSize();
                                        float offsetX = root.StartX + i;
                                        float offsetY = root.StartY + j;
                                        float offsetZ = root.StartZ + k;

                                        zone = new Zone(name, box, octreeDepth, i, j, k, offsetX, offsetY, offsetZ);
                                    }

                                    StellarSystem newsystem;
                                    // TODO: we should have a flag in the newUniverse command for AddSolSystem = true
                                   
                                    int centerX, centerY, centerZ;
                                    root.GetZoneCenterSubscripts(out centerX, out centerY, out centerZ);
                                    // todo: we should add a default viewpoint to the SceneInfo pointing to this zone as well.
                                    if (totalZoneCount == 1 || (i == centerX && j == centerY && k == centerZ))
                                        newsystem = Keystone.Celestial.ProceduralHelper.GenerateSolSystem(new Vector3d(0, 0, 0), newUniverse.RandomSeed);
                                    else
                                    {
                                        // create new stellar system and add to zone
                                        uint starCount = GetNumberofStars(celestialParams, _random);
                                        newsystem = _systemGen.GenerateSystem(starCount);


                                        // - flesh out the solar system if this is not a bare bones universe with stars only
                                        if (celestialParams.GeneratePlanets)
                                            GenerateWorldsForSystem(newsystem, _worldGen, _random, celestialParams.GenerateMoons, celestialParams.GeneratePlanetoidBelts);
                                    }
                                    zone.AddChild(newsystem);

                                    // only create db star record AFTER system is added to zone or globaltranslations will be incorrect
                                    if (newUniverse.CreateStarDigest)
                                    {
                                        CreateStarSystemDatabaseRecords(newsystem);
                                    }

                                    // Nov.5.2016 - WriteSychronous() is very slow.  It is the bottleneck during universe creation
                                    // TODO: Maybe faster if I can do one XML file per Zone or 
                                    //       switch to sqlite.  First i should try the one XML file per Zone though... and Stars 
                                    //       and worlds should be inline.  The other problem is that textures, models are all in one file still.
                                    //       so even if we break up the Zones, they still wind up pointing to other bloated XML files. In other words
                                    //       if instead we could have a seperate FOLDER for each Zone, and then have the XML in those folders be just
                                    //       for entities and resources in that Zone.  In effect, we're talking about a seperate XMLDB for each Zone.
                                    //       The problem there is, shared resources are no longer shared properly. Is that a problem?


                                    xmldb.WriteSychronous(zone, true, false, false);
                                }
                            }
                            // now we can write this region which has a fully created star (or empty) system with planets and moons
                            if (info.SerializeEmptyZones == true && zone.ChildCount == 0)
                                xmldb.WriteSychronous(zone, true, false, false); // must not increment/decrement 
                            // TODO: removeChildren here can screw up the scene whilst IPageable resources
                            // that depend on this branch are being unloaded and removed from Repository!
                            // so how to fix this aspect of IPageableNode
                            // TODO: I should be able to put some kind of "abort" status on it?
                            // but how to ensure i cover all cases?

                            if (zone != null) // empty zones with serializeemptyzones == false will result in zone == null here so test for it
                            {
                                // Dec.6.2012 - zone.RemoveChildren() is wrong.
                                //      in fact tests show that zone.RemoveChildren() is not required 
                                // 		so long as IncrementRef/DecrementRef removes from cache and will trigger cascade of RemoveChild 
                                // 		as all parent refcounts == 0.
                                //zone.RemoveChildren();  
                                //Repository.Remove(zone);
                                Repository.IncrementRef(zone); // artificially raise refcount to 1
                                Repository.DecrementRef(zone); // force refcount back to 0 and this will trigger removal from cache and cascade to children
                            }

                            //System.Diagnostics.Debug.WriteLine("ZONES REMAINING = " + (--totalZoneCount).ToString());
                        }
                    }
                }

                // NOTE: Disabling paging for resources (except for DomainObject scripts for now until we start persisting to .save)
                //       But even with disabling paging of most resources, the generation is slow because the xmldb.WriteSynchronous() is taking
                //       ~30 seconds whereas the actual galaxy generation for 5x1x5 galaxy takes just 2 seconds.
                PagerBase.Disabled = false;

                System.Diagnostics.Debug.WriteLine("-- {0} -- ZONES CREATED", width * height * depth);


                // NOTE: ZoneRoot does not actually add any Zone's as children here.  Otherwise they would be deserialized automatically
                // when we actually want the Pager to handle loading and unloading of child Zones to the ZoneRoot.
                xmldb.WriteSychronous(root, true, true, false);

                xmldb.WriteSychronous(info, true, false, false);
                xmldb.SaveAllChanges();
                xmldb.Dispose();

                // NOTE: Node.ctor adds to Repository with refcount == 0, however the call above to xmldb.Create() already does IncrementRef and DecrementRef on the SceneInfo
                //       node to remove it from Repository.
                //Repository.IncrementRef(info); 
                //Repository.DecrementRef(info); 
                // however we do need to remove StarDigest and Viewpoints
                info.RemoveChildren();

                stopWatch.Stop();
                System.Diagnostics.Trace.WriteLine(string.Format("Universe generated in {0} seconds", stopWatch.Elapsed.TotalSeconds));
                
            }
            catch (Exception ex)
            {
            }
        }


        private void CreateStarSystemDatabaseRecords(StellarSystem system)
        {
            // In SQLite3 opening the db also creates the file if necessary
            // NOTE: GetConnection() returns an opened database connection 
            System.Data.SQLite.SQLiteConnection conn = Database.AppDatabaseHelper.GetConnection();


            // using a single transaction allows MUCH faster inserts
            using (var transaction = conn.BeginTransaction())
            {
                for (int n = 0; n < system.StarCount; n++)
                {
                    Database.AppDatabaseHelper.CreateStarRecord(system.Stars[n], conn);

                    for (int m = 0; m < system.Stars[n].ChildCount; m++)
                    {
                        World planet = system.Stars[n].Children[m] as World;
                        if (planet != null)
                        {
                            Database.AppDatabaseHelper.CreateWorldRecord(planet, conn);

                            for (int p = 0; p < planet.ChildCount; p++)
                            {
                                World moon = planet.Children[p] as World;
                                if (moon != null)
                                {
                                    Database.AppDatabaseHelper.CreateWorldRecord(moon, conn);
                                }
                            }
                        }
                    }
                }
                transaction.Commit();
            }
            conn.Close();
        }


        /// this routine determines if a new system should be created
        ///  at a particular region in space.  It uses the
        ///  Density setting to determine the odds that a system is created
        ///  or not.  The higher the Density, the better the chance that a system
        ///  will be created in that region
        private bool ShouldSystemGoHere(float clusterDensity, Random rand)
        {
            return (rand.NextDouble() <= clusterDensity);
        }

        /// this function returns the number of companion stars are created based
        /// on the users settings.  Possible outcomes are 1, 2, 3 or 4 (for now)
        private uint GetNumberofStars(KeyCommon.Messages.UniverseCreationParams mParams, Random rand)
        {
            double d = rand.NextDouble() * 100;
            uint result = 0;

            //  Generate random byte between 1 and 100.
            if (d <= mParams._percentSingleStarSystems)
                result = 1;
            else if (d <= mParams._percentSingleStarSystems + mParams._percentBinaryStarSystems)
                result = 2;
            else if (d <= mParams._percentSingleStarSystems + mParams._percentBinaryStarSystems + mParams._percentTrinarySystem)
                result = 3;
            else
            {
                result = 4;
                Debug.Assert(d >= 0 &&
                             d <= mParams._percentSingleStarSystems + mParams._percentBinaryStarSystems + mParams._percentTrinarySystem +
                                  mParams._percentQuadrupleStarSystems);
                Debug.Assert(mParams._percentSingleStarSystems + mParams._percentBinaryStarSystems + mParams._percentTrinarySystem +
                             mParams._percentQuadrupleStarSystems == 100f);
            }
            return result;
        }

        
        /// <summary>
        /// After each system is created and before the region is written to xml, this is called to generate the orbital
        /// info for worlds that will be in the system.
        /// </summary>
        /// <param name="system"></param>
        private void GenerateWorldsForSystem(StellarSystem system, GenerateWorld worldGen, Random rand, bool moonGenerationEnabled, bool planetoidBeltGenerationEnabled)
        {
            // generate positions for worlds around stars and the star system 
            // note: zoneGenerator just generates the habitable/forbidden/inner/outer zones for the star system
            OrbitSelector zoneGenerator = new OrbitSelector(rand);
            zoneGenerator.Apply(system);

            List<OrbitSelector.OrbitInfo> orbits = new List<OrbitSelector.OrbitInfo>();
            // note: orbitGenerator just generates the orbits and the types of planets for each star systems based on it's orbtial zone information
            foreach (OrbitSelector.OrbitalZoneInfo zoneInfo in zoneGenerator.OribtalZones)
                orbits.AddRange(zoneGenerator.GenerateOrbits(zoneInfo));


            // flesh out the full world statistics for each planet
            foreach (OrbitSelector.OrbitInfo orbit in orbits)
            {
            	string id = Repository.GetNewName (typeof(World));
            	World newplanet = new World(id);
				newplanet.Name = orbit.ParentBody.GetFreeChildName();
				// TODO: is this generating unique orbit's based on Bode's Law or is it
                //       just creating random orbits?  Because for moons, it seems there 
                //       are too many bunched together.
                // NOTE: Remember child Entity.Translation is always relative to parent Entity. This simplifies calculation.
                // make sure this translation fits within the current region's radius.  Ideally the entire planet
                // and it's orbit should fit within the bounds of the Zone 
                newplanet.Translation = new Vector3d(orbit.OrbitalRadius, 0, 0);
                // TODO: System.Diagnostics.Trace.Assert(PlanetFitsEntirelyInSystem(newplanet));
                // TODO: eventually compute more sophisticated positions instead of linear along x axis
                //       note: this is now fixed by selecting start epoch in orbit animation
                newplanet.OrbitalRadius = orbit.OrbitalRadius;
                newplanet.WorldType = orbit.WorldType;

                // NOTE: if planetoidBeltGenerationEnabled == false, we'll replace the belt with a planet
                if (orbit.WorldType == WorldType.PlanetoidBelt && planetoidBeltGenerationEnabled)
                {
                    int asteroidCount = 1000;
                    //Celestial.PlanetoidBelt belt = new Celestial.PlanetoidBelt();
                    // TODO: create a PlanetoidField object that inherits Region perhaps and then uses our new
                    // system of "circled covered wagon formation" of boxes which we'll be procedurally generated during rendering
                    // i.e no need to store every asteroid.  This will mitigate some of the performance annoyance with saving regions to disk
                    // TODO: newPlanet in this case should actually be thought of as a "field"
                    // and perhaps rather than a world at all, we should pass in an OctreeRegion
                    ProceduralHelper.InitAsteroidField(newplanet, (float)orbit.OrbitalRadius, asteroidCount);
                }
                else
                {
                    if (orbit.ParentBody is Star)
                    {
                        Star star = (Star)orbit.ParentBody;
                        worldGen.ComputeWorldStatistics(newplanet, star.Age, star.Luminosity, star.LuminosityClass,
                                            star.SpectralType, star.SpectralSubType,
                                            orbit, orbit.Zone);

                        if (moonGenerationEnabled)
                        {
                            // generate moons using SINGLE star stats
                            Keystone.Celestial.GenerateMoon moonGen = new GenerateMoon(rand);
                            World[] moons = moonGen.GenerateMoons(newplanet, orbit, star.Age, star.Luminosity, (byte)star.LuminosityClass,
                                (float)orbit.OrbitalZoneInfo.SnowLine, orbit.Zone);

                            if (moons != null)
                                for (int i = 0; i < moons.Length; i++)
                                {
                                    worldGen.ComputeWorldStatistics(moons[i], star.Age, star.Luminosity, star.LuminosityClass,
                                                    star.SpectralType, star.SpectralSubType,
                                                    orbit, orbit.Zone);

                                    // create visual model of moon and add moon as child to world
                                    ProceduralHelper.InitWorldVisuals(newplanet, moons[i], true, false, false, false);
                                }
                        }
                    }
                    else // starsystem
                    {
                        // TODO: temp hack hardcoded values
                        SPECTRAL_TYPE spectralType = SPECTRAL_TYPE.M;
                        SPECTRAL_SUB_TYPE spectralSubType = SPECTRAL_SUB_TYPE.SubType_5;
                        LUMINOSITY highestLuminosityClass = LUMINOSITY.WHITEDWARF_D;
                        float oldestAge = 0;
                        //  multistar systems usually have stars of same age since they usually form together, exceptions are when stars capture other stars that formed seperately.  there are rules for exceptions for very old 10billion+ year systems too but i dont know if i implemented those

                        float combinedLuminosity = 0;

                        for (int j = 0; j < ((StellarSystem)orbit.ParentBody).StarCount; j++)
                        {
                            combinedLuminosity += ((StellarSystem)orbit.ParentBody).Stars[j].Luminosity;

                            highestLuminosityClass =
                                highestLuminosityClass.CompareTo(((StellarSystem)orbit.ParentBody).Stars[j].Luminosity) > 0
                                    ?
                                        highestLuminosityClass
                                    : ((StellarSystem)orbit.ParentBody).Stars[j].LuminosityClass;
                        }
                        // TODO:  read the rules and figure out what to use for spectraltype and subtype and
                        // verify im handling luminosity correctly with combined and highest for class
                        worldGen.ComputeWorldStatistics(newplanet, oldestAge, combinedLuminosity, highestLuminosityClass,
                                 spectralType, spectralSubType,
                                orbit, orbit.Zone);

                        // generate moons using COMBINED star stats
                        if (moonGenerationEnabled)
                        {
                            Keystone.Celestial.GenerateMoon moonGen = new GenerateMoon(rand);
                            World[] moons = moonGen.GenerateMoons(newplanet, orbit, oldestAge, combinedLuminosity, (byte)highestLuminosityClass,
                                (float)orbit.OrbitalZoneInfo.SnowLine, orbit.Zone);

                            if (moons != null)
                                for (int i = 0; i < moons.Length; i++)
                                {
                                    worldGen.ComputeWorldStatistics(moons[i], oldestAge, combinedLuminosity, highestLuminosityClass,
                                                    spectralType, spectralSubType,
                                                    orbit, orbit.Zone);

                                    // create visual model of moon and add moon as child to world
                                    ProceduralHelper.InitWorldVisuals(newplanet, moons[i], true, false, false, false);
                                }
                        }
                    }

                    // create visual model of planet
                    ProceduralHelper.InitWorldVisuals(orbit.ParentBody, newplanet, orbit.WorldType == WorldType.Terrestial, orbit.WorldType != WorldType.Terrestial, true, true);
                    //Debug.WriteLine (orbit.WorldType.ToString() + " planet '" + newplanet.ID +"' placed at position " + newplanet.Translation.ToString () + " has radius of " + newplanet.Radius.ToString ());
                }


            } // end for
        }

        
        // NOTE: This must only be allowed on Prefabs and not SavedEntities
        // when launching floorplan, user can iterate through list of found Containers.
        // if no interior exists (at any stage of completion) the user is presented a menu
        // item to generate interior.
        // 1) Upon clicking, they are given a dialog that yeilds some basic stats about the exterior mesh
        // and then asks 
        //   a) how many decks (and it will compute the deck height)
        //   b) the deck height and it will compute how many decks 
        //   -  allows selection of 2 - 6 meter deck height 
        //   - Textures will tile at 2 meter increments and scale inbeteen.
        //   - 6 meters allows for floorplans of buildings and space stations.  Especially warehouses for instance
        // that can be constructed of 2 floors each 6 meters high but with no ceiling for the first floor 
        // thus providing a continguous space and a place lots can be stored.
        // 2) Allows for importing of a new container by popping up asset dialog and allow selection
        //    of the exterior mesh?
        //    - or we need some otehr way to convert an existing Entity into one that is a Container.
        //    - we could do that via an option in the floorplan toolbar when selecting an Entity
        //    to view the floorplan of... to allow conversion to container and then generation of interior.
        // 3) I think the above is a good start and better than alternatives.  We can tweak from there
        //    as need be.
        private void AddInterior(Keystone.Entities.Container container, string interiorID, uint quadtreeDepth, string relativeDestinationPath)
        {

            // launch dialog to assist in generation of basic interior
            FormNewInterior newInterior = new FormNewInterior();

            System.Windows.Forms.DialogResult result = newInterior.ShowDialog();


            // validate floor height is in acceptable range

            // validate the Container entity's exterior mesh is 
            // of appropriate size for a floorplan.  If it's too small
            // it may only be allowed as a fighter/bomber

            // TODOO: 
            // TODO: the vehicle's exterior mesh is not loaded?  We're not getting proper container.BoundingBox values
            const uint OVERLAP = 2; // ensure there is 1 out of bounds cell on BOTH sides of the Interior floors.  No walls can be placed on the outer edge of these out of bounds cells.  TODO: we need to enforce that
            Vector3d cellSize = newInterior.CellSize;
            // verify the mesh is loading, then verify the Container object calculates it's boundingbox properly
            Keystone.Types.BoundingBox bounds = container.BoundingBox;
            uint cellsAcross = (uint)(bounds.Width / cellSize.x) + OVERLAP;
            uint cellsLayers = (uint)(bounds.Height / cellSize.y);
            if (cellsLayers == 0) cellsLayers = 1; // minimum of one for models with low ceilings

            uint cellsDeep = (uint)(bounds.Depth / cellSize.z) + OVERLAP;


            // TODO: "decks" can be labeled as spacing decks that can be used to hold
            // access tubes/ventilation shafts?
            // 1) Any half deck is built as a normal deck wwhere we specify it's height as smaller
            //    So that is how any crawlspace would be made as being a short height deck 
            // 2) shafts within walls would be constructed as sandwiched between walls.
            //    so you could create extra thick bulkheads and then have one part within the walls
            //    not be solid but hollow for a special access route should it be needed.
            //    There is no need to change how our decks are layed out otherwise.
            //    The only thing we need is a way to specify the height of the walls of the deck and
            //    the start (aka how thick the floor is)
            //
            // NOTE: from now on, creating a new vehicle consists of 
            // 1) selecting a normal modeled entity
            // 2) in plugin, click the floorplan tab and enable floorplan creation

            // 3) click Add Deck from the floorplan plugin tab and 
            //    generate floorplan
            //    where you will be prompted 
            //      a) for whether the first deck will be at lowest z or lowest y value
            //      b) for a height between -y and +y for each
            //    new deck you wish to add.  This y value will represent center height of that
            //    deck/floor
            // ?) When do we specify the thickness of the deck floor?
            // ?) How do we specify thickness of ceiling of final top deck?
            // 4) specify the height of the deck.  
            //      a) allow options for user to be assisted with coming up for values by
            //      making sure that an above floor starts at height of the below floor.
            //      b) ensure that decks are built from bottom floor up? or top down?
            // 
            // 5) internally we compute a cross section at the y height of the deck
            //    and use this polygon to determine which cells will be available for 
            //    plotting deck design 
            // 6) if the exterior geometry is removed, the entire deckplan will be destroyed as well.
            // 7) create a version of this code that can validate each deck based on cross sections
            //    after the fact so that submitted designs can be checked.


            Keystone.Elements.Mesh3d exteriorMesh = (Keystone.Elements.Mesh3d)container.Model.Geometry;

            int crc32 = 0; // TODO: we must get crc32 of the Mesh3d object
            // that does crc32 of all verts + scale of that mesh and model
            // and entity!  (actually model and entity scales must be 1,1,1)
            // we do not support scaling of the geometry for security reasons???


            // we only disable picking of exterior or rendering of exterior in the floorplan view
            // by setting options on the Context and it's pickparameters
            container.Pickable = true;
            container.Visible = true;

            /////////////////////////////////////////////////////////////////////////////
            // INTERIOR 
            /////////////////////////////////////////////////////////////////////////////
            // based on exterior mesh bounding volume and the cell size, compute cellsacross/layers/deep
            
            Keystone.Portals.Interior interior =
                new Keystone.Portals.Interior(interiorID, cellSize,
                            cellsAcross, cellsLayers, cellsDeep, quadtreeDepth);

            interior.SetProperty("datapath", typeof(string), relativeDestinationPath + "\\" + interiorID);

            // todo: script path should be customizable
            string scriptPath = @"caesar\scripts_entities\ship_interior.css";
            Keystone.Celestial.ProceduralHelper.MakeDomainObject(interior, scriptPath);

            // orientation of interior will always be origin with 0 rotation.  With "front" of exterior facing positive Z
            // this will have the effect of the lower ID cell indices having the highest Z values and the cells
            // at "back" of exterior vehicle having lowest Z values.  
            container.AddChild(interior);

            //  interior.CreateMask("boundaries", 0);
            //  interior.CreateMask("floors", 0);

            // add a default directional light.  Interior does not use the Zone's star light  - Dec.1.2022
            // todo: is the correct light being used?  also, the Interior light should be placed at rroot in QuadtreeCollection and not in a single Quadtree child node. 
            // todo: and how do reactors and shuttlecraft that take up multiple floors get placed in the QuadtreeCollection?
            float range = (float)(cellsAcross * cellSize.x);
            range = (float)Math.Max(range, cellsDeep * cellSize.z);
            Keystone.Lights.DirectionalLight light = Keystone.Celestial.LightsHelper.LoadDirectionalLight(range);
            Keystone.Traversers.SuperSetter setter = new Keystone.Traversers.SuperSetter(interior);
            setter.Apply(light);


            // obsolete for 1.0 - no more auto generated boundaries
            //// autogenerate all floors based on bounds of mesh and a fixed height of 2 meters (or 3?)
            //// TODO: maybe let's try to get our floorplan view based on an autogenerated
            //// interior of the yorktown...
            //System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            //Keystone.Types.Polygon[] crossSections = new Keystone.Types.Polygon[cellsLayers];
            //for (uint i = 0; i < cellsLayers; i++)
            //{
            //    double height = interior.GetFloorHeight(i);

            //    watch.Start();
            //    // NOTE: CreateCrossSection takes into account any scaling of the Model or Entity
            //    //       so that should be done to each crossSection[n]
            //    // TODO: its also taking into account rotation and that's wrong since we are conducting
            //    // the cross section tests at origin with 0 rotation
            //    Keystone.Types.Matrix transform = vehicle.Model.RegionMatrix;
            //    transform.M41 = transform.M42 = transform.M43 = 0.0; // remove translation from the matrix
            //    crossSections[i] = Keystone.Elements.Mesh3d.CreateCrossSection(exteriorMesh, (float)height, (float)cellSize.y);
            //    crossSections[i] = crossSections[i].Transform(transform);
            //    watch.Stop();
            //    // TODO: for each cross section, we need to test each tile in each celledregion
            //    // to see if that tile is INSIDE of the cross section and if so, set a flag indicating
            //    // it has a "floor" and thus allowing user to place interior components on it. by default
            //    // all tiles are out of bounds.
            //    int outOfBoundsCount = 0;
            //    for (uint j = 0; j < interior.CellCountX; j++)
            //        for (uint k = 0; k < interior.CellCountZ; k++)
            //        {

            //            Keystone.Types.Vector3d[] tileVertices = interior.GetTileVertices(j, i, k); // j, i, k is x,y,z order
            //            for (int n = 0; n < tileVertices.Length; n++)
            //                tileVertices[n].y += cellSize.y *.5;

            //            // is this tile entirely inside the bounds of the polygon?
            //            if (crossSections[i].ContainsPoints(tileVertices))
            //            // set all 16x16 footprint of this tile to FLOOR
            //            {
            //                uint tileStartX = j * 16;
            //                uint tileStartZ = k * 16;

            //                for (int x = 0; x < 16; x++)
            //                    for (int z = 0; z < 16; z++)
            //                    {
            //                        // mTileMask is y, x, z index order
            //                        interior.mTileMask[i, tileStartX + x, tileStartZ + z] |= 1 << 0; // TILEMASKFLAGS_FLOOR;
            //                    }
            //            }
            //            else
            //            {
            //                System.Diagnostics.Debug.WriteLine("Tile is out of cross section boundaries.");
            //                outOfBoundsCount++;
            //            }
            //        }

            //    System.Diagnostics.Debug.WriteLine(string.Format("{0} of {1} tiles out of bounds", outOfBoundsCount, interior.CellCountZ * interior.CellCountX));
            //    // TODO: then we should save this vehicle and verfy we can reload it because
            //    // this process is too slow to have to do everytime i want to test.
            //    System.Diagnostics.Debug.WriteLine(string.Format ("Cross Section {0} of {1} completed in {2} seconds.",i+1, cellsLayers, watch.Elapsed.TotalSeconds));
            //    watch.Reset();
            //}

            // IMPORTANT REMINDERS:
            // 1) Floor/Ceiling thickness is simply regulated by increasing the y height of a floor
            //    or ceiling tile.
            // 2) Vertical lift tubes or stairwell or "Jefferies tubes"  can be built manually out of a single
            //    tile that has walls around it to form a tube.  Doors/hatches can exist on each level
            //    and can contain either a ladder or an electric lift.  The idea is that these tubes
            //    can allow access through damaged parts of the ship where pressureization has failed
            //    while being protected by the pressurized tube itself.
            //    Sections of the tube can be sealed off with irises to act as airlocks.
            // 3) Horizontal ventilation shafts style tubes can be built similarly using a special
            //    type of floor tile or ceiling tile that is hollow.  Thus the tile contains flags
            //    that indicate it can be stepped on like a normal floor tile but also can contain
            //    a volume.
            // 4) The overall deck height is fixed for every single deck.  This compromise however
            //    will still allow us to replicate any type of starship interior.

            // 5) In this sims, you could simply see down through to any floor if there was no ceiling
            //   or floor tiles in the way and it would continue until you eventually did hit one.
            // 6) any area that has a full floor is considered a room that extends upwards until
            //   it meets a full ceiling.  If there is a NON door/access hole in the ceiling then that 
            //   space through the ceiling becomes apart of the room below it.
            //
            // I could convert a "CelledRegion" into a "CelledArea" and then enforce these are strictly
            // 2d width x depth but which because they match the width depth of anything above or below it
            // they can still compute traversal upstairs or downstairs
            // Doing this we can do two things
            // 1) we can save a bit of memory for decks that aren't all as wide as the widest deck
            // 2) we can more easily join subassemblies that have different altitudes and such
            //    so that subassemblies can allow for different sectors like in a huge babylon 5 station
            //


        }

        //        private void AddFloor(Keystone.Entities.Container vehicle, float height)
        //        {
        //            string interiorID = vehicle.ID + "_interor";
        //            Keystone.Portals.CelledRegion interior = (Keystone.Portals.CelledRegion)Repository.Get(interiorID);

        ////            // OBSOLETE - No need to create a cross section any longer.  We will manually determine
        ////            // in bounds and out of bounds interior tiles.
        ////            // NOTE: CreateCrossSection takes into account any scaling of the Model or Entity
        ////            //       so that should be done to each crossSection[n]
        ////            // TODO: its also taking into account rotation and that's wrong since we are conducting
        ////            // the cross section tests at origin with 0 rotation
        ////            Keystone.Types.Matrix transform = vehicle.Model.RegionMatrix;
        ////            transform.M41 = transform.M42 = transform.M43 = 0.0; // remove translation from the matrix
        ////            float stepSize = 1.0f;
        ////            Keystone.Types.Polygon crossSection = Keystone.Elements.Mesh3d.CreateCrossSection((Keystone.Elements.Mesh3d)vehicle.Model.Geometry, (float)height, stepSize);
        ////            crossSection = crossSection.Transform(transform);
        ////            // END OBSOLETE CROSS SECTION 

        //            // cross section above should be obsolete since we no longer care to automate
        //            // creation of inbounds/out of bounds tiles.  we will do it manually and we want
        //            // to be able to do it with a placement tool "bounds" brush

        //            // saving this vehicle in Morena_Full shoudl also be able to load the floor layers
        //            // when we assign decks to specific layer indices.  But all inbound/out of bounds should exist
        //            // for every possible layer even though only few decks will actually be created at runtime

        //            // is there a difference between a floor being designated to a layer
        //            // and assigning the inbounds/outofbounds of a floor?  Well yes.  A layer never
        //            // has to be a floor, but it must always have bounds flagged before floors can be added
        //            // because in the future, users will want to remodel and change locations of floors.
        //            // 
        //            // TODO: perhaps our "Interior" floors mesh is a single ModelSequence with a Model for each
        //            // floor rather than Floor entities being added.  So how about this.  How about prelimirily
        //            // when we "create floor" below, it adds a new grid to the Interior.ModelSequence
        //            Keystone.Entities.Entity floor = interior.CreateFloor(0);
        //        }


        /// <summary>
        /// Load's an entity from either a prefab during insert in non simulation scene building, or a saved entity during a Simulation_Spawn call. 
        /// This function should always be called from a worker thread so will occur in the background.
        /// This function is NOT called during normal scene deserialization.
        /// </summary>
        /// <param name="fullpath"></param>
        /// <param name="isSavedEntity"></param>
        /// <param name="generateIDs"></param>
        /// <param name="recurse"></param>
        /// <param name="delayResourceLoading"></param>
        /// <param name="translation"></param>
        /// <returns></returns>yeilds
        private Entity LoadEntity (string fullpath, string relativePath, bool generateIDs, bool recurse, bool delayResourceLoading, string[] nodeIDsToUse, Vector3d translation)
        {
          
          int nodeCount = 0;
          Hud hud = coreClient.Scenes[0].Viewports[0].Context.Hud;
          hud.BackgroundLoad(fullpath,guid, nodeCount);
          
          // id, numNodesTraversedSoFar
          
          EntityLoadProgressHandler;
          
          TODO: need to pass callback function to the SceneReader
          
            //delayResourceLoading = false;
            //bool delay = true;
            Entity entity = Keystone.ImportLib.Load(fullpath, generateIDs, recurse, delayResourceLoading, nodeIDsToUse) as Entity;
l
            System.Diagnostics.Debug.Assert(entity != null);

            // if this is a Container, copy the prefab's cellDB to the correct Scenes\\CurrentSceneName\\ folder.
            // NOTE: This is not the same as a "Spawn" command which only occurs during Simulation and not just prefab or floorplan designing.
            // During "Simulation_Spawn" the .interior file and .kgbentity for the Container should already exist in the \\Saves\\ folder.  todo: verify this
            if (entity is Container)
            {
                Interior interior = ((Container)entity).Interior as Interior;
                if (interior != null)
                {
                    // NOTE: its ok that the datapath was originally loaded from a prefab for instance, but for this instance
                    // we now need to rename and copy the datapath so that we don't overwrite the existing prefab's Inteiror data file.
                    string originalRelativeDBPath = (string)interior.GetProperty("datapath", false).DefaultValue;
                    string originalFullPath = System.IO.Path.Combine(AppMain.MOD_PATH, originalRelativeDBPath);
                    Debug.WriteLine("FormMain.Commands.() - Interior dbpath = " + originalRelativeDBPath);
                    Debug.WriteLine("FormMain.Commands.() - Interior resource loaded = " + interior.TVResourceIsLoaded.ToString());
                    // Feb.29.2024 - the following seems wrong for simple scenes. The new saved path of the .interior file doesn't match the expected path when Interior.LoadTVResource() is performed
                    // Interior may not be loaded if delayResourceLoading == true, but if Interior is not null, the datapath should be set and available to reassign to this new instance
   
                    string newRelativePath = Path.Combine(AppMain.CURRENT_SCENE_NAME, entity.ID + ".interior");
                                        
                    // copy the relativePath to the new relativePath for this instance
                    // TODO: destinationpath needs to vary based on Core.SimulationEnabled.  Actually i dont think so because the spawned Container and it's .interior file will already be in the SAVE_PATH. TODO: verify this 
                    string newFullPath = System.IO.Path.Combine(AppMain.SCENES_PATH, newRelativePath);

                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(newFullPath);
                    fileInfo.Directory.Create();

                    if (System.IO.File.Exists(originalFullPath))
                    {
                        if (System.IO.File.Exists(newFullPath))
                        System.IO.File.Delete(newFullPath);

                        System.IO.File.Copy(originalFullPath, newFullPath);
                    }
                    else
                        System.IO.File.Create(newFullPath);

                    interior.SetProperty("datapath", typeof(string), newRelativePath);
                }
            }


            // load any entity script now during this worker thread
            if (!delayResourceLoading)
                PagerBase.LoadTVResource(entity, true);

            // set the prefab link.  Only caller of clone/deserialize/readsychrnous should
            // assign prefab links because sometimes when we clone or deserialize we don't want to
            //KeyCommon.IO.ResourceDescriptor descriptor = new KeyCommon.IO.ResourceDescriptor(addPrefab.RelativeArchivePath, addPrefab.EntryPath);
            entity.SRC = relativePath; // descriptor.ToString();

            ((Entity)entity).Translation = translation;
            ((Entity)entity).LatestStepTranslation = translation;


            return entity;
        }

        // we are only generating IDs here, we dont need scripts or any other resource
        private BonedEntity GenerateBonedEntity(string[] prefabs, Random random)
        {
            int index = random.Next(prefabs.Length);
            string relativePath = prefabs[index];
               
            string fullPath = Path.Combine(AppMain.MOD_PATH, relativePath);
            bool delayResourceLoading = true; // we are only generating IDs here, we dont need scripts or any other resource
            bool generateIDs = true;
            BonedEntity entity = (BonedEntity)LoadEntity(fullPath, relativePath, generateIDs, true, delayResourceLoading, null, new Vector3d()); // NOTE: initial (eg: first run after generation) crew translations are calculated in Loopback upon Interior region load completed.  We need Interior loaded in order to find the unoccupied FLOOR flags.

            return entity;
        }
        
        private void SaveInterior()
        {
        }


        void PositionVehicle(Keystone.Vehicles.Vehicle vehicle)
        {
            Database.AppDatabaseHelper.StarRecord[] starRecords = Database.AppDatabaseHelper.GetStarRecords();
            Database.AppDatabaseHelper.WorldRecord[] worldRecords = Database.AppDatabaseHelper.GetWorldRecords(starRecords[0].ID);

            // TODO: the star and world need to be paged in or otherwise we can't get the current positions of the worlds.
            double smass = starRecords[0].Mass;
            double sradius = starRecords[0].Radius;
            double wmass = worldRecords[0].Mass;
            double wradius = worldRecords[0].Radius;
            double woradius = worldRecords[0].OrbitalRadius;

            //Star star = (Star)Repository.Get(starRecords[0].ID);
            //            World w = (World)Repository.Get(worldRecords[0].ID);

            // June.20.2017 - orbital animations feature cut (postponed til version 2.0)
            //          w.Animations.Play(0, true);
            //          w.Animations.Update(w, 0);
            // TODO: this worldRecords[0].Translation is all wrong, wtf?  it's outside of the zone boundaries.
            //       I think it's because worldRecords[0].Translation is global translation.  I think it is!  We only want the Region space translation, so we'll need to add those to the record.
            Vector3d worldPosition = worldRecords[0].Translation; // w.Translation; // TODO: for moon this wont work since we need RegionTranslation but for planet, Translation and RegionTranslation are the same thing.
            double altitude = wradius + 1000000;

            Vector3d dir = Vector3d.Normalize(worldPosition); // can just normalize because it's dir to star and we know that star is at origin
            Vector3d vehicleTranslation = worldPosition + dir * altitude;
            // TODO: On even numbered zone's across, height, depth the coord 0,0,0 is far away from camera starting point! 
            //       This is why we need to choose relative region position and parent that is Zone and not ZoneRoot (Or a position relative to a Star or World)
            vehicle.Translation = vehicleTranslation; // vehicleTranslation; //  Vector3d.Zero(); // TODO: use proper position here based on orbit

            Vector3d basisVector = Vector3d.Up();// w.RegionMatrix.Right;

            Vector3d tangent = Vector3d.Normalize(Vector3d.CrossProduct(-dir, basisVector));
            //tangent = Vector3d.CrossProduct(w.RegionMatrix.Up, tangent);
            //double worldVelocity = GenerateWorld.GetOrbitalVelocity(smass, 0, woradius);
            //worldVelocity = Keystone.Celestial.Temp.GetCircularOrbitVelocity(smass + wmass, woradius);
            //worldVelocity = Keystone.Animation.EllipticalAnimation.GetTrueAnomaly(w.OrbitalPeriod, 1d);

            // TODO: I don't know if this is working because it seems the star's gravity pull is pulling us
            //       through the planet and onto the star.  It seems perhaps we need something like gravity wells
            //       where gravity emitting bodies only affect vehicles within their well. Let's revisit this issue
            //       once we get the tangent vector solved correctly and see if our starship can at least orbit
            //       a few times before the orbit is destabilized by the Star's gravity pull.
            // TODO: I could test it by just removing that from the Star script... see if we can orbit this planet.
            double velocity = Keystone.Celestial.GenerateWorld.GetOrbitalVelocity(wmass, 0, altitude);
            //velocity += worldVelocity;
            // is v= Math.Sqr(GMr)
            // - retrieve world and moon records and assign moon as parentID?  No.  That is only if we want
            //   hierarchical position of our ship and we're still trying to use absolute region positions based on gravitation
            // - compute velocity for ship around moon including hierarchical velocities for worlds around stars
            //      - find tangent vector velocities for each and add them
            vehicle.Velocity = tangent * velocity;

            //float altitude = 10000000000f;
            double semiMajorAxis = altitude;
            ////string vehiclePath = @"caesar\\meshes\\vehicles\\uesn_yorktown.kgbentity";
            ////// TODO: there are issues with loading .obj files.  Perhaps if i switched from AddVertex to SetGeometry it would solve the problem?
            //////vehiclePath = @"caesar\\meshes\\vehicles\\morena smuggler\\morena1.kgbentity";
            ////   ModeledEntity vehicle = CreateVehicle (vehiclePath, region, star, altitude);

            ////   // TODO: 
            ////   double G = Keystone.Celestial.Temp.GRAVCONST;

            ////   //TODO: does this yeild KM/s or M/s? we want M/s.
            ////   double velocityMetersPS = Math.Sqrt((2d * G * star.MassKg / altitude) - (G * star.MassKg / semiMajorAxis));
            ////   // velocityMetersPS *= 1000;

            ////   vehicle.Velocity = new Vector3d (velocityMetersPS, 0, 0 );
            ////   //transformable.AngularVelocity = ;



            // sychronous loading
            //            Keystone.IO.PagerBase.LoadTVResource (vehicle);

            //// vehicle's are not hierarchically tied to worlds, they are Region relative
            //// so translation to a world should apply cumulative translation of Sol system hierarchy
            //// WARNING: vehicle starting too close to planet (such as inside planet!) will cause physics to go crazy.
            //vehicle.Translation = body.GlobalTranslation + new Vector3d(0, 0, body.Radius + altitude);

            //region.AddChild(vehicle);

            //// compute starting velocity
            //vehicle.Force += body.Velocity;
            //// TODO: how can we test this with 1 star and 1 computed velocity vector
            ////       and then distance to star should be constant for a nearly perfect starting velocity vector
            ////       at close distance.  

            //// TODO: can we delay 1 frame so that our orbital animation can play at least once to set the
            ////       orbits at their starting positions so we know where to place this vehicle and then 
            ////       the starting velocity to assign?

            //// actual velocity is cumulative velocity of world velocity and relative orbital velocity
            //// - velocity of a world is vector length of previous position and current
            ////   - but can we get it from the eliptical animation itself

            // assign this vehicle as chase camera follow target
        }

        // crew and world generation belong in game01.dll
        private void GenerateCrew(string interiorID, int crewCount, int seed)
        {
            if (crewCount <= 0) return;

            // todo:  Maybe we can use a callback from .CreateCharacters() to then load the models?
            // todo: i need a ratio for the department each member will be assigned and i dont even know yet what full list of departments there will be. 
            // todo: need to construct a chain of command
            // todo: our behaviorContext can be assigned to mCustomData as well if its not already
            Game01.GameObjects.Character[] characters = Game01.ProcGen.CreateCharacters(crewCount, _core.Seed);
            
            
            // IMPORTANT!!!!!!!!!!!!!!!!!!!
            // ================================================
            // if We are going to generate a character here, then here is where we must Register
            // any custom interfaces/structs that are specific to this Game.  
            // We must also register any customProperties specs if we wish to.
            //
            // We will then rely on the Entity's dispose to free these resources
            // (or perhaps Repository.DecrementRef() when the Entity is being removed.)
            //
            //
            // so our KeyCommon.Data.UserData Entity.BlackBoardData; 
            // is a UserData object that can contain an Dictionary of 
            // Memory<T> that is returned as "object" but the caller
            // will know which user struct (eg Weapon_Struct) to cast 
            // it to.
            //
            //   UserData  // the idea here is that all user data for one entity is hosted here 
            //   UserDataStore        // all UserDataStore instances are tracked here
            //   ComponentsStore<T>   // all struct instances are tracked here
            //   ComponentsStoreCollection // all ComponentStore instances for all types of generic <T> are tracked here
            //  
            // - so we have:   
            //   Repository.IntrinsicMemoryStores // <- a collection of ComponentStores
            //   Simulation.IntrinsicProcessors
            //
            //   Repository.UserMemoryStores      // <- a collection of ComponentStores
            //   Repository.UserDataStores        // it can hold all our user data including AI data, Memory<T>, user defined structs, etc.
            //   Simulation.UserProcessors
            //   
            //   Entity.BlackboardData data = Repository.UserData.CheckOut(entityID);
            //   entity.UserData = data;
            //   
            //   store = StoreCollection.CheckOut()
            //   Memory<T> mem = store.CheckOut(entity.ID)
            //   entity.IntrinsicMemory = mem;
            //   IntrinsicProcessors.CheckOut<STEER>();
            
            
            //   entity.Dispose()
            //   {
            //       System.Diagnostics.Debug.Assert(mUserData == null);
            //       System.Diagnostics.Debug.Assert(mIntrinsicMemory == null);
            //       System.Diagnostics.Debug.Assert(mIntrinsicProcessors == null);
            //       System.Diagnostics.Debug.Assert(mUserMemory == null);
            //   }
            // 
            
            //  
            // The same thing for our Viewpoint's blackboarddata.  
            //
            // Entity.BlackboardData or Entity.CustomData or Entity.UserData or Entity.RuntimeData
            // 
            // ================================================
            
            
            
            System.Diagnostics.Debug.Assert(crewCount == characters.Length);
            BonedEntity[] bonedEntities = new BonedEntity[crewCount];

            string[] relativePaths = new string[crewCount];
            string[] malePrefabs = new string[] { "caesar\\actors\\colonel-x.kgbentity" };
            string[] femalePrefabs = new string[] { "caesar\\actors\\aiko_physics.kgbentity" };

            // NOTE: using parallel.for() breaks SceneReader which is not thread safe particularly when it comes to shared behavior tree nodes
            //System.Threading.Tasks.Parallel.For(0, crewCount, i =>
            //{
            //    string[] prefabs = malePrefabs;

            //    if (characters[i].Gender == 1)
            //        prefabs = femalePrefabs;
            //    // todo: when i have multiple male and female models, i may need to know their rank and department to determine which model to use
            //    Random random = new Random(seed + i);
            //    bonedEntities[i] = GenerateCrewModel(prefabs, random); // todo: pass in characters[i] so we have access to more data about this crew member to determine the models to use
            //    bonedEntities[i].CustomData = new KeyCommon.Data.UserData();
            //    // todo: same should be done for celestial bodies.  Celestial should be merged into game01.dll and all propertiies for it should be assigned by the script
            //    bonedEntities[i].CustomData.SetObject("character", characters[i]);
            //    bonedEntities[i].Name = characters[i].FirstName + " " + characters[i].LastName;
            //    relativePaths[i] = AppMain.CURRENT_SCENE_NAME + "\\" + bonedEntities[i].ID + ".kgbentity";
            //});

            for(int i = 0; i < crewCount; i++)
            {
                string[] prefabs = malePrefabs;

                if (characters[i].Gender == 1)
                    prefabs = femalePrefabs;
                // todo: when i have multiple male and female models, i may need to know their rank and department to determine which model to use
                Random random = new Random(seed + i);
                bonedEntities[i] = GenerateBonedEntity(prefabs, random); // todo: pass in characters[i] so we have access to more data about this crew member to determine the models to use
                // todo: i think i need to add a random seed counter to all Entities
                // TODO: this is problematic because we are not assigning the BlackboardData during 
                //       Scene.EntityAttached() because this Crew member is not already assigned to a ship
                bonedEntities[i].BlackboardData = new KeyCommon.Data.UserData();

                // todo: same should be done for celestial bodies.  Celestial should be merged into game01.dll and all propertiies for it should be assigned by the script
                bonedEntities[i].BlackboardData.SetObject("character", characters[i]);
                bonedEntities[i].Name = characters[i].FirstName + " " + characters[i].LastName;
                relativePaths[i] = AppMain.CURRENT_SCENE_NAME + "\\" + bonedEntities[i].ID + ".kgbentity";

                // set custom properties for Station operators. Grab highest ranking Characters for starters
            }


            // todo: server ultimately in real client/server configuration, needs to be able to send Character info over and the client can store it how it wishes.  
            //       So GameObjects may need to implement NetBuffer read/write
            // todo: database needs to accomodate storing/retreiving Game01.GameObjects.Character
            // todo: i believe the bonedEntities[i].ID is the primary key we are using and so when we delete bonedEntities, we know which record to delete.
            Database.AppDatabaseHelper.CreateCharacterRecords(bonedEntities, interiorID, relativePaths);

            for (int i = 0; i < crewCount; i++)
            {
                bonedEntities[i].SRC = null;
                bonedEntities[i].SetFlagValue("forceserializeseperate", true);
                Scene.WriteEntity(bonedEntities[i], true);
                // NOTE: we do not add these BonedEntities to the Vehicle.Interior.
                // when vehicle is successfully spawned, then server can start spawning the Crew
            }
        }

        private bool CrewPositionsNeedInitialization(Database.AppDatabaseHelper.CharacterRecord[] characters)
        {
            if (characters == null || characters.Length == 0) return false;

            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].Translation != new Vector3d())
                    return false;
            }

            return true;
        }

        private Vector3d[] PositionCrew(string parentID, int count)
        {
            if (string.IsNullOrEmpty(parentID)) return null;

            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Repository.Get(parentID);
            System.Diagnostics.Debug.Assert(interior.TVResourceIsLoaded);

            //Keystone.Portals.Interior.TILE_ATTRIBUTES.COMPONENT;

            Vector3d[] positions = new Vector3d[count];
            int flag = (int)Keystone.Portals.Interior.TILE_ATTRIBUTES.FLOOR;
            uint[] cells = interior.GetCellList(0, (interior.CellCountX * interior.CellCountY * interior.CellCountZ) - 1, flag);

            if (cells == null || cells.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("PositionCrew() - Failed to find any available FLOOR cells");
                return null;
            }
            // todo: this random is not using a seed
            Random random = new Random();
            // we already have an OUT_OF_BOUNDS flag and TILEMASK.FLOOR i think.  We would have to find only FLOOR and no other OBSTACLE flags on them
            bool[] occupied = new bool[cells.Length]; // of the pruned cells, flag the ones that are already occupied with an actor
            for (uint i = 0; i < positions.Length; i++)
            {
                uint cellIndex = (uint)random.Next(0, cells.Length);
                while (occupied[cellIndex] == true)
                {
                    cellIndex = (uint)random.Next(0, cells.Length);
                }

                occupied[cellIndex] = true;
                uint cellID = cells[cellIndex];
                positions[i] = interior.GetCellCenter(cellID);
                positions[i].y = positions[i].y - (interior.CellSize.y / 2d);
            }

            return positions;

        }
	}
}