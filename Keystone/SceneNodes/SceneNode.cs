using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Enums;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Extensions;

namespace Keystone.SceneNodes
{
    /// <summary>
    /// A sceneNode represents the _hierarchical_ spatial information about an Entity
    /// in it's most immediate Region's coordinate system.
    /// </summary>
    public abstract class SceneNode : ITraversable, IBoundVolume, IDisposable
    {
        protected SceneNode _parent;
        protected SceneNode[] _children;

        protected BoundingBox _box;
        protected BoundingSphere _sphere;


        protected Enums.ChangeStates _changeStates;

        internal SceneNode()
        {
            SetChangeFlags(Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
        }

        #region ITraversable Members
        // TODO: these should return object then i can simply pass a new state object
        public virtual object Traverse(Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion


        public ChangeStates ChangeFlags
        {
            get { return _changeStates; }
        }


        // TODO: for the following dec.4.2012 to be done, i have to verify that the order in which
        //       a child EntityNodes's bbox is updated is before  the parent's is updated so that the parent
        //       can benefit from the most up to date position of the child.  One way to guarantee this
        //       perhaps is to have SceneNode boxes updated from a direct 
        // Dec.4.2012 - Hypnotron - Remove ChangeSource param? BoundingBoxDirty is only 
        // flag SceneNode's use since it's for lazy update of the bounding volumes.
        // SceneNodes do not recursively notify their own parents or children.  They don't have to 
        // because the Entity nodes they host will do so as it is the Entity's that take on recursive
        // notification responsibilities.
        // WAIT, the question then is, is the order such that when say a Star moves
        // and it's child entity Planet is notifed by the Star entity, both call for their
        // own boundingboxes to be updated, but is the Star's SceneNode box updated before
        // the Planet's SceneNode box has been updated which means the Star now does not fully
        // contain the child.
        // note: there's no need to propogate anything because hierarchical entities
        // already handle propogation and then call their own this.SceneNode.SetChangeFlags().
        internal void SetChangeFlags(ChangeStates flags, ChangeSource source)
        {
            _changeStates |= flags;
        }

        internal void ToggleChangeFlags(ChangeStates flags)
        {
            _changeStates ^= flags;
            // NOTE: For simplicitiy, we dont ever use this "Toggle" option
            throw new Exception("SceneNode.ToggleChangeFlags() - DO NOT USE THIS METHOD.  USE SET, DISABLE and CLEAR only with SET being the only Version that NOTIFIES the LISTENER.");
        }
        internal void DisableChangeFlags(ChangeStates flags)
        {
            _changeStates &= ~flags;
        }

        protected void ClearChangeFlags()
        {
            _changeStates = ChangeStates.None;
        }

        // TODO: I should implement an overloaded version of Query that traverses for Entities that lay within a bounding box or sphere with potential also of matching a "match" predicate.
        //       Multi-threading of the query would be ideal.
		/// <summary>
        /// Looks for Regions/Entities that are in descendant RegionNodes or EntityNodes 
        /// that match the specified predicate.
        /// </summary>
        /// <param name="recurse"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public virtual List<Entities.Entity> Query(bool recurse, Predicate<Entities.Entity> match)
        {
        	if (match == null) throw new ArgumentNullException("SceneNode.Query() - match cannot be null.");
            
        	List<Entities.Entity> results = new List<Entities.Entity>();
        	
        	RegionNode regionNode = this as RegionNode;
        	Portals.EntityNode entityNode = this as Portals.EntityNode;
        	if (regionNode != null)
        	{
        		if (match(regionNode.Region))
	                results.Add(regionNode.Region);
        	}
        	else if (entityNode != null)
        	{
        		if (match(entityNode.Entity))
	                results.Add(entityNode.Entity);
        	}
        				
        	if (_children == null || _children.Length == 0) return results;

            // all direct children of RegionNode will be EntityNodes possibly even nested RegionNodes
            for (int i = 0; i < _children.Length; i++)
            {
            	SceneNode childSceneNode = _children[i];
            	Entities.Entity entity = null;
            	
            	RegionNode rn = childSceneNode as RegionNode;
            	Portals.EntityNode en = childSceneNode as Portals.EntityNode;
            	if (rn != null)
            	{
            		entity = rn.Region;
            	}
            	else if (en != null)
            	{
            		entity = en.Entity;
            	}
            	else continue;
            	
                if (match(entity))
                    results.Add(entity);

                // interior traversal has to take special path since no descendant SceneNode from current is available
                Entities.Container container = entity as Entities.Container;
                if (container != null)
                {
                    if (container.Interior != null)
                    {
                        // recurse interior components
                        if (container.Interior.RegionNode != null)
                        {
                            List<Entities.Entity> nestedResults = container.Interior.RegionNode.Query(recurse, match);
                            if (nestedResults != null)
                                results.AddRange(nestedResults);
                        }
                    }
                }
                else if (recurse) // recurse children's children
                {
                	if (childSceneNode.Children != null)
                    {
                        // NOTE: We recurse the SceneNodes, not child Entities
                        for (int j = 0; j < childSceneNode.Children.Length; j++)
                        {
                            List<Entities.Entity> nestedResults = childSceneNode.Children[j].Query(recurse, match);
                            if (nestedResults != null)
                                results.AddRange(nestedResults);
                        }
                    }
                }
            }

            if (results.Count == 0) return null;
            return results;
        }

        #region IGroup Members
        public virtual void AddChild(SceneNode child)
        {
            // NOTE: the child's desired world position and world bounding volume must already be corrrect.
            //       the world position is the child's entities position + all parent entity position combined.
            //       The combined parent positions result in a translation vector that we can apply to the min/max of the
            //       boundingvolume to get the worldbounding volume
            int count = 0;
            if (_children != null){
            	count = _children.Length;
            	if (ChildrenContains (child))  throw new ArgumentException("SceneNode.AddChild() - Child SceneNode already exists.");
            }
            child.Parent = this;
            _children = _children.ArrayAppend(child);

            // NOTE: Is there a scenario where a SceneNode is added and it's child Entity
            // is not added to it's Entity parent such that the parent Entity does not 
            // propogate a BoundingBoxDirty flag?
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
        }

        private bool ChildrenContains (SceneNode child)	
        {
        	if (_children == null) return false;
        	for (int i = 0; i < _children.Length; i++)
        		if (_children[i] == child)
        			return true;
        	
        	return false;
        }
        
        public SceneNode Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public SceneNode[] Children
        {
            get
            {
                return _children;
            }
        }

        public int ChildCount
        {
            get
            {
                if (_children == null) return 0;
                return _children.Length;
            }
        }

        public virtual void RemoveChildren()
        {
            while (_children != null)
            {
                RemoveChild(_children[0]);
            }
        }

        public virtual void RemoveChild(SceneNode child)
        {
            try
            {
                _children = _children.ArrayRemove (child);
                child.Parent = null;
                if (_children != null)
                    if (_children.Length == 0) _children = null;

                // NOTE: Is there a scenario where a SceneNode is removed and it's child Entity
                // is not removed from it's Entity parent such that the parent Entity does not 
                // propogate a BoundingBoxDirty flag?
                SetChangeFlags(Enums.ChangeStates.GeometryRemoved | ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
            }
            catch
            {
            }
        }
        #endregion

        #region MoveableNode Members
        // for some sceneNode types like Octree nodes, the position is the center of the
        // bounding box
        public virtual Vector3d Position
        {
            get 
            { 
                return BoundingBox.Center; 
            }
        }

        #endregion

        #region IBoundVolume Members
        public virtual BoundingBox BoundingBox
        {
            get
            {
                if (BoundVolumeIsDirty)
                    UpdateBoundVolume();

                return _box;
            }
        }

        public virtual BoundingSphere BoundingSphere
        {
            get
            {
                if (BoundVolumeIsDirty)
                    UpdateBoundVolume();

                return _sphere;
            }
        }

        public virtual bool BoundVolumeIsDirty
        {
            get
            {
                return ((_changeStates &
                         (Enums.ChangeStates.BoundingBox_TranslatedOnly | Enums.ChangeStates.BoundingBoxDirty)) !=
                        Enums.ChangeStates.None);
            }
        }

        protected abstract void UpdateBoundVolume();

        #endregion

        #region IDisposable Members

        public abstract void Dispose();

        #endregion
    }
}