using System;
using System.Collections.Generic;
using Keystone.Entities;
using Keystone.SceneNodes;

namespace Keystone.SceneNodes
{
    // SceneNode's can contain other SceneNode's withotu containing any entity or region can't they?  
    // hrm... actually ihave that as abstract SceneNode now
    // can i think of a case where a sceneNode would never have its own entity but yet have 
    // child scenenodes?  I can't really.  SceneNode and EntityNode should be merged as just SceneNode
    // with RegionNode\ZoneNode being the only derived cases
    public class RegionNode : SceneNode
    {
        protected Region _region;

        // NOTE: When adding child SceneNode's to this RegionNode they will either be added
        // to a list of children or inserted into the Spatial Data Structure which could be 
        // an OctreeOctant or a list of Volumes connected by Portals.
        // Thus our Octree.OctreeOctant here could more generically be an ISpatialLayoutManager 
        protected SpatialNodes.ISpatialNode mSpatialNodeRoot;

		protected uint mRegionNestingLevel = 0; // when region's are added as children to other regions, their nesting level is incremented.  This allows us to mouse pick the deepest containing region during pick traversal
        
        public RegionNode(Region region)
        {
            if (region == null) throw new ArgumentNullException();
            _region = region;


            if (region is ZoneRoot == false && region.MaxSpatialTreeDepth > 0)
            {
                // TODO: region.OctreeDepth is used but we should first test
                // region.SpatialLayout 
                // enum SpatialLayout
                // {
                //    none = 0,
                //    octree = 1,
                //    sectors = 2, // portal connected sectors
                //    grid = 3     // 3d volume of cells
                // }
                System.Diagnostics.Debug.Assert (region.BoundingBox != null, "RegionNode.ctor() - Region BoundingBox is null.");
                // TODO: there is a race condition where Pager is loading a Zone and instantiating things like the mSpatialNodeRoot below
                //       and where scenereader is loading the zone and it's structure and then attempting to add here...
                mSpatialNodeRoot = new Octree.OctreeOctant(0, 0, region.BoundingBox, null);
                Octree.OctreeOctant.MaxDepth = _region.MaxSpatialTreeDepth;
                Octree.OctreeOctant.WorldBox = region.BoundingBox;
                System.Diagnostics.Debug.Assert (region.BoundingBox.Center.Equals (Types.Vector3d.Zero()), "Zone centers must always be at 0,0,0 for every child Zone under ZoneRoot!");
                System.Diagnostics .Debug.Assert (region.BoundingBox != null);
                Octree.OctreeOctant.SplitThreshHold = 10;
            }


	        _region.SceneNode = this;
            SetChangeFlags(Enums.ChangeStates.All, Enums.ChangeSource.Self);
        }

        public uint NestingLevel {get{return mRegionNestingLevel;}}
        
        public override void Dispose()
        {
            //if (_region != null)
            //    _region.SceneNode = null; // this seems to just cause problems where in some cases we expect to re-use scenenodes.

            //_region = null;
            //mSpatialNodeRoot = null;
        }

        #region ITraversable Members
        public override object Traverse(Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion


        #region IGroup Members
        public override void AddChild(SceneNode child)
        {
            //if (child is EntityNode)
            //    System.Diagnostics.Trace.WriteLine(((EntityNode)child).Entity.TypeName + " added to Region");
            
            // child is added regardless of whether it's added to any spatial node
            base.AddChild(child);

            
            if (child is RegionNode)
            	((RegionNode)child).mRegionNestingLevel = this.mRegionNestingLevel + 1;
            
            if (child is EntityNode)
            {
                // Viewpoints are NOT added to Octree or Quadtree
                if (((EntityNode)child).Entity is Viewpoint) return;

            #if DEBUG
	            if (((EntityNode)child).Entity is TileMap.Structure)
	            {
	            	System.Diagnostics.Debug.WriteLine ("RegionNode.AddChild() - Adding as child 'TileMap.Structure.'");
	            	System.Diagnostics.Debug.Assert (mSpatialNodeRoot != null);
	            }
            #endif
            }
            


            // NOTE: we CAN insert child regions into the spatial structure.  In fact we must.
            //      Thus if we do not want a child region inserted into this spatial structure
            //      then DO NOT add it to this Region in the first place!
            if (mSpatialNodeRoot != null)
            {
                // TODO: here i actually need overloads added to our spatial structure interface
                // to support adding of child regions, but i do think it is useful although
                // i think i'll just ignore this functionality until its required someday down the line

                // HUD elements are forced added to root
                if ((((EntityNode)child).Entity.Attributes & KeyCommon.Flags.EntityAttributes.HUD) 
                    == KeyCommon.Flags.EntityAttributes.HUD)
                    mSpatialNodeRoot.Add((EntityNode)child, true);
                else
                    mSpatialNodeRoot.Add((EntityNode)child);
            }
        }

        public override void RemoveChild(SceneNode child)
        {
            base.RemoveChild(child);

            // viewpoints are NOT added to spatial nodes therefore we cannot remove it
            // from the spatial node
            if (child is EntityNode)
                if (((EntityNode)child).Entity is Viewpoint) return;

            // TODO: um... why is below commented? when do they get removed from spatial node?                     
            if (mSpatialNodeRoot != null)
            {
                EntityNode en = child as EntityNode;
                en.SpatialNode.RemoveEntityNode(en);
                //mSpatialNodeRoot.RemoveEntityNode((EntityNode)child);
                // note: mSpatialNodeRoot is never null'd after removing all children.
            }
        }
        #endregion


        public SpatialNodes.ISpatialNode SpatialNodeRoot
        {
            get { return mSpatialNodeRoot; }
        }

        public Region Region
        {
            get { return _region; }
        }


        // these are spatial queries which is why these functions
        // are created here in SceneNode's and not in Entity.  
        // And because these are spatial queries, only Entities are 
        // represented as having physical presence in the world and thus
        // we always return entities.
        // return true if the current Entity in the Query should be included
        //public delegate bool CustomQueryEvaluator(Entities.Entity entity, CustomQueryFilter customFilter);

        //public class CustomQueryFilter
        //{
        //    public CustomQueryEvaluator Evaluator;
        //    public object[] EvaluatorArguments;
        //}

        // public Entities.Entity[] Query (bool recurseChildRegions) // for zones, start at root
        // 
        //public Entities.Entity[] Query(bool skipRegionNodes)
        //{
        //    return Query(skipRegionNodes, null);
        //}

        //public Entities.Entity[] Query(bool skipRegionNodes, Predicate<Entities.Entity> match)
        //{
        //    return Query(skipRegionNodes, false, null, match);
        //}

        //public Entities.Entity[] Query(Keystone.Types.Vector3d position, double radius)
        //{
        //    return Query(position, radius, null);
        //}

        //public Entities.Entity[] Query(Keystone.Types.Vector3d position, double radius, Predicate<Entities.Entity> match)
        //{
        //    return Query(true, true, new Keystone.Types.BoundingSphere(position, radius), match);
        //}

        //public Entities.Entity[] Query(Keystone.Types.BoundingSphere sphere)
        //{
        //    return Query(sphere, null);
        //}

        //public Entities.Entity[] Query(Keystone.Types.BoundingSphere sphere, Predicate<Entities.Entity> match)
        //{
        //    return Query(true, true, sphere, match);
        //}

        // TODO: we can run a callback function to exclude once've we've found spatial query
        // exclude Lights
        // exclude Terrain
        // exclude Portals
        // exclude child Regions
        // 
        // Scene.QueryByRegion (Entity target, 
        // // TODO: ability to pass results from one query to another for additional filtering?
        // TODO:  if so then we pass the Entities[] or Nodes[] into a static function taht accepts a match
        //        we do not need an instanced node.
        // 
        // Scene.QueryByProximity (Entity target, BoundingSphere sphere)

        #region IBoundVolume Members
        protected override void UpdateBoundVolume()
        {
             // _region.MaxVisibleDistanceSquared;
            //_maxVisibleDistance = 0;
            //if (_region != null)
            //    _maxVisibleDistance = _region.MaxVisibleDistanceSquared;

            // TODO: this _maxVisibleDistance calc is completely flawed.  It must take into account position and Radius of child relative
            // to parent's center 
            //if (ChildCount > 0)
            //    for (int i = 0; i < _children.Count; i++)
            //        _maxVisibleDistance = System.Math.Max(_maxVisibleDistance, _children[i].MaxVisibleDistance);


            // It's ok for us to use the region bounding box directly because
            // region's by definition volumentrically speaking encompass all entities
            // under them in the hierarchy.  Unlike normal EntityBase where the entities bounding box
            // is not hierarchical. We use SceneNodes for hierarchical volume info.
            _box = _region.BoundingBox; // NOTE: BoundingBox is a class not a struct so watch for unintended consequences of not making a copy but using the exact reference here

            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
        }
        #endregion
    }
}