using System;
using System.Collections.Generic;
using Keystone.Entities;
using Keystone.Extensions;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Types;

namespace Keystone.Portals
{
    public class ZoneRoot : Root
    {

    	public uint StructureLevelsHigh;
    	public uint StructureGroundFloorIndex;
    	
        public uint RegionsAcross;
        public uint RegionsHigh;
        public uint RegionsDeep;

        public float StartX;
        public float StopX;
        public float StartY;
        public float StopY;
        public float StartZ;
        public float StopZ;

        public ZoneRoot(string id, uint regionsAcross,
                         uint regionsHigh, uint regionsDeep,
                         float regionDiameterX, float regionDiameterY, float regionDiameterZ,
                          uint octreeDepth = 5,
                         uint structureLevelsHigh = 32)
            // NOTE: In base call we pass not total of ZoneRoot's dimensions but of just one
            // child Zone.  Instead in the Init() here we recompute the HalfWidth, HalfHeight and HalfDepth
            : base(id, regionDiameterX , regionDiameterY, regionDiameterZ, octreeDepth)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException();
            if (regionsAcross == 0) regionsAcross = 1; // 0 is impossible to have.  User most likely meant 1.
            if (regionsHigh == 0) regionsHigh = 1;
            if (regionsDeep == 0) regionsDeep = 1;

            RegionsHigh = regionsHigh;
            RegionsAcross = regionsAcross;
            RegionsDeep = regionsDeep;


            StructureLevelsHigh = structureLevelsHigh;
            
            ComputeGroundFloor(StructureLevelsHigh);
		            	
            Init();
        }

        internal ZoneRoot(string id)
            : base(id)
        {
        }


        private void Init()
        {
            GetStartStop(out StartX, out StopX, RegionsAcross);
            GetStartStop(out StartY, out StopY, RegionsHigh);
            GetStartStop(out StartZ, out StopZ, RegionsDeep);

            // MUST compute HalfWidth, HalfHeight, HalfDepth to include number of zones across, high, depth
            // whereas with a regular Root node, there's no need for that.
            HalfWidth = RegionDiameterX * RegionsAcross * .5d;
            HalfHeight = RegionDiameterY * RegionsHigh * .5d;
            HalfDepth = RegionDiameterZ * RegionsDeep * .5d;

            SetChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty, Keystone.Enums.ChangeSource.Self);
        }

        private void ComputeGroundFloor (uint structureLevelsHigh)
        {
        	// compute ground floor as middle of total possible levels so we can dig as deep as we can build\stack upwards
			// eg: int(sizeY / 2) is always above ground and any indices less than that are below ground)
			// - basements, catcombs, dungeons, caves, fissures
			// - upstairs, attics inside castles, mesas, hills, moutains, mountain caves and crevices 
			// - and in all of these unique domains at different depths and atltitudes, different creatures
			// A level -32 zombie, demon, or skeleton for example will be insanely tough.
		    StructureGroundFloorIndex = structureLevelsHigh / 2; 
        }
     
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[4 + tmp.Length];
            tmp.CopyTo(properties, 4);

            properties[0] = new Settings.PropertySpec("regionsacross", RegionsAcross.GetType().Name);
            properties[1] = new Settings.PropertySpec("regionshigh", RegionsHigh.GetType().Name);
            properties[2] = new Settings.PropertySpec("regionsdeep", RegionsDeep.GetType().Name);
			properties[3] = new Settings.PropertySpec("structurelevelshigh", StructureLevelsHigh.GetType().Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = RegionsAcross;
                properties[1].DefaultValue = RegionsHigh;
                properties[2].DefaultValue = RegionsDeep;
                properties[3].DefaultValue = StructureLevelsHigh;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "regionsacross":
                        RegionsAcross = (uint)properties[i].DefaultValue;
                        break;
                    case "regionshigh":
                        RegionsHigh = (uint)properties[i].DefaultValue;
                        break;
                    case "regionsdeep":
                        RegionsDeep = (uint)properties[i].DefaultValue;
                        break;
                   case "structurelevelshigh":
                        StructureLevelsHigh = (uint)properties[i].DefaultValue;
                        ComputeGroundFloor (StructureLevelsHigh);
                        break;
                }
            }
            Init();
        }

        /// <summary>
        /// Computes the start index and end index offset for each axis so that the overall
        /// universe is centered and that there's rougly same negative axis zones as positive
        /// </summary>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="length"></param>
        internal static void GetStartStop(out float start, out float stop, uint length)
        {
            if (length == 1)
            {
                start = 0;
                stop = 0;
            }
            // for odd numbers the center zone/tile will be at 0 and start and stop
            // will be same on both sides (one positive, one negative)
            else if (length % 2 == 1) // odd number
            {
                start = -((length - 1) / 2f);
                stop = -start;
            }
            else
            {
                // for even numbers, we will always have one more negative value than positive
                // so for length = 2 we will have START tile at -1, and the STOP at 0.  Thus
                //        ----------              ------        ---         -----  ---
                // for tiled structures to keep world origin at 0,0,0, we will compute the positions of the 
                // tiles as being +halfWidth, +halfHeight, +halfLength (assuming all 3 axis have length = 2 or even)
                // and this will put the origin on the border of the -1 and 0 cells since it will have shifted
                // the origin along the positive axis.  Draw this on paper and you'll see it makes sense.
         //       start = (int)-(length / 2);
         //       stop = -(start) - 1;

                // for even numbers it starts at 0.5 and increments by 1.  So -3.5, -2.5, -1.5, 0.5, 1.5, 2.5, 3.5
                start = -(length / 2f);
                start += .5f;
                stop = -start;
            }
        }

        public Vector3d GlobalCoordinatesToZoneCoordinates(double x, double y, double z, out string regionID)
        {
            if (!BoundingBox.Contains(x, y, z)) throw new ArgumentOutOfRangeException("Root does not contain coordinates, therefore neither do any children.");

            Vector3d regionCoords = new Vector3d(x, y, z);


            // let's say that the universe x axis ranges -1000, +1000 in size
            // and each zone is -500, +500.
            // the first zone would be centered in galactic coords at -500
            // the second zone would be centered in galactic coords at 500.
            // so let's say we have a galactic coord of 600, how do we 
            // algorithmicly obtain the 2nd region and a region coord of 100.
            // or if the galactic coord is -800, it would like in 1st region
            // with a region coord of -300


            // a slow way is to get the galactic bounds of every region and iterate
            for (int i = 0; i < RegionsAcross; i++)
            {
                for (int j = 0; j < RegionsHigh; j++)
                {
                    for (int k = 0; k < RegionsDeep; k++)
                    {
                        // construct bounding box of zone at this offset
                        BoundingBox box = GetChildZoneSize();
                        float offsetX = StartX + i;
                        float offsetY = StartY + j;
                        float offsetZ = StartZ + k;

                        Vector3d zoneTranslation = new Vector3d(offsetX * box.Width,
                                           offsetY * box.Height,
                                           offsetZ * box.Depth);

                        Vector3d half = new Vector3d(box.Width / 2d, box.Height / 2d, box.Depth / 2d);
                        Vector3d min = zoneTranslation - half;
                        Vector3d max = zoneTranslation + half;
                        box = new BoundingBox(min, max);

                        if (box.Contains(x, y, z))
                        {
                            regionID = GetZoneName(i, j, k);

                            regionCoords -= zoneTranslation;

                            return regionCoords;
                        }
                    }
                }
            }

            throw new ArgumentOutOfRangeException();
        }

        public BoundingBox GetChildZoneSize()
        {
            // each zone region has the exact same region relative bounding box 
            double halfWidth = RegionDiameterX * .5d;
            double halfHeight = RegionDiameterY * .5d;
            double halfDepth = RegionDiameterZ * .5d;
            Vector3d min = new Vector3d(-halfWidth, -halfHeight, -halfDepth);
            Vector3d max = new Vector3d(halfWidth, halfHeight, halfDepth);
            BoundingBox box = new BoundingBox(min, max);
            return box;
        }

        /// <summary>
        /// Checks if a subscript is valid and does not exceed the bounds of our array of child Zones.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private bool InBounds(int x, int y, int z)
        {
//            return (x >= StartX && x <= StopX &&
//                    y >= StartY && y <= StopY &&
//                    z >= StartZ && z <= StopZ);

			return (x > 0 && x < RegionsAcross &&
                    y > 0  && y < RegionsHigh &&
                    z > 0 && z < RegionsDeep);
        }

        // With 3x3x3 galaxy the integer division rounds down and does yield 1,1,1 which is center subscript of 0-2, 0-2, 0-2.
        // Thus for odd numbered count it yields an index that has equal number of indices on both the negative and positive sides
        // With 2x2x2 galaxy (even numbered counts where there simply cannot be equal number of indices on either side) 
        // it selects the index in the positive axis and yields 1,1,1  which is by design. 
        public void GetZoneCenterSubscripts(out int subscriptX, out int subscriptY, out int subscriptZ)
        {
            subscriptX = GetCenterSubscript((int)RegionsAcross);
            subscriptY = GetCenterSubscript((int)RegionsHigh);
            subscriptZ = GetCenterSubscript((int)RegionsDeep);

        }

        private int GetCenterSubscript(int count)
        {
            if (count == 1) return 0;
            if (count % 2 == 1) return (count - 1) / 2;
            return count / 2;
        }
        /// <summary>
        /// Returns a child Zone ID that is composed of the ZoneRoot's ID + a prefix 
        /// representing the region[,,] array subscript 
        /// </summary>
        /// <param name="subscriptX">subscript X</param>
        /// <param name="subscriptY">subscript Y</param>
        /// <param name="subscriptZ">subscript Z</param>
        /// <returns></returns>
        public string GetZoneName(int subscriptX, int subscriptY, int subscriptZ)
        {
            const string DELIMITER = ",";

            return string.Format("{0}{1}{2}{3}{4}{5}{6}", typeof(Keystone.Portals.Zone).Name, DELIMITER,
                                 subscriptX, DELIMITER,
                                 subscriptY, DELIMITER,
                                 subscriptZ);
        }

        
        public string[] GetAllZoneNames()
        {
            List<string> regions = new List<string>();
            
//            for (float i = StartX; i <= StopX; i++)
//                for (float j = StartY; j <= StopY; j++)
//                    for (float k = StartZ; k <= StopZ; k++)
//                        AddRegionName(ref regions, i, j, k);

			for (int i = 0; i < RegionsAcross; i++)
                for (int j = 0; j < RegionsHigh; j++)
                    for (int k = 0; k < RegionsDeep; k++)
                        AddRegionName(ref regions, i, j, k);
                        
            return regions.ToArray();
        }

        public static void GetZoneSubscriptsFromName(string id, out int subscriptX, out int subscriptY, out int subscriptZ)
        {
            const string DELIMITER = ",";
            string[] segments = id.Split(new string[] { DELIMITER }, StringSplitOptions.None);
            string typeName = segments[0];
            subscriptX = int.Parse(segments[1]);
            subscriptY = int.Parse(segments[2]);
            subscriptZ = int.Parse(segments[3]);
        }

        internal Region[] GetAdjacents(int x, int y, int z)
        {

            // verify the x,y,z is in bounds and add that zone first
            if (InBounds(x, y, z))
            {
                List<Region> regions = new List<Region>();

                // add the current zone
                //regionNames.Add(ref regionNames, x,y,z);
                AddRegion(ref regions, x, y, z);


                // add the zone immediately to the west
                if (InBounds(x - 1, y, z)) AddRegion(ref regions, x - 1, y, z);
                // add the zone immediately to the east
                if (InBounds(x + 1, y, z)) AddRegion(ref regions, x + 1, y, z);
                // add the zone immediately to the south
                if (InBounds(x, y - 1, z)) AddRegion(ref regions, x, y - 1, z);
                // add the zone immediately to the north
                if (InBounds(x, y + 1, z)) AddRegion(ref regions, x, y + 1, z);
                // add the zone immediately in front
                if (InBounds(x, y, z - 1)) AddRegion(ref regions, x, y, z - 1);
                // add the zone immediately behind
                if (InBounds(x, y, z + 1)) AddRegion(ref regions, x, y, z + 1);


                // for the non cardinal (diagonal) directions
                if (InBounds(x + 1, y + 1, z)) AddRegion(ref regions, x + 1, y + 1, z); // +y (+x)
                if (InBounds(x + 1, y - 1, z)) AddRegion(ref regions, x + 1, y - 1, z); // -y (+x) 
                if (InBounds(x + 1, y, z - 1)) AddRegion(ref regions, x + 1, y, z - 1); // -z (+x)  
                if (InBounds(x + 1, y, z + 1)) AddRegion(ref regions, x + 1, y, z + 1); // +z (+x)  

                if (InBounds(x - 1, y + 1, z)) AddRegion(ref regions, x - 1, y + 1, z); // -x (+y)
                if (InBounds(x - 1, y - 1, z)) AddRegion(ref regions, x - 1, y - 1, z); // -x (-y) 
                if (InBounds(x - 1, y, z + 1)) AddRegion(ref regions, x - 1, y, z + 1); // +z (-x)  
                if (InBounds(x - 1, y, z - 1)) AddRegion(ref regions, x - 1, y, z - 1); // -z (-x)  

                if (InBounds(x, y + 1, z + 1)) AddRegion(ref regions, x, y + 1, z + 1); // +y (+z)
                if (InBounds(x, y + 1, z - 1)) AddRegion(ref regions, x, y + 1, z - 1); // +y (-z) 
                if (InBounds(x, y - 1, z + 1)) AddRegion(ref regions, x, y - 1, z + 1); // -y (+z)  
                if (InBounds(x, y - 1, z - 1)) AddRegion(ref regions, x, y - 1, z - 1); // -y (-z)  

                // the furthest corner diagonals
                if (InBounds(x + 1, y + 1, z + 1)) AddRegion(ref regions, x + 1, y + 1, z + 1); // +x (+y) +z
                if (InBounds(x + 1, y + 1, z - 1)) AddRegion(ref regions, x + 1, y + 1, z - 1); // +x (+y) -z
                if (InBounds(x + 1, y - 1, z + 1)) AddRegion(ref regions, x + 1, y - 1, z + 1); // +x (-y) +z
                if (InBounds(x + 1, y - 1, z - 1)) AddRegion(ref regions, x + 1, y - 1, z - 1); // +x (-y) -z 

                if (InBounds(x - 1, y + 1, z + 1)) AddRegion(ref regions, x - 1, y + 1, z + 1); // -x (+y) +z
                if (InBounds(x - 1, y + 1, z - 1)) AddRegion(ref regions, x - 1, y + 1, z - 1); // -x (+y) -z
                if (InBounds(x - 1, y - 1, z + 1)) AddRegion(ref regions, x - 1, y - 1, z + 1); // -x (-y) +z
                if (InBounds(x - 1, y - 1, z - 1)) AddRegion(ref regions, x - 1, y - 1, z - 1); // -x (-y) -z 

                return regions.ToArray();
            }
            return null;
        }

        
        
        internal string[] GetAdjacentZoneNamesBeyondRange (int x, int y, int z, uint range)
        {
        	// RANGE IS NOT INCLUSIVE OF THE (X,Y,Z) ZONE COORDS PASSED IN.  A RANGE OF '1'
        	// WILL THUS ALWAYS INCLUDE THE IMMEDIATE ADJACENTS AND A RANGE OF '0' WILL INCLUDE
        	// JUST THE CURRENT ADJACENT
        	if (range < 0) throw new ArgumentOutOfRangeException ("ZoneRoot.GetAdjacentZoneNamesBeyondRange()");
        	     	
        	// similar to below but instead of returning names within range, it only
        	// returns the names OUTSIDE of the range 
        	// (eg if range is 4 zones, it will only return zones 5+ away)
        	string[] inclusive = GetAdjacentZoneNames (x, y, z, range);
        	
        	string[] outerInclusive = GetAllZoneNames();
        	
        	if (inclusive == null)
        		return outerInclusive;
        	
        	if (outerInclusive == null) throw new ArgumentException();
        	
        	if (inclusive.Length == outerInclusive.Length) return null;
        	
        	
        	// return only those names that only exist in the outer list and not both
        	int count = outerInclusive.Length - inclusive.Length;
        	string[] result = new string[count];
        	count = 0;
        	for (int i = 0; i < outerInclusive.Length; i++)
        	{
        		if (inclusive.ArrayContains (outerInclusive[i]) == false)
        			result[count++] = outerInclusive[i];
        	}
        	
        	return result;
        	
        }
        
        // NOTE: this version of GetAdjacentZoneNames() i think is better than all the others
        internal string[] GetAdjacentZoneNames (int x, int y, int z, uint range)
        {
        	// RANGE IS --NOT INCLUSIVE-- OF THE (X,Y,Z) ZONE COORDS PASSED IN.  A RANGE OF '1'
        	// WILL THUS ALWAYS INCLUDE THE IMMEDIATE ADJACENTS AND A RANGE OF '0' WILL INCLUDE
        	// JUST THE CURRENT ADJACENT
        	BoundingBox box = new BoundingBox (x - range, 
        	                                   y - range, 
        	                                   z - range, 
        	                                   x + range, 
        	                                   y + range, 
        	                                   z + range);
        	List<string> regions = new List<string>();

			for (int i = 0; i < RegionsAcross; i++)
                for (int j = 0; j < RegionsHigh; j++)
                    for (int k = 0; k < RegionsDeep; k++)
						if (box.Contains (i , j, k))
	                        AddRegionName(ref regions, i, j, k);
			
            return regions.ToArray();
    
        }
                
        // TODO: how do we deal with zone's within zones like inside of ships?  does that have to follow portal rules where the camera must traverse
        // to those regions unless we "jump" to a specific region in which case we already know implicitly where the camera is placed?
        // what if i go to the "zones are connected by portals" method instead?  the downside is there i would need allot of portals loaded
        // for each zone off the main zone to know which ones to load, but then there is no traversal from the main zone, it requires jumps
        internal List<string> GetAdjacentZoneNames(int x, int y, int z)
        {

            // verify the x,y,z is in bounds and add that zone first
            if (InBounds(x, y, z))
            {
                List<string> regionNames = new List<string>();

                // add the current zone
                AddRegionName(ref regionNames, x, y, z);

                // add the zone immediately to the west
                if (InBounds(x - 1, y, z)) AddRegionName(ref regionNames, x - 1, y, z);
                // add the zone immediately to the east
                if (InBounds(x + 1, y, z)) AddRegionName(ref regionNames, x + 1, y, z);
                // add the zone immediately to the south
                if (InBounds(x, y - 1, z)) AddRegionName(ref regionNames, x, y - 1, z);
                // add the zone immediately to the north
                if (InBounds(x, y + 1, z)) AddRegionName(ref regionNames, x, y + 1, z);
                // add the zone immediately in front
                if (InBounds(x, y, z - 1)) AddRegionName(ref regionNames, x, y, z - 1);
                // add the zone immediately behind
                if (InBounds(x, y, z + 1)) AddRegionName(ref regionNames, x, y, z + 1);


                // for the non cardinal (diagonal) directions
                if (InBounds(x + 1, y + 1, z)) AddRegionName(ref regionNames, x + 1, y + 1, z); // +y (+x)
                if (InBounds(x + 1, y - 1, z)) AddRegionName(ref regionNames, x + 1, y - 1, z); // -y (+x) 
                if (InBounds(x + 1, y, z - 1)) AddRegionName(ref regionNames, x + 1, y, z - 1); // -z (+x)  
                if (InBounds(x + 1, y, z + 1)) AddRegionName(ref regionNames, x + 1, y, z + 1); // +z (+x)  

                if (InBounds(x - 1, y + 1, z)) AddRegionName(ref regionNames, x - 1, y + 1, z); // -x (+y)
                if (InBounds(x - 1, y - 1, z)) AddRegionName(ref regionNames, x - 1, y - 1, z); // -x (-y) 
                if (InBounds(x - 1, y, z + 1)) AddRegionName(ref regionNames, x - 1, y, z + 1); // +z (-x)  
                if (InBounds(x - 1, y, z - 1)) AddRegionName(ref regionNames, x - 1, y, z - 1); // -z (-x)  

                if (InBounds(x, y + 1, z + 1)) AddRegionName(ref regionNames, x, y + 1, z + 1); // +y (+z)
                if (InBounds(x, y + 1, z - 1)) AddRegionName(ref regionNames, x, y + 1, z - 1); // +y (-z) 
                if (InBounds(x, y - 1, z + 1)) AddRegionName(ref regionNames, x, y - 1, z + 1); // -y (+z)  
                if (InBounds(x, y - 1, z - 1)) AddRegionName(ref regionNames, x, y - 1, z - 1); // -y (-z)  

                // the furthest corner diagonals
                if (InBounds(x + 1, y + 1, z + 1)) AddRegionName(ref regionNames, x + 1, y + 1, z + 1); // +x (+y) +z
                if (InBounds(x + 1, y + 1, z - 1)) AddRegionName(ref regionNames, x + 1, y + 1, z - 1); // +x (+y) -z
                if (InBounds(x + 1, y - 1, z + 1)) AddRegionName(ref regionNames, x + 1, y - 1, z + 1); // +x (-y) +z
                if (InBounds(x + 1, y - 1, z - 1)) AddRegionName(ref regionNames, x + 1, y - 1, z - 1); // +x (-y) -z 

                if (InBounds(x - 1, y + 1, z + 1)) AddRegionName(ref regionNames, x - 1, y + 1, z + 1); // -x (+y) +z
                if (InBounds(x - 1, y + 1, z - 1)) AddRegionName(ref regionNames, x - 1, y + 1, z - 1); // -x (+y) -z
                if (InBounds(x - 1, y - 1, z + 1)) AddRegionName(ref regionNames, x - 1, y - 1, z + 1); // -x (-y) +z
                if (InBounds(x - 1, y - 1, z - 1)) AddRegionName(ref regionNames, x - 1, y - 1, z - 1); // -x (-y) -z 

                return regionNames;
            }
            return null;
        }

        public override void AddChild(Entity child)
        {
            // NOTE: On the client, Zone's are only ever paged in and are not just loaded all at once
            //       when the Scene is loaded from disk.
            // NOTE: Zone's are Added to the ZoneRoot in ClientPager.Update() via super.Apply(r)
            if (child is Zone)
            {

            }
            base.AddChild(child);
        }


        private void AddRegion(ref List<Region> regions, int x, int y, int z)
        {
            regions.Add((Region)Repository.Get(GetZoneName(x, y, z)));
        }

        private void AddRegionName(ref List<string> regionNames, int x, int y, int z)
        {
            regionNames.Add(GetZoneName(x, y, z));
        }

        internal override bool ChildEntityBoundsFail(Entity child, out Region newParentRegion)
        {
            newParentRegion = this; // initialize
           // throw new Exception("This must be overriden"); 
            return false;
        }
    }
}
