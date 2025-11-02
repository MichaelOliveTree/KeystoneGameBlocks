using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game01.GameObjects
{
    public  class Factory
    {
        /// <summary>
        /// Called server side to generate an ID
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static KeyCommon.DatabaseEntities.GameObject Create(string typeName)
        {
            long id = -1;
            return Create(id, typeName);
        }


        public static KeyCommon.DatabaseEntities.GameObject Create(long id, string typeName)
        {
            KeyCommon.DatabaseEntities.GameObject gameObject = null;

            switch (typeName)
            {
                case "User":
                    gameObject = new KeyCommon.DatabaseEntities.User(id);
                    break;
                // i think HelmState is never serialized.  It's just cache var to speed up
                // scripting 
                //case "HelmState":
                //    gameObject = new Game01.GameObjects.HelmState(id);
                case "NavPoint":
                    gameObject = new Game01.GameObjects.NavPoint(id);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return gameObject ;
        }
    }
}
