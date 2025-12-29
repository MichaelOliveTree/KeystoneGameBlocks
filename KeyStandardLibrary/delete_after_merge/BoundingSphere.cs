using Core.Types;
using MTV3D65;

namespace Core.Types
{
    public class BoundingSphere
    {
        private Vector3d _center;
        private double _radius;

        public BoundingSphere(Vector3d center, double radius)
        {
            _center = center;
            _radius = radius;
        }

        // Sphere to sphere can be a faster test, but results in much greater overdraw because the sphere is HUGE to make 
        // sure it doesnt cull things that are visible.  This is best used as a preliminary stage cull.
        public BoundingSphere(float nearPlane, float farPlane, float fovRadians,
                              Vector3d cameraPosition, Vector3d lookAt)
        {
            // note in TV3d the lookAt is actually the point in world coordinates of where we are looking
            // to get the real direction vector, substract it from the camera position
            Vector3d result = lookAt - cameraPosition;
            lookAt = result;

            double diameter = farPlane - nearPlane;
            double sngRadius = diameter*0.5;
            double farPlaneHeight = diameter*System.Math.Tan(fovRadians*0.5);

            // with an aspect ratio of 1, our width = height
            double farPlaneWidth = farPlaneHeight;

            //todo: once we have the radius, we dont actually have to update it unless the far/near/fov changes
            // but we'll still always need to update the center based on the LookAt
            Vector3d center = new Vector3d(0f, 0f, nearPlane + sngRadius);
            Vector3d farCorner = new Vector3d(farPlaneWidth, farPlaneHeight, diameter);

            result = farCorner - center;
            // the frustum sphere radius becomes the length of this vector
            _radius = (float) result.Length;

            // TODO: Below is actually the only things that need to be updated every frame.  So to optimize
            // this seperate out the FrustumSphereInitialization from the FrustumSphereUpdate.  Only re-init
            // if for some reason the near/far/fov changes.
            // calculate the center of the sphere           
            result = Vector3d.Normalize(lookAt)*_radius;
            _center = cameraPosition + result;
        }

        public double Radius
        {
            get { return _radius; }
        }

        public Vector3d Center
        {
            get { return _center; }
        }

        // if the distance between the sphere center is less than radius it contains the point
        public bool Contains(Vector3d point)
        {
            Vector3d v = _center - point;
            double distance = v.LengthSquared();
            return distance < _radius;
        }

        // if the distance between the centers is less than the radius of this sphere
        // _and_ the radius of this sphere is larger than the other, this sphere fully contains the other
        public bool Contains(BoundingSphere bsphere)
        {
            Vector3d v = _center - bsphere.Center;
            double distance = v.LengthSquared();
            // similar to intersect only instead of sumRadiiSquared, its just the radius squared of the source sphere
            // for small meshes being tested against a frustum sphere this results in much less "intersect" false positives
            // however for large meshes, this will ignore the ones that dont fully fit within the source (e.g. frustum) sphere.
            return distance < _radius*_radius && _radius > bsphere.Radius;
        }


        // if the distance between the centers is less than the sum of the radii then the two spheres intersect
        public bool Intersects(BoundingSphere bsphere)
        {
            Vector3d v = _center - bsphere.Center;
            double RadiiSum = _radius + bsphere.Radius;

            double distance = v.LengthSquared();
            double RadiiSquared = RadiiSum*RadiiSum;
            return distance < RadiiSquared;
        }

        // Ray-sphere intersection. 
        // r=(ray),
        // Output:
        // i1=first intersection distance,
        // i2=second intersection distance
        // i1<=i2
        // i1>=0
        // returns true if intersection found,false otherwise.
        //
        public bool Intersects (Ray  r,  ref double  i1, ref double i2)
        {
            Vector3d p = r.origin - _center;
            double b = -Vector3d.DotProduct(p, r.direction);
            double det = b * b - Vector3d.DotProduct(p, p) + _radius * _radius;
	        if (det<0) return false;
        	
	        det= System.Math.Sqrt(det);
	        i1= b - det;
	        i2= b + det;
	        // intersecting with ray?
	        if(i2<0) return false;
	        if(i1<0) i1=0;
	        return true;
        }

       
        public void Draw(CONST_TV_COLORKEY color)
        {
            DebugDraw.DrawSphere(_center, _radius, color);
        }
    }
}