using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using Amib.Threading;
using Amib.Threading.Internal;
using Keystone.Appearance;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Resource;
using Keystone.Scene;
using Keystone.Traversers;
using Keystone.Types;
using Settings;
using KeyCommon.Helpers;

namespace Keystone.IO
{
	public class SceneReader : IDisposable 
    {
        // basically can be used to notify everytime a node is read including number of nodes written and number remaining.
        // TODO: all of these callbacks "could" be handled directly in the ReadContext's and then the caller
        // can subscribe to those events..?
        public delegate void ReadProgress(string prefab, out int nodesRead, out int nodesCount);
        
        // We want all reads to be done through a single group that has a concurrency of 1.  Thus
        // our threadpool can also handle the work of other groups at the same time, but as for Writes, only one threaded
        // write at a time is possible.
        private IWorkItemsGroup mSceneReaderThreadPoolGroup;
        private SmartThreadPool mThreadPool;
        private int mTimeOut = 60000;
        private object mGroupLock;
        private const int REQUIRED_CONCURRENCY_OF_ONE = 1;

        private XMLDatabase mXMLDB;

        public SceneReader(XMLDatabase xmldb, SmartThreadPool threadpool)
        {
            if (threadpool == null) throw new ArgumentNullException();
            mThreadPool = threadpool;
            //mArchiver = archiver; // can be null for xml direct write of prefab
            mXMLDB = xmldb;
            mGroupLock = new object();
            mSceneReaderThreadPoolGroup = mThreadPool.CreateWorkItemsGroup(REQUIRED_CONCURRENCY_OF_ONE);
            mSceneReaderThreadPoolGroup.Name = "SceneWriterThreadpoolGroup";
            mSceneReaderThreadPoolGroup.OnIdle += SceneReaderThreadGroupIdle;
        }

        private void SceneReaderThreadGroupIdle(IWorkItemsGroup group)
        {
            Debug.WriteLine("SceneReader.SceneReaderThreadGroupIdle() - Theadpool Group '" + group.Name + "' idle.");
        }

        private mReadCountOnly = false;
        private int mNodesCounted = 0;
        
        public int ReadNodeCount (string typename string name)
        { 
            mReadCountOnly = true;
            bool delayPagingResources = true; // default is to not delay
            ReadSynchronous(typename, null, true, false, null, delayPagingResources, false);
            
            return mNodesCounted;
        }
        
#region Sychronous Reads
        // sychronous read that executes on the calling thread.  Note there is no callback set
        public ReadContext ReadSynchronous(string typename, string name)
        {
            bool delayTVResourceLoading = false; // default is to not delay
            return ReadSynchronous(typename, name, null, true, false, null, delayTVResourceLoading);
        }

        public ReadContext ReadSynchronous(string typename, string name, bool delayTVResourceLoading = false)
        {
            return ReadSynchronous(typename, name, null, true, false, null, delayTVResourceLoading);
        }

        /// <summary>
        /// Returns the specified node.
        /// </summary>
        /// <param name="typename">the node Typename</param>
        /// <param name="name">the node ID</param>
        /// <param name="parentName">IRRELEVANT. Only needed to distinguish overload signature</param>
        /// <param name="recursivelyLoadChildren"></param>
        /// <param name="generateIDs">if generateIDs == false, we use the existing Node IDs contained in the XML documents</param>
        /// <param name="delayTVResourceLoading"></param>
        /// <returns></returns>
        public ReadContext ReadSynchronous(string typename, string name, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, bool delayTVResourceLoading = false)
        {
            ReadContext context = new ReadContext(mXMLDB, typename, name, null, null);
            context.RecursivelyLoadChildren = recursivelyLoadChildren;
            context.DelayTVResourceLoading = delayTVResourceLoading; //TODO: this .DelayTVResourceLoading i think is never used
            context.GenerateIDs = generateIDs;
            if (nodeIDsToUse != null)
            {
                context.NodeIDsToUse = new Queue<string>();
                for (int i = 0; i < nodeIDsToUse.Length; i++)
                    context.NodeIDsToUse.Enqueue(nodeIDsToUse[i]);
            }
            ReadXmlDocumentWorker(context);
            return context;
        }

        /// <summary>
        /// Returns all children of the specified parent node but does not return the specified parent.
        /// </summary>
        /// <param name="parentTypename"></param>
        /// <param name="parentName"></param>
        /// <param name="recursivelyLoadChildren"></param>
        /// <param name="generateIDs"></param>
        /// <param name="delayTVResourceLoading"></param>
        /// <returns></returns>
        public ReadContext ReadSynchronous(string parentTypename, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, bool delayTVResourceLoading = false)
        {
            ReadContext context = new ReadContext(mXMLDB, null, null, parentName, null);
            context.ParentTypeName = parentTypename;
            context.RecursivelyLoadChildren = recursivelyLoadChildren;
            context.DelayTVResourceLoading = delayTVResourceLoading;
            context.GenerateIDs = generateIDs;
            if (nodeIDsToUse != null)
            {
                context.NodeIDsToUse = new Queue<string>();
                for (int i = 0; i < nodeIDsToUse.Length; i++)
                    context.NodeIDsToUse.Enqueue(nodeIDsToUse[i]);
            }
            ReadXmlDocumentWorker(context);
            return context;
        }

        public ReadContext ReadSynchronous(string xmlFilePath)
        {
            ReadContext context = new ReadContext(xmlFilePath, null);
            context.DelayTVResourceLoading = true;
            ReadXmlDocumentWorker(context);
            return context;
        }
#endregion // Synchronous ReadSynchronous

#region Asychronous Reads
        public void Read(string xmlFilePath, PostExecuteWorkItemCallback readCompletedHandler)
        {
            ReadContext context = new ReadContext(xmlFilePath, readCompletedHandler);
            context.DelayTVResourceLoading = true;
            Read(context);
        }

        // Asychronous Read
        public void Read(string typename, string name, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, PostExecuteWorkItemCallback readCompletedHandler)
        {
            ReadContext context = new ReadContext(mXMLDB, typename, name, parentName, readCompletedHandler);
            context.RecursivelyLoadChildren = recursivelyLoadChildren;
            context.GenerateIDs = generateIDs;
            if (nodeIDsToUse != null)
            {
                context.NodeIDsToUse = new Queue<string>();
                for (int i = 0; i < nodeIDsToUse.Length; i++)
                    context.NodeIDsToUse.Enqueue(nodeIDsToUse[i]);
            }
            context.DelayTVResourceLoading = true;
            Read(context);
        }

        // Asychronous Read
        // overload where no specific child type or child is is specified and so we return all 1st level children of the specified parent node
        public void Read(string parentTypeName, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, PostExecuteWorkItemCallback readCompletedHandler)
        {
            ReadContext context = new ReadContext(mXMLDB, null, null, parentName, readCompletedHandler);
            context.ParentTypeName = parentTypeName;
            context.RecursivelyLoadChildren = recursivelyLoadChildren;
            context.GenerateIDs = generateIDs;
            if (nodeIDsToUse != null)
            {
                context.NodeIDsToUse = new Queue<string>();
                for (int i = 0; i < nodeIDsToUse.Length; i++)
                    context.NodeIDsToUse.Enqueue(nodeIDsToUse[i]);
            }
            context.DelayTVResourceLoading = true;
            Read(context);
        }

        // asychronous load that uses thread pool but uses a WaitHandle to make sure the object is returned to the caller in a sychronous manner
        public void Read(ReadContext context)
        {
            lock (mGroupLock)
            {
                //_outstandingWork.Enqueue(context);
                //if (_outstandingWork.Count >= 1)
                AsychRead(mSceneReaderThreadPoolGroup, context);
            }
        }

        private void AsychRead(IWorkItemsGroup group, ReadContext context)
        {
            // NOTE: IWorkItemResult is basically a wrapper for WorkItem that just excludes access to certain methods which are meant to be internal only to SmartThreadPool
            IWorkItemResult result = group.QueueWorkItem(new WorkItemCallback(ReadXmlDocumentWorker), context,
                context.ReadCompletedCallback);
        }
#endregion

        // todo: i think i should switch to XMLReader and iterate through the xml rather than load the entire xml into an XmlDocument
        //       when we want to build the scene from a single xml file.
        private object ReadXmlDocumentWorker(object obj)
        {
            return ReadDocument((ReadContext)obj);
        }

        private XmlDocument ReadDocument(ReadContext context)
        {
            XmlDocument document = null;
            //document.Load(context.Stream);

            // does the node already exist in the cache.
            Node node = (Node)Repository.Get(context.NodeID);

            if (node != null)
            {
                if (node.Shareable == true)
                {
                    // if the context Parent's stack is null, we add this node as 1st level
                    if (context.Parents == null || context.Parents.Count == 0 || context.Parents.Peek() == null)
                    {
                        context.AddNode(node);
                    }
                    else // otherwise we add as child to the Parents.Peek()
                    {
                        SuperSetter super = new SuperSetter(context.Parents.Peek());
                        super.Apply(node);
                    }
                    
                    return document;
                }
                else // Shareable == false
                {
                    // There shouldn't be a case where a Node that is NOT shareable, exists in the cache already because
                    // if we want to use THE EXISTING IDs within an XMLDB (such as for a static scene or .saved Entity) 
                    // then there surely can't be multiple instances of that Entity already loaded. 
                    System.Diagnostics.Debug.WriteLine ("SceneReader.ReadDocument() - Existing Node '" + node.ID + "' of type '" + node.TypeName + "' found.  GenerateIDs == " + context.GenerateIDs.ToString() + " is this an ERROR?");
                    
                    // NOTE: this doesn't just clone Entities, but rather any node that is NOT Shareable.
                    if (context.GenerateIDs)  // don't use the existing Node we found, clone it
                    {
                        // false for "neverShare" because shareable types like mesh paths, textures,
                        // domainobject scripts can never NOT be shared.  There is just the one path
                        // string refID = context.Node.ID; // <-- NO.  We do not assign this to cloned nodes. 
                        // Only calling method decides .
                        string cloneID = Repository.GetNewName(node.TypeName);
                        if (context.GenerateIDs)
                            context.CachedIDs.Enqueue(cloneID);
                        else if (context.NodeIDsToUse != null)
                            cloneID = context.NodeIDsToUse.Dequeue();
    
                        // never clone, instead just fall through and load directly from the XML
                        node = null;
                        //node = node.Clone(cloneID, context.RecursivelyLoadChildren, false, context.DelayTVResourceLoading);
                    }
                }
            }


            XmlNode selectedParentXmlNode = null;
            XmlNode startingXmlNode = null;
            if (context.XmlParentNodes != null && context.XmlParentNodes.Count > 0)
                startingXmlNode = context.XmlParentNodes.Peek();


            if (string.IsNullOrEmpty(context.TypeName))
            {
                // if the typeName was empty, then we are not interested in any particular node
                // and want to load all children descended from the parent
                document = context.SelectXMLDocument(context.ParentTypeName);

                //  here are we actually starting with the parent node to ensure
                //  that we will get all children since we don't know the individual child types and ids
                selectedParentXmlNode = SelectXMLNode(document,
                                            startingXmlNode, context.ParentTypeName,
                                            context.ParentNodeID);
            }
            else // TypeName is valid, but "id" may not be.  Load the specified node from the document
                // recall that often when loading our sceneInfo will tell us the first node's type but not id
            {
                if (!string.IsNullOrEmpty(context.XmlFilePath))
                    document = XmlHelper.OpenXmlDocument(context.XmlFilePath);
                else
                    // this is selecting the document of the specified child type
                    // we are looking for.  This is NOT an error.  We are NOT 
                    // looking for the document of the parent.  However, this also means
                    // that we cannot retreive an array of unspecified children by going
                    // directly to the document that holds nodes of the specified type.
                    // instead we must open the parent's document so that we can get the children
                    // specified there and their types so then we can open the linked documents.
                    document = context.SelectXMLDocument(context.TypeName);

                // TODO: if we do not specify the nodeID here, i think it automatically goes for first node
                selectedParentXmlNode = SelectXMLNode(document, startingXmlNode, context.TypeName, context.NodeID);
            }

            context.XmlParentNodes.Push(selectedParentXmlNode);
            ReadXMLInput(document, context);
            context.XmlParentNodes.Pop();
            
            return document;
        }

        private void ReadXMLInput(XmlDocument document, ReadContext context)
        {
            XmlNode currentXMLNode = null;
            bool skipChildren = false;

            if (context.XmlParentNodes != null && context.XmlParentNodes.Count > 0 && context.XmlParentNodes.Peek() != null)
                currentXMLNode = context.XmlParentNodes.Peek();

            if (currentXMLNode == null)
            {
                // This is actually expected for Zone's that are empty when 
                // not deserializing empty zones is enabled.
                return;
            }

            // is our currentXMLNode "root" if so, then we are looking for all 1st level children
            if (currentXMLNode.Name == "root")
            {
                // eg. sibling 1st level nodes in a single prefab? 
                //     to be honest i dont recall the scenario that i used this
                throw new NotImplementedException();
            }
            else
            {
                Node currentSceneNode = null;
                // else if the currentXMLNode is a KGB node type, we are looking to load just that branch
                string id = "", refID = "", src = "";
                bool isRef = XmlHelper.GetAttributeValue(currentXMLNode, Core.ATTRIB_REF, ref refID);
                bool isNestedPrefabSrcID = XmlHelper.GetAttributeValue(currentXMLNode, Core.ATTRIB_SRC, ref src);
                
                
                // NOTE: isRef and isSrc can never both be true due to the way if "ref" to a dedicated
                // XML document is required, then the "src" will not be used until inside that dedicated table
                System.Diagnostics.Debug.Assert((isRef == false && isNestedPrefabSrcID == false) || isRef != isNestedPrefabSrcID,
                    "Both must be false or one must be true and the other false. They must never both be true");

                mNodesCounted++;
                
                // If isRef we need to jump to another document (eg Models.xml, Entities.xml). 
                // If 'isNestedPrefabSrcID' we jump to another .kgbentity prefab archive.
                // NOTE: for these NON inline types, the initial node that points to the dedicated xml
                // is actually a "ref" node.  So we _always_ use a ref first for any type
                // that has a dedicated xml table and then in the dedicated table we'll specify
                // a "isNestedPrefabSrcID" link to a prefab.
                if (isRef)
                {
                    // cache current NodeID and Typename
                    string previousID = context.NodeID;
                    string previousTypename = context.TypeName;
                    
                    // assign the referenceID as current NodeID
                    context.NodeID = refID;
                    context.TypeName = currentXMLNode.Name;
                    
                    // recurse
                    context.XmlParentNodes.Push(null); // new ref document means starting at root of it
                    XmlDocument refDocument = ReadDocument(context);
                    context.XmlParentNodes.Pop();
                    
                    // restore NodeID and Typename after recursion that takes place above
                    context.NodeID = previousID;
                    context.TypeName = previousTypename;
                    return; // we've completed the branch we're done
                }
                else if (isNestedPrefabSrcID) // here we must load in an instance of a prefab
                {
                    // read call will clone the src node for us regardless of whether
                    // context.GenerateIDs is true or false, HOWEVER 
                    // the current "id" we may want to keep (eg we're loading a scene file where normally
                    // we want to restore the id's that are within the scene file since the server generated them)
                    // so keep the current 'id' unless GenerateIDs == true
                    if (context.GenerateIDs)
                        id = Repository.GetNewName(context.TypeName);

                    // When reading a prefab INSTANCE we ALWAYS clone the prefab we have as SRC
                    // Even in cases where we wish to modify/tweak an existing prefab, we do it by
                    // overwriting the existing with a clone of a live instance that assumes the ResourceDescriptor
                    // as it's ID so there is never a need to make a prefab into a non cloned instance.
                    currentSceneNode = ReadNestedPrefabFromSrcID(src, id, context.DelayTVResourceLoading);
                    context.PrefabInstanceStartingNode = currentSceneNode;

                    NodeState dummy = new NodeState (Repository.GetNewName (typeof(NodeState).Name));
                    ReadNodeProperties(dummy, currentXMLNode);

                    // calling NodeState.ApplyProperties() ensures that changes to "id" are updated
                    // in the Repository cache
                    dummy.ApplyProperties(currentSceneNode);

                    // creation of any node always adds it to cache with 0 refcount, so remove it
                    Repository.IncrementRef(dummy);
                    Repository.DecrementRef (dummy);
                }
                else // neither a ref to another xml document nor a prefab SRC link, just an inline type
                {
                    if (!mReadCountOnly)
                    {
                        // instance the node
                        bool hasID = GetNodeID(currentXMLNode, currentXMLNode.Name, context.GenerateIDs, context.NodeIDsToUse, out id);
                        currentSceneNode = CreateNodeInstance(currentXMLNode.Name, id);
    
                        // NOTE: currentXMLNode that result in instanced shareable Nodes of type IGroup that already exist in Repository 
                        //       will not be recursed.  Thus we set skipChildren = true.  Currently this only seems to apply
                        //       to Behavior Sequence and Selector nodes which are shareable.  Every other type of IGroup
                        //       node is currently unshareable although in the future, we may be able to add 
                        if (currentSceneNode.Shareable && currentSceneNode is IGroup && currentSceneNode.RefCount > 0)
                        {
                            skipChildren = true;
                        }
                        // read the node properties
                        ReadNodeProperties(currentSceneNode, currentXMLNode);
    
                        if (!context.DelayTVResourceLoading)
                            if (currentSceneNode is IPageableTVNode)
                                PagerBase.LoadTVResource(currentSceneNode);
                    }
                }

                if (!mReadCountOnly)
                {
                    // if the context Parent's stack is null, we add this node as 1st level
                    if (context.Parents == null || context.Parents.Count == 0 || context.Parents.Peek() == null)
                        context.AddNode(currentSceneNode);
                    else  // otherwise we add as child to the Parents.Peek()
                    {
                        // Add node as a child to existing 1st level unless it's a NodeState of a cloned prefab Instance
                        if (currentSceneNode as NodeState != null)
                        {
                            // apply the persisted state to the corresponding node in the already instantianted prefab instance
                            NodeState ns = (NodeState)currentSceneNode;
                            // calling NodeState.ApplyProperties() ensures that changes to "id" are updated
                            // in the Repository cache
                            ns.ApplyProperties(context.PrefabInstanceStartingNode);
                        }
                        else
                        {
                            if (!context.DelayTVResourceLoading)
                                if (context.Parents.Peek() is IPageableTVNode)
                                    PagerBase.LoadTVResource(context.Parents.Peek());
    
                            SuperSetter super = new SuperSetter(context.Parents.Peek());
                            try 
                            {
                                if (!context.DelayTVResourceLoading)
                                    if (currentSceneNode is IPageableTVNode)
                                        PagerBase.LoadTVResource(currentSceneNode);
    
                                super.Apply(currentSceneNode);
                            }
                            catch (Exception ex)
                            {
                            	System.Diagnostics.Debug.WriteLine ("SceneReader.ReadXMLInput() - ERROR - Cannot add child node to parent type. " + ex.Message);
                            }
                        }
                    }
                }
                // obsolete - all deserialization uses delayed and if we want to start loading after call
                //            but before the deserialized prefab has been added to scene, we can simply
                //            make that deserialization call manually to PagerBase.LoadTVResourceSynchronously()
                //if (currentSceneNode is IPageableTVNode && context.DelayTVResourceLoading == false)
                //{
	                // June.17.2013 - Hypno - Commented out.  After the node is deserialized, eventually
	                // we must .AddChild() it to the scene and that is when Repository will call
	                // IO.PagerBase.QueuePageableResource() for this node. So it appears the following was
	                // redundant and caused a race condition.  Actually, it is useful for AssetPlacementTool
	                // where we want to load resources prior to being able to place the entity... but i think we
	                // should just do that there... and only when we aree using "ReadSynchronous".  Otherwise is makes no sense.
	                // Jan.29.2014 - Hypno - UNcommented out.  This call does not result in a queued page but an immediate one
	                // which is sometimes desired such as AssetPlacementTool and loading of a prefab from over wire during
	                // prefab insert command.
	                // TODO: perhaps a better fix is to just have caller call 
	                // Keystone.IO.PagerBase.LoadTVResource(currentSceneNode, recurse = true); and this way
	                // it can be re-used whenever i need to have a hierachy of hardcoded models loaded
	                // Keystone.IO.PagerBase.LoadTVResourceSynchronously(currentSceneNode);
                //}
                
                // NOTE: read the children occurs after the parent Node is fully paged in if DelayTVResourceLoading == false.
                // NOTE: parents are already instantiated prior to entering this foreach
	            //       so we don't have to worry about child nodes being orphans upon traversing them here
                if (!mReadCountOnly) context.Parents.Push(currentSceneNode);
                if (context.RecursivelyLoadChildren && skipChildren == false)
                {
	                foreach (XmlNode childXMLNode in currentXMLNode)
	                {
	                    context.XmlParentNodes.Push(childXMLNode);
	                    ReadXMLInput(document, context);
	                    context.XmlParentNodes.Pop();
	                }
                }
                if (!mReadCountOnly) context.Parents.Pop();

                if (isNestedPrefabSrcID)
                {
                    context.PrefabInstanceStartingNode = null;
                }
            }
        }
        
        // passing in name is good because in the various .Create methods, we dont want to assume name gets read properly from the xml
        // also, we may have passed in a new guid when we want to clone an Entity which otherwise can only have one instance
        // in the scene. (eg. loading prefabs) and thus in those instances we dont want to read\use the existing guid. 
        // TODO: perhaps a simple way to clone is to save the xmlnode to a string, then load that and replace the guid's
        // with "".  Since all Prefabs must be in a single file (must they?) then it'll automatically gen a guid.  Ugh what's the best way
        // to do this...  and now that i think about it, how would materials for instance be "ref'd" in a prefab if they are all
        // in the same xml? since normally i check for "isref" and then use SelectDocument() to find the right one!!!!  I could
        // save prefabs in zip's as well with their own seperate documents....  that could help perhaps with keeping the
        // prefab icon browser in tact, as well as keep user's textures and meshes and such all together.   The main problem there tho
        // is geometry of the same type in different prefabs would have different paths and be treated seperately and not shared! hrm.
        private bool GetNodeID(XmlNode xmlNode, string typeName, bool generateIDs, System.Collections.Generic.Queue<string> nodeIDsToUse, out string id)
        {
            id = null;
            // read the ID for this node from the xml file.  
            // NOTE: if this function is called, then this isRef == false and so the 'id' attribute must exist
            bool found = XmlHelper.GetAttributeValue(xmlNode, Core.ATTRIB_ID, ref id);
            if (!found) throw new Exception("SceneReader.GetNodeID() - 'id' attribute not found.");

            // this switch is for non clone-able nodes only and will generate and use a unique "id" in the 
            // prefab XML if the parameter cloneEntities==true
            // NOTE: if received from server, we may not want to clone entities but rather
            // use exact id's sent by the server
            bool shareableType = true;

            // WARNING: this switch requires maintenance whenever a new non-shareable type is added to the library
            switch (typeName)
            {
                case "SceneInfo":
                    // NOTE: we do not count SceneInfo as type of Node that dequeue's from nodeIDsToUse
                    if (generateIDs)
                        id = Repository.GetNewName(typeName);
                    else if (found)
                        return true;
                    else throw new Exception();
                    break;
                case "Occluder":
                case "ZoneRoot":
                case "Root":
                case "Region":
                case "Zone":
                case "CelledRegion":
                case "Interior":
                case "Structure":

                case "StellarSystem":
                case "Star":
                case "Body":
                case "World":

                case "Entity":
                case "Background3D":
                case "ModeledEntity":
                case "DefaultEntity":
                case "BonedEntity":
                case "Vehicle":
                case "Component":

                case "Player":
                case "NPC":

                case "Light":
                case "DirectionalLight":
                case "SpotLight":
                case "PointLight":

                // TODO: I think our new method should simply be to check
                // if the item node.Shareable because early on, only Entities were not shared
                // but things have changed.  Now we have Models, Selectors, Appearances, and Layers
                // for instance that can NOT be shared... are per instance.
                // But as with above Entities, the only time we would ever be allowed to 
                // share them is upon initial loading of scene and we do intend to load
                // them using the "id" that is in the xmldb.
                case "Animation":
                case "AnimationClip":
                case "BonedAnimation":
                case "ActorBlendedAnimation":
                case "TextureAnimation":
                case "EllipticalAnimation":
                case "KeyframeInterpolator_translation":
                case "KeyframeInterpolator_scale":
                case "KeyframeInterpolator_rotation":
                // TODO: this entire switch statement should be deleted and instead
                // we should look at the "shareable" property of the node... except that
                // the node isn't instanced yet so we'd have to instance a dummy first...
                // and currently we rely on computing a new replacement ID first so we can
                // pass it to the factory.
                case "Model":
                case "ModelSelector":
                case "ModelSequence":

                // appearance and textures
                case "GeometrySwitch":
                case "LODSwitch":
                case "DefaultAppearance":
                case "SplatAppearance":
                case "GroupAttribute":
                case "Appearance":
                case "TextureCycle":
                case "SplatAlpha":
                case "Diffuse":
                case "Specular":
                case "NormalMap":
                case "Emissive":
                case "VolumeTexture":
                case "DUDVTexture":
                case "CubeMap":

                case "Layer":


                // physics nodes
                case "RigidBody":
                case "CapsuleCollider":
                case "BoxCollider":
                case "SphereCollider":

                    shareableType = false;
                    break;

                default: // all other types we can share
                            // Selector
                            // Sequence
                            // Switch
                            // Script // // Keystone.Behavior.Actions.Script
                            // Mesh3d
                            // Actor3d
                            // etc
                            // ... other types of Behavior nodes are treated as 100% shareable.  Its the entity blackboard data that the behaviors use which determine how the tree nodes are selected and processed at runtime 
                    shareableType = true;
                    break;
                
            }

            if (generateIDs)
            {
                System.Diagnostics.Debug.Assert(nodeIDsToUse == null || nodeIDsToUse.Count == 0);
                if (!shareableType)
                {
                    id = Repository.GetNewName(typeName);
                }
            }
            else if (nodeIDsToUse != null && nodeIDsToUse.Count > 0 && !shareableType)
            {
                id = nodeIDsToUse.Dequeue();
            }

            return found;
        }

        private Node CreateNodeInstance(string typeName, string guid)
        {
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException();
            string id = guid;
            Node node;

            try
            {
                // note:  Repository.Create() will always first return 
                //       any existing Repository entry with that key value.
                if (typeName == "Selector" || typeName == "Sequence")
                {
                    System.Diagnostics.Debug.WriteLine("SceneReader.CreateNodeInstance() - " + typeName);
                }
                node = Resource.Repository.Create(id, typeName);
                if (node == null) throw new Exception("SceneReader.CreateNodeInstance() - Node '" + typeName + "' " could not be instanced");
                return node;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SceneReader.CreateNodeInstance() - ERROR reading node '" + typeName + "'" + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="xmlnode"></param>
        /// <returns></returns>
        private void ReadNodeProperties(Node node, XmlNode xmlnode)
        {
            if (node == null || xmlnode == null) throw new ArgumentNullException();
            PropertySpec[] specs = null;

            if (node is NodeState)
            {
                string unknownType = null;
                string unknownCategory = null;

                // for NodeState nodes, we don't know what properties are in the xml so we must gather them all
                // and create the specs ourselves
                int count = xmlnode.Attributes.Count;
                if (count == 0) return;

                specs = new PropertySpec[count];
                for (int i = 0; i < count; i++)
                {
                    XmlAttribute attribute = xmlnode.Attributes[i];
                    string propertyName = attribute.Name;
                    string propertyValue = attribute.Value;

                    // keep the "id" that was passed into this method (ie. node.ID) since we've already instanced
                    // the Node from the factory using the id we wanted.  
                    // We don't want to change it again.
                    if (propertyName == "id")
                        propertyValue = node.ID;
                    
// Dec.16.2013 - newDatabasePath modification seems unnecessary
//                    // TODO: here do we sit and watch for "datapath" so we can copy the db and update
//                    // the property accordingly?
//                    if ((propertyName == "datapath") && readID == false)
//                    {
//                        // TODO: doesn't this already occur however in CelledRegion.LoadTVResource() ?
//                        string newDatabasePath = Keystone.Portals.CelledRegion.GetDatabasePath(node.ID);
//                        //System.IO.File.Copy(propertyValue, newDatabasePath);
//                        //propertyValue = newDatabasePath;
//                    }

                    // NOTE: cast to (object) ensure correct constructor is called
                    specs[i] = new PropertySpec(propertyName, unknownType, unknownCategory, (object)propertyValue);
                }
            }
            else // we can get the list of specs and read in their values one by one
            {
                specs = node.GetProperties(true);
                if (specs == null) return;

                for (int i = 0; i < specs.Length; i++)
                {
                    if (specs[i] == null) continue; // NOTE: this usually occurs when we edit the properties but are trying to read using a previous .kgbentity save
                    string xmlAttributeValue = XmlHelper.GetAttributeValue(xmlnode, specs[i].Name);
                    // keep the "id" that was passed into this method (aka node.ID) since we've already instanced
                    // the Node from the factory using the id we wanted.  
                    // We don't want to change it again.
                    if (specs[i].Name == "id")
                        xmlAttributeValue = node.ID;

                   
                    // Dec.16.2013 - newDataPath modification seems unnecessary
                    // TODO: here do we sit and watch for "datapath" so we can copy the db and update
                    // the property accordingly?
                    //                    if ((specs[i].Name == "databpath") && readID == false)
                    //                    {
                    //                        // TODO: doesn't this already occur however in CelledRegion.LoadTVResource() ?
                    //                        string newDataPath =Keystone.Portals.CelledRegion.GetDatabasePath(node.ID);
                    //                        //System.IO.File.Copy(propertyValue, newDataPath);
                    //                        //propertyValue = newDataPath;
                    //                    }

                    // NOTE: we do not attempt to set/modify any "ref" value here.
                    // instead we allow any "ref" property to be copied normally like any other property.

                    if (!string.IsNullOrEmpty(xmlAttributeValue))
                    try 
                    {
                        specs[i].ReadXMLAttribute(xmlAttributeValue); // extension method to parse from string the VALUE based on spec's stated type
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine ("SceneReader.ReadEntityProperties() - ERROR reading property " + specs[i].Name);
                    }
                }
            }

            try
            {
                node.SetProperties(specs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SceneReader.ReadEntity() - Failed to set one or more properties for node " + node.TypeName + "  Continuing...");
            }
        }
        
        private XmlNode SelectXMLNode(XmlDocument document, XmlNode startingNode, string typename, string nodename)
        {
            if (startingNode == null)
                startingNode = document.DocumentElement;

            XmlNode selectedXmlNode;
            if (string.IsNullOrEmpty(nodename))
                // when we just want the first child and we don't know its actual name
                selectedXmlNode = XmlHelper.SelectFirstChild(startingNode);
            else
                // select the node of the specified type with the attribute of the specified value
                selectedXmlNode = XmlHelper.SelectNode(startingNode, typename, Core.ATTRIB_ID, nodename);

            return selectedXmlNode;
        }
        
        private Node ReadNestedPrefabFromSrcID(string resourceDescriptor, string newID, bool delayResourceLoading)
        {
            bool recurse = true;
            // When reading a prefab INSTANCE we ALWAYS clone the prefab we have as SRC
            // Even in cases where we wish to modify/tweak an existing prefab, we do it by
            // overwriting the existing with a clone of a live instance that assumes the ResourceDescriptor
            // as it's ID so there is never a need to make a prefab into a non cloned instance.
            bool clone = true;
            string[] clonedEntityIDs = null; // TODO: these may need to be assigned by calling function
            KeyCommon.IO.ResourceDescriptor srcDescriptor = new KeyCommon.IO.ResourceDescriptor(resourceDescriptor);
            

            // This partricular xmldatabase has only one model or one entity in it so name and parent name can be ""
            Keystone.IO.XMLDatabaseCompressed xmldatabase = new Keystone.IO.XMLDatabaseCompressed();
            Keystone.Scene.SceneInfo info = null;
            Keystone.Elements.Node clonedSrcNode = null;
            
            try
            {
            	if (srcDescriptor.IsArchivedResource)
            	{
            		string archiveFullPath = Keystone.Core.FullNodePath(srcDescriptor.ModName);
            		System.IO.Stream stream = KeyCommon.IO.ArchiveIOHelper.GetStreamFromMod(srcDescriptor.EntryName, 
            		                                                                            null, 
            		                                                                            archiveFullPath);
                    // TODO: GetStreamFromArchive(guid, "", guid); // use GUIDs and a hash table of database and entries to keep network packet small (no need for string paths)

                	info = xmldatabase.Open(stream);
                	clonedSrcNode = xmldatabase.ReadSynchronous(info.FirstNodeTypeName, resourceDescriptor, recurse, clone, clonedEntityIDs, delayResourceLoading, false);
                	
                	stream.Dispose();
                	xmldatabase.Dispose();
            	}
            	else
            	{
            		info = xmldatabase.Open (Keystone.Core.FullNodePath(srcDescriptor.EntryName), true);
            		clonedSrcNode = xmldatabase.ReadSynchronous(info.FirstNodeTypeName, null, recurse, clone, clonedEntityIDs, delayResourceLoading, false);
            	}
                // read call will clone the src node for us (context.CloneEntities value is irrelevant.  We always clone a src node)
                try
                {

                    if (clonedSrcNode == null) 
                    	throw new ArgumentNullException("Remember SelectSingleNode is case sensitive.");
                    clonedSrcNode.SRC = resourceDescriptor;
                    
                    // the cloned src node is not using the ID we want
                    // however, neither would any child Model or Appearances or sub-Entities.  
                    // The only way around that is through our NodeState's.
                    // Similarly if network server told us to load a prefab but to consist of certain nodestates
                    // We'd apply those states afterwards.
                    // So go ahead and return the clonedSrcNode for now...
                    return clonedSrcNode;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("SceneReader.ReadNestedPrefabFromSrcID() - ERROR cloning node '" + resourceDescriptor + "'. " + ex.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SceneReader.ReadNestedPrefabFromSrcID() - ERROR opening XMLDB prefab. " + ex.Message);
                return null;
            }
        }
        
        #region IDisposable Members
        public void Dispose()
        {
            mSceneReaderThreadPoolGroup.OnIdle -= SceneReaderThreadGroupIdle;
            mSceneReaderThreadPoolGroup = null;
        }

        #endregion
    }
}