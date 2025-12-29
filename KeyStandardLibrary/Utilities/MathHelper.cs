using System;
using Keystone.Types;


namespace Keystone.Utilities
{
    public class MathHelper
    {









        public const float Epsilon = float.Epsilon; // 1.0e-5f;
                                                    // Epsilon: A constant representing the lower threshold of single-precision accuracy.

        public const double DEGREES_TO_RADIANS = Math.PI / 180d;
        public const double RADIANS_TO_DEGREES = 180d / Math.PI;
        public const double PI_OVER_2 = Math.PI / 2d;
        public const double TWO_PI = Math.PI * 2d;











        public const float Epsilon = 1.0e-5f;
                // Epsilon: A constant representing the lower threshold of single-precision accuracy.


        public static double PiOver2
        {
            get { return PI_OVER_2; }
        }

    //    ''' <summary>
    //''' Simple quad mesh useful for FIXED 2d billboards (i.e. billboards that dont rotate to face the camera)
    //''' </summary>
    //''' <param name="height"></param>
    //''' <param name="width"></param>
    //''' <returns></returns>
    //''' <remarks></remarks>
    //Public Shared Function CreateQuadMesh(ByVal height As Single, ByVal width As Single) As MTV3D65.TVMesh
    //    Dim quadmesh As MTV3D65.TVMesh
    //    quadmesh = _Scene.CreateMeshBuilder()

    //    ' 6 vertices for our quad
    //    quadmesh.AddVertex(-width, 0, 0, 0, 1, 0, 0, 1)
    //    quadmesh.AddVertex(width, height, 0, 0, 1, 0, 1, 0)
    //    quadmesh.AddVertex(-width, height, 0, 0, 1, 0, 0, 0)

    //    quadmesh.AddVertex(-width, 0, 0, 0, 1, 0, 0, 1)
    //    quadmesh.AddVertex(width, 0, 0, 0, 1, 0, 1, 1)
    //    quadmesh.AddVertex(width, height, 0, 0, 1, 0, 1, 0)

    //    'Dim height, width As Single
    //    'height = 10 : width = 5

    //    'width = width * 0.5!
    //    'Dim quadmesh As MTV3D65.TVMesh = CreateQuadMesh(height, width)

    //    '' to give our grass depth, we will cross two more grass planes to make a single 18 vertex grass clump
    //    'Dim depth As Single = width
    //    '' this grass plane crosses perpendicular to the previous
    //    'quadmesh.AddVertex(0, 0, depth, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(0, height, -depth, 0, 1, 0, 1, 0)
    //    'quadmesh.AddVertex(0, height, depth, 0, 1, 0, 0, 0)
    //    'quadmesh.AddVertex(0, 0, depth, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(0, 0, -depth, 0, 1, 0, 1, 1)
    //    'quadmesh.AddVertex(0, height, -depth, 0, 1, 0, 1, 0)

    //    '' this one crosses at 45 degree angle to both the others
    //    'quadmesh.AddVertex(-width * 0.5!, 0, depth * 0.5!, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(width * 0.5!, height, -depth * 0.5!, 0, 1, 0, 1, 0)
    //    'quadmesh.AddVertex(-width * 0.5!, height, depth * 0.5!, 0, 1, 0, 0, 0)
    //    'quadmesh.AddVertex(-width * 0.5!, 0, depth * 0.5!, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(width * 0.5!, 0, -depth * 0.5!, 0, 1, 0, 1, 1)
    //    'quadmesh.AddVertex(width * 0.5!, height, -depth * 0.5!, 0, 1, 0, 1, 0)


    //    'quadmesh.WeldVertices()
    //    'm.CreateFromMesh(quadmesh)
    //    'quadmesh.Destroy()
    //    Return quadmesh
    //End Function

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
        // todo: MPJ -but why is he using Acos here and not Cos?
        public double Cosine_Interpolate(float a, float b, float x)
        {
            double ft = x * 3.1415927;
            double f = (1.0d - Math.Acos(ft)) * 0.5d;

            return a * (1.0d - f) + b * f;
        }

//http://www.truevision3d.com/forums/tv3d_sdk_65/rotation_and_normals-t18209.0.html
//        public TV_3DMATRIX BindMatrixToLand(TV_3DMATRIX InMatrix, TV_3DVECTOR Scale, TV_3DVECTOR Position)
//                {
//                    {
//                        TV_3DVECTOR TopVector = World.Landscape.GetNormal(InMatrix.m41, InMatrix.m43);
//                        TV_3DVECTOR TentativeForwardVector = new TV_3DVECTOR(InMatrix.m31, InMatrix.m32, InMatrix.m33);
//                        TV_3DVECTOR RightVector = new TV_3DVECTOR();
//                        TV_3DVECTOR ForwardVector = new TV_3DVECTOR();
//                        TVMathLibrary MathLibrary = new TVMathLibrary();
//                        MathLibrary.TVVec3Cross(ref RightVector, TopVector, TentativeForwardVector);
//                        MathLibrary.TVVec3Cross(ref ForwardVector, RightVector, TopVector);
//                        MathLibrary.TVVec3Normalize(ref RightVector, RightVector);
//                        MathLibrary.TVVec3Normalize(ref ForwardVector, ForwardVector);
//                        InMatrix.m11 = RightVector.x * Scale.x;
//                        InMatrix.m12 = RightVector.y * Scale.x;
//                        InMatrix.m13 = RightVector.z * Scale.x;
//                        InMatrix.m21 = TopVector.x * Scale.y;
//                        InMatrix.m22 = TopVector.y * Scale.y;
//                        InMatrix.m23 = TopVector.z * Scale.y;
//                        InMatrix.m31 = ForwardVector.x * Scale.z;
//                        InMatrix.m32 = ForwardVector.y * Scale.z;
//                        InMatrix.m33 = ForwardVector.z * Scale.z;
//                        InMatrix.m41 = Position.x;
//                        InMatrix.m42 = Position.y;
//                        InMatrix.m43 = Position.z;

//                    }

//I store the matrix information returned this routine and I apply teh matrix to the mesh with setmatrix array: _MiniMesh.SetMatrixArray(_DisplayAmount, FoilageMatrix);

        public static double DegreesToRadians(double degrees)
        {
            return degrees*DEGREES_TO_RADIANS;
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians*RADIANS_TO_DEGREES;
        }

        public static double Heading2DRadians(Vector3d vec)
        {
            // .NET already handles the various cases so we dont need to compensate using -vec.x or anything
            //double angle = (double)System.Math.Atan2(-vec.x, vec.z);
            //return -1 * angle;
            return (double) Math.Atan2(vec.x, vec.z);
        }

        public static double Heading2DDegrees(Vector3d vec)
        {
            return RadiansToDegrees(Heading2DRadians(vec));
        }




        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * Clamp(t);
        }

        public static double AU_To_Meters(double AU)
        {
            return AU * 149597892000.0D;
        }


        public static double TimeToRotate(double heading, double destinationHeading, double acceleration)
        {
            // https://stackoverflow.com/questions/72222504/calculate-spin-frequency-from-rotation-change-in-degrees
            return 2.5d;
        }

        // https://www.quora.com/How-do-I-find-velocity-with-distance-and-acceleration?no_redirect=1
        public static double GetVelocityAtDistance(double distance, double acceleration)
        {
            return Math.Sqrt (2d * acceleration * distance);
        }

        public static double DegreesToRadians(double degrees)
        {
            return degrees * DEGREES_TO_RADIANS;
        }

        public static double RadiansToDegrees(double radians)
        {
            return radians * RADIANS_TO_DEGREES;
        }
        
        public static Types.Vector3d Floor (Types.Vector3d v)
        {
        	Types.Vector3d result;
        	
        	result.x = Math.Floor (v.x);
        	result.y = Math.Floor(v.y);
        	result.z = Math.Floor(v.z);
        	return result;
        }
        
        // pack/unpack 3 floats into 1 float thanks to user "sqrt[-1]"  
        // https://www.opengl.org/discussion_boards/showthread.php/162035-GLSL-packing-a-normal-in-a-single-float
        //Helper method to emulate GLSL
		private static float fract(float value)
		{
		  return value % 1.0f;
		}
		 
		//Helper method to go from a float to packed char
		private static char ConvertChar(float value)
		{
		  //Scale and bias
		  value = (value + 1.0f) * 0.5f;
		  return (char)(value*255.0f);
		}
		 
		//Pack 3 values into 1 float
		public static float PackToFloat(float r, float g, float b)
		{
			return PackToFloat (ConvertChar (r), ConvertChar (g), ConvertChar (b));
		}
		
		public static float PackToFloat(char x, char y, char z)
		{
			uint packedColor = (uint)((x << 16) | (y << 8) | z);
		  	float packedFloat = (float) ( ((double)packedColor) / ((double) (1 << 24)) );  
		 
		  return packedFloat;
		}
		 
		//UnPack 3 values from 1 float
		public static void UnPackFloat(float src, out float r, out float g, out float b)
		{
		  r = fract(src);
		  g = fract(src * 256.0f);
		  b = fract(src * 65536.0f);
		 
		  //Unpack to the -1..1 range
		  r = (r * 2.0f) - 1.0f;
		  g = (g * 2.0f) - 1.0f;
		  b = (b * 2.0f) - 1.0f;
		}
		 
		//Test pack/unpack 3 values
		void DoTest(float r, float g, float b)
		{
		  float outR, outG, outB;
		 
		 
		  //Pack
		  float result = PackToFloat(ConvertChar(r), ConvertChar(g), ConvertChar(b));
		 
		  //Unpack
		  UnPackFloat(result, out outR, out outG, out outB);
		}

        public static Vector3d Snap(Vector3d position, Vector3d interval)
        {
            double xDiff = position.x % interval.x;
           // double yDiff = position.y % interval.y;
            double zDiff = position.z % interval.z;

            position.x -= xDiff;
           // position.y -= yDiff;
            position.z -= zDiff;

            if (xDiff > (interval.x / 2d))
                position.x += interval.x;
            //if (yDiff > (interval.y / 2d))
            //    position.y += interval.y;
            if (zDiff > (interval.z / 2d))
                position.z += interval.z;

            return position;
        }

        public static double Round(double input, double nearest)
        {
        	double result = nearest * Math.Round(input / nearest);

            // round(3.5, 1);  // outputs 4
            // round(3.44, 1); // outputs 3
            // round(3.44, 0.1); // outputs 3.4
            // round(1.68, 0.33); // outputs 1.65
            // round(1.59, 0.33); // outputs 1.65
        
            // round (726, 30); // output 720 which is evenly divisible by 30
            // round (726, 100); // outputs 700
            
            double diff = input % nearest;
			double result2 = input - diff;
			
			System.Diagnostics.Debug.Assert (result == result2);
            	
            return result;
        }

        public static double RoundAuto (double input)
        {
        	if (input < 0) throw new ArgumentOutOfRangeException();
        	if (input == 0) return 0;
        	if (input < 1.0) return 1;
        	
        	// rounds a value to nearest 10^X based on how many places are left of the decimal 
        	// For instance, for a value of 1233 it will round to nearest 10 = 1230
        	// For a value of 16722 it will round to nearest 100 = 16,700
        	// For a value of 983346 it will round to nearest 1000 = 983,000
        	// (or we could use nearest 5, 50, 500, etc)
        	// The main point is, this function calculates what the nearest should be
        	// based on the places left of decimal
        	
        	double places = Math.Floor(Math.Log10(input) + 1);
        	
        	if (places  <= 2) return Round (input, 1);
        	
        	else return Round (input, places - 2);
        }
        
        
        // truncation errors can accumulate. If both x and y are computed values then you have to increase the epsilon.
        // http://stackoverflow.com/questions/2411392/double-epsilon-for-equality-greater-than-less-than-less-than-or-equal-to-gre
        // - that thread shows how epsilon values should be chosen based on use case.  
        public static bool AboutEqual(double x, double y)
        {
        	double epsilon = Math.Max(Math.Abs(x), Math.Abs(y)) * 1E-15;
        	return Math.Abs(x - y) <= epsilon;
    	}

        /// <summary>
        /// Finds the nearest point in a BoundingBox to the point argument passed in.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Vector3d ClosestPointTo(BoundingBox box, Vector3d point)
        {
            Vector3d result;
            result.x = Clamp(point.x, box.Min.x, box.Max.x);
            result.y = Clamp(point.y, box.Min.y, box.Max.y); // box.Center.y;
            result.z = Clamp(point.z, box.Min.z, box.Max.z);
            return result;
        }

        /// <summary>
        /// 2-D.  Unflattens an index into seperate X,Z 0 based array indices 
        /// All arrays are 0 based which is why these are not converted into 
        /// axis coordinate values instead.
        /// </summary>
        public static void UnflattenIndex(uint index, uint tileCountX, out int x, out int z)
        {
			int tilesPerRow = (int)tileCountX;
 
			z = (int)index / tilesPerRow;
			x = (int)index % tilesPerRow;
        }
				
        /// <summary>
        /// 3-D.  Unflattens an index into seperate X,Y,Z 0 based array indices 
        /// All arrays are 0 based which is why these are not converted into 
        /// axis coordinate values instead.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="cellcountX"></param>
        /// <param name="cellcountZ"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static void UnflattenIndex(uint index, uint cellcountX, uint cellcountZ, out uint x, out uint y, out uint z)
        {
            uint cellsPerLevel = cellcountX * cellcountZ;
            y = index / cellsPerLevel;

            uint remainder = index % cellsPerLevel;

            if (remainder == 0)
            {
                x = z = 0;
                return;
            }

            z = remainder / cellcountX;
            x = remainder % cellcountX;
        }
        
		/// <summary>
		/// 2-D - flattens 0 based array indices.
		/// </summary>
		/// <param name="x">Row Index, not a real x coord</param>
		/// <param name="z">Column Index, not a real z coord</param>
		/// <returns></returns>
        public static uint FlattenIndex (int x, int z, uint tileCountX)
        {
            // TODO: if the x, z are out of range of possible x, z, this will create an indvalid index
            int tilesPerRow = (int)tileCountX;

            int index =  (z * tilesPerRow) + x;
            return (uint)index;
        }
        
        /// <summary>
        /// 3-D - flattens 0 based array indices into a single unsigned integer.
        /// Since arrays are always 0 based, we use them to produce the flattened result
        /// as opposed to the axis coordinate system values which can contain negative values.
        /// </summary>
        /// <param name="x">0 based array index</param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="countX"></param>
        /// <param name="countZ"></param>
        /// <returns></returns>
        public static uint FlattenCoordinate(uint x, uint y, uint z, uint countX, uint countZ)
        {

            // TODO: if the x, y, z are out of range of possible x,y,z, this will create an indvalid index
            uint tilesPerFloor = countX * countZ;
            uint tilesPerRow = countX;

            uint index =  (y * tilesPerFloor) + (z * tilesPerRow) + x;
            return index;

        } 
        
        public static byte NormalizeDegrees(float degrees)
        {
            // TODO: should i normalize rotations as a byte value or should i use
            // an int or should i use byte but as enum to represent 45 degree increments?
            // If d is the degrees, the normalized value is 255*((d%360)/360). 
            return (byte)(255 * ((degrees % 360) / 360d)); 
        }

        public static Vector3d WrapAngleRadians(Vector3d pitchYawRollRadians)
        {
            double maxRadians = TWO_PI;
            double minRadians = 0.0d;

            pitchYawRollRadians.x = Wrap(pitchYawRollRadians.x, maxRadians, minRadians);
            pitchYawRollRadians.y = Wrap(pitchYawRollRadians.y, maxRadians, minRadians);
            pitchYawRollRadians.z = Wrap(pitchYawRollRadians.z, maxRadians, minRadians);

            return pitchYawRollRadians;
        }

        public static double WrapAngle(double angleDegrees)
        {

            //keep the angle values between 0 and 360
            if (angleDegrees < 0d)
                angleDegrees = 360d - (Math.Abs(angleDegrees) % 360d);
            //    angle += 360d;

            else if (angleDegrees > 360d) angleDegrees %= 360d;

            //angle = angle/ 360.0d;
            //return (angle - Math.Floor(angle)) * 360.0d;


            return angleDegrees;
        }

        public static double Wrap(double value, double max, double min)
	    {
	        value -= min;
	        max -= min;
	        if (max == 0)
	            return min;
    	 
	        value = value % max;
	        value += min;
	        while (value < min)
	        {
	            value += max;
	        }
    	 
	        return value;
	    }

        /// <summary>
        /// Value returned will never be less than 0.0 or > 1.0
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Clamp (double value)
        {
        	return Clamp (value, 0.0, 1.0);
        }
        
        public static double Clamp (double value, double min, double max)
        {
            if (value < min ) return min;
            if (value > max) return max;
            return value;
        }


        public static void Swap<T>(ref T lhs, ref T rhs)
		{
		    T temp;
		    temp = lhs;
		    lhs = rhs;
		    rhs = temp;
		}
        
        

        public static bool SolveQuadratic(double a, double b, double c, out double t0, out double t1)
        {
            double sqrt = Math.Sqrt(b * b - 4 * a * c);
            double twoA = 2 * a;

            t0 = (-b + sqrt) / twoA;
            t1 = (-b - sqrt) / twoA; 
         
            
            // if t0 < 0 && t1 < 0 there is no solution return false
            // if t0 == t1 there is only one solution return true

            return true;
        }
  




        public static void BarycentricCoordinate(Triangle tri, Vector3d point, out double u, out double v, out double w)
        {
            BarycentricCoordinate(tri.Points[0], tri.Points[1], tri.Points[2], point, out u, out v, out w);
        }

        public static void BarycentricCoordinate(Vector3d a, Vector3d b, Vector3d c, Vector3d point, out double u, out double v, out double w)
        {
            Vector3d v0 = b - a;
            Vector3d v1 = c - a;
            Vector3d v2 = point - a;

            double d00 = Vector3d.DotProduct(v0, v0);
            double d01 = Vector3d.DotProduct(v0, v1);
            double d11 = Vector3d.DotProduct(v1, v1);
            double d20 = Vector3d.DotProduct(v2, v0);
            double d21 = Vector3d.DotProduct(v2, v1);
            double denom = d00 * d11 - d01 * d01;

            v = (d11 * d20 - d01 * d21) / denom; // TODO: is the proper order for these u, v, w instead of v, w, u?
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }


        // https://stackoverflow.com/questions/9879258/how-can-i-generate-random-points-on-a-circles-circumference-in-javascript
        public static Vector3d GetRandomPointOnCircle(Vector3d center, double radius)
        {
            Vector3d result = center;
            var angle = RandomHelper.RandomDouble() * TWO_PI;

            result.x += Math.Cos(angle) * radius;
            result.z += Math.Sin(angle) * radius;
            result.y += 0d; // NOTE: we limit the result to the galactic plane


            // todo: we could create an array of points around a circle to represent waypoints to patrol around a celestial body
            // and we can start our patrol by heading to the nearest point
          //  result = MoveAroundPoint(center, radius, angle, 0);
            return result;
        }

        public static Vector3d[] GenerateOrbitalWaypoints(Vector3d center, double radius, uint numberOFPoints)
        {
            return GenerateOrbitalWaypoints(0, center, radius, numberOFPoints);
        }

        public static Vector3d[] GenerateOrbitalWaypoints(double angle, Vector3d center, double radius, uint numberOFPoints)
        {
            if (numberOFPoints == 0) return null;
            if (numberOFPoints > 360) numberOFPoints = 360;

            Vector3d[] results = new Vector3d[numberOFPoints];

            double step = TWO_PI / (double)numberOFPoints; // (360 / numberOFPoints) * DEGREES_TO_RADIANS;

            for (int i = 0; i < numberOFPoints; i++)
            {
                results[i] = MoveAroundPoint(center, radius, angle, 0);
                angle -= step;
            }

            return results;
        }


  

        // MoveAroundPoint mimics TV's function of the same name
        public static Vector3d MoveAroundPoint(Vector3d origin, Vector3d pivot, double radius, double horzAngleRadians, double vertAngleRadians)
        {
            // todo: is just adding the pivot correct?
            return MoveAroundPoint(origin, radius, horzAngleRadians, vertAngleRadians) + pivot;
          
        }

        // MoveAroundPoint mimics TV's function of the same name
        /// <summary>
        /// Returns a 3D point around the origin with desired radius and with specified horizontal and verticle angels
        /// </summary>
        public static Vector3d MoveAroundPoint (Vector3d origin, double radius, double horzAngleRadians, double vertAngleRadians)
        {

            return new Vector3d(
                origin.x + (radius * Math.Sin(horzAngleRadians) * Math.Cos(vertAngleRadians)),
                origin.y - (radius * Math.Sin(vertAngleRadians)),
                origin.z + (radius * Math.Cos(horzAngleRadians) * Math.Cos(vertAngleRadians)));

        }

        public static Vector3d MoveAroundPoint_Ellipse(double radiusXMeters, double radiusZMeters)
        {
            // t = orbital period seconds
            // k = seconds into current orbital period

            // earth
            // 152,098,232,000 aphelion // furthest
            // 147,098,290,000   perihelion // closest

            // radiusX = aphelion
            // radiusZ = perihelion
            
            // eccentricity = (radiusX - radiusZ) / (radiusX + radiusZ)
            // eccentricity = 4999942 / 299196522
            // eccentricty = 0.01671123035313893120722840488099; [CORRECT]

            // OR

            // eccentricity = 1 - ( 2 / ((aphelion / perihlion) + 1))
            // eccentricty = 1 - ( 2 / (1.0339904835059605383583996795612 + 1))
            // eccentricty = 1 - (2 / 2.0339904835059605383583996795612)
            // eccentricty = 1 - 0.98328876964686106879277159511901;
            // eccentricty = 0.01671123035313893120722840488099; [CORRECT]

            // FROM ECCENTRICTY 
            // we can get a unit ratio that represents aphelion and perihelion and if we then
            // scale that by average orbital radius (semi-major axis), we get back to aphelion and perihelion!
            
            // 149,598,261,000 earth semi major axis
            // perihelion / ahelion = (1 - eccentricty) / (1 + eccentricty)
            // perihelion / ahelion = (1 - eccentricty) / (1 + eccentricty)
            // perihelion / ahelion = 0.98328876964686106879277159511901 / 1.01671123035313893120722840488099
            // perihelion / ahelion = 0.96712688941709723489751018276145
            // 
            // 1 = aphelion * scale = ahelion
            // 0.96712688941709723489751018276145 * scale = perhilion

            float orbitalPeriodSeconds = 95000000f; 
            float currentOrbitElapsedSeconds = 0f;

            Vector3d result;
            result.x = radiusXMeters * Math.Cos(TWO_PI * currentOrbitElapsedSeconds / orbitalPeriodSeconds);
            result.y = 0.0d;
            result.z = radiusZMeters * Math.Cos(TWO_PI * currentOrbitElapsedSeconds/ orbitalPeriodSeconds);

            // TODO: here our result must take into account the anamoly of the elipse where position
            // updates are not linear because in eliptical orbits, the velocity changes!

            // result represents a unit 
            return result;
        }
        

    //    ''' <summary>
    //''' Simple quad mesh useful for FIXED 2d billboards (i.e. billboards that dont rotate to face the camera)
    //''' </summary>
    //''' <param name="height"></param>
    //''' <param name="width"></param>
    //''' <returns></returns>
    //''' <remarks></remarks>
    //Public Shared Function CreateQuadMesh(ByVal height As Single, ByVal width As Single) As MTV3D65.TVMesh
    //    Dim quadmesh As MTV3D65.TVMesh
    //    quadmesh = _Scene.CreateMeshBuilder()

    //    ' 6 vertices for our quad
    //    quadmesh.AddVertex(-width, 0, 0, 0, 1, 0, 0, 1)
    //    quadmesh.AddVertex(width, height, 0, 0, 1, 0, 1, 0)
    //    quadmesh.AddVertex(-width, height, 0, 0, 1, 0, 0, 0)

    //    quadmesh.AddVertex(-width, 0, 0, 0, 1, 0, 0, 1)
    //    quadmesh.AddVertex(width, 0, 0, 0, 1, 0, 1, 1)
    //    quadmesh.AddVertex(width, height, 0, 0, 1, 0, 1, 0)

    //    'Dim height, width As Single
    //    'height = 10 : width = 5

    //    'width = width * 0.5!
    //    'Dim quadmesh As MTV3D65.TVMesh = CreateQuadMesh(height, width)

    //    '' to give our grass depth, we will cross two more grass planes to make a single 18 vertex grass clump
    //    'Dim depth As Single = width
    //    '' this grass plane crosses perpendicular to the previous
    //    'quadmesh.AddVertex(0, 0, depth, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(0, height, -depth, 0, 1, 0, 1, 0)
    //    'quadmesh.AddVertex(0, height, depth, 0, 1, 0, 0, 0)
    //    'quadmesh.AddVertex(0, 0, depth, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(0, 0, -depth, 0, 1, 0, 1, 1)
    //    'quadmesh.AddVertex(0, height, -depth, 0, 1, 0, 1, 0)

    //    '' this one crosses at 45 degree angle to both the others
    //    'quadmesh.AddVertex(-width * 0.5!, 0, depth * 0.5!, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(width * 0.5!, height, -depth * 0.5!, 0, 1, 0, 1, 0)
    //    'quadmesh.AddVertex(-width * 0.5!, height, depth * 0.5!, 0, 1, 0, 0, 0)
    //    'quadmesh.AddVertex(-width * 0.5!, 0, depth * 0.5!, 0, 1, 0, 0, 1)
    //    'quadmesh.AddVertex(width * 0.5!, 0, -depth * 0.5!, 0, 1, 0, 1, 1)
    //    'quadmesh.AddVertex(width * 0.5!, height, -depth * 0.5!, 0, 1, 0, 1, 0)


    //    'quadmesh.WeldVertices()
    //    'm.CreateFromMesh(quadmesh)
    //    'quadmesh.Destroy()
    //    Return quadmesh
    //End Function

        

//http://www.truevision3d.com/forums/tv3d_sdk_65/rotation_and_normals-t18209.0.html
//        public TV_3DMATRIX BindMatrixToLand(TV_3DMATRIX InMatrix, TV_3DVECTOR Scale, TV_3DVECTOR Position)
//                {
//                    {
//                        TV_3DVECTOR TopVector = World.Landscape.GetNormal(InMatrix.m41, InMatrix.m43);
//                        TV_3DVECTOR TentativeForwardVector = new TV_3DVECTOR(InMatrix.m31, InMatrix.m32, InMatrix.m33);
//                        TV_3DVECTOR RightVector = new TV_3DVECTOR();
//                        TV_3DVECTOR ForwardVector = new TV_3DVECTOR();
//                        TVMathLibrary MathLibrary = new TVMathLibrary();
//                        MathLibrary.TVVec3Cross(ref RightVector, TopVector, TentativeForwardVector);
//                        MathLibrary.TVVec3Cross(ref ForwardVector, RightVector, TopVector);
//                        MathLibrary.TVVec3Normalize(ref RightVector, RightVector);
//                        MathLibrary.TVVec3Normalize(ref ForwardVector, ForwardVector);
//                        InMatrix.m11 = RightVector.x * Scale.x;
//                        InMatrix.m12 = RightVector.y * Scale.x;
//                        InMatrix.m13 = RightVector.z * Scale.x;
//                        InMatrix.m21 = TopVector.x * Scale.y;
//                        InMatrix.m22 = TopVector.y * Scale.y;
//                        InMatrix.m23 = TopVector.z * Scale.y;
//                        InMatrix.m31 = ForwardVector.x * Scale.z;
//                        InMatrix.m32 = ForwardVector.y * Scale.z;
//                        InMatrix.m33 = ForwardVector.z * Scale.z;
//                        InMatrix.m41 = Position.x;
//                        InMatrix.m42 = Position.y;
//                        InMatrix.m43 = Position.z;

//                    }

//I store the matrix information returned this routine and I apply teh matrix to the mesh with setmatrix array: _MiniMesh.SetMatrixArray(_DisplayAmount, FoilageMatrix);



        /// <summary>
        /// Returns the X,Y rotation (turning like a dial about the z axis)
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static double XYHeading2DRadians(Vector3d vec)
        {
            return Math.Atan2 (-vec.y, Math.Sqrt (vec.x * vec.x + vec.z * vec.z));
        }

        /// <summary>
        /// Returns the X,Z rotation (spinning like a top about the Y axis)
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static double Heading2DRadians(Vector3d vec)
        {
            // .NET already handles the various cases so we dont need to compensate using -vec.x or anything
            //double angle = (double)System.Math.Atan2(-vec.x, vec.z);
            //return -1 * angle;
            return  Math.Atan2(vec.x, vec.z);
        }

        public static double Heading2DDegrees(Vector3d vec)
        {
            return RadiansToDegrees(Heading2DRadians(vec));
        }

        // XZ plane vector
        public static Vector3d VectorFrom2DHeading(double angleDegrees)
        {

            double angleRadians = DEGREES_TO_RADIANS * angleDegrees;

            Vector3d result;
            result.x = Math.Sin(angleRadians) * Math.Cos(0);
            result.y = 0;
            result.z = Math.Cos(angleRadians) * Math.Cos(0);

            return Vector3d.Normalize (result);
        }

        //https://stackoverflow.com/questions/46387747/find-angle-of-point-on-circle
        public static double AngleOfPointOnCircle(Vector3d point, Vector3d circleCenter)
        {
            double theta = RADIANS_TO_DEGREES * (Math.Atan2(point.z - circleCenter.z, point.x - circleCenter.x));
            return theta;
        }

        // https://discussions.unity.com/t/finding-a-tangent-vector-from-a-given-point-and-circle/221943/2
        // this approach is more geometrical and less algebraic than approach 1,
        // and far more stable. thanks to Mike Plotz for suggesting this direction.
        public static bool CircleTangents_2(Vector3d circleCenter, double circleRadius, Vector3d externalPosition, ref Vector3d tanPosA, ref Vector3d tanPosB)
        {
            externalPosition -= circleCenter;

            double P = externalPosition.Length;

            // if p is inside the circle, there ain't no tangents.
            if (P <= circleRadius)
            {
                return false;
            }

            double a = circleRadius * circleRadius / P;
            double q = circleRadius * System.Math.Sqrt((P * P) - (circleRadius * circleRadius)) / P;

            Vector3d pN = externalPosition / P;
            Vector3d pNP = new Vector3d(-pN.z, 0, pN.x);
            Vector3d va = pN * a;

            tanPosA = va + pNP * q;
            tanPosB = va - pNP * q;

            tanPosA += circleCenter;
            tanPosB += circleCenter;

            return true;
        }
    }

//    // source: http://www.dreamincode.net/forums/topic/277514-normalize-angle-and-radians/
//// author: http://www.dreamincode.net/forums/user/422465-lordofduct/
//// http://www.lordofduct.com/blog/
//    public static class DblMathUtil
//    {

//        #region "Public ReadOnly Properties"
//        // Number pi
//        public const double PI = 3.14159265358979;
//        // PI / 2 OR 90 deg
//        public const double PI_2 = 1.5707963267949;
//        // PI / 4 OR 45 deg
//        public const double PI_4 = 0.785398163397448;
//        // PI / 8 OR 22.5 deg
//        public const double PI_8 = 0.392699081698724;
//        // PI / 16 OR 11.25 deg
//        public const double PI_16 = 0.196349540849362;
//        // 2 * PI OR 180 deg
//        public const double TWO_PI = 6.28318530717959;
//        // 3 * PI_2 OR 270 deg
//        public const double THREE_PI_2 = 4.71238898038469;
//        // Number e
//        public const double E = 2.71828182845905;
//        // ln(10)
//        public const double LN10 = 2.30258509299405;
//        // ln(2)
//        public const double LN2 = 0.693147180559945;
//        // logB10(e)
//        public const double LOG10E = 0.434294481903252;
//        // logB2(e)
//        public const double LOG2E = 1.44269504088896;
//        // sqrt( 1 / 2 )
//        public const double SQRT1_2 = 0.707106781186548;
//        // sqrt( 2 )
//        public const double SQRT2 = 1.4142135623731;
//        // PI / 180
//        public const double DEG_TO_RAD = 0.0174532925199433;
//        //  180.0 / PI
//        public const double RAD_TO_DEG = 57.2957795130823;

//        // 2^16
//        public const int B_16 = 65536;
//        // 2^31
//        public const long B_31 = 2147483648L;
//        // 2^32
//        public const long B_32 = 4294967296L;
//        // 2^48
//        public const long B_48 = 281474976710656L;
//        // 2^53 !!NOTE!! largest accurate double floating point whole value
//        public const long B_53 = 9007199254740992L;
//        // 2^63
//        public const ulong B_63 = 9223372036854775808;
//        //18446744073709551615 or 2^64 - 1 or ULong.MaxValue...
//        public const ulong B_64_m1 = ulong.MaxValue;

//        //  1.0/3.0
//        public const double ONE_THIRD = 0.333333333333333;
//        //  2.0/3.0
//        public const double TWO_THIRDS = 0.666666666666667;
//        //  1.0/6.0
//        public const double ONE_SIXTH = 0.166666666666667;

//        // COS( PI / 3 )
//        public const double COS_PI_3 = 0.866025403784439;
//        //  SIN( 2*PI/3 )
//        public const double SIN_2PI_3 = 0.03654595;

//        // 4*(Math.sqrt(2)-1)/3.0
//        public const double CIRCLE_ALPHA = 0.552284749830793;

//        public const bool ONN = true;

//        public const bool OFF = false;
//        // round integer epsilon
//        public const double SHORT_EPSILON = 0.1;
//        // percentage epsilon
//        public const double PERC_EPSILON = 0.001;
//        // single float average epsilon
//        public const double EPSILON = 0.0001;
//        // arbitrary 8 digit epsilon
//        public const double LONG_EPSILON = 1E-08;

//        public static readonly double MACHINE_EPSILON = DblMathUtil.ComputeMachineEpsilon();

//        public static double ComputeMachineEpsilon()
//        {
//            double fourThirds = 4.0 / 3.0;
//            double third = fourThirds - 1.0;
//            double one = third + third + third;
//            return Math.Abs(1.0 - one);
//        }
//        #endregion

//        #region "Public Shared Methods"

//        /// <summary>
//        /// Calculates the integral part of a float
//        /// </summary>
//        /// <param name="value"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double Truncate(double value)
//        {
//            return Math.Truncate(value);
//        }

//        /// <summary>
//        /// Shears off the fractional part of a float.
//        /// </summary>
//        /// <param name="value"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double Shear(double value)
//        {
//            return value % 1;
//        }

//        /// <summary>
//        /// Returns if the value is in between or equal to max and min
//        /// </summary>
//        /// <param name="value"></param>
//        /// <param name="max"></param>
//        /// <param name="min"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static bool InRange(double value, double max, double min)
//        {
//            return (value >= min && value <= max);
//        }

//        public static bool InRange(double value, double max)
//        {
//            return InRange(value, max, 0);
//        }

//        #region "series"
//        /// <summary>
//        /// Sums a series of numeric values passed as a param array...
//        /// 
//        /// MathUtil.Summation(1,2,3,4) == 10
//        /// </summary>
//        /// <param name="arr"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double Summation(params double[] arr)
//        {
//            double result = 0;

//            foreach (double value in arr)
//            {
//                result += value;
//            }

//            return result;
//        }

//        public static double Summation(double[] arr, int startIndex, int endIndex)
//        {
//            double result = 0;

//            for (int i = startIndex; i <= Math.Min(endIndex, arr.Length - 1); i++)
//            {
//                result += arr[i];
//            }

//            return result;
//        }

//        public static double Summation(double[] arr, int startIndex)
//        {
//            return Summation(arr, startIndex, int.MaxValue);
//        }

//        /// <summary>
//        /// Multiplies a series of numeric values passed as a param array...
//        /// 
//        /// MathUtil.ProductSeries(2,3,4) == 24
//        /// </summary>
//        /// <param name="arr"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double ProductSeries(params double[] arr)
//        {
//            if (arr == null || arr.Length == 0)
//                return double.NaN;

//            double result = 1;

//            foreach (double value in arr)
//            {
//                result *= value;
//            }

//            return result;
//        }
//        #endregion

//        #region "Value interpolating and warping"
//        /// <summary>
//        /// The average of an array of values
//        /// </summary>
//        /// <param name="values">An array of values</param>
//        /// <returns>the average</returns>
//        /// <remarks></remarks>
//        public static double Average(params double[] values)
//        {
//            double avg = 0;

//            foreach (double value in values)
//            {
//                avg += value;
//            }

//            return avg / values.Length;
//        }

//        /// <summary>
//        /// a one dimensional linear interpolation of a value.
//        /// </summary>
//        /// <param name="a">from value</param>
//        /// <param name="b">to value</param>
//        /// <param name="weight">lerp value</param>
//        /// <returns>the value lerped from a to b</returns>
//        /// <remarks></remarks>
//        public static double Interpolate(double a, double b, double weight)
//        {
//            return (b - a) * weight + a;
//        }

//        /// <summary>
//        /// The percentage a value is from min to max
//        /// 
//        /// eg:
//        /// 8 of 10 out of 0->10 would be 0.8f
//        /// 
//        /// Good for calculating the lerp weight
//        /// </summary>
//        /// <param name="value">The value to text</param>
//        /// <param name="max">The max value</param>
//        /// <param name="min">The min value</param>
//        /// <returns>The percentage value is from min</returns>
//        /// <remarks></remarks>
//        public static double PercentageMinMax(double value, double max, double min = 0)
//        {
//            value -= min;
//            max -= min;

//            if (max == 0)
//            {
//                return 0;
//            }
//            else
//            {
//                return value / max;
//            }
//        }

//        /// <summary>
//        /// The percentage a value is from max to min
//        /// 
//        /// eg:
//        /// 8 of 10 out of 0->10 would be 0.2f
//        /// 
//        /// Good for calculating a discount
//        /// </summary>
//        /// <param name="value">The value to text</param>
//        /// <param name="max">The max value</param>
//        /// <param name="min">The min value</param>
//        /// <returns>The percentage value is from max</returns>
//        /// <remarks></remarks>
//        public static double PercentageOffMinMax(double value, double max, double min = 0)
//        {
//            value -= max;
//            min -= max;

//            if (min == 0)
//            {
//                return 0;
//            }
//            else
//            {
//                return value / min;
//            }
//        }

//        /// <summary>
//        /// Return the minimum value of several values
//        /// </summary>
//        /// <param name="args"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double Min(params double[] args)
//        {
//            if (args.Length == 0)
//                return double.NaN;
//            double value = args[0];

//            for (int i = 0; i <= args.Length - 1; i++)
//            {
//                if (args[i] < value)
//                    value = args[i];
//            }

//            return value;
//        }

//        /// <summary>
//        /// Return the maximum of several values
//        /// </summary>
//        /// <param name="args"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double Max(params double[] args)
//        {
//            if (args.Length == 0)
//                return double.NaN;
//            double value = args[0];

//            for (int i = 1; i <= args.Length - 1; i++)
//            {
//                if (args[i] > value)
//                    value = args[i];
//            }

//            return value;
//        }

//        /// <summary>
//        /// Wraps a value around some significant range.
//        /// 
//        /// Similar to modulo, but works in a unary direction over any range (including negative values).
//        /// 
//        /// ex:
//        /// Wrap(8,6,2) == 4
//        /// Wrap(4,2,0) == 0
//        /// Wrap(4,2,-2) == -2
//        /// </summary>
//        /// <param name="value">value to wrap</param>
//        /// <param name="max">max in range</param>
//        /// <param name="min">min in range</param>
//        /// <returns>A value wrapped around min to max</returns>
//        /// <remarks></remarks>
//        public static double Wrap(double value, double max, double min = 0)
//        {
//            value -= min;
//            max -= min;
//            if (max == 0)
//                return min;

//            value = value % max;
//            value += min;
//            while (value < min)
//            {
//                value += max;
//            }

//            return value;

//        }

//        /// <summary>
//        /// Arithmetic version of Wrap... unsure of which is more efficient.
//        /// 
//        /// Here for demo purposes
//        /// </summary>
//        /// <param name="value">value to wrap</param>
//        /// <param name="max">max in range</param>
//        /// <param name="min">min in range</param>
//        /// <returns>A value wrapped around min to max</returns>
//        /// <remarks></remarks>
//        public static double ArithWrap(double value, double max, double min = 0)
//        {
//            max -= min;
//            if (max == 0)
//                return min;

//            return value - max * Math.Floor((value - min) / max);
//        }

//        /// <summary>
//        /// Clamp a value into a range.
//        /// 
//        /// If input is LT min, min returned
//        /// If input is GT max, max returned
//        /// else input returned
//        /// </summary>
//        /// <param name="input">value to clamp</param>
//        /// <param name="max">max in range</param>
//        /// <param name="min">min in range</param>
//        /// <returns>calmped value</returns>
//        /// <remarks></remarks>
//        public static double Clamp(double input, double max, double min = 0)
//        {
//            return Math.Max(min, Math.Min(max, input));
//        }

//        /// <summary>
//        /// Ensures a value is within some range. If it doesn't fall in that range than some default value is returned.
//        /// </summary>
//        /// <param name="input">value to clamp</param>
//        /// <param name="max">max in range</param>
//        /// <param name="min">min in range</param>
//        /// <param name="defaultValue">default value if not in range</param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double ClampOrDefault(double input, double max, double min, double defaultValue)
//        {
//            return input < min || input > max ? defaultValue : input;
//        }

//        /// <summary>
//        /// roundTo some place comparative to a 'base', default is 10 for decimal place
//        /// 
//        /// 'place' is represented by the power applied to 'base' to get that place
//        /// </summary>
//        /// <param name="value">the value to round</param>
//        /// <param name="place">the place to round to</param>
//        /// <param name="base">the base to round in... default is 10 for decimal</param>
//        /// <returns>The value rounded</returns>
//        /// <remarks>e.g.
//        /// 
//        /// 2000/7 ~= 285.714285714285714285714 ~= (bin)100011101.1011011011011011
//        /// 
//        /// roundTo(2000/7,-3) == 0
//        /// roundTo(2000/7,-2) == 300
//        /// roundTo(2000/7,-1) == 290
//        /// roundTo(2000/7,0) == 286
//        /// roundTo(2000/7,1) == 285.7
//        /// roundTo(2000/7,2) == 285.71
//        /// roundTo(2000/7,3) == 285.714
//        /// roundTo(2000/7,4) == 285.7143
//        /// roundTo(2000/7,5) == 285.71429
//        /// 
//        /// roundTo(2000/7,-3,2)  == 288       -- 100100000
//        /// roundTo(2000/7,-2,2)  == 284       -- 100011100
//        /// roundTo(2000/7,-1,2)  == 286       -- 100011110
//        /// roundTo(2000/7,0,2)  == 286       -- 100011110
//        /// roundTo(2000/7,1,2) == 285.5     -- 100011101.1
//        /// roundTo(2000/7,2,2) == 285.75    -- 100011101.11
//        /// roundTo(2000/7,3,2) == 285.75    -- 100011101.11
//        /// roundTo(2000/7,4,2) == 285.6875  -- 100011101.1011
//        /// roundTo(2000/7,5,2) == 285.71875 -- 100011101.10111
//        /// 
//        /// note what occurs when we round to the 3rd space (8ths place), 100100000, this is to be assumed 
//        /// because we are rounding 100011.1011011011011011 which rounds up.</remarks>
//        public static double RoundTo(double value, int place, uint @base)
//        {
//            if (place == 0)
//            {
//                //'if zero no reason going through the math hoops
//                return Math.Round(value);
//            }
//            else if (@base == 10 && place > 0 && place <= 15)
//            {
//                //'Math.Round has a rounding to decimal spaces that is very efficient
//                //'only useful for base 10 if places are from 1 to 15
//                return Math.Round(value, place);
//            }
//            else
//            {
//                double p = Math.Pow(@base, place);
//                return Math.Round(value * p) / p;
//            }
//        }

//        public static double RoundTo(double value, int place)
//        {
//            return RoundTo(value, place, 10);
//        }

//        public static double RoundTo(double value)
//        {
//            return RoundTo(value, 0, 10);
//        }

//        /// <summary>
//        /// FloorTo some place comparative to a 'base', default is 10 for decimal place
//        /// 
//        /// 'place' is represented by the power applied to 'base' to get that place
//        /// </summary>
//        /// <param name="value"></param>
//        /// <param name="place"></param>
//        /// <param name="base"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double FloorTo(double value, int place, uint @base)
//        {
//            if (place == 0)
//            {
//                //'if zero no reason going through the math hoops
//                return Math.Floor(value);
//            }
//            else
//            {
//                double p = Math.Pow(@base, place);
//                return Math.Floor(value * p) / p;
//            }
//        }

//        public static double FloorTo(double value, int place)
//        {
//            return FloorTo(value, place, 10);
//        }

//        public static double FloorTo(double value)
//        {
//            return FloorTo(value, 0, 10);
//        }

//        /// <summary>
//        /// CeilTo some place comparative to a 'base', default is 10 for decimal place
//        /// 
//        /// 'place' is represented by the power applied to 'base' to get that place
//        /// </summary>
//        /// <param name="value"></param>
//        /// <param name="place"></param>
//        /// <param name="base"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double CeilTo(double value, int place, uint @base)
//        {
//            if (place == 0)
//            {
//                //'if zero no reason going through the math hoops
//                return Math.Ceiling(value);
//            }
//            else
//            {
//                double p = Math.Pow(@base, place);
//                return Math.Ceiling(value * p) / p;
//            }
//        }

//        public static double CeilTo(double value, int place)
//        {
//            return CeilTo(value, place, 10);
//        }

//        public static double CeilTo(double value)
//        {
//            return CeilTo(value, 0, 10);
//        }
//        #endregion

//        #region "Simple fuzzy arithmetic"

//        /// <summary>
//        /// Test if Double is kind of equal to some other value by some epsilon.
//        /// 
//        /// Due to float error, two values may be considered similar... but the computer considers them different. 
//        /// By using some epsilon (degree of error) once can test if the two values are similar.
//        /// </summary>
//        /// <param name="a"></param>
//        /// <param name="b"></param>
//        /// <param name="epsilon"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static bool FuzzyEqual(double a, double b, double epsilon)
//        {
//            return Math.Abs(a - B)/> < epsilon;
//        }

//        public static bool FuzzyEqual(double a, double B)/>
//        {
//            return FuzzyEqual(a, b, EPSILON);
//        }

//        /// <summary>
//        /// Test if Double is greater than some other value by some degree of error in epsilon.
//        /// 
//        /// Due to float error, two values may be considered similar... but the computer considers them different. 
//        /// By using some epsilon (degree of error) once can test if the two values are similar.
//        /// </summary>
//        /// <param name="a"></param>
//        /// <param name="b"></param>
//        /// <param name="epsilon"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static bool FuzzyLessThan(double a, double b, double epsilon)
//        {
//            return a < b + epsilon;
//        }

//        public static bool FuzzyLessThan(double a, double B)/>
//        {
//            return FuzzyLessThan(a, b, EPSILON);
//        }

//        /// <summary>
//        /// Test if Double is less than some other value by some degree of error in epsilon.
//        /// 
//        /// Due to float error, two values may be considered similar... but the computer considers them different. 
//        /// By using some epsilon (degree of error) once can test if the two values are similar.
//        /// </summary>
//        /// <param name="a"></param>
//        /// <param name="b"></param>
//        /// <param name="epsilon"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static bool FuzzyGreaterThan(double a, double b, double epsilon)
//        {
//            return a > b - epsilon;
//        }

//        public static bool FuzzyGreaterThan(double a, double B)/>
//        {
//            return FuzzyGreaterThan(a, b, EPSILON);
//        }

//        /// <summary>
//        /// Test if a value is near some target value, if with in some range of 'epsilon', the target is returned.
//        /// 
//        /// eg:
//        /// Slam(1.52,2,0.1) == 1.52
//        /// Slam(1.62,2,0.1) == 1.62
//        /// Slam(1.72,2,0.1) == 1.72
//        /// Slam(1.82,2,0.1) == 1.82
//        /// Slam(1.92,2,0.1) == 2
//        /// </summary>
//        /// <param name="value"></param>
//        /// <param name="target"></param>
//        /// <param name="epsilon"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double Slam(double value, double target, double epsilon)
//        {
//            if (Math.Abs(value - target) < epsilon)
//            {
//                return target;
//            }
//            else
//            {
//                return value;
//            }
//        }

//        public static double Slam(double value, double target)
//        {
//            return Slam(value, target, EPSILON);
//        }
//        #endregion

//        #region "Angular Math"
//        /// <summary>
//        /// convert radians to degrees
//        /// </summary>
//        /// <param name="angle"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double RadiansToDegrees(double angle)
//        {
//            return angle * RAD_TO_DEG;
//        }

//        /// <summary>
//        /// convert degrees to radians
//        /// </summary>
//        /// <param name="angle"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double DegreesToRadians(double angle)
//        {
//            return angle * DEG_TO_RAD;
//        }

//        /// <summary>
//        /// Find the angle of a segment from (x1, y1) -> (x2, y2 )
//        /// </summary>
//        /// <param name="x1"></param>
//        /// <param name="y1"></param>
//        /// <param name="x2"></param>
//        /// <param name="y2"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double AngleBetween(double x1, double y1, double x2, double y2)
//        {
//            return Math.Atan2(y2 - y1, x2 - x1);
//        }

//        /// <summary>
//        /// set an angle with in the bounds of -PI to PI
//        /// </summary>
//        /// <param name="angle"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double NormalizeAngle(double angle, bool useRadians)
//        {
//            double rd = (useRadians ? PI : 180);
//            return Wrap(angle, PI, -PI);
//        }

//        public static double NormalizeAngle(double angle)
//        {
//            return NormalizeAngle(angle, true);
//        }

//        /// <summary>
//        /// closest angle between two angles from a1 to a2
//        /// absolute value the return for exact angle
//        /// </summary>
//        /// <param name="a1"></param>
//        /// <param name="a2"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double NearestAngleBetween(double a1, double a2, bool useRadians)
//        {
//            double rd_2 = (useRadians ? PI_2 : 90);
//            double two_rd = (useRadians ? TWO_PI : 360);

//            a1 = NormalizeAngle(a1);
//            a2 = NormalizeAngle(a2);

//            if (a1 < -rd_2 & a2 > rd_2)
//                a1 += two_rd;
//            if (a2 < -rd_2 & a1 > rd_2)
//                a2 += two_rd;

//            return a2 - a1;
//        }

//        public static double NearestAngleBetween(double a1, double a2)
//        {
//            return NearestAngleBetween(a1, a2, true);
//        }

//        /// <summary>
//        /// normalizes independent and then sets dep to the nearest value respective to independent
//        /// 
//        /// 
//        /// for instance if dep=-170 degrees and ind=170 degrees then 190 degrees will be returned as an alternative to -170 degrees
//        /// note: angle is passed in radians, this written example is in degrees for ease of reading
//        /// </summary>
//        /// <param name="dep"></param>
//        /// <param name="ind"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double NormalizeAngleToAnother(double dep, double ind, bool useRadians)
//        {
//            return ind + NearestAngleBetween(ind, dep, useRadians);
//        }

//        public static double NormalizeAngleToAnother(double dep, double ind)
//        {
//            return NormalizeAngleToAnother(dep, ind, true);
//        }

//        /// <summary>
//        /// interpolate across the shortest arc between two angles
//        /// </summary>
//        /// <param name="a1"></param>
//        /// <param name="a2"></param>
//        /// <param name="weight"></param>
//        /// <returns></returns>
//        /// <remarks></remarks>
//        public static double InterpolateAngle(double a1, double a2, double weight, bool useRadians)
//        {
//            a1 = NormalizeAngle(a1, useRadians);
//            a2 = NormalizeAngleToAnother(a2, a1, useRadians);

//            return Interpolate(a1, a2, weight);
//        }

//        public static double InterpolateAngle(double a1, double a2, double weight)
//        {
//            return InterpolateAngle(a1, a2, weight, true);
//        }
//        #endregion

//        #region "Advanced Math"
//        /// <summary>
//        /// Compute the logarithm of any value of any base
//        /// </summary>
//        /// <param name="value"></param>
//        /// <param name="base"></param>
//        /// <returns></returns>
//        /// <remarks>
//        /// a logarithm is the exponent that some constant (base) would have to be raised to 
//        /// to be equal to value.
//        /// 
//        /// i.e.
//        /// 4 ^ x = 16
//        /// can be rewritten as to solve for x
//        /// logB4(16) = x
//        /// which with this function would be 
//        /// LoDMath.logBaseOf(16,4)
//        /// 
//        /// which would return 2, because 4^2 = 16
//        /// </remarks>
//        public static double LogBaseOf(double value, double @base)
//        {
//            return Math.Log(value) / Math.Log(@base);
//        }
//        #endregion



//        #region Geometric Calculations

//        public static float ApproxCircumOfEllipse(double a, double B)/>
//        {
//            return (float)(PI * Math.Sqrt((a * a + b * B)/> / 2));
//        }

//        #endregion

//        #endregion
//    }

}