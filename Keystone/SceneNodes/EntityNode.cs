using System;
using Keystone.Entities;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Elements;
using System.Collections.Generic;

namespace Keystone.SceneNodes
{
    /// <summary>
    /// Hierarchical spatial bounds information.  Unlike the Entity it hosts which has only 
    /// it's own bounding volume, the EntityNode also includes in it's own volume the volume of all
    /// child EntityNodes.
    /// </summary>
    public class EntityNode : SceneNode
    {
        protected Entity _entity;
        private SpatialNodes.ISpatialNode mOctant;

        public EntityNode(Entity entity) : base()
        {
            if (entity == null) throw new ArgumentNullException();
            _entity = entity;
            _entity.SceneNode = this;
        }

        public override void Dispose()
        {
            //if (_entity != null)
            //    _entity.SceneNode = null;  // this seems to just cause problems where in some cases we expect to re-use scenenodes.
    
            //_entity = null;
            //mOctant = null;
        }

        #region Node Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        internal SpatialNodes.ISpatialNode SpatialNode { get { return mOctant; } set { mOctant = value; } }

        public Entity Entity
        {
            get { return _entity; }
        }


        // called by SceneBase.NodeResized. spatial management housekeeping 
        // NOTE: Here is where the spatial node gets a chance at updating it's position in the scene.
        // This way this SpatialNode update occurs here during Update() and not during Cull()
        internal void OnEntityResized()
        {
            if (this.SpatialNode != null)
                this.SpatialNode.OnEntityNode_Resized(this);
        }

        // TODO: seems to me that Scene.cs itself could do all of this...
        //       OnEntityNode and ONEntityResized are not even virtual so there's only the one
        //       implementation.  i suppose it's inconsequential for now where it's done.
        // called by Scene.NodeMoved. spatial management housekeeping 
        // NOTE: Here is where the spatial node gets a chance at updating it's position in the scene.
        // This way this SpatialNode update occurs here during Update() and not during Cull()
        internal virtual void OnEntityMoved()
        {
            Region currentRegion = _entity.Region;
            if (currentRegion == null) return; // node is somehow detached from scene yet still has SceneNode

            Vector3d position = _entity.Translation;

            // TODO: but when is the physics check done?
            // and how would physcs occur across regions/zones?  seems it'd have to continue
            // against the entity in the new zone/region it has wound up in?
            // bounds check the node.  This goes for our Viewpoint as well
            // The region the Viewpoint is in determines which regions get paged in.
            Region newRegion = null; // ideally set by the out param in the .ChildEntityBoundsCheck() below
            bool regionBoundsFail = false;

            //if (_entity is Keystone.Lights.PointLight)
            //    System.Diagnostics.Debug.WriteLine("EntityNode.OnEntityMoved() - PointLight");
            // TODO: under what circumstances would this entity's parent not be currentRegion?  Well if it's
            //       actually an NPC under a Structure of a Region for instance.  We could test if
            //       _entity.Region == currentRegion... but then we have to know that when such an Entity 
            //       is moving within a structure, when crossing zones then it should reparent to the
            //       structure of the new Zone it's moved into...
            if (_entity.Parent == currentRegion)
            {
                // only an entity that is direct 1st child of a Region can change regions.
                // otherwise it must stay attached to it's existing parent.  We then pass
                // authority to that parent when it's Moved event occurs.
                regionBoundsFail = currentRegion.ChildEntityBoundsFail(_entity, out newRegion);
			
                // NOTE: Star pointlights are attached to their Star entities and so this code
                // never gets called because _entity.Parent != any region.  This means that
                // when a star moves, it uses the regionChanged == false code below to move 
                // the EntityNode within the same region
                //if (_entity is Keystone.Lights.PointLight)
                //    System.Diagnostics.Debug.WriteLine("EntityNode.OnEntityMoved() - PointLight");

                if (regionBoundsFail)
                {
                	System.Diagnostics.Debug.Assert (Entity.Region != newRegion, "EntityNode.OnEntityMoved() - New Region should not be same as previous entity.Region");
                	System.Diagnostics.Debug.Assert (newRegion != null, "EntityNode.OnEntityMoved() - New Region should not be null");
                	System.Diagnostics.Debug.WriteLine ("EntityNode.OnEntityMoved() - Entity has left region " + currentRegion.ID + " and entered region " + newRegion.ID); 
                    if (this.SpatialNode != null)
                    {
                        // removing entirely from this Region means it's no longer
                        // in the same SpatialTree (eg quadtree or octree)
                        this.SpatialNode.RemoveEntityNode(this);
                        this.SpatialNode = null;
                    }

                    _entity.SwitchParent(newRegion);
                    return;
                }
            }

            if (regionBoundsFail == false)
            {
                // region hasn't changed so we don't remove from the current SpatialTree instead
                // just trigger "move" so octree or quadtree can reposition the node within the tree
                // if necessary
                //if (_entity is Keystone.Lights.PointLight)
                //    System.Diagnostics.Debug.WriteLine("EntityNode.OnEntityMoved() - PointLight");
                if (this.SpatialNode != null)
                    this.SpatialNode.OnEntityNode_Moved(this);
            }

            // if node is Viewpoint and newParent is null, then we haven't paged it in yet
            // but node should have it's mStartingRegionID set
            // TODO: and why isn't our pager updating the adjacents?

            // TODO: events for leaving old region
            // TODO: events for entering new region
            // TODO: recurse to child entities (stopping at first child region eg interiors)

           
            // since we're still here, we know we must have moved outside of previous parent's bounds
            // only case i can think of where maybe old still is new is if we were at edge and thus
            // had to instead just cap the player's bounds
            // so in that case, newRegion == null;
            //    
            //if (newRegion == null)
            //{
            //   we could not move to a new parent because we've reached edge of game world
            //   so we must cap the player's position at the boundary
            // }
            // else
            // {
            //    remove the entity from it's old Region, but we don't want to bring 
            //    the Repository refcount to 0.
            //    and add to the new
            // }



            // TODO: the other problem is, how do we avoid something like a "gun"
            // from detaching from it's parent entity?  Or is that never a problem 
            // because we're dealing with SceneNode's?  Well not true
            // because a fighter leaving a carrier would need to switch Regions from
            // interior to exterior... hrm... 
            // Well, if an entity's immediate parent is not a region, then it should 
            // never detach unless that entity itself explicitly "drops" that child.
            // But if an Entity that is attached directly to a Region moves to another
            // RegionEntity, then it's children stay on it of course but must move
            // to the new RegionNode it's parent moved too... hrm...
            // but I "think" that can be handled by erm... not sure... there's only two options
            // either we recursively notify Moved() on all those child entities and allow
            // them to be in whatever regionNode they end up in according to spatial location
            // or we force them to always be in same regionNode as parent even if that means
            // they are out of bounds! ???
        }

        // It's ok for us to use the region bounding box directly because
        // region's by definition volumentrically speaking encompass all entities
        // under them in the hierarchy.  Entity's bounding volumes are not hierarchical but
        // EntityNode's bounding box is.
        #region IBoundVolume Members
        protected override void UpdateBoundVolume()
        {
            // NOTE: these flags have to be disabled first or we will
            // get infinite recursion into this UpdateBoundVolume() upon
            // OnEntityNode_Resized() call
            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty | Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly);
           
            // TODO: this _maxVisibleDistance calc is completely flawed.  It must take into account position and Radius of child relative
            // to parent's center 
            //if (ChildCount > 0)
            //    for (int i = 0; i < _children.Count; i++)
            //        _maxVisibleDistance = System.Math.Max(_maxVisibleDistance, _children[i].MaxVisibleDistance);

            if (_entity != null)
            {
            	_box.Reset();
                
            	// the bounding box of any entity is always already in Region specific coords.
            	// the EntityNode however is a hierarchical box whereas the Entity or Model is just itself.
            	_box.Combine (_entity.BoundingBox);
            }

            // no children means there are no child SceneNodes 
            if (_children != null && _children.Length > 0)
            {
                // continue to include other Node's bounds into our final
                // NOTE: The bounding volume of any children SceneNodes (entity and other SceneNodes) 
                // are already in Region specific coordinates so there is no need to transform the box's
                // coordinates.
                // Recall that I decided EntityBounding volumes would always be in the coordinate system
                // of the region its most directly associated.  This means even a sword attached to an 
                // Actor will have the sword bounding volume in Region coordinates.
                for (int i = 0; i < _children.Length ; i++)
                {
                	if (_children[i] == null) continue;
                    SceneNode child = _children[i];

                    if (child is EntityNode && ((EntityNode)child).Entity is Lights.Light)
                        continue;
                    // NOTE: Here we must skip child RegionNodes if the entity of this EntityNode
                    // is a Container which makes any RegionNode the RegionNode for an Interior.
                    // And Interior's have no bearing on the bounding box of the Vehicle\Container.
                    // NOTE: We tried not even connecting the interior's regionNode to the Vehicle\Container's
                    // EntityNode and instead relying on PortalNode traversal to render Interiors, 
                    // but that made it difficult to do Isometric view rendering.
                    // Simply excluding the interior's boundingbox from being combined here with
                    // the EntityNode for this Container is better.  
                    // NOTE: An exterior mesh however is required or the boundingbox will always be 
                    // empty.
                    else if (child is RegionNode && _entity is Container)
                        continue;

                    _box.Combine(child.BoundingBox);
                }
            }

            _sphere = new BoundingSphere(_box);

            // ========================================================================================
            // Dec.4.2012 - however the code below is about the spatial tree updating.  when does this ever
            //              get a chance to occur?  lazy updates means this UpdateBoundVolume() is called
            //              during cull() which is not a good time to update our spatial trees is it?
            //              ACTUALLY, it does occur and it occurs inside SceneBase.NodeResized()
            //              and SceneBase.NodeMoved()!  I think none of the below is a concern anymore.
            //
            //
            // Dec.4.2012 - I'm fairly certain that child recursive call updating is completely unnecessary
            //              here because it takes place within
            //              each EntityNodes own UpdateBoundVolume() which are guaranteed to be done in 
            //              proper order so that child entitynode volumes are up to date before parent's
            //              so that parent entitynode's volume is completely up to date.  
            // TODO: Dec.4.2012
            //       I think the below old notes are wrong and all the below can be deleted.  In fact i think
            //       change flags on SceneNodes are possibly irrelevant as updates to volumes and position
            //       can occur from direct calls by the entity they host. 
            //       Further, since each Entity will propogate flags to it's children when that Entity
            //       moves, those child entitys will independantly notify SceneBase that it has moved and so
            //       it's EntityNode will receive the .OnEntityMoved.  I'm fairly certain this is how it works
            //       and why there is no need for any EntityNode to ever contact or call it's own children
            //       for anything.  Even with bounding volumes, the child entity who'sparent has moved will
            //       be notified by it's parent and then that child can notify it's own scenenode.
            // --- 
            // OLD NOTES PRE-DEC 2012
            // TODO: since EntityNode's are heirarchical, then this presents a problem
            // when adding children to an EntityNode that is directly added to Octant.. 
            // adding a child EntityNode to the parent EntityNode that is in an Octant
            // must trigger a move event so that the parent can ultimately be moved within the Octree?
            // Beyond that, it is up to us to deal with cases of 
            // children as children under a hierarchy when we intended to actually
            // put them directly under an octant.
            // TODO: is the above issue of a PointLight placed under a Star entity placed inside an Octant
            //       the issue with active pointlights seeming to not affect rendering?
            // call OnEntityNode_Moved() / OnEntityNode_Resized() AFTER DiableChangeFlags so we dont recurse trying to UpdateBoundingVolume after having just done so!
            //if (this.SpatialNode != null)
            //{
                //if ((_changeStates & Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly) == Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly)
                //{
                //    DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty | Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly);
                //    this.Octant.OnEntityNode_Moved(this);
                //}
                //else
                //{
                    
                   // TODO: following call produces a bug as an entity we are placing in a 
                //          quadtree will computing this bounding volume and then call resize
                //          which will attempt to MOVE the entity while it's already newly being
                //          added to the quadtree!  Obviously not desired.  This results in nodes
                //          being added incorrectly and unpredictably.  We need to ensure that resize
                //          can only be called after the entity has fully been inserted
                // TODO: Cull/Render must never be allowed to modify the Quadtree or Octree
                //      that must occur during the Update() logic.  Thus we cannot make this call here
                //      because CUll/Render can trigger boundingvolume update.  We must instead manually
                //      sendmessage to entitynode that a move has occurred and to resize or move
                //      the entity.
                //    this.SpatialNode.OnEntityNode_Resized(this);
                //}
            //}
         
        }
        #endregion
    }
}