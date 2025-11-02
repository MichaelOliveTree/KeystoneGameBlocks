using System;
using System.Collections.Generic;
using System.Text;
using Core.Elements;

namespace GUI
{
    // TODO: Right now this contains both Graphical Scene stuff and Controller State stuff.  This needs to be split.
    // TODO: Lots to clean up here.  For instance, I have Traversers for things like Render whereas this uses a virtual method.
    // TODO: Also consider how we use Appearance and such still as well as switch and such for doing rollovers and such.
    //       so our GUI controller code would be responsible for changing the order of things after the switch or setting a "current" index
    //       in that switch.
    // As an optimization, it'd be good to pack our near plane GUI textures into a single texture as well as use a single vertex buffer
    // and ideally, a single drawprimitive call similar to what we do with Imposters.  
    // Further, this saves us from needing a fat TVMesh for each, we'll just be using our own vertex buffer management...
    public abstract class Control : Element
    {
        public override void Traverse(Core.Traversers.ITraverser target)
        {
            target.Apply(this);
        }
        
        /// <summary>Base class for all controls</summary>

        #region Instance data
        
        public uint index; // Index within the control list
        public bool isDefault;

        // Protected members
        protected object localUserData; // User specificied data
        protected bool visible;
        protected bool isMouseOver;
        protected bool hasFocus;
        protected int controlId; // ID Number
        protected ControlType controlType; // Control type, set in constructor
        
    
        protected Dialog parentDialog; // Parent container
        protected System.Windows.Forms.Keys hotKey; // Controls hotkey
        protected System.Drawing.Rectangle boundingBox; // Rectangle defining the active region of the control

        // todo: we are going to use our Position.X,.Y,.Z so that our controls can exist on door keypads and such
        // TODO: WAIT!  Actually these 2d positions will be fine and we'll just unproject these into world space
        // for drawing.  The reason is it's simpler to have a consistant render this way and picking too is easier when
        // our underlying 3d coords are always accurate.
        // IMPORTANT:  See public PickResults[] Pick(int mouseX, int mouseY) for code on how to UnProject a 2d coordinate
        // into world space.  
        // Alternatively there is TVMaths.GetMousePickVectors (x, z, ref nearStart, ref farEnd)
        protected int controlX,controlY,width,height; // Size, scale, and positioning members
        private Core.Core _core;
        
        
        // todo: this should be apart of CompoundControl which inherits from Group  
        // and then we also have Dialog which inherits from CompoundControl and can host any other control
        // either simple or Compound
        
        protected List <Control> elementList = new List <Control>(); // All display elements
        #endregion

        /// <summary>Initialize the control</summary>
        public virtual void OnInitialize() {} // Nothing to do here
        /// <summary>Render the control</summary>
        public virtual void Render(Device device, float elapsedTime) {} // Nothing to do here
        /// <summary>Message Handler</summary>
        public virtual bool MsgProc(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam) {return false;} // Nothing to do here
        /// <summary>Handle the keyboard data</summary>
        public virtual bool HandleKeyboard(NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam) {return false;} // Nothing to do here
        /// <summary>Handle the mouse data</summary>
        public virtual bool HandleMouse(NativeMethods.WindowMessage msg, System.Drawing.Point pt, IntPtr wParam, IntPtr lParam) {return false;} // Nothing to do here

        /// <summary>User specified data</summary>
        public object UserData { get { return localUserData; } set { localUserData = value; } }
        /// <summary>The parent dialog of this control</summary>
        public Dialog Parent { get { return parentDialog; } }
        /// <summary>Can the control have focus</summary>
        public virtual bool CanHaveFocus { get { return false; } }
        /// <summary>Called when control gets focus</summary>
        public virtual void OnFocusIn() { hasFocus = true;}
        /// <summary>Called when control loses focus</summary>
        public virtual void OnFocusOut() { hasFocus = false;}
        /// <summary>Called when mouse goes over the control</summary>
        public virtual void OnMouseEnter() { isMouseOver = true;}
        /// <summary>Called when mouse leaves the control</summary>
        public virtual void OnMouseExit() { isMouseOver = false;}
        /// <summary>Called when the control's hotkey is hit</summary>
        public virtual void OnHotKey() {} // Nothing to do here
        /// <summary>Does the control contain this point</summary>
        public virtual bool ContainsPoint(System.Drawing.Point pt) { return boundingBox.Contains(pt); }
        /// <summary>Is the control enabled</summary>
        public virtual bool IsEnabled { get { return _enable; } set { _enable = value; } }
        /// <summary>Is the control visible</summary>
        public virtual bool IsVisible { get { return visible; } set { visible = value; } }
        /// <summary>Type of the control</summary>
        public virtual ControlType ControlType { get { return controlType; } }
        /// <summary>Unique ID of the control</summary>
        public virtual int ID { get { return controlId; } set { controlId = value; } }
        /// <summary>Called to set control's location</summary>
        public virtual void SetLocation(int x, int y) { controlX = x; controlY = y; UpdateRectangles(); }
        /// <summary>Called to set control's size</summary>
        public virtual void SetSize(int w, int h) { width = w; height = h; UpdateRectangles(); }
        /// <summary>The controls hotkey</summary>
        public virtual System.Windows.Forms.Keys Hotkey { get { return hotKey; } set { hotKey = value; } }

        /// <summary>
        /// Index for the elements this control has access to
        /// </summary>
        public Element this[uint index]
        {
            get { return elementList[(int)index] as Element; }
            set 
            { 
                if (value == null)
                    throw new ArgumentNullException("ControlIndexer", "You cannot set a null element.");
                
                // Is the collection big enough?
                for(uint i = (uint)elementList.Count; i <= index; i++)
                {
                    // Add a new one
                    elementList.Add(new Element());
                }
                // Update the data (with a clone)
                elementList[(int)index] = value.Clone();
            }
        }
        /// <summary>
        /// Create a new instance of a control
        /// </summary>
        protected Control(Dialog parent)
        {
            controlType = ControlType.Button;
            parentDialog = parent;
            controlId = 0;
            index = 0;

            _enable = true;
            visible = true;
            isMouseOver = false;
            hasFocus = false;
            isDefault = false;

            controlX = 0; controlY = 0; width = 0; height = 0;
        }

        /// <summary>
        /// Refreshes the control
        /// </summary>
        public virtual void Refresh()
        {
            isMouseOver = false;
            hasFocus = false;
            for(int i = 0; i < elementList.Count; i++)
            {
                (elementList[i] as Element).Refresh();
            }
        }

        /// <summary>
        /// Updates the rectangles
        /// </summary>
        protected virtual void UpdateRectangles()
        {
            boundingBox = new System.Drawing.Rectangle(controlX, controlY, width, height);
            
        }
    }        
 }

