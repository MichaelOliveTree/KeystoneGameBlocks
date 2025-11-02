using System;


namespace Keystone.Utilities
{
	/// <summary>
	/// Description of InterpolationHelper.
	/// </summary>
	public class InterpolationHelper
	{
		
		#region BezierCurves
		// see KeyStandardLibrary.Primitives.BezierCurve 
		// - the implementation there contructs keyframes at a given resolution
		// but I should generalize the function here first and then use that Primitives.BezierCurve for constructing spline primitive
		#endregion
        
   // c - Mapping a numeric range onto another - Stack Overflow https://share.google/nhvSCm1CltbJlzS05
   // https://stackoverflow.com/questions/5731863/mapping-a-numeric-range-onto-another
    public static int MapValue(float input_start, float input_end, float output_start, float outout_end, float input)
    {
         double slope = 1.0 * (output_end - output_start) / (input_end - input_start)
         
       //  output = output_start + slope * (input - input_start)
         
         output = output_start + round(slope * (input - input_start))
         
         return (int)output;
    }
    
    double round(double d)
    {
        double result = floor(d + 0.5);
        System.Diagnosticd.Debug.Assert (result == Math.Round(d));
        
        return result;
    }
        
    
		/// <summary>
        /// Returns a value between 0.0 and 1.0 that represents the ratio of the value between min & max 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float LinearMapValue(float min, float max, float value)
        {
           // if (value < min || value > max) throw new ArgumentOutOfRangeException();

            //k = (B' - A') / (A - B); 
            //d = A' - B * k;

            float desiredMax = 1.0f;
            float desiredMin = 0.0f;
            
            float k  =  (desiredMax - desiredMin) / (min - max);

            float d = desiredMin - max * k;

            float result = value * k + d;
            return result;
        }
        
		/// <summary>
        /// Returns a value between 0.0 and 1.0 that represents the ratio of the value between min & max 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double LinearMapValue(double min, double max, double value)
        {
           // if (value < min || value > max) throw new ArgumentOutOfRangeException();

            //k = (B' - A') / (A - B); 
            //d = A' - B * k;

            double desiredMax = 1.0d;
            double desiredMin = 0.0d;
            
            double k  = (desiredMax - desiredMin) / (min - max);

            double d = desiredMin - max * k;

            double result = value * k + d;
            return result;
        }
        // same as normal lerp only this takes into account lerping with values that
        // wrap at 360 degrees which would cause a sudden jump in the camera as we 
        // rotated past 360 or below 0.
        // http://scott-franks.blogspot.com/
        public static double LerpAngle(double unwrappedStartAngleDegrees, double wrappedDestinationAngleDegrees, double lerp)
        {
            double c, d;
             
            if (wrappedDestinationAngleDegrees < unwrappedStartAngleDegrees)
            {
                c = wrappedDestinationAngleDegrees + MathHelper.TWO_PI * MathHelper.RADIANS_TO_DEGREES;
                //c > nowrap > wraps
                d = c - unwrappedStartAngleDegrees > unwrappedStartAngleDegrees - wrappedDestinationAngleDegrees
                    ? Lerp(unwrappedStartAngleDegrees, wrappedDestinationAngleDegrees, lerp)
                    : Lerp(unwrappedStartAngleDegrees, c, lerp);

            }
            else if (wrappedDestinationAngleDegrees > unwrappedStartAngleDegrees)
            {
                c = wrappedDestinationAngleDegrees - MathHelper.TWO_PI * MathHelper.RADIANS_TO_DEGREES;
                //wraps > nowrap > c
                d = wrappedDestinationAngleDegrees - unwrappedStartAngleDegrees > unwrappedStartAngleDegrees - c
                    ? Lerp(unwrappedStartAngleDegrees, c, lerp)
                    : Lerp(unwrappedStartAngleDegrees, wrappedDestinationAngleDegrees, lerp);

            }
            else { return unwrappedStartAngleDegrees; } //Same angle already

            return MathHelper.WrapAngle(d);
        }

        public static float Lerp(float start, float end, float weight)
        {
            return start * (1.0f - weight) + weight * end;
        }

        public static double Lerp(double start, double end, double weight)
        {
            return start * (1.0d - weight) + weight * end;
        }

        //http://www.truevision3d.com/forums/showcase/procedural_terrain_heightmap_generation_with_erosion-t18085.0.html
        //You can mess around with the Pi and the 0.5 number for different results too
        // TODO: MPJ -but why is he using Acos here and not Cos?
        public double Cosine_Interpolate(float a, float b, float x)
        {
            double ft = x * Math.PI;
            double f = (1.0d - Math.Acos(ft)) * 0.5d;

            return a * (1.0d - f) + b * f;
        }
	}
}