using System;
using Keystone.Animation;
using Keystone.Elements;


namespace Keystone.Entities
{
    public class BonedEntity : ModeledEntity
    {
        protected AnimationController _animationController;

        private int _boundBoneID = -1; // used when this BonedEntity is attached to another BonedEntity
        private int[] _correspondingParentBones;

        public BonedEntity(string id)
            : base(id)
        {
            _animationController = new AnimationController(this);
        }

        public AnimationController AnimationController
        {
            get { return _animationController; }
        }

        public int BoundBoneID
        {
            get { return _boundBoneID; }
        }

        public int[] CorrespondingParentBones
        {
            get { return _correspondingParentBones; }
        }

        public BonedModel BonedModel
        {
            get { return (BonedModel) _model; }
        }


        public void AddChild(BonedEntity child)
        {
            base.AddChild(child);

            // child BonedEntities that are added to a parent boned entity must match it's bone's with corresponding ones in the parent.
            // fortunately here, since the child is of same type we can directly access the private variables directly
            string[] parentBoneNames = BonedModel.BoneNames;
            string[] childBoneNames = child.BonedModel.BoneNames;
            bool found = false;

            child._correspondingParentBones = new int[child.BonedModel.BoneCount];
            for (int i = 0; i < child._correspondingParentBones.Length; i ++)
            {
                // itterate through every parent bone name and find a match we can assign to that child
                for (int j = 0; j < parentBoneNames.Length; j ++)
                {
                    if (childBoneNames[i] == parentBoneNames[j])
                    {
                        child._correspondingParentBones[i] = j;
                        found = true;
                        break;
                    }
                }
                if (!found) throw new Exception("No matching parent bone.");
                found = false;
            }
        }

        public void AddChild(BonedModel model)
        {
            base.AddChild(model);
            model.RegisterDuplicate(_id);
        }

        public void AddChild(EntityBase child, int boneID)
        {
            if (boneID >= 0)
                child.AttachedToBoneID = boneID;

            base.AddChild(child);
        }

        // TODO:  I think ive totally got this entirely 100% wrong.  We dont want Update() being called from Simulation!
        // This is just animation update and Render() which we want on the Render Thread (and remember Culler occurs on Render thread too!)
        // Recall we want our animation to properly interpolate regardless of what's going on in Update().
        public void Update(ModeledEntity entity, float elapsed)
        {
            _animationController.Update(elapsed);
            SetChangeFlags(Enums.ChangeStates.KeyFrameUpdated, Enums.ChangeSource.Self );
            //if (_bones == null) _bones = new TV_3DMATRIX[_actor.GetBoneCount()];
            //// if actor is an Actor3d then addition to setting the matrix
            //// we need to set the proper keyframe. (only non keyframe animations (e.g. ragdoll physics) need 
            //// to restore BoneMatrices.
            ////_currentActor.AnimationID = AnimationID; // when setting the animationID, store the start and endframes
            //int dummy = 0;
            //// _currentActor.Matrix = _matrix; // todo: this is only needed if i intend to grab BoneMatrices and such here.

            //// todo: Dont use this GetAnimationRangeInfo thing.  it seems to report wrong endframe value.  Instead, set these
            ////       in the model's animation config data xml then use that data for AddAnimationRange
            //BonedModel.GetAnimationRangeInfo(AnimationID, ref dummy, ref _currentAnimationStartFrame, ref _currentAnimationEndFrame);

            ////_currentActor.PlayAnimation(1); // this is required whenever the AnimationID between instances has changed. But not if we do our own keyframe calcs

            // float positionAtTime = GetKeyFrame(_currentAnimationStartFrame, _currentAnimationEndFrame, elapsed);

            //_animationController.KeyFrame = positionAtTime;


            //// but unfortunately i also end up calling it from Draw traverser via ModelBase.Render which leads to Actor3d.Render() which calls update
            //// the key though is that here is where we update the keyframe and bones and then NotifyChildModels that are attached.
            //// If we just update at ModelBase.Render() then we would in that sub need to NotifyChildModels after the Actor3d.Render since thats where actor.Update occurs
            //_actor.Update(entity); // must call update to get the keyframe to produce new bonematrices

            ////for (int i = 0; i < _bones.Length; i++)
            ////{
            ////    _bones[i] = _currentActor.GetBoneMatrix(i, false);
            ////}

            //// IMPORTANT: FOr actors, Simulation first moves them, and then we update the animation 
            //// normally though, we want Movement to NotifyChildModels, but since animation occurs afterwards, and since the bone matrices will change
            //// any child Model attached to a bone will not sync up unless we NotifyChildModels() here!
            //// But to avoid having two NotifyChildModels() for moving the character and then updating the animation, I think we could do something like
            //// Move (pos, scale, rotation, bUpdate) <-- where the animation is updated here too.
            //// But there's a problem.  If we want threaded updates, then Animation should be done INDEPENDANT of simulation thread.  
            //// Animations should interpolate and this would mean we _always_ notify childsModels here.
            ////            NotifyChildEntities();
        }
    }
}