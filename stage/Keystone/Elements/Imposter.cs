using System;
using System.Diagnostics;
using Keystone.Cameras;
using Keystone.Entities;
using Keystone.FX;
using Keystone.Types;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using MTV3D65;
using Color = System.Drawing.Color;
using Matrix = Keystone.Types.Matrix;
using Viewport = Microsoft.DirectX.Direct3D.Viewport;

namespace Keystone.Elements
{
    // NOTE: Sharing of RS slots is not allowed and is not desireable (contrary to what you might think)
    // The primary reason is that lighting may be different for otherwise identicle instances.  This alone
    // is enough frankly.  But beyond that, it's too hectic to deal with and probably slows us down overall
    // trying to manage it.


    // fundamental difference between a billboard and an imposter is that an imposter
    // is generally created dyanmically.  Not only that, but an imposter's 2d representations
    // can be/ are usually generated in a picture array and the proper offsets into the image
    // can be computed and the proper one drawn at runtime.
    // The next major distinction is that the imposter is drawn via Screen2d.Draw_Texture() and 
    // does not utilize the Mesh3d.BillboardType.
    public class Imposter
    {
        private ModeledEntity _entity;

        public bool HasGeometry;


        // The current imposter camera distance, computed each time imposters are rendered.
        // This is used for depth sorting alpha-blended imposters.
        public double CameraDistSq;
        // Direction vector from imposter centre to camera position.
        public Vector3 LastcameraDir;

        // The vertices that comprise the imposter billboard.
        public CustomVertex.PositionTextured[] Vertices = new CustomVertex.PositionTextured[6];

        // Centre position of the imposter billboard in world space.
        public Vector3 Center;

        public BoundingBox BoundingBox; //boundingbox of the underlying mesh in world units
        public float BoundingRadius; //sphere radius of the underlying mesh in world length


        /// The offset of the imposter in the render texture.
        public int Left;

        public int Top;
        public int Width;
        public int Height;

        // Set to ‘true’ when this imposter needs to be regenerated.
        public bool RequiresRegeneration;

        // The time the imposter was created.
        public double StartTime;

        // The time the imposter was last generated.
        public double LastGeneratedTime;

        /// Set to 'true' when an imposter is visible.
        public bool InFrustum;

        /// Set to 'true' when an imposter is generated.
        public bool Generated;

        private TVRenderSurface _rs;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rs">RS is shared by many imposters</param>
        public Imposter(TVRenderSurface rs)
        {
            //if (element == null) throw new ArgumentNullException();
            //Element = element;
            if (rs == null) throw new ArgumentNullException();
            _rs = rs;
        }

        public Vector3d[] BoundingBoxVertices()
        {
            Vector3d[] verts = new Vector3d[8];

            
            for (int i = 0; i < 8; i++)
            {
                verts[i] = BoundingBox.Vertices[i];
            }

            return verts;
        }

        public ModeledEntity  Entity
        {
            get { return _entity; }
            set
            {
                _entity = value;
                if (_entity == null)
                {
                    HasGeometry = false;
                }
                else
                {
                    Trace.Assert(_entity != null);
                    Trace.Assert(_entity.Model != null);
                    HasGeometry = true;
                    RequiresRegeneration = true;
                    LastGeneratedTime = 0;
                }
            }
        }

        /// <summary>
        /// Update is called by the schedular
        /// </summary>
        public void Update()
        {
            // check that our allocation is not expired.  I dont suspect we'd have a seperate thread
            // doing expirations but if so, we might want a mutex

            // check all the epsilons and see if we need to get a new allocation

            // if we're in good shape we can reset our Schedule timeInQueue 

            // otherwise we try to get a new allocation, if we cant get one, then we'll keep the old
            // but we wont reset our timeInQueue

            // if we can get one then we can update 

            //ProgressiveMesh pmesh;
            //Mesh mesh;

            //mesh = new Mesh( Core._CoreClient.Internals.GetD3DMesh(1));

            //mesh.Save(, , , XFileFormat.Text);
        }


        // init imposter viewport that peers into just the single allocation on the rendersurface
        public Viewport GetViewPort()
        {
            Viewport viewport = new Viewport();
            viewport.X = Left;
            viewport.Y = Top;
            viewport.Width = Width;
            viewport.Height = Height;
            viewport.MinZ = 0.0f;
            viewport.MaxZ = 1.0f;
            return viewport;
        }

        ///// <summary>
        ///// Computes the screen space quad using camera space bounding box position of mesh and then projects
        ///// back to camera space coords so that they can be rendered in a single drawprimitive command.
        ///// </summary>
        ///// <param name="vp"></param>
        //public void Update(Vector3 curCameraPos, Cameras.Viewport vp)
        //{
        //    const int NUM_VERTS_IN_A_BOX = 8;
        //    const int NUM_VERTS_IN_A_QUAD = 4;

        //    //int stride = sizeof(Vector3d);
        //    //BoundingBox = Entity.BoundingBox; 
        //    //BoundingBox = new BoundingBox(Entity.BoundingBox.Min - _relativeRegionOffsets.Peek() - curCameraPos, Entity.BoundingBox.Max - _relativeRegionOffsets.Peek() - curCameraPos);
        //    Vector3d pos = new Vector3d(curCameraPos.X, curCameraPos.Y, curCameraPos.Z);
        //    // camera space translated bounding box (TODO: this isnt taking into account relative region offset)
        //    BoundingBox = new BoundingBox(Entity.BoundingBox.Min - pos, Entity.BoundingBox.Max - pos);

        //    Vector3d[] projectedVerts = BoundingBoxVertices();
        //    BoundingRadius = (float)Entity.BoundingSphere.Radius;

        //    // Project bounding box into screen space.
        //    //D3DXVec3ProjectArray(screenVerts, stride, boundingBoxVerts, stride, 
        //    //                        viewport, projMatrix, viewMatrix, worldTransform,
        //    //                        NUM_BOX_VERTS);

        //    for (int i = 0; i < NUM_VERTS_IN_A_BOX; i++)
        //    {
        //        projectedVerts[i] = vp.Project(projectedVerts[i].x, projectedVerts[i].y, projectedVerts[i].z);                
        //    }

        //    // Determine the smallest screen-space quad that encompasses the bounding box.
        //    double minX = projectedVerts[0].x;
        //    double maxX = projectedVerts[0].x;
        //    double minY = projectedVerts[0].y;
        //    double maxY = projectedVerts[0].y;
        //    double minZ = projectedVerts[0].z;

        //    // start at index 1 since index 0 was already used to initialize our min & max values above
        //    for (int i = 1; i < NUM_VERTS_IN_A_BOX; ++i)
        //    {
        //        minX = Math.Min(projectedVerts[i].x, minX);
        //        maxX = Math.Max(projectedVerts[i].x, maxX);
        //        minY = Math.Min(projectedVerts[i].y, minY);
        //        maxY = Math.Max(projectedVerts[i].y, maxY);
        //        minZ = Math.Min(projectedVerts[i].z, minZ);
        //    }

        //    Vector3d[] screenSpaceVerts = new Vector3d[] // our quad in 2d screenspace
        //        {
        //            new Vector3d(minX, minY, minZ),
        //            new Vector3d(maxX, minY, minZ),
        //            new Vector3d(maxX, maxY, minZ),
        //            new Vector3d(minX, maxY, minZ)
        //        };

        //    // Unproject the screen-space quad into world-space (actually these will be in camera space since
        //    // we pass in viewMatrix that has camera at origin).
        //    Vector3d[] worldSpaceVerts = new Vector3d[NUM_VERTS_IN_A_QUAD];


        //    //D3DXVec3UnprojectArray(worldSpaceVerts, stride, screenSpaceVerts, stride,
        //    //                          viewport, projMatrix, viewMatrix, identity, NUM_QUAD_VERTS); //worldTransform instead of identity?

        //    for (int i = 0; i < NUM_VERTS_IN_A_QUAD; i++)
        //    {
        //        worldSpaceVerts[i] = vp.UnProject((int)screenSpaceVerts[i].x, (int)screenSpaceVerts[i].y, (float)screenSpaceVerts[i].z);
        //    }

        //    // using the 4 unprojected vertices, map these to the 6 vertices of our two triangle quad that
        //    // makes up this imposter and save the vertices for later
        //    Vertices[0].X = (float)worldSpaceVerts[0].x;
        //    Vertices[0].Y = (float)worldSpaceVerts[0].y;
        //    Vertices[0].Z = (float)worldSpaceVerts[0].z;

        //    Vertices[1].X = (float)worldSpaceVerts[1].x;
        //    Vertices[1].Y = (float)worldSpaceVerts[1].y;
        //    Vertices[1].Z = (float)worldSpaceVerts[1].z;

        //    Vertices[2].X = (float)worldSpaceVerts[3].x;
        //    Vertices[2].Y = (float)worldSpaceVerts[3].y;
        //    Vertices[2].Z = (float)worldSpaceVerts[3].z;

        //    Vertices[3].X = (float)worldSpaceVerts[1].x;
        //    Vertices[3].Y = (float)worldSpaceVerts[1].y;
        //    Vertices[3].Z = (float)worldSpaceVerts[1].z;

        //    Vertices[4].X = (float)worldSpaceVerts[2].x;
        //    Vertices[4].Y = (float)worldSpaceVerts[2].y;
        //    Vertices[4].Z = (float)worldSpaceVerts[2].z;

        //    Vertices[5].X = (float)worldSpaceVerts[3].x;
        //    Vertices[5].Y = (float)worldSpaceVerts[3].y;
        //    Vertices[5].Z = (float)worldSpaceVerts[3].z;

        //    Vector3d tempCenter = (worldSpaceVerts[0] + worldSpaceVerts[1] + worldSpaceVerts[2] + worldSpaceVerts[3]) * 0.25f;
        //    Center = new Vector3((float)tempCenter.x, (float)tempCenter.y, (float)tempCenter.z);
        //}

        /// <summary>
        /// Computes the screen space quad using camera space bounding box position of mesh and then projects
        /// back to camera space coords so that they can be rendered in a single drawprimitive command.
        /// </summary>
        /// <param name="vp"></param>
        public void Update(Vector3 curCameraPos, Cameras.Viewport vp, Viewport viewport, Microsoft.DirectX.Matrix viewMatrix,
                           Microsoft.DirectX.Matrix projMatrix, Microsoft.DirectX.Matrix worldTransform)
        {
            const int NUM_VERTS_IN_A_BOX = 8;
            const int NUM_VERTS_IN_A_QUAD = 4;

            //int stride = sizeof(Vector3d);
            //BoundingBox = Entity.BoundingBox; 
            //BoundingBox = new BoundingBox(Entity.BoundingBox.Min - _relativeRegionOffsets.Peek() - curCameraPos, Entity.BoundingBox.Max - _relativeRegionOffsets.Peek() - curCameraPos);
            Vector3d pos = new Vector3d(curCameraPos.X, curCameraPos.Y, curCameraPos.Z);
            // camera space translated bounding box (TODO: this isnt taking into account relative region offset)
            BoundingBox = new BoundingBox(Entity.BoundingBox.Min - pos, Entity.BoundingBox.Max - pos);

            Vector3d[] projectedVerts = BoundingBoxVertices();
            BoundingRadius = (float)Entity.BoundingSphere.Radius;

            // Project bounding box into screen space.
            //D3DXVec3ProjectArray(screenVerts, stride, boundingBoxVerts, stride, 
            //                        viewport, projMatrix, viewMatrix, worldTransform,
            //                        NUM_BOX_VERTS);

            Vector3[] tempVerts = new Vector3 [NUM_VERTS_IN_A_BOX];
            for (int i = 0; i < NUM_VERTS_IN_A_BOX; i++)
            {
                Vector3 tempVert = new Vector3((float)projectedVerts[i].x, (float)projectedVerts[i].y, (float)projectedVerts[i].z);
                // TODO: convert this to just Viewport.Project? maybe not cuz this is all in DX Vector3 types
                tempVerts[i] =
                    Vector3.Project(tempVert, viewport, projMatrix, viewMatrix, worldTransform);
            }
            for (int i = 0; i < NUM_VERTS_IN_A_BOX; i++)
            {
                projectedVerts[i] = vp.Project(projectedVerts[i].x, projectedVerts[i].y, projectedVerts[i].z, 
            	                               Helpers.TVTypeConverter.FromDXMatrix (viewMatrix),
            	                               Helpers.TVTypeConverter.FromDXMatrix (projMatrix), 
            	                               Helpers.TVTypeConverter.FromDXMatrix (worldTransform));
                
                projectedVerts[i] = Helpers.TVTypeConverter.FromDXVector (tempVerts[i]);
            }

            // Determine the smallest screen-space quad that encompasses the bounding box.
            double minX = projectedVerts[0].x;
            double maxX = projectedVerts[0].x;
            double minY = projectedVerts[0].y;
            double maxY = projectedVerts[0].y;
            double minZ = projectedVerts[0].z;

            // start at index 1 since index 0 was already used to initialize our min & max values above
            for (int i = 1; i < NUM_VERTS_IN_A_BOX; ++i)
            {
                minX = Math.Min(projectedVerts[i].x, minX);
                maxX = Math.Max(projectedVerts[i].x, maxX);
                minY = Math.Min(projectedVerts[i].y, minY);
                maxY = Math.Max(projectedVerts[i].y, maxY);
                minZ = Math.Min(projectedVerts[i].z, minZ);
            }

            Vector3d[] screenSpaceVerts = new Vector3d[] // our quad in 2d screenspace
                {
                    new Vector3d(minX, minY, minZ),
                    new Vector3d(maxX, minY, minZ),
                    new Vector3d(maxX, maxY, minZ),
                    new Vector3d(minX, maxY, minZ)
                };

            // Unproject the screen-space quad into world-space (actually these will be in camera space since
            // we pass in viewMatrix that has camera at origin).
            Vector3d[] worldSpaceVerts = new Vector3d[NUM_VERTS_IN_A_QUAD];


            //D3DXVec3UnprojectArray(worldSpaceVerts, stride, screenSpaceVerts, stride,
            //                          viewport, projMatrix, viewMatrix, identity, NUM_QUAD_VERTS); //worldTransform instead of identity?

            for (int i = 0; i < NUM_VERTS_IN_A_QUAD; i++)
            {
                Vector3 temp = new Vector3((float)screenSpaceVerts[i].x, (float)screenSpaceVerts[i].y, (float)screenSpaceVerts[i].z);
                temp = Vector3.Unproject(temp, viewport, projMatrix, viewMatrix, worldTransform);
                worldSpaceVerts[i] = new Vector3d(temp.X , temp.Y , temp.Z );
              //  worldSpaceVerts[i] = vp.UnProject((int)screenSpaceVerts[i].x, (int)screenSpaceVerts[i].y, 0); //(float)screenSpaceVerts[i].z);
            }

            // using the 4 unprojected vertices, map these to the 6 vertices of our two triangle quad that
            // makes up this imposter and save the vertices for later
            Vertices[0].X = (float)worldSpaceVerts[0].x;
            Vertices[0].Y = (float)worldSpaceVerts[0].y;
            Vertices[0].Z = (float)worldSpaceVerts[0].z;

            Vertices[1].X = (float)worldSpaceVerts[1].x;
            Vertices[1].Y = (float)worldSpaceVerts[1].y;
            Vertices[1].Z = (float)worldSpaceVerts[1].z;

            Vertices[2].X = (float)worldSpaceVerts[3].x;
            Vertices[2].Y = (float)worldSpaceVerts[3].y;
            Vertices[2].Z = (float)worldSpaceVerts[3].z;

            Vertices[3].X = (float)worldSpaceVerts[1].x;
            Vertices[3].Y = (float)worldSpaceVerts[1].y;
            Vertices[3].Z = (float)worldSpaceVerts[1].z;

            Vertices[4].X = (float)worldSpaceVerts[2].x;
            Vertices[4].Y = (float)worldSpaceVerts[2].y;
            Vertices[4].Z = (float)worldSpaceVerts[2].z;

            Vertices[5].X = (float)worldSpaceVerts[3].x;
            Vertices[5].Y = (float)worldSpaceVerts[3].y;
            Vertices[5].Z = (float)worldSpaceVerts[3].z;

            Vector3d tempCenter = (worldSpaceVerts[0] + worldSpaceVerts[1] + worldSpaceVerts[2] + worldSpaceVerts[3]) * 0.25f;
            Center = new Vector3((float)tempCenter.x, (float)tempCenter.y, (float)tempCenter.z);
        }
        ///// <summary>
        ///// Updates the coordinates of the Imposter Quad
        ///// </summary>
        ///// <param name="curCameraPos"></param>
        ///// <param name="viewport"></param>
        ///// <param name="viewMatrix"></param>
        ///// <param name="projMatrix"></param>
        ///// <param name="worldTransform"></param>
        //public void Update(Vector3 curCameraPos, Viewport viewport, Microsoft.DirectX.Matrix viewMatrix,
        //                   Microsoft.DirectX.Matrix projMatrix, Microsoft.DirectX.Matrix worldTransform)
        //{
        //    const int NUM_BOX_VERTS = 8;
        //    const int NUM_QUAD_VERTS = 4;

        //    //int stride = sizeof(Vector3d);
        //    //BoundingBox = Entity.BoundingBox; 
        //    //BoundingBox = new BoundingBox(Entity.BoundingBox.Min - _relativeRegionOffsets.Peek() - curCameraPos, Entity.BoundingBox.Max - _relativeRegionOffsets.Peek() - curCameraPos);
        //    Vector3d pos = new Vector3d(curCameraPos.X, curCameraPos.Y, curCameraPos.Z);
        //    // camera space translated bounding box (TODO: this isnt taking into account relative region offset)
        //    BoundingBox = new BoundingBox(Entity.BoundingBox.Min - pos, Entity.BoundingBox.Max - pos);

        //    Vector3d[] screenVerts = BoundingBoxVertices();
        //    BoundingRadius = (float)Entity.BoundingSphere.Radius;

        //    // Project bounding box into screen space.
        //    //D3DXVec3ProjectArray(screenVerts, stride, boundingBoxVerts, stride, 
        //    //                        viewport, projMatrix, viewMatrix, worldTransform,
        //    //                        NUM_BOX_VERTS);

        //    for (int i = 0; i < NUM_BOX_VERTS; i++)
        //    {
        //        // TODO: convert this to just Viewport.Project? maybe not cuz this is all in DX Vector3 types
        //        screenVerts[i] =
        //            Vector3.Project(screenVerts[i], viewport, projMatrix, viewMatrix, worldTransform);



        //    }

        //    // Determine the smallest screen-space quad that encompasses the bounding box.
        //    float minX = screenVerts[0].X;
        //    float maxX = screenVerts[0].X;
        //    float minY = screenVerts[0].Y;
        //    float maxY = screenVerts[0].Y;
        //    float minZ = screenVerts[0].Z;

        //    for (int i = 1; i < NUM_BOX_VERTS; ++i)
        //    {
        //        minX = Math.Min(screenVerts[i].X, minX);
        //        maxX = Math.Max(screenVerts[i].X, maxX);
        //        minY = Math.Min(screenVerts[i].Y, minY);
        //        maxY = Math.Max(screenVerts[i].Y, maxY);
        //        minZ = Math.Min(screenVerts[i].Z, minZ);
        //    }


        //    Vector3[] screenSpaceVerts = new Vector3[] // our quad in 2d screenspace
        //        {
        //            new Vector3(minX, minY, minZ),
        //            new Vector3(maxX, minY, minZ),
        //            new Vector3(maxX, maxY, minZ),
        //            new Vector3(minX, maxY, minZ)
        //        };

        //    // Unproject the screen-space quad into world-space (actually these will be in camera space since
        //    // we pass in viewMatrix that has camera at origin).
        //    Vector3[] worldSpaceVerts = new Vector3[NUM_QUAD_VERTS];


        //    //D3DXVec3UnprojectArray(worldSpaceVerts, stride, screenSpaceVerts, stride,
        //    //                          viewport, projMatrix, viewMatrix, identity, NUM_QUAD_VERTS); //worldTransform instead of identity?

        //    for (int i = 0; i < NUM_QUAD_VERTS; i++)
        //    {
        //        worldSpaceVerts[i] =
        //            Vector3.Unproject(screenSpaceVerts[i], viewport, projMatrix, viewMatrix, worldTransform);
        //    }

        //    // using the 4 unprojected vertices, map these to the 6 vertices of our two triangle quad that
        //    // makes up this imposter and save the vertices for later
        //    Vertices[0].X = worldSpaceVerts[0].X;
        //    Vertices[0].Y = worldSpaceVerts[0].Y;
        //    Vertices[0].Z = worldSpaceVerts[0].Z;

        //    Vertices[1].X = worldSpaceVerts[1].X;
        //    Vertices[1].Y = worldSpaceVerts[1].Y;
        //    Vertices[1].Z = worldSpaceVerts[1].Z;

        //    Vertices[2].X = worldSpaceVerts[3].X;
        //    Vertices[2].Y = worldSpaceVerts[3].Y;
        //    Vertices[2].Z = worldSpaceVerts[3].Z;

        //    Vertices[3].X = worldSpaceVerts[1].X;
        //    Vertices[3].Y = worldSpaceVerts[1].Y;
        //    Vertices[3].Z = worldSpaceVerts[1].Z;

        //    Vertices[4].X = worldSpaceVerts[2].X;
        //    Vertices[4].Y = worldSpaceVerts[2].Y;
        //    Vertices[4].Z = worldSpaceVerts[2].Z;

        //    Vertices[5].X = worldSpaceVerts[3].X;
        //    Vertices[5].Y = worldSpaceVerts[3].Y;
        //    Vertices[5].Z = worldSpaceVerts[3].Z;

        //    Center = (worldSpaceVerts[0] + worldSpaceVerts[1] + worldSpaceVerts[2] + worldSpaceVerts[3]) * 0.25f;
        //}

        /// <summary>
        /// Updates the target area of the RenderSurface with a new render of this imposter's geometry 
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="curCameraPos"></param>
        /// <returns></returns>
        public bool Render(Camera camera, Vector3 cameraWorldSpacePosition, Device device)
        {
            // NOTE: We render to the RS in camera space like the rest of the scene because our lights are also translated
            // to camera space during cull traversal and lighting manager because we want these imposters to be rendered
            // using the lighting that is correct for their position in the game world.

            // TODO: and how / where do I setup the lighting?  I mean if this imposter
            //       is near indoors where there's a pointlight, i want to know the info determined by the traverser
            //       which lights to enable and disable (e.g. enable outdoor directional, disable that nearby indoor)
            //       I think ultimately culling traversal should sort by 
            //       lights, alpha, appearance, shader, 

            // prepare to update this imposter in the RS
            // compute the world vectors for our billboard
            Vector3 cameraOrigin = new Vector3(0, 0, 0);
            Vector3 up = new Vector3(0, 1, 0); // TODO: for true 6d0f camera, this wont cut it.  we'll need to use quaternion
            Vector3 orientation = -Vector3.Normalize(cameraOrigin - Center);
            Vector3 side = Vector3.Cross(up, orientation);
            side = Vector3.Normalize(side);
            up = Vector3.Cross(orientation, side); //TODO: these two lines may be ok to comment out?
            up = Vector3.Normalize(up); //TODO: these two lines may be ok to comment out?


            Microsoft.DirectX.Matrix imposterViewMatrix = new Microsoft.DirectX.Matrix();
            Microsoft.DirectX.Matrix imposterProjMatrix = new Microsoft.DirectX.Matrix();

            // Calculate near and far planes.  NOTE: Unlike our Planet imposter 
            // ( FXPlanetAtmosphere.GenerateImposter()) where we
            // calculate the near and far planes from the center of the mesh minus the camera pos
            // here we are use the center of the 2d imposter billboard minus the camera pos so 
            // our near plane is essentially correct from the start.
            Vector3 nearPlaneVec = Center - cameraOrigin;
            float nearPlane = Vector3.Length(nearPlaneVec) - .5F;
            // this .5F is arbitrary and in the event your world scale 
            // is less than 1 tvunit = ~1 meter, then this .5F might need to be much tinier.
            float farPlane = nearPlane + (BoundingRadius * 2.0f);

            CreateMatrices(cameraOrigin, up, ref imposterViewMatrix, ref imposterProjMatrix,
                           nearPlane, farPlane);

            camera.SetLookAt(Center.X, Center.Y, Center.Z);
            camera.Projection = Helpers.TVTypeConverter.FromDXMatrix(imposterProjMatrix);


            // Render the mesh to the imposter.
            _rs.SetNewCamera(camera.TVCamera);
            // TODO: switch to a dedicated camera and viewport?
            CoreClient._CoreClient.Engine.GetViewport().SetTargetArea(Left, Top, Width, Height);


            // this clears just the portion of the RS defined by the current viewport
            device.Clear(ClearFlags.Target, Color.FromArgb(0, 1, 0, 1), 100.0F, 0);
            // Core._CoreClient.D3DDevice.Clear(ClearFlags.Target, Color.FromArgb(0, 1, 0, 1), 100.0F, 0);

            // TODO: screenspace constant render re-scale check?  Maybe imposters cant be (shouldnt ever be) so? 
            //Entity.Model.Render(Entity,new Vector3d(-curCameraPos.X, -curCameraPos.Y, -curCameraPos.Z), FX_SEMANTICS.FX_IMPOSTER);
            Vector3d entityCameraSpacePosition = Entity.Translation - new Vector3d(cameraWorldSpacePosition.X, cameraWorldSpacePosition.Y, cameraWorldSpacePosition.Z);

            throw new NotImplementedException("Since adding Entity.Model.Render() need to update");
            //Entity.Render(entityCameraSpacePosition, new SwitchNodeOptions(), FX_SEMANTICS.FX_IMPOSTER);

            // TODO: maybe i can delete this cameraPos parameter after i finish Model to have an overloaded version that doesnt need it.             

            // LastcameraDir angle is only updated and used for purposes of Regneration test
            LastcameraDir = Vector3.Normalize(cameraOrigin - Center);
            Generated = true;
            RequiresRegeneration = false;
            LastGeneratedTime = Entity.Scene.Simulation.GameTime.TotalElapsedSeconds;
            return true;
        }

        // Create view and projection matrices, based on the current camera and 
        // using the imposter quad as the viewing plane.
        // can probably grab some of this out of my planet imposter code
        private void CreateMatrices(Vector3 curCameraPos, Vector3 curCameraUp,
                                    ref Microsoft.DirectX.Matrix imposterViewMatrix,
                                    ref Microsoft.DirectX.Matrix imposterProjMatrix, float imposterNearPlane,
                                    float imposterFarPlane)
        {
            imposterViewMatrix = Microsoft.DirectX.Matrix.LookAtLH(curCameraPos, Center, curCameraUp);

            Vector3 tmp1 = new Vector3(Vertices[1].X, Vertices[1].Y, Vertices[1].Z);
            Vector3 tmp2 = new Vector3(Vertices[0].X, Vertices[0].Y, Vertices[0].Z);

            Vector3 widthVec = tmp1 - tmp2;
            tmp1 = new Vector3(Vertices[5].X, Vertices[5].Y, Vertices[5].Z);

            Vector3 heightVec = tmp1 - tmp2;
            float viewWidth = Vector3.Length(widthVec);
            float viewHeight = Vector3.Length(heightVec);
            imposterProjMatrix =
                Microsoft.DirectX.Matrix.PerspectiveLH(viewWidth, viewHeight, imposterNearPlane, imposterFarPlane);
        }
    }
}
