using System;
using Core.Types;
using MTV3D65;

namespace Core.Types
{
    public class BoundingCone
    {
        private float _sinReciprocal;
        private float _sinSquared;
        private float _cosReciprocal;
        private float _cosSquared;
        private float _fovRadians;

        private Vector3d _axis, _position;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="axis">Direction vector</param>
        /// <param name="fovRadians"></param>
        public BoundingCone(Vector3d position, Vector3d axis, float fovRadians)
        {
            _fovRadians = fovRadians;
            _axis = axis;
            _position = position;

            _sinReciprocal = (float) (Math.Sin(fovRadians));
            _sinSquared = _sinReciprocal*_sinReciprocal;
            _cosReciprocal = (float) (Math.Cos(fovRadians));
            _cosSquared = _cosReciprocal*_cosReciprocal;
        }

        public BoundingCone(float fovRadians, Vector3d cameraPosition,
                            Vector3d cameraLookAt, float viewportWidth, float viewportHeight)
        {
            double depth,
                   halfViewportHeight = viewportHeight*0.5,
                   // since we dont have more than one viewport, this is the same as screen
                   halfViewportWidth = viewportWidth*0.5,
                   //  but eventually we'll want to assign a viewport to this camera and then query its height/width
                   corner;


            // calculate the length of the field of view triangle
            depth = (double) (halfViewportHeight/Math.Tan(fovRadians*0.5f));

            // calc the corner of the screen
            corner = (double) (Math.Sqrt((halfViewportWidth*halfViewportWidth) +
                                         (halfViewportHeight*halfViewportHeight)));

            // update the new fovRadians
            fovRadians = (float) Math.Atan(corner/depth);

            // todo: to optimize, only the below call needs to be made every frame.  The lines above this are merely initialization.
            Init(cameraPosition, cameraLookAt - cameraPosition, fovRadians);
        }

        private void Init(Vector3d position, Vector3d axis, float fovRadians)
        {
            _fovRadians = fovRadians;
            _axis = axis;
            _position = position;

            _sinReciprocal = (float) (Math.Sin(fovRadians));
            _sinSquared = _sinReciprocal*_sinReciprocal;
            _cosReciprocal = (float) (Math.Cos(fovRadians));
            _cosSquared = _cosReciprocal*_cosReciprocal;
        }

        public Vector3d Position
        {
            get { return _position; }
        }

        public Vector3d Axis
        {
            get { return _axis; }
        }

        public float SinReciprocal
        {
            get { return _sinReciprocal; }
        }

        public double CosReciprocal
        {
            get { return _cosReciprocal; }
        }

        public double SinSquared
        {
            get { return _sinSquared; }
        }

        public double CosSquared
        {
            get { return _cosSquared; }
        }

        public double FOVRadians
        {
            get { return _fovRadians; }
        }

        public INTERSECT_RESULT Intersects(BoundingSphere sphere)
        {
            double dotSquare;
            double e;
            Vector3d D, U;

            U = _axis*(sphere.Radius - _sinReciprocal); // todo: replace with non tv dot product 

            U.x = _position.x - U.x;
            U.y = _position.y - U.y;
            U.z = _position.z - U.z;

            D = sphere.Center - U;
                // todo: replace with non tv dot product // todo: this comment is here, did i accidentally delete something here?  maybe was just a TVMath.VSubtract before and then forgot to delete the comment when i changed to use overloaded -

            dotSquare = Vector3d.DotProduct(D, D); // todo: replace with non tv dot product 

            e = Vector3d.DotProduct(_axis, D); // todo: replace with non tv dot product 

            if (e > 0 && e*e >= dotSquare*_cosSquared)
            {
                D = sphere.Center - _position;

                dotSquare = Vector3d.DotProduct(D, D); // todo: replace with non tv dot product 

                e = -(Vector3d.DotProduct(_axis, D)); // todo: replace with non tv dot product 

                if (e > 0 && e*e >= dotSquare*_sinSquared)
                    if (dotSquare <= sphere.Radius*sphere.Radius)
                        return INTERSECT_RESULT.INSIDE;
                    else
                        return INTERSECT_RESULT.OUTSIDE;
                else
                    return INTERSECT_RESULT.INSIDE;
            }
            return INTERSECT_RESULT.OUTSIDE;
        }

//        public Vector3d [] GetVertices(int numSlices, double length)
//        {
//            // to create the vertices we simply find one point
//            // and then rotate it about the cone's axis

//            Vector3d axisEndPoint = Core._Core.Maths.VSubtract(Translation, (Core._Core.Maths.VScale(_axis, length))); 

//            // now find a 3rd point such that the angle to it from the position is theta and that is also 90 to the endpoint
//            // axis is the unit vector in the direction of axisEndPoint-->Translation

//            // then newV in the direction of axisEndPoint --> newPoint =
//            // newV = (Math.Cos(axisEndPoint.X) 

////Well, then it's easy. I assume the angle B goes clockwise from
////BA to BC. If u=(u1,u2) is the unit vector
////(x1-x2, y1-y2)/sqrt((x1-x2)^2+(y1-y2)^2) in the direction of the
////vector BA, then the unit vector in the direction of BC is
////v = (cos(B) u1 - sin(B) u2, cos(B) u2 + sin(B) u1),
////and C = B + |BC| v.

//        }

        public void Draw()
        {
        }
    }
}