using System;
using System.Collections.Generic;
using System.Xml;

using KeyCommon.Flags;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Scene;
using Keystone.Traversers;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Entities
{
    // *NOTE: Insides of ships are also contained in Regions.  
    // In effect, a Region is any place that has it's own unique local coordinate system.
    //
    // for very large game worlds, we use a special fixed size volume of Region called a Zone.cs
    // that divide up our continguous space into an array of local coordinate systems but which
    // visually and physically appear whole to the user while playing. 
    public class Region : Entity
    {
        
        private List<Portal> _portals;   // portals can be attached from internal sectors to this Region as well.  In other words, a sector doesnt have to fill the entire bounds of a region
        protected uint mMaxSpatialTreeDepth; // the maximum depth


        public Region(string name, BoundingBox box,  uint spatialTreeDepth) : this(name)
        {
            mTranslation = new Vector3d(box.Center.x, box.Center.y, box.Center.z);
            mBox = box;
            
            // TODO: i need to test various depths, i think im using depth on interiors that is too deep.
            // it might be good for exterior, but interior at least its way too deep i think.
            // 
            if (spatialTreeDepth > 0)
            {
                mMaxSpatialTreeDepth = spatialTreeDepth;
            }
            SetChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty |
                Keystone.Enums.ChangeStates.RegionMatrixDirty,
                Keystone.Enums.ChangeSource.Self);
        }

        internal Region(string id) : base(id)
        {
            mEntityFlags |= EntityAttributes.Region; 
            SetEntityAttributesValue((uint)EntityAttributes.VisibleInGame, true);
             
        	SetChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty |
                Keystone.Enums.ChangeStates.RegionMatrixDirty, 
                Keystone.Enums.ChangeSource.Self);
        }

        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        #region IResource Members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[1 + tmp.Length];
            tmp.CopyTo(properties, 1);

            properties[0] = new Settings.PropertySpec("octreedepth", mMaxSpatialTreeDepth.GetType().Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = mMaxSpatialTreeDepth;
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
                    case "octreedepth":
                        mMaxSpatialTreeDepth = (uint)properties[i].DefaultValue;
                        break;
                }
            }

            // Region's will never allow for their translation, rotation or scale to be altered
            // TODO: This ideally should be handled via a business rule.
            mScale.x = 1;
            mScale.y = 1;
            mScale.z = 1;
            mTranslation.x = 0;
            mTranslation.y = 0;
            mTranslation.z = 0;
            mRotation.X = 0;
            mRotation.Y = 0;
            mRotation.Z = 0;
            mRotation.W = 1;
            
            // TODO: temp hack, June.16.2014 - old saved regions wont have the Region flag set after deserialization
            mEntityFlags |= EntityAttributes.Region;  

        }
#endregion


//
//		/// <summary>
//		/// How many levels of Region ancestors does this Region have.
//		/// This value is used when picking nested Regions and wanting to find
//		/// deepest.  
//		/// This has nothing to do with Octree Depth.
//		/// </summary>
//        public uint RegionDepth 
//        {
//            get
//            {
//            	// TODO: I think this entire property is redundant since I think i forgot
//            	//       I added NestingLevel under RegionNode scenenode.
//                uint depth = 0;
//                
//            	if (_parents == null || _parents[0] == null) return depth;
//                if ((_parents[0] is Region) == false) return depth; 
//                
//                Region parent = (Region)_parents[0];
//                while (parent != null)
//                {
//                	depth++;
//                    if (parent is Region == false) break; 
//                    parent = (Region)parent.Parent;
//                }
//                return depth;
//            }
//        }
        
        /// <summary>
        /// RegionNode's are where we implement spatial structures like octrees, quadtrees or grids.
        /// </summary>
        public RegionNode RegionNode
        {
            get { return (RegionNode) mSceneNode; }
        }
        
        public uint MaxSpatialTreeDepth  // octree or quadtree
        {
            get { return mMaxSpatialTreeDepth; }
        }

        public bool HasSpatialPartition // octree or quadtree
        {
            get { return mMaxSpatialTreeDepth > 0; }
        }

        public Portal[] Portals
        {
            get
            {
                if (_portals == null) return null;
                return _portals.ToArray();
            }
        }
                
        internal override ChildSetter GetChildSetter()
        {
            return new RegionSetter(this);
        }



        
        // OBSOLETE - Matrix is equivalent to Region matrix and for Regions and Zones the 
        // Matrix is identity by virtue of preventing scale/rotation/translation from being anything other
        // than Identity.  In fact for Zones, we added a new property ZoneTranslation to be used
        // to compute global position offsets
        //public override Matrix Matrix
        //{
        //    get
        //    {
        //        //        // TODO: Eventually all i have to do here is return Identity and don't allow
        //        //        // any Region to have translation/scale or rotaton.
        //        //        // Then in my overridden version of GetGlobalTransform, for Zone.cs only
        //        //        // i include the zone's offset translation as a replacement for it's local Matrix
        //        return Matrix.Identity(); // base.Matrix;
        //    }
        //    //    internal set
        //    //    {
        //    //        // Region's must always be Identity.
        //    //    }
        //}


        ///// <summary>
        ///// We override the RegionMatrix from EntityBase because here any Region 
        ///// (both Interior or Exterior) either has it's Matrix as Identity (Root and Interiors)
        ///// or as offsets from Root for inner Zone Regions that are stored in it's Matrix.
        ///// The reason this works for Interiors is because we always set the View during Culling
        ///// and Rendering to the same reference frame as the current region.  So all we ever care 
        ///// about is how to transform the camera between the regions so that one is properly 
        ///// relative to another.  But as far as any child entity to a region, those child's are 
        ///// always in their own region's coordinate system.  
        ///// </summary>
        //public override Matrix RegionMatrix
        //{
        //    get
        //    {
        //        if ((_changeStates & (Enums.ChangeStates.RegionMatrixDirty | Enums.ChangeStates.MatrixDirty)) > 0)
        //        {
        //            // MPJ - June.16.2011  - RegionMatrix for any region must always be Identity() and
        //            // we will compensate for the times when we truely want a global Matrix that is actually
        //            // not global but camera relative in order to maintain our double precision.
        //            // Thus when culling, we will need to remember to offset the translation
        //            // and then create a new Translation matrix from that.
        //            // But here Identity is required because Region's (Interior or Zone or whatever)
        //            // always have their own coordinate systems.
        //            // NOTE: We do not support scaled or rotated regions or translated regions.
        //            // That would be like saying that our Root is scaled.  Root is default single region
        //            // game world and it's never scaled either.  Regions are always simply unique 
        //            // coordinate systems.
        //            // NOTE 2: It seems odd, but when we see an interior of a ship being drawn correctly
        //            // we must remember that the interior has not moved at all.  It is just like a fixed
        //            // root region unto itself.  It only appears to have moved because the view matrix
        //            // is modified to put itself in a relative distance and orientation that the interior
        //            // when drawn appears to be moving with it's exterior.
        //            // NOTE 3: This reality reminds us of things like asteroid fields.  If we want asteroid fields
        //            // in Zones, then the field must be attached to a "Container" if we want to at render time
        //            // create the illusion that it is orbiting.
        //            // NOTE 4: Just remember, everything is done in Region space.  We draw in Region Space
        //            // (plus camera space translation), we pick in region space, we collide and physics in region space.
        //            // The region's themselves never move.
        //            _regionMatrix = Matrix.Identity();            
        //            DisableChangeFlags(Enums.ChangeStates.RegionMatrixDirty);
        //        }
        //        return _regionMatrix;
        //    }
        //}



        // TODO: Should the simulation instead call a parent scene node regarding a child entitynode?
        // instead of the simulation calling the region about a child entity?
        // I think maybe it should.  Afterall, what about entities who's parents are not regions but other children?
        //      Well those entities should NEVER be hierarchically moved to a different parent obviously.  We only do that
        //      for child entities attached hierarchically directly to Regions.  If a sword carried by an Actor moves
        //      such that the sword is in the future region and the actor is still in the previous region (with respect to the sword)
        //      then the sword should stay obviously attached to it's parent, but spatially... not sure yet.  Is there any harm in allowing it
        //      to update it's spatial node?  I think it could be confusing when the sword.Region != sword.EntityNode.Region?
        /// <summary>
        /// If the child entity moves out of this region, we're reponsible here for determining
        /// what the new region is and passing that as the out param.  
        /// </summary>
        /// <remarks>
        /// See Zone.cs for overridden version that handles translation across Zones.
        /// </remarks>
        /// <param name="child"></param>
        /// <param name="newParentRegion"></param>
        /// <returns></returns>
        internal virtual bool ChildEntityBoundsFail(Entity child, out Region newParentRegion)
        {
        	 // single region constrains translation to be in bounds of this Region
            newParentRegion = this;

            // TODO: but when is the physics check done?

            if (BoundingBox.Contains(child.DerivedTranslation)) return false;

            Vector3d position = child.DerivedTranslation;
            Vector3d max = mBox.Max;
            Vector3d min = mBox.Min ;

            if (position.x > max.x) position.x = max.x;
            else if (position.x < min.x) position.x = min.x;

            if (position.y > max.y) position.y = max.y;
            else if (position.y < min.y) position.y = min.y;

            if (position.z > max.z) position.z = max.z;
            else if (position.z < min.z) position.z = min.z;

            child.Translate (position);
            return false;
        }
        
		private List <Simulation.IEntitySystem> mEntitySystems;
        		
        public Simulation.IEntitySystem[] EntitySystems
        {
            get
            {
                if (mEntitySystems == null) return null;
                return mEntitySystems.ToArray();
            }
        }
        
		#region IGroup Members
        public void AddChild(Portal portal)
        {
            base.AddChild(portal);
            if (_portals == null) _portals = new List<Portal>();
            _portals.Add(portal);
        }

    
        public override void RemoveChild(Node child)
        {
            base.RemoveChild(child);
            if (child as Portal != null)
            {
                if (_portals == null) throw new Exception("No portals exist in this region. Nothing to remove.");
                _portals.Remove((Portal)child);
                if (_portals.Count == 0) _portals = null;
            }
        }
		#endregion

         //If octree use fixed region bounding volume computation
        // If not octree partitioned, then use hierarchical bound volume of all child entities.
        // NOTE: Regions are exception to the EntityBase.UpdateBoundVolume() that ignores child entities.
        // and only worries about it's own geometry based bounding volume.
        #region IBoundVolume Members
        protected override void UpdateBoundVolume()
        {
            // these Zones regions are fixed size, but some other types of Regions can be 
            // scaled.  Thus the bounding volume for those types can change.  Effectively if scaled and not
            // rotated, we can just scale the box.
            // todo; ideally we should have seperate "Regions" for unscaleable zones vs
            // other region types just as we do wtih Root region and CelledRegion

            //if (IsOctreePartition)
            //{
            //    // our region is fixed.
            //    // TODO: We should never have to transform the Region, Regions are always
            //    // in their own coordinate system.
            //    _box = BoundingBox.Transform(_box, RegionMatrix);
            //}
            //else
            //{
                // NOTE: REgion's are an exception to the rule that says an Entity only
                // includes it's own bounding volume and SceneNodes are responsible for
                // hiearchical bounding volumes.  This is because Regions are virtual spaces
                // whos volumes are not defined by geometry.
                //if ((_changeStates & Enums.ChangeStates.BoundingBox_TranslatedOnly & Enums.ChangeStates.BoundingBoxDirty) == Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly)
                //{
                //    throw new Exception("This code should never run.  Region's must not be translated. They are origin of their own coordinate system.");
                //    // TODO: test the above & test to see if it only triggers the following code
                //    // if just BoundingBox_TranslatedOnly is set
                //    DisableChangeFlags(Enums.ChangeStates.BoundingBox_TranslatedOnly);

                //    _box.Max -= _translationDelta;
                //    _box.Min -= _translationDelta;
                    
                //    _sphere = new BoundingSphere(_box);
                //    return;
                //}

                //if (_children != null && _children.Count > 0)
                //{
                //    _box = BoundingBox.Initialized();
                //    BoundingBox childboxes = BoundingBox.Initialized();
                //    for (int i = 0; i < _children.Count; i++)
                //    {
                //        if (_children[i] is ModeledEntity) // child entities bbox are already in region coords
                //        {
                //            childboxes = BoundingBox.Combine(childboxes, ((ModeledEntity)_children[i]).BoundingBox);
                //        }
                //        if (_children[i] is Geometry)
                //        {
                //            // child geometry have their bounding boxes box's transformed using their 
                //            // parent entity's RegionMatrix
                //            childboxes = BoundingBox.Combine(childboxes,
                //                                             BoundingBox.Transform(
                //                                                 ((Geometry)_children[i]).BoundingBox, RegionMatrix));
                //        }
                //    }
                //    _box = BoundingBox.Combine(_box, childboxes);
                //}

            //}
            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);

            //if (_box == null) return; // if the box still hasnt been set

            _maxVisibleDistanceSq = (mBox.Diameter * 2); // typically, we can only see the current zone and adjacents and not across two zones
            _maxVisibleDistanceSq *= _maxVisibleDistanceSq; // expected result is distance Squared

            mSphere = new BoundingSphere(mBox);
        }
        #endregion
    }
}