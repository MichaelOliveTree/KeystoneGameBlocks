#region

using System;
using System.Diagnostics;

#endregion

namespace Core.Types
{
    public class Quaternion
    {
        private double[] _quat;

        //Quaternions add a fourth element to the [ x, y, z] values that define a vector, 
        //resulting in arbitrary 4D vectors. However, the following illustrates how each 
        //element of a unit quaternion relates to an axis-angle rotation (where q 
        //represents a unit quaternion (x, y, z, w), axis is normalized, and theta is the
        //desired CCW rotation about the axis):

        //q.x = sin(theta/2) * axis.x
        //q.y = sin(theta/2) * axis.y
        //q.z = sin(theta/2) * axis.z
        //q.w = cos(theta/2)
        public Quaternion()
        {
            _quat = new double[4];
        }

        public Quaternion(double x, double y, double z, double w) : this()
        {
            _quat[0] = x;
            _quat[1] = y;
            _quat[2] = z;
            _quat[3] = w;
        }

        public Quaternion(Vector3d axis, double angleRadians) : this()
        {
            double length, Temp2;
            length = axis.Length;

            if (length != 1d)
            {
                Trace.Assert(length == 1, "Axis is not unit vector");
                Temp2 = Math.Sin(angleRadians * 0.5d) / length;
            }
            else
                Temp2 = Math.Sin(angleRadians * 0.5d);

            _quat[0] = axis.x*Temp2;
            _quat[1] = axis.y*Temp2;
            _quat[2] = axis.z*Temp2;
            _quat[3] = Math.Cos(angleRadians * 0.5d);
        }

        // todo: must read this and verify my algos are optimized
        // http://www.edn.com/archives/1995/030295/05df3.htm
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
                sinv = 0.5/s;
                _quat[0] = (rotationMatrix.M32 - rotationMatrix.M23)*sinv;
                _quat[1] = (rotationMatrix.M13 - rotationMatrix.M31)*sinv;
                _quat[2] = (rotationMatrix.M21 - rotationMatrix.M12)*sinv;
                _quat[3] = s*0.5;
            }
            else if (rotationMatrix.M11 > rotationMatrix.M22 && rotationMatrix.M11 > rotationMatrix.M33)
            {
                s = Math.Sqrt(rotationMatrix.M11 - (rotationMatrix.M22 + rotationMatrix.M33) + 1);
                sinv = 0.5/s;
                _quat[0] = s*0.5;
                _quat[1] = (rotationMatrix.M12 + rotationMatrix.M21)*sinv;
                _quat[2] = (rotationMatrix.M31 + rotationMatrix.M13)*sinv;
                _quat[3] = (rotationMatrix.M32 - rotationMatrix.M23)*sinv;
            }
            else if (rotationMatrix.M22 > rotationMatrix.M33)
            {
                s = Math.Sqrt(rotationMatrix.M22 - (rotationMatrix.M33 + rotationMatrix.M11) + 1);
                sinv = 0.5/s;
                _quat[0] = (rotationMatrix.M12 + rotationMatrix.M21)*sinv;
                _quat[1] = s*0.5;
                _quat[2] = (rotationMatrix.M23 + rotationMatrix.M32)*sinv;
                _quat[3] = (rotationMatrix.M13 - rotationMatrix.M31)*sinv;
            }
            else
            {
                s = Math.Sqrt(rotationMatrix.M33 - (rotationMatrix.M11 + rotationMatrix.M22) + 1);
                sinv = 0.5/s;
                _quat[0] = (rotationMatrix.M31 + rotationMatrix.M13)*sinv;
                _quat[1] = (rotationMatrix.M23 + rotationMatrix.M32)*sinv;
                _quat[2] = s*0.5;
                _quat[3] = (rotationMatrix.M21 - rotationMatrix.M12)*sinv;
            }
        }

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
            get { return Distance(this, this); }
        }

        public static Quaternion Normalize(Quaternion quat)
        {
            double L = quat.Length;
            return new Quaternion(quat.X/L, quat.Y/L, quat.Z/L, quat.W/L);
        }

        public static double Distance(Quaternion q1, Quaternion q2)
        {
            return Math.Sqrt(q1.X*q2.X + q1.Y*q2.Y + q1.Z*q2.Z + q1.W*q2.W);
        }

        // Given a quaternion (x, y, z, w), this method returns the quaternion (-x, -y, -z, w). 
        public static Quaternion Conjugate(Quaternion quat)
        {
            return new Quaternion(-quat.X, -quat.Y, -quat.Z, quat.W);
        }

        public static Quaternion Scale(Quaternion q1, double scale)
        {
            Quaternion result = new Quaternion
                                    {
                                        X = (scale*q1.X),
                                        Y = (scale*q1.Y),
                                        Z = (scale*q1.Z),
                                        W = (scale*q1.W)
                                    };
            return result;
        }

        public static Quaternion Multiply(Quaternion q1, Quaternion q2)
        {
            Quaternion result = new Quaternion
                                    {
                                        X = (q1.W*q2.X + q1.X*q2.W + q1.Y*q2.Z - q1.Z*q2.Y),
                                        Y = (q1.W*q2.Y - q1.X*q2.Z + q1.Y*q2.W + q1.Z*q2.X),
                                        Z = (q1.W*q2.Z + q1.X*q2.Y - q1.Y*q2.X + q1.Z*q2.W),
                                        W = (q1.W*q2.W - q1.X*q2.X - q1.Y*q2.Y - q1.Z*q2.Z)
                                    };

            return result;
        }

        // http://msdn.microsoft.com/en-us/library/bb205417(VS.85).aspx
        // YawPitchRoll correctly implemented from John Ratcliff's Code Suppository http://codesuppository.blogspot.com/
        // note: he indicates an error with the one in d3d
        private void QuaternionRotationYawPitchRoll(float yaw, float pitch, float roll, out float[] quat)
        {
            quat = new float[4];
            float sinY, cosY, sinP, cosP, sinR, cosR;
            sinY = (float) Math.Sin(0.5f*yaw);
            cosY = (float) Math.Cos(0.5f*yaw);

            sinP = (float) Math.Sin(0.5f*pitch);
            cosP = (float) Math.Cos(0.5f*pitch);

            sinR = (float) Math.Sin(0.5f*roll);
            cosR = (float) Math.Cos(0.5f*roll);

            quat[0] = cosY*sinP*cosR + sinY*cosP*sinR;
            quat[1] = sinY*cosP*cosR - cosY*sinP*sinR;
            quat[2] = cosY*cosP*sinR - sinY*sinP*cosR;
            quat[3] = cosY*cosP*cosR + sinY*sinP*sinR;
        }

        public static double DotProduct (Quaternion q1, Quaternion q2)
        {
            return q1.W * q2.W + q1.X*q2.X + q1.Y*q2.Y + q1.Z*q2.Z;

        }

         public void  GetAxisAngle(ref Vector3d axis, ref double angleRadians) 
        {
            angleRadians = 2.0 * Math.Acos(_quat[3]);
            double ss = Math.Sin(angleRadians * .5d);
            if (ss != 0) {
                axis.x = _quat[0] / ss;
                axis.y = _quat[1] / ss;
                axis.z = _quat[2] / ss;
            }
            else
            {
                axis.x = 0;
                axis.y = 0;
                axis.z = 1;
            }
        }

         public void GetEulerAngles(ref Vector3d angles)
         {
             const double case1 = Math.PI / 2.0f * 180.0f;
             const double case2 = -Math.PI / 2.0f;

             angles.z = Math.Atan2(2.0f * (_quat[0] * _quat[1] + _quat[3] * _quat[2]), (_quat[3] * _quat[3] + _quat[0] * _quat[0] - _quat[1] * _quat[1] - _quat[2] * _quat[2]));
             double sine = -2.0f * (_quat[0] * _quat[2] - _quat[3] * _quat[1]);

             if (sine >= 1)     //cases where value is 1 or -1 cause NAN
                 angles.y = case1;
             else if (sine <= -1)
                 angles.y = case2;
             else
                 angles.y = Math.Asin(sine);

             angles.x = Math.Atan2(2.0f * (_quat[3] * _quat[0] + _quat[1] * _quat[2]), (_quat[3] * _quat[3] - _quat[0] * _quat[0] - _quat[1] * _quat[1] + _quat[2] * _quat[2]));
         }


        // Slerp (spherical interpolation of quaternions
        public static Quaternion Slerp(Quaternion start, Quaternion end, double t)
        {
            // need to implement my own QuaternionDot function
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

            // Make sure the two quaternions are not exactly opposite? (within a little slop).

                if ((1.0d - costheta) > epsilon)
                {
                    // Standard case (slerp)
                    double omega = Math.Acos(costheta);
                    double sinom = Math.Sin(omega);
                    sclp = Math.Sin((1.0f - t)*omega)/sinom;
                    sclq = Math.Sin(t*omega)/sinom;
                }
                else
                {
                    // very close. linear interpolation will be faster
                    sclp = 1.0f - t;
                    sclq = t;
                }

                return new Quaternion(sclp*start.X + sclq*end.X,
                                      sclp*start.Y + sclq*end.Y,
                                      sclp*start.Z + sclq*end.Z,
                                      sclp*start.W + sclq*end.W);
            

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

        public static Quaternion operator +(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(q1.X + q2.X, q1.Y + q2.Y, q1.Z + q2.Z, q1.W + q2.W);
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
            // convert quaterinion rotation to matrix, zeros out the translation component.
        {
            return new Matrix(quat);
        }
    }
}