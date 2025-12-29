using System;
using System.Collections.Generic;
using KeyCommon.Traversal;
using Keystone.Collision;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Extensions;
using Keystone.Resource;
using Keystone.Types;

namespace Keystone.SceneNodes
{
    /// <summary>
    /// Shareable node with ID = the encoded string of the footprint data.
    /// </summary>
    public class CellFootprint3D : Node
    {
        private int[][,] mData;
        public int[][,] Data { get { return mData; } } // read only!

        public int Width(int level)
        {
            if (mData == null || mData.GetLength(0) < level - 1) throw new ArgumentOutOfRangeException();
            if (mData[level] == null || mData[level].GetLength(0) < 1) throw new ArgumentOutOfRangeException();
            return mData[level].GetLength(0);
        }

        public int Depth(int level)
        {
            if (mData == null || mData.GetLength(0) < level - 1) throw new ArgumentOutOfRangeException();
            if  (mData[level] == null || mData[level].GetLength(1) < 1) throw new ArgumentOutOfRangeException();
            return mData[level].GetLength(1);
        }

        /// <summary>
        /// CellFootprint nodes do not serialize.  They are resources
        /// stored in the parent Entity's "footprint" property which is stored
        /// as a string of compressed encoded data.
        /// Changing the footprint's data in the Editor will result in a new
        /// CellFootprint node being used _after_ the user "saves" the changes.
        /// They can be prompted to save changed when they attempt to close if
        /// they haven't already.  
        /// </summary>
        /// <param name="id"></param>
        private CellFootprint3D(string encodedData)
            : base(encodedData)
        {
            if (string.IsNullOrEmpty(encodedData)) throw new ArgumentNullException("CellFootprint3D.Ctor() - Footprint data must at least contain dimensions and initialized data with 0's.");
            Shareable = true;
            Serializable = false;

            mData = Decode(encodedData);
        }

        public static CellFootprint3D Create(int[][,] data)
        {
            if (data == null) throw new ArgumentNullException("CellFootprint.Ctor() - Footprint data must at least contain dimensions and initialized data with 0's.");

            // determine the encoded id for this data
            string encodedID = Encode(data);

            return Create(encodedID);
        }


        public static CellFootprint3D Create(string encodedData)
        {
            if (string.IsNullOrEmpty(encodedData)) throw new ArgumentNullException();

            CellFootprint3D fp = (CellFootprint3D)Repository.Get(encodedData);
            if (fp != null) return fp;

            fp = new CellFootprint3D(encodedData);
            return fp;
        }

        private static bool Collide(ModeledEntity entity, Ray r, KeyCommon.Traversal.PickParameters parameters)
        {
            Model[] models = entity.SelectModel(SelectionMode.Collision, -1);
            // TODO: apply _parameters.PickSearchType to limit scope if applicable
            if (models != null)
                for (int i = 0; i < models.Length; i++)
                    if (models[i].Geometry != null && models[i].Geometry.TVResourceIsLoaded)
                    {
                        // we must compute modelspace ray for each unique model to take into account any different scaling or rotations
                        // that exist independantly on each model.
                        Ray msRay = Helpers.Functions.GetModelSpaceRay(r, entity.Region, models[i], entity.Region);

                        PickResults result =
                            models[i].Geometry.AdvancedCollide(msRay.Origin, msRay.Origin + (msRay.Direction * 1000d),
                                               parameters);

                        if (result.HasCollided) return true;
                    }

            return false;
        }

        public static int[][,] Decode(string encodedData)
        {
            if (string.IsNullOrEmpty(encodedData)) throw new ArgumentNullException();
            // decode to get the uncompressed byte stream
            byte[] data = encodedData.Explode();

            if (data == null) throw new Exception("CellFootprint3D.Decode() - Footprint data invalid.");
            const int bytesInInt32 = 4;
            const int sizeOfInt16 = 2; // size in bytes
            int start = 0;

            // first 2 bytes contains the number of floor levels in this jagged array of multidemnsional array
            ushort height;
            height = BitConverter.ToUInt16(data, start); start += sizeOfInt16;
            int[][,] footprint = new int[height][,];

            // next set of ushort's contain the width and depth of each level of the multidimensional array
            // followed by the data they contain
            for (int i = 0; i < height; i++)
            {
                ushort width, depth;

                width = BitConverter.ToUInt16(data, start); start += sizeOfInt16;
                depth = BitConverter.ToUInt16(data, start); start += sizeOfInt16;

                footprint[i] = new int[width, depth];

                for (int j = 0; j < width; j++)
                    for (int k = 0; k < depth; j++)
                    {
                        int startIndex = start + (k * width * bytesInInt32) + (j * bytesInInt32);
                        int tmp = BitConverter.ToInt32(data, startIndex);
                        footprint[i][j, k] = tmp;
                    }
            }

            return footprint;
        }

        //public static string Encode(int width, int height)
        //{
        //    return Encode(new int[width, height]);
        //}1

        //public static string Encode(uint width, uint height)
        //{
        //    return Encode(new int[width, height]);
        //}

        public static string Encode(int[][,] footprintData)
        {
            if (footprintData == null) throw new ArgumentNullException();
            const int bytesInInt32 = 4;
            const int sizeOfInt16 = 2;

            ushort height = (ushort)footprintData.GetLength(0);
            byte[] data = null;

            // for each level, get the widthCout and depthCount so we can write them to the header
            // then we can properly initialize the data byte array that will hold all the data so we can encode it to a string
            // that we then use as the "id" name of this CellFootprint3d node.
            ushort[] widths = new ushort[height];
            ushort[] depths = new ushort[height];

            int dataSize = 0;
            for (int i = 0; i < height; i++)
            {
                // TODO: Should we enforce that widths and depths be the same across all levels?
                // because if we don't enforce this rule, when we go to Apply/UnApply Occupy/UnOccupy tile
                // data, we wont know where the x, z offset is for upper levels of the footprint jagged array. 
                // Because the x,z offset we supply, is the lowest x,z tile location, not the center (and remember, center cannot be a 
                // tile location because the center can be inbetween tiles on x or z axis.
                // I think this tells us that in some cases, if we need the upper level to be larger than
                // is necessary for the lower level, then we simply need to make the lower level as wide or deep
                // as necessary for the upper level.  Then we just don't paint any footprint data on the tiles we dont need.
                // Perhaps we will keep seperate width and depth values in the header in case one day we want to change
                // how this works - allowing different dimensions for differnet levels and supplying seperate x,z offsets 
                // for those cases when we apply/unapply, occupy/unoccupy levels in a loop where we recompute the x,z offsets for each level.
                // That should be possible because we compute the x,z start location based on mouse position and odd/even width/depth which
                // could do independantly for each level.
                widths[i] = (ushort)footprintData[i].GetLength(0);
                depths[i] = (ushort)footprintData[i].GetLength(1);
                System.Diagnostics.Debug.Assert(widths[i] > 0 && depths[i] > 0);

                dataSize += widths[i] * depths[i];
            }

            int header_size = (int)sizeOfInt16 * 3; // 
            int start = 0;


            for (int i = 0; i < height; i++)
            {
                // to make it easier to decode, we create a header
                // that tells us the number of levels followed by the width & depth ahead of each, and the actual data
                // for each width*depth combination, appended to the end.  This is also
                // safer since we can verify the data length and not have any buffer overflows

            }

            // TODO: applying/unapply occuping/unoccupying tiles and test "IsPlaceable" and such
            //       all need to be updated to account for multiple level footprint data. ugh.
            // TODO: I think this also means that our wall structures need multilevel footprints 
            //       to indicate "SUPPORT" exists underneath the upper level.  But without "SUPPORT" flags
            //       on the upper level, all we have to do is check for WALL on the level beneath it.  There's no 
            //       need for multiple footprint levels in this case.
            // TODO: so i'm worried about making all these changes and discovering its more headache
            //       and unnecessary complexity.
            // TODO: so what if for "stairs" we added flags on the 2D footprint that indicated the upper
            //       landing to differentiate from the lower landing and up/down traversal flags?
            //       Even just using lo/hi word values on a single level footprint which tells us if 
            //       we need to modify the upper level's footprint seems better than jagged array of multidimensional arrays.
            //       And for elevators, we stack elevator shaft components that still just leaves flags
            //       on the level they are placed.
            for (int i = 0; i < height; i++)
            {
                ushort width = (ushort)footprintData[i].GetLength(0);
                ushort depth = (ushort)footprintData[i].GetLength(1);

                
                
                // TODO: we need to know the width and depth of all the multidemensional arrays in advance to dimension this byte array properly.
                //       This means we need to modify our Decode to store the width and depth's at the the beginning of the data stream
                // copy 2 dimensional footprint array data into a one dimensional array
                data = new byte[sizeOfInt16 + sizeOfInt16 + (width * depth * bytesInInt32)]; // sizeOfInt16 x2 to account for width and depth 

                byte[] tmp = BitConverter.GetBytes(width);
                Array.Copy(tmp, 0, data, start, sizeOfInt16); start += sizeOfInt16;
                tmp = BitConverter.GetBytes(depth);
                Array.Copy(tmp, 0, data, start, sizeOfInt16); start += sizeOfInt16;

                // write data
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < depth; k++)
                    {
                        tmp = BitConverter.GetBytes(footprintData[i][j, k]);
                        for (int m = 0; m < bytesInInt32; m++)
                        {
                            data[start + (k * width * bytesInInt32) + (j * bytesInInt32) + m] = tmp[m];
                        }
                    }
            }

            string base64CompressedData = data.Compress();
            return base64CompressedData;
        }



        #region ITraversable members
        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        internal override Keystone.Traversers.ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }
    }

    // Shareable node with ID = the encoded string of the footprint data.
    // or how do we ensure this footprint gets reference by the next entity
    // that is to share this prefab?
    // In our entity AssetPlacement i do store the entity resourceDescriptor but
    // I never store an instance of that prefab as a "prefab" it's always just as a unique
    // instance.  So i never link entities.
    public class CellFootprint : Node
    {
        // NOTE: these have evolved to not really be "Cell" footprints anymore.  Only certain structures like floor,
        // walls, stairs and such must be centered on an edge or in a cell.  Other components can straddle cell boundaries
        // because they must only conform to the overall tilemap grid.
        private int[,] mData;
        public int Width { get { return mData.GetLength(0);}}
        public int Depth { get { return mData.GetLength(1);}}
        public int[,] Data { get { return mData; } } // read only! }


        /// <summary>
        /// CellFootprint nodes do not serialize.  They are resources
        /// stored in the parent Entity's "footprint" property which is stored
        /// as a string of compressed encoded data.
        /// Changing the footprint's data in the Editor will result in a new
        /// CellFootprint node being used _after_ the user "saves" the changes.
        /// They can be prompted to save changed when they attempt to close if
        /// they haven't already.  
        /// </summary>
        /// <param name="encodedData">the node ID which is base64 encode of the data this footprint contains</param>
        internal CellFootprint(string encodedData)
            : base(encodedData)
        {
            if (string.IsNullOrEmpty (encodedData)) throw new ArgumentNullException("CellFootprint.Ctor() - Footprint data must at least contain dimensions and initialized data with 0's.");
            Shareable = true;
            Serializable = false;

            mData = Decode(encodedData);
        }

        public static CellFootprint Create(int[,] data)
        {
            if (data == null) throw new ArgumentNullException("CellFootprint.Ctor() - Footprint data must at least contain dimensions and initialized data with 0's.");

            // determine the encoded id for this data
            string encodedID = Encode(data);

            return Create(encodedID);
        }

                
        public static CellFootprint Create(string encodedData)
        {
            if (string.IsNullOrEmpty(encodedData)) throw new ArgumentNullException();

            // Dec.17.2022 - added call to Repository.Create() to avoid multithreading issues during Parallel.For() creation of BonedEntities
            return (CellFootprint)Repository.Create(encodedData, "CellFootprint");

            // OBSOLETE - the following is not thread safe
            //// attempt to share the footprint if an exact copy already exists
            //CellFootprint fp = (CellFootprint)Repository.Get(encodedData);
            //if (fp != null) return fp;

            //fp = new CellFootprint(encodedData);
            //return fp;
        }
        

        // TODO: This auto generation of the footprint is really bad and should not be used.
        //       It can't even help in the case of multiple floors of footprint data.  its only for the most
        //       simplistic of geometries.  We should be hand designing the footprints.
        public static CellFootprint Create (ModeledEntity target, Keystone.Portals.Interior.TILE_ATTRIBUTES flags)
        {    
        	
        	if (target.IsGeometryFullyLoaded() == false) return null;
        	
        	
            // note: this can only work if target model is loaded with all geometry such that bounding box is accurate
            BoundingBox box = target.BoundingBox ;
            
            // TODO: eww, hard coded values
    		double cellWidth = 1.25d;
    		double cellDepth = 1.25d;
			uint tilesPerCellX = 16;
			uint tilesPerCellZ = 16;
			double tileSizeX = cellWidth / (double)tilesPerCellX; 
			double tileSizeZ = cellDepth / (double)tilesPerCellZ;
    		
    		double gridWidth = box.Width; // mWorkspace.GridWidth
    		double gridDepth = box.Depth; // mWorkspace.GridDepth
    		uint footprintWidth = (uint)System.Math.Ceiling (gridWidth / tileSizeX);
    		footprintWidth = (tilesPerCellX - footprintWidth % tilesPerCellX) + footprintWidth;
    		uint footprintHeight = (uint)System.Math.Ceiling (gridDepth / tileSizeZ);
    		// TODO: for our power reactor the following line is not producing correct result
    	//	footprintHeight = (region.TilesPerCellZ - footprintHeight % region.TilesPerCellZ) + footprintHeight;
    			
            // recall that "excluded" types will skip without traversing children whereas "ignored" will traverse children
            KeyCommon.Flags.EntityAttributes excludedObjectTypes =
                KeyCommon.Flags.EntityAttributes.Background |
                KeyCommon.Flags.EntityAttributes.HUD; // TODO: we do want to pick HUD when any painter/eraser is active, but not otherwise

            
            PickParameters parameters = new PickParameters
            {
                SearchType = PickCriteria.Closest,
                SkipBackFaces = false,
                Accuracy = PickAccuracy.Face,
                ExcludedTypes = excludedObjectTypes,
                FloorLevel = int.MinValue,
                T0 = -1,
                T1 = -1
            };
            
            
            // iterate through the center of every tile and cast a ray from it upward to top of the bbox of source entity
        	// if there is a collision, we add flags that are set in the brush to that tile.
        	
        	//if (target.Footprint == null || target.Footprint.Data == null) return null;
        	
        	int[,] data = new int[footprintWidth, footprintHeight]; //  target.Footprint.Data;
            //Array.Clear(data, 0, data.Length);  // set array values to 0        
            //int width = data.GetLength(0);
            //int height = data.GetLength(1);
            
            
            for (int i = 0; i < footprintWidth; i++)
            	for (int j = 0; j < footprintHeight; j++)
            	{
            		// TODO: surely i have a tile to world position written somewhere already?!
            		Vector3d position = Keystone.Celestial.ProceduralHelper.PixelCoordinateToPosition(
            			(float)gridWidth, (float)gridDepth, footprintWidth, footprintHeight,
                               i, j);
            	
            		//position -= mWorkspace.ViewportControls[0].Viewport.Context.Position ;
            		//position.y -= 1f;
            		position += target.Translation;
            		Ray r = new Ray (position, Vector3d.Up()); 
            		
            		// TODO: does this ray have to be in camera space?
            		// TODO: is there a way to do this pick in model space irrespective of camera or target's position?
            		//Keystone.Traversers.Picker picker = new Keystone.Traversers.Picker ();
            		// since the target is not connected to the scene necessarily, we just want direct pick starting at the target node and not
            		// the scene root because again, the target hasnt even been added to the scene.... so perhaps we can just directly
            		// test collide instead?
            		// TODO: our preview hud is using a clone of this which does NOT have footprint assigned after this new footprint is created
            		// and assigned!  So this has to be done prior to the hud cloning the target
            		if (Collide (target, r, parameters))
            		{
            			data[i,j] = (int)flags; // TODO: autogen tile flags
            			//System.Diagnostics.Debug.WriteLine ("collided " + i.ToString() + " and " + j.ToString());
            		}
            	}
            
            return Create (data);
        }
        
        
        
        private static bool Collide (ModeledEntity entity, Ray r, KeyCommon.Traversal.PickParameters parameters)
        {
        	Model[] models = entity.SelectModel(SelectionMode.Collision, -1);
            // TODO: apply _parameters.PickSearchType to limit scope if applicable
            if (models != null)
                for (int i = 0; i < models.Length; i++)
                    if (models[i].Geometry != null && models[i].Geometry.TVResourceIsLoaded)
           		 	{
            			// we must compute modelspace ray for each unique model to take into account any different scaling or rotations
            			// that exist independantly on each model.
            			Ray msRay = Helpers.Functions.GetModelSpaceRay(r, entity.Region, models[i], entity.Region);
                        
            			PickResults result =
            				models[i].Geometry.AdvancedCollide(msRay.Origin, msRay.Origin + (msRay.Direction * 1000d),
            				                   parameters);
            			
            			if (result.HasCollided) return true;
            		}	
            
            return false;
        }
               
//        public PickResults Collide(Vector3d start, Vector3d end, Matrix worldMatrix, KeyCommon.Traversal.PickParameters parameters)
//        {
//        	// TODO: this is wrong.  We should be selecting geometry from here and colliding with it, NOT calling the geometry from within here
//        	// i think part of the reason we do is we wanted to be able to call .Collide on model from places outside of Picker but we should avoid it
//            if (_geometry != null)
//            {
//                if (_geometry is Actor3d)
//                    parameters.ActorDuplicateInstanceID = _id;
//
//                return Geometry.AdvancedCollide(worldMatrix, start, end, parameters);
//            }
//
//            // still here means no collision
//            PickResults result = new PickResults();
//
//            return result;
//        }
		        
        // TODO: why not test zip and base64 encode?
        public static int[,] Decode(string encodedData)
        {
            if (string.IsNullOrEmpty(encodedData)) throw new ArgumentNullException();
            // decode to get the uncompressed byte stream
            byte[] data = encodedData.Explode();

            if (data == null) throw new Exception();
            const int bytesInInt32 = 4;

            // first two ushort's contain the width and height of the 2d array we're restoring
            ushort width, depth;
            int start = 0;
            width = BitConverter.ToUInt16(data, start); start += 2;
            depth = BitConverter.ToUInt16(data, start); start += 2;
                        
            int[,] footprint = new int[width, depth];

            for (int i = 0; i < width; i++)
                for (int j = 0; j < depth; j++)
                {
                    int startIndex = start + (j * width * bytesInInt32) + (i * bytesInInt32);
                    int tmp = BitConverter.ToInt32(data, startIndex);
                    footprint[i, j] = tmp;
                }

            return footprint;
        }

        public static string Encode(int width, int height)
        {
            return Encode(new int[width, height]);
        }

        /// <summary>
        /// Encodes a default empty footprint of specified width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static string Encode(uint width, uint height)
        {
            return Encode(new int[width, height]);
        }

        public static string Encode(int[,] footprintData)
        {
            if (footprintData == null) throw new ArgumentNullException();
            const int bytesInInt32 = 4;
            ushort width = (ushort)footprintData.GetLength(0);
            ushort depth = (ushort)footprintData.GetLength(1);
            int sizeOfInt16 = 2;
            int start = 0;

            // copy 2 dimensional footprint array data into a one dimensional array
            byte[] data = new byte[sizeOfInt16 + sizeOfInt16 + (width * depth * bytesInInt32)]; // sizeOfInt16 x2 to account for width and depth 

            byte[] tmp = BitConverter.GetBytes(width);
            Array.Copy (tmp, 0, data, start, sizeOfInt16); start += 2;
            tmp = BitConverter.GetBytes(depth);
            Array.Copy(tmp, 0, data, start, sizeOfInt16); start+=2;

            // write data
            for (int i = 0; i < width; i++)
                for (int j = 0; j < depth; j++)
                {
                    tmp = BitConverter.GetBytes(footprintData[i, j]);
                    for (int k = 0; k < bytesInInt32; k++)
                    {
                        data[start + (j * width * bytesInInt32) + (i * bytesInInt32) + k] = tmp[k];
                    }
                }
            string base64CompressedData = data.Compress();
            return base64CompressedData;
        }


        #region ITraversable members
        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        internal override Keystone.Traversers.ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }

        #region ResourceBase members
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[1 + tmp.Length];
            tmp.CopyTo(properties, 1);

            // TODO: domainobjects are not serializable so these properties are basically no good
            //       for that sort of thing.  CustomProperties 
            // see Keystone.Helpers.ExtensionMethods.ParseValue()
            // as you can see int[,] is not yet supported for deserialization from xml
            // do we store the path to the footprint instead? or do we
            // store the int[,] as a encoded binary string where we start with the length
            // of both dimensions and then base64 encode 
            // http://www.codeproject.com/Articles/80289/Saving-Image-Data-in-an-XML-File?msg=3468583#xx3468583xx
            // Convert.ToBase64String() and Convert.FromBase64String()
            properties[0] = new Settings.PropertySpec("footprint", typeof(int[,]).Name); // footprint is shared by all Entities using this DomainObject

            if (!specOnly)
            {
                properties[0].DefaultValue = mData;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].DefaultValue == null) continue;
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "footprint":
                        object tmp = properties[i].DefaultValue;
                        if (tmp == null)
                        {
                            mData = null;
                            continue;
                        }
                        mData = (int[,])tmp;
                        break;
                }
            }


            // TODO: uh... what's up with initalizing rules var here?  its a local var
            // so i think the idea was that during SetProperties we'd potentially validate
            // against rules and then store all broken results in here and... hrm
            List<KeyScript.Rules.Rule> broken = new List<KeyScript.Rules.Rule>();

            // TODO: TEMP HACK make sure Serializable is false even after loading previous file format node flags
            Serializable = false; // DomainObject's are basically just script nodes and they are shareable but not serializable. 
            // They can only be instanced by the parent Entity which saves a ref path.
            // This change will require our saved prefabs be updated
        }
        #endregion



        public void HitPoint(float impactPointX, float impactPointZ, out int endPixelX, out int endPixelZ)
        {
            throw new NotImplementedException();
            //Keystone.Celestial.ProceduralHelper.MapImpactPointToPixelCoordinate(
            //                   mWorkspace.GridWidth, mWorkspace.GridDepth,
            //                   mWorkspace.FootPrintWidth, mWorkspace.FootPrintHeight,
            //                   impactPointX, impactPointZ,
            //                   out endPixelX, out endPixelZ);

        }

        public System.Drawing.Point[] GetPixels(int startX, int endX, int startZ, int endZ)
        {
            // get the tiles within the footprint that are in the path from start and end pick points
            // find the biggest component difference
            int sizeX = Math.Abs(startX - endX);
            int sizeZ = Math.Abs(startZ - endZ);

            int max = Math.Max(sizeX, sizeZ) + 1;
            System.Drawing.Point[] pixels = new System.Drawing.Point[max];

            // if sizeX and sizeZ are the same then the start and end pixels MUST EITHER BE the same point
            // or form a diagonal which is not allowed.
            if (sizeX == 0 && sizeZ == 0)
            {
                // start and end are same point
                pixels[0] = new System.Drawing.Point(startX, startZ);
            }
            else if (sizeX != 0 && sizeZ != 0)
            {
                System.Diagnostics.Debug.WriteLine("Diagonals not allowed.");
                return null;
            }
            else if (sizeX > sizeZ)
            {
                // horizontal line
                int j = 0;
                int start = startX, stop = endX;
                if (startX > endX)
                {
                    start = endX;
                    stop = startX;
                }
                for (int i = start; i <= stop; i++)
                {
                    pixels[j] = new System.Drawing.Point(i, endZ);
                    j++;
                }
            }
            else if (sizeZ > sizeX)
            {
                // vertical line
                int j = 0;
                int start = startZ, stop = endZ;
                if (startZ > endZ)
                {
                    start = endZ;
                    stop = startZ;
                }
                for (int i = start; i <= stop; i++)
                {
                    pixels[j] = new System.Drawing.Point(endX, i);
                    j++;
                }
            }


            if (pixels.Length == 0) return null;
            return pixels;
        }
    }
}
