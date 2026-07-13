using System;
using KeyCommon.Traversal;
using Keystone.Animation;
using Keystone.Animation;
using Keystone.Behavior;
using Keystone.Resource;
using Keystone.Types;
using Keystone.Utilities;

namespace KeyEdit.Workspaces
{
	// TODO: Add an "Address" property similar to OctreeOctant that will return the hierarchical location of the current node.

	// TODO: Add profiler blocks to test How long each BehaviorTree run takes per frame.

	
	/// <summary>
	/// </summary>
	public partial class EditorWorkspace   
	{
		
//             Viewpoint Behavior Tree Diagram
//     ------------------------------------------------
//				------- selector-----
//				|			|		|
//			   User	    Animated  Scripted-----------
//				|			|		 |				|
//				|		  FlyTo	 PathFollow  KeyFrame Timeline
//              |           
//				|		  
//				|
//		---------sequence------------------
//		|		|		  |         |	  |
//		init  chase	   selector  smooth shake
//				|	|
//			free	orbit  
//	
// TODO: In the above, could "init" belong in the sequence node above the "init" action node i have here?

		public static Keystone.Behavior.Composites.Composite CreateVPRootSelector () // selects between User, Animated and Scripted behaviors 
        {
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Composites.Selector));
        	Keystone.Behavior.Composites.Composite rootSelector = new Keystone.Behavior.Composites.Selector (id);
        	
        	// first 1st level child is sequence for chase, a nested selector, smoothen, and shake
        	id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Composites.Sequence));
        	Keystone.Behavior.Composites.Composite sequence = new Keystone.Behavior.Composites.Sequence (id);
			rootSelector.AddChild (sequence);
			
        	sequence.AddChild (CreateVPChaseBehavior());
        	
        	id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Composites.Selector));
        	Keystone.Behavior.Composites.Composite innerSelector = new Keystone.Behavior.Composites.Selector (id);
        	
			innerSelector.AddChild (CreateVPFree()); // a standard wasd camera controller behavior
			innerSelector.AddChild (CreateVPOrbitBehavior()); 
			sequence.AddChild (innerSelector);
			sequence.AddChild (CreateVPSmoothen());
			sequence.AddChild (CreateVPApplyBehavior());
			
			// second 1st level child is animation sequence
			id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Composites.Sequence));
        	Keystone.Behavior.Composites.Composite animationSequence = new Keystone.Behavior.Composites.Sequence (id);
        	rootSelector.AddChild (animationSequence);
        	
        	// add flyto animation behavior
        	animationSequence.AddChild (CreateVPFlyToBehavior());

        	// when flyto completes, we transition to "chase"
        	
       		return rootSelector;
        }
		
		public static Keystone.Behavior.Composites.Composite CreateOrbitAndSnapToBehavior()
		{
			string id = Repository.GetNewName (typeof (Keystone.Behavior.Composites.Selector));
			var rootSelector = new Keystone.Behavior.Composites.Selector (id);

			id = Repository.GetNewName (typeof(Keystone.Behavior.Composites.Sequence));
			var sequence = new Keystone.Behavior.Composites.Sequence (id);

			sequence.AddChild (CreateVPOrbitBehavior());
			sequence.AddChild (CreateVPApplyBehavior());
			
			rootSelector.AddChild (sequence);

			id = Repository.GetNewName (typeof (Keystone.Behavior.Composites.Sequence));
        	Keystone.Behavior.Composites.Composite snapToSequence = new Keystone.Behavior.Composites.Sequence (id);
        	rootSelector.AddChild (snapToSequence);
        	
        	// add flyto animation behavior
        	snapToSequence.AddChild (CreateVPSnapToBehavior());

        	// TODO: verify after snapto, we transition to orbit. this is not happening!
        	
			return rootSelector;
		}
		
		// orbit combined with on demand flyto behavior
		public static Keystone.Behavior.Composites.Composite CreateOrbitAndFlyToBehavior()
		{
			string id = Repository.GetNewName (typeof (Keystone.Behavior.Composites.Selector));
			var rootSelector = new Keystone.Behavior.Composites.Selector (id);
			
			id = Repository.GetNewName (typeof(Keystone.Behavior.Composites.Sequence));
			var sequence = new Keystone.Behavior.Composites.Sequence (id);

			sequence.AddChild (CreateVPOrbitBehavior());
			sequence.AddChild (CreateVPApplyBehavior());
			
			rootSelector.AddChild (sequence);

			id = Repository.GetNewName (typeof (Keystone.Behavior.Composites.Sequence));
        	Keystone.Behavior.Composites.Composite animationSequence = new Keystone.Behavior.Composites.Sequence (id);
        	rootSelector.AddChild (animationSequence);
        	
        	// add flyto animation behavior
        	animationSequence.AddChild (CreateVPFlyToBehavior());

        	// TODO: verify when flyto completes we transition to orbit.
        	
			return rootSelector;
		}
		
		// orbit behavior
		public static Keystone.Behavior.Composites.Composite CreateOrbitOnlyBehavior()
		{
			string id = Repository.GetNewName (typeof (Keystone.Behavior.Composites.Selector));
			var sequence = new Keystone.Behavior.Composites.Sequence (id);
			
			sequence.AddChild (CreateVPOrbitBehavior());
			sequence.AddChild (CreateVPApplyBehavior());
			
			return sequence;
		}

        public static Keystone.Behavior.Composites.Composite CreateIsometricBehavior()
        {

            string id = Repository.GetNewName(typeof(Keystone.Behavior.Composites.Selector));
            var sequence = new Keystone.Behavior.Composites.Sequence(id);

            sequence.AddChild(CreateVPIsometricBehavior());
            sequence.AddChild(CreateVPApplyBehavior());

            return sequence;
        }

        /// <summary>
        /// Applies the final translation and rotation to the Viewpoint entity which our camera's renderingcontext binds to.
        /// </summary>		
        /// <returns></returns>
        private static Keystone.Behavior.Actions.Action CreateVPApplyBehavior ()
        {
        	Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (viewpoint, elapsedSeconds) =>
        	{
				KeyCommon.Data.UserData data = viewpoint.BlackboardData;
				
        		// FAIL if not in User mode
				if (data.GetString ("control") != "user")
					return BehaviorResult.Fail;
				
				
	            Keystone.Types.Vector3d translationDelta = data.GetVector ("delta_translation");		      
	            viewpoint.Translate (translationDelta.x, translationDelta.y, translationDelta.z, false);
	
	            // note: setting rotation to the viewpoint does not update the camera
	            // instead the ViewpointController will read it
	            // and update the context.Rotation which will update the camera.
	            bool useQuaternionRotation = data.GetBool ("use_quaternion_rotation");
	            //if (useQuaternionRotation) 
	            	// NOTE: once we've computed a quaternion rotation using vectors, we no longer need to continue using a seperate "use_quaternion_rotation" path
	            	// TODO: Sept.10.2016 - cam_dest_smooth rotation is not producing the proper rotation at final camera rest position but cam_dest_rotation does
	            	viewpoint.Rotation = data.GetQuaternion ("cam_dest_rotation"); //"cam_dest_smooth");
	            	
	            //else
	            //{
	            //	Vector3d yawPitchRoll = data.GetVector ("yawpitchroll");
	            //	entity.SetRotation (yawPitchRoll.y, yawPitchRoll.x, yawPitchRoll.z);
	            //}
	            // clear delta_translation now that we're done with it
				data.SetVector ("delta_translation", Vector3d.Zero());
	            				
	            return BehaviorResult.Success;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action  (id, f);
        }
		
		 
    	// http://www.thebuddyforum.com/mediawiki/index.php/TreeSharp:_Adopting_a_Behavior_Tree_Developer_mindset
    	private static Keystone.Behavior.Actions.Action CreateVPFree()
    	{
    		Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (viewpoint, elapsedSeconds) =>
        	{
				KeyCommon.Data.UserData data = viewpoint.BlackboardData;
				
				if (viewpoint.Animations != null && viewpoint.Animations.IsPlaying())
				{
					// TODO: how do we know if this animation has nothing to do with the rotation though?  Presumeably
					//       because behaviortree is controlling all animations!
					// If animation is controlling our translation and rotation
					// set smooth and final to existing rotation
					data.SetQuaternion ("cam_dest_smooth", viewpoint.Rotation);
					data.SetQuaternion ("cam_dest_rotation", viewpoint.Rotation);
					return BehaviorResult.Fail;
				}

				// FAIL if not in User mode and free mouse behavior
				bool userControlled = data.GetString ("control") == "user";
				if (userControlled == false)
					return BehaviorResult.Fail;
				
				
				// reset deltas before accumulating inputs
				data.SetPoint ("mouse_deltas", new System.Drawing.Point (0,0)); 

				ProcessInputs(viewpoint, data);
				
	        	Quaternion destinationRotation = data.GetQuaternion ("cam_dest_rotation");
					        	
	        	double mouseSpeed = data.GetDouble("mouselookspeed");
	        	System.Drawing.Point mouseDeltas = data.GetPoint ("mouse_deltas");
	        	Vector3d angle = Vector3d.Zero();
	        	angle.y = MathHelper.WrapAngle(mouseDeltas.X * mouseSpeed); // mouse x axis movement controls horizontal rotation about world Y axis (YAW / HORIZONTAL)
				angle.x = MathHelper.WrapAngle(mouseDeltas.Y * mouseSpeed); // PITCH / VERTICAL	
					          

				bool useQuaternionRotation = data.GetBool ("use_quaternion_rotation");
				
				
				// compute new destinationRotation if mouse has moved on either axis
				if (angle.x != 0d || angle.y != 0d)
				{
					if (useQuaternionRotation)
					{
						// contains delta look angles.  We should now be creating a quat and using this to compute destination quat
						//Quaternion rotationDelta = new Keystone.Types.Quaternion (angle.y * MathHelper.DEGREES_TO_RADIANS, 
					    //                                                     angle.x * MathHelper.DEGREES_TO_RADIANS, 
					    //                                                     angle.z * MathHelper.DEGREES_TO_RADIANS);
						
						// TEMP
						Matrix rotMatrix = new Matrix (viewpoint.Rotation); 
											
						Vector3d yRotation = new Vector3d (0d, 1d, 0d); 
						Vector3d xRotation = new Vector3d (1d, 0d, 0d); 
						
						// transform each by current Rotation so that we're in the same axis system
						yRotation = Vector3d.TransformCoord (yRotation, rotMatrix); 
						xRotation = Vector3d.TransformCoord (xRotation, rotMatrix); 
						
						Quaternion yquat = new Quaternion (yRotation , angle.y * MathHelper.DEGREES_TO_RADIANS);
						Quaternion xquat = new Quaternion (xRotation , angle.x * MathHelper.DEGREES_TO_RADIANS);
									
						Quaternion rotationDelta = yquat * xquat;
						// END TEMP
		
			            // http://kreationsedge.net/?p=608
		            	// http://stackoverflow.com/questions/319189/should-quaternion-based-3d-cameras-accumulate-quaternions-or-euler-angles
		            	// http://xboxforums.create.msdn.com/forums/p/77029/467846.aspx
		            	// http://number-none.com/product/Understanding%20Slerp,%20Then%20Not%20Using%20It/
		            	// http://www.ogre3d.org/tikiwiki/Quaternion+and+Rotation+Primer
		            	// NOTE: Quaternions concatenate backwards from matrices so the existing quaternion is the second parameter in the multiply operator <-- TODO: this doesnt seem true with my multiply operator, seems to screw up royally.  could jsut be my implementation of multiply operator
						destinationRotation = Quaternion.Normalize(Quaternion.Normalize(rotationDelta) * destinationRotation);
						//destinationRotation = Quaternion.Normalize(Quaternion.Concatenate (destinationRotation, Quaternion.Normalize(rotationDelta)));
						
					}
					else // standard vector rotations since quaternions add unintended camera roll (that so far I haven't been able to prevent/eliminate)
					{
						// accumulate rotation delta to existing rotation
						Vector3d yawpitchroll = data.GetVector ("yawpitchroll");
						yawpitchroll.x = MathHelper.WrapAngle(yawpitchroll.x + angle.x);
						yawpitchroll.y = MathHelper.WrapAngle(yawpitchroll.y + angle.y);
						yawpitchroll.z = 0;
						data.SetVector ("dest_yawpitchroll", yawpitchroll);
						
						destinationRotation = new Quaternion (yawpitchroll.y  * MathHelper.DEGREES_TO_RADIANS, 
						                                      yawpitchroll.x  * MathHelper.DEGREES_TO_RADIANS, 
						                                      yawpitchroll.z * MathHelper.DEGREES_TO_RADIANS);
					}
        		}
        	
				data.SetQuaternion ("cam_dest_rotation", destinationRotation);
				
				// translation in the look direction or backwards or orthogonal strafing
				Matrix rotationMatrix = new Matrix (destinationRotation);
				  
				Vector3d panning = data.GetVector ("cam_move_dir");
				double panningSpeed = data.GetFloat("cam_speed");

	            double speedThisFrame = elapsedSeconds * panningSpeed;
	
	            // NOTE: you don't need to compute a lookAt vector from a quat.  You just transform the
				// movement unit direction vector by local rotation matrix to find pan direction in
				// local space and scale by speed this frame for translation delta
	            Vector3d translation = Vector3d.TransformNormal (panning, rotationMatrix) * speedThisFrame;

	            // accumulate with any translation made by chase or previous behavior
//	            translation += data.GetVector ("delta_translation");
	            data.SetVector ("delta_translation", translation); 
				return  BehaviorResult.Success ;
			};
			

        	//TODO: if pan was modified shouldnt we be normalizing the result?
        	// 	        		// if keyboard panning, update the "direction" variable to be = 1
    		// in every direction and then normalize it right?  cuz i dont think we have been which means going diagonally is moving faster.  i could be wrong.
    		string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
    		return new Keystone.Behavior.Actions.Action (id, f);
    	}
    

    	private static Keystone.Behavior.Actions.Action CreateVPOrbitBehavior()
    	{
    		Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (viewpoint, elapsedSeconds) =>
        	{
        		// TODO: eventually, will we not even allow context.Entity but .EntityID?
        		//       -and then EntityAPI.GetCustomData(entityID) to retrieve the entire UserData store?
				KeyCommon.Data.UserData data = viewpoint.BlackboardData;

				// FAIL if not in User mode
				if (data.GetString ("control") != "user")
					return BehaviorResult.Fail;
				
				
				string focusEntityID = data.GetString ("focus_entity_id");
				if (string.IsNullOrEmpty (focusEntityID))
                { 
                    // TODO: can we remain focussed on the StarMap entity as a whole
                    //       whilst still zooming down to an individual star?
					data.SetString ("prev_focus_entity_id", null);
					return BehaviorResult.Fail;
				}
				//else if (data.GetString ("prev_focus_entity_id") != focusEntityID)
				//	// setting orbital_radius here overrides that set by caller
				//	data.SetDouble ("orbit_radius", -80d);

				
                // if previous behavior was "behavior_default" then we need to transition to this view angle.  Slerping
                // to a starting rotation will also eliminate any roll incurred during the preview camera behavior mode
                //if (data.GetString ("previous_behavior", "behavior_free"))
                //{
//    				data.SetString ("previous_behavior" = "behavior_orbit");
//    				data.SetString ("previous_control" = "user");
//    				data.SetString ("control", "animated"); 
//    				data.SetString ("behavior", "animation_flyto"); 

//					// flyto_coordinate as opposed to flyto_entity
//                  // TODO: i think ideally this means that this OrbitBehavior should be a selector node
//					//       where one of the children is orbit, the other is the transition of the viewpoint
//					//		 to a starting orbit position and orientation
//					ComputeFlyToArguments (entity, data, out );

//              	return  BehaviorResult.Success ;
                //}
 				// else // previous behavior was our transition animation so we're now ready to just orbit via input
 				

				
				
				// TODO: each magnification level change needs to update the min/max view distances so we can constrain zoom distance
				// double minViewDistance = data.GetDouble ("min_view_distance");
				// double maxViewDistance = data.GetDouble ("max_view_distance");
				
                // TODO: initial camera pitch should be viewAngle and set as part of destinationRotation in "flyto" transition
                double orbitViewAngle = data.GetDouble ("orbit_view_angle");
                
 				
				// reset delta before accumulating inputs
                // TODO: to eliminate unintended z axis roll, maybe we should store
                //       full rotations on each axis and not just deltas
				data.SetPoint ("mouse_deltas", new System.Drawing.Point (0,0)); 

				ProcessInputs(viewpoint, data);
				
				
	        	double mouseSpeed = data.GetDouble("mouselookspeed");
                // TODO: i'm pretty sure this is total accumulated mouseDeltas, and NOT just per frame deltas
	        	System.Drawing.Point mouseDeltas = data.GetPoint ("mouse_deltas");
	        	Vector3d angle = Vector3d.Zero();
	        	angle.y = MathHelper.WrapAngle(mouseDeltas.X * mouseSpeed); // mouse x axis movement controls horizontal rotation about world Y axis (YAW(heading) / HORIZONTAL)
				angle.x = MathHelper.WrapAngle(mouseDeltas.Y * mouseSpeed); // PITCH / VERTICAL	

                // TODO: limit rotation angles for each applicable axis if limitX, limitY or limitZ is set
                // TODO: for starmap, we want 0 z-axis rotation, 85-95 x axis rotation, and unrestrained y axis rotation



                //Quaternion deltaRotation = new Keystone.Types.Quaternion (angle.y * MathHelper.DEGREES_TO_RADIANS, 
                //                                                     angle.x * MathHelper.DEGREES_TO_RADIANS, 
                //                                                     angle.z * MathHelper.DEGREES_TO_RADIANS);

                // when concatenating, the z component will start to roll if one of x and y are not level.  We need to force it to remain 0.
                // this wouldn't be a problem if computing a single cumulative rotation from start, but we are concatenating delta rotations
                //	 Quaternion desiredRotation = entity.Rotation  * Quaternion.Normalize(deltaRotation);
                //   Quaternion desiredRotation = Quaternion.Concatenate (entity.Rotation, Quaternion.Normalize(deltaRotation));


                // TEMP
                Matrix rotMatrix = new Matrix (viewpoint.Rotation); 
				
				Vector3d yRotation = new Vector3d (0, 1, 0); 
				Vector3d xRotation = new Vector3d (1, 0, 0); 
				
				// transform each by current Rotation so that we're in the same axis system
//				yRotation = Vector3d.TransformCoord (yRotation, rotMatrix); 
//				xRotation = Vector3d.TransformCoord (xRotation, rotMatrix); 
				rotMatrix = Matrix.CreateRotationX (angle.x * MathHelper.DEGREES_TO_RADIANS);
				yRotation = Vector3d.TransformCoord (yRotation, rotMatrix);
				
				Quaternion yquat = new Quaternion (yRotation, angle.y * MathHelper.DEGREES_TO_RADIANS);			
				Quaternion xquat = new Quaternion (xRotation, angle.x * MathHelper.DEGREES_TO_RADIANS);

                Quaternion deltaRotation = xquat * yquat;
                // TODO: concatenate with delta probably is giving us precision error accumulation
                //       I think i had a reason for using deltas but i have to revisit why that is
                //       I think it might have had something to do with smoothing mouse movement
				Quaternion desiredRotation = Quaternion.Normalize(Quaternion.Concatenate (viewpoint.Rotation, Quaternion.Normalize(deltaRotation)));

                // INNER TEMP - lets compute quaternion directly from camera angles
                // TODO: we seem to be getting z-axis rotation that we don't want
                // TODO: I think we need to construct our quats from yaw, pitch, roll
                // http://stackoverflow.com/questions/42263325/3d-camera-has-unintended-roll   
                Vector3d mouseAngles = data.GetVector("cam_mouse_angles");
                mouseAngles.x += angle.x;
                mouseAngles.y += angle.y;                                                                           
                
                // TODO: limit mouse angles for relevant axis 

                desiredRotation = new Quaternion(mouseAngles.y * MathHelper.DEGREES_TO_RADIANS, mouseAngles.x * MathHelper.DEGREES_TO_RADIANS, 0);
                desiredRotation = Quaternion.Normalize(desiredRotation);

                data.SetVector("cam_mouse_angles", mouseAngles);
                // END INNER TEMP
                // END TEMP


                // for orbital radius, find the delta translation along z axis only since orbit camera will only yaw and move in and out at fixed pitch angle
                Vector3d panning = data.GetVector ("cam_move_dir");
                panning.x = 0;
                panning.y = 0;
                
                double panningSpeed = data.GetFloat("cam_speed");
	            double speedThisFrame = elapsedSeconds * panningSpeed;

				double orbitRadius = data.GetDouble ("orbit_radius");
                orbitRadius += panning.z * speedThisFrame;
                
                // limit orbitRadius to min/max values
                orbitRadius = Math.Max (orbitRadius, data.GetDouble ("orbit_radius_min"));
                orbitRadius = Math.Min (orbitRadius, data.GetDouble ("orbit_radius_max"));

                // based on the distance, compute a height that is x degrees above target.y.  
                // this angle should be negative value of angle.x
                double altitude = Math.Tan (MathHelper.DEGREES_TO_RADIANS * -angle.x) * orbitRadius;

                // NOTE: you don't need to compute a lookAt vector from a quat.  You just transform the
                // movement unit direction vector by local rotation matrix to find pan direction in
                // local space and scale by speed this frame for translation delta
                Matrix rotationMatrix = new Matrix (desiredRotation);
                
                Vector3d coord = new Vector3d (0, 0,  orbitRadius);
                Vector3d orbitTranslation = Vector3d.TransformNormal (coord, rotationMatrix);
				
                // IMPORTANT: If viewpoint.mInheritScale == true, this will actually cause the viewpoint to inherit the rotation of the Entity it's attached to 
                //            which can for example be the starship Vehicle that is the focusEntity.
                // TODO: do we have precision issues far from origin when GetGlobalPosition() is called?
				Vector3d focusEntityPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace (focusEntityID);
                focusEntityPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace(focusEntityID);
                Vector3d derived;
                Vector3d global;
                AppMain.mScriptingHost.EntityAPI.GetPosition(focusEntityID, out focusEntityPosition, out derived, out global);
                

                Vector3d diff = derived - viewpoint.Translation; // viewpoint.GlobalTranslation;
                // May.18.2017 - not using the global values here breaks the ability of the camera/viewpoint to follow the ship, no idea why.
                //               still i have worries that this will break at distances really far from origin.  must test soon.
                diff = global - viewpoint.GlobalTranslation;

                orbitTranslation += diff;

                // TODO: does this break if our orbit rotation causes us to cross a zone boundary such that the viewpoint
                //       and focusEntity are in different zones? 
                Vector3d translationDelta = orbitTranslation;

                // store our results
                data.SetVector ("prev_focus_translation", focusEntityPosition);
                data.SetString ("prev_focus_entity_id", focusEntityID);
                
                data.SetDouble ("orbit_radius", orbitRadius);      
                data.SetQuaternion ("cam_dest_rotation", desiredRotation);                
                data.SetVector ("delta_translation", translationDelta); 
				return  BehaviorResult.Success;
 			};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
    	}

        // todo: update for viewpoint Entity must occur every frame. verify this is true.
        // This viewpoint is for viewing starship interiors. We need to be able to track the
        // Vehicle's position, but maintain relative position with regards to Interior.  We also
        // need to be able to rotate around the focal point (offset) as well as pan and zoom.  we also need
        // to restrict the vertical rotation angle.  This is really a chase behavior with extra functionality as well as limits. i can probably combine the mods into the ChaseBehavior.
        // or maybe it can just be another behavior node that modifies what the Chase behavior has already computed before ApplyBehavior
        private static Keystone.Behavior.Actions.Action CreateVPIsometricBehavior()
        {
            Func<Keystone.Entities.Entity, double, BehaviorResult> f;

            f = (viewpoint, elapsedSeconds) =>
            {
                // TODO: eventually, will we not even allow context.Entity but .EntityID?
                //       -and then EntityAPI.GetCustomData(entityID) to retrieve the entire UserData store?
                KeyCommon.Data.UserData data = viewpoint.BlackboardData;

                // FAIL if not in User mode
                if (data.GetString("control") != "user")
                    return BehaviorResult.Fail;


                string focusEntityID = data.GetString("focus_entity_id");
                if (string.IsNullOrEmpty(focusEntityID))
                {
                    // TODO: can we remain focussed on the StarMap entity as a whole
                    //       whilst still zooming down to an individual star?
                    data.SetString("prev_focus_entity_id", null);
                    return BehaviorResult.Fail;
                }
                //else if (data.GetString ("prev_focus_entity_id") != focusEntityID)
                //	// setting orbital_radius here overrides that set by caller
                //	data.SetDouble ("orbit_radius", -80d);


                // if previous behavior was "behavior_default" then we need to transition to this view angle.  Slerping
                // to a starting rotation will also eliminate any roll incurred during the preview camera behavior mode
                //if (data.GetString ("previous_behavior", "behavior_free"))
                //{
                //    				data.SetString ("previous_behavior" = "behavior_orbit");
                //    				data.SetString ("previous_control" = "user");
                //    				data.SetString ("control", "animated"); 
                //    				data.SetString ("behavior", "animation_flyto"); 

                //					// flyto_coordinate as opposed to flyto_entity
                //                  // TODO: i think ideally this means that this OrbitBehavior should be a selector node
                //					//       where one of the children is orbit, the other is the transition of the viewpoint
                //					//		 to a starting orbit position and orientation
                //					ComputeFlyToArguments (entity, data, out );

                //              	return  BehaviorResult.Success ;
                //}
                // else // previous behavior was our transition animation so we're now ready to just orbit via input


                // August.20.2022 - lets see if we can alter the offset and enforce bounds and altitude in local space
                Vector3d offset = data.GetVector("offset");
                Vector3d minBounds = data.GetVector("min_bounds");
                Vector3d maxBounds = data.GetVector("max_bounds");
                double max_altitude = data.GetDouble("max_altitude");
                double min_altitude = data.GetDouble("min_altitude");
                double min_VerticalAngle = data.GetDouble("min_vertical_angle");
                double max_VerticalAngle = data.GetDouble("max_vertical_angle");
                // TODO: each magnification level change needs to update the min/max view distances so we can constrain zoom distance
                // double minViewDistance = data.GetDouble ("min_view_distance");
                // double maxViewDistance = data.GetDouble ("max_view_distance");

                // TODO: initial camera pitch should be viewAngle and set as part of destinationRotation in "flyto" transition
                double orbitViewAngle = data.GetDouble("orbit_view_angle");
                double mouseSpeed = data.GetDouble("mouselookspeed");

                // reset delta before accumulating inputs
                // TODO: to eliminate unintended z axis roll, maybe we should store
                //       full rotations on each axis and not just deltas
                data.SetPoint("mouse_deltas", new System.Drawing.Point(0, 0));

                ProcessInputs(viewpoint, data);

                System.Drawing.Point mouseDeltas = data.GetPoint("mouse_deltas");
                Vector3d angle = Vector3d.Zero();
                angle.y = MathHelper.WrapAngle(mouseDeltas.X * mouseSpeed); // mouse x axis movement controls horizontal rotation about world Y axis (YAW(heading) / HORIZONTAL)
                //angle.x = MathHelper.WrapAngle(mouseDeltas.Y * mouseSpeed); // PITCH / VERTICAL	
                angle.x = mouseDeltas.Y * mouseSpeed; // PITCH / VERTICAL	// NOTE: we don't wrap here because we want the range to be from -180 to 180.

                // http://stackoverflow.com/questions/42263325/3d-camera-has-unintended-roll   
                Vector3d mouseAngles = data.GetVector("cam_mouse_angles");
                mouseAngles.x += angle.x;
                mouseAngles.y += angle.y;

               // mouseAngles.x = MathHelper.WrapAngle(mouseAngles.x); // NOTE: if we wrap, we need a special range from -180 to 180 and not 0 - 360
                mouseAngles.y = MathHelper.WrapAngle(mouseAngles.y);
                mouseAngles.x = MathHelper.Clamp(mouseAngles.x, min_VerticalAngle, max_VerticalAngle); // clamp has the same effect as wrapping anyways so we dont really need a specialized "clamp()" function for Vertical angles
                data.SetVector("cam_mouse_angles", mouseAngles);

                Quaternion yRotation = new Quaternion(mouseAngles.y * MathHelper.DEGREES_TO_RADIANS, 0d, 0d);
                yRotation = Quaternion.Normalize(yRotation);
                Quaternion desiredRotation = new Quaternion(mouseAngles.y * MathHelper.DEGREES_TO_RADIANS, mouseAngles.x * MathHelper.DEGREES_TO_RADIANS, 0d);
                desiredRotation = Quaternion.Normalize(desiredRotation);


                // for orbital radius, find the delta translation along z axis only since orbit camera will only yaw and move in and out at fixed pitch angle
                Vector3d panning = data.GetVector("cam_move_dir");              

                // NOTE: you don't need to compute a lookAt vector from a quat.  You just transform the
                // movement unit direction vector by local rotation matrix to find pan direction in
                // local space and scale by speed this frame for translation delta
                Matrix yRotationMat = Matrix.CreateRotationY(mouseAngles.y * MathHelper.DEGREES_TO_RADIANS);
                panning = Vector3d.TransformCoord(panning, yRotationMat); // when this line is commented out, the rotation offset works great. but then the controls for panning aren't correct
                offset += panning;
                data.SetVector("offset", offset);

                // todo: im not using the panningSpeed so its frame rate dependant
                double panningSpeed = data.GetFloat("cam_speed");
                double speedThisFrame = elapsedSeconds * panningSpeed;

                bool smoothZoom = data.GetBool("smooth_zoom_enabled");
               
                double currentRadius = data.GetDouble("orbit_radius");
                double destinationRadius = data.GetDouble("orbit_radius_destination");

                if (smoothZoom && currentRadius != destinationRadius)
                {
                    double smoothTime = data.GetDouble("smooth_zoom_time"); // milliseconds
                    double eplapsed = data.GetDouble("smooth_zoom_elapsed");

                    double t = eplapsed + elapsedSeconds;
                    t = t / smoothTime;
                    t = MathHelper.Clamp(t);
                    currentRadius = MathHelper.Lerp(currentRadius, destinationRadius, t);

                    data.SetDouble("smooth_zoom_elapsed", eplapsed + elapsedSeconds);
                }
                else
                    data.SetDouble("smooth_zoom_elapsed", 0d);

                currentRadius = MathHelper.Clamp(currentRadius, data.GetDouble("orbit_radius_min"), data.GetDouble("orbit_radius_max"));
                

                // IMPORTANT: If viewpoint.mInheritScale == true, this will actually cause the viewpoint to inherit the rotation of the Entity it's attached to 
                //            which can for example be the starship Vehicle that is the focusEntity.
                // TODO: do we have precision issues far from origin when GetGlobalPosition() is called?
                Vector3d focusEntityPosition;
                Vector3d derived;
                Vector3d global;

                // if player's ship is destroyed, we will just lock the movearound point to the last good position
                // NOTE: actually the above does not happen because if "focus_entity_id" is null, it just returns failed and the camera position is locked
                if (!string.IsNullOrEmpty(focusEntityID))
                    AppMain.mScriptingHost.EntityAPI.GetPosition(focusEntityID, out focusEntityPosition, out derived, out global);
                else
                    global = data.GetVector("prev_focus_translation");

                // May.18.2017 - not using the global values here breaks the ability of the camera/viewpoint to follow the ship, no idea why.
                //               still i have worries that this will break at distances really far from origin.  must test soon.
                Vector3d newPosition = MathHelper.MoveAroundPoint(global + offset, -currentRadius, mouseAngles.y * MathHelper.DEGREES_TO_RADIANS, mouseAngles.x * MathHelper.DEGREES_TO_RADIANS);
                Vector3d translationDelta = newPosition - viewpoint.GlobalTranslation;

                // TODO: get the focus entity working to follow individual crew members as they traverse the Interior

                // store our results
                data.SetVector("prev_focus_translation", global); // focusEntityPosition); // todo: this isn't using the focusEntity's globalTranslation.  is this ok in all cases?
                data.SetString("prev_focus_entity_id", focusEntityID);

                data.SetDouble("orbit_radius", currentRadius);
                data.SetQuaternion("cam_dest_rotation", desiredRotation);
                data.SetVector("delta_translation", translationDelta);
                return BehaviorResult.Success;
            };

            string id = Keystone.Resource.Repository.GetNewName(typeof(Keystone.Behavior.Actions.Action));
            return new Keystone.Behavior.Actions.Action(id, f);
        }

   
        // chase MUST be combined with default, orbit or some type of lookAt behavior.  
        // - when combined with default, camera will remain relative to moving target object
        // and allow free mouse look and relative position changes with respect to target object.
        // - when combined with orbit, camera will remain relative to moving target object
        // and allow orbit mouse translating behavior and still allow relative zoom +/- with
        // respect to target.
        private static Keystone.Behavior.Actions.Action CreateVPChaseBehavior()
    	{
    		Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (viewpoint, elapsedSeconds) =>
        	{
				KeyCommon.Data.UserData data = viewpoint.BlackboardData;
				

//            	// http://devmaster.net/forums/topic/6630-a-camera-that-orbits-around-a-mesh-while-matching-its-roll/
//                // this simple chase option allows for simple relative camera position with respect
//                // to the target.  You maintain all other relative manual camera moves.
//                // apply translation change of the entity so that our relative position is unchanged
//                // TODO: could also apply dampening to allow camera to lag the target translation
//                // and speed up to it and slow down to reach it... basically smoothstep interpolation
//                // using a time value of say 1 second? or .75 second?
                               
//				  // TODO: shouldnt we also get the radius so that we can compute a scale value for our zooms?
//                //       - small ship has smaller chase zoom increments than a planet, star or galaxy for example
//                //       as well as custom min/max zoom values
//
//				 // TODO: flyto transition to halfway between min & max_chase_distance
//				 //        - but flyto on a fast moving entity will not work properly.  we can surely
//			     //        Vector3d Lerp without using an actual animation, but lerping with constantly moving
//               //        values will make the timing off... we would need to estimate where the target would be
//               //        at that future time if we want smooth lerping that isn't speeding up and slowing down constantly
//               //        but in the short term, flyto will be ok
//				 // TODO: if dampening, assign acceleration and deceleration?
				bool chase = data.GetBool ("chase_enabled");
				string focusEntityID = data.GetString ("focus_entity_id");
				if (string.IsNullOrEmpty (focusEntityID)) 
				{
					chase = false;
					data.SetString ("prev_focus_entity_id", null);
				}
				
				if (chase == false) return BehaviorResult.Success;
				
								
				// TODO: if .GetPosition returned nullable type, we'd know if there was a failure
                // todo: for these GetPosition() i probably need to get region space coords based on the focusEntityID and camera's region.
				Vector3d focusEntityPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace(focusEntityID);				
				Vector3d previousFocusEntityTranslation = focusEntityPosition;
				
				// if focusEntityID has changed, we must reset previousFocusEntityTranslation to equal current
				if (focusEntityID == data.GetString ("prev_focus_entity_id"))
						previousFocusEntityTranslation = data.GetVector("prev_focus_translation");

                Vector3d deltaTranslation = focusEntityPosition - previousFocusEntityTranslation;
    
                // store our results
                data.SetVector ("delta_translation", deltaTranslation);
                data.SetVector ("prev_focus_translation", focusEntityPosition);
                data.SetString ("prev_focus_entity_id", focusEntityID);
                return BehaviorResult.Success;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
    	}
    	
    	
    	private static Keystone.Behavior.Actions.Action CreateVPOrthographicBehavior()
    	{
    		Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (viewpoint, elapsedSeconds) =>
        	{
				Keystone.Entities.Entity target = viewpoint;
				KeyCommon.Data.UserData data = target.BlackboardData;
				
				
//				// can this behavior someone get access to the mContext?
//				// we could maybe viewpoint.QueueInput ("context"); // shrug?
//				
//				// THIS IF SHOULD HAVE BEEN READ TO CAUSE THIS BEHAVIOR  ALONG WITH ORTHO PERSPECTIVE CHECK if ((bool)mAxisStateVars["mousepan"])
//        		// TODO: the following can still be in a Behavior, but i feel like which behavior tree is set should be at least
//        		//       determined when switching from Ortho to Perspective for us rather than having to make that determination
//        		//       here and adding it as a node.
//        		System.Drawing.Point mouseStart = data.GetPoint("mousepanstart");
//        		System.Drawing.Point mouseEnd = data.GetPoint("mousepanend");
//        		
//	            Vector3d planeNormal = Vector3d.Up ();
//	            switch (mContext.ViewType)
//	            {
//	                case Viewport.ViewType.Top:
//	                    planeNormal = new Vector3d(0, 1, 0);
//	                    break;
//	                case Viewport.ViewType.Bottom:
//	                    planeNormal = new Vector3d(0, -1, 0);
//	                    break;
//	
//	                case Viewport.ViewType.Left:
//	                    planeNormal = new Vector3d(-1, 0, 0);
//	                    break;
//	                case Viewport.ViewType.Right:
//	                    planeNormal = new Vector3d(1, 0, 0);
//	                    break;
//	
//	                case Viewport.ViewType.Front:
//	                    planeNormal = new Vector3d(0, 0, -1);
//	                    break;
//	                case Viewport.ViewType.Back:
//	                    planeNormal = new Vector3d(0, 0, 1);
//	                    break;
//	
//	            }
//	            // TODO: for deckplans, the planeDistance should be the current deck's
//	            // y coordinate component right?  But 0 seems to be working for all zoom heights
//	            // although perhaps that's because the 0 distance we hardcoded now has the grid
//	            // plane always at 0!  So when we get into multilevel deckplans, we will have
//	            // some grids that are higher or lower then the center primary deck.
//	            double planeDistance = 0; // -mContext.Position.y;
//	            //planeDistance = -mContext.Position.y;
//	            Plane p = new Plane(planeNormal, planeDistance); // GetPlane(mSelectedAxes);
//	
//	            double hitDistanceStart, hitDistanceEnd;
//	            Ray start_ray, end_ray;
//	
//	            // cast rays into the scene from the mouse start and end points and
//	            // intersect the pick rays with the dual axis plane we want to move along
//	
//	            // if either of the intersections is invalid then bail out as it would
//	            // be impossible to calculate the difference
//	            if (!mContext.Viewport.Pick_PlaneStartEndRays(null, null, mouseStart, mouseEnd, p, out start_ray, out end_ray, out hitDistanceStart, out hitDistanceEnd))
//	            {
//	                //System.Diagnostics.Debug.WriteLine("error...");
//	                return BehaviorResult.Success;
//	            }
//	            // obtain the intersection points of each ray with the dual axis plane
//	            Vector3d start_pos = start_ray.Origin + (start_ray.Direction * hitDistanceStart);
//	            Vector3d end_pos = end_ray.Origin + (end_ray.Direction * hitDistanceEnd);
//	
//	            // calculate the difference between the intersection points
//	            Vector3d difference = end_pos - start_pos;
//	            //difference.y = 0;
//	             
//	            data.SetVector ("delta_translation", -difference);
           		return BehaviorResult.Success ;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
		}
	    		
		private static Keystone.Behavior.Actions.Action CreateVPSmoothen ()
		{
			Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (target, elapsedSeconds) =>
        	{
				KeyCommon.Data.UserData data = target.BlackboardData;
			
				double smoothing = 0.3; // higher the value the less the smoothing, but less sense of mouse lag
				
				Quaternion smooth;
				
				bool useQuaternionRotation = data.GetBool ("use_quaternion_rotation");
				if (useQuaternionRotation)
				{
					Quaternion current = target.Rotation;
	                Quaternion dest = data.GetQuaternion("cam_dest_rotation");
					
	                // TODO: our increment value is .1 which is 1/10 of the way towards our goal every
	                //       frame which makes our smoothing behave differently at different frame rates
	                //       i need to test this to see if this is actually a good thing since it'll 
	                //       appear to smooth more with worse frame rate or whether we want some
	                //       frame rate independant smooth rate so the smoothing is consistant
	                //       note: an increment of 0.1 seems to suggest we never actually reach our goal
	                //       we just get closer and closer.  perhaps this is good too as it prevents
	                //       any snapping.  And so long as currentRotation and targetRotation are not
	                //       the same value, it will always compute a closer new angle 
	                
	                
	                smooth = Quaternion.Slerp2 (current , dest, smoothing);
				}
				else 
				{
					//Euler e = new Euler (quat);
					Vector3d dest = data.GetVector ("dest_yawpitchroll");
					Vector3d yawpitchroll = data.GetVector ("yawpitchroll");
					yawpitchroll.x = InterpolationHelper.LerpAngle(yawpitchroll.x, dest.x, smoothing);
                	yawpitchroll.y = InterpolationHelper.LerpAngle(yawpitchroll.y, dest.y, smoothing);
                	// must update the current accumulated x,y,z axis rotations to the smoothed value
                	data.SetVector ("yawpitchroll", yawpitchroll);
                	
                	smooth = new Quaternion (yawpitchroll.y * MathHelper.DEGREES_TO_RADIANS, 
                	                        yawpitchroll.x * MathHelper.DEGREES_TO_RADIANS,
											yawpitchroll.z * MathHelper.DEGREES_TO_RADIANS);
				}
				
                data.SetQuaternion ("cam_dest_smooth", smooth);
				return BehaviorResult.Success;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
		}
		
		private static Keystone.Behavior.Actions.Action CreateVPShake ()
		{
			Func <Keystone.Entities.Entity , double, BehaviorResult> f;
        	
        	f = (target, elapsedSeconds) =>
        	{
                // TODO: maybe we can pass the Entity to the Func and have intrinsic Action.cs accept it instead of a BehaviorContext
                // but for actual Script Actions, we pass the EntityID.  We don't really seem to use context for anything
                // other than testing Initialized and Initialization should occur when assigning the viewpoint Entity when building the behavior tree
				KeyCommon.Data.UserData data = target.BlackboardData;
			
//				// TODO: if viewpoint script version of this and we delete EditorViewController
//				// then we can query Viewpoint.Context.Target or something and see if it's had an explosion
//				translation += Shake (elapsedSeconds);

				return BehaviorResult.Success;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
		}
		
		private static Keystone.Behavior.Actions.Action CreateVPSnapToBehavior()
		{
			Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (target, elapsedSeconds) =>
        	{
				KeyCommon.Data.UserData data = target.BlackboardData;
			
				
				Vector3d startingPosition = target.Translation;

				BehaviorResult result = BehaviorResult.Success;
				
				Quaternion startingRotation; // = data.GetQuaternion ("animation_flyto_starting_rotation");
				Quaternion destinationRotation; // = data.GetQuaternion ("animation_flyto_destination_rotation");

				string focusEntityID = data.GetString ("focus_entity_id");
				if (string.IsNullOrEmpty (focusEntityID))
				{
					result = BehaviorResult.Fail;
				}
				else
				{
					// we use either regional or global positions and we apply new translation as a DELTA which means we can
					// set it to the local translation just fine.
					
					double radius = AppMain.mScriptingHost.EntityAPI.GetRadius(focusEntityID);  // not local radius, but global taking into account any scaling of the root node
					// limit orbitRadius to min/max values
		            radius = Math.Max (radius, data.GetDouble ("orbit_radius_min"));
		            radius = Math.Min (radius, data.GetDouble ("orbit_radius_max"));
					//double radius = destinationTarget.BoundingBox.Radius;
					//data.GetDouble ("focus_entity_radius");
		
					
		            // distanceForFit repres   ents the distance away from target center we need to end up at
		            // it does NOT represent the distance we need to travel.  
		            float fovradians = data.GetFloat ("camera_fov");
		            double distanceForFit = radius / Math.Tan(fovradians / 2d);
						
		            // our positions are region relative and we apply regionOffset if necessary
		            Vector3d currentPosition = target.DerivedTranslation ;
		            
		            Vector3d destinationPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace(focusEntityID); // .SetDeltaTranslation, .SetPosition sets local translation, .GetLocalTranslation and SetLocalTranslation should be used?
		            //Vector3d destinationPosition = destinationTarget.DerivedTranslation;
					//data.GetVector ("focus_entity_translation"); // global vs regional
		
					// TODO: with stardigest, .GetEntityRegionID "focusEntityID" will fail since pointsprite star is not child entity of the original star's system or region
		            string destinationTargetRegionID = AppMain.mScriptingHost.EntityAPI.GetEntityRegionID (focusEntityID);

		            bool useGlobalCoordinates;
		            if (target.Region.ID == destinationTargetRegionID) 
		            {
		            	useGlobalCoordinates = false;
		            }
		            else
		            {
						currentPosition = target.GlobalTranslation ; 
		            	//destinationPosition = destinationTarget.GlobalTranslation;
		            	destinationPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace(focusEntityID);
		            	useGlobalCoordinates = true;
		            }
		
					Vector3d directionToTarget = destinationPosition - currentPosition;
		            if (directionToTarget == Vector3d.Zero())
		                directionToTarget.z = 1;
		
		            double distanceToTarget;
		            directionToTarget = Vector3d.Normalize (directionToTarget, out distanceToTarget);
		                            
		            // translations for keyframe 1 find the point in direction of -direcionToTarget from targetPosition because
		            // we want a point from target towards camera at radius == distanceForFit
		            destinationPosition += (-directionToTarget * distanceForFit);
		            
					Vector3d up = data.GetVector ("camera_up");		            
		            // rotations used for keyframe 0 and 1
		            Quaternion cameraStartingRotation = target.Rotation;   
		            destinationRotation =  Quaternion.CreateRotationTo(currentPosition, destinationPosition, up);
		#if DEBUG
		if (destinationRotation.IsNan()) //throw new Exception ();
			destinationRotation = new Quaternion ();
		#endif            
		           
		        	
					Vector3d deltaTranslation = startingPosition - destinationPosition;
					data.SetVector ("delta_translation", deltaTranslation);		      
	            	data.SetQuaternion ("cam_dest_rotation", destinationRotation); //"cam_dest_smooth");

					result = BehaviorResult.Success;
				}
										
				return result;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
		}
		
		private static Keystone.Behavior.Actions.Action CreateVPFlyToBehavior()
		{					
			Func <Keystone.Entities.Entity, double, BehaviorResult> f;
        	
        	f = (target, elapsedSeconds) =>
        	{
				KeyCommon.Data.UserData data = target.BlackboardData;
				
				
				Vector3d startingPosition; // = data.GetVector ("animation_flyto_starting_position");
				Vector3d destinationPosition; // = data.GetVector ("animation_flyto_destination_position");

				bool globalCoordinates; // = data.GetBool ("animation_flyto_use_global_coordinates");
				
				
				BehaviorResult result = BehaviorResult.Success;
				
				if (target.Animations == null || target.Animations.Contains ("flyto") == false)
				{
					data.SetBool ("animation_playing", false);
					
					// create's keyframe based animation, but does not start playing so we can pass dummy start/dest values here
					CreateFlyTo (target, Vector3d.Zero(), Vector3d.Zero(),
					             new Quaternion(), new Quaternion(), false);
				}


				if (target.Animations.Contains ("flyto")) 
				{
					if (target.Animations.CurrentAnimation == "flyto")
					{
						// Do nothing. We're already playing
						// REMEMBER, this is playing through AnimationController using keyframes
						result = BehaviorResult.Running;
					}
					else // "flyto" animation is not playing
					{
						// animation therefore must have completed last frame
						if (data.GetBool ("animation_playing")) 
						{
							// TODO: How can i trigger an event here when animation "flyto" has completed?
							// I can set a variable "flyto_completed" = AnimationState.Completed.
							// TODO: wait, why can't i detect that during the running of this state machine
							//       and then switch to whatever we want to switch to?  And in this example case
							//       I want to switch back to "control" == "user" and "behavior" == "orbit"
							// that we can then poll.  But isn't that happening below?  
							data.SetString ("control", "user");
  							data.SetBool ("chase_enabled", true);
							data.SetBool ("animation_playing", false);
							result = BehaviorResult.Success;
						}
						else // animation will be started with the computed start/dest position and rotation keyframe values
						{
							Quaternion startingRotation; // = data.GetQuaternion ("animation_flyto_starting_rotation");
							Quaternion destinationRotation; // = data.GetQuaternion ("animation_flyto_destination_rotation");
							// NOTE: here we do NOT update DURING flight.  If we did, NaNs occur in our endrotation
							//       as we near the initial final rotation.  We could do a check and
							//       disable "flyto_rotation" clip when "close enough" epsilon reached 
							string focusEntityID = data.GetString ("focus_entity_id");
							if (string.IsNullOrEmpty (focusEntityID))
							{
								result = BehaviorResult.Fail;
							}
							else
							{
								ComputeFlyToArguments (target, data, out startingPosition, out destinationPosition, out startingRotation, out destinationRotation, out globalCoordinates);
								
								UpdateFlyTo (target, startingPosition, destinationPosition,
					       					 startingRotation, destinationRotation, globalCoordinates);		
								
								int count = (int)(destinationPosition.Length / AppMain.REGION_DIAMETER);
								//System.Diagnostics.Debug.Assert (Math.Abs (count) < 20);
								
								data.SetBool ("animation_playing", true);
								target.Animations.Play("flyto");
								result = BehaviorResult.Running;
							}
						}
					}
				}
				return result;
        	};
        	
        	string id = Keystone.Resource.Repository.GetNewName (typeof (Keystone.Behavior.Actions.Action));
        	return new Keystone.Behavior.Actions.Action (id, f);
		}
		

		private static void ComputeFlyToArguments (Keystone.Entities.Entity animationTarget, KeyCommon.Data.UserData data, 
		                                   out Vector3d startPosition, out Vector3d endPosition, out Quaternion startRotation, out Quaternion endRotation,
		                                   out bool useGlobalCoordinates)
		{
			
			// TODO: what if the destinationTargetID is for an entity that is out of scope?  Destroyed? Paged out? Exists only in proxy form in star digest? etc?
			
			// TODO: how do we chase a moving target considering that we may not have access to Entity?
            //       we might need to IEntityAPI.GetTranslation (destinationTargetID);
            // TODO: verify our radius check works for entities that use inherited scales of parents
            //        - TODO: boundingBox must have first been rescaled when mRoot was rescaled. 

			Vector3d up = data.GetVector ("camera_up");
        		
                      
			// TODO: in addition to "MoveTo" toolbar icon, "Chase" that will chase and orbit selected vehicle (later we can add view offsets and rotation constraints, perhaps locked constraint but with rotation dampening so it matches rotation with some lag)
			// TODO: if focus target is already set by chase, then we can just use a focus_coordinate
			// TODO: fov can always be updated to the viewpoint.CustomData whenever it is changed
			// TODO: orbit target/chase target/flyto target, 
			// TODO: if "chase" already gives us relative delta to target, do we need it here?
			string focusEntityID = data.GetString ("focus_entity_id");
			// we use either regional or global positions and we apply new translation as a DELTA which means we can
			// set it to the local translation just fine.
			
			double radius = AppMain.mScriptingHost.EntityAPI.GetRadius(focusEntityID);  // not local radius, but global taking into account any scaling of the root node
			// limit orbitRadius to min/max values
            radius = Math.Max (radius, data.GetDouble ("orbit_radius_min"));
            radius = Math.Min (radius, data.GetDouble ("orbit_radius_max"));
			//double radius = destinationTarget.BoundingBox.Radius;
			//data.GetDouble ("focus_entity_radius");

			
            // distanceForFit represents the distance away from target center we need to end up at
            // it does NOT represent the distance we need to travel.  
            float fovradians = data.GetFloat ("camera_fov");
            double distanceForFit = radius / Math.Tan(fovradians / 2d);
				
            // our positions are region relative and we apply regionOffset if necessary
            Vector3d currentPosition = animationTarget.DerivedTranslation ;
            
            Vector3d destinationPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace(focusEntityID); // .SetDeltaTranslation, .SetPosition sets local translation, .GetLocalTranslation and SetLocalTranslation should be used?
            //Vector3d destinationPosition = destinationTarget.DerivedTranslation;
			//data.GetVector ("focus_entity_translation"); // global vs regional

			// TODO: with stardigest, .GetEntityRegionID "focusEntityID" will fail since pointsprite star is not child entity of the original star's system or region
            string destinationTargetRegionID = AppMain.mScriptingHost.EntityAPI.GetEntityRegionID (focusEntityID);
            //if (animationTarget.Region == destinationTarget.Region)
            if (animationTarget.Region.ID == destinationTargetRegionID) 
            {
            	useGlobalCoordinates = false;
            }
            else
            {
				currentPosition = animationTarget.GlobalTranslation ; 
            	//destinationPosition = destinationTarget.GlobalTranslation;
            	destinationPosition = AppMain.mScriptingHost.EntityAPI.GetPositionGlobalSpace(focusEntityID);
            	useGlobalCoordinates = true;
            }

			Vector3d directionToTarget = destinationPosition - currentPosition;
            if (directionToTarget == Vector3d.Zero())
                directionToTarget.z = 1;

            double distanceToTarget;
            directionToTarget = Vector3d.Normalize (directionToTarget, out distanceToTarget);
                            
            // translations for keyframe 1 find the point in direction of -direcionToTarget from targetPosition because
            // we want a point from target towards camera at radius == distanceForFit
            destinationPosition += (-directionToTarget * distanceForFit);
                         
            // rotations used for keyframe 0 and 1
            Quaternion cameraStartingRotation = animationTarget.Rotation;   
            Quaternion destinationRotation =  Quaternion.CreateRotationTo(currentPosition, destinationPosition, up);
#if DEBUG
if (destinationRotation.IsNan()) //throw new Exception ();
	destinationRotation = new Quaternion ();
#endif            
            // TODO: viewpoint needs to know camera Up and needs to know camera FOV to find fitdistance from target radius 
//        	animationTarget.CustomData.SetBool ("animation_flyto_use_global_coordinates", useGlobalCoordinates);
//        	animationTarget.CustomData.SetVector ("animation_flyto_starting_position", currentPosition);
//        	animationTarget.CustomData.SetVector ("animation_flyto_destination_position", destinationPosition);
        	animationTarget.BlackboardData.SetQuaternion ("animation_flyto_starting_rotation", cameraStartingRotation);
        	animationTarget.BlackboardData.SetQuaternion ("animation_flyto_destination_rotation", destinationRotation);
        	
        	startPosition = currentPosition;
        	endPosition = destinationPosition ;
        	startRotation = cameraStartingRotation;
        	endRotation = destinationRotation ;
		}
		
    	private static void CreateFlyTo (Keystone.Entities.Entity animationTarget, 
    	                    Vector3d startingPosition, Vector3d destinationPosition, 
    	                    Quaternion startingRotation, Quaternion destinationRotation, 
    	                    bool useGlobalCoordinates)
    	{

        	string id = Repository.GetNewName(typeof(Keystone.Animation.Animation));

            Animation anim = new Keystone.Animation.Animation(id);
            anim.Looping = false;
            string animation1_name = "flyto"; //Repository.GetNewName(typeof(Animation.KeyframeInterpolator<Vector3d>));
            anim.Name = animation1_name;
			
            // translation animation clip
        	id = Repository.GetNewName(typeof(KeyframeInterpolator<Vector3d>));
        		KeyframeInterpolator<Vector3d> translationClip =  
        		 new Keystone.Animation.KeyframeInterpolator<Vector3d>(id, "translation");
        	string translationClip_Name = "flyto_translation";
            translationClip.Name = translationClip_Name;
            translationClip.TargetName = animationTarget.Name; //null; // note uses friendly name which requires node be a descendant of host enity (eg viewpoint's descendant)
            translationClip.Duration = 2.25f; //seconds // TODO: what if we scaled this by distance?  farther we are the more time we allow?
         
            // add the keyframes in reverse order
            translationClip.AddKeyFrame(startingPosition);
            translationClip.AddKeyFrame(destinationPosition); 
           // translation key frames should be interpreted as global coord values
            translationClip.UseGlobalCoordinates = useGlobalCoordinates;
            
            anim.AddChild(translationClip);

    		// rotation animation clip		        
            id = Repository.GetNewName(typeof(KeyframeInterpolator<Quaternion>));
        		KeyframeInterpolator<Quaternion> rotationClip =  
        		 new Keystone.Animation.KeyframeInterpolator<Quaternion>(id, "rotation");
        	string rotationClip_name = "flyto_rotation";
            rotationClip.Name = rotationClip_name;
            rotationClip.TargetName = animationTarget.Name; // note uses friendly name which requires node be a descendant of host enity (eg viewpoint's descendant) 
            rotationClip.Duration = 2.25f; //seconds // TODO: what if we scaled this by distance?  the farther camera is from target the more time we allow?
                                        // NOTE: clips can have different durations within the same Animation.
            
            // add the keyframes
            rotationClip.AddKeyFrame (startingRotation);
            rotationClip.AddKeyFrame (destinationRotation);
            anim.AddChild(rotationClip);
                 
			// TODO: this animation is never removed from the animationTarget?            
			// add 
        	animationTarget.AddChild (anim);
    	}
	
    		   
		private static void UpdateFlyTo (Keystone.Entities.Entity animationTarget, 
    	                    Vector3d startingPosition, Vector3d destinationPosition, 
    	                    Quaternion startingRotation, Quaternion destinationRotation, 
    	                    bool useGlobalCoordinates)
    	{

            Predicate<Keystone.Elements.Node> match = e =>
            {
                return
                    e as Keystone.Animation.AnimationClip != null &&
                    e.Name == "flyto_translation";
            };
			                
			// get the translation clip
			KeyframeInterpolator<Vector3d> translationClip =  
				(Keystone.Animation.KeyframeInterpolator<Vector3d>)animationTarget.Query (true, match)[0];
			
			// must update whether the new frames use global
			translationClip.UseGlobalCoordinates = useGlobalCoordinates;
			
			// remove existing keyframes and add new. 
			translationClip.RemoveAllKeyFrames();
			translationClip.AddKeyFrame(startingPosition);
            translationClip.AddKeyFrame(destinationPosition); 
            
			// get the rotation clip
			match = e =>
            {
                return
                    e as Keystone.Animation.AnimationClip != null &&
                    e.Name == "flyto_rotation";
            };
			
			KeyframeInterpolator<Quaternion> rotationClip =  
				(Keystone.Animation.KeyframeInterpolator<Quaternion>)animationTarget.Query (true, match)[0];
			
			// remove existing keyframes and addnew
			// note: if our quicklook is flyto animating same time our main is, there is no conflict because
			//       they are actually using seperate (cloned) viewpoint entity instances. So the animation
			//       attached to them are unique.
			rotationClip.RemoveAllKeyFrames();
			rotationClip.AddKeyFrame (startingRotation);
            rotationClip.AddKeyFrame (destinationRotation);
    	}
    	
//        #region Shaking Camera - Camera Modififer - along with "Smoothing" I think these camera fx should be classes that we attach so that adding new ones is easy
//        // http://xnaessentials.com/archive/2011/04/26/shake-that-camera.aspx
//        // http://gamedev.stackexchange.com/questions/1828/realistic-camera-screen-shake-from-explosion
//        
//        // We only need one Random object no matter how many Cameras we have
//		private static readonly Random mRandomShakeVal = new Random();
//		
//		// Are we shaking?
//		private bool mShaking = true;
//		
//		// The maximum magnitude of our shake offset
//		private double mShakeMaxMagnitude = 0.1d;
//		
//		// The total duration of the current shake
//		private double mShakeDuration = 0;
//		
//		// A timer that determines how far into our shake we are
//		private double mShakeTimer;
//		
//		// The shake offset vector
//		private Vector3d mShakeOffset;
//
//        /// <summary>
//		/// Shakes the camera with a specific magnitude and duration.
//		/// </summary>
//		/// <param name="magnitude">The largest magnitude to apply to the shake.</param>
//		/// <param name="duration">The length of time (in seconds) for which the shake should occur.</param>
//		public void Shake(float magnitude, float duration)
//		{
//			if (duration < 0d) throw new ArgumentOutOfRangeException ();
//			if (magnitude < 0d) throw new ArgumentOutOfRangeException();
//			
//		    // We're now shaking
//		    mShaking = true;
//		
//		    // Store our magnitude and duration
//		    mShakeMaxMagnitude = magnitude;
//		    mShakeDuration = duration;
//		    
//		    // Reset our timer
//		    mShakeTimer = 0d;
//		}
//
//		/// <summary>
//		/// Helper to generate a random double in the range of [-1, 1].
//		/// </summary>
//		private double NextShakeValue()
//		{
//		    return mRandomShakeVal.NextDouble() * 2d - 1d;
//		}

//		private Vector3d Shake (double elapsedSeconds)
//        {
//        	// Move our timer ahead based on the elapsed time
//		   	mShakeTimer += elapsedSeconds;
//			
//		    // If we're at the max duration, we're not going to be shaking anymore
//		    if (mShakeTimer >= mShakeDuration)
//		    {
//		        mShaking = false;
//		    	mShakeTimer = mShakeDuration;
//		    	return Vector3d.Zero();
//		   	}
//			else 
//			{
//			   // Compute our progress in a [0, 1] range
//			   double progress = mShakeTimer / mShakeDuration;
//					 
//			   // Compute our magnitude based on our maximum value and our progress. This causes
//			   // the shake to reduce in magnitude as time moves on, giving us a smooth transition
//			   // back to being stationary. We use progress * progress to have a non-linear fall 
//			   // off of our magnitude. We could switch that with just progress if we want a linear 
//			   // fall off.
//			   double magnitude = mShakeMaxMagnitude * (1d - (progress * progress));
//			 
//			   // Generate a new offset vector with three random values and our magnitude
//			   mShakeOffset = new Vector3d( NextShakeValue(), NextShakeValue(), NextShakeValue()) * magnitude;
//			
//			   // If we're shaking, add our offset to our position and target
//			   return mShakeOffset;
//			  // lookat += mShakeOffset; // TODO: i dont think we need to modify lookAt here in our implementation because we work directly with a Rotation and not a LookAT.
//			  //                         // and to the extent that we use LookAt, it is to compute the Rotation
//			}	
//        }
//		#endregion  
    			
    	internal static void ProcessInputs(Keystone.Entities.Entity entity, KeyCommon.Data.UserData data)
		{
    		
			Keystone.Entities.Entity.EntityInput[]  inputs = entity.DeQueueAll ();
        	if (inputs == null) return;
        	
            // var to collect total mouse x,y deltas for this frame.  Remember that
            // our input system can accept multiple buffered input from mouse device per frame
        	System.Drawing.Point totalMouseDeltas = new System.Drawing.Point (0,0);

        	
        	// TODO: can we create "outputs" here that our viewpoint would know how to interpret?
        	//       its either that or we intercept "select", "delete", "cancel", "use", "bind", and still create events for "mouseover"
        	for (int i = 0; i < inputs.Length; i++)
        	{
        		Keystone.Entities.Entity.EntityInput input = inputs[i];
        		
        		switch (input.Name)
        		{	        				
        			case "mouselook":
    				{
    					data.SetBool("mouselook", input.Enabled);
    					break;
        			}
        			case "mousemove":
    				{	
		        		System.Drawing.Point mouseDelta = (System.Drawing.Point)input.Value;
        				// update our state vars tracking the angles if mouselook enabled
        				if (data.GetBool ("mouselook"))
        				{
            				totalMouseDeltas.X += mouseDelta.X;
            				totalMouseDeltas.Y += mouseDelta.Y;
        				}

        				// our Update() we will do the actual Panning() update
        				// TODO: this is bad place to have because we can get multiple "mousemove" changes in the stack per Update()? or is it ok?
        				// well, it doesnt make sense to run this multiple times in A SINGLE UPDATE... its good for movement but not picking surely?
        				//mContext.Workspace.ToolMouseMove(entity, mScreenPosition);
        				break;
        			}
        			case "forward":
    				{
    					Vector3d dir = data.GetVector("cam_move_dir"); // movement direction
        				if (input.Enabled)
	        				dir.z = 1;
        				else dir.z = 0;

        				data.SetVector ("cam_move_dir", dir);
        				break;
    				}
        			case "backward":
    				{
        				Vector3d dir = data.GetVector("cam_move_dir"); // movement direction
        				if (input.Enabled)
	        				dir.z = -1;
        				else dir.z = 0;

        				data.SetVector ("cam_move_dir", dir);
        				break;
        			}
        			case "panup":
    				{
        				Vector3d dir = data.GetVector("cam_move_dir"); // movement direction
        				if (input.Enabled)
	        				dir.y = 1;
        				else dir.y = 0;

        				data.SetVector ("cam_move_dir", dir);
        				break;
    				}
        			case "pandown":
    				{
        				Vector3d dir = data.GetVector("cam_move_dir"); // movement direction
        				if (input.Enabled)
	        				dir.y = -1;
        				else dir.y = 0;

        				data.SetVector ("cam_move_dir", dir);
        				break;
        			}
        			case "panright":
    				{
        				Vector3d dir = data.GetVector("cam_move_dir"); // movement direction
        				if (input.Enabled)
	        				dir.x = 1;
        				else dir.x = 0;

        				data.SetVector ("cam_move_dir", dir);
        				break;
    				}
        			case "panleft":
    				{
        				Vector3d dir = data.GetVector("cam_move_dir"); // movement direction
        				if (input.Enabled)
	        				dir.x = -1;
        				else dir.x = 0;

        				data.SetVector ("cam_move_dir", dir);
        				break;
        			}
        			case "mousepan":
    				{		
//	        					know.SetBool("mousepan", input.Enabled);
//	        					
//	        					if (input.Enabled)
//	        					{	
        						// TODO: here we could enforce that mScreenPosition is set as var?
//	        						int x, y;
        						// TODO: these mousepos2D coords we should just store along with mousepos3D
//	            					mContext.Viewport.ScreenToViewport(mScreenPosition.X, mScreenPosition.Y, out x, out y);
//	            					System.Drawing.Point mouseStart = new System.Drawing.Point (x, y);
//	        						know.SetPoint("mousepanstart", mouseStart);
//	        						know.SetPoint("mousepanend", mouseStart);
//	        					}
        					break;
    				}
    				case "zoomout":
        			case "zoomin":
    				{
                        bool smooth = data.GetBool("smooth_zoom_enabled");
                        double value = data.GetDouble ("orbit_radius");
                        if (smooth)
                               value = data.GetDouble("orbit_radius_destination");

                        // NOTE: for Zoom Out input.Value will be negative so we still += not -=
                        if ((int)input.Value > 0)
                            value += value * 2 * (int)input.Value;
                        else
                            value += value / 2 * (int)input.Value;

    					double limit = value;
    					limit = Math.Max (value, data.GetDouble ("orbit_radius_min"));
    					limit = Math.Min (limit, data.GetDouble ("orbit_radius_max"));

                        
                        if (smooth)
                            data.SetDouble("orbit_radius_destination", limit);
                        else
    					    data.SetDouble ("orbit_radius", limit); 

                            
                         data.SetDouble("smooth_zoom_elapsed", 0d);
                         break;
        			}
        			default:
        				System.Diagnostics.Debug.WriteLine ("EditorWorkspace.ViewpointBehaviors.ProcessInputs() - Unsupported Axis " + input.Name );
        				break;
        		}
        	}

        	
			data.SetPoint ("mouse_deltas", totalMouseDeltas); // in future may include roll (angle.z) so use Vector not Point					            				
    	}
	}
}
