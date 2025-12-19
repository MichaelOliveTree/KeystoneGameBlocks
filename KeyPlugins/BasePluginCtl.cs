using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using KeyPlugins;


namespace KeyPlugins
{
    // NOTE: This cannot be abstract because the IDE Designer for all derived plugins will not be able to render
    public partial class BasePluginCtl : UserControl, IPlugin
    { 

        // Queue is first in, first out.  (stack is last in, first out)
        protected Queue<EventHandler> mEvents;
        protected Queue<object> mSenders;
        protected Queue<EventArgs> mEventArgs;

        protected string mName = "Keystone Plugin";
        protected string mSceneName = "";
        protected string mTargetNodeID = "";
        protected string mTargetTypename = "Entity";
        protected string[] mSupportedTypenames;
        protected string mProfile; // a plugin can be selected based on it's profile or supported typenames

        protected string mAuthor = "Michael P. Joseph";
        protected string mDescription = "A plugin control for configuring a scene";
        protected string mVersion = "1.0.0";
        protected IPluginHost mHost;

        protected string mModFolderPath;
        protected string mModName;
        
        // Typically every entity has a tree of child nodes branching from it.
        // This Plugin needs to notify the plugin host that it wants to be
        // notified anytime any of these nodes are changed.  This way we can update
        // the panels.

        // The second task is to track the current selected superTabItem# and to 
        // maintain the state of those items so that when we do update and then return
        // to those superTabItems, we can reproduce states such as which tree items are expanded
        // and which is currently selected (eg appearanceGroups)

        protected BasePluginCtl()
        {
            InitializeComponent();
        }

        
        protected BasePluginCtl(IPluginHost host, string modspath, string modname)
        {
            InitializeComponent();
            mHost = host;
            
            mModFolderPath = modspath;
            mModName = modname;
        }

        #region IPlugin Members
        public void Initialize()
        {
            // TODO: here's a problem... how do we sync the format of the state
            // i could device a general format for the way all state is passed
            // nodeID:
            //      stateName, stateValue; stateName, stateValue;
            //      

            // then have the IPluginInterface control the parsing to/from
            // not the underlying Node?  i mean no  node.Serialize()
            // The good part about receiving it back as XML however is that
            // every plugin can examin the XML and figure out how to write a GUI for it
            // even if the docs are weak.

            // but what if returning from the network, the full state is not XML?  What if its
            // our IRemotable binary form...  would we then have to convert that to XML so
            // that the plugin could deal with it?  What if instead we simply returned the IRemotable binary
            // formatted type?  And then each IRemotable type can access the same lib for remoting
            // and we dont have duplicate code..

            //mHost.GetState(); // get the state of the selected node

        }


        public IPluginHost Host
        {
            get { return mHost; }
        }

        // TODO: this plugin should be able to subscribe to events and then receive notifications when those
        // events occur... events such as when a node is added/removed, when a child of the current node is added/removed
        // when the plugin is Activated or Deactivated (node of the proper type is selected and then unselected), etc
        // the node's state/attributes/properties has changed 

        public EventHandler EditResourceRequest { get; set; }


        public virtual void SelectTarget(string sceneName, string id, string typeName)
        {
            mSceneName = sceneName;
            mTargetNodeID = id;
            mTargetTypename = typeName;
        }

        public virtual void ProcessEventQueue()
        {
            object sender = mSenders.Dequeue();
            EventArgs args = mEventArgs.Dequeue();
            EventHandler handler = mEvents.Dequeue();

            while (handler != null)
            { 
                handler.Invoke(sender, args);
                sender = mSenders.Dequeue();
                args = mEventArgs.Dequeue();
                handler = mEvents.Dequeue();
            }
        }

        protected void EnqueueEvent (EventHandler handler, object sender, EventArgs args)
        {
            if (mEvents == null)
            {
                mEvents = new Queue<EventHandler>();
                mSenders = new Queue<object>();
                mEventArgs = new Queue<EventArgs>();
            }
            mEvents.Enqueue(handler);
            mSenders.Enqueue(sender);
            mEventArgs.Enqueue(args);
        }

        public virtual void WatchedNodeCustomPropertyChanged(string id, string typeName)
        {
            //throw new NotImplementedException("This must be overridden");
        }

        public virtual void WatchedNodePropertyChanged(string id, string typeName) 
        {
            //throw new NotImplementedException("This must be overridden");
        }

        public virtual void WatchedNodeAdded(string parent, string child, string typeName)
        {
            //throw new NotImplementedException("This must be overridden");
        }

        public virtual void WatchedNodeRemoved(string parent, string child, string typeName)
        {
            //throw new NotImplementedException("This must be overridden");
        }

        public virtual void WatchedNodeMoved(string oldParent, string newParent, string child, string typeName)
        {
            //throw new NotImplementedException("This must be overridden");
        }

        public string TargetID { get { return mTargetNodeID; } }
        public string TargetTypename { get { return mTargetTypename; } }
        public string[] SupportedTypes { get { return mSupportedTypenames; } }
        public string Profile { get { return mProfile; } }

        public bool ContainsSupportedType(string typename)
        {
            if (mSupportedTypenames == null || mSupportedTypenames.Length == 0) return false;

            for (int i = 0; i < mSupportedTypenames.Length; i++)
                if (mSupportedTypenames[i] == typename) return true;

            return false;
        }

        public new string Name { get { return mName; } set { mName = value; } }

        public string Description
        {
            get { return mDescription; }
        }

        // note: Name property is implemented by UserControl which we inherit

        public string Author
        {
            get { return mAuthor; }
        }

        public string Version
        {
            get { return mVersion; }
        }

#if DEBUG
        public string Summary
        {
            get { return Name + "(" + Version + ")" + "By: " + Author + Description;}
        }
#endif

        public UserControl MainInterface
        {
            get { return this; }
        }

        
        /// <summary>
        /// This is called by EditorWorkspace.Treeview.cs.SelectContextMenu()
        /// which itself is called during the treeview event handling procedure at 
        /// KeyEdit.Workspace.EditorWorkspace.treeEntityBrowser_MouseUp
        /// NOTE: Plugins that OVERRIDE this virtual function USUALLY will call
        ///       ContextMenuStrip menu = base.ContextMenuStrip(...) at the very beginning
        ///       of the overrided implementation, and then build off the base
        ///       ContextMenuStrip that is created here.
        /// </summary>
        public virtual ContextMenuStrip GetContextMenu(string resourceID, string parentID, System.Drawing.Point location)
        {
            contextMenuStrip.Items.Clear();

            ToolStripMenuItem menuItem = new ToolStripMenuItem("Rename");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(Rename_Click);
            contextMenuStrip.Items.Add(menuItem);

            ToolStripSeparator seperator = new ToolStripSeparator();
            contextMenuStrip.Items.Add(seperator);

            menuItem = new ToolStripMenuItem("Delete");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(Delete_Click);
            contextMenuStrip.Items.Add(menuItem);
                       

            menuItem = new ToolStripMenuItem("Copy");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(Copy_Click);
            contextMenuStrip.Items.Add(menuItem);

            menuItem = new ToolStripMenuItem("Paste");
            menuItem.Name = resourceID;
            menuItem.Tag = parentID;
            menuItem.Click += new EventHandler(Paste_Click);
            //TODO: only if there does NOT exist a copy in the copy buffer, disable the paste item
            menuItem.Enabled = false;
            contextMenuStrip.Items.Add(menuItem);


            contextMenuStrip.Items[2].Enabled = true;
            return contextMenuStrip;
        }
        #endregion


        public void Delete_Click(object sender, EventArgs e)
        {
            // entity cant be shared so it's parent id is not needed
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            if (!(sender is ToolStripMenuItem)) return;

            string childID = (string)item.Name; 
            string  parentID = (string)item.Tag;
            mHost.Node_Remove(childID, parentID);
            // todo: this will fail on certain nodes in our plugin that aren't Nodes such as Emitters and Attractors
            System.Diagnostics.Debug.WriteLine("Node_Remove command sent...");
        }


        private void Rename_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            if (!(sender is ToolStripMenuItem)) return;

            string childID = (string)item.Name;
            string parentID = (string)item.Tag;

            // TODO: init userText with value of current name (can be null)
            string userText = null;
            userText = (string)mHost.Node_GetProperty(childID, "name");

            System.Windows.Forms.DialogResult result = KeyPlugins.StaticControls.InputBox("Enter name of Entity:", "enter new entity name", ref userText);
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // rename the friendly name only!  Not the GUID
                mHost.Node_ChangeProperty(childID, "name", typeof(string), userText);
                // TODO: update the rename of this node in the treeview
                // TODO: does the scene re-save after ChangeProperty calls?`
                // this is part of the problem with having the right mouseclick contextmenu
                // in the plugin. 
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
         //   mHost.SendMessage(((ToolStripMenuItem)sender).Tag.ToString(), 4, null);
            MessageBox.Show("BasePluginCtl.Copy_Click()");
        }

        private void Paste_Click(object sender, EventArgs e)
        {
         //   mHost.SendMessage(((ToolStripMenuItem)sender).Tag.ToString(), 5, null);
            MessageBox.Show("BasePluginCtl.Paste_Click()");
        }
    }
}
