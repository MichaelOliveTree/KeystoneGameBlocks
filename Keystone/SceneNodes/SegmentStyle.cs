using System;

namespace Keystone.SceneNodes
{

    /// <summary>
    /// This is only used for serialization of the command
    /// over the wire and then we store CornerStyle and EdgeStyle's in seperate dictionaries.
    /// CornerStyle and EdgeStyles are stored seperately on disk for saved interior databases.
    /// </summary>
    public class SegmentStyle
    {
        public CornerStyle CornerStyle;
        public EdgeStyle EdgeStyle;

        // Network I/O
        public void Read(Lidgren.Network.NetBuffer buffer)
        {
            CornerStyle = new CornerStyle();
            CornerStyle.Read(buffer);
            EdgeStyle = new EdgeStyle();
            EdgeStyle.Read(buffer);
        }

        public void Write(Lidgren.Network.NetBuffer buffer)
        {
            CornerStyle.Write(buffer);
            EdgeStyle.Write(buffer);
        }

        // NOTE: No File I/O methods because CornerStyle and EdgeStyles are stored
        // seperately on disk.  We only need the Network Read/Write methods to 
        // send user edge painting commands over the wire.
    }


    /// <summary>
    /// Styles are stored within "Segment"s to disk for saving wall and corner
    /// visuals.
    /// Corner segments cannot be represented within SegmentStyle because
    /// corners can be shared between adjacent Segments so any "style" settings
    /// within SegmentStyle to represent a Corner's style could conflict
    /// with adjacent Segment's SegmentStyle
    /// </summary>
    public class CornerStyle
    {
        protected int mStyleID;
        public string PrefabPath;
        //public int ModelIndex;   // the sub-model index under the ModelSelector. But wait, don't we dynamically compute the sub-model to use based on adjacent walls?
        // Corners and Wall meshes must have only 1 group so that they can be used in TVMinimesh
        public string TexturePath; // these can override the textures of the prefab
        public string MaterialID; // these can override the material of the prefab
        public int[,] Footprint; // todo: are corner pieces comprised of two parts just like the Edge? an inner and outer? inner and outer are relative though.
                                 // but i think for v1.0 we decided to just go with one square corner piece, unless it's adjacent to out of bounds cell.
                                 // still not sure how we auto-compute footprints.  Didn't we try this before?  walls and corner pieces would be far less
                                 // complex than other ship board components like engines and reactors and chairs, etc.  
                                 // How might we do this?  We could go along the bounding volume and test every 1/16th to see if a 2D point
                                 // is contained by a Tile using just X and Z dimensions.  But this would fail for edge segments that contain
                                 // doors.  
        public int StyleID { get { return mStyleID; } set { mStyleID = value; } }


        // Network I/O
        public void Read(Lidgren.Network.NetBuffer buffer)
        {
            mStyleID = buffer.ReadInt32();

            string typename;
            PrefabPath = buffer.ReadString();
            TexturePath = buffer.ReadString();
            MaterialID = buffer.ReadString();
            
            // TODO: here if the footprint is null, we have an error so we need
            // a flag to say if the footprint is there or not
            bool footPrintDataAvailable = (bool)buffer.ReadBoolean();
            if (footPrintDataAvailable)
                Footprint = (int[,])KeyCommon.Helpers.ExtensionMethods.ReadType(buffer, out typename);
            footPrintDataAvailable = (bool)buffer.ReadBoolean();
        }

        public void Write(Lidgren.Network.NetBuffer buffer)
        {
            buffer.Write(mStyleID);

            buffer.Write(PrefabPath);
            buffer.Write(TexturePath);
            buffer.Write(MaterialID);

            bool footPrintDataAvailable = Footprint != null;
            buffer.Write(footPrintDataAvailable);
            if (footPrintDataAvailable)
                KeyCommon.Helpers.ExtensionMethods.WriteType(buffer, Footprint);
        }

        // File I/O
        public void Read(System.IO.BinaryReader reader)
        {
            mStyleID = reader.ReadInt32();

            string result = reader.ReadString();
            if (string.IsNullOrEmpty(result) == false)
                PrefabPath = result;

            result = reader.ReadString();
            if (string.IsNullOrEmpty(result) == false)
                TexturePath = result;

            result = reader.ReadString();
            if (string.IsNullOrEmpty(result) == false)
                MaterialID = result;

            Footprint = KeyCommon.Helpers.ExtensionMethods.Read2DInt32Array(reader);
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(mStyleID);

            if (PrefabPath == null)
                writer.Write("");
            else
                writer.Write(PrefabPath);

            if (TexturePath == null)
                writer.Write("");
            else
                writer.Write(TexturePath);

            if (MaterialID == null)
                writer.Write("");
            else
                writer.Write(MaterialID);

            KeyCommon.Helpers.ExtensionMethods.Write2DInt32Array(writer, Footprint);
        }
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Seperate prefab, textures and materials can be used for each side of double sided walls. 
    /// </remarks>
    public class EdgeStyle 
    {
        // TODO: edge segments have limited style auto-tiling capabilities.  could we add 
        //       that to this class? like which Meshes get used for the various cases?
        //       NOTE: the bottom left and top right mesh paths are because our walls are
        //       potentially double-sided.  That is, they can have a different mesh on either
        //       side, but im thinking of cutting that feature.  The meshes may all be the same
        //       but the color and perhaps in the future, the thickness can be variable.
        // TODO: Shouldn't instead of MeshPath I make these PrefabPaths?  We can keep the texture and material id's for
        //       overrides.  Furthermore, if walls and corners are always seperate prefabs, we can draw out the
        //       footprints in the component editor and not have to dynamically compute them.
        protected int mStyleID; // TODO: do we need different styleID's for each side of the wall?

        //public string PrefabPathTopRight; 
        public string Prefab;
        // TODO: for a given style, we do we need to select which ModelSelector sub-model index is being used?
        //       the footprint created does depend on the sub-model that's selected.  But if we're dynamically
        //       generating the footprints based on the selected sub-model, there's probably no need to have them.
        //       So the question is, when do we determine which sub-model is used?  I think our sub-models must
        //       always be in the same order for all Wall segments... so we can use constants to pick the correct
        //       one based on cell location, door existance, window existance, etc.
 //       public int ModelIndexTopRight; // don't we dynamically select the sub-model based on existance of things like door components existing on a particular edge?
 //       public int ModelIndexTopLeft; // selector node model indices can be different for each side of a wall even if using the same prefab for both sides.
        //public string TopRightMeshPath;
        //public string BottomLeftMeshPath;
        public int CeilingAtlasIndex;
        public int FloorAtlasIndex;
        public int[,] TopRightFootprint;      // TODO: if we dynamically compute the footprint based on the sub-model under the ModelSelector, we don't need to cache them here
        public int[,] BottomLeftFootprint;
        public string TopRightMaterialID;      // materials can only be different for different mesh styles.  otherwise only color can be changed on minis BottomLeftColor, TopRightColor
        public string BottomLeftMaterialID;
        
        // all  // path to the wall used when their is a ceiling above and below
        //      // or if no ceiling, then no wall above.  
        //      // note: if no floor, then a wall cannot be placed.  Remember, we unlike
        //      // sims or FPSCreator are enforcing architectural rules.
        //      //
        // middle
        // down
        // up
        //  - 
        // interior_left_t
        public int StyleID { get { return mStyleID; } set { mStyleID = value; } }



        // Network I/O
        public void Read(Lidgren.Network.NetBuffer buffer)
        {
            mStyleID = buffer.ReadInt32();

            string typename;
            // texture atlas assigned to Floor and Ceiling is currently hardcoded in CelledRegion.LoadStructureVisuals()
            Prefab = buffer.ReadString();
            FloorAtlasIndex = buffer.ReadInt32();
            CeilingAtlasIndex = buffer.ReadInt32();

            bool footPrintDataAvailable = (bool)buffer.ReadBoolean();
            if (footPrintDataAvailable)
            {
                BottomLeftFootprint = (int[,])KeyCommon.Helpers.ExtensionMethods.ReadType(buffer, out typename);
            }

            footPrintDataAvailable = (bool)buffer.ReadBoolean();

            if (footPrintDataAvailable)
                TopRightFootprint = (int[,])KeyCommon.Helpers.ExtensionMethods.ReadType(buffer, out typename);
        }

        public void Write(Lidgren.Network.NetBuffer buffer)
        {
            buffer.Write(mStyleID);

            buffer.Write(Prefab);
            // texture atlas assigned to Floor and Ceiling is currently hardcoded in CelledRegion.LoadStructureVisuals()
            buffer.Write(FloorAtlasIndex);
            buffer.Write(CeilingAtlasIndex);

            bool footPrintDataAvailable = BottomLeftFootprint != null;
            buffer.Write(footPrintDataAvailable);
            if (footPrintDataAvailable)
                KeyCommon.Helpers.ExtensionMethods.WriteType(buffer, BottomLeftFootprint);

            footPrintDataAvailable = TopRightFootprint != null;
            buffer.Write(footPrintDataAvailable);
            if (footPrintDataAvailable)
                KeyCommon.Helpers.ExtensionMethods.WriteType(buffer, TopRightFootprint);
        }

        // File I/O
        public void Read(System.IO.BinaryReader reader)
        {
            mStyleID = reader.ReadInt32();

            // texture atlas assigned to Floor and Ceiling is currently hardcoded in CelledRegion.LoadStructureVisuals()

            string result = reader.ReadString();
            if (string.IsNullOrEmpty(result) == false)
                Prefab = result;

            FloorAtlasIndex = reader.ReadInt32();
            CeilingAtlasIndex = reader.ReadInt32();

            if (FloorAtlasIndex != -1 && CeilingAtlasIndex != -1)
            {
                BottomLeftFootprint = KeyCommon.Helpers.ExtensionMethods.Read2DInt32Array(reader);
                TopRightFootprint = KeyCommon.Helpers.ExtensionMethods.Read2DInt32Array(reader);
            }
        }

        public void Write(System.IO.BinaryWriter writer)
        {
            writer.Write(mStyleID);

            // texture atlas assigned to Floor and Ceiling is currently hardcoded in CelledRegion.LoadStructureVisuals()

            if (Prefab == null)
                writer.Write("");
            else
                writer.Write(Prefab);

            writer.Write(FloorAtlasIndex);
            writer.Write(CeilingAtlasIndex);

            if (FloorAtlasIndex != -1 && CeilingAtlasIndex != -1)
            {
                KeyCommon.Helpers.ExtensionMethods.Write2DInt32Array(writer, BottomLeftFootprint);
                KeyCommon.Helpers.ExtensionMethods.Write2DInt32Array(writer, TopRightFootprint);
            }
        }
    }

}
