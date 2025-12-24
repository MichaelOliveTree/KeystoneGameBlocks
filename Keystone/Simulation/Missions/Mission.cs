using System;


namespace Keystone.Simulation.Missions
{
    ///<summary>
    /// A mission is on the same "rank" as a Scene.
    ///</summary>
    public class Mission
    {
        public enum MissionState
        {
            //Editing,   Mission's will not execute in Simulation.UpdateLoopback() if Editing.  So I think if we are Editing a mission, then that mission simply should not be assigned to Simulation.CurrentMission
            Starting,
            WaitingForPlayers,
            Running,
            Paused,
            Completed,
            Failed
        }

        MissionState mState; 
        MissionData mData;
        protected CSScriptLibrary.CSScript.LoadedScript mScript;
        string mBriefing;
        string mAfterActionReport;
        string mAuthor;
        
        // TODO: We need to make sure the appropriate area of the Scene is paged in before we load the Mission data or at least attempt to spawn anything and then set targetIDs to Objectives.
        
        // todo: are server entity IDs stored in the mission file? well it should depend on whether we are resuming a set of campaign missions? maybe not.
        //       spawn points simplly let the server simulation know to spawn an Entity and transmit to relevant clients
        // todo: in order to get the startingRegionID and know what parts of the Scene to load, we need to know the player's spawnpoint RegionID\ZoneID

        // todo: how does the mission get assigned the Simuation.CurrentMission?

        // todo: do we need different missionData files for each Region\Zone\Interior?

        // todo: how do we, or do we not persist things like vehicle damage, supplies onboard and crew injuries and deaths?
        //       well, maybe we just need to re-save the vehicle .kgbentity file since it stores all customproperty values for each sub-Entity including health points of crew and components.
        //       we can store state of vehicle at the beginning and successful completion of each mission


        SpawnPoint[] mSpawnPoints;
        // SpawnPoint[] mVehicleSpawnPoints;
        // SpawnPoint[] mCharacterSpawnPoints;
        // ObjectivePoints; //i forget what these were for.  If it's just a point in space, then just use a regular Objective with a "TravelTo" type along with coords (well, ai opponents would need an id set)

         Objective[] mPlayerObjectives; //
        // Objective mEnemyObjectives;
        


        // todo: i think we need a simple GUI to add Objectives to a Mission and set ObjectiveType and Objective targetID ship for player to destroy for that Objective.
        //       FOR TESTING, we know when we save the scene with vehicle's added, they are part of the Scene and not Mission Data
        //       however, Mission's can use those Entities obviously since many ships, stations, colonies,  npcs, etc, all need to be stored in
        //       Scenes and we limit Mission data to storing "notable" ships and characters.

        public bool Load(string path)
        {
            mData = new MissionData();
            mData.Load(path);

            mState = MissionState.Starting;

            // 0 - can we have a Mission that has no Campaign assigned to it or perhaps just a dummy Campaign?
            // 1 - create interface for creating a mission
            //     - select a campaign from a list
            //          - if no campaigns, add button to create new one
            //              - select the Scene this Campaign will use
            //              - do we need to specify / tag any Scene elements as belonging to any particular faction?

            //     - set mission name 
            //     - set mission briefing
            //     - assign objectives
            //       - what is an objective? 
            //         - does the objective have to be a mission object?
            //         - imagine a simple objective to destroy an Entity with a particular ID.  Or do we use Entity.Name?
            //              What about order of operations of multiple objectives such as Escort and Defend
            //              Are these defined types intrinsic and we can still offer scriptable objectives?

            


            //         - perhaps its coupled with an Objective to "survive" so if the target is destroyed, but the player is also destroyed, objective fail
            //         - what if the target retreats out of paging range?
            //         State {Open, Success, Fail}
            //

            // fill the vehicle spawn points
            // iterate from 0 to N until no section "vehicle_spawnpoint_N" exists


            // in multiplayer, spawnpoints may exist in different zones for different players
            // but there is only one mission object so where do we determine which vehicles for instance
            // get spawned to which players?

            // we could store spawnpoints by regionID

            // regions and zones should be identified by their friendly name NOT their ID.  So we should set Zone names by their zone ID. Or perhaps if they have a star system, by their system name?  In the case of a planetside
            // colony, by their colony name.

            // spawned vehicles and characters should always have a unique friendly name

            // TODO: we could potentially have seperate mission objects "prefabs" (or saved entities in \\saves\\ path) for each region and point to those in the mission file so they get loaded when a region is fully paged in.



            // TODO: mission objects using DefaultEnity derive directly from Entity not ModeledEntity.  However, when in EDIT MODE, the hud will render their icon which can be mouse clicked.
            //      - perhaps when culling during EDIT MODE, if the Entity is a mission object or light or something, we cull using a bounding box that we generate on the fly?
            //      - for rendering, we render an icon and perhaps a bounding volume.
            //          - for a spawnpoint, perhaps we render a sphere if the "shape" is a sphere
            //     WARNING: i dont think we want spawnpoints defined in the mission object to run their scripts in the general client update. We want it to only update from the loopback right?
            //              And potentially, we want to be able to fix the frequency.


            // TODO: after a region completely loads, load mission objects for taht region( such as spawnpoint entities) 
            // TODO: after all mission objects for that region are added to the scene (with the "mission object" entity flag) 
            //       trigger the spawnpoint so that it spawns player vehicle and trigger enemy spawnpoint so that it spawns enemy vehicle.
            // TODO: de-activate spawnpoint after it triggers (assuming it triggers "on start" and uses POINT shape and executes only once and is then disabled)

            // todo: spawnpoints should have a Hz/Frequency and not just update every frame right?
            //      todo: need a generic Hz/Frequency function. But what about a scheduler and round robin so we can spread updates of various
            //            entities over multiple frames rather than every frame.  That is, any Entity that uses a Hz/Frequency should update 
            //            via a schedular using round robin perhaps so that for instance, only 10 entities update every frame... then next frame 
            //            the next ten, etc.


            // fill the objective points


            return true;
        }

        public void Save(string path)
        {
            //System.Diagnostics.Debug.Assert(mState == MissionState.Editing);
            mData.Save(path);
        }

        public void Add(Objective obj)
        {
            mPlayerObjectives = Keystone.Extensions.ArrayExtensions.ArrayAppend(mPlayerObjectives, obj);
        }

        public MissionState State { get { return mState; } }

        public bool Enable { get; set; }
        /// <summary>
        /// Handle Entities being added to the Scene which may be important for Mission Objectives.
        /// For Entities that are already loaded prior to the Mission being assigned to Simulation, the Objective.OnStart()
        /// can just query Resource.Repository for existance of specific Entities.
        /// </summary>
        /// <param name="entity"></param>
        public void EntityActivated(Entities.Entity entity)
        {
            if (mPlayerObjectives != null)
                for (int i = 0; i < mPlayerObjectives.Length; i++)
                {
                    mPlayerObjectives[i].EntityActivated(entity);
                }
        }

        /// <summary>
        /// Handle Entities being removed from the Scene which may be important for Mission Objectivves
        /// </summary>
        /// <param name="entity"></param>
        public void EntityDeActivated(Entities.Entity entity)
        {
            if (mPlayerObjectives != null)
                for (int i = 0; i < mPlayerObjectives.Length; i++)
                {
                    mPlayerObjectives[i].EntityDeActivated(entity);
                }
        }

        public MissionState Update(double elapsedSeconds)
        {
            if (!Enable) return mState;

            if (mState == MissionState.Starting)
            {
                OnStart();
                // todo: shouldn't the user be able to select their ship type? maybe not for 1.0

                // the target for some objectives might not be loaded especially if they are in a Region other
                // than the  one the player is in.


                // check spawnpoints that have trigger "onstart"

                if (mScript != null)
                {
                    //Execute("StartUp");
                }
                mState = MissionState.Running;
            }
            else if (mState == MissionState.Running)
            {
                Execute();
            }
            else if (mState == MissionState.Completed)
            {
                // total any result statistics for all objectives and player vehicle health and crew stats
                // perhaps compute a "score" for how well the player did


                // todo: show a Mission Completed dialog
                //       do we use a callback? well definetly not a reference to client form because Mission's run serverside 
                //       so it is the server that needs to inform the clients. Well, no callback is nessary if Update() returns a MissionState
            }

            return mState;
        }

        private void OnStart()
        {
            if (mPlayerObjectives != null)
                for (int i = 0; i < mPlayerObjectives.Length; i++)
                {
                    if (mPlayerObjectives[i].Type == Objective.ObjectiveType.Custom)
                    {

                    }
                    else
                        mPlayerObjectives[i].OnStart();
                }
        }


        private void Execute()
        {
            if (mScript != null)
            {
                // TODO: if this mission is scripted, then are all objectives handled by the mission and mPlayerObjectives or mEnemyObjectives all null?

                //Execute("Update");
            }

            // TODO: if an enemy reaches all it's objectives before the player reaches theirs, doesn't this also constitute a Fail state for the player?
            // todo: is there a scenario where players and enemies can all reach their objectives simultaneously? I think objectives should be designed
            //       such that this is impossible.  
            if (mPlayerObjectives != null)
            {
                int succeededCount = 0;
                for (int i = 0; i < mPlayerObjectives.Length; i++)
                {
                    // only process the objective if it is not yet succeeded or failed
                    Objective.ObjectiveState currentState = mPlayerObjectives[i].State;
                    if (currentState != Objective.ObjectiveState.None) continue;

                    // advance and return new state
                    Objective.ObjectiveState newState = mPlayerObjectives[i].Tick();

                    if (newState == Objective.ObjectiveState.Fail)
                    {
                        mState = MissionState.Failed;
                    }
                    else if (newState == Objective.ObjectiveState.Success)
                    {
                        // flag for removal from mPlayerObjectives
                        succeededCount++;
                    }
                    if (mState == MissionState.Failed) break;
                }

                if (mState != MissionState.Failed && succeededCount == mPlayerObjectives.Length) 
                {
                    mState = MissionState.Completed;
                }
            }
        }

        // add a default mission that is freeform with no objectives or objectivepoints with option to select how many opponents or even never ending waves of opponents
        // add a random mission generator that can generate mission data and load it for random experiences


        // a mission script isn't (CAN'T BE!) tied to any one particular Entity. Entity.IDs must be variable and passed into the script

        // what does the mission script need to know to deal with player and any opponents and objectives that exist in the game and need to be analyzed every hz?

        // missions run server side (loopback in current 1.0 version) and send updates to the players

        // there are no sub-missions.  Those are just objectives
        


        // lets say we place a player spawn point... how do we save the spawn point to a mission data file? what is te parent?
        //  - well, if we keep Ids matching across scene and missions its possible but, then when loading, dont we end up having mission data files as big as the scene files?
        //  - maybe mission data files arernt hierarchical and instead only store for instance, the spawn points
        //  - what about scripts attached to spawnpoints? well, defining custom properties such as the prefabs it will spawn is good, but we dont really need Update() logic do we?  After all
        //    what is the mission update logic going to do?

        // maybe the mission data xml is more like INI with sections and then key value pairs?

        // maybe mission data is loaded and overlayed into the scene with a completely seperate load logic?
        // - spawnpoints for instance are invisible and HUD can maybe show the location in Edit mode but maybe it doesn't need to be a Node or Entity with a script.
        //   Its mostly just a piece of data and the HUD gives a visual representation so that it can be manipulated in the Mission Editor.
        //   - well, it can be just data, but then interpreted by the loader as a generic non-ModeledEntity

        // mission data can perhaps contain briefing text and other text that we want to display at various points during the mission

        // we can display mission stats at the end of the mission

        // where are the missions stored?
        //  Data\scenes\Campaign001\missions\mission00.xml

        // mission status needs to be stored serverside in db

        // what about enemy ship objectives and even friendly ship objectives?

        // what about actions that need to occur such as loading specific crew and passengers and marines to the drop ship?


        // 1 - create a mission INI data file and copy paste the spawn point translations into the file 

        // 2 - we wont bother rendering spawn points in edit mode just yet
        // 3 - GetMissions command from client to server that returns the available missions based on which were completed

    }
}

