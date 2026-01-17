using System;
using System.Drawing;
using Keystone.Resource;

namespace Keystone.SpatialNodes.Quadtree
{
    // Uses a recursive "BuidlTree()" method to instantiate children up to the prescribed depth.
    // Root node = depth 0.  The first quad divided nodes (the root node's 4 children) = depth 1
    public class TreeBuilder
    {
        private int _depth;
        private Type _leafType, _branchType;

        public TreeBuilder(int depth, Type leaf, Type branch)
        {
            if (depth <= 0) throw new ArgumentOutOfRangeException();
            _depth = depth;
            _leafType = leaf;
            _branchType = branch;
        }

        private RectangleF CreateChildBounds(RectangleF r, QUADRANT q)
        {
            float width, height;
            width = r.Width/2;
            height = r.Height/2;

            switch (q)
            {
                case QUADRANT.NE:
                    return new RectangleF(r.Left + width, r.Top, width, height);
                case QUADRANT.NW:
                    return new RectangleF(r.Left, r.Top, width, height);
                case QUADRANT.SE:
                    return new RectangleF(r.Left + width, r.Top + height, width, height);
                case QUADRANT.SW:
                    return new RectangleF(r.Left, r.Top + height, width, height);
                default:
                    return r;
            }
        }

        //Christian or George, is it possible to do something like instantiating an object based on just the type
        // without having to use some kind of switch?
        private Leaf CreateLeaf(RectangleF r, QTreeNode parent)
        {
            //switch (_leafType.Name) 
            //{
            //    case "Leaf3d" :
            //}
            //return _leafType.InvokeMember(leaf.TypeInitializer.Name);
            return new Sector(Repository.GetNewName(typeof (Sector)), r, parent);
        }

        private Branch CreateBranch(RectangleF r, QTreeNode parent)
        {
            //return _branchType.InvokeMember(leaf.TypeInitializer.Name);
            return new Quadrant(r, parent);
        }


        public void BuildTree(ref Branch currentNode)
        {
            if (currentNode == null) throw new ArgumentNullException();

            int currentDepth = currentNode.Depth + 1;

            if (currentDepth < _depth)
                //instantiate the 4 child branch nodes using the quartered dimensions of the current node
            {
                // using a for loop and casting ensures that the children are assigned to child(index) that matches the Quadrant enum value
                for (int i = 0; i < 4; i++)
                {
                    Branch child = CreateBranch(CreateChildBounds(currentNode.Bounds, (QUADRANT) i), currentNode);
                    child.Depth = currentDepth;
                    //currentNode.Child[i].Depth = currentDepth;
                    // recurse child
                    BuildTree(ref child);
                    currentNode.Child[i] = child;
                }
                //currentNode.child(QUADRANT.NE) = CreateBranch(CreateChildBounds(currentNode.Bounds, QUADRANT.NE), currentNode);
                //currentNode.child(QUADRANT.NW) = CreateBranch(CreateChildBounds(currentNode.Bounds, QUADRANT.NW), currentNode);
                //currentNode.child(QUADRANT.SE) = CreateBranch(CreateChildBounds(currentNode.Bounds, QUADRANT.SE), currentNode);
                //currentNode.child(QUADRANT.SW) = CreateBranch(CreateChildBounds(currentNode.Bounds, QUADRANT.SW), currentNode);

                // now loop through each child node recursively
                //for (int i = 0; i < 4; i++)
                //{
                //    BuildTree(ref (Branch)currentNode.Child[i]);
                //}
            }
            else 
                // instantiate leaf nodes
            {
                for (int i = 0; i < 4; i++)
                {
                    currentNode.Child[i] = CreateLeaf(CreateChildBounds(currentNode.Bounds, (QUADRANT) i), currentNode);
                    currentNode.Child[i].Depth = currentDepth;
                }

                //currentNode.child(QUADRANT.NE) = CreateLeaf(CreateChildBounds(currentNode.Bounds, QUADRANT.NE), currentNode);
                //currentNode.child(QUADRANT.NW) = CreateLeaf(CreateChildBounds(currentNode.Bounds, QUADRANT.NW), currentNode);
                //currentNode.child(QUADRANT.SE) = CreateLeaf(CreateChildBounds(currentNode.Bounds, QUADRANT.SE), currentNode);
                //currentNode.child(QUADRANT.SW) = CreateLeaf(CreateChildBounds(currentNode.Bounds, QUADRANT.SW), currentNode);
            }
        }
    }
}