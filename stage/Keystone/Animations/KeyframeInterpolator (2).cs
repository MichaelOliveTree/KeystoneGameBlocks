using System;
using Keystone.Extensions;
using Keystone.Elements;
using System.Diagnostics;

namespace Keystone.Animation
{
    /// <summary>
    /// Generic Type KeyFrame Interpolator class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KeyframeInterpolator<T> : AnimationClip 
    {
        public delegate T InterpolationMethod (T start, T end, double weight);
        public delegate void InterpolationApplicator(KeyframeInterpolator<T> interpolationClip, object target, T result);

        private InterpolationMethod mInterpolationHandler;
        private InterpolationApplicator mInterpolationApplicationHandler;
        
        private T[] mKeyFrames;
        public bool UseGlobalCoordinates {get; set;}
        public bool UseRelativeMovement;
        private T mStartFrame;
        
        public KeyframeInterpolator (string id) : base(id)
        {
        }

        public KeyframeInterpolator(string id, string targetPropertyName)
            : this(id)
        {
            // TODO: im not serializing/deserializing TargetPropertyName or is that being done elsewhere?
            TargetPropertyName = targetPropertyName;
        }

        #region ResourceBase members
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[1 + tmp.Length];
            tmp.CopyTo(properties, 1);

            properties[0] = new Settings.PropertySpec("keyframes", typeof(T[]).Name);

            if (!specOnly)
            {
                properties[0].DefaultValue = mKeyFrames;
            }

            return properties;
        }

        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                switch (properties[i].Name)
                {
                    // TODO: I think for the plugin, we need to pass in all keyframes at once when any single keyframe is modified (eg minVector and maxVector and everything in between)
                    //       Even if for near term we only support 2 keyframes in the plugin interface, we can build in code scenarios where we have multiple keyframes such as what we do
                    //       with rotation interpolation.
                    case "keyframes":
                        if (mKeyFrames is Types.Quaternion[] && properties[i].DefaultValue is Types.Vector3d[])
                        {
                            Types.Vector3d[] vecs = (Types.Vector3d[])properties[i].DefaultValue;
                            Types.Quaternion[] quats = new Types.Quaternion[2];
                            quats[0] = new Types.Quaternion(vecs[0].y * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS, vecs[0].x * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS, vecs[0].z * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS);
                            quats[1] = new Types.Quaternion(vecs[1].y * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS, vecs[1].x * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS, vecs[1].z * Keystone.Utilities.MathHelper.DEGREES_TO_RADIANS);
                            object o = quats;
                            mKeyFrames =(T[])o;
                        }
                        else
                            mKeyFrames = (T[])properties[i].DefaultValue;
                        // note: we propgate here and do not set our own. 
                        PropogateChangeFlags(Keystone.Enums.ChangeStates.TargetChanged, Keystone.Enums.ChangeSource.Child);
                        break;
                    // case "speed": // is speed apart of the overall animation and not just this interpolatin "clip"?
                    //   break;
                }
            }
        }
        #endregion

        
        public override int KeyFrameCount
        {
            get 
            {
                // TODO: this might need to be a float? i recall there was a bug with any float value rounding to 0 or 1
                if (mKeyFrames == null) return 0;
                return mKeyFrames.Length; 
            }
        }

        public virtual void AddKeyFrame(T keyframe)
        {
            if (mKeyFrames == null) mKeyFrames = new T[0];
            mKeyFrames = mKeyFrames.ArrayAppend(keyframe);        
        }

        public virtual void RemoveKeyFrame(T keyframe)
        {
            if (mKeyFrames == null) throw new ArgumentOutOfRangeException();
            if (keyframe == null) throw new ArgumentNullException();

            mKeyFrames = mKeyFrames.ArrayRemove(keyframe);

            if (mKeyFrames == null || mKeyFrames.Length == 0) mKeyFrames = null;
        }
        
        public virtual void RemoveAllKeyFrames ()
        {
        	mKeyFrames = null;
        }

        #region ResourceBase TypeName override
        public override string TypeName
        {
            get
            {
                return "KeyframeInterpolator_" + TargetPropertyName; 
                // generic type name must be constructed so
                // Keystone.Resource.Repository.Create(id, typename) can reconstruct this generic type
                // so we must use .FullName which will include the T parameter typenames
                return GetType().FullName; 
            }
        }
        #endregion

        #region KeyframeAnimation
        // the interpolation function to use
        public InterpolationMethod InterpolationHandler 
        { 
        	get { return mInterpolationHandler; } 
        	set { mInterpolationHandler = value; } 
        }

        // applies computed interpolation value to the target
        public InterpolationApplicator InterpolationApplicatorHandler 
        { 
        	get { return mInterpolationApplicationHandler; }
        	set { mInterpolationApplicationHandler = value; } 
        }

        public T Interpolate(int start, int end, double weight)
        {
            if (mInterpolationHandler == null) return default(T);
            if (mKeyFrames == null) return default(T);
            
            mStartFrame = mKeyFrames[start];
            
            return mInterpolationHandler(mKeyFrames[start], mKeyFrames[end], weight); 
        }

        // http://www.web3d.org/files/specifications/19775-1/V3.2/Part01/components/interp.html#ColorInterpolator
        internal override void Update(AnimationTrack track, object target)
        {
        	// TODO: Mercury Particle Engine does a similar thing only via it's "Modifiers" 
        	//       it does not have generic "applicator" as we do... it instead uses very specific
        	//       interpolators (eg rotation, color, translation) that target very specific properties
        	if (mKeyFrames == null || mKeyFrames.Length == 0) return;
            
            // TODO: i think this might need to be a float???
            int keyFrameCount = KeyFrameCount;
            System.Diagnostics.Debug.Assert (keyFrameCount > 1, 
                                             "KeyframeInterpolator.Update() - KeyFrame count < 1.  Cannot interpolate with only 1 KeyFrame.");
            
            if (Duration <= 0)
            {
                System.Diagnostics.Debug.WriteLine("KeyframeInterpolator.Update() - Animation duration == 0.");
                return;
            }

            float clipWeight = AnimationTrack.ComputeWeight (track.ElapsedSeconds, Duration, track.Animation.Looping);
            if (clipWeight == float.NaN) return;

            // get the start & end frame indices and the weight between the two
            int prevKeyFrame = (int)Math.Floor (clipWeight * (keyFrameCount - 1));
            int nextKeyFrame;

            // if looping == false and we're at last frame, nextframe must always == last frame 
            // and prevFrame second to last
            if (track.Animation.Looping == false && prevKeyFrame == keyFrameCount - 1)
            {
            	nextKeyFrame = keyFrameCount - 1; // last frame
            	prevKeyFrame = nextKeyFrame - 1; // second to last frame
            }
            else if (prevKeyFrame < keyFrameCount  - 1)
                nextKeyFrame = prevKeyFrame + 1;
            else
                nextKeyFrame = 0; // wrap back to start

            
            T interpolatedValue = Interpolate (prevKeyFrame , nextKeyFrame , clipWeight);   
            
            // Apply the new value to the target.  
            // See AnimationTrack.SetTarget and 
            // See AnimationTrack."#region static handlers" 
            mInterpolationApplicationHandler (this, target, startingValue, interpolatedValue);
        }
        #endregion
    }
}