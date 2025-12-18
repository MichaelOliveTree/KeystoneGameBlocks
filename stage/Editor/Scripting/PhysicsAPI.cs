using System;
using System.Collections.Generic;
using KeyScript.Interfaces;
using Keystone.Types;

namespace KeyEdit.Scripting
{
    public class PhysicsAPI : IPhysicsAPI
    {

        Vector3d IPhysicsAPI.GetTargetLeadingPositionQuadratic(Vector3d launchPosition, Vector3d launcherVelocity, Vector3d projectileAcceleration, Vector3d targetPosition, Vector3d targetVelocity, Vector3d targetAcceleration, double distance, double projectileSpeed, int iterations)
        {
            return Keystone.Physics.Newtonian.GetTargetLeadingPositionQuadratic(launchPosition, launcherVelocity, projectileAcceleration, targetPosition, targetVelocity, targetAcceleration, distance, projectileSpeed, iterations);
        }

        Vector3d IPhysicsAPI.GetTargetLeadingVelocityQuadratic(Vector3d launchPosition, Vector3d launcherVelocity, Vector3d projectileAcceleration, Vector3d targetPosition, Vector3d targetVelocity, Vector3d targetAcceleration, double distance, double projectileSpeed, int iterations)
        {
            return Keystone.Physics.Newtonian.GetTargetLeadingVelocityQuadratic(launchPosition, launcherVelocity, projectileAcceleration, targetPosition, targetVelocity, targetAcceleration, distance, projectileSpeed, iterations);
        }



        Quaternion IPhysicsAPI.RotationAfterAngularVelocity(Vector3d angularVelocity, Quaternion currentRotation, double elapsedSeconds)
        {
            return Keystone.Physics.Newtonian.RotationAfterAngularVelocity(angularVelocity, currentRotation, elapsedSeconds);
        }


        /// <summary>
        /// For each celestial body in the Region, call Physics_Gravitation and combine the forces.  Each Celestial body should register GRAVITY force production
        /// </summary>
        /// <param name="entityID"></param>
        /// <param name="celestialBodyID"></param>
        /// <param name="celestialBodyMass"></param>
        /// <param name="elapsedSeconds"></param>
        /// <returns></returns>
        Vector3d IPhysicsAPI.Gravitation(string entityID, string celestialBodyID, double celestialBodyMass, double elapsedSeconds)
        {
            Keystone.Entities.Entity ent = EntityAPI.GetEntity(entityID);
            Keystone.Celestial.Body celestialBody = (Keystone.Celestial.Body)Keystone.Resource.Repository.Get(celestialBodyID);

            // TODO: currently below i still assume Celestial.Body implements a hardcoded
            // property named "mass" but eventually I may get rid of these special entity types
            // and use all scripted properties which is why i have an argument for it here
            // note: we use the derived translations, not the parent relative translations
            Vector3d gravity = Keystone.Physics.Newtonian.GravityVector(celestialBody.DerivedTranslation, ent.DerivedTranslation, celestialBody.MassKg);

            // convert the gravity direction from region to vehicle's local coordinate system
            // - isnt vehicles region and local coordinates one and the same?  In other words, it's not source of our bug with gravity vector being off...
            System.Diagnostics.Debug.Assert(ent.RegionMatrix.GetTranslation().Equals(ent.DerivedTranslation));
            System.Diagnostics.Debug.Assert(celestialBody.RegionMatrix.GetTranslation().Equals(celestialBody.DerivedTranslation));

            // gravity per second scaled to the elapsed seconds
            gravity *= elapsedSeconds;

            return gravity;


            //            Matrix src2dest = Matrix.Source2Dest (ent.RegionMatrix, ent.LocalMatrix);
            //            Vector3d localTranslation = Vector3d.TransformCoord (celestialBody.DerivedTranslation, src2dest);
            //            Vector3d gravity = KeyCommon.Simulation.NBodyPhysics.GravityVector(localTranslation, ent.Translation, celestialBody.Mass );
            //
            //            // gravity per second scaled to the elapsed seconds
            //            // NOTE: gravity is an acceleration and we only compute an acceleration here.  
            //            //       All accelerations will be totaled and applied during Simulation.Acceleration() 
            //            gravity *= elapsedSeconds; 
            //            return gravity; 
        }

        Vector3d IPhysicsAPI.Thrust(string entityID, string thrustSourceEntityID, double thrustAmount, Vector3d thrustDirection, double elapsedSeconds)
        {
            return new Vector3d(); // this function should be obsolete now.  Thrust compuation now is just adding to "force" property

            //Keystone.Entities.Entity ent = GetEntity(entityID);
            //Keystone.Entities.Entity thruster = GetEntity(thrustSourceEntityID);


            //Vector3d translation, acceleration, velocity;
            //translation = ent.LatestStepTranslation; // ent.Translation;
            //acceleration = ent.Acceleration;
            //velocity = ent.Velocity;

            ////

            ////

            //// TODO: i think the below .Accelerate is appropriate for Gravity
            ////       that pulls and accelerates at per second * persecond
            //// but for Thrust, the acceleration at a given throttle is constant with 
            //// some variation in acceleration due to ship mass decrease as fuel is burned.
            //// However, i dont understand the jumping and non smooth animation particularly consdering
            //// the ship is not actually traveling that fast... so over the course of frames it seems
            //// to only move once per second and not every time.  Is this becuase
            //// our thrust consumption is not realtime?

            ////thrustAmount = 9.8 * 2; // 2G acceleration
            ////thrustDirection.z = 1;
            //Vector3d force = KeyCommon.Simulation.NBodyPhysics.VehicleAcceleration(thrustDirection * thrustAmount, ref translation, ref acceleration, ref velocity, elapsedSeconds, 0f);
            //return force;
            //// TODO: the issue here Translation/Rotation/Scale must
            ////       be passed back to the main Simulation running these scripts
            ////       so that ultimately the final .Translation/Rotation/Scale is the
            ////       computed interpolated version and not one set unnecessarily.  plus 
            ////       without the fixedtimestepalpharatio, we cant even compute the proper
            ////       interpolation.
            ////ent.Translation = translation;
            ////ent.LatestStepTranslation = translation;

            ////if (alpha > 0)
            ////    ent.Translation = Interpolate();

            ////System.Diagnostics.Debug.WriteLine(translation.ToString());
            ////ent.Velocity = velocity;
            ////ent.Acceleration = acceleration;

        }
    }
}
