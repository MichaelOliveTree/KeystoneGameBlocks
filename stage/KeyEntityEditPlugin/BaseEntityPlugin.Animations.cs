using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KeyPlugins;
using Keystone.Types;
using SourceGrid;

namespace KeyPlugins
{
    partial class BaseEntityPlugin
    {
    	private const int ANIM_NUM_COLUMNS = 5;
        private const int ANIM_ROW_HEADER = 0;

        private const int ANIM_COLUMN_START_FRAME = 0;
        private const int ANIM_COLUMN_END_FRAME = 1;
        private const int ANIM_COLUMN_SPEED = 2;
        private const int ANIM_COLUMN_REVERSE = 3;  // or would you just set the end frame as start frame and start as end?
        private const int ANIM_COLUMN_LENGTH = 4;

        private Timer mMediaTimer;

        private FloatPropertyController mEndFrameValueController;
        private FloatPropertyController mStartFrameValueController;
        private VectorPropertyController mStartVectorValueController;
        private VectorPropertyController mEndVectorValueController;
        private FloatPropertyController mSpeedValueController;
        private FloatPropertyController mDurationValueController;

        // https://github.com/siemens/sourcegrid  // <- i think this is a forked version.  i dont know if the original author's updates are out there anymore
        // NOTE: [TypeConverter(typeof(Keystone.TypeConverters.ColorConverter))] or Vector3fConverter or Vector3dConverter, etc needs to be added as attribute to the struct or class as well as a TypeConverter implementation
        SourceGrid.Cells.Editors.TextBox mEditorColor;
        SourceGrid.Cells.Editors.TextBox mEditorVector;
        SourceGrid.Cells.Editors.TextBox mEditorVector3f;
        SourceGrid.Cells.Editors.TextBox mEditorString; // editor is required along with a controller
        SourceGrid.Cells.Editors.TextBox mEditorFloat;
        
        SourceGrid.Cells.Editors.NumericUpDown mEditorNumericFloat;
        
        //SourceGrid.Cells.Editors.

        bool mAnimationGridInitialized = false;

        

        private void ClearAnimationsPanel()
        {
            if (mAnimationGridInitialized == false)
                InitializeAnimationGrid();

            // clear the animation combobox and grid
            PopulateAnimationCombobBox(mTargetNodeID);
            ClearAnimationGrid();

        }

        void ButtonNewAnimationClick(object sender, EventArgs e)
		{
			// spawn "enter name" dialog box (checking for duplicates)
			
			// create new Animation node
			// add Animation to combobox
			// set new Animation as current Animation
			// clear animation grid
			
			// TODO: implement "Add" of new keyframe interpolators nodes to this 
			// or any existing Animation.
			string resultText = null;
			
			DialogResult result = KeyPlugins.StaticControls.InputBox ("Create New Animation", "Enter animation name.", ref resultText);                
            if (result != DialogResult.OK) return;
			                            
			// TODO: check for illegal names
			// null/empty name
			// illegal characters
			
			
			// check for duplicate names under the same entity
			foreach (string value in mComboBoxData.Values)
			{
				if (resultText.ToLower() == value.ToLower())
				{
					MessageBox.Show ("Animation names must be unique for each Entity.", "Duplicate animation name not allowed!", MessageBoxButtons.OK);
					return;
				}
			}
			
			// create the new Animation node.
			Settings.PropertySpec[] properties  = new Settings.PropertySpec[1];
			properties[0] = new Settings.PropertySpec ("name", typeof(string));
			properties[0].DefaultValue = resultText;
			mHost.Node_Create ("Animation", mTargetNodeID, properties);
		}
		
		void ButtonDeleteAnimationClick(object sender, EventArgs e)
		{
			if (cboAnimations.SelectedIndex == -1) return;
			
			string animationID = ((KeyValuePair<string, string>)cboAnimations.ComboBox.SelectedItem).Key;
			if (string.IsNullOrEmpty (animationID)) throw new Exception ();

			// do not allow deletion of intrinsic BonedAnimation which should always be at index == 0 if it exists.

			
			// TODO: implement "Delete" of existing keyframe interpolators nodes 
			// from the currently selected animation or does this happen for us
			// TODO: prevent delete of intrinsic boned animations
			
			mHost.Node_Remove (animationID, mTargetNodeID);
			// TODO: In Animations_Node_Removed() update cboAnimation mComboboxData binding
			// TODO: if one or more animations remain in combobox set first as current
			// TODO: update keyframes grid
		}
		
		Dictionary<string, string> mComboBoxData;
        private void PopulateAnimationCombobBox (string entityID)
        {
            
        	cboAnimations.ComboBox.DataSource = null;
        	cboAnimations.Items.Clear();
            cboAnimations.Text = "";

            // init combobox
            mComboBoxData = new Dictionary<string, string>();
            cboAnimations.ComboBox.ValueMember = "Key";
            cboAnimations.ComboBox.DisplayMember = "Value";
        	
            string[] childIDs;
            string[] childTypes;
            mHost.Node_GetChildrenInfo(entityID, null, out childIDs, out childTypes);

            if (childIDs == null || childIDs.Length == 0) return;
            
            for (int i = 0; i < childIDs.Length; i++)
            {
                string nodeType = childTypes[i];
                string nodeID = childIDs[i];
                // skip child entities
                switch (nodeType)
                {
                    case "Animation":
                		
                        string name = (string)mHost.Node_GetProperty (nodeID, "name");
                        mComboBoxData.Add (nodeID, name);
                        
                        break;
                }
            }
            
            cboAnimations.ComboBox.DataSource = new BindingSource (mComboBoxData, null);
            cboAnimations.ComboBox.SelectedIndex = 0;
        }
        
        private void cboAnimations_SelectedIndexChanged (object sender, EventArgs e)
        {
        	ClearAnimationGrid();
        	PopulateAnimationTree(mTargetNodeID);
        }
        
        private void PopulateAnimationTree(string entityID)
        {
        	
			// clear and rebuild treeview
            treeViewAnimations.Nodes.Clear();

            TreeNode entity = treeViewAnimations.Nodes.Add (entityID, "Entity");
            TreeNode transform = entity.Nodes.Add (entityID, "Transform");
            transform.Nodes.Add (entityID, "Position");
            transform.Nodes.Add (entityID, "Scale");
            transform.Nodes.Add (entityID, "Rotation");

            PopulateAnimationTreeChildren (entity, entityID);
            
            treeViewAnimations.ExpandAll();
            entity.EnsureVisible();
            
        }
        
        private void PopulateAnimationTreeChildren (TreeNode parentNode, string parentNodeID)
        {
        	string[] childIDs;
            string[] childTypes;

            mHost.Node_GetChildrenInfo(parentNodeID, null, out childIDs, out childTypes);
            
            if (childIDs != null)
	            for (int i = 0; i < childIDs.Length; i++)
    	        {
    	        	string nodeType = childTypes[i];
    	        	string nodeID = childIDs[i];
    	        	switch (nodeType)
    	        	{
                        case "ModelSelector": // TODO: Oct.3.2019 - just added this case to fix an issue with our Door Component.  The door component is using ModelSelector and not ModelSequence, why?  
                            // Somehow when i added the ModelSequence to the Door entity prefab, it is using ModelSelector and not ModelSequence for each half of the door.  It's rendering fine as a ModelSequence so why is it nodeType here a ModelSelectoor?
                            TreeNode selector = parentNode.Nodes.Add(nodeID, "ModelSelector");
                            // recurse
                            PopulateAnimationTreeChildren(selector, nodeID);
                            break;
                        case "ModelSequence":
    	        			TreeNode sequence = parentNode.Nodes.Add (nodeID, "ModelSequence");
    	        			// recurse
    	        			PopulateAnimationTreeChildren (sequence, nodeID);
    	        			break;
    	        		case "Model":
    	        			TreeNode model = parentNode.Nodes.Add (nodeID, "Model");
    	        			if (ModelHasBonedActor(nodeID))
	    	        			model.Nodes.Add (nodeID, "Skeletal");
    
    	        			TreeNode transform = model.Nodes.Add (parentNodeID, "Transform");
				            transform.Nodes.Add (nodeID, "Position");
				            transform.Nodes.Add (nodeID, "Scale");
				            transform.Nodes.Add (nodeID, "Rotation");
    	        			
				            // get the Appearance 
				            string[] modelChildIDs;
            				string[] modelChildTypes;
				            mHost.Node_GetChildrenInfo(nodeID, null, out modelChildIDs, out modelChildTypes);
				            
				            if (modelChildIDs != null)
					            for (int j = 0; j < modelChildIDs.Length; j++)
				            	{
					            	switch (modelChildTypes[j])
					            	{
					            		case ("DefaultAppearance"):
					            			TreeNode material = model.Nodes.Add ("Material");
							            	material.Nodes.Add (modelChildIDs[j], "Diffuse");
							            	material.Nodes.Add (modelChildIDs[j], "Ambient");
							            	material.Nodes.Add (modelChildIDs[j], "Specular");
							            	material.Nodes.Add (modelChildIDs[j], "Emissive");

							            	break;
					            	}
				            	}
				            break;
    	        	}
    	        }
        }
        
        private bool ModelHasBonedActor (string modelID)
        {
        	// get children and return TRUE if one of them is Actor3d
        	string[] childIDs;
            string[] childTypes;

            // get models
            mHost.Node_GetChildrenInfo(modelID, null, out childIDs, out childTypes);
            
            if (childIDs == null) return false;
            
            for (int i = 0; i < childIDs.Length; i++)
            	if (childTypes[i] == "Actor3d") return true;
            		
        	return false;
        }
        
        void TreeViewAnimationsNodeMouseClick (object sender, TreeNodeMouseClickEventArgs  e)
        {
        	if (sender as TreeView == null) return;
        	
        	TreeView tree = (TreeView)sender;
        	
        	switch (e.Button)
        	{
        		case MouseButtons.Left:
        			break;
        		case MouseButtons.Right:
        			// right mouse click context menu for adding
        			// new animation clips
        			tree.SelectedNode = e.Node;
        			if (tree.SelectedNode.Parent == null || tree.SelectedNode.Text == "Entity") return;
                    string animationNodeID = null;
                    string selectedNodeID = null;
                    string selectedNodeType = null;
                    try
                    {
                        // NOTE: if there are no Animations added to the Entity, the combobox will be blank and the KeyValuePair grab attempt
                        //       will throw an exception.
                        animationNodeID = ((KeyValuePair<string, string>)cboAnimations.ComboBox.SelectedItem).Key;
                        selectedNodeID = tree.SelectedNode.Name;
                        selectedNodeType = tree.SelectedNode.Text;
                    }
                    catch (Exception ex)
                    {
                        return;
                    }
        			// todo: if Clip already exists under a specific node, do we allow it?
        			// - i think no.  One clip per node type.
        			
            		switch (selectedNodeType)
        			{
        				case "Position":
            				ShowContextMenu (e.Node, animationNodeID, e.X, e.Y);
        					break;
        				case "Scale":
        					ShowContextMenu (e.Node, animationNodeID, e.X, e.Y);
        					break;
        				case "Rotation":
        					ShowContextMenu (e.Node, animationNodeID, e.X, e.Y);
        					break;
        				case "Skeletal":
        					ShowContextMenu (e.Node, animationNodeID, e.X, e.Y);
        					break;
        				case "Diffuse":
                            // TODO: where  is the code for a context menu for Materials?
        					break;
        				case "Ambient":
        					break;
        				case "Specular":
        					break;
        				case "Emissive":
        					break;
        				case "Texture":
        					break; 
        			}
        			break;
        	}
        }
        
        private void ShowContextMenu(TreeNode node, string animationNodeID, int x, int y)
        {
			ContextMenuStrip contextMenu = new ContextMenuStrip();
			ToolStripMenuItem menuItem = new ToolStripMenuItem("Add Clip");
            menuItem.Name = animationNodeID;
            menuItem.Tag = node;
            menuItem.Click += ContextMenu_New_Clip_Click;
            contextMenu.Items.Add(menuItem);

    		//ToolStripSeparator seperator = new ToolStripSeparator();
    		//contextMenu.Items.Add(seperator);
    		
    		contextMenu.Show (treeViewAnimations, x, y);
        }
        
        private void ContextMenu_New_Clip_Click (object sender, EventArgs e)
        {
        	if (sender as ToolStripMenuItem == null) return;
        	
        	ToolStripMenuItem item = sender as ToolStripMenuItem;
        	
        	switch (item.Text )
        	{
        		case "Add Clip":
        			TreeNode node = (TreeNode) item.Tag;
        			string animationNodeID = item.Name;
        			
        			Settings.PropertySpec[] properties = new Settings.PropertySpec[2];
        			// targetname and targetid properties
        			string targetName;
        			string targetID;
        			GetTargetNameAndID (node, out targetName, out targetID);
        			properties[0] = new Settings.PropertySpec ("target", typeof (string));
        			properties[0].DefaultValue = targetName ;
        			properties[1] = new Settings.PropertySpec ("targetid", typeof (string));
        			properties[1].DefaultValue = targetID;
        			
        			switch (node.Text)
        			{
        				case "Position":
        					// NOTE: minimum default number of keyframes are already added in Repository.Create() for this node type
        					mHost.Node_Create ("KeyframeInterpolator_translation", animationNodeID, properties);
        					break;
        				case "Scale":
        					// NOTE: minimum default number of keyframes are already added in Repository.Create() for this node type
        					mHost.Node_Create ("KeyframeInterpolator_scale", animationNodeID, properties);
        					break;
        				case "Rotation":
        					// NOTE: minimum default number of keyframes are already added in Repository.Create() for this node type
        					mHost.Node_Create ("KeyframeInterpolator_rotation", animationNodeID, properties);
        					break;
        				case "Skeletal":
                            string animationName = (string)mHost.Node_GetProperty(animationNodeID, "name");

                            properties = new Settings.PropertySpec[3];
                            properties[0] = new Settings.PropertySpec("target", typeof(string));
                            properties[0].DefaultValue = targetName;
                            properties[1] = new Settings.PropertySpec("targetid", typeof(string));
                            properties[1].DefaultValue = targetID;
                            // NOTE: here we use the friendly name of the Animation.cs instance for the
                            //       BonedAnimation node.  Duplicate actors will all share one copy of the Animation
                            //       so adding AddAnimationRange() to one actor will add them to all the duplicates
                            //       and using friendly name instead of GUID helps us to recycle those animations internally.
                            //       BonedAnimation.cs itself is derived from AnimationClip and currently cannot be shared as a Node unfortunately
                            //       until we can update AnimationClip to not use per copy state variables.
                            properties[2] = new Settings.PropertySpec("name", typeof(string));
                            properties[2].DefaultValue = animationName;

                            mHost.Node_Create ("BonedAnimation", animationNodeID, properties);
        					break;
        			}

        			break;
        	}
        	
        }
        
        private void GetTargetNameAndID (TreeNode node, out string targetName, out string targetID)
        {
        	switch (node.Text)
        	{
                // TODO: aren't i missing Material animations here for ambient, diffuse, specular and emissive?
        		case "Position":
        		case "Scale":
        		case "Rotation":
        			// parent node is "Transform" so target will be parent of parent
                    // TODO: if a modelselector/modelsequence has two or more Models with same default typename as "name" then
                    //       the targetID of "name" will be "Model" and result in all animations added to the second in the sequence/selector
                    //       to be applied to the first Model in the selector/sequence.  This means we MUST rename the Models friendly "name"
                    //       under the selector/sequence.  WAIT - im not sure that is actually true.
        			targetID = node.Parent.Parent.Name;
        			targetName = (string)mHost.Node_GetProperty (targetID, "name");
        			break;
        		case "Skeletal":
        			targetID = node.Parent.Name;
        			targetName = (string)mHost.Node_GetProperty (targetID, "name");
        			break;
        		default:
        			throw new NotImplementedException ();
        	}
        }

        /// <summary>
        /// Called when a new animation clip (eg keyframe node) is created on the server and 
        /// added to the client scene.
        /// </summary>
        /// <param name="parentNodeID">The Animation ID</param>
        /// <param name="childNodeID">The keyframe node ID</param>
        /// <param name="childTypeName">The keyframe node type</param>
        private void Animation_Node_Created(string parentNodeID, string childNodeID, string childTypeName)
        {
        	
        	switch (childTypeName)
        	{
        		case "Animation":
        			string friendlyName = (string)mHost.Node_GetProperty (childNodeID, "name");
        			mComboBoxData.Add (childNodeID, friendlyName);
        			cboAnimations.ComboBox.DataSource = new BindingSource (mComboBoxData, null);
        			cboAnimations.SelectedIndex = GetAnimationIndex(childNodeID);
        			
        			// TODO: refresh keyframe grid
        			
       				break;
        	}
        }
        
        private int GetAnimationIndex(string id)
        {
        	if (mComboBoxData == null || mComboBoxData.Count == 0) throw new Exception("BaseEntityPlug.Animations.GetAnimationIndex() - Invalid animation id.");
        	
        	int index  = 0;
        	
        	foreach (string key in mComboBoxData.Keys)
        	{
        		if (key == id) return index;
        		index++;
        	}
        	
        	throw new Exception ("BaseEntityPlug.Animations.GetAnimationIndex() - Animation id not found.");
        	
        }
        
		void TreeViewAnimationsAfterSelect(object sender, TreeViewEventArgs e)
		{
			ClearAnimationGrid();
			
			if (sender is TreeView)
			{
				// populate grid with any keyframes from currently selected 
				// Animation that are associated with this node type
				TreeNode selected = ((TreeView)sender).SelectedNode;
				TreeNode entity = selected.TreeView.Nodes[0];
                if (cboAnimations.ComboBox.SelectedItem == null) return;

                try
                {
                    if (cboAnimations.Items.Count == 0) return;
                    KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)cboAnimations.ComboBox.SelectedItem;
                }
                catch (Exception ex)
                {
                    return;
                }
				string currentAnimationID = ((KeyValuePair<string, string>)cboAnimations.ComboBox.SelectedItem).Key;
				
				switch (selected.Text)
				{
					case "Position":
					case "Scale":
					case "Rotation":
					case "Skeletal":
						
						string[] clipIDs;
						string[] clipTypes;
						
                        // we only want to show the clips that have target == modelID
						mHost.Node_GetChildrenInfo (currentAnimationID, null, out clipIDs, out clipTypes);
						if (clipIDs == null) return;
                        
						for (int i = 0; i < clipIDs.Length; i++)
						{
                            if (selected.Text != "Skeletal")
                            {
                                string nodeID = selected.Name;
                                string parentID = selected.Parent.Parent.Name; // this is actually either an entityID or modelID
                                string modelName = (string)mHost.Node_GetProperty(parentID, "name"); // this is actually an Entity name or Model name.
                                string targetName = (string)mHost.Node_GetProperty(clipIDs[i], "target");
                                // TODO: if we change the model's "name" after adding a clip, this breaks. we need to then delete the animation and recreate them along with the clips
                                // TODO: currently, there's no way to delete clips individually.... you have to delete the entire animation.
                                if (modelName != targetName) continue;
                            }
                            switch (selected.Text)
							{
								case "Position":
									if (clipTypes[i] != "KeyframeInterpolator_translation") continue;
									break;
								case "Scale":
									if (clipTypes[i] != "KeyframeInterpolator_scale") continue;
									break;
								case "Rotation":
									if (clipTypes[i] != "KeyframeInterpolator_rotation") continue;
									break;
								case "Skeletal":
									if (clipTypes[i] != "BonedAnimation") continue;
									break;
							}
							
							AddClip (clipIDs[i], clipTypes[i]);
						}
						break;
					case "Diffuse":
					case "Ambient":
					case "Specular":
					case "Emissive":
					    break;
					    
				}
			}
		}
        		
                		
        private void AddClip (string clipID, string clipType)
        {
			// TODO: initialize the grid to accept BonedAnimation keyframe columns
			int rowIndex = AddAnimationRow(clipType);
            string friendlyName = (string)mHost.Node_GetProperty(clipID, "name");
            string targetName = (string)mHost.Node_GetProperty(clipID, "target");
            

            switch (clipType)
            {
            	case "EllipticalAnimation":
            		break;
            	case "KeyframeInterpolator_translation":
            	case "KeyframeInterpolator_scale":
            		{
            			int index = 0; 
			            //float speed = (float)mHost.Node_GetProperty(nodeID, "speed");
			            // TODO: speed is in Animation not AnimationClip.  Should both
			            // animation.cs and animationclip.cs have "speed" parameter?
			            // HACK
			            float speed = 1f;
			            // end HACK
			            Vector3d[] keyframes = (Vector3d[])mHost.Node_GetProperty(clipID, "keyframes");
			            int length = 0;
			            if (keyframes != null) length = keyframes.Length;
			            bool intrinsic = false;
			            if (keyframes == null)
							UpdateAnimationRowValue(rowIndex, clipID, clipType, null, null, speed, false, length);		            	
		            	else
				            UpdateAnimationRowValue(rowIndex, clipID, clipType, (Vector3d)keyframes[0], (Vector3d)keyframes[1], speed, false, length);
            		
            			break;
            		}
            	case "KeyframeInterpolator_rotation":
            		{
            			int index = 0; 
			            //float speed = (float)mHost.Node_GetProperty(nodeID, "speed");
			            // TODO: speed is in Animation not AnimationClip.  Should both
			            // animation.cs and animationclip.cs have "speed" parameter?
			            // HACK
			            float speed = 1f;
			            // end HACK
			            Quaternion[] keyframes = (Quaternion[])mHost.Node_GetProperty(clipID, "keyframes");
                        float length = 0;
                        if (keyframes != null) length = keyframes.Length;

                        Vector3d[] eulers = new Vector3d[2];
                        eulers[0] = keyframes[0].GetEulerAngles(true);
                        eulers[1] = keyframes[1].GetEulerAngles(true);
                       
			            
			            bool intrinsic = false;
			            
			            UpdateAnimationRowValue(rowIndex, clipID, clipType, (Vector3d)eulers[0], (Vector3d)eulers[1], speed, false, length);
	            		break;
            		}
            	case "BonedAnimation":
            		{
			            int index = (int)mHost.Node_GetProperty(clipID, "index");
			            //float speed = (float)mHost.Node_GetProperty(nodeID, "speed");
			            // TODO: speed is in Animation not AnimationClip.  Should both
			            // animation.cs and animationclip.cs have "speed" parameter?
			            // HACK
			            float speed = 1f;
			            // end HACK
			            float startFrame = (float)mHost.Node_GetProperty(clipID, "startframe");
			            float endFrame = (float)mHost.Node_GetProperty(clipID, "endframe");
			            float length = endFrame - startFrame;
			            bool intrinsic = (bool)mHost.Node_GetProperty(clipID, "intrinsic");
			
			            UpdateAnimationRowValue(rowIndex, clipID, clipType, (int)startFrame, (int)endFrame, speed, false, length);
	            		break;
            		}
            }
        }
      
        private string mClipID;
        #region Playback Events
		void ButtonPlaybackForwardClick(object sender, EventArgs e)
		{
			// get the current animation selected in the cboAnimations
			string animationID = ((KeyValuePair<string, string>)cboAnimations.ComboBox.SelectedItem).Key;
			
			if (string.IsNullOrEmpty (animationID)) return;
			
			mHost.Entity_PlayAnimation(mTargetNodeID, animationID);
		}
		
		void ButtonPlaybackStopClick(object sender, EventArgs e)
		{
			// get the current animation selected in the cboAnimations
			string animationID = ((KeyValuePair<string, string>)cboAnimations.ComboBox.SelectedItem).Key;
			
			if (string.IsNullOrEmpty (animationID)) return;
			
			mHost.Entity_StopAnimation(mTargetNodeID, animationID);
		}
		
		void ButtonPlaybackReverseClick(object sender, EventArgs e)
		{
			// get the current animation selected in the cboAnimations
			string animationName = cboAnimations.Text;
			
			if (string.IsNullOrEmpty (animationName)) return;
			
			// mHost.Entity_PlayAnimation(mTargetNodeID, mCurrentAnimationID);
		}
		
		void ButtonPlaybackReverseFrameClick (object sender, EventArgs e)
		{
		}
		
		void ButtonPlaybackForwardFrameClick (object sender,EventArgs e)
		{
		}
		
		
        private void OnPlayBackEvent(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Control)
            {
                string senderControlName = ((System.Windows.Forms.Control)sender).Name;

                SourceGrid.Selection.RowSelection selectedRow = (SourceGrid.Selection.RowSelection)gridAnimations.Selection;
                if (selectedRow == null) return;

                int[] rows = selectedRow.GetSelectionRegion().GetRowsIndex();
                if (rows == null || rows.Length == 0) return;

                int animationIndex = rows[0] - 1; // subtract 1 because the grid rows are +1 comparedd to
                mClipID = (string)gridAnimations.Rows[rows[0]].Tag;

                switch (senderControlName)
                {
                    case "buttonPlay":
                        if (mMediaTimer == null)
                        {
                            mMediaTimer = new Timer();
                            mMediaTimer.Tick += OnTimerTick;
                            mMediaTimer.Interval = 30;
                        }
                        mMediaTimer.Start();
                        // in scripts we'd probably do
                        // string value = "walk";
                        // EntityAPI.PlayAnimation (entityID, value);
                        // where internally it would
                        // Resource res = Repository.GetNode(nodeID);
                        // EntityBase ent = (EntityBase)res;
                        // if (ent.AnimationSet != null)
                        //    ent.AnimationSet.Play(value);
                        //
                        // THe key here i think is it provides a generic method
                        // to execute a method on a node without having to implement
                        // a unique scripting interface for every single one... 
                        // entityID, triggerName, args
                        // mHost.Node_Trigger(animationSetID, "play", animationName);

                        // however, I could also in script for our vehicle components use
                        // these triggers too for things like 
                        //     mHost.Node_Trigger (nodeID, "destroyed"); // but wait, what's the point of that?
                        // why not just run another behavior?
                        mHost.Entity_PlayAnimation(mTargetNodeID, mClipID);
                        float end = (float)mHost.Node_GetProperty(mClipID, "endframe");
                        float start = (float)mHost.Node_GetProperty(mClipID, "startframe");
 //                       playbackControl.FrameCount = (int)(end - start);

                        // TODO: the plugin I think needs a notification system and I think
                        // that was how we decided we would try to update the GUI
                        // when a node is active.  
                        // 
                        // List <Node> mSubscribed;
                        // // when switching gui panels, we can unsubscribe all of the subscribed
                        // // and then resubscribe the new subscribes 
                        // //

                        // in terms of triggers in scripting, what occurs there?
                        // A condition is evaluated and potentially the trigger is
                        // invoked?
                        // For instance, collision
                        //       - proximity sensor collision that triggers the "opendoor" 
                        //       - I think my plan was that take collisions for instance
                        //       An entity would register with physics and thus be able to receive
                        //       CollisionEvents and then a specific entity like ProximitySensor
                        //       Could verify what entered the sensor and whether that entity is 
                        //       sufficient to invoke trigger in which case it will call
                        //       EntityAPI.InvokeTrigger(triggerID, EventID, args)
                        // mHost.ChangeNodeProperty (animationSet, 
                        break;
                    case "buttonFrameAdvance":
                        break;
                    case "buttonFrameReverse":
                        break;
                    case "buttonStop":
                        // stop timer 
                        if (mMediaTimer != null)
                        {
                            mMediaTimer.Stop();
                            mMediaTimer.Dispose();
                            mMediaTimer = null;
                        }
                        // TODO: this will fail because it expects curentAnimationID not one of it's clipIDs
                        mHost.Entity_StopAnimation(mTargetNodeID, mClipID);

                        break;
                    case "buttonPause":
                        break;

                    default:
                        break;
                }
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Timer Fired @ " + DateTime.Now.ToString());
            float start = (float)mHost.Node_GetProperty(mClipID, "startframe");
            double keyframe = (double)mHost.Entity_GetCurrentKeyFrame(mClipID);
 //           this.playbackControl.CurrentFrame = (int)(keyframe - start);
        }
#endregion

        private void InitializeAnimationGrid()
        {
//            playbackControl.PlayBack_Event += OnPlayBackEvent;

            // init editors
            mEditorString = new SourceGrid.Cells.Editors.TextBox(typeof(string));
            mEditorFloat = new SourceGrid.Cells.Editors.TextBox(typeof(float));
            mEditorNumericFloat = new SourceGrid.Cells.Editors.NumericUpDown(typeof(int), 2500, 0, 1);
            //mEditorVector = SourceGrid.Cells.Editors.Factory.Create(typeof(Keystone.Types.Vector3d));
            mEditorVector = new SourceGrid.Cells.Editors.TextBox(typeof(Vector3d));
            // init controllers 
            // IMPORTANT!!! These controllers should be persisted and shared for each
            // row. Trying to add a controller that is not persisted here at a module level 
            // (eg by doing grid[i,j].AddController(new FloatPropertyController(mHost, "speed"));
            // will result in a Null Exception that won't get thrown for some reason and instead just
            // hang the entire GUI even though the 3d render thread will keep going.

            mEndFrameValueController = new FloatPropertyController(mHost, "endframe");
            mStartFrameValueController = new FloatPropertyController(mHost, "startframe");
            
            // TODO: what if we want more than just 2 keyframes (start and end)?  would we need a drop down list of keyframes?
            //       for simple animations, we could just limit to a start and an end.  For v1.0 maybe this is good enough?
            mStartVectorValueController = new VectorPropertyController(mHost, "keyframes");
            mEndVectorValueController = new VectorPropertyController(mHost, "keyframes");
            mDurationValueController = new FloatPropertyController(mHost, "duration");
            mSpeedValueController = new FloatPropertyController(mHost, "speed");

            gridAnimations.Redim(1, ANIM_NUM_COLUMNS); // just a single header for now
            gridAnimations.SelectionMode = SourceGrid.GridSelectionMode.Row;
            //gridAnimations.MinimumWidth = 50
            //gridAnimations.AutoSizeCells() ' <-- this makes the heights of each row insanely huge?  Why?

            for (int i = 0; i < ANIM_NUM_COLUMNS; i++)
                gridAnimations.Columns[i].AutoSizeMode = SourceGrid.AutoSizeMode.EnableAutoSize | SourceGrid.AutoSizeMode.EnableStretch;


            gridAnimations.AutoStretchColumnsToFitWidth = true;
            //gridAnimations.AutoStretchRowsToFitHeight = true;
            gridAnimations.Columns.StretchToFit(); // <-- this evenly stretches all columns width's to fill the full width of the grid's client area
            //gridAnimations.Rows.StretchToFit();  // <-- this evenly stretches all rows heights to fill the full height of the grid's client area


            // configure the header row
            SourceGrid.Cells.Views.ColumnHeader viewColumnHeader = new SourceGrid.Cells.Views.ColumnHeader();
            DevAge.Drawing.VisualElements.ColumnHeader backHeader = new DevAge.Drawing.VisualElements.ColumnHeader();
            backHeader.BackColor = System.Drawing.Color.FromArgb (255, 71, 122, 212);
            backHeader.Border = DevAge.Drawing.RectangleBorder.NoBorder;

            viewColumnHeader.Background = backHeader;
            viewColumnHeader.ForeColor = System.Drawing.Color.White;
            viewColumnHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 12);
            viewColumnHeader.TextAlignment = DevAge.Drawing.ContentAlignment.MiddleCenter;
            //mCaptionModel = New SourceGrid.Cells.Views.Cell();
            //mCaptionModel.BackColor = gridClients.BackColor;
            

            // build the headers

            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_START_FRAME] = new SourceGrid.Cells.Cell("Start");
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_START_FRAME].View = viewColumnHeader;
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_START_FRAME].ColumnSpan = 1;

            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_END_FRAME] = new SourceGrid.Cells.Cell("End");
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_END_FRAME].View = viewColumnHeader;
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_END_FRAME].ColumnSpan = 1;

            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_SPEED] = new SourceGrid.Cells.Cell("Speed");
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_SPEED].View = viewColumnHeader;
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_SPEED].ColumnSpan = 1;

            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_REVERSE] = new SourceGrid.Cells.Cell("Reverse");
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_REVERSE].View = viewColumnHeader;
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_REVERSE].ColumnSpan = 1;

            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_LENGTH] = new SourceGrid.Cells.Cell("Length (seconds)");
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_LENGTH].View = viewColumnHeader;
            gridAnimations[ANIM_ROW_HEADER, ANIM_COLUMN_LENGTH].ColumnSpan = 1;

            mAnimationGridInitialized = true;
        }


        private int AddAnimationRow(string typeName)
        {
            int prevCount = gridAnimations.RowsCount;
            int newCount = prevCount + 1;
            gridAnimations.Redim(newCount, ANIM_NUM_COLUMNS);

            int rowIndex = newCount - 1; // 0 based means our actual row is one less than count


            switch (typeName)
            {
                case "TextureAnimation":
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].Editor = mEditorNumericFloat; // mEditorFloat;
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].AddController(mStartFrameValueController);

                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].Editor = mEditorNumericFloat; // mEditorFloat;
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].AddController(mEndFrameValueController);
                    break;
                case "BonedAnimation":
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].Editor =  mEditorFloat;
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].AddController(mStartFrameValueController);

                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].Editor =  mEditorFloat;
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].AddController(mEndFrameValueController);
                    break;

                case "EllipticalAnimation":
                case "KeyframeInterpolator_translation":
                case "KeyframeInterpolator_scale":
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].Editor = mEditorVector;
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].AddController(mStartVectorValueController);
                    //gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].Model = ;

                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME] = new SourceGrid.Cells.Cell(new Vector3d(), mEditorVector);
                    //gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].Editor = mEditorVector; 
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].AddController(mEndVectorValueController);
                    break;
                case "KeyframeInterpolator_rotation":
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].Editor = mEditorVector;
                    gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].AddController(mStartVectorValueController);

                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].Editor = mEditorVector;
                    gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].AddController(mEndVectorValueController);
                    break;
            }


            gridAnimations[rowIndex, ANIM_COLUMN_SPEED] = new SourceGrid.Cells.Cell("...");
            gridAnimations[rowIndex, ANIM_COLUMN_SPEED].Editor = mEditorFloat;
            gridAnimations[rowIndex, ANIM_COLUMN_SPEED].AddController(mSpeedValueController);
            // NOTE: DO NOT try to add controllers as the following shows. This will result in mysterious hangs freezes!
            // You must instead assign controllers that are persisted in module level variables.
            //gridAnimations[rowIndex, ANIM_COLUMN_SPEED].AddController(new FloatPropertyController(mHost, "speed"));

            gridAnimations[rowIndex, ANIM_COLUMN_REVERSE] = new SourceGrid.Cells.CheckBox(null, true);

            switch (typeName)
            {
                case "KeyframeInterpolator_rotation":
                case "EllipticalAnimation":
                case "KeyframeInterpolator_translation":
                case "KeyframeInterpolator_scale":
                    gridAnimations[rowIndex, ANIM_COLUMN_LENGTH] = new SourceGrid.Cells.Cell("...");
                    gridAnimations[rowIndex, ANIM_COLUMN_LENGTH].Editor = mEditorFloat; // mEditorNumericFloat; // mEditorFloat;
                    gridAnimations[rowIndex, ANIM_COLUMN_LENGTH].AddController(mDurationValueController);
                    break;
                default:
                    gridAnimations[rowIndex, ANIM_COLUMN_LENGTH] = new SourceGrid.Cells.Cell("...");
                    break;
            }

            // gridAnimations.Columns(GRID_CLIENT_NUMBER).AutoSizeMode = SourceGrid.AutoSizeMode.MinimumSize Or SourceGrid.AutoSizeMode.Default
            //gridAnimations.Columns(GRID_STATE).AutoSizeMode = SourceGrid.AutoSizeMode.EnableAutoSizeView

            //gridAnimations.Columns(GRID_FILE_PROGRESS).Width = 50
            // gridAnimations.Columns(GRID_FILE_PROGRESS).AutoSizeMode = SourceGrid.AutoSizeMode.MinimumSize Or SourceGrid.AutoSizeMode.Default
            //gridAnimations[currentRow, GRID_FILE_PROGRESS] = New SourceGrid.Cells.Cell("control cell");
            //var progressBar = new ProgressBar();
            //progressBar.Name = "progressbar" + currentRow.ToString();
            //gridAnimations.LinkedControls.Add(New SourceGrid.LinkedControlValue(progressBar, new SourceGrid.Position(currentRow, GRID_FILE_PROGRESS)));

            return rowIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowIndex">0 based index for the row in the grid to update</param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="startFrame"></param>
        /// <param name="endFrame"></param>
        /// <param name="speed"></param>
        /// <param name="reverse"></param>
        /// <param name="length"></param>
        /// <param name="numFrames"></param>
        private void UpdateAnimationRowValue(int rowIndex, string id, string typeName, object startFrame, object endFrame, float speed, bool reverse, float length)
        {
            if (rowIndex == ANIM_ROW_HEADER) return;


            //if (cellType == "keyframe")
            gridAnimations[rowIndex, ANIM_COLUMN_START_FRAME].Value = startFrame;
            gridAnimations[rowIndex, ANIM_COLUMN_END_FRAME].Value = endFrame;
            //else if (cellType == "vector")
            //else if (cellType == "angle")
            //else 


            gridAnimations[rowIndex, ANIM_COLUMN_SPEED].Value = speed;
            gridAnimations[rowIndex, ANIM_COLUMN_REVERSE].Value = reverse;
            if (typeName == "BonedAnimation")
            {
                gridAnimations[rowIndex, ANIM_COLUMN_LENGTH].Value = (length / 24).ToString();
                gridAnimations[rowIndex, 0].Row.Tag = id;
            }
            else
            {
                float duration = (float)mHost.Node_GetProperty(id, "duration");
                gridAnimations[rowIndex, ANIM_COLUMN_LENGTH].Value = duration; // length.ToString();
                gridAnimations[rowIndex, 0].Row.Tag = id;
            }

            if (!(string.IsNullOrEmpty(mClipID)) && id == mClipID)
            {
                //playbackControl1.FrameCount = (int)(startFrame - endFrame);
            }
        }

        private void ClearAnimationGrid()
        {
            if (mAnimationGridInitialized)
            {
                if (gridAnimations.RowsCount > 1) // first row is header
                    for (int i = 1; i < gridAnimations.RowsCount; i++)
                        for (int j = 0; j < ANIM_NUM_COLUMNS; j++)
                            gridAnimations[i, j] = null;

                if (gridAnimations.RowsCount > 1) // first row is header
                    // delete all rows from bottom up but NOT the header which is row 0
                    for (int j = gridAnimations.RowsCount - 1; j >= 1; j--)
                        gridAnimations.Rows.Remove(j);
            }
        }

        private void ReleaseAnimationGrid()
        {
            try
            {
//                playbackControl.PlayBack_Event -= OnPlayBackEvent;

                gridAnimations.Dispose();
                mAnimationGridInitialized = false;
            }
            catch { }
        }

        #region Custom Cell Edit Controllers
        private class TextPropertyController : SourceGrid.Cells.Controllers.ControllerBase
        {
            protected IPluginHost mHost;
            protected string mPropertyName;

            public TextPropertyController(IPluginHost host, string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException();
                mHost = host;
                mPropertyName = propertyName;
            }

            public override void OnValueChanged(SourceGrid.CellContext sender, EventArgs e)
            {
                base.OnValueChanged(sender, e);

                int row = sender.Position.Row;
                string id = (string)((SourceGrid.Grid)sender.Grid)[row, 0].Row.Tag;

                if (mHost != null && (!(string.IsNullOrEmpty(id))))
                    mHost.Node_ChangeProperty(id, mPropertyName, typeof(string), (string)sender.Value);
            }
        }

        private class FloatPropertyController : TextPropertyController
        {
            public FloatPropertyController(IPluginHost host, string propertyName)
                : base(host, propertyName)
            {
            }

            public override void OnValueChanged(SourceGrid.CellContext sender, EventArgs e)
            {
                int row = sender.Position.Row;
                string id = (string)((SourceGrid.Grid)sender.Grid)[row, 0].Row.Tag;

                // TODO: when changing the start frame if it's higher than the end, i should increase
                // the end by same amount, and hten change the min value
                // on the up/down for the end frame to always be >= to start up down.value
                if (mHost != null && (!(string.IsNullOrEmpty(id))))
                	mHost.Node_ChangeProperty(id, mPropertyName, typeof(float), (float)sender.Value);
            }
        }

        private class VectorPropertyController : TextPropertyController
        {
            public VectorPropertyController(IPluginHost host, string propertyName)
                : base(host, propertyName)
            {
            }

            public override void OnValueChanged(SourceGrid.CellContext sender, EventArgs e)
            {
                int row = sender.Position.Row;
                SourceGrid.Grid grid = (Grid)sender.Grid;

                string id = (string)grid[row, 0].Row.Tag;
                if (string.IsNullOrEmpty(id)) return;

                Vector3d[] keyframes = new Vector3d[2];
                keyframes[0] = (Vector3d)grid[row, 0].Value;
                keyframes[1] = (Vector3d)grid[row, 1].Value;

                // TODO: can we grab both the start and end Vectors and send them
                //       as an array of type Vector3d[] and with propertyname "keyframes"?
                //       I think we can because we know the sender.Position.Row so we CAN
                //       get both start and end keyframes and pass them as single array.


                // TODO: when changing the start frame if it's higher than the end, i should increase
                // the end by same amount, and then change the min value
                // on the up/down for the end frame to always be >= to start up down.value
                if (mHost != null && (!(string.IsNullOrEmpty(id))))
                    mHost.Node_ChangeProperty(id, mPropertyName, typeof(Vector3d[]), (Vector3d[])keyframes);
            }
        }

        private class CommonEmitterKeyFrameController : TextPropertyController
        {
            int mGroupIndex;
            string mParticleSystemID;

            public CommonEmitterKeyFrameController(IPluginHost host, string particleSystemID, int groupIndex, string propertyName)
                : base(host, propertyName)
            {
                mGroupIndex = groupIndex;
                mParticleSystemID = particleSystemID;
            }

            public override void OnValueChanged(SourceGrid.CellContext sender, EventArgs e)
            {
                 int row = sender.Position.Row;
                Grid grid = (Grid)sender.Grid;

                // we need to construct an array of EmitterKeyFrame and pass that entire array to Geometry_ChangeGroupProperty()
                // thus we only need one Controller type for every cell in the gridEmitter and another for gridParticle.  
                // Even though sending all particle array or emitter array is somewhat expensive, there really shouldn't be too many keyframes for each
                int rowCount = grid.RowsCount;

                Keystone.KeyFrames.EmitterKeyframe[] keyframes = new Keystone.KeyFrames.EmitterKeyframe[rowCount-1];
                for (int i = 1; i < rowCount; i++) // we start at i = 1 since first row is header
                {
                    keyframes[i-1].Key = (float)grid[i, EMITTER_COLUMN_KEY].Value;
                    keyframes[i-1].Color = (Keystone.Types.Color)grid[i, EMITTER_COLUMN_COLOR].Value;
                    keyframes[i-1].MainDirection = (Vector3f)grid[i, EMITTER_COLUMN_DIR].Value;
                    keyframes[i-1].LocalPosition = (Vector3f)grid[i, EMITTER_COLUMN_POSITION].Value;
                    keyframes[i-1].Lifetime = (float)grid[i, EMITTER_COLUMN_LIFETIME].Value;
                    keyframes[i-1].Power = (float)grid[i, EMITTER_COLUMN_POWER].Value;
                    keyframes[i-1].Radius = (float)grid[i, EMITTER_COLUMN_RADIUS].Value;
                    keyframes[i-1].Speed = (float)grid[i, EMITTER_COLUMN_SPEED].Value;
                }

                if (mHost != null && !mHost.PluginChangesSuspended  && (!(string.IsNullOrEmpty(mParticleSystemID))))
                    mHost.Geometry_ChangeGroupProperty(mParticleSystemID, mGroupIndex, mPropertyName, typeof(Keystone.KeyFrames.EmitterKeyframe[]).Name, keyframes);
            }
        }

        private class CommonParticleKeyFrameController : TextPropertyController
        {
            int mGroupIndex;
            string mParticleSystemID;

            public CommonParticleKeyFrameController(IPluginHost host, string particleSystemID, int groupIndex, string propertyName)
                : base(host, propertyName)
            {
                mGroupIndex = groupIndex;
                mParticleSystemID = particleSystemID;
            }

            public override void OnValueChanged(SourceGrid.CellContext sender, EventArgs e)
            {
                int row = sender.Position.Row;
                Grid grid = (Grid)sender.Grid;
                               
                // we need to construct an array of ParticleKeyframe and pass that entire array to Geometry_ChangeGroupProperty()
                // thus we only need one Controller type for every cell in the gridEmitter and another for gridParticle.  
                // Even though sending all particle array or emitter array is somewhat expensive, there really shouldn't be too many keyframes for each
                int rowCount = grid.RowsCount;

                Keystone.KeyFrames.ParticleKeyframe[] keyframes = new Keystone.KeyFrames.ParticleKeyframe[rowCount-1];
                for (int i = 1; i < rowCount; i++) // we start at i = 1 since first row is header
                {
                    keyframes[i-1].Key = (float)grid[i, PARTICLE_COLUMN_KEY].Value;
                    keyframes[i-1].Color = (Keystone.Types.Color)grid[i, PARTICLE_COLUMN_COLOR].Value;
                    keyframes[i-1].Rotation = (Vector3f)grid[i, PARTICLE_COLUMN_ROTATION].Value;
                    keyframes[i-1].Size = (Vector3f)grid[i, PARTICLE_COLUMN_SIZE].Value;
                }

                if (mHost != null && (!(string.IsNullOrEmpty(mParticleSystemID))))
                    mHost.Geometry_ChangeGroupProperty(mParticleSystemID, mGroupIndex, mPropertyName, typeof(Keystone.KeyFrames.ParticleKeyframe[]).Name, keyframes);
            }
        }
        #endregion
    }
}