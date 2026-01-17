//using System;
//using System.Diagnostics;
//using Keystone.Elements;
//using Keystone.Octree;
//using Keystone.Portals;
//using Keystone.Traversers;

//namespace Keystone.SpatialNodes.Octree
//{
//    /// <summary>
//    // note: i need to enforce the following:
//    // 0 - OctreeRegionNode is a type of Region node that contains an Otree root as a private variable
//    // 1 - OctreeRegionNode is a regionNode of course and any SceneNode inserted 
//    //     under it should always have it as the parent to the child
//    //     sceneNode no matter where that child sceneNode is inserted into the octree 
//    //      data structure contained in the OctreeRegionNode.
//    // 2 - Thus, OctreeRegionNode should have as it's list of "Children" all SceneNodes no matter where they are in the Octree.
//    // 3 - Thus, OctreeNode is not a SceneNode and is not in that Children list.
//    // 4 - RegionNode's can only be added under other RegionNodes and not under EntityNodes or regular SceneNodes.
//    // 5-  My original thinking for making OctreeNode a SceneNode was for traversal purposes since as you traverse any SceneNode
//    //     all Children are also SceneNodes so going from the OctreeHost to the OctreeNode's themsleves was no different.  "Children"
//    //     always contained "SceneNodes" this way and not an OctreeNode that was not itself a type of sceneNode where the meaning
//    //     of Children changed.
//    //     And with OctreeRegionNode having a list of all "real" SceneNodes directly under it (EntityNodes, RegionNodes, SceneNodes)
//    //     then you'd have to special case the traversal to skip those and instead traverse the OctreeNode's but these  small
//    //     special cases is so minor and it doesnt outweigh the problems with having OctreeNode's as actual SceneNode children.
//    // 6 - IMPORTANT: That is also why any SceneNode, RegionNode or EntityNode added to an OctreeRegionNode to be inserted into a SceneNode
//    //     must be added directly to some OctreeNode and NEVER traversed further from there into SceneNodes inserted into that OctreeNode
//    //     because that would imply that the OctreeRegionNode ISNOT the SceneNode parent and we can't have that.  If you have a RegionHost
//    //     under an OctreeRegionNode and it's inserted into that OctreeNode somewhere, then if you want to add child SceneNode's to it
//    //     you must do so directly via RegionHost.AddChild() and not via Adding to the OctreeRegionNode and having it look for any 
//    //     region's.  This is desired behavior and it should not be any real limitation, but it will maintain design elegance.


//    /// Originally I had lots of confusion regarding whether OctreeNodes themselves should be a type 
//    /// of SceneNode or whether a seperate OctreeRegionNode class should hold
//    /// the OctreeNode root and each OctreeNode can contain unlimited EntityNodes.
//    /// In terms of a generic solution, it'd be nice if OctreeNode was not a SceneNode and instead
//    /// only needed it's children to implement a few basic things like IBoundVolume
//    /// and this way the OctreeNode can be used elsewhere.  
//    /// 
//    /// But as for entities needed to still have their own sceneNode there's no doubt.  A sceneNode
//    /// is in region specific coordinates and the entity itself is in parent relative coordinates.
//    /// 
//    /// 
//    /// A key deciding factor is that a SceneNode is in region-centric coordinates whereas
//    /// an Entity is in parent relative coordinates!  Clearly for an octree to work the entire
//    /// structure and each of its individual elements should not be transformed every frame even for
//    /// entities that dont move!
//    /// 
//    /// that it should basically be a partitioned type of SceneNode.
//    /// I actually did finally decide that YES this was correct
//    /// because I had almost decided that Region Entities would be connected by Portal entities
//    /// and so whether a Region was hosted by an Octree or just an unpartitioned SceneNode
//    /// was now of no importance.  In other words there was no longer any issue with having 
//    /// Octree inherit ISector since Portal's only joined ISector's together.
//    /// 
//    /// </summary>
//    public class OctreeRegionNode : RegionNode
//    {
//        private OctreeNode _octreeRoot;

//        public OctreeRegionNode(Region region) : base(region)
//        {
//            // not sure how i should handle this.  I could wait for a "Region" Entity to be added 
//            // and then use it's size to generate the octree.  I could wait for Region notifications
//            // if the Region size is changed but this really complicates things because if the region changes
//            // size and we try to regen, we could have child SceneNodes that no longer fit in the bounds.

//            // but this node definetly needs a region so perhaps it should be passed in the constructor?
//            // The key feature about Region hosting nodes is that the host node's bounding volume is solely
//            // determined by that Region and child SceneNodes must then fit within those boundaries.

//            // Other sceneNode types by comparison are simply grouping nodes and will take the size of all of its
//            // children as it's size.

//            // if somehow the region were to be removed, you would then have to recurse all octreenodes
//            // remove all children, and remove all other scenenodes that are at root level and update the bounds of
//            // this node to be null?

//            // So next question is, do we want these types of SceneNodes where we have a special one for each entity type?
//            // No. I think the clear answer is only when we need different functionality (e.g. computeboundingbox is handled 
//            // differently for fixed sized nodes that contain a region or variable sized nodes that rely on child bounds.

//            // generate the octree based on the region's parameters
//            region.SceneNode = this;
//            _octreeRoot = new OctreeNode(_region.BoundingBox, region.OctreeDepth);
//            //      OctreeNode.GenerateNodes((OctreeNode)_children[0], maxdepth);
//            //      Trace.WriteLine("Octreenodes Generated = " + OctreeNode.ActiveNodes.Count);
//            SetChangeFlags( Enums.ChangeStates.All, Enums.ChangeSource.Self);
//        }

//        public OctreeNode OctreeRoot
//        {
//            get { return _octreeRoot; }
//        }

//        public override object Traverse(ITraverser target, object data)
//        {
//            return target.Apply(this, data);
//        }

//        // sceneNode's added to an OctreeHost must get inserted into the Octree itself and NOT
//        // to any _children SceneNodes because we dont use them.
//        public override void AddChild(SceneNode child)
//        {
//            if (_octreeRoot == null)
//                throw new Exception("Root OctreeNode must already exist before we can insert child SceneNodes");

//            _octreeRoot.Insert(child);

//            //PersistIsDrity = true;
//        }


//        #region IBoundVolume 

//        protected override void UpdateBoundVolume()
//        {
//            // TODO: this _maxVisibleDistance calc is completely flawed.  It must take into account position and Radius of child relative
//            // to parent's center 
//            //if (ChildCount > 0)
//            //    for (int i = 0; i < _children.Count; i++)
//            //        _maxVisibleDistance = System.Math.Max(_maxVisibleDistance, _children[i].MaxVisibleDistance);
//            //else
//            //    _maxVisibleDistance = 0;

//            // the bounding volume of an octree host is always restricted to the bounds of the region it contains
//            Trace.Assert(_region != null);
//            _box = _region.BoundingBox;
//            _sphere = _region.BoundingSphere;

//            ClearChangeFlags();
//        }

//        #endregion
//    }
//}