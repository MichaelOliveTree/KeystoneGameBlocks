using System;
using System.Collections.Generic;
using System.Drawing;
using Keystone.Entities;

namespace Keystone.SceneNodes
{
    
    // Let's rethink this from ground up and consider a few factors
    // - Sims 3 
    // - FPSC  - http://www.youtube.com/watch?v=vrEIQzUQ3DY
    // - Alien Shooter -> Love the details  http://www.youtube.com/watch?v=Gmhi_Vezdk0
    //   
    //  Rooms + Portals beats quadtree or octree
    //      - to enforce good design, you can only place interior components
    //        in "rooms" where a room is defined as a completely walled in interior.
    //  1) So a room is an array of tiles
    //    - in FPSC each tile is composed of several segments
    //      - 4 walls (each potentially divided into bottom/middle/top)
    //      - floor
    //      - ceiling
    //      - corners 
    //      - in FPSC Creator, a prefab segment behaves like a brush and dynamically determines
    //        what meshes are used for any adjacent tile based on whether corners are specified
    //        whether a CSG mesh is specified, etc.
    //      So i like this aspect of a "brush" to quickly flesh out rooms, but where FPSC stops
    //      short is in making all their parts composed of NON entities whereas I want fully destructable
    //      so everything will be an entity. 
    //      So portals + rooms is critical when indoors and NO SceneNode's per entity.  That would be insane
    //      for interior meshes.
    //      Our "rooms" or "volumes" or "spaces" whatever are our sceneNode's and are defined by our
    //      cells which can contain null space as well.
    //
    //  2) a ship is an array of rooms at various deck levels
    //  3) Unlike a region, a room does not have it's own coordinate system.
    //     It is like any other entity in the coordinate system of it's most immediate parent region
    //  
    //  Isometric view, we use fog of war 
    //  Isometric view we can also focus that fog around the player's captain avatar such that
    //  he can only see around him or via security cams if his character is in front of one.
    // 
    // Vehicle
    //      Mesh = ExteriorShip
    //      Region Interior - SceneNode connects not to Vehicle's, but to the Vehicle's SceneNode's parent SceneNode
    //             Volume[x,y,z] Volumes;  // each volume contains an array of cells
    //                                     // a cell contains AI hints mostly since otehrwise
    //                                     // the list of entities just need to be within the bounds
    //                                     // of the volume and can fall in dynamic vs static entities
    //                                     // Static entities can be destroyed potentially but otherwise
    //                                     // they aren't movable. 
    //                                     // So the cell info can tell us if an area is passable
    //                                     // if it has wiring, plumbing, venting, etc flags set
    //                                     // to determine flow of current, water, gas
    //             
    //
    //  as a user edits cells and errects walls or tears some down, we can easily
    // use floodfill to determine where new rooms are formed and where others are merged
    // 
    // So do we need to have cell level granularity for stacks of entities?
    // Maybe this is nothing we need to worry about for now.  WHat is i think decided
    // is that invidiual "Cells" are necessary because anytime you wish to edit a cell
    // you need to be able to know what that cell contains (via it's references) to know
    // how to delete it. 
    // Perhaps the compromise is that each "cell" does not use a scenenode because a Cell
    // is not an Entity.  It is more of an AI map.
    // 
    public enum CellFlags
    {
        None = 0
    }

    // TODO: For viewing interior of your ship while moving, user maybe has to switch to
    // interior view cam.  If their machine has enough horsepower, maybe they can have tactical
    // view and interior both open.
    // -- HOWEVER- we can enforce a fog cone around the view so that the player as captain can only
    // see the area they are focussing on and this will preserve frame rate.
    // Otherwise it will just be insane.
    //  - we would need heavy duty lod mechanisms... but for now we wont worry about LOD
    //    we just want to focus on laying down our cells and saving and loading them.
    //
    //   So we start with an isometric "floorplan" editor.
    //
    //
    // TODO: SO far InteriorCell isn't being used.  I'm using a SceneNode that is divided into
    // CellSceneNode.cs 
    // The question is, how best to both maximize performance for things like picking and pathing
    // as well as fit it in an elegant way.
    // TODO: I think what should occur is these cell's should be created in CelledRegion and can be
    // added and removed at offset positions once a Cell dimension is defined.
    // Then accessing cell's is by x,y,z offset
    // Furthermore, the SceneNode's are added / removed when the interior cells are added/removed
    // And finally, we should have plenty of helper functions for painting the celing/floor/walls of
    // any cell such that the benefit of a celled region is that it auto handles u,v, and positioning
    // of the floor/ceiling/wall geometry for us by dynamically generating the correct meshes
    // And keep in mind that 
    //
    /// <summary>
    /// A cell is not an entity, but it's not a sceneNode either.  
    /// A cell is virtual dividing up of a space
    /// to make various game tasks easier such as picking, path finding, but most
    /// importantly for making it easier for users to design their ships ala the Sims 3.
    /// This is the most important aspect because otherwise we could use nav meshes and such.
    /// The key reason a Cell is not a SceneNode is that for entities that span multiple cells
    /// there'd be no way to fit them under a single cell.
    /// 
    /// But the more i think of this notion of "cells" i think of an array of
    /// DiffusionMap and how every "map" is a named array in a dictionary OR a BitArray.
    /// The problem here though is the grid which is a bit array now lacks 
    /// 
    /// </summary>
    internal class InteriorCell // Some cells in our array of cells will be null.
    {
        // flags 
        //    jails bars when closed aren't passable but i suppose that's a door?
        //      -somedoors can pass scent but not large objects.
        //    an invisible forcefield might block scent, sound and objects but not light
        //    damange 
        //    vented   <-- connected to life support?
        //    plumbed  <-- connected to plumbing pipe?
        //    powered  <-- connected to a power grid or source?
        //    type of terrain (metal, grating, cement)
        //    causes damage (fire, radiation, if so how, i think an object in a cell
        //    can take the cell(s) it's on and then emit to neighboring cells any radiation
        //    but then i get the funny square emissions and not ciricular radius as i would
        //    by just having an invisible "radiation" emission entity zone
        //   - provides cover (some components can set some types of flags to indicate where there is cover from what direction?)
        //    -HasSeen (for fog of war but our lastSeen state needs to be tracked)
        //    -HasItems
        //    -HasUnit (npc or player)
        //    -HasDoor 
        //    -HasPath (to up or down or thru a door to neighboring cell)
        //      - we want to compute passable on the fly though rather than store yet another
        //        bit of memory 
        //    (obsolete) walkable (wall flag for each of 4 cells, plus celing flag as well?)
        //  int x
        //  int z
        //      
        // hrm... a unit can know what tile it's on, so tile itself doesnt need to
        // know what units are on it?
        // Like a component can know it's tiles... and apply flags to the tile
        //   - but actually for ai pathing if a tile doesn't know it's units, ai entities
        //     traveling wont be able to easily find out if there's an entity on that tile 
        //     that will block it's path.  A bullet for instance may hit a computer
        // 
        // Our cell is a data structure... within an Entity because ideally each cell would
        // be an entity but that's a waste of memory...
        // So each cell can contain references to the wall, floor and ceilng tiles
        // What is the most efficient way to do that?  I mean yes flags can be used to 
        // speed up AI (wall, railing, floor, stairway down or ladder etc)
        // 
        // In theory, these "flags" are the short cuts our AI uses but in reality
        // each cell can have variable "things" in it and those "things" affect the flags
        // 

        // TODO: I think this class is completely unnecessary as all we need 
        // is the CellSceneNode and the Interior that then contains a bunch of arrays
        // or dictionaries.  
        CellFlags mFlags;    // flags isnt necessary either.  
        Point Coordinate;    // this shouldn't be necessary either as the coordinate can be computed based on the index
        Entity mStacks;  // the entities stacked on this tile.  Can be null.
                         // 

        // override AddChild(EntityBase entity)
        // {
        //      base.AddChild(entity);
        //      RegisterEntity (entity);
        // }
        internal void RegisterEntity(Entity entity)
        {
            
        }

        internal void UnRegisterEntity(Entity entity)
        {
            // modify any underlying diffusion maps

        }

        // TODO: this can be a static function that accepts all cells, and the index
        // of the target and perhaps our index can be 3d array? so 3 indices (xPos, yPos, floor)
        InteriorCell[] GetNeighbors()
        {
            InteriorCell[] neighbors;
            neighbors = new InteriorCell[6];
            return neighbors;
        }

    }
}
