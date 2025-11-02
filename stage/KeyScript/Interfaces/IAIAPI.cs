using System;
using Keystone.Types;

namespace KeyScript.Interfaces
{
	/// <summary>
	/// Description of IAIAPI.
	/// </summary>
	public interface IAIAPI
	{

        bool BehaviorEnabled(string entityID);

        void EnableBehavior(string entityID, bool enable);

        #region Path Finding
        Vector3d GetRandomDestination(string regionID, string npcID);
        Vector3d[] Path_Find(string interiorID, string entityID, Vector3d start, Vector3d end, out string[] components);
        Vector3d[] Path_Find (string entityID, string destinationRegionID, Vector3d destination);
		void Path_Find (int startX, int startY, int startZ, int stopX, int stopY, int stopZ);
		void Create_NavPoint (string entityID, string targetID);
		Vector3i Tile_FindNearestAdjacent (string regionID, string entityID, Vector3d targetPosition, out Vector3d destination);
		#endregion
		
		#region Targeting 
		string Target_Find (string entityID, out Vector3d targetPosition);
		string Target_Find (string entityID, Predicate<string> match, out Vector3d targetPosition);
		#endregion
		
		#region Steering Behaviors
		Quaternion RotateTo (Vector3d direction);
		Quaternion RotateTo (Vector3d v1, Vector3d v2, double rotationSpeed, double elapsedSeconds);
		Vector3d Steer(string ISteerableEntityID, Vector3d targetPosition, double maxForce, double maxSpeed, double slowDownDistance, double elapsedSeconds);
		//Vector3d Steer(string ISteerableEntityID, Vector3d targetPosition, double slowDownDistance);
		Vector3d Wander(string ISteerableEntityID);
		Vector3d Follow(); // path tracing
		
		// persue is follow but with goal of catching/intercepting
		Vector3d Pursue(string ISteerableEntityID, string ISteerableEntityTargetID);
		Vector3d Pursue (string ISteerableEntityID, Vector3d target);
		Vector3d Pursue(string ISteerableEntityID, string ISteerableEntityTargetID, double slowDownDistance);
		Vector3d Pursue (string ISteerableEntityID, Vector3d target, double slowDownDistance);
		
		Vector3d Avoid ();
		#endregion

	}
}
