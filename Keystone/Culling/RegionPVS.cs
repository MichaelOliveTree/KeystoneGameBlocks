using System;
using System.Collections.Generic;
using Keystone.Types;
using Keystone.Immediate_2D;
using Keystone.Entities;
using Keystone.Elements;
using Keystone.Culling;
using System.Diagnostics;
using MTV3D65;
using Keystone.Lights;

namespace Keystone.Culling 
{
	[Flags]
    public enum BucketMasks
    {
        None = 0,
        SmallFrustum = 1 << 0,
        LargeFrustum = 1 << 1,
        
        Background = 1 << 2,  // if a background can be rendered without alpha and at real scale, it does not need this flag.  
        Overlay = 1 << 3,     // overlay items are items rendered on top of the rest of the scene
        LateGeometry = 1 << 14, // water meshes for instance need to be rendered as last meshes in the regular scene (though still before overlay items)        
        
        CSGStencil = 1 << 4,
        CSGTarget = 1 << 5,
        AlphaBlending = 1 << 6,

        Item2D = 1 << 7,
        Item3D = 1 << 8,
        
        LinePrimitives = 1 << 9, // | Item2D
        Textured2DPrimitive = 1 << 10, // | Item2D
        
        Text = 1 << 11, // | Item2D
        TextBillboard = 1 << 12,
        
        Debug = 1 << 13,
        All = int.MaxValue
    }
    /// <summary>
    /// Visible Set of items in this particular Region
    /// </summary>
    public class RegionPVS
    {
        private interface IVisibleItemComparer : System.Collections.Generic.IComparer<VisibleItem>
        { 
             //int Compare(VisibleItem x, VisibleItem y);
        }

        /// <summary>
        /// If the node being inserted is CLOSER to the camera than 
        /// the current existing node being evaluated, return 1 (true)
        /// so that it will be placed HIGHER in the list. (higher means have a smaller
        /// index so that it will be encountered earlier when iterating from 0 to N)
        /// </summary>
        private class FrontToBackComparer : IVisibleItemComparer
        {
            #region IComparer<VisibleState> Members
            // bool mTestDistanceToSurface; // rather than distanceToCenter
            // bool mUseSortOrder;          // if a sort order is specified, always refer to that
            public int Compare(VisibleItem newItem, VisibleItem existingItem)
            {
                Model model1 = newItem.Model;
                Model model2 = existingItem.Model;

                //if (model1.Geometry != model2.Geometry)
                //    return 0;

                if (newItem.RenderPriority > existingItem.RenderPriority)
                    return 1;
                else if (newItem.RenderPriority < existingItem.RenderPriority)
                    return 0;
                // else the priorities are the same

                // TODO: how would we sort on a transparency priority
                if (model1.Appearance == null || model2.Appearance == null)
                {
                    if (model1.Appearance == null) return 0;
                    return 1;
                }

                if (model1.Appearance.GetHashCode() != model2.Appearance.GetHashCode())
                    return 0;
                if (model1.UseInstancing != model2.UseInstancing)
                    return 0;

                // sort items that use same appearance front to back.  If the new item is CLOSER
                // then we want to render it first and so we want it HIGHER in the list so we return 1
                return newItem.DistanceToCameraSq < existingItem.DistanceToCameraSq ? 1 : 0;
            }
            #endregion
        }

        /// <summary>
        /// Items that are farther away are inserted ahead of items that are near.
        /// This is PAINTERS ALGORITHM so that near stuff is rendered last and will render
        /// after far stuff is rendered.  Useful for Alpha Sorting.
        /// 
        /// If the node being inserted is FARTHER from the camera than 
        /// the current existing node being evaluated, return 1 (true)
        /// so that it will be placed HIGHER in the list. (higher means have a smaller
        /// index so that it will be encountered earlier when iterating from 0 to N)
        /// </summary>
        private class BackToFrontComparer : IVisibleItemComparer
        {
            #region IComparer<VisibleState> Members
            public int Compare(VisibleItem newItem, VisibleItem existingItem)
            {

                Model model1 = newItem.Model;
                Model model2 = existingItem.Model;

                //if (model1.Geometry != model2.Geometry)
                //    return 0;
                if (newItem.RenderPriority > existingItem.RenderPriority) 
                    return 1; 
                else if (newItem.RenderPriority < existingItem.RenderPriority)
                    return 0;
                // else the priorities are the same

                // WARNING: BackToFrontComparer is usually used for ALPHA BLENDING
                // and this means the Distance test is more important than the state sorting
                // however, if we can sort by distance first _AND THEN_ appearance, fine
                // but distance must come first
                
               // sort items that use same appearance front to back.  If the new item is FARTHER
                // then we want to render it first and so we want it HIGHER in the list so we return 1
                return newItem.DistanceToCameraSq > existingItem.DistanceToCameraSq ? 1 : 0;
                
                // TODO: id like to add profiler code to test how long culling of octrees take
                //       and all frustum cull tests in general.
                // TODO: i believe these tests for null cost us a ton in IComparer
                //       need to test adding a shortcut in model1.AppearanceCode;
//                if (model1.Appearance != null && model2.Appearance != null) // both are not null
//                {
//                    // sort by appearance to minimize state changes
//                    if (model1.Appearance.LastHashCode != model2.Appearance.LastHashCode)
//                        return 0;
//                }
//                else if (model1.Appearance == null || model2.Appearance == null) // one of them is null but not both
//                    return 0;
//                if (model1.UseInstancing != model2.UseInstancing)
//                    return 0;


            }
            #endregion
        }


        public List<LightInfo> _visibleLights;

        // NOTE: SingleLinkedLIst is used to speed up sorted insertion
        //// http://www.sjbaker.org/steve/omniv/alpha_sorting.html
        //// render alpha items last and then optionally render the alpha's in order back to front
        //// TODO: DO not need a seperate alpha items.  Can add those such that they are always last.
        //// Overlays potentially too.  WRONG.  We need seperate because we have to render both
        //// large and regular before alphas or overlays of either type
        //public keymath.DataStructures.SingleLinkedList<VisibleItemInfo>[] mItemsGeneral;
        //public keymath.DataStructures.SingleLinkedList<VisibleItemInfo>[] mItemsCSGTargets;
        //public keymath.DataStructures.SingleLinkedList<VisibleItemInfo>[] mItemsCSGTPunches;
        //public keymath.DataStructures.SingleLinkedList<VisibleItemInfo>[] mItemsAlpha;
        //public keymath.DataStructures.SingleLinkedList<VisibleItemInfo>[] mItemsOverlay;
        //public keymath.DataStructures.SingleLinkedList<IRenderable2DItem>[] mItems2d;
        //public keymath.DataStructures.SingleLinkedList<IRenderable2DItem>[] mItems2dAlpha;
        //public keymath.DataStructures.SingleLinkedList<IRenderable2DItem>[] mItemsTexts;


        // buckets of renderable items 
        public Dictionary<BucketMasks, keymath.DataStructures.SingleLinkedList<VisibleItem>> mBuckets;
        public Dictionary<BucketMasks, keymath.DataStructures.SingleLinkedList<IRenderable2DItem>> m2DBuckets;
        
        
        private object mBucketLock = new object();

        public FrustumInfo[] FrustumInfo;
        public Matrix InverseView;
        private Matrix View;
        public Vector3d RegionRelativeCameraPos;

        private RenderingContext _context;
        private RegionNode RegionNode; 

        public bool _indoor; // determined based on the Region we traverse thru
        private FrontToBackComparer mFrontToBackComparer;
        private BackToFrontComparer mBackToFrontComparer;
        //private BucketInsertComparer 

        double mDistanceToSmallFarPlaneSquared;
        double mDistanceToLargeNearPlaneSquared;


     
        public RegionPVS(RegionNode regionNode, RenderingContext context, 
                         Matrix relativeInvViewMatrix, Matrix relativeViewMatrix, 
                         Vector3d regionRelativeCameraPosition)
        {
            RegionNode = regionNode;
            _context = context;
            InverseView = relativeInvViewMatrix;
            View = relativeViewMatrix;
            RegionRelativeCameraPos = regionRelativeCameraPosition;

            mDistanceToSmallFarPlaneSquared = _context.Far * _context.Far;
            mDistanceToLargeNearPlaneSquared = _context.Near2 * _context.Near2;

            _visibleLights = new List<LightInfo>();

            mBuckets = new Dictionary<BucketMasks, keymath.DataStructures.SingleLinkedList<VisibleItem>>();
            m2DBuckets = new Dictionary<BucketMasks, keymath.DataStructures.SingleLinkedList<IRenderable2DItem>>();

            _indoor = false;
            mFrontToBackComparer = new FrontToBackComparer();
            mBackToFrontComparer = new BackToFrontComparer();

            FrustumInfo = new FrustumInfo[2];
            FrustumInfo[0] = CreateFrustumInfo(_context.Near, _context.Far);           
            FrustumInfo[1] = CreateFrustumInfo(_context.Near2, _context.Far2);
            	
            // disable 6th plane for very large items that have a max visible distance > farplane distance
            FrustumInfo[1].Frustum.EnabledPlanes = new bool[]{true, true, true, true, true, false};
       
            // TODO: what if first I did a sphere test and then started to add the item
            // and then do cone and frustum tests here?
        }

        public string ID 
        {
            get { return RegionNode.Region.ID; }
        }

       
        private FrustumInfo CreateFrustumInfo(double near, double far)
        {
            FrustumInfo f = new FrustumInfo();
            f.Far = far;
            f.Near = near;

            // seperate sets of projection and frustums for default and large object culling
            double dummy = 0;
            if (_context.ProjectionType == Viewport.ProjectionType.Perspective)
                f.Projection = Matrix.PerspectiveFOVLH(f.Near, f.Far,
                                                _context.FOVRadians,
                                               _context.Viewport.Width, _context.Viewport.Height,
                                               ref dummy);
            else
            {
                // should i specify here that the near and far planes be disabled so that
                // 
                f.Projection = Types.Matrix.ScaledOrthoMatrix(_context.Zoom, _context.Viewport.Width,
                                            _context.Viewport.Height, f.Near, f.Far);
            }

            f.Frustum = new ViewFrustum();
            f.Frustum.Update((float)f.Near, (float)f.Far,
                (float)_context.FOVRadians,
                _context.Position, _context.LookAt,
                View, f.Projection,
                _context.Viewport.Width, _context.Viewport.Height);

            return f;
        }

        /// <summary>
        /// Our RegionCullingInfo Contains two frustums so our IntersectResult method is moved here
        /// and we add arguments for entity distance to camera and max visible range so that
        /// we can determine first which frustum to you use and then perfect the test and return
        /// the intersection result
        /// </summary>
        /// <returns></returns>
        internal IntersectResult IntersectionTest(BoundingBox cameraSpaceBox, double distanceToCameraSq)
        {
            if (distanceToCameraSq < mDistanceToSmallFarPlaneSquared)
            {
                return FrustumInfo[0].Frustum.Intersects(cameraSpaceBox);
            }
            // else if (distanceToCameraSq < maxVisibileDistanceSq)  // obsolete - we dont want our cull to be limited by this, we want the rendering to be limited by maxVisibleDistance.  Otherwise we run into issues where we're too far from Star and planets stop being culled because we're too far from star even though we ARE within the star's SceneNode's overall bbox
            else  
            {
                return FrustumInfo[1].Frustum.Intersects(cameraSpaceBox);
            }
            // object is beyond small frustum and camera is further from it than it's max visibile distance 
            // it's too far to be visible even though it may still be inside the Far frustum, it can't be seen
            // by naked eye.  If we need some radar sensor check then that is game logic and not visibility
            // and our game logic radar test cannot be limited to what is visible to the frustum cull
            //else
            //    return Types.IntersectResult.OUTSIDE; // obsolete - see above note on maxVisibleDistanceSq problems.  We cannot fail frustum test just because a parent is too far because children may actually be really close such as close to Neptune but too far from Sol so that Sol cull test fails and then Neptune never gets traversed.
        }

        internal IntersectResult IntersectionTest(Vector3d[] coords, double distanceToCameraSq)
        {
        	if (distanceToCameraSq < mDistanceToSmallFarPlaneSquared)
	            return FrustumInfo[0].Frustum.Intersects(coords);
        	else
        		return FrustumInfo[1].Frustum.Intersects(coords);
        }

        private bool IsSelected(Entity entity)
        {
            return _context.Workspace.SelectedEntity != null && _context.Workspace.SelectedEntity.Entity == entity ||
                (_context.Workspace.MouseOverItem != null && _context.Workspace.MouseOverItem.Entity != null &&
                _context.Workspace.MouseOverItem.Entity == entity);
        }


        /// <summary>
        /// Moves the TV Light within the TVEngine to camera space in preparation for rendering.
        /// Does NOT move the actual Entity and so no octree or quadtree
		/// or movement flags are triggered. 
		/// HOWEVER, you MUST stilll restore the original 
		/// position within the DX / TV3D light or artifacts will occur 
		/// and you won't remember why - MichaelOliveTree Dec 19 2025
        /// </summary>
        /// <param name="lightInfo"></param>
        private void MoveTVLightToCameraSpace(LightInfo lightInfo)
        {
            // the following call DOES NOT MOVE the Light.Translation
            // instead it calls against TVLightFactory thusly:
            // CoreClient._CoreClient.Light.SetLightPosition(_tvfactoryIndex, (float)cameraSpacePosition.x, (float)cameraSpacePosition.y, (float)cameraSpacePosition.z);
            lightInfo.Light.SetCameraSpaceTranslationForRendering(lightInfo.CameraSpacePosition);
        }
        

        // TODO: the _visibleLights.Add() is not threadsafe and the count gets incremented before
        // the lightInfo is referenced, thus in another thread we can see count == 1 but
        // lightInfo still null and not yet set.  
        // If the Add is not threadsafe, it should be ok so long as we dont do any sorting here or removal.
        public void Add(LightInfo lightInfo)
        {
            // during cull traversal we determine and store all visible lights.
            // it's not until Draw traverser's Render() as we iterate through each PVS list of buckets
            // and then iterate through each item in those buckets to determine which lights affect them.
            // 
            _visibleLights.Add(lightInfo);
            lightInfo.mRegionPVS = this;


            //if (DeferredRendering == false) // FORWARD
            //{
                
            //}
            //else // DEFERRED
            //{
            //    if (light.Light is Keystone.Lights.DirectionalLight)
            //    {
            //        // a single dir light can be rendered in Deferred too but it is
            //        // passed in as semantic to the deferred shader
            //        // i think?  have to research this
            //    }
            //    else if (light.Light is Keystone.Lights.PointLight)
            //    {
            //        // TODO: pointlight minis just like ALL minis must be added relative to
            //        // the region because the projection and matrix must be switched inbetween regions
            //        // This is why light info should be per RegionPVS just like all VisibleItemInfos.
            //        // However, i think there's still value in maybe using the regions as a key
            //        // into a minimesh that is stored in the RenderingContext or the ScaleDrawer...hrm...
            //        //
            //        mPointLightSphereMesh.Minimesh.AddInstanceData((Keystone.Lights.PointLight)light.Light, light.CameraSpacePosition);
            //        // TODO: modify our "Add Pointlight" gui button to generate random
            //        // pointlights like in the deferred tutorial
            //        // TODO: where does the detection of Settings.ReadBoolean ("deferred"); 
            //        // first dected and changed?  In the initialization of the rendering context?
            //        // 
            //        // int numLights = 52;
            //        // for (int i = 0; i < numLights; i ++)
            //        // {
            //        //      CreatePointLight(random);
            //        // }
            //    }
            //}

        }

       

        public void Add(VisibleItem item)
        {
#if LOGZ // logarithmic z-buffer
            addAsLargeItem = false;
#endif
            Entity entity = (Entity)item.Entity;
            Model model = item.Model;

        	// TODO: when is this .InFrustum ever set back to false? in Cull list clear?
            entity.InFrustum = true;
                            
            // do not Render models that have an overall appearance material that is completely transparent.
            // If the Appearance has Groups then all groups must be completely transparent, but for now we 
            // wont bother testing for that.  This optimization gives our interior rendering a huge boost
            // when exterior transparency = 0.0f
            // I don't believe this should ever cause any false positives when working with models that have
            // shaders attached and need some material that has 0.0 for some reason.

            // Dec.19.2023 - The following opacity test is wrong.  We may want fully transparent Models such as mouse pickable planes and 
            //               if the opacity is 0.0f we wont be updating the geometry's matrix and thus picking will fail.
            // todo: in the actual model.Render() we could compute a new matrix and then skip the actual geometry.Render()
//            if (model.Appearance != null && model.Appearance.Material != null && model.Appearance.Material.Opacity == 0.0)
//            {
//#if DEBUG
////                System.Diagnostics.Trace.WriteLine(@"RegionPVS.Add() - WARNING: model appearance or material null or 0.0 Opacity. 
////                                                   Skipping rendering.  Check Material Editor opacity setting."); 
//#endif
//                return;

//            }
            // TODO: maybe we want to add to imposter same way we add to
            // minimesh via AddInstance() ?  The Imposter system should be a type of renderer
            // not an FX.
            if (entity.FXData[(int)Keystone.FX.FX_SEMANTICS.FX_IMPOSTER] != null && item.DistanceToCameraSq >= Keystone.FX.FXImposters.StartDistanceSq)
            {

            	// TODO: and i suppose, why even bother when we have the VisibleItem list here?
            	//       The main thing is when we need to do seperate cull passes for seperate cameras such as for
            	//       water reflection pass.  In that case, i think we should simply instance another Culler() and allow it to create
            	//       a seperate Visibile Items list.

            }
            //// instanced entities must have geometry that has only one group
            //else if (model.UseInstancing)
            //{
            //    // TODO: This entire if/else if should be moved within the current RegionPVS and then
            //    // we can sort using same scheme as always.
            //    // note: Instancing not compatible with Actors 
            //    Mesh3d mesh = (Mesh3d)model.Geometry;
            //    if (mesh.Minimesh != null)
            //    {
            //        // TODO: performance test to see just how much extra work culling is without an octree
            //        // TODO: i might just have entity.Model.SelectGeometry() include an 'out' param for the selected geometry?
            //        //         todo if a model has multiple LOD's
            //        //         for internal geometry that ruins that.  Might be best to have a new class instead of ModelBase called
            //        //         InstancedModel that cannot be
            //        //         hierarchical NOR contain any LOD and perhaps also enforce only a tvmesh with a single group in it.
            //        //  Although wouldn't it be nice to also be able to render LOD's this way seemlessly?  One thing perhaps that could be done is
            //        //  when/if a Model is added to the Instancer, that this model can also contain seperate Minimeshes for each
            //        //  Then in our Instancer, we dont have a _minimeshes dictionary, but a Model dictionary and during our Render
            //        // we not only recurse every model, but in each model, every _minimesh if it's .InstanceCount > 0

            //        // April.27.2011 - Minimesh is now a type of rendering system that gets referenced
            //        // by the Geometry.  MInimesh itself is now no longer a Geometry node. 
            //        // This is the most elegant implementation i've come up with. 
            //        // TODO: there are two fundamental issues here however...
            //        // 1) the lighting information is not associated with the minimesh instances.
            //        //    - ideally, light tests that are found per region, 
            //        // 2) the individual appearances are not associated.
            //        //    - ideally within .AddInstanceData the appearance data is used to sort them
            //        // 3) each minimesh instance needs to be per region as well!  it suggests that
            //        //   our RegionPVS itself should host seperate buckets!
            //        mesh.Minimesh.AddInstanceData(entity, model, item.CameraSpacePosition);

            //        // TODO: older note (note's obsolete?) the fact i have FXData[] here says that any entity that wants to be instanced has to subscribe first.
            //        //         and im still not resolved on best approach if not subscribing and simply being able to derive the minimesh to use
            //        //         based on the underlying Model and thus Mesh3d used by the current Entity.  Then as a world editing process
            //        //         you can either use menu options to generate fields, trees, grass which automatically creates the minimesh binding
            //        //         Or you can add them one by one by Adding from the toolbox any existing minimeshes.   So then all are re-using
            //        //         an existing FXInstancer.GetModel("modelName") and that model has a reference to the specific
            //        //         Minimesh in the Instancer since it was added to or loaded by the FXInstancer.  So actually here i think
            //        //         when we add a Minimesh or load a minimesh, we should be loading the Mesh3d and then
            //        //         calling Minimesh.Create(mesh3d)
            //        // TODO: 2) how do you reconcile when / if an Instanced model should be Imposter-ized instead?
            //        //         perhaps the rule isnt so complicated.  If you have a field of _same_ stuff then minimesh makes sense
            //        //         but for disimilar stuff, at a certain distance away from camera, imposters are better?  For LOD trees
            //        //         and for 2d grass, minimeshes are superior because its basically the best of both worlds.  You have a static
            //        //         texture, only 4 quads, and you're only updating position, scale and rotation.  The only advantage Imposter
            //        //         would have in this context is it could do more per single call whereas I think Sylvain batches only 20 or so
            //        //         whereas an imposter draw call depending on the size of our texture, can do hundreds.  But the downside of course
            //        //         is that imposters do require an actual render be done periodically.  There's some optimizations we could do here
            //        //         where if an LOD is already a billboard, then we need simply make one initial render and then only  have to update it
            //        //         on a limited basis when distance changes if we need higher/lower resolution
            //        // TODO: 3). i have the "entity" as requiring the Iinstancer here and not the Model itself
            //        // you would think that this would be a model specific action and really have nothing to do with the entity itself...
            //        // same with FXImposter i think, but for imposter system to work properly it needs to be subscribed to ahead of time
            //        // because imposters re-use their portion of the underlying rendersurface and the static render is based
            //        // on the entity's location and rotation and it's not just the same for every model.
            //    }
            //}
            else
            {
                BucketMasks mask = BucketMasks.None;
                if (item.Model.Geometry == null || item.Model.Geometry.PageStatus != Keystone.IO.PageableNodeStatus.Loaded) 
                	return;

                //if (item.Entity.ID == "motionfield_")
                //	System.Diagnostics.Debug.WriteLine ("test");
                

                
                if (item.Model.Geometry as TexturedQuad2D != null)
                {
                    mask |= BucketMasks.Item2D;

                    Controls.Control2D guiControl = (Controls.Control2D)item.Entity;
                    TexturedQuad2D quad = (TexturedQuad2D)item.Model.Geometry;

                    // TODO: being able to cache these quad objects would be nice... similar to what we do with Proxy's
                    if (guiControl.ZOrder < 0) return; // object is behind camera
                    Keystone.Immediate_2D.Renderable2DTexturedQuad item2d = new Keystone.Immediate_2D.Renderable2DTexturedQuad(item.Model.Appearance.Layers[0].Texture,
                        guiControl.CenterX,
                        guiControl.CenterY,
                        guiControl.Width, guiControl.Height, quad.Angle, true);

                    int color = Color.White.ToInt32();
                    if (item.Model.Appearance.Material != null)
                        color = item.Model.Appearance.Material.Diffuse.ToInt32();

                    item2d.Color1 = color; // quad.mColor1;
                    item2d.Color2 = color; // quad.mColor2;
                    item2d.Color3 = color; // quad.mColor3;
                    item2d.Color4 = color; // quad.mColor4;

                    Add(item2d, false);
                }
                else if (item.Model.Geometry is BillboardText)
                {
                	mask |= BucketMasks.Item2D; 
                	mask |= BucketMasks.Text;
                	mask |= BucketMasks.SmallFrustum;
                	
                	AddToBucket(item, mask);
                	
                }
                else if (item.Model.Geometry is LinesGeometry3D)
                {
                    mask |= BucketMasks.Item2D;
                    LinesGeometry3D linesGeometry = (LinesGeometry3D)item.Model.Geometry;

                    // as a geoemtry node, linesGeometry intenal line points are always local space.  Thus we need to
                    // provide item.CameraSpacePosition which will also have include region matrix since the geometry
                    // is attached to an entity and so is like a Mesh3d 
                    Keystone.Immediate_2D.Renderable3DLines lines3d = new Renderable3DLines(linesGeometry.LineList, linesGeometry.Colors, item.CameraSpacePosition);

                    Add(lines3d, true);
                }
                else
                {

                    mask |= BucketMasks.Item3D;

                    if (item.Entity.GetEntityFlagValue ("laterender") == true)
                		mask |= BucketMasks.LateGeometry;
                                    
                    // TODO: the distance to surface works for a spherical planet, but not for a very large billboard
                    // like a corona
                    
                    double distanceToSurface = item.DistanceToCameraSq;
                    if (model.BoundingBox != BoundingBox.Initialized())
                    	distanceToSurface = item.DistanceToCameraSq - model.BoundingBox.RadiusSquared;
                    //if (distanceToSurface < mContext.Near2 * mContext.Near2)
                    //    Debug.WriteLine("near");
                    bool addToLargeFrustum = (entity.MaxVisibleDistanceSquared > mDistanceToSmallFarPlaneSquared) &&
					                         (distanceToSurface >= mDistanceToLargeNearPlaneSquared) &&
					                         (item.DistanceToCameraSq > mDistanceToSmallFarPlaneSquared);

                    if (addToLargeFrustum)
                        mask |= BucketMasks.LargeFrustum;
                    else
                        mask |= BucketMasks.SmallFrustum;


                    if (item.Entity.Overlay)
                        mask |= BucketMasks.Overlay;

                    if (item.Entity is Background3D)
                        mask |= BucketMasks.Background;

                    AddToBucket(item, mask);
                }
            }
        }


        private void AddToBucket(VisibleItem item, BucketMasks mask)
        {
            // why should there ever be a NON ModeledEntity?
            System.Diagnostics.Debug.Assert(item.Entity != null);

            System.Diagnostics.Debug.Assert(item.Entity is ModeledEntity ||
                                            item.Entity is Keystone.Portals.Interior); 
            // no item without Geometry should make it here
            System.Diagnostics.Debug.Assert(item.Model != null && item.Model.Geometry != null);

            bool frontToBackSorting = true; // default is to render close items first to help with fillrate

            if (item.Entity is ModeledEntity || item.Entity is Keystone.Portals.Interior)
            {
                Model model = item.Model;
                Geometry geometry = model.Geometry;
                Appearance.Appearance appearance = item.Model.Appearance;
               

                // TODO: with these flags, when are ModelSelector geometry switch types even needed? 
                // are they now made obsolete by using model flags instead?
// Sept.8.2017 - If we want doors and windows on Walls, we need to replace that wall segment with a door or window modeled wall.
//               Because we can't seem to fix the bug with CSG targets disappearing when looking through them via the window of another CSG target
//               Also, our interior walls are not rendering anymore when using these stencil flags.
//                if (item.Model.GetFlagValue((uint)Model.ModelFlags.CSGStencilSource))
//                    mask |= BucketMasks.CSGStencil;
//                else if (item.Model.GetFlagValue((uint)Model.ModelFlags.CSGStencilTarget))
//                    mask |= BucketMasks.CSGTarget;

                // TODO: i forget why i commented out overlay as part of mask?
                //if (entity.Overlay == true)
                //    mask |= BucketMasks.Overlay;
                //else 
                if (model.BlendingMode != CONST_TV_BLENDINGMODE.TV_BLEND_NO)
                {
                    // alpha blended models rendered back to front
                    // TODO: but also i think in our Draw traverser, we may need to render all opaques
                    // first from BOTH large and default items, and then go back over and do alpha blended
                    // but unfortunately, that's not really possible with the zbuffer changing...
                    mask |= BucketMasks.AlphaBlending;
                    // alpha blending works best back to front
                    frontToBackSorting = false;
                }

                keymath.DataStructures.SingleLinkedList<VisibleItem> bucket;
                lock (mBucketLock)
                {
                    // does the bucket already exist for this mask or do we have to create it?
                    bool success = mBuckets.TryGetValue(mask, out bucket);
                    //System.Diagnostics.Trace.WriteLine(model.Name);
                    if (success == false)
                    {
                        using (CoreClient._CoreClient.Profiler.HookUp("Bucket Creation"))
                        {
                        	bucket = new keymath.DataStructures.SingleLinkedList<VisibleItem>();   
	                        mBuckets.Add(mask, bucket);
                        }
                    }
                }

                IComparer<VisibleItem> comparer;
                if (frontToBackSorting) 
                    comparer = mFrontToBackComparer;
                else 
                    comparer = mBackToFrontComparer;
                // Dec.4.2012 - Light tests and assignments now done in ScaleDrawer.Render() at beginning of method
 //               LightTest(item);
 				using (CoreClient._CoreClient.Profiler.HookUp("Bucket Insertion"))
 					// NOTE: if BucketInsertion is slow, it's usually because appearance.GetHashCode() is called ComputeHashCode()
 					// every frame unnecessarily because a flag is either getting set every frame or never cleared
	                bucket.Add(item, comparer);
                //bucket.Add(item);
            }
        }

        
//        public void Add(VisibleItemInfo item)
//        {
//#if LOGZ
//            addAsLargeItem = false;
//#endif
//            ModeledEntity entity = (ModeledEntity)item.Entity;
//            Model model = item.Model;

//            // TODO: maybe we want to add to imposter same way we add to
//            // minimesh via AddInstance() ?  The Imposter system should be a type of renderer
//            // not an FX.
//            if (entity.FXData[(int)Keystone.FX.FX_SEMANTICS.FX_IMPOSTER] != null && item.DistanceToCameraSq >= Keystone.FX.FXImposters.StartDistanceSq)
//            {
//                entity.InFrustum = true;
//            }
//            // instanced entities must have geometry that has only one group
//            else if (model.UseInstancing)
//            {
//                // TODO: This entire if/else if should be moved within the current RegionPVS and then
//                // we can sort using same scheme as always.
//                // note: Instancing not compatible with Actors 
//                Mesh3d mesh = (Mesh3d)model.Geometry;
//                if (mesh.Minimesh != null)
//                {
//                    // TODO: performance test to see just how much extra work culling is without an octree
//                    // TODO: i might just have entity.Model.SelectGeometry() include an 'out' param for the selected geometry?
//                    //         todo if a model has multiple LOD's
//                    //         for internal geometry that ruins that.  Might be best to have a new class instead of ModelBase called
//                    //         InstancedModel that cannot be
//                    //         hierarchical NOR contain any LOD and perhaps also enforce only a tvmesh with a single group in it.
//                    //  Although wouldn't it be nice to also be able to render LOD's this way seemlessly?  One thing perhaps that could be done is
//                    //  when/if a Model is added to the Instancer, that this model can also contain seperate Minimeshes for each
//                    //  Then in our Instancer, we dont have a _minimeshes dictionary, but a Model dictionary and during our Render
//                    // we not only recurse every model, but in each model, every _minimesh if it's .InstanceCount > 0

//                    // April.27.2011 - Minimesh is now a type of rendering system that gets referenced
//                    // by the Geometry.  MInimesh itself is now no longer a Geometry node. 
//                    // This is the most elegant implementation i've come up with. 
//                    // TODO: there are two fundamental issues here however...
//                    // 1) the lighting information is not associated with the minimesh instances.
//                    //    - ideally, light tests that are found per region, 
//                    // 2) the individual appearances are not associated.
//                    //    - ideally within .AddInstanceData the appearance data is used to sort them
//                    // 3) each minimesh instance needs to be per region as well!  it suggests that
//                    //   our RegionPVS itself should host seperate buckets!
//                    mesh.Minimesh.AddInstanceData(entity, model, item.CameraSpacePosition);

//                    // TODO: older note (note's obsolete?) the fact i have FXData[] here says that any entity that wants to be instanced has to subscribe first.
//                    //         and im still not resolved on best approach if not subscribing and simply being able to derive the minimesh to use
//                    //         based on the underlying Model and thus Mesh3d used by the current Entity.  Then as a world editing process
//                    //         you can either use menu options to generate fields, trees, grass which automatically creates the minimesh binding
//                    //         Or you can add them one by one by Adding from the toolbox any existing minimeshes.   So then all are re-using
//                    //         an existing FXInstancer.GetModel("modelName") and that model has a reference to the specific
//                    //         Minimesh in the Instancer since it was added to or loaded by the FXInstancer.  So actually here i think
//                    //         when we add a Minimesh or load a minimesh, we should be loading the Mesh3d and then
//                    //         calling Minimesh.Create(mesh3d)
//                    // TODO: 2) how do you reconcile when / if an Instanced model should be Imposter-ized instead?
//                    //         perhaps the rule isnt so complicated.  If you have a field of _same_ stuff then minimesh makes sense
//                    //         but for disimilar stuff, at a certain distance away from camera, imposters are better?  For LOD trees
//                    //         and for 2d grass, minimeshes are superior because its basically the best of both worlds.  You have a static
//                    //         texture, only 4 quads, and you're only updating position, scale and rotation.  The only advantage Imposter
//                    //         would have in this context is it could do more per single call whereas I think Sylvain batches only 20 or so
//                    //         whereas an imposter draw call depending on the size of our texture, can do hundreds.  But the downside of course
//                    //         is that imposters do require an actual render be done periodically.  There's some optimizations we could do here
//                    //         where if an LOD is already a billboard, then we need simply make one initial render and then only  have to update it
//                    //         on a limited basis when distance changes if we need higher/lower resolution
//                    // TODO: 3). i have the "entity" as requiring the Iinstancer here and not the Model itself
//                    // you would think that this would be a model specific action and really have nothing to do with the entity itself...
//                    // same with FXImposter i think, but for imposter system to work properly it needs to be subscribed to ahead of time
//                    // because imposters re-use their portion of the underlying rendersurface and the static render is based
//                    // on the entity's location and rotation and it's not just the same for every model.
//                }
//            }
//            else
//            {

//                // TODO: the distance to surface works for a spherical planet, but not for a very large billboard
//                // like a corona
//                double distanceToSurface = item.DistanceToCameraSq - model.BoundingBox.RadiusSquared;
//                //if (distanceToSurface < mContext.Near2 * mContext.Near2)
//                //    Debug.WriteLine("near");
//                bool addToLargeFrustum = (entity.MaxVisibleDistanceSquared > mDistanceToSmallFarPlaneSquared) &&
//                    (distanceToSurface >= mDistanceToLargeNearPlaneSquared) &&
//                    (item.DistanceToCameraSq > mDistanceToSmallFarPlaneSquared);

//                //double value = System.Math.Sqrt (item.DistanceToCameraSq);
//                // Formatting strings--> http://idunno.org/archive/2004/07/14/122.aspx
//                //System.Diagnostics.Debug.WriteLine("Distance to camera = " + String.Format("{0:£#,##0.00;(£#,##0.00);Nothing}", value));

//                if (model.BlendingMode != CONST_TV_BLENDINGMODE.TV_BLEND_NO)
//                    // alpha blended models rendered back to front
//                    // TODO: but also i think in our Draw traverser, we may need to render all opaques
//                    // first from BOTH large and default items, and then go back over and do alpha blended
//                    // but unfortunately, that's not really possible with the zbuffer changing...
//                    this.Add(item, mBackToFrontComparer, addToLargeFrustum);
//                else
//                    this.Add(item, mFrontToBackComparer, addToLargeFrustum);
//            }
//        }

        // TODO: performance check eventually with and without comparer
        //private void Add(VisibleItemInfo item, IDistanceComparer comparer, bool addToFarFrustum)
        //{
        //    System.Diagnostics.Debug.Assert(item.Entity != null);
        //    System.Diagnostics.Debug.Assert(item.Entity is ModeledEntity); // why should there ever be a NON ModeledEntity?
        //    // no itemw without Geometry should make it here
        //    System.Diagnostics.Debug.Assert(item.Model != null && item.Model.Geometry != null);
            
        //    // do we add to large, default, alpha or overlay?
        //    if (item.Entity is ModeledEntity)
        //    {
        //        ModeledEntity entity = (ModeledEntity)item.Entity;
        //        Model model = item.Model;
        //        Geometry geometry = model.Geometry;
        //        Appearance.Appearance appearance = item.Model.Appearance;

        //        // TODO: the model should have the CSGSource and CSGTarget info, not the Entity right?
        //        // so these flags need to be moved to the model and then these added to a ModelSelector
        //        // were the selector can choose between stencil sour
        //        // TODO: our scripts must set these flags on the models... this assumes the script
        //        // is loaded after the model though!  otherwise maybe our scripts dont have to set it
        //        // we have to set it in the saved prefab for the relevant models under a selector.
        //        // TODO: with these flags, when are ModelSelector geometry switch types even needed? 
        //        // are they now made obsolete by using model flags instead?
        //        bool isCSGSource = item.Entity.GetFlagValue((uint)Entity.EntityFlags.CSGStencilSource);
        //        bool isCSGTarget = item.Entity.GetFlagValue((uint)Entity.EntityFlags.CSGStencilTarget);


        //        if (entity.Overlay == true)
        //        {
        //            if (addToFarFrustum)
        //                mItemsOverlay[1].Add(item);
        //            else
        //                mItemsOverlay[0].Add(item);
        //        }
        //        else if (model.BlendingMode != CONST_TV_BLENDINGMODE.TV_BLEND_NO)
        //        {
        //            if (addToFarFrustum)
        //                mItemsAlpha[1].Add(item);
        //            else
        //                mItemsAlpha[0].Add(item);

        //            LightTest(item);
        //        }
        //        else
        //        {

        //            // IMPORTANT: Minimeshes never even get added to either CSGPunches or CSGTargets
        //            // TODO:      THus if we try to "UseInstancing" we will never get a chance to add 
        //            //            the minimesh elements as punches or targets.  Ideally it'd be nice
        //            //        if we didnt need a seperate "Targets" array... that is really frustrating
        //            //
        //            if (isCSGSource)
        //            {
        //                // System.Diagnostics.Debug.WriteLine("CSG Punch");
        //                if (addToFarFrustum)
        //                    mItemsCSGTPunches[1].Add(item);
        //                else
        //                    mItemsCSGTPunches[0].Add(item);

        //            }
        //            else if (isCSGTarget)
        //            {
        //                // System.Diagnostics.Debug.WriteLine("CSG Target");
        //                if (addToFarFrustum)
        //                    mItemsCSGTargets[1].Add(item);
        //                else
        //                    mItemsCSGTargets[0].Add(item);

        //                LightTest(item);
        //            }
        //            else
        //            {
        //                if (addToFarFrustum)
        //                    mItemsGeneral[1].Add(item);
        //                else
        //                    mItemsGeneral[0].Add(item);
    
        //                LightTest(item);
        //            }
        //        }
        //    }

        //    // note: CelledRegion is never added as a visibleItem but it's ok
        //    // as it's not necessary for the drawing of markers
        //    if (item.Entity is Vehicles.Vehicle)
        //        DrawMarkers(item.CameraSpacePosition);


        //    if (_context.ShowEntityLabels)
        //    {
        //        // text is scaled by distance to camera, not size of planet
        //        float scale = (float)_context.GetConstantScreenSpaceScale(item.CameraSpacePosition, .02f);
        //        double yOffset = item.Entity.Height * .5;
        //        Vector3d lablePosition = item.CameraSpacePosition;
        //        // lablePosition.y -= yOffset;

        //        Vector3d screenPos = _context.Viewport.Project(lablePosition);
        //        if (screenPos.z < 0) return;

        //        double distanceKM = Math.Sqrt(item.DistanceToCameraSq) / 1000f;
        //        if (string.IsNullOrEmpty(item.Entity.Name) == false)
        //        {
        //            string lableText = item.Entity.Name + string.Format(" {0:#,##,###,###.##} KM", distanceKM);
        //            //Renderable3DText text = new Renderable3DText(lableText,
        //            //    lablePosition.x, lablePosition.y, lablePosition.z,
        //            //    CONST_TV_COLORKEY.TV_COLORKEY_CYAN,
        //            //    _context.TextureFontID, scale, scale);

        //            // NOTE: This 2DText is much better looking than the 3d text!
        //            Renderable2DText text = new Renderable2DText(lableText, (int)screenPos.x, (int)screenPos.y, CONST_TV_COLORKEY.TV_COLORKEY_YELLOW, _context.TextureFontID);
        //            this.Add(text, true, false); //addToFarFrustum);
        //        }
        //    }
        //}
        
        #region 3D Line Primitives use BucketMasks.Item2D | LinePrimitives
        public void Add(Vector3d[] points, CONST_TV_COLORKEY color, bool addToFarFrustum)
        {
            BucketMasks mask = BucketMasks.Item2D | BucketMasks.LinePrimitives;

            if (addToFarFrustum)
                mask |= BucketMasks.LargeFrustum;
            else
                mask |= BucketMasks.SmallFrustum;

            keymath.DataStructures.SingleLinkedList<IRenderable2DItem> bucket;
            lock (mBucketLock)
            {
                bool success = m2DBuckets.TryGetValue(mask, out bucket);
                if (success == false)
                {
                    bucket = new keymath.DataStructures.SingleLinkedList<IRenderable2DItem>();
                    m2DBuckets.Add(mask, bucket);
                }
            }
            IComparer<VisibleItem> comparer = mBackToFrontComparer;
            //bucket.Add(item, comparer);
            bucket.Add(new Renderable2DPolygon(points, (int)color));
        }


        public void Add(Line3d[] lines, CONST_TV_COLORKEY color, bool addToFarFrustum)
        {
            BucketMasks mask = BucketMasks.Item2D | BucketMasks.LinePrimitives;

            if (addToFarFrustum)
                mask |= BucketMasks.LargeFrustum;
            else
                mask |= BucketMasks.SmallFrustum;

            keymath.DataStructures.SingleLinkedList<IRenderable2DItem> bucket;
            lock (mBucketLock)
            {
                bool success = m2DBuckets.TryGetValue(mask, out bucket);
                if (success == false)
                {
                    bucket = new keymath.DataStructures.SingleLinkedList<IRenderable2DItem>();
                    m2DBuckets.Add(mask, bucket);
                }
            }
            IComparer<VisibleItem> comparer = mBackToFrontComparer;
            //bucket.Add(item, comparer);
            bucket.Add(new Renderable3DLines(lines, (int)color));
        }

        public void Add(BoundingBox box, CONST_TV_COLORKEY color, bool addToFarFrustum)
        {
            this.Add(BoundingBox.GetEdges(box), color, addToFarFrustum);
        }
        #endregion 

        /// <summary>
        /// TODO: note that here  the items must be passed in, in camera relative position
        /// </summary>
        public void Add(IRenderable2DItem[] items, bool addToFarFrustum)
        {
            if (items != null && items.Length > 0)
                for (int i = 0; i < items.Length; i++)
                    Add(items[i], addToFarFrustum);
               
        }

        public void Add(string text, int left, int top, int color, bool addToFarFrustum)
        {
            Add(new Renderable2DText(text, left, top, color, _context.TextureFontID), addToFarFrustum);
        }

        public void Add(IRenderable2DItem item, bool addToFarFrustum)
        {
            BucketMasks mask = BucketMasks.Item2D;
            if (item is Renderable3DLines || item is Renderable2DPolygon)
                mask |= BucketMasks.LinePrimitives;


            if (addToFarFrustum)
                mask |= BucketMasks.LargeFrustum;
            else
                mask |= BucketMasks.SmallFrustum;

            if (item is Renderable2DText || item is Renderable3DText)
                mask |= BucketMasks.Text;

            if (item is Renderable2DTexturedQuad) // this is 2D drawn on near plane
                mask |= BucketMasks.Textured2DPrimitive;

            if (item.AlphaBlend)
                mask |= BucketMasks.AlphaBlending;

            keymath.DataStructures.SingleLinkedList<IRenderable2DItem> bucket;
            lock (mBucketLock)
            {
                bool success = m2DBuckets.TryGetValue(mask, out bucket);
                if (success == false)
                {
                    bucket = new keymath.DataStructures.SingleLinkedList<IRenderable2DItem>();
                    m2DBuckets.Add(mask, bucket);
                }
            }
            IComparer<VisibleItem> comparer = mBackToFrontComparer;
            //bucket.Add(item, comparer);
            bucket.Add(item);
        }

        internal void ClearInFrustumFlag()
        {
        	foreach (keymath.DataStructures.SingleLinkedList<VisibleItem> bucket in mBuckets.Values)
        	{
        		int numItemsInBucket = bucket.Count;
        		
        		for (int i = 0; i < numItemsInBucket; i++)
                {
                    VisibleItem item = bucket[i];
        		
                    item.Entity.InFrustum = false;
        		}
        	}
        }
        
        #region DRAW
        private struct LightPositionAndDirection
        {
            public Vector3d Position;
            public Vector3d Direction;
        }

        internal void Draw(BucketMasks mask, double elapsedSeconds, FX.FX_SEMANTICS fxSemantics)
        {
            if ((mask & BucketMasks.Item3D) == BucketMasks.Item3D)
            {
                // use mask to get the correct bucket
                keymath.DataStructures.SingleLinkedList<VisibleItem> bucket;

                using (CoreClient._CoreClient.Profiler.HookUp("RegionPVS.Draw.mBuckets.TryGetValue"))
                {
                    bool success = mBuckets.TryGetValue(mask, out bucket);
                    if (success == false || bucket == null || bucket.Count == 0) return;
                }

                int numItemsInBucket = bucket.Count;
                int instancedCount = 0;
                int count = 0;
                
                // TODO: maybe call a different Draw depending on the bucket type... but either way
                // i think we need a way to make IVisibleItemInfo coincide with IRenderableItems2d
                // TODO: depending on the mask, set up state, or we rely on caller to have already done so
                int lastAppearanceHashCode = -1;
                int lastLightHashCode = -1;

                Minimesh2 mini = null;
                // the following loop relies on the fact that items are added as follows
                // - lightingcode
                //      - geometry code
                //          - appearance code
                //               - instanced 
                //                  - if new instanced item
                //                      - render previous if applicable
                //                      - clear it's minimesh array
                //                      - add the instance
                //                      - update the appearance
                //                  - if same instanced item as previous
                //                      - add the instance
                //                    
                //               - not instanced
                //                  - distance
                //

                keymath.DataStructures.SingleLinkedList<SortableLightInfo> previousInfluentialLights = null;
                List<LightPositionAndDirection> movedLights; // light index, and original position and direction 

                for (int i = 0; i < numItemsInBucket; i++)
                {
                    VisibleItem item = bucket[i];
                    //System.Diagnostics.Trace.WriteLine("BEGIN render bucket item " + item.Entity.ID);
                    //if (item.Model.Geometry is MinimeshGeometry)
                    //    System.Diagnostics.Debug.WriteLine("MinimeshGeometry");
                    if (item.Model.Geometry == null) continue;
                    if (fxSemantics == FX.FX_SEMANTICS.FX_SHADOW_DEPTH_PASS && item.Model.CastShadow == false) continue;

                    // TODO: if we're in SHADOW_DEPTH_PASS, skip all models that are neither
                    // casting or receiving shadows
                    //if (item.Model.CastShadow == false && item.Model.ReceiveShadow == false) continue;

                    count++;

                    // For each item, disable all lights and we will enable only the relevant ones
                    Core._CoreClient.SceneManager[0].DisableAllLights();

            
                    if (item.InfluentialLights != null && item.InfluentialLights.Count > 0)
                    {
                        movedLights = new List<LightPositionAndDirection>(item.InfluentialLights.Count);

                        for (int j = 0; j < item.InfluentialLights.Count; j++)
                        {
                            SortableLightInfo sortable = item.InfluentialLights[j];
                            sortable.LightInfo.Light.Active = true;
                            // NOTE: recall that specular lighting gets disabled after drawing 2D so we need to make sure it's re-enabled for 3D.  This is a TV3D issue.
                            sortable.LightInfo.Light.SpecularLightingEnabled = true; // TODO: this should grab the setting from LIVE SETTINGS
                            // todo: I'M PRETTY SURE THIS is our elusive FLICKERING LIGHT bug!
                            // we need to add each lightInfo struct
                            // to a list so we can reposition them back 
                            // to original position 
                            LightPositionAndDirection original;
                            original.Position = sortable.LightInfo.CameraSpacePosition;
                            original.Direction = sortable.LightInfo.Direction;
                            movedLights.Add(original);

                            //System.Diagnostics.Trace.WriteLine("Light " + lightInfo.LightInfo.Light.TVIndex.ToString() + " ENABLED.");
                            using (CoreClient._CoreClient.Profiler.HookUp("RegionPVS.Draw.MoveTVLightToCameraSpace"))
                            {
								MoveTVLightToCameraSpace(sortable.LightInfo);

                                DirectionalLight dl = sortable.LightInfo.Light as DirectionalLight;
                                if (dl != null)
                                {
									Vector3d origLightDir = dl.Direction;
									
                                    if (dl.IsBillboard)
                                    {

                                        //TODO: sphere and disk need to be unit radius geometry
                                        //     
                                        Model model = item.Model;
                                        Entity entity = item.Entity;
                                        Appearance appearance = model.Appearance;
                                        
                                        if (model.UseModelSpaceDirectionalLightDirection && appearance.Shader != null) // TODO: add this propery and ability to set it via GUI?
                                        {
                                            Shader shader = appearance.Shader;

                                            // the light here is in cameraspace already, however for the ring2.fx shader to be able to use it
                                            // properly to calculate where the World's sphere casts a shadow on the ring mesh, it needs to
                                            // perform it's calculations in MODEL SPACE because our worlds and rings are at real-life scales 
                                            // and there's not enough precision to do that with floats in a shader.  So we use Model Space to 
                                            // determine where a shadow is cast on the ring by the sphere, and to do that we translate the 
                                            // light position by the Model's -cameraSpacePosition so that the light is now in model relative space

											// We use DerivedTranslation whenever we want the region relative position of something 
                                            Vector3d lightDir = Vector3d.Normalize(dl.DerivedTranslation - item.CameraSpacePosition);

                                            // TODO: the shader itself needs to be updated and needs the following var added to it
                                            shader.SetShaderParameterVector3("LightDirection_ModelSpace", lightDir);

                                            // NOTE: We do NOT need to actually move the light itself... so there is no need to
                                            // worry about re-positioning the light to it's previous position.

                                            // TODO: is there a bug with us moving directional lights around and NOT putting them
                                            // back in the correct position?!  Is this the source of that long-time light flicker bug
                                            // we've been having?  It really shouldn't be i don't think because don't we always
                                            // update the light to the calculated "cameraspace" position prior to rendering a mesh
                                            // that is lit by it?
                                            // Is there an issue here somehow though we are overlooking?


											// WARNING: we need to assign all shader parameters here
											// since Draw occurs next for this specific model instance
											

                                        // NOTE: This requires that the directional light's camera space position is up to date.
                                        //       Normally a directionallight does not need a position, but it does if we intend
                                        //       to simulate a pointlight through billboard rotation towards current renderable item
                                        //       because the Direction of the light must change for each renderable item.
                                        //if (item.Entity is Vehicles.Vehicle)
                                        //    System.Diagnostics.Debug.WriteLine("RegionPVS.Draw() - item is Vehicle");
                                        dl.Direction = item.CameraSpacePosition - sortable.LightInfo.CameraSpacePosition;
#if DEBUG
                                        if (dl.Direction == Vector3d.Zero())
                                            dl.Direction = -Vector3d.Up();
#endif
                                    }
                                }
                            }
                        }
                    }
                    previousInfluentialLights = item.InfluentialLights;

                    //if (item.LightHashCode != lastLightHashCode)
                    //{
                    //    // update the lights which are enabled or disabled
                    //}
                    //if (item.AppearanceHashCode != lastAppearanceHashCode)
                    //{
                    //    // if instancing, render the current batch and now update appearance and start
                    //  
                    //}
                    // TODO: FX_IMPOSTER code snippet below used to be in ScaleCuller but having switched to
                    //       RegionPVS for determination of how to render things, never was re-enabled.
                    // if (entity.FXData[(int)FX_SEMANTICS.FX_IMPOSTER] != null && vi.DistanceToCameraSq >= FXImposters.StartDistanceSq)
                    // {
                    //     entity.InFrustum = true;
                    //}

                    bool instanced = item.Model.UseInstancing;
                    //instanced = false; // force disable of instancing to compare frame rates
                    //System.Diagnostics.Debug.WriteLine("geometry = " + item.Model.Geometry.ID);
                    if (instanced) // check for null because when first converting to an instanced Minimesh, this property will be null for a short interval
                    {

                        // TODO: if this is not first rendering pass on something like PSSM, then we shouldn't have to
                        //       assign minimesh instances again.  I do need to add profiling code here to see what the biggest slow downs are
                        //       maybe i can use pointers here to work with unmanaged arrays to speed things up
                        Keystone.Elements.Minimesh2 tmp = ((Mesh3d)item.Model.Geometry).Minimesh;

                        if (tmp == null)
                        {
                            // we need to create this mini if it hasn't been created yet
                            CoreClient._CoreClient.InstanceRenderer.CreateMinimesh((Mesh3d)item.Model.Geometry, item.Model.Appearance, 1000);
                            tmp = ((Mesh3d)item.Model.Geometry).Minimesh;
                        }
                        instancedCount++;
                        if (tmp == mini)
                        {
                            using (CoreClient._CoreClient.Profiler.HookUp("MMAddInstance"))
                                mini.AddInstanceData((ModeledEntity)item.Entity, item.Model, item.CameraSpacePosition);
                        }
                        else
                        {
                            if (mini != null)
                            {
                                using (CoreClient._CoreClient.Profiler.HookUp("MMRender"))
                                    mini.Render();

                                using (CoreClient._CoreClient.Profiler.HookUp("MMClear"))
                                    mini.Clear();
                            }
                            mini = ((Mesh3d)item.Model.Geometry).Minimesh;
                            // TODO: when using our star lights, our asteroids minimeshes will be unlit
                            // if we dont set a custom shader to these asteroid minimeshes.
                            using (CoreClient._CoreClient.Profiler.HookUp("MMAddInstance"))
                                mini.AddInstanceData((ModeledEntity)item.Entity, item.Model, item.CameraSpacePosition);
                        }

                        if (count == numItemsInBucket)
                        {
                            // last item in this bucket so render out any remaining minimesh instances
                            using (CoreClient._CoreClient.Profiler.HookUp("MMRender"))
                                mini.Render();
                            using (CoreClient._CoreClient.Profiler.HookUp("MMClear"))
                                mini.Clear();
                        }
                    }
                    else if (mini != null)
                    {
                        // render outstanding minis accrued up to this point
                        using (CoreClient._CoreClient.Profiler.HookUp("MMRender"))
                            mini.Render();
                        using (CoreClient._CoreClient.Profiler.HookUp("MMClear"))
                            mini.Clear();
                        mini = null;
                    }

                    // STILL HERE? THIS IS THE PRIMARY DRAW FOR NON MINI-MESH 3D ITEMS
                    if (mini == null)
                        item.Draw(_context, elapsedSeconds, fxSemantics);

                    //System.Diagnostics.Trace.WriteLine("END Render bucket item " + item.Entity.ID);


                    // Move the INTERNAL Light position and direction back to previous values that existed at start of this loop 
                    if (previousInfluentialLights != null && previousInfluentialLights.Count > 0)
                    {
                        for (int j = 0; j < previousInfluentialLights.Count; j++)
                        {
                            SortableLightInfo sortable = previousInfluentialLights[j];
                            // we don't need to disable them again right ->  what about re-enable though? -> sortable.LightInfo.Light.Active = false;

							// the following call DOES NOT MOVE the Light.Translation and thus does not require any Spatial Graph updates
							// or CHANGE_STATE flags to occur for boundingbox calcs, etc.  This is PURELY INTERNAL for DX9 rendering in this function.
            				// instead it calls against TVLightFactory thusly:
            				
							// todo: SetDirectionForRendering() not yet implemented
            				sortable.LightInfo.Light.SetCameraSpaceTranslationForRendering( movedLights[j].Position);
							// CoreClient._CoreClient.Light.SetLightPosition(_tvfactoryIndex, (float)movedLights[j].Position.x, (float)movedLights[j].Position.y, (float)movedLights[j].Position.z);
							sortable.LightInfo.Light.SetDirectionForRendering( movedLights[j].Direction);
							// CoreClient._CoreClient.Light.SetLightDirection(_tvfactoryIndex, (float)movedLights[j].Direction.x, (float)movedLights[j].Direction.y, (float)movedLights[j].Direction.z);

                        }
                    }
                }
                //System.Diagnostics.Debug.WriteLine ("instances count = " + instancedCount.ToString());

				// WARNING: i think the below comment is wrong, 
				//.         we DO need to restore origLightPosition and origLightDirection
				
                // NOTE: We do not have to restore light positions because we never ever permanently
                // set the camera space position. It is only used direct in the LightEngine.SetLightPosition() call.
                // and never stored in the Entity.Translation setter

            }
            else  // includes Elements\TexturedQuad2D.cs <- These do not need Lights to Render
            {
                keymath.DataStructures.SingleLinkedList<IRenderable2DItem> bucket2D;
                bool success = m2DBuckets.TryGetValue(mask, out bucket2D);
                
                if (success == true && bucket2D.Count > 0)
	                foreach (IRenderable2DItem item in bucket2D)
    	            {
    	                item.Draw();
    	            }
                
                // BillboardText will be in seperate bucket but has same masks, let's see if there are any
                keymath.DataStructures.SingleLinkedList<VisibleItem> bucket;

                success = mBuckets.TryGetValue(mask, out bucket);
                if (success == false || bucket == null || bucket.Count == 0) return;
                
                
                foreach (VisibleItem item in bucket)
                	item.Draw (_context, elapsedSeconds, fxSemantics);
            }
        }
        #endregion
    }
}


