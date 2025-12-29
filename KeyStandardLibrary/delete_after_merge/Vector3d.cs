using System;
using Core.Converters;
using MTV3D65;

namespace Core.Types
{
    // [TypeConverter(typeof (Vector3Converter))]
    public struct Vector3d
    {
        public double x, y, z;

        public static Vector3d Parse(string s)
        {
            char[] delimiterChars = {' ', ','};
            string[] sToks = s.Split(delimiterChars);
            return new Vector3d(double.Parse(sToks[0]), double.Parse(sToks[1]), double.Parse(sToks[2]));
        }

        public Vector3d(TV_3DVECTOR v) : this(v.x, v.y, v.z)
        {
        }

        public Vector3d(double Vx, double Vy, double Vz)
        {
            x = Vx;
            y = Vy;
            z = Vz;
        }

        public static Vector3d Up()
        {
            Vector3d v;
            v.x = 0;
            v.y = 1;
            v.z = 0;
            return v;
        }

        public static Vector3d Right()
        {
            Vector3d v;
            v.x = 1;
            v.y = 0;
            v.z = 0;
            return v;
        }

        public static Vector3d Forward()
        {
            Vector3d v;
            v.x = 0;
            v.y = 0;
            v.z = -1;
            return v;
        }

        public double Length
        {
            get { return GetLength(this); }
        }
        public double LengthSquared()
        {
            return Vector3d.GetLengthSquared( this);
        }

        public void ZeroVector()
        {
            x = y = z = 0;
        }
        public double Normalize()
        {
            if (Length == 0) return 0; //  new Vector3d(0, 0, 0);
            double inverse = 1.0/ Length;
            x *= inverse;
            y *= inverse;
            z *= inverse;
            return Length;
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
        public static Vector3d TransformNormal(Vector3d v1, Matrix m)
        {
            Vector3d result;
            if (m == null)
            {
                result.x = v1.x;
                result.y = v1.y;
                result.z = v1.z;
                return result;
            }
            result.x = (v1.x * m.M11) + (v1.y * m.M21) + (v1.z * m.M31);
            result.y = (v1.x * m.M12) + (v1.y * m.M22) + (v1.z * m.M32);
            result.z = (v1.x*m.M13) + (v1.y*m.M23) + (v1.z*m.M33);
            return result;
        }
        /// <summary>
        /// 3x3 matrix transform.  This is not intended to be used with a 4x4 matrix such as a projection matrix
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Vector3d TransformCoord(Vector3d v1, Matrix m)
        {
            if (m == null || (v1.IsNullOrEmpty()))
                return v1;

            Vector3d result;
            result.x = (v1.x*m.M11) + (v1.y*m.M21) + (v1.z*m.M31) + m.M41;
            result.y = (v1.x*m.M12) + (v1.y*m.M22) + (v1.z*m.M32) + m.M42;
            result.z = (v1.x*m.M13) + (v1.y*m.M23) + (v1.z*m.M33) + m.M43;
            return result;
        }

        public static Vector3d TransformCoord(Vector3d v1, Quaternion q)
        {
            
            if ((q == null) || (q.IsNullOrEmpty()) || (v1.IsNullOrEmpty()))
                return v1;
            
            return TransformCoord(v1, Quaternion.ToMatrix(q));
        }

        public static double GetDistance3d(Vector3d v1, Vector3d v2)
        {
            return Math.Sqrt(GetDistance3dSquared( v1, v2));
        }

        public static double GetLengthSquared (double x, double y, double z)
        {
            return (x*x) + (y*y) + (z*z);
        }
        public static double GetLengthSquared(Vector3d v)
        {
            return GetLengthSquared(v.x, v.y, v.z);
        }
        public static double GetLength(double x, double y, double z)
        {
            return Math.Sqrt(GetLengthSquared(x,y,z));
        }
        public static double GetLength(Vector3d v)
        {
            return Math.Sqrt(GetLengthSquared(v));
        }
        public static double GetDistance3dSquared(Vector3d v1, Vector3d v2)
        {
            double dx = (v1.x - v2.x);
            double dy = (v1.y - v2.y);
            double dz = (v1.z - v2.z);
            return GetLengthSquared(dx, dy, dz);
        }

        public static double AngleBetweenVectors(Vector3d v1, Vector3d v2)
        {
            double dot = DotProduct(v1, v2);
            double vectorsMagnitude = v1.Length*v2.Length;
            double angle = Math.Acos(dot/vectorsMagnitude);

            if (double.IsNaN(angle))
                return 0;
            else
                return angle;
        }

        /// <summary>
        /// In XNA they call this function "Negate" since the original + negated (or inverse) results in = 0,0,0
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3d Inverse(Vector3d v)
        {
            Vector3d result;
            result.x = -v.x;
            result.y = -v.y;
            result.z = -v.z;
            return result;
            //return new Vector3d(-v.x, -v.y, -v.z);
        }

        // dot productive is commutative (i.e.  v1 dot v2 == v2 dot v1)
        public static double DotProduct(Vector3d v1, Vector3d v2)
        {
            return (v1.x*v2.x + v1.y*v2.y + v1.z*v2.z);
        }

        // cross product is NOT commutative (ie.. v1 cross v2 != v2 cross v1)
        public static Vector3d CrossProduct(Vector3d v1, Vector3d v2)
        {
            Vector3d vResult;
            vResult.x = v1.y * v2.z - v1.z * v2.y;
            vResult.y = v1.z * v2.x - v1.x * v2.z;
            vResult.z = v1.x * v2.y - v1.y * v2.x;
            return vResult;
            //return new Vector3d(v1.y*v2.z - v1.z*v2.y, v1.z*v2.x - v1.x*v2.z, v1.x*v2.y - v1.y*v2.x);
        }

        public static Vector3d Subtract(Vector3d v1, Vector3d v2)
        {
            //return new Vector3d(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
            Vector3d result;
            result.x = v1.x - v2.x;
            result.y = v1.y - v2.y;
            result.z = v1.z - v2.z;
            return result;
        }

        public static Vector3d Add(Vector3d v1, Vector3d v2)
        {
            //   return new Vector3d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
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
            //return new Vector3d(v1.x*scale, v1.y*scale, v1.z*scale);
        }

        // clamp the vector's magnitude (length) to the limit length
        public static Vector3d Limit(Vector3d vec, double limit)
        {
            if (vec.Length > limit)
                return Normalize(vec)*limit;

            return vec;
        }

        // pass in a Random number object so that it's already seeded. If we create it below each time, it will generate the same vector
        public static Vector3d RandomVector(Random rand)
        {
            Vector3d result = new Vector3d(rand.NextDouble()*2.0d - 1.0d, rand.NextDouble()*2.0 - 1.0d, rand.NextDouble()*2.0 - 1.0d);
            return Normalize(result);
        }

        public static Vector3d Lerp(Vector3d start, Vector3d end, double weight)
        {
            return (start*(1.0d - weight)) + (end*weight);
        }

        public static bool operator ==(Vector3d v1, Vector3d v2)
        {
            return (v1.x == v2.x && v1.y == v2.y && v1.z == v2.z);
        }

        public static bool operator !=(Vector3d v1, Vector3d v2)
        {
            return !(v1 == v2);
        }

        public static Vector3d operator -(Vector3d v1)
        {
            return Inverse(v1);
        }

        public static Vector3d operator -(Vector3d v1, Vector3d v2)
        {
            return Subtract(v1, v2);
        }

        public static Vector3d operator +(Vector3d v1, Vector3d v2)
        {
            return Add(v1, v2);
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
            return Scale(v1, 1.0d/value);
        }

        public static Vector3d operator /(double value, Vector3d v1)
        {
            return Scale(v1, 1.0d / value);
        }

        public static Vector3d FromTV3DVector(Vector3d v)
        {
            return new Vector3d(v.x, v.y, v.z);
        }

        public TV_3DVECTOR ToTV3DVector()
        {
            return new TV_3DVECTOR((float) x, (float) y, (float) z);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector3d))
                return base.Equals(obj);
            else
                return (this == (Vector3d) obj);
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
            return string.Format("{0},{1},{2}", x, y, z);
        }

        //    ' todo: now we're gonna need a whole bunch of component convertors for custom accessing of our nodes.  We'll start with basic types
        //    '       such as color, Vector3d, 
        //    Friend Class Vector3Converter : Inherits ExpandableObjectConverter
        //        '''
        //        Public Overloads Overrides Function CanConvertFrom(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal sourceType As System.Type) As Boolean
        //            If (sourceType Is GetType(String)) Then
        //                Return True
        //            End If
        //            Return MyBase.CanConvertFrom(context, sourceType)
        //        End Function

        //        Public Overloads Overrides Function ConvertFrom(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object) As Object
        //            If TypeOf value Is String Then
        //                Try
        //                    Dim s As String = CType(value, String)
        //                    Dim parts() As String
        //                    parts = Split(s, ",")
        //                    If Not IsNothing(parts) Then
        //                        Dim vec As X3DSceneGraph.X3D.Vector3d = New X3DSceneGraph.X3D.Vector3d()
        //                        If Not IsNothing(parts(0)) Then vec.x = CSng(parts(0))
        //                        If Not IsNothing(parts(1)) Then vec.y = CSng(parts(1))
        //                        If Not IsNothing(parts(2)) Then vec.z = CSng(parts(2))
        //                        Return vec  ' todo: i dont think we want to return _Armor do we?  Note with this commented out, our dr & pd changes update immediately.
        //                    End If
        //                Catch ex As Exception
        //                    Throw New ArgumentException("Can not convert '" & value.ToString & "' to type SFVec3f.")
        //                End Try
        //            End If
        //            Return MyBase.ConvertFrom(context, culture, value)
        //        End Function

        //        Public Overloads Overrides Function ConvertTo(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal culture As System.Globalization.CultureInfo, ByVal value As Object, ByVal destinationType As System.Type) As Object
        //            If (destinationType Is GetType(System.String) AndAlso TypeOf value Is X3DSceneGraph.X3D.SFVec3f) Then
        //                Dim vec As X3DSceneGraph.X3D.SFVec3f = CType(value, X3DSceneGraph.X3D.SFVec3f)
        //                ' build the string as "x, y, z"
        //                Return "" & vec.x & ", " & vec.y & ", " & vec.z
        //            End If
        //            Return MyBase.ConvertTo(context, culture, value, destinationType)
        //        End Function

        //        Public Overloads Overrides Function CanConvertTo(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal destinationType As System.Type) As Boolean
        //            If (destinationType Is GetType(X3DSceneGraph.X3D.SFVec3f)) Then
        //                Return True
        //            End If
        //            Return MyBase.CanConvertFrom(context, destinationType)
        //        End Function
        //    End Class


        //    Public Structure MFVec2f
        //        Private _items() As SFVec2f

        //        Sub New(ByRef s As String)
        //            Dim sToks As Generic.List(Of String)
        //            sToks = Tokenizer.Tokenize(s)
        //            ReDim _items(sToks.Count \ 2 - 1)
        //            Dim j As Int32 = 0
        //            For k As Int32 = 0 To sToks.Count - 1 Step 2
        //                _items(j).x = CSng(sToks(k))
        //                _items(j).y = CSng(sToks(k + 1))
        //                j += 1
        //            Next
        //        End Sub

        //        Sub New(ByVal vecs() As SFVec2f)
        //            _items = vecs
        //        End Sub
        //        Default Public Property item(ByVal i As Integer) As SFVec2f
        //            Get
        //                Return _items(i)
        //            End Get
        //            Set(ByVal value As SFVec2f)
        //                _items(i) = value
        //            End Set
        //        End Property

        //        Public Function ToSFVec2fArray() As SFVec2f()
        //            Return _items
        //        End Function
        //        Public Function length() As Integer
        //            Return DirectCast(IIf(_items Is Nothing, 0%, _items.Length), Int32)
        //        End Function
        //    End Structure

        
        //    public struct MVector3d
        //        Private _items() As Vector3d

        //        Sub New(ByRef s As String)
        //            Dim sToks As Generic.List(Of String)
        //            sToks = Tokenizer.Tokenize(s)
        //            ReDim _items(sToks.Count \ 3 - 1)
        //            Dim j As Int32 = 0
        //            For k As Int32 = 0 To sToks.Count - 1 Step 3
        //                _items(j).x = CSng(sToks(k))
        //                _items(j).y = CSng(sToks(k + 1))
        //                _items(j).z = CSng(sToks(k + 2))
        //                j += 1
        //            Next
        //        End Sub

        //        Sub New(ByVal vecs() As SFVec3f)
        //            _items = vecs
        //        End Sub

        //        Default Public Property item(ByVal i As Integer) As SFVec3f
        //            Get
        //                Return _items(i)
        //            End Get
        //            Set(ByVal value As SFVec3f)
        //                _items(i) = value
        //            End Set
        //        End Property

        //        Public Function ToSFVec3fArray() As SFVec3f()
        //            Return _items
        //        End Function
        //        Public Function length() As Integer
        //            Return DirectCast(IIf(_items Is Nothing, 0%, _items.Length), Int32)
        //        End Function
        //    }
    }


//    'NOTE: Im so stupid... but I finally get it.  You cannot deserialize a class if its an attribute
//    '      only if its an element.  It is why we can deserialize Coordinate class but not MFVec3d.  
//    Public Structure MFInt32
//        Private _items() As Integer

//        Sub New(ByRef s As String)
//            Dim sToks As Generic.List(Of String)
//            sToks = Tokenizer.Tokenize(s)
//            ReDim _items(sToks.Count - 1)
//            For i As Integer = 0 To sToks.Count - 1
//                _items(i) = CInt(sToks(i))
//            Next
//        End Sub

//        Default Public Property item(ByVal i As Integer) As Integer
//            Get
//                Return _items(i)
//            End Get
//            Set(ByVal value As Integer)
//                _items(i) = value
//            End Set
//        End Property

//        Public Function ToIntegerArray() As Integer()
//            Return _items
//        End Function
//        Public Function length() As Integer
//            If _items Is Nothing Then
//                Return 0
//            Else
//                Return _items.Length
//            End If
//        End Function
//    End Structure

//    Public Structure MFFloat
//        Private _items() As Single

//        Sub New(ByRef s As String)
//            Dim sToks As Generic.List(Of String)
//            sToks = Tokenizer.Tokenize(s)
//            ReDim _items(sToks.Count - 1)
//            For i As Integer = 0 To sToks.Count - 1
//                _items(i) = CSng(sToks(i))
//            Next
//        End Sub

//        Default Public Property item(ByVal i As Integer) As Single
//            Get
//                Return _items(i)
//            End Get
//            Set(ByVal value As Single)
//                _items(i) = value
//            End Set
//        End Property

//        Public Function ToSingleArray() As Single()
//            Return _items
//        End Function
//        Public Function length() As Integer
//            If _items Is Nothing Then
//                Return 0
//            Else
//                Return _items.Length
//            End If
//        End Function
//    End Structure
}