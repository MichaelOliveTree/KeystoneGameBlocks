using System;
using System.Collections.Generic;
using Keystone.Traversers;
using Keystone.Entities;
using Keystone.Animation;
using Keystone.Types;
using Keystone.Extensions;

namespace Keystone.Elements
{
    // perhaps a SelectNode can have arbitrary hashtable of custom 'fields'
    // these fields can contain an "lod distance" or a 
    // "damage level" 
    // or even something like "on" vs "off" state selector
    // Then we must call a custom select function to use against the Select node
    // If provided by the entity.DomainObject.Script[select_render, debug | collision]
    // perhaps?

    /// <summary>
    /// These nodes are NOT shareable.  Thus they can be Transformable and maintain state information about
    /// frame to frame selections.  The reason they are unshareable is they host child models
    /// which can contain different appearances each.  Change the appearance on one instance and
    /// it requires every parent up the chain from that appearance to be unique.
    /// </summary>
    //public abstract class ModelSelector : BoundGroup
    //{
    //    protected SelectorNodeFlags mFlags;   // these are custom flags that can be set via script
    //                            // These flags determine which selection algorithm is used to determine
    //                            // which children are returned.
    //                            // These flags can also be set via the plugin gui

    //    internal ModelSelector(string id)
    //        : base(id)
    //    {
    //        _shareable = false;
    //    }

    //    // selects between models or other child Selector nodes

    //    public abstract Model[] Select(SwitchNodeOptions options);
    //    //public abstract Model[] Select(SwitchNodeOptions options, out int[] selections);
    //    public abstract Model[] Select(SelectPass pass, double distanceSqrd); 
    //}
    

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Because ModelSelector's is derived from Transform node
    /// it can be made a target for an Entity's Animation.  Also because it
    /// is derived from a Transform node it contains state information can NOT 
    /// be shared by multiple entities.  Furthermore, any parent of a Model
    /// which is also a state carrying node cannot be shareable so two reasons
    /// why ModelSelector nodes cannot be shared
    
    // TODO: this should be an abstract class.
    
    
    // TODO:  maybe this can change in the future.  but for now, because selector nides can cintain children that do have state like Models which can have different apoearances and pos/scaler/rotation, selector nodes cannot be shstef.
    
    
    
    // KeyCommon.DelegateHelper.SelectorNodeDelegate
    
    
    
    /// </remarks>
    public class ModelSelector : BoundTransformGroup, ISelectorNode 
    {
        private const int MAX_SELECTOR_CHILD_COUNT = 32; // 32 for 32 bitflags in mChildrenEnabledFlags.  To have >32, we'd need a different type of enabledFlags var
         protected int mChildrenEnabledFlags; // 32 bits to indicate which of up to 32 children are enabled

        //       protected List<Model> mSelectionResults;

        public delegate int SelectorNodeDelegate(object obj);
        privater SelectorNodeDelegate mSelectorDelegate;

		// this class should implement ISelectorNode
		// and they should all use the following struct
        internal struct SelectorContext 
        {
            Node Node;
			
        }
        
        public SelectorNodeDelegate SelectorDelegate 
        {
            get { return mSelectorDelegate;}
            set { mSelectorDelegate = value;}
        }
        
        protected SelectorNodeStyle mStyle;   // these are custom flags that can be set via script
        // These flags determine which selection algorithm is used to determine
        // which children are returned.  For instance, whether we select based on distance
        // or whether we return all models as a sequence.
        //
        // These flags can also be set via the plugin gui

        internal ModelSelector(string id)
            : base(id)
        {
            Shareable = false;  // selectors cannot be shareable because they derive from Transform
            this.mStyle = SelectorNodeStyle.Sequence;
            // instance our list here and we'll just clear it at top of Select() call
 //           mSelectionResults = new List<Model>(4);
            InheritRotation = true;
            InheritScale = true; // only Entities have option of not inheriting scale.
                                 // Models and ModelSwitches MUST inherit because whenever
                                 // user scales Entity, they mean to scale the models it contains
                                 // otherwise there is nothing to scale!
                                 // If they wish for per-model-level scaling, then they can
                                 // still scale those additionally independantly at their local
                                 // matrices, but otherwise Models and ModelSelectors always inherit.
        }

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }

        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        #region ResourceBase members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {

            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[2 + tmp.Length];
            tmp.CopyTo(properties, 2);

            properties[0] = new Settings.PropertySpec("style", typeof(int).Name);
            properties[1] = new Settings.PropertySpec("enabledchildren", typeof(int).Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = (int)mStyle;
                properties[1].DefaultValue = (int)mChildrenEnabledFlags;
            }

            return properties;
        }

        // TODO: this should return any broken rules ?
        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                switch (properties[i].Name)
                {
                    case "style":
                        mStyle = (SelectorNodeStyle)(int)properties[i].DefaultValue;
                        break;
                    case "enabledchildren":
                        mChildrenEnabledFlags = (int)properties[i].DefaultValue;
                        break;
                }
            }

            // TODO: verify no plugin options for InheritScale exist for Model or ModelSelector
            //       verify there is no way for users to modify this property to be "false"
            InheritScale = true;
        }
        #endregion

        #region IGroup members
        protected override void PropogateChangeFlags(Enums.ChangeStates flags, Enums.ChangeSource source)
        {
        	Keystone.Enums.ChangeStates filter = Keystone.Enums.ChangeStates.NotifyModelTransformed | 
        	                                     Keystone.Enums.ChangeStates.GeometryAdded | 
        	                                     Keystone.Enums.ChangeStates.GeometryRemoved;
			// filter out the ChangeStates from the variable "flags" that are _not in_ the filter list
        	Keystone.Enums.ChangeStates foundFlags = flags & filter;
        	
            if (foundFlags != 0)
            {
                if ((foundFlags & Keystone.Enums.ChangeStates.NotifyModelTransformed) !=0)
                {
                    // if we've been notified that a Model has Transformed (perhaps due to an animation, but irrelevant)
                    // respond appropriately based on whether the source of the notification is a Child or Parent.
                    if (source == Enums.ChangeSource.Child)
                    {
                        // source is a Child so is a ModelSelector (passing upward this notification from a Model added to a nested ModelSelector) 
                        // or the actual Model itself 
                        NotifyParents(Keystone.Enums.ChangeStates.NotifyModelTransformed);
                    }
                    // else if (source == Enums.ChangeSource.Parent) // <-- MUST NOT DO.  INFINTE RECURSION CAN RESULT 
                    
                    // remove the flag and continue
                    foundFlags &= ~Keystone.Enums.ChangeStates.NotifyModelTransformed;
                }
                // source in this case should only ever be self or a Model that is a child to this ModelSelector 
                // this assert is obsolete if we're allowing ComplexModels to host child Model's directly to simulate tv mesh groups
                //  System.Diagnostics.Trace.Assert(source == Enums.ChangeSource.Self); 
                NotifyParents(foundFlags);
                DisableChangeFlags(foundFlags);
            }

            filter  = Keystone.Enums.ChangeStates.Translated |
                      Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly |
                      Keystone.Enums.ChangeStates.BoundingBoxDirty |
                      Keystone.Enums.ChangeStates.KeyFrameUpdated |
                      Keystone.Enums.ChangeStates.MatrixDirty |
            	      Keystone.Enums.ChangeStates.RegionMatrixDirty;
            
        	// filter out a different set of ChangeStates from the variable "flags" that are _not in_ the filter list
    		foundFlags = flags & filter;
        		
            if (foundFlags != 0)
            {                		
                // if source of the flag is a child or self (and not a parent), notify parents
                if (mParents != null && source == Enums.ChangeSource.Child || source == Enums.ChangeSource.Self)
                    NotifyParents(foundFlags);

                // if source of the flag is a parent or self (and not a child), notify relevant children for relevant flags
                if (source == Enums.ChangeSource.Parent || source == Enums.ChangeSource.Self)
                {
                    if (mChildren == null) return;
                    // mChildren.ToArray() required to avoid issue of iterating _children and another child eleemnt being added during that
					// for loop (this can happen simply by adding a new entity to the scene and in this case one that has multiple
					// models and which will attempt to start iterating as we load each child model)
                    Node[] children = mChildren.ToArray(); 
                    // TODO: but i think the above can still fail if when first checking _children == null, it evaluates to not null
                    //       and then by the time we attempt _children.ToArray() it is now suddenly null to do paging out of nodes for example.
                    if (children.Length > 0)
                        for (int i = 0; i < children.Length; i++)
                            children[i].SetChangeFlags(foundFlags, Enums.ChangeSource.Parent);
                }
            }
        }

        public void AddChild(Model model)
        {
            if (model.Parents != null && model.Parents.Length == 1) throw new Exception("Model's cannot be shared.  Can only have one parent. Recommendation is to duplicate the Model, and just share the Geometry.");

            // all children added should be of same type (eg all ModelSelectors or all models)
            if (mChildren != null)
            {
                foreach (Node child in mChildren)
                {
                    if (child is Model == false) throw new Exception("ModelSelector.AddChild() - Children of a ModelSelector cannot be of mixed types ModelSelector and Model.  Children must be all of same type Model or ModelSelector.");
                }
            }

            base.AddChild(model);
            SetChangeFlags(Enums.ChangeStates.GeometryAdded |
                           Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            if (mChildren.Count > MAX_SELECTOR_CHILD_COUNT)
            {
                //System.Diagnostics.Trace.WriteLine("ModelSelector.AddChild() - Warning: greater than 32 child nodes under Selector node allowed but incompatible with mChildEnabledFlags.");
            }
        }

        public void AddChild(ModelSelector selector)
        {
            // all children added should be of same type (eg all ModelSelectors or all models)
            if (mChildren != null)
            {
                foreach (Node child in mChildren)
                {
                    if (child is ModelSelector == false) throw new Exception("ModelSelector.AddChild() - Children of a ModelSelector cannot be of mixed types ModelSelector and Model.  Children must be all of same type Model or ModelSelector.");
                }
            }
            
            base.AddChild(selector);
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | 
                           Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            
            if (mChildren.Count > MAX_SELECTOR_CHILD_COUNT)
                System.Diagnostics.Trace.WriteLine("ModelSelector.AddChild() - Warning: greater than 32 child nodes under Selector node allowed but incompatible with mChildEnabledFlags.");
        }
        
        public void InsertChild (ModelSelector selector)
        {
        	if (mChildren == null)
        	{
        		AddChild (selector);
        		return;
        	}
        	
        	foreach (Node child in mChildren)
            {
                if (child is ModelSelector == false) throw new Exception("ModelSelector.AddChild() - Children of a ModelSelector cannot be of mixed types ModelSelector and Model.  Children must be all of same type Model or ModelSelector.");
            }
        	
        	base.InsertChild(selector);
            
            if (mChildren.Count > 32)
                System.Diagnostics.Trace.WriteLine("ModelSelector.AddChild() - Warning: greater than 32 child nodes under Selector node allowed but incompatible with mChildEnabledFlags.");
        
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | 
                           Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
        }
        #endregion 


        public int IndexOf (Node node)
        {
        	if (mChildren == null) return -1;
        	
        	for (int i = 0; i < mChildren.Count; i++)
        		if (mChildren[i] == node) return i;
        	
        	return -1;
        }
        
        public virtual Model Select(uint index)
        {
            if (mChildren == null) return null;
            if (index > mChildren.Count - 1) throw new ArgumentOutOfRangeException ();

            if (mChildren[(int)index] == null) return null;
            
            // if the child at index is a nested ModelSelector
            // NOTE: remember we do not allow mixing of Models and ModelSelctor child nodes within a ModelSelector
            //       all children must either be Model's or ModelSelectors
            if (mChildren[(int)index] is ModelSelector) return null; 

            // at this point we are guaranteed the child is a Model
            return (Model)mChildren[(int)index];
        }

        
        // TODO: entity passed in here is not used at all except to pass to recursive Select() calls.
        //       - i think perhaps part of the idea here was to be able to query entity for determining
        //         damage level and thus which damage model branch to take.
        public virtual Model[] Select(SelectionMode pass, Entity entity, double distanceSquared)
        {
            if (mChildren == null) return null;

            // TODO: I think adding items to selectionResults list or array is just extremely slow!
            //       I might need to precache the array and then fill in the values and return
            //       an array that has null values starting at first empty slot.
            Model[] selectionResultsArray = null; // = new Model[mChildren.Count]; // max possible size? not exactly, if nested Selectors could be more!
 //           mSelectionResults.Clear();

            object retVal = null;
            bool isScripted = false;
            // TODO: this script call even when there is no script assembly available, is just horribly slow.
            //       according to the Profiler. WTF?
            using (CoreClient._CoreClient.Profiler.HookUp ("OnSelectModel"))
            	retVal = entity.Execute("OnSelectModel", new object[] { entity.ID, this.ID, this.Name, distanceSquared });
            
            if (retVal != null)
            {
            	mChildrenEnabledFlags = (int)retVal;
            	isScripted = true;
            }
            
            switch (mStyle)
            { 
                case SelectorNodeStyle.LOD: 
                    for (int i = 0; i < mChildren.Count; i++)
                    {
                    	
                    	if (isScripted && (mChildrenEnabledFlags & 1 << i) == 0) continue;
                    	
                    	// models can be independantly disable.  TODO: still waiting to determine if instead
						// we should add a model.Visible property or whether conceptually for models they
						// are the same thing?  because when not visible we dont want picking or rendering, 
						// but we will still want physics modeling if applicable (see ModeledEntity.SelectModel too)
                    	if (mChildren[i] == null || mChildren[i].Enable == false) continue;   
                        
                    	// TODO: if SelectPass is collision...
                    	// TODO: if SelectPass is render, and the very last model and a max draw range is reached, 
                    	//       and the "constrainMaxDrawRange" flag is set
                    	//       on the selector, we should NOT accept that LOD and return null instead thus allowing us to draw nothing.
                        // if this is the very last model in the list of LODs we always accept it
                        // otherwise we search until we find one that meets the distance requirement
                        if (mChildren[i] is Model && i == mChildren.Count - 1 || distanceSquared > ((Model)mChildren[i]).VisibleDistance)
                        {
                            selectionResultsArray = selectionResultsArray.ArrayAppend((Model)mChildren[i]);
                            //mSelectionResults.Add((Model)mChildren[i]);
                            break; // return first Model that meets LOD dist reqt
                        }
                        else if (mChildren[i] as ModelSelector != null)
                        {
                            // TODO: we should not automatically add these results
                            // for ModelSelector.Select... we need to test an LOD distance range still
                            // and we need to know if any models got added at all so that we can
                            // break and exist the for loop.  We do NOT want to iterate any further.
                            // That said, I feel that this type of LOD node should not have child
                            // ModelSelectors, only Models.  Also recall
                            // I have a preliminary rule that says all children must be of the same type.
                            Model[] models = ((ModelSelector)mChildren[i]).Select(pass, entity, distanceSquared);
                            if (models != null && models.Length > 0)
                            {
                                selectionResultsArray = selectionResultsArray.ArrayAppendRange(models);
                                //mSelectionResults.AddRange(models);
                                break;
                            }
                        }
                        else throw new Exception("ModelSelector.Select() - ERROR: Unexpected child type.");
                    }
                    
                    break;
                case SelectorNodeStyle.Sequence:

                    for (int i = 0; i < mChildren.Count; i++)
                    {
                    	if (isScripted && (mChildrenEnabledFlags & 1 << i) == 0) continue;
                    	
                    	if (mChildren[i] == null || mChildren[i].Enable == false) continue;
                        
                        if (mChildren[i] as Model != null)
                        {
                            Model m = (Model)mChildren[i];
                            // skip if no minimesh elements exist
                            if (m.Geometry is MinimeshGeometry && ((MinimeshGeometry)m.Geometry).EnableArray == null)
                            	continue;
                            
                            switch (pass)
                            {
                                case SelectionMode.Collision:
                                    selectionResultsArray = selectionResultsArray.ArrayAppend(m);
                                    //mSelectionResults.Add(m);
                                    break;
                                    
                                case SelectionMode.Render:
                                    // skip only csgsource punches?  but why?
                                    // wont we still need this for Renderer? 
                                    // 
                                    //if (m.GetFlagValue("csgsource") == false)
                                    
                                    // if this was the only node selected from script, then return
                                    // without adding to list collection since .Add() produces fair bit of
                                    // overhead and this Select() method gets called a ton.
                                    if (isScripted & ((mChildrenEnabledFlags & ~(1 << i)) == 0))
                                    	return new Model[]{m};

                                    selectionResultsArray = selectionResultsArray.ArrayAppend(m);
                                    //mSelectionResults.Add(m);
                                    break;
      
                                   case SelectionMode.CSGStencilSource:
                                    if (m.GetFlagValue("csgsource"))
                                        selectionResultsArray = selectionResultsArray.ArrayAppend(m);
                                    //mSelectionResults.Add(m);
                                    break;
                                case SelectionMode.CSGStencilAccept:
                                    if (m.GetFlagValue("csgaccept")) // can accept, but not necessarily a target in this frame
                                        selectionResultsArray = selectionResultsArray.ArrayAppend(m);
                                    // mSelectionResults.Add(m);
                                    break;
                            }
                        }
                        else if (mChildren[i] as ModelSelector != null)
                        {
                        	// it's ok for .Select to return null.  For instance, we use a dummy selector
                        	// to represent an "EMPTY" tile element so that our index values are aligned so that 0 == empty
                        	// yet still points to a valid child ModelSelector but which has no children of it's own.
                        	Model[] selection = ((ModelSelector)mChildren[i]).Select(pass, entity, distanceSquared);
                        	if (selection != null)
                                selectionResultsArray = selectionResultsArray.ArrayAppendRange(selection);
                             //mSelectionResults.AddRange(selection);
                        }
                        else throw new Exception("ModelSelector.Select() - ERROR: Unexpected child type.");
                    }
            		
                    break;


                case SelectorNodeStyle.Custom:
                    
                    // TODO: being limited to 32 flags may pose
                    System.Diagnostics.Debug.Assert (mChildren.Count <= 32, "ModelSelector.Select() - ERROR: Only 32bit flags available in child enable mask.");

                    for (int i = 0; i < mChildren.Count; i++)
                    {
                        // TODO: what makes mChildrenEnabledFlags better than just setting enable directly on the child?
                        // ANSWER: I suspect, it's because disabling the node makes it harder for different types of passes
                        // to select models based on different selection requirements.  I think perhaps the
                        // flags that are examined would thus be more useful if they were passed in... but where do we
						// get them from?  We don't want to run a Select scripted method                        
                        if (isScripted && (mChildrenEnabledFlags & 1 << i) == 0) continue;
                        
                        if (mChildren[i] == null || mChildren[i].Enable == false) continue;
                            
                        if (mChildren[i] as Model != null)
                        {
                            Model model = (Model)mChildren[i];
                            if (PassIsValid(pass, model))
                                selectionResultsArray = selectionResultsArray.ArrayAppend(model);
                            //mSelectionResults.Add(model);
                        }
                        else if (mChildren[i] as ModelSelector != null)
                        {
                        	Model[] selection = ((ModelSelector)mChildren[i]).Select(pass, entity, distanceSquared);
                        	if (selection != null)
                                selectionResultsArray = selectionResultsArray.ArrayAppendRange(selection);
                            //mSelectionResults.AddRange(selection);
                        }
                        else throw new Exception("ModelSelector.Select() - ERROR: Unexpected child type.");
                    }
                    
                    break;

                default:
                    break;

            }

            return selectionResultsArray;
            //return mSelectionResults.ToArray();
        }

        private bool PassIsValid(SelectionMode pass, Model model)
        {
            // TODO: rather than have Models with flags for CSGStencil or not
            //       or Hull, etc
            //       Why not store them as a child of entity but
            //       accessible as Entity.Hull and Entity.CSGStencil ?
            //       Well, one reason why CSGStencil would be bad is because
            //       different LODs might have different stencils and some damage models
            //       might not have one at all.
            //
            // if CSGStencil, only return the child model if it's a CSGStencil

            // if the pass is Normal, it will skip models in the sequence if they have CSGStencil flago

            // if the pass is CSGStencil, it will skip models that do NOT have CSGStencil flag set
            return true;

        }

        #region IBoundVolume Members
        /// <summary>
        /// GeometrySequence uses combined volume of all first level children.
        /// </summary>
        protected override void UpdateBoundVolume()
        {
            if (mChildren == null || mChildren.Count == 0) 
            	return;
			
                        
			mBox = BoundingBox.Initialized();

            // copy children to array in case list changes.
            Node[] children = mChildren.ToArray();
            
            // if Model and Entity bounding boxes are already in ModelSpace, this means it seems to me
            // that when we move the entire Entity, we must always recalc Model bboxes too.
            // if however Models' bbox were in 

            // TODO: for sequence, how could you ever create a model space box that 
            // encompasses multiple models each with potentially it's own unique positions
            // relative to the host entity. We would have to enforce some best practice when using
            // model sequence or rely on best practice being used.
            switch (mStyle)
            {
                default:
                    for (int i = 0; i < children.Length; i++)
                    {
                    	 // TODO: enable/disable of child Model or Entity should enable change flag Keystone.Enums.ChangeStates.BoundingBoxDirty
                    	if (children[i].Enable == false) continue;
                    	if (children[i] == null) continue;
                    	
                    	BoundingBox transformedChildBox = BoundingBox.Initialized();
                        if (children[i] is Geometry)
                        {
                        	// TODO: actually not sure... if you look at ModelSwitch.cs you'll see some old notes
                        	// about the concept of a ModelSwitch where appearance was shared by all Geometry under it
                        	
                        	System.Diagnostics.Trace.Assert (false, "ModelSelector.UpdateBoundVolume() - Should never hit.  I think this Geometry support was removed ages ago.");
                            //if (((Geometry)children[i]).PageStatus == IO.PageableNodeStatus.Loaded)
                            //    transformedChildBox = BoundingBox.Transform(((Geometry)children[i]).BoundingBox, RegionMatrix);
                        }
                        else if (children[i] is BoundNode)
                            //childBox =  ((BoundNode)children[i]).BoundingBox;
                            // transformedChildBox = BoundingBox.Transform(((BoundTransformGroup)children[i]).BoundingBox, ((BoundTransformGroup)children[i]).RegionMatrix);
                        	System.Diagnostics.Trace.Assert (false, "ModelSelector.UpdateBoundVolume() - Should never hit.  Geometry is only possible type of BoundNode here and we specifically test for that above.");
                        
                        else if (children[i] as BoundTransformGroup != null) // child Models and nested ModelSelectors
                            // NOTE: here we only transform models by their LocalMatrix.  The Entity will then transform the combined box represented by
                        	// this ModelSelector by the ModelSelector's RegionMatrix.
							transformedChildBox = BoundingBox.Transform(((BoundTransformGroup)children[i]).BoundingBox, ((BoundTransformGroup)children[i]).LocalMatrix);
                        
                        if (transformedChildBox != BoundingBox.Initialized())
                            mBox = BoundingBox.Combine(mBox, transformedChildBox);
                    }

                    if (mBox != BoundingBox.Initialized())
                    {
                        mSphere = new BoundingSphere(mBox);
                        DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);
                    }
                    break;
            }
        }
        #endregion 
    }
}

