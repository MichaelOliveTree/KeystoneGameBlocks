using System;
using System.Collections.Generic;
using Keystone.Types;
using Lidgren.Network;

namespace Game01.GameObjects
{
    // stored as a private variabable within the script
    public class HelmState : GameObject
    {
        private string mVehicleID;
        string mHelmStationID;
        string mOperatorID;
        // current state // todo: i think these "current state" vars should just be grabbed from Entity physics body??
        private Vector3d mPosition;
        private Vector3d mVelocity;
        private Vector3d mAcceleration;
        private Vector3d mAngularVelocity;
        private Vector3d mAngularAcceleration;
        private Euler mEulerRotation;

        // todo: max acceleration needs to be grabbed from the ship.css customProperties
        private Queue<Maneuver> mManeuvers;
        public Maneuver CurrentManeuver;
        //      void SelectManeuver() {} 
        //      string[] Engines;  // this array can never change except when re-fitting or
        //                         // designing a new ship.
        //                         // we know the custom property names of our ships because
        //                         // they are our scripts.  so we can query custom property values
        //                         // of these engines to determine what thrust we have
        //                         // at our disposal.
        //                         // todo: we need to InitializeHelmState() from script to assign the Engine IDs.
        // 

        public NavPoint[] NavPoints;

        // TODO: but how do we track which thrusters are of which axis?
        // there are 6 axis if we treat positive and negative quadrants as distinct. 
        // -x, +x, -y, +y, -z, +z
        // also in addition to rotating by adjusting the firing of these thrusters
        // we can strafe without changing rotation.
        // now our helm state would like to be able to cache some data about engine state so that
        // we dont have to query this data in the script.  but maybe we should for now just query the
        // data and optimize later?
        // Still, our VehicleAPI i think can have .ManeuverStrafe, .ManeuverRotateX,Y,Z
        //
        private int mThrustersEnabled;  // bitflag to track thrusters that are enabled
        private string[] mThrusterIDs;  // up to 32 thrusters allowed


        // todo: perhaps every crew station has a State GameObject
        public HelmState(string vehicleID, int type) : base (type)
        {
            mEulerRotation = Euler.IDENTITY;

            Initialize(vehicleID);
        }

        public Euler Orientation { get { return mEulerRotation; } set { mEulerRotation = value; } }
        public Vector3d Position { get { return mPosition; } set { mPosition = value; } }
        public Vector3d Velocity { get { return mVelocity; } set { mVelocity = value; } }
        public Vector3d Acceleration { get { return mAcceleration; } set { mAcceleration = value; } }
        public Vector3d AngularVelocity { get { return mAngularAcceleration; } set { mAngularAcceleration = value; } }
        public Vector3d AngularAcceleration { get { return mAngularAcceleration; } set { mAngularAcceleration = value; } }

        public int EnabledThrusterCount 
        { 
            get 
            { 
                return 0; //  TODO: iterate and count up the enabled flags
            } 
        }

        void Initialize (string vehicleID)
        {
            mVehicleID = vehicleID;

            // reset vars
            mThrusterIDs = null;
            CurrentManeuver = null;
            mThrustersEnabled = 0;
            
            // get all engines.  We create a new HelmState
            // whenever a ship leaves dock.  This will allow us to
            // upgrade engines / add / remove engines, and thus
            // be dynamic and not forced to keep one configuration

            // 
        }

        public override void Read(NetBuffer buffer)
        {
            base.Read(buffer);
        }

        public override void Write(NetBuffer buffer)
        {
            base.Write(buffer);
        }
    }
}

// E:\dev\_projects\Orbiter\Rocket.ctl
//Public Sub Tick()
//Dim R, g, z, tx, ty, v, gx, gy, delta, a As Double
//
//R = Sqr(rx * rx + ry * ry)
//v = Sqr(vx * vx + vy * vy)
//
//' to keep things simple, we'll just test for the rocket base being in the
//' earth (i.e. crash landed)
//If R >= re Then
//    ' alter the position
//    Extender.Top = e_ceny + ry - Height
//    Extender.Left = e_cenx + rx - Width / 2
//    
//'   calculate the new thrust angle
//'   we have to be careful about small angles and the sign because the
//'   machine approximations are not good enough to deal with these correctly
//    delta = alpha
//    If v > 0.01 Then
//    z = Atn(Abs(vx) / Abs(vy))
//        If vx > 0 Then
//            If vy > 0 Then
//                alpha = pi - z ' vx, vy > 0
//            Else
//                alpha = z ' vx > 0, vy < 0
//            End If
//        Else
//            If vy > 0 Then ' vx < 0, vy > 0
//                alpha = pi + z
//            Else
//                alpha = 2 * pi - z ' vx, vy < 0
//            End If
//        End If
//    End If
//    ' calculate the acceleration due to the burn ...
//    ' ... if there is any fuel left to burn
//    If m > 0 Then
//        a = m_BurnRate * m_FuelVelocity / (m + m_Payload)
//        ' reduce the rocket's fuel mass by the amount of fuel burnt
//        m = m - m_BurnRate
//    Else
//        a = 0
//    End If
//    
//    ' set gravity
//    g = g_0 * re * re / R / R
//    ' set thrust components
//    If v <> 0 Then
//        tx = a * vx / v
//        ty = a * vy / v
//    Else ' crudely avoid problems with zero velocity
//        tx = a * Sin(alpha)
//        ty = a * Cos(alpha)
//    End If
//    ' set gravity components
//    gx = g * rx / R
//    gy = g * ry / R
//    
//    ' alter the speeds and distances
//    vx = vx + tx - gx
//    vy = vy + ty - gy
//    rx = rx + vx
//    ry = ry + vy
// 
//    ' point in the new direction
//    NoseCone.Rotate (delta - alpha)
//    
//    ' tell the world what's happened
//    RelativeX = rx
//    RelativeY = ry
//    EffectiveG = g
//    Acceleration = a
//    Velocity = v
//End If
//
//End Sub


// forward vector from quaternion
// http://forum.unity3d.com/threads/26921-Quaternions-forward-direction

// combining thrusters on each axis
// http://stackoverflow.com/questions/6906573/xna-quaternion-createfromaxisangle

// http://bulletphysics.org/Bullet/phpBB3/viewtopic.php?f=4&t=1818
// create an absolute quaternion using the dir as an axis
// TODO: i believe the below though is creating a rotation from the existing
// to the dest?
//Quaternion currentRotation = (Quaternion)EntityAPI.GetPropertyValue(entityID, "rotation");
//Quaternion rotation = Quaternion.GetRotationTo (Vector3d.Forward(), dir, Vector3d.Up());

// gary's mod spacecraft autopilot 3.6
// http://www.youtube.com/watch?v=NcV5XLzDCVc

// Rodrigues formula rotation
// http://stackoverflow.com/questions/4011442/how-does-this-code-work
// http://en.wikipedia.org/wiki/Euler%E2%80%93Rodrigues_formula

// http://stackoverflow.com/questions/1171849/finding-quaternion-representing-the-rotation-from-one-vector-to-another
// http://scholar.lib.vt.edu/theses/available/etd-08242003-133909/unrestricted/turneretd.pdf

// http://stackoverflow.com/questions/3684269/component-of-a-quaternion-rotation-around-an-axis
// http://www.garrysmod.org/downloads/?a=view&id=125426

// TODO: waypoints must always be in a coordinate system that is relative to 
//       the ship's region as far as being able to compute rotation and such.
// TODO: verify this rotation works regardless of whether ship is hierarchical child
//       to any number of parent entities.  Because i suspect the following is an
//       absolute rotation and not a local rotation.  But then again, i think 
// the only time a ship will inherit rotation of another entity is if it's a fighter
// inside a carrier or something.
// TODO: do i grab the yaw pitch roll from this resulting rotation and then
//       construct a maneuver sequence to do this?
// TODO: the angular velocity on any given axis must be 0 when we reach
//       the destination angle on that axis.  So we need to know our angularVelocity
//       and our capacity to accelerate and decelerate on each axis to know
//       when to keep accelerating and when to start deceleration.


// TODO: we do need retro thrusters at front of ship or else we cant slow to dock
//       
// TODO: we create a maneuver sequence based on these results and then we execute
//       until finished?

// TODO: i could assume
// 			1) all axial thrusters have same speed
//	       2) all axial thrusters are either enabled or disabled at same time.
//     this way i can always compute optimal rotation and exact time to slerp
//     and not worry about fuel for maneuvering thrusters
//  	     3) well, if they can be repaired always, then we can always fix any
//       out of control rotation that is preventing us from docking for instance

// find the interpolated quaternion that is halfway between
// these current and destination rotations

// now how do we determine the actual time to fire on the thrusters
// controlling each axis?
//http://stackoverflow.com/questions/11474121/weird-issue-quaternion-rotation-in-xna-space-simulator
// http://www.ogre3d.org/forums/viewtopic.php?t=44704

// TODO: Initially when we test our code, we will not use any component engines
// we can just simulate their existance here.  Intially we will just get
// rotation orientation to work.

// once that works, we can add the visuals.
// angular velocity has to do with the rates of rotation on each axis,
// the calculation we need though is for how long do we accelerate to reach
// halfway point so we can decelerate the second leg and then be at our desired
// orientation.  And to complicate things, if some thrusters are damaged, we may have
// to turn in the opposite direction if it's faster even though longer based on
// thrusters being damaged or something?
// I think first we need the angle between our current orientation
// and the destination.


// what is our current angular velocity?

// enable the engines and thrusters we need and disable the ones we dont


// if we have thrust from engines, accumulate it.
// http://sharpsteer.codeplex.com/
// http://code.google.com/p/opensteerdotnet/
// http://code.google.com/p/strategygame/source/browse/trunk/XBanksPlatform/XBanksPlatform/Object+Manager/Steering+Behaviors/Steering+Behaviors.cs?r=67
// http://pastebin.com/f7682667c
// http://www.codeproject.com/Articles/29323/Driving-Simulation-in-XNA
// http://www.red3d.com/cwr/steer/gdc99/
// is the waypoint a fixed point or dynamic target?




// enable the axial thrusters based on the rotations needed.
// and define a "burn time" to use such that we burn halfway
// there (if fuel conservation is low and priority is speed), then 
// TODO: axial thrusters can be controlled here along with their
//       fx.  They can be added as part of the vehicle.
//       the main engines can be accessed directly or perhaps
//       as a group in a single api call.


// is the target on an eliptical path or not
// or if it is eliptical, is it eccentric enough at current time slice
// to treat as straight line?


// first assume target is not moving, given the ships current velocity
// 
// do we need to start slowing down to either
// - achieve orbit
//		- our speed must be correct given our desired orbital altitude
//      - our velocity direction at insertion must be tangent 
//      if we want a perfectly circular orbit.  If not in perfectly circular
//      we can correct.
//			- "circularize orbit" could be a task to perform at the waypoint?
//			   or is this something that waypoints aren't responsible for but
//             part of a task that is added that gets triggered to run
//             once we've reached the waypoint?

// - come to full stop


// is the next waypoint we are en route to a final destination waypoint?
// if it is final, we must potentially slow down to reach orbit, or slow down
// to full stop to settle into a lagrange point.
// does the waypoint specify a velocity to use?
// are we interested in cruising to best utilize fuel or 
// to get there at best time (without running out of fuel ie. we can refuel 
// on arrival)
// 

// steer towards waypoint and while not at waypoint
// move towards it.  Determine halfway point if traveling with thrusters
// whether we must decelerate to
// - attain proper speed for orbit when reaching waypoint
// - verify we don't overshoot our waypoints if frame rate is bad
//   or if resuming from paused simulation, etc
// - 