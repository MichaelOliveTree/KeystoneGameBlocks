using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using Keystone.Entities;
using Keystone.Resource;
using Keystone.Scene;
using System.Drawing;


namespace KeyEdit.Workspaces
{
    public partial class EditorWorkspace   
    {
        private delegate void AddTreeItem(Keystone.Elements.Node parent, Keystone.Elements.Node child);
        private delegate void RemoveTreeItem(Keystone.Elements.Node parent, Keystone.Elements.Node child);
              
        
        private void InitializeTreeview()
        {
            // 
            // treeEntityBrowser
            // 
            this.mTreeEntityBrowser = new TreeView();
            this.mTreeEntityBrowser.AllowDrop = true;
            this.mTreeEntityBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mTreeEntityBrowser.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mTreeEntityBrowser.Location = new System.Drawing.Point(0, 0);
            this.mTreeEntityBrowser.Name = "treeEntityBrowser";
            this.mTreeEntityBrowser.Size = new System.Drawing.Size(260, 518);
            this.mTreeEntityBrowser.TabIndex = 1;
            this.mTreeEntityBrowser.NodeMouseDoubleClick +=
                new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeEntityBrowser_NodeMouseDoubleClick);
            this.mTreeEntityBrowser.DragLeave += new System.EventHandler(this.treeEntityBrowser_OnDragLeave);
            this.mTreeEntityBrowser.MouseUp += new System.Windows.Forms.MouseEventHandler(this.treeEntityBrowser_MouseUp);
            this.mTreeEntityBrowser.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeEntityBrowser_OnDragDrop);
            this.mTreeEntityBrowser.MouseMove += new System.Windows.Forms.MouseEventHandler(this.treeEntityBrowser_MouseMove);
            this.mTreeEntityBrowser.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeEntityBrowser_MouseDown);
            this.mTreeEntityBrowser.DragEnter += new System.Windows.Forms.DragEventHandler(this.treeEntityBrowser_OnDragEnter);
            this.mTreeEntityBrowser.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeEntityBrowser_NodeMouseClick);
            this.mTreeEntityBrowser.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.treeEntityBrowser_NodeDrag);
            this.mTreeEntityBrowser.DragOver += new System.Windows.Forms.DragEventHandler(this.treeEntityBrowser_OnDragOver);
        }

        
        public void OnEntityChanged(string entityID)
        {
            // mScene.Name, entityID, entityTypeName
        }

        

        // overloaded version for Tree node selection of an Entity
        public void OnTreeviewEntitySelected(Entity entity)
        {
            Keystone.Collision.PickResults result = new Keystone.Collision.PickResults();
            result.SetEntity (entity);

            OnEntitySelected(result);
        }

        
        private void PopulateTreeview(Keystone.Elements.Node parent, Keystone.Elements.Node child)
        {
            lock(mSyncLock)
            {
                if (child as Entity == null) return;

                AddTreeItemHandler(null, child);

                Keystone.Elements.IGroup group = child as Keystone.Elements.IGroup;
                if (group == null) return;

                Keystone.Elements.Node[] children = group.Children;
                if (children == null) return;
                for (int i = 0; i < children.Length; i++)
                {
                    // recurse
                    PopulateTreeview(child, children[i]);
                }

            }
        }
        private void AddTreeItemHandler(Keystone.Elements.Node parent, Keystone.Elements.Node child)
        {
            try
            {
                // This sub is essentially called via Repository.Resource.IncrementRef()
                // in succession for every node.  If an Entity with a single Geometry child is added
                // to the scene, this event will fire for each.  However, we will skip non Entity children.
                // 
                // we only add Entities (note: Viewpoint is an entity)
                if ((child is Entity) == false) return;


                if (parent != null)
                {
                    // TODO: For actors we special case that node and add children
                    // for each boneID so that we can drag and drop actors or meshes
                    // to specific child bones.
                    //
                    // search starting at root node collection
                    TreeNode[] nodes = mTreeEntityBrowser.Nodes.Find(parent.ID, true);
                    // since this only contains entities, there should only ever be just one node with this key found
 //                   Debug.Assert(nodes.Length == 1, "AddTreeviewItemHandler() - ERROR: More than one entity instance found in treeview.  Entities should only ever have one instance!");

                    if (nodes.Length > 0)
                    {
                        int imageIndex = -1;
                        string label = GetLabelText (child.Name, child.TypeName);


                        // since nodes can be added by OnEntityAdded() and PopulateTreeview() we verify
                        // that the child we are attempting to add to the parent does not already exist
                        if (nodes[0].Nodes.ContainsKey(child.ID)) return;
                                               
                        nodes[0].Nodes.Add(child.ID, label, imageIndex);

                        nodes[0].ExpandAll();
                    }
                    // NOTE: following we wont use.  If a parent node does not exist, this child is
                    // not actually connected to the Scene (eg. widget controls, sceneInfo, floor_overlay hud entities)
                    //else
                    //    treeEntityBrowser.Nodes.Add(child.ID, child.TypeName + " " + child.ID); // temp
                }
                else
                {
                    string label = GetLabelText(child.Name, child.TypeName);
                    TreeNode treeNode = mTreeEntityBrowser.Nodes.Add(child.ID, label);
                    treeNode.Name = child.ID;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error adding node {0} to treeview." + ex.Message, child.TypeName));
            }
        }

        

        private void RemoveTreeItemHandler(Keystone.Elements.Node parent, Keystone.Elements.Node child)
        {
            try
            {
                // we know we only add Entities so non entity children cant be removed because
                // they were never added.
                if ((child is Entity) == false) return;
                TreeNode[] nodes = mTreeEntityBrowser.Nodes.Find(child.ID, true);

                if (nodes.Length > 0)
                {
                    if (nodes.Length == 1)
                        if (nodes[0].Parent != null)
                            nodes[0].Parent.Nodes.Remove(nodes[0]);
                        //if (treeEntityBrowser.Nodes.ContainsKey(nodes[0].Name))
                        //    treeEntityBrowser.Nodes[nodes[0].Index].Remove();
                        else
                        {
                            // since our tree is based off of a DAG, there might be some child nodes that have multiple instances in the tree
                            // we only want to remove the one who's parent matches parent.ID
                            // TODO: when generating a temporary scene such as Universe, we dont want these RemoveTreeItemHandler or
                            // AddTreeItemHandlers to fire.
                            for (int i = 0; i < nodes.Length; i++)
                                if (nodes[i].Parent != null)
                                    if (nodes[i].Parent.Name == parent.ID)
                                        if (mTreeEntityBrowser.Nodes.ContainsKey(nodes[i].Name))
                                            mTreeEntityBrowser.Nodes[nodes[i].Index].Remove();
                        }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("Error removing node {0} to treeview." + ex.Message, child.ID));
            }
        }

        /// <summary>
        /// If an EntityBase is being added, this Clears the tree and adds a new Entity resource as root node of the Treeview
        /// </summary>
        /// <param name="resourceID"></param>
        private void AddResourceToTree(IResource resource)
        {

            if (resource != null)
            {
                // if we're already on this resource, just return
                if (mCurrentResource == resource) return;
                mCurrentResource = resource;

                // save the tree state, push it onto the stack and then 
                // clear the treeview and repopulate it with the selected entities
                if (resource is Entity)
                {
                    TreeviewState state = new TreeviewState();
                    string parentID = "";
                    if (((Entity)resource).Parent != null)
                        parentID = ((Entity)resource).Parent.ID;

                    mTreeEntityBrowser.Tag = parentID;
                    state.SaveTreeView(mTreeEntityBrowser, parentID);
                    mTreeStates.Push(state);

                    mTreeEntityBrowser.Nodes.Clear();

                    
                    // if this is not the root region, add as first tree node "..." for returning to the previous parent
                    if ((resource is Keystone.Portals.Root) == false)
                        mTreeEntityBrowser.Nodes.Add("..", "..");

                    // add the entity itself
                    string name = "";
                    if (resource is Keystone.Elements.Node)
                        name = ((Keystone.Elements.Node)resource).Name;

                    string label = GetLabelText(name, resource.TypeName);
                    TreeNode node = mTreeEntityBrowser.Nodes.Add(resource.ID, label);
                    node.Name = resource.ID;

                    // Add all the entity components as well as all recursive child entities.  But only our
                    // primary selected Entity will have it's sub-components loaded.  Child entities will appear just as if we
                    // were still in the main Entity browser mode
                    AddSubNodes((Entity)resource, node, true);

                    // expand after the child nodes added
                    if (node.Parent == null)
                        mTreeEntityBrowser.ExpandAll();
                    else
                        node.Parent.Expand();
                }
            }
        }


        private void AddSubNodes(Keystone.Elements.IGroup group, TreeNode parentNode, bool recurseChildEntities)
        {
            if (group.Children != null)
                for (int i = 0; i < group.Children.Length; i++)
                {
                    TreeNode subParent = null;
                    if (group.Children[i] is Entity)
                    {
                        string label = GetLabelText (group.Children[i].Name, group.Children[i].TypeName);
                        subParent = parentNode.Nodes.Add(group.Children[i].ID, label);
                        subParent.Name = group.Children[i].ID;
                    }
                    // now recurse all child Groups except Entities if prohibited
                    if (group.Children[i] is Keystone.Elements.IGroup)
                    {
                        if (group.Children[i] is Entity && !recurseChildEntities)
                            continue;

                        // only add entiy children, not non entities like mesh3d, scripts, etc
                        AddSubNodes((Keystone.Elements.IGroup)group.Children[i], subParent, recurseChildEntities);
                    }
                }
        }


        #region treeEntityBrowser event handlers
        private IResource mCurrentResource;
        private System.Collections.Generic.Stack<TreeviewState> mTreeStates = new Stack<TreeviewState>();
        /// <summary>
        /// Double click causes us to act like we're switching URLs or following a link to the internals of that Entity.
        /// We can later use the "back or up" commands to go back to the Entity treeview.   Using two seperate treeviews
        /// will make it easier to maintain our state when going between the Entity and it's Internals.  To simplify things we
        /// should not worry about a "history" stack or any such thing.  All we need is a single "Forward" button when an 
        /// Entity is highlighted and a "Back" image to take the place on this toggle button when viewing an Entity's internal
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeEntityBrowser_NodeMouseDoubleClick(object sender, System.Windows.Forms.TreeNodeMouseClickEventArgs e)
        {

            mTreeEntityBrowser.SelectedNode = mTreeEntityBrowser.GetNodeAt(e.X, e.Y);

            if (mTreeEntityBrowser.SelectedNode != null)
            {

                if (mTreeEntityBrowser.SelectedNode.Name == "..")
                {
                    // clear the treeview and move to the previous treeState 
                    mTreeEntityBrowser.Nodes.Clear();
                    // Display a wait cursor while the TreeNodes are being created.
                    Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                    // Suppress repainting the TreeView until all the objects have been created.
                    mTreeEntityBrowser.BeginUpdate();

                    TreeviewState state = mTreeStates.Pop();

                    AddResourceToTree(mScene.GetResource((string)state.Tag));
                    OnTreeviewEntitySelected((Entity)mScene.GetResource((string)state.Tag));

                    state.RestoreTreeView(mTreeEntityBrowser, "");

                    // TODO: now there's a slight issue here... what happens if
                    // we somehow add or delete new nodes to the scene such that when we
                    // revert back, nodes in the cached state do not match those in the actual scene
                    // Well i think for robustness this answers itself, we should not store the actual nodes, but just the parent
                    // since it should be impossible to delete a parent without being on that node in the tree 
                    // further, when trying to restore the expanded state, if a particular nodes in the expanded list does not match
                    // any node in the tree, we skip it
                    // treeEntityBrowser.node

                    // TODO: if we do delete a parent, we should clear the stack and return to the root
                    mTreeEntityBrowser.EndUpdate();
                    Cursor.Current = System.Windows.Forms.Cursors.Default;
                }
                else
                { 
                    // double click is always preceded by NodeMouseClick (single click event) so there is no need to set the Selected
                    // entity in SceneManager.ActiveScene.Selected or to set the property grid again.
                    IResource resource = mScene.GetResource(mTreeEntityBrowser.SelectedNode.Name);
                    // AddResourceToTree actually clears the tree and adds this resource as a root tree node
                    AddResourceToTree(resource);
                    OnTreeviewEntitySelected((Entity)resource);
                }
            }
        }

        // TODO: this particular treeview should be owned by the Editor workspace
        private void treeEntityBrowser_NodeMouseClick(object sender, System.Windows.Forms.TreeNodeMouseClickEventArgs e)
        {
            // note: this only fires on left mouse click.
            EndDrag();

            mTreeEntityBrowser.SelectedNode = mTreeEntityBrowser.GetNodeAt(e.X, e.Y);

            if (mTreeEntityBrowser.SelectedNode != null)
            {
                IResource resource = Repository.Get(mTreeEntityBrowser.SelectedNode.Name);
                if (resource != null)
                    OnTreeviewEntitySelected((Entity)resource);

                if (resource as Entity != null)
                {
                	Keystone.Collision.PickResults psuedoResult = new Keystone.Collision.PickResults ();
                	psuedoResult.HasCollided = true;
                	psuedoResult.SetEntity ((Entity)resource);
                }
            }
        }

        /// <summary>
        /// Right Mouse Click Context Menu Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeEntityBrowser_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                EndDrag();
                return;
            }

            if (e.Button != System.Windows.Forms.MouseButtons.Right)
                return;

            mTreeEntityBrowser.SelectedNode = mTreeEntityBrowser.GetNodeAt(e.X, e.Y);

            if (mTreeEntityBrowser.SelectedNode != null)
            {
                // based on the node type, select the context menu to use
                IResource resource = Repository.Get(mTreeEntityBrowser.SelectedNode.Name);
                if (resource != null)
                    SelectContextMenu(resource, e.Location);
            }
        }

        private KeyPlugins.DragDropContext mDragContext;
        private bool mDraggingItem;
        private const int DRAG_START_DELAY = 150; // milliseconds
        private Stopwatch mDragDelayStopWatch;
        private void QueryDrag(object sender, EventArgs e)
        {
            // drag and drop sites
            // http://blogs.techrepublic.com.com/howdoi/?p=148
            // http://stackoverflow.com/questions/495666/c-drag-drop-from-listbox-to-treeview
            // http://www.codeproject.com/KB/cs/DragDropImage.aspx

            if (!mDraggingItem)
            {
                if (mDragDelayStopWatch == null)
                {
                    mDragDelayStopWatch = new Stopwatch();
                    mDragDelayStopWatch.Start();
                    return;
                }
                // if the time since the mouse down occurred and the current mouse move >= START_DRAG_INTERVAL then
                //  we initiate drag operation

                if (mDragDelayStopWatch.ElapsedMilliseconds >= DRAG_START_DELAY)
                {
                    mDraggingItem = true;
                    mDragContext = new KeyPlugins.DragDropContext();
                    mDragDelayStopWatch = null;
                    if (sender is TreeView && ((TreeView)sender).SelectedNode != null)
                    {
                        mDragContext.EntityID = ((TreeView)sender).SelectedNode.Name;
                        mDragContext.TypeName = ((TreeView)sender).SelectedNode.Text;
                        ((TreeView)sender).DoDragDrop(mDragContext, DragDropEffects.Move);
                    }
                    else if (sender is Control)
                        ((Control)sender).DoDragDrop(mDragContext, DragDropEffects.Copy);
                    else if (sender is DevComponents.DotNetBar.ButtonItem)
                    {
                        mDragContext.ModName = AppMain.ModName;  
                        mDragContext.ResourcePath = ((DevComponents.DotNetBar.ButtonItem)sender).Name;
 // TODO: FIX                       DoDragDrop(mDragContext, DragDropEffects.Copy);
                    }
 //                   else
 // TODO: FIX                       DoDragDrop(mDragContext, DragDropEffects.Copy);
                }
            }
        }

        private void EndDrag()
        {
            mDraggingItem = false;
            mDragDelayStopWatch = null;
            mDragContext = null;
        }


        /// <summary>
        /// This is called during the treeview event handling procedure at 
        /// KeyEdit.Workspace.EditorWorkspace.treeEntityBrowser_MouseUp
        /// <summary> 
        private void SelectContextMenu(IResource resource, Point location)
        {
            ContextMenuStrip menu = null;

            if (mTreeEntityBrowser.SelectedNode == null) return;

            // TODO: lets see if we can't just grab the right click menu from the plugin
            KeyPlugins.AvailablePlugin plugin = mCurrentPlugin;
            if (plugin != null && plugin.Instance != null)
            {
                string parentID = "";
                // if the very first node is ".." it means the parent is one level up 
                // and we've stored that parent's ID in the tag
                if (mTreeEntityBrowser.Nodes[0].Name == "..")
                {
                    // for the scene, get the root node ID
                    // which I suppose is the treeEntityBrowser's tag?
                    parentID = (string)mTreeEntityBrowser.Tag;
                }
                else if (mTreeEntityBrowser.SelectedNode.Parent == null)
                {
                    parentID = "";
                }
                else
                {
                    parentID = mTreeEntityBrowser.SelectedNode.Parent.Name;
                }
                // TODO: if we have entered a sub-entity, then the "..." might be the parent
                // if that's the case, then we look at the treeview tag to know the real parent
                
                // NOTE: GetContextMenu() base implementation exists in KeyPlugins.BasePlugiCtl.cs
                //       and is overridden in KeyPluginEntityEdit.BaseEntityPlugin.cs where the base
                //       implementation's menu items are retrieved as well.
                menu = plugin.Instance.GetContextMenu(resource.ID, parentID, location);
            }

            if (menu != null)
            {
                menu.Show(mTreeEntityBrowser, location);
            }
        }

        private void treeEntityBrowser_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            mTreeEntityBrowser.SelectedNode = mTreeEntityBrowser.GetNodeAt(e.X, e.Y);
            // following three lines involving the KeyPlugins.DragDropContext is handled in QueryDrag
            //KeyPlugins.DragDropContext context = new KeyPlugins.DragDropContext();
            //context.Data = treeEntityBrowser.SelectedNode;
            //treeEntityBrowser.DoDragDrop(context, DragDropEffects.Copy);
            EndDrag();

            if (e.Button == MouseButtons.Left)
            {
                // when clicking on a folder, i dont want to autocollapse, but when clicking on an item, perhaps i do
                Debug.Assert(mTreeEntityBrowser == sender);
                QueryDrag(sender, e);
            }
            else
            {
                // show a popup menu... or currently we're allowing the treeEntityBrowser_MouseUp to do that
                //ShowGalleryRightMouseClickMenu((ButtonItem)sender, (Control)((ButtonItem)sender).ContainerControl, e);
            }
        }

        private void treeEntityBrowser_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                QueryDrag(mTreeEntityBrowser, e);
            }
        }

        // This event is actually ONLY called on the final DROP action and not
        // during drag (so disregard the confusing event name).  
        // This event will trigger when dropping on either a treenode onto another treenode
        // or a treenode onto the assets (as in for saving an entity to .kgbentity)
        private void treeEntityBrowser_NodeDrag(object sender, ItemDragEventArgs itemArgs)
        {
            if (itemArgs != null && itemArgs.Item != null && itemArgs.Item is TreeNode)
            {
                TreeNode node = (TreeNode)itemArgs.Item;
                // we are attempting to re-parent an entity
                // DoDragDrop(node, DragDropEffects.Move);
                QueryDrag(sender, itemArgs);
            }
        }

        private void treeEntityBrowser_OnDragEnter(object sender, DragEventArgs drgevent)
        {
            System.Diagnostics.Debug.WriteLine("treeEntityBrowser.OnDragEnter() " + drgevent.ToString());
            if (sender as TreeView != null)
	            drgevent.Effect = DragDropEffects.Move;
            
            // drgevent.Data
        }

        private void treeEntityBrowser_OnDragOver(object sender, DragEventArgs drgevent)
        {
            System.Diagnostics.Debug.WriteLine("treeEntityBrowser.OnDragOver() " + drgevent.ToString());

            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = mTreeEntityBrowser.PointToClient(new Point(drgevent.X, drgevent.Y));
            //  Select the node at the mouse position.
            mTreeEntityBrowser.SelectedNode = mTreeEntityBrowser.GetNodeAt(targetPoint);

        }

        private void treeEntityBrowser_OnDragDrop(object sender, DragEventArgs drgevent)
        {
            System.Diagnostics.Debug.WriteLine("treeEntityBrowser.OnDragDrop() " + drgevent.ToString());
            KeyPlugins.DragDropContext context = (KeyPlugins.DragDropContext)drgevent.Data.GetData(typeof(KeyPlugins.DragDropContext));

            if (context != null && context.EntityID != null)
            {
            	// Retrieve the client coordinates of the drop location.
            	Point targetPoint = mTreeEntityBrowser.PointToClient(new Point(drgevent.X, drgevent.Y));

            	// Retrieve the node at the drop location.
            	TreeNode targetNode = mTreeEntityBrowser.GetNodeAt(targetPoint);
            	if (targetNode == null) return;
            
	
	            // Retreive the node that was dragged (note the following two lines replaces above two lines when using DragDropContext as opposed to built in TreeNode DragObject)
	            TreeNode[] foundNodes = mTreeEntityBrowser.Nodes.Find(context.EntityID, true);
	            if (foundNodes == null) return;
	            if (foundNodes.Length == 0) return;
	            if (foundNodes.Length != 1) throw new Exception("FormMain.Treeview() - Entity with same ID should not exist twice in the treeview.");
	            TreeNode draggedNode = foundNodes[0];
	            if (draggedNode == null) return;
	
	            // Confirm that the node at the drop location is not 
	            // the dragged node or a descendant of the dragged node.
	            if (!draggedNode.Equals(targetNode) && !ContainsNode(draggedNode, targetNode))
	            {
	                // If it is a move operation, remove the node from its current 
	                // location and add it to the node at the drop location.
	                if (drgevent.Effect == DragDropEffects.Move)
	                {
	                    // IMPORTANT: TODO: if double clicked on the sub node, the Root (or whatever other
	                    // parent will not be in the treeview, it'll be "..." so this will fail.
	                    // For now, make sure the entire tree of entities is visible.
                        // send command to move this node, NOTE: the move may be rejected
                		// if the type is not allowed
	                    KeyCommon.Messages.Node_ChangeParent changeParent = new KeyCommon.Messages.Node_ChangeParent(draggedNode.Name, targetNode.Name, draggedNode.Parent.Name);
	                    AppMain.mNetClient.SendMessage(changeParent);
	                }
	
	                // If it is a copy operation, clone the dragged node 
	                // and add it to the node at the drop location.
	                else if (drgevent.Effect == DragDropEffects.Copy)
	                {
	                    // TODO: not implemented.  I should only follow thru with copy
	                    // if server authorizes
	                    // targetNode.Nodes.Add((TreeNode)draggedNode.Clone());
	                }
	
	                // Expand the node at the location 
	                // to show the dropped node.
	                targetNode.Expand();
	            }
            }
        }

        // Determine whether one node is a parent 
        // or ancestor of a second node.
        private bool ContainsNode(TreeNode node1, TreeNode node2)
        {
            // Check the parent node of the second node.
            if (node2.Parent == null) return false;
            if (node2.Parent.Equals(node1)) return true;

            // If the parent node is not null or equal to the first node, 
            // call the ContainsNode method recursively using the parent of 
            // the second node.
            return ContainsNode(node1, node2.Parent);
        }


        private void treeEntityBrowser_OnDragLeave(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("treeEntityBrowser.OnDragLeave() ");
        }
        #endregion
    }
}
