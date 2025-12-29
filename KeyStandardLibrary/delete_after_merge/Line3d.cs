using Core.Types;
using MTV3D65;

namespace Core
{
    public struct Line3d
    {
        private Vector3d[] _p;

        public Line3d(Vector3d v1, Vector3d v2)
        {
            _p = new Vector3d[2];
            _p[0] = v1;
            _p[1] = v2;
        }

        public Line3d (float x1, float y1, float z1, float x2, float y2, float z2) : this(new Vector3d( x1,y1,z1), new Vector3d( x2,y2,z2))
        {
            
        }
        public Vector3d[] Point
        {
            get { return _p; }
        }

        // Overloaded the == operator in EDGE to return as true any two edges that have same endpoints regardless of order.
        // i.e.  AB=AB && AB = BA
        public static bool operator ==(Line3d e1, Line3d e2)
        {
            return (e1.Point[0].x == e2.Point[0].x
                    && e1.Point[0].y == e2.Point[0].y
                    && e1.Point[0].z == e2.Point[0].z
                    && e1.Point[1].x == e2.Point[1].x
                    && e1.Point[1].y == e2.Point[1].y
                    && e1.Point[1].z == e2.Point[1].z)
                   ||
                   (e1.Point[0].x == e2.Point[1].x
                    && e1.Point[0].y == e2.Point[1].y
                    && e1.Point[0].z == e2.Point[1].z
                    && e1.Point[1].x == e2.Point[0].x
                    && e1.Point[1].y == e2.Point[0].y
                    && e1.Point[1].z == e2.Point[0].z);
        }

        public static bool operator !=(Line3d e1, Line3d e2)
        {
            return !(e1 == e2);
        }

        public override bool Equals(object obj)
        {
            if (obj is Line3d)
                return this == (Line3d) obj;
            else
                return base.Equals(obj);
        }
    }
}