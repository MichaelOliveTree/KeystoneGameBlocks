using System;
using System.Drawing;
using Keystone.Traversers;

namespace Keystone.SpatialNodes.Quadtree
{
    public abstract class Leaf : QTreeNode
    {
        public Leaf(string name, RectangleF r, QTreeNode parent)
            : base(name, r, parent)
        {
        }

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }
    }
}