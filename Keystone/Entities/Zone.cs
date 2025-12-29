using System;
using System.Collections.Generic;
using Keystone.Types;
using Keystone.Entities;


namespace Keystone.Entities
{
    /// <summary>
    /// A type of Region that neighbors other Zones and exist only one child deep under
    /// a Root Zone.  Thus we know our parent entity must always be a Root Zone.
    /// </summary>
    /// <remarks>
    /// ZoneRegionNodes are spatial objects and do provide automatic handlng of crossing zones
    /// and notification mechanisms back to ISceneNodeListener for then making any
    /// appropriate reassignments of entities to different Zone entities.  But it's the logic
    /// in the Listener responsible for handling the actual entity parenting reassignments.
    /// NOTE: Client side, since we can't load all Zones at once, spatially we have also
    /// a case where neighbors may not be loaded which is why we can't just have actual
    /// references to neighbors set in advance.  We can have "indices" however.  Another option
    /// though is for the ZoneSceneNode's to be loaded in advance since they wont contain
    /// any entities underneath but will be available for Zones when they are loaded.  
    /// Sucks that I'm having to go back and rethink any of this crap. Argh.
    /// It would be nice to have the ZoneSceneNode's already loaded because an entity that
    /// somehow manages to cross into a region entity that is not loaded can still notify
    /// the Simulation and then the simulation can hold that entity in a queue while it waits
    /// for the Zone entity to load.  But memory wise for really large universes that would be
    /// too much.  It would be ok for 100x100x100 universes though as that would be less than 100megs
    /// of memory, but 1000x1000x1000 jumps to ~10 gigabytes (assuming 100 bytes per node)
    /// IMPORTANT: Also don't forget that every SceneNode for any type of Region does NOT
    /// use world coordinates. All of these SceneNode's stay in their Region's coordinate system.
    /// </remarks>
    public class Zone : Region 
    {
        private Vector3d _zoneTranslation;

        // TODO: I don't think Offset needs to be public because it's only used to compute the
        //       _zoneTranslation and then, _zoneTranslation is serialized so there's no need to store the offset at all.
        //       Furthermore, Offset is never referenced outside of this Zone class.
        public float[] Offset; // position offset multiplier
        public int[] ArraySubscript;
        
        
        public Zone(string name, BoundingBox box, uint octreeDepth, int subscriptX, int subscriptY, int subscriptZ, float offsetX, float offsetY, float offsetZ)
            : base(name, box, octreeDepth )
        {
            // ok for offsets to reflect fractions
            Offset = new float[] { offsetX, offsetY, offsetZ };
            // NOTE: The Zone name's are derived from the ArraySubscript NOT the Offset.
            ArraySubscript = new int[] {subscriptX, subscriptY, subscriptZ};
#if DEBUG
            int x, y, z;
            ZoneRoot.GetZoneSubscriptsFromName(name, out x, out y, out z);
            System.Diagnostics.Debug.Assert(subscriptX == x && 
                                            subscriptY == y && 
                                            subscriptZ == z);
#endif
            // note: we assign to ZoneTranslation and not Translation.  That's because
            // Zones like all Regions will always have their Matrix computed as Identity
            // ZoneTranslation = new Vector3d(offsetX * halfWidth,
            //                                offsetY * halfHeight,
            //                                offset * halfDepth);
            ZoneTranslation = new Vector3d(offsetX * box.Width,
                                           offsetY * box.Height,
                                           offsetZ * box.Depth);
        }

        public Zone(string id)
            : base(id)
        {
            Offset = new float[3];
            ArraySubscript = new int[3];
            
            // NOTE: Offset, ArraySubscript and BoundingBox are loaded from xml.
            
        }

        public static Vector3d GlobalTranslationFromName(ZoneRoot root, string name)
        {
            int x, y, z;
            ZoneRoot.GetZoneSubscriptsFromName(name, out x, out y, out z);

            float offsetX = root.StartX + x;
            float offsetY = root.StartY + y;
            float offsetZ = root.StartZ + z;

            BoundingBox box = root.GetChildZoneSize();

            Vector3d translation = new Vector3d(offsetX * box.Width,
                                           offsetY * box.Height,
                                           offsetZ * box.Depth);

            return translation;
        }

        public static Zone[, ,] Create(ZoneRoot root, uint octreeDepth, Keystone.IO.XMLDatabase db)
        {
            // all regions are centered at 0,0,0.  It's world coordinates can easily be deduced based on it's position in the array and 
            // the width, height and depth of each region.
            // this routine essentially creates a cube shaped array of regions for the purposes of creating
            // neighboring zones.  
            // Zones dont have to be cube but can be any rectangular shape (each zone the same dimensions however) 
            // To do this, simply 

            Zone[, ,] zones = new Zone[root.RegionsAcross, root.RegionsHigh, root.RegionsDeep];
            // each zone region has the exact same region relative bounding box 
            BoundingBox box = root.GetChildZoneSize();

            // TODO: We need to create a version of universe gen
            //       that only serializes Zones if they contain a child Entity.  Otherwise
            //       empty Zones should be viewed as empty space and although the Pager can page them
            //       in, they wont exist in the xmldb unless in the editor during galaxy design
            //       we add an entity to it.
            for (int i = 0; i < root.RegionsAcross; i++)
            {
                for (int j = 0; j < root.RegionsHigh; j++)
                {
                    for (int k = 0; k < root.RegionsDeep; k++)
                    {
                        float offsetX = root.StartX + i;
                        float offsetY = root.StartY + j;
                        float offsetZ = root.StartZ + k;

                        string name = root.GetZoneName(i, j, k);

                        zones[i, j, k] = new Zone(name, box, octreeDepth, i, j, k, offsetX, offsetY, offsetZ);

                        // TODO: i think here the original thinking behind saving these immediately
                        // is so we didn't have to keep them all in memory.
                        db.WriteSychronous(zones[i, j, k], true, false, false);
                        // note: (false, false) <--correct. 
                        // Region's and all other nodes are added with refcount==0 to Repository when they are instantiated
                        // HOWEVER note that for these Regions we do not increment their refcount and then decrement it 
                        // so these regions will remain in memory so that our caller can then unload them so no parent/child
                        // relationships get screwed during creation.
                        // It is the caller's responsibility to handle any inc/dec removal.
                    }
                }
            }

            // // the good thing about portals is they provide a string name of the region they connect to so we can easily find the ones to page
            // // add the portals to each region connecting them to their neighbors
            //for (int i = 0; i < regionsAcross; i++)
            //{
            //    for (int j = 0; j < regionsHigh; j++)
            //    {
            //        for (int k = 0; k < regionsDeep; k++)
            //        {
            //            // for each of the six possible sides
            //            for (int side = 0; side < 6; side++)
            //            {
            //                // if this side doesn't already have a portal

            //                // if this side is not against a border, it has a neighbor 
            //                Region destination;
            //                Portal p =
            //                    new Portal(Repository.GetNewName(typeof (Portal)), regions[i, j, k], destination,
            //                               BoundingBox.GetQuadFaceVertices(destination.BoundingBox));

            //                // we can do both sides at the same time by reversing the vertices for the other

            //            }
            //        }    
            //    }
            //}

            return zones;
        }

        #region ResourceBase members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[7 + tmp.Length];
            tmp.CopyTo(properties, 7);

            properties[0] = new Settings.PropertySpec("offsetx", Offset[0].GetType().Name);
            properties[1] = new Settings.PropertySpec("offsety", Offset[1].GetType().Name);
            properties[2] = new Settings.PropertySpec("offsetz", Offset[2].GetType().Name);
            properties[3] = new Settings.PropertySpec("subscriptx", ArraySubscript[0].GetType().Name);
            properties[4] = new Settings.PropertySpec("subscripty", ArraySubscript[1].GetType().Name);
            properties[5] = new Settings.PropertySpec("subscriptz", ArraySubscript[2].GetType().Name);
            properties[6] = new Settings.PropertySpec("zonetranslation", _zoneTranslation.GetType().Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = Offset[0];
                properties[1].DefaultValue = Offset[1];
                properties[2].DefaultValue = Offset[2];
                properties[3].DefaultValue = ArraySubscript[0];
                properties[4].DefaultValue = ArraySubscript[1];
                properties[5].DefaultValue = ArraySubscript[2];
                properties[6].DefaultValue = _zoneTranslation;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "offsetx":
                        Offset[0] = (float)properties[i].DefaultValue;
                        break;
                    case "offsety":
                        Offset[1] = (float)properties[i].DefaultValue;
                        break;
                    case "offsetz":
                        Offset[2] = (float)properties[i].DefaultValue;
                        break;
                    case "subscriptx":
                        ArraySubscript[0] = (int)properties[i].DefaultValue;
                        break;
                    case "subscripty":
                        ArraySubscript[1] = (int)properties[i].DefaultValue;
                        break;
                    case "subscriptz":
                        ArraySubscript[2] = (int)properties[i].DefaultValue;
                        break;
                    case "zonetranslation":
                        _zoneTranslation = (Vector3d)properties[i].DefaultValue;
                        break;
                }
            }

            SetChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty, Keystone.Enums.ChangeSource.Self);
        }
        #endregion

		#region IPageableTVNode Members
		public override void LoadTVResource()
		{
			base.LoadTVResource();
		}
		
		public override void UnloadTVResource()
		{
			base.UnloadTVResource();
			
			// Unload all map layers for the specified Zone.
        	// first child for Zone's that do have Structure (eg terrain or interior Zones as opposed to void outer space zones ) is always Structure
        	if (this.Children == null || this.Children[0] is Keystone.TileMap.Structure == false) return;
        		
        	Keystone.TileMap.Structure structure = (Keystone.TileMap.Structure)this.Children[0]; 
        	structure.Dispose();
		} 
		#endregion

		public virtual Vector3d ZoneTranslation
        {
            get { return _zoneTranslation; }
            set
            {
                _zoneTranslation = value;
                // TODO: this should not actually translate the zone or change it's matrix!
                // these flag changes are irrelevant
                SetChangeFlags(Enums.ChangeStates.BoundingBoxDirty | Enums.ChangeStates.MatrixDirty,
                                Enums.ChangeSource.Self);
            }
        }

        ///// <summary>
        ///// A Zone is a special case.  It has an offset position technically but usually
        ///// uses Identity Matrix for computing it's own Region Matrix (and child entities consult
        ///// the RegionMatrix to determine their own Region centrix Matrix)
        ///// So here we override so that we can include the offset in the calculation for the Global.
        ///// </summary>
        //public override Matrix GlobalMatrix
        //{
        //    get
        //    {
        //			// TODO: i commented all this out and now i seem to have issues with Zones... and there's a difference between
        //          //       generated universes with even numbered zones vs odd numbered zones. 
        //        // TODO: this is only used by Keystone.Traversers.Picker line 220 at the moment
        //        //      - verify all is well with this.  It does seem it allows us to pick through
        //        //      across zones just fine, but what about picking through portals?
        //        //      - seems taking into accoun the Zone's RootOffset is correct.

        //       // throw new Exception("What is this being called by?  I don't think we need this.");
        //        if (_parents == null || _parents[0] == null) return Matrix.Identity();

        //        Matrix result = Matrix.Translation (_zoneTranslation);

        //        // can we return just the zonetranslation matrix * Matrix?  Perhaps
        //        // in effect instead of here returning Matrix we return our ZoneMatrix
        //        // in fact that is exactly what we do below

        //        // TODO: a fundamental problem right now is that im not tracking the bone matrices per
        //        // Entity instance so when i grab a bone matrix it wont necessarily be for this current entity
        //        // instance when computing world matrix from bone, we cant use the parent's WorldMatrix 
        //        // because the matrix multiplication will give wrong result
        //        // Note: Matrix is first parameter in the multiplication because we want to perform local transforms
        //        // on the geometry first.
        //        if (AttachedToBoneID > -1)
        //            result *=  ((EntityBase)_parents[0]).Matrix *
        //                            ((BonedEntity)_parents[0])._actor.GetBoneMatrix(AttachedToBoneID, true) *
        //                            ((EntityBase)((EntityBase)_parents[0]).Parents[0]).GlobalMatrix;
        //        else
        //            result *=  ((EntityBase)_parents[0]).GlobalMatrix;

        //        return result;
        //    }
        //}

        public string[] GetAdjacentZoneNames (uint range)
        {
        	if (Parent == null) return null;
        	
        	ZoneRoot root = (ZoneRoot)Parent;
        	
        	return root.GetAdjacentZoneNames (ArraySubscript[0], ArraySubscript[1], ArraySubscript[2], range);
        }
        
        public string[] GetAdjacentZoneNames()
        {
            if (Parent == null) return null;

            ZoneRoot root = (ZoneRoot)Parent;
            return root.GetAdjacentZoneNames(ArraySubscript[0], ArraySubscript[1], ArraySubscript[2]).ToArray ();
        }

        public string[] GetAdjacentZoneNamesBeyondRange(uint range)
        {
        	if (Parent == null) return null;

            ZoneRoot root = (ZoneRoot)Parent;
            return root.GetAdjacentZoneNamesBeyondRange(ArraySubscript[0], ArraySubscript[1], ArraySubscript[2], range);
        }

        
        
        // NOTE: By the time we call this, the server will have already validated the
        // length of the entity's movement.
        //
        // An entity traversing the boundaries of a Zone must land in a neighboring Zone
        // or if edge of world boundary is reached, their position halted at that boundary
        // and remaining in the current Zone.  They must never recurse up and land in the ZoneRoot.
    
        // I thought about using Portals to connect Zones but it offers no advantage.
        // In fact Portals add the complexity of connectiong the Zones as they are loaded
        // to any existing Portals.

        // However, the idea of Portals made it obvious that Zone siblings should manage the 
        // crossing of boundaries themselves and not the ZoneRoot.  This makes sense in that
        // the current region knows whenever one of it's child entities is leaving it's own zone.
        // It's possible at that point for that child region to then defer to a parent region
        // to determine the new region to place it.  But below we do that determine ourselves
        // for sibling zones

        // Of course eventually our Interior to Exterior boundary crossing will rely on
        // Portals.

        // TODO: we could add a bool to cancel if it exceeds game world or bounds
        // and return true/false if this function succeeds?   It would help there to defer to 
        // the parent root zone
        // 
        // Called via Simulation.Update()
        //				scene.FinalizeEntityMovement()
        //					scene.EntityMoved()
		//	        			entityNode.OnEntityMoved()
        internal override bool ChildEntityBoundsFail(Entity child, out Region newParentRegion)
        {
            newParentRegion = this; // initialize with existing region which we'll return if we do not fail
            if (mBox.Contains(child.DerivedTranslation)) return false;

            if (child is ModeledEntity)
            {
            	// NOTE: if you expect a specific child to be processed here but you never see it
            	//       make sure that the child is not actually child of a lower entity like TileMap.Structure
            	//       rather than Zone!  
            	//       TODO: but consider what happens when NPC within Structure tries to move.. it does
            	//       still have it's EntityNode.OnEntityMoved() fired however, we are currently limiting
            	//       proper handling within it's SceneNode if the child's parent is not Region/Zone. And
            	//       that should not be allowed because we want child entities of entities to get proper 
            	//       handling when they move too.
//            	ModeledEntity modeledEntity =  (ModeledEntity)child;
//            	if (modeledEntity.Model != null)
//            		if (modeledEntity.Model.Geometry != null && modeledEntity.Model.Geometry is Elements.Mesh3d)
//            		{
//            			Elements.Mesh3d mesh = (Elements.Mesh3d)modeledEntity.Model.Geometry;
//            			if (mesh.ID == "common.zip|actors/infantry_bot_mk4.obj")
//            			{
//            				System.Diagnostics.Debug.WriteLine ("Zone.ChildEntityBoundsFail() - bounds testing...");
//            			}
//            		}
            }
            
            Vector3d position = child.DerivedTranslation; // derivedTranslation is the correct Region space coordinate for any entity in a region.
            Vector3d max = mBox.Max;
            Vector3d min = mBox.Min ;
            Vector3d dimensions = max - min;
            Vector3d radius = dimensions * .5d;

            // how far have we traversed beyond this region's boundary
            Vector3d excessDistance;
            excessDistance.x = 0;
            excessDistance.y = 0;
            excessDistance.z = 0;
            
            int subscriptX = ArraySubscript[0];
            int subscriptY = ArraySubscript[1];
            int subscriptZ = ArraySubscript[2];
            
            ZoneRoot root = (ZoneRoot)Parent;
            if (root == null) throw new Exception ("Zone.ChildEntityBoundsFail() - ZoneRoot cannot be null.");

            //float offsetX = Offset[0];
            //float offsetY = Offset[1];
            //float offsetZ = Offset[2];

            //            float leftNeighbors = root.StartX - Offset[0]; // # zones available along negative X
            //            float rightNeighbors = root.StopX - Offset[0]; // # zones available along positive X
            //            float upNeighbors = root.StartY - Offset[1];   // # zones available along negative Y
            //            float downNeighbors = root.StopY - Offset[1];  // # zones available along positive Y
            //            float frontNeighbors = root.StartZ - Offset[2];// # zones available along negative Z
            //            float backNeighbors = root.StopZ - Offset[2];  // # zones available along position Z

            bool newParentX = false;
			bool newParentY = false;
			bool newParentZ = false;

			// determine if we've crossed Zone boundaries and need to switch Zones
			// and compute a new translation based on new Zone's coordinate system.
            double result;
            int count = 0;
            ///////////////////////////////////////////////////////////////////
            // X AXIS
            ///////////////////////////////////////////////////////////////////
            if (position.x > radius.x)
            {
                excessDistance.x = position.x - max.x;
                count = (int)Math.Ceiling(excessDistance.x / dimensions.x);
                subscriptX += count;
                newParentX = DoPositiveAxisUpdate(ref subscriptX, (int)root.RegionsAcross - 1, excessDistance.x, dimensions.x, radius.x, min.x, max.x, out result);
                position.x = result;
            }
            else if (position.x < -radius.x)
            {
                excessDistance.x = position.x - min.x;
                count = (int)Math.Floor(excessDistance.x / dimensions.x);
                subscriptX += count;
                newParentX = DoNegativeAxisUpdate(ref subscriptX, 0, excessDistance.x, dimensions.x, radius.x, min.x, max.x, out result);
                position.x = result;
            }

            ///////////////////////////////////////////////////////////////////
            // Y AXIS
            ///////////////////////////////////////////////////////////////////
            if (position.y > radius.y)
            {
                excessDistance.y = position.y - max.y;
                count = (int)Math.Ceiling(excessDistance.y / dimensions.y);
                subscriptY += count;
                newParentY = DoPositiveAxisUpdate(ref subscriptY, (int)root.RegionsHigh - 1, excessDistance.y, dimensions.y, radius.y, min.y, max.y, out result);
                position.y = result;
            }
            else if (position.y < -radius.y)
            {
                excessDistance.y = position.y - min.y;
                count = (int)Math.Floor(excessDistance.y / dimensions.y);
                subscriptY += count;
                newParentY = DoNegativeAxisUpdate(ref subscriptY, 0, excessDistance.y, dimensions.y, radius.y, min.y, max.y, out result);
                position.y = result;
            }
            ///////////////////////////////////////////////////////////////////
            // Z AXIS
            ///////////////////////////////////////////////////////////////////
            if (position.z > radius.z)
            {
                excessDistance.z = position.z - max.z;
                count = (int)Math.Ceiling(excessDistance.z / dimensions.z);
                subscriptZ += count;
                newParentZ = DoPositiveAxisUpdate(ref subscriptZ, (int)root.RegionsDeep - 1, excessDistance.z, dimensions.z, radius.z, min.z, max.z, out result);
                position.z = result;
            }
            else if (position.z < -radius.z)
            {
                excessDistance.z = position.z - min.z;
                count = (int)Math.Floor(excessDistance.z / dimensions.z);
                subscriptZ += count;
                newParentZ = DoNegativeAxisUpdate(ref subscriptZ, 0, excessDistance.z, dimensions.z, radius.z, min.z, max.z, out result);
                position.z = result;
            }


            // TODO: i think i have a problem here when it comes to entities being added to Structures are concerned.
            //       The entities are moving across Zones and then having their new parent being a different Zone instead of the Structure of that Zone.
            //       But the question then also becomes, why doesn't it still work to have this occur?
            bool newParent = (newParentX || newParentY || newParentZ);
            
            if (newParent)
            {
            	//if (offsetX != Offset[0] || offsetY != Offset[1] || offsetZ != Offset[2])
            	//{
            		// TODO: if sudden debug breakpoint is hit while camera was moving, when we resume the subscript calc will be off.. as if moving entire time.
            		//        that will result in null newParentRegion since we haven't actually traveled there yet
            		string newParentRegionName = ((ZoneRoot)mParents[0]).GetZoneName(subscriptX, subscriptY, subscriptZ);
            	    //  TODO: what if the adjacent soon to be "newParentRegion" zone isn't paged in yet?  Indeed, this is a real issue...
            	    newParentRegion = (Region)Resource.Repository.Get(newParentRegionName);
            	    System.Diagnostics.Debug.Assert (newParentRegion != null, "Zone.ChildEntityBoundsFail() - New Region should not be null");
            	    System.Diagnostics.Debug.Assert (this != newParentRegion, "Zone.ChildEntityBoundsFail() - New Region should not be same as previous entity.Region");
            	//}
            	//else newParent = false; // we must have hit hard world boundary and cannot proceed in this direction any further
            }
            

            // assign the final position
            child.Translation = position;
            return newParent;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="neighborCount">The number of neighbors toward positive direction on this particular axis of the current region</param>
        /// <param name="excessDistance"></param>
        /// <param name="diameter"></param>
        /// <param name="radius"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private bool DoPositiveAxisUpdate(ref int offset, int maxOffset,  
            double excessDistance, double diameter, double radius, 
            double min, double max, out double result)
        {
            result = 0;
            if (offset > maxOffset)
            {
                // cap it at furthest possible boundary
                offset = maxOffset; // Offset[0] + neighborCount;
                result = max;
                return false;
            }
            else // there are indeed enough neighbors to this side
            {
                double remainder = Math.IEEERemainder(excessDistance, diameter);
                if (remainder > radius)
                    result = remainder - radius;
                else
                    result = min + remainder;
                
                return  true;
            }
        }

        private bool DoNegativeAxisUpdate(ref int offset, int minOffset,
            double excessDistance, double diameter, double radius,
            double min, double max, out double result)
        {
            result = 0;
            ZoneRoot root = (ZoneRoot)Parent;
            if (offset < minOffset)
            {
                // cap it at furthest possible boundary
                offset = minOffset; // Offset[0] + neighborCount;
                result = min;
                return false;
            }
            else // there are indeed enough neighbors to this side
            {
                double remainder = Math.IEEERemainder(excessDistance, diameter);
                if (remainder > radius)
                    result = remainder - radius;
                else
                    result = max + remainder;
                
                return true;
            }
        }

        /// <summary>
        /// Todo: i'm still debating on this.
        /// Ideally when you consider entities moving throughout the game world
        /// boundary traversing to other regions should be handled by the SceneNodes
        /// since they do spatial relationships
        /// Also you'd want it to be able to work comprehensively with collisions.
        /// Currently one thing i do is in Simulation.cs i will flag objects that have moved
        /// Further, an entity like a sword attached to a player that crosses a zone
        /// we want hierarchically the sword to still be attached to the player but
        /// scenenode wise for the sword to always be in world coordinates in the region.
        /// So one of the rules we must have is if an entity is a child of another entity
        /// that is NOT a type of region then that entity should never be re-parented to
        /// but keep it's existing.  The only time you re-parent an entity is when it's
        /// current parent is a region and it has crossed a regional boundary.  
        /// Ideally then we want our SceneNode's to deal with these determinations and 
        /// notifications... 
        /// Now that's different from what i do where i set a bunch of changeflags
        /// and then itterate through "everything".  The change flags i think are ok
        /// but 1) sometimes you'll want to respond immediately to a change event such as a
        /// collision response that needs to be handled so that physics can resume (such as
        /// fast collision check that collides mid tick and needs to resume).
        /// To avoid re-queuing an object for update by the Listener (eg. simulation) we can
        /// do Entity.ChangeFlags |= ChangeFlags.QueuedForUpdate and then check
        /// if Entity.ChangeFlags &= ChangeFlags.QueuedForUpdate == ChangeFlags.QueuedForUpdate) skip
        /// Interestingly, their spatial "Nodes" also include the LightList since that
        /// i guess is determined by Spatial Rules... hrm...
        /// So I'm kinda liking this here... we create SceneNodes in Simulation.cs so
        /// why not also supply the listener (Simulation.IListener) to it as well.
        /// </summary>

        #region IBoundVolume Members
        protected override void UpdateBoundVolume()
        {
            // Zones regions are fixed size
            if ((mChangeStates & Enums.ChangeStates.BoundingBoxDirty) == Enums.ChangeStates.BoundingBoxDirty)
            {
                // TODO: i believe the BoundingBox.Transform line below is wrong.  All regions are
                // culled (bounds tested) in that Region's space.  
                //_box = BoundingBox.Transform(_box, RegionMatrix);

                mSphere = new BoundingSphere(mBox);
                _maxVisibleDistanceSq = mBox.Diameter * 2;
                _maxVisibleDistanceSq *= _maxVisibleDistanceSq;
                DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
            }
        }
        #endregion

    }
}
