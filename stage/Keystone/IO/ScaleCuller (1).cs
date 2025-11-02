#define MULTITHREADED 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Keystone.Cameras;
using Keystone.Culling;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.FX;
using Keystone.Lights;
using Keystone.Octree;
using Keystone.Portals;
using Keystone.QuadTree;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Traversers
{

    public class ScaleCuller : ITraverser
    {
        //       re: use of EntityFlags for excluding certain items during traversals..
        //       I think in a traditional scenegraph, a "layer" would be an attribute node
        //       It would not be culled, it would just be evaluated.  The problem however
        //       is that such a layer since it's not a spatial thing itself, you cannot drag
        //       and drop stuff within scene into different layers.  Layers are not physical volumes.
        //       THey are purely virtual.  This is why it seems the only way to really do them
        //       is via Entities and then perhaps to allow that layers can be (optionally)
        //       hierarchically inherited just like say rotations and scale.

        RenderingContext.VisibleItemFoundDelegate mItemFoundDelegate;
        RenderingContext.VisibleLightFoundDelegate mLightFoundDelegate;
        RenderingContext.RegionSetCreatedDelegate mRegionSetCreatedHandler;

        private RenderingContext mContext;
        private KeyCommon.Traversal.CullParameters mCullParameters;
                
#if MULTITHREADED
        private static Amib.Threading.IWorkItemsGroup mWorkItemsGroup;
        private Stack<SceneNode> mNodesToTraverse = new Stack<SceneNode>();
#else
        private Stack<IntersectResult> _frustumIntersectResult = new Stack<IntersectResult>();
        
        private RegionPVS _currentRegionPVS = null;
        private Stack<RegionPVS> _regionPVSStack = new Stack<RegionPVS>();
        private double _regionZoomScaling;
#endif
 
        // TODO: this should just be List<IOccluder> or something?
        private List<BoundingCone> _coneOccluders = new List<BoundingCone>();
        private List<Occluder> _occluders = new List<Occluder>();
 


        internal ScaleCuller(RenderingContext context, 
                            RenderingContext.RegionSetCreatedDelegate regionPVSCreatedDelegate, 
                            RenderingContext.VisibleItemFoundDelegate itemFoundDelegate, 
                            RenderingContext.VisibleLightFoundDelegate lightFoundDelegate)
        {
            mContext = context;
            mItemFoundDelegate = itemFoundDelegate;
            mLightFoundDelegate = lightFoundDelegate;
            mRegionSetCreatedHandler = regionPVSCreatedDelegate;


#if MULTITHREADED
            mWorkItemsGroup = CoreClient._Core.ThreadPool.CreateWorkItemsGroup(Environment.ProcessorCount);
#endif
      }

        
        private void Clear()
        { 
#if MULTITHREADED
            mNodesToTraverse.Clear();
#else
            _regionPVSStack.Clear();
            _currentRegionPVS = null;
            _frustumIntersectResult.Clear();
#endif
            _coneOccluders.Clear();
            _occluders.Clear();
        }


		TVRenderSurface mOcclusionSurface; // surface shared by all Zones
		float occlusionSurfaceSizeFactor = 2f; // 1f... 4f 
        internal RegionPVS Cull( SceneNode node, KeyCommon.Traversal.CullParameters parameters)
        {
            System.Diagnostics.Debug.Assert(node is RegionNode);

            if (mContext.Camera == null || mContext.Viewpoint == null)
            {
                Trace.WriteLine("RenderingContext.Cull() -- Waiting for Dedicated Camera Creation.");
                return null; // when first initializing the scene the context.Camera may not quite be created yet via RenderingContext.CreateDedicatedCamera()
            }

            mCullParameters = parameters;
            if (mCullParameters.HardwareOcclussionEnabled)
            {
            	if (mOcclusionSurface == null)
            	{
            		mOcclusionSurface = CoreClient._CoreClient.Scene.CreateRenderSurfaceEx(-1, -1, 
            		                                                                       CONST_TV_RENDERSURFACEFORMAT.TV_TEXTUREFORMAT_R5G6B5,
            		                                                                       true, true,
            		                                                                       1.0f / occlusionSurfaceSizeFactor);
            	}
            }
            Clear();


            // TODO: do we have to bind this at all really?  i think the main purpose
            // to the bindable stack is to know when the background has changed
            // and issue events... but what i don't get is any other use for a "stack"
            // of Background nodes.  Perhaps just that the default can be pushed on first
            // and then during our search, another could be found and added ontop
            // but still... there'd be no good reason to respond to an unbound event at that point
            // 
            // TODO: if we try to add the background to the camera's RegionPVS, then we would have
            // to iterate through all RegionPVS to find the one that contained the background.  
            // that would be silly. The background should have it's own dedicated PVS, but still
            // rendered with respect to the camera region.  That way when viewed from interior
            // we'd still see stars in right relation to interior which is in a seperate coordinate system.
            // That is to say, we must not render it relative to the interior.
            // WE MUST TEST THIS as we cross Regions with backgrounds set in Zones and not just at root
            // and from interior with background both at root and in seperate exterior Zones and verify
            // all cases work properly. TODO: also dont forget my lights i believe are not being
            // transformed with respect to region when a given light affects cross region boundaries
            //
            //
            // background node determination
            // NOTE: This is problematic.  if the background is added to current camera zone
            // and not Root, then this background will not be found
            // TODO: set "background" layer flag for cull filtering to the background entity
            Background3D background = FindBackgroundNode(mContext.Region);

            // TODO: fog fields would be a lot easier because
            // they would get associated with each visibleItem as they were encountered..
            // they'd in fact be very similar to how light volumes work... except
            // only one fog volume is allowed 


#if MULTITHREADED == false
                        
            _regionZoomScaling = 1.0d; // GetNavZoomFactor();
             node.Traverse(this, null);
#else
            CullState state;
            state.Context = mContext;
            state.SceneNode = node;
		
            // TODO: it seems this first pvs is never used.  it never gets added
            // to context.mRegionSets because mRegionSetCreatedHandler is never invoked
            RegionNode dummyRegionNode = null;
            RegionPVS dummyPVS = new RegionPVS(dummyRegionNode, state.Context,
                    						   state.Context.Camera.InverseView,
                    						   state.Context.Camera.View,
                    						   mContext.Position);

            state.CameraSpacePosition = mContext.Position;
            state.CameraSpaceBoundingBoxVertices = null;
            state.PVS = dummyPVS;
            state.ParentIntersectResult = IntersectResult.INTERSECT;

            // psuedo - while able to traverse
            //        - for each child, queue cull task
            //          - when recursing a child, we cannot complete the parent until 
            //            all children are completed.  How do we guarantee this orderly traversal?
            // http://blogs.msdn.com/b/pfxteam/archive/2008/01/31/7357135.aspx
            TraverseChildSceneNodes(state);
           
            // must wait here until all items for this group are completed
            mWorkItemsGroup.WaitForIdle();
#endif
        
        // TODO: fog volumes should be treated similar to Lights and added to VisibleItem as 
        //       rendering state modifers
        
            // bind background now that the 1st RegionPVS exists
            if (background != null) mContext.Bind(background);

            // bind the foreground (aka local) node
            // perhpas i mean HUD and now i do that from workspace.hud

             return dummyPVS;
        }

        internal void Cull( RegionPVS pvs, SceneNode node, KeyCommon.Traversal.CullParameters parameters)
        {

            if (mContext.Camera == null || mContext.Viewpoint == null)
            {
                Debug.WriteLine("RenderingContext.Cull() -- Waiting for Dedicated Camera Creation.");
                return; // when first initializing the scene the context.Camera may not quite be created yet via RenderingContext.CreateDedicatedCamera()
            }


            mCullParameters = parameters;
            //Clear();  // we dont want to clear from main cull
#if MULTITHREADED == false
                        
            _regionZoomScaling = 1.0d; // GetNavZoomFactor();
             node.Traverse(this, null);
#else
            CullState state;
            state.Context = mContext;
            state.SceneNode = node;

            state.PVS = pvs;
            state.CameraSpacePosition = mContext.Position;
            state.CameraSpaceBoundingBoxVertices = null;
            state.ParentIntersectResult = IntersectResult.INTERSECT;
			
            TraverseChildSceneNodes(state);
           
            // must wait here until all items for this group are completed
            mWorkItemsGroup.WaitForIdle();
#endif
        }

#if MULTITHREADED
        private struct CullState
        {
            public RenderingContext Context;
            public Vector3d CameraSpacePosition;
            public Vector3d[] CameraSpaceBoundingBoxVertices;
            public RegionPVS PVS;
            public object SceneNode; // can be either OctreeOctant or a SceneNode
            public IntersectResult ParentIntersectResult;

            public CullState Clone()
            {
                CullState copy;
                copy.Context = Context;
                copy.PVS = PVS;
                copy.SceneNode = SceneNode;
                copy.ParentIntersectResult = ParentIntersectResult;
                copy.CameraSpacePosition = CameraSpacePosition;
                copy.CameraSpaceBoundingBoxVertices = CameraSpaceBoundingBoxVertices ;
                //copy.ViewScaling = ViewScaling;

                return copy;
            }
        }

        private void CullCompletion(object state)
        {
            Amib.Threading.IWorkItemResult result = (Amib.Threading.IWorkItemResult)state;
            
            CullState cullState = (CullState)result.State;
            if (cullState.SceneNode is EntityNode)
            {
            	EntityNode en = (EntityNode)cullState.SceneNode;
            	if (en.Entity is Root)
            	{
            		Debug.WriteLine ("ScaleCuller.CullCompletion() - Cull completed.");
            		
            		// occlussion can now be serialized.  in fact, perhaps it can be done
            		// in ScaleDrawer since they are sorted into buckets?  and clearly our
            		// structure octree is a 
            	}
            }
        }
#endif

        private Background3D FindBackgroundNode(Region cameraRegion)
        {
            // if not found, recurse upwards to next region
            Entity parent = cameraRegion;
            while (parent != null)
            {
                foreach (Node n in parent.Children)
                    if (n as Background3D != null)
                    {
	                	return (Background3D)n;
                    }

                // TODO: what if there's a portal?  we would need to traverse through them too
                // but then we have multiple paths, how would we pick a background if there were more
                // than one at equal depths?
                parent = parent.Parent;
            }
            return null;
        }

#region VisibilityTest
#if MULTITHREADED
		private Vector3d[] GetCameraSpaceBoxVertices (BoundingBox box, Vector3d regionRelativeCameraPosition)
		{
        	using (CoreClient._CoreClient.Profiler.HookUp("GetCameraSpaceBox"))
        	{
        		Vector3d[] cameraSpaceBoxVertices = BoundingBox.GetVertices(box);
        		for (int i = 0; i < cameraSpaceBoxVertices.Length; i++)
        			cameraSpaceBoxVertices[i] -= regionRelativeCameraPosition;
        		
    	    	return cameraSpaceBoxVertices;
        	}
		}
		
        private BoundingBox GetCameraSpaceBox (BoundingBox box, Vector3d regionRelativeCameraPosition)
        {
        	using (CoreClient._CoreClient.Profiler.HookUp("GetCameraSpaceBox"))
        	{
	        	// TODO; BoundingBox should be a struct.
	        	// TODO: this is causing massive frame rate slowdown... GC?
        		BoundingBox cameraSpaceBox = new BoundingBox(box.Min - regionRelativeCameraPosition, box.Max - regionRelativeCameraPosition);
    	    	return cameraSpaceBox;
        	}
        }
        
        private IntersectResult PushVisibilityTestResult(ref CullState state, BoundingBox box, bool useLargeFrustum, bool selected, bool showBoundingBox, CONST_TV_COLORKEY color)
        {       	
        	// temp hack for testing if culling is causing issue with some planets not rendering: 
        	//return IntersectResult.INSIDE ;

            // here we no longer have a visibility stack so our objective is simply to determine the visibility
            // so that the caller can set the results to on a new state that will be passed to children
            RegionPVS current = state.PVS;
            IntersectResult result;
            Vector3d[] cameraSpaceBoxVertices = null;
            double distanceToCameraSq = 0;
            
           	cameraSpaceBoxVertices = GetCameraSpaceBoxVertices(box, current.RegionRelativeCameraPos);        
            state.CameraSpaceBoundingBoxVertices = cameraSpaceBoxVertices;
            //if (useLargeFrustum)
            //{
    	      	// NOTE: for space planet and star rendering, the following works and the GetCameraSpaceBoxVertices() above does not
 				BoundingBox cameraSpaceBox = GetCameraSpaceBox (box, current.RegionRelativeCameraPos);           
 				distanceToCameraSq = Vector3d.GetLengthSquared (cameraSpaceBox.Center);
            //}

            
            // if the parent node is fully visibile, then so is the child.  Return the same result and skip the frustum test
            if (state.ParentIntersectResult == IntersectResult.INSIDE)
            {
                result = IntersectResult.INSIDE;
            }
            else
            {
                // if the parent is INTERSECT then, run a visible test on this child's box and push the result
                // NOTE: we dont need to first check for an OUTSIDE result because
                // the callers to this function do that on the parent.  We may decide to 
                // modify this behavior in the future such that its not posssible to forget to do that
                // in future Apply() overload.
                using (CoreClient._CoreClient.Profiler.HookUp("IntersectTest"))
                {
                	if (useLargeFrustum)
						result = current.IntersectionTest (cameraSpaceBoxVertices, distanceToCameraSq); 
                	else 
                		result = current.IntersectionTest (cameraSpaceBoxVertices, 0);
                }
            }


            if (showBoundingBox)
            {
                if (selected)
                    color = state.Context.SelectedColor;

                // TODO: "AddToLargeFrustum" bool is horrible.  Eventually I will get rid of these two frustum types
                //       and instead, set flag so that certain large celestial bodies will get a custum frustum made
                //       for it, just when rendering it.  This only applies to large items that are within a certain range
                //       since otherwise we will switch to scaling the target and/or using billboards (i.e. imposter)
                // TODO: I think that the custom frustum and projection we use also needs to be used when Projecting 3d to 2d coords for 
                //       things like planet labels.
                
                // TODO: if a group is selected in plugin Model panel, render the bbox for the selected group
              
              // TODO: allow mouse oicking by group and to show bbox for that group. also, plugin group index should be listed next to each grouo
              PickResults pr = state.Context.Workspace.SelectedEntity;
              
              // AUGUST
              // todo: trsnsform group box to cameraSpaceBox
              BoundingBox groupBox =  pr.Geometry.GeBoundingBox ( pr.GroupIndex);
                
                current.Add (BoundingBox.GetEdges (cameraSpaceBox), color, useLargeFrustum);
                // NOTE: cameraSpaceBoxVertices array are not in correct order to be rendered as line list
                //current.Add(cameraSpaceBoxVertices, color, useLargeFrustum);
            }

            return result;
        }

        private IntersectResult PushVisibilityTestResult(ref CullState state, Vector3d[] regionSpaceCoordinates, bool useLargeFrustum, bool drawBounds, bool selected, out Vector3d[] cameraSpaceCoords)
        {
            RegionPVS current = state.PVS;
            IntersectResult result;

            cameraSpaceCoords = new Vector3d[4];
            //Matrix matrix = Matrix.Translation(_relativeRegionOffsets.Peek()) * p.RegionMatrix;
            cameraSpaceCoords[0] = regionSpaceCoordinates[0] - current.RegionRelativeCameraPos;
            cameraSpaceCoords[1] = regionSpaceCoordinates[1] - current.RegionRelativeCameraPos;
            cameraSpaceCoords[2] = regionSpaceCoordinates[2] - current.RegionRelativeCameraPos;
            cameraSpaceCoords[3] = regionSpaceCoordinates[3] - current.RegionRelativeCameraPos;
            state.CameraSpaceBoundingBoxVertices = cameraSpaceCoords ;
            
            // TODO: this distance should be from center of the cameraSpaceCoords rectangle
            double distanceToCameraSquared = Vector3d.GetLengthSquared(cameraSpaceCoords[0]);
            result = current.IntersectionTest(cameraSpaceCoords, distanceToCameraSquared);

            // draw the portals if enabled
            if (drawBounds)
            {
                // note: the lines are already in camera space
                Line3d[] lines = new Line3d[4];
                lines[0] = new Line3d(cameraSpaceCoords[0], cameraSpaceCoords[1]);
                lines[1] = new Line3d(cameraSpaceCoords[1], cameraSpaceCoords[2]);
                lines[2] = new Line3d(cameraSpaceCoords[2], cameraSpaceCoords[3]);
                lines[3] = new Line3d(cameraSpaceCoords[3], cameraSpaceCoords[0]);

                CONST_TV_COLORKEY color;
                if (selected)
                    color = state.Context.SelectedColor;
                else
                    color = CONST_TV_COLORKEY.TV_COLORKEY_RED;

                // add these 2d elements to the current regionPVS so that the correct view/projection
                // matrices are used
                // _currentRegionPVS.Add(lines, color);
            }

            return result;
        }
#else
        /// <summary>
        /// Region centric - camera space frustum culling with relative region scaling/rotation applied to the view matrix used by the frustum.  
        /// Accepts a box that is in the coordinate system (already scaled and rotated and translated) to it's own local region 
        /// and then uses the current frustum which was created to be relative to that local region.  
        /// It is relative to that local region in terms of scale and rotation. 
        /// TODO: But why is not also relative in terms of position?  Then we wouldn't even need to
        /// translate the bounding boxes....  I dont understand why that is not automatic.
        /// 
        /// This way there is no need to transform the bounding box of any entity, but merely translate it.
        /// If we do not translate it to camera relative position, the values in the frustum planes are too huge and inaccurate culling
        /// occurs.  I've tested this all ways and this is the only way that works perfectly.
        /// </summary>
        /// <remarks >
        /// Frustum test is short circuited if a parent node of the current child's box was fully visible since the child must be fully visible.
        /// </remarks>
        /// <param name="box"></param>
        private void PushVisibilityTestResult(BoundingBox box, bool selected, bool showBoundingBox, CONST_TV_COLORKEY color)
        {
            RegionPVS current = _regionPVSStack.Peek();
            BoundingBox cameraSpaceBox = new BoundingBox(box.Min - current.RegionRelativeCameraPos, box.Max - current.RegionRelativeCameraPos);
            
            // if the parent node is fully visibile, then so is the child.  Push the same result and skip the frustum test
            if (_frustumIntersectResult.Count > 0 && _frustumIntersectResult.Peek() == IntersectResult.INSIDE)
            {
                _frustumIntersectResult.Push(IntersectResult.INSIDE);
            }
            else
            {
                // if the parent is INTERSECT then, run a visible test on this child's box and push the result
                // NOTE: we dont need to first check for an OUTSIDE result because
                // the callers to this function do that on the parent.  We may decide to 
                // modify this behavior in the future such that its not posssible to forget to do that
                // in future Apply() overload.
                IntersectResult result = current.IntersectionTest(cameraSpaceBox);
                _frustumIntersectResult.Push(result);
            }
                        
            if (showBoundingBox)
            {                
                if (selected)
                    color = mContext.SelectedColor;
                       
                current.Add(cameraSpaceBox, color, false); // Hypnotron Dec.4.2014 - switched to current from _currentRegionPVS below.  Does it still work properly?
                // _currentRegionPVS.Add(cameraSpaceBox, color, false);
            }

            return;
        }

        private void PushVisibilityTestResult(Vector3d[] regionSpaceCoordinates, bool drawBounds, bool selected, out Vector3d[] cameraSpaceCoords)
        {
            RegionPVS current = _regionPVSStack.Peek();
            cameraSpaceCoords = new Vector3d[4];
            //Matrix matrix = Matrix.Translation(_relativeRegionOffsets.Peek()) * p.RegionMatrix;
            cameraSpaceCoords[0] = regionSpaceCoordinates[0] - current.RegionRelativeCameraPos;
            cameraSpaceCoords[1] = regionSpaceCoordinates[1] - current.RegionRelativeCameraPos;
            cameraSpaceCoords[2] = regionSpaceCoordinates[2] - current.RegionRelativeCameraPos;
            cameraSpaceCoords[3] = regionSpaceCoordinates[3] - current.RegionRelativeCameraPos;

            IntersectResult result = _regionPVSStack.Peek().IntersectionTest(cameraSpaceCoords);
            _frustumIntersectResult.Push(result);

            // draw the portals if enabled
            if (drawBounds)
            {
                // note: the lines are already in camera space
                Line3d[] lines = new Line3d[4];
                lines[0] = new Line3d(cameraSpaceCoords[0], cameraSpaceCoords[1]);
                lines[1] = new Line3d(cameraSpaceCoords[1], cameraSpaceCoords[2]);
                lines[2] = new Line3d(cameraSpaceCoords[2], cameraSpaceCoords[3]);
                lines[3] = new Line3d(cameraSpaceCoords[3], cameraSpaceCoords[0]);

                CONST_TV_COLORKEY color;
                if (selected)
                    color = mContext.SelectedColor;
                else
                    color = CONST_TV_COLORKEY.TV_COLORKEY_RED;

                // add these 2d elements to the current regionPVS so that the correct view/projection
                // matrices are used
//                _currentRegionPVS.Add(lines, color);
            }
        }

        private void PopVisibilityTestResult()
        {
            _frustumIntersectResult.Pop();
        }
#endif
        #endregion

        private bool Allow(Entity entity)
        {
            // Viewpoint nodes are skipped because they are hardcoded to always have Visible = false which prevents attempts at render, 
            // but still allows physics updates.
            bool visible = entity.Visible || entity is DefaultEntity;
            return (visible == true &&
                   (entity.Attributes & mCullParameters.AllowedAttributes) > KeyCommon.Flags.EntityAttributes.None); // TODO: Fix below lines - they are causing culling to always be Allow == false // &&
                  // (mCullParameters.CullPassType == KeyCommon.Traversal.CullPassType.Normal && entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Occluder) == false) &&
                   //(mCullParameters.CullPassType == KeyCommon.Traversal.CullPassType.Occlussion && entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Occluder));
        }

        private bool Include(Entity entity)
        {
            return (entity != null &&
                    entity.Enable == true &&
                    entity.PageStatus == IO.PageableNodeStatus.Loaded &&
                    (entity.Attributes & mCullParameters.IncludedAttributes) > KeyCommon.Flags.EntityAttributes.None);
        }

        // obsolete - Feb.22.2107 - using Allow() and Include() instead
        //private bool Ignore (Entity entity)
        //{
        //	// Viewpoint nodes are skipped because they are hardcoded to always have Visible = false which prevents attempts at render, 
        //	// but still allows physics updates.
        //	return (entity.Visible == false || 
        //           (entity.Attributes &  mCullParameters.IgnoredAttributes) != KeyCommon.Flags.EntityAttributes.None) ||
        //		   (mCullParameters.CullPassType == KeyCommon.Traversal.CullPassType.Normal && entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Occluder)) ||
        //		   (mCullParameters.CullPassType == KeyCommon.Traversal.CullPassType.Occlussion && entity.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Occluder) == false);
        //}
        
        
        //private bool Exclude (Entity entity)
        //{
        //	// TVResourceIsLoaded is very useful to skip items like Structure.cs that self manage their own Geometry child nodes
        //    // NOTE: We don't check for .Geometry != null because root entities dont always have geometry yet we are still required 
        //    // to Scale them if their child entities do have children.  We save .Geometry null test for Apply (Model)
        //	return (entity == null ||
        //            entity.Enable == false ||
        //            entity.PageStatus != IO.PageableNodeStatus.Loaded ||
        //            (entity.Attributes &  mCullParameters.ExcludedAttributes) != KeyCommon.Flags.EntityAttributes.None);
        		       	
        //}
        
        #region ITraverser Members
        // TODO: there's no need for SceneNode because all SceneNode's must contain
        // entities.   What i should do is have SceneNode as default with an Entity
        // and delete EntityNode (actuall delete SceneNode and rename EntityNode)
        // and then make RegionNode derived from EntityNode
        public object Apply(SceneNode node, object data)
        {
            throw new Exception("Should never reach here.  All SceneNodes must contain Entities even if those entities are not renderable.");
        }

#if MULTITHREADED
        public object Apply(RegionNode node, object data)
        {
        	CullState state = (CullState)data;
        	if (state.Context.Region == null) return null;
      
			mContext.Statistics.IncrementStat("Regions Visited");

            Matrix tmpView;
            Matrix invView;
            Vector3d cameraTranslation;



            
            // if this scene node does not reference the current camera's Region then 
            // the two nodes are in different coordinate systems and 
            // we must transform the frustum when switching to the next region for culling + rendering
            
            // TODO: why is our Context.Region incorrect when in Navigation Workspace main viewport?
            if (node.Region != state.Context.Region)// && node.Region is Root)
            {
                // TODO: I think that the following TODO about interiors in multi zones being broken is no longer true...
                // TODO: The following works with a single large root region and an Interior inside it
                // but the Interior won't work in multi zones
                // (eg. generated universe) that has many sibling zones and I believe it's
                // because the Matrices for those child regions is Identity!  And I believe that is
                // because our fixed exterior zone regions RegionMatrix property requires the
                // Matrix be identity or else child entities will not get region centric coords at all
                // but rather global and we dont want global because we need seperation of zones
                // to minimize imprecision errors.  So hopefully the simple answer is rather than
                // node.Region.GetRegionalTransform()  that we allow for the final relative
                // translation matrix to be applied here or passed in as a param.  This way 
                // we dont have to store a second matrix, we can use relative translation instead
                // of the full translation which would result in too much imprecision
                // So the current region uses identity, and all others have an an extra
                // relative translation matrix included in the computation

                // MPJ - June.20.2011 - added a relative transform offset so as to maintain precicison
                // we can add scaling and rotation back in after we compute the translation difference
                //int[] offsetDifference = new int[3]; // if both are Zones, then find the differences in offset
                // if one is an interior, but yet the camera is outside that interior
                // then we still have to multiply in the offset of the interior's zone 
                // with respect to the camera's zone


                Matrix source2dest = Matrix.Source2Dest(state.Context.Region.GlobalMatrix, node.Region.GlobalMatrix);


                // our camera's view matrix is computed with the position component at Origin
                // if we multiply that view matrix with this frame matrix, we'll result in a new view matrix that is relative
                // to the new frame but no longer in camera space.  In other words, it'll be relative to our Interior at the proper
                // rotation and distance when using the Interior's own coordinate system for culling and rendering that interior.
                // The idea is we want to be able to cull bounding box and mesh without having to transform them (just translate only)
                // but if we do want to translate them to camera space during the cull/render, we need to subtract the offsets of the original camera
                // position (i.e mContext.Position) as well as the offset computed by the new view matrix.
                // So below we compute a new view with the translation because our source2dest includes the region relative position
                // of the object and whereas the view cached in our camera is set to origin
                //Matrix.Multiply4x4  or wait.. maybe that's only for projection...??????????
                tmpView = Matrix.Multiply4x4(source2dest, Matrix.InverseView(state.Context.Camera.InverseView * Matrix.CreateTranslation(state.Context.Position)));

                // TODO: Eventually i think(?????) we'll be able to get rid of this InverseView step
                // because by supply a camera offset in to an improved version of .GetRelativeGlobalTransform(cameraSpaceoffset)
                // so that the returned source2dest will remain in camera space... we'll see
                //
                //
                // we need to create the inverse so we can grab the camera position, cache the translation and then reset in inverse to origin
                invView = Matrix.InverseView(tmpView);
                cameraTranslation.x = invView.M41;
                cameraTranslation.y = invView.M42;
                cameraTranslation.z = invView.M43;

                invView.M41 = 0;
                invView.M42 = 0;
                invView.M43 = 0;

                float det = 0;
                TV_3DMATRIX inv = Helpers.TVTypeConverter.ToTVMatrix(invView);
                TV_3DMATRIX view = default(TV_3DMATRIX);
                CoreClient._CoreClient.Maths.TVMatrixInverse(ref view, ref det, inv);
                tmpView = Helpers.TVTypeConverter.FromTVMatrix(view);
            }
            else
            {
                invView = state.Context.Camera.InverseView;
                tmpView = state.Context.Camera.View;
                cameraTranslation = state.Context.Position;  
            }


            state.PVS = new RegionPVS(node, state.Context, invView, tmpView, cameraTranslation);
           
            // NOTE: These translations never need to take into account scalng or rotation 
            // because that is done in the View transform we compute
            // TODO: so why not just include that camera space translation in the view matrix?
            // Well, that would add a very large value to the View matrix 

            // add to the total list of RegionSets whereas the stack version is for runtime tracking of
            // the current set
            mRegionSetCreatedHandler (state.PVS);
            
            
            // - portal frustum's should be created here not just regular full frustums if we've traveled to thsi region through
            //   a portal
            // how do i take the existing frustum and do something like create a new frustum in that region's space that also
            // utilizes that portal?  I guess the easiest way is to first create the new frustum, then create frustum planes
            // again for that portal based on the new reference frame and use that to continue culling.
            // so it's all more complicated than you think however the number of code lines is small and hopefully once it works
            // and we get picking working, we'll be done with it 
            // following we are testing this Region node's bounding box against the regular frustum we pushed on the stack. 
            // what we havent done is first perhaps use the large frustum and then cull only celestial.body children
            // we really do have to do two passes with both frustums because
            // 1) only the really large frustum will catch planets that are in sectors that would be in visible range
            // TODO: if we've entered this region through a portal, we should build the new frustum using the bounds of that portal
            // and not simply based on Near/Far/FOVRadians.  In other words, we should create a new PortalFrustum based on the current frustum (frustumstack.Peek())


             
            // the above code computes the frustum based on the Region and computes the InverseView used for that frustum so the 
            // Renderer can use it as well for the appropriate states.  Below we now traverse and test for visibility
            IntersectResult intersection = PushVisibilityTestResult(ref state, 
                                                                    node.BoundingBox,
                                                                    false,
													                false,
													                state.Context.ShowRegionBoundingBoxes, 
													                CONST_TV_COLORKEY.TV_COLORKEY_YELLOW);

            state.ParentIntersectResult = intersection;
            return state;
        }
#else
        public object Apply(RegionNode node, object data)
        {
            if (mContext.Region == null) return null;

            mContext.Statistics.IncrementStat("Regions Visited");
            
            // TODO: shis scale should apply only to the camera right?
            //If we want to scale the viewport for wide out solar system view, then that scale added now requires
            // more than simple translation approach to camera space culling.  It requires a transform be applied.  Or wait... why
            // not just put the scale in the camera only?  not in any entity?  but then im sure i do need the ability to scale Regions
            // and have that scale be hierarchical.  Even if not this game, then down the line its proper for any engine.  I think our
            // universgen planets use scale to have different sphere mesh sizes when recyling the same sphere.tvm.  
            // BUT even with scale, this can be simply applied to the view as we traverse 
            Vector3d scale;
            scale.x = _regionZoomScaling;
            scale.y = _regionZoomScaling;
            scale.z = _regionZoomScaling;

            node.Region.Scale = scale; 

            Matrix tmpView;
            Matrix invView;
            Vector3d cameraTranslation;

                // KEEP IN MIND 4: I think for picking, we can do exact same as culling!  We simply first transform the ray to region relative
                // and then do ray origin space picking.  And I'm pretty sure we can use the previous cull if we arent doing that already to limit
                // the items we will test against.  We shouldn't have to test against the entire scene right?  Also to avoid having to 
                // set a new matrix to every mesh and we simply have a relatively simple transform of the ray itself to object space as 
                // opposed even to ray space.  It's basically an opposite translation to the ray origin which is tons faster.  
                // Further, we dont even have to useTV' advanced collide which assumes a world position ray.  We can
                // itterate the triangles in object space.  Considering we're only needing to do that for a few items that are hit pre-depth sort, its
                // should be very fast.


            // if this scene node does not reference the current camera's Region then 
            // the two nodes are in different coordinate systems and 
            // we must transform the frustum when switching to the next region for culling + rendering
            if ( node.Region != mContext.Region)// && node.Region is Root)
            {
              
                // TODO: The following works with a single large root region and an Interior inside it
                // but the Interior won't work in multi zones
                // (eg. generated universe) that has many sibling zones and I believe it's
                // because the Matrices for those child regions is Identity!  And I believe that is
                // because our fixed exterior zone regions RegionMatrix property requires the
                // Matrix be identity or else child entities will not get region centric coords at all
                // but rather global and we dont want global because we need seperation of zones
                // to minimize imprecision errors.  So hopefully the simply answer is rather than
                // node.Region.GetRegionalTransform()  that we allow for the final relative
                // translation matrix to be applied here or passed in as a param.  This way 
                // we dont have to store a second matrix, we can use relative translation instead
                // of the full translation which would result in too much imprecision
                // So the current region uses identity, and all others have an an extra
                // relative translation matrix included in the computation

                // MPJ - June.20.2011 - added a relative transform offset so as to maintain precicison
                // we can add scaling and rotation back in after we compute the translation difference
                //int[] offsetDifference = new int[3]; // if both are Zones, then find the differences in offset
                                                     // if one is an interior, but yet the camera is outside that interior
                                                     // then we still have to multiply in the offset of the interior's zone 
                                                     // with respect to the camera's zone


                Matrix source2dest = Matrix.Source2Dest(mContext.Region.GlobalMatrix, node.Region.GlobalMatrix);

                
                // our camera's view matrix is computed with the position component at Origin
                // if we multiply that view matrix with this frame matrix, we'll result in a new view matrix that is relative
                // to the new frame but no longer in camera space.  In other words, it'll be relative to our Interior at the proper
                // rotation and distance when using the Interior's own coordinate system for culling and rendering that interior.
                // The idea is we want to be able to cull bounding box and mesh without having to transform them (just translate only)
                // but if we do want to translate them to camera space during the cull/render, we need to subtract the offsets of the original camera
                // position (i.e mContext.Position) as well as the offset computed by the new view matrix.
                // So below we compute a new view with the translation because our source2dest includes the region relative position
                // of the object and whereas the view cached in our camera is set to origin
                //Matrix.Multiply4x4  or wait.. maybe that's only for projection...??????????
                tmpView = Matrix.Multiply4x4(source2dest, Matrix.InverseView(mContext.Camera.InverseView  * Matrix.CreateTranslation(mContext.Position)));

                // TODO: Eventually i think(?????) we'll be able to get rid of this InverseView step
                // because by supply a camera offset in to an improved version of .GetRelativeGlobalTransform(cameraSpaceoffset)
                // so that the returned source2dest will remain in camera space... we'll see
                //
                //
                // we need to create the inverse so we can grab the camera position, cache the translation and then reset in inverse to origin
                invView = Matrix.InverseView(tmpView);
                cameraTranslation.x = invView.M41;
                cameraTranslation.y = invView.M42;
                cameraTranslation.z =  invView.M43;

                invView.M41 = 0;
                invView.M42 = 0;
                invView.M43 = 0;

                float det = 0;
                TV_3DMATRIX inv = Helpers.TVTypeConverter.ToTVMatrix(invView);
                TV_3DMATRIX view = default(TV_3DMATRIX);
                CoreClient._CoreClient.Maths.TVMatrixInverse(ref view, ref det, inv);
                tmpView = Helpers.TVTypeConverter.FromTVMatrix(view);
            }
            else
            {
                invView = mContext.Camera.InverseView;
                tmpView = mContext.Camera.View;
                cameraTranslation = mContext.Position;  
            }

            
            _currentRegionPVS = new RegionPVS(mContext, invView, tmpView, cameraTranslation);
            
            // NOTE: These translations never need to take into account scalng or rotation 
            // because that is done in the View transform we compute
            // TODO: so why not just include that camera space translation in the view matrix?
            // Well, that would add a very large value to the View matrix 

            // add to the total list of RegionSets whereas the stack version is for runtime tracking of
            // the current set
            mRegionSetCreatedHandler(_currentRegionPVS);
            _regionPVSStack.Push(_currentRegionPVS);
            

            // TODO: why do i even need a stack of offsets
            // a) you cant thread easily that way - unless i spawn multiple Culler.cs's that cover different branches
            // b) I can just maintain the state locally
            //      technically i dont even need to modify the camera, i just need to recreate the frustum using the new matrix
            //      so just as how i handle creating protal frustum and pushing that on the stack, i could do that.  

            // - portal frustum's should be created here not just regular full frustums if we've traveled to thsi region through
            //   a portal
            // how do i take the existing frustum and do something like create a new frustum in that region's space that also
            // utilizes that portal?  I guess the easiest way is to first create the new frustum, then create frustum planes
            // again for that portal based on the new reference frame and use that to continue culling.
            // so it's all more complicated than you think however the number of code lines is small and hopefully once it works
            // and we get picking working, we'll be done with it 
            // following we are testing this Region node's bounding box against the regular frustum we pushed on the stack. 
            // what we havent done is first perhaps use the large frustum and then cull only celestial.body children
            // we really do have to do two passes with both frustums because
            // 1) only the really large frustum will catch planets that are in sectors that would be in visible range
            // TODO: if we've entered this region through a portal, we should build the new frustum using the bounds of that portal
            // and not simply based on Near/Far/FOVRadians.  In other words, we should create a new PortalFrustum based on the current frustum (frustumstack.Peek())


             
            // the above code computes the frustum based on the Region and computes the InverseView used for that frustum so the 
            // Renderer can use it as well for the appropriate states.  Below we now traverse and test for visibility
            
            PushVisibilityTestResult(node.BoundingBox, 
					                false, 
					                mContext.ShowRegionBoundingBoxes, 
					                CONST_TV_COLORKEY.TV_COLORKEY_YELLOW);
            
            if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
            {
                mContext.Statistics.IncrementStat("Regions Visible");
                // what if instead of node.Children 
                // we had SceneNode[] childNodes = node.GetChildren(arg);
                // where we could also pass in an arg to help us determine
                // which children to filter.
                // But this goes against our normal filter process where
                // normally we get all children and then allow this traversr
                // to determine what to exclude.  Afterall, different traversers
                // have different goals.  That's why it seems to me
                // that somehow you'd need nested SceneNode's but that would be
                // breaking our pattern of one entity per scenenode... since those
                // virtual space scenenodes would not have any actual "room" or "floor" entities.
                // Hrm.  I really really do not want rooms and floor entities.  I want the space
                // for interiors contiguous.  So this means
                // we MUST find a way of making the space exist within the CelledRegionNode
                // One thing we could do is have CelledRegionVolumeNode
                // which can represent a floor or a room
                // and have these volume nodes managed solely by the CelledRegionNode
                // such taht Simulation still just does CelledRegionNode.AddChild(childSceneNode)
                // and doesn't worry about how that child is added internally so long as
                // the simulation can add/remove nodes in the same way.
                // Then during Traversal, we can either do normal node.Children
                // or for a CelledRegionNode, we can specifically choose to iterate through
                // Volumes.  This fits with our Traverser logic where Traverser's are custom and 
                // know how to handle various node types.
                // Here also, any entity can query the CelledRegionNode for spatial children in 
                // a particular room or volume.
                if (node.SpatialNodeRoot != null)
                {
                    // ZoneRoot's can never use Octree partitioning because it uses Zone partition children
                    // TODO: what about interior regions or even "field" regions?  Can they use octrees?  I suppose they can.  This way
                    // Field regions are useful for culling and picking in field local space and yet allowing the entire field
                    // to be moved as a single entity which is cheaper than moving all of entities within the field seperately.
                    Debug.Assert(node.Region is ZoneRoot == false);
                    Apply((OctreeOctant)node.SpatialNodeRoot, data);
                }
                else if (node.Children != null)
                {
                    foreach (SceneNode child in node.Children)
                    {
                        if (TraverseChildSceneNodes(child))
                            child.Traverse(this, data);
                    }
                }
                // note: below is commented out intentionally.  A node's region's child entities are not traversed directly for culling,
                // instead we traverse them through their SceneNode children
                //node.Region.Traverse(this);
            }

            PopVisibilityTestResult();

            // always pop region stack because entering a region node implies a new coordinate system which implies a region relative frustum
            _regionPVSStack.Pop();

            if (_regionPVSStack.Count == 0)
                _currentRegionPVS = null;
            else
                _currentRegionPVS = _regionPVSStack.Peek();

            return null;
        }
#endif

#if MULTITHREADED
        // only traversese SceneNodes (RegionNodes, EntityNodes, Octants, PortalNodes, CellSceneNodes)
        // even PortalNodes traverse through entity destination's EntityNode and not the Entity itself.
        private object TraverseChildSceneNodes(object data)
        {
            CullState state = (CullState)data;
            IntersectResult intersection = state.ParentIntersectResult;

            SceneNode sceneNode = null;

         
            if (state.SceneNode as SceneNode != null)
                sceneNode = (SceneNode)state.SceneNode;

#if DEBUG
            EntityNode tmp = state.SceneNode as EntityNode;
            if (tmp != null)
            {
                if (tmp.Entity.Name == "#Hud3DRoot")
                    System.Diagnostics.Debug.WriteLine("Hud3DRoot");
            }
#endif

            // if the parent Region was not completely outside, then we 
            // must test descendants
            if (intersection != IntersectResult.OUTSIDE)
            {
                if (sceneNode as RegionNode != null)
                {
                    RegionNode regionNode = (RegionNode)sceneNode;
                    if (!Include (regionNode.Region)) return null;
                    // if we only allow traversal to interiors through a portal, skip
                    // Eventually we shouldn't need a constant, it should be dynamic where
                    // if our camera is outside and it's a friendly ship and we're in edit mode
                    // or 3rd person camea with respect to our ship, we can see cutaway view
                    // like The Sims
                    // TODO: TraverseInteriorsAlways should be part of CullParameters struct
                    if (regionNode.Region.Parent is Container && mContext.TraverseInteriorsAlways == false) // child being a RegionNode does not necessarily mean it's an interior.  Zones are child of Root and are not interior
                        return null;

                    // cull test the current RegionNode
                    object result = regionNode.Traverse(this, state);
                    if (result == null) return null;

                    CullState resultState = (CullState)result;
                    if (resultState.ParentIntersectResult == IntersectResult.OUTSIDE) 
                    	return null;
                    
                    state.Context.Statistics.IncrementStat("Regions Visible");
                                        
                    // what if instead of node.Children 
                    // we had SceneNode[] childNodes = node.GetChildren(arg);
                    // where we could also pass in an arg to help us determine
                    // which children to filter.
                    // But this goes against our normal filter process where
                    // normally we get all children and then allow this traversr
                    // to determine what to exclude.  Afterall, different traversers
                    // have different goals.  That's why it seems to me
                    // that somehow you'd need nested SceneNode's but that would be
                    // breaking our pattern of one entity per scenenode... since those
                    // virtual space scenenodes would not have any actual "room" or "floor" entities.
                    // Hrm.  I really really do not want rooms and floor entities.  I want the space
                    // for interiors contiguous.  So this means
                    // we MUST find a way of making the space exist within the CelledRegionNode
                    // One thing we could do is have CelledRegionVolumeNode
                    // which can represent a floor or a room
                    // and have these volume nodes managed solely by the CelledRegionNode
                    // such taht Simulation still just does CelledRegionNode.AddChild(childSceneNode)
                    // and doesn't worry about how that child is added internally so long as
                    // the simulation can add/remove nodes in the same way.
                    // Then during Traversal, we can either do normal node.Children
                    // or for a CelledRegionNode, we can specifically choose to iterate through
                    // Volumes.  This fits with our Traverser logic where Traverser's are custom and 
                    // know how to handle various node types.
                    // Here also, any entity can query the CelledRegionNode for spatial children in 
                    // a particular room or volume.
                    if (regionNode.SpatialNodeRoot != null)
                    {
                        // ZoneRoot's can never use Octree partitioning because it uses Zone partition children
                        // TODO: what about interior regions or even "field" regions?  Can they use octrees?  I suppose they can.  This way
                        // Field regions are useful for culling and picking in field local space and yet allowing the entire field
                        // to be moved as a single entity which is cheaper than moving all of entities within the field seperately.
                        Debug.Assert(regionNode.Region is ZoneRoot == false);

                        CullState childState = resultState.Clone();
                        childState.SceneNode = regionNode.SpatialNodeRoot;
                        mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                    								  childState, CullCompletion);

                    }
                    else if (sceneNode.Children != null)
                    {
                    	SceneNode[] childSceneNodes = sceneNode.Children;
                        // enqueue them 
                        for (int i = 0; i < childSceneNodes.Length ; i++)
                        {
                            CullState childState = resultState.Clone();
                            childState.SceneNode = childSceneNodes[i];

                            mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                            							  childState, CullCompletion);
                        }
                    }
                    // note: below is commented out intentionally.  A node's region's child entities are not traversed directly for culling,
                    // instead we traverse them through their SceneNode children
                    //node.Region.Traverse(this);
                    
                }
                else if (state.SceneNode as QuadtreeCollection != null) // collection of quadtree interior decks
                {
                    QuadtreeCollection collection = (QuadtreeCollection)state.SceneNode;

                    // cull test the current QuadtreeCollection
                    CullState result = (CullState)collection.Traverse(this, data); //(CullState)Apply(collection, data);

                    if (result.ParentIntersectResult != IntersectResult.OUTSIDE)
                    {
                        if (collection.Children == null) return data;

                        // iterate through all Quadtree's in the collection and skip 
                        // those which are not visible.
                        Keystone.SpatialNodes.ISpatialNode[]  quadtrees = collection.Children;
                        for (int i = 0; i < quadtrees.Length; i++)
                        {
                            if (quadtrees[i] == null || quadtrees[i].Visible == false) continue; 
                            CullState childState = result.Clone();
                            childState.SceneNode = quadtrees[i];

                            mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                            							  childState, CullCompletion);
                        }
                    }
                }
                else if (state.SceneNode as Keystone.SpatialNodes.ISpatialNode != null)
                {
                    Keystone.SpatialNodes.ISpatialNode octant = (Keystone.SpatialNodes.ISpatialNode)state.SceneNode;

                    // cull test the current OctreeOctant
                    CullState result = (CullState)Apply(octant, data);

                    if (result.ParentIntersectResult != IntersectResult.OUTSIDE)
                    {
                        // cull the entityNodes first to take advantage of cache coherency of 
                        // bounding volume on stack
                        if (octant.EntityNodes != null)
                        {
                        	EntityNode[] octantEntityNodes = octant.EntityNodes;
                        	for (int i = 0; i < octantEntityNodes.Length; i++)
                            {
                                CullState childState = result.Clone();
                                childState.SceneNode = octantEntityNodes[i];

                                mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                            								  childState, CullCompletion);
                            }
                        }

                        if (octant.Children != null)
                        {
                        	Keystone.SpatialNodes.ISpatialNode[] octantChildren = octant.Children;
                            for (int i = 0; i < octantChildren.Length; i++ )
                                if (octantChildren[i] != null) // ok for some octants to be null if they have no EntityNodes in them
                                {
                                    CullState childState = result.Clone();
                                    childState.SceneNode = octantChildren[i];

                                    mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                                   		 						  childState, CullCompletion);
                                }
                        }
                    }
                }
                // NOTE: CellSceneNode is derived from EntityNode so this should result in traversal here without test sceneNode is CellSceneNode
                else if (sceneNode as EntityNode != null)
                {
                	if (!Include (((EntityNode)sceneNode).Entity)) 
                		return null;
                	
                    object ret = sceneNode.Traverse(this, data);
                    if (ret == null) return null; // Viewpoint node will result in a null CullState so check for it
                    CullState result = (CullState)ret;

                    
                                                
                    if (result.ParentIntersectResult != IntersectResult.OUTSIDE)
                    {
                        // for scenenodes and derived types, children are always other scene nodes 
                        SceneNode[] childSceneNodes = ((EntityNode)sceneNode).Children;
                        if (childSceneNodes != null)
                            for (int i = 0; i < childSceneNodes.Length; i++)
                            {
                                // in TraverseChildSceneNodes we determine if portals or isometric
                                // view of interiors is enabled
                                // if allowed to traverse these children
                                CullState childState = result.Clone();
                                childState.SceneNode = childSceneNodes[i];

                                mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                                							  childState, CullCompletion);
                            }
                    }
                }
                else if (sceneNode as PortalNode != null)
                {
                	if (!Include (((PortalNode)sceneNode).Entity)) return null;
                	
                    object ret = sceneNode.Traverse(this, data);
                    if (ret == null) return null; // Viewpoint node will result in a null CullState so check for it
                    CullState result = (CullState)ret;


                    if (result.ParentIntersectResult != IntersectResult.OUTSIDE)
                    {
                        mContext.Statistics.IncrementStat("Portals Traversed");

                        Portal p = (Portal)((PortalNode)sceneNode).Entity;

                        // TODO: below is broken in the sense that the portal's frustum is not
                        // taken into account when entering a new region.  Since entering a new
                        // region results in a new region relative frustum being created.  That is 
                        // fine however that region if there through a portal should instead
                        // create two FrustumInfo's that are constrained by the portal's planes.

                        // note: Our viewfrustum is created at origin and so is our portal frustum
                        Vector3d origin = Vector3d.Zero();

                		// PortalFrustum pf = new PortalFrustum(origin, cameraSpaceCoords);
                        // _frustumStack.Push(pf);
                        // TODO: here two we need multiple portal frustums, one for each projection?

                        // during cull we actually want to start traversing the Destination's owning SceneNode so p.Destination.SceneNode if it's not null
                        // we don't have to worry about recursing back because our portals are one way _and_ we dont traverse portals we can't see

                        // only traverse thru portals if isometric view is OFF
                        // TODO: here should use mCullParameters
                        if (mContext.TraverseInteriorsAlways == false && mContext.TraverseInteriorsThroughPortals == true)
                        {
                            // if allowed to traverse these children
                            CullState childState = result.Clone();
                            childState.SceneNode = p.Destination.SceneNode;

                            mWorkItemsGroup.QueueWorkItem(TraverseChildSceneNodes,
                            							  childState, CullCompletion);
                        }
                        //         _frustumStack.Pop();
                    }
                }
            }

            return data;
        }
#else
            // TODO: MPJ June.22.2011 - This may be obsolete as now I no longer attach the RegionNode's of Region's
            // that are added as children to Containers to their parent Container's SceneNode.  So the only
            // way to traverse them at all is through portals.
            // So this does mean to show the top down view then need to toggle disabling of portals and
            // attaching those nodes.  That altogether I think is a more sane way of doing it in any case.
        private bool TraverseChildSceneNodes(SceneNode sceneNode)
        {

            // depending on settings in mContext, traverse interior nodes through portals or
            // as any other children or not at all.
            // TODO: here should use mCullParameters
            if (sceneNode is PortalNode && (mContext.TraverseInteriorsThroughPortals || mContext.TraverseInteriorsAlways))
                return false;
            else if (sceneNode is RegionNode && ((RegionNode)sceneNode).Region.Parent is Container) // child being a RegionNode does not necessarily mean it's an interior.  Zones are child of Root and are not interior
            {
                // if we only allow traversal to interiors through a portal, skip
                // Eventually we shouldn't need a constant, it should be dynamic where
                // if our camera is outside and it's a friendly ship and we're in edit mode
                // or 3rd person camea with respect to our ship, we can see cutaway view
                // like The Sims
                return mContext.TraverseInteriorsAlways;
            }
            else
            {
                if (sceneNode is EntityNode)
                {
                    if (((EntityNode)sceneNode).Entity.CellIndex >= 0)
                    {
                        // TODO: are we skipping culling of floors when in interior view mode
                        // that are not visible (i.e are not the floor we are currently looking at?)
                        //
                        // TODO: if this cell index is on a floor that is being hidden
                        // skip it.
                        //
                        // instead here, when traversing the CelledRegionNode
                        // for the purposes of both collision and culling 
                        // I think we can 
                    }
                }
                return true;
            }
        }
#endif

#if MULTITHREADED
        public object Apply(SpatialNodes.ISpatialNode spatialNode, object data)
        {
            if (spatialNode is OctreeOctant)
                return Apply((OctreeOctant)spatialNode, data);
            else if (spatialNode is QuadtreeQuadrant)
                return Apply((QuadtreeQuadrant)spatialNode, data);
            else throw new ArgumentOutOfRangeException();
        }

        // TODO: in a large stellar region, many otants will be too far away.. particularly 
        // those who's sizes are below the visible from very far away threshild.  Thus octants
        // of increasing size will have greater visibility distances...
        public object Apply(OctreeOctant octant, object data)
        {
            CullState state = (CullState)data;

            RegionPVS current = state.PVS;

            // for bounding box, we wish to test a loose box which we compute on the fly
            Vector3d radius = (octant.BoundingBox.Max - octant.BoundingBox.Min) / 2d;
            BoundingBox looseBox = new BoundingBox(octant.BoundingBox.Min - radius, octant.BoundingBox.Max + radius);

            IntersectResult intersection = PushVisibilityTestResult(ref state, looseBox,
                                                                    false,
													                false,
													                state.Context.ShowOctreeBoundingBoxes,
													                CONST_TV_COLORKEY.TV_COLORKEY_WHITE);


            state.Context.Statistics.IncrementStat("Octree Nodes Visited");
            state.ParentIntersectResult = intersection;
            return state;
        }

        public object Apply(QuadtreeQuadrant quadrant, object data)
        {
            CullState state = (CullState)data;

            // Visibility Test against 2d QuadtreeQuadrant will create a 3d BoundingBox where the min.y
            // of that box = quadrantBoundingRect.y and the max.y = min.y + mSize.y;
            // TODO: actually i should do quadtreequadrant visibility using just a 2d test
            // 
            RegionPVS current = state.PVS;

            
            BoundingBox box = BoundingBox.FromBoundingRect (quadrant.BoundingRect); 


            // TODO: temp MaxVisibleDistance var
            //double tmp = 100000;
            //throw new Exception("Not implemented max visible for octreenodes");

            // for bounding box, we wish to test a loose box which we compute on the fly
            Vector3d radius = (box.Max - box.Min) / 2d;
            BoundingBox looseBox = new BoundingBox(box.Min - radius, box.Max + radius);

            IntersectResult intersection = PushVisibilityTestResult(ref state, looseBox,
                                                                    false,
													                false,
													                state.Context.ShowOctreeBoundingBoxes,
													                CONST_TV_COLORKEY.TV_COLORKEY_WHITE);


            state.Context.Statistics.IncrementStat("Octree Nodes Visited");
            state.ParentIntersectResult = intersection;
            return state;
        }

#else
        // TODO: in a large stellar region, many otants will be too far away.. particularly 
        // those who's sizes are below the visible from very far away threshild.  Thus octants
        // of increasing size will have greater visibility distances...
        public object Apply(OctreeOctant octant, object data)
        {
            RegionPVS current = _regionPVSStack.Peek();

            // TODO: temp MaxVisibleDistance var
            //double tmp = 100000;
            //throw new Exception("Not implemented max visible for octreenodes");
            // for bounding box, we wish to test a loose box which we compute on the fly
            Vector3d radius = (octant.BoundingBox.Max - octant.BoundingBox.Min) / 2d;
            BoundingBox looseBox = new BoundingBox(octant.BoundingBox.Min - radius, octant.BoundingBox.Max + radius);
            
            PushVisibilityTestResult(looseBox, 
					                false, 
					                mContext.ShowOctreeBoundingBoxes, 
					                CONST_TV_COLORKEY.TV_COLORKEY_WHITE);
            
            if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
            {
                mContext.Statistics.IncrementStat("Octree Nodes Visited");

                // cull the entityNodes first to take advantage of cache coherency of 
                // bounding volume on stack
                if (octant.EntityNodes != null)
                    foreach (SceneNode sn in octant.EntityNodes)
                        sn.Traverse(this, data);


                if (octant.Children != null)
                    foreach (OctreeOctant node in octant.Children)
                        if (node != null) // ok for some octants to be null if they have no EntityNodes in them
                            node.Traverse(this, data);

            }
            PopVisibilityTestResult();
            return data;
        }

        public object Apply(QuadtreeQuadrant quadrant, object data)
        {
            RegionPVS current = _regionPVSStack.Peek();
            BoundingBox box = BoundingBox.FromBoundingRect(quadrant.BoundingRect);

            // Visibility Test against 2d QuadtreeQuadrant will create a 3d BoundingBox where the min.y
            // of that box = quadrantBoundingRect.y and the max.y = min.y + mSize.y;
            // TODO: actually i should do quadtreequadrant visibility using just a 2d test
            // 


            // TODO: temp MaxVisibleDistance var
            //double tmp = 100000;
            //throw new Exception("Not implemented max visible for octreenodes");
            double maxVisibleDistance = double.MaxValue; // float.MaxValue;
            // for bounding box, we wish to test a loose box which we compute on the fly
            Vector3d radius = (box.Max - box.Min) / 2d;
            BoundingBox looseBox = new BoundingBox(box.Min - radius, box.Max + radius);

            PushVisibilityTestResult(looseBox,
					                false,
					                 mContext.ShowOctreeBoundingBoxes, 
					                CONST_TV_COLORKEY.TV_COLORKEY_WHITE);


            if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
            {
                mContext.Statistics.IncrementStat("Quadtree Nodes Visited");

                // cull the entityNodes first to take advantage of cache coherency of 
                // bounding volume on stack
                if (quadrant.EntityNodes != null)
                    foreach (SceneNode sn in quadrant.EntityNodes)
                        sn.Traverse(this, data);


                if (quadrant.Children != null)
                    foreach (OctreeOctant node in quadrant.Children)
                        if (node != null) // ok for some octants to be null if they have no EntityNodes in them
                            node.Traverse(this, data);

            }
            PopVisibilityTestResult();
            return data;

        }
#endif

#if MULTITHREADED
        public object Apply(CelledRegionNode node, object data)
        {
            // mContext.Floor 
            //  - the best place to cull them is probably in the boundingbox test
            //    but for CellSceneNode's we test the floor first and not the box for quick out
            //
            // TODO: cull out any child nodes on a Floor that is not current focus?
            object ret = Apply((RegionNode)node, data); // for now just using RegionNode is ok
            if (ret == null) return null;


            // use the result from RegionNode apply, not the data passed in because we must have
            // new state with updated regionPVS
            CullState state = (CullState)ret;


            // determine the cameraspace position so we can store it in the VisibleItemInfo
            RegionPVS current = state.PVS;
            state.CameraSpacePosition = node.Position - current.RegionRelativeCameraPos; // TODO: dec.5.2012 switched to node.Position from node.BoundingBox.Center
            double distanceToCameraSquared = Vector3d.GetLengthSquared(state.CameraSpacePosition);

            // TODO: here it seems we never cull celledregion nodes bbox!

            
            Model[] results = ((Interior) node.Region).SelectModel(SelectionMode.Render, distanceToCameraSquared);
            VisibleModelsFound (state, node.Region, results); 

            // NOTE: Sub entities are already attached via sceneNodes and thus are culled from there and not by traversing entity children here

            return state;
        }
#else
        public object Apply(CelledRegionNode node, object data)
        {
            // mContext.Floor 
            //  - the best place to cull them is probably in the boundingbox test
            //    but for CellSceneNode's we test the floor first and not the box for quick out
            //
            // TODO: cull out any child nodes on deck that is not current focus?
                        
            return Apply((RegionNode)node, null); // for now just using RegionNode is ok

            //bool drawBounds = false;
            //bool selected = false;

            //PushVisibilityTestResult(node.BoundingBox);
            //if (_visibilityResult.Peek() != IntersectResult.OUTSIDE)
            //{
            //    _sectorsVisible++;
            //    if (node.Children != null)
            //        foreach (SceneNode child in node.Children)
            //            child.Traverse(this);
            //}

            //PopVisibilityTestResult();
            return data;
        }
#endif

        public object Apply(CellSceneNode node, object state)
        {
            // for now just use EntityNode
            return Apply((EntityNode)node, state); 
            // entity translations are relative so to get the proper center we just use
            // the bbox center since that is always in regional.  However what i could do
            // is change it to Entity returns local (but what's the point of that?) and then add
            // a seperate BBox to SceneNode that is not hierarchical but only has it's Entity's box
        }

#if MULTITHREADED
        public object Apply(PortalNode node, object data)
        {
            CullState state = (CullState)data;
            Portal p = (Portal)node.Entity;
            if (p.Destination != null && p.Destination.SceneNode != null)
            {
                RegionPVS current = state.PVS;

                Vector3d[] cameraSpaceCoords;
                
                // TODO: is pushvisibilityTestResult not useable when traversing a portal?
                // if the portal is fully contained in the frustum, does that mean
                // so are things on the other side?  absolutely NOT!  I think logically we would
                // be doing a fresh sub-cull starting with a new sub-frustum and new visibility stack 
                IntersectResult intersection = PushVisibilityTestResult(ref state, p.CoordinatesRegionSpace, 
                                                                        false,
													                    p == mContext.Workspace.SelectedEntity.Entity,
                    													mContext.ShowPortals, out cameraSpaceCoords);

                state.ParentIntersectResult = intersection;

                if (intersection != IntersectResult.OUTSIDE)
                {
                	#if DEBUG
                    mContext.Statistics.IncrementStat("Portals Visible");
                    #endif
                    Vector3d dir = cameraSpaceCoords[0];
                    Vector3d normal = Vector3d.Normalize(Vector3d.CrossProduct(cameraSpaceCoords[0] - cameraSpaceCoords[1], cameraSpaceCoords[1] - cameraSpaceCoords[2]));

                    // skip this frustum traversal if it's back facing - our portals are only one way.
                    // This is required to avoid endless recursion back and through portals over and over
                    if (Vector3d.DotProduct(dir, normal) < 0)
                    {
                        // change the intersection to OUTSIDE to avoid traversing
                        intersection = IntersectResult.OUTSIDE;
                    }
                }
                                
                return state;
            }
            return data;
        }
#else
        public object Apply(PortalNode node, object data)
        {
            Portal p = (Portal)node.Entity;
            if (p.Destination != null)
            {
                RegionPVS current = _regionPVSStack.Peek();
                Vector3d[] cameraSpaceCoords;
                // TODO: is pushvisibilityTestResult not useable when traversing a portal?
                // if the portal is fully contained in the frustum, does that mean
                // so are things on the other side?  absolutely NOT!  I think logically we would
                // be doing a fresh sub-cull starting with a new sub-frustum and new visibility stack 
                PushVisibilityTestResult(p.CoordinatesRegionSpace, 
					                    p == mContext.Scene.Selected,
                    					mContext.ShowPortals, out cameraSpaceCoords);
                
                if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
                {
                	#if DEBUG
                    mContext.Statistics.IncrementStat("Portals Visible");
                    #endif
                    Vector3d dir = cameraSpaceCoords[0];
                    Vector3d normal = Vector3d.Normalize(Vector3d.CrossProduct(cameraSpaceCoords[0] - cameraSpaceCoords[1], cameraSpaceCoords[1] - cameraSpaceCoords[2]));

                    // skip this frustum traversal if it's back facing - our portals are only one way.
                    // This is required to avoid endless recursion back and through portals over and over
                    if (Vector3d.DotProduct(dir, normal) > 0)
                    {
                       node.Entity.Traverse(this, data);
                    }
                }
                PopVisibilityTestResult();
            }
            return data;
        }
#endif

#if MULTITHREADED
        public object Apply(GUINode node, object data)
        {
            CullState state = (CullState)data;

            state.ParentIntersectResult = IntersectResult.INSIDE;
            data = node.Entity.Traverse(this, state);
            return data;
        }

        public object Apply(EntityNode sceneNode, object data)
        {
            //if (node.Entity.ID == "axis_move_tool")
            //   System.Diagnostics.Debug.WriteLine("HUD element found");
            //if (node.Entity is EntityProxy)
            //{
            //    EntityProxy proxy = (EntityProxy)node.Entity;
            //    if (proxy.ReferencedEntity.Name == "Io")
            //        System.Diagnostics.Debug.WriteLine("Proxy moon found");
            //}
            CullState state = (CullState)data;


            // NOTE: Here the entity's node bounding box is a hierarchical bounding box
            // such that a Stellar System's SceneNode bounding box is the bounding volume of all children and sub-children
            // A star system's SceneNode bounding box is the bounding volume of all child planets and moons and asteroids
            // a Planet's SceneNode bounding box is the bounding volume of all child entities
            // However the entity itself never takes it's own children into account.
            RegionPVS current = state.PVS;

            
            // TODO: with our interior producing a scenenode that now connects to the Vehicle's
            // sceneNode, that then creates a combined hierarchical bounding box for this Vehicle's
            // EntityNode that is using the coordinate system of the interior in it's own local space
            // That's undeseriable as far as how the bounding box is computed, but yet we do want
            // the traversal option for when isometric view is active.
            // And on the other hand, with isometric view active, we want the VEHICLES scene node
            // to include it but only in the Vehicle's region's coordinate system... we want to keep
            // the interior's own scenenode however in it's own interior region coord system.  So i think
            // this is strictly a matter of how we compute the bounding box?  
            // Probably the best answer is we _never_ include the interior bbox in the Vehicle's sceneNode
            // and we should _only_ ever go by the Vehicle's own exterior mesh.  This would solve every problem.
            // So all we need is an exterior mesh and then to exclude any interior region from being used
            // to compute an entityNode's bbox.
            // TODO: if im too far from the star, will this ruin our ability to see any child planets?
            bool selected =  state.Context.Workspace.SelectedEntity != null && sceneNode.Entity == state.Context.Workspace.SelectedEntity.Entity;
            
            bool useLargeFrustum = false;
            if (sceneNode.Entity as ModeledEntity != null)
            	useLargeFrustum = ((ModeledEntity)sceneNode.Entity).UseLargeFrustum;

            //if (node.Entity.SRC != null && node.Entity.SRC.Contains("chair"))
            //    Debug.WriteLine("chair");

            BoundingBox bb = sceneNode.BoundingBox;
            bool traverseEntity = true;

            if (sceneNode.Entity is DefaultEntity)
            {
                // DefaultEntity's do not contain ModelSelectors or Models or Geometry, but when in Edit mode we want to be able for the Hud to
                // generate a clickable proxy icon.  For this to happen, we must traverse the DefaultEntity where we can call
                // VisibleModelsFound() so that the Hud will be able to find the visible item and generate a pickable Icon for it.
                traverseEntity = Core._Core.Scene_Mode == Core.SceneMode.EditScene;
                // NOTE: child Entities are always traversed via EntityNode hierarchy not Entity hierarchy.
                //       So it ok to skip traversing the actual Entity 
            }


            IntersectResult intersection = PushVisibilityTestResult(ref state,
                                                                    bb,
                                                                    useLargeFrustum,
                                                                    selected,
						                							mContext.ShowEntityBoundingBoxes, 
						                							CONST_TV_COLORKEY.TV_COLORKEY_GREEN);
            state.ParentIntersectResult = intersection;
//            if (node.Entity is Celestial.Star)
//            {
//            	int abc = 0;
//            	
//            }
//            if (node.Entity is Celestial.StellarSystem)
//            {
//            	int abcd = 0;
//            	
//            }
               
                            	
            // TODO: 
            // i think en.Entity.Traverse should instead of having Traversse(this) should use
            // a custom private method so we can pass the cameraPosition and distanceToCameraSq 
            // that we've already calculated here rather than do it again in Apply (Entity)
            // 
            // NOTE: for planet atmosphere it does matter the order here since the child Model which is the planet sphere must 
            // render before the child entities clouds and then atmosphere last.  So keep this in mind if i should later have to move thigns around
            if (state.ParentIntersectResult != IntersectResult.OUTSIDE && traverseEntity)
            {
                //if (node.Entity is Keystone.Lights.PointLight)
                //    System.Diagnostics.Debug.WriteLine("Light index = " + ((Keystone.Lights.PointLight)node.Entity).TVIndex.ToString());

                // state.CameraSpacePosition = node.Position - current.RegionRelativeCameraPos;
                state.CameraSpacePosition = sceneNode.Entity.DerivedTranslation - current.RegionRelativeCameraPos;

                data = sceneNode.Entity.Traverse(this, state);
            }
            return data;
        }
#else
        public object Apply(GUINode node, object data)
        {
            if (node.Children != null)
                foreach (SceneNode child in node.Children)
                    // in TraverseChildSceneNodes we determine if portals or isometric
                    // view of interiors is enabled
                    if (TraverseChildSceneNodes (child))
                        child.Traverse(this, data);

            return null;
        }

        public object Apply(EntityNode node, object data)
        {
            // NOTE: Here the entity's node bounding box is a hierarchical bounding box
            // such that a Stellar System's SceneNode bounding box is the bounding volume of all children and sub-children
            // A star system's SceneNode bounding box is the bounding volume of all child planets and moons and asteroids
            // a Planet's SceneNode bounding box is the bounding volume of all child entities
            // However the entity itself never takes it's own children into account.
            RegionPVS current = _regionPVSStack.Peek();

            // TODO: with our interior producing a scenenode that now connects to the Vehicle's
            // sceneNode, that then creates a combined hierarchical bounding box for this Vehicle's
            // EntityNode that is using the coordinate system of the interior in it's own local space
            // That's undeseriable as far as how the bounding box is computed, but yet we do want
            // the traversal option for when isometric view is active.
            // And on the other hand, with isometric view active, we want the VEHICLES scene node
            // to include it but only in the Vehicle's region's coordinate system... we want to keep
            // the interior's own scenenode however in it's own interior region coord system.  So i think
            // this is strictly a matter of how we compute the bounding box?  
            // Probably the best answer is we _never_ include the interior bbox in the Vehicle's sceneNode
            // and we should _only_ ever go by the Vehicle's own exterior mesh.  This would solve every problem.
            // So all we need is an exterior mesh and then to exclude any interior region from being used
            // to compute an entityNode's bbox.
            PushVisibilityTestResult(node.BoundingBox,
					                node.Entity == mContext.Scene.Selected, 
					                mContext.ShowEntityBoundingBoxes, CONST_TV_COLORKEY.TV_COLORKEY_GREEN);

            if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
            {
                // NOTE: for planet atmosphere it does matter the order here since the child Model which is the planet sphere must 
                // render before the child entities clouds and then atmosphere last.  So kee this in mind if i shoudl later have tomove thigns around
                // the easiest generic fix would be to not add the earth as a model but as another child entity to World then simply control
                // the order those children are added to their shared parent.
                node.Entity.Traverse(this, data);

                // for scenenodes and derived types, children are always other scene nodes 
                if (node.Children != null)
                    foreach (SceneNode child in node.Children)
                        // in TraverseChildSceneNodes we determine if portals or isometric
                        // view of interiors is enabled
                        if (TraverseChildSceneNodes (child))
                            child.Traverse(this, data);
            }
            PopVisibilityTestResult();
            return null;
        }
#endif


        public object Apply(Node node, object data)
        {
        	if (node is Viewpoint)
        		return null;
        	
            throw new NotImplementedException("ScaleCuller.Apply(Node node)");
        
            return data;
        }
        
        public object Apply (Entity entity, object data)
        {
        	// non modeled entities such as Viewpoints
        	// NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
        	if (!Allow (entity)) return data;
        	
            return data;
        }
        
        public object Apply(Portal p, object data)
        {
            throw new NotImplementedException();
   //         mContext.Statistics.IncrementStat("Portals Traversed");
   //         // TODO: below is broken in the sense that the portal's frustum is not
   //         // taken into account when entering a new region.  Since entering a new
   //         // region results in a new region relative frustum being created.  That is 
   //         // fine however that region if there through a portal should instead
   //         // create two FrustumInfo's that are constrained by the portal's planes.

   //         // note: Our viewfrustum is created at origin and so is our portal frustum
   //         Vector3d origin = Vector3d.Origin();

   ////         PortalFrustum pf = new PortalFrustum(origin, cameraSpaceCoords);
   ////         _frustumStack.Push(pf);
   //         // TODO: here two we need multiple portal frustums, one for each projection?

   //         // during cull we actually want to start traversing the Destination's owning SceneNode so p.Destination.SceneNode if it's not null
   //         // we don't have to worry about recursing back because our portals are one way _and_ we dont traverse portals we can't see
   //         if ((p.Destination != null) && (p.Destination.SceneNode != null))
   //             // only traverse thru portals if isometric view is OFF
   //             if (mContext.TraverseInteriorsAlways == false && mContext.TraverseInteriorsThroughPortals == true)
   //                 p.Destination.SceneNode.Traverse(this, ref data);

   //    //         _frustumStack.Pop();
            
			return data;
        }
        
        public object Apply(Region region, object data)
        {
            throw new Exception("ScaleCuller.Apply() - We should not be using Regions, Entities, Models or end Geometry for culling.  Only SceneNodes like OctreeNodes, EntityNodes, RegionNodes and CellSceneNodes");
        }

        public object Apply(Interior interior, object data)
        {
            throw new NotImplementedException();

            //if (interior.mOctree.BoundingBox == null) return data;

            //Vector3d halfOctreeSize = interior.mOctree.Size / 2.0d;
            //int maxLevel = 5; // (int)Math.Log(interior.mStaticOctree.Size, 2) - SampleState.OctreeTraversalCoarseness;
            //                  //            frustumCulledInstances = 0;

            ////            TODO: i think the flagging is done for the occlussion step to follow. The flags don't need to
            ////                  persist from frame to frame, only within the same frame between cull and occlussion steps
            ////            if (!onlyFlagNodes)
            ////                foreach (var mesh in meshes.Values)
            ////                    mesh.ResetBatch();

            //CullState state = (CullState)data;

            //UpdateMinimeshes(interior, interior.mOctree.Root);

            //return Apply((Region)interior, data);
        }

        public object Apply(TileMap.Structure structure, object data)
        {
            return Apply((ModeledEntity)structure, data);
        }

        public object Apply(TileMap.StructureVoxels structure, object data)
        {        	
        	if (structure.mOctree.BoundingBox == null) return data;
        	
            Vector3d halfOctreeSize = structure.mOctree.Size / 2.0d;
            int maxLevel = 5; // (int)Math.Log(structure.mStaticOctree.Size, 2) - SampleState.OctreeTraversalCoarseness;
//            frustumCulledInstances = 0;

//            TODO: i think the flagging is done for the occlussion step to follow. The flags don't need to
//                  persist from frame to frame, only within the same frame between cull and occlussion steps
//            if (!onlyFlagNodes)
//                foreach (var mesh in meshes.Values)
//                    mesh.ResetBatch();

			CullState state = (CullState)data;
			structure.ClearAllInstances ();
            CullOCtreeOctant(state.PVS, structure, structure.mOctree.Root, 0, maxLevel, structure.mOctree.BoundingBox.Center, halfOctreeSize);

// TEMP COMMENT            UpdateMinimeshes(structure, structure.mOctree.Root);

        	return Apply ((ModeledEntity)structure, data);
        }

        // TODO: after culling and occlusion have set the "FrustumContainment" 
        //       we then need to have the PartitionedModel.cs arrays set for position, rotation,scale, enable
        //       if we recurse the structure's octants, finding all "inside" or "intersect" and then
        //       for those, we now grab the segmentIndices and model indices to get a reference to the Model, and then we add an instance  
        //       to the PartitionedModel since it is sharing MinimeshGeometry
        //        - it would be nice if maybe we could do all the ones of same Segment/Model at the same time...
        //        but i think to do that, we'd need to sort the visibility results so that we dont have to iterate the tree against
        //        but instead can just iterate a list of results
        //       
//			// TODO: some models like MinimeshModel may have it's own static octree structure internally that 
//			// will allow us to cull elements of the selected model.  Since the octree is part of the model
//			// it is static.  Since it's part of the model, it allows us to use ModelSelector's to have different
//			// LOD versions that might not have an octree structure at all.
//			//  - the trick however is that the VisibleItemInfo for these types of MinimeshModels is viewport/renderingContext specific
//			// and so cannot reside in the Entity or Model.  But that info which must contains enableArray 
//			if (mCullParameters.HardwareOcclussionEnabled)
//			{
//				// TODO: with threaded culling, im not sure how we do occlussion queries without effectively blocking and limiting usefulness of threaded
//				// culling. Or perhaps if culling is limited to our static structure octrees, then perhaps we can serialize them before then
//				// calling mItemFoundDelegate() 
//		//		Camera occlussionCamera;
//		//		occlussionCamera.Projection = current.FrustumInfo[0].Projection;
//		//		occlussionCamera.InverseView = current.InverseView;
//		//		mOcclusionSurface.SetCamera (occlussionCamera.TVCamera);
//				
//				// TODO: wait a second, the above i think is better off being done in
//				// context.mDrawer using the mRegionSets formed here as mItemFoundDelegate occurs
//				// however what we should be doing here is octree culling the elements themselves.. forget occlusion for now!
//				// so - if we've found a structure, then we should traverse it's octree to find visible tiles, and then create "inFrustum" arrays
//				// for the relevant minimeshes
//			}
//
//			            if (model is PartionedModel)
//            {
//            	PartionedModel pmodel = (PartionedModel)model;
//            	// TODO: actually, this model is not quite what we must cull because
//            	//       we are culling an octree that will have TileInfo that represents multiple models.
//            	//       And for each model, we'll want to store the position/rotation/enable array based on the cull results.
//            	//       So... besides actually building the octree, the only real question is, when we cull traverse the octree
//            	//       how do we accumulate and assign the position/rotation/enable arrays to the corrrect models.  
//            	//       AddInstance() / ChangeInstance() as we go... but rather than assigning to Minimesh, it must assign to the PartitionedModel.
//            	//       One problem here is, the "minimeshElement" index we store in the maplayer... how do we maintain that?  Or is it 
//            	//       OctreeIndex we care about more since our minimesh would now be rendered frame 2 frame immediate, not retained.
//            	//       This could simplify / speed up autotiling - the real question is of course performance.  We want to be able to render these mini's faster with less overdraw
//            	//       AND with less memory AND faster zone paging.
//            	//       - can we do a non-recursive cull 
//            	pmodel.Cull(current, current.RegionRelativeCameraPos);
//            }
//			            
        
        void CullOCtreeOctant(RegionPVS current, TileMap.StructureVoxels structure, StaticOctant<Keystone.TileMap.TileInfo> octant, int level, int maxLevel, Vector3d center, Vector3d halfSize)
        {
            BoundingBox box = new BoundingBox(center - halfSize, center + halfSize);
                        
            BoundingBox cameraSpaceBox = GetCameraSpaceBox (box, current.RegionRelativeCameraPos);
            double distanceToCameraSq = Vector3d.GetLengthSquared (cameraSpaceBox.Center);
            
            IntersectResult intersectResult;

            using (CoreClient._CoreClient.Profiler.HookUp("IntersectTest"))
                intersectResult = current.IntersectionTest(cameraSpaceBox, distanceToCameraSq);
            
    
            if (intersectResult == IntersectResult.OUTSIDE)
            {
            	// NO NEED TO CULL THIS BRANCH ANY FURTHER, ALL DESCENDANT NODES ARE OUTSIDE THE FRUSTUM
            	SetIntersectResultRecursively(octant, level, maxLevel, intersectResult);
                return;
            }

            
            if ((level == maxLevel || octant.Children == null)  || intersectResult == IntersectResult.INSIDE) // && octant.Filled))
            {
            	// NO NEED TO CULL THIS BRANCH ANY FURTHER, ALL DESCENDANT NODES ARE INSIDE THE FRUSTUM
                SetIntersectResultRecursively(octant, level, maxLevel, intersectResult);
//                if (!onlyFlagNodes)
                AddTiles(structure, octant, current.RegionRelativeCameraPos);
				return;
            }

            octant.FrustumContainment = intersectResult;
        	

            halfSize /= 2d;
            for (int i = 0; i < 8; i++)
            {
                var childNode = octant.Children[i];
                if (childNode != null)
                	// recurse
                	CullOCtreeOctant(current, structure, childNode, level + 1, maxLevel, center + halfSize * StaticOctree<Keystone.TileMap.TileInfo>.BoundsOffsetTable[i], halfSize);
            }
        }
        
        void SetIntersectResultRecursively(StaticOctant<Keystone.TileMap.TileInfo> octant, int level, int maxLevel, IntersectResult intersectResult)
        {
            if (intersectResult == IntersectResult.OUTSIDE)    
                 mContext.Statistics.IncrementStat("Octant's Culled");

            octant.FrustumContainment = intersectResult;

            if (level == maxLevel || octant.Children == null) return;
            foreach (var childNode in octant.Children)
                if (childNode != null)
                    SetIntersectResultRecursively(childNode, level + 1, maxLevel, intersectResult);
        }
          
        void AddTiles (TileMap.StructureVoxels structure, StaticOctant<Keystone.TileMap.TileInfo> octant, Vector3d regionRelativeCameraPosition)
        {
        	// TODO: for some reason the following test for IntersectResult.OUTSIDE causes problems
        	//       when Zone resolution is anything other than 32 (eg culling issues on 64)
        	//if (octant.FrustumContainment == IntersectResult.OUTSIDE)    
            //     return;

            if (octant.IsLeaf)
            {
            	Keystone.TileMap.TileInfo tileInfo = octant.Data;
            	// TODO: supply a LOD index?
            	Model model = TileMap.StructureVoxels.GetModel (structure, tileInfo.SegmentID, tileInfo.ModelID);
            	#if DEBUG
            	if (tileInfo.SegmentID == 2)
            		//System.Diagnostics.Debug.WriteLine ("ScaleCuller.AddTiles() - " + tileInfo.SegmentID.ToString());
            	#endif
            	if (model == null) 
            		return;

            	// TODO: tileInfo.AtlasTextureIndex - can we use combination of SegmentID and ModelID to compute
            	//       the atlas texture Row, Column index? Or perhaps the AutoTile can produce that for us
            	//       just as it computes a Rotation for us
            	//       But for now, let's just get the texture itself rendering with index = 0 for all instances
            	model.AddInstance (tileInfo.Position, new Vector3d(0, tileInfo.RotationDegrees, 0)); // tileInfo.Scale;
            }
            else 
            {
	            for (int i = 0; i < 8; i++)
	            {
	            	var child = octant.Children[i];
	                if (child != null)
	                {
	                	// recurse
	                	AddTiles (structure, child, regionRelativeCameraPosition);
	                }
	            }
            }
        }
        
        void UpdateMinimeshes(TileMap.StructureVoxels structure, StaticOctant<Keystone.TileMap.TileInfo> octant)
        {
            if (octant.FrustumContainment == IntersectResult.OUTSIDE)    
                 return;

            if (octant.IsLeaf)
            {
            	Keystone.TileMap.TileInfo tileInfo = octant.Data;
            	Model model = TileMap.StructureVoxels.GetModel (structure, tileInfo.SegmentID, tileInfo.ModelID);
            	if (model == null) 
            		return;

            	model.AddInstance (tileInfo.Position, new Vector3d(0, tileInfo.RotationDegrees, 0)); // tileInfo.Scale;
            	return;
            }
                       
            foreach (var childNode in octant.Children)
                if (childNode != null)
                    UpdateMinimeshes(structure, childNode);
        }        
#if MULTITHREADED
        public object Apply(Controls.Control2D control, object data)
        {
            CullState state = (CullState)data;

            Model[] results = control.SelectModel(SelectionMode.Render, 0);
            state.CameraSpacePosition = control.Translation;
            VisibleModelsFound (state, control, results); 

            return state;
        }

        public object Apply(DefaultEntity entity, object data)
        {
            if (!Allow(entity)) return data;
            CullState state = (CullState)data;

            // NOTE: if this Apply() gets called, it means we are in EditScene mode and all we need to do is
            // apply any scaling for the Hud iconization
            
            RegionPVS current = state.PVS;
            BoundingBox cameraSpaceBBox = GetCameraSpaceBox(entity.SceneNode.BoundingBox, current.RegionRelativeCameraPos);
            Vector3d cameraSpacePosition = entity.Translation - current.RegionRelativeCameraPos;
            // invoke handler in RenderingContext.cs 
            mItemFoundDelegate(current, entity, null, cameraSpacePosition, cameraSpaceBBox);

            return state;
        }

        public object Apply(ModeledEntity entity, object data)
        {
            //if (entity is Proxy3D)
            //	System.Diagnostics.Debug.WriteLine ("ScaleCuller.Apply() - Proxy3D");
            //if (entity.ID == "motionfield")
            //{
            //    System.Diagnostics.Debug.WriteLine("Motionfield found with ID = " + entity.ID);
                //System.Diagnostics.Debug.WriteLine (entity.SceneNode.Position.ToString());
                //System.Diagnostics.Debug.WriteLine (entity.DerivedTranslation.ToString());
            //}

            //if (entity.ID == "UNDEFINED_INTERIOR_GRID_ENTITY")
            //    System.Diagnostics.Debug.WriteLine("floorplan found.");
            // note culling was already done by the sceneNode of this entity
            // NOTE: We don't check for .Geometry != null because root entities dont always 
            // have geometry yet we are still required to Scale them if their child entities
            // do have children

            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (!Allow(entity)) return data;
            
            CullState state = (CullState)data;
            RegionPVS current = state.PVS;
 

            double distanceToCameraSquared = Vector3d.GetLengthSquared(state.CameraSpacePosition);

            // NOTE: we do this test here on the entity and not the EntityNode since we do 
            // still want to traverse child EntityNode's.  The reason for this is because 
            // a Star may be too far to render, but we still want to cull it's children since
            // camera can be right next to Neptune or Pluto and obviously very far from Sol
            // but being far from Sol is no excuse to not try to cull test Neptune or Pluto.
            // TODO: replace MaxVisibleDistanceSquared with simple flag for use large frustum or not.
            // TODO: the reason this MaxVisibleDistanceSquared test seems to have suddenly broken is that with 2D Proxy, those proxies weren't
            // subjected to this cull test, but the 3D retained ones are.   and this test was failing for all planets anyways
            // we just didn't notice it because the 2d proxies were being rendered in there place
            // and the "IconizeNoRecurse" for 2d doesn't depend on what is "visible" during culling.  It occurs BEFORE culling
            // and is using just StellarSystem hierarchy to recurse to discover all relevant entities.
            // TODO: even vehicles that are not visually in range will need to be handled by HUD
            //       to mimic sensors.  I think celestials too, and we're not entirely doing that with out hud yet
            //       our sensors and our ship's library of celestial bodies isn't doing that either.  There are 
            //       after all systems with planets where the stars should be visbible but the planets typically
            //       maybe detected but actual size and composition unknown.  
            //       TODO: but maybe some signs of civilization could be such as radio emissions
            //  - so basically what we're talking about here is a need for a more systematic way of dealing with
            //  the rendering of all available sources of information be it from library (sqldb), sensors, sensor histories and prediction, 
            //  or actual visual observations.
            //		- and our library needs a way to track the information a user actually has and the information the user has not discvoered yet
            //			- further information can be stale.  A weapon might have destroyed a city on a world sicne the last update
            //          and data about that city will no longer be accurate unless it's updated from some src driven by the simulation.
            //			- data that can go stale then should have a date as to when that bit of information was obtained.
            //  - this doesnt necessarily mean our HUD has to run any "simulation" but the simulation must be able to provide the HUD
            //    with this info or a store of it.  The star digest is an example of a store of minimal star info for HUD display purposes.     
			// TODO: we need to assign proxies to far planets in stellar system or we can't navigate to them
			//       the following MaxVisibleDistanaceSquared is commented out until we fix that 
//            if (entity is IEntityProxy == false)
//            	if (distanceToCameraSquared > entity.MaxVisibleDistanceSquared ) return null; // <-- 3D Proxies will get culled here unless we test if IEntityProxy and/or KeyCommon.Flags.EntityFlags.HUD and skip
            
            
            // NOTE: Scaling WILL affect child ModeledEntities even if this current ModeledEntity
            //       has NO Model of it's own.
            // NOTE: Remember scaling if InheritScaling will have a cumulative
            // affect so to scale a player will also scale everything it's holding.  
            // Keep this in mind for our editor Widgets too.

            // because some Entity's may have child entities but no model of it's own, 
            // but those children will need to inherit the scale of it's parent

            // this is intended for GUI objects like scaling, translation, rotation widgets where we want accurate picking as well
            // and so scaling the entire entity allows picking to work whereas simply scaling the geometry prior to render would not
            // however this same method is not acceptable for things like worlds where we're scaling position and size to compensate
            // for zbuffer precision issues.
            if (entity.UseFixedScreenSpaceSize && entity.ScreenSpaceSize != 0)
            {
                // TODO: rather than change scale here, should we set this tempscale
                // in our visibleiteminfo and then during modeledEntity.Render() pass that
                // temp scale?  Well, maybe not because by scaling it up, we still can
                // pick the entity at it's presented size on screen.
                // However, this screenspace recalc seems more like something the entity's
                // own Update() script would apply
                double scale;
                if (mContext.ProjectionType == Viewport.ProjectionType.Orthographic)
                {
                    //scale = -mContext.GetConstantScreenSpaceScale2(cameraSpacePosition, entity.ScreenSpaceSize);
                    scale = mContext.GetConstantScreenSpaceScaleOrthographic(2);
                }
                else
                {
                	// TODO: our picker needs to take into account screenspace scaling if entity.UseFixedScreenSpaceSize == true
                	// TODO: are we having issues here if the entity has modelSequence node and the hierarchical scaling goes screwy perhaps?
                	//       something is happening here since i refactored tool preview's to use strategy pattern of HUDToolPreview derived classes
                    scale = mContext.GetConstantScreenSpaceScalePerspective(state.CameraSpacePosition, entity.ScreenSpaceSize);
                }

                entity.Scale = Vector3d.Scale(scale);;
            }
            
			// TODO: this call to SelectModel selects models but what if the first child is a nested Selector node?
			// TODO: also note that here we're not culling the individual models. in the case of Structure's which contains
			// many floors, this means if any part of a zone is visible, all floors are going to be selected and rendered.
            Model[] results = entity.SelectModel(SelectionMode.Render, distanceToCameraSquared);
            if (results != null)
            {
            	//if (results[0].Geometry.ID.Contains("caesar\\meshes\\pointsprites\\motionfield.tvm"))
            	//{
            	//	Debug.WriteLine ("ScaleCuller.Apply(ModeledEntity) - " + entity.ID);
            	//}
//            	if (entity.ID == "1234")
//            	{
//            		Debug.WriteLine ("ScaleCuller.Apply(ModeledEntity) - " + entity.ID);
//            	}
            	VisibleModelsFound (state, entity, results);
            }
            // NOTE: Sub entities are already attached via sceneNodes and thus are culled from there and not by traversing entity children here
            return state;
        }
#else
        public object Apply(Controls.Control2D control, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (Ignore (control)) return data;
            
            throw new NotImplementedException("todo!");
            return data;
        }

        public object Apply(ModeledEntity entity, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (Ignore (entity)) return data;
            
        	// TODO: when does this flag ever get reset?
            entity.Flags |= Entity.EntityFlags.AlreadyCulled;

            // determine the cameraspace position so we can store it in the VisibleItemInfo
            RegionPVS current = _regionPVSStack.Peek();

            double distanceToCameraSquared = Vector3d.GetLengthSquared(state.CameraSpacePosition);

            // NOTE: Scaling WILL affect child ModeledEntities even if this current ModeledEntity
            //       has NO Model of it's own.
            // NOTE: Remember scaling if InheritScaling will have a cumulative
            // affect so to scale a player will also scale everything it's holding.  
            // Keep this in mind for our editor Widgets too.
            if (entity.UseFixedScreenSpaceSize && entity.ScreenSpaceSize != 0) 
            {   
                // because some Entity's may have child entities but no model of it's own, 
                // but those children will need to inherit the scale of it's parent

                // this is intended for GUI objects like scaling, translation, rotation widgets where we want accurate picking as well
                // and so scaling the entire entity allows picking to work whereas simply scaling the geometry prior to render would not
                // however this same method is not acceptable for things like worlds where we're scaling position and size to compensate
                // for zbuffer precision issues.
                
                // TODO: rather than change scale here, should we set this tempscale
                // in our visibleiteminfo and then during modeledEntity.Render() pass that
                // temp scale?  Well, maybe not because by scaling it up, we still can
                // pick the entity at it's presented size on screen.
                // However, this screenspace recalc seems more like something the entity's
                // own Update() script would apply
                double scale = mContext.GetConstantScreenSpaceScale(Math.Sqrt(distanceToCameraSquared));
                if (mContext.ProjectionType == Viewport.ProjectionType.Orthographic)
                {
                    //scale = -mContext.GetConstantScreenSpaceScale2(cameraSpacePosition, entity.ScreenSpaceSize);
                    scale = mContext.GetConstantScreenSpaceScaleOrthographic(2);
                }
                Vector3d vecScale;
                vecScale.x = scale;
                vecScale.y = scale;
                vecScale.z = scale;
                entity.Scale = vecScale;
            }

            // NOTE: entity.SelectModel excludes model's that have Model.Enable == false
            Model[] results = entity.SelectModel(SelectionMode.Render, distanceToCameraSquared);
            VisibleModelsFound (mContext.ID, entity, results, cameraSpacePosition, null);

            // NOTE: Sub entities are already attached via sceneNodes and thus are culled from there and not by traversing entity children here
            
        	// _visibleNodes.Pop(); // NO Popping in this stack (unless its an Imposter!). We are creating a stack of all nodes that must be rendered.
        
        
            return data;
        }
#endif

        public object Apply(PlayerCharacter entity, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (!Allow(entity)) return data;
            mContext.Statistics.IncrementStat("Actors Visible");
            return data;
        }

#if MULTITHREADED
		// OBSOLETE - June.2014 - Terrain is not an Entity and is Geometry now
        public object Apply(Terrain terrain, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            //if (Ignore (terrain)) return data;
            
            //    RegionPVS current = _regionPVSStack.Peek();
            //    Vector3d cameraSpacePosition = terrain.SceneNode.Position - current.RegionRelativeCameraPos;
            //    double distanceToCameraSq = Vector3d.GetLengthSquared(cameraSpacePosition);

            //    PushVisibilityTestResult(terrain.BoundingBox, distanceToCameraSq,
            //        terrain.MaxVisibleDistanceSquared,
            //        terrain == mContext.Scene.Selected,
            //        mContext.ShowTerrainBoundingBoxes,
            //        CONST_TV_COLORKEY.TV_COLORKEY_GREEN);

            //    if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
            //    {
            //        mContext.Statistics.IncrementStat("Terrains Visible");

            //        throw new NotImplementedException("model parameter for VisibleItemInfo not valid for terrain... How to resolve?");
            //        VisibleItemInfo vi;// TODO: uncomment/fix following after finishing refactor -  new VisibleItemInfo(terrain, cameraSpacePosition);
            //        _currentRegionPVS.Add(vi, false);
            //        if (terrain.Children != null)
            //        {
            //            foreach (Node chunk in terrain.Children)
            //            {
            //                //TODO: we could use this as our single point where we enable/disable chunks during rendering from anywhere be it landshadow or regular
            //            }
            //        }
            //    }
            
            return data;
        }
#else
		// OBSOLETE - June.2014 - Terrain is not an Entity and is Geometry now
        public object Apply(Terrain terrain, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (Ignore (terrain)) return data;
//                RegionPVS current = _regionPVSStack.Peek();
//
//                PushVisibilityTestResult(terrain.BoundingBox,
//					                    terrain == mContext.Scene.Selected, 
//					                    mContext.ShowTerrainBoundingBoxes,
//					                    CONST_TV_COLORKEY.TV_COLORKEY_GREEN);
//                
//                if (_frustumIntersectResult.Peek() != IntersectResult.OUTSIDE)
//                {
//                    mContext.Statistics.IncrementStat ("Terrains Visible");
//                    mItemFoundDelegate(current, terrain, null, cameraSpacePosition);
//
//                    if (terrain.Children != null)
//                    {
//                        foreach (Node chunk in terrain.Children)
//                        {
//                            //TODO: we could use this as our single point where we enable/disable chunks during rendering from anywhere be it landshadow or regular
//                        }
//                    }
//                }
//                PopVisibilityTestResult();
            
            return data;
        }
#endif
        
#if MULTITHREADED
        /// <summary>
        /// Lights are found through regular traversal, but maybe doing an initial 
        /// light cull pass before geometry cull pass would be better?  The reason is because
        /// then during geometry cull, i'll already have found all lights.  However, once I have both
        /// lights and geometry in seperate lists, i should be able to do what i do now which is traverse
        /// each light and find which geometry they touch?  A traversal down list beats two traversals down
        /// scenegraph right?
        /// </summary>
        /// <param name="light"></param>
        public object Apply(Light light, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (!Allow (light)) return data;
            
            CullState state = (CullState)data;
            RegionPVS current = state.PVS;
            Vector3d cameraSpacePosition = light.SceneNode.Position - current.RegionRelativeCameraPos;
            cameraSpacePosition = light.RegionMatrix.GetTranslation() - current.RegionRelativeCameraPos;
            // hack light.Range = float.MaxValue;
            // IntersectResult tells us if the light was fully inside hte frustum, 
            // or partial (it'll never be outside at this point since this Apply() 
            // method would be skipped
            IntersectResult result = state.ParentIntersectResult;
            
            // invoke handler in RenderingContext.OnVisibleLightFound()
            BoundingBox cameraSpaceBBox = GetCameraSpaceBox (light.SceneNode.BoundingBox, current.RegionRelativeCameraPos);
            mLightFoundDelegate(current, light, cameraSpacePosition, result, cameraSpaceBBox);
            return data;
            // a key for adding these visible lights to interiors is that
            // we can (hopefully) use the interior "rooms" to also enable/disable visible lights
            // and any light that intersects a portal, we construct an light volume through that
            // portal.  This way our lights will not penetrate through rooms (in deferred though i think
            // we need a different solution)
        }
#else
        /// <summary>
        /// 
        /// </summary>
        /// <param name="light"></param>
        public object Apply(Light light, object data)
        {
            // NOTE: for Culler, Ignore() is only used on Apply(Entities), never Apply(SceneNodes). 
            //       Exclude() is used on SceneNodes.
            // NOTE: we do not return "null" here because we do still want child EntityNodes to be recursed.
            if (Ignore (light)) return data;
            
            RegionPVS current = _regionPVSStack.Peek();
            Vector3d cameraSpacePosition = light.SceneNode.Position - current.RegionRelativeCameraPos;
            
            // IntersectResult tells us if the light was fully inside hte frustum, 
            // or partial (it'll never be outside at this point since this Apply() 
            // method would be skipped
            IntersectResult result = _frustumIntersectResult.Peek();
            BoundingBox cameraSpaceBBox = GetCameraSpaceBox (light.SceneNode.BoundingBox, current.RegionRelativeCameraPos);
            mLightFoundDelegate(current, light, cameraSpacePosition, result, cameraSpaceBBox);     
            // a key for adding these visible lights to interiors is that
            // we can (hopefully) use the interior "rooms" to also enable/disable visible lights
            // and any light that intersects a portal, we construct an light volume through that
            // portal.  This way our lights will not penetrate through rooms (in deferred though i think
            // we need a different solution)
            return data;
        }
#endif

#if MULTITHREADED
        public object Apply(Model model, object data)
        {
        	
            if (model.Geometry.PageStatus != IO.PageableNodeStatus.Loaded) return null;

            CullState state = (CullState)data;

           	Entity entity = model.Entity;

            //if (model.ID == "4269e86a-46d0-4c0f-9fec-039d5c29e10a")
            //    System.Diagnostics.Debug.WriteLine("ring...");
            // NOTE: we use the model's Region relative Transform, NOT the global.  We don't need the global because
            // we are already transforming the camera to the current region's transform during cull traversal.
            RegionPVS current = state.PVS;
            // TODO: i forget but what about putting the cameraSpacePosition inside of CullState?
            //       and along with ViewScaling, pass those both to the mItemFoundDelegate so the bucket item can be created
            // model.DerivedTranslation ? <-- why not use that instead of model.RegionMatrix.GetTranslation()?
            
            Vector3d cameraSpacePosition = model.RegionMatrix.GetTranslation() - current.RegionRelativeCameraPos; // entity.SceneNode.Position - current.RegionRelativeCameraPos;
            // Oct.19.2016 - I believe the following special case for IEntityProxy is wrong.  Our proxies can have different 
            //               global positions (eg. scaled down Starmaps and System Maps) so cannot rely on ReferencedEntity.GlobalTranslation.
            //if (entity is IEntityProxy) // for proxies, since they are drawn from root, their camera space position never uses regionRelative coords, but global
            	//cameraSpacePosition = ((IEntityProxy)entity).ReferencedEntity.GlobalTranslation - state.Context.Viewpoint.GlobalTranslation; // entity.GlobalTranslation - state.Context.Viewpoint.GlobalTranslation;
            
#if DEBUG
            if (model.Geometry is Terrain)
            	 mContext.Statistics.IncrementStat("Terrains Visible");
            else if (model.Geometry is Mesh3d)
                mContext.Statistics.IncrementStat("Meshes Visible");
            else if (model.Geometry is Actor3d)
                mContext.Statistics.IncrementStat("Actors Visible");
            else if (model.Geometry is TexturedQuad2D)
            {
                mContext.Statistics.IncrementStat("2D Quads Visible");
            }
            else if (model.Geometry is MinimeshGeometry)
            {
            	mContext.Statistics.IncrementStat("MinimeshGeometry Visible");
            	MinimeshGeometry mini = (MinimeshGeometry)model.Geometry;
            	for (uint i = 0; i < mini.InstancesCount; i++)
	            	mContext.Statistics.IncrementStat ("MinimeshGeometry Element Count");
            }
            else if (model.Geometry is Emitter)
                mContext.Statistics.IncrementStat("Particle Systems Visible");
            else
                mContext.Statistics.IncrementStat("Other Geometry Visible");
#endif // debug

            // invoke handler in RenderingContext.cs 
            BoundingBox transformedModelBox = BoundingBox.Transform(model.BoundingBox, model.RegionMatrix);
            BoundingBox cameraSpaceBBox = GetCameraSpaceBox (transformedModelBox, current.RegionRelativeCameraPos);
                        
            mItemFoundDelegate(current, entity, model, cameraSpacePosition, cameraSpaceBBox);
            return data;
        }
#else
		// TODO: eventually i will need to move all the Multithreaded versions into a seperate .partial file 
		//       because it will be easier to see the differences between the two versions and keep them sychronized if
		//       in seperate files we can DIFF
        public object Apply(Model model, object data)
        {
            if (model.Geometry.PageStatus != IO.PageableNodeStatus.Loaded) return null;

            ModeledEntity entity;
            
            if (model.Parents[0] is ModelSelector)
                entity = (ModeledEntity)((ModelSelector)model.Parents[0]).Parents[0];
            else
                entity = (ModeledEntity)model.Parents[0];

            //Debug.WriteLine(entity.ID);  

            // NOTE: we use the model's Region relative Transform, NOT the global.  We don't need the global because
            // we are already transforming the camera to the current region's transform during cull traversal.
            RegionPVS current = _regionPVSStack.Peek();
            // when we have multiple viewports, the distance is specific to each camera anyway.
            // model.DerivedTranslation ? <-- why not use that instead of model.RegionMatrix.GetTranslation()?
            Vector3d cameraSpacePosition = model.RegionMatrix.GetTranslation() - current.RegionRelativeCameraPos; // entity.SceneNode.Position - current.RegionRelativeCameraPos;                

#if DEBUG
            if (model.Geometry is Terrain)
            	 mContext.Statistics.IncrementStat("Terrains Visible");
            else if (model.Geometry is Mesh3d)
                mContext.Statistics.IncrementStat("Meshes Visible");
            else if (model.Geometry is Actor3d)
                mContext.Statistics.IncrementStat("Actors Visible");
            else if (model.Geometry is TexturedQuad2D)
            {
                mContext.Statistics.IncrementStat("2D Quads Visible");
            }
            else if (model.Geometry is MinimeshGeometry)
            {
            	mContext.Statistics.IncrementStat("MinimeshGeometry Visible");
            }
            else if (model.Geometry is Emitter)
                mContext.Statistics.IncrementStat("Particle Systems Visible");
            else
                mContext.Statistics.IncrementStat("Other Geometry Visible");
#endif // debug
			
			// invoke handler in RenderingContext.cs
			BoundingBox cameraSpaceBBox = GetCameraSpaceBox (entity.SceneNode.BoundingBox, current.RegionRelativeCameraPos);
            mItemFoundDelegate(current, entity, model, cameraSpacePosition, cameraSpaceBBox;
            return data;
        }
#endif

		private void VisibleModelsFound (CullState state, Entity entity, Model[] results)
		{
			if (results == null) return;
			string renderingContextID = state.Context.ID;
			Vector3d cameraSpacePosition = state.CameraSpacePosition;
			
			// TODO: as with ModelIDs array, should we pass shaderIDs array to OnRender() script
			string shaderID = "";
			if ( results[0].Appearance != null && results[0].Appearance.Shader != null)
				shaderID = results[0].Appearance.Shader.ID;
            string modelID = results[0].ID;

            // TODO: why is entity.Scene sometimes null for 2D controls?  Seems if we can cull it, that it's Scene property should be assigned
            //            if (entity.Scene == null) return;

            double elapsedSeconds = 0;
            if (entity.Scene != null)
            {
                elapsedSeconds = entity.Scene.Simulation.GameTime.ElapsedSeconds;
            }
            // NOTE: here we call a single OnRender() script method so that any new visual
            // fx like lens flare or HUD can be added to regionPVS before we start ScaleDrawer		
            // NOTE: we provide an array of all the modelID's to the OnRender() script
            string[] modelIDs = new string[results.Length];
            for (int i = 0; i < modelIDs.Length; i++)
                modelIDs[i] = results[i].ID;

        	using (CoreClient._CoreClient.Profiler.HookUp("OnRenderScript"))
    		    entity.Execute("OnRender", new object[] { renderingContextID, entity.ID, modelIDs, shaderID, cameraSpacePosition, state.CameraSpaceBoundingBoxVertices, elapsedSeconds });

            for (int i = 0; i < results.Length; i++)
                if (results[i] != null)
                    if (results[i].Geometry != null && results[i].Geometry.Enable) // in case of internal structure of CelledRegion, walls for instance might have Geometry filtering enabled so we don't see the walls.
            		{
                        // TODO: i think we should clone states, although probably not necessary for geometry
                        // but the rule should be the parent node passes in the cloned node to the child
                        //if (entity.Name == "distance disk")
                        //{
                        //    System.Diagnostics.Debug.WriteLine("distance disk");
                        //}
                        //if (i == 50)
                        //    System.Diagnostics.Debug.WriteLine("MinimeshGeometry");
            			Apply(results[i], state);
            		}	
		}

        public object Apply(ModelLODSwitch lod, object data)
        {
            throw new Exception();
        }

        public object Apply(Geometry element, object data)
        {
            throw new Exception();
        }
        #endregion

    }
}
