using MTV3D65;

namespace Core.Types
{
    ///
    /// Ray class, for use with the optimized ray-box intersection test
    /// described in:
    ///
    ///      Amy Williams, Steve Barrus, R. Keith Morley, and Peter Shirley
    ///      "An Efficient and Robust Ray-Box Intersection Algorithm"
    ///      Journal of graphics tools, 10(1):49-54, 2005
    /// 
    /// http://www.realtimerendering.com/intersections.html
    public class Ray
    {
        public Vector3d origin;
        public Vector3d direction;
        public Vector3d inv_direction;
        public int[] sign = new int[3];

        public Ray(Vector3d o, Vector3d d)
        {
            origin = o;
            direction = d;
            inv_direction = new Vector3d(1/d.x, 1/d.y, 1/d.z);
            sign[0] = (inv_direction.x < 0) ? 1 : 0;
            sign[1] = (inv_direction.y < 0) ? 1 : 0;
            sign[2] = (inv_direction.z < 0) ? 1 : 0;
        }

        //  Ray(const Ray &r) {
        //origin = r.origin;
        //direction = r.direction;
        //inv_direction = r.inv_direction;
        //sign[0] = r.sign[0]; sign[1] = r.sign[1]; sign[2] = r.sign[2];
    }
}