using System;
using System.Collections.Generic;
using KeyCommon.Traversal;
using Keystone.Collision;
using Keystone.Culling;  // RegionPVS, FrustumInfo, PlanedFrustum, VisibleItemInfo, LightInfo
using Keystone.Entities;
using Keystone.FX;
using Keystone.Portals;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Cameras
{
	public class RenderingContext : IDisposable
	{

		public delegate void RenderingContextEventHandler(RenderingContext context);
		public event RenderingContextEventHandler CullingStart;
		public event RenderingContextEventHandler CullingComplete;
		public event RenderingContextEventHandler RenderingStart;
		public event RenderingContextEventHandler RenderingComplete;

		internal delegate void RegionSetCreatedDelegate(RegionPVS pvs);
		internal delegate void VisibleLightFoundDelegate(RegionPVS pvs, Lights.Light light, Vector3d cameraSpacePosition, IntersectResult intersection, BoundingBox cameraSpaceBoundingBox);
		internal delegate void VisibleItemFoundDelegate(RegionPVS pvs, Entities.Entity entity, Elements.Model model, Vector3d cameraSpacePosition, BoundingBox cameraSpaceBoundingBox);
		
		protected Keystone.Workspaces.IWorkspace3D mWorkspace;
		protected Scene.Scene  mScene;
		private Camera mCamera;
		private Viewport mViewport;
		internal ViewpointController mViewController;

		// by default, NO types are excluded so that we do not skip picking or culling of anything.
		private PickParameters mPickParameters;
        private KeyCommon.Flags.EntityAttributes mIncludedAttributes;
        private KeyCommon.Flags.EntityAttributes mAllowedAttributes;
		private ScaleCuller mCuller;
		private ScaleDrawer mDrawer;
		private Traversers.Picker mPicker;

		object regionSetListLock;
		private List<RegionPVS> mRegionSets;
		List<LightInfo> mVisibleLights = new List<LightInfo>();


		private double mNear;  // use doubles because our custom projection and view matrix calcs use doubles
		private double mFar;
		private double mNear2;
		private double mFar2;
		private double mFovRadians;
		
		private Viewport.ViewType mViewType;
		private Viewport.ProjectionType mProjectionType;
		private Viewport.ViewType mLastViewType;
		private Viewport.ProjectionType mLastProjectionType;

		private float mDistanceToOrthoPlane = 10000;
		private float mZoom = 1f; // Zoom of 1 = viewport.Height number of tvunits will be visible vertically
		private bool mTraverseInteriorsAlways;
		private bool mTraverseInteriorsThroughPortals;

		private double mMaxVisibleDistance;
		
		// shadow mapping (forward renderer only)
		internal Keystone.PSSM.CParallelSplitShadowMapping ParallelSplitShadowMapping;
		private bool mShadowMappingEnabled;
		
		// deferred shading
		private bool mDeferredEnabled = false;
		private TVRenderSurface mMultipleRenderTarget;
		private Keystone.Elements.Mesh3d mPointLightSphereMesh;
		private Shaders.Shader mPointLightShader;

		// bindable node stacks
		// TODO: fog? or IEnvironmentalVolume (covers fog, rainy areas, etc)
		private Stack<Keystone.Entities.Viewpoint> mViewpoints;
		public RegionPVS VisibleBackground;
		private Stack<Keystone.Elements.Background3D> mBackgrounds;

		private bool mEnabled;
		private double mElapsedSeconds;
		private bool mNeedNewCamera = true;

		// screenshots
		private bool mScreenShotScheduled;
		private string mScreenShotPath;
		private EventHandler mScreenShotSavedHandler;
		
		// HUD related
		// Strategy pattern, this Hud can be swapped in/out depending on what type of
		// Viewport the user has selected (eg Tactical, Strategic, Nav, Build Ship, etc)
		private Hud.Hud mHud;

		private Stats mStats = new Stats();


		// backColor is not a HUD property (perhaps a viewport property)
		// screenshot vars and methods too might be more viewport worthy than here in renderingcontext
		private Settings.PropertyTable mCustomOptions;
		private bool mHardwareOcclussionEnabled = true;
		// int mHardwareOcclussionSurfaceSizeFactor; // 1, 0.75, 0.5, 0.25

		private Color _backColor;

		// debug - TODO: should debug be more like Stats where i use
		//        a hashtable instead of all these hardcoded vars?
		
		private bool _showLightBoundingBoxes = false;
		private bool _showLightLocations = true;// false;
		private bool _showPortals = true;
		private bool _showFPS = true;
		private bool _showTVProfiler;
		private bool _showStats;

		// TODO: all of these can be moved to custom options
		private Color _fontColorStats = new Color(255, 255, 255, 255);
		private bool _showEntityLabels = false;
		private bool _showEntityBoundingBoxes = false;
		private bool _showRegionBoundingBoxes = false;
		private bool _showOctreeBoundingBoxes = false;
		private bool _showTerrainBoundingBoxes = false;
		private bool _drawHulls = false;
		private bool _drawBoundingSpheres = false;
		bool _showLineProfiler = false;
		
		private CONST_TV_COLORKEY _bboxColor = CONST_TV_COLORKEY.TV_COLORKEY_GREEN;
		private CONST_TV_COLORKEY _entityNodeBoxColor = CONST_TV_COLORKEY.TV_COLORKEY_WHITE;
		private CONST_TV_COLORKEY _regionNodeBoxColor = CONST_TV_COLORKEY.TV_COLORKEY_YELLOW;
		private CONST_TV_COLORKEY _regionBoxColor = CONST_TV_COLORKEY.TV_COLORKEY_MAGENTA;
		
		private CONST_TV_COLORKEY _bsphereColor = CONST_TV_COLORKEY.TV_COLORKEY_YELLOW;
		private CONST_TV_COLORKEY _hullColor = CONST_TV_COLORKEY.TV_COLORKEY_RED;
		private CONST_TV_COLORKEY _selectedColor = CONST_TV_COLORKEY.TV_COLORKEY_MAGENTA;


		double MAX_SHADOW_DRAW_DISTANCE_SQUARED;
		
		public RenderingContext(Keystone.Workspaces.IWorkspace3D workspace, Scene.Scene scene, Viewport viewport,
		                        double near, double far,
		                        double near2, double far2,
		                        double maxVisibleDistance,
		                        double fovRadians)
		{
			if (workspace == null || scene == null || viewport == null) throw new ArgumentNullException();
			mWorkspace = workspace;
			mViewport = viewport;
			mViewport.Context = this;
			
			mScene = scene;

			mNear = near;
			mFar = far;
			mNear2 = near2;
			mFar2 = far2;
			mMaxVisibleDistance = maxVisibleDistance;

			mFovRadians = fovRadians;
			mLastProjectionType = Viewport.ProjectionType.None;
			mLastViewType = Viewport.ViewType.None;

			mProjectionType = Viewport.ProjectionType.Perspective;
			mViewType = Viewport.ViewType.Free;

			mViewpoints = new Stack<Keystone.Entities.Viewpoint>();
			mViewController = new ViewpointController(this);

            mIncludedAttributes = KeyCommon.Flags.EntityAttributes.All; // all types included
			mAllowedAttributes = KeyCommon.Flags.EntityAttributes.All;  // no types are excluded
			

			mCuller = new ScaleCuller(this, OnRegionPVSCreated, OnVisibleItemFound, OnVisibleLightFound);
			mDrawer = new ScaleDrawer();
			mPicker = new Traversers.Picker();
			
			regionSetListLock = new object();
			mRegionSets = new List<RegionPVS>();

			// TODO: some of the viewports may not use "graphics" setting for instance
			// if this is a wireframe viewport only?
			DeferredRendering = CoreClient._CoreClient.Settings.settingReadBool("graphics", "deferred");
			mShadowMappingEnabled = CoreClient._Core.Settings.settingReadBool ("graphics", "shadowmap");
			MAX_SHADOW_DRAW_DISTANCE_SQUARED = double.Parse (CoreClient._CoreClient.Settings.settingRead("graphics", "maxshadowdrawrange"));
			MAX_SHADOW_DRAW_DISTANCE_SQUARED *= MAX_SHADOW_DRAW_DISTANCE_SQUARED; // distanceSquared
			
			bool WATER_RENDERING_ENABLED = false;
			if (WATER_RENDERING_ENABLED)
			{
				TV_COLOR refractionColor = new TV_COLOR (1f, 1f, 1f, .3f);
				TV_COLOR reflectionColor = new TV_COLOR (1f, 1f, 1f, .3f);
				float fresnel = 1.0f;
				// NOTE: FXWater.ctor() - requires callback to RenderScene()
				FX.FXWater water = new FXWater (1, 1, new Vector3d(0, 1, 0), true, true, reflectionColor, refractionColor, fresnel, RenderScene);
				CoreClient._CoreClient.SceneManager.Add (water);
			}
			bool glowEnabled = false; // CoreClient._CoreClient.Settings.settingReadBool ("graphics", "glow");
			if (glowEnabled)
			{
				// NOTE: ClientSceneManager.LoadFXProviders()  is where global ones are currently being added such as
				//       FXLasers.
				// TODO: we .Add() these to the SceneManager even though we are creating them PER RENDERING CONTEXT!  PER RENDERING CONTEXT
				//       providers should not be added there or else it's pointless making them per rendering context.
				// NOTE: FXGlow.ctor() - requires callback to RenderScene()
				FX.FXGlow glow = new FXGlow (Keystone.RenderSurfaces.RSResolution.R_1024x1024, Keystone.Types.Color.White, 0.1f, 1, RenderScene);
				CoreClient._CoreClient.SceneManager.Add (glow);
			}


		}

		public void Dispose()
		{
			UninitializeDeferred();
			mHud = null;
			mScene = null;
			mCamera = null;
			mViewport.Dispose ();
			mViewport = null;
			mViewController = null;
			mDrawer = null;
			mCuller = null;
			mPicker = null;
		}

		public string ID { get { return mViewport.Name; } }
		
		internal Stats Statistics { get { return mStats; } }
		
		//public Settings.PropertyTable CustomOptions { get { return mCustomOptions; } }

		public PickParameters PickParameters
        {
            get { return mPickParameters; }
            set { mPickParameters = value; }
        }
		public int Floor { get; set; }

		public double Far { get { return mFar; } }
		public double Far2 { get { return mFar2; } }
		public double Near { get { return mNear; } }
		public double Near2 { get { return mNear2; } }
		public double FOVRadians { get { return mFovRadians; } }
		public double MaxVisibleDistance { get { return mMaxVisibleDistance; } set { mMaxVisibleDistance = value; } }

        const int DEFAULT_TV_FONT_ID = 0;
        public int TextureFontID { get { if (mHud != null) return mHud.TextureFontID; return DEFAULT_TV_FONT_ID; } }
        public Color DistanceLabelColor { get { return mHud.DistanceLabelColor; } }
		public Color FontColor { get { return mHud.FontColor; } }
		public Color GridColor { get { return mHud.Grid.OuterColor; } set { mHud.Grid.OuterColor = value; } }
		public CONST_TV_COLORKEY SelectedColor { get { return _selectedColor; } set { _selectedColor = value; } }
		public CONST_TV_COLORKEY BoxColor { get { return _bboxColor; } set { _bboxColor = value; } }
		public CONST_TV_COLORKEY RegionNodeBoxColor { get { return _regionNodeBoxColor; } set { _regionNodeBoxColor = value; } }
		public CONST_TV_COLORKEY EntityNodeBoxColor { get { return _entityNodeBoxColor; } set { _entityNodeBoxColor = value; } }
		public CONST_TV_COLORKEY RegionBoxColor { get { return _regionBoxColor; } set { _regionBoxColor = value; } }

		public bool Enabled
		{
			get { return mEnabled; }
			set
			{
				mEnabled = value;
				if (mHud != null)
				{
					if (mEnabled)
						mHud.Show(mScene.Root);
					else
						mHud.Hide(mScene.Root);
				}
			}
		}

		// TODO: rather than IWorkspace perhaps all Contexts should
		// use a Workspace3D or IWorkspace3D that signifies this workspace has Selected, MouseOver, and Tool
		public Keystone.Workspaces.IWorkspace3D Workspace {get {return mWorkspace;}}
		
		public ViewpointController ViewpointController { get { return mViewController; } }

		public Viewport Viewport { get { return mViewport; } }

		public Camera Camera
		{
			get { return mCamera; }
		}

		public Scene.Scene Scene
		{
			get { return mScene; }
			internal set { mScene = value; }
		}
		
		public Hud.Hud Hud
		{
			get { return mHud; }
			set
			{
				mHud = value;
				if (mHud != null)
					mHud.Context = this;
			}
		}

		
		#region Custom Options
		// custom options can now be easily serialized to our app config, but should these options be
		// merged with our entity blackboard data?
		public void AddCustomOption(string category, string name, string type, object value)
		{
			Settings.PropertySpec spec = new Settings.PropertySpec(name, type, category, value);
			AddCustomOption(spec);
		}

		public void AddCustomOption(Settings.PropertySpec spec)
		{
			if (mCustomOptions == null)
			{
				mCustomOptions = new Settings.PropertyTable();
				mCustomOptions.SetValue += OnCustomOptions_SetValue;
				mCustomOptions.GetValue += OnCustomOptions_GetValue;
			}

			// if the option already exists, replace existing
			if (mCustomOptions.Properties.Contains(spec.Category + spec.Name))
				mCustomOptions.Properties.Remove(spec.Category + spec.Name);

			mCustomOptions.Properties.Add(spec);
			SetCustomOptionValue(spec.Category, spec.Name , spec.DefaultValue);
		}

		public object GetCustomOptionValue(string category, string propertyName)
		{
			if (mCustomOptions == null) return null;

			return mCustomOptions[category + propertyName];
		}

		public void SetCustomOptionValue(string category, string propertyName, object value)
		{
			if (mCustomOptions == null) throw new ArgumentOutOfRangeException ();

			mCustomOptions[category + propertyName] = value;
		}

		// these events fire when propertygrid control is changing the value and we want to persist the new value
		// to the underlying storage, however, if not using propertygrid, there's no real use to these events and
		// no real purpose in using propertySpec either.... we can just use knowledge blackboard so this custom
		// configuration and state becomes accessible to Behaviortree.
		// TODO: perhaps we can really optimize our blackboards when after initially creating them, we can define
		// the counts on serializing such than on deserialization we now know exact sizes to make arrays then we
		// can ditch collections and our arrays become guaranteed to use contiguous blocks of mem
		protected virtual void OnCustomOptions_SetValue(object value, Settings.PropertySpecEventArgs target)
		{
			target.Value = mCustomOptions[target.Property.Category + target.Property.Name];
		}

		protected virtual void OnCustomOptions_GetValue(object value, Settings.PropertySpecEventArgs target)
		{

			//if (e.Property.Name == "adapter")
			//    AppMain._core.DeviceCaps.CurrentAdapterName = (string)target.Value;
			//else if (e.Property.Name == "soundcards")
			//    AppMain._core.AudioManager.CurrentSoundCard = new Guid((string)target.Value);
			//else if (e.Property.Name == "speakers")
			//    AppMain._core.AudioManager.CurrentSpeaker = (string)target.Value;

			// TODO: How do i set the default value for some of these list items?  Normally
			// i set the "value" attribute in the XML, but for these sorts where it produces from a variable
			// list, there's no way to know a default value in advance. If we could instead pick the index=0 item...

			//TODO: Not sure how to determine from here whether the current displaymode
			// is available on the current adapter in scenarios where they change the adapter
			// but leave the display mode setting unchanged from the previous adapter setting.
			// I mean i can "set" it in the Graphics Manager
			// from here and then have it do the verification, but it still doesnt solve the problem
			// of getting the user to select a valid one in the grid.
			mCustomOptions[target.Property.Category + target.Property.Name] = target.Value;
		}
		#endregion

		#region Bindable Child Nodes  http://www.web3d.org/x3d/specifications/OLD/ISO-IEC-19775-X3DAbstractSpecification/Part01/components/core.html
		public Keystone.Entities.Viewpoint Viewpoint
		{
			// note: _scene.SceneInfo should push the default viewpoint onto the stack upon creation
			// of the RenderingContext
			get
			{
				if (mViewpoints == null || mViewpoints.Count == 0)
					return null;
				return mViewpoints.Peek();
			}
		}

		public Keystone.Elements.Background3D Background
		{
			get
			{
				if (mBackgrounds == null || mBackgrounds.Count == 0) return null;
				return mBackgrounds.Peek();
			}
		}

        /// <summary>
        /// the bound viewpoint is added to the relevant Region (Viewwpoint.StartingRegionID) in ClientPager.Update() 
        /// after the Region has been paged in
        /// </summary>
        /// <param name="viewpoint"></param>
        public void Bind(Keystone.Entities.Viewpoint viewpoint)
		{
			mViewpoints.Push(viewpoint);
		}

		public void UnBind(Keystone.Entities.Viewpoint viewpoint)
		{
			Keystone.Entities.Viewpoint vp = mViewpoints.Pop();
			System.Diagnostics.Debug.Assert(vp == viewpoint);
		}


		public void Bind(Keystone.Elements.Background3D background)
		{
			if (mRegionSets == null || mRegionSets.Count == 0) return;
			
			RegionPVS camerasStartingPVS = mRegionSets[0]; // index 0 is always camera starting RegionPVS!
			//RegionPVS backgroundRegionPVS = new RegionPVS(this, Camera.InverseView, Camera.View, Vector3d.Origin());
			Elements.Model[] models = background.SelectModel(Keystone.Elements.SelectionMode.Render, -1);

			if (models == null) return;
			
			for (int i = 0; i < models.Length; i++)
			{
				VisibleItem itemInfo = new VisibleItem(background, models[i], Vector3d.Zero());
				camerasStartingPVS.Add(itemInfo);
			}

			VisibleBackground = camerasStartingPVS;
		}

		public void UnBind(Keystone.Elements.Background3D background)
		{
		}
		#endregion

		
		public Viewport.ViewType ViewType
		{
			get { return mViewType; }
			set
			{
				mViewType = value;

				bool viewChanged = mLastViewType != mViewType ||
					(mProjectionType != mLastProjectionType);


				if (viewChanged)
					mNeedNewCamera = true;
			}
		}

		public Viewport.ProjectionType ProjectionType
		{
			get { return mProjectionType; }
			set
			{
				mProjectionType = value;

				bool viewChanged = mLastViewType != mViewType ||
					(mProjectionType != mLastProjectionType);


				if (viewChanged)
					mNeedNewCamera = true;
			}
		}

		public bool TraverseInteriorsThroughPortals
		{
			get { return mTraverseInteriorsThroughPortals; }
			set { mTraverseInteriorsThroughPortals = value; }
		}

		public bool TraverseInteriorsAlways
		{
			get { return mTraverseInteriorsAlways; }
			set { mTraverseInteriorsAlways = value; }
		}
		
		public bool ShowFPS
		{ get { return _showFPS; } set { _showFPS = value; } }

		public bool ShowTVProfiler
		{ get { return _showTVProfiler; } set { _showTVProfiler = value; } }

		public bool ShowLineProfiler
		{
			get { return _showLineProfiler; } set { _showLineProfiler = value; }
		}
		
		public bool ShowLineGrid { get { return mHud.Grid.Enable; } set { if (mHud == null) return;  mHud.Grid.Enable = value; } }
		public bool ShowEntityLabels { get { return _showEntityLabels; } set { _showEntityLabels = value; } }
		public bool ShowCullingStats { get { return _showStats; } set { _showStats = value; } }
		public bool ShowEntityBoundingBoxes { get { return _showEntityBoundingBoxes; } set { _showEntityBoundingBoxes = value; } }
		public bool ShowRegionBoundingBoxes { get { return _showRegionBoundingBoxes; } set { _showRegionBoundingBoxes = value; } }
		public bool ShowOctreeBoundingBoxes { get { return _showOctreeBoundingBoxes; } set { _showOctreeBoundingBoxes = value; } }
		public bool ShowTerrainBoundingBoxes { get { return _showTerrainBoundingBoxes; } set { _showTerrainBoundingBoxes = value; } }
		public bool ShowLightBoundingBoxes { get { return _showLightBoundingBoxes; } set { _showLightBoundingBoxes = value; } }

		public bool ShowLightLocation
		{
			get { return _showLightLocations; }
			set { _showLightLocations = value; }
		}

		public bool ShowPortals
		{
			get { return _showPortals; }
			set { _showPortals = value; }
		}

		public bool ShowBoundingSpheres
		{
			get { return _drawBoundingSpheres; }
			set { _drawBoundingSpheres = value; }
		}

		public bool ShowConvexHulls
		{
			get { return _drawHulls; }
			set { _drawHulls = value; }
		}

		//public bool EnableScalingFactor
		//{
		//    get { return ((ScaleDrawer )_drawer).mUseScalingFactor; }
		//    set { ((ScaleDrawer)_drawer).mUseScalingFactor = value; }
		//}

		public bool DeferredRendering
		{
			get { return mDeferredEnabled; }
			set
			{
				if (mDeferredEnabled == value) return; // already set
				
				mDeferredEnabled = value;

				if (mDeferredEnabled)
				{
					InitializeDeferred();
					UpdateDeferredRenderTargets();
				}
				else
				{
					UninitializeDeferred();
				}
			}
		}

        /// <summary>
        /// Ignored attributes are skipped on cull but their children
        /// are still traversed.  Excluded attributes have their children
        /// skipped as well.
        /// </summary>
        /// <param name="attributes"></param>
        public void AllowedEntityAttributes(KeyCommon.Flags.EntityAttributes attributes)
		{
			mAllowedAttributes |= attributes;
		}

		public void ToggleEntityAttributes(KeyCommon.Flags.EntityAttributes attributes)
		{
            mAllowedAttributes ^= attributes;
		}

		public void IncludeEntityAttributes(KeyCommon.Flags.EntityAttributes attributes)
		{
            mAllowedAttributes &= ~attributes;
		}

		
		#region Viewpoint & Camera Manipulation
		// it is OK for Region {get} to return null if no viewpoint is bound.  We can sometimes
		// wait for a viewpoint to be bound which signifies the start of a game.
		public Region Region
		{
			get
			{
				if (Viewpoint == null) return null;
				return Viewpoint.Region;
			}
		}
		
		// TODO: i suspect trying to set zoom here fails when our editorviewcontroller is overriding the zoom level each frame unless
		//       it is linking to camera.zoom itself
		//http://www.truevision3d.com/forums/tv3d_sdk_65/nonsquare_isometric_camera-t13994.0.html;msg96749#msg96749
		// the zoom value defines how many world units are seen vertically by the camera along its basis vectors.
		public float Zoom { get { return mCamera.Zoom; } set { mCamera.Zoom = value; } }

		// NOTE: We use DerivedTranslation of the Viewpoint so that the Viewpoint can be translated relative
		// to whatever entity it is child of
		public Vector3d Position { get { if (Viewpoint == null) return Vector3d.Zero(); return Viewpoint.DerivedTranslation; } }

		public Vector3d LookAt
		{
			get { return mCamera.LookAt; }
			set { mCamera.LookAt = value; }
		}

		public Vector3d Up
		{
			get { return mCamera.Up; }
			//set { _camera.Up = value; }
		}
		#endregion

		
		#region Camera Creation & Configuration
		/// <summary>
		/// </summary>
		private void CreateDedicatedCamera()
		{
			System.Diagnostics.Debug.Assert(mProjectionType != Viewport.ProjectionType.None);
			if (mCamera != null)
			{
				if (mCamera.Viewport == mViewport && mLastProjectionType == mProjectionType)
				{
					// if the projection hasn't changed, we'll re-use the existing camera
					// and we can retain our _camera.Zoom settings if we only changed View and not Projection
					return;
				}

				// TODO: should i preserve the last Zoom and other camera settings
				// for transfer to the new one?
				mCamera.Dispose();
				mCamera = null;
			}

			// TODO: after we refactor our ViewpointController, we wont need seperate versions as below and we'll be able to instance
			//       it just once in the ctor of this RenderingContext() and that also means we can assign things like EntityMouseOver and EntitySelected
			//       callbacks there as well.
			//       TODO: workspace is also passed into our InputController which will no longer be necessary either.
			switch (mProjectionType)
			{
				case Viewport.ProjectionType.Perspective:
					// TODO: these near/far values technically dont matter if we have multiple regions
					// because we wind up manually setting the view matrix and projection
					mCamera = new Camera((float)mNear, (float)mFar, (float)(mFovRadians * Utilities.MathHelper.RADIANS_TO_DEGREES), false, true, false);
					break;
				case Viewport.ProjectionType.Orthographic:
					if (mViewType == Viewport.ViewType.Free || mViewType == Viewport.ViewType.None)
						ViewType = Viewport.ViewType.Top;
					
					// Use orthographic constructor
					mCamera = new Camera(mZoom, (float)mNear, (float)mFar);
					break;

				default:
					// cameras are created from the scene
					mCamera = new Camera((float)mNear, (float)mFar, (float)(mFovRadians * Utilities.MathHelper.RADIANS_TO_DEGREES), false, true, false);
					break;
			}

			mCamera.Viewport = mViewport;
		}

		private void ViewProjectionChanged()
		{
			CreateDedicatedCamera();
			
			// March.6.2011 - the following rotations are all correct.
			switch (mViewType)
			{
				case Viewport.ViewType.Top: // top is looking down negative Y
					Viewpoint.SetRotation(90, 180, 0);
					break;
				case Viewport.ViewType.Bottom:
					Viewpoint.SetRotation(-90, 0, 0);
					break;
				case Viewport.ViewType.Right: // right is looking over positive X
					Viewpoint.SetRotation(0, -90, 0);
					break;
				case Viewport.ViewType.Left: // left is looking over negative X
					Viewpoint.SetRotation(0, 90, 0);
					break;
				case Viewport.ViewType.Front: // default starting tv camera
					Viewpoint.SetRotation(0, 0, 0);     // front is looking down positive z
					break;
				case Viewport.ViewType.Back: // back is looking down negative z
					Viewpoint.SetRotation(0, 180, 0);
					break;
				default:
					break;
			}

			if (mProjectionType != Viewport.ProjectionType.Perspective && mProjectionType != Viewport.ProjectionType.None)
				// pass in current position and zoom by default
				UpdateOrthographicView(Position, mCamera.Zoom);


			mLastViewType = mViewType;
			mLastProjectionType = mProjectionType;
			mNeedNewCamera = false;

            mShadowMappingEnabled = false; // temp HACK TO DISABLE while debugging isometric terrain structures
			if (mShadowMappingEnabled)
				// TODO: unintialize so we can disable on the fly?
				InitializePSSM();
		}

		// TODO: this needs to be moved to BehaviorTree for viewpoint
		// TODO: this needs to be called when
		// -zoom changes
		// - view changes (if orthographic)
		// - projective changes to orthographic
		// - camera distanceToOrthoPlane changes
		private void UpdateOrthographicView(Vector3d target, float zoomAmount)
		{
			Vector3d translation = Vector3d.Zero();

			// TODO: we simply need a way to get the height of things
			// in the frustum and to dynamically calculate the mDistanceToOrthoPlane
			// value

			// mDistanceToOrthoPlane assumes a fixed elevation above the plane we're orthogonal too
			// 
			// 
			// the mDistanceToOrthoPlane in reality just needs to be as big as half the bounding box dimension for that axis of the
			// biggest item in the world
			switch (mViewType)
			{
				case Viewport.ViewType.Top:
					// translate on x/z plane to put camera's x,z position at target's
					translation.x = target.x - this.Position.x;
					translation.y = mDistanceToOrthoPlane - this.Position.y;
					translation.z = target.z - this.Position.z;
					break;
				case Viewport.ViewType.Bottom:
					translation.x = target.x - this.Position.x;
					translation.y = -mDistanceToOrthoPlane - this.Position.y;
					translation.z = target.z - this.Position.z;
					break;
				case Viewport.ViewType.Right:
					// translate on x to be wider than the WIDTH property of the bounding box
					translation.x = mDistanceToOrthoPlane - this.Position.x; ;
					translation.y = target.y - this.Position.y;
					translation.z = target.z - this.Position.z;
					break;
				case Viewport.ViewType.Left:
					translation.x = -mDistanceToOrthoPlane - this.Position.x;
					translation.y = target.y - this.Position.y;
					translation.z = target.z - this.Position.z;
					break;
				case Viewport.ViewType.Front:
					// translate on z to be deeper than the DEPTH property of the bounding box
					translation.x = target.x - this.Position.x;
					translation.y = target.y - this.Position.y;
					translation.z = -mDistanceToOrthoPlane - this.Position.z;
					break;
				case Viewport.ViewType.Back:
					translation.x = target.x - this.Position.x;
					translation.y = target.y - this.Position.y;
					translation.z = mDistanceToOrthoPlane - this.Position.z;
					break;
			}

			Viewpoint.Translate(translation); // TODO: note how this translates viewpoint, not camera directly
			Zoom = zoomAmount;
		}
		#endregion
		
		public Vector3d GetRegionRelativeCameraPosition (string regionID)
		{
			if (mRegionSets != null && mRegionSets.Count > 0)
				for (int i = 0; i < mRegionSets.Count; i++)
					if (mRegionSets[i].ID == regionID)
						return mRegionSets[i].RegionRelativeCameraPos;

			return this.Position;
		}
		
		private object mPickLock = new object();
		/// <summary>
		/// Initiates a mouse pick into the scene.  Alternatively there is
		/// TVMaths.GetMousePickVectors (x, z, ref nearStart, ref farEnd)
		/// </summary>
		/// <param name="vp"></param>
		/// <param name="mouseX">Client mouse position X value.</param>
		/// <param name="mouseY">Client mouse position Y value</param>
		/// <param name="parameters"></param>
		public PickResults Pick(int mouseX, int mouseY, PickParameters parameters)
		{
			// NOTE: This Pick() gets called from
			// 1) CheckInput->Device.Notify()->InputControllerBase.HandleUpdate()->ViewpointController.SetAxisStateChange()->
			//	  Workspace3D.ToolMouseMove()->CurrentTool.HandleEvent()->currentTool.Pick()->RenderingContext.Pick()
			// 2) EditorWorkSpace.ToolBox.ToolboxItem_Dropped()
			// #2 means we need to sychronize access to the Picker until perhaps we can queue the Toolbox request
			// somehow OR make all Toolbox requests into EditTool instance spawners that then fullfill the request
			// as the CurrentTool and thus take place in #1.
			
			lock(mPickLock)
			{
				// project mouse coords to 3d and create a ray in the direction of the camera
				Vector3d rayDir;
				Vector3d nearPick;
				Vector3d farPick;
				
				// NOTE: camera.View contains 41 = 0, 42 = 0, 43 = 0 because of camera space rendering.
				//
				rayDir = mViewport.UnProject(mCamera.View, mCamera.Projection,
				                             mouseX, mouseY,
				                             out nearPick, out farPick);
				// inside of our picker.Pick() we  take into account a ray start that is not at camera position
				
				#if DEBUG
				// rounded reverseTest.x and reverseTest.y  shoudl match mouseX and mouseY values
				Vector3d reverseTest = mViewport.Project(nearPick, Camera.View, Camera.Projection, Matrix.Identity());
				int reverseMouseX = (int)System.Math.Round(reverseTest.x, 0);
				int reverseMouseY = (int)System.Math.Round(reverseTest.y, 0);
				//  System.Diagnostics.Debug.Assert(reverseMouseX == mouseX && reverseMouseY == mouseY);
				#endif
				Vector3d rayOrigin = nearPick;
				
				Ray regionSpaceRay = new Ray(rayOrigin + this.Position, rayDir);


                PickResults hudOverlayResult = new PickResults();

                if (this.Hud != null)
				{
					
					// NOTE: The appeal of seperate pick here is that we dont have to change selected entity
					//       or mouse over entity in the Workspace when GUI menus are selected.
					//       This is i think rather ideal.
					
					// update mouse coords in the parameters but DO NOT MODIFY other parameters here
					// that should already be set
					parameters.MouseY = mouseX;
					parameters.MouseY = mouseY;
					if ((parameters.ExcludedTypes & KeyCommon.Flags.EntityAttributes.HUD) == KeyCommon.Flags.EntityAttributes.None)
					{
						//NOTE: we disable 2d and 3d retained HUD entities in the scenegraph
						//      otherwise all other open and active viewports will cull them and render them
						//      if visible.  We dont want that.  HUD items must always be per Viewport.
						mHud.RootElement3D.Enable = true;
						mHud.RootElement2D.Enable = true;
						
                        // perform the PICK
						hudOverlayResult = this.Hud.Pick(this, regionSpaceRay, parameters); 
						
						mHud.RootElement3D.Enable = false;
						mHud.RootElement2D.Enable = false;

                        // NOTE: The hudOverlayResult.Entity will be the actual Entity the proxy is pointing to 
                        // but the hudOverlayResult.Model will be the Control2d itself
						if (hudOverlayResult != null && hudOverlayResult.HasCollided && IsVisible(hudOverlayResult.Model))
						{
							//System.Diagnostics.Debug.WriteLine("RenderingContext.Pick() - Hud Element Mouse Picked.");
							return hudOverlayResult;
						}
					}
				}
				
				
				// if still here, then HUD was not picked above, now try to pick main scene
				if (this.Scene.Root == null || this.Region == null) return PickResults.Empty(); 

                PickResults results;
                if (this.Scene.Root is ZoneRoot)
                {
                    results = mPicker.Pick(this.Viewport, this.Region, this.Scene.Root.RegionNode, regionSpaceRay, parameters);
                    //if (results.HasCollided && results.Entity is ModeledEntity && (hudOverlayResult.HasCollided && results.Entity is Celestial.Body == false))
                    //    return results;
                    //else return hudOverlayResult
                    return Picker.FindClosest(results, hudOverlayResult);
                }
                else
                {
                    // NOTE: below works for single regions, interiors and star nav map,
                    // but since it does not start at root, it doenst work for multi zoned worlds
                    // TODO: but ideally, id like to be able to always just start at root.  I think for that
                    // i need to make improvements to the EntityFlag layer mask culling to avoid HUD branch.
                    results = mPicker.Pick(this.Viewport, this.Region, this.Region.RegionNode, regionSpaceRay, parameters);
                    if (results.HasCollided && results.Entity is Region && Core._Core.ArcadeEnabled == false)
                        return results;
                    //else if (results.HasCollided && results.Entity is ModeledEntity && (hudOverlayResult.HasCollided && results.Entity is Celestial.Body == false))
                    //    return results;
                    //else return hudOverlayResult; // typically used for colliding with invisible pickplane which is a HUD element
                    else return Picker.FindClosest(results, hudOverlayResult);
                }
            } // end lock
		}



        private bool IsVisible(Elements.Model model)
        {

            if (model != null && model.Appearance != null && model.Appearance.Material != null && model.Appearance.Material.Opacity > 0.0f)
                return true; 
            
            return false;
        }

		/// <summary>
		/// ScaleDrawer calls this method to set in the engine the viewport
		/// specific settings stored in this context
		/// </summary>
		public void ApplyState()
		{
			// TODO: this i think is made obsolete by HUD options
			CoreClient._CoreClient.Engine.DisplayFPS(_showFPS);
			CoreClient._CoreClient.Engine.EnableProfiler(_showTVProfiler);
		}

		
		internal void GraphicsLoop (double elapsedSeconds)
		{
			#if DEBUG
			CoreClient._CoreClient.Profiler.StartLoop ();
			#endif

			// Cull() primarily occurs during this context.Update() call
			// Update() occurs before RenderBeforeClear() allowing us to do hardware occlusion
			// during Cull()
			Update(elapsedSeconds);

			
			// NOTE: we _do_ call context.Update() above even if null camera
			// because input control occurs, but obvously we do not draw below if no camera
			if (mCamera == null || mCamera.TVCamera == null) return;

            mScene.DisableAllLights();
			using (CoreClient._CoreClient.Profiler.HookUp("Render Before Clear"))
			{
				RenderBeforeClear();
			}
			
			using (CoreClient._CoreClient.Profiler.HookUp("Clear"))
			{
				Clear();
			}
			using (CoreClient._CoreClient.Profiler.HookUp("Render After Clear"))
			{
				Render();
			}
			
			
			#if DEBUG
			CoreClient._CoreClient.Profiler.EndLoop();
			#endif
		}
		
		/// <summary>
		/// Called by ClientSceneManager.Update() each RenderingContext will be
		/// sequentially culled and renderered BEFORE moving onto the next RenderingContext.
		/// Thus, we can make temporary modifications (overrides) to the Appearance of parts
		/// of the scene or the entire scene and as long as we roll back those overrides
		/// at completion of rendering of the current RenderingContext, we never have to
		/// worry about those temporary modifications affecting any other RenderingContext.
		/// </summary>
		/// <remarks>
		/// Update() occurs before RenderBeforeClear() allowing us to do hardware occlusion
		/// during Cull().
		/// </remarks>
		/// <param name="elapsedSeconds"></param>
		private void Update(double elapsedSeconds)
		{
			if (mNeedNewCamera)
				ViewProjectionChanged();
			
			mElapsedSeconds = elapsedSeconds;
            
			// NOTE: viewpoints are NOT updated in Simulation because their behavior trees are only
			//       active when that viewpoint is bound to a RenderingContext.
			// Viewpoint.Update ();
			
			// update the view controller.  It is here that the bound Viewpoint has it's .Update() called.
			mViewController.Update(elapsedSeconds);
			
			// NOTE: we update the view controller even if camera is null
			if (mCamera == null) return;

			// TODO: the following foreach should replace the below.. so that fxproviders are per context and not global?
			//foreach (IFXProvider fx in camera.Viewport.Context.FXProviders)
			//    if (fx != null) fx.Update((int)elapsed, camera);

			// update all the FX Managers running in the scene
			// NOTE: Update of FXInstanceRenderer results in the Instance count being reset
			// which must occur BEFORE Cull    
			// TODO: note how the FXProviders are in SceneManager, but shouldn't they be per scene?  Because there are some FXProviders
			// that have meshes registered to them which are only relevant for particular scenes.  ???
			foreach (FX.IFXProvider fx in CoreClient._CoreClient.SceneManager.FXProviders) // _fxProviders)
				if (fx != null) fx.Update(elapsedSeconds, this);

			mStats.Clear(true);
			ClearRegionPVSList();
			

			// update HUD Retained elements before Cull() since UpdateHUD may create new 3D Proxies as well as 2D
			// and those will get added to the RegionPVS as they are generated during Cull()
			if (mHud != null)
				this.mHud.UpdateBeforeCull(this, elapsedSeconds);
			
			//NOTE: we disable 2d and 3d retained HUD entities in the scenegraph
			//      otherwise all other open and active viewports will cull them and render them
			//      if visible.  We dont want that.  HUD items must always be per Viewport.
			if (mHud != null)
			{
				mHud.RootElement3D.Enable = true;
				mHud.RootElement2D.Enable = true;
			}
			// Cull and generate RegionPVS array
			// Recall that since this.Update() occurs before RenderBeforeClear(),
			// it allows us to do hardware occlusion during Cull().
			using (CoreClient._CoreClient.Profiler.HookUp("Update - Cull"))
				Cull();
			
			if (mHud != null)
			{
				mHud.RootElement3D.Enable = false;
				mHud.RootElement2D.Enable = false;
			}
			
			// update HUD Immediate elements after Cull() since RegionPVS will not be generated
			if (mHud != null)
				this.mHud.UpdateAfterCull(this, elapsedSeconds);
			
			// TODO: this could be threaded? But must be done seperately from Cull since
			// access to sychronized buckets is better when cull is threaded by itself.  Then
			// light assignments is threaded serially after cull is done.
			AssignLightsToPVSLists(mRegionSets, mVisibleLights);
		}

		
		private void Cull()
		{
			if (CullingStart != null)
				CullingStart(this);

			// culling occurs per viewport seperately automatically
			// different viewports can have a different effect on the spatialgraph
			// pager and each viewport Context should maintain seperately the list of pages it requires
			// ViewportManager/Context can track the camera and the camera should perhaps register its callback here so we can track movement properly
			// and determine the current region and such.
			if (Viewpoint == null || Viewpoint.Region == null) return;

			if (mShadowMappingEnabled)
				ParallelSplitShadowMapping.ClearSceneBoundingBox();

			// now if the _cameraRegion is an exterior Zone that is ultimately parented to
			// the scene's Root, but yet we are still NOT using "portals" to connect these zones, then
			// we have to traverse starting at the root node.  If however _cameraRegion is
			// an interior Region (child Region of Container), then we can traverse from it directly
			// TODO: also if this RenderingContext is interior mode, we should start _culler.Cull()
			// by passing in the sceneNode to the scenenode for the specified interior?
			KeyCommon.Traversal.CullParameters cullParameters = new KeyCommon.Traversal.CullParameters();
			cullParameters.AllowedAttributes = mAllowedAttributes; // | KeyCommon.Flags.EntityFlags.Background
            cullParameters.IncludedAttributes = mIncludedAttributes;
			cullParameters.HardwareOcclussionEnabled = mHardwareOcclussionEnabled;

            if (mScene.Simulation.GameTime == null) return;

			if (Viewpoint.Region.Parent is Keystone.Entities.Container)
			{
				// inside of a container, we start at the viewpoints region and then only traverse to
				// scene root if we hit a portal to the exterior of ship

				// TODO: bug -> the first pvs created by main cull is never actually used...
				RegionPVS pvs = mCuller.Cull(Viewpoint.Region.SceneNode, cullParameters);
			}
			else
			{
				mCuller.Cull(mScene.Root.SceneNode, cullParameters);
			}

			
			if (CullingComplete != null)
				CullingComplete(this);
		}


		#region Cull event handlers
		internal void OnRegionPVSCreated(RegionPVS regionPVS)
		{
			// thread safe so multiple cullers can add
			lock (regionSetListLock)
			{
				mRegionSets.Add(regionPVS);
			}
		}


		// =========================================================================
		// BEGIN - POTENTIALLY THE FOLLOWING THREE METHODS CAN BE MOVED TO RegionPVS?
	#region MOVETOREGIONPVS
		internal void OnVisibleLightFound(RegionPVS pvs, Lights.Light light, Vector3d cameraSpacePosition, IntersectResult intersection, BoundingBox cameraSpaceBoundingBox)
		{
			// this function is access by many threads as our culling is threaded
			LightInfo lightInfo = new LightInfo(light, cameraSpacePosition, intersection);
            light.Active = true;
			pvs.Add(lightInfo);
			mVisibleLights.Add(lightInfo);
           // System.Diagnostics.Debug.WriteLine("OnVisibleLightFound() - count == " + mVisibleLights.Count);
			if (mShadowMappingEnabled)
			{
				if (light is Keystone.Lights.DirectionalLight && light.Enable == true)
				{
					// if the previous shadowmapper instance is using this light, we can re-use it
					// otherwise if not, or if null, we create a new one
					
					// NOTE: a seperate shadowmapper instance is used for each viewport instance and
					// our RS depthmap size will also need to vary based on the viewport size.  This way
					// even with multiple viewports, we can keep memory use down since not each viewport can take up
					// all or most of the screen
					ParallelSplitShadowMapping.LightDirection = light.Direction;
				}
			}
			
			// if HUD is available, create and add a mouse pickable Light icon
			if (mHud != null)
			{
				// having Iconize in a specific HUD would allow us to respond differently
				// in different Hud implementations (eg Nav Hud vs Edit Hud vs etc).
				// Another way to think of the HUD though is a Plugable strategy pattern
				// that is not directly connected to the "Layer" Group node which will contain
				// our HUD models.

				double scale;
				if (this.ProjectionType == Viewport.ProjectionType.Orthographic)
				{
					//scale = -GetConstantScreenSpaceScale2(cameraSpacePosition, entity.ScreenSpaceSize);
					scale = GetConstantScreenSpaceScaleOrthographic(2);
				}
				else
				{
					// Settings.LightHUDIconScreenSpaceSize = 0.2
					scale = GetConstantScreenSpaceScalePerspective(cameraSpacePosition, 0.05f);
				}

				// TODO: The idea behind Iconize was that these hud icons would be added per frame
				//       and NOT persisted. This is why they are added directly to pvs.Add()
				//       But that makes it more complicated for tracking these icons for Picking,
				//       And I think the idea here is to have the HUD cache these temporary iconified
				//       proxy's to use for that picking.
				VisibleItem? result = mHud.Iconize(lightInfo, scale);  // hrm...
				if (result != null)
					pvs.Add((VisibleItem)result);
			}
		}

		
		/// <summary>
		/// After culling has found all visible lights and all visible models,
		/// we iterate through all buckets of all RegionPVSs' found models and assign the lightInfo's
		/// </summary>
		/// <param name="regionPVSList"></param>
		/// <param name="lightList"></param>
		/// <remarks>NOTE: we only test items against lights and assign them AFTER all culling is done and all lights and geometry found.</remarks>
		private void AssignLightsToPVSLists(List<RegionPVS> regionPVSList, List<LightInfo> lightList)
		{
			using (CoreClient._CoreClient.Profiler.HookUp("Assign Lights"))
			{
				if (lightList == null || lightList.Count == 0) return;
				
				
				for (int i = 0; i < regionPVSList.Count; i++)
				{
					foreach (keymath.DataStructures.SingleLinkedList<VisibleItem> bucketItems in regionPVSList[i].mBuckets.Values)
					{
						if (bucketItems == null || bucketItems.Count ==  0) continue;
						
						// iterate every visible light
						for (int k = 0; k < lightList.Count; k++)
							// and test against every item in every bucket
							AssignLightsToBucketItems(bucketItems, lightList[k]);
					}
				}
			}
		}
		

		/// <summary>
		/// test if passed in Light affects any of the Visible Entites in the current bucket
		/// </summary>
		/// <param name="bucketItems">A SingleLinkedList<T> of VisibleItem.cs</param>
		/// <param name="lightInfo"></param>
		/// <remarks>NOTE: we only test items against lights and assign them AFTER all culling is done and all lights and geometry found.</remarks>
		private void AssignLightsToBucketItems(keymath.DataStructures.SingleLinkedList<VisibleItem> bucketItems, LightInfo lightInfo)
		{
            if (lightInfo == null) return; // throw new ArgumentNullException();
			for (int i = 0; i < bucketItems.Count; i++)
			{
                //if (bucketItems[i].Entity is Vehicles.Vehicle)
                //    System.Diagnostics.Debug.WriteLine("assigning lights to hull");

                // TODO: I shouldn't be doing a distance comparison but rather a boundging box intersect test
                if (!lightInfo.Light.SceneNode.BoundingBox.Intersects(bucketItems[i].Entity.SceneNode.BoundingBox)) continue;
                // get distance between light and this item
                double distanceSquared = Vector3d.GetDistance3dSquared(lightInfo.mCameraSpacePosition,
                                                                       bucketItems[i].CameraSpacePosition);
                // if (distanceSquared > lightInfo.Light.RangeSquared) continue;

                // TODO; is there any reason I dont just make the regular LightInfo a sortable IComparable on it's own so i can ditch SortableLightInfo wrapper?
                SortableLightInfo sortableInfo;
				sortableInfo.LightInfo = lightInfo;
                sortableInfo.DistanceToItemSquared = distanceSquared;

				// attempt to add this item to the VisibleItem using this distance info
				// adding may result in an existing light being removed if the max light limit is reached
				bucketItems[i].AddLight(sortableInfo);
			}
		}
		// END - POTENTIALLY MOVE THE ABOVE 3 METHODS TO REGIONPVS?
		// =========================================================================
	#endregion


		internal void OnVisibleItemFound(RegionPVS pvs, Entities.Entity entity, Elements.Model model, Vector3d cameraSpacePosition, BoundingBox cameraSpaceBoundingBox)
		{
			// TODO: what if it's too far to be rendered yet will be visible as icon?
			// how do we handle this?  i mean we dont want culler to eliminate it and never
			// call this as visibleitemfound potentially.
			// what are our options?
			//  - we can make the entity qualify as something that should still be sent to large culler
			//  - we can use a seperate "sensor" pass run by the simulation and then somehow we insert
			//    as visibleItem
			//      - id like to try this latter method first because this way the data is not required
			//        to be sent by network to clients if it is not detected.  So ideally if it's
			//        beyond visible range, we must rely on sensors determining if user can see it
			//        and this data must come as a detection status update that the sensors simulation
			//        can manage when managing the list of contacts.
			//        Vehicle.Detection.Contacts;
			VisibleItem vi;
			using (CoreClient._CoreClient.Profiler.HookUp("VisibleItem Creation"))
				vi = new VisibleItem(entity, model, cameraSpacePosition);

            //if (entity.ID.Contains("footprint"))
            //   System.Diagnostics.Debug.WriteLine("footprint");

			// TODO: here we can add icons to the overlay scene
			//       perhaps we can determine which ones are no longer visible
			//       and which are?  perhaps make them stale and remove them
			//       after x interval?

			// we can also query for the direction of an entity and then draw a hud icon with a rotatoin
			// in the 2d projected dir of travel.

			//  is there a way we can determine if this item should have a hud icon?
			//  mHud.Iconize(entity, model, position);
			// for example, when rendeing interior, we dont want to draw an icon
			// for everything there.. maybe for some though like electricity consuming devices
			// and maybe some "icons" are treated like a label that can have multiple images
			// blitted into them and rendered as one overall icon label.  One way of course
			// is to check some custom flags in domainobject.. showing what types of icons are
			// potentially available/can be shown and then the implementation of the icon drawing
			// is left to the hud implementation that can be defined by workspace and added to
			// the relevant renderingcontexts

			// eg. we find a stellarsystem, mHud which is custom will know how to deal with it
			// but the point is, id like a fast way to know before having to call
			// mHud.Iconize (entity...)  we could maybe use a regular entity.flag instead which
			// is faster.  This way most interior objects can have this disabled... like walls
			// floors and such.
			// what other possibilities are there for determining when something needs to have
			// an icon drawn?  we could do giant switch in .Iconize() and test the type
			// componenttype too...
			// Also actually for mHud. nav workstation HUD will definetly skip interior stuff
			// because the interior isn't even being rendered at all.  the traverser doesnt even
			// enter the ship.. (well technically maybe not true, but perhaps testing the type of
			// pvs it's added to can assist there.  If the pvs.Region is an interior region we know
			// to skip.  I think in the short term we can do that.
			//if (entity.GetFlagValue(KeyCommon.Flags.EntityFlags.Iconizeable))

			if (mHud != null)
			{
				double scale;
				using (CoreClient._CoreClient.Profiler.HookUp("SSScaleCalc"))
				{
					if (this.ProjectionType == Viewport.ProjectionType.Orthographic)
					{
						//scale = -GetConstantScreenSpaceScale2(cameraSpacePosition, entity.ScreenSpaceSize);
						scale = GetConstantScreenSpaceScaleOrthographic(2);
					}
					else
					{
						// Settings.LightHUDIconScreenSpaceSize = 0.2
						scale = GetConstantScreenSpaceScalePerspective(cameraSpacePosition, 0.025f);
					}
				}

				using (CoreClient._CoreClient.Profiler.HookUp("Iconize"))
				{
                    // NOTE: Iconize() always results in Immediate mode rendering
                    //       since we add new VisibleItem directly to pvs not to HUDRoot.
                    // TODO: Isn't this a problem though when we want to mouse click those icons?
                    // For now let's get the Vehicle icon rendering and then we'll see about
                    // changing it to use Retained mode.

                    // todo: use the DefaultEntity.UserTypeID to determine which texture to use for this icon
                    //       Recall that UserTypeID is purely application specific and keystone.dll and keyCommon.dll do not care about that property
                    // todo: when creating lights, i should similar assign the UserTypeID so i know if its a pointlight, dir light or spotlight
                    //       and from there i know which texture to use
                    if (!(vi.Entity is IEntityProxy))
                    {

                        // todo: wire up the mouse click events
                        //
                        //       - perhaps we can get away with generic event handlers for iconized Entities as opposed to
                        //         dedicated GUI elements.  
                        //       - same would have to be true for worlds and vehicles that are beyond visual range.
                        //       - all we really need is the entity.ID and perhaps the UserTypeID.  Well, from the proxy.ReferencedEntityID
                        //         we can find everything else.
                        //          - add the UserTypeIDs in the user_constants file
                        //          - assign the UserTypeIDs in the toolbox creation methods for Lights, SpawnPoint and TriggerVolume
                        //       - do we need to know which RenderingContext was picked? if not, we should probably just wire
                        //         to handlers in this.Workspace
                        //       - keep in mind that different Huds may have different Workspaces such as Editor vs Floorplan.  This means
                        //       we'd need to wire seperate events... actually, we'd also need seperate proxies for each viewport?? well
                        //       i think we just need to translate the proxies prior to rendering each viewport's Hud.  This already occurs
                        //       properly, howewver, it still doesn't wire up the events for each hud or workspace.
                        //
                        //       - add gui options for adjusting the icon sizes
                        //       - add gui options for showing/hiding icons
                        //       - how do we remove proxy icons when we no longer want to see them such as when we move
                        //         to a different zone?

                        // CANCEL EVGO!!!!!!!!!!!!!!!!!!!!

                        //
                        //
                        //this.Workspace.

                        // todo: add iconization for Lights too. user Entity.UserTypeID so we can determine which texture to use
                        // todo: create .png for SP (spawnpoint)

                        // TEMP: for our DefaultEntity (spawnpoint) proxy, the proxy is added retained mode
                        //       which means it gets culled and then this method gets called which adds it to the pvs
                        //       So we just return right away and in subsequent frames the proxy gets added to the pvs
                        //       NOTE: the mHud.Iconize() therefore should only be used for non pickable immediate mode rendering
                        // TODO: Im not even sure what circumstances i would NOT want to be able to mouse pick an Icon.  
                        VisibleItem? result = mHud.Iconize(vi, scale);  // hrm...

                        // if we've created an iconized version of this visibleItem, add that proxy to the pvs 
                        // and return before adding the original visibleItem to the pvs
                        if (result != null)
                        {
                            if (result.Value.Model != null)
                            {
                                pvs.Add((VisibleItem)result);
                                mHud.GenerateTextLabel((VisibleItem)result);
                                return;
                            }
                            else return;
                        }
                    }
				}
			}


			// TODO: add each visible shadow receiving geometry to shadowmapping bounds before we call ParallelSplitShadowMapping.Update()
			if (mShadowMappingEnabled && vi.Model.ReceiveShadow && vi.DistanceToCameraSq < MAX_SHADOW_DRAW_DISTANCE_SQUARED)
			{
				using (CoreClient._CoreClient.Profiler.HookUp("ShadowMap.AddBBox"))
					ParallelSplitShadowMapping.AddBoundingBoxToSceneBoundingBox(cameraSpaceBoundingBox);
			}
			
			// still here?  that means we didn't iconize, just add and render normally
			// well... optionally we don't render here.  It does depend on
			// which Workspace we are in and what settings of this context...
			pvs.Add(vi);
			
            if (mHud != null)
			    mHud.GenerateTextLabel (vi);
		}
		#endregion

		
		private void RenderBeforeClear()
		{
			foreach (FX.IFXProvider fx in CoreClient._CoreClient.SceneManager.FXProviders)
				if (fx != null) fx.RenderBeforeClear(this);
			
			if (mShadowMappingEnabled == true && mVisibleLights != null && mVisibleLights.Count > 0)
			{
				// TODO: if no visible lights found here, then any previous light (eg. sunlight is suddenly eclipsed)
				//       the depth texture will not get updated.  So what we should do is update until there is
				//       no change between frames in both lights, light directions and view matrix.
				// TODO: if no lights this frame, and lightcount has changed, then set light direction 0,0,0 and have
				//       depthmap be cleared and no render
				using (CoreClient._CoreClient.Profiler.HookUp("ShadowMap.RenderBeforeClear"))
					ParallelSplitShadowMapping.RenderBeforeClear(mElapsedSeconds);
			}

			if (mDeferredEnabled == true)
			{
				// TODO: ok to re-use camera here since its multiple targets of backbuffer?
				mMultipleRenderTarget.SetNewCamera(mCamera.TVCamera);
				mMultipleRenderTarget.StartRender(false);
				// All geometry gets rendered before Clear in Deferred
				RenderScene();
				mMultipleRenderTarget.EndRender();
			}
			
			// TODO: i forget, when is camera and viewport set into engine needed?  we dont want this interfering with RS cameras
			CoreClient._CoreClient.Engine.SetViewport(mCamera.Viewport.TVViewport);
			// note: setting the viewport alone does NOT also set the camera even if that viewport has it's own camera.
			// we must explicitly set the tv camera to tvengine as well
			CoreClient._CoreClient.Engine.SetCamera(mCamera.TVCamera);

           
		}

		private void Render()
		{
			if (RenderingStart != null) // this event is typically wired up by the Workspace
				RenderingStart(this);

			// RenderDebug() just adds the debug info to the RegionPVS and allows for it to be rendered by RenderScene() call.
			// This is potentially problematic in that our debug text we only want rendered one time on final scene.  We dont want it rendered
			// by shaders passes such as Shadows and Deferred lighting.
			RenderDebug();

			
			// Engine.ClearDepthBuffer must be called between
			// Engine.Clear() and Engine.RenderToScreen().  Keep this in mind
			// when using callbacks with various FX classes.
			this.ClearDepthBuffer();
			
			if (mDeferredEnabled == false)
			{
				RenderScene();
			}
			else if (mDeferredEnabled == true)
			{
				mPointLightSphereMesh.Minimesh.Render();
			}
			
			// Render post FX such as Bloom
			// NOTE: post process shaders like Bloom work fine with forward and deferred with no special handling for it to work with deferred
			foreach (FX.IFXProvider fx in CoreClient._CoreClient.SceneManager.FXProviders)
			{
				// NOTE: because Bloom uses the 2d fullscreen quad draw command it must be the very last
				// 2d operation or it will get wiped out.  Water is also rendered here because the actual water meshes are only rendered once
				if (fx != null)
					fx.RenderPost(this);
			}

			// interestingly, the way AccurateTimeElapsed works is, it takes the time between frameRenderN - frameRenderN-1
			// this means the following debug line = 0 first time because there is no frameRenderN or frameRenderN-1,
			// however the second time around, the value is filled.  This must be because when the engine is initialized
			// the frameRenderN = Now
			// Anyways, this isnt more accurate than me calling Environement.GetTickCOunt() just once at the same place everytime.
			// The difference is, TV's version is immune from changing every subsequent call between frames.
			// 
			// the AccurateTimeElasped is updated only at each RenderToScreen/RenderTootherHwnd.
			// It takes the time between the last RenderToScreen and the current one.
			//
			//
			//Trace.WriteLine(string.Format("Accurate time elapsed == {0}", Engine.Core.Engine.AccurateTimeElapsed()));
			CoreClient._CoreClient.Engine.RenderToScreen();

			if (mScreenShotScheduled)
			{
				CoreClient._CoreClient.Engine.Screenshot(mScreenShotPath, CONST_TV_IMAGEFORMAT.TV_IMAGE_PNG);
				System.Diagnostics.Debug.WriteLine ("RenderingContext.Render() - Scheduled Screenshot saved.");
				if (mScreenShotSavedHandler != null)
					mScreenShotSavedHandler(mScreenShotPath, null);

				mScreenShotScheduled = false;
				mScreenShotPath = null;
				mScreenShotSavedHandler = null;
			}

			if (RenderingComplete != null)
				RenderingComplete(this);
		}


		// RenderScene() is the method for drawing the visible items.
		// It's also the method that is invoked directly by FX's such as reflecting Water
		// to render the reflection.  As such, we have to test
		private void RenderScene()
		{
			// TODO: i might need to provide within some of thse FXProviders
			// a method whereby they are passed the RegionCullingInfo struct either
			// with each Entity they are rendering (if applicable) or generally for the whole thing.
			// FXInstanceRenderer for instance should have seperate RegionCullingInfo for each minimesh
			// to be rendered.

			// skies and starboxes and starfields, long distance nebulas, should be rendered first
			foreach (FX.IFXProvider fx in CoreClient._CoreClient.SceneManager.FXProviders)
				if (fx != null && fx.Layout == FXLayout.Background)
					fx.Render(this);

			// insert HUD elements into applicable visible regionPVS's
			if (mHud != null && mRegionSets != null && mRegionSets.Count > 0)
				// TODO: If Root has a SelectorNode which can operate as Sequence or Switch
				//       we can easily enable/disable hud & scene
				// TODO: using Layers, this should be irrelevant as they'll have been rendered
				// in main _drawer.Render() based on the cullmask
				mHud.Render(this, mRegionSets);

            // TEST HACK - CoreClient._CoreClient.Scene.SetShadeMode(CONST_TV_SHADEMODE.TV_SHADEMODE_PHONG);
            using (CoreClient._CoreClient.Profiler.HookUp("RenderingContext.RenderScene"))
                ((ScaleDrawer)mDrawer).Render(this, mRegionSets, mVisibleLights, mElapsedSeconds);

			// draw those scene elements that should be drawn after the rest of the scene has been drawn.
			foreach (FX.IFXProvider fx in CoreClient._CoreClient.SceneManager.FXProviders)
			{
				// Some FX's "Render()" method (e.g Water) must only be rendered in the final scene render.
				if (fx != null && fx.Layout == FXLayout.Foreground)
					fx.Render(this);
			}
		}
		
		private void RenderEntityHandler (Entity entity)
		{
			// we want the mRegionPVS for just this visible entity... hrm...
			// can we test flags on the entity and cache it when adding to ReginPVS in the first place?
			// 
			((ScaleDrawer)mDrawer).Render(this, mRegionSets, mVisibleLights, mElapsedSeconds);
		}
		
		// TODO: render's before clear since the mesh renders here are drawing to a RenderSurface
		private void RenderGeometryIntoDepthMap(TV_3DMATRIX view, TV_3DMATRIX proj )
		{

			// TODO: for each split, the meshes visible is different, but for  now we dont care if we render
			//       out of frustum items... we just need to test basic functionality
			
			// TODO: don't our light-centric cameras also need to be computed in camera space?  or is that automatic for us?
			
			// cache
			//        	Matrix prevInverseView = mRegionSets[0].InverseView;
			//        	Matrix prevProjection = mRegionSets[0].FrustumInfo[0].Projection;
			
			// no Text, no Lines, no billboards(?), no particles, no backgrounds, no far frustums, just standard meshes
			// and the depth shader needs to be set
			// TODO: perhaps a  new function that will only get meshes from certain buckets

			//       	mRegionSets[0].InverseView = Helpers.TVTypeConverter.FromTVMatrix(view);
			//       	mRegionSets[0].FrustumInfo[0].Projection = Helpers.TVTypeConverter.FromTVMatrix(proj);
			
			// TODO: see notes in ScaleDrawer about how we may need seperate sets of split matrices for each RegionPVS
			//       for our depth pass to properly handle all Zones/Regions
			// TODO: all visible items in initial cull, i can flag as visible
			//       then at the end i can de-flag them all... i just need to create
			//       seperate visibleitem lists so that when im done with all, ill know
			//       which ones to unflag... but this will allow me to avoid re-bounds testing
			//       visible items from any of the previous passes
			//       -but when culling from lights perspective, i do need to pass in
			//       the matrices created here...
			((ScaleDrawer)mDrawer).RenderGeometryIntoDepthMap(this.Camera, mRegionSets);
			
			// restore matrices
			//            mRegionSets[0].InverseView = prevInverseView;
			//        	mRegionSets[0].FrustumInfo[0].Projection = prevProjection;
		}
		
		public void ClearDepthBuffer()
		{
			if (mDeferredEnabled == true)
				//TODO: When rendering the far frustum worlds/stars, i dont think i need to clear
				// that depth buffer at all.  Since it will still have the pixel shaders run so long
				// as the primary depth buffer IS cleared as we do here.  So really, all i need for
				// worlds (the ones without an atmosphere because of alpha transparency!)
				// is a proceduralshader added to the creations for those that will still include
				// How might i combine the two though in case i have to do worlds in forwad?
				mMultipleRenderTarget.SetDepthBufferClear(true);
			else
				CoreClient._CoreClient.Engine.ClearDepthBuffer();
		}

		private void Clear()
		{
			CoreClient._CoreClient.Engine.Clear(false);
		}
		
		private void ClearRegionPVSList()
		{
			//_visibleLights.Clear();
			lock (regionSetListLock)
			{
				mRegionSets.Clear();
				mVisibleLights.Clear();
			}
		}
		
		#region PSSM shadow mapping
		private void InitializePSSM()
		{
			if (ParallelSplitShadowMapping != null) return;
			
			int shadowMapSize = 2048; // 512, 1024 2048
			float renderInterval = 0.0125f;
			
			bool shadowmappingEnabled = Core._Core.Settings.settingReadBool ("graphics", "shadowmap");
			string shadowMapDefines= "";
			if (shadowmappingEnabled)
			{
				int numSplits = Core._Core.Settings.settingReadInteger ("graphics", "shadowsplits");
				
				// Create the depth map with a white background and ARGB16F
				// You may want to change that to a different format
				// CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_HDR_FLOAT32);
				ParallelSplitShadowMapping = new Keystone.PSSM.CParallelSplitShadowMapping(
					this,
					numSplits,
					shadowMapSize,
					CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_HDR_FLOAT16,
					renderInterval,
					RenderGeometryIntoDepthMap);
				
				
				// Limit the far plane to 1000.0f in the PSSM frustum
				// calculation. If your real scene camera far plane
				// is way to large, you can limit the scene shadowing
				// to that range
				ParallelSplitShadowMapping.FarPlane = 1000.0f;
				
				// Adds some extra distance between the lights camera position
				// and the center of each frustum split. This is neccessary to
				// let the depth map include shadow casters that are not visible
				// from the scenes camera. A better, but more expensive way is to
				// include all the potential casters in a frustum every frame, but
				// I found this to complicated for this small sample.
				// TODO: having this a property allows us to tweak at runtime to experiment
				ParallelSplitShadowMapping.ExtraDistance = 10;
				
				// the shared shaders used need to be added to the PSSM manager
				// actor
				shadowMapDefines =  "SHADOW_MAPPING_ENABLED";
				
				switch (numSplits)
				{
					case 4:
						shadowMapDefines += ",NUM_SPLITS_4";
						break;
					case 3:
						shadowMapDefines += ",NUM_SPLITS_3";
						break;
					case 2:
						shadowMapDefines += ",NUM_SPLITS_2";
						break;
					case 1:
						shadowMapDefines += ",NUM_SPLITS_1";
						break;
					default:
						shadowMapDefines += ",NUM_SPLITS_4";
						break;
				}
			}
			else
			{
				shadowMapDefines = "";
			}
			
			// TODO: is there a way to add these shaders to the shadow mapper on the fly?  THe main problem is it's
			// per Rendering context and is not global... though there is a way to get viewport.Context and iterate through all of them
			// and from there, we should restrict our editor to just 1 viewport.  THe goal of the multi-viewport really is relevant to 3d modelers
			// and not so much for games or even world builders outside of 3d wireframe style builders
			string shaderID = @"caesar\shaders\PSSM\pssm.fx@GEOMETRY_ACTOR," + shadowMapDefines + ",FORWARD";
			Shaders.Shader shader = (Shaders.Shader)Repository.Create (shaderID, "Shader");
			ParallelSplitShadowMapping.AddShader (shader);
			
			// mesh and tvlandscape
			shaderID =  @"caesar\shaders\PSSM\pssm.fx@GEOMETRY_MESH," + shadowMapDefines + ",FORWARD";
			shader = (Shaders.Shader)Repository.Create (shaderID, "Shader");
			ParallelSplitShadowMapping.AddShader (shader);
			
			// minimesh
			shaderID =  @"caesar\shaders\PSSM\pssm.fx@GEOMETRY_MINIMESH," + shadowMapDefines + ",FORWARD";
			shader = (Shaders.Shader)Repository.Create (shaderID, "Shader");
			ParallelSplitShadowMapping.AddShader (shader);
			
			// quad billboards
			shaderID = @"caesar\shaders\PSSM\pssm.fx@BILLBOARD_INSTANCING," + shadowMapDefines + ",FORWARD";
			shader = (Shaders.Shader)Repository.Create (shaderID, "Shader");
			ParallelSplitShadowMapping.AddShader (shader);
			
			// instanced geometry (only 1 vertices group supported)
			shaderID = @"caesar\shaders\PSSM\pssm.fx@GEOMETRY_INSTANCING," + shadowMapDefines + ",FORWARD";
			shader = (Shaders.Shader)Repository.Create (shaderID, "Shader");
			ParallelSplitShadowMapping.AddShader (shader);
			
			shaderID = @"caesar\shaders\PSSM\pssm.fx@TERRAIN_ATLAS_TEXTURING,GEOMETRY_INSTANCING," + shadowMapDefines + ",FORWARD";
			shader = (Shaders.Shader)Repository.Create (shaderID, "Shader");
			ParallelSplitShadowMapping.AddShader (shader);
			
			
			// TODO: having to assign shader paths manually to pssm is problematic
			//       - ideally, the shader gets assigned based on the Model's settings and global settings
			//       eg. deferred, shadowmapping, cast/recv shadows,
			//       however, there are cloud shaders, atmosphere shaders where they do need different types of shaders by default
			//       - should those shaders too be selected based on options on the Model?
			//         - what if we by default set the default shader when importing assets where Appearance.ResourcePath== null
			//         but allow that to be overridden
			//		- i think fundamnetally, we need to be able to assign shaders, the only question is to
			//        the assignment of a default shader and then being able to replace that default shader in editor with another
			//        and have it load and get assigned just like any other "appearance" change.
			//      - TODO: the shaders used here by PSSM, should be cached elsewhere and allow ParallelSplitShadowMapping
			//        references to the ones it needs.  Where should shaders be loaded then?
			//        - by the EXE?   If i automatically assign the default during Import, then i can't change it EXE side so
			//        [default_internal] should be assigned on Import and then it can be changed to null (to use tvdefault) or
			//        assigned from a drop down list of named shaders, or assigned via a path
			//      -TODO: but for now, lets just focus on the terrain paging
			//			- the simplist approach is to just have the default shader used if "default shader" is checked, ignoring
			//            any shader assigned?  No.  Simplist is to have a "default" shader used when no shader is assigned at all.
			//            - and then to rely on the checkboxes for setting variious DEFINES in that shader.  It would be nice if we could
			//             get the defines listed as checkboxes and combos in the plugin sidebar instead of the hardcoded options
			// 
			//            Shaders static cache? or Repository.Shaders[]
			//       - workspace could load default shaders?
			//			- if shaders are assigned to Models/appearances, then they shouldn't be specific to any rendering context or
			//            workspace.  The only exception is that each workspace and context can maybe assign certain global values to
			//            the shaders in the cache.
		}
		#endregion

		#region private methods Deferred Rendering
		internal void ViewportResized()
		{
			if (mDeferredEnabled == true)
			{
				UpdateDeferredRenderTargets();
			}
		}

		private void InitializeDeferred()
		{
			if (mPointLightSphereMesh == null)
			{
				mPointLightSphereMesh = Keystone.Elements.Mesh3d.CreateSphere(1.0f, 10, 10, CONST_TV_MESHFORMAT.TV_MESHFORMAT_NOTEX, false);
				mPointLightSphereMesh.CullMode = (int)CONST_TV_CULLING.TV_FRONT_CULL; // for pointlight minimeshes use front cull
				// our sphere may be grabbed from Repository on CreateSphere if shared between
				// multiple viewports
				Resource.Repository.IncrementRef(mPointLightSphereMesh);
			}

			if (mPointLightSphereMesh.Minimesh == null)
			{
				CoreClient._CoreClient.InstanceRenderer.CreateMinimesh(mPointLightSphereMesh, null, 52);
				mPointLightSphereMesh.Minimesh.UseColorMode = true; // required!
				mPointLightSphereMesh.Minimesh.CullMode = (int)CONST_TV_CULLING.TV_FRONT_CULL;
			}

			if (mPointLightShader == null)
			{
				string path = @"caesar\shaders\deferred_pointlight.fx";
				mPointLightShader = (Shaders.Shader)Resource.Repository.Create(path, "Shader");
				Keystone.IO.PagerBase.LoadTVResource (mPointLightShader, false);

				Resource.Repository.IncrementRef(mPointLightShader);

				mPointLightSphereMesh.Minimesh.Shader = mPointLightShader;
			}
		}

		private void UpdateDeferredRenderTargets()
		{
			int width = this.Viewport.Width;
			int height = this.Viewport.Height;
			// TODO: We must re-create the mMultipleRenderTarget whenever the viewport
			// width or height changes
			mMultipleRenderTarget = CoreClient._CoreClient.Scene.CreateMultiRenderSurface(
				4, width, height, true, true, 1,
				CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_HDR_FLOAT16,
				CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_HDR_FLOAT16,
				CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_HDR_FLOAT16,
				CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_HDR_FLOAT16,
				"multiple_render_target");

			// Send in the inverse viewport dimension.
			mPointLightShader.TVShader.SetEffectParamFloat("viewport_width", 1 / width);
			mPointLightShader.TVShader.SetEffectParamFloat("viewport_height", 1 / height);

			// We want to send our MRT textures to the lighting shader. Again, this is easy.
			mPointLightShader.TVShader.SetEffectParamTexture("color_tex", mMultipleRenderTarget.GetTextureEx(0));
			mPointLightShader.TVShader.SetEffectParamTexture("normals_tex", mMultipleRenderTarget.GetTextureEx(1));
			mPointLightShader.TVShader.SetEffectParamTexture("position", mMultipleRenderTarget.GetTextureEx(2));
		}

		private void UninitializeDeferred()
		{
			if (mMultipleRenderTarget != null)
				mMultipleRenderTarget.Destroy();

			if (mPointLightShader != null)
				// TODO: all calls to DecrementRef should go through Factory.Destroy()
				// or perhaps, .IncrementRef too should sychronize the node as a key
				// so instead of thinking of Factory... how about we move all that to  then at least i can share
				// the keys
				Resource.Repository.DecrementRef(mPointLightShader);

			if (mPointLightSphereMesh != null)
				Resource.Repository.DecrementRef(mPointLightSphereMesh);
		}
		#endregion

		// called by VisualFXAPI
		public void DrawQuad(float left, float top, float right, float bottom, int color1)
		{
			Keystone.Immediate_2D.IRenderable2DItem vi = new Keystone.Immediate_2D.Renderable2DQuad(left, top, right, bottom, color1);

			// TODO: need an overload so a 2d textured quad can have alpha and so be sorted
			// and some should be as part of scene, others are overlay like hud items
			mRegionSets[0].Add(vi, false);
		}
		
		public void DrawTexturedQuad(string textureID, float x, float y, float width, float height, float angleRadians, bool alphaBlend)
		{
			Keystone.Immediate_2D.IRenderable2DItem vi = new Keystone.Immediate_2D.Renderable2DTexturedQuad(textureID, x, y, width, height, angleRadians, alphaBlend);

			// TODO: need an overload so a 2d textured quad can have alpha and so be sorted
			// and some should be as part of scene, others are overlay like hud items
			mRegionSets[0].Add(vi, false);
		}

		public void DrawTexturedQuad(string textureID, float x, float y, float width, float height, float angleRadians, bool alphaBlend, int color)
		{
			Keystone.Immediate_2D.IRenderable2DItem vi = new Keystone.Immediate_2D.Renderable2DTexturedQuad(textureID, x, y, width, height, angleRadians, alphaBlend, color);

			// TODO: need an overload so a 2d textured quad can have alpha and so be sorted
			// and some should be as part of scene, others are overlay like hud items
			mRegionSets[0].Add(vi, false);
		}
		
		public void DrawTexturedQuad(string textureID, float x, float y, float width, float height, float angleRadians, bool alphaBlend, int color, int color2)
		{
			Keystone.Immediate_2D.IRenderable2DItem vi = new Keystone.Immediate_2D.Renderable2DTexturedQuad(textureID, x, y, width, height, angleRadians, alphaBlend, color, color2 );

			// TODO: need an overload so a 2d textured quad can have alpha and so be sorted
			// and some should be as part of scene, others are overlay like hud items
			mRegionSets[0].Add(vi, false);
		}
		
		//public void DrawTexturedQuad(string textureID, float angle, float left, float top, float right, float bottom, bool alphaBlend)
		//{
		//     throw new NotImplementedException();
		//Keystone.Immediate_2D.IRenderable2DItem vi = new Keystone.Immediate_2D.Renderable2DTexturedQuad(textureID, x, y, width, height, alphaBlend);

		// TODO: need an overload so a 2d textured quad can have alpha and so be sorted
		// and some should be as part of scene, others are overlay like hud items
		//this.VisibleRegionSets[0].Add(vi, false, false);

		//int texID = 0;
		//float width = right - left;
		//float height = bottom - top;
		
		//CoreClient._CoreClient.Screen2D.Draw_TextureRotated(texID, left, top, width, height, angle, color);
		//}

		/// <summary>
		/// Viewpoint will chase a focus entity such as a ship
		/// </summary>
		/// <param name="target"></param>
		/// <param name="screenSpace"></param>
		public void Viewpoint_Chase(Entities.Entity destinationTarget, float screenSpace)
		{
		}
		
		/// <summary>
		/// Viewpoint will orbit the focus entity
		/// </summary>
		/// <param name="target"></param>
		/// <param name="screenSpace"></param>
		public void Viewpoint_Orbit(Entities.Entity destinationTarget, float screenSpace)
		{
		}
		
		/// <summary>
		/// Viewpoint will move so that it's looking at target from a distance such that the target fills a certain amount of screenspace
		/// </summary>
		/// <param name="destinationTarget"></param>
		/// <param name="screenSpace"></param>
		public void Viewpoint_MoveTo(Entities.Entity destinationTarget, float screenSpace, bool snap)
		{
			if (destinationTarget == null)
			{
				System.Diagnostics.Debug.WriteLine ("RenderingContext.Viewpoint_MoveTo() - destinationTarget is null.");
				return;
			}
			// this function currently called by
			//	- ViewportEditorControl
			//	- editorhost
			//	- RenderPreviewWorkspace
			//  - navworkspace

			// TODO: the dependance on context for MoveTo needs to be marginalized or eliminated.
			//       There's three uses of the context/camera
			//			- for FOV to compute scale to fit distance - and this can change if the viewport dimensions change
			//            either while flyto or even
			//			- camera up vector
			//          - panning start/end ray cast results
			if (mProjectionType == Viewport.ProjectionType.Perspective)
			{
				Entity animationTarget = this.Viewpoint;
				// TODO: what if we forced Up to always be orthogonal to plane of elliptic for system?
				//Vector3d up = this.Camera.Up;
				Vector3d up = Vector3d.Up();
				
				animationTarget.BlackboardData.SetString ("focus_entity_id", destinationTarget.ID);
				// TODO: what if instead we provided a Viewport index and from that we can query fov and up vector
				animationTarget.BlackboardData.SetFloat ("camera_fov", mCamera.FOVRadians);
				animationTarget.BlackboardData.SetVector ("camera_up", up);
				
				// goal arguments, destinationTargetID, fitDistance <-- requires camera fov ultimately since we can gain radius
				// min/max distances away from targetPosition (for zooms)
				// TODO: Nov.26.2016 - do the following control and behavior set's belong here? If animation behavior is playing wont we stop it? 
				if (snap)
				{
					// TODO: these min/max values seem reversed.  0 allows closer in zoom and -80 further out.     
					animationTarget.BlackboardData.SetDouble ("orbit_radius_min", 10);
					animationTarget.BlackboardData.SetDouble ("orbit_radius_max", 1000);
					animationTarget.BlackboardData.SetString ("control", "user");
					animationTarget.BlackboardData.SetString ("behavior", "behavior_orbit");
				}
				else 
				{
					animationTarget.BlackboardData.SetString ("control", "animated");
					animationTarget.BlackboardData.SetString ("behavior", "animation_flyto");   
				}

				// TODO: does the "behavior" and "control" get set back to "user" and "behavior_orbit" or "animation_flyto" after animation ends?
				//       I believe that it does get set back after flyto animation completes.  
			}
			else  // orthographic or isometric
			{
				// TODO: verify our radius check works for entities that use inherited scales of parents
				//        - TODO: boundingBox must have first been rescaled when mRoot was rescaled.
				double radius = destinationTarget.BoundingBox.Radius;
				double diameter = radius * 2;
				// since there is no perspective, zoom is diameter / viewportWidth
				float zoomAmount = (float)(diameter / mViewport.Width);
				UpdateOrthographicView(destinationTarget.GlobalTranslation, zoomAmount);
			}
		}
		

		#region ZoomScaleFactors
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// This is the same as GetConstantScreenSpaceScale since scaling distance
		/// or scaling the size of the model results in the same ratio being achieved.
		/// </remarks>
		/// <param name="targetRadius"></param>
		/// <returns></returns>
		public double GetZoomToFitDistance(double targetRadius)
		{
			// a simple zoom out value computation
			// http://www.gamedev.net/topic/590943-calculating-the-zoom-out-value/   <-- used this one
			// - not used -> http://www.ericsink.com/wpf3d/A_AutoZoom.html
			// - not used -> http://www.gamedev.net/topic/528056-zoom-of-a-3d-object/
			// our FOV never changes for a particular context regardless of planet rendering or small things
			if (mNeedNewCamera)
				ViewProjectionChanged();
			
			if (mCamera == null) return 0;

			return targetRadius / Math.Tan(mCamera.FOVRadians / 2d);
		}

		/// <summary>
		/// returns a scaling value
		/// </summary>
		/// <returns></returns>
		public double GetConstantScreenSpaceScale(double distanceToCamera)
		{
			return distanceToCamera / Math.Tan(mCamera.FOVRadians / 2d);
		}

		public double GetConstantScreenSpaceScaleOrthographic(double diameter)
		{
			double result = mViewport.Width * mCamera.Zoom;

			// 1600 meter's visible horizontally

			// 2 meter diameter our object


			return result / diameter;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cameraPosition">Camera space position of the Entity</param>
		/// <param name="position"></param>
		/// <param name="percentageScreenSpace"></param>
		/// <returns></returns>
		public double GetConstantScreenSpaceScalePerspective(Vector3d cameraSpacePosition, float percentageScreenSpace)
		{
			return GetConstantScreenSpaceScalePerspective (cameraSpacePosition.Length, percentageScreenSpace);
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cameraPosition">Camera space position of the Entity</param>
		/// <param name="position"></param>
		/// <param name="percentageScreenSpace"></param>
		/// <returns></returns>
		public double GetConstantScreenSpaceScalePerspective(double distance, float percentageScreenSpace)
		{
			double result;

			if (distance <= mFar)
			{
				result = mFar / (mFar - mNear) + (mNear * mFar / (mFar - mNear) * (1d / distance));
				//UZ = (FarPl / (FarPl - NearPl)) + ((NearPl * FarPl) / (FarPl - NearPl)) * (1 / Z)

				result = mNear + ((mFar - mNear) * ((distance - mNear) / (mFar - mNear)));

				//NearPos + ((FarPos - NearPos) * ((sngDistFromNearPlane - NearPl) / (FarPl - NearPl)))

				//return result * 0.01;
			}
			else
			{
				result = mFar2 / (mFar2 - mNear2) + (mNear2 * mFar2 / (mFar2 - mNear2) * (1d / distance));
				//UZ = (FarPl / (FarPl - NearPl)) + ((NearPl * FarPl) / (FarPl - NearPl)) * (1 / Z)

				result = mNear2 + ((mFar2 - mNear2) * ((distance - mNear2) / (mFar2 - mNear2)));

				//NearPos + ((FarPos - NearPos) * ((sngDistFromNearPlane - NearPl) / (FarPl - NearPl)))

				//return result * 0.01;
			}
			result *= percentageScreenSpace;
			double comparisonTest = GetConstantScreenSpaceScale(distance);
			comparisonTest *= percentageScreenSpace;
			//return comparisonTest;
			return result;
		}

		public double GetConstantScreenSpaceScale2(Vector3d cameraSpacePosition, float percentageScreenSpace)
		{
			Matrix tmpView = mCamera.View;
			tmpView.M41 = 0;
			tmpView.M42 = 0;
			tmpView.M43 = 0;

			Vector3d ppcam0 = Vector3d.TransformCoord(cameraSpacePosition, tmpView);
			Vector3d ppcam1 = ppcam0;
			Matrix projection = mCamera.Projection;

			//     Point ppcam0 = pos * view;
			//     Point ppcam1 = ppcam0;
			ppcam1.x += 1.0f;


			double l1 = 1.0f / (ppcam0.x * projection.M14 + ppcam0.y * projection.M24 + ppcam0.z * projection.M34 + projection.M44);
			double l2 = 1.0f / (ppcam1.x * projection.M14 + ppcam1.y * projection.M24 + ppcam1.z * projection.M34 + projection.M44);

			double c1 = (ppcam0.x * projection.M11 + ppcam0.y * projection.M21 + ppcam0.z * projection.M31 + projection.M41) * l1;

			double c2 = (ppcam1.x * projection.M11 + ppcam1.y * projection.M21 + ppcam1.z * projection.M31 + projection.M41) * l2;

			// TODO: verify c1 > c2
			// c1 - c2 should always result in a value between 0.0 and 1.0 (but never exactly 0.0) or else
			// the item should not be rendered at all
			// TODO: if c1 - c2 = 0.0 then make that result equal some minimum value like 0.00001
			double CorrectScale = 1.0f / (c1 - c2);

			// TODO: if we can verify that "CorrectScale" is close to 1.0 when we're very close to the object and then
			// larger scale as we move far away, then i think the only problem is the following divide by _viewport.Width which i think
			// represents our "percentage screen space" when the object is as near as possible and filling the screen
			double result = percentageScreenSpace * CorrectScale / mViewport.Width;
			System.Diagnostics.Trace.WriteLine("Percentage screen space = " + result);
			return result;
		}

		//// The two functions COmputeConstantScale() and GetScalingFactor() are originally from code in Billboard.cs to create a screenspace quad
		//// from http://www.flipcode.com/archives/Textured_Lines_In_D3D.shtml
		//float ComputeConstantScale(const Point& pos, const Matrix view, const Matrix proj)
		//{
		//     Point ppcam0 = pos * view;
		//     Point ppcam1 = ppcam0;
		//     ppcam1.x += 1.0f;

		//     float l1 = 1.0f/(ppcam0.x*proj.m[0][3] + ppcam0.y*proj.m[1][3] + ppcam0.z*proj.m[2][3] + proj.m[3][3]);
		//     float c1 =  (ppcam0.x*proj.m[0][0] + ppcam0.y*proj.m[1][0] + ppcam0.z*proj.m[2][0] + proj.m[3][0])*l1;
		//     float l2 = 1.0f/(ppcam1.x*proj.m[0][3] + ppcam1.y*proj.m[1][3] + ppcam1.z*proj.m[2][3] + proj.m[3][3]);
		//     float c2 =  (ppcam1.x*proj.m[0][0] + ppcam1.y*proj.m[1][0] + ppcam1.z*proj.m[2][0] + proj.m[3][0])*l2;
		//     float CorrectScale = 1.0f/(c2 - c1);
		//     return CorrectScale / float(mRenderWidth);
		//}

		//// the below routine is from the planet rendering gamasutra article by Oneil
		//private float GetScalingFactor(Vector3d camPos, Vector3d meshWorldPos)
		//{
		//    Vector3d dir = meshWorldPos - camPos;
		//    double distance = dir.Length;
		//    double half_farplane = _farplane * .5; // TODO: these farplane values and such should be based on the current viewport

		//    // scale down the distance to the exponent that puts it between farplane / 2 and  farplane
		//    double scale = 1;
		//    scale *= (distance >= _max_visible_range)
		//                 ? _farplane
		//                 : half_farplane + half_farplane * (1.0f - (double)Math.Exp(-2.5F * distance / _max_visible_range));
		//    scale /= distance;

		//    return (float)scale;
		//}

		// there's a practical limit to how far we can set our farplane and some large objects should still be visible
		// even beyond that so that's what max_visible_range is for...
		//double _max_visible_range = 10000000000;

		// TODO: I think GetScalingFactor we've moved to mContext (RenderingContext.cs)
		//double _farplane = 100000;
		//private double GetScalingFactor(Vector3d camPos, Vector3d meshWorldPos)
		//private double GetScalingFactor (double distance)
		//{
		//                //#define MIN_DISTANCE	0.01
		//    //#define MAX_DISTANCE	1000.0				// Distance to desired far clipping plane
		//    //#define MAX_DISCERNABLE	1000000.0			// Beyond this distance, everything is rendered at MAX_DISTANCE
		//    //#define HALF_MAX		(MAX_DISTANCE*0.5)	// Everything between HALF_MAX and MAX_DISCERNABLE is scaled exponentially between HALF_MAX and MAX_DISTANCE

		//    //Vector3d dir = meshWorldPos - camPos;
		//    //double distance = dir.Length;
		//    double _farplane = mCurrentState._far;
		//    double half_farplane = _farplane * .5;

		//    // scale down the distance to the exponent that puts it between farplane / 2 and  farplane
		//    double scale = 1;
		//    scale *= (distance >= _max_visible_range)
		//                 ? _farplane
		//                 : half_farplane + half_farplane * (1.0f - Math.Exp(-2.5d * distance / _max_visible_range));
		//    scale /= distance;

		//    return scale;
		//}



		// actually what other games i think do is have a farplane depth and a max_visible_distance constant
		// for which they can see things like planets but that still doesnt solve the question of
		// how do you know if a particular thing is visible between the farplane and the maxvisible distance
		// there is where we do our first pass to find these "very large objects"  and we shouldnt have to compute
		// the screenspace of the thing every frame, we should simply be able to compute it's max visible distance
		// based on it's size and cache that value.
		// how can we determine a formula for that is the question?

		// distance where the angular diameter = 1 = 360 * Diameter / ( 2 * Math.PI);
		// so if we want the distance where the angular diameter is .1 then use 3600 instead
		// if you want .01 then 36000

		// 
		// if (farplane.Distance < entity.VisibleDistance && distance < entity.VisibleDistance)

		// earth's moon is 3,000 kilometers in diameter
		// executor class star destroyer is 19km
		// largest space ship in scifi is ~100km and some of those especially large space stations
		// will perhaps be visible beyond a farplane distance of 10km so a rule of thumb on visible distance might be
		// for every magnitude over 1km in size, it's visible by a magnitude over 10km.  So
		// a 10km space station would be visible from 100km
		// a 100km death star would be visible from 1,000km
		// but the main point is this, it's more than just moons and above that will be visible beyond the farplane
		// and besides, some tiny ships might have a visibilityrange that is even smaller than the farplane
		// visibilityrange being visible naked eye detection, but there'd also be sensor detection profiles
		// that would use a scale of say a 1meter by 1meter super reflective object at 100 meters
		// a stealth fighter might have a signature of .1 (or 1 centimeter of super reflective object)

		// distnace = (log 1000) + 1 = 4
		//
		// note: since our scales are so vast, scaling down the sizes and positions of our bodies will result in some imprecision i think
		// so say 1000 meter difference in distance might just get wiped out.
		public double GetNavZoomFactor()
		{
			// TODO: replace this with our contex mContext.GetConstantScreenSpaceScale
			//mContext.GetZoomToFitDistance
			// or ?  i think GetZoomToFitDistance is working
			//mContext.GetConstantScreenSpaceScale

			return 1;
			// TODO: not half the farplane depth, we want the width and height of the far frustum plane

			//double halfWidth, halfHeight;
			//double result = Math.Min(halfHeight, halfWidth) * 2;

			double viewLen = Far - Near;

			// use some trig to find the height of the frustum at the far plane
			double height = viewLen * Math.Tan(FOVRadians * 0.5);

			// with an aspect ratio of 1, the width will be the same
			double width = height * Viewport.AspectRatio;

			double result = Math.Min(height, width);

			// TODO: final result should be based on a zoom factor
			return result / mScene.Root.RegionDiameterX;
		}


		/// <summary>
		/// Returns a Vector3d value that represents the minimum distance to guarantee the entire
		/// model is visible on screen at once.
		/// </summary>
		/// <param name="parentEntity"></param>
		/// <param name="target"></param>
		/// <param name="pick"></param>
		/// <param name="mousePosition3D"></param>
		/// <returns></returns>
		public Vector3d GetNonCelledPerfectFitPlacementPosition(Keystone.Entities.Entity target, Vector3d mousePosition3D)
		{
			Vector3d result;
			double targetRadius = 0;

			try
			{
                if (target is ModeledEntity)
                {
                    // We cannot test entity.SceneNode.BoundingBox because it's not added to the scene yet
                    ModeledEntity modeledEntity = (ModeledEntity)target;
                    if (modeledEntity.Model == null && modeledEntity.SelectModel(0) == null)
                    {
                        BoundingBox childBoxes = GetBoundingBoxesRecursive((Elements.BoundTransformGroup)modeledEntity, new BoundingBox());
                        targetRadius = childBoxes.Radius;
                    }
                    else
                    {
                        targetRadius = target.BoundingBox.Radius;
                    }
                }
                else
                    targetRadius = 1;  
                
            }
			catch (Exception ex)
			{
				System.Diagnostics.Debug.Assert(true, "RenderingContext.GetNonCelledPerfectFitPlacementPosition() - need to track down why sometimes the bounding box is screwed...");
			}

			if (targetRadius == float.MaxValue * .5f)
				targetRadius = 1; // the boundingbox is not initialized yet
			// so far this seems to occur with .obj's that are loaded.
			else if (Math.Abs (targetRadius) > Far)
				targetRadius = Far * .5f;


			// mousePosition3D is in camera space, we need it in world space
			Vector3d worldMousePickPosition = mousePosition3D + this.Position;
			Vector3d dir = Vector3d.Normalize(worldMousePickPosition - Position);


			// TODO: this GetZoomToFitDistance is always the same and that's fine because
			// it's supposed to allow us to keep the entity at a fixed distance, however
			// for some reason this is not working when we move further from the origin it seems
			Vector3d scale = dir * GetZoomToFitDistance(targetRadius);
			result = worldMousePickPosition + scale;

			return result;
		}
        #endregion

		// TODO: move this perhaps as static method to BoundTransformGroup?
        private BoundingBox GetBoundingBoxesRecursive(Elements.BoundTransformGroup group, BoundingBox box)
        {
            if (group == null) throw new ArgumentNullException();

            BoundingBox boxes = new BoundingBox();
            boxes.Combine(box);
            for (int i = 0; i < group.ChildCount; i++)
                // TODO: for Models, do they need to be transformed to give us a World box?
                if (group.Children[i] is Elements.BoundTransformGroup)
                    boxes = GetBoundingBoxesRecursive((Elements.BoundTransformGroup)group.Children[i], boxes);
                else if (group.Children[i] is Elements.BoundNode)
                {
                    BoundingBox childBox = BoundingBox.Transform(((Elements.BoundNode)group.Children[i]).BoundingBox, group.RegionMatrix);
                    boxes.Combine(childBox);
                }

            return boxes;
        }

		/// <summary>
		/// Initialize threaded screenshot variables.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="screenshotCompletedHandler"></param>
		public void Screenshot(string path, EventHandler screenshotCompletedHandler)
		{
			mScreenShotScheduled = true;
			mScreenShotPath = path;
			mScreenShotSavedHandler = screenshotCompletedHandler;
		}

		#region Debugging

//        private void DrawMarkers(Vector3d cameraSpacePosition)
//        {
//            // obsolete?
//            return;
//            // TODO: Can all of this be done during the _currentRegionSet.Add()
//            // and then we can test if the item IsSelected
//            // 
//            // update things such as selected edge, face, vertex, etc
//            // now that doesnt require a "Selected" entity... so how do we render those?
//            // in our Controller we could set in the scene the MouseOverItem as an entire PickResult
//            // object and grab the data from there.  Then here in the viewport RenderingContext we can skip it
//            // if rendering those things isn't enabled here
//            Keystone.Collision.PickResults result = _context.Workspace.MouseOverItem;
//            if (result == null) return;
//            
//            // TODO: i added the following for selected, but 
//            // 1) it may be broken and not property being placed into camera space
//            // 2) i may decide that rather than debug lines for this, use 2d image
//            // so that the selected box is in screenspace... but then, we dont get proper depth sorting
//            // We could use a mesh though just like we do with our Widgets.  In fact
//            // all we'd need is 4 instances of 1 corner mesh that we position and scale by distance
//            // just like all widgets.
//            if (_context.Workspace.Selected != null)
//            {
//                
//                //Line3d l = new Line3d(_context.Scene.DebugPickLine.Point[0], _context.Scene.DebugPickLine.Point[1]);
//                //l.Point[0] -= _context.Position; // line must be rendered like everything else in camera space
//                //l.Point[1] -= _context.Position;
//
//                //this.Add(new Line3d[] { l }, CONST_TV_COLORKEY.TV_COLORKEY_MAGENTA);
//            }
//
//            //  if (result.CollidedObjectType != CollidedObjectType.EditableMesh) return;
//
//            //// draw the selected polygon
//            //if (result.FaceID > -1)
//            //{
//            if (result.FacePoints != null)
//            {
//                // draw the face
//                // our face points are in model space so we need the camera view matrix
//                // and we need the polygon's world matrix if we want to draw it in the correct place
//                //  System.Diagnostics.Debug.WriteLine("Mouse over cell " + result.FaceID.ToString());
//                //  System.Diagnostics.Debug.WriteLine("Mouse over vertex " + result.VertexID.ToString());
//                // fortunately, the current Region Info has the correct view/matrix to use for this 
//                // so we only need to transform the world space coords to region space
//                Vector3d[] polyPoints = new Vector3d[result.FacePoints.Length];
//
//                // we use relativeRegionOffset because the camera offset changes based on the current
//                // region vs the region that this selected face were drawing is in
//                // TODO: this MUST use similar code to our culling which takes into account
//                // player vehicle rotations.  In fact it's the same code required for mouse picking
//                // to work on an interior that is always at origin technically
//                // BUT WAIT, isn't this supposed to be set for us in the cached Projection and View matrices
//                // here in this PVS?!  I should only need to worry about region specific coordinates
//                
//                Matrix translationMatrix = Matrix.CreateTranslation(-_context.Position);
//                Matrix worldMatrix = result.Entity.RegionMatrix * translationMatrix;
//                polyPoints[0] = Vector3d.TransformCoord(result.FacePoints[0], worldMatrix);
//                polyPoints[1] = Vector3d.TransformCoord(result.FacePoints[1], worldMatrix);
//                polyPoints[2] = Vector3d.TransformCoord(result.FacePoints[2], worldMatrix);
//                polyPoints[3] = Vector3d.TransformCoord(result.FacePoints[3], worldMatrix);
//
//                this.Add(polyPoints, CONST_TV_COLORKEY.TV_COLORKEY_GREEN, false);
//
//                // debug draw the closest edge 
//                if (result.EdgeID > -1)
//                {
//
//                    Line3d edge = new Line3d(Vector3d.TransformCoord(result.EdgeOrigin, worldMatrix),
//                        Vector3d.TransformCoord(result.EdgeDest, worldMatrix));
//                    this.Add(new Line3d[] { edge }, CONST_TV_COLORKEY.TV_COLORKEY_RED, false);
//
//                    //   DrawNeighboringFaces(qeFace);
//                }
//            }
//        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// It's important that this get called with the proper camera matrix and projection
        /// which is why we test if the current item being culled is also currently selected
        /// by the mouse, or has mouse rollover 
        /// </remarks>
//        private void DrawSelectedDebugInfo(Entity entity, Vector3d cameraSpacePosition)
//        {
//            
//        }

        //private void DrawNeighboringFaces(EditDataStructures.Face face)
        //{
        //    // now for testing purposes, lets draw in another color, the neighobring faces
        //    EditDataStructures.Face[] qeNeighbors = face.Neighbors;

        //    foreach (EditDataStructures.Face f in qeNeighbors)
        //    {
        //        // get the vertices
        //        Vector3d[] vertices = f.Vertices;

        //        Vector3d[] p = new Vector3d[vertices.Length];
        //        for (int k = 0; k < vertices.Length; k++)
        //        {
        //            Vector3f dummy = new Vector3f();
        //            // just so we can get the temp var to have transformed coordinates
        //            Microsoft.DirectX.Direct3D.CustomVertex.PositionNormalTextured temp = MakeD3DCustomVertex(vertices[k],
        //                                                                           dummy, true);
        //            p[k].x = temp.X;
        //            p[k].y = temp.Y;
        //            p[k].z = temp.Z;
        //        }

        //        // draw them in a new color
        //        DebugDraw.Draw(new Polygon(p), CONST_TV_COLORKEY.TV_COLORKEY_RED);
        //    }
        //}

		public void RenderDebugText(string text, int offsetX, int offsetY, int color)
		{
			// send to the debugDraw static object for queued drawing which gets "commited" in our actual Render() method
			if (mRegionSets == null || mRegionSets.Count == 0) return;
			mRegionSets[0].Add(text, offsetX, offsetY, color, false);
		}
		
		private void RenderDebug()
		{
			Entities.Entity _selected = null;
			if (this.Workspace.SelectedEntity != null)
				_selected = this.Workspace.SelectedEntity.Entity;

			// 	PSSM DEBUG
			if (ShowCullingStats)
			{
				if (mShadowMappingEnabled)
					ParallelSplitShadowMapping.RenderShadowMapDebug();
			}
			
			// DEBUG TEXT IS NOT PART OF SCENE IN THE WAY AN ENTITY LABEL IS SO WE DONT
			// WANT TO INJECT IT AS IMMEDIATE MODE ITEM THAT THEN GETS RENDERED EVERY Render Pass
			// of ScalerDrawer.Render() especially for shadows and deferred where we have multiple passes.
			// We certaintly don't intend to render debug text multiple times and into any RenderSurface
			// CoreClient._CoreClient.Text.Action_BeginText();
			//CoreClient._CoreClient.Profiler.Display (10f, 20f);
			
			int textLeft = 10;
			int textTop = 5;
			int fontHeight = 14; // TODO: font spacing should be computed based on font setting
			

			if (this.ShowTVProfiler)
				// blank lines
				for (int i = 0; i < 7; i++)
					RenderDebugText ("", textLeft, textTop += fontHeight, 0);

            
			if (ShowCullingStats)
			{
				// camera text
				string cullingText = GetCameraDebugText() + "      " +  GetCullingStats();;
				RenderDebugText (cullingText, textLeft, textTop, _fontColorStats.ToInt32());
				
				// blank line
				//RenderDebugText ("", textLeft, textTop += fontHeight, 0);
				
				// cull stats
				//cullingText = CompileDebugOutput();
				//RenderDebugText (cullingText, textLeft, textTop += fontHeight, _fontColorStats.ToInt32());
				
				// blank line
				//RenderDebugText ("", textLeft, textTop += fontHeight, 0);
			}

            textLeft = this.mViewport.Width / 4;
            textTop =  fontHeight + 5;

            if (this.Scene.Simulation.Running == false)
                RenderDebugText("PAUSED", textLeft, textTop, _fontColorStats.ToInt32());

            textLeft = this.mViewport.Width / 8;
            if (this.Scene.Simulation.CurrentMission != null && this.Scene.Simulation.CurrentMission.Enable)
                RenderDebugText("ARCADE MODE", textLeft, textTop, _fontColorStats.ToInt32());

            if (ShowLineProfiler)
				CoreClient._CoreClient.Profiler.Display (textLeft, textTop, RenderDebugText);
			
			//CoreClient._CoreClient.Text.Action_EndText();

		}
		
		private string GetCameraDebugText ()
		{
            if (Viewpoint == null) return null;

			string padding = "       ";
			string cullingText = "";
			
			float x, y, z;
			x = y = z = 0;
			if (Viewpoint.Region is Zone)
			{
				x = ((Zone)Viewpoint.Region).ArraySubscript[0];
				y = ((Zone)Viewpoint.Region).ArraySubscript[1];
				z = ((Zone)Viewpoint.Region).ArraySubscript[2];
			}

			string startPos = padding + padding + padding + padding + padding;
			
			// TODO: is there a way to get the screen width and the text's width on screen
			//       and subtract startPos = (screenWidth / 2) - (textWidth / 2)
			string text = string.Format("{0}Zone: [{1}, {2}, {3}]", startPos, x, y, z);
			
			cullingText += text;
			
			text = string.Format("{0}Pos: [{1:0.00}, {2:0.00}, {3:0.00}]",
			                     padding,
			                     this.Position.x, this.Position.y, this.Position.z);
			cullingText += text;
			
			text = string.Format("{0}Look: [{1:0.00}, {2:0.00}, {3:0.00}]",
			                     padding, this.LookAt.x, this.LookAt.y, this.LookAt.z);
			cullingText += text;
			
			return cullingText;
		}

		private string GetCullingStats()
		{
			
			if (this.ShowCullingStats == false) return "";

			string cullingText = GetFormattedCullingStat("Regions Visible: ", "Regions Visible");

			object portalCount = mStats["Portals Visible"];
			if (portalCount != null)
			{
				object portalsTraversed = mStats["Portals Traversed"];
				int count = 0;
				if (portalsTraversed != null)
					count = (int)portalsTraversed;

				cullingText +="Portals Visible\\Traversed: " + mStats["Portals Visible"] + "\\" + count.ToString();
				
			}
			
			cullingText += GetFormattedCullingStat("Octree Nodes: ", "Octree Nodes Visited");
			cullingText += GetFormattedCullingStat ("Octant's Culled", "Octant's Culled");
			cullingText += GetFormattedCullingStat("Terrains: ", "Terrains Visible");
			cullingText += GetFormattedCullingStat("Actors: ", "Actors Visible");
			cullingText += GetFormattedCullingStat("Meshes: ", "Meshes Visible");
			cullingText += GetFormattedCullingStat("MinimeshGeometry: ", "MinimeshGeometry Visible");
			cullingText += GetFormattedCullingStat("#", "MinimeshGeometry Element Count");
			cullingText += GetFormattedCullingStat("2D Quads: ", "2D Quads Visible");

			return cullingText;
		}

		private string GetFormattedCullingStat(string label, string key)
		{
			object stat = mStats[key];
			if (stat == null) return "";
			
			string trailingPadding = "  ";
			
			string text = "[" + label + mStats[key].ToString() + "]" + trailingPadding;
			return text;
		}
		#endregion
		
		#region Stats
		/// <summary>
		/// Thread sychronized stats tracking class since we may have many culling threads.
		/// </summary>
		internal class Stats
		{
			private System.Collections.Hashtable _stats = new System.Collections.Hashtable();
			private object mStatSync = new object();


			public object this[string statName]
			{
				get { return _stats[statName];}
				set { _stats[statName] = value; }
			}

			public void IncrementStat (string statName)
			{
				IncrementStatByAmount(statName, 1);
			}

			public void IncrementStatByAmount(string statName, uint amount)
			{
				lock (mStatSync)
				{
					if (_stats.ContainsKey(statName))
						_stats[statName] = (uint)_stats[statName] + amount;
					else
						_stats[statName] = (uint)1;
				}
			}

			public void Clear(bool valuesOnly)
			{
				lock (mStatSync)
				{
					if (valuesOnly)
					{
						if (_stats.Count == 0) return;

						string[] keys = new string[_stats.Count];
						_stats.Keys.CopyTo (keys, 0);
						for (int i = 0; i < keys.Length; i++)
							_stats[keys[i]] = (uint)0;
					}
					else _stats.Clear();
				}
			}
		}
		#endregion

		#region ClientScene.cs
		// TODO: The below is from when most of these Update/Render/RenderScene were in ClientScene.cs
		//            However, until i get the various FX working again, lets keep the below so we remember the order things were
		//            done before we started overhauling things.

		//// cull requests can take place from camera positions that are different than our main position so find the sector for the camera's position
		//public Stack<Node> CullRequest(Camera camera)
		//{
		//    //Culler tmpCuller = new Culler();
		//    //tmpCuller.Clear(camera);

		//    //_spatialGraph.FindCameraRegion(camera.Position).Traverse(tmpCuller);
		//    //return tmpCuller.NodeStack;

		//    Stack<Node> result = _spatialGraph.CullFrom(camera);
		//    return result;
		//}


		//// TODO: It's fine to have _pager.Update() once, but oterhwise context's should render all to each viewport
		//// on their own... not go through Scene which then goes through each context and updates, renderbeforeclear etc
		//public void Update(float elapsed)
		//{
		//    _pager.Update(); // TODO: pager must have viewports register/unregister so it can make proper determination

		//    // TODO: this is wrong.  We dont want these cameras tied to the scene.  We want the contexts
		//    // so rather than having CreateCamera() here, we should have CreateContext.  Then contexts can be the sole
		//    // holder of the camera's it needs based on the context.  No other place should care about cameras.
		//    // only itterate thru the viewports assigned to this scene
		//    foreach (RenderingContext context in _contexts )
		//    {
		//        if (context.Viewport != null && context.Scene == this)
		//        {
		//            context.Update(context, elapsed);


		//            RenderBeforeClear(context.Camera);
		//            context.Clear();

		//            //ps.Render(); // alpha particles like smoke need to be rendered in depth sorted order
		//            //  _waterFall.Update();
		//            //  _waterFall.Render();

		//            // DebugDraw.DrawAxisIndicator(new Vector3d( 0,0,0), null, 0);
		//            //_occlusionFrustum.Render();

		//            //if (DepthofField) DoF.UpdateDepthOfField();

		//            //foreach (EDGE[] seg in TerrainGridBuilder._segments)
		//            //{
		//            //    DebugDraw.DrawLines(seg, CONST_TV_COLORKEY.TV_COLORKEY_RED);
		//            //}
		//            Render(context);
		//            DebugDraw.Clear();
		//        }
		//    }
		//}


		//private void RenderBeforeClear(Camera camera)
		//{
		//    // TODO: do I need HACK because when water and such modifies the camera, the subsequent calls to CulLDrawer.Clear() would then start using
		//    // that incorrect position to Translate models??
		//    foreach (IFXProvider fx in ((ClientSceneManager)_sceneManager).FXProviders)
		//        if (fx != null) fx.RenderBeforeClear(camera);
		//}

		//// RenderScene() is the method for drawing the visible items.
		//// It's also the method that is invoked directly by FX's such as reflecting Water
		//// to render the reflection.  As such, we have to test
		//// TODO: not meant to be a "public" method but for right now during init in Engine.CS i need to refer to this
		//// method for the callback method for some of the FX
		//private void RenderScene(RenderingContext context)
		//{
		//    // skies and starboxes and starfields, long distance nebulas, should be rendered first
		//    foreach (IFXProvider fx in ((ClientSceneManager)_sceneManager).FXProviders)
		//        if (fx != null && fx.Layout == FXLayout.Background)
		//            fx.Render(context.Camera);

		//    // draw the scene // TODO: We need to use a culler that is specific to the camera since this method gets invoked by FX doing things such as reflection
		//    //((ScaleDrawer)_drawer).Render(camera, camera.Viewport.Context.VisibleNodes, camera.Viewport.Context.VisibleLights);
		//    ((ScaleDrawer)_drawer).Render(context, context.VisibleStates , context.VisibleLights);

		//    ((ScaleDrawer)_drawer).RenderOverlays(context.Camera);

		//    // draw those scene elements that should be drawn after the rest of the scene has been drawn.
		//    foreach (IFXProvider fx in ((ClientSceneManager)_sceneManager).FXProviders)
		//    {
		//        // Some FX's "Render()" method (e.g Water) must only be rendered in the final scene render.
		//        if (fx != null && fx.Layout == FXLayout.Foreground )
		//            fx.Render(context.Camera);
		//    }
		//}

		//private void Render(RenderingContext context)
		//{
		//    RenderScene(context);

		//    // render post FX such as Bloom
		//    foreach (IFXProvider fx in ((ClientSceneManager)_sceneManager).FXProviders) // _fxProviders)
		//    {
		//        // NOTE: because Bloom uses the 2d fullscreen quad draw command it must be the very last
		//        // 2d operation or it will get wiped out.  Water is also rendered here because the actual water meshes are only rendered once
		//        if (fx != null) fx.RenderPost(context.Camera);
		//    }
		//    // TODO: DisplayList is committed after we pop the camera from local origin to local coordinate system! this is why
		//    // our bounding box lines dont show up properly and why some of the translations are wrong in both culler and drawer
		//    DebugDraw.CommitDisplayList(context.Camera);
		//    DebugDraw.CommitTextList();

		//    // interestingly, the way AccurateTimeElapsed works is, it takes the time between frameRenderN - frameRenderN-1
		//    // this means the following debug line = 0 first time because there is no frameRenderN or frameRenderN-1,
		//    // however the second time around, the value is filled.  This must be because when the engine is initialized
		//    // the frameRenderN = Now
		//    // Anyways, this isnt more accurate than me calling Environement.GetTickCOunt() just once at the same place everytime.
		//    // The difference is, TV's version is immune from changing every subsequent call between frames.
		//    // 
		//    //Trace.WriteLine(string.Format("Accurate time elapsed == {0}", Engine.Core.Engine.AccurateTimeElapsed()));
		//    CoreClient._CoreClient.Engine.RenderToScreen();
		//}
		#endregion
	}
}