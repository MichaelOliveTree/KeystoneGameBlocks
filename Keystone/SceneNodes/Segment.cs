using System;
using System.Collections.Generic;
using Keystone.Elements;
using Keystone.CSG;


namespace Keystone.SceneNodes
{
	
    // TODO: could be nice if we had a special type of MinimeshMap object that could contain
    // under a single Model the minimeshes we are using for all walls.  This way we only need one
    // Model since our current code says only one geometry object per Model.  So a "MinimeshGeometryAtlas"
    // might wrap a collection under a single Geometry node.
    
    // TODO: this struct should be "internal" to keystone.dll only
    public struct MinimeshMap // TODO: we need a comparable "map" for ceilings and floors
    {
        // TODO: Maybe for MinimeshGeometry we use an array instead of BottomLeftMesh and TopRightMinimesh we just use Minimesh[] Geometry
        public CellEdge Edge; // TODO: I think we don't need to maintain the Edge here. Just an edgeID is sufficient.  Too much memory useage to maintain the CellEdge struct on every occupied edge.
        public EdgeStyle Style; // TODO: what is the purpose of storing the style here? Isn't EdgeStyle just for sending command across wire to change an edge's visuals?


        public ModelSelector ModelSelector;
        public int SubModelIndex;
        public Model Model; // TODO: Model and Minimesh references aren't really needed.  Removing will save 8 bytes per occupied edge right?
        public MinimeshGeometry Minimesh;
        public int ElementIndex;

        //public ModelSelector BottomLeftModelSelector;
        //public ModelSelector TopRightModelSelector;

        //public int BottomLeftSubModelIndex;
        //public int TopRightSubModelIndex;

        //public Model BottomLeftModel; // TODO: minimesh could just be key'd to the geometry's resource descriptor and appearance hashcode?
        //public Model TopRightModel;
        //public MinimeshGeometry BottomLeftMinimesh; // bottom and top can end up using same MinimeshGeometry, but not necessarily
        //public MinimeshGeometry TopRightMinimesh;
        //public int BottomLeftElementIndex;
        //public int TopRightElementIndex;

        public CellEdge.EdgeLocation EdgeLocation;
    }
 

    internal enum Direction
    {
        Front = 0,
        Back,
        Left,
        Right,
        Top,
        Bottom
    }
    
    // is this something that should be inherited and then scripted?
    public class PhysicalProperties // physical properties for a Segment such as damage, weight, volume
                                    // because segments are not individual entity children of the celledregion.
                                    // They are part of the data of the celledRegion.
    {

    }



    //public class SegmentState
    //{
    // SegmentState is a type 'object' known by scripts only 
    //	    Hitpoints
    //	    Damage
    //	    Armor
    //		    DR
    //		    PD
    //}

    /// <summary>
    /// Only instanced for in bounds valid segments and so doesn't take up space by 
    /// representing empty cells or edges or corners.
    /// </summary>
    public class Segment
    {
        public uint Location; // flattened x,y,z location of the segment 

        //                    // http://benjamin-meyer.blogspot.com/2011/11/using-collaborative-diffusion-rather.html
        //                    // cell.Diffusion["enemy_boarder"] = val;
        //                    // <-- wouldnt be so bad if my diffusion keys could use ints and a fixed length array
        //    
        public EdgeStyle Style; // cannot be null but one side of walls can be null 
        // must be associated so that segements can change their style when adjacent segments change (added/removed)
        //bool mOverrideInteriorTexture; // if user has manually changed this texture we will not update it automatically based on adjacent segment changes
        //bool mOverrideExteriorTexture; // can override textures but not mesh.  overriding the mesh style will result in the SegmentStyle itself switching to defaultSegment

        //	Sides Side; // maybe an Enum { Left, Right, Both }
        public int InteriorAtlasTextureIndex; // index within the atlas, not the tvindex
        public int ExteriorAtlasTextureIndex; // index within the atlas, not the tvindex

        //	int[] MeshPuzzleID; // corner piece, corner bottom, corner middle, middle, etc
        //public long Footprint; // 64bits represents 8x8 footprint.
        // note: footprint and it's methods are NOT game specific but 1st class constructs of KGB
        // TODO: should footprints be of various sizes based on celledregion's cellsize?
        // this way exterior celledregions can use much larger terrain tiles 

        public PhysicalProperties PhysicalProperties;

        //             I think state is a type 'object' known by scripts only 
        public object State; // game state



        public Segment(uint location)
            : this()
        {
            Location = location;
        }

        public Segment()
        {
            Style = new EdgeStyle();
        }


        public void Read(System.IO.BinaryReader reader)
        {
            Location = reader.ReadUInt32();

            InteriorAtlasTextureIndex = reader.ReadInt32();
            ExteriorAtlasTextureIndex = reader.ReadInt32();
            Style.Read(reader);
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(Location);
            // TODO: if we're collapsing the floor tile to have a hole in the floor, is it not ok to make altas textreindex -1?
           // System.Diagnostics.Debug.Assert(InteriorAtlasTextureIndex >= 0);
           // System.Diagnostics.Debug.Assert(ExteriorAtlasTextureIndex >= 0);
            writer.Write(InteriorAtlasTextureIndex);
            writer.Write(ExteriorAtlasTextureIndex);
            Style.Write(writer);
        }
    }

}
