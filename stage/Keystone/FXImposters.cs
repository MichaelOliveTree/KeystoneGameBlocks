using System;
using System.Diagnostics;
using Keystone.Cameras;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Interfaces;
using Keystone.RenderSurfaces;
using Keystone.Resource;
using Keystone.Types;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using MTV3D65;
using Viewport = Microsoft.DirectX.Direct3D.Viewport;
using Matrix = Microsoft.DirectX.Matrix;

namespace Keystone.FX
{
    public class FXImposters : FXBase, INotifyDeviceReset
    {
        private static double _startDistanceSq;

        private const double MAX_IMPOSTER_AGE = 1000; // ms
        private const double VIEW_ANGLE_THRESHOLD = 25f; //degrees
        private double _viewLimit;

        //private double distance;
        //private double distEpsilon;
        //private Vector3d angle;
        //private Vector3d angleEpsilon;
        //private Vector3d targetRotation;
        //private Vector3d targetRotEpsilon;
        private Camera ImposterCamera;
        private ImposterPool _imposterPool;
        private Device _device = new Device(CoreClient._CoreClient.Internals.GetDevice3D());


        //public FXImposters(int expirationTime)
        //    : base(expirationTime)
        //{

        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolution"></param>
        /// <param name="impostersX">Should be a multiple of 2 or artifacts may occur</param>
        /// <param name="impostersY">Should be a multiple of 2 or artifacts may occur</param>
        public FXImposters(RSResolution resolution, int impostersX, int impostersY, float startDistance)
        {
            if (impostersX <= 0 || impostersY <= 0) throw new ArgumentOutOfRangeException();
            _semantic = FX_SEMANTICS.FX_IMPOSTER;
            _imposterPool = new ImposterPool(resolution, impostersX, impostersY);
            // We can share the vertex buffer since we just need to disable the ActiveImposters
            // we've already drawn as we draw each page.  However we do need to set a max
            // number of imposters we can handle so that we can properly allocate our vertexbuffer
            // well... unless we did want to use seperate VB's for each page...  
            // regardless we would need seperate DrawPrimitves() calls because we switch the textures
            // so...  it might also be more flexible in terms of letting us "grow" and "shrink" the required
            // pages..
            _startDistanceSq = startDistance * startDistance;
            _viewLimit = (double)Math.Cos(Utilities.MathHelper.DegreesToRadians(VIEW_ANGLE_THRESHOLD));
            ImposterCamera = new Camera(0, 0, (float)_viewLimit );
        }

        #region INotifyDeviceReset Members

        public void OnBeforeReset()
        {
            _imposterPool.ReleaseStateBlocks();
        }

        public void OnAfterReset()
        {
            _imposterPool.CreateStateBlocks();
        }

        #endregion

        public static double StartDistanceSq
        {
            get { return _startDistanceSq; }
        }

        public override void Register(IFXSubscriber subscriber)
        {
            // create and assign the FXSubscriberData
            FXSubscriberData data = new FXSubscriberData();
            data.Data = new Imposter[2];
            Imposter imposter = _imposterPool.CheckOut();
            imposter.Entity = (ModeledEntity)subscriber;
            data.Data[0] = imposter;
            data.Provider = this;
            subscriber.FXData[(int)FX_SEMANTICS.FX_IMPOSTER] = data;

            // once we start to "update" we'll assign the imposter we wish to use based on the distance.
            // however, care needs to be taken to make sure we dont switch to another imposter
            // on a seperate page where we _must_ update the imposter image there even when we cant
            // afford it.
            // so if a subscriber already has a generated imposter yet we want to change its imposter
            // we can reserve the imposter we want to switch too, and then make the final swap only
            // when our schedular lets us update it.

            base.Register(subscriber);
        }


        public override void Update(double elapsedSeconds, RenderingContext context)
        {
            if (_subscribers.Count == 0) return;

            Vector3 cameraWorldPosition = new Vector3((float)context.Position.x, (float)context.Position.y, (float)context.Position.z);

            Matrix projMatrix = _device.GetTransform(TransformType.Projection);
            Matrix worldMatrix = _device.GetTransform(TransformType.World);
            Matrix viewMatrix = _device.GetTransform(TransformType.View);
            //Matrix projMatrix = Types.Matrix.ToD3DMatrix(camera.ProjectionMatrix);
            //Matrix worldMatrix = Matrix.Identity;
            //Matrix viewMatrix = Types.Matrix.ToD3DMatrix(camera.Matrix);

            Types.Matrix  oldproj = context.Camera.Projection;
            Types.Matrix oldView = context.Camera.InverseView;

            ImposterCamera.Projection = oldproj;
            ImposterCamera.InverseView = oldView;
            Imposter imposter;
            Device _rsDevice = _imposterPool.BeginRender(ImposterCamera);
            Viewport d3dviewport = _rsDevice.Viewport;
            foreach (IFXSubscriber subscriber in _subscribers)
            {
                if (subscriber.InFrustum)
                {
                    // TODO: i think to accomodate switching the RS resolution of an existing imposter
                    //       we'd check the distance to see if we want a higher res imposter
                    //       then we create a wrapper FXImposterSubscriberData to be the .Data object that would include
                    //       the currentImposter and the desiredImposter and then
                    //       when the schedular goes to update it, if the (Imposter)Data[1] != null
                    //       it will release the (Imposter)Data[0], put the Data2 in its place 
                    //       setting Data[1] = null and then
                    //       carry on with the update.  Then final renders are all the same
                    //       we simply call Render on each Pool and the pools will setup the vertexbuffer
                    //       to only render the active & visible imposters.
                    imposter = (Imposter)subscriber.FXData[(int)FX_SEMANTICS.FX_IMPOSTER].Data[0];
                    imposter.InFrustum = subscriber.FXData[(int)FX_SEMANTICS.FX_IMPOSTER].InFrustum;

                    if (imposter.InFrustum)
                    {
                        Trace.Assert(imposter.HasGeometry, "Imposter does not have a Entity assigned to it.");

                        // update the imposter vertices, view and projection matrices.
                        //imposter.Update(cameraWorldPosition, d3dviewport, viewMatrix, projMatrix, worldMatrix);
                        imposter.Update(cameraWorldPosition, context.Viewport, d3dviewport, viewMatrix, projMatrix, worldMatrix);
                        //imposter.Update(cameraWorldPosition, camera.Viewport);
                        if (imposter.RequiresRegeneration = ImposterRequiresUpdate(cameraWorldPosition, imposter))
                        {
                            // TODO: this line along with the .being and endrender on the device could be moved to PreRender() for consistancy sake
                            imposter.Render(ImposterCamera, cameraWorldPosition, _rsDevice);
                        }
                        subscriber.FXData[(int)FX_SEMANTICS.FX_IMPOSTER].InFrustum = false; //reset for next frame
                    }
                }
            }
            _imposterPool.EndRender();

            CoreClient._CoreClient.Engine.GetViewport().SetTargetArea(0, 0, CoreClient._CoreClient.Graphics.Width, CoreClient._CoreClient.Graphics.Height);
          // _imposterPool.SaveTexture("g:\\stuff\\zzz_imposter9.dds", CONST_TV_IMAGEFORMAT.TV_IMAGE_DDS);
            //camera.Matrix = oldView;
            //camera.ProjectionMatrix = oldproj;
        }

        //public override void PreRender(Camera camera)
        //{
        //    Imposter imposter;
        //    Vector3 camPos = new Vector3(camera.Translation.x, camera.Translation.y, camera.Translation.z);
        //    Device _rsDevice = _imposterPool.BeginRender();

        //    foreach (IFXSubscriber subscriber in _subscribers)
        //    {
        //        imposter = (Imposter)subscriber.Data[(int)FX_SEMANTICS.FX_IMPOSTER].Data[0];
        //        if (imposter.InFrustum)
        //            imposter.Render(camera, camPos, _rsDevice);
        //    }
        //    _imposterPool.EndRender();
        //}
        // TODO: However we are still not tracking the proper lighting (which lights are on/off)
        // when rendering these imposters.  So appearance, shader and lighting arent properly handled
        // prior to rendering these to the RS Texture.
        // NOTE: It's ok to have a shader attached when we render. Just remember
        // to remove those shaders that you dont intend to reach here (e.g. landshadowmap lightmap shader)
        // But take PointLight shadowMaps for instance, the receivers keep the shaders attached
        // however it doesnt work well with billboards.  I think the point is however
        // that those items that have those shaders should not be rendered as imposters in the first place.
        // When they are that close to the camera, they will use the regular rendering path.
        // NOTE: Imposters must always render after terrain or else they get clipped.
        public override void Render(RenderingContext context)
        {
            _imposterPool.RenderImposters();
        }

        ////Object Pool members
        //protected override bool Validate(object obj)
        //{
        //    throw new NotImplementedException();
        //}

        //protected override object Create()
        //{
        //    throw new NotImplementedException();
        //}

        //protected override void Expire(object obj)
        //{
        //    throw new NotImplementedException();
        //}


        /// Determine if an imposter requires regeneration.
        private bool ImposterRequiresUpdate(Vector3 curCameraPos, Imposter pImposter)
        {
            return true; // TODO: below is broke and returns false to often
            // This imposter already requires regeneration, don't bother with anymore tests.
            if (pImposter.RequiresRegeneration) return true;

            // Imposter's age has expired.
    //        if ((Core._Core.Timer.Time() - pImposter.LastGeneratedTime) > MAX_IMPOSTER_AGE) return true;

            // Test the angle between the current camera vector and the camera vector at the
            // time the imposter was last generated.
            //Values range from 1 to -1. If the two input vectors are pointing in the same direction, 
            //then the return value will be 1. If the two input vectors are pointing in opposite directions,
            //then the return value will be -1. If the two input vectors are at right angles, then the return 
            //value will be 0. So, in effect, it is telling you how similar the two vectors are.
            Vector3 curCameraDir = Vector3.Normalize(curCameraPos - pImposter.Center);
            double viewAngle = Vector3.Dot(curCameraDir, pImposter.LastcameraDir);

            // The camera view angle has become to extreme.  
            if (viewAngle <= _viewLimit)
            {
                return true;
            }

            return false;
        }
    }
}
