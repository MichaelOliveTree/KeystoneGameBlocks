using System;

using System.Diagnostics;
using System.Xml;
using Keystone.IO;
using Keystone.Traversers;
using Keystone.Types;
using Keystone.Elements;
using Keystone.Entities;

namespace Keystone.Entities
{
    /// <summary>
    /// This particular Root Region is not for Zoned worlds. (see ZoneRoot).
    /// This Root is for single large region worlds with only interior regions allowed
    /// as child regions.
    /// </summary>
    public class Root : Region 
    {
        public double RegionDiameterX;
        public double RegionDiameterY;
        public double RegionDiameterZ;
        public uint OctreeNodeDepth;

        public double HalfHeight;
        public double HalfWidth;
        public double HalfDepth;


        public Root(string id, double regionDiameterX, double regionDiameterY, double regionDiameterZ,
                         uint octreeDepth)
            : this(id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException();

            RegionDiameterX = regionDiameterX;
            RegionDiameterY = regionDiameterY;
            RegionDiameterZ = regionDiameterZ;
            OctreeNodeDepth = octreeDepth;

            Init();
        }

        internal Root(string id)
            : base(id)
        {
        	SetEntityAttributesValue ((uint)KeyCommon.Flags.EntityAttributes.Root, true);
        	SetEntityAttributesValue ((uint)KeyCommon.Flags.EntityAttributes.Region, true);
        }


        #region Node Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        private void Init()
        {
            HalfWidth = RegionDiameterX * .5d;
            HalfHeight = RegionDiameterY * .5d;
            HalfDepth = RegionDiameterZ * .5d;
                      
            SetChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty, Keystone.Enums.ChangeSource.Self);
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[4 + tmp.Length];
            tmp.CopyTo(properties, 4);

            properties[0] = new Settings.PropertySpec("regiondiameterx", RegionDiameterX.GetType().Name);
            properties[1] = new Settings.PropertySpec("regiondiametery", RegionDiameterY.GetType().Name);
            properties[2] = new Settings.PropertySpec("regiondiameterz", RegionDiameterZ.GetType().Name);
            properties[3] = new Settings.PropertySpec("octreenodedepth", OctreeNodeDepth.GetType().Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = RegionDiameterX;
                properties[1].DefaultValue = RegionDiameterY;
                properties[2].DefaultValue = RegionDiameterZ;
                properties[3].DefaultValue = OctreeNodeDepth;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "regiondiameterx":
                        RegionDiameterX = (double)properties[i].DefaultValue;
                        break;
                    case "regiondiametery":
                        RegionDiameterY = (double)properties[i].DefaultValue;
                        break;
                    case "regiondiameterz":
                        RegionDiameterZ = (double)properties[i].DefaultValue;
                        break;
                    case "octreenodedepth":
                        OctreeNodeDepth = (uint)properties[i].DefaultValue;
                        break;
                }
            }

            Init(); // TODO: this is required to init the bounding box 

        }


        internal override bool ChildEntityBoundsFail(Entity child, out Region newParentRegion)
        {
            // Root.cs we know there are never any Zones (only ZoneRoot.cs can have child Zones)
            // So here enforceBounds means an entity simply cannot ever leave the bounds.  It's position
            // must be halted.
            return base.ChildEntityBoundsFail(child, out newParentRegion);
        }

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Root bounding volume is always fixed.
        /// </summary>
        protected override void UpdateBoundVolume()
        {
            Vector3d min = new Vector3d(-HalfWidth, -HalfHeight, -HalfDepth);
            Vector3d max = new Vector3d(HalfWidth, HalfHeight, HalfDepth);
            mBox  = new BoundingBox(min, max);
            mSphere = new BoundingSphere(mBox);
            _maxVisibleDistanceSq = mBox.Diameter;
            _maxVisibleDistanceSq *= _maxVisibleDistanceSq;

            DisableChangeFlags(Keystone.Enums.ChangeStates.BoundingBoxDirty 
                | Keystone.Enums.ChangeStates.BoundingBox_TranslatedOnly);
        }
    }
}
