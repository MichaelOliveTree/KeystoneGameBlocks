namespace Game01.Components
{
   
    public struct Armor
    {
        public const int MAX_ARMOR_LAYERS = 5;
        public const int NUM_ARMOR_FACES = 6; //4 = front, back, left, right.  6 adds top, back.

        public ArmorFace[] Faces;
    }
    
    
    public struct ArmorFace
    {
        public bool RAP;  // reactive armor plate
        public bool Electrified;
        public bool ThermalCoating;
        public bool RadShielding;
        public string ReflectiveCoating;  // todo: what types are there? see gvd // todo:  need enums or perhaps a coefficient value instead AND THE GUI can interpet this coefficient into a string if desired
        public int PD; 
        public int DR;   //<--- todo: need more space? DR is cumlative in the "Face" since it adds all layer's DR
        public float SurfaceArea;
        public float Weight;
        public float Cost;

    }
    
    public struct ArmorLayer
    {
        public string Material;   // material type e.g metal // todo; need enums
        public string Quality;    // material quality e.g. "cheap"  // todo:  need enums or perhaps a coefficient value instead AND THE GUI can interpet this coefficient into a string if desired
        public int DR;
        public float Weight;
        public float Cost;   
    }
        
}