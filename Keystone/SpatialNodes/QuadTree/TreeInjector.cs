//using System;
//using System.Drawing;
//using Keystone.Elements;
//using Keystone.Lights;
//using Keystone.Octree;
//using Keystone.Portals;
//using Keystone.Traversers;
//using Keystone.Types;
//using MTV3D65;

//namespace Keystone.SpatialNodes.Quadtree
//{
//    public class TreeInjector : ITraverser
//    {
//        private Branch _root;
//        private Vector3d _position;
//        private PointF _position2D;
//        private Geometry _object;

//        public TreeInjector(Branch node)
//        {
//            if (node == null) throw new Exception();
//            _root = node;
//        }

//        public void Inject(Geometry o, Vector3d pos)
//        {
//            _position = pos;
//            _position2D = new PointF(_position.x, _position.z);
//            _object = o;
//            _root.Traverse(this);
//        }

//        #region ITraverser Members

//        void ITraverser.Apply(Node o)
//        {
//        }

//        public object Apply(SceneNode sn)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(OctreeHost host)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(EntityNode en)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(RegionNode rn)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Portals.Region sector)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Portal p)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(OctreeNode o)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(LODSwitch lod)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Terrain terrain)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(ModelBase model)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Geometry element)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Light directionalLight)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Appearance.Appearance app)
//        {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }
//}
