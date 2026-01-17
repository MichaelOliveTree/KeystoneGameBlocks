using System;
using Keystone.SpatialNodes.Octree;


namespace Keystone.SpatialNodes
{
	
	public interface IStaticSpacialNode
	{
		
	}
	
	public interface IDynamicSpacialNode
	{
	}
	
    public interface ISpatialNode
    {

        EntityNode[] EntityNodes {get;}
        ISpatialNode[] Children {get;}

        void Add(EntityNode entityNode, bool forceRoot);
        void Add(EntityNode entityNode);
        void RemoveEntityNode(EntityNode entityNode);

        void OnEntityNode_Resized(EntityNode entityNode);

        void OnEntityNode_Moved(EntityNode entityNode);

        bool Visible { get; set; } // can disable rendering of entire spatial tree without disabling it

        //void OnEntityNode_Removed(EntityNode entityNode);

    }
}
