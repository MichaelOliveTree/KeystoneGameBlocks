using System;

namespace Settings
{
    ///Class for simplifying getting the enumerated constant from a name and the enum constant type from the name.
    /// Mike Joseph May.14.2006
    public class EnumHelper
    {
        public static Int32[] EnumeratedValuesByType(Enum enumType)
        {
            return (int[])Enum.GetValues(enumType.GetType());
        }

        public static string EnumeratedMemberName(Enum enumType, int value)
        {
            return EnumerateMemberName(enumType.GetType(), value);
        }
        
        public static string EnumerateMemberName (Type enumType, int value)
        {
            try
            {
                return Enum.GetName(enumType, value);
            }
            catch
            {
                return "";
            }
        }


        public static object EnumeratedMemberValue(Type enumType, string name)
        {
           
            try
            {
                string[] names = Enum.GetNames(enumType);
                foreach (string s in names)
                    if (s == name) return Enum.Parse(enumType, name);
                
            }
            catch
            {
            }
            return null;
        }
        
        public static object EnumeratedMemberValue(Enum enumType, string name)
        {
            return EnumeratedMemberValue(enumType.GetType(), name);
            //try
            //{
            //    string[] names = Enum.GetNames(enumType.GetType());

            //    for (int i = 0; i < names.Length; i++)
            //    {
            //        if (names[i] == name)
            //            return Enum.Parse(enumType.GetType(), name);
            //    }
            //}
            //catch
            //{
            //}
            //return null;
        }
    }
}