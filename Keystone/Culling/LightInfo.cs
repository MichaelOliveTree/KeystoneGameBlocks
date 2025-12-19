
using System;
using System.Collections.Generic;
using Keystone.Types;
using Keystone.Lights;
using Keystone.Entities;
using Keystone.Elements;

namespace Keystone.Culling
{
    
    public struct SortableLightInfo
    {
        internal LightInfo LightInfo;
        internal double DistanceToItemSquared;
    }

    // Irrlicht light management - http://irrlicht.sourceforge.net/docu/example020.html
    // Ogre light management - http://www.ogre3d.org/forums/viewtopic.php?f=5&t=67504
    // visible light info
    public class LightInfo
    {
        internal RegionPVS mRegionPVS;
        private Light mLight;
        public Vector3d mCameraSpacePosition;
        public double DistanceToCameraSq;
        private int mHashCode;
        private int mFlags;
        private IntersectResult mVisibility;  // is this camera fully inside the light volume or is the light fully inside the camera or is it partial intersection?
        // if the camera is fully inside, we never need to compare the entity with it, we know the entity is in range because the entity is in the frustum
        public LightInfo(Light l, Vector3d cameraSpacePosition, IntersectResult visibility)
        {
            if (l == null) throw new ArgumentNullException();
            mLight = l;
            mCameraSpacePosition = cameraSpacePosition;
            DistanceToCameraSq = Vector3d.GetLengthSquared(cameraSpacePosition);
            mVisibility = visibility;

            // Lights[] lights = mLight.Scene.Lights;

            // when this lightinfo is created, we need to assign it to entities
            // that it is in range of.  We can use a sphere, box, or cone light volume and iterate 
            // with a custom traverser to find entities in that volume.
            //
            // for spotlights
            //mLight.Scene.

        }

        public Light Light { get { return mLight; } }
        public Vector3d OriginalPosition { get { return mLight.Translation; } }
        public Vector3d CameraSpacePosition { get { return mCameraSpacePosition; } }

        public override int GetHashCode()
        {
            return mHashCode;
        }
    }

    
    //// a Light info container object that is attached to each entity
    //// to cache which lights affect a particular entity.  
    //// TODO: not really implemented yet, just a concept of a listener/subscriber system to cache
    //// per frame results to speed up calcs for light influence determination
    //public class AreaLights
    //{
    //    internal Light[] mLights;
    //    private int mFlags;   // isDirty | 
    //    private Entity mEntity;

    //    internal AreaLights(Entity entity)
    //    {
    //        if (entity == null) throw new ArgumentNullException();
    //        mEntity = entity;
    //    }

    //    internal Light[] Lights  // we use array for minimum memory and extension methods for adding/removing
    //    {
    //        get
    //        {
    //            //if (IsDirty)
    //            //{
    //            //    mLights = Core._core.Scene.LightManager.GetAreaLights(mEntity);
    //            //}
    //            return mLights;
    //        }
    //    }

    //    // flag is reset if entity.Flag & EntityFlags.RequeryLightsOnMove 
    //    // and the entity has moved.  It also requeries even if RequeryLightsOnMove == false 
    //    // but the entity has crossed a new region
    //    private bool IsDirty
    //    {
    //        get
    //        {
    //            return false; // (mFlags & LightFlags.IsDirty) == LightFlags.IsDirty;
    //        }
    //    }
    //}
}