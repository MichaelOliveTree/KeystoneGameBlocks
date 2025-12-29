using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keystone.SceneNodes
{
    ///// <summary>
    ///// PlacementLocations govern editor placing of components and it governs NPC
    ///// manipulation and movement of components and so allows an NPC to determine how
    ///// something can be manipulated or moved without doing some calculation about the size
    ///// and mass of the object.
    ///// </summary>
    //enum PlacementLocations : byte  // what about distinction of where you cna place something
    //// when editing versus where an npc might place it while playing?
    //// perhaps i should have multiple EDIT_FLOOR, FLOOR, EDIT_WALL, WALL
    //// so we know that to place an entity when building, you look for
    //// availability of appropriate edit flags, but during runtime
    //// an entity like a fire alarm can fall off and land on the floor
    //// and later be recovered and added to manifest/supplies/inventory/cargo and available for
    //// for re-adding to the vehicle when the user is repairing their ship
    //{
    //    Floor = 1 << 1,
    //    Wall = 1 << 2,  // eg. mounts only on wall (not fences or railings)
    //    Ceiling = 1 << 3, // eg. ceiling fan, smoke detector
    //    Counter = 1 << 4 // eg. on a desk, table, counter top
    //}

    // this CellIndex is actually an Anchor and so for a wall entity (which is 
    // determined in CelledRegion.AddChild()->Register(), we know
    // the anchor index refers to an Edge Index
    // 
    // TODO: What we should do instead is have CellIndex indicate the real cellID
    // and then to include a .CellPlacement (byte) var to indicate where in the cell
    //  (left wall, right wall, front left corner, front right corner, etc the object
    // should go.  And furthermore, maybe CellIndex should be .CellIndices[] to 
    // indicate all of the cells a particular component overlaps
    // perhaps we could have a class
    // CellPlacementInfo  // and this can be null if the item is not placed in a cell
    internal enum CellPlacement : byte
    {
         None = 0,
         BottomFace = 1 << 0,
         TopFace = 1 << 1,
         LeftFace = 1 << 2,
         RightFace = 1 << 3,
         FrontFace = 1 << 4,
         RearFace = 1 << 5,
         FrontLeftCorner = FrontFace | LeftFace, 
         FrontRightCorner = FrontFace | RightFace,
         RearLeftCorner = RearFace | LeftFace,
         RearRightCorner = RearFace | RightFace

    //     The above is necessary because we do not have a 3d
    //     footprint.  And we only want to load those elements of the footprint that are
    //     necessary.  For now let's not even worry about any of this.  just implement it 
    //     in this current form
    //     
    //     CounterTop,
    //     Ceiling,           // indicates item mounted on ceiling (requires TopFace != null)
    //     Floor,             // indicates item placed on floor (requires BottomFace != null)
    //     LeftWall,
    //     RightWall,
    //     FrontWall,
    //     RearWall,
    //     // TODO: Now we may have it such that each wall, floor or ceiling can also
    //     // have a virtual grid where if an entity only takes up 1/3 x 1/3 wall size
    //     // it can be placed in one of 6 spots on the wall.  That is done simply
    //     // via a more detailed list of enums.
    //     // further, when placing something on a wall, we then check the existance of
    //     // other entities on that wall and then check that against their size to see
    //     // how many of the slots it takes up.
    //     //
     }
    //
    //   
    // enum CellSize // bitflag which will tell us how much a cell overlaps other cells in the cardinal directions
    // // huh?  did i mean for footprints?
    //
    //

    internal enum CellLayout : byte
    {
        XAxis_One = 1 << 0,
        XAxis_Two = 1 << 1,
        XAxis_Three = 1 << 2,
        XAxis_Four = 1 << 3,
        ZAzis_One = 1 << 4,
        ZAxis_Two = 1 << 5,
        ZAxis_Three = 1 << 6,
        ZAxis_Four = 1 << 7,

        OneXOne = 0,
        OneXTwo,
        OneXThree,
        OneXFour,
        TwoXOne,
        TwoXTwo,
        TwoXThree,
        TwoXFour,
        ThreeXOne,
        ThreeXTwo,
        ThreeXThree,
        ThreeXFour,
        FourXOne,
        FourXTwo,
        FourXThree,
        FourXFour
    }

    // TODO: ok i think i can in fact get rid of "Direction" because
    // if a CellPlacement type is "wall" and it must manually set flags
    // on the footprint corresponding to the area it's walling, then
    // it checks for existance of anotehr wall, must replace it... but technically
    // it's not replacing the entity, it's only replacing the domainobject and perhaps mesh and appearance
    // 
    internal class CellPlacementInfo
    {
        // TODO: can we get rid of the "PartitionID" that this entity should be place in?
        // if it's a floor, ceiling or wall?  Or should we only use CellPlacement enum to determine
        // this?  The problem here is, the placement and the footprint loaded should match.
        // And some footprints should be dynamically expandable such as a wall that can have it's width
        // increased up to max width of a cell... such that an entire cell can be a wall.
        // Ideally, it'd be nice if we only had footprints and nothing else... but
        // we do need to know the type of thing we're placing...  
        // So that for instance we can know if we can place a table because we know there is already
        // a floor underneath.  

        // the layout is used to determine how to parse the footprint data
        internal byte CellOffset;       // since a cell is 16x16 = 256, a single byte can serve as offset
        internal byte CellRotation;    // y axis rotation values of 0 - 8 to represent 45 degree increments.  Overrides any entity.Rotation setting
        internal uint CellIndex;

        // layout should also be a function since it's the same for any instance. 
        //internal CellLayout Layout;  // 1x1, 1x2, 1x3, 1x4, 2x1, 2x2,2x3,2x4, 3x1, 3x2, 3x3, 3x4, 4x1, 4x2, 4x3,4x4
        //internal byte[] Footprint;  // we may decide to always grab the offset via DomainObject function
                                    // the only time we need it is when adding/removing/moving the entity
                                    // as a function (or resx) it can also be shared.

        //     // can we get away from using CellPlacement by instead examining the footprint?
        //     // we could but only if we never had to worry about ceilings and walls.
        //     // that's wh y footprint is really only useful for things that touch the floor
        //     // and things on the walls
        //     // TODO: How using just footprint can we determine if
        //     // an item has a counter?   Is it just a seperate footprint layer that 
        //     // details the location of counters?  Having this per cell could mean
        //     // we save on most cells that dont even need that layer added.
        //     // Same with walls

        // placement may not be necessary given footprint, offset and and rotation info
        //internal CellPlacement Placement;   //overides any entity.Translation saved in xml

    }
}
