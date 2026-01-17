using System;
using System.Collections.Generic;
using System.Drawing;
using Keystone.Elements;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.Types;

namespace Keystone.SpatialNodes.Quadtree
{
    // Quadrant extends a regular QTree branch into a scene element that can
    // also contain other Node types.  Hrm.. this sucks because id rather
    // have Quadrant inherit from Group and then also implement IBranchNode or something
    // but if it inherits from Group, then it cant inherit from QTreeNode.
    // so instead, we'd have to make Group into IGroup and then have a Group inherit Node and implement IGroup
    // argh crap... 
    public class Quadrant : Branch3d, IGroup
    {
        public override object Traverse(ITraverser t, object data)
        {
            return t.Apply(this, data);
        }

        public Quadrant(RectangleF r, QTreeNode parent)
            : base(Repository.GetNewName(typeof (Quadrant)), r, parent)
        {
            _box = new BoundingBox(new Vector3d(Bounds.X + Bounds.Width, Bounds.Width, Bounds.Y + Bounds.Height),
                                   new Vector3d(Bounds.X, -Bounds.Width, Bounds.Y));
        }

        #region IGroup Members
        private List<Node> _children;

        public virtual void AddChild(Node child)
        {
            if (_children == null) _children = new List<Node>();

            // same node cannot be added twice
            if (_children.Contains(child)) throw new ArgumentException("Node already exists in collection");
            
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
                _children.Remove(child);
                child.RemoveParent(this);
                if (_children.Count == 0) _children = null;
                
                BoundVolumeIsDirty = true;
                // ChangeSource.Child will ensure this notification is sent upwards
                SetChangeFlags(Keystone.Enums.ChangeStates.ChildNodeRemoved, Keystone.Enums.ChangeSource.Child);

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
            // add up the bbox of any IBoundVolume Node's contained herein
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
                        if (_box != null) _box.Combine(bv.BoundingBox);
                    }
                }
            }

            // combine those of all children
            foreach (QTreeNode node in Child)
            {
                IBoundVolume tmp;
                BoundingBox box;
                if (node is Branch3d || node is Leaf3d)
                {
                    tmp = (IBoundVolume) node;
                    box = tmp.BoundingBox;

                    if (_box == null) _box = box;
                    if (_box != null) _box.Combine(box);
                }
            }
            BoundVolumeIsDirty = false;
        }

        #endregion
    }
}