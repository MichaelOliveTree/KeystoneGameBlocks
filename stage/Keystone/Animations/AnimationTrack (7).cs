using System;
using Keystone.Elements;


namespace Keystone.Animation
{
    /// <summary>
    /// An "AnimationTrack" is only responsible for controlling playback of one 'Animation'.
    /// However 'Animation' node can contain a sequence of 'AnimationClip' child nodes.
    /// Instances of this AnimationTrack class hold per instance state information and cannot be shared.
    /// </summary>
    internal class AnimationTrack  // current animation info.  Normal Animations.cs are just data holders.
    // it is in TrackInfo that we track instance data.
    // a TrackInfo is created for active animations.  When a particular animation
    // is no longer active, it's corresponding track gets removed.
    {

        // states
        public enum TrackState
        {
            None,
            Error, // often when a clip does not have a Duration specified (eg Duration == 0.0f)
            Playing,
            Finished,//  requires all AnimationClips within the Animation to be completed
                     // but since AnimationClip's don't track themselves, doesnt that mean
                     // this Track must do it?  This assumes all clips are not same length exactly.
                     // i.e. that we dont require all of them to be same length.
            Stopped, 
            Paused,
        }

        public enum TrackDirection
        {
            Forward,
            Reverse
        }

        
        public AnimationController Controller;
        public Animation Animation;  // AnimationIndex can be grabbed from it
        public object[] mTargets;   // 1:1 with each AnimationClip.  Targets tend to be of Transform.cs (derived types of Transform like like Entities, Models)
                                    //  or Material colors and Lights colors. 
                                  
        public bool Enabled;
        public TrackState State;
        public TrackState PreviousState;
        public TrackDirection Direction;

        // blending
        public int BlendToIndex;
        public double ElapsedSeconds;

        // timing
        public float Speed;      // speed can override default speed of the animation
        public float Weight;  // current keyframe 

        //private System.Diagnostics.Stopwatch mStopWatch; 

        #region static handlers
        KeyframeInterpolator<Keystone.Types.Vector3d>.InterpolationApplicator mTranslationHandler =
        	(clip, transform, startingValue, value) => 
        {
        	// should we interpret the "value" as local or global coordinate?
        	// global is easier to move exterior things because we dont have to worry about 
        	// computing different local frames for each zone we'll be crossing and
        	// we just set global and allow entities to convert to local themselves
        	if (transform as Transform != null)
        	{
        		Transform t = (Transform)transform;
        		bool useGlobalCoordinates = clip.UseGlobalCoordinates;
            	if (useGlobalCoordinates)
            		t.GlobalTranslation = value;
            	else
    	        	t.Translation = value;
        	}
        };

        KeyframeInterpolator<Keystone.Types.Vector3d>.InterpolationApplicator mScaleHandler =
        	(clip, transform, startingValue, value) => {
        	if (transform as Transform != null)
        	{
        		Transform t = (Transform)transform;
        		t.Scale = value;
        	}
    	};

        KeyframeInterpolator<Keystone.Types.Quaternion>.InterpolationApplicator mRotationHandler =
        	(clip, transform, startingValue, value) => {
        	if (transform as Transform != null)
        	{
        		Transform t = (Transform)transform;
        		t.Rotation = value;
        	}
        };
        
        // Light colors
        KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mLightDiffuseHandler =
        	(clip, light, startingValue, value) => {
        	if (light as Light != null)
        	{
        		Light l = (Light)light;
        		l.Diffuse = value;
        	}
    	};
    	
    	
    	KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mLightAmbientHandler =
        	(clip, light, startingValue, value) => {
        	if (light as Light != null)
        	{
        		Light l = (Light)light;
        		l.Ambient = value;
        	}
    	};
    	
    	KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mLightSpecularHandler =
        	(clip, light, startingValue, value) => {
        	if (light as Light != null)
        	{
        		Light l = (Light)light;
        		l.Specular = value;
        	}
    	};
    	
        // Materials
        KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mMaterialDiffuseHandler =
        	(clip, material, startingValue, value) => {
        	if (material as Material != null)
        	{
        		Material m = (Material)material;
        		m.Diffuse = value;
        	}
    	};
    	
    	KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mMaterialAmbientHandler =
        	(clip, material, startingValue, value) => {
        	if (material as Material != null)
        	{
        		Material m = (Material)material;
        		m.Ambient = value;
        	}
    	};
    	
    	KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mMaterialSpecularHandler =
        	(clip, material, startingValue, value) => {
        	if (material as Material != null)
        	{
        		Material m = (Material)material;
        		m.Specular = value;
        	}
    	};
    	
    	KeyframeInterpolator<Keystone.Types.Color>.InterpolationApplicator mMaterialEmissiveHandler =
        	(clip, material, startingValue, value) => {
        	if (material as Material != null)
        	{
        		Material m = (Material)material;
        		m.Emissive = value;
        	}
    	};
    	
        #endregion 

        internal AnimationTrack(AnimationController controller, Animation animation)
        {
            if (controller == null || animation == null) throw new ArgumentNullException();

            Controller = controller;
            Animation = animation;
           
            mTargets = null;

            Enabled = true;
            PreviousState = State = TrackState.None;

            Direction = TrackDirection.Forward;

            BlendToIndex = -1;
            ElapsedSeconds = 0;

            Speed = animation.Speed;
            Weight = 0;
        }


        public string Name { get { return Animation.Name; } }

        public float Duration { get { return Animation.Duration; } }

        public object[] Targets { get { return mTargets; } set { mTargets = value; } }

        public void ClearTargets() 
        {
            mTargets = null;
        }

        public void SetTarget(int index, object target, string propertyName)
        {
            mTargets[index] = target;

            AnimationClip clip = Animation.AnimationClips[index];
            
            // TODO: BonedAnimation has no target property, what happens here when a BonedAnimation is the AnimationClip?
            
            // TODO: you know ideally, propertyName would be specified with the Animation
            //       itself.  Then during deserialization, when (if) this value is set,
            //       a static one is used that is created in the InterpolationAnimation.

            // TODO: why not all clip if of type InterpolatorAnimation, allow set of InterpolationHandler based on an enum setting contained in it?
            switch (propertyName)
            {
                case "translation":
            		((KeyframeInterpolator<Keystone.Types.Vector3d>)clip).InterpolationHandler = Keystone.Types.Vector3d.LerpSmoothDeceleration;
                    ((KeyframeInterpolator<Keystone.Types.Vector3d>)clip).InterpolationApplicatorHandler = mTranslationHandler;
                    break;

                case "scale":
                    ((KeyframeInterpolator<Keystone.Types.Vector3d>)clip).InterpolationHandler = Keystone.Types.Vector3d.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Vector3d>)clip).InterpolationApplicatorHandler = mScaleHandler;
                    break;

                case "rotation":
                    ((KeyframeInterpolator<Keystone.Types.Quaternion>)clip).InterpolationHandler = Keystone.Types.Quaternion.Slerp2;
                    ((KeyframeInterpolator<Keystone.Types.Quaternion>)clip).InterpolationApplicatorHandler = mRotationHandler;
                    break;
               
               // Materials
               	case "diffuse":
               	    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mMaterialDiffuseHandler;
                    break;
                case "ambient":
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mMaterialAmbientHandler;
                    break;
                case "specular":
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mMaterialSpecularHandler;
                    break;
                case "emissive":
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mMaterialEmissiveHandler;
                    break;
                // case "power":
                // case "alpha":
                
                // Lights
                case "diffuse":
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mLightDiffuseHandler;
                    break;
                case "ambient":
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mLightAmbientHandler;
                    break;
                case "specular":
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationHandler = Keystone.Types.Color.Lerp;
                    ((KeyframeInterpolator<Keystone.Types.Color>)clip).InterpolationApplicatorHandler = mLightSpecularHandler;
                    break;
                    
               case null:
                    // is a BonedAnimation?
                    break;
            }
        }

        public void Stop()
        {
            State = TrackState.Stopped;
            
            AnimationClip[] clips = Animation.AnimationClips;
            if (clips == null || clips.Length == 0) return;
            
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] is BonedAnimation)
                {
                    // TODO: this is hard coded to mTargets[0]? and i dont remember why or whether this is correct . investigate
                    Model model = (Model)mTargets[0];  // TODO: BonedAnimation should be renamed BonedAnimationClip
                    Actor3d actor = (Actor3d)model.Geometry;
                    Actor3dDuplicate duplicate = actor[model.ID];
                    duplicate.mDuplicateActor.StopAnimation ();
                }
            }
        }

        //public void Play(float startWeight, float endWeight, bool loop = false)
        //{
        //    // we need to seed the ElapsedSeconds needed to be at startWeight and the Duration needed to reach endWeight
        //    // hmmm... hing is, we might as well just make seperate Animations than do this.  Simply name them 
        //    // "rotate_45", "rotate_90", "rotate_115", ... "rotate_360"
        //    // Then obviously if we need to load a specific missile because it has a unique warhead, we would need to 
        //    // based on the current rotary missile magazine rotation, determine which series of animations we need to play
        //    // to reach the desired rotation.

        //    // So next thing to think about is sequencing animations such as the rotary rotation followed by the delivery
        //    // into the missile launcher breach.



        //}


        public void Play(bool loop = false)
        {
            // if the State was already playing, this resets it
            State = TrackState.Playing;
            this.Weight = 0;
            this.ElapsedSeconds = 0;
            
            // target must be assigned based on the friendly name of Animation.TargetName
            if (mTargets == null) return;
            
            // TODO: if the speed has changed, change it;
            
            // TODO: we could use double dispatch here to get the correct target type
            // just make AnimationTrack implement ITraverser and then
            // Target.Traverse(this);
            Animation.Looping = loop;
            AnimationClip[] clips = Animation.AnimationClips;
            if (clips == null || clips.Length == 0) return;

            for (int i = 0; i < clips.Length; i++)
            {
            	BonedAnimation ba = clips[i] as BonedAnimation;
                if (ba != null)
                {
                    // do we have to initialize the non intrinsic tvactor animation?
                    //if (ba.IsIntrinsicAnimation && ba.
                    // TODO: this is hard coded to mTargets[0]? and i dont remember why or whether this is correct . investigate
                    Model model = (Model)mTargets[0];  // TODO: BonedAnimation should be renamed BonedAnimationClip
                    Actor3d actor = (Actor3d)model.Geometry;
                    if (ba.Initialized == false)
                    	ba.Initialize(model);
                    
                    Actor3dDuplicate duplicate = actor[model.ID];
                    if (duplicate.mDuplicateActor == null) return;
                    duplicate.mDuplicateActor.SetAnimationID(ba.Index);
                    duplicate.mDuplicateActor.PlayAnimation(Animation.Speed);
                    duplicate.mDuplicateActor.SetAnimationLoop (Animation.Looping);
                    //actor._actor.SetAnimationID(Animation.Index);
                    // NOTE: calling Play has nothing to do with our .Update() speed problem.
                    // That is a product of sharing the same underlying Actor3d in all instances.
                    // Thus above we use duplicate._actor.PlayAnimation
                    //actor._actor.PlayAnimation(Animation.Speed);
                }
            }
        }


        public TrackState Update(double elapsedSeconds)
        {
        	if (mTargets == null) return TrackState.None; // during deserialization this can be null initially while track has already started to receive this Update() call
        	
            // some tracks can be flagged to not update if the model they are animating is not visible...
            // this is not viable for all types of animations because some animations could actually
            // result in the model moving into view!  // TODO: should look into doing this for TVActors especially
            if (State == TrackState.Stopped || State == TrackState.Finished) return State;


            if (Duration <= 0)
            {
                //System.Diagnostics.Debug.WriteLine("AnimationTrack.Update() Animation duration is 0.0");
                State = TrackState.Error;
            }
            else if (State == TrackState.Playing)
            {
                if (ElapsedSeconds == 0.0f)
                {
                    //
                }    
                // TODO: is this ElapsedSeconds missing out on some frames because Simulation.Update (gameTime.ElapsedSeconds) is run at a different frequency?
                //       Otherwise, why is my open and close door animations so slow?
                ElapsedSeconds += elapsedSeconds;

                Weight = ComputeWeight(ElapsedSeconds, Duration, Animation.Looping);

                // have we reached the end of the animation?
                if (State != TrackState.Error && Animation.Looping == false)
                {
                    if (Weight >= 1.0f)
                    {
                        // animation is finished and we can update TrackState. 
                        State = TrackState.Finished;

                        // DO NOT RETURN, we must render final frame at Weight = 1.0 
                        // or the animation will not have made it to final frame!
                        // return State; <-- no
                    }
                }
            }

            AnimationClip[] clips = Animation.AnimationClips;
            if (clips == null || clips.Length  == 0) return TrackState.None;

           	// NOTE: individual clips in a single Track may have shorter durations but always guaranteed
			//       to never exceed the track.Duration since it's always equal to max clip[] item duration
			//       TODO: but what if we want to play each clip in an Animation one after another rather than all at same time?
            for (int i = 0; i < clips.Length; i++)
            {
            	if (mTargets[i] == null) continue; // targtes can be null initially during deserialization while this Update() is being called

            	if (State == TrackState.Stopped && clips[i] as BonedAnimation != null)
            	{
            		Model model = (Model)mTargets[0]; 
                    Actor3d actor = (Actor3d)model.Geometry;
                    if (((BonedAnimation)clips[i]).Initialized)
                    {
                    	Actor3dDuplicate duplicate = actor[model.ID];
                    	duplicate.mDuplicateActor.StopAnimation();
                    }
            	}
               	clips[i].Update (this, mTargets[i]);
            }

            return State;
        }

        internal static float ComputeWeight (double elapsedSeconds, double duration, bool wrap)
        {
        	if (wrap) return (float)((elapsedSeconds % duration) / duration);
        	
        	double result = elapsedSeconds / duration ;
        	bool endOfAnimation = EndOfAnimation(result);
            if (endOfAnimation)
        		result = 1.0f;
            			
			return (float)result ;
        }
        
        internal static bool EndOfAnimation( double weight)
        {
            return weight >= 1.0f;
        }

        public void Reset()
        {
            //mStopWatch.Reset ();
            Weight = 0;
            ElapsedSeconds = 0;
        }
    }
}