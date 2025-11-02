using System;
using Keystone.Collision;
using Keystone.Entities;
using Keystone.FX;
using Keystone.IO;
using Keystone.Traversers;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Elements
{
	/// <remarks>
	/// in keystone notes.txt search string "Model Hierarchy Notes"
	/// </remarks>
    public class Model : BoundTransformGroup
    {
        [Flags]
        public enum ModelFlags : uint
        {
            None = 0,
            UseInstancing = 1 << 0,
            Pickable = 1  << 1, // individual models in a sequence can have their own "pickable" setting. Very useful.
            ReceiveShadow = 1 << 2,
            CastShadow = 1 << 3,
            Occluder = 1 << 4,
            CSGStencilSource = 1 << 5, // having CSG flags in  Model or ModelSelector (to set for all children in ModelLOD)
            CSGStencilTarget = 1 << 6, // instead of Entity allows more complex entities to be constructed where sub Models.cs can be csg 
            CSGStencilAccept = 1 << 7,
            StaticBoundingBoxDimensions = 1 << 8,  // box can still be translated, but the volume cannot.  Useful for things like rotating planets whos size never changes but position does
            All = uint.MaxValue
        }

        protected Geometry mGeometry;
        // NOTE: although it would be nice if we could grab reference to an appearance from a parent ModelSelector to be used
        // by all Models that are underneath that Selector node, we cannot because some properties of Appearance nodes are model instance specific. FYI.
        protected Appearance.Appearance mAppearance; 
        protected ModelFlags mModelFlags;
        protected CONST_TV_CULLING _cullMode; // TODO: this should be mesh/actor because Entity might need seperate settings for different instances.  Or then, Appearance should have the cull mode

        // TODO: I think ShaderParameters should exist here on the Model...
        //       this is where they should be persisted i mean...

        public double VisibleDistance { get; set; }

        public Model(string id)
            : base(id)
        {
            // like Entities, Model's cannot be shared.  This is because different appearances
            // on one mesh off the Model's hierarchy will result in all Model's that share the
            // Model instance to have that change regardless if it's what we intend.  
            // This makes it too unweidly to use.  Instead we will endeavor to make Model
            // lightweight.
            Shareable = false;
            ReceiveShadow = true;
            CastShadow = true;
			Pickable = true; // default
            InheritRotation = true;
            InheritScale = true; // only Entities have option of not inheriting scale.
            // Models and ModelSwitches MUST inherit because whenever
            // user scales Entity, they mean to scale the models it contains
            // otherwise there is nothing to scale!
            // If they wish for per-model-level scaling, then they can
            // still scale those additionally independantly at their local
            // matrices, but otherwise Models and ModelSelectors always inherit.

        }

        #region IResource Members
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

            properties[0] = new Settings.PropertySpec("cullmode", typeof(int).Name);
            properties[1] = new Settings.PropertySpec("modelflags", typeof(uint).Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = (int)_cullMode;
                properties[1].DefaultValue = (uint)mModelFlags;
            }

            System.Diagnostics.Debug.Assert(Shareable == false);
            //System.Diagnostics.Debug.WriteLine ("model node flags = " + ((byte)_nodeFlags).ToString());
            return properties;
        }

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
                    case "cullmode":
                        _cullMode = (CONST_TV_CULLING)(int)properties[i].DefaultValue;
                        break;
                    case "modelflags":
                        mModelFlags = (ModelFlags)(uint)properties[i].DefaultValue;
                        break;
                }
            }

            // TODO: verify no plugin options for InheritScale exist for Model or ModelSelector
            //       verify there is no way for users to modify this property to be "false"
            InheritScale = true; // Model and ModelSelectors must always inherit Scale and Rotation
            Shareable = false;  // TODO: temp hack to get old prefab file formats that didnt have this var yet to force models to NOT allow sharing
            Pickable = true;    // TODO: temp hack to get old prefab file formats that didnt have this var yet to force models to allow picking
        }
        #endregion

        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }



        public virtual bool GetFlagValue(uint flag)
        {
            switch ((ModelFlags)flag)
            {
            	 case ModelFlags.Pickable:
                    return (mModelFlags & ModelFlags.Pickable) == ModelFlags.Pickable;
                // whether this is a CSGSource depends on 
                // a) if our Geometry child != null then we can use the _entityFlags value
                // b) else if LOD child != null, then we must query the LOD for the csg source
                // value
                // currently the only thing being checked is this Entity's flag
                // which is set in the script and so it then traverses the lod looking 
                // for a stencil mesh
                // whether this is a CSGSource depends on 
                // a) if our Geometry child != null then we can use the _entityFlags value
                // b) else if LOD child != null, then we must query the LOD for the csg source
                // value
                // currently the only thing being checked is this Entity's flag
                // which is set in the script and so it then traverses the lod looking 
                // for a stencil mesh
                case ModelFlags.CSGStencilSource: // TODO: should csg related flags be moved to model?
                    return (mModelFlags & ModelFlags.CSGStencilSource) == ModelFlags.CSGStencilSource;
                case ModelFlags.CSGStencilAccept: // TODO: should csg related flags be moved to model?
                    return (mModelFlags & ModelFlags.CSGStencilAccept) == ModelFlags.CSGStencilAccept;
                case ModelFlags.CSGStencilTarget:
                    return (mModelFlags & ModelFlags.CSGStencilTarget) == ModelFlags.CSGStencilTarget;
                case ModelFlags.CastShadow:
                    return (mModelFlags & ModelFlags.CastShadow) == ModelFlags.CastShadow;
                case ModelFlags.ReceiveShadow:
                    return (mModelFlags & ModelFlags.ReceiveShadow) == ModelFlags.ReceiveShadow;
                case ModelFlags.UseInstancing:
                    return (mModelFlags & ModelFlags.UseInstancing) == ModelFlags.UseInstancing;
                case ModelFlags.Occluder:
                    return (mModelFlags & ModelFlags.Occluder) == ModelFlags.Occluder;
                default:
                    throw new ArgumentOutOfRangeException("Model flag '" + flag.ToString() + "' is undefined.");
            }
        }

        public virtual bool GetFlagValue(string flagName)
        {
            switch (flagName)
            {
           		case "pickable":
                    return (mModelFlags & ModelFlags.Pickable) == ModelFlags.Pickable;
                case "csgsource": // TODO: should csg related flags be moved to model?
                    return (mModelFlags & ModelFlags.CSGStencilSource) == ModelFlags.CSGStencilSource;
                case "csgaccept":
                    return (mModelFlags & ModelFlags.CSGStencilAccept) == ModelFlags.CSGStencilAccept;
                case "csgtarget":
                    return (mModelFlags & ModelFlags.CSGStencilTarget) == ModelFlags.CSGStencilTarget;
                case "castshadow":
                    return (mModelFlags & ModelFlags.CastShadow) == ModelFlags.CastShadow;
                case "recvshadow":
                    return (mModelFlags & ModelFlags.ReceiveShadow) == ModelFlags.ReceiveShadow;
                case "instancing":
                    return (mModelFlags & ModelFlags.UseInstancing) == ModelFlags.UseInstancing;
                case "occluder":
                    return (mModelFlags & ModelFlags.Occluder) == ModelFlags.Occluder;
                default:
#if DEBUG
                    throw new ArgumentOutOfRangeException("Model flag '" + flagName + "' is undefined.");
#endif
					return false;
                    break;
            }
        }

        // note: flags don't get set very often
        public virtual void SetFlagValue(uint flag, bool value)
        {
            switch ((ModelFlags)flag)
            {
            	case ModelFlags.Pickable:
                    if (value)
                        mModelFlags |= ModelFlags.Pickable;
                    else
                        mModelFlags &= ~ModelFlags.Pickable;
                    break;
                case ModelFlags.CSGStencilSource:
                    if (value)
                        mModelFlags |= ModelFlags.CSGStencilSource;
                    else
                        mModelFlags &= ~ModelFlags.CSGStencilSource;
                    break;
                case ModelFlags.CSGStencilAccept:
                    if (value)
                        mModelFlags |= ModelFlags.CSGStencilAccept;
                    else
                        mModelFlags &= ~ModelFlags.CSGStencilAccept;
                    break;
                case ModelFlags.CSGStencilTarget:
                    if (value)
                        mModelFlags |= ModelFlags.CSGStencilTarget;
                    else
                        mModelFlags &= ~ModelFlags.CSGStencilTarget;
                    break;
                case ModelFlags.CastShadow:
                    if (value)
                        mModelFlags |= ModelFlags.CastShadow;
                    else
                        mModelFlags &= ~ModelFlags.CastShadow;
                    break;
                case ModelFlags.ReceiveShadow :
                    if (value)
                        mModelFlags |= ModelFlags.ReceiveShadow;
                    else
                        mModelFlags &= ~ModelFlags.ReceiveShadow;
                    break;
                case ModelFlags.UseInstancing: // TODO: seems again more model type flag like UseInstancing and not Entity flag
                    UseInstancing = value;
                    break;

                case ModelFlags.Occluder:
                    if (value)
                        mModelFlags |= ModelFlags.Occluder;
                    else
                        mModelFlags &= ~ModelFlags.Occluder;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Model flag '" + flag.ToString() + "' is undefined.");

            }
        }

        public virtual void SetFlagValue(string flagName, bool value)
        {
            switch (flagName)
            {
        		case "pickable":
                    SetFlagValue((uint)ModelFlags.Pickable, value);
                    break;
                case "csgsource":
                    SetFlagValue((uint)ModelFlags.CSGStencilSource, value);
                    break;
                case "csgaccept":
                    SetFlagValue((uint)ModelFlags.CSGStencilAccept, value);
                    break;
                case "csgtarget":
                    SetFlagValue((uint)ModelFlags.CSGStencilTarget, value);
                    break;
                case "castshadow":
                    SetFlagValue((uint)ModelFlags.CastShadow, value);
                    break;
                case "recvshadow":
                    SetFlagValue((uint)ModelFlags.ReceiveShadow, value);
                    break;
                case "instancing":
                    SetFlagValue((uint)ModelFlags.UseInstancing, value);
                    break;
                case "occluder":
                    SetFlagValue((uint)ModelFlags.Occluder, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Model flag '" + flagName + "' is undefined.");
            }
        }

        // TODO: remove setter and enforce setter only thru SetFlagValue
        // TODO: need to verify that instancing can only be set with Mesh3d\Billboard
        // and then only with single groups and limited textures/material options (eg. i dont think
        // minimeshes can have more than a diffuse)
        public bool UseInstancing
        {
            // TODO: compare against _appearanceFlags and ModelFlags.UseInstancing
            get { return (mModelFlags & ModelFlags.UseInstancing) == ModelFlags.UseInstancing; }

            set
            {
                // if true but the geometry is not added or loaded yet, just return.
                // we will track the adding of the geometry and then recall UseInstancing to
                // create the minimesh
                // TODO: disable geometry test because if it's null, then it wont set the flag we need
                //if (value && !(_geometry is Mesh3d))
                //    return; 

				// TODO: if mesh is not yet loaded, groupcount test will fail so we wont do this here
				if (value ) // && ((Mesh3d)_geometry).GroupCount == 1)
                {
                    mModelFlags |= ModelFlags.UseInstancing;
                    return;
                }

                // still here, set the flag to false
                mModelFlags &= ~ModelFlags.UseInstancing;
            }
        }

        // TODO: remove setter and enforce setter only thru SetFlagValue
        public virtual bool Occluder
        {
            get { return (mModelFlags & ModelFlags.Occluder) == ModelFlags.Occluder; }
            set
            {
                if (value)
                    mModelFlags |= ModelFlags.Occluder;
                else
                    mModelFlags &= ~ModelFlags.Occluder;
            }
        }

        // TODO: remove setter and enforce setter only thru SetFlagValue
        public virtual bool Pickable
        {
            get { return (mModelFlags & ModelFlags.Pickable) == ModelFlags.Pickable; }
            set
            {
                if (value)
                    mModelFlags |= ModelFlags.Pickable;
                else
                    mModelFlags &= ~ModelFlags.Pickable;
            }
        }
        
        // TODO: remove setter and enforce setter only thru SetFlagValue
        public virtual bool CastShadow
        {
            get { return (mModelFlags & ModelFlags.CastShadow) == ModelFlags.CastShadow; }
            set
            {
                if (value)
                    mModelFlags |= ModelFlags.CastShadow;
                else
                    mModelFlags &= ~ModelFlags.CastShadow;
            }
        }

        // TODO: remove setter and enforce setter only thru SetFlagValue
        public virtual bool ReceiveShadow
        {
            get { return (mModelFlags & ModelFlags.ReceiveShadow) == ModelFlags.ReceiveShadow; }
            set
            {
                if (value)
                    mModelFlags |= ModelFlags.ReceiveShadow;
                else
                    mModelFlags &= ~ModelFlags.ReceiveShadow;
            }
        }

        public CONST_TV_BLENDINGMODE BlendingMode // short cut accessor to the Appearance.BlendingMode
        {
            get 
            { 
                if (mAppearance == null) return CONST_TV_BLENDINGMODE.TV_BLEND_NO;
                return mAppearance.BlendingMode ;
            }
        }

        /// <summary>
        /// Direct accessor to any available Appearance child node. 
        /// No "setter" allowed.
        /// </summary>
        public virtual Appearance.Appearance Appearance
        {
            get { return mAppearance; }
        }
        
        
        /// <summary>
        /// Direct accessor to any available Geometry child node. 
        /// No "setter" allowed.
        /// </summary>
        public virtual Geometry Geometry
        {
            get
            {
                return mGeometry;
            }
        }
        
        internal Entity Entity 
        {
        	get 
        	{
                if (mParents == null || mParents[0] == null) return null;
                
                //if (_parents[0] is Entity) return (Entity)_parents[0];
                    
                IGroup parent = (IGroup)mParents[0];
                while (parent != null)
                {
                    if (parent is Entity) return (Entity)parent;
                    parent = ((Node)parent).Parents[0];
                }
                return null;
        	}
        }
        
        private void UpdateShaderDefines()
        {      		
        	// TODO: shouldn't all of these actions recurse down all GroupAttribute children?  
        	//       Afterall, these are Model wide defines, not just top level Appearance node
        	//       Now technically it is possible for some groups to have different shaders
        	//       for instance, one groupattribute shader might handle transparency differently
        	//       but otherwise, the defines should be the same for them.
            if (mAppearance != null && mGeometry != null) // TODO: or Has GroupAttribute children
            {
            	
            	string previousDefines = Keystone.Appearance.GroupAttribute.GetDefinesToString(mAppearance.mDefines);
            	
            	// TODO: ClearDefines() wipes out #define NORMALMAP which is used by WorldShader.fx
            	//       but for now, AddDefine will replace any existing so we won't clear defines at all.
            	// mAppearance.ClearDefines ();
            
            	
				// add appropriate DEFINE for geometry type
				if (mGeometry is MinimeshGeometry)
					mAppearance.AddDefine("GEOMETRY_MINIMESH", null);
				else if (mGeometry is InstancedBillboard)
					mAppearance.AddDefine ("BILLBOARD_INSTANCING", null);
				else if (mGeometry is InstancedGeometry)
					mAppearance.AddDefine ("GEOMETRY_INSTANCING",null);
				else if (mGeometry is Mesh3d || Geometry is Terrain)
					mAppearance.AddDefine("GEOMETRY_MESH", null);
				else if (mGeometry is Actor3d)
					mAppearance.AddDefine("GEOMETRY_ACTOR", null);
				
				
				bool shadowMappingEnabled = Core._Core.Settings.settingReadBool ("graphics", "shadowmap");
				shadowMappingEnabled = shadowMappingEnabled && (this.CastShadow || this.ReceiveShadow);
					
				if (shadowMappingEnabled)
				{
					mAppearance.AddDefine ("SHADOW_MAPPING_ENABLED", null);
					
					int numSplits = Core._Core.Settings.settingReadInteger ("graphics", "shadowsplits");
		            string numSplitsDefine = "NUM_SPLITS_4";
		
		            switch (numSplits)
		            {
		            	case 4:
		            		numSplitsDefine = "NUM_SPLITS_4";
		            		break;
		            	case 3:
		            		numSplitsDefine = "NUM_SPLITS_3";
		            		break;
		            	case 2:
		            		numSplitsDefine = "NUM_SPLITS_2";
		            		break;
		            	case 1:
		            		numSplitsDefine = "NUM_SPLITS_1";
		            		break;
		            	default:
		            		numSplitsDefine = "NUM_SPLITS_4";
		            		break;
		            }
		            mAppearance.AddDefine(numSplitsDefine, null);
				}

                // NOTE: all viewports must use same graphics setting
                // Deferred vs Forward is one of the defines we must set dynamically.
            	bool deferredEnabled = CoreClient._CoreClient.Settings.settingReadBool("graphics", "deferred");
            	if (deferredEnabled)
            	{
                	mAppearance.AddDefine("DEFERRED", null);
            	}
            	else
            	{
            	    mAppearance.AddDefine("FORWARD", null);
            		//Appearance.AddDefine("MATERIAL", null);
            	}
            	            	
            	string current = Keystone.Appearance.GroupAttribute.GetDefinesToString(mAppearance.mDefines);
            	
            	if (current != previousDefines)
	            	// since the DEFINES have changed, we need to change the shader
            		// TODO: this entire function (including Defines modifications) 
            		//       should be a queued command that gets executed on command processing thread so that
            		//       we're not modifying the scene while its rendering
    	        	mAppearance.ReloadResource();
            }
    	}
        
        #region IGroup Members
        protected override void PropogateChangeFlags(Enums.ChangeStates flags, Enums.ChangeSource source)
        {
        	// NOTE: the actual flag we want to UpdateShader() in response to is GeometryAdded and AppearanceNodeChanged.  Otherwise we do NOT
        	//       want to UpdateShader() or UpdateAppearance() every change in appearance!  Perhaps if certain textures and materials children
        	//       under Appearance are added/removed too... but otherwise we should ignore general appearance changes
        	if ((flags & Keystone.Enums.ChangeStates.AppearanceNodeChanged) != 0)
        	{
        		UpdateShaderDefines();
        		DisableChangeFlags(Keystone.Enums.ChangeStates.AppearanceNodeChanged);
        	}
            
            // Keystone.Enums.ChangeStates.GeometryAdded flag set when Geometry completes LoadTVResource
        	Keystone.Enums.ChangeStates filter = Keystone.Enums.ChangeStates.GeometryAdded;
        	// filter out the ChangeStates from the variable "flags" that are _not in_ the filter list
        	Keystone.Enums.ChangeStates foundFlags = flags & filter;
        	
        	if (foundFlags != 0)
            {
                // UseInstancing is specifically for when we want to use a shared Minimesh2 for rendering instead of
                // the Mesh3d that is actually assigned as this.Geometry!  The reference to that shared Minimesh2 is
                // Minimesh2 mini = ((Mesh3d)this.Geometry).Minimesh;
                if (UseInstancing)
                {
                    UseInstancing = true; // hacky way to call that which will in turn call CreateMinimesh
                    System.Diagnostics.Debug.Assert (this.Geometry is Mesh3d && ((Mesh3d)this.Geometry).Minimesh != null);
                }
                else if (this.Geometry as Actor3d != null)
                {
                	// TODO: is the Geometry instantiated at this point? we need to initialize the BonedAnimations
                	//       after the BonedAnimations and Actor3d and it's duplicates are TVResourceIsLoaded == true.
                }
                else if (this.Geometry as InstancedGeometry != null)
                {
                	int maxCount = (int)((InstancedGeometry)this.Geometry).MaxInstancesCount;
                	InitializeArrays(maxCount);
                }
                else if (this.Geometry as MinimeshGeometry != null )
                {
                	// Alternatively, there are cases where Model uses MinimeshGeometry (not Mesh3d->Minimesh2) for things like
                	// our "voxel" structured terrain.  In those cases, we want the Model to have it's local Position\Rotation\Scale\Enable 
                	// arrays initialized when the Geometry gets added.
                	int maxCount = (int)((MinimeshGeometry)this.Geometry).MaxInstancesCount;
                	InitializeArrays(maxCount);
                }
                
                NotifyParents(foundFlags);
                UpdateShaderDefines();
                DisableChangeFlags(foundFlags);
            }
        	
        	if ((flags & Keystone.Enums.ChangeStates.AppearanceParameterChanged) != 0)
        	{
        		DisableChangeFlags(Keystone.Enums.ChangeStates.AppearanceParameterChanged);
        	}

        	filter = Keystone.Enums.ChangeStates.Translated |
        	    Keystone.Enums.ChangeStates.Scaled |
        	    Keystone.Enums.ChangeStates.Rotated |
                Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly |
                Keystone.Enums.ChangeStates.BoundingBoxDirty |
                Keystone.Enums.ChangeStates.MatrixDirty |
                Keystone.Enums.ChangeStates.RegionMatrixDirty |
                Keystone.Enums.ChangeStates.GeometryRemoved |
                Keystone.Enums.ChangeStates.KeyFrameUpdated;
        	
        	// filter out the ChangeStates from the variable "flags" that are _not in_ the filter list
        	foundFlags = flags & filter;
            if (foundFlags != 0)
            {            	
                // if source of the flag is a child or self (and not a parent), notify parents
                if (mParents != null && (source == Enums.ChangeSource.Child || source == Enums.ChangeSource.Self))
                    NotifyParents(foundFlags);
            
                // leave matrix and bbox flags dirty, but clear GeometryRemoved
                DisableChangeFlags(Keystone.Enums.ChangeStates.GeometryRemoved);
            }

            // OBSOLETE - never use switch for testing for certain flags.
            //switch (flags)
            //{
            //    case Enums.ChangeStates.AppearanceChanged:
            //        // if it's appearance, we don't notify anyone because the buck stops here for Appearances 
            //        //TODO: Is this still true now that we have Entity.Model instead of Entity.Appearance?
            //        break;
            //    case Enums.ChangeStates.GeometryAdded:
            //        // source in this case should only ever be self or another model that is a child to this ComplexModel 
            //        //  System.Diagnostics.Trace.Assert(source == Enums.ChangeSource.Self); // this assert is obsolete if we're allowing ComplexModels to host child Model's directly to simulate tv mesh groups

            //        // geometrAdded flag set when Geometry completes LoadTVResource
            //        if ((_changeStates & Enums.ChangeStates.GeometryAdded) > 0)
            //        {
            //            if (UseInstancing)
            //                UseInstancing = true; // hacky way to call that which will in turn call CreateMinimesh
            //        }

            //        NotifyParents(flags);
            //        break;

            //    // for models, movement related changes propogate up to parent entity which in turn will update the spatial graph (i.e SceneNode)
            //    case Enums.ChangeStates.Translated:
            //    case Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly:
            //    case Enums.ChangeStates.BoundingBoxDirty:

            //    case Enums.ChangeStates.GeometryRemoved:
            //    case Enums.ChangeStates.KeyFrameUpdated:
            //    case Enums.ChangeStates.MatrixDirty:
            //    case Enums.ChangeStates.RegionMatrixDirty:
            //    default:


            //        // if source of the flag is a child or self (and not a parent), notify parents
            //        if (_parents != null && source == Enums.ChangeSource.Child || source == Enums.ChangeSource.Self)
            //            NotifyParents(flags);

            //        break;   
            //}
        }


        // NOTE: No child Model's allowed under Model.AddChild(Model model)  If we want more than one model
        // we must use a sequence switch and add two+ child Models instead of one.
        // Likewise, no LOD as a child here either. LOD's can only be children of other
        // LOD or switch family nodes.
        //public void AddChild(Model model)
        //{
        //    base.AddChild(model);
        //}


        /// <summary>
        /// Geometry can consist of Mesh3d, Billboard, Actor3d, etc.  However only one of either is allowed.
        /// </summary>
        /// <param name="geometry"></param>
        public void AddChild(Geometry geometry)
        {
            if (mGeometry != null)
                throw new Exception("Model.AddChild() - Geometry already exists.  Must remove existing first.");

            mGeometry = geometry;
            base.AddChild(geometry);

            // call UseInstancing to start creation of the minimesh
            // TODO: i believe the UseInstancing line below is not necessary since in PropogateChangeFlags
            // it is also called.
            UseInstancing = (mModelFlags & ModelFlags.UseInstancing) == ModelFlags.UseInstancing;
            
    
            
            // set both geometry added and appearance changed!
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | 
                Keystone.Enums.ChangeStates.AppearanceParameterChanged |
                Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
        }

        public virtual void AddChild(Appearance.Appearance app)
        {
            // only one instance of Appearance node allowed as child
            if (mAppearance != null)
                throw new Exception("Model.AddChild() - Appearance node already exists.  Only one node of this type allowed.");

            mAppearance = app;

            base.AddChild(app);
            SetChangeFlags(Enums.ChangeStates.AppearanceNodeChanged | 
                           Enums.ChangeStates.AppearanceParameterChanged | 
                           app.ChangeFlags, Enums.ChangeSource.Child);
        }

        public override void RemoveChild(Node child)
        {
            base.RemoveChild(child);
            if (child is Geometry)
            {
                mGeometry = null;
                SetChangeFlags(Enums.ChangeStates.GeometryRemoved | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            }
            else if (child is Appearance.Appearance)
            {
                mAppearance = null;
                SetChangeFlags(Enums.ChangeStates.AppearanceNodeChanged | Enums.ChangeStates.AppearanceParameterChanged, Enums.ChangeSource.Child);
            }
        }
        #endregion
        

        #region MinimeshGeometry instance Members
        private int mInstanceCount;
		private Vector3d[] mPositions;
		private Vector3d[] mScales;
		private Vector3d[] mRotations;
		private byte[] mEnabledArray;
		
		private void InitializeArrays (int count)
		{
			mPositions = new Vector3d[count];	
			mScales = new Vector3d[count];	
			mRotations = new Vector3d[count];	
			mEnabledArray = new byte[count];
			mInstanceCount = 0;			
		}
		
		internal void ClearVisibleInstances()
		{
			if (mGeometry == null || mGeometry.PageStatus != Keystone.IO.PageableNodeStatus.Loaded)
				return;
			
			((InstancedGeometry)mGeometry).ClearVisibleInstances();
				
			if (mEnabledArray == null) 
				return;
			
			for (int i = 0; i < mEnabledArray.Length; i++)
				mEnabledArray[i] = 0;
			
			// Array.Clear (mEnabledArray, 0, mEnabledArray.Length);
			mInstanceCount = 0;
		}
		
		
		internal void AddInstance (Vector3d regionSpacePosition, Vector3d rotationDegrees)
		{
			if (mGeometry == null || mGeometry.PageStatus != Keystone.IO.PageableNodeStatus.Loaded)
				return;
			
			Vector3d defaultScale;
			// HACK - increase scale slightly to help deal with seams between terrain tiles
			defaultScale.x = 1.001d;
			defaultScale.y = 1.001d;
			defaultScale.z = 1.001d;

			mPositions[mInstanceCount] = regionSpacePosition;
			mRotations[mInstanceCount] = rotationDegrees; 
			// instanced geometry requires radians
			if (mGeometry is InstancedBillboard)
			{
				// do nothing
			}
			else if(mGeometry is InstancedGeometry)
			{		
				mRotations[mInstanceCount] *= Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS;
			}
			mScales[mInstanceCount] = defaultScale;
			mEnabledArray[mInstanceCount] = (byte)1;
			mInstanceCount++;
		}
        #endregion
        
        // TODO: do we need an "Update()" so that we can call per model update script
        // for things like rotating parts of the sub-mesh differently?  or do we 
        // handle independant movement of sub-models all in the entity's Update script?
        public virtual void Render(Keystone.Cameras.RenderingContext context, Entity entity, Vector3d cameraSpacePosition, double elapsedSeconds, FX_SEMANTICS source)
        {
            // DistanceToCameraSq
            //         SwitchNodeParams.Geometry // selects the normal geometry in a child Switch
            // Params  SwitchNodeParams.Stencil  // selects the Stencil geometry in a child Switch
            //         SwitchNodeParams.Collider // draw the collider typically for debug
            //         SwitchNodeParams.Switch1  
            //         SwitchNodeParams.SelectLOD // selects an LOD based on distance
            //         SwitchNodeParams.UseExistingLODSelection // uses the current set LOD index if available

            // TODO: I realized that it's in our Culler.cs where we check for Imposter and we Pop it off the stack
            //       since it was already added.  So maybe i dont need the FX_SEMANTICS source parameter
            //       but leaving it for now just in case down the road...
            //if (source != FX_SEMANTICS.FX_IMPOSTER && Data[(int)FX_SEMANTICS.FX_IMPOSTER] != null)
            //    return;

            // TODO: if we add possibility to run a script that can update shader parameters
            // it's for dealing with things where the script knows more about the model than we can
            // handle generically in an app non specific way.  For instance, the script can know that
            // a particular model has a shader that expects up to 4 star light sources whereas another
            // will accept just an average of the 4 and treat as a dir light (for shadow purposes)
            // if (mScript != null && mScript.TVResourceIsLoaded())
            //      mScript.Execute["UpdateShaderParameters_Forward];

            // todo; rather than rely on Appearance.Apply() on the mesh there
            // to perform updates of texturemod/texture animations and such
            // shoudl we instead do that here?  Afterall, if we're running scripts, scripts would
            // call here and perform similar Appearance FX operations.

            // model now only contains geometry and appearance.  no LODs to worry about.
            // instead this model if Render is called is the selected model if any LODSwitch nodes were involved
#if DEBUG
            if (mGeometry == null || 
                mGeometry.PageStatus != PageableNodeStatus.Loaded || 
                mGeometry.Enable == false) 
					throw new Exception ("Model.Render() - ERROR: should never happen.  ScaleCuller tested these values already.");
#endif
            if (mGeometry is TexturedQuad2D)
            {
                mGeometry.Render(entity.RegionMatrix, context.Scene, this, elapsedSeconds);
                return; 
            }
 
            Matrix matrixOverride = Matrix.Identity();
            // MinimeshGeometry used for interior region walls
            if (mGeometry is MinimeshGeometry)
            {
                using (CoreClient._CoreClient.Profiler.HookUp("MinimeshGeometry.Assign Arrays"))
                {
                    // apply all the positions, scales, rotations and enable's to the MinimeshGeometry
                    MinimeshGeometry mini = (MinimeshGeometry)this.mGeometry;
                    // TODO: Sept.29.2016 - The following array assignments used to be required back when we used MinimeshGeometry
                    // to store our voxel terrain Geometry.  But the we switched to InstancedGeometry
                    // which uses a custom instancing shader.  But the key is, setting these arrays here
                    // nullifies the arrays we assign elsewhere.  (eg for starmap HUD)
                    //mini.EnableArray = mEnabledArray;
                    //mini.PositionArray = mPositions;
                    //mini.ScaleArray = mScales;
                    //mini.RotationArray = mRotations;
                    matrixOverride = GetCameraSpaceWorldMatrixOverride(context, entity, cameraSpacePosition);
                }
            }
            else if (mGeometry is ParticleSystem)
            {
                matrixOverride = GetCameraSpaceWorldMatrixOverride(context, entity, cameraSpacePosition);
            }
            else if (mGeometry is InstancedBillboard)
            {
                // TEMP HACK to supply viewprojection without adding new geometry.Render() overload
                matrixOverride = context.Camera.ViewProjection;

                //        		// NOTE: instanced positions are added as regionSpace via Model.AddInstance()
                // Converting to cameraSpace here is consistant with existing design and supports
                // tiles as well as particle effects.
                Vector3d[] cameraSpacePositions = new Vector3d[mInstanceCount];
                for (int i = 0; i < mInstanceCount; i++)
                    cameraSpacePositions[i] = mPositions[i] + cameraSpacePosition;

                InstancedGeometry instancedGeometry = (InstancedGeometry)this.mGeometry;
                instancedGeometry.AddInstances(cameraSpacePositions, mRotations, mInstanceCount);

                if (this.Appearance != null && this.Appearance.Shader != null && this.Appearance.Shader.TVShader != null && this.Appearance.Shader.PageStatus == PageableNodeStatus.Loaded)
                {
                    // NOTE: Keystone.Traversers.VisibleItemInfo.cs.Draw() sets light color and light direction for InstancedGeometry shader
                    this.Appearance.Shader.TVShader.SetEffectParamMatrix("ViewProjection", Keystone.Helpers.TVTypeConverter.ToTVMatrix(context.Camera.ViewProjection));
                    this.Appearance.Shader.TVShader.SetEffectParamVector3("g_ViewPosition", Keystone.Helpers.TVTypeConverter.ToTVVector(Vector3d.Zero()));
                    this.Appearance.Shader.SetShaderParameterVector("g_CameraOffset", context.Position);
                }
            }
            else if (mGeometry is InstancedGeometry)
            {
                // TEMP HACK to supply viewprojection without adding new geometry.Render() overload
                matrixOverride = context.Camera.ViewProjection;

                if (mInstanceCount > 0)
                {
                    // NOTE: instanced positions are added as regionSpace via Model.AddInstance()
                    // Converting to cameraSpace here is consistant with existing design and supports
                    // tiles as well as particle effects.
                    Vector3d[] cameraSpacePositions = new Vector3d[mInstanceCount];
                    for (int i = 0; i < mInstanceCount; i++)
                        cameraSpacePositions[i] = mPositions[i] + cameraSpacePosition;

                    InstancedGeometry instancedGeometry = (InstancedGeometry)this.mGeometry;
                    instancedGeometry.AddInstances(cameraSpacePositions, mRotations, mInstanceCount);
                }

                if (this.Appearance != null && this.Appearance.Shader != null && this.Appearance.Shader.TVShader != null && this.Appearance.Shader.PageStatus == PageableNodeStatus.Loaded)
                {
                    // NOTE: Keystone.Traversers.VisibleItemInfo.cs.Draw() sets light color and light direction for InstancedGeometry shader
                    this.Appearance.Shader.TVShader.SetEffectParamMatrix("ViewProjection", Keystone.Helpers.TVTypeConverter.ToTVMatrix(context.Camera.ViewProjection));
                    this.Appearance.Shader.TVShader.SetEffectParamVector3("g_ViewPosition", Keystone.Helpers.TVTypeConverter.ToTVVector(Vector3d.Zero()));
                    this.Appearance.Shader.SetShaderParameterVector("g_CameraOffset", context.Position);
                }
            }
            else
            {
                matrixOverride = GetCameraSpaceWorldMatrixOverride(context, entity, cameraSpacePosition);
            }

            // TODO: if appearnace or geometry changes, we must change DEFINES on Appearance (recursing any AppearanceGroups as well)
            //       and then set ResourceStatus to unloaded so it can be reloaded
            //       if the geometry type has not changed we dont need to change GEOMETRY_MINIMESH or GEOMETRY_ACTOR or GEOMETRY_MESH
            mGeometry.Render(matrixOverride, context.Scene, this, elapsedSeconds);
        }


        internal Matrix GetCameraSpaceWorldMatrixOverride(Keystone.Cameras.RenderingContext context, Entity entity, Vector3d cameraSpacePosition)
        {
            // NOTE: care must be taken that in our Models and ModelSelectors we have
            // proper flags set for InheritScale and InheritRotation and that we've applied a
            // scaling to Models and or ModelSelector and or Entity as desired.  This can be tricky
            // to remember and understand.
            Matrix result;

            
        	// TODO: By rotating the mesh here, our picking is thrown off because the bounding box
            // is not taking into account this rotation...   Technically axial rotation shouldn't be 
            // done on the Mesh but rather on the Entity. Oh really? The mesh is better because the rotation
            // is only for graphical effect and does not represent any real rotation...
            bool axialRotation = false;
            bool customRotation = false;
			bool isMini = mGeometry is MinimeshGeometry;
			
            if (mGeometry as Billboard != null) 
            {
                // compute a billboard roation so we face camera.  this must be combined with
                // the model's region matrix.  TV3D doesnt have built in functionality to combine
                // the billboarding camera facing rotation with an ortho rotation so you can have for example
                // a pinwheel rotation. So, you have to compute it yourself.
                Billboard bb = (Billboard)mGeometry;
                axialRotation = bb.AxialRotationEnable;
                if (axialRotation == false)
                    customRotation = bb.BillboardType == CONST_TV_BILLBOARDTYPE.TV_BILLBOARD_NOROTATION;
            }
            else if (entity as Keystone.EntitySystems.Emitters.AnimatedTextureHPEmitter != null)
        	{    	
				// non-minimesh HPParticleEmitter             	
				// since each particle can have it's own region, we must override passed in cameraSpacePosition and compute new
				int index = ((ModelSelector)this.Parents[0]).IndexOf(this);
				if (index >= 0)
    				cameraSpacePosition = GetParticleEmitterElementPosition (context, (uint)index, (Keystone.EntitySystems.Emitters.AnimatedTextureHPEmitter)entity, axialRotation || isMini == false);
        	}
            else if (isMini)
            {
            	MinimeshGeometry mini = (MinimeshGeometry)mGeometry;
            	if (mini.IsBillboard)
            	{
            		axialRotation = mini.AxialRotationEnable;
            		if (axialRotation == false)
                    	customRotation = mini.BillboardType == CONST_TV_BILLBOARDTYPE.TV_BILLBOARD_NOROTATION;
            		else 
            		{
	            		// mini.GetElementMatrix (i);
	            		for (uint i = 0; i < mini.InstancesCount; i++)
	            		{
	            			if (mini.GetElementEnable(i) == false) continue;
	            			            			
	            			// TODO: elementPosition i think needs to take into account this.mDerivedTranslation because
	            			// using SetElementMatrix() means we must compute final world matrix since Model.RegionMatrix will 
	            			// be ignored since SetElementMatrix() requires final world matrix to be computed and set and Minimesh.SetGlobalMatrix is ignored.
	            			Vector3d elementPosition = Vector3d.Zero();
	            			if (entity as Keystone.EntitySystems.HeavyParticleEmitter != null)
	            			{
	            				// since each particle can have it's own region, we must override passed in cameraSpacePosition and compute new
	            				// position for each individual minimesh element
            					elementPosition = GetParticleEmitterElementPosition(context, i, (Keystone.EntitySystems.HeavyParticleEmitter)entity, axialRotation);  
	            			}
	            			else
	            			{
		            			elementPosition = this.mDerivedTranslation + mini.GetElementPosition (i) + cameraSpacePosition;
	            			}
	            			
	            			// for axial billboards, the minimesh element rotation
	            			// actually stores the rotation axis
	            			// TODO: i think axis needs to take into account this.mDerivedRotation if .InheritRotation == true
	            			Vector3d axis = mini.GetElementRotation (i);
	            			// NOTE: the automatic texture coordinates of tvbillboard are such that our beam weapon textures
			                // need to be pointed up verticaly such that the texture's height is bigger than it's width
			                Matrix axialRotationMatrix = Matrix.CreateAxialBillboardRotationMatrix(axis,
			                                                                                 	   elementPosition,
	                                                                 						 	   Vector3d.Zero());
	                
			                // TODO: these values i think need to take derived translation and scale 
			                Matrix elementScalingMatrix =  Matrix.CreateScaling (mini.GetElementScale(i));
			                Matrix elementTranslationMatrix = Matrix.CreateTranslation (elementPosition);
	            			Matrix elementMatrix  = elementScalingMatrix * axialRotationMatrix * elementTranslationMatrix; 
	
	            			// NOTE: calls to MinimeshGeometry.SetElementMatrix() leads to a call to TVMinimesh.SetMatrixArray() 
	            			// which will disable SetGlobalPosition() and so allow us to compute axial billboarding in 
	            			// worldspace (or cameraspace) without worrying about the parent TVMinimesh.Matrix
	            			// being applied.  The minimesh matrices in this case are used as the final world matrices.
	            			mini.SetElementMatrix (i, elementMatrix);
	            		}
	            		
	            		// disable axial for the rest of the computation which is for global Minimesh 
	            		axialRotation = false;
	            		customRotation = false;
            		}
            	}
            }


            // TODO: re: below comment on scalingFactor for sub-models.  If all sub-Models are at
            // same world translation, then the scalingFactor will be identicle.  However for
            // offset sub-models, not true.  Ideally we would apply a single scaling factor
            // TODO: scalingFactor for all sub-models of an entity i think should be the same
            // or else we get slight errors trying to line them up.  Now this will typically come out
            // to the same if all sub-models are at same position, but for those at varying positions, this
            // within the entity, there will be variations....
            // GetScalingFactor() returns 1 if no scaling factor is being applied.
            double scalingFactor = GetScalingFactor(context.Far, context.Far2 - context.Near2, context.MaxVisibleDistance, cameraSpacePosition.Length);
             

            if (axialRotation)
            {   
                Vector3d lookAt = context.LookAt; // lookAt = Vector3d.Normalize(cameraSpacePosition);

                // the axial rotation passes in the camera space position of the billboard in world space
                // so the axialRotationMatrix is already a fully derived rotation matrix.
                Matrix tmpRotationMatrix = GetRotationOverrideMatrix(entity.DerivedRotation, mRotation, cameraSpacePosition, Vector3d.Zero(), context.Up, lookAt, axialRotation, customRotation);
               
                // NOTE: the scaling matrix is required if we want to scale the beam to match the barrel bore size, and the distance to target.
                // NOTE: scaling has zero impact on the billboard rotation so can be performed after we retreive the axial rotation.
                result = Matrix.CreateScaling(mDerivedScale) * tmpRotationMatrix;

                result.M41 = cameraSpacePosition.x;
                result.M42 = cameraSpacePosition.y;
                result.M43 = cameraSpacePosition.z;

                if (axialRotation)
                    if (scalingFactor == 1.0)
                        return result;
            }
            else
            {	
	            // note: we clone the matrix so as not to alter the original 
    	        // note2: This is the model's RegionMatrix and so includes any inherited rotation
    	        result = new Matrix(this.RegionMatrix);   
            }
            
                        	
            result.M41 = cameraSpacePosition.x;
            result.M42 = cameraSpacePosition.y;
            result.M43 = cameraSpacePosition.z;


            if (axialRotation || customRotation || scalingFactor != 1.0d)
            {
                // verify scalingFactor defaults to 1.0d. It's typically only used for scaling planets and star billboards
                if (scalingFactor != 1.0d)
                {
                    // scale the position down`
                    result.M41 *= scalingFactor;
                    result.M42 *= scalingFactor;
                    result.M43 *= scalingFactor;
                }

                // NOTE: The above matrices already include the mDerivedScale and we multiply that
                //       matrix with our GetScaledModelMatrix() so we must reduce the scaling factor
                // by that amount or it will be SQUARED value and thus way too large.         
                //if (InheritScale == false) <-- i think we should be canceling out the scaling factor regardless
                //{
                if (scalingFactor != 1d)
                    scalingFactor /= mDerivedScale.x;
                //}

                // any one of axialRotation or freeRotation or scalingFactor != 0, requires overriding the rotationMatrix
                // NOTE: when we call this function for mouse picking, the CAMERA POSITION (not to be comfused with cameraSPACEPosition) parameter is the actual context.Position not Vector3d.Zero() which is origin based camera rendering
                //       This is because the picker places the billboard at Vector3d.Zero() to create a modelspace Ray so we need a modelspace camera position for that case.
                Matrix rotationMatrix = GetRotationOverrideMatrix(entity.DerivedRotation, mRotation, cameraSpacePosition, Vector3d.Zero(), context.Up, context.LookAt, axialRotation, customRotation);
              //  rotationMatrix = new Matrix(entity.DerivedRotation) * rotationMatrix;

                if (scalingFactor == 1.0)
                    return rotationMatrix * result;
                    //return result * rotationMatrix;
                else
                {
                    Vector3d scale = mDerivedScale;
                    Matrix scaledModelMatrix = GetScaledModelMatrix(rotationMatrix, scale, scalingFactor);
                    result = scaledModelMatrix * result;
                }
            }
            
            return result;
        }
        
        Vector3d GetParticleEmitterElementPosition (Keystone.Cameras.RenderingContext context, uint elementIndex, EntitySystems.HeavyParticleEmitter emitter, bool computeCameraSpacePerElement)
        {
			// NOTE: The global position of the minimesh is irrelevant.  It can remain at 0,0,0 since each element we will manually
			//       position relative to camera.
			// TODO: if particles were sorted by region, we wouldn't have to compute Source2Dest for each element.
			//       elements which shared same region could use same transform
			Matrix elementGlobalMatrix = emitter.GetRegion(elementIndex).GlobalMatrix;
			Matrix transform = Matrix.Source2Dest (context.Region.GlobalMatrix, elementGlobalMatrix);

			Vector3d cameraSpacePosition = Vector3d.TransformCoord (emitter.GetPosition(elementIndex), transform);
			if (computeCameraSpacePerElement)
			{
				cameraSpacePosition -= context.Position; 
			}
			return cameraSpacePosition;
        }
        
        public static Matrix GetRotationOverrideMatrix (Quaternion entityRotation, Quaternion modelRotation, Vector3d cameraSpacePosition, Vector3d cameraPosition, Vector3d up, Vector3d lookAt, bool axialRotation, bool customRotation)
        {
        	Matrix result;
        	
            if (axialRotation)
            {

                // forward vector is the axis we want to rotate around to face camera
                Vector3d axis =  entityRotation.Forward();
                axis = Vector3d.Normalize(axis);

                // NOTE: CreateAxialBillboardRotationMatrix() returns a camera space or world space matrix that does not need to be multiplied with any derived region or world matrix
                result = Matrix.CreateAxialBillboardRotationMatrix(axis,
                                                                  cameraSpacePosition,
                                                                  cameraPosition);


            }
            else if (customRotation)
            {
                // compute a billboard roation so we face camera.  this must be combined with
                // the model's region matrix.  TV3D doesnt have built in functionality to combine
                // the billboarding camera facing rotation with an ortho rotation so you can have for example
                // a pinwheel rotation. So, you have to compute it yourself.
                // NOTE: we always use 
                Matrix tmp = Matrix.CreateBillboardRotationMatrix(up, lookAt);
                //result = tmp;
                result = new Matrix(entityRotation * modelRotation) * tmp;
                //System.Diagnostics.Debug.WriteLine (mRotation.ToString());
                //result = Matrix.Multiply4x4(new Matrix(mRotation), tmp);
            }
            else 
            	result = new Matrix(entityRotation); // todo: should this be (entityRotation * modelRotaiton) ?
            
            return result; 
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="farPlaneDistance1"></param>
        /// <param name="farPlaneDistance2"></param>
        /// <param name="max_visible_range"></param>
        /// <param name="distance"></param>
        /// <returns>1.0d if no scaling factor is required.</returns>
        protected double GetScalingFactor(double farPlaneDistance1, double farPlaneDistance2, double max_visible_range, double distance)
        {
            // the below routine is from the planet rendering gamasutra article by Oneil
            if (distance == 0) return 1d;
            if (distance < max_visible_range && distance < farPlaneDistance1) return 1d;

            double half_farplane = farPlaneDistance2 * .5d; // TODO: these farplane values and such should be based on the current viewport
            double largerFar_sq = farPlaneDistance2 * farPlaneDistance2;
            // scale down the distance to the exponent that puts it between farplane / 2 and  farplane 
            double scale = 1d;
            scale *= (distance >= max_visible_range)
                         ? largerFar_sq
                         : half_farplane + half_farplane * (1.0d - Math.Exp(-2.5d * distance / max_visible_range));
            scale /= distance;

            return scale;
        }

        protected Matrix GetScaledModelMatrix(Matrix rotation, double scalingFactor)
        {
            Vector3d startScale;
            startScale.x = 1.0;
            startScale.y = 1.0;
            startScale.z = 1.0;
            return GetScaledModelMatrix(rotation, startScale, scalingFactor);
        }

        protected Matrix GetScaledModelMatrix(Matrix rotation, Vector3d startScale, double scalingFactor)
        {
            Matrix temp;
            Matrix scale;
            if (scalingFactor != 1.0)
            {

                scale = Matrix.CreateScaling(startScale.x * scalingFactor,
                                              startScale.y * scalingFactor,
                                              startScale.z * scalingFactor);
            }
            else
            {
                scale = Matrix.CreateScaling(startScale.x,
                                              startScale.y,
                                              startScale.z);

            }

            temp = scale * rotation;
            return temp;
        }

        //static double GetScalingFactor(double dDistance)
        //{
        //    double dFactor = 1.0;
        //    //return 1.0f;
        //    if(dDistance > HALF_MAX)
        //    {
        //        dFactor *= (dDistance >= MAX_DISCERNABLE) ? MAX_DISTANCE : HALF_MAX + HALF_MAX * (1.0 - Math.Exp(-2.5 * dDistance / MAX_DISCERNABLE));
        //        dFactor /= dDistance;
        //    }
        //    return dFactor;
        //}

        //float ScaleModelMatrix(CMatrix &m, double dDistance, double dFactor=1.0)
        //{
        //    // This code scales the object's size and distance to the camera down when it's too far away.
        //    // This solves a problem with many video card drivers where objects too far away aren't rendering properly.
        //    // It also alleviates the Z-buffer precision problem caused by having your near and far clipping planes too far apart.
        //    float fFactor = GetScalingFactor(dDistance, dFactor);
        //    if(fFactor <= 1.0f)
        //    {
        //        m.f41 *= fFactor;
        //        m.f42 *= fFactor;
        //        m.f43 *= fFactor;
        //        m.Scale(fFactor, fFactor, fFactor);
        //    }
        //    return fFactor;
        //}

        //float GetScaledModelMatrix(CMatrix &m, C3DObject *pCamera, double dFactor=1.0)
        //{
        //    CDoubleVector vPos = m_vPosition - pCamera->m_vPosition;
        //    double dDistance = vPos.Magnitude();
        //    m.ModelMatrix(*this, vPos);
        //    return ScaleModelMatrix(m, dDistance, dFactor);
        //}


        #region IBoundVolume Members
        protected override void UpdateBoundVolume()
        {
            if (mGeometry == null || mGeometry.TVResourceIsLoaded == false) 
            {
            	mBox = BoundingBox.Initialized();
            	return; // Sept.10.2016 - We return now before disabling BoundingBoxDirty flag because
            	        // there are times when the geometry is added to the model but not yet loaded
            	        // and it results in stars and planets not fully rendering because the boundingbox 
            	        // never gets updated.  By returning here we force UpdateBoundVolume() to be called
            	        // over and over until the geometry is loaded.
            }
            // see if we can get away with just translating the existing bounding box?
            if ((mChangeStates & Enums.ChangeStates.BoundingBox_TranslatedOnly & Enums.ChangeStates.BoundingBoxDirty) == Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly)
            {
                mBox.Max -= mTranslationDelta;
                mBox.Min -= mTranslationDelta;
                mSphere = new BoundingSphere(mBox);
                //DisableChangeFlags(Enums.ChangeStates.BoundingBox_TranslatedOnly);
            }
            // a Model can only have one geometry child and no sub-models
            else if (mGeometry != null && mGeometry.TVResourceIsLoaded)
            {
                // Model's local bounding box is computed by taking child geometry model space boxes
                // and  transforming them by this Model's LocalMatrix. 
                // This is because a Model can be independantly scaled, rotated or offset from the
                // Entity it's attached too.
                //_box = BoundingBox.Transform(mGeometry.BoundingBox, LocalMatrix);
                // Oct.2.2013 - it was a mistake to transform Model's boxes. There is no point to doing so.
                // We should think of Model's as the wrapper for Geometry.  It is only because TV3D incorporates BBox
                // inside TVMesh, TVActor, etc that we also include it in Mesh3d and Actor3d. 
                // But really, Geometry nodes should just be vertex and index buffer management
                mBox = mGeometry.BoundingBox;
                // NOTE: Sept.11.2016 - we dont disable the change flags for BoundingBoxDirty | BoundingBox_TranslatedOnly if mGeometry is NULL.
	            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty | Enums.ChangeStates.BoundingBox_TranslatedOnly);
            }
            
            if (mBox == null) return; 
            mSphere = new BoundingSphere(mBox);
        }
        #endregion

        #region Static Helper Methods
        public static Model Load2DTexturedQuad(string name, string texturePath, int widthPixels, int heightPixels)
        {
            System.Diagnostics.Debug.Assert(name != texturePath, "Texture name and TexuredQuad2D.cs name cannot be same or Repository will retreive wrong type.");
            Model model = new Model(Keystone.Resource.Repository.GetNewName(typeof(Model)));


            // create the mesh
            Keystone.Elements.TexturedQuad2D quad = (Keystone.Elements.TexturedQuad2D)Resource.Repository.Create(name, "TexturedQuad2D");

            Keystone.Appearance.Material emissiveWhite = Keystone.Appearance.Material.Create(
                Keystone.Appearance.Material.DefaultMaterials.white_fullbright);
            Keystone.Appearance.Appearance appearance = Keystone.Celestial.ProceduralHelper.CreateAppearance(MTV3D65.CONST_TV_LIGHTINGMODE.TV_LIGHTING_NORMAL, "tvdefault", texturePath, "", "", "");
            //// CreateAppearance call above creates and adds our diffuse for us
            //Diffuse diffuse = Diffuse.Create(texturePath);
            //Keystone.IO.Pager.LoadTVResourceSychronously (diffuse.Texture, false);
            //appearance.AddChild(diffuse);

            // TODO: these materials that are not owned, never get released from cache do they?
            appearance.RemoveMaterial();
          
            appearance.AddChild(emissiveWhite);

            appearance.BlendingMode = MTV3D65.CONST_TV_BLENDINGMODE.TV_BLEND_ADD;  // TV_BLEND_ADD is correct so that starfield shines through

            model.AddChild(appearance);
            model.AddChild(quad);

            return model;
        }
        #endregion
    }
}