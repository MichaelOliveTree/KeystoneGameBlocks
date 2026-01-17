//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using Keystone.Elements;
//using Keystone.Traversers;
//using Keystone.Types;

//namespace Keystone.SpatialNodes.Octree
//{
//    public enum OctreeQuadrant : int
//    {
//        ROOT = -1,
//        // these must be numbered 0 - 7 to match the array indices
//        NX_Y_NZ = 0,
//        X_Y_NZ = 1,
//        NX_NY_NZ = 2,
//        X_NY_NZ = 3,

//        NX_Y_Z = 4,
//        X_Y_Z = 5,
//        NX_NY_Z = 6,
//        X_NY_Z = 7
//    }

//    public enum CardinalDirection : int
//    {
//        East = 0,
//        West,
//        South,
//        North,
//        Front,
//        Back
//    }

//    public enum Modifier : int
//    {
//        EastWest = 1,
//        NorthSouth = 2,
//        BackFront = 4
//    }

//    /// <summary>
//    /// A dynamic + loose octree implementation. 
//    /// Dynamic = children are only added up to max depth to accomodate items being inserted into the tree.
//    /// This class implements ISector because we always want any depth octreenode to be link-able to/from a portal to another type of ISector.
//    /// </summary>
//    public class OctreeNode : ITraversable, IBoundVolume
//    {
//        public static BoundingBox WorldBox;
//        private static uint _maxDepth;
//        private static uint _splitThreshHold;

//        private const int ROOT_DEPTH = 0; // the root node is depth 0.  The first set of 8 children are at depth 1, etc.
//        private int _id;

//        private Vector3d _radius; //tight radius  // TODO: should be computed on fly
//        private uint _depth; // TODO: depth could be determined from naming convention of node iD
//                             // where node with name.Length = 1 is root.  name.Lenght == 2 is first child, etc
                             
//        private OctreeQuadrant _quadrant; // octreeQuadrant can be deduced from name 

//        // TODO: if we only store the center position of a node, we can easily compute sphere, loose and tight boxes
//        private BoundingBox _looseBox;// externally visible as our regular BoundingBox and is used to determine visibility
//        private BoundingBox _tightBox; // internally used to determine placement in the graph
        

//        private int[] _neighbors;    // how can we reference neighbors that are not instanced because they've been collapsed for being empty to save memory?
        
//        private OctreeNode _parent;
//        private OctreeNode[] _children;

//        // TODO: switch to linked list?
//        private List<SceneNode> mElements;  // TODO: this is just wrong.  As it turns out I believe I want to use Octree for static geometry particularly outdoor worlds.

//        // we can still handle moving static geometry like barrels and such that get moved, but by and large we dont.
//        // and we allow the Octree itself to handle hierarchical bounding and depth balancing.  But it does NOT
//        // deal with SceneNodes under SceneNodes, etc.  Actually for a "player" entity that has items attached to it.
//        // we do want to treat that as a group that must always be under a single grid... but this assumes moving players exist in the grid?  And that assumes
//        // that the grid is fully formed so we dont waste time creating/destroying grids that are not there or that are now empty.
//        //

//        #region ITraversable Members

//        public void Traverse(ITraverser target, object data)
//        {
//            return target.Apply(this, data);
//        }

//        #endregion

//        public OctreeNode(BoundingBox box, uint maxDepth)
//            : this(box.Center, box.Width, box.Height, box.Depth, maxDepth)
//        {
//        }

//        public OctreeNode(Vector3d center, double width, double height, double depth, uint maxDepth)
//        {
//            if (maxDepth < 1) throw new ArgumentOutOfRangeException("MaxDepth must be >=1");
//            if (width <= 0 || height <= 0 || depth <= 0)
//                throw new ArgumentOutOfRangeException("Width, height and depth must be > 0.");
//            _maxDepth = maxDepth;
//            _depth = ROOT_DEPTH;
//            _parent = null;
//            _radius = new Vector3d(width * .5d, height * .5d, depth * .5d);
//            _id = 0; //root is always 0
//            _quadrant = OctreeQuadrant.ROOT;
//            _tightBox = new BoundingBox(center - _radius, center + _radius);
//            _looseBox = _tightBox; // for the root node, these are the same
//            WorldBox = _looseBox;

//            UpdateBoundVolume();
//        }

//        // This PRIVATE version of the constructor is called in the CreateSubNodes() function
//        // which itself is called in the Insert() method.
//        // Note the loosebox is computed automatically from the tightbox passed in.
//        private OctreeNode(OctreeNode parent, int id, OctreeQuadrant quadrant, BoundingBox tight)
//        {
//            if (parent == null) throw new ArgumentNullException();
//            _depth = parent.Depth + 1;
//            _parent = parent;
//            _tightBox = tight;
//            _id = id;
//            _quadrant = quadrant;

//            _radius = parent.Radius * 0.5d; // tight radius of child is always half the parent
            
//            //Vector3d overlappedRadius = parent.Radius; // overlapped is twice the tight (aka: same as parent)

//            _looseBox = new BoundingBox(tight.Min - _radius, tight.Max + _radius);
//            UpdateBoundVolume();
//        }

//        ~OctreeNode()
//        {
//        }

//        public bool IsLeaf { get { return mElements == null || mElements.Count == 0; } }

//        public int ID
//        {
//            get { return _id; }
//        }

//        internal Vector3d Radius
//        {
//            get { return _radius; }
//        }

//        internal uint Depth
//        {
//            get { return _depth; }
//        }

//        public OctreeQuadrant Quadrant
//        {
//            get { return _quadrant; }
//        }

//        public OctreeNode[] OctreeChildren
//        {
//            get { return _children; }
//        }

//        public OctreeNode Parent
//        {
//            get { return _parent; }
//        }

//        public SceneNode[] Elements
//        {
//            get
//            {
//                if (mElements == null) return null;
//                return mElements.ToArray();
//            }
//        }

//        public void Insert(SceneNode element)
//        {
//            Trace.WriteLine("Inserting scene node '" + "'...");
//            //TODO: how to notify when element moves to determine if we need to move the element in the tree?
//            //TODO: what about removing elements?  notifications and movement?
//            //TODO: what about reclaiming (de-allocating) child nodes that are empty for a "long" time.
//            //      maybe on remove, if all children under a parent are now empty set a time and then
//            //      periodically in our update thread we can "compact" or "prune" traversal.
//            // TODO: use an insert traversal.
//            // TODO: I suppose as a sceneNode is "inserted" into the scene, the scene maintains the list of subscribers.
//            //       then as the sceneNode moves, it can fire a notify to the Scene its subscribed too.
//            //       Then if the sceneNode needs to move in the graph, the handler can do it easily by
//            //       (and can optimize for most cases by reverse recursive checking since odds are the element has just moved
//            // over to a sibling.

//            Vector3d childNodeRadius = Radius*0.5d;
//            BoundingBox elementBox = element.BoundingBox;
//            Vector3d elementCenter = elementBox.Center;
//            Vector3d elementRadius =  new Vector3d(elementBox.Width, elementBox.Height, elementBox.Depth) * .5d;
            

//            // will this node being added fit into one of the child nodes loose boxes based on it's radius and the child's Tight radius?
//            // The logic goes, if it will fit in the child's tight radius without considering position, then it will definetly fit 
//            // into one of the children's loosebox's when we do take into account the node to be inserted's positioning,
//            if (_depth < _maxDepth && V1LessThanEqualsV2(elementRadius, childNodeRadius))
//            {
//                // lazy instantiation of child nodes 
//                // TODO: on node removal we should also have lazy reclaiming of nodes when all children
//                // underneath a particular branch contains no more elements
//                if (_children == null) _children = CreateSubNodes(this);
                
//                foreach (OctreeNode subnode in _children)
//                {
//                    // Find which child to insert it into. 
//                    // We only need to TEST THE CENTER POINT because 
//                    // we already know if the center fits into the tight box
//                    // the whole box will fit into it's loose box
//                    if (subnode.TightBox.Contains(elementCenter))
//                    {
//                        subnode.Insert(element);  // recursive to find smallest child sub node 
//                        return;
//                    }
//                }
//                // ERROR: The element being inserted lies outside of the bounds of the entire octree
//                Debug.Assert(false, "Must not attempt to insert that is outside of the bounding volume of the Octree.");
//            }
//            else // wont fit in any of the child octree nodes so add it here
//            {
//                if (mElements == null) mElements = new List<SceneNode>();
//                if (mElements.Contains(element)) throw new ArgumentException("Octree.Insert() -- Child SceneNode already exists.");
//                mElements.Add(element);
//                //Trace.WriteLine(string.Format("'{0}' added to octree at depth {1}", sn.Entity.Name, _depth));
//                Trace.Assert(_looseBox.Contains(elementBox),
//                string.Format("'{0}' should fit, but does not.  This should never happen!", "bleh"));
//            }
//        }


//        #region IBoundVolume Members

//        internal BoundingBox TightBox
//        {
//            get { return _tightBox; }
//        }

//        public BoundingBox BoundingBox
//        {
//            get { return _looseBox; }
//        }

//        public BoundingSphere BoundingSphere
//        {
//            get { return null; } // TODO: compute center from x,y,z index, then return sphere new BoundingSphere(center, _radius); }
//        }

//        public bool BoundVolumeIsDirty
//        {
//            get { return false; } // octree bounds are fixed.
//        }


//        protected void UpdateBoundVolume()
//        {
//        }

//        #endregion

//        private bool V1LessThanEqualsV2(Vector3d v1, Vector3d v2)
//        {
//            return (v1.x <= v2.x && v1.y <= v2.y && v1.z <= v2.z);
//        }

//        // Create 8 child nodes.  
//        // NOTE: A particular octreenode has either 8 children or none.  It is either a collapsed
//        // leaf or a group with 8 children.
//        // TODO: However, a sparse tree does not even create the empty children... i could
//        // very well leave the empty ones null and i dont think it ocmplicates the code much more...
//        // just an extra null test.
//        // http://www.altdevblogaday.com/2011/08/01/loose-octrees-for-frustum-culling-part-1/
//        private static OctreeNode[] CreateSubNodes(OctreeNode parentOctreeNode)
//        {
//            // TODO: in an optimized for memory version of this octree, we should always compute
//            // a bounding box on the fly using recursive method based on id and it's path to parent.
//            // or at very least, we should ONLY store the center of an octreeNode.
//            BoundingBox box = parentOctreeNode.TightBox;
//            double minX = box.Min.x;
//            double minY = box.Min.y;
//            double minZ = box.Min.z;

//            double maxX = box.Max.x;
//            double maxY = box.Max.y;
//            double maxZ = box.Max.z;

//            double midX = (minX + maxX) * .5d;
//            double midY = (minY + maxY) * .5d;
//            double midZ = (minZ + maxZ) * .5d;

//            OctreeNode[] subNodes = new OctreeNode[8];

//            Vector3d min;
//            Vector3d max;

//            // given a node's ID, we can compute/find the id's of its children as being
//            // childID's 1 thru 8 = (parentOctreeNode'sID * 8) + 1 thru 8 respectively.
//            // we actually use (parentOctreeNode'sID * 8) + 1 + 0 thru 7 because our OctreeQuadrant enum starts at 0 for first non root quadrant type 
//            int id;

//            min = new Vector3d(minX, minY, minZ);
//            max = new Vector3d(midX, midY, midZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_Y_NZ;
//            subNodes[(int) OctreeQuadrant.NX_Y_NZ] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_Y_NZ,
//                               new BoundingBox(min, max));

//            min = new Vector3d(minX, minY, midZ);
//            max = new Vector3d(midX, midY, maxZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_Y_NZ;
//            subNodes[(int) OctreeQuadrant.X_Y_NZ] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_Y_NZ,
//                               new BoundingBox(min, max));

//            min = new Vector3d(minX, midY, minZ);
//            max = new Vector3d(midX, maxY, midZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_NY_NZ;
//            subNodes[(int) OctreeQuadrant.NX_NY_NZ] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_NY_NZ,
//                               new BoundingBox(min, max));

//            min = new Vector3d(minX, midY, midZ);
//            max = new Vector3d(midX, maxY, maxZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_NY_NZ;
//            subNodes[(int) OctreeQuadrant.X_NY_NZ] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_NY_NZ,
//                               new BoundingBox(min, max));

//            min = new Vector3d(midX, minY, minZ);
//            max = new Vector3d(maxX, midY, midZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_Y_Z;
//            subNodes[(int) OctreeQuadrant.NX_Y_Z] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_Y_Z,
//                               new BoundingBox(min, max));

//            min = new Vector3d(midX, minY, midZ);
//            max = new Vector3d(maxX, midY, maxZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_Y_Z;
//            subNodes[(int) OctreeQuadrant.X_Y_Z] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_Y_Z,
//                               new BoundingBox(min, max));

//            min = new Vector3d(midX, midY, minZ);
//            max = new Vector3d(maxX, maxY, midZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_NY_Z;
//            subNodes[(int) OctreeQuadrant.NX_NY_Z] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_NY_Z,
//                               new BoundingBox(min, max));

//            min = new Vector3d(midX, midY, midZ);
//            max = new Vector3d(maxX, maxY, maxZ);
//            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_NY_Z;
//            subNodes[(int) OctreeQuadrant.X_NY_Z] =
//                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_NY_Z,
//                               new BoundingBox(min, max));

//            return subNodes;
//        }

//        // I've decided that based on the world size and max depth, all nodes will be added to the DB upon creation and at once.
//        // THat is to say, we wont stop prior to max depth just because no scene elements exist along that path.
//        // Instead, we will add a "childrenEmpty" flag on all parent nodes so that we can stop retrieval or processing
//        // of a parent's children if those children have no elements.


//        //So let's assume we want to dynamically find siblings without storing them anywhere, 
//        //this is used primarily for paging.  

//        //- a node either has children or it does not. It cant just have 1 or 2 or 5.  It's always 8.
//        //- if a sparse node that has no children at max depth because of the sparseness of the area,
//        // and you want to find which nodes to page in (and under this scenario it could 
//        //very well require we find neighbors and then go at lower depths than were we started 
//        //the search) what do we do?

//        //   1) get the current node which is the lowest depth node containing the camera.
//        //   2) cache that node and all nodes leading to it.
//        //   3) depending on the draw range we need to determine the id's of all concentric neighbors 
//        //from this one out to the max distance.  We have a formula that can find node id's 
//        //for any node that neighbors another and will account for world wrap and boundaries.

//        //    a) this could very well mean then that our database is not a dynamic octree but 
//        //rather it has all the child nodes (even the empty ones) but that we will flag
//        //whether any node in the database has "elements" and then recursively set all 
//        //parent's flags to whether any of their children has elements or not so that 
//        //when we retreive nodes during a query we can (maybe with a store procedure?) skip
//        //the retrieval of those that have no elements down its hierarcy.
//        //   4) during paging, the work items are added hierarchally but also perhaps in level order 
//        //so that all nodes of 1 depth are loaded first and then all of their children 
//        //and then all of their children's children, etc.
//        //        a) so perhaps just load all the nodes we've computed as needing and then sort them 
//        //after retrieval or maybe the db can sort by the depth field of a node.

//        //-  So i think with the above, the key points is that we are building an overlapping grid at the lowest level. 
//        //-  Our database will contain alot of records but that this shouldnt be a problem.  We will test this first thing
//        // and will only try to come up with other solutions _if_ it becomes way too slow or takes too much space.

//        //Possibly, even by generating these elements in advance and enforcing that all paths lead to the max depth 
//        //node, we _can_ still enforce which nodes any particular scene element is added to... be it a end 
//        //child node or a parent node.  In fact, this _must_ be the case because i now recall that some large 
//        //scene elements will only fully fit in bigger nodes.  So in our editor, when we are adding/removing 
//        //scene elemnts each element will have a NodeID tag to indicate where it exists.  When it moves, 
//        //we can compute and update the db if necessary. And when adding/removing elements, we can determine
//        //if we want to push some sparse entries down to childen (if they will fit) or remove them from children 
//        //and push them up to the parent and flag the parent as all children no longer having any elements...  

//        //So i think thats our path we must take and we need to produce the list of the queryies and routines we need to 
//        //handle these situations... how much can be done in the query versus, versus stored proc, or a method.  

//        //One big question is that since parents can contain scene elements, then whenever we need to page the world,
//        //we cant simply compute concentrically the end nodes, we must also retrieve all the parents of 
//        //those nodes but with no duplicates.  It's probably best to do this in an algorithm so that we 
//        //compute all the indices we need and then just retrieve all those at once... probably our 
//        //algorithm can compute the list in the "level" order we want too.
//        // TODO: ActiveNodes is only used if we pre-generate the entire Octree but 
//        // if we have multiple Octree's in seperate Graphs(Zones) then this ActiveNodes must be cleared before building 
//        //public static Dictionary<int, OctreeNode> ActiveNodes = new Dictionary<int, OctreeNode>();
//        //public static void GenerateNodes(OctreeNode parentOctreeNode, uint maxDepth)
//        //{
//        //    if (parentOctreeNode.Depth < maxDepth)
//        //    {
//        //        parentOctreeNode._children = CreateSubNodes(parentOctreeNode);

//        //        if (parentOctreeNode._children == null) throw new Exception();
//        //        //node._children = new OctreeNode[8];

//        //        for (int i = 0; i < 8; i++)
//        //        {       //node._children[i] = children[i];
//        //            parentOctreeNode._children[i]._parent = parentOctreeNode;
//        //            ActiveNodes.Add(parentOctreeNode._children[i].ID, parentOctreeNode._children[i]);
//        //        }

//        //        foreach (OctreeNode node in parentOctreeNode._children)
//        //        {
//        //            GenerateNodes(node, maxDepth); // recurse using the child's tightbox
//        //        }
//        //    }
//        //}

//        ////$#$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
//        ////$$$$$
//        //// IMPORTANT:  As it turns out, GenerateNodes, SaveNodes, LoadNodes, AssignNeibhgors, CardinalDirection and all the rest below this line
//        //// could just be added to our Pager object instead because we will not be assigning neighbors permanently but will try to just
//        //// compute them at runtime based on the current node's index.
//        ////
//        //public static void SaveNodes()
//        //{
//        //    // the above generates the basic tree.
//        //    // Now we can flush the entire ActiveNodes to the db and then start paging
//        //    // TODO: save all ActiveNodes to the db.

//        //    // Below, we compute the neighbor ID's for the Cardinal direction neighbors.
//        //    // In theory we dont even have to maintain sibling ID's, we can compute them for any given
//        //    // node on the fly.  But for now at least, we will compute them and store them all in one go
//        //    // TODO: add code to also compute any of the other non cardinal direction adjacent nodes. (only needed for depth > 1)
//        //    // WARNING: Actually by at least assigning the 6 cardinal neighbor ID's we vastly simply the process of finding the
//        //    // outer concentric layers around a given node because we can simply travel along the 6 cardinal neighbors
//        //    // easily.  Although we could compute each cardinal direction each time on the fly.. we'll try that first
//        //    // since it doesnt require us adding anything to the OctreeNode or the database
//        //    foreach (OctreeNode currentNode in ActiveNodes.Values)
//        //    {
//        //        AssignNeighbors(currentNode);
//        //    }
//        //}


//        /// <summary>
//        /// Fills the neighbor's int array with OctreeNode index values
//        /// of the neighbors.
//        /// </summary>
//        /// <remarks>
//        /// These neighbors may not be instanced!  They may be collapsed if they are empty 
//        /// in order to save memory.
//        /// </remarks>
//        /// <param name="node"></param>
//        private static void AssignNeighbors(OctreeNode node)
//        {
//            node._neighbors = new int[6];

//            // given a node's, we can find all 6 of it's CARDINAL direction neighbors as follows
//            // based on the current node's quadrant ID we use the appropriate DirX,Y,Z constant.
//            // for depth 1, we just use these values direcly.  For depths > 1 we use the following formula
//            for (int i = 0; i < 6; i++)
//                node._neighbors[i] = GetCardinalNeighborIndex(node, (CardinalDirection) i);
//        }

//        public static int GetCardinalNeighborIndex(OctreeNode node, CardinalDirection dir)
//        {
//            // for each neighbor, determine the distance to the common ancestor.  
//            int ancestorDistance = GetCommonAncestorDistance(node, dir);
//            int modifier = 0;
//            bool worldWrapped = false;
//            NeighborIndex(node, dir, WorldBox, ref modifier, ref worldWrapped);
//            // modifier is computed based on value of dir
//            int neighborOffset = modifier;

//            if (ancestorDistance == 1)
//            {
//                // Children that share same parent are at distance 1 and use the resulting modifier directly
//                // NOTE: worldWrapped usually has no effect because by definition, if its worldwrapped they do not share the same parent
//                // UNLESS they are depth = 1 and share the root node as parent
//                if (!worldWrapped)
//                    neighborOffset -= modifier;
//                else
//                    // however if world wrapping, to find the neighbor on the other side of the world
//                    // the formula changes by making the subtraction sign into a positive sign as
//                    neighborOffset += modifier;
//            }
//            else
//            {
//                // children that share a same grandparent are at minimum ancestorDistance of 2 and use the formula
//                for (int j = 2; j < ancestorDistance + 1; j++) // starting at depth 2
//                {
//                    if (!worldWrapped)
//                        neighborOffset = (neighborOffset*8) - modifier;
//                    else
//                        // however if world wrapping, to find the neighbor on the other side of the world
//                        // the formula changes by making the subtraction sign into a positive sign as
//                        neighborOffset = (neighborOffset*8) + modifier;
//                }
//            }
//            return node._id + neighborOffset;
//        }

//        private static void NeighborIndex(OctreeNode node, CardinalDirection direction, BoundingBox worldBox,
//                                          ref int modifier, ref bool worldWrapped)
//        {
//            // determine if the neighbor in the specified direction requires us to 
//            // world wrap if worldWrap is enabled.
//            switch (direction)
//            {
//                case CardinalDirection.West:
//                    if (node.BoundingBox.Min.x == worldBox.Min.x)
//                        worldWrapped = true;

//                    modifier = (int) Modifier.EastWest;
//                    break;
//                case CardinalDirection.East:
//                    if (node.BoundingBox.Max.x == worldBox.Max.x)
//                        worldWrapped = true;

//                    modifier = (int) Modifier.EastWest;
//                    break;

//                case CardinalDirection.South:
//                    if (node.BoundingBox.Min.y == worldBox.Min.y)
//                        worldWrapped = true;

//                    modifier = (int) Modifier.NorthSouth;
//                    break;
//                case CardinalDirection.North:
//                    if (node.BoundingBox.Max.y == worldBox.Max.y)
//                        worldWrapped = true;

//                    modifier = (int) Modifier.NorthSouth;
//                    break;

//                case CardinalDirection.Front:
//                    if (node.BoundingBox.Min.z == worldBox.Min.z)
//                        worldWrapped = true;

//                    modifier = (int) Modifier.BackFront;
//                    break;
//                case CardinalDirection.Back:
//                    if (node.BoundingBox.Max.z == worldBox.Max.z)
//                        worldWrapped = true;

//                    modifier = (int) Modifier.BackFront;
//                    break;
//            }
//        }


//        // This method provides a means of determining the common ancestor distance with just
//        // the current node and not needing to know the second.  This is good because it simplifies
//        // creation of the db when setting sibling id's is not dependant on those id's already existing in the db
//        // It works on the principle that neighbors at a given depth that are not true siblings (share the same parent)
//        // must share the first ancestor of the current node who's extents in the direction in question exceed
//        // the extents of the current node's parents extents on that same side.
//        private static int GetCommonAncestorDistance(OctreeNode node, CardinalDirection direction)
//        {
//            int distance = 0;
//            OctreeNode octreeParent = node.Parent;

//            if (node.Depth == 1) return 1; // all nodes at depth 1 share the same root node as parent

//            switch (node.Quadrant)
//            {
//                    // top left negative Z (front) node
//                case OctreeQuadrant.NX_Y_NZ:
//                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

//                            octreeParent = (OctreeNode) octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.South) // south neighbor shares same parent
//                        return 1;

//                    else if (direction == CardinalDirection.North) // north neighbor has different parent
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Back) // back neighbor shares same parent
//                        return 1;

//                    else if (direction == CardinalDirection.Front)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }
//                    break;

//                    // top right negative Z (front) node
//                case OctreeQuadrant.X_Y_NZ:
//                    if (direction == CardinalDirection.East) // east neighbor has differet parent 
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.West) // west neighbor has same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.South) // south neighbor has same parent
//                        return 1;

//                    else if (direction == CardinalDirection.North) // north neighbor has different parent
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Back) // back neighbor has same parent
//                        return 1;

//                    else if (direction == CardinalDirection.Front)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }
//                    break;

//                    // bottom left negative Z (front) node     
//                case OctreeQuadrant.NX_NY_NZ:
//                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.South) // south neighbor shares different parent
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.North) // north neighbor has same parent
//                        return 1;

//                    else if (direction == CardinalDirection.Back) // back shares same parent
//                        return 1;

//                    else if (direction == CardinalDirection.Front)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }
//                    break;

//                    // bottom right negative Z (front) node
//                case OctreeQuadrant.X_NY_NZ:
//                    if (direction == CardinalDirection.East)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.West) // west neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.South)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.North) // north neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.Back) // back neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.Front)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }
//                    break;

//                    // top left positive Z (back) node
//                case OctreeQuadrant.NX_Y_Z:
//                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.South) // south neighbor shares same parent
//                        return 1;

//                    else if (direction == CardinalDirection.North) // north neighbor has different parent
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Back)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Front) // back neighbor shares same parent
//                        return 1;

//                    break;

//                    // top right positive Z (back) node
//                case OctreeQuadrant.X_Y_Z:
//                    if (direction == CardinalDirection.East) // east neighbor has differet parent 
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.West) // west neighbor has same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.South) // south neighbor has same parent
//                        return 1;

//                    else if (direction == CardinalDirection.North) // north neighbor has different parent
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Back)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Front) // front neighbor has same parent
//                        return 1;

//                    break;

//                    // bottom left positive Z (back) node
//                case OctreeQuadrant.NX_NY_Z:
//                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.South) // south neighbor shares different parent
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.North) // north neighbor has same parent
//                        return 1;

//                    else if (direction == CardinalDirection.Back)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Front) // front shares same parent
//                        return 1;

//                    break;

//                    //bottom right positive Z (back) node
//                case OctreeQuadrant.X_NY_Z:
//                    if (direction == CardinalDirection.East)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.West) // west neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.South)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.North) // north neighbor shares same parent 
//                        return 1;

//                    else if (direction == CardinalDirection.Back)
//                        while (true)
//                        {
//                            distance++;
//                            if (octreeParent == null) return distance;
//                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

//                            octreeParent = octreeParent.Parent;
//                        }

//                    else if (direction == CardinalDirection.Front) // front neighbor shares same parent 
//                        return 1;
//                    break;
//            }

//            return distance;
//        }

//        //    vector<sint32> TrianglesOfMeshInRange(std::vector<sint32> indexes, TriangleMesh* triMesh, const Range3D& theRange)
//        //    {
//        //            std::vector<sint32> inRangeTris;

//        //            for (size_t idxIt = 0; idxIt < indexes.size(); ++idxIt)
//        //            {
//        //                    Vector3D min( INFINITY, INFINITY, INFINITY);
//        //                    Vector3D max(-INFINITY,-INFINITY,-INFINITY);
//        //                    sint32          currentTriIdx(indexes[idxIt]);
//        //                    Triangle*   currentTri(triMesh->TriangleAtIndex(currentTriIdx));
//        //                    for (int vertexIt = 0; vertexIt < 3; ++vertexIt)
//        //                    {
//        //                            sint32          currentVIdx(currentTri->vertices[vertexIt]);
//        //                            Vector3D        currentV(triMesh->VertexAtIndex(currentVIdx));
//        //                            min = MinVector3D(min, currentV);
//        //                            max = MaxVector3D(max, currentV);
//        //                    }
//        //                    Range3D currentR(min, max);
//        //                    Range3D commonR(currentR & theRange);
//        //                    if (commonR.IsEmpty())
//        //                            continue;
//        //                    inRangeTris.push_back(currentTriIdx);
//        //            }

//        //            return inRangeTris;
//        //    };

//        //    void    OctreeNode::SubDivideNode(TriangleMesh* triMesh, sint32 maxLevel)
//        //    {
//        //            if ((triangle_indices.size() <= 2) || !maxLevel)
//        //                    return;

//        //            Vector3D xyzV(node_range.min_v);
//        //            Vector3D XYZV(node_range.max_v);
//        //            Vector3D cccV((xyzV + XYZV)*0.5f);

//        //            Vector3D xccV(xyzV[0], cccV[1], cccV[2]);
//        //            Vector3D xczV(xyzV[0], cccV[1], xyzV[2]);
//        //    //      Vector3D xcZV(xyzV[0], cccV[1], XYZV[2]);
//        //            Vector3D xycV(xyzV[0], xyzV[1], cccV[2]);
//        //    //      Vector3D xyZV(xyzV[0], xyzV[1], XYZV[2]);
//        //    //      Vector3D xYcV(xyzV[0], XYZV[1], cccV[2]);
//        //    //      Vector3D xYzV(xyzV[0], XYZV[1], xyzV[2]);
//        //    //      Vector3D xYZV(xyzV[0], XYZV[1], XYZV[2]);

//        //         Vector3D XccV(XYZV[0], cccV[1], cccV[2]);
//        //    //      Vector3D XczV(XYZV[0], cccV[1], xyzV[2]);
//        //            Vector3D XcZV(XYZV[0], cccV[1], XYZV[2]);
//        //    //      Vector3D XycV(XYZV[0], xyzV[1], cccV[2]);
//        //    //      Vector3D XyzV(XYZV[0], xyzV[1], xyzV[2]);
//        //    //      Vector3D XyZV(XYZV[0], xyzV[1], XYZV[2]);
//        //            Vector3D XYcV(XYZV[0], XYZV[1], cccV[2]);
//        //    //      Vector3D XYzV(XYZV[0], XYZV[1], xyzV[2]);

//        //            Vector3D cycV(cccV[0], xyzV[1], cccV[2]);
//        //            Vector3D cyzV(cccV[0], xyzV[1], xyzV[2]);
//        //    //      Vector3D cyZV(cccV[0], xyzV[1], XYZV[2]);

//        //            Vector3D cYcV(cccV[0], XYZV[1], cccV[2]);
//        //    //      Vector3D cYzV(cccV[0], XYZV[1], xyzV[2]);
//        //            Vector3D cYZV(cccV[0], XYZV[1], XYZV[2]);

//        //            Vector3D cczV(cccV[0], cccV[1], xyzV[2]);
//        //            Vector3D ccZV(cccV[0], cccV[1], XYZV[2]);

//        //            OctreeNode* xyzN = new OctreeNode;
//        //            OctreeNode* xyZN = new OctreeNode;
//        //            OctreeNode* xYzN = new OctreeNode;
//        //            OctreeNode* xYZN = new OctreeNode;

//        //            OctreeNode* XyzN = new OctreeNode;
//        //            OctreeNode* XyZN = new OctreeNode;
//        //            OctreeNode* XYzN = new OctreeNode;
//        //            OctreeNode* XYZN = new OctreeNode;

//        //            // xyzV >(xyzN)> cccV >(XYZN)> XYZV
//        //            xyzN->SetNodeRange(Range3D(xyzV, cccV));
//        //            xyZN->SetNodeRange(Range3D(xycV, ccZV));
//        //            xYzN->SetNodeRange(Range3D(xczV, cYcV));
//        //            xYZN->SetNodeRange(Range3D(xccV, cYZV));

//        //            XyzN->SetNodeRange(Range3D(cyzV, XccV));
//        //            XyZN->SetNodeRange(Range3D(cycV, XcZV));
//        //            XYzN->SetNodeRange(Range3D(cczV, XYcV));
//        //            XYZN->SetNodeRange(Range3D(cccV, XYZV));

//        //            child_nodes.push_back(xyzN);
//        //            child_nodes.push_back(xyZN);
//        //            child_nodes.push_back(xYzN);
//        //            child_nodes.push_back(xYZN);
//        //            child_nodes.push_back(XyzN);
//        //            child_nodes.push_back(XyZN);
//        //            child_nodes.push_back(XYzN);
//        //            child_nodes.push_back(XYZN);

//        //            for (size_t i = 0; i < 8; ++i)
//        //                    child_nodes[i]->parent_node = this;

//        //            for (size_t i = 0; i < 8; ++i)
//        //                    child_nodes[i]->SetTriangleIndices(TrianglesOfMeshInRange(triangle_indices, triMesh, child_nodes[i]->node_range));

//        //            for (size_t i = 0; i < 8; ++i)
//        //                    child_nodes[i]->SubDivideNode(triMesh, maxLevel-1);

//        //    };


//        //    void    MeshOctree::CreateOctree(const sint32 treeDepth)
//        //    {
//        //            Vector3D min ( INFINITY, INFINITY, INFINITY);
//        //            Vector3D max (-INFINITY,-INFINITY,-INFINITY);
//        //            sint32 triMeshCount = tri_mesh->GetTriangleCount();
//        //            for (int i = 0; i < triMeshCount; ++i)
//        //            {
//        //                    Triangle* theTri = tri_mesh->TriangleAtIndex(i);
//        //                    for (int j = 0; j < 3; ++j)
//        //                    {
//        //                            sint32          vidx(theTri->vertices[j]);
//        //                            Vector3D        theV(tri_mesh->VertexAtIndex(vidx));
//        //                            min = MinVector3D(min, theV);
//        //                            max = MaxVector3D(max, theV);
//        //                    }
//        //            }

//        //            std::vector<sint32> allIndices;
//        //            for (sint32 i = 0; i < triMeshCount; ++i)
//        //                    allIndices.push_back(i);

//        //            root_node = new OctreeNode;
//        //            root_node->SetTriangleIndices(allIndices);
//        //            root_node->SetNodeRange(Range3D(min, max));
//        //            root_node->SubDivideNode(tri_mesh, treeDepth);
//        //    };

//        //    std::vector<OctreeNode*>        MeshOctree::FindIntersectingBoxes(MeshOctree* meshA, const TMatrix3D AM, MeshOctree* meshB, const TMatrix3D BM)
//        //    {
//        //            return OctreeNode::FindIntersectingBoxes(meshA->root_node, AM, meshB->root_node, BM);
//        //    };

//        //    bool    BoxBoxIntersects(const Vector3D* A, const Vector3D* B)
//        //    {
//        //            return BoundingBox.Intersects (todo:)
//        //        ;
//        //    };

//        //    std::vector<OctreeNode*>        OctreeNode::FindIntersectingBoxes(OctreeNode* AN, const TMatrix3D AM, OctreeNode* BN, const TMatrix3D BM)
//        //    {
//        //            std::vector<OctreeNode*>  pairs;

//        //            // no need to test empty boxes
//        //            if (AN->IsEmpty() || BN->IsEmpty())
//        //                    return pairs;

//        //            Vector3D Arad = (AN->node_range.max_v - AN->node_range.min_v)*0.5f;
//        //            Vector3D Brad = (BN->node_range.max_v - BN->node_range.min_v)*0.5f;


//        //            Vector3D        AV[4] = { Vector3D(AM[0])*Arad[0], Vector3D(AM[1])*Arad[1], Vector3D(AM[2])*Arad[1], (AN->node_range.min_v + AN->node_range.max_v)*0.5f};

//        //            Vector3D        BV[4] = { Vector3D(BM[0])*Brad[0], Vector3D(BM[1])*Brad[1], Vector3D(BM[2])*Brad[1], (BN->node_range.min_v + BN->node_range.max_v)*0.5f};

//        //            if (!BoxBoxIntersects(AV, BV))
//        //                    return pairs;

//        //            if (AN->IsLeafNode() && BN->IsLeafNode())
//        //            {
//        //                    pairs.push_back(AN);
//        //                    pairs.push_back(BN);
//        //            }
//        //            else if (AN->IsLeafNode())
//        //            {
//        //                    // only BN has child nodes
//        //                    for (int i = 0; i < 8; ++i)
//        //                    {
//        //                            std::vector<OctreeNode*> newPairs = FindIntersectingBoxes(AN, AM, BN->child_nodes[i], BM);
//        //                            for (size_t k = 0; k < newPairs.size(); ++k)
//        //                                    pairs.push_back(newPairs[k]);
//        //                    }
//        //            }
//        //            else if (BN->IsLeafNode())
//        //            {
//        //                    // only AN has child nodes
//        //                    for (int i = 0; i < 8; ++i)
//        //                    {
//        //                            std::vector<OctreeNode*> newPairs = FindIntersectingBoxes(AN->child_nodes[i], AM, BN, BM);
//        //                            for (size_t k = 0; k < newPairs.size(); ++k)
//        //                                    pairs.push_back(newPairs[k]);
//        //                    }
//        //            }
//        //            else
//        //            {
//        //                    // both boxes have child nodes
//        //                    for (int i = 0; i < 8; ++i)
//        //                            for (int j = 0; j < 8; ++j)
//        //                            {
//        //                                    std::vector<OctreeNode*> newPairs = FindIntersectingBoxes(AN->child_nodes[i], AM, BN->child_nodes[j], BM);
//        //                                    for (size_t k = 0; k < newPairs.size(); ++k)
//        //                                            pairs.push_back(newPairs[k]);
//        //                            }
//        //            }

//        //            return pairs;
//        //    }
//    }
//}

////using System;
////using System.Diagnostics;
////using Keystone.Elements;
////using Keystone.Traversers;
////using MTV3D65;

////namespace Keystone.Octree
////{
////    public enum OctreeQuadrant : int
////    {
////        ROOT = -1,
////        // these must be numbered 0 - 7 to match the array indices
////        NX_Y_NZ = 0,
////        X_Y_NZ = 1,
////        NX_NY_NZ = 2,
////        X_NY_NZ = 3,

////        NX_Y_Z = 4,
////        X_Y_Z = 5,
////        NX_NY_Z = 6,
////        X_NY_Z = 7
////    }

////    public enum CardinalDirection : int
////    {
////        East = 0,
////        West,
////        South,
////        North,
////        Front,
////        Back
////    }

////    public enum Modifier : int
////    {
////        EastWest = 1,
////        NorthSouth = 2,
////        BackFront = 4
////    }


////    /// <summary>
////    /// A dynamic + loose octree implementation. 
////    /// Dynamic = children are only added up to max depth to accomodate items being inserted into the tree.
////    /// This class implements ISector because we always want any depth octreenode to be link-able to/from a portal to another type of ISector.
////    /// </summary>
////    public class OctreeNode : SceneNode 
////    {

////        public static BoundingBox WorldBox;

////        private const int ROOT_DEPTH = 0; // the root node is depth 0.  The first set of 8 children are at depth 1, etc.

////        private BoundingBox _looseBox;
////                            // externally visible as our regular BoundingBox and is used to determine visibility

////        private BoundingBox _tightBox; // internally used to determine placement in the graph
////        private Vector3d _radius; //tight radius

////        private int _id;
////        private OctreeQuadrant _quadrant;

////        private int[] _neighbors;
////        private OctreeNode _octreeParent;
////        private OctreeNode[] _octreeChildren;


////        private static uint _maxDepth;
////        private uint _depth;

////        #region ITraversable Members
////        public override object Traverse(ITraverser target, object data)
////        {
////            return target.Apply(this, data);
////        }
////        #endregion

////        public OctreeNode (BoundingBox box, uint maxDepth) : this( box.Center, box.Width , box.Height , box.Depth , maxDepth )
////        {
////        }

////        public OctreeNode( Vector3d center, double width, double height, double depth, uint maxDepth)
////        {
////            if (maxDepth < 1) throw new ArgumentOutOfRangeException("MaxDepth must be >=1");
////            if (width <= 0 || height <= 0 || depth <= 0)
////                throw new ArgumentOutOfRangeException("Width, height and depth must be > 0.");
////            _maxDepth = maxDepth;
////            _depth = ROOT_DEPTH;
////            _octreeParent = null;
////            _radius = new Vector3d(width/2, height/2, depth/2);
////            _id = 0; //root is always 0
////            _quadrant = OctreeQuadrant.ROOT;
////            _tightBox = new BoundingBox(center - _radius, center + _radius);
////            _looseBox = _tightBox; // for the root node, these are the same
////            WorldBox = _looseBox;

////            //for a loose octree, the internal bounds becomes * RMOD as large
////            //as the _tightBox.
////            _boundVolumeDirty = true;
////        }

////        // This PRIVATE version of the constructor is called in the CreateSubNodes() function
////        // which itself is called in the Insert() method.
////        // Note the loosebox is computed automatically from the tightbox passed in.
////        private OctreeNode( OctreeNode parent, int id, OctreeQuadrant quadrant, BoundingBox tight)
////        {
////            if (parent == null) throw new ArgumentNullException();
////            _depth = parent.Depth + 1;
////            _octreeParent = parent;
////            _tightBox = tight;
////            _id = id;
////            _quadrant = quadrant;

////            _radius = parent.Radius*0.5F; // tight radius of child is always half the parent
////            Vector3d overlappedRadius = parent.Radius; // overlapped is twice the tight (aka: same as parent)

////            _looseBox = new BoundingBox(tight.Min - _radius, tight.Max + _radius);
////            _boundVolumeDirty = true;
////        }

////        ~OctreeNode()
////        {
////        }

////        public int ID
////        {
////            get { return _id; }
////        }

////        internal Vector3d Radius
////        {
////            get { return _radius; }
////        }

////        internal uint Depth
////        {
////            get { return _depth; }
////        }

////        public OctreeQuadrant Quadrant
////        {
////            get { return _quadrant; }
////        }

////        public OctreeNode[] OctreeChildren
////        {
////            get { return _octreeChildren ; }
////        }

////        public OctreeNode Parent
////        {
////            get { return _octreeParent; }
////        }

////        public override void AddChild(SceneNode sn)
////        {
////            Trace.WriteLine("Inserting scene node '" +  "'...");
////            //TODO: how to notify when element moves to determine if we need to move the element in the tree?
////            //TODO: what about removing elements?  notifications and movement?
////            //TODO: what about reclaiming (de-allocating) child nodes that are empty for a "long" time.
////            //      maybe on remove, if all children under a parent are now empty set a time and then
////            //      periodically in our update thread we can "compact" or "prune" traversal.
////            // TODO: use an insert traversal.
////            // TODO: I suppose as a sceneNode is "inserted" into the scene, the scene maintains the list of subscribers.
////            //       then as the sceneNode moves, it can fire a notify to the Scene its subscribed too.
////            //       Then if the sceneNode needs to move in the graph, the handler can do it easily by
////            //       (and can optimize for most cases by reverse recursive checking since odds are the element has just moved
////            // over to a sibling.

////            Vector3d childRadius = Radius * 0.5F;
////            BoundingBox box = sn.BoundingBox ;
////            Vector3d eRadius = new Vector3d(box.Width, box.Height, box.Depth) * .5f;

////            // will this node being added fit into one of the child nodes loose boxes based on it's radius and the child's Tight radius?
////            // The logic goes, if it will fit in the child's tight radius without considering position, then it will definetly fit 
////            // into one of the children's loosebox's when we do take into account the node to be inserted's positioning,
////            if (_depth < _maxDepth && V1LessThanEqualsV2(eRadius, childRadius))
////            {
////                // lazy instantiation of child nodes 
////                if (_octreeChildren == null)
////                {
////                    _octreeChildren  = CreateSubNodes(this);
////                }

////                foreach (OctreeNode subnode in _octreeChildren)
////                {
////                    //find which child to insert it into.  We only need to test the center because we already know if the center fits into the tight box
////                    // the whole box will fit into it's loose box
////                    if (subnode.TightBox.Contains(box.Center))
////                    {
////                        subnode.AddChild(sn);
////                        return;
////                    }
////                }

////                // ERROR!  We're still here and we shouldn't be.
////     //           Trace.Assert(false, string.Format("'{0}' should have fit into a child octree node but failed.", sn.Entity.Name));
////            }
////            else // wont fit in any of the child octree nodes so add it here
////            {
////                // TODO: i believe what i need to enforce is the following:
////                // 1 - OctreeHost is a regionNode of course and any SceneNode inserted under it should always have it as the parent to the child
////                //     sceneNode no matter where that child sceneNode is inserted into the octree.
////                // 2 - Thus, OctreeHost should have as it's list of "Children" all SceneNodes no matter where they are in the Octree.
////                // 3 - Thus, OctreeNode is not a SceneNode and is not in that Children list.
////                // 4 - RegionNode's can only be added under other RegionNodes and not under EntityNodes or regular SceneNodes.
////                // 5-  My original thinking for making OctreeNode a SceneNode was for traversal purposes since as you traverse any SceneNode
////                //     all Children are also SceneNodes so going from the OctreeHost to the OctreeNode's themsleves was no different.  "Children"
////                //     always contained "SceneNodes" this way and not an OctreeNode that was not itself a type of sceneNode where the meaning
////                //     of Children changed.
////                //     And with OctreeHost having a list of all "real" SceneNodes directly under it (EntityNodes, RegionNodes, SceneNodes)
////                //     then you'd have to special case the traversal to skip those and instead traverse the OctreeNode's but these  small
////                //     special cases is so minor and it doesnt outweigh the problems with having OctreeNode's as actual SceneNode children.


////                // we now must determine if there are any
////                // child region nodes it may fit in as well!  This is the complicated aspect of not directly adding a child to its proper
////                // region parent.  The reason is we cant just itterate through each child to see if it's a region node because I dont think
////                // regionNode's can only exist under another regionNode.  Clearly if this octreeNode is just a SceneNode and only the OctreeHost
////                // is a RegionNode, then in a way, any child SceneNode added to an OctreeNode is really a child of the OctreeHost.
////                foreach (SceneNode node in _children)
////                {

////                }
////     //           Trace.WriteLine(string.Format("'{0}' added to octree at depth {1}", sn.Entity.Name, _depth));
////    //            Trace.Assert(_looseBox.Contains(box),
////     //                        string.Format("'{0}' should fit, but does not.  This should never happen!", sn.Entity.Name));

////                base.AddChild(sn);
////            }
////        }


////        #region IGroup Members
////        public void NotifyChange(Node child)
////        {
////            //there's nothing really for this to do is there? What we want
////            // is for the child to notify the Scene as an FXProvider.  Or maybe
////            // instead of FXProvider we provide some other sets of events for scene nodes
////            // to callback to the SceneManager
////            // OnTranslation
////            // OnScale
////            // OnRotation
////            // OnCollision?
////            // 
////        }
////        #endregion

////        #region IBoundVolume Members

////        internal BoundingBox TightBox
////        {
////            get { return _tightBox; }
////        }

////        public override BoundingBox BoundingBox
////        {
////            get
////            {
////                if (_boundVolumeDirty)
////                    UpdateBoundVolume();

////                return _looseBox;
////            }
////        }

////        public override BoundingSphere BoundingSphere
////        {
////            get
////            {
////                if (_boundVolumeDirty)
////                    UpdateBoundVolume();

////                return _sphere;
////            }
////        }


////        public override bool BoundVolumeIsDirty
////        {
////            set {} // octree bounds are fixed.
////        }

////        public override void UpdateBoundVolume()
////        {
////            // nothing to do here 
////            _sphere = new BoundingSphere(_looseBox);
////            _boundVolumeDirty = false;
////        }

////        #endregion


////        private bool V1LessThanEqualsV2(Vector3d v1, Vector3d v2)
////        {
////            return (v1.x <= v2.x && v1.y <= v2.y && v1.z <= v2.z);
////        }

////        private static OctreeNode[] CreateSubNodes(OctreeNode parentOctreeNode)
////        {
////            BoundingBox box = parentOctreeNode.TightBox;
////            double minX = box.Min.x;
////            double minY = box.Min.y;
////            double minZ = box.Min.z;

////            double maxX = box.Max.x;
////            double maxY = box.Max.y;
////            double maxZ = box.Max.z;

////            double midX = (minX + maxX)/2f;
////            double midY = (minY + maxY)/2f;
////            double midZ = (minZ + maxZ)/2f;

////            OctreeNode[] subNodes = new OctreeNode[8];

////            Vector3d min;
////            Vector3d max;

////            // given a node's ID, we can compute/find the id's of its children as being
////            // childID's 1 thru 8 = (parentOctreeNode'sID * 8) + 1 thru 8 respectively.
////            // we actually use (parentOctreeNode'sID * 8) + 1 + 0 thru 7 because our OctreeQuadrant enum starts at 0 for first non root quadrant type 
////            int id;

////            min = new Vector3d(minX, minY, minZ);
////            max = new Vector3d(midX, midY, midZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_Y_NZ;
////            subNodes[(int) OctreeQuadrant.NX_Y_NZ] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_Y_NZ,
////                               new BoundingBox(min, max));

////            min = new Vector3d(minX, minY, midZ);
////            max = new Vector3d(midX, midY, maxZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_Y_NZ;
////            subNodes[(int) OctreeQuadrant.X_Y_NZ] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_Y_NZ,
////                               new BoundingBox(min, max));

////            min = new Vector3d(minX, midY, minZ);
////            max = new Vector3d(midX, maxY, midZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_NY_NZ;
////            subNodes[(int) OctreeQuadrant.NX_NY_NZ] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_NY_NZ,
////                               new BoundingBox(min, max));

////            min = new Vector3d(minX, midY, midZ);
////            max = new Vector3d(midX, maxY, maxZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_NY_NZ;
////            subNodes[(int) OctreeQuadrant.X_NY_NZ] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_NY_NZ,
////                               new BoundingBox(min, max));

////            min = new Vector3d(midX, minY, minZ);
////            max = new Vector3d(maxX, midY, midZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_Y_Z;
////            subNodes[(int) OctreeQuadrant.NX_Y_Z] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_Y_Z,
////                               new BoundingBox(min, max));

////            min = new Vector3d(midX, minY, midZ);
////            max = new Vector3d(maxX, midY, maxZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_Y_Z;
////            subNodes[(int) OctreeQuadrant.X_Y_Z] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_Y_Z,
////                               new BoundingBox(min, max));

////            min = new Vector3d(midX, midY, minZ);
////            max = new Vector3d(maxX, maxY, midZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.NX_NY_Z;
////            subNodes[(int) OctreeQuadrant.NX_NY_Z] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.NX_NY_Z,
////                               new BoundingBox(min, max));

////            min = new Vector3d(midX, midY, midZ);
////            max = new Vector3d(maxX, maxY, maxZ);
////            id = (parentOctreeNode.ID*8) + 1 + (int) OctreeQuadrant.X_NY_Z;
////            subNodes[(int) OctreeQuadrant.X_NY_Z] =
////                new OctreeNode(parentOctreeNode, id, OctreeQuadrant.X_NY_Z,
////                               new BoundingBox(min, max));

////            return subNodes;
////        }

////        // I've decided that based on the world size and max depth, all nodes will be added to the DB upon creation and at once.
////        // THat is to say, we wont stop prior to max depth just because no scene elements exist along that path.
////        // Instead, we will add a "childrenEmpty" flag on all parent nodes so that we can stop retrieval or processing
////        // of a parent's children if those children have no elements.


////        //So let's assume we want to dynamically find siblings without storing them anywhere, 
////        //this is used primarily for paging.  

////        //- a node either has children or it does not. It cant just have 1 or 2 or 5.  It's always 8.
////        //- if a sparse node that has no children at max depth because of the sparseness of the area,
////        // and you want to find which nodes to page in (and under this scenario it could 
////        //very well require we find neighbors and then go at lower depths than were we started 
////        //the search) what do we do?

////        //   1) get the current node which is the lowest depth node containing the camera.
////        //   2) cache that node and all nodes leading to it.
////        //   3) depending on the draw range we need to determine the id's of all concentric neighbors 
////        //from this one out to the max distance.  We have a formula that can find node id's 
////        //for any node that neighbors another and will account for world wrap and boundaries.

////        //    a) this could very well mean then that our database is not a dynamic octree but 
////        //rather it has all the child nodes (even the empty ones) but that we will flag
////        //whether any node in the database has "elements" and then recursively set all 
////        //parent's flags to whether any of their children has elements or not so that 
////        //when we retreive nodes during a query we can (maybe with a store procedure?) skip
////        //the retrieval of those that have no elements down its hierarcy.
////        //   4) during paging, the work items are added hierarchally but also perhaps in level order 
////        //so that all nodes of 1 depth are loaded first and then all of their children 
////        //and then all of their children's children, etc.
////        //        a) so perhaps just load all the nodes we've computed as needing and then sort them 
////        //after retrieval or maybe the db can sort by the depth field of a node.

////        //-  So i think with the above, the key points is that we are building an overlapping grid at the lowest level. 
////        //-  Our database will contain alot of records but that this shouldnt be a problem.  We will test this first thing
////        // and will only try to come up with other solutions _if_ it becomes way too slow or takes too much space.

////        //Possibly, even by generating these elements in advance and enforcing that all paths lead to the max depth 
////        //node, we _can_ still enforce which nodes any particular scene element is added to... be it a end 
////        //child node or a parent node.  In fact, this _must_ be the case because i now recall that some large 
////        //scene elements will only fully fit in bigger nodes.  So in our editor, when we are adding/removing 
////        //scene elemnts each element will have a NodeID tag to indicate where it exists.  When it moves, 
////        //we can compute and update the db if necessary. And when adding/removing elements, we can determine
////        //if we want to push some sparse entries down to childen (if they will fit) or remove them from children 
////        //and push them up to the parent and flag the parent as all children no longer having any elements...  

////        //So i think thats our path we must take and we need to produce the list of the queryies and routines we need to 
////        //handle these situations... how much can be done in the query versus, versus stored proc, or a method.  

////        //One big question is that since parents can contain scene elements, then whenever we need to page the world,
////        //we cant simply compute concentrically the end nodes, we must also retrieve all the parents of 
////        //those nodes but with no duplicates.  It's probably best to do this in an algorithm so that we 
////        //compute all the indices we need and then just retrieve all those at once... probably our 
////        //algorithm can compute the list in the "level" order we want too.
////        // TODO: ActiveNodes is only used if we pre-generate the entire Octree but 
////        // if we have multiple Octree's in seperate Graphs(Zones) then this ActiveNodes must be cleared before building 
////        //public static Dictionary<int, OctreeNode> ActiveNodes = new Dictionary<int, OctreeNode>();
////        //public static void GenerateNodes(OctreeNode parentOctreeNode, uint maxDepth)
////        //{
////        //    if (parentOctreeNode.Depth < maxDepth)
////        //    {
////        //        parentOctreeNode._octreeChildren = CreateSubNodes(parentOctreeNode);

////        //        if (parentOctreeNode._octreeChildren == null) throw new Exception();
////        //        //node._children = new OctreeNode[8];

////        //        for (int i = 0; i < 8; i++)
////        //        {       //node._children[i] = children[i];
////        //            parentOctreeNode._octreeChildren[i]._octreeParent = parentOctreeNode;
////        //            ActiveNodes.Add(parentOctreeNode._octreeChildren[i].ID, parentOctreeNode._octreeChildren[i]);
////        //        }

////        //        foreach (OctreeNode node in parentOctreeNode._octreeChildren)
////        //        {
////        //            GenerateNodes(node, maxDepth); // recurse using the child's tightbox
////        //        }
////        //    }
////        //}

////        ////$#$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
////        ////$$$$$
////        //// IMPORTANT:  As it turns out, GenerateNodes, SaveNodes, LoadNodes, AssignNeibhgors, CardinalDirection and all the rest below this line
////        //// could just be added to our Pager object instead because we will not be assigning neighbors permanently but will try to just
////        //// compute them at runtime based on the current node's index.
////        ////
////        //public static void SaveNodes()
////        //{
////        //    // the above generates the basic tree.
////        //    // Now we can flush the entire ActiveNodes to the db and then start paging
////        //    // TODO: save all ActiveNodes to the db.

////        //    // Below, we compute the neighbor ID's for the Cardinal direction neighbors.
////        //    // In theory we dont even have to maintain sibling ID's, we can compute them for any given
////        //    // node on the fly.  But for now at least, we will compute them and store them all in one go
////        //    // TODO: add code to also compute any of the other non cardinal direction adjacent nodes. (only needed for depth > 1)
////        //    // WARNING: Actually by at least assigning the 6 cardinal neighbor ID's we vastly simply the process of finding the
////        //    // outer concentric layers around a given node because we can simply travel along the 6 cardinal neighbors
////        //    // easily.  Although we could compute each cardinal direction each time on the fly.. we'll try that first
////        //    // since it doesnt require us adding anything to the OctreeNode or the database
////        //    foreach (OctreeNode currentNode in ActiveNodes.Values)
////        //    {
////        //        AssignNeighbors(currentNode);
////        //    }
////        //}

////        private static void AssignNeighbors(OctreeNode node)
////        {
////            node._neighbors = new int[6];

////            // given a node's, we can find all 6 of it's CARDINAL direction neighbors as follows
////            // based on the current node's quadrant ID we use the appropriate DirX,Y,Z constant.
////            // for depth 1, we just use these values direcly.  For depths > 1 we use the following formula
////            for (int i = 0; i < 6; i++)
////                node._neighbors[i] = GetCardinalNeighborIndex(node, (CardinalDirection) i);
////        }

////        public static int GetCardinalNeighborIndex(OctreeNode node, CardinalDirection dir)
////        {
////            // for each neighbor, determine the distance to the common ancestor.  
////            int ancestorDistance = GetCommonAncestorDistance(node, dir);
////            int modifier = 0;
////            bool worldWrapped = false;
////            NeighborIndex(node, dir, WorldBox, ref modifier, ref worldWrapped);
////            // modifier is computed based on value of dir
////            int neighborOffset = modifier;

////            if (ancestorDistance == 1)
////            {
////                // Children that share same parent are at distance 1 and use the resulting modifier directly
////                // NOTE: worldWrapped usually has no effect because by definition, if its worldwrapped they do not share the same parent
////                // UNLESS they are depth = 1 and share the root node as parent
////                if (!worldWrapped)
////                    neighborOffset -= modifier;
////                else
////                    // however if world wrapping, to find the neighbor on the other side of the world
////                    // the formula changes by making the subtraction sign into a positive sign as
////                    neighborOffset += modifier;
////            }
////            else
////            {
////                // children that share a same grandparent are at minimum ancestorDistance of 2 and use the formula
////                for (int j = 2; j < ancestorDistance + 1; j++) // starting at depth 2
////                {
////                    if (!worldWrapped)
////                        neighborOffset = (neighborOffset*8) - modifier;
////                    else
////                        // however if world wrapping, to find the neighbor on the other side of the world
////                        // the formula changes by making the subtraction sign into a positive sign as
////                        neighborOffset = (neighborOffset*8) + modifier;
////                }
////            }
////            return node._id + neighborOffset;
////        }

////        private static void NeighborIndex(OctreeNode node, CardinalDirection direction, BoundingBox worldBox,
////                                          ref int modifier, ref bool worldWrapped)
////        {
////            // determine if the neighbor in the specified direction requires us to 
////            // world wrap if worldWrap is enabled.
////            switch (direction)
////            {
////                case CardinalDirection.West:
////                    if (node.BoundingBox.Min.x == worldBox.Min.x)
////                        worldWrapped = true;

////                    modifier = (int) Modifier.EastWest;
////                    break;
////                case CardinalDirection.East:
////                    if (node.BoundingBox.Max.x == worldBox.Max.x)
////                        worldWrapped = true;

////                    modifier = (int) Modifier.EastWest;
////                    break;

////                case CardinalDirection.South:
////                    if (node.BoundingBox.Min.y == worldBox.Min.y)
////                        worldWrapped = true;

////                    modifier = (int) Modifier.NorthSouth;
////                    break;
////                case CardinalDirection.North:
////                    if (node.BoundingBox.Max.y == worldBox.Max.y)
////                        worldWrapped = true;

////                    modifier = (int) Modifier.NorthSouth;
////                    break;

////                case CardinalDirection.Front:
////                    if (node.BoundingBox.Min.z == worldBox.Min.z)
////                        worldWrapped = true;

////                    modifier = (int) Modifier.BackFront;
////                    break;
////                case CardinalDirection.Back:
////                    if (node.BoundingBox.Max.z == worldBox.Max.z)
////                        worldWrapped = true;

////                    modifier = (int) Modifier.BackFront;
////                    break;
////            }
////        }


////        // This method provides a means of determining the common ancestor distance with just
////        // the current node and not needing to know the second.  This is good because it simplifies
////        // creation of the db when setting sibling id's is not dependant on those id's already existing in the db
////        // It works on the principle that neighbors at a given depth that are not true siblings (share the same parent)
////        // must share the first ancestor of the current node who's extents in the direction in question exceed
////        // the extents of the current node's parents extents on that same side.
////        private static int GetCommonAncestorDistance(OctreeNode node, CardinalDirection direction)
////        {
////            int distance = 0;
////            OctreeNode octreeParent = node.Parent;

////            if (node.Depth == 1) return 1; // all nodes at depth 1 share the same root node as parent

////            switch (node.Quadrant)
////            {
////                    // top left negative Z (front) node
////                case OctreeQuadrant.NX_Y_NZ:
////                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

////                            octreeParent = (OctreeNode)octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.South) // south neighbor shares same parent
////                        return 1;

////                    else if (direction == CardinalDirection.North) // north neighbor has different parent
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Back) // back neighbor shares same parent
////                        return 1;

////                    else if (direction == CardinalDirection.Front)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }
////                    break;

////                    // top right negative Z (front) node
////                case OctreeQuadrant.X_Y_NZ:
////                    if (direction == CardinalDirection.East) // east neighbor has differet parent 
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.West) // west neighbor has same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.South) // south neighbor has same parent
////                        return 1;

////                    else if (direction == CardinalDirection.North) // north neighbor has different parent
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Back) // back neighbor has same parent
////                        return 1;

////                    else if (direction == CardinalDirection.Front)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }
////                    break;

////                    // bottom left negative Z (front) node     
////                case OctreeQuadrant.NX_NY_NZ:
////                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.South) // south neighbor shares different parent
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.North) // north neighbor has same parent
////                        return 1;

////                    else if (direction == CardinalDirection.Back) // back shares same parent
////                        return 1;

////                    else if (direction == CardinalDirection.Front)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }
////                    break;

////                    // bottom right negative Z (front) node
////                case OctreeQuadrant.X_NY_NZ:
////                    if (direction == CardinalDirection.East)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.West) // west neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.South)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.North) // north neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.Back) // back neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.Front)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.z != octreeParent.BoundingBox.Min.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }
////                    break;

////                    // top left positive Z (back) node
////                case OctreeQuadrant.NX_Y_Z:
////                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.South) // south neighbor shares same parent
////                        return 1;

////                    else if (direction == CardinalDirection.North) // north neighbor has different parent
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Back)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Front) // back neighbor shares same parent
////                        return 1;

////                    break;

////                    // top right positive Z (back) node
////                case OctreeQuadrant.X_Y_Z:
////                    if (direction == CardinalDirection.East) // east neighbor has differet parent 
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.West) // west neighbor has same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.South) // south neighbor has same parent
////                        return 1;

////                    else if (direction == CardinalDirection.North) // north neighbor has different parent
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.y != octreeParent.BoundingBox.Max.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Back)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Front) // front neighbor has same parent
////                        return 1;

////                    break;

////                    // bottom left positive Z (back) node
////                case OctreeQuadrant.NX_NY_Z:
////                    if (direction == CardinalDirection.East) // east neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.West) // west neighbor has different parent 
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.x != octreeParent.BoundingBox.Min.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.South) // south neighbor shares different parent
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.North) // north neighbor has same parent
////                        return 1;

////                    else if (direction == CardinalDirection.Back)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Front) // front shares same parent
////                        return 1;

////                    break;

////                    //bottom right positive Z (back) node
////                case OctreeQuadrant.X_NY_Z:
////                    if (direction == CardinalDirection.East)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.x != octreeParent.BoundingBox.Max.x) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.West) // west neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.South)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Min.y != octreeParent.BoundingBox.Min.y) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.North) // north neighbor shares same parent 
////                        return 1;

////                    else if (direction == CardinalDirection.Back)
////                        while (true)
////                        {
////                            distance++;
////                            if (octreeParent == null) return distance;
////                            if (node.BoundingBox.Max.z != octreeParent.BoundingBox.Max.z) return distance;

////                            octreeParent = octreeParent.Parent;
////                        }

////                    else if (direction == CardinalDirection.Front) // front neighbor shares same parent 
////                        return 1;
////                    break;
////            }

////            return distance;
////        }

//////    vector<sint32> TrianglesOfMeshInRange(std::vector<sint32> indexes, TriangleMesh* triMesh, const Range3D& theRange)
//////    {
//////            std::vector<sint32> inRangeTris;

//////            for (size_t idxIt = 0; idxIt < indexes.size(); ++idxIt)
//////            {
//////                    Vector3D min( INFINITY, INFINITY, INFINITY);
//////                    Vector3D max(-INFINITY,-INFINITY,-INFINITY);
//////                    sint32          currentTriIdx(indexes[idxIt]);
//////                    Triangle*   currentTri(triMesh->TriangleAtIndex(currentTriIdx));
//////                    for (int vertexIt = 0; vertexIt < 3; ++vertexIt)
//////                    {
//////                            sint32          currentVIdx(currentTri->vertices[vertexIt]);
//////                            Vector3D        currentV(triMesh->VertexAtIndex(currentVIdx));
//////                            min = MinVector3D(min, currentV);
//////                            max = MaxVector3D(max, currentV);
//////                    }
//////                    Range3D currentR(min, max);
//////                    Range3D commonR(currentR & theRange);
//////                    if (commonR.IsEmpty())
//////                            continue;
//////                    inRangeTris.push_back(currentTriIdx);
//////            }

//////            return inRangeTris;
//////    };

//////    void    OctreeNode::SubDivideNode(TriangleMesh* triMesh, sint32 maxLevel)
//////    {
//////            if ((triangle_indices.size() <= 2) || !maxLevel)
//////                    return;

//////            Vector3D xyzV(node_range.min_v);
//////            Vector3D XYZV(node_range.max_v);
//////            Vector3D cccV((xyzV + XYZV)*0.5f);

//////            Vector3D xccV(xyzV[0], cccV[1], cccV[2]);
//////            Vector3D xczV(xyzV[0], cccV[1], xyzV[2]);
//////    //      Vector3D xcZV(xyzV[0], cccV[1], XYZV[2]);
//////            Vector3D xycV(xyzV[0], xyzV[1], cccV[2]);
//////    //      Vector3D xyZV(xyzV[0], xyzV[1], XYZV[2]);
//////    //      Vector3D xYcV(xyzV[0], XYZV[1], cccV[2]);
//////    //      Vector3D xYzV(xyzV[0], XYZV[1], xyzV[2]);
//////    //      Vector3D xYZV(xyzV[0], XYZV[1], XYZV[2]);

//////         Vector3D XccV(XYZV[0], cccV[1], cccV[2]);
//////    //      Vector3D XczV(XYZV[0], cccV[1], xyzV[2]);
//////            Vector3D XcZV(XYZV[0], cccV[1], XYZV[2]);
//////    //      Vector3D XycV(XYZV[0], xyzV[1], cccV[2]);
//////    //      Vector3D XyzV(XYZV[0], xyzV[1], xyzV[2]);
//////    //      Vector3D XyZV(XYZV[0], xyzV[1], XYZV[2]);
//////            Vector3D XYcV(XYZV[0], XYZV[1], cccV[2]);
//////    //      Vector3D XYzV(XYZV[0], XYZV[1], xyzV[2]);

//////            Vector3D cycV(cccV[0], xyzV[1], cccV[2]);
//////            Vector3D cyzV(cccV[0], xyzV[1], xyzV[2]);
//////    //      Vector3D cyZV(cccV[0], xyzV[1], XYZV[2]);

//////            Vector3D cYcV(cccV[0], XYZV[1], cccV[2]);
//////    //      Vector3D cYzV(cccV[0], XYZV[1], xyzV[2]);
//////            Vector3D cYZV(cccV[0], XYZV[1], XYZV[2]);

//////            Vector3D cczV(cccV[0], cccV[1], xyzV[2]);
//////            Vector3D ccZV(cccV[0], cccV[1], XYZV[2]);

//////            OctreeNode* xyzN = new OctreeNode;
//////            OctreeNode* xyZN = new OctreeNode;
//////            OctreeNode* xYzN = new OctreeNode;
//////            OctreeNode* xYZN = new OctreeNode;

//////            OctreeNode* XyzN = new OctreeNode;
//////            OctreeNode* XyZN = new OctreeNode;
//////            OctreeNode* XYzN = new OctreeNode;
//////            OctreeNode* XYZN = new OctreeNode;

//////            // xyzV >(xyzN)> cccV >(XYZN)> XYZV
//////            xyzN->SetNodeRange(Range3D(xyzV, cccV));
//////            xyZN->SetNodeRange(Range3D(xycV, ccZV));
//////            xYzN->SetNodeRange(Range3D(xczV, cYcV));
//////            xYZN->SetNodeRange(Range3D(xccV, cYZV));

//////            XyzN->SetNodeRange(Range3D(cyzV, XccV));
//////            XyZN->SetNodeRange(Range3D(cycV, XcZV));
//////            XYzN->SetNodeRange(Range3D(cczV, XYcV));
//////            XYZN->SetNodeRange(Range3D(cccV, XYZV));

//////            child_nodes.push_back(xyzN);
//////            child_nodes.push_back(xyZN);
//////            child_nodes.push_back(xYzN);
//////            child_nodes.push_back(xYZN);
//////            child_nodes.push_back(XyzN);
//////            child_nodes.push_back(XyZN);
//////            child_nodes.push_back(XYzN);
//////            child_nodes.push_back(XYZN);

//////            for (size_t i = 0; i < 8; ++i)
//////                    child_nodes[i]->parent_node = this;

//////            for (size_t i = 0; i < 8; ++i)
//////                    child_nodes[i]->SetTriangleIndices(TrianglesOfMeshInRange(triangle_indices, triMesh, child_nodes[i]->node_range));

//////            for (size_t i = 0; i < 8; ++i)
//////                    child_nodes[i]->SubDivideNode(triMesh, maxLevel-1);

//////    };


//////    void    MeshOctree::CreateOctree(const sint32 treeDepth)
//////    {
//////            Vector3D min ( INFINITY, INFINITY, INFINITY);
//////            Vector3D max (-INFINITY,-INFINITY,-INFINITY);
//////            sint32 triMeshCount = tri_mesh->GetTriangleCount();
//////            for (int i = 0; i < triMeshCount; ++i)
//////            {
//////                    Triangle* theTri = tri_mesh->TriangleAtIndex(i);
//////                    for (int j = 0; j < 3; ++j)
//////                    {
//////                            sint32          vidx(theTri->vertices[j]);
//////                            Vector3D        theV(tri_mesh->VertexAtIndex(vidx));
//////                            min = MinVector3D(min, theV);
//////                            max = MaxVector3D(max, theV);
//////                    }
//////            }

//////            std::vector<sint32> allIndices;
//////            for (sint32 i = 0; i < triMeshCount; ++i)
//////                    allIndices.push_back(i);

//////            root_node = new OctreeNode;
//////            root_node->SetTriangleIndices(allIndices);
//////            root_node->SetNodeRange(Range3D(min, max));
//////            root_node->SubDivideNode(tri_mesh, treeDepth);
//////    };

//////    std::vector<OctreeNode*>        MeshOctree::FindIntersectingBoxes(MeshOctree* meshA, const TMatrix3D AM, MeshOctree* meshB, const TMatrix3D BM)
//////    {
//////            return OctreeNode::FindIntersectingBoxes(meshA->root_node, AM, meshB->root_node, BM);
//////    };

//////    bool    BoxBoxIntersects(const Vector3D* A, const Vector3D* B)
//////    {
//////            return BoundingBox.Intersects (todo:)
//////        ;
//////    };

//////    std::vector<OctreeNode*>        OctreeNode::FindIntersectingBoxes(OctreeNode* AN, const TMatrix3D AM, OctreeNode* BN, const TMatrix3D BM)
//////    {
//////            std::vector<OctreeNode*>  pairs;

//////            // no need to test empty boxes
//////            if (AN->IsEmpty() || BN->IsEmpty())
//////                    return pairs;

//////            Vector3D Arad = (AN->node_range.max_v - AN->node_range.min_v)*0.5f;
//////            Vector3D Brad = (BN->node_range.max_v - BN->node_range.min_v)*0.5f;


//////            Vector3D        AV[4] = { Vector3D(AM[0])*Arad[0], Vector3D(AM[1])*Arad[1], Vector3D(AM[2])*Arad[1], (AN->node_range.min_v + AN->node_range.max_v)*0.5f};

//////            Vector3D        BV[4] = { Vector3D(BM[0])*Brad[0], Vector3D(BM[1])*Brad[1], Vector3D(BM[2])*Brad[1], (BN->node_range.min_v + BN->node_range.max_v)*0.5f};

//////            if (!BoxBoxIntersects(AV, BV))
//////                    return pairs;

//////            if (AN->IsLeafNode() && BN->IsLeafNode())
//////            {
//////                    pairs.push_back(AN);
//////                    pairs.push_back(BN);
//////            }
//////            else if (AN->IsLeafNode())
//////            {
//////                    // only BN has child nodes
//////                    for (int i = 0; i < 8; ++i)
//////                    {
//////                            std::vector<OctreeNode*> newPairs = FindIntersectingBoxes(AN, AM, BN->child_nodes[i], BM);
//////                            for (size_t k = 0; k < newPairs.size(); ++k)
//////                                    pairs.push_back(newPairs[k]);
//////                    }
//////            }
//////            else if (BN->IsLeafNode())
//////            {
//////                    // only AN has child nodes
//////                    for (int i = 0; i < 8; ++i)
//////                    {
//////                            std::vector<OctreeNode*> newPairs = FindIntersectingBoxes(AN->child_nodes[i], AM, BN, BM);
//////                            for (size_t k = 0; k < newPairs.size(); ++k)
//////                                    pairs.push_back(newPairs[k]);
//////                    }
//////            }
//////            else
//////            {
//////                    // both boxes have child nodes
//////                    for (int i = 0; i < 8; ++i)
//////                            for (int j = 0; j < 8; ++j)
//////                            {
//////                                    std::vector<OctreeNode*> newPairs = FindIntersectingBoxes(AN->child_nodes[i], AM, BN->child_nodes[j], BM);
//////                                    for (size_t k = 0; k < newPairs.size(); ++k)
//////                                            pairs.push_back(newPairs[k]);
//////                            }
//////            }

//////            return pairs;
//////    }
////    }
////}