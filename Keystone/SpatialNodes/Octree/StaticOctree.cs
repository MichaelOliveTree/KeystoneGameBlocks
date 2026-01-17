using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Types;


namespace Keystone.SpatialNodes.Octree
{
	// http://www.flipcode.com/archives/Octree_Implementation.shtml
    /// <summary>
    /// A static, tight octree implementation. 
    /// static = children added/removed, but will never move 
    /// and thus not need to be moved to different octants.
    /// </summary>
    public class StaticOctree<T>
    {
        internal static int MAX_CHILD_COUNT = 8;
        
        /// <summary>
        /// Depth 1 = 2 octants along each axis (2^1)
		///	Depth 2 = 4 octants along each axis (2^2)
		///	Depth 3 = 8 octants along each axis (2^3)
		///	Depth 4 = 16 octants along each axis (2^4)
		///	Depth 5 = 32 octants along each axis (2^5)
		///	Depth 6 = 64 octants along each axis (2^6)
		///	NOTE: MaxDepth for StaticOctree _MUST_ be set such that they fully encompass #tiles on each axis 
		///	So for instance, for 32x32x32 tile Zones, the Depth must be set to 5.  That's 2^5 = 32.
        /// </summary>
        public uint MaxDepth;
        
        private BoundingBox mBox; 
        private Vector3d mSize;
    	private StaticOctant<T> mRoot;
    	
        internal static Vector3d[] BoundsOffsetTable = new Vector3d[] 
        {
                new Vector3d(-1, -1, -1),
                new Vector3d(+1, -1, -1),
                new Vector3d(-1, +1, -1),
                new Vector3d(+1, +1, -1),
                new Vector3d(-1, -1, +1),
                new Vector3d(+1, -1, +1),
                new Vector3d(-1, +1, +1),
                new Vector3d(+1, +1, +1)
        };
        
        
        public StaticOctree()
        {
        	mRoot = new StaticOctant<T>();
        }
        
        internal StaticOctant<T> Root 
        {
        	get {return mRoot;}
        }

        internal BoundingBox BoundingBox 
        { 
        	get {return mBox;} 
        	set 
        	{
        		mBox = value;
        		mSize = mBox.Max - mBox.Min;
        	}
        }
        
        internal Vector3d Size 
        { 
        	get {return mSize;}
        }
                
    	// sign vector where -1 is an octant -x,-y, and/or -z of parent center
		// and +1 is an octant +x, +y, and/or +z of parent center    	
        internal int[] LocalIndexToVector(int index)
        {
            // divide the index by  2 ^ depth

            int[] v = new int[3];
            if ((index & 1) >= 0) 
            	v[0] = 1;
            else 
            	v[0] = -1;

            if ((index & 2) >= 0) 
            	v[1] = 1;
            else 
            	v[1] = -1;

            if ((index & 4) >= 0) 
            	v[2] = 1;
            else 
            	v[2] = -1;

            return v;
        }

        // returns 0 - 7 child octant index 
        internal int LocalVectorToIndex(int[] v)
        {
	        int index = 0;	
            
            if (v[0] >= 0) index |= 1;
	        if (v[1] >= 0) index |= 2;
	        if (v[2] >= 0) index |= 4;	

            return index;
        }
        
        internal int LocalVectorToIndex(Vector3d v)
        {
        	return LocalVectorToIndex(new int[]{(int)v.x, (int)v.y, (int)v.z});
        }
        
        private StaticOctant<T> FindOctant (int index)
        {
        	StaticOctant<T> octant = mRoot;
        	
        	while (octant.Children != null || octant.Index != index)
        	{
        		// note: this line is actually getting a local vector from global index
    			int[] localVector = LocalIndexToVector(index);
				
        		int localIndex = LocalVectorToIndex(localVector);
        		
        		if (octant.Children[localIndex] == null) throw new Exception ("StaticOctree.FindOctant() - Child octant at index '" + localIndex.ToString() + "' should not be null.");
	            
	            octant = octant.Children[localIndex];
        	}
        	
        	return octant;
        }
        
        private bool NearlyEquals (Vector3d vec1, Vector3d vec2)
        {
        	return NearlyEquals (vec1.x, vec2.x) && NearlyEquals (vec1.y, vec2.y) && NearlyEquals (vec1.z, vec2.z);
        }
        
        private bool NearlyEquals (double val, double val2)
        {
        	double epsilon = 0.01;
        	double result = val - val2;
        	return Math.Abs (result) <= epsilon;
        }
        
        private StaticOctant<T> FindOctant (Vector3d position, bool create)
        {
            var center = this.mBox.Center;
            var halfSize = Size / 2.0d;


            StaticOctant<T> node = mRoot;

            int pow = 2;
            //int depth = 0;
            
            // IMPORTANT: If experiencing CULLING PROBLEMS remember that 
            // Structure.mInititialMinimeshCapacity must be high enough and
            // MaxOctreeDepth must be N where 2 ^ N = tileCountAlongAxis
            // Also remember that tileCount along all axis should be square
            // but that doesn't mean we have to support stacking/generation of terrain
            // above a certain height/depth.
            while (NearlyEquals(center, position) == false)
            //while (center.x != position.x || center.y != position.y || center.z != position.z)
            //while (center.x != position.x || center.z != position.z)
            {
            	Vector3d diff = position - center;
            	Vector3d sign;
            	sign.x = Math.Sign(diff.x);
            	if (sign.x == 0d) sign.x = 1d;
         
            	sign.y = Math.Sign(diff.y);
            	if (sign.y == 0d) sign.y = 1d;
            	sign.z = Math.Sign(diff.z);
            	if (sign.z == 0d) sign.z = 1d;
            	
            	int childOctantIndex = LocalVectorToIndex (sign);
            	Vector3d validate = BoundsOffsetTable[childOctantIndex];
            	Debug.Assert (sign.x == validate.x && sign.y == validate.y && sign.z == validate.z);
            	int globalIndex = -1; 
            	
                if (node.Children == null || node.Children[childOctantIndex] == null)
                {
                    if (create)
    	            {
	                	if (node.Children == null) 
	                		node.Children = new StaticOctant<T>[MAX_CHILD_COUNT];
                		
	                	node = node.Children[childOctantIndex] = new StaticOctant<T>(node, globalIndex, node.Depth + 1);
    	            }
	                else
                    {
	                	node = null;
                        break; 
	                }
                }
                else
                {
                    node = node.Children[childOctantIndex];
                }
                
               // depth++;
                //pow *= 2;
                halfSize /= 2d; // Size / pow;
                center += halfSize * sign;
            }

            return node;
        }

        private StaticOctant<T> FindOctant(Vector3i position)
        {
            StaticOctant<T> octant = mRoot;
            var center = this.mBox.Center;
            var halfSize = Size / 2.0d;
            
			int insertionDepth =  mRoot.Depth;
			
            for (int currentDepth = 0; currentDepth <= MaxDepth; ++currentDepth)
            {
                if (!Split(octant))  // if the octant cannot be split, this is as far as we can go
                    return octant;
                else
                {
                    /*
                        We can find the exact child without any comparisons
                        For example, we're looking for an octant at depth 3 with x,y,z = (2,1,3)
                        This will be a child of the octant at depth 2 with x,y,z = (1,0,2)

                        We take the convention that childOctants are layed out as:

                        local index              1D index
                        [(0,1,0) (1,1,0)]        [2 3]
                        [(0,0,0) (1,0,0)]        [0 1]
                                            =
                        [(0,1,1) (1,1,1)]        [6 7]
                        [(0,0,1) (1,0,1)]        [4 5]

						// TODO: are we talking about flattened indices below?
						// TODO: also instead of dividing/multiplying, we're talking about shifting left or right
                        To find the local index of an octant in the frame of it's direct parent, 
                        we have to divide the index by two.
                        To find the local index of an octant in the frame of it's parent x times up,
                        we have to divide the index by 2^x
                    */

                    //this generates the local index of the child octant at (currentDepth - 1)
                    int currentDepthX = position.X >> (insertionDepth - (currentDepth + 1));
                    int currentDepthY = position.Y >> (insertionDepth - (currentDepth + 1));
                    int currentDepthZ = position.Z >> (insertionDepth - (currentDepth + 1));
                    int globalIndex = currentDepthX + currentDepthY << 1 + currentDepthZ << 2;

                    int localIndex = LocalVectorToIndex(new int[] { currentDepthX, currentDepthY, currentDepthZ});

                    if (octant.Children[localIndex] == null) 
                    {
                        // create a box with half the diameter and offset to the parent's center
                        // according to it's octant index
                        Vector3d offset = BoundsOffsetTable[localIndex] * halfSize;
                        center = center + offset;
   
                        // NOTE: we don't create all 8 children, we only create the one we need
                        octant.Children[localIndex] =
                            new StaticOctant<T>(octant, globalIndex, currentDepth + 1);
                    }
                    octant = octant.Children[localIndex];
                }
            }

            //if we make it here, we're at the minimum depth. and we found our octant
            return octant;
        }
        
        private int GetChildIndex(StaticOctant<T> child, StaticOctant<T> parent)
        {
        	if (child == null || parent == null) throw new ArgumentNullException();
        	
        	for (int i = 0; i < parent.Children.Length; i++)
        		if (parent.Children[i] == child) return i;
        	
        	throw new Exception();
        }
        
        public void Add(T data, Vector3d position, Vector3i offset)
        {
        	StaticOctant<T> foundOctant = FindOctant(offset);
            if (foundOctant == null) throw new Exception("StaticOctree.Add() - Could not find suitable octant to insert data.");
            
            int childIndex = GetChildIndex (foundOctant, foundOctant.Parent);
            
            //System.Diagnostics.Debug.WriteLine ("StaticOctree.Add() - " + position.ToString() + " FOUND and inserted at childIndex " + childIndex);
            
            //System.Diagnostics.Debug.WriteLine(string.Format("StaticOctree.Add () - Adding data to node {0} at {1}, depth {2}", foundOctant.Index, position, foundOctant.Depth ));
            foundOctant.Position = position;
            foundOctant.Data = data;
        }
        
        public void Add(T data, Vector3d position)
        {
        	bool create = true;
            StaticOctant<T> foundOctant = FindOctant(position, create);
            if (foundOctant == null) throw new Exception("StaticOctree.Add() - Could not find suitable octant to insert data.");
                        
            int childIndex = GetChildIndex (foundOctant, foundOctant.Parent);
            
            //System.Diagnostics.Debug.WriteLine ("StaticOctree.Add() - " + position.ToString() + " FOUND and inserted at childIndex " + childIndex);
            //System.Diagnostics.Debug.WriteLine(string.Format("StaticOctree.Add () - Adding data to node {0} at {1}, depth {2}", foundOctant.Index, position, foundOctant.Depth ));
            foundOctant.Position = position;
            foundOctant.Data = data;
        }
          
        public void Remove(Vector3d position)
        {
        	bool create = false;
            StaticOctant<T> foundOctant = FindOctant(position, create);

            if (foundOctant != null && foundOctant.Children == null)
            {
            	int childIndex = GetChildIndex (foundOctant, foundOctant.Parent);
            
            	//System.Diagnostics.Debug.WriteLine ("StaticOctree.Remove() - " + position.ToString() + " FOUND and removed at childIndex " + childIndex);
            	
            	Vector3d sign = foundOctant.Parent.Position - position;
            	int childOctantIndex = LocalVectorToIndex (sign);
            	
                // Collapse branch if possible
	        	if (foundOctant.Parent == null) 
	        	{
	            	foundOctant = null;
	        		return;
	        	}
	        	CollapseBranch(foundOctant.Parent, foundOctant);
            	foundOctant = null;
            }
            else
            	System.Diagnostics.Debug.WriteLine ("StaticOctree.Remove() - " + position.ToString() + " NOT found.");
        }
        
        // recursively collapse empty branches
        private void CollapseBranch(StaticOctant<T> parent, StaticOctant<T> childOctant )
        {
            int nullCount = 0;
            for (int i = 0; i < parent.Children.Length; i++)
                if (parent.Children[i] == childOctant)
                {
                    parent.Children[i].Parent = null; // or mChildOctants[i].Dispose() ?
                    parent.Children[i] = null;
                    nullCount++;
                }
                else if (parent.Children[i] == null)
                    nullCount++;

            // if all child octants are null, we can delete the entire child array
            // and potentially it's parents too
            if (nullCount == StaticOctree<T>.MAX_CHILD_COUNT)
            {
                parent.Children = null;
                if (childOctant.IsRoot == false && childOctant.Data.Equals(default(T))) // TODO: is default(T) ok here?  is default(T) ever a legal position?  I think not since it's perfectly centered and octree divides center by 8 such that all octants are offset from center other than root, but we ignore root here
                	// recurse upwards
                	CollapseBranch(parent.Parent, parent);
            }
        }
        
        private bool Split(StaticOctant<T> octant)
        {
            // cannot split because we're at max depth
            if (octant.Depth == this.MaxDepth) 
                return false;

            // we are already split
            if (octant.Children != null)
                return true;

            // initialize the array but do not instance or assign Octants to the array subscripts
            octant.Children = new StaticOctant<T>[8];
            return true;
        }
    }
    
    internal class StaticOctant<T> 
    {
        private int _depth;
        // _globalIndex is specific to each depth and contains x,y,z offset at that depth
		// and is useful for finding neighbors (which we may never do and just always 
		// move EntityNodes by re-inserting starting at root)
        private int _globalIndex;   

        private StaticOctant<T> mParent;  
        private StaticOctant<T>[] mChildOctants;

        internal Vector3d Position;
        private T mData;
		internal IntersectResult FrustumContainment;

        public StaticOctant(StaticOctant<T> parent, int index, int depth)
            : this()
        {
            _globalIndex = index;
            _depth = depth;
            mParent = parent;
            //System.Diagnostics.Debug.WriteLine("StaticOctant() -- Created at index " + index.ToString());
        }

        public StaticOctant()
        {
        }

        ~StaticOctant()
        {
        }


        public StaticOctant<T> Parent { get { return mParent; } set { mParent = value; } }
                
        public bool IsRoot { get { return mParent == null;  } }
        
        public bool IsLeaf { get { return mChildOctants == null; } }

        public int Index
        {
            get { return _globalIndex; }
        }
        

        internal int Depth
        {
            get { return _depth; }
        }

        public StaticOctant<T>[] Children
        {
            get { return mChildOctants; }
            set { mChildOctants = value;}
        }

        public T Data
        {
            get 
            {
                return mData;
            }
            set { mData = value;}
        }
    }
}
