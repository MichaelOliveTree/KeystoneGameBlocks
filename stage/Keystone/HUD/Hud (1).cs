using System;
using System.Collections.Generic;
using KeyCommon.Flags;
using Keystone.Appearance;
using Keystone.Cameras;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Immediate_2D;
using Keystone.Portals;
using Keystone.Resource;
using Keystone.Types;

namespace Keystone.Hud
{
	public delegate void HUDAddRemoveElement_Retained (Entity entity);
	public delegate void HUDAddElement_Immediate (Entity parent, ModeledEntity entity, bool entityTranslationIsInCameraSpace);


	#region Tool Preview Visuals Helper Classes
	/// <summary>
	/// A helper class for displaying HUD graphics related to the use of
	/// in game editing tools such as waypoint placer, asset placement, terrain painting
	/// wall painting, etc.
	/// </summary>
	public interface IHUDToolPreview : IDisposable
	{
		void Preview (RenderingContext context, Keystone.Collision.PickResults pick);
		void Clear();
	}
	 
	public abstract class HUDToolPreview : IHUDToolPreview
	{
		
		protected HUDAddRemoveElement_Retained mAddRetainedHandler;
		protected HUDAddRemoveElement_Retained mRemoveRetainedHandler;
		protected HUDAddElement_Immediate mAddImmediateHandler;
		protected bool mDisposed = false;
		

		public HUDToolPreview ( HUDAddElement_Immediate addImmediateHandler, 
		                       HUDAddRemoveElement_Retained addRetainedHandler, 
		                       HUDAddRemoveElement_Retained removeRetainedHandler)
		{
			mAddRetainedHandler = addRetainedHandler;
			mRemoveRetainedHandler = removeRetainedHandler ;
			mAddImmediateHandler = addImmediateHandler ;
		}
		
		public virtual void Preview (RenderingContext context, Keystone.Collision.PickResults pick) {}
		public virtual void Clear() {}
		 
		 
		 
		 #region IDisposable Members
        ~HUDToolPreview()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) here is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // pass true if managed resources should be disposed. Otherwise false.
        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    DisposeManagedResources();
                    mDisposed = true;
                }
                DisposeUnmanagedResources();
            }
        }

        protected virtual void DisposeManagedResources()
        {
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }

        protected void CheckDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(this.GetType().Name + " is disposed.");
        }

        protected bool IsDisposed
        {
            get { return mDisposed; }
        }
    #endregion
	}
    	
	public class NullPreview : HUDToolPreview 
	{
		public NullPreview () 
			: base (null, null, null) 
		{}
	}
	
	/// <summary>
	/// Translation, Scale, Rotation HUD Preview class.
	/// </summary>
	public class TransformPreview : HUDToolPreview
	{
		protected ModeledEntity mSource;
		protected Keystone.EditTools.TransformTool mTool;
		
		public TransformPreview (Keystone.EditTools.TransformTool tool, ModeledEntity sourceEntity, 
		                         HUDAddElement_Immediate addImmediateHandler, 
		                         HUDAddRemoveElement_Retained addRetainedHandler, 
		                         HUDAddRemoveElement_Retained removeRetainedHandler) : base (addImmediateHandler, addRetainedHandler, removeRetainedHandler)
		{
			
			if (sourceEntity == null || tool == null) throw new ArgumentNullException ();
			mSource = sourceEntity;

            // August.24.2017
            // NOTE: we do not recurse on LoadTVResource because for Interior's we
            // do not want CelledRegion's cellmap db and deck TVMeshes loaded for a preview
            // entity.  However, we do need to grab the Geometry and Appearance nodes under the mSource and load it.
            // NOTE: Also note that we do not inadvertantly load the Entity Script either which 
            // we don't want during preview.
            //bool recurse = false;
            //Keystone.IO.PagerBase.LoadTVResource (mSource, recurse);
            Model[] models = mSource.SelectModel(SelectionMode.Render, 0);
            if (models != null)
            {
                for (int i = 0; i < models.Length; i++)
                {
                    Keystone.Appearance.Appearance appearance = models[i].Appearance;
                    Keystone.IO.PagerBase.LoadTVResource(appearance, true);
                    Keystone.Elements.Geometry geometry = models[i].Geometry;
                    Keystone.IO.PagerBase.LoadTVResource(geometry, false);
                }
            }

            mTool = tool;
			
			if (mAddRetainedHandler != null)
    			mAddRetainedHandler (mTool.Control);
		}
		
		#region IDisposable
		protected override void DisposeManagedResources()
		{
			base.DisposeManagedResources();
			
			if (mRemoveRetainedHandler != null && mTool.Control != null)
				mRemoveRetainedHandler (mTool.Control);
		} 
		#endregion
		
		public override void Preview(RenderingContext context, Keystone.Collision.PickResults pick)
		{
			// need only update the transformation 
            if (mSource != null) // for some derived types, mSource _is_ null but mTool.Control is not.
            {
                //System.Diagnostics.Debug.Assert (mSource != mTool.Source);
                
                mSource.Translation = mTool.ComponentTranslation; // NOTE: CameraSpace is done for 3DHUD items in Hud.cs.Render() but not (yet) for 2d items.  I might change that
           	 	mSource.Dynamic = false;
            	mSource.Rotation = mTool.ComponentRotation;
            }

            if (mTool.Control != null)
	            mTool.Control.Translation = mTool.ComponentTranslation;
		}
		
		public override void Clear()
		{
			//
		}
		
        // TODO: this does not appear to be working anymore.  Preview geometry is just white.
		// assigns a replacement material over an entire model's Appearance
        protected void SetHudPreviewMaterial(Node node, Keystone.Appearance.Material material)
        {
            if (node is Appearance.GroupAttribute)
            {
                Appearance.GroupAttribute attrib = (Appearance.GroupAttribute)node;
                if (material != attrib.Material)
                {
                	if (attrib.Shader != null)
                	{
                		attrib.RemoveShader();
                	}
	                if (attrib.Material != null)
	                {
	                    attrib.RemoveMaterial();
	                }
	
	                attrib.AddChild(material);
	                	                
	                if (material.Opacity != 1.0f)
	                    attrib.BlendingMode = MTV3D65.CONST_TV_BLENDINGMODE.TV_BLEND_ALPHA;
	                else
	                    attrib.BlendingMode = MTV3D65.CONST_TV_BLENDINGMODE.TV_BLEND_NO;
		                
	                attrib.SetChangeFlags (Enums.ChangeStates.AppearanceNodeChanged | Enums.ChangeStates.AppearanceParameterChanged, Enums.ChangeSource.Target);
                }
            }

            // recurse
            if (node is IGroup)
            {
                IGroup group = (IGroup)node;
                if (group.Children != null && group.ChildCount > 0)
                    for (int i = 0; i < group.ChildCount; i++)
                        SetHudPreviewMaterial(group.Children[i], material);
            }
        }
		
	}
	
	public class PlacementPreview : TransformPreview
	{
		protected Material mInvalidPlacementMaterial;
		protected Appearance.Material mOverrideMaterial;  // material that will override a model's existing while tool is active
		
		public PlacementPreview (Keystone.EditTools.TransformTool tool, ModeledEntity source, 
		                         HUDAddElement_Immediate addImmediateHandler,
		                         HUDAddRemoveElement_Retained addRetainedHandler, 
		                         HUDAddRemoveElement_Retained removeRetainedHandler) : base (tool, source, addImmediateHandler, addRetainedHandler, removeRetainedHandler)
		{
			
			CreatePreviewInvalidPlacementOverrideMaterial();
			CreatePreviewOverrideMaterial();
			
            // iterate through all appearances and change their materials to use an alpha red material
            Material materialOverride = mOverrideMaterial;
            // NOTE: if Source is not a clone, then tool.Source and source will be the same.  This means
            // the Source is already a component inside the SceneGraph and editing it's Material here
            // could cause an Exception to be thrown during TVMesh.Render() call.  So we avoid changing
            // the material here.  But when do we change the material?  This leads me to believe
            // that we do need to clone the source entity afterall.
            if (tool.Source != source)
            {
                if (materialOverride != null)
                {
                    SetHudPreviewMaterial(source, materialOverride);
                }
            }
		}
		
        #region IDisposable
        protected override void DisposeManagedResources ()
        {
        	base.DisposeManagedResources();
        	
            if (mOverrideMaterial != null)
                Keystone.Resource.Repository.DecrementRef(mOverrideMaterial);

            
            if (mInvalidPlacementMaterial != null)
                Keystone.Resource.Repository.DecrementRef(mInvalidPlacementMaterial);
        }
		#endregion
		
		public override void Preview(RenderingContext context, Keystone.Collision.PickResults pick)
		{
			base.Preview(context, pick);
		
			// NOTE: we pass in context.Region and not context.Scene.Root since we want this preview
			//       parented in Immediate mode to the PVS for the ray origin region. (which is same as camera region)
			
			// NOTE: Interior uses derived type 'InteriorPlacementToolPreview' and not this one
			mAddImmediateHandler(context.Region, mSource, false);
		}
		
        private void CreatePreviewInvalidPlacementOverrideMaterial()
        {
            if (mInvalidPlacementMaterial == null)
            {
                mInvalidPlacementMaterial = Keystone.Appearance.Material.Create(Keystone.Appearance.Material.DefaultMaterials.red_fullbright);
                mInvalidPlacementMaterial.Opacity = 0.6f;
                // NOTE: without LoadTVResource() this material will never load
 //               Keystone.IO.PagerBase.LoadTVResource(mInvalidPlacementMaterial);
                // PlacementTool.DisposeUnmanagedResources DecrementRef's this
                Keystone.Resource.Repository.IncrementRef(mInvalidPlacementMaterial);
            }
        }
        
        private void CreatePreviewOverrideMaterial()
        {
            // create material that should be used to override the material on the model while tool is active
            if (mOverrideMaterial == null)
            {
                // TODO: the more i think of it, override materials and the call to  should not be in the tool but in the HUDs
                mOverrideMaterial = Keystone.Appearance.Material.Create(Keystone.Appearance.Material.DefaultMaterials.green_fullbright);
                // HACK: opacity below 1.0 on non-reflective water shader will clip entirely.. not sure why... it isn't the shader
                // it must be an alpha-blending render state issue
                // TODO: but when the opacity is 1.0, the render of the placed entity is completely unlit! WTF? 
				// TODO: also the override material now doesn't render at all!  WTF!?  it must be shader
				// related... I think during placement, we not only need to override the Material, but the shader as well..
				// TODO: note that with the pssm.fx shader assigned, no texture is currently rendering on morena smuggler starship for example.				
                mOverrideMaterial.Opacity = 0.60f;
                // NOTE: without LoadTVResource() this material will never load
//                Keystone.IO.PagerBase.LoadTVResource(mOverrideMaterial);
                // PlacementTool.DisposeUnmanagedResources DecrementRef's this.. but it shouldn't. it should be done in this class right?
                Keystone.Resource.Repository.IncrementRef(mOverrideMaterial);
            }
        }
	}
	#endregion
    	

    public class Hud : Keystone.TileMap.IMapLayerGridObserver, IDisposable 
    {

        public struct BackgroundLoadingEntity
        {
            public string EntityFullPath;
            public int StartTime;
            public int NodeCount_Current;
            public int NodeCount_Total;
            public bool Completed;
        }
        
        public List<BackgroundLoadingEntity> mBackgroundLoadingEntities;
        
        public struct TextureFont // TODO: move to Keystone.Fonts?
        {
            public int TextureFontID;
            public string Name;
            public bool Bold;
            public int Size;
            public bool Underline;
            public bool Italic;
            public bool International;
            public bool ClearTypeAntialising;
            public Color Color;
        }

        protected TextureFont mFont;
        protected Color mDistanceLabelColor;

        protected Dictionary<string, List<ModeledEntity>> mHUD3D_Immediate;
        protected Dictionary<string, List<ModeledEntity>> mHUD3D_Immediate_CameraSpace;
        
        protected Dictionary<string, List<Keystone.Immediate_2D.IRenderable2DItem>> mHUD2D_Immediate;
        protected Dictionary<string, List<Keystone.Immediate_2D.IRenderable2DItem>> mHUD2D_Retained;
       
        protected Entity mRoot;  // usually scene.Root but not necessarily (assigned during Show/Hide)
        protected Entity mHud3DRoot;
        protected Controls.Control2D mGUI2DRoot;

        protected Keystone.EditTools.Tool mLastTool;
        protected IHUDToolPreview mPreviewGraphic; 
        protected ModeledEntity mPlacementToolGraphic;
        
        public Keystone.Hud.CoordinateGrid Grid;
        
        // state vars to flag when we need to attach or detach during next Update() call so that
        // we are not modifying the scenegraph outside of the thread that does all scenegraph modifications/updates.
        protected bool mAttach = false;
        protected bool mDetach = false;

        protected KeyCommon.Traversal.PickParameters[] mPickParameters;

        // TODO: instead a HUD maybe we associate a Layer node that ensures for instance
        // that GUI entity items added to the HUD are always added to the Root node and that will
        // ensure they are always in the [0] regionPVS
        public Hud() 
        {
        	// attach to the MapGrid so we can be notified of changes
        	if (CoreClient._CoreClient.MapGrid != null) // if Single region universe, Zone MapGrid layers won't exist
        		CoreClient._CoreClient.MapGrid.Attach (this);
        		
        	// private vars
            mHUD3D_Immediate = new Dictionary<string, List<ModeledEntity>>();
            mHUD3D_Immediate_CameraSpace = new Dictionary<string, List<ModeledEntity>> ();
            	
            mHUD2D_Immediate = new Dictionary<string, List<Keystone.Immediate_2D.IRenderable2DItem>>();
            mHUD2D_Retained = new Dictionary<string, List<Keystone.Immediate_2D.IRenderable2DItem>>();

            // create a HUD root node where Retained HUD items will attach so that they are not attached as child to Scene.Root.
            string id = Repository.GetNewName(typeof(Entities.ModeledEntity));
            mHud3DRoot = new ModeledEntity(id); // NOTE: For umbrella node just use ModeledEntity and not a Region because with HUD root as a Region node, it cannot be added as child o CelledRegion which can be our staring viewpoint region for culling purposes. new Region(id, parent.BoundingBox, 0);
            mHud3DRoot.Name = "HUD 3D Root";

            Repository.IncrementRef(mHud3DRoot);


            // must set flag on umbrella node as well as any children we add to it.
            // currently, a entity should only be of one type (eg exist in one layer)
            mHud3DRoot.SetEntityAttributesValue((uint)EntityAttributes.AllEntityAttributes, false);
            mHud3DRoot.SetEntityAttributesValue((uint)EntityAttributes.Dynamic, false);
            mHud3DRoot.SetEntityAttributesValue((uint)EntityAttributes.HUD, true);
            mHud3DRoot.Serializable = false;

            id = Repository.GetNewName(typeof(Keystone.Controls.Control2D));
            mGUI2DRoot = new Keystone.Controls.Control2D(id);
            mGUI2DRoot.Name = "HUD 2D GUI Root";
            Repository.IncrementRef(mGUI2DRoot);
            
            mGUI2DRoot.SetEntityAttributesValue((uint)EntityAttributes.AllEntityAttributes, false);
            mGUI2DRoot.SetEntityAttributesValue((uint)EntityAttributes.Dynamic, false);
            mGUI2DRoot.SetEntityAttributesValue((uint)EntityAttributes.HUD, true);
            mGUI2DRoot.Serializable = false;

            // HUD
            mDistanceLabelColor = Color.Gold;
            // TODO: some Viewports will not need a hug and thus not need to instantiate fonts and hud related items
            mFont = new TextureFont();
            mFont.Name = CoreClient._CoreClient.Settings.settingRead("graphics", "fontname");
            mFont.Size = CoreClient._CoreClient.Settings.settingReadInteger("graphics", "fontsize");
            mFont.Bold = false; // CoreClient._CoreClient.Settings.settingReadBoolean("graphics", "fontbold");
            mFont.Underline = false; // CoreClient._CoreClient.Settings.settingReadBoolean("graphics", "fontunderline");
            mFont.Italic = false; // CoreClient._CoreClient.Settings.settingReadBoolean("graphics", "fontitalic");
            mFont.International = false;// CoreClient._CoreClient.Settings.settingReadBoolean("graphics", "fontinternational");
            mFont.ClearTypeAntialising = false;// CoreClient._CoreClient.Settings.settingReadBoolean("graphics", "fontcleartypeaa");
            //System.Drawing.Font.Height;
            mFont.Color = Color.Green;

            mFont.TextureFontID = CoreClient._CoreClient.Text.TextureFont_Create("TextureFont", mFont.Name, mFont.Size, mFont.Bold, mFont.Underline, mFont.Italic, mFont.International, mFont.ClearTypeAntialising);
            if (mFont.TextureFontID > 0)
                System.Diagnostics.Debug.WriteLine("Hud.ctor() - Created font: " + mFont.Name + " at size: " + mFont.Size.ToString());
            else
                System.Diagnostics.Debug.WriteLine("Hud.ctor() - ERROR: Can't create textured font: " + mFont.Name + " at size: " + mFont.Size.ToString());

            // the column/row counts and spacing can be modified at runtime
            Grid = new Keystone.Hud.CoordinateGrid(3000, 3000);
            
            mBackgroundLoadingEntities = new List<BackgroundLoadingEntity>();
        }


        public TextureFont Font { get { return mFont; } }
        public int TextureFontID { get { return mFont.TextureFontID; } }

        public Color FontColor { get { return mFont.Color; } }       
        public Color DistanceLabelColor {get {return mDistanceLabelColor;}}
        public RenderingContext Context { get; set; }

        internal Entity RootElement3D
        {
            get 
            {
                if (mHud3DRoot == null) return null;
                return mHud3DRoot; 
            } 
        }

        internal Entity RootElement2D
        {
            get 
            {
                if (mGUI2DRoot == null) return null;
                return mGUI2DRoot; 
            } 
        }
        
                
        #region IMapLayerObserver interface
        public virtual void NotifyOnDatabaseCreated(TileMap.Structure structure, string layerName, int[,] data, int layerSubscriptX, int layerSubscriptY, int layerSubscriptZ, int tileCountX, int tileCountZ, float width, float depth)
        {
        	        	
        }
		
        public virtual void NotifyOnDatabaseChanged(string layerName, int layerSubscriptX, int layerSubscriptY, int layerSubscriptZ, int x, int z, int value)
        {
        }
        
        public virtual void NotifyOnDatabaseDestroyed(int layerSubscriptX, int layerSubscriptY, int layerSubscriptZ)
        {}
		#endregion
		
        #region Tool Events
        protected virtual void OnEditTool_Activated(Keystone.EditTools.Tool tool) 
        { 
        }

        protected virtual void OnEditTool_DeActivated(Keystone.EditTools.Tool tool) 
        { 
        }

        protected virtual void OnEditTool_ToolChanged(Keystone.EditTools.Tool tool)
        { 
        }

        #endregion

        public virtual VisibleItem? Iconize(LightInfo lightInfo, double iconScale)
        {
            // item.entity, item.model, item.cameraSpacePosition, item.DistanceToCameraSq
            return null;
        }

        public virtual VisibleItem? Iconize(VisibleItem visibleItem, double iconScale)
        {
            // item.entity, item.model, item.cameraSpacePosition, item.DistanceToCameraSq
            return null;
        }


        /// <summary>
        /// Mouse Pick 2D and 3D HUD Elements that are not apart of the main Scene graph but instead descended from mGUI2DRoot and mHUD3DRoot which are typically not serializable.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="regionSpaceRay"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Keystone.Collision.PickResults Pick(RenderingContext context, Ray regionSpaceRay, KeyCommon.Traversal.PickParameters parameters)
        {
            if (this.mHud3DRoot == null || this.mHud3DRoot.SceneNode == null) return null;
                        
            #if DEBUG
            bool debugOutput = parameters.DebugOutput;
            parameters.DebugOutput = false;
            #endif
            
            Traversers.Picker _picker = new Traversers.Picker();

            // first perform 2D pick test of TexturedQuads under mGUI2DRoot. This does not traverse the entire scene
            Keystone.Collision.PickResults result = _picker.Pick(context.Viewport, context.Region, this.mGUI2DRoot.SceneNode, regionSpaceRay, parameters);

            // if no 2D element picked, perform 3D picking of 3D HUD Elements only by starting traversal at mHud3DRoot.  This does not traverse the entire scene
            if (result.HasCollided == false)
	            result = _picker.Pick(context.Viewport, context.Region, this.mHud3DRoot.SceneNode, regionSpaceRay, parameters);
            
            return result;
        }

        public virtual void OnEntityMouseOver(Keystone.Collision.PickResults pickResult)
        {
        }

        public virtual void OnEntityClicked (Keystone.Collision.PickResults pickResult)
        {
        }
        
        public virtual void OnEntityDoubleClicked (Keystone.Collision.PickResults pickResult)
        {
        }

        #region Background Loading Entities
        public void BackgroundLoadingEntity_Add(string fullPath)
        {
            if (mBackgroundLoadingEntities == null)
                mBackgroundLoadingEntities = new mBackgroundLoadingEntities();
                
            // init struct
            backLoadEntInfo.EntityFullPath = fullPath;
            backLoadEntInfo.StartTime = Time.Now();
            backLoadEntInfo.NodeCount_Total = totalNodes;
            backLoadEntInfo.NodeCount_Current = 0;
            backLoadEntInfo.Completed = false;
            
            mBackgroundLoadingEntities.Add (backLoadEntInfo);
        }
        
        
        #end region
        
        #region Text Labels
        private object m2DImmediateLock = new object ();
        
        public void GenerateTextLabel (VisibleItem item)
        {
        	// NOTE: Since our threaded Culler will add text labels as visible items are found
        	//       the access to our mHUD2D_Immediate collections is synchronized
        	lock (m2DImmediateLock)
        	{      		
        		try 
        		{
		            if (this.Context.ShowEntityLabels)
		            {
						if (item.Entity is IEntityProxy)
		                {
		                	
		                	if (item.Entity is Proxy3D)
		                	{
		                		Proxy3D proxy = (Proxy3D)item.Entity;

                                //IEntityProxy proxy = (IEntityProxy)item.Entity;

                                // NOTE: recall that since proxy's are placed on root, the proxy.Translation is also equal to proxy.GlobalTranslation
                                // NOTE: we don't want the proxy distancesquared, we want the actual referencedEntity distanceSquared, but that is not shown in our NavHud only the proxy so we should not generate distance, just .Name
                                CreateLabels(item.CameraSpacePosition, item.Entity.Name); //, GetDistanceLabel(item.DistanceToCameraSq));
		                	}
		                	else 
		                	{
			                	Controls.Control2D guiControl = (Controls.Control2D)item.Entity;
			                	IEntityProxy proxy = (IEntityProxy)item.Entity;
								
			                	// NOTE: recall that since proxy's are placed on root, the proxy.Translation is also equal to proxy.GlobalTranslation
			                	CreateLabels (guiControl.CenterX, guiControl.CenterY, guiControl.ZOrder, proxy.Name, GetDistanceLabel(item.DistanceToCameraSq ), DistanceLabelColor.ToInt32());
		                	}
		                }
		        		else
		                {
		                    // TODO: i should have each model "show label" and then allow only
		                    //       one of our multi sequence models to show a label
		                    // and to disable them altogether on orbits.
		                    // TODO: allow the root entity to disable for all children too
		                    if (string.IsNullOrEmpty(item.Entity.Name)) return;
                            if (item.Entity.GetEntityFlagValue ("playercontrolled") == true) return;
                            if (item.Entity.Container is Vehicles.Vehicle) return;
                            
                            if (item.Entity.Parent is Celestial.World && (item.DistanceToCameraSq > Celestial.Temp.AU_TO_METERS * Celestial.Temp.AU_TO_METERS)) return;

		                    CreateLabels (item.CameraSpacePosition, item.Entity.Name, GetDistanceLabel(item.DistanceToCameraSq), DistanceLabelColor.ToInt32());
		                }
		            }
        		}
        		catch (Exception ex)
        		{
        			System.Diagnostics.Debug.WriteLine ("Hud.GenerateTextLabel() - " + ex.Message);
        		}
            }
        }
        
		// TODO: These should be renamed to DrawLable_Immediate or AddLabel_Immediate
		// TODO: Furthermore, these should be moved to a Keystone.2DImmediate.DrawHelper or some such 
        protected void CreateLabels(Vector3d cameraSpacePosition, string labelName)
        {
        	if (string.IsNullOrEmpty (labelName)) return; // do not render name or distance labels if name is null
        	
        	Vector3d screenPos = this.Context.Viewport.Project(cameraSpacePosition, Context.Camera.View, Context.Camera.Projection, Matrix.Identity());
            CreateLabels ((int)screenPos.x, (int)screenPos.y, (int)screenPos.z, labelName);
        }
        
        protected void CreateLabels(Vector3d cameraSpacePosition, string labelName, string labelDistance, int color)
        {
        	if (string.IsNullOrEmpty (labelName)) return; // do not render name or distance labels if name is null
        	
        	Vector3d screenPos = this.Context.Viewport.Project(cameraSpacePosition, Context.Camera.View, Context.Camera.Projection, Matrix.Identity());
            CreateLabels ((int)screenPos.x, (int)screenPos.y, (int)screenPos.z, labelName, labelDistance, color);
        }
        
		// TODO: i should add an option to pass in both the font color and the shadow font color
        private void CreateLabels(int screenPosX, int screenPosY, int screenPosZ, string labelName, string labelDistance, int color)
        {
        	if (string.IsNullOrEmpty (labelName)) return; // do not render name or distance labels if name is null
            if (screenPosZ < 0) return; // object is behind camera

            // NOTE: This 2DText is much better looking than the 3d text!
            // TODO: hardcoded offset for screenPosY is bad
            Renderable2DText text = new Renderable2DText(labelName, screenPosX, screenPosY + 5, this.FontColor.ToInt32(), this.TextureFontID);
            AddHUDEntity_Immediate (this.Context.Scene.Root, text);

            // TODO: hardcoded offset for screenPosY is bad
            text = new Renderable2DText(labelDistance, screenPosX, screenPosY + 21, color, this.TextureFontID);
            AddHUDEntity_Immediate (this.Context.Scene.Root, text);
        }
        
        private void CreateLabels(int screenPosX, int screenPosY, int screenPosZ, string labelName)
        {
        	if (string.IsNullOrEmpty (labelName)) return;
            if (screenPosZ < 0) return; // object is behind camera

            // NOTE: This 2DText is much better looking than the 3d text!
            // TODO: hardcoded offset for screenPosY is bad
            Color green = new Color (0, 255, 0, 255);
            //if (labelName == "VEHICLE")
            //    System.Diagnostics.Debug.WriteLine("Hud.CreateLabels() - Vehicle label found");
            Renderable2DText text = new Renderable2DText(labelName, screenPosX, screenPosY + 5, green.ToInt32(), this.TextureFontID);
            AddHUDEntity_Immediate (this.Context.Scene.Root, text);
        }

        protected string GetVelocityLabel(Vector3d velocity)
        {
            double length = velocity.Length;
            const double METERS_TO_KILOMETERS = 1000d;

            string result = string.Format("{0:#,###} km\\s", length * METERS_TO_KILOMETERS);
            if (length < 10000)
                result = string.Format("{0:#,###} m\\s", length);
            return result;
        }
        protected string GetDistanceLabel(double distanceMetersSquared)
        {
            string result = null;
            double distanceMeters = Math.Sqrt (distanceMetersSquared);

            if (distanceMeters >= 100000000000000000d) // ten million AU
            {
                decimal distanceLY = Celestial.Temp.METERS_TO_LY * (decimal)distanceMeters;
                result = string.Format("{0:#,###,###.##} LY", distanceLY);
            }         
            else if (distanceMeters >= 10000000000000d) // 10 billion kilometers ~60AU
            {
                double distanceAU = Celestial.Temp.METERS_TO_AU * distanceMeters;
                result = string.Format("{0:#,###,###} AU", distanceAU);
            }
            else if (distanceMeters >= 10000000000d) // ten million kilometers
            {
                double distanceAU = Celestial.Temp.METERS_TO_AU * distanceMeters;
                result = string.Format("{0:#,###,###.##} AU", distanceAU);
            }
            else if (distanceMeters > 1000d) // 1 kilometer
            {
                double distanceKM = distanceMeters / 1000d;
                //result = string.Format("{0:#,###,###.##} km", distanceKM);
                // result = string.Format("{0:#,###,###} km", distanceKM);
                result = string.Format("{0:#,###} km", distanceKM);
                //result = string.Format("{0:#} km", distanceKM);
            }
            else if (distanceMeters > 1d) // 1 meter
            	result = string.Format("{0:#,###} meters", distanceMeters);
            else
                result = string.Format("{0:#.##} meters", distanceMeters);


            //double value = System.Math.Sqrt (item.DistanceToCameraSq);
            // Formatting strings--> http://idunno.org/archive/2004/07/14/122.aspx
            //System.Diagnostics.Debug.WriteLine("Distance to camera = " + String.Format("{0:£#,##0.00;(£#,##0.00);Nothing}", value));

            return result;
        }
        #endregion
        
		
        // Any Retained HUD items should be added in UpdateBeforeCull() whereas UpdateAfterCull()
        // is reserved for 2D Immediate items since RegionPVS are not accessible until after Cull()
        public virtual void UpdateBeforeCull(RenderingContext context,  double elapsedSeconds)
        {
        	context.PickParameters = mPickParameters[0];
        	
            if (mAttach)
                Attach();
            else if (mDetach)
                Dettach();

    		                                                     
            if ((bool)context.GetCustomOptionValue (null, "show axis indicator"))
            {
	            // TODO: we get exceptions sometimes when trying to load
	            // axis indicator in this thread.  We should background page these
	            //
            	LoadAxisIndicator();
	            UpdateAxisIndicator();
            }
        }

        // IMPORTANT: 3d Lines must be rendered in relation to the Region they are in.  Thus
        // they are added as renderable2ditems in the _currentRegionSet.  However, the RegionPVS
        // are not created until Cull() so HUD 2d immediate items should be added during
        // UpdateAfterCull() whereas Retained HUD items should be added in UpdateBeforeCull()
        //
        // Keep in mind also that during Cull Traversal, the RegionPVS are created and 2d such as
        // debug lines for culled items should be added to the RegionPVS right then as they are traversed
        // rather than here in HUD.
        public virtual void UpdateAfterCull(RenderingContext context,  double elapsedSeconds)
        {
        }
        
        public virtual void Render(RenderingContext context, List<RegionPVS> regionPVSList)
        {
            // NOTE: Root HUD3D element must always
            //       be at origin because this umbrella is not just for menu buttons on near plane
            //       it's also for sensor contacts and planet iconology and lightbulb icons during editor, etc!

            // Immediate 3d only.  Retained are culled by normal scene culler and rendered by normal scene traversers
            if (mHUD3D_Immediate != null && mHUD3D_Immediate.Count > 0)
            {
                // for every regionPVS, add all 3d hud items that are assigned to them
                for (int i = 0; i < regionPVSList.Count; i++)
                {
                    List<ModeledEntity> entityList = null;
                    if (mHUD3D_Immediate.TryGetValue(regionPVSList[i].ID, out entityList) == false) 
                    	continue;
                    
                    if (entityList == null || entityList.Count == 0) 
                    	continue;
         
                    for (int j = 0; j < entityList.Count; j++)
                    	RenderImmediate (entityList[j], false, regionPVSList[i]);
                }
        	}

            // Immediate 3d Camera Space only.  Coordinates do not need to be converted to camera space because they already are
            if (mHUD3D_Immediate_CameraSpace != null && mHUD3D_Immediate_CameraSpace.Count > 0)
            {
                // for every regionPVS, add all 3d hud items that are assigned to them
                for (int i = 0; i < regionPVSList.Count; i++)
                {
                    List<ModeledEntity> entityList = null;
                    if (mHUD3D_Immediate_CameraSpace.TryGetValue(regionPVSList[i].ID, out entityList) == false) 
                    	continue;
                    
                    if (entityList == null || entityList.Count == 0) 
                    	continue;
         
                    for (int j = 0; j < entityList.Count; j++)
                    	RenderImmediate (entityList[j], true, regionPVSList[i]);
                }
        	}
            
            // Immediate 2d only.  Retained are rendered by normal scene traversers
            bool TEMP_HACK_USE_FAR_FRUSTUM = false; // Grid.UseFarFrustum; // TODO: cannot hardcode this because these aren't just grid lines we're rendering here but all 2d immediate primitives
            if (mHUD2D_Immediate != null && mHUD2D_Immediate.Count > 0)
            {
                for (int i = 0; i < regionPVSList.Count; i++)
                {
                    List<Keystone.Immediate_2D.IRenderable2DItem> list = null;
                    if (mHUD2D_Immediate.TryGetValue(regionPVSList[i].ID, out list) == false) 
                    	continue;
                    
                    if (list == null || list.Count == 0) 
                    	continue;
                    
                    for (int j = 0; j < list.Count; j++)
                        // Note currently im pre-subtracting the region's region relative camerapos
                        // to render the 2d items in camera space, but i should not have to do that...
                    	// because often times i dont know what that value is
            			if (list[j] != null)
                            regionPVSList[i].Add(list[j], TEMP_HACK_USE_FAR_FRUSTUM);
                }
            }

            // note: for now we must clear at end of render as opposed to start of Update() else we
            //       remove items from list that were added prior to Update()
            mHUD3D_Immediate.Clear();
            mHUD3D_Immediate_CameraSpace.Clear();
            mHUD2D_Immediate.Clear();
        }

        private void RenderImmediate (ModeledEntity entity, bool entityAlreadyInCameraSpace, RegionPVS pvs)
        {

			if (entity == null) 
				return; // i think its a race condition here elements in list that has some items null and not set yet so we just skip them
            
            Model[] models = entity.SelectModel(SelectionMode.Render, -1);

            if (models == null) 
            	return;
            
            
            for (int k = 0; k < models.Length; k++)
            {
            	// camera space is always for each model, not entity since models can have their own offsets
                Vector3d cameraSpacePosition = models[k].DerivedTranslation;
            	if (entityAlreadyInCameraSpace == false)
					cameraSpacePosition -= pvs.RegionRelativeCameraPos;
            	
                if (entity.UseFixedScreenSpaceSize && entity.ScreenSpaceSize > 0)
                {
                    double scale = Context.GetConstantScreenSpaceScalePerspective(cameraSpacePosition, entity.ScreenSpaceSize);
                    // TODO: isn't there a way to do this without actually affecting the scale
                    // permanently? but at same time we want hierarchical scaling so probably not?
                    // TODO: translation widget is one test case?   is this scale even needed? arent we computing fixed screenspace scale already
                    //       in Model.Draw()?  Because im thinking this entity.Scale = is legacy from before we added these immediates to regionPVSList???
                    //       ... actually no, it seems model.draw() only does scaling for fitting large worlds in frustum
                    entity.Scale = Vector3d.Scale(scale);
                }

            
                VisibleItem itemInfo = new VisibleItem(entity, models[k], cameraSpacePosition);
                pvs.Add(itemInfo);
            }
        }
        
        // GUI is always PER VIEWPORT / CONTEXT and can never be tied just to one Scene.
        // This is why the GUIRoot is also specific to each context and must always be attached
        // to the region that the Context is in.
        public void ShowDialog(Controls.Control control, System.Drawing.Point location)
        {

        }

        public virtual void ContextualizeMenu(RenderingContext context, System.Drawing.Point vpRelativeMousePos, Keystone.Collision.PickResults pickResult)
        { 
        }



        internal virtual void Show(Entity parent)
        {

        	// NOTE: this will be called for each Viewport including QuickLook if applicable.
        	// TODO: if instead of attach/detach we simply Enable = True/False the root 2d and 3d nodes, we shouldnt have to wait for update() to make that type of change.
        	mDetach = false;
        	mAttach = true; // flag attach so next Update() we can call it and this way scene modification occurs during update() and not at any unpredictable time
            mRoot = parent;
        }

        internal virtual void Hide(Entity parent)
        {
        	mAttach = false;
            mDetach = true;
            mRoot = parent;
        }
		
        // attach and detach are called from Update() if mAttach or mDetach are "true" so that
        // the scenegraph modifications only occur during our Update() 
        internal void Attach()
        {
            if (mRoot == null || mHud3DRoot.Parent != null) return; // duplicate call

            // TODO: rather than attach and detach, why not just .Enable = True/False?
            //       and only attach and detach on ctor and dtor respectively.
            mRoot.AddChild(mHud3DRoot);
            mRoot.AddChild(mGUI2DRoot);
            mAttach = false;
            mDetach = false;
        }

        internal void Dettach()
        {
            if (mHud3DRoot.Parent == null) return; // duplicate call

            mRoot.RemoveChild(mHud3DRoot);
            mRoot.RemoveChild(mGUI2DRoot);
            mDetach = false;
        }

        // NOTE: 2D primitives coordinates passed in are already presumed to be in camera space
        public void AddHUDEntity_Immediate(Entity parent, Keystone.Types.BoundingBox box, int color)
        {
        	Keystone.Immediate_2D.Renderable3DLines lines = new Renderable3DLines(BoundingBox.GetEdges (box), color); 
        	AddHUDEntity_Immediate (parent, lines);
        }
        
        // NOTE: 2D primitives coordinates passed in are already presumed to be in camera space
        public void AddHUDEntity_Immediate(Entity parent, Keystone.Types.BoundingBox[] boxes)
        {
        	
        }
        
        /// <summary>
        /// Items added to Immediate are injected into the root PVS and therefore are not visible in other viewports in the workspace
        /// because they are not part of the actual scene.  Immediate Items cannot be mouse picked.
        /// </summary>
        /// <remarks>
        /// NOTE: 2D primitives coordinates passed in are already presumed to be in camera space
        /// </remarks>
        /// <param name="parent"></param>
        /// <param name="item"></param>
        public void AddHUDEntity_Immediate(Entity parent, Keystone.Immediate_2D.IRenderable2DItem[] item)
        {
            if (item == null) return;
            for (int i = 0; i < item.Length; i++)
                AddHUDEntity_Immediate(parent, item[i]);
        }
        
        	
        /// <summary>
        /// Items added to Immediate are injected into the root PVS and therefore are not visible in other viewports in the workspace
        /// because they are not part of the actual scene.  Immediate Items cannot be mouse picked.
        /// </summary>
        /// <remarks>
        /// NOTE: 2D primitives coordinates passed in are already presumed to be in camera space
        /// </remarks>
        /// <param name="control"></param>
        /// <param name="location"></param>
        public void AddHUDEntity_Immediate(Entity parent, Keystone.Immediate_2D.IRenderable2DItem item)
        {
            if (parent == null) return;
        	if (parent is Region == false) throw new ArgumentOutOfRangeException("Parent must always be a Region because the Region.ID is used to identify the RegionPVS.");
        	List<Keystone.Immediate_2D.IRenderable2DItem> rootList;

            if (mHUD2D_Immediate.TryGetValue( parent.ID, out rootList) == false)
                mHUD2D_Immediate.Add(parent.ID, new List<Keystone.Immediate_2D.IRenderable2DItem>());

            mHUD2D_Immediate[parent.ID].Add(item);
        }

        /// <summary>
        /// Items added to Immediate are injected into the root PVS and therefore are not visible in other viewports in the workspace
        /// because they are not part of the actual scene.  Immediate Items cannot be mouse picked.
        /// </summary>
        public void AddHUDEntity_Immediate(Entity parent, ModeledEntity element, bool entityTranslationIsCameraSpace)
        {
        	if (parent is Region == false) throw new ArgumentOutOfRangeException("Hud.AddHUDEntity_Immediate() - Parent must always be a Region because the Region.ID is used to identify the RegionPVS.");
        	
            List<ModeledEntity> rootList;

            if (entityTranslationIsCameraSpace)
            {
            	if (mHUD3D_Immediate_CameraSpace.TryGetValue(parent.ID, out rootList) == false)
                	mHUD3D_Immediate_CameraSpace.Add(parent.ID, new List<ModeledEntity>());

            	mHUD3D_Immediate_CameraSpace[parent.ID].Add(element);
            }
            else
            {
            	if (mHUD3D_Immediate.TryGetValue(parent.ID, out rootList) == false)
                	mHUD3D_Immediate.Add(parent.ID, new List<ModeledEntity>());

            	mHUD3D_Immediate[parent.ID].Add(element);
            }
        }

        
        // Retained items are added to a special HUD root Node and thus are ONLY visible
        // on the current HUD and NOT visible across all viewports.  Retained Items unlike
        // Immediate items CAN be MOUSE PICKED.
        public void AddHudEntity_Retained(Entity hudElement)
        {
            //
            // TODO: here if the parent is a celledregion, we must
            //       find a CellIndex and must query footprint and test validity?

            if (hudElement == null) return;
            //hudElement.CellIndex = 0;  //inialize to 0, but surely we should find actual index
            // and then move the entity if it moves?


            // easiest to compute the position of the marker relative to the parent
            // so we must add the marker to the parent if it's not already
            if (hudElement.Parent != null)
            {
                if (hudElement is Controls.Control2D)
                {
                    if (hudElement.Parent != mGUI2DRoot)
                    {
                        hudElement.Parent.RemoveChild(hudElement);
                        mGUI2DRoot.AddChild(hudElement);
                    }
                }
                else
                {
                    if (hudElement.Parent != mHud3DRoot)
                    {
                        hudElement.Parent.RemoveChild(hudElement);
                        mHud3DRoot.AddChild(hudElement);
                    }
                }
            }
            else
            {
                if (hudElement is Controls.Control2D)
                    mGUI2DRoot.AddChild(hudElement);
                else
                    mHud3DRoot.AddChild(hudElement);
            }
        }

        public void RemoveHUDEntity_Retained(Keystone.Immediate_2D.IRenderable2DItem item)
        {
        	throw new NotImplementedException ("Hud.cs.RemoveHUDEntity_Retained()");
            //if (item.Parent == null) return;

            //mMenuBar = null;

            //List<Controls.Control> rootList;

            //if (mHUD2D_Immediate.TryGetValue(item.Parent.ID, out rootList) == false)
            //    return;

            //mHUD2D_Immediate[item.Parent.ID].Remove(mMenuBar);
        }

        public void RemoveHUDEntity_Retained(Entity hudElement)
        {
            if (hudElement == null) return;

            if (hudElement.Parent == null || 
                (hudElement.Parent != mHud3DRoot && hudElement.Parent != mGUI2DRoot))
                return;

            // TODO: note: retained items are still not full unloaded because ref counts are still +1
            //       so during dispose we should finally decrementref them
            System.Diagnostics.Debug.Assert(mHud3DRoot == hudElement.Parent || mGUI2DRoot == hudElement.Parent);
            hudElement.Parent.RemoveChild(hudElement);
        }

        protected Keystone.Controls.Control LoadControl3D(string name, Model model,
           EventHandler mouseEnter, EventHandler mouseLeave,
           EventHandler mouseDown, EventHandler mouseUp,
           EventHandler mouseClick)
        {
            // menuButton - can be placed and re-used whenever our ProxyIcon is clicked
            string id = Repository.GetNewName(typeof(Keystone.Controls.Control));

            // create new menu button
            Keystone.Controls.Control ctl = new Keystone.Controls.Control(id);
            ctl.Name = name;

            //menuButton.InheritRotation = true; // allows us to move umbrella node to keep buttons in same position with respect to viewport
            //menuButton.UseFixedScreenSpaceSize = true;
            //menuButton.ScreenSpaceSize = 0.2f;
            ctl.Enable = true;
            ctl.CollisionEnable = false;
            ctl.Pickable = true;
            ctl.Dynamic = false; // no physics step is used

            // AddWaypointMenuButton events
            ctl.MouseEnter += mouseEnter;
            ctl.MouseLeave += mouseLeave; //change rollover material
            ctl.MouseDown += mouseDown; // change rollover material
            ctl.MouseUp += mouseUp;
            ctl.MouseClick += mouseClick;
            // no drag needed
            //menuButton.MouseDrag += AddWaypointMenuButton_OnMouseDrag;


            ctl.AddChild(model);
            return ctl;


        }

        protected Keystone.Controls.Control LoadMenu2D(string name, string menuTexture, int width, int height,
            EventHandler mouseEnter, EventHandler mouseLeave, 
            EventHandler mouseDown, EventHandler mouseUp, 
            EventHandler mouseClick)
        {
            // TODO: I think width and height here should be taken from the Control2D .Width and .Height
            //       and this way the textured quad can be easily shared.
            Model model = Model.Load2DTexturedQuad(name, menuTexture, width, height);

            // menuButton - can be placed and re-used whenever our ProxyIcon is clicked
            string id = Repository.GetNewName(typeof(Keystone.Controls.Control));
            
            // create new menu button
            Keystone.Controls.Control2D menuButton = new Keystone.Controls.Button2D(id);
            
            menuButton.Name = name;
            menuButton.Overlay = true;
            menuButton.Width = width;
            menuButton.Height = height;
            //menuButton.InheritScale = true; // this i dont understand, when the menuButton
            // is added a child of the entity who's click event will result in showing
            // of this menu, why cant i disable inheritscale and then
            // apply a separate screenspace size?
            // is the screenspace size incompatible with 
            // inheriting scale?  but if so
            // as i said, why cant i disable inherit and then
            // scale it up on my own?

            //menuButton.InheritRotation = true; // allows us to move umbrella node to keep buttons in same position with respect to viewport
            //menuButton.UseFixedScreenSpaceSize = true;
            //menuButton.ScreenSpaceSize = 0.2f;
            menuButton.Enable = true;
            menuButton.CollisionEnable = false;
            menuButton.Pickable = true;
            menuButton.Dynamic = false; // no physics step is used

            // AddWaypointMenuButton events
            menuButton.MouseEnter += mouseEnter;
            menuButton.MouseLeave += mouseLeave; //change rollover material
            menuButton.MouseDown += mouseDown; // change rollover material
            menuButton.MouseUp += mouseUp;
            menuButton.MouseClick += mouseClick;
            // no drag needed
            //menuButton.MouseDrag += AddWaypointMenuButton_OnMouseDrag;


            menuButton.AddChild(model);
            return menuButton;
        }


        #region Update HUD Elements
        protected ModeledEntity mAxisIndicator;
        private void UpdateAxisIndicator()
        {
            if (mAxisIndicator != null)
            {
            	// TODO: jitter occurs far from origin. this is because adding GlobalTranslation below is ruining our precision.
            	// TODO: surely we can find a near plane with 0 rotation, cache these corners
            	//       and then render any 3d entity that is tied to this near plane using
            	//       a regionPVS and frustum view/inverseview that is always at 0 rotation.  This would guarantee
            	//       anything rendered to it could never move based on rotation.
            	//		- but i dont think it's the rotation at all.  It's the .GlobalTranslation we add which ironicly
            	//        is only added because we know it will be negated later and the code expects to negate the camera translation
            	//        for camera space rendering.
                Vector3d v = Culling.ViewFrustum.GetFixedNearPlanePoint(Context.FOVRadians, Context.Viewport.Width, Context.Viewport.Height,  0.05d, 0.05d); 

                // transform to camera relative taking into account camera rotation
            	v =	Vector3d.TransformCoord(v, Context.Camera.InverseView); // before this was using pvs.InverseView which
            	
                // 'v'  is already camera space, so later we will not have to subtract relativeCameraRegion Position from it and
				// thus we avoid lots of precision issues when trying to render far from origin using Context.GlobalTranslation values
				mAxisIndicator.Translation = v;
                AddHUDEntity_Immediate (mRoot, mAxisIndicator, true);
            }
        }
        #endregion

        #region Load
        protected void LoadAxisIndicator()
        {
            if (mAxisIndicator == null)
            {
                mAxisIndicator = EditTools.Widgets.LoadTranslationWidget();
					              
                mAxisIndicator.Enable = true;
                mAxisIndicator.Pickable = false; // TODO: we need to set pickable = false for child models
                mAxisIndicator.Overlay = true; // axis we do want overlay = true
                mAxisIndicator.Dynamic = false;
                mAxisIndicator.UseFixedScreenSpaceSize = false;
                //mAxisIndicator.ScreenSpaceSize = .1f;
                mAxisIndicator.SetEntityAttributesValue((uint)EntityAttributes.AllEntityAttributes, false);
                mAxisIndicator.SetEntityAttributesValue((uint)EntityAttributes.HUD, true);
                mAxisIndicator.Serializable = false;

                Vector3d scale;
                scale.x = 0.1;
                scale.y = 0.1;
                scale.z = 0.1;
                mAxisIndicator.Scale = scale;

                // NOTE: We increment regardless of whether we grabbed the existing copy from Repository
                // because a second instance of the placement tool may be disposing still while this
                // new placementtool instance is activated so it could result in race condition with
                // it's deref.
                // NOTE: increment the ref count to keep it from falling out of scope.
                // NOTE: We only ever have to increment just the top most entity 
                // not each child.
                Repository.IncrementRef(mAxisIndicator);
                // Immediate Rendered Hud items must load pageable resources manually
				// NOTE: we queue this loading and do not call .LoadTVResource()
				Keystone.IO.PagerBase.QueuePageableResourceLoad (mAxisIndicator, null, true);
                // Keystone.IO.PagerBase.LoadTVResource (mAxisIndicator);
            }
        }
        #endregion

        #region IDisposeable
        public virtual void Dispose()
        {
        	if (CoreClient._CoreClient.MapGrid != null)
	        	CoreClient._CoreClient.MapGrid.Detach(this);
        	
            CoreClient._CoreClient.Text.Font_Delete(mFont.TextureFontID);
            if (mAxisIndicator != null)
	            Repository.DecrementRef(mAxisIndicator);
            
            Repository.DecrementRef(mHud3DRoot);
            Repository.DecrementRef(mGUI2DRoot);
        }
        #endregion
    }
}