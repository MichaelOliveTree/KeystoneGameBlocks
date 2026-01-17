using System.Drawing;

namespace Keystone.SpatialNodes.Quadtree
{
    // Branch does in fact inherit from leaf in this case, but for derived
    // types of branches, you can either inherit from here or inherit from
    // another derivation of Leaf (e.g Sector : Leaf3d : Leaf )
    // So for instance if Leaf3d contains IObject3d collection and you want to create
    // a version of the branch type that does NOT contain IObject3d then you can
    // instead derive from Branch3d so you'd have e.g. Quadrant3dFree : Branch3d : Leaf3d : Leaf)
    // But for now we're just going to have Quadrant : Sector : Leaf3d : leaf)
    public abstract class Branch : Leaf
    {
        public QTreeNode[] Child = new QTreeNode[4];
        // TODO: publicly accessible to the var is bad because it can be redefined to have more than 4 elements

        public Branch(string name, RectangleF r, QTreeNode parent)
            : base(name, r, parent)
        {
        }
    }
}