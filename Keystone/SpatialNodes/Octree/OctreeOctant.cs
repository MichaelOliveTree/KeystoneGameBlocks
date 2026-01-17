using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Portals;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.SpatialNodes;

namespace Keystone.SpatialNodes.Octree
{

    // TODO: Fast Ray / Octree intersection traversal
    // // TODO: // TODO: https://daeken.dev/a-stupidly-simple-fast-octree-traversal-for-ray-intersection

    // TODO: https://discussions.unity.com/t/free-nativetrees-burst-compatible-generic-octree-and-quadtree/901144

    // http://www.flipcode.com/archives/Octree_Implementation.shtml
    //  - COTD Entry submitted by Paul Nettle [midnight@FluidStudios.com]
    //  - Corresponds with an Ask MidNight response (http://www.flipcode.com/askmid/)
    /// <summary>
    /// </summary>
    public class OctreeOctant : ISpatialNode, ITraversable, IBoundVolume
    {

        #region Static variables

        public uint MaxDepth;
        public uint SplitThreshHold;

        private static Vector3d[] BoundsOffsetTable = new Vector3d[]
        {
                new Vector3d(-0.5, -0.5, -0.5),
                new Vector3d(+0.5, -0.5, -0.5),
                new Vector3d(-0.5, +0.5, -0.5),
                new Vector3d(+0.5, +0.5, -0.5),
                new Vector3d(-0.5, -0.5, +0.5),
                new Vector3d(+0.5, -0.5, +0.5),
                new Vector3d(-0.5, +0.5, +0.5),
                new Vector3d(+0.5, +0.5, +0.5)
        };

        #endregion

        private int _depth;
        private int _index;   // index is specific to each depth and contains x,y,z offset at that depth and is useful for finding neighbors (which we may never do and just always move EntityNodes by re-inserting starting at root)
        private const int MAX_CHILD_COUNT = 8;

        private BoundingBox mBox;
        private OctreeOctant mParent;
        private OctreeOctant[] mChildOctants;

        // TODO: switch to linked list?
        private List<EntityNode> mEntityNodesCollection;


        public OctreeOctant(int index, int depth, BoundingBox box, OctreeOctant parent)
            : this()
        {
            _index = index;
            _depth = depth;
            mBox = box;
            mParent = parent;
            //System.Diagnostics.Debug.WriteLine("OctreeOctant() -- Created at index " + index.ToString());
        }

        public OctreeOctant()
        {
            Visible = true;
        }

        ~OctreeOctant()
        {
        }

        
        #region ITraversable Members
        public object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion
        

        private bool IsRoot { get { return mParent == null; } }

        private OctreeOctant Parent { get { return mParent; } set { mParent = value; } }

        public bool IsLeaf { get { return mChildOctants == null; } }

        public int Index
        {
            get { return _index; }
        }

        internal int[] LocalIndexToVector(int index)
        {

            // divide the index by  2 ^ depth
            // 

            int[] v = new int[3];
            if ((index & 1) > 0) v[0] = 1;
            else v[0] = -1;

            if ((index & 2) > 0) v[1] = 1;
            else v[1] = -1;

            if ((index & 4) > 0) v[2] = 1;
            else v[2] = -1;

            return v;
        }

        internal int LocalVectorToIndex(int[] v)
        {
            int index = 0;

            if (v[0] >= 0) index |= 1;
            if (v[1] >= 0) index |= 2;
            if (v[2] >= 0) index |= 4;

            return index;
        }

        internal Vector3d Radius
        {
            get
            {
                
				Vector3d radius;
                radius.x = mBox.Width * 0.5d;
                radius.y = mBox.Height * 0.5d;
                radius.z = mBox.Depth* 0.5d;
                return radius;
            }
        }

        internal int Depth
        {
            get { return _depth; }
        }

        public ISpatialNode[] Children
        {
            get { return mChildOctants; }
        }

        #region ISpatialNode
        public bool Visible { get; set; }

        public EntityNode[] EntityNodes
        {
            get
            {
                if (mEntityNodesCollection == null) return null;
                return mEntityNodesCollection.ToArray();
            }
        }

        public void Add(EntityNode entityNode, bool forceRoot)
        {
            if (forceRoot)
            {
                this.AddEntityNodeToCollection((EntityNode)entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - "  + entityNode.Entity.TypeName + " Forced into Root");
            }
            else
            {
                this.Add(entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName);
            }
        }

        private void AddEntityNodeToCollection(EntityNode entityNode)
        {
            if (mEntityNodesCollection == null)
                mEntityNodesCollection = new List<EntityNode>();

            entityNode.SpatialNode = this;
            mEntityNodesCollection.Add(entityNode);
        }

        public void Add(EntityNode entityNode)
        {
            System.Diagnostics.Debug.Assert(this.BoundingBox != null, "OctreeOctant.Add() - BoundingBox is null.");
#if DEBUG
            // only support square octree octants for performance
//            // TODO: we are going to see if this "performance" concern is no longer valid.  non square octrees are useful 
//            System.Diagnostics.Debug.Assert(this.BoundingBox.Max.x - this.BoundingBox.Min.x ==
//                this.BoundingBox.Max.y - this.BoundingBox.Min.y &&
//            this.BoundingBox.Max.x - this.BoundingBox.Min.x == 
//            this.BoundingBox.Max.z - this.BoundingBox.Min.z);
#endif

            int count;

            if (mEntityNodesCollection == null)
                count = 0;
            else
                count = mEntityNodesCollection.Count;

            //    NOTE: We specifically use ">=" for the depth comparison so that we
            //          can set the maximumDepth depth to 0 if we want a tree with
            //          no depth.
            if (count >= OctreeOctant.SplitThreshHold || _depth >= OctreeOctant.MaxDepth)
            {
                // add to this octant immmediately
                // Non Recursive Add
                this.AddEntityNodeToCollection((EntityNode)entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName);
                return;
            }

            // note: we intentionally compute a radius without taking into account hypotenuse.
            // note: if allowiing non square octants, we take the smallest octant radius and we'll compare that against largest radius of entity being inserted
            double octantRadius = this.BoundingBox.Max.x - this.BoundingBox.Min.x;
            octantRadius = Math.Min(this.BoundingBox.Max.y - this.BoundingBox.Min.y, octantRadius);
            octantRadius = Math.Min(this.BoundingBox.Max.z - this.BoundingBox.Min.z, octantRadius);
            octantRadius *= 0.5d;

            double childOctantRadius = octantRadius * 0.5d;
            double entityRadius = entityNode.BoundingBox.Radius;

            // attempt to insert using tightbox, but we must cull with loose box
            if (entityRadius > childOctantRadius)
            {
                // this entity won't fit in any children of this octant 
                // so what about the parent octant?
                if (entityRadius > octantRadius)
                {
                    // wont fit, can we try to move up to a parent?
                    if (this.IsRoot == false)
                    {
                        // Recurse UPWARDS
                        mParent.Add(entityNode);
                        return;
                    }
                }
                // Non Recursive Add because we're still here, 
                // so it either fits or we're at root and there's
                // no other place to put it
                this.AddEntityNodeToCollection(entityNode);
                //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Add() - " + entityNode.Entity.TypeName);
                return;
            }

            // can't go further, add entitynode here
            if (this.Split() == false)
            {
                this.AddEntityNodeToCollection(entityNode);
                return;
            }

            Vector3d octantCenter = this.BoundingBox.Center;
            Vector3d entityCenter = entityNode.BoundingBox.Center; // TODO: is the entityNode box initialized at this point?
            int code = 0;
            if (entityCenter.x > octantCenter.x)
                code |= 1;
            if (entityCenter.y > octantCenter.y)
                code |= 2;
            if (entityCenter.z > octantCenter.z)
                code |= 4;

            for (int i = 0; i < MAX_CHILD_COUNT; i++)
            {
                // if this bitflag cobmination is not set
                if (code != i) continue;

                Vector3d offset = OctreeOctant.BoundsOffsetTable[i] * octantRadius;
                Vector3d center = octantCenter + offset;

                BoundingBox childOctantBox = new BoundingBox(center, (float)childOctantRadius);

                if (mChildOctants[i] == null)
                    mChildOctants[i] =
                        new OctreeOctant(0, _depth + 1, childOctantBox, this);

                // Recursive Add() until max depth is reached or the entity's radius > octant's loose radius
                mChildOctants[i].Add(entityNode);
            }
        }

        public void RemoveEntityNode(EntityNode entityNode)
        {
            // NOTE: The reason for this function as opposed to just using OnEntityNode_Removed()
            // is that when a node is moving, then we directly call OnEntityNode_Removed() instead
            // so that the .SpatialNode = null can occur before we call OnEntityNode_Removed() 
            // and yet so we dont have to make the OnEntityNode_Removed() before we .Add to the new
            // destination.  This is important to avoid collapsing of empty branches before we've
            // had a chance to find the correct new parent.
            entityNode.SpatialNode = null;
            OnEntityNode_Removed(entityNode);
        }


        public void OnEntityNode_Moved(EntityNode entityNode)
        {
            // is the entity still in this bounds?
            // we dont have to test the radius of the entityNode because
            // we already know it fits.
            //    System.Console.WriteLine("m");
            if (mBox.Contains(entityNode.BoundingBox.Center)) return;

            // inform the parent that the entity in this octant no longer fits
            // NOTE: we do not add/remove the entityNode here.  The parent must do it
            // so that we don't trigger collapse of all 8 of it's children before parent can 
            // have a chance to fit it into one of its other 7 children
            if (this.IsRoot == false)
                mParent.Move(this, entityNode); // calls on Parent
        }

        private void Move(OctreeOctant childOctant, EntityNode entityNode)
        {
            //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Move() - " + entityNode.Entity.TypeName);
            // NOTE: Here we clear the .SpatialNode first but we must not call OnEntityNode_Removed()
            //       until AFTER .Add() is called.
            entityNode.SpatialNode = null;

            // we cannot simply attempt to add to this parent because
            // if the entityNode has moved beyond this parent's own bounds
            // our fast Add() (which avoids having to do a Box.Contains() call 
            // will not be able to determine this and will simply force insert
            // the entityNode into itself.

            // so we can easily avoid that by recursing til we find the first parent
            // that contains the entityNode.. and provided the entityNode has not changed size
            // (particularly has not gotten larger) we are guaranteed that the parent octant
            // is large enought to contain it if the entityNode's center is with in it.

            OctreeOctant newOctant = this;
            Vector3d entityCenter = entityNode.BoundingBox.Center;
            while (newOctant.Parent != null)
            {
                if (newOctant.BoundingBox.Contains(entityCenter))
                    break;

                newOctant = newOctant.Parent;
            }

            //System.Console.WriteLine("Moved to new octant");
            newOctant.Add(entityNode);// Add must always occur before Remove() because we dont want to collapse branches before we've had a chance to determine if the child will move there!
            childOctant.OnEntityNode_Removed(entityNode);
        }

        public void OnEntityNode_Resized(EntityNode entityNode)
        {
            // does this entityNode still fit in this octant?
            // we must test against entire box since this entity may now be too big to fit
            if (mBox.Contains(entityNode.BoundingBox)) return;

            if (this.IsRoot == false)
                mParent.Resize(this, entityNode);
        }

        private void Resize(OctreeOctant childOctant, EntityNode entityNode)
        {
            //System.Diagnostics.Debug.WriteLine ("OctreeOctant.Resize() - " + entityNode.Entity.TypeName);
            entityNode.SpatialNode = null;

            // if the entity itself has resized, we cannot do the quick .Contains(point)
            // and instead must do .Contains(box) to see if this entity still fits within this octant
            OctreeOctant newOctant = this;
            BoundingBox box = entityNode.BoundingBox;


            while (newOctant.Parent != null)
            {
                if (newOctant.BoundingBox.Contains(box))
                    break;

                newOctant = newOctant.Parent;
            }

            // once we've found a parent that fully contains the box, we can do an a normal
            // Add() to recurse downwards again.
            // Add must always occur before Remove() because we dont want to collapse branches 
            // before we've had a chance to determine if the child will move there!
            newOctant.Add(entityNode);
            childOctant.OnEntityNode_Removed(entityNode);
        }


        internal void OnEntityNode_Removed(EntityNode entityNode)
        {
            // remove the entityNode
            if (mEntityNodesCollection == null) return;

            mEntityNodesCollection.Remove(entityNode);

            // can we collapse this octant?
            if (mEntityNodesCollection.Count == 0)
            {
                mEntityNodesCollection = null;
                // must now notify the parent that this octant can be destroyed
                // TODO: nov.27.2012 - i think when an entityNode is added to octree root node, there is no parent
                //       so how do we prevent this?  should i just return if null?  will do for now
                // TODO: however i also think part of the problem this seems to keep being called for non moving
                //       think like a manually place directional light is our physics update
                if (mParent == null) return;
                mParent.OnChildOctant_Empty(this);
            }
        }

        private void OnChildOctant_Empty(OctreeOctant childOctant)
        {
            int nullCount = 0;
            for (int i = 0; i < mChildOctants.Length; i++)
                if (mChildOctants[i] == childOctant)
                {
                    mChildOctants[i].Parent = null; // or mChildOctants[i].Dispose() ?
                    mChildOctants[i] = null;
                    nullCount++;
                }
                else if (mChildOctants[i] == null)
                    nullCount++;

            // if all child octants are null, we can delete the entire child array
            // and potentially it's parents too
            if (nullCount == MAX_CHILD_COUNT)
            {
                mChildOctants = null;
                if (IsRoot == false && mEntityNodesCollection == null)
                    mParent.OnChildOctant_Empty(this); // recurse upwards
            }
        }
        #endregion

        //public void Add(EntityNode element)
        //{
        //    int x = 0;
        //    int y = 0;
        //    int z = 0;
        //    int depth = FindIdealInsertion(element.Position, element.BoundingBox.Radius, ref x, ref y, ref z);

        //    OctreeOctant foundOctant = FindBestFittingOctant(x, y, z, depth);
        //    System.Diagnostics.Debug.WriteLine(string.Format("OctreeOctant.Add () - Adding entityNode to node {0} at {1},{2},{3} depth {4}", foundOctant.Index, x,y,z,depth ));
        //    foundOctant.AddEntityNode((EntityNode)element);

        //    if (foundOctant == null) throw new Exception();
        //}

        //private int FindIdealInsertion(Vector3d objectPosition, double objectRadius, ref int x, ref int y, ref int z)
        //{
        //    // TODO: if we enforce cubic octree, we dont need a box, just a diameter
        //    // and this shoudl be desireable because insertions is complicated if we need to test 3 axis 
        //    // radius
        //    if (objectRadius < 0)
        //    {
        //        System.Diagnostics.Debug.WriteLine("OctreeOctant.FindIdealInsertion() - Entity has negative radius.");
        //        return 0;
        //    }

        //    double octantDiameter = OctreeOctant.WorldBox.Diameter;
        //    int depth = 0;
        //    double k = .5;


        //    // iterate downwards in depth until the octant's loose radius is finally smaller than
        //    // the object's radius
        //    while(depth <= OctreeOctant.MaxDepth)
        //    {
        //        octantDiameter /= 2;
        //        depth++;

        //        if(octantDiameter * (1- k ) / 2 < objectRadius)
        //            break;
        //    }

        //    //we're off by one
        //    depth--;
        //    octantDiameter *= 2;

        //    //get the x,y,z index of the node at this level in the tree
        //    x = (int) (objectPosition.x / octantDiameter);
        //    y = (int) (objectPosition.y / octantDiameter);
        //    z = (int) (objectPosition.z / octantDiameter);


        //    return depth ;
        //}

        //private OctreeOctant FindBestFittingOctant(int x, int y, int z, int depth)
        //{
        //    OctreeOctant octant = RootOctant;
        //    BoundingBox box = OctreeOctant.WorldBox;

        //    for (int currentDepth = 0; currentDepth != depth; ++currentDepth)
        //    {
        //        if (!octant.Split())  // if the octant cannot be split, this is as far as we can go
        //            return octant;
        //        else
        //        {
        //            /*
        //                We can find the exact child without any comparisons
        //                For example, we're looking for an octant at depth 2 with x,y,z = (2,1,3)
        //                This will be a child of the octant at depth 1 with x,y,z = (1,0,1)

        //                We take the convention that childOctants are layed out as:

        //                local index                1D index
        //                [(0,1,0) (1,1,0)]        [2 3]
        //                [(0,0,0) (1,0,0)]        [0 1]
        //                                    =
        //                [(0,1,0) (1,1,0)]        [6 7]
        //                [(0,0,0) (1,0,0)]        [4 5]

        //                To find the local index of an octant in the frame of it's direct parent, 
        //                we have to divide the index by two.
        //                To find the local index of an octant in the frame of it's  parent x times up,
        //                we have to divide the index by 2^x

        //            */
        //            //this generates the local index of the child octant at (currentDepth - 1)
        //            int currentDepthX = x >> (depth - (currentDepth + 1));
        //            int currentDepthY = y >> (depth - (currentDepth + 1));
        //            int currentDepthZ = z >> (depth - (currentDepth + 1));
        //            int globalIndex = currentDepthX + currentDepthY << 1 + currentDepthZ << 2;

        //            int localIndex = LocalVectorToIndex(new int[] { currentDepthX, currentDepthY, currentDepthZ});

        //            if (octant.Children[localIndex] == null) 
        //            {
        //                // create a box with half the diameter and offset to the parent's center
        //                // according to it's octant index
        //                Vector3d offset = OctreeOctant.BoundsOffsetTable[localIndex] * box.Radius;
        //                Vector3d center = box.Center + offset;
        //                double radius = box.Radius / 2;

        //                box = new BoundingBox(center, (float)radius);

        //                octant.Children[localIndex] =
        //                    new OctreeOctant(globalIndex, currentDepth + 1, box);
        //            }
        //            octant = octant.Children[localIndex];
        //        }
        //    }

        //    //if we make it here, we're at the minimum depth. and we found our octant
        //    return octant;
        //}

        private bool Split()
        {
            // cannot split because we're at max depth
            if (_depth == OctreeOctant.MaxDepth)
                return false;

            // we are already split
            if (this.mChildOctants != null)
                return true;

            // initialize the array but do not instance or assign an Octant
            this.mChildOctants = new OctreeOctant[8];
            return true;
        }

        // TODO: I should implement an overloaded version of Query that traverses for Entities that lay within a bounding box or sphere with potential also of matching a "match" predicate.
        //       Multi-threading of the query would be ideal.
        /// <summary>
        /// Looks for Regions/Entities that are in descendant RegionNodes or EntityNodes 
        /// that match the specified predicate.
        /// </summary>
        /// <param name="recurse"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public virtual List<EntityNode> Query(EntityNode refEnt, bool recurse, BoundingBox searchArea, Func<EntityNode, EntityNode, bool> match)
        {
            if (match == null) throw new ArgumentNullException("SceneNode.Query() - match cannot be null.");

            if (!this.mBox.Intersects(searchArea))
                return null;
            //Console.WriteLine ("Query B");

            List<EntityNode> results = new List<EntityNode>();

            if (mEntityNodesCollection != null)
                for (int i = 0; i < mEntityNodesCollection.Count; i++)
                {
                    if (match(mEntityNodesCollection[i], refEnt))
                        results.Add(mEntityNodesCollection[i]);
                }

            if (recurse)
            {
                if (mChildOctants != null)
                {
                    // NOTE: We recurse the child OctreeOctants, not EntityNodes
                    for (int j = 0; j < mChildOctants.Length; j++)
                    {
                        List<EntityNode> nestedResults = mChildOctants[j].Query(refEnt, recurse, searchArea, match);
                        if (nestedResults != null)
                            results.AddRange(nestedResults);

                    }
                }
            }

            if (results.Count == 0) return null;
            return results;
        }


        #region IBoundVolume Members
        /// <summary>
        /// Public bbox used for culling tests
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                return mBox;
            }
        }

        
        public BoundingSphere BoundingSphere
        {
            get { return new BoundingSphere(mBox); } // TODO: compute center from x,y,z index, then return sphere new BoundingSphere(center, _radius); }
        }
        

        public bool BoundVolumeIsDirty
        {
            // octree bounds are fixed.
            get { return false; }
        }


        protected void UpdateBoundVolume()
        {

        }
        #endregion
    }
}