using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using KeyScript.Rules;
using Keystone.Elements;
using Keystone.Extensions;
using Keystone.IO;
using Keystone.Resource;
using Keystone.Types;

namespace Keystone.DomainObjects
{
    /// <summary>
    /// Eventually I'll rename this to EntityScript.
    /// EntityScript node is a resource of an Entity and so is actually loaded
    /// by the Entity and is NOT loaded by SceneReader.cs during deserialization. 
    /// In every other respect it acts like a typical node that can be added/removed
    /// from the KeyEdit front end.  However, this node is skipped during serialization
    /// and deserialization and is loaded by the Entity directly.
    /// </summary>
    /// <remarks>
    /// This is a shareable node and thus can never contain per instance data.
    /// This also allows us to treat the node as an Entity resource that each
    /// entity can instantiate and AddChild() to itself.
    /// </remarks>
    public class DomainObject : Node, IPageableTVNode 
    {
        
        protected uint mFlags; // component specific flags set by script.  NOT the same as custom flags which are per entity instance and reside in Entity
        internal Settings.PropertySpec[] mCustomProperties; // TODO: This should be moved out of the script, but... then they can't be shared with only values hosted elsewhere...
                                                            //       This is a problem... since i do anticipate that behavior trees will want to be able to query some of these values
                                                            //       which it technically can do, just through the Entity and not directly... but it still leaves us with multiple data stores
                                                            //       So it would be nice if we could store the data here but not necessarily have any script loaded with it... just data and rules
                                                            //       validation.  But that also means i wouldn't want it to be a Node or IPageableTVNode... so the question is, how do i
                                                            //       share customproperties and production 
                                                            //       Mainly, we need to get mCustomProperties out of here...  and then... do we just not share them?
                                                            //       What if the "PropertySpec[]" was only generated when we wanted to display the values and not for just storage by kvp?
                                                            //       The key to PropertySpec is we store the type which allows us to generically iterate them without knowing the type 
                                                            //       ahead of time.  This is useful for propertyGrid, serialization INCLUDING database serialization.  
                                                            //       But this concern is seperate from merging these custom fields with those needed by BehaviorTrees and
                                                            //       the main issue there is whether we must stop sharing custom properties between other entity's of the same definition.
                                                            //       Let's assume we have a UserData object, how could it be shared? A naming convention based on the resourceID if applicable?
                                                            //       Entities, DomainObject and BehaviorTrees could then all have access to Entity.UserData 
                                                            //       There isn't really a problem with storing the user data seperately.  We could still share the definitions....
                                                            // TODO: WAIT.  "knowledge" data is a different sort of data than custom properties.  "knowledge" is 
                                                            //       runtime scratch data, custom properties though are more about definition and include things like "hitpoints, armor DR/PD, cost, weight, craftsmenship, etc"
                                                            //       It is true that id still like to be able to have custom properties without an actual script file... so there could still be some value in
                                                            //       seperating _if_ we can still share that definition.  But maybe best is to just always require definitions be defined in script.  The only
                                                            //       reason to not use a script is just not wanting to bother with creating it!  which isn't a good reason by itself.
                                                            // TODO: so it seems then that we should continue with UserData as seperate type of data.  

        // the rules can be created by the script upon initialization
        // also these rules can be shared for all Entities that use this DomainObject.  That is the
        // nice aspect of these DomainObjects
        protected Dictionary<string, RuleSet> mRuleSet;  


        //internal int[,] mFootPrint; // having FootPrint here allows us to share the data. //--TODO: footprint is obsolete, now we use shareable CellFootprint node
                
        protected CSScriptLibrary.CSScript.LoadedScript mScript;
        protected Dictionary<string, CSScriptLibrary.MethodDelegate> mMethods;
        private readonly object mSyncRoot;
        protected PageableNodeStatus _resourceStatus;


        // these specific types of things, are app specific custom properties
        // that can take the form of custom flags.
        // But what about Machines that have fields for Production and Consumption?  These should
        // just be part of a common set of interfaces that scripts for all "machine like"
        // domina objects should use right?
        //
        // The primary issue is, when the entity is loaded by the PlacementTool
        // if that loaded entity does not have a specific interface (whcih the placementtool 
        // being in Interior placement mode will look for) it will cancel.
        // Properties like the footprint, orientation, dimensions will be read after the object
        // is loaded.
        // As far as sorting objects in the Asset Browser, this must rely on a predetermined 
        // group.  Walls, Floors & Ceilings,  Doors & Windows, 
        // 
        //private ComponentType ComponentType;      // TODO: type as noted above i think is irrelevant.   Type is inferred from the products and consumption capabilities of htis component
        //private ComponentDimension Dimensions;    // TODO: for now we'll ignore this while testing floors and walls
        //private uint[] mFootprint;
        //private ComponentOrientation Orientation;  // this is entity instance specific, should not be here because DOmainObjects are definitions and are shared



        internal DomainObject(string id) : base(id) 
        {
            Serializable = false; // DomainObject's are basically just script nodes and they are shareable but not serializable. 
                                  // They can only be instanced by the parent Entity which saves a ref path.
                                  // This change will require our saved prefabs be updated
            Shareable = true; 
            mSyncRoot = new object(); 
        }

       

        #region ITraversable members
        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            throw new NotImplementedException();
            return target.Apply(this, data);
        }

        internal override Keystone.Traversers.ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }
        #endregion 


        #region ResourceBase members
        /// <summary>
        /// DomainObject is SKIPPED on serialization/deserialization because as a predominantly SCRIPT NODE
        /// it is loaded by the Entity.  So how do we share our footprint?
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
//            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
//            Settings.PropertySpec[] properties = new Settings.PropertySpec[1 + tmp.Length];
//            tmp.CopyTo(properties, 1);
//
//            // TODO: domainobjects are not serializable so these properties are basically no good
//            //       for that sort of thing.  CustomProperties 
//            // see Keystone.Helpers.ExtensionMethods.ParseValue()
//            // as you can see int[,] is not yet supported for deserialization from xml
//            // do we store the path to the footprint instead? or do we
//            // store the int[,] as a encoded binary string where we start with the length
//            // of both dimensions and then base64 encode 
//            // http://www.codeproject.com/Articles/80289/Saving-Image-Data-in-an-XML-File?msg=3468583#xx3468583xx
//            // Convert.ToBase64String() and Convert.FromBase64String()
//            // TODO: obsolete? footprint is now a node of it's own but one thta is loaded as a shareable resource
//            properties[0] = new Settings.PropertySpec("footprint", typeof(int[,]).Name); // footprint is shared by all Entities using this DomainObject
//
//            if (!specOnly)
//            {
//                properties[0].DefaultValue = mFootPrint;
//            }
//
//            return properties;
			return base.GetProperties(specOnly);
        }

        // http://stackoverflow.com/questions/6644668/mixins-with-c-sharp-4-0
        // Prefabs (unity) - Per GameObject / Definitions (gamebryo) - Per Node (non resource nodes)
        // Instances       / Entities w/ Mixins
        //                 / The "link" is node types i supsect Behavior/Physics/Script/Model/Appearance
        // 
        // Overriding properties?
        // first of all, there is obviously a distinction between a SHARED node (can have multiple parents)
        // and one that can have custom property values and so by definition cannot be shared and the concept
        // of a "linked" node where 
        // However, by storing all the "values" away from the reference nodes so that they can be overriden
        // provides a way of doing that while allowing even more nodes to be shared.
        // However, this seems complicated unless we prevent the core hierarchy from being modified... 
        // However the Minx method i think makes that a bit more sane... since now we are defined by node and
        // removing a node simply decouples the mixin.
        //
        // So let's think in terms of the Unity way... example is a chair
        // - Chair Entity - references "ChairEntity.KGBEntity" <-- prefab is a file and first instance is stored in "ActiveLibrary"
        //                                                     <-- prefab struct object in ActiveLibrary also tracks all instances of it
        //   - Child Entities (can contain instance data, can reference a Definition?
        //   - Script (resource)
        //   - Animations
        //   - Behavior (tree)
        //   - ModelSelector
        //   - Model (model can contain instance data, can reference a Definition?)
        //         - Appearance (appearance can contain instance data, can reference a Definition?)
        //              GroupAttribute[](GroupAttribute can contain instance data, can reference a Definition?)
        //               Material (resource)
        //               Shader (resource)
        //               Layer (layers can contain instance data, can reference a Definition?)
        //                  Texture (resource)
        //         - Mesh (resource)
        //
        // Our overrides are basically our "instance data" however we do not provide a way
        // of canceling an override and having it automatically use the default value
        // because we are not maintaining links to the prefabs
        // - Adding/Removing of a NODE _ON_AN_INSTANCE_ will break the prefab connection if the entity has a prefab connection already
        //   - This goes for all the way up the chain of nested entities to where a prefab reference is assigned.
        //   and it recurses through until root.
        // - Adding/Removing of a Node on a PREFAB should update all instances yes? This is how we could globally change
        //   a script or change a mesh, or whatever.  
        //      - during serialization/deserialization the prefab's structure should take precedence and user overriden properties
        //        comes second.  So consider how does this look in a scene xml file?!
        //          - at one point, I think we were considering not using prefabs... that since if we wanted global changes
        //          we could simply replace the shared resources instead.  But this of course requires that we change the
        //          shared res and use the same IO path and doesnt allow us to just change to an existing alternative of a diff
        //          io path and have it automatically update all use cases.
        //   - TODO: but what about a material?  You want a prefab but you want to be able to change the material
        //           and not break the connection... this seems not very compatible with how we handle materials as nodes
        //           and not as properties
        // - so i sort of like the idea of "Mixins" better, but the key is thinking how we serialize/deserialize
        //   in such a way as that if we change the prefab and then load the scene, any entities using that changed prefab
        //   will update based on the prefab.
        // - the Gamebryo "Mixins" seems to reflect more the ideology behind Scene Graph nodes "DEF" attribute for nodes
        //   where nodes could also override properties.
        //      - the idea is that if a DEF is referenced by a Node, during scene reading we start to read in that DEF node first
        //      and then we resume reading in any overriden attributes.
        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

//            for (int i = 0; i < properties.Length; i++)
//            {
//                if (properties[i].DefaultValue == null) continue;
//                // use of a switch allows us to pass in all or a few of the propspecs depending
//                // on whether we're loading from xml or changing a single property via server directive
//                switch (properties[i].Name)
//                {
//                    case "footprint":
//                        object tmp = properties[i].DefaultValue;
//                        if (tmp == null)
//                        {
//                        	// TODO: isn't this un-used?  Don't i now use a CellFootprint which is a shareable node?
//                            mFootPrint = null;
//                            continue;
//                        }
//                        mFootPrint = (int[,])tmp;
//                        break;
//                    // TODO: should emission and product flags be save-able or only assignable from script?
//                    //case "emission_tx_flags":
//                    //    mEmissionTransmissionFlags = (uint)properties[i].DefaultValue;
//                    //    break;
//                    //case "emission_rx_flags":
//                    //    mEmissionReceptionFlags = (uint)properties[i].DefaultValue;
//                    //    break;
//                    //case "productionflags":
//                    //    mProductionFlags = (uint)properties[i].DefaultValue;
//                    //    break;
//                    //case "consumptionflags":
//                    //    mConsumptionFlags = (uint)properties[i].DefaultValue;
//                    //    break;
//                }
//            }
 

            // TODO: uh... what's up with initalizing rules var here?  its a local var
            // so i think the idea was that during SetProperties we'd potentially validate
            // against rules and then store all broken results in here and... hrm
            List<KeyScript.Rules.Rule> broken = new List<KeyScript.Rules.Rule>();
            
            // TODO: TEMP HACK make sure Serializable is false even after loading previous file format node flags
            Serializable = false; // DomainObject's are basically just script nodes and they are shareable but not serializable. 
            // They can only be instanced by the parent Entity which saves a ref path.
        }
        #endregion

        public CSScriptLibrary.CSScript.LoadedScript Script { get { return mScript; } }




        public int GetCustomPropertyIndex(string propertyName)
        {
            if (mCustomProperties == null || mCustomProperties.Length == 0)
                return -1;

            for (int i = 0; i < mCustomProperties.Length; i++)
                if (mCustomProperties[i].Name == propertyName)
                    return i;

            return -1;
        }
        /// <summary>
        /// Custom Properties are completely independant of our normal node Properties.
        /// Thus they do not use the standard Get/SetProperties functions.  Changing
        /// a DomainObject's properties from Plugin infers we're actually only changing
        /// the Custom Properties.
        /// IMPORTANT: This only stores the propertyspec definitions but NOT the values.
        /// The values which are entity specific are stored in the entities.
        /// </summary>
        /// <remarks>The CustomProperties are loaded via the script Initialize() and never via XML.</remarks>
        public Settings.PropertySpec[] CustomProperties
        {
            get 
            { 
            	return mCustomProperties; 
            }
            
            set 
            { 
            	mCustomProperties = value; 
            }
        }

        // currently only called by IEntityAPI - not sure if even used... i think our customflags are actually set in Entity's customFlags... not sure
        public virtual bool GetFlagValue(uint bitPosition)
        {
            return ((mFlags & bitPosition) == bitPosition);
        }

        public virtual void SetFlagValue(uint bitPosition, bool value)
        {
            if (value == true)
                mFlags |= bitPosition;
            else
                mFlags &= ~bitPosition;
        }



        // NOTE: DomainObjects don't respond to most flags related to geometry loading, transforms,
        // entities attached/detached, SceneNode related.  
        protected override void PropogateChangeFlags(global::Keystone.Enums.ChangeStates flags, Enums.ChangeSource source)
        {
            // NOTE: DomainObject is not a Group node so will never have ChildNodeAdded or ChildNodeRemoved 
            // flags to respond to.

            if ((flags & Keystone.Enums.ChangeStates.EntityScriptLoaded) != 0)
            {
                NotifyParents(Keystone.Enums.ChangeStates.EntityScriptLoaded);
                // NOTE: We cannot disable this if this DomainObject has not been added to a parent yet.
                //DisableChangeFlags(Keystone.Enums.ChangeStates.DomainScriptLoaded); // no need to persist this flag here
            }
            if ((flags & Keystone.Enums.ChangeStates.DomainScriptUnloaded) != 0)
            {
                NotifyParents(Keystone.Enums.ChangeStates.DomainScriptUnloaded);
                //DisableChangeFlags(Keystone.Enums.ChangeStates.DomainScriptUnloaded); // no need to persist this flag here
            }

            // OBSOLETE - never use switch for testing for certain flags.
            //switch (flags)
            //{
            //    case Keystone.Enums.ChangeStates.ScriptLoaded:
            //        // when the script is loaded, we know it's connected to an entity?  Is 
            //        // that always true?  Hrm...  if not we'd have to check both for initialized
            //        // and for parent added and then if either event happens, we check for whether
            //        // Entity.IsDomainObjectInitialized flag (and this flag is per Entity because
            //        // entities share domainObjects and need to receive the CustomProperties
            //        // So in fact, the above assertion is FALSE.  We can't just initialize on
            //        // Script loaded.  We have to initialize for each entity after the script
            //        // is loaded
            //        // run the initialization script.  
            //        // TODO: I think the parent should call initialize... but the one thing
            //        // is the parent doesnt get the ID of the child... why didnt i include that
            //        // 
            //        object result = Execute("Initialize", new object[] { this.ID });
            //        NotifyParents(Keystone.Enums.ChangeStates.ScriptLoaded);
            //        break;
            //    case Keystone.Enums.ChangeStates.ScriptUnloaded:
            //        NotifyParents(Keystone.Enums.ChangeStates.ScriptUnloaded);


            //        break;

            //    default:
            //        break;
            //}
        }

        #region Events and Rules
        // this Dictionary is exclusively for shared instances of this Script when their OWN PROPERTY changes. 
        // It is not for other Entities (EVEN IF THEY ARE USING THE SAME SCRIPT!) that have Subscribed to 
        // another Entities PropertyChangedEvent.  
        Dictionary<string, KeyScript.Events.PropertyChangedEventDelegate> mPropertyChangedEvents;

        public Dictionary<string, KeyScript.Events.PropertyChangedEventDelegate> PropertyChangedEvents
        {
            get { return mPropertyChangedEvents; }
        }

        private string[] mEventNames;
        public string[] EventNames
        {
            get { return mEventNames; }
        }


        public void EventAdd (string eventName, KeyScript.Events.EventDelegate eventHandler)
        {
            if (string.IsNullOrEmpty(eventName)) throw new Exception("Script.cs.EventAdd() - eventName cannot be Null or Empty.");

            if (mEventNames != null)
                for (int i = 0; i < mEventNames.Length; i++)
                    if (mEventNames[i] == eventName)
                        throw new ArgumentException("Script.cs.EventAdd() - Duplicate event '" + eventName +"'");

            mEventNames = Keystone.Extensions.ArrayExtensions.ArrayAppend(mEventNames, eventName);
        }


        // For now, all events are "OnChange" events of the custom property specified.
        // Also remember, these are just explicit declarations.  The actual serializeable
        // Routes are stored in Entity.
        public void AddPropertyChangedEvent(string propertyName, KeyScript.Events.PropertyChangedEventDelegate e)
        {
            if (mPropertyChangedEvents == null) mPropertyChangedEvents = new Dictionary<string, KeyScript.Events.PropertyChangedEventDelegate>();

            KeyScript.Events.PropertyChangedEventDelegate existing;
            bool eventExists = mPropertyChangedEvents.TryGetValue(propertyName, out existing);

            if (eventExists == false)
            {
                mPropertyChangedEvents.Add(propertyName, e);
            }
            else 
            {
            	throw new ArgumentException ("Script.cs.AddPropertyChangedEvent() - Duplicate key");
            }
        }
        
        // specifically PropertyChangedEvent.  Since domain object script is shareable
        // we must specify which entity
        public bool PropertyChangedEventRaise (string entityID, string propertyName)
        {
        	if (mPropertyChangedEvents == null) return false;
        	
        	KeyScript.Events.PropertyChangedEventDelegate e = null;
        	if (mPropertyChangedEvents.TryGetValue (propertyName, out e))
        	{
#if DEBUG 
                // make sure entityID matches that of one of the parent nodes
        		bool validEntityID = false;
                for (int i = 0; i < mParents.Count; i++)
                    if (((Node)mParents[i]).ID == entityID)
                    {
                        validEntityID = true;
                        break;
                    }
        		
        		System.Diagnostics.Debug.Assert (validEntityID);
#endif
        		e.Invoke (entityID);
                return true;
        	}
            return false;
        }
        
        public void AddRule(string propertyName, Rule rule)
        {
            if (mRuleSet == null) mRuleSet = new Dictionary<string, RuleSet>();

            RuleSet set;
            bool setExists = mRuleSet.TryGetValue(propertyName, out set);

            if (setExists == false)
            {
                set = new RuleSet();
                mRuleSet.Add(propertyName, set);
            }

            set.AddRule(rule);
        }

        public bool RulesValidate(string entityID, string propertyName, object value, out int[] brokenCode)
        {
            brokenCode = null;
            if (mRuleSet == null) return true;
            
            if (!(mRuleSet.ContainsKey(propertyName))) 
                return true;

            // get array of rules governing this property name
            Rule[] rules = mRuleSet[propertyName].Rules;

            if (rules == null || rules.Length == 0)
                return true;

            bool isBroken = false;
            brokenCode = new int[rules.Length];
            // run all rules for this property and store any error codes
            for (int i = 0; i < rules.Length; i++)
            {
                // rules are run using a delegate that was specified in the script
                // that is why no script.Execute() has to be called
                bool result = rules[i].Validate(entityID, new object[] { value });

                if (!result)
                {
                    brokenCode[i] = rules[i].ErrorCode;
                    isBroken = true;
                }
            }

            if (isBroken == false) brokenCode = null;
            return isBroken == false;
        }
#endregion

        /// <summary>
        /// Returns an error description string from integer code.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public string GetError(int code)
        {
            // TODO: DomainObject should load a static localized error string table from disk
            return "";
        }


        // TODO: individual scripts should be enabled/disable-able on a per Entity basis.
        // In our DomainObject viewer when we see the available scripts, a checkbox to enable/disable
        // on a per entity basis should be allowed.
        // TODO: does this mean each Model should have it's own seperate scripts for shader parameter
        // OnRender() scripted methods?  "updateShaderParameters_forward" "updateShaderParameters_deferred"
        // 
        internal object Execute(string eventName, object[] args)
        {
            lock (mSyncRoot) // TODO: domainobject is shared now and we shouldn't lock it's Execute since it's scripts are using seperate data per Entity anyways!  We only need to lock for resource loading
            {
                if (!Core._Core.ScriptsEnabled) return null;
                
                if (_resourceStatus == PageableNodeStatus.Loading) return null; // in process of loading, we'll return and try later
	
                if (_resourceStatus == PageableNodeStatus.Error)
                {
                	if (this.Parents == null || this.Parents[0] == null) return null; // <- should never be null but...
                    string location = ((Node)this.Parents[0]).TypeName + " " + eventName;
                    System.Diagnostics.Debug.WriteLine ("DomainObject.Execute() - ERROR: Cannot execute script '" + location + "'");
                    return null; // BehaviorResult.Fail;
                }
                if (mMethods == null) return null; // BehaviorResult.Fail;

                System.Diagnostics.Debug.Assert (mScript != null && mScript.asm != null, "DomainObject.Execute() - Script should be loaded but is not!");
                
                // we must not allow execution to continue if not loaded because it is possible
                // to try and execute OnRender() or OnUpdate() from Entity before it's DomainObject
                // has finished loading and we don't want to rely on that Entity to check it's own
                // resourceStatus
                if (_resourceStatus != PageableNodeStatus.Loaded) throw new Exception ();
                
               return ExecuteInternal (eventName, args);
            }
        }

        private object ExecuteInternal (string eventName, object[] args)
        {
        	CSScriptLibrary.MethodDelegate result;
            if (!mMethods.TryGetValue(eventName, out result))
            {
                // TODO: our scripts dont return BehaviorResults, only our
                // nodes do.  It's up to our nodes to define when
                // a particular script is Running or Success/Completed
                // not our ScriptNodes... i need to verify that is right way to implement
                // and that it makes sense from the BehaviorTree perspective
                // WAIT, how on earth is for instance a "MoveTo" script supposed to know
                // when the script has completed moving an entity to a waypoint without
                // the script notifying?
                //System.Diagnostics.Debug.WriteLine("Script.ExecuteInternal() - Method '" + eventName + "' not found!");
                return null; // BehaviorResult.Success;
            }
            // TODO: here i do not allow calling of methods that dont already exist
            //       one reason is how would i know when to stop trying to find that method if it doesnt exist?
            // TODO: not true. i do allow calling of methods that dont exist and ijust return null.
            //       Is this "ok"?  Not every script needs every method. hmmm.

            try
            {
                if (args == null || args.Length == 0)
                {
                    // call overloaded version that accepts no args
                    // or maybe throw exception if null args passed is better?
                    // or maybe just returning error is best?
                    return result();
                    //throw new ArgumentNullException();
                    // BehaviorResult.Error_Script_Invalid_Arguments;
                }
                else
                {
                    return result(args);
                    // BehaviorResult.Success;
                    //return (BehaviorResult)result(args);
                }
            }
            catch (InvalidCastException ex)
            {
                // NOTE: This is usually when in our custom properties defined in script we expect
                // a certain type but end up storing the value as a "string" and so we end up trying to cast
                // a string to a double or a string to bool.  The fix for this is to
                // make sure when we assign values to these custom properties we are always assigning them
                // of the proper type.
                Debug.WriteLine(string.Format("Script.Execute() -- Error executing script '{0}'.  {1}", eventName, ex.Message));
                Enable = false; // disable script
                _resourceStatus = PageableNodeStatus.Error;
                return null; // BehaviorResult.Error_Script_Exception;
            }
            catch (Exception ex)
            {
            	Debug.WriteLine(string.Format("Script.Execute() -- Error executing script '{0}'. MESSAGE= {1}.  STACKTRACE= {2}", eventName, ex.Message, ex.StackTrace.ToString()));
                Enable = false; // disable script
                _resourceStatus = PageableNodeStatus.Error;
                return null; // BehaviorResult.Error_Script_Exception;
            }
        }
        

        #region IPageableTVNode Members
        public object SyncRoot { get { return mSyncRoot; } }

        public int TVIndex
        {
            get 
            {
                lock (mSyncRoot)
                    if (mScript == null || mScript.asm == null) return -1; else return 0; 
            }
        }

        public bool TVResourceIsLoaded
        {
            get 
            {
                lock (mSyncRoot)
                    return mScript != null && mScript.asm != null; 
            }
        }

        public string ResourcePath
        {
            get { return _id; }
            set { throw new Exception ("DomainObject.ResourcePath - cannot change DomainObject resource path since it's same as node.ID"); } 
        }

        public PageableNodeStatus PageStatus { get { return _resourceStatus; } set { _resourceStatus = value; } }

        public void UnloadTVResource()
        {
        	// TODO: UnloadTVResource()
        }
                
        public void LoadTVResource()
        {
           
            KeyCommon.IO.ResourceDescriptor descriptor = new KeyCommon.IO.ResourceDescriptor(_id);
            
            string code = null;
            try
            {
                if (descriptor.IsArchivedResource)
                {
                    string zipFilePath = Keystone.Core.FullNodePath(descriptor.ModName);
                    code = KeyCommon.IO.ArchiveIOHelper.GetTextFromArchive(descriptor.EntryName, "", zipFilePath);
                }
                else
                {
                    string filePath = System.IO.Path.Combine(Keystone.Core._Core.ModsPath, descriptor.EntryName);
                    code = System.IO.File.ReadAllText(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("DomainObject.LoadTVResource() - Error reading script source text. " + ex.Message);
            }
            

            if (string.IsNullOrEmpty(code)) goto error;

            // TODO: error check when trying to load the same code using same path twice
            // i know this errors but im not sure why it's not trying to recycle the existing and
            // perhaps notify of the error,... something ACTUALLY i think it errors more specifically
            // if somehow due to some loading error on a previous attempt, its not locked and you 
            // cant change it with a subsequent successful laod... i dunno, needs tons of error handling
            System.Diagnostics.Debug.Assert (Core._Core.ScriptLoader != null, "DomainObject.LoadTVResource() - ERROR: ScriptLoader is NULL.  Ensure user_assemblies.txt path was correct.");
            CSScriptLibrary.CSScript.LoadedScript loadedScript = Core._Core.ScriptLoader.LoadCode(code, _id, true);

            if (loadedScript == null ) goto error;

            int capacity = 19;
            Vector3d zeroVec = Vector3d.Zero();
            Vector3i zeroIVec = Vector3i.Zero();
            Dictionary<string, object[]> supportedMethodSignatures = new Dictionary<string, object[]>(capacity);
            supportedMethodSignatures.Add("Initialize", new object[] {""});
            supportedMethodSignatures.Add("InitializeEntity", new object[] {""});
            supportedMethodSignatures.Add("UnInitializeEntity", new object[] { "" });
            supportedMethodSignatures.Add("UnRegisterChild", new object[] {"", ""});
            supportedMethodSignatures.Add("RegisterChild", new object[] {"", ""});
            supportedMethodSignatures.Add("OnAddedToParent", new object[] { "", "" });
            supportedMethodSignatures.Add("OnRemovedFromParent", new object[] { "", "" });
            supportedMethodSignatures.Add("OnParentChanged", new object[] { "", "", "" }); // entityID, parentID, previousParentID
			supportedMethodSignatures.Add("OnRegionChanged", new object[] { "", "", "" }); // entityID, regionID, previousRegionID

            // new object[] {} with no args works, verified can't just pass null (though i could write a custom overload in CSScript to allow so)
            supportedMethodSignatures.Add("QueryPlacementBrushType", new object[] { });
            supportedMethodSignatures.Add("QueryCellPlacement", new object[] { "", "", zeroVec, (byte)0 });

            supportedMethodSignatures.Add("QueryPlacement", new object[] { "", zeroVec, zeroVec, zeroVec });

            supportedMethodSignatures.Add ("OnSelected", new object[] {"", "", ""});
            supportedMethodSignatures.Add ("OnSelectionLost", new object[] {"", "", ""});
            supportedMethodSignatures.Add("OnCelledRegion_DataLayerValue_Changed", new object[] { "", "", (uint)0, new object()});
            supportedMethodSignatures.Add("OnObserved_Value_Changed", new object[] { "", "", "", (uint)0, new object() });
            // NOTE: Validate is obsolete here. We use Rules delegates that point to the scripted 
            // methods instead. "Validate" is however used tentatively still for Behavior.Action scripts. 
            // See Keystone\Behaviors\Action\Script.cs
            // supportedMethodSignatures.Add("Validate", new object[] { "", "", new object() });
            supportedMethodSignatures.Add ("OnCustomPropertyChanged", new object[] {"", (int)0});
            supportedMethodSignatures.Add ("OnUpdate", new object[] {"", (double)0});
            supportedMethodSignatures.Add ("OnSelectModel", new object[] {"", "", "", (double)0});
            supportedMethodSignatures.Add ("OnRender", new object[] { "", "", new string[]{}, "", zeroVec, new Vector3d[]{}, (double)0 });  // contextID, entityID, modelIDs, shaderID, cameraSpacePosition, elapsedSeconds


            // UI
            supportedMethodSignatures.Add("GUILayout_GetMarkup", new object[] { "", new KeyCommon.Traversal.PickResultsBase() });
            supportedMethodSignatures.Add("GUILayout_LinkClick", new object[] { "", "" });
            supportedMethodSignatures.Add("GUILayout_ButtonClick", new object[] { "", "" });
            supportedMethodSignatures.Add("GUILayout_CheckBoxClick", new object[] { "", "" });
            supportedMethodSignatures.Add("GUILayout_TextBoxClick", new object[] { "", "" });
            supportedMethodSignatures.Add("GUILayout_TextBoxKeyPress", new object[] { "", "" });


            // triggers - NOTE: it does appear that in the call to GetSupportedMethods() below that if the method doesn't exist in the script, it is ignored and
            //           we don't just fail to load the script. Not every script needs to include Trigger events afterall.
            supportedMethodSignatures.Add("OnTriggerAreaEnter", new object[] { "", "" });
            supportedMethodSignatures.Add("OnTriggerAreaExit", new object[] { "", "" });

            // gameplay 
            supportedMethodSignatures.Add("IsAccessible", new object[] { "", "" }); // determines if an NPC can access this component (includes "doors", "locked computer consoles", etc)
            supportedMethodSignatures.Add("Use", new object[] { "", "" });          // have npc use the entityID


            mMethods = Core._Core.ScriptLoader.GetSupportedMethods(loadedScript, supportedMethodSignatures);
            if (mMethods != null)
            {
                mScript = loadedScript;
                try
                {
                	// "Initialize" is only called once per shared domain object.
                	// NOTE: we use private ExecuteInternal() here NOT public Execute() since 
					// we prevent public Execute() from running until PageStatus == Loaded 
					// and here we haven't finished loading yet!
                    object tmp = ExecuteInternal("Initialize", new object[] { this.ID });
                    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("DomainObject.LoadTVResource() - Error executing scripted method 'Initialize()'." + ex.Message);
                }

                SetChangeFlags(Keystone.Enums.ChangeStates.EntityScriptLoaded, Keystone.Enums.ChangeSource.Self);
                return;
                
            }
        error:
            _resourceStatus = PageableNodeStatus.Error;
            System.Diagnostics.Debug.WriteLine("DomainObject.LoadTVResource() - Error creating script");
            return;
        }


        public void SaveTVResource(string filepath)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IDisposable
        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            SetChangeFlags(Keystone.Enums.ChangeStates.DomainScriptUnloaded, Keystone.Enums.ChangeSource.Self);

            //if (mScript != null)
            //{
            //   mScript.as
            //}
            mMethods = null;
            mScript = null;
        }
        #endregion
    }
}
