
#define USE_STRUCT

using System;
using System.Diagnostics;

namespace Keystone.Types
{

#if USE_STRUCT
    public struct Quaternion
#else
    public class Quaternion 
#endif
    {
        private double[] _quat;  // subscripts 0 = x, 1=y, 2=z, 3=w

        public static Quaternion Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            if (values == null || values.Length != 4) throw new ArgumentException();
            return new Quaternion(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2]), double.Parse(values[3]));
        }

        public static Quaternion[] ParseArray(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
            if (values == null || values.Length < 4 || values.Length % 4 != 0) throw new ArgumentException();

            int arraySize = values.Length / 4;
            Quaternion[] results = new Quaternion[arraySize];

            int j = 0;
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = new Quaternion();
                results[i]._quat[0] = double.Parse(values[j]); j++;
                results[i]._quat[1] = double.Parse(values[j]); j++;
                results[i]._quat[2] = double.Parse(values[j]); j++;
                results[i]._quat[3] = double.Parse(values[j]); j++;
            }
            return results;
        }

        //Quaternions add a fourth element to the [ x, y, z] values that define a vector, 
        //resulting in arbitrary 4D vectors. However, the following illustrates how each 
        //element of a unit quaternion relates to an axis-angle rotation (where q 
        //represents a unit quaternion (x, y, z, w), axis is normalized, and theta is the
        //desired CCW rotation about the axis):

        //q.x = sin(theta/2) * axis.x
        //q.y = sin(theta/2) * axis.y
        //q.z = sin(theta/2) * axis.z
        //q.w = cos(theta/2)
        public Quaternion(bool identity)
        {
            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat = new double[4];
            if (identity)
                _quat[3] = 1.0; // for no rotation, w = 1 typically
                                // else it's "Empty"
        }


        // http://msdn.microsoft.com/en-us/library/bb205417(VS.85).aspx
        // YawPitchRoll correctly implemented from John Ratcliff's Code Suppository http://codesuppository.blogspot.com/
        // note: he indicates an error with the one in d3d
        /// <summary>
        /// Yaw pitch roll represents Y, X, Z and NOT X,Y,Z 
        /// </summary>
        /// <remarks>This is broken.  Build a Matrix rotationMatrix instead</remarks>
        /// <param name="radianYaw">the Y axis in radians</param>
        /// <param name="radianPitch">the X axis in radians</param>
        /// <param name="radianRoll">the Z axis in radians</param>
        public Quaternion(double radianYaw, double radianPitch, double radianRoll) : this()
        {
            // TODO: This overload is broken. If i use RotationMatrix and create Quaternion from that
            //       it works.  But below is broken.

            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat = new double[4];
            //double c1 = Math.Cos(radianYaw / 2);
            //double s1 = Math.Sin(radianYaw / 2);
            //double c2 = Math.Cos(radianPitch / 2);
            //double s2 = Math.Sin(radianPitch / 2);
            //double c3 = Math.Cos(radianRoll / 2);
            //double s3 = Math.Sin(radianRoll / 2);
            //double c1c2 = c1 * c2;
            //double s1s2 = s1 * s2;
            //_quat[3] = c1c2 * c3 - s1s2 * s3;
            //_quat[0] = c1c2 * s3 + s1s2 * c3;
            //_quat[1] = s1 * c2 * c3 + c1 * s2 * s3;
            //_quat[2] = c1 * s2 * c3 - s1 * c2 * s3;
            ////_quat[3] = c1c2 * c3 - s1s2 * s3;
            ////_quat[2] = c1c2 * s3 + s1s2 * c3;
            ////_quat[1] = s1 * c2 * c3 + c1 * s2 * s3;
            ////_quat[0] = c1 * s2 * c3 - s1 * c2 * s3;

            double sinY, cosY, sinP, cosP, sinR, cosR;
            double halfYaw = 0.5d * radianYaw;
            sinY = Math.Sin(halfYaw);
            cosY = Math.Cos(halfYaw);

            double halfPitch = 0.5d * radianPitch;
            sinP = Math.Sin(halfPitch);
            cosP = Math.Cos(halfPitch);

            double halfRoll = 0.5d * radianRoll;
            sinR = Math.Sin(halfRoll);
            cosR = Math.Cos(halfRoll);

            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat[0] = cosY * sinP * cosR + sinY * cosP * sinR;
            _quat[1] = sinY * cosP * cosR - cosY * sinP * sinR;
            _quat[2] = cosY * cosP * sinR - sinY * sinP * cosR;
            _quat[3] = cosY * cosP * cosR + sinY * sinP * sinR;

        }

        /// <summary>
        /// Creates a quaternion with the passed in component values. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="w"></param>
        /// <remarks> X,Y,Z do are not components of an axis vector.  
        /// If you wish to create a quaternion from radian axis vector values
        /// you should use the constructor that accepts a Vector3d axis and double angleRadians</remarks>
        public Quaternion(double x, double y, double z, double w) : this()
        {
            // subscripts 0 = x, 1=y, 2=z, 3=w
            _quat[0] = x;
            _quat[1] = y;
            _quat[2] = z;
            _quat[3] = w;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis">Unit vector representing the axis of rotation</param>
        /// <param name="angleRadians"></param>
        public Quaternion(Vector3d axis, double angleRadians) : this()
        {
            // xna's version
            axis.Normalize();
            double halfAngle = angleRadians * 0.5d;
            double sineHalfAngle = Math.Sin(halfAngle);

            _quat[0] = axis.x * sineHalfAngle;                   // x
            _quat[1] = axis.y * sineHalfAngle;                   // y
            _quat[2] = axis.z * sineHalfAngle;                   // z
            _quat[3] = Math.Cos(halfAngle);        // w

            // http://www.codeproject.com/KB/graphics/YLScsDrawing3d.aspx
            //double length = axis.Length;
            //const double epsilon = 0.0001;
            //if (length > epsilon)
            //{
            //    double halfAngle = angleRadians * 0.5d;
            //    double sineHalfAngle = Math.Sin(halfAngle);

            //     // divide by length to normalize first since axis must be unit vector
            //    _quat[0] = axis.x / length * sineHalfAngle;                   // x
            //    _quat[1] = axis.y / length * sineHalfAngle;                   // y
            //    _quat[2] = axis.z / length * sineHalfAngle;                   // z
            //    _quat[3] = Math.Cos(halfAngle);        // w
            //}
            //else
            //{
            //    _quat[0] = 0;                   // x
            //    _quat[1] = 0;                   // y
            //    _quat[2] = 0;                   // z
            //    _quat[3] = 1;
            //}
        }


        // TODO: must read this and verify my algos are optimized
        // http://www.edn.com/archives/1995/030295/05df3.htm
        // http://www.gamedev.net/topic/613595-quaternion-lookrotationlookat-up/
        // http://code.google.com/p/slimdx/source/browse/branches/lite/SlimMath/Quaternion.cs
        public Quaternion(Matrix rotationMatrix) : this()
        {
            /** construct Quaternion from a rotation matrix expressed as a triple
            of vectors, each one a row of the matrix.
            Code adapted from Shoemake's paper "Quaternions".
            */
            double tr, s, sinv;
            tr = rotationMatrix.M11 + rotationMatrix.M22 + rotationMatrix.M33;
            if (tr >= 0.0)
            {
                s = Math.Sqrt(tr + 1);
                sinv = 0.5 / s;
                _quat[0] = (rotationMatrix.M32 - rotationMatrix.M23) * sinv;
                _quat[1] = (rotationMatrix.M13 - rotationMatrix.M31) * sinv;
                _quat[2] = (rotationMatrix.M21 - rotationMatrix.M12) * sinv;
                _quat[3] = s * 0.5;
            }
            else if (rotationMatrix.M11 > rotationMatrix.M22 && rotationMatrix.M11 > rotationMatrix.M33)
            {
                s = Math.Sqrt(rotationMatrix.M11 - (rotationMatrix.M22 + rotationMatrix.M33) + 1);
                sinv = 0.5 / s;
                _quat[0] = s * 0.5;
                _quat[1] = (rotationMatrix.M12 + rotationMatrix.M21) * sinv;
                _quat[2] = (rotationMatrix.M31 + rotationMatrix.M13) * sinv;
                _quat[3] = (rotationMatrix.M32 - rotationMatrix.M23) * sinv;
            }
            else if (rotationMatrix.M22 > rotationMatrix.M33)
            {
                s = Math.Sqrt(rotationMatrix.M22 - (rotationMatrix.M33 + rotationMatrix.M11) + 1);
                sinv = 0.5 / s;
                _quat[0] = (rotationMatrix.M12 + rotationMatrix.M21) * sinv;
                _quat[1] = s * 0.5;
                _quat[2] = (rotationMatrix.M23 + rotationMatrix.M32) * sinv;
                _quat[3] = (rotationMatrix.M13 - rotationMatrix.M31) * sinv;
            }
            else
            {
                s = Math.Sqrt(rotationMatrix.M33 - (rotationMatrix.M11 + rotationMatrix.M22) + 1);
                sinv = 0.5 / s;
                _quat[0] = (rotationMatrix.M31 + rotationMatrix.M13) * sinv;
                _quat[1] = (rotationMatrix.M23 + rotationMatrix.M32) * sinv;
                _quat[2] = s * 0.5;
                _quat[3] = (rotationMatrix.M21 - rotationMatrix.M12) * sinv;
            }
        }

        /// <summary>
        /// LookAt Quaternion
        /// </summary>
        /// <param name="forward">The LookAt direction</param>
        /// <param name="up"></param>
        public Quaternion(Vector3d forward, Vector3d up) : this()
        {
            //forward = Vector3d.Normalize(forward);
            //up = Vector3d.Normalize(up);
            Vector3d.OrthoNormalize(ref forward, ref up);
            Vector3d right = Vector3d.CrossProduct(up, forward);

            _quat[3] = Math.Sqrt(1.0d + right.x + up.y + forward.z) * 0.5d;
            double w4_recip = 1.0d / (4.0d * _quat[3]);
            _quat[0] = (up.z - forward.y) * w4_recip;
            _quat[1] = (forward.x - right.z) * w4_recip;
            _quat[2] = (right.y - up.x) * w4_recip;
        }

        public void GetAxisAngle(out Vector3d axis, out double angle)
        {
            // http://content.gpwiki.org/index.php/OpenGL%3aTutorials%3aUsing_Quaternions_to_represent_rotation#Rotating_vectors
            double scale = Math.Sqrt(_quat[0] * _quat[0] + _quat[1] * _quat[1] + _quat[2] * _quat[2]);
            axis.x = _quat[0] / scale;
            axis.y = _quat[1] / scale;
            axis.z = _quat[2] / scale;

            angle = Math.Acos(_quat[3]) * 2.0d;
        }

        // http://www.euclideanspace.com/maths/algebra/vectors/lookat/index.htm
        //public static double LookAt(Vector3d target, Vector3d position, Vector3d eye, Vector3d up)
        // {
        // TODO: finish converting this function
        //// turn vectors into unit vectors 
        //n1 = (current - eye).norm();
        //n2 = (target - eye).norm();  
        //d = sfvec3f.dot(n1,n2); 
        //// if no noticable rotation is available return zero rotation
        //// this way we avoid Cross product artifacts 
        //if( d > 0.9998 ) return new sfquat( 0, 0, 1, 0 ); 
        //// in this case there are 2 lines on the same axis 
        //if(d < -0.9998){ 
        //    n1 = n1.Rotx( 0.5f ); 
        //    // there are an infinite number of normals 
        //    // in this case. Anyone of these normals will be 
        //    // a valid rotation (180 degrees). so rotate the curr axis by 0.5 radians this way we get one of these normals 
        //} 
        //sfvec3f axis = n1;
        //axis.cross(n2);
        //sfquat pointToTarget= new sfquat(1.0 + d,axis.x,axis.y,axis.z); 
        //pointToTarget.norm();
        //// now twist around the target vector, so that the 'up' vector points along the z axis
        //sfmatrix projectionMatrix=new sfmatrix();
        //double a = pointToTarget.x;
        //double b = pointToTarget.y;
        //double c = pointToTarget.z;
        //projectionMatrix.m00 = b*b+c*c;
        //projectionMatrix.m01 = -a*b;
        //projectionMatrix.m02 = -a*c;
        //projectionMatrix.m10 = -b*a;
        //projectionMatrix.m11 = a*a+c*c;
        //projectionMatrix.m12 = -b*c;
        //projectionMatrix.m20 = -c*a;
        //projectionMatrix.m21 = -c*b;
        //projectionMatrix.m22 = a*a+b*b;
        //sfvec3f upProjected = projectionMatrix.transform(up);
        //sfvec3f yaxisProjected = projectionMatrix.transform(new sfvec(0,1,0);
        //d = sfvec3f.dot(upProjected,yaxisProjected);
        //// so the axis of twist is n2 and the angle is arcos(d)
        ////convert this to quat as follows   
        //double s=Math.sqrt(1.0 - d*d);
        //sfquat twist=new sfquat(d,n2*s,n2*s,n2*s);
        //return sfquat.mul(pointToTarget,twist);

        //}


        public bool IsNan()
        {
            return double.IsNaN(_quat[0]) ||
                   double.IsNaN(_quat[1]) ||
                   double.IsNaN(_quat[2]) ||
                   double.IsNaN(_quat[3]);

            // NOTE: IEEEE 754 says the following will still return false. We must use .IsNaN() method 
            //	return  _quat[0] == double.NaN || 
            //			_quat[1] == double.NaN || 
            //			_quat[2] == double.NaN || 
            //			_quat[3] == double.NaN;
        }

        // http://answers.unity3d.com/questions/35541/problem-finding-relative-rotation-from-one-quatern.html
        public static Quaternion CreateRelativeRotation(Quaternion a, Quaternion b)
        {
            // TODO: Jan.31.2014 - never been tested
            Quaternion relative = Quaternion.Inverse(a) * b;
            return relative;
        }

        public static Quaternion CreateRotationTo(Vector3d source, Vector3d dest, Vector3d up)
        {
            Matrix m = Matrix.CreateLookAt(source, dest, up);
            // return new Quaternion (m);

            double w = Math.Sqrt(1.0d + m.M11 + m.M22 + m.M33) * 0.5d;
            double w4_recip = 1.0d / (4.0d * w);
            double x = (m.M32 - m.M23) * w4_recip;
            double y = (m.M13 - m.M31) * w4_recip;
            double z = (m.M21 - m.M12) * w4_recip;

            // TODO: here if the start and end rotations are nearly the same, it'll produce NaN and in that case
            // we should return a rotation of 0,0,0,1 

            Quaternion result = new Quaternion(x, y, z, w);

#if DEBUG
            if (result.IsNan())
                result = new Quaternion();
#endif

            return result;
        }

        // http://stackoverflow.com/questions/12435671/quaternion-lookat-function

        /// <summary>
        /// Creates a rotation quaternion that will orient the source entity to
        /// face a destination coordinate.  This is in effect a "LookAt" for entities.
        /// </summary>
        /// <param name="source">Normalized destination coordinate</param>
        /// <param name="dest">Normalized destination coordinate</param>
        /// <param name="up">Normalized up vector</param>
        /// <returns></returns>
        //         public static Quaternion GetRotationTo(Vector3d source, Vector3d dest, Vector3d up)
        //         {
        //             double dot = Vector3d.DotProduct(source, dest);
        //
        //             if (Math.Abs(dot - -1.0d) < 0.000001d)
        //             {
        //                 // vector source and dest point exactly in the opposite direction, 
        //                 // so it is a 180 degrees turn around the up-axis
        //                 return new Quaternion(up, Utilities.MathHelper.DEGREES_TO_RADIANS  * 180.0d);
        //             }
        //             if (Math.Abs(dot - 1.0d) < 0.000001d)
        //             {
        //                 // vector source and dest point exactly in the same direction
        //                 // so we return the identity quaternion
        //                 return Quaternion.Identity();
        //             }
        //
        //             double rotAngle = Math.Acos(dot);
        //             // TODO: why isn't the rotation axis the dir?  
        //             Vector3d rotAxis = Vector3d.CrossProduct(source, dest);
        //             rotAxis = Vector3d.Normalize(rotAxis);
        //             return new Quaternion(rotAxis, rotAngle);
        //          
        //         }

        // note: below seems similar as above but with different code. untested.
        // however what is interesting is the special cases it makes for directly forward or
        // directly backwards facing quats where it special cases the handling.
        // http://gamedev.stackexchange.com/questions/15070/orienting-a-model-to-face-a-target
        //public static Quaternion GetRotation(Vector3 source, Vector3 dest, Vector3 up)
        //{
        //    float dot = Vector3.Dot(source, dest);

        //    if (Math.Abs(dot - (-1.0f)) < 0.000001f)
        //    {
        //        // vector a and b point exactly in the opposite direction, 
        //        // so it is a 180 degrees turn around the up-axis
        //        return new Quaternion(up, MathHelper.ToRadians(180.0f));
        //    }
        //    if (Math.Abs(dot - (1.0f)) < 0.000001f)
        //    {
        //        // vector a and b point exactly in the same direction
        //        // so we return the identity quaternion
        //        return Quaternion.Identity;
        //    }

        //    float rotAngle = (float)Math.Acos(dot);
        //    Vector3 rotAxis = Vector3.Cross(source, dest);
        //    rotAxis = Vector3.Normalize(rotAxis);
        //    return Quaternion.CreateFromAxisAngle(rotAxis, rotAngle);
        //}

        /// <summary>
        /// The multiplication Identity Quaternion (the addition identity quaternion which we don't use is 0 (0,0,0)
        /// </summary>
        /// <returns></returns>
        public static Quaternion Identity()
        {
            Quaternion q = new Quaternion();
            q._quat[0] = 0;
            q._quat[1] = 0;
            q._quat[2] = 0;
            q._quat[3] = 1;

            return q;
        }

        public bool IsNullOrEmpty()
        {
            return (_quat[0] == 0 && _quat[1] == 0 && _quat[2] == 0 && _quat[3] == 0);
        }

        public Vector3d Up()
        {
            double xx = _quat[0] * _quat[0];
            double zz = _quat[2] * _quat[2];
            double xy = _quat[0] * _quat[1];
            double yz = _quat[1] * _quat[2];
            double wx = _quat[3] * _quat[0];
            double wz = _quat[3] * _quat[2];

            Vector3d result;
            result.x = 2.0 * (xy + wz);
            result.y = 1.0 - 2.0 * (xx + zz);
            result.z = 2.0 * (yz - wx);

            return result;
        }

        public Vector3d Forward()
        {
            double xx = _quat[0] * _quat[0];
            double yz = _quat[1] * _quat[2];
            double wx = _quat[3] * _quat[0];

            double xz = _quat[0] * _quat[2];
            double yy = _quat[1] * _quat[1];
            double wy = _quat[3] * _quat[1];

            Vector3d result;
            result.x = 2.0 * (xz + wy);
            result.y = 2.0 * (yz - wx);
            result.z = 1.0d - 2.0 * (xx + yy);

            return result;
        }

        public double X
        {
            get { return _quat[0]; }
            set { _quat[0] = value; }
        }

        public double Y
        {
            get { return _quat[1]; }
            set { _quat[1] = value; }
        }

        public double Z
        {
            get { return _quat[2]; }
            set { _quat[2] = value; }
        }

        public double W
        {
            get { return _quat[3]; }
            set { _quat[3] = value; }
        }

        public double Length
        {
            get { return Math.Sqrt(_quat[0] * _quat[0] + _quat[1] * _quat[1] + _quat[2] * _quat[2] + _quat[3] * _quat[3]); }
        }

        public static Quaternion Normalize(Quaternion quat)
        {
            double invLength = 1d / quat.Length;

            return new Quaternion(quat.X * invLength, quat.Y * invLength, quat.Z * invLength, quat.W * invLength);
        }

        // https://github.com/ehsan/ogre/blob/master/OgreMain/src/OgreQuaternion.cpp
        public static Quaternion Inverse(Quaternion quat)
        {
            double mag = quat.W * quat.W + quat.X * quat.X + quat.Y * quat.Y + quat.Z * quat.Z;
            if (mag > 0.0d)
            {
                double magInvNorm = 1.0d / mag;
                return new Quaternion(-quat.X * magInvNorm, -quat.Y * magInvNorm, -quat.Z * magInvNorm, quat.W * magInvNorm);
            }
            else
            {
                // return an invalid result to flag the error
                return new Quaternion(0d, 0d, 0d, 0d);
            }

        }

        /// <summary>
        /// Given a quaternion (x, y, z, w), this method returns the quaternion (-x, -y, -z, w). 
        /// </summary>
        /// <param name="quat"></param>
        /// <returns>Unliked in Negate(), here the W component is the only compponent not negated</returns>
        public static Quaternion Conjugate(Quaternion quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        /// <summary>
        /// Negates all components of the quaternion.
        /// </summary>
        /// <param name="quat"></param>
        /// <returns></returns>
        public static Quaternion Negate(Quaternion quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, -quat.W);
        }

        public static Quaternion Scale(Quaternion q1, double scale)
        {
            Quaternion result = new Quaternion
            {
                X = (scale * q1.X),
                Y = (scale * q1.Y),
                Z = (scale * q1.Z),
                W = (scale * q1.W)
            };
            return result;
        }

        // xna concatenate
        public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
        {
            double x = value2.X;
            double y = value2.Y;
            double z = value2.Z;
            double w = value2.W;
            double x2 = value1.X;
            double y2 = value1.Y;
            double z2 = value1.Z;
            double w2 = value1.W;
            double num = y * z2 - z * y2;
            double num2 = z * x2 - x * z2;
            double num3 = x * y2 - y * x2;
            double num4 = x * x2 + y * y2 + z * z2;
            Quaternion result = new Quaternion();
            result.X = x * w2 + x2 * w + num;
            result.Y = y * w2 + y2 * w + num2;
            result.Z = z * w2 + z2 * w + num3;
            result.W = w * w2 - num4;
            return result;
        }

        public static Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            Quaternion result = new Quaternion
            {
                X = (q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y),
                Y = (q1.W * q2.Y - q1.X * q2.Z + q1.Y * q2.W + q1.Z * q2.X),
                Z = (q1.W * q2.Z + q1.X * q2.Y - q1.Y * q2.X + q1.Z * q2.W),
                W = (q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z)
            };

            return result;
        }

        //public static Quaternion Multiply(Quaternion value1, Quaternion value2)
        //{
        //    Quaternion result = new Quaternion();
        //    double single8 = value1.X;
        //    double single7 = value1.Y;
        //    double single6 = value1.Z;
        //    double single5 = value1.W;
        //    double single4 = value2.X;
        //    double single3 = value2.Y;
        //    double single2 = value2.Z;
        //    double single1 = value2.W;
        //    double single12 = (single7 * single2) - (single6 * single3);
        //    double single11 = (single6 * single4) - (single8 * single2);
        //    double single10 = (single8 * single3) - (single7 * single4);
        //    double single9 = ((single8 * single4) + (single7 * single3)) + (single6 * single2);
        //    result.X = ((single8 * single1) + (single4 * single5)) + single12;
        //    result.Y = ((single7 * single1) + (single3 * single5)) + single11;
        //    result.Z = ((single6 * single1) + (single2 * single5)) + single10;
        //    result.W = (single5 * single1) - single9;

        //    return result;
        //}

        //public static Vector3d  Multiply(Quaternion q, Vector3d v)
        //{
        //    Vector3d tmp = Vector3d.Normalize (v);
        //    Quaternion tmpQuat = new Quaternion (tmp.x, tmp.y, tmp.z, 0D);

        //    Quaternion result = tmpQuat * Quaternion.Conjugate (q);
        //    result = q * result;

        //    return new Vector3d (result.X , result.Y , result.Z , result.W );

        //}

        public static double DotProduct(Quaternion q1, Quaternion q2)
        {
            return q1.W * q2.W + q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z;

        }

        public static Quaternion RotateTowards(Quaternion from, Quaternion to, double maxDegreesDelta)
        {
            double angle = Quaternion.Angle(from, to);
            if (angle == 0.0d) return to;
            return Slerp(from, to, Math.Min(1.0d, maxDegreesDelta / angle));
        }

        public static double Angle(Quaternion a, Quaternion b)
        {
            double dot = Math.Min(Math.Abs(DotProduct(a, b)), 1.0F);
            return IsEqualUsingDot(dot) ? 0.0d : Math.Acos(dot) * 2.0d * Utilities.MathHelper.RADIANS_TO_DEGREES;
        }

        // Is the dot product of two quaternions within tolerance for them to be considered equal?
        private static bool IsEqualUsingDot(double dot)
        {
            // Returns false in the presence of NaN values.
            return dot > 1.0d - double.Epsilon;
        }


        public Vector3d GetAxisAngle(ref double angleRadians)
        {
            Vector3d axis;
            angleRadians = 2.0 * Math.Acos(_quat[3]);
            double sinHalfAngle = Math.Sin(angleRadians * .5d);
            if (sinHalfAngle != 0) // check for divide by zero 
            {
                axis.x = _quat[0] / sinHalfAngle;
                axis.y = _quat[1] / sinHalfAngle;
                axis.z = _quat[2] / sinHalfAngle;

                // TODO: i think according to this gamedev, the result asis should be normalized
                // http://www.gamedev.net/topic/310603-quaternion-to-axis-angle-and-back/
            }
            else
            {
                axis.x = 0;
                axis.y = 0;
                axis.z = 1;
            }

            return axis;
        }

        // http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/index.htm
        public Vector3d GetEulerAngles(bool degrees)
        {
            Vector3d angles;
            double sqw = _quat[3] * _quat[3];
            double sqx = _quat[0] * _quat[0];
            double sqy = _quat[1] * _quat[1];
            double sqz = _quat[2] * _quat[2];
            double unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            double test = _quat[0] * _quat[1] + _quat[2] * _quat[3];

            if (test > 0.499 * unit)
            { // singularity at north pole
                angles.y = 2 * Math.Atan2(_quat[0], _quat[3]);
                angles.x = Math.PI / 2;
                angles.z = 0;
            }
            else if (test < -0.499 * unit)
            { // singularity at south pole
                angles.y = -2 * Math.Atan2(_quat[0], _quat[3]);
                angles.x = -Math.PI / 2;
                angles.z = 0;
            }
            else
            {
                angles.y = Math.Atan2(2 * _quat[1] * _quat[3] - 2 * _quat[0] * _quat[2], sqx - sqy - sqz + sqw);
                angles.x = Math.Asin(2 * test / unit);
                angles.z = Math.Atan2(2 * _quat[0] * _quat[3] - 2 * _quat[1] * _quat[2], -sqx + sqy - sqz + sqw);
            }

            // make negative rotations, positive rotations
            angles = Utilities.MathHelper.WrapAngleRadians(angles);

            if (degrees)
            {
                angles.x *= Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.y *= Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.z *= Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
            }
            return angles;
        }

        /// <summary>
        /// TODO: this is buggy and does not work correctly in all cases.  converting quat
        /// to euler should be avoided i think.
        /// Returns euler angle representation of the quat in radians
        /// http://forums.create.msdn.com/forums/p/4574/62520.aspx //<-- TODO: this site has alternatives starting at ed022's 17th post
        /// </summary>
        /// <returns></returns>
        public Vector3d GetEulerAnglesOLD(bool degrees)
        {
            Vector3d angles;
            const double case1 = Math.PI / 2.0d;
            const double case2 = -Math.PI / 2.0d;
            // quat must be normalized
            angles.z = Math.Atan2(2.0d * (_quat[0] * _quat[1] +
                _quat[3] * _quat[2]),
                (_quat[3] * _quat[3] +
                _quat[0] * _quat[0] -
                _quat[1] * _quat[1] -
                _quat[2] * _quat[2]));
            double sine = -2.0d * (_quat[0] * _quat[2] - _quat[3] * _quat[1]);

            if (sine >= 1d)     //cases where value is 1 or -1 cause NAN
                angles.y = case1;
            else if (sine <= -1d)
                angles.y = case2;
            else
                angles.y = Math.Asin(sine);

            angles.x = Math.Atan2(2.0d * (_quat[3] * _quat[0] + _quat[1] * _quat[2]), (_quat[3] * _quat[3] - _quat[0] * _quat[0] - _quat[1] * _quat[1] + _quat[2] * _quat[2]));

            if (degrees)
            {
                angles.x *= Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.y *= Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
                angles.z *= Keystone.Utilities.MathHelper.RADIANS_TO_DEGREES;
            }
            return angles;
        }

        //public static Vector3d ToEulerAngles(Quaternion q)
        //{
        //    // Store the Euler angles in radians
        //    Vector3d pitchYawRoll = new Vector3d();

        //    double sqx = q.X * q.X;
        //    double sqy = q.Y * q.Y;
        //    double sqz = q.Z * q.Z;
        //    double sqw = q.W * q.W;

        //    // If quaternion is normalised the unit is one, otherwise it is the correction factor
        //    double unit = sqx + sqy + sqz + sqw;

        //    double test = q.X * q.Y + q.Z * q.W;
        //    //double test = q.X * q.Z - q.W * q.Y;

        //    if (test > 0.4999f * unit)                              // 0.4999f OR 0.5f - EPSILON
        //    {
        //        // Singularity at north pole
        //        pitchYawRoll.y = 2f * (float)Math.Atan2(q.X, q.W);  // Yaw
        //        pitchYawRoll.x = PIOVER2;                           // Pitch
        //        pitchYawRoll.z = 0f;                                // Roll
        //        return pitchYawRoll;
        //    }
        //    else if (test < -0.4999f * unit)                        // -0.4999f OR -0.5f + EPSILON
        //    {
        //        // Singularity at south pole
        //        pitchYawRoll.y = -2f * (float)Math.Atan2(q.X, q.W); // Yaw
        //        pitchYawRoll.x = -PIOVER2;                          // Pitch
        //        pitchYawRoll.z = 0f;                                // Roll
        //        return pitchYawRoll;
        //    }
        //    else
        //    {
        //        pitchYawRoll.y = (float)Math.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);       // Yaw
        //        pitchYawRoll.x = (float)Math.Asin(2f * test / unit);                                              // Pitch
        //        pitchYawRoll.z = (float)Math.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);      // Roll

        //        //pitchYawRoll.Y = (float)Math.Atan2(2f * q.X * q.W + 2f * q.Y * q.Z, 1 - 2f * (sqz + sqw));      // Yaw 
        //        //pitchYawRoll.X = (float)Math.Asin(2f * (q.X * q.Z - q.W * q.Y));                                // Pitch 
        //        //pitchYawRoll.Z = (float)Math.Atan2(2f * q.X * q.Y + 2f * q.Z * q.W, 1 - 2f * (sqy + sqz));      // Roll 
        //    }

        //    return pitchYawRoll;
        //}

        public byte GetComponentYRotationIndex()
        {
            Vector3d anglesRadians = GetEulerAngles(false);
            int snapLimit = 90; // for v1.0 we dont allow 45 degree increments.  Only 90

            double angleDegrees = anglesRadians.y * Utilities.MathHelper.RADIANS_TO_DEGREES;

            if (angleDegrees < 0.0)
                angleDegrees += 360;
            else if (angleDegrees > 360)
                angleDegrees = angleDegrees % 360;

            // snap to 90 degree increments
            int snapped = ((int)Math.Round(angleDegrees / snapLimit)) * snapLimit;

            byte result = (byte)(64 * snapped / snapLimit);
            return result;
            // 0 = 0 or 360 degrees
            // 32 = 45 degrees // not used
            // 64 = 90 degrees
            // 96 = 135 degrees // not used
            // 128 = 180 degrees
            // 160 = 225 degrees // not used
            // 192 = 270 degrees
            // 224 = 315 degrees // not used
        }

        // TODO: Slerp2 is being used in some places and Slerp in others.  I think all
        //       should use Slerp2		
        // Slerp - spherical interpolation of quaternions
        public static Quaternion Slerp(Quaternion start, Quaternion end, double t)
        {
            // NOTE: start and end must be normalized rotations
            double costheta = Quaternion.DotProduct(start, end);
            const double epsilon = 0.001d;
            double sclp, sclq;
            //// decide if one of the quaternions is backwards
            //double a = (x - quat2.x) * (x - quat2.x) + (y - quat2.y) * (y - quat2.y) + (z - quat2.z) * (z - quat2.z) + (r - quat2.r) * (r - quat2.r);
            //double b = (x + quat2.x) * (x + quat2.x) + (y + quat2.y) * (y + quat2.y) + (z + quat2.z) * (z + quat2.z) + (r + quat2.r) * (r + quat2.r);
            //if (a > b)
            //{
            //    //quato.Negate();
            //   costheta = -costheta;
            //   end.x *= -1;   // Reverse all signs
            //   end.y *= -1;
            //   end.z  *= -1;
            //   end.w  *= -1;
            //}

            // http://www.cs.wisc.edu/graphics/Courses/cs-838-1999/Students/thorek/final/Quatern.cpp
            // Make sure the two quaternions are not exactly opposite? (within a little slop).

            if (1.0d - costheta > epsilon)
            {
                // Standard case (slerp)
                double omega = Math.Acos(costheta);
                double sinom = Math.Sin(omega);
                sclp = Math.Sin((1.0d - t) * omega) / sinom;
                sclq = Math.Sin(t * omega) / sinom;
            }
            else
            {
                // very close. linear interpolation will be faster
                sclp = 1.0d - t;
                sclq = t;
            }

            return new Quaternion(sclp * start.X + sclq * end.X,
                                  sclp * start.Y + sclq * end.Y,
                                  sclp * start.Z + sclq * end.Z,
                                  sclp * start.W + sclq * end.W);

            // TODO: i never properly finished this function or tested it
            // Still here? Then the quaternions are nearly opposite so to avoid a divided by zero error
            // Calculate a perpendicular quaternion and slerp that direction
            sclp = Math.Sin((1.0d - t) * Math.PI);
            sclq = Math.Sin(t * Math.PI);
            return new Quaternion(
                sclp * start.W + sclq * end.Z,
                sclp * start.X + sclq * -end.Y,
                sclp * start.Y + sclq * end.X,
                sclp * start.Z + sclq * -end.W);

        }

        // xna slerp
        public static Quaternion Slerp2(Quaternion start, Quaternion end, double t)
        {
            double opposite;
            double inverse;
            double dot = DotProduct(start, end);
            const double epsilon = .001d;
            if (Math.Abs(dot) > 1.0d - epsilon)
            {
                inverse = 1.0d - t;
                opposite = t * Math.Sign(dot);
            }
            else
            {
                double acos = Math.Acos(Math.Abs(dot));
                double invSin = (1.0d / Math.Sin(acos));
                inverse = Math.Sin((1.0d - t) * acos) * invSin;
                opposite = Math.Sin(t * acos) * invSin * Math.Sign(dot);
            }

            return new Quaternion((inverse * start.X) + (opposite * end.X),
                   (inverse * start.Y) + (opposite * end.Y),
                   (inverse * start.Z) + (opposite * end.Z),
                   (inverse * start.W) + (opposite * end.W));
        }

        // xna lerp
        public static Quaternion Lerp(Quaternion start, Quaternion end, double amount)
        {
            double num = 1f - amount;
            Quaternion result = new Quaternion();
            double num2 = start.X * end.X + start.Y * end.Y + start.Z * end.Z + start.W * end.W;
            if (num2 >= 0f)
            {
                result.X = num * start.X + amount * end.X;
                result.Y = num * start.Y + amount * end.Y;
                result.Z = num * start.Z + amount * end.Z;
                result.W = num * start.W + amount * end.W;
            }
            else
            {
                result.X = num * start.X - amount * end.X;
                result.Y = num * start.Y - amount * end.Y;
                result.Z = num * start.Z - amount * end.Z;
                result.W = num * start.W - amount * end.W;
            }

            double num3 = result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W;
            double num4 = 1d / Math.Sqrt(num3);
            result.X *= num4;
            result.Y *= num4;
            result.Z *= num4;
            result.W *= num4;
            return result;
        }

        // -------------------------------------------------------------
        // SLERP: Spherical Linear Interpolation
        // Step from q1 to q2, 0=<t=<1
        // SLERP(q1,q2,0) = q1
        // SLERP(q1,q2,1) = q2
        // -------------------------------------------------------------
        //Quat SLERP(Quat q1, Quat q2, float t)
        //{
        //    Quat result = new Quat();
        //    float[] to1 = new float[4];
        //    float omega, cos_omega, sin_omega, scale0, scale1;

        //    // calc cosine
        //    cos_omega = q1.r * q2.r + q1.x * q2.x + q1.y * q2.y + q1.z * q2.z;

        //    // adjust signs (if necessary)
        //    if (cos_omega < 0.0)
        //    {
        //        cos_omega = -cos_omega;
        //        to1[0] = -q2.r;
        //        to1[1] = -q2.x;
        //        to1[2] = -q2.y;
        //        to1[3] = -q2.z;

        //    }
        //    else
        //    {
        //        to1[0] = q2.r;
        //        to1[1] = q2.x;
        //        to1[2] = q2.y;
        //        to1[3] = q2.z;
        //    }


        //    // calculate coefficients

        //    if ((1.0 - cos_omega) > 0.01)
        //    {
        //        // standard case (slerp)
        //        omega = (float)Math.acos(cos_omega);
        //        sin_omega = sin(omega);
        //        scale0 = sin((1.0 - t) * omega) / sin_omega;
        //        scale1 = sin(t * omega) / sin_omega;


        //    }
        //    else
        //    {
        //        // "from" and "to" Quats are very close 
        //        //  ... so we can do a linear interpolation
        //        scale0 = 1.0 - t;
        //        scale1 = t;
        //    }
        //    // calculate final values
        //    result.r = scale0 * q1.r + scale1 * to1[0];
        //    result.x = scale0 * q1.x + scale1 * to1[1];
        //    result.y = scale0 * q1.y + scale1 * to1[2];
        //    result.z = scale0 * q1.z + scale1 * to1[3];

        //    return result;
        //}


        // http://physicsforgames.blogspot.com/2010/02/quaternions.html
        //        How to Integrate a Quaternion:
        //
        //Updating the dynamical state of a rigid body is referred to as integration. If you represent the orientation of this body with a quaternion, you will need to know how to update it. This is done with the following quaternion formula.
        //
        //q' = Δq q
        //
        //We calculate Δq using a 3D vector ω whose magnitude represents the angular velocity, and whose direction represents the axis of the angular velocity. We also use the time step Δt over which the velocity should be applied. Δq is still a rotation quaternion, and has the same form involving sines and cosines of a half angle. We use the angular velocity and time step to construct a vector θ, whose magnitude is the half angle, and whose direction is the axis.
        //
        //θ = ωΔt/2
        //
        //Note: I've included the factor of 1/2, which shows up inside the trig functions of the rotation quaternion. Expressing the rotation quaternion in terms of this vector you have
        //
        //Δq = ( cos(θ), (θ/|θ|) sin(θ) )
        //
        //This works well, however this formula becomes numerically unstable as |θ| approaches zero. If we can detect that |θ| is small, we can safely use the Taylor series expansion of the sin and cos functions. The "low angle" version of this formula is
        //
        //Δq = (1 - |θ|2/2, θ - θ|θ|2/6)
        //
        //We use the first 3 terms of the Taylor series expansion, so we should ensure that the fourth term is less than machine precision before we use the "low angle" version. The fourth term of the expansion is
        //
        //|θ|4/24 < ε
        //
        //Here is a sample function for integrating a quaternion with a given angular velocity and time step
        //
        //Quat QuatIntegrate(const Quat& q, const Vector& omega, float deltaT) { Quat deltaQ; Vector theta = VecScale(omega, deltaT * 0.5f); float thetaMagSq = VecMagnitudeSq(theta); float s; if(thetaMagSq * thetaMagSq / 24.0f < MACHINE_SMALL_FLOAT) { deltaQ.w = 1.0f - thetaMagSq / 2.0f; s = 1.0f - thetaMagSq / 6.0f; } else { float thetaMag = sqrt(thetaMagSq); deltaQ.w = cos(thetaMag); s = sin(thetaMag) / thetaMag; } deltaQ.x = theta.x * s; deltaQ.y = theta.y * s; deltaQ.z = theta.z * s; return QuatMultiply(deltaQ, q); }
        //


        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);
        }

        //public static Vector3d operator *(Quaternion q, Vector3d v)
        //{
        //    // nVidia SDK implementation
        //    Vector3d uv, uuv;

        //    Vector3d qvec;
        //    qvec.x = q.X;
        //    qvec.y = q.Y;
        //    qvec.z = q.Z;                 
        //    uv = Vector3d.CrossProduct(qvec,v);                
        //    uuv = Vector3d.CrossProduct(qvec, uv);                 
        //    uv *= (2.0 * q.W);                 
        //    uuv *= 2.0;                  
        //    return v + uv + uuv; 
        //}

        // http://www.java-gaming.org/index.php?PHPSESSID=dkpq2dfr89eks0atgndch2cjm3&topic=25517.msg220313#msg220313
        public static Vector3d operator *(Quaternion q, Vector3d v)
        {
            double k0 = q.W * q.W - 0.5;
            double k1;
            double rx, ry, rz;

            // k1 = Q.V  
            k1 = v.x * q.X;
            k1 += v.y * q.Y;
            k1 += v.z * q.Z;

            // (qq-1/2)V+(Q.V)Q  
            rx = v.x * k0 + q.X * k1;
            ry = v.y * k0 + q.Y * k1;
            rz = v.z * k0 + q.Z * k1;

            // (Q.V)Q+(qq-1/2)V+q(QxV)  
            rx += q.W * (q.Y * v.z - q.Z * v.y);
            ry += q.W * (q.Z * v.x - q.X * v.z);
            rz += q.W * (q.X * v.y - q.Y * v.x);

            //  2((Q.V)Q+(qq-1/2)V+q(QxV))  
            rx += rx;
            ry += ry;
            rz += rz;

            return new Vector3d(rx, ry, rz);
        }

        public static Quaternion operator *(Quaternion q1, double scale)
        {
            return Scale(q1, scale);
        }

        public static Quaternion operator *(Quaternion q1, Quaternion q2)
        {
            return Multiply(q1, q2);
        }

        public static Matrix ToMatrix(Quaternion quat)
        {
            return new Matrix(quat);
        }

        //public static Quaternion operator /(Quaternion q, double scale)
        //{

        //}

        public override bool Equals(object obj)
        {
            if (obj is Quaternion == false)
                return false;

            Quaternion arg = (Quaternion)obj;
            return _quat[0] == arg._quat[0] &&
                _quat[1] == arg._quat[1] &&
                _quat[2] == arg._quat[2] &&
                _quat[3] == arg._quat[3];
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
            return 0;
        }

        public override string ToString()
        {
            string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
            return string.Format("{0}{1}{2}{3}{4}{5}{6}", _quat[0], delimiter,
                                                    _quat[1], delimiter,
                                                    _quat[2], delimiter,
                                                    _quat[3]);
        }

        public static string ToString(Quaternion[] quatArray)
        {

            if (quatArray == null || quatArray.Length == 0) return null;

            char[] delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string result = string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(result);

            for (int i = 0; i < quatArray.Length; i++)
            {
                sb.Append(quatArray[i].ToString());
                if (i != quatArray.Length - 1)
                    // append delimiter. NOTE: same delimiter is used even between quaternions and not just their elements
                    sb.Append(keymath.ParseHelper.English.XMLAttributeDelimiter); // note: we use a single delimiter here not the char[] since that holds multiple delims
            }
            result = sb.ToString();

            return result;
        }
    }
}