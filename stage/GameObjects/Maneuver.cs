using System;
using System.Collections.Generic;
using Keystone.Types;


namespace Game01.GameObjects
{
    public enum ManeuverState
    {
        Idle,
        Executing,
        Completed,
        Error
    }

    // maneuvers can run many times per frame per physics step
    // whilst the logic can run here every physics step.  assuming i can
    // get the physics engine to execute update to maneuvers.  
    // Because it should be re-inforced this is AI logic and not really physics
    // because these maneuvers calculate what to enable/disable and how long.
    // by themselves they dont provide any acceleration or anything.
    // Furthermore, this logic is script side called (for now) and
    // that means there are no Entity references. I would need to pass in
    // the Entity physics state.  Also i would need to pass in Thruster/Engine states

    public class Maneuver
    {


        //		int StartTime;
        //		int Length;
        // below goals are a way to implement by predicting our goals in advance and then the 
        // current - desired tells us if we accelerate or decelerate
        HelmState mHelm;
        Vector3d mVelocityGoal; // when reaching this halfway point, we decelerate
        Vector3d mAngularVelocityGoal; // when reaching this halfway point, we decelerate 

        ManeuverState mState;

        // target 
        public Vector3d LookAtGoal;   // a simple maneuver who's only purpose is to
                                  // rotate the ship until it is oriented towards
                                 // the LookAt coordinate.

        public Maneuver(HelmState helm)
        {
            if (helm == null) throw new ArgumentNullException();
            mHelm = helm;
            mState = ManeuverState.Idle;
        }

        public ManeuverState State
        {
            get { return mState; }
        }

        // TODO: execute should ideally i think modify the on/off of thrusters and engines and nothing more
        //       however, how would we burn for a partial step if we had to?  The only way i can think of is
        //      to instruct the particular engine to only generate a fraction of the thrust
        //      it would normally during a step.
        // http://www4.ncsu.edu/unity/lockers/users/f/felder/public/kenny/papers/partial.html
        // not sure if this partial fractions page is relevant to what im talking about 
        // wrt partial physics steps.
        // 
        // TODO: perhaps an alternative is to know that if in the next physics step after the current
        //       one we are calculating it's determined we need a partial, then maybe instead we skip it
        //       or we do one of two things 1) if we need to reverse acceleration on the second half of the
        //       partial, we can instead skip acceleration altogether and that will negate the need for
        //       the other partial deceleration (assuming it's 50% partial split)
        //       2) if we need to be at a complete stop on that axis, then we instruct the engine to fire
        //       3) if the engine can be made to know, it can compute a velocity in it's PRODUCTION
        //          that takes into account the ratio of acelerate to deceleration.
        //       So it becomes a matter of how to relay this info to the thrust producers?  The only way
        //       I can think of is to have a property that is not just on/off but also includes a percentage
        //       of thrust.  Since our maneuvering engines are seperate for + and - rotations, we could
        //       supply for example 60% to + and then 40% to the minus by setting a property
        //       indicating that this next physics step is a partial step.
        //       4) an undesireable (i think) 4th option that i only include for completeness sake is
        //       to somehow instruct the physics to only take a fraction of the result to simulate a sub-step
        //       or to split the step time into the sub-step time, but that requires too much knowledge
        //       between the App and the physics engine.

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Make sure at default rotation, Entity's visual mesh model is facing "forward" down positive Z.
        /// </remarks>
        /// <param name="elapsedSeconds">Typically a fixed time step.</param>
        public void Execute(double elapsedSeconds)
        {
            mState = ManeuverState.Executing;
            // TODO: should we return a State after Execute so we know if we can
            //       remove the Maneuver from the Helm?

            // if we want to start accelerating with thrusters
            // along the various axis we need, but we want to do it
            // every frame as opposed to periodically with corrections inbetween

            // then we need to be able to analyze our acceleration and know our deceleration capability
            // on that axis and so when to start slowing down.
            // That means our aceleration must take into account the current velocity on all axis!
            // This is a must regardless because our ship will likely already have some velocity.

            // So we can easily compute the time needed to decelerate given a deceleration rate
            // 
            // DeltaV - the change we need in the velocity we can compute every frame
            //          this deltaV can guide our logic for which axis to burn.



            // what is our current velocity?
            Vector3d velocity = mHelm.Velocity;
            Vector3d angularVelocity = mHelm.AngularVelocity;

            // get vector and distance to waypoint target
            double distance;
            // --------------------------------- begin todo notes
            // TODO: i had the commented out version first which i think is correct but the problem is         
            //       the test vehicle i'm using has the ship's visual mesh at default rotation 0,0,0 
            //       NOT with it's front facing Positive Z.  A fix is to
            //       make sure the exterior model (not the entity) has it's rotation modified so that when parented
            //       to the Entity, what we want to be the "front" of the model visually, should point down positive z
            //       "Forward" is always positive Z    
            //       TODO: i should make a reminder message appear at some point to remind users of this when they've imported
            //       a mesh.
            // TODO: I think if the distance between our lookatgoal and helm.position is below epsilon, we should cease to move at all (including orientation since we're right on the destination)
            //
            Vector3d dir = Vector3d.Normalize(LookAtGoal - mHelm.Position, out distance);
            dir.y *= -1d; // we seem to need to reverse the y rotation. perhaps rather than +Z facing this expects front to be -Z facing? // Sept.13.2013 Hypnotron
            //Vector3d dir = Vector3d.Normalize(mHelm.Position - LookAtGoal, out distance);
            // TODO: see related comment under Keystone.Types.Euler.SetDirection
            // --------------------------------end todo notes

            // calc remaining rotation to reach target orientation
            // NOTE: dealing with Euler struct so .Add() directly on mHelm is adding to a copy
            // so we must set mHelm.Orientation with the new result
            Euler currentOrientation = mHelm.Orientation;
            Euler relativeEularRotation = currentOrientation.GetRotationTo(dir, true, true, true);

            // how much acceleration do we have available on each axis?
            // for now we will assume fixed 100 
            // The time, T , it takes for an object to go thru one comblete rotation of 360 degrees
            // or 2pi radians is its "period". The rate at which it completes the rotation is its
            // "angular velocity" The rate is the angle (in radians) divided by the time.
            // So , Angular Velocity = 2 pi / T

            // Angular velocity, also called rotational velocity, is a quantitative expression of the
            // amount of rotation that a spinning object undergoes per unit time. It is a vector quantity, 
            // consisting of an angular speed component and either of two defined directions or senses.
 
            // The magnitude, or length, of the angular velocity vector is directly proportional to the 
            // angular speed, and is measured in the same units as angular speed (radians per second, 
            // degrees per second, revolutions per second, or revolutions per minute). The direction of 
            // the angular velocity vector is perpendicular to the plane in which the rotation takes place.
            // If the rotation appears clockwise with respect to an observer, then the angular velocity 
            // vector points away from the observer. If the rotation appears counterclockwise, then the 
            // angular velocity vector points toward the observer.
 
            // Consider a car rolling forward along a highway. The angular velocity vectors for all four 
            // tires point toward the left along the lines containing the wheel axles. If the car speeds up,
            // the vectors get longer. If the car slows down, the vectors get shorter. If the car stops, 
            // the vector lengths become zero. If the car is put into reverse, the vectors reverse their 
            // directions, and point toward the right along the lines containing the wheel axles.
 
            // So the question is, how do we create a thrust force into angular acceleration calculation?
            // http://www.philsrockets.org.uk/forces.pdf
            // our angular velocity for now we can just say is 90degrees per second or
            // 1/4 pi radians per second squared.
            double angularSpeed = .25 * Math.PI;
            Vector3d angularAcceleration = new Vector3d(angularSpeed, angularSpeed, angularSpeed);
            angularAcceleration *= elapsedSeconds;

            // we'll assume acceleration and deceleration is same (ie. all thrusters provide identical angular acceleration)



            // for each axis, if we are not at goal, do we accelerate or decelerate?
            // if this time is >= the time it would take to slow down to 0
            // then we must start slowing down else we can accelerate

            // if for each axis, we're within epsilon of target angle
            // and current velocity on that axis is less than or equal to our angularAcceleration
            // then set acceleration to 0, set the goal to exact, 

            double slowDownTime;
            double t;

            // Yaw June.4.2017 - restricting version 1.0 to yaw axis rotations only.  Sticking all orbits, navigation
            //                   to a 2D plane ala Starfleet Command will help us finish 1.0 sooner.  v2.0 we'll add 
            //                   full 3D navigation and orbits.
            if (WithinEpsilon(relativeEularRotation.Yaw) && Math.Abs(angularVelocity.y) <= angularAcceleration.y)
            {
                // can we snap the rotation to the exact rotation required?
                angularVelocity.y = 0;
                angularAcceleration.y = 0;
            }
            else
            {
                slowDownTime = angularAcceleration.y / relativeEularRotation.Yaw;
                t = relativeEularRotation.Yaw / angularVelocity.y;
                if (t > slowDownTime)
                    angularVelocity.y += angularAcceleration.y;
                else
                    angularVelocity.y -= angularAcceleration.y;
            }

            //// Pitch
            //if (WithinEpsilon(relativeEularRotation.Pitch) && Math.Abs(angularVelocity.x) <= angularAcceleration.x)
            //{
            //    angularVelocity.x = 0;
            //    angularAcceleration.x = 0;

            //}
            //else
            //{
            //    slowDownTime = angularAcceleration.x / relativeEularRotation.Pitch;
            //    t = relativeEularRotation.Pitch / angularVelocity.x;
            //    if (t > slowDownTime)
            //        angularVelocity.x += angularAcceleration.x;
            //    else
            //        angularVelocity.x -= angularAcceleration.x;
            //}
            //// Roll
            //if (WithinEpsilon(relativeEularRotation.Roll) && Math.Abs(angularVelocity.z) <= angularAcceleration.z)
            //{
            //    angularVelocity.z = 0;
            //    angularAcceleration.z = 0;
            //}
            //else
            //{
            //    slowDownTime = angularAcceleration.z / relativeEularRotation.Roll;
            //    t = relativeEularRotation.Roll / angularVelocity.z;
            //    if (t > slowDownTime)
            //        angularVelocity.z += angularAcceleration.z;
            //    else
            //        angularVelocity.z -= angularAcceleration.z;
            //}

            angularVelocity.x = angularVelocity.z = 0; // June.4.2017 - sticking to 2D navigation only.
            Euler rotationThisFrame = new Euler(angularVelocity.y, angularVelocity.x, angularVelocity.z);
            currentOrientation.Add(rotationThisFrame); 
            mHelm.Orientation = currentOrientation;

            if (angularVelocity == Vector3d.Zero())
            {
                mState = ManeuverState.Completed;
                // TODO: if any previous thrusters were on, turn them off (fx and audio)
                // and perhaps run a cooldown animation
            }
            else
            {
                // based on our accelerations, animate & play sound for thrusters for appropriate axis
                //
                // since these thrusters are part of the vehicle, we can perhaps
                // play the animation as part of the vehicle entity animation and not as part of
                // a thruster entity animation

                // here we are obviously tracking the state of each thruster, 
                // if we store & update variables related to the state of each thruster then our script
                // can make the calls to execute a firing/shutdown, audo play.  But i think this maneuver
                // illustrates why HelmState may be ok for storing vars we want to pass to maneuver
                // the maneuver functions should probably be defined in script and not in this maneuver
                // object? i dunno.
                // because ideally id like to be able to just call the api right here, not have to
                // flag a variable so the script can make a call to play/stop an audio or
                // play/shutdown an animation

                // this could be a good argument i think for making sure every thruster is in fact
                // a seperate entity since animation state management is only needed for one, not every thruster
                // so lets think in terms of calling
                // FX such as exhaust plume and playing an audio file
                // - if every thruster is it's on entity, it can track it's own state
                // in it's Update() function and enable/disable animations and audios on it's own.
                // Vehicle would still need to be able to set the on/off states however
                // and so would know that those on/off vars exist for that entity type

            }
            // TODO: setting a new waypoint prior to reaching goal of current will not cause
            // a new maneuver to be created.  The previous goal must be reached first.


            // TODO: our second maneuver is once oriented, to start accelerating towards
            // a position in space

            // TODO: our third maneuver should be to orient and maneuver towards a changing target
            // as a missile would

            // TODO: third maneuver would be to reach and orbit perhaps,
            
            // TODO: etc

        }

        private const double AngleEpsilon = 0.001 * 0.0174532925;  
        private bool WithinEpsilon(double value)
        {
            return Math.Abs(value) <= AngleEpsilon;
        }
 

        // navigate to waypoint using thrusters

        //  - orient to target

        //  - thrust towards target when orientation is within some epsilon
        

    }
}
