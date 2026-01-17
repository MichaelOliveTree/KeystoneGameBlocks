using System;
using System.Drawing;
using Keystone.Types;
using Keystone.Traversers;

namespace KeyKeystone.SpatialNodesstone.Quadtree
{
    // A "3d" quadtree node allows us to use TVCamera.IsBoxVisible() for culling rather than just
    // using 2d test.  The benefit is that when looking upward for instance, you can still cull
    // nodes that are otherwise still in front of the camera but still below the bottom plane of the frustum
    public class Leaf3d : Leaf, IBoundVolume
    {
        private bool _isDirty = true; // TODO: should be a generic 32bit flag
        protected BoundingBox _box;

        public override object Traverse(ITraverser t, object data)
        {
            return t.Apply(this,  data);
        }

        public Leaf3d(string name, RectangleF r, QTreeNode parent)
            : base(name, r, parent)
        {
        }

        #region IBoundVolume Members

        public BoundingBox BoundingBox
        {
            get
            {
                if (_isDirty) UpdateBoundVolume();
                return _box;
            }
        }

        public BoundingSphere BoundingSphere
        {
            get { throw new NotImplementedException(); }
        }

        protected virtual void UpdateBoundVolume()
        {
            //TODO: loop through items contained herin and update bounded volume
            // this needs to be overriden by Sector
            // _box = BoundingBox.Combine(_box, node.BoundVolume);
            BoundVolumeIsDirty = false;
            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
        }

        public bool BoundVolumeIsDirty
        {
            get { return _isDirty; }
            set
            {
                _isDirty = value;
                if (_isDirty)
                {
                    // TODO: the below needs to be implemented but without the _isDirty flag which we made obsolete everywhere else when we
                    //  switched to the _changeState flags
                    //PropogateChangeFlags();
                    //if (_quadrant != QUADRANT.ROOT)
                    //{
                    //    ((IBoundVolume) _parent).BoundVolumeIsDirty = true;
                    //}
                }
            }
        }

        #endregion
    }
}