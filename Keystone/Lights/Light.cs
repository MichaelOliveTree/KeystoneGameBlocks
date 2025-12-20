using System;
using System.Collections.Generic;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Interfaces;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Collision;
using KeyCommon.Flags;
using MTV3D65;

namespace Keystone.Lights
{

    public abstract class Light : Entity, IComparable<Light>, IPageableTVNode 
    {
    	public static Color GlobalAmbient;
    	
        // TODO: i though this list of subscribers was a good thing but i dont believe it is afterall.  Its best to use real time
        // traversal to establish which lights should be on for which meshes
        // and then seperate ShadowCasters, ShadowReceivers and ShadowVolume objects for tracking shadows
        // the ShadowVolumes can rely on the visible lights to determine which volumes are active from previous render
        // and it can compare the light's position if it's changed, and it can compare the meshlists
        // but in general, i dont believe i should have subscribers in general for every light
        // OBSOLETE - for now im testing use of LightInfo attached to each entity in a light's bounds
        //protected List<IObserver> _subscribers = new List<IObserver>();
        protected const bool TV_MANAGES_LIGHTS = false; // TODO: i think we should manage our lights especially with deferred
        
        protected Vector3d _direction;
        protected double _rangeSquared;
        protected float _range;
        // Note that although Direct3D uses RGBA values for lights, the alpha color component is not used. 
        protected Color _diffuse;
        protected Color _ambient;
        protected Color _specular;
        protected float _specularLevel = 1.0f; // 0.0 to 1.0f i believe

        // light properties
        protected bool mActive; // culling tests use .Enabled so we need a different var to signify whether light is used during render 
        protected bool _specularEnabled;        // TODO: add as flag?
        protected bool _castShadows;            // TODO: add as flag?
        protected bool _managedLight;
        protected bool _bumplightUseSpecular;
        protected bool _bumplightApproximatePointLightbyDirLight;
        protected bool _useforLightMapping;
        protected bool _isIndoorLight;
        protected int _tvfactoryIndex = -1;
       

//        int lightpriority [??]  - Unknown. I'm not sure what this does, but a reasonable guess would be it controls when the
        //lightsource casts a shadow. We know that in NWN only the strongest lightsource in an area casts shadows, 
        //this may be the value that determines that. Or it could be a flag of some kind.

//bool shadow [0|1] - Probably determines if this light is capable of casting shadows.

//bool lensflares

        #region Static Methods

        public static void SetGlobalAmbient(float r, float g, float b)
        {
            CoreClient._CoreClient.Light.SetGlobalAmbient(r, g, b);
            GlobalAmbient = new Color (r, g, b, 1.0f);
        }

        // NOTE: if we want some lights to use specular and others not, best way is
        //       to set the specular level on that light to 0, and the specular color to 0,0,0,1
        // NOTE: This MUST be re-enabled after 2d draw calls if you intend to draw more 3d calls
        //       before rendering to screen. This is a known TV3D issue.
        // http://www.truevision3d.com/forums/bugs/resolved_actorrender_after_draw2daction_makes_specular_disappear-t17081.0.html
        public static void SetSpecularLighting(bool enable)
        {
            CoreClient._CoreClient.Light.SetSpecularLighting(enable);
        }

        #endregion

        /// <summary>
        /// TODO: need more constructors for various diffuse, ambient, specular, emissive settings, etc
        /// </summary>
        /// <param name="id"></param>
        /// <param name="direction"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        /// <param name="specularlevel"></param>
        /// <param name="indoorLight"></param>
        public Light(string id, Vector3d direction, float r, float g, float b, bool indoorLight)
            : this(id)
        {
            // Note that although Direct3D uses RGBA values for lights, the alpha color component is not used. 
            Diffuse = new Color(r, g, b, 1f);
             Direction = Vector3d.Normalize(direction);
            _isIndoorLight = indoorLight;
        }

        protected Light(string id)
            : base(id)
        {

            mEntityFlags |= EntityAttributes.Light;
            Diffuse = new Color (1f, 1f, 1f, 1f);
            Ambient = new Color(1f, 1f, 1f, 1f); // defaults

            // specular is off by default because it's expensive (relatively speaking) and we only enable it if specifically set in a call
            // but frankly specular is cool and is what makes our metal ships look good.
            SpecularLightingEnabled = true;
            Specular = new Color(1f, 1f, 1f, 0f);
            _specularLevel = 1f;

            SetEntityAttributesValue((uint)EntityAttributes.VisibleInGame, true);
            SetEntityAttributesValue((uint)EntityAttributes.PickableInGame, false);
            SetEntityAttributesValue((uint)EntityAttributes.Dynamic, false);
        }
        
        
        #region ITraverser Members
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
            Settings.PropertySpec[] properties = new Settings.PropertySpec[13 + tmp.Length];
            tmp.CopyTo(properties, 13);
            

            properties[0] = new Settings.PropertySpec("direction", _direction.GetType().Name);
            properties[1] = new Settings.PropertySpec("range", _range.GetType().Name);
            properties[2] = new Settings.PropertySpec("diffuse", _diffuse.GetType().Name);
            properties[3] = new Settings.PropertySpec("ambient", _ambient.GetType().Name);
            properties[4] = new Settings.PropertySpec("specular", _specular.GetType().Name);
            properties[5] = new Settings.PropertySpec("specularlevel", _specularLevel.GetType().Name);
            properties[6] = new Settings.PropertySpec("specularenabled", _specularEnabled.GetType().Name);
            properties[7] = new Settings.PropertySpec("indoorlight", _isIndoorLight.GetType().Name);
            properties[8] = new Settings.PropertySpec("useforlightmapping", _useforLightMapping.GetType().Name);
            properties[9] = new Settings.PropertySpec("castshadows", _castShadows.GetType().Name);
            properties[10] = new Settings.PropertySpec("managed", _managedLight.GetType().Name);
            properties[11] = new Settings.PropertySpec("bumpusespecular", _bumplightUseSpecular.GetType().Name);
            properties[12] = new Settings.PropertySpec("bumpapproxpointlightbydir", _bumplightApproximatePointLightbyDirLight.GetType().Name);


            if (!specOnly)
            {
                properties[0].DefaultValue = _direction;
                properties[1].DefaultValue = _range;
                properties[2].DefaultValue = _diffuse;
                properties[3].DefaultValue = _ambient;
                properties[4].DefaultValue = _specular;
                properties[5].DefaultValue = _specularLevel;
                properties[6].DefaultValue = _specularEnabled;
                properties[7].DefaultValue = _isIndoorLight;
                properties[8].DefaultValue = _useforLightMapping;
                properties[9].DefaultValue = _castShadows;
                properties[10].DefaultValue = _managedLight;
                properties[11].DefaultValue = _bumplightUseSpecular;
                properties[12].DefaultValue = _bumplightApproximatePointLightbyDirLight;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "direction":
                        Direction =(Vector3d)properties[i].DefaultValue;
                        break;
                    case "range":
                        Range = (float)properties[i].DefaultValue;
                        break;
                    case "diffuse":
                        Diffuse = (Color)properties[i].DefaultValue;
                        break;
                    case "ambient":
                        Ambient = (Color)properties[i].DefaultValue;
                        break;
                    case "specular":
                        Specular = (Color)properties[i].DefaultValue;
                        break;
                    case "indoorlight":
                        _isIndoorLight = (bool)properties[i].DefaultValue;
                        break;
                    case "useforlightmapping":
                        _useforLightMapping = (bool)properties[i].DefaultValue;
                        break;
                    case "castshadows":
                        _castShadows = (bool)properties[i].DefaultValue;
                        break;
                    case "managed":
                        _managedLight = true;// TEMP HACK - (bool)properties[i].DefaultValue;
                        break;
                    case "specularenabled":
                        _specularEnabled = (bool)properties[i].DefaultValue;
                        break;
                    case "bumpusespecular":
                        _bumplightUseSpecular = (bool)properties[i].DefaultValue;
                        break;
                    case "bumpapproxpointlightbydir":
                        _bumplightApproximatePointLightbyDirLight = (bool)properties[i].DefaultValue;
                        break;
                }
            }
        }
        #endregion

        #region IPageableTVNode Members
         public override int TVIndex
        {
            get { return _tvfactoryIndex; }
        }

        public override bool TVResourceIsLoaded
        {
            get { return _tvfactoryIndex > -1; }
        }

        public override string ResourcePath
        {
        	get { return null;}
        }

        public override void LoadTVResource()
        {
           // base.LoadTVResource();
           // NOTE: June.9.2017 - It's important that all enabled lights start off as 
           //                     Active = false, then during draw traversal, we Activate=true 
           //                     only those lights that are influencing the culled scene.
            Active = false;
        }
        #endregion

        ///<summary>
        ///Note that although Direct3D uses RGBA values for lights, the alpha color component is not used. 
        ///</summary>
        public Color Diffuse
        {
            get { return _diffuse; }
            set
            {
                _diffuse = value;
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.SetLightDiffuseColor(_tvfactoryIndex, value.r, value.g, value.b);
            }
        }

        public Color Ambient
        {
            get { return _ambient; }
            set
            {
                _ambient = value;
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.SetLightAmbientColor(_tvfactoryIndex, value.r, value.g, value.b);
                
                SetGlobalAmbient (value.r, value.g, value.b);
                
              
            }
        }

        // NOTE: if we want some lights to use specular and others not, best way is
        //       to set the specular level on that light to 0, and the specular color to 0,0,0,1
        // NOTE: This MUST be re-enabled after 2d draw calls if you intend to draw more 3d calls
        //       before rendering to screen. This is a known TV3D issue.
        // http://www.truevision3d.com/forums/bugs/resolved_actorrender_after_draw2daction_makes_specular_disappear-t17081.0.html
        public bool SpecularLightingEnabled 
        {
            get { return _specularEnabled; }
            set
            {
                _specularEnabled = value;
                SetSpecularLighting(_specularEnabled);
            }
        }

        /// <summary>
        /// Required for Material.Specular and Material.SpecularPower to be enabled on geometry.
        /// </summary>
        public Color Specular
        {
            get { return _specular; }
            set
            {
                _specular = value;
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.SetLightSpecularColor(_tvfactoryIndex, value.r, value.g, value.b);
            }
        }

        /// <summary>
        /// Active sets whether a light is on/off during render of part of scene. 
        /// A light should be set to Active = true when it is visible and casts on
        /// the geometry we're about to render.
        /// </summary>
        public bool Active
        {
            get { return mActive; }
            set
            {
               
                mActive = value;

                if (TVResourceIsLoaded)
                {
                    if (Enable)
                        CoreClient._CoreClient.Light.EnableLight(_tvfactoryIndex, mActive);
                    else
                        // cannot make Active a disabled light
                        CoreClient._CoreClient.Light.EnableLight(_tvfactoryIndex, false);
                }   
            }
        }

        /// <summary>
        /// Enable for Lights must only be global enable/disable of the entity itself and NOT
        /// for toggling visible lights on/off
        /// </summary>
        public override bool Enable
        {
            set
            {
                base.Enable = value;
                // only enable if it's loaded
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.EnableLight(_tvfactoryIndex, value);
            }
        }

        public bool IsIndoorLight
        {
            get { return _isIndoorLight; }
            set { _isIndoorLight = value; }
        }

        public bool UseForLightMapping
        {
            get { return _useforLightMapping; }
            set
            {
                _useforLightMapping = value;
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.SetLightProperties(_tvfactoryIndex, TV_MANAGES_LIGHTS, _castShadows, _useforLightMapping);
            }
        }

        //TODO: get rid of this? i dont think ill ever really use stencils.  And i determine if a light
        // is used to cast shadows by passing it to the shadowmappers
        public bool CastShadows
        {
            get { return _castShadows; }
            set
            {
                _castShadows = value;
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.SetLightProperties(_tvfactoryIndex, TV_MANAGES_LIGHTS, _castShadows, _useforLightMapping);
            }
        }

        public bool ManagedLight
        {
            get { return _managedLight; }
            set
            {
                _managedLight = value;
                if (TVResourceIsLoaded)
                    CoreClient._CoreClient.Light.SetLightProperties(_tvfactoryIndex, TV_MANAGES_LIGHTS, _castShadows, _useforLightMapping);
            }
        }


        public bool BumpLighting_UseSpecular
        {
            get { return _bumplightUseSpecular; }
            set
            {
                _bumplightUseSpecular = value;
                if (TVResourceIsLoaded)
                //TODO: once you call this, how do you disable it? I guess its like the regular SetLightProperties
                // its just the individual aspects you can enable / disable (like shadows and usefor lightmapping, etc)
                    CoreClient._CoreClient.Light.SetBumpLightProperties(_tvfactoryIndex, _bumplightUseSpecular,
                                                        _bumplightApproximatePointLightbyDirLight);
            }
        }

        public bool BumpLighting_ApproximatePointLightbyDirLight
        {
            get { return _bumplightApproximatePointLightbyDirLight; }
            set
            {
                _bumplightApproximatePointLightbyDirLight = value;
                if (TVResourceIsLoaded)
                //TODO: once you call this, how do you disable it? I guess its like the regular SetLightProperties
                // its just the individual aspects you can enable / disable (like shadows and usefor lightmapping, etc)
                    CoreClient._CoreClient.Light.SetBumpLightProperties(_tvfactoryIndex, _bumplightUseSpecular,
                                                        _bumplightApproximatePointLightbyDirLight);
            }
        }

        public void SetBumpLightProperties(bool specular, bool approximatePointLightbyDirLight)
        {
            if (TVResourceIsLoaded)
                CoreClient._CoreClient.Light.SetBumpLightProperties(_tvfactoryIndex, specular, approximatePointLightbyDirLight);
        }

        
        //public static void SetProjectiveShadowsProperties()
        //{
        //    Core._CoreClient.Light.SetProjectiveShadowsProperties( CONST_TV_LIGHTSHADOW.TV_LIGHTSHADOW_SHADOWMAP_GLOBAL );
        //}

        public void SetCubeTextureMap(int cubeTextureMapID)
        {
            if (TVResourceIsLoaded)
                CoreClient._CoreClient.Light.SetLightCubeMap(_tvfactoryIndex, cubeTextureMapID);
        }


        // Directional Lights and Spot lights make use of Direction but only spot lights
        // will need a proper rotation to be computed so that if the spotlight is a child of some hierarchical entity
        // (eg inside of a ship on a space marine's weapon flashlight) then the light will properly be rotated with respect
        // to that hierarchy.  Thus even this "Direction" must be thought of as a relative direction, not absolute even though
        // the actual TV3D Call results in an absolute light direction. (actually ill just have to test this i guess)
        // Caluclating a rotation matrix from direction
        //http://www.gamedev.net/page/resources/_/reference/programming/140/wwh-series/wwh---calculating-a-rotation-matrix-based-on-locationtarget-r665
        public Vector3d Direction
        {
            get { return _direction; }
            set
            {
                //(NOTE: pointlights dont have a direction)

                _direction = value;
                _direction.Normalize();

                if (TVResourceIsLoaded)
                {
                    // NOTE: June.8.2017 - if .SetLightDirection crashes, make sure only 1 directional light is enabled.   
                    //       NOTE: we can't update Direction in Update() it must be in Render() loop because it must be executed right away
                    //       because we are using a DirectionalLight to mimic a PointLight for rendering our Worlds and that means being able
                    //       to change the direction of a light for each World and ship as we render.
                    if (_direction != Vector3d.Zero())
                        // NOTE: if _direction is Vector3d.Zero() an error gets outputed to TVDebug.log
                        CoreClient._CoreClient.Light.SetLightDirection(_tvfactoryIndex, (float)_direction.x, (float)_direction.y, (float)_direction.z);
                        
                    //CoreClient._CoreClient.Light.GetLight(_tvfactoryIndex, ref mInfo);
                    //mInfo.direction = Helpers.TVTypeConverter.ToTVVector(_direction);
                    //CoreClient._CoreClient.Light.SetLight(_tvfactoryIndex, ref mInfo);
                }
            }
        }

        public double RangeSquared { get { return _rangeSquared; } }
        public float Range
        {
            get { return _range; }
            set
            {
                // directional lights have no range
                _range = value;
                _rangeSquared = _range * _range;
                _maxVisibleDistanceSq = _rangeSquared;
                // For directional lights a bounding box is still used to determine visibility of the light source.
                // TODO: maybe want to override this in the other types then and not do this here
                SetChangeFlags(Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Self);
                if (TVResourceIsLoaded ) 
                    CoreClient._CoreClient.Light.SetLightRange(_tvfactoryIndex, _range);
            }
        }

        public override PickResults Collide(Vector3d start, Vector3d end, KeyCommon.Traversal.PickParameters parameters)
        { 
        	throw new NotImplementedException("Light.Collide() - This Entity type can be special cased in the Traverser and following code should exist there instead.");
        	// (see as example Picker.cs - public object Apply(Light light, object data) {} )
        }

        public virtual void Update(double elapsedSeconds)
        {
            throw new NotImplementedException("Light.Update() - Need to apply changes to Lights during Update and not immediately as currently in property setters!!");

            if (TVResourceIsLoaded && mChanged == true)
            {
                CoreClient._CoreClient.Light.SetLightRange(_tvfactoryIndex, _range);
            }
        }

        private TV_LIGHT mInfo;
        public void SetCameraSpaceTranslationForRendering(Vector3d cameraSpacePosition)
        {
            // We do not store this position permanently.  This is only used during rendering
            // to place the light at the camera space position.  It must therefore be done
            // every frame for every light.
               
                // TODO: Could the "attempting to read or write to protected memory" exceptionally
                // occassionally thrown on .SetLightPosition be
                // about a light that is paged in or created during universe generation
                // and is then removed but yet we still try to translate it prior to 
                // removing htis node or something or what the fuck?
                // UPDATE Nov.27.2012: I think hopefully the above described exception bug has been fixed by
                //         Pager.cs moving of the closing brace on the lock(obj) {} to include
                //         the pageableNode.LoadTVResource() method call.  So far no more light
                //         translation issues.
                // UPDATE: Nov.29.2012: Nope, not fixed.  
                // UPDATE: Dec.3.2012: Using TVLightEngine.SetLight() instead of .SetLightPosition() seems to resolve issue
                if (TVResourceIsLoaded == false) return; 

                int activeCount = CoreClient._CoreClient.Light.GetActiveCount();
                int lightCount = CoreClient._CoreClient.Light.GetCount();

                CoreClient._CoreClient.Light.GetLight(_tvfactoryIndex, ref mInfo);
                bool active = CoreClient._CoreClient.Light.IsLightActive(_tvfactoryIndex);
                bool enabled = CoreClient._CoreClient.Light.IsLightEnabled(_tvfactoryIndex);
                System.Diagnostics.Trace.Assert(active == enabled == true);
                // Setting a position on a directional light seems to break the light causing black mesh rendering.
                if (this is DirectionalLight == false)
                    try
                    {
                     	// NOTE: For some reason, calling Light.SetLightPosition() causes AccessViolation in TV 
                		//       but using Light.SetLight with a TV_LIGHT struct where we assign position vector to it
                		//       works fine.  No idea why.
                        // June.9.2017 - is this fixed now so that we can use .SetLightPosition instead of .SetLight(index, ref mInfo)?    
                    //System.Diagnostics.Debug.WriteLine("Light.SetCameraSpaceTranslationForRendering() - Light index " + _tvfactoryIndex.ToString());
                        CoreClient._CoreClient.Light.SetLightPosition(_tvfactoryIndex, (float)cameraSpacePosition.x, (float)cameraSpacePosition.y, (float)cameraSpacePosition.z);
              //          CoreClient._CoreClient.Light.SetLightPosition(_tvfactoryIndex, 0f, 0f, 0f);
                        //mInfo.position.x = (float)cameraSpacePosition.x;
                        //mInfo.position.y = (float)cameraSpacePosition.y;
                        //mInfo.position.z = (float)cameraSpacePosition.z;
             //           mInfo.position = Helpers.TVTypeConverter.ToTVVector(cameraSpacePosition);
             //           CoreClient._CoreClient.Light.SetLight(_tvfactoryIndex, ref mInfo);
                        //CoreClient._CoreClient.Light.GetLight(_tvfactoryIndex, ref mInfo);
                        //System.Diagnostics.Debug.Assert(mInfo.position.Equals(Helpers.TVTypeConverter.ToTVVector(cameraSpacePosition)));
              //          CoreClient._CoreClient.Light.SetIndex(_tvfactoryIndex); // what is this for?
                    }
                    catch (Exception ex)
                    {
                        CoreClient._CoreClient.Engine.AddToLog("Light.SetCameraSpaceTranslationForRendering() - light index = " + _tvfactoryIndex.ToString() + " - " + ex.Message);
                    }
                // create a world position matrix for the point light 
                //_matrix.M41 = (float) _translation.x;
                //_matrix.M42 = (float) _translation.y;
                //_matrix.M43 = (float) _translation.z;
                // For directional lights a bounding box is irrelevant because there is no position or range.
                // everything (unless the light is disabled (i.e. sunlight) such as when you step indoors) is affected
                // no matter where they are placed.  TODO: maybe want to override this in the other types then and not do this here

                
                // TODO: Obsolete for now - we dynamically determine which geometry is affected by ligths
                // during cull traversal and prior to Drawer
                //foreach (IObserver s in _subscribers)
                //{
                //    s.HandleUpdate(this); // TODO: i barely remember this IObserver usage for lights?  hrm... what to do with this?
                //}
        }

        #region IDisposable Members
        // TODO: this isnt really a resource at all right? Entities are supposed to be unshareable.  Hrm... but it is a node in that
        // different nodes can attach to different nodes.  So that's probably necessary.
        protected override void DisposeUnmanagedResources()
        {
            if (TVResourceIsLoaded)
            {
                CoreClient._CoreClient.Light.EnableLight(_tvfactoryIndex, false);
                CoreClient._CoreClient.Light.DeleteLight(_tvfactoryIndex);
                //System.Diagnostics.Debug.WriteLine("Light.DisposeUnmanagedResources() - Light Disposed at index " + _tvfactoryIndex.ToString());
                _tvfactoryIndex = -1;
            }
            base.DisposeUnmanagedResources();
        }
        #endregion

        #region IComparable<LightingData> Members
        public int CompareTo(Light other)
        {
            return _tvfactoryIndex.CompareTo(other.TVIndex);
        }
        #endregion

        #region IBoundVolume members
        protected override void UpdateBoundVolume()
        {
            // box is created in model space then transformed by the RegionMatrix
            #if DEBUG
            if (_range == 0) System.Diagnostics.Debug.WriteLine ("Light.UpdateBoundVolume() - WARNING: LIGHT RANGE == 0.");
            #endif
            mBox = new BoundingBox(Vector3d.Zero(), _range);
            mBox = BoundingBox.Transform(mBox, RegionMatrix);
            _maxVisibleDistanceSq = _rangeSquared;
            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly | Keystone.Enums.ChangeStates.BoundingBoxDirty);

        }
        #endregion

    }
}