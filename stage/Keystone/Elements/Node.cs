using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using Keystone.Enums;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.States;
using Keystone.Extensions;

namespace Keystone.Elements
{
    public abstract class Node : ResourceBase, ITraversable
    {
        [Flags]
        public enum NodeFlags : byte
        {
            None = 0,
            Enable = 1 << 0,
            Serializable = 1 << 1,
            Shareable = 1 << 2, // Make sure for this flag, we ignore save xml value in the flag.
            ForceSerializeSeperate = 1 << 3, // this is usually set by server during galaxy generation.  it is not meant to be modified via user plugin.
            MissionObject = 1 << 4, // mission objects like spawnpoints do not get serialized to the scene, only to the missionNN.xml in the scenes\\SceneName\\missions\\ folder
            All = byte.MaxValue
        }

        protected List<IGroup> mParents;
        protected ChangeStates mChangeStates;
        protected NodeFlags mNodeFlags;

        protected string mFriendlyName;
        protected string mTag; // gamebryo tags are like classifications/categories that can be comma delimited used to
                               // query for various objects during edit time or deign time

                               
        protected Node(string id)
            : base(id)
        {
            Serializable = true;
            Shareable = false; // by default.  we ignore any changes from save file for this.
            Enable = true;
            mFriendlyName = TypeName.ToUpper(); // TODO: not sure if assigning a default name is good idea.  If anything
                                                // app side should do this 
        }

        /// <summary>
        /// Duplicate for our scene Nodes is not like Cloning or Deep Cloning.
        /// It is instead a method of copying all nodes that are copyable, and sharing
        /// the children that are shareable.  
        /// </summary>
        /// <param name="recurseChildren"></param>
        /// <param name="neverShare">Whether shareable resources like Meshes, Textures should never be shared.  Usually this argument is False since we do want to share resources.</param>
        /// <returns></returns>
        public virtual Node Clone(string cloneID, bool recurseChildren, bool neverShare, bool delayResourceLoading = true)
        {
            // TODO: when im cloning an object that is tied to the AsssetPlacementTool, then when i cancel the AssetPlacementTool before
            //       the cloned Entity is fully loaded, it will lose access to those child Entities and fail to add them to the Interior.
            //       I'm not sure why its only the Interior nodes that are being lost and not some of the Exterior nodes like the Turrets and Engines
            if ((neverShare == false && Shareable == true) || (Shareable == true && this is IPageableTVNode))
            {
                // This node can be shared so we can just return it instead of a deep cloned copy.
                // note: children will always be returned with this because
                // they are attached to this node if it's an IGroup node with children.
                // Thus if you do not want children, you must neverShare = true and recurseChildren == false
                return this;
            }
            

            Node copy = null;
            try 
            {
	            copy = Resource.Repository.Create (cloneID, this.TypeName );
	
	            Settings.PropertySpec[] tmp = GetProperties(false);
	            // remove the property that contains the old "id" so that we do not overwrite the cloned
	            // copy's unique ID with that of the original.  This causes havok in the Repository cache
	            // because the key will now no longer necessarily refer to the instance we desire.
	            Settings.PropertySpec[] copiedPropertiesWithoutID = tmp.ArraySlice((uint)tmp.Length - 1);
	
	            // NOTE: We do NOT attempt to maintain assign as "ref" on the clone the ID
	            // of this node that is being cloned.  That is up to the caller.
	            // In the meantime however, if a "ref" value is set in the entity being cloned we will
	            // obviously be copying it to the clone just as any other property is copied.
	            copy.SetProperties(copiedPropertiesWithoutID);

                // NOTE: When we are saving a prefab we clone first and use delayResourceLoading = true.  
                // This is critical for performance when saving Vehicles for example where we dont need to load the Interior
                if (delayResourceLoading == false && copy is IPageableTVNode)
                {
                    Keystone.IO.PagerBase.LoadTVResource(copy, false); // we don't have to recurse here because we do that belowe
                    // todo: do we need to reset the coopiedPropertiesWithoutID "customproperties" after we've loaded the script.  NOTE: we cannot LoadTVResource() earlier than here because we need to grab the dbpath from the properties
                }//((IPageableTVNode)copy).LoadTVResource(); July.25.2014 - switched to LoadTVResourceSynchronously() with recurse == true. see if it breaks anything


                if (recurseChildren == true)
	            {
	                if (this is IGroup)
	                {
	                    IGroup g = (IGroup)this;
	                    if (g.ChildCount > 0)
	                    {
	                        SuperSetter setter = new SuperSetter(copy);
	                        for (int i = 0; i < g.Children.Length; i++)
	                        {
                                ////if (g is Entities.Entity && ((Entities.Entity)g).Name == "tactical")
                                ////    System.Diagnostics.Debug.WriteLine("Removing Script");
                                ////if (g is Portals.Interior)
                                ////    Debug.WriteLine("Interior");

                                // we dont have to clone DomainObjects or Footprints because those resources
                                // will load themselves using existing resources if applicable
                                if (g.Children[i].Serializable == false)
                                {
                                    // todo: if this is a Entity with CustomProperties, we do need to deerialize the properties to the Entity
                                    // todo: the main probelm sis NOT LETTING THE SCRIPT TO FALL OUT OF SCOPE BEFORE WE FALL OUT OF SCOPE


	                            	// nothing to do here.  If the node is not serializable then it is not cloneable either. 
                                  
                                    // However, the Script node which is not serialable stores its "custom" values here in the Entity
                                    // so to preserve those custom property values, we need to NOT skip them, but 

                                    
                                    // Typically such nodes get auto created by their parent node automatically.
	                            	// eg. a scriptnode will get automatically created by it's parent entity during entity.LoadTVResource()
	                            	// However, be aware that if resource loading is delayed until AddChild() which triggers
	                            	// QueuePageableResource in Repository.IncrementRef(), then some actions against the Entity
	                            	// when performed before resources have loaded may not behave properly.
	                            	continue;
	                            }
	
	                            // TODO: i'm not sure here if we should just create random name here
	                            //       and also whether if this particular child is shareable == false whether
	                            //       we should create a REF link to the new clonedChild because these are nested children
	                            //       and the calling method wont have control here until after the fact
	                            string clonedChildID = Repository.GetNewName(g.Children[i].TypeName);
                                // NOTE: This next line is Recursive to clone down the current child
	                            Node clonedChild = g.Children[i].Clone(clonedChildID, recurseChildren, neverShare, delayResourceLoading);
	                            // ?? clonedChild.REF = g.Children[i].ID; 
	                            setter.Apply(clonedChild);
	                        }
	                    }
	                }
	            }
	

	        	
            }
            catch (Exception ex)
            {
            	System.Diagnostics.Debug.WriteLine ("Node.Clone() - ERROR: " + ex.Message);
            }
            
            return copy;
        }

        #region ITraversable Members
        public abstract object Traverse(ITraverser target, object data);
        internal abstract ChildSetter GetChildSetter();
        #endregion
        
        #region ResourceBase members
        /// <summary>
        /// TODO: in addition to specOnly, maybe I should add a param bool capture that will store the previous
        /// values for the specified properties...  so that we can implement unrolling of states.  Perhaps this would 
        /// belong in SetProperties not in GetProperties?
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[2 + tmp.Length];
            tmp.CopyTo(properties, 2);

            properties[0] = new Settings.PropertySpec("flags", typeof (byte).Name);
            properties[1] = new Settings.PropertySpec("name", typeof(string).Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = (byte) mNodeFlags;
                properties[1].DefaultValue = mFriendlyName;
            }

            return properties;
        }

        // TODO: what about having SetProperties() return Rules[] as broken rules
        // so we can easily apply buiness rules?  
        // aka: SetState vs  PerformBehavior
        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].DefaultValue == null) continue;
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "flags":
                        // NOTE: calling the full property will allow any overridden 
                        // "Enable" to be called such as with Light.css
                        mNodeFlags = (NodeFlags)(byte)properties[i].DefaultValue;

                        break;
                    case "name":
                        mFriendlyName = (string)properties[i].DefaultValue;
                        break;
                }
            }
        }
        #endregion 

        #region Node Flags
        public virtual bool GetFlagValue(byte flag)
        {
            switch ((NodeFlags)flag)
            {
                case NodeFlags.Enable:
                    return (mNodeFlags & NodeFlags.Enable) == NodeFlags.Enable;
                case NodeFlags.Serializable:
                    return (mNodeFlags & NodeFlags.Serializable) == NodeFlags.Serializable;
                case NodeFlags.Shareable:
                    return (mNodeFlags & NodeFlags.Shareable) == NodeFlags.Shareable;
                case NodeFlags.ForceSerializeSeperate:
                    return (mNodeFlags & NodeFlags.ForceSerializeSeperate) == NodeFlags.ForceSerializeSeperate;
                default:
                    throw new ArgumentOutOfRangeException("Node flag '" + flag.ToString() + "' is undefined.");
            }
        }

        public virtual bool GetFlagValue(string flagName)
        {
            switch (flagName)
            {
                case "enable":
                    return (mNodeFlags & NodeFlags.Enable) == NodeFlags.Enable;
                case "shareable":
                    return (mNodeFlags & NodeFlags.Shareable) == NodeFlags.Shareable;
                case "serializable":
                    return (mNodeFlags & NodeFlags.Serializable) == NodeFlags.Serializable;
                case "forceserializeseperate":
                    return (mNodeFlags & NodeFlags.ForceSerializeSeperate) == NodeFlags.ForceSerializeSeperate;

                default:
#if DEBUG
                    throw new ArgumentOutOfRangeException("Node flag '" + flagName + "' is undefined.");
#endif
					return false;                    
				break;
            }
        }

        public virtual void SetFlagValue(byte flag, bool value)
        {
            switch ((NodeFlags)flag)
            {
                case NodeFlags.Enable: // TODO: seems again more model type flag like UseInstancing and not Entity flag
                    if (value)
                        mNodeFlags |= NodeFlags.Enable;
                    else
                        mNodeFlags &= ~NodeFlags.Enable;
                    break;

                case NodeFlags.Serializable:
                    if (value)
                        mNodeFlags |= NodeFlags.Serializable;
                    else
                        mNodeFlags &= ~NodeFlags.Serializable;
                    break;
                case NodeFlags.Shareable :
                    if (value)
                        mNodeFlags |= NodeFlags.Shareable;
                    else
                        mNodeFlags &= ~NodeFlags.Shareable;
                    break;
                case NodeFlags.ForceSerializeSeperate:
                    if (value)
                        mNodeFlags |= NodeFlags.ForceSerializeSeperate;
                    else
                        mNodeFlags &= ~NodeFlags.ForceSerializeSeperate;
                    break;
                default:
                    //throw new ArgumentOutOfRangeException("Node flag '" + flag.ToString() + "' is undefined.");
                    if (value)
                        mNodeFlags |= (NodeFlags)flag;
                    else
                        mNodeFlags &= ~(NodeFlags)flag;
                    break;
            }
        }

        public virtual void SetFlagValue(string flagName, bool value)
        {
            switch (flagName)
            {
                case "enable":
                    SetFlagValue((byte)NodeFlags.Enable, value);
                    break;
                case "shareable":
                    SetFlagValue((byte)NodeFlags.Shareable, value);
                    break;
                case "serializable":
                    SetFlagValue((byte)NodeFlags.Serializable, value);
                    break;
                case "forceserializeseperate":
                    SetFlagValue((byte)NodeFlags.ForceSerializeSeperate, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Node flag '" + flagName + "' is undefined.");
            }
        }
        #endregion
        

        public bool IsConnectedToScene ()
        {
        	if (this is Keystone.Entities.Entity)
        	{
        		Entities.Entity entity = (Entities.Entity) this;
        		return entity.Scene != null;
        	}
        	
        	if (mParents == null) return false;
        	
        	for (int i = 0; i < mParents.Count; i++)
        	{
        		Node parent = (Node)mParents[i];
        		// recursive call
        		if (parent.IsConnectedToScene())
        			return true;
        	}
        	
        	return false;
        }
        
        public virtual Elements.Node[] Query(bool recurse, Predicate<Elements.Node> match)
        {
            List<Elements.Node> results = new List<Elements.Node>();
            if (match != null && match(this))
                results.Add(this);

            IGroup g = this as IGroup;
            if (g != null && recurse)
            {
                if (g.ChildCount > 0) 
                {
                	Elements.Node[] children = g.Children;
	                for (int i = 0; i < children.Length; i++)
	                {
	                    Elements.Node[] nestedResults = children[i].Query(recurse, match);
	                    if (nestedResults != null)
	                        results.AddRange(nestedResults);
	                }
                }
            }          

            if (results.Count == 0) return null;
            return results.ToArray();
        }

        /// <summary>
        /// Name is the "friendly name" used for assigning animation targets and trigger targets by name
        /// because our GUID "ID" property is not user friendly.
        /// Name will be it's TYPENAME in CAPS by default. (CAPS will encourage it's renaming)
        /// Name can be null or empty.
        /// Name is stored during property reads and writes 
        /// </summary>
        /// <remarks>
        /// Name must be unique within the scope of the nearest
        /// Entity it's descended from. 
        /// If there is a name collision when adding a node
        /// the newly added node will have it's friendly name reverted back to default
        /// In other words, you can have
        /// two or more instances of an AK-47, both with their own Material nodes
        /// named "GunMetal" for instance.  
        /// </remarks>
        public virtual string Name
        {
            get { return mFriendlyName; }
            set
            {
                if (this is Vehicles.Vehicle)
                    System.Diagnostics.Debug.WriteLine("vehicle");
                mFriendlyName = value;
            }
        }


        // gamebryo tags are like classifications/categories that can be comma delimited used to
        // query for various objects during edit time or deign time
        public virtual string Tag
        {
            get { return mTag; }
            set { mTag = value; }
        }
        // TODO: i believe all these properties should be internal.
        //       node properties from app/script should only be allowed through
        //       get/set properties.
        public virtual bool Enable
        {
            // when enabled, further evaluation of child nodes of this Node is stopped.
            get { return (mNodeFlags & NodeFlags.Enable) == NodeFlags.Enable; }
            set 
            {
                if (value)
                    mNodeFlags |= NodeFlags.Enable;
                else
                    mNodeFlags &= ~NodeFlags.Enable;
            }
        }

        public virtual bool Serializable
        {
            // when DISabled, serialization of entire Node is skipped. I should probably just add a seperate flag for serialization only as seperate single .kdgbentity  
            get { return (mNodeFlags & NodeFlags.Serializable) == NodeFlags.Serializable; }
            set
            {
                if (value)
                    mNodeFlags |= NodeFlags.Serializable;
                else
                    mNodeFlags &= ~NodeFlags.Serializable;
            }
        }

        public virtual bool Shareable
        {
            // when DIS-abled, this node cannot be used as a shared resource.
            // NOTE: I believe this "Shareable" node concept is obsolete but for now ill keep it.
            get { return (mNodeFlags & NodeFlags.Shareable) == NodeFlags.Shareable; }
            set
            {
                if (value)
                    mNodeFlags |= NodeFlags.Shareable;
                else
                    mNodeFlags &= ~NodeFlags.Shareable;
            }
        }

        public virtual void AddParent(IGroup parent)
        {
            if (mParents == null) mParents = new List<IGroup>();

            // same node cannot be added twice
            if (mParents.Contains(parent)) throw new ArgumentException("Node.AddParent() - Parent node already exists.");
            mParents.Add(parent);
            
            Repository.IncrementRef(this);
        }

        public virtual void RemoveParent(IGroup parent)
        {
            try
            {
                mParents.Remove(parent);
                if (mParents.Count == 0) mParents = null;

                // we only remove children of this node, if this node has no more parents 
                // AND the node's own reference count == 0 thus
                // indicating that this node is removed from the scene completely.
                // NOTE: the call to RemoveChildren() only occurs from here.  It does not get recursively
                // called from within .RemoveChildren() or .RemoveChild().  This is why this works
                if (RefCount == 0 && (mParents == null || mParents.Count == 0))
                    if (this is IGroup)
                        ((IGroup)this).RemoveChildren();
            }
            catch
            {
            }
        }

        
        /// <summary>
        /// Finds the ancestry index of a particular descendant node with respect to a starting ancestral node.
        /// </summary>
        /// <param name="ancestor"></param>
        /// <param name="descendant"></param>
        /// <param name="startingCounter"></param>
        /// <returns></returns>
        public static bool GetNodeAncestryIndex(IGroup ancestor, Node descendant, ref int startingCounter)
        {
            if (ancestor.ChildCount == 0) return false;

            // find the genelogy index with respect to the ancestor
            foreach (Node child in ancestor.Children)
            {
                startingCounter++;
                if (child == descendant) return true;

                if (child is IGroup)
                {
                    // recurse
                    if (GetNodeAncestryIndex((IGroup)child, descendant, ref startingCounter))
                        return true;
                }

            }

            return false;
        }

        public static bool ChildNodeIsDescendantOf (string childNodeID, string elderNodeID)
        {
            Node nodeToCompare = (Node)Keystone.Resource.Repository.Get(childNodeID);
            if (nodeToCompare == null) throw new ArgumentOutOfRangeException();
            
            return nodeToCompare.IsDescendantOf (elderNodeID);
        }
        
        public bool IsDescendantOf(string elderNode)
        {
            Node start = (Node)Repository.Get(elderNode);
            return IsDescendantOf(start);
        }

        public bool IsDescendantOf(Node elderNode)
        {
            // impossible to be a descendant if the elder node is childless
            if (elderNode == null || elderNode is IGroup == false) return false;

            if (((Node)elderNode).ID == this.ID) return true;

            IGroup group = (IGroup)elderNode;

            Node[] children = group.Children;
            if (children == null || children.Length == 0) return false; // impossible to be a descendant if there are no more children

            // iterate through all children and compare with childNodeID and if match, return true 
            for (int i = 0; i < children.Length; i++)
            {
                Node child = children[i];
                if (child == this) return true;

                if (child is IGroup)
                    if (child.IsDescendantOf (elderNode))
                        return true; // keep searching if false, otherwise return true
            }

            //// iterate through all children and compare with childNodeID and if match, return true 
            //if (group.ChildCount == 0) return false; // impossible to be a descendant if there are no more children

            //foreach (Node node in group.Children)
            //{
            //    if (node == this) return true;

            //    // recurse
            //    if (node is IGroup)
            //        if (node.IsDescendantOf(elderNode))
            //            return true; // keep searching if false, otherwise return true
            //}

            // still here then we didn't find it
            return false;
        }


        public ChangeStates ChangeFlags
        {
            get { return mChangeStates; }
        }

        internal void SetChangeFlags(ChangeStates flags, ChangeSource source)
        {
            // TODO:checking for existing flags is broken.
            // If this is so, it is because our lazy update is broken...
            // we should only perform actions based on flags during Update and Render()
            // or access of a property like BoundingVolume during Update.
            // but otherwise, checking for existing flags and skipping SHOULD WORK. must fix eventually

            // if any of the flags being passed in are already set, we skip propogate
            //ChangeStates newFlags = _changeStates ^ flags; // gets the bits that are different
            //newFlags &= _changeStates;  // tests the different bits to see if those are already set in existing
            //if (newFlags != 0) return;

            #if DEBUG
//            if ((flags & (ChangeStates.AppearanceParameterChanged | ChangeStates.AppearanceNodeChanged)) !=0)
//            	System.Diagnostics.Debug.WriteLine ("Node.SetChangeFlags() - AppearanceParameterChanged");
            #endif
            mChangeStates |= flags;
            PropogateChangeFlags(flags, source);
        }

        internal void ToggleChangeFlags(ChangeStates flags)
        {
            mChangeStates ^= flags;
        }
        internal void DisableChangeFlags(ChangeStates flags)
        {
            mChangeStates &= ~flags;
        }

        // June.23.2011 - MPJ - Commented out this method.
        // NOTE: I should never ever use this because it can result in unpredictable behavior.
        // Keep in mind that InheritScale and InheritOrientation use this
        //protected void ClearChangeFlags()
        //{
        //    _changeStates = ChangeStates.None;
        //}

        /// <summary>
        /// Flags are used for Lazy Updates.
        /// This method is alled when SetChangeFlags is called.  It is critical that 
        /// no other actions except flag propogation occurs here because
        /// some flags will get passed along multiple times and that would result in 
        /// multiple unnecessary actions.  Instead, responses to various flag settings
        /// should always occur during Update or Render or reading of a property value like 
        /// BoundingBox and then the flag can be cleared.
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="source"></param>
        protected virtual void PropogateChangeFlags(ChangeStates flags, ChangeSource source)
        {
        	if (mParents != null && (source == Enums.ChangeSource.Child || source == Enums.ChangeSource.Self))
                NotifyParents(flags);

            if ((flags & Keystone.Enums.ChangeStates.ChildNodeAdded) != 0)
            {
                // clear flag
                DisableChangeFlags(Keystone.Enums.ChangeStates.ChildNodeAdded);
            }
            else if ((mChangeStates & Keystone.Enums.ChangeStates.ChildNodeRemoved) != 0)
            {
                // clear flag
                DisableChangeFlags(Keystone.Enums.ChangeStates.ChildNodeRemoved);
            }
        }

        protected virtual void NotifyParents(Enums.ChangeStates flags)
        {
            if (mParents == null) return;

            for (int i = 0; i < mParents.Count; i++)
            {
            	if (mParents[i] == null) continue;
                Node parent = (Node)mParents[i];
                parent.SetChangeFlags(flags, Enums.ChangeSource.Child);
            }
        }

        public IGroup[] Parents
        {
            get
            {
                if (mParents == null) return null;
                return mParents.ToArray();
            }
        }
    }
}