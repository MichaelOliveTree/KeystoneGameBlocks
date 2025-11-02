using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Keystone.Appearance;
using Keystone.Collision;
using Keystone.Culling;
using Keystone.Entities;
using Keystone.Enum;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Shaders;
using Keystone.Traversers;
using Keystone.Types;
using KeyCommon.Traversal;
using MTV3D65;

namespace Keystone.Elements
{
    // todo:  maybe this should inherit from Mesh3d instead of Geometry?
    public class GeometryBuffer : Geometry
    {
        internal class AppearanceState
        {
            //// AppearanceOverrides will match AppearanceGroup however it is not a node
            //// when rendering, an override can be specified 
            //// TODO: what if we used a stack where when pushing an appearance state
            ////       onto an actual GroupAttribute, the existing state is stored in AppearanceGroupState
            ////       this can potentially accomplish what we want too...  and we can potentially
            ////       use our property bags to restore... hrmmmm!!!  but for the nested
            ////       material and shader their bags are in seperate nodes... ..  do we
            ////       ignore that and just have 3 different arrays of properties?
            //private CONST_TV_BLENDINGMODE _blendingMode;
            //private bool _alphaTest;
            //private int _alphaTestRefValue;
            //private bool _alphaTestDepthBufferWriteEnable;


            //private Material _material;
            //private Shader _shader;
            //private Dictionary<string, object> mParameterValues;
            //private string mPersistedParameterValues; // string only used for serialization

            //private Layer[] _layers;

            //Settings.PropertySpec[] mAttributes;
            //Settings.PropertySpec[] mShader;
            //Settings.PropertySpec[] mMaterial;
            //Settings.PropertySpec[][] mLayers;

            //public void ReadState(GroupAttribute attributes)
            //{ 
            //}

            //public void WriteState(GroupAttribute attributes)
            //{

            //}
        }

        

        protected TVMesh _tvmesh;
        protected CONST_TV_LIGHTINGMODE _lightingMode; // lightingmode of the mesh is cached only so during traversal we can tell if we need to change it after rendering a previous instance.  But it does NOT need to be saved/read from file
        
        
        public ConvexHull Hull; // TODO: i think perhaps the Hull needs to be apart of Model such as an LOD?  Model.Geometry,  Model.Hull
        public string CollisionHullPath;

        // version that doesnt require a TVMesh or filepath so that we can load the xmlnode and set the tvresource
        // load state as unloaded so that a traverser can load it later
        internal GeometryBuffer(string id) : base(id)
        {
        }

        protected GeomrtryBuffer(string id, TVMesh mesh)
            : this(id)
        {
            if (mesh == null) throw new Exception();
            _tvmesh = mesh;
            _tvfactoryIndex = _tvmesh.GetIndex();
            _resourceStatus = PageableNodeStatus.Loaded;

            _tvmesh.SetCollisionEnable(true);
        !;

        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        #region ResourceBase members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
        	return base.GetProperties (specOnly);
//            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
//            Settings.PropertySpec[] properties = new Settings.PropertySpec[1 + tmp.Length];
//            tmp.CopyTo(properties, 1);
//
//            properties[0] = new Settings.PropertySpec("", typeof(int).Name);
//            // TODO: i dont think it's necessary to save pointsprite or it's related parameters
//            // is there?  because i think those are saved in the mesh and restored when you load it.
//            // the parameters are only useful when you are creating the mesh
//            //properties[1] = new Settings.PropertySpec("pointsprite", typeof(int).Name);
//
//            if (!specOnly)
//            {
//                properties[0].DefaultValue = (int)bleh;
//                //properties[1].DefaultValue = mPointSprite;
//            }
//
//            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);
//
//            for (int i = 0; i < properties.Length; i++)
//            {
//                // use of a switch allows us to pass in all or a few of the propspecs depending
//                // on whether we're loading from xml or changing a single property via server directive
//                switch (properties[i].Name)
//                {
//                    case "bleh":
//                        bleh = ()properties[i].DefaultValue;
//                        break;
//                    //case "pointsprite":
//                    //    mPointSprite = (bool)properties[i].DefaultValue;
//                    //    break;
//                }
//            }
        }
        #endregion

        public static DefaultAppearance GetAppearance (string meshResourcePath)
        {
        	DefaultAppearance appearance = null;
        	// NOTE: We should be requring that when we configure our meshes, we assign textures and materials ourselves
        	//       and quit relying on TV to do it.  True it's convenent for many groups and also materials tend to be
        	//       baked in, but still it's lazy and breaks our system.
        	
        	// we'll load the TVM directly without creating a Mesh3d.  We only want the appearance
        	
        	// we should then come up with a naming convention for each material and texutre so we can share them
        	// without worrying about whether the Mesh3d that encapsulates this tvm already exists and has 
        	// a model that has these materials and textures set....besides, we wont even know if those existing models
        	// are still using the defaults
        	bool loadTextures = true;
        	bool loadMaterials = true;
        	
        	TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(meshResourcePath);
        	bool lineList;
        	tvm = LoadTVMesh(tvm, meshResourcePath, loadTextures, loadMaterials, out appearance, out lineList);

            //CoreClient._CoreClient.Scene.DestroyMesh(tvm);
        	return appearance ;
        }
  
//        /// <summary>
//        /// In order to load appearance, this Create() method immediately loads the Mesh and does
//        /// not wait for paging.
//        /// </summary>
//        /// <param name="resourcePath"></param>
//        /// <param name="loadTextures"></param>
//        /// <param name="loadMaterials"></param>
//        /// <param name="appearance"></param>
//        /// <returns></returns>
//        public static Mesh3d Create(string resourcePath, bool loadTextures, bool loadMaterials,
//                                    out DefaultAppearance appearance)
//        {
//            // TODO: if this mesh is from a mod archive, then what we need to do is load our materials and textures differently
//            //          1) we need to get the texture names
//            //          2) we need to modify these filenames to be relative to resourcePath
//            //          3) since forcetexture loading, we then need to track whether 
//
//                // BUG: we have to lock the entire Repository._cache or inbetween an attempt to .Get()
//                // and .Add() which occurs during creation of new Mesh3d(), we can get multiple threads
//                // trying to create the same resource.  Hack for now is to just wrap
//                // in lock () {}.  
//                // TODO: I need to verify during testing all cases where i'm using this .Get()
//                // .Add() mechanism for resource sharing because it is fundamentally broken if 
//                // i don't use a giant lock
//                Mesh3d mesh = (Mesh3d) Keystone.Resource.Repository.Create (resourcePath, "Mesh3d");
//                //Mesh3d mesh = (Mesh3d)Repository.Get(resourcePath);
//
//                // we can skip loading the mesh and go straight to loading the appearance if the mesh3d already exists in the cache.
//                // NOTE: We cannot know the appearance though of a previous one... but the way to avoid creating a virtual dupe 
//                // is to just .Clone the model and not try to re-load the appearance this way.  But
//                // how do we know the model?
//                appearance = null;
//                
//                // TODO: below is all about the Appearance node creation.  This is never an issue during deserialization of scene or prefab.
//                // it is only an issue during prefab IMPORT or hardcoded model generation which includes celledregion interior floors and walls
//                // since we are not yet supporting those to be defined in the SegmentStyle but should
//                if (mesh == null)
//                {
//                    // TODO: not waiting for paging here is annoying... this is called by Minimesh.Create() which means we start paging in a mesh3d when we may not want to
//                    // do not wait for paging because we wish to get the built in Appearance info immediately.  ideally we'd read this ourselves
//                    // from the mesh file.  of course that would be too annoying to do with .x, but possible with .obj and .tvm
//                    // TODO: the other option i think is to make a custom "LoadTVMesh" that will not add the mesh to the factory
//                    // TODO: the other option i think is i really should be breaking up this functionality into seperate functions where first
//                    // the caller just gets the mesh created, then forces .LoadTVResource, then grabs the materials and textures and creates an appearance
//                    TVMesh tvm = LoadTVMesh(resourcePath, loadTextures, loadMaterials, out appearance);
//                    mesh = new Mesh3d(resourcePath, tvm);
//                }
//                else
//                {
//                    if (mesh.Parents != null) // TODO: if the mesh is recycled from repository but it's being used for MinimeshGeometry, it wont have it's Parent set
//                    {
//                        for (int i = 0; i < mesh.Parents.Length; i++)
//                        {
//                            if (mesh.Parents[i] is Model && ((Model)mesh.Parents[i]).Appearance != null)
//                            {
//                                // TODO: i should probably verify i have checks 4 any non share-able node such as
//                                // appearance, Models, Entities, etc can never have more than one parent.
//                                // perhaps we can find an alternative way to do this...  Node.Clone()
//                                // method
//								// TODO: here we dont even know if the existing is even using the default textures and materials anymore!                                
//                                string clonedAppearanceID = Repository.GetNewName(((Model)mesh.Parents[i]).Appearance.TypeName);
//                                appearance = (DefaultAppearance)((Model)mesh.Parents[i]).Appearance.Clone(clonedAppearanceID, true, false);
//                                break;
//                            }
//                        }
//                    }
//                    Trace.WriteLine("Mesh " + resourcePath + " already found in cache... sharing mesh instance.");
//                }
//                return mesh;
//            
//        }



        



                    
        /// <summary>
        /// Will compute a rectangular floor mesh with center at origin 0,0,0 and who's width and depth
        /// are specified.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="width"></param>
        /// <param name="depth"></param>
        /// <param name="numCellsX"></param>
        /// <param name="numCellsZ"></param>
        /// <param name="tileU"></param>
        /// <param name="tileV"></param>
        /// <returns></returns>
        public static Mesh3d CreateFloor(Texture texture, float width, float depth,
                                         uint numCellsX, uint numCellsZ, float tileU, float tileV)
        {
            if (depth <= 0.0f || width <= 0.0f) throw new ArgumentOutOfRangeException();
            if (numCellsX <= 0 || numCellsZ <= 0) throw new ArgumentOutOfRangeException();

            string filename = string.Format(PRIMITIVE_FLOOR + "_{0}_{1}_{2}_{3}_{4}_{5}", width, depth, numCellsX, numCellsZ, tileU, tileV);
            string path = filename; // no full path needed for primitives
            
            
            return (Mesh3d)Repository.Create (path, "Mesh3d");
        }

        public static TVMesh CreateFloorTVM(string name, float width, float depth,
                                         uint numCellsX, uint numCellsZ, float tileU, float tileV)
        {
        	// TODO: We may not wish to ever share FloorGrids because for a given grid different instances
			//    	may have different configurations of which floor cells are collapsed and that means
            //      UVs and vertex coordinates for those cells will be different. 
			//      This means a unique identifer should be provided in the id resource string.

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(name);

            float x1, x2, z1, z2;
            
            // compute x1,x2, z1,z2 coordinates such that the center is origin (x = 0, z = 0)
            x2 = width * .5f;
            x1 = -x2;
            z2 = depth * .5f;
            z1 = -z2;

            if (numCellsX * numCellsZ * 2 > 65000) throw new ArgumentOutOfRangeException ("Mesh3d.CreateFloorTVM() - Number of vertices will exceed limit of 65k.");
            
            // NOTE: if width and depth are too large, picking will start to fail.  However in case of a cloud layer, the size may still be ok.
            if (width > 65000 || depth > 65000) 
            	System.Diagnostics.Debug.WriteLine ("Mesh3d.CreateFloorTVM() - WARNING: Size exceeds safe limit.  Picking will not always work (eg. dead spots will exist)");
            
            // NOTE:  .AddFloorGrid() creates a new group each time you call it on same mesh so there's potential to have multiple textures and materials
            tvm.AddFloorGrid(-1, x1, z1, x2, z2, (int)numCellsX, (int)numCellsZ, 0, tileU, tileV);
            
            // NOTE: use this.CullMode, this.InvertNormals or this.ComputeNormals should all
            // be set as flags that LoadTVResource() will apply
            return tvm;
        }

        /// <summary>
        /// </summary>
        /// <param name="cellWidth">width of individual cell</param>
        /// <param name="cellDepth">depth of individual cell</param>
        /// <param name="numCellsX">number of cells along x axis</param>
        /// <param name="numCellsZ">number of cells along z axis</param>
        /// <param name="insetScale">Visual scale of the inner quads with respect to the overall tile.  
        /// This way we can inset the inner quads if we want to. 1.0 - 0.75 are most likely ranges to use. 
        /// 1.0 would set the inner scale to be same as outer and so no inset effect would occur.</param>
        /// <param name="flipFaceNormalsDown"></param>
        /// <param name="tag">A unique key value so that we can create unique mesh instances 
        /// even when they share the exact same dimensions.  This way we can have different
        /// UV coords and collapsed cell states for seperate instances.</param>
        /// <returns></returns>
        /// <remarks>
        /// Immediate creation (no delayed loading during LoadTVResource)
        /// </remarks>
        public static Mesh3d CreateCellGrid(float cellWidth, float cellDepth, 
                                            uint numCellsX, uint numCellsZ, 
                                         float insetScale, bool flipFaceNormalsDown, string tag) 
        
        {
            if (cellDepth <= 0.0f || cellWidth <= 0.0f) throw new ArgumentOutOfRangeException();
            if (numCellsX <= 0 || numCellsZ <= 0) throw new ArgumentOutOfRangeException();

            string name = string.Format(PRIMITIVE_CELL_GRID + "_{0}_{1}_{2}_{3}_{4}_{5}_{6}",
                cellWidth, cellDepth, 
                numCellsX, numCellsZ, 
                insetScale, flipFaceNormalsDown, tag);

            // TODO: We may not wish to ever share CellGrids because for a given cell grid interior of ship for instance
            //       different ships may have different configurations of which floor cells are collapsed and that means
            //       UVs and vertex coordinates for those cells will be different.  This means a unique identifer should be provided in the id resource string.
            Mesh3d mesh = (Mesh3d)Repository.Get(name);
            if (mesh != null) return mesh;

            // unless the caller explicitly calls Mesh.LoadTVResource() this mesh will be loaded on demand.
            mesh = new Mesh3d(name);
            //CoreClient._CoreClient.Scene.SetTextureFilter(CONST_TV_TEXTUREFILTER.TV_FILTER_NONE);
            mesh.TextureClamping = false; // TODO: TextureClamping should be assignable via Appearance
            // TODO: maybe invertnormals instead of doublesided?
           // mesh.CullMode = CONST_TV_CULLING.TV_DOUBLESIDED;
            return mesh;
        }


        private static TVMesh CreateCellGridTVM(string name,
                                         float cellWidth, float cellDepth,
                                         uint numCellsX, uint numCellsZ,
                                         float insetScale, bool reverseWindingOrder)
        {

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(name);
            // tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_TRIANGLELIST);

            TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[3];
            elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;
            elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;
            elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT2;

            elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
            elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
            elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;

            elements[0].stream = 0;
            elements[1].stream = 0;
            elements[2].stream = 0;

            int strideInBytes = 4 * 8;  // four bytes in a float * 8 floats per vertex
            tvm.SetLightingMode(CONST_TV_LIGHTINGMODE.TV_LIGHTING_MANAGED);
            tvm.SetMeshFormatEx(elements, elements.Length);


            float startX, stopX, startZ, stopZ;

            Keystone.Portals.ZoneRoot.GetStartStop(out startX, out stopX, numCellsX);
            Keystone.Portals.ZoneRoot.GetStartStop(out startZ, out stopZ, numCellsZ);

            // convert the startX/Z from cell values to real local space measurements
            // remember that if 9x6 width x height, the startx = -4 and startz = -2.5 
            // and if the cellSize are not 1x1 meters than those values without conversion
            // to real cell sizes will fail to compute proper sized cells.
            startX *= cellWidth;
            startZ *= cellDepth;

            float halfSizeX = cellWidth * 0.5f;
            float halfSizeZ = cellDepth * 0.5f;

            uint groupCount = 1;
            int[] faceGroupIDs = new int[] { 0 };     // we'll only have 1 group

            uint cellCount = numCellsX * numCellsZ;
            int triangleCount = (int)(cellCount * 2); // * 2 because 2 triangles per quad
            int vertexCount = (int)(cellCount * 4); // we dont use vertices.Length because vertices contains vertex data which includes 8 floats per vertex

            float[] vertices = new float[cellCount * 4 * 8]; // * 4 because there are 4 vertices in a quad, * 8 because each vertex element is comprised of 8 floats
            int[] indices = new int[cellCount * 6];

            bool optimize = false; // we wont optimize because we don't want our vertex indices switched (which WOULD happen).  We are already going to use indexed vertices so optimization would not improve performance because there's no optimization that can be done
            float normalY = 1.0f;

            // create a reference set of vertices which we'll use to position the rest
            // this reference set is built around origin so it can easily be scaled
            // NOTE: the indices will get re-ordered within a triangle face due to a bug
            // detailed here: http://www.truevision3d.com/forums/tv3d_sdk_65/setgeometry_optimization_and_face_order-t14255.0.html
            // vertex 0 will wind up being given the index of vertex 2.  And vertex 1 will
            // be swapped with vertex 3.  
            float[] reference = new float[8 * 4]; // 8 floats per vertex * 4 vertices per quad tile
            // vertex 0
            reference[0] = -(halfSizeX * insetScale);
            reference[1] = 0.0f;
            reference[2] = -(halfSizeZ * insetScale);
            reference[3] = 0.0f;    // normalX
            reference[4] = normalY; // normalY
            reference[5] = 0.0f;    // normalZ
            reference[6] = 0.0f;    // textureU
            reference[7] = 1.0f;    // textureV

            // vertex 1
            reference[8] = -(halfSizeX * insetScale);
            reference[9] = 0.0f;
            reference[10] = halfSizeZ * insetScale;
            reference[11] = 0.0f;
            reference[12] = normalY;
            reference[13] = 0.0f;
            reference[14] = 0.0f;
            reference[15] = 0.0f;

            // vertex 2
            reference[16] = halfSizeX * insetScale;
            reference[17] = 0.0f;
            reference[18] = halfSizeZ * insetScale;
            reference[19] = 0.0f;
            reference[20] = normalY;
            reference[21] = 0.0f;
            reference[22] = 1.0f;
            reference[23] = 0.0f;

            // vertex 3
            reference[24] = halfSizeX * insetScale;
            reference[25] = 0.0f;
            reference[26] = -(halfSizeZ * insetScale);
            reference[27] = 0.0f;
            reference[28] = normalY;
            reference[29] = 0.0f;
            reference[30] = 1.0f;
            reference[31] = 1.0f;

            int k = 0;
            int index = 0;
            int count = 0;


            // NOTE: we will go across all cells in the x direction til we hit the boundary
            // then onto the next row.  This will make it easy to determine neighboring tiles
            // Every 4 vertices represents a rectangle and every 2 triangles represents a rectangle
            // going from left to right and bottom to top (negative to positive along x and z axis respectively)
            for (int i = 0; i < numCellsZ; i++)
            {
                for (int j = 0; j < numCellsX; j++)
                {
                    // NOTE: Default DirectX winding order is CLOCKWISE vertices for
                    // front facing.  XNA also uses clockwise for front facing.
                    // based on current cell, compute the polypoints of the cell
                    // THUS 
                    // 1 ___ 2
                    // |    |
                    // 0 ___ 3
                    // is our layout

                    // compute vertices for 2 triangles to create a square
                    // NOTE: startX and startZ are CENTER coordinate values.  
                    //       Any offset for left and right or top/botom edges of the cell
                    //       are added into the reference which is constructed at origin.
                    float xOffset = (j * cellWidth) + startX;
                    float zOffset = (i * cellDepth) + startZ;


                    // vertex 0
                    vertices[k++] = xOffset + reference[0];   // coord.x
                    vertices[k++] = 0;                        // coord.y
                    vertices[k++] = zOffset + reference[2];   // coord.z
                    vertices[k++] = reference[3];
                    vertices[k++] = reference[4];
                    vertices[k++] = reference[5];
                    vertices[k++] = reference[6];
                    vertices[k++] = reference[7];

                    indices[count++] = index++;

                    //if (reverseWindingOrder == false) // we swap vertex 1 and 3 if we want to revrese winding order
                    //{
                    // vertex 1
                    vertices[k++] = xOffset + reference[8];   // coord.x
                    vertices[k++] = 0;                        // coord.y
                    vertices[k++] = zOffset + reference[10];  // coord.z
                    vertices[k++] = reference[11];
                    vertices[k++] = reference[12];
                    vertices[k++] = reference[13];
                    vertices[k++] = reference[14];
                    vertices[k++] = reference[15];
                    //}
                    //else // NOTE: this is not working.  Reversing the order i add the verts makes no difference it seems but tvmesh.InvertNormals() does work
                    //{
                    //    // vertex 3
                    //    vertices[k++] = xOffset + reference[24];   // coord.x
                    //    vertices[k++] = 0;                         // coord.y
                    //    vertices[k++] = zOffset + reference[26];   // coord.z
                    //    vertices[k++] = reference[27];
                    //    vertices[k++] = reference[28];
                    //    vertices[k++] = reference[29];
                    //    vertices[k++] = reference[30];
                    //    vertices[k++] = reference[31];
                    //}

                    indices[count++] = index++;

                    // vertex 2
                    vertices[k++] = xOffset + reference[16];   // coord.x
                    vertices[k++] = 0;                         // coord.y
                    vertices[k++] = zOffset + reference[18];   // coord.z
                    vertices[k++] = reference[19];
                    vertices[k++] = reference[20];
                    vertices[k++] = reference[21];
                    vertices[k++] = reference[22];
                    vertices[k++] = reference[23];

                    indices[count++] = index;
                    indices[count++] = index++; // add this vertex again since it's shared with second triangle
                                                // indices[count++] = index;
                                                // index++;
                                                //if (reverseWindingOrder == false) // we swap vertex 1 and 3 if we want to revrese winding order
                                                //{
                                                // vertex 3
                    vertices[k++] = xOffset + reference[24];   // coord.x
                    vertices[k++] = 0;                         // coord.y
                    vertices[k++] = zOffset + reference[26];   // coord.z
                    vertices[k++] = reference[27];
                    vertices[k++] = reference[28];
                    vertices[k++] = reference[29];
                    vertices[k++] = reference[30];
                    vertices[k++] = reference[31];
                    //}
                    //else // NOTE: this is not working.  Reversing the order i add the verts makes no difference it seems but tvmesh.InvertNormals() does work
                    //{
                    //    // vertex 1
                    //    vertices[k++] = xOffset + reference[8];   // coord.x
                    //    vertices[k++] = 0;                        // coord.y
                    //    vertices[k++] = zOffset + reference[10];  // coord.z
                    //    vertices[k++] = reference[11];
                    //    vertices[k++] = reference[12];
                    //    vertices[k++] = reference[13];
                    //    vertices[k++] = reference[14];
                    //    vertices[k++] = reference[15];
                    //}
                    indices[count++] = index;
                    indices[count++] = index - 3; // add the first vertex index again since it's shared with second triangle
                    index++;
                }
            }

            faceGroupIDs = new int[triangleCount];
            for (int i = 0; i < triangleCount; i++)
                faceGroupIDs[i] = 0; // all triangles assigned to same group

            tvm.SetGeometryEx(vertices, strideInBytes, vertexCount, indices, triangleCount, (int)groupCount, faceGroupIDs, optimize);
            // 152280, 36, 16920, 25380, 8460, 1, 8460

           // tvm.ComputeNormals(); // testing to see if this fixes our specular lighting problem, but it doesn't
            
            // TODO: aren't we already reversing the vertex orders during creation above?
            if (reverseWindingOrder)
                tvm.InvertNormals();

            

            //            System.Diagnostics.Debug.WriteLine(string.Format("Mesh3d.CreateCellGridTVM() - Sucess.  {0} tiles.  {1} vertices {2} bytes.", triangleCount / 2, vertexCount, vertexCount * strideInBytes));
            return tvm;
        }

        //private static TVMesh CreateCellGridTVM(string name,
        //                             float cellWidth, float cellDepth,
        //                             uint numCellsX, uint numCellsZ,
        //                             float insetScale, bool reverseWindingOrder)
        //{

        //    TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(name);
        //    //tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_TRIANGLELIST);

        //    TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[3];
        //    elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;
        //    elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;
        //    elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT2;

        //    elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
        //    elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
        //    elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;

        //    elements[0].stream = 0;
        //    elements[1].stream = 0;
        //    elements[2].stream = 0;

        //    int strideInBytes = 4 * 9;  // four bytes in a float * 9 floats per vertex
        //    tvm.SetMeshFormatEx(elements, elements.Length);
        //    //    tvm.SetLightingMode(CONST_TV_LIGHTINGMODE.TV_LIGHTING_MANAGED);

        //    float startX, stopX, startZ, stopZ;

        //    Keystone.Portals.ZoneRoot.GetStartStop(out startX, out stopX, numCellsX);
        //    Keystone.Portals.ZoneRoot.GetStartStop(out startZ, out stopZ, numCellsZ);

        //    // convert the startX/Z from cell values to real local space measurements
        //    // remember that if 9x6 width x height, the startx = -4 and startz = -2.5 
        //    // and if the cellSize are not 1x1 meters than those values without conversion
        //    // to real cell sizes will fail to compute proper sized cells.
        //    startX *= cellWidth;
        //    startZ *= cellDepth;

        //    float halfSizeX = cellWidth * 0.5f;
        //    float halfSizeZ = cellDepth * 0.5f;

        //    uint groupCount = 1;
        //    int[] faceGroupIDs = new int[] { 0 };     // we'll only have 1 group

        //    uint cellCount = numCellsX * numCellsZ;
        //    int triangleCount = (int)(cellCount * 2); // * 2 because 2 triangles per quad
        //    int vertexCount = (int)(cellCount * 4); // we dont use vertices.Length because vertices contains vertex data which includes 9 floats per vertex

        //    float[] vertices = new float[cellCount * 4 * 9]; // * 4 because there are 4 vertices in a quad, * 9 because each vertex element is comprised of 9 floats
        //    vertexCount = vertices.Length / 9;
        //    int[] indices = new int[cellCount * 6];
        //    // System.Diagnostics.Debug.Assert(vertexCount == triangleCount * 3);
        //    //vertexCount = triangleCount * 3;
        //    // vertexCount = vertices.Length / strideInBytes;
        //    bool optimize = false; // we wont optimize because we don't want our vertex indices switched (which WOULD happen).  We are already going to use indexed vertices so optimization would not improve performance because there's no optimization that can be done
        //    float normalY = 1.0f;

        //    // create a reference set of vertices which we'll use to position the rest
        //    // this reference set is built around origin so it can easily be scaled
        //    // NOTE: the indices will get re-ordered within a triangle face due to a bug
        //    // detailed here: http://www.truevision3d.com/forums/tv3d_sdk_65/setgeometry_optimization_and_face_order-t14255.0.html
        //    // vertex 0 will wind up being given the index of vertex 2.  And vertex 1 will
        //    // be swapped with vertex 3.  
        //    float[] reference = new float[9 * 4]; // 9 floats per vertex * 4 vertices per quad tile
        //    // vertex 0
        //    reference[0] = -halfSizeX * insetScale;
        //    reference[1] = 0.0f;
        //    reference[2] = -halfSizeZ * insetScale;
        //    reference[3] = 1.0f;
        //    reference[4] = 0.0f;    // normalX
        //    reference[5] = normalY; // normalY
        //    reference[6] = 0.0f;    // normalZ
        //    reference[7] = 0.0f;    // textureU
        //    reference[8] = 1.0f;    // textureV
        //    // vertex 1
        //    reference[9] = -halfSizeX * insetScale;
        //    reference[10] = 0.0f;
        //    reference[11] = halfSizeZ * insetScale;
        //    reference[12] = 1.0f;
        //    reference[13] = 0.0f;
        //    reference[14] = normalY;
        //    reference[15] = 0.0f;
        //    reference[16] = 0.0f;
        //    reference[17] = 0.0f;

        //    // vertex 2
        //    reference[18] = halfSizeX * insetScale;
        //    reference[19] = 0.0f;
        //    reference[20] = halfSizeZ * insetScale;
        //    reference[21] = 1.0f;
        //    reference[22] = 0.0f;
        //    reference[23] = normalY;
        //    reference[24] = 0.0f;
        //    reference[25] = 1.0f;
        //    reference[26] = 0.0f;

        //    // vertex 3
        //    reference[27] = halfSizeX * insetScale;
        //    reference[28] = 0.0f;
        //    reference[29] = -halfSizeZ * insetScale;
        //    reference[30] = 1.0f;
        //    reference[31] = 0.0f;
        //    reference[32] = normalY;
        //    reference[33] = 0.0f;
        //    reference[34] = 1.0f;
        //    reference[35] = 1.0f;

        //    int k = 0;
        //    int index = 0;
        //    int count = 0;


        //    // NOTE: we will go across all cells in the x direction til we hit the boundary
        //    // then onto the next row.  This will make it easy to determine neighboring tiles
        //    // Every 4 vertices represents a rectangle and every 2 triangles represents a rectangle
        //    // going from left to right and bottom to top (negative to positive along x and z axis respectively)
        //    for (int i = 0; i < numCellsZ; i++)
        //    {
        //        for (int j = 0; j < numCellsX; j++)
        //        {
        //            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
        //            // front facing.  XNA also uses clockwise for front facing.
        //            // based on current cell, compute the polypoints of the cell
        //            // THUS 
        //            // 1 ___ 2
        //            // |    |
        //            // 0 ___ 3
        //            // is our layout

        //            // compute vertices for 2 triangles to create a square
        //            // NOTE: startX and startZ are CENTER coordinate values.  
        //            //       Any offset for left and right or top/botom edges of the cell
        //            //       are added into the reference which is constructed at origin.
        //            float xOffset = (j * cellWidth) + startX;
        //            float zOffset = (i * cellDepth) + startZ;


        //            // vertex 0
        //            vertices[k++] = xOffset + reference[0];   // coord.x
        //            vertices[k++] = 0;                        // coord.y
        //            vertices[k++] = zOffset + reference[2];   // coord.z
        //            vertices[k++] = reference[3];
        //            vertices[k++] = reference[4];
        //            vertices[k++] = reference[5];
        //            vertices[k++] = reference[6];
        //            vertices[k++] = reference[7];
        //            vertices[k++] = reference[8];

        //            indices[count++] = index++;

        //            //if (reverseWindingOrder == false) // we swap vertex 1 and 3 if we want to revrese winding order
        //            //{
        //            // vertex 1
        //            vertices[k++] = xOffset + reference[9];   // coord.x
        //            vertices[k++] = 0;                        // coord.y
        //            vertices[k++] = zOffset + reference[11];  // coord.z
        //            vertices[k++] = reference[12];
        //            vertices[k++] = reference[13];
        //            vertices[k++] = reference[14];
        //            vertices[k++] = reference[15];
        //            vertices[k++] = reference[16];
        //            vertices[k++] = reference[17];
        //            //}
        //            //else // NOTE: this is not working.  Reversing the order i add the verts makes no difference it seems but tvmesh.InvertNormals() does work
        //            //{
        //            //    // vertex 3
        //            //    vertices[k++] = xOffset + reference[24];   // coord.x
        //            //    vertices[k++] = 0;                         // coord.y
        //            //    vertices[k++] = zOffset + reference[26];   // coord.z
        //            //    vertices[k++] = reference[27];
        //            //    vertices[k++] = reference[28];
        //            //    vertices[k++] = reference[29];
        //            //    vertices[k++] = reference[30];
        //            //    vertices[k++] = reference[31];
        //            //}

        //            indices[count++] = index++;

        //            // vertex 2
        //            vertices[k++] = xOffset + reference[18];   // coord.x
        //            vertices[k++] = 0;                         // coord.y
        //            vertices[k++] = zOffset + reference[20];   // coord.z
        //            vertices[k++] = reference[21];
        //            vertices[k++] = reference[22];
        //            vertices[k++] = reference[23];
        //            vertices[k++] = reference[24];
        //            vertices[k++] = reference[25];
        //            vertices[k++] = reference[26];

        //            indices[count++] = index;
        //            indices[count++] = index++; // add this vertex again since it's shared with second triangle

        //            //if (reverseWindingOrder == false) // we swap vertex 1 and 3 if we want to revrese winding order
        //            //{
        //            // vertex 3
        //            vertices[k++] = xOffset + reference[27];   // coord.x
        //            vertices[k++] = 0;                         // coord.y
        //            vertices[k++] = zOffset + reference[29];   // coord.z
        //            vertices[k++] = reference[30];
        //            vertices[k++] = reference[31];
        //            vertices[k++] = reference[32];
        //            vertices[k++] = reference[33];
        //            vertices[k++] = reference[34];
        //            vertices[k++] = reference[35];
        //            //}
        //            //else // NOTE: this is not working.  Reversing the order i add the verts makes no difference it seems but tvmesh.InvertNormals() does work
        //            //{
        //            //    // vertex 1
        //            //    vertices[k++] = xOffset + reference[8];   // coord.x
        //            //    vertices[k++] = 0;                        // coord.y
        //            //    vertices[k++] = zOffset + reference[10];  // coord.z
        //            //    vertices[k++] = reference[11];
        //            //    vertices[k++] = reference[12];
        //            //    vertices[k++] = reference[13];
        //            //    vertices[k++] = reference[14];
        //            //    vertices[k++] = reference[15];
        //            //}
        //            indices[count++] = index;
        //            indices[count++] = index - 3; // add the first vertex index again since it's shared with second triangle
        //            index++;
        //        }
        //    }

        //    faceGroupIDs = new int[triangleCount];
        //    for (int i = 0; i < triangleCount; i++)
        //        faceGroupIDs[i] = 0; // all triangles assigned to same group

        //    tvm.SetGeometryEx(vertices, strideInBytes, vertexCount, indices, triangleCount, (int)groupCount, faceGroupIDs, optimize);
        //    // 152280, 36, 16920, 25380, 8460, 1, 8460
        //    // TODO: aren't we already reversing the vertex orders during creation above?
        //    if (reverseWindingOrder)
        //        tvm.InvertNormals();

        //    //            System.Diagnostics.Debug.WriteLine(string.Format("Mesh3d.CreateCellGridTVM() - Sucess.  {0} tiles.  {1} vertices {2} bytes.", triangleCount / 2, vertexCount, vertexCount * strideInBytes));
        //    return tvm;
        //}

        /// <summary>
        /// Used for creating a Skysphere using triangle stirps for Zak's  sky
        /// </summary>
        /// <remarks>
        /// A unit sphere has a radius of 1.0.
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="Roundness"></param>
        /// <param name="Stacks"></param>
        /// <param name="Slices"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Mesh3d CreateUVTriangleStripSphere(double Roundness, int Stacks, int Slices,
                                                         double scale)
        {
            string filename = string.Format(PRIMITIVE_UV_TRISTRIP_SPHERE + "_{0}_{1}_{2}", Stacks, Slices, scale);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;


            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(path);
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_TRIANGLESTRIP);
            //NOTE: Very important not to set a lighting mode on this mesh!  The UV beomes FUBAR

            // note: SetLightingMode must be called after geometry is loaded because the lightingmode
            // is stored in .tvm, .tva files and so anything you set prior to loading, will be
            // replaced with the lightingmode stored in the file.
            tvm.SetLightingMode(CONST_TV_LIGHTINGMODE.TV_LIGHTING_BUMPMAPPING_TANGENTSPACE);

            double InvRoundness = 1 - Roundness;
            double TwoPi = 2*Math.PI;
            double HalfPi = Math.PI/2;

            for (int Stack = 0; Stack < Stacks - 1; Stack++)
            {
                for (int Slice = 0; Slice < Slices; Slice++)
                {
                    double StackFactor = ((double) Stack/(double) (Stacks - 1));
                    double UpperStackFactor = (Stack + 1)/(double) (Stacks - 1);
                    double SliceFactor = Slice/(double) (Slices - 1);

                    double StackAngle = StackFactor*TwoPi + HalfPi;
                    double UpperStackAngle = UpperStackFactor*TwoPi + HalfPi;
                    double SliceAngle = SliceFactor*TwoPi;

                    TV_3DVECTOR Position = new TV_3DVECTOR(
                        (float) (Math.Sin(SliceAngle)*Math.Cos(StackAngle)),
                        (float) (Math.Sin(StackAngle)),
                        (float) (Math.Cos(SliceAngle)*Math.Cos(StackAngle)));

                    TV_3DVECTOR UpperPosition = new TV_3DVECTOR(
                        (float) (Math.Sin(SliceAngle)*Math.Cos(UpperStackAngle)),
                        (float) (Math.Sin(UpperStackAngle)),
                        (float) (Math.Cos(SliceAngle)*Math.Cos(UpperStackAngle)));

                    // UVs are top-down planar, so only depend on X and Z
                    // ...which appears as a rounded texture projection
                    TV_2DVECTOR PlanarUV = new TV_2DVECTOR(Position.x, Position.z)/2;
                    TV_2DVECTOR UpperPlanarUV = new TV_2DVECTOR(UpperPosition.x, UpperPosition.z)/2;

                    // But a tangent-warping factor can make it look planar on all sides
                    // This was tweaked until the results looks OK...
                    float TangentFactor =
                        (float) ((1 + Math.Tan((StackAngle - HalfPi)*InvRoundness))*Math.Sqrt(Roundness));
                    float UpperTangentFactor =
                        (float) ((1 + Math.Tan((UpperStackAngle - HalfPi)*InvRoundness))*Math.Sqrt(Roundness));

                    tvm.AddVertex(Position.x, Position.y, Position.z,
                                   0, Position.y, 0,
                                   PlanarUV.x*TangentFactor,
                                   PlanarUV.y*TangentFactor);

                    tvm.AddVertex(UpperPosition.x, UpperPosition.y, UpperPosition.z,
                                   0, UpperPosition.y, 0,
                                   UpperPlanarUV.x*UpperTangentFactor,
                                   UpperPlanarUV.y*UpperTangentFactor);
                }
            }

            tvm.SetCollisionEnable(false);
            // We're inside the mesh, so cull the front-faces
            tvm.SetCullMode(CONST_TV_CULLING.TV_FRONT_CULL);
            return new Mesh3d(path, tvm);
        }


		// TODO: the labels for this will have to be done in the HUD itself... 
		//      - we can easily compute the intersection of each LAT/LONG line via LONG_SPACING and LAT_SPACING
		//      and from that we can get the x,y,z coords which we can use to draw 3d text
		//      CoreClient._CoreClient.Text.TextureFont_DrawBillboardText ("-10�", x, y, z, color);
		//      Renderable3DText text = new Renderable3DText();
		//      - actually we can compute these labels ahead of time and add them to the mGalacticGrid entity as a child and they will inherit
		//      the scale.  And I think it's an ok way to do this because these labels unlike other lables are much more part of the entity
		//      itself.  
        public static Mesh3d CreateLatLongSphere (int theta_degrees, int phi_degrees)
        {

        	string filename = string.Format(PRIMITIVE_LATLONG_LINELIST_SPHERE + "_{0}_{1}", theta_degrees, phi_degrees);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(path);
            
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_LINELIST);
            
            // change meshformat since we only need POSITION and DIFFUSE
            bool result = tvm.SetMeshFormat ((int)(CONST_TV_MESHFORMAT.TV_MESHFORMAT_DIFFUSE | CONST_TV_MESHFORMAT.TV_MESHFORMAT_NOLIGHTING));
            if (result == false) throw new Exception();
            
            
            int NUM_SEGMENTS = 61; // number of segments in each circle that makes up a lat or long line.  If we intend to enable/disable rendering of polar caps, then NUM_SEGMENTS should be multiple of LAT_SPACING
            int NUM_LAT_LINES = 11; // odd number where there are same number of belts above and below the middle equator belt
            int NUM_LONG_LINES = 12; // 12 complete circles or 24 half circles around the globe
			int NUM_LAT_LINES_BELOW_EQUATER = 5;
            // NOTE: the 15 degrees is chosen to match with 12 NUM_LONG_LINES since that yields
			//        360 / 15 = 24 but each of our 12 lines actually cover left and right hemispheres at same time thus covering all 24 increments at 15 degree spacing
			//       If we were to use 20 degree LONG_SPACING we would need 360 / 20 = 18 / 2 = 9 NUM_LONG_LINES
			int LONG_SPACING = 15;
			int LAT_SPACING = 15;
			double TWO_PI = Math.PI * 2d;
			
            double radius = 1.0d; // unit sphere
            
			double lng, lat, xz, y;

			Vector3d point;
			Vector3d prevPoint = Vector3d.Zero();
			
			// Lines of latitude appear horizontal with varying curvature
			// They run parallel with equator like belts
			// The latitude specifies a location's distance north or south of the equator
			for (int j = 0; j < NUM_LAT_LINES; j++)
			{
				// the lat lines below equator will have negative lat values
				lat = (j - NUM_LAT_LINES_BELOW_EQUATER ) * LAT_SPACING * Utilities.MathHelper.DEGREES_TO_RADIANS;
				xz = radius * Math.Cos(lat);
				y  = radius * Math.Sin(lat);
				// System.Diagnostics.Debug.WriteLine ("CreateLatLongSphere() - y:" + y.ToString ());
				
				for (int i = 0; i < NUM_SEGMENTS; i++)
				{
					lng = TWO_PI * (double)i / ((double)NUM_SEGMENTS - 1d);
					point.x = xz * Math.Cos(lng);
					point.z = xz * Math.Sin(lng);
					point.y = y;
					
					// NOTE: unfortunatley we're creating a line list and not a linestrip.  TV3D Mesh does
					//       not support LINESTRIP so we have to connect our lines 					
					if (i > 0)
					{
						tvm.AddVertex((float)prevPoint.x, (float)prevPoint.y, (float)prevPoint.z, 0, 0, 0, 0, 0);
						tvm.AddVertex((float)point.x, (float)point.y, (float)point.z, 0, 0, 0, 0, 0);
					}
					
					prevPoint = point;
				}
			}


			double POLAR_ARC = 0.08d;
			// Lines of longitude appear vertical with varying curvature
			// They run up down like hot air ballon cables
			// The longitude specifies the location's distance east or west from an imaginary line connecting the North and South Poles, called the Prime Meridian. 
			for (int j = 0; j < NUM_LONG_LINES; j++) 
			{
				lng = j * LONG_SPACING * Utilities.MathHelper.DEGREES_TO_RADIANS;
				
				// each longitudinal line starts at the equator on one hemisphere and goes up +y Axis to top, then -y axis to bottom and +y again 
				// eventually ends back at the equator on the same hemisphere for a complete circuit
				for (int i = 0; i < NUM_SEGMENTS; i++) 
				{
					lat = TWO_PI * (double)i / ((double)NUM_SEGMENTS - 1d);
					
					xz = radius * Math.Cos(lat);
					y  = radius * Math.Sin(lat);
					point.x = xz * Math.Cos(lng);
					point.z = xz * Math.Sin(lng);
					point.y = y;
					
  					//System.Diagnostics.Debug.WriteLine ("CreateLatLongSphere() - y:" + y.ToString ());
//                  // epsilon?
  					// skip segments within POLAR_ARC degrees of poles on either side
  					// TODO: a much more reliable way is to not render x% of the segments at top and bottom
					//       however, our prevPoint  
					// TODO: I think it'll just be easier to count the actual segment indices and skip certain ranges rather than
					//       try to test altitude					
					if ((y > radius - POLAR_ARC) ||
					    (y < -radius + POLAR_ARC))
					{
						prevPoint = point;
						continue;
					}
					
					// NOTE: unfortunatley we're creating a line list and not a linestrip.  TV3D Mesh does
					//       not support LINESTRIP so we have to connect our lines 					
					if (i > 0)
					{
						tvm.AddVertex((float)prevPoint.x, (float)prevPoint.y, (float)prevPoint.z, 0, 0, 0, 0, 0);
						tvm.AddVertex((float)point.x, (float)point.y, (float)point.z, 0, 0, 0, 0, 0);
					}
							
					prevPoint = point;
				}
			}
			
			
            tvm.SetColor(CoreClient._CoreClient.Globals.RGBA(.6f, .6f, .6f, .6f));
            tvm.EnableFrustumCulling(false);
            
			mesh = new Mesh3d(path, tvm); 
			mesh.mLineList = true;
			return mesh;
        }
        
        // step theta_degrees and step phi_degrees
        public static Mesh3d CreateSphereWireframeLineList(int theta_degrees, int phi_degrees)
        {

            string filename = string.Format(PRIMITIVE_WIREFRAME_LINELIST_SPHERE + "_{0}_{1}", theta_degrees, phi_degrees);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(path);
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_LINELIST);
			
			// change meshformat since we only need POSITION and DIFFUSE
            bool result = tvm.SetMeshFormat ((int)(CONST_TV_MESHFORMAT.TV_MESHFORMAT_DIFFUSE | CONST_TV_MESHFORMAT.TV_MESHFORMAT_NOLIGHTING));
            if (result == false) throw new Exception();
            
            int n;
            double theta, phi;
            Vector3d[] p;
            double DTOR = Utilities.MathHelper.DEGREES_TO_RADIANS;

            for (theta = -90; theta <= 90 - theta_degrees; theta += theta_degrees)
            {
                for (phi = 0; phi <= 360 - phi_degrees; phi += phi_degrees)
                {
                    double cos_theta_dtor = Math.Cos(theta * DTOR);
                    double sin_theta_dtor = Math.Sin(theta * DTOR);
                    double cos_phi_dtor = Math.Cos(phi*DTOR);
                    double sin_ph_dtor = Math.Sin(phi*DTOR);
                    double cos_thetaplusdtheta_dtor = Math.Cos((theta + theta_degrees)*DTOR);
                    double sin_thetaplusdtheta_dtor = Math.Sin((theta + theta_degrees)*DTOR);
                    double cos_phiplusdphi_dtor = Math.Cos((phi + phi_degrees)*DTOR);
                    double sin_phiplusdphi_dtor = Math.Sin((phi + phi_degrees)*DTOR);

                    if (theta > -90 && theta < 90)
                        p = new Vector3d[4];
                    else
                        p = new Vector3d[3]; // poles

                    n = 0;
                    p[n].x = cos_theta_dtor*cos_phi_dtor;
                    p[n].y = cos_theta_dtor*sin_ph_dtor;
                    p[n].z = sin_theta_dtor;
                    n++;
                    
                    p[n].x = cos_thetaplusdtheta_dtor*cos_phi_dtor;
                    p[n].y = cos_thetaplusdtheta_dtor*sin_ph_dtor;
                    p[n].z = sin_thetaplusdtheta_dtor;
                    n++;
                    
                    p[n].x = cos_thetaplusdtheta_dtor*cos_phiplusdphi_dtor;
                    p[n].y = cos_thetaplusdtheta_dtor*sin_phiplusdphi_dtor;
                    p[n].z = sin_thetaplusdtheta_dtor;
                    n++;
                    
                    if (theta > -90 && theta < 90)
                    {
                        p[n].x = cos_theta_dtor*cos_phiplusdphi_dtor;
                        p[n].y = cos_theta_dtor*sin_phiplusdphi_dtor;
                        p[n].z = sin_theta_dtor;
                        n++;
                    }

                    // Do something with the n vertex facet p 
                    tvm.AddVertex((float)p[0].x, (float)p[0].y, (float)p[0].z, 0, 0, 0, 0, 0);
                    tvm.AddVertex((float)p[1].x, (float)p[1].y, (float)p[1].z, 0, 0, 0, 0, 0);


                    tvm.AddVertex((float)p[1].x, (float)p[1].y, (float)p[1].z, 0, 0, 0, 0, 0);
                    tvm.AddVertex((float)p[2].x, (float)p[2].y, (float)p[2].z, 0, 0, 0, 0, 0);


                    if (p.Length == 3)
                    {
                        tvm.AddVertex((float)p[2].x, (float)p[2].y, (float)p[2].z, 0, 0, 0, 0, 0);
                        tvm.AddVertex((float)p[0].x, (float)p[0].y, (float)p[0].z, 0, 0, 0, 0, 0);
                    }
                    else if (p.Length == 4)
                    {
                        tvm.AddVertex((float)p[2].x, (float)p[2].y, (float)p[2].z, 0, 0, 0, 0, 0);
                        tvm.AddVertex((float)p[3].x, (float)p[3].y, (float)p[3].z, 0, 0, 0, 0, 0);

                        tvm.AddVertex((float)p[3].x, (float)p[3].y, (float)p[3].z, 0, 0, 0, 0, 0);
                        tvm.AddVertex((float)p[0].x, (float)p[0].y, (float)p[0].z, 0, 0, 0, 0, 0);

                    }
                }
            }

            tvm.SetColor(CoreClient._CoreClient.Globals.RGBA(.6f, .6f, .6f, .6f));
            tvm.EnableFrustumCulling(false);
            
			mesh = new Mesh3d(path, tvm); 
			mesh.mLineList = true;
			return mesh;
        }

        //public static Mesh3d CreateCylinderLineList(int NFACETS, float radiusTop, float radiusBottom)
        //{

        //    if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("Mesh3d.CreateCylinderLineList() - 'id' cannot be empty");

        //    Mesh3d mesh = (Mesh3d)Repository.Get(id);
        //    if (mesh != null) return mesh;
        // string path = filename; // no full path needed for primitives
        //    TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(id);
        //    tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_LINELIST);

        //    Vector3d[] p = new Vector3d[NFACETS * 2];

        //    for (int i = 0; i < NFACETS; i++) 
        //    {
        //      int n = 0;
        //      float theta1 = i * Utilities.MathHelper.TWO_PI / (float)NFACETS;
        //      float theta2 = (i + 1) * Utilities.MathHelper.TWO_PI / (float)NFACETS;
        //      p[n].x = P1.x + radiusTop * Math.Cos(theta1) * A.x + radiusTop * Math.Sin(theta1) * B.x;
        //      p[n].y = P1.y + radiusTop * Math.Cos(theta1) * A.y + radiusTop * Math.Sin(theta1) * B.y;
        //      p[n].z = P1.z + radiusTop * Math.Cos(theta1) * A.z + radiusTop * Math.Sin(theta1) * B.z;
        //      n++;
        //      p[n].x = P2.x + radiusBottom * Math.Cos(theta1) * A.x + radiusBottom * Math.Sin(theta1) * B.x;
        //      p[n].y = P2.y + radiusBottom * Math.Cos(theta1) * A.y + radiusBottom * Math.Sin(theta1) * B.y;
        //      p[n].z = P2.z + radiusBottom * Math.Cos(theta1) * A.z + radiusBottom * Math.Sin(theta1) * B.z;
        //      n++;
        //      if (radiusBottom != 0)
        //      {
        //          p[n].x = P2.x + radiusBottom * Math.Cos(theta2) * A.x + radiusBottom * Math.Sin(theta2) * B.x;
        //          p[n].y = P2.y + radiusBottom * Math.Cos(theta2) * A.y + radiusBottom * Math.Sin(theta2) * B.y;
        //          p[n].z = P2.z + radiusBottom * Math.Cos(theta2) * A.z + radiusBottom * Math.Sin(theta2) * B.z;
        //         n++;
        //      }
        //      if (radiusTop != 0)
        //      {
        //          p[n].x = P1.x + radiusTop * Math.Cos(theta2) * A.x + radiusTop * Math.Sin(theta2) * B.x;
        //          p[n].y = P1.y + radiusTop * Math.Cos(theta2) * A.y + radiusTop * Math.Sin(theta2) * B.y;
        //          p[n].z = P1.z + radiusTop * Math.Cos(theta2) * A.z + radiusTop * Math.Sin(theta2) * B.z;
        //         n++;
        //      }
        //      //do something with p[0..n]
        //   }

        //    tvm.SetColor(CoreClient._CoreClient.Globals.RGBA(1f, 1f, 1f, 1f));
        //    tvm.EnableFrustumCulling(false);
        //    return new Mesh3d(id, tvm); ;
        //}

        /* Create a cone/cylinder uncapped between end points p1, p2 
         * radius r1, r2, and precision m Create the cylinder between theta1 and theta2 in radians */
        public static Mesh3d CreateCone(Vector3d p1, Vector3d p2, double r1, double r2, int m, double theta1, double theta2) 
        {
            string filename = string.Format(PRIMITIVE_CONE + "_{0}_{1}_{2}_{3}_{4}_{5}_{6}", p1.ToString(), p2.ToString(), r1, r2, m, theta1, theta2);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(path);
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_LINELIST);

            Vector3d p;
            
            //int j; 

            
            Vector3d n, q, perp; 
            
            /* Normal pointing from p1 to p2 */ 
            n.x = p1.x - p2.x; 
            n.y = p1.y - p2.y; 
            n.z = p1.z - p2.z; 
            
            /* Create two perpendicular vectors perp and q on the plane of the disk */ 
            perp = n; 
            
            if (n.x == 0 && n.z == 0) 
                n.x += 1; 
            else n.y += 1; 
            
            q = Vector3d.CrossProduct (perp, n); 
            perp = Vector3d.CrossProduct (n, q);; 
            perp.Normalize(); 
            q.Normalize(); 
            

            for (int i = 0; i <= m; i++) 
            {
                double theta = theta1 + i * (theta2 - theta1) / m;
                n.x = Math.Cos(theta) * perp.x + Math.Sin(theta) * q.x;
                n.y = Math.Cos(theta) * perp.y + Math.Sin(theta) * q.y;
                n.z = Math.Cos(theta) * perp.z + Math.Sin(theta) * q.z; 
                n.Normalize(); 
                p.x = p2.x + r2 * n.x; 
                p.y = p2.y + r2 * n.y; 
                p.z = p2.z + r2 * n.z;

                tvm.AddVertex((float)p.x, (float)p.y, (float)p.z, 0, 0, 0, 0, 0);
                //glNormal3f(n.x, n.y, n.z); 
                //glTexCoord2f(i / (double)m, 1.0); 
                //glVertex3f(p.x, p.y, p.z);
                
                p.x = p1.x + r1 * n.x; 
                p.y = p1.y + r1 * n.y; 
                p.z = p1.z + r1 * n.z;

                tvm.AddVertex((float)p.x, (float)p.y, (float)p.z, 0, 0, 0, 0, 0);
                //glNormal3f(n.x, n.y, n.z); 
                //glTexCoord2f(i / (double)m, 0.0); 
                //glVertex3f(p.x, p.y, p.z); 
            }

            tvm.SetColor(CoreClient._CoreClient.Globals.RGBA(.6f, .6f, .6f, 1f));
            tvm.EnableFrustumCulling(false);
            
			mesh = new Mesh3d(path, tvm); 
			mesh.mLineList = true;
			return mesh;
        }

        public static Mesh3d CreateCircle(int numSegments, float radius, bool dashedSegments, bool quadLines, float thickness = 1.0f)
        {
            string filename = string.Format(PRIMITIVE_CIRCLE + "_{0}_{1}", numSegments, radius);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;
            
            Vector3d[] points = Keystone.Primitives.Ellipse.CreateCirclePoints(numSegments, radius);

            TVMesh tvm = null;
            
            if (quadLines)
            	tvm = Create3DLineStripTVM (path, points, false, thickness);
           	else 
           		tvm = CreateLineListTVM(path, points, false, dashedSegments, false); // false because Keystone.Primitives.Ellipse.CreateCirclePoints already closes the loop for us
           	
			mesh = new Mesh3d(path, tvm); 
			mesh.mLineList = true;
			return mesh;
        }

        public static Mesh3d CreateEllipse(float semimajoraxis, float eccentricity, int numSegments, bool quadLines, float thickness = 1.0f)
        {
            string filename = string.Format(PRIMITIVE_ELLIPSE + "_{0}_{1}_{2}", semimajoraxis, eccentricity, numSegments);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;

            Vector3d[] points = Keystone.Primitives.Ellipse.CreateEllipse(semimajoraxis, eccentricity, numSegments);
            
            TVMesh tvm = null;
            if (quadLines)
            	tvm = Create3DLineStripTVM (path, points, false, thickness);
           	else 
           		tvm = CreateLineListTVM(path, points, false, false, false); // false because Keystone.Primitives.Ellipse.CreateEllipse already closes the loop for us
            
			mesh = new Mesh3d(path, tvm); 
			mesh.mLineList = true;
			return mesh;
        }

    	public static Mesh3d Create3DLineList(string id, Vector3d[] points, bool dashedLines, bool quadLines, float thickness = 1.0f)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("Mesh3d.Create3DLine() - 'id' cannot be empty");
            Mesh3d mesh = (Mesh3d)Repository.Get(id);
            if (mesh != null) return mesh;
            
           //throw new Exception("TODO: add support for serializing this type of PRIMITIVE");

            TVMesh tvm = null;
            if (quadLines)
            {
            	if (thickness <= 0) throw new ArgumentOutOfRangeException("Mesh3d.Create3DLine() - 'Thickness' must be > 0.0f");
//
//	            if (points == null ||
//	        	    points.Length < 3) throw new ArgumentOutOfRangeException ("Mesh3d.Create3DLine() - Mininum of 3 points must be supplied for thick 2-D trianglestrip based lines.");
//	            
            	// here a segment connecting the end point back to the first will automatically be created??
            	tvm = Create3DLineStripTVM (id, points, true, thickness);
            }
            else
			{ 
				tvm = CreateLineListTVM(id, points, true, dashedLines, false);	
			}
			
			mesh = new Mesh3d(id, tvm); 
			mesh.mLineList = true;
			return mesh;
        }
                
        private static TVMesh CreateLineListTVM(string path, Vector3d[] points, bool discreteLines, bool dashedLines, bool closeLoop)
        {       
			// NOTE: for LINELIST primitive type, it _is_verified_ that only 2 points are required (and NOT at least 3 to form a degenerate triangle)        	
        	if (points == null ||
        	    points.Length < 2) throw new ArgumentOutOfRangeException ("Mesh3d.CreateLineTVM() - Mininum of 2 points must be supplied for 1-D lines.");
        	
            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(path);
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_LINELIST);

			
            // 2D lines only need a position and a color.  No need for texturecoords or normals
            TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[2];
            elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position
            elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;  // color
            elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
            elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_DIFFUSE;
            elements[0].stream = 0;
            elements[1].stream = 0;

            // "MeshFormat" is more accurately called VertexFormat
            tvm.SetMeshFormatEx(elements, elements.Length);

            // change meshformat since we only need POSITION and DIFFUSE
            //bool result = tvm.SetMeshFormat ((int)(CONST_TV_MESHFORMAT.TV_MESHFORMAT_DIFFUSE | CONST_TV_MESHFORMAT.TV_MESHFORMAT_NOLIGHTING));
            //if (result == false) throw new Exception();

            int step = 1;
            if (discreteLines || dashedLines)
                step = 2;

            // NOTE: each itteration adds 2 vertices so with a step of 1 and points.Length == 20, the generated mesh will have 40 vertices
            for (int i = 0; i < points.Length - step; i+=step)
            {
                Vector3d start = points[i];
                Vector3d end;
                if (!discreteLines && closeLoop && i == points.Length - 1)
                    end = points[0]; // connect back to first if at the last point
                else
                {
                    end = points[i + 1];  // default is end is the next point in the list              	
                }
                
                // TODO: with the modified format above, does calling .AddVertex instead of .AddVertexEx
				//       revert the format back to default since the arguments include vertex elements
				//       we have not (and do not want) defined?
                tvm.AddVertex((float)start.x, (float)start.y, (float)start.z, 0, 0, 0, 0, 0);
                tvm.AddVertex((float)end.x, (float)end.y, (float)end.z, 0, 0, 0, 0, 0);
                
                // TODO: test following version using AddVertexEx()
//                float[] vertex = new float[7];
//                vertex[0] = (float)start.x;
//                vertex[1] = (float)start.y;
//                vertex[2] = (float)start.z;
//                TV_COLOR color;
//                color.r = 1.0f;
//                color.g = 0.0f;
//                color.b = 0.0f;
//                color.a = 1.0f;
//                // NOTE: the actual color values be unnecessary if we supply it in material
//                vertex[3] = color.r;
//                vertex[4] = color.g;
//                vertex[5] = color.b;
//                vertex[6] = color.a;
//                tvm.AddVertexEx (ref vertex[0], vertex.Length);
                
                if (closeLoop == false && i == points.Length - 2) 
                	break;                
            }
    
            tvm.SetColor(CoreClient._CoreClient.Globals.RGBA(1f, 1f, 1f, 1f));
            return tvm; ;
        }

        /// <summary>
        /// Generate 3d lines that rely on the shader @"caesar\shaders\VolumeLines.fx"
        /// </summary>
        /// <param name="id"></param>
        /// <param name="points"></param>
        /// <param name="discreteLines">Indicates whether every 2 points represents a discrete line.  If false, the lines are connected whereby each point forms a line with the next point</param>
        /// <param name="thickness"></param>
        /// <returns></returns>
        private static TVMesh Create3DLineStripTVM(string id, Vector3d[] points, bool discreteLines, float thickness)
        {
            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(id);
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_TRIANGLESTRIP);
            
            TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[4];
            elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position start
            elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position end
            elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;  // uv offset
            elements[3].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // thickness

            elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
            elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
            elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;
            elements[3].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD1;

            elements[0].stream = 0;
            elements[1].stream = 0;
            elements[2].stream = 0;
            elements[3].stream = 0;

            tvm.SetMeshFormatEx(elements, elements.Length);
            tvm.CancelGeneration();

            int elementSizeFloats = 13;
            float[] vertex = new float[elementSizeFloats];
            const int bytesInFloat = 4;
            int sizeBytes = elementSizeFloats * bytesInFloat;

            int step = 1;
            if (discreteLines) step = 2;

            for (int i = 0; i < points.Length - 1; i+= step)
            {
                Vector3d start = points[i];
                Vector3d end = points[i + 1];

                // vertex 1
                vertex[0] = (float)start.x; 
                vertex[1] = (float)start.y; 
                vertex[2] = (float)start.z; 

                vertex[3] = (float)end.x; 
                vertex[4] = (float)end.y; 
                vertex[5] = (float)end.z; 

                // uv offsets
                vertex[6] = thickness; 
                vertex[7] = thickness; 
                vertex[8] = 0.0f; 
                vertex[9] = 0.0f; 

                // thickness
                vertex[10] = -thickness; 
                vertex[11] = 0.0f; 
                vertex[12] = thickness * 0.5f;

                tvm.AddVertexEx(ref vertex[0], sizeBytes);
                /////////////////////////////////////////////
                // vertex 2
                vertex[0] = (float)end.x;
                vertex[1] = (float)end.y;
                vertex[2] = (float)end.z;

                vertex[3] = (float)start.x;
                vertex[4] = (float)start.y;
                vertex[5] = (float)start.z;

                    // uv offsets
                vertex[6] = thickness;
                vertex[7] = thickness;
                vertex[8] = 0.25f;
                vertex[9] = 0.0f;

                // thickness
                vertex[10] = thickness;
                vertex[11] = 0.0f;
                vertex[12] = thickness * 0.5f;

                tvm.AddVertexEx(ref vertex[0], sizeBytes);
                /////////////////////////////////////////////
                // vertex 3
                vertex[0] = (float)start.x;
                vertex[1] = (float)start.y;
                vertex[2] = (float)start.z;

                vertex[3] = (float)end.x;
                vertex[4] = (float)end.y;
                vertex[5] = (float)end.z;

                // uv offsets
                vertex[6] = thickness;
                vertex[7] = thickness;
                vertex[8] = 0.0f;
                vertex[9] = 0.25f;

                // thickness
                vertex[10] = thickness;
                vertex[11] = 0.0f;
                vertex[12] = thickness * 0.5f;

                tvm.AddVertexEx(ref vertex[0], sizeBytes);
                /////////////////////////////////////////////
                // vertex 4
                vertex[0] = (float)end.x;
                vertex[1] = (float)end.y;
                vertex[2] = (float)end.z;

                vertex[3] = (float)start.x;
                vertex[4] = (float)start.y;
                vertex[5] = (float)start.z;

                // uv offsets
                vertex[6] = thickness;
                vertex[7] = thickness;
                vertex[8] = 0.25f;
                vertex[9] = 0.25f;

                // thickness
                vertex[10] = -thickness;
                vertex[11] = 0.0f;
                vertex[12] = thickness * 0.5f;

                tvm.AddVertexEx(ref vertex[0], sizeBytes);
            }

            return tvm;
        }

        public static Mesh3d Create3DLine(Vector3d start, Vector3d end, float thickness)
        {
            if (thickness <= 0) throw new ArgumentOutOfRangeException("Mesh3d.Create3DLine() - 'Thickness' must be > 0.0f");


            string filename = string.Format(PRIMITIVE_3D_LINE + "_{0}_{1}_{2}", start, end, thickness);
            string path = filename; // no full path needed for primitives

            Mesh3d mesh = (Mesh3d)Repository.Get(path);
            if (mesh != null) return mesh;


            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(path);
            // to use custom MeshFormat we must also use SetVertexEx()  not SetVertex()
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_TRIANGLESTRIP);

            TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[4];
            elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position start
            elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position end
            elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;  // uv offset
            elements[3].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // thickness

            elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
            elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
            elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;
            elements[3].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD1;

            elements[0].stream = 0;
            elements[1].stream = 0;
            elements[2].stream = 0;
            elements[3].stream = 0;

            tvm.SetMeshFormatEx(elements, elements.Length);

            int numVerts = 4;
            int elementSizeFloats = 13;
            float[] vertex = new float[elementSizeFloats * numVerts];
            const int bytesInFloat = 4;
            int sizeBytes = elementSizeFloats * bytesInFloat;

            // vertex 1
            vertex[0] = (float)start.x;
            vertex[1] = (float)start.y;
            vertex[2] = (float)start.z;

            vertex[3] = (float)end.x;
            vertex[4] = (float)end.y;
            vertex[5] = (float)end.z;

            // uv offsets
            vertex[6] = thickness;
            vertex[7] = thickness;
            vertex[8] = 0.0f;
            vertex[9] = 0.0f;

            // thickness
            vertex[10] = -thickness;
            vertex[11] = 0.0f;
            vertex[12] = thickness * 0.5f;

            tvm.AddVertexEx(ref vertex[elementSizeFloats * 0], sizeBytes);
            /////////////////////////////////////////////
            // vertex 2
            vertex[13] = (float)end.x;
            vertex[14] = (float)end.y;
            vertex[15] = (float)end.z;

            vertex[16] = (float)start.x;
            vertex[17] = (float)start.y;
            vertex[18] = (float)start.z;

            // uv offsets
            vertex[19] = thickness;
            vertex[20] = thickness;
            vertex[21] = 0.25f;
            vertex[22] = 0.0f;

            // thickness
            vertex[23] = thickness;
            vertex[24] = 0.0f;
            vertex[25] = thickness * 0.5f;

            tvm.AddVertexEx(ref vertex[elementSizeFloats * 1], sizeBytes);
            /////////////////////////////////////////////
            // vertex 3
            vertex[26] = (float)start.x;
            vertex[27] = (float)start.y;
            vertex[28] = (float)start.z;

            vertex[29] = (float)end.x;
            vertex[30] = (float)end.y;
            vertex[31] = (float)end.z;
            
            // uv offsets
            vertex[32] = thickness;
            vertex[33] = thickness;
            vertex[34] = 0.0f;
            vertex[35] = 0.25f;

            // thickness
            vertex[36] = thickness;
            vertex[37] = 0.0f;
            vertex[38] = thickness * 0.5f;

            tvm.AddVertexEx(ref vertex[elementSizeFloats * 2], sizeBytes);
            /////////////////////////////////////////////
            // vertex 3
            vertex[39] = (float)end.x;
            vertex[40] = (float)end.y;
            vertex[41] = (float)end.z;

            vertex[42] = (float)start.x;
            vertex[43] = (float)start.y;
            vertex[44] = (float)start.z;

            // uv offsets
            vertex[45] = thickness;
            vertex[46] = thickness;
            vertex[47] = 0.25f;
            vertex[48] = 0.25f;

            // thickness
            vertex[49] = -thickness;
            vertex[50] = 0.0f;
            vertex[51] = thickness * 0.5f;


            tvm.AddVertexEx(ref vertex[elementSizeFloats * 3], sizeBytes);
            tvm.EnableFrustumCulling(false);

            return new Mesh3d(path, tvm);
        }

        public static Mesh3d CreateTriangleStripLine(string id, Vector3d[] vertices, Vector3d[] normals, Vector4[] thickness, Vector2f[] uv1)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException();
            if (vertices == null || vertices.Length < 4) throw new ArgumentOutOfRangeException("Trianglestrip requires at least 4 verices");
            if (normals != null && normals.Length != vertices.Length) throw new ArgumentOutOfRangeException("Normals array must either be null or be same size as vertices.");
            if (uv1 != null && uv1.Length != vertices.Length) throw new ArgumentOutOfRangeException("UV array must either be null or be same size as vertices.");


            Mesh3d mesh = (Mesh3d)Repository.Get(id);
            if (mesh != null) return mesh;

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(id);

            // to use custom MeshFormat we must also use SetVertexEx()  not SetVertex()
            //TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[4];
            //elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position start
            //elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position end
            //elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;  // thickness
            //elements[3].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // uv offset

            //elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
            //elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
            //elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;
            //elements[3].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;

            //elements[0].stream = 0;
            //elements[1].stream = 0;
            //elements[2].stream = 0;
            //elements[3].stream = 0;

            //tvm.SetMeshFormatEx(elements, elements.Length);

            TV_3DVECTOR position;
            TV_3DVECTOR normal;
            TV_2DVECTOR uv;
            int count = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                position.x = (float)vertices[i].x;
                position.y = (float)vertices[i].y;
                position.z = (float)vertices[i].z;

                if (normals == null)
                {
                    normal.x = 0; // (float)normals[i].x;
                    normal.y = 1; //  (float)normals[i].y;
                    normal.z = 0; // (float)normals[i].z;
                }
                else
                {
                    normal.x = (float)normals[i].x;
                    normal.y = (float)normals[i].y;
                    normal.z = (float)normals[i].z;
                }
                if (uv1 == null)
                {
                    // we can try alternating the uv's to tile the texture
                    // but the fact is, if the user doesnt pass in UVs we cant know
                    // for sure the desired uv mapping
                    uv.x = 0;
                    uv.y = 0;

                    if (count == 0)
                    {
                        uv.x = 0.0f;
                        uv.y = 0.0f;
                    }
                    else if (count == 1)
                    {
                        uv.x = 1.0f;
                        uv.y = 0.0f;
                    }
                    else if (count == 2)
                    {
                        uv.x = 0.0f;
                        uv.y = 1.0f;
                    }
                    else if (count == 3)
                    {
                        uv.x = 1.0f;
                        uv.y = 1.0f;
                        count = -1;
                    }
                    count++;
                }
                else
                {
                    uv.x = uv1[i].x;
                    uv.y = uv1[i].y;
                }


                tvm.AddVertex(position.x, position.y, position.z,
                    normal.x, normal.y, normal.z,
                    uv.x, uv.y);

            }

            tvm.EnableFrustumCulling(false);
            return new Mesh3d(id, tvm);

        }

        public static Mesh3d CreateTriangleStrip(string id, Vector3d[] vertices, Vector3d[] normals, Vector2f[] uv1)
        {
            if (string.IsNullOrEmpty (id)) throw new ArgumentNullException();
            if (vertices == null || vertices.Length < 4) throw new ArgumentOutOfRangeException ("Trianglestrip requires at least 4 verices");
            if (normals != null && normals.Length != vertices.Length) throw new ArgumentOutOfRangeException("Normals array must either be null or be same size as vertices.");
            if (uv1 != null && uv1.Length != vertices.Length) throw new ArgumentOutOfRangeException("UV array must either be null or be same size as vertices.");


            Mesh3d mesh = (Mesh3d)Repository.Get(id);
            if (mesh != null) return mesh;

            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(id);

            // to use custom MeshFormat we must also use SetVertexEx()  not SetVertex()
            //TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[4];
            //elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position start
            //elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // position end
            //elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;  // thickness
            //elements[3].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;  // uv offset

            //elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
            //elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
            //elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;
            //elements[3].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;

            //elements[0].stream = 0;
            //elements[1].stream = 0;
            //elements[2].stream = 0;
            //elements[3].stream = 0;

            //tvm.SetMeshFormatEx(elements, elements.Length);

            TV_3DVECTOR position;
            TV_3DVECTOR normal;
            TV_2DVECTOR uv;
            int count = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                position.x = (float)vertices[i].x;
                position.y = (float)vertices[i].y;
                position.z = (float)vertices[i].z;

                if (normals == null)
                {
                    normal.x = 0; // (float)normals[i].x;
                    normal.y = 1; //  (float)normals[i].y;
                    normal.z = 0; // (float)normals[i].z;
                }
                else
                {
                    normal.x = (float)normals[i].x;
                    normal.y = (float)normals[i].y;
                    normal.z = (float)normals[i].z;
                }
                if (uv1 == null)
                {
                    // we can try alternating the uv's to tile the texture
                    // but the fact is, if the user doesnt pass in UVs we cant know
                    // for sure the desired uv mapping
                    uv.x = 0;
                    uv.y = 0;

                    if (count == 0)
                    {
                        uv.x = 0.0f;
                        uv.y = 0.0f;                    
                    }
                    else if (count == 1)
                    {
                        uv.x = 1.0f;
                        uv.y = 0.0f;
                    }
                    else if (count == 2)
                    {
                        uv.x = 0.0f;
                        uv.y = 1.0f;
                    }
                    else if (count == 3)
                    {
                        uv.x = 1.0f;
                        uv.y = 1.0f;
                        count = -1; 
                    }
                    count++;
                }
                else
                {
                    uv.x = uv1[i].x;
                    uv.y = uv1[i].y;
                }


                tvm.AddVertex(position.x, position.y, position.z,
                    normal.x, normal.y, normal.z,
                    uv.x, uv.y);

            }

            tvm.EnableFrustumCulling(false);
            return new Mesh3d(id, tvm);
        }
            
        ///<summary>
        /// Creates a model space XZ cross section Polygon of a Mesh3d at y plane altitude
        /// </summary>
        public static Polygon CreateCrossSection(Mesh3d mesh, float y, float step)
        {
            if (mesh == null ||
                mesh.TVResourceIsLoaded == false ||
                step <= float.Epsilon) throw new ArgumentOutOfRangeException();

            // TODO: test if the y value within the upper min/max y bounds of the mesh?
            // if not this could indicate that we're unexpectedly sending in an un-centered mesh
            // supply a warning
            // TODO: test if the min/max y values are roughly equal and that the bounding box is
            // center is roughly 0,0,0

            System.Collections.Generic.List<Vector3d> points = new System.Collections.Generic.List<Vector3d>();
            double RAY_SCALE = 1000000000d;

            Triangle[] triangles = mesh.GetTriangles();
            Vector3d origin;
            origin.x = 0;
            origin.y = y;
            origin.z = 0;
            // iterate 360 degrees about Y axis at step 
            for (float angleDegrees = 0.0f; angleDegrees <= 360; angleDegrees += step)
            {
                Vector3d dir = Utilities.MathHelper.VectorFrom2DHeading(angleDegrees);
                Ray r = new Ray(origin, dir);
                bool found = false;
                Triangle lastFoundTriangle = null;
                Vector3d foundIntersection = Vector3d.Zero();
                Vector3d intersection;

                for (int i = 0; i < triangles.Length; i++)
                {
                    // iterate through all triangles and find the closest triangle if any 
                    // and add the intersection point to list 
                    Triangle t = triangles[i];

                    if (Triangle.Intersects(r, RAY_SCALE, t.Points, false, out intersection))
                    {
                        // is this triangle closer than previously found triangle
                        if (found)
                        {
                            if (t.GetPlane().Distance < lastFoundTriangle.GetPlane().Distance)
                            {
                                lastFoundTriangle = t;
                                foundIntersection = intersection;
                            }
                        }
                        else
                        {
                            lastFoundTriangle = t;
                            foundIntersection = intersection;
                        }
                        found = true;
                    }
                }
                if (found)
                {
                    points.Add(foundIntersection);
                }
            }

            if (points.Count == 0 || points.Count < 3) throw new Exception("Degenerate polygon.");
            Polygon poly = new Polygon(points.ToArray());


            return poly;
        }

       
        // TODO: PointSprite mesh should be a seperate kind of mesh
        // where we can track every individual item and... I think we do use this for
        // our galaxy star rendering and we refer to it as... hrm..  an star atlas?
        // And we do implement picking on it... just havent extended it to work with
        // our overall galaxy stars 
        // TODO: with a seperate type of Mesh3d called a PointSpriteMesh, we can implement
        // collision/picking to pick individual items using simple ray sphere collissions for each
        // pointsprite.  Also, adding labels might be easier since we'll retain pointsprite
        // position data in the dedicated mesh

        /// <summary>
        /// Create a mesh where each vertex is drawn as a single pixel or as a point sprite.
        /// </summary>
        /// <param name="pointSprite">If true, a textured quad is rendered for each vertex. If false, a single untextured point.</param>
        /// <param name="spriteSize"></param>
        /// <param name="points">There is a minimum of points I know at least >1 for the mesh to be created properly and not generate error in TV3D debug log.</param>
        /// <param name="vertexColors">Array length must match points.Length or must be null</param>
        /// <remarks>Color parameter in TVMesh.AddVertex() call requires Custom Mesh Format that includes
        /// TV_MESHFORMAT_DIFFUSE.  Otherwise, must use TVMinimesh if you need seperate colors
        /// for individual pointsprites.</remarks>
        /// <returns></returns>
        public static Mesh3d CreatePointSprite(string relativePath, Vector3d[] points, int[] vertexColors, bool pointSprite = false, float spriteSize = 1.0f)
        {
            // NOTE: points can be null because we can create empty pointsprite mesh
            if (vertexColors != null && vertexColors.Length != points.Length) throw new ArgumentException("Mesh3d.CreatePointSprite() - invalid argument.");

            int length = 0;
            if (points != null)
            {
                length = points.Length;
            	if (length == 0 || length <= 2)
            	{
            	    CoreClient._CoreClient.Engine.AddToLog("Mesh3d.CreatePointSprite() - Too few points to create PointSprite Mesh" );                
            	}
            	else if (length > ushort.MaxValue)
            	{
            		CoreClient._CoreClient.Engine.AddToLog("Mesh3d.CreatePointSprite() - Too many points to create PointSprite Mesh" );    
            		throw new ArgumentOutOfRangeException ();
            	}
            }
            

            // TODO: not sychronized properly by going through Repository.Create() but even so
            // pointsprites should require us supply an ID because we tend to never share them
            Mesh3d mesh = (Mesh3d)Repository.Get(relativePath);
            if (mesh != null) return mesh;


            TVMesh tvm = CoreClient._CoreClient.Scene.CreateMeshBuilder(relativePath);
            tvm.SetPrimitiveType(CONST_TV_PRIMITIVETYPE.TV_POINTLIST);

            // custom vertex format required for per vertex color using .AddVertex(iColor) parameter
            // NOTE: even withou UV in format, billboard pointsprites work fine 
            // HOWEVER, we seem to be losing the rendering of some of the smaller scaled sprites? So we'll leave SetMeshFormat() commented out for now
//            bool result = tvm.SetMeshFormat ((int)(CONST_TV_MESHFORMAT.TV_MESHFORMAT_DIFFUSE | CONST_TV_MESHFORMAT.TV_MESHFORMAT_NOLIGHTING));
//            if (result == false) throw new Exception();

            // NOTE: We use AddVertex. I'm not sure if SetGeometry/Ex will even work since we must 
            //       supply an index to SetGeometry() and I DO KNOW that D3D9 does NOT support
            //       POINTLIST type with DrawIndexedPrimitives(). 
            //       "Member of the D3DPRIMITIVETYPE enumerated type, describing the type of
            //       primitive to render. D3DPT_POINTLIST is not supported with this method. See Remarks. "

            // fill the mesh with our array of vertices
            int color = 0;

            for (int i = 0; i < points.Length; i++)
            {
                if (vertexColors != null)
                    color = vertexColors[i];
                tvm.AddVertex((float)points[i].x, (float)points[i].y, (float)points[i].z, 0, 0, 0, 0, 0, 0, 0, color);
            }


            // NOTE: point paramters and blending must be done AFTER the mesh geometry is fully created.
            // i.e. after SetGeometry and the last AddVertex()
            if (pointSprite)
            {
                // TODO: alpha test should be set by Appearance node
                tvm.SetAlphaTest(true, 128, false);
            }

            // TODO: can SetPointParameters be called at runtime to modify spriteSize dynamically?
            // normally you would always want scaling, but perhaps in some cases with far far away things you want to never seem to get closer or further, you'd want to not use scaling
             tvm.SetPointParameters(pointSprite, spriteSize != 1.0f, spriteSize); 

            // why does it seem to work for our star field but not motion field
            // TODO: for some reason our initial bbox calculation is wrong.  Could this be because all pointsprites start at 0,0,0 when we load?
            //       and just before we can calc the volume?
            tvm.Render (); 
            tvm.EnableFrustumCulling(false);
            mesh = new Mesh3d(relativePath, tvm);
            
            mesh.mPointSprite = pointSprite ; // whether it's using textured pointsprites or just points
            mesh.mPointSpriteScale = spriteSize;
            
            // TODO: these flags shouldn't be set here... the node isnt even attached so they wont propogate.  It will compute its own bbox though
            mesh.SetChangeFlags (Enums.ChangeStates.GeometryAdded | Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
            return mesh;
        }

        #region IPageableNode Members
        public override void UnloadTVResource()
        {
        	DisposeManagedResources();
        }
        	
        public override void LoadTVResource()
        {
            // TODO: if the mesh's src file has changed such that it's same name, but different geometry
            // then how do we detect that so we can unload the previous mesh and load in the new mesh?
            if (_tvmesh != null)
            {
                try
                {
                    _tvmesh.Destroy();
                }
                catch
                {
                    Debug.WriteLine("Mesh3d.LoadTVResource() - Error on Mesh.Destroy() - mesh path == " + _id);
                }
            }
            try
            {
                // TODO: if _resourcePath == "" and _primitive = box, sphere, teapot, etc, we can load via tv's built in primitives
                // TODO: I think the above todo is wrong.  When we create a primitive during edit, we want to export that primitive
                //         to the mod archive and then use that path just as any other mesh.  Further, the primitive can be re-used easily.
                DefaultAppearance dummy;
                
                _tvmesh = CoreClient._CoreClient.Scene.CreateMeshBuilder(_id);
                //Debug.WriteLine ("Mesh3d.LoadTVResource() - CreateMeshBuilder - '" + _id + "'");
                // NOTE: meshformat _must_ always be set after CreateMeshBuilder()
				// but BEFORE adding verts or allowing mesh/actor to be restored from saved file
                MeshFormat = _meshFormat;
                				
                _tvmesh = LoadTVMesh(_tvmesh, _id, false, false, out dummy, out mLineList); // false false because we are loading from xml including the appearance info

                _tvmesh.EnableFrustumCulling (false);
                                
                // NOTE: LightingMode set _after_ LoadTVMesh().  
                LightingMode = _lightingMode;
                
                // assign values to Properties so they get assigned to _tvMesh                
				PointSpriteSize = mPointSpriteScale;
                TextureClamping = mTextureClamping;

                // TODO: .ComputeNormals() must be called if you want binormal and tangents to be
                //       computed and your vertex format must support those so they can be
                //       passed in semantic to shader
                // TODO: however in general, we might not want to recompute normals and lose the existing
                //       normals of the model!  This could result in  smoothing group style normals being lost
                //       and replaced with faceted normals.
// Hypnotron - August.23.2020 - dont compute normals if it messses up the ones we load from our .obj models                ComputeNormals();
                CullMode = _cullMode;
                  //_tvmesh.ComputeNormalsEx(fixSeems, threshold); 
				//_tvmesh.ComputeTangents(); // in future build i havent upgraded yet?
                // TODO: use this.CullMode, this.InvertNormals or this.ComputeNormals should all
            	// be set as flags that LoadTVResource() will apply.  Search for all instances in Mesh3d where
            	// we try to tvmesh.SetCullmode or tvmesh.InvertNormals or tvmesh.ComputeNormals directly and 
            	// remove them and switch to flags
            
                //_tvmesh.GenerateUV(); 
                _tvfactoryIndex = _tvmesh.GetIndex();
                //Debug.WriteLine("Mesh3d.LoadTVResource() - SUCCESS '" + _id +"'");
                SetChangeFlags(Keystone.Enums.ChangeStates.GeometryAdded | 
                    		   Keystone.Enums.ChangeStates.AppearanceParameterChanged |
                               Keystone.Enums.ChangeStates.BoundingBoxDirty | 
                               Keystone.Enums.ChangeStates.RegionMatrixDirty |
                               Keystone.Enums.ChangeStates.MatrixDirty, Keystone.Enums.ChangeSource.Self);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Mesh3d.LoadTVResource() - ERROR: " + _id + " " + ex.Message);
                throw ex;
            }
            // TODO: i think convexHull needs to be apart of a Model, not Mesh3d.  Right?  Model.Geometry,  Model.Hull
            // TODO: how do we load the convexhull if this LoadTVResource() doesnt get called from the server? because
            // either mesh3d itself isnt loaded or because we skip the method call on the server
            // and if mesh3d is not even laoded, then this ConvexHull property doesnt exist so where does it sit?
            // TODO: also i need to go back and look at how jiglibx handles convex hulls and whether they do in fact 
            // re-compute world hull every itteration..  In fact
            // our jiglibx PhysicsBody property could be something we use instead of convexhull property here..?
            // UPDATE: jiglibx uses Primitive object which
            //  and inside of CollisionSkin object it maintains lists of local space , world space and the previous frame's world space collision skins
            // it uses lists because you can have multiple collision skins to make up a single object such as three spheres to represent a football

            //ConvexHull hull = ConvexHull.GetStanHull (this._tvmesh);
           
        }

        // TODO: why is a resourcePath passed in?  currently we do use it to save to a temp file on disk
        // which we can then import into archive and overwrite existing or add as a variation copy with
        // different name
        // TODO: however should we support a path that is resourcedescriptor to archive and directly
        // save to the archive from here?
        // TODO: will SaveTVM work to a memory stream? similar to the mem data path when loading tvm?
        public override void SaveTVResource(string resourcePath)
        {
            // _relativeResourcePath = resourcePath;
            if (_resourceStatus == PageableNodeStatus.Loaded && TVResourceIsLoaded)
                // TODO: need to create a full path from the relative path
                // string fullPath = Path.Combine (Core._Core.Mod_Path, _relativeResourcePath);
                _tvmesh.SaveTVM(resourcePath); // this is why name i think should not be equal to filepath right?
        }
        #endregion

        private static TVMesh LoadTVMesh(TVMesh tvm, string relativeResourcePath, bool loadTextures, bool loadMaterials, out DefaultAppearance appearance, out bool lineList)
        {
            System.Diagnostics.Debug.Assert(tvm != null, "Mesh3d.LoadTVMesh() - tvm must be initialized via CreateMeshBuilder!");

            if (string.IsNullOrEmpty(relativeResourcePath)) throw new ArgumentNullException();
            appearance = null;
            lineList = false;
            try
            {
#if DEBUG
                Stopwatch watch = new Stopwatch();
                watch.Reset();
                watch.Start();
#endif

                
                //            System.Diagnostics.Debug.WriteLine("Mesh3d.LoadTVMesh() - Attempting load '" + relativeResourcePath + "'");
                if (IsPrimitive(relativeResourcePath))
                {
                    string[] split = relativeResourcePath.Split(PRIMITIVE_DELIMITER);
                    if (split != null && split.Length > 0)
                    {
                        // NOTE: By reaching this method we that the primitive does not already exist in Repository cache
                        // parse the primitive type and attributes and build it
                        switch (split[0])
                        {
                            case PRIMITIVE_3D_LINE:
                                lineList = true;
                                throw new NotImplementedException();
                                break;
                            case PRIMITIVE_CIRCLE:
                                lineList = true;
                                throw new NotImplementedException();
                                //tvm = CreateCircle();
                            
                                break;
                            case PRIMITIVE_ELLIPSE:
                                Vector3d[] points = Keystone.Primitives.Ellipse.CreateEllipse(int.Parse(split[1]), float.Parse(split[2]), int.Parse(split[3]));
                                // TODO: i should be passing in empty TVM so that any mesh format we've set will not get lost
                                tvm = CreateLineListTVM(relativeResourcePath, points, false, false, true);
                                lineList = true;
                                break;
                            case PRIMITIVE_BOX:
                                // TODO: i should be passing in empty TVM so that any mesh format we've set will not get lost
                                tvm = CreateBoxTVM(relativeResourcePath, float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
                                break;
                            case PRIMITIVE_CYLINDER:

                                tvm = CreateCylinderTVM(relativeResourcePath, float.Parse(split[1]), float.Parse(split[2]), int.Parse(split[3]));
                                break;
                            case PRIMITIVE_TEAPOT:
                                // TODO: i should be passing in empty TVM so that any mesh format we've set will not get lost
                                tvm = CreateTeapotTVM(relativeResourcePath);
                                break;
                            case PRIMITIVE_FLOOR:
                                // TODO: i should be passing in empty TVM so that any mesh format we've set will not get lost
                                tvm = CreateFloorTVM(relativeResourcePath, float.Parse(split[1]), float.Parse(split[2]), uint.Parse(split[3]), uint.Parse(split[4]), float.Parse(split[5]), float.Parse(split[6]));
                                break;
                            case PRIMITIVE_CELL_GRID:
                                // TODO: i should be passing in empty TVM so that any mesh format we've set will not get lost
                                tvm = CreateCellGridTVM(relativeResourcePath, float.Parse(split[1]), float.Parse(split[2]), uint.Parse(split[3]), uint.Parse(split[4]), float.Parse(split[5]), bool.Parse(split[6]));
                                // normals are already set for CellGridTVM
                                break;
                            case PRIMITIVE_SPHERE:
                                // TODO: i should be passing in empty TVM so that any mesh format we've set will not get lost
                                tvm = CreateSphereTVM(relativeResourcePath, float.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]), (CONST_TV_MESHFORMAT)int.Parse(split[4]));
                                break;
                            case PRIMITIVE_DISK:
                                tvm = CreateDiskTVM(relativeResourcePath, float.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]));
                                break;
                            case PRIMITIVE_CONE:
                            case PRIMITIVE_UV_TRISTRIP_SPHERE:
                                tvm = CreateUVTriangleStripSphere(10, 10, 10, 10000)._tvmesh; //todo: this should return a TVM and not a Mesh3d
                                //throw new NotImplementedException();
                                break;
                            case PRIMITIVE_WIREFRAME_LINELIST_SPHERE:
                            case PRIMITIVE_LATLONG_LINELIST_SPHERE:
                                lineList = true;
                                throw new NotImplementedException();
                            default:
                                System.Diagnostics.Debug.WriteLine("Mesh3d.LoadTVMesh() - primitive not found.");
                                throw new Exception();
                                break;
                        }
                    }
                    else
                        throw new Exception("Mesh3d.LoadTVMesh() - ERROR: Unknown primitive type '" + split[0].ToString() + "'");
                }
                else
                {
                    KeyCommon.IO.ResourceDescriptor descriptor = new KeyCommon.IO.ResourceDescriptor(relativeResourcePath);

                    bool result = false;

                    //// !!!!
                    //// NOTE: MeshFormat MUST be done PRIOR to loading in the geometry!!!
                    //TV_VERTEXELEMENT[] elements = new TV_VERTEXELEMENT[6];
                    //elements[0].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT4;
                    //elements[1].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;
                    //elements[2].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;
                    //elements[3].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT3;
                    //elements[4].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT2;
                    //elements[5].element = (int)CONST_TV_ELEMENTTYPE.TV_ELEMENT_FLOAT2;

                    //elements[0].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_POSITION;
                    //elements[1].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TANGENT;
                    //elements[2].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_BINORMAL;
                    //elements[3].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_NORMAL;
                    //elements[4].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD0;
                    //elements[5].usage = (int)CONST_TV_ELEMENTUSAGE.TV_ELEMENTUSAGE_TEXCOORD3;

                    //elements[0].stream = 0;
                    //elements[1].stream = 0;
                    //elements[2].stream = 0;
                    //elements[3].stream = 0;
                    //elements[4].stream = 0;
                    //elements[5].stream = 0;

                    //m.SetMeshFormatEx(elements, elements.Length);


                    // assuming i just pass a progress callback here to the calling app fine, but what about the 
                    // tv load call?  Here our only option is for the caller to guestimate the total time in advance
                    // then itterate until the Import command either fails or completes and update the progress.
                    // So what we need is  a good mechanism for our asychronous worker threads to provide a periodic
                    // progress update call.  Probably none.  We could have our main app loop query the threadpool
                    // for workitems that are attached to some progress meter and update any dialogs that way.

                    if (descriptor.IsArchivedResource)
                    {
                        if (descriptor.Extension == ImportLib.XFILE_EXTENSION || descriptor.Extension == ImportLib.TVMESH_EXTENSION)
                        {
                            System.Runtime.InteropServices.GCHandle gchandle;
                            string memoryFile = Keystone.ImportLib.GetTVDataSource(descriptor.EntryName, "", Keystone.Core.FullNodePath(descriptor.ModName), out gchandle);
                            if (string.IsNullOrEmpty(memoryFile)) throw new Exception("Mesh3d.LoadTVMesh() - ERROR: '" + descriptor.ToString() + "' failed to import from archive.");

                            if (descriptor.Extension == ImportLib.XFILE_EXTENSION)
                                result = tvm.LoadXFile(memoryFile, loadTextures, loadMaterials);
                            else if (descriptor.Extension == ImportLib.TVMESH_EXTENSION)
                            {
                                result = tvm.LoadTVM(memoryFile, loadTextures, loadMaterials);
                            }
                            else throw new Exception("Mesh3d.LoadTVMesh() - ERROR: unexpected extension '" + descriptor.Extension + "'.");

                            gchandle.Free();
                            if ((loadMaterials) || (loadTextures))
                                ImportLib.GetMeshTexturesAndMaterials(tvm, out appearance);
                        }
                        else if (descriptor.Extension == ImportLib.WAVEFRONTOBJ_EXTENSION)
                        {
                            string[] dummy;
                            bool reverseWinding = true;
                            Loaders.WaveFrontObj obj = Loaders.WavefrontObjLoader.ParseWaveFrontObj(descriptor.ModName, descriptor.EntryName, reverseWinding, true, out dummy, out dummy);
                            Loaders.WavefrontObjLoader.ObjToTVM(obj, relativeResourcePath, loadTextures, loadMaterials, ref tvm, out appearance);

                            if (tvm != null) result = true;
                        }
                    }
                    else
                    {
                        if (descriptor.Extension == ImportLib.XFILE_EXTENSION || descriptor.Extension == ImportLib.TVMESH_EXTENSION)
                        {
                            if (descriptor.Extension == ImportLib.XFILE_EXTENSION)
                                result = tvm.LoadXFile(Core.FullNodePath(descriptor.EntryName), loadTextures, loadMaterials);
                            else if (descriptor.Extension == ImportLib.TVMESH_EXTENSION)
                            {
                                result = tvm.LoadTVM(Core.FullNodePath(descriptor.EntryName), loadTextures, loadMaterials);
                            }
                            else throw new Exception("Mesh3d.LoadTVMesh() - ERROR: unexpected extension '" + descriptor.Extension + "'.");

                            if ((loadMaterials) || (loadTextures))
                                ImportLib.GetMeshTexturesAndMaterials(tvm, out appearance);
                        }
                        else if (descriptor.Extension == ImportLib.WAVEFRONTOBJ_EXTENSION)
                        {
                            string[] dummy2;
                            Loaders.WaveFrontObj obj = Loaders.WavefrontObjLoader.ParseWaveFrontObj(Core.FullNodePath(descriptor.EntryName), true, true, out dummy2, out dummy2);
                            Loaders.WavefrontObjLoader.ObjToTVM(obj, relativeResourcePath, loadTextures, loadMaterials, ref tvm, out appearance);

                            if (tvm != null) result = true;
                        }
                    }

                    if (!result)
                        // note: if the geometry was loaded via tv's built in createbox or some such
                        // then currently that's unsupported and this exceptionw ill be thrown. 
                        // we need to have a valid resource path.
                        throw new Exception(string.Format("Mesh3d.LoadTVMesh() - ERROR: '{0}' failed to load.  Check path.", relativeResourcePath));
                }


            
#if DEBUG
            watch.Stop();
            //            Trace.WriteLine("Mesh3d.LoadTVMesh() SUCCESS '" + relativeResourcePath + "' loaded with " + tvm.GetGroupCount() + " groups." + watch.Elapsed + "seconds, with = " + tvm.GetVertexCount() + " vertices in " + tvm.GetTriangleCount() + " triangles. ");

#endif
        }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Mesh3d.LoadTVMesh() - FAILED " + relativeResourcePath + " " + ex.Message);
            }

            try
            {
                //Debug.WriteLine("Mesh3d.LoadTVMesh() - Begin HACK RENDER ");
                // June.30.2022 - Commented tvm.Render() out because it appears to be the cause of Crash To Desktop during scene loading.
                //tvm.Render(); // Dec.11.2013 - hack workaround for TV3D bug because until first Render() called, some things like tvm.AdvancedCollide() will not report successful collisions
                //Debug.WriteLine("Mesh3d.LoadTVMesh() - End HACK RENDER ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Mesh3d.LoadTVMesh() - EXCEPTION " + ex.Message) ;
            }
            // TODO: if the Mesh already exists, then perhaps the Materials and Textures do too?  Yet 
            // i dont believe it's possible (or desirable?) to try and share these underlying material nodes?
            // perhaps what is required is that when import a mesh, i alter the default materials name to be "mesh.ID + materialName")
            // or perhaps on the import dialog, i first notify user that a mesh of that path already exists in the scene or
            // in the prefab directory they are attempting to import to and do they want to share\use the existing?
            // typically they'll want to.  Another reason for doing this by default tho is that if the mesh is not loaded in the current pages
            // then it may still already exist as a prefab, and we would want the underlying resource to be used... or maybe
            // at that pont we simply say "user, you should've just browsed prefabs first...)
            return tvm;
        }

        internal static bool IsPrimitive(string resourceID)
        {
            if (Path.GetExtension(resourceID).ToLower() == ".tvm" || Path.GetExtension(resourceID).ToLower() == ".x" || Path.GetExtension(resourceID).ToLower() == ".obj")
                return false;

        	if (resourceID.Contains(PRIMITIVE_QUAD) ||
        	    resourceID.Contains(PRIMITIVE_CUBE) ||
        		resourceID.Contains(PRIMITIVE_BILLBOARD) ||
        		resourceID.Contains(PRIMITIVE_ELLIPSE) ||
                resourceID.Contains(PRIMITIVE_CIRCLE) ||
                resourceID.Contains(PRIMITIVE_3D_LINE) ||
                //resourceID.Contains(PRIMITIVE_POINT_SPRITE) ||
                resourceID.Contains(PRIMITIVE_FLOOR) ||
                resourceID.Contains(PRIMITIVE_CELL_GRID) ||
                resourceID.Contains(PRIMITIVE_BOX) ||
                resourceID.Contains(PRIMITIVE_CYLINDER) ||
                resourceID.Contains(PRIMITIVE_TEAPOT) ||
                resourceID.Contains(PRIMITIVE_SPHERE) ||
                resourceID.Contains(PRIMITIVE_DISK ) ||
                resourceID.Contains(PRIMITIVE_UV_TRISTRIP_SPHERE) ||
                resourceID.Contains(PRIMITIVE_WIREFRAME_LINELIST_SPHERE) ||
				resourceID.Contains(PRIMITIVE_LATLONG_LINELIST_SPHERE) ||                
                resourceID.Contains(PRIMITIVE_CONE))
                return true;
            return false;
        }

        // SaveTVResource - done asychronously thru an IO plugin host api command 
        //              - _tvmesh.SaveTVM
        //_tvmesh.ResetMesh 
        //_tvmesh.WeldVertices

        public void ResetTransform(Matrix m)
        {
            int vertexCount = VertexCount;

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3d v = GetVertex(i);
                v = Vector3d.TransformCoord(v, m);
                SetVertex(i, v);
            }

            if (IsPrimitive(this.ID))
            {
                // this mesh needs to be saved with the path being the new ID
                // and the saved mesh resource needs to replace this resource

            }


        }

        // rotations occur about the mesh center which is always 0,0,0
        // however, sometimes the vertices may not be centered about 0,0,0 or
        // you may not want it to (as in a hinge joint mesh)
        public void ResetCenter(Vector3d position)
        {
            _tvmesh.SetMeshCenter((float)position.x, (float)position.y, (float)position.z);
        }

        public void ResetScale(Vector3d scale)
        {
            int vertexCount = VertexCount;

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3d v = GetVertex(i);
                v *= scale;
                SetVertex(i, v);
            }
        }

        public void ResetRotation(Vector3d yawPitchRoll)
        {
            int vertexCount = VertexCount;
            Vector3d rotation = new Vector3d (yawPitchRoll.y, yawPitchRoll.x, yawPitchRoll.z);

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3d v = GetVertex(i);
                v = rotation * v;
                SetVertex(i, v);
            }
        }


        /// <summary>
        /// Mesh format should be set prior to loading geometry.
        /// However, .tvm and .tva upon Mesh.Save() will save the
        /// current mesh format and reload it subsequently.
        /// </summary>
        public void SetMeshFormat(TV_VERTEXELEMENT[] elements)
        {
            if (elements == null || elements.Length == 0) return;
            _tvmesh.SetMeshFormatEx(elements, elements.Length);
        }

        /// <summary>
        /// Mesh format should be set prior to loading geometry.
        /// However, .tvm and .tva upon Mesh.Save() will save the
        /// current mesh format and reload it subsequently.
        /// </summary>
        public CONST_TV_MESHFORMAT MeshFormat
        {
            get { return _meshFormat; }
            set
            {
                _meshFormat = value;
                if (_tvmesh != null)
	                _tvmesh.SetMeshFormat((int)value);
            }
        }
        
        public bool IsPointList 
        {
        	get {return mLineList;}
        }
                
        public float PointSpriteSize 
        {
        	get 
        	{
        		return mPointSpriteScale;
        	}
        	set
        	{
        		mPointSpriteScale = value;
        		// NOTE: verified. .SetPointParameters() _can_ be called dynamically to modify scale.  Perhaps we cannot change from Points to Sprites dynamically
        		//       but maybe we can change the scales
        		if (mLineList)
	        		_tvmesh.SetPointParameters (mPointSprite, mPointSpriteScale != 1.0f, mPointSpriteScale);
        	}
        }

        // TODO: TextureClamping should be assigned and set in AppearanceGroup?
        bool mTextureClamping = false;
        public override bool TextureClamping
        {
            set
            {
                mTextureClamping = value;
                if (_tvmesh != null)
                    _tvmesh.SetTextureClamping(value);
            }
        }
        
        /// <summary>
        /// NOTE: SetLightingMode must be called after geometry is loaded because the lightingmode
        /// is stored in .tvm, .tva files and so anything you set prior to loading, will be
        /// replaced with the lightingmode stored in the file.
        /// Thus it's proper and only allowed that LightingMode is set by Appearance. 
        /// Thus keep this access modifier "internal"
        /// OR, we have to remove _lightingMode from Appearance and force this to be
        /// geometry level only.  
        /// </summary>
        public CONST_TV_LIGHTINGMODE LightingMode
        {
            get { return _lightingMode; }
            set
            {
                if (_tvmesh != null) 
                    _tvmesh.SetLightingMode(value);

                _lightingMode = value;
            }
        }

        public int GetMaterial(int groupID)
        {
            return _tvmesh.GetMaterial(groupID);
        }

        public void SetMaterial(int tvMaterialID, int groupID)
        {
        	if (_resourceStatus != PageableNodeStatus.Loaded) return;
        	
            if (groupID == -1)
                _tvmesh.SetMaterial(tvMaterialID);
            else
                _tvmesh.SetMaterial(tvMaterialID, groupID);
        }

        public int GetTextureEx(CONST_TV_LAYER layer, int groupID)
        {
            return _tvmesh.GetTextureEx((int) layer, groupID);
        }

        public void SetTexture(int tvTextureID, int groupID)
        {
        	if (_resourceStatus != PageableNodeStatus.Loaded) return;
        	
            if (groupID == -1)
                _tvmesh.SetTexture(tvTextureID);
            else
                _tvmesh.SetTexture(tvTextureID, groupID);
        }

        public void SetTextureEx(int layer, int tvTextureID, int groupID)
        {
        	if (_resourceStatus != PageableNodeStatus.Loaded) return;
        	
            if (groupID == -1)
                _tvmesh.SetTextureEx(layer, tvTextureID);
            else
                _tvmesh.SetTextureEx(layer, tvTextureID, groupID); 
        }

        /// <summary>
        /// Sets texture scaling and texture translation offsets
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="groupID"></param>
        /// <param name="translationU"></param>
        /// <param name="translationV"></param>
        /// <param name="scaleU"></param>
        /// <param name="scaleV"></param>
        public void SetTextureMod(int layer, int groupID, float translationU, float translationV, float scaleU, float scaleV)
        {
        	if (_tvmesh == null) return;  
            // TODO: when the translation/scale/rotation gets set back to 0,1,0 respectively
            // i dont ever disable the texture mod...
            _tvmesh.SetTextureModEnable(true, groupID, layer);
            _tvmesh.SetTextureModTranslationScale(translationU, translationV, scaleU, scaleV, groupID, layer);
        }

        public void SetTextureRotation(int layer, int groupID, float angleZ)
        {
        	if (_tvmesh == null) return;  
            // TODO: when the translation/scale/rotation gets set back to 0,1,0 respectively
            // i dont ever disable the texture mod...
            _tvmesh.SetTextureModEnable(true, groupID, layer);
            _tvmesh.SetTextureModRotation(angleZ, groupID, layer);
        }
        
        internal void SetGroupRenderOrder(bool enableRenderOrder, int[] order)
        {
        	if (_tvmesh == null) return;  
        	
            if (order != null)
                _tvmesh.SetGroupRenderOrder (enableRenderOrder, order.Length, order);
        }

        public void ComputeNormals()
        {
        	if (_tvmesh == null) return; 
        	if (mLineList || mPointSprite || mLineList) return;

            _tvmesh.ComputeNormals();
        }
        
        public void InvertNormals()
        {
        	if (_tvmesh == null) return;
        	if (mLineList || mPointSprite || mLineList) return;
        	_tvmesh.InvertNormals();
        }

        public void ReverseNormals()
        {
        	InvertNormals();
        }
        
        // used by Appearance
        // TODO: this needs to be changed to allow for call to
        // .SetShaderEx() which allows a groupIndex.. 
        // or maybe don't allow for 1.0.  Seperate shaders per group is a poor
        // way to do things.  If you want for instance a clear group, best to just
        // make it a seperate mesh and attach it as a child in the scene. (eg say
        // for window on a car or glowing animated cylon eyes on a robot)
        internal override Shader Shader
        {
            set
            {
                if (_shader == value && ( value == null || value.PageStatus == PageableNodeStatus.Loaded)) return;

                if (value != null)
                {
                    // also we don't want to assign this shader if it's underlying tvshader is not yet paged in.
                    if (value.PageStatus != PageableNodeStatus.Loaded)
                    {
                        //System.Diagnostics.Debug.WriteLine("Mesh3d.Shader '" + value.ID + "' not loaded.  Cannot assign to Mesh '" + _id + "'");
                        return;
                    }
                }
        
                // NOTE: In our ImportLib and WavefrontObjLoader, if there is only 1 group in the model
                // we add materials, shaders, and textures directly to the defaultappearance and add
                // NO GroupAttribute children.
                _shader = value;
                if (_shader == null)
                    _tvmesh.SetShader(null);
                else
                    _tvmesh.SetShader(_shader.TVShader);
            }
        }

                
        /// <summary>
        /// The alpha test is a last chance to reject a pixel from being written to the screen.
        /// After the final output color has been calculated, the color can optionally have its alpha value 
        /// compared to a fixed value. If the test fails, the pixel is not written to the display.
        /// 
        /// When rendering plants and trees, many games have the hard edges typical of alpha testing. 
        /// A way around that is to render the object twice. In the first pass, we use alpha testing to 
        /// only render pixels that are more than 50% opaque. In the second pass, we alpha-blend the graphic 
        /// in the parts that were cut away, without recording the depth of the pixel. We might get a bit of 
        /// confusion as further away branches overwrite the nearby ones, but in practice, that is hard to 
        /// see as leaves have a lot of visual detail in them.
        /// </summary>
        /// <param name="enable"></param>
        /// <param name="iGroup"></param>
        internal override void SetAlphaTest (bool enable, int iGroup)
        {
            _alphaTestEnable = enable;
            // caution: DepthBufferWrite  can be enabled or disabled
            // even if the param _alphaTestEnable is set to false.   
            // but normally, you are only disabling it so that unsorted
            // alphatested groups in the mesh just render ontop of each other
            // with minimal artifacts.
            if (_tvmesh != null)
            {
    	        // TODO: alpha test can be set different PER GROUP, but for now
        		// let's not do that unless we have to.  it saves space in xml
        		// to not have every group store that information
	            _tvmesh.SetAlphaTest(_alphaTestEnable, _alphaTestRefValue, _alphaTestDepthBufferWriteEnable, iGroup);
    	        if (_alphaTestEnable)
    	        	_tvmesh.SetCullMode (CONST_TV_CULLING.TV_DOUBLESIDED); // HACK until we add this option 
            }
        }

        internal int AlphaTestRefValue
        {
            get { return _alphaTestRefValue; }
            set
            {
                _alphaTestRefValue = value;
            }
        }

        internal bool AlphaTestDepthWriteEnable
        {
            get { return _alphaTestDepthBufferWriteEnable; }
            set
            {
                _alphaTestDepthBufferWriteEnable = value;
            }
        }

        /// <summary>
        /// Note: CONST_TV_BLENDINGMODE.TV_BLEND_ALPHA is required when using alpha 
        /// component in the diffuse of material to enable Alpha Transparency.
        /// Note: When alpha blending is enabled in the hardware, some optimizations 
        /// such as zbuffer pixel rejections in the pixel shader are disabled.
        /// </summary>
        internal override CONST_TV_BLENDINGMODE BlendingMode
        {
            set
            {
                _tvmesh.SetBlendingMode(value);
            }
        }

        internal override void SetBlendingMode(CONST_TV_BLENDINGMODE blendingMode, int group)
        {
            if (group <= -1)
                _tvmesh.SetBlendingMode(blendingMode);
            else
                _tvmesh.SetBlendingMode(blendingMode, group);
        }

        #region Geometry Member
        /// <summary>
        /// 0 = backface, 1 = doublesided, 2 = frontface
        /// </summary>
        public override int CullMode // TODO: cullMode like  TextureClamping should be set from Appearance
        {
            set
            {
                _cullMode = value;
                if (_tvmesh != null)
                    _tvmesh.SetCullMode((CONST_TV_CULLING)_cullMode);
            }
        }

        public override int GetVertexCount(uint groupIndex)
        {
            return _tvmesh.GetVertexCountEx((int)groupIndex);
        }

        public override int VertexCount
        {
            get { return _tvmesh.GetVertexCount(); }
        }

        public override int GroupCount
        {
            get { return _tvmesh.GetGroupCount(); }
        }

        public override int TriangleCount
        {
            get { return _tvmesh.GetTriangleCount(); }
        }

        /// <summary>
        /// Very expensive call.  
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int[] GetTriangleIndices(int index)
        {
            if (_tvmesh == null || TVResourceIsLoaded == false) return null;
            int[] results = new int[3];
            int group = 0;
            // .GetTriangleInfo is a VERY expensive call in tv3d
            _tvmesh.GetTriangleInfo(index, ref results[0], ref results[1], ref results[2], ref group);
            return results;
        }

        public Triangle[] GetTriangles()
        {
            int count = _tvmesh.GetTriangleCount();
            Triangle[] results = new Triangle[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = GetTriangle(i);
            }
            return results;
        }

        public Triangle GetTriangle(int index)
        {
            int index1 = -1;
            int index2 = -1;
            int index3 = -1;
            int group = -1;

            _tvmesh.GetTriangleInfo(index, ref index1, ref index2, ref index3, ref group);

            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float normalx = 0.0f;
            float normaly = 0.0f;
            float normalz = 0.0f;
            float dummy = 0.0f; // for uv
            float u2 = -1.0f;
            float vv2 = -1.0f;
            float u = 0.0f;
            float v = 0.0f;
            int color = 0;

            _tvmesh.GetVertex(index1, ref x, ref y, ref z, ref normalx, ref normaly, ref normalz, ref u, ref v, ref u2, ref vv2, ref color);
            Vector3d vec1;
            vec1.x = x;
            vec1.y = y;
            vec1.z = z;

            _tvmesh.GetVertex(index2, ref x, ref y, ref z, ref normalx, ref normaly, ref normalz, ref u, ref v, ref u2, ref vv2, ref color);
            Vector3d vec2;
            vec2.x = x;
            vec2.y = y;
            vec2.z = z;

            _tvmesh.GetVertex(index3, ref x, ref y, ref z, ref normalx, ref normaly, ref normalz, ref u, ref v, ref u2, ref vv2, ref color);
            Vector3d vec3;
            vec3.x = x;
            vec3.y = y;
            vec3.z = z;

            return new Triangle(vec1, vec2, vec3);
        }

        public void SetVertexUV(int index, float u1, float v1, float u2, float v2)
        {
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float normalx = 0.0f;
            float normaly = 0.0f;
            float normalz = 0.0f;
            float tempu = -1f;
            float tempv = -1f;
            float tempu2 = -1.0f;
            float tempv2 = -1.0f;
            int color = 0;

            _tvmesh.GetVertex(index, ref x, ref y, ref z, ref normalx, ref normaly, ref normalz, ref tempu, ref tempv, ref tempu2, ref tempv2, ref color);
            _tvmesh.SetVertex(index, x, y, z, normalx, normaly, normalz, u1, v1, u2, v2, color);
        }

        /// <summary>
        /// Modifies U1 and V1 and keeps the existing U2 and V2
        /// </summary>
        /// <param name="index"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        public void SetVertexUV (int index, float u, float v)
        {
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float normalx = 0.0f;
            float normaly = 0.0f;
            float normalz = 0.0f;
            float tempu = -1f;
            float tempv = -1f;
            float tempu2 = -1.0f;
            float tempv2 = -1.0f;
            int color = 0;

            if (_tvmesh.GetVertexCount() == 0)
            {
                System.Diagnostics.Debug.WriteLine("Mesh3d.SetVertexUV() - ERROR: Mesh has no vertices.");
                throw new Exception();
            }
            _tvmesh.GetVertex(index, ref x, ref y, ref z, ref normalx, ref normaly, ref normalz, ref tempu, ref tempv, ref tempu2, ref tempv2, ref color);
            _tvmesh.SetVertex(index, x, y, z, normalx, normaly, normalz, u, v, tempu2, tempv2, color);
        }

        // NOTE: SetVertex does not update TVMesh bounding box so TV will cull this mesh
        // even if our own culling determined the mesh to be visible.  So tvmesh.EnableFrustumCulling(false)
        // or force a BoundingVolumeIsDirty = true
        public void SetVertexUVs(int[] index, float[] u, float[] v)
        {
            if (index == null || u == null || v == null) throw new ArgumentNullException ();
            if (index.Length != u.Length || u.Length != v.Length) throw new ArgumentOutOfRangeException("All three array paramters must have same length.");

            for (int i = 0; i < index.Length; i++)
            {
                SetVertexUV (index[i], u[i], v[i]);
            }
        }

        public Vector3d[] GetVertexPositions()
        {
            int count = _tvmesh.GetVertexCount();
            if (count == 0) return null;

            Vector3d[] results = new Vector3d[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = GetVertex(i);
            }
            return results;
        }

       public BoundingBox GetGroupBoundingBox(int groupID)
       {
         Vector3d[] verts = GetVertexPositions(groupID);
         
         return new BoundingBox(verts);
       }
       
       public Vector3d[] GetVertexPositions(int groupID)
        {
            int count = _tvmesh.GetVertexCount();
            int groupCount = GroupCount;

            TV_SVERTEX[] verts = new TV_SVERTEX[count];
            indices = new int[count];
            groups = new int[count];
            _tvmesh.GetGeometry(verts, indices, groups);
            
            int vertexCountInGroup = 0;
            for (int i = 0; i < verts.Length; i++)
            {
              if (groups[i] == groupID)
                   vertexCountInGroup++;
            }
            
            Vector3d[] groupVerts = new Vector3d[vertexCountInGroup];
            int index = 0;
            for (int i = 0; i < verts.Length; i++)
            {
              System.Diagnostics.Debug.Assert(groups[i] < groupCount);
              if (groups[i] == groupID)
                   groupVerts[index++] = verts[i];
            }

            return groupVerts;
        }


        public TV_SVERTEX[] GetVertices()
        {
            int count = _tvmesh.GetVertexCount();

            TV_SVERTEX[] verts = new TV_SVERTEX[count];
            indices = new int[count];
            groups = new int[count];
            _tvmesh.GetGeometry(verts, indices, groups);

            int stride = 0;
            float[] ret = new float[count * 13];
            _tvmesh.GetGeometryEx(ret, ref stride, indices, groups);

            int numFaces = 9998;
            SetVertices(ret, stride, 10000, numFaces);

            return verts;
        }

        int[] indices;
        int[] groups;

        public void GetVertices(out Vector3d[] positions, out Vector3d[] normals)
        {
            int count = _tvmesh.GetVertexCount();

            positions = new Vector3d[count];
            normals = new Vector3d[count];

            if (count == 0) return;
            
            float x, y, z, nx, ny, nz, tu1, tu2, tv1, tv2;
            x = y = z = nx = ny = nz = tu1 = tu2 = tv1 = tv2 = 0.0f;
            int color = 0;

            for (int i = 0; i < count; i++)
            {
                _tvmesh.GetVertex(i, ref x, ref y, ref z, ref nx, ref ny, ref nz, ref tu1, ref tv1, ref tu2, ref tv2, ref color);
                positions[i].x = x;
                positions[i].y = y;
                positions[i].z = z;

                normals[i].x = nx;
                normals[i].y = ny;
                normals[i].z = nz;

            }
        }

        public Vector3d GetVertex(int index)
        {
            float x, y, z, nx, ny, nz, tu1, tu2, tv1, tv2;
            x = y = z = nx = ny = nz = tu1 = tu2 = tv1 = tv2 = 0.0f;
            int color = 0;

            _tvmesh.GetVertex(index, ref x, ref y, ref z, ref nx, ref ny, ref nz, ref tu1, ref tv1, ref tu2, ref tv2, ref color);

            Vector3d result;
            result.x = x;
            result.y = y;
            result.z = z;

            return result;
        }

        public void SetVertex(int index, Vector3d position) 
        {
             SetVertex(index, (float)position.x, (float) position.y, (float)position.z);
        }

        public void SetVertex(int index, float x, float y, float z)
        {
            float dummy = 0;
            float normalx = 0;
            float normaly = 0;
            float normalz = 0;
            float u = 0;
            float v = 0;
            float u2 = 0;
            float v2 = 0;
            int color = Color.White.ToInt32();

            // NOTE: if we do not get existing values for normals, or colors, or uvs
            //       we risk screwing up the entire mesh because tvmesh.SetVertex() has no
            //       overloads that allows us to change just position for example without
            //       changing any of the other vertex parameters
            _tvmesh.GetVertex(index, ref dummy, ref dummy, ref dummy, 
                ref normalx, ref normaly, ref normalz, 
                ref u, ref v,
                ref u2, ref v2, ref color);

            _tvmesh.SetVertex(index, x, y, z, normalx, normaly, normalz, u, v, u2, v2, color);
            SetChangeFlags (Enums.ChangeStates.BoundingBoxDirty , Enums.ChangeSource.Self );
        }

        public void SetVertex (int index, float x, float y, float z, float normalx, float normaly, float normalz, float u, float v)
        {
            float dummy = 0.0f;
            float u2 = dummy;
            float v2 = dummy;
            int color = 0;

            // NOTE: if we do not get existing values for normals, or colors, or uvs
            //       we risk screwing up the entire mesh because tvmesh.SetVertex() has no
            //       overloads that allows us to change just position for example without
            //       changing any of the other vertex parameters
            _tvmesh.GetVertex (index, ref dummy, ref dummy, ref dummy, 
                ref dummy, ref dummy, ref dummy, 
                ref dummy, ref dummy, 
                ref u2, ref v2, 
                ref color);

            _tvmesh.SetVertex (index, x, y, z, normalx, normaly, normalz, u, v, u2, v2, color);
            SetChangeFlags (Enums.ChangeStates.BoundingBoxDirty , Enums.ChangeSource.Self );
        }
        public void SetVertex (int index, float x, float y, float z, float normalx, float normaly, float normalz, float u, float v, float u2, float v2, int color)
        {
            _tvmesh.SetVertex (index, x, y, z, normalx, normaly, normalz, u, v, u2, v2, color);
        }

        public void SetVertices(float[] vertices, int stride, int numVertices, int numFaces)
        {
            _tvmesh.SetGeometryEx(vertices, stride, numVertices, indices, numFaces);
           // _tvmesh.SetGeometry(vertices, vertices.Length)
           
            //_tvmesh.WeldVertices()
        }

        public void SetVertices(Vector3d[] position, Vector3d[] normal, Vector2f[] uv)
        {
            float dummy = 0;
            float existingNormalX = 0;
            float existingNormalY = 0;
            float existingNormalZ = 0;
            float existingU1 = 0;
            float existingV1 = 0;

            float positionX = 0;
            float positionY = 0;
            float positionZ = 0;
            float normalX = 0;
            float normalY = 0;
            float normalZ = 0;
            float u1 = 0;
            float v1 = 0;
            float u2 = 1;
            float v2 = 1;
            int color = 0;

            for (int i = 0; i < position.Length; i++)
            {
                // NOTE: if we do not get existing values for normals, or colors, or uvs
                //       we risk screwing up the entire mesh because tvmesh.SetVertex() has no
                //       overloads that allows us to change just position for example without
                //       changing any of the other vertex parameters
                _tvmesh.GetVertex(i, ref dummy, ref dummy, ref dummy,
                            ref existingNormalX, ref existingNormalY, ref existingNormalZ,
                            ref existingU1, ref existingV1,
                            ref u2, ref v2,
                            ref color);
                
                positionX = (float)position[i].x;
                positionY = (float)position[i].y;
                positionZ = (float)position[i].z;
                if (normal != null)
                {
                    normalX = (float)normal[i].x;
                    normalY = (float)normal[i].y;
                    normalZ = (float)normal[i].z;
                }
                else
                {
                    normalX = existingNormalX;
                    normalY = existingNormalY;
                    normalZ = existingNormalZ;
                }

                if (uv != null)
                {
                    u1 = uv[i].x;
                    v1 = uv[i].y;
                }
                else
                {
                    u1 = existingU1;
                    v1 = existingV1;
                }


                _tvmesh.SetVertex(i, positionX, positionY, positionZ,
                    normalX, normalY, normalZ, u1, v1, u2, v2, color);

            }
            SetChangeFlags (Enums.ChangeStates.BoundingBoxDirty , Enums.ChangeSource.Self );
        }


        // The start/end should alredy be in ModelSpace if the Picker traverser has done it's job.  
        // However for billboards it wont have taken into account the last minute rotate to face camera
        // transform (see Billboard.cs AdvancedCollide())
        internal override PickResults AdvancedCollide(Vector3d start, Vector3d end, KeyCommon.Traversal.PickParameters parameters)
        {
            TV_3DMATRIX identity = Helpers.TVTypeConverter.CreateTVIdentityMatrix();
            // note: picking is done in model space so we use Identity always.  
            _tvmesh.SetMatrix(identity);
            
            PickResults pr = new PickResults();
			Ray ray = new Ray (start,  end - start);

            if (mLineList)
            {
                const double pickHalo = 180.0d;	// minimum range to consider a point
                int foundIndex = -1; double foundIntersectionDistance = double.MaxValue;
                BoundingSphere sphere = new BoundingSphere (Vector3d.Zero(), pickHalo);
                
                int vertextCount = _tvmesh.GetVertexCount();
                
                // TODO: the entity itself that holds these pointsprites if each pointsprite
                // represents a seperate start IS a digest.  So this pointsprite implementation ends up
                // being just another way to pick them.
                
                for (int i = 0; i < vertextCount ; i++)
                {
                    Vector3d point = GetVertex(i);
                    
                    _sphere.Center = point; 
                    
                    double distanceFrontIntersection = 0;
					double distanceBackSideIntersection = 0;
                    if (sphere.Intersects (ray, ref distanceFrontIntersection))
                    {
                    	
                    	// TODO: ray sphere intersection seems broken
                    	// TODO: however we could also have a problem with our start and end vectors not being in model space which means they should be 
                    	// about origin because our starfield is moving with the camera
	                    if (distanceFrontIntersection < foundIntersectionDistance)
	                    {
	                        foundIntersectionDistance = distanceFrontIntersection;
	                        foundIndex = i;
	                        pr.HasCollided = true;
	                        // TODO: we can't early exit because another vertex might be closer 
	                    }
                    }   
                }
   
                
                if (pr.HasCollided)
                {
                	pr.FaceID = foundIndex;  // for pointsprites, faceID and vertexID are the same thing
                    pr.VertexID = foundIndex;
                    pr.VertexCoord = GetVertex (foundIndex); // for pointsprites, impactPoint and vertexcoord are same thing
                    pr.ImpactPointLocalSpace =  pr.VertexCoord ;  
                    pr.ImpactNormal = -ray.Direction; // Helpers.TVTypeConverter.FromTVVector(tvResult.vCollisionNormal());
                    pr.CollidedObjectType = PickAccuracy.Geometry; // TODO: can we say more specifically POINTSPRITE? so traveser can handle this result differently if necessary? perhaps associating it with a Digest?
                    
                    System.Diagnostics.Debug.WriteLine ("Mesh3d.AdvancedCollided() - PointSprite index found = " + foundIndex);
                }
            }
            else
            {
   	            TV_COLLISIONRESULT tvResult = Helpers.TVTypeConverter.CreateTVCollisionResult();
                // Apr.18.2017 - using Identity matrix on Billboard style mesh surely makes for tiny cross section to pick at certain camera angles?  
                //               Shouldn't we override AdvancedCollide() in Billboard.cs?
            	// Dec.11.2013 - after a tvmesh is loaded, the first tvmesh.Render() must be called before AdvancedCollision will work.  This is a known TV3D bug.
            	_tvmesh.AdvancedCollision(Helpers.TVTypeConverter.ToTVVector(start), 
            	                          Helpers.TVTypeConverter.ToTVVector(end), 
            	                          ref tvResult, 
            	                          PickResults.ToTVTestType(parameters.Accuracy));
                
                // NOTE: if mesh material.Opacit = 0.0f, AdvancedCollision() will always fail even though mesh.IsVisible() will still return true.
                //       Work around is to just set opacity to some very small value so that its effectively invisible
                pr.HasCollided = tvResult.bHasCollided;
                if (pr.HasCollided)
                {
	                #if DEBUG
                	if (this.ID == @"common.zip|actors/infantry_bot_mk4.obj")
                	{
                		System.Diagnostics.Debug.WriteLine (System.DateTime.Now.ToString() + " colliding successful");
                	}
                	#endif
                    // Debug.WriteLine("Face index = " + tvResult.iFaceindex.ToString());
                    // Debug.WriteLine("Distance = " + tvResult.fDistance.ToString());
                    // Debug.WriteLine("Tu = " + tvResult.fTexU.ToString());
                    // Debug.WriteLine("Tv = " + tvResult.fTexV.ToString());
                    // Debug.WriteLine("Fu = " + tvResult.fU.ToString());
                    // Debug.WriteLine("Fv = " + tvResult.fV.ToString());
                    
                    pr.FaceID = tvResult.iFaceindex;
                    pr.ImpactPointLocalSpace = Helpers.TVTypeConverter.FromTVVector(tvResult.vCollisionImpact);
                    pr.ImpactNormal = -ray.Direction; // Helpers.TVTypeConverter.FromTVVector(tvResult.vCollisionNormal); // 
                    pr.CollidedObjectType = PickAccuracy.Geometry;
                }
                #if DEBUG
                else 
                {
                	if (this.ID == @"common.zip|actors/infantry_bot_mk4.obj")
                	{
                		//System.Diagnostics.Debug.WriteLine (System.DateTime.Now.ToString() + " colliding failed.");
                	}
                }
                #endif
            }
            return pr;
        }
        
        public Keystone.Collision.PickResults Collide2D(Keystone.Cameras.Viewport vp, Matrix regionMatrix, KeyCommon.Traversal.PickParameters parameters)
        {
            Keystone.Collision.PickResults pickResult;
            pickResult = new Keystone.Collision.PickResults();
            Keystone.Types.Vector2f mousePosition = new Vector2f (parameters.MouseX, parameters.MouseY);
            
            //System.Diagnostics.Trace.WriteLine("Digest.Collide2D() - Beginning.");

            double previousDistance = double.MaxValue;

            if (mLineList)
            {
            	// TODO: this loop is expensive.  I might decide to make our "Digest" like a Region that has ISpatialStructure and then
            	// insert the records into that and then our picks can be done against the octree for instance.
            	int vertextCount = _tvmesh.GetVertexCount();
	            for (int i = 0; i < vertextCount; i++)
	            {
	                // TODO: for pointsprites, we should test if this call to GetVertex() is too slow and if we should cache coords in that case
	                Vector3d vertex = GetVertex (i);
	                // GetVertex() always returns local space, but we want world.  World Matrix will automatically be in camera space if this is
	                // a Background3D viewpoint following entity so there's no need to subtract camera position.  
	                // From here we can project the vertex coord to screenspace and do easy distance compare to
	                // mouse screenspace location.
	                Vector3d coord = Vector3d.TransformCoord(vertex, regionMatrix);
	                // convert to screenspace
	                coord = vp.Project(coord, vp.Context.Camera.View, vp.Context.Camera.Projection, Matrix.Identity());
	                Vector2f screenSpaceCoord;
	                screenSpaceCoord.x = (float)coord.x;
	                screenSpaceCoord.y = (float)coord.y;
	                // compare the distance between 2d mouse position with the screenpos
	                double distance = (mousePosition - screenSpaceCoord).Length;
	
	                // if the distance is greater than previous continue
	                if (distance > previousDistance) continue; 
	
	                previousDistance = distance;

	                pickResult.FaceID = i;  // for pointsprites, faceID and vertexID are the same thing
	                pickResult.VertexID = i;
	                
	                pickResult.HasCollided = true;
	                // TODO: should this type be DigestRecord instead of Vertex?
	                pickResult.CollidedObjectType = KeyCommon.Traversal.PickAccuracy.Vertex;
	
	                pickResult.ImpactPointLocalSpace = vertex; // TODO: shoudl caller assign the Translation of the record? 
	                pickResult.ImpactNormal = Vector3d.Zero(); // -dir;
	
	                // TODO: verify this DistanceSquared computation is correct.  
	                pickResult.DistanceSquared = vertex.LengthSquared();
	                //     System.Diagnostics.Debug.WriteLine ("Pick distance to cell = "+ _lastPickResult.DistanceSquared.ToString ());
	                pickResult.Matrix = regionMatrix;
	            }
	            
	            //if (pickResult.HasCollided)
	            //	System.Diagnostics.Debug.WriteLine ("Mesh3d.Collide2D() - PointSprite index found = " + pickResult.FaceID);
            }
            else throw new Exception ("Mesh3d.Collide2D() - Expected MeshFormat POINTLIST.");

            //System.Diagnostics.Trace.WriteLine("Vertex ID = " + pickResult.VertexID.ToString());
            return pickResult;
        }


        // TODO: can this be internal scope for all Geometry versions of Render(), AdvancedCollide and Update
        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="scene">Usually Context.Scene rather than Entity.Scene since AssetPlacementTool preview Entity's are not connected to Scene.</param>
        /// <param name="model"></param>
        /// <param name="elapsedSeconds"></param>
        internal override void Render(Matrix matrix, Scene.Scene scene, Model model, double elapsedSeconds)
        {
    		lock (mSyncRoot)
            {
            
			try
            {

            	// TODO: it should be impossible for _resourceStatus to be "Loaded" before it's actually loaded
            	//       HOWEVER if paging out, we could start to render here first since it's not synclocked and then
            	//       while minimesh.Render() we page out and set _resourceStatus to Unloading but we're already in .Render()!
            	
            	// NOTE: we check PageableNodeStatus.Loaded and NOT TVResourceIsLoaded because that 
            	// TVIndex is set after Scene.CreateMesh() and thus before we've finished adding vertices
            	// via .AddVertex()  or .SetGeometry() or even loaded the mesh from file.
                if (_resourceStatus != PageableNodeStatus.Loaded ) return;
                
                // TODO: we should i think provide an overloaded Render() where we can
                // override the Appearance by passing one in?  Typically we can clone the 
                // appearance so all groups match up, then we can do things like alter the
                // material alpha or ambience, etc, however ambience should be done with
                // a shader parameter.  
                // i think supplying an override appearance is the easiest way...
                // one of the downsides is determining which ones will use the alternate
                // and how to create that appearance.  it would be so much better if we
                // could override this in some other fashion...but i just cant really think of a way
                // to do that. 
                // I mean lets say we have a selected entity in the world, again here is an entity
                // where i may want to alter the default rendering appearance... 
                // the thing is, if i modify the existing, i lose the original settings 
                // it wouldnt be bad if there was a way to easily restore original settings.
                // Because we could run functions across the entire graph to modify all appearances
                // in some simple way, but then how do we restore them?
                // is there some way to "remember" the original state or a way to
                // pass overrides to the default state in such a way that we can still
                // take advantage of hashcode test of changes?
                // - because perhaps the .Appearance can store overrides and if the overrides
                //   havent changed then hashcode will be same and no need to modify anything...
                // - also, we could conceivably apply the overrides during cull so that we can
                //   still add them to proper buckets?  bucket ordering is necessary for alpha blending for

                if (model.Appearance != null) // TODO: i could potentially set it to use the parent model's appearance if applicable
                {
                    // TODO: why is this check for geometry added only now setting appearancechanged flag?
                    //if ((_changeStates & Enums.ChangeStates.GeometryAdded) != 0)
                    //    _changeStates |= Keystone.Enums.ChangeStates.AppearanceChanged;

                            // TODO: shouldn't the above call SetChangeStates
                            // TODO: actually, shouldn't this have already been done once this geometry node was added? including after the resource is loaded
                            //SetChangeFlags(Keystone.Enums.ChangeStates.AppearanceChanged, Keystone.Enums.ChangeSource.Self);


                    // TODO: Note: a TextureCycle should be viewed as a visual EFFECT, not an Animation
                    // 
                    _appearanceHashCode = model.Appearance.Apply(this, elapsedSeconds);

                    // TODO: for a depth pass, we might not want to update shader params
                    //       should we have another parameter for this  method (bool updateShaderParameters)

                    // in OnRender, shaderParameters are looped thru and set
                    // but the bonus of a script is this can easily be different for other models.
 
                    // // or wait, actually... the script doesnt need the shader's ID
                    // all it needs is to do...
                    // SomeAPI.SetShaderParameter (modelID, "parameterName", parameterValue);
                    // "PlanetRadiusSquared"  // // radius of planet squared, is needed because the rings.fx doesnt inherently know the size of the planet casting a shadow on the ring
                    // "BeginCameraFade "


                    // iterate thru each shader is probably best
                    //void setParameters(){

                    //    sarMaterial.SetColor("_BaseColor", baseColor );
                    //    sarMaterial.SetFloat("_NormalContribution", normalContribution );
                    //    sarMaterial.SetFloat("_DepthContribution", depthContribution );
                    //    sarMaterial.SetFloat("_NormalPower", normalPower );
                    //    sarMaterial.SetFloat("_DepthPower", depthPower );       
                    //}
                    

                    // shader.UpdateParameters (entityInstance)
                    //    
                    // void UpdateParameters (EntityBase entity)
                    // {
                    //      // the short term fix is to simply derive a new type of shader "RingShader" that then 
                    //      // knows all of it's constants and would know that it needs to get the entity's parent entity World radius
                    //      if (delegate != null)
                    //          delegate.Invoke(entity);
                    // }

                }
                else
                    _appearanceHashCode = NullAppearance.Apply(this, _appearanceHashCode);

            }
            catch
            {
                // note: TODO:  very very rarely ill get an access violation on render after a mesh has been paged in.  The mesh shows as loading fine
                // in my code and in tv debug log.  My only real guess is that when the materials and textures get loaded, there's a race condition
                // where either of those will attempt to set while the mesh is rendering.  we need some kind of guard against that.
                Trace.WriteLine(string.Format("Mesh3d.Render() - Failed to render '{0}' w/ refount '{1}'.", _id, RefCount));
            }

            try
            {                 
                // note: TODO:  very very rarely i will get an access violation on render after a mesh has been paged in.  The mesh shows as loading fine
                // in my code and in tv debug log.  My only real guess is that when the materials and textures get loaded, there's a race condition
                // where either of those will attempt to set while the mesh is rendering.  we need some kind of guard against that. 
                // Shaders as well
                // one bug i fixed (which may have fixed this entirely(?) is if the shader was applied while it was loading, this would
                // result in an access violation.
                // TODO: also can it be the shader applied to it is not fully ready?
                //System.Diagnostics.Debug.WriteLine(model.ID.ToString());
                // TODO: If we are unloading a mesh/scene and we are still calling render,
                //       then problem!  resourcestatus var value should be switch to unloading yes?
                //       - this needs to be fixed for all pageable nodes. 
                //       Because zone paging In/Out occur in another thread, we definetly need to
                //       lock the pageable for these nodes and update their resource status
                //       - clearly there's also a race condition since we check resourcestatus and never prevent it from unloading 
                //       between then and here! does adding lock (mSyncRoot) in this Render() method solve it?
                // TODO: or is the access violation related to the scale set in matrix? this current access violation has scale = 16961330.3042836,16961330.3042836,16961330.3042836
                // but surely i have stars larger than that.  
                //      - is it something in shader?
                _tvmesh.SetMatrix(Helpers.TVTypeConverter.ToTVMatrix (matrix));
                    // NOTE: this will throw accessviolation if we try to modify group materials or textures while rendering.
                    //       An example of this is trying to modify material or texture in HUD.cs on nodes that are attached
                    //       to the scene as opposed to Hud elements that are added only as immediate mode renderables.
                    //       and im not sure how to best prevent render until modifications are done.  Best actually is to
                    //       treat modifications to mesh as we do all attempts to change scene... force through Command processor
                    //       and then through SetProperties()

                // NOTE: Dec.19.2023 - RegionPVS.Add() originally had this code as an optimization to skip rendering of geometry that was 
                //                     completely invisible, but it broke mouse picking which relies on the matrix being updated. 
                //                     Unfortunately, trying to move that optimization here after we've updated the geometry's tvmatrix still fails.
               // if (model.Appearance != null && model.Appearance.Material != null && model.Appearance.Material.Opacity == 0.0f)
               //     return;

                _tvmesh.Render();

            }
            catch
            {
                Trace.WriteLine(string.Format("Mesh3d.Render() - Failed to render '{0}' w/ refount '{1}'.", _id, RefCount));
            }
    		}
        }
        #endregion


        #region IBoundVolume Members
        // From Geometry always return the Local box/sphere
        // and then have the model's return World box/sphere based on their instance
        protected override void UpdateBoundVolume()
        {
            if (_resourceStatus != PageableNodeStatus.Loaded) return;

            float radius = 0f;
            TV_3DVECTOR min, max, center;
            min.x = min.y = min.z = 0f;
            max.x = max.y = max.z = 0f;
            center.x = center.y = center.z = 0f;
            
            if (_box == null)
            	_box = new BoundingBox ();
            else
            	_box.Reset ();
	            
            // model space bounding box
            _tvmesh.GetBoundingBox(ref min, ref max, true);
            _box.Resize(min.x, min.y, min.z, max.x, max.y, max.z);

            _tvmesh.GetBoundingSphere(ref center, ref radius, true);
            _sphere = new BoundingSphere(_box);
            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
        }
        #endregion

        #region IDisposable Members
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            try
            {
            	// TODO: I must sychronize dispose of resource after they are removed from repository
            	//       in the same way that I do with sychronizing their Create() in factory.
            	if (_tvmesh != null)
	                _tvmesh.Destroy();
    
            	System.Diagnostics.Debug.Assert (RefCount == 0, "Mesh3d.DisposeManagedResources() - RefCount not at 0.");
				//System.Diagnostics.Debug.WriteLine(string.Format("Mesh3d.DisposeManagedResources() - Disposed '{0}'.", _id));
                if (_minimesh != null)
                    _minimesh = null; // dont dispose mini, fxinstancerenderer will do that? // TODO: i think that's changd? we have other ways of using minimesh now right? such as MinimeshGeometry
            }
            catch
            {
            }
        }
        #endregion
    }
}
