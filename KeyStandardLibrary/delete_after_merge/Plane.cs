using MTV3D65;

namespace Core.Types
{
    // TV_PLANE only stores the normal of the plane.  This wrapper has one constructor that allows us to
    // retain the points used to create the plane from the 2nd and 3rd constructors.
    // NOTE: yeah its not that great because _points is null for all other cases...
    // in the future it might be possible to initialize 3 other points on the planes for all cases....
    // but for me right now, that's not useful so i wont bother...
    public class Plane // faster as a class than a struct
    {
        private Vector3d[] _points;
        private TV_PLANE _plane;
        private Vector3d _normal;
        private double _distance;

        public Plane(Vector3d point, Vector3d normal)
        {
            _plane = new TV_PLANE();
            Core._Core.Maths.TVPlaneFromPointNormal(ref _plane, point.ToTV3DVector(), normal.ToTV3DVector());
            _points = null;
            _normal = normal;
            _distance = Vector3d.DotProduct(_normal, point);
        }

        public Plane(TV_PLANE tvplane)
        {
            _normal = new Vector3d( tvplane.Normal);
            _distance = tvplane.Dist;
            _plane = tvplane;
            _points = null;
        }

        public Plane(double normalX, double normalY, double normalZ, double distance)
        {
            _distance = distance;
            _normal = new Vector3d(normalX, normalY, normalZ);
            _plane = new TV_PLANE(new TV_3DVECTOR((float) normalX, (float) normalY, (float) normalZ), (float) distance);
            _points = null;
        }

        // our retained points version of the constructor
        public Plane(Vector3d p1, Vector3d p2, Vector3d p3)
        {
            _points = new Vector3d[3];
            _plane = new TV_PLANE();
            _points[0] = p1;
            _points[1] = p2;
            _points[2] = p3;

            Vector3d edge1 = p2 - p1;
            Vector3d edge2 = p3 - p1;
            _normal = Vector3d.CrossProduct(edge1, edge2);
            _normal.Normalize();
            //_distance = - (Vector3d.DotProduct(_normal, p1)); // todo: negative result required?
            _distance = (Vector3d.DotProduct(_normal, p1));

            //Core._Core.Maths.TVPlaneFromPoints(ref _plane, _points[0].ToTV3DVector(), _points[1].ToTV3DVector(),
            //                                   _points[2].ToTV3DVector());
        }

        public Plane(Triangle tri)
            : this(tri.Points[0], tri.Points[1], tri.Points[2])
        {
        }

        

        //public TV_PLANE TVPlane
        //{
        //    get { return _plane; }
        //}

        public Vector3d[] Points
        {
            get { return _points; }
        }

        public Vector3d Normal
        {
             get { return _normal; } 
            //get {return new Vector3d(_plane.Normal);}
        }

        public void Translate(Vector3d translation)
        {
            //Plane tmp = new Plane(translation, Normal);
            //_plane.Dist += (float)tmp.Distance; //
           // _plane.Dist -=  Vector3d.DotProduct(Normal, translation);
            _distance -=  Vector3d.DotProduct(Normal, translation);
            if (_points != null)
            {
                for (int i = 0; i < _points.Length; i++)
                {
                    _points[i] -= translation;
                }
                System.Diagnostics.Debug.Assert(_plane.Dist == new Plane(_points[0], _points[1], _points[2]).Distance);
            }
        }

        public void InvertNormal()
        {
            _plane.Normal.x *= -1;
            _plane.Normal.y *= -1;
            _plane.Normal.z *= -1;
            _plane.Dist *= -1;
        }

        public double Distance //distance from the origin
        {
            get { return _distance; } 
             //get { return _plane.Dist; }
        }

        public bool Intersects(Ray r, ref double distance)
        {
            return Intersects(r, this, ref distance);
        }

        public bool Intersects(Ray r, ref double distance, ref Vector3d intersectionPoint)
        {
            return Intersects(r, this, ref distance, ref intersectionPoint);
        }

        public double DistanceToCoordinate (Vector3d coord)
        {
            return DistanceToPlane(coord, this);
        }

        public static double DistanceToPlane(Vector3d coord, Plane plane)
        {
            return Vector3d.DotProduct(coord, plane.Normal) + plane.Distance;
            //return (plane.Normal.x*coord.x + plane.Normal.y*coord.y + plane.Normal.z*coord.z) + plane.Distance;
            //return Core.Maths.VDotProduct(plane.Normal, coord) + plane.Distance; // calls to tv3d's vector math is sooo slow.
        }

        public static Plane Normalize(Plane plane)
        {
            double mag;
            Vector3d normal = Vector3d.Normalize(plane.Normal, out mag);
            return new Plane(normal.x, normal.y, normal.z, mag);
            //double mag = (double) (Math.Sqrt(plane.Normal.x*plane.Normal.x + plane.Normal.y*plane.Normal.y +
            //                               plane.Normal.z*plane.Normal.z));
            //return new PLANE(plane.Normal.x/mag, plane.Normal.y/mag, plane.Normal.z/mag, mag);
        }

        public static bool Intersects(Ray r, Plane p, ref double distance)
        {
            double d = -Vector3d.DotProduct(p.Normal, p.Points[0]);

            double numer = Vector3d.DotProduct(p.Normal, r.origin) + d;
            double denom = Vector3d.DotProduct(p.Normal, r.direction);

            if (denom == 0) // normal is orthogonal to vector, cant intersect
                return false;

            distance = -(numer/denom);
            return true;
        }

        public static bool Intersects(Ray r, Plane p, ref double distance, ref Vector3d intersectionPoint)
        {
            bool result = Intersects(r, p, ref distance);
            if (result) intersectionPoint = IntersectionPoint(r, distance);
            return result;
        }

        public static Vector3d IntersectionPoint(Ray r, double distance)
        {
            return r.origin + (r.direction*distance);
        }
    }
}