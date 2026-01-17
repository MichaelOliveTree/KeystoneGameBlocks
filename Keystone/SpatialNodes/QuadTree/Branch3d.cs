using System;
using System.Drawing;
using Keystone.Types;
using Keystone.Traversers;

namespace Keystone.SpatialNodes.Quadtree
{
    public class Branch3d : Branch, IBoundVolume
    {
        private bool _isDirty = true;
        protected BoundingBox _box;

        public override object Traverse(ITraverser t, object data)
        {
            return t.Apply(this, data);
        }

        public Branch3d(string name, RectangleF r, QTreeNode parent)
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
            foreach (IBoundVolume node in Child)
            {
                _box.Combine(node.BoundingBox);
            }
            BoundVolumeIsDirty = false;
        }

        public bool BoundVolumeIsDirty
        {
            get { return _isDirty; }
            set
            {
                _isDirty = value;
                if (_isDirty)
                {
                    throw new Exception("not fully implemented");
                    // TODO: the below is the old way of doing thigns before we switched to the ChangeStateFlags.  We no longer use a single _isDirty bool
                    //       but instead use a 32 bit int that supports 32 different flags
                    //if (_parents != null)
                    //{
                    //    foreach (IGroup g in _parents)
                    //        g.NotifyChange(this);
                    //}

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