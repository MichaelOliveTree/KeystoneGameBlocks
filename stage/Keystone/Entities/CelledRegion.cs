using System;
using System.Collections.Generic;
using KeyCommon.Flags;
using KeyCommon.Traversal;
using Keystone.Collision;
using Keystone.CSG;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Extensions;
using Keystone.IO;
using Keystone.Types;

namespace Keystone.Portals
{
    // On Fallout 3
    //      - from what i gather on fallout 1 & 2 vs 3, people liked that 1 & 2 were not 
    //      twitch games, and that you could really play without combat, and that the world 
    //      was vast but didnt feel barren as it does in the full real time 3d of Fallout 3.
    //      They also dont like that the combat feels stupid and the skills you can max out in all
    //      and not have to pick and choose to suit your play style...  
    //      ** So thinking of this, i think i definetly want a more deliberate thinking game
    //      non twitched base, skills and role playing do matter, and you may play the role of a 
    //      defense frigate and never commit any piracy, or anything.  I want there to be many boring paths.
    //      NOT to just cater to twitch and shoot kiddies.
    //      
    // -Frame rate issues of rendering 3d isometric tiles can be mitigated by limiting zoom out factor
    // -Exterior planet side designs can be done using larger tile sets perhaps?  For like towns
    //     as in http://upload.wikimedia.org/wikipedia/commons/thumb/5/59/Unknown_horizons_3176.PNG/220px-Unknown_horizons_3176.PNG
    //
    // Starcraft 2 Editor - it uses tiles too (or a grid where objects snap to grid)
    // http://www.youtube.com/watch?v=lLwj5GVvdLI
    //
    // Sims3 gameplay objects 
    //  - http://www.simswiki.info/wiki.php?title=TS3PR/Sims3GameplayObjects
    //
    // UFO: Aftershock - http://www.youtube.com/watch?v=mFOIoZuYgsg
    //  - this shows a good idea for a mission to Mars Colony where the user is said to be
    //   there to transport food but secretly they are to find a contact who says he has information
    //   about a group that is becoming increasingly influentential on Mars and may incite
    //   a rebellion if they are not careful.  So the user goes to Mars Colony and underground 
    //   tries to make contact with this informant.  This informant gives information about the Canon-ites.
    //  - http://www.youtube.com/watch?v=mGecdJTZ7RM <-- NEAT CHARACTER SKILLS SCREEN!
    //
    // Laser Squad - GREAT GAME... Didnt realize it was basically XCOM sequel
    // - http://www.youtube.com/watch?v=UJWfyPKlx4M
    //   http://www.youtube.com/watch?v=bm4VuHylH7Q  <-- love the queens ability to lay eggs
    //   Laser Squad shows that we can probably have a zoomed out view of a deck with LOTS of
    //   units, and rooms, but with relatively little graphics but the gameplay rules... imagine
    //   warding off an alien invasion on a space station... Being sent down to squash an uprising on a colony
    //   or a station or ship that is adrift before the aliens can fix the engines and before
    //   their help arrives.
    //   etc, etc.   
    //  
    // Shelter teaser trailer (isometric game really cool!) - http://www.youtube.com/watch?v=ahfW0i9vOC4
    //  - shelter appears to be an indie game by this user http://www.youtube.com/user/shihonage
    //  - really great concept too http://www.youtube.com/user/shihonage#p/u/1/6yuPvOx87UI
    //  - http://www.youtube.com/user/shihonage#p/u/2/5FXgTD0z2Q4

    // - Arcanum

    // UFO2000 source code for tips
    //
    // spinDizzy remake
    // - http://www.youtube.com/watch?v=1JIiPWgS6yI
    // That spindizzy remake reminds me of how ideally it'd be nice to just use 1 cm cube voxels
    // but for contiguous walls, floors, ceilings, we can cheat and use a sort of Compression
    // block to represent that space until there is any damage.
    // - Then yes, you do have to test and make sure all your maps play on min spec
    // That makes it tougher to design "any" ship but so be it... any game you can just add too much
    // geometry as is.
    //
    //// http://www.youtube.com/watch?v=z-Jm8Q1U-7I
    // The above in game editor vid shows how a wall is just another component in the cell.
    //      - it also shows how a component can easily have a relative offset within a tile
    //      There's no need for it to be in the center or exactly on the edge in case of walls/floors/ceilings
    //       
    // search "Receptron" in entire solution to find out more notes on collaborative diffusion
    // and how that relates to receptors and emissions emitters



    /// <summary>
    /// A CelledRegion divides up a region of space (all regions have their own coordinate systems
    /// whether they are interior or not!) with a bunch of equal sized cells.
    /// This is very common in isometric game engines.  Our cells are 3d and not 2d "tiles"
    /// http://www.cs.ucf.edu/~jmesit/publications/scsc%202005.pdf
    /// </summary>
    /// <remarks>
    /// IMPORTANT: Note how the celledRegion entity itself has no internal spatial divisions.
    /// It is the CellSceneNode's that provide the division by hosting child EntityNodes according
    /// to the interior spatial position of the entities they host.  However, it is the CelledRegion
    /// entity that provides the underlying entity logic and data map layers for computing
    /// AI behavior.
    /// </remarks>
    public class Interior : Region
    {
        // TODO: we can add static glue methods to help us do things like entity traversal between
        //       neighboring celledregions.  it's similar to what we do with Zones.

        // NOTE: there is no flag for an Agent/NPC. Is that ok?
        // TODO: how do i seperate loword and hiword attributes?
        [Flags]
        public enum TILE_ATTRIBUTES : int
        {
            NONE = 0,
            BOUNDS_IN = 1 << 0,    // bit0 - painted by bounds painting. NOTHING can exist/be placed in a cell where bit BOUNDS_IN = 0
            WALL = 1 << 1, // NOTE: OBSTACLE flag is no longer used. It's too generic and we should not combine it with flags like WALL or COMPONENT //  | OBSTACLE,         // walls are obstacles that offer strucutal support
            FLOOR = 1 << 2,
            CEILING = 1 << 3,      // ceiling can have seperate texture from floor
            STAIR_LOWER_LANDING = 1 << 4,      // we need to know if a component is traversable and where the lower and upper enter Areas are if any.
            STAIR_UPPER_LANDING = 1 << 5, // should this flag modify the tile attributes above it? I don't think so. It's only to tell us during Portal generation and Pathing where we can enter from. 
                                          // still doesn't answer question of how we connect the lower portal to that upper area?
                                          // or how to make that upper area portal connect to the lower traversable stairs.
                                          // a component can be traversable along the top, but for stairs, we must enter from the bottom or top, not inbetween.
                                          // I think these attributes are only needed for computing a path and aren't used at any other time.
                                          // ACTUALLY, they are placed in the footprint data and that footprint data is what we use to compute Areas and Portals.
                                          //
                                          // What if there were a "height" byte to tell us the height of any tile? 0 being on the floor of that current level.  256 being at the ceiling.
                                          // Or using flags for height.  HALF_HEIGHT, FULL_HEIGHT otherwise default is floor height.
                                          // If we just use a single flag titled "STAIRS" then we'd know that we have to raytest to find the low and high parts and at any particular rotation.
                                          // WAIT: I think this flag must modify the upper in order for the upper level to know that an accessible Area can be generated there.
                                          //  In fact, i think we need t o modify both the upper and lower with _some_ flag to denote that the traversable component cannot be entered on lower level from upper exit tiles.
                                          // and the upper level needs to know that a unique Area can be generated there for exiting/entering the stair component that is in the floor below.
                                          // Or can these flags just signify AreaPortal locations?  So that we don't need to generate Areas, just enter/exit portals.
                                          // So these can be thought of as PORTAL creation "hint" flags for 3D portals, not AREA creation flags.
                                          // Well Portals always exist inside of Areas.  If the Area is not there, there is no Portal created.

            // ACCESSIBLE_DESTINATION_LOWER = 1 << 6,
            LADDER_LOWER_LANDING = 1 << 6,
            LADDER_UPPER_LANDING = 1 << 7,
            COMPONENT = 1 << 8, // OBSTACLE flag is no longer used.  We should not combine with WALL and COMPONENT. // | OBSTACLE,    // components are obstacles
            COMPONENT_TRAVERSABLE = 1 << 9, // for stairs, TRAVERSABLE can be done from the lower level.  For upper level flags with no COMPONENT flag set, can walk across the tops of components that are placed in the lower level but who's height extends to the upper level
            COMPONENT_STACKABLE = 1 << 10,   // if it is "stackable" and a stacked component is placed there, how do we know to remove the COMPONENT_TRAVERSABLE flag?
                                             // maybe we add a flag STACKED which indicates a component is placed on it and so it overrules the COMPONENT_TRAVERSABLE flag?
                                             // USEABLE_COMPONENT = 1 << 19,  // for things like chairs and bunks and stations? In a sense, all components are USABLE in that if damaged can be fixed, though with chairs, beds, showers, etc, crew can "enter" those tiles and "merge" with the component and flagging that component as IN_USE
            ENTRANCE_AND_EXIT = 1 << 11, // TODO: Door and Lift enter and exit could probably be combined into a single enum value
            LIFT_ENTRANCE_AND_EXIT = 1 << 12, // TODO: don't use LIFT_ENTRANC_AND_EXIT for now... just use DOOR_ENTRANCE_AND_EXIT for both
            LIFT = 1 << 13,
            //OBSTACLE = 1 << 14,  // OBSOLETE - we do not want to combine flags with WALL and COMPONENT // main difference between WALL and OBSTACLE are that walls additionally offer structural support.
            LINE_POWER = 1 << 15,
            LINE_FUEL = 1 << 16,
            LINE_PLUMBING = 1 << 17,
            LINE_NETWORK = 1 << 18, // these can be used as dedicated weapon links that use seperate targeting computers isolated from main system, just dont connect them to main system!
            LINE_VENTILATION = 1 << 19,
            LINE_MECHANICAL_LINKAGE = 1 << 20,
            DAMAGE = 1 << 21,    // traversing this tile means an agent could potentially fall through to lower deck
            FIRE = 1 << 22,      // shouldn't FIRE, VACUUM and HAZARD be cell wide flags and not per tile?
            VACUUM = 1 << 23,    // depressurized/vacuum/trace air (such as tile beyond an airlock or door)
            HAZARD = 1 << 24,    // acid, poison, gas, radiation
                                 // can't "fire, Vacuum, hazard be combined into just one "Hazard" flag?
            ANY = COMPONENT | COMPONENT_TRAVERSABLE | COMPONENT_STACKABLE
        }

        // Connecting what are essentially an array of 2D TileMaps should not be so difficult.  
        // All we really need is a flag on a bottom tile that says it connects to the upper level.  That's it!


        // TODO: why do we need a seperate enum?  surely we can just reserve these "upper" attributes toward the bottom and we can test against them.
        //       i dont think there's a good reason to have them as seperate enum and divided into hi vs lo words. 
        //       Well, when i add/remove flags, the entity's upper and lower flags need to be seperated, but not exactly, they still exist in one single 32bit flag variable.
        //       We just need to know what part of the flags is for the lower tiles and which for the upper level tiles.
        // I need to get this working soon.  This entire problem with pathing up/down needs to be resolved by March 1, 2019.
        // Is there a way to do this without having lower footprint use a higher footprint too?  
        // I think i should just reserve 1 byte (8 bits) for upper level flags and 24bits for lower level.  
        [Flags]
        public enum TILE_UPPER_ATTRIBUTES : short // could this just be 1 byte and then lower attributes can be 24bits?
        {
            None = 0,
            SUPPORT = 1 << 0               // not entirely necessary since for "SUPPORT" we can just test if WALL exists on the level beneath us.

        }

        ///// <summary>
        ///// All flags that are setable in our "footprint" datalayer
        ///// </summary>
        //[Flags]
        //public enum TILEMASK_FLAGS : int
        //{
        //    NONE = 0,
        //    BOUNDS_IN = 1 << 0, // bit0 - painted by bounds painting. NOTHING can exist/be placed in a cell where bit BOUNDS_IN = 0
        //    OBSTACLE_GROUND_MOUNTED = 1 << 1,
        //    OBSTACLE_WALL_MOUNTED = 1 << 2,
        //    OBSTACLE_CEILING_MOUNTED = 1 << 3,
        //    OBSTACLE_ACCESS_SPACE = 1 << 4,        // walkable, but a buffer area preventing placement of other components (this may not be necessary. user should just leave space for NPC's to navigate obstacles themselves instead of enforcing it in footprint)
        //    OBSTACLE_LINE = 1 << 5, // prevents multiple lines from being drawn thru same tiles
        //    OBSTACLE_ATTRIBUTE_HALF_HEIGHT = 1 << 6,
        //    OBSTACLE_ATTRIBUTE_POUROUS = 1 << 7, // like a chain link fence
        //    OBSTACLE_ATTRIBUTE_TRANSPARENT = 1 << 8, // like a glass pane


        //    // obstacle placement tests no problem, but how do we do architectural/structural
        //    // provider/consumer tests using flags since the consumer and provider flags arent even the same!?
        //    // if trying to place a support consumer, we must check that a support provider exists in the dest footprint
        //    // - structural support can be assigned in cell[] struct


        //    // TODO: first tile at any Y altitude that has no inbounds cell beneath it is automatically
        //    // a support provider and is itself supported horizontally and vertically 
        //    STRUCTURE_FLOOR = 1 << 9,     // has a floor, requires support
        //    STRUCTURE_CEILING = 1 << 10,   // ceiling might be obsolete, indicates cannot climb upwards from this tile (but isnt this same as saying there is a floor tile in the footprint Y+1 above? yes it is.


        //    // TODO: this is a problem, walls and floors set the same SUPPORT_PROVIDER_VERTICAL and SUPPORT_CONSUMER
        //    //       so they set the same flags on the tilemask and so if you remove one or the other you have no idea
        //    //       when those flags should be cleared if both are gone or one remains
        //    // In my connectivity Area generation, instead of checking for OBSTACLE_* we should check seperately for obstacles, walls, etc
        //    // and never combine flags here in TILEMASK_FLAGS.  Because again, if a wall is both a ground mounted obstacle and a structural wall,
        //    // then removing the wall will remove the FLOOR flags underneath it as well even though we have not removed the floor.
        //    STRUCTURE_WALL = OBSTACLE_GROUND_MOUNTED | 1 << 11, // an obstacle with support for support consumers to use
        //    // TODO: railings are not structure, they are edge segments though... a fence i think shoudl be no different than perhaps a weak wall that is pourous
        //    //STRUCTURE_RAILING = OBSOLETE - a railing is just a floor mounted obstacle STRUCTURE_EDGE_SEGMENT | OBSTACLE_ATTRIBUTE_POUROUS | OBSTACLE_GROUND_MOUNTED_HALF_HEIGHT,   // has railing. must exist on top of floor or wall, but cannot have wall on top
        //    //STRUCTURE_FENCE = obsolete - a fence is not a structure, it is an obstacle - STRUCTURE_EDGE_SEGMENT | OBSTACLE_ATTRIBUTE_POUROUS | OBSTACLE_GROUND_MOUNTED_FULL_HEIGHT,     // a wall with visibility through it, but not traversibility


        //    COMPONENT_FLOOR_MOUNTED = OBSTACLE_GROUND_MOUNTED,
        //    COMPONENT_WALL_MOUNTED = OBSTACLE_WALL_MOUNTED,
        //    COMPONENT_CEILING_MOUNTED = OBSTACLE_CEILING_MOUNTED,
        //    COMPONENT_DOOR = COMPONENT_WALL_MOUNTED | ACCESSIBLE_THROUGH,      // allows passing through walls/railings
        //    // a hatch goes hand in hand with a ladder component.  This is tricky.  The component is accessible "THROUGH" but it requires
        //    // a ladder to use it to traverse up and down the ladder through the hatch.
        //    COMPONENT_HATCH = COMPONENT_FLOOR_MOUNTED | ACCESSIBLE_THROUGH,     // allows passing through floors/ceilings

        //    // todo: i dont think i need seperate component types for these.  They are just obstacles, but
        //    //       we use seperate flags altogether and the landings to indicate accessible_up_down.
        //    // In fact, even without the ladder component, if you have accessible_Up_down flag set on tiles (and thus in an Area)
        //    // then the npc can move to that area, determine if it's for a ramp, stairs, ladder or lift that it might have to wait on, and use correct animation
        //    // to traverse upwards
        //    COMPONENT_LADDER = COMPONENT_FLOOR_MOUNTED | ACCESSIBLE_UP_DOWN | 1 << 12,
        //    COMPONENT_STAIRS = COMPONENT_FLOOR_MOUNTED | ACCESSIBLE_UP_DOWN | 1 << 13,
        //    // elevator is COMPONENT_LIFT and perhaps can move up through COMPONENT_SHAFT
        //    COMPONENT_LIFT = COMPONENT_FLOOR_MOUNTED | ACCESSIBLE_UP_DOWN | 1 << 14,
        //    // COMPONENT_RAMP  // ramp is basically same as STAIRS
        //    ACCESSIBLE_THROUGH = 1 << 15, // door or hatch for instance
        //    ACCESSIBLE_UP_DOWN = 1 << 16, // if no obstacle is up or down, (no floor or ceiling) then shouldn't this flag be unnecessary? Perhaps it's for the landing of the ladder to show
        //                                  // where a NPC must stand to go up/down and our Area generation can make that a single area so when pathfind searching, we never needs to check the grid, only the Areas generated.
        //                                  // but i think we need seperate flags for the upper landing of a stairs and the lower landing... and inbetween it's ACCESSIBLE_THROUGH

        //    // The following tile flags do not result in recalc of the Area and Portal connectivity.  or what?
        //    LINE_POWER = OBSTACLE_LINE | 1 << 17,
        //    LINE_FUEL = OBSTACLE_LINE | 1 << 18,
        //    LINE_PLUMBING = OBSTACLE_LINE | 1 << 19,
        //    LINE_NETWORK = OBSTACLE_LINE | 1 << 20, // these can be used as dedicated weapon links that use seperate targeting computers isolated from main system, just dont connect them to main system!
        //    LINE_VENTILATION = OBSTACLE_LINE | 1 << 21,
        //    LINK_MECHANICAL_LINKAGE = OBSTACLE_LINE | 1 << 22,

        //    AGENT = 1 << 23,     // has an NPC standing here
        //    DAMAGE = 1 << 24,    // traversing this tile means an agent could potentially fall through to lower deck
        //    FIRE = 1 << 25,
        //    VACUUM = 1 << 26,    // depressurized/vacuum/trace air (such as tile beyond an airlock or door)
        //    HAZARD = 1 << 27,    // acid, poison, gas, radiation
        //    ALL = int.MaxValue  // TODO: this should never be used.
        //}


        // as far as parallelism, the collaborative diffusion paper
        // suggests simply dividing up the diffusion space equally between processors
        // but im missing something... because it seems sense must diffuse across any divisions we 
        // set up so how do we avoid having to just flood through with that new info when it's ready. 
        // that doesnt save us any time at all.
        internal AI.ConnectivityInterior mConnectivity;
        protected CellMap mCellMap;
        private string mDatatPath;
        public const float CEILING_HEIGHT_OFFSET = -0.125f; 

        #region Structure Visualization 
        Dictionary<uint, MinimeshMap> mEdgeModelMap = new Dictionary<uint, MinimeshMap>();


        protected Model _model;            // usually null for interiors because _selector is used instead
        protected ModelSelector _sequence; // can be null, but shouldnt be for interiors

        public static readonly char PREFIX_DELIMIETER = '_';
        public static readonly string SENTINEL = "$";
        public static readonly string PREFIX_CEILING = "ceiling";
        public static readonly string PREFIX_FLOOR = "floor";
        public static readonly string PREFIX_WALL = "wall";
        #endregion



        /// <summary>
        /// StartX contains the LOCAL SPACE center position.X of the smallest position.X cell on the axis
        /// The actual left and right cell bounds then become
        /// StartX * offsetX +/- sizeX where +/- depending on which side of origin we want
        /// </summary>
        public float StartX { get; private set; }
        /// <summary>
        /// StartY contains the LOCAL SPACE center position.Y of the smallest position.Y cell on the axis
        /// The actual top and bottom cell bounds then become
        /// StartY * offsetY +/- sizeY where +/- depending on which side of origin we want
        /// </summary>
        public float StartY { get; private set; }
        /// <summary>
        /// StartZ contains the LOCAL SPACE center position.Z of the smallest position.Z cell on the axis
        /// The actual front and back cell bounds then become
        /// StartZ * offsetZ +/- sizeZ where +/- depending on which side of origin we want
        /// </summary>
        public float StartZ { get; private set; }
        public float StopX { get; private set; }
        public float StopY { get; private set; }
        public float StopZ { get; private set; }

        // typical high density interior has 1.25 meter x 1.25 meter x 1.25 meter cells
		// where 1 cell height is for a crawl space, and 2 cells are required to stand.
		// high density exterior is 12.5 meters x 12.5 meters with 1.25 meter tile masks.
		// Remember, we can page zone data in/out so even our exterior cell data can be
		// paged in when we get close.
		// Typically the main reason our Cell's contain Tiles is to enforce placement of fixed size structural
		// segments like walls and floor tiles.  This is especially considering the edgeIDs are based on
		// that overlay resolution of 1.25x1.25.  For exteriors, that overlay could be even bigger.. say 
		// fullwidth and fullheight with only concern being the joining up of adjacents.
        private Vector3d mCellSize;  
                                     
        // individual cell/segment/structure width (x), cell height (y) and cell depth (z)
        public Vector3d CellSize { get {return mCellSize;} }   
        

        public uint VertexCountX { get { return CellCountX + 1; } } // horizontal vertex count is always 1 more than horizontal CellCount
        public uint CellCountX { get; set; }  // number of cells in x dimension
        public uint CellCountY { get; set; } // number of cells in y dimension
        public uint CellCountZ { get; set; } // number of cells in z dimension
        // TODO: it's occurring to me that the concept of "cells" which i wanted to equate to
        //       2d isometric "tiles" and the use of "stacks" of items in those tiles is just wrong.
        //       we are a 3d game and all we really want is a grid with the ability to have different
        //       grid resolution and the ability to determine  edges from id's and such.  Otherwise, we are
        //       not using "stacks" of entities.  We are placing (non structural) entities into an
        //       quadtree spatial grid.

        public uint TilesPerCellX // number of inner tiles in a single cell in x dimension
        {
            get;
            set;
        } 
        public uint TilesPerCellZ { get; set; } // number of inner tiles in a single cell in z dimension
        public Vector3d TileSize { get { return new Vector3d(mCellSize.x / TilesPerCellX, mCellSize.y, mCellSize.z / TilesPerCellZ); } }

        /// <summary>
        /// TileStartX contains the LOCAL SPACE center position.X of the smallest position.X tile on the axis
        /// The actual left and right tile bounds then become
        /// TileStartX * offsetX +/- sizeX where +/- depending on which side of origin we want
        /// </summary>
        public float TileStartX { get; private set; }
        /// <summary>
        /// TileStartY contains the LOCAL SPACE center position.Y of the smallest position.Y tile on the axis
        /// The actual top and bottom tile bounds then become
        /// TileStartY * offsetY +/- sizeY where +/- depending on which side of origin we want
        /// </summary>
        public float TileStartY { get; private set; }
        /// <summary>
        /// TileStartZ contains the LOCAL SPACE center position.Z of the smallest position.Y tile on the axis
        /// The actual front and back tile bounds then become
        /// TileStartZ * offsetZ +/- sizeZ where +/- depending on which side of origin we want
        /// </summary>
        public float TileStartZ { get; private set; }
        public float TileStopX { get; private set; }
        public float TileStopY { get; private set; }
        public float TileStopZ { get; private set; }



        public bool EmptyCellsAffectVolume { get; set; }

        // todo: this isn't really a database.  footprint data is stored differently than entity/component location data
        public CellMap Database { get { return mCellMap; } }
         


        public Interior(string id)
            : base(id)
        {
            EmptyCellsAffectVolume = false;
            mEntityFlags |= EntityAttributes.Region | EntityAttributes.Structure;

            int test = 1;
            byte[] uint24 = BitConverter.GetBytes(test);
            if (BitConverter.IsLittleEndian)
            {
                int result = BitConverter.ToInt32(uint24, 0);
            }
            else
            {
            }
        }

        public Interior(string id, Vector3d cellSize,
            uint cellCountX, uint cellCountY, uint cellCountZ)
            : this(id, (float)cellSize.x, (float)cellSize.y, (float)cellSize.z, 
            cellCountX, cellCountY, cellCountZ)
        {
            _resourceStatus = PageableNodeStatus.NotLoaded;
        }

        public Interior(string id, Vector3d cellSize,
                uint cellCountX, uint cellCountY, uint cellCountZ, uint spatialTreeDepth)
            : this(id, (float)cellSize.x, (float)cellSize.y, (float)cellSize.z,
            cellCountX, cellCountY, cellCountZ, spatialTreeDepth)
        {
        }

        public Interior(string id, float cellWidth, float cellHeight, float cellLength,
            uint cellCountX, uint cellCountY, uint cellCountZ)
            : this(id, cellWidth, cellHeight, cellLength,
                    cellCountX, cellCountY, cellCountZ, 0)
        {
        }

        public Interior(string id, float cellWidth, float cellHeight, float cellLength,
                uint cellCountX, uint cellCountY, uint cellCountZ, 
                uint spatialTreeDepth)
            : this(id)
        {
            mCellSize.x = cellWidth;
            mCellSize.y = cellHeight;
            mCellSize.z = cellLength;

            CellCountX = cellCountX;
            CellCountY = cellCountY;
            CellCountZ = cellCountZ;

            // August.20.2017 - out of memory exception in Cellmap footprintData 
            //                  so instead of 16x16 we're downsizing to 8x8 which is still really
            //                  good resolution when using 1.25x1.25 meter Cells.
            TilesPerCellX = 16; // 16; // hardcode default init inner tile count
            TilesPerCellZ = 16; // 16; // hardcode default init inner tile count

            ConnectivityDirty = new bool[CellCountY];
            for (int i = 0; i < CellCountY; i++)
                ConnectivityDirty[i] = true;

            // spatial tree depth is used by the CelledRegionNode 
            if (spatialTreeDepth > 0)
                mMaxSpatialTreeDepth = spatialTreeDepth;

        }

        ////public override Node Clone(string cloneID, bool recurseChildren, bool neverShare, bool delayResourceLoading = true)
        ////{
        ////    Node clone = base.Clone(cloneID, recurseChildren, neverShare, delayResourceLoading);

        ////    // TODO: i think our GetDatabasePath should return a path where the Ship prefab is located.
        ////    //       this goes both for the prefab in our Mods directory, and the prefab that is actually a live object
        ////    //       in an active scene.  I'm leaning towards removing a single set of XML files for storing
        ////    //       our scene and instead using SQLite and a bunch of live object prefabs stored in the Scene\\Live path.
        ////    //       Further, those entities i think should contain the entire node hierarchy for that entity and not
        ////    //       just references to Appearance, Mesh, Actor, etc, xml db's.
        ////    //       - do we need to do this right now?  Or can we ignore it and see what is causing our interior's to not render.
        ////    //         and why the exterior is not utilizing the opacity setting?
        ////    // TODO: when the import as container occurs, we should create the cell db in the same path as the prefab we've created.

        ////    // TODO: the below file copy and database rename is already occurring inside of LoadTVResource() right?
        ////    // change the mDataPath
        ////    //string relativeDBPath = Interior.GetDatabasePath(clone.ID);

        ////    // TODO: are we saving each deck's tv mesh as we generate them?
        ////    // TODO: how are we saving the Wall 
        ////    // TODO: are we even reading/writing to the datapath file?
        ////    // TODO: this is problematic, how do we clone the cell db when using assetplacementtool?
        ////    //       furthermore, the path to the prefab's cell db needs to be in the prefab's folder
        ////    //       but the path to the live db, needs to be in the Scenes folder?  What happens when we
        ////    //       reload the saved scene?  does it know where the live db resides?
        ////    //       And what about sqlite db?  How does that fit in with a prefab AND a live vehicle?
        ////    //       And how does that fit in with network client/server?  I think for starters, we should
        ////    //       forget about network client/server and just focus on single player.   
        ////    //       We still haven't resolved how we will store Components, in Prefab or SQLIte? 
        ////    //       - i think live objects should be stored as live prefabs in the Scene directory
        ////    //       with sqlite db entry to find them? 
               
        ////    // If the prefab had a saved cell db, then clone() needs to copy that too otherwise
        ////    // we must always construct Interior structure by hand every time we load a Container prefab.
        ////    //if (mDataPath != null)
        ////    //{
        ////    //    // TODO: verify when we resave the prefab after making Structural changes manually, that the
        ////    //    // mCellDatabasePath.ToString() for the prefab is no longer null.  
        ////    //    // And keep in mind, the  path the prefab's celldatabase needs to be in the prefabs directory.
        ////    //    // So when we save the prefab, we cannot use GetDatabasePath()
        ////    //    string fullDBPath = System.IO.Path.Combine(Core._Core.DataPath, relativeDBPath);
        ////    //    System.IO.File.Copy(mCellDatabasePath, fullDBPath);

        ////    //    Settings.PropertySpec dbPathSpec = new Settings.PropertySpec("datapath", typeof(string).Name, relativeDBPath);
        ////    //    clone.SetProperties(new Settings.PropertySpec[] { dbPathSpec });
        ////    //}
        ////    return clone;
        ////}

        #region ITraversable Members
        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        #region ResourceBase members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {

            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[8 + tmp.Length];
            tmp.CopyTo(properties, 8);

            properties[0] = new Settings.PropertySpec("cellsize", mCellSize.GetType().Name);
            properties[1] = new Settings.PropertySpec("cellcountx", typeof(uint).Name);
            properties[2] = new Settings.PropertySpec("cellcounty", typeof(uint).Name);
            properties[3] = new Settings.PropertySpec("cellcountz", typeof(uint).Name);
            properties[4] = new Settings.PropertySpec("spatialtreedepth", typeof(uint).Name);
            properties[5] = new Settings.PropertySpec("datapath", typeof(string).Name); // todo: holds MOD_PATH\\ mod relative path and filename or if in arcade holds SAVES_PATH\\ relativePath
            properties[6] = new Settings.PropertySpec("tilecountx", typeof(uint).Name);
            properties[7] = new Settings.PropertySpec("tilecountz", typeof(uint).Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = mCellSize;
                properties[1].DefaultValue = CellCountX;
                properties[2].DefaultValue = CellCountY; 
                properties[3].DefaultValue = CellCountZ;
                properties[4].DefaultValue = mMaxSpatialTreeDepth;
                //System.Diagnostics.Debug.Assert (string.IsNullOrEmpty(mDataPath.ToString()) || mDataPath.ToString() == GetDatabasePath (_id));
                properties[5].DefaultValue = mDatatPath;
                properties[6].DefaultValue = TilesPerCellX;
                properties[7].DefaultValue = TilesPerCellZ;
            }

            return properties;
        }

        // TODO: this should return any broken rules ?
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
                    case "cellsize":
                        mCellSize = (Vector3d)properties[i].DefaultValue;
                        break;
                    case "cellcountx":
                        CellCountX = (uint)properties[i].DefaultValue;
                        break;
                    case "cellcounty":
                        CellCountY = (uint)properties[i].DefaultValue;
                        ConnectivityDirty = new bool[CellCountY];
                        for (int j = 0; j < CellCountY; j++)
                            ConnectivityDirty[j] = true;
                        break;
                    case "cellcountz":
                        CellCountZ = (uint)properties[i].DefaultValue;
                        break;
                    case "spatialtreedepth":
                        mMaxSpatialTreeDepth = (uint)properties[i].DefaultValue;
                        break;
                    case "datapath":
                        mDatatPath = (string)properties[i].DefaultValue;

                        //    System.Diagnostics.Debug.Assert(string.IsNullOporEmpty(mDatatPath.ToString()) || mDatatPath.ToString() == GetDatabasePath(_id));
                        break;
                    case "tilecountx":
                        TilesPerCellX = (uint)properties[i].DefaultValue;
                        break;
                    case "tilecountz":
                        TilesPerCellZ = (uint)properties[i].DefaultValue;
                        break;
                }
            }
            
            InitializeStartStop();
        }
        #endregion


        private void InitializeStartStop()
        {
            float start, stop;
            // Start and Stop's contain the LOCAL SPACE center position offsets for the start and end cells
            // on those respective axis.  The actual cell bounds then become
            // StartX * offsetX +/- sizeX where +/- depending on which side of origin we want
            // see Mesh3d.CreateCellGrid() for a version that calcs cell boundaries
            ZoneRoot.GetStartStop(out start, out stop, CellCountX);
            StartX = start;
            StopX = stop;
            ZoneRoot.GetStartStop(out start, out stop, CellCountY);
            StartY = start;
            StopY = stop;
            ZoneRoot.GetStartStop(out start, out stop, CellCountZ);
            StartZ = start;
            StopZ = stop;

            ZoneRoot.GetStartStop(out start, out stop, CellCountX * TilesPerCellX);
            TileStartX = start;
            TileStopX = stop;

            TileStartY = StartY;
            TileStopY = StopY;

            ZoneRoot.GetStartStop(out start, out stop, CellCountZ * TilesPerCellZ);
            TileStartZ = start;
            TileStopZ = stop;

        }

        #region IPageableTVNode Members
        public override void LoadTVResource()
        {

            InitializeStartStop();
            mCellMap = new CellMap(this.ID);

            base.LoadTVResource(); // base class will load any script

            // Load the prototypes for our structure visuals.  
            // meshes for our floors and walls must be created regardless of whether
            // we're loading from existing database or new
            // TODO: LoadStructureVisuals() call results in floor/ceiling meshes being created. Consider the placement tool.
            // if we unload the vehicle entity before the rest of LoadstructureVisuals() returns, it will likely end up
            // trying to modify those interior meshes (eg. collapse floor tiles or modify UVs) after those mesh3d's are unloaded.
            // thus we should lock access to our Group.RemoveChild perhaps if that group has IResource interface and
            // is currently being loaded?			
            try
            {
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Beging loading structure visuals.");
                LoadStructureVisuals();
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Completed loading of structure visuals.");
            }
            catch (OutOfMemoryException oomException)
            {
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - " + oomException.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - ERROR: " + ex.Message);
            }

            // determine the path to the db file.  We can do this because a naming convention is enforced
            // NOTE: Paths are different for the prefab and the live instance.  Only the live instance
            // will have this LoadTVResource() called because our AssetPlacementTool will not load
            // the Interior region of a Container vehicle.
            string relativeDataPath = (string)GetProperty("datapath", false).DefaultValue;
            System.Diagnostics.Debug.Assert(string.IsNullOrEmpty(relativeDataPath) == false, "Interior.LoadTVResource() - Path should not be null.");

            string fullDataPath;
            if (Core._Core.Scene_Mode == Keystone.Core.SceneMode.Simulation)
                fullDataPath = System.IO.Path.Combine(Core._Core.SavesPath, relativeDataPath);
            else if (System.IO.File.Exists(Core.FullNodePath(relativeDataPath)))
                // the only time modspath\\ is used is when explicitly loading a prefab directly from mods or when explicitly saving a prefab to mod location.  All other times its ScenesPath\\
                fullDataPath = System.IO.Path.Combine(Core._Core.ModsPath, relativeDataPath);
            else
                fullDataPath = System.IO.Path.Combine(Core._Core.ScenesPath, relativeDataPath);

            // there are the following requirements
            // - user has ability to SAVE and LOAD prefabs from the mods\\modname\\  folders
            // - user has ability to create and edit campaign missions
            // - user can _PLAY_ a "mission" and it is pre-canned in the datapath\\campaigns\\campaign name\\missions\\mission name folder

            //- user has ability to save their progress in a campaign (even if it just means tracking missions completed.
            //      - but things like crew deaths should be saved from mission to mission



            if (!System.IO.File.Exists(fullDataPath))
            {
                // the interior database gets created at the end of Worker_ImportGeometry() and it gets copied during NewUniverse generation to the saves folder.
                // it MUST exist already.

                throw new System.IO.FileNotFoundException("Interior.LoadTVResource() - Interior data file not found.");


                //SetChangeFlags(Keystone.Enums.ChangeStates.CellDataLoaded, Keystone.Enums.ChangeSource.Self);
            }

            // Obsolete - Copying of the Vehicle prefab's CellDB is done in the Worker() that places the Container vehicle.
            //else if (System.IO.File.Exists(mDataPath.ToString()))
            //{
            //    // we clone the existing using our new path
            //    // TODO: is there a race condition here and with celledRegion.Clone() and SceneReader.ReadEntityProperties()
            //    System.IO.File.Copy(fullDBPath, path, true);
            //}


            System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Loading Interior data " + fullDataPath);
            System.IO.FileInfo info = new System.IO.FileInfo(fullDataPath);
 
            if (info.Length == 0)
            {
                // this db is empty.  we start with empty cell structure
                //SetChangeFlags(Keystone.Enums.ChangeStates.CellDataLoaded, Keystone.Enums.ChangeSource.Self);
                return;
            }
            try
            {
                // still here, try to read the data
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Beging read cell data.");
                mCellMap.ReadCellData(fullDataPath, CellCountX, CellCountY, CellCountZ);
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Completed reading of cell data.");

                // iterate and call our DomainObject script as if these settings are being
                // generated by player or we're just playing back a recording.
                // NOTE: the footprint layer data is generated as we read and apply
                // structure to the layers.
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Beging applying boundary data.");
                for (uint y = 0; y < CellCountY; y++)
                    for (uint x = 0; x < CellCountX; x++)
                        for (uint z = 0; z < CellCountZ; z++)
                        {
                            // reconstruct the cells that are in bounds
                            bool value = (bool)Layer_GetValue("boundaries", x, y, z);

                            uint index = FlattenCell(x, y, z);

                            Layer_ValueChanged("boundaries", index, value);
                        }
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Completed applying boundary data.");

                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Begining applying tile styles.");
                for (uint y = 0; y < CellCountY; y++)
                    for (uint x = 0; x < CellCountX; x++)
                        for (uint z = 0; z < CellCountZ; z++)
                        {
                            uint index = FlattenCell(x, y, z);
                            // "tile style" contains both ceiling and floor halves
                            object result = Layer_GetValue("tile style", index);

                            // default is null, only perform ValueChanged event if non null
                            if (result != null)
                            {
                                EdgeStyle style = (EdgeStyle)result;
                                Layer_ValueChanged("tile style", index, style);
                            }
                        }
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Completed applying tile styles.");

                // reconstruct walls - NOTE: here index is the edgeIndex so to iterate these we first need total edge count
                CellEdge.EdgeStatistics es = CellEdge.GetEdgeStatistics(CellCountX, CellCountY, CellCountZ);
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Begining applying wall styles.");
                for (uint edgeID = 0; edgeID < es.MaxEdgeCount; edgeID++)
                {
                    object result = Layer_GetValue("wall style", edgeID);
                    if (result != null)
                    {
                        EdgeStyle style = (EdgeStyle)result;
                        try
                        {
                            bool loaded = CreateEdgeSegmentStyle(edgeID, style);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - ERROR: CreateEdgeSegmentStyle - " + ex.Message);
                        }
                        Layer_ValueChanged("wall style", edgeID, style);
                    }
                }
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Completed applying wall styles.");


                // TODO: obsolete? these flags are obsolete
                //DisableChangeFlags(Keystone.Enums.ChangeStates.CellDataLoaded);

                //SetChangeFlags(Keystone.Enums.ChangeStates.CellDataLoaded, Keystone.Enums.ChangeSource.Self);

                // since our visual structures (floor, wall meshes and such) are generated here
                // in LoadTVResource and are thus done on a pager thread, we can now start to
                // apply the cell data to re-create the proper look of the floor WITHOUT having to WORRY
                // about the order of operations of whether floors are available or not.
                // We only need to ensure that components aren't "AddChild()" to this CelledRegion
                // until after LoadTVResource() completes.
                // Wait, there is still the issue of the domain object having been loaded!
                // OK, so i think all we need is a flag for TVResourceLoaded and then when DomainObject
                // loads we can immediately start to apply
            }
            catch (System.IO.InvalidDataException ex)
            {
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - ERROR: " + ex.ToString());
            }
            catch (OutOfMemoryException oomEx)
            {
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - ERROR: " + oomEx.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - ERROR: " + ex.ToString());
            }

            System.Diagnostics.Debug.WriteLine("Interior.LoadTVResource() - Loading Interior COMPLETE");
        }

        public override void UnloadTVResource()
        {
            base.UnloadTVResource();

            // todo: unload all visuals from here and Repository


        }

        public override void SaveTVResource(string filepath)
        {
            // when called from outside, we write to the map
            // TODO: it'd be nice if we could write cell data after all Layer_SetValues for a given
            //       paint operation were done, rather than after each seperate modified cell...
            //       this can still be done if we do it from the Worker... although
            //       normal calls to this function should they not call WriteCellData?  what is a "normal" call
            //       isnt it always from the worker?  Shouldnt the workers only be allowed to call .WriteCellData then?
            //System.Diagnostics.Debug.WriteLine("CelledRegion.SaveTVResource() - Saving cell data " + mDataPath.ToString());
            
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(filepath);
            fileInfo.Directory.Create();

            mCellMap.WriteCellData(filepath);
        }
        #endregion

        // OBSOLETE
        ///// <summary>
        ///// Retrieves the live cell database path, not the prefab db path.
        ///// </summary>
        ///// <param name="celledRegionID"></param>
        ///// <returns></returns>
        //public static string GetDatabasePath(string celledRegionID)
        //{
        //    // TODO: shouldn't this be saved in the Vehicle prefab's directory and not in \\scenes?
        //    //       but then, what happens when multiple instances of the prefab are loaded?
        //    //       The interior db does in fact only need to be saved to the vehicle's prefab
        //    //       during Mod making, not during game runtime.
        //    string relativeFolder = "scenes\\" + Core._Core.SceneName + "\\";
            
        //    string path = string.Format(System.IO.Path.Combine (relativeFolder, celledRegionID + ".interior"));
        //    string fullPath = System.IO.Path.Combine(Core._Core.DataPath, path);
        //    string dirPathOnly = System.IO.Path.GetDirectoryName(fullPath);
        //    System.IO.FileInfo fileInfo = new System.IO.FileInfo(dirPathOnly);
            
        //    fileInfo.Directory.Create();
        //    return path;
        //}

        // used as a tag to Mesh3d.CreateCellGrid() to ensure the grids are unique and not shared
        // since they require independantly modifiable UVs and vertex positions.
        public static string GetInteriorElementPrefix(string interiorID, Type t)
        {
            return SENTINEL + PREFIX_DELIMIETER + 
                interiorID + PREFIX_DELIMIETER + 
                t.Name;
        }

        public static string GetInteriorElementPrefix(string interiorID, string description, Type nodeType)
        {
            return GetInteriorElementPrefix(interiorID, nodeType) 
                + PREFIX_DELIMIETER +
                description;
        }

        public static string GetInteriorElementPrefix(string interiorID, string description, Type nodeType, int index)
        {
            return GetInteriorElementPrefix(interiorID, description, nodeType) 
                + PREFIX_DELIMIETER +
                index.ToString();
        }

        /// <summary>
        /// Similar to GetInteriorElementPrefix but includes a hash of the prefabPath and texturePath so that
        /// model's with MinimeshGeometry using same hash and floor index can be shared.
        /// </summary>
        /// <param name="prefabPath"></param>
        /// <param name="texturePath"></param>
        /// <param name="floor"></param>
        /// <returns></returns>
        private string GetModelSequenceID(string prefabPath, uint floor)
        {
            uint result = 0;
            Keystone.Utilities.JenkinsHash.Hash(prefabPath, ref result);
            // TODO: verify there is an Entity Script assigned and that we didn't throw a handled exception where we were expecting a script.
            System.Diagnostics.Debug.Assert(result != 0);

            string description = PREFIX_WALL + PREFIX_DELIMIETER + result.ToString();
            
            // for different edges, we don't need to be able to Select different sub-models.
            // when we add this modelSelector to _sequence, we render all of them, but only the MinimeshGeometry nodes
            // with relevant Element's added will render visible geometry.  Thus, when auto-tiling, we need to be
            // able to remove elements from MinimeshGeometry that is no longer relevant and add them to another
            // NOTE: description includes a hash value of the prefabPath so we can find the correct ModelSequence in the Repository.
            string modelSequenceID = GetInteriorElementPrefix(this.ID, description, typeof(ModelSequence), (int)floor);
            return modelSequenceID;
        }


        public int Layer_GetHashCode(string layerName)
        {
            return mCellMap.GetHashCode(layerName);
        }

        public object Layer_GetValue(string layerName, uint index)
        {
            return mCellMap.GetMapValue(layerName, index);
        }

        public object Layer_GetValue(string layerName, uint x, uint y, uint z)
        {
            return mCellMap.GetMapValue(layerName, x, y, z);
        }

        public object Layer_GetData(string layerName)
        {
            return mCellMap.GetMapData(layerName);
        }


        /// <summary>
        /// Changes a value in the specified layer.  Layer hashCode is updated to reflect change
        /// so that objects tracking changes in layers can simply test if the hashCode for a layer
        /// has changed. 
        /// </summary>
        /// <param name="layerName"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <remarks>Current implementation we ensure that a CelledRegion's cell data
        /// and models, meshes and domainObject are all already loaded BEFORE calls to Layer_SetValue
        /// will take place.  However, if the user in mod design mode manually deletes their
        /// DomainObject, it could cause problems.
        /// </remarks>
        public void Layer_SetValue(string layerName, uint index, object value)
        {
            // June.17.2012 - removed all validation.  Validation is more app centric (eg CommandProcessor)
            // and script centric and should not be hard coded here.

            // TODO: when drawing power lines for instance, index is a tile index and not a cell index
            //       this difference we do between the two is just horribly problematic.  we either need to make them all
            //       per tile index and then convert to cell, or we need seperate calls 
#if DEBUG
            if (layerName == "tile style")
                System.Diagnostics.Debug.WriteLine("tile style = " + value.ToString());
#endif
            mCellMap.SetMapValue(layerName, index, value);
            // TODO: this should be assigned as an Event and then we rename Layer_ValueChanged to
            // CellMap_OnValueChanged
            Layer_ValueChanged(layerName, index, value);
            UpdateAdjacents(layerName, index);
        }

//        public void Layer_SetValue(string layerName, uint x, uint y, uint z, object value)
//        {
//            Layer_SetValue(layerName, index, value);
//        }

        
        // TODO: better if our cellMap raised this as an event 
        internal void Layer_ValueChanged(string layerName, uint index, object value)
        {
            // This is called from Layer_SetValue and PropogateChangeFlags


            // where do hatches and doors fit in?
            // like ideally speaking, it seems hatches and doors seem like componentns but maybe they are more part of structure?
            // and so take up edges and cells.  

            // floor style (thickness, etc)
            // floor appearance (atlas texture index)

            // NOTE - DomainObjects/Models/Meshes and such are now guaranteed to be instanced by the time this method is called
            //            Because for CelledRegion, the interior visuals are goverened entirely by the CelledRegion itself

            // TODO: SetSegmentStyle is called from script currently because it's an easy way to run
            //       our script logic when reloading our saved interiors.  The scripts run this way
            //       either on deserialization or during real-time editing.  
            //      TODO: having switched to tile x,y,z our interior script must now be able to cope with this as well 
            //      specifically when above we attempt to flattenCell (x,y,z) where x,y,z are in tile coords not cell
            //      it will result in an invalid index
            //      But what do we do in case of something like a Hatch that we want to collapse an entire cell?

            Execute("OnCelledRegion_DataLayerValue_Changed", new object[] { this.ID, layerName, index, value });
            NotifyObservers(layerName, index, value);
        }

        Dictionary<string, List<Entity>> mObservers = new Dictionary<string, List<Entity>>();
        
        public void RegisterObserver(Entity child, string layerName)
        {
            if (!mObservers.ContainsKey (layerName))
                mObservers[layerName] = new List<Entity>();

            List<Entity> entities = mObservers[layerName];
            entities.Add(child);
        }

        public void UnregisterObserver(Entity child, string layerName)
        {
            System.Diagnostics.Debug.Assert(mObservers[layerName] != null);
            System.Diagnostics.Debug.Assert(mObservers[layerName].Contains(child));

            mObservers[layerName].Remove(child);
            if (mObservers[layerName].Count == 0) mObservers[layerName] = null;
        }

        void NotifyObservers(string layerName, uint index, object value)
        {
            if (!mObservers.ContainsKey(layerName) || mObservers[layerName] == null) return;
            List<Entity> observers = mObservers[layerName];
            
            foreach (Entity observer in observers)
                observer.Execute("OnObserved_Value_Changed", new object[] { this.ID, observer.ID, layerName, index, value });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="startTileLocation"></param>
        /// <param name="rotatedFootprint"></param>
        /// <remarks>If this method is not being called, verify there is a script added to the entity we are trying to add.</remarks>
        private void OccupyTiles(Entity entity, Vector3i startTileLocation, int[,] rotatedFootprint)
        {
            if (rotatedFootprint == null) throw new ArgumentNullException();
            
            // convert the centered footprint location to the bottom, left most footprint tile
            //int originX, originZ;
            //GetDestinationOriginFromTileLocation(tileLocation, rotatedFootprint, out originX, out originZ);

            //System.Diagnostics.Debug.WriteLine(string.Format("ApplyFootprint() - X = {0} Z = {1}", originX, originZ));
            //if (originX < 0 || originZ < 0) throw new ArgumentOutOfRangeException("IsPlaceable should have detected this and prevented ApplyFootprint from being called.");
            
            
            bool result = ApplyFootprint((uint)startTileLocation.X, (uint)startTileLocation.Y, (uint)startTileLocation.Z, rotatedFootprint);

            int sourceWidth = rotatedFootprint.GetLength(0);
            int sourceHeight = rotatedFootprint.GetLength(1);
            if (sourceWidth == 0 || sourceHeight == 0) throw new ArgumentOutOfRangeException();

            // add the entity to all relevant tiles
            for (uint i = 0; i < sourceWidth; i++)
                for (uint j = 0; j < sourceHeight; j++)
                    // TODO: the following ATTRIBUTE test should never be needed because IsPlaceable() should validate first
                    if ((rotatedFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.COMPONENT) != 0 || (rotatedFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.ENTRANCE_AND_EXIT) != 0)
                        mCellMap.SetMapValue("floorentity", (uint)startTileLocation.X + i, (uint)startTileLocation.Y, (uint)startTileLocation.Z + j, entity);
                    

        }

        private void UnOccupyTiles(Entity entity, Vector3i startTileLocation, int[,] rotatedFootprint)
        {
            if (mCellMap == null) return;
            if (rotatedFootprint == null) throw new ArgumentNullException();

            // TODO: the following line was uncommented but don't we remove by tile in the loop below?
            //mCellMap.SetMapValue("floorentity", flattenedTileIndex, null);

            // convert the centered footprint location to the bottom, left most footprint tile
            //int originX, originZ;
            //GetDestinationOriginFromTileLocation(tileLocation, rotatedFootprint, out originX, out originZ);


            bool result = UnApplyFootprint((uint)startTileLocation.X, (uint)startTileLocation.Y, (uint)startTileLocation.Z, rotatedFootprint);

            int sourceWidth = rotatedFootprint.GetLength(0);
            int sourceHeight = rotatedFootprint.GetLength(1);
            if (sourceWidth == 0 || sourceHeight == 0) throw new ArgumentOutOfRangeException();

            // remove the entity from all relevant tiles
            for (uint i = 0; i < sourceWidth; i++)
                for (uint j = 0; j < sourceHeight; j++)
                    if ((rotatedFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.COMPONENT) != 0 || (rotatedFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.ENTRANCE_AND_EXIT) != 0)
                        mCellMap.SetMapValue("floorentity", (uint)startTileLocation.X + i, (uint)startTileLocation.Y, (uint)startTileLocation.Z + j, null);

        }

        public void ClearWalls()
        { }

        public void ClearFloors()
        { }

        public void FillFloors()
        {
            Keystone.Portals.EdgeStyle edgeStyle = new EdgeStyle();
            edgeStyle.FloorAtlasIndex = 4; 
            edgeStyle.CeilingAtlasIndex = 0; 
            edgeStyle.Prefab = null;


            for (uint k = 0; k < CellCountY - 1; k++)
                for (uint i = 0; i < CellCountX; i++)
                    for (uint j = 0; j < CellCountZ; j++)
                    {
                        uint index = FlattenCell(i, k, j);
                        if (IsCellBOUNDS_PAINTED_IN(i, k, j))
                        {
                            Layer_SetValue("tile style", index, edgeStyle);
                        }
                    }
        }

        public void FillGaps()
        {
            FillFloors();

            // auto-generate all exterior walls
            // NOTE: this requires that the BOUNDS of each floor be painted as desired for the given exterior hull geometry.
            Keystone.Portals.EdgeStyle edgeStyle = new EdgeStyle();
            edgeStyle.FloorAtlasIndex = -1; // null; // TODO: this should just use the texture in the prefab. System.IO.Path.Combine(AppMain.MOD_PATH, @"caesar\textures\walls\wall00.png"); // TODO: Texture application not working
            edgeStyle.CeilingAtlasIndex = -1; // null; // edgeStyle.BottomLeftTexturePath;
            edgeStyle.Prefab = @"caesar\entities\structure\walls\wall.kgbentity";
            
            CellEdge.EdgeStatistics stats = new CellEdge.EdgeStatistics(CellCountX, CellCountY, CellCountZ);
            
            for (uint edgeID = 0; edgeID < stats.MaxEdgeCount; edgeID++)
            {
                CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
                if (edge.BottomLeftCell == -1 || 
                    edge.TopRightCell == -1 ||
                    (!this.IsCellBOUNDS_IN((uint)edge.BottomLeftCell) && !this.IsCellBOUNDS_IN((uint)edge.TopRightCell)) || // both sides are out of bounds
                    (this.IsCellBOUNDS_IN((uint)edge.BottomLeftCell) && this.IsCellBOUNDS_IN((uint)edge.TopRightCell)))     // both sides are inbounds so it's not an exterior edge
                        continue;

                CreateEdgeSegmentStyle(edgeID, edgeStyle);
                ApplyEdgeSegmentStyle(edgeID, edgeStyle);
                Layer_SetValue("wall style", edgeID, edgeStyle);
            }


            // fills in the gaps between smaller upper floors and the floor beneath it
            // the gaps will still be BOUNDS_IN == false, but they will have geometry and a texture but no corresponding ceiling.
            // NOTE: this is just a visual "mask" and doesn't require any changes to rules or the footprint data layer.  Very elegant.
            // NOTE: this requires that at least one Cell on the deck has a "floor" texture assigned since this means the Model and Appearance will be assigned to the floor Model.
            for (uint k = 0; k < CellCountY - 1; k++)
            {
                string modelID = GetInteriorElementPrefix(this.ID, Interior.PREFIX_FLOOR, typeof(Model), (int)k + 1);
                Keystone.Elements.Model floorModel = (Model)Keystone.Resource.Repository.Get(modelID);
                Keystone.Appearance.Appearance appearance = floorModel.Appearance;

                if (appearance == null) continue; // warning: no atlas set
                if (appearance.Layers == null) continue;
                if (appearance.Layers.Length == 0) continue;
                if (appearance.Layers[0].Texture == null) continue;
                if (appearance.Layers[0].Texture is Keystone.Appearance.TextureAtlas == false) continue;

                Keystone.Appearance.TextureAtlas atlas = (Keystone.Appearance.TextureAtlas)appearance.Layers[0].Texture;

                for (uint i = 0; i < CellCountX; i++)
                    for (uint j = 0; j < CellCountZ; j++)
                    {
                        if (IsCellBOUNDS_PAINTED_IN(i, k, j))
                        {
                            if (!IsCellBOUNDS_PAINTED_IN(i, k + 1, j))
                            {
                                // floor only
                                Celestial.ProceduralHelper.CellGrid_SetCellUV(CellCountX, CellCountZ, i, j, (Mesh3d)floorModel.Geometry, atlas, 1);
                                Keystone.Celestial.ProceduralHelper.CellGrid_CellSetCollapseState(this.CellCountX, this.CellCountZ, (uint)i, (uint)j, (Mesh3d)floorModel.Geometry, false);
                            }
                        }
                    }
            }

            //SaveTVResource ()
        }

        private bool[] ConnectivityDirty;
        public void UpdateConnectivity()
        {
            // recompute Connectivity Areas
            // dynamic update to mConnectivity for the deck/floor in question
            // the hud cannot access Connectivity because it's in KeyEdit whereas Connectivity is internal to Keystone.dll

            // for Tiles, the index values in the Areas generated by ConnectivityInterior are in 3D so we unflatten to 3D 
            //uint x, y, z;
            //Keystone.Utilities.MathHelper.UnflattenIndex(index, CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ, out x, out y, out z);
            mConnectivity = new AI.ConnectivityInterior(this, this.mScene, CellCountX * TilesPerCellX, CellCountY, CellCountZ * TilesPerCellZ);

            
            for (int y = 0; y < CellCountY; y++)
            {
                // TODO: above we re-instantiate mConnectivity so all Areas accept the dirty one will end up being NULL.  So we recompute everything for now and comment out the if(ConnectivityDirty) statement
                //if (ConnectivityDirty[y])
                //{
                // TODO: we should probably just pas the mCellMap and then GenerateAreas can modify the footprint data for Y axis traversal
                // TODO: or we have to have the component with ACCESSIBLE_TO_UPPER modify both the lower and the upper tile data.  Then
                // when we create Portals, we could connect Y axis whenever there is an identicle footprint (in terms of XZ dimensions) above it. But
                // this means we would still need to pass more than just the current floor level Areas to the CreatePortals() method
                mConnectivity.Areas[y] = mConnectivity.GenerateAreas(y, this.mCellMap.mFootprintData);
                    // TODO: we cannot connect up/down traversal if we only send the areas for one deck at a time
                    // TODO: we could pass the entire jagged array of Areas.
                    // TODO: what makes the most sense?  I mean just look at what we're doing here?  We're calling functions(GenerateAreas) and methods (CreatePortals)
                    // on mConnectivity, and passing in the Areas that we already have access too. 

 // TODO - i think we can create all portals at once after all Areas on all floors have been generated.  Its not computationally too expensive i dont think.  need to profile it.
  //                  mConnectivity.CreatePortals(mConnectivity.Areas[y], y);

                    // TODO: Or do we add a 3rd method to create the Y axis portals?
                    // In that case, we would scan only Areas for Up/Down accessible tile values and create new portals that way, but we'd still
                    // need the upper Area to exist.  And the easiest way to do that is when adding the components with the accessible up/down flag.

                //    ConnectivityDirty[y] = false;
                    System.Diagnostics.Debug.WriteLine("Interior.UpdateConnectivity() - " + y.ToString());
                //}
            }

            mConnectivity.CreatePortals(mConnectivity.Areas);

        }

        Vector3d[] mPathPoints;
        // TODO: the first problem here is, the start and end vectors are not resulting in TileLocations that are being rendered by the FloorplanHud.cs
        // TODO: We need more information than just Vector3d array.  We need information about things like "doors" and "elevators" so that our
        //       BehaviorTree can determine how to proceed.
        internal Vector3d[] PathFind(Entity npc, Vector3d start, Vector3d end, out string[] components)
        {
            if (mConnectivity == null || mConnectivity.Areas == null)
            {
                components = null;
                return null;
            }
            // TODO: test code to path from one tile to another 
            AI.PathingInterior pathing = new AI.PathingInterior();

            AI.PathFindParameters parameters;
            parameters.AllowDiagonalMovement = false;
            parameters.AllowVerticalMovement = true;
            parameters.DIAGONAL_MOVEMENT_PENALTY_MULTIPLIER = 1;
            parameters.Formula = AI.HeuristicFormula.Manhattan;
            parameters.HEstimate =  2;
            parameters.PenalizeDiagonalMovement = true;
            parameters.PunishHorizontalChangeInDirection = false;
            parameters.PunishVerticalChangeInDirection = false;
            parameters.SearchLimit = 2000;  
            parameters.TieBreaker = false;

            // TODO: stopwatch to time pathing
            mPathStart = TileLocationFromPoint(start);
            mPathEnd = TileLocationFromPoint(end);
            mPathResults = pathing.Find(this, npc, mConnectivity.Areas, mPathStart, mPathEnd, parameters);

            if (mPathResults.FoundNodes == null)
            {
                System.Diagnostics.Debug.WriteLine("Interior.PathFind() - Could not find path.");
                components = null;
                return null;
            }


            // TODO: we need to create the components[] in the ConstructWaypoints because that function
            // creates more pathpoints than foundNodes.Length potentially.
            mPathPoints = ConstructWaypoints(mPathResults, start, end);
            //if (mPathPoints != null)
            //    System.Diagnostics.Debug.Assert(mPathPoints.Length == mPathResults.FoundNodes.Count);

            if (mPathPoints == null)
                components = null;
            else
            {
                // we need to be able to handle "use" of components such as doors by the current npc.
                // The BehaviorTree needs to use knowledge of these "use"able components 
                // TODO: what if ConstructWaypoints() generates more waypoints than there are "FoundNodes"?
                // like what if at a door we want waypoints generated at the entrance side of the door and the exit side of the door
                // TODO: part of the problem i'm having is that im finding the Areas but not explicitly stating which portals that path goes through.
                //       ACTUALLY we do use AI.ConnectivityInterior.AreaPortal portal = area.Portals[node.PortalIndex] where the node.PortalIndex I think reflects a portal on the current area adjacent to the next area.
                components = new string[mPathPoints.Length];
                for (int i = 0; i < mPathPoints.Length; i++)
                {

                    if (i >= mPathResults.FoundNodes.Count || mPathResults.FoundNodes[i].Entity == null)
                        components[i] = null;
                    else
                        components[i] = mPathResults.FoundNodes[i].Entity.ID;
                }
            }
            return mPathPoints; 
        }

        

        // convert broadphase path results to more refined points that follow the portals, not the Area centers
        // TODO: this is temmporary.  we eventually need to compute better path through portals that get us closer to final goal point and
        // which can set us up to traverse stairs, ladders, doors, etc and not cut corners as what happens with MathHelper.ClosestPointTo() in some cases
        private Vector3d[] ConstructWaypoints(AI.InteriorPathFindResults results, Vector3d start, Vector3d end)
        {
            // TODO: results array should probably be turned into a List so we can remove or insert more refined path points between Areas that were found such as bezier curve points
            Vector3d[] waypoints = new Vector3d[results.FoundNodes.Count + 1]; // we use +1 to append the final "end" Vector3d
            // if length == 1, then start area and end area are the same so we have direct path (no obstacles) to the end location
            if (waypoints.Length == 1)
            {
                waypoints[0] = end;
                return waypoints;
            }

            // if we're traversing multiple floors, find the first node that leads us
            // to the next floor (eg. TILE_ATTRIBUTES_STAIR_LOWER_LANDING or TILE_ATTRIBUTES_STAIR_UPPER_LANDING, ladders, elevators, etc)
            // NOTE: this should work even if to get to say the 3rd floor from the 1st floor, we may have to go up and down and up again to get around closed off areas
            // This means we can't just test start.Y and end.Y because we may start and end up on the 1st floor but have to travel to the 2nd floor to get to that end location on the 1st floor.
            bool hasFloorSwitching = false;
            List<int> floorSwitchIndices = new List<int>();
            for (int i = 0; i < results.FoundNodes.Count; i++)
            {
                int tileValue = results.FoundNodes[i].Area.TileValue;
                if ((tileValue & (int)TILE_ATTRIBUTES.STAIR_LOWER_LANDING) != 0 || (tileValue & (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0 ||
                    (tileValue & (int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING) !=0  || (tileValue & (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING) != 0)
                {
                    hasFloorSwitching = true;
                    // TODO: Wait, what if we traverse a landing but don't actually intend to go up/down that stairs?  we could simply be passing through on the same floor.
                    // The only way to know for sure is to further search and see if we encounter the opposite LANDING tile value... 
                    // If we encounter another tilevalue that is same floor landing, then we can toss out the previously encountered one
                    // What about Doors?  Doors are similar in that they represent a distinct destination within the overall waypoints.
                    floorSwitchIndices.Add(i);
                    // TODO: if we could pair-up landings between floors somehow that would be nice.
                    break;
                }
            }

            bool lowerLandingReached = false; // for when about to be traveling upstairs
            bool upperLandingReached = false; // for when about to be traveling downstairs

            // TODO: if i reach an area that has STAIR_LOWER_LANDING, then the next waypoint
            //       should be computed as center of area + y elevated to the stair collision height after doing a downward raycast.
            //       Then the next waypoint is at the upper landing which is already in correct position
            for (int i = 0; i < results.FoundNodes.Count; i++)
            {
                Vector3d position = Vector3d.Zero();
                AI.InteriorPathFinderNodeFast node = results.FoundNodes[i];
                AI.ConnectivityInterior.Area area = node.Area;

                //if (i == results.FoundNodes.Count - 1)
                //{
                //    // TODO: for a stair landing, we need to maneuver a certain way and not just go directly to the "end"... that is if this random location does wind up at a stair landing
                //    // WARNING: Making the final node == end will result in the NPC cutting corners and walking through walls.  We need to use normal flow of pathing through portals
                //    //          and then appending the "end" vector to the end of the constructed path list.
                //    position = end;
                //}
                //else
                //{
                    AI.ConnectivityInterior.AreaPortal portal = area.Portals[node.PortalIndex];

#if DEBUG
                    // this is the default case, not just "debug"
                    position = GetTileCenter(portal.MinMax[0]); // min
                    position += GetTileCenter(portal.MinMax[1]); // min + max
                    position /= 2d; // center point

                    //position = GetTileCenter(entrance);
                    position.y -= CellSize.y / 2.0d;

#endif
                int tileValue = area.TileValue;
                if ((tileValue & (int)TILE_ATTRIBUTES.ENTRANCE_AND_EXIT) != 0)
                {
                    // TODO: this should be positioned at the "front" or "back" of the Area parallel to the door edge, not in the center.
                    // NOTE: portal.Location[] contains min/max tile positions.  If we convert the min/max tiles to 3d coordinates, we can then find a proper "center" coordinate
                    // BUT we want the location offset towards the nearest part of the entrance_and_exit.  But wait, the PortalIndex does give us the
                    // correct portal on the side we want.  We just need the center in 3d coords right?
                    // Vector3i entrance = portal.Location[0]; // area.Center;

                    // if (!area.Contains(entrance.X, entrance.Z))
                    //     entrance = portal.Location[1];                  

                    //position = GetTileCenter(entrance);
                    position = GetTileCenter(portal.MinMax[0]); // min
                    position += GetTileCenter(portal.MinMax[1]); // min + max
                    position /= 2d; // center point


                    //position = GetTileCenter(entrance);
                    position.y -= CellSize.y / 2.0d;
                }
                else if ((tileValue & (int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING) != 0)
                {
                    position = GetTileCenter(area.Center);
                    position.y -= CellSize.y / 2.0d;
                    lowerLandingReached = true;
                }
                else if ((tileValue & (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING) != 0)
                {
                    position = GetTileCenter(area.Center); // portal.Center);
                    position.y -= CellSize.y / 2.0d; // place Y back at the deck level instead of floating in the middle of the tile
                    upperLandingReached = true;
                }
                else if ((tileValue & (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0)
                {
                    // use center of stair landing and ladder landings which have their own unique Areas defined in the footprint
                    position = GetTileCenter(area.Center); // portal.Center);
                    position.y -= CellSize.y / 2.0d; // place Y back at the deck level instead of floating in the middle of the tile
                    upperLandingReached = true;
                }
                else if ((tileValue & (int)TILE_ATTRIBUTES.STAIR_LOWER_LANDING) != 0)
                {
                    position = GetTileCenter(area.Center);
                    position.y -= CellSize.y / 2.0d;
                    lowerLandingReached = true;
                }
                else if ((tileValue & (int)TILE_ATTRIBUTES.COMPONENT_TRAVERSABLE) != 0)
                {
                    // July.11.2020 - without this, the NPC will start to climb down from the stairs before walking across it to the lower landing (but only when certain rotations of the stairs are applied)
                    position = GetTileCenter(area.Center);
                }
                else
                {
                    //if (i == 0)
                    //    position = start;
                    //else
                    //{
                    if (lowerLandingReached && upperLandingReached) // TODO: this only works if our near immediate destination is a STAIR_UPPER_LANDING.  and likewise in reverse, we need to check for STAIR_UPPER_LANDING reached and then determine if next near destination is a STAIR_LOWER_LANDING and position the intermediate waypoint in Area.Center but with ray cast collisiion for Y altitude
                    {
                        lowerLandingReached = false;
                        upperLandingReached = false;
                    }

                    //Vector3d goal = GetNextGoal(i, results.FoundNodes, end);

                    //// TODO: find a point within the AreaPortal that is closest to the goal

                    //// TODO: first thing, we need a better heruristics calculation in our PathFind()... perhaps based on
                    ////       the "mechanism" of the portals we encounter such as stair landings.  

                    //// TODO: we need a better way to find shortest path when the "end" is on a different floor.
                    ////       we cant use "ClosestPointTo()" in those cases.

                    //// TODO: at some point we either need the stairs ray collision when moving
                    ////       or our steering needs to follow the angle from lower landing to upper landing.
                    ////       I think with our use of tiles, we figured we wouldnt need to do expensive ray casts 
                    ////       for our NPCs while moving, so steering with the angle from lower to upper landing seems better if we can make it look right.
                    ////       Otherwise we special case stairs and only do ray casts for stairs and not general movement case.
                    ////       So, how do we know when we're on the stairs component as opposed to on the landings?
                    //BoundingBox box = this.GetAreaPortalBoundingBox(portal);
                    //// TODO: if the "end" is on a different floor, this will result in our position clamping to the floor above even when the NPC is navigating the floor below 
                    //// TODO: here we should be checking for nearest point within Portal to the "Goal" we are heading towards
                    //// NOTE: ClosestPointTo() will automatically clamp the position.y to the floor so we dont have to subtract halfCellSize.y
                    //position = Utilities.MathHelper.ClosestPointTo(box, goal);


                    //// TODO: verify next goal for doors is along center of door but offset so that it's on the correct side of the door but in front of it by a tile size or two.
                    //Vector3d nextGoal = GetNextGoal(i - 1, results.FoundNodes, end);
                    //nextGoal.y += 0.1d;

                    //position = Utilities.MathHelper.ClosestPointTo(box, nextGoal);

                    //bool useRayCast = true;
                    //if (useRayCast)
                    //{
                    //    Vector3d rayStart = waypoints[i - 1];
                    //    rayStart.y += 0.1d;
                    //    double distance1, distance2;
                    //    double t0 = 0.1d;
                    //    double t1 = double.MaxValue;
                    //    Ray r = new Ray(rayStart, nextGoal - rayStart);
                    //    //r = new Ray(rayStart, rayStart - nextGoal);
                    //    bool result = box.Intersects(r, t0, t1, out distance1, out distance2);
                    //    // if the distance1 and distance2 are > 0, then we can scale the dir vector and add it to the r.Origin and find our impact points in world space
                    //    if (result)
                    //    {
                    //        System.Diagnostics.Debug.WriteLine("Interior.ConstructWaypoints() - Ray intersection found");
                    //        r.Origin.y -= 0.1d;
                    //        position = r.Origin + r.Direction * distance1;
                    //    }
                    //}
                    //}
                    //}
                } //end if

                // when climbing stairs/ladders/etc, the proximity radius in the pathing script can make the NPC stop before its fully above 
                // the upper floor which results in GetFloor(entity.Translation.y) returning the floor below. So instead we raise
                // the pathpoints we generate to be well above the floor.  This means when steering, our proximity radius must be less than
                // this epsilon value.
                double epsilon = 0.03;
                position.y += epsilon;
                waypoints[i] = position;
                
            } // end for

            waypoints[results.FoundNodes.Count] = end;
            return waypoints;
        }

        private Vector3d GetNextGoal(int startIndex, List<AI.InteriorPathFinderNodeFast> foundNodes, Vector3d end)
        {
            Vector3d result = end;

            int count = foundNodes.Count;

            if (startIndex > count - 1) throw new ArgumentOutOfRangeException();
            if (startIndex == count - 1) return result;


            for (int i = startIndex; i < count; i++)
            {
                AI.MOVEMENT_STATE movementState = foundNodes[i].MovementState ;
                int tileValue = foundNodes[i].Area.TileValue;

                if ((tileValue & (int)TILE_ATTRIBUTES.ENTRANCE_AND_EXIT) != 0 || (tileValue & (int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING) != 0 ||
                    (tileValue & (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING) != 0 || (tileValue & (int)TILE_ATTRIBUTES.STAIR_LOWER_LANDING) != 0 || 
                    (tileValue & (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0)
                {

                    result = GetTileCenter(foundNodes[i].Area.Center);
                    result.y -= CellSize.y / 2.0d;
                    break;
                }
            }

            return result;
        }

        public BoundingBox[] GetFoundAreas()
        {
            if (mPathResults.FoundNodes == null) return null;
            List<BoundingBox> boxes = new List<BoundingBox>();
            for (int i = 0; i < mPathResults.FoundNodes.Count; i++)
            {
                Vector3d size = GetTileCenter(mPathResults.FoundNodes[i].Area.Center);
                size.y -= CellSize.y / 2.0d;
                uint y = (uint)mPathResults.FoundNodes[i].Area.Floor;
                Vector3d min = GetTileCenter((uint)mPathResults.FoundNodes[i].Area.MinX, y,(uint)mPathResults.FoundNodes[i].Area.MinZ); // mPathResults.FoundNodes[i].Area.Floor
                min.y -= CellSize.y / 2.0d;
                Vector3d max = GetTileCenter((uint)mPathResults.FoundNodes[i].Area.MaxX, y + 1, (uint)mPathResults.FoundNodes[i].Area.MaxZ); // mPathResults.FoundNodes[i].Area.Floor
                max.y -= CellSize.y / 2.0d;
                BoundingBox box = new BoundingBox(min, max);
                boxes.Add(box);

            }

            return boxes.ToArray();
        }

        public List<int> PathFoundPortalIndices = new List<int>();

        public BoundingBox[] GetFoundPathPortals()
        {
            if (mPathResults.FoundNodes == null) return null;
            List<BoundingBox> results = new List<BoundingBox>();

            int floor = 1;
            if (mConnectivity == null) return null;
            if (mConnectivity.Areas[floor] == null) return null;

            Vector3d halfTileSize = TileSize / 2d;
            for (int i = 0; i < mPathResults.FoundNodes.Count; i++)
            {
                if (mPathResults.FoundNodes[i].Area.Portals == null)
                    continue;

                int index = mPathResults.FoundNodes[i].PortalIndex;
                PathFoundPortalIndices.Add(index);
                if (index == -1) continue;

                AI.ConnectivityInterior.AreaPortal portal = mPathResults.FoundNodes[i].Area.Portals[index];

                Vector3d min, max;

                min.x = (TileStartX + portal.MinMax[0].X) * TileSize.x - halfTileSize.x;
                min.y = GetFloorHeight((uint)floor);
                min.z = (TileStartZ + portal.MinMax[0].Z) * TileSize.z - halfTileSize.z;
                max.x = (TileStartX + portal.MinMax[1].X) * TileSize.x + halfTileSize.x;
                max.y = GetFloorHeight((uint)floor + 1);
                max.z = (TileStartZ + portal.MinMax[1].Z) * TileSize.z + halfTileSize.z;
                BoundingBox box = new BoundingBox(min, max);

                results.Add(box);
                
            }

            return results.ToArray();
        }

        public Vector3d[] GetFoundPathPoints()
        {
            return mPathPoints;
        }

        /// <summary>
        /// TODO: I think this is only used for debug in FloorplanHud rendering.
        /// Returns fractional representation of Tile location and not cartesian coordinates.
        /// // April.22.2019. huh? what does fractional representation mean?
        /// </summary>
        /// <returns></returns>
        //public Vector3i[] GetFoundPathPoints()
        //{
        //    if (!mPathResults.Found) return null;

        //    Vector3i[] results = new Vector3i[mPathResults.FoundNodes.Count];
        //    for (int i = 0; i < results.Length; i++)
        //    {
        //        if (i == 0)
        //            results[i] = mPathStart;
        //        else if (i == results.Length - 1)
        //            results[i] = mPathEnd;
        //        else
        //        {
        //            AI.InteriorPathFinderNodeFast node = mPathResults.FoundNodes[i];
        //            Vector3i c = node.Area.Portals[node.PortalIndex].Center;
        //            results[i].X = (int)c.X;
        //           // System.Diagnostics.Debug.Assert(node.Floor == 2); // TODO: we're limiting testing to just floor index == 2 for now
        //            results[i].Y = node.Floor; // c.Y - CellSize.y / 2.0d;
        //            results[i].Z = (int)c.Z;
        //        }
        //    }

        //    return results;
        //}

        internal AI.ConnectivityInterior.Area[][] Areas { get { return mConnectivity.Areas; } }

        public Vector3i mPathStart, mPathEnd;
        AI.InteriorPathFindResults mPathResults;
        

        internal AI.ConnectivityInterior.Area LocateAreaContainingTile(int x, int y, int z)
        {
            AI.ConnectivityInterior.Area result;
            result.Index = -1;
            result.Floor = -1;
            result.MaxX = -1;
            result.MaxZ = -1;
            result.MinX = -1;
            result.MinZ = -1;
            result.Portals = null;
            result.TileValue = -1;

            if (mConnectivity == null || mConnectivity.Areas == null) return result;
            if (mConnectivity.Areas.Length < y) return result;

            int index = mConnectivity.LocateAreaContainingTile(x, y, z);
            if (index == -1)
                return result;

            return mConnectivity.Areas[y][index];

        }

        /// <summary>
        /// Returns portal bounding box in region space coordinates and NOT cameraspace
        /// </summary>
        /// <param name="portal"></param>
        /// <returns></returns>
        internal BoundingBox GetAreaPortalBoundingBox(AI.ConnectivityInterior.AreaPortal portal)
        {
            Vector3d halfTileSize = TileSize / 2d;
            Vector3d min, max;

            min.x = (TileStartX + portal.MinMax[0].X) * TileSize.x - halfTileSize.x;
            min.y = GetFloorHeight((uint)portal.MinMax[0].Y);
            min.z = (TileStartZ + portal.MinMax[0].Z) * TileSize.z - halfTileSize.z;
            max.x = (TileStartX + portal.MinMax[1].X) * TileSize.x + halfTileSize.x;
            max.y = GetFloorHeight((uint)portal.MinMax[0].Y + 1);
            max.z = (TileStartZ + portal.MinMax[1].Z) * TileSize.z + halfTileSize.z;

            return new BoundingBox(min, max);
        }

        public BoundingBox[] GetConnectivityPortals(int floor)
        {
            List<BoundingBox> results = new List<BoundingBox>();

            if (mConnectivity == null) return null;
            if (mConnectivity.Areas[floor] == null) return null;

            Vector3d halfTileSize = TileSize / 2d;
            for (int i = 0; i < mConnectivity.Areas[floor].Length; i++)
            {
                if (mConnectivity.Areas[floor][i].Portals == null)
                    continue;

                for (int j = 0; j < mConnectivity.Areas[floor][i].Portals.Length; j++)
                {
                    Vector3d min, max;
                    //System.Diagnostics.Debug.Assert(mConnectivity.Areas[floor][i].Portals[j].Location[0].X != mConnectivity.Areas[floor][i].Portals[j].Location[1].X &&
                    //                                mConnectivity.Areas[floor][i].Portals[j].Location[0].Z != mConnectivity.Areas[floor][i].Portals[j].Location[1].Z);
                    min.x = (TileStartX + mConnectivity.Areas[floor][i].Portals[j].MinMax[0].X) * TileSize.x - halfTileSize.x;
                    min.y = GetFloorHeight((uint)floor);
                    min.z = (TileStartZ + mConnectivity.Areas[floor][i].Portals[j].MinMax[0].Z) * TileSize.z - halfTileSize.z;
                    max.x = (TileStartX + mConnectivity.Areas[floor][i].Portals[j].MinMax[1].X) * TileSize.x + halfTileSize.x;
                    max.y = GetFloorHeight((uint)floor + 1);
                    max.z = (TileStartZ + mConnectivity.Areas[floor][i].Portals[j].MinMax[1].Z) * TileSize.z + halfTileSize.z;
                    BoundingBox box = new BoundingBox(min, max);

                    results.Add(box);
                }
            }

            return results.ToArray();
        }

        public BoundingBox[] GetConnectivityAreas(int floor)
        {

            if (mConnectivity == null) return null;
            if (mConnectivity.Areas[floor] == null) return null; // the areas for this floor have not been computed yet

            // halfTileSize is used because TileStartX and TileStartZ give us the center positions of the first Tile.
            Vector3d halfTileSize = TileSize / 2d;
            List<BoundingBox> results = new List<BoundingBox>(mConnectivity.Areas[floor].Length);
            for (int i = 0; i < mConnectivity.Areas[floor].Length; i++)
            {
                Vector3d min = GetTileCenter((uint)mConnectivity.Areas[floor][i].MinX, (uint)floor, (uint)mConnectivity.Areas[floor][i].MinZ);
                min -= halfTileSize;
                Vector3d max = GetTileCenter((uint)mConnectivity.Areas[floor][i].MaxX, (uint)floor, (uint)mConnectivity.Areas[floor][i].MaxZ);
                max += halfTileSize;
                BoundingBox box = new BoundingBox(min, max);

                results.Add(box);
            }
            return results.ToArray();
        }

        public int[,] GetCollisionFootprint(int[,] sourceFootprint, Vector3d position, Quaternion rotation, out Vector3i startTileLocation)
        {
            startTileLocation.X = startTileLocation.Y = startTileLocation.Z = -1; 
            if (sourceFootprint == null)
            {
                System.Diagnostics.Debug.WriteLine("GetCollisionFootprint() - COMPONENT FOOTPRINT NOT SET");
                return null;
            }

            byte rotationIndex = rotation.GetComponentYRotationIndex();
            
            // NOTE: i use the sourceFootprint and not the rotatedFootprint in this call.  I wonder if i should change this to pass in the rotatedFootprint with no rotationIndex 
            Vector3d snapPosition = GetTileSnapPosition(sourceFootprint, position, rotationIndex, out startTileLocation);


            int[,] rotatedFootprint = GetRotatedFootprint(sourceFootprint, rotation);
            int[,] collisionFootprint = new int[rotatedFootprint.GetLength(0), rotatedFootprint.GetLength(1)];
            // todo: rotatedFootprint must fit entirely within the destination tilemap
            int[,] destFootprint = mCellMap.GetFootprint((uint)startTileLocation.X, (uint)startTileLocation.Y, (uint)startTileLocation.Z, (uint)rotatedFootprint.GetLength(0), (uint)rotatedFootprint.GetLength(1));

            // TODO: compare the bits and return the collisionFootprint
            for (int i = 0; i < rotatedFootprint.GetLength (0); i++)
                for (int j = 0; j < rotatedFootprint.GetLength (1); j++)
                {
                    bool hasWallOrComponent = (rotatedFootprint[i, j] & (int)TILE_ATTRIBUTES.ANY) != 0 &&
                                                (((destFootprint[i, j] & (int)TILE_ATTRIBUTES.WALL) != 0 &&
                                                (rotatedFootprint[i, j] & (int)TILE_ATTRIBUTES.ENTRANCE_AND_EXIT) == 0 ||
                                                 (destFootprint[i, j] & (int)TILE_ATTRIBUTES.ANY) != 0));

                    if (hasWallOrComponent )
                        collisionFootprint[i, j] = 1;

                }


            return collisionFootprint;
        }

        /// <summary>
        /// Checks if the wall and corner are allowed to be placed (whether they are in bounds and not conflicting with existing footprints)
        /// </summary>
        /// <param name="style"></param>
        /// <param name="edgeID"></param>
        /// <returns></returns>
        public bool IsSegmentPlaceable(SegmentStyle style, uint edgeID)
        {
            // TODO: This function needs to be implemented.

            // if the wall portion of the segment (excluding corners) is placeable, we can place the wall then
            // determine which corner(s) styles are placeable
            //style.EdgeStyle;
            //style.CornerStyle;
            // if existing segments, but no other type of obstacle, we signal True.

            // do we need to compute the footprints here?  segment indicates walls and corners
            return true;
        }

        // eg. walls, railings, segments are always tested on cell boundaries, whereas components can straddle cell boundaries
        // as long as their are no obstacles.
        public bool IsSegmentPlaceable(EdgeStyle style, uint edgeID)
        {
            Keystone.CSG.CellEdge edge = Keystone.CSG.CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            byte leftRotation, rightRotation;
            edge.GetByteRotation(out leftRotation, out rightRotation);

            // is the edge in bounds?
            bool bounds = IsEdgeInBounds(edgeID);
            if (!bounds) return false;

            // TODO: when deleting an edge wall, footprint test collision occurs
            // and results in IsSegmentPlaceable returning false when we're not even interested in placing
            // but removing.  i need to handle deletion differently
            // todo: is BottomLeftFootprint and TopRightFootprint are null, it always returns true on IsSegmentPlaceable()

            MinimeshMap map = new MinimeshMap ();
            map.Edge = edge;
            map.EdgeLocation = GetEdgeLocation(edge.BottomLeftCell, edge.TopRightCell);
            map.Style = style;
            // TODO: Not every EdgeStyle has a prefab that contains a ModelSequence node.  Some just have a single Model (eg. railings)
            string modelSequenceID = GetModelSequenceID(style.Prefab, edge.Layer); // style.BottomLeftTexturePath, edge.Layer);
            map.ModelSelector = (ModelSelector)Keystone.Resource.Repository.Get(modelSequenceID);
            map.Model = (Model)map.ModelSelector.Children[0];
            //map.Model = (Model)Keystone.Resource.Repository.Get(modelSequenceID);`
            //map.Model = null;
            map.Minimesh = null;
            map.ElementIndex = -1;

            float rotation;
            bool onlyParallelsExist;
            bool adjacentsExist;
            int subModelIndex = SelectSubModelIndex(edgeID, out rotation, out adjacentsExist, out onlyParallelsExist);
            map.SubModelIndex = subModelIndex;

            if (map.ModelSelector != null && map.ModelSelector.Children.Length - 1 >= subModelIndex)
                map.Model = (Model)map.ModelSelector.Children[subModelIndex];
            else
            {
                map.Model = (Model)map.ModelSelector.Children[0]; // default model. Railings00.kgbentity for instance only has one model
                map.SubModelIndex = 0;
            }

            map.Minimesh = (MinimeshGeometry)map.Model.Geometry;
            // TODO: for now, let's just always return the very first wall model and get it added, and get it positioned properly 
            //       so that we can model in Blender the other wall sub-models for our prefab.
            //                   MinimeshMap currentMap = AutoTileEdgeSegments(edge, true);

            // construct a temporary WallModelView within the minimesh geometry so that we can generate a footprint for testing validity
            map.ElementIndex = AddWallModelView(map, edge.ID, rotation, map.EdgeLocation);
            // TODO: we need to remove this RemoveWallModelView() since this is just for validation and not placement.


            Vector3i startLocation;
            // NOTE: footprint returned is already rotated
            int[,] footprint = GetEdgeFootprint(edge, map, out startLocation);

            // TODO: since walls are not double sided, we only use leftRotation now. I think. need to verify
            bool inbounds = IsEdgeFootPrintInBounds(footprint, leftRotation, startLocation );
            // TODO: we can have two sets of railings.  Railings around stairs and ladder holes, and rails along stretches of open floor.  We can use similar "smart" placement of the models based on how long the stretches are so that the railings all connect.
            bool isValid = inbounds; // ValidatePlacement(startLocation.X, startLocation.Y, startLocation.Z, footprint);
            if (!isValid) System.Diagnostics.Debug.WriteLine("Interior.IsSegmentPlaceable() - segment crosses an OBSTACLE");

            // delete the temporary WallModelView element
            RemoveWallModelView(map.Minimesh, (uint)map.ElementIndex);

            return inbounds && isValid;


            bool leftPlaceable = IsSegmentPlaceable(style.BottomLeftFootprint, edge.BottomLeftCell, leftRotation);
            if (leftPlaceable == false)
                System.Diagnostics.Debug.WriteLine("Interior.IsSegmentPlaceable() -segment not placeable");

            // TODO: we don't use both TopRightFootprint anymore because walls are not
            //       double sided and because we generate footprint for walls on demand.
            bool rightPlaceable = IsSegmentPlaceable(style.TopRightFootprint, edge.TopRightCell, rightRotation);
            if (rightPlaceable == false)
                System.Diagnostics.Debug.WriteLine("Interior.IsSegmentPlaceable() -segment not placeable");

            // TODO: here we should trigger updtaes to data and visuals and run architectural logic
            // TODO: be nicer i think if some of this can be added to the script
            //       so that it's available to both deserialization and painting without
            //       duplication
            // TODO: architectural rules go where?
            // TODO: recall that architectural rules should also get called during deserialization to prevent
            //       hacked ships from loading over the wire
            // note: for now if either wall half is not placeable, we will cancel
            if (leftPlaceable == false && rightPlaceable == false)
            {
                System.Diagnostics.Debug.WriteLine("Interior.IsSegmentPlaceable() - Segment Not Placeable.");
                return false;
            }

            return true;

        }

        private bool IsEdgeFootPrintInBounds(int[,] sourceFootprint, byte rotation, Vector3i startLocation)
        {
            int sourceWidth = sourceFootprint.GetLength(0);
            int sourceHeight = sourceFootprint.GetLength(1);

            // NOTE: sourceFootprint is already rotated!  This function should not exist.  We just need ValidatePlacement() with sourceFootprint
            int[,] rotatedFootprint = sourceFootprint; // GetRotatedFootprint(sourceFootprint, rotation);

            int[,] destFootprint = null;
            bool valid = ValidatePlacement(startLocation.X, startLocation.Y, startLocation.Z, rotatedFootprint, out destFootprint);


            return valid;
        }

        private bool IsSegmentPlaceable(int[,] sourceFootprint, int cellIndex, byte rotation)
        {
            // TODO: segments are for walls and railings and any "wall" type structure that sits on an Edge.
            //       Those should all have footprints and not be null, yet below here we return 'true' because
            //       there is no footprint and we havent generated one yet.  But there is a GetEdgeFootprint(edge, out startLocation) that
            //       we can use here to determine if we can place the footprint.
            if (sourceFootprint == null) return true;

            int[,] rotatedFootprint = GetRotatedFootprint(sourceFootprint, rotation);

            int sourceWidth = rotatedFootprint.GetLength(0);
            int sourceHeight = rotatedFootprint.GetLength(1);

            if (sourceWidth == 0 || sourceHeight == 0) return false;
            if (cellIndex < 0) return false; // cell is out of bounds.  this is why the arg cellIndex should remain an int or a cast to uint will flip it

            uint x, y, z;
            UnflattenCellIndex((uint)cellIndex, out x, out y, out z);
            // from the cellIndex we can compute the offsetX and offsetZ of the bottom/left most tile in this cell
            x *= TilesPerCellX;
            z *= TilesPerCellZ;

            int[,] destFootprint;
            bool valid = ValidatePlacement((int)x, (int)y, (int)z, rotatedFootprint, out destFootprint);
            if (valid == false)
                System.Diagnostics.Debug.WriteLine("CelledRegion.IsSegmentPlaceable() - not placeable");

            return valid;

            // return false == FootprintCollides((int)x, (int)y, (int)z, rotatedFootprint);
        }

        /// <summary>
        /// Determine if an edge is within the space of the overall grid.  It does not test whether the edge is IN_BOUNDS.
        /// NOTE: we prevent Edge segments from being placed on first or last row or first or last column since Segments
        /// often need to overlap with adjacent edges, and that would fall OOB where there is no footprint data, so we return false for those cases as well.
        /// </summary>
        /// <param name="edgeID">edgeID is a unique ID across all decks/floors</param>
        /// <returns></returns>
        public bool IsEdgeInBounds(uint edgeID)
        {
            CellEdge e = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            CellEdge.EdgeStatistics stats = new CellEdge.EdgeStatistics(CellCountX, CellCountY, CellCountZ);

            CellEdge[] adjacents = GetAdjacentEdges(edgeID);

            if (e.Orientation == CellEdge.EdgeOrientation.Horizontal)
            {
                if (e.Row == 0 || e.Row == stats.HorizontalEdgeRows - 1)
                    return false;

                if (adjacents[2].Column == 0 || adjacents[5].Column == stats.VerticalEdgeColumns - 1)
                    return false;


            }
            else if (e.Orientation == CellEdge.EdgeOrientation.Vertical)
            {
                if (e.Column == 0 || e.Column == stats.VerticalEdgeColumns - 1)
                    return false;

                if (adjacents[2].Row == 0 || adjacents[5].Row == stats.HorizontalEdgeRows - 1)
                    return false;

            }
            else throw new NotImplementedException("Diagonal orientation edges not yet supported.");

            return true;
        }

        // FootprintCollides() is only called here from this function and it seems to be wrong.
        // I should be validating as normal component?  Or a special validate for things like doors...
        // but FootprintCollides() it seems does a test for ANY tilemask match including INBOUNDS so it always
        // returns false.  I think this used to work before with the old doors that used CSG because
        // they had no footprint at all or a blank one.
        // I think perhaps in our command worker, we need to delete any wall that already exists
        // and replace it with the door or another wall that has a window.
        // eg. doors - an edgeID is provided and tileLocation is out parameter
        public bool IsEdgeComponentPlaceable(int[,] sourceFootprint, uint edgeID, out Vector3d position, out Vector3i tileLocation)
        {
            // edge segments and tiles do not use scripts to compute positions because segments
            // are not entities.  They are apart of the celledregion's structure. Positions and rotations
            // are only useful for their model's transform

            tileLocation = GetTileLocationFromEdgeID(edgeID, out position);
            return FootprintCollides(tileLocation.X, tileLocation.Y, tileLocation.Z, sourceFootprint);
        }


        public bool IsChildPlaceableWithinInterior(ModeledEntity entity, Vector3d position, Quaternion rotation, out int[,] destFootprint)
        {
            destFootprint = null;
            if (entity.Footprint == null)
            {
                System.Diagnostics.Debug.WriteLine("Interior.IsChildPlaceableWithinInterior() - No footprint found!");
                return true;
            }
            return IsChildPlaceableWithinInterior(entity.Footprint.Data, position, rotation, out destFootprint);
        }

        /// <summary>
        /// This is basically a collision test, but we don't provide any collision result information.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <remarks>
        /// This should return broken rule descriptions as out parameter?
        /// </remarks>
        /// <returns></returns>
        public bool IsChildPlaceableWithinInterior(int[,] sourceFootprint, Vector3d position, Quaternion rotation,out int[,] destFootprint)
        {
            destFootprint = null;
            byte rotationIndex = rotation.GetComponentYRotationIndex();
            Vector3i tileLocation;
            Vector3d snapPosition = GetTileSnapPosition(sourceFootprint, position, rotationIndex, out tileLocation);

            // no footprint means it CANNOT be placed in INTERIORS
            if (sourceFootprint == null)
            {
                System.Diagnostics.Debug.WriteLine("IsChildPlaceableWithinInterior() - NO FOOTPRINT FOUND IN COMPONENT");
                return false;
            }

            int[,] rotatedFootprint = GetRotatedFootprint(sourceFootprint, rotation);
            bool valid = ValidatePlacement(tileLocation.X, tileLocation.Y, tileLocation.Z, rotatedFootprint, out destFootprint);
            //if (valid == false)
            //    System.Diagnostics.Debug.WriteLine("Interior.IsChildPlaceableWithinInterior() - component not placeable");
            //else
            //    System.Diagnostics.Debug.WriteLine("component IS placeable");

            return valid;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">lowest x value representing start tile location for the footprint within the destination tilemap</param>
        /// <param name="y">lowest y value for lowest floor of this component</param>
        /// <param name="z">lowest z value representing start tile location for the footprint within the destination tilemap</param>
        /// <param name="componentFootprint">The component footprint must already be rotated if applicable</param>
        /// <returns></returns>
        private bool ValidatePlacement(int x, int y, int z, int[,] componentFootprint, out int[,] destFootprint)
        {

            // TODO: not in tool, but in HUD do we validate.  Tool only computes location from "QueryCellPlacement"
            //      1) clearance rules (in 2d it's footprint, but what about 3d?) 
            //          - a height property added to the footprint? and which shows up as a bounding box marker
            //          and which comes in 1.25meter increments?
            //          - this allows this component to register with the tiles in the space above it 
            //          - what about SCALING of the component?  Or do we manually provide a big SelectorNode of
            //            sizes user can scale from where the footprints and height properties for each cell in the footprint
            //            is specified by hand in footprint editor?  This is the easiest way but a bit of maintenance reqt.

            //      2) access space (in 2d it's footprint again, but what about 3d? we allow access
            //                       from upstairs catwalks too and does this count?
            //      3) foundational rules - eg must be set ontop of floor or a wall 
            //              - if CelledRegion.TILEMASK_FLAGS 
            //      4) support rules - if this is removed, it cause change in structural support and can cascade
            //              - this item can add a "supported" flag to tiles above it whether or not those tiles have anything 
            //             added to them yet or not.
            //              - support radius in tiles for above tiles ( making support square radius would be simplest) 
            //                  - 
            //              - the question is how to get these flags removed when it's not necessarily clear which component is
            //              setting those flags? if two columns are setting support flags to some of the same tiles above
            //              and i remove one column, how do i know which tiles i can safely de-flag and which not?
            //                  - best solution is to find all elements beneath that tile and recurse outwards and down
            //                  and test footprints for items that are still supporting before determine if a flag should be
            //                  set or not.  
            //                      - for performance, first test should find all items in range of all tiles in question
            //                      then we can individually test against this set of components each tile if need be
            //                  - actually other solution is to NOT ALLOW OVERLAP of support flags ever.  if you try to place
            //                    a supporting column where another overlaps it will not be allowed. This is good for preventing
            //                    unrealistic buffing\spamming of supports too.
            //                  - so perhaps we can have several flags for the degree of support? low, medium, high? and they can be
            //                    combined such that low + medium = high, low + high = very high, medium + high = max
            //                    - but this is limiting somewhat
            //                      - would be nice if each tile we could have a type of fragment program tied to it

            //    // RAMPS / STAIRCASES
            //    //  - a 1 level ramp connects one level to the level above it
            //    //    - it cannot be placed unless the level above it is floored
            //    //      with an opening directly above and floor on the adjacent side to
            //    //      the top of the ramp
            //    //  - a 2 level ramp 
            //    // 
            //    // LADDERS
            //    //
            //    // LIFTS

            destFootprint = null;
            if (componentFootprint == null) return false;

            // NOTE: componentFootprint is already rotated before being passed into this function
            //componentFootprint = GetRotatedFootprint(componentFootprint, 64); // if wall is vertical, rotate by 64


            int width = componentFootprint.GetLength(0);
            int height = componentFootprint.GetLength(1);

            if (width == 0 || height == 0) return false;

            // footpring with and height must be even divisible by 2 (todo: i dont think the evenly divisble by  2 is true anymore)
            // warning: wait this is not (yet) necessarily so when we are rotating!
            //if (sourceWidth % 2 != 0 && sourceHeight % 2 != 0) throw new ArgumentOutOfRangeException();

            if (mCellMap.TilesAreInBounds(x, y, z, width, height) == false) return false;

            // TODO: what if width and height places some of the returned results out of bounds?
            // TODO: does this take into account the rotated component footprint passed in and we're just using dstination unroated positions?
            destFootprint = mCellMap.GetFootprint((uint)x, (uint)y, (uint)z, (uint)width, (uint)height);



            // TODO: I should be able to combine the for loops into just one set of nested for loops
            // if all tiles are BOUNDS_IN == 0, then return false.
            // Otherwise this is probably an edge segment and as long as their is no obstacle (eg: an airlock door to the exterior of the vehicle?), we should allow it
            bool isWall = IsWall(componentFootprint);
            if (isWall)
            {
                // if this is a wall, we need to first determine which edge it's on.
                CellEdge edge = GetEdgeFromFootprint(destFootprint, x, y, z);

                if (edge.BottomLeftCell == -1 && edge.TopRightCell == -1) return false;

                if (!IsCellBOUNDS_IN((uint)edge.BottomLeftCell) && !IsCellBOUNDS_IN((uint)edge.TopRightCell))
                    return false;


                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                    {
                        // dont allow a wall to be placed over a Component
                        // check just component flag not obstacle since COMPONENT enum has both
                        if ((destFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.ANY) != 0) 
                            return false;
                    }

            }
            else
            {
                // get all areas in the source footprint where we need support and build a mask to test against the dest

                // 1) test if our destination is entirely in-bounds
                // NOTE: but we only care if the dest footprint is in bounds where the source componnet footprint has OBSTACLES.  So if
                //       the part of the source component that is out of bounds has no COMPONENT flag we should allow the component to be placed
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                        if ((destFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.BOUNDS_IN) == 0) // use '==' NOT '!='
                            return false;
            }

            if (isWall)
            {
                for (int i = 0; i < width; i++)
                    for (int j = 0; j < height; j++)
                    {
                        // special case for Wall footprints which can overlap with other walls. 
                        // TODO: if we place another wall over an existing wall, do we delete the existing wall first? verify.
                        if ((componentFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.NONE) == 0 || ((destFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.WALL) != 0 && (componentFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.WALL) != 0))
                        {
                            continue;
                        }
                        // TODO: GetBits() does not appear to work. not sure why yet.
                        //int collissionBitsInSrc = GetBits(componentFootprint[i, j], 0, 15);
                        //if ((collissionBitsInSrc & destFootprint[i, j]) != 0)  // use '!=' NOT '=='
                        //    return false; 
                        bool hasComponent = (destFootprint[i, j] & (int)TILE_ATTRIBUTES.ANY) != 0; // NOTE: WALL and COMPONENT enum no longer has OBSTACLE flag combined with it
                        if (hasComponent)
                            return false;
                    }

                return true;
            }

            // 2) test for obstacle collission, intra floor penetrating obstacles should work too
            // we have to just get bits for TILE_ATTRIBUTES.COMPONENT (ANY) and WALL to make sure there are no obstructions above
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    if ((componentFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.COMPONENT_TRAVERSABLE) != 0 ||
                         (componentFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0)
                        continue;

                    bool hasWallOrComponent = (componentFootprint[i, j] & (int)TILE_ATTRIBUTES.ANY) != 0 &&
                                                (((destFootprint[i, j] & (int)TILE_ATTRIBUTES.WALL) != 0 &&
                                                (componentFootprint[i,j] & (int)TILE_ATTRIBUTES.ENTRANCE_AND_EXIT) == 0 ||
                                                 (destFootprint[i, j] & (int)TILE_ATTRIBUTES.ANY) != 0)); 
                    if (hasWallOrComponent)
                        return false;
                }

            // 3) get bit at current tile location that will indicate if this component requires support
            //    and then test destination if that support is offered.  eg. a chair might need supported surface
            //    to be placed upon.  A light might need wall or ceiling as support.  However, specifying a need for
            //    support in the footprint should not be confused with producing support and modifying any dest footprint data.
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    // TODO: shouldn't wall test for the floor beneath it if there is a presence of a wall on the tile below?
                    //       We would need to get footprint data of tile beneath this one to test and compare.
                    // if component we wish to place requires WALL OR FLOOR tile at current tile location 
                    bool srcRequiresSupport = (componentFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.FLOOR) != 0;
                    if (srcRequiresSupport)
                        // then test the destination if it is a WALL or FLOOR at that tile location
                        if ((destFootprint[i, j] & (int)Interior.TILE_ATTRIBUTES.FLOOR
                            & (int)Interior.TILE_ATTRIBUTES.WALL) == 0) // use '==' NOT '!='
                            return false;
                }

            // 4) if wall mount, requires wall support (obsolete - no component can be wall mounted in version 1.0)


            // 5) overhead clearance test (TODO: ancillary test is that we can't place floor where a tall item from lower floor
            //    is protruding through the cell we are trying to add a floor.  I think this test is obsolete.  Reactors and Engines and such
            //    can be stacked and auto-tiled to produce good shaped components.


            // - we need to autogen the exterior walls, but how?
            //      - does it occur only after final inbounds painting is done?  what if you want to change bounds?
            //      - todo: we dont need to autgoen exterior walls, we just need to be able to make them undeleatable when user customizes interior of their ship
            

            // still here? must be valid
            bool isvalid = true; // ((bool)this.DomainObject.Execute("TilePlacement_Validate", new object[] { this.ID, component.TileLocation, component.CellRotation }));
            // TODO: should we return rules / messages for why validation failed if applicable?
            // TODO: what do we do on removal of a component or segment that now causes those components to
            //       no longer pass their architectural rules?

            return isvalid;

        }


        private void ApplyArchitecturalRules()
        {
        }


        private bool IsWall(int[,] soureFootprint)
        {

            if (soureFootprint == null) return false;
            int width = soureFootprint.GetLength(0);
            int height = soureFootprint.GetLength(1);

            if (width == 0 || height == 0) return false;


            //// TEMP: hack, remove the wall flag that seems to be forcing itself onto our footprints. What the heck?
            //for (int i = 0; i < width; i++)
            //    for (int j = 0; j < height; j++)
            //    {
            //        if ((soureFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.WALL) != 0)
            //            return true;
            //    }

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                {
                    if ((soureFootprint[i, j] & (int)TILE_ATTRIBUTES.WALL) != 0)
                        return true;
                }


            return false;
        }
#if DEBUG
        public Vector3i WallPlacementStartTileLocation;
        public Vector3i WallPlacementEndTileLocation;
#endif
        /// <summary>
        /// Finds the Edge associated with a Wall footprint.  The footprint is already rotated when passed in.
        /// </summary>
        /// <param name="footprint"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private CellEdge GetEdgeFromFootprint(int[,] footprint, int x, int y, int z)
        {
            int width = footprint.GetLength(0);
            int height = footprint.GetLength(1);


            Vector3i startLocation;
            startLocation.X = x;
            startLocation.Y = y;
            startLocation.Z = z;

#if DEBUG
            WallPlacementStartTileLocation = startLocation;
#endif
            Vector3i endLocation = startLocation;
            if (width > height)
            {
                endLocation.X += width - 1;
            }
            else
            {
                endLocation.Z += height - 1;
            }
#if DEBUG
            WallPlacementEndTileLocation = endLocation;
#endif 

            Vector3d start = GetTileCenter(startLocation);
            PickParameters parameters = new PickParameters();
            parameters.Accuracy = PickAccuracy.Tile;
            parameters.FloorLevel = y;

            start.y += 0.1;
            Vector3d end = start;
            end.y -= 0.2;
            PickResults results = Collide(start, end, parameters);
            int vertexOrigin = results.VertexID;

            start = GetTileCenter(endLocation);
            start.y += 0.1;
            end = start;
            end.y -= 0.2;
            results = Collide(start, end, parameters);
            int vertexDest = results.VertexID;

            CellEdge edge;

            if (vertexOrigin > -1 && vertexDest > -1)
                edge = CellEdge.CreateEdge((uint)vertexOrigin, (uint)vertexDest, CellCountX, CellCountY, CellCountZ);
            else
            {
                edge.BottomLeftCell = -1;
                edge.Column = 0;
                edge.Row = 0;
                edge.TopRightCell = -1;
                edge.Orientation = CellEdge.EdgeOrientation.Horizontal;
                edge.Origin = 0;
                edge.Dest = 0;
                edge.ID = 0;
                edge.Layer = 0;
                
            }

            return edge;
        }

        private bool CellContainsFlag(uint x, uint y, uint z, int flag)
        {
            // convert cell indices to tile indices to get the footprint
            x *= TilesPerCellX;
            z *= TilesPerCellZ;

            int[,] footprint = mCellMap.GetFootprint(x, y, z, 1, 1);

            if ((footprint[0, 0] & (int)flag) != 0)
                return true;

            return false;
        }
        private bool IsCellBOUNDS_PAINTED_IN(uint x, uint y, uint z)
        {
            // convert cell indices to tile indices to get the footprint
            x *= TilesPerCellX;
            z *= TilesPerCellZ;

            int[,] footprint = mCellMap.GetFootprint(x, y, z, 1, 1);

            if ((footprint[0, 0] & (int)TILE_ATTRIBUTES.BOUNDS_IN) != 0)
                return true;

            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellID">all cellIDs are unique across all decks</param>
        /// <returns></returns>
        private bool IsCellBOUNDS_IN(uint cellID)
        {
            uint x, y, z;
            UnflattenCellIndex(cellID, out x, out y, out z);

            return IsCellBOUNDS_PAINTED_IN(x, y, z);
        }

        /// <summary>
        /// Components and Segments all leave footprints on the data layer for that floor
        /// which can be used for obstacle avoidance during path finding as well as 
        /// component placement.
        /// NOTE: For Components, this method is called from OcupyTiles()
        /// For wall edge segments, its called from EntityAPI.ApplyEdgeSegmentFootprint() and EntityAPI.ApplyTileSegmentFootprint()
        /// </summary>
        /// <param name="cellIndex"></param>
        /// <param name="sourceFootprint"></param>
        /// <remarks>
        /// footprint passed in must already be rotated
        /// </remarks>
        /// <returns>TRUE on success.</returns>
        public bool ApplyFootprint(int cellIndex, int[,] sourceFootprint)
        {
            if (cellIndex < 0) return false;
            uint x, y, z;
            UnflattenCellIndex((uint)cellIndex, out x, out y, out z);

            // convert x and z from Cell Coordinates to Tile Coordinates.
            // NOTE: This results in x,z coordinate of the bottom / left most tile coordinate
            // of the Cell
            x *= TilesPerCellX;
            z *= TilesPerCellZ;

            return ApplyFootprint(x, y, z, sourceFootprint);
        }


        /// <remarks>
        /// footprint passed in must already be rotated
        /// NOTE: For Components, this method is called from OcupyTiles()
        /// For wall edge segments, its called from EntityAPI.ApplyEdgeSegmentFootprint() and EntityAPI.ApplyTileSegmentFootprint()
        /// </remarks>
        /// <param name="x">Starting x coordinate in Tiles not Cells</param>
        /// <param name="y">y coordinate in Tiles not Cells</param>
        /// <param name="Z">Starting z coordinate in Tiles not Cells</param>
        public bool ApplyFootprint(uint x, uint y, uint z, int[,] sourceFootprint)
        {
            
            if (sourceFootprint == null) return false;
            int sourceWidth = sourceFootprint.GetLength(0);
            int sourceHeight = sourceFootprint.GetLength(1);

            if (sourceWidth == 0 || sourceHeight == 0) return false;

            bool hasUpperLevel = y < CellCountY - 1;

            int[,] destLowerFootprint = mCellMap.GetFootprint(x, y, z, (uint)sourceWidth, (uint)sourceHeight);
            int[,] destUpperFootprint = null;

            if (hasUpperLevel)
            {
                Vector3i cell = CellLocationFromTileLocation(x, y + 1, z);
                if (cell.X == -1 || cell.Z == -1)
                    hasUpperLevel = false;
                else
                {
                    hasUpperLevel = IsCellInBounds((uint)cell.X, y + 1, (uint)cell.Z);
                    if (hasUpperLevel)
                        destUpperFootprint = mCellMap.GetFootprint(x, y + 1, z, (uint)sourceWidth, (uint)sourceHeight);
                }
            }

            bool hasDestUpper = false;
            // logical OR the sourcefootprint to the destination
            for (int i = 0; i < sourceWidth; i++)
                for (int j = 0; j < sourceHeight; j++)
                {  
#if DEBUG
                    var before = destLowerFootprint[i, j];
#endif
                    // now we test for special structural Attributes such as Y axis traversability (i.e up down traversability)
                    // and we add flags to the upper level if that is the case
                    if (hasUpperLevel && (sourceFootprint[i, j] & (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING) != 0)
                    {
                        hasDestUpper = true;

                        // collapse the ceiling and upper floor cell
                        // we only do it one time though, not for every iteration of LADDER_UPPER_LANDING
                        //if (!ceilingCollapsed)
                        //{
                        //    // TODO: If already at top most deck, we should not be allowed to place Ladders or Stairs.
                        //    // Throw an assert if that is the case.
                        //    // NOTE: we collapse the ceiling and upper floor before we apply destUpperFootprint so that we can
                        //    // restore FLOOR attribute as needed.  Need to make sure UnApplyFootprint() handles this as well where we
                        //    // delete the footprint, then UnCollapseCeilingAndUpperFloor() to restore the FLOOR tile attribute across entire cell
                        //    CollapseCeilingAndUpperFloor(x, y, z);
                        //}

                        destUpperFootprint[i, j] |= (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING;
                        destUpperFootprint[i, j] |= (int)TILE_ATTRIBUTES.FLOOR;

                        // TODO: LADDER_LOWER_LANDING attriute can share same tile position as LADDER_UPPER_LANDING so we need to 
                        //       check for that here rather because it wont fall through to the last "else" statement below
                        if ((sourceFootprint[i, j] & (int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING) != 0)
                        {
                            // todo: I need to test that this is working in the UnApply
                            destLowerFootprint[i, j] |= (int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING;
                        }
                    }
                    else if (hasUpperLevel && (sourceFootprint[i, j] & (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0)
                    {
                        destUpperFootprint[i, j] |= (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING;
                        destUpperFootprint[i, j] |= (int)TILE_ATTRIBUTES.FLOOR;
                        // HACK - we dont want portals connecting upper landing with area directly beneath it
                        // destLowerFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING; // <- |= TILE_ATTRIBUTES.WALL does not work. prevents pathing from lower to upper landing,
                        // TODO: but what if in the CreatePortals() i modify the connectivity function to test for this special condition and allow the connectivity to occur?
                        //       NO this wont work because our connectivity function exists only in our CreateAreas() method.  And modifying flags here
                        //       occurs before CreateAreas() 
                        // TODO: are we unapplying this properly?
                        hasDestUpper = true;
                    }
                    else
                    {
                        // NOTE: Here, we dont' apply STAIR_UPPER_LANDING if it exists in the sourceFootprint to the lower destination.
                        //       it's ONLY applied to the upper level TODO: but i think if hasUpperLevel == false, (eg we're on top most floor, or at top of a nacelle) then we shouldn't allow placement of stairs or ladder
                        
                        // This requires that when we remove a structure or component, that we have access to it's original footprint.
                        destLowerFootprint[i, j] |= sourceFootprint[i, j]; // flags in the sourceFootprint are logical OR'd to dest
                    }
#if DEBUG
                    // TODO: this assert fails if the tile already contains a floor tile and we try to add another on top of it.  is this a bug?
                    //         var after = before;
                    //         after &= ~sourceFootprint[i, j];
                    //         System.Diagnostics.Debug.Assert(before == after);
#endif
                }
            // apply the data to the "footprint" layer of our mCellMap 
            // NOTE: footprint data (as is all cellmap data) fully replaced by the new value.
            //       Any bitwise operation that needs to be done must be done before setting the new value.
            if (hasDestUpper)
                mCellMap.SetMapValue("footprint", x, y + 1, z, destUpperFootprint);

            //System.Diagnostics.Debug.WriteLine("ApplyFootprint() - Origin = " + x.ToString() + ", " + z.ToString());
            mCellMap.SetMapValue("footprint", x, y, z, destLowerFootprint);
            ConnectivityDirty[y] = true;
            return true;
        }

        // NOTE: footprint passed in must already be rotated
        public bool UnApplyFootprint(int cellIndex, int[,] sourceFootprint)
        {
            uint x, y, z;
            UnflattenCellIndex((uint)cellIndex, out x, out y, out z);
            // convert from Cell coordinates to Tile coordinates and UnApply the footprint
            bool result = UnApplyFootprint (x * TilesPerCellX, y, z * TilesPerCellZ, sourceFootprint);

            return result;
        }


        // NOTE: footprint passed in must already be rotated
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">x coordinate in Tiles not Cells</param>
        /// <param name="y">y coordinate in Tiles not Cells</param>
        /// <param name="z">z coordinate in Tiles not Cells</param>
        /// <param name="sourceFootprint"></param>
        /// <returns></returns>
        public bool UnApplyFootprint(uint x, uint y, uint z, int[,] sourceFootprint)
        {

            int sourceWidth = sourceFootprint.GetLength(0);
            int sourceHeight = sourceFootprint.GetLength(1);

            if (sourceWidth == 0 || sourceHeight == 0) return false;
            bool hasUpperLevel = y < CellCountY - 2;

            int[,] destFootprint = mCellMap.GetFootprint(x, y, z, (uint)sourceWidth, (uint)sourceHeight);
            int[,] destUpperFootprint = null;

            if (hasUpperLevel)
            {
                Vector3i cell = CellLocationFromTileLocation(x, y + 1, z);
                if (cell.X == -1 || cell.Z == -1)
                    hasUpperLevel = false;
                else
                {
                    hasUpperLevel = IsCellInBounds((uint)cell.X, y + 1, (uint)cell.Z);
                    if (hasUpperLevel)
                        destUpperFootprint = mCellMap.GetFootprint(x, y + 1, z, (uint)sourceWidth, (uint)sourceHeight);
                }
            }

            bool hasDestUpper = false;
            // disable the flags in the source footprints where applicable
            for (int i = 0; i < sourceWidth; i++)
                for (int j = 0; j < sourceHeight; j++)
                {

                    // TODO: I think our ladder prefab needs LADDER_UPPER_LANDING on both the LADDER_LOWER_LANDING as well
                    //       as the area where you exit the top of the ladder
                    if (hasUpperLevel && (sourceFootprint[i, j] & (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING) != 0)
                    {
                        destUpperFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING;
                        //  here we do not want to disable the FLOOR on the destUpperFootprint if the upper floor cell is no longer collapsed
                        int index = CellIndexFromTileLocation(x, y + 1, z);
                        if (IsCellCollapsed((uint)index))
                            destUpperFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.FLOOR;

                        hasDestUpper = true;
                        if ((sourceFootprint[i, j] & (int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING) != 0)
                        {
                            // TODO: verify this is all working correctly
                            destFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.LADDER_LOWER_LANDING;
                        }
                    }
                    else if (hasUpperLevel && (sourceFootprint[i, j] & (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0)
                    {
                        destUpperFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING;
                        //  here we do not want to disable the FLOOR on the destUpperFootprint if the upper floor cell is no longer collapsed.
                        int index = CellIndexFromTileLocation(x, y + 1, z);
                        if (IsCellCollapsed((uint)index))
                            destUpperFootprint[i, j] &= ~(int)TILE_ATTRIBUTES.FLOOR;

                        hasDestUpper = true;
                    }
                    else if ((destFootprint[i, j] & (int)TILE_ATTRIBUTES.LADDER_UPPER_LANDING) != 0)
                    {
                        // DO NOTHING. if we are collapsing this cell and it contains LADDER_UPPER_LANDING and FLOOR, we do
                        // not want to delete those attributes on the current tile because the user has already placed a ladder beneath this
                        // tile. HOWEVER, we must delete those attributes if later the user decides to delete the Ladder
                    }
                    else if ((destFootprint[i, j] & (int)TILE_ATTRIBUTES.STAIR_UPPER_LANDING) != 0)
                    {
                        // DO NOTHING. if we are collapsing this cell and it contains STAIR_UPPER_LANDING and FLOOR, we do
                        // not want to delete those attributes on the current tile because the user has already placed a ladder beneath this
                        // tile.  However, we must delete those attributes if later the user to delete the stairs.

                        // TODO: if the stairs is deleted and the upper area is collapsed, we could still have FLOOR there i think.
                        //       But when we place a new floor segment there, it cleans it up and no unnecessary Areas or Portals are created.
                    }
                    else
                    {
                        // NOTE: we're only unapplying STAIR_UPPER_LADDER on the upper floor because it was
                        // never added to the lower destFootprint. (i.e the bottom floor does not have these flags set to begin with!)
                        destFootprint[i, j] &= ~sourceFootprint[i, j];
                    }
                }
                
            // remove the data from the "footprint" layer of our mCellMap by applying the new data values
            // NOTE: footprint data, as is all cellmap data, is fully replaced by the new value during SetMapValue().
            //       Any bitwise modification that needs to be done must be done before SetMapValue() and then used to
            //       replace the old destination value completely.
            if (hasDestUpper)
                mCellMap.SetMapValue("footprint", x, y + 1, z, destUpperFootprint);

            mCellMap.SetMapValue("footprint", x, y, z, destFootprint);
            return true;
        }


        // TODO: i think this UpdateAdjacents should be app centric which means in this case script centric
        //       because how segments update is specific to the scripted celledregion type
        private void UpdateAdjacents(string layerName, uint elementIndex) // TODO: is element cell or edge?
        {

            // TODO: here should we grab all neighboring Edges (including above) and running
            //       on change there too?
            // TODO: our first basic test should be, we remove a cell from being floored so that
            //       it becomes an exterior model, then we verify the model wall on the OOB cell
            //       gets removed
            // TODO:  once that is fixed, it means that we took a change, applied a rule to modify
            //       affected. and that means we can later use any rules including architectural rules
            // TODO: we must avoid recursion... how?
            // - part of the idea is that if we think about endpoints, adjacent segments have an affected endpoint and
            //   an unaffected endpoint "style".  the unaffected we simply keep the same, the affected, we must dynamically
            //   compute a new endpoint "style"
            //   - how do we know the existing endpoint styles?  they must be stored in the segment object which must have
            //     endpoints left and right i suppose?


            // we can move these out of the script, and then into the
            // Layer_SetValue()  and then on subsequent changes to the data that are done in code
            //                   _SetValue() is never called because those are for exterior calls not internal scripts
            //                   and so our Adjacents updates never get called.   However
            //                   it does not prevent us from still calling a scripted function for
            //                   validation or post action

            //                    cell boundary status changed
            //                    cell segment changed (added/removed/new style)
            //                    edge segment changed (added/removed/new style)

            switch (layerName)
            {
                case "boundaries":
                case "tile style":
                    uint[] adjacentCells = GetAdjacentCellList(elementIndex);
                    break;
                case "wall style":
                    CellEdge[] adjacentEdges = GetAdjacentEdges(elementIndex);
                    break;
                default:
                    break;
            }
            // leftEdge, rightEdge, 
            // 
            // is there a way to know here if this change was initiated by script or by exe app?
            //	- if our API calls we make here cannot be done outside of scripts, then we know it must be scripts
            //    and maybe we can prevent 
            // object sender <-- hrm
            // TODO: is there a way to run a rule from rules engine here?
        }

        internal bool CellHasFloor(uint x, uint y, uint z)
        {
            // todo: x,y,z args are cell array indices and not tile indices right?
            //       I think they must be tile indices and that means we're not enforcing
            //       that the footprint array we retrieve is restricted to just a single cell.
            //       they could just as easily straddle cell boundaries.
            //System.Diagnostics.Debug.Assert(IsCellInBounds(x, y, z));
            // These asserts verify the tile indices passed in align with full cell boundaries
 //           System.Diagnostics.Debug.Assert(x == 0 || x % TilesPerCellX == 0);
 //           System.Diagnostics.Debug.Assert(y == 0 || y < CellCountY);
 //           System.Diagnostics.Debug.Assert(z == 0 || z % TilesPerCellZ == 0);

            int[,] footprint = (int[,])Layer_GetValue("footprint", x, y, z);
            int footprintWidth = footprint.GetLength(0);
            int footprintHeight = footprint.GetLength(1);

            // does every tile within this cell have floor flag set?
            for (uint i = 0; i < footprintWidth; i++)
                for (uint j = 0; j < footprintHeight; j++)
                    if ((footprint[i, j] & (int)Interior.TILE_ATTRIBUTES.FLOOR) == 0)
                        return false;

            return true;
        }
     
        private int GetBits(int sourceBits, int start, int stop)
        {
            int results = 0;

            for (int i = start; i <= stop; i++)
                results |= (sourceBits & 1 << i);

            return results;
        }
               

        /// <summary>
        /// Given a child Entity that has a footprint, and a position within the tile grid
        /// find the Snap Position based on the odd or even count of the footprint dimensions
        /// on both axis.  For both axis being odd numbered for example, the snap position
        /// would be in the center of the tile that contains the "position" argument.
        /// If the axis are even, the snap point will be between tiles along the edge
        /// that is closest to the "position" argument.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="position"></param>
        /// <param name="startTileLocation">the bottom left most tile where this footprint will be applied</param>
        /// <returns>snap position</returns>
        /// <remarks>This method is called via EntityAPI during QueryPlacement and Hud for rendering footprint preview</remarks>
        public Vector3d GetTileSnapPosition(int[,] footprint, Vector3d position, byte rotation, out Vector3i startTileLocation)
        {
            if (footprint == null) throw new ArgumentOutOfRangeException();

            Vector3d gridInterval = new Vector3d(TileSize.x, TileSize.y, TileSize.z);
            double gridSizeX = CellCountX * TilesPerCellX;
            double gridSizeY = CellCountY * TileSize.y;
            double gridSizeZ = CellCountZ * TilesPerCellZ;

            Vector3d snap = Utilities.MathHelper.Snap(position, gridInterval); // todo: the snap should be clamped to gridSizeX/Y/Z
            Vector3d center = new Vector3d(snap.x + TileSize.x / 2d, snap.y, snap.z + TileSize.z / 2d);
            Vector3i location = TileLocationFromPoint(center);

            int[,] rotatedFootprint = GetRotatedFootprint(footprint, rotation);

            // NOTE: startTileLocation is not used for the case where there is no footprint, but we assign the "out" parameter anyway
            startTileLocation = location;
            
            if (rotatedFootprint == null) return center;

            int footprintWidth = rotatedFootprint.GetLength(0);
            int footprintHeight = rotatedFootprint.GetLength(1);

            // default startTileLocation
            startTileLocation.X = location.X - (footprintWidth / 2);
            startTileLocation.Z = location.Z - (footprintHeight / 2);
                      
            return snap;             

            // NOTE: if both footprint width and height are both even length,
            // the position we snap to will be a tile Vertex, not the mid-point of a tile Edge.
            // That vertex position then is the center of the plotted footprint on the tilemask grid
            // where there are even number of tiles on all sides of that vertex.

        }

        /// <summary>
        /// Given a child Entity that has a footprint, and a position within the tile grid
        /// find the Snap Position based on the odd or even count of the footprint dimensions
        /// on both axis.  For both axis being odd numbered for example, the snap position
        /// would be in the center of the tile that contains the "position" argument.
        /// If the axis are even, the snap point will be between tiles along the edge
        /// that is closest to the "position" argument.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3d GetTileSnapPosition(int[,] footprint, Vector3d position, byte rotation)
        {
            Vector3i dummy;
            return GetTileSnapPosition(footprint, position, rotation, out dummy);
        }

        /// <summary>
        /// Finds the lower left most tile given a footprint that we want centered about the tile specified.
        /// </summary>
        /// <param name="tileLocation"></param>
        /// <param name="footprint">Footprint width and height (depth) dimensions should both be multiples of 2</param>
        /// <param name="originX"></param>
        /// <param name="originZ"></param>
        public void GetDestinationOriginFromTileLocation(Vector3d position, int[,] footprint, out int originX, out int originZ)
        {
            Vector3i tileLocation = TileLocationFromPoint(position);
            GetDestinationOriginFromTileLocation(tileLocation, footprint, out originX, out originZ);
        }


        /// <summary>
        /// Finds the lower left most tile given a footprint that we want centered about the tile specified.
        /// </summary>
        private void GetDestinationOriginFromTileLocation(Vector3i tileLocation, int[,] footprint, out int originX, out int originZ)
        {
            // TODO: I think the only real issue with this function is that a new Vector3d position should be computed and passed out
            //       The new position should snap to either center of tileLocation if the footprint is even numbered on x and z axis
            //       or it should snap to the position between tiles for x and/or z if odd numbered footprint width and/or height.
            // TODO: And I think we should know that if the snapping is Center of Cell, then the tileLocation passed in is just
            //       an approximation because the center of the cell that is 16x16 tiles sits between tiles.
            //       This is why startTileLocation (which is bottom left most tile) and footprint[,] size is more important than
            //       the tile that contains the mouse drop location.  The mouse drop tile location is basically just a guide and otherwise insignificant
            //       once the component is placed on the deck/level.

            // TODO: This entire function should be removed.  We should be computing start tile location from a Vector3d position passed in.
            // TODO: this should work with a 3d coordinate that is on a tile border so that we can determine which start tile to use that is equally on both sides of the 3d position.
            //       only if odd number of tiles in footprint, should the tilelocation passed in be regarded as the "center" tile.
            int width = footprint.GetLength(0);
            int height = footprint.GetLength(1);
            System.Diagnostics.Debug.Assert(width % 2 == 0);
            System.Diagnostics.Debug.Assert(height % 2 == 0);

            // NOTE: for odd numbered width and height, the integer divide rounds down to 0 and that is correct.
            // NOTE: width and height should always be multiples of 2... maybe.  i really dont like that restriction
            originX = tileLocation.X - (width / 2);
            originZ = tileLocation.Z - (height / 2);
        }

        /// <summary>
        /// Find the footprint start location given the drop point within the floor/level.  This can output
        /// a refined "snapped to" position as well.
        /// </summary>
        /// <param name="position">Generally, the position.y component should be at the floor of the Cell, not the center height</param>
        /// <param name="footprint"></param>
        /// <returns></returns>
        public Vector3i GetFootprintStartLocationFromPosition(Vector3d position, int[,] footprint)
        {
            // TODO: I think we have two cases to start with.  Either position is snapped to center of a tile if footprint is odd in x and/or z axis
            //       Or position is inbetween Tiles if footprint is even on x and/or z axis.  So we must always snap to one of these two
            //       methods for each axis and compute the start tile location for the footprint based on those values.

            // TODO: if the position is directly snapped to the border of two tiles, which tile is returned in the following call?
            //       I assume it retrieves the tile that is bottom left most.  That would mean the tile location.x and loaction.z components are odd numbered
            //       HOWEVER, if it's a smaller footprint, say 4x4, then we could easily have a placement location where the location should have odd numbered locaty.x/z components right?
            //       Are we doing this the wrong way?  Should we be computing the start tile location first and from there, compute the position of the child entity?  
            //       That way we can visualize the placement of the footprint during AssetPlacementTool preview render 


            // somehow i think im just overcomplicating this issue.  
            Vector3i tileLocation = TileLocationFromPoint(position);

            // TODO: verify even numbered width and depth of footprint when rotated 90, 180 and 270 degrees
            // maintains the same number of tiles in the footprint for width and depth
            if (footprint == null) throw new ArgumentNullException();

            int width = footprint.GetLength(0);
            int depth = footprint.GetLength(1);

            if (width < 1 || depth < 1) throw new ArgumentOutOfRangeException();

            bool evenWidth = width % 2 == 0;
            bool evenDepth = depth % 2 == 0;

            // is the point directly in the center of the Cell?
            // todo: does the caller of this method already know the pickresults? we should have the "FaceID" or CellID most likely
            int floor = GetFloorFromAltitude(position.y);
            if (floor == -1) throw new ArgumentOutOfRangeException();

            // convert the impact point to i, k, j 0 based indices
            // NOTE: no "reversal" of Z is needed because our cells are not stored in an image, but in an array
            uint j = (uint)(((mCellSize.x * CellCountX / 2f) + position.x) / mCellSize.x);
            if (j >= CellCountX) j = CellCountX - 1;
            uint k = (uint)(((mCellSize.z * CellCountZ / 2f) + position.z) / mCellSize.z);
            if (k >= CellCountZ) k = CellCountZ - 1;
            // TODO: the following code i think does the same thing and we already have it.
            // furthermore, i think we can optimize both into a simpler function
            Vector3i tmp = CellLocationFromPoint(position.x, position.y, position.z);
            System.Diagnostics.Debug.Assert(tmp.X == j);
            System.Diagnostics.Debug.Assert(tmp.Z == k);

            // TODO: In the above code, we need to know if we're snapping to the center of a Cell or not.
            //       Because in most cases, I think we allow most components to lay across cell boundaries.
            //       It's only things like stairs that must snap, walls and doors and hatches which have placement restrictions
            //       but we need to keep it possible to have just regular components to lay across multiple cells if necessary such as
            //       with big Reactors and such.
            // TODO: or maybe we dont let things like chairs crew stations and such straddle Cells if they can be placed wholely within a single cell do their footprint size being 16x16 or less.
            //       But i don't like that.  If there are no Cell or Edge snap restrictions, we should allow regular components to straddle cell boundaries if the player chooses too.
            uint cellID = FlattenCell(j, (uint)floor, k);

            Vector3d cellCenterPosition = GetCellCenter(cellID);
            cellCenterPosition.y = GetFloorHeight((uint)floor);

            //System.Diagnostics.Debug.Assert(position.y == cellCenterPosition.y);
            bool centerOfCell = position == cellCenterPosition;


            Vector3d snapPosition;
            if (evenWidth)
            {
                // we should snap to a position along x axis that is between two tiles

                double halfWidth = TileSize.x * 0.5d;
                snapPosition.x = TileStartX * tileLocation.X + halfWidth;
            }

            if (evenDepth)
            {
                // we should snap to a position along the z axis that is between two tiles
                double halfDepth = TileSize.z * 0.5d;
                snapPosition.z = TileStartZ * tileLocation.Z + halfDepth;
            }

            Vector3i result;
            result.X = (int)j;
            result.Y = floor;
            result.Z = (int)k;

            return result;
        }

        ///http://gravitywarsgame.com/CollisionDetectionTutorial.aspx
        //
        //
        //                    // we can & our two values together and do the mask flag last because
        //                    //<Mith> 00 & 00 & 10 = 00
        //                    //<Mith> 00 & 10 & 10 = 00
        //                    //<Mith> 10 & 00 & 10 = 00
        //                    //<Mith> 10 & 10 & 10 = 10
        //                    // http://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net
        //                    // http://stackoverflow.com/questions/2031217/what-is-the-fastest-way-i-can-compare-two-equal-size-bitmaps-to-determine-whethe/2038515#2038515

        // http://www.austincc.edu/cchrist1/GAME1343/TransformedCollision/TransformedCollision.htm
        //// http://gravitywarsgame.com/CollisionDetectionTutorial.aspx

        // http://gamedev.stackexchange.com/questions/38118/best-way-to-mask-2d-sprites-in-xna
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFootprint">The already rotated, footprint of the component we wish to place.</param>
        /// <param name="destFootprint">The footprint or portion of footprint that we will compare with source footprint.
        /// The dest footprint's dimensions must be equal to or greater than the respective dimensions of the source and 
        /// must already be aligned to same coordinate space</param>
        /// <param name="mask"></param>
        /// <returns></returns>
        private bool FootprintCollides(int[,] sourceFootprint, int[,] destFootprint)
        {
            int sourceWidth = sourceFootprint.GetLength(0);
            int sourceHeight = sourceFootprint.GetLength(1);
            int destWidth = destFootprint.GetLength(0);
            int destHeight = destFootprint.GetLength(1);

            // if any index of the source with or without the offset applied will be out of bounds
            // within the destination, return as collision == true.
            if (sourceWidth > destWidth || sourceHeight > destHeight) return true;

            for (int x = 0; x < sourceWidth; x++)
                for (int z = 0; z < sourceHeight; z++)
                {
                    // here we test if the source and dest have any flags the same, if so that is a collision
                    // since two components cannot set the same flags on the destination
                    if ((sourceFootprint[x, z] & destFootprint[x, z]) != 0)
                        return true;
                }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceFootprint">The already rotated, footprint of the component we wish to place.</param>
        private bool FootprintCollides(int x, int y, int z, int[,] sourceFootprint)
        {
            if (sourceFootprint == null) return false;
            int sourceWidth = sourceFootprint.GetLength(0);
            int sourceHeight = sourceFootprint.GetLength(1);

            if (sourceWidth == 0 || sourceHeight == 0) return false;

            // footpring with and height must be even divisible by 2 
            // warning: wait this is not (yet) necessarily so when we are rotating!
            //if (sourceWidth % 2 != 0 && sourceHeight % 2 != 0) throw new ArgumentOutOfRangeException();

            if (mCellMap.TilesAreInBounds(x, y, z, sourceWidth, sourceHeight) == false) return true;

            int[,] destFootprint = mCellMap.GetFootprint((uint)x, (uint)y, (uint)z, (uint)sourceWidth, (uint)sourceHeight);

            bool result = FootprintCollides(sourceFootprint, destFootprint);
            //System.Diagnostics.Debug.WriteLine("CelledRegion.IsChildPlaceable() - " + result.ToString());

            return result;
        }

        public int[,] GetRotatedFootprint(int[,] sourceFootprint, Quaternion rotation)
        {
            byte componentRotationIndex = rotation.GetComponentYRotationIndex();
            return GetRotatedFootprint(sourceFootprint, componentRotationIndex);
        }

        public int[,] GetRotatedFootprint(int[,] sourceFootprint, byte rotation)
        {
            switch (rotation)
            {
                case 32:  // 45 degrees
                    sourceFootprint = ArrayExtensions.RotateTheta(sourceFootprint, Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS * 45d);
                    break;
                case 64:  // 90 degrees 
                    sourceFootprint = ArrayExtensions.Rotate90(sourceFootprint);
                    break;
                case 96:  // 135 degrees
                    sourceFootprint = ArrayExtensions.RotateTheta(sourceFootprint, Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS * 135d);
                    break;
                case 128:  // 180 degrees 
                    sourceFootprint = ArrayExtensions.Rotate180(sourceFootprint);
                    break;
                case 160: // 225 degrees
                    sourceFootprint = ArrayExtensions.RotateTheta(sourceFootprint, Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS * 225d);
                    break;
                case 192: // 270 degrees
                    sourceFootprint = ArrayExtensions.Rotate270(sourceFootprint);
                    break;
                case 224: // 315 degrees
                    sourceFootprint = ArrayExtensions.RotateTheta(sourceFootprint, Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS * 315d);
                    break;
                case 0:   // 0 or 360 degrees
                default:
                    break;
            }

            return sourceFootprint;
        }



        #region Visualizations      

        /// <summary>
        /// Floors and Ceiling models.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="textureAtlas"></param>
        /// <param name="index"></param>
        /// <param name="reverseWindingOrder"></param>
        /// <returns></returns>
        private Model CreateFloorOrCeilingModel(string description, Keystone.Appearance.TextureAtlas textureAtlas, int index, bool reverseWindingOrder)
        {
            // create Floor Model
            string modelID = GetInteriorElementPrefix(this.ID, description, typeof(Model), index);
            Keystone.Elements.Model model = new Keystone.Elements.Model(modelID);
            // OBSOLETE: defaulting to .Enable = false is no longer necessary.  We can now
            //           set Visible=false for ISpatialNodes (eg individual Quadtree roots 
            //           in our QuadtreeCollections that host entire floors)
            //model.Enable = false; // default to false

            string id = GetInteriorElementPrefix(this.ID, description, typeof(Keystone.Appearance.Layer), index);
            Keystone.Appearance.Layer layer = new Keystone.Appearance.Diffuse(id);
            layer.AddChild(textureAtlas);

            id = GetInteriorElementPrefix(this.ID, description, typeof(Keystone.Appearance.DefaultAppearance), index);
            Keystone.Appearance.DefaultAppearance appearance = new Keystone.Appearance.DefaultAppearance(id);
            appearance.AddChild(Keystone.Appearance.Material.Create(Keystone.Appearance.Material.DefaultMaterials.iron));
            appearance.AddChild(layer);

            // we dont want to share this grid so we produce unique name rather
            // than auto generating a name based on the grid specifications
            // NOTE: we use a naming convention on the mesh so we can deduce it in script
            string meshID = GetInteriorElementPrefix(this.ID, description, typeof(Mesh3d), index);
            // TODO: this is an issue.  normally the floormesh can be recreated from the id string
            // if the mesh is a primitive, however, in the case we dont want to share meshes
            // becuase we want unique UV's, then we are in a pickle.  The problem is not in the .CreateCellGrid
            // call but in the LoadTVResource function which reads the id to determine if this resource
            // should be found on disk or generated as primitve using specification defined in the 'id" string
            // itself.  So we need a way to also tag a unqiue ID to a primitve mesh.  The easiest way
            // is to on the .CreateCellGrid to enforce saving the file... perhaps its something we even do
            // here.  where do we save it?
            // can we save it in the prefab DURING the creation of the prefab?
            // TODO: but you know, we dont actually have to save it IF we can just have grid creation
            // based on proper grid name but with a tag to ensure we don't ever share existing mesh and create new and then for LoadTVResource()
            // to properly parse that id name and create a unique mesh instance

            Keystone.Elements.Mesh3d mesh = Keystone.Elements.Mesh3d.CreateCellGrid(
                (float)this.CellSize.x,
                (float)this.CellSize.z,
                this.CellCountX,
                this.CellCountZ,
                1.0f, reverseWindingOrder, meshID);

            // this call to LoadTVResource will occur on the same thread that is handling
            // this CelledRegion's.LoadTVResource() must be called before attempting to initialize UV coords
            if (mesh.TVResourceIsLoaded == false)
                Keystone.IO.PagerBase.LoadTVResource(mesh, false); // NOTE: should always call through pager, never directly geometry.LoadTVResource()

            // initialize the UV
            uint textureAtlasIndex = 0;
            for (uint x = 0; x < CellCountX; x++)
                for (uint z = 0; z < CellCountZ; z++)
                    Keystone.Celestial.ProceduralHelper.CellGrid_SetCellUV(this.CellCountX, this.CellCountZ, x, z, mesh, textureAtlas, textureAtlasIndex);


            mesh.CullMode = (int)MTV3D65.CONST_TV_CULLING.TV_BACK_CULL;
            model.AddChild(appearance);
            model.AddChild(mesh);

            return model;
        }
        
        private ModelSequence CreatePrefabModelsWithMinimeshGeometry(string prefabPath, uint floor)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(prefabPath));
            ModeledEntity prefab = null;
            ModelSequence modelSequence = null;
            ModelSelector modelSelector = null;
            Model model = null;

            // GetModelSequenceID is similar to GetInteriorElementPrefix() except that for the description, it computes the hash of the prefabPath and texturePath
            string id = GetModelSequenceID(prefabPath, floor);
            //System.Diagnostics.Debug.WriteLine("CelledRegion.CreatePrefabModelsWithMinimeshGeometry() - Model Sequence ID = " + id);
            // attempt to recylce existing ModelSequence or Model

            object tmp = Resource.Repository.Get(id);
            if (tmp == null)
            {
                // load the prefab
                // TODO: should the prefab be already loaded before it's path is passed in here? I think
                // if we allow user to select the type of wall from assetbrowser, the workerthread
                // should load the prefab and pass it in.
                prefab = LoadPrefab(prefabPath);
                System.Diagnostics.Debug.Assert(prefab.ChildCount > 0);

                // create the Sequence
                modelSequence = (ModelSequence)Resource.Repository.Create(id, "ModelSequence");

                if (prefab.Children[0] is ModelSelector)
                {
                    modelSelector = (ModelSelector)prefab.Children[0];
                    for (int i = 0; i < modelSelector.ChildCount; i++)
                    {
                        model = ((Model)(modelSelector.Children[i]));

                        DoModel(modelSequence, model, i, floor);
                    }
                }
                else if (prefab.Children[0] is Model)
                {
                    model = (Model)prefab.Children[0];
                    DoModel(modelSequence, model, -1, floor);
                }
                else throw new ArgumentOutOfRangeException("Unexpected child type - " + prefab.Children[0].TypeName);
            }
            else
            {
                modelSequence = (ModelSequence)tmp;
            }
                

            return modelSequence;
        }


        private void DoModel(ModelSequence modelSequence, Model model, int modelSubIndex, uint floor)
        {
            uint count = CellCountX * CellCountZ;
            bool doubleSided = false;
            if (doubleSided) count *= 2;
            //count += 1024; // 2048 // TODO: temp HACK to make room for wall segments we add at runtime until we finalize that implementation
                           // keep in mind that lots of Cells on each floor are not painted as being InBounds

            // NOTE: MinimeshGeometry's internal TVMinimesh is not instantiated here.  That is handled
            //       in Worker_CelledRegion_Paint() but frankly, I don't what difference it would make since
            //       this call still gets executed on the same worker thread.
            string minimeshID = Resource.Repository.GetNewName("MinimeshGeometry"); // this isn't needed right? -> GetInteriorElementPrefix(this.ID, description, typeof(MinimeshGeometry), (int)floor);
            MinimeshGeometry miniMesh = (MinimeshGeometry)Resource.Repository.Create(minimeshID, "MinimeshGeometry");
            string meshPath = model.Geometry.ID;
            miniMesh.SetProperty("meshortexresource", typeof(string), meshPath);
            miniMesh.MaxInstancesCount = count;
            // NOTE: -> miniMesh.Mesh3d  property exists for doing a collision on a particular element
            //          we simply need to minimesh.Collide (elementIndex) as well as provide a RegionMatrix that we can multiply the local
            //          transform with 				
            //Matrix m = miniMesh.GetElementMatrix (elementIndex);
            //miniMesh.AdvancedCollide (m, start, end, parameters);

            // create two seperate models hosting the same MinimeshGeometry for each Model and Mesh3d in the prefab's ModelSequence.
            // 2 seperate models are required because Model's cannot be shared and one Model must be parented
            // to the CelledRegion._sequence Node so it can be rendered, and the other is recycled via the "modelSequence" var we create for every
            // instance of an edge's MinimeshMap that uses this prefab + appearance combination.
            string subIndex = "";
            if (modelSubIndex > -1)
                subIndex = PREFIX_DELIMIETER + modelSubIndex.ToString();

            string description = minimeshID + subIndex + PREFIX_DELIMIETER + "minimeshmap" + PREFIX_DELIMIETER + PREFIX_WALL;
            string modelID = GetInteriorElementPrefix(this.ID, description, typeof(Model), (int)floor);
            Model modelWithMinimeshGeometry = (Model)Resource.Repository.Create(modelID, "Model");
            description = minimeshID + subIndex + PREFIX_DELIMIETER + "sequence" + PREFIX_DELIMIETER + PREFIX_WALL; 
            modelID = GetInteriorElementPrefix(this.ID, description, typeof(Model), (int)floor);
            Model modelWithMinimeshGeometryForSelector = (Model)Resource.Repository.Create(modelID, "Model");

            // TODO: miniMesh.LoadTVResource() immediately if celledRegion.LoadTVResource() is called?
            modelWithMinimeshGeometry.AddChild(miniMesh);
            modelWithMinimeshGeometryForSelector.AddChild(miniMesh);

            // appearance 
            string appearanceID = Resource.Repository.GetNewName("DefaultAppearance"); // GetInteriorElementPrefix(this.ID, description, typeof(Keystone.Appearance.DefaultAppearance), (int)floor);
                                                                                       //Keystone.Celestial.ProceduralHelper.CreateAppearance(MTV3D65.CONST_TV_LIGHTINGMODE.TV_LIGHTING_NORMAL, null, texturePath, null, null, null, true);
            Appearance.DefaultAppearance appearance = (Appearance.DefaultAppearance)Resource.Repository.Create(appearanceID, "DefaultAppearance");
            Appearance.Appearance prefabAppearance = model.Appearance;
            if (prefabAppearance != null)
            {
                // todo: do i need to apply the ModsPath here?  Doesn't our LoadTVResource() within shader do that for us?
                string shaderID = Core._Core.ModsPath + "\\caesar\\shaders\\minimesh\\mini_textured_specular_dirlightt.fx"; // "\\caesar\\shaders\\minimesh\\mini_notexture_specular_dirlightt.fx";
                Shaders.Shader shader = (Shaders.Shader)Keystone.Resource.Repository.Create(shaderID, "Shader");

                appearance.AddChild(shader);

                // for standard TVMinimesh built in shader, the material diffuse is used to set the minimesh element color.
                // specular, ambient, emissive are ignored.
                Keystone.Appearance.Material material = prefabAppearance.Material; // TODO: should this use model.Material? otherwise we are using a default Material
                if (material != null)
                    appearance.AddChild(material);

                // NOTE: we add the Layer and not the Texture 
                // TODO: I might just be able to add the Layer[0] and not the Material above which is already assigned to the Layer
                // TODO: verify it's ok to share this Layer?  It should be ok since the Mesh3d which uses it only uses it for MinimeshGeometry assigned to it
                if (prefabAppearance.Layers != null && prefabAppearance.Layers[0] != null)
                    appearance.AddChild(prefabAppearance.Layers[0]);

                // TODO: What if we need a shader?  Actually shader should be in the Layer and I need to make sure the Layer is getting it assigned.  MinimeshGeometry shader is different than Mesh3d shader
                // TODO: can we easily change the Material on MinimeshGeometry at runtime? 

            }
            // TODO: I think appearances normally should never be shared (just the Material and Texture child nodes are shared)
            //       However, in this case, one of these two models is never visible so changes to the shared Appearance node is fine.
            modelWithMinimeshGeometry.AddChild(appearance);
            modelWithMinimeshGeometryForSelector.AddChild(appearance);
            // TODO: all sub-Models MUST use same texture
            // TODO: no texture is added here and neither is a material.  Can material color be assigned per minimesh element? should be able to, would have to be done in auto-tile i think
            modelWithMinimeshGeometry.Enable = false;
            modelWithMinimeshGeometryForSelector.Enable = true;



            // we add 2 seperate Models to both the modelSequence which will only be stored in Repository and
            // in MinimeshMap, and to the celledRegion's internal _sequence so they can be rendered. 
            // NOTE: the same miniMesh instance is added to both Models
            modelSequence.AddChild(modelWithMinimeshGeometry);
            // TODO: are we removing this modelSequence from Repository when all walls that use it are deleted? we need to IncrementRef and DecrementRef to remove it from Repository
            // TODO: how do we find the correct modelWithMinimeshGeometryForSelector in the _selector to delete when deleting all walls of a specific prefab and appearance?
            // TODO: are we even trying to delete them from the _sequence


            //       for now ill just add all modelWithMinimeshGeometryForSelector all under the same _sequence even though in future
            //       we might want nested _sequences for walls and floors or for each floor? Though, we do create seperate Models
            //       for each sub-Model based on floor level.

            // TODO: how do we find the _sequence version of the Model to remove when all 
            //       minimesh elements for that particular Model are removed and since we have 
            //       two different models 1)for _sequence 2)for minimeshmaps. Combined with the fact
            //       we should only delete all sub-models from the _sequence when all sub-models of
            //       a particular prefab have 0 minimesh elements in use.
            // TODO: shouldn't _sequence first contain child modelSelector's for cases where we have different
            //       styled walls?  Right now, this only allows one prefab's ModelSequence children. Or
            //       should we just store in a dictionary, every modelSequence based on prefab ID and floor/layer?
            //       Then it's a quick lookup to find the correct one without going through the dictionary.
            _sequence.AddChild(modelWithMinimeshGeometryForSelector);
        }

        //private Model CreateMinimeshGeometry()
        //{

        //}

        // todo: this is tempoary.  In the future, the AssetPlacementTool (or maybe in FormMain.Commands) will load the ModeledEntity prefab
        // and pass it in, instead of just the prefabPath. 
        // NOTE: this function does end up being called by WorkerThread which is good.
        private ModeledEntity LoadPrefab(string prefabPath)
        {
            System.IO.Stream stream = null;
            Keystone.IO.XMLDatabaseCompressed xmldb_c = new Keystone.IO.XMLDatabaseCompressed();
            string fullPath = Core.FullNodePath(prefabPath);
            KeyCommon.IO.ResourceDescriptor descriptor = new KeyCommon.IO.ResourceDescriptor(fullPath);
            Keystone.Scene.SceneInfo info = xmldb_c.Open(descriptor, out stream);

            // NOTE: clone entities arg == false in ReadSychronous() call.
            bool delayIPageableLoading = true; // we don't want to recursively page in all  nodes.  This is important for Interior regions too.
            bool recursiveLoadChildren = true;
            bool cloneEntities = true;
            string[] clonedEntityIDs = null;
            Keystone.Elements.Node node = xmldb_c.ReadSynchronous(info.FirstNodeTypeName, null, recursiveLoadChildren, cloneEntities, clonedEntityIDs, delayIPageableLoading, false);

            if (stream != null)
            {
                // NOTE: stream must remain open until after ReadSynchronous call above
                stream.Close();
                stream.Dispose();
            }

            xmldb_c.Dispose();

            if (!(node is ModeledEntity))
                throw new Exception("CelledRegion.LoadPrefab() - ERROR: Node not of valid Entity type.");

            ModeledEntity e = (ModeledEntity)node; // node must always be modeled entity

            e.CollisionEnable = false;
            e.Visible = true;
            e.Pickable = false;
            e.Dynamic = false; // no physics step is used
            e.InheritRotation = false;
            e.InheritScale = true; // <-- The preview entity SHOULD inherit scale of the world it's being placed into right?
            e.Attributes |= KeyCommon.Flags.EntityAttributes.HUD;

            // set up the prefab link (i dont suppose there is a better place to do this?)
            e.SRC = prefabPath; // TODO: shouldn't this be the descriptor because we need to know which ModPath, or does the prefabPath contain the modpath in it?

            // sychronous loading
            // August.22.2017 - we do not want to recurse and load Interior Region's for asset preview
            // But we DO need any available entity script to load.
            Keystone.IO.PagerBase.LoadTVResource(e, false);
            // TODO: I think we do need to load the script afterall.  This does make creating the preview
            //       entity slower, but we do need to be able to query the type of entity this is so that
            //       the parent entity that this child is added to, knows how to place it. e.g a door within 
            //       an interior.
            //       TODO: I think the key is that we remember that a preview entity which is NOT added
            //       the the scene (it's just rendered by HUD) cannot be initialized and so certain functions
            //       within the script cannot be called.  However, some can and that's ok.. such as QueryCellPlacementType
            Keystone.Elements.Model[] models = e.SelectModel(Keystone.Elements.SelectionMode.Render, 0);

            if (models != null)
            {
                for (int i = 0; i < models.Length; i++)
                {
                    Keystone.Appearance.Appearance appearance = models[i].Appearance;
                    Keystone.IO.PagerBase.LoadTVResource(appearance, true);
                    Keystone.Elements.Geometry geometry = models[i].Geometry;
                    Keystone.IO.PagerBase.LoadTVResource(geometry, false);
                }
            }

            return e;
        }

        


        //            //// TODO: THIS IS A TON OF MEMORY!!!!!! 
        //            //int bytesInMegabyte = 1024000; // 1000000;
        //            //float megabytes = mTileMask.Length / ((float)bytesInMegabyte);
        //            //System.Diagnostics.Debug.WriteLine(string.Format("Creating 8bit tile data containing {0} elements and consuming {1} megabytes.", mTileMask.Length, megabytes));


        //            // mLayersDatabase.Add("connectivity", new BitArrayLayer(CellCountX, CellCountY, CellCountZ));
        //            // mLayersDatabase.Add("footprints", new BitArrayLayer(CellCountX, CellCountY, CellCountZ));

        //            // following layer types can be added via custom script for this celledRegion
        //            // mLayersDatabase.Add("computernetwork", new BitArrayLayer(CellCountX, CellCountY, CellCountZ));
        //            // mLayersDatabase.Add("electrgrid", new BitArrayLayer(CellCountX, CellCountY, CellCountZ));
        //            // mLayersDatabase.Add("ventilation", new BitArrayLayer(CellCountX, CellCountY, CellCountZ));
        //            // mLayersDatabase.Add("plumbing", new BitArrayLayer(CellCountX, CellCountY, CellCountZ));

        //            // TODO: following about floors and walls being entity is not true.  They can be
        //            //       defined as inherent structures of the CelledRegion but with ability to
        //            //       have a custom script side defined "State" object for handling damage, hitpoints, armor,etc


        /// <summary>
        /// Called by Worker_CelledRegion_Paint() in the worker thread to instantiate our MinimeshGeometry and Textures
        /// If the MinimeshGeometry already exists, it recycles it.
        /// </summary>
        /// <param name="edgeID"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public bool CreateEdgeSegmentStyle(uint edgeID, EdgeStyle style)
        {
            // NOTE: here the wall models (which host the MinimeshGeometry) are added to internal _selector 
            // when created, but their resources are not paged in (eg. mini, mesh or textures)
            // It is callers responsibility to initiate Paging.  In this case
            // it is Worker_CelledRegion_Paint() that pages in the Geometry and Textures in the worker thread.

            
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            uint floor = edge.Layer;

            // the following call now involves ultimately a call to a script which then
            // calls SetSegmentStyle, which primarily will determine the edge, get the model and minimesh
            // (creating them if need be) and removing an existing if applicable.  
            // then it also enables the minimesh element and tracks it with MinimeshMap object
            // but what really needs to happen is, Layer_SetValue needs to do a lot less work.
            // we need more functions here for first creating any meshes we need.
            //celledRegion.Layer_SetValue(paintCell.LayerNames[j], index, value);

            // TODO: here we create (unless we recycle) two MinimeshGeometry Models 
            //       but that was before we had ModelSelector hosting sub-models for different
            //       wall meshes.  Do we need to create all MinimeshGeometry for each sub-model
            //       or do we select via auto-tile algorith, the Model/MinimeshGeometry we want
            //       and just load that one for each side of the double sided edge?
            //       Hrm, or do we create all of the sub-Models and MinimeshGeometry and add them
            //       to the MinimeshMap?
            //       Do we auto-tile here?  I think that should be done in the call where we AddElement to the appropriate MinimeshGeometry node.
            //
            //       Recall that we are not adding elements here, we're just creating the Model and MinimeshGeometry
            //       and assigning it to the MinimeshMap which gets added to Dictionary by edgeID.
            // TODO: I think we do need to create all the sub-models here because the EdgeStyle holds the prefab
            //       paths for each side of the double sided wall, and the style allows for selection
            //       of any of the sub-models.  In fact, changing/adding/removing an edge style
            //       later, means we need to be able to query the adjacents and potentially re-auto-tile their
            //       sub-model selections and for that to work properly and quickly, we need to have
            //       all sub-model's instantiated.




            //System.Diagnostics.Debug.WriteLine(string.Format("Edge Row = {0} Column = {1}", edge.Row, edge.Column));

            //uint adjacentX, adjacentY, adjacentZ;

            // TODO: Wall placement logic/updates given the style should occur automatically
            //       so that when new adjacents are placed, they are updated and such that
            //       if new cells are made in bounds and tiled, single sided walls will properly
            //       turn into double sided walls.
            //       Or, maybe not... maybe our double sided walls provide us with the opportunity to define
            //       the exterior hull... but if our damage/hitpoints are essentially side independant
            //       then what is the point of double sided exteriors as they add nothing...
            //       - the real question is, triggering the updates based on changes to cells
            //       and segments and adjacent cells/edges/segments.
            //       - so short term, we dont care about writing the logic, we just want to know where
            //       to do that.
            //       - i suspect 
            // TODO: after completing the boundary tiles, i should be able to autogenerate a default
            //       exterior walls for every layer that can have it's style changed, but not deleted.
            // TODO: after comopleting boundary tiles, i should also start the floors as collapsed
            //       with the exception of ceilings where there is no in bounds cell above it
            //       just below it, then that ceiling should not be deletable also, only style changed.
            // TODO: option to view current floor and all beneath it since we will want
            //       to be able to see through collapsed cells

            if (string.IsNullOrEmpty(style.Prefab))
            {
                return false;
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine("CelledRegion.CreateEdgeSegmentStyle() - style." + style.Prefab);
                ModelSelector selector = CreatePrefabModelsWithMinimeshGeometry(style.Prefab, floor);

                if (selector != null)
                    Keystone.IO.PagerBase.LoadTVResource(selector, true);
                return true;

            }
        }


        private MinimeshMap DeleteWallModelView(uint segmentEdgeID, EdgeStyle style)
        {
            //CellEdge.EdgeStatistics es = CellEdge.GetEdgeStatistics(CellCountX, CellCountY, CellCountZ);

            CellEdge edge = CellEdge.CreateEdge(segmentEdgeID,
                    CellCountX,
                    CellCountY,
                    CellCountZ);

            // TODO: isn't this supposed to grab an existing MinimeshMap at the edgeID and if it doesn't exist
            // it means there is no wall segment there and the user is just dragging the erase wall tool
            // over an empty edge which is fine.  It just does nothing.
            MinimeshMap map;
            bool result = mEdgeModelMap.TryGetValue (segmentEdgeID, out map);
            // if this edge is already empty, return an empty map
            if (!result) return map;

            bool success = RemoveWallModelView(segmentEdgeID, out map);
            System.Diagnostics.Debug.Assert(result == true, "CelledRegion.DeleteWallModelView() - Edge not found.");
            // TODO: and then, only modify the Model's, Minimesh indices, etc.  
            //       and then also, shouldn't we be updating the value in the mEdgeModelMap since MinimeshMap is a struct not a class?
            map.Edge = edge;
            map.Style = style;
            map.EdgeLocation = GetEdgeLocation(edge.BottomLeftCell, edge.TopRightCell);
            map.Model = null;
            map.Minimesh = null;
            // TODO: where am I removing the minimesh element prior to assigning it's map ElementIndex to -1?
            map.ElementIndex = -1;

            // TODO: here if the prefab is no longer in use by any edge on this floor, we need to delete
            //       from celledRegion._sequence all sub-models used.  We also need to delete via incrementRef/decrementRef
            //       the ModelSequence cached in at least on of the MinimeshMaps.  I think this means we should cache
            //       the modelsequenceID in each MinimeshMap
            foreach (Node child in this._sequence.Children)
            {
                if (child is Model)
                {
                    Model model = (Model)child;

                }
            }
            return map;
        }


        // TODO: should we calculate the footprint dimensions and bottom/left location here
        //       too? or should we create a seperate function that essentially has similar structure as this one
        //       with giant if/else statements based on adjacents and parallels and perpendiculars and such.
        //       Just as this function is not elegant, i dont think there is an elegant way to compute
        //       the footprint location and dimensions.  Although, i think there are fewer cases... that is to say
        //       cases can be merged.  There are only 4 possible footprint cases (8 if you count horizontal and vertical edges seperate).
        //       And unfortunately, this is all hard coded.  The footprints are not varied based on the actual dimensions of the wall
        //       in case in the future we want some walls to be thicker than others for example.  If we want thicker walls
        //       we would have to hardcode those options as well.  Can we automate the calculation of the dimensions?
        //       This would help when it comes time to modify the edge footprints when there are doors.  
        //       We could for instance, shoot a collision ray upward toward the wall from the center of underlying
        //       tiles, and see which collide and that constitutes the dimensions.  

        /// <summary>
        /// Adds and Removes recursively, the visuals and footprint data for a cell edge.
        /// To "remove" a style.ID of -1 is passed in indicating no visual.
        /// </summary>
        /// <param name="edgeID"></param>
        /// <param name="style"></param>
        /// <param name="recurse"></param>
        public void ApplyEdgeSegmentStyle(uint edgeID, EdgeStyle style, bool recurse = true)
        {
            // TODO: why is the map.ModelSelector != null even when we are doing a remove operation? need to investigate this
            // TODO: I don't think we're handling the case of where map.ModelSelector == null where we're trying to REMOVE the wall on the edge.
            //       Because even on removal, we need to recurse the adjacents and subModelIndex should be -1.  
            //       IMPORTANT: we need to know the original model along the edge we're removing, so that we can unapply it's footprint correctly.
            MinimeshMap map;
            MinimeshMap previousMap;
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);

            // TODO: verify edgeID is in bounds or in valid cell!
            // TODO: verify i remove existing map from mEdgeModelMap collection after an edge wall is removed
            bool edgeHasExistingMap = mEdgeModelMap.TryGetValue (edgeID, out previousMap);
            if (edgeHasExistingMap)
                map = previousMap;
            else
            {
                uint floor = edge.Layer;
                map = new MinimeshMap();
                map.Edge = edge;
                map.EdgeLocation = GetEdgeLocation(edge.BottomLeftCell, edge.TopRightCell);
                map.Style = style;
                if (style != null && style.StyleID != -1)
                {
                    string modelSequenceID = GetModelSequenceID(style.Prefab, floor);
                    map.ModelSelector = (ModelSelector)Keystone.Resource.Repository.Get(modelSequenceID);
                }
                map.SubModelIndex = -1;
                map.Model = null;
                map.Minimesh = null;
                map.ElementIndex = -1;
            }

            // delete is indicated by -1 StyleID.  
            if (style == null || style.StyleID == -1)
            {
                // there's nothing pre-existing to delete. just return
                if (!edgeHasExistingMap) return;

                if (map.ModelSelector != null)
                {
                    Vector3i startLocation;
                    // TODO: should deletion of the footprint occur in ApplyEdgeSegmentStyle() overloaded function? along with DeleteWallModelView?
                    int[,] footprint = GetEdgeFootprint(edge, map, out startLocation);

                    // TODO: I think the rotation for the footprint isn't working? or the collision tests performed on rotated Mesh3d is off.
                    // - easy way to test is to just step through the code and verify
                    // todo:  component footprint editor should now allow arbitrary x and z depths.
                    // todo: verify that the add/remove of adjacents when adding/removing a wall edge, correctly updates the footprint tilemask
                    // TODO: verify footprints where the ends are angled, that our mouse picking collides and tests for those cases too
                    //      - we cannot just test the center of the tiles at the ends on those cases.
                    // - TODO: verify footprint gets added even though we're no longer confined to widths/depths of TILES_PER_CELLX x TILES_PER_CELLZ
                    // - TODO: very we can remove the footprints
                    // - TODO: verify footprints get calculated on OUT_OF_BOUND boundaries
                    // - TODO: only compute connectivity after all walls in operation are placed
                    // TODO: when applying footprint, if multiple walls being placed in an operation, we dont
                    // want to recalc the connectivity graph until after the last wall in the operation has been placed.
                    // That probably means we want to update connectivity from the CommandProcessor
                    UnApplyFootprint ((uint)startLocation.X, (uint)startLocation.Y, (uint)startLocation.Z, footprint);

                    DeleteWallModelView(edgeID, map.Style);

                }
                mEdgeModelMap.Remove(edgeID);
            }
            else
            {
                float rotation = 0;
                bool adjacentsExist = false;
                bool onlyParallelsExist = false;

                int subModelIndex = SelectSubModelIndex(edgeID, out rotation, out adjacentsExist, out onlyParallelsExist);

                if (!adjacentsExist)
                {
                    map = ApplyEdgeSegmentStyle(map, subModelIndex, rotation);
                    mEdgeModelMap[edgeID] = map;
                    // no need to recurse, even when removing the edge segment style
                    return;
                }
                else if (onlyParallelsExist)
                {
                    map = ApplyEdgeSegmentStyle(map, subModelIndex, rotation);
                    mEdgeModelMap[edgeID] = map;
                    // no need to recurse, even when removing the edge segment style
                    return;
                }

                map = ApplyEdgeSegmentStyle(map, subModelIndex, rotation);
                mEdgeModelMap[edgeID] = map;
            }

            // recurse for each adjacent
            if (recurse)
            {
                CellEdge[] adjacents = GetAdjacentEdges(edgeID);
                MinimeshMap[] adjacentModelMaps = GetMinimeshMaps(adjacents);

                for (int i = 0; i < adjacentModelMaps.Length; i++)
                    if (adjacentModelMaps[i].ModelSelector != null)
                        ApplyEdgeSegmentStyle(adjacentModelMaps[i].Edge.ID, adjacentModelMaps[i].Style, false);
            }
        }

        // for this to be recursive, we must apply footprint too.  because this is called from script
        // and the script doesn't know what footprint to use for any particular wall edge based on adjacents
        private MinimeshMap ApplyEdgeSegmentStyle(MinimeshMap map, int subModelIndex, float rotation)
        {
            CellEdge edge = map.Edge;
            
            //System.Diagnostics.Debug.WriteLine(string.Format("CelledRegion.ApplyEdgeSegment() - EDGE = {0} X= {1} Z = {2}", edge.ID, edge.Column, edge.Row));

            // TODO: why am i only checking BottomLeftCell and not TopRightCell for being in bounds?
            if (edge.BottomLeftCell != -1) // if not out of interior dimenions completely
            {
                //UnflattenCellIndex((uint)edge.BottomLeftCell, out adjacentX, out adjacentY, out adjacentZ);
                if (map.ModelSelector == null)
                {
                    // TODO: even when removing, we need to Auto-tile the adjacents
                    //       we cannot just do it below if adding a new model to an edge
// TEMP: don't need to remove yet for testing placing of walls                    RemoveWallModelView(edge.ID, false, out existingMap);
                    //RemoveWallModelView(upperEdgeID, false, out existingMap);
                }

                //else if (IsCellInBounds(adjacentX, adjacentY, adjacentZ))
                //{
                //if (mCellMap[adjacentX, adjacentY, adjacentZ] != null)
                //{
     
                if (map.ModelSelector != null) // if we're not just removing the existing, we may be replacing it with different model
                {
                    if (map.ModelSelector.ChildCount - 1 < subModelIndex)
                        subModelIndex = 0; // default

                    map.SubModelIndex = subModelIndex;
                    map.Model = (Model)map.ModelSelector.Children[subModelIndex];
                    map.Minimesh = (MinimeshGeometry)map.Model.Geometry;
                    
                    // TODO: for now, let's just always return the very first wall model and get it added, and get it positioned properly 
                    //       so that we can model in Blender the other wall sub-models for our prefab.
 //                   MinimeshMap currentMap = AutoTileEdgeSegments(edge, true);
                    // TODO: here we pass in a MinimeshMap, but in the following call, we check if there is an existing one
                    //       Why aren't we passing in the MinimeshMap that is passed in to this function?
                    //       I do know why we return the same "map" var that is passed in, it's because the map is a struct
                    //       and changes made to the one passed in, do not affect it (unless we pass by ref)
                    map.ElementIndex = AddWallModelView(map, edge.ID, rotation, map.EdgeLocation);
                    mEdgeModelMap[edge.ID] = map;

                    Vector3i startLocation;
                    int[,] footprint = GetEdgeFootprint(edge, map, out startLocation);



                    // todo:  component footprint editor should now allow arbitrary x and z depths.
                    // todo: verify that the add/remove of adjacents when adding/removing a wall edge, correctly updates the footprint tilemask
                    // TODO: verify footprints where the ends are angled, that our mouse picking collides and tests for those cases too
                    //      - we cannot just test the center of the tiles at the ends on those cases.
                    // - TODO: verify footprint gets added even though we're no longer confined to widths/depths of TILES_PER_CELLX x TILES_PER_CELLZ
                    // - TODO: very we can remove the footprints
                    // - TODO: verify footprints get calculated on OUT_OF_BOUND boundaries
                    // - TODO: only compute connectivity after all walls in operation are placed
                    // TODO: when applying footprint, if multiple walls being placed in an operation, we dont
                    // want to recalc the connectivity graph until after the last wall in the operation has been placed.
                    // That probably means we want to update connectivity from the CommandProcessor
                    ApplyFootprint((uint)startLocation.X, (uint)startLocation.Y, (uint)startLocation.Z, footprint); 
                }
                else
                    System.Diagnostics.Debug.WriteLine("CelledRegion.ApplyEdgeSegment() - ERROR ModelSelector null");
                //}
                //}
            }

            
            //// upper double height wall <-- obsolete?: shouldnt this result in a seperate call here? rather than doing double height here?
            //if (edge.BottomLeftCell != -1) // if not out of interior dimenions completely
            //    AddWallModelView(map.BottomLeftMinimesh, upperEdgeID, edge.Column, edge.Row, edge.Layer + 1, edge.Orientation, true, edge.Location);

            //if (edge.TopRightCell != -1) // if not out of interior dimenions completely
            //    AddWallModelView(map.TopRightMinimesh, upperEdgeID, edge.Column, edge.Row, edge.Layer + 1, edge.Orientation, false, edge.Location);

            // TODO: i don't think i'm including all the proper settings on to the new resulting map such
            //       as the sub-model index used if any, and the minimesh element index
            return map;

        }

        private int SelectSubModelIndex(uint edgeID, out float rotation, out bool adjacentsExist, out bool onlyParallelsExist)
        {
            int subModelIndex = -1;
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);

            // before we can determine which sub-model to use, we need to know
            // which adjacent edges have existing wall models already
            CellEdge[] adjacents = GetAdjacentEdges(edgeID);
            MinimeshMap[] adjacentModelMaps = GetMinimeshMaps(adjacents);

            rotation = GetEdgeRotation(edge.Orientation);
            onlyParallelsExist = true;

            // are there no walls on any adjacents whatsoever? if so, just use the first sub-model and do not recurse
            adjacentsExist = AdjacentWallsExist(adjacentModelMaps);

            if (!adjacentsExist)
            {
                subModelIndex = 0;
                // no need to recurse, even when removing the edge segment style
                return subModelIndex;
            }

            // are there only walls on parallel adjacents? just use the first sub-model and do not recurse
            onlyParallelsExist = true;
            for (int i = 2; i < adjacentModelMaps.Length; i++)
            {
                // note: we iterate starting at index 2 which is where perpendicular adjacent maps are begun to be stored
                if (adjacentModelMaps[i].ModelSelector != null)
                {
                    onlyParallelsExist = false;
                    break;
                }
            }

            if (onlyParallelsExist)
            {
                subModelIndex = 0;
                // no need to recurse, even when removing the edge segment style
                return subModelIndex;
            }


            // are there both parallel and 2 perpendicular adjacents at each end?
            // in other words, are ALL adjacents occupied?
            bool allAdjacentsOccupied = true;
            for (int i = 0; i < adjacentModelMaps.Length; i++)
            {
                if (adjacentModelMaps[i].ModelSelector == null)
                {
                    allAdjacentsOccupied = false;
                    break;
                }
            }

            if (allAdjacentsOccupied)
            {
                subModelIndex = 29;
            }

            // TODO: I'm getting nowhere fast. What if we just have one single-sided wall per edge and we allow overlaps.
            //       Then I think we only need 4 sub-model types not including the one's with cut-outs for doors.
            //       - if we only use material and no textures, we wont have the texture zfighting at the overlaps.
            //          - well we could still use bevels so that adjacents join cleanly and don't overlap.  But the
            //          main benefit is we don't have nearly as many cases of interior/exterior walls to auto-tile... and its
            //          the interior/exterior combinations which seem too complex to deal with.
            //       - another benefit is not having to deal with relative interior/exterior adjacent differences in auto-tiling
            //      - using bevels, this still has problems with auto-tiling, but its much more manageable i think.
            // - WARNING: Does one double-width wall make it harder to do things like filtering cut-away views so you can see inside of rooms from any camera angle?
            //   - we could have a 3rd set of wall meshes that get used (sort of like LOD) when we want cut-away views.  They would
            //     be accessible in the selected autotile sub-model index * 3. (just as we use *2 for doors)

            // TODO: If when auto-tiling the walls, we remove and re-add adjacents, then footprints will always be updated.
            //       That is, when we remove a wall such that an overlapped area gets it footprint removed, when we auto-tile
            //       we could remove and re-add the adjacent and that will update the footprint.

            // TODO: I think we should only have double-width walls even at Out of Bound boundaries.  Just let the
            //       out of bounds part of the wall hang over without a tilemap underneath and be a special case.  Otherwise
            //       it looks weird that outer walls are thinner (if they are just single-width) than interior walls.


            int perpendicularCount = GetPerpendicularAdjacentsCount(adjacents);

            // 4 sided perpendicular 
            if (perpendicularCount == 4)
            {
                bool twoParallels = adjacentModelMaps[0].ModelSelector != null && adjacentModelMaps[1].ModelSelector != null;
                bool parallel0 = adjacentModelMaps[0].ModelSelector != null;
                bool parallel1 = adjacentModelMaps[1].ModelSelector != null;

                subModelIndex = 27;
                if (twoParallels)
                {
                    subModelIndex = 29;
                }
                else if (parallel0)
                {
                    subModelIndex = 28;
                    if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                        rotation += 180;

                }
                else if (parallel1)
                {
                    if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                        rotation += 180;

                    subModelIndex = 28;
                }
            }
            else if (perpendicularCount == 3)
            {
                // 3 sided perpendicular 
                bool twoParallels = adjacentModelMaps[0].ModelSelector != null && adjacentModelMaps[1].ModelSelector != null;
                bool parallel0 = adjacentModelMaps[0].ModelSelector != null;
                bool parallel1 = adjacentModelMaps[1].ModelSelector != null;

                // the single perpendicular is at the destination endpoint
                if (adjacentModelMaps[2].ModelSelector != null && adjacentModelMaps[3].ModelSelector != null)
                {
                    System.Diagnostics.Debug.Assert(adjacentModelMaps[4].ModelSelector == null || adjacentModelMaps[5].ModelSelector == null);

                    // is the single ended perpedicular's origin vertex ID less than or greater than that of the current edge origin or destination
                    if (adjacentModelMaps[4].ModelSelector != null)
                    {
                        if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                        {
                            subModelIndex = 19;
                            if (twoParallels)
                                subModelIndex = 25;
                            else if (parallel0)
                                subModelIndex = 23;
                            else if (parallel1)
                                subModelIndex = 21;
                        }
                        else
                        {
                            subModelIndex = 20;
                            if (twoParallels)
                                subModelIndex = 26;
                            else if (parallel0)
                                subModelIndex = 24;
                            else if (parallel1)
                                subModelIndex = 22;

                            rotation += 180;
                        }
                    }
                    else if (adjacentModelMaps[5].ModelSelector != null)
                    {
                        if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                        {
                            subModelIndex = 20;
                            if (twoParallels)
                                subModelIndex = 26;
                            else if (parallel0)
                                subModelIndex = 24;
                            else if (parallel1)
                                subModelIndex = 22;
                        }
                        else
                        {
                            subModelIndex = 19;
                            if (twoParallels)
                                subModelIndex = 25;
                            else if (parallel0)
                                subModelIndex = 23;
                            else if (parallel1)
                                subModelIndex = 21;

                            rotation += 180;
                        }
                    }
                }
                else // the single perpendicular is here at the origin endpoint
                {
                    System.Diagnostics.Debug.Assert(adjacentModelMaps[4].ModelSelector != null && adjacentModelMaps[5].ModelSelector != null);

                    // is the single ended perpedicular's origin vertex ID less than or greater than that of the current edge origin or destination
                    if (adjacentModelMaps[2].ModelSelector != null)
                    {
                        if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                        {
                            subModelIndex = 19;
                            if (twoParallels)
                                subModelIndex = 25;
                            else if (parallel0)
                                subModelIndex = 21;
                            else if (parallel1)
                                subModelIndex = 23;
                        }
                        else
                        {
                            subModelIndex = 20;
                            if (twoParallels)
                                subModelIndex = 26;
                            else if (parallel0)
                                subModelIndex = 22;
                            else if (parallel1)
                                subModelIndex = 24;
                            rotation += 180;
                        }
                    }
                    else if (adjacentModelMaps[3].ModelSelector != null)
                    {
                        if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                        {
                            subModelIndex = 20;
                            if (twoParallels)
                                subModelIndex = 26;
                            else if (parallel0)
                                subModelIndex = 22;
                            else if (parallel1)
                                subModelIndex = 24;
                        }
                        else
                        {
                            subModelIndex = 19;
                            if (twoParallels)
                                subModelIndex = 25;
                            else if (parallel0)
                                subModelIndex = 21;
                            else if (parallel1)
                                subModelIndex = 23;
                            rotation += 180;
                        }
                    }
                }
            }
            else if (perpendicularCount == 2)
            {
                // 2 sided perpendicular cases at same end
                if (adjacentModelMaps[2].ModelSelector != null && adjacentModelMaps[3].ModelSelector != null)
                {
                    bool parrallel = adjacentModelMaps[0].ModelSelector != null;

                    if (parrallel)
                        subModelIndex = 6;
                    else
                        subModelIndex = 5;

                    if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                        rotation += 180;
                }
                else if (adjacentModelMaps[4].ModelSelector != null && adjacentModelMaps[5].ModelSelector != null)
                {
                    bool parrallel = adjacentModelMaps[1].ModelSelector != null;

                    if (parrallel)
                        subModelIndex = 6;
                    else
                        subModelIndex = 5;

                    if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                        rotation += 180;
                }
                // 2 sided perpendicular cases at opposite ends and on same side 
                else if (adjacentModelMaps[2].ModelSelector != null && adjacentModelMaps[4].ModelSelector != null)
                {
                    bool twoParallels = adjacentModelMaps[0].ModelSelector != null && adjacentModelMaps[1].ModelSelector != null;
                    bool parallel0 = adjacentModelMaps[0].ModelSelector != null;
                    bool parallel1 = adjacentModelMaps[1].ModelSelector != null;

                    if (twoParallels)
                        subModelIndex = 10;
                    else
                    {
                        if (parallel0)
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 8;
                            else
                                subModelIndex = 9;
                        else if (parallel1)
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 9;
                            else
                                subModelIndex = 8;
                        else
                            subModelIndex = 7;
                    }
                }
                else if (adjacentModelMaps[3].ModelSelector != null && adjacentModelMaps[5].ModelSelector != null)
                {
                    bool twoParallels = adjacentModelMaps[0].ModelSelector != null && adjacentModelMaps[1].ModelSelector != null;
                    bool parallel0 = adjacentModelMaps[0].ModelSelector != null;
                    bool parallel1 = adjacentModelMaps[1].ModelSelector != null;

                    if (twoParallels)
                        subModelIndex = 10;
                    else
                    {
                        if (parallel0)
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 9;
                            else
                                subModelIndex = 8;
                        else if (parallel1)
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 8;
                            else
                                subModelIndex = 9;
                        else
                            subModelIndex = 7;
                    }

                    rotation += 180;
                }
                // 2 sided perpendicular case at opposite ends, on opposite sides
                else if (adjacentModelMaps[2].ModelSelector != null && adjacentModelMaps[5].ModelSelector != null)
                {
                    bool twoParallels = adjacentModelMaps[0].ModelSelector != null && adjacentModelMaps[1].ModelSelector != null;
                    bool parallel0 = adjacentModelMaps[0].ModelSelector != null;
                    bool parallel1 = adjacentModelMaps[1].ModelSelector != null;

                    if (twoParallels)
                        if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                            subModelIndex = 14;
                        else
                            subModelIndex = 18;
                    else
                    {
                        if (parallel0)
                        {
                            if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                            {
                                subModelIndex = 12;
                                rotation += 180f;
                            }
                            else
                                subModelIndex = 16;
                        }
                        else if (parallel1)
                        {
                            if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                            {
                                subModelIndex = 13;
                                rotation += 180f;
                            }
                            else
                                subModelIndex = 17;
                        }
                        else
                        {
                            if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                                subModelIndex = 11;
                            else
                                subModelIndex = 15;
                        }
                    }
                }
                else if (adjacentModelMaps[3].ModelSelector != null & adjacentModelMaps[4].ModelSelector != null)
                {
                    bool twoParallels = adjacentModelMaps[0].ModelSelector != null && adjacentModelMaps[1].ModelSelector != null;
                    bool parallel0 = adjacentModelMaps[0].ModelSelector != null;
                    bool parallel1 = adjacentModelMaps[1].ModelSelector != null;

                    if (twoParallels)
                        if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                            subModelIndex = 14;
                        else
                            subModelIndex = 18;
                    else
                    {
                        if (parallel0)
                        {
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 12;
                            else
                                subModelIndex = 17;
                        }
                        else if (parallel1)
                        {
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 13;
                            else
                                subModelIndex = 16;
                        }
                        else
                        {
                            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                                subModelIndex = 11;
                            else
                                subModelIndex = 15;
                        }
                    }
                }
            }
            else if (perpendicularCount == 1)
            {
                // 1 sided perpendicular cases
                if (adjacentModelMaps[2].ModelSelector != null)
                {
                    bool parallel = adjacentModelMaps[0].ModelSelector != null;

                    if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                        if (!parallel)
                            subModelIndex = 2;
                        else
                            subModelIndex = 4;
                    else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                    {
                        if (!parallel)
                            subModelIndex = 1;
                        else
                            subModelIndex = 3;
                        rotation += 180;
                    }
                    else throw new Exception("Unsupported edge orientation");
                }
                else if (adjacentModelMaps[3].ModelSelector != null)
                {
                    bool parallel = adjacentModelMaps[0].ModelSelector != null;

                    if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                        if (!parallel)
                            subModelIndex = 1;
                        else
                            subModelIndex = 3;
                    else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                    {
                        if (!parallel)
                            subModelIndex = 2;
                        else
                            subModelIndex = 4;
                        rotation += 180;
                    }
                    else throw new Exception("Unsupported edge orientation");
                }
                else if (adjacentModelMaps[4].ModelSelector != null)
                {
                    bool parallel = adjacentModelMaps[1].ModelSelector != null;

                    if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                    {
                        if (!parallel)
                            subModelIndex = 1;
                        else
                            subModelIndex = 3;
                        rotation += 180;
                    }
                    else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                        if (!parallel)
                            subModelIndex = 2;
                        else
                            subModelIndex = 4;
                    else throw new Exception("Unsupported edge orientation");
                }
                else if (adjacentModelMaps[5].ModelSelector != null)
                {
                    bool parallel = adjacentModelMaps[1].ModelSelector != null;

                    if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
                    {
                        if (!parallel)
                            subModelIndex = 2;
                        else
                            subModelIndex = 4;
                        rotation += 180;
                    }
                    else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
                        if (!parallel)
                            subModelIndex = 1;
                        else
                            subModelIndex = 3;
                    else throw new Exception("Unsupported edge orientation");
                }
                else throw new Exception("ModelSelector must be NON null at one of the perpendicular ends!");
            }

            int subModelCount = 30; // GetSubModelCount(); adjacentModelMaps[1].ModelSelector.ChildCount;
            // are we filtering cut-away views so we can see into every room?
            // if (filtering)
            //  subModelIndex += subModelCount;

            // is there a door?
            //if (IsDoorExist(edge))
            //{
            //    if (filtering)
            //      subModelIndex += 2 * subModelCount;
            //    else
            //      subModelIndex += 4 * subModelCount;
            //}

            return subModelIndex;
        }

        // when this method is called, we should already know which sub-Model and MinimeshGeometry to use (in other words
        // auto-tile must have already occurred).
        // positions and adds the minimesh element and the MinimeshMap tracking object so that for each CellEdge
        // we know which minimesh elements are assigned to it
        private int AddWallModelView(MinimeshMap map, uint edgeID, float rotationY, CellEdge.EdgeLocation edgeLocation)
        {
            MinimeshGeometry miniMesh = map.Minimesh;
            // TODO: so far it seems the computed position is just added to the minimesh element and nowhere else.
            //       so to do picking, we'd be forced to pick the entire minimesh and not just a manually transformed Model for just this minimesh element
            //       since manually picking a Model hosting a Mesh3d would be faster than picking the entire Minimesh
            Vector3d position = GetEdgePosition(edgeID);

            // NOTE: there is no need to offset the zPos to get both wall halves to line up
            // flush back to back.  The models are exported with their model space origin z component flush with the back of wall model
            // TODO: we must specify the rotation to use based on whether it's an an interior, exterior, and horizontal or vertical wall
            // TODO: .AddElement could include a field for a "tag" where i can create an intenral array that maps
            //       stores the tag that is EdgeID and the minimesh element index so we can later add/remove
            //       elements or flag them as deleted and able to be copied over, etc.
            //       TODO: but it'd be better if i was able to keep the tracking array somewhere else
            //       .. that is not inside the minimesh class
            // ideally 

            double x = CellSize.x / TilesPerCellX;
            Vector3d scale;
            scale.x = 1.0d; // x; // 0.025f;
            scale.y = 1.0d; // 0.025f;
            scale.z = 1.0d; // 0.025f;

            Vector3d rotation;
            rotation.x = 0.0d;
            rotation.y = rotationY;
            rotation.z = 0.0d;


            // remove any existing map using the edgeID as the key
            // TODO: I think in RemoveWallModelView we want to only remove the minimesh element and not the ModelSelector assigned to that map
            //        then any changes to that map, must be restored in the mEdgeModelMap[] dictionary
            MinimeshMap existingMap;
            bool hasExistingMap = RemoveWallModelView(edgeID, out existingMap);

            if (hasExistingMap == false)
            {
                // initialize a new map struct
                // TODO: there should always be an existing map because this call to AddWallModelView() assumes
                //       the ModelSequence for the prefab was created. Or wait... the ModelSequence but not
                //       necessarily the MinimeshMap for this particular Edge.  No, I think the way our 
                //       edge painting works, we always end up creating a new MinimeshMap for the current edge right?
                //       We do recycle the ModelSequence though and we don't need to load the prefab again.
                
                existingMap.EdgeLocation = edgeLocation;
                existingMap.ElementIndex = -1;
            }

            // Add the new visual model to the Minimesh.
            // Note: element index is returned and assigned to the map
            uint index = miniMesh.AddElement(position, scale, rotation);
            return (int)index;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="miniMesh"></param>
        /// <param name="minimeshElementIndex">This is NOT the edge segment ID.  It is the
        /// index that was generated when the Segment was first added to the MinimeshGeometry.</param>
        private void RemoveWallModelView(MinimeshGeometry miniMesh, uint minimeshElementIndex)
        {
            // remove element from Minimesh
            try
            {
                miniMesh.RemoveElement(minimeshElementIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CelledRegion.RemoveWallModelView() ERROR: " + ex.Message);
            }
        }

        private bool RemoveWallModelView(uint edgeID, out MinimeshMap existingMap)
        {
            // TODO: nowhere are we deleting the ModelSelector if the edgeStyle is no longer needed.
            if (mEdgeModelMap.TryGetValue(edgeID, out existingMap))
            {
                if (existingMap.ElementIndex > -1)
                {
                    RemoveWallModelView(existingMap.Minimesh, (uint)existingMap.ElementIndex);
                    existingMap.Minimesh = null;
                    existingMap.ElementIndex = -1;

                    // remove the entire MinimeshMap from the collection
                    mEdgeModelMap.Remove(edgeID);
                }   
                else
                    mEdgeModelMap[edgeID] = existingMap;

                return true;
            }
            return false;
        }


        private bool AdjacentWallsExist(MinimeshMap[] adjacentModelMaps)
        {
            bool result = false;
            for (int i = 0; i < adjacentModelMaps.Length; i++)
            {
                if (adjacentModelMaps[i].ModelSelector != null)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private MinimeshMap[] GetMinimeshMaps(CellEdge[] edges)
        {
            if (edges == null || edges.Length == 0) return null;

            MinimeshMap[] results = new MinimeshMap[edges.Length];

            for (int i = 0; i < edges.Length; i++)
            {
                mEdgeModelMap.TryGetValue(edges[i].ID, out results[i]);
            }

            return results;
        }

        private bool mVisualsLoaded;
        /// <summary>
        /// Loads the floor and ceiling meshes into Models and adds them to the
        /// _selector ModelSequence.  
        /// NOTE: CreateWallModel() is where Segments such as walls, are loaded into
        /// MinimeshGeometry and added to Models which are also then added to the _selector ModelSequence.
        /// </summary>
        private void LoadStructureVisuals()
        {
            // TODO: Our CelledRegion scripts should perhaps do the model sequence creation.
            if (mVisualsLoaded == false)
            {
                string id = GetInteriorElementPrefix(this.ID, typeof(Keystone.Elements.ModelSequence));
                Keystone.Elements.ModelSequence sequence = new Keystone.Elements.ModelSequence(id);
                // TODO: if this is not serializable, how do we store and retreive components like doors
                // reactors, engines,etc? Should only walls, floors and ceilings be Serializable = false?
                sequence.Serializable = false;

                // note: entire sequence is direct models of interior and is NOT added to any quadtree.
                //       this also means that disabling rendering of certain floor meshes requires
                //       we either enable/disable sequences or modify model = Sequence.Select()
                //       to filter the models we dont want
                this.AddChild(sequence);


                //m.SetMaterial(matIndex);

                // we'll use a premade texture atlas
                // Edge Artifacts - If you are using Mimpapping you will need to use GL_NEAREST_MIPMAP_LINEAR 
                // and also duplicate the edge pixels
                // https://developer.nvidia.com/sites/default/files/akamai/tools/files/Texture_Atlas_Whitepaper.pdf
                //Keystone.Appearance.TextureAtlas t = Keystone.Appearance.TextureAtlas.Create(@"F:\Downloads\half_tiles_textures\floor_textures_atlas.png0.dds");
                //t.AddSubTexture(0.0f, 0.0f, 0.0f, 0.125f, 1.0f);
                //t.AddSubTexture(0.125000f, 0.0f, 0.125f, 0.0f, 1.0f);
                //t.AddSubTexture(0.250000f, 0.0f, 0.125f, 0.0f, 1.0f);
                //t.AddSubTexture(0.375000f, 0.0f, 0.125f, 0.0f, 1.0f);
                //t.AddSubTexture(0.500000f, 0.0f, 0.125f, 0.0f, 1.0f);
                //t.AddSubTexture(0.625000f, 0.0f, 0.125f, 0.0f, 1.0f);
                //t.AddSubTexture(0.750000f, 0.0f, 0.125f, 0.0f, 1.0f);
                string path = System.IO.Path.Combine(Core._Core.ModsPath, "caesar\\textures\\floors\\flooratlas.tai");
                // (@"F:\Downloads\half_tiles_textures\floor_textures_atlas.png.tai" 
                AtlasTextureTools.AtlasRecord[] records =
                    AtlasTextureTools.AtlasParser.Parse(path);

                // creation of this node is not being properly locked.  it seems strange this bug is occuring so suddenly now
                // because the window to add this object to the Repository is tiny... but what is happening is
                // while our AssetPlacementTool is instancing this Vehicle, the EditorHud is also cloning the 
                // AssetBrowswer's selected Entity so that it can display the preview version. 
                path = System.IO.Path.Combine(Core._Core.ModsPath, "caesar\\textures\\floors\\flooratlas.tai.dds");

                Keystone.Appearance.TextureAtlas floorTextureAtlas = Appearance.TextureAtlas.Create(path, records);

                // NOTE: Why are these Models and NOT Entities?  Floors and Walls are intrinsic structure
                // of Interior and are not seperate Entities.
                for (int i = 0; i < CellCountY; i++)
                {
                    // NOTE: in the old version, the i iterator matches the level index AND 
                    //       the child index within the ModelSequence node.  Adding "Ceiling" models
                    //       will now mean that the child indices of these models within the 
                    //       ModelSequence will no longer be synchronized 
                    Model floorModel = CreateFloorOrCeilingModel(Interior.PREFIX_FLOOR, floorTextureAtlas, i, false);

                    Keystone.Types.Vector3d position = Keystone.Types.Vector3d.Zero();
                    position.y = this.GetFloorHeight((uint)i);
                    floorModel.Translation = position;
                    sequence.AddChild(floorModel);


                    Model ceilingModel = CreateFloorOrCeilingModel(Interior.PREFIX_CEILING, floorTextureAtlas, i, true);
                    // ceiling is offset slightly below floor because this is the ceiling from the floor below
                    double translationHeightOffset = CEILING_HEIGHT_OFFSET;

                    position.y = this.GetFloorHeight((uint)i) + translationHeightOffset;
                    ceilingModel.Translation = position;
                    sequence.AddChild(ceilingModel);

                    // we always start with every cell in floor or ceiling as collapsed and unpainted with any floor segment
                    for (int j = 0; j < CellCountX; j++)
                        for (int k = 0; k < CellCountZ; k++)
                        {
                            // floor
                            Keystone.Celestial.ProceduralHelper.CellGrid_CellSetCollapseState(this.CellCountX, this.CellCountZ, (uint)j, (uint)k, (Mesh3d)floorModel.Geometry, true);
                            // ceiling
                            Keystone.Celestial.ProceduralHelper.CellGrid_CellSetCollapseState(this.CellCountX, this.CellCountZ, (uint)j, (uint)k, (Mesh3d)ceilingModel.Geometry, true);
                        }
                    // TODO: create ceiling model?
                    //       - if our ceilings simply used identicle texture, we could
                    //         just render them double sided.  
                    //       - if we create an appearance LOD then we could change the atlas texture
                    //         based on the view angle (above or below the floor) and then
                    //         whenever the last appearance has changed we modify the UVs based on
                    //       - floor vs ceiling paint mode in tool? or is there some way where we can
                    //         more easily view the floors from the bottom to handle ceiling painting?
                    //       - so then i DO need to be able to store and restore our painted floor
                    //         atlas indices for both floor and ceiling.  This is part of a cell's
                    //         stored data.
                    //       - we can also tweak the position of the floor so that it matches
                    //         the top of the skirt or the bottom of the skirt depending if we
                    //         are looking at top or bottom of that floor.
                    //         - the skirt doesnt have to move but the floor Y location is tweaked
                    //           to be at top or bottom
                }


                Keystone.IO.PagerBase.LoadTVResource(sequence, true);

                // - floor painting brush.  select level and start painting a floor
                //   - can paint uneven floors by placing ramps that will connect a bottom level to
                //     the very next level above it



                // TODO: CRC32 of the mesh must be tied to the interior.  We cannot allow
                //       exterior mesh to change or the scale of the model or entity to change
                //       once the interior has been created.
                //       Furthermore, this crc32 must be verified upon adding of the interior during
                //       serialization.
                //
                // TODO: problem with above is it doesnt allow to adjust each deck
                // and how would we add/remove decks?  Well adding/removing seems not entirely correct
                // unless adding/removing automatically re-sizes each deck to fit with perhaps
                // some way to apply a buffer value between decks where potentially conduits and tubes
                // and such can be placed.  I'm not sure how that would work exactly except that maybe they'd
                // be sort of like "half decks" that you could plot out as well but which could not contain
                // any components, they are access paths only and plus ventilation/plumbing/etc?
                // AddDeck()
                // AddHalfDeck() AddCrawlSpace()
                // 

                // TODO: the y offset of the floors cannot ever move.  You simply allow for some layers will have
                // no valid interior tiles.  Perhaps it can use those for exterior mounts.  But we will not allow
                // moving of interior layers.  It doesnt really make sense to allow this.
            }
            mVisualsLoaded = true;
        }

        #region obsolete visualizations
        //{   
        //    Keystone.Portals.CelledRegion interior = (Keystone.Portals.CelledRegion)vehicle.Interior;


        //    Keystone.Types.Vector3d cellSize = interior.CellSize;

        //    double height = interior.GetFloorHeight(floor);

        //    uint cellsAcross = (uint)(bounds.Width / cellSize.x);
        //    uint cellsLayers = (uint)(bounds.Height / cellSize.y);
        //    if (cellsLayers == 0) cellsLayers = 1; // minimum of on


        //    // do we simply select a list of edges to apply walls to?
        //    interior.GetEdge(0, 0);

        //    string id = interior.ID + "_Mesh3d_Walls_" + floor.ToString();
        //    Keystone.Elements.Mesh3d.TEST_CreateShipInteriorWallsInSingleMesh(id, template);
        //}

        //public Model TEST_CreateShipInteriorWallsInSingleMesh(string name, uint floor)
        //{
        //    // load a test mesh to clone
        //    string path;
        //    path = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\meshes\";
        //    path = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\FPSCreator_Data\meshbank\scifi\moonbase\rooms\control_room\";
        //    string segmentPath = path + "wall_ALL_a.X"; // "wall_1_x_3.obj";

        //    // note: uses Mesh3d not Minimesh, but in Model we enable Model.UseInstancing = true;
        //    MTV3D65.TVMesh template = CoreClient._CoreClient.Scene.CreateMeshBuilder(segmentPath);
        //    template.LoadXFile(segmentPath, true, true);


        //    uint countX = 35;
        //    uint countZ = 35;

        //    int count = (int)(countX * countZ);


        //    int[] meshList = new int[count];
        //    MTV3D65.TVMesh[] duplicate = new MTV3D65.TVMesh[count];

        //    float xStartOffset = StartX * (float)mSize.x;
        //    float zStartOffset = StartZ * (float)mSize.z;
        //    int k = 0;
        //    for (uint i = 0; i < countX; i++)
        //    {
        //        for (uint j = 0; j < countZ; j++)
        //        {
        //            float halfSizeX = (float)mSize.x * .5f; // only needed if we need to adjust mesh to center of edge
        //            float halfSizeZ = (float)mSize.z * .5f;
        //            float xPos = (i * (float)mSize.x) + xStartOffset;
        //            float zPos = (j * (float)mSize.z) + zStartOffset - halfSizeX;
        //            float yPos = (float)GetFloorHeight(floor) + 0.75f;
        //            //uint index = FlattenCell (i, floor, j);
        //            duplicate[k] = template.Duplicate();

        //            // set the position for each duplicate.
        //            float scale = 0.0125f; // 0.015f;
        //            duplicate[k].SetScale(scale, scale, scale);
        //            duplicate[k].SetPosition(xPos, yPos, zPos);

        //            meshList[k] = duplicate[k].GetIndex(); // this blows, it wont let us use theis
        //            k++;
        //        }
        //    }

        //    MTV3D65.TVMesh result = CoreClient._CoreClient.Scene.MergeMeshesList(count, meshList, true);

        //    // save the result so we can load it i guess
        //    string savePath = System.IO.Path.GetTempPath();
        //    savePath = System.IO.Path.Combine(savePath, System.IO.Path.GetRandomFileName() + ".tvm");
        //    result.SaveTVM(savePath);

        //    Keystone.Appearance.DefaultAppearance appearance;
        //    Mesh3d mesh = Mesh3d.Create(savePath, true, true, out appearance);


        //    string modelID = GetInteriorElementPrefix(this.ID, CelledRegion.PREFIX_WALL, typeof(Model), (int)floor);
        //    Keystone.Elements.Model model = new Keystone.Elements.Model(modelID);

        //    // load and apply a texture
        //    string texturePath = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\textures\W_g_ALL_01_D2.dds";
        //    Keystone.Appearance.Layer texture = Keystone.Appearance.Diffuse.Create(texturePath);

        //    appearance.AddChild(texture);

        //    //Appearance.Material material = Appearance.Material.Create(Keystone.Appearance.Material.DefaultMaterials.matte);
        //    //appearance.AddChild(material);

        //    model.AddChild(mesh);
        //    model.AddChild(appearance);
        //    return model;
        //}

        //// The first test above we use a single Mesh3d merged from duplicates.
        //// This test we'll use a minimesh but NOT with an enable array or matrix arrays
        //// that we have to populate as we find visible items.  This one we'll simply dump to the gpu
        //// 
        //public Model TEST2_CreateShipInteriorWallsInSingleMesh(string name, uint floor, bool doubleSided)
        //{
        //    Model model = CreateWallModel(floor);
        //    MinimeshGeometry miniMesh = (MinimeshGeometry)model.Geometry;


        //    float xStartOffset = StartX * (float)mSize.x;
        //    float zStartOffset = StartZ * (float)mSize.z;
        //    int k = 0;
        //    for (uint i = 0; i < CellCountX; i++)
        //    {
        //        for (uint j = 0; j < CellCountZ; j++)
        //        {
        //            float halfSizeX = (float)mSize.x * .5f; // only needed if we need to adjust mesh to center of edge
        //            float halfSizeZ = (float)mSize.z * .5f;
        //            float xPos = (i * (float)mSize.x) + xStartOffset;
        //            float zPos = (j * (float)mSize.z) + zStartOffset - halfSizeX;
        //            float halfWallHeight = 0.75f;
        //            float yPos = (float)GetFloorHeight(floor) + halfWallHeight;

        //            // set the position for each minimesh element.
        //            float scale = 0.0125f; // 0.015f;
        //            // NOTE: there is no need to offset the zPos to get both wall halves to line up
        //            // flush back to back.  The models are exported with their model space origin z component flush with the back of wall model
        //            miniMesh.AddElement(xPos, yPos, zPos, scale, scale, scale, 0f, 0f, 0f, (uint)k); k++;
        //            if (doubleSided)
        //                miniMesh.AddElement(xPos, yPos, zPos, scale, scale, scale, 0f, 180f, 0f, (uint)k); k++;
        //        }
        //    }

        //    // TODO: is there a way to save this config so that we can reload it from save 
        //    //       easily?
        //    // save the result so we can load it i guess
        //    string savePath = System.IO.Path.GetTempPath();
        //    savePath = System.IO.Path.Combine(savePath, System.IO.Path.GetRandomFileName() + ".tvm");
        //    //result.SaveTVM(savePath);

        //    return model;
        //}
        #endregion - obsolete visualizations


        #endregion


        // TODO: Shouldn't RegisterChild be public for Entity? Wouldn't for example a
        // sword that is AddChild to an actor require REgistering too?
        /// <summary>
        /// Register's an entity with a domain object to the parent so that
        /// the child can apply any modifications to the parent.
        /// </summary>
        /// <param name="child"></param>
        private void RegisterChild(Entities.Entity child)
        {
           // GetEdgeFromFootprint (child.Footprint.Data, )
            // TODO: This entire function should be a script of ship_interior.cs
            // 
            // Then here we can also compute the cellIndices that a child entity/component
            // affects and make the calls to have the child entity modify the map layers
            // it desires.


            // when a certain entity is registered it may have to
            // modify the appropriate map (diffusion, connectivity, etc)

            //
            // when does the child entity get it's cellIndices set?  Maybe that should
            // afterall be in the Register here and let the script do it?
            // Maybe let's not try to save/restore CellIndices at all? 

            // entity.Anchor () ?
            // 

            // armor layers?
            // but we dont need mesh for that just armor stats which get added as layer...

            // take a look at clsBeamWeapon.cls for instance
            // setting the RateOfFire a cyclic rate is computed and then
            // rules are checked as well for 

            // TODO: rather than require a domain object, I should have default placement 
            //       where a centered object is placed halfY above the floor height and centered on x,z 
            //       at the selected cell location.  In fact, when we "Apply" our footprint, we're not even
            //       paying attention to the QueryCellLocation result...
            // domainobject must be initialized before we can query for placement
            // and placement is required to find the cellID
            if (child.Script != null && child.Script.TVResourceIsLoaded)
            {
                //uint x, y, z;

                // TODO: rather than put the entity on a tile, is it possible to put
                // an entity reference on each tile to set which entity is there?
                // if so, how do we deal with add/remove and the changng of references?

                // then when removing the entity, it's footprint areas are used to clear those
                // indices... this way an entity iself needs no reference to an array of tiles it intersects

                // here only tiles with entities need have references to them
                // or any actors, etc


                // http://docs.worldviz.com/vizard/commands/node3d/stencilFunc.htm
                // stencil buffer windows example
                // http://cpntools.org/cpn2000/clipping_in_opengl
                // - with our buckets, can we add target wall segments to a seperate list of minimeshes?
                //   or models?
                // - with our buckets can we add the stencil sources to a seperate bucket?
                // - can our sources apply a tag to the target so an association is created? 
                // - for minimeshes, even with color storing a ref value, i dont think we can change the state
                //   during the loop where the minimesh geometry is being rendered?
                //   - how can i possibly 
                //   - if our minimesh color uses just 1bit to store whether that minimesh element is csg or not
                //   then i can quickly test it and now which walls im rendering or not during any csg pass
                //     - the question then becomes, how do i get the punches to only affect pixels where the zbuffer
                //     value is > it's own

                // NOTE: Calling QueryCellPlacement puts responsibility on Interior or Structure to know
                // how child Components are to be placed within the Interior or Struture.  This removes responsibility
                // from the PlacementTool to know for every variety of potential Entity.  Any snapping to inbetween tiles
                // or center of Cells is done in the script as well.  We just need to also compute the
                // start tile location (bottom left most tile) for the footprint.

                // note: entity.Translation has already been set the first time in AssetPlacementTool which used pick impactPoint
                //       originally and not entity translation!  so the following "QueryCellPlacement" is for SECURITY PURPOSES
                //       in case a modified packet was sent.
                // TODO: if we want to move responsibility of QueryCellPlacement into the ship_interior.css, then we need to know
                //       type of child this is. (chair, door, hatch, engine part, etc).
                // The not so simple question is, elegants wise, where should QueryCellPlacement reside, in the child or the parent?
                // The parent can grab TileLocationFromPoint.  The parent knows what kind of internal organization/structure is used
                // to host any child components.  So it seems the parent should determine cell placement.  The only info the parent
                // needs is the child's component type.... something we didn't want to have to do.  We wanted assetplacementtool to
                // only know based on the directory/folder the component was stored in.  In other words, we didnt want to have to query
                // the child component because we didn't want to have to load the script for that component during assetplacement preview.
                // We could add and assign a property child.CustomType or child.Tag and assign the type there during asset placement...
                // and this way it wont be a custom property but an intrinsic entity property.  Or we can have a mix of the two.
                // QueryCellPlacement goes in the ship_interior.css, but the type of component is child.BrushType.  Or we make
                // child.BrushType an intrinsic property that we can assign when we create the preview source entity in the AssetPlacementTool.
                // - Or, we keep the QueryCellPlacement in the child and that function only gets called here by Interior.  Otherwise
                // a different type of parent knows it doesn't need to call that function.
                // The more I think about this problem, the more it seems that having the AssetPlacementTool load the entity script 
                // for the preview entity is NOT A PROBLEM.  If there is no script, then we simply use the default brush type (single_drop)
                // I just dont see the problem in loading the script during assetplacement.  But we can still use ship_interior.css QueryCellPlacement for the 
                // child entity we want to place.

                // NOTE: This is calling QueryCellPlacement on this Interior.cs instance, NOT the child.
                //       The brushStyle of the child is retrieved from the script. It's up to modders not to abuse this feature
                Vector3d tmpOriginalChildPosition = child.Translation;
                Vector3d[] vecs = (Vector3d[])this.Execute("QueryCellPlacement", new object[] { child.ID, this.ID, child.Translation, child.Rotation.GetComponentYRotationIndex() });
                if (vecs != null && vecs[0] != null)
                    child.Translation = vecs[0];
              //  System.Diagnostics.Debug.Assert (tmpOriginalChildPosition == vecs[0]);
                // entities placed on interior cannot use physics
                // TODO: then during Unregister set it back to it's default which would likely be Dynamic = true;
                child.SetEntityAttributesValue((uint)EntityAttributes.Dynamic, false);


                // http://www.isogenicengine.com/engine/documentation/root/IgeEntity.html

                // occupyTile({Number} x, {Number} y, {Number} width, {Number} height)
                // Ad ds the object to the tile map at the passed tile co-ordinates. 
                // If no tile co-ordinates are passed, will use the current tile position
                // and the tileWidth() and tileHeight() values.
                //{Number}x X co-ordinate of the tile to occupy.
                //{Number}y Y co-ordinate of the tile to occupy.
                //{Number}width Number of tiles along the x-axis to occupy.
                //{Number}height Number of tiles along the y-axis to occupy.

                // unOccupyTile({Number} x, {Number} y, {Number} width, {Number} height)
                // Removes the object from the tile map at the passed tile co-ordinates. 
                // If no tile co-ordinates are passed, will use the current tile position 
                // and the tileWidth() and tileHeight() values.
                //{Number}x X co-ordinate of the tile to un-occupy.
                //{Number}y Y co-ordinate of the tile to un-occupy.
                //{Number}width Number of tiles along the x-axis to un-occupy.
                //{Number}height Number of tiles along the y-axis to un-occupy.

                // overTiles()
                // Returns an array of tile co-ordinates that the object is currently over,
                // calculated using the current world co-ordinates of the object as well as it's 3d geometry.
                // Returns {Array} The array of tile co-ordinates as IgePoint instances.



                // TODO: delete footprint from entity scripts in unregister?
                if (child.Footprint != null)
                {
                    int[,] rotatedFootprint = GetRotatedFootprint(child.Footprint.Data, child.Rotation.GetComponentYRotationIndex());

                    Vector3i startTileLocation;
                    // startTileLocation is the bottom left most tile where the footprint is placed on the tile data
                    Vector3d snap = GetTileSnapPosition(child.Footprint.Data, child.Translation, child.Rotation.GetComponentYRotationIndex(), out startTileLocation);
                    OccupyTiles(child, startTileLocation, rotatedFootprint); //
                    
                    // TODO: what about triggers? they have no obstacle
                    //       footprint but perhaps different kind?  problem there is, triggers should be overlappable and
                    //       currently we dont support that in our tilemasks.
                    //
                    // for each tile occupied by this entity, add it to the tile indexed mEntities lists 
                    // // TODO: we only want to occupy areas where there is collision footprint
                    
                }


                //entity.Scale = vecs[1]; // our current wall entities have super tiny scales, we dont actually want to modify those. And even so, those tiny (or whatever) scales should be done on the Model not the Entity
                if (vecs != null && vecs.Length == 3)
                    child.Rotation = new Quaternion(vecs[2].y * Utilities.MathHelper.DEGREES_TO_RADIANS, vecs[2].x * Utilities.MathHelper.DEGREES_TO_RADIANS, vecs[2].z * Utilities.MathHelper.DEGREES_TO_RADIANS);

                // TODO: determine if this child spans multiple cells
                //       - since walls, floors/ceilings which are the most numerous now
                //       are NOT seperate entities, we've dramatically decreased the need to have
                //       entities assigned to any one tile.  Now we can have a list of entities
                //       each at an index in that list, and then an "entities" layer that stores
                //       in that layer, their index as footprint so that we can know which
                //       entity is there by checking the entity index stored on that layer
                //       and perhaps we can get away with the lower resolution layer there...
                //       we'll see.
                //       - the idea is to make queries faster when we need to find all entities that
                //       exist in a certain area.  
                //       - or perhaps entities exist in a quadtree as we've done in the past.
                //       - it certainly makes for faster picking of entities than trying to throught a list
                //       and meanwhile our walls and floors use the older technique of edge and tile indices

                // TODO: is register and initialize all that different?
                // YES.  In one simple way... Initialize() script method is called once for all shared instances
                // of DomainObject, but OnAddedToParent() is called for each unique Entity instance when it's
                // added to a parent Entity (eg. a CelledRegion \ Interior)
                object result; // TODO: rename RegisterChild to "OnChildAdded")
                result = Execute("RegisterChild", new object[] { this.ID, child.ID });
                // TODO: "OnAddedToParent" in door.css the door will already have it's punch sub model with flag
                //       CSGStencil set, however the door must now specifically notify that it needs to be assigned to
                //       Segment's for the edges it affects _and_ then those segments must disable their minimesh and create
                //       a proxy that it adds to a CSGTarget list of models under their own _csgTargetsSequence ModelSequence
                //       node.  When/If the door is removed, it notifies again and the proxy model can be removed and the
                //       minimesh element can be re-enabled (or grid quad uncollapsed)
                //
                //       anymore now that they are added as "structure" and rendered with minimeshes.  Perhaps we
                //       disassociate those walls from the minimesh structure and make them as sub-models
                //       of the overall "door" using selector nodes?
                result = child.Execute("OnAddedToParent", new object[] { child.ID, this.ID });

                // TODO: when do our rules check for valid placement.  i cant remember?
                // it should create an error reply to send back too yes?
                // TODO: Note that only during build do we need to validate build rules because
                // our build is networked... (unless we allow offline building)
                // But assuming builds are all networked then validation is done server side at that time
                // and server maintains it's own saved copy.  It never has to rely on the user
                // sending a saved file and then loading it and validating it after the fact.
                // Character designers in other games require online conneciton.
                // brushes allowed
                // type (wall segment, csg door/wall, ceiling/floor, csg ladder/hatch)
                // mount locations (wall, floor, ceiling, counter (eg computer must be on counter)
                // 
                // uint mountLocations = (uint)entity.DomainObject.Execute("QueryMountLocations", null);

                
            }
            else
                System.Diagnostics.Debug.WriteLine("Interior.RegisterChild() - NO ENTITY SCRIPT FOUND for child type: '" + child.TypeName + "' childID: " + child.ID);
        }

        private void UnregisterEntity(Entities.Entity entity)
        {
            object result;
            if (this.Script != null)
                result = Execute("UnRegisterChild", new object[] { this.ID, entity.ID });

            if (entity.Script != null && entity.Script.TVResourceIsLoaded)
            {
                if (entity.Footprint != null)
                {
                    int[,] rotatedFootprint = GetRotatedFootprint(entity.Footprint.Data, entity.Rotation.GetComponentYRotationIndex());

                    Vector3i startTileLocation;
                    GetTileSnapPosition(entity.Footprint.Data, entity.Translation, entity.Rotation.GetComponentYRotationIndex(), out startTileLocation);
                    UnOccupyTiles(entity, startTileLocation, rotatedFootprint);
                }

                if (this.Script != null)
                    // TODO: door.css should do things like removing csg flags/associations when it's removed.
                    result = entity.Execute("OnRemovedFromParent", new object[] { entity.ID, this.ID });
            }

            // TODO: be nice to debug draw the bitfields for each layer
            // since afterall, we want to be able to sketch the bitflags for electric conduits and 
            // ventilation systems, fuel lines, etc.
            // essentially i would iterate thru the flags and place a "decal" at every location
            // that is set... is that fastest way to do it?
            //((CelledRegionNode)mSceneNode).UpdateVolumes();

        }
        
       

        public ModelSelector ModelSelector 
        {
            get 
            {
                return _sequence;
            }
        }

        public Model[] SelectModel(SelectionMode pass, double distance)
        {
            if (_sequence != null)
            {
                return _sequence.Select(pass, this, distance);
            }
            else if (_model == null)
                return null;
            else
                return new Model[] { _model };
        }


        public virtual void AddChild(Model model)
        {
            // Ensure only one instance of Selector _OR_ one Model is allowed, but not one of both
            if (_model != null && _sequence != null)
                throw new Exception("Model or Selector node already exists.  Only one node of either type allowed.");

            _model = model;
            AddChild((Node)_model);
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
        }

        public void AddChild(ModelSelector sequenceNode)
        {
            // Ensure only one instance of Selector _OR_ one Model is allowed, but not one of both
            if (_model != null && _sequence != null)
                throw new Exception("Model or Selector node already exists.  Only one node of either type allowed.");

            // See notes at LoadStructureVisuals() where this AddChild() method is called.
            _sequence = sequenceNode;
            AddChild((Node)_sequence);
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
        }

        public override void AddChild(Keystone.Entities.Entity child)
        {
            base.AddChild(child);

            //  skip register if this is a HUD element
            if ((child.Attributes & EntityAttributes.HUD) != EntityAttributes.HUD && TVResourceIsLoaded)
            {
                System.Diagnostics.Debug.Assert(TVResourceIsLoaded);
                // TODO: should RegisterEntity return bool if successful? Registration succeeded? (eg it can fail if it's a hack attempt)
                RegisterChild(child); // what if the child's domainobject is not deserialized yet?
                                      // we should be notified of this via change notifications and then
                                      // we must register.
            }
        }

        public override void RemoveChild(Node child)
        {
            if (child is Entity && (((Keystone.Entities.Entity)child).Attributes & EntityAttributes.HUD) != EntityAttributes.HUD)
                UnregisterEntity((Keystone.Entities.Entity)child); // unregister occurs first because we use reverse order in RemoveChild from when added

            base.RemoveChild(child);

            if (child is ModelSelector)
            {
                _sequence = null;
                // TODO: is change flag necessary?  because I think the remove recurse will
                // propogate up the ultimate removal of geometry?
                SetChangeFlags(Enums.ChangeStates.GeometryRemoved | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            }
            else if (child is Model)
            {
                _model = null;
                // TODO: is change flag necessary?  because I think the remove recurse will
                // propogate up the ultimate removal of geometry?
                SetChangeFlags(Enums.ChangeStates.GeometryRemoved | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            }
        }

        /// <summary>
        /// CelledRegion's have hidden spatial division so we need to determine not only
        /// if a child entity has left this region, but if it needs to be moved to a new
        /// Cell (and potentially Volume) within this CelledRegion.
        /// </summary>
        /// <param name="child"></param>
        /// <param name="newParentRegion"></param>
        /// <returns></returns>
        internal override bool ChildEntityBoundsFail(Keystone.Entities.Entity child, out Region newParentRegion)
        {
            newParentRegion = this; // initialize

            // TODO: handle movement between cells

            // TODO: but when is the physics check done?
            
            // TODO: for moving Entities Is this updating the position of the child within the quadtree collection if it moves but stays within the same region/zone?

            // TODO: When this is called, we are not updating the footprint position or Occupy/UnOccupy tiles for this entity.


            bool value = base.ChildEntityBoundsFail(child, out newParentRegion);
            //System.Diagnostics.Debug.WriteLine("Interior.cs.ChildEntityBoundsFail() - " + child.TypeName + " - " + value);

            // typically returns False if has not left the boundaries of this Region.
            return value;
        }


        /// <summary>
        /// Called by CellSceneNode.OnEntityMoved() to test tilemap for trigger and collision
        /// and where appicable, raise events in child scripts between objects that collide or activate trigger areas in the tilemap
        /// </summary>
        /// <param name="entity"></param>
        internal void Collide(Entity entity)
        {
            Vector3d previousTranslation = entity.PreviousTranslation;
            Vector3d translation = entity.Translation;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// ModelSpace Ray Collision with respect to this CelledRegion.  
        /// This means the Interior Decks/Floors .y component hitpoints ARE at the correct 
        /// relative altitudes for that Deck/Floor.  
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="regionMatrix"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override Keystone.Collision.PickResults Collide(Keystone.Types.Vector3d start, Keystone.Types.Vector3d end, KeyCommon.Traversal.PickParameters parameters)
        {
        	// TODO: would it be better if CelledRegion was treated as a sort of invisible and permeable divisible space
        	//       that was represented by a GEOMETRY node?  This way it could also be attached to any Region that wanted to have collisions for it's interior space
            Vector3d intersectionPoint;
            PickResults pickResult = new PickResults();
            pickResult.HasCollided = false;

            // TODO: normally i can update the current result through the picker
            //       but here i cannot because the iteration of the internal structure is done
            //       here and not in the picker...maybe if i could supply a callback here
            //       TODO: be nice if there were a way to use that callback though.. so we dont
            //       have to do any of that type of appropriate or best evaluations here

            // does the pick parameters specify we should search floors?
            if ((parameters.Accuracy & PickAccuracy.Tile) == PickAccuracy.Tile)
            {
                const double RAY_SCALE = 100000; // picking an interior assumes camera will be relatively close to it and that the interior is not "huge"
                Vector3d dir = Vector3d.Normalize(end - start);
                Ray r = new Ray(start, dir);

                bool hit = false;
                int floor = parameters.FloorLevel;

                // if the selected floor is picked, then we can deduce which
                // tile was picked from the "intersectionPoint" instead of iterating through every tile on the floor
                // like we used to do in the old version of this function.
                Vector3d[] floorPoints = GetFloorVertices((uint)floor);

                // polygon.Intersect because it provides an intersectionPoint
                // which we need to determine closest vertex and closest edge
                hit = Polygon.Intersects(r, RAY_SCALE, floorPoints, false, out intersectionPoint);

                // FIXED: Update the intersectionPoint.y values to always be perfect for this "hit" floor.
                //      This fixes issues with attempting to add components to an OctreeOctant
                double altitude = GetFloorHeight (floor);
                System.Diagnostics.Debug.Assert (altitude == floorPoints[0].y);
                intersectionPoint.y = altitude; // = floorPoints[0].y
                
                if (hit)
                {
                	// All collisions are done in model space
                    if ((parameters.Accuracy & PickAccuracy.Tile) == PickAccuracy.Tile)
                    {
                    	pickResult.HasCollided = true;

                        // TODO: here we should test if the accuracy here has met more conditions than the previous
                        //       TODO: if there exists a floor tile, a ceiling tile above, or wall on the edge
                        //       then we should pick the walls and see if they are closer since
                        //       visually, users will expect to be able to pick a wall visually and not just from the edge location
                        //       Performance wise, edge is still fastest but it allows us also to narrow our search to the edge and any adjacents
                        //       
                        pickResult.CollidedObjectType = parameters.Accuracy;

                        
                        // convert the "intersectionPoint" to i, k, j 0 based indices
                        // NOTE: no "reversal" of Z is needed because our cells are not stored in an image, but in an array
                        uint j = (uint)(((mCellSize.x * CellCountX / 2f) + intersectionPoint.x) / mCellSize.x);
                        if (j >= CellCountX) j = CellCountX - 1;
                        uint k = (uint)(((mCellSize.z * CellCountZ / 2f) + intersectionPoint.z) / mCellSize.z);  
                        if (k >= CellCountZ) k = CellCountZ - 1;

                        // if cell j, i, k is out of bounds of max possible interior dimensions this should NOT report a hit
                        // this is the only filter we do here, rest is responsibility of Picker.cs
                        if (mCellMap.CellIsInBounds(j, (uint)floor, k) == false)
                            return pickResult;


                        //System.Diagnostics.Debug.WriteLine(string.Format("{0}, {1}, {2} impact point.", intersectionPoint.x, intersectionPoint.y, intersectionPoint.z));

                        // flatten the j, i, k (x,y,z) to a cellIndex so we can store this in our pickresults
                        uint flattenedCellArrayIndex = FlattenCell(j, (uint)floor, k); // i = x, k = y, j = z
						pickResult.FaceID = (int)flattenedCellArrayIndex;
                        //pickResult.FaceID = (int)i; // faceID is floor index by default, gets updated to cellID if .Cell accuracy is used


                        // OBSOLETE - switching to TileMap.Structure has made  CelldRegion obsolete
 //                       pickResult.TileLocation = TileLocationFromPoint(intersectionPoint.x, y, intersectionPoint.z);
                       // pickResult.FaceID = (int)FlattenCoordinate((uint)pickResult.TileLocation.X, (uint)pickResult.TileLocation.Y, (uint)pickResult.TileLocation.Z, 
                       //                                     CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ); 
                     //   System.Diagnostics.Debug.Assert(pickResult.TileLocation.Y == floor);


                        // since the collision is done in ModelSpace (aka LocalSpace), the intersectionPoint should be already also
                        pickResult.ImpactPointLocalSpace = intersectionPoint;
                        pickResult.ImpactNormal = -dir;

                        Vector3i cellLocation;
                        cellLocation.X = (int)j;
                        cellLocation.Y = floor;
                        cellLocation.Z = (int)k;
                        // NOTE: For CelledRegion.Collide() "pickResult.CellLocation" is used not .TileLocation
                        //       but for TileMap.Structure.Collide(), the TileLocation depends on the MapLayer name and
                        //       different map layers can have different resolutions.  
                        pickResult.CellLocation = cellLocation;
                        // note: k does in fact == z 
                        // likewise i == y and j == x
                        
#if DEBUG
                        uint testX, testY, testZ;
                        // verify unflatted produces same results
                        UnflattenCellIndex (flattenedCellArrayIndex, out testX, out testY, out testZ);
                        
                        System.Diagnostics.Debug.Assert(j == testX && floor == testY && k == testZ);
#endif

                        // get the polypoints of this cell
                        Vector3d[] polyPoints = GetCellVertices(j, (uint)floor, k); // returns 4 verts because we want to be able to pick any part of quad, not just triangle
                        pickResult.FacePoints = polyPoints;
                        
                        // use the impact point to find the closest corner
                        uint closestCornerIndex = 0;
                        double dist = double.MaxValue;
                        for (uint n = 0; n < polyPoints.Length; n++)
                        {
                            //
                            double newDist = Vector3d.GetDistance3dSquared(intersectionPoint, polyPoints[n]);
                            if (newDist < dist)
                            {
                                closestCornerIndex = n;
                                dist = newDist;
                            }
                        }

                        pickResult.TileVertexIndex = (int)closestCornerIndex;
                        pickResult.VertexID = GetVertexID(flattenedCellArrayIndex, closestCornerIndex);
                        
                        // compute the closest edge
                        // TODO: if nearest the center, then it's center edge
                        // NOTE: there is only one center edge and it has one
                        // of two orientations. This is elegant as it allows us
                        // still 1:1 edges everywhere and not some special case
                        // couple that both travel through center of the tile.
                        // either corner 0 to 2 or 1 to 3 (lowest corner id always
                        // first)
                        Line3d[] edges = new Line3d[4];
                        edges[0] = new Line3d(polyPoints[0], polyPoints[1]);
                        edges[1] = new Line3d(polyPoints[1], polyPoints[2]);
                        edges[2] = new Line3d(polyPoints[2], polyPoints[3]);
                        edges[3] = new Line3d(polyPoints[3], polyPoints[0]);

                        int edgeIndex = FindClosestEdge(edges, intersectionPoint);

                        uint originID, destID;
                        switch (edgeIndex)
                        {
                            case 0:
                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 0);
                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 1);
                                break;
                            case 1:
                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 1);
                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 2);
                                break;
                            case 2:
                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 2);
                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 3);
                                break;
                            default:
                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 3);
                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 0);
                                break;
                        }

                        if (destID < originID)
                        {
                            uint t = destID;
                            destID = originID;
                            originID = t;
                        }
                        
                        CellEdge tmp = CellEdge.CreateEdge(originID, destID, CellCountX, CellCountY, CellCountZ);
                        pickResult.EdgeID = (int)tmp.ID;
                        pickResult.EdgeOrigin = edges[edgeIndex].Point[0];
                        pickResult.EdgeDest = edges[edgeIndex].Point[1];
                        
                        // BEGIN - Perform 3D Pass to resolve wall vs floor collision
                        // - is there a wall on any of the 4 edges?  if not, set EdgeID = -1
                        // TODO: this method of picking will not fix the problem unless the camera angle is 
                        // fixed at a relatively steep angle.  The closer to deck horizon the camera gets,
                        // the more walls we can pick before every intersecting the floor itself with our pick ray.
                        List<Model> models = new List<Model>();
                        for (int i = 0; i < 4; i++)
                        {
                        //	  originID = (uint)GetVertexID(flattenedCellArrayIndex, i);
                        //    int n =  i < 3 ? i + 1 : 0;
                        //    destID = (uint)GetVertexID(flattenedCellArrayIndex, n);	
                        //    orientation = i == 0 || i == 2 ? CellEdge.EdgeOrientation.Vertical : CellEdge.EdgeOrientation.Horizontal ;
                        // 
                        //	  tmp = CellEdge.CreateEdge(originID, destID, CellCountX, CellCountY, CellCountZ, orientation);
                        //    
                        //	  MinimeshMap existingMap;
                        //	  if (mEdgeModelMap.TryGetValue(edgeID, out existingMap))
                        //    {
                        //			// recall mEdgeModelMap is MinimeshMap instance and contains references to Models and Geometry
                        //			// whereas Segment only contains a SegmentStyle that has resourceIDs only, no object refs.  Here
                        //          // either can be used to determine if a wall is at that location
                        //			
                        //			// add to list of models in pickResult so that Picker.cs traverser can perform 
                        //			if (existingMap.BottomLeftModel != null)
                        //				models.Add (existingMap.BottomLeftModel);
                        //			if (existingMap.TopRightModel != null)
                        //				models.Add (existingMap.TopRightModel);
                        //			
                    	//	  }
                        }
                        //
                       	// - our HUD can add a second instance of the renderable to the pipeline making this one x% larger and
                       	//   rendered before the scene, OR we can modify the Material to make the selected wall brighter
                       	// 		- TODO: good time to make a "Selected" HUD material changer for our SelectionTool
                       	//  - http://www.gamedev.net/topic/280596-how-to-render-highlight-model-with-edges-on-it/
                       	// - http://rbwhitaker.wikidot.com/toon-shader
                       	// - http://gamedev.stackexchange.com/questions/34652/outline-object-effect
                       	// - http://www.flipcode.com/archives/Object_Outlining.shtml <-- uses stencil buffer
                       	// http://www.codeproject.com/Articles/94817/Pixel-Shader-for-Edge-Detection-and-Cartoon-Effect  - sobel filter
                       	// - http://www.gamedev.net/topic/524102-d3d10-single-pass-outline-rendering/#entry4402201 - single pass shader option using inner lines
                        // END - Perform 3D Pass 
                    }

                    //System.Diagnostics.Debug.WriteLine ("Pick distance to cell = "+ _lastPickResult.DistanceSquared.ToString ());
                    //System.Diagnostics.Debug.WriteLine("Closest edgeID = " + tmp.ID.ToString());
                    // System.Diagnostics.Debug.WriteLine("Face = " + pickResult.FaceID.ToString());
                    //break; // exit for loop on first find? or keep iteratng for closer deck?
                    
                }
            }

            return pickResult;
        }



        //    /// <summary>
        //    /// ModelSpace Ray Collision
        //    /// </summary>
        //    /// <param name="start"></param>
        //    /// <param name="end"></param>
        //    /// <param name="regionMatrix"></param>
        //    /// <param name="parameters"></param>
        //    /// <returns></returns>
        //    public override Keystone.Collision.PickResults Collide(Keystone.Types.Vector3d start, Keystone.Types.Vector3d end, Keystone.Types.Matrix regionMatrix, Keystone.Collision.PickParameters parameters)
        //    {
        //        Vector3d intersectionPoint;
        //        PickResults pickResult = new PickResults();
        //        pickResult.HasCollided = false;

        //        // TODO: normally i can update the current result through the picker
        //        //       but here i cannot because the iteration of the internal structure is done
        //        //       here and not in the picker...maybe if i could supply a callback here
        //        //       TODO: be nice if there were a way to use that callback though.. so we dont
        //        //       have to do any of that type of appropriate or best evaluations here

        //        // does the pick parameters specify we should search floors?
        //        if ((parameters.Accuracy & PickAccuracy.Deck) == PickAccuracy.Deck)
        //        {
        //            const double RAY_SCALE = 100000; // picking an interior assumes camera will be relatively close to it and that the interior is not "huge"
        //            Vector3d dir = Vector3d.Normalize(end - start);
        //            Ray r = new Ray(start, dir);

        //            bool hit = false;

        //            Vector3d halfDimension = mCellSize * .5;

        //            // iterate through floors - would be some advantages if i could iterate through the visual structure
        //            // and not just the logical internal structure
        //            for (uint i = 0; i < CellCountY; i++) // Y iterator is first
        //            {

        //                // -1 parameters.Floor indicate we search all floors
        //                if (parameters.Floor != -1)
        //                {
        //                    // always skip floors greater than the specified .Floor
        //                    if (i > parameters.Floor) continue;
        //                }

        //                double y = (StartY * mCellSize.y) + (i * mCellSize.y); // y uses iterator k which is first


        //                // if the selected floor is picked, then we can deduce which
        //                // tile was picked instead of iterating through every tile on the floor
        //                // like we used to do in the old version of this function.
        //                Vector3d[] floorPoints = new Vector3d[4];



        //                // NOTE: Default DirectX winding order is CLOCKWISE vertices for
        //                // front facing.  XNA also uses clockwise for front facing.
        //                // based on current cell, compute the polypoints of the cell
        //                // THUS 
        //                // 1 ___ 2
        //                // |    |
        //                // 0 ___ 3
        //                // is our layout
        //                floorPoints[0].x = StartX * mCellSize.x;
        //                floorPoints[0].y = y; 
        //                floorPoints[0].z = StartZ * mCellSize.z;
        //                // -halfDimension will translate to the bottom corner 
        //                // of the cell (ie vertex 0 of the floor face)
        //                floorPoints[0] -= halfDimension;


        //                floorPoints[1].x = floorPoints[0].x;
        //                floorPoints[1].y = floorPoints[0].y;
        //                floorPoints[1].z = floorPoints[0].z + (mCellSize.z * CellCountZ);

        //                floorPoints[2].x = floorPoints[0].x + (mCellSize.x * CellCountX);
        //                floorPoints[2].y = floorPoints[0].y;
        //                floorPoints[2].z = floorPoints[1].z;

        //                floorPoints[3].x = floorPoints[2].x;
        //                floorPoints[3].y = floorPoints[0].y;
        //                floorPoints[3].z = floorPoints[0].z;


        //                // polygon.Intersect because it provides an intersectionPoint
        //                // which ineed to determine closest vertex and closest edge
        //                hit = Polygon.Intersects(r, RAY_SCALE, floorPoints, false, out intersectionPoint);

        //                if (hit)
        //                {
        //                    Vector3d impactPoint = Vector3d.TransformCoord(intersectionPoint, regionMatrix);
        //                    double distanceSquared = Vector3d.GetDistance3dSquared(Vector3d.TransformCoord(start, regionMatrix), impactPoint);
        //                    if (pickResult.DistanceSquared < distanceSquared) continue;




        //                    if ((parameters.Accuracy & PickAccuracy.Cell) == PickAccuracy.Cell)
        //                    {
        //                        // convert the impact point to i, k, j 0 based indices
        //                        uint j = (uint)(((mCellSize.x * CellCountX / 2f) + intersectionPoint.x) / mCellSize.x);
        //                        if (j >= CellCountX) j = CellCountX - 1;
        //                        uint k = (uint)(((mCellSize.z * CellCountZ / 2f) + intersectionPoint.z) / mCellSize.y);
        //                        if (k >= CellCountZ) k = CellCountZ - 1;

        //                        // if this j, i, k are out of bounds this should NOT report a hit
        //                        if (mCellMap.CellIsInBounds(j, i, k) == false)
        //                            continue;

        //                        // TODO: i think problem here is for placing components, we dont want to require floor here
        //                        //       because it prevents us from rendering the invalid location because the pickResult is not
        //                        //       filled properly.  This means we really should be returning the "pick" and then using IsChildPlaceable
        //                        //       code to determine if the picked location is ok.
        //                        bool cellFloorRequired = (parameters.Accuracy & PickAccuracy.CellWithFloor) != PickAccuracy.None;
        //                        // if we're picking any cell (not just the cell's with floors), then we will skip any floor
        //                        // that is not the current
        //                        if (parameters.Floor != i && cellFloorRequired == false)
        //                            continue; 

        //                        // if this cell is collapsed, depending on game\build mode, we may want to NOT report a hit
        //                        // really this is all about pick parameters because during build mode to uncollapse a cell 
        //                        // to place it in bounds or place a floor, collapsed must be pickable but when in game
        //                        // mode, we may want to test the floor beneath next
        //                        if (cellFloorRequired && mCellMap.CellHasFloor(j, i, k) == false)
        //                           continue;


        //                        //System.Diagnostics.Debug.WriteLine(string.Format("{0}, {1}, {2} impact point.", intersectionPoint.x, intersectionPoint.y, intersectionPoint.z));

        //                        // flatten the j, i, k (x,y,z) to a cellIndex so we can store this in our pickresults
        //                        uint flattenedCellArrayIndex = FlattenCell(j, i, k, CellCountX, CellCountY, CellCountZ); // i = x, k = y, j = z

        //                        uint testX, testY, testZ;
        //#if DEBUG
        //                        // verify unflatted produces same results
        //                        UnflattenCellIndex(flattenedCellArrayIndex, CellCountX, CellCountZ, out testX, out testY, out testZ);

        //                        // note: k does in fact == z 
        //                        // likewise i == y and j == x
        //                        System.Diagnostics.Debug.Assert(j == testX && i == testY && k == testZ);
        //#endif
        //                        // get the polypoints of this cell
        //                        Vector3d[] polyPoints = new Vector3d[4]; // four because we want to be able to pick any part of quad, not just triangle
        //                        float x = (StartX * (float)mCellSize.x) + (j * (float)mCellSize.x);
        //                        float z = (StartZ * (float)mCellSize.z) + (k * (float)mCellSize.z); 

        //                        // NOTE: Default DirectX winding order is CLOCKWISE vertices for
        //                        // front facing.  XNA also uses clockwise for front facing.
        //                        // based on current cell, compute the polypoints of the cell
        //                        // THUS 
        //                        // 1 ___ 2
        //                        // |    |
        //                        // 0 ___ 3
        //                        // is our layout
        //                        polyPoints[0].x = x;
        //                        polyPoints[0].y = y; 
        //                        polyPoints[0].z = z;
        //                        // -halfDimension will translate to the bottom corner 
        //                        // of the cell (ie vertex 0 of the floor face)
        //                        polyPoints[0] -= halfDimension;

        //                        polyPoints[1].x = polyPoints[0].x;
        //                        polyPoints[1].y = polyPoints[0].y;
        //                        polyPoints[1].z = polyPoints[0].z + mCellSize.z;
        //                        // NOTE: no need to add offset since we're using polyPoints[0]'s values
        //                        // which already takes those into account
        //                        //polyPoints[1] += mCellOriginOffset;

        //                        polyPoints[2].x = polyPoints[0].x + mCellSize.x;
        //                        polyPoints[2].y = polyPoints[0].y;
        //                        polyPoints[2].z = polyPoints[1].z;
        //                        // NOTE: no need to add offset since we're using 0 and 1's values
        //                        // which already takes those into account
        //                        //polyPoints[2] += mCellOriginOffset;

        //                        polyPoints[3].x = polyPoints[2].x;
        //                        polyPoints[3].y = polyPoints[0].y;
        //                        polyPoints[3].z = polyPoints[0].z;
        //                        // NOTE: no need to use offset since we're using 0 and 2's values
        //                        // which already takes those into account
        //                        //polyPoints[3] += mCellOriginOffset;

        //                        // use the impact point to find the closest corner
        //                        uint closestCornerIndex = 0;
        //                        double dist = double.MaxValue;
        //                        for (uint n = 0; n < polyPoints.Length; n++)
        //                        {
        //                            //
        //                            double newDist = Vector3d.GetDistance3dSquared(intersectionPoint, polyPoints[n]);
        //                            if (newDist < dist)
        //                            {
        //                                closestCornerIndex = n;
        //                                dist = newDist;
        //                            }
        //                        }

        //                        pickResult.FaceVertexIndex = (int)closestCornerIndex;
        //                        pickResult.FaceID = (int)flattenedCellArrayIndex;
        //                        pickResult.VertexID = GetVertexID(flattenedCellArrayIndex, closestCornerIndex);

        //                        // compute the closest edge
        //                        // TODO: if nearest the center, then it's center edge
        //                        // NOTE: there is only one center edge and it has one
        //                        // of two orientations. This is elegant as it allows us
        //                        // still 1:1 edges everywhere and not some special case
        //                        // couple that both travel through center of the tile.
        //                        // either corner 0 to 2 or 1 to 3 (lowest corner id always
        //                        // first)
        //                        Line3d[] edges = new Line3d[4];
        //                        edges[0] = new Line3d(polyPoints[0], polyPoints[1]);
        //                        edges[1] = new Line3d(polyPoints[1], polyPoints[2]);
        //                        edges[2] = new Line3d(polyPoints[2], polyPoints[3]);
        //                        edges[3] = new Line3d(polyPoints[3], polyPoints[0]);

        //                        int edgeIndex = FindClosestEdge(edges, intersectionPoint);
        //                        CellEdge.EdgeOrientation orientation;
        //                        if (edgeIndex == 0 || edgeIndex == 2)
        //                            orientation = CellEdge.EdgeOrientation.Vertical;
        //                        else
        //                            orientation = CellEdge.EdgeOrientation.Horizontal;

        //                        uint originID, destID;
        //                        switch (edgeIndex)
        //                        {
        //                            case 0:
        //                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 0);
        //                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 1);
        //                                break;
        //                            case 1:
        //                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 1);
        //                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 2);
        //                                break;
        //                            case 2:
        //                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 2);
        //                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 3);
        //                                break;
        //                            default:
        //                                originID = (uint)GetVertexID(flattenedCellArrayIndex, 3);
        //                                destID = (uint)GetVertexID(flattenedCellArrayIndex, 0);
        //                                break;
        //                        }

        //                        if (destID < originID)
        //                        {
        //                            uint t = destID;
        //                            destID = originID;
        //                            originID = t;
        //                        }
        //                        CellEdge tmp = CellEdge.CreateEdge(originID, destID, CellCountX, CellCountY, CellCountZ, orientation);
        //                        pickResult.EdgeID = (int)tmp.ID;
        //                        pickResult.EdgeOrigin = edges[edgeIndex].Point[0];
        //                        pickResult.EdgeDest = edges[edgeIndex].Point[1];
        //                        pickResult.FacePoints = polyPoints;


        //                        int tileX, tileZ;
        //                        Keystone.Celestial.ProceduralHelper.MapImpactPointToPixelCoordinate(
        //                            (float)CellSize.x * CellCountX,
        //                            (float)CellSize.z * CellCountZ,
        //                            CellCountX * TilesPerCellX,
        //                            CellCountZ * TilesPerCellZ,
        //                            (float)intersectionPoint.x, (float)intersectionPoint.z,
        //                            out tileX, out tileZ);

        //                        Vector3i tileLocation;
        //                        tileLocation.X = tileX;
        //                        tileLocation.Y = (int)i; 
        //                        // TODO: im confused why the following z reverse must be done here when 
        //                        // MapImpactPointToPixelCoordinate is supposed to have done the conversion 
        //                        // from texture to tilemap indices for us.  Really confusing.  
        //                        // Actually, the reason I think is because we're mapping it to texture pixel coords
        //                        // but we don't need that.  We just want tile coords.
        //                        tileLocation.Z = Math.Abs(tileZ - (int)CellCountZ * (int)TilesPerCellZ + 1);

        //                        pickResult.TileLocation = tileLocation;

        //                        pickResult.HasCollided = true;
        //                        // TODO: here we should test if the accuracy here has met more conditions than the previous
        //                        //       
        //                        pickResult.CollidedObjectType = parameters.Accuracy;
        //                        pickResult.SetEntity(this);

        //                        // since the collision is done in local space, the intersectionPoint should be already also
        //                        pickResult.ImpactPointLocalSpace = intersectionPoint;
        //                        pickResult.ImpactNormal = -dir;

        //                        // TODO: verify this DistanceSquared computation is correct.  I think it is since it seems correct in Actor3d.cs and Mesh3d.cs where we convert model space back to region space and find distance squared
        //                        pickResult.DistanceSquared = distanceSquared;
        //                        pickResult.Matrix = regionMatrix;
        //                        //pickResult.FaceID = (int)i; // faceID is floor index by default, gets updated to cellID if .Cell accuracy is used

        //                    }

        //                    //System.Diagnostics.Debug.WriteLine ("Pick distance to cell = "+ _lastPickResult.DistanceSquared.ToString ());
        //                    //System.Diagnostics.Debug.WriteLine("Closest edgeID = " + tmp.ID.ToString());
        //                    // System.Diagnostics.Debug.WriteLine("Face = " + pickResult.FaceID.ToString());
        //                    //break; // exit for loop on first find? or keep iteratng for closer deck?
        //                }
        //            }
        //        }                  

        //        return pickResult;
        //    }

        /// <summary>
        /// Find's the center position of a tile that contains the passed in coord
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public Vector3d TileCenterPositionFromPoint(Vector3d coord)
        {
            Vector3d interval = new Vector3d(TileSize.x, TileSize.y, TileSize.z);
            Vector3d snap = Utilities.MathHelper.Snap(coord, interval);

            Vector3d center = snap;
            // NOTE: Snap() leaves the Y axis alone.
            center.x += TileSize.x / 2d;
            center.z += TileSize.z / 2d;
            //// TODO: we dont always want this because the coord may be intended
            ////       to lie inbetween tiles, but we're forcing on the following line
            ////       to find a tileLocation from that coord.  And consider if the coord
            ////       is directly on the border, how do we choose which TileLocation to use?
            ////       It's rather arbitrary to just pick the first tile that contains the coord
            ////       on it's border for each X and Z axis
            //Vector3i tileLocation = TileLocationFromPoint(coord);
            //Vector3d halfTile = TileSize * 0.5d;
            //Vector3d center;

            //// NOTE: TileStartX and TileStartZ already start at the centers of their respective boundary tiles, so no halfTile offset is needed.
            //center.x = TileStartX + (tileLocation.X * TileSize.x); // + halfTile.x;
            //center.y = coord.y;
            //center.z = TileStartZ + (tileLocation.Z * TileSize.z); // + halfTile.z;

            return center;
        }

        // TODO: here we're forcing to find a TileLocaction from a coord when that coord
        //       may be intended to straddle Tiles on one or both X and Z axis.  What we really
        //       care about is the bottom, left most tile where the footprint starts.
        //       But it is ok for "link" painting one tile at a time for things like electrical, networking, plumbing, ducts, etc.
        public Vector3i TileLocationFromPoint(Vector3d coord)
        {
            return TileLocationFromPoint(coord.x, coord.y, coord.z);
        }

        // TODO: if floor is just vector.y where the .y is not actually the floor but
        // a point in 3d space which looks to find the floor, then this will be wrong!
        // TODO: however from now on we should really always be returning real points in 3d interior space
        //      and not using a special "floor" value that is a corruption of our entity.Translation value
        // and which is just plain old confusing.  stick to one simple real coordinate system
        // todo; i need to verify the above
        public Vector3i TileLocationFromPoint(double x, double y, double z)
        {
            int tileX, tileZ;
            Keystone.Celestial.ProceduralHelper.MapImpactPointToTileCoordinate(
                (float)CellSize.x * CellCountX,
                (float)CellSize.z * CellCountZ,
                CellCountX * TilesPerCellX,
                CellCountZ * TilesPerCellZ,
                (float)x, (float)z,
                out tileX, out tileZ);

            int floor = GetFloorFromAltitude(y);

            Vector3i tileLocation;
            tileLocation.X = tileX;
            tileLocation.Y = floor;
            tileLocation.Z = tileZ;

            return tileLocation;
        }

        public Vector3i CellLocationFromPoint(double x, double y, double z)
        {
            int cellX, cellZ;
            Keystone.Celestial.ProceduralHelper.MapImpactPointToTileCoordinate(
                (float)CellSize.x * CellCountX,
                (float)CellSize.z * CellCountZ,
                CellCountX,
                CellCountZ,
                (float)x, (float)z,
                out cellX, out cellZ);

            int floor = GetFloorFromAltitude(y);

            Vector3i tileLocation;
            tileLocation.X = cellX;
            tileLocation.Y = floor;
            tileLocation.Z = cellZ;

            return tileLocation;
        }

        public uint CellIndexFromPoint(Vector3d v)
        {
            return CellIndexFromPoint(v.x, v.y, v.z);
        }

        public uint CellIndexFromPoint(double x, double y, double z)
        {
            Vector3i location = CellLocationFromPoint(x, y, z);
            uint index = FlattenCell((uint)location.X, (uint)location.Y, (uint)location.Z);
            return index;
        }

        public Vector3i CellLocationFromTileLocation(uint tileX, uint floor, uint tileZ)
        {
            Vector3i result;
            result.X = -1;
            result.Y = (int)floor;
            result.Z = -1;

            if (floor > CellCountY - 1)
            {
                result.Y = -1;
                return result;
            } 

            result.X = (int)(tileX / TilesPerCellX);
            if (result.X > CellCountX - 1)
            {
                result.X = -1;
                return result;
            }

            result.Z = (int)(tileZ / TilesPerCellZ);
            if (result.Z > CellCountZ - 1)
            {
                result.Z = -1;
                return result;
            }
            return result;
        }

        public int CellIndexFromTileLocation(uint tileX, uint floor, uint tileZ)
        {
            Vector3i tmp = CellLocationFromTileLocation(tileX, floor, tileZ);
            uint index = FlattenCell((uint)tmp.X, (uint)tmp.Y, (uint)tmp.Z);
            return (int)index;
        }

        public bool IsCellCollapsed(uint index)
        {
            uint x, y, z;
            UnflattenCellIndex(index, out x, out y, out z);

            string modelID = Keystone.Portals.Interior.GetInteriorElementPrefix(this.ID, Keystone.Portals.Interior.PREFIX_FLOOR, typeof(Keystone.Elements.Model), (int)y);

            Keystone.Elements.Model model = (Keystone.Elements.Model)Keystone.Resource.Repository.Get(modelID);
            if (model == null) return true;

            Keystone.Elements.Mesh3d mesh = (Keystone.Elements.Mesh3d)model.Geometry;


            bool result = Celestial.ProceduralHelper.CellGrid_IsCellCollapsed(CellCountX, CellCountZ, x, z, mesh);


            return result;
        }

        public int GetFloorFromAltitude(double height)
        {
            const double epsilon = 0.05;
            // transform the celledRegion local space height to 0 based floor height where floor 0 is lowest floor
            double min = (StartY * mCellSize.y) - mCellSize.y / 2d;
            double max = (StopY * mCellSize.y) + mCellSize.y / 2d;

            double ratioY = Utilities.InterpolationHelper.LinearMapValue(min, max, height);
            ratioY = 1.0 - ratioY;
            double transformedY = ratioY * CellCountY;
            transformedY += epsilon; // required to ensure values like 0.99999999999543 don't Math.Floor() == 0 when it's close enough to be 1.
            
            int result = (int)Math.Floor(transformedY);

            //System.Diagnostics.Debug.Assert(result <= CellCountY, "CelledRegion.GetFloorFromAltitude() - Floor exceeds CellCountY");
            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="floor">0 is lowest floor to arbitrary high</param>
        /// <returns>The altitude of this deck or floor at it's lowest point (the GROUND.  NOT halfway between ground and ceiling)</returns>
        public double GetFloorHeight(uint floor)
        {
            // always add the mCellOriginOffset.y to make sure we're at center and then deduct halfHeight
            // to compute floor.  Do not assume that we are already on the floor because that assumption
            // is only true on floorplans that have an even number of total floors
            double halfHeight = mCellSize.y * .5d;
            double result = ((StartY + floor) * mCellSize.y) - halfHeight;
            return result;
        }        

        public Vector3d GetCellCenter(uint x, uint y, uint z)
        {
            Vector3d result;
            Vector3d halfDimension = CellSize * .5d;

            result.x = (StartX + x) * CellSize.x;
            result.z = (StartZ + z) * CellSize.z;
            result.y = (StartY + y) * CellSize.y;

            return result;
        }

        public Vector3d GetCellCenter(uint cellID)
        {
            uint x, y, z;
            UnflattenCellIndex(cellID, out x, out y, out z);
            return GetCellCenter(x, y, z);
        }

        /// <summary>
        /// Retrieves the center position of a Tile in 3d world space coordinates.
        /// NOTE: The .y component is the center of the tile within the Cell so you must
        /// subtract half cellheight to place the .y component on the floor of that deck/level.
        /// </summary>
        /// <param name="x">tile X location using 0 based location indices</param>
        /// <param name="y">deck index using 0 based indices</param>
        /// <param name="z">Tile Z location using 0 based location indices</param>
        /// <returns></returns>
        public Vector3d GetTileCenter(uint x, uint y, uint z)
        {
            Vector3d result;
            Vector3d halfDimension = TileSize * .5d;

            result.x = (TileStartX + x) * TileSize.x;
            result.z = (TileStartZ + z) * TileSize.z;
            result.y = (StartY + y) * mCellSize.y; // this is  halfway between ground and ceiling.  Need to deduct halfDimension.y to find the point on ground

            return result;
        }

        public Vector3d GetTileCenter(Vector3i location)
        {
            return GetTileCenter((uint)location.X, (uint)location.Y, (uint)location.Z);
        }

        /// <summary>
        /// Retrieves the 4 vertex coordinates of a Tile in 3d world space coordinates.
        /// NOTE: The .y component is actually on the ground and NOT halfway between ground and ceiling.
        /// </summary>
        /// <param name="x">tile X location using 0 based location indices</param>
        /// <param name="y">deck index using 0 based indices</param>
        /// <param name="z">Tile Z location using 0 based location indices</param>
        /// <returns></returns>
        public Vector3d[] GetTileVertices(uint x, uint y, uint z)
        {
            Vector3d[] polyPoints = new Vector3d[4]; // four because we want to be able to pick any part of quad, not just triangle
            Vector3d halfDimension;

            halfDimension = TileSize * .5d;
            
            // fX, fZ, fY holds the center position in 3d world coords of the tile at x,y,z 0 based location indices
            double fX = (TileStartX + x) * TileSize.x;
            double fZ = (TileStartZ + z) * TileSize.z;
            double fY = (StartY + y) * mCellSize.y;

            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front facing.  XNA also uses clockwise for front facing.
            // based on current cell, compute the polypoints of the cell
            // THUS 
            // 1 ___ 2
            // |    |
            // 0 ___ 3
            // is our layout
            polyPoints[0].x = fX;
            polyPoints[0].y = fY;
            polyPoints[0].z = fZ;
            // -halfDimension will translate to the bottom corner 
            // of the cell (ie vertex 0 of the floor face)
            polyPoints[0] -= halfDimension;


            polyPoints[1].x = polyPoints[0].x;
            polyPoints[1].y = polyPoints[0].y;
            polyPoints[1].z = polyPoints[0].z + halfDimension.z;
            // NOTE: no need to add offset since we're using polyPoints[0]'s values
            // which already takes those into account
            //polyPoints[1] += mCellOriginOffset;

            polyPoints[2].x = polyPoints[0].x + halfDimension.x;
            polyPoints[2].y = polyPoints[0].y;
            polyPoints[2].z = polyPoints[1].z;
            // NOTE: no need to add offset since we're using 0 and 1's values
            // which already takes those into account
            //polyPoints[2] += mCellOriginOffset;

            polyPoints[3].x = polyPoints[2].x;
            polyPoints[3].y = polyPoints[0].y;
            polyPoints[3].z = polyPoints[0].z;
            // NOTE: no need to use offset since we're using 0 and 2's values
            // which already takes those into account
            //polyPoints[3] += mCellOriginOffset;

            return polyPoints;
        }

        /// <summary>
        /// Retreives the 4 vertices that comprise a cell at 0 based Cell index location x, y, z
        /// NOTE: The .y component is actually on the ground and NOT halfway between ground and ceiling.
        /// </summary>
        /// <param name="x">Cell location at X 0 based index</param>
        /// <param name="y">Deck location at 0 based index</param>
        /// <param name="z">Cell location at Z 0 based index</param>
        /// <returns></returns>
        public Vector3d[] GetCellVertices(uint x, uint y, uint z)
        {
            Vector3d[] polyPoints = new Vector3d[4]; // four because we want to be able to pick any part of quad, not just triangle
            Vector3d halfDimension;
            halfDimension = mCellSize * .5d;

            // fX, fZ, fY hold the center positions of the cell at 0 based location index x, y, z
            double fX = (StartX + x) * (float)mCellSize.x;
            double fZ = (StartZ + z) * (float)mCellSize.z;
            double fY = (StartY + y) * mCellSize.y;

            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front facing.  XNA also uses clockwise for front facing.
            // based on current cell, compute the polypoints of the cell
            // THUS 
            // 1 ___ 2
            // |    |
            // 0 ___ 3
            // is our layout
            polyPoints[0].x = fX; 
            polyPoints[0].y = fY;
            polyPoints[0].z = fZ;
            // -halfDimension will translate to the bottom corner 
            // of the cell (ie vertex 0 of the floor face)
            polyPoints[0] -= halfDimension;


            polyPoints[1].x = polyPoints[0].x;
            polyPoints[1].y = polyPoints[0].y;
            polyPoints[1].z = polyPoints[0].z + mCellSize.z;
            // NOTE: no need to add offset since we're using polyPoints[0]'s values
            // which already takes those into account
            //polyPoints[1] += mCellOriginOffset;

            polyPoints[2].x = polyPoints[0].x + mCellSize.x;
            polyPoints[2].y = polyPoints[0].y;
            polyPoints[2].z = polyPoints[1].z;
            // NOTE: no need to add offset since we're using 0 and 1's values
            // which already takes those into account
            //polyPoints[2] += mCellOriginOffset;

            polyPoints[3].x = polyPoints[2].x;
            polyPoints[3].y = polyPoints[0].y;
            polyPoints[3].z = polyPoints[0].z;
            // NOTE: no need to use offset since we're using 0 and 2's values
            // which already takes those into account
            //polyPoints[3] += mCellOriginOffset;

            return polyPoints;
        }

        /// <summary>
        /// Returns quad vertices of an entire deck or floor.
        /// NOTE: The resulting .y component is actually on the ground and NOT halfway between ground and ceiling.
        /// </summary>
        /// <param name="floor">0 based floor index</param>
        /// <returns></returns>
        public Vector3d[] GetFloorVertices(uint floor)
        {
            Vector3d halfDimension = mCellSize * .5;
            double y = (StartY + floor) * mCellSize.y; // y uses iterator k which is first
            Vector3d[] floorPoints = new Vector3d[4];


            // Here we get quad verts of entire floor, NOT just a single cell.
            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front facing.  XNA also uses clockwise for front facing.
            // based on current cell, compute the polypoints of the cell
            // THUS 
            // 1 ___ 2
            // |    |
            // 0 ___ 3
            // is our layout
            floorPoints[0].x = StartX * mCellSize.x;
            floorPoints[0].y = y;
            floorPoints[0].z = StartZ * mCellSize.z;
            // -halfDimension will translate to the bottom corner 
            // of the floor (ie vertex 0 of the floor face)
            floorPoints[0] -= halfDimension;


            floorPoints[1].x = floorPoints[0].x;
            floorPoints[1].y = floorPoints[0].y;
            floorPoints[1].z = floorPoints[0].z + (mCellSize.z * CellCountZ);

            floorPoints[2].x = floorPoints[0].x + (mCellSize.x * CellCountX);
            floorPoints[2].y = floorPoints[0].y;
            floorPoints[2].z = floorPoints[1].z;

            floorPoints[3].x = floorPoints[2].x;
            floorPoints[3].y = floorPoints[0].y;
            floorPoints[3].z = floorPoints[0].z;

            return floorPoints;
        }

        public uint FlattenCell(uint x, uint y, uint z)
        {
            return Utilities.MathHelper.FlattenCoordinate(x, y, z, CellCountX, CellCountZ);
        }

        public uint FlattenTile(uint x, uint y, uint z)
        {
            return Utilities.MathHelper.FlattenCoordinate(x, y, z, CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ);
        }
                
        public void UnflattenCellIndex(uint index, out uint x, out uint y, out uint z)
        {
            // TODO: if the index is out of range this fails!  i should get rid of all uint
            Utilities.MathHelper.UnflattenIndex(index, CellCountX, CellCountZ, out x, out y, out z);
        }

        public void UnflattenTileIndex(uint index, out uint x, out uint y, out uint z)
        {
            // TODO: if the index is out of range this fails!  i should get rid of all uint
            Utilities.MathHelper.UnflattenIndex(index, CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ, out x, out y, out z);
        }


        /// <summary>
        /// Returns an Edge's rotation in Degrees.
        /// </summary>
        /// <param name="orientation"></param>
        /// <returns></returns>
        /// <remark>
        /// NOTE: we do not need rotations for 180 and 270 because an Edge's Top or Bottom or Left or Right
        ///  half is irrelevant because they are relative to the Cells they are adjacent to.
        ///  Thus, there is only vertical and horizontal. 0 or 90 degree rotation.
        ///  </remark>
        private float GetEdgeRotation(CellEdge.EdgeOrientation orientation)
        {
            float rotation = 0;
            if (orientation == CellEdge.EdgeOrientation.Vertical)
            {
                rotation = 90f;
            }

            return rotation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="edgeID"></param>
        /// <returns>pitch, yaw, roll in Radians</returns>
        public Vector3d GetEdgeRotation(uint edgeID)
        {
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);

            if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
            {
                double rotationRadians = 90d * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS; // MathHelper.DEGREES_TO_RADIANS
                return new Vector3d(0, rotationRadians, 0);
            }

            return new Vector3d(0d, 0d, 0d);
        }

        /// <summary>
        /// Using picking/collision, finds the center position in 3d world space of the edge within the deckplan.
        /// Also outputs the edgeID and the rotation of the edge in radians.
        /// </summary>
        /// <param name="positionLocalSpace"></param>
        /// <param name="edgeID"></param>
        /// <param name="rotationRadians"></param>
        /// <returns></returns>
        public Vector3d GetEdgePosition(Vector3d positionLocalSpace, out int edgeID, out Vector3d rotationRadians)
        {
            PickParameters parameters = new PickParameters();
            parameters.FloorLevel = GetFloorFromAltitude(positionLocalSpace.y);
            parameters.Accuracy = PickAccuracy.Tile | PickAccuracy.Face | PickAccuracy.Edge;

            Vector3d pos = Vector3d.Zero();
            Vector3d start = positionLocalSpace;
            start.y += 1;
            PickResults results = Collide(start, positionLocalSpace, parameters);
            edgeID = results.EdgeID;

            if (edgeID == -1)
            {
                rotationRadians.x = 0;
                rotationRadians.y = 0;
                rotationRadians.z = 0;
                return pos;
            }

            CellEdge edge = CellEdge.CreateEdge((uint)edgeID, CellCountX, CellCountY, CellCountZ);

            byte cellRotation = GetSegmentRotationAngleIndex(results.FaceID, edge);
            const double TO_DEGREES = 360d / 256d;

            double rotationY = cellRotation * TO_DEGREES;

            Vector3i cellLocation = results.CellLocation;
            Vector3d halfSize = CellSize * .5;

            pos = GetEdgePosition((uint)edgeID);
            // our door component's Y = 0 is at the floor and not the center of the door
            // so we must move it down from the middle of the Cell.
            // NOTE: actually above comment is wrong.  The edge Y returned from GetEdgePosition is already at FloorHeight so we
            //       do not need to move pos.y lower.
            //pos.y -= halfSize.y;
            rotationRadians.y = rotationY; // * Utilities.MathHelper.DEGREES_TO_RADIANS;
            rotationRadians.x = 0;
            rotationRadians.z = 0;
            return pos;
        }


        /// <summary>
        /// Finds the center position in 3d world space of the edge within the deckplan.
        /// This way a Model or Mesh can be positioned there.
        /// </summary>
        /// <param name="edgeID"></param>
        /// <param name="edgeColumnX"></param>
        /// <param name="edgeRowZ"></param>
        /// <param name="floor"></param>
        /// <param name="orientation"></param>
        /// <returns>Edge center position in 3d world space</returns>
        public Vector3d GetEdgePosition(uint edgeID)
        {
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            CellEdge.EdgeOrientation orientation = edge.Orientation;

            double xStartOffset = StartX * mCellSize.x;
            double zStartOffset = StartZ * mCellSize.z;

            double halfSizeX = mCellSize.x * .5d; // only needed if we need to adjust mesh to center of edge
            double halfSizeZ = mCellSize.z * .5d;
            double halfWallHeight = mCellSize.y * 0.5d;


            Vector3d result;
            result.x = 0d;
            result.z = 0d;
            result.y = GetFloorHeight(edge.Layer); // + halfWallHeight; // halfWallHeight isn't needed because our walls are modeled with Y bottom = 0 so it's already on the floor.

            // set the position for each minimesh element.
            if (orientation == CellEdge.EdgeOrientation.Horizontal)
            {
                result.x = (edge.Column * mCellSize.x) + xStartOffset;
                result.z = (edge.Row * mCellSize.z) + zStartOffset - halfSizeZ; // center on z
            }
            else if (orientation == CellEdge.EdgeOrientation.Vertical)
            {
                result.x = (edge.Column * mCellSize.x) + xStartOffset - halfSizeX; // center on x
                result.z = (edge.Row * mCellSize.z) + zStartOffset;
            }

            return result;
        }

        /// <summary>
        /// Because edge vertices lay between inner tiles, we return the bottom, left most
        /// tile location (which happens to be the lowest tile location in terms of 
        /// coordinates.  If that location is out of bounds on either side (left and bottom)
        /// it returns the next closest tile.
        /// </summary>
        /// <param name="vertexID">Cell vertex ID, not inner tile ID</param>
        /// <returns></returns>
        private Keystone.Types.Vector3i VertexIDToLocation(uint vertexID)
        {
            Keystone.Types.Vector3i result;

            CellEdge.EdgeStatistics es = CellEdge.GetEdgeStatistics(CellCountX, CellCountY, CellCountZ);

            result.Y = (int)(vertexID / es.VerticesPerDeck);

            uint deckSpaceVertexID = (uint)(vertexID - (es.VerticesPerDeck * result.Y));
            result.X = (int)(deckSpaceVertexID % es.VertexCountX);
            result.Z = (int)(deckSpaceVertexID / es.VertexCountX);

            result.X *= (int)TilesPerCellX;
            result.Z *= (int)TilesPerCellZ;
            // NOTE: result.X, Y and Z are in deckspace coordinates, not global within all decks and thats because we have a Y coordinate that differentiates
            return result;
        }


        // TODO: an edge technically sits between tiles (and cells).  So how am I choosing
        //       which TileLocation to return?  TopRight or BottomLeft tile?  
        public Vector3i GetTileLocationFromEdgeID(uint edgeID, out Vector3d edgePosition)
        {
            Vector3i tileLocation;
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);

            edgePosition = GetEdgePosition(edgeID);

            // tileLocation starts at x = 0 z = 0 like a texture.  But edgePosition is in coordinates where
            // the middle of the floor is origin.  So we need to first convert this edgePosition to same coordinates
            // as our tiles.
            Vector3d halfSize;
            halfSize.x = CellCountX * CellSize.x / 2d;
            halfSize.z = CellCountZ * CellSize.z / 2d;
            halfSize.y = 0;

            edgePosition += halfSize;

            tileLocation.X = (int)(edgePosition.x / TileSize.x);
            tileLocation.Z = (int)(edgePosition.z / TileSize.z);
            tileLocation.Y = (int)edge.Layer;

            return tileLocation;
        }

        /// <summary>
        /// </summary>
        /// <param name="edge"></param>
        /// <returns>Bottom Left most tile where the footprint for this edge starts.</returns>
        private Vector3i GetTileLocation(CellEdge edge)
        {
            Vector3i result;
            
            result.X = (int)(edge.Column * TilesPerCellX);
            result.Y = (int)edge.Layer;
            result.Z = (int)(edge.Row * TilesPerCellZ);

#if DEBUG
            Vector3i tmp = VertexIDToLocation(edge.Origin);
            System.Diagnostics.Debug.Assert(tmp.Y == edge.Layer);
            System.Diagnostics.Debug.Assert(tmp.X == result.X);
            System.Diagnostics.Debug.Assert(tmp.Z == result.Z);
#endif 

            // we should rename this function to GetEdgeFootprint(edge)
            // and return proper footprint bottom/left most tile location
            // and given that we use fixed size walls such that it's only 2 tiles thick.
            // If for example, it were 4 tiles thick, the bottom/left most tile would be
            // different.
            // 
            if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
            {
                if (edge.BottomLeftCell == -1)
                {
                }
                else
                {
                } 
            }
            else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
            {
            }
            else throw new Exception("Edge orientation not supported.");

            return result;
        }

        /// <summary>
        /// Returns a footprint array for the edge based on the subModelIndex of the Mesh element on that edge
        /// and takes into account the orientation of that edge (vertical or horizontal)
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="startLocation"></param>
        /// <returns></returns>
        /// <remarks> uses ray collisions to determine the footprint so that edges with door openings are computed correctly as well</remarks>
        private int[,] GetEdgeFootprint(CellEdge edge, MinimeshMap map, out Vector3i startLocation)
        {
            int[,] result;

            System.Diagnostics.Debug.Assert(map.ModelSelector != null);

            Model model = map.Model;
            MinimeshGeometry geometry = (MinimeshGeometry)model.Geometry;
           
            Mesh3d mesh = geometry.Mesh3d;

            Vector3i tileLocation = GetTileLocation(edge);
            Vector3d start, end;
            const double START_EPSILON = 0.1d;
            const double END_EPSILON = 1.52d;

            // our collision test must be performed in model space so we must create a transformation matrix to convert world space ray to model space
            Vector3d position = geometry.GetElementPosition((uint)map.ElementIndex);
            Vector3d rotation = geometry.GetElementRotation((uint)map.ElementIndex);
            Matrix translationMatrix = Matrix.CreateTranslation(position);
            Matrix rotationMatrix = Matrix.CreateRotationY(rotation.y * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS);
            Matrix matrix = rotationMatrix * translationMatrix;
            Matrix regionSpaceToModelSpaceTransform = Matrix.Inverse(matrix);

            PickParameters parameters = new PickParameters
            {
                Accuracy = PickAccuracy.Face,
                PickPassType = PickPassType.Collide,
                T0 = 0,
                T1 = END_EPSILON,
                SearchType = PickCriteria.Closest,
                SkipBackFaces = true
            };

            if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
            {
                tileLocation.X -= 1;
                tileLocation.Z -= 1; // initialize the Z to start the opposite side of the boundary
                startLocation = tileLocation;

                // first we test the extended locations adjacent to edge.origin boundary.  
                // There is left, right and middle.  If any of those three result in
                // positive collision, we include tiles that extend the width of the edge.
                // Second we do the same thing with the edge.Destination boundary.
                // Finally we test the main edge array dimensions to test for things
                // like doors.
                // TODO: can't we just do one set of pick's using the widest possible extents of the footprint?
                // Then our footprint can overlap, but the ends will contain no data if they are not used.
                const int TILE_DEPTH = 2;
                result = new int[TilesPerCellX + 2, TILE_DEPTH]; //  why is this TilesPerCellX + 2 added? I think its because even in the case of 16x2 wall model, we allow for 18x2 to cover the overlap for when some of our models do require overlapping based on wall adjacency
            }
            else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
            {
                const int TILE_WIDTH = 2;
                result = new int[TILE_WIDTH, TilesPerCellZ + 2 ]; // why is this TilesPerCellZ + 2 added? I think its because even in the case of 16x2 wall model, we allow for 18x2 to cover the overlap for when some of our models do require overlapping based on wall adjacency
                tileLocation.Z -= 1;
                tileLocation.X -= 1; // initialize the X to start on the opposite side of the boundary
                startLocation = tileLocation;

                // first we test the extended locations adjacent to edge.origin boundary.  
                // There is left, right and middle.  If any of those three result in
                // positive collision, we include tiles that extend the width of the edge.
                // Second we do the same thing with the edge.Destination boundary.
                // Finally we test the main edge array dimensions to test for things
                // like doors.
                // TODO: can't we just do one set of pick's using the widest possible extents of the footprint?
                // Then our footprint can overlap, but the ends will contain no data if they are not used.
            }
            else throw new NotImplementedException("Diagonal edges not supported.");


            for (int i = 0; i <= result.GetUpperBound(0); i++)
            {
                for (int j = 0; j <= result.GetUpperBound(1); j++)
                {
                    Vector3d tilePosition = GetTileCenter((uint)tileLocation.X + (uint)i, (uint)tileLocation.Y, (uint)tileLocation.Z + (uint)j);
                    // TODO: modified the following line of code from -= 1.5 to -= CellSize.y / 2.0d.  verify this didn't break anything.  CellSize.y I think has a value of 3.0d so should be same
                    tilePosition.y -= CellSize.y / 2.0d; // place Y back at the deck level instead of floating in the middle of the tile
                    start = tilePosition;
                    start.y -= START_EPSILON;

                    Ray regionSpaceRay = new Ray(start, Vector3d.Up());

                    // NOTE: the pick start and end parameters need to be converted to ModelSpace since mesh.AdvancedCollide() is run in modelspace
                    Ray msRay = regionSpaceRay.Transform(regionSpaceToModelSpaceTransform);
                    end = msRay.Origin;
                    end.y += END_EPSILON;

                    PickResults pick = mesh.AdvancedCollide(msRay.Origin, end, parameters);
                    if (pick.HasCollided)
                    {
                        result[i, j] = (int)TILE_ATTRIBUTES.WALL; 
                    }
                }
            }
            return result;
        }

        // this is used for applying and unapplying footprints
        // there are tiles on both sides of an edge unless one of the edges is out of bounds
        // returns's int[,] of correct size based on adjacent edges being occupied
        // and containing the footprint tilemask data
        private int[,] GetTilesAlongEdge(CellEdge edge)
        {
            uint sizeX = TilesPerCellX;
            uint sizeZ = TilesPerCellZ;

            int[,] results;

            Vector3d position;
            Vector3i location = GetTileLocationFromEdgeID(edge.ID, out position);

            // TODO: when creating a footprint for an edge, we need the bottomLeft 
            //       location I think and the position we use to apply/unapply
            //       the footprint is based on that origin.  Our footprint itself
            //       should only contain tilemask data, not flattened coordinate indices.

            // TODO: the footprint's size should be modified based on whether this edge
            //       is on a boundary or not. (are hard boundaries handled differently than IN_BOUND boundaries?)

            //    // is edge along a boundary, if so only half the tiles will be returned
            // based on adjacents, we determine if we overlap into neighboring cells on either end
            if (edge.Orientation == CellEdge.EdgeOrientation.Horizontal)
            {
                sizeX = 2;
                sizeZ = TilesPerCellZ;
            }
            else if (edge.Orientation == CellEdge.EdgeOrientation.Vertical)
            {
                sizeZ = 2;
                sizeX = TilesPerCellX;
            }
            else throw new Exception("Edge orientation not supported.");


            results = new int[sizeX, sizeZ];

            for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeZ; j++)
                    results[i, j] = (int)TILE_ATTRIBUTES.WALL | (int)TILE_ATTRIBUTES.COMPONENT;

            return results;
        }

        private int[,] GetTilesAlongEdge(uint edgeID)
        {
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            return GetTilesAlongEdge(edge);
        }

        //private int[,] GetWallFootprint(int edgeID)
        //{
        //    //    // based on adjacents and Edge.Orientation, determine footprint based on wall sub-model

        //}

        #region AutoTileEdgeSegments
        public bool WallExists(uint edgeID)
        {
            return mEdgeModelMap.ContainsKey(edgeID);
        }

        private bool WallExists(CellEdge edge)
        {
            if (mEdgeModelMap == null) return false;

            // recall that edge.ID's are unique amgonst all floors
            // based on adjacency, we should be able to deduce whether there are
            // wall models that need to be added/removed
            return mEdgeModelMap.ContainsKey(edge.ID);
        }

        private int GetPerpendicularAdjacentsCount(CellEdge[] adjacents)
        {
            int result = 0;
            for (int i = 2; i <= 5; i++)
            {
                if (WallExists(adjacents[i]))
                    result++;
            }

            return result;
        }

        // todo: verify out of bounds edges have an ID of -1
        public int[] GetParrallelAdjacentEdges(uint edgeID)
        {
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            CellEdge[] results =  GetAdjacentEdges(edge);
            
            int[] iresults = new int[2];
            iresults[0] = (int)results[0].ID;
            iresults[1] = (int)results[1].ID;
            return iresults;
        }

        // todo: verify out of bounds edges have an ID of -1
        public int[] GetAllAdjacentEdges(uint edgeID)
        {
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            CellEdge[] results = GetAdjacentEdges(edge);

            int[] iresults = new int[results.Length];
            for (int i = 0; i < results.Length; i++)
                iresults[i] = (int)results[i].ID;

            return iresults;
        }

        private CellEdge[] GetAdjacentEdges(uint edgeID)
        {
            CellEdge edge = CellEdge.CreateEdge(edgeID, CellCountX, CellCountY, CellCountZ);
            return GetAdjacentEdges(edge);
        }

        private CellEdge[] GetAdjacentEdges(CellEdge edge)
        {
            CellEdge[] results = new CellEdge[6];
            CellEdge.EdgeStatistics stats = CellEdge.GetEdgeStatistics(CellCountX, CellCountY, CellCountZ);

            CellEdge.EdgeOrientation orientation = edge.Orientation;

            // TODO: if the adjacent edge is out of bounds then what?

            if (orientation == CellEdge.EdgeOrientation.Vertical)
            {
                // 2 parallel
                // -- 

                // parallel bottom
                results[0] = CellEdge.CreateEdge(edge.Origin - stats.VertexCountX, edge.Origin, CellCountX, CellCountY, CellCountZ);

                // parallel top
                results[1] = CellEdge.CreateEdge(edge.Dest, edge.Dest + stats.VertexCountX, CellCountX, CellCountY, CellCountZ);

                // 4 perpendicular (2 at each end)
                // --

                // perpendicular bottom left
                results[2] = CellEdge.CreateEdge(edge.Origin - 1, edge.Origin, CellCountX, CellCountY, CellCountZ);

                // perpendicular bottom right
                results[3] = CellEdge.CreateEdge(edge.Origin, edge.Origin + 1, CellCountX, CellCountY, CellCountZ);

                // perpendicular top left
                results[4] = CellEdge.CreateEdge(edge.Dest - 1, edge.Dest, CellCountX, CellCountY, CellCountZ);

                // perpendicular top right
                results[5] = CellEdge.CreateEdge(edge.Dest, edge.Dest + 1, CellCountX, CellCountY, CellCountZ);
            }
            else if (orientation == CellEdge.EdgeOrientation.Horizontal)
            {
                // 2 parallel
                // -- 

                // parallel left
                results[0] = CellEdge.CreateEdge(edge.Origin - 1, edge.Origin, CellCountX, CellCountY, CellCountZ);

                // parallel right
                results[1] = CellEdge.CreateEdge(edge.Dest, edge.Dest + 1, CellCountX, CellCountY, CellCountZ);

                // 4 perpendicular (2 at each end)
                // --

                // perpendicular left bottom
                results[2] = CellEdge.CreateEdge(edge.Origin - stats.VertexCountX, edge.Origin, CellCountX, CellCountY, CellCountZ);

                // perpendicular left top
                results[3] = CellEdge.CreateEdge(edge.Origin, edge.Origin + stats.VertexCountX, CellCountX, CellCountY, CellCountZ);

                // perpendicular right bottom
                results[4] = CellEdge.CreateEdge(edge.Dest - stats.VertexCountX, edge.Dest, CellCountX, CellCountY, CellCountZ);

                // perpendicular right top
                results[5] = CellEdge.CreateEdge(edge.Dest, edge.Dest + stats.VertexCountX, CellCountX, CellCountY, CellCountZ);

            }
            else throw new NotImplementedException("Diagonal walls not (yet?) supported.");

            return results;
        }


        private void ClearExistingEdge(CellEdge edge)
        {
            MinimeshMap map;
            bool exists = mEdgeModelMap.TryGetValue(edge.ID, out map);

            if (!exists) return;

            throw new NotImplementedException("Aren't we supposed to be removing the map from the mEdgeModelMap dictionary?");

            // is there a door on this edge?

            // clear models

            // clear footprints
        }
        
        #endregion


        private Quaternion GetSegmentRotationAngle(int cellID, CellEdge edge)
        {
            Quaternion result;

            // TODO: for walls, this is not giving us consistant rotation for 2 out of the 4 possible rotations.
            // we need to take into account the cellID... i think...

            // 0 to 360 for dynamics like actors, but 0 - 315 in 45 increments for statics
            switch (edge.Orientation)
            {
                case CellEdge.EdgeOrientation.Horizontal:
                    result = new Quaternion(0d, 0d, 0d);   // 0 or 360 degrees
                    break;
                case CellEdge.EdgeOrientation.Vertical:
                    result = new Quaternion(0d, 270d, 0d); ; // 270 degrees // todo: the angle should be in radians but this method is never called from anywhere
                    break;
                default:
                    throw new NotImplementedException("AssetPlacementTool - Diagonal doors not implimented.");
                    break;
            }

            return result;
        }

        private byte GetSegmentRotationAngleIndex(int cellID, CellEdge edge)
        {
            byte result = 0;

            //Interior interior;
            //interior.GetEdge
            // 0 to 360 for dynamics like actors, but 0 - 315 in 45 increments for statics
            switch (edge.Orientation)
            {
                case CellEdge.EdgeOrientation.Horizontal:
                    result = 0;   // 0 or 360 degrees
                    break;
                case CellEdge.EdgeOrientation.Vertical:
                    result = 64; // 270 degrees
                    break;
                default:
                    throw new NotImplementedException("AssetPlacementTool - Diagonal doors not implimented.");
                    break;
            }

            return result;
        }


        /// <summary>
        /// This is called by renderer to enable/disable walls for up/down/cutaway modes.
        /// TODO: can i do this in a way that doesnt require sync locking of the mEdgeModelMap?
        /// TODO: i believe the problem is that the command to place walls is occurring as a
        /// thread pool operation that does not wait to finish between renders...  
        /// it is started via our appMain.CheckInput which leads to sending of the threaded
        /// command to place a wall... but the main game loop is on a seperate thread so that
        /// rendering is never blocked by processing of commands. 
        /// Processing of commands meanwhile is done in ((FormMainBase)_form).ProcessCompletedCommandQueue();
        /// off the main app windows message proc.
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="cameraLookAt"></param>
        public void FilterCutAway(int mode, Vector3d cameraPosition, Vector3d cameraLookAt)
        {

            // TEMP HACK: Disable all minimeshes
 //           foreach (MinimeshMap map in mEdgeModelMap.Values)
 //               map.Minimesh.Enable = false;
 //
 //           return; // TODO: this method is filtering all interior walls so they do not render.

            //// NOTE: our rendering is camera space but with respect to exterior vehicle's
            ////       location so our world space camera position converted to interior space
            ////       which is at origin must deduct the vehicle's translation
            ////position += this.Parent.Translation;
            //cameraPosition -= this.Parent.Translation;

            //byte enableElement = 0;
            //if (mode == 2)
            //    enableElement = 1;

            //foreach (MinimeshMap map in mEdgeModelMap.Values)
            //{
            //    int leftMinimeshElementCount = 0;
            //    if (map.BottomLeftMinimesh != null)
            //        leftMinimeshElementCount = (int)map.BottomLeftMinimesh.InstancesCount;

            //    int rightMinimeshElementCount = 0;
            //    if (map.TopRightMinimesh != null)
            //        rightMinimeshElementCount = (int)map.TopRightMinimesh.InstancesCount;

            //    // TODO: for the first two modes, its best to just iterate
            //    //       through MinimeshGeometry list and not mEdgeModelMap collection
            //    switch (mode)
            //    {
            //        case 0: // Walls Down Filter
            //        case 2: // Walls Up Filter
            //            {
            //                if (leftMinimeshElementCount > 0)
            //                {

            //                    byte[] enable = new byte[leftMinimeshElementCount];
            //                    for (int j = 0; j < enable.Length; j++)
            //                        enable[j] = enableElement;

            //                     map.BottomLeftMinimesh.EnableArray = enable;

            //                     // the corresponding minimesh on other side if it exists must also be disabled
            //                     map.TopRightMinimesh.EnableArray = enable;
            //                }
            //                break;
            //            }

            //        case 1: // Cut Away Filter, find all walls that need to be enabled/disabled
            //        default:
            //            {
            //                if (map.EdgeLocation == CellEdge.EdgeLocation.Inner)
            //                {
            //                    // cut away interior walls 
            //                    // TODO: eventually, can we enable our interior minimesh 
            //                    // wall stubs that show the cutaway view?  I think what
            //                    // sims does is use csg so that the cutaway
            //                    map.BottomLeftMinimesh.SetElementEnable((uint)map.BottomLeftElementIndex, 0);
            //                    map.TopRightMinimesh.SetElementEnable((uint)map.TopRightElementIndex, 0);
            //                }
            //                else
            //                {
            //                    // exterior walls need to be tested based on which side (top/left or back/right)
            //                    // is facing the exterior

            //                    Vector3d position = Vector3d.Zero();
            //                    Vector3d rotation = Vector3d.Zero();

            //                    if (map.EdgeLocation == CellEdge.EdgeLocation.Exterior_BottomLeft)
            //                    {
            //                        // for Exterior_BottomLeft EdgeLocation we test TopRightMinimesh
            //                        if (map.TopRightElementIndex != -1 && map.TopRightElementIndex < map.TopRightMinimesh.InstancesCount)
            //                        {
            //                            position = map.TopRightMinimesh.GetElementPosition((uint)map.TopRightElementIndex);
            //                            rotation = map.TopRightMinimesh.GetElementRotation((uint)map.TopRightElementIndex);
            //                        }
            //                    }
            //                    else if (map.EdgeLocation == CellEdge.EdgeLocation.Exterior_TopRight)
            //                    {
            //                        // for Exterior_TopRight EdgeLocation we test BottomLeftMinimesh
            //                        if (map.BottomLeftElementIndex != -1 && map.BottomLeftElementIndex < map.BottomLeftMinimesh.InstancesCount)
            //                        {
            //                            position = map.BottomLeftMinimesh.GetElementPosition((uint)map.BottomLeftElementIndex);
            //                            rotation = map.BottomLeftMinimesh.GetElementRotation((uint)map.BottomLeftElementIndex);
            //                        }
            //                    }
            //                    else throw new NotImplementedException();

            //                    // still here, test facing and continue if either is not correct

            //                    byte backFacing = IsBackFacing(position, rotation, cameraPosition, cameraLookAt);
            //                    // if this segment is backfacing, disable both left and right
            //                    if (backFacing == 0)
            //                        enableElement = 0;
            //                    else
            //                        enableElement = 1;


            //                    if (map.BottomLeftElementIndex != -1 && map.BottomLeftElementIndex < map.BottomLeftMinimesh.InstancesCount)
            //                        map.BottomLeftMinimesh.SetElementEnable((uint)map.BottomLeftElementIndex, enableElement);

            //                    // could be -1 for walls on edge of floorplan 
            //                    if (map.TopRightElementIndex != -1 && map.TopRightElementIndex < map.TopRightMinimesh.InstancesCount)
            //                        map.TopRightMinimesh.SetElementEnable((uint)map.TopRightElementIndex, enableElement);

            //                }
            //                break;
            //            }
            //    }
            //
            // return;

            //// so does this simply represent a culling option? try to find
            //// wall models and then manually enable/disable backfacing walls if walls
            //// are "cutaway".   The other modes are easy all on/off.
            //// note: the "up" mode might be unnecessary as it is sort of like showing
            ////       full exterior hull...

            //// define a match for finding all nodes of type "Model" who's IDs start with a prefix
            //// that we know all floor\ceiling and wall models for any one particular Interior share.
            //Predicate<Keystone.Elements.Node> match = e =>
            //{
            //    return
            //        e is Keystone.Elements.Model &&
            //        e.ID.Contains(Keystone.Portals.CelledRegion.GetInteriorElementPrefix(this._id, CelledRegion.PREFIX_WALL, typeof(Keystone.Elements.Model)));
            //};

            //// Perform the actual query passing in our match 
            //// NOTE: we are guaranteed because of the Predicate delegate we are using, that all results
            //// are of type Keystone.Elements.Model
            //Keystone.Elements.Node[] foundModels = this.Query(true, match);
            //if (foundModels == null) return;
            //for (int i = 0; i < foundModels.Length; i++)
            //{
            //    Keystone.Elements.Model model = (Keystone.Elements.Model)foundModels[i];
            //    Keystone.Elements.MinimeshGeometry miniMesh = (Keystone.Elements.MinimeshGeometry)model.Geometry;

            //    switch (mode)
            //    {
            //        case 0: // down
            //            {
            //                byte[] enable = new byte[miniMesh.InstancesCount];
            //                for (int j = 0; j < enable.Length; j++)
            //                    enable[j] = 0;

            //                miniMesh.EnableArray = enable;
            //                break;
            //            }
            //        case 2: // up
            //            {
            //                byte[] enable = new byte[miniMesh.InstancesCount];
            //                for (int j = 0; j < enable.Length; j++)
            //                    enable[j] = 1;

            //                miniMesh.EnableArray = enable;
            //                break;
            //            }
            //        case 1: // cut away
            //        default:
            //            {
            //                // TODO: going purely by the minimesh element's rotation
            //                //       i dont think is going to work because we have seperate
            //                //       elements for our double sided walls and so there's no way
            //                //       to know which is which
            //                // instead what we need is a mapping of segments to minimeshes
            //                // and then we can test the rotation of just the "front" or "left"
            //                // minimesh and if front facing we disable it as well as it's paired
            //                // backsided minimesh element.
            //                // TODO: but how do we know which segment this is, the front/left or
            //                // the back/right?  and then, how would we find it's matching 
            //                // back/right?  perhaps we can query the celledregion or mabye...
            //                // best to just pass the call to enable/disable backfacing to it?

            //                //.. but until then, we can at least verify that we can determine backfacing
            //                // properly
            //                // TODO: how do we query the mapping though and not the minimesh?
            //                //       the mapping currently is in CelledRegion and not
            //                // in the model...  if the model had the mapping it would be nice...
            //                // wasn't one of our concepts when making celledRegion structure
            //                // hosts a ModelSelector and then models underneath and so
            //                // walls in turn have to support perhaps a selector for each?eww
            //                // it's either that or a type of Model that can host a type of Geometry
            //                // node that can represent up to 2 minimeshes and so our "map"
            //                // is now that node that holds "left" and "right" sides 
            //                // but no... because left and right sides can have different
            //                // appearances so it is two models and so must have each
            //                // i suspect under a selector... but... ugh.  currently we have
            //                // one "wall" model and that holds one minimesh for all segments
            //                // so... i think its best to have a special type of geometry node
            //                // that hosts all our minimeshes like an atlas... so we still
            //                // have one wall model...
            //                byte[] enable = new byte[miniMesh.InstancesCount];
            //                Vector3d[] positions = miniMesh.PositionArray;
            //                Vector3d[] rotations = miniMesh.RotationArray;
            //                for (int j = 0; j < enable.Length; j++)
            //                    enable[j] = IsBackFacing(positions[j], rotations[j], cameraPosition, cameraLookAt);

            //                miniMesh.EnableArray = enable;
            //                break;
            //            }
            //    }

        }

        private byte IsBackFacing(Vector3d segmentPosition, Vector3d rotation, Vector3d cameraPosition, Vector3d cameraLookAt)
        {
            // returns a byte instead of a bool
            // 0 = disable, 1 = enable
            // so we can directly set
            // our minimesh enable array 

            // compute camera direction in model space 
            Vector3d direction = -(cameraPosition - segmentPosition);
            direction.Normalize();
            Vector3d normal;
            normal.x = 0;
            normal.y = 0;
            normal.z = 1;

            // transform the normal into world space
            if (rotation.y == 0) // facing +z
            {
            }
            else if (rotation.y == 90d) // counter-clockwise rotation -x
            {
                normal.x = 1;
                normal.z = 0;
            }
            else if (rotation.y == 180d)
            {
                normal.x = 0;
                normal.z = -1;
            }
            else if (rotation.y == 270d)
            {
                normal.x = -1;
                normal.z = 0;
            }
            else
                throw new ArgumentOutOfRangeException();


            // draw the polygon here, it's visible
            double result = Vector3d.DotProduct(direction, normal);
            if (result < 0)
                return 1;

            return 0;
        }


        internal int GetNeighbor (uint cell, Direction direction)
        {
            // if the neighbor in the specified cardinal direction
            // is in bounds return it's index.
            // else return -1
            uint x, y, z;

            // recall unflatted always return 0 based to Width/Depth/Height and not
            // the actual x,y,z offsets 
            UnflattenCellIndex (cell, out x, out y, out z);

            switch (direction)
            {
                // finding the corresponding neighbor in a cardinal direction 
                // is easy by unflattening the cell.  The only concern is that we 
                // check we are not out of bounds.  And that's easy by 
                case Direction.Front:
                    z += 1;
                    break;
                case Direction.Back:
                    z -= 1;
                    break;
                case Direction.Left:
                    x -= 1;
                    break;
                case Direction.Right:
                    x += 1;
                    break;
                case Direction.Top:
                    y += 1;
                    break;
                case Direction.Bottom:
                    y -=1;
                    break;
                default:
                    throw new ArgumentException("Unexpected cardinal direction '" + direction.ToString() + "' Range should be 0 - 7.");
            }

            if (IsCellInBounds(x,y,z))
                return (int)FlattenCell (x, y, z);
            else return -1;
        }

           

        public uint[] GetAdjacentCellList(uint cellIndex)
        {
            List<uint> results = new List<uint>();

            uint x, y, z;
            Utilities.MathHelper.UnflattenIndex(cellIndex, CellCountX, CellCountZ, out x, out y, out z);

            if (x < StopX)
                results.Add(FlattenCell (x + 1, y, z));
            if (x > StartX)
                results.Add(FlattenCell (x - 1, y, z));
            if (y < StopY)
                results.Add(FlattenCell (x, y + 1, z));
            if (y > StartY)
                results.Add(FlattenCell (x, y - 1, z));
            if (z < StopZ)
                results.Add(FlattenCell (x, y, z + 1));
            if (z > StartZ)
                results.Add(FlattenCell (x, y, z - 1));

            return results.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startTileIndex"></param>
        /// <param name="endTileIndex"></param>
        /// <param name="limitToSingleAxis">Return list of tiles along single axis line rather than a rubber band box of tiles between start and end tile.</param>
        /// <returns></returns>
        public uint[] GetTileList(uint startTileIndex, uint endTileIndex, bool limitToSingleAxis)
        {
            // I think these should be generated dynamically 
            uint startX, startY, startZ, stopX, stopY, stopZ;
            Utilities.MathHelper.UnflattenIndex(startTileIndex, CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ, 
                out startX, out startY, out startZ);
            Utilities.MathHelper.UnflattenIndex(endTileIndex, CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ, 
                out stopX, out stopY, out stopZ);

            // if somehow the Y elevation is different, we disallow and return null
            if (startY != stopY) return null;

            if (IsTileInBounds(startX, startY, startZ) && IsTileInBounds(stopX, stopY, stopZ))
            {
                if (startTileIndex == endTileIndex) return new uint[] { startTileIndex };
				
				uint horizontalSeperation = (uint)Math.Abs ((int)stopX - (int)startX) + 1;
				uint verticalSeperation = (uint)Math.Abs ((int)stopZ - (int)startZ) + 1;
					
				
				if (limitToSingleAxis)
				{
					if (horizontalSeperation == verticalSeperation)
					{
						if (horizontalSeperation <= 1)
						{
							// don't have to do anything, it's just one point
						}
						else 
							// just pick horizontal always and reset vertical to 0 seperation
							stopZ = startZ;
					}
					else if (horizontalSeperation > verticalSeperation)
						stopZ = startZ;
					else
						stopX = startX;
				
					// update the seperation values (note: not casting to (int) may result in overflow if Stop < Start but rather than throw exception it will create a huge int value
					horizontalSeperation = (uint)Math.Abs ((int)stopX - (int)startX) + 1;
					verticalSeperation = (uint)Math.Abs ((int)stopZ - (int)startZ) + 1;
				}
				
				
				// reverse the order so that start indices are always lower than the end indices
                if (startX > stopX) // horizontal seperation
                {
					Utilities.MathHelper.Swap (ref startX, ref stopX);
                }
                else if (startZ > stopZ) // vertical seperation
                {                  
					Utilities.MathHelper.Swap (ref startZ, ref stopZ);
                }

                uint[] result = new uint[horizontalSeperation  * verticalSeperation];
                uint count = 0;
                for (uint i = startX; i <= stopX; i++)
                    for (uint j = startZ; j <= stopZ; j++)
                    {
                        result[count] = Utilities.MathHelper.FlattenCoordinate(i, startY, j, CellCountX * TilesPerCellX, CellCountZ * TilesPerCellZ);
                        count++;
                    }

                return result;
            }

            return null;


        }

        /// <summary>
        /// generates the flood filled rectangular list of cell indices
        /// given a start and end cell index
        /// </summary>
        /// <param name="startCellIndex"></param>
        /// <param name="endCellIndex"></param>
        /// <returns></returns>
        public uint[] GetCellList(uint startCellIndex, uint endCellIndex)
        {
            // I think these should be generated dynamically 
            uint startX, startY, startZ, stopX, stopY, stopZ;
            Utilities.MathHelper.UnflattenIndex(startCellIndex, CellCountX, CellCountZ, out startX, out startY, out startZ);
            Utilities.MathHelper.UnflattenIndex(endCellIndex, CellCountX, CellCountZ, out stopX, out stopY, out stopZ);

            // if somehow the Y elevation is different, we disallow and return null
            if (startY != stopY) return null;

            // recall the unflattened stop/x/y/z and start/x/y/z are 0 based on CellCount's 
            // not cell x,y,z coordinates
            if (IsCellInBounds(startX, startY, startZ) && IsCellInBounds(stopX, stopY, stopZ))
            {
                if (startCellIndex == endCellIndex) return new uint[] { startCellIndex };

                // reverse the order so that start indices are always lower than the end indices
                if (startX > stopX)
                {
                    Utilities.MathHelper.Swap (ref startX, ref stopX);
                }
                if (startZ > stopZ)
                {
                    Utilities.MathHelper.Swap (ref startZ, ref stopZ);
                }

                uint[] result = new uint[(stopX - startX + 1) * (stopZ - startZ + 1)];
                uint count = 0;
                for (uint i = startX; i <= stopX; i++)
                    for (uint j = startZ; j <= stopZ; j++)
                    {
                        result[count] = Utilities.MathHelper.FlattenCoordinate(i, startY, j, CellCountX, CellCountZ);
                        count++;
                    }

                return result;
            }
 
            return null;
        }

        public uint[] GetCellList(uint startCellIndex, uint endCellIndex, int flag)
        {
            // I think these should be generated dynamically 
            uint startX, startY, startZ, stopX, stopY, stopZ;
            Utilities.MathHelper.UnflattenIndex(startCellIndex, CellCountX, CellCountZ, out startX, out startY, out startZ);
            Utilities.MathHelper.UnflattenIndex(endCellIndex, CellCountX, CellCountZ, out stopX, out stopY, out stopZ);
            

            // recall the unflattened stop/x/y/z and start/x/y/z are 0 based on CellCount's 
            // not cell x,y,z coordinates
            if (IsCellInBounds(startX, startY, startZ) && IsCellInBounds(stopX, stopY, stopZ))
            {
                if (startCellIndex == endCellIndex) return new uint[] { startCellIndex };

                // reverse the order so that start indices are always lower than the end indices
                if (startX > stopX)
                {
                    Utilities.MathHelper.Swap(ref startX, ref stopX);
                }
                if (startZ > stopZ)
                {
                    Utilities.MathHelper.Swap(ref startZ, ref stopZ);
                }

                List<uint> results = new List<uint>();
                uint count = 0;
                for (uint i = startY; i <= stopY; i++)
                    for (uint j = startX; j <= stopX; j++)
                        for (uint k = startZ; k <= stopZ; k++)
                        {
                            if (IsCellBOUNDS_PAINTED_IN(j, i, k) && CellContainsFlag (j, i, k, flag))
                            {
                                results.Add (Utilities.MathHelper.FlattenCoordinate(j, i, k, CellCountX, CellCountZ));
                                count++;
                            }
                        }

                return results.ToArray ();
            }

            return null;
        }

        public CellEdge[] GetEdgeList(uint startCellIndex, uint startCellCorner, uint endCellIndex, uint endCellCorner)
        {
            // I think these should be generated dynamically 
            uint startX, startY, startZ, stopX, stopY, stopZ;
            Utilities.MathHelper.UnflattenIndex(startCellIndex, CellCountX, CellCountZ, out startX, out startY, out startZ);
            Utilities.MathHelper.UnflattenIndex(endCellIndex, CellCountX, CellCountZ, out stopX, out stopY, out stopZ);

            // recall the unflattened stop/x/y/z and start/x/y/z are 0 based on CellCount's 
            // not cell x,y,z coordinates
            if (IsCellInBounds(startX, startY, startZ) && IsCellInBounds(stopX, stopY, stopZ))
            {
                // using the cell index and corner index, find the vertex ID for start and end
                int startVertexID = GetVertexID(startCellIndex, startCellCorner);
                int endVertexID = GetVertexID(endCellIndex, endCellCorner);

                // swap id's if the end vertex is smaller than start
                // This way the computed origin and dest for each edge will correctly
                // have origin as smaller value
                if (endVertexID < startVertexID)
                {
                    int tmp = endVertexID;
                    endVertexID = startVertexID;
                    startVertexID = tmp;
                }

            //    System.Diagnostics.Debug.WriteLine("Start Cell Index = " + startCellIndex.ToString() + " Corner = " + startCellCorner.ToString() + " Vertex ID = " + startVertexID.ToString());
            //    System.Diagnostics.Debug.WriteLine("End Cell Index = " + endCellIndex.ToString() + " Corner = " + endCellCorner.ToString() + " Vertex ID = " + endVertexID.ToString());


                // determine the x axis row for the origin and dest
                int horizVertexCount = (int)CellCountX + 1;
                int originRow = (int)(startVertexID / horizVertexCount);
                int destRow = (int)(endVertexID / horizVertexCount);
                int verticalSeperation = System.Math.Abs(originRow - destRow);

                int originColumn = (int)(startVertexID % horizVertexCount);
                int destColumn = (int)(endVertexID % horizVertexCount);
                int horizontalSeperation = System.Math.Abs(originColumn - destColumn);

                //System.Diagnostics.Debug.WriteLine("Start Vertex = " + startVertexID.ToString() +
                //    "  End Vertex = " + endVertexID.ToString());
                //System.Diagnostics.Debug.WriteLine("Origin row = " + originRow.ToString() +
                //    "  Dest row = " + destRow.ToString());

                //System.Diagnostics.Debug.WriteLine("Horiz Seperation = " + horizontalSeperation.ToString());

                int count = verticalSeperation + horizontalSeperation;
                CellEdge[] edges = new CellEdge[count];

                // no seperation at all
                if (horizontalSeperation == 0 && verticalSeperation == 0) return null; 


                if (horizontalSeperation > 0 && verticalSeperation > 0)
                {
                    return null;
                    if (horizontalSeperation == verticalSeperation)
                    {
                        // this is a perfect diagonal line and is allowed
                   
                    }
                    else
                    {
                        // this is an angled line that is NOT perfectly diagonal and is NOT allowed
                    }
                }
                else if (horizontalSeperation > 0)
                {
                    // perfect horizontal line is allowed
                    uint origin = (uint)startVertexID ;
                    uint i = 0;
                    while  ( origin != endVertexID)
                    {
                        // dest = next vertex towards destVertexID (- or + direction)
                        uint dest;
                        // TODO: these should be renamed to start/end but with the knowledge
                        // that the computed origin and dest should always have origin as smaller value vertex index!
                        System.Diagnostics.Debug.Assert(startVertexID < endVertexID);
                        //if (startVertexID < endVertexID)
                            dest = (uint)origin + 1;
                        //else
                        //    dest = (uint)origin - 1; // this line should never get called and may be obsolete.  startVertexID should always be less than endVertexID

                            edges[i] = CellEdge.CreateEdge(origin, dest, CellCountX, CellCountY, CellCountZ);
                        origin = dest;
                        i++;
                    }
                    //System.Diagnostics.Debug.WriteLine("breakpoint");
                }
                else
                {
                    // perfect verticle line is allowed
                    uint origin = (uint)startVertexID;
                    uint i = 0;
                    while (origin != endVertexID)
                    {
                        // dest = next vertex towards destVertexID (- or + direction)
                        uint dest;
                        System.Diagnostics.Debug.Assert(startVertexID < endVertexID);
                        //if (startVertexID < endVertexID)
                            dest = (uint)(origin + VertexCountX);
                        //else
                        //    dest = (uint)(origin - VertexCountX); // this line should never get called and may be obsolete.  startVertexID should always be less than endVertexID

                        edges[i] = CellEdge.CreateEdge(origin, dest, CellCountX, CellCountY, CellCountZ);
                        origin = dest;
                        i++;
                    }
                   // System.Diagnostics.Debug.WriteLine("breakpoint");
                }

                return edges;
            }
            return null;
        }
         
        public CellEdge GetEdge(uint cellIndex, byte rotation)
        {
            uint startVertex = 0;
            uint endVertex = 0;

            switch (rotation)
            {
                case 0:   // 0 or 360 degrees = left edge
                    startVertex = 0;
                    endVertex = 1;
                    break;
                case 32:  // 45 degrees
                    
                    break;
                case 64:  // 90 degrees = top edge
                    startVertex = 1;
                    endVertex = 2;
                    break;
                case 96:  // 135 degrees
                    break; 
                case 128:  // 180 degrees = right edge
                    startVertex = 2;
                    endVertex = 3;
                    break;
                case 160: // 225 degrees
                    break;
                case 192: // 270 degrees = bottom edge
                    startVertex = 3;
                    endVertex = 0;
                    break;
                case 224: // 315 degrees
                    break; 
                default:
                    throw new Exception();
                    break;
            }

            CellEdge result = GetEdgeList(cellIndex, startVertex, cellIndex, endVertex)[0];
            return result;
        }

        private CellEdge.EdgeLocation GetEdgeLocation(int bottomLeftCell, int topRightCell)
        {
            CellEdge.EdgeLocation location = CellEdge.EdgeLocation.Inner;

            if (bottomLeftCell > -1 && topRightCell > -1)
            {
                location = CellEdge.EdgeLocation.Inner;

                // make sure this edge lie within the cells that were painted as allowed interior
                // cells of the vehicle
                bool inBounds = (bool)Layer_GetValue("boundaries", (uint)bottomLeftCell);
                if (inBounds == false)
                    location = CellEdge.EdgeLocation.Exterior_BottomLeft;
                else
                {
                    inBounds = (bool)Layer_GetValue("boundaries", (uint)topRightCell);
                    if (inBounds == false)
                        location = CellEdge.EdgeLocation.Exterior_TopRight;
                    // else remains Inner
                }
            }
            else if (bottomLeftCell == -1)
                location = CellEdge.EdgeLocation.Exterior_BottomLeft;
            else if (topRightCell == -1)
                location = CellEdge.EdgeLocation.Exterior_TopRight;
            else
                throw new NotImplementedException();

            return location;
        }

       

        private int FindClosestEdge(Line3d[] edges, Vector3d intersectionPoint)
        {
            double distance = double.MaxValue;
            int closestEdgeID = -1;
            Vector3d closestPoint;
            if (edges.Length > 0)
            {
                for (int j = 0; j < edges.Length; j++)
                {
                    Vector3d point, o, d;
                    o = edges[j].Point[0];
                    d = edges[j].Point[1];
                    
                    double dist = Line3d.DistanceSquared(o, d,
                                                         intersectionPoint, out point);
                    if (dist < distance)
                    {
                        distance = dist;
                        closestPoint = point;
                        closestEdgeID = j;
                    }
                }
            }
            //// the below i beleive is actually finding the cloest endpoint on the cloesest edge
            //// the closestPoint is not necessarily an end point of this edge
            //// it's just the closest perpendicular to the line
            //// Actually, even this is not true.  The closest vertex is not even necessarily on this edge!
            //// finding the nearest edge to the impact point does not necessarily mean that the closest 
            //// vertex also lies on that closest edge.  In the case of a triangle that is very wide at it's base but with a short
            //// peak at center top, you could have an impact point closest to the bottom edge but with closest vertex at peak.
            //// So the following code is incorrect really... What we really need is a new routine to find closest vert
            //// were we test dist and dist2 and then get the intersectionPoint and then compare those in the above closest edge
            //// search loop.  To avoid having to do these loops twice, keep in mind that typically you're in edge or vert selection mode
            //// so you'd only do one or the other.  Verify in sketchup how that works
            ////result.iNearestVertex =;
            //double dist1 = Vector3d.GetDistance3dSquared(origin, intersectionPoint);
            //double dist2 = Vector3d.GetDistance3dSquared(dest, intersectionPoint);
            //if (dist1 < dist2)
            //{
            //    coord = origin;
            // //   result.VertexIndex = (int)edges[iclosestEdge].Origin.ID;
            //}
            //else
            //{
            //    coord = dest;
            // //   result.VertexIndex = (int)edges[iclosestEdge].Destination.ID;
            //}

            return closestEdgeID;
        }
        

        internal int GetVertexID(uint cellIndex, uint cellCorner)
        {
            uint vertexCountX = CellCountX + 1;
            uint vertexCountZ = CellCountZ + 1;
            uint cellsPerDeck = CellCountX * CellCountZ;
            uint verticesPerDeck = vertexCountX * vertexCountZ;
            uint currentDeck = cellIndex / cellsPerDeck;
            uint deckSpaceCellID = cellIndex - (cellsPerDeck * currentDeck);
            uint vertexID = deckSpaceCellID + (verticesPerDeck * currentDeck); // (cellIndex + 1) / CellCountX + cellIndex;
            uint cellRow = deckSpaceCellID / CellCountX;


            //if (cellCorner == 0) // do nothing
            if (cellCorner == 1 ||cellCorner == 2)
            {
                vertexID += CellCountX + cellCorner;
        
            }
            else if (cellCorner == 3)
                vertexID++;

           
            return (int)(vertexID + cellRow );
        }

        /// <summary>
        /// Using the 0 - CellCountX/Y/Z, verifies the mouse picked cell
        /// is valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool IsCellInBounds(uint x, uint y, uint z)
        {
            return (x >= 0 && x < CellCountX &&
                    y >= 0 && y < CellCountY &&
                    z >= 0 && z < CellCountZ);

        }

        /// <summary>
        /// verifies the mouse picked tile is valid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool IsTileInBounds(uint x, uint y, uint z)
        {
            return (x >= 0 && x < CellCountX * TilesPerCellX  &&
                    y >= 0 && y < CellCountY &&
                    z >= 0 && z < CellCountZ * TilesPerCellZ);

        }

        public bool IsCellExists(uint x, uint y, uint z)
        {
            return true; //  mCellMap[x, y, z] != null; // TODO: why did i comment this out?  was it during debugging?
        }

        /// <summary>
        /// A normal region has a fixed size that cannot be resized (such as a star sector's size)
        /// However a CelledRegion is comprised of fixed size "cells" but the dimensions in
        /// row, column, and layer can change and that will result in a change in the bounding volume.
        /// 
        /// However there is one other consideration.  Cells that are completely empty can be flagged
        /// as not affecting the volume.  This is the default case.
        /// </summary>
        #region IBoundVolume members
        protected override void UpdateBoundVolume()
        {
            // TODO: when designing a new ship floorplan, new cells can be added/removed at will.
            // This bounding box should ocmpute it's bounds strictly by the child cells within it...
            if ((mChangeStates & Enums.ChangeStates.BoundingBoxDirty)  == Enums.ChangeStates.BoundingBoxDirty)
            {  // TODO: test for Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly needs to be included yes?

                Keystone.Types.Vector3d min;
                Keystone.Types.Vector3d max;
                Keystone.Types.Vector3d halfSize;

                halfSize = mCellSize * .5;
                
                min.x = StartX * mCellSize.x - halfSize.x;
                min.y = StartY * mCellSize.y - halfSize.y;
                min.z = StartZ * mCellSize.z - halfSize.z;
                               

                max = -min;


                mBox = new Keystone.Types.BoundingBox(min, max);
                mBox = Keystone.Types.BoundingBox.Transform(mBox, RegionMatrix);
                double radius = mBox.Height * .5;
                radius = Math.Max(radius, mBox.Width * .5);
                radius = Math.Max (radius, mBox.Depth * .5);
                mSphere = new Keystone.Types.BoundingSphere(mBox.Center, radius);
                DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
            }
        }
        #endregion

    }
}