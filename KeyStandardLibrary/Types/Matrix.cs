#define USE_STRUCT


using System;

namespace Keystone.Types
{
    // http://msdn.microsoft.com/en-au/library/bb206269%28VS.85%29.aspx
#if USE_STRUCT
    public struct Matrix
#else
    public class Matrix
#endif
    {
        private double[,] _mat;
        private double _determinant;

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
        public Matrix(bool identity)
        {
            _mat = new double[4, 4];
            _determinant = 0d;
            if (identity)
            {
                _mat[0, 0] = 1.0d;
                _mat[1, 1] = 1.0d;
                _mat[2, 2] = 1.0d;
                _mat[3, 3] = 1.0d;
            }
        }

        /// <summary>
        /// Matrix from orientation quaternion.
        /// </summary>
        /// <param name="quat">Unit quaternion</param>
        public Matrix(Quaternion quat) : this()
        {
            //Matrix matrix = Matrix.Identity(); // new Matrix(); //

            double xx = quat.X * quat.X;
            double yy = quat.Y * quat.Y;
            double zz = quat.Z * quat.Z;
            double xy = quat.X * quat.Y;
            double xz = quat.X * quat.Z;
            double yz = quat.Y * quat.Z;
            double wx = quat.W * quat.X;
            double wy = quat.W * quat.Y;
            double wz = quat.W * quat.Z;

            _mat[0, 0] = 1.0 - 2.0 * (yy + zz);
            _mat[1, 0] = 2.0 * (xy - wz);
            _mat[2, 0] = 2.0 * (xz + wy);

            _mat[0, 1] = 2.0 * (xy + wz);
            _mat[1, 1] = 1.0 - 2.0 * (xx + zz);
            _mat[2, 1] = 2.0 * (yz - wx);

            _mat[0, 2] = 2.0 * (xz - wy);
            _mat[1, 2] = 2.0 * (yz + wx);
            _mat[2, 2] = 1.0 - 2.0 * (xx + yy);

            _mat[3, 0] = _mat[3, 1] = _mat[3, 2] = 0.0d;
            _mat[0, 3] = _mat[1, 3] = _mat[2, 3] = 0.0d;
            _mat[3, 3] = 1.0d;



            //
            //            double single9 = quat.X * quat.X;
            //            double single8 = quat.Y * quat.Y;
            //            double single7 = quat.Z * quat.Z;
            //            double single6 = quat.X * quat.Y;
            //            double single5 = quat.Z * quat.W;
            //            double single4 = quat.Z * quat.X;
            //            double single3 = quat.Y * quat.W;
            //            double single2 = quat.Y * quat.Z;
            //            double single1 = quat.X * quat.W;
            //            _mat[0, 0] = 1.0 - (2.0 * (single8 + single7));
            //            _mat[0, 1] = 2.0 * (single6 + single5);
            //            _mat[0, 2] = 2.0 * (single4 - single3);
            //            _mat[0, 3] = 0.0;
            //            _mat[1, 0] = 2.0 * (single6 - single5);
            //            _mat[1, 1] = 1.0 - (2.0 * (single7 + single9));
            //            _mat[1, 2] = 2.0 * (single2 + single1);
            //            _mat[1, 3] = 0.0;
            //            _mat[2, 0] = 2.0 * (single4 + single3);
            //            _mat[2, 1] = 2.0 * (single2 - single1);
            //            _mat[2, 2] = 1.0 - (2.0 * (single8 + single9));
            //            _mat[2, 3] = 0.0;
            //            _mat[3, 0] = 0.0;
            //            _mat[3, 1] = 0.0;
            //            _mat[3, 2]= 0.0;
            //            _mat[3, 3] = 1.0;
        }

        public Matrix(double m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24,
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44) : this()
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
        public Matrix(Matrix m) : this()
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

        //http://www.euclideanspace.com/maths/algebra/matrix/orthogonal/index.htm
        public Vector3d Right
        {
            get
            {
                Vector3d result;
                result.x = _mat[0, 0];
                result.y = _mat[1, 0];
                result.z = _mat[2, 0];
                return result;
            }
        }
        public Vector3d Up
        {
            get
            {
                Vector3d result;
                result.x = _mat[0, 1];
                result.y = _mat[1, 1];
                result.z = _mat[2, 1];
                return result;
            }
        }
        public Vector3d Backward
        {
            get
            {
                Vector3d result;
                result.x = _mat[0, 2];
                result.y = _mat[1, 2];
                result.z = _mat[2, 2];
                return result;
            }
        }

        public Vector3d GetTranslation()
        {
            Vector3d result;
            result.x = _mat[3, 0];
            result.y = _mat[3, 1];
            result.z = _mat[3, 2];
            return result;
        }

        public Vector3d GetScale()
        {
            Vector3d result;
            result.x = _mat[0, 0];
            result.y = _mat[1, 1];
            result.z = _mat[2, 2];
            return result;
        }

        public void SetTranslation(Vector3d translation)
        {
            _mat[3, 0] = translation.x;
            _mat[3, 1] = translation.y;
            _mat[3, 2] = translation.z;
        }

        public static Matrix Identity()
        {
            Matrix m = new Matrix(true);
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

        /// <summary>
        /// Creates a Translation Matrix that is first initialized to Identity.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix CreateTranslation(Vector3d v)
        {
            Matrix result = Identity();
            result.SetTranslation(v);
            return result;
        }

        public static Matrix CreateScaling(double x, double y, double z)
        {
            Matrix result = Identity();
            result._mat[0, 0] = x;
            result._mat[1, 1] = y;
            result._mat[2, 2] = z;
            return result;
        }

        public static Matrix CreateScaling(Vector3d v)
        {
            return CreateScaling(v.x, v.y, v.z);
        }


        // NOTE: The following offset rotation is not really useful because instead we are computing
        // the RegoinMatrix already taking into account a .Pivot value.
        // http://www.ogre3d.org/forums/viewtopic.php?f=5&t=11088&start=25
        // http://stackoverflow.com/questions/8747870/xna-rotation-over-given-vector <-- answer is translation offset + axis rotation 
        // http://stackoverflow.com/questions/8791845/xna-rotate-a-bone-with-an-offset-translation
        public static Matrix Rotation(Vector3d axis, double angleRadians, Vector3d offset)
        {
            return Matrix.CreateTranslation(offset) * Matrix.CreateRotation(axis, angleRadians);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rotationAxis">Unit vector</param>
        /// <param name="angleRadians"></param>
        /// <returns></returns>
        public static Matrix CreateRotation(Vector3d rotationAxis, double angleRadians)
        {
            //http://tools.devshed.com/c/a/Web-Development/Part-Three-Rotation-About-an-Arbitrary-Axis/
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);
            double invcos = 1.0d - cos;

            Matrix m = new Matrix();

            m.M11 = cos + rotationAxis.x * rotationAxis.x * invcos;
            m.M12 = rotationAxis.x * rotationAxis.y * invcos + (rotationAxis.z * sin);
            m.M13 = rotationAxis.x * rotationAxis.z * invcos - (rotationAxis.y * sin);
            m.M14 = 0;

            m.M21 = rotationAxis.x * rotationAxis.y * invcos - (rotationAxis.z * sin);
            m.M22 = cos + rotationAxis.y * rotationAxis.y * invcos;
            m.M23 = rotationAxis.y * rotationAxis.z * invcos + (rotationAxis.x * sin);
            m.M24 = 0;

            m.M31 = rotationAxis.x * rotationAxis.z * invcos + (rotationAxis.y * sin);
            m.M32 = rotationAxis.y * rotationAxis.z * invcos - (rotationAxis.x * sin);
            m.M33 = cos + rotationAxis.z * rotationAxis.z * invcos;
            m.M34 = 0;

            m.M41 = 0;
            m.M42 = 0;
            m.M43 = 0;
            m.M44 = 1;

            // below is extracted from xna's RotationFromAxis and it seems to produce same results
            // strange because using a 0,0,1 axis with 0 angleRadians is resulting in no rotation (identity) at all!
            // wtf?  

            //double single5 = rotationAxis.x;
            //double single4 = rotationAxis.y;
            //double single3 = rotationAxis.z;
            //double single2 = Math.Sin(angleRadians);
            //double single1 = Math.Cos(angleRadians);
            //double single11 = single5 * single5;
            //double single10 = single4 * single4;
            //double single9 = single3 * single3;
            //double single8 = single5 * single4;
            //double single7 = single5 * single3;
            //double single6 = single4 * single3;
            //m.M11 = single11 + (single1 * (1.00 - single11));
            //m.M12 = (single8 - (single1 * single8)) + (single2 * single3);
            //m.M13 = (single7 - (single1 * single7)) - (single2 * single4);
            //m.M14 = 0.00;
            //m.M21 = (single8 - (single1 * single8)) - (single2 * single3);
            //m.M22 = single10 + (single1 * (1.00 - single10));
            //m.M23 = (single6 - (single1 * single6)) + (single2 * single5);
            //m.M24 = 0.00;
            //m.M31 = (single7 - (single1 * single7)) + (single2 * single4);
            //m.M32 = (single6 - (single1 * single6)) - (single2 * single5);
            //m.M33 = single9 + (single1 * (1.00 - single9));
            //m.M34 = 0.00;
            //m.M41 = 0.00;
            //m.M42 = 0.00;
            //m.M43 = 0.00;
            //m.M44 = 1.00;
            return m;
        }

        // left handed x rotation matrix
        public static Matrix CreateRotationX(double angleRadians)
        {
            // Assuming the angle is in radians. 
            double cos_x = Math.Cos(angleRadians);
            double sin_x = Math.Sin(angleRadians);
            Matrix tmp = Matrix.Identity();
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

        public static Matrix CreateRotationY(double angleRadians)
        {
            // Assuming the angle is in radians.
            double c = Math.Cos(angleRadians);
            double s = Math.Sin(angleRadians);
            Matrix tmp = Matrix.Identity();
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

        public static Matrix CreateRotationZ(double angleRadians)
        {
            // Assuming the angle is in radians. 
            double c = Math.Cos(angleRadians);
            double s = Math.Sin(angleRadians);
            Matrix tmp = Matrix.Identity();
            tmp.M11 = c;
            tmp.M12 = s;
            tmp.M13 = 0.0;
            tmp.M14 = 0;
            tmp.M21 = -s;
            tmp.M22 = c;
            tmp.M23 = 0.0;
            tmp.M24 = 0;
            tmp.M31 = 0.0;
            tmp.M32 = 0.0;
            tmp.M33 = 1.0;
            tmp.M34 = 0;
            tmp.M41 = 0;
            tmp.M42 = 0;
            tmp.M43 = 0;
            tmp.M44 = 1;
            return tmp;
        }

        // http://stackoverflow.com/questions/349050/calculating-a-lookat-matrix
        // this is a left handed view matrix.  to use as a rotation for a model, take it's inverse.
        public static Matrix CreateLookAt(Vector3d position, Vector3d target, Vector3d up)
        {
            Matrix matrix1 = new Matrix();
            // TODO: negation hack! for some reason we have to negate Vector3d.Normalize(position - target) or the rotation is off by 180 degrees
            // TODO: hopefully this is not something quirky with TV View matrix which is all we use this for so far.
            //       But eventually when we try to get one ship to rotate to another, we'll see if that is reversed and then we'll know
            Vector3d forward = -Vector3d.Normalize(position - target); // ZAXIS
            // orthonormalize (aka up and forward are orthogonal and normalized)
            Vector3d newUp = Vector3d.Normalize(Vector3d.CrossProduct(up, forward)); // XAXIS
            //Vector3d right = Vector3d.CrossProduct(forward, newUp); // YAXIS
            Vector3d right = Vector3d.Normalize(Vector3d.CrossProduct(forward, newUp)); // YAXIS // Normalize here not necessary?
            matrix1.M11 = newUp.x;   // XAXIS
            matrix1.M12 = right.x;   // YAXIS
            matrix1.M13 = forward.x; // ZAXIS
            matrix1.M14 = 0.00d;
            matrix1.M21 = newUp.y;   // XAXIS
            matrix1.M22 = right.y;   // YAXIS
            matrix1.M23 = forward.y; // ZAXIS
            matrix1.M24 = 0.00d;
            matrix1.M31 = newUp.z;   // XAXIS
            matrix1.M32 = right.z;   // YAXIS
            matrix1.M33 = forward.z; // ZAXIS
            matrix1.M34 = 0.00d;
            //matrix1.M41 = -Vector3d.DotProduct(newUp, position);
            //matrix1.M42 = -Vector3d.DotProduct(right, position);
            //matrix1.M43 = -Vector3d.DotProduct(forward, position);
            matrix1.M44 = 1.00d;
            return matrix1;
        }

        public static Matrix PerspectiveFOVLH(double near, double far, double fovRadians, int viewportWidth, int viewportHeight, ref double aspectRatio)
        {
            Matrix proj = new Matrix();

            double cot = 1d / Math.Tan(fovRadians * 0.5d);

            aspectRatio = (double)viewportWidth / (double)viewportHeight; // floating point divide

            proj.M11 = cot / aspectRatio;
            proj.M22 = cot;
            proj.M33 = far / (far - near);
            proj.M34 = 1d;
            // Hypno - May.2.2012 - I switched the bottom line around from the commented one to the current
            // I havent noticed a difference yet but i've not tested much.  I need to verify which is correct.
            proj.M43 = -near * far / (far - near);
            // proj.M43 = -(far * near / (far - near));
            return proj;


            //double num = 1d / Math.Tan(fovRadians * 0.5d);
            //double m = num / aspectRatio;
            //Matrix result = new Matrix ();
            //result.M11 = m;
            //result.M12 = result.M13 = result.M14 = 0d;
            //result.M22 = num;
            //result.M21 = result.M23 = result.M24 = 0d;
            //result.M31 = result.M32 = 0d;
            //result.M33 = far / (near - far);
            //result.M34 = -1d;         
            //result.M41 = result.M42 = result.M44 = 0d;
            //result.M43 = near * far / (near - far);


            //result.M33 = far / (far - near);
            //result.M34 = 1d;
            //result.M43 = -near * far / (far - near);
            //return result;
        }

        /// <summary>
        /// http://www.codeguru.com/Cpp/misc/misc/math/article.php/c10123__2/    <-- deriving projection matrices
        /// src : http://www.ogre3d.org/forums/viewtopic.php?f=2&t=26244&start=0
        /// Then you can use it like this. The ctrl.Width & scale are the width and height of your window/viewport in pixels. This is needed to keep a correct aspect ratio.
        /// float scale = 0.5f; // Your scale here.
        ///Matrix4 p = this.BuildScaledOrthoMatrix(ctrl.Width  / scale / -2.0f,
        ///                                        ctrl.Width  / scale /  2.0f,
        ///                                        ctrl.Height / scale / -2.0f,
        ///                                        ctrl.Height / scale /  2.0f, 0, 1000);
        ///m_camera.SetCustomProjectionMatrix(true, p);
        ///You can also pan simply by moving the position of the plane :
        ///Matrix p = this.ScaledOrthoMatrix(ctrl.Width  / scale / -2.0f + tx,
        ///                                ctrl.Width  / scale /  2.0f + tx,
        ///                                ctrl.Height / scale / -2.0f + ty,
        ///                                ctrl.Height / scale /  2.0f + ty, 0, 1000);
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <param name="top"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public static Matrix ScaledOrthoMatrix(double left, double right, double top, double bottom, double near, double far)
        {
            // March.6.2011 - This projection matrix is verified correct for all ortho views
            double invw = 1 / (right - left);
            double invh = 1 / (bottom - top);
            double invd = 1 / (far - near);

            Matrix proj = new Matrix();  // Matrix.Zero
            proj._mat[0, 0] = 2 * invw;
            //proj._mat[0, 3] = -(right + left) * invw;  // for offcenter matrices
            proj._mat[1, 1] = 2 * invh;
            //proj._mat[1, 3] = -(top + bottom) * invh; // for offcenter matrices
            proj._mat[2, 2] = invd;
            proj._mat[2, 3] = -near * invd;
            proj._mat[3, 3] = 1;
            return proj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scale">1.0 equals a field of view of 45 (i think???)</param>
        /// <param name="viewportWidth"></param>
        /// <param name="viewportHeight"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public static Matrix ScaledOrthoMatrix(double scale, double viewportWidth, double viewportHeight, double near, double far)
        {
            double right = viewportWidth * scale / 2;
            double left = -right;
            double bottom = viewportHeight * scale / 2;
            double top = -bottom;

            return ScaledOrthoMatrix(left, right, top, bottom, near, far);
        }

        public static Matrix CreateBillboardRotationMatrix(Vector3d cameraUp, Vector3d cameraLook)
        {
            Matrix rotationMatrix = new Matrix(); // Types.Matrix.Identity();

            Vector3d r = Vector3d.CrossProduct(cameraUp, cameraLook);
            r.Normalize();
            rotationMatrix.M11 = r.x;
            rotationMatrix.M12 = r.y;
            rotationMatrix.M13 = r.z;
            rotationMatrix.M21 = cameraUp.x;
            rotationMatrix.M22 = cameraUp.y;
            rotationMatrix.M23 = cameraUp.z;
            rotationMatrix.M31 = cameraLook.x;
            rotationMatrix.M32 = cameraLook.y;
            rotationMatrix.M33 = cameraLook.z;
            // we dont include position in this rotation matrix
            // caller should do rotationMatrix.SetTranslation(vec) if they wish
            // to make this a final world matrix and not just a rotation matrix
            rotationMatrix.M44 = 1.0;

            return rotationMatrix;
        }

        //<Hypnotron> <Toaster> cause if you look at them head on its just flat which is to be expected  <-- i think i missed tha tline the first time somehow
        //<Hypnotron> i see what you're saying now tho.. maybe they special case the rendering when its headed directly at the camera (+/- some variance)
        //<Hypnotron> and do as you say, add a cap billboard to the end /me shrugs
        //<Hypnotron> or maybe they do nothing
        //<Aeon> do what I do, put a split right in the middle, when the angle to the camera is so that it's staring down the middle, inflate the split a little so it shows up
        //<Aeon> so it's 4 triangles instead of two
        //<Aeon> instead of a plane like [/]  make it like [X] and get that center point, duplicate underneath, then you can move them up and down the planes Z plane to give depth when looking down the plane
        //<Hypnotron> interesting
        //<Hypnotron> like opening an umbrella...
        //<Hypnotron> somewhat
        //<Hypnotron> cracking it open anyway
        // note: using 8 triangles and not just sandwhiching 2 x 2 triangle quads looks better at all angles... in case wondering why not just use two quads instead of 4.


        /// <summary>
        /// Creates a world matrix in camera space NOT a local space matrix.  NOTE that the billboard position is in camera space position or world position if cameraPosition is in world space.
        /// In otherwords, there is no need to multiply this returned matrix with a derivedRotation.
        /// </summary>
        /// <param name="up"></param>
        /// <param name="billboardPosition"></param>
        /// <param name="cameraPosition"></param>
        /// <returns></returns>
        public static Matrix CreateAxialBillboardRotationMatrix(Vector3d up, Vector3d billboardPosition, Vector3d cameraPosition)
        {
            // https://www.flipcode.com/archives/Billboarding-Excerpt_From_iReal-Time_Renderingi_2E.shtml
            // https://forum.unity.com/threads/billboard-script-flat-spherical-arbitrary-axis-aligned.539481/

            // https://gamedev.stackexchange.com/questions/188636/cylindrical-billboarding-around-an-arbitrary-axis-in-geometry-shader
            up = Vector3d.Up(); // Hypnotron Dec.18.2025 // TODO: verify this fixes the issue and then we can modifiy what we pass into this function            
            
            // look == forward vector
            Vector3d look = Vector3d.Normalize(billboardPosition - cameraPosition);

            Vector3d right = Vector3d.Normalize(Vector3d.CrossProduct(up, look));

            // March.11.2024 - up is actually our axis and should not be recomputed. This fixes the issue with billboard not appearing to point towards it target at certain angles
            //           up = Vector3d.Normalize(Vector3d.CrossProduct(look, right));

            // recompute a new Up() vector. // Hypnotron Dec.18.2025
            // todo: test the following now that we think we understand that the initial up vector is that of the Billboard which is always 0,1,0?
            up = Vector3d.Normalize(Vector3d.CrossProduct(look, right));

            Matrix rotationMatrix = new Matrix(); // Types.Matrix.Identity();
            rotationMatrix.M11 = right.x;
            rotationMatrix.M12 = right.y;
            rotationMatrix.M13 = right.z;
            rotationMatrix.M21 = up.x;
            rotationMatrix.M22 = up.y;
            rotationMatrix.M23 = up.z;
            rotationMatrix.M31 = look.x;
            // rotationMatrix.M32 = look.z; //? apparently this z and y following are not transposed by accident
            // rotationMatrix.M33 = look.y;
            rotationMatrix.M32 = look.y;  //? i dunno, y and z seems to work with also with no visual difference at runtime
            rotationMatrix.M33 = look.z;

            rotationMatrix.M44 = 1.0;
            return rotationMatrix;
        }



        public static Matrix CreateAxialBillboardRotationMatrix(Matrix rotationMatrix, Vector3d billboardPosition,
                                                       Vector3d cameraPosition)
        {
            return
                CreateAxialBillboardRotationMatrix(new Vector3d(rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23), billboardPosition, cameraPosition);
        }


        ////For a square matrix A, the inverse is written A-1. When A is multiplied by A-1 the result is the identity matrix I. 
        //// Non-square matrices do not have inverses.
        //// http://www.mathwords.com/i/inverse_of_a_matrix.htm
        //// Note: Not all square matrices have inverses. A square matrix which has an inverse is called invertible or nonsingular,
        //// and a square matrix without an inverse is called noninvertible or singular.
        //public static Matrix Inverse(Matrix m)
        //{
        //    // the following seems to work with physics collision whereas the simple InverseView() transpose does not.  That one
        //    // seems to work ok for perspective view picking

        //    // WARNING: The xna code works with picking best
        //    // for some reason the following loop does not!

        //  double e;
        //  Matrix m1 = new Matrix(m);

        //  for (int k = 0; k < 4; ++k)
        //  {
        //      e = m1._mat[k, k];
        //      m1._mat[k, k] = 1.0d;
        //      if (e == 0.0) { System.Diagnostics.Debug.WriteLine("Matrix.Inverse() - Inversion error.");  return m1; }// TODO: returning seems to work ok'ish.  throwing exception is lame throw new Exception("Matrix inversion error");
        //      for (int j = 0; j < 4; ++j)
        //           m1._mat[k, j] = m1._mat[k, j] / e;
        //      for (int i = 0; i < 4; ++i)
        //      {
        //          if (i != k)
        //          {
        //              e = m1._mat[i, k];
        //              m1._mat[i, k] = 0.0d;
        //              for (int j = 0; j < 4; ++j)
        //                  m1._mat[i, j] = m1._mat[i, j] - e * m1._mat[k, j];
        //          }
        //      }
        //  }
        //
        //  Matrix tmp = m * m1;
        //  //System.Diagnostics.Debug.Assert (tmp.Equals (Matrix.Identity())); 
        //  return m1;
        //
        //}

        //        private static Matrix InverseTV3D (Matrix m)
        //        {
        //
        //            // TVMatrixInverse() works for picking and culling and everything but
        //            // 1) we don't want the MTV3D65 dependancy in keystone.dll or keymath.dll since server shouldn't require windows and DX
        //            // 2) the loss of precision when using single floating point precision matrices are bad for space sim
        //            MTV3D65.TV_3DMATRIX tvmat = Keystone.Types. Matrix.ToTV3DMatrix(m);
        //            MTV3D65.TV_3DMATRIX inv = new MTV3D65.TV_3DMATRIX();
        //            float det = 0;
        //
        //            CoreClient._Core.Maths.TVMatrixInverse(ref inv, ref det, tvmat);
        //            return new Matrix(inv);
        //        }

        private static Matrix InvertSlimDX(Matrix value)
        {
            double b0 = (value.M31 * value.M42) - (value.M32 * value.M41);
            double b1 = (value.M31 * value.M43) - (value.M33 * value.M41);
            double b2 = (value.M34 * value.M41) - (value.M31 * value.M44);
            double b3 = (value.M32 * value.M43) - (value.M33 * value.M42);
            double b4 = (value.M34 * value.M42) - (value.M32 * value.M44);
            double b5 = (value.M33 * value.M44) - (value.M34 * value.M43);

            double d11 = value.M22 * b5 + value.M23 * b4 + value.M24 * b3;
            double d12 = value.M21 * b5 + value.M23 * b2 + value.M24 * b1;
            double d13 = value.M21 * -b4 + value.M22 * b2 + value.M24 * b0;
            double d14 = value.M21 * b3 + value.M22 * -b1 + value.M23 * b0;

            double det = value.M11 * d11 - value.M12 * d12 + value.M13 * d13 - value.M14 * d14;
            if (Math.Abs(det) <= 00.0000001d) // the epsilon used here could fail if a very large model is scaled down sufficiently much. For now this value works.
            {

                return new Matrix();
            }

            det = 1d / det;

            double a0 = (value.M11 * value.M22) - (value.M12 * value.M21);
            double a1 = (value.M11 * value.M23) - (value.M13 * value.M21);
            double a2 = (value.M14 * value.M21) - (value.M11 * value.M24);
            double a3 = (value.M12 * value.M23) - (value.M13 * value.M22);
            double a4 = (value.M14 * value.M22) - (value.M12 * value.M24);
            double a5 = (value.M13 * value.M24) - (value.M14 * value.M23);

            double d21 = value.M12 * b5 + value.M13 * b4 + value.M14 * b3;
            double d22 = value.M11 * b5 + value.M13 * b2 + value.M14 * b1;
            double d23 = value.M11 * -b4 + value.M12 * b2 + value.M14 * b0;
            double d24 = value.M11 * b3 + value.M12 * -b1 + value.M13 * b0;

            double d31 = value.M42 * a5 + value.M43 * a4 + value.M44 * a3;
            double d32 = value.M41 * a5 + value.M43 * a2 + value.M44 * a1;
            double d33 = value.M41 * -a4 + value.M42 * a2 + value.M44 * a0;
            double d34 = value.M41 * a3 + value.M42 * -a1 + value.M43 * a0;

            double d41 = value.M32 * a5 + value.M33 * a4 + value.M34 * a3;
            double d42 = value.M31 * a5 + value.M33 * a2 + value.M34 * a1;
            double d43 = value.M31 * -a4 + value.M32 * a2 + value.M34 * a0;
            double d44 = value.M31 * a3 + value.M32 * -a1 + value.M33 * a0;

            Matrix result = Matrix.Identity();
            result.M11 = +d11 * det; result.M12 = -d21 * det; result.M13 = +d31 * det; result.M14 = -d41 * det;
            result.M21 = -d12 * det; result.M22 = +d22 * det; result.M23 = -d32 * det; result.M24 = +d42 * det;
            result.M31 = +d13 * det; result.M32 = -d23 * det; result.M33 = +d33 * det; result.M34 = -d43 * det;
            result.M41 = -d14 * det; result.M42 = +d24 * det; result.M43 = -d34 * det; result.M44 = +d44 * det;

            return result;
        }

        // 4x4 Matrix Inverse
        public static Matrix Inverse(Matrix m)
        {
            // return InverseTV3D (m);
            return InvertSlimDX(m);

            //double e;
            //Matrix m1 = new Matrix(m);

            //for (int k = 0; k < 4; ++k)
            //{
            //    e = m1._mat[k, k];
            //    m1._mat[k, k] = 1.0d;
            //    if (e == 0.0) { System.Diagnostics.Debug.WriteLine("Matrix.Inverse() - Inversion error."); return m1; }// TODO: returning seems to work ok'ish.  throwing exception is lame throw new Exception("Matrix inversion error");
            //    for (int j = 0; j < 4; ++j)
            //        m1._mat[k, j] = m1._mat[k, j] / e;
            //    for (int i = 0; i < 4; ++i)
            //    {
            //        if (i != k)
            //        {
            //            e = m1._mat[i, k];
            //            m1._mat[i, k] = 0.0d;
            //            for (int j = 0; j < 4; ++j)
            //                m1._mat[i, j] = m1._mat[i, j] - e * m1._mat[k, j];
            //        }
            //    }
            //}

            //Matrix tmp = m * m1;
            ////System.Diagnostics.Debug.Assert (tmp.Equals (Matrix.Identity())); 
            //return m1;

            // WARNING: Below works with picking really well, above does not!
            Matrix result = new Matrix();
            double single5 = m.M11;
            double single4 = m.M12;
            double single3 = m.M13;
            double single2 = m.M14;
            double single9 = m.M21;
            double single8 = m.M22;
            double single7 = m.M23;
            double single6 = m.M24;
            double single17 = m.M31;
            double single16 = m.M32;
            double single15 = m.M33;
            double single14 = m.M34;
            double single13 = m.M41;
            double single12 = m.M42;
            double single11 = m.M43;
            double single10 = m.M44;
            double single23 = (single15 * single10) - (single14 * single11);
            double single22 = (single16 * single10) - (single14 * single12);
            double single21 = (single16 * single11) - (single15 * single12);
            double single20 = (single17 * single10) - (single14 * single13);
            double single19 = (single17 * single11) - (single15 * single13);
            double single18 = (single17 * single12) - (single16 * single13);
            double single39 = ((single8 * single23) - (single7 * single22)) + (single6 * single21);
            double single38 = -(((single9 * single23) - (single7 * single20)) + (single6 * single19));
            double single37 = ((single9 * single22) - (single8 * single20)) + (single6 * single18);
            double single36 = -(((single9 * single21) - (single8 * single19)) + (single7 * single18));
            double single1 = 1.00d / ((((single5 * single39) + (single4 * single38)) + (single3 * single37)) + (single2 * single36));
            result.M11 = single39 * single1;
            result.M21 = single38 * single1;
            result.M31 = single37 * single1;
            result.M41 = single36 * single1;
            result.M12 = -(((single4 * single23) - (single3 * single22)) + (single2 * single21)) * single1;
            result.M22 = (((single5 * single23) - (single3 * single20)) + (single2 * single19)) * single1;
            result.M32 = -(((single5 * single22) - (single4 * single20)) + (single2 * single18)) * single1;
            result.M42 = (((single5 * single21) - (single4 * single19)) + (single3 * single18)) * single1;
            double single35 = (single7 * single10) - (single6 * single11);
            double single34 = (single8 * single10) - (single6 * single12);
            double single33 = (single8 * single11) - (single7 * single12);
            double single32 = (single9 * single10) - (single6 * single13);
            double single31 = (single9 * single11) - (single7 * single13);
            double single30 = (single9 * single12) - (single8 * single13);
            result.M13 = (((single4 * single35) - (single3 * single34)) + (single2 * single33)) * single1;
            result.M23 = -(((single5 * single35) - (single3 * single32)) + (single2 * single31)) * single1;
            result.M33 = (((single5 * single34) - (single4 * single32)) + (single2 * single30)) * single1;
            result.M43 = -(((single5 * single33) - (single4 * single31)) + (single3 * single30)) * single1;
            double single29 = (single7 * single14) - (single6 * single15);
            double single28 = (single8 * single14) - (single6 * single16);
            double single27 = (single8 * single15) - (single7 * single16);
            double single26 = (single9 * single14) - (single6 * single17);
            double single25 = (single9 * single15) - (single7 * single17);
            double single24 = (single9 * single16) - (single8 * single17);
            result.M14 = -(((single4 * single29) - (single3 * single28)) + (single2 * single27)) * single1;
            result.M24 = (((single5 * single29) - (single3 * single26)) + (single2 * single25)) * single1;
            result.M34 = -(((single5 * single28) - (single4 * single26)) + (single2 * single24)) * single1;
            result.M44 = (((single5 * single27) - (single4 * single25)) + (single3 * single24)) * single1;
            return result;

        }


        // a simple inverse to work with View matrix and is used by Picking
        // http://www.gamedev.net/community/forums/topic.asp?topic_id=288155
        public static Matrix InverseView(Matrix m)
        {
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
            // negate the translation of T
            TInv.M41 = -T.M41;
            TInv.M42 = -T.M42;
            TInv.M43 = -T.M43;
            TInv.M44 = -T.M44; // TODO: have tried with this line commented out and not and no real difference.

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">the coordinate system that is we start from</param>
        /// <param name="dest">the destination coordinate system we want to transform the source to</param>
        /// <returns></returns>
        public static Matrix Source2Dest(Matrix source, Matrix dest)
        {
            Matrix root2dest = dest;
            Matrix source2root = Matrix.Inverse(source);  // NOTE: fails on 4x4 matrix such as view
            Matrix source2dest = root2dest * source2root;

#if DEBUG
        	// TODO: verify that we can first subtract the relative difference in positions from both matrices to cancel those out before we 
        	//       start matrix multiplication?  <-- Feb.4.2014 - i think the below actually proves this does work, however if there is no difference
        	//       then there's no need to do it because it's a bit more expensive
            // get the difference in translation between src and dst matrices
            // verify the computed source2dest now has a translation that is equal to -difference
            // within some tolerance and that if we were to subtract out that difference first, then
			// our results would be more precise  
		
			Vector3d srcTranslation = source.GetTranslation();
			Vector3d dstTranslation= dest.GetTranslation ();
			Vector3d diff = dstTranslation - srcTranslation;
			Vector3d s2dTranslation = source2dest.GetTranslation();
	//		System.Diagnostics.Debug.Assert (s2dTranslation.Equals (diff));
#endif
            return source2dest;
        }

        // TODO: looks like Sylvain has finally added TVMath.TVEulerAnglesFromMatrix(Rot, Matx)
        // if i have any problems with this, i can try switching to tv's version
        public static void Decompose(Matrix mx, out Vector3d outPosition, out Vector3d outRotation,
                                       out Vector3d outScale)
        {
            Vector3d positionResult;
            positionResult.x = mx.M41;
            positionResult.y = mx.M42;
            positionResult.z = mx.M43;

            Vector3d scaleResult;
            scaleResult.x = mx.M11;
            scaleResult.y = mx.M22;
            scaleResult.z = mx.M33;

            outPosition = positionResult;
            outScale = scaleResult;

            outRotation = DecomposeRollPitchYawZXYMatrix(mx);
        }

        //TODO: Read this article
        // http://www.robertblum.com/articles/2005/02/14/decomposing-matrices#comments
        // that suggest the following isnt really the best way to do this.  We should implement
        // the other as well.
        /// <summary>
        /// Decomposes a RotationMatrix to yaw, pitch and roll.
        /// Freeware Code taken from  Mike Pelton's blog.
        /// http://blogs.msdn.com/mikepelton/archive/2004/10/29/249501.aspx
        /// </summary>
        /// <param name="mx"></param>
        public static Vector3d DecomposeRollPitchYawZXYMatrix(Matrix mx)
        {
            double toDegrees = Utilities.MathHelper.RADIANS_TO_DEGREES;
            double xPitch, yYaw, zRoll;
            xPitch = Math.Asin(-mx.M32) * toDegrees; // TODO: the toDegrees... is consistatnt with code above were we convert degrees to radians, but with JigLib, it seems we need to leave them in radians so it seems i have some issue where im mixng the two improperly... :/
            double threshold = 0.001; // Hardcoded constant - burn him, he's a witch
            double test = Math.Cos(xPitch);

            if (test > threshold)
            {
                zRoll = Math.Atan2(mx.M12, mx.M22) * toDegrees;
                yYaw = Math.Atan2(mx.M31, mx.M33) * toDegrees;
            }
            else
            {
                zRoll = Math.Atan2(-mx.M21, mx.M11) * toDegrees;
                //"This being maths there are gotcha's - when the cosine of the pitch angle gets small, 
                //(for a pitch of 90-ish degrees, say) numerically things go bananas, so you can take an arbitrary
                //decision about the yaw angle (here I've set it to zero) and deduce a roll. This is okay except
                //where numerical consistency is important (flying jet fighters. guiding satellites and so on) where 
                //you can't just swizzle your object in space to make the sums work. If you're looking for the "proper" 
                //way to do this, I can recommend a piece I stumbled on called The Right Way to Calculate Stuff by Don Hatch."
                // - Mike Pelton 
                yYaw = 0.0d;
            }

            Vector3d result;
            result.x = xPitch;
            result.y = yYaw;
            result.z = zRoll;
            return result;
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
        public static Quaternion RotationArc(Vector3d v0, Vector3d v1)
        {
            Vector3d cross = Vector3d.CrossProduct(v0, v1);
            double d = Vector3d.DotProduct(v0, v1);
            double s = Math.Sqrt((1 + d) * 2);
            double recip = 1.0d / s;

            Vector3d res = cross * recip;
            return new Quaternion(res.x, res.y, res.z, s * 0.5d);
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

        public static Matrix Subtract(Matrix m1, Matrix m2)
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
            return Multiply4x4(m1, m2);

            //Matrix result = new Matrix();
            //result._mat[0, 0] = (m1.M11*m2.M11) + (m1.M12*m2.M21) + (m1.M13*m2.M31);
            //result._mat[0, 1] = (m1.M11*m2.M12) + (m1.M12*m2.M22) + (m1.M13*m2.M32);
            //result._mat[0, 2] = (m1.M11*m2.M13) + (m1.M12*m2.M23) + (m1.M13*m2.M33);
            //result._mat[0, 3] = 0.0;

            //result._mat[1, 0] = (m1.M21*m2.M11) + (m1.M22*m2.M21) + (m1.M23*m2.M31);
            //result._mat[1, 1] = (m1.M21*m2.M12) + (m1.M22*m2.M22) + (m1.M23*m2.M32);
            //result._mat[1, 2] = (m1.M21*m2.M13) + (m1.M22*m2.M23) + (m1.M23*m2.M33);
            //result._mat[1, 3] = 0.0;

            //result._mat[2, 0] = (m1.M31*m2.M11) + (m1.M32*m2.M21) + (m1.M33*m2.M31);
            //result._mat[2, 1] = (m1.M31*m2.M12) + (m1.M32*m2.M22) + (m1.M33*m2.M32);
            //result._mat[2, 2] = (m1.M31*m2.M13) + (m1.M32*m2.M23) + (m1.M33*m2.M33);
            //result._mat[2, 3] = 0.0;

            //result._mat[3, 0] = (m1.M41*m2.M11) + (m1.M42*m2.M21) + (m1.M43*m2.M31) + m2.M41;
            //result._mat[3, 1] = (m1.M41*m2.M12) + (m1.M42*m2.M22) + (m1.M43*m2.M32) + m2.M42;
            //result._mat[3, 2] = (m1.M41*m2.M13) + (m1.M42*m2.M23) + (m1.M43*m2.M33) + m2.M43;
            //result._mat[3, 3] = 1.0;
            //return result;
        }

        /// <summary>
        /// Scalar multiplication is easy. You just take a regular number (called a "scalar") and multiply it on every entry in the matrix.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Multiply(Matrix m, double scalar)
        {
            Matrix result = new Matrix();
            result._mat[0, 0] = m.M11 * scalar;
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
            result._mat[3, 2] = m.M43 * scalar;
            result._mat[3, 3] = m.M44 * scalar;
            return result;
        }

        ///// <summary>
        ///// Full 4x4 multiplication for 4x4 matrices such as the projection matrix
        ///// </summary>
        ///// <param name="m1"></param>
        ///// <param name="m2"></param>
        ///// <returns></returns>
        //public static Matrix Multiply4x4(Matrix m1, Matrix m2)
        //{
        //    Matrix result = new Matrix();
        //    for (int i = 0; i < 4; i++) // rows
        //    {
        //        for (int j = 0; j < 4; j++) // columns
        //        {
        //            double value = 0;
        //            for (int k = 0; k < 4; k++) 
        //            {
        //                value += m1._mat[i, k] * m2._mat[k, j];
        //            }
        //            result._mat[i, j] = value;  // [row , column]
        //        }
        //    }
        //    return result;
        //}
        public static Matrix Multiply4x4(Matrix matrix1, Matrix matrix2)
        {
            Matrix result = new Matrix();
            result.M11 = matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.M41;
            result.M12 = matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.M42;
            result.M13 = matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.M43;
            result.M14 = matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44;
            result.M21 = matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.M41;
            result.M22 = matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.M42;
            result.M23 = matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.M43;
            result.M24 = matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44;
            result.M31 = matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.M41;
            result.M32 = matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.M42;
            result.M33 = matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.M43;
            result.M34 = matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44;
            result.M41 = matrix1.M41 * matrix2.M11 + matrix1.M42 * matrix2.M21 + matrix1.M43 * matrix2.M31 + matrix1.M44 * matrix2.M41;
            result.M42 = matrix1.M41 * matrix2.M12 + matrix1.M42 * matrix2.M22 + matrix1.M43 * matrix2.M32 + matrix1.M44 * matrix2.M42;
            result.M43 = matrix1.M41 * matrix2.M13 + matrix1.M42 * matrix2.M23 + matrix1.M43 * matrix2.M33 + matrix1.M44 * matrix2.M43;
            result.M44 = matrix1.M41 * matrix2.M14 + matrix1.M42 * matrix2.M24 + matrix1.M43 * matrix2.M34 + matrix1.M44 * matrix2.M44;
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
            return Matrix.Multiply(m1, m2);
        }

        public static Matrix operator *(Matrix m, double scalar)
        {
            return Matrix.Multiply(m, scalar);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Matrix == false) return false;

            return this.Equals((Matrix)obj);
        }

        public bool Equals(Matrix m)
        {
            return
                _mat[0, 0] == m.M11 &&
                _mat[0, 1] == m.M12 &&
                _mat[0, 2] == m.M13 &&
                _mat[0, 3] == m.M14 &&

                _mat[1, 0] == m.M21 &&
                _mat[1, 1] == m.M22 &&
                _mat[1, 2] == m.M23 &&
                _mat[1, 3] == m.M24 &&

                _mat[2, 0] == m.M31 &&
                _mat[2, 1] == m.M32 &&
                _mat[2, 2] == m.M33 &&
                _mat[2, 3] == m.M34 &&

                _mat[3, 0] == m.M41 &&
                _mat[3, 1] == m.M42 &&
                _mat[3, 2] == m.M43 &&
                _mat[3, 3] == m.M44;
        }

        public bool IsNullOrEmpty()
        {
            return _mat[0, 0] == 0d &&
                _mat[0, 1] == 0d &&
                _mat[0, 2] == 0d &&
                _mat[0, 3] == 0d &&

                _mat[1, 0] == 0d &&
                _mat[1, 1] == 0d &&
                _mat[1, 2] == 0d &&
                _mat[1, 3] == 0d &&

                _mat[2, 0] == 0d &&
                _mat[2, 1] == 0d &&
                _mat[2, 2] == 0d &&
                _mat[2, 3] == 0d &&

                _mat[3, 0] == 0d &&
                _mat[3, 1] == 0d &&
                _mat[3, 2] == 0d &&
                _mat[3, 3] == 0d;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
            return base.GetHashCode();
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