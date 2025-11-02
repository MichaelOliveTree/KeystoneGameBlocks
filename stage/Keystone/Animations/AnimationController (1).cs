using System;
using System.Collections.Generic;
using MTV3D65;
using Keystone.Elements;
using Keystone.Entities;

namespace Keystone.Animation
{
    // NOTE: Regarding TurretBase, TurretHousing, TurretBarrel.... nested models is not something
    //       i think i want to support. I'm reminded of why i was going the nested entity only route.
    //       In the long run, my design goal is to have hyper interactive worlds where everything
    //       is an entity.  We don't cheat by building complex models as actors and animating them and
    //       making them impossible to mix and match individually.  A barrel is a barrel and can be
    //       replaced as an entity, not as a mesh.  It has individual statistics and physics capabilities.
    //       This will complicate some things though...  designing a Turret script is easy if we have
    //       just one entity with no child entities.  however, we can design things more realisticly
    //       such as having a turret that rotates and is attached to a base that is more of a dumb mount
    //       entity and not really part of the "turret."  Then we can create a universal joint for it
    //       to sit on, with limits on it's rotation.  Then we can attach things to those joints.
    // NOTE: THe KEY however is that in our scripts, it's easier to reference child entities in scripts
    //       and rotate them or whatever than it is to reference nested models.  Child entities places
    //       a logical constraint when designing things that will help the designer remembering to 
    //       design things in a logical and careful way rather than just going overboard with a mess
    //       of nested models.
    //
    // NOTE: The key when thinking about the AnimationController and scripting the AniamtionController
    //       is that the animation controller and it's script will assume the layout of the overall
    //       entity hierarchy.  That is perfectly ok since the scripts can be 1:1 unique for each
    //       entity type.  Therefore in this way, the AnimationController or the Entity script can
    //       decide what animations to play, can decide which visual sub-models to use, and thus will know
    //       from frame to frame which animations and models are available to it and how to blend
    //       between various animations.
    //
    // http://docs.garagegames.com/torque-3d/official/content/documentation/World%20Editor/Editors/ShapeEditor.html
    // http://animationcomponents.codeplex.com/
    // http://unity3d.com/support/documentation/Manual/Character-Animation.html
    // havok aniamtion behaviors - http://www.youtube.com/watch?v=sclYyTiqRrw&lr=1
    //     - The idea here is our animations are tied to our behavior tree.  We dont need any specific
    //       animation behavior tree implementation.  Instead our main behavior tree + scripts handles this as well.
    //
    // Entity
    //     |_AnimationController (aka AnimationSet) (.StartClip( model[i], model[i].Animations["rotateTurretYAxis"]);
    //                                              (.CrossFade(model[i], model[i].Animations["fire"], TimeSpan.FromSeconds(0.3f));
    //               - the key is the AnimationController needs to be able to
    //                 affect the proper Models based on the frame to frame Selection.
    //               - thus it must deal with the proper Sequence selected and the proper Model
    //               - But overall, the controller binds all the animation  of a single logical entity
    //                 together even if it has several virtual moving parts (i.e Models).
    //                - AnimationController is not a Node.  
    //     
    //            |_Tracks ActiveTracks;  // used to blend, or run multiple animations against multiple sub-models
    //            |_AnimationHandler[key] AnimHandler; // holds a seperate state for every unique named animation below it
    //                                            // this is because a given Entity can have multiple animations running for multiple models in a sequence
    //                                            // wait, what is the difference between a Track and an AnimationHandler?
    //
    //     |_ModelSequence.Select(out animationSet[]) <-- (doesn't select between children, rather returns array of all Models)
    //                |_ModelSwitch - Select between damage paths for Door
    //                         |__ModelLOD - Un-Damaged Path
    //                                  |__Model - LOD0
    //                                       |__Animations[]
    //                                       |__Actor3d
    //                                       |__Appearance
    //                                  |__Model - LOD1
    //                                       |__Actor3d
    //                                       |__Appearance
    //                         |__ModelLOD - Medium Damage Path
    //                         |__ModelLOD - Destroyed Path
    //                |_ModelSelector - Select between damage paths for Frame
    //
    //     |_ModelSwitch.Select(out animationSet[])  <-- Returns a Model.  I think an entity should select the current animationSet from the same set of animations
    //                |__ModelLOD - Un-Damaged Path
    //                         |__AnimationSet  <-- should Model.Animations[] be AnimationSet? with AnimationController in Entity storing TrackInfo which would store the specific model a track was associated with
    //                         |__Model - LOD0
    //                              |__Actor3d
    //                              |__Appearance
    //                         |__Model - LOD1
    //                              |__Actor3d
    //                              |__Appearance
    //                |__ModelLOD - Medium Damage Path
    //                         |__AnimationSet
    //                         |__Model - LOD0
    //                              |__Actor3d
    //                              |__Appearance
    //                         |__Model - LOD1
    //                              |__Actor3d
    //                              |__Appearance
    //                |__ModelLOD - Destroyed Path
    //                         |__AnimationSet <-- contains a destroy animation
    //                         |__Model - LOD0
    //                              |__Actor3d
    //                              |__Appearance
    //                         |__Model - LOD1
    //                              |__Actor3d
    //                              |__Appearance
    //
    // * NOTE: ModelLOD is the only type where Actor3d's under the respetive lod Models must contain
    //         the same animation names so that they match across all lod's.
    //
    // AnimationController is child of an Entity or a ModelLOD.  It is 1:1 with a TVActor or actor duplicate
    // When adding a ModelBase is responsible for ensuring that all LOD's have matching animation names/ids
    // a ModelBase is also responsible for ensuring that all LOD's have matching bone names for 
    // those bone's that can be used as Attachment points.
    // the bone ID's can be different however.  One useful assert is to verify that the actor LOD 
    // with the fewest bonecount has a corresponding bone in _all_ higher LOD versions.  
    // Of course "attachments" are done via AddChild via Entities so when getting boneMatrix we
    // must use name or some id mapping
    public class AnimationController 
    {
        internal class AnimationEventArgs : EventArgs
        {
            public string EntityID;
            public string EntityName;
        }


        public EventHandler mAnimationEventHandler; // animation finished
        private Entity mEntity;
        private AnimationEventArgs mEventArgs;

        // each active track represents a different Animation that is playing
        private Dictionary<string, AnimationTrack> mTracks;

       
        // BehaviorTrees + Scripts control the selection of animations, but the AnimationController
        // maintains state about the current set of running animations which are useable by
        // the BehaviorTree & Scripts.

        // a very good site 
        // http://www.jkarlsson.com/Articles/loadframes.asp
        // controller holds state information and is thus 1:1 with an IAnimated Entity

        private bool mOverideBehaviors;


        // scroll down in thread (see link) to see discussion of an animation state machine/behavior tree
        // http://forum.unity3d.com/threads/66504-Animation-system-wish-list
        // this must be tied to a specific duplicate yes?
        public AnimationController(Entity hostEntity)
        {
            if (hostEntity == null) throw new ArgumentNullException();

            mTracks = new Dictionary<string, AnimationTrack>();
            mEntity = hostEntity;

            mEventArgs = new AnimationEventArgs();
            mEventArgs.EntityID = mEntity.ID;
            mEventArgs.EntityName = mEntity.Name;

            mAnimationEventHandler = null;
        }


        public bool OverrideBehaviors 
        { 
            get { return mOverideBehaviors; } 
            set { mOverideBehaviors = value; } 
        }


        internal AnimationTrack[] ActiveTracks
        {
            get 
            {
                if (mTracks == null) return null;
                AnimationTrack[] array = new  AnimationTrack[mTracks.Count];
                mTracks.Values.CopyTo (array, 0);
                return array;
            }
        }

        /// <summary>
        /// Adds an animation and generates a track to host it.
        /// </summary>
        /// <param name="anim"></param>
        public void Add(Animation anim)
        {
            if (mTracks.ContainsKey(anim.Name)) throw new Exception("AnimationController.Add() -- Animation with same key already exists.");
            AnimationTrack track = new AnimationTrack(this, anim);

            mTracks.Add(anim.Name, track);
    		ReMapAnimationsToTargets(track);  		
        }

        public void Remove(Animation anim)
        {
            Remove(anim.Name);
        }

        public void Remove(string animation)
        {
            if (mTracks.ContainsKey(animation))
            {
                mTracks.Remove(animation);
            }
        }

        internal void ReMapAnimationsToTargets(AnimationTrack track)
        {
            List<Node> descendants = new List<Node>();
            // TODO: any BoundTransformGroup can be mapped and
            // a SpriteSheet can also be mapped although im not sure
            // if we 
            // TODO: what if the target is not a child? can we set the track to autoMapTarget = false? and allow only the target manually set?
            Group.GetDescendants(mEntity.Children, new Type[] { typeof(Entity) }, ref descendants);
            ReMapAnimationToTarget (track, descendants.ToArray());
        }
        
        // TODO: This is very much like the X3D ROUTE instructions and ideally id like to make this
        //       more generic than just tying to AnimationController.
        internal void ReMapAnimationsToTargets()
        {
            List<Node> descendants = new List<Node>();
            // TODO: any BoundTransformGroup can be mapped and
            // a SpriteSheet can also be mapped although im not sure
            // if we 
            // TODO: what if the target is not a child? can we set the track to autoMapTarget = false? and allow only the target manually set?
            Group.GetDescendants(mEntity.Children, new Type[] { typeof(Entity) }, ref descendants);
            ReMapAllAnimationsToTargets(descendants.ToArray());
        }

                
        internal void ReMapAllAnimationsToTargets (Node[] targets)
        {
            if (mTracks == null || mTracks.Count == 0) return;

            // clear all targets if there are none.  This should be virtually impossible
            // since the Animation itself wouldn't even exist which means the Track won't exist
            if (targets == null)
                foreach (AnimationTrack track in mTracks.Values)
                    track.ClearTargets ();
            else
            {
                foreach (AnimationTrack track in mTracks.Values)
                {
                	ReMapAnimationToTarget(track, targets);
                }
            }
        }
        
        internal void ReMapAnimationToTarget(AnimationTrack track, Node[] targets)
        {
            // BonedAnimation should be treated as a special type of
            // KeyframeInterpolator ideally.  This way we can also add
            // other sequences to it such as translating y to climb ladder while
            // the character is climbing, or to translate y and x while the character
            // is jumping.  Etc.  
            AnimationClip[] clips = track.Animation.AnimationClips;
            if (clips == null || clips.Length == 0) return;

            bool hasChanged = IsTrackTargetChanged (track, targets);
            if (hasChanged == false) return;
            
            // re-initialize track.Targets after we've determined the previous
            // target assignment has changed
            track.Targets = new object[clips.Length];
            
            for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                bool found = false;

            	if (clips[clipIndex].TargetName == this.mEntity.Name)
                {
                    track.SetTarget(clipIndex, this.mEntity, clips[clipIndex].TargetPropertyName);

                    // TODO: here what about mapping target's affected property?
                    // i think the .Animation.TargetProperty should exist for that purpose
                    // and then somehow based on the type of property, we need to assign the proper
                    // interpolation function
                    found = true;
                }
                // friendly target name is not null.  find the descendant that has this name
                else if (string.IsNullOrEmpty (clips[clipIndex].TargetName) == false)
                {
                    // try for the descendants which were passed in as arguments instead
                    for (int j = 0; j < targets.Length; j++)
                        if (string.IsNullOrEmpty(clips[clipIndex].TargetName) == false &&
                            string.IsNullOrEmpty(targets[j].Name) == false &&
                            clips[clipIndex].TargetName == targets[j].Name)
                        {
                            track.SetTarget(clipIndex, targets[j], clips[clipIndex].TargetPropertyName);
                            found = true;
                            break;
                        }
                }

                if (found == false) track.ClearTargets ();
                found = false; // reset our found flag
            }
        }

        private bool IsTrackTargetChanged (AnimationTrack track, Node[] targets)
        {
        	// .Targets was not previously set.  Return "true" so it can be mapped.
        	if (track.Targets == null) return true;
        	
        	AnimationClip[] clips = track.Animation.AnimationClips;
        	for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
        		// this track's target is not set.  Return true so that it can be mapped.
        		if (track.Targets == null || 
        		    clipIndex > track.Targets.Length - 1 || 
        		    track.Targets[clipIndex] == null) return true;
        		
                bool changed = false;

                // animation clip mapped to the entity itself?
                if ( clips[clipIndex].TargetName == this.mEntity.Name)
                {
                	if (changed == false && track.Targets[clipIndex] != this.mEntity)
                    	changed = true;
                }
                // friendly target name is not null.  find the descendant that has this name
                else if (string.IsNullOrEmpty (clips[clipIndex].TargetName) == false)
                {
                    // try for the descendants which were passed in as arguments instead
                    for (int j = 0; j < targets.Length; j++)
                        if (string.IsNullOrEmpty(clips[clipIndex].TargetName) == false &&
                            string.IsNullOrEmpty(targets[j].Name) == false &&
                            clips[clipIndex].TargetName == targets[j].Name)
                        {
                    		if (changed == false && track.Targets[clipIndex] != targets[j])
                				changed = true;
                            break;
                        }
                }

                if (changed) return true;
            }
        	
        	return false;
        }
        
        
        //public int GetAnimationIDByName(string sName)
        //{
        //    return mGeometry._actor.GetAnimationIDByName(sName);
        //}

        //public void RenameAnimation(int index, string newName)
        //{
        //    mGeometry._actor.RenameAnimation(index, newName);
        //}
        
        ///// <param name="animationSource">The AnimationID of an existing animation from which to create the new animation.  
        /////This value is usually 0 for an animation that has no defined animations.</param>
        //public bool GetAnimationRangeInfo(int animationID, ref int animationSource, ref float startFrame,
        //                                  ref float endFrame)
        //{
        //    bool success =
        //        mGeometry._actor.GetAnimationRangeInfo(animationID, ref animationSource, ref startFrame, ref endFrame);
        //    return success;
        //}


        ///// <summary>
        ///// Makes a cutom animation using a startframe and endframe. Can be used with models that have no animation-id/names, 
        ///// to control the animations.  Preferable to using SetCustomAnimation() because here we can retreive the ID
        ///// and use that to set AnimationID in the future.
        ///// </summary>
        ///// <param name="sourceAnimationID">The animation ID of an existing animation from which to create the new animation.  
        ///// This value is usually 0 for an animation that has no defined animations.</param>
        ///// <param name="newAnatimonName"></param>
        ///// <param name="startFrame">
        ///// By specifying the source, the startFrame's and endFrames are relative to that animationSource
        ///// so although the aniamtionSource may have it's own start frame of 50 and end 100
        ///// the new animation if specified to be the first half of the source woud specify
        ///// startFrame 0 and endFrame 50</param>
        ///// <param name="endFrame"></param>
        ///// <returns>An animation ID.  The lowest return id in a model that has only one intrinsic animations
        /////  is 1.  </returns>
        //public int AddAnimationRange(int sourceAnimationID, string newAnatimonName, float startFrame, float endFrame)
        //{
        //    // TODO: im thinking maybe this should be a private method here
        //    // that gets called automatically when a new child ActorAnimation is added
        //    // and when those child's properties change to warrant the range being modified...
        //    // or something..

        
       
        //    // TODO: Actor3d should be passed in so that this AddAnimationRange can be
        //    // controlled via BonedEntity.ActorAnimation object
        //    return mGeometry._actor.AddAnimationRange(sourceAnimationID, startFrame, endFrame, newAnatimonName);
        
        //    // NOTE: Can delete intrinsic animations?  Probably not only the ones created
        //    // with AddAnimationRange!
        //    //_actor._actor.DeleteAnimationRange ()
        //}

        //// Only custom (virtual) animations created from animation ranges can be deleted or added.  Existing animations in the actor cannot.
        //public void DeleteAnimationRange(int animationID)
        //{
        //    mGeometry._actor.DeleteAnimationRange(animationID);
        //}

        //public void SetCustomAnimation(float startFrame, float endFrame)
        //{
        //    // this does not add an animation but is used similarly to play a subset of 
        //    // the main 0 index full animation.  If not looping then u have to
        //    // respecify the animation start/end
        //    // I think this is a rather obsolete version compared to AddAnimationRange
        //    // or one that is designed to be much easier to start using if you're new to tv3d.
        //    mGeometry._actor.SetCustomAnimation(startFrame, endFrame);
        //}

        //public void ExportAnimations(string sDatasource)
        //{
        //    mGeometry._actor.ExportAnimations(sDatasource);
        //}

        //public void ExportAnimations(string sDatasource, int iSpecificAnimation)
        //{
        //    mGeometry._actor.ExportAnimations(sDatasource, iSpecificAnimation);
        //}

        //public bool ImportAnimations(string sDatasource)
        //{
        //    return mGeometry._actor.ImportAnimations(sDatasource);
        //}

        //public bool ImportAnimations(string sDatasource, bool bAppendAnimations)
        //{
        //    return mGeometry._actor.ImportAnimations(sDatasource, bAppendAnimations);
        //}


        #region Animation Controls
        //public void BlendAnimationTo(int iNewAnimation)
        //{
        //    mGeometry._actor.BlendAnimationTo(iNewAnimation);
        //}

        //public void BlendAnimationTo(int iNewAnimation, float fTransitionLengthSec)
        //{
        //    mGeometry._actor.BlendAnimationTo(iNewAnimation, fTransitionLengthSec);
        //}

        public string CurrentAnimation 
        {        	
        	get 
        	{
	        	if (mTracks == null || mTracks.Count == 0) return null;
	        	
	        	foreach (AnimationTrack track in mTracks.Values)
	            {
	        		if (track.State == AnimationTrack.TrackState.Playing)
	        			return track.Name;
	            }
	        	
	        	return null;
        	}
        }
        
        public bool IsPlaying () // at least one animation is playing
        {
        	if (mTracks == null || mTracks.Count == 0) return false;
        	
        	foreach (AnimationTrack track in mTracks.Values)
            {
        		if (track.State == AnimationTrack.TrackState.Playing)
        			return true;
            }
        	
        	return false;
        }
        
        public bool Contains (string animationName)
        {
        	if (mTracks == null || mTracks.Count == 0) return false;
        	return mTracks.ContainsKey (animationName);
        }
        
        private string GetAnimationNameAtIndex (int animationIndex)
        {
        	if (mTracks == null) 
        		throw new Exception ();
        	
        	if (animationIndex > mTracks.Count - 1) 
        		throw new Exception();
        	
        	int count = 0;
        	foreach (AnimationTrack track in mTracks.Values)
        	{
        		if (count++ == animationIndex )
        		{
	        		string animationName = track.Name;
	        		return animationName;
        		}
        	}
        	
        	return null;
        }

        public float GetLength(string animationName)
        {
            AnimationTrack track;
            bool result = mTracks.TryGetValue(animationName, out track);

            if (result)
                return mTracks[animationName].Duration;
            else return 0;
        }

        public void Play(int animationIndex)
        {
        	string animationName = GetAnimationNameAtIndex(animationIndex);
        	
        	if (string.IsNullOrEmpty(animationName )) throw new ArgumentOutOfRangeException ();
        	
        	Play (animationName);
        }

        public void Play(int animationIndex, bool loop = false)
        {
            string animationName = GetAnimationNameAtIndex(animationIndex);

            if (string.IsNullOrEmpty(animationName)) throw new ArgumentOutOfRangeException();

            Play(animationName, loop);
        }

        public void Play(string animationName, bool loop = false)
        {
            // TODO: we want to stop any previous animations from playing if animationName == null... does "return" here do that? I don't think so...
            if (string.IsNullOrEmpty(animationName)) return;

            // NOTE: the AnimationName is ensured not to collide upon creation in the Editor.
            // When user goes to create a new animation, they must enter a name and that name
            // is checked immediately and before the animation is created so that there is opportunity
            // to inform user of invalidity of name.

            // does the current track with this animation index already exist?
            // TODO: for something like a TextureAnimation explosion, the following check results
            // in the same track being played again and thus wont have independant keyframes... every
            // shared instance will be the same!  
            if (mTracks.ContainsKey(animationName) == false || mEntity == null)
            {
                System.Diagnostics.Debug.WriteLine("AnimationController.Play() - Verify that the animation controller has been added to an Entity first and that the Animation Track has been added to the controller first!");
                return;
            }
            AnimationTrack track = mTracks[animationName];
            Animation anim = track.Animation;
            // anim.TargetName;1

            // the track can have a named Source/Target but is it set? 
            // TODO: when an animation's target is changed, we need to re-map
            // that should be initiated by the entity on flag evaluation
            if (track.Targets == null)
            	ReMapAnimationsToTargets (track);

            if (track.Targets != null && track.Targets.Length > 0)
            {
                track.Play(loop);
                if (track.State != track.PreviousState)
                    if (mAnimationEventHandler != null)
                        mAnimationEventHandler.Invoke(track, mEventArgs);

                track.PreviousState = track.State;
            }
        }

        public void Stop (int animationIndex)
        {
        	string animationName = GetAnimationNameAtIndex(animationIndex);
        	
        	if (string.IsNullOrEmpty(animationName )) throw new ArgumentOutOfRangeException ();
        	Stop(animationName);
        }
        
        public void Stop(string animationName)
        {
 			if (mTracks.ContainsKey(animationName)  == false || mEntity == null)
                throw new Exception ("AnimationController.Play() - Verify that the animation controller has been added to an Entity first and that the Animation Track has been added to the controller first!");

            AnimationTrack track = mTracks[animationName];
            track.Stop ();

            if (track.State != track.PreviousState)
                if (mAnimationEventHandler != null)
                    mAnimationEventHandler.Invoke(track, mEventArgs);

            track.PreviousState = track.State;
        }


        public void Update(double elapsedSeconds)
        {

            if (mTracks == null || mTracks.Count == 0) return;

            // iterate through all playing tracks and fire an event for any that have completed 
            foreach (AnimationTrack track in mTracks.Values)
            {
                // if track.Update() returns a TrackState enum value, then here we can
                // invoke the eventHandler for each track as needed.
                AnimationTrack.TrackState state = track.Update(elapsedSeconds);
                if (state != track.PreviousState)
                    if (mAnimationEventHandler != null)
                        mAnimationEventHandler.Invoke(track, mEventArgs);

                track.PreviousState = state;
            }


 //           // determine desired animation state based on Entity actions.  This aspect here should be scriptable

 //           // if previous animation != currentAnimation 
 //           // 1) use BlendToAnimation to transition
 //           // 2) use the call to PlayAnimation with the proper speed

 //           // if the entitie's selectedLOD has changed, then we may need to get the keyframe from the previous LOD first so we can set it 
 //           // the problem of course is, if you're transitioning to im not sure how you'd do that unless you can restore that as well
 //           // for that seems to me you'd need to be able to know when the transition ended

 //           // here in update we only should "PlayAnimation" if the animation has changed
 //           if (_currentAnimation != _lastAnimation)
 //           {
 //               if (_lastAnimation >  -1 )
 //                   _animations[_lastAnimation].Stop(entity);

 //               if (_currentAnimation >  -1)
 //                   _animations[_currentAnimation].Play(entity);

 //               _lastAnimation = _currentAnimation;
                
 //           }

 //           if (_currentAnimation == -1) return;

 //           // update the current animation if it's playing
 //           _animations[_currentAnimation].Update(entity, elapsedMilliseconds);

 //           // Perhaps also have overrides for Blending  
 //           //_animations[_currentAnimation].Update(entity, elapsedMilliseconds, bool blendingAnimations)

           
        }
        #endregion
    }
}