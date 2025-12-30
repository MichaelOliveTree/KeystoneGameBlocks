#define CACHE_VERTICES

using System;
using System.Diagnostics;
using Keystone.Primitives;

namespace Keystone.Types
{
    public struct BoundingBox
    {
        private static Vector3d MIN_INIT = new Vector3d(float.MaxValue * .5f, float.MaxValue * .5f, float.MaxValue * .5f);
        private static Vector3d MAX_INIT = new Vector3d(float.MinValue * .5f, float.MinValue * .5f, float.MinValue * .5f);

        //private Vector3d[] _parameters;
        private Vector3d _min;
        private Vector3d _max;

        public static BoundingBox Parse(string delimitedString)
        {
            if (string.IsNullOrEmpty(delimitedString)) throw new ArgumentNullException();

            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
            string[] values = delimitedString.Split(delimiterChars);

            Vector3d min, max;
            min.x = double.Parse(values[0]);
            min.y = double.Parse(values[1]);
            min.z = double.Parse(values[2]);

            max.x = double.Parse(values[3]);
            max.y = double.Parse(values[4]);
            max.z = double.Parse(values[5]);

            return new BoundingBox(min, max);
        }

        public static BoundingBox Initialized()
        {
            BoundingBox box;
            box._min = MIN_INIT;
            box._max = MAX_INIT;

            return box;
        }

        public bool IsNullOrEmpty()
        {
            return (_min.x == 0d &&
                    _min.y == 0d &&
                    _min.z == 0d &&
                    _max.x == 0d &&
                    _max.y == 0d &&
                     _max.z == 0d);
        }

        public static BoundingBox FromBoundingRect(BoundingRect rect)
        {
            Vector3d min, max;
            min.x = rect.Min.x;
            min.y = float.MinValue;
            min.z = rect.Min.y;

            max.x = rect.Max.x;
            max.y = float.MaxValue;
            max.z = rect.Max.y;
            BoundingBox result = new BoundingBox(min, max);

            return result;
        }
       
        public override string ToString()
        {
            string delimiter = keymath.ParseHelper.English.XMLAttributeDelimiter;
            string s = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}", Min.x, delimiter,
                                                                Min.y, delimiter,
                                                                Min.z, delimiter,
                                                                Max.x, delimiter,
                                                                Max.y, delimiter,
                                                                Max.z);
            return s;
        }

        public BoundingBox(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Vector3d min, max;
            min.x = minX;
            min.y = minY;
            min.z = minZ;
            max.x = maxX;
            max.y = maxY;
            max.z = maxZ;

            _min = min;
            _max = max;
        }

        public BoundingBox(Vector3d min, Vector3d max)
        {
            // TODO: assert if width/height/depth of this box is > double.MaxValue 
            // TODO: assert if box is inversed such that any component of max is smaller than any component of max
            _min = min;
            _max = max;
        }


        ///<summary>
        /// construct a square bounding box who's ceter is at "position" and who's
        /// center points on each face are "radius" distance from the center.
        /// This type of box will always full encompass a sphere of the same radius.  
        ///</summary>
        public BoundingBox(Vector3d position, double radius)
            :
                this(position.x - radius, position.y - radius, position.z - radius,
                     position.x + radius, position.y + radius, position.z + radius)
        {
        }

        ///<summary>
        /// construct a square bounding box who's ceter is at "position" and who's
        /// center points on each face are "radius" distance from the center.
        /// This type of box will always full encompass a sphere of the same radius.  
        ///</summary>
        public BoundingBox(Vector3d position, float radius)
            :
                this(position.x - radius, position.y - radius, position.z - radius,
                     position.x + radius, position.y + radius, position.z + radius)
        {
        }
        // we use * .5f because if we try to take width of these it returns infinity since double overlows
        //        public BoundingBox()
        //            : this(MIN_INIT, MAX_INIT)
        //        {
        //        }

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
                return GetVertices(this);
            }
        }

        public Vector3d Min
        {
            get { return _min; }
            set
            {
                _min = value;
            }
        }

        public Vector3d Max
        {
            get { return _max; }
            set
            {
                _max = value;
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

        /// <summary>
        /// True Radius which takes into account half the diagonal length from one corner to it's opposite.
        /// </summary>
        public double Radius
        {
            get
            {
                return Diameter * .5;
            }
        }

        public double RadiusSquared
        {
            get
            {
                double radius = Diameter * .5;
                return radius * radius;
            }
        }

        /// <summary>
        /// True diameter which takes into account the diagonal length from one corner to it's opposite.
		/// TODO: this is not actually diameter which is a diagonal line, this is the max axis length
        /// </summary>
        public double Diameter
        {
            get
            {
                double axisLength = _max.x - _min.x;
                axisLength = Math.Max(axisLength, _max.y - _min.y);
                return Math.Max(axisLength, _max.z - _min.z);


                //return (Max - Min).Length;
            }
        }

        public Vector3d Center
        {
            get
            {
                Vector3d result;
                result.x = Min.x + (Width * 0.5d);
                result.y = Min.y + (Height * 0.5d);
                result.z = Min.z + (Depth * 0.5d);
                return result;
            }
        }

        public void Translate(double translationX, double translationY, double translationZ)
        {
            _min.x += translationX;
            _min.y += translationY;
            _min.z += translationZ;

            _max.x += translationX;
            _max.y += translationY;
            _max.z += translationZ;
        }

        public void Translate(Vector3d translation)
        {
            Min += translation;
            Max += translation;
        }

        public void Scale(Vector3d scale)
        {
            Min *= scale;
            Max *= scale;
        }

        public static BoundingBox Scale(BoundingBox box, Vector3d scale)
        {
            Vector3d min = box.Min * scale;
            Vector3d max = box.Max * scale;
            return new BoundingBox(min, max);
        }

        public static BoundingBox Transform1(BoundingBox box, Matrix m)
        {
            // If we're empty, then bail


            // Start with the translation portion
            Vector3d min, max;
            min = max = new Vector3d(m.M41, m.M42, m.M43);

            // Examine each of the 9 matrix elements
            // and compute the new AABB

            if (m.M11 > 0.0d)
            {
                min.x += m.M11 * box.Min.x; max.x += m.M11 * box.Max.x;
            }
            else
            {
                min.x += m.M11 * box.Max.x; max.x += m.M11 * box.Min.x;
            }

            if (m.M12 > 0.0d)
            {
                min.y += m.M12 * box.Min.x; max.y += m.M12 * box.Max.x;
            }
            else
            {
                min.y += m.M12 * box.Max.x; max.y += m.M12 * box.Min.x;
            }

            if (m.M13 > 0.0d)
            {
                min.z += m.M13 * box.Min.x; max.z += m.M13 * box.Max.x;
            }
            else
            {
                min.z += m.M13 * box.Max.x; max.z += m.M13 * box.Min.x;
            }

            if (m.M21 > 0.0d)
            {
                min.x += m.M21 * box.Min.y; max.x += m.M21 * box.Max.y;
            }
            else
            {
                min.x += m.M21 * box.Max.y; max.x += m.M21 * box.Min.y;
            }

            if (m.M22 > 0.0d)
            {
                min.y += m.M22 * box.Min.y; max.y += m.M22 * box.Max.y;
            }
            else
            {
                min.y += m.M22 * box.Max.y; max.y += m.M22 * box.Min.y;
            }

            if (m.M23 > 0.0d)
            {
                min.z += m.M23 * box.Min.y; max.z += m.M23 * box.Max.y;
            }
            else
            {
                min.z += m.M23 * box.Max.y; max.z += m.M23 * box.Min.y;
            }

            if (m.M31 > 0.0d)
            {
                min.x += m.M31 * box.Min.z; max.x += m.M31 * box.Max.z;
            }
            else
            {
                min.x += m.M31 * box.Max.z; max.x += m.M31 * box.Min.z;
            }

            if (m.M32 > 0.0d)
            {
                min.y += m.M32 * box.Min.z; max.y += m.M32 * box.Max.z;
            }
            else
            {
                min.y += m.M32 * box.Max.z; max.y += m.M32 * box.Min.z;
            }

            if (m.M33 > 0.0d)
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
            m.M14 = 0.0f;

            m.M21 = Math.Abs(xform.M21);
            m.M22 = Math.Abs(xform.M22);
            m.M23 = Math.Abs(xform.M23);
            m.M24 = 0.0f;

            m.M31 = Math.Abs(xform.M31);
            m.M32 = Math.Abs(xform.M32);
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
            // do not attempt to transform a box that is not initialized.  Instead
            // return the original box
            if (box.Min == MIN_INIT && box.Max == MAX_INIT)
                return box;
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

            Vector3d[] verts = box.Vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                v2 = Vector3d.TransformCoord(verts[i], matrix);
                worldMax.x = Math.Max(worldMax.x, v2.x);
                worldMax.y = Math.Max(worldMax.y, v2.y);
                worldMax.z = Math.Max(worldMax.z, v2.z);
                worldMin.x = Math.Min(worldMin.x, v2.x);
                worldMin.y = Math.Min(worldMin.y, v2.y);
                worldMin.z = Math.Min(worldMin.z, v2.z);
            }

            //          // TODO: http://dev.theomader.com/transform-bounding-boxes/ ?
            //          var xa = m.Right * boundingBox.Min.X;
            //		    var xb = m.Right * boundingBox.Max.X;
            //		 
            //		    var ya = m.Up * boundingBox.Min.Y;
            //		    var yb = m.Up * boundingBox.Max.Y;
            //		 
            //		    var za = m.Backward * boundingBox.Min.Z;
            //		    var zb = m.Backward * boundingBox.Max.Z;
            //		 
            //		    return new BoundingBox(
            //		        Vector3.Min(xa, xb) + Vector3.Min(ya, yb) + Vector3.Min(za, zb) + m.Translation,
            //		        Vector3.Max(xa, xb) + Vector3.Max(ya, yb) + Vector3.Max(za, zb) + m.Translation

            return new BoundingBox(worldMin, worldMax);
        }


        public bool Intersects(Ray r, double t0, double t1, out double distance1, out double distance2)
        {
            distance1 = distance2 = 0;

            // https://people.csail.mit.edu/amy/papers/box-jgt.pdf
            Vector3d[] parameters = new Vector3d[2];
            parameters[0] = _min;
            parameters[1] = _max;
            double tXmin = (parameters[r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
            double tXmax = (parameters[1 - r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
            double tymin = (parameters[r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;
            double tymax = (parameters[1 - r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;

            // TODO: is there an issue in this method of failing to collide when the t0 and t1 are both inside the 
            // min/max of the bounding box such that there is no collision with any plane?  In that way, the box "contains"
            // the ray but never intersects it.

            //         distance1 = tXmin;
            //         distance2 = tXmax;

            if ((tXmin > tymax) || (tymin > tXmax)) return false;
            // consolidate min/max into txmin and txmax respectively
            if (tymin > tXmin)
                tXmin = tymin;
            if (tymax < tXmax)
                tXmax = tymax;

            double tzmin = (parameters[r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;
            double tzmax = (parameters[1 - r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;

            if ((tXmin > tzmax) || (tzmin > tXmax)) return false;
            // consolidate min/max into txmin and txmax respectively
            if (tzmin > tXmin)
                tXmin = tzmin;
            if (tzmax < tXmax)
                tXmax = tzmax;

            // The code from this lesson returns intersections with the box which are in front or behind 
            // the origin of the ray. For instance, if the ray's origin is inside the box (like in the 
            // image on the right), there will be two intersections: one in front of the ray and one behind.
            // We know that an intersection is "behind" the origin of the ray when the value for t is negative.
            // When t is positive, the intersection is in front of the origin of the ray. If your algorithm
            // is not interested in intersections for values of t lower than 0, then you will have to carefully
            // deal with these cases when you return from the ray-box intersection box (as it is often a source 
            // of bugs).

            distance1 = tXmin;
            distance2 = tXmax;

            // if -1 for t0 and t1, no min/max range testing wanted
            if (t0 == -1d || t1 == -1d) return true;

            // return true if any part of collision segmewnt overlaps the min/max range
            return ((tXmin < t1) && (tXmax > t0));
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
        /// is good enough for t1 and perhaps t0 being 0 or very close to it .001.
        /// Same principle works for collision of bullets and particle lasers between frames.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="t0">Start interval</param>
        /// <param name="t1">End interval</param>
        /// <returns></returns>
        public bool Intersects(Ray r, double t0, double t1)
        {
            //https://tavianator.com/2011/ray_box.html
            // http://www.scratchapixel.com/lessons/3d-basic-lessons/lesson-7-intersecting-simple-shapes/ray-box-intersection/
            Vector3d[] parameters = new Vector3d[2];
            parameters[0] = _min;
            parameters[1] = _max;
            double tXmin = (parameters[r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
            double tXmax = (parameters[1 - r.Sign[0]].x - r.Origin.x) * r.InverseDirection.x;
            double tymin = (parameters[r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;
            double tymax = (parameters[1 - r.Sign[1]].y - r.Origin.y) * r.InverseDirection.y;

            // TODO: is there an issue in this method of failing to collide when the t0 and t1 are both inside the 
            // min/max of the bounding box such that there is no collision with any plane?  In that way, the box "contains"
            // the ray but never intersects it.


            if ((tXmin > tymax) || (tymin > tXmax)) return false;
            // consolidate min/max into txmin and txmax respectively
            if (tymin > tXmin)
                tXmin = tymin;
            if (tymax < tXmax)
                tXmax = tymax;

            double tzmin = (parameters[r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;
            double tzmax = (parameters[1 - r.Sign[2]].z - r.Origin.z) * r.InverseDirection.z;

            if ((tXmin > tzmax) || (tzmin > tXmax)) return false;
            // consolidate min/max into txmin and txmax respectively
            if (tzmin > tXmin)
                tXmin = tzmin;
            if (tzmax < tXmax)
                tXmax = tzmax;

            // The code from this lesson returns intersections with the box which are in front or behind 
            // the origin of the ray. For instance, if the ray's origin is inside the box (like in the 
            // image on the right), there will be two intersections: one in front of the ray and one behind.
            // We know that an intersection is "behind" the origin of the ray when the value for t is negative.
            // When t is positive, the intersection is in front of the origin of the ray. If your algorithm
            // is not interested in intersections for values of t lower than 0, then you will have to carefully
            // deal with these cases when you return from the ray-box intersection box (as it is often a source 
            // of bugs).

            // if -1 for t0 and t1, no min/max range testing wanted
            if (t0 == -1d || t1 == -1d) return true;

            // return true if any part of collision segmewnt overlaps the min/max range
            return ((tXmin < t1) && (tXmax > t0));
        }

        // also a good article on various collision detections
        // http://www.harveycartel.org/metanet/tutorials/tutorialA.html
        // a simple collision response so that two colliding boxes dont penetrate
        // A this point we've already determined that the boxes intersect...

        //float dist[4];

        //dist[0] = box1.max.x - box2.min.x;
        //dist[1] = box2.max.x - box1.min.x;
        //dist[2] = box1.max.y - box2.min.y;
        //dist[3] = box2.max.y - box1.min.y;

        //size_t direction = std::distance(dist, std::min_element(dist, dist + 4));

        //switch (direction) {
        //    case 0: /* Move box1 along -x by dist[0] */ // break;
                                                        //    case 1: /* Move box1 along +x by dist[1] */ break;
                                                        //    case 2: /* Move box1 along -y by dist[2] */ break;
                                                        //    case 3: /* Move box1 along +y by dist[3] */ break;
                                                        //}



        //returns if the passed inbox is contained in whole or in part with the existing box
        //NOTE: We must test both boxes against each other because if one box totally encompasses the other
        // then none of its corners will be in the bounds of the other, but the opposite WILL be true.
        // sadly, the worst case represents a maximum of 24 tests but we can usually bail out early 
        //TODO: I think technically, if the boxes arent necessarily exact but have all 4 corners on the same
        // planes as two sides each then it might not register as being "contained" since its more accurately "on" the other.
        public bool Intersects(BoundingBox box)
        {

            bool result = (this.Min.x < box.Max.x) && (this.Max.x > box.Min.x) &&
                (this.Min.y < box.Max.y) && (this.Max.y > box.Min.y) &&
                (this.Min.z < box.Max.z) && (this.Max.z > box.Min.z);

            return result;

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
            //Console.WriteLine("Contains");
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

        public bool Contains(double pointX, double pointY, double pointZ)
        {
            return (pointX >= Min.x && pointX <= Max.x &&
                    pointY >= Min.y && pointY <= Max.y &&
                    pointZ >= Min.z && pointZ <= Max.z);
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


        public void Reset()
        {
            _min = MIN_INIT;
            _max = MAX_INIT;
        }

        public void Resize(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            _min.x = minX;
            _min.y = minY;
            _min.z = minZ;
            _max.x = maxX;
            _max.y = maxY;
            _max.z = maxZ;
        }

        /// <summary>
        /// Combines the dimensions of the parameter box to this existing box.
        /// </summary>
        /// <param name="target"></param>
        public void Combine(BoundingBox target)
        {
            Vector3d min, max;
            min.x = Math.Min(_min.x, target._min.x);
            min.y = Math.Min(_min.y, target._min.y);
            min.z = Math.Min(_min.z, target._min.z);

            max.x = Math.Max(_max.x, target._max.x);
            max.y = Math.Max(_max.y, target._max.y);
            max.z = Math.Max(_max.z, target._max.z);

            _min = min;
            _max = max;
        }

        //When combining bounding volumes, an uninitialized box will have 0,0,0 for both min & max vectors
        //This is usually unintional so make sure your bounding boxes are initialized with proper min/max vectors.
        public static BoundingBox Combine(BoundingBox b1, BoundingBox b2)
        {
            // TODO: I need a version of Combine that is NOT static and which will simply
            //       increase the size of the existing
            Vector3d min, max;
            min.x = Math.Min(b1.Min.x, b2.Min.x);
            min.y = Math.Min(b1.Min.y, b2.Min.y);
            min.z = Math.Min(b1.Min.z, b2.Min.z);

            max.x = Math.Max(b1.Max.x, b2.Max.x);
            max.y = Math.Max(b1.Max.y, b2.Max.y);
            max.z = Math.Max(b1.Max.z, b2.Max.z);
            return new BoundingBox(min, max);
        }

        //TODO: I really should make these regular NON static methods
        public static Vector3d[,] GetQuadFaceVertices(BoundingBox box)
        {
            Vector3d[,] vertices = new Vector3d[6, 4];
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

        public static Vector3d[] GetVertices(BoundingBox box)
        {
           //Console.WriteLine("Get Vertices");
            Vector3d[] vertices = new Vector3d[8];

            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THIS 
            // 6 ___ 7
            // |    |
            // 4 ___ 5
            //  \    \
            //   2 ___ 3
            //   |    |
            //   0 ___ 1
            // is our layout

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

        public static Triangle[] GetTriangleFaces(BoundingBox box)
        {
            // construct 12 triangles from our bounding box vertices.  
            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THUS 
            // 6 ___ 7
            // |    |
            // 4 ___ 5
            //  \    \
            //   2 ___ 3
            //   |    |
            //   0 ___ 1
            // is our layout     
            Triangle[] tris = new Triangle[12];
            Vector3d[] v = box.Vertices;

            // bottom 2 faces
            tris[0] = new Triangle(v[0], v[1], v[3]);
            tris[1] = new Triangle(v[0], v[3], v[2]);

            // top 2 faces
            tris[10] = new Triangle(v[4], v[6], v[7]);
            tris[11] = new Triangle(v[4], v[7], v[5]);

            // the side faces
            tris[2] = new Triangle(v[0], v[4], v[1]); // front
            tris[3] = new Triangle(v[1], v[4], v[5]);

            tris[4] = new Triangle(v[2], v[6], v[0]); // left
            tris[5] = new Triangle(v[2], v[4], v[0]);

            tris[6] = new Triangle(v[3], v[6], v[2]); // back
            tris[7] = new Triangle(v[3], v[7], v[6]);

            tris[8] = new Triangle(v[1], v[7], v[3]);
            tris[9] = new Triangle(v[7], v[1], v[5]); // right
            return tris;
        }

        public static Polygon[] GetPolyFaces(BoundingBox box)
        {
            // NOTE: Default DirectX winding order is CLOCKWISE vertices for
            // front (outward) facing.  XNA also uses clockwise for front facing.
            // THUS 
            // 6 ___ 7
            // |    |
            // 4 ___ 5
            //  \    \
            //   2 ___ 3
            //   |    |
            //   0 ___ 1
            // is our layout      

            Polygon[] polys = new Polygon[6];
            Vector3d[] v = box.Vertices;

            // bottom face
            polys[0] = new Polygon(v[0], v[1], v[3], v[2]);

            // top face
            polys[5] = new Polygon(v[4], v[6], v[7], v[5]);

            // the side faces
            polys[1] = new Polygon(v[0], v[2], v[6], v[4]); // left 
            polys[2] = new Polygon(v[1], v[5], v[7], v[3]); // right
            polys[3] = new Polygon(v[0], v[4], v[5], v[1]); // front
            polys[4] = new Polygon(v[3], v[7], v[6], v[2]); // back

            return polys;
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
                double CurrentFar = ProjectedVector * Math.Sign(ProjectedVector);

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
        

		public override bool Equals (object bb)
		{
			if (bb is BoundingBox == false) return false;
			
			return this == (BoundingBox)bb;
		}
		
		public override int GetHashCode()
		{
			return this.Min.GetHashCode() + this.Max.GetHashCode();
		}
		
        // Equality operator. Returns dbNull if either operand is dbNull, 
        // otherwise returns dbTrue or dbFalse:
        public static bool operator ==(BoundingBox a, BoundingBox b)
        {
            if (a.Min == b.Min && a.Max == b.Max) return true;
            return false;
        }

        // Inequality operator. Returns dbNull if either operand is
        // dbNull, otherwise returns dbTrue or dbFalse:
        public static bool operator !=(BoundingBox a, BoundingBox b)
        {
            if (a.Min != b.Min || a.Max != b.Max) return true;
            return false;
        }
    }
}
