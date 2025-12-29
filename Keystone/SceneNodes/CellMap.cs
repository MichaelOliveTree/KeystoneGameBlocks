using System;
using System.Collections;
using System.Collections.Generic;

using Keystone.CSG;
using Keystone.Elements;
using Keystone.Extensions;
using Keystone.Interfaces;
using Keystone.IO;

namespace Keystone.SceneNodes
{
    public class CellMap 
    {
        public delegate void OnDatabaseChanged(CellMap database, CellMap.DataFrame dataFrame);

        private List<OnDatabaseChanged> mObservers;

        private Interior mCelledRegion; // TODO: in the future, this should be Structure as well as Interior.  Structure is specifically for outdoor tilemaps

        private bool mIsLittleEndian;
        private uint FILE_MAGIC;
        private const uint FILE_VERSION_LATEST = 2; // incremented to 2 since i deleted mCells array


        // when a npc come across an Edge segment that has a door (as determined by the footprint flags), 
        // a) how do they know that it's traversible?  well the footprint will indicate there's a "Accessible through"
        //    attribute on that tile.
        //     i) the A* can lookup the door and determine if that particular npc can get through it
        //        a) door functions, non emergency, has authorization
        //        b) door functions, non emergency, no authorization
        //        c) door functions, emergency, no authorization, has hack capability/has explosives,etc
        //        d) door does not function, non emergency, authorization
        //        e) etc
        // b) how do we find the particular door at that edge?
        //    i) well simple right?  we track the doors in another Dictionary?
        //       - this is the method we will try first.
        //    ii) or we do spatial lookup
        //  power link relays required or just adjacents in 3d top and lower levels?

        internal int[, ,] mFootprintData;

        // 48 x 19 = 912 x 14 = 12,765 (x4 bytes per subscript to hold an object reference = 51,072bytes 
        // per layer per ship (already includes all decks) even if all empty). 
        // consider this just in terms of how much empty space we are spending memory on.
        // Basically we are spending right there 32 bits per element already!  Of course,
        // using 16x16 footprint data, it's 256 bits for one layer. So 224 bytes still needed.
        // If we used one 32bit image per, it's (48x16)x (19x16)x4bytes = 933888 bytes (~1 meg)
        // per floor x 14 floors = 14 megs per ship and that gives us 32 "layers" to store data.
        // This data only needs to be paged in when using the realtime simulation, otherwise
        // we switch to a statistical simulation that perhaps most broadly, is simulating
        // on a per ship basis rather than a per crew member basis.
        // http://www.codeproject.com/Articles/32654/Monte-Carlo-Simulation
        // http://stackoverflow.com/questions/1192147/how-the-dynamics-of-a-sports-simulation-game-works
        // Performance wise, I think the above could be fastest.
        // But that depends on how fast to read from the image... which is in video memory...
        // That's why it frankly should not be in video memory. We should reserve that for 
        // just the rendering. indeed server side we wont want to use video memory for that.
        // for 
        //
        // so for each in bounds cell, to instance a Cell array, its 51,027 for the empty Cell[,,] array
        // too, and then each Cell object instance is now only per used cell...

        private Dictionary<uint, Segment> mEdgeStructures; // Walls, Fences, Railings, etc indexed by edge ID. fences and railings MUST footprint wise, take up tiles on both sides of an edge.
        private Dictionary<uint, Segment> mTileStructures; // floors, ceilings, catwalks, etc indexed by cell ID.  What about hatches and ramps and stairs?

        // TODO: are mFloorEntities indexed by cellID or tileiD? are components such as Engines which can span multiple entire cells indexed in their own dictionary?
        private Dictionary<uint, Entities.Entity> mFloorEntities; // TODO: use seperate dictionaries for ceiling mounted, wall mounted entities
        private Dictionary<uint, Entities.Entity> mNPCs;

        private Types.Vector3i[] mAdjacentsLookup;

        // cellmaps use internal constructor and are created by CelledRegion using a 
        // fixed naming convention.
        internal CellMap(string celledRegionID)
        {
            // can we push Initialize into LoadTVResource() ?
            Initialize ( (Interior)Keystone.Resource.Repository.Get(celledRegionID));
        }

        internal CellMap(Interior celledRegion)
        {
            Initialize(celledRegion);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="celledRegion"></param>
        /// <remarks>
        /// AssetPlacementTool does NOT result in Initialize() being performed during PREVIEW placement.  
        /// It only occurs after the Container is placed in the scene.
        /// </remarks>
        private void Initialize(Interior celledRegion)
        {
            // note: windows uses little endian which means the last byte (the least significant) of the
            //       value is stored in the lowest address
            mIsLittleEndian = BitConverter.IsLittleEndian;

            mCelledRegion = celledRegion;

            Keystone.Utilities.JenkinsHash.Hash("KGB_CELLED_REGION_LAYER_DATABASE", ref FILE_MAGIC);

            // initialize entity mapping
            mFloorEntities = new Dictionary<uint, Keystone.Entities.Entity>();
            mNPCs = new Dictionary<uint, Keystone.Entities.Entity>();

            // TODO: when trying to drag and drop the prefab, we are initializing
            //       twice and running out of memory.  We need a way to initialize
            //       only after the Entity is placed in the scene.  Furthermore,
            //       i think we're still using too much memory.  


            // - scale down our ship
            // - lower footprint[,,] from int to short
            // TODO: what happened here?  Is our Morena Smuggler scaled too big suddenly? Are the 
            //       cell dimensions too small?  Is 1/16 too many tiles. 1/8 is plenty for a 1.25 meter square cell.
            //       - can also try making the mFootPrintData as array of byte[] which would
            //       reduce memory consumption by 3/4.  We would have to limit flags stored in the footprintData to 8bits obviously.
            //       This used to work without the out of memory exception.  And I think we used
            //       to only have 9 decks and not over 60.  Something happened here.  
            // TODO: for large starships, we get out of memory exception trying to allocate
            // mFootprintData multidimensional array.  Maybe we just have to go back to
            // smaller Firefly or Traveller ships?
            // 32 bits per each 1/16 tile for ALL decks = 3.2 megs
            // NOTE: we don't need CellCountY * 8 (or * 16) since we are only using 2D on each deck
            try
            {
                mFootprintData = new int[mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX,
                                         mCelledRegion.CellCountY,
                                         mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ];
            }
            catch (OutOfMemoryException oomException)
            {
                System.Diagnostics.Debug.WriteLine("CellMap.Initialize() - " + oomException.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CellMap.Initialize() - " + ex.Message);
            }

            int i = mFootprintData.Length * 4; // * 4 bytes per element
            System.Diagnostics.Debug.WriteLine("CellMap.Initialize() - Cell data size = " + i.ToString() + " bytes.");

            mEdgeStructures = new Dictionary<uint, Segment>();
            mTileStructures = new Dictionary<uint, Segment>();

            mAdjacentsLookup = new  Keystone.Types.Vector3i[8];
            // lower row
            mAdjacentsLookup[0] = new Keystone.Types.Vector3i(-1, 0, -1);
            mAdjacentsLookup[1] = new Keystone.Types.Vector3i(0, 0, -1);
            mAdjacentsLookup[2] = new Keystone.Types.Vector3i(1, 0, -1);

            // middle row (skipping the actual middle tile which is our current tile and not an adjacent)
            mAdjacentsLookup[3] = new Keystone.Types.Vector3i(-1, 0, 0);
            mAdjacentsLookup[4] = new Keystone.Types.Vector3i(1, 0, 0);
            
            // upper row
            mAdjacentsLookup[5] = new Keystone.Types.Vector3i(-1, 0, 1);
            mAdjacentsLookup[6] = new Keystone.Types.Vector3i(0, 0, 1);
            mAdjacentsLookup[7] = new Keystone.Types.Vector3i(1, 0, 1);
        }

        #region ISubject Members

        public struct DataFrame
        {
            public string LayerName;
            public uint XOffset;
            public uint YOffset;
            public uint ZOffset;
            public int[,] Data;
        }

        public void Attach(OnDatabaseChanged observer)
        {
            // TODO: ive noticed some Subscribe methods allow passing of a callback function
            // to call rather than rely on any IObserver "HandleUpdate" interface method
            if (mObservers == null) mObservers = new List<OnDatabaseChanged>();

            mObservers.Add(observer);
        }

        public void Detach(OnDatabaseChanged observer)
        {
            mObservers.Remove(observer);
            if (mObservers.Count == 0) mObservers = null;
        }

        public void Notify(DataFrame dataFrame) // TODO: i think really Notify() method should not be part of ISubject but rather an internal method
        {
            if (mObservers == null) return;
            foreach (OnDatabaseChanged observer in mObservers)
            {
                observer.Invoke(this, dataFrame);
            }
        }
        #endregion

        #region Search
        // productID = 5; // power
        // http://www.gamasutra.com/blogs/RyanCzech/20130927/201163/A_Practical_Guide_to_Constructing_Tubes_on_Foreign_Planets.php
        // drawing procedural pipes could be nice
        // i like the pipes textured on the exterior of the Morena Smuggler model
        public void DoLinkSearch(uint startIndex, Keystone.Portals.Interior.TILE_ATTRIBUTES flag, bool value)
        {
            // if a "link" (eg "powerlink") layer value has changed, we must update the list of consumers for
            // all power producers that touch that link.  Also, is there a way to determine which consumers
            // have power priority distribution?  we'd need some form of "distributor" logic which could be a
            // AI "first officer"
 
            bool includeStartingIndex = value;
            
            // each link is a list of connected tiles
            // breadth search is what we want
            uint[][] links = FindLinks(startIndex, includeStartingIndex);

            // TODO: convert the .TILEMASK_FLAGS flag to a productID
            uint productID = 5; // user_constants.css enum Products 5 = power, 13 = thrust

            // TODO: if a previous link had just one tile and is now deleted, we must still 
            //       update producer's list of consumers if applicable!
            if (links == null) return;
            //System.Diagnostics.Debug.WriteLine("FOUND " + links.Length + " LINKS.");
            List<Entities.Entity> producers = new List<Keystone.Entities.Entity>();
            List<Entities.Entity> consumers = new List<Keystone.Entities.Entity>();

            // find all of the producers and consumer entities adjacent to any tile in a link
            int linkCount = links.Length ;
            
            // for each link
            for (int i = 0; i < linkCount; i++)
            {
            	// for each tile in this current link
            	int tileCount = links[i].Length;
                for (int j = 0; j < tileCount; j++)
                {
                	// key is the flattened index of a tile in this link
                	uint tileIndex = links[i][j];
                	// do we have a floor standing entity that is stored in the dictionary at this current flattened tile index?
                	// NOTE: recall that only one floor standing entity can occupy any particular floor tile
                	Entities.Entity entityOnTile = null;
                	bool found = mFloorEntities.TryGetValue (tileIndex, out entityOnTile);
                	if (found)
                    {	
                		System.Diagnostics.Debug.Assert (entityOnTile.Footprint != null);
                        // is this a producer or a consumer of this particular productID?
                        // note: distribution mode for "links" must be of type 
                        // KeyCommon.Simulation.DistributionType.List 

                        // determine if this found entity produces the productID we are focussed on
                        object tmp = entityOnTile.GetCustomPropertyValue("production");
                        if (tmp != null)
                        {
                            KeyCommon.Simulation.Production[] production = (KeyCommon.Simulation.Production[])tmp;
                            if (production.Length > 0)
                                for (int k = 0; k < production.Length; k++)
                            		// filter list of products to just the productID we are looking for 
                                    if (production[k].ProductID == productID)
                            		{
                            			producers.Add (entityOnTile);
                            			System.Diagnostics.Trace.WriteLine("Production Found - " + production[k].ProductID.ToString());
                            			break; 
                            		}
                        }

                        
                        // determine if this found entity consumes the productID we are focussed on
                        tmp = entityOnTile.GetCustomPropertyValue("consumption");
                        if (tmp != null)
                        {
                            int[] consumption = (int[])tmp;
                            if (consumption.Length > 0)
                                for (int k = 0; k < consumption.Length; k++)
                                    if (consumption[k] == productID)
                            		{
                            			consumers.Add (entityOnTile);
                            			System.Diagnostics.Trace.WriteLine("Consumption Found - " + consumption[k].ToString());
                            			break;
                            		}
                        }
                	}
                }
                
                
                // if we found any producers of this particular productID, update their list of consumers for that productID
                // note: the types of products a component produces cannot change at runtime.  the consumers for each product
                // being produced by a particular producer are stored in a jagged array that is in same order as the list of products.
                // however, i dont like the idea of having to rebuild the entire jagged array just because the links for one type of
                // product being produced has changed
                if (consumers.Count > 0 && producers.Count > 0)
                {
                	string[] consumerIDs = new string[consumers.Count];
                	for (int j = 0; j < consumerIDs.Length; j++)
                		consumerIDs[j] = consumers[j].ID;
                	
                    for (int j = 0; j < producers.Count; j++)
                    {
	                	object tmp = producers[j].GetCustomPropertyValue ("production");
                		if (tmp != null)
                		{
                			KeyCommon.Simulation.Production[] p = (KeyCommon.Simulation.Production[])tmp;
                			for (int k = 0; k < p.Length; k++)
                			{
                				if (p[k].ProductID == productID)
	                			{
	                				p[k].DistributionList = consumerIDs ;
                                    
                                    // TODO: if a consuming device is connected to multiple producers, we should specify
                                    //       within each producer the priority for each of it's connected consumers. 
                                    //       This may entail maintaining a priorty list is smart enough to update
                                    //       itself to remove the associated allocations to consumers when they are removed or to give when added.
                                    //		 Allocation% is how much of a consuming devices demanded power will the producer supply to that consumer.
                                    // debug.assert the producer's distribution type is of type list
                                    // assign all consumers on this link to the producer's consumer distribution list for that product
                                    // since multiple products can be produced by any given component
                                    int[] brokenCode;
			                        producers[j].SetCustomPropertyValue ("production", p, false, false, out brokenCode);
	                			}
                			}
                		}
                    }
                }

                producers.Clear();
                consumers.Clear();

                // TODO: how do we handle any priority of consumers and such?  
                //       a producer should manage priorities of it's consumers itself based on
                //       user assignments, however, this must then still be able to deal with
                //       real-time changing of links after assignments were already made.
                //       we could render a little sub-script "index" icon to show the priority
                //       would be a text drawn on top the icon or next to it, etc

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startIndex">a Tile index, not a Cell.</param>
        /// <param name="includeStartingIndex"></param>
        /// <returns></returns>
        public uint[][] FindLinks(uint startIndex, bool includeStartingIndex)
        {
        	// the algorithm for finding "links" between components here seems like a type of maze following algorithm
            // to find all adjacents as we step through to the next segment 
            // http://channel9.msdn.com/coding4fun/blog/Getting-lost-and-found-with-the-C-Maze-Generator-and-Solver
            // - what we need is a list of all cells (don't we mean tiles?) that are in a network and find all producers and consumers of that link's type
            //   that touch it.  HOWEVER, making links span entire tiles horizontally or vertically might be simpler/faster to update.
            // breadth search is what we want
            // http://www.codeproject.com/Articles/9040/Maze-Solver-shortest-path-finder


            // http://stackoverflow.com/questions/5111645/breadth-first-traversal-using-c-sharp
            //Queue<Node> q = new Queue<Node>();
            //q.Add(root);
            //while (q.Count > 0)
            //{
            //    Node current = q.Dequeue();
            //    if (current == null)
            //        continue;
            //    q.Enqueue(current.Left);
            //    q.Enqueue(current.Right);
            //    // alternatively instead of the above which assumes binary (left and right)
            //    // iterating all children assumes arbitrary number
            //    // however in the case of our grid, we know that we want to enqueue 
            //    // the adjacent tiles in a grid
            //    foreach (var child in node.children)
            //    {
            //       queue.Enqueue(child);
            //    }

            //    DoSomething(current);
            //}
            
            // return a jagged array of all found links where a link contains the list of tile indices that make it up
            Func <uint, uint, uint, bool> condition = (x, y, z) => 
            {
                int data = mFootprintData[x, y, z];

                // TODO: what about LINEs other than POWER? we should pass in the type of LINE we want to search for
                if ((data & (int)Interior.TILE_ATTRIBUTES.LINE_POWER) != 0)
                    return true;

                return false;
            };

            if (includeStartingIndex)
                return new uint[][] 
                { 
                    BreadthFirstSearch(startIndex, condition)
                };
            else
            {
                // we'll search the adjacents of the startIndex seperately as their own roots
                // and then we'll throw out the results for those adjacents that are redundant
                uint[][] temp = new uint[mAdjacentsLookup.Length][];
                for (int i = 0; i < mAdjacentsLookup.Length; i++)
                    temp[i] = BreadthFirstSearch(FlattenTileIndex((uint)mAdjacentsLookup[i].X, (uint)mAdjacentsLookup[i].Y, (uint)mAdjacentsLookup[i].Z), condition);

                // throw out the branches that are redundant by making them null.
                for (int i = 0; i < temp.Length; i++)
                    if (temp[i] != null)
                        for (int j = 1; j < temp.Length ; j++)
                            if (temp[j].ArrayContains (temp[i][0])) 
                            {
                                temp[j] = null;
                                continue; // continue or break?
                            }

                // get the none null count
                int current = 0, count = 0;
                for (int i = 0; i < temp.Length; i++)
                    if (temp[i] != null) count++;

                if (count == 0) return null;

                uint[][] result = new uint[count][];
                for (int i = 0; i < temp.Length; i++)
                    if (temp[i] != null)
                    {
                        result[current++] = temp[i];
                    }

                return result;
            }
        }

        public uint[] BreadthFirstSearch(uint index, Func <uint, uint , uint, bool> condition)
        {
            Queue<uint> history = new Queue<uint>(); // history never dequeues
            Queue<uint> workQueue = new Queue<uint>(); // enqueues and dequeues
            workQueue.Enqueue(index);
            history.Enqueue(index);
            
            while (workQueue.Count > 0)
            {
                // dequeue next tile index
                uint current = workQueue.Dequeue();
                uint x, y, z;
                UnflattenTileIndex(current, out x, out y, out z);

                // process only if it's in bounds
                if (TileIsInBounds(x, y, z))
                {
                    uint adjacentX, adjacentY, adjacentZ;

                    // iterate through all adjacent tiles
                    for (int i = 0; i < mAdjacentsLookup.Length; i++)
                    {
                        adjacentX = x + (uint)mAdjacentsLookup[i].X;
                        adjacentY = y + (uint)mAdjacentsLookup[i].Y;
                        adjacentZ = z + (uint)mAdjacentsLookup[i].Z;

                        // enqueu adjacent tiles that pass condition 
                        if (condition(adjacentX, adjacentY, adjacentZ) == true)
                        {
                            uint adjacentIndex = FlattenTileIndex(adjacentX, adjacentY, adjacentZ);
                            // if this tile has not previously been queued, we can add it
                            if (history.Contains(adjacentIndex) == false)
                            {
                                // ultimately we only enqueue this tile if it passes and that means
                                // it only gets it's own adjacent tiles tested by making it this far
                                workQueue.Enqueue(adjacentIndex);
                                history.Enqueue(adjacentIndex);
                            }
                        }
                    }
                }
            }

            return history.ToArray();
        }
        #endregion

        //internal void CellLocationFromID(int id, out int x, out int y, out int z)
        //{

        //}

        internal bool CellIsInBounds(uint x, uint y, uint z)
        {
            if (x >= mCelledRegion.CellCountX || 
                y >= mCelledRegion.CellCountY || 
                z >= mCelledRegion.CellCountZ ) return false;
            return true;
        }

        internal bool TileIsInBounds(uint x, uint y, uint z)
        {
            if (x < 0 || y < 0 || z < 0 ||
                x >= mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX ||
                y >= mCelledRegion.CellCountY || 
                z >= mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ ) 
                return false;

            return true;
        }

        internal bool TilesAreInBounds(int x, int y, int z, int width, int height)
        {
            int maxDestIndexX = x + width - 1;
            int maxDestIndexZ = z + height - 1;

            if (x < 0 || y < 0 || z < 0 ||
                maxDestIndexX >= mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX ||
                y >= mCelledRegion.CellCountY ||
                maxDestIndexZ >= mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ )
                return false;

            return true;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">X offset in Tiles (not cells)</param>
        /// <param name="y">Y offset in cells</param>
        /// <param name="z">Z offset in Tiles (not cells)</param>
        /// <param name="footprint"></param>
        public void SetFootprint(uint x, uint y, uint z, int[,] footprint)
        {
            // IMPORTANT: Footprint data being applied here must ALREADY have had
            //            any rotations applied and must already enabled/disabled 
            //            relevant bits.  Here we change the entire 32 bit value
            //            at each position. 
            int footprintWidth = footprint.GetLength(0);
            int footprintHeight = footprint.GetLength(1);

            for (uint i = 0; i < footprintWidth; i++)
                for (uint j = 0; j < footprintHeight; j++)
                {
                    // TEMP: todo: remove this after i determine all is goood with OBSTACLE removal and COMPONENT adding
                    if ((footprint[i, j] & 1 << 14) != 0) //0 << 14 used to be for OBSTACLE flag which ive since removed`. must remove from all existing prefabs such a doors, chairs, bunks
                        System.Diagnostics.Debug.WriteLine("Error");

                    mFootprintData[x + i, y, z + j] = footprint[i, j];
                }
            Notify(x, y, z, footprint);
        }

        private void Notify(uint x, uint y, uint z, int[,] footprint)
        {
            DataFrame frame = new DataFrame();
            frame.Data = footprint;
            frame.XOffset = x;
            frame.YOffset = y;
            frame.ZOffset = z;
            Notify(frame);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">X offset in Tiles (not cells)</param>
        /// <param name="y">Y offset in cells</param>
        /// <param name="z">Z offset in Tiles (not cells)</param>
        public int[,] GetFootprint(uint x, uint y, uint z, uint width, uint depth)
        {
            int[,] result = new int[width, depth];
            // TODO: We need to return null if the x,y,z location is OOB
            // TODO: additionally, we should return footprint with all empty data for tile locations that are not OOB but are NOT IN_BOUNDS
            for (int i = 0; i < width; i++)
                for (int j = 0; j < depth; j++)
                    result[i, j] = mFootprintData[x + i, y, z + j];

            return result;
        }


        #region IPageableTVNode Members
        public int TVIndex
        {
            get { throw new NotImplementedException(); }
        }

        public bool TVResourceIsLoaded
        {
            get { throw new NotImplementedException(); }
        }

        public string ResourcePath
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public PageableNodeStatus PageStatus
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void LoadTVResource()
        {
            throw new NotImplementedException();
        }

        public void SaveTVResource(string filepath)
        {
            throw new NotImplementedException();
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        #endregion

        public object GetMapData(string layerName)
        {
            switch (layerName)
            {
                case ("boundaries"):
                    return null; // mCells;
                case ("footprint"):
                    return mFootprintData;
                default:
                    return null;
            }
        }


        public object GetMapValue(string layerName, uint index)
        {
            if (layerName == "boundaries") // note: "boundaries" exists in both GetMapValue overloads
            {
                uint x, y, z;
                // here index is a CellID
                mCelledRegion.UnflattenCellIndex(index, out x, out y, out z);
                return GetMapValue(layerName, x, y, z);
            }
            else if (layerName == "boundaries")
            {
                throw new NotImplementedException();
            }
            else if (layerName == "tile style")
            {
                Segment segment;
                // here index is a CellID
                if (mTileStructures.TryGetValue(index, out segment) == false) return null;
                return segment.Style;
            }
            else if (layerName == "wall style")
            {
                Segment segment;
                // here index is an EdgeID (TODO: what if in the future we rewrite EdgeID's to be a combination of
                // lo/hi word's that are the start and end tile's? that would allow diagonals pretty easily.  Our limitation
                // would be uint.Max / 2
                // 4,294,967,295 / 2 = 2,147,483,647.5 tiles total in the ship which i think is enough to give us
                // 16x16 tiles in a ship that is 1000 x 8 x 1000 cells 
                // That's not quite big enough.  Unless we computed the indices to always be in multiples of TilesPerCellX, TilesPerCellZ
                // so that we knew an edgeStartIndex of 1 = tile Index 16
                // http://stackoverflow.com/questions/1873402/is-there-a-nice-way-to-split-an-int-into-two-shorts-net
                // - for diagonals i really do like the idea of just storying start/end tiles in hi/lo of uint32
                //   we do have to check for endianess though because if we store in a saved file x,y tile in little endian
                //   restoring those on big endian will reverse the x and y components to be y,x.
                //   - we could have helper functions to get the x,y tile in a 2d vector2i and internally it can use
                //   BitConverter.IsLittleEndian to determine which value is the x and y
                //   and based on what the save data format was.. which perhaps we can vary?

                // TODO: note even our total tile indices cannot exceed 4,294,967,295!!  That means 16x16 ~1000 x 17 x 1000
                // We would have to link celledRegion's to make bigger
                if (mEdgeStructures.TryGetValue(index, out segment) == false) return null;
                return segment.Style;
            }
            else if (layerName == "physical properties") // TODO: mEdgeStructures for "edge physical properties"
            // TODO: mTileStructures for "floor/ceiling physical properties"
            {
                Segment segment;
                if (mEdgeStructures.TryGetValue(index, out segment) == false) return null;

                return segment.PhysicalProperties;
            }
            else if (layerName == "floorentity")
            {
                Entities.Entity entity;
                if (mFloorEntities.TryGetValue(index, out entity) == false) return null;
                return entity;
            }
            else if (layerName == "npc")
            {
                Entities.Entity npc;
                if (mNPCs.TryGetValue(index, out npc) == false) return null;
                return npc;
            }
            else throw new NotImplementedException("CellMap.GetMapValue() -- Unsupported layer '" + layerName + "'");
        }

        public object GetMapValue(string layerName, uint x, uint y, uint z)
        {

            // NOTE: Segments are not placed in Cells\Tiles because segments are typically SHARED
            //       between cells as structural segments (floors/ceilings and walls)
            //       that divide or partition adjacent cells.
            //       Another advantage is that cells which have no segments don't have to 
            //       maintain a reference to any segment struct.  These cells  
            //       may  not be tiled but are still created as they may just be open air and
            //       propogate objects and gas and elements like heat, fire or radiation but never
            //       themselvse have segment structures so it would be a waste to have a
            //       an empty array ref in all those cells.
            //       However, we may decide to set
            //       a flag in the cell indicating if it has segment and/or tile structures
            //       or anything else.
            // NOTE: if called externally, we reate segments here
            //       but when deserializing this Layer_SetValue() method is not called
            //       so we don't have any problem with attempting to recreate segments that
            //       were already deserialized.
            if (layerName == "boundaries")
            {
                return (mFootprintData[x * mCelledRegion.TilesPerCellX, y, z * mCelledRegion.TilesPerCellZ] & (int)Interior.TILE_ATTRIBUTES.BOUNDS_IN) != 0;
            }
            else if (layerName == "footprint") // traversable true/false sub-tiles 
            {
                return GetFootprint(x * mCelledRegion.TilesPerCellX, 
                                    y, 
                                    z * mCelledRegion.TilesPerCellZ,
                                    mCelledRegion.TilesPerCellX, mCelledRegion.TilesPerCellZ);
            }
            else if (layerName == "floorentity")
            {
                uint tileIndex = FlattenTileIndex (x, y, z);
                return mFloorEntities[tileIndex];
            }
            else throw new NotImplementedException("CellMap.GetMapValue() -- Unsupported layer '" + layerName + "'");
            
        }

        // TODO: i should ensure setting of these values can not be done directly by client EXE.  should be done
        // through command processor
        /// <summary>
        /// 
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="x">Cell Offset X (not tile offset)</param>
        /// <param name="y">Cell Offset Y</param>
        /// <param name="z">Cell Offset Z (not tile offset)</param>
        /// <param name="value"></param>
        public void SetMapValue(string layerName, uint x, uint y, uint z, object value)
        {
            switch (layerName)
            {
                case "footprint":
                case "powerlink":
                case "floorentity":
                case "npc":
                    if (TileIsInBounds (x, y, z) == false) throw new ArgumentOutOfRangeException();
                    break;
                case "boundaries":
                case "floor style":;
                    break;
                case "tile style":

                    // x,y,z bounds tests of this kind only relevant for cells, NOT for edges!          
                    if (CellIsInBounds (x, y, z) == false) throw new ArgumentOutOfRangeException();
                    break;
                default:
                    throw new Exception("CellMap.SetMapValue() - Unexpected layerName '" + layerName + "'");
            }

            


            if (layerName == "footprint")
            {
                // TODO: here if x,y,z is not in tile coords, this is wrong.  
                
                SetFootprint(x, y, z, (int[,])value);
            }
            else if (layerName == "powerlink")
            {
                // TODO: the value we are setting does not indicate whether we are erasing or setting this value
                //       which is what we'd require if we instead used "powerlink" "netlink" etc all as seperate
                bool linkValue = (bool)value;
                int[,] footprint = GetFootprint(x, y, z, 1, 1);
                if (linkValue)
                    footprint[0, 0] |= (int)Interior.TILE_ATTRIBUTES.LINE_POWER;
                else
                    footprint[0, 0] &= ~(int)Interior.TILE_ATTRIBUTES.LINE_POWER;

                // set the new modified footprint values
                SetFootprint(x, y, z, footprint);
            }
            // TILEMASK_FLAGS.BOUNDS_IN is first bit in footprint layer.
            // boundaries is a special case because whether a cell is flagged as BOUNDS_IN or 
            // BOUNDS_OUT determines whether the cell within the mCells[] array is instanced
            // or not.
            else if (layerName == "boundaries")
            {
                mHashCodeDirty = true;
                // IMPORTANT: defining an inbound tile does NOT create any tile segment structure.  
                // It also does NOT set any STRUCTURE flags on the footprint.  Only BOUNDS_IN.
                // Tiles\Tile Segments must always be explicitly placed by user now.
                bool boundaryValue = (bool)value;
                uint tileX = x * mCelledRegion.TilesPerCellX;
                uint tileZ = z * mCelledRegion.TilesPerCellZ;

                int[,] footprint = GetFootprint(tileX, y, tileZ, mCelledRegion.TilesPerCellX, mCelledRegion.TilesPerCellZ);

                if (boundaryValue == true) // in bounds
                {
                    // enable BOUNDS_IN flag
                    for (int i = 0; i < mCelledRegion.TilesPerCellX; i++)
                        for (int j = 0; j < mCelledRegion.TilesPerCellZ; j++)
                            footprint[i, j] |= (int)Interior.TILE_ATTRIBUTES.BOUNDS_IN;
                }
                else // out of bounds (script is called after that which will unapply footprint)
                {
                    // disable BOUNDS_IN flag
                    for (int i = 0; i < mCelledRegion.TilesPerCellX; i++)
                        for (int j = 0; j < mCelledRegion.TilesPerCellZ; j++)
                            footprint[i, j] &= ~(int)Interior.TILE_ATTRIBUTES.BOUNDS_IN;
                }

                // set the new modified footprint values
                SetFootprint(tileX, y, tileZ, footprint);
            }
            else if (layerName == "floorentity")
            {
                uint tileIndex = FlattenTileIndex (x, y, z);
                SetMapValue(layerName, tileIndex, value);
            }
        }

        /// <summary>
        /// Set Value from exterior runtime command by user (or perhaps programatic by script)
        /// but is NOT called when internally deserializing (restoring) a saved floorplan.
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="value"></param>
        /// <remarks>
        /// No validation occurs here.  Validation of commands always occurs in Command Processor.
        /// Here it is strictly setting of values.
        /// </remarks>
        public void SetMapValue(string layerName, uint index, object value)
        {
            mHashCodeDirty = true;
            // NOTE: Segments are not placed in Cells because segments are typically SHARED
            //       amongst cells.  Thus these structural segments (floors/ceilings and walls)
            //       are not really part of cells, they are dividers or partitions.
            //       Another advantage is that cells which have no segments don't have to 
            //       evenmaintain a reference.  These cells are 
            //       may  not be tiled but are still created. They may just be open air and
            //       propogate objects and gas and elements like heat, fire or radiation but never
            //       themselvse have segment structures so it would be a waste to have a
            //       an empty array ref in all those cells.
            //       However, we may decide to set
            //       a flag in the cell indicating if it has segment and/or tile structures
            //       or anything else.
            // NOTE: if called externally, we reate segments here
            //       but when deserializing this Layer_SetValue() method is not called
            //       so we don't have any problem with attempting to recreate segments that
            //       were already deserialized.
            switch (layerName)
            {
                case "boundaries":
                case "tile style":
                case "wall style":
                    break;   
                case "physical properties":
                case "floorentity":
                case "npc":
                case "powerlink":
                    break;
                default:
                    throw new Exception ("make sure x,y,z is not needed in this overloaded method");
            }

            if (layerName == "boundaries")
            {
                // unflatten the tile index and verify the X and Z are proper modulus of TilesPerCellX and TilesPerCellZ
                uint x, y, z;
                mCelledRegion.UnflattenCellIndex(index, out x, out y, out z);
                SetMapValue(layerName, x, y, z, value);
            }
            else if (layerName == "powerlink")
            {
                uint x, y, z;
                Utilities.MathHelper.UnflattenIndex (index,  mCelledRegion.TilesPerCellX * mCelledRegion.CellCountX, 
                                                  mCelledRegion.TilesPerCellZ * mCelledRegion.CellCountZ,  
                                                  out x, out y, out z);
                SetMapValue(layerName, x, y, z, value);
            }
            else if (layerName == "tile style")
            {
                EdgeStyle style = (EdgeStyle)value;
                Segment segment;
                bool existing = false;
                existing = mTileStructures.TryGetValue(index, out segment);
                if (existing == false)
                    segment = new Segment(index);

                // for floor/ceilings, texture paths contain the atlas index
                segment.InteriorAtlasTextureIndex = style.FloorAtlasIndex; 
                segment.ExteriorAtlasTextureIndex = style.CeilingAtlasIndex;
                segment.Style = style;

                if (existing == true)
                {
                    // if this segment already exists and the style we want to add is a "null" style then
                    // delete this tile segment
                    if (style.StyleID == -1)
                        mTileStructures.Remove(index);
                }
                else if (style.StyleID != -1) // segment does NOT exist already, but if we're attempting to delete a non existing segment, we certainly do not want to add it
                    mTileStructures.Add(index, segment);

            }
            else if (layerName == "wall style")
            {
                // No validation occurs here.  Validation of commands always occurs in Command Processor.
                
                
                // Here it is strictly setting of values.
                CellEdge e = CellEdge.CreateEdge(index, mCelledRegion.CellCountX, mCelledRegion.CellCountY, mCelledRegion.CellCountZ);
                EdgeStyle style = (EdgeStyle)value; // TODO: only covering one of the styles here.  Why not store the MinimeshMap and not just the Segment in our structures?
                Segment segment;
                bool existing = false;
                existing = mEdgeStructures.TryGetValue(index, out segment);
                if (existing == false)
                    segment = new Segment(index);

                //segment.InteriorAtlasTextureIndex = styles[0].TextureIndex;
                //segment.ExteriorAtlasTextureIndex = styles[1].TextureIndex;
                segment.Style = style;


                if (existing == true)
                {
                    // if this segment exists and the new style has both halves of the mesh path null
                    // delete this wall segment
                    if (style == null || style.StyleID == -1)
                        mEdgeStructures.Remove(index);
                }
                else if (style.StyleID != -1) // segment does NOT exist already, but if we're attempting to delete a non existing segment, we certainly do not want to add it
                    mEdgeStructures.Add(index, segment);

                //System.Diagnostics.Debug.WriteLine("CellMap.SetMapValue() - EdgeStructures Count == " + mEdgeStructures.Count.ToString());
                // TODO: we should remove structures that have styles
                // where both sides are null meshes right?
            }
            else if (layerName == "physical properties")
            {
                // edge segments are initially placed by selection of a style.  cannot
                // apply physical properties if no segment yet exists.
            }
            else if (layerName == "floorentity")
            {
                // this value i think may end up being complete array? how else do you add/remove 
                // individual entities?
                Entities.Entity entity;
                bool existing = mFloorEntities.TryGetValue(index, out entity);

                if (existing == true)
                    if (value == null)
                        // we are clearing the existing entity from the tile
                        mFloorEntities.Remove(index);
                    else
                        // TODO: I should probably just return here or throw up a message box or rules reminder to the user
                        throw new Exception("Two floor entities cannot occupy the same tile.  Existing entity must first be removed.");
                else if (value != null)
                {
   	            	// TODO: storing this reference consumes a lot of memory... but for now we'll leave it and see how things go
                    entity = (Entities.Entity)value;
                    mFloorEntities.Add(index, entity);
                    
                }
                // TODO: how do you stack crates?
                //       - easiest solution is to say that one crate must accept the other as a child
                //         and that crate can determine what can be stacked ontop of it. This solves footprint issues
                //       - and also allows for lower crate to "drop" carried crate
                // TODO: note, this can simply how we deal with things like guns or items or plates and cups
                //        that are stacked ontop of items like shelves, tables, etc.

            }
            else if (layerName == "npc")
            {
                // this value i think may end up being complete array? how else do you add/remove 
                // individual entities?
                Entities.Entity npc;
                bool existing = mNPCs.TryGetValue(index, out npc);

                if (existing == true)
                    if (value == null)
                        // we are clearing the existing NPC from the tile
                        mNPCs.Remove(index);
                    else
                        throw new Exception("Two npc's entities cannot occupy the same tile.  Existing NPC must first be removed.");
                else
                    mNPCs.Add(index, npc);


                // TODO: how do you have one NPC carrying another?
                //       - easiest solution is to say that one NPC must accept the other as a child entity node
                //         and that NPC can determine what it can / cannot carry.
                // TODO: note, this can simply how we deal with things like guns or items held by npc's too.

            }
        }


        private void UnflattenTileIndex(uint index, out uint x, out uint y, out uint z)
        {

            Utilities.MathHelper.UnflattenIndex(index,
                                            mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX,
                                            mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ,
                                            out x, out y, out z);

        }

        private uint FlattenTileIndex (uint x, uint y, uint z)
        {
            return Utilities.MathHelper.FlattenCoordinate(x, y, z, 
                         mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX, 
                         mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ);
        }

        #region Cell Data IO
        internal void ReadCellData(string filename, uint cellCountX, uint cellCountY, uint cellCountZ)
        {

            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException();
            if (System.IO.File.Exists(filename) == false) throw new System.IO.FileNotFoundException();

            System.IO.FileInfo info = new System.IO.FileInfo(filename);

            int MIN_HEADER_LENGTH = 8; // magic + version
            if (info.Length < MIN_HEADER_LENGTH) throw new System.IO.InvalidDataException();

            uint count = 0;

            System.Diagnostics.Debug.WriteLine("CellMap.ReadCellData() - Begin read.");

            using (System.IO.BinaryReader reader = new System.IO.BinaryReader(System.IO.File.Open(filename, System.IO.FileMode.Open)))
            {
                // verify this if a proper file
                if (FILE_MAGIC != reader.ReadUInt32()) throw new System.IO.InvalidDataException();
                uint version = reader.ReadUInt32();
                if (version != 1 && version != FILE_VERSION_LATEST) throw new System.IO.InvalidDataException();

 
                // get the number of INBOUND cells we'll be instancing since it is NOT equal to the 
                // total size of the mCells array
                count = reader.ReadUInt32();
                System.Diagnostics.Debug.Assert(cellCountX == mCelledRegion.CellCountX &&
                                                cellCountY == mCelledRegion.CellCountY &&
                                                mCelledRegion.CellCountZ == cellCountZ);

                // load the cells but DO NOT try to run any logic here.  
                // This is strictly deserialization of the data structure.
                // In fact, tilemask data is not even read here because we can
                // rebuild that from loading of the structure.
                for (uint i = 0; i < count; i++)
                {
                    uint location = reader.ReadUInt32();
                    SetMapValue("boundaries", location, true);
                }
                

                // read in the total number of TileSegments
                count = reader.ReadUInt32();
                mTileStructures = new Dictionary<uint, Segment>((int)count);
                for (int i = 0; i < count; i++)
                {
                    Segment segment = new Segment();
                    segment.Read(reader);
                    // TODO: i think when writing over floor tiles with different textures we are not
                    //       replacing the existing but adding a new one?
                    try
                    {
                        mTileStructures.Add(segment.Location, segment);
                    }
                    catch 
                    {
                    }
                }

                // read in the total number of EdgeSegments
                count = reader.ReadUInt32();
                mEdgeStructures = new Dictionary<uint, Segment>((int)count);
                for (int i = 0; i < count; i++)
                {
                    Segment segment = new Segment();
                    segment.Read(reader);
                    // TODO: since this format has been changing, if we read in a segment
                    // that appears to be null for mesh, we should ignore it and not add it to edgeStructures
                    // in fact, if we want to just ignore all walls because the old implementation of footprints is
                    // no longer any good we can do that too
                    // TODO: we need to read/write corner styles too don't we?
                    if (segment.Style == null || (string.IsNullOrEmpty(segment.Style.Prefab) && string.IsNullOrEmpty(segment.Style.Prefab)))
                        continue;
                    
                    mEdgeStructures.Add(segment.Location, segment);
                }
            }

            System.Diagnostics.Debug.WriteLine("CellMap.ReadCellData() - Read completed.");
        }

        internal void WriteCellData(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                throw new System.IO.FileNotFoundException();
            }


            System.Diagnostics.Debug.WriteLine("CellMap.WriteCellData() - Begin write.");
           // return;
            // NOTE: we don't save the tielmask data because that can be reconstructed from
            //       the edge data and components we deserialize.  Indeed, trying to deserialize components
            //       would be problematic if we had to prevent validation & apply of their footprints during that time
            //       since they'd already have been loaded here.
            using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(System.IO.File.Open(filepath, System.IO.FileMode.Create)))
            {
                // but for path, we dont know vehicle prefab path so cant save inside vehicle prefab 
                // MAGIC = 4 bytes
                // VERSION = 4 bytes
                // #LAYERS = 4 bytes
                // write magic and version
                writer.Write(FILE_MAGIC);
                writer.Write(FILE_VERSION_LATEST);


                // write the flattened index for each Cell that is inbounds
                // count total number of instanced cells and write that number
                long countPosition = writer.BaseStream.Position;
                uint count = 0;
                writer.Write(count); // temporary count of 0 while we determine actual count

                // boundaries
                // write the locations (flattened indices) of each cell that is INBOUNDS
                for (uint k = 0; k < mCelledRegion.CellCountY; k++)
                    for (uint i = 0; i < mCelledRegion.CellCountX; i++)
                        for (uint j = 0; j < mCelledRegion.CellCountZ; j++)
                        {
                            uint x = i * mCelledRegion.TilesPerCellX;
                            uint z = j * mCelledRegion.TilesPerCellZ;
                            //if (mFootprintData[x, k, z] != 0)
                            if ((mFootprintData[x,k,z] & (int)Interior.TILE_ATTRIBUTES.BOUNDS_IN) != 0)
                            {
                                uint index = mCelledRegion.FlattenCell(i, k, j);
                                writer.Write(index);
                                count++;
                            }
                        }

                // now we can overwrite the temp placeholder count with actual count value 
                // at the desired position in the file
                writer.BaseStream.Seek(countPosition, System.IO.SeekOrigin.Begin);
                writer.Write(count);
                writer.Seek(0, System.IO.SeekOrigin.End);

                // write the count of the Tile Segments
                writer.Write((uint)mTileStructures.Count);
                foreach (Segment segment in mTileStructures.Values)
                {
                    segment.Write(writer);
                }

                // write the count of the Edge Segments
                writer.Write((uint)mEdgeStructures.Count);
                foreach (Segment segment in mEdgeStructures.Values)
                {
                    segment.Write(writer);
                }

                System.Diagnostics.Debug.WriteLine("CellMap.WriteCellData() - Write Completed.");
            }
        }

        //private Cell[, ,] ReadCellData(string filename)
        //{

        //    if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException();
        //    if (System.IO.File.Exists(filename) == false) throw new System.IO.FileNotFoundException();

        //    System.IO.FileInfo info = new System.IO.FileInfo(filename);

        //    int MIN_HEADER_LENGTH = 8; // magic + version
        //    if (info.Length < MIN_HEADER_LENGTH) throw new System.IO.InvalidDataException();


        //    using (System.IO.BinaryReader reader = new System.IO.BinaryReader(System.IO.File.Open(filename, System.IO.FileMode.Open)))
        //    {
        //        // verify this if a proper file
        //        if (FILE_MAGIC != reader.ReadUInt32()) throw new System.IO.InvalidDataException();
        //        if (FILE_VERSION != reader.ReadUInt32()) throw new System.IO.InvalidDataException();

        //        // get the number of cells we'll be instancing since it is NOT equal to the 
        //        // total size of the mCells array
        //        uint count = reader.ReadUInt32();

        //        // load the cells but DO NOT try to run any logic here.  
        //        // This is strictly deserialization of the data structure.
        //        Cell[, ,] cells = new Cell[CellCountX, CellCountY, CellCountZ];
        //        for (uint i = 0; i < count; i++)
        //        {
        //            Cell c = new Cell();
        //            c.Read(reader);

        //            uint x, y, z;
        //            UnflattenCellIndex(c.Location, out x, out y, out z);

        //            cells[x, y, z] = c;
        //        }

        //        return cells;
        //    }
        //}

        //private void WriteCellData(string filepath)
        //{
        //    if (string.IsNullOrEmpty(filepath))
        //    {
        //        throw new System.IO.FileNotFoundException();
        //    }

        //    using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(System.IO.File.Open(filepath, System.IO.FileMode.Create)))
        //    {
        //        // but for path, we dont know vehicle prefab path so cant save inside vehicle prefab 
        //        // MAGIC = 4 bytes
        //        // VERSION = 4 bytes
        //        // #LAYERS = 4 bytes
        //        // write magic and version
        //        writer.Write(FILE_MAGIC);
        //        writer.Write(FILE_VERSION);

        //        // count total number of instanced cells and write that number
        //        long countPosition = writer.BaseStream.Position;
        //        uint count = 0;
        //        writer.Write(count); // temporary count of 0 while we determine actual count

        //        // TODO: where do we write the
        //        //mTileStructures and mEdgeStructures since those are just partitions and can be
        //        // referenced by multiple cells
        //        // and what is the difference between loading those structures which then
        //        // affects the visuals and the cells, and just loading the cell data itself?
        //        // my point is that the cell data i think can be rebuilt by just loading the
        //        // segments

        //        // write the cells
        //        for (uint k = 0; k < CellCountY; k++)
        //            for (uint i = 0; i < CellCountX; i++)
        //                for (uint j = 0; j < CellCountZ; j++)
        //                {

        //                    if (mCells[i, k, j] != null)
        //                    {
        //                        mCells[i, k, j].Write(writer);
        //                        count++;
        //                    }
        //                }

        //        // now we can overwrite the temp placeholder count with actual count value 
        //        // at the desired position in the file
        //        writer.BaseStream.Seek(countPosition, System.IO.SeekOrigin.Begin);
        //        writer.Write(count);
        //        writer.Seek(0, System.IO.SeekOrigin.End);
        //    }
        //}
        #endregion

        private bool mHashCodeDirty = true;
        private int mHashCode;
        // TODO: here we dont really have "layers" and this is predominantly
        // because it is for boolean array.  We can fix this on a per bit basis for
        // something like "footprint".
        // but mostly these hashcodes are designed to be more like "enabled" or "disabled"
        // and not for specific data.  
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Array.GetHashCode() does reference equality, not value equality
        /// so we need to roll our own otherwise the hashcode returned will
        /// always be the same.  i.e that is why we can't just do 
        /// 'return mCells.GetHashCode();'
        /// </remarks>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public int GetHashCode(string layerName)
        {
            // TODO: use an array of hashcodes for each layer?
            //       and a corresponding mHashCodeDirty array?
            if (!mHashCodeDirty) return mHashCode;

            mHashCodeDirty = false;

            // http://stackoverflow.com/questions/6832139/gethashcode-from-booleans-only
            unchecked // unchecked will skip overflow check
            {
                mHashCode = 17; // start with prime
                int prime_multiplier = 23;
                int elementHashValue = 0;

                //switch (layerName)
                //{
                //    case "boundaries": // TODO: i think this is strictly a footprint thing now, it's not seperate
                        //for (uint i = 0; i < mCelledRegion.CellCountX; i++)
                        //    for (uint j = 0; j < mCelledRegion.CellCountY; j++)
                        //        for (uint k = 0; k < mCelledRegion.CellCountZ; k++)
                        //        {
                        //            elementHashValue = 0;
                        //            if (mCells[i, j, k] != null)
                        //                elementHashValue = 1;

                        //            mHashCode = mHashCode * prime_multiplier + elementHashValue;
                        //        }
                    //    break;
                    //case "footprint":
                        for (uint i = 0; i < mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX; i++)
                            for (uint j = 0; j < mCelledRegion.CellCountY; j++)
                                for (uint k = 0; k < mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ; k++)
                                {
                                    elementHashValue = 0;
                                    if ((mFootprintData[i, j, k] & 1 << 0) != 0)
                                        elementHashValue = 1;

                                    mHashCode = mHashCode * prime_multiplier + elementHashValue;
                                }
                   //    break;
                   // default:
                   //     break;
                //}

                

                return mHashCode;
            }
        }

        const int BITS_PER_INT32 = 32;
        public int GetHashCode(int layer)
        {
            unchecked // unchecked will skip overflow check
            {
                mHashCode = 17; // start with prime
                int prime_multiplier = 23;

                for (uint i = 0; i < mCelledRegion.CellCountX * mCelledRegion.TilesPerCellX; i++)
                    for (uint j = 0; j < mCelledRegion.CellCountY; j++)
                        for (uint k = 0; k < mCelledRegion.CellCountZ * mCelledRegion.TilesPerCellZ; k++)
                        {
                            int elementHashValue = 0;
                            int data = mFootprintData[i, k, j];       
                            
                            // test all 32 bits in the int
                            int bit = 0;  
                            for (int n = 0; n < BITS_PER_INT32; n++)
                            {
                                if ((data & bit) == bit)
                                {
                                    elementHashValue = 1;
                                    mHashCode = mHashCode * prime_multiplier + elementHashValue;
                                }
                                bit = 1 << n; 
                            }
                        }
            }
            return mHashCode;
        }
    }


    //// TODO: if one day we want to use memory mapped files and implement some system
    //// where huge chunks of the world are dynamically loaded, i think really rather than
    //// try to page data within a particular cursor view in the DB, it's best to just implement
    //// MULTIPLE celledRegion's within an Interior!  This is a much better method IMO and
    //// allows designers to create the boundaries between celledRegions at logical places.
    //public struct Record
    //{
    //    public Header Header;
    //    public int[] Data;
    //}

    //public struct Header // 304 bytes
    //{
    //    public string Name; // fixed length 256 bytes when read or written to file
    //    public uint DataLength;   // 4 bytes
    //    public uint DataOffset; // 4 bytes

    //    public static uint Size { get { return 256 + 4 + 4; } }
    //}



    //internal class LayerDatabase
    //{
    //    // TODO: for a Minecraft style huge worlds, we really end up having to stop usign XML for
    //    //       content storage potentially.  And we possibly need to link celledRegion's together
    //    //       and page them in as we do now with space regions. Or to have celledRegion's contain
    //    //       "chunks".  Minecraft is able to use integers for positions of world items and doesnt 
    //    //       run into floating point precision issues though.
    //    // TODO: "chunks" would allow us to break up the data but share a single coordinate system which
    //    //       is preferred for vehicles that move
    //    //       a "celledRegion" is already a "chunk" of isolated, pageable content though.
    //    //       so why not just use them as such where we can group celledRegion's together to create a
    //    //       larger region just as we do with Zones?
    //    // TODO: this is something i should try one day with .net 4.0 as a research project to see
    //    //       how big of a tile based world i can make and what strategy or combination of strategies
    //    //       i have to employ including dealing with NPCs in those areas.  For instance, potentially
    //    //       with memory mapping, i could update 1 unloaded region at a time using a broad phase
    //    //       AI/NPC/Economic/Tactical simulation.



    //    Record[] mRecords;
    //    Dictionary<string, IMapLayer> mLayers;
    //    uint MAGIC;
    //    const uint VERSION = 1;
    //    string mFullFilePath;
    //    string mTempFilePath; 

    //    // difference between a layer and cell data is that a layer is 1 bit of data typically 
    //    // and discribes all cells, not just in bound cells that are instanced.
    //    // but the question now becomes, is this even necessary?  In/Out of bounds seems really only
    //    // real layer im using.  So why not just use a "CellDatabase" instead that no longer deals in
    //    // map layers but instead cell data where only instanced cells are saved/written.
    //    // WAIT, one thing.  Our first in/out bounds layer actually describes to us which cells to instance
    //    // and thus which we'll be reading in.  Without that, we would need to store prior to each
    //    // written cell the location before we could know where to place it in the array.  Then again
    //    // we do in fact store the location.  I think this is probably superior then.  There is no need for
    //    // a "layer"
    //    public LayerDatabase()
    //    {
    //        mLayers = new Dictionary<string, IMapLayer>();
    //        Keystone.Utilities.JenkinsHash.Hash("KGB_CELLED_REGION_LAYER_DATABASE", ref MAGIC);
    //    }


    //    public IMapLayer Layers(string layerName)
    //    {
    //        if (mLayers == null) return null;
    //        IMapLayer foundLayer;

    //        if (mLayers.TryGetValue(layerName, out foundLayer))
    //            return foundLayer;

    //        return null;
    //    }

    //    public int LayersCount
    //    {
    //        get { if (mLayers == null) return 0; return mLayers.Count; }
    //    }

    //    public void Add(IMapLayer layer)
    //    {
    //        mLayers.Add(layer.Name, layer);

    //    }

    //    #region IO
    //    public void Create(string filename)
    //    {
    //        mRecords = new Record[0];

    //        mTempFilePath = Keystone.IO.XMLDatabase.GetTempFileName();
    //        mFullFilePath = filename;

    //        Save();

    //        Open(mFullFilePath, System.IO.FileMode.Open);
    //    }

    //    public void Open(string filename, System.IO.FileMode mode)
    //    {
    //        mFullFilePath = filename;
    //        if (string.IsNullOrEmpty(mFullFilePath)) throw new ArgumentNullException();
    //        if (System.IO.File.Exists(mFullFilePath) == false) throw new System.IO.FileNotFoundException();

    //        System.IO.FileInfo info = new System.IO.FileInfo(filename);

    //        int MIN_HEADER_LENGTH = 8; // magic + version
    //        if (info.Length < MIN_HEADER_LENGTH) throw new System.IO.InvalidDataException();


    //        using (System.IO.BinaryReader reader = new System.IO.BinaryReader(System.IO.File.Open(mFullFilePath, System.IO.FileMode.Open)))
    //        {

    //            // verify this if a proper file
    //            if (MAGIC != reader.ReadUInt32()) throw new System.IO.InvalidDataException();
    //            if (VERSION != reader.ReadUInt32()) throw new System.IO.InvalidDataException();

    //            uint layersCount = reader.ReadUInt32();


    //            if (layersCount > 0)
    //            {
    //                mRecords = new Record[layersCount];

    //                // READ THE HEADERS
    //                for (int i = 0; i < mRecords.Length; i++)
    //                {
    //                    Header header;
    //                    header.Name = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(256)).Trim('\0');
    //                    // note: if we know it's always BitArrayLayer and never IntArrayLayer, we dont
    //                    //       need elementcount and type.  for now we will see if we can avoid using
    //                    //       any layer type other than BitArray. This means even connectivity will
    //                    //       use 6 seperate BitArrayLayers instead of a single ByteArrayLayer for example.
    //                    // header.ElementCount = reader.ReadUInt32();
    //                    // header.ElementType = reader.ReadUInt32(); // bit vs int32
    //                    header.DataLength = reader.ReadUInt32();
    //                    header.DataOffset = reader.ReadUInt32();
    //                    if (i == 0)
    //                        System.Diagnostics.Debug.Assert(header.DataOffset == Header.Size * mRecords.Length);

    //                    mRecords[i].Header = header;
    //                }

    //                // READ AND CREATE LAYERS
    //                for (int i = 0; i < mRecords.Length; i++)
    //                {
    //                    BitArrayLayer layer = new BitArrayLayer(mRecords[i].Header.Name);
    //                    layer.Read(reader);
    //                    mLayers.Add(layer.Name, layer);
    //                }
    //            }
    //        }
    //    }



    //    public void Save()
    //    {
    //        if (string.IsNullOrEmpty(mFullFilePath))
    //        {
    //            throw new System.IO.FileNotFoundException();
    //        }

    //        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(System.IO.File.Open(mFullFilePath, System.IO.FileMode.Create)))
    //        {
    //            // but for path, we dont know vehicle prefab path so cant save inside vehicle prefab 
    //            // MAGIC = 4 bytes
    //            // VERSION = 4 bytes
    //            // #LAYERS = 4 bytes
    //            // write magic and version
    //            writer.Write(MAGIC);
    //            writer.Write(VERSION);

    //            uint count = (uint)mLayers.Count;
    //            writer.Write(count);

    //            mRecords = new Record[count];
    //            int currentRecord = 0;
    //            foreach (IMapLayer layer in mLayers.Values)
    //            {
    //                // ------------------------BeginHeaders (NOTE: Headers are Fixed Length)
    //                // LAYER_NAME = 256
    //                // LAYER_DATA_LENGTH = 4 bytes
    //                // LAYER_DATA_OFFSET = 4 bytes
    //                // ------------------------EndHeaders

    //                // write all headers first.  they are fixed size so computing
    //                // the dataoffset is easy
    //                // first record data offset starts at end of all headers
    //                uint dataOffset = count * Header.Size;

    //                Header header;
    //                header.Name = layer.Name;
    //                // header.ElementCount 
    //                // header.ElementType // bit vs int32
    //                header.DataLength = layer.SizeOnDisk();
    //                header.DataOffset = dataOffset;

    //                // write header
    //                writer.Write(Helpers.Functions.StringToByteArray(header.Name, 256));
    //                writer.Write(header.DataLength);
    //                writer.Write(header.DataOffset);

    //                mRecords[currentRecord++].Header = header;
    //                dataOffset += header.DataLength;
    //            }

    //            foreach (IMapLayer layer in mLayers.Values)
    //            {
    //                // ------------------------BeginData
    //                // LAYER_DATA = BitArrayLayer.Write()
    //                // ------------------------EndData
    //                // write data after headers are all written
    //                layer.Write(writer);
    //            }
    //        }
    //    }
    //    #endregion
    //}

    // TODO: use a generic instead?  cuz our get/set values vary
    // TODO: cant really because each generic instance ends up being a very specific type
    //       that is not cast-able to a base type that is shared (i mean other than 'object')
    public interface IMapLayer
    {
        // see LibNoise
        string Name { get; }
        bool GetValue(int index);
        void SetValue(int index, bool value);
        void Write(System.IO.BinaryWriter writer);
        void Read(System.IO.BinaryReader reader);
        uint SizeOnDisk();
    }

    //// connectivity is 6 bits per tile
    //internal class IntArrayLayer : IMapLayer
    //{
    //    public void Write(System.IO.BinaryWriter writer)
    //    { 
    //    }

    //    public void Read(System.IO.BinaryReader reader)
    //    { }
    //}

    // TODO: what if all layers are BitArrayLayer's and 
    //       some layers simply have more bits per element?
    //       Connectivity for instance has 6 bits.  So could we structure
    //       Connectivity as an array of 6  BitArrayLayers? This way our flatten/unflatten works
    //       "connectivity_positive_x", "connectivity_negative_x"
    //       "connectivity_positive_y", "connectivity_negative_y"
    //       "connectivity_positive_z", "connectivity_negative_z"
    internal class BitArrayLayer : IMapLayer
    {
        string mLayerName;
        System.Collections.BitArray mBits;
        //int SizeX;
        //int SizeY;
        //int SizeZ;
        int mHashCode;
        bool mHashCodeDirty;

        internal BitArrayLayer(string name) // used when we will be Reading mBits from stream
        {
            mLayerName = name;
        }

        internal BitArrayLayer(string name, int numElements)
            : this(name, (uint)numElements)
        {
        }


        internal BitArrayLayer(string name, uint numElements)
        {
            mLayerName = name;
            mHashCodeDirty = true;
            mBits = new System.Collections.BitArray((int)numElements);

            int bytesInMegabyte = 1024000; // 1000000;
            int bitsPerByte = 8;
            float megabytes = (mBits.Length / (float)(bitsPerByte) / (float)bytesInMegabyte);
            System.Diagnostics.Debug.WriteLine(string.Format("BitArrayLayer.Ctor() - 1bit tile data containing {0} elements and consuming {1} megabytes.", mBits.Length, megabytes));
        }

        //internal BitArrayLayer(uint cellCountX, uint cellCountY, uint cellCountZ)
        //{
        //    mHashCodeDirty = true;
        //    SizeX = (int)cellCountX;
        //    SizeY = (int)cellCountY;
        //    SizeZ = (int)cellCountZ;
        //    mBits = new System.Collections.BitArray(SizeX * SizeY * SizeZ);

        //    int bytesInMegabyte = 1024000; // 1000000;
        //    int bitsPerByte = 8;
        //    float megabytes = (mBits.Length / (float)(bitsPerByte) / (float)bytesInMegabyte);
        //    System.Diagnostics.Debug.WriteLine(string.Format("BitArrayLayer.Ctor() - 1bit tile data containing {0} elements and consuming {1} megabytes.", mBits.Length, megabytes));
        //}

        public string Name { get { return mLayerName; } }


        // TODO: how do we update floors (since not managed by HUD) 
        // when tile masks have changed and we need to update uv and/or vertices to collapse/uncollapse?
        public void SetValue(int index, bool value)
        {
            mBits[index] = value;
            mHashCodeDirty = true;
        }

        //public void SetValue(uint x, uint y, uint z, bool value)
        //{
        //    // TODO: would the index and flattened/unflattened indices here always correlate?
        //    //       they might!  
        //    int cellsPerFloor = SizeX * SizeZ;
        //    int cellsPerRow = SizeX;
        //    int index = (int)y * cellsPerFloor + (SizeX * (int)z) + (int)x;
        //    mBits[index] = value;
        //    mHashCodeDirty = true;
        //}

        public bool GetValue(int index)
        {
            return mBits[index];
        }

        // TODO: get/set simply are different depending on the layer.  and its not just the return
        // it's the arguments as well.
        //public bool GetValue(uint x, uint y, uint z) 
        //{
        //    int cellsPerFloor = SizeX * SizeZ;
        //    int cellsPerRow = SizeX;
        //    int index = (int)y * cellsPerFloor + (SizeX * (int)z) + (int)x; 
        //    return mBits[index]; 
        //}

        public override int GetHashCode()
        {
            if (!mHashCodeDirty) return mHashCode;

            mHashCode = this.GetHashCode(mBits);
            mHashCodeDirty = false;

            // TODO: should we trigger a save here of the database?
            // TODO: having to re-open xmldb everytime here is stupid
            //       maybe the save takes place in FormMain.Commands.cs
            return mHashCode;
        }

        // http://stackoverflow.com/questions/3125676/generating-a-good-hash-code-gethashcode-for-a-bitarray
        private int GetHashCode(BitArray array)
        {
            UInt32 hash = 17;
            int bitsRemaining = array.Length;
            foreach (int value in array.GetInternalValues())
            {
                UInt32 cleanValue = (UInt32)value;
                if (bitsRemaining < 32)
                {
                    //clear any bits that are beyond the end of the array
                    int bitsToWipe = 32 - bitsRemaining;
                    cleanValue <<= bitsToWipe;
                    cleanValue >>= bitsToWipe;
                }

                hash = hash * 23 + cleanValue;
                bitsRemaining -= 32;
            }
            return (int)hash;
        }

        public uint SizeOnDisk()
        {
            return mBits.SizeOnDisk();
        }

        public void Read(System.IO.BinaryReader reader)
        {
            mBits = BitArrayExtensions.Read(reader);
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            mBits.Write(writer);
        }
    }

    // Goal agents
    // Persuer agents
    // Path agents (floor tiles, ladders)
    // Obstacle agents (walls)
    // for now i think these 3 diciontaries is all we need to be concerned about.

    /// <summary>
    /// This layer defines connectivity between cells.  One connectivity layer can be for entity
    /// pathfinding.  Different entity types can have different layers such as for really
    /// tiny alien infestation invaders, they can crawl through ventiliation as well as normal
    /// pathing.
    /// Other layers can define visibility, electrical, ventiliation, plumbing
    /// </summary>
    //public class Connectivity : IMapLayer
    //{
    //    // an array of 8 bit flag values where one 8bit byte for each cell
    //    // where each bit enable/disable correlates to ability to traverse/not traverse
    //    // to neighboring cell
    //    public uint[] Values;

    //    public bool GetValue(uint x, uint y, uint z)
    //    {
    //        return true; // Values[x * y * z];
    //    }

    //    public void SetValue(uint x, uint y, uint z, bool value)
    //    { }
    //}

    ///// <summary>
    ///// This layer details the dynamic entities 
    ///// </summary>
    //public class DynamicStacks : IMapLayer
    //{
    //    #region IMapLayer Members

    //    public float GetValue(uint x, uint y, uint z)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}

    ///// <summary>
    ///// These define walls, floors and ceilings and other permanent floorplan
    ///// structures like doors and windows
    ///// </summary>
    //public class StaticStacks : IMapLayer
    //{

    //    #region IMapLayer Members

    //    public float GetValue(uint x, uint y, uint z)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}

}
