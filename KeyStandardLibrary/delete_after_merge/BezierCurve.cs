using System;
using MTV3D65;

namespace Core.Types
{
    internal class BezierCurve
    {
        //http://www.gamedev.net/reference/articles/article1808.asp
        public static Vector3d[] Get3DBezierLinePoints(Vector3d A, Vector3d B, uint numPoints)
        {
            return Get3DBezierLinePoints(A.x, A.y, A.z, B.x, B.y, B.z, numPoints);
        }

        public static Vector3d[] Get3DBezierLinePoints(double Ax, double Ay, double Az, double Bx, double By, double Bz,
                                                       uint numPoints)
        {
            Vector3d[] results = new Vector3d[numPoints];
            double step = 1f/(numPoints - 1);
            double a = 1;

            for (int i = 0; i < results.Length; i++)
            {
                double b = 1 - a;

                double x = (a*Ax) + (b*Bx);
                double y = (a*Ay) + (b*By);
                double z = (a*Az) + (b*Bz);

                results[i] = new Vector3d(x, y, z);
                a -= step;
            }
            return results;
        }

        public static TV_2DVECTOR[] Get2DBezierLinePoints(TV_2DVECTOR A, TV_2DVECTOR B, uint numPoints)
        {
            return Get2DBezierLinePoints(A.x, A.y, B.x, B.y, numPoints);
        }

        public static TV_2DVECTOR[] Get2DBezierLinePoints(double Ax, double Ay, double Bx, double By, uint numPoints)
        {
            TV_2DVECTOR[] results = new TV_2DVECTOR[numPoints];
            double step = 1f/(numPoints - 1);
            double a = 1;

            for (int i = 0; i < results.Length; i++)
            {
                double b = 1 - a;

                double x = (a*Ax) + (b*Bx);
                double y = (a*Ay) + (b*By);

                results[i] = new TV_2DVECTOR((float) x, (float) y);
                a -= step;
            }
            return results;
        }

        // A & C (first and last) represent the Points and B is the control point.
        public static Vector3d[] Get3DQuadraticBezierCurvePoints(Vector3d A, Vector3d B, Vector3d C, uint numPoints)
        {
            return Get3DQuadraticBezierCurvePoints(A.x, A.y, A.z, B.x, B.y, B.z, C.x, C.y, C.z, numPoints);
        }

        public static Vector3d[] Get3DQuadraticBezierCurvePoints(double Ax, double Ay, double Az, double Bx, double By,
                                                                 double Bz, double Cx, double Cy, double Cz,
                                                                 uint numPoints)
        {
            Vector3d[] results = new Vector3d[numPoints];
            double step = 1f/(numPoints - 1);
            double a = 1;

            for (int i = 0; i < results.Length; i++)
            {
                double a_squared = a*a;
                double b = 1 - a;
                double b_squared = b*b;

                double x = (a_squared*Ax) + (2*a*b*Bx) + (b_squared*Cx);
                double y = (a_squared*Ay) + (2*a*b*By) + (b_squared*Cy);
                double z = (a_squared*Az) + (2*a*b*Bz) + (b_squared*Cz);

                results[i] = new Vector3d(x, y, z);
                a -= step;
            }
            return results;
        }

        public static TV_2DVECTOR[] Get2DQuadraticBezierCurvePoints(TV_2DVECTOR A, TV_2DVECTOR B, TV_2DVECTOR C,
                                                                    uint numPoints)
        {
            // Alternative version where you can use the overloaded scale * operator on Vector 
            //TV_2DVECTOR[] results = new TV_2DVECTOR[numPoints];
            //double step = 1f / numPoints;
            //double a = 1;

            //for (int i = 0; i < results.Length; i++)
            //{
            //    double b = 1 - a;
            //    results[i] = (a * a * A) + (2 * a * b * B) + (b * b * C);
            //    a -= step;
            //}
            //return results;

            return Get2DQuadraticBezierCurvePoints(A.x, A.y, B.x, B.y, C.x, C.y, numPoints);
        }

        public static TV_2DVECTOR[] Get2DQuadraticBezierCurvePoints(double Ax, double Ay, double Bx, double By,
                                                                    double Cx, double Cy, uint numPoints)
        {
            TV_2DVECTOR[] results = new TV_2DVECTOR[numPoints];
            double step = 1f/(numPoints - 1);
            double a = 1;

            for (int i = 0; i < results.Length; i++)
            {
                double a_squared = a*a;
                double b = 1 - a;
                double b_squared = b*b;

                double x = (a_squared*Ax) + (2*a*b*Bx) + (b_squared*Cx);
                double y = (a_squared*Ay) + (2*a*b*By) + (b_squared*Cy);

                results[i] = new TV_2DVECTOR((float) x, (float) y);
                a -= step;
            }
            return results;
        }

        // A & D (first and last) represent the Points and B & C are the control points.
        public static Vector3d[] Get3DCubicBezierCurvePoints(Vector3d A, Vector3d B, Vector3d C, Vector3d D,
                                                             uint numPoints)
        {
            return Get3DCubicBezierCurvePoints(A.x, A.y, A.z, B.x, B.y, B.z, C.x, C.y, C.z, D.x, D.y, D.z, numPoints);
        }

        public static Vector3d[] Get3DCubicBezierCurvePoints(double Ax, double Ay, double Az, double Bx, double By,
                                                             double Bz, double Cx, double Cy, double Cz, double Dx,
                                                             double Dy, double Dz, uint numPoints)
        {
            Vector3d[] results = new Vector3d[numPoints];

            double step = 1f/(numPoints - 1);
            double a = 1;
            System.Diagnostics.Debug.WriteLine(
                string.Format("Get3DCubicBezierCurvePoints A={0},{1},{2}  B={3},{4},{5} C={6},{7},{8} D={9},{10},{11}",
                              Ax, Ay, Az, Bx, By, Bz, Cx, Cy, Cz, Dx, Dy, Dz));
            for (int i = 0; i < results.Length; i++)
            {
                double a_squared = a*a;
                double a_cubed = a_squared*a;
                double b = 1 - a;
                double b_squared = b*b;
                double b_cubed = b_squared*b;

                double x = (a_cubed*Ax) + (3*a_squared*b*Bx) + (3*a*b_squared*Cx) + (b_cubed*Dx);
                double y = (a_cubed*Ay) + (3*a_squared*b*By) + (3*a*b_squared*Cy) + (b_cubed*Dy);
                double z = (a_cubed*Az) + (3*a_squared*b*Bz) + (3*a*b_squared*Cz) + (b_cubed*Dz);

                results[i] = new Vector3d(x, y, z);
                a -= step;
            }
            return results;
        }

        public static TV_2DVECTOR[] Get2DCubicBezierCurvePoints(TV_2DVECTOR A, TV_2DVECTOR B, TV_2DVECTOR C,
                                                                TV_2DVECTOR D, int numPoints)
        {
            return Get2DCubicBezierCurvePoints(A.x, A.y, B.x, B.y, C.x, C.y, D.x, D.y, numPoints);
        }

        public static TV_2DVECTOR[] Get2DCubicBezierCurvePoints(double Ax, double Ay, double Bx, double By, double Cx,
                                                                double Cy, double Dx, double Dy, int numPoints)
        {
            TV_2DVECTOR[] results = new TV_2DVECTOR[numPoints];

            double step = 1f/(numPoints - 1);
            double a = 1;

            for (int i = 0; i < results.Length; i++)
            {
                double a_squared = a*a;
                double a_cubed = a_squared*a;
                double b = 1 - a;
                double b_squared = b*b;
                double b_cubed = b_squared*b;


                double x = (a_cubed*Ax) + (3*a_squared*b*Bx) + (3*a*b_squared*Cx) + (b_cubed*Dx);
                double y = (a_cubed*Ay) + (3*a_squared*b*By) + (3*a*b_squared*Cy) + (b_cubed*Dy);

                results[i] = new TV_2DVECTOR((float) x, (float) y);

                a -= step;
            }
            return results;
        }
    }
}