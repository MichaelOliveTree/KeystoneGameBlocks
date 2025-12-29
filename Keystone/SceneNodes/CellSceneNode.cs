using System;
using System.Collections.Generic;
using Keystone.Traversers;


namespace Keystone.SceneNodes
{
    /// <summary>
    /// the idea here that a CelLSceneNode replaces EntityNode for entities added to
    /// CelledRegions.  So that this CellSceneNode can track when an entity spans multiple cells but still using
    /// just a single cellSceneNode?  That could actually be a decent idea because we keep
    /// just one CellSceneNode per Entity but where it tracks the cell indices it's in.  hrm...
    /// likewise each Cell will have a layer map array of entity stacks that shows which entities it contains.
    /// </summary>
    public class CellSceneNode : Portals.EntityNode
    {
        // TODO: I think this is completely irrelevant now.  Cells are used for placing components
        // in design mode and for modifying the footprint for path finding.  But otherwise we only use
        // Quadtree to host components.  Exception are partitions and access (walls, floors, doors, windows, etc)
        // but wait, we need to be able to handle triggers and collision events (OnEnter(), OnExit, OnStay()) in the child entity Scripts of the interior (eg NPC walking onto an Elevator)
        // 
        //int[] mSpannedCellIndices; // <-- ??? as alternative to complexity of dealing with spanned cells
        //                         //     rather I think
        //                         //     EntityNode's like a Engine or Reactor need to have
        //                         //     perhaps multiple seperate entitiy lego parts that can only individually
        //                         //     take up a single scenenode but can be stacked to create a large collective super component.?
        //                         //     Hrm..   
        // 
        // on creation, this CellSceneNode will query the entity it's attached to
        // then determine which underlying cells to register with.
        //
        // NOTE: here the Entity represented spatially by this CellSceneNode
        // can easily determine which cells it is placed on via
        // ((CellSceneNode)this.SceneNode).mSpannedCellIndices;
        // and obviously can find out which map overall by querying the parent sceneNode of this
        // CellEntityNode and then looking at it's Entity which must be the CelledInterior
        // and that Interior.cs has info on height/width/etc

        // To determine what entities are in a specific cell, 
        //
        // I think the above is the only type of per cell type of class we need
        // and the rest is just Arrays in our CelledInerior region.

        internal CellSceneNode(Entities.Entity entity) : base (entity)
        {
        }

        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion


        internal override void OnEntityMoved()
        {
            base.OnEntityMoved();

            Interior interior = (Interior)this.Entity.Parent;

            // do TileMap collision tests here so collision and trigger events can be handled by child Entity scripts
            interior.Collide(this.Entity);
            

        }
    }
}
