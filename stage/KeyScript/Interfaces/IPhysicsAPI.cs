using System;
using System.Collections.Generic;
using Keystone.Types;

namespace KeyScript.Interfaces
{
    public interface IPhysicsAPI
    {
        /// <summary>
        /// For each celestial body in the Region, call Physics_Gravitation and combine the forces.  Each Celestial body should register GRAVITY force production
        /// </summary>
        /// <param name="entityID"></param>
        /// <param name="celestialBodyID"></param>
        /// <param name="celestialBodyMass"></param>
        /// <param name="elapsedSeconds"></param>
        /// <returns></returns>
        Vector3d Gravitation(string entityID, string celestialBodyID, double celestialBodyMass, double elapsedSeconds);
        Vector3d Thrust(string entityID, string thrustSourceEntityID, double thrustAmount, Vector3d thrustDirection, double elapsedSeconds);
        Quaternion RotationAfterAngularVelocity(Vector3d angularVelocity, Quaternion currentRotation, double elapsedSeconds);
        Vector3d GetTargetLeadingPositionQuadratic(Vector3d launchPosition, Vector3d launcherVelocity, Vector3d projectileAcceleration, Vector3d targetPosition, Vector3d targetVelocity, Vector3d targetAcceleration, double distance, double projectileSpeed, int iterations);
        Vector3d GetTargetLeadingVelocityQuadratic(Vector3d launchPosition, Vector3d launcherVelocity, Vector3d projectileAcceleration, Vector3d targetPosition, Vector3d targetVelocity, Vector3d targetAcceleration, double distance, double projectileSpeed, int iterations);

    }
}
