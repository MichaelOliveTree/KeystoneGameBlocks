using System;
using Keystone.Types;

namespace Keystone.Controls
{
    public class Control2D : Control
    {
        // Position.Z value is ignored during rendering however
        //    it is used for z-order.
        // Rotation is ignored
        // Scale is ignored


        public Control2D(string id)
            : base(id)
        {
            Pickable = true;
            Dynamic = false;

        }

        #region ITraverer
        public override object Traverse(Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        // this center value is relative either to the Viewport or
        // if the parent of this control2D is another control2D it's relative  to that
        // mTranslation.x = Center.x
        // mTranslation.y = center.y
        // mTranslation.z = zOrder
        // thus the ScreenSpaceCenter is the cumulative result of that 
        // TODO: and then what if we do want to add in real scaling of the entire GUI?
        //       I suppose in that case, when we pass in the RegionMatrix we would calc
        //       the region matrix so that translation and width/height of the child
        //       reflects the scale changes to the parent.
        //       However this does mean that Scale must be kept seperate from Width and Height
        //       vars.  That is, width and height cannot store in mScale var.  Else we cannot
        //       set a scale that is to be applied to the width and height.

        /// <summary>
        /// Center explicitly refers to the center of the rectangle of the control.
        /// This is less ambiguous than "Position" which may refer to either the center
        /// or the top left corner of a control.  The convention changes depending on
        /// the GUI library or Window system or library.
        /// We will use Center.
        /// </summary>
        private Vector3d mCenter2D; // NOTE: mTranslation holds the values of the 3d Entity represented by this Control if its a Proxy so we use a seperate Center2D var to hold the 2d render coords
        public int CenterX
        {
            get { return (int)mCenter2D.x; }
            set { mCenter2D.x = value; }
        }

        public int CenterY
        {
            get { return (int)mCenter2D.y; }
            set { mCenter2D.y = value; }
        }

        public int ZOrder
        {
            get { return (int)mCenter2D.z; }
            set { mCenter2D.z = value; }
        }

        private float mWidth;
        private float mHeight;
        private float mAngle = 0.0f;
        public float Width
        {
            get { return mWidth; }
            set { mWidth = value; }
        }

        public float Height
        {
            get { return mHeight; }
            set { mHeight = value; }
        }

        // radians
        public float Angle
        {
            get { return mAngle; }
            set { mAngle = value; }
        }


        public override Quaternion Rotation
        {
            get
            {
                return base.Rotation;
            }
            set
            {
                return; // cannot set Rotation here for a 2D Element.  Our LocalMatrix is set up a specific way.
            }
        }



        #region Transform Members
        /// <summary>
        /// The RegionMatrix of a 2D Element is irrelevant.  AT least for now
        /// because nested child 2D Elements are not implemented.
        /// </summary>
        public override Matrix RegionMatrix
        {
            get
            {
                // TODO: this really does in retrospect, need to include
                //       the parent's translation and parent's scaling so that
                //       here we can store in the RegionMatrix the complete relative
                //       translation and scale, however we will very likely just ignore
                //       rotations.. unless perhaps we allow some fixed 90/180/270 rotations
                mLocalMatrix = Matrix.Identity ();
                mLocalMatrix.M11 = mWidth * mScale.x; // TODO: is not taking into account hierarchical parent scaling
                mLocalMatrix.M22 = mHeight * mScale.y;
                mLocalMatrix.M33 = mAngle; // WARNING!!!!!!!!!: if we try to add 3D geometry (eg mesh3d) to Control2D, it will have bad z axis scaling!
                mLocalMatrix.M41 = mTranslation.x;
                mLocalMatrix.M42 = mTranslation.y;
                mLocalMatrix.M43 = mTranslation.z;
                return mLocalMatrix; // LocalMatrix;
            }

        }


        public override Matrix GlobalMatrix
        {
            get
            {
                return RegionMatrix;
            }
        }
        #endregion
    }
}
