using System;


namespace Game01
{
    public static class Enums
    {
        public enum UserMessage : int
        {
            None = KeyCommon.Messages.Enumerations.UserMessages, // NOTE: this enum starts at 99999
            Game_PerformRangedAttack,
            Game_AttackResults

        }

        public enum COMPONENT_TYPE : uint
        {
            NONE = 0,
            FLOOR,
            CEILING,
            WALL,
            // the above members represent interior _structure_ and 

            DOOR,
            WINDOW,
            HATCH,
            LADDER,
            STAIRS,
            LIFT,
            
            // superstructures
            HULL_PRIMARY = 250,
            HULL_SUPERSTRUCTURE,
            HULL_FULL_TURRET,
            HULL_HALF_TURRET,
            HULL_ENGINE_POD,
            


            COMPONENT = 500, // any value higher than this is a component
            STATION_HELM,
            STATION_TACTICAL,
            STATION_SCIENCE,
            STATION_COMMUNICATIONS,
            STATION_ENGINEERING,
            STATION_SECURITY,
            STATION_MEDICAL,
            STATION_DAMAGE_CONTROL,
            STATION_OPERATIONS,
            STATION_COMMAND,
            STATTION_PERSONAL, // personal stations typically within crew quarters.  +1 to morale when crew can communicate with family back home
            CHAIR,
            BUNK,
            DOUBLE_BUNK,
            SOFA,

            WEAPON = 1000,
            WEAPON_LASER = 1001,
            WEAPON_RAILGUN,
            WEAPON_GAUSS_GUN, // also known as a COILGUN
            WEAPON_TURBO_LASER ,

            WEAPON_MISSILE_LAUNCHER = 1500,
            WEAPON_ROTARY_MISSILE_MAGAZINE,

            COUNTER_MEASURE_CHAFF = 1600,
            COUNTER_MEASURE_FLARE,

            SENSOR = 2000, // sensors are generally on the exterior of the vehicle and controlled via a STATION
            SENSOR_OPTICAL,
            SENSOR_OPTICAL_ENHANCED, // aka: night vision is a pasive sensor
            SENSOR_RADAR,
            SENSOR_INFRARED,
            SENSOR_LIDAR,
            SENSOR_TACHYON,
            SENSOR_NEUTRINO,

            STORAGE = 3000,
            STORAGE_FUEL,
            STORAGE_WATER,
            STORAGE_AIR,

            POWERGENERATOR,
            REACTOR = 4000,
            BATTERY = 4100,
            

            ENGINE,
            THRUSTER, // maneuvering thrusters

            

            COMPUTER
        }

    }
}
