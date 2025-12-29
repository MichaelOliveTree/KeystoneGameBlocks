using System;
using System.Collections.Generic;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Collision;
using Keystone.Elements;
using keymath.Primitives;

namespace Keystone.SceneNodes
{
    // TODO: July.7.2011 - actually, im going to experiment with moving the cell arrays from CelledRegion
    // to this CelledRegionNode.  
    // TODO: June.30.2011 - this class is likely obsolete now that CelledRegion will contain
    // the array of cells and cell layers without needing a special CelledRegionNode
    // TODO: Actually, I think a CelledRegionNode should allow for grouping of cells
    // into floors and rooms.
    // It will still be a single CelledRegionNode and will not contain sub-SceneNodes,
    // but it will contain structs for subdividing mCells
    // and mChildStacks
    // Each floor tile can contain an "id" for the room it's in which is maintained automatically.
    // But this provides a quick way for any entity standing in a cell to know where it is.
    // These "ids" can be used against a lookup table for a name for each room that the user
    // can customize.
    // There is no problem with rooms within rooms either since they arent represented in a nested fashion.
    // They are just like side by side rooms as far as how the code treats them.
    // A cell with a diagonal wall that exists in two rooms can note this.
    public class CelledRegionNode : RegionNode 
    {

        //PickResults _lastPickResult;       
        //private Volume[] mVolumes;
        // TODO: if CelledStructure is ModeledEntity then
        //          we must allow for non RegionNodes (eg EntityNodes) to potentially
        //          contain a SpatialStructure
        public CelledRegionNode(Interior celledRegionEntity)
            : base(celledRegionEntity)
        {

            //UpdateVolumes();
           
            // NOTE: We do not have "Entity" rooms or "Entity" floors that 
            // are children of the Interior region because that would result in all components
            // placed in those rooms and floors to have positions relative to those rooms
            // and floors since child entity positions are always relative to the parent
            // and we want for all child components in an interior to be child of the entire
            // interior.
            //
            // Thus we will use the CelledRegionNode to partition into volumes
            // which represent floors and rooms.
            // note; actually we're just going to use a quadtree here because
            // volumes are only useful for first person portal rendering, but we'll be iso top down
            if (celledRegionEntity.MaxSpatialTreeDepth > 0)
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

                // quadtreecollection allows a seperate quadtree for each floor
                // whilst still maintaining the ISpatialNode interface
                mSpatialNodeRoot = new Keystone.QuadTree.QuadtreeCollection(celledRegionEntity);
            }
        }
                

        public override void  AddChild(SceneNode child)
        {
            System.Diagnostics.Debug.Assert(child is EntityNode);

 	        if (!(child is CellSceneNode )) throw new ArgumentOutOfRangeException ();

            // base.AddChild will result in call to mSpatialNodeRoot.Add()
            base.AddChild(child);
        }

        // this overrides RegionNode.RemoveChild() which does exactly what we need.
        //public override void RemoveChild(SceneNode child)
        //{
        //    base.RemoveChild(child);

        //    if (mSpatialNodeRoot != null)
        //    {
        //        mSpatialNodeRoot.RemoveEntityNode((EntityNode)child);
        //    }
        //}


        //// TODO: note most below code for per face collision is from EditableMesh
        //// note: having our ship divided into volumes can speed up our collision testing
        //public Keystone.Collision.PickResults Collide(Keystone.Types.Vector3d start, Keystone.Types.Vector3d end, Keystone.Types.Matrix worldMatrix, Keystone.Collision.PickParameters parameters)
        //{
        //    // TODO: shouldn't all our SceneNode's also have Collide functions?
        //    // 
        //    // TODO: If the start/end ray is in Region coordinates
        //    // then determining the selected should be trivial to determine yes IF we're
        //    // in the bounds of this region at all.
        //    // 
        //    //
        //    Vector3d intersectionPoint;

        //    _lastPickResult.HasCollided = false;

        //    // a volume represents a volume and is used for spatial partitioning
        //    // both for faster picking and faster rendering.  The key point however is that
        //    // a Volume simply maps to a part of the underlying map structure.  
        //    // How do we do that without having copies of those cells in memory?
        //    // Well, one way is for a volume to be represented by an array of indices
        //    // into the primary cell listing...

        //    //if (_cell.Faces == null) return _lastPickResult;

        //    //if (parameters.Accuracy == PickAccuracy.BoundingBox || parameters.Accuracy == PickAccuracy.BoundingSphere)
        //    //{
        //    //    _lastPickResult.HasCollided = true;
        //    //    _lastPickResult.Geometry = this;
        //    //    _lastPickResult.CollidedObjectType = CollidedObjectType.EditableMesh;
        //    //    return _lastPickResult;
        //    //}

        //    //if (parameters.Accuracy == PickAccuracy.Face)
        //    //{
        //    _lastPickResult.ImpactNormal = Vector3d.Normalize(start - end);
        //    Ray r = new Ray(start, _lastPickResult.ImpactNormal);

        //    // transform the ray into modelspace
        //    //r = r.Transform(Matrix.Inverse(worldMatrix));

        //    Vector3d[] polyPoints = new Vector3d[4]; // four because we want to be able to pick any part of quad, not just triangle

        //    // iterate through all cells
        //    for (uint x = 0; x < CellCountX; x++)
        //        for (uint y = 0; y < CellCountY; y++)
        //            for (uint z = 0; z < CellCountZ; z++)
        //            {
        //                double halfWidth = CellWidth * .5;
        //                double halfLength = CellLength * .5;

        //                // based on current cell, compute the polypoints of the cell
        //                polyPoints[0].x = x * CellWidth; // -halfWidth;
        //                polyPoints[0].y = y * CellHeight;
        //                polyPoints[0].z = z * CellLength; // -halfLength;


        //                polyPoints[1].x = x * CellWidth + CellWidth; // halfWidth;
        //                polyPoints[1].y = polyPoints[0].y;
        //                polyPoints[1].z = polyPoints[0].z;

        //                polyPoints[2].x = polyPoints[1].x; // halfWidth;
        //                polyPoints[2].y = polyPoints[0].y;
        //                polyPoints[2].z = z * CellLength + CellLength;

        //                polyPoints[3].x = polyPoints[0].x;
        //                polyPoints[3].y = polyPoints[0].y;
        //                polyPoints[3].z = polyPoints[2].z; // halfLength;

        //                polyPoints[0] = Vector3d.TransformCoord(polyPoints[0], worldMatrix);
        //                polyPoints[1] = Vector3d.TransformCoord(polyPoints[1], worldMatrix);
        //                polyPoints[2] = Vector3d.TransformCoord(polyPoints[2], worldMatrix);
        //                polyPoints[3] = Vector3d.TransformCoord(polyPoints[3], worldMatrix);


        //                bool hit = Polygon.Intersects(r, polyPoints, parameters.SkipBackFaces, out intersectionPoint);
        //                if (hit)
        //                {
        //                    uint flattenedArrayIndex = (x * CellCountX) + (y * CellCountY) + CellCountZ;
        //                    _lastPickResult.Entity = this;
        //                    _lastPickResult.HasCollided = true;
        //                    // _lastPickResult.Geometry = this;
        //                    _lastPickResult.CollidedObjectType = CollidedObjectType.Cell;
        //                    _lastPickResult.FaceID = (int)flattenedArrayIndex; // (int)_cell.Faces[i].ID;
        //                    _lastPickResult.FacePoints = polyPoints;
        //                    // _lastPickResult.FacePointIDs = facePointIDs;
        //                    _lastPickResult.ImpactPoint = intersectionPoint;
        //                    System.Diagnostics.Debug.WriteLine(string.Format("{0}, {1}, {2} picked.", x, y, z));
        //                    // we can return because we know there is no overlap
        //                    return _lastPickResult;
        //                }
        //            }
        //    //    }
        //    //}
        //    return _lastPickResult; // base.Collide(start, end, worldMatrix, parameters);
        //}

        // TODO: This may become obsolete.  It's too slow and Quadtree is a better general replacement
        // for real 3D isometric games.
        // 
        /// <summary>
        /// When an Edge is changed (wallplaced or removed) we update the internal
        /// Volumes.  This is called by CelledRegion.RegisterEntity() for edge types
        /// and it's typically only used during floorplan design mode.  At arcade runtime
        /// it is not used even if walls are destroyed.  The original volume layout always remains.
        /// (incidentally, wall destruction can be modeled similarly to doors)
        /// </summary>
        //internal void UpdateVolumes()
        //{
        //    // TODO:this return skips the volume updates because for very large ship interiors
        //    // it takes FOREVER.  Also for the hud, the interior tilemasks take up a shit ton of memory
        //    // and i'm not sure if that's going to be feasible... 
        //    CelledRegion cregion = (CelledRegion)_region;

        //    const int NUMBER_OF_CUBE_FACES = 6;
        //    uint width = ((CelledRegion)_region).CellCountX;
        //    uint height = ((CelledRegion)_region).CellCountY;
        //    uint depth = ((CelledRegion)_region).CellCountZ;

        //    // all cells in the initial task list must be
        //    // assigned to the volumeID = 0  since that is the orphan volume?
        //    List<uint> unvisitedList = new List<uint>();
        //    Stack<uint> taskList = new Stack<uint>();
        //    List<Volume> volumes = new List<Volume>();

        //    uint count = width * depth * height;
        //    for (uint i = 0; i < count; i++)
        //    {
        //        unvisitedList.Add(i);
        //    }



        //    Cell current;
        //    Cell neighbor;
        //    Volume currentVolume;

        //    if (cregion.mCells != null)
        //    {
        //        // iterate thru all cells using marching cubes style traversal
        //        while (unvisitedList.Count > 0)
        //        {
        //            uint unvisitedCellID = unvisitedList[0];
        //            taskList.Push(unvisitedCellID);
        //            unvisitedList.Remove(unvisitedCellID);
        //            currentVolume = new Volume();
        //            currentVolume.ID = (uint)(volumes.Count);
        //            volumes.Add(currentVolume);

        //            // while there are still cells in the current task list
        //            // iterate them.  All cells in this task list will get added to
        //            // same volume
        //            while (taskList.Count > 0)
        //            {
        //                // TODO: here im creating new cells... why?
        //                // either these cells are already initialized and empty
        //                // and then just retreived from a list/dictionary
        //                // or they're created only when entities are added to them
        //                // but then... how do you instance an interor volume that is just air
        //                // in the middle of a room?
        //                // I think the solution is to instance all, but then for completed design
        //                // to prune the cells that are left in the original
        //                // root volume.  Thus even our initial volume should be created
        //                // prior to UpdateVolumes()?
        //                // What about entities within the cells?  How on earth do we
        //                // determine the partitions if the cells are recreated and any partitions
        //                // added to them are lost?  This is why cells are never created here.
        //                uint id = taskList.Pop();
        //                current = cregion.mCells[id];
        //                currentVolume.Add(current);
        //                // try to traverse to all 8 cardinal neigbhors
        //                for (int i = 0; i < 4; i++) // TODO: for now skip Top & Bottom tests // NUMBER_OF_CUBE_FACES; i++)
        //                {
        //                    if ((neighbor = GetTraversableNeighbor(current, i)) != null)
        //                    {
        //                        // if this neighbor has already been added to a volume skip it
        //                        if (currentVolume.Contains(neighbor.ID)) continue;

        //                        // add this neighbor cell to the same volume of the current cell
        //                        // and add it to the task list
        //                        neighbor.VolumeID = currentVolume.ID;
        //                        taskList.Push(neighbor.ID);
        //                        // this neighbor cell must be removed from the unvisited list however
        //                        unvisitedList.Remove(neighbor.ID);
        //                        currentVolume.Add(neighbor);



        //                        // interor empty cells do become part of the volume yes?
        //                        // but why?  well they are needed for pathing... or actually maybe htey arent?
        //                        // empty cells should be removed from list of unassigned always?
        //                        // Not having references for empty cells in our volumes will save memory
        //                        // 
        //                        //
        //                        // but what we want to do is once we find a cell that has structure
        //                        // is to use it as a starting node for a new volume and then traverse
        //                        // and remove until that volume is fully defined.  If we can traverse
        //                        // and the volume is not closed, then the volume is illegal.  

        //                        // but what about floors?  maybe the volume's themselves can be split
        //                        // into floors?  This way we maintain our interior visibility spatial grouping
        //                        // for when the camera is inside the interior, but also have
        //                        // floor spatiality when in isometric view mode

        //                        // while we can continue traversing, add the current cell to the current volume

        //                        // i think with this algorithm, it doesnt matter where we start our marching
        //                        // cubes algo.  

        //                        // cubes that are connected to a border cell that has no closed edges are
        //                        // added to a special "null volume" and removed from the overall list during
        //                        // traversal.

        //                        // an 8 bit flag is set if there is an edge on a given side
        //                        // of a cell.  
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    System.Diagnostics.Debug.WriteLine("UpdateVolumes() - " + volumes.Count.ToString() + " volumes found");
        //    if (volumes == null)
        //        mVolumes = null;
        //    else
        //        mVolumes = volumes.ToArray();
        //}


        /// <summary>
        /// Based on the partition configuration in the CelledRegion this function
        /// returns the neighboring cell of the specified cell in the specified cardinal
        /// direction.  If there is no neighbor or it is not possible to traverse to that 
        /// neighbor do to a wall, null is returned.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        //private Cell GetTraversableNeighbor(Cell cell, int direction)
        //{
        //    int neighborID = ((CelledRegion)_region).GetNeighbor(cell.ID, (Direction)direction);
        //    if (neighborID > -1)
        //    {
        //        // this tells us that the neighboring cell exists and that it's not out of bounds
        //        // and it gives us that neighbor cell's index.
        //        // HOWEVER, it does not tell us if their is a partition blocking us.  So now
        //        // we need to check if the celledRegion's partition table has an entry 
        //        // for the partition shared by these two cells.
        //        // So a) what does the partition table look like (especially since it contains
        //        // edgeID based walls and floor/ceilings
        //        // and b) how to get the edgeID from two cellID's so that we know which
        //        // edgeID to look to see if a partition exists on that edge.
        //        // *** Or maybe this is backwards.  I only need edges during our picking and
        //        // placing of entities, and from there i can determine the cells and then
        //        // directly add the partition entities to those cells... 
        //        // Then when using the delete tool, we're still just selecting the edges which
        //        // will give us both cells and we'll know to delete the relevant entities in the
        //        // relevant cells.

        //        // so grab the origin cell

        //        // based on direction, is there a wall entity at that side?
        //        // do we need to take into account 
        //        CelledRegion cRegion = (CelledRegion)_region;


        //        // get's the edge ID for the 4 left, right, front, back cardinal direction
        //        // partitions.  The bottom and top are simply cellID and cellID * cellsPerFloor respectively.
        //        Keystone.CSG.CellEdge.EdgeOrientation orientation;
        //        int originID, destID;

        //        // based on the orientation, we cand etermine the origin and dest vertexID's
        //        // the simple rule of thumb is starting at bottom left vertex and going clockwise
        //        // we increase the vertex ID by 1.  Then we always define any edge by it's lowest
        //        // vertex corner first and it's highest second.  So based on the orientation, we
        //        // know which corner's to pass for the vertices.
        //        switch ((Direction)direction)
        //        {
        //            case Direction.Front:
        //                orientation = Keystone.CSG.CellEdge.EdgeOrientation.Horizontal;
        //                // corner ID values are lowest to highest in 
        //                // the order of 0, 3, 1, 2.
        //                // thus originID must use the lower of the two corners 
        //                originID = cRegion.GetVertexID(cell.ID, 1);
        //                destID = cRegion.GetVertexID(cell.ID, 2);
        //                break;

        //            case Direction.Back:
        //                orientation = Keystone.CSG.CellEdge.EdgeOrientation.Horizontal;
        //                originID = cRegion.GetVertexID(cell.ID, 0);
        //                destID = cRegion.GetVertexID(cell.ID, 3);
        //                break;

        //            case Direction.Left:
        //                orientation = Keystone.CSG.CellEdge.EdgeOrientation.Vertical;
        //                originID = cRegion.GetVertexID(cell.ID, 0);
        //                destID = cRegion.GetVertexID(cell.ID, 1);
        //                break;
        //            case Direction.Right:
        //                orientation = Keystone.CSG.CellEdge.EdgeOrientation.Vertical;
        //                originID = cRegion.GetVertexID(cell.ID, 3);
        //                destID = cRegion.GetVertexID(cell.ID, 2);
        //                break;

        //            // top or bottom uses our cellID's themselves... but clearly this isproblematic
        //            // because we aren't using the same shareable ID's for those partitions
        //            case Direction.Top:
        //            case Direction.Bottom:
        //            default:
        //                throw new ArgumentOutOfRangeException();
        //        }

        //        Keystone.CSG.CellEdge edge = Keystone.CSG.CellEdge.CreateEdge(orientation, (uint)originID, (uint)destID, cRegion.CellCountX, cRegion.CellCountY, cRegion.CellCountZ);

        //        // now determine if this edge ID has any partitions assigned to it
        //        // This is the key aspect of how to proceed with partitions and how to
        //        // manage this data.  So we find the origin Cell and see if it has a wall/barrier
        //        // at the index for that wall.  Then we find the dest cell and
        //        // see if it has a wall/barrier inbetween the two cells
        //        // NOTE: The idea here if we go with it is that partitions are virtual
        //        // and never cached and are dynamically computed so we can find the two cell's
        //        // shared by this partition and then to check those to see if there's any
        //        // wall actually assigned to either cell's side of the divide
        //        // TODO: Here is the next step... we need to get this partition data
        //        // Do we even need the edge?  We know both cells and we know
        //        // the orientation and since all walls are now single or double sided and seperate
        //        // entities if double sided, then partition is not needed?
        //        // Because based on the direction we can determine which 
        //        if (cell.Partitions != null)
        //        {
        //            // test the relevant partition
        //            if (cell.Partitions[direction] != null)
        //            {
        //                return null;
        //            }

        //        }
        //        if (cRegion.mCells[neighborID].Partitions != null)
        //        {
        //            // test the partition using the destination cell from the opposite direction
        //            int oppositeDir = direction;
        //            if ((oppositeDir & 1) == 0)
        //                // it's even, so add one for the opposite direction enum
        //                oppositeDir += 1;
        //            else
        //                // it's odd, so subtract one to get the opposite direciton enum
        //                oppositeDir -= 1;
        //            if (cell.Partitions != null)
        //                if (cell.Partitions[oppositeDir] != null)
        //                {
        //                    return null;
        //                }
        //        }
        //        // note: our implementation of partitions is such that they are just a list of 
        //        // Entities into a list of cell indices.  
        //        // that must change i suspect.  A better way is going to be to just have
        //        // a cell struct maintain a bitflag for partitions which get set during
        //        // RegisterEntity?  or something...  
        //        // in the short term how can we test thta walls will divide a volume
        //        // (keeping in mind that we'd need walls thru all floors since intiaillty for testing
        //        // floors/ceilings arent checked.

        //        // the original idea was that our shared partitions would not exist within a
        //        // single cell and instead be their own seperate entities.  However, that doesnt 
        //        // allow them to take advantage of any spatial culling...
        //        // Maybe we change this somewhat... we can still dynamically determine 
        //        // neighboring cells based on an edge and such
        //        // but since walls may potentially have different textures on either side,
        //        // they should be seperate and simply set as assigned to the same edge.
        //        // but also assigned to a specific cell.
        //        // So a key to building rooms easily is when selecting a wall entity, to
        //        // have it so we intelligently pick between a bunch of sub-wall entity types
        //        // so we can have single walls for exterior walls, doublesided walls for interior shared
        //        // and curved walls for corners... and different length walls for the diagonals.
        //        // So it's becomes an "segment" (a segment is a lookup of entities for things like walls)
        //        //
        //        // The other part is, currently the edge ids and cell ids and such are geared towards
        //        // picking for the purposes of layign down walls, floors.  It works really well for that
        //        // But now, we need to think about volumes and pathing.

        //        return cRegion.mCells[neighborID];
        //    }
        //    else
        //        return null;
        //}

        #region ITraversable Members
        /// <summary>
        /// Primary reason for InteriorRegionNode is just to have the unique Traverse handler
        /// </summary>
        /// <param name="target"></param>
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        #region IBoundVolume members
        protected override void UpdateBoundVolume()
        {
            // NOTE: our overall volume is just the volume of the CelledRegion.  
            // we do not actually have to iterate through our child cells because they
            // are already fully contained by the CelledRegion
            base.UpdateBoundVolume();
        }
        #endregion
    }
}
