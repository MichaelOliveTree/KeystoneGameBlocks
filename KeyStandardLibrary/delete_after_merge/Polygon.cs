using System;

namespace Core.Types
{
    public class Polygon
    {
        protected int[] _indices;
        protected Vector3d[] _points;
        protected Vector3d _normal;

        public Vector3d Normal
        {
            get { return _normal; }
        }

        public int[] Indices
        {
            get { return _indices; }
        }

        public Vector3d[] Points
        {
            get { return _points; }
        }

        public Plane GetPlane()
        {
            return new Plane(_points[0], _points[1], _points[2]);
        }

        public static Vector3d FaceNormal(Vector3d v1, Vector3d v2, Vector3d v3)
        {
            //1. The two edges chosen must not be parallel, i.e. the angle between the edges must not be 0 or 180 degrees. 
            //   The normal will be more accurate if the angle between the lines is closer to 90 degrees. 
            //2. The length of the edges must be non-zero and the normal will be more accurate if the length is high compared 
            //   with the accuracy of the coordinates. 
            //3. If the angle is concave then the direction of the normal needs to be reversed.

            Vector3d r1, r2;

            r1 = v1 - v2;
            r2 = v2 - v3;
            return Vector3d.Normalize(Vector3d.CrossProduct(r1, r2));
        }

        public bool Intersects(Vector3d start, Vector3d end)
        {
            double distance = 0;
            Ray r = new Ray(start, end);
            Plane p = GetPlane();
            Vector3d intersectionPoint = new Vector3d();
            bool result = (p.Intersects(r, ref distance, ref intersectionPoint));
            if (!result) return false;
            return ContainsPoint(intersectionPoint);
        }

        public bool ContainsPoint(Vector3d rayPlaneIntersectionPoint)
        {
            return ContainsPoint(rayPlaneIntersectionPoint, _points, _points.Length);
        }

        // Tests if a point on the plane of a polygon is within the actual bounds of the point
        // http://www.realtimerendering.com/intersections.html
        public static bool ContainsPoint(Vector3d rayPlaneIntersectionPoint, Vector3d[] polygonVertices,
                                         int verticeCount)
        {
            const double MATCH_FACTOR = 0.99; // Used to cover up the error in floating point
            double angle = 0.0; // Initialize the angle
            Vector3d vA, vB; // Create temp vectors
            for (int i = 0; i < verticeCount; i++) // Go in a circle to each vertex and get the angle between
            {
                vA = polygonVertices[i] - rayPlaneIntersectionPoint;
                    // Subtract the intersection point from thecurrent vertex
                vB = polygonVertices[(i + 1)%verticeCount] - rayPlaneIntersectionPoint;
                    // Subtract the point from the next vertex
                angle += Vector3d.AngleBetweenVectors(vA, vB);
                    // Find the angle between the 2 vectors and add them all up as we go along
            }
            if (angle >= (MATCH_FACTOR*(2.0*Math.PI))) // If the angle is greater than 2 PI, (360 degrees)
                return true; // The point is inside of the polygon
            return false; // If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }
    }
}