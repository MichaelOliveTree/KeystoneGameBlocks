//using System;
//using System.Collections.Generic;
//using Keystone.Elements;
//using Keystone.Entities;
//using KeyCommon.Helpers;

//namespace Keystone.SceneNodes
//{
//    /// <summary>
//    /// All cells during build mode are instantiated.  During simulation/play
//    /// of the completed design, cells that are outside of the design's closed
//    /// volume of cells are pruned and set to null.  
//    /// </summary>
//    internal class Cell
//    {
        
//        internal uint ID;       //TODO: i would like to make these ushort instead to half memory use
//        internal uint VolumeID; 
//        internal byte Flags;


//        /// <summary>
//        /// These arrays are with respect to each cell.  Thus, a wall that is shared by
//        /// two cells, will each have a partition entry made.  Yes?
//        /// 
//        /// This array is not initialized for empty Cells.
//        /// The elements stored in these face partitions are
//        /// arraned 0 - 6 as front, back, left, right, top, bottom.
//        /// Actually isn't the following correct based on the vertex corner order where bottom left is corner 0
//        /// and thus wall 0 is comprised of corners 0 and 1 which make up left wall
//        /// [0] = left edge
//        /// [1] = front edge
//        /// [2] = right edge
//        /// [3] = back edge
//        /// [4] = floor     
//        /// [5] = ceiling
//        /// If any particular partition is null, that array subscript will
//        /// be null.
//        /// </summary>
//        /// <remarks>
//        /// Based on the cell.ID and the partition subscript, we can dynamically determine the edge ID.
//        /// </remarks>
//        internal Entities.ModeledEntity[] Partitions; // walls and floor entities.
//        internal Entities.ModeledEntity[] Access;     // access openings through partitions (uses CSGStencil to render openings)
//        internal Entities.Entity[] StaticItems; // items when destroyed can be repaired and are not removed from the array
//        internal Entities.Entity[] DynamicItems; // items that are transient like item drops
//        internal Entities.Entity[] Characters; // characters are always transient

//        // NOTE: componentType is Entity.UserTypeID which is set via a combobox EntityEditPlugin General tab.
//        internal void AddPartition(CelledRegion parent, Entities.Entity entity, uint componentType)
//        {
//            int partitionIndex = 0;
//            // TODO: test partition indices by placing 4 walls at each side of a cell
//            // and a door on each and verify that the edge we compute from the rotation of the wall
//            // is correct.
//            if (componentType > 1)
//            {
//                Keystone.CSG.CellEdge edge = parent.GetEdge(entity.CellIndex, entity.CellRotation);
//                System.Diagnostics.Debug.WriteLine("AddPartition() - EdgeID = " + edge.ID.ToString());

//                // based purely on the .CellRotation we can know which partition index it should go
//                // and thus which wall entity to set a csgtarget flag on if that wall has a csgaccept
//                // flag set


//                switch (entity.CellRotation)
//                {
//                    case 0:   // 0 or 360 degrees = left edge
//                        partitionIndex = 0;
//                        break;
//                    case 32:  // 45 degrees

//                        break;
//                    case 64:  // 90 degrees = top edge
//                        partitionIndex = 1;
//                        break;
//                    case 96:  // 135 degrees
//                        break;
//                    case 128:  // 180 degrees = right edge
//                        partitionIndex = 2;
//                        break;
//                    case 160: // 225 degrees
//                        break;
//                    case 192: // 270 degrees = bottom edge
//                        partitionIndex = 3;
//                        break;
//                    case 224: // 315 degrees
//                        break;
//                    default:
//                        throw new Exception();
//                }
//            }
            
            

//            // TODO: the following can be added into the .Add(entity) method
//            // componentType can be passed in. For partitions, which wall or celing can be determined
//            // by the rotation uint value.
//            switch (componentType)
//            {
//                case 0: // floor // floor & ceiling are conceptually same thing just 1 entity with two models so the sides can be given different appearance/materials
//                case 1: // ceiling
//                case 2: // wall
                                    
//                    // i'm starting to think that the .CellIndex to place
//                    // this into should be computed from the x,y,z position deserialized
//                    // Normally this is not a problem since we're guaranteed to deserialize
//                    // coords prior to AddChild back to parent, but the DomainObject is not!
//                    // But this is a difference between designing a ship and needing domainobject
//                    // for validation, and then loading a ship design received from the server.
//                    // In other words, we dont rely on user's version of the designed ship anyway!

                    
//                    // is this a partition, static component(engine block), dynamic item (gun on floor) or character?
//                    if (Partitions == null) Partitions = new Keystone.Entities.ModeledEntity[6];

//                    Partitions[partitionIndex] = (ModeledEntity)entity;

//                   // ((CelledRegionNode)entity.Parent.SceneNode).UpdateVolumes();
//                break;

//                case 3: // wall door/hatch
//                case 4: // window
//                case 5: // floor/ceiling hatch
//                    if (Access == null) Access = new Keystone.Entities.ModeledEntity[6];

//                    Access[partitionIndex] = (ModeledEntity)entity;
//                    // an Access entity on this index should never get this far unless footprint test verifies
//                    // there is already a partition here
//                    System.Diagnostics.Debug.Assert(Partitions != null && Partitions[partitionIndex] != null);

//                    //if (entity.CellIndex == -1) throw new Exception();
//                    //TODO: if a partition already exists here, do we replace it?


//                    // query the entity type to see if it's a partition.
//                    // We should track partitions because we want to be able to
//                    // pair them with their CSG stencils

//                    // TODO: assert that this partition slot is already null?
//                    // TODO: maybe we don't need them by ID though... just as a list...
//                    // because our tileflags will be used to determine if a wall exists on that side
//                    //... but the main thing is associating our stencil punches with the proper wall
//                    // Partitions[(int)partition] = entity;

//                    // if these cells were maintained by CelledRegion entity this would be easier
//                    // but then how would that spatial info be handled by the CelledRegionNode during
//                    // cull queries 

//                    // well if the CelledRegionNode has a list of CelledEntityNodes
//                    // by cell ID and then adding a new CelledEntityNode it checks the
//                    // entity's spanned indices to know which one to put it under...


//                    // TODO: when it comes to rendering our doors and such, we now have more information
//                    // for our scaledrawer to figure out which doors match with which entity
//                    // since they are now in the same cell and possibly the adjacent cell sharing that wall also

//                    //
//                    //
//                    // TODO: for physical rperesentations of walls and floors, should i enforce
//                    // players start from either center or bottom floor and then only allow floors
//                    // above if that floor is supported on at least two sides by a wall or a floor
//                    // that is also supported on two sides (not counting the current side) by a wall or 
//                    // a floor ETC!  Thus we know when to make upper floors collapse
//                    //
//                    // if a csg source is added to an edge stack that already contains a wall
//                    // then set that wall's CSGStencilTarget flag.
//                    // if a wall is added to an edge stack that already contains a door
//                    // then set that wall's CSGStencilTarget flag.  Normally you would think
//                    // the door can't be added prior to the wall, but there's a possibilty perhaps
//                    // during XML load.  So for now we'll allow it.
//                    ModeledEntity partition = Partitions[partitionIndex];
//                    if (partition == null) break;
//                    Model[] targetModels = ((ModeledEntity)partition).SelectModel(SelectPass.CSGStencilAccept, 0);
//                    if (targetModels == null || targetModels.Length == 0) break;
                    
//                    Model[] sourceModels = ((ModeledEntity)entity).SelectModel(SelectPass.CSGStencilSource, 0);
//                    if (sourceModels == null || sourceModels.Length == 0) break;

//                    for (int i = 0; i < targetModels.Length; i++)
//                    {
//                        targetModels[i].SetFlagValue(
//                            (uint)Keystone.Elements.Model.ModelFlags.CSGStencilTarget, true);

//                        // TODO: this flag needs to be applied recursively
//                        // and it needs to be applied to any new children added to that entity
//                        // or any of it's children!  
//                        // TODO: but do we take into account the geometry of the stencil source?
//                        // since here we do add all targets to different render buckets
//                        // TODO: next major question is, if we wind up having to render
//                        // sources and targets together with the specific target assigned,
//                        // how do we achieve this?
//                        // NOTE: we cannot assign a stencil reference value here.  There's only
//                        // 255 values (0 is reserved for empty) and during editing who knows
//                        // how many doors and walls an entire ship might have.  So if we wanted to use
//                        // ref values, we'd have to pick the closest visible csg sources  and start numbering
//                        // up to max of 255 from there (recycling 255 at that point probably would be fine 
//                        // and result in unnoticeable errors as any geometry you saw thru them would be far away)
//                        // In any case, the result is that we have to assign the ref numbers during render
//                        // So that means each CSG Source must know it's target.  There's no way around it.
//                        // So the questoin is, how do we manage that?
//                        // eg. entity.RegisterCSGTarget(wallEntity)
//                        //             - that recursively registers childs that do have CSGAvailable == true
//                        //             - how do we unregister these? say if i a target gets destroyed? How do
//                        //               we remove the wallEntity reference from the entity?

//                        // TODO: Perhaps something we can do instead is, set a CSGStencilSource flag fine
//                        //       and set a flag for a potential target, and THEN when our ScaleCuller.cs
//                        //       traverses a Cell, it can couple in the Visibility, the pairs itself.
//                        //       since we know all pairs will be within the same cell... erm... well
//                        //       and neihboring... right?  This way in my scale culler i can group them...
//                        //       perhaps i can even group them here in the cell ahead of time!  
//                        //       The good news is that there wont be many visible doors at once... like even
//                        //       20 visible (whihc is unlikely) would be relatively few in the overall scheme 
//                        //       of things I think.
//                    }
//                    break;

//                case 6: // component
//                    break;

//            }



//        }

//        internal void Remove(Entities.Entity entity)
//        {
 
//        }

//    }




//    /// <summary>
//    /// Volume's are  just groupings to make queries faster 
//    /// (eg. collisions, culling, etc)
//    /// Volumes will be maintained by the CelledRegion
//    /// </summary>
//    internal class Volume
//    {
//        // what if instead of having a list of Cells, it instead has
//        // a list of cell ID's and a list of all the SceneNodes that
//        // are in all of those cells?

//        // our main objective is to spatially divide the SceneNodes contained
//        // within this CelledRegionNode.

//        internal uint ID;
//        // The volume is made up of the cells sizes and not
//        // by the sizes of entities held in the volume.
//        private List<Cell> mCells;

//        //internal string Name;
//        //internal int ID; // since Entities will technically ahve CelledRegion as parent, we can assign this ID to the Entity too so we know which volume

//        //internal CelledRegion mParent;

//        //// two child volumes.
//        //// So typically the CelledRegion will have 
//        //// an array of Volumes where each volume represents one floor
//        //// If floors are added or removed, a corresponding array element
//        //// is added or removed.

//        //internal BoundingBox mBox;
//        //internal uint[] Cells;  // the indices of all cells contained in the ship
//        //// to which belong to this volume (but not of it's child volumes?).
//        //// Correct.  Child volumes should have their cells removed
//        //// from this array of indices

//        //Dictionary<uint, Stack<Entities.Entity>> mEntities;  // child entities of this CelledRegion will be stored by volume.


//        internal bool Contains(uint cellID)
//        {
//            if (mCells == null) return false;
//            foreach (Cell c in mCells)
//                if (c.ID == cellID) return true;

//            return false;
//        }

//        internal void Add(Cell cell)
//        {
//            if (mCells == null) mCells = new List<Cell>();
//            mCells.Add(cell);
//        }

//        internal void Remove(Cell cell)
//        {
//            if (mCells == null || mCells.Contains(cell) == false) throw new ArgumentOutOfRangeException();
//            mCells.Remove(cell);
//        }


//        //internal Volume Split(Edge e)
//        //{
//        //    // remove cells on the other side of the edgelist into a new volume
//        //    // and return it?
//        //    throw new NotImplementedException();
//        //}

//        internal void Combine(Volume v)
//        {
//            // add all cells from this Volume into this one.
//            throw new NotImplementedException();
//        }

//        // see Region.ChildEntityBoundsCheck()
//        // for thoughts on how to do child Entity movement tracking across cells, volumes, regions

//        //internal Keystone.Collision.PickResults Collide(Keystone.Types.Vector3d start, Keystone.Types.Vector3d end, Keystone.Types.Matrix worldMatrix, Keystone.Collision.PickParameters parameters)
//        //{
//        //    Vector3d intersectionPoint;
//        //    PickResults result = new PickResults();
//        //    result.HasCollided = false;

//        //    // first test the entire volume
//        //    if (mBox != null && mBox.Intersects(new Ray(start, end - start), 0, 1000000d))
//        //    {
//        //        // test the individual cells and the faces of that cell depending on the parameters
//        //        if (Cells != null)
//        //            for (int i = 0; i < Cells.Length; i++)
//        //            {

//        //            }
//        //    }
//        //    return result;
//        //}





                   


//        //// hrm... the original idea of course was that we'd have a
//        //// type of 2d map, but we do need actually a 3d map for our
//        //// collaborative diffusiion map
//        //// Diffusion maps main aspect is they contain weighted info
//        //// and are these 2d arrays that can be easily run computations against.
//        //// In that sense, they simplify a lot... and make it agnostic to
//        //// "entities" and such.  Instead it's the entities that create the map
//        //// 

//        //// THE KEY OBJECTIVE IS TO BE ABLE TO CULL SPATIALLY BY FLOOR
//        //// AND ROOMS AND MAYBE ALLOW FOR PORTAL VISIBILITY WITH DOORS/WINDOWS TO ADJACENT ROOMS
//        //// WE CAN USE A SPECIAL TYPE OF PORTAL.  DOESN'T HAVE TO BE SAME KIND AS GENERAL PORTAL
//        //// struct Cell
//        //// {
//        //// whata bout the idea of having seperate layer for all?
//        //// this shouldn't be bad right because all this is really is
//        //// where we store our entities and is independant of our diffusion maps
//        //// The diffusion maps are strictly for running algorithms against like
//        //// air ventilation, fire travel, ai movement, electrical and plumbling links
//        //// SO in that sense, our diffusion maps are created and manipulated here
//        //// but the algorithms then run against them.
//        //// Entity[] Floors
//        //// Entity[] Ceilings
//        //// Entity[] WallLeft  // but does this mean the entity then has to exist as both a left and right since walls fall on edges and are shared?
//        //// Entity[] WallRight
//        //// Entity[] WallFront
//        //// Stack<Entity>[] Components 
//        //// Or the above can be placed into a larger "Cell struct"
//        //// }
//        //private Volume[] Volumes;   // an array of indices into our cells array
//        //// can be used to define a Volume
//        //// such that these locations point into
//        //// an array element in each layer
//        //// NOTE: Volumes can extend through multiple layers
//        //// such as an engineering room that is 2 floors high.
//        //// However, a "floor" is still something that we will use
//        //// because a floor determines the current Y level we're editing
//        //// or viewing in our 3d viewport.
//    }
//}


//////private class CellNode : Keystone.Elements.SceneNode
//////{
//////    // stack of entities in this cell
//////    // what's the advantage to having a specific cell node instead?
//////    // none.  Hell i think the entire point of creating that flat array
//////    // structure was to avoid it.  
//////    // I think originally we just wanted to define volumes by indices of the
//////    // cells taht were in it.
//////    // And what were we planning on doing with walls and floors/ceilings?
//////    // Since these straddled edges, which stack would they exist in?
//////    // Well, I think edges helps us determine which cells to place in
//////    // but in the end, there are interior walls and exterior walls and
//////    // that can be easily determined which is which.  
//////    // Interior walls can have different textures on each side precisely because
//////    // they are comprised of two seperate walls meshes.
//////    // Thus the actual mesh and texture and material of a wall can be changed out
//////    // dynamically if using a material brush Widget or 
//////    // **KEY: Interior walls must be double sided, and spatially they exist in both
//////    // cells.  Or should they be two seperate entities?
//////    // which would be easier/make more sense as far as damage handling, user 
//////    // design editing ease of use
//////    // **KEY2: Having the walls strictly associated with a cell can make it easier
//////    // to do things like Vents.
//////    List<Keystone.Elements.SceneNode> mElements;

//////    // why do i need these seperate vars?  Well, i think one
//////    // idea is that when determining if a user is replacing an existing or adding
//////    // for first time one of these elements, you'd need to have them seperate.
//////    // But maybe just looping through an array of all mElements is just fine. Afterall
//////    // we're not changing things that much... it's not going to be speed critical
//////    // and better to have simpler code which is an array.
//////    //Keystone.Elements.SceneNode Floor;
//////    //Entities.Entity Ceiling;
//////    //Entities.Entity LeftWall;



//////    public SceneNode[] Elements
//////    {
//////        get
//////        {
//////            if (mElements == null) return null;
//////            return mElements.ToArray();
//////        }
//////    }
//////}

/////// <summary>
/////// VolumeNode's inherit directly from SceneNode and not EntityNode and so
/////// has no Entity itself.
/////// VolumeNode's are not managed by the Simulation but rather
/////// the CelledRegionNode itself.  In this way, the Simulation can still
/////// only worry about specific entities and not at how those entities are arranged
/////// spatially.  This is similar to how Octrees are managed.  The simulation
/////// doesnt have to worry about adding/removing child SceneNode's in the octree.
/////// It's done transparently.
/////// </summary>
/////// <remarks>
/////// The Traversers can choose to get .Children or .Volumes[i].Children.  
/////// This gives a traverser a chance to cull by volume first and then to continue
/////// traversing only children of volumes that are visible (relevant)
/////// 
/////// Nested Volumes - For vents, or rooms inside of rooms, are represented as
/////// nested volumes.  Thus culling can be done hierarchically for those.
/////// </remarks>
////private class VolumeNode : Keystone.Elements.SceneNode
////{
////    // note: I think all a VolumeNode really needs is the cells and
////    // to then just have the SceneNodes which normally would be directly under CelledRegionNode
////    // to instead be placed in their appropriate cells.  Then we only allow
////    // dynamic entities to exist in multiple volumes... otherwise a static component like
////    // a ship's reactor must exist in a single volume....
////    // But what about a volume that exists over multiple floors like a tall reactor
////    // that sits on 1st floor but extends up to 2nd or higher?
////    // Whn you're filtering all floors but the 2nd, do we see the top of the reactor that extends
////    // to 2nd floor or not?
////    //
////    // Recall ideally i wouldnt need "volumes"
////    // But spatially for portals and for culling they are very useful.  So this
////    // is all my goals for volumes should be... not for hierarchical locations but
////    // purely spatial grouping.  And the necessary portals would be generated
////    // by the spatial nodes automatically when a "door" or "window" type component was placed
////    // on a cell edge... if that edge was part of a closed volume.  Because of this
////    // There is no need to store portal info in the save file!  This is key.  These are
////    // special types of portals and not the same as our other kind.  Call them
////    // VolumePortals or CellPortals for instance.
////    //
////    // Any particular cell can only ever be apart of one volume.  Components placed in a 
////    // a cell however can take up mulitple cells and thus multiple volumes.
////    // However one of our proposed fixes was that an entity that takes up multiple volumes
////    // would itself consist of multiple sub-entities.  In this way, a tall reactor
////    // would be comprised of a bunch of "blocks" that then could fill up just one cell.
////    // The definition would describe which cells it occupied from the origin cell.
////    //
////    // I dont think CelledRegion entity needs to worry about any of that.  Here is also
////    // where entity stacks are maintained as that too is a spatial query that really is only
////    // needed at runtime.  
////    private class Cell
////    {
////        internal List<SceneNode> mSceneNodes;
////    }

////    private Vector3d mCellSize;

////    // The volume is made up of the cells sizes and not
////    // by the sizes of entities held in the volume.
////    private List<Cell> mCells;


////    #region Node Members
////    public override object Traverse(ITraverser target, object data)
////    {
////        return target.Apply(this, data);
////    }
////    #endregion



////    #region IBoundVolume Members
////    protected override void UpdateBoundVolume()
////    {
////        _maxVisibleDistance = 0;

////        if (ChildCount > 0)
////            for (int i = 0; i < _children.Count; i++)
////                _maxVisibleDistance = System.Math.Max(_maxVisibleDistance, _children[i].MaxVisibleDistance);


////        // no children means there are no child SceneNodes 
////        if (_children != null && _children.Count > 0)
////        {
////            // continue to include other Node's bounds into our final
////            // NOTE: The bounding volume of any children SceneNodes (entity and other SceneNodes) 
////            // are already in Region specific coordinates so there is no need to transform the box's
////            // coordinates.
////            // Recall that I decided EntityBounding volumes would always be in the coordinate system
////            // of the region its most directly associated.  This means even a sword attached to an 
////            // Actor will have the sword bounding volume in Region coordinates.
////            foreach (SceneNode child in _children)
////            {
////                // NOTE: Here we must skip child RegionNodes if the entity of this EntityNode
////                // is a Container which makes any RegionNode the RegionNode for an Interior.
////                // And Interior's have no bearing on the bounding box of the Vehicle\Container.
////                // NOTE: We tried not even connecting the interior's regionNode to the Vehicle\Container's
////                // EntityNode and instead relying on PortalNode traversal to render Interiors, 
////                // but that made it difficult to do Isometric view rendering.
////                // Simply excluding the interior's boundingbox from being combined here with
////                // the EntityNode for this Container is better.  
////                // NOTE: An exterior mesh however is required or the boundingbox will always be 
////                // empty.
////                if ((child is EntityNode) == false)
////                    continue;

////                _box = BoundingBox.Combine(_box, child.BoundingBox);
////            }
////        }

////        _sphere = new BoundingSphere(_box);
////        DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty | Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly);
////    }
////    #endregion
////}