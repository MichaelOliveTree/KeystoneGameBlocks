using System;
using Keystone.Types;

namespace Keystone.Utilities
{
	/// <summary>
	/// Description of RandomHelper.
	/// </summary>
	public class RandomHelper
	{
		internal static Random mRandom = new Random();
		 
		public static bool RandomBool()
		{
			return RandomNumber (0,1) == 1;
		}
		
        ///////////////////////////////////////////////////////////////////
        // TODO: merge below which used to be old from Keystone.Math2.Math2
        public static Vector3d RandomRotation (Random random)
        {
            return new Vector3d(RandomNumber(random,0d, 360d)*DEGREES_TO_RADIANS, RandomNumber(random,0d, 360d)*DEGREES_TO_RADIANS,
                                RandomNumber(random, 0d, 360d)*DEGREES_TO_RADIANS);
        }
        public static Vector3d RandomVector (Random random, Vector3d min, Vector3d max)
        {
            Vector3d v;
            v.x = RandomNumber(random,min.x, max.x);
            v.y = RandomNumber(random,min.y, max.y);
            v.z = RandomNumber(random,min.z, max.z); 
            return v;   
        }

        public static double Clamp (double value, double min, double max)
        {
            if (value < min ) return min;
            return value > max ? max : value;
        }
        
        public static double RandomNumber(Random random, double min, double max)
        {
            return (max - min) * random.NextDouble() + min;
        }
        // TODO: merge above which used to be old from Keystone.Math2.Math2
         ///////////////////////////////////////////////////////////////////

		
		public static int RandomNumber (int min, int max)
		{
			return mRandom.Next (min, max);
		}
		
		public static double RandomNumber (double min, double max)
		{
			return RandomNumber (mRandom, min, max);
		}
				        
        public static double RandomNumber(Random random, double min, double max)
        {
            return (max - min) * random.NextDouble() + min;
        }
        
        public static double RandomDouble()
        {
        	return mRandom.NextDouble();
        }
                
        public static float RandomNumber(Random random, float min, float max)
        {
        	return (max - min) * RandomFloat() + min;
        }
        
		public static float RandomNumber (float min, float max)
		{
			return RandomNumber (mRandom, min, max);
		}
		
        public static float RandomFloat()
        {
        	return (float)mRandom.NextDouble();
        }
        
        public static Color RandomColor()
        {
        	return RandomColor(mRandom);
        }
        
        public static Color RandomColor(Random rand)
        {
            byte r, g, b, a;
           
            r = (byte)rand.Next (0, 255);
            g = (byte)rand.Next (0, 255);
            b = (byte)rand.Next (0, 255);
            a = 255; // rand.Next (0, 255); 
     
            return new Color(r,g,b,a);
        }
        
        public static Quaternion RandomQuaternion (double degreeRange)
        {
        	double max = 1.0 - InterpolationHelper.LinearMapValue (0, 360, degreeRange);
        	
        	double u1 = RandomNumber(0.0, max);
        	max = 1.0 - InterpolationHelper.LinearMapValue (0, 360, degreeRange);
            double u2 = RandomNumber(0.0, max);
            max = 1.0 - InterpolationHelper.LinearMapValue (0, 360, degreeRange);
            double u3 = RandomNumber(0.0, max);

            const double TWO_PI = 2 * Math.PI;

            double u1sqrt = Math.Sqrt(u1);
            double u1m1sqrt = Math.Sqrt(1-u1);
            double x = u1m1sqrt * Math.Sin(TWO_PI * u2);
            double y = u1m1sqrt * Math.Cos(TWO_PI * u2);
            double z = u1sqrt * Math.Sin(TWO_PI * u3);
            double w = u1sqrt * Math.Cos(TWO_PI * u3);

            return new Quaternion (x, y, z,w);
        }
        
        /// <summary>
        /// Generates a random rotation using an algorithm that should generate evenly distributed random rotations.
        /// http://planning.cs.uiuc.edu/node198.html
        /// </summary>
        /// <param name="rand"></param>
        public static Quaternion RandomQuaternion() 
        {
            double u1 = mRandom.NextDouble();
            double u2 = mRandom.NextDouble();
            double u3 = mRandom.NextDouble();

            const double TWO_PI = 2 * Math.PI;

            double u1sqrt = Math.Sqrt(u1);
            double u1m1sqrt = Math.Sqrt(1-u1);
            double x = u1m1sqrt * Math.Sin(TWO_PI * u2);
            double y = u1m1sqrt * Math.Cos(TWO_PI * u2);
            double z = u1sqrt * Math.Sin(TWO_PI * u3);
            double w = u1sqrt * Math.Cos(TWO_PI * u3);

            return new Quaternion (x, y, z,w);
        }
        

        /// <summary>
        /// Spherical produces much better results
        /// </summary>
        /// <param name="rand"></param>
        /// <returns></returns>
        public static Vector3d RandomVector(Random rand)
        {
            // get a random angle between 0 and 2pi (0 and 360)
            double theta = rand.NextDouble() * 360;
            return RandomVector (rand, theta);
        }

        public static Vector3d RandomVector ()
        {
        	return RandomVector (mRandom, 360);
        }
                
        // TODO: this Random() results in more uniform distribution for starfield and motion field generation than "RandomVector" which we are using for particle system
        public static Vector3d RandomUnitSphere() 
        {
            Vector3d result;
            result.x = mRandom.NextDouble() * 2.0d - 1.0d;
            result.y = mRandom.NextDouble() * 2.0d - 1.0d;
            result.z = mRandom.NextDouble() * 2.0d - 1.0d;
            result.Normalize();
            return result;
        }
        
        public static Vector3d RandomVector (Vector3d value, double degreeRange)
        {
        	return value; // TODO: this code should be faster and also actually work.  below is fubar
        	
        	// what we want to do here is transform this value by a random rotation within
        	// the degree range on all rotation axis
//        	Quaternion rotation = RandomHelper.RandomQuaternion (degreeRange);
        	
//        	Vector3d result = Vector3d.TransformNormal (value, rotation);
//       	return result;
        	//return Vector3d.Normalize(value + RandomHelper.RandomVector(degreeRange));
        }
        
        public static Vector3d RandomVector (double degreeRange)
        {
        	return RandomVector (mRandom, degreeRange);
        }
        
        public static Vector3d RandomVector(Random rand, double degreeRange)
        {
        
        	return Vector3d.Normalize (RandomQuaternion(degreeRange).GetEulerAngles (false));
// BROKEN - seems to produce random yaw only, no pitch or roll
//        	if (degreeRange > 360.0f) 
//        		degreeRange = 360.0f;
//        	else if (degreeRange < 0) degreeRange = 0;
//        	
//        	double thetaRadians = degreeRange * Utilities.MathHelper.DEGREES_TO_RADIANS;
//        	
//            // get a random value between -1 and +1  
//            double u = -1 + rand.NextDouble() * 2;
//            System.Diagnostics.Debug.Assert(u >= -1d && u <= 1d);
//
//            double x, y, z;
//            double uSquared = u * u;
//
//            x = Math.Cos(thetaRadians) * Math.Sqrt(1 - uSquared);
//            y = Math.Sin(thetaRadians) * Math.Sqrt(1 - uSquared);
//            z = u;
//
//            
//            Vector3d vec;
//            vec.x = x;
//            vec.y = y;
//            vec.z = z;
//            vec.Normalize();
//            
//            return vec;
        }
        
        // pass in a Random number object so that it's already seeded. If we create it below each time, it will generate the same vector
        public static Vector3f RandomVector3f()
        {
        	return RandomVector3f(mRandom);
        }
        
        public static Vector3f RandomVector3f(Random rand)
        {
            Vector3f result = new Vector3f((float) rand.NextDouble() * 2 - 1, 
        	                               (float) rand.NextDouble() * 2 - 1,
                                           (float) rand.NextDouble() * 2 - 1);
        	return Vector3f.Normalize (result);
        }

        public static Vector3d[] RandomSphericalPoints(int count, float radius)
        {
            Vector3d[] points = new Vector3d[count];

            // TODO: would be nice if we could limit results to being within FOV
            for (int i = 0; i < count; i++)
            {
                points[i] = Utilities.RandomHelper.RandomUnitSphere() * radius;
            }

            return points;
        }

    }
}
