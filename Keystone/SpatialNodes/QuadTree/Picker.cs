//using System;
//using System.Drawing;
//using Keystone.Elements;
//using Keystone.Entities;
//using Keystone.Lights;
//using Keystone.Octree;
//using Keystone.Portals;
//using Keystone.Quadtree;
//using Keystone.Traversers;
//using Keystone.Types;
//using MTV3D65;
//using Sector = Keystone.SpatialNodes.Quadtree.Sector;
//using Keystone.QuadTree;

//namespace Keystone.SpatialNodes.QuadTree
//{
//    // used to find a particular leaf node in a quadtree
//    public class Picker : ITraverser
//    {
//        private QTreeNode _node;
//        private Vector3d _point;
//        private PointF _point2D;
//        private bool _use2D = false;

//        public delegate void FoundNode(QTreeNode n);

//        private FoundNode _foundNodeCB;


//        public Picker(QTreeNode o, PointF p, FoundNode cb)
//        {
//            if (o == null) throw new ArgumentNullException();
//            _node = o;
//            _point2D = p;
//            _use2D = true;
//            _foundNodeCB = cb;
//        }

//        public Picker(QTreeNode o, Vector3d p, FoundNode cb)
//        {
//            if (o == null) throw new ArgumentNullException();
//            _node = o;
//            _point = p;
//            _use2D = false;
//            _foundNodeCB = cb;
//        }

//        // in case you dont want to use the callback
//        public QTreeNode Node
//        {
//            get { return _node; }
//        }

//        #region ITraverser Members

//        object ITraverser.Apply(Node o, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(SceneNode node, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(OctreeOctant octant, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(QuadtreeQuadrant quadrant, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(RegionNode node, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(CelledRegionNode node, object data)
//        {
//            throw new NotImplementedException();
//        }
//        public object Apply(CellSceneNode node, object state)
//        {
//            throw new NotImplementedException();
//        }
//        public object Apply(PortalNode node, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(EntityNode node, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Portals.Region region, object data)
//        {
//            throw new NotImplementedException();
//        }

//        //public object Apply(Interior interior, object data)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public object Apply(ModeledEntity entity, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Portal p, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Terrain terrain, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(ModelLODSwitch lod, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Geometry element, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Light light, object data)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Appearance.Appearance app, object data)
//        {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }
//}