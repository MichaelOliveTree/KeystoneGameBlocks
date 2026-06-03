using System;


namespace Keystone.Simulation.Missions
{
    // Mission Objective
    public class Objective
    {
        public enum ObjectiveState
        {
            None,
            Running,
            Success,
            Fail
        }


        public enum ObjectiveType
        {
            None,   // mission should simply play until player chooses to leave the mission
            Custom, // does this imply Script?
            Destroy,
            DestroyN,
            Disable,
            Board,
            Defend,
            TravelTo,
            Escort,
            Patrol,
            Recon,
            SearchDestroy,
            Find,
            Analyze,
            Transport,
            RTB
                
        }

        
        //   eg: scan planet for X (eg drop ship ladning site)
        //       - board drop ship
        //       - launch drop ship
        //       - protect drop ship
        //       - drop ship landed
        //       - drop ship deboarded
        //       - drop ship RTB initiated
        //       - drop ship recovered

        //              - ObjectiveTypes.Destroy targetID
        //              - ObjectiveTypes.DestroyN #num FactionID
        //              - ObjectiveTypes.Disable targetID
        //              - Objectivetypes.Board targetID (troop count? what if board and rescue, or board and take-over, or board and investigate, etc). What if we actually want specific npc's to board such as a materlergy scientist?
        //              - ObjectiveTypes.Defend targetID, duration
        //              - ObjectiveTypes.Escort
        //              - ObjectiveTypes.Patrol 
        //              - ObjectiveTypes.Recon boundingbox or targetID or path?
        //              - ObjectiveTypes.Search&Destroy
        //              - ObjectiveTypes.Find (eg artifact)
        //              - ObjectiveTypes.Analyze
        //              - ObjectiveTypes.HoldPosition
        //              - ObjectiveTypes.Liberate (worlds, areas, zones of control)
        //              - ObjectiveTypes.Capture (zones of control, grid-points/map-points)
        //              - ObjectiveTypes.Rescue (people)
        //              - ObjectiveTypes.Transport item quantity where

        private Entities.Entity mSubject;
        private string mSubjectID; 
        private Entities.Entity mTarget;
        private string mTargetID; // todo: what if this is a classID or factionID such as "10 cylon raiders"

        private object[] mParams;
        private int mNumber;      // the desired count to achieve objective.  maybe make all these object[] params where each ObjectiveType knows what params will exist and how to parse them
        private int mCount;       //       this is the active count for classID or factionID based Destroy orders or Transport. mCount must be >= mNumber

        
        private ObjectiveType mType;
        private ObjectiveState mState;

        public ObjectiveType Type { get { return mType; } }
        public ObjectiveState State { get { return mState; } }



        public Objective()
        { }

        public Objective(ObjectiveType type)
        {
            mType = type;
        }

        public Objective(string scriptRelativePath)
        {
            mType = ObjectiveType.Custom;
            
        }

        public void EntityActivated(Entities.Entity entity)
        {
            if (entity.Name == "player")
            {
                mSubjectID = entity.ID;
                mSubject = entity;
            }
            else if (entity.Name == "enemy")
            {
                mTargetID = entity.ID;
                mTarget = entity;
            }
        }

        public void EntityDeActivated(Entities.Entity entity)
        {

        }

        public void OnStart()
        {
            string player = "player";
            string enemy = "enemy";
            // todo: for testing purposes, instead of writing a gui to assign subject and target ships, lets just search by vehicle name.
            //       eg: player1 and enemy1
            mSubject = (Keystone.Entities.Entity)Resource.Repository.GetByName(player);
            if (mSubject != null)
                mSubjectID = mSubject.ID;

            mTarget = (Keystone.Entities.Entity)Resource.Repository.GetByName(enemy);
            if (mTarget != null)
                mTargetID = mTarget.ID;


            // cache the Entity that is the subject of this objective and the target Entity.  All entities should already be loaded
            // prior to this objective being instanced.

            //if (string.IsNullOrEmpty(mSubjectID) == false)
            //    mSubject = (Entities.Entity)Resource.Repository.Get(mSubjectID);
            //if (string.IsNullOrEmpty(mTargetID) == false)
            //    mTarget = (Entities.Entity)Resource.Repository.Get(mTargetID);


        }

        public ObjectiveState Tick()
        {
            switch (mType)
            {
                case ObjectiveType.None:
                    mState = ObjectiveState.Running;
                    break;
                case ObjectiveType.Destroy:
                    //if (mSubject == null || mTarget == null)
                    //{
                    //    mState = ObjectiveState.Fail;
                    //    break;
                    //}

                    // a very simple test objective where player must destroy an enemey ship
                    if (mSubject.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Destroyed))
                        mState = ObjectiveState.Fail;
                    else if (mTarget.GetEntityAttributesValue((uint)KeyCommon.Flags.EntityAttributes.Destroyed))
                        mState = ObjectiveState.Success;

                    // todo: when does GC take affect? how do we flag the Entity as ready for GC? Or do we just rely on a timeout value?
                    // todo: Simulation needs to create a command to send the user notifying them of Mission Fail or Success

                    break;
                default:
                    break;
            }

            return mState;
        }
    }
}
