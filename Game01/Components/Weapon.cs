namespace Game01.Components
{
    public struct Weapon
    {
        // build 
        public float Bore;
        public int BarrelLength;
                
        // stats
        public int RoF;
        public int DamageType;
        public int Damage;
        public int HalfDamage;
        public float Range;
        public float Accuracy;
        public float Malfunction; // 0.0 - 1.0f coefficient for tendancy to malfunction. MaterialQuality and Craftsmanship have impact
        
        
        // runtime flags
        public bool IsFiring;
        public bool IsReloading;
        public bool IsUnJamming; // represents fix of minor malfunction... does not require a "repair"
        public bool IsPowered;
        public bool IsHealthy;
        
        // nested weapon.  
        public Weapon SecondaryWeapon;
        
    }
}