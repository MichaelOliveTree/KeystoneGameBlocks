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



    public struct Laser_Struct
	{
		// common component properties
		public int TL;

		public float Quality_;  // a coefficient with 1.0f being finely crafted and 0.0 being barely MacGuyvered together and may only last one shot
		//public string Quality; // todo: this needs to be a coefficient of 0.0 to 1.0


		public bool Ruggedized;

		// common component stats 
		public int HitPoints; // from LivingEntity
		public double Cost;
		public double Weight;
		public double SurfaceArea;
		public double Volume;
		public int DR;
		// public int PD;       // See Google AI Overview in Game01.Components.Armor.cs 

		// beam specific
		public int Type;       // type is really just about what types of Damage(s) (ProductID(s)) it results in such as Paralysis, Crushing, Burning, Impaling
		public float Duration;   // duration in seconds

				
		public float BeamOutput;    // what is the difference between this and kW of power... is it the convsion rate of the input power to the output power?
		public bool EnergyDrill;
		public bool FTL;
		public bool Reliable;
		public bool Compact;


		

		public int Accuracy;
		public int SnapShot;
		//			public string Shots;

		public float CyclicRate;
		public double CoolDown_;              // computed directl from CycleRate or RateOfFire
		//			public string RoF;

		public double PowerReqt;
		public float Malfunction_ ; // 0 to Malfunction with 1.0 being maximum meaning it would malfunction every time and 0.0f never.
		//public string Malfunction; // TOOD: Need an ENUM or logarithmic value? or 

		//			
		//			public string Mount;
		//			public string Direction;

		// TODO: these are like "internal" items and can be used if another power source is no longer connected
		//			public string PowerCellType;  // TOOD: Need an ENUM
		//			public int PowerCellQuantity;
		//			public double PowerCellWeight;

		// https://panoptesv.com/RPGs/Equipment/Weapons/BeamWeapons.php?HR=0
		// https://gamedev.stackexchange.com/questions/148961/how-to-design-a-damage-formula-in-an-rpg-which-keeps-weapons-with-different-atta
		public DAMAGE_TYPE TypeDamage;     // TOOD: Need an ENUM
		//public string Damage;         // this is dice of damage, but often contains a multiplier like (100) afterwards.  We don't need the multiplier since we just compute a min/max damage range or maybe we compute a single damage that then gets modified based on the target evasive maneuvers and such
		public int AverageDamage;       
		//			public double KEDamage;
		//			public double HalfDamage; 
		//			public double VacuumHalfDamage;


		//			public string Range; // string description of range (eg: "very long range")
		public double MaxRange;          // distance in meters
		//			public double MaxRange2;
		//			public double VacuumMaxRange;
		//			public double VacuumMaxRange2;
	}
}