using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core;
using Core.Cameras;
using Core.Collision;
using Core.Elements;
using Core.Enum;
using Core.FX;
using Core.IO;
using Core.Octree;
using Core.Portals;
using Core.Resource;
using Core.Scene;
using Core.Traversers;
using Core.Types;
using MTV3D65;
using Light=Core.Light;


namespace Keystone.Scene
{
    // NOTE: Hrm.  I had this inheriting FXBase because when scene elements move, I wanted the Scene to be notifiied in case it needs to
    // move that object within the scene to a different sector or octree node.  Or is there another mechanism we want to use for that?
    // Hrm... but if i do do something whereby Update() will handle this sort of thing when its sending those translation commands to the
    // the scene nodes, then it brings into question the Notify() model in general for FXBase... i mean, the Update() here can
    // check for subscriptions and notify the FX directly?  Meh. I dunno.  Need to contemplate this.  
    // On the one hand, it's only Shadows that really utilized subscribers the most... and Water generally doesnt move at runtime except in edit mode.
    // 
    public class Scene3d_2 :  Node, IGroup
    {
        // todo: i havent decided if say spacesectors
        SceneInfo _info;
        OctreeNode _root;
        List< Node> _children;
        
        IFXProvider[] _fxProviders = new IFXProvider[Enum.GetNames(typeof (FX_SEMANTICS)).Length];

        //todo: actually there's no reason we can hold references both in lists and in the _subscrbers collection
        List<Actor3d> _actors = new List<Actor3d>();
        List<Light> _lights = new List<Light>();
        ITraverser _drawer;
        List<string> _debugText = new List<string>();


        // our traverser needs to be more generic for all Node types.
        // and where we can implement new overloaded applys for the types we need specific (non generic) handling for
        //public Scene3d_2(string Name, OctreeNode  root, Camera cam)
        //{
        //    if (cam == null || root == null) throw new ArgumentNullException();
        //    _camera = cam;
        //    _renderer = renderer;
        //    _root = root; // todo: depending on if the root is a quadtree root node or a "Group" container, determines 
        //    // what type of renderer and culler we can use... 
        //    // hrm.. we pass in a Renderer... should we?  We will instance the culler ourselves?  Then what about
        //    // other types like Picker and such.  Maybe we shouldnt pass in the renderer
        //    // and instead keep doing  from outside, Scene.Cull, Scene.Render, etc?  
        //    // and maintain the VisibleList and such for use by other traversers?

        //    //_injector = new OTreeInjector(_root);

        //    //OcclusionQuery notes:
        //    //_camera.OccQuery_Init(1);
        //    //// todo: then render the occluders to a lower res RS

        //    //// then for each item to be rendered do
        //    //int id = _camera.OccQuery_Begin();
        //    ////_camera.OccQuery_DrawBox( ) // draw the box or quad of the mesh to be checked if its occluded
        //    //_camera.OccQuery_End(); //  

        //    //// the above should be done in a loop and then you do the following also in  aloop of the id's
        //    //// iPixelCount = _camera.OccQuery_GetData(id,);
        //    //// if hte iPixelCount = 0 this item is occluded and you dont have to render to the main buffer
        //    //_camera.OccQuery_GetData(id); // calling without the bWaitResult =true will by default block until it returns
        //}

        private ITraverser _culler;
        private bool _portalCullingMode = false;
        private Core.Traversers.Picker _picker;
        private Locator _locator;
       
        private ISector _currentSector;
        private List<ISector> _sectors = new List<ISector>(); // todo: for now lets just store htem all here
        

        public Scene3d_2(string name, SceneInfo info): base(name)
        {
            try
            {
                Add(info);
                
                // create the octree
                OctreeNode root = new OctreeNode("root", info.Center, info.Width, info.Height, info.Depth, (uint)info.OctreeNodeDepth);
                Add(root);
                
                Init(name);

                
                // next determine which manifests to start paging in. This should be moved out
                // of the constructor and into the main Update code, however if we are presented with a starting
                // coordinate, we can pre-page in the scene and use a loading screen 
                // todo: for now we are going to just try and load the root scene that contains the single terrain in our test world
                
                // asychronously start instancing these Nodes and inserting them into the tree
                
                // todo: set this as the current Scene in the Scene dictionary

                //_archiver.Close();
                
                // start rendering using this root node of this scene
                
                // todo: So, in order to handle some GUI element's updates for like progress meters
                // we'll need constants for some SCENE_EVENT_LOAD_PROGRESS or something that can be
                // be used to "register" GUI element's appearance to those events...
                // Consider my keybinder, there I use a script where the "constant" function to bind too
                // is referred to via an alias that is "hooked" at runtime.  So I think a similar
                // function can exist for GUI where we'll have some hard coded routines, but the GUI elements
                // can "bind" to those routines when they are loaded.
                // There are other types of handlers we can specify too that GUI elements can bind too.
                // NetworkIn/Out events so we can update any labels.
                // Paging/Load events so we can update any debug progress meters and such.
                
                // So one question is, should the GUI map directly to those handlers in say Network.Events or Reader.Events, Loader.Events?
                //      
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void Init(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException();
            _name = name;
           

            _culler = new Cull();
            _drawer = new Draw();

            _picker = new Core.Traversers.Picker();
            _locator = new Locator();
                _portalCullingMode = true;

        }

        #region ITraversable Members
        public override void Traverse(ITraverser target)
        {
            target.Apply(this);
        }
        #endregion

        #region ResourceBase members
        protected override void DisposeUnmanagedResources()
        {
            Unload();
            base.DisposeUnmanagedResources();
        }
        #endregion

        #region IGroup Members
        public virtual void NotifyChange(Node child)
        {
            //based on the type of child that is notifying us, determine
            // if we need to (for instance) flag BoundingVolumeIsDirty=true
            // or AppearanceIsDirty=true, etc so that lazy update can occur

        }
        internal void AddChild(Node child)
        {
            if (_children == null) _children = new List<Node>();

            // same node cannot be added twice
            if (_children.Contains(child)) throw new ArgumentException("Node of type ' " + child.TypeName +
                                                    "' already exists.");
            child.AddParent(this);
            _children.Add(child);
            Repository.IncrementRef(child);
            FileManager.WriteNewNode(this);
        }

        public virtual void RemoveChildren()
        {
            while (_children != null)
            {
                RemoveChild(_children[0]);
            }
        }

        public virtual void RemoveChild(Node child)
        {
            try
            {
                if (child is Group)
                    ((Group)child).RemoveChildren();

                _children.Remove(child);
                child.RemoveParent(this);
                if (_children.Count == 0) _children = null;
                Repository.DecrementRef(child);
            }
            catch { }
        }

        public Node[] Children
        {
            get
            {
                if (_children == null) return null;
                return _children.ToArray();
            }
        }

        public virtual int ChildCount
        {
            get
            {
                if (_children == null) return 0;
                return _children.Count;
            }
        }
        #endregion
        public void Unload()
        {
            // remove all nodes from the scene from bottom to top
            // Question: Should this.RemoveChild(_root) automatically result in the recursive removal of child nodes?
            //      That is, inside the RemoveChild() it will first call RemoveChild() on it's own children and thus act recursively
            this.RemoveChildren();
            
            // todo: unload all FX providers?  Should these be apart of scene then or apart of our core engine?
           // todo: am i unsubscribing nodes from FX providers properly?
           
        }
        

        public OctreeNode Root
        {
            get { return _root; }
        }
        
        // todo: these should be IEntity _selected and exist only in the Simulation?
        ModelBase _selected;

        ModelBase Selected
        {
            set { _selected = value; }
        }

        //Insert Scene Elements
        //Remove Scene Elements
        //Move Scene Elemenets
        //   // updates the elements location and position data


        // Add's are  only done through a script or during SceneEdit mode 
        // the items passed in must already have been imported (as tv3d objects) and packaged in our wrappers.
        // Here we will also insert them into the DB and then to the Scene itself
        #region IO
        //private void WriteNewNode(Node node)
        //{
        //    WriteContext context = new WriteContext();
        //    context.Node = node;
        //    _writer.Write(context, 6000);
        //}
        #endregion
        
        
        /// <summary>
        /// Initiates a mouse pick into the scene.  Alternatively there is
        /// TVMaths.GetMousePickVectors (x, z, ref nearStart, ref farEnd)
        /// </summary>
        /// <param name="mouseX">Client mouse position X value.</param>
        /// <param name="mouseY">Client mouse position Y value</param>
        public PickResults[] Pick(Viewport vp, int mouseX, int mouseY)
        {
            // project mouse coords to 3d and create a ray in the direction of the camera
            TV_3DVECTOR v, rayDir;
            TV_3DMATRIX view = vp.Camera.Matrix;
            TV_3DMATRIX projection = vp.Camera.ProjectionMatrix;

            // NOTE: Height and Width are actual client dimensions and not window dimensions.
            v.x = (((2.0f*mouseX)/ vp.TVViewport.GetWidth()) - 1)/projection.m11;
            v.y = -(((2.0f * mouseY) / vp.TVViewport.GetHeight()) - 1) / projection.m22;
            v.z = 1.0f;

            //Engine.Core.Maths.TVMatrixInverse(ref m,ref det, view); // only required if getting the view from device.  TV3D already provides the inverse
            // Transform the screen space pick ray into 3D space
            rayDir.x = v.x*view.m11 + v.y*view.m21 + v.z*view.m31;
            rayDir.y = v.x*view.m12 + v.y*view.m22 + v.z*view.m32;
            rayDir.z = v.x*view.m13 + v.y*view.m23 + v.z*view.m33;
            // todo: if we wanted the far plane location, we'd get the distance and scale the rayDir by that amount
            //       and then add that to the v vector.
            //      
            // todo: alternatively there is 
            // TVMaths.GetMousePickVectors (x, z, ref nearStart, ref farEnd)
            return Pick(vp.Camera.Position, rayDir);
        }


        public PickResults[] Pick(TV_3DVECTOR start, TV_3DVECTOR direction)
        {
            Ray r = new Ray(start, direction);
            PickResults[] results = _picker.Pick(_root, r, PickObjects.All, PickAccuracy.Point, true);

            return results;
        }

        public string[] DebugText
        {
            get { return _debugText.ToArray(); }
        }

        #region IScene Members        
        public int MeshesRendered
        {
            get { return ((Cull) _culler).MeshesVisible; }
        }

        public int ActorsRendered
        {
            get { return ((Cull) _culler).ActorsVisible; }
        }

        public int OctreeNodesVisited
        {
            get { return ((Cull) _culler).OctreeNodesVisited; }
        }

        public int SectorsRendered
        {
            get { return ((Cull) _culler).SectorsVisible; }
        }

        public int TerrainsRendered
        {
            get { return ((Cull) _culler).TerrainsVisible; }
        }

        // picks into the Octree to find the current leaf node that contains the camera
        // if we're in an internal sector with a view to the outside, we still only care about
        // the OctreeNode itself.  However if we cannot see the outside, we dont care and this really
        // shouldnt be called right?
        private ISector GetCurrentNode(TV_3DVECTOR camPosition)
        {
            return _locator.Search(_root, camPosition);
        }

        // For every depth in the octree, we will determine how many sectors outward (concentricly) we also need to
        // draw or retrieve from the db.  So at Root its always 0.
        private int GetConcentricDrawDistance(OctreeNode node)
        {
            // always round up and substract 1
            Debug.Assert(node.BoundingBox.Width > 0);

            if (node.BoundingBox.Width > _drawDistance)
                return 0;
            else
                return (RoundUp(_drawDistance/node.BoundingBox.Width)) - 1;
        }

        private int RoundUp(float valueToRound)
        {
            return (int) Math.Round(Math.Floor(valueToRound + 0.5f));
        }

        private List<OctreeNode> _nodesToLoad;
        private List<OctreeNode> _nodesToUnload;
        private List<OctreeNode> _loadedNodes;
        // gets all nodes around the passed in node that are adjacent to that Node outward in a concentric fashion
        private OctreeNode[] GetAdjacentNodes(OctreeNode targetNode, int concentricDistance, bool includeParents)
            // parents are never duplicated
        {
            // in our quadtree we'd simply start concentricly at a layer and work around.
            // but in octree, we will also need to then work upward after reach evolution 
            // until we've covered all neighbors from top to bottom.
            // We will also need to then append those nodes extending from the top and bottom poles
            //  --- >
            // ^  _  |
            // | |_| |
            //  <--  v

            int i = 0;
            int index = OctreeNode.GetCardinalNeighborIndex(targetNode, (CardinalDirection) i);

            if (targetNode != null)
            {
                // no need to queue any new loads / unloads if we're still in the same sector as last time.  
                //This is our primary scenario
                if (targetNode != _lastSector)
                {
                    //// does the current page need to be loaded?
                    //Page currentpage = targetNode.Page;

                    //if (! IsPageLoaded(currentpage)) Me.Load(currentpage);

                    //// compile list of pages that should be loaded
                    ////_pagesToLoad.add(currentpage)

                    ////For i As Int32 = 0 To 7
                    ////    currentpage = DirectCast(_focusSector.Neighbors(i), Unity.WorldSector).Page
                    ////    _pagesToLoad.Add(currentpage)
                    ////'Next
                    //_nodesToLoad.Add(currentpage);
                    //WorldSector nextSector = _focusSector;

                    //// set nextSector to start at the SW most corner
                    //for (int i = 1; i < concentricDistance; i++)
                    //    nextSector = DirectCast(nextSector.Neighbors(QT_NODE_NEIGHBOR.SW), WorldSector);

                    //_nodesToLoad.Add(nextSector.Page);
                    //// NOTE: The number of pages loaded = ((page_view_distance * 2+ 1)^2   
                    ////so if page_view_distance = 1 then 9 pages loaded. if 2 then 25 if 3 then 49. 

                    //int dir = 0 ; // 0 = east, 1 = west
                    //for (int i = 1; i < concentricDistance*2; i++)
                    //{
                    //    for (int j = 1; j < concentricDistance*2; j++)
                    //    {
                    //        if (dir == 0)
                    //            nextSector = DirectCast(nextSector.Neighbors(QT_NODE_NEIGHBOR.EAST), WorldSector);
                    //        else
                    //            nextSector = DirectCast(nextSector.Neighbors(QT_NODE_NEIGHBOR.WEST), WorldSector);

                    //        _nodesToLoad.Add(nextSector.Page);
                    //    }
                    //    dir = (int) IIf(dir = 0, 1, 0);
                    //    nextSector = DirectCast(nextSector.Neighbors(QT_NODE_NEIGHBOR.NORTH), WorldSector);
                    //    _nodesToLoad.Add(nextSector.Page);
                    //}


                    //// any page in our loadedPages not in this list gets unloaded
                    //foreach (OctreeNode p in _loadedNodes)
                    //    if (!_nodesToLoad.Contains(p))
                    //        _nodesToUnload.Add(p);


                    //// any pages in our pages to Load list not loaded, gets loaded
                    //foreach (OctreeNode p in _nodesToLoad)
                    //{
                    //    if (p != null)
                    //    {
                    //        if (p.PageStatus == PAGE_STATUS.Unloaded)
                    //            Me.Load(p);
                    //    }
                    //}

                    //// unload all the pages no longer needed
                    //foreach (OctreeNode p in _nodesToUnload)
                    //{
                    //    _loadedNodes.Remove(p);
                    //    Me.UnLoad(p);
                    //}
                    //_nodesToLoad.Clear();
                    //_nodesToUnload.Clear();

                    //_lastNode = targetNode;
                }
            }
            return _nodesToLoad.ToArray();
        }

        // Attempts to find an internal portal sector.  Isnt necessarily successful and in that case returns the OctreeRoot which implements ISector
        private ISector FindSector(TV_3DVECTOR position, ISector start)
        {
            //return start;
            if (start != null)
            {
                // the root is not really a pure sector and is the only one that can completely overlap other sectors.
                // so let's ignore that one and test if we've entered a "real" sector
                Locator locator = new Locator();
                return locator.Search(start, position);
            }

            // we will always be in the root sector if not in any other...
            return (ISector)_root;
        }
        
        #region Add
        public void Add (Node node)
        {
            
        }
        internal void Add(OctreeNode root)
        {
            if (_root != null) throw new Exception("Previous root must be removed.");
            _root = root;
            this.AddChild(_root);
        }

        private void Add(SceneInfo info)
        {
            if (_info != null) throw new Exception("Previous info must be removed.");
            _info = info;
            this.AddChild(info);
        }

        internal void Add(ISector sector)
        {
            if (_sectors.Contains(sector)) throw new ArgumentException("Duplicate sectors are not allowed.");
            _sectors.Add(sector);
        }

        public void Add(IFXProvider p)
        {
            int semanticID = (int) p.Semantic;
            if (_fxProviders[semanticID] != null)
                throw new ArgumentException(
                    "Scene.Add (IFXProvider) - Provider at this semantic slot already occupited.  It must first be removed.");

            if (p.Semantic == FX_SEMANTICS.FX_NONE)
                throw new ArgumentException("Scene.Add (IFXProvider) - Provider Semantic not set.  Cannot add Provider.");
            // todo: not sure how to swap though because we do want to register the proper scene elements with 
            // the new semantic.  We might need a traversal and the individual elements might need a sort of bitflag
            // indicating which semantics they should be registered too.
            // also some elements like lights i think might not need these... though actually maybe they do because
            // consider a pointlight may not necessarily be used as a shadowmap volume.  So we would need the semantics bitflag.
            _fxProviders[(int) p.Semantic] = p;
        }

        internal void Add(Occluder o)
        {
        }

        internal void Add(ModelBase m, bool forceAddToRoot)
        {
            if (!forceAddToRoot)
                Add(m);
            else _root.Insert(m, forceAddToRoot);
            //todo: I believe this needs to check if we can insert to an ISector first?
        }

        internal void Add(ModelBase m)
        {
            try
            {
                _root.Insert(m, false);
            }
            catch
            {
                Trace.WriteLine("Error injecting mesh at position " + m.Position.ToString());
            }
        }


        internal void Add(Terrain t)
        {
            //todo: load the EnableLOD settings.ini as scene.settings and then set them here
            t.FrustumCullMode = FrustumCullMode.custom;
            _root.Insert(t, false);

            FileManager.WriteNewNode(t);
        }

        internal void Add(Light l)
        {
            // not sure what to do about "position" because Directional doesnt have one, but to simulate sunlight
            // i think maybe we should use a direction and even an artificial range to allow us to define the area
            // and create a "box" and to maybe help us determine if when indoors when we are able to see light.
            // And recall, even that is maybe not 100% enough because we dont want indoor meshes lit by sunlight
            // it would be light coming through the walls.  So i think we need to turn lights on/off during the render
            // So perhaps categorically, when rendering indoors, turn off the outdoor lights.  Maybe that's the deal.
            //
            // _root.Insert(l);  //_injector.Inject(l); // todo: adds the light to the octree.  At runtime, the Renderer traversal
            // should manage the indoor / outdoor light stack
            _lights.Add(l);
        }
        #endregion

        /// <summary>
        /// Removes elements from the DB and the Scene itself
        /// </summary>


        #region Paging
        //// The game loop runs and calls Scene.Update() which will compute which octreenodes need to be loaded/unloaded
        //// and then those loads/unloads are handled by the loader.  Completed loads are notified via the PageNotify callback function
        //// which is passed to each Loader.Load() method

        //private void SQLQueryResultNotify(Context context)
        //{
        //    switch (context.Code)
        //    {
        //        case RESULTS:

        //        case ABORTED:

        //        case ERROR:

        //    }
        //}
        //private void PageNotify(Context context)
        //{
        //    // if the object is loaded, add it to the scene.  The context will contain the info we need
        //    // to know where in the scene to add this object.
        //    // since this notification occurs on the main thread, there's no need to lock the scene.

        //    switch (context.Code)
        //    {

        //        case ABORTED:
        //            //remove from the list of objects that we're waiting to receive abort confirmation 

        //        case LOADED:
        //            // verify that because of lack of synchronization that we didnt intend to abort this by checking the abort list

        //            // if not aborted, then add it to the scene and remove it from the list of items we're waiting to receive load confirmation

        //        case UNLOADED:
        //            //         

        //    }
        //}

        //// these are paging specific functions that 
        //UnloadNode() // nulls a branch or leaf of the Octree.  Any items in the Changes list must be processed prior to unloading.  

        //The parent is notified that this child is unloaded.   (perhaps all Children for any parent must be fully loaded or 
        //unloaded?  No scenarios where some children are loaded but others are not...)

        //LoadNode() // adds the node to the partial Octree in memory and to the correct parent.

        //private void UnloadSceneElements () {} // doesnt touch the DB, just unloads from in memory

        #endregion

        public ModelBase Selected;

        private void UpdateDebugText(Camera camera)
        {
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add(" ");
            _debugText.Add ("ESC exits.");
     
            _debugText.Add("Octree Nodes: " + OctreeNodesVisited);
            //_debugText.Add("Sectors: " + SectorsRendered);
            //_debugText.Add("Terrains: " + TerrainsRendered);
            //_debugText.Add("Actors: " + ActorsRendered);
            //_debugText.Add("Meshes: " + MeshesRendered);


            //if (_customFrustumEnabled)
            //    _debugText.Add("User Frustum On");
            //else
            //    _debugText.Add("User Frustum Off");


            //if (_occlusionEnabled)
            //    _debugText.Add("Occlusion On");
            //else
            //    _debugText.Add("Occlusion Off");

            //if (_sphereTest)
            //    cullText = "Occlusion: Test Mesh Spheres";
            //else
            //    cullText = "Occlusion: Test Mesh Boxes";
            //y += 10;
            //Core.Text.TextureFont_DrawText(cullText, x, y, Core.Globals.RGBA(1, 0, 1, 1), textureFontId);
            string text = string.Format("CAMERA Pos: [{0}, {1}, {2}] Look: [{3}, {4}, {5}]", camera.Position.x, camera.Position.y, camera.Position.z, camera.LookAt.x, camera.LookAt.y, camera.LookAt.z);
            _debugText.Add(text);
           
            foreach (Actor3d a in _actors)
            {
                text = string.Format("PLAYER '{0}' Pos: [{1}, {2}, {3}]", a.Name , a.Position.x, a.Position.y, a.Position.z);
                _debugText.Add(text);
            }

            if (_selected != null)
            {
                text = string.Format("SELECTED '{0}' Pos: [{1}, {2}, {3}]  Rotation: [{4}, {5}, {6}] Scale: [{7}, {8}, {9}]", _selected.Name, _selected.Position.x, _selected.Position.y, _selected.Position.z, _selected.Rotation.x, _selected.Rotation.y, _selected.Rotation.z, _selected.Scale.x, _selected.Scale.y, _selected.Scale.z);
                _debugText.Add(text);
               
            }

            // send to the debugDraw static object for queued drawing which gets "commited" in our actual Render() method
            foreach (string s in DebugText)
            {
                DebugDraw.DrawText(s, CONST_TV_COLORKEY.TV_COLORKEY_MAGENTA);
            }
        }

        public Stack<Node> CullRequest(Camera camera)
        {
            Cull tmpCuller = new Cull();
            tmpCuller.Clear(camera);
            _root.Traverse(tmpCuller);
            return tmpCuller.NodeStack;
        }
        
        public void Cull()
        {
            //_root.Traverse(culler);

            // copy the visible list from the traverser

            // note: any shadow map culling has been done too at this point?
        }
                
        //long StopBytes = 0;
        //public int GetSize(object foo)
        //{
        //    // do this prior to instantiating the object
        //    long StartBytes = System.GC.GetTotalMemory(true);
            
        //    // do this after instantiating the object
        //    StopBytes = System.GC.GetTotalMemory(true);
        //    GC.KeepAlive(foo); // This ensure a reference to object keeps object in memory

        //    Debug.WriteLine("Size is " + ((long)(StopBytes - StartBytes)).ToString());
        //}

        ISector _lastSector;
        ISector[] _adjacents;
        float _drawDistance = 500f; // right now this is the distance in meters that should be drawn.
        // GetSectorDrawDistance then uses this value to come up with the int distance in Sectors

        public void BeginUpdate(TV_3DVECTOR position)
        {
            
        }
        
        public void EndUpdate(object callback)
        {
            
        }
        
        public void Update(float elapsed, Camera camera)
        {

            // based on the location of the camera and the draw distance
            ISector currentNode = GetCurrentNode(camera.Position);
            //todo: bug.  the following can occur when paused (e.g. breakpoint/debugging) and resuming because the timeelapsed doesnt get reset.
            // best solution even for this i think is to just use min/max to restrict the camera since not always when "pausing" do we want sim time
            // to stop... meaning we still want Simulation to run, just not rendering..
           // if (currentNode == null) throw new Exception("Camera has exceeded the bounds of the World.");
    
            if (currentNode is OctreeNode)
            {
                int distance = GetConcentricDrawDistance((OctreeNode)currentNode);

                if (currentNode != _lastSector)
                {
                    _lastSector = currentNode;
                    //Node[] adjacents = GetAdjacentNodes(currentNode, distance, true);

                    // since the adjacents are likely to be different too now, we will want to cancel any potential loading
                    // of those in the _lastAdjacents that are no longer in this one.  

                    // and for any that are already loaded, we will want to unload


                    // and when paging, we simply Load and the pager will use a priority queues for these types
                    // to ensure nodes are loaded by depth and then by integer distance from the currentNode


                    // note that the pager itself is just a friendly go between to the main threadpool object we use for the database workers also
                    // it knows how to break up complex Nodes, Sectors and Model objects for instance that are passed in, and send them 
                    // individually to the threadpool and then waits for all the parts to load so it can then report back to the caller
                    // when the entire object is ready.
                    // It can maintain the relationships and ownerships so that if an abort comes in, it can easily determine which ones
                    // need to be aborted.


                    // so now for all sectors or nodes that need to be loaded, we send the query to the DB to get the
                    // assets that contain those nodeID's  

                    // now we create contexts and have the pager handle the rest

                }
            }
            else
            {
                // we are in an internal sector and we want to find and prepare to page adjacent sectors
                
            }
            
            // find the current internal sector we're in. If not it returns OctreeRoot's ISector implementation
            _currentSector = FindSector(camera.Position, _currentSector);
            // TODO: Following line is  atemp hack because we are not properly detecting the currentSEctor in our FindSector
            // its resulting in us skipping the darn root octreenode which includes the terrain
            _currentSector = _root; // <-- TODO: Temp hack

            //camera.Frustum.Update();
            ((Cull) _culler).Clear(camera);

            // portal rendering we always start in the camera's curent sector if it exists.
            if (_portalCullingMode && _currentSector != null)
                _currentSector.Traverse(_culler);

            else
                // elegantly the following line of code works regardless of portal culling mode or not.
                //even though the current sector is null, the root node is also a Sector 
                // since with portalrendering the root sector must always be a Sector even a Quadtree implementing ISector
                _root.Traverse(_culler);

            // get and output the culling statistics
            UpdateDebugText(camera);

            // update all the FX Managers running in the scene
            foreach (IFXProvider fx in _fxProviders)
                if (fx != null) fx.Update((int) elapsed, camera);

            if (_selected != null)
                DebugDraw.DrawBox(_selected.BoundingBox, CONST_TV_COLORKEY.TV_COLORKEY_RED);
        }

        public void RenderBeforeClear(Camera camera)
        {
             // todo: do I need HACK because when water and such modifies the camera, the subsequent calls to CulLDrawer.Clear() would then start using 
            // that incorrect position to Translate models??
            foreach (IFXProvider fx in _fxProviders)
                if (fx != null) fx.PreRender(camera);

        }

        public void Clear()
        {
            _debugText.Clear();
            AppMain._core.Engine.Clear();
        }

        // RenderScene() is the method for drawing the visible items.
        // It's also the method that is invoked directly by FX's such as reflecting Water
        // to render the reflection.  As such, we have to test 
        // todo: not meant to be a "public" method but for right now during init in Engine.CS i need to refer to this
        // method for the callback method for some of the FX
        public void RenderScene(Camera camera)
        {
            // we only render sky here
            foreach (IFXProvider fx in _fxProviders)
                if (fx != null && fx.Semantic == FX_SEMANTICS.FX_SKY )
                    fx.Render(camera);


            // draw the scene // TODO: We need to use a culler that is specific to the camera since this method gets invoked by FX doing things such as reflection
            ((Draw) _drawer).Render(camera,(Cull) _culler);

            // draw those scene elements that should be drawn after the rest of the scene has been drawn
            foreach (IFXProvider fx in _fxProviders)
            {
                // Some FX's "Render()" method (e.g Water) must only be rendered in the final scene render.
                // Is there a better way for detecting these types?  In effect, any "reflection" FX cant do this.
                // but that's why Water's dont override the fx.Render() method.
                if (fx != null && fx.Semantic != FX_SEMANTICS.FX_SKY && fx.Semantic != FX_SEMANTICS.FX_WATER_LAKE && fx.Semantic != FX_SEMANTICS.FX_WATER_OCEAN)
                    fx.Render(camera);
            }
        }


        public void Render(Camera camera)
        {

            RenderScene(camera);
          
            // render post FX such as Bloom
            foreach (IFXProvider fx in _fxProviders)
            {
                // NOTE: because Bloom uses the 2d fullscreen quad draw command it must be the very last
                // 2d operation or it will get wiped out.  Water is also rendered here because the actual water meshes are only rendered once
                if (fx != null) fx.PostRender(camera);
            }

            DebugDraw.CommitDisplayList();
            DebugDraw.CommitTextList();
            
            // interestingly, the way AccurateTimeElapsed works is, it takes the time between frameRenderN - frameRenderN-1
            // this means the following debug line = 0 first time because there is no frameRenderN or frameRenderN-1, 
            // however the second time around, the value is filled.  This must be because when the engine is initialized
            // the frameRenderN = Now 
            // Anyways, this isnt more accurate than me calling Environement.GetTickCOunt() just once at the same place everytime.
            // The difference is, TV's version is immune from changing every subsequent call between frames.
            // 
            //Debug.WriteLine(string.Format("Accurate time elapsed == {0}", Engine.Core.Engine.AccurateTimeElapsed()));
            AppMain._core.Engine.RenderToScreen();
        }
        #endregion
    }
}