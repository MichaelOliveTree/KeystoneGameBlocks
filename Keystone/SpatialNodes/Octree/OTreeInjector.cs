//using System;
//using Keystone.Elements;
//using Keystone.Entities;
//using Keystone.Lights;
//using Keystone.Portals;
//using Keystone.Quadtree;
//using Keystone.Traversers;
//using Keystone.Types;
//using MTV3D65;
//using Sector = Keystone.Quadtree.Sector;

//namespace Keystone.SpatialNodes.Octree
//{
//    public class OTreeInjector : ITraverser
//    {
//        private OctreeNode _root;
//        private Vector3d _position;
//        private Geometry _element;

//        public OTreeInjector(OctreeNode node)
//        {
//            if (node == null) throw new Exception();
//            _root = node;
//        }

//        public void Inject(Geometry o, Vector3d pos)
//        {
//            _position = pos;
//            _element = o;
//            _root.Traverse(this);
//        }

//        #region ITraverser Members

//        void ITraverser.Apply(Node o)
//        {
//        }

//        public object Apply(SceneNode node)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(OctreeOctant octant)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(OctreeRegionNode host)
//        {
//            throw new NotImplementedException();
//        }

//        //// TODO: we should add option for adding a node into a quadrant instead of the sector
//        //// in case we only want things to be wholly contained in a quadtree node and not overlapping.
//        //// so in this case, we insert into the tree only where its wholly contained
//        //void ITraverser.Apply(OctreeNode o)
//        //{
//        //    //     //TODO: for now lets just allow element3d's into tree
//        //    ////TODO: how to notify when element moves to determine if we need to move the element in the tree?
//        //    ////TODO: what about removing elements?  notifications and movement?
//        //    ////TODO: what about reclaiming (de-allocating) child nodes that are empty for a "long" time.
//        //    ////      maybe on remove, if all children under a parent are now empty set a time and then
//        //    ////      periodically in our update thread we can "compact" or "prune" traversal.
//        //    //// TODO: use an insert traversal.
//        //    //// TODO: I suppose as an element is "inserted" into the scene, the scene maintains the list of subscribers.
//        //    ////       then as the element moves, it can fire a notify to the Scene its subscribed too.
//        //    ////       Then if the element needs to move in the graph, the handler can do it easily by
//        //    ////       (and can optimize for most cases by reverse recursive checking since odds are the element has just moved
//        //    //// over to a sibling.
//        //    //// TODO: So this does mena i think that OctreeNode needs to inherit IGroup so that "Model" for instance can
//        //    ////       in fact have OctreeNode as a parent.

//        //    //    Vector3d childRadius = o.Radius  * 0.5F;
//        //    //    const double RMOD = 2f;
//        //    //    const int _maxDepth = 3;

//        //    //    Vector3d eRadius =
//        //    //        new Vector3d(_element.BoundingBox.Width / 2, _element.BoundingBox.Height / 2, _element.BoundingBox.Depth / 2);
//        //    //    if (o.Depth < _maxDepth && V1LessThanEqualsV2(eRadius, childRadius ))
//        //    //    {
//        //    //        if (o.Children == null) o.CreateChildren();

//        //    //        foreach (OctreeNode node in o.Children )
//        //    //        {
//        //    //            //find which child to insert it into
//        //    //            Vector3d center = _element.BoundingBox.Center ;

//        //    //            if (node.TightBox.Contains(center))
//        //    //            {
//        //    //                node.Traverse( this);
//        //    //                break;
//        //    //            }
//        //    //        }
//        //    //        //if we havent broken out then something is wrong
//        //    //        //because the parent reported as containing hte node
//        //    //        // yet no child node was traversed.  Simple hack would be to just
//        //    //        // add the element to theparent
//        //    //        // o.Add(_element);
//        //    //    }
//        //    //    else // we add it right here
//        //    //    {
//        //    //        o.Add(_element);
//        //    //    }
//        //}

//        public object Apply(RegionNode node)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(CelledRegionNode node)
//        {
//            throw new NotImplementedException();
//        }
//        public object Apply(CellSceneNode node)
//        {
//            throw new NotImplementedException();
//        }
//        public object Apply(PortalNode node)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(EntityNode node)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Portals.Region region)
//        {
//            throw new NotImplementedException();
//        }

//        //public object Apply(Interior interior)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public object Apply(ModeledEntity entity)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Portal p)
//        {
//            throw new NotImplementedException();
//        }


//        //public object Apply(Quadrant o)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //void ITraverser.Apply(Sector o)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public object Apply(QTreeNode o)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public object Apply(Branch o)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public object Apply(Terrain terrain)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Leaf o)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(ModelLODSwitch lod)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Geometry element)
//        {
//            throw new NotImplementedException();
//        }

//        public object Apply(Light light)
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