namespace Game01.Components
{
    // NOTE: Memory<T> should be useable with .NET 4.8 framework and downloading the Nuget package 
    // 
    // if you are working with older .NET Framework projects (like 4.8) or need to target a 
    // specific older version of .NET where these types might not be natively available, you might
    // need to install the System.Memory NuGet package.
    // 
    // To install the System.Memory NuGet package (if necessary):
    // Using the NuGet Package Manager Console in Visual Studio:
    // Code
    //    Install-Package System.Memory
    // Using the .NET CLI.
    // Code
    //      dotnet add package System.Memory
    // Using the NuGet Package Manager UI in Visual Studio:
    // Right-click on your project in Solution Explorer and select "Manage NuGet Packages...".
    // Search for "System.Memory".
    // Select the package and click "Install".
    // Note: Always ensure you are using the appropriate version of the System.Memory 
    // package that is compatible with your project's target framework. For most modern 
    // .NET development, explicit installation of System.Memory is not required as it's 
    // part of the core libraries.

    
    // 1 - we want game01.dll and maybe the entity scripts to be the only places with access to component structs
    // 2 - we want to be able to compute results from Rules defined in game01.dll and then update the data
    //     by just operating and iterating over the inherently cache friendly continguous Memory<T[]> as opposed to 
    //     iterating on this game specific data via each Entity.Update() seperately.
    //     Each Entity object will have shared access to the structs that define it's game data however, 
    //     but fast computation of rules and results are done via iterating the Memory<T[]> all at once.
    //     a) So what does this require? 
    //        - well, if changes to certain values occur via Entity.Update(), which for most Entities that are not
    //          currently moving, does not occur every frame, then the update of calculations
    //          for Stats, should occur 
    //        - we haven't really looked at things like Bufs and such, but here's a good page. i'll save the .pdf of the webpage todo
    //          https://discussions.unity.com/t/buff-debuff-system-for-ecs/868504/2
    //          "If you can imagine an RTS game where all damage is handled through stat change events, 
    //          entity events would be causing large-scale structural changes almost all the time during battles.
    //          Even worse for a game like Factorio/Mindustry/Satisfactory/OxygenNotIncluded where the many stats 
    //          of every building/object would be fluctuating constantly every frame"
    //          - My response is, the above clearly needs to operate on a "change" propogation system like we use for our 
    //            SceneGraph as a whole.
    
    // 3 - the PropertySpec[] allowed KeyEdit and Keystone to not care at all about game01.dll types / structs
    // 

            /*
        Build interfaces
        
        - PRODUCTION is primary role of our components. Everything that serves
          them, is ultimatel for their production
          
        Runtime interfaces
        - table - perhaps produces an aesthetic bonus of sorts eg +0.2 morale
        - chair or bunk
        - electric powered 
            - sensors 
            - computers
            - stations
        - fuel powered
            - propulsion - engines
            - propulsion - rockets
            - power generators 
        - Hyperdrive / JumpDrive / StarDrive
        */

    public struct Build_Struct
	{
		public string PersistString;
		
		
		public string Serialize()
		{
			// javascript object notation
			Laser_Struct laser = new Laser_Struct();
			// TODO: test whether this saves all the different types of data we need with our complex structs/class properties
			string persistedString = System.Text.Json.JsonSerializer.Serialize(laser);
			
			return persistedString;
		}
		
		public bool Deserialize(string persistString)
		{
			return true;
		}	
		
	}

    public struct Component  // aka: "Useable Component"
    {
        public int Interfaces; // 32 bit flags for the various interfaces (Build and Runtime) used by this component
        public string EntityID; // Guid.NewGuid().ToString() results in a 36 character string.
                
        public bool RequiresOperator;
		public int OperatorsRequired;
		public string[] OperatorIDs;
		
        public float MaterialQuality;
        public float Craftsmanship;
        public bool Repairable; 

        public ExternalStructure Defense; 
        public InternalStructure Internals; 
		public Armor Armor;

		public Production[] Production;   // eg. even a painting on a wall can produce +0.2 aesthic bonus to morale or happiness to crew
		public Consumption[] Consumption; // eg. all components can consume damage.  
					      
        // stats
        public int Hitpoints;
        public int Damage;
        public int PD;
        public int DR;

        public float Cost;
        public float Weight;
        public float Volume;
        public float SurfaceArea;


        // runtime
        public bool InUse;
		public float StartTime;
		public float Duration;
		public bool Looping; // Repeating
		public float CooldownDuration; 
		public bool InCoolDown;
		
        public delegate void OnCreate();  // or OnAddedToScene()
        public delegate void OnDestroy(); // or OnRemovedFromScene()
		public delegate void OnUseStarted();
		public delegate void OnUseEnded();

		        
        //  build 
        public string BuildPersistString;
        public bool StatsChanged;
        public bool BuildChanged;


		public void Use(string entityID)
		{
 		}


        
        // Get/SetProperties is only for GUI and for Serializing/Deserializing
        // NOTE: the underlying data is retrieved from the Memory<T> each struct holds
        //       for storing it's data directly to the continguous memory reserved for that type
        public Settings.PropertySpec[] GetProperties(bool valuesOnly)
        {
            
        }
        
        public void SetProperties (Settings.PropertySpec[] properties)
        {
            
        }

        public string ToString()
		{
		}

		public void FromString(string parseableString)
		{
			
		}
    }
    
}