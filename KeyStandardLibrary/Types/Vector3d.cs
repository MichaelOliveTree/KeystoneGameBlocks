using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Keystone.Types
{
    // This attribute is not required at least for the PropertyGrid using PropertyBags
    // because you can specify the converter to use for each PropertySpec item in the bag.
    // But it is needed for KeyPluginEntityEdit.Animations for modifying keyframe values in the plugin GUI interface
    [TypeConverter(typeof(Keystone.TypeConverters.Vector3dConverter))]
    public struct Vector3d
    {
        public double x;
        public double y;
        public double z;

        public static Vector3d Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            if (values == null || values.Length != 3) throw new ArgumentException();
            Vector3d results;
            results.x = double.Parse(values[0]);
            results.y = double.Parse(values[1]);
            results.z = double.Parse(values[2]);
            return results;
        }

        public Vector3d(string delimitedString)
        {
            Vector3d parse = Vector3d.Parse(delimitedString);
            this.x = parse.x;
            this.y = parse.y;
            this.z = parse.z;
        }

        public static Vector3d[] ParseArray(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);
            if (values == null || values.Length < 3 || values.Length % 3 != 0) throw new ArgumentException();

            int arraySize = values.Length / 3;
            Vector3d[] results = new Vector3d[arraySize];

            int j = 0;
            for (int i = 0; i < results.Length; i++)
            {
                results[i].x = double.Parse(values[j]); j++;
                results[i].y = double.Parse(values[j]); j++;
                results[i].z = double.Parse(values[j]); j++;
            }
            return results;
        }

        public Vector3d(double Vx, double Vy, double Vz)
        {
            x = Vx;
            y = Vy;
            z = Vz;
        }

        public Vector3d(Quaternion axisAngle)
        {

            double angleRadians = 0; // this value will be lost, only the axis is kept
            Vector3d result = axisAngle.GetAxisAngle(ref angleRadians);
            x = result.x;
            y = result.y;
            z = result.z;
        }

        public static Vector3d Zero()
        {
            Vector3d v;
            v.x = v.y = v.z = 0d;
            return v;
        }

        public static Vector3d MaxValue
        {
            get
            {
                Vector3d v;
                v.x = v.y = v.z = double.MaxValue;
                return v;
            }
        }

        public static Vector3d Up()
        {
            Vector3d v;
            v.x = 0d;
            v.y = 1d;
            v.z = 0d;
            return v;
        }

        public static Vector3d Right()
        {
            Vector3d v;
            v.x = 1d;
            v.y = 0d;
            v.z = 0d;
            return v;
        }

        public static Vector3d Forward()
        {
            Vector3d v;
            v.x = 0d;
            v.y = 0d;
            v.z = 1d;
            return v;
        }

        public double Length
        {
            get { return GetLength(this); }
        }

        public double LengthSquared()
        {
            return Vector3d.GetLengthSquared(this);
        }

        public void ZeroVector()
        {
            x = y = z = 0d;
        }

        public static void OrthoNormalize(ref Vector3d normal, ref Vector3d tangent)
        {
            normal = Normalize(normal);
            Vector3d proj = Scale(DotProduct(tangent, normal));
            tangent = tangent - proj;
            tangent = Normalize(tangent);
        }

        public double Normalize()
        {
            double l = Length;
            if (l == 0) return 0d; //  new Vector3d(0, 0, 0);
            double inverse = 1.0d / l;
            x *= inverse;
            y *= inverse;
            z *= inverse;
            return l;
        }
        public static Vector3d Normalize(Vector3d vec)
        {
            double dummy;
            return Normalize(vec, out dummy);
        }

        public static Vector3d Normalize(Vector3d vec, out double length)
        {
            double t = vec.Normalize();
            length = t;
            return vec;
        }

        public static Vector3d[] TransformNormalArray(Vector3d[] v, Matrix m)
        {
            Vector3d[] result = new Vector3d[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = TransformNormal(v[i], m);

            return result;
        }

        public static Vector3d[] TransformCoordArray(Vector3d[] v, Matrix m)
        {
            Vector3d[] result = new Vector3d[v.Length];
            for (int i = 0; i < v.Length; i++)
                result[i] = TransformCoord(v[i], m);

            return result;
        }
        /// <summary>
        /// 3x3 matrix transform but assumes the vector is a normal and so only
        /// scaling and rotation will be applied, not translation
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector3d TransformNormal(Vector3d v, Matrix m)
        {
            Vector3d result;
            if (m.IsNullOrEmpty())
            {
                result.x = v.x;
                result.y = v.y;
                result.z = v.z;
                return result;
            }
            result.x = (v.x * m.M11) + (v.y * m.M21) + (v.z * m.M31);
            result.y = (v.x * m.M12) + (v.y * m.M22) + (v.z * m.M32);
            result.z = (v.x * m.M13) + (v.y * m.M23) + (v.z * m.M33);
            return result;
        }

        public static Vector3d TransformNormal(Vector3d v, Quaternion q)
        {
            if (q.Equals(Quaternion.Identity()) || q.IsNullOrEmpty() || v.IsNullOrEmpty())
                return v;

            return TransformNormal(v, Quaternion.ToMatrix(q));
        }

        /// <summary>
        /// 3x4 matrix transform.  This is not intended to be used with a 4x4 matrix such as a projection matrix
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector3d TransformCoord(Vector3d v, Matrix m)
        {
            if (m.Equals(Matrix.Identity()) || m.IsNullOrEmpty() || v.IsNullOrEmpty())
                return v;

            Vector3d result;
            result.x = (v.x * m.M11) + (v.y * m.M21) + (v.z * m.M31) + m.M41;
            result.y = (v.x * m.M12) + (v.y * m.M22) + (v.z * m.M32) + m.M42;
            result.z = (v.x * m.M13) + (v.y * m.M23) + (v.z * m.M33) + m.M43;
            return result;
        }

        public static Vector3d TransformCoord(Vector3d v, double M11, double M12, double M13,
                                                                double M21, double M22, double M23,
                                                                double M31, double M32, double M33,
                                                                double M41, double M42, double M43)
        {
            Vector3d result;
            result.x = (v.x * M11) + (v.y * M21) + (v.z * M31) + M41;
            result.y = (v.x * M12) + (v.y * M22) + (v.z * M32) + M42;
            result.z = (v.x * M13) + (v.y * M23) + (v.z * M33) + M43;
            return result;
        }

        public static Vector3d TransformCoord(Vector3d v, Quaternion q)
        {

            if (q.IsNullOrEmpty() || v.IsNullOrEmpty())
                return v;

            return TransformCoord(v, Quaternion.ToMatrix(q));
        }

        public static double GetDistance3d(Vector3d v, Vector3d v2)
        {
            return Math.Sqrt(GetDistance3dSquared(v, v2));
        }
        public static double GetLength(double x, double y, double z)
        {
            return Math.Sqrt(GetLengthSquared(x, y, z));
        }
        public static double GetLength(Vector3d v)
        {
            return Math.Sqrt(GetLengthSquared(v));
        }

        public static double GetLengthSquared(double x, double y, double z)
        {
            return (x * x) + (y * y) + (z * z);
        }
        public static double GetLengthSquared(Vector3d v)
        {
            return GetLengthSquared(v.x, v.y, v.z);
        }
        public static double GetDistance3dSquared(Vector3d v1, Vector3d v2)
        {
            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;
            double dz = v1.z - v2.z;
            return GetLengthSquared(dx, dy, dz);
        }

        /// <summary>
        /// Returns angle between two vectors in radians.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double AngleBetweenVectors(Vector3d v1, Vector3d v2)
        {
            double dot = DotProduct(v1, v2);
            double vectorsMagnitude = v1.Length * v2.Length;
            double angleRadians = Math.Acos(dot / vectorsMagnitude);

#if DEBUG
            //if (v1 == Vector3d.Up())
             //   System.Diagnostics.Debug.WriteLine("Determining if v2 is parallel to Up vector");
#endif
            if (double.IsNaN(angleRadians))
                return 0;
            else
                return angleRadians;
        }

        public static bool AreParallel(Vector3d v1, Vector3d v2, double epsilon)
        {
            return AngleBetweenVectors(v1, v2) <= epsilon;
        }

        /// <summary>
        /// Flips a vector.  Note: To avoid confusion, I've deleted the Vector3d.Inverse() function altogether
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3d Negate(Vector3d v)
        {
            Vector3d result;
            result.x = -v.x;
            result.y = -v.y;
            result.z = -v.z;
            return result;
        }

        /// <summary>
        /// dot productive is commutative (i.e.  v1 dot v2 == v2 dot v1)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double DotProduct(Vector3d v1, Vector3d v2)
        {
            return (v1.x * v2.x + v1.y * v2.y + v1.z * v2.z);
        }

        /// <summary>
        /// cross product is NOT commutative (ie.. v1 cross v2 != v2 cross v1)
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector3d CrossProduct(Vector3d v1, Vector3d v2)
        {
            Vector3d vResult;
            vResult.x = v1.y * v2.z - v1.z * v2.y;
            vResult.y = v1.z * v2.x - v1.x * v2.z;
            vResult.z = v1.x * v2.y - v1.y * v2.x;
            return vResult;
        }

        public static Vector3d Subtract(Vector3d v1, Vector3d v2)
        {
            Vector3d result;
            result.x = v1.x - v2.x;
            result.y = v1.y - v2.y;
            result.z = v1.z - v2.z;
            return result;
        }

        public static Vector3d Add(Vector3d v1, Vector3d v2)
        {
            Vector3d result;
            result.x = v1.x + v2.x;
            result.y = v1.y + v2.y;
            result.z = v1.z + v2.z;
            return result;
        }

        public static Vector3d Scale(Vector3d v1, double scale)
        {
            Vector3d result;
            result.x = v1.x * scale;
            result.y = v1.y * scale;
            result.z = v1.z * scale;
            return result;
        }

        public static Vector3d Scale(double scale)
        {
            Vector3d result;
            result.x = scale;
            result.y = scale;
            result.z = scale;
            return result;
        }

        public Vector3d ProjectOnToPlane(Vector3d planeNormal)
        {
            Vector3d result = this;

            double sqrMag = Vector3d.DotProduct(planeNormal, planeNormal);
            if (sqrMag > double.Epsilon)
            {
                double dot = Vector3d.DotProduct(this, planeNormal);
                result.x = this.x - planeNormal.x * dot / sqrMag;
                result.y = this.y - planeNormal.y * dot / sqrMag;
                result.z = this.z - planeNormal.z * dot / sqrMag;
            }

            return result;
        }

        public static Vector3d FromTV3DVector(Vector3d v)
        {
            Vector3d result;
            result.x = v.x;
            result.y = v.y;
            result.z = v.z;
            return result;
        }

#region move to KeyStandardLibrary.Utilities.InterpolationHelper.cs and .MathHelper.cs
        // clamp the vector's magnitude (length) to the limit length
        public static Vector3d Limit(Vector3d vec, double limit)
        {
            if (vec.Length > limit)
                return Normalize(vec) * limit;

            return vec;
        }


        // yes you can do spherical interpolation between two vectors
        // http://keithmaggio.wordpress.com/2011/02/15/math-magician-lerp-slerp-and-nlerp/
        public static Vector3d Slerp(Vector3d start, Vector3d end, float weight)
        {
           // Dot product - the cosine of the angle between 2 vectors.
            double dot = Vector3d.DotProduct(start, end);
            // Clamp it to be in the range of Acos()
            Utilities.MathHelper.Clamp(dot, -1.0f, 1.0f);
            // Acos(dot) returns the angle between start and end,
            // And multiplying that by percent returns the angle between
            // start and the final result.
            double theta = Math.Acos(dot) * weight;
            Vector3d RelativeVec = end - start * dot;
            RelativeVec.Normalize();     // Orthonormal basis
            // The final result.
            return ((start * Math.Cos(theta)) + (RelativeVec * Math.Sin(theta)));
        }

        // http://keithmaggio.wordpress.com/2011/02/15/math-magician-lerp-slerp-and-nlerp/
        // Nlerp: Nlerp is our solution to Slerp�s computational cost. Nlerp also handles 
        // rotation and is much less computationally expensive, however it, too has it�s drawbacks.
        // Both travel a torque-minimal path, but Nlerp is commutative where Slerp is not, and 
        // Nlerp aslo does not maintain a constant velocity, which, in some cases, may be a 
        // desired effect. Implementing Nlerp in place of some Slerp calls may produce the same 
        // effect and even save on some FPS. However, with every optimization, using this improperly
        // may cause undesired effects. Nlerp should be used more, but it doesn�t mean cut out Slerp 
        // all together. Nlerp is very easy, too. Just normalize the result from Lerp()!
        public static Vector3d NLerp(Vector3d start, Vector3d end, double weight)
        {
            Vector3d result = Lerp(start, end, weight);
            result.Normalize();
            return result;
        }

        /// <summary>
        /// TODO: In my actor waypoint following, I should be using Lerp and computing a weight based on the total elapsed
        /// to get to my end point and this way regardless of frame rate or even alt_tab where tons of time has passed, It'll not overshoot
        /// although, if there are linked waypoints, we would need to subtract remaining time to get the actor to move to the next 
        /// i'll have to double check my code.  Also when alt_tabbed the simulation should keep going, just rendering should stop.  Or
        /// if i do pause the simualtion, on re-start the elapsed values should pause and recalibrate on resume as well.
        /// note: also in my spline following, rather than use waypoints and going from point to point, i could just based on elapsed, 
        /// input the value and get the new postion on th espline dynamically and never worry about overshooting/undershooting anything
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="weight">typically 0 - 1.0 but if outside the bounds, results in under or overshoot along that path e.g. 2.0 would be twice as far as the end from the start. 
        /// Typically you could compute weight as i / N  where i is the current itterator count and N is the max number of itterations
        /// so 0 to 20, step 2 would be  2 / 20  and that would result in weight falling in the range of 0.0 to 1.0</param>
        /// <returns></returns>
        public static Vector3d Lerp(Vector3d start, Vector3d end, double weight)
        {
            return (start * (1.0d - weight)) + (end * weight);
        }

        private static Vector3d Lerp(Vector3d start, Vector3d end, double step, double maxSteps)
        {
            return Lerp(start, end, step / maxSteps);
        }

        /// <summary>
        /// Accelerates from start and slows down towards end.
        /// http://sol.gfxile.net/interpolation/
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static Vector3d LerpSmoothStep(Vector3d start, Vector3d end, double step, double maxSteps)
        {
            return Lerp(start, end, SmoothStep(step / maxSteps));
        }

        public static Vector3d LerpSmoothStep(Vector3d start, Vector3d end, double weight)
        {
            return Lerp(start, end, SmoothStep(weight));
        }

        public static Vector3d LerpSmoothAcceleration(Vector3d start, Vector3d end, double weight)
        {
            return Lerp(start, end, SmoothAcceleration(weight));
        }

        public static Vector3d LerpSmoothDeceleration(Vector3d start, Vector3d end, double weight)
        {
            return Lerp(start, end, SmoothDeceleration(weight));
        }

        /// <summary>
        /// Adds acceleration and deceleration to the interpolation
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double SmoothStep(double weight)
        {
            return (weight * weight * (3d - 2d * weight));
        }

        /// <summary>
        /// Adds acceleration but no deceleration
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double SmoothAcceleration(double weight)
        {
            return weight * weight;
        }

        /// <summary>
        /// Adds deceleration but no acceleration
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double SmoothDeceleration(double weight)
        {
            return 1d - (1d - weight) * (1d - weight) * (1d - weight);
        }

        /// <summary>
        /// One rather handy algorithm, especially when you don't necessarily
        /// know how the target will behave in the future (such as a camera
        /// tracking the player's character), is to apply weighted average
        /// to the value.
        /// where 'weight' is the current value, w is the value towards which
        /// we want to move, and N is the slowdown factor. The higher N, the
        /// slower 'weight' approaches w.
        /// http://sol.gfxile.net/interpolation/
        /// </summary>
        /// <param name="weight"></param>
        /// <returns></returns>
        private static double WeightedAverage(double weight)
        {
            // TODO: return ((weight* (N - 1)) + w) / N; 
            return 0;
        }
#endregion

        public static Vector3d operator -(Vector3d v1)
        {
            return Negate(v1);
        }

        public static Vector3d operator -(Vector3d v1, Vector3d v2)
        {
            return Subtract(v1, v2);
        }

        public static Vector3d operator +(Vector3d v1, Vector3d v2)
        {
            return Add(v1, v2);
        }

        // Multiplying a quaternion q with a vector v applies the q-rotation to v
        public static Vector3d operator *(Vector3d vec, Quaternion quat)
        {
            // http://content.gpwiki.org/index.php/OpenGL%3aTutorials%3aUsing_Quaternions_to_represent_rotation#Rotating_vectors
            Vector3d vn = Vector3d.Normalize(vec);

            Quaternion vecQuat = new Quaternion(vn.x, vn.y, vn.z, 0.0d);
            Quaternion resultQuat = vecQuat * Quaternion.Conjugate(quat);

            resultQuat = quat * resultQuat;

            return new Vector3d(resultQuat.X, resultQuat.Y, resultQuat.Z);
        }

        public static Vector3d operator *(Vector3d v1, Vector3d v2)
        {
            Vector3d result;
            result.x = v1.x * v2.x;
            result.y = v1.y * v2.y;
            result.z = v1.z * v2.z;

            return result;
        }

        public static Vector3d operator *(Vector3d v1, double value)
        {
            return Scale(v1, value);
        }
        public static Vector3d operator *(double value, Vector3d v1)
        {
            return Scale(v1, value);
        }
        public static Vector3d operator /(Vector3d v1, double value)
        {
            if (value == 0) return Vector3d.Zero();

            Vector3d result;
            result.x = v1.x / value;
            result.y = v1.y / value;
            result.z = v1.z / value;

            return result;
        }

        // March.11.2024 - this was only ever used by Ray.css for finding the inverse direction vector.
        //                The problem was invesrse_direction = new Vector3d(1d / v.x, 1d/v.y, 1d/.vz) 
        //                is not the same as what we get when calling this overloaded operator because
        //                we are assigning 0 inappropriately.
        //public static Vector3d operator /(double value, Vector3d v1)
        //{

        //	Vector3d result;

        //    // July.10.2012 to avoid divide by zero use ternary ?: to assign 0 or 1 / v 
        //	result.x = (v1.x == 0d) ? 0d : value / v1.x;
        //	result.y = (v1.y == 0d) ? 0d : value / v1.y;
        //	result.z = (v1.z == 0d) ? 0d : value / v1.z;

        //	return result;
        //}

        public static bool operator ==(Vector3d v1, Vector3d v2)
        {
            return (v1.x == v2.x && v1.y == v2.y && v1.z == v2.z);
        }

        public static bool operator !=(Vector3d v1, Vector3d v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Vector3d))
                return false;

            return (this == (Vector3d)obj);
        }

        public bool Equals(Vector3d v)
        {
            return this.x == v.x && this.y == v.y && this.z == v.z;
        }

        public bool IsNullOrEmpty()
        {
            return (x == 0 && y == 0 && z == 0);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
            return string.Format("{0}{1}{2}{3}{4}", x, delimiter,
                                                       y, delimiter,
                                                       z);
        }

        public static string ToString(Vector3d[] vecArray)
         {

             if (vecArray == null || vecArray.Length == 0) return null;

             string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
             string result = string.Empty;
             System.Text.StringBuilder sb = new System.Text.StringBuilder(result);

             for (int i = 0; i < vecArray.Length; i++)
             {
                 sb.Append(vecArray[i].ToString());
                 if (i != vecArray.Length - 1)
                     // append delimiter. NOTE: same delimiter is used even between vectors and not just their elements
                     sb.Append(delimiter);
             }
             result = sb.ToString();
 
             return result;
         }
        
        // TODO: i think the thing to do is move this out from here and into the PropertyBags
        //#region ICustomTypeDescriptor Members
        //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        //{
        //    bool filtering = (attributes != null && attributes.Length > 0);
        //    PropertyDescriptorCollection props;

        //    // Create the property collection and filter
        //    props = new PropertyDescriptorCollection(null);
        //    foreach (PropertyDescriptor prop in
        //        TypeDescriptor.GetProperties(
        //        this, attributes, true))
        //    {
        //        props.Add(prop);
        //    }

        //    // add public fields to property description collection
        //    FieldInfo[] allFields = this.GetType().GetFields();
        //    foreach (FieldInfo field in this.GetType().GetFields())
        //    {
        //        // at this point we wind up adding a value type
        //        // and there's no way to call the value changed handler for the current instance
        //        // that's why we should be changing the entire Vector3d for the PropertySpec
        //        // and it's the PropertySpec's SetValue that should be firing
        //        FieldPropertyDescriptor fieldDesc =
        //            new FieldPropertyDescriptor(ref this, field);
        //        //if (!filtering ||
        //        //    fieldDesc.Attributes.Contains(attributes))

        //        fieldDesc.AddValueChanged(this, PropertyGridFieldChanged);
        //        props.Add(fieldDesc);
        //    }

        //    return props;
        //}

        //// i think this instance of the vector doesn't get called either... it's a value type
        //internal void PropertyGridFieldChanged(object sender, EventArgs e)
        //{
        //    this.x = 0;
        //    this.y = 0;
        //    this.z = 0;
        //}

        //AttributeCollection ICustomTypeDescriptor.GetAttributes()
        //{
        //    //throw new NotImplementedException();
        //    return null;
        //}

        //string ICustomTypeDescriptor.GetClassName()
        //{
        //    throw new NotImplementedException();
        //}

        //string ICustomTypeDescriptor.GetComponentName()
        //{
        //    throw new NotImplementedException();
        //}

        //TypeConverter ICustomTypeDescriptor.GetConverter()
        //{
        //    throw new NotImplementedException();
        //}

        //EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        //{
        //    throw new NotImplementedException();
        //}

        //PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        //{
        //    //throw new NotImplementedException();
        //    return null;
        //}

        //object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        //{
        //    throw new NotImplementedException();
        //}

        //EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        //{
        //    throw new NotImplementedException();
        //}

        //EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        //{
        //    throw new NotImplementedException();
        //}

        //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        //{
        //    throw new NotImplementedException();
        //    //return GetProperties(null);
        //}

        //object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        //{
        //    // TODO: here I should be returning the PropertySpec
        //    return this;
        //}

        //#endregion
    }

    //public class FieldPropertyDescriptor : PropertyDescriptor
    //{
    //    private Vector3d mTarget;
    //    FieldInfo fieldInfo;
    //    public override Type ComponentType { get { return fieldInfo.DeclaringType; } }
    //    public override bool IsReadOnly { get { return false; } }            
    //    public override Type PropertyType { get { return fieldInfo.FieldType; } }            

    //    //public FieldPropertyDescriptor(FieldInfo fieldInfo) : base(fieldInfo.Name, 
    //    //    (Attribute[])fieldInfo.GetCustomAttributes(true)) 
    //    //{ 
    //    //    this.fieldInfo = fieldInfo; 
    //    //}

    //    public FieldPropertyDescriptor(ref Vector3d target, FieldInfo fieldInfo)
    //        : base(fieldInfo.Name,
    //        (Attribute[])fieldInfo.GetCustomAttributes(typeof(Attribute), true))
    //    {
    //        this.fieldInfo = fieldInfo;
    //        mTarget = target;
    //    }


    //    public override bool CanResetValue(object component) { return false; }            
    //    public override object GetValue(object component) 
    //    {
    //        object value = fieldInfo.GetValue(component);
    //        return fieldInfo.GetValue(component);


    //        //Type type = value.GetType();
    //        //Type converterType = typeof(MyConverter<,>).MakeGenericType(type, typeof(FieldsToProperties));
    //        //ConversionDelegate dlg = delegate(object o)
    //        //{
    //        //    return new FieldsToProperties(o);
    //        //};

    //        //return converterType.InvokeMember(
    //        //"Convert",
    //        //BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
    //        //null,
    //        //Activator.CreateInstance(converterType),
    //        //new object[] { value, dlg });


    //    }            
    //    public override void ResetValue(object component) { }    


    //    public override void SetValue(object component, object value) 
    //    {

    //        Vector3d v = (Vector3d)component;


    //        if (this.Name == "x")
    //        {
    //            mTarget.x = (double)value;
    //        }
    //        else if (this.Name == "y")
    //        {
    //            mTarget.y = (double)value;
    //        }
    //        else if (this.Name == "z")
    //        {
    //            mTarget.z = (double)value;
    //        }

    //        fieldInfo.SetValue(mTarget, value);
    //        OnValueChanged(mTarget, EventArgs.Empty);
    //        OnValueChanged(this, EventArgs.Empty);
    //    }            
    //    public override bool ShouldSerializeValue(object component) { return true; }            
    //    public override int GetHashCode() { return fieldInfo.GetHashCode(); }            
    //    public override bool Equals(object obj) 
    //    { 
    //        if (obj == null)                  return false; 
    //        if (GetType() != obj.GetType()) 
    //            return false; return (obj as FieldPropertyDescriptor).fieldInfo.Equals(fieldInfo); 
    //    }


    //    delegate object ConversionDelegate(object a);
    //    class MyConverter<From, To>
    //    {
    //        public object Convert(object src, ConversionDelegate dlg)
    //        {
    //            if (src.GetType().IsArray)
    //            {
    //                From[] a = (From[])src;
    //                To[] b = new To[a.Length];
    //                for (int i = 0; i < a.Length; i++)
    //                {
    //                    b[i] = (To)dlg(a[i]);
    //                }
    //                return b;
    //            }
    //            else
    //            {
    //                return dlg((From)src);
    //            }
    //        }
    //    }

    //    //public override object GetValue(object component)
    //    //{
    //    //    object value = fieldInfo.GetValue(component);

    //    //    Type type = value.GetType();
    //    //    bool isArray = type.IsArray;
    //    //    if (isArray) type = type.GetElementType();


    //    //    //if (type == typeof(IntPtr))
    //    //    //{
    //    //    //    return (new MyConverter<IntPtr, MemoryAddress>()).Convert(value, delegate(object o)
    //    //    //    {
    //    //    //        IntPtr ip = (IntPtr)o;
    //    //    //        return new MemoryAddress(ip);
    //    //    //    });
    //    //    //}

    //    //    if (type.DeclaringType == typeof(Native))
    //    //    {
    //    //        //if (type == typeof(Native.UNICODE_STRING))
    //    //        //{
    //    //        //    return (new MyConverter<Native.UNICODE_STRING, string>()).Convert(value, delegate(object o)
    //    //        //    {
    //    //        //        Native.UNICODE_STRING us = (Native.UNICODE_STRING)o;
    //    //        //        return RemoteReader.ReadStringUni(MainForm.Doc.ProcessHandle, us.Buffer, us.Length);
    //    //        //    });
    //    //        //}

    //    //        //if (type == typeof(Native.HANDLE))
    //    //        //{
    //    //        //    return (new MyConverter<Native.HANDLE, string>()).Convert(value, delegate(object o)
    //    //        //    {
    //    //        //        Native.HANDLE h = (Native.HANDLE)o;
    //    //        //        return "0x" + h.Handle.ToString("X8");
    //    //        //    });
    //    //        //}

    //    //        if (!type.IsEnum)
    //    //        {
    //    //            Type converterType = typeof(MyConverter<,>).MakeGenericType(type, typeof(FieldsToProperties));
    //    //            ConversionDelegate dlg = delegate(object o)
    //    //            {
    //    //                return new FieldsToProperties(o);
    //    //            };

    //    //            return converterType.InvokeMember(
    //    //              "Convert",
    //    //              BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public,
    //    //              null,
    //    //              Activator.CreateInstance(converterType),
    //    //              new object[] { value, dlg });


    //    //            //return new FieldsToProperties(value);
    //    //        }
    //    //    }

    //    //    return value;
    //    //}


    //}


    //public class FieldsToProperties : ICustomTypeDescriptor
    //{
    //    #region Private fields

    //    private object _target;

    //    #endregion
    //    #region Construction

    //    public FieldsToProperties(object target)
    //    {
    //        if (target == null) throw new ArgumentNullException("target");
    //        _target = target;
    //    }

    //    #endregion
    //    #region Object overrides

    //    public override string ToString()
    //    {
    //        return string.Format("({0})", _target.GetType().Name);
    //    }

    //    #endregion
    //    #region ICustomTypeDescriptor Members

    //    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
    //    {
    //        // Properties belong to the target object
    //        return _target;
    //    }

    //    AttributeCollection ICustomTypeDescriptor.GetAttributes()
    //    {
    //        // Gets the attributes of the target object
    //        return TypeDescriptor.GetAttributes(this, true);
    //    }

    //    string ICustomTypeDescriptor.GetClassName()
    //    {
    //        // Gets the class name of the target object
    //        return TypeDescriptor.GetClassName(this, true);
    //    }

    //    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
    //    {
    //        return ((ICustomTypeDescriptor)this).GetProperties(null);
    //    }

    //    private class FilterCache
    //    {
    //        public Attribute[] Attributes;
    //        public PropertyDescriptorCollection FilteredProperties;

    //        public FilterCache(Attribute[] att, PropertyDescriptorCollection props)
    //        {
    //            Attributes = att;
    //            FilteredProperties = props;
    //        }

    //        public bool IsValid(Attribute[] other)
    //        {
    //            if (other == null || Attributes == null) return false;

    //            if (Attributes.Length != other.Length) return false;

    //            for (int i = 0; i < other.Length; i++)
    //            {
    //                if (!Attributes[i].Match(other[i])) return false;
    //            }

    //            return true;
    //        }
    //    }

    //    private PropertyDescriptorCollection _propCache;
    //    private FilterCache _filterCache;

    //    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    //    {
    //        bool filtering = (attributes != null && attributes.Length > 0);
    //        PropertyDescriptorCollection props = _propCache;
    //        FilterCache cache = _filterCache;

    //        // Use a cached version if possible
    //        if (filtering && cache != null && cache.IsValid(attributes))
    //            return cache.FilteredProperties;

    //        if (!filtering && props != null)
    //            return props;

    //        // Create the property collection and filter
    //        props = new PropertyDescriptorCollection(null);
    //        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(this, attributes, true))
    //        {
    //            props.Add(prop);
    //        }

    //        foreach (FieldInfo field in _target.GetType().GetFields())
    //        {
    //            FieldPropertyDescriptor fieldDesc = new FieldPropertyDescriptor(field., field);
    //            if (!filtering || fieldDesc.Attributes.Contains(attributes))
    //                props.Add(fieldDesc);
    //        }

    //        // Store the computed properties
    //        if (filtering)
    //        {
    //            cache = new FilterCache(attributes, props);
    //            _filterCache = cache;
    //        }
    //        else
    //        {
    //            _propCache = props;
    //        }

    //        return props;
    //    }

    //    string ICustomTypeDescriptor.GetComponentName()
    //    {
    //        return null;
    //    }

    //    TypeConverter ICustomTypeDescriptor.GetConverter()
    //    {
    //        return null;
    //    }

    //    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
    //    {
    //        return null;
    //    }

    //    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
    //    {
    //        return null;
    //    }

    //    object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
    //    {
    //        return null;
    //    }


    //    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
    //    {
    //        return null;
    //    }

    //    EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
    //    {
    //        return null;
    //    }

    //    #endregion
    //}
}