using System;
using Keystone.Extensions;
using Keystone.Elements;
using System.Diagnostics;



namespace Keystone.Animation
{
    public abstract class AnimationClip : Node
    {

    	// OBSOLETE - TargetID will vary across prefabs during deserializtion.
    	// protected string _targetID;           
        protected string _targetName;         // Parent friendly name exists here.
        protected string _targetPropertyName;
        
        protected float _duration; // length of just this clip
        protected bool mChanged;

        public AnimationClip(string id)
            : base(id)
        {
            // TODO: "_targetID" and "_targetName" and "_targetPropertyName"
            // prevents us from sharing.  If we could move those into the AnimationTrack.cs
            // then animations are fully shareable.  Well, we cannot do that because
            // a single track can have multiple clips each with different targets!
            // Or if we did move to AnimationTrack.cs then we'd have to store arrays 
            // of _targetID/_targetName/_targetPropertyName
            Shareable = false;
        }

        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            throw new NotImplementedException();
        }
        internal override Keystone.Traversers.ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }


        #region ResourceBase members
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[3 + tmp.Length];
            tmp.CopyTo(properties, 3);

            //OBSOLETE - properties[0] = new Settings.PropertySpec("targetid", typeof(string).Name);
            properties[0] = new Settings.PropertySpec("target", typeof(string).Name);
            properties[1] = new Settings.PropertySpec("targetproperty", typeof(string).Name);
            properties[2] = new Settings.PropertySpec("duration", typeof(float).Name);
            if (!specOnly)
            {
            	//properties[0].DefaultValue = _targetID;
                properties[0].DefaultValue = _targetName;
                properties[1].DefaultValue = _targetPropertyName;
                properties[2].DefaultValue = _duration;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            // TODO: we're not saving the keyframes here
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].DefaultValue == null) continue;
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
//                	case "targetid": // OBSOLETE: targetID will vary across instances of a prefab so this should not be a field we save
//                        _targetID = (string)properties[i].DefaultValue;
//                        // note: we propgate here and do not set our own. 
//                        PropogateChangeFlags(Keystone.Enums.ChangeStates.TargetChanged, Keystone.Enums.ChangeSource.Child);
//                        break;
                    case "target":
                        _targetName = (string)properties[i].DefaultValue;
                        // note: we propgate here and do not set our own. 
                        PropogateChangeFlags(Keystone.Enums.ChangeStates.TargetChanged, Keystone.Enums.ChangeSource.Child);
                        break;
                    case "targetproperty":
                        _targetPropertyName = (string)properties[i].DefaultValue;
                        // note: we propgate here and do not set our own. 
                        PropogateChangeFlags(Keystone.Enums.ChangeStates.TargetChanged, Keystone.Enums.ChangeSource.Child);
                        break;
                    case "duration":
                        _duration = (float)properties[i].DefaultValue;
                        // note: we propgate here and do not set our own. 
                        PropogateChangeFlags(Keystone.Enums.ChangeStates.TargetChanged, Keystone.Enums.ChangeSource.Child);
                        break;
                }
            }
            mChanged = true; // TODO: i cant rmemeber what this is for
        }
        #endregion

        // todo: public members here should actually be "internal" and plugin should use GetProperty(id, propertyName)
        public float Duration { get { return _duration; } set { _duration = value; } }
        public abstract int KeyFrameCount { get; }

        /// <summary>
        /// friendly name of the Target (Entity1, Entity2, Model, Texture/TextureCycle/TextureMod
        /// If friendly name is used, Target _must_ be a descendant node of the AnimationClip's host Entity.
        /// </summary>
        public string TargetName
        {
            get { return _targetName; }
            set
            {
               // Debug.Assert(!String.IsNullOrEmpty(value));
                _targetName = value;
            }
        }

        /// <summary>
        /// id of the Target.  If string.IsNullOrEmpty (TargetID), then TargetID will be used over TargetName.
        /// NOTE: TargetID's can change when deserializing prefabs.  .TargetID property should only be used
        /// against Entity's dynamically generated Animations such as FlyTo viewpoint animations
        /// </summary>
//        public string TargetID
//        {
//            get { return _targetID; }
//            set
//            {
//                //Debug.Assert(!String.IsNullOrEmpty(value));
//                _targetID = value;
//            }
//        }
        
        /// <summary>
        /// Name of the Target Property (Diffuse, Opacity, SpecularPower, Texture/TextureCycle/TextureMod
        /// </summary>
        public string TargetPropertyName
        {
            get { return _targetPropertyName; }
            set
            {
                Debug.Assert(!String.IsNullOrEmpty(value));
                _targetPropertyName = value;
            }
        }

        // TODO: ideally i think we should not need an Update here.  It should be
        // handled with delegate methods set in the track based on the type
        // of Animation it is hosting
        internal abstract void Update(AnimationTrack track, object target);
    }
}
