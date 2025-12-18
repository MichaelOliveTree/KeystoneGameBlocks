using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;

namespace KeyEdit.Scripting
{
	/// <summary>
	/// Description of AIAPI.
	/// </summary>
	public class AIAPI : IAIAPI 
	{

        public bool BehaviorEnabled(string entityID)
        {
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);
            if (entity.Behavior == null) return false;

            return entity.Behavior.Enable;
        }

        public void EnableBehavior(string entityID, bool enable)
        {
            Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(entityID);
            if (entity.Behavior != null)
                entity.Behavior.Enable = enable;
        }

        #region Path Finding
        public int mFloor = 1;
        public Vector3d GetRandomDestination(string regionID, string npcID)
        {
            // NOTE: Currently, when npc.css gets called InitializeEntity, it attempts to make call to EnableBehavior(false) above
            //       but if no root behavior node exists, that is skipped.  So when importing a new BonedEntity and configuring
            //       it's behavior, we should first add the root behavior then save the imported BonedEntity, and then reload it
            //       and add any child scripts.  Otherwise we'll likely get an exception for trying to find a GetRandomDestination()
            Keystone.Portals.Region region = (Keystone.Portals.Region)Keystone.Resource.Repository.Get(regionID);
            Keystone.Entities.BonedEntity npc = (Keystone.Entities.BonedEntity)Keystone.Resource.Repository.Get(npcID);

            if (region == null || npc == null) throw new ArgumentException("Region or NPC does not exist in Repository");
            Vector3d result;

            if (region is Keystone.Portals.Interior)
            {
                Keystone.Portals.Interior interior = (Keystone.Portals.Interior)region;
                // TEMP: for now, just get a position on the 2nd or 3rd floors of this Interior that's in a valid BOUNDs.
                // NOTE: max value is non inclusive so if 4 is passed in, it will search for random number including 2 and 3 only, not 4
                int low = 1; // 0;
                int high = 3; // interior.CellCountY;
                int floor = interior.GetFloorFromAltitude(npc.Translation.y);


                // TODO: if the tilevalue is for a door, or other illogical final destinations, pick a new random Area.
                //interior.Database.GetMapValue ("footprint", )
                BoundingBox[] areas = interior.GetConnectivityAreas(floor);
                if (areas == null || areas.Length == 0) return Vector3d.Zero();
                int value =  Keystone.Utilities.RandomHelper.RandomNumber(0, areas.Length);
                //value = 0;
                //value = 6; //9,  forced debug values
                result = areas[value].Center;
                result.y -= interior.CellSize.y / 2d;
                //int tmp = (int)interior.GetFloorHeight((uint)floor);
                //System.Diagnostics.Debug.Assert(tmp == result.y);
            }  
            else
            {
                // NOTE: Currently, when npc.css gets called InitializeEntity, it attempts to make call to EnableBehavior(false) above
                //       but if no root behavior node exists, that is skipped.  So when importing a new BonedEntity and configuring
                //       it's behavior, we should first add the root behavior then save the imported BonedEntity, and then reload it
                //       and add any child scripts.  Otherwise we'll likely get an exception for trying to find a GetRandomDestination()
                throw new NotImplementedException("AIAPI.GetRandomDestination() - not implemented for Regions of this type " + region.GetType().Name);
            }
            
            return result;
        }

        public Vector3d[] Path_Find(string interiorID, string npcID, Vector3d start, Vector3d end, out string[] components)
        {
            Keystone.Portals.Interior interior = (Keystone.Portals.Interior)Keystone.Resource.Repository.Get(interiorID);
            Keystone.Entities.Entity npc = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(npcID);
            return interior.Scene.PathFind(interior, npc, start, end, out components);
        }

        public Vector3d[] Path_Find (string entityID, string destinationRegionID, Vector3d destination)
		{

			// find the startRegion where entity is located
			Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
            // entity.Region
			Keystone.Scene.Scene scene = entity.Scene;
			
            // TODO: the following call to PathFind is not using PathingInterior.cs which is what wew want
            // TODO: we should pass entity.Translation and not entire entity right?
			return scene.PathFind (entity, destinationRegionID, destination);
		}

        // TODO: Path_Find() should not accept tile values.  it should only accept Vector3d start and end locations
        //       Our path finder will find the tile locations as needed.
        // TODO: should entity regionID be passed in?
        public void Path_Find(int startX, int startY, int startZ, int stopX, int stopY, int stopZ)
		{
			// find the region that contains the tile location startX, startY, startZ // <-- todo this is wrong.  should find location containing Vector3d start position and internally we can find tiles and Areas as necessary
			throw new NotImplementedException();
		}

        // TODO: what is Tile_FindAdjacent() used for?  Tiles should be internal to celledRegion and not available to scripts.
        //       and why is "Adjacent" used?  Adjacent to what?  There are many tiles adjacent to any other tile.
        public Vector3i Tile_FindNearestAdjacent (string regionID, string entityID, Vector3d targetPosition, out Vector3d destination)
		{
			Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
			Keystone.Scene.Scene scene = entity.Scene;
			
			// find nearest adjacent tile, to the tile that hosts the targetPosition
			return scene.Tile_FindAdjacent (regionID, entity.Translation, targetPosition, out destination);
		}
		#endregion
		
		#region Targeting 
		// SearchParameters\filters - Type (item or player or npc), team, capacity to damage target, 
		//                    search radius, proximity sort, threat sort, mission priority sort
		//                    assignment by leader/role, weapons available, own health, 
		//                    or maybe we're tracking a specific targetID and need new position/telemetry information on it.
		public string Target_Find (string entityID, out Vector3d targetPosition)
		{
			Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
			Keystone.Scene.Scene scene = entity.Scene;
			
			Predicate<Keystone.Entities.Entity> match = (e) =>
			{
				if (e is Keystone.Entities.Character && 
				   e.ID != entityID) return true;
				
				return false;
			};
			
			targetPosition = Vector3d.Zero();
			List<Keystone.Entities.Entity> results = scene.FindEntities (true, match);
			
			if (results == null) return null;
			
			
			// TODO: targetPosition must always returned as relative to region calling entity is in.
			// in this way, it's like Path_Find()
			targetPosition = results[0].BoundingBox.Center;
			return results[0].ID;
		}
		
		public string Target_Find (string entityID, Predicate<string> match, out Vector3d targetPosition)
		{
			Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
			Keystone.Scene.Scene scene = entity.Scene;
			
            // todo: 
			Predicate<Keystone.Entities.Entity> innerMatch = (e) =>
			{
				if (e is Keystone.Entities.Character && 
				   e.ID != entityID) return true;
				
				return false;
			};
			
			targetPosition = Vector3d.Zero();
			List<Keystone.Entities.Entity> results = scene.FindEntities (true, innerMatch);
			
			if (results == null) return null;
			
			//System.Collections.Generic.List<Keystone.Entities.Entity> outerResults = new System.Collections.Generic.List<Keystone.Entities.Entity>();
			for (int i = 0; i < results.Count; i++)
			{
				if (match(results[i].ID))
				{
					targetPosition = results[i].BoundingBox.Center;
					return results[i].ID;
				}
			}
			
			
			return null;
		}
		
		public void Create_NavPoint (string entityID, string targetID)
		{
			Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (entityID);
			Keystone.Entities.Entity target = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (targetID);
            string destinationRegionID = target.Region.ID;
            
            
            Game01.GameObjects.NavPoint wp = new Game01.GameObjects.NavPoint(entity.ID, destinationRegionID, target.Translation);

            Lidgren.Network.IRemotableType msg =
                new KeyCommon.Messages.GameObject_Create_Request(wp.TypeName, entity.ID, wp);

          	AppMain.mNetClient.SendMessage(msg);
		}
		

		
//		public void Target_GetTelemetry(string entityID, string targetID)
//		{
//		}
		#endregion
		
		#region Steering Behaviors
		public Keystone.Types.Quaternion RotateTo (Vector3d direction)
		{
			return Keystone.AI.Steering.RotateTo (direction);
		}
				
		public Keystone.Types.Quaternion RotateTo (Vector3d v1, Vector3d v2, double rotationSpeed, double elapsedSeconds)
		{
			return Keystone.AI.Steering.RotateTo (v1, v2, rotationSpeed, elapsedSeconds);
		}
		
		public Keystone.Types.Vector3d Steer(string ISteerableEntityID, Vector3d targetPosition, double maxForce, double maxSpeed, double slowDownDistance, double elapsedSeconds)
		{
			Keystone.Entities.ModeledEntity entity = (Keystone.Entities.ModeledEntity)Keystone.Resource.Repository.Get (ISteerableEntityID);

            KeyCommon.Data.UserData userdata = entity.BlackboardData;


            Keystone.Types.Vector3d steeringVector = Keystone.AI.Steering.Steer(entity.Translation, targetPosition, entity.Velocity, maxSpeed, maxForce, slowDownDistance);
            return steeringVector;

            //double maxSpeed = 5;


            // TODO: navpoints were originally structs for dealing with terrain zones so that we could cross zones planet side.
            //Game01.GameObjects.NavPoint[] navpoints = (Game01.GameObjects.NavPoint[])property;
            //Vector3d[] points = navpoints[0].Path;
            //Steering.Pathway.IPathway pathway = new Steering.Pathway.PolylinePathway(points, 1.0f, false);

            //Vector3d force = Steering.Helpers.VehicleHelpers.SteerToFollowPath(vehicle, true, 0.1, pathway, maxSpeed, null);

            //// arrived 
            //if (force == Vector3d.Zero()) return force;

            //entity.Mass = 1.0;
            //entity.Position = entity.Translation;
            //entity.ApplySteeringForce (force, elapsedSeconds);
            // TODO: shouldnt the translation occur in simulation.Update()?  scripts should only set velocity
            //entity.Translation = entity.Position;
            //return new Vector3d(0,0,0);
            //return this.Steer (ISteerableEntityID, targetPosition, 0);	
        }



        //public Keystone.Types.Vector3d Steer(string ISteerableEntityID, Vector3d targetPosition, double slowDownDistance)
        //{
        //    Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get(ISteerableEntityID);

        //    KeyCommon.Data.UserData userdata = (KeyCommon.Data.UserData)entity.GetCustomPropertyValue("userdata"); // agent.CustomData.GetDouble ("steer_wander_theta"); // agent.WanderTheta

        //    double maxSpeed = userdata.GetDouble("max_speed"); // agent.MaxSpeed; TODO: max speed can be variable based on damage
        //    double maxForce = userdata.GetDouble("max_force"); // agent.MaxForce; TODO: max force can be variable based on damage

        //    Keystone.Types.Vector3d steeringVector = Keystone.AI.Steering.Steer(entity.Translation, targetPosition, entity.Velocity, maxSpeed, maxForce, slowDownDistance);
        //    return steeringVector;
        //}



        public Keystone.Types.Vector3d Wander(string ISteerableEntityID)
		{
			Keystone.Entities.Entity entity = (Keystone.Entities.Entity)Keystone.Resource.Repository.Get (ISteerableEntityID);
			
            //TODO: this should grab entity.CustomData. We no longer store "userdata" as a custom property
            KeyCommon.Data.UserData userdata = (KeyCommon.Data.UserData)entity.GetCustomPropertyValue ("userdata"); // agent.CustomData.GetDouble ("steer_wander_theta"); // agent.WanderTheta
            
            double maxSpeed = userdata.GetDouble ("max_speed"); // agent.MaxSpeed; TODO: max speed can be variable based on damage
            double maxForce = userdata.GetDouble ("max_force"); // agent.MaxForce; TODO: max force can be variable based on damage
            double wanderTheta = userdata.GetDouble ("steer_wander_theta");  
                        
			Keystone.Types.Vector3d steeringVector = Keystone.AI.Steering.Wander (entity.Translation, entity.Velocity, wanderTheta, maxSpeed, maxForce, out wanderTheta);
			
			// accumulate wander theta values
            userdata.SetDouble ("steer_wander_theta", wanderTheta);
            int[] brokencodes;
            entity.SetCustomPropertyValue ("userdata", userdata, false, false, out brokencodes);
            
			return steeringVector;
		}
		
		public Keystone.Types.Vector3d Follow()
		{
			throw new NotImplementedException();
		}
		
		public Keystone.Types.Vector3d Pursue(string ISteerableEntityID, string ISteerableEntityTargetID)
		{
			throw new NotImplementedException();
		}
		
		public Keystone.Types.Vector3d Pursue(string ISteerableEntityID, Keystone.Types.Vector3d target)
		{
			throw new NotImplementedException();
		}
		
		public Keystone.Types.Vector3d Pursue(string ISteerableEntityID, string ISteerableEntityTargetID, double slowDownDistance)
		{
			throw new NotImplementedException();
		}
		
		public Keystone.Types.Vector3d Pursue(string ISteerableEntityID, Keystone.Types.Vector3d target, double slowDownDistance)
		{
			throw new NotImplementedException();
		}
		
		public Keystone.Types.Vector3d Avoid()
		{
			throw new NotImplementedException();
		}
		#endregion
		
	}
}
