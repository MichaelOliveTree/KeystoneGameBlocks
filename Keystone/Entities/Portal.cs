using System;
using Keystone.Elements;
using Keystone.Traversers;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Portals
{
    // Issue: Should portals be a type of SceneNode or Entity or a mix?  Why?
    //           1) For pathing, entities need to be able to have spatial awareness, knowledge of the existance of portals.
    //           Perhaps no knowledge directly by the entity, but some mechanism to connect to the spatial area
    //           Now maybe this doesnt require Portals be an entity at all.  Surely as a pure SceneNode it's sufficient.
    //           2) Portals will be placeable by the editor, and so are pickable as well and can be selected, deleted, moved, resized, etc.
    //           If portals are purely scenenodes then we must make an exception that such nodes can be manipulated like entities.
    //           This will complicate somewhat our picking code i think.
    // 
    // 
    // One way portals.  Typically it requires two portals if you want to connect two rooms together. 
    // One way portals are more flexible because you can implement one way mirrors and such
    // But more importantly, you can have the exterior entrance to a room point back to the root Octree node
    // rather than point to the lower OctreeNode where it exists.
    //public class PortalEnt : Entities.EntityBase
    //{
    //    // flags Pickable

    //    // during traversal to a PortalNode, the traverser will check if the
    //    // portalNode is visible and if so will grab the 
    //    // portalNode.PortalEntity.Target.SceneNode and continue traversal
    //    // this way the actual portalNode doesnt actually need to be special
    //    // type of spatial node actually.  This solves any issues of redundancy of information
    //    // in the two!   The only real difference is that a PortalNode as unique and not just
    //    // another EntityNode is for double dispatch traversal so we can have a special overload 
    //    // to handle portalTraversals.
    //}

    
    public class Portal : Entity 
    {
        private Region _destination;
        private string _destinationName; // required for paging

        private Face _face;
        private Vector3d[] _modelSpaceCoords;  // 4 coords in clockwise order starting at top left which comprise the quad which we use to construct the planes

        private Vector3d _normal;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="destination"></param>
        /// <param name="modelSpaceCoords">4 model space coords in clockwise order starting at top left which comprise the quad which we use to construct the planes</param>
        public Portal(string name, Region destination, Vector3d[] modelSpaceCoords)
            : base(name)
        {
            // TODO: during paging, the destination can be null but the location must never be
            if (destination == null ) throw new ArgumentNullException();

            _destination = destination;
            _destinationName = destination.ID;
            CoordinatesModelSpace = modelSpaceCoords;

            _normal =
                Vector3d.Normalize(Vector3d.CrossProduct(modelSpaceCoords[0] - modelSpaceCoords[1], modelSpaceCoords[1] - modelSpaceCoords[2]));
        }

        public Portal(string name, string destinationName, Vector3d[] modelSpaceCoords)
            : base(name)
        {
            _destinationName = destinationName;
            CoordinatesModelSpace = modelSpaceCoords;

            _normal =
                Vector3d.Normalize(Vector3d.CrossProduct(modelSpaceCoords[0] - modelSpaceCoords[1], modelSpaceCoords[1] - modelSpaceCoords[2]));
        }

        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }

        public Vector3d Normal
        {
            get { return _normal; }
        }

        public string DestinationName
        {
            get { return _destinationName; }
        }

        /// <summary>
        /// Read/Write Property for the destination of this portal
        /// </summary>
        public Region Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }

        public Vector3d[] CoordinatesModelSpace
        {
            get { return _modelSpaceCoords; }
            set
            {
                if (value.Length != 4) throw new Exception("Coordinate array length must equal 4");
                _modelSpaceCoords = value;
                SetChangeFlags(Keystone.Enums.ChangeStates.GeometryAdded, Keystone.Enums.ChangeSource.Self );
            }
        }

        public Vector3d[] CoordinatesRegionSpace
        {
            get { return Vector3d.TransformCoordArray (_modelSpaceCoords , RegionMatrix); }
        }

        protected override void UpdateBoundVolume()
        {
            float radius = 0f;
            Vector3d  min, max, center;
            min = new Vector3d(0, 0, 0);
            max = new Vector3d(0, 0, 0);
            center = new Vector3d(0, 0, 0);

            // model space bounding box
            for (int i = 0; i < _modelSpaceCoords.Length; i++)
            {
                if (_modelSpaceCoords[i].x < min.x) min.x = _modelSpaceCoords[i].x;
                if (_modelSpaceCoords[i].x > max.x) max.x = _modelSpaceCoords[i].x;
                if (_modelSpaceCoords[i].y < min.y) min.y = _modelSpaceCoords[i].y;
                if (_modelSpaceCoords[i].y > max.y) max.y = _modelSpaceCoords[i].y;
                if (_modelSpaceCoords[i].z < min.z) min.z = _modelSpaceCoords[i].z;
                if (_modelSpaceCoords[i].z > max.z) max.z = _modelSpaceCoords[i].z;
            }

            mBox = new BoundingBox(min.x, min.y, min.z, max.x, max.y, max.z);
            // when a portal's region rotates or moves, then hierarchically this must move as well so we transform by RegionMatrix
            mBox = BoundingBox.Transform(mBox, RegionMatrix);
            mSphere = new BoundingSphere(mBox);

            throw new Exception("TODO: Must compute maxvisibledistancequared based on what? NOTE: This is about regular visibility of portal and not visibility THROUGH the portal.");
            _maxVisibleDistanceSq = float.MaxValue;
            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
        }
    }
}