using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Elements;
using Keystone.Enum;
using Keystone.FX;
using Keystone.IO;
using Keystone.Portals;
using Keystone.Resource;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Collision;
using Keystone.Extensions;
using KeyScript.Routes;
using KeyCommon.Flags;

namespace Keystone.Entities
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// IPageableTVNode is for loading our Entity Script, but is also useful for loading Region's which inherit Entity
    /// </remarks>
    public abstract class Entity : BoundTransformGroup, IFXSubscriber, IPageableTVNode
    {
        protected Scene.Scene mScene; // a reference to the scene this entity is part of
        protected SceneNode mSceneNode;
        
        protected DomainObjects.DomainObject mScript;
        // IPageablve vars - script objects are main data thats pageable, although Terrain Entities have heightfield data
        protected string mScriptResource;

        protected Animation.AnimationController mAnimationController;
        protected Animation.Animation[] mAnimations;
        protected Behavior.Behavior mBehaviorRoot;
        protected KeyCommon.Traversal.BehaviorContext mBehaviorContext; // holds state vars related to traversing the behavior tree. Currently not used anywhere. Seems viewpoint behavviors are just using mBlackboardData but... hmm..

        protected Dictionary<string, object> mUserStructs; // See HelloBoids.cs for help with finishing this implementation     
        // todo: do we need a reference to GameObject or Character? I don't want there to be a dependancy on Game01.dll or KeyScript or even the Exe so it might need to just be a generic "object"
        protected KeyCommon.Data.UserData mBlackboardData; // todo: allow exe to add an object from game01.dll.  can be stored with key named "npc" and then cast in the script to (Game01.GameObjects.Crew)npc; This data then is stored in a database unlike the mCustomPropertyValues
                                                       // todo: this probably means the Plugins need to be able to grab this object too.
                                                       // todo: celestial body data should also be stored as an object in mCustomData

        Dictionary<string, object> mCustomPropertyValues; // NOTE: the custom properties themselves are hosted in the Script.  They are created during Script.Initialize() within the scripts themselves. This var only holds the values.
        private string mPersistedCustomPropertyValues;

        protected KeyCommon.Flags.EntityAttributes mEntityFlags;
        protected string mReferencedEntityID; // id of the entity this entity references when using as proxy
                                              // can mRefID also be useful when having a bunch of entities that are meant
                                              // to be defined by a single entity in the xmldb such that when it is changed
                                              // anywhere from within it's hiearchy, they are changed too?

        protected uint mUserTypeID; // This value is assigned via the EntityEditPlugin General tab.  mHost.Entity_GetUserTypeIDsToString() 
                                    // is used to populate the combobox with string versions of the enum Game01.Enums.COMPONENT_TYPE
                                    // keystone.dll agnostic value that every app/game and their scripts can assign to differentiate between entity sub-types (eg engines vs sensors vs power gen)
                                    // different games built on KGB can have
                                    // different sets of flags for it's components
        protected uint _customFlags;  // these are flags scripts can define and interpret freely
                                      // TODO: i think the following should be moved to ModeledEntities 
        private const ushort DEFAULT_RENDER_PRIORITY = 128; // using a default value > 0 allows room to temporarily decrease.
        //  a render priority rather than always be forced to raise 
        // the priority of other entities
        protected ushort mRenderPriority = DEFAULT_RENDER_PRIORITY; // TODO: we are not saving this to xml, should we be?
        protected double _maxVisibleDistanceSq = -1;

        protected CellFootprint mFootprint;
        protected Vector3i mTileLocation; // if parent is celledRegion then this value will contain the x,y,z offsets
                                          // for the footprint center.  TODO: I don't think this value is being used anywhere including scripts. hrm.

        protected readonly object mSyncRoot;
        protected PageableNodeStatus _resourceStatus;

        protected Entity(string id)
            : base(id)
        {
            mSyncRoot = new object(); // for loading script and in case of Terrains, loading heightfield

            // all entities are unique and cannot be shared thus it's ok to NOT have a static constructor for them.
            // because there will never be a need to search for an existing and then call IncrementRef on it.
            Shareable = false; // entities can never be shared, only copied
            Pickable = true;
        }
        
        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            throw new Exception("Entity.Traverse() - This method must be overriden by derived types.");
        }
        #endregion 

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }

        #region ResourceBase members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {

            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[7 + tmp.Length];
            tmp.CopyTo(properties, 7);

            properties[0] = new Settings.PropertySpec("entityflags", typeof(uint).Name);
            properties[1] = new Settings.PropertySpec("attachedtoboneid", typeof(int).Name);
            properties[2] = new Settings.PropertySpec("tilelocation", typeof(Vector3i).Name);
            properties[3] = new Settings.PropertySpec("scriptresource", typeof(string).Name);
            properties[4] = new Settings.PropertySpec("usertypeid", typeof(uint).Name);
            // custom property values that are unique to each entity
            // note: custom property names/types/default values are defined in our DomainObjectScript node so
            // until that script is loaded, we cant seperate them out into seperate mCustomProperties
            // so instead we store them as a single xml attribute "customproperties" with the attribute
            // value consisting of a single comma seperate value list 
            properties[5] = new Settings.PropertySpec("customproperties", typeof(string).Name);
            properties[6] = new Settings.PropertySpec("footprint", typeof(string).Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = (uint)mEntityFlags;
                properties[1].DefaultValue = AttachedToBoneID;
                properties[2].DefaultValue = mTileLocation;
                properties[3].DefaultValue = mScriptResource;
                properties[4].DefaultValue = mUserTypeID;
                //if (this is Vehicles.Vehicle)
                //    System.Diagnostics.Debug.WriteLine("Entity.GetProperties() - custom properties ");
                properties[5].DefaultValue = mPersistedCustomPropertyValues; // we need to cache the result because on operations lile node.Clone() 

                if (mFootprint == null)
                    properties[6].DefaultValue = null;
                else
                    properties[6].DefaultValue = mFootprint.ID;
            }

            return properties;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// SetProperties does not need to do validation or rules checking because these properties
        /// are not game specific.  These are engine properties predominaly.  Furthermore,
        /// translation, scale and rotation are not modified here for real-time purposes. The physics engine
        /// and simulation engine does that through direct internal accessors.  This is also good for performance.
        /// Thus, change events that need to be raised in script during entity transforms will be done directly from those
        /// accessors.         
        /// </remarks>
        /// <param name="properties"></param>
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
                    case "usertypeid":
                        mUserTypeID = (uint)properties[i].DefaultValue;
                        break;
                    case "entityflags":
                        mEntityFlags = (EntityAttributes)(uint)properties[i].DefaultValue;
                        break;
                    case "customproperties":
                        string temp = (string)properties[i].DefaultValue;
                        mPersistedCustomPropertyValues = temp;
                        break;
                    case "scriptresource":
                        mScriptResource = (string)properties[i].DefaultValue;
                        // TODO: should i force load script here?  problem is... LoadTVResource() might load
                        // different things on a entity....
                        break;
                    case "attachedtoboneid":
                        AttachedToBoneID = (int)properties[i].DefaultValue;
                        break;
                    case "tilelocation":
                        mTileLocation = (Vector3i)properties[i].DefaultValue;
                        break;
                    case "footprint":
                        string footprintID = (string)properties[i].DefaultValue;
                        //if the footprintID is null, we can delete the footprint since it's null
                        if (string.IsNullOrEmpty(footprintID))
                        {
                            if (mFootprint != null)
                                RemoveChild(mFootprint);
                        }
                        else
                        {
                            if (mFootprint != null)
                            {
                                if (mFootprint.ID == footprintID)
                                    continue;

                                // footprint resource needs to be changed
                                RemoveChild(mFootprint);
                            }
                            try
                            {
                                // attemp to decode the new footprint
                                CellFootprint fp = CellFootprint.Create(footprintID);
                                if (fp.Data == null)
                                    throw new Exception("Entity.SetProperties() - Error decoding footprint");

                                AddChild(fp);
                            }
                            catch (Exception ex)
                            {
                                // footprint decode fails, catch exception but do nothing. 
                                // mFootprint is already null by the time we've reached this
                                System.Diagnostics.Debug.WriteLine("Entity.SetProperties() - Footprint error: " + ex.Message);
                            }
                        }
                        break;
                }
            }
        }

        public virtual bool GetEntityAttributesValue(uint flag)
        {
            EntityAttributes f = (EntityAttributes)flag;

            return (mEntityFlags & f) != 0;
        }

        public virtual bool GetEntityFlagValue(string flagName)
        {
            switch (flagName)
            {
                case "visible":
                    return (mEntityFlags & EntityAttributes.VisibleInGame) == EntityAttributes.VisibleInGame;
                case "pickable":
                    return (mEntityFlags & EntityAttributes.PickableInGame) == EntityAttributes.PickableInGame;
                case "collidable":
                    return (mEntityFlags & EntityAttributes.CollisionEnabled) == EntityAttributes.CollisionEnabled;
                case "dynamic":
                    return (mEntityFlags & EntityAttributes.Dynamic) == EntityAttributes.Dynamic;
                case "awake":
                    return (mEntityFlags & EntityAttributes.Awake) == EntityAttributes.Awake;
                case "overlay":
                    return (mEntityFlags & EntityAttributes.Overlay) == EntityAttributes.Overlay;

                case "laterender":
                    return (mEntityFlags & EntityAttributes.LateRender) == EntityAttributes.LateRender;
                case "autogenfootprint":
                    return (mEntityFlags & EntityAttributes.AutoGenerateFootprint) == EntityAttributes.AutoGenerateFootprint;
                case "terrain":
                    return (mEntityFlags & EntityAttributes.Terrain) == EntityAttributes.Terrain;
                case "hasviewpoint":
                    return (mEntityFlags & EntityAttributes.HasViewpoint) == EntityAttributes.HasViewpoint;
                case "playercontrolled":
                    return (mEntityFlags & EntityAttributes.PlayerControlled) == EntityAttributes.PlayerControlled;
                case "destroyed":
                    return (mEntityFlags & EntityAttributes.Destroyed) == EntityAttributes.Destroyed;
                default:
#if DEBUG
                    throw new ArgumentOutOfRangeException("Entity flag '" + flagName + "' is undefined.");
#endif
                    return false;
                    break;
            }
        }

        public virtual void SetEntityAttributesValue(uint flag, bool value)
        {
            EntityAttributes f = (EntityAttributes)flag;
            //throw new ArgumentOutOfRangeException("Entity flag '" + flag.ToString() + "' is undefined.");
            if (value)
                mEntityFlags |= f;
            else
                mEntityFlags &= ~f;
        }

        public virtual void SetEntityFlagValue(string flagName, bool value)
        {
            switch (flagName)
            {
                case "visible":
                    SetEntityAttributesValue((uint)EntityAttributes.VisibleInGame, value);
                    break;
                case "pickable":
                    SetEntityAttributesValue((uint)EntityAttributes.PickableInGame, value);
                    break;
                case "collidable":
                    SetEntityAttributesValue((uint)EntityAttributes.CollisionEnabled, value);
                    break;
                case "dynamic":
                    SetEntityAttributesValue((uint)EntityAttributes.Dynamic, value);
                    break;
                case "awake":
                    SetEntityAttributesValue((uint)EntityAttributes.Awake, value);
                    break;
                case "overlay":
                    SetEntityAttributesValue((uint)EntityAttributes.Overlay, value);
                    break;

                case "autogenfootprint":
                    SetEntityAttributesValue((uint)EntityAttributes.AutoGenerateFootprint, value);
                    break;
                case "terrain":
                    SetEntityAttributesValue((uint)EntityAttributes.Terrain, value);
                    break;
                case "playercontrolled":
                    SetEntityAttributesValue((uint)EntityAttributes.PlayerControlled, value);
                    break;
                case "hasviewpoint":
                    SetEntityAttributesValue((uint)EntityAttributes.HasViewpoint, value);
                    break;
                case "destroyed":
                    SetEntityAttributesValue((uint)EntityAttributes.Destroyed, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Entity flag '" + flagName + "' is undefined.");
            }
        }
        #endregion

        #region GameObject properties and methods
        /// <summary>
        /// This value is assigned via the EntityEditPlugin General tab.  mHost.Entity_GetUserTypeIDsToString() is used
        /// to populate the combobox with string versions of the enum Game01.Enums.COMPONENT_TYPE
        /// </summary>
        /// <remarks>I don't know about this since it means to have a distinct userTypeID, every component needs its own script.  
        /// I should probably just use a customproperty to hold this value instead.  In fact the game01.Builders.BuildWeapon() 
        /// can probably be used to modify this custom property value
        /// </remarks>
        public uint UserTypeID
        {
            // Enum assign from game01.dll.Enums.COMPONENT_TYPE
            set { mUserTypeID = value; }
            get
            {
                return mUserTypeID;
            }
        }

        /// <summary>
        /// Blackboard data is used by Scripts to store runtime data that is needed during gameplay but does  not need to be serialized.
        /// It is also used by 
        /// </summary>
        public KeyCommon.Data.UserData BlackboardData
        {
            get
            {
                return mBlackboardData;
            }
            set
            {
                mBlackboardData = value;
            }
        }

        
		
		// See HelloBoids.cs for help with finishing this implementation     
		public void AddUserStruct(object memStore)
		{
			string genericTypeName = memStore.GetType().FullName;
			// our Memory<T>'s will look as follows:
			// 'System.Memory`1[[HelloBoids.Laser_Struct, nkj43iat.exe, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]'
			
			// if we want to parse out just the first string
			int start = genericTypeName.IndexOf("[[") + 2;
			int end = genericTypeName.IndexOf(",");
											  
		    genericTypeName = genericTypeName.Substring(start, end - start);
			
			// For Memory<T> just use the above, the below is  NOT what we want
        	// Remove the generic arity part (e.g., "`1")
        	//genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
			
			AddUserStruct (genericTypeName, memStore);
		}
		
		public void AddUserStruct(string typename, object memStore)
		{
			if (mUserStructs == null) mUserStructs = new Dictionary<string, object>();
			
			//Console.WriteLine ("EntityNode.AddUserStruct() - Adding User Struct '" + typename + "'");
			mUserStructs.Add(typename, memStore);
		}
		
		public object GetUserStruct (Type t)
		{
			string typename = t.FullName;
			
			return GetUserStruct( typename);
		}
		
		public object GetUserStruct(string typename)
		{
			//Console.WriteLine ("EntityNode.GetUserStruct '" + typename + "'");
			if (mUserStructs == null) return null;
			
			object result;
			if (mUserStructs.TryGetValue(typename, out result))
				return result;
				
			return null;
		}
        

        // TODO: changing the underlying Script must clear and re-init the mCustomPropertyValues collection
        public Settings.PropertySpec[] GetCustomProperties(bool specOnly)
        {
            // NOTE: some scripts of other Entities such as a tactical_station will try to get
            //       customproperty values from another Entity within it's hierarchy such as a Turret
            //       and if that Turret's script isn't loaded, the helm will still try to call it's GetCustomProperty()
            //       and it's best if we just return null in those cases rather than try to force load the script here
            //       and RestoreCustomPropertyValues().
            // NOTE2: Trying to RestoreCustomPropertyValues() during calls to GetCustomProperties() or GetCustomPropertyValue()
            //        can result in a race condition within RestoreCustomPropertyValues().  
            if (string.IsNullOrEmpty(mScriptResource)) return null;
            if (mScript == null) return null;
            //{
            //    // NOTE: we only load the script and not any Geometry associated with the Entity such as Interior floors and ceilings
            //    mScript = (DomainObjects.DomainObject)Keystone.Resource.Repository.Create(mScriptResource, "DomainObject");
            //    Keystone.IO.PagerBase.LoadTVResource(mScript, false);

            //    if (!mScript.TVResourceIsLoaded)
            //        throw new Exception("Entity.GetCustomProperties() - resource not loaded. '" + mScriptResource);

            //    RestoreCustomPropertyValuesFromPersistString();

            //    // NOTE: we dont AddChild() here because the Entity may not be connected to the Scene yet and AddChild() will call Execute ("InitializeEntity")
            //    //this.AddChild(mScript);
            //}

            Settings.PropertySpec[] specs = mScript.CustomProperties;

            if (specs == null) return null;

            if (!specOnly)
                for (int i = 0; i < specs.Length; i++)
                {
                    object defaultValue = specs[i].DefaultValue;
                    specs[i].DefaultValue = GetCustomPropertyValue(specs[i].Name);
                    if (specs[i].DefaultValue == null)
                        specs[i].DefaultValue = defaultValue;
                }
            return specs;
        }

        public object GetCustomPropertyValue(string customPropertyName)
        {
            ////////////////////////////////////////////////////////
            // March.2.2026 - MichaelOliveTree - using Memory<T> and ComponentStores to hold structs for Component data
            //               requires the below:
            // Using structs like Component and Weapon to store the values of those aspects of a Laser for example
            // requires we call Script.GetCustomPropertyValue (customPropetyName)
            // The script will know which interface to grab the "values" from in the store.Span[0].PROPERTY
            // NOTE: some customPropertyNames may not be related to the Component's at all, such as descriptions and name and such
            //       which are not needed for high performance computation so those can be stored in NON ComponentStore.cs variables
            //       within the UserData object.
            // NOTE: For bool valid = SetCustomPropertyValues(customPropertyName, value)   <-- VALIDATION CAN OCCUR HERE and there'd
            //       be no need for mScript.RulesValidate() 
 //           if (mScriptIsInitialized)
 //               return mScript.GetCustomPropertyValue (customPropertyName);
 //           else 
 //               return null;
//          //////////////////////////////////////////////////////


            // TODO: Uncomment the above and comment out all of the below
            if (mCustomPropertyValues == null)
            {
                if (!string.IsNullOrEmpty(mPersistedCustomPropertyValues))
                    RestoreCustomPropertyValuesFromPersistString();
                if (mCustomPropertyValues == null)
                {
                    System.Diagnostics.Debug.WriteLine("Entity.GetCustomPropertyValue() - ERROR: mCustomPropertyValues is NULL.  Cannot get '" + customPropertyName + "' value.");
                    return null; // script likely has not been instanced yet.
                }
            }
            object value = null;

            // NOTE: the below commented out code never executes nor would we even want it to since the script might
            //       not even be loaded.  If the script isn't loaded, we should just return null for any attempt to
            //       get a custompropertyvalue.
            //if (mCustomPropertyValues.Count == 0)
            //{
            //    // RestoreCustomPropertyValues assumes that Initialize() script has been run
            //    // because that is where the script first assigns the custom property specs to DomainObject
            //    RestoreCustomPropertyValuesFromPersistString();
            //    if (mCustomPropertyValues == null) return value;
            //}

            bool found = mCustomPropertyValues.TryGetValue(customPropertyName, out value);
            //System.Diagnostics.Debug.Assert(found == true);
            if (found == false)
                System.Diagnostics.Debug.WriteLine("Entity.GetCustomPropertyValue() - WARNING: Cannot find value for custom property '" + customPropertyName + "'");
            return value;
        }

        public void SetCustomPropertyValues(string[] propertyNames, object[] values, bool validateRules, bool raiseChangeEvent, out int[] brokenCodes)
        {
            System.Diagnostics.Debug.Assert(mScript != null, "Entity.SetCustomPropertyValues() - ERROR: DomainObject is null!");
            System.Diagnostics.Debug.Assert(mCustomPropertyValues != null);

            if (propertyNames == null || values == null) throw new ArgumentNullException();
            if (propertyNames.Length != values.Length) throw new ArgumentException();

            brokenCodes = null;

            if (validateRules)
            {
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    // NOTE: below assert is incorrect. Some custom properties we can flag as "not serializable" and so
                    //       when first instantiating the entity, it will not have the non serializable properties' values in
                    //       mCustomPropertyValues
                    //System.Diagnostics.Debug.Assert(mCustomPropertyValues.ContainsKey(propertyNames[i]));
                    // NOTE: if any rule fails for any property, we return and do NOT apply ANY of them
                    // TODO: server should also track validation fail frequency
                    bool result = false;

                    // TODO: ROUTES should be able to work similar to our rules
                    // rules are run using a delegate that was specified in the script
                    // that is why no script.Execute() has to be called
                    result = mScript.RulesValidate(this.ID, propertyNames[i], values[i], out brokenCodes);
                    // property will not be changed because rules validation failed
                    if (result == false)
                    {
                        System.Diagnostics.Debug.WriteLine("Entity.SetCustomPropertyValues() - Aborting all custom property value changes because property '" + propertyNames[i] + "' FAILED VALIDATION");
                        // continue;
                        return;
                    }
                }
            }

            // still here.  Validation of all properties succeeded.  Let's apply the values
            // TODO: when / how do i update any stats calcs?  Since here this apply doesnt actually
            // invoke any script code?   Yes we do want a "change".  yes we would like to set a sort
            // of dirty flag so that we can run an update when it's time to access changed properties
            // (eg when the script tries to access the properties on Update(), it can determine that
            // build flags have changed and so recompute build statistics) but then not have these
            // validated.
            for (int i = 0; i < propertyNames.Length; i++)
            {
                if (propertyNames[i] == "operator")
                    System.Diagnostics.Debug.WriteLine("operator changed");

                // TODO: we want to ensure we dont wind up with cyclic property changes when
                //       script changes one property after another has been changed which causes
                //       the OnCustomPropertyChanged to fire yet again.
                // change property and notify script that this property has changed
                // then script can set a flag based on the property or category of the property
                // SetCustomFlagValue (
                // so that on next update we can do a dirty update of component's state
                // and not certain parts of the components state if unnecessary.

                // NOTE: below assert is incorrect. Some custom properties we can flag as "not serializable" and so
                //       when first instantiating the entity, it will not have the non serializable properties' values in
                //       mCustomPropertyValues
                //System.Diagnostics.Debug.Assert(mCustomPropertyValues.ContainsKey(propertyNames[i]));
                // assign the validated value
                if (mCustomPropertyValues.ContainsKey(propertyNames[i]))
                    mCustomPropertyValues[propertyNames[i]] = values[i];
                else
                    mCustomPropertyValues.Add(propertyNames[i], values[i]);

                // route any changes to properties that have routes
                // Routes are automaticly tied to changes rather than having to script it.  
                // In effect, it allows us to add triggers without having to require a script be written
                RouteRaiseEvent(propertyNames[i], values[i]);

                // note: generally, if a script is modifying it's own entity's custom properties, no change event
                // need be raised.  But if the custom property values are being modified outside of the entity, then
                // the event does need to be raised.  This event helps us avoid polling custom property values for changes
                // that the script would need to respond to.
                // note: however, what the changeevent typically does is set a flag so that the final response
                // can be done in Update().  This way multiple changes to various properties will still only result in
                // one response per tick. But the script is responsible for setting the flags because we dont want
                // to hardcode them.
                if (raiseChangeEvent)
                {
                    // TODO: we need a seperate set of events for pure events that aren't connected to property value changes
                    //       This is needed for implementing Triggers.
                    // If there is NOT an explict propertychanged event for this property, continue to call the OnCustomPropertyChanged script method
                    if (!mScript.PropertyChangedEventRaise(this.ID, propertyNames[i]))
                    {
                        // todo: when plugin is sending command via EditorHost.cs to change the properties, raiseChangeEvent needs to be set to "true"
                        int index = mScript.GetCustomPropertyIndex(propertyNames[i]);
                        if (index < 0) throw new Exception("Entity.SetCustomPropertyValue() - Custom Property '" + propertyNames[i] + "' not found. Check spelling.");
                        mScript.Execute("OnCustomPropertyChanged", new object[] { this.ID, index });
                    }

                    // invoke handlers for subscribers of this entity's property if any
                    PublicPropertyChangedEventRaise(propertyNames[i]);
                }
            }
            mPersistedCustomPropertyValues = PersistCustomPropertyValuesToString();
        }

        // TODO: should SetProperties return list of broken rules?
        // or should we have to call GetBrokenRules()
        public void SetCustomPropertyValues(Settings.PropertySpec[] properties, bool validateRules, bool raiseChangeEvent, out int[] brokenCodes)
        {
            // TODO: THis will break when we try to assign waypoint when accidentally clicking
            // "GOTO" hud menu item for a Vehicle that has no DomainObject script assigned to it.
            brokenCodes = null;

            if (properties == null || properties.Length == 0) return;

            string[] propertyNames = new string[properties.Length];
            object[] values = new object[propertyNames.Length];

            for (int i = 0; i < propertyNames.Length; i++)
            {
                propertyNames[i] = properties[i].Name;
                values[i] = properties[i].DefaultValue;
            }

            SetCustomPropertyValues(propertyNames, values, validateRules, raiseChangeEvent, out brokenCodes);
        }


        public void SetCustomPropertyValue(string customPropertyName, object value, bool validateRules, bool raiseChangeEvent, out int[] brokenCode)
        {
            //if (customPropertyName == "contacts")
            //    System.Diagnostics.Debug.WriteLine("contacts changed");
            int[] dummy;
            SetCustomPropertyValues(new string[] { customPropertyName }, new object[] { value }, validateRules, raiseChangeEvent, out dummy);
            brokenCode = dummy;
        }


        // RestoreCustomPropertyValues assumes that Initialize() script has been run
        // because that is where the script first assigns the custom property specs to DomainObject
        // XML Persisted shader property values are parse from a single persist string
        // and stored into the mParameterValues dictionary.
        private void RestoreCustomPropertyValuesFromPersistString()
        {
            if (mScript != null && mScript.Script != null && mScript.TVResourceIsLoaded)
            {
                try
                {
                    // NOTE: the following call will fill with DEFAULTS if mPersistedCustomPropertyValues is null so the call MUST be made.
                    //       TODO: I think instead i should fill with default when the custom properties are first set...
                    // TODO: still hate this way of storing customm properties.  I need to use a blackboard style grid of data that i can perhaps index
                    //       from Entity so that pointers aren't needed similar to what im doing with AI Blackboard data.. and so all
                    //       custom Entity data can be in the same memory grid and hopefully batch operations can be run on their data quickly
                    //       and in parallel that way.
                    // TODO: KeyCommon.Data.UserData
                    //       - can i create a sort of high performance db that uses unsafe code and memory access for each record?  
                    // 		// (See E:\dev\_projects\_XNA\Mercury Particle Engine\ProjectMercury.Windows\Emitters\Emitter.cs.Update() method)
                    // but one thing it does which i think defeats the purpose somewhat perhaps is it creates a fixed pointer to the particle array rather than allocating it as pointer from start.  having to "fix" it seems like enough overhead to nullify any performance advantages
                    // and again
                    //         each Entity's custom data has reference to that record index and the offsets for each field can be app specific
                    //         and we know what type to cast the data too. need to run lots of tests with these sorts of in mem dbs and just arrays of objects and such
                    // TODO: i should make a seperate test project that tests performance there


                    if (mScript.CustomProperties == null || mScript.CustomProperties.Length == 0) return;

                    Settings.PropertySpec[] customProperties = mScript.CustomProperties;
                    mCustomPropertyValues = new Dictionary<string, object>();

                    if (string.IsNullOrEmpty(mPersistedCustomPropertyValues))
                    {
                        // load the default values
                        for (int i = 0; i < customProperties.Length; i++)
                        {
                            // NOTE: never check if "IsSerializable" here since the values wont be
                            //       serialized in the string in the first place, and we MUST have the
                            //       opportunity to assign the default value that was specified in the
                            //       entity script when first creating the propertspecs.
                            // if (properties[i].IsSerializable == false) continue;
                            mCustomPropertyValues.Add(customProperties[i].Name, customProperties[i].DefaultValue);
                        }
                    }
                    else
                    {
                        // load the persited values from the Entity's xml
                        // NOTE: It's guaranteed that if persisted values exist, they will be
                        // restored here by the time any DomainObject or DomainObjectScript loads.
                        // 
                        // with the script loaded, if any custom property values are cached
                        // then we must assign them to the mCustomPropertyValues dictionary
                        // TODO: if the script's crc32 doesnt match those of the custom properties
                        // then we must assume the script has changed and the current custom properties
                        // no longer apply...?
                        string[] values = mPersistedCustomPropertyValues.Split(keymath.ParseHelper.English.XMLAttributeNestedDelimiterChars, StringSplitOptions.None);

                        if (values.Length != customProperties.Length)
                        {
                            // WARNING: if the lengths of the customProperties and the values do not match
                            // then its most likely the customProperties defined in the script were changed.
                            // This means the values will not line up and when we go to align them in the for() loop 
                            // blow, we will be potentially assigning the wrong values to the wrong properties.

                            // we may be able to solve this issue when we implement the save\resume functionality of missions
                            // since we will start saving entity properties (including custom) to a different more streamlined
                            // ".save" that does not require the rest of the scene hierarchy for each Entity to be saved.

                            // this should not happen during production but may happen as we modify our
                            // scripts to have more properties than the previous saved version.
                            // we will throw an exception just to be safe
                            //throw new Exception("Keystone.Helpers.ExtensionMethods.ParseCustomPropertiesPersistString()");
                            System.Diagnostics.Debug.WriteLine("Entity.RestoreCustomPropertyValuesFromPersistString() - PROPERTY VALUES DO NOT MATCH CUSTOM PROPERTIES LENGTH - '" + this.Name + "' REVERTING TO DEFAULT VALUES!");
                            mPersistedCustomPropertyValues = null;
                            RestoreCustomPropertyValuesFromPersistString();
                            return;
                        }
                        for (int i = 0; i < customProperties.Length; i++)
                        {
                            // NOTE: never check if "IsSerializable" here since the values wont be
                            //       serialized in the string in the first place, and we MUST have the
                            //       opportunity to assign the default value that was specified in the
                            //       entity script when first creating the propertspecs.
                            // if (properties[i].IsSerializable == false) continue;
                            object value = KeyCommon.Helpers.ExtensionMethods.ReadXMLAttribute(customProperties[i].TypeName, values[i]);
                            if (value != null)
                                // use persist string value
                                mCustomPropertyValues.Add(customProperties[i].Name, value);
                            else
                                // use default value
                                mCustomPropertyValues.Add(customProperties[i].Name, customProperties[i].DefaultValue);
                        }
                    }

                    // assert that the custom properties in the script haven't changed since beginning of this method
                    System.Diagnostics.Debug.Assert(customProperties.Length == mScript.CustomProperties.Length);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Entity.RestoreCustomPropertyValuesFromPersistString() - ERROR: restoring custom property values: " + ex.Message);
                }
            }
        }

        private string PersistCustomPropertyValuesToString()
        {
            string result = null;

            // NOTE: In the fixed properties above, we know what type to cast to
            // but here, we don't.  This isn't a problem with xml, but potentially is
            // when it comes to sending over the wire where we want to restore based on
            // a typename or typeID.  
            Settings.PropertySpec[] specs = GetCustomProperties(false);

            if (specs == null || specs.Length == 0)
                return null;

            try
            {
                result = KeyCommon.Helpers.ExtensionMethods.CustomPropertyValuesToString(specs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Entity.PersistCustomPropertyValuesToString() - Failure persisting custom property values: " + ex.Message);
            }

            return result;
        }


        #region Events
        private Dictionary<string, KeyScript.Events.PropertyChangedEventSubscribers> mCustomPropertyChangedEventSubscribers;
        Dictionary<string, KeyScript.Events.EventSubscribers> mEvents;
        private Dictionary<string, KeyScript.Events.AnimationCompletedEventSubscriber> mAnimationFinishedEventSubscribers;

        long mLastTick;
        public long LastTick { get { return mLastTick; } set { mLastTick = value; } }

        private List<Entity> mCollisionList = new List<Entity>();
        private List<Entity> mTriggerAreas = new List<Entity>();
        /// <summary>
        /// Called from Simulation.cs when a physics collision is detected between this entity and entity2
        /// </summary>
        /// <param name="entity2"></param>
        public void OnCollisionDetected(Entity entity2)
        {
            // TODO: we want to be able to send messages when OnEnter and OnExit collisions in zones occur.
            //       we dont want duplicate trigger messages sent either.
            //       I think both the trigger zone entity and the npc entity need to reference each other
            // TODO: how do we treat (which are most likely errors) a situation where an npc collides with multiple zones?
            //       Or even, when zones collide with one another? (even though this situation should not be allowed)
            // TODO: what if a npc capsule collides with two trigger zones at same time such as two elevators side by side and the walk
            //       through middle where they overlap two zones?  Do we wait until the capsule center.x/z is solely in one zone?

            // is this a new collision event? (i.e these two bodies have not interacted in previous frames)
            // the trigger area could be colliding with ("containing") multiple npcs.
            // https://docs.unity3d.com/Manual/CollidersOverview.html
            // each body needs to track all current collisions
            //   and track when they leave an area.
            // todo: each body needs to maintain a list of entities it has collided with (without duplicates, easy enough), but how do we know when to remove those entities from the list?
            //       perhaps first of all, we need seperate lists for just collision and for "trigger" area collisions.  Because these are distinct differences.
            //       Then every movement update, we need to test if every listed entity is still in that area.

            // TODO: 1) start by tracking the collisions and only adding to collision list if the entity.Translation also is within the trigger area and not just the capsule collider of the npc 
            //       2) ignore "collisions" unless its an NPC entering a trigger area.
            //       3) only send exit events after call to entity.FinalizeMovement where we can determine if we've left a trigger volume.
            //       4) we dont track collisions otherwise.  In the future we can maybe do that, but not for v1.0 where we just need trigger areas working.
            //       5) What happens next when i want to use an elevator?  How does the pathing work?
            //       6) We need to know which is the Trigger Area and which is the NPC entity (because we don't send the same OnEnter/OnExit events to both entities do we?)


            // do we need to raise trigger events?  We don't do this with projectile collisions, only with trigger areas and NPCs. (eg a bullet or laser traveling through an area wouldn't raise an event)
            // Or actually, we do need to raise events for projectiles, but not OnTriggerEnter() but rather OnCollision()
            // Do we define materials to indicate which Body's can interact with the trigger area and which cant (eg. npcs can, bullets cannot)
            // is one of the bodies a trigger area? (Note: It should be impossible that both are trigger areas since trigger areas are static for version 1.0.)
            // note: a possible scenario of moving trigger area would be a field of view cone for npcs.  The view vector changes based on head rotation as well as when the npc moves

            // TODO: i think i need to resave the elevator prefab to have the Trigger Area in it. I need to add the trigger area via the plugin interface.
            //       Can i make it so it maps with the footprint upon adding the trigger box collider?

            if (this.IsTrigger() == entity2.IsTrigger() == true)
            {
                //Debug.WriteLine("Entity.OnCollisionDetected() - One (and only one) entity must be a trigger.");
                return;
            }

            if (this.IsTrigger())
            {
                // todo: if this is a trigger area, why are we not adding it to trigger area list?
                //       or why add it to the mCollisionList?  What are we accomplishing here?
                //       it should be "hosted" list of the entities (npcs) that are in the this.RigidBody.Collider
                if (!mCollisionList.Contains(entity2) && TriggerAreaContains(this, entity2.Translation))
                    mCollisionList.Add(entity2);

                // TODO: when are we removing items from the mCollisionList?  If we test moving NPCs in finalizeMovement,
                //       we can maybe remove them from the affected trigger areas.
                // notify this trigger, that an NPC has entered it's area

            }
            else if (entity2.IsTrigger())
            {
                if (TriggerAreaContains(entity2, this.Translation))
                {
                    // notify this entity's script that it has entered a trigger area (entity2)
                    if (!mTriggerAreas.Contains(entity2))
                    {
                        OnTriggerAreaEnter(entity2);
                    }
                }
            }

            // TODO: for version 1.0, we should simply ONLY allow for Trigger area collisions.  There will be 
            //       no bullets/lasers and combat/boarding parties within the interior of the ship. And our Connectivity based pathing prevents collision with obstacles.
            //       So this should make handling our collision / trigger lists and raising the 
            //       appropriate events easier.
            //       We might have lasers and explosions from exterior sim impact interior structure and components, but we'll deal with that when the time comes. :/


            // what about child/nested Entities?  Do those get their own collision event? They should if they
            // are initialized with jitter.  In Unity3d im sure they do.  the elevator enter/exit trigger zone is a child entity of the Elevator prefab right?  But 
            // do nested entities generate a FinalizeMovement() call?


            // todo: the collisions seem to occur as one actor crosses the boundaries of the trigger area.
            //       then when it is entirely within the trigger area, there are no more collision events.
            //       Similarly, when existing the trigger area, there are no collision events.  
            //       I could maybe track when we enter a "trigger area" and then track when we leave that specific "trigger area" and 
            //       raise a "ExitTriggerArea" event in the script.  
            //       This means that trigger areas should not overlap since we can only be in one trigger area
            //       at a time.  Also trigger areas must be static. (this means no trigger areas in v1.0 for npc vision cones for example)

        }

        private void OnTriggerAreaExit(Entity area)
        {
            this.Execute("OnTriggerAreaExit", new object[] { this.ID, area.ID });
            mTriggerAreas.Remove(area);
            area.mCollisionList.Remove(this);
        }

        private void OnTriggerAreaEnter(Entity area)
        {
            mTriggerAreas.Add(area);
            this.Execute("OnTriggerAreaEnter", new object[] { this.ID, area.ID });
        }

        private bool IsTrigger()
        {
            if (this.mBoxCollider != null)
                return this.mBoxCollider.Trigger;

            if (this.mSphereCollider != null)
                return this.mSphereCollider.Trigger;

            if (this.mCapsuleCollider != null)
                return this.mCapsuleCollider.Trigger;

            return false;
        }

        private void OnAnimationEvent(object sender, EventArgs args)
        {
            Animation.AnimationTrack track = (Animation.AnimationTrack)sender;
            Animation.AnimationController.AnimationEventArgs animationEventArgs = (Animation.AnimationController.AnimationEventArgs)args;

            System.Diagnostics.Debug.WriteLine("Entity.OnAnimationEvent() - " + track.Name);

            switch (track.State)
            {

                case Animation.AnimationTrack.TrackState.None:
                    break;
                case Animation.AnimationTrack.TrackState.Error:
                    break;
                case Animation.AnimationTrack.TrackState.Playing:
                    break;

                case Animation.AnimationTrack.TrackState.Finished:
                    System.Diagnostics.Debug.WriteLine("Entity.OnAnimationEvent() - Animation '" + track.Name + "' for Entity '" + animationEventArgs.EntityName + "' finished.");
                    AnimationFinishedEventRaise(track.Name);
                    break;
                case Animation.AnimationTrack.TrackState.Paused:
                    break;
                case Animation.AnimationTrack.TrackState.Stopped:
                    break;
            }
        }

        public void AnimationFinishedEventSubscribe(string animationName, Entities.Entity subscriber, KeyScript.Events.AnimatioCompletedEventDelegate eventHandler)
        {
            if (mAnimationFinishedEventSubscribers == null) mAnimationFinishedEventSubscribers = new Dictionary<string, KeyScript.Events.AnimationCompletedEventSubscriber>();

            KeyScript.Events.AnimationCompletedEventSubscriber subscribers;
            bool eventExists = mAnimationFinishedEventSubscribers.TryGetValue(animationName, out subscribers);

            if (eventExists)
            {
                subscribers.Add(subscriber.ID, eventHandler);
            }
            else
            {
                // check if this subscriber is already subscribed to this event for the specified eventGeneratorID
                subscribers = new KeyScript.Events.AnimationCompletedEventSubscriber();
                subscribers.Add(subscriber.ID, eventHandler);
            }

            mAnimationFinishedEventSubscribers[animationName] = subscribers;
        }

        private void AnimationFinishedEventRaise(string animationName)
        {
            // NOTE: the subscribers have wired explicit event handlers so they know which animation the event is for
            if (mAnimationFinishedEventSubscribers != null)
            {
                KeyScript.Events.AnimationCompletedEventSubscriber subscribers;
                if (mAnimationFinishedEventSubscribers.TryGetValue(animationName, out subscribers))
                {
                    if (subscribers.SubscriberIDs != null)
                        for (int j = 0; j < subscribers.SubscriberIDs.Length; j++)
                            subscribers.Handlers[j].Invoke(this.ID); 
                }
            }
        }

        public void PublicPropertyChangedEventSubscribe(string propertyName, Entities.Entity subscriber, KeyScript.Events.PropertyChangedEventDelegate eventHandler)
        {
            if (mCustomPropertyChangedEventSubscribers == null) mCustomPropertyChangedEventSubscribers = new Dictionary<string, KeyScript.Events.PropertyChangedEventSubscribers>();
            
            KeyScript.Events.PropertyChangedEventSubscribers subscribers;
            bool eventExists = mCustomPropertyChangedEventSubscribers.TryGetValue(propertyName, out subscribers);
            
            if (eventExists)
            {
                subscribers.Add(subscriber.ID, eventHandler);
            }
            else
            {
                // check if this subscriber is already subscribed to this event for the specified eventGeneratorID
                subscribers = new KeyScript.Events.PropertyChangedEventSubscribers();
                subscribers.Add(subscriber.ID, eventHandler);
            }

            mCustomPropertyChangedEventSubscribers[propertyName] = subscribers;
        }


        private void PublicPropertyChangedEventRaise(string propertyName)
        {
            // NOTE: the subscribers have wired explicit event handlers so they know which property the event is for
            if (mCustomPropertyChangedEventSubscribers != null)
            {
                KeyScript.Events.PropertyChangedEventSubscribers subscribers;
                if (mCustomPropertyChangedEventSubscribers.TryGetValue(propertyName, out subscribers))
                {
                    if (subscribers.SubscriberIDs != null)
                        for (int j = 0; j < subscribers.SubscriberIDs.Length; j++)
                            subscribers.Handlers[j].Invoke(this.ID); // TODO: should we pass the actual value of the property?
                }
            }
        }

        public void EventSubscribe(string eventName, Entities.Entity subscriber, KeyScript.Events.EventDelegate eventHandler)
        {
            KeyScript.Events.EventSubscribers subscribers;
            bool eventExists = mEvents.TryGetValue(eventName, out subscribers);
            
            if (eventExists == false)
            {
                throw new ArgumentException("Entity.EventSubscribe() - Event '" + eventName + "' does not exist in script '" + this.ID + "'");
            }
            else
            {
                // check if this subscriber is already subscribed to this event for the specified eventGeneratorID
                subscribers = new KeyScript.Events.EventSubscribers();
                subscribers.Add(subscriber.ID, eventHandler);
            }
        }


        public void EventRaise(string eventName)
        {
            if (mEvents == null) return;

            KeyScript.Events.EventSubscribers subscribers;
            if (mEvents.TryGetValue(eventName, out subscribers))
            {
                if (subscribers.SubscriberIDs != null)
                    for (int i = 0; i < subscribers.SubscriberIDs.Length; i++)
                        subscribers.Handlers[i].Invoke(this.ID, eventName, null); // TODO: should we pass the actual value of the property?
            }
        }
        #endregion

        /// <summary>
        /// Custom flag values are set (optionally) by the DomainObjectScript and are not saved in XML.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        public void SetCustomFlagValue(uint flag, bool value)
        {
            if (value)
                _customFlags |= flag;
            else
                _customFlags &= ~flag;
        }

        public bool GetCustomFlagValue(uint flag)
        {
            return ((_customFlags & flag) == flag);
        }



        protected Dictionary<string, RouteSet> mEventListeners;

        // TODO: AddRoute should be done through a NetMessage that is typically
        //       done via our editor gui when the routing of one field in one entity
        //       to a field in another entity is done.
        public void AddRoute(string propertyName, Route route)
        {
            if (mEventListeners == null) mEventListeners = new Dictionary<string, RouteSet>();

            route.Source.Entity = this;
            System.Diagnostics.Debug.Assert(route.Source.EntityID == this.ID);

            RouteSet set;
            bool setExists = mEventListeners.TryGetValue(propertyName, out set);

            if (setExists == false)
            {
                set = new RouteSet();
                mEventListeners.Add(propertyName, set);
            }

            set.AddRoute(route);
        }

        // TODO: how do we restore route entity target references when entities are
        //       added or removed?  could we tie it into the Repository somehow?
        //       can all "routes" be stored in some global handler?  i dont really like that
        //       though because the current way we automatically have the starting source
        //       and then can easily just find if a route exists locally in the mEventListeners
        //       Can the RouteEndpoints be watching perhaps?
        public void RouteRaiseEvent(string propertyName, object value)
        {
            if (mEventListeners == null) return;

            RouteSet routeSet;
            bool routeExists = mEventListeners.TryGetValue(propertyName, out routeSet);
            if (!routeExists)
                return;

            // get all routes for this property
            Route[] routes = routeSet.Routes;

            if (routes == null || routes.Length == 0)
                return;

            // execute the routing from source to target
            for (int i = 0; i < routes.Length; i++)
            {
                // TODO: here we need to use direct references to entities
                // but they should be stored where?  not in the Route itself
                // for animations we have a thing called a "AnimationController.cs"
                // that ReMaps entities within a "Track" where the "Track" represents a
                // per instance playback state as opposed to the shared animation itself.
                // So what are the parallels here?  Our "scripts" register Routes and these routes
                // cannot be removed because they are inherent to the specific Entity.  But the
                // resulting source and endpoints can be and so can the mapping to the actual
                // entity references and methods.

                // note: we do not validate or initiate raiseChangeEvent when the target's property
                // is changed due to a ROUTE?
                bool validate = false;
                bool raiseChangeEvent = false;
                int[] errorCodes;

                if (routes[i].Target.Entity != null)
                {
                    Entity target = (Entity)routes[i].Target.Entity;
                    target.SetCustomPropertyValues(
                        new string[] { routes[i].Target.MethodName },
                        new object[] { value },
                        validate,
                        raiseChangeEvent,
                        out errorCodes);
                }
            }
        }
        #endregion

        #region IPageableTVNode Members
        public object SyncRoot
        {
            get { return mSyncRoot; }
        }

        public virtual int TVIndex
        {
            get { throw new NotImplementedException(); }
        }

        public virtual bool TVResourceIsLoaded
        {
            get
            {
                // NOTE: some entites don't use domainscript but still have db resources to load
                // so rather than override, just check for .PageableNodeStatus == Loaded
                return _resourceStatus == PageableNodeStatus.Loaded;
            }
        }

        public virtual string ResourcePath
        {
            get { if (mScriptResource == null) return ""; return mScriptResource.ToString(); }
            // cannot set domain script manually. Must be done through AddChild() & RemoveChild()
            // or through loading of the var from deserialization of the Entity
            // TODO: wait, why not through setting path and then clearing the resourcestatus so LoadTVResource gets called?
            // i know there is a concern that when we try to run domainobject scripts that it is attached to its Entity by then.
            // well i see no reason why this should affect that ever. DO can never be loaded this way until entity.LoadTVResource is called
            // although 
            set { mScriptResource = value; } // throw new Exception(); }
        }

        public PageableNodeStatus PageStatus
        {
            get { return _resourceStatus; } // TODO: shouldn't this return the mDomainObject's status instead?
            set { _resourceStatus = value; }
        }

        public virtual void UnloadTVResource()
        {
            if (this.Name == "tactical")
                System.Diagnostics.Debug.WriteLine("Removing Script");

            // no script node to page out            
            if (mScript == null) return;

            Keystone.IO.ClientPager.UnloadTVResource(mScript, false);

            if (mScript.PageStatus != PageableNodeStatus.NotLoaded)
                throw new Exception("Entity.UnLoadTVResourceSynchronously() - Error unloading script '" + mScriptResource + "'");



            // TODO: wont this occur on .RemoveAllChildren() anyway? Why are we removing script here?
            this.RemoveChild(mScript);
        }

        // TODO: should this be "internal" also? sometimes we want to force load though from EXE so maybe cannot
        public virtual void LoadTVResource()
        {
            // if the computed path and the path retreived during GetProperties() 
            // are not the same, we have one of two scenarios
            if (string.IsNullOrEmpty(mScriptResource))
            {
                return;
            }

            try
            {
                // if the EntityScript node is already a child and is the same child
                // we can exit.
                if (mScript != null && mScript.ResourcePath == mScriptResource)
                //TODO: if LoadTVResource() is called from worker_insert__xx  as opposed to being
                //      called from Pager.cs, and if the domainobject is already cached, this will return
                //       and _resourceStatus never gets set since it is only set in Pager.cs.  This could be a 
                //       more fundamental problem where every call to LoadTVResource() not from Pager.cs is
                //       not setting the _resourceStatus var properly.  TODO: I should ensure all worker .LoadTVResource() has to
                //        go through PagerBase.LoadTVResourceSynchronously 
                {

                    
                    if (!mChildren.Contains(mScript))
                        this.AddChild(mScript);

                    // hack, force set of _resourceStatus and return without continuing on below to try and load in an already loaded script
                    _resourceStatus = mScript.PageStatus;

                    // NOTE: On this path, it is possible to retreive a shared script instance that is still Loading
                    // so in that case, we need to check for Keystone.Enums.ChangeStates.EntityScriptLoaded in Entity.Update()
                    // and then call the script's "InitializeEntity"
                }
                else
                {
                    Keystone.DomainObjects.DomainObject scriptNode =
                        (DomainObjects.DomainObject)Keystone.Resource.Repository.Create(mScriptResource, "DomainObject");

                    // attempt to load the script immediately.  If it's already shared and loaded, it is no problem.
                    Keystone.IO.PagerBase.LoadTVResource(scriptNode, false);
                    if (scriptNode.PageStatus != PageableNodeStatus.Loaded)
                        throw new Exception("Entity.LoadTVResource() - Error loading script '" + mScriptResource + "'");

                    this.AddChild(scriptNode);
                }
            }
            catch (System.IO.InvalidDataException ex)
            {
                System.Diagnostics.Debug.WriteLine("Entity.LoadTVResource() - " + ex.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Entity.LoadTVResource() - " + ex.Message);
            }
        }

        public virtual void SaveTVResource(string filepath)
        {

        }
        #endregion

        /// <summary>
        /// Used primarily by Proxies to indicate the entity they are meant to represent.
        /// </summary>
        public string ReferencedEntityID { get { return mReferencedEntityID; } set { mReferencedEntityID = value; } }

        public KeyCommon.Flags.EntityAttributes Attributes
        {
            get { return mEntityFlags; }
            set { mEntityFlags = value; }
        }

        public Scene.Scene Scene
        {
            get { return mScene; }
            internal set
            {
                //System.Diagnostics.Debug.Assert(mScene == null);
                mScene = value;

                InitializeEntityScript();

                // InitializeEntity can only be called when the Entity.Scene is set.  So here we
                // recursively initialize all children when a parent Entity is connected to the scene.
                // This is very useful when spawning Vehicles with lots of Interior and exterior components.
                if (mChildren != null)
                    foreach (Node child in mChildren)
                        if (child is Entities.Entity)
                            ((Entity)child).Scene = mScene;
            }
        }

        public SceneNode SceneNode
        {
            get { return mSceneNode; }
            set { mSceneNode = value; }
        }

        public Entity Parent // NOTE: entities can only have one parent hence the hard coded [0] subscript
        {
            get
            {
                if (mParents == null) return null;
                System.Diagnostics.Debug.Assert(mParents.Count == 1, "Entity.getParent - Entities can only have one parent.");
                return (Entity)mParents[0];
            }
            //set { _parents[0] = value; } // we use child.AddParent() and rely on assert during dev to ensure this rule isnt broken.
        }

        /// <summary>
        /// Recurses up through parent hierarchy until the first Region is found.  This way
        /// a sword attached to an actor can find it's region by recursing upwards.
        /// </summary>
        public Region Region
        {
            get
            {
                if (mParents == null || mParents[0] == null) return null;

                if ((mParents[0] is Entity) == false) return null; // Viewpoint entity is attached to a SceneInfo IGroup
                Entity parent = (Entity)mParents[0];
                while (parent != null)
                {
                    if (parent is Region) return (Region)parent;
                    parent = parent.Parent;
                }
                return null;
            }
        }

        public Container Container
        {
            get
            {
                if (mParents == null) return null;
                Entity parent = (Entity)mParents[0];
                while (parent != null)
                    if (parent is Keystone.Entities.Container)
                        return (Keystone.Entities.Container)parent;
                    else
                        parent = parent.Parent;

                return null; // entity has no container owner which is ok.
            }
        }

        public DomainObjects.DomainObject Script { get { return mScript; } }

        public Behavior.Behavior Behavior { get { return mBehaviorRoot; } }

        public ushort RenderPriority { get { return mRenderPriority; } set { mRenderPriority = value; } }

        /// <summary>
        /// Direct accessor to any available LODSwitch or Geometry child node. 
        /// No "setter" allowed. note: viewpoints and other non renderable types can have animations.
        /// </summary>
        public virtual Animation.AnimationController Animations
        {
            get
            {
                return mAnimationController;
            }
        }

        private bool mFootprintIsDirty = true;
        // TODO: these flags should be set-able by script or property i suppose
        private Interior.TILE_ATTRIBUTES mAutoGenFootprintFlags = Keystone.Portals.Interior.TILE_ATTRIBUTES.COMPONENT;
        public CellFootprint Footprint
        {
            get
            {
                bool autoGenerateFP = GetEntityAttributesValue((uint)EntityAttributes.AutoGenerateFootprint);
                if (autoGenerateFP == false) return mFootprint;

                System.Diagnostics.Debug.Assert(false, "Entity.Get_Footprint() should never attempt to AutoGenerateFootprint. This was a mistake.  This assert is for making sure we never actually use it.");

                // NOTE: mFootprint can be null, but for components to be placeable inside of Interiors they are required

                //Avoid needless re-autogeneration and only update when footprint is dirty (i.e mFootprintIsDirty == true)
                //       TODO: dirty footprint occurs when 
                //       - component Scale is modified or any of it's child Model's scales are modified 
                //		 - component is freshly loaded or it's models are finally loaded
                //       - when in design mode.  How do we know?

                // TODO: im not sure the below code is ever being used because the AutoGenerateFootprint flag is currently never set anywhere.
                //        But even if it were set, i think its a bad idea to autogenerate footprints.  There's too many problems associated with that.
                if (mFootprint == null || mFootprintIsDirty)
                {
                    // the entity passed in must be at identity
                    CellFootprint footprint = CellFootprint.Create((ModeledEntity)this, mAutoGenFootprintFlags);

                    // TODO: here if geometry nodes are not fully loaded in, we will not be able to compute a footprint
                    //       so unless we force delayResourceLoading = false 
                    if (footprint == null) throw new Exception();

                    // always ignore and replace any existing footprint if AutoGenerateFootprint = true from within this function
                    // use the SetProperty so that the CellFootprint node is properly Added/Removed as child node
                    SetProperty("footprint", typeof(string), footprint.ID);
                    mFootprintIsDirty = false;
                }
                return mFootprint;
            }
        }

        // Visible = false means the entity could still be Enabled but is not rendered.
        // Region's can be Visible = false to hide all child Entities but those children will still be active
        // Children will NOT be recursed for rendering if a parent is Visible = false.
        // TODO: is above child recurse note correct including considering that
        // an exterior Vehicle can not be rendered but still allow for traversal
        // of interior.
        // NOTE: If Pickable=true a Visible = false entity can still picked.

        public virtual bool Visible
        {
            get { return (mEntityFlags & EntityAttributes.VisibleInGame) == EntityAttributes.VisibleInGame; }
            set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.VisibleInGame;
                else
                    mEntityFlags &= ~EntityAttributes.VisibleInGame;
            }
        }


        public virtual bool Pickable
        {
            get { return (mEntityFlags & EntityAttributes.PickableInGame) == EntityAttributes.PickableInGame; }
            set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.PickableInGame;
                else
                    mEntityFlags &= ~EntityAttributes.PickableInGame;
            }
        }

        public virtual bool Overlay
        {
            get { return (mEntityFlags & EntityAttributes.Overlay) == EntityAttributes.Overlay; }
            set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.Overlay;
                else
                    mEntityFlags &= ~EntityAttributes.Overlay;
            }
        }

        /// <summary>
        /// The max distance squared at which this object is visible.
        /// Only used if _visibleBeyondFarPlane == true such as for stars and worlds
        /// </summary>
        public double MaxVisibleDistanceSquared
        {
            set { _maxVisibleDistanceSq = value; }
            get { return _maxVisibleDistanceSq; }
        }

        public Physics.RigidBody RigidBody
        {
            get { return mRigidBody; }
        }

        public Physics.Colliders.BoxCollider BoxCollider
        {
            get { return mBoxCollider; }
        }

        public Physics.Colliders.SphereCollider SphereCollider
        {
            get { return mSphereCollider; }
        }

        public Physics.Colliders.CapsuleCollider CapsuleCollider
        {
            get { return mCapsuleCollider; }
        }


        public bool Awake
        {
            get { return (mEntityFlags & EntityAttributes.Awake) == EntityAttributes.Awake; }
            set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.Awake;
                else
                    mEntityFlags &= ~EntityAttributes.Awake;
            }
        }

        public override Vector3d Scale
        {
            set
            {
                if (value == mScale) return; // no change
                base.Scale = value;

                // todo: im not sure why i added the following.  It's not even a custom property and that's what PropertyChangedEventRaise() is for
                //if (mScript != null && mScript.TVResourceIsLoaded)
                //    mScript.PropertyChangedEventRaise(this.ID, "scale");
            }
        }

        public double DistanceToSquared(Entity target)
        {
            if (this.Region == target.Region)
            {
                // DerivedTranslation takes into account hierarchical translation up the parent hierarchy to
                // the host Region
                return Vector3d.GetDistance3dSquared(target.DerivedTranslation, this.DerivedTranslation);
            }

            // This works and takes into account scaling and inheritance and different regions.
            return Vector3d.GetDistance3dSquared(target.GlobalTranslation, this.GlobalTranslation);
            // TODO: i really do need to find all cases where we're computing distances
            // between objects and determine if i should be using this instead to ensure
            // we get correct global positions

        }

        public double DistanceTo(Entity target)
        {
            return Math.Sqrt(DistanceToSquared(target));
        }

        //        TODO: this is a clusterfuck and unnecessary.  all i need to do is use entity.GlobalTranslation and target.GlobalTranslation minus any regions.GlobalTranslation
        //        public Vector3d GetTranslationRelativeToRegion (Region r)
        //        {
        //        	// TODO: this is buggy/untested/unverified function
        //        	// TODO: before i did a simple entity.GlobalTranslation - viewpoint.GlobalTranslation
        //        	
        //        	// and then didnt even make it camera space and it worked
        //        	
        //        	// get matrix to convert from one to the other
        //        	Matrix source2dest = Matrix.Source2Dest(this.Region.GlobalMatrix, r.GlobalMatrix);
        //        	Matrix transformedTargetMatrix = Matrix.Multiply(source2dest, Matrix.CreateTranslation (this.DerivedTranslation));            	
        //        	Vector3d targetPosition = transformedTargetMatrix.GetTranslation();
        //        	
        //        	//#if DEBUG
        //        	Matrix dest2source = Matrix.Source2Dest (r.GlobalMatrix, this.Region.GlobalMatrix);
        //        	Matrix transformedSourceMatrix = Matrix.Multiply (dest2source, Matrix.CreateTranslation (targetPosition));
        //        	Vector3d startingPosition = transformedSourceMatrix.GetTranslation();
        //        	System.Diagnostics.Debug.Assert (startingPosition.Equals (this.DerivedTranslation));
        //        	//#endif
        //        	
        //        	return targetPosition; 
        //        }

        // SwitchParent in this context refers to an entity being removed from one part of the scene and insert into another.
        // The main purpose is to keep the entity in Repository while RemoveChild is called and then AddChild again in it's new place without the entity
        // getting removed from the Repository.  THATS why I'm thinking a "entityMoved" event isnt entirely necessary. Although
        // we do know it's sceneNode would need to be updated, but in reality, it's a Remove->Add operation and not a "move"
        // that would require special continuous collision or anything either.  
        //
        // For a regular movement related move, we can use coherence to first check we are still in the same region and only check
        // neighboring regions recursively (up to a depth) to find where it needs to move and then fall back to root search.
        // WARNING! For things like cargo being jettisoned or fighters or shuttles leaving a hangar bay, we need to make sure
        // inertia is applied to the child craft and taht we handle the transition properly even if the parent ship is moving fast
        // across zones, that the child craft then is moved into the correct zone

        // SwitchParent allows us to bypass the AddChild method completely and specifically
        // the resulting EntityAdded event which is supposed to be for when Entities are first loaded into the SceneGraph
        // we also want to prevent the removal of the Entity as it's ref count hits 0 on Repository.DecrementRef
        // What is the real issue with EntityAdded event though since it first is preceded by an EntityRemoved event?
        public virtual void SwitchParent(Entity newParent)
        {
            IGroup previousParent = this.Parents[0];
            Region previousRegion = this.Region;

            System.Diagnostics.Debug.Assert(newParent == previousParent == false, "Entity.SwitchParent() - Parents are one and the same!");
            if (newParent == null) throw new ArgumentNullException("Cannot move entity to null parent!");


            Repository.IncrementRef(this); // artificial increment this child node to make sure Repository doesn't delete during .RemoveChild()          
            previousParent.RemoveChild(this); // remove should not allow the SceneNode to be deleted and apparently it doesnt in BoundElementGroup.RemoveChild()
            SuperSetter setter = new SuperSetter(newParent);
            setter.Apply(this);
            Repository.DecrementRef(this); // decrement the child node that was artifically incremented in this method earlier


            this.Execute("OnParentChanged", new object[] { this.ID, newParent.ID, previousParent.ID });

            if (previousRegion != this.Region)
                this.Execute("OnRegionChanged", new object[] { this.ID, this.Region.ID, previousRegion.ID });

            //SetChangeFlags(Enums.ChangeStates.GeometryAddedRemoved); // handled by the RemoveChild and AddChild calls
        }

        #region IGroup
        public override void RemoveParent(IGroup parent)
        {
            base.RemoveParent(parent);

            if (parent is Entity)
            {
                // note: it must be this child Entity which ishaving it's RemoveParent() called on it
                // to be the one to call .EntityDetached()
                // TODO: I believe if the mScene is null here, it is because this item was already detached
                // from the scene but it's ref count was not yet 0 due to being manually incremented.  
                // So under that circustance it should be legitimate to have mScene == null so we test for it
                if (mScene != null)
                {
                    mScene.EntityDetached(this);
                    // NOTE: simulation.UnRegister() occurs for producing DomainObjects here
                    // in RemoveChild()

                    // Here is correct location to unregister this entity from simulation
                    // if it was a production entity.
                    // TODO: shouldnt this be done on Child Enties?
                    if (this.Script != null)
                    {
                        this.Script.Execute("UnInitializeEntity", new object[] { this.ID });
                        mScriptIsInitialized = false;
                        // OBSOLETE - we now unregister from within the script on call to UnInitializeEntity();
                        //if (this.Script.UserProduction != null)
                        //       	    	mScene.Simulation.UnRegisterProducer(this);
                        //if (this.Script.ForceProduction != null)
                        //       	    	mScene.Simulation.UnRegisterForceProducer(this);
                    }
                }
            }
        }

        public override void AddParent(IGroup parent)
        {
            // must assign scene prior to Repository.IncrementRef() which occurs in base method
            // TODO: i think one fundamental problem we have is that when we add a child to the scene
            // our method of assigning the Scene is just all fubar.  When an entity is connected to a scene
            // via it's parent being connected, then that parent should immediately connect all children 
            // BEFORE calling the base.AddParent() 
            // I mean essentially what we want is for recursive set of all Scene's first.   
            // That does imply it cannot happen in AddParent.  It should happen in Scene property
            // 
            //if (parent is Entity)
            // TODO: recursively here, once this Zone for example is added, 
            //	this.mScene = ((Entity)parent).Scene ;

            // AddParent last because we want the Repository.IncrementRef() to occur after we've
            // attached this node to the scene.
            base.AddParent(parent);

            //if (this.ResourcePath != null && (this.ResourcePath.Contains("helm") || this.ResourcePath.Contains("tactical")))
            //{
            //    System.Diagnostics.Debug.Assert(mScriptIsInitialized);
            //    System.Diagnostics.Debug.Assert(this.Scene != null);
            //}

            if (parent is Entity && ((Entity)parent).Scene != null) // if Parent.Scene is null, we will recursively call EntityAttached from within EntityAttached on child nodes later on
            {
                Keystone.Scene.Scene scene = ((Entity)parent).Scene;
                // note: it must be this child Entity which is having it's AddParent() called on it
                // to be the one to call .EntityAttached() because if the Parent does, the
                // Simulation will not know which child it's referring to.  But child Entities
                // on the other hand only ever have one parent.
                // note: Root never has this called because it has no parent so no AddParent() call ever occurs.
                // Root is Attached() by hand.
                scene.EntityAttached(this, scene);
                InitializeEntityScript();
            }


            // NOTE: These flags are necessary (i think) because a child entity can be added
            //       to it's parent or switch parents and that will result in dirty Global and Regional 
            //       matrices (but not Local) because the parent matrices are now different.
            // NOTE: These flags are source PARENT and so only propogate DOWN
            // NOTE: The bounding volumes and RegionMatrix must be flagged dirty too
            // because if this is a re-parent operation, then inherited translation, rotation and scale
            // will invalidate the region matrices
            // NOTE: It's vital to only propogate these DOWN 
            SetChangeFlags(Enums.ChangeStates.GlobalMatrixDirty |
                            Enums.ChangeStates.RegionMatrixDirty |
                            Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Parent);
            // // temp version without the bbox flags to see if this is ok (eg bbox flags are redudndant)
            //SetChangeFlags(Enums.ChangeStates.GlobalMatrixDirty |
            //    			 Enums.ChangeStates.RegionMatrixDirty, Enums.ChangeSource.Parent);
        }

        public virtual void AddChild(Entity child)
        {
            // NOTE: always set flags early before any .AddParent/.Add so that if those other methods
            // result in calls to update a bounding box for example, that we then don't return here only
            // to set those flags as dirty again after they've just been updated
            // NOTE: only ChangeStates.GeometryAdded here.  ChangeStates.EntityAttached must only occur
            // in the AddParent method! (and actually i think that's no longer true as i directly call
            // mScene.NodeAttached(this) from there
            SetChangeFlags(Keystone.Enums.ChangeStates.GeometryAdded |
                Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);

#if DEBUG
            if (this is Zone && child is TileMap.Structure)
            {
                System.Diagnostics.Debug.WriteLine("Entity.AddChild() - TileMap.Structure '" + child.ID + "' added.");
                if (mChildren != null)

                    System.Diagnostics.Debug.Assert(mChildren[0] is Viewpoint);
            }
#endif
            base.AddChild(child); //child.Parent = this; // NOTE: even though this uses AddChild() from the Node base class, it should still only ever result in _parents[0] being used. All entities only have one parent 
            Debug.Assert(child.Parent == this);
        }

        internal void AddChild(DomainObjects.DomainObject script)
        {

            mScript = script;
            mScriptResource = mScript.ResourcePath;

            // Oct.11.2024 - Since we can now InitializeEntityScript() if needed in Entiy.Update() when ScriptLoaded changeflag is set
            // this is no longer necessary.
            //if (script.PageStatus != PageableNodeStatus.Loaded)
            //    throw new Exception("Entity.AddChild() - DomainObject script MUST be loaded at this time or else we cannot invoke 'InitializeEntity'" + "Entity.AddChild() - DomainObject is now a 'resource' of Entity which means it's now guaranteed to be loaded before it's added as child.");

            base.AddChild(script);
            
            // Oct.9.2016 - testing for mParent == null below causes problems when loading the entity such as for Nav Map where Star's
            //              : but we need to verify not testing mParent != null does not cause other problems. I.E why did we perform this test in the first place?
            //				  I think it was because when an Entity is parented, it means it's been added to the Scene.  But we now know that this is
            //				  not always the case.  NavMap stars are not currently parented, but are loaded to generate the starmap.
            //				  we need to ensure all scripts when executing InitializeEntity() do not require code that references parent node.
            InitializeEntityScript();
            

            // TODO: here we can set the state of this Entity to "awake"
            //       which means we can notify the scene simulation so that
            //       it can now run register producers & forceproducers
            //       - the question i have is, when the entity is not awake
            //       and we are receiving state info from server about it, what
            //       do we do with that state info? as the script is unable to respond
            //       - what if we are not connected to the scene yet?

            bool autoGenerateFP = GetEntityAttributesValue((uint)EntityAttributes.AutoGenerateFootprint);
            SetChangeFlags(Keystone.Enums.ChangeStates.EntityScriptLoaded, Keystone.Enums.ChangeSource.Child);
        }

        private void InitializeEntityScript()
        {
            // InitializeEntity with per-entity settings defined in the script
            // recall that the "Initialize" method is performed once per script and occurs
            // in script.LoadTVResource().   "InitializeEntity" on the otherhand is once per-entity
            // NOTE: we use mScript.Execute here since mScriptIsInitilazed is not yet 'true'.
            if (this.mScript != null && this.mScript.PageStatus == PageableNodeStatus.Loaded && mScriptIsInitialized == false && mScene != null)
            {
                // we need to restore the default property values BEFORE InitializeEntity() because
                // some scripts will try to call SetCustomPropertyValue() which requires mCustomPropertyValues 
                // is restored.

                // since this is a shared domain object and when added to a subsequent entity
                // although we dont need to initialize it again, we do need it's default properties
                RestoreCustomPropertyValuesFromPersistString();


                mScript.Execute("InitializeEntity", new object[] { this.ID });
                mScriptIsInitialized = true;
            }
        }

        public void AddChild(Behavior.Behavior behavior)
        {
            // all behaviors extend from a single root and thus there can only be
            // one direct behavior child within an Entity
            if (mBehaviorRoot != null) throw new ArgumentException("Entity.AddChild() - Previous Behavior not removed");
            mBehaviorRoot = behavior;
            base.AddChild(behavior);

            // April.13.2019 - obsolete? we no longer need to pass in a BehaviorContext. We use the entity.CustomData which serves as our AIBlackboardData
            //                 todo: i dunno. hmm.
            // if (mBehaviorContext == null)
            //     mBehaviorContext = new KeyCommon.Traversal.BehaviorContext(this, mBehaviorRoot);

            // TODO: SetChangeFlags?  
        }

        public void AddChild(CellFootprint footprint)
        {
            if (mFootprint != null) throw new ArgumentException("Entity.AddChild() - Previous Footprint not removed.");
            mFootprint = footprint;

            base.AddChild(mFootprint);
        }

        public void AddChild(Animation.Animation animation)
        {
            base.AddChild(animation);

            mAnimations = mAnimations.ArrayAppend(animation);

            if (mAnimationController == null)
            {
                mAnimationController = new Keystone.Animation.AnimationController(this);
                mAnimationController.mAnimationEventHandler += OnAnimationEvent;
            }
            mAnimationController.Add(animation);
            // TODO: changeflags so that some animations such as BonedAnimations can be initialized?
            //       and/or clips mapped to targets

        }


        // TODO: for Interior, does RigidBody need to exist for each floor?  Interior.cs may need to handle RigidBodies differently (eg allow more than just 1 per entity)
        //       Perhaps for Interior if it's all just automated (just like the walls and floor geometry is automated and not created by AddChild() of Models and such)
        //       then perhaps the physics can just be automated for Interior.cs for floors and walls as well.
        //       For that case, we would internally use arrays of RigidBody and Colliders and manage them ourselves and mark them as not serializable, but created at runtime when loading the ship.
        Physics.RigidBody mRigidBody;
        Physics.Colliders.BoxCollider mBoxCollider;
        Physics.Colliders.SphereCollider mSphereCollider;
        Physics.Colliders.CapsuleCollider mCapsuleCollider;

        public void AddChild(Physics.RigidBody body)
        {
            if (mRigidBody != null) throw new Exception("RigidBody already exists.  Remove the existing one first.");

            mRigidBody = body;
            base.AddChild(body);
            SetChangeFlags(Keystone.Enums.ChangeStates.PhysicsNodeAdded, Enums.ChangeSource.Child);
        }

        // note: unity allows multiple colliders, but only if they are different (eg. one BoxCollider and one ShereCollider but 
        // they must all have same properties like both must be triggers are not triggers)
        public void AddChild(Physics.Colliders.BoxCollider collider)
        {
            if (mBoxCollider != null) throw new Exception("Box Collider already exists.  Remove the existing one first.");

            // todo: should we initialize the box collider's position and dimensions to match this Entity's BoundingBox and Translation?

            // TODO: need to remove these references in RemoveChild() or else the object wont reduce it's refCount
            mBoxCollider = collider;
            base.AddChild(collider);
            SetChangeFlags(Keystone.Enums.ChangeStates.PhysicsNodeAdded, Enums.ChangeSource.Child);
        }

        public void AddChild(Physics.Colliders.SphereCollider collider)
        {
            if (mSphereCollider != null) throw new Exception("Sphere Collider already exists.  Remove the existing one first.");

            // TODO: need to remove these references in RemoveChild() or else the object wont reduce it's refCount
            mSphereCollider = collider;
            base.AddChild(collider);
            SetChangeFlags(Keystone.Enums.ChangeStates.PhysicsNodeAdded, Enums.ChangeSource.Child);
        }

        public void AddChild(Physics.Colliders.CapsuleCollider collider)
        {
            if (mCapsuleCollider != null) throw new Exception("Capsule Collider already exists.  Remove the existing one first.");

            // TODO: need to remove these references in RemoveChild() or else the object wont reduce it's refCount
            mCapsuleCollider = collider;
            base.AddChild(collider);
            SetChangeFlags(Keystone.Enums.ChangeStates.PhysicsNodeAdded, Enums.ChangeSource.Child);
        }

        public override void RemoveChild(Node child)
        {
            base.RemoveChild(child);
            if (child as Entity != null)
            {
                // NOTE: EntityDetached is not called here, but in the call Entity.RemoveParent()
                SetChangeFlags(Enums.ChangeStates.GeometryRemoved |
                    Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            }
            else if (child as CellFootprint != null)
            {
                Debug.Assert(child == mFootprint);
                mFootprint = null;
            }
            else if (child as DomainObjects.DomainObject != null)
            {
                Debug.Assert(child == mScript);

                mScriptResource = null;
                mScript = null;
            }
            else if (child as Behavior.Behavior != null)
            {
                Debug.Assert(child == mBehaviorRoot);
                mBehaviorRoot = null;
                mBehaviorContext = null;
            }
            else if (child as Animation.Animation != null)
            {
                mAnimations = mAnimations.ArrayRemove((Animation.Animation)child);
                mAnimationController.Remove((Animation.Animation)child);
                if (mAnimations == null || mAnimations.Length == 0)
                {
                    mAnimations = null;
                    mAnimationController = null;
                }
            }
            else if (child as Physics.RigidBody != null)
            {
                Debug.Assert(child == mRigidBody);
                mRigidBody = null;
            }
            else if (child as Physics.Colliders.BoxCollider != null)
            {
                Debug.Assert(child == mBoxCollider);
                mBoxCollider = null;
            }
            else if (child as Physics.Colliders.SphereCollider != null)
            {
                Debug.Assert(child == mSphereCollider);
                mSphereCollider = null;
            }
            else if (child as Physics.Colliders.CapsuleCollider != null)
            {
                Debug.Assert(child == mCapsuleCollider);
                mCapsuleCollider = null;
            }

        }
        #endregion

        /// <summary>
        /// Flags are used for Lazy Updates.
        /// This method is also when SetChangeFlags is called.  It is critical that 
        /// no other actions except flag propogation occurs here because
        /// some flags will get passed along multiple times and that would result in 
        /// multiple unnecessary actions.  Instead, responses to various flag settings
        /// should always occur during Update or Render or reading of a property value like 
        /// BoundingBox and then the flag can be cleared.
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="source"></param>
        protected override void PropogateChangeFlags(Keystone.Enums.ChangeStates flags, Enums.ChangeSource source)
        {
            // for entities, only movement and size related changes propogate up to parents in the spatial graph or entity graph
            Keystone.Enums.ChangeStates filter =
                Keystone.Enums.ChangeStates.Translated |
                Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly |
                Keystone.Enums.ChangeStates.BoundingBoxDirty |
                Keystone.Enums.ChangeStates.Scaled |
                Keystone.Enums.ChangeStates.Rotated |
                Keystone.Enums.ChangeStates.GeometryAdded |
                Keystone.Enums.ChangeStates.GeometryRemoved |
                Keystone.Enums.ChangeStates.KeyFrameUpdated |
                Keystone.Enums.ChangeStates.GlobalMatrixDirty | //<-- this does need to be here as it needs to propogate down to child Model node.
                Keystone.Enums.ChangeStates.MatrixDirty |
                Keystone.Enums.ChangeStates.RegionMatrixDirty;


            // filter out the flags that are _not in_ the filter list
            Keystone.Enums.ChangeStates filteredFlags = flags & filter; // = flags & ~filter;

            if (filteredFlags != 0)
            {

                // if source of the flag is a child or self (and not a parent), notify parents
                if (mParents != null && (source == Enums.ChangeSource.Child || source == Enums.ChangeSource.Self))
                    NotifyParents(filteredFlags);

                // if source of the flag is a parent or self (and not a child), notify relevant children for relevant flags
                if (mChildren != null && (source == Enums.ChangeSource.Parent || source == Enums.ChangeSource.Self))
                    NotifyChildEntities(filteredFlags);


                // Scene.EntityMoved and
                // sceneNode's will need to have it's bounding volume dirty flag set
                if (mSceneNode != null)
                {
                    filter = Keystone.Enums.ChangeStates.GeometryAdded |       // June.12.2014 - Added .GeometryAdded and .GeometryRemoved because that 
                             Keystone.Enums.ChangeStates.GeometryRemoved |     // flag is propogated after .LoadTVResource in Geometry nodes but
                             Keystone.Enums.ChangeStates.Scaled |             // was not getting handled here so that SceneNode's could update their own bounding volumes.
                             Keystone.Enums.ChangeStates.Translated |          // Thus we'd have to manually initiate a re-scale in plugin to get the SceneNode to recalc it's bbox
                             Keystone.Enums.ChangeStates.Rotated |
                             Keystone.Enums.ChangeStates.BoundingBoxDirty |    // Oct.8.2014 - Added BoundingBox_TranslatedOnly and  BoundingBoxDirty since changes to Geometry 
                             Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly; // bounding box can occur when there is no scaling/translation or rotation such as with adding new Minimesh Elements.

                    // filter out different set of flags that are _not_ in the list
                    filteredFlags = flags & filter; // filteredFlags = flags & ~filter;
                    if (filteredFlags != 0)
                    {
                        // Dec.4.2012 - It's critical that we don't send the following mSceneNode.SetChangeFlags
                        //              or mScene.EntityMoved() until we've notified parents and children as we have
                        //              done above!
                        // Dec.4.2012 - It's critical to notice here that because each Entity
                        //              is setting BoundingBoxDirty flag on it's SceneNode
                        //              that nested SceneNodes will all have thier flags set
                        //              as dirty such that when the parent sceneNode is asked
                        //              for it's bbox and it's dirty, it's UpdateBoundVolume()
                        //              will in turn see that it's child's bbox is dirty and so
                        //              will be updated as well.  
                        //              THis is because this PropogateChangeFlags() occur immediately
                        //              throughout the affected branches as our Update() is being performed.
                        //              Meanwhile culling doesn't start until after Update().
                        mSceneNode.SetChangeFlags(filteredFlags, Enums.ChangeSource.Target);   //"Target" refers to a SceneNode's Entity target as opposed to it's actual Child node.


                        if (this is Proxy3D == false) // we dont move proxies in Simulation
                        {
                            // NOTE: If it seems a particular Entity is generating EntityMoved flags unnecessarily, check that
                            //       the Entity does not have Dynamic flag set.  Recall we re-ordered the EntityAttributes so that some
                            //       old prefabs may still be trying to deserialize bad flag values.
                            // note: EntityMoved flag is never propogated up or down.  It's only used in Update() for NotifyScene
                            this.SetChangeFlags(Enums.ChangeStates.EntityMoved, source);
                            if (mScene != null)
                                mScene.EntityUpdated(this);
                        }
                    }
                }

                // Scene.EntityResized flag in Scene
                filter = Keystone.Enums.ChangeStates.Scaled |
                              Keystone.Enums.ChangeStates.Rotated |
                              Keystone.Enums.ChangeStates.GeometryAdded |
                              Keystone.Enums.ChangeStates.GeometryRemoved;

                if ((flags & filter) != 0)
                {
                    if (this is Proxy3D == false) // we dont move proxies in Simulation
                    {
                        // note: EntityResized flag is never propogated up or down.  It's only used in Update() for NotifyScene
                        this.SetChangeFlags(Enums.ChangeStates.EntityResized, source);
                        if (mScene != null)
                            // TODO: Zone and ZoneRoot are getting "Resize" when child enties are Rotating and propogating
                            // that upward, i think they also respond to translated and boundingbox dirty and such unnecessarily
                            mScene.EntityUpdated(this);
                        // footprint is dirty 
                        mFootprintIsDirty = true;
                    }
                }
            }
        }


        protected override void NotifyParents(Enums.ChangeStates flags)
        {
            if (mParents == null || flags == Keystone.Enums.ChangeStates.None) return;

            // TODO: for Zones and fixed size Regions, we would like to skip
            //       SetChangeFlags() call for resize and translation and such flags
            //       from children!

#if DEBUG
            if (mParents.Count >= 0)
                System.Diagnostics.Debug.Assert(mParents.Count == 1);
#endif
            // entity (other than Viewpoint.cs) can only have 1 parent (and of type Entity) at most because it is not a shareable scene graph node
            Parent.SetChangeFlags(flags, Keystone.Enums.ChangeSource.Child);
        }


        // NOTE: this is also called in the BonedModel.Update() for handling child models attached to bones
        protected virtual void NotifyChildEntities(Enums.ChangeStates flags)
        {
            // this function is called by this class whenever the parent is translated, rotated or scaled. 
            // notify child entities that they will need to compute new world position and/or rotation and/or scale and related matrices
            // depending on what has changed in the parent
            if (mChildren == null || mChildren.Count == 0) return;
            Node[] children = mChildren.ToArray(); // to avoid issue of iterating _children and another child eleemnt being added during that for loop

            for (int i = 0; i < children.Length; i++)
                // NOTE: Container overrides this to avoid notifying a Region child
                //       since region's have their own coordinate systems that are unaffected by
                //       a parent's transform change.
                // TODO: but how can you get the flags to contain flags for Rotation, Scale and Translation at the same time
                // when we originally wanted to call NotifyChildEntities right after Translation and such has changed?
                // i mean, i could do something like MoveActor (position, scale, rotation) to ensure its just one call but... meh.
                if (children[i] is BoundTransformGroup)
                    children[i].SetChangeFlags(flags, Enums.ChangeSource.Parent);
        }


        // NOTE: Activate() exists because sceneBase.EntityAttached() can do recursion to initiate the attaching
        //       of any child Entities once it's parent has been attached.  However, that does not mean that
        //       Activation will succeed.  mScript must also be loaded if one is specified in mResourcePath
        //       In the case of Interior, celldatabase must be loaded as well
        // mScene != null, SceneNode != null, parent != null and mScript != null (and mScript.Loaded) if string.IsNullOrEmpty(mResourcePath) == false
        public virtual void Activate(bool value)
        {
            if (value)
            {
                mEntityFlags |= KeyCommon.Flags.EntityAttributes.Awake;

                // we have to call this directly because Update() will never get called
                // which means NotifyListener() will never get called.
                mScene.EntityActivated(this);

            }
            else // deactivate
            {
                mEntityFlags &= ~KeyCommon.Flags.EntityAttributes.Awake;
                //if (mScene != null)
                mScene.EntityDeactivated(this);
            }
        }


        internal void FinalizeMovement()
        {
            // TODO: are the script static methods intrinsic methods?  Not all entities need them.
            // and as i recall, they need to be added to every script if i make them intrinsic.
            // And our ship.css doesn't need those events because we only define trigger Areas
            // as volumes INSIDE the ship (eg. floorplan).  But in Unity, these scripted methods
            // do exist, but it seems only used if they are added to the script.

            // TODO: there is a problem here.  If the outer edge of the capsule collider for an NPC
            // enters a trigger area, the center of the NPC may not be in yet and here we check
            // if the npc entity.Translation is inside the trigger area which would return false
            // and a false OnTriggerExit event.  We could maybe not add the mTriggerArea until the
            // entity.Translation is inside the trigger area, but i don't know if we still generate
            // collisions at that point.
            // NOTE: I do believe we are checking for BonedEntity.Translation to be within the trigger area
            //       before raising an OnTriggerAreaEnter event and adding that trigger to the mTriggerAreas.
            //       So the above "TODO:" should be irrelevant now.
            if (mTriggerAreas != null && mTriggerAreas.Count > 0)
            {
                //Debug.Assert(mTriggerAreas.Count == 1); //TODO: not sure why this assert is failing, but sometimes the Exit doesn't occur before the next Enter
                // are we still in any of these trigger areas? If not, send an Event and remove us from the mTriggerAreas
                // NOTE: There should only be one trigger area in the list since trigger areas are static and should not overlap
                for (int i = 0; i < mTriggerAreas.Count; i++)
                {
                    if (!TriggerAreaContains(mTriggerAreas[i], this.Translation))
                    {
                        OnTriggerAreaExit(mTriggerAreas[i]);
                    }
                }
            }
        }

        private bool TriggerAreaContains(Entity triggerArea, Vector3d point)
        {
            Vector3d halfSize = triggerArea.BoxCollider.Size / 2d;
            Vector3d min = triggerArea.BoxCollider.Center - halfSize;
            Vector3d max = triggerArea.BoxCollider.Center + halfSize;
            BoundingBox box = new BoundingBox(min + triggerArea.Translation, max + triggerArea.Translation);
            return box.Contains(point);
        }

        // TODO: this should be renamed RayCast, not "Collide" which uses other primitive types
        public virtual PickResults Collide(Vector3d start, Vector3d end, KeyCommon.Traversal.PickParameters parameters)
        {
            // NOTE: see notes in ModeledEntity.Collide();
            throw new NotImplementedException();
        }

        public struct EntityInput
        {
            public string Name;  // AxisName
            public bool Enabled; // if the Axis has begin/end states, enabled will indicate true if "begin"
            public object Value; // value of the axis
            public string Source;
        }

        private object mInputQueueLock = new object();
        private System.Collections.Generic.Queue<EntityInput> mInputQueue;
        public void QueueInput(EntityInput input)
        {
            lock (mInputQueueLock)
            {
                if (mInputQueue == null) mInputQueue = new Queue<Entity.EntityInput>();
                mInputQueue.Enqueue(input);
            }
        }

        public EntityInput[] DeQueueAll()
        {
            lock (mInputQueueLock)
            {
                if (mInputQueue == null || mInputQueue.Count == 0) return null;

                EntityInput[] results = new Entity.EntityInput[mInputQueue.Count];

                for (int i = mInputQueue.Count - 1; i >= 0; i--)
                    results[i] = mInputQueue.Dequeue();

                return results;
            }
        }

        // called from Simulation.Update()
        // ORDER  SEE KeystoneGameBlocks/Editor/Simulation.cs
        // TODO: UpdatePhysics() should move as first thing so correct ORDER is maintained
            // ORDER -
            //      1 - physics
            //      2 - logic / rules   <-- occurs in Entity.Script.Exeucte("OnUpdate" currently)
            //      3 - AI, Behavior    <-- occurs in Entity.UpdateAI() currently
            //      5 - Animations      <-- occurs in Entity.Update() 
        public virtual void UpdateAI(double elapsedSeconds)
        {
            //System.Diagnostics.Debug.WriteLine("Entity.UpdateAI - " + mFriendlyName);
            // TODO: how do we get AI to execute round robin if necessary?
            //       entity script OnUpdate() further below should always be called
            //       to update movement (rotation and translation and animations should 
            //       probably update every frame as well.  What if we moved the following
            //       behavior.Perform() in an entity.Think() method that gets called by the simulation
            //       at a fixed frequency and can execute round robin.
            if (mBehaviorRoot != null && mBehaviorRoot.Enable)
                mBehaviorRoot.Perform(this, elapsedSeconds);
        }

        // TODO: Water meshes and such that are rendered not by our Cullers, are still setting .Matrix and not .WorldMatrix

        /// <summary>
        /// Called by Simulation.Update _only_ against enabled ACTIVE entities.
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        public virtual void Update(double elapsedSeconds)
        {
            // TODO: What we need to fix the above is for our Simulation to have an Active
            // list of Entities.    So things like Walls, Floors and such
            // can deactivate and then re-activate when for instance the PhysicsEngine
            // tells us that the entity has been collided with. Or when
            // a Sound zone gets collided with by the Camera.

            if (this.SceneNode == null) return;

            // We know that for BonedAnimations, if the Entity script attempts to "Play" an animation
            // before the Actor3d geometry is loaded, the "Play" will fail.
            if (!mScriptsAndGeometryLoaded)
            {
                if (this.Container != null)
                    mScriptsAndGeometryLoaded = CheckScriptsAndGeometryLoaded(this.Container);
                else
                    mScriptsAndGeometryLoaded = CheckScriptsAndGeometryLoaded(this);
                return;
            }
            // In this usage, it is our PreviousTranslation since entity.Update() scripts can modify the translation.
            mPreviousTranslation = mTranslation;

            if ((mChangeStates & Keystone.Enums.ChangeStates.EntityScriptLoaded) != 0)
            {
                // Dec.16.2013 - we used to restore custom property values here but since
                // Script node is now a non-serializable resource of Entity, we can
                // do that after loading the script here in LoadTVResource() so this
                // test here is not useful anymore... but we'll keep it around
                // check registered flag, and if not, but parent != null, then register

                if (mScene == null) return; // if we are not connected to scene yet, we will NOT disable flag and will poll

                // still here?  then script AND scene are connected so we can register production then disable flag
                //            // OBSOLETE - March.7.2023 - registering of products occur in the script's Register() and UnRegister() methods
                //if (mScript.UserProduction != null)
                //	        	mScene.Simulation.RegisterProducer(this);
                //if (mScript.ForceProduction != null)
                //	        	mScene.Simulation.RegisterForceProducer(this);
                InitializeEntityScript();

                DisableChangeFlags(Keystone.Enums.ChangeStates.EntityScriptLoaded);
            }
            if ((mChangeStates & Keystone.Enums.ChangeStates.BehaviorScriptLoaded) != 0)
            {
                DisableChangeFlags(Keystone.Enums.ChangeStates.BehaviorScriptLoaded);
            }

            // Physics nodes added/removed
            if ((mChangeStates & Keystone.Enums.ChangeStates.PhysicsNodeAdded) != 0)
            {
                if (mSceneNode != null && mScene != null && mScene.Simulation != null) // are we connected to the scene and Simulation? Otherwise don't disable the flag 
                {
                    mScene.Simulation.RegisterPhysicsObject(this);
                    DisableChangeFlags(Keystone.Enums.ChangeStates.PhysicsNodeAdded);
                }
            }
            if ((mChangeStates & Keystone.Enums.ChangeStates.PhysicsNodeRemoved) != 0)
            {
                mScene.Simulation.UnRegisterPhysicsObject(this);
            }

            bool targetChanged = (mChangeStates & Keystone.Enums.ChangeStates.TargetChanged) != 0; // animation target changed
            bool nodeAdded = (mChangeStates & Keystone.Enums.ChangeStates.ChildNodeAdded) != 0;
            bool nodeRemoved = (mChangeStates & Keystone.Enums.ChangeStates.ChildNodeRemoved) != 0;

            if (targetChanged || nodeAdded || nodeRemoved)
            {
                if (mAnimationController != null)
                {
                    // TODO: during paging i think some descendants retreived in .GetDescendants() may wind up being null? 
                    //       Or why did that happen once (so far)? havent tested deserialization of animations nearly enough...
                    // TODO: why am i filtering GroupAttribute? shouldn't it be possible to do a color interpolation animation on a mesh group?
                    // get a flag list of child nodes
                    List<Node> descendants = new List<Node>();
                    Group.GetDescendants(Children, new Type[] { typeof(Entity), typeof(Appearance.GroupAttribute) }, ref descendants);
                    mAnimationController.ReMapAllAnimationsToTargets(descendants.ToArray());
                }
                // this flag will not propogate up any higher
                DisableChangeFlags(Enums.ChangeStates.ChildNodeAdded |
                    Keystone.Enums.ChangeStates.ChildNodeRemoved |
                    Keystone.Enums.ChangeStates.TargetChanged);
            }


            if (Core._Core.ScriptsEnabled)
                if (this.Script != null && mScriptIsInitialized) 
                {
                    System.Diagnostics.Debug.Assert(this.Scene != null);

                    // TODO: is there a way to do fixed frequency updates?
                    // Is it bad that scripts execute at frame rate speeds?  I'm not talking about
                    // fixed step updates, im talking about guaranteeing same amount of updates
                    // per second across all users' hardware. (eg: 30 fps) but do we have a problem 
                    // if the physics runs faster say at 100fps? Animations can run as fast as possible
                    using (CoreClient._CoreClient.Profiler.HookUp("OnUpdate"))
                        Execute("OnUpdate", new object[] { this.ID, elapsedSeconds });
                }


            // TODO: move to this.UpdateAnimations() as we already have this.UpdateAI() here
            if (mAnimationController != null)// && mAnimationSet.OverrideBehaviors)
            {
                // skip running behavior and instead follow animation playback
                // commands including ability to pause and advance to next frame 
                // only on demand
                // We could track the current animation since it is
                // a child here... allow our "ChangeNodeProperties" to
                // change a property in the AnimationSet itself ... yes..
                // AnimationSet.AnimationTestMode
                // AnimationSet.CurrentAnimation
                // etc

                mAnimationController.Update(elapsedSeconds);
            }
        }

        /// <summary>
        /// Check to ensure all Geometry and Scripts descended from the current Entity are loaded.
        /// But this isn't good enough.  We have run into cases where for example the TacticalStation entity
        /// will try to query CustomProperties for things like Weapons and Turrets on the Vehicle it's placed in
        /// and if those weapons and turrets aren't fully loaded, then that means the entire Vehicle isn't fully loaded
        /// and the tactical station which sits in the Interior of the Vehicle should not be having it's Update() execute.
        /// So, how does a tactical station for instance know when the entire vehicle it's apart of has its scripts loaded?
        /// Well, this.Container yields the Vehicle\Container this Entity is apart of.  We can then call CheckScriptsAndGeometryLoaded(container)
        /// to see if the entire hierarchy is loaded.  This assumes that our SceneReader always pages in all the nodes and assembles
        /// the Entity hierarchy before those nodes resources are paged in.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool CheckScriptsAndGeometryLoaded(IGroup node)
        {
            if (node.Children == null) return true;

            for (int i = 0; i < node.Children.Length; i++)
            {
                // we don't traverse nested Entities. Yes actually we DO traverse nested Entities
                // because scripts for Entities like tacticalstation and helm inside a Container\Vehicle will often try to
                // query other Entities within the ship (or even the exterior such as turrets and engines) and
                // so the tacticalstation or helm scripts need to know that the entire Container\Vehicle is loaded
                // before it can run entity.Update() and thus "Execute("OnUpdate", new object[] { this.ID, elapsedSeconds });"
                //if (node.Children[i] is Entities.Entity) 
                //    return true;
                if (node.Children[i] is IGroup)
                {
                    if (!CheckScriptsAndGeometryLoaded((IGroup)node.Children[i]))
                        return false;
                }
                else if (node.Children[i] is Geometry || node.Children[i] is DomainObjects.DomainObject)
                    if (!((IPageableTVNode)node.Children[i]).TVResourceIsLoaded)
                        return false;
            }
            return true;
        }

        private bool mScriptsAndGeometryLoaded = false;
        private bool mScriptIsInitialized = false;
        public object Execute(string eventName, object[] args)
        {
        	if (mScript == null) return null;

            // NOTE: We only check if TVResourceIsLoaded and not mScriptIsInitialized.  This will call ANY registered script function such as "QueryPlacementBrushType"
            // even if "InitializeEntity" hasn't been called.
            if (mScript.TVResourceIsLoaded)
                return mScript.Execute(eventName, args);
        
			return null;        	
        }
        
        #region OBSOLETE - the below ExecuteScript supercedes this PerformBehavior
        ///// <summary>
        ///// Calls into our script dictionary after validating the authority and appropriateness of the Behavior.
        ///// Only Entities (not ResourceBase or non EntityBase derived Nodes) can have behaviors Peformed?
        ///// </summary>
        //public virtual void PerformBehavior(string behavior)
        //{
        //    throw new NotImplementedException("i believe ExecuteScript() function below now supercedes this?  Keeping around til i know for sure.");
        //    float elapsedMilliseconds = 0; // TODO: umm... do we need to always pass elapsed?
        //    if (mScripts != null)
        //        if (mScripts.ContainsKey(behavior))
        //            mScripts[behavior].Execute(new object[] { this.ID, elapsedMilliseconds });
        //}
        #endregion 

        #region OBSOLETE ALSO - Now that we've switched to behavior trees, this is not needed
        //public void ExecuteScript(string key, object[] args)
        //{
        //    if (!Core._Core.ScriptsEnabled) return;

        //    if (mScripts != null)
        //    {
        //        if (mScripts.ContainsKey(key))
        //            if (mScripts[key].Enable)
        //            {
        //                try
        //                {
        //                    mScripts[key].Execute(args);
        //                }
        //                catch (Exception ex)
        //                {
        //                    Trace.WriteLine(string.Format("EntityBase.ExecuteScript() -- Error executing script '{0}'.  {1}", key, ex.Message));
        //                    mScripts[key].Enable = false; // disable any bad scripts
        //                }
        //            }
        //    }
        //}
        #endregion

        #region IBoundVolume
        // ModeledEntities have combined volume of child MODELS but NOT of child Entities!
        // Only EntityNode's use hierarchical bound volumes of child EntityNodes.
        protected override void UpdateBoundVolume()
        {
            
            //if (this.Name == "Neptune")
            //    System.Diagnostics.Debug.WriteLine("Neptune.BBOX");
            // see if we can get away with just translating the existing bounding box?
            if ((mChangeStates & Enums.ChangeStates.BoundingBox_TranslatedOnly & Enums.ChangeStates.BoundingBoxDirty) == Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly)
            {
                // TODO: test the above & test to see if it only triggers the following code
                // if just BoundingBox_TranslatedOnly is set
                DisableChangeFlags(Enums.ChangeStates.BoundingBox_TranslatedOnly);

                mBox.Max -= mTranslationDelta;
                mBox.Min -= mTranslationDelta;
                mSphere = new BoundingSphere(mBox);
            }
            else if (mChildren != null && mChildren.Count > 0)
            {
               	mBox.Reset();
                
                BoundingBox childboxes = BoundingBox.Initialized();
                for (int i = 0; i < mChildren.Count; i++)
                {
                    // NOTE: It was always a mistake for a NON REGION Entity.cs derived class
                    // to include the bounding volume of child _ENTITIES_ (child Models and ModelSelectors are OK). 
					// The SceneNode's are  responsible for Hierarchical bounding volumes and Entities bounding volumes are
                    // only for themselves and never include their child entities.
                    //
                    // NOTE: We combine Model/ModelSelector children boxes. We DO NOT combine child _ENTITIES_. (again
                    // only Models/ModelSelectors)  It is EntityNode sceneNodes responsibility to use hierarchical volumes that
                    // include child EntityNodes.
                    if (mChildren[i] is Model || mChildren[i] is ModelSelector) 
                    {
                        BoundingBox transformedChildBox;

                        // July.9.2023 - fixed issue with axial billboard bounding boxes not updating properly
                        if (mChildren[i] is Model && ((Model)mChildren[i]).Geometry is Billboard)
                        {
                            Billboard bb = (Billboard)((Model)mChildren[i]).Geometry;

                            if (bb.AxialRotationEnable)
                            {
                                // Feb.11.2024 - commented out all the axialbillboard rotation.  It wont create a proper rotation matrix properly without a proper billboard position
                                // todo: also note that our quaternion to euler for displaying in the plugin is broken.  It ruins the rotation. The workaround is not to modify the rotatino in the plugin after initially setting it.

                                // Vector3d axis = (this.DerivedRotation * ((Model)mChildren[i]).Rotation).Up();
                                // March.11.2024 - we don't need or want the Model.Rotatino.Up().  
                                Vector3d axis = this.DerivedRotation.Forward();
                                Matrix axialBillBoradRotationMatrix = Matrix.CreateAxialBillboardRotationMatrix(axis,
                                                                                  Vector3d.Zero(),
                                                                                  Vector3d.Zero());


                                Matrix tmat = Matrix.CreateTranslation(((Model)mChildren[i]).DerivedTranslation);
                                Matrix smat = Matrix.CreateScaling(((Model)mChildren[i]).DerivedScale);
                                //Matrix tmat = Matrix.CreateTranslation(mDerivedTranslation);
                                //Matrix smat = Matrix.CreateScaling(mDerivedScale);

                                axialBillBoradRotationMatrix = smat * axialBillBoradRotationMatrix * tmat; // this.RegionMatrix;
                                transformedChildBox = BoundingBox.Transform(((BoundTransformGroup)mChildren[i]).BoundingBox, axialBillBoradRotationMatrix); // this.RegionMatrix);
                            }
                            else // a regular billboard such as a Star
                                transformedChildBox = BoundingBox.Transform(((BoundTransformGroup)mChildren[i]).BoundingBox, this.RegionMatrix); // todo: the billboard's Model could have a scaling, shouldnt we be using the ((Model)mChildren[i]).RegionMatrix?
                        }
                        else if (mChildren[i] is Lights.Light)
                            continue;
                        else

                            // note: model's are transformed by their RegionMatrix and not the RegionMatrix of this Entity.  
                            // But what do we do here when a ModelSelector has individual model's each with potentially different RegionMatrices?  
                            // Since if the ModelSelector is not transforming each invidiaul sub-model by it's RegionMatrix and it's not happening here either
                            // then it's not happening anywhere.  I suspect we should be
                            // calling to "Select()" all models relevant and independantly transforming and combining the resulting boxes.
                            transformedChildBox = BoundingBox.Transform(((BoundTransformGroup)mChildren[i]).BoundingBox, ((BoundTransformGroup)mChildren[i]).RegionMatrix);

                        childboxes.Combine(transformedChildBox);
                    }
                    //else if (mChildren[i] is Entity)
                    //{
                    //    // NOTE: we do NOT include child entities here.  Leave that to EntityNode.cs
                   //     childboxes = BoundingBox.Combine(childboxes, ((BoundTransformGroup)mChildren[i]).BoundingBox);
                    //}
                }
                mBox.Combine(childboxes);
                DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty | Enums.ChangeStates.BoundingBox_TranslatedOnly);
            }

            if (mBox == BoundingBox.Initialized()) return; // if the box still hasn't been set

            mSphere = new BoundingSphere(mBox);
            double radius = mSphere.Radius;

            // TODO: with regards to planets, we could hardcode this such that
            // it's always visible within half region range for worlds, and 1/8th region range for moons
            // the problem is, in order to render even the icon for the world, it must pass
            // the cull test.   however, if we to treat it properly like a HUD item, we would
            // only find the worlds we detected via sensors (including visual sensors (telescopes and eyeballs))
            // In this way, the world would cull, but still show up in the hud.
            // This way the hud would independantly query for the nearby worlds and generate
            // proxies for them.
            // 
            // http://astrobob.areavoices.com/2012/01/05/what-would-the-sun-look-like-from-jupiter-or-pluto/
            // 30 arc minutes equal 1/2 degree or the diameter of the sun and moon. 
            double angleDegrees = 0.5d;  
            double angleRadians = Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS * angleDegrees;
            // http://www.bautforum.com/showthread.php/106931-Calculating-angular-size
            // sin(angle/2) = radius/(distance+radius)
            _maxVisibleDistanceSq = radius / System.Math.Sin(angleRadians * .5); // / Radius;
            _maxVisibleDistanceSq *= _maxVisibleDistanceSq;

            
            //_maxVisibleDistanceSq = float.MaxValue;
            // below calc is same as above....hrm...
            double distance = radius / Math.Tan(angleRadians / 2d);
            
            
            distance *= distance;
        }
        #endregion

        #region IEntitySystemSubscriber members
        public void Subscribe (Keystone.Simulation.IEntitySystem system)
        {
        }
        #endregion
        
        #region IFXSubscriber Members // TODO: is most of the FXprovider stuff now really relevant for Entity or should move to Models? 
        // and what we think of as IFXProviders for entities maybe should be IEntitySystems? like stardigest or a world economic simulation
        protected List<IFXProvider> _providers = new List<IFXProvider>();
        protected FXSubscriberData[] _subscriberData = new FXSubscriberData[System.Enum.GetNames(typeof (FX_SEMANTICS)).Length];
        protected bool _inFrustum;

        public IFXProvider[] FXProviders
        {
            get { return _providers.ToArray(); }
        }

        // typenames of all the providers to be subscribed too (for deserialization purposes)
        // we can start with a count and then itterate through typename0='bleh'  as far as deserializing the attributes
        // TODO: this is i think supposed to be a string representation
        public string[] Providers
        {
            get { return null; } //  throw new NotImplementedException(); }
            set { } // throw new NotImplementedException(); }
        }

        public FXSubscriberData[] FXData
        {
            get { return _subscriberData; }
            set { _subscriberData = value; }
        }

        public bool InFrustum
        {
            get { return _inFrustum; }
            set
            {
                _inFrustum = value;
                foreach (FXSubscriberData data in _subscriberData)
                    if (data != null) data.InFrustum = value;
            }
        }

        public void Subscribe(IFXProvider fxProvider)
        {
            fxProvider.Register(this);
        }

        public void UnSubscribe(IFXProvider fxProvider)
        {
            fxProvider.UnRegister(this);
        }

        public void UnSubscribeAll()
        {
            foreach (IFXProvider p in _providers)
            {
                p.UnRegister(this);
            }
        }
        #endregion
        
    }
}