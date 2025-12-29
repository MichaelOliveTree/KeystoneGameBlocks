using System;
using Keystone.Entities;

namespace Keystone.SceneNodes
{
    // http://www.isogenicengine.com/engine/documentation/root/IgeEntity.html
    internal class CellObject  // like PhysicsObject only this is for Cell operations on the Entity
    {
        private Entity mOwner;
        private System.Drawing.Point mOrigin;


        // TODO: internally, our Cells[,,] array could contain an array of this data.  
        //       we'd still have some indirection though both to access the shared footprints and
        //       the entity's themselves.  But its unavoidable that our Entities are in random memory
        //       

        public CellObject(Entity owner)
        {
            if (owner == null) throw new ArgumentNullException("CellObject.ctor() - Owner Entity cannot be null.");
            mOwner = owner;

            // if the owner.Footprint == null what do we do?  
            // must a CellObject Entity always have a footprint?
            // what about triggers?  They should too right because
            // the point with cells is that the cells are our structures
            // for collisions, pathing, and for occupying tiles.
            // without that, they dont really have any way of interacting
            // with the CellMap they are on.
            //
            // what if the Footprint changes?  This should be notified?
            // can it change?  This would break layouts undoubtedly and be annoying
            // to recheck and enforce that users must delete improperly placed
            // components.
            //
            // What happens when we edit footprints and then load in a save file
            // and now suddenly the placement is broken?  This doesnt just affect modders
            // it can affect end users who use mods where the modders changed
            // a prefab's footprint in a way that breaks the placement.  I could simply
            // leave the objects placed and have a flag set if there's an error and
            // allow editor to ignore flag so user can modify, but allow game server
            // to prevent the vehicle from being loaded.  User must select another or some such
            // or be given a default.
        }

        public Entity Owner { get { return mOwner; } }

        // for TileWidth and TileDepth to make sense, the smallest 
        // x index and y index occupied tile is the "origin" tile.
        // It is not the center because even numbered width/height 
        // footprints wont have a center tile.  And it's just easier
        // know that the 0,0 tile of the footprint corresponds with
        // the origin within the floorplan.
        public System.Drawing.Point Origin { get { return mOrigin; } }

        public int TileWidth { get { return Footprint.Width; } }
        public int TileDepth { get { return Footprint.Depth; } }
        public int CellWidth { get { return Footprint.Width / 16; } }
        public int CellDepth { get { return Footprint.Depth / 16; } }

        public CellFootprint Footprint { get { return mOwner.Footprint; } }

        public void TranslateToTile()
        {
        }

        public int[] GetOverTiles()
        {
            int[] results = null;

            return results;
        }
    }
}
