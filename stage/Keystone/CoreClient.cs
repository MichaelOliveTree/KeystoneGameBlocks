using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Keystone.Utilities;
using Keystone.Cameras;
using Keystone.Controllers;
using Keystone.Devices;
using Keystone.Interfaces;
using Keystone.Sound;
using Keystone.Types;
using Microsoft.DirectX.Direct3D;
using MTV3D65;
using Viewport = Keystone.Cameras.Viewport;

namespace Keystone
{
    public class CoreClient : Core
    {
        internal static CoreClient _CoreClient
        {
            get { return (CoreClient)_internalCore; }
            set { _internalCore = value; }
        }

        // core client
        private GraphicsDevice _window;
        private static Helpers.ThreadSafeDictionary<string, Viewport> _viewports;
        private AudioManager _audio;
        private DeviceCaps _deviceCaps;
        private TVInternalObjects _internals;
        private TVScreen2DImmediate _screen2D;
        private TVEngine _engine;
        private TVScene _tvscene;
        private TVGlobals _globals;
        private TVMaterialFactory _materials;
        private TVTextureFactory _textures;
        private TVCameraFactory _cameras;
        private TVLightEngine _light;
        private TVAtmosphere _atmosphere;
        private TVScreen2DText _text;
        private TVMathLibrary _math;
        private TVPhysics _physics;
        //private TVInputEngine _input;
        private Keystone.Profiler.Profiler mProfiler;
        
        private Device _device;
        private List<INotifyDeviceReset> _objectsToNotify;
        private Mouse _mouse;
        private Keyboard _keyboard;
        private InputController _systemIOController;
        //private InputController ioController;
        //private RenderingContext  _primaryContext;
        private Keystone.FX.InstanceRenderer _instanceRenderer;

        private const string DEBUG_FILENAME = @"debugfile.txt";
        // TODO: singleton pattern
        public CoreClient(Settings.Initialization ini, string basePath, string dataPath,string modsPath, string savesPath,  bool multithreadingEnable) 
            : base(ini, basePath, dataPath, modsPath, savesPath)
        {
            try
            {
                _internalCore = this;
                _engine = new TVEngine();
                _engine.SetFPUPrecision(true);
                if (File.Exists(Path.Combine(_basePath, DEBUG_FILENAME)))
                    File.Delete(Path.Combine(_basePath, DEBUG_FILENAME));

                _engine.SetDebugMode(true);
                _engine.SetDebugFile(Path.Combine(_basePath, DEBUG_FILENAME));
                // multithreading required before engine is initialized to any window
                _engine.AllowMultithreading(multithreadingEnable);
                _engine.SetAngleSystem(CONST_TV_ANGLE.TV_ANGLE_DEGREE); // our camera and player controls all use degrees
                             
                //TODO: in the future this may be configurable... so like different mods can use different folders so long as the underlying hierarchy remains identicle
                _engine.SetSearchDirectory(_dataPath);
                // search directory must be done immediately here prior to attempting to load config scripts
               
                _deviceCaps = new DeviceCaps();
                _audio = new AudioManager();
                CRC32.Init();
               
                InitializeTV3D();
                

            	InitializeProfiler();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("CoreClient.ctor() - ERROR: Failed to initialize. " + ex.Message);
            }
        }
        
    	public new Keystone.Scene.ClientSceneManager SceneManager
        {
    		get { return (Keystone.Scene.ClientSceneManager)_sceneManager; }
            set { _sceneManager = value; }
        }
                
        private void InitializeProfiler ()
        {
            // how do we profile in other classes if the profiler is not static in Core?
    		mProfiler = new Keystone.Profiler.Profiler (CoreClient._CoreClient.Text, CoreClient._CoreClient.Globals);
    		mProfiler.ProfilerEnabled = false;
	        mProfiler.ShowFramesPerSecond = true;
	        mProfiler.CategorizeByTypename = true;
	        mProfiler.Verbose = true;
	        // mProfiler.FullyQualifiedTypename = true

	        // NOTE: since profiler is global, we register these vars once no matter how many viewports are open
	        	        // Registers profiles before we use them
	        int categoryIndex = 0;
	        	  
			// mouse/keyboard	  
			//categoryIndex ++;
	        //string ioCategory = categoryIndex.ToString() + " - I/O";		
			//mProfiler.Register("View Behavior", ioCategory);	        
						
			
	        // networking
	        //categoryIndex ++;
	        //string networkingCategory = categoryIndex.ToString() + " - Networking";
	        //mProfiler.Register("TBD", networkingCategory);
	        
	        // physics
	        //categoryIndex ++;
	        //string phsyicsCategory = categoryIndex.ToString() + " - Physics";
	        //mProfiler.Register("Gravity", phsyicsCategory);
			//mProfiler.Register("Production", phsyicsCategory);
	        //mProfiler.Register("Consumption", phsyicsCategory);
	        
	        // scripting
	        categoryIndex ++;
	        string scriptingCategory = categoryIndex.ToString() + " - Scripting";
	        mProfiler.Register("OnUpdate", scriptingCategory);	
	        mProfiler.Register("OnRenderScript", scriptingCategory);	
	        mProfiler.Register ("OnSelectModel", scriptingCategory);
	        	
	        // culling  
	        categoryIndex ++;
	        string cullingCategory = categoryIndex.ToString() + " - Culling";
	        mProfiler.Register("Bucket Insertion", cullingCategory);	
	        mProfiler.Register("Bucket Creation", cullingCategory);	
	        mProfiler.Register("GetCameraSpaceBox", cullingCategory);
	        mProfiler.Register("IntersectTest", cullingCategory);	
	        mProfiler.Register ("Model Selection", cullingCategory);
	        mProfiler.Register("VisibleItem Creation", cullingCategory);
	        mProfiler.Register("Add Model To PVS", cullingCategory);	

	        
	        mProfiler.Register("Assign Lights", cullingCategory);	
	        mProfiler.Register("Iconize", cullingCategory);		
	        mProfiler.Register("SSScaleCalc", cullingCategory);	
	        	        	
			// minimesh renderer
			categoryIndex ++;	
			string miniCategory = categoryIndex.ToString() + " - Minimesh";			
        	mProfiler.Register("MMAddInstance", miniCategory);
        	mProfiler.Register("MMRender", miniCategory);
        	mProfiler.Register("MMClear", miniCategory);
        	
	        // shadows
	        categoryIndex ++;
	        string shadowCategory = categoryIndex.ToString() + " - ShadowMapping";
	        mProfiler.Register("ShadowMap.AddBBox", shadowCategory);
	        mProfiler.Register("ShadowMap.RenderBeforeClear", shadowCategory);

	        // deferred
//	        categoryIndex ++;
//	        string deferredCategory = categoryIndex.ToString() + " - Deferred";
//	        mProfiler.Register("TBD", deferredCategory);
//	        mProfiler.Register("TBD", deferredCategory);
	        	
	        categoryIndex++;
	        string loopCategory = categoryIndex.ToString() + " - Graphics Loop";
	        mProfiler.Register ("Update - Cull", loopCategory);
	        mProfiler.Register("Render Before Clear", loopCategory);
	        mProfiler.Register("Clear", loopCategory);
	        mProfiler.Register("Render After Clear", loopCategory);
            mProfiler.Register("Model Draw", loopCategory);
            mProfiler.Register("RegionPVS.Draw.mBuckets.TryGetValue", loopCategory);
            mProfiler.Register("RegionPVS.Draw.MoveTVLightToCameraSpace", loopCategory);
            mProfiler.Register("RenderingContext.RenderScene", loopCategory);

            mProfiler.Register("ScaleDrawer.Render() 1", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 2", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 3", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 4", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 5", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 6", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 7", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 8", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 9", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 10", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 11", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 12", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 13", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 14", loopCategory);
            mProfiler.Register("ScaleDrawer.Render() 15", loopCategory);

            // NOTE: profiling indicates that assigning arrays via TVMinimesh.SetPosition/Scale/Rotation/Color/EnableArrays is prohibitively expensive to do every frame!
            //       we may just have to use the method that Zak uses which is to use our own Vertex and Index buffers.
            mProfiler.Register ("MinimeshGeometry.Assign Arrays", loopCategory);
        }

        public void Initialize(GraphicsDevice graphics)
        {
            base.Initialize();
            _isInitialized = false; // will return true after this method completes
            if (graphics == null) throw new ArgumentNullException();
            _window = graphics;
            _mouse = new Mouse(graphics.Handle, false, false);
            _keyboard = new Keyboard(graphics.Handle, true);
            
            // TEMP: june.16.2024 - TODO: trying to initialize perhaps vorbisdotnet or directsound is causing a filenotfoundexception similar to the msvcr71.dll issue when initializing mtv3d65.dll
            //_audio.Initialize(graphics.Handle);


            //Engine.EnableSmoothTime(true);
            // get a reference to a D3DDevice and wire up events so we know when to reacquire it
            _device = new Device(_internals.GetDevice3D());
            Device.IsUsingEventHandlers = true;
            _device.DeviceLost +=D3DDeviceLost;
            _device.DeviceReset += D3DDeviceReset;
            _device.DeviceResizing += D3DDeviceResizing;



            //if (settings.UsesVSync != ThreeState.DriverDefault)
            //{
            //    Engine.SetVSync(settings.UsesVSync == ThreeState.Enabled);
            //}
            //if (settings.UsesFSAA != ThreeState.DriverDefault)
            //{
            //    Engine.SetAntialiasing(settings.UsesFSAA == ThreeState.Enabled, settings.MultisampleType);
            //}


            //Engine.GetViewport().SetAutoResize(true);

            //TVGraphicEffect ge = new TVGraphicEffect();
            //ge.ChangeGamma(3F);

            _tvscene.SetShadeMode(CONST_TV_SHADEMODE.TV_SHADEMODE_PHONG);
            
            //TEMP HACK -- The following settings seem to require that the scene be setup with a camera first or exceptions will be thrown.
            //      Scene.SetMipmappingPrecision(-2);
            //      Scene.SetBackgroundColor(1, 1, 1);
            //      Scene.SetRenderMode(CONST_TV_RENDERMODE.TV_LINE);
            //            Scene.SetAutoTransColor((int)CONST_TV_COLORKEY.TV_COLORKEY_USE_ALPHA_CHANNEL);
            //set's the auto transparency color for textures that are loaded when tv3d auto loads them during Mesh or Actor file loading
            //
            //      Scene.SetTextureFilter - 3D texture filter (as opposed to Screen2D.SEttings_SetTextureFilter) allows you to change the style of your textures. 
            //Note than the CUBIC filters and ANISOTROPIC are not supported on all hardware. 
            //TRILINEAR and ANISOTROPIC filters allows the Mipmapping and produces the best image quality. 
            //POINT produces a Pixellised texturing, and BILINEAR is a good filter but without Mipmapping. 
            //This filter will be stored in memory for next frame and hasn't any impact on 2D filters.
            // // note for the 3d texture filter, bilinear makes bloom look all grainy.  Cant use it with bloom. Use ANISOTROPIC.

            //  Changes the texture filtering to a new texture filter for 2d only. usually to get a good pixel-to-pixel behaviour you should select "point" filter.
            Screen2D.Settings_SetTextureFilter(CONST_TV_TEXTUREFILTER.TV_FILTER_BILINEAR);
            // these two lines related to anisotropic filtering is good for planet shader... 
            // http://en.wikipedia.org/wiki/Anisotropy
            _tvscene.SetMaxAnisotropy(16);
            _tvscene.SetTextureFilter(CONST_TV_TEXTUREFILTER.TV_FILTER_ANISOTROPIC); //(CONST_TV_TEXTUREFILTER.TV_FILTER_ANISOTROPIC);
            //_textures.SetTextureMode(CONST_TV_TEXTUREMODE.TV_TEXTUREMODE_32BITS);
            //_textures.SetTextureMode(CONST_TV_TEXTUREMODE.TV_TEXTUREMODE_COMPRESSED); // <-- TV_TEXTUREMODE__BETTER according to tv forums helps with alpha channel on textures issues
             _textures.SetTextureMode(CONST_TV_TEXTUREMODE.TV_TEXTUREMODE_BETTER);
            //_tvscene.SetMipmappingPrecision(1.0f); // (?);
            _engine.SetAntialiasing(true, CONST_TV_MULTISAMPLE_TYPE.TV_MULTISAMPLE_4_SAMPLES);

            SetBackGroundColor(0.75f, 0.75f, 0.75f, 1.0f);

            

            Light.SetGlobalAmbient(0.0f, 0.0f, 0.0f);
            Light.SetSpecularLighting(true);
            //_tvscene.SetMousePickRangeLimit 
            //_tvscene.SetDithering
            //_engine.SetMainBufferAlpha
            //_engine.SetREFMode 
            //_engine.SetVSync

            
            // TODO: do the following settings disable to zbuffer rejection test for occluded pixels when rednering front to back?
            // need to find out which ones exactly do this so i can only enable /disable them when needed
                       
            // this screen2D blending mode is critical for the atmospheric planet rendering.  actually all 3 of the following lines might be needed for proper rendering
            // i took them from zak's code and frankly even with the above anisotrop and texture mode and filtering i couldnt get it to work until i pasted the following
            // TODO: these sorts of settings should be added as appearance for things like drawing textured quads
            // for things like lens flares
            _screen2D.Settings_SetAlphaBlending(true, CONST_TV_BLENDINGMODE.TV_BLEND_ADD);
           // _screen2D.Settings_SetAlphaTest(false, 0);
            _screen2D.Settings_SetTextureFilter(CONST_TV_TEXTUREFILTER.TV_FILTER_ANISOTROPIC);

            
            
            // TODO: WARNING WARNING WARNING: Fog will cause Sun and Moon since far away, to render at full fog color!
            //       we need to resolve a way of showing atmosphere and fog in such a way that it's not pointless to render
            //       them.  Certainly we could use our own billboard shader and just disable fog there... or we can use a Fog node
            //       that is traversed during render in such a way that it's pushed on to environmental render state (similar to wind too eventually)
            //       that affects zone areas but not Atmosphere.
            
           // HACK: this is a lousy place to setup fog.  it's impossible to just enable/disable on demand this way
           // TODO: fog should be part of an environmental volume node.  Thus when generating a test world
			//      we can add one to each zone, but for the sun day & night cycle, not.
			bool fogEnable = false;
			if (fogEnable)
			{
				// HACK - fog must NOT be enabled during space scene renderings or starfield and sun billboards will be all white
				Vector3d fogColor = new Vector3d(0.75, 0.75, 0.75);
	            _atmosphere.Fog_Enable (fogEnable);
	            _atmosphere.Fog_SetColor ((float)fogColor.x, (float)fogColor.y, (float)fogColor.z);
	          
	            // NOTE: shader semantic fogDensity is not set if LINEAR since simple LERP is expected at ratio between start and end distance
	            _atmosphere.Fog_SetType (CONST_TV_FOG.TV_FOG_EXP2, CONST_TV_FOGTYPE.TV_FOGTYPE_PIXEL);
	            float fogstart = 120 * 4;
	            float fogEnd = fogstart * 4;
	            float fogDensity = 0.0025f;
	            _atmosphere.Fog_SetParameters (fogstart, fogEnd, fogDensity);

            SetBackGroundColor (fogColor);
			}
            // TODO: these should be created during SceneLoad in the scene file data and ensured to be loaded before
            // any object that uses that FX is loaded.  but wait, InstanceRenderer uses minimeshes and is sorta not really
            // an FX at all... i just implemented in that because of how i used to add instances during load now i add them dynamically during cull
            _instanceRenderer = new Keystone.FX.InstanceRenderer(); // minimesh asteroids use this

            // does the following have to be done before we create lights?
            Keystone.Lights.Light.SetSpecularLighting(true);

            _isInitialized = true;
        }

        public Keystone.Profiler.Profiler Profiler { get {return mProfiler;}}
                
        public bool HardwareInstancing
        {
            get
            {
                bool useHardware = (Engine.GetInternalShaderVersion() != CONST_TV_SHADERMODEL.TV_SHADERMODEL_NOSHADER
                    && Engine.GetInternalShaderVersion() != CONST_TV_SHADERMODEL.TV_SHADERMODEL_1_1
                    && Engine.GetInternalShaderVersion() != CONST_TV_SHADERMODEL.TV_SHADERMODEL_2_0);
                return useHardware;
            }
        }
        #region coreClient
        public Device D3DDevice
        {
            get { return _device; }
        }
        


        public Mouse Mouse
        {
            get { return _mouse; }
            set
            {
                _mouse = value;
                if (_mouse != null) _mouse.Attach(_systemIOController);
            }
        }

        public Keyboard Keyboard
        {
            get { return _keyboard; }
            set
            {
                _keyboard = value;
                if (_keyboard != null) _keyboard.Attach(_systemIOController);
            }
        }

        //public RenderingContext  PrimaryContext
        //{
        //    get { return _primaryContext; }
        //    set { _primaryContext = value; }
        //}

        public Keystone.FX.InstanceRenderer InstanceRenderer
        {
            get { return _instanceRenderer; }
        }

        // system controller is always initialized prior to keyboard and mouse because the system controller can run from interpreted commands read from file
        public InputController SystemIOController
        {
            get { return _systemIOController; }
            set
            {
                // system controller should always be the first added to mouse and keyboard
                // in this way it is always given first opportunity to act on a specific input. Thi sway
                // no user script can ever defeat the built in sytem reserved commands (e.g. ESC and F1-F12)
                if (!(value is Controllers.SystemController)) throw new Exception("Invalid IOController type.");
                // detatch the previous controller if it was already set
                if (_systemIOController != value && _systemIOController != null)
                {
                    if (_mouse != null) _mouse.Detach(_systemIOController);
                    if (_keyboard != null) _keyboard.Detach(_systemIOController);
                }
                _systemIOController = value;
                if (_systemIOController != null)
                {
                    if (_mouse != null) _mouse.Attach(_systemIOController);
                    if (_keyboard != null) _keyboard.Attach(_systemIOController);
                }
            }
        }


        public GraphicsDevice Graphics
        {
            get { return _window; }
        }

        //TODO: this public dictionary is horrible
        
        public Helpers.ThreadSafeDictionary<string, Viewport> Viewports
        {
            get { return _viewports; }
        }

        public DeviceCaps DeviceCaps
        {
            get { return _deviceCaps; }
        }

        public AudioManager AudioManager
        {
            get { return _audio; }
        }
        #endregion

        #region TVObjects
        public TVInternalObjects Internals
        {
            get { return _internals; }
        }

        public TVScreen2DImmediate Screen2D
        {
            get { return _screen2D; }
        }

        public TVEngine Engine
        {
            get { return _engine; }
        }

        public TVScene Scene
        {
            get { return _tvscene; }
        }

        public TVGlobals Globals
        {
            get { return _globals; }
        }

        public TVMaterialFactory MaterialFactory
        {
            get { return _materials; }
        }

        public TVTextureFactory TextureFactory
        {
            get { return _textures; }
        }

        public TVCameraFactory CameraFactory
        {
            get { return _cameras; }
        }

        public TVLightEngine Light
        {
            get { return _light; }
        }

        public TVAtmosphere Atmosphere
        {
            get { return _atmosphere; }
        }

        public TVScreen2DText Text
        {
            get { return _text; }
        }

        public TVMathLibrary Maths
        {
            get { return _math; }
        }
        //public TVInputEngine Input
        //{
        //    get { return _input; }
        //}

        public TVPhysics Physics
        {
            get { return _physics; }
            set { _physics = value; }
        }

        #endregion

        private void InitializeTV3D()
        {
            _tvscene = new TVScene();
            _globals = new TVGlobals();
            _math = new TVMathLibrary();
            _light = new TVLightEngine();
            _materials = new TVMaterialFactory();
            _textures = new TVTextureFactory();
            _screen2D = new TVScreen2DImmediate();
            _text = new TVScreen2DText();
            _internals = new TVInternalObjects();
            _cameras = new TVCameraFactory();
            //_physics = new TVPhysics();
            //_input = new TVInputEngine();
            _atmosphere = new TVAtmosphere();

            _objectsToNotify = new List<INotifyDeviceReset>();
            _viewports = new Helpers.ThreadSafeDictionary<string, Viewport>();
        }

        public void SetBackGroundColor(Vector3d color)
        {
        	SetBackGroundColor ((float)color.x, (float)color.y, (float)color.z, 1.0f);
        }
         
        public void SetBackGroundColor(System.Drawing.Color color)
        {
            SetBackGroundColor(new Keystone.Types.Color (color.R, color.G, color.B, color.A));
        }

        public void SetBackGroundColor(Keystone.Types.Color color)
        {
            SetBackGroundColor(color.r, color.g, color.b, color.a);
        }

        public void SetBackGroundColor(float r, float g, float b, float a)
        {
        	// NOTE: this sets the backcolor for ALL viewports under all their respective context's
            foreach (Viewport vp in _viewports.Values)
                vp.BackColor = new Keystone.Types.Color(r, g, b, a);
        }

        #region Device Reset, Resize, Lost
        public void RegisterForDeviceResetNotification(INotifyDeviceReset obj)
        {
            _objectsToNotify.Add(obj);
        }

        public void UnregisterForDeviceResetNotification(INotifyDeviceReset obj)
        {
            _objectsToNotify.Remove(obj);
        }

        private void D3DDeviceLost(object sender, EventArgs e)
        {
            _device = new Device(_internals.GetDevice3D());
        }

        private void D3DDeviceReset(object sender, EventArgs e)
        {
            _device = new Device(_internals.GetDevice3D());
        }

        private void D3DDeviceResizing(object sender, CancelEventArgs e)
        {
            // TODO: is this necessary?  why new device here?
            _device = new Device(_internals.GetDevice3D());
           
        }

        public void ResizeDevice(TVViewport vp)
        {
            foreach (INotifyDeviceReset obj in _objectsToNotify)
                obj.OnBeforeReset();

            // NOTE: If we see error VIEWPORT MANAGER ERROR : Viewport_OnReset : Couldn't create
            // rendering surfaces, unknown DirectX error. Maybe Out of video memory. 
            // dx error : -2147024809
            // it could means that the window we're using to host the viewport
            // has 0 width and 0 height
            vp.Resize();

            foreach (INotifyDeviceReset obj in _objectsToNotify)
                obj.OnAfterReset();
        }

        public void ResizeDevice()
        {
            foreach (INotifyDeviceReset obj in _objectsToNotify)
                obj.OnBeforeReset();

            _engine.ResizeDevice();

            foreach (INotifyDeviceReset obj in _objectsToNotify)
                obj.OnAfterReset();
        }
        #endregion


        protected override void Terminate()
        {
            // remove controllers which may have references to visual widgets before
            // we call base.Terminate() which will shut down the threadpool which may be doing background writes
            //RemoveAllIOControllers();

            _audio.Dispose();
            _mouse.Dispose();
            _mouse = null;
            _keyboard.Dispose();
            _keyboard = null;
            base.Terminate();

            if (_device != null && !_device.Disposed)
            {
                //_device.DeviceLost -= D3DDeviceLost;
                //_device.DeviceReset -= D3DDeviceReset;
                //_device.DeviceResizing -= D3DDeviceResizing;
                //_device = null;
                _device.Dispose();
            }

        }
    }
}