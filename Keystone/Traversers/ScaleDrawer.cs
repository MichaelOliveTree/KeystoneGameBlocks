using System;
using System.Collections.Generic;
using Keystone.Cameras;
using Keystone.FX;
using Keystone.Types;
using MTV3D65;

namespace Keystone.Traversers
{
    // ARTICLE ON USING DX11 MULTITHREADED RENDERING TO TAKE ADVANTAGE OF SLI AND SUCH
    // http://www.rorydriscoll.com/2009/04/21/direct3d-11-multithreading/



    // TODO: This currently is NOT a traverser...  
    public class ScaleDrawer 
    {        
        // stores info about all lights that are visible in this frame
        // obsolete i think? private LightInfo[] mVisibleLightsInfoArray;

        public bool mUseScalingFactor = true;


        private bool _stateBlocksInitialized = false;
        private Microsoft.DirectX.Direct3D.StateBlock _default;
        private Microsoft.DirectX.Direct3D.StateBlock _targetsToDepthBufferOnly;
        private Microsoft.DirectX.Direct3D.StateBlock _stencilsOnly;
        private Microsoft.DirectX.Direct3D.StateBlock _targetsWithStencilBuffer;


        public ScaleDrawer() {}

        private void ClearInFrustumFlag (RegionPVS pvs)
        {
        	pvs.ClearInFrustumFlag();
        }
        
        public void Render (RenderingContext context, List<RegionPVS>regionPVSList, List<LightInfo> lightList, double elapsedSeconds)
        {
            if (regionPVSList == null || regionPVSList.Count == 0) return;
            if (context == null || context.Camera == null) return;

            // TODO: there is no need to convert this List<> to an Array!  
            if (lightList == null || lightList.Count == 0)
            	mVisibleLightsInfoArray = null;
            else
	            mVisibleLightsInfoArray = lightList.ToArray();

            // cache existing camera matrices so they can be restored since they will change
            // as they traverse from root RegionPVS to children
            Camera camera = context.Camera;
            Matrix prevInverseView = camera.InverseView;
            Matrix prevProjection = camera.Projection;

// TODO: for now since stencils are disabled, we disable state block capturing and initialization
//            if (_stateBlocksInitialized == false)
//                InitializeStateBlocks();

            context.ApplyState();

            // BEGIN STENCILS /////////////////////////////////////////////////////
            //            #region Stencil tests
            //            foreach (RegionPVS pvs in regionPVSList)
            //            {
            //                // http://cpntools.org/cpn2000/clipping_in_opengl
            //                // TODO: using minimeshes, i have to use a flag in the color field of our minimesh element
            //                //       indicating whether that minimesh element is a stencil target or not
            //                //       and read that in the shader and determine if the item should be skipped or not
            //                //       depending on whether we are rendering targets first to depth buffer
            //                // TODO: so what if we could track those wall segments that are csg targets
            //                //       as seperate models such that when rendering interior, we add them to the PVS seperately?
            //                //
            //                //
            //                // note; stencil tests in shader occurs between vertex and pixel functions and occurs
            //                //       with depth test so no extra cost there
            //                // http://xboxforums.create.msdn.com/forums/p/97636/582319.aspx
            //                // step 1 - draw the target using z only and no color writes
            //                // step 2 - draw stencil value > 0 wherever the stencil's zbuffer is lessthanequal to target pixels to create mask
            //                // step 3 - clear the zbuffer but not the stencil buffer
            //                // step 4 - draw the target only where stencil buffer value > 0
            //                // http://iloveshaders.blogspot.com/2011/05/how-depth-and-stencil-testing-work.html
            //                // BEGIN TARGETS ONLY TO DEPTH BUFFER W/ NO PIXEL OUTPUT
            //                _targetsToDepthBufferOnly.Apply();
            //
            //                // #### close CSGTargets ####
            //                DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.CSGTarget, 0, camera, elapsedSeconds);
            //                // END TARGETS ONLY TO DEPTH BUFFER W/ NO PIXEL OUTPUT
            //
            //
            //                // BEGIN STENCILS ONLY
            //                _stencilsOnly.Apply();
            //
            //                // #### close CSG Punches ####
            //                DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.CSGStencil, 0, camera, elapsedSeconds);
            //                
            //                //    // TODO: easy to test here to see if our camera is in the bounds
            //                //    // of a stencil.  if so, we record that value and then
            //                //    // if further down when rendering targets we discover that one of them
            //                //    // is a target to the stencil the camera is in, skip rendering the target
            //                //    // altogether.
            //
            //                //    // for each stencil, find the targets which are same distance away plus/minus 1 meter
            //                //    // and render those 
            //                //    // TODO: if this sort of filtering is necessary, maybe we should do it in culler
            //                //    // similar to how we do lighttests?
            //
            //                //    // clear stencil buffer and go to next stencil
            //
            //                // END STENCILS ONLY
            //                // clear zbuffer WITHOUT clearing stencil buffer
            //                CoreClient._CoreClient.D3DDevice.Clear(Microsoft.DirectX.Direct3D.ClearFlags.ZBuffer, 0, 1f, 0);
            //                // BEGIN TARGETS ONLY USING STENCIL BUFFER
            //                _targetsWithStencilBuffer.Apply();
            //                //Draw the CSGTargets that are to be clipped by the stencil 
            //                DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.CSGTarget, 0, camera, elapsedSeconds);
            //                DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.CSGTarget | BucketMasks.AlphaBlending, 0, camera, elapsedSeconds);
            //                // END TARGETS ONLY USING STENCIL BUFFER
            //
            //
            //                // disable stencil, return to normal rendering states and draw everything else
            //                _default.Apply();
            //                
            //                // so why are the door frames not rendered at least?  the walls i can understand
            //                // because when they are rendered again, they are blocked by ALL
            //                // stencils...
            //                //mSwitchOptions.SetSwitchMode(SwitchMode.Geometry);
            //                //foreach (ScaleCuller.VisibleItemInfo item in info._defaultItemsCSGPunches) // #### _defaultCSGSources ####
            //                //{
            //                //    // render any NON punch geometry with stencilings OFF
            //                //    Apply(item);
            //                //}
            //            }
            //            #endregion
            // END STENCILS /////////////////////////////////////////////////////


            // RENDER BACKGROUND FROM 
            if (context.VisibleBackground != null)
            {
                using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 1"))
                {
                    CoreClient._CoreClient.D3DDevice.RenderState.ZBufferWriteEnable = false;

                    // background that does not need alpha
                    DrawBucket(context.VisibleBackground, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.Background, 0, camera, elapsedSeconds);
                    DrawBucket(context.VisibleBackground, BucketMasks.LargeFrustum | BucketMasks.Item3D | BucketMasks.Background, 1, camera, elapsedSeconds);

                    // TODO: is there a way to use "background" as a bucket mask instead!?  Having seperate
                    //       regionPVS seems wrong here.  What we really want when rendering background is to
                    //       use the root regionPVS sure, but then to only render background items

                    // our starfield uses alpha and is added to it
                    DrawBucket(context.VisibleBackground, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.Background | BucketMasks.AlphaBlending, 0, camera, elapsedSeconds);
                    DrawBucket(context.VisibleBackground, BucketMasks.LargeFrustum | BucketMasks.Item3D | BucketMasks.Background | BucketMasks.AlphaBlending, 1, camera, elapsedSeconds);

                    CoreClient._CoreClient.D3DDevice.RenderState.ZBufferWriteEnable = true;
                }
            }


            // http://tulrich.com/geekstuff/log_depth_buffer.txt
            // http://blogs.xnainfo.com/post/Logarithmic-Depth-Buffer.aspx
            // http://outerra.blogspot.com/search/label/depth%20buffer
            // http://outerra.blogspot.com/2009/08/logarithmic-z-buffer.html
            // http://www.gamedev.net/blog/73/entry-2006307-tip-of-the-day-logarithmic-zbuffer-artifacts-fix/
            // http://communistgames.blogspot.com/2010/11/depth-bufferz-buffer-and-its-precision.html
            // output.Position.z = log(C*output.Position.z + 1) / log(C*Far + 1) * output.Position.w;
            //            CoreClient._CoreClient.D3DDevice.SetRenderState(Microsoft.DirectX.Direct3D.RenderStates.ZEnable, true);

            //-----------------------------------------------------------
            // RENDER LARGE FRUSTUM ITEMS FIRST FROM EVERY RegionPVS
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 2"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // large far items
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item3D, 1, camera, elapsedSeconds);
                }
            }
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 3"))
            {
                CoreClient._CoreClient.Screen2D.Action_Begin2D();
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // large line primitives drawn here so they work with alpha blending
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item2D | BucketMasks.LinePrimitives, 1, camera, elapsedSeconds);
                }
                CoreClient._CoreClient.Screen2D.Action_End2D();
            }
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 4"))
            {
                // NOTE: must re-enable specular (fog i think is disabled too) after 2d call
                // http://www.truevision3d.com/forums/bugs/resolved_actorrender_after_draw2daction_makes_specular_disappear-t17081.0.html
                CoreClient._CoreClient.D3DDevice.RenderState.SpecularEnable = true;
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // large far items with alpha
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item3D | BucketMasks.AlphaBlending, 1, camera, elapsedSeconds);
                }
            }
            // END - RENDER LARGE FRUSTUM ITEMS FIRST FROM EVERY RegionPVS///


            // clear the depth buffer after rendering far stuff (todo; or is this not necessary since we use painters algo eg. background->far->near)
            //context.ClearDepthBuffer();
            //CoreClient._CoreClient.D3DDevice.Clear(Microsoft.DirectX.Direct3D.ClearFlags.Stencil, 0, 0, 0);

            //-----------------------------------------------------------
            // BEGIN - RENDER SMALL FRUSTUM ITEMS FIRST FROM EVERY RegionPVS
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 5"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // small frustum general items
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D, 0, camera, elapsedSeconds);

                    // debug - render the punches so we can see them
                    // #### close CSG Punches ####
                    // DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.CSGStencil, 0, camera, elapsedSeconds);
                }
            }

            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 6"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // line primitives drawn here so they work with alpha blending
                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item2D | BucketMasks.LinePrimitives, 0, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();
                }
            }
            // NOTE: must re-enable specular (fog i think is disabled too) after 2d call
            // http://www.truevision3d.com/forums/bugs/resolved_actorrender_after_draw2daction_makes_specular_disappear-t17081.0.html
            CoreClient._CoreClient.D3DDevice.RenderState.SpecularEnable = true;

            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 7"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // small frustum general items with alpha
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.AlphaBlending, 0, camera, elapsedSeconds);
                }
            }
            // END - RENDER SMALL FRUSTUM ITEMS FIRST FROM EVERY RegionPVS

            //-----------------------------------------------------------
            // BEGIN - RENDER THE LATE RENDER 3D ITEMS LIKE WATER PATCHES
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 8"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.LateGeometry, 0, camera, elapsedSeconds);
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item3D | BucketMasks.LateGeometry, 1, camera, elapsedSeconds);
                }
            }
            // END - LATE RENDER

            //-----------------------------------------------------------
            // BEGIN - RENDER THE 2D PRIMITIVES WITHOUT ALPHA

            // TEMP HACK force disable alpha blending for health bars
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 9"))
            {
                CoreClient._CoreClient.Screen2D.Settings_SetAlphaBlending(true, MTV3D65.CONST_TV_BLENDINGMODE.TV_BLEND_NO);
                foreach (RegionPVS pvs in regionPVSList)
                {
                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item2D, 0, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();// NOTE: Begin/End for 2D must be seperate for each view+projection combination

                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item2D, 1, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();// NOTE: Begin/End for 2D must be seperate for each view+projection combination
                }
            }
            //END - RENDER THE 2D PRIMITIVES

            //-----------------------------------------------------------
            // BEGIN - RENDER THE 2D TEXTURED PRIMITIVES (NON ALPHA BLENDED)
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 10"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item2D | BucketMasks.Textured2DPrimitive, 0, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();// NOTE: Begin/End for 2D must be seperate for each view+projection combination

                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item2D | BucketMasks.Textured2DPrimitive, 1, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();// NOTE: Begin/End for 2D must be seperate for each view+projection combination
                }
            }
            //END - RENDER THE 2D TEXTURED PRIMITIVES
             
            //-----------------------------------------------------------
            // BEGIN - RENDER THE TEXTURED 2D PRIMITIVES THAT USE ALPHABLENDING
            int iOldAlphaRef = 0;
            bool bOldAlphaTest = false;
            bool bOldAlphaBlending = false;
            CONST_TV_BLENDEX oldSrcBlend = CONST_TV_BLENDEX.TV_BLENDEX_ZERO;
            CONST_TV_BLENDEX oldDestBlend = CONST_TV_BLENDEX.TV_BLENDEX_ZERO;

            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 11"))
            {
                CoreClient._CoreClient.Screen2D.Settings_GetAlphaTest(ref bOldAlphaTest, ref iOldAlphaRef);
                CoreClient._CoreClient.Screen2D.Settings_GetAlphaBlendingEx(ref bOldAlphaBlending, ref oldSrcBlend, ref oldDestBlend);

                CoreClient._CoreClient.Screen2D.Settings_SetAlphaTest(false, 0);
                CoreClient._CoreClient.Screen2D.Settings_SetAlphaBlending(true, MTV3D65.CONST_TV_BLENDINGMODE.TV_BLEND_ADD);

                foreach (RegionPVS pvs in regionPVSList)
                {
                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item2D | BucketMasks.AlphaBlending | BucketMasks.Textured2DPrimitive, 0, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();// NOTE: Begin/End for 2D must be seperate for each view+projection combination

                    CoreClient._CoreClient.Screen2D.Action_Begin2D();
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item2D | BucketMasks.AlphaBlending | BucketMasks.Textured2DPrimitive, 1, camera, elapsedSeconds);
                    CoreClient._CoreClient.Screen2D.Action_End2D();// NOTE: Begin/End for 2D must be seperate for each view+projection combination
                }

                // restore state
                CoreClient._CoreClient.Screen2D.Settings_SetAlphaTest(bOldAlphaTest, iOldAlphaRef);
                CoreClient._CoreClient.Screen2D.Settings_SetAlphaBlendingEx(bOldAlphaBlending, oldSrcBlend, oldDestBlend);
            }
            // END - RENDER THE TEXTURED 2D PRIMITIVES THAT USE ALPHABLENDING


            //-----------------------------------------------------------
            // BEGIN - RENDER THE 2D & 3D BILLBOARD TEXT
            // text doesn't work with multiple regions... i may have to restrict to 2d...
            // although i do have to figure out how to place planet labels regardless of how far
            // TODO: the issue im having is since we never clear the zbuffer (we cannot)
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 12"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    CoreClient._CoreClient.Text.Action_BeginText();
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item2D | BucketMasks.Text, 0, camera, elapsedSeconds);
                    CoreClient._CoreClient.Text.Action_EndText(); // NOTE: Begin/End for 2D must be seperate for each view+projection combination


                    CoreClient._CoreClient.Text.Action_BeginText();
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item2D | BucketMasks.Text, 1, camera, elapsedSeconds);
                    CoreClient._CoreClient.Text.Action_EndText(); // NOTE: Begin/End for 2D must be seperate for each view+projection combination
                }
            }


            // NOTE: must re-enable specular (fog i think is disabled too) after 2d call
            // http://www.truevision3d.com/forums/bugs/resolved_actorrender_after_draw2daction_makes_specular_disappear-t17081.0.html
            CoreClient._CoreClient.D3DDevice.RenderState.SpecularEnable = true;
            // END - RENDER THE 2D & 3D BILLBOARD TEXT

            //-----------------------------------------------------------
            // BEGIN - RENDER THE OVERLAYS
            // for overlays, disable zbuffer test no need to clear the depth buffer
            CoreClient._CoreClient.D3DDevice.SetRenderState(Microsoft.DirectX.Direct3D.RenderStates.ZEnable, false);
            //or is it  //Microsoft.DirectX.Direct3D.RenderStates.ZBufferFunction
            //or       Microsoft.DirectX.Direct3D.RenderStates.ZBufferWriteEnable

            // TODO: see this thread for why we need to do more sophisticated tracking of the order of things and when overlay is on/off
            // http://www.truevision3d.com/forums/tv3d_sdk_65/setoverlay_problems-t17616.0.html;msg120635#msg120635
            // render overlay items like editor axis indicators last
            // overlays with NO ALPHA
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 13"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D | BucketMasks.Overlay, 0, camera, elapsedSeconds);
                    DrawBucket(pvs, BucketMasks.LargeFrustum | BucketMasks.Item3D | BucketMasks.Overlay, 1, camera, elapsedSeconds);
                }
            }
            // overlays with alpha
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 14"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    DrawBucket(pvs, BucketMasks.Overlay | BucketMasks.AlphaBlending | BucketMasks.SmallFrustum | BucketMasks.Item3D, 0, camera, elapsedSeconds);
                    DrawBucket(pvs, BucketMasks.Overlay | BucketMasks.AlphaBlending | BucketMasks.LargeFrustum | BucketMasks.Item3D, 1, camera, elapsedSeconds);
                }
            }
            // END - RENDER THE OVERLAYS 


            // clear the InFrustum bool
            using (CoreClient._CoreClient.Profiler.HookUp("ScaleDrawer.Render() 15"))
            {
                foreach (RegionPVS pvs in regionPVSList)
                {
                    // large far items
                    ClearInFrustumFlag(pvs);
                }
            }
            // re-enable depth test
            CoreClient._CoreClient.D3DDevice.SetRenderState(Microsoft.DirectX.Direct3D.RenderStates.ZEnable, true);

            // restore our camera
            camera.InverseView = prevInverseView;
            camera.Projection = prevProjection;
        }


        private void DrawBucket(RegionPVS pvs, BucketMasks mask, int frustum, Camera camera, double elapsedSeconds)
        {
        	FX_SEMANTICS semantic = FX_SEMANTICS.FX_NONE;

            // very important to set the camera matrices such that we're in relative space of current pvs region
            if (mTempHackDepthPass == false) // <-- this does seem to help, TODO but it could mean that for each RegionPVS, we
            	                             //     need unique split view/projection matrices. This means that we should do this
            	                             //     so that the info is already in the RegionPVS stored as SplitVIewMatrices[] and SPlitProjections[]
            {                               
            	camera.InverseView = pvs.InverseView;
            	camera.Projection = pvs.FrustumInfo[frustum].Projection;
            }
            else
            {
            	// TODO: the camera passed in here isn't the one being used on the ShadowRS... so changing matrices here would have no affect
            	//       but for now everything works but... it's unclear whether things are rendering across zones? since we're not computing
            	//       seperate matrices for the other zones when they clearly have different offsets from the other zones
            	// instruct RegionPVS to only render those visible models that are casting shadows into depth map
            	semantic = FX_SEMANTICS.FX_SHADOW_DEPTH_PASS;
            }
            pvs.Draw(mask, elapsedSeconds, semantic);
        }

        private bool mTempHackDepthPass = false;
        public void RenderGeometryIntoDepthMap(Camera camera, List<RegionPVS>regionPVSList)
        {
        	if (regionPVSList == null || regionPVSList.Count == 0) return;
            if (camera == null) return;
            
			// NOTE: this method is called from within a RenderSurface.StartRender()/EndRender() 		
            mVisibleLightsInfoArray = null;

            mTempHackDepthPass = true;
            // only render small frustum 3d items with no alpha
            foreach (RegionPVS pvs in regionPVSList)
            {
            	// TODO: the camera matrices in each pvs here is wrong.  it needs to be the ones we computed
            	//       BUT, these matrices can vary with each different RegionPVS!  In our tests, we'll only have the one
            	//       region, but eventually we'll have more.
            	//       - seems best way for that is a second regionPVSList that now has our light-centric camera matrices to ue
            	
            	// temporarily need to assign shaders, set technique 
            	DrawBucket(pvs, BucketMasks.SmallFrustum | BucketMasks.Item3D, 0, camera, 0);
            }
            
            mTempHackDepthPass = false;
            // restore original matrices to camera
        }



        private void InitializeStateBlocks()
        {
            SetDefaultStateBlock();
            _default.Capture();

            SetStencilsStateBlock();
            SetTargetsStateBlock();
            SetTargetsWithStencilBufferStateBlock();
            _stateBlocksInitialized = true;
        }

        private void SetDefaultStateBlock()
        {
            CoreClient._CoreClient.D3DDevice.BeginStateBlock();
            
            // this is the list of states that we will capture so we know what to restore
            CoreClient._CoreClient.D3DDevice.RenderState.SpecularEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferWriteEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferFunction = Microsoft.DirectX.Direct3D.Compare.Less;
            CoreClient._CoreClient.D3DDevice.RenderState.BlendOperation = Microsoft.DirectX.Direct3D.BlendOperation.Add;
            CoreClient._CoreClient.D3DDevice.RenderState.AlphaBlendEnable = false;
            CoreClient._CoreClient.D3DDevice.RenderState.AlphaTestEnable = false;
            CoreClient._CoreClient.D3DDevice.RenderState.ReferenceAlpha = 0;
          
       //     CoreClient._CoreClient.D3DDevice.RenderState.ColorWriteEnable = 0;
            CoreClient._CoreClient.D3DDevice.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.CounterClockwise;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilFunction = Microsoft.DirectX.Direct3D.Compare.Never;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilEnable = false;
            CoreClient._CoreClient.D3DDevice.RenderState.ReferenceStencil = 0;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilMask = 0;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilPass = Microsoft.DirectX.Direct3D.StencilOperation.Replace;

            _default = CoreClient._CoreClient.D3DDevice.EndStateBlock();
        }

        // render stencil targets' depth but don't render pixels
        private void SetTargetsStateBlock()
        {
            CoreClient._CoreClient.D3DDevice.BeginStateBlock();

            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferWriteEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.AlphaBlendEnable = false; // why is this set if no color is written? should we have seperate for alpha?
            // color off
            CoreClient._CoreClient.D3DDevice.RenderState.ColorWriteEnable =  0;

            CoreClient._CoreClient.D3DDevice.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.CounterClockwise;
            
            _targetsToDepthBufferOnly = CoreClient._CoreClient.D3DDevice.EndStateBlock();
        }

        // http://www.gamedev.net/topic/546943-depth-stencil-buffer/
        //Posted 09 September 2009 - 05:52 PM

        //Point sprites are a nice optimization when the hardware supports them, 
        //and when you can work within their limitations. The main benefit is that 
        //since you only need one vertex per sprite you can use a much smaller vertex
        //buffer (with no index buffer necessary), which saves on memory, vertex 
        //processing load, and bandwidth. SpriteBatch just renders quads with a vertex 
        // buffer + index buffer, but is generally easier to use and gives you some more 
        //flexibility if you need it. Plus it doesn't require hardware support for point 
        // sprites, of course. If you're going to be rendering many many sprites (for 
        // something like a particle system), point sprites are a good choice if they're available.

        //Anyway your original question is unrelated to point sprites vs. SpriteBatch,
        // it's simply an issue of using the depth buffer correctly. First of all, what you 
        // want is basic z-buffering behavior: you want to render all of your 3D geometry
        // with z-writes and z-buffering enabled (RenderState.DepthBufferEnable = true and 
        // RenderState.DepthBufferWriteEnabled = true), and then render your sprites also
        // with z-buffering enabled. As long as...

        //1. z-buffering is enabled
        //2. the depth buffer hasn't been cleared
        //3. you're setting a proper depth value for your sprites

        //...they should be tested for visibility just like 3D geometry. If you're not 
        // getting this behavior, I would suggest checking those 3 things. 

        //Also on a side note, the stencil buffer doesn't hold "Z position"...it just 
        // holds an integer value. This value is typically 8-bits, can be 1-bit (or no bits) 
        // depending on the format of your DepthStencilBuffer. Either way it's just a value 
        // that's set depending on the values of RenderState.StencilFail and 
        // RenderState.StencilPass. If an incoming pixel passes the stencil test (specified by 
        // RenderState.StencilFunc) then StencilPass gets used, otherwise if it fails StencilFail 
        // gets used. At that point the stencil value doesn't get set to Z-position or anything 
        // like that...instead a simple operation is performed. It's either incremented, 
        // decremented, inverted, zeroed, or set to a value. See the documentation for
        // StencilOperation for more details.

        //You also can't copy stencil data to Z or vice-versa. In fact you can't really directly
        // manipulate the contents of the DepthStencilBuffer aside from clearing it...everything
        // has to be done through render states and drawing geometry. 
        private void SetStencilsStateBlock()
        {
            CoreClient._CoreClient.D3DDevice.BeginStateBlock();
            // note: zbuffer compare is still enabled, just write is disabled
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferWriteEnable = false;
            CoreClient._CoreClient.D3DDevice.RenderState.ColorWriteEnable = 0;
            CoreClient._CoreClient.D3DDevice.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.Clockwise;

            CoreClient._CoreClient.D3DDevice.RenderState.StencilEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilFunction = Microsoft.DirectX.Direct3D.Compare.Always;
            CoreClient._CoreClient.D3DDevice.RenderState.ReferenceStencil = 0;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilMask = 1;
            // if the stencil function (set to always) passes, then whenever we are to write a pixel (except that 
            // colorwriteneable = 0) we will increment the ReferenceStencil of 0 to 1 and thus everywhere the stencil
            // is visible, we will write a 1.
            CoreClient._CoreClient.D3DDevice.RenderState.StencilPass = Microsoft.DirectX.Direct3D.StencilOperation.Increment;


            _stencilsOnly = CoreClient._CoreClient.D3DDevice.EndStateBlock();
        }

        private void SetTargetsWithStencilBufferStateBlock()
        {
            CoreClient._CoreClient.D3DDevice.BeginStateBlock();

            // NOTE: This time we will write to the zbuffer (not just use it for compares) but the clipped volume
            // will be skipped so things rendered behind the wall will be visible through opening.
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferWriteEnable = true;
            CoreClient._CoreClient.D3DDevice.RenderState.SpecularEnable = true;
            // color back on
            CoreClient._CoreClient.D3DDevice.RenderState.ColorWriteEnable = Microsoft.DirectX.Direct3D.ColorWriteEnable.RedGreenBlueAlpha;
            CoreClient._CoreClient.D3DDevice.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.CounterClockwise;
            CoreClient._CoreClient.D3DDevice.RenderState.ReferenceStencil = 0;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilFunction = Microsoft.DirectX.Direct3D.Compare.Equal ;
            // stencil test passes because the ReferenceStencil 0 == StencilBuffer value 0 so the pixel of the target is drawn
            // since it is not masked out
            CoreClient._CoreClient.D3DDevice.RenderState.StencilPass = Microsoft.DirectX.Direct3D.StencilOperation.Replace;
            // stencil test fails because ReferenceStencil 0 != StencilBuffer value of 1 so we keep whatever pixel is already
            // in the buffer because that part of the buffer is masked out from writes
            CoreClient._CoreClient.D3DDevice.RenderState.StencilFail = Microsoft.DirectX.Direct3D.StencilOperation.Keep;
            CoreClient._CoreClient.D3DDevice.RenderState.StencilZBufferFail = Microsoft.DirectX.Direct3D.StencilOperation.Keep;
            CoreClient._CoreClient.D3DDevice.RenderState.ZBufferFunction = Microsoft.DirectX.Direct3D.Compare.LessEqual ;

            _targetsWithStencilBuffer = CoreClient._CoreClient.D3DDevice.EndStateBlock();
        }

        // obsolete - this never seems to be called.  DId we move all light management to RegionPVS
//        public object Apply(VisibleItem item)
//        {
//
//            // TODO: should cache the previously enabled lights and only change if there's a difference
//            // between enabled and disabled.  Then we can avoid disablealllights if not necessary
//            _context.Scene.DisableAllLights();
//            // disable all lights and enable the ones in item
//            if (item.InfluentialLights != null)
//            {
//                //if (item.Entity.ID == "50c1d96b-d486-4bea-a38a-3685c97bc544 0")
//                //    System.Diagnostics.Debug.WriteLine("test");
//                for (int i = 0; i < item.InfluentialLights.Count; i++)
//                {                    
//                    LightInfo info = item.InfluentialLights[i].LightInfo;
//                    System.Diagnostics.Debug.Assert(info.Light.TVIndex != -1);
//
//                    CoreClient._CoreClient.Light.SetLightPosition(info.Light.TVIndex, 
//                        (float)info.mCameraSpacePosition.x, 
//                        (float)info.mCameraSpacePosition.y, 
//                        (float)info.mCameraSpacePosition.z);
//
//                    CoreClient._CoreClient.Light.EnableLight(info.Light.TVIndex, true);
//                }
//            }
//
//            // Render
//            item.Draw(_context, mElapsedSeconds, FX_SEMANTICS.FX_NONE);
//
//            return null;
//        }
    }
}
