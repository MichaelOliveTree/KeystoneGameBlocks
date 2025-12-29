using System;
using Keystone.Culling;
using Keystone.Traversers;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Elements
{
    // Node wrapper of the OcclusionFrustum so that it can be added and manipulated in the scene
    // just like any other node.
    public class Occluder : Node, IBoundVolume

    {
        private OcclusionFrustum _frustum;
        private TVMesh _retainedMesh;

        public Occluder(string id, TVMesh occluder, bool retainMesh)
            : base(id)
        {
            TV_3DVECTOR min, max;
            min = new TV_3DVECTOR(0, 0, 0);
            max = new TV_3DVECTOR(0, 0, 0);
            occluder.GetBoundingBox(ref min, ref max, false);
            _box = new BoundingBox(min.x, min.y, min.z, max.x,max.y, max.z);
            BoundVolumeIsDirty = false;
            ConvexHull hull = new ConvexHull(_box);

            _frustum = new OcclusionFrustum(hull);
            if (retainMesh) _retainedMesh = occluder; // TODO: destroy it otheriwse? 
        }

        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }

        public OcclusionFrustum Frustum
        {
            get { return _frustum; }
        }

        #region IBoundVolume Members
        private BoundingBox _box;
        private bool _boundVolumeIsDirty = true;

        public new IBoundVolume Parent
        {
            get { throw new NotImplementedException(); }
        }

        public BoundingBox BoundingBox
        {
            get { return _box; }
        }

        public BoundingSphere BoundingSphere
        {
            get { throw new NotImplementedException(); }
        }

        public bool BoundVolumeIsDirty
        {
            get { return _boundVolumeIsDirty; }
            set { _boundVolumeIsDirty = value; }
        }

        protected void UpdateBoundVolume()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}