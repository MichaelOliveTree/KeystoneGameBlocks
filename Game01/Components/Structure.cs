namespace Game01.Components
{
    public struct ExternalStructure
    {
        public Armor[6] Armor;
        public int Defense;     // Passive Defense is a type of defense that requires no active trying to defeat an attack against it
    }
    
    public struct InternalStructure
    {
        public int MaterialType;
        public float Strength;  // frame strength
        
        public bool Robotic;
    `   public bool Biomechanical;
        public bool Responsive;
        public bool LivingMetal;
        
        public byte SlopeLeft; // note: slope uses constants to represent 0, 30 or 60
        public byte SlopeRight;
        public byte SlopeFront;
        public byte SlopeBack;
        
        // todo: is this correct place to have streamlining?  It would have to be set individually for each subassembly?
        public string StreamLining; // todo:  need enums or perhaps a coefficient value instead AND THE GUI can interpet this coefficient into a string if desired
        // NOTE: hitpoints I think is fine for inanimate objects,
        //       but not good for living things. 
        //       https://www.youtube.com/watch?v=sMWMB9bjFGo
        public int HitPoints; 
        public int CurrentHP;
    }
                
        
}