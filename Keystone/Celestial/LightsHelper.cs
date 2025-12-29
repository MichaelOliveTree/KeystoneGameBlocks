using Keystone.Types;
using System;
using System.Collections.Generic;


namespace Keystone.Celestial
{
    // TODO: all these need to be moved out of Keystone.Celestial
    public class LightsHelper
    {
        public static Keystone.Lights.DirectionalLight LoadDirectionalLight(float range)
        {
            Vector3d direction = new Vector3d(0.5f, -0.5f, 0.8f);
            Keystone.Lights.DirectionalLight light = new Keystone.Lights.DirectionalLight(Keystone.Resource.Repository.GetNewName(typeof(Keystone.Lights.Light)),
                direction, 1.0f, 1.0f, 1.0f, 1f, false);
            light.Range = range; // dir light range can allow us seperate dir lights per region?
            light.Specular = new Keystone.Types.Color(1, 1, 1, 1);
            light.SetEntityFlagValue("pickable", false);
            return light;
        }



        // camera position is always in current region coords
        public static Keystone.Lights.PointLight LoadPointLight(Keystone.Portals.Region region, Vector3d pos)
        {
            Keystone.Lights.PointLight light = new Lights.PointLight(Keystone.Resource.Repository.GetNewName(typeof(Keystone.Lights.PointLight)));
            float range = 10; // float.MaxValue * .01f; // float.MaxValue * .5 is still too high.  .1 works so far

            // http://www.directxtutorial.com/Lesson.aspx?lessonid=9-4-9
            // There are three constant values in this equation, att0, att1 and att2. 
            // If you look, you will see that att0 is not multiplied by anything. 
            // This makes it a constant modifier. If you place a number in only this 
            // variable, you get a constant amount of light. This means there is no 
            // attenuation at all. For example, if you put 0.5 in this value, you 
            // will get a half-lit light that will extend all the way to the maximum
            // range of the light.
            float attenuation0 = 0.0f;
            // This is the second constant value. If used by itself, it is an inverse 
            // function, meaning that the light will dissipate more slowly as the 
            // distance increases. This is probably the most realistic type of light. 
            // Usually you can get away with just setting this to 1.0 and the other 
            // two values to 0.0. However, because of varying types of lights, this 
            // doesn't always work out.
            float attenuation1 = 0.0f;
            // This is the third constant value. If used by itself, it is an inverse 
            // square function, meaning that the light will not only dissipate more 
            // slowly as the distance increases, but the dissipation will be very rapid 
            // at first, then sharply slow down. This type of attenuation has the effect
            // of, say, a campfire at night. It is very bright around the campfire. But 
            // if you walk fifty feet away, you can still see objects lit by the fire, 
            // but very dimly. If you walk a hundred feet away, you'll still be able to 
            // see the light, and it probably won't get that much darker.
            float attenuation2 = 0.0f;

            //Random random = new Random();
            float r = 1.0f; // (float)random.NextDouble();
            float g = 1.0f;// (float)random.NextDouble();
            float b = 1.0f; // (float)random.NextDouble();
            float a = 1.0f;

            Keystone.Types.Color color = new Keystone.Types.Color(r, g, b, a);
            Keystone.Types.Color specular = new Keystone.Types.Color(1f, 1f, 1f, 1f);

            light.Range = range;
            light.SetAttentuation(new float[] { attenuation0, attenuation1, attenuation2 });
            light.Diffuse = color;
            light.Ambient = color;
            light.Specular = specular;

            light.Translation = pos;

            //KeyCommon.Messages.Node_Create_Request msg = new KeyCommon.Messages.Node_Create_Request("PointLight", region.ID);
            //msg.Add("range", typeof(float).Name, range);
            //msg.Add("color", color.GetType().Name, color);
            //msg.Add("specular", specular.GetType().Name, specular);
            //msg.Add("position", pos.GetType().Name, pos);
            //msg.Add("attenuation", typeof(float[]).GetType().Name, new float[] { attenuation0, attenuation1, attenuation2 });
            //AppMain.mNetClient.SendMessage(msg);
            return light;
        }

        public static Keystone.Lights.SpotLight LoadSpotLight (Keystone.Portals.Region region, Vector3d pos)
        {
           
            float range = float.MaxValue * .01f; // float.MaxValue * .5 is still too high.  .1 works so far
            float fallOff = 100000;
            float phi = 45;
            float theta = 15;

            //Random random = new Random();
            float r = 1.0f; // (float)random.NextDouble();
            float g = 1.0f;// (float)random.NextDouble();
            float b = 1.0f; // (float)random.NextDouble();
            float a = 1.0f;


            // camera position is always in current region coords
            Vector3d dir = new Vector3d(0.5f, -0.5f, 0.8f);

            Keystone.Lights.SpotLight light = new Lights.SpotLight(Keystone.Resource.Repository.GetNewName(typeof(Keystone.Lights.SpotLight)),pos, dir, r, g, b, a, phi, theta, false);

            Keystone.Types.Color color = new Keystone.Types.Color(r, g, b, a);
            Keystone.Types.Color specular = new Keystone.Types.Color(1f, 1f, 1f, 1f);

            light.Range = range;
            light.FallOff = fallOff;
  //          light.THeta
            //KeyCommon.Messages.Node_Create_Request msg = new KeyCommon.Messages.Node_Create_Request("SpotLight", region.ID);
            //msg.Add("range", typeof(float).Name, range);
            //msg.Add("color", color.GetType().Name, color);
            //msg.Add("specular", specular.GetType().Name, specular);
            //msg.Add("position", pos.GetType().Name, pos);
            //msg.Add("falloff", typeof(float).Name, fallOff);
            //msg.Add("theta", typeof(float).Name, theta);
            //msg.Add("phi", typeof(float).Name, phi);

            //AppMain.mNetClient.SendMessage(msg);

            return light;
        }
    }
}
