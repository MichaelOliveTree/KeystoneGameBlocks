using System;
using Core.Types;
using MTV3D65;

namespace Core.Types
{
    public class Triangle : Polygon
    {
        public Triangle(Vector3d p1, Vector3d p2, Vector3d p3)
        {
            _points = new Vector3d[3];
            _points[0] = p1;
            _points[1] = p2;
            _points[2] = p3;

            _normal = FaceNormal(_points[0], _points[1], _points[2]);

            // bad triangles where any two points are the same could result in exception?
            if (_points[0].Equals(_points[1]) || _points[0].Equals(_points[2]) || _points[1].Equals(_points[2]))
            {
                throw new ArgumentException("Triangle cannot have two identicle vertices.");
            }
            _indices = null;
        }

        public Triangle(TVMesh mesh, int index1, int index2, int index3)
        {
            _indices = new int[3];
            _points = new Vector3d[3];

            double tu, tv, tu2, tv2;
            tu = tv = tv2 = tu2 = 0;
            int color = 0;

            _normal = new Vector3d();
            _indices[0] = index1;
            _indices[1] = index2;
            _indices[2] = index3;

            Vector3d p = new Vector3d();
            // todo: temporarily commented out while i finish this TV_3DVECTOR to Vector3d conversion. Triangle should probably use Vector3f same with
            //       EDGE and LINE and such.  
            //for (int i = 0; i < 3; i++)
            //{
            //    mesh.GetVertex(_indices[i], ref _points[i].x, ref _points[i].y, ref _points[i].z, ref _normal.x,
            //                   ref _normal.y, ref _normal.z, ref tu, ref tv, ref tu2, ref tv2, ref color);
            //    Core._Core.Maths.TVVec3TransformCoord(ref p, _points[i], mesh.GetMatrix());
            //    _points[i] = p;
            //}
            _normal = FaceNormal(_points[0], _points[1], _points[2]);

            // bad triangles where any two points are the same could result in exception?
            if (_points[0].Equals(_points[1]) || _points[0].Equals(_points[2]) || _points[1].Equals(_points[2]))
            {
                throw new ArgumentException("Triangle cannot have two identicle vertices.");
            }
        }

        public Vector3d Center
        {
            get { return getCenter(Points[0], Points[1], Points[2]); }
        }

        public static Vector3d getCenter(Vector3d v1, Vector3d v2, Vector3d v3)
        {
            return new Vector3d((v1.x + v2.x + v3.x)/3, (v1.y + v2.y + v3.y)/3, (v1.z + v2.z + v3.z)/3);
        }
    }
}