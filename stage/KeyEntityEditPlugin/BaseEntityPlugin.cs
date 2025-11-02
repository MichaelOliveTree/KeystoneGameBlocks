using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KeyPlugins;
using Keystone.Types;

namespace KeyPlugins
{
    public partial class BaseEntityPlugin : BasePluginCtl
    {

        private struct SubPanel
        {
            string Name;
            DevComponents.DotNetBar.SuperTabItem SuperTabItem;

            void IncrementalUpdate(Settings.PropertySpec[] changedProperties)
            {
                //

            }
        }

        protected struct TabPanelNode
        {
            public string ID;
            public string Typename;

        }

        protected TabPanelNode[] mTabPanelNodes;
        protected TreeView mFullTree;

        protected delegate void NodeCreated2(DevComponents.AdvTree.AdvTree tvw, DevComponents.AdvTree.Node parentNode, DevComponents.AdvTree.Node childNode, string childTypeName);



        public BaseEntityPlugin()
            : base()
        {
            InitializeComponent();

            // zones - uneditable positions, rotations, scales
            //    - interiors are moved via their exterior ship connection
            //
            // lights
            // invisible entities like triggers/sensors  <-- think of all these as flags?  
            // entities with visual representation       <-- as in no seperate classes just flags?
            // entities with physical representation     <-- and the different property pages show up
            //                                           <-- based on the flags set?
            //                                           No, there's too many permutations.
            //                                           It's best to go by typename
            //                                           However it would be nice if we could
            //                                           have the plugin register for specific
            //                                           typenames rather than to simply add them
            //                                           to a dictionary under the single plugin.mTargetType
            //                                           
            //                                           
            // 
            // boned actors
            // animated mesh hierarchies (tank + turret)
            // 
        }

        public BaseEntityPlugin(IPluginHost host, string modspath, string modname)
            : base(host, modspath, modname)
        {
            InitializeComponent();


            propGridDomainObject.PropertyValueChanging += OnPropGridDomainObject_PropertyValueChanging;

            mName = "Keystone Default Entity Editor Plugin";
            mSupportedTypenames = new string[] { "Entity", "ModeledEntity", "Star", "World", "Planet", "Moon", "Vehicle", "Light", "DirectionalLight", "SpotLight", "PointLight" };
            mDescription = "A control for configuring entities.";

            treeModel.MarkupLinkClick += OnTreeModel_BlendLink_Clicked;
            treeModel.NodeClick += OnTreeModel_NodeClick2;
            treeModel.AfterCheck += OnTreeModel_NodeChecked2;

            //vecEditCardRotation.VectorChanged += vecEditRotation_OnChange;
            //vecEditCardRotation.Text = "Rotation";
            //vecEditCardPosition.VectorChanged += vecEditPosition_OnChange;
            //vecEditCardPosition.Text = "Position";
            //vecEditCardScale.VectorChanged += vecEditScale_OnChange;
            //vecEditCardScale.Text = "Scale";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //treeModel = new TreeView(); // to hide checkboxes, treeview must be created AFTER form loaded, 
            //// and NOT in form designer IntializeControls
            //treeModel.CheckBoxes = true;
            //treeModel.Dock = DockStyle.Fill;
            //treeModel.AllowDrop = true;
            //this.treeModel.Location = new System.Drawing.Point(0, 56);
            //this.treeModel.Name = "treeModel";
            //this.treeModel.Size = new System.Drawing.Size(209, 315);
            //this.treeModel.TabIndex = 20;
            //this.navigationPanePanel3.Controls.Add(this.treeModel);

            //treeBehavior = new TreeView();
            //treeBehavior.CheckBoxes = true;


            //this.treeBehavior.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.treeBehavior.Location = new System.Drawing.Point(0, 0);
            //this.treeBehavior.Name = "treeBehavior";
            //this.treeBehavior.Size = new System.Drawing.Size(956, 409);
            //this.treeBehavior.TabIndex = 0;

            //this.superTabControlPanel7.Controls.Add(this.treeBehavior);
        }

        protected void AutoArrangePanelCards(DevComponents.DotNetBar.SuperTabControlPanel panel)
        {
            System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.AutoArrangePanelCards() - Begin.");
            // when adding a new control, find it's final location based on existing cards
            int left = 5;
            if (panel.Controls != null)
                foreach (Control c in panel.Controls)
                {
                    if (c is NotecardBase == false) continue;
                    c.Top = 40; // the card tops are aligned to be below the lightMode combobox
                    c.Left = left;
                    left += c.Width;
                }
            // then animate the control to slide from out of view to in view until it's reached
            // it's final location

            // if the animation takes longer than 1 second,w e'll just snap it
            // notify when we've stopped animating so we can stop redrawing
            System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.AutoArrangePanelCards() - Complete.");
        }


        public override void SelectTarget(string sceneName, string id, string typeName)
        {

            System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.SelectTarget() - Begin.");
            //this.SuspendLayout();
            //superTabItem5.SuspendLayout = true;
            //superTabItem2.SuspendLayout = true;
            //superTabItem8.SuspendLayout = true;
            //superTabItem1.SuspendLayout = true;
            //superTabItem3.SuspendLayout = true;
            //superTabItem7.SuspendLayout = true;

            try
            {
                //TODO: is there a cross threading issue here with updating gui components in plugin
                // on selecttarget?
                base.SelectTarget(sceneName, id, typeName);

                // generate full tree from this entity, but stopping at any child entity nodes
                //                TreeNode rootNode = GenerateTreeNode("", id, typeName, null, true);
                //               if (rootNode == null)
                //               {
                //                   System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                //               }
                //               else
                //               {
                ////                   mFullTree = new TreeView();
                ////                   mFullTree.SuspendLayout();
                ////                   mFullTree.Nodes.Add(rootNode);
                ////                   mFullTree.ResumeLayout();

                //                   // TODO: For now we only implement memory over the current node
                //                   // If we want to implement persistance as the user navigates different entities
                //                   // then we would store their panelstate instances collections by Entity ID.
                //                   //
                //                   //
                //                   // Stack <Dictionary<string, PanelState>> mStateStack;  // maintains stack across navigating child entities
                //                   //
                //                   // Dictionary<string, PanelState> mPanelStates; // stores each panel with a key
                //                   //                                              // such as Model, Appearance, Behavior

                //                   // 
                //                   // class PanelState
                //                   // {
                //                   //      SuperTabItem mTabItem;
                //                   //      DevComponents.AdvTree.AdvTree mTreeView;
                //                   //      DevComponents.AdvTree.Node mSelected; 
                //                   //      Dictionary <string, object> mUserStates; // a collection of other state info that the user can manually decide what is tracked under what key name
                //                   //      public delegate void RefreshDelegate();
                //                   //      private RefreshDelegate mRefreshHandler;
                //                   //      public PanelState (RefreshDelegate refreshHandler)
                //                   //      {
                //                   //          mRefreshHandler = refreshHandler;
                //                   //      }
                //                   //
                //                   //      public void Refresh()
                //                   //      {
                //                   //          // 
                //                   //          if (mRefreshHandler != null) mRefreshHandler.Invoke();
                //                   //      }
                //                   // }


                // TODO: Why after selecting a tab, the individual panels no longer
                // maintain their layout locks to top/left/bottom/right and so dont resize
                // properly upon changing the window dimensions of either plugin or main window

//                this.buttonGeneral.RaiseClick();
                //                   // TODO: is the collection of panel states for this mTargetNodeID cached in our
                //                   // collection of previously visited entities?  If so retreive it and restore it.
                //                   // (we would have to update it with any changes to it's fulltree state however since
                //                   // while cached some nodes may have been added/removed)
                //                   //
                //                   // whenever we enter a panel, that wasn't previously the panel, clear it first
                //                   // but also any navigation pane control such as advTreeModel which is shared with both
                //                   // the appearance card tabItem and geometry and lod tabitems
                mPosScaleRotationTarget = mTargetNodeID;
                //                   // intiial state.  Later they will be triggered on Notify_NodeRemoved
                ClearAppearanceCardsPanel(superTabControlPanel2);
                ClearDomainObjectPanel();
                ClearModelPanel();
                ClearBehaviorPanel();
                ClearAnimationsPanel();
                PopulateAnimationTree(mTargetNodeID);
                PopulateAnimationCombobBox(mTargetNodeID);

                //ClearPhysicsPanel();
                //ClearParticleSystemsPanel();
                //Lights

                // contains just the sub panel since the "General" is always there
                // although potentially with some hidden options
                mTabPanelNodes = GetTabNodes(mTargetNodeID);

                if (mTabPanelNodes != null && mTabPanelNodes.Length > 0)
                    for (int i = 0; i < mTabPanelNodes.Length; i++)
                        RebuildPanel(mTabPanelNodes[i]);

                System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.SelectTarget() - OK.");
                //               }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.SelectTarget() - Exception " + ex.Message);
            }
            finally
            {
                //superTabItem5.SuspendLayout = false;
                //superTabItem2.SuspendLayout = false;
                //superTabItem8.SuspendLayout = false;
                //superTabItem1.SuspendLayout = false;
                //superTabItem3.SuspendLayout = false;
                //superTabItem7.SuspendLayout = false;
                //this.ResumeLayout();
                //this.PerformLayout();

            }

            System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.SelectTarget() - End.");
        }

        // Model tab always goes to the root model or lod and NOT any
        // child models.  It is meant as overal visual model and not any 
        // one specific model.  Thus when viewing the visual model, the entire
        // tree is always visible, not just a subsection.  Thus treeModel always
        // starts with the root Model or Switch/Sequence node.
        // - there are several panels within the VisualModel state.
        //   - lod generation panel
        //      - when LOD style is selected for the ModelSelector node
        //   - mesh statistics
        //   - appearance cards
        //   - 
        private void buttonModel_Click(object sender, EventArgs e)
        {
            // TODO: These suspend/resume blocks seem critical in preventing flood of messages
            // that will freeze our peekmessage loop in AppMain. 
            // I must eventually go through our plugin and by systematic in funneling
            // all plugin gui updates through suspend blocks for each panel
            //this.SuspendLayout();
            //superTabControl.SuspendLayout();
            //superTabItem5.SuspendLayout = true;
            superTab.SelectedTab = superTabItem5;  // allows showing of 
            //superTabItem5.SuspendLayout = false;
            //superTabControl.ResumeLayout();
            //this.ResumeLayout();
        }

        private void buttonAnimations_Click(object sender, EventArgs e)
        {
            //this.SuspendLayout();
            //superTabControl.SuspendLayout();
            //superTabItem8.SuspendLayout = true;
            superTab.SelectedTab = superTabItem8;
            //superTabItem8.SuspendLayout = false;
            //superTabControl.ResumeLayout();
            //this.ResumeLayout();
        }

        private void buttonGeneral_Click(object sender, EventArgs e)
        {
            //this.SuspendLayout();
            //superTabControl.SuspendLayout();
            //superTabItem1.SuspendLayout = true;
            if (mTargetTypename == "DirectionalLight" || mTargetTypename == "PointLight" || mTargetTypename == "SpotLight")
            {
                PopulateLightTab(mTargetNodeID);
                superTab.SelectedTab = superTabItem10;
            }
            else
            {
                superTab.SelectedTab = superTabItem1;
                PopulateGeneralTab(mPosScaleRotationTarget);
            }
            //superTabItem1.SuspendLayout = false;
            //superTabControl.ResumeLayout();
            //this.ResumeLayout();

        }

        private void buttonDomainObject_Click(object sender, EventArgs e)
        {
            //this.SuspendLayout();
            //superTabControl.SuspendLayout();
            //superTabItem3.SuspendLayout = true;
            superTab.SelectedTab = superTabItem3;
            //superTabItem3.SuspendLayout = false;
            //superTabControl.ResumeLayout();
            //this.ResumeLayout();
        }

        private void buttonBehavior_Click(object sender, EventArgs e)
        {
            //this.SuspendLayout();
            //superTabControl.SuspendLayout();
            //superTabItem7.SuspendLayout = true;
            superTab.SelectedTab = superTabItem7;
            //superTabItem7.SuspendLayout = false;
            //superTabControl.ResumeLayout();
            //this.ResumeLayout();
        }

        private void buttonPhysics_Click(object sender, EventArgs e)
        {
            //this.SuspendLayout();
            //superTabControl.SuspendLayout();
            //superTabItem9.SuspendLayout = true;
            superTab.SelectedTab = superTabItem9;
            //superTabItem9.SuspendLayout = false;
            //superTabControl.ResumeLayout();
            //this.ResumeLayout();
        }



        protected DevComponents.AdvTree.Node GenerateTreeBranch2(DevComponents.AdvTree.AdvTree treeview, DevComponents.AdvTree.Node parentNode, string parentID, string childID, string childTypename, string[] filteredTypes, bool recurse, NodeCreated2 nodeCreatedCallBack)
        {
            DevComponents.AdvTree.Node childNode = CreateTreeNode2(parentID, childID, childTypename);

            if (childNode == null)
            {
                System.Diagnostics.Debug.WriteLine("Unexpected null child tree node...");
                return null;
            }
            // TODO: every node that is created, we want to inform the IPluginHost that we want
            // to be notified if that node changes.

            if (parentNode == null)
            {
                // add as root
                treeview.Nodes.Add(childNode);
            }
            else
            {
                parentNode.Nodes.Add(childNode);
                parentNode.Expand();
                childNode.Expand();
            }

            if (nodeCreatedCallBack != null)
                nodeCreatedCallBack(treeview, parentNode, childNode, childTypename);

            // ParticleSystem Emitters and Attractors are treated differently because they are not of type NOde, but internal members of ParticleSystem Geometry just as Mesh Groups are.
            if (childTypename == "ParticleSystem")
            {
                int count = (int)mHost.Node_GetProperty(childID, "emittercount");
                int[] indices = (int[])mHost.Node_GetProperty(childID, "emitterindices");
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        DevComponents.AdvTree.Node emitterNode = new DevComponents.AdvTree.Node();
                        emitterNode.Name = "Emitter #" + indices[i].ToString();
                        emitterNode.Text = "Emitter #" + indices[i].ToString();
                        emitterNode.Tag = childID;
                        childNode.Nodes.Add(emitterNode);
                        emitterNode.Expand();
                    }
                }
                count = (int)mHost.Node_GetProperty(childID, "attractorcount");
                indices = (int[])mHost.Node_GetProperty(childID, "attractorindices");
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        DevComponents.AdvTree.Node attractorNode = new DevComponents.AdvTree.Node();
                        attractorNode.Name = "Attractor #" + indices[i].ToString();
                        attractorNode.Text = "Attractor #" + indices[i].ToString();
                        attractorNode.Tag = childID;
                        childNode.Nodes.Add(attractorNode);
                        attractorNode.Expand();
                    }
                }
            }
            else if (recurse)
            {
                // get the child nodes and add them to the tree and recurse them
                string[] childIDs;
                string[] childTypes;
                mHost.Node_GetChildrenInfo(childID, filteredTypes, out childIDs, out childTypes);

                if (childIDs != null && childIDs.Length > 0)
                {
                    for (int i = 0; i < childIDs.Length; i++)
                    {
                        string nodeType = childTypes[i];
                        string nodeID = childIDs[i];

                        // TODO: if this is the fullTreeview and
                        // it's a child Entity we should add but dont recurse
                        // if the user clicks the child entity it will set it as active in plugin?
                        // we can decide later..
                        DevComponents.AdvTree.Node treeNode = GenerateTreeBranch2(treeview, childNode, childID, nodeID, nodeType, filteredTypes, recurse, nodeCreatedCallBack);
                        if (treeNode == null)
                        {
                            System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                            continue;
                        }
                    }
                }
            }

            return childNode;
        }


        private DevComponents.AdvTree.Node CreateTreeNode2(string parentID, string nodeID, string typeName)
        {
            DevComponents.AdvTree.Node treeNode = new DevComponents.AdvTree.Node();
            treeNode.Name = nodeID;
            treeNode.Text = typeName;
            treeNode.Tag = parentID;
            treeNode.Expand();
            return treeNode;
        }

        /// <summary> 
        /// Hides the checkbox for the specified node on a TreeView control. 
        /// </summary> 
        private void HideCheckBox(TreeView tvw, TreeNode node)
        {

            TVITEM tvi = new TVITEM();
            tvi.hItem = node.Handle;
            tvi.mask = TVIF_STATE;
            tvi.stateMask = TVIS_STATEIMAGEMASK;
            tvi.state = 0;
            SendMessage(tvw.Handle, TVM_SETITEM, IntPtr.Zero, ref tvi);

            //IntPtr lparam = Marshal.AllocHGlobal(Marshal.SizeOf(tvi));
            //Marshal.StructureToPtr(tvi, lparam, false);
            //SendMessage(tvw.Handle, TVM_SETITEM, IntPtr.Zero, ref lparam);
        }



        public override void WatchedNodeCustomPropertyChanged(string id, string typeName)
        {

        }

        public override void WatchedNodePropertyChanged(string id, string typeName)
        {
            // one consideration and two steps
            // consideration
            //  - when editing,  no change is technically allowed unless allowed by the server.
            //    Thus to keep the GUI in sync (user updates his plugin gui before he discovers
            //    the property change was not allowed by the server) we need a way to refresh the gui
            //    in the case of a rejected command.  
            // 
            // two steps
            // - is the thing that changed a child ancestor of the current selected plugin entity?
            // - if no, return and do nothing.
            // - if YES, then now query the current selected "superTabControlPanel#" control
            //   and use a switch to determine if it's possible for the changed node to be even 
            //   relevant to that panel (eg. behavior changed but we're currently at the appearance superTabControlPanel#
            //   so we do nothing.
            //   If yes, then now we need to update just that panel and remember which node was changed
            //   and re-highlight or select any appearance Group, or specific behavior node, etc
            // 

            // TODO: call those specific functions for populating the current
            // superTabPanel#

            // TODO: could i perhaps put a speed limiter on some property changes like
            // position and rotation vectors?

        }

        public override void WatchedNodeAdded(string parent, string child, string typeName)
        {
            // a new node has been added to the entity itself
            if (parent == mTargetNodeID)
            {
                // this must be either a Model or an LOD/Switch/Sequence (if this is a Model related add)
            }
            else
            {

            }

            switch (typeName)
            {
                // animation needs to be added to combobox with correct server generated nodeID
                case "Animation":
                // animation clips (target needs to be assigned)
                case "KeyframeInterpolator_translation":
                case "KeyframeInterpolator_scale":
                case "KeyframeInterpolator_rotation":
                case "EllipticalAnimation":
                case "BonedAnimation":
                case "TextureAnimation":
                    Animation_Node_Created(parent, child, typeName);
                    break;
            }


            // OBSOLETE - Below code needs to be incorporated in above code where we switch based on typeName rather than the current active plugin panel

            // TODO: everytime a panel activates, i should re-pull the data
            // used to populate it.  This is ok because the data comes local
            // and not across the wire. "Gets" are always local. Only "Sets" go
            // across the wire.  This way, I only have to find the active panel, 
            // and only update relevant parts of it if it's the proper panel
            // given the added node.  Otherwise the added node does not impact the current panel

            string panelTarget = (string)superTab.SelectedTab.Tag;
            bool isDescendant = (bool)mHost.Node_HasDescendant(panelTarget, child);

            // which panel is currently active?
            switch (superTab.SelectedTab.Name)
            {
                case "superTabItem5": // "Model" is active panel.  Is the added node relevant to this panel?
                    // 1) Is this node's TYPE impactful when this panel is visible?
                    switch (typeName)
                    {
                        case "DefaultAppearance":
                            // add the node without changing the current selection in the advTreeModel
                            DevComponents.AdvTree.Node[] nodes = treeModel.Nodes.Find(parent, true);
                            if (nodes == null || nodes.Length == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                                return;
                            }
                            DevComponents.AdvTree.Node recursedNode = GenerateTreeBranch2(treeModel, nodes[0], parent, child, typeName, new string[] { "Material", "Texture", "Shader" }, true, ModelTree_NodeCreated2);
                            if (recursedNode == null)
                            {
                                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                                return;
                            }

                            // TODO: after adding the new node, do we select it?  no?
                            break;
                        // obsolete - geometry nodes are no longer added to the Model tree in Model panel
                        //case "Mesh3d":
                        //case "Billboard3d":
                        //case "Actor3d":
                        default:
                            return; // do nothing
                    }

                    // 2) Is this node descended from the parent which is root of this panel?
                    //    i.e. just because it's the appearance unit cards, doesn't mean this texture
                    //    that was added exists under this particular appearance group.

                    break;
                case "superTabItem1": // Model tree
                    switch (typeName)
                    {
                        case "DefaultAppearance": // TODO: should highlight this node by default and on the main panel, show BlendingModes, cullmode, etc
                            DevComponents.AdvTree.Node[] nodes = treeModel.Nodes.Find(parent, true);
                            if (nodes == null || nodes.Length == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                                return;
                            }
                            DevComponents.AdvTree.Node recursedNode = GenerateTreeBranch2(treeModel, nodes[0], parent, child, typeName, new string[] { "Material", "Texture", "Shader" }, true, ModelTree_NodeCreated2);
                            if (recursedNode == null)
                            {
                                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                                return;
                            }
                            break;
                        default:
                            return; // do nothing
                    }
                    break;
                case "superTabItem2": // "Appearance Cards" is active panel.  Is the added node relevant to this panel?
                    switch (typeName)
                    {
                        case "Texture":
                        case "TextureCycle":
                        case "SplatAlpha":
                        case "Diffuse":
                        case "Specular":
                        case "NormalMap":
                        case "Emissive":
                        case "VolumeTexture":
                        case "DUDVTexture":
                        case "CubeMap":
                        case "Material":
                        case "ProceduralShader": // TODO: If the shader is not loaded yet, the parameters wont be visible.  We need notification when the shader finishes pages in
                        case "Shader":
                            string appearanceID = this.treeModel.SelectedNode.Name; // 
                            CreateAppearanceCards(appearanceID);
                            break;
                        default:
                            return; // do nothing
                    }
                    break;
                case "superTabItem8": // "Animations" is active panel.  Is the added node relevant to this panel?
                    ClearAnimationGrid();
                    break;
                case "superTabItem9":
                    ClearPhysicsCardsPanel(superTabControlPanel9);
                    CreatePhysicseCards(mTargetNodeID);

                    // node added is likely a new Animation Clip, we should add

                    break;
                case "superTabItem7": // "Behavior Tree" is active panel.  Is the added node relevant to this panel?
                    switch (typeName)
                    {
                        case "Sequence":
                            if (parent == mTargetNodeID) // this is a root behavior
                                PopulateBehaviorPanel(parent, child, typeName);
                            break;
                        case "Script":
                            // add the node without changing the current selection in the advTreeModel
                            TreeNode[] nodes = this.treeBehavior.Nodes.Find(parent, true);
                            if (nodes == null || nodes.Length == 0)
                            {
                                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                                return;
                            }

                            TreeNode recursedNode = KeyPlugins.StaticControls.GenerateTreeBranch(mHost, treeBehavior, nodes[0], parent, child, typeName, null, true, null);
                            if (recursedNode == null)
                            {
                                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                                return;
                            }
                            break;
                        default:

                            break;
                    }
                    break;

                case "superTabItem3": // "DomainObject" is active panel.  Is the added node relevant to this panel?
                    PopulateDomainObjectPanel(child);
                    break;
                // case "????" : // "Physics" is active panel.  Is the added node relevant to this panel?
                // break;
                default:
                    break;
            }
        }

        public override void WatchedNodeRemoved(string parent, string child, string typeName)
        {
            // rebuild the full tree
            mFullTree = new TreeView();
            TreeNode rootNode = KeyPlugins.StaticControls.GenerateTreeBranch(mHost, mFullTree, null, "", mTargetNodeID, mTargetTypename, null, true, null);
            if (rootNode == null)
            {
                System.Diagnostics.Debug.WriteLine("Unexpected null tree node...");
                return;
            }


            // TODO: depending on below, we'll need to modify the advModelTree 
            // or some other gui element

            // which node removed
            switch (typeName)
            {
                case "DomainObject":
                    ClearDomainObjectPanel();
                    break;
                case "Model":
                case "ModelSequence":
                case "ModelLODSwitch":
                case "ModelSelector":
                    if (parent == mTargetNodeID)
                    {
                        ClearModelPanel();
                    }
                    break;
                case "Behavior":
                case "Sequence":
                    // if the node's parent is our targetEntity, then it was the root
                    // behavior and we can clear the entire panel
                    if (parent == mTargetNodeID)
                        ClearBehaviorPanel();
                    break;
                case "Physics":
                    throw new NotImplementedException("BaseEntityPlugin.WatchNodeRemoved() - Physics not implemented.");
                    break;

                case "AnimationSet":
                    throw new NotImplementedException("BaseEntityPlugin.WatchNodeRemoved() - AnimationSet not implemented.");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Notifies when a node has moved from one parent to another. 
        /// </summary>
        /// <param name="oldParent"></param>
        /// <param name="newParent"></param>
        /// <param name="child"></param>
        /// <param name="typeName"></param>
        public override void WatchedNodeMoved(string oldParent, string newParent, string child, string typeName)
        {
            //TreeNode draggedNode = treeEntityBrowser.Nodes[changePar.NodeID];
            //draggedNode.Remove();
            //TreeNode targetNode = treeEntityBrowser.Nodes[changePar.ParentID];
            //targetNode.Nodes.Add(draggedNode);

        }

        /// <summary>
        /// Based on the typename, determine which panel this type is represented by
        /// </summary>
        /// <param name="typename"></param>
        /// <returns></returns>
        protected virtual TabPanelNode SelectPanelNode(string typename)
        {
            switch (typename)
            {
                default:
                    //throw new ArgumentOutOfRangeException("Unexpected type '" + typename + "'");
                    return new TabPanelNode();
                    break;
            }
        }

        // TODO: the idea here is to iterate thru the entity finding all the nodes which
        // will have a tab assigned to them.  However, things have changed a bit since
        // Model tree is used in place of seperate Geoemtry and Appearance
        protected virtual TabPanelNode[] GetTabNodes(string entityID)
        {
            string[] childIDs;
            string[] childTypes;

            List<string> idResult = new List<string>();
            List<string> typeResults = new List<string>();

            mHost.Node_GetChildrenInfo(entityID, null, out childIDs, out childTypes);

            if (childIDs != null && childIDs.Length > 0)
            {
                System.Diagnostics.Trace.WriteLine("");
                for (int i = 0; i < childIDs.Length; i++)
                {
                    // search only key node types that represent a supertabControl panel
                    switch (childTypes[i])
                    {
                        case "ModelSequence":
                        case "ModelSelector":
                        case "Model":
                        case "DomainObject":
                        case "AnimationSet":
                        case "Sequence":  // for behavior
                        case "RigidBody":
                        case "BoxCollider":
                        case "SphereCollider":
                        case "CapsuleCollider":
                            // OBSOLETE - the following types are not triggers for their own panels.  
                            // instead they are handled within one of the main panels
                            //case "Mesh3d":
                            //case "Actor3d":
                            //case "Billboard":
                            //case "GeometrySwitch":
                            //case "LODSwitch":


                            //case "Appearance":
                            //case "DefaultAppearance":
                            //case "GroupAttribute": // perhaps since GroupAttribute's do represent a sub-view
                            //                       // we should include it too?
                            idResult.Add(childIDs[i]);
                            typeResults.Add(childTypes[i]);
                            break;
                        default:
                            break; // skip all other node types
                    }
                }
            }

            TabPanelNode[] result = new TabPanelNode[idResult.Count + 1]; // +1 because we include default General tab
            result[0].ID = entityID;
            result[0].Typename = mHost.Node_GetTypeName(entityID);
            for (int i = 0; i < idResult.Count; i++)
            {
                result[i + 1].ID = idResult[i];
                result[i + 1].Typename = typeResults[i];
            }
            return result;
        }

        protected virtual void RebuildPanel(TabPanelNode tpn)
        {
            switch (tpn.Typename)
            {
                case "Viewpoint":

                    break;
                case "Region":
                case "CelledRegion":
                case "ZoneRoot":
                case "Root":
                case "DefaultEntity":
                case "BonedEntity":
                case "ModeledEntity":
                case "World":
                case "Star":
                case "StellarSystem":
                case "Light":
                case "DirectionalLight":
                case "SpotLight":
                case "PointLight":
                case "PlayerVehicle":
                case "Vehicle":
                    PopulateGeneralTab(tpn.ID);
                    ClearAnimationGrid();
                    break;
                case "DomainObject":
                    PopulateDomainObjectPanel(tpn.ID);
                    break;
                // NOTE: each entity only has one root Model or one Selector node 
                // The caller of this function UpdatePanel() is not iterating any deeper children so it will stop
                // at this first node.  If however it is a switch node, our PopulateGeometryPanel(tpn.ID);
                // will recurse and generate more tree nodes to represent any children.
                case "Model":
                case "ModelSelector":
                case "SegmentSwitch":
                case "ModelSequence":
                case "ModelSwitch":
                case "ModelLODSwitch":
                    PopulateModelTree(mTargetNodeID, tpn.ID, tpn.Typename);
                    PopulateModelPanel(mTargetNodeID, tpn.ID, tpn.Typename);
                    break;

                case "RigidBody":
                case "BoxCollider":
                case "SphereCollider":
                case "CapsuleCollider":
                    CreatePhysicseCards(mTargetNodeID);
                    break;

                case "Sequence": // lots of different node types can be root behavior nodes, even a script!
                    PopulateBehaviorPanel(mTargetNodeID, tpn.ID, tpn.Typename);
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.UpdatePanel() - Unexpected panel target " + tpn.Typename);
                    break;
            }

            //System.Diagnostics.Debug.WriteLine("BaseEntityPlugin.UpdatePanel() - Rebuild Panel target " + tpn.Typename + " complete.");

        }

        private void IncrementalUpdatePanel()
        {

        }

        private void PopulateGeneralTab(string entityID)
        {
            // when assigning these values, we don't want to supress the change events which may
            // do things like updating labels and such, but we do want to supress the command to
            // send the "change" to the network because the source of these changes is already
            // from the network! And not from the user.
            try
            {
                // TODO: enable should be a flag in Entity not a bool?

                uint entityFlags = (uint)mHost.Node_GetProperty(mTargetNodeID, "entityflags");

                cbEnable.Checked = mHost.Node_GetFlagValue(mTargetNodeID, "enable");

                cbVisible.Checked = mHost.Entity_GetFlagValue(mTargetNodeID, "visible");
                cbPickable.Checked = (bool)mHost.Entity_GetFlagValue(mTargetNodeID, "pickable");
                cbCollisions.Checked = (bool)mHost.Entity_GetFlagValue(mTargetNodeID, "collidable");
                cbDynamic.Checked = (bool)mHost.Entity_GetFlagValue(mTargetNodeID, "dynamic");

                // todo: we should add a special mHost function to return the selected uint to a string as well as populate the entire combobox with types from user_constants.css
                PopulateUserTypeIDCombobox(mTargetNodeID);
                uint userTypeID = (uint)mHost.Node_GetProperty(mTargetNodeID, "usertypeid");
                string userTypeIDToString = mHost.Entity_GetUserTypeStringFromID(userTypeID);

                // remove the event handler to prevent combobox from firing change event when we are just initializing the starting value
                // of the comboUserTypeID control
                this.comboUserTypeID.SelectedValueChanged -= new System.EventHandler(this.comboUserTypeID_SelectedIndexChanged);
                comboUserTypeID.Text = userTypeIDToString;
                this.comboUserTypeID.SelectedValueChanged += new System.EventHandler(this.comboUserTypeID_SelectedIndexChanged);

                // TODO: inheritrotation and inheritscale are "properties" of
                // Transform.cs instead of flags because there's only 2 of them and two bools
                // in theory should take up less mem than 32bits
                cbInheritRotation.Checked = (bool)mHost.Node_GetProperty(mTargetNodeID, "inheritrotation");
                cbInheritScale.Checked = (bool)mHost.Node_GetProperty(mTargetNodeID, "inheritscale");


                // string.Format{"{0:0.00}"}; // set in the spinner control's format property
                Vector3d vec = (Vector3d)mHost.Node_GetProperty(mTargetNodeID, "position");
                spinPositionX.Value = vec.x;
                spinPositionY.Value = vec.y;
                spinPositionZ.Value = vec.z;

                vec = (Vector3d)mHost.Node_GetProperty(mTargetNodeID, "scale");
                spinScaleX.Value = vec.x;
                spinScaleY.Value = vec.y;
                spinScaleZ.Value = vec.z;
                Quaternion quat = (Quaternion)mHost.Node_GetProperty(mTargetNodeID, "rotation");
                vec = quat.GetEulerAngles(true);
                spinRotationX.Value = vec.x;
                spinRotationY.Value = vec.y;
                spinRotationZ.Value = vec.z;
                //vecEditCardRotation.Value = quat.GetEulerAngles(true);
            }
            catch (NullReferenceException nullEx)
            {
                // this method assumes an entity is the nodeID, but currently
                // if in scene tree you click a non entity like a Script node, then certain
                // properties like rotation, scale, etc wont exist and a mHost.Node_GetProperty
                // will return null.
            }
            catch (Exception ex)
            {
            }
        }

        private void PopulateUserTypeIDCombobox(string entityID)
        {
            comboUserTypeID.Items.Clear();
            string[] results = mHost.Entity_GetUserTypeIDsToString();
            if (results == null || results.Length == 0) return;


            for (int i = 0; i < results.Length; i++)
                comboUserTypeID.Items.Add(results[i]);
        }

        private void PopulateLightTab(string entityID)
        {
            // when assigning these values, we don't want to supress the change events which may
            // do things like updating labels and such, but we do want to supress the command to
            // send the "change" to the network because the source of these changes is already
            // from the network! And not from the user.
            try
            {
                // TODO: enable should be a flag in Entity not a bool?

                uint entityFlags = (uint)mHost.Node_GetProperty(mTargetNodeID, "entityflags");

                cbEnable.Checked = mHost.Node_GetFlagValue(mTargetNodeID, "enable");

                cbVisible.Checked = mHost.Entity_GetFlagValue(mTargetNodeID, "visible");
                cbPickable.Checked = (bool)mHost.Entity_GetFlagValue(mTargetNodeID, "pickable");
                cbCollisions.Checked = (bool)mHost.Entity_GetFlagValue(mTargetNodeID, "collidable");
                cbDynamic.Checked = (bool)mHost.Entity_GetFlagValue(mTargetNodeID, "dynamic");

                // TODO: inheritrotation and inheritscale are "properties" of
                // Transform.cs instead of flags because there's only 2 of them and two bools
                // in theory should take up less mem than 32bits
                cbInheritRotation.Checked = (bool)mHost.Node_GetProperty(mTargetNodeID, "inheritrotation");
                cbInheritScale.Checked = (bool)mHost.Node_GetProperty(mTargetNodeID, "inheritscale");


                // string.Format{"{0:0.00}"}; // set in the spinner control's format property
                if (mTargetTypename != "DirectionalLight")
                {
                    groupLightPosition.Visible = true;
                    Vector3d vec = (Vector3d)mHost.Node_GetProperty(mTargetNodeID, "position");
                    spinLightPositionX.Value = vec.x;
                    spinLightPositionY.Value = vec.y;
                    spinLightPositionZ.Value = vec.z;
                }
                else
                    groupLightPosition.Visible = false;

                Keystone.Types.Color color = (Keystone.Types.Color)mHost.Node_GetProperty(mTargetNodeID, "diffuse");
                spinLightDiffuseR.Value = 255 / color.r;
                spinLightDiffuseG.Value = 255 / color.g;
                spinLightDiffuseB.Value = 255 / color.b;

                color = (Keystone.Types.Color)mHost.Node_GetProperty(mTargetNodeID, "ambient");
                spinLightAmbientR.Value = (int)(255f / color.r);
                spinLightAmbientG.Value = 255 / color.g;
                spinLightAmbientB.Value = 255 / color.b;

                color = (Keystone.Types.Color)mHost.Node_GetProperty(mTargetNodeID, "specular");
                spinLightSpecularR.Value = 255 / color.r;
                spinLightSpecularG.Value = 255 / color.g;
                spinLightSpecularB.Value = 255 / color.b;

                // range, falloff, theta, phi
                spinLightRange.Value = (float)mHost.Node_GetProperty(mTargetNodeID, "range");
                if (mTargetTypename == "SpotLight")
                {
                    spinLightFalloff.Value = (float)mHost.Node_GetProperty(mTargetNodeID, "falloff");
                    spinLightTheta.Value = (float)mHost.Node_GetProperty(mTargetNodeID, "theta");
                    spinLightPhi.Value = (float)mHost.Node_GetProperty(mTargetNodeID, "phi");

                    spinLightFalloff.Visible = true;
                    spinLightTheta.Visible = true;
                    spinLightPhi.Visible = true;

                    lblFalloff.Visible = true;
                    lblTheta.Visible = true;
                    lblPhi.Visible = true;
                }
                else
                {
                    spinLightFalloff.Visible = false;
                    spinLightTheta.Visible = false;
                    spinLightPhi.Visible = false;

                    lblFalloff.Visible = false;
                    lblTheta.Visible = false;
                    lblPhi.Visible = false;
                }
            }
            catch (NullReferenceException nullEx)
            {
                // this method assumes an entity is the nodeID, but currently
                // if in scene tree you click a non entity like a Script node, then certain
                // properties like rotation, scale, etc wont exist and a mHost.Node_GetProperty
                // will return null.
            }
            catch (Exception ex)
            {
            }
        }

        public override ContextMenuStrip GetContextMenu(string resourceID, string parentID, Point location)
        {
            ContextMenuStrip menu = base.GetContextMenu(resourceID, parentID, location);

            // a group attribute cannot be deleted because it is associated with a group in tvactor or tvmesh.
            // a Group can be deleted if the mesh group is somehow merged or removed.  Maybe we could have
            // a "Merge Group" or "Delete Group" and when that occurs, make a call to the underlying Mesh3d or Actor3d
            // to merge or delete the group.



            // On component based systems
            // http://www.gamedev.net/community/forums/topic.asp?topic_id=463508
            //     How does the PhysicsComponent talk to the AnimationComponent?
            //  So does this always mean that if an entity has a PhysicsComponent is must also have an AnimationComponent, 
            //what if it didn't?
            //Remember that things can be set up such that the creation of a PhysicsComponent is the result of the creation
            //of an AnimationComponent, not the creation of an Entity. So the AnimationComponent is available to 
            //PhysicsComponent at creation time, and Entities without AnimationComponents aren't visible to the 
            //PhysicsComponent. (Of course, if you wanted the PhysicsComponent to be able to attach to entities 
            //without AnimationComponents, you'd listen to World instead of AnimationSubsystem, and on Entity 
            //addition simply ask AnimationSubsystem if it had a component for that Entity.)
            // NOTE: with regards to the above, i belive that's why some people's "component" systems aren't just a Controller
            // added to an Entity, but instead, the creation of a Component class such as an AnimatedGroundWalker that would
            // then know to have support for physics, IAnimated, etc.  It's definetly more of a hybrid approach to the 
            // component system i think though.
            //  And you know,when you analyze it, you realize that this component system is very similar to any SceneGraph
            // where components combine to form complex visuals... the difference is, we have more node types for handling
            // complex groups of nodes which we refer to as Entities.
            // ---
            //
            // add the menu items specifically related to Entity editing
            //  or even the type of Entity
            //      - GUIControl
            //      - Component
            //      - Vehicle
            //      - PlayerCharacter <-- perhaps only differnece between player and npc is how it's controlled.
            //                              thus anything can be either NPC or player controlled by replacing the Behavior
            //      - NPC


            // Model  
            //   physics body 1:1 entity:body (what about joints and range of motion restrictors and such?)
            //   controller\behavior (a behavior could just be something simple like orbit a position
            //          such that a behavior can govern how something moves on it's own.  Has nothing to do with physics necessarily
            //          for instance, the behavior for orbit could use a bezier spline path)
            //   AI controller
            //   animation controller?
            //   script, event handler wiring
            //   Emitter 
            // Type t = mHost.ChildTypes();

            // if no physics body already exists, add option to add one
            // "Add Model"
            // 

            menu.Items.Add(new ToolStripSeparator());

            // TODO: have option to view xml of the prefab
            ToolStripMenuItem menuItem = new ToolStripMenuItem("Save Prefab...");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(buttonSavePrefab_Click);
            menu.Items.Add(menuItem);

            menu.Items.Add(new ToolStripSeparator());

            menuItem = new ToolStripMenuItem("Set As Player Controlled");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Checked = mHost.Entity_GetFlagValue(resourceID, "playercontrolled");
            menuItem.Click += new EventHandler(SetPlayerControlledEntity_Click);
            menu.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("Is Viewpoint");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Checked = mHost.Entity_GetFlagValue(resourceID, "hasviewpoint");
            menuItem.Click += new EventHandler(SetEntityHasViewpoint_Click);
            menu.Items.Add(menuItem);

            menu.Items.Add(new ToolStripSeparator());

            menuItem = new ToolStripMenuItem("Add Empty Entity");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(AddEmptyEntity_Click);
            menu.Items.Add(menuItem);

            // 
            menuItem = new ToolStripMenuItem("Add Prefab...");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(AddPrefab_Click);
            menu.Items.Add(menuItem);

            menu.Items.Add(new ToolStripSeparator());

            menuItem = new ToolStripMenuItem("Add physics body");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(addPhysicsBody_Click);
            menu.Items.Add(menuItem);

            menu.Items.Add(new ToolStripSeparator());

            // goto / lookAt/ viewEntity
            menuItem = new ToolStripMenuItem("Goto Entity");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(goto_Click);
            menu.Items.Add(menuItem);

            // vehicle orbit celestial body
            menuItem = new ToolStripMenuItem("Orbit");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(orbit_Click);
            menu.Items.Add(menuItem);

            // vehicle travel to target
            menuItem = new ToolStripMenuItem("Travel To");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(travelTo_Click);
            menu.Items.Add(menuItem);

            // intercept
            menuItem = new ToolStripMenuItem("Intercept");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(intercept_Click);
            menu.Items.Add(menuItem);

            // dock
            menuItem = new ToolStripMenuItem("Dock");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(dock_Click);
            menu.Items.Add(menuItem);

            //string[] boneNames = mHost.GetBones();

            //if (boneNames != null && boneNames.Length > 0)
            //{
            //    // for boned entity, we  dynamically fill the menu with Attach To Bone #
            //    for (int i = 0; i < boneNames.Length; i++)
            //    {
            //        menuItem = new ToolStripMenuItem("Attach to bone '" + boneNames[i] + "'");
            //        menuItem.Tag = resourceID;
            //        menuItem.Click += new EventHandler(AttachToBone_Click);
            //        menu.Items.Add(menuItem);
            //    }
            //}

            menu.Items.Add(new ToolStripSeparator());

            return menu;
        }

        #region ContextMenuHandlers
        //edit menu handlers
        private void addPhysicsBody_Click(object sender, EventArgs e)
        {
            MessageBox.Show(((ToolStripMenuItem)sender).Tag.ToString());
        }

        private void goto_Click(object sender, EventArgs e)
        {
            float percentExtents = 1f;
            if (mHost != null)
                mHost.View_LookAt(mTargetNodeID, percentExtents); // perhaps add a percentage for % of extents ranging anywhere from 1-1000% 
        }

        private void orbit_Click(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Vehicle_Orbit(mTargetNodeID);
        }

        // This only works if there is a playable vehicle loaded
        private void travelTo_Click(object sender, EventArgs e)
        {
            // todo: we should set up an orbit distance
            if (mHost != null)
                mHost.Vehicle_TravelTo(mTargetNodeID);
        }

        private void intercept_Click(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Vehicle_Intercept(mTargetNodeID);
        }

        private void dock_Click(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Vehicle_Dock(mTargetNodeID);
        }

        private void SetPlayerControlledEntity_Click(object sender, EventArgs e)
        {
            // NOTE: There can only be one player controlled Vehicle
            ToolStripMenuItem item = (ToolStripMenuItem)sender;

            mHost.Entity_SetFlagValue(item.Name, "playercontrolled", !item.Checked);
        }

        private void SetEntityHasViewpoint_Click(object sender, EventArgs e)
        {
            // NOTE: There can only be one player controlled Vehicle
            ToolStripMenuItem item = (ToolStripMenuItem)sender;

            mHost.Entity_SetFlagValue(item.Name, "hasviewpoint", !item.Checked);
        }

        private void AddEmptyEntity_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            string typeName = "ModeledEntity"; // for v1.0 no Model child node will be added. Just empty ModeledEntity
            string parentID = item.Name;
            // TODO: we need to add a "name" for the node and that name should show up next to the tree node's typename 
            CreateNode(typeName, parentID);

        }

        private void AddPrefab_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException("this is ancient unfinished and wrong code");
            // 
            string file = OpenFile(@"E:\dev\c#\KeystoneGameBlocks\Data\pool\"); // TODO: remove hardcoded path);
            byte[] value;
            if (!string.IsNullOrEmpty(file))
            {
                // TODO: 
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                value = encoding.GetBytes(file);

                //          mHost.SendMessage(((ToolStripMenuItem)sender).Tag.ToString(), 9, value);
            }
        }

        private void AttachToBone_Click(object sender, EventArgs e)
        {
            //   mHost.SendMessage(((ToolStripMenuItem)sender).Tag.ToString(), 8, null);
            MessageBox.Show(((ToolStripMenuItem)sender).Tag.ToString());
        }


        // TODO: open file and save file need to grab from the custom dialog from EditorHost
        // because our custom will allow us to have a preview and a button to generate snapshot
        // then instead of SendMessage() to return the results, it simply just relies directly on
        // EditorHost.OpenFile
        // EditorHost.SaveFile
        private string OpenFile(string initialPath)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.InitialDirectory = initialPath;


            openFile.Filter = "prefab files (*.prefab)|*.prefab"; ;
            openFile.FilterIndex = 0; // select the 0th file filter specified as default

            openFile.RestoreDirectory = true;

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                return openFile.FileName;
                //string fileName = Path.GetFileName(fullName);
            }
            return "";
        }

        //private string SaveFile()
        //{
        //    SaveFileDialog saveFile = new SaveFileDialog();
        //    saveFile.InitialDirectory = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\";


        //    saveFile.Filter = "prefab files (*.prefab)|*.prefab";
        //    saveFile.FilterIndex = 0; // select the 0th file filter specified as default

        //    saveFile.RestoreDirectory = true;

        //    if (saveFile.ShowDialog() == DialogResult.OK)
        //    {
        //        return saveFile.FileName;
        //        //string fileName = Path.GetFileName(fullName);
        //    }
        //    return "";
        //}
        #endregion 

        private void buttonSavePrefab_Click(object sender, EventArgs e)
        {

            if (mHost != null)
            {
                mHost.Entity_SavePrefab(mTargetNodeID, mModFolderPath, mModName, "");
            }
        }

        //private void SaveAs_Click(object sender, EventArgs e)
        //{
        //    // launch the SaveAs dialog
        //    string file = SaveFile();
        //    byte[] value;
        //    if (!string.IsNullOrEmpty(file))
        //    {

        //        System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
        //        value = encoding.GetBytes(file);

        //        //        mHost.SendMessage(((ToolStripMenuItem)sender).Tag.ToString(), 6, value);
        //    }


        //}

        /// <summary>
        /// Calls host.Node_Create() with a null resourcePath parameter which triggers a File Browse dialog to open.
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="parentNodeID"></param>
        protected void BrowseNewResource(string resourceType, string parentNodeID, string fileDialogFilter)
        {
            if (mHost == null) return;
             mHost.Node_Create(resourceType, parentNodeID, null, fileDialogFilter);
        }

        protected void EditResource(string resourceType, string descriptor, string parentNodeID)
        {
            EditResourceEventArgs e = new EditResourceEventArgs();
            e.ResourcePath = descriptor;
            //e.ResourceType = resourceType;
            EditResourceRequest(this, e); //TODO: why not mHost.Node_EditResource() instead of this event?
        }

        protected void BrowseReplacementResource(string resourceType, string previousDescriptor, string parentNodeID)
        {
            // passing in null will signify to host user must browse and so will launch browse dialog
            mHost.Node_ReplaceResource(previousDescriptor, null, resourceType, parentNodeID);
        }

        protected void CreateNode(string typeName, string requestedResourceID, string parentID)
        {
            if (mHost != null)
                mHost.Node_Create(typeName, parentID, requestedResourceID, null);
        }

        protected void CreateNode(string typeName, string parentID)
        {
            if (mHost != null)
                // TODO: i think i need the full typename for generic types
                // but the plugin does not have direct access to those classes.
                // perhaps i can use a short version of the typename 
                // InterpolatorAnimation<Vector3d>
                // TODO: but even so, how do i get it to make a Translation target?
                // well that we should do automatically somehow... but when do we get
                // the chance?  it would be easier if we could keep using the
                // short hand typename i suppose "TranslationInterpolator" and use that
                // to create the actual type we need.
                mHost.Node_Create(typeName, parentID);
        }

        /// <summary>
        /// Moves an existing node from it's current parent to a new parent that will be created.
        /// </summary>
        /// <param name="typeName">The typename of the new node that will be created. It must
        /// be of type IGroup so that the existing nodeID can be moved under it.</param>
        /// <param name="parentID">The parent which the existing NodeID will be removed and
        /// to which the new node will be added as child instead.</param>
        /// <param name="nodeID">The existing node which will be added under the new node.</param>
        protected void InsertUnderNewNode(string typeName, string parentID, string nodeID)
        {
            if (mHost != null)
                mHost.Node_InsertUnderNewNode(typeName, parentID, nodeID);
        }

        private string mPosScaleRotationTarget; // can be either Entity or one of it's models
        private void vecEditRotation_OnChange(object sender, EventArgs e)
        {
            Rotation_Changed(mTargetNodeID, "rotation",
                spinRotationX.Value,
                spinRotationY.Value,
                spinRotationZ.Value);
        }

        private void spinRotationModel_ValueChanged(object sender, EventArgs e)
        {
            Rotation_Changed(mPosScaleRotationTarget, "rotation",
                spinRotateModelX.Value,
                spinRotateModelY.Value,
                spinRotateModelZ.Value);
        }

        private void vecEditScale_OnChange(object sender, EventArgs e)
        {
            Transformation_Changed(mTargetNodeID, "scale",
                spinScaleX.Value,
                spinScaleY.Value,
                spinScaleZ.Value);
        }

        private void spinScaleModel_ValueChanged(object sender, EventArgs e)
        {
            Transformation_Changed(mPosScaleRotationTarget, "scale",
                spinScaleModelX.Value,
                spinScaleModelY.Value,
                spinScaleModelZ.Value);
        }

        private void vecEditPosition_OnChange(object sender, EventArgs e)
        {
            Transformation_Changed(mTargetNodeID, "position",
                spinPositionX.Value,
                spinPositionY.Value,
                spinPositionZ.Value);
        }

        private void spinPositionModel_ValueChanged(object sender, EventArgs e)
        {
            Transformation_Changed(mPosScaleRotationTarget, "position",
                spinPositionModelX.Value,
                spinPositionModelY.Value,
                spinPositionModelZ.Value);
        }

        private void Transformation_Changed(string target, string propertyname, double x, double y, double z)
        {
            Vector3d value;
            value.x = x;
            value.y = y;
            value.z = z;

            if (mHost != null)
                mHost.Node_ChangeProperty(target, propertyname, typeof(Vector3d), value);
        }

        private void Rotation_Changed(string target, string propertyname, double x, double y, double z)
        {
            double pitch, yaw, roll;

            pitch = Keystone.Utilities.MathHelper.DegreesToRadians(x);
            yaw = Keystone.Utilities.MathHelper.DegreesToRadians(y);
            roll = Keystone.Utilities.MathHelper.DegreesToRadians(z);

            // change the property using the Quaternion contructor that accepts radian euler angles 
            if (mHost != null)
            {
                Quaternion quat = new Quaternion(yaw, pitch, roll);
                quat = Quaternion.Normalize(quat);
                mHost.Node_ChangeProperty(mPosScaleRotationTarget, "rotation", typeof(Quaternion), quat);
            }
        }

        private void cbEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_SetFlagValue(mTargetNodeID, "enable", cbEnable.Checked);
        }

        private void cbVisible_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Entity_SetFlagValue(mTargetNodeID, "visible", cbVisible.Checked);
        }

        private void cbPickable_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Entity_SetFlagValue(mTargetNodeID, "pickable", cbPickable.Checked);
        }

        private void cbDynamic_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Entity_SetFlagValue(mTargetNodeID, "dynamic", cbDynamic.Checked);
        }

        private void cbCollisions_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Entity_SetFlagValue(mTargetNodeID, "collidable", cbCollisions.Checked);
        }


        private void cbInheritScale_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_ChangeProperty(mTargetNodeID, "inheritscale", typeof(bool), cbInheritScale.Checked);
        }

        private void cbInheritRotation_CheckedChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_ChangeProperty(mTargetNodeID, "inheritrotation", typeof(bool), cbInheritRotation.Checked);
        }

        #region DragDrop
        private void OnScriptPictureBox_DragDrop(object sender, DragEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("Control Drag Drop");
            try
            {
                DragDropContext node = (DragDropContext)args.Data.GetData(typeof(DragDropContext));
                System.Drawing.Point mousePoint = new System.Drawing.Point(args.X, args.Y);
                Control c = (Control)sender;
                mousePoint = c.PointToClient(mousePoint);

                if (c.Bounds.Contains(mousePoint))
                {
                    // we can't rely on plugin excluding non .CSS script nodes
                    // but in our own implementation we'll add the check here as well even though
                    // our command processor server side will also check and disallow unauthorized node
                    // placements
                    //     if (System.IO.Path.GetExtension(node.ResourcePath).ToUpper() != ".CSS") 
                    //{
                    //    MessageBox.Show("The node of type '" + "' cannot be placed onto a parent of type '");
                    //    return;
                    //}
                    string typeName = "";
                    switch (System.IO.Path.GetExtension(node.ResourcePath).ToUpper())
                    {
                        case ".CSS":
                            typeName = "DomainObjectScript";
                            break;
                        //case ".X":
                        //case ".TVM":
                        //case ".OBJ":
                        //    typeName = "Mesh3d";
                        //    break;
                    }
                    string filter = "Scripts|*.css";
                    string parentID = c.Tag.ToString();
                    mHost.Node_Create(typeName, node.ModName + "|" + node.ResourcePath, parentID, filter);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("EntityEditCtl.OnBehaviorTree_DragDrop() - " + ex.Message);
            }
        }

        private void OnScriptPictureBox_DragEnter(object sender, DragEventArgs args)
        {
            args.Effect = args.AllowedEffect;
            DragDropContext node = (DragDropContext)args.Data.GetData(typeof(DragDropContext));
            if (node == null)
                args.Effect = DragDropEffects.None;

            // TODO: if this is not the correct node type, 
            // then we should also set the effect to .None
        }

        private void OnScriptPictureBox_DragLeave(object sender, EventArgs args)
        {

        }

        private void OnScriptPictureBox_DragOver(object sender, DragEventArgs args)
        {
            System.Drawing.Point mousePoint = new System.Drawing.Point(args.X, args.Y);
            Control c = (Control)sender;
            mousePoint = c.PointToClient(mousePoint);
            string parent = "";

            if (sender is DevComponents.AdvTree.AdvTree)
            {
                DevComponents.AdvTree.AdvTree tree = (DevComponents.AdvTree.AdvTree)sender;
                DevComponents.AdvTree.Node node = tree.GetNodeAt(mousePoint);
                if (node == null) return;
                parent = node.Name;
            }
            else
            {
                Control child = c.GetChildAtPoint(mousePoint);
                if (child == null) return;
                parent = child.Name;
            }

            ((DragDropContext)args.Data.GetData(typeof(DragDropContext))).ParentID = parent;
        }
        #endregion



        private void superTabControlPanel6_Click(object sender, EventArgs e)
        {

        }

        private void superTabControl_SelectedTabChanged(object sender, DevComponents.DotNetBar.SuperTabStripSelectedTabChangedEventArgs e)
        {

        }

        #region LightParameters
        private void spinLightDiffuseB_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "diffuse",
                (float)spinLightDiffuseR.Value,
                (float)spinLightDiffuseG.Value,
               (float)spinLightDiffuseB.Value);

        }

        private void spinLightDiffuseG_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "diffuse",
                (float)spinLightDiffuseR.Value,
                (float)spinLightDiffuseG.Value,
                (float)spinLightDiffuseB.Value);
        }

        private void spinLightDiffuseR_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "diffuse",
                           (float)spinLightDiffuseR.Value,
                           (float)spinLightDiffuseG.Value,
                            (float)spinLightDiffuseB.Value);
        }

        private void Light_Color_Changed(string targetLightID, string parameter, float r, float g, float b)
        {
            if (mHost != null)
            {
                Keystone.Types.Color color = new Keystone.Types.Color(r / 255f, g / 255f, b / 255f, 1.0f);
                mHost.Node_ChangeProperty(targetLightID, parameter, typeof(Keystone.Types.Color), color);
            }
        }

        private void spinLightAmbientR_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "ambient",
                (float)spinLightAmbientR.Value,
                (float)spinLightAmbientG.Value,
               (float)spinLightAmbientB.Value);
        }

        private void spinLightAmbientG_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "ambient",
                (float)spinLightAmbientR.Value,
                (float)spinLightAmbientG.Value,
               (float)spinLightAmbientB.Value);
        }

        private void spinLightAmbientB_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "ambient",
                (float)spinLightAmbientR.Value,
                (float)spinLightAmbientG.Value,
               (float)spinLightAmbientB.Value);
        }

        private void spinLightSpecularR_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "specular",
                (float)spinLightSpecularR.Value,
                (float)spinLightSpecularG.Value,
               (float)spinLightSpecularB.Value);
        }

        private void spinLightSpecularG_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "specular",
                (float)spinLightSpecularR.Value,
                (float)spinLightSpecularG.Value,
               (float)spinLightSpecularB.Value);
        }

        private void spinLightSpecularB_ValueChanged(object sender, EventArgs e)
        {
            Light_Color_Changed(mTargetNodeID, "specular",
                (float)spinLightSpecularR.Value,
                (float)spinLightSpecularG.Value,
               (float)spinLightSpecularB.Value);
        }

        private void spinAttenuation0_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                float[] values = new float[3];
                values[0] = (float)spinAttenuation0.Value;
                values[1] = (float)spinAttenuation1.Value;
                values[2] = (float)spinAttenuation2.Value;

                mHost.Node_ChangeProperty(mTargetNodeID, "attenuation", typeof(float[]), values);
            }
        }

        private void spinAttenuation1_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                float[] values = new float[3];
                values[0] = (float)spinAttenuation0.Value;
                values[1] = (float)spinAttenuation1.Value;
                values[2] = (float)spinAttenuation2.Value;
                mHost.Node_ChangeProperty(mTargetNodeID, "attenuation", typeof(float[]), values);
            }
        }

        private void spinAttenuation2_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                float[] values = new float[3];
                values[0] = (float)spinAttenuation0.Value;
                values[1] = (float)spinAttenuation1.Value;
                values[2] = (float)spinAttenuation2.Value;
                mHost.Node_ChangeProperty(mTargetNodeID, "attenuation", typeof(float[]), values);
            }
        }


        private void igSpinLightRange_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_ChangeProperty(mTargetNodeID, "range", typeof(float), (float)spinLightRange.Value);
        }

        private void spinLightFalloff_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_ChangeProperty(mTargetNodeID, "falloff", typeof(float), (float)spinLightFalloff.Value);
        }

        private void spinLightTheta_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_ChangeProperty(mTargetNodeID, "theta", typeof(float), (float)spinLightTheta.Value);
        }

        private void spinLightPhi_ValueChanged(object sender, EventArgs e)
        {
            if (mHost != null)
                mHost.Node_ChangeProperty(mTargetNodeID, "phi", typeof(float), (float)spinLightPhi.Value);
        }

        private void spinLightPositionX_ValueChanged(object sender, EventArgs e)
        {
            Transformation_Changed(mTargetNodeID, "position", (double)spinLightPositionX.Value, (double)spinLightPositionY.Value, (double)spinLightPositionZ.Value);
        }

        private void spinLightPositionY_ValueChanged(object sender, EventArgs e)
        {
            Transformation_Changed(mTargetNodeID, "position", (double)spinLightPositionX.Value, (double)spinLightPositionY.Value, (double)spinLightPositionZ.Value);
        }

        private void spinLightPositionZ_ValueChanged(object sender, EventArgs e)
        {
            Transformation_Changed(mTargetNodeID, "position", (double)spinLightPositionX.Value, (double)spinLightPositionY.Value, (double)spinLightPositionZ.Value);
        }

        private void cboLightingMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                string appearanceID = this.treeModel.SelectedNode.Name;
                mHost.Node_ChangeProperty(appearanceID, "lightingmode", typeof(int), cboLightingMode.SelectedIndex);
            }
        }

        private void comboUserTypeID_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mHost != null)
            {
                // todo: the selection index is not the same as the COMPONENT_TYPE value
                string userTypeName = comboUserTypeID.SelectedItem.ToString();
                uint userTypeID = mHost.Entity_GetUserTypeIDFromString(userTypeName);
                mHost.Node_ChangeProperty(mTargetNodeID, "usertypeid", typeof(uint), userTypeID);

            }
        }

        private void BaseEntityPlugin_Resize(object sender, EventArgs e)
        {
           
        }


        // particle emitter tab size changed
        private void superTabControlPanel11_SizeChanged(object sender, EventArgs e)
        {
            DevComponents.DotNetBar.SuperTabControlPanel panel = (DevComponents.DotNetBar.SuperTabControlPanel)sender;

            foreach (Control c in panel.Controls)
            {
                if (c is NotecardBase == false) continue;

                const int bottomPadding = 6;
                double height = panel.Size.Height;
                Size size = new Size(c.Width, panel.Height - bottomPadding) ;
                c.Size = size;
            }
        }


        #endregion









        //protected virtual void cbSnapToGrid_CheckedChanged(object sender, EventArgs e)
        //{

        //}

        //protected virtual void cbSnapAngles_CheckedChanged(object sender, EventArgs e)
        //{

        //}

        //protected virtual void comboPositionIncrement_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //}

        //protected virtual void cbLockPositionScaleRotation_CheckedChanged(object sender, EventArgs e)
        //{

        //}
    }
}
