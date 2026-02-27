namespace Game01.Builders
{


	public struct Build_Laser : IBuilder
	{
        // build specific LASER properties



        // struct for component properties and stats
        Game01.Components.Component component; 

        // struct for basic weapons properties
        Game01.Components.Weapon weapon;

        // struct for laser specific weapon properties
        Game01.Components.Laser_Struct laser;


#region IBuilder implementation
        public void Update()
        {
            
        }


        public string ToString (IBuilder b)
        {
            // NOTE: we only need to write out the build parameters and from that we can
            //       reconstitute the full entity

            public string PersistString;

			Build_Laser buildLaser = (Build_Laser)b;

            // JSon == javascript object notation
			string persistedString = System.Text.Json.JsonSerializer.Serialize(buildLaser);
            return persistedString;
        }

        public IBuilder FromString (string persistString)
        {
            
            // NOTE: we only need the build parameters and from that we can
            //       create the full entity
            Game01.Components.Weapon.Laser_Struct laser;



        }
#endregion
	}
}