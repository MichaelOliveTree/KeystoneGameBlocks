
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KeyPlugins;
using Keystone.Types;
using System.Runtime.InteropServices;

namespace KeyPlugins
{
    partial class BaseEntityPlugin
    {
        private const int TVIF_STATE = 0x8; 
        private const int TVIS_STATEIMAGEMASK = 0xF000; 
        private const int TV_FIRST = 0x1100; 
        private const int TVM_SETITEM = TV_FIRST + 63; 
     
        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Auto)] 
        private struct TVITEM 
        { 
            public int mask; 
            public IntPtr hItem; 
            public int state; 
            public int stateMask; 
            [MarshalAs(UnmanagedType.LPTStr)] 
            public string lpszText; 
            public int cchTextMax; 
            public int iImage; 
            public int iSelectedImage; 
            public int cChildren; 
            public IntPtr lParam; 
        } 
     
        [DllImport("user32.dll", CharSet = CharSet.Auto)] 
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, 
                                                 ref TVITEM lParam); 
 
        
        private const int GEOM_NUM_COLUMNS = 2;
        private const int GEOM_ROW_HEADER = 0;

        private const int GEOM_COLUMN_NAME = 0;
        private const int GEOM_COLUMN_GEOMETRY_TYPE = 1;
        private const int GEOM_COLUMN_FORCE_TRANSITION = 2;
        private const int GEOM_COLUMN_SWITCH_DISTANCE = 3;


        private void CreateGeometryCard(string nodeID)
        {
            IntPtr handle = mHost.Engine_CreateViewport();
            GeometryEditCard geoCard = new GeometryEditCard(handle, nodeID);
            geoCard.Text = nodeID;
            superTabControlPanel5.Controls.Add(geoCard);
        }


        // TODO: this must also be called when the root model/switch is deleted
        //       This should happen on the NodeRemoved notify.  DomainObject tree should be
        // modified to also clear the panel on the domainobject's removal.
        // ditto for animation
        private void ClearModelPanel()
        {
            // if there is no model, we show buttons to Create Model or Create LOD or create Sequence
            if (this.treeModel.Nodes != null && treeModel.Nodes.Count > 0)
                treeModel.Nodes.Clear();  // TODO: why does this clear attempt with count == 0 fail? cross threading issue?

            treeModel.Visible = false;
            buttonAddModel.Visible = true;
            buttonAddModelSelector.Visible = true;
        }

        private void PopulateModelTree(string parentID, string childID, string typeName)
        {
           
            // else we show the tree so users can start editing their entity's visual model
            if (treeModel.Nodes != null && treeModel.Nodes.Count > 0)
                treeModel.Nodes.Clear();

            treeModel.Visible = true;
            buttonAddModel.Visible = false;
            buttonAddModelSelector.Visible = false;


            string[] filter = new string[12];
            // Material Resource
            filter[0] = "Material";
            // Texture Resource
            filter[1] = "Texture";  // texture is jsut resource
            // Layers
            filter[2] = "Diffuse";  // Diffuse, NormalMap, etc are Layers that contain instance info (eg tiling settings, texture offsets, etc)
            filter[3] = "NormalMap";
            filter[4] = "SpecularMap";
            filter[5] = "EmissiveMap";
            filter[6] = "CubeMap";
            filter[7] = "SpriteSheet";
            filter[8] = "SplatAlpha";
            filter[9] = "VolumeTexture";
            filter[10] = "DUDVTexture";
            filter[11] = "Shader";
            
            DevComponents.AdvTree.Node rootNode = GenerateTreeBranch2(treeModel, null, parentID, childID, typeName, filter, true, ModelTree_NodeCreated2);
            // TODO: if we supply a callback to GenerateTree, we can add our checkbox flag nodes to the
            // specific node types we want.
            if (rootNode == null)
            {
                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                return;
            }
        }

        private void PopulateModelPanel(string parentID, string childID, string typeName)
        {
            textModelName.Text = (string)mHost.Node_GetName(childID);
            // TODO: models can have positions/rotations/scales of their own so why not
            //       allow clicking on the model to bring up similar tab as entity general tab
            //       and have our flag checkboxes there...
;

            cbModelEnable.Checked = (bool)mHost.Node_GetFlagValue(childID, "enable");
            
            cbShadowRecv.Checked = (bool)mHost.Model_GetFlagValue(childID, "recvshadow");
            cbShadowCast.Checked = (bool)mHost.Model_GetFlagValue(childID, "castshadow");
            cbOccluder.Checked = (bool)mHost.Model_GetFlagValue(childID, "occluder");
            cbUseInstancing.Checked = (bool)mHost.Model_GetFlagValue(childID, "instancing");
            cbCSGAccept.Checked = (bool)mHost.Model_GetFlagValue(childID, "csgaccept");
            cbCSGStencil.Checked = (bool)mHost.Model_GetFlagValue(childID, "csgsource");

            // string.Format{"{0:0.00}"}; // set in the spinner control's format property
            // TODO: i think there's a bug where placementtool will load an entity and that
            // temp entity will get populated in the plugin and wwhen unloaded, trying
            // to modify that entity here will result in null ref exception
            Vector3d vec = (Vector3d)mHost.Node_GetProperty(childID, "position");
            spinPositionModelX.Value = vec.x;
            spinPositionModelY.Value = vec.y;
            spinPositionModelZ.Value = vec.z;

            vec = (Vector3d)mHost.Node_GetProperty(childID, "scale");
            spinScaleModelX.Value = vec.x;
            spinScaleModelY.Value = vec.y;
            spinScaleModelZ.Value = vec.z;
            Quaternion quat = (Quaternion)mHost.Node_GetProperty(childID, "rotation");
            vec = quat.GetEulerAngles(true);
            spinRotateModelX.Value = vec.x;
            spinRotateModelY.Value = vec.y;
            spinRotateModelZ.Value = vec.z;

            // sub grids
            // lod/selector switch settings grid
            // model types grid
            //ReleaseGeometryGrid();
            //InitializeGeometryGrid();
        }

        private void PopulateGeometryPanel(string geometryID, string typeName, string modelID)
        {
            // dependant on which particular Geometry is clicked
            int count = (int)mHost.Geometry_GetStatistic(geometryID, "groups");
            string FORMAT = "{0:0,0}";
            
            
            if (typeName == "ParticleSystem")
            {
                lblGroupCount.Text = "Emitters: " + string.Format(FORMAT, count);
                lblTriangles.Visible = false;
                lblBones.Visible = false;
                //lblHeight
                //lblDepth
                //lblWidth
                lblCenterOffsetX.Visible = false;
                lblCenterOffsetY.Visible = false;
                lblCenterOffsetZ.Visible = false;

                cbDoubleSided.Visible = false;

                // emitters will be handled in a seperate supertab. particle and emitter keyframes can be edited there
                // todo: when adding/removing emitters, the corresponding GroupAttribute under DefaultAppearance should be added/removed. We should have seperate Add menu items for the different types such as PointSprite, Billboard, minimesh? can i test this in tv3dtools particle editor? for instance, addd  rotation and a color keyframe arrays. Problem is
                //       we also need space for regular emitter properties such as behavior. But i don't think the grids for keyframes take up much space.  
                // graphically to implement this, we just need two sourcegrids side by side... one for the entire emitter and the other for individual particle
            }
            else
            {
                lblGroupCount.Text = "Groups: " + string.Format(FORMAT, count);
                count = (int)mHost.Geometry_GetStatistic(geometryID, "triangles");
                lblTriangles.Text = "Triangles: " + string.Format(FORMAT, count);
                count = (int)mHost.Geometry_GetStatistic(geometryID, "vertices");
                lblVertices.Text = "Vertices: " + string.Format(FORMAT, count);

                if (typeName == "Actor3d")
                {
                    lblBones.Visible = true;
                    lblBones.Text = "Bones: " + (int)mHost.Geometry_GetStatistic(geometryID, "bones");
                }
                else
                    lblBones.Visible = false;


                FORMAT = "{0:#,###.###}";
                double size = (double)mHost.Geometry_GetStatistic(geometryID, "height");
                lblHeight.Text = "Height: " + string.Format(FORMAT, size);

                size = (double)mHost.Geometry_GetStatistic(geometryID, "depth");
                lblDepth.Text = "Depth: " + string.Format(FORMAT, size);

                size = (double)mHost.Geometry_GetStatistic(geometryID, "width");
                lblWidth.Text = "Width: " + string.Format(FORMAT, size);

                Vector3d centerOffset = (Vector3d)mHost.Geometry_GetStatistic(geometryID, "centeroffset");
                lblCenterOffsetX.Text = "Center Offset X: " + string.Format(FORMAT, centerOffset.x);
                lblCenterOffsetY.Text = "Center Offset Y: " + string.Format(FORMAT, centerOffset.y);
                lblCenterOffsetZ.Text = "Center Offset Z: " + string.Format(FORMAT, centerOffset.z);

                labelResourceDescriptor.Text = geometryID;

                cbDoubleSided.Checked = (int)mHost.Node_GetProperty(geometryID, "cullmode") == 1;// 1 == double-sided, 3 == backface cull. default is unchecked and == 3
            }
            superTabItem4.Tag = new string[] { modelID, geometryID };
        }


        private void textModelName_TextChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                if (treeModel.SelectedNode == null) return;
                string modelID = treeModel.SelectedNode.Name; 
                mHost.Node_ChangeProperty(modelID, "name", typeof(string), textModelName.Text);
            }
        }

        private void cbDoubleSided_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                string[] ids = (string[])superTabItem4.Tag;
                string modelID = ids[0];
                string geometryID = ids[1];
                int value = (cbDoubleSided.Checked ? 1 : 3); // 1 == double-sided, 3 == backface cull. default is unchecked and == 3
                mHost.Node_ChangeProperty(geometryID, "cullmode", typeof(int), value);
            }
        }

        private void buttonComputeResetCenter_Click(object sender, EventArgs e)
        {
            string[] ids = (string[])superTabItem4.Tag;
            string modelID = ids[0];
            string geometryID = ids[1];

            Vector3d centerOffset = (Vector3d)mHost.Geometry_GetStatistic(geometryID, "centeroffset");
            spinResetCenterX.Value = -centerOffset.x;
            spinResetCenterY.Value = -centerOffset.y;
            spinResetCenterZ.Value = -centerOffset.z;
        }

        private void buttonSaveGeometryAs_Click(object sender, EventArgs e)
        {
            string[] ids = (string[])superTabItem4.Tag;
            string modelID = ids[0];
            string geometryID = ids[1];
            string fileDialogFilter = "Meshes|*.tvm";
            SaveGeometryAs(geometryID, modelID, fileDialogFilter);
        }

        private void SaveGeometryAs(string geometryID, string modelID, string fileDialogFilter)
        {
            System.Windows.Forms.FileDialog browser = new System.Windows.Forms.SaveFileDialog();
            browser.InitialDirectory = this.mModFolderPath;
            browser.RestoreDirectory = false;
            browser.Filter = fileDialogFilter;
            System.Windows.Forms.DialogResult result = browser.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            // resourcePath = new KeyCommon.IO.ResourceDescriptor(browser.ModName.ToLower(), browser.ModDBSelectedEntry.ToLower()).ToString();
            string resourcePath = browser.FileName;
            int modPathLength = this.mModFolderPath.Length + 1;
            // use relative path to "mods" folder so that code works on any install folder location
            resourcePath = resourcePath.Remove(0, modPathLength);

            if (string.IsNullOrEmpty(resourcePath))
                return;

            // save the geometry to the specified file
            mHost.Geometry_Save(geometryID, resourcePath, this.mModFolderPath);

            // we now need to update this ParticleSystem geometry's ID to use the new resourcepath?
            // todo: problem with Node_ReplaceResource is it doesn't give us the option to manage the GroupAttributes. 
            //       all i think we need though is ability to refresh the model tree and if "automanage" then recreate the DefaultAppearance and it's Groups.  The server doesn't care about Appearance nodes.
            if (geometryID != resourcePath)
                // NOTE: this only does the replacement for the current Model thus it's meant to mostly only be used when editing prefabs and particlesystems
                //       TODO: so it'd be nice to have a callback function we can pass to know when the replacement has occurred.
                //              TODO: or maybe the servver can do it for certain Geometry types and inform the client of the new nodes, then all we need to do is refresh the model tree and panels.
                //              TODO: this seems reasonable for all Geometry types that > 1 group or perhaps if the groupCount.  This is a good solution as its only used for editing.
                //              TODO: this calls into question the whole point of ReplaceResource command.  Its useful for Material, Texture, Shaders, but should not be used for Geometry.  
                //                    TODO: we should use a different command if we just want to change the ID/ResourcePath of a Geometry.
                //                     TODO HOWEVER, in this particular use case of saving the existing particle system, all we want is to replace the ID.  That is the only instance where we should allow Node_ReplaceResource should be used, otherwise you need to delete the resource and add in a new one where we can automanage GroupAttributes.
                //                   TOOD: so maybe for clarification, we should add a "Node_RenameResource" where we don't technically add or remove any node.
                mHost.Node_ResourceRename(geometryID, resourcePath, modelID); // we should have an option to automanage GroupAttributes so that when we replace, we first delete all existing GroupAttributes and replace them as we identity groups within the swapped in geometry
        }

        private void buttonApplyGeometryChanges_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show("These changes modify the actual geometry resource and as such will affect all Models & Entities that share this resource.  Do you still wish to continue and apply these changes?", "Confirm geometry modification?", MessageBoxButtons.YesNoCancel);
            if (confirm == DialogResult.Yes)
            {
                Vector3d translation, scale, rotation;

                translation.x = spinResetCenterX.Value;
                translation.y = spinResetCenterY.Value;
                translation.z = spinResetCenterZ.Value;

                scale.x = spinResetScaleX.Value;
                scale.y = spinResetScaleY.Value;
                scale.z = spinResetScaleZ.Value;

                rotation.x = spinResetRotationPitch.Value;
                rotation.y = spinResetRotationYaw.Value;
                rotation.z = spinResetRotationRoll.Value;

                Matrix tmat = Matrix.CreateTranslation(translation);
                Matrix smat = Matrix.CreateScaling(scale);
                Quaternion quat = new Quaternion(rotation.y, rotation.x, rotation.z);
                quat = Quaternion.Normalize(quat);

                Matrix rmat = new Matrix(quat);
                Matrix result = smat * rmat * tmat;

                // TODO: or Geometry_TransformVertices
                string[] ids = (string[])superTabItem4.Tag;
                string modelID = ids[0];
                string geometryID = ids[1];
                mHost.Geometry_ResetTransform(geometryID, result);

                // save and overwrite (assumes existing resource already exists and whos same resource path
                // can be used
                // TODO: i shoudl automatically start "save" upon confirming user wants to apply changes
                //mHost.Geometry_Save(mPosScaleRotationTarget);

                // TODO: save as copy can pop up asset browser

                // TODO: export can pop up file save as dialog
            }
        }

        private void buttonAddModel_Click(object sender, EventArgs e)
        {
            // if the sender is the ButtonX, then we're adding a root Model or ModelSelector
            // otherwise the PopupMenu is sender and we can use the "Name" to determine the parent
            if (sender is DevComponents.DotNetBar.ButtonX)
                CreateNode("Model", TargetID);
            else
                CreateNode("Model", ((ToolStripMenuItem)sender).Name.ToString());
        }

        private void buttonAddModelSelector_Click(object sender, EventArgs e)
        {
            if (sender is DevComponents.DotNetBar.ButtonX)
                CreateNode("ModelSelector", TargetID);
            else
                CreateNode("ModelSelector", ((ToolStripMenuItem)sender).Name.ToString());
        }

        private void cbModelEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_SetFlagValue(mPosScaleRotationTarget, "enable", cbModelEnable.Checked);
        }

        private void cbCSGStencil_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Model_SetFlagValue(mPosScaleRotationTarget, "csgsource", cbCSGStencil.Checked);
        }

        private void cbCSGAccept_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Model_SetFlagValue(mPosScaleRotationTarget, "csgaccept", cbCSGAccept.Checked);
        }

        private void cbUseInstancing_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Model_SetFlagValue(mPosScaleRotationTarget, "instancing", cbUseInstancing.Checked);
        }

        private void cbOccluder_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Model_SetFlagValue(mPosScaleRotationTarget, "occluder", cbOccluder.Checked);
        }

        private void cbShadowCast_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Model_SetFlagValue(mPosScaleRotationTarget, "castshadow", cbShadowCast.Checked);
        }

        private void cbShadowRecv_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Model_SetFlagValue(mPosScaleRotationTarget, "recvshadow", cbShadowRecv.Checked);
        }

        #region TreeModel
        protected void ModelTree_NodeCreated(TreeView tvw, TreeNode parentNode, TreeNode childNode, string childTypeName)
        {
            HideCheckBox(tvw, childNode);
        }


        protected void ModelTree_NodeCreated2(DevComponents.AdvTree.AdvTree tvw, DevComponents.AdvTree.Node parentNode, DevComponents.AdvTree.Node childNode, string childTypeName)
        {
            switch (childTypeName)
            {
                case "DefaultAppearance":
                case "GroupAttribute":
                    DevComponents.AdvTree.Cell c = new DevComponents.AdvTree.Cell();

                    string currentBlendingValue = GetSimplifiedBlendingName((int)mHost.Node_GetProperty(childNode.Name, "blendingmode"));

                    c.Text = "<a href=\"blendingmode\">" + currentBlendingValue + "</a>";

                    childNode.Cells.Add(c);
                    childNode.CheckBoxStyle = DevComponents.DotNetBar.eCheckBoxStyle.CheckBox;
                    childNode.CheckBoxVisible = true;
                    break;
            }
        }

        private string GetSimplifiedLightingModeName(int lightningMode)
        {
            switch (lightningMode)
            {

                case 0: return "NONE";
                case 1: return "NORMAL";
                case 2: return "MANAGED";
                case 3: return "BUMP_TANGENT_SPACE";
                case 4: return "BUMP_OFFSET";
                case 5: return "PER-VERTEX PRT";

               default: return "MANAGED";
            }
        }
    

        private string GetSimplifiedBlendingName(int blendingMode)
        {
            switch (blendingMode)
            {
                
                case 1: return "BLEND_ALPHA";
                case 2: return "BLEND_ADD";
                case 3 : return "BLEND_COLOR";
                case 4: return "BLEND_ADDALPHA";
                case 5: return "BLEND_MULTIPLY";
                
                case 0: default: return "BLEND_NO";
            }
        }

        private void OnTreeModel_BlendLink_Clicked(object sender, DevComponents.AdvTree.MarkupLinkClickEventArgs e)
        {
            
            DevComponents.AdvTree.Cell c = (DevComponents.AdvTree.Cell)sender;
            DevComponents.AdvTree.Node node = c.Parent;
            
            string propertyName = e.HRef;
            string value = c.Text;

            // popup a menu for changing the blend
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Tag = c;
            ToolStripMenuItem menuItem = null;

            menuItem = new ToolStripMenuItem("BLEND_NO");
            menuItem.Name = node.Name;
            menuItem.Tag = node.Tag.ToString();
            menuItem.Click += new EventHandler(menuBlendingMode_Click);
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("BLEND_ALPHA");
            menuItem.Name = node.Name;
            menuItem.Tag = node.Tag.ToString();
            menuItem.Click += new EventHandler(menuBlendingMode_Click);
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("BLEND_ADD");
            menuItem.Name = node.Name;
            menuItem.Tag = node.Tag.ToString();
            menuItem.Click += new EventHandler(menuBlendingMode_Click);
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("BLEND_COLOR");
            menuItem.Name = node.Name;
            menuItem.Tag = node.Tag.ToString();
            menuItem.Click += new EventHandler(menuBlendingMode_Click);
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("BLEND_ADDALPHA");
            menuItem.Name = node.Name;
            menuItem.Tag = node.Tag.ToString();
            menuItem.Click += new EventHandler(menuBlendingMode_Click);
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("BLEND_MULTIPLY");
            menuItem.Name = node.Name;
            menuItem.Tag = node.Tag.ToString();
            menuItem.Click += new EventHandler(menuBlendingMode_Click);
            menu.Items.Add(menuItem);

            menu.Show(node.TreeControl, node.TreeControl.PointToClient(System.Windows.Forms.Cursor.Position));

            
        }

        private void menuBlendingMode_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item =  (ToolStripMenuItem)sender;
            DevComponents.AdvTree.Cell c = (DevComponents.AdvTree.Cell)((ContextMenuStrip)item.Owner).Tag;

            string nodeID = item.Name;
            string propertyName = "blendingmode";

            object value;

            c.Text = "<a href=\"blendingmode\">" + item.Text + "</a>";

            switch (item.Text)
            { 
                case "BLEND_ADD":
                    value = (int)2;
                    break;
                case "BLEND_ADDALPHA":
                    value = (int)4;
                    break;
                case "BLEND_ALPHA":
                    value = (int)1;
                    break;
                case "BLEND_COLOR":
                    value = (int)3;
                    break;
                case "BLEND_MULTIPLY":
                    value = (int)5;
                    break;
                case "BLEND_NO":
                    value = (int)0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            mHost.Node_ChangeProperty(nodeID, propertyName, typeof(int),value);
        }

        private void OnTreeModel_NodeChecked2(object sender, DevComponents.AdvTree.AdvTreeCellEventArgs e)
        {
        }

        private void OnTreeModel_NodeChecked(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Text)
            {

                default:
                    break;
            }
        }

        private void OnTreeModel_NodeClick2(object sender, DevComponents.AdvTree.TreeNodeMouseEventArgs e)
        {
            string parentNodeID = null;
            if (e.Node.Parent != null)
                parentNodeID = (string)e.Node.Parent.Name;
            else // this is root Model or ModelSelector and so the current entity is parent
                parentNodeID = TargetID;

            string nodeID = e.Node.Name;
            string typeName = e.Node.Text;

            if (e.Button == MouseButtons.Right)  // right mouse click menu
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                ToolStripMenuItem menuItem = null;

                //string modelID = treeModel.Nodes[0].Name; // TODO: this could be a ModelSelector so this is wrong

                // if it is a Model or ModelSelector,  allow for it be inserted under a new
                // ModelSelector.  Selectors under Selectors are allowed.
                if (treeModel.SelectedNode.Index == 0)
                {
                    menuItem = new ToolStripMenuItem("Insert under new Model Selector parent");
                    menuItem.Name = nodeID;
                    menuItem.Tag = parentNodeID;
                    menuItem.Click += new EventHandler(menuInsertNewModelSelectorParent_Click);
                    menu.Items.Add(menuItem);

                    // seperator
                    menu.Items.Add(new ToolStripSeparator());
                }



                string nodeType = typeName;
                if (typeName.Contains("Emitter #"))
                    nodeType = "Emitter";
                else if (typeName.Contains("Attractor #"))
                    nodeType = "Attractor";

                switch (nodeType)
                {
                    
                    case "Model":
                        menuItem = new ToolStripMenuItem("Add Appearance");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddAppearance_Click);
                        menu.Items.Add(menuItem);

                        menuItem = new ToolStripMenuItem("Add Group Appearances");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddGroupAppearances_Click);
                        menu.Items.Add(menuItem);

                        // seperator
                        menu.Items.Add(new ToolStripSeparator());

                        menuItem = new ToolStripMenuItem("Add Actor3d");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddActor3d_Click);
                        menu.Items.Add(menuItem);

                        menuItem = new ToolStripMenuItem("Add Mesh3d");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddMesh3d_Click);
                        menu.Items.Add(menuItem);

                        menuItem = new ToolStripMenuItem("Add Billboard");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddBillboard_Click);
                        menu.Items.Add(menuItem);

                        // seperator
                        menu.Items.Add(new ToolStripSeparator());

                        menuItem = new ToolStripMenuItem("Add Particle System");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddParticleSystem_Click);
                        menu.Items.Add(menuItem);

                        // seperator
                        menu.Items.Add(new ToolStripSeparator());

                        menuItem = new ToolStripMenuItem("Delete");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                        menu.Items.Add(menuItem);
                        break;

                    case "ModelSelector":
                        menuItem = new ToolStripMenuItem("Add Model");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddModel_Click);
                        menu.Items.Add(menuItem);

                        menuItem = new ToolStripMenuItem("Add Model Selector");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddModelSelector_Click);
                        menu.Items.Add(menuItem);

                        // seperator
                        menu.Items.Add(new ToolStripSeparator());

                        menuItem = new ToolStripMenuItem("Delete");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                        menu.Items.Add(menuItem);
                        break;

                    case "Actor3d":
                    case "Mesh3d":
                    case "Billboard":
                        // geometry types cannot have children, but can be deleted
                        menuItem = new ToolStripMenuItem("Delete");
                        menuItem.Tag = parentNodeID;
                        menuItem.Name = nodeID;
                        menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                        menu.Items.Add(menuItem);
                        break;
                    case "ParticleSystem":
                        {
                            string particleSystemID = nodeID;
                            string modelID = (string)e.Node.Parent.Name; // parent of the ParticleSystem node is a Model node.
                            menuItem = new ToolStripMenuItem("Add Pointsprite Emitter");
                            menuItem.Name = particleSystemID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(menuAddEmitterPointSprite);
                            menu.Items.Add(menuItem);

                            menuItem = new ToolStripMenuItem("Add Billboard Emitter");
                            menuItem.Name = particleSystemID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(menuAddEmitterBillboard);
                            menu.Items.Add(menuItem);

                            menuItem = new ToolStripMenuItem("Add Minimesh Emitter");
                            menuItem.Name = particleSystemID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(menuAddEmitterMinimesh);
                            menu.Items.Add(menuItem);
                            
                            menuItem = new ToolStripMenuItem("Add Attractor");
                            menuItem.Name = particleSystemID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(menuiAddAttractor_Click);
                            menu.Items.Add(menuItem);

                            // seperator
                            menu.Items.Add(new ToolStripSeparator());

                            menuItem = new ToolStripMenuItem("Save");
                            menuItem.Name = particleSystemID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(Save_Click); 
                            menu.Items.Add(menuItem);

                            // seperator
                            menu.Items.Add(new ToolStripSeparator());
                            menuItem = new ToolStripMenuItem("Delete");
                            menuItem.Name = parentNodeID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                            menu.Items.Add(menuItem);
                            break;
                        }
                    case "Emitter":
                        {
                            string particleSystemID = parentNodeID;
                            string modelID = (string)e.Node.Parent.Parent.Tag; // parent of the parent of this Emitter node a Model node.
                            menuItem = new ToolStripMenuItem("Delete");
                            menuItem.Name = particleSystemID + "," + e.Node.Text; 
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(Delete_Emitter_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                            menu.Items.Add(menuItem);
                            break;
                        }
                    case "Attractor":
                        {
                            string particleSystemID = parentNodeID + "," + e.Node.Text;
                            string modelID = (string)e.Node.Parent.Parent.Tag; // parent of the parent of this Attractor node a Model node.
                            menuItem = new ToolStripMenuItem("Delete");
                            menuItem.Name = particleSystemID;
                            menuItem.Tag = modelID;
                            menuItem.Click += new EventHandler(Delete_Attractor_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                            menu.Items.Add(menuItem);
                            break;
                        }
                    case "Appearance":
                    case "DefaultAppearance":
                    case "SplatAppearance":
                        PopulateOverallAppearanceMenu(menu, nodeID, parentNodeID, new Point(e.X, e.Y));
                        break;
                    case "GroupAttribute":
                        // NOTE: GroupAttributes cannot be deleted independantly
                        // and so there is no Delete menu item available.
                        // The entire DefaultAppearance must be deleted
                        menuItem = new ToolStripMenuItem("Rendering Order - Move Up");
                        menuItem.Tag = parentNodeID;
                        menuItem.Name = nodeID;
                        menuItem.Click += new EventHandler(menuGroupAttributeMoveUp_Click);
                        menu.Items.Add(menuItem);

                        menuItem = new ToolStripMenuItem("Rendering Order - Move Down");
                        menuItem.Tag = parentNodeID;
                        menuItem.Name = nodeID;
                        menuItem.Click += new EventHandler(menuGroupAttributeMoveDown_Click);
                        menu.Items.Add(menuItem);

                        menu.Items.Add(new ToolStripSeparator());

                        PopulateGroupAppearanceMenu(menu, nodeID, parentNodeID, new Point(e.X, e.Y));
                        break;
                    case "TextureCycle":
                        menuItem = new ToolStripMenuItem("Add Texture");
                        menuItem.Name = nodeID;
                        menuItem.Tag = parentNodeID;
                        menuItem.Click += new EventHandler(menuAddTextureCycleTexture_Click);
                        menu.Items.Add(menuItem);

                        menuItem = new ToolStripMenuItem("Delete");
                        menuItem.Tag = parentNodeID;
                        menuItem.Name = nodeID;
                        menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
                        menu.Items.Add(menuItem);
                        break;
                    
                    default:
                        throw new Exception("Unexpected type '" + typeName + "'");
                }

                menu.Show(treeModel, e.X, e.Y);
            }
            else if (e.Button == MouseButtons.Left) // selected a group from the list
            {
                string nodeTypeName = typeName;
                if (typeName.Contains("Emitter #"))
                    nodeTypeName = "Emitter";
                else if (typeName.Contains("Attractor #"))
                    nodeTypeName = "Attractor";

                switch (nodeTypeName)
                {
                    case "ModelSelector":
                        superTab.SelectedTab = superTabItem5;
                        // is this obsolete now that Mesh3d is clickable from treeModel?
                        // note: one issue with having geometry type set from ModelSelector is
                        // that if the model is deleted, we now have to update this grid...
                        // 
                        break;
                    case "Model":
                        // TODO: Model should have a drop down for GeometryType _if_ under a Selector
                        // or well.. hm... 
                        // TODO: superTabItem6 should have a reference to the geometry
                        // resource so we can drag and drop and change it or do something such as
                        // change it's level of detail.
                        // TODO: what about the CSGPunch and CSGStencil nodes?
                        superTab.SelectedTab = superTabItem6;
                        mPosScaleRotationTarget = nodeID;
                        PopulateModelPanel(parentNodeID, nodeID, typeName);
                        break;

                    // for Material, Diffuse, etc we show the appearance cards still
                    // but for nodeID we have to use the parent Appearance/GroupAttribute
                    case "Appearance":
                    case "SplatAppearance":
                    case "DefaultAppearance":
                    case "GroupAttribute":
                        superTab.SelectedTab = superTabItem2;
                        CreateAppearanceCards(nodeID);
                        break;

                    // NOTE: the following types are excluded from appearing in the treeviewModel
                    // so will never actually be clicked.
                    case "Material":
                    case "TextureCycle":
                    case "SplatAlpha":
                    case "Diffuse": // etc layer types
                    case "Specular":
                    case "NormalMap":
                    case "Emissive":
                    case "VolumeTexture":
                    case "DUDVTexture":
                    case "CubeMap":
                    case "Texture":
                        break;

                    case "Actor3d":
                    case "Mesh3d":
                        PopulateGeometryPanel(nodeID, typeName, parentNodeID);
                        // nodeID is cached in the superTabItem4.Tag
                        superTab.SelectedTab = superTabItem4;

                        // show a card that is similar to the one we show for Model
                        // except we add options to re-save in archive as new format, export, re-center,
                        // lod tools, rotate and re-save, generate hulls 
                        break;
                     
                    // todo: i believe these should fill GeometryPanel but have some more properties for billboards and particlesystems and not just position, scale and rotation
                    case "Billboard":
                    case "ParticleSystem":
                        PopulateGeometryPanel(nodeID, typeName, parentNodeID);
                        // nodeID is cached in the superTabItem4.Tag
                        superTab.SelectedTab = superTabItem4;
                        break;
                    case "Emitter":
                        superTab.SelectedTab = superTabItem11;
                        string modelID = (string)e.Node.Parent.Parent.Name;
                        CreateEmitterPanel(modelID, parentNodeID, nodeID);
                        break;
                    case "Attractor":
                        superTab.SelectedTab = superTabItem12;

                        string modelID2 = (string)e.Node.Parent.Parent.Name;
                        CreateAttractorPanel(modelID2, parentNodeID, nodeID);
                        break;
                }
            }
        }

        //private void OnTreeModel_NodeClick(object sender, TreeNodeMouseClickEventArgs e)
        //{
        //    string parentNodeID = null;
        //    if (e.Node.Parent != null)
        //        parentNodeID = (string)e.Node.Parent.Name;
        //    else // this is root Model or ModelSelector and so the current entity is parent
        //        parentNodeID = TargetID;

        //    string nodeID = e.Node.Name;
        //    string typeName = e.Node.Text;

        //    if (e.Button == MouseButtons.Right)  // right mouse click menu
        //    {
        //        ContextMenuStrip menu = new ContextMenuStrip();
        //        ToolStripMenuItem menuItem = null;

        //        // if it is a Model or ModelSelector,  allow for it be inserted under a new
        //        // ModelSelector.  Selectors under Selectors are allowed.
        //        if (treeModel.SelectedNode.Index == 0)
        //        {
        //            menuItem = new ToolStripMenuItem("Insert under new Model Selector parent");
        //            menuItem.Name = nodeID;
        //            menuItem.Tag = parentNodeID;
        //            menuItem.Click += new EventHandler(menuInsertNewModelSelectorParent_Click);
        //            menu.Items.Add(menuItem);

        //            // seperator
        //            menu.Items.Add(new ToolStripSeparator());
        //        }

        //        switch (typeName)
        //        {
        //            case "Model":
        //                menuItem = new ToolStripMenuItem("Add Appearance");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddAppearance_Click);
        //                menu.Items.Add(menuItem);

        //                // seperator
        //                menu.Items.Add(new ToolStripSeparator());

        //                menuItem = new ToolStripMenuItem("Add Actor3d");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddActor3d_Click);
        //                menu.Items.Add(menuItem);

        //                menuItem = new ToolStripMenuItem("Add Mesh3d");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddMesh3d_Click);
        //                menu.Items.Add(menuItem);

        //                menuItem = new ToolStripMenuItem("Add Billboard");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddBillboard_Click);
        //                menu.Items.Add(menuItem);

        //                // seperator
        //                menu.Items.Add(new ToolStripSeparator());
                                                
        //                menuItem = new ToolStripMenuItem("Add Particle System");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddParticleSystem_Click);
        //                menu.Items.Add(menuItem);

        //                // seperator
        //                menu.Items.Add(new ToolStripSeparator());

        //                menuItem = new ToolStripMenuItem("Delete");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
        //                menu.Items.Add(menuItem);
        //                break;

        //            case "ModelSelector":
        //                menuItem = new ToolStripMenuItem("Add Model");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddModel_Click);
        //                menu.Items.Add(menuItem);
                                               
        //                menuItem = new ToolStripMenuItem("Add Model Selector");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddModelSelector_Click);
        //                menu.Items.Add(menuItem);

        //                // seperator
        //                menu.Items.Add(new ToolStripSeparator());

        //                menuItem = new ToolStripMenuItem("Delete");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
        //                menu.Items.Add(menuItem);
        //                break;
                
        //            case "Actor3d":
        //            case "Mesh3d":
        //            case "Billboard":
        //            case "ParticleSystem": // TODO: actually can't this have children? Emitters and Attractors?
        //                // geometry types cannot have children, but can be deleted
        //                menuItem = new ToolStripMenuItem("Delete");
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Name = nodeID;
        //                menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
        //                menu.Items.Add(menuItem);
        //                break;

        //            case "Appearance":
        //            case "DefaultAppearance":
        //            case "SplatAppearance":
        //            case "GroupAttribute":
        //                PopupAppearanceMenu(nodeID, new Point(e.X, e.Y));
        //                return;
        //            case "TextureCycle":
        //                menuItem = new ToolStripMenuItem("Add Texture");
        //                menuItem.Name = nodeID;
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Click += new EventHandler(menuAddTextureCycleTexture_Click);
        //                menu.Items.Add(menuItem);

        //                menuItem = new ToolStripMenuItem("Delete");
        //                menuItem.Tag = parentNodeID;
        //                menuItem.Name = nodeID;
        //                menuItem.Click += new EventHandler(Delete_Click); // Delete_Click() is protected function in BasePluginCtl.cs
        //                menu.Items.Add(menuItem);
        //                break;
        //            default:
        //                throw new Exception("Unexpected type '" + typeName + "'");
        //        }

        //        menu.Show(treeModel, e.X, e.Y);
        //    }
        //    else if (e.Button == MouseButtons.Left) // selected a group from the list
        //    {
        //        switch (typeName)
        //        {
        //            case "ModelSelector":
        //                superTabControl.SelectedTab = superTabItem5;
        //                // is this obsolete now that Mesh3d is clickable from treeModel?
        //                // note: one issue with having geometry type set from ModelSelector is
        //                // that if the model is deleted, we now have to update this grid...
        //                // 
        //                break;
        //            case "Model":
        //                // TODO: Model should have a drop down for GeometryType _if_ under a Selector
        //                // or well.. hm... 
        //                // TODO: superTabItem6 should have a reference to the geometry
        //                // resource so we can drag and drop and change it or do something such as
        //                // change it's level of detail.
        //                // TODO: what about the CSGPunch and CSGStencil nodes?
        //                superTabControl.SelectedTab =  superTabItem6;
        //                mPosScaleRotationTarget = nodeID;
        //                PopulateModelPanel(parentNodeID, nodeID, typeName);
        //                break;

        //            // for Material, Diffuse, etc we show the appearance cards still
        //            // but for nodeID we have to use the parent Appearance/GroupAttribute
        //            case "Appearance":
        //            case "SplatAppearance":
        //            case "DefaultAppearance": 
        //            case "GroupAttribute":
        //                superTabControl.SelectedTab = superTabItem2;
        //                CreateAppearanceCards(nodeID);
        //                break;

        //            // NOTE: the following types are excluded from appearing in the treeviewModel
        //            // so will never actually be clicked.
        //            case "Material":
        //            case "Diffuse": // etc layer types
        //            case "Texture":
        //                break;

        //            case "Actor3d":
        //            case "Mesh3d":
        //                PopulateGeometryPanel(parentNodeID, nodeID, typeName);
        //                // nodeID is cached in the superTabItem4.Tag
        //                superTabControl.SelectedTab = superTabItem4;
                        
        //                // show a card that is similar to the one we show for Model
        //                // except we add options to re-save in archive as new format, export, re-center,
        //                // lod tools, rotate and re-save, generate hulls 
        //                break;

        //            case "Billboard":
        //            case "ParticleSystem":
        //                break;
        //        }
        //    }
        //}
        #endregion

        private void menuAddModel_Click(object sender, EventArgs e)
        {
            // TODO: should the user be prompted to drag and drop a mesh/actor/billboard
            // from the archive?  or at the very least, pop up the import dialog
            // and import without creating the entity wrapper around it...
            CreateNode("Model", ((ToolStripMenuItem)sender).Name);
        }
        
        private void menuAddModelSelector_Click(object sender, EventArgs e)
        {
            CreateNode("ModelSelector", ((ToolStripMenuItem)sender).Name);
        }

        private void menuInsertNewModelSelectorParent_Click(object sender, EventArgs e)
        {
            // menuAddModelSelector_Click
            // TODO: Perhaps this is a type of command that should not be allowed.
            // what we want is to create a ModelSelector and then to move the model
            // onto it.  However, how do we get the modelSelector into the scene... if 
            // under a different entity then it too later has to be moved to the desired Entity.
            // Although maybe we wouldn't have to do that if the ModelSelector is created
            // and then it's ref count artificially incremented.  
            // 1) server instructs us to remove the Model
            //    a) server side the model is removed from parent and added to new modelselector
            //      - even though server does not add the modelselector to the parent yet (but does
            //      add the model to the modelselector) there is no need to refcount artificially
            //      because the modelselector will not be removed from Repository until it's
            //      added to the scene and then remove, or it's artifically ref counted up and then ref counted down.
            // 
            // 2) server instructs us to create a node starting with the modelselector
            //    and the model now already moved under it for us
            string currentNode = ((ToolStripMenuItem)sender).Name;
            string parent = ((ToolStripMenuItem)sender).Tag.ToString();
            InsertUnderNewNode("ModelSelector", parent, currentNode);
        }


        private void menuAddGroupAppearances_Click(object sender, EventArgs e)
        {
            // if a default appearance does not exist, it must be created first
            // if a geometry does not exist, we cannot create group appearances because
            // there are no groups
            // if group appearances already exist, they must be deleted
            // TODO: when deleting a single group appearance, all must deleted
            // TODO: i think our rule is
            //  1) A DefaultAppearance must be created and whether GroupAttributes are generated
            //     or not must be done here.
            //  2) Deleting GroupAttributes requires deleting of the entire DefaultAppearance
            //  3) Individual GroupAttributes cannot be deleted or removed
            //  4) if an existing DefaultAppearance exists and you want to add GroupAttributes
            //     a new DefaultAppearance must be created.  Our plugin can specify a messagebox
            //     telling the user that the existing Default must be deleted.  

            string currentNode = ((ToolStripMenuItem)sender).Name;
            if (ChildOfTypeAlreadyExists(currentNode, new string[] {"DefaultAppearance", "SplatAppearance"}))
            {
                MessageBox.Show("A child of type 'Appearance' already exists under this parent.  You must delete the existing 'Appearance' node first before replacing it with another type of 'Appearance' node.");
                return;
            }

            // this type of GroupAppearance requires specifically Mesh3d or Actor3d (not even Billboard)
            // because a Billboard i believe is always a quad.)
            // So we must first check that Mesh3d or Actor3d exists as child.
            if (ChildOfTypeAlreadyExists(currentNode, new string[] { "Mesh3d", "Actor3d" }))
            {
                // TODO: how do we specify here a DefaultAppearance along with GroupAttributes
                // for every corresponding group in the Mesh3d or Actor3d?  The Mesh/Actor must also
                // already be loaded.
                // TODO: if the mesh/actor is deleted, the group attributes should all be removed.
                // TODO: what if we specify "DefaultAppearance[]" as the typename and this can be
                // a signal that we should look for Mesh3d or Actor3d and generate a GroupAttribute
                // child for each corresponding Group in the Mesh3d or Actor3d.
                CreateNode("DefaultAppearance[]", ((ToolStripMenuItem)sender).Name);
            }
            else 
            {
                MessageBox.Show("A child of type 'Mesh3d' or 'Actor3d' must exist under the parent Model before GroupAttribute nodes can be generated to correspond with every group in the Mesh3d or Actor3d.");
                return;
            }
        }

        private void menuAddAppearance_Click(object sender, EventArgs e)
        {
            string currentNode = ((ToolStripMenuItem)sender).Name;
            if (ChildOfTypeAlreadyExists(currentNode, new string[] { "DefaultAppearance", "SplatAppearance" }))
            {
                MessageBox.Show("A child of type 'Appearance' already exists under this parent.  You must delete the existing 'Appearance' node first before replacing it with another type of 'Appearance' node.");
                return;
            }
            // TODO: when adding an appearance, there must first exist a geometry
            // TODO: when adding an appearance, if multiple groups, they must result in GroupAttributes created for each
            // TODO: when deleting a geometry, the appearance should be deleted as well
            // TODO: individual GroupAttributes cannot be deleted
            CreateNode("DefaultAppearance", ((ToolStripMenuItem)sender).Name);
        }

        private void menuAddMesh3d_Click(object sender, EventArgs e)
        {
            // Landscape - this has to be an Entity type since Landscape is an entity...
            //           - I think Landscape should be an overall Entity that holds all Landscape models though.
            //           - hrm... TODO: i dont know.  i dont have to worry about this in Version 1.0

            string currentNode = ((ToolStripMenuItem)sender).Name;
            if (ChildOfTypeAlreadyExists(currentNode, new string[] { "Mesh3d", "Actor3d", "Billboard", "ParticleSystem" }))
            {
                MessageBox.Show("A child of type 'Geometry' already exists under this parent.  You must delete the existing 'Geometry' node first before replacing it with another type of 'Geometry' node.");
                return;
            }

            string fileDialogFilter = "Meshess|*.obj;*.x;*.tvm";
            BrowseNewResource("Mesh3d", ((ToolStripMenuItem)sender).Name, fileDialogFilter);
        }

        private void menuAddActor3d_Click(object sender, EventArgs e)
        {
            string currentNode = ((ToolStripMenuItem)sender).Name;
            if (ChildOfTypeAlreadyExists(currentNode, new string[] { "Mesh3d", "Actor3d", "Billboard", "ParticleSystem" }))
            {
                MessageBox.Show("A child of type 'Geometry' already exists under this parent.  You must delete the existing 'Geometry' node first before replacing it with another type of 'Geometry' node.");
                return;
            }

            string fileDialogFilter = "Actors|*.tva;*.x";
            BrowseNewResource("Actor3d", ((ToolStripMenuItem)sender).Name, fileDialogFilter);
        }

        private void menuAddBillboard_Click(object sender, EventArgs e)
        {
            string currentNode = ((ToolStripMenuItem)sender).Name;
            if (ChildOfTypeAlreadyExists(currentNode, new string[] { "Mesh3d", "Actor3d", "Billboard", "ParticleSystem" }))
            {
                MessageBox.Show("A child of type 'Geometry' already exists under this parent.  You must delete the existing 'Geometry' node first before replacing it with another type of 'Geometry' node.");
                return;
            }

            string fileDialogFilter = null;
            BrowseNewResource("Billboard", ((ToolStripMenuItem)sender).Name, null);
        }



        private void menuGroupAttributeMoveUp_Click(object sender, EventArgs e)
        { 
            // this should send to the parent node 
            string child = ((ToolStripMenuItem)sender).Name;
            string parent = ((ToolStripMenuItem)sender).Tag.ToString();

            if (mHost != null)
                mHost.Node_MoveChildOrder(parent, child, false);
        }

        private void menuGroupAttributeMoveDown_Click(object sender, EventArgs e)
        {
            string child = ((ToolStripMenuItem)sender).Name;
            string parent = ((ToolStripMenuItem)sender).Tag.ToString();

            if (mHost != null)
                mHost.Node_MoveChildOrder(parent, child, true);
        }

        private void menuAddTextureCycleTexture_Click(object sender, EventArgs e)
        {
            string fileDialogFilter = "Images|*.dds;*.png;*.bmp;*.jpg;*.tga;*.gif";
            BrowseNewResource("Texture", ((ToolStripMenuItem)sender).Name, fileDialogFilter);
        }

        private void buttonMoveUp_Click(object sender, EventArgs e)
        {

        }

        private void buttonMoveDown_Click(object sender, EventArgs e)
        {

        }

        private bool ChildOfTypeAlreadyExists(string parentID, string[] existingChildTypeName)
        {
            if (existingChildTypeName == null || existingChildTypeName.Length == 0) return false;

            string[] childIDs;
            string[] childTypes;

            mHost.Node_GetChildrenInfo(parentID, null, out childIDs, out childTypes);

            if (childIDs != null && childIDs.Length > 0)
            {
                for (int i = 0; i < childIDs.Length; i++)
                {
                    string nodeType = childTypes[i];
                    string nodeID = childIDs[i];

                    // TODO: problem here is, it'd be better to check against
                    // an array of types because we're comparing strings here and not actual
                    // types and so we can't look for a generic "Geometry" type, must be
                    // "Mesh3d" and "Actor3d" and "Terrain" etc
                    for (int j = 0; j < existingChildTypeName.Length; j++)
                        if (nodeType == existingChildTypeName[j]) return true;
                }
            }

            return false;
        }

        private const string GT_GEOMETRY = "Geometry";
        private const string GT_STENCIL = "CSGStencil";
        private const string GT_COLLIDER = "Collider";
        SourceGrid.Cells.Editors.ComboBox mEditorComboBox;
        ComboPropertyController mGeometryTypeController;
        //private void InitializeGeometryGrid()
        //{

        //    gridSwitchNode.SelectionMode = SourceGrid.GridSelectionMode.Cell;
            
        //    // init editors
        //   // mEditorString = new SourceGrid.Cells.Editors.TextBox(typeof(string));
        //   // mEditorFloat = new SourceGrid.Cells.Editors.TextBox(typeof(float));
        //   // mEditorNumericFloat = new SourceGrid.Cells.Editors.NumericUpDown(typeof(int), 2500, 0, 1);
        //   // mEditorVector = new SourceGrid.Cells.Editors.TextBox(typeof(Keystone.Types.Vector3d));
        //    mEditorComboBox = new SourceGrid.Cells.Editors.ComboBox(typeof(string), new List<string>(){GT_GEOMETRY, GT_STENCIL, GT_COLLIDER}, true);
        //    mEditorComboBox.EditableMode = SourceGrid.EditableMode.SingleClick | SourceGrid.EditableMode.Focus;

        //    // init controllers
        //    mGeometryTypeController = new ComboPropertyController(mHost, "switchmodes");
        //    // mForceTransitionController = new FloatPropertyController(mHost, "forcetransition");
        //    // mSwitchDistanceController = new VectorPropertyController(mHost, "switchdistances");
           

        //    gridSwitchNode.Redim(1, GEOM_NUM_COLUMNS); // just a single header for now
        //    gridSwitchNode.SelectionMode = SourceGrid.GridSelectionMode.Row;
        //    //gridSwitchNode.MinimumWidth = 50
        //    //gridSwitchNode.AutoSizeCells() ' <-- this makes the heights of each row insanely huge?  Why?

        //    for (int i = 0; i < GEOM_NUM_COLUMNS; i++)
        //        gridSwitchNode.Columns[i].AutoSizeMode = SourceGrid.AutoSizeMode.EnableAutoSize | SourceGrid.AutoSizeMode.EnableStretch;


        //    gridSwitchNode.AutoStretchColumnsToFitWidth = true;
        //    //gridSwitchNode.AutoStretchRowsToFitHeight = true;
        //    //gridSwitchNode.Columns.StretchToFit(); // <-- this evenly stretches all columns width's to fill the full width of the grid's client area
        //    //gridSwitchNode.Rows.StretchToFit();  // <-- this evenly stretches all rows heights to fill the full height of the grid's client area


        //    // configure the header row
        //    SourceGrid.Cells.Views.ColumnHeader viewColumnHeader = new SourceGrid.Cells.Views.ColumnHeader();
        //    DevAge.Drawing.VisualElements.ColumnHeader backHeader = new DevAge.Drawing.VisualElements.ColumnHeader();
        //    backHeader.BackColor = System.Drawing.Color.Maroon;
        //    backHeader.Border = DevAge.Drawing.RectangleBorder.NoBorder;

        //    viewColumnHeader.Background = backHeader;
        //    viewColumnHeader.ForeColor = System.Drawing.Color.White;
        //    viewColumnHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 12);
        //    viewColumnHeader.TextAlignment = DevAge.Drawing.ContentAlignment.MiddleCenter;
        //    //mCaptionModel = New SourceGrid.Cells.Views.Cell();
        //    //mCaptionModel.BackColor = gridClients.BackColor;



        //    // build the headers

        //    gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_NAME] = new SourceGrid.Cells.Cell("name");
        //    gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_NAME].View = viewColumnHeader;
        //    gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_NAME].ColumnSpan = 1;

        //    gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_GEOMETRY_TYPE] = new SourceGrid.Cells.Cell("geometry type");
        //    gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_GEOMETRY_TYPE].View = viewColumnHeader;
        //    gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_GEOMETRY_TYPE].ColumnSpan = 1;

        //    //gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_FORCE_TRANSITION] = new SourceGrid.Cells.Cell("force transition");
        //    //gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_FORCE_TRANSITION].View = viewColumnHeader;
        //    //gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_FORCE_TRANSITION].ColumnSpan = 1;

        //    //gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_SWITCH_DISTANCE] = new SourceGrid.Cells.Cell("Start");
        //    //gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_SWITCH_DISTANCE].View = viewColumnHeader;
        //    //gridSwitchNode[GEOM_ROW_HEADER, GEOM_COLUMN_SWITCH_DISTANCE].ColumnSpan = 1;

            

        //}

        //private int AddGeometryRow(string typeName)
        //{
        //    int prevCount = gridSwitchNode.RowsCount;
        //    int newCount = prevCount + 1;
        //    gridSwitchNode.Redim(newCount, GEOM_NUM_COLUMNS);

        //    int rowIndex = newCount - 1; // 0 based means our actual row is one less than count
        //    gridSwitchNode[rowIndex, GEOM_COLUMN_NAME] = new SourceGrid.Cells.Cell("...");
        // //   gridSwitchNode[rowIndex, GEOM_COLUMN_NAME].Column.Width = 25;

        //    gridSwitchNode[rowIndex, GEOM_COLUMN_GEOMETRY_TYPE] = new SourceGrid.Cells.Cell("...");
        //    gridSwitchNode[rowIndex, GEOM_COLUMN_GEOMETRY_TYPE].Editor =  mEditorComboBox;
        //    gridSwitchNode[rowIndex, GEOM_COLUMN_GEOMETRY_TYPE].AddController(mGeometryTypeController);
        ////    gridSwitchNode[rowIndex, GEOM_COLUMN_GEOMETRY_TYPE].Column.Width = 25;

        //    switch (typeName)
        //    {
        //        case "ModelSwitch":
        //            // no extra fields
        //            break;
        //        case "ModelLODSwitch":
        //            //gridSwitchNode[rowIndex, GEOM_COLUMN_FORCE_TRANSITION] = new SourceGrid.Cells.Cell("...");
        //            //gridSwitchNode[rowIndex, GEOM_COLUMN_FORCE_TRANSITION].Editor = null;// no editing of geometry name, but LOD perhaps we should
        //            //gridSwitchNode[rowIndex, GEOM_COLUMN_FORCE_TRANSITION].AddController(mForceTransitionController);

        //            //gridSwitchNode[rowIndex, GEOM_COLUMN_SWITCH_DISTANCE] = new SourceGrid.Cells.Cell("...");
        //            //gridSwitchNode[rowIndex, GEOM_COLUMN_SWITCH_DISTANCE].Editor = null;// no editing of geometry name, but LOD perhaps we should
        //            //gridSwitchNode[rowIndex, GEOM_COLUMN_SWITCH_DISTANCE].AddController(mSwitchDistanceController);
        //            break;
        //    }

        //    return rowIndex;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="rowIndex">0 based index for the row in the grid to update</param>
        ///// <param name="id"></param>
        ///// <param name="name"></param>
        ///// <param name="startFrame"></param>
        ///// <param name="endFrame"></param>
        ///// <param name="speed"></param>
        ///// <param name="reverse"></param>
        ///// <param name="length"></param>
        ///// <param name="numFrames"></param>
        //private void UpdateGeometryGridRowValue(int rowIndex, string geometryName, string parentID, string geometryMode)
        //{
        //    if (rowIndex == GEOM_ROW_HEADER) return;

        //    gridSwitchNode[rowIndex, GEOM_COLUMN_NAME].Value = geometryName;

        //    string[] csv = geometryMode.Split(new string[] { "," }, StringSplitOptions.None);
        //    string result = "";
        //    switch (csv[rowIndex - 1])
        //    {
        //        case "0":
        //            result = GT_GEOMETRY.ToString();
        //            break;
        //        case "1":
        //            result = GT_STENCIL.ToString();
        //            break;
        //        case "2" :
        //            result = GT_COLLIDER.ToString();
        //            break;
        //        default:
        //            throw new Exception("Unsupported geometry type");
        //    }

        //    gridSwitchNode[rowIndex, GEOM_COLUMN_GEOMETRY_TYPE].Value = result;
        //    gridSwitchNode[rowIndex, GEOM_COLUMN_GEOMETRY_TYPE].Tag = parentID;
        //  //  gridSwitchNode[rowIndex, GEOM_COLUMN_FORCE_TRANSITION].Value = forceTransition;
        //  //  gridSwitchNode[rowIndex, GEOM_COLUMN_SWITCH_DISTANCE].Value = switchDistance;
        //}


        //private void ReleaseGeometryGrid()
        //{
        //    try
        //    {
        //        for (int i = 0; i < gridSwitchNode.RowsCount; i++)
        //            for (int j = 0; j < GEOM_NUM_COLUMNS; j++)
        //                gridSwitchNode[i, j] = null;

        //        // delete all rows from bottom up
        //        for (int j = gridSwitchNode.RowsCount - 1; j >= 0; j--)
        //            gridSwitchNode.Rows.Remove(j);
        //    }
        //    catch { }
        //}


        #region Custom Cell Edit Controllers
        // TODO: this could maybe made more generic if i pass in a delegate to be called
        // for OnValueChanged
        private class ComboPropertyController : SourceGrid.Cells.Controllers.ControllerBase
        {
            protected IPluginHost mHost;
            protected string mPropertyName;

            public ComboPropertyController(IPluginHost host, string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException();
                mHost = host;
                mPropertyName = propertyName;
            }

            public override void OnValueChanged(SourceGrid.CellContext sender, EventArgs e)
            {
                base.OnValueChanged(sender, e);

                int row = sender.Position.Row;
                int childIndex = row - 1; // subtract one for the header row
                SourceGrid.Grid grid = (SourceGrid.Grid)sender.Grid;
                string id = (string)grid[row, GEOM_COLUMN_GEOMETRY_TYPE].Tag;

                // value needs to include the index as well as the drop down selection
                // parentID, switchModes, typeof(string), 

                if (mHost != null && (string.IsNullOrEmpty(id) == false))
                {
                    // I think the only way to change the property value using our
                    // standard Node_ChangeProperty is to simply return the entire comma seperated values
                    // string which represents all of the child nodes.
                    string tmp = (string)mHost.Node_GetProperty(id, "switchmodes");
                    // technically i dont think i need to do this, I should be able to
                    // iterate through every row in the grid to create this new value
                    string[] csv = new string[grid.RowsCount - 1];

                    for (int i = 1; i < grid.RowsCount; i++) // start at index 1 to skip header
                    {
                        string type = (string)grid[i, GEOM_COLUMN_GEOMETRY_TYPE].Value;
                        switch (type)
                        {
                            case GT_GEOMETRY:
                                csv[i-1] = "0";
                                break;
                            case GT_STENCIL :
                                csv[i-1] = "1";
                                break;
                            case GT_COLLIDER :
                                csv[i-1] = "2";
                                break;
                            default:
                                throw new Exception();

                        }
                    }


                    string result = String.Join(",", csv);
                    mHost.Node_ChangeProperty(id, mPropertyName, typeof(string), result);
                }
            }
        }
        #endregion


        
    }
}