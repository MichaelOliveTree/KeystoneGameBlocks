using System;
using MTV3D65;

namespace Core.Types
{
    public class Matrix
    {
        private double[,] _mat;
        private double _determinant = 0d;

        public double Determinant { get { return _determinant; } }
        public double M11
        {
            get { return _mat[0, 0]; }
            set { _mat[0, 0] = value; }
        }

        public double M12
        {
            get { return _mat[0, 1]; }
            set { _mat[0, 1] = value; }
        }

        public double M13
        {
            get { return _mat[0, 2]; }
            set { _mat[0, 2] = value; }
        }

        public double M14
        {
            get { return _mat[0, 3]; }
            set { _mat[0, 3] = value; }
        }

        public double M21
        {
            get { return _mat[1, 0]; }
            set { _mat[1, 0] = value; }
        }

        public double M22
        {
            get { return _mat[1, 1]; }
            set { _mat[1, 1] = value; }
        }

        public double M23
        {
            get { return _mat[1, 2]; }
            set { _mat[1, 2] = value; }
        }

        public double M24
        {
            get { return _mat[1, 3]; }
            set { _mat[1, 3] = value; }
        }

        public double M31
        {
            get { return _mat[2, 0]; }
            set { _mat[2, 0] = value; }
        }

        public double M32
        {
            get { return _mat[2, 1]; }
            set { _mat[2, 1] = value; }
        }

        public double M33
        {
            get { return _mat[2, 2]; }
            set { _mat[2, 2] = value; }
        }

        public double M34
        {
            get { return _mat[2, 3]; }
            set { _mat[2, 3] = value; }
        }

        public double M41
        {
            get { return _mat[3, 0]; }
            set { _mat[3, 0] = value; }
        }

        public double M42
        {
            get { return _mat[3, 1]; }
            set { _mat[3, 1] = value; }
        }

        public double M43
        {
            get { return _mat[3, 2]; }
            set { _mat[3, 2] = value; }
        }

        public double M44
        {
            get { return _mat[3, 3]; }
            set { _mat[3, 3] = value; }
        }

        //* The zero-based row-column position:
        //      o _m00, _m01, _m02, _m03
        //      o _m10, _m11, _m12, _m13
        //      o _m20, _m21, _m22, _m23
        //      o _m30, _m31, _m32, _m33
        //* The one-based row-column position:
        //      o _11, _12, _13, _14
        //      o _21, _22, _23, _24
        //      o _31, _32, _33, _34
        //      o _41, _42, _43, _44

        //A matrix can also be accessed using array access notation, which is a zero-based set of indices. 
        //Each index is inside of square brackets. A 4x4 matrix is accessed with the following indices:
        //* [0][0], [0][1], [0][2], [0][3]
        //* [1][0], [1][1], [1][2], [1][3]
        //* [2][0], [2][1], [2][2], [2][3]
        //* [3][0], [3][1], [3][2], [3][3]
        public Matrix()
        {
            _mat = new double[4,4];
        }

        public Matrix (Quaternion quat) : this()
        {
            Matrix matrix = new Matrix();
            
            double xx = quat.X * quat.X;
            double yy = quat.Y * quat.Y;
            double zz = quat.Z * quat.Z;
            double xy = quat.X * quat.Y;
            double xz = quat.X * quat.Z;
            double yz = quat.Y * quat.Z;
            double wx = quat.W * quat.X;
            double wy = quat.W * quat.Y;
            double wz = quat.W * quat.Z;

            _mat[0, 0] = 1 - 2 * (yy + zz);
            _mat[1, 0] = 2 * (xy - wz);
            _mat[2, 0] = 2 * (xz + wy);

            _mat[0, 1] = 2 * (xy + wz);
            _mat[1, 1] = 1 - 2 * (xx + zz);
            _mat[2, 1] = 2 * (yz - wx);

            _mat[0, 2] = 2 * (xz - wy);
            _mat[1, 2] = 2 * (yz + wx);
            _mat[2, 2] = 1 - 2 * (xx + yy);

            _mat[3, 0] = matrix.M42 = matrix.M43 = 0.0d;
            _mat[0, 3] = matrix.M24 = matrix.M34 = 0.0d;
            _mat[3, 3] = 1.0d;
        }

        public Matrix (double  m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24, 
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44): this()
        {
            _mat[0, 0] = m11;
            _mat[0, 1] = m12;
            _mat[0, 2] = m13;
            _mat[0, 3] = m14;
            _mat[1, 0] = m21;
            _mat[1, 1] = m22;
            _mat[1, 2] = m23;
            _mat[1, 3] = m24;
            _mat[2, 0] = m31;
            _mat[2, 1] = m32;
            _mat[2, 2] = m33;
            _mat[2, 3] = m34;
            _mat[3, 0] = m41;
            _mat[3, 1] = m42;
            _mat[3, 2] = m43;
            _mat[3, 3] = m44;
        }
        public Matrix  (Matrix m): this()
        {
            _mat[0, 0] = m.M11;
            _mat[0, 1] = m.M12;
            _mat[0, 2] = m.M13;
            _mat[0, 3] = m.M14;
            _mat[1, 0] = m.M21;
            _mat[1, 1] = m.M22;
            _mat[1, 2] = m.M23;
            _mat[1, 3] = m.M24;
            _mat[2, 0] = m.M31;
            _mat[2, 1] = m.M32;
            _mat[2, 2] = m.M33;
            _mat[2, 3] = m.M34;
            _mat[3, 0] = m.M41;
            _mat[3, 1] = m.M42;
            _mat[3, 2] = m.M43;
            _mat[3, 3] = m.M44;
        }
        public Matrix(TV_3DMATRIX m) : this()
        {
            _mat[0, 0] = m.m11;
            _mat[0, 1] = m.m12;
            _mat[0, 2] = m.m13;
            _mat[0, 3] = m.m14;
            _mat[1, 0] = m.m21;
            _mat[1, 1] = m.m22;
            _mat[1, 2] = m.m23;
            _mat[1, 3] = m.m24;
            _mat[2, 0] = m.m31;
            _mat[2, 1] = m.m32;
            _mat[2, 2] = m.m33;
            _mat[2, 3] = m.m34;
            _mat[3, 0] = m.m41;
            _mat[3, 1] = m.m42;
            _mat[3, 2] = m.m43;
            _mat[3, 3] = m.m44;
        }

        public Matrix(Microsoft.DirectX.Matrix m) : this()
        {
            _mat[0, 0] = m.M11;
            _mat[0, 1] = m.M12;
            _mat[0, 2] = m.M13;
            _mat[0, 3] = m.M14;
            _mat[1, 0] = m.M21;
            _mat[1, 1] = m.M22;
            _mat[1, 2] = m.M23;
            _mat[1, 3] = m.M24;
            _mat[2, 0] = m.M31;
            _mat[2, 1] = m.M32;
            _mat[2, 2] = m.M33;
            _mat[2, 3] = m.M34;
            _mat[3, 0] = m.M41;
            _mat[3, 1] = m.M42;
            _mat[3, 2] = m.M43;
            _mat[3, 3] = m.M44;
        }

        public void SetTranslation(Vector3d translation)
        {
            _mat[0, 0] = translation.x;
            _mat[0, 1] = translation.y;
            _mat[0, 2] = translation.z;
        }

        public static Matrix Identity()
        {
            Matrix m = new Matrix();
            m._mat[0, 0] = 1.0f;
            m._mat[0, 1] = 0.0f;
            m._mat[0, 2] = 0.0f;
            m._mat[0, 3] = 0.0f;

            m._mat[1, 0] = 0.0f;
            m._mat[1, 1] = 1.0f;
            m._mat[1, 2] = 0.0f;
            m._mat[1, 3] = 0.0f;

            m._mat[2, 0] = 0.0f;
            m._mat[2, 1] = 0.0f;
            m._mat[2, 2] = 1.0f;
            m._mat[2, 3] = 0.0f;

            m._mat[3, 0] = 0.0f;
            m._mat[3, 1] = 0.0f;
            m._mat[3, 2] = 0.0f;
            m._mat[3, 3] = 1.0f;

            return m;
        }

        //http://www.euclideanspace.com/maths/algebra/matrix/orthogonal/index.htm
        public Vector3d Right
        {
            get { return new Vector3d(_mat[0, 0], _mat[1, 0], _mat[2, 0]); }
        }
        public Vector3d Up
        {
            get { return new Vector3d(_mat[0, 1], _mat[1, 1], _mat[2, 1]); }
        }
        public Vector3d Backward
        {
            get { return new Vector3d(_mat[0, 2], _mat[1, 2], _mat[2, 2]); }
        }
        // Rotation Arc
        // Reference, from Stan Melax in Game Gems I
        //  Quaternion q;
        //  vector3 c = CrossProduct(v0,v1);
        //  float   d = DotProduct(v0,v1);
        //  float   s = (float)sqrt((1+d)*2);
        //  q.x = c.x / s;
        //  q.y = c.y / s;
        //  q.z = c.z / s;
        //  q.w = s /2.0f;
        //  return q;
        public static TV_3DQUATERNION RotationArc(Vector3d v0, Vector3d v1)
        {
            Vector3d cross = Vector3d.CrossProduct(v0, v1);
            float d = (float) Vector3d.DotProduct(v0, v1);
            float s = (float) Math.Sqrt((1 + d)*2);
            float recip = 1.0f/s;

            Vector3d res = cross*recip;
            return new TV_3DQUATERNION((float) res.x, (float) res.y, (float) res.z, s*0.5f);
        }

        //todo: I need to get rid of Rotation as vector and use just Quat
        public static Matrix Rotation(Vector3d rotationAxis, float angleRadians)
        {
            Microsoft.DirectX.Vector3 v1;
            v1.X = (float) rotationAxis.x;
            v1.Y = (float) rotationAxis.y;
            v1.Z = (float) rotationAxis.z;

            Microsoft.DirectX.Matrix m1 = Microsoft.DirectX.Matrix.RotationAxis(v1, angleRadians);

            return new Matrix(m1);
        }

        // a simple inverse to work with View matrix
        // http://www.gamedev.net/community/forums/topic.asp?topic_id=288155
        public static Matrix Inverse(Matrix m)
        {
            // the below at least works for everything... 
            MTV3D65.TV_3DMATRIX tvmat = Matrix.ToTV3DMatrix(m);
            MTV3D65.TV_3DMATRIX transposed = new MTV3D65.TV_3DMATRIX();
            float det = 0;
            
            Core._Core.Maths.TVMatrixInverse(ref transposed, ref det, tvmat);
            return new Matrix(transposed); 

            
            Matrix R = new Matrix(m);
            R.M41 = 0;
            R.M42 = 0;
            R.M43 = 0;
            R.M44 = 1;

            Matrix T = Matrix.Identity();
            T.M41 = m.M41;
            T.M42 = m.M42;
            T.M43 = m.M43;
            T.M44 = m.M44;

            //System.Diagnostics.Trace.Assert(m.Equals(Matrix.Multiply( R, T)));

            Matrix TInv = new Matrix(T);
            // invert the translation of T
            TInv.M41 = -T.M41;
            TInv.M42 = -T.M42;
            TInv.M43 = -T.M43;
            TInv.M44 = -T.M44; // todo: have tried with this line commented out and not and no real difference.

            // inverse of rotation only is it's transpose
            Matrix RInv = Matrix.Transpose(R);

            return Matrix.Multiply(TInv, RInv);
        }

        // swap rows with columns
        public static Matrix Transpose(Matrix m)
        {
            Matrix transposed = new Matrix();
            transposed._mat[0, 0] = m._mat[0, 0];
            transposed._mat[0, 1] = m._mat[1, 0];
            transposed._mat[0, 2] = m._mat[2, 0];
            transposed._mat[0, 3] = m._mat[3, 0];

            transposed._mat[1, 0] = m._mat[0, 1];
            transposed._mat[1, 1] = m._mat[1, 1];
            transposed._mat[1, 2] = m._mat[2, 1];
            transposed._mat[1, 3] = m._mat[3, 1];

            transposed._mat[2, 0] = m._mat[0, 2];
            transposed._mat[2, 1] = m._mat[1, 2];
            transposed._mat[2, 2] = m._mat[2, 2];
            transposed._mat[2, 3] = m._mat[3, 2];

            transposed._mat[3, 0] = m._mat[0, 3];
            transposed._mat[3, 1] = m._mat[1, 3];
            transposed._mat[3, 2] = m._mat[2, 3];
            transposed._mat[3, 3] = m._mat[3, 3];
            return transposed;
        }

        public static Matrix Translation(Vector3d v)
        {
            Matrix result = Identity();
            result._mat[3, 0] = (double) v.x;
            result._mat[3, 1] = (double) v.y;
            result._mat[3, 2] = (double) v.z;
            return result;
        }

        public static Matrix Scaling(Vector3d v)
        {
            Matrix result = new Matrix(); // Identity();
            result._mat[0, 0] = (double) v.x;
            result._mat[1, 1] = (double) v.y;
            result._mat[2, 2] = (double) v.z;
            return result;
        }

        // These transforms are for left handed coordinate systems and should be moved to Matrix class as static methods
        public static Matrix RotationX(double angleRadians)
        {
            // Assuming the angle is in radians. 
            double cos_x = Math.Cos(angleRadians);
            double sin_x = Math.Sin(angleRadians);
            Matrix tmp = Types.Matrix.Identity();
            tmp.M11 = 1.0D;
            tmp.M12 = 0.0D;
            tmp.M13 = 0.0D;
            tmp.M14 = 0;
            tmp.M21 = 0.0D;
            tmp.M22 = cos_x;
            tmp.M23 = sin_x;
            tmp.M24 = 0;
            tmp.M31 = 0.0D;
            tmp.M32 = -sin_x;
            tmp.M33 = cos_x;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1D;
            return tmp;
        }

        public static Matrix RotationY(double angleRadians)
        {
            // Assuming the angle is in radians.
            double c = Math.Cos(angleRadians);
            double s = Math.Sin(angleRadians);
            Matrix tmp = Types.Matrix.Identity();
            tmp.M11 = c;
            tmp.M12 = 0.0D;
            tmp.M13 = -s;
            tmp.M14 = 0;
            tmp.M21 = 0.0D;
            tmp.M22 = 1;
            tmp.M23 = 0.0D;
            tmp.M24 = 0;
            tmp.M31 = s;
            tmp.M32 = 0.0D;
            tmp.M33 = c;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1D;
            return tmp;
        }

        public static Matrix RotationZ(double angleRadians)
        {
            // Assuming the angle is in radians. 
            double c = Math.Cos(angleRadians);
            double s = Math.Sin(angleRadians);
            Matrix tmp = Types.Matrix.Identity();
            tmp.M11 = c;
            tmp.M12 = s;
            tmp.M13 = 0.0F;
            tmp.M14 = 0;
            tmp.M21 = -s;
            tmp.M22 = c;
            tmp.M23 = 0.0F;
            tmp.M24 = 0;
            tmp.M31 = 0.0F;
            tmp.M32 = 0.0F;
            tmp.M33 = 1.0F;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1F;
            return tmp;
        }

        public static Matrix Add(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m1.M11 + m2.M11;
            result._mat[0, 1] = m1.M12 + m2.M12;
            result._mat[0, 2] = m1.M13 + m2.M13;
            result._mat[0, 3] = m1.M14 + m2.M14;
            result._mat[1, 0] = m1.M21 + m2.M21;
            result._mat[1, 1] = m1.M22 + m2.M22;
            result._mat[1, 2] = m1.M23 + m2.M23;
            result._mat[1, 3] = m1.M24 + m2.M24;
            result._mat[2, 0] = m1.M31 + m2.M31;
            result._mat[2, 1] = m1.M32 + m2.M32;
            result._mat[2, 2] = m1.M33 + m2.M33;
            result._mat[2, 3] = m1.M34 + m2.M34;
            result._mat[3, 0] = m1.M41 + m2.M41;
            result._mat[3, 1] = m1.M42 + m2.M42;
            result._mat[3, 2] = m1.M43 + m2.M43;
            result._mat[3, 3] = m1.M44 + m2.M44;
            return result;
        }

        public static Matrix Subtract (Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m1.M11 - m2.M11;
            result._mat[0, 1] = m1.M12 - m2.M12;
            result._mat[0, 2] = m1.M13 - m2.M13;
            result._mat[0, 3] = m1.M14 - m2.M14;
            result._mat[1, 0] = m1.M21 - m2.M21;
            result._mat[1, 1] = m1.M22 - m2.M22;
            result._mat[1, 2] = m1.M23 - m2.M23;
            result._mat[1, 3] = m1.M24 - m2.M24;
            result._mat[2, 0] = m1.M31 - m2.M31;
            result._mat[2, 1] = m1.M32 - m2.M32;
            result._mat[2, 2] = m1.M33 - m2.M33;
            result._mat[2, 3] = m1.M34 - m2.M34;
            result._mat[3, 0] = m1.M41 - m2.M41;
            result._mat[3, 1] = m1.M42 - m2.M42;
            result._mat[3, 2] = m1.M43 - m2.M43;
            result._mat[3, 3] = m1.M44 - m2.M44;
            return result;
        }

        /// <summary>
        /// Short cut multiplication for matrices that have 0,0,0,1 in final column.  Cannot be used with perspective matrix for instance
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = (m1.M11*m2.M11) + (m1.M12*m2.M21) + (m1.M13*m2.M31);
            result._mat[0, 1] = (m1.M11*m2.M12) + (m1.M12*m2.M22) + (m1.M13*m2.M32);
            result._mat[0, 2] = (m1.M11*m2.M13) + (m1.M12*m2.M23) + (m1.M13*m2.M33);
            result._mat[0, 3] = 0.0f;

            result._mat[1, 0] = (m1.M21*m2.M11) + (m1.M22*m2.M21) + (m1.M23*m2.M31);
            result._mat[1, 1] = (m1.M21*m2.M12) + (m1.M22*m2.M22) + (m1.M23*m2.M32);
            result._mat[1, 2] = (m1.M21*m2.M13) + (m1.M22*m2.M23) + (m1.M23*m2.M33);
            result._mat[1, 3] = 0.0f;

            result._mat[2, 0] = (m1.M31*m2.M11) + (m1.M32*m2.M21) + (m1.M33*m2.M31);
            result._mat[2, 1] = (m1.M31*m2.M12) + (m1.M32*m2.M22) + (m1.M33*m2.M32);
            result._mat[2, 2] = (m1.M31*m2.M13) + (m1.M32*m2.M23) + (m1.M33*m2.M33);
            result._mat[2, 3] = 0.0f;

            result._mat[3, 0] = (m1.M41*m2.M11) + (m1.M42*m2.M21) + (m1.M43*m2.M31) + m2.M41;
            result._mat[3, 1] = (m1.M41*m2.M12) + (m1.M42*m2.M22) + (m1.M43*m2.M32) + m2.M42;
            result._mat[3, 2] = (m1.M41*m2.M13) + (m1.M42*m2.M23) + (m1.M43*m2.M33) + m2.M43;
            result._mat[3, 3] = 1.0f;
            return result;
        }

        /// <summary>
        /// Scalar multiplication is easy. You just take a regular number (called a "scalar") and multiply it on every entry in the matrix.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Multiply (Matrix m, double scalar)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m.M11*scalar; 
            result._mat[0, 1] = m.M12 * scalar;
            result._mat[0, 2] = m.M13 * scalar;
            result._mat[0, 3] = m.M14 * scalar;

            result._mat[1, 0] = m.M21 * scalar;
            result._mat[1, 1] = m.M22 * scalar;
            result._mat[1, 2] = m.M23 * scalar;
            result._mat[1, 3] = m.M24 * scalar;

            result._mat[2, 0] = m.M31 * scalar;
            result._mat[2, 1] = m.M32 * scalar;
            result._mat[2, 2] = m.M33 * scalar;
            result._mat[2, 3] = m.M34 * scalar;

            result._mat[3, 0] = m.M41 * scalar;
            result._mat[3, 1] = m.M42 * scalar;
            result._mat[3, 2] = m.M43*scalar;
            result._mat[3, 3] = m.M44 * scalar;
            return result;
        }

        /// <summary>
        /// Short cut multiplication for matrices that have 0,0,0,1 in final column.  Cannot be used with perspective matrix for instance
        /// </summary>
        /// <param name="m1"></param>
        /// <param name="m2"></param>
        /// <returns></returns>
        public static Matrix MultiplyFull(Matrix m1, Matrix m2)
        {
            Matrix result = new Matrix();
            for (int i = 0; i < 4; i ++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double value = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        value += m1._mat[i, k]*m2._mat[k, j];
                    }
                    result._mat[i, j] = value;
                }
            }
            return result;
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            return Matrix.Add(m1, m2);
        }
        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            return Matrix.Subtract(m1, m2);
        }

        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            return Matrix.Multiply(m1,m2);
        }

        public static Matrix operator *(Matrix m, double scalar)
        {
            return Matrix.Multiply(m, scalar);
        }

        public static TV_3DMATRIX ToTV3DMatrix(Matrix mat)
        {
            TV_3DMATRIX m;
            m.m11 = (float) mat.M11;
            m.m12 = (float) mat.M12;
            m.m13 = (float) mat.M13;
            m.m14 = (float) mat.M14;
            m.m21 = (float) mat.M21;
            m.m22 = (float) mat.M22;
            m.m23 = (float) mat.M23;
            m.m24 = (float) mat.M24;
            m.m31 = (float) mat.M31;
            m.m32 = (float) mat.M32;
            m.m33 = (float) mat.M33;
            m.m34 = (float) mat.M34;
            m.m41 = (float) mat.M41;
            m.m42 = (float) mat.M42;
            m.m43 = (float) mat.M43;
            m.m44 = (float) mat.M44;
            return m;
        }


        public static TV_3DMATRIX D3DTOTVMatrix(Microsoft.DirectX.Matrix m1)
        {
            TV_3DMATRIX m2;
            m2.m11 = m1.M11;
            m2.m12 = m1.M12;
            m2.m13 = m1.M13;
            m2.m14 = m1.M14;
            m2.m21 = m1.M21;
            m2.m22 = m1.M22;
            m2.m23 = m1.M23;
            m2.m24 = m1.M24;
            m2.m31 = m1.M31;
            m2.m32 = m1.M32;
            m2.m33 = m1.M33;
            m2.m34 = m1.M34;
            m2.m41 = m1.M41;
            m2.m42 = m1.M42;
            m2.m43 = m1.M43;
            m2.m44 = m1.M44; //= 1;
            return m2;
        }

        public static Microsoft.DirectX.Matrix ToD3DMatrix(TV_3DMATRIX m1)
        {
            Microsoft.DirectX.Matrix m2;
            m2.M11 = m1.m11;
            m2.M12 = m1.m12;
            m2.M13 = m1.m13;
            m2.M14 = m1.m14;
            m2.M21 = m1.m21;
            m2.M22 = m1.m22;
            m2.M23 = m1.m23;
            m2.M24 = m1.m24;
            m2.M31 = m1.m31;
            m2.M32 = m1.m32;
            m2.M33 = m1.m33;
            m2.M34 = m1.m34;
            m2.M41 = m1.m41;
            m2.M42 = m1.m42;
            m2.M43 = m1.m43;
            m2.M44 = m1.m44; //= 1;
            return m2;
        }

//// src http://www.idevgames.com/forum/archive/index.php/t-10866.html
////    >>Original post by jyk
////    >>Here's another option to consider. You can get the same interpolation as you would with quaternion slerp (albeit at greater expense) by finding the matrix that rotates from A to B, extracting the axis and angle from this matrix, scaling the angle, and then recomposing the matrix from the axis and angle and multiplying with A.
////    >>I'm at work now, but if you're interested in this method I can post details later (or perhaps someone else will in the meantime).
        

////Linear interpolation (Vectors, scalars):
////delta = b - a;
////// alpha = [0..1]
////c = a + delta*alpha

////3x3 rotation matrix form (right-hand element evaluated first):

////delta = b * transpose(a) // transpose(a) followed by b.
////delta.getAxisAngle(axis,deltaAngle)
////// alpha = [0..1]
////c = axisAngleToMatrix(axis,deltaAngle*alpha) * a

//public void interpolate(Matrix a, Matrix b, float alpha, Matrix c) 
//{
//  Matrix delta = b ^ !a; // ^ = matrix product, ! = matrix transpose.
//  Vector3d axis;
//    float deltaAngle;
//  delta.AxisAngle(ref axis, ref deltaAngle);
//  Matrix rm = Matrix.Rotation(axis,deltaAngle*alpha);
//  c = rm ^ a;
//} // interpolate

//        public Matrix AxisAngle(Vector3d axis, float deltaAngle)
//        {
            
//        }


// void Matrix::setInverseTranslation( const float *translation )
//{
//    m_matrix[12] = -translation[0];
//    m_matrix[13] = -translation[1];
//    m_matrix[14] = -translation[2];
//}

//void Matrix::setRotationDegrees( const float *angles )
//{
//    float vec[3];
//    vec[0] = ( float )( angles[0]*180.0/PI );
//    vec[1] = ( float )( angles[1]*180.0/PI );
//    vec[2] = ( float )( angles[2]*180.0/PI );
//    setRotationRadians( vec );
//}

//void Matrix::setInverseRotationDegrees( const float *angles )
//{
//    float vec[3];
//    vec[0] = ( float )( angles[0]*180.0/PI );
//    vec[1] = ( float )( angles[1]*180.0/PI );
//    vec[2] = ( float )( angles[2]*180.0/PI );
//    setInverseRotationRadians( vec );
//}

//void Matrix::setRotationRadians( const float *angles )
//{
//    double cr = cos( angles[0] );
//    double sr = sin( angles[0] );
//    double cp = cos( angles[1] );
//    double sp = sin( angles[1] );
//    double cy = cos( angles[2] );
//    double sy = sin( angles[2] );

//    m_matrix[0] = ( float )( cp*cy );
//    m_matrix[1] = ( float )( cp*sy );
//    m_matrix[2] = ( float )( -sp );

//    double srsp = sr*sp;
//    double crsp = cr*sp;

//    m_matrix[4] = ( float )( srsp*cy-cr*sy );
//    m_matrix[5] = ( float )( srsp*sy+cr*cy );
//    m_matrix[6] = ( float )( sr*cp );

//    m_matrix[8] = ( float )( crsp*cy+sr*sy );
//    m_matrix[9] = ( float )( crsp*sy-sr*cy );
//    m_matrix[10] = ( float )( cr*cp );
//}

//void Matrix::setInverseRotationRadians( const float *angles )
//{
//    double cr = cos( angles[0] );
//    double sr = sin( angles[0] );
//    double cp = cos( angles[1] );
//    double sp = sin( angles[1] );
//    double cy = cos( angles[2] );
//    double sy = sin( angles[2] );

//    m_matrix[0] = ( float )( cp*cy );
//    m_matrix[4] = ( float )( cp*sy );
//    m_matrix[8] = ( float )( -sp );

//    double srsp = sr*sp;
//    double crsp = cr*sp;

//    m_matrix[1] = ( float )( srsp*cy-cr*sy );
//    m_matrix[5] = ( float )( srsp*sy+cr*cy );
//    m_matrix[9] = ( float )( sr*cp );

//    m_matrix[2] = ( float )( crsp*cy+sr*sy );
//    m_matrix[6] = ( float )( crsp*sy-sr*cy );
//    m_matrix[10] = ( float )( cr*cp );
//}

//void Matrix::setRotationQuaternion( const Quaternion& quat )
//{
//    m_matrix[0] = ( float )( 1.0 - 2.0*quat[1]*quat[1] - 2.0*quat[2]*quat[2] );
//    m_matrix[1] = ( float )( 2.0*quat[0]*quat[1] + 2.0*quat[3]*quat[2] );
//    m_matrix[2] = ( float )( 2.0*quat[0]*quat[2] - 2.0*quat[3]*quat[1] );

//    m_matrix[4] = ( float )( 2.0*quat[0]*quat[1] - 2.0*quat[3]*quat[2] );
//    m_matrix[5] = ( float )( 1.0 - 2.0*quat[0]*quat[0] - 2.0*quat[2]*quat[2] );
//    m_matrix[6] = ( float )( 2.0*quat[1]*quat[2] + 2.0*quat[3]*quat[0] );

//    m_matrix[8] = ( float )( 2.0*quat[0]*quat[2] + 2.0*quat[3]*quat[1] );
//    m_matrix[9] = ( float )( 2.0*quat[1]*quat[2] - 2.0*quat[3]*quat[0] );
//    m_matrix[10] = ( float )( 1.0 - 2.0*quat[0]*quat[0] - 2.0*quat[1]*quat[1] );
//}
    }
}