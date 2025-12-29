using System;
using System.Diagnostics;
using Core.Types;
using MTV3D65;

namespace Core.Types
{
    public class BoundingBox
    {
        private readonly Vector3d[] _parameters = new Vector3d[3];
        private bool _isdirty;
        private Vector3d[] _vertices;

        public static BoundingBox Parse(string s)
        {
            char[] delimiterChars = {' ', ','};
            string[] sToks = s.Split(delimiterChars);
            return new BoundingBox(new Vector3d(double.Parse(sToks[0]), double.Parse(sToks[1]), double.Parse(sToks[2])),
                                   new Vector3d(double.Parse(sToks[3]), double.Parse(sToks[4]), double.Parse(sToks[5])));
        }

        public override string ToString()
        {
            string s = string.Format("{0},{1},{2},{3},{4},{5}", Min.x, Min.y, Min.z, Max.x, Max.y, Max.z);
            return s;
        }

        public BoundingBox(Vector3d min, Vector3d max)
        {
            // todo: assert if width/height/depth of this box is > double.MaxValue 
            // todo: assert if box is inversed such that any component of max is smaller than any component of max
            Min = min;
            Max = max;
        }

        // construct a square bounding box who's ceter is at "position" and who's
        // center points on each face are "radius" distance from the center.
        public BoundingBox(Vector3d position, float radius)
            :
                this(new Vector3d(position.x - radius, position.y - radius, position.z - radius),
                     new Vector3d(position.x + radius, position.y + radius, position.z + radius))
        {
        }

        public BoundingBox(int centerX, int centerY, int centerZ, int indexX, int indexY,
                           int indexZ, Vector3d demensions)
        {
            Vector3d min, max;

            min.x = -(demensions.x/2) + (demensions.x*(indexX - centerX));
            max.x = min.x + demensions.x;

            min.y = -(demensions.y/2) + (demensions.y*(indexY - centerY));
            max.y = min.y + demensions.y;

            min.z = -(demensions.z/2) + (demensions.z*(indexZ - centerZ));
            max.z = min.z + demensions.z;
            
            Max = max;
            Min = min;
        }

        // we use * .5f because if we try to take width of these it returns infinity since double overlows
        public BoundingBox()
            : this(new Vector3d(float.MaxValue*.5f, float.MaxValue*.5f, float.MaxValue*.5f),
                   new Vector3d(float.MinValue*.5f, float.MinValue*.5f, float.MinValue)*.5f)
        {
        }

        //http://www.truevision3d.com/forums/tv3d_sdk_65/why_not_mesh_group_bounding_boxes-t17758.0.html
        // jviper's boundingbox for mesh groups
//Function GetBoundingBox(MshTV as TVMesh,intGroup as integer,Transformed as boolean) as Box3D
//    dim TVIO as TVInternalObjects
//    dim TmpMesh as Microsoft.DirectX.Direct3D.Mesh
//    dim Attr() as Mircrosoft.DirectX.Direct3D.AttributeRange
//    dim Vec as Vector3d
//    dim Ret as Box3D

//    TmpMsh = New Microsoft.DirectX.Direct3D.Mesh(TVIO.GetD3DMesh(MshTV.GetIndex))
//    Attr = TmpMsh.GetAttributeTable()
//    Ret.Min=new Vector3d(single.maxvalue,single.maxvalue,single.maxvalue)   
//    Ret.Max=new Vector3d(single.minvalue,single.minvalue,single.minvalue)   
//    For i as integer=Attr(intGroup).VertexStart to Attr(intGroup).VertexStart+Attr(intGroup).VertexCount-1
//        mshTV.GetVertex(i,Vec.x,Vec.y,Vec.z,0,0,0,0,0,0,0,0)
//        if Transformed then vec=tvvec3transformcoord(vec,mshTV.GetMatrix)
//        Ret.Min.x = min(Ret.Min.x,Vec.x)
//        Ret.Min.y = min(Ret.Min.y,Vec.y)
//        Ret.Min.z = min(Ret.Min.z,Vec.z)
//        Ret.Max.x = max(Ret.Max.x,Vec.x)
//        Ret.Max.y = max(Ret.Max.y,Vec.y)
//        Ret.Max.z = max(Ret.Max.z,Vec.z)
//    Next i
//    Return Ret
//End Function


        public Vector3d[] Vertices
        {
            get
            {
                if (_isdirty)
                {
                    _vertices = GetVertices(this);
                    _isdirty = false;
                }
                return _vertices;
            }
        }

        public Vector3d Min
        {
            get { return _parameters[0]; }
            set
            {
                _parameters[0] = value;
                _isdirty = true;
            }
        }

        public Vector3d Max
        {
            get { return _parameters[1]; }
            set
            {
                _parameters[1] = value;
                _isdirty = true;
            }
        }

        public double Height
        {
            get { return Max.y - Min.y; }
        }

        public double Width
        {
            get { return Max.x - Min.x; }
        }

        public double Depth
        {
            get { return Max.z - Min.z; }
        }

        public Vector3d Center
        {
            get { return new Vector3d(Min.x + (Width*.5d), Min.y + (Height*.5d), Min.z + (Depth*.5d)); }
        }

        /// <summary>Ray-box intersection using IEEE numerical properties to ensure 
        ///  that the test is both robust and efficient, as described in:
        /// 
        ///       Amy Williams, Steve Barrus, R. Keith Morley, and Peter Shirley
        ///       "An Efficient and Robust Ray-Box Intersection Algorithm"
        ///       Journal of graphics tools, 10(1):49-54, 2005
        ///        
        /// t0 and t1 accept a valid intersection interval.  In this way
        /// you can ignore positive hits that are too close or too far away
        /// from the desired area you're testing. (e.g. in a game with an avatar
        /// testing only the length by which the player traveled since the last frame
        /// is good enough for t1 and perhaps t0 being 0 or very close to it .001
        /// </summary>
        /// <param name="r"></param>
        /// <param name="t0">Start interval</param>
        /// <param name="t1">End interval</param>
        /// <returns></returns>
        public bool Intersects(Ray r, double t0, double t1)
        {
            double tmin = (_parameters[r.sign[0]].x - r.origin.x)*r.inv_direction.x;
            double tmax = (_parameters[1 - r.sign[0]].x - r.origin.x)*r.inv_direction.x;
            double tymin = (_parameters[r.sign[1]].y - r.origin.y)*r.inv_direction.y;
            double tymax = (_parameters[1 - r.sign[1]].y - r.origin.y)*r.inv_direction.y;
            if ((tmin > tymax) || (tymin > tmax))
                return false;
            if (tymin > tmin)
                tmin = tymin;
            if (tymax < tmax)
                tmax = tymax;
            double tzmin = (_parameters[r.sign[2]].z - r.origin.z)*r.inv_direction.z;
            double tzmax = (_parameters[1 - r.sign[2]].z - r.origin.z)*r.inv_direction.z;
            if ((tmin > tzmax) || (tzmin > tmax))
                return false;
            if (tzmin > tmin)
                tmin = tzmin;
            if (tzmax < tmax)
                tmax = tzmax;
            return ((tmin < t1) && (tmax > t0));
        }

        // also a good article on various collision detections
        // http://www.harveycartel.org/metanet/tutorials/tutorialA.html
        // a simple collision response so that two colliding boxes dont penetrate
//        // A this point we've already determined that the boxes intersect...

//float dist[4];

//dist[0] = box1.max.x - box2.min.x;
//dist[1] = box2.max.x - box1.min.x;
//dist[2] = box1.max.y - box2.min.y;
//dist[3] = box2.max.y - box1.min.y;

//size_t direction = std::distance(dist, std::min_element(dist, dist + 4));

//switch (direction) {
//    case 0: /* Move box1 along -x by dist[0] */ break;
//    case 1: /* Move box1 along +x by dist[1] */ break;
//    case 2: /* Move box1 along -y by dist[2] */ break;
//    case 3: /* Move box1 along +y by dist[3] */ break;
//}

        //returns if the passed inbox is contained in whole or in part with the existing box
        //NOTE: We must test both boxes against each other because if one box totally encompasses the other
        // then none of its corners will be in the bounds of the other, but the opposite WILL be true.
        // sadly, the worst case represents a maximum of 24 tests but we can usually bail out early 
        //todo: I think technically, if the boxes arent necessarily exact but have all 4 corners on the same
        // planes as two sides each then it might not register as being "contained" since its more accurately "on" the other.
        public bool Intersects(BoundingBox box)
        {
            Vector3d[] points = box.Vertices;
            // if any of the corners of the target box are contained in the src box, return true.
            for (int i = 0; i < points.Length; i++)
            {
                if (Contains(points[i]))
                    return true;
            }

            points = Vertices;
            for (int i = 0; i < box.Vertices.Length; i++)
            {
                if (box.Contains(points[i]))
                    return true;
            }

            // or in the unlikely event these boxes are identicle
            return box.Equals(this);
        }

        /// <summary>
        /// Returns true if the box passed in is entirely contained in the bounds of this box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool Contains(BoundingBox box)
        {
            return Contains(box.Vertices);
        }

        // returns true if _all_ points are contained
        public bool Contains(Vector3d[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (!Contains(points[i])) return false;
            }
            return true;
        }

        public bool Contains(Vector3d point)
        {
            return (point.x >= Min.x && point.x <= Max.x &&
                    point.y >= Min.y && point.y <= Max.y &&
                    point.z >= Min.z && point.z <= Max.z);
        }

        public static BoundingBox Transform1 (BoundingBox box, Matrix m)
        {
            // If we're empty, then bail
            

            // Start with the translation portion
            Vector3d min, max;
            min = max = new Vector3d( m.M41 , m.M42 , m.M43);

            // Examine each of the 9 matrix elements
            // and compute the new AABB

            if (m.M11 > 0.0f)
            {
                min.x += m.M11 * box.Min.x; max.x += m.M11 * box.Max.x;
            }
            else
            {
                min.x += m.M11 * box.Max.x; max.x += m.M11 * box.Min.x;
            }

            if (m.M12 > 0.0f)
            {
                min.y += m.M12 * box.Min.x; max.y += m.M12 * box.Max.x;
            }
            else
            {
                min.y += m.M12 * box.Max.x; max.y += m.M12 * box.Min.x;
            }

            if (m.M13 > 0.0f)
            {
                min.z += m.M13 * box.Min.x; max.z += m.M13 * box.Max.x;
            }
            else
            {
                min.z += m.M13 * box.Max.x; max.z += m.M13 * box.Min.x;
            }

            if (m.M21 > 0.0f)
            {
                min.x += m.M21 * box.Min.y; max.x += m.M21 * box.Max.y;
            }
            else
            {
                min.x += m.M21 * box.Max.y; max.x += m.M21 * box.Min.y;
            }

            if (m.M22 > 0.0f)
            {
                min.y += m.M22 * box.Min.y; max.y += m.M22 * box.Max.y;
            }
            else
            {
                min.y += m.M22 * box.Max.y; max.y += m.M22 * box.Min.y;
            }

            if (m.M23 > 0.0f)
            {
                min.z += m.M23 * box.Min.y; max.z += m.M23 * box.Max.y;
            }
            else
            {
                min.z += m.M23 * box.Max.y; max.z += m.M23 * box.Min.y;
            }

            if (m.M31 > 0.0f)
            {
                min.x += m.M31 * box.Min.z; max.x += m.M31 * box.Max.z;
            }
            else
            {
                min.x += m.M31 * box.Max.z; max.x += m.M31 * box.Min.z;
            }

            if (m.M32 > 0.0f)
            {
                min.y += m.M32 * box.Min.z; max.y += m.M32 * box.Max.z;
            }
            else
            {
                min.y += m.M32 * box.Max.z; max.y += m.M32 * box.Min.z;
            }

            if (m.M33 > 0.0f)
            {
                min.z += m.M33 * box.Min.z; max.z += m.M33 * box.Max.z;
            }
            else
            {
                min.z += m.M33 * box.Max.z; max.z += m.M33 * box.Min.z;
            }

            return new BoundingBox(min, max);
        }
        // This should only be used when the origin of the box and the origin of the mesh are the same.
        // In other words, if the mesh's center is in the center of the mesh.  Remember that often times
        // actors and other meshes will have their center.Y at the foot of the mesh and rotation occur
        // about that position.
        // A faster looking method simply takes the extents and the center 
        // transforms the center and then creates a new box 
        public static BoundingBox Transform2(BoundingBox src, Matrix xform)
        {
            // get center and transform
            Vector3d c = (src.Min + src.Max) * 0.5f;
            c = Vector3d.TransformCoord(c, xform);

            // get extent and transform
            Vector3d e = (src.Max - src.Min) * 0.5f;
            Matrix m = new Matrix();

            // working just with scaling and rotation
            m.M11 = Math.Abs(xform.M11);
            m.M12 = Math.Abs(xform.M12);
            m.M13 = Math.Abs(xform.M13);
            m.M14 =  0.0f;
            
            m.M21 =      Math.Abs(xform.M21);
            m.M22 = Math.Abs(xform.M22);
            m.M23 = Math.Abs(xform.M23);
            m.M24 = 0.0f;
            
            m.M31 =Math.Abs(xform.M31);
            m.M32 =  Math.Abs(xform.M32);
            m.M33 = Math.Abs(xform.M33);
            m.M34 = 0.0f;
            
            m.M41 = 0.0f;
            m.M42 = 0.0f;
            m.M43 = 0.0f;
            m.M44 = 1.0f;
            
            // use transform normal to 
            e = Vector3d.TransformNormal(e, m);

            // convert back to bounding box representation
            return new BoundingBox(c - e, c + e);
        }

        public static BoundingBox Transform(BoundingBox box, Matrix matrix)
        {
            if (box == null) return null;
            // when transforming a local box to world, you cannot (unfortunately) simply
            // transform the min and max coords.  You have to transform all 8 and then take the min,max of those.
            //Vector3d worldMax = new Vector3d(double.MinValue , double.MinValue , double.MinValue  );
            //Vector3d worldMin = new Vector3d(double.MaxValue, double.MaxValue, double.MaxValue);
            Vector3d worldMax, worldMin;
            worldMax.x = double.MinValue;
            worldMax.y = double.MinValue;
            worldMax.z = double.MinValue;
            worldMin.x = double.MaxValue;
            worldMin.y = double.MaxValue;
            worldMin.z = double.MaxValue;
            
            Vector3d v2;

            foreach (Vector3d v in box.Vertices)
            {
                v2 = Vector3d.TransformCoord(v, matrix);
                worldMax.x = Math.Max(worldMax.x, v2.x);
                worldMax.y = Math.Max(worldMax.y, v2.y);
                worldMax.z = Math.Max(worldMax.z, v2.z);
                worldMin.x = Math.Min(worldMin.x, v2.x);
                worldMin.y = Math.Min(worldMin.y, v2.y);
                worldMin.z = Math.Min(worldMin.z, v2.z);
            }
            return new BoundingBox(worldMin, worldMax);
        }

        ///// <summary>
        ///// performs intersection testing based on the separating axis theorem. As soon as a separating axis is found, the function returns.
        ///// </summary>
        ///// <param name="box"></param>
        ///// <returns></returns>
        //public bool Intersects2 (BoundingBox box)
        //{

        //    // A = normals of the faces that touch the minimum vector
        //    // B = normals of the faces that touch the maximum vector
        //   // Vector3d[]    CA =  A[3];
        //    Vector3d      CB = B[3];
        //    Vector3d      T (CB - CA);

        //    double         rA;
        //    double         rB;
        //    double         rT;
        //    Vector3d        L;

        //    for (int i = 0; i < 3; ++i)
        //    {
        //        L = A[i];

        //        rA = Math.Abs(A[0].VDot(L)) + Math.Abs(A[1].VDot(L)) + Math.Abs(A[2].VDot(L));
        //        rB = Math.Abs(B[0].VDot(L)) + Math.Abs(B[1].VDot(L)) + Math.Abs(B[2].VDot(L));
        //        rT = Math.Abs(T.VDot(L));

        //        if (rT > rA + rB)
        //            return false;

        //        L = B[i];
        //        rA = Math.Abs(A[0].VDot(L)) + Math.Abs(A[1].VDot(L)) + Math.Abs(A[2].VDot(L));
        //        rB = Math.Abs(B[0].VDot(L)) + Math.Abs(B[1].VDot(L)) + Math.Abs(B[2].VDot(L));
        //        rT = Math.Abs(T.VDot(L));

        //        if (rT > rA + rB)
        //            return false;
        //    }

        //    // and now for the cross product axes
        //    for (int i = 0; i < 3; ++i)
        //        for (int j = 0; j < 3; ++j)
        //        {
        //            L = A[i].VCross(B[j]);
        //            rA = Math.Abs(A[0].VDot(L)) + Math.Abs(A[1].VDot(L)) + Math.Abs(A[2].VDot(L));
        //            rB = Math.Abs(B[0].VDot(L)) + Math.Abs(B[1].VDot(L)) + Math.Abs(B[2].VDot(L));
        //            rT = Math.Abs(T.VDot(L));

        //            if (rT > rA + rB)
        //                return false;
        //        }
        //    return true;
        //}

        //When combining bounding volumes, an uninitialized box will have 0,0,0 for both min & max vectors
        //This is usually unintional so make sure your bounding boxes are initialized with proper min/max vectors.
        public static BoundingBox Combine(BoundingBox b1, BoundingBox b2)
        {
            if ((b1 == null) && (b2 == null)) throw new ArgumentNullException();

            if (b1 == null) return b2;
            if (b2 == null) return b1;

            Vector3d min, max;
            min.x = Math.Min(b1.Min.x, b2.Min.x);
            min.y = Math.Min(b1.Min.y, b2.Min.y);
            min.z = Math.Min(b1.Min.z, b2.Min.z);

            max.x = Math.Max(b1.Max.x, b2.Max.x);
            max.y = Math.Max(b1.Max.y, b2.Max.y);
            max.z = Math.Max(b1.Max.z, b2.Max.z);
            return new BoundingBox(min, max);
        }

        //todo: I really should make these regular NON static methods
        public static Vector3d[,] GetQuadFaceVertices(BoundingBox box)
        {
            Vector3d[,] vertices = new Vector3d[6,4];
            // NOTE: for AABB the first subscript 0 to 5 indices correspond with 
            //the CUBEMAP_FACE enumeration such that
            // face 0 is the PositiveX = 0
            // face 1 is the NegativeX 
            // face 2 is the PositiveY 
            // face 3 is the NegativeY
            // face 4 is the PositiveZ
            // face 5 is the NegativeZ

            // the top quad (PositiveY)
            vertices[2, 0] = new Vector3d(box.Min.x, box.Max.y, box.Min.z);
            vertices[2, 1] = new Vector3d(box.Max.x, box.Max.y, box.Min.z);
            vertices[2, 2] = new Vector3d(box.Min.x, box.Max.y, box.Max.z);
            vertices[2, 3] = new Vector3d(box.Max.x, box.Max.y, box.Max.z);

            // the bottom quad (NegativeY)
            vertices[3, 0] = new Vector3d(box.Min.x, box.Min.y, box.Min.z);
            vertices[3, 1] = new Vector3d(box.Max.x, box.Min.y, box.Min.z);
            vertices[3, 2] = new Vector3d(box.Min.x, box.Min.y, box.Max.z);
            vertices[3, 3] = new Vector3d(box.Max.x, box.Min.y, box.Max.z);


            // the side quads consist of existing top and bottom vertices 
            // PostiveX
            vertices[0, 0] = vertices[3, 1];
            vertices[0, 1] = vertices[3, 3];
            vertices[0, 2] = vertices[2, 3];
            vertices[0, 3] = vertices[2, 1];

            // NegativeX
            vertices[1, 0] = vertices[3, 2];
            vertices[1, 1] = vertices[3, 0];
            vertices[1, 2] = vertices[2, 0];
            vertices[1, 3] = vertices[2, 2];

            // PositiveZ
            vertices[4, 0] = vertices[3, 3];
            vertices[4, 1] = vertices[3, 2];
            vertices[4, 2] = vertices[2, 2];
            vertices[4, 3] = vertices[2, 3];

            // NegativeZ
            vertices[5, 0] = vertices[3, 0];
            vertices[5, 1] = vertices[3, 1];
            vertices[5, 2] = vertices[2, 1];
            vertices[5, 3] = vertices[2, 0];
            return vertices;
        }

        private static Vector3d[] GetVertices(BoundingBox box)
        {
            Vector3d[] vertices = new Vector3d[8];

            // the bottom 4 vertices form a square
            //vertices[0] = new Vector3d(box.Min.x, box.Min.y, box.Min.z);
            //vertices[1] = new Vector3d(box.Max.x, box.Min.y, box.Min.z);
            //vertices[2] = new Vector3d(box.Min.x, box.Min.y, box.Max.z);
            //vertices[3] = new Vector3d(box.Max.x, box.Min.y, box.Max.z);

            // the top 4 vertices form a square
            //vertices[4] = new Vector3d(box.Min.x, box.Max.y, box.Min.z);
            //vertices[5] = new Vector3d(box.Max.x, box.Max.y, box.Min.z);
            //vertices[6] = new Vector3d(box.Min.x, box.Max.y, box.Max.z);
            //vertices[7] = new Vector3d(box.Max.x, box.Max.y, box.Max.z);

            vertices[0].x = box.Min.x;
            vertices[0].y = box.Min.y;
            vertices[0].z = box.Min.z;
            vertices[1].x = box.Max.x;
            vertices[1].y = box.Min.y;
            vertices[1].z = box.Min.z;
            vertices[2].x = box.Min.x;
            vertices[2].y = box.Min.y;
            vertices[2].z = box.Max.z;
            vertices[3].x = box.Max.x;
            vertices[3].y = box.Min.y;
            vertices[3].z = box.Max.z;
            vertices[4].x = box.Min.x;
            vertices[4].y = box.Max.y;
            vertices[4].z = box.Min.z;
            vertices[5].x = box.Max.x;
            vertices[5].y = box.Max.y;
            vertices[5].z = box.Min.z;
            vertices[6].x = box.Min.x;
            vertices[6].y = box.Max.y;
            vertices[6].z = box.Max.z;
            vertices[7].x = box.Max.x;
            vertices[7].y = box.Max.y;
            vertices[7].z = box.Max.z;
            return vertices;
        }

        /// <summary>
        /// Constructs the 12 edges of the bouding box
        /// </summary>
        public static Line3d[] GetEdges(BoundingBox box)
        {
            Vector3d[] vertices = box.Vertices;
            Line3d[] edges = new Line3d[12];
            // X-aligned lines on both sides, both heights
            edges[0] = new Line3d(vertices[0], vertices[1]);
            edges[1] = new Line3d(vertices[2], vertices[3]);
            edges[2] = new Line3d(vertices[4], vertices[5]);
            edges[3] = new Line3d(vertices[6], vertices[7]);

            // Y-aligned lines at each corner
            edges[4] = new Line3d(vertices[0], vertices[4]);
            edges[5] = new Line3d(vertices[2], vertices[6]);
            edges[6] = new Line3d(vertices[1], vertices[5]);
            edges[7] = new Line3d(vertices[3], vertices[7]);

            // Z-aligned lines on both sides, both heights
            edges[8] = new Line3d(vertices[0], vertices[2]);
            edges[9] = new Line3d(vertices[1], vertices[3]);
            edges[10] = new Line3d(vertices[4], vertices[6]);
            edges[11] = new Line3d(vertices[5], vertices[7]);

            return edges;
        }

        public static Triangle[] GetFaces(BoundingBox box)
        {
            // construct 12 triangles from our bounding box vertices.  Remember that winding order
            // for d3d (and hence tv3d) by default for outward facing polygons is clockwise.
            Triangle[] tris = new Triangle[12];
            Vector3d[] v = box.Vertices;

            // top 2 faces
            tris[0] = new Triangle(v[0], v[3], v[1]);
            tris[1] = new Triangle(v[0], v[2], v[3]);

            // bottom 2 faces
            tris[10] = new Triangle(v[4], v[7], v[6]);
            tris[11] = new Triangle(v[4], v[5], v[7]);

            // the side faces
            tris[2] = new Triangle(v[0], v[1], v[4]);
            tris[3] = new Triangle(v[1], v[5], v[4]);

            tris[4] = new Triangle(v[2], v[0], v[6]);
            tris[5] = new Triangle(v[0], v[4], v[6]);

            tris[6] = new Triangle(v[3], v[2], v[6]);
            tris[7] = new Triangle(v[3], v[6], v[7]);

            tris[8] = new Triangle(v[1], v[3], v[7]);
            tris[9] = new Triangle(v[7], v[5], v[1]);
            return tris;
        }

        // one good thing is this code can be used for our imposter code too
        // find the minimum and maximum distance needed to enclose that box on the supplied axis.
        public static void GetProjectedDistances(BoundingBox box, Vector3d OnVector, out double NearDistance,
                                                 out double FarDistance)
        {
            double FarAssociatedNear = double.MinValue;
            NearDistance = double.MaxValue;
            FarDistance = double.MinValue;
            const double DEPTH_BIAS = 1f;

            Line3d[] edges = GetEdges(box);
            Trace.Assert(edges.Length == 12);
            foreach (Line3d e in edges)
            {
                // the projected offset gives us the seperation from this edge vertex and the supplied axis vector
                double ProjectedOffset = Vector3d.DotProduct(e.Point[0], OnVector);
                // subtract the unscaled direction vector from our supplied axis and compute the dot product
                // this dot product gives us the scalar projection of this direction vector onto our OnVector
                double ProjectedVector =
                    Vector3d.DotProduct(e.Point[1] - e.Point[0], OnVector);

                double CurrentNear = ProjectedOffset;
                if (ProjectedVector < 0)
                    CurrentNear += ProjectedVector;

                NearDistance = Math.Min(NearDistance, CurrentNear);
                double CurrentFar = ProjectedVector*Math.Sign(ProjectedVector);

                if (CurrentNear + CurrentFar > FarAssociatedNear + FarDistance)
                {
                    FarAssociatedNear = CurrentNear;
                    FarDistance = CurrentFar;
                }
            }

            FarDistance += FarAssociatedNear - NearDistance;
            FarDistance += DEPTH_BIAS;
            NearDistance -= DEPTH_BIAS;
        }

        public void Draw (Matrix transform, CONST_TV_COLORKEY color)
        {
            DebugDraw.DrawBox(Transform(this, transform), color);
        }

        public void Draw (Vector3d translation, CONST_TV_COLORKEY color)
        {
            Vector3d newMin = Min + translation;
            Vector3d newMax = Max + translation;
            DebugDraw.DrawBox( new BoundingBox( newMin, newMax), color);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        public void Draw(CONST_TV_COLORKEY color)
        {
            //Draws the bounding box with Quad faces, not triangles.
            DebugDraw.DrawBox(this, color);
        }
    }
}