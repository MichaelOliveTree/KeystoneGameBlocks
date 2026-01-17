using System;
using System.Collections.Generic;
using Keystone.Portals;
using Keystone.Types;
using Keystone.Primitives;
using Keystone.Traversers;
using Keystone.Entities;

namespace Keystone.SpatialNodes.QuadTree
{
    public class QuadtreeCollection : QuadtreeQuadrant
    {

        Interior mInterior;

        public QuadtreeCollection(Interior celledRegionEntity) 
        {
            if (celledRegionEntity == null) throw new ArgumentNullException();
            mInterior = celledRegionEntity;

            if (mInterior.MaxSpatialTreeDepth > 0)
            {
                // TODO: This isn't a square, but a rectangle right? Or actually its a square because QuadtreeQuadrant has to be a square??
                BoundingRect squareBoundingRect = BoundingRect.Square(BoundingRect.FromBoundingBox(mInterior.BoundingBox));

                Keystone.QuadTree.QuadtreeQuadrant.MaxDepth = mInterior.MaxSpatialTreeDepth;
                Keystone.QuadTree.QuadtreeQuadrant.WorldRect = squareBoundingRect;
                Keystone.QuadTree.QuadtreeQuadrant.SplitThreshHold = 10;

                // TODO: region.OctreeDepth is used but we should first test
                // region.SpatialLayout 
                // enum SpatialLayout
                // {
                //    none = 0,
                //    octree = 1,
                //    sectors = 2, // portal connected sectors
                //    grid = 3     // 3d volume of cells
                // }

                mChildQuadrants = new QuadtreeQuadrant[mInterior.CellCountY]; // todo: these childrent aren't quadrants, they are 1:1 for each deck floor and so there can be more than just 4 quadrants.
                for (int i = 0; i < mInterior.CellCountY; i++) 
                {
                    mChildQuadrants[i] = new QuadtreeQuadrant(0, 0, squareBoundingRect, this); // TODO: shouldn't null parent actually be "this" QuadtreeCollection?
                }
            }
        }

        ~QuadtreeCollection()
        {
        }

       

        #region ITraversable Members
        public object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        public override void Add(EntityNode entityNode, bool forceRoot)
        {
            // note: i believe this entityNode must always be a CellSceneNode because 
            //       the entityNode.Entity _must_ exist in a CelledRegion and therefore
            //       entity.CellIndex must have a meaningful value we can use to determine
            //       the unflatted y axis of the cell this entity has been placed in and thus
            //       which quadtree in this collection to place it in.
            System.Diagnostics.Debug.Assert(entityNode is CellSceneNode, "QuadtreeCollection.Add () - Should EntityNode always be CellSceneNode here?");
            Entities.Entity entity = entityNode.Entity;
            //CelledRegion celledRegion = (CelledRegion)entity.Region;
            //CelledRegion celledRegion = (CelledRegion)entity.Parent;


            // Added to the proper child quadrant based on the Y floor of the entity
            // TODO: really we need to ensure that entity.Translation.y == a whole index value indicating the floor, but
            //       when this entity moves, jumps, is exploded, how would we modify that? A dynamic entity maybe stays
            //       not added for a while until it settles into a static mode? 
            // TODO: how do we move entities between quadtree collections? i dont see that we account for that possibility anywhere
            // TODO: if .Translation.y is center of mesh while floor of mesh is boundingBox.Min.y, but for BonedEntities the Floor is translation.y
            // TODO: if the Entity being placed is taller than a single floor height, like a Reactor or ShuttleCraft, where do we place it?
            //       I Think this QuadtreeCollection.cs itself should contain those types of Nodes as a Root collection of nodes.
            if (entity is Lights.DirectionalLight)
            {
                mChildQuadrants[0].Add(entityNode, true);
                System.Diagnostics.Debug.WriteLine("QuadtreeCollection.Add() - DirectionalLight added to deck = 0.  This may not be correct.");
            }
            else
            {
                int deck = mInterior.GetFloorFromAltitude(entity.Floor); // TODO: if .Translation.y already contains floor we are in trouble, but it should not ever!  it cannot! that was the old way of doing things.
                if (deck == -1)
                {
                    System.Diagnostics.Debug.Assert(false, "QuadtreeCollection.Add() - invalid deck number");
                    return;
                }
#if DEBUG
                else
                    System.Diagnostics.Debug.WriteLine("Adding '" + entity.Name + "' to deck '" + deck.ToString() + "'");
#endif
                try
                {
                    mChildQuadrants[deck].Add(entityNode, forceRoot);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, "QuadtreeCollection.Add() - invalid deck number. " + ex.Message);
                }
            }
        }

        // I think Add is the only one we need to override because we do have to choose
        // the correct floor level
        public override void Add(EntityNode entityNode)
        {
            this.Add(entityNode, false);
        }

        // NOTE: I don't think this requires overriding because it's going to directly
        // call the correct QuadtreeQuadrant, however if we need to move between
        // floors/decks then OnEntityNode_Moved might need to be able to handle that?
        // Not sure yet if that's not handled exterior to this and then is simply reAdded
        // from mScene.NodeAttached()
        //
        //public override void RemoveEntityNode(EntityNode entityNode)
        //{ }

            

    }
}
