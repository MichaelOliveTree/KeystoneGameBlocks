namespace Game01.Builders
{


    public class BuildFactory
    {

        public static IBuilder Build (Type t, string persistString)
        {
            return Build (t.Name, persistString);
        }

        public static IBuilder Build (string typeName, string persistString)
        {
            IBuilder result = null;

            switch (typeName)
            {

                case "laser":
                    result = Builders.Build_Laser.FromString(persistString);

                    break;
                default:
                    throw new Exception ("Game01.Builders.BuildFactory.Build() - Unsupported type '" + typeName + "'");

            }

            return result;

        }

    }

}