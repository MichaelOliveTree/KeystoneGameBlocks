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
    
    /// <summary>
    /// Components allow Users to essentially extend a Keystone.Entity to a necessary game-specific
    /// object.
    /// </summary>
    public class UserComponents
    {
        // Each Entity with a BlackboardData object instance, has an "object[]"
        // of Memory<T> for each intrinsic "interface" the Entity/Component requires.
        // For instance, "TransformInterface" "BoundedInterface" "PhysicsInterface"
        // CollissionsInterface, etc.
        // AS WELL AS each game-specific "interface" such as "ComponentBase", "Weapon", "EnergyWeapon"
        public Memory<T> GetDataByInterface (Type t)
        {
            // NOTE: for EntitySystems, the EntitySyste.cs implementation will
            //       serialize/deserialize the data from an entire array of Entities 
            //       data within the IEntitySystem
            //       For single Entities, we will allow the Serialization/Deserialization
            //       to occur as normal using our Settings.PropertySpec.cs Properties and CustomProperties
            public Memory<Weapon[]> Weapons = new Weapon[128];
            
            // todo: fill Weapons array with all weapons used in game for any ship owned by any player or faction
            
            
            // test referencing and modifying a single element within Weapons
            int elementIndex = 32;
            int sliceLength = 1;
            
            // a slice will result in a new Memory<Weapon> object with a span of 0 to sliceLength
            Memory<Weapon> singleWeapon = Weapons.Slice(elementIndex, sliceLength); // A Memory<T> representing the element at index 32
            
            // since sliceLength == 1, Weapons.Span[elementIndex] will work but Weapons.Span[elementIndex + 1] will be indexOutOfRangeException
            singleWeapon = Weapons.Span[elementIndex];
            
            
            // assigning a modified weapon to the singleWeapon.
            int damageTaken = 5;
            Weapon modifiedWeapon;
            modifiedWeapon = singleWeapon.Span[0];
            modifiedWeapon.CurrentHP -= damageTaken;
    
            // two ways to modify the data in the original Memory<Weapons[]>
            singleWeapon.Span[0] = modifiedWeapon;        // 1) modifying the shared element
            Weapons.Span[elementIndex] = modifiedWeapon;  // 2) modifying the struct at the specified index directly
            
        
        }
        
        public const int MAX_ARMOR_LAYERS = 5;
        public const int NUM_ARMOR_FACES = 6; //4 = front, back, left, right.  6 adds top, back.
        public struct Armor
        {
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
        
        public struct ComponentBase
        {
            public int Interfaces; // 32 bit flags for the various interfaces (Build and Runtime) used by this component
            public string EntityID; // Guid.NewGuid().ToString() results in a 36 character string.
            
            public float MaterialQuality;
            public float Craftsmanship;
            
            public float Cost;
            public float Weight;
            public float Volume;
            public float SurfaceArea;
            
            public ExternalStructure Defense; 
            public InternalStructure Internals; 
            
            //  build 
            public bool StatsChanged;
            public bool BuildChanged;
            
            // Get/SetProperties is only for GUI and for Serializing/Deserializing
            // NOTE: the underlying data is retrieved from the Memory<T> each struct holds
            //       for storing it's data directly to the continguous memory reserved for that type
            public Settings.PropertySpec[] GetProperties(bool valuesOnly)
            {
                
            }
            
            public void SetProperties (Settings.PropertySpec[] properties)
            {
                
            }
        }
        
        public struct Weapon : ComponentBase
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
}