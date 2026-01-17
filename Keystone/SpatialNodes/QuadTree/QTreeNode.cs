using System;
using System.Drawing;
using Keystone.Elements;
using Keystone.Traversers;

namespace Keystone.SpatialNodes.Quadtree
{
    public enum QUADRANT : int
    {
        // the regular quadrants must range from 0 to 3 
        // since these values correspond to their subscripts in the node.Child[] arrays
        ROOT = -1,
        NE,
        SE,
        SW,
        NW
    }

    public abstract class QTreeNode : Node, ITraversable
    {
        private RectangleF _bounds;
        protected QUADRANT _quadrant;
        private int _depth;
        protected QTreeNode _parent;


        //Constructor accepts the bounds of this node
        public QTreeNode(string name, float x1, float y1, float x2, float y2, QTreeNode parent)
            : this(name, new RectangleF(x1, y1, x2 - x1, y2 - y1), parent)
        {
        }

        public QTreeNode(string name, RectangleF r, QTreeNode parent) : base(name)
        {
            if (r.Width <= 0) throw new ArgumentOutOfRangeException();
            if (r.Height <= 0) throw new ArgumentOutOfRangeException();
            if (parent != null)
            {
                _depth = parent.Depth + 1;
                _parent = parent;
            }
            else
            {
                _depth = 0;
                _quadrant = QUADRANT.ROOT;
            }
            _bounds = r;
        }

        ~QTreeNode()
        {
        }

        public int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        public QTreeNode Parent
        {
            get { return _parent; }
        }

        public QUADRANT Quadrant
        {
            get { return _quadrant; }
            set { _quadrant = value; }
        }

        public RectangleF Bounds
        {
            get { return _bounds; }
            set { _bounds = value; }
        }
    }
}