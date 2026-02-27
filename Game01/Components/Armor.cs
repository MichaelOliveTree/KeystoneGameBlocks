namespace Game01.Components
{
   
// NOTE: Feb.26.2026 - MichaelOliveTree - Placeholder notes about rules to verify code architecture for computing and applying gameplay rules.

/* In GURPS, Component DR and Armor DR represent two different layers of protection, particularly in vehicle design (GURPS Vehicles) and combat, where the structural, unarmored part of a vehicle interacts with added armor plating. 
Component DR (Structural/Material DR)
Definition: This is the natural protection provided by the material and structure of the vehicle component itself (e.g., the 1-inch thick steel hull, the aluminum skin of an aircraft, or the glass of a windshield).
Purpose: It represents the toughness of the object before any specialized armor is added.
Characteristics:
Low to Moderate: Generally lower than specialized armor (e.g., aluminum skin might be DR 5-10, steel plating DR 20-70 per inch).
Destructible: Once this DR is penetrated, the component (e.g., engine, fuel tank) takes damage and may be destroyed.
Material Specific: Based on thickness and material properties (steel, aluminum, ceramic, etc.). 
Steve Jackson Games Forums
Steve Jackson Games Forums
 +4
Armor DR (Applied Armor)
Definition: This is additional, specialized defensive material added to the component to increase its protection (e.g., bolted-on steel plates, Kevlar blankets, or composite ceramic armor).
Purpose: To stop weapon damage from reaching the underlying component or occupant.
Characteristics:
High: Usually much higher than the component it is protecting.
Ablative/Damaged: Specific types of armor (like composite/ceramic) may have ablative properties, losing effectiveness as they take hits.
Hardened: Often designed to resist Armor Divisor (AD) attacks better than raw component materials. 
Steve Jackson Games Forums
Steve Jackson Games Forums
 +4
Key Differences in Mechanics
Stacking: Armor DR is added on top of Component DR. A hit must penetrate the Armor DR first, and then the remaining damage must penetrate the Component DR.
Weight and Cost: Adding Armor DR significantly increases weight and reduces vehicle performance (speed, load capacity), whereas Component DR is inherent to the object's construction.
Damage Type Interaction: Component DR (e.g., glass) might be weak against crushing damage but strong against heat, while Armor DR (e.g., composite) might be strong against shaped charges (HEAT) but weaker against crushing.
Coverage: Armor can be applied only to specific hit locations (front, side, top), while the component DR covers the entire structure. 
Steve Jackson Games Forums
Steve Jackson Games Forums
 +4
Example: A light truck component (cab) might have DR 5 (steel skin). If you add +20 DR steel plating, the total DR is 25.
*/


/* Passive Defense - Key Aspects of Component PD in GURPS Vehicles:
Definition: PD acts as a bonus to the vehicle's evasion roll (Active Defense). Component PD is used when a specific part (like a turret, rotor, or sensor array) is targeted rather than the vehicle as a whole.
Armor System: Component armor is calculated based on the material's properties. Different materials provide different ratios of PD to DR. For example, some armor types might offer higher PD but lower DR, while others are the opposite.
Component Ruggedization: Vehicles can have "Ruggedized Components," which may impact how PD and DR are applied to sensitive systems like electronics.
Conversion to 4th Edition: In GURPS 4th Edition, Passive Defense (PD) was largely eliminated. When converting 3rd Edition vehicles, PD is usually ignored, and the focus shifts entirely to Damage Resistance (DR) and Hit Points (HP).
Examples: A component might have stats listed as "PD 3, DR 12". In a 4e context, this would likely be converted to DR 12 or higher, with the PD bonus

Passive Defense (PD) was eliminated in GURPS Fourth Edition primarily to correct math imbalances that made armored characters nearly impossible to hit and to simplify the combat system. 
Reddit
Reddit
 +2
Key reasons for its removal include:
Broken Math/Unrealistic Defense Stacking: In 3e, PD added directly to active defenses (Dodge, Parry, Block). This caused the probability of avoiding damage to increase exponentially rather than linearly. A heavily armored character often became too untouchable, with defense scores that made logical sense for a "glancing blow" but proved problematic in gameplay.
Encumbrance Paradox: PD allowed characters in heavy armor to have a higher effective Dodge than unarmored characters, despite having lower mobility, because the high PD bonus outweighed the penalty from encumbrance.
Redundancy with Damage Resistance (DR): The role of armor in GURPS is to stop damage, which is handled by Damage Resistance (DR). PD served as a "passive" way to avoid being hit at all, which was deemed unnecessary and confusing.
Separation of Shield/Armor Roles: 4e replaced general PD with Defense Bonus (DB) for shields. This correctly models a shield as an active tool a fighter uses to block, whereas armor simply reduces damage. 
Reddit
Reddit
 +4
In 4e, DR was adjusted in some cases to compensate for the loss of PD.
*/
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