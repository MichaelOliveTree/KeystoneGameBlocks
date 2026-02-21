using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;
using Keystone.CSG;
using Keystone.Elements;

namespace KeyEdit.Scripting
{
    // Gamebryo was an initial model for how we'd handle our scripting by having carefully exposed API's 
    // so that the scripts wouldn't need access to Keystone.dll objects and methods directly.
    // https://www.youtube.com/watch?v=ZStLU2BW3Zw
    public class EntityAPI : IEntityAPI
    {
        // TODO: question - ChangeProperty and others go through AppMain.mNetClient.SendMessage()
        // but if these functions are called from script, then we are already in our Update() 
        // ... however, assuming we update scripts (and we should) before we process message queue
        // then those messages will still get applied before next render so they wont be one frame behind

        #region Node Members
        void IEntityAPI.SetFlag(string nodeID, byte flag, bool value)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            node.SetFlagValue(flag, value);
        }

        void IEntityAPI.SetFlag(string nodeID, string flagName, bool value)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            node.SetFlagValue(flagName, value);
        }

        bool IEntityAPI.GetFlag(string nodeID, byte flag)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            return node.GetFlagValue(flag);
        }

        bool IEntityAPI.GetFlag(string nodeID, string flagName)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            return node.GetFlagValue(flagName);
        }

        object IEntityAPI.GetPropertyValue(string entityID, string propertyName)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(entityID);
            Settings.PropertySpec property = node.GetProperty(propertyName, false);
            return property.DefaultValue;
        }

        void IEntityAPI.SetPropertyValue(string entityID, string propertyName, object propertyValue)
        {
            Settings.PropertySpec spec = new Settings.PropertySpec();
            spec = new Settings.PropertySpec(propertyName, propertyValue.GetType());
            spec.DefaultValue = propertyValue;

            ChangeProperty(entityID, spec);
        }

        string IEntityAPI.GetTypename(string nodeID)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            if (node == null) return null;

            return node.TypeName;
        }

        /// <summary>
        /// UPDATE(Oct.31.2012) - EntityAPI are for Scripts not for plugins.  Scripts are trusted
        /// and are crc32d at start.  Scripts compute behaviors based on current state and 
        /// if a script computes a gravity force for a certain frame to a consumer, and then tries
        /// to update the .force property of that consumer, it must take place immediately!
        /// So the following should be obsolete thinking.
        /// OBSOLETE - per notes above, property changes must occur immediately when called from script.
        /// 
        /// Changing all properties indirectly through PropertySpec ensures many things
        /// - changeflags are sent on the nodes and trigger bounding box recalcs and such
        /// - all changes are thread safe since they are serialized after the scene update is done.
        /// - goes through command processor and so any plugin notifications are triggered there.
        /// </summary>
        /// <remarks>
        /// However we're undecided as to whether any client side scripts will have to send
        /// commands over the wire.  I think it's best to avoid this entirely.  Server side AI npc
        /// crew for instance would decide to fire some weapon and the client should be notified.
        /// Client should only run AI to interpolate things and keep the visual simulation in
        /// good approximation.  
        /// </remarks>
        /// <param name="nodeID"></param>
        /// <param name="spec"></param>
        private void ChangeProperty(string nodeID, Settings.PropertySpec spec)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            if (node == null)
            {
                System.Diagnostics.Trace.WriteLine("EntityAPI.ChangeProperty() -- Could not find node '" + nodeID + "'");
                return;
            }

            node.SetProperties(new Settings.PropertySpec[] { spec });

            // Oct.31.2012 - OBSOLETE code below - Scripts compute many things based on current state such as
            // forces from gravity from stars and worlds or engine thrust and so these forces must
            // be assigned to the entity immediately and not in some unknown frame in the future after
            // the network trip has completed.

            //KeyCommon.Messages.Node_ChangeProperty changeProperty = new KeyCommon.Messages.Node_ChangeProperty();
            //changeProperty.SetFlag(KeyCommon.Messages.Flags.SourceIsClientScript);
            //changeProperty.NodeID = nodeID;
            //changeProperty.Add(spec);

            //AppMain.mNetClient.SendMessage(changeProperty);
        }

        private void ChangeProperty(string nodeID, string propertyName, Type propertyType, object value)
        {
            // using the property spec allows us to validate using our Rules engine as well
            // as utitilize existing code for notifying plugins
            Settings.PropertySpec spec = new Settings.PropertySpec();
            spec = new Settings.PropertySpec(propertyName, propertyType);
            spec.DefaultValue = value;

            ChangeProperty(nodeID, spec);
        }

        string IEntityAPI.GetName(string nodeID)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            if (node == null)
            {
                System.Diagnostics.Trace.WriteLine("EntityAPI.GetName() -- Could not find node '" + nodeID + "'");
                return null;
            }
            return node.Name;
        }
        #endregion


        /// <summary>
        /// AddRule is called by the actual Entity Script csscript module
        /// to assign the validation rules for properties that are created in the script
        /// (eg. the validation rule delegate functions are actually written in the script!)
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="rule"></param>
        void IEntityAPI.AddRule(string scriptID, string property, KeyScript.Rules.Rule rule)
        {
            Keystone.DomainObjects.DomainObject script = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(scriptID);
            if (script == null)
            {
                System.Diagnostics.Debug.WriteLine("EntityAPI.AddRule() - Script node does not exist in Repository.");
                return;
            }
            script.AddRule(property, rule);
        }

#region Events
        void IEntityAPI.EventAdd(string scriptID, string eventName, KeyScript.Events.EventDelegate eventHandler)
        {
            Keystone.DomainObjects.DomainObject script = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(scriptID);

            script.EventAdd(eventName, eventHandler);
        }

        void IEntityAPI.EventSubscribe(string subscriberID, string entityThatGeneratedTheEvent, string eventName, KeyScript.Events.EventDelegate eventHandler)
        {
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityThatGeneratedTheEvent);
            Keystone.DomainObjects.DomainObject script = entity.Script;

            if (!Keystone.Extensions.ArrayExtensions.ArrayContains(script.EventNames, eventName))
            {
                System.Diagnostics.Debug.WriteLine("Script '" + script.ResourcePath + "' has not registered an Event named '" + eventName + "'");
                return;
            }
            Keystone.Entities.Entity subscriber = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(subscriberID);
            entity.EventSubscribe(eventName, subscriber, eventHandler);
        }

        //void IEntityAPI.EventUnSubscribe (string subscriberID, string entityThatGeneratedTheEvent, string eventName, EventDelegate eventHandler)
        //{
        //    Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(eventGeneratorID);
        //    Keystone.DomainObjects.DomainObject script = entity.Script;

        //    Keystone.Entities.Entity subscriber = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(subscriberID);

        //    //script.EventUnSubscribe(eventName, subscriber, eventHandler);
        //}

        /// <summary>
        /// EventRaise() is always called by the script of the Entity that is raising the event and always
        /// invokes handlers of subscribers.  In other words, the Entity that raises the event never raises it to itself because
        /// it obviously already knows because it is the one calling EventRaise() in the first place.
        /// </summary>
        /// <param name="entityThatGeneratedTheEvent"></param>
        /// <param name="eventName"></param>
        void IEntityAPI.EventRaise(string entityThatGeneratedTheEvent, string eventName)
        {
            // NOTE: Only an Entity that is using a particular Script can raise events owned by that Script
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityThatGeneratedTheEvent);
            entity.EventRaise(eventName);
        }

        /// <summary>
        /// AddPropertyChangedEvent is called by the actual Entity Script csscript module
        /// to assign an event for properties that are modified 
        /// (eg. the event handler functions are actually written in the script!)
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="eventArg"></param>
        void IEntityAPI.PropertyChangedEventAdd(string scriptID, string property, KeyScript.Events.PropertyChangedEventDelegate handler)
        {
            Keystone.DomainObjects.DomainObject script = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(scriptID);
            if (script == null)
            {
                System.Diagnostics.Debug.WriteLine("EntityAPI.AddEvent() - Script node does not exist in Repository.");
                return;
            }
            script.AddPropertyChangedEvent(property, handler);
        }

        void IEntityAPI.PropertyChangedEventSubscribe(string entityThatGeneratesTheEvent, string subscriberID, string eventName, KeyScript.Events.PropertyChangedEventDelegate eventHandler)
        {

        }

        void IEntityAPI.AnimationEventSubscribe(string entityThatGeneratesTheEvent, string subscriberID, string animationName, KeyScript.Events.AnimatioCompletedEventDelegate eventHandler)
        {
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityThatGeneratesTheEvent);

            Keystone.Entities.Entity subscriber = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(subscriberID);
            entity.AnimationFinishedEventSubscribe(animationName, subscriber, eventHandler);
        }
        #endregion

        

        #region IEntityAPI Queries
        uint IEntityAPI.GetBrushStyle(string entityID)
        {
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);
            if (entity.Script == null) return 0;

            return (uint)entity.Execute("QueryPlacementBrushType", null);
        }

        string[] IEntityAPI.GetNearbyLights(string entityID, int maxDirLights, int maxLights, bool priortizeByRange)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);

            return null;
        }

        string IEntityAPI.FindDescendantByName(string nodeID, string name)
        {
            Keystone.Elements.Node node = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(nodeID);
            if (node == null) return null;

            IGroup group = node as IGroup;
            if (group == null) return null;

            Predicate<Keystone.Elements.Node> match = (n) =>
            {
                if (n.Name == name) return true;

                return false;
            };

            Node descendant = group.FindDescendant(match);
            if (descendant == null) return null;
            return descendant.ID;
        }

        // searches by node.Typename
        string IEntityAPI.GetDescendantOfType(string startingNodeID, string descendantTypename)
        {
            Keystone.Elements.Node start = (Keystone.Elements.Node)Keystone.Resource.Repository.Get(startingNodeID);

            if (start == null) return null;

            Predicate<Keystone.Elements.Node> match;

            match = node =>
            {
                return node.TypeName == descendantTypename;
            };

            Keystone.Elements.Node[] results = start.Query(true, match);

            if (results == null || results.Length == 0) return null;

            // just return first result
            return results[0].ID;
        }

        //FindDescendantsByCustomType

        // searches by custom entity.UserTypeID
        string[] IEntityAPI.GetComponentsOfType(string startingEntity, uint componentTypeID)
        {
            Keystone.Entities.Entity start = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(startingEntity);
            if (start.SceneNode == null) return null;

            Predicate<Keystone.Entities.Entity> match;

            match = e =>
            {
                return e.UserTypeID == componentTypeID;
            };

            List<Keystone.Entities.Entity> results = start.SceneNode.Query(true, match);

            if (results == null || results.Count == 0) return null;

            string[] ids = new string[results.Count];
            for (int i = 0; i < results.Count; i++)
                ids[i] = results[i].ID;

            return ids;
        }
        #endregion

        #region IEntityAPI Members
        KeyCommon.Data.UserData IEntityAPI.GetAIBlackboardData(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.BlackboardData;
        }

        void IEntityAPI.SetAIBlackboardData(string entityID, KeyCommon.Data.UserData data)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            ent.BlackboardData = data;
        }

        /// <summary>
        /// This spawn API call is typically called by spawnpoint scripts which are only executed
        /// by the Server (eg loopback). (though perhaps in future other Entities can spawn objects
        /// such as missile launchers and kinetic weaponry.) This means this code is executing on the server
        /// and so here if we are spawning from a prefab, we can generate a unique ID.
        /// </summary>
        /// <param name="entityID"></param>
        /// <param name="regionID"></param>
        /// <param name="prefabRelativePath"></param>
        /// <param name="position"></param>
        void IEntityAPI.Spawn(string entityID, string regionID, string prefabRelativePath, Vector3d position)
        {
            //// send server message to spawn?
            //KeyCommon.Messages.MessageBase networkMessage = new KeyCommon.Messages.Prefab_Load(AppMain._core.ModsPath, prefabRelativePath);
            //((KeyCommon.Messages.Prefab_Load)networkMessage).ParentID = regionID;
            //((KeyCommon.Messages.Prefab_Load)networkMessage).Position = position;
            //((KeyCommon.Messages.Prefab_Load)networkMessage).CloneEntityIDs = true;
            //((KeyCommon.Messages.Prefab_Load)networkMessage).Recurse = true;
            //((KeyCommon.Messages.Prefab_Load)networkMessage).DelayResourceLoading = true;

            //// TODO: I don't know how this all ends up... script running client side but
            ////       particle weapon fx and other effects spawning client side only
            ////       it's a mash up mix i think
            //AppMain.mNetClient.SendMessage(networkMessage);

            // TODO: we need a way to pass an array of IDs to the LoadEntity() function so that
            // we can load a prefab but use the unique IDs we've generated here.  I think this means
            // we need to load the prefab (if it is indeed a prefab and not a save since saved kgbentity
            // already contain the IDs we want to use eg clone = false) but without loading any resources.
            // And we don't just want to delay resource loading, we want to skip it completely as the loopback
            // server has no use for them.  A dedicated server however would want to load scripts but ignore
            // those script APIs that want to do animations, particle effects, etc

            // todo: we also want to LoadEntity in a worker thread and then send the spawn command after
            // we've generated the IDs.
            KeyCommon.Messages.Simulation_Spawn spawn = new KeyCommon.Messages.Simulation_Spawn();
            spawn.EntitySaveRelativePath = prefabRelativePath;
            spawn.Translation = position;
            spawn.ParentID = regionID;
            spawn.CellDBData = null;
            //spawn.EntityFileData = null;

            // find all users that will receive this Simulation_Spawn command.
            string[] users = GetUsers();
            for (int i = 0; i < users.Length; i++)
                AppMain.Form.ServerSendMessage(users[i], spawn);


        }

        private string[] GetUsers()
        {
            return new string[] { AppMain._core.Settings.settingRead("network", "username") };
        }


        object IEntityAPI.Execute(string entityID, string methodName, object[] args)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent.Script == null)
            {
                System.Diagnostics.Debug.WriteLine("EntityAPI.Execute() - DomainObject node does not exist in Repository.");
                return null;
            }
            return ent.Execute(methodName, args);
        }
        //string IEntityAPI.FindDescendant(string domainObjectID, string descendantID)
        //{
        //Keystone.DomainObjects.DomainObject domainObj = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(domainObjectID);

        //Keystone.Elements.Group.GetChildIDs 
        //}

        string IEntityAPI.GetEntitySceneID(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent == null || ent.Scene == null)
            {
                System.Diagnostics.Debug.WriteLine("EntityAPI.GetParentID() - Parent Node does not exist in Repository.");
                return null;
            }
            return ent.Scene.ID;
        }

        string IEntityAPI.GetEntityRegionID(string entityID)
        {
            // ok to use getters
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent is Keystone.Portals.Root)
                // TODO: should we return self.ID for _any_ region and not just root? 
                return ent.ID;
            else
                return ent.Region.ID;
        }

        bool IEntityAPI.EntityValid(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent == null || ent.Scene == null || ent.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Destroyed))
                return false;

            return true;
        }
    
        
        // This get's the Container/Vehicle that this entity belongs to.
        string IEntityAPI.GetOwnerID(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent is Keystone.Entities.Container) throw new Exception("EntityAPI.GetOwnerID() - entity is already a Container. Is this a vehicle inside a vehicle? For now we will just throw an exception.");

            Keystone.Entities.Container container = ent.Container;            
            if (container == null)             
            {
            	System.Diagnostics.Debug.WriteLine ("EntityAPI.GetOwnerID() - Entity does not exist within a Container or Vehicle.");
            	return null;
            }
            return container.ID;
        }
                
        string IEntityAPI.GetParentID(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            Keystone.Entities.Entity parent = ent.Parent;
            if (parent == null) 
            {
            	System.Diagnostics.Debug.WriteLine ("EntityAPI.GetParentID() - Parent Node does not exist in Repository.");
            	return null;
            }
            
            return parent.ID;
        }
        
        /// <summary>
        /// Whether this Entity node is to be rendered or not.  Invisible nodes
        /// will Update() but won't Render().  If you want to disable both use
        /// Enable = false.
        /// This method does not tell you if the entity was determined to be visible 
        /// by the camera after culling.
        /// </summary>
        /// <param name="entityID"></param>
        /// <returns></returns>
        bool IEntityAPI.GetVisible(string entityID)
        {
        	Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.Visible;
        }
        
        void IEntityAPI.SetVisible(string entityID, bool value)
        {
        	Keystone.Entities.Entity ent = GetEntity(entityID);
            ent.Visible = value;
        }
                
                        
        Vector3d IEntityAPI.GetRegionOffsetRelative (string regionA, string regionB)
        {
        	Keystone.Portals.Region A = (Keystone.Portals.Region)Keystone.Resource.Repository.Get(regionA);
        	Keystone.Portals.Region B = (Keystone.Portals.Region)Keystone.Resource.Repository.Get(regionB);
        
        	// TODO: this is quick hack version is susceptible to imprecision issues
			//     	 since it's starting from global values which can be very large
        	//    	 instead of using relative zone offsets, subtracting them and multiplying that diff by zone dimensions

        	Vector3d relativeResult = A.GlobalTranslation - B.GlobalTranslation;
        	return relativeResult;
        }
        
        double IEntityAPI.GetFloor(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.Floor;
        }

        double IEntityAPI.GetHeight(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.Height;
        }

        double IEntityAPI.GetRadius(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            // TODO: we have problems here if the BoundingBox is not initialized such as for an Entity that has no Model child set.
            //       and where the BoundingBox there is just initialized to min/max float.Min/float.Max
            if (ent.BoundingBox == null) return 0;
          
            double radius = ent.BoundingBox.Radius;
            if (radius < 0) radius = 0;
            
            return radius;
        }


        void IEntityAPI.GetPosition(string entityID, out Vector3d local, out Vector3d position, out Vector3d global)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);

            local = ent.Translation;
            position = ent.DerivedTranslation;
            global = ent.GlobalTranslation;
        }

        Vector3d IEntityAPI.GetPositionLocalSpace(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.Translation;
        }

        // NOTE: this will NOT be camera space position but region space position.  
        // TODO: we may end up having to pass in the id of the region that we want the position to be in relation to
        Vector3d IEntityAPI.GetPositionRegionSpace(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.DerivedTranslation;
        }

        Vector3d IEntityAPI.GetPositionGlobalSpace(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.GlobalTranslation;
        }

        void IEntityAPI.SetPositionLocalSpace(string entityID, Vector3d localSpacePosition)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            ent.Translation = localSpacePosition;
        }

        // TODO: This set's Local Position only whereas "GetPosition" returns Derived! I should rename these methods Set/GetLocalPosition, GetPosition (no Set), Set/GetGlobalPosition
        // and update all entity script files accordingly.        There should be no way to directly set global or regional.
        void IEntityAPI.SetPositionRegionSpace(string entityID, Vector3d regionSpacePosition)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            // todo:i think th is is wrong.  i need to take the parent regionMatrix and inverse that
            Matrix local = ent.LocalMatrix;
            Matrix inverse = Matrix.Inverse(local);

            // TODO: this is broken.  
            Vector3d coord = Vector3d.TransformCoord(regionSpacePosition, inverse);
            ent.Translation = coord;
        }

        void IEntityAPI.SetPositionGlobalSpace(string entityID, Vector3d globalSpacePosition)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            Matrix local = ent.LocalMatrix;
            Matrix inverse = Matrix.Inverse(local);

            // todo: shouldn't this use inverse matrix?
            Vector3d coord = Vector3d.TransformCoord(globalSpacePosition, local);
            ent.Translation = coord;
        }

        void IEntityAPI.GetRotation(string entityID, out Quaternion local, out Quaternion regionSpaceRotation, out Quaternion global)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            local = ent.Rotation;
            regionSpaceRotation = ent.DerivedRotation;
            global = ent.GlobalRotation;
        }

        Quaternion IEntityAPI.GetRotationLocalSpace(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.Rotation;
        }

        Quaternion IEntityAPI.GetRotationRegionSpace(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.DerivedRotation;
        }

        Quaternion IEntityAPI.GetRotationGlobalSpace(string entityID)
        {
            // ok to use getters
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            return ent.GlobalRotation;
        }

        void IEntityAPI.SetRotationLocalSpace (string entityID, Quaternion rotationLocalSpace)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            ent.Rotation = rotationLocalSpace;
        }

        void IEntityAPI.SetRotationRegionSpace(string entityID, Quaternion regionSpaceRotation)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);
            // NOTE: This assumes we are rotating an Entity that has a parent Entity (eg. turret on a vehicle)
            // calc the local rotation and assign that
            System.Diagnostics.Debug.Assert(ent.Parents.Length == 1 && ent.Parents[0] is Transform);
            Quaternion parentRotation = ((Transform)ent.Parents[0]).DerivedRotation; 
            Quaternion inverse = Quaternion.Inverse (parentRotation);

            ent.Rotation = inverse * regionSpaceRotation;
        }

        void IEntityAPI.SetRotationGlobalSpace(string entityID, Quaternion globalSpaceRotation)
        {
            Keystone.Elements.BoundTransformGroup ent = (Keystone.Elements.BoundTransformGroup)Keystone.Resource.Repository.Get(entityID);

            // NOTE: This assumes we are rotating an Entity that has a parent Entity (eg. turret on a vehicle)
            // calc the local rotation and assign that
            System.Diagnostics.Debug.Assert(ent.Parents.Length == 1 && ent.Parents[0] is Transform);
            Quaternion parentRotation = ((Transform)ent.Parents[0]).GlobalRotation;
            Quaternion inverse = Quaternion.Inverse(parentRotation);

            ent.Rotation = inverse * globalSpaceRotation;
        }


        Vector3d IEntityAPI.GetVelocity(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.Velocity;
        }

        void IEntityAPI.SetVelocity(string entityID, Vector3d velocity)
        {
            ChangeProperty(entityID, "velocity", typeof(Vector3d), velocity);
        }

        Vector3d IEntityAPI.GetAcceleration(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.Acceleration;
        }

        void IEntityAPI.SetAcceleration(string entityID, Vector3d acceleration)
        {
            ChangeProperty(entityID, "acceleration", typeof(Vector3d), acceleration);
        }

        Vector3d IEntityAPI.GetAngularVelocity(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.AngularVelocity;
        }

        void IEntityAPI.SetAngularVelocity(string entityID, Vector3d velocity)
        {
            ChangeProperty(entityID, "angularvelocity", typeof(Vector3d), velocity);
        }

        Vector3d IEntityAPI.GetAngularAcceleration(string entityID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.AngularAcceleration;
        }

        void IEntityAPI.SetAngularAcceleration(string entityID, Vector3d acceleration)
        {
            ChangeProperty(entityID, "angularacceleration", typeof(Vector3d), acceleration);
        }


        object IEntityAPI.GetCustomPropertyValue(string entityID, string propertyName)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent == null)
            {
                System.Diagnostics.Debug.WriteLine ("EntityAPI.GetCustomPropertyValue() - ERROR: Entity " + entityID + "' does not exist in Repository.");
                return null;
            }
            // TODO: temp hack
            if (ent is Keystone.Celestial.Body)
            {
                if (propertyName == "mass")
                    return ((Keystone.Celestial.Body)ent).MassKg;
            }
            
            //if (propertyName == "contacts")
            //    System.Diagnostics.Debug.WriteLine("EntityAPI.GetCustomPropertyValue() - entiy.ID: " + ent.ID + " - property name: " +propertyName + "'");

            object result = ent.GetCustomPropertyValue(propertyName);
			if (result == null)
			{
            	//System.Diagnostics.Debug.WriteLine ("EntityAPI.GetCustomPropertyValue() - ERROR: Property " + propertyName + "' does not exist or value is null.");
			}
			
			return result;
        }

        Settings.PropertySpec[] IEntityAPI.GetCustomProperties(string entityID, bool specOnly)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent == null) return null;

            return ent.GetCustomProperties(specOnly);
        }


        void IEntityAPI.SetCustomPropertyValues (string entityID, string[] propertyNames, object[] values, bool raiseEvent = false)
        {
            int[] errorCode;
            Keystone.Entities.Entity ent = GetEntity(entityID);
            bool validate = false;
            
            ent.SetCustomPropertyValues(propertyNames, values, validate, raiseEvent, out errorCode);
        }

        // todo: i think we need a SRC argument so we know if its a behavior script or an Entity script and this lets us know if we need to raiseCustomPropertyChangedEvent
        // perhaps srcTypename 
        void IEntityAPI.SetCustomPropertyValue(string entityID, string propertyName, object value, bool raiseEvent = false)
        {
            //if (propertyName == "navpoints")
            //{
            //    System.Diagnostics.Debug.WriteLine("navpoints");
            //    ((IEntityAPI)this).SetCustomPropertyValues(entityID, new string[] { propertyName }, new object[] { value });
            //    object tmp = ((IEntityAPI)this).GetCustomPropertyValue (entityID, propertyName);
            //    Game01.GameObjects.NavPoint[] navpoints = (Game01.GameObjects.NavPoint[])tmp;
            //}
            ((IEntityAPI)this).SetCustomPropertyValues(entityID, new string[] { propertyName }, new object[] { value }, raiseEvent);
        }

        /// <summary>
        /// AddCustomProperties is called by the actual DomainObjectScript csscript module to
        /// assign the custom properties created in that csscript module
        /// </summary>
        /// <param name="entityID"></param>
        /// <param name="properties"></param>
        void IEntityAPI.AddCustomProperties(string scriptID, Settings.PropertySpec[] properties)
        {
            // TODO: i think it's ok to directly set against properties here because
            // when the scripts are running, it is during Update() and for adding custom properties
            // there is no need to notify server.  The server is running the same script server side
            Keystone.DomainObjects.DomainObject script = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(scriptID);
            if (script == null) return;
            script.CustomProperties = properties;
        }

        
        void IEntityAPI.SetEntityFlag(string entityID, uint flag, bool value)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            ent.SetEntityAttributesValue(flag, value);
        }

        bool IEntityAPI.GetEntityFlag(string entityID, uint flag)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.GetEntityAttributesValue(flag);
        }

        /// <summary>
        /// Component flags are shareable and set in DomainObjectScript initialization.
        /// They can also be set via a plugin.  These are NOT the same as custom
        /// flags which the user can define.  These flags specifically relate to things like
        /// -type (wall,door,window,floor,ceiling,component)
        /// -mount location (wall, ceiling, floor, counter)
        /// -brush type (tile, single, terrain sculpt, etc)
        /// </summary>
        /// <param name="scriptID"></param>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        void IEntityAPI.SetComponentFlagValue(string scriptID, uint flag, bool value)
        {
            Keystone.DomainObjects.DomainObject script = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(scriptID);
            script.SetFlagValue(flag, value);
        }

        bool IEntityAPI.GetComponentFlagValue(string scriptID, uint flag)
        {
            Keystone.DomainObjects.DomainObject script = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(scriptID);
            return script.GetFlagValue(flag);
        }

        /// <summary>
        /// Custom Flag values are stored in each Entity.
        /// </summary>
        /// <param name="entityID"></param>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        void IEntityAPI.SetCustomFlagValue(string entityID, uint flag, bool value)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            ent.SetCustomFlagValue(flag, value);
        }

        bool IEntityAPI.GetCustomFlagValue(string entityID, uint flag)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            return ent.GetCustomFlagValue(flag);
        }

        

        //        private Keystone.Entities.Entity mRecentEntity;
        internal static Keystone.Entities.Entity GetEntity(string entityID)
        { 

 //       	if (mRecentEntity != null && mRecentEntity.ID == entityID)
 //       		return mRecentEntity;
        	
        	// TODO: i should have seperate repository for entities and then also have Repository cache
        	//       the ones that are frequently accessed?
        	Keystone.Entities.Entity ent = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);
        	
    		// assign  our most recent entity that we cache for performance reasons
//			if (ent != null)
//				mRecentEntity = ent;
			
			return ent;
        }
        #endregion

        #region Production & Consumption
        void IEntityAPI.RegisterProduction(string entityID, uint productID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent.Scene != null)
                AppMain._core.SceneManager.Scenes[0].Simulation.RegisterProducer(productID, ent);
           // TODO: I think maybe there's a problem here because the Entity has yet to be added to the Scene and Simulation.  
           // So i cant use the below call, i have to use the above call.
            // ent.Scene.Simulation.RegisterProducer(productID, ent);
        }

        void IEntityAPI.UnRegisterProduction(string entityID, uint productID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent.Scene != null)
                AppMain._core.SceneManager.Scenes[0].Simulation.UnRegisterProducer(productID, ent);
            //ent.Scene.Simulation.UnRegisterProducer(productID, ent);
        }

        
        void IEntityAPI.RegisterConsumption(string entityID, uint productID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent.Scene != null)
                AppMain._core.SceneManager.Scenes[0].Simulation.RegisterConsumption(productID, ent);
        }

        void IEntityAPI.UnRegisterConsumption(string entityID, uint productID)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            if (ent.Scene != null)
                AppMain._core.SceneManager.Scenes[0].Simulation.UnRegisterConsumption(productID, ent);
        }

        ///// <summary>
        ///// Force Production handler runs at physics update frequency.
        ///// </summary>
        //void IEntityAPI.AssignForceProductionHandler(string domainObjectID, KeyCommon.Simulation.Production_Delegate productionHandler)
        //{
        //    Keystone.DomainObjects.DomainObject domainObj = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(domainObjectID);

        //    //TODO: what if i specified the frequency of this production here upon creation
        //    // of the production?
        //    if (domainObj == null) return;
        //    domainObj.AddForceProduction(productionHandler);
        //}

        /// <summary>
        /// User Production Handler runs at 1 Hz and it's start time can be set round robin
        /// so that user productions from Entities are spread out across multiple frames.
        /// </summary>
        /// <param name="domainObjectID"></param>
        /// <param name="productionHandler"></param>
        void IEntityAPI.AssignProductionHandler(uint productID, KeyCommon.Simulation.Production_Delegate productionHandler)
        {
            // todo: scripts should probably use a Game01.Rules.Processors.*****   method for the handler rather than one from the script
        //       if the handler already exists for this ProductID that is DIFFERENT from the existing, we should show/log a warning
        //       and resume? or throw an error so we can fix?
            if (ent.Scene != null)
                AppMain._core.SceneManager.Scenes[0].Simulation.AddProductionHandler(productID, productionHandler);
            //  
        }

        // todo: scripts should probably use a Game01.Rules.Processors.*****   method for the handler rather than one from the script
        //       if the handler already exists for this ProductID that is DIFFERENT from the existing, we should show/log a warning
        //       and resume? or throw an error so we can fix?
        void IEntityAPI.AssignConsumptionHandler(string productID, KeyCommon.Simulation.Consumption_Delegate consumptionHandler)
        {
            // TODO: the current thinking is, we should NOT store these in the Script(aka DomainObject because all Entities that can
            //       conume this productID should do so in the same way.  For example, if a Microwave creates a beam
            //       then we create seperate productIDs for MicrowaveEmission, microwaveReflection and microwaveDamage so that here we only need 
            //       one handler for every component that can respond to ONE of these productIDs.  So a crew member can consume both a
            //       MicrowaveEmission (and reflect a MicrowaveReflection back... note a seperate MicrowaveReflection is created so that we dont end
            //       up with infinite loop of MicrowaveEmissions going back and forth as well as consume MicrowaveDamage))

            if (ent.Scene != null)
                AppMain._core.SceneManager.Scenes[0].Simulation.AddConsumptionHandler(productID, consumptionHandler);
        }

        // TODO: i think the following is irrelevant?  A production store is just a var in
        // the Entity to where the Production update part of the script will store units created
        // during that tick. Then that update can either erase that var at start of tick or
        // add to it depending on the capacity and nature of the storage.  
        void IEntityAPI.CreateProductionStore(string entityID, string productID, double capacity)
        {
            Keystone.Entities.Entity ent = GetEntity(entityID);
            throw new NotImplementedException();
        //    does the store exist in the struct for  'struct Producer_Struct {}'  ?
        //    //  
        }

        

        //void IEntityAPI.CreateTransmitter(string domainObjID, string transmitterName, uint emmissionTypeFlag)
        //{
        //    Keystone.DomainObjects.DomainObject domainObj = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(domainObjID);

        //    // create and add the transmitter to the entity
        //    // TODO: I think maybe this is wrong.  We should not be hosting a Transmitter
        //    // but rather returning a Transmitter which serves as a discreate diffuseable
        //    // game object after calling Transmitter = entity.Transmit (transmitterName, flag);
        //    // Then we can get receivers and apply the transmitter object to them.

        //    Transmitter t = new Transmitter(transmitterName, emmissionTypeFlag);
        //    domainObj.AddTransmitter(t);
        //}

        //void IEntityAPI.CreateReceiver(string domainObjID, string receiverName, uint emmissionTypeFlag)
        //{

        //    Keystone.DomainObjects.DomainObject domainObj = (Keystone.DomainObjects.DomainObject)Keystone.Resource.Repository.Get(domainObjID);

        //    // create and add the transmitter to the entity
        //    Receiver r = new Receiver(receiverName, emmissionTypeFlag);
        //    domainObj.AddReceiver(r);
        //}
        #endregion
        
        #region Animations
        //void IEntityAPI.PlayAnimation(string entityID, string animationName)
        //{
        //    // how do i control whether to add as a track to the AnimationController or
        //    // to replace?
        //    // part of it is whether they are related/unrelated models under the sub-entity
        //    Keystone.Resource.IResource res = Keystone.Resource.Repository.Get(entityID);
        //    if (res == null) return;
        //    if (!(res is Keystone.Entities.ModeledEntity)) return;

        //    Keystone.Entities.ModeledEntity ent = (Keystone.Entities.ModeledEntity)res;

        //    // NOTE: Setting the animation is same as playing it.  Otherwise we
        //    // can clear the animmation to stop it or explicily call stop 
        //    ent.Animations.SetCurrentAnimation(animationName);
        //}

        //void IEntityAPI.PlayAnimation(string entityID, int animationIndex)
        //{
        //    Keystone.Resource.IResource res = Keystone.Resource.Repository.Get(entityID);
        //    if (res == null) return;
        //    if (!(res is Keystone.Entities.ModeledEntity)) return;

        //    Keystone.Entities.ModeledEntity ent = (Keystone.Entities.ModeledEntity)res;

        //    // NOTE: Setting the animation is same as playing it.  Otherwise we
        //    // can clear the animmation to stop it or explicily call stop 
        //    ent.Animations.SetCurrentAnimation(animationIndex);
        //    //ent.Animations.Play();

        //}
        #endregion

        #region CelledRegions

        void IEntityAPI.CellMap_RegisterObserver(string interiorID, string entityID, string layerName)
        {
            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            if (interior == null) return;

            Keystone.Entities.Entity child = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);

            interior.RegisterObserver(child, layerName);
        }

        void IEntityAPI.CellMap_UnregisterObserver(string interiorID, string entityID, string layerName)
        {
            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            if (interior == null) return;

            Keystone.Entities.Entity child = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);

            interior.UnregisterObserver(child, layerName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interiorID"></param>
        /// <param name="position">mouse pick location in local space used to find nearest edge</param>
        /// <param name="rotation">x,y,z rotation in degrees</param>
        /// <returns></returns>
        Vector3d IEntityAPI.CellMap_GetEdgePosition(string interiorID, Vector3d position, out Vector3d rotation, out int edgeID)
        {
            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            position = interior.GetEdgePosition(position, out edgeID, out rotation);

            return position;
        }

        bool IEntityAPI.CellMap_EdgeHasWall(string interiorID, int edgeID)
        {
            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            bool result = interior.WallExists((uint)edgeID);
            return result;
        }

        int[] IEntityAPI.CellMap_GetEdgeAdjacents(string interiorID, int edgeID, bool parallelsOnly)
        {
            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            if (parallelsOnly)
                return interior.GetParrallelAdjacentEdges((uint)edgeID);
            else
                return interior.GetAllAdjacentEdges((uint)edgeID);
        }

        uint[] IEntityAPI.CellMap_GetMapDimensions(string interiorID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            return new uint[] { celledRegion.CellCountX, celledRegion.CellCountY, celledRegion.CellCountZ };
        }

        Vector3d IEntityAPI.CellMap_GetTileSnapPosition(string interiorID, string childID, Vector3d position, byte rotation)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            Keystone.Entities.Entity child = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(childID);

            // BonedEntities do not have a footprint with any data because they do not get added to the TileMapGrid.
            // todo: maybe they should have a default empty footprint?
            if (child.Footprint == null || child.Footprint.Data == null) return position;

            Vector3d result = celledRegion.GetTileSnapPosition(child.Footprint.Data, position, child.Rotation.GetComponentYRotationIndex());
            return result;

        }

        /// <summary>
        /// Gets the Cell Index of a given entityID's position in world space // TODO: shouldn't this read in "region space" since the cellmap is using it's own Region's coordinate system?
        /// </summary>
        /// <param name="interiorID"></param>
        /// <param name="entityID"></param>
        /// <returns></returns>
        uint IEntityAPI.CellMap_GetCellIndexFromWorldPosition(string interiorID, string entityID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);
            return celledRegion.CellIndexFromPoint(entity.Translation);
        }

        uint IEntityAPI.CellMap_GetCellIndexFromWorldPosition(string interiorID, Vector3d position)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            return celledRegion.CellIndexFromPoint(position);
        }

        Vector3d IEntityAPI.CellMap_GetCellSize(string entityID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            return celledRegion.CellSize;
        }

        Vector3d IEntityAPI.CellMap_GetTileSize(string entityID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            return celledRegion.TileSize;
        }

        CellEdge IEntityAPI.CellMap_GetEdge(string celledRegionID, uint edgeID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(celledRegionID);
            return CellEdge.CreateEdge(edgeID, celledRegion.CellCountX, celledRegion.CellCountY, celledRegion.CellCountZ);
        }

        // NOTE: the user could call this directly from Script since Utilities.KeyMath is directly accessible however
        //       they do not get the cellCountX, cellCountZ easily that way.  They'd have to query those properties seperately
        //       before they could pass them to KeyMath.UnflattenIndex()
        uint[] IEntityAPI.CellMap_Unflatten(string celledRegionID, uint cellID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(celledRegionID);
            uint x, y, z;
            Keystone.Utilities.MathHelper.UnflattenIndex(cellID, celledRegion.CellCountX, celledRegion.CellCountZ, out x, out y, out z);
            return new uint[] { x, y, z };
        }


        // TODO: fix calling script to take float
        float[] IEntityAPI.CellMap_GetStartIndices(string entityID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            return new float[] { celledRegion.StartX, celledRegion.StartY, celledRegion.StartZ };
        }

        /// <summary>
        /// Returns the center position of cell.
        /// </summary>
        /// <param name="interiorID"></param>
        /// <param name="cellID"></param>
        /// <returns></returns>
        Vector3d IEntityAPI.CellMap_GetCellPosition(string interiorID, uint cellID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            uint x, y, z;
            Keystone.Utilities.MathHelper.UnflattenIndex(cellID, celledRegion.CellCountX, celledRegion.CellCountZ, out x, out y, out z);

            Vector3d position;
            position.x = (celledRegion.StartX + x) * celledRegion.CellSize.x;
            // NOTE: y position here is at center of cell and not on the floor of the deck/level.
            // If the caller wants the y position of the floor, they should call EntityAPI.CellMap_GetCellSize(interiorID) and do position.y = position.y - (size.y / 2d) 
            position.y = (celledRegion.StartY + y) * celledRegion.CellSize.y;
            position.z = (celledRegion.StartZ + z) * celledRegion.CellSize.z;
            return position;
        }

        Vector3d IEntityAPI.CellMap_GetTilePosition3D(string entityID, Vector3i tileLocation)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            return new Vector3d((celledRegion.TileStartX + tileLocation.X) * celledRegion.TileSize.x,
                                  (celledRegion.TileStartY + tileLocation.Y) * celledRegion.TileSize.y,
                                  (celledRegion.TileStartZ + tileLocation.Z) * celledRegion.TileSize.z);
        }


        void IEntityAPI.CellMap_SetDataLayerValue(string entityID, string layerName, uint elementIndex, bool value)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            celledRegion.Layer_SetValue(layerName, elementIndex, value);
        }

        object IEntityAPI.CellMap_GetDataLayerValue(string entityID, string layerName, uint elementIndex)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            return celledRegion.Layer_GetValue(layerName, elementIndex);
        }

        void IEntityAPI.CellMap_UpdateLinkNetwork(string entityID, uint tileID, string layerName, bool value)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            switch (layerName)
            {
                case "powerlink":
                    celledRegion.Database.DoLinkSearch(tileID, Keystone.Portals.Interior.TILE_ATTRIBUTES.LINE_POWER, value);
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("EntityAPI.CellMap_UpdateLinkNetwork() - unexpected layerName '" + layerName + "'");
                    break;
            }
        }
        // OBSOLETE - this does nothing.
        //void IEntityAPI.CellMap_SetCellBoundsValue(string entityID, uint cellID, bool value)
        //{
        //    Keystone.Portals.CelledRegion celledRegion = (Keystone.Portals.CelledRegion)Keystone.Resource.Repository.Get(entityID);

        //    // if true, we must set TILEMASK_FLAGS.BOUNDS_IN for the entire cell
        //    // else, we must clear all flags because this cell is now out of bounds
        //    // TODO: somewhere in validation perhaps (CommandProcessor i suspect) if this cell
        //    // is not empty, we must abort.
        //    uint x, y, z;
        //    celledRegion.UnflattenCellIndex(cellID, out x, out y, out z);
        //    byte rotation = 0; //ceilings and floor tile segments have no rotation

        //    if (celledRegion.IsCellInBounds(x, y, z))
        //        if (celledRegion.IsCellExists(x, y, z))
        //        {
        //            int[,] footprint = new int[celledRegion.TilesPerCellX, celledRegion.TilesPerCellZ];
        //            for (int i = 0; i < celledRegion.TilesPerCellX; i++)
        //                for (int j = 0; j < celledRegion.TilesPerCellZ; j++)
        //                    if (value)
        //                        footprint[i, j] |= (int)Keystone.Portals.CelledRegion.TILEMASK_FLAGS.BOUNDS_IN;
        //                    else
        //                        // we do not OR the value for out of bounds, we replace it
        //                        footprint[i, j] = (int)Keystone.Portals.CelledRegion.TILEMASK_FLAGS.NONE;


        //            int[,] rotatedFootprint = celledRegion.GetRotatedFootprint(footprint, rotation);

        //            // BOTTOM/LEFT TILE FLOOR FOOTPRINT of the current floor
        //            celledRegion.ApplyFootprint((int)cellID, rotatedFootprint);

        //            // TOP/RIGHT TILE CEILING FOOTPRINT of the floor below
        //            celledRegion.ApplyFootprint((int)cellID, rotatedFootprint);
        //        }
        //}

        object IEntityAPI.CellMap_GetTileSegmentState(string entityID, uint cellID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            return null;
        }

        void IEntityAPI.CellMap_SetTileSegmentState(string entityID, uint cellID, object segment_state)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
        }

        void IEntityAPI.CellMap_SetTileSegmentStyle(string entityID, uint tileID, object value)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            Keystone.Portals.EdgeStyle style = (Keystone.Portals.EdgeStyle)value;

            int atlasIndex = 0;
            bool deleteTile = style.StyleID == -1;

            if (deleteTile == false)
            {
                atlasIndex = style.FloorAtlasIndex;// int.Parse(style.FloorAtlasIndex);
                if (atlasIndex < 0) atlasIndex = 0;
                ((IEntityAPI)this).CellMap_SetFloorAtlasTexture(entityID, tileID, (uint)atlasIndex);

                atlasIndex = style.CeilingAtlasIndex; // int.Parse(style.CeilingAtlasIndex);
                if (atlasIndex < 0) atlasIndex = 0;
                ((IEntityAPI)this).CellMap_SetCeilingAtlasTexture(entityID, tileID, (uint)atlasIndex);
            }

            SetCollapseState(entityID, Keystone.Portals.Interior.PREFIX_FLOOR, tileID, deleteTile);
            SetCollapseState(entityID, Keystone.Portals.Interior.PREFIX_CEILING, tileID, deleteTile);

        }


        //uint[] edges = EntityAPI.CellMap_Unflatten (entityID, elementIndex);
        // TODO: here we're trying to find a model assuming that the same model is used
        // over the entire floor when instead, we should be discriminating by floor AND mesh style.
        // (texture of same style should be sharing atlases thus can use same minimesh?)
        void IEntityAPI.CellMap_SetEdgeSegmentStyle(string entityID, uint edgeID, object style)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            //Keystone.Portals.MinimeshMap minimeshMap = (Keystone.Portals.MinimeshMap)map;
            Keystone.Portals.EdgeStyle edgeStyle = (Keystone.Portals.EdgeStyle)style;

            // NOTE: this isn't just for visual style, it also modifies the underlying data.  It has to be this way
            //       because changes to one edge can affect adjacent edges, and those edges may need to have
            //       their models changed and thus their underlying data as well.
            //       That's why I removed from the ship_interior.cs script, the seperate applying of the data.
            //   
            // TODO: the trick i need to solve is when there are multiple wall operations within a single Tool call
            //       and I dont want to recalc things like connectivity graph after each individual wall, but rather only
            //       when all wall operations have completed.

            // TODO: i think any call here should result in calls to run logic when a cell's bounds status changes
            //       or when any of it's segments have changed (added/removed/changed)
            // NOTE: ApplyEdgeSegmentStyle can also apply a NULL style to delete an existing style and replace with nothing
            celledRegion.ApplyEdgeSegmentStyle(edgeID, edgeStyle);
        }



        void IEntityAPI.CellMap_ApplyFootprint(string entityID, uint index, object styleObject)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            Keystone.Portals.EdgeStyle style = null;

            // TODO: we need to determine if index is a Edge ID or a flattened Tile index.
            // TODO: how do we do that? we could add a seperate parameter to this method
            //       eg. bool isEdge.  But maybe not, because i think in our new implementation
            //       ApplyEdgeSegmentStyle() dynamically computes the footprint of the edge wall and
            //       applies or unapplies the footprint.  This method is no longer required for
            //       EdgeStyles.  Then we would only need to call ApplyTileSegmentFootprint() here.
            //if (styleObject is Keystone.Portals.EdgeStyle)
            //{
            //    style = (Keystone.Portals.EdgeStyle)styleObject;  
            //    // unapply of footprint is also done in the following call
            //    ApplyEdgeSegmentFootprint(celledRegion, index, style);                        
            //}
            //else
            //{
            style = (Keystone.Portals.EdgeStyle)styleObject;
            // unapply of footprint is also done in the following call
            ApplyTileSegmentFootprint(celledRegion, index, style);
            //}
        }

        // todo: this method i think is obsolete - edge wall segment footprints are dynamically computed now based on sub model index for the prefab's ModelSelector
        private void ApplyEdgeSegmentFootprint(Keystone.Portals.Interior celledRegion, uint edgeID, Keystone.Portals.EdgeStyle style)
        {
            Keystone.CSG.CellEdge e = Keystone.CSG.CellEdge.CreateEdge(edgeID, celledRegion.CellCountX, celledRegion.CellCountY, celledRegion.CellCountZ);

            // compute the rotation based on the orientation for use with footprints
            byte leftRotation, rightRotation;
            e.GetByteRotation(out leftRotation, out rightRotation);

            // NOTE: For Client/Server, the server
            //       runs the same validation so client simply places when server responds ok.
            // if both left and right cells are -1 there is a problem
            // if both left and right cells are painted as "out of bounds" there is a problem
            //  - at least one or the other must be in bounds
            uint adjacentX, adjacentY, adjacentZ;
            celledRegion.UnflattenCellIndex((uint)e.BottomLeftCell, out adjacentX, out adjacentY, out adjacentZ);
            // NOTE: For edges, footprints typically will fall on both cells that share the edge    
            // So we test if the cells adjacent to this EDGE are available to have footprints set on them
            // i.e. they are in bounds and they have floors under them
            if (celledRegion.IsCellInBounds(adjacentX, adjacentY, adjacentZ))
            {
                // BOTTOM/LEFT CELL EDGE FOOTPRINT
                if (celledRegion.IsCellExists(adjacentX, adjacentY, adjacentZ))
                {
                    // NOTE: by having call to apply footprint outside of CelledRegion and inside of Command Processor
                    // we are saying that the rules/logic for when to do these things is determined by the app
                    // and is not hardcoded into the CelledRegion.  This allows us to have variable resolution
                    // footprint data.
                    int[,] rotatedFootprint = celledRegion.GetRotatedFootprint(style.BottomLeftFootprint, leftRotation);

                    if (style.StyleID > -1)
                        celledRegion.ApplyFootprint(e.BottomLeftCell, rotatedFootprint);
                    else
                        celledRegion.UnApplyFootprint(e.BottomLeftCell, rotatedFootprint);
                }
            }

            celledRegion.UnflattenCellIndex((uint)e.TopRightCell, out adjacentX, out adjacentY, out adjacentZ);
            if (celledRegion.IsCellInBounds(adjacentX, adjacentY, adjacentZ))
            {
                // TOP/RIGHT CELL EDGE FOOTPRINT
                if (celledRegion.IsCellExists(adjacentX, adjacentY, adjacentZ))
                {
                    int[,] rotatedFootprint = celledRegion.GetRotatedFootprint(style.TopRightFootprint, rightRotation);
                    if (style.StyleID > -1)
                        celledRegion.ApplyFootprint(e.TopRightCell, rotatedFootprint);
                    else
                        celledRegion.UnApplyFootprint(e.TopRightCell, rotatedFootprint);
                }
            }
        }

        private void ApplyTileSegmentFootprint(Keystone.Portals.Interior celledRegion, uint cellID, Keystone.Portals.EdgeStyle style)
        {
            uint x, y, z;
            celledRegion.UnflattenCellIndex(cellID, out x, out y, out z);
            byte rotation = 0; //ceilings and floor tile segments have no rotation

            if (celledRegion.IsCellInBounds(x, y, z))
                if (celledRegion.IsCellExists(x, y, z))
                {
                    // NOTE: by having call to apply footprint outside of CelledRegion and inside of Command Processor
                    // we are saying that the rules/logic for when to do these things is determined by the app
                    // and is not hardcoded into the CelledRegion

                    int[,] rotatedFootprint = celledRegion.GetRotatedFootprint(style.BottomLeftFootprint, rotation);

                    // BOTTOM/LEFT TILE FLOOR FOOTPRINT of the current floor
                    if (style.StyleID > -1)
                        celledRegion.ApplyFootprint((int)cellID, rotatedFootprint);
                    else
                        celledRegion.UnApplyFootprint((int)cellID, rotatedFootprint);

                    rotatedFootprint = celledRegion.GetRotatedFootprint(style.TopRightFootprint, rotation);

                    // TOP/RIGHT TILE CEILING FOOTPRINT of the floor below
                    if (style.StyleID > -1)
                        celledRegion.ApplyFootprint((int)cellID, rotatedFootprint);
                    else
                        celledRegion.UnApplyFootprint((int)cellID, rotatedFootprint);

                }

        }

        object IEntityAPI.CellMap_GetEdgeSegmentState(string entityID, uint edgeID)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
            return null;
        }

        void IEntityAPI.CellMap_SetEdgeSegmentState(string entityID, uint edgeID, object segment_state)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);
        }
        void IEntityAPI.CellMap_SetFloorCollapseState(string entityID, uint cellIndex, bool collapse)
        {
            SetCollapseState(entityID, Keystone.Portals.Interior.PREFIX_FLOOR, cellIndex, collapse);
        }

        void IEntityAPI.CellMap_SetCeilingCollapseState(string entityID, uint cellIndex, bool collapse)
        {
            SetCollapseState(entityID, Keystone.Portals.Interior.PREFIX_CEILING, cellIndex, collapse);
        }

        void SetCollapseState(string entityID, string modelDescription, uint cellIndex, bool collapse)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            uint x, y, z;
            celledRegion.UnflattenCellIndex(cellIndex, out x, out y, out z);

            string modelID = Keystone.Portals.Interior.GetInteriorElementPrefix(entityID, modelDescription, typeof(Keystone.Elements.Model), (int)y);

            Keystone.Elements.Model model = (Keystone.Elements.Model)Keystone.Resource.Repository.Get(modelID);
            if (model == null) return;

            Keystone.Elements.Mesh3d mesh = (Keystone.Elements.Mesh3d)model.Geometry;
            // TODO: Note, this is not updating the footprint attributes in the tilemap.
            //       That particular call is done from the Script's call to CellMap_ApplyFootprint()
            //      but i think that's not a good way to handle it.  Footprint changes should be done here and not left up to the script... right?
            Keystone.Celestial.ProceduralHelper.CellGrid_CellSetCollapseState(celledRegion.CellCountX, celledRegion.CellCountZ, x, z, mesh, collapse);
        }

        void IEntityAPI.CellMap_SetFloorAtlasTexture(string entityID, uint cellIndex, uint atlasTextureIndex)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            uint x, y, z;
            celledRegion.UnflattenCellIndex(cellIndex, out x, out y, out z);
            string floorModelID = Keystone.Portals.Interior.GetInteriorElementPrefix(entityID, Keystone.Portals.Interior.PREFIX_FLOOR, typeof(Keystone.Elements.Model), (int)y);
            SetAtlasTile(celledRegion, floorModelID, x, z, atlasTextureIndex);
        }

        void IEntityAPI.CellMap_SetCeilingAtlasTexture(string entityID, uint cellIndex, uint atlasTextureIndex)
        {
            Keystone.Portals.Interior celledRegion = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(entityID);

            uint x, y, z;
            celledRegion.UnflattenCellIndex(cellIndex, out x, out y, out z);
            string ceilingModelID = Keystone.Portals.Interior.GetInteriorElementPrefix(entityID, Keystone.Portals.Interior.PREFIX_CEILING, typeof(Keystone.Elements.Model), (int)y);
            SetAtlasTile(celledRegion, ceilingModelID, x, z, atlasTextureIndex);
        }

        void SetAtlasTile(Keystone.Portals.Interior celledRegion, string modelID, uint x, uint z, uint atlasTileIndex)
        {
            Keystone.Elements.Model model = (Keystone.Elements.Model)Keystone.Resource.Repository.Get(modelID);
            if (model == null) return;

            Keystone.Elements.Mesh3d mesh = (Keystone.Elements.Mesh3d)model.Geometry;
            Keystone.Appearance.Appearance appearance = model.Appearance;

            if (appearance == null) return; // warning: no atlas set

            if (appearance.Layers == null) return;
            if (appearance.Layers.Length == 0) return;
            if (appearance.Layers[0].Texture == null) return;
            if (appearance.Layers[0].Texture is Keystone.Appearance.TextureAtlas == false) return;

            Keystone.Appearance.TextureAtlas atlas = (Keystone.Appearance.TextureAtlas)appearance.Layers[0].Texture;
            // TODO: if instead we pass the atlas Texture node ID and an index into the atlas
            //       then we can easily compute new UV's 
            // TODO: we must either pass the atlas dimensions or pass the atlas so it can be queried
            Keystone.Celestial.ProceduralHelper.CellGrid_SetCellUV(celledRegion.CellCountX, celledRegion.CellCountZ, x, z, mesh, atlas, atlasTileIndex);
        }

        //void IEntityAPI.CellMap_SetInteriorEdgeSegmentStyle(string entityID, uint cellIndex)
        //{ 
        //}

        //void IEntityAPI.CellMap_SetExteriorEdgeSegmentStyle(string entityID, uint cellIndex)
        //{
        //}

        //void IEntityAPI.Mesh_GridCell_SetCollapseState(string entityID, string modelID, uint x, uint z, bool unCollapsed)
        //{
        //    Keystone.Portals.CelledRegion celledRegion = (Keystone.Portals.CelledRegion)Keystone.Resource.Repository.Get(entityID);
        //    Keystone.Elements.Model model = (Keystone.Elements.Model)Keystone.Resource.Repository.Get(modelID);
        //    if (model == null) return; 

        //    Keystone.Elements.Mesh3d mesh = (Keystone.Elements.Mesh3d)model.Geometry;
        //    Keystone.Celestial.ProceduralHelper.CellGrid_CellSetCollapseState(celledRegion, x, z, mesh, unCollapsed);
        //}

        //void IEntityAPI.Mesh_GridCell_SetAtlasTile(string entityID, string modelID, uint x, uint z, uint atlasTileIndex)
        //{
        //    Keystone.Portals.CelledRegion celledRegion = (Keystone.Portals.CelledRegion)Keystone.Resource.Repository.Get(entityID);
        //    Keystone.Elements.Model model = (Keystone.Elements.Model)Keystone.Resource.Repository.Get(modelID);
        //    if (model == null) return; 

        //    Keystone.Elements.Mesh3d mesh = (Keystone.Elements.Mesh3d)model.Geometry;
        //    Keystone.Appearance.Appearance appearance = model.Appearance;

        //    if (appearance == null) return; // warning: no atlas set

        //    if (appearance.Layers == null) return;
        //    if (appearance.Layers.Length == 0) return;
        //    if (appearance.Layers[0].Texture == null) return;
        //    if (appearance.Layers[0].Texture is Keystone.Appearance.TextureAtlas == false) return;

        //    Keystone.Appearance.TextureAtlas atlas = (Keystone.Appearance.TextureAtlas)appearance.Layers[0].Texture;
        //    // TODO: if instead we pass the atlas Texture node ID and an index into the atlas
        //    //       then we can easily compute new UV's 
        //    // TODO: we must either pass the atlas dimensions or pass the atlas so it can be queried
        //    Keystone.Celestial.ProceduralHelper.CellGrid_SetCellUV(celledRegion, x, z, mesh, atlas, atlasTileIndex);
        //}
        #endregion
    }

}
