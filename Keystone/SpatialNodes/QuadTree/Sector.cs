using System;
using System.Collections.Generic;
using System.Drawing;
using Keystone.Elements;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.Types;
using MTV3D65;

namespace Keystone.SpatialNodes.Quadtree
{
    public class Sector : Leaf3d, IGroup
    {
        private List<Node> _children;

        public override object Traverse(ITraverser t, object data)
        {
            return t.Apply(this, data);
        }

        public Sector(string name, RectangleF r, QTreeNode parent)
            : base(name, r, parent)
        {
            _box = new BoundingBox(new Vector3d(Bounds.X + Bounds.Width, Bounds.Width, Bounds.Y + Bounds.Height),
                                   new Vector3d(Bounds.X, -Bounds.Width, Bounds.Y));
        }

        #region IGroup Members
        public virtual void AddChild(Node child)
        {
            if (_children == null) _children = new List<Node>();

            // same node cannot be added twice
            if (_children.Contains(child)) throw new ArgumentException("Node already exists in collection");
            _box = BoundingBox.Initialized();

            // ChangeSource.Child will ensure this notification is sent upwards
            // NOTE: always set flags early before any .AddParent/.Add so that if those other methods
            // result in calls to update a bounding box for example, that we then don't return here only
            // to set those flags as dirty again after they've just been updated
            SetChangeFlags(Keystone.Enums.ChangeStates.ChildNodeAdded, Keystone.Enums.ChangeSource.Child);
            child.AddParent(this);
            _children.Add(child);
        }

        public virtual void RemoveChildren()
        {
            while (_children != null)
            {
                RemoveChild(_children[0]);
            }
        }

        public void RemoveChild(Node child)
        {
            try
            {
                if (child is IGroup)
                    ((IGroup) child).RemoveChildren();

                Repository.DecrementRef(child); // decrement first
                if (_children.Count == 0) _children = null;
                
                _box = BoundingBox.Initialized();
                BoundVolumeIsDirty = true;
                // ChangeSource.Child will ensure this notification is sent upwards
                SetChangeFlags(Keystone.Enums.ChangeStates.ChildNodeRemoved | Keystone.Enums.ChangeStates.BoundingBoxDirty, Keystone.Enums.ChangeSource.Child);
                _children.Remove(child);
                child.RemoveParent(this);

            }
            catch
            {
            }
        }

        public virtual void MoveChildOrder(string childID, bool down)
        {
            Group.MoveChildOrder(_children, childID, down);
        }

        public void GetChildIDs(string[] filteredTypes, out string[] childIDs, out string[] childNodeTypes)
        {
            Group.GetChildIDs(_children, filteredTypes, out childIDs, out childNodeTypes);
        }


        /// <summary>
        /// Finds the first descendant that matches.
        /// </summary>
        public Node FindDescendant(Predicate<Keystone.Elements.Node> match)
        {
            return Group.FindDescendant(_children.ToArray(), match);
        }

        public Node FindNodeAtDescendantIndex(int index)
        {
            // for quadtrees or octrees, this is not useful.  This is only for DAG and not 
            // spatial structures.
            throw new NotImplementedException();
        }

        public Node FindDescendantOfType(string typename, bool recurse)
        {
            return Group.FindDescendantOfType(this, typename, recurse);
        }

        public Node[] FindDescendantsOfType(string typename, bool recurse)
        {
            return Group.FindDescendantsOfType(this, typename, recurse);
        }

        public Node[] Children
        {
            get
            {
                if (_children == null) return null;
                return _children.ToArray();
            }
        }

        public int ChildCount
        {
            get
            {
                if (_children == null) return 0;
                return _children.Count;
            }
        }
        #endregion

        #region IBoundVolume members

        protected override void UpdateBoundVolume()
        {
            // add up the bbox of any Element3D's contained herein
            if (_children != null)
            {
                foreach (Node d in _children)
                {
                    if (d is IBoundVolume)
                    {
                        IBoundVolume bv = (IBoundVolume) d;
                        if (_box == null)
                        {
                            _box = bv.BoundingBox;
                        }
                        _box.Combine(bv.BoundingBox);
                    }
                }
            }
            // leaf node has no children.  all done.
            BoundVolumeIsDirty = false;
        }

        #endregion
    }
}