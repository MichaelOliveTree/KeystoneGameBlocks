using System;
using System.Collections.Generic;
using Keystone.Types;
using Keystone.Portals;
using Keystone.SpatialNodes;
using Keystone.Traversers;
using Keystone.Primitives;

namespace Keystone.SpatialNodes.QuadTree
{
    
    // http://www.flipcode.com/archives/Octree_Implementation.shtml
    /// <summary>
    /// A dynamic + loose quadtree implementation. 
    /// Dynamic = children are only added up to max depth to accomodate items being inserted into the tree.
    /// </summary>
    public class QuadtreeQuadrant : ISpatialNode, ITraversable //, IBoundVolume
    {

        #region Static variables
        public static BoundingRect WorldRect;
        public static uint MaxDepth;
        public static uint SplitThreshHold;

        private static Vector2f[] BoundsOffsetTable = new Vector2f[] 
        {
                new Vector2f(-0.5f, -0.5f),
                new Vector2f(+0.5f, -0.5f),
                new Vector2f(-0.5f, +0.5f),
                new Vector2f(+0.5f, +0.5f)
        };

        #endregion

        private int _depth;
        private int _index;   // index is specific to each depth and contains x,y,z offset at that depth and is useful for finding neighbors (which we may never do and just always move EntityNodes by re-inserting starting at root)
        private const int MAX_CHILD_COUNT = 4;

        //private BoundingBox mBox;
        protected QuadtreeQuadrant mParent;
        protected QuadtreeQuadrant[] mChildQuadrants;

        // TODO: switch to linked list?
        protected List<EntityNode> mEntityNodes;


        public QuadtreeQuadrant(int index, int depth, BoundingRect rect, QuadtreeQuadrant parent) 
            : this()
        {
            _index = index;
            _depth = depth;
            mRect = rect;
            mParent = parent;
        }

        protected QuadtreeQuadrant()
        {
            Visible = true;
        }

        ~QuadtreeQuadrant()
        {
        }


        #region ITraversable Members
        public object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        protected QuadtreeQuadrant Parent { get { return mParent; } set { mParent = value; } }

        public bool IsLeaf { get { return mChildQuadrants == null; } }

        public int Index
        {
            get { return _index; }
        }

        internal int[] LocalIndexToVector(int index)
        {
            // divide the index by  2 ^ depth

            int[] v = new int[2];
            if ((index & 1) > 0) v[0] = 1;
            else v[0] = -1;

            if ((index & 2) > 0) v[1] = 1;
            else v[1] = -1;

            return v;
        }

        internal int LocalVectorToIndex(int[] v)
        {
            int index = 0;

            if (v[0] >= 0) index |= 1;
            if (v[1] >= 0) index |= 2;

            return index;
        }

        internal Vector2f Radius
        {
            get
            {
                Vector2f radius;
                float denominator = 2 ^ (Depth + 1);

                radius.x = WorldRect.Width / denominator;
                radius.y = WorldRect.Height / denominator;
                return radius;
            }
        }

        internal int Depth
        {
            get { return _depth; }
        }

        public ISpatialNode[] Children
        {
            get { return mChildQuadrants; }
        }

        #region ISpatialNode
        public bool Visible { get; set; }

        public EntityNode[] EntityNodes
        {
            get
            {
                if (mEntityNodes == null) return null;
                return mEntityNodes.ToArray();
            }
        }

        public virtual void Add(EntityNode entityNode, bool forceRoot)
        {
            if (forceRoot)
                this.AddEntityNode((EntityNode)entityNode);
            else
                this.Add(entityNode);
        }

        public virtual void Add(EntityNode entityNode)
        {
#if DEBUG
            // only support square octree octants for performance
            System.Diagnostics.Debug.Assert(this.BoundingRect.Max.x - this.BoundingRect.Min.x ==
                this.BoundingRect.Max.y - this.BoundingRect.Min.y);
#endif
            // note: we intentionally compute a radius without taking into account hypotenuse
            float octantRadius = (this.BoundingRect.Max.x - this.BoundingRect.Min.x) * 0.5f;
            float childOctantRadius = octantRadius * 0.5f;
            float entityRadius = (float)entityNode.BoundingBox.Radius;

            int count;

            if (mEntityNodes == null)
                count = 0;
            else
                count = mEntityNodes.Count;

            //    NOTE: We specifically use ">=" for the depth comparison so that we
            //          can set the maximumDepth depth to 0 if we want a tree with
            //          no depth.
            if (count >= QuadtreeQuadrant.SplitThreshHold || _depth >= QuadtreeQuadrant.MaxDepth)
            {
                // Non Recursive Add
                this.AddEntityNode((EntityNode)entityNode);
                return;
            }
            // insert using tightbox, but we must cull with loose box
            else if (childOctantRadius < entityRadius)
            {
                // this entity won't fit in any children of this octant so will it fit here?
                if (entityRadius > octantRadius)
                {
                    // it wont fit, can we try to move up to a parent?
                    if (this.IsRoot == false)
                    {
                        // Recursive Add()
                        mParent.Add(entityNode);
                        return;
                    }
                }
                // Non Recursive Add because we're still here, so it either fits or we're at root and there's no other place to put it
                this.AddEntityNode(entityNode);
                return;
            }

            Vector2f octantCenter = this.BoundingRect.Center;
            Vector2f entityCenter = BoundingRect.FromBoundingBox(entityNode.BoundingBox).Center;

            int code = 0;
            if (entityCenter.x > octantCenter.x)
                code |= 1;
            if (entityCenter.y > octantCenter.y)
                code |= 2;


            // can't go further, add it here
            if (this.Split() == false)
            {
                this.AddEntityNode(entityNode);
                return;
            }

            for (int i = 0; i < MAX_CHILD_COUNT; i++)
            {
                if (code != i) continue;

                Vector2f offset = QuadtreeQuadrant.BoundsOffsetTable[i] * octantRadius;
                Vector2f center = octantCenter + offset;

                BoundingRect childOctantRect = new BoundingRect(center, (float)childOctantRadius);

                if (mChildQuadrants[i] == null)
                    mChildQuadrants[i] =
                        new QuadtreeQuadrant(0, _depth + 1, childOctantRect, this);

                // Recursive Add() until max depth is reached or the entity's radius > octant's loose radius
                mChildQuadrants[i].Add(entityNode);
            }
        }

        private void AddEntityNode(EntityNode entityNode)
        {
            if (mEntityNodes == null)
                mEntityNodes = new List<EntityNode>();

            entityNode.SpatialNode = this;
            mEntityNodes.Add(entityNode);
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
        

        protected virtual void Move(QuadtreeQuadrant childOctant, EntityNode entityNode)
        {
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

            QuadtreeQuadrant newOctant = this;
            Vector2f entityCenter = BoundingRect.FromBoundingBox(entityNode.BoundingBox).Center;


            Interior interior = entityNode.Entity.Parent as Interior;

            if (interior != null)
            {
                // todo: maybe i should have entity update it's deck location as it moves and cache it rather than recompute it here as it moves

                bool deckChanged = DeckChanged (interior, entityNode.Entity);
                if (deckChanged)
                {
                    // TODO: I don't think this is working.  breakpoint never hits, but yet, the walking between decks appears to work. it could be just a bug with the Y axis never getting updated when traversing the quadtree. So the NPC is still in the same XZ plane as the current quadtree keeps it there.
                    // todo: more testing will reveal that, if it's true
                    QuadtreeCollection qc = interior.RegionNode.SpatialNodeRoot as QuadtreeCollection;
                    qc.Add(entityNode);
                    // NOTE: here we call OnEntityNode_Removed() and NOT RemoveEntityNode() since the latter will set null the EnttiyNode.SpatialNod
                    childOctant.OnEntityNode_Removed(entityNode);
                    return;
                }
            }
            //

            while (newOctant.Parent != null)
            {
                if (newOctant.BoundingRect.Contains(entityCenter))
                    break;

                newOctant = newOctant.Parent;
            }

            newOctant.AddEntityNode(entityNode); // AddEntityNode must always occur before Remove() because we dont want to collapse branches before we've had a chance to determine if the child will move there!
            childOctant.OnEntityNode_Removed(entityNode);
        }

        private bool DeckChanged(Interior interior, Entities.Entity entity)
        {
            int deck = interior.GetFloorFromAltitude(entity.Floor); // TODO: if .Translation.y already contains floor we are in trouble, but it should not ever!  it cannot! that was the old way of doing things.
            bool deckChanged = deck != interior.GetFloorFromAltitude(entity.PreviousTranslation.y);

            return deckChanged;
        }

        // TODO: when an entity crosses quadtree collection boundaries, we need to remove and add them to the new one in the same way we do it when we first add them.
        //      This could maybe be done in Simulation.FinalizeEntityMovement()

        public virtual void OnEntityNode_Moved(EntityNode entityNode)
        {
            // is the entity still in this bounds?
            // we dont have to test the radius of the entityNode because
            // we already know it fits.
            bool deckChanged = DeckChanged(entityNode.Entity.Parent as Interior, entityNode.Entity);
#if DEBUG
            if (deckChanged)
                System.Diagnostics.Debug.WriteLine("Deck changed for Entity '" + entityNode.Entity.Name + "'");
#endif
            if (!deckChanged && mRect.Contains(BoundingRect.FromBoundingBox(entityNode.BoundingBox).Center)) return;

            // inform the parent that the entity in this octant no longer fits
            // NOTE: we do not add/remove the entityNode here.  The parent must do it
            // so that we don't trigger collapse of all 8 of it's children before parent can 
            // have a chance to fit it into one of its other 7 children
            if (this.IsRoot == false)
                mParent.Move(this, entityNode); // calls on Parent
#if DEBUG
            else 
                System.Diagnostics.Debug.Assert (mParent == null);
#endif
        }

        public virtual void OnEntityNode_Resized(EntityNode entityNode)
        {
            // does this entityNode still fit in this quadrant?
            // we must test against entire box since this entity may now be too big to fit
            if (mRect.Contains(BoundingRect.FromBoundingBox(entityNode.BoundingBox))) return;
            if (this.IsRoot == false)
                mParent.Resize(this, entityNode); // calls on Parent
        }

        private void Resize(QuadtreeQuadrant childOctant, EntityNode entityNode)
        {
            entityNode.SpatialNode = null;

            QuadtreeQuadrant newOctant = this;
            BoundingRect rect = BoundingRect.FromBoundingBox (entityNode.BoundingBox);

            // if the entity itself has resized, we cannot do the quick .Contains(point)
            // and instead must do .Contains(box) to see if this entity still fits within this quadrant
            while (newOctant.Parent != null)
            {
                if (newOctant.BoundingRect.Contains(rect))
                    break;

                newOctant = newOctant.Parent;
            }

            // TODO: we do not test if the entityNode is already within the existing octant
            //       and so we don't avoid the costly Add/Remove calls below.
            newOctant.AddEntityNode(entityNode); // Add must always occur before Remove() because we dont want to collapse branches before we've had a chance to determine if the child will move there!
            childOctant.OnEntityNode_Removed(entityNode);
        }


        private void OnChildQuadrant_Empty(QuadtreeQuadrant childQuadrant)
        {
            int nullCount = 0;
            for (int i = 0; i < mChildQuadrants.Length; i++)
                if (mChildQuadrants[i] == childQuadrant)
                {
                    System.Diagnostics.Debug.Assert(mChildQuadrants[i].EntityNodes == null);
                    if (mChildQuadrants[i].Parent is QuadtreeCollection)
                        continue;
                    mChildQuadrants[i].Parent = null; // or mChildQuadrants[i].Dispose() ?
                    mChildQuadrants[i] = null;
                    nullCount++;
                }
                else if (mChildQuadrants[i] == null)
                    nullCount++;

            // if all child quadrants are null, we can delete the entire child array
            // and potentially it's parents too
            if (nullCount == MAX_CHILD_COUNT)
            {
                mChildQuadrants = null;
                if (IsRoot == false && mEntityNodes == null) 
                    mParent.OnChildQuadrant_Empty(this); // recurse upwards
            }
        }

        internal void OnEntityNode_Removed(EntityNode entityNode)
        {
            System.Diagnostics.Debug.Assert(mEntityNodes != null && mEntityNodes.Count > 0);

            // remove the entityNode
            mEntityNodes.Remove(entityNode);

            // can we collapse this octant?
            if (mEntityNodes.Count == 0 && ChildOctantsEmpty(this))
            {
                mEntityNodes = null;
                // must now notify the parent that this octant can be destroyed
                mParent.OnChildQuadrant_Empty(this);
            }
        }

        private bool ChildOctantsEmpty(QuadtreeQuadrant quadrant)
        {
            if (mChildQuadrants == null) return true;

            int count = 0;
            for (int i = 0; i < mChildQuadrants.Length; i++)
            {
                if (mChildQuadrants[i] == null)
                    count++;
            }

            if (count == MAX_CHILD_COUNT) return true;

            return false;
        }
#endregion


        private bool IsRoot { get { return mParent == null; } }


        private bool Split()
        {
            // cannot split because we're at max depth
            if (_depth == QuadtreeQuadrant.MaxDepth)
                return false;

            // we are already split
            if (this.mChildQuadrants != null)
                return true;

            // initialize the array but do not instance or assign an Octant
            this.mChildQuadrants = new QuadtreeQuadrant[4];
            return true;
        }


#region IBoundVolume Members
        private BoundingRect mRect;
        public BoundingRect BoundingRect
        {
            get { return mRect; }
        }

        /// <summary>
        /// Public bbox used for culling tests
        /// </summary>
        //public BoundingBox BoundingBox
        //{
        //    get
        //    {
        //        return mBox;
        //    }
        //}

        //public BoundingSphere BoundingSphere
        //{
        //    get { return new BoundingSphere(mBox); } // TODO: compute center from x,y,z index, then return sphere new BoundingSphere(center, _radius); }
        //}

        public bool BoundVolumeIsDirty
        {
            get { return false; } // octree bounds are fixed.
        }


        protected void UpdateBoundVolume()
        {

        }
#endregion
    }
}
