//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Keystone.GUI
//{
//    public class EvoGUI
//    {
//        public static string BASE_PATH;

//        public TVTextureFactory Tex;
//        public TVScreen2DImmediate Scr2D;
//        public TVScreen2DText ScrText;
//        public TVGlobals Utils;
//        public TVScene Scene;
//        public TVInputEngine Inputs;
//        public Device d3dDevice;
//        public Sprite d3d_Sprite;
//        public TVInternalObjects TVIO;

//        public TVEngine Tv;
//        public float Milliseconds;
//        public int CurColKey;
//        public int TextCursorFlashDelay;
//        public int SpinnerUpdateDelay;
//        public int SpinnerFirstClickDelay;
//        public bool TextInputActive;
//        public bool ResizingAWindow;


//        public string SkinsFolderName;
//        public string ImagesFolderName;

//        private int mouse_x;
//        private int mouse_x_prev;
//        private int mouse_y;
//        private int mouse_y_prev;
//        private bool mouse_b1;
//        private bool mouse_b1_prev;
//        private bool mouse_b2;
//        private bool mouse_b2_prev;
//        private bool mouse_b3;
//        private bool mouse_b3_prev;
//        private bool mouse_b1_pressed;
//        private bool mouse_b2_pressed;
//        private bool mouse_b3_pressed;
//        public int mouse_offset_x;
//        //Number of pixels to offset from the top left corner of the mouse cursor, 
//        public int mouse_offset_y;
//        //such as when needing to center the cursor picture

//        public EvoGUI_Texture tex_cursor_arrow;
//        public EvoGUI_Texture tex_cursor_text;
//        public EvoGUI_Texture tex_cursor_bright;
//        public EvoGUI_Texture tex_cursor_bleft;
//        public EvoGUI_Texture tex_cursor_leftright;
//        public EvoGUI_Texture tex_cursor_updown;
//        public EvoGUI_Texture tex_cursor_column;

//        private string CurrentCursorName;
//        private float Cursor_u2;
//        private float Cursor_v2;

//        private EvoGUI_D3DFont CurFont2;
//        private bool MouseOverMenu;
//        private bool MouseOverComboBox;
//        private bool TextCursorState;
//        private int PreviousTextCursorTime;

//        public event ButtonEventEventHandler ButtonEvent;
//        public delegate void ButtonEventEventHandler(string Name);
//        public event StateChangeEventHandler StateChange;
//        public delegate void StateChangeEventHandler(string CallerName, string id);
//        public event MenuClickEventHandler MenuClick;
//        public delegate void MenuClickEventHandler(string Name);

//        public int MouseIsOverWindowIndex;
//        public int FocusedWindowIndex;

//        public static Collection EvoGUI_col = new Collection();


//        public EvoGUI(TVInputEngine TVInput)
//        {
//            this.Tex = new TVTextureFactory();
//            this.Scr2D = new TVScreen2DImmediate();
//            this.ScrText = new TVScreen2DText();
//            this.Utils = new TVGlobals();
//            this.Scene = new TVScene();
//            this.Inputs = TVInput;
//            BASE_PATH = Application.StartupPath.Substring(0, Application.StartupPath.LastIndexOf("\\bin\\")) + "\\";
//            SkinsFolderName = "Skins";
//            ImagesFolderName = "Images";
//            this.TVIO = new TVInternalObjects();

//            Scr2D.Settings_SetTextureFilter(CONST_TV_TEXTUREFILTER.TV_FILTER_BILINEAR);

//            this.Tv = new TVEngine();

//            this.TextCursorFlashDelay = 500;
//            SpinnerUpdateDelay = 70;
//            SpinnerFirstClickDelay = 300;
//            CurColKey = 0;

//            this.tex_cursor_arrow = new EvoGUI_Texture("cursor_arrow", "Cursors");
//            this.tex_cursor_text = new EvoGUI_Texture("cursor_text", "Cursors");
//            this.tex_cursor_bleft = new EvoGUI_Texture("cursor_bleft", "Cursors");
//            this.tex_cursor_bright = new EvoGUI_Texture("cursor_bright", "Cursors");
//            this.tex_cursor_leftright = new EvoGUI_Texture("cursor_leftright", "Cursors");
//            this.tex_cursor_updown = new EvoGUI_Texture("cursor_updown", "Cursors");
//            this.tex_cursor_column = new EvoGUI_Texture("cursor_column", "Cursors");

//            this.CurrentCursorName = this.tex_cursor_arrow.Name;
//            this.Cursor_u2 = this.tex_cursor_arrow.u2;
//            this.Cursor_v2 = this.tex_cursor_arrow.v2;
//            this.mouse_offset_x = 0;
//            this.mouse_offset_y = 0;

//            this.d3dDevice = new Device(this.TVIO.GetDevice3D);

//            this.d3dDevice.SetRenderState(RenderStates.SeparateAlphaBlendEnable, true);
//            this.d3dDevice.SetRenderState(RenderStates.SourceBlendAlpha, 5);
//            //5 = SRCALPHA
//            this.d3dDevice.SetRenderState(RenderStates.DestinationBlendAlpha, 11);
//            //11 = D3DBLEND_SRCALPHASAT

//            EvoGUI_col.Add(this, "engine");
//        }

//        public void Draw()
//        {
//            int MaxWP1 = 0;
//            int MaxWP2 = 0;
//            int MinWP1 = 0;
//            int MinWP2 = 0;
//            int CurWP1 = 0;
//            int CurWP2 = 0;
//            //Current window priorirty
//            int WinNumP1 = 0;
//            int WinNumP2 = 0;
//            int i = 0;

//            int LastDrawnP1 = 0;
//            int LowestEncounteredP1 = 0;
//            int CurrentCandidateP1 = 0;
//            //holds window index
//            LowestEncounteredP1 = -1;
//            //lowest encountered in the last priorirty 1 list check
//            LastDrawnP1 = -1;
//            //last drawn priority 1


//            CurWP1 = -1;
//            CurWP2 = -1;
//            MinWP1 = -1;
//            MinWP2 = -1;
//            MaxWP1 = -1;
//            MaxWP2 = -1;
//            WinNumP1 = 0;
//            WinNumP2 = 0;
//            //Draw windows in the order of their respective priorities

//            //First draw from lowest to highest priority1 windows (that have show=true):
//            foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                //1-determine how many shown windows there are
//                if (EvoGUI_Window_item.IsOpen == true)
//                {
//                    WinNumP1 += 1;
//                }
//            }

//            //Improvement needed: store the order of the windows in an array, and update it only once a window changes focus
//            //(doing this check for/each every loop is a little slower)

//            //Form1.debug4 = "WinNumP1: " + CStr(WinNumP1) 'this equals 2
//            for (i = 1; i <= WinNumP1; i++)
//            {
//                foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                {
//                    if (EvoGUI_Window_item.IsOpen == true)
//                    {
//                        //-If current candidate is greater than the last drawn then
//                        if (EvoGUI_Window_item.Priority1 > LastDrawnP1)
//                        {
//                            //--See if it is lower than all the other candidates encountered so far
//                            //If its the first so far
//                            if (LowestEncounteredP1 == -1)
//                            {
//                                CurrentCandidateP1 = EvoGUI_Window_item.Index;
//                                LowestEncounteredP1 = EvoGUI_Window_item.Priority1;
//                            }
//                            if (EvoGUI_Window_item.Priority1 < LowestEncounteredP1)
//                            {
//                                CurrentCandidateP1 = EvoGUI_Window_item.Index;
//                                LowestEncounteredP1 = EvoGUI_Window_item.Priority1;
//                            }
//                        }
//                    }
//                    //'---if yes, leave it as current minimum, then draw it and reset LastDrawnP1
//                }
//                //Now we know the CurrentCandidateP1, so draw the window with that Index
//                foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                {
//                    if (EvoGUI_Window_item.Index == CurrentCandidateP1)
//                    {
//                        EvoGUI_Window_item.Draw();
//                        LastDrawnP1 = EvoGUI_Window_item.Priority1;
//                    }
//                }
//                LowestEncounteredP1 = -1;
//            }


//            //Loop until number of drawn windows = WinNumP1


//            //Draw all the open menus:
//            DrawOpenMenus();

//            DrawOpenComboBoxes();

//            DrawTextCursor();

//            //Draw the mouse on top
//            this.Scr2D.Draw_Texture(this.Utils.GetTex(this.CurrentCursorName), this.mouse_x - this.mouse_offset_x, this.mouse_y - this.mouse_offset_y, this.mouse_x - this.mouse_offset_x + this.tex_cursor_arrow.Width - 1, this.mouse_y - this.mouse_offset_x + this.tex_cursor_arrow.Height - 1, -1, -1, -1, -1, 0,
//            0, this.Cursor_u2, this.Cursor_v2);
//        }

//        public void Update()
//        {
//            int MaxWindowPriority1 = 0;
//            int MaxWindowPriority2 = 0;
//            //counting only the windows that are stacked under the mouse
//            int MaxTotalWindowPriority1 = 0;
//            int MaxTotalWindowPriority2 = 0;
//            //counting all the windows for displaying which is on top
//            int i = 0;
//            int csx1 = 0;
//            int csx2 = 0;
//            int csy1 = 0;
//            int csy2 = 0;
//            int x1 = 0;
//            int y1 = 0;
//            int x2 = 0;
//            int y2 = 0;
//            bool DefaultCursor = false;

//            bool MouseStateChange = false;

//            this.Inputs.GetAbsMouseState(this.mouse_x, this.mouse_y, this.mouse_b1, this.mouse_b2);

//            if (mouse_x == mouse_x_prev & mouse_y == mouse_y_prev & mouse_b1 == mouse_b1_prev & mouse_b2 == mouse_b2_prev)
//            {
//                MouseStateChange = false;
//            }
//            else
//            {
//                MouseStateChange = true;
//            }

//            mouse_x_prev = mouse_x;
//            mouse_y_prev = mouse_y;
//            mouse_b1_prev = mouse_b1;
//            mouse_b2_prev = mouse_b2;

//            this.Milliseconds += this.Tv.AccurateTimeElapsed();

//            if (MouseStateChange)
//            {
//                //Optimisation: Check if the mouse state has changed (movement/buttons). 
//                //If not, there is no need to check and update various parts

//                CheckMouseOverMenus(mouse_x, mouse_y);

//                CheckMouseOverComboBox(mouse_x, mouse_y);

//                MaxWindowPriority1 = 0;
//                MaxWindowPriority2 = 0;
//                this.MouseIsOverWindowIndex = -1;

//                //-First check on top of which window the mouse is
//                //:Check for each window if mouse is over, and leave it as on for the highest priority
//                if (MouseOverMenu == false & MouseOverComboBox == false)
//                {

//                    foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        if (EvoGUI_Window_item.IsOpen == true)
//                        {
//                            if (this.mouse_x >= EvoGUI_Window_item.x & this.mouse_x < EvoGUI_Window_item.x + EvoGUI_Window_item.Width)
//                            {
//                                if (this.mouse_y >= EvoGUI_Window_item.y & this.mouse_y < EvoGUI_Window_item.y + EvoGUI_Window_item.Height)
//                                {
//                                    //This means the mouse is over the window 
//                                    //Check if it is the highest priority so far
//                                    if (EvoGUI_Window_item.Priority2 > MaxWindowPriority2)
//                                    {
//                                        //if it is set it as the one on top
//                                        MaxWindowPriority2 = EvoGUI_Window_item.Priority2;
//                                        this.MouseIsOverWindowIndex = EvoGUI_Window_item.Index;
//                                    }
//                                    else if (EvoGUI_Window_item.Priority1 > MaxWindowPriority1)
//                                    {
//                                        MaxWindowPriority1 = EvoGUI_Window_item.Priority1;
//                                        this.MouseIsOverWindowIndex = EvoGUI_Window_item.Index;
//                                    }
//                                }
//                                else
//                                {
//                                    //If it isn't over that window, unhighlight all the buttons on that window
//                                    if (EvoGUI_Window_item.SomethingHighlighted == true) EvoGUI_Window_item.UnhighlightAll();
//                                }
//                            }
//                            else
//                            {
//                                if (EvoGUI_Window_item.SomethingHighlighted == true) EvoGUI_Window_item.UnhighlightAll();
//                            }
//                        }
//                    }

//                    MaxTotalWindowPriority2 = -1;
//                    MaxTotalWindowPriority1 = -1;

//                    foreach (EvoGUI_Window EvoGUI_Window_item2 in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        //look through all the windows and find max priority2
//                        if (EvoGUI_Window_item2.Priority2 > MaxTotalWindowPriority2)
//                        {
//                            MaxTotalWindowPriority2 = EvoGUI_Window_item2.Priority2;
//                        }
//                    }

//                    foreach (EvoGUI_Window EvoGUI_Window_item2 in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        //look through all the windows and find max priority1
//                        if (EvoGUI_Window_item2.Priority1 > MaxTotalWindowPriority1)
//                        {
//                            MaxTotalWindowPriority1 = EvoGUI_Window_item2.Priority1;
//                        }
//                    }

//                    //Check which cursor to draw, and all mouse-over effects::
//                    //Check if the cursor is on any of the corners
//                    //First check if the mouse is not over any window 
//                    if (MouseIsOverWindowIndex == -1 | MouseIsOverWindowIndex != FocusedWindowIndex)
//                    {
//                        //This is because sometimes when resizing a window the cursor is actualy on the outside of it
//                        if (ResizingAWindow == false)
//                        {
//                            this.CurrentCursorName = this.tex_cursor_arrow.Name;
//                            this.Cursor_u2 = this.tex_cursor_arrow.u2;
//                            this.Cursor_v2 = this.tex_cursor_arrow.v2;
//                            this.mouse_offset_x = 0;
//                            this.mouse_offset_y = 0;
//                        }
//                    }
//                    else
//                    {
//                        foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                        {
//                            if (this.MouseIsOverWindowIndex == EvoGUI_Window_item.Index)
//                            {
//                                //and do this only if it is the active window
//                                if (EvoGUI_Window_item.Priority1 == MaxTotalWindowPriority1 | MaxTotalWindowPriority2 > 0 & EvoGUI_Window_item.Priority2 == MaxTotalWindowPriority2)
//                                {

//                                    DefaultCursor = true;

//                                    if (!EvoGUI_Window_item.Borderless & !EvoGUI_Window_item.Minimised)
//                                    {

//                                        //Check right border (this and the following few cursor change checks are only for windows with borders)
//                                        x1 = EvoGUI_Window_item.x + EvoGUI_Window_item.Width - EvoGUI_Window_item.ntex_right.Width;
//                                        y1 = EvoGUI_Window_item.y + EvoGUI_Window_item.ntex_top.Height;
//                                        x2 = x1 + EvoGUI_Window_item.ntex_right.Width;
//                                        y2 = EvoGUI_Window_item.y + EvoGUI_Window_item.Height - EvoGUI_Window_item.ntex_bottom.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_right == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = tex_cursor_leftright.Name;
//                                            Cursor_u2 = tex_cursor_leftright.u2;
//                                            Cursor_v2 = tex_cursor_leftright.v2;
//                                            mouse_offset_x = tex_cursor_leftright.Width / 3;
//                                            mouse_offset_y = tex_cursor_leftright.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //Check left border
//                                        x1 = EvoGUI_Window_item.x;
//                                        y1 = EvoGUI_Window_item.y + EvoGUI_Window_item.ntex_top.Height;
//                                        x2 = x1 + EvoGUI_Window_item.ntex_left.Width;
//                                        y2 = EvoGUI_Window_item.y + EvoGUI_Window_item.Height - EvoGUI_Window_item.ntex_bottom.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_left == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = tex_cursor_leftright.Name;
//                                            Cursor_u2 = tex_cursor_leftright.u2;
//                                            Cursor_v2 = tex_cursor_leftright.v2;
//                                            mouse_offset_x = tex_cursor_leftright.Width / 3;
//                                            mouse_offset_y = tex_cursor_leftright.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //check top border
//                                        x1 = EvoGUI_Window_item.x + EvoGUI_Window_item.ntex_left.Width;
//                                        y1 = EvoGUI_Window_item.y;
//                                        x2 = EvoGUI_Window_item.x + EvoGUI_Window_item.Width - EvoGUI_Window_item.ntex_right.Width;
//                                        y2 = EvoGUI_Window_item.y + EvoGUI_Window_item.ntex_bottom.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_top == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = tex_cursor_updown.Name;
//                                            Cursor_u2 = tex_cursor_updown.u2;
//                                            Cursor_v2 = tex_cursor_updown.v2;
//                                            mouse_offset_x = tex_cursor_updown.Width / 3;
//                                            mouse_offset_y = tex_cursor_updown.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //b0tt0m b0rdeR___
//                                        x1 = EvoGUI_Window_item.x + EvoGUI_Window_item.ntex_bottom_left.Width;
//                                        y1 = EvoGUI_Window_item.y + EvoGUI_Window_item.Height - EvoGUI_Window_item.ntex_bottom.Height;
//                                        x2 = EvoGUI_Window_item.x + EvoGUI_Window_item.Width - EvoGUI_Window_item.ntex_bottom_right.Width;
//                                        y2 = EvoGUI_Window_item.y + EvoGUI_Window_item.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_bottom == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = tex_cursor_updown.Name;
//                                            Cursor_u2 = tex_cursor_updown.u2;
//                                            Cursor_v2 = tex_cursor_updown.v2;
//                                            mouse_offset_x = tex_cursor_updown.Width / 3;
//                                            mouse_offset_y = tex_cursor_updown.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //Check bottom right corner
//                                        x1 = EvoGUI_Window_item.x + EvoGUI_Window_item.Width - EvoGUI_Window_item.ntex_bottom_right.Width;
//                                        y1 = EvoGUI_Window_item.y + EvoGUI_Window_item.Height - EvoGUI_Window_item.ntex_bottom_right.Height;
//                                        x2 = x1 + EvoGUI_Window_item.ntex_bottom_right.Width;
//                                        y2 = y1 + EvoGUI_Window_item.ntex_bottom_right.Height;
//                                        //Form1.debug2 = "br x1: " + CStr(x1) + " y1: " + CStr(y1) + " x2: " + CStr(x2) + " y2: " + CStr(y2)
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_br == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = tex_cursor_bright.Name;
//                                            Cursor_u2 = tex_cursor_bright.u2;
//                                            Cursor_v2 = tex_cursor_bright.v2;
//                                            mouse_offset_x = tex_cursor_bright.Width / 3;
//                                            mouse_offset_y = tex_cursor_bright.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //top right
//                                        x1 = EvoGUI_Window_item.x + EvoGUI_Window_item.Width - EvoGUI_Window_item.ntex_right.Width;
//                                        y1 = EvoGUI_Window_item.y;
//                                        x2 = x1 + EvoGUI_Window_item.ntex_right.Width;
//                                        y2 = y1 + EvoGUI_Window_item.ntex_top_right.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_br == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = this.tex_cursor_bleft.Name;
//                                            Cursor_u2 = tex_cursor_bleft.u2;
//                                            Cursor_v2 = tex_cursor_bleft.v2;
//                                            mouse_offset_x = tex_cursor_bleft.Width / 3;
//                                            mouse_offset_y = tex_cursor_bleft.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //top left
//                                        x1 = EvoGUI_Window_item.x;
//                                        y1 = EvoGUI_Window_item.y;
//                                        x2 = x1 + EvoGUI_Window_item.ntex_left.Width;
//                                        y2 = y1 + EvoGUI_Window_item.ntex_top.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_tl == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = this.tex_cursor_bright.Name;
//                                            Cursor_u2 = tex_cursor_bright.u2;
//                                            Cursor_v2 = tex_cursor_bright.v2;
//                                            mouse_offset_x = tex_cursor_bright.Width / 3;
//                                            mouse_offset_y = tex_cursor_bright.Height / 3;
//                                            DefaultCursor = false;
//                                        }

//                                        //top left
//                                        x1 = EvoGUI_Window_item.x;
//                                        y1 = EvoGUI_Window_item.y + EvoGUI_Window_item.Height - EvoGUI_Window_item.ntex_bottom.Height;
//                                        x2 = x1 + EvoGUI_Window_item.ntex_left.Width;
//                                        y2 = y1 + EvoGUI_Window_item.ntex_bottom.Height;
//                                        if ((mouse_x >= x1 & mouse_x < x2 & mouse_y >= y1 & mouse_y < y2) | EvoGUI_Window_item.resize_tl == true)
//                                        {
//                                            EvoGUI_Window_item.UnhighlightAll();
//                                            CurrentCursorName = this.tex_cursor_bleft.Name;
//                                            Cursor_u2 = tex_cursor_bleft.u2;
//                                            Cursor_v2 = tex_cursor_bleft.v2;
//                                            mouse_offset_x = tex_cursor_bleft.Width / 3;
//                                            mouse_offset_y = tex_cursor_bleft.Height / 3;
//                                            DefaultCursor = false;
//                                        }
//                                    }

//                                    //Check listbox columns:
//                                    foreach (EvoGUI_ListBox ListBox in EvoGUI_ListBox.col)
//                                    {
//                                        if (object.ReferenceEquals(ListBox.ParentWindow, EvoGUI_Window_item))
//                                        {
//                                            for (i = 1; i <= ListBox.Columns; i++)
//                                            {
//                                                csx1 = EvoGUI_Window_item.x + ListBox.ColumnSeparator(i, 1) + EvoGUI_Window_item.ntex_left.Width;
//                                                csy1 = EvoGUI_Window_item.y + ListBox.ColumnSeparator(i, 2) + EvoGUI_Window_item.ntex_top.Height;
//                                                csx2 = EvoGUI_Window_item.x + ListBox.ColumnSeparator(i, 3) + EvoGUI_Window_item.ntex_left.Width;
//                                                csy2 = EvoGUI_Window_item.y + ListBox.ColumnSeparator(i, 4) + EvoGUI_Window_item.ntex_top.Height;
//                                                if (csx1 != 0 | csy1 != 0 | csx2 != 0 | csy2 != 0)
//                                                {
//                                                    //Form1.debug1 = "csx1: " + CStr(csx1) + " csy1: " + CStr(csy1)
//                                                    //Form1.debug2 = "mx: " + CStr(Me.mouse_x) + " my: " + CStr(Me.mouse_y)
//                                                    if (this.mouse_x >= csx1 & this.mouse_x < csx2)
//                                                    {
//                                                        if (this.mouse_y >= csy1 & this.mouse_y < csy2)
//                                                        {
//                                                            //MsgBox("cursor column")
//                                                            this.CurrentCursorName = this.tex_cursor_column.Name;
//                                                            this.Cursor_u2 = this.tex_cursor_column.u2;
//                                                            this.Cursor_v2 = this.tex_cursor_column.v2;
//                                                            this.mouse_offset_x = this.tex_cursor_column.Width / 2;
//                                                            //center the cursor 
//                                                            this.mouse_offset_y = this.tex_cursor_column.Height / 2;
//                                                            DefaultCursor = false;
//                                                        }
//                                                    }
//                                                }
//                                            }
//                                        }
//                                    }


//                                    if (DefaultCursor == true)
//                                    {
//                                        this.CurrentCursorName = this.tex_cursor_arrow.Name;
//                                        this.Cursor_u2 = this.tex_cursor_arrow.u2;
//                                        this.Cursor_v2 = this.tex_cursor_arrow.v2;
//                                        this.mouse_offset_x = 0;
//                                        this.mouse_offset_y = 0;
//                                    }

//                                    //Check mouse-over effects for the window over which the mouse is located:::
//                                    if (ResizingAWindow == false) EvoGUI_Window_item.CheckMouseOver(mouse_x, mouse_y);
//                                }

//                            }
//                        }

//                    }

//                }
//                //End if - checking is mouse is over any window 
//                //End if - checking if mouse is over menus or combo boxes= false 


//                CheckClicks(MaxTotalWindowPriority2, MaxTotalWindowPriority1);

//                CheckMouseRelease();

//                UpdateWindowPositions();

//                //Check for any listbox column resizing
//                foreach (EvoGUI_ListBox ListBox in EvoGUI_ListBox.col)
//                {

//                    if (ListBox.ColumnDragged > 0)
//                    {
//                        ListBox.DragColumn(this.mouse_x, this.mouse_y);
//                    }
//                }

//            }
//            //checking if mouse has moved at all since last update '--------------------------------------
//            //(the following updates need to be done/checked regardless of mouse movement)


//            UpdateTextCursor();

//            CheckKeyboardInput();


//            //Check any scroll bars if they are being pressed and held 
//            foreach (EvoGUI_ScrollBar ScrollBar in EvoGUI_ScrollBar.col)
//            {

//                if (ScrollBar.Button_tl_Pressed == true)
//                {
//                    if (ScrollBar.Vertical == true)
//                    {
//                        ScrollBar.MoveArrowUp();
//                    }
//                    else
//                    {
//                        ScrollBar.MoveArrowLeft();
//                    }
//                }

//                if (ScrollBar.Button_br_Pressed == true)
//                {
//                    if (ScrollBar.Vertical == true)
//                    {
//                        ScrollBar.MoveArrowDown();
//                    }
//                    else
//                    {
//                        ScrollBar.MoveArrowRight();
//                    }
//                }

//                if (ScrollBar.HandleDragged == true)
//                {
//                    ScrollBar.DragHandle(mouse_x, mouse_y);
//                }
//            }


//            foreach (EvoGUI_Slider Slider in EvoGUI_Slider.col)
//            {
//                if (Slider.Held == true)
//                {
//                    Slider.Drag(mouse_x, mouse_y);
//                }
//            }


//            //Check if any of the spinners are being pressed and held
//            foreach (EvoGUI_Spinner Spinner in EvoGUI_Spinner.col)
//            {
//                if (Spinner.Pressed_up)
//                {
//                    Spinner.ArrowUp();
//                }
//                if (Spinner.Pressed_down)
//                {
//                    Spinner.ArrowDown();
//                }
//            }


//            //Refresh any textboxes that are qued for refreshing
//            foreach (EvoGUI_TextBox TextBox in EvoGUI_TextBox.EvoGUI_TextBox_col)
//            {
//                if (TextBox.active == true)
//                {
//                    TextBox.Refresh();
//                }
//            }

//            foreach (EvoGUI_TextBoxInput TextBoxInput in EvoGUI_TextBoxInput.EvoGUI_TextBoxInput_col)
//            {
//                if (TextBoxInput.Active)
//                {
//                    TextBoxInput.Refresh();
//                }
//            }


//            //'Istolated squares' update technique ::: Check if any windows are qued for refresh
//            foreach (EvoGUI_Window Window in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                if (Window.RefreshQued == true)
//                {
//                    Window.IsolatedRefresh();
//                }
//            }

//            foreach (EvoGUI_ComboBox ComboBox in EvoGUI_ComboBox.col)
//            {
//                if (ComboBox.RefreshQuedExternal == true)
//                {
//                    ComboBox.RefreshQuedExternal = false;
//                    ComboBox.Refresh(false, true);
//                }
//            }


//        }

//        public void RaiseEvent_Button(string nButtonName)
//        {
//            if (ButtonEvent != null)
//            {
//                ButtonEvent(nButtonName);
//            }
//        }

//        public void RaiseEvent_StateChange(string CallerName, string id)
//        {
//            if (StateChange != null)
//            {
//                StateChange(CallerName, id);
//            }
//        }


//        public string GetCurColKey()
//        {
//            //Returns a unique collection key, which is a unique identifier for every new gadget that is added to a collection 
//            //This is used so that each gadget can easily be removed from a collection using the unique key
//            CurColKey += 1;
//            return (string)CurColKey;
//        }

//        public EvoGUI_Window GetWindow(string Name)
//        {
//            foreach (EvoGUI_Window win in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                if (win.Name == Name | string.IsNullOrEmpty(Name))
//                {
//                    return win;
//                }
//            }
//            return null;
//        }

//        public EvoGUI_ComboBox GetComboBox(string Name)
//        {
//            foreach (EvoGUI_ComboBox cb in EvoGUI_ComboBox.col)
//            {
//                if (cb.Name == Name)
//                {
//                    return cb;
//                }
//            }
//            Interaction.MsgBox("Error: combo box " + Name + " not found.");
//            return null;
//        }

//        public EvoGUI_CheckBox GetCheckBox(string ComboBoxName)
//        {
//            foreach (EvoGUI_CheckBox cbx in EvoGUI_CheckBox.col)
//            {
//                if (cbx.Name == ComboBoxName)
//                {
//                    return cbx;
//                }
//            }
//            Interaction.MsgBox("Error: check box " + ComboBoxName + " not found.");
//            return null;
//        }

//        public EvoGUI_TextBox GetTextBox(string Name)
//        {
//            foreach (EvoGUI_TextBox tb in EvoGUI_TextBox.EvoGUI_TextBox_col)
//            {
//                if (tb.Name == Name)
//                {
//                    return tb;
//                }
//            }
//            return null;
//        }

//        public EvoGUI_TextureSegment GetTextureSegment(string nName, string SkinName)
//        {

//            foreach (EvoGUI_TextureSegment ts in EvoGUI_TextureSegment.EvoGUI_TextureSegment_col)
//            {
//                if (ts.Name == nName)
//                {
//                    if (ts.btSkin == SkinName)
//                    {
//                        return ts;
//                    }
//                }
//            }
//            Interaction.MsgBox("Error: TextureSegment " + nName + " not found.");
//            return null;

//        }

//        public EvoGUI_TextBoxInput GetTextBoxInput(string Name)
//        {
//            foreach (EvoGUI_TextBoxInput tbi in EvoGUI_TextBoxInput.EvoGUI_TextBoxInput_col)
//            {
//                if (tbi.Name == Name)
//                {
//                    return tbi;
//                }
//            }
//            return null;
//        }

//        public int GetSpinnerValue(string Name)
//        {
//            foreach (EvoGUI_Slider Slider in EvoGUI_Slider.col)
//            {
//                if (Slider.Name == Name)
//                {
//                    return Slider.CurrentValue;
//                }
//            }
//            return null;
//        }

//        public EvoGUI_TVFont GetTVFont(string Name)
//        {

//            foreach (EvoGUI_TVFont tvFont in EvoGUI_TVFont.EvoGUI_TVFont_col)
//            {
//                if (tvFont.Name == Name)
//                {
//                    return tvFont;
//                }
//            }

//            Interaction.MsgBox("Error: TVFont " + Name + " not found.");
//            return null;

//        }

//        public EvoGUI_Label GetLabel(string LabelName)
//        {

//            foreach (EvoGUI_Label lbl in EvoGUI_Label.EvoGUI_Label_col)
//            {
//                if (lbl.Name == LabelName)
//                {
//                    return lbl;
//                }
//            }
//            Interaction.MsgBox("Error: Label " + LabelName + " not found.");
//            return null;
//        }

//        public EvoGUI_ScrollBar GetScrollBar(string ScrollBarName)
//        {

//            foreach (EvoGUI_ScrollBar sb in EvoGUI_ScrollBar.col)
//            {
//                if (sb.Name == ScrollBarName)
//                {
//                    return sb;
//                }
//            }
//            Interaction.MsgBox("Error: Scroll Bar " + ScrollBarName + " not found.");
//            return null;

//        }

//        public EvoGUI_D3DFont GetD3DFont(string Name)
//        {

//            foreach (EvoGUI_D3DFont Fontd3d in EvoGUI_D3DFont.EvoGUI_D3DFont_col)
//            {
//                if (Fontd3d.Name == Name)
//                {
//                    return Fontd3d;
//                }
//            }

//            Interaction.MsgBox("Error: D3DFont " + Name + " not found.");
//            return null;

//        }

//        public EvoGUI_Button GetButton(string Name)
//        {

//            foreach (EvoGUI_Button btn in EvoGUI_Button.EvoGUI_Button_col)
//            {
//                if (btn.Name == Name)
//                {
//                    return btn;
//                }
//            }

//            Interaction.MsgBox("Error: Button " + Name + " not found.");
//            return null;

//        }

//        public EvoGUI_TextInput GetTextInput(string Name)
//        {

//            foreach (EvoGUI_TextInput ti in EvoGUI_TextInput.col)
//            {
//                if (ti.Name == Name)
//                {
//                    return ti;
//                }
//            }

//            Interaction.MsgBox("Error: Text Input " + Name + " not found.");
//            return null;

//        }

//        public EvoGUI_Spinner GetSpinner(string SpinnerName)
//        {

//            foreach (EvoGUI_Spinner sp in EvoGUI_Spinner.col)
//            {
//                if (sp.Name == SpinnerName)
//                {
//                    return sp;
//                }
//            }

//            Interaction.MsgBox("Error: Spinner " + SpinnerName + " not found.");
//            return null;

//        }

//        public void SetBlendingModes(int mode)
//        {
//            //1 = 5/11 'alpha blending with preserving saturation
//            //2 = 2/0 default (alpha overwriting)

//            if (mode == 0)
//            {
//                this.d3dDevice.SetRenderState(RenderStates.SeparateAlphaBlendEnable, false);
//                return;
//            }

//            if (mode == 1)
//            {
//                this.d3dDevice.SetRenderState(RenderStates.SeparateAlphaBlendEnable, true);
//                this.d3dDevice.SetRenderState(RenderStates.SourceBlendAlpha, 5);
//                //5 = SRCALPHA
//                this.d3dDevice.SetRenderState(RenderStates.DestinationBlendAlpha, 11);
//                //11 = D3DBLEND_SRCALPHASAT
//                return;
//            }

//            if (mode == 2)
//            {
//                this.d3dDevice.SetRenderState(RenderStates.SeparateAlphaBlendEnable, true);
//                this.d3dDevice.SetRenderState(RenderStates.SourceBlendAlpha, 2);
//                this.d3dDevice.SetRenderState(RenderStates.DestinationBlendAlpha, 1);
//                return;
//            }

//        }

//        public void DrawOpenMenus()
//        {
//            foreach (EvoGUI_Menu Menu in EvoGUI_Menu.EvoGUI_OpenMenu_col)
//            {
//                this.Scr2D.Draw_Texture(Menu.Surface.GetTexture, Menu.x, Menu.y, Menu.x + Menu.Width - 1, Menu.y + Menu.Height - 1, -1, -1, -1, -1, 0,
//                0, 1, 1);
//            }
//        }

//        public void DrawOpenComboBoxes()
//        {
//            int x1 = 0;
//            int y1 = 0;
//            int x2 = 0;
//            int y2 = 0;

//            foreach (EvoGUI_ComboBox ComboBox in EvoGUI_ComboBox.col)
//            {
//                if (ComboBox.IsOpen)
//                {
//                    x1 = ComboBox.OpenX;
//                    y1 = ComboBox.OpenY;
//                    x2 = x1 + ComboBox.Width - 1;
//                    y2 = y1 + ComboBox.ShownHeight - 1;
//                    this.Scr2D.Draw_Texture(ComboBox.OpenSurface.GetTexture, x1, y1, x2, y2, -1, -1, -1, -1, 0,
//                    0, 1, 1);
//                }
//            }
//        }

//        public void DrawTextCursor()
//        {

//            int x1 = 0;
//            int y1 = 0;
//            int x2 = 0;
//            int y2 = 0;

//            foreach (EvoGUI_TextInput TextInput in EvoGUI_TextInput.col)
//            {
//                if (TextInput.Active == true)
//                {
//                    if (this.TextCursorState == true)
//                    {
//                        x1 = TextInput.ParentWindow.x + TextInput.ParentWindow.ntex_left.Width + TextInput.x + TextInput.CursorX;
//                        y1 = TextInput.ParentWindow.y + TextInput.ParentWindow.ntex_top.Height + TextInput.y;
//                        x2 = x1;
//                        y2 = y1 + TextInput.Font.Size;
//                        Scr2D.Draw_Line(x1, y1, x2, y2, TextInput.Colour_Cursor);
//                    }
//                }
//            }

//            foreach (EvoGUI_TextBoxInput TextBoxInput in EvoGUI_TextBoxInput.EvoGUI_TextBoxInput_col)
//            {
//                if (TextBoxInput.InputActive == true)
//                {
//                    TextBoxInput.DrawTextCursor();
//                }
//            }

//        }

//        public void CheckClicks(int MaxTotalWindowPriority2, int MaxTotalWindowPriority1)
//        {
//            if (this.mouse_b1 == true & this.mouse_b1_pressed == false)
//            {
//                this.mouse_b1_pressed = true;

//                if (this.MouseOverMenu == false)
//                {

//                    //1-Close all open menus
//                    foreach (EvoGUI_Menu OpenMenu in EvoGUI_Menu.EvoGUI_OpenMenu_col)
//                    {
//                        OpenMenu.CloseMenu();
//                    }

//                    //2-Do Window.CheckClick for each window 
//                    foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        if (EvoGUI_Window_item.Index == MouseIsOverWindowIndex & ResizingAWindow == false)
//                        {
//                            EvoGUI_Window_item.CheckClick(this.mouse_x, this.mouse_y);
//                        }
//                    }

//                    //3-And if the window was behind another window, sort their priorities 
//                    foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        //we have found the right window
//                        if (EvoGUI_Window_item.Index == MouseIsOverWindowIndex)
//                        {
//                            if (EvoGUI_Window_item.InFocus == false)
//                            {
//                                EvoGUI_Window_item.InFocus = true;
//                                EvoGUI_Window_item.EventRaised("win_getfocus");
//                                this.FocusedWindowIndex = EvoGUI_Window_item.Index;
//                                EvoGUI_Window_item.RefreshTop();
//                            }

//                            //If priority2 isn't 0 (ie: if its in the always-on-top group) then switch priority 2
//                            if (EvoGUI_Window_item.Priority2 > 0)
//                            {

//                                foreach (EvoGUI_Window EvoGUI_Window_item2 in EvoGUI_Window.EvoGUI_Window_col)
//                                {
//                                    //max priority is stored in MaxTotalWindowPriority1
//                                    //If priority of item2 > item, decrease item2 priority
//                                    if (EvoGUI_Window_item2.Priority2 > EvoGUI_Window_item.Priority2)
//                                    {
//                                        EvoGUI_Window_item2.Priority2 -= 1;
//                                    }
//                                }
//                                EvoGUI_Window_item.Priority2 = MaxTotalWindowPriority2;
//                            }

//                            else
//                            {

//                                //then decrease priority of all the windows above it, and put it on top
//                                foreach (EvoGUI_Window EvoGUI_Window_item2 in EvoGUI_Window.EvoGUI_Window_col)
//                                {
//                                    //max priority is stored in MaxTotalWindowPriority1
//                                    if (EvoGUI_Window_item2.Priority1 > EvoGUI_Window_item.Priority1)
//                                    {
//                                        EvoGUI_Window_item2.Priority1 -= 1;
//                                    }
//                                }
//                                EvoGUI_Window_item.Priority1 = MaxTotalWindowPriority1;
//                            }

//                        }
//                        else
//                        {
//                            if (EvoGUI_Window_item.InFocus == true)
//                            {
//                                EvoGUI_Window_item.InFocus = false;
//                                EvoGUI_Window_item.EventRaised("win_loosefocus");
//                                EvoGUI_Window_item.RefreshTop();

//                                //If window looses focus, deactivate any text inputs that were active in it
//                                if (TextInputActive == true) EvoGUI_Window_item.DeactivateAllKeyboardInputs();
//                            }

//                        }

//                    }


//                    //5-Check clicks on any ComboBox open areas
//                    foreach (EvoGUI_ComboBox ComboBox in EvoGUI_ComboBox.col)
//                    {
//                        if (ComboBox.IsOpen == true)
//                        {
//                            ComboBox.CheckClick(this.mouse_x, this.mouse_y);
//                        }
//                    }
//                }

//                //>>> If the mouse IS over a menu... 
//                else
//                {
//                    //-Check if any of the menu items in the menus have been clicked
//                    foreach (EvoGUI_MenuItem Item in EvoGUI_MenuItem.EvoGUI_MenuItem_col)
//                    {
//                        if (Item.Highlighted == true)
//                        {
//                            Item.Highlighted = false;
//                            Item.Pressed = true;
//                            Item.ParentMenu.CloseAllOpenMenus();
//                            Item.ParentMenu.ParentWindow.UnhighlightAll();
//                            if (MenuClick != null)
//                            {
//                                MenuClick(Item.Name);
//                            }
//                        }
//                    }
//                }

//            }


//        }

//        public void CheckMouseRelease()
//        {

//            bool ShouldRefresh = false;

//            //Check if mouse was just released
//            if (this.mouse_b1 == false & this.mouse_b1_pressed == true)
//            {
//                this.mouse_b1_pressed = false;

//                if (this.MouseOverMenu == false)
//                {
//                    //Check if any of the elements have been pressed and if mouse is over them >> trigger event
//                    foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        if (EvoGUI_Window_item.Index == this.MouseIsOverWindowIndex)
//                        {
//                            EvoGUI_Window_item.CheckRelease1(this.mouse_x, this.mouse_y);
//                        }
//                    }

//                    //...and unpress all the buttons on all windows, regardless of mouse position
//                    foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//                    {
//                        EvoGUI_Window_item.UnpressAll();
//                        //The windows aren't being dragged any more
//                        EvoGUI_Window_item.Dragged = false;
//                        EvoGUI_Window_item.resize_right = false;
//                        EvoGUI_Window_item.resize_left = false;
//                        EvoGUI_Window_item.resize_top = false;
//                        EvoGUI_Window_item.resize_bottom = false;
//                        EvoGUI_Window_item.resize_br = false;
//                        EvoGUI_Window_item.resize_bl = false;
//                        EvoGUI_Window_item.resize_tr = false;
//                        EvoGUI_Window_item.resize_tl = false;

//                        this.CurrentCursorName = this.tex_cursor_arrow.Name;
//                        this.Cursor_u2 = this.tex_cursor_arrow.u2;
//                        this.Cursor_v2 = this.tex_cursor_arrow.v2;
//                        this.mouse_offset_x = 0;
//                        this.mouse_offset_y = 0;

//                        if (EvoGUI_Window_item.IsResizing) EvoGUI_Window_item.EndResize();
//                    }

//                    //Unpress/refresh any scroll bars which are not bound to a window
//                    foreach (EvoGUI_ScrollBar ScrollBar in EvoGUI_ScrollBar.col)
//                    {
//                        if (ScrollBar.DrawWithWindow == false)
//                        {
//                            //-Combo boxes
//                            if (ScrollBar.BelongsToComboBox == true)
//                            {
//                                if (ScrollBar.Button_br_Pressed == true) ShouldRefresh = true;
//                                if (ScrollBar.Button_tl_Pressed == true) ShouldRefresh = true;
//                                if (ScrollBar.HandlePressed == true) ShouldRefresh = true;
//                                if (ShouldRefresh)
//                                {
//                                    ScrollBar.Button_br_Pressed = false;
//                                    ScrollBar.Button_tl_Pressed = false;
//                                    ScrollBar.HandlePressed = false;
//                                    ScrollBar.HandleDragged = false;
//                                    ScrollBar.QueRefresh(false);
//                                }
//                            }
//                        }
//                    }
//                }

//                else
//                {
//                    //Now check all the items/menus
//                    foreach (EvoGUI_MenuItem Item in EvoGUI_MenuItem.EvoGUI_MenuItem_col)
//                    {
//                        if (Item.Pressed == true & Item.Highlighted == true)
//                        {
//                            Item.Pressed = false;
//                            Item.Highlighted = false;
//                            //Close all open menus
//                            foreach (EvoGUI_Menu OpenMenu in EvoGUI_Menu.EvoGUI_OpenMenu_col)
//                            {
//                                OpenMenu.CloseMenu();
//                            }
//                            //Make sure the top window menu is completely unhighlighted 
//                            foreach (EvoGUI_Menu imenu in EvoGUI_Menu.EvoGUI_Menu_col)
//                            {
//                                if (imenu.Name == Item.ParentMenuName)
//                                {
//                                    imenu.UnhighlightWindowMenu();
//                                }
//                            }
//                        }
//                    }
//                }


//                foreach (EvoGUI_ListBox ListBox in EvoGUI_ListBox.col)
//                {
//                    ListBox.ColumnDragged = 0;
//                }
//            }




//        }


//        public void UpdateWindowPositions()
//        {
//            int TempHeight = 0;
//            int TempWidth = 0;

//            //Update positions of windows which are being dragged or resized
//            foreach (EvoGUI_Window EvoGUI_Window_item in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                if (EvoGUI_Window_item.Dragged == true)
//                {
//                    EvoGUI_Window_item.x = mouse_x - EvoGUI_Window_item.Dragged_x;
//                    EvoGUI_Window_item.y = mouse_y - EvoGUI_Window_item.Dragged_y;
//                }
//                else
//                {

//                    if (EvoGUI_Window_item.resize_right == true)
//                    {
//                        TempWidth = mouse_x - EvoGUI_Window_item.Orig_x;
//                        if (TempWidth != EvoGUI_Window_item.Width)
//                        {
//                            if (TempWidth < EvoGUI_Window_item.MinWidth) TempWidth = EvoGUI_Window_item.MinWidth;
//                            if (TempWidth != EvoGUI_Window_item.Width) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, TempWidth, EvoGUI_Window_item.Height);
//                            //Refresh window only if it has actually changed size
//                            EvoGUI_Window_item.Width = TempWidth;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_left == true)
//                    {
//                        TempWidth = EvoGUI_Window_item.Orig_x + EvoGUI_Window_item.OrigWidth - mouse_x;
//                        if (TempWidth != EvoGUI_Window_item.Width)
//                        {
//                            if (TempWidth < EvoGUI_Window_item.MinWidth)
//                            {
//                                TempWidth = EvoGUI_Window_item.MinWidth;
//                                EvoGUI_Window_item.x = EvoGUI_Window_item.Orig_x + EvoGUI_Window_item.OrigWidth - EvoGUI_Window_item.MinWidth;
//                            }
//                            else
//                            {
//                                EvoGUI_Window_item.x = mouse_x;
//                            }
//                            if (TempWidth != EvoGUI_Window_item.Width) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, TempWidth, EvoGUI_Window_item.Height);
//                            EvoGUI_Window_item.Width = TempWidth;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_top == true)
//                    {
//                        TempHeight = EvoGUI_Window_item.Orig_y + EvoGUI_Window_item.OrigHeight - mouse_y;
//                        if (TempHeight != EvoGUI_Window_item.Height)
//                        {
//                            if (TempHeight < EvoGUI_Window_item.MinHeight)
//                            {
//                                TempHeight = EvoGUI_Window_item.MinHeight;
//                                EvoGUI_Window_item.y = EvoGUI_Window_item.Orig_y + EvoGUI_Window_item.OrigHeight - EvoGUI_Window_item.MinHeight;
//                            }
//                            else
//                            {
//                                EvoGUI_Window_item.y = mouse_y;
//                            }
//                            if (TempHeight != EvoGUI_Window_item.Height) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, EvoGUI_Window_item.Width, TempHeight);
//                            EvoGUI_Window_item.Height = TempHeight;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_bottom == true)
//                    {
//                        TempHeight = mouse_y - EvoGUI_Window_item.Orig_y;
//                        if (TempHeight != EvoGUI_Window_item.Height)
//                        {
//                            if (TempHeight < EvoGUI_Window_item.MinHeight) TempHeight = EvoGUI_Window_item.MinHeight;
//                            if (TempHeight != EvoGUI_Window_item.Height) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, EvoGUI_Window_item.Width, TempHeight);
//                            EvoGUI_Window_item.Height = TempHeight;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_br == true)
//                    {
//                        TempWidth = mouse_x - EvoGUI_Window_item.Orig_x;
//                        TempHeight = mouse_y - EvoGUI_Window_item.Orig_y;
//                        if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height)
//                        {
//                            if (TempWidth < EvoGUI_Window_item.MinWidth) TempWidth = EvoGUI_Window_item.MinWidth;
//                            if (TempHeight < EvoGUI_Window_item.MinHeight) TempHeight = EvoGUI_Window_item.MinHeight;
//                            if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, TempWidth, TempHeight);
//                            //Refresh window only if it has actually changed size
//                            EvoGUI_Window_item.Width = TempWidth;
//                            EvoGUI_Window_item.Height = TempHeight;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_bl == true)
//                    {
//                        TempWidth = EvoGUI_Window_item.Orig_x + EvoGUI_Window_item.OrigWidth - mouse_x;
//                        TempHeight = mouse_y - EvoGUI_Window_item.Orig_y;
//                        if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height)
//                        {

//                            if (TempWidth < EvoGUI_Window_item.MinWidth)
//                            {
//                                TempWidth = EvoGUI_Window_item.MinWidth;
//                                EvoGUI_Window_item.x = EvoGUI_Window_item.Orig_x + EvoGUI_Window_item.OrigWidth - EvoGUI_Window_item.MinWidth;
//                            }
//                            else
//                            {
//                                EvoGUI_Window_item.x = mouse_x;
//                            }

//                            if (TempHeight < EvoGUI_Window_item.MinHeight) TempHeight = EvoGUI_Window_item.MinHeight;

//                            if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, TempWidth, TempHeight);
//                            EvoGUI_Window_item.Width = TempWidth;
//                            EvoGUI_Window_item.Height = TempHeight;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_tl == true)
//                    {
//                        TempWidth = EvoGUI_Window_item.Orig_x + EvoGUI_Window_item.OrigWidth - mouse_x;
//                        TempHeight = EvoGUI_Window_item.Orig_y + EvoGUI_Window_item.OrigHeight - mouse_y;
//                        if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height)
//                        {

//                            if (TempWidth < EvoGUI_Window_item.MinWidth)
//                            {
//                                TempWidth = EvoGUI_Window_item.MinWidth;
//                                EvoGUI_Window_item.x = EvoGUI_Window_item.Orig_x + EvoGUI_Window_item.OrigWidth - EvoGUI_Window_item.MinWidth;
//                            }
//                            else
//                            {
//                                EvoGUI_Window_item.x = mouse_x;
//                            }

//                            if (TempHeight < EvoGUI_Window_item.MinHeight)
//                            {
//                                TempHeight = EvoGUI_Window_item.MinHeight;
//                                EvoGUI_Window_item.y = EvoGUI_Window_item.Orig_y + EvoGUI_Window_item.OrigHeight - EvoGUI_Window_item.MinHeight;
//                            }
//                            else
//                            {
//                                EvoGUI_Window_item.y = mouse_y;
//                            }

//                            if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, TempWidth, TempHeight);
//                            EvoGUI_Window_item.Width = TempWidth;
//                            EvoGUI_Window_item.Height = TempHeight;
//                        }
//                    }

//                    if (EvoGUI_Window_item.resize_tr == true)
//                    {
//                        TempWidth = mouse_x - EvoGUI_Window_item.Orig_x;
//                        TempHeight = EvoGUI_Window_item.Orig_y + EvoGUI_Window_item.OrigHeight - mouse_y;
//                        if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height)
//                        {
//                            if (TempWidth < EvoGUI_Window_item.MinWidth) TempWidth = EvoGUI_Window_item.MinWidth;

//                            if (TempHeight < EvoGUI_Window_item.MinHeight)
//                            {
//                                TempHeight = EvoGUI_Window_item.MinHeight;
//                                EvoGUI_Window_item.y = EvoGUI_Window_item.Orig_y + EvoGUI_Window_item.OrigHeight - EvoGUI_Window_item.MinHeight;
//                            }
//                            else
//                            {
//                                EvoGUI_Window_item.y = mouse_y;
//                            }

//                            if (TempWidth != EvoGUI_Window_item.Width | TempHeight != EvoGUI_Window_item.Height) EvoGUI_Window_item.RefreshResize(EvoGUI_Window_item.Width, EvoGUI_Window_item.Height, TempWidth, TempHeight);
//                            EvoGUI_Window_item.Width = TempWidth;
//                            EvoGUI_Window_item.Height = TempHeight;
//                        }
//                    }
//                }
//            }


//        }

//        public void RefreshWindow(string WindowName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(false)]  // ERROR: Optional parameters aren't supported in C#
//bool OnlyFixedGadgets, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(false)]  // ERROR: Optional parameters aren't supported in C#
//bool StartOfResize)
//        {

//            EvoGUI_Window CurWindow = null;

//            foreach (EvoGUI_Window Window in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                if (Window.Name == WindowName)
//                {
//                    CurWindow = Window;
//                }
//            }
//            if (CurWindow == null) Interaction.MsgBox("Error: window " + WindowName + "not found");

//            //Draw background
//            CurWindow.DrawBackground();

//            //Que all gadgets to refresh
//            //::: 'isolated squares' technique:
//            foreach (EvoGUI_Group Group in EvoGUI_Group.col)
//            {
//                if (object.ReferenceEquals(Group.ParentWindow, CurWindow))
//                {
//                    Group.Refresh();
//                }
//            }

//            foreach (EvoGUI_Button Button in EvoGUI_Button.EvoGUI_Button_col)
//            {
//                if (object.ReferenceEquals(Button.ParentWindow, CurWindow))
//                {
//                    Button.Refresh();
//                }
//            }

//            foreach (EvoGUI_ImgButton ImgButton in EvoGUI_ImgButton.EvoGUI_ImgButton_col)
//            {
//                if (object.ReferenceEquals(ImgButton.ParentWindow, CurWindow))
//                {
//                    ImgButton.Refresh();
//                }
//            }

//            foreach (EvoGUI_TextBox TextBox in EvoGUI_TextBox.EvoGUI_TextBox_col)
//            {
//                if (object.ReferenceEquals(TextBox.ParentWindow, CurWindow))
//                {
//                    //if we are refreshing only non-relative gadgets
//                    if (OnlyFixedGadgets == true)
//                    {
//                        if (TextBox.IsRelative == false)
//                        {
//                            if (StartOfResize == true)
//                            {
//                                TextBox.InstantRefresh(1, 2000);
//                            }
//                            //if at start of resize, also store the surface for 2 seconds in case sidesurface or bottomsurface needs it
//                            else
//                            {
//                                TextBox.InstantRefresh();
//                            }
//                            //then que textbox refresh only if it isn't relative (ie not maximised) 
//                        }
//                    }
//                    else
//                    {
//                        TextBox.QueRefresh();
//                    }
//                    //otherwise, always que it 
//                }
//            }

//            foreach (EvoGUI_TextBoxInput TextBoxInput in EvoGUI_TextBoxInput.EvoGUI_TextBoxInput_col)
//            {
//                if (object.ReferenceEquals(TextBoxInput.ParentWindow, CurWindow))
//                {
//                    //if we are refreshing only non-relative gadgets
//                    if (OnlyFixedGadgets == true)
//                    {
//                        if (TextBoxInput.IsRelative == false)
//                        {
//                            if (StartOfResize == true)
//                            {
//                                TextBoxInput.nFirstBlockToRefresh = 1;
//                                TextBoxInput.nTimeToPreserveSurface = 2000;
//                                TextBoxInput.InstantRefresh();
//                            }
//                            //if at start of resize, also store the surface for 2 seconds in case sidesurface or bottomsurface needs it
//                            else
//                            {
//                                TextBoxInput.InstantRefresh();
//                            }
//                            //then que TextBoxInput refresh only if it isn't relative (ie not maximised) 
//                        }
//                    }
//                    else
//                    {
//                        TextBoxInput.QueRefresh();
//                    }
//                    //otherwise, always que it 
//                }
//            }

//            foreach (EvoGUI_CheckBox CheckBox in EvoGUI_CheckBox.col)
//            {
//                if (object.ReferenceEquals(CheckBox.ParentWindow, CurWindow))
//                {
//                    CheckBox.Refresh();
//                }
//            }

//            foreach (EvoGUI_RadioButton Radio in EvoGUI_RadioButton.col)
//            {
//                if (object.ReferenceEquals(Radio.ParentWindow, CurWindow))
//                {
//                    Radio.Refresh();
//                }
//            }

//            foreach (EvoGUI_Spinner Spinner in EvoGUI_Spinner.col)
//            {
//                if (object.ReferenceEquals(Spinner.ParentWindow, CurWindow))
//                {
//                    Spinner.Refresh();
//                }
//            }

//            foreach (EvoGUI_Slider Slider in EvoGUI_Slider.col)
//            {
//                if (object.ReferenceEquals(Slider.ParentWindow, CurWindow))
//                {
//                    Slider.Refresh();
//                }
//            }

//            foreach (EvoGUI_Image Image in EvoGUI_Image.col)
//            {
//                if (object.ReferenceEquals(Image.ParentWindow, CurWindow))
//                {
//                    Image.Refresh();
//                }
//            }

//            foreach (EvoGUI_ListBox ListBox in EvoGUI_ListBox.col)
//            {
//                if (object.ReferenceEquals(ListBox.ParentWindow, CurWindow))
//                {
//                    ListBox.Refresh();
//                }
//            }

//            foreach (EvoGUI_ComboBox ComboBox in EvoGUI_ComboBox.col)
//            {
//                if (object.ReferenceEquals(ComboBox.ParentWindow, CurWindow))
//                {
//                    ComboBox.Refresh(true, false);
//                }
//            }

//            foreach (EvoGUI_ProgressBar ProgressBar in EvoGUI_ProgressBar.col)
//            {
//                if (object.ReferenceEquals(ProgressBar.ParentWindow, CurWindow))
//                {
//                    ProgressBar.Refresh();
//                }
//            }

//            foreach (EvoGUI_TextInput TextInput in EvoGUI_TextInput.col)
//            {
//                if (object.ReferenceEquals(TextInput.ParentWindow, CurWindow))
//                {
//                    TextInput.Refresh();
//                }
//            }

//            foreach (EvoGUI_Label Label in EvoGUI_Label.EvoGUI_Label_col)
//            {
//                if (object.ReferenceEquals(Label.ParentWindow, CurWindow))
//                {
//                    Label.Refresh();
//                }
//            }

//            foreach (EvoGUI_WindowMenu WindowMenu in EvoGUI_WindowMenu.EvoGUI_WindowMenu_col)
//            {
//                if (object.ReferenceEquals(WindowMenu.ParentWindow, CurWindow))
//                {
//                    WindowMenu.Refresh();
//                }
//            }


//            //ScrollBars should be last
//            foreach (EvoGUI_ScrollBar ScrollBar in EvoGUI_ScrollBar.col)
//            {
//                if (object.ReferenceEquals(ScrollBar.ParentWindow, CurWindow))
//                {
//                    //Scroll bars belonging to open combo boxes shouldnt be refreshed here
//                    if (!ScrollBar.BelongsToComboBox)
//                    {

//                        if (OnlyFixedGadgets == true)
//                        {
//                            if (ScrollBar.IsRelative == false) ScrollBar.Refresh();
//                        }
//                        else
//                        {
//                            ScrollBar.Refresh();
//                        }
//                    }

//                }
//            }

//        }

//        public void CheckMouseOverMenus(int mouse_x, int mouse_y)
//        {
//            bool RefreshMenu = false;
//            int y1 = 0;
//            int y2 = 0;

//            this.MouseOverMenu = false;

//            RefreshMenu = false;


//            //Check if the mouse is over any of the open menus
//            foreach (EvoGUI_Menu Menu in EvoGUI_Menu.EvoGUI_OpenMenu_col)
//            {
//                if (mouse_x >= Menu.x & mouse_x < Menu.x + Menu.Width & mouse_y >= Menu.y & mouse_y < Menu.y + Menu.Height)
//                {

//                    this.MouseOverMenu = true;

//                    //If it is over the given menu, check if it is over any of the item labels
//                    foreach (EvoGUI_MenuItem Item in EvoGUI_MenuItem.EvoGUI_MenuItem_col)
//                    {
//                        if (Item.ParentMenuName == Menu.Name)
//                        {
//                            y1 = Menu.y + Item.LabelY - (Item.LabelHeight / 2);
//                            y2 = y1 + (Item.LabelHeight * 2);
//                            if (mouse_x >= Menu.x & mouse_x < Menu.x + Menu.Width & mouse_y >= y1 & mouse_y < y2)
//                            {
//                                //If it is over any of the items, and it wasn't already highlighted, highlight it
//                                if (Item.Highlighted == false)
//                                {
//                                    Item.Highlighted = true;
//                                    RefreshMenu = true;
//                                }
//                                //unhighlight any other items
//                            }
//                            else
//                            {
//                                if (Item.Highlighted == true)
//                                {
//                                    RefreshMenu = true;
//                                }
//                                Item.Highlighted = false;
//                            }
//                        }
//                    }

//                    //Check submenus and if it is over any of the labels, open them
//                    foreach (EvoGUI_Menu SubMenu in EvoGUI_Menu.EvoGUI_Menu_col)
//                    {
//                        if (SubMenu.ParentMenuName == Menu.Name)
//                        {
//                            y1 = Menu.y + SubMenu.LabelY - (SubMenu.LabelHeight / 2);
//                            y2 = y1 + (SubMenu.LabelHeight * 2);
//                            if (mouse_x >= Menu.x & mouse_x < Menu.x + Menu.Width & mouse_y >= y1 & mouse_y < y2)
//                            {
//                                if (SubMenu.Highlighted == false)
//                                {
//                                    SubMenu.Highlighted = true;
//                                    RefreshMenu = true;
//                                    if (SubMenu.Open == true) SubMenu.CloseMenu();
//                                    SubMenu.OpenMenu(Menu.x + Menu.Width, Menu.y + SubMenu.LabelY - (SubMenu.LabelHeight / 2));
//                                }
//                            }

//                            else
//                            {
//                                if (SubMenu.Highlighted == true)
//                                {
//                                    RefreshMenu = true;
//                                }
//                                SubMenu.Highlighted = false;
//                                SubMenu.CloseMenu();
//                            }
//                        }
//                    }

//                    if (RefreshMenu) Menu.Refresh();
//                }

//                else
//                {

//                    //if cursor isn't over the given menu,
//                    //and if this menu is a submenu (parentmenu is not windowmenu)
//                    //and if it has children, then if the cursor isn't over any of it's children
//                    //and if the cursor is over any of the parents 
//                    //then unhighlight all of the items and submenus
//                    foreach (EvoGUI_Menu SubMenu in EvoGUI_Menu.EvoGUI_Menu_col)
//                    {
//                        if (SubMenu.ParentMenuName == Menu.Name)
//                        {
//                            if (SubMenu.Highlighted == true)
//                            {
//                                if (SubMenu.Open == true)
//                                {
//                                    SubMenu.Highlighted = false;
//                                    RefreshMenu = true;
//                                }
//                            }
//                        }
//                    }

//                    foreach (EvoGUI_MenuItem Item in EvoGUI_MenuItem.EvoGUI_MenuItem_col)
//                    {
//                        if (Item.ParentMenuName == Menu.Name)
//                        {
//                            if (Item.Highlighted == true)
//                            {
//                                Item.Highlighted = false;
//                                RefreshMenu = true;
//                            }
//                        }
//                    }

//                    bool pwm = false;
//                    pwm = false;
//                    //If it's parent isnt a WindwoMenu then close this menu as well and refresh it's parent
//                    foreach (EvoGUI_WindowMenu WM in EvoGUI_WindowMenu.EvoGUI_WindowMenu_col)
//                    {
//                        if (Menu.ParentMenuName == WM.Name)
//                        {
//                            pwm = true;
//                        }
//                    }

//                    if (RefreshMenu) Menu.Refresh();
//                }


//            }



//        }

//        public void CheckMouseOverComboBox(int mouse_x, int mouse_y)
//        {

//            MouseOverComboBox = false;

//            foreach (EvoGUI_ComboBox ComboBox in EvoGUI_ComboBox.col)
//            {
//                if (ComboBox.IsOpen == true)
//                {
//                    if (mouse_x >= ComboBox.OpenX & mouse_x < ComboBox.OpenX + ComboBox.Width)
//                    {
//                        if (mouse_y >= ComboBox.OpenY & mouse_y < ComboBox.OpenY + ComboBox.ShownHeight)
//                        {
//                            MouseOverComboBox = true;
//                            ComboBox.CheckMouseOver(mouse_x, mouse_y);
//                        }
//                    }
//                }

//            }

//        }


//        public EvoGUI_Window NewWindow(string Name, string Title, string Skin, int Width, int Height, int x, int y, string D3DFontName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(false)]  // ERROR: Optional parameters aren't supported in C#
//bool Borderless)
//        {
//            EvoGUI_Window CurWindow = new EvoGUI_Window(Name, Title, Skin, Width, Height, x, y, D3DFontName, this, Borderless
//            );
//            return CurWindow;
//        }


//        public void CloseWindow(string Name)
//        {
//            foreach (EvoGUI_Window Window in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                if (Window.Name == Name)
//                {
//                    Window.CloseWindow();
//                    return;
//                }
//            }
//            Interaction.MsgBox("Error: Cannot close window named: " + Name + ". Window not found.");
//        }

//        public void OpenWindow(string Name)
//        {
//            foreach (EvoGUI_Window Window in EvoGUI_Window.EvoGUI_Window_col)
//            {
//                if (Window.Name == Name)
//                {
//                    Window.OpenWindow();
//                }
//            }
//        }

//        public void Unload()
//        {
//            this.d3dDevice.Dispose();

//            //Clear all the d3dlines from all the gadgets that contain one

//            foreach (EvoGUI_Menu menu in EvoGUI_Menu.EvoGUI_Menu_col)
//            {
//                menu.d3dLine.Clear();
//            }

//            foreach (EvoGUI_TextBox tb in EvoGUI_TextBox.EvoGUI_TextBox_col)
//            {
//                tb.d3dLine.Clear();
//            }

//            foreach (EvoGUI_TextBoxInput tbi in EvoGUI_TextBoxInput.EvoGUI_TextBoxInput_col)
//            {
//                tbi.d3dLine.Clear();
//            }

//            foreach (EvoGUI_TextInput ti in EvoGUI_TextInput.col)
//            {
//                ti.d3dLine.Clear();
//            }

//            foreach (EvoGUI_WindowMenu wm in EvoGUI_WindowMenu.EvoGUI_WindowMenu_col)
//            {
//                wm.d3dLine.Clear();
//            }

//            foreach (EvoGUI_D3DFont d3dfont in EvoGUI_D3DFont.EvoGUI_D3DFont_col)
//            {
//                d3dfont.d3dLine.Clear();
//            }

//        }

//        public void UpdateTextCursor()
//        {


//            if (this.PreviousTextCursorTime + this.TextCursorFlashDelay < this.Milliseconds | this.PreviousTextCursorTime == 0)
//            {
//                this.PreviousTextCursorTime = this.Milliseconds;
//                this.TextCursorState = !this.TextCursorState;
//            }

//        }


//        public void CheckKeyboardInput()
//        {
//            TV_KEYDATA[] KeyBuffer = new TV_KEYDATA[257];
//            long NumEvents = 0;
//            int i = 0;

//            bool ShiftPressed = false;
//            bool CtrlPressed = false;


//            this.Inputs.GetKeyBuffer(KeyBuffer, NumEvents);


//            if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_LEFTSHIFT) == true | Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_RIGHTSHIFT) == true)
//            {
//                ShiftPressed = true;
//            }

//            if (Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_LEFTCONTROL) == true | Inputs.IsKeyPressed(CONST_TV_KEY.TV_KEY_RIGHTCONTROL) == true)
//            {
//                CtrlPressed = true;
//            }


//            if (NumEvents > 0)
//            {
//                for (i = 0; i <= NumEvents - 1; i++)
//                {
//                    if (KeyBuffer(i).Pressed == 1)
//                    {

//                        foreach (EvoGUI_TextBoxInput TextBoxInput in EvoGUI_TextBoxInput.EvoGUI_TextBoxInput_col)
//                        {
//                            if (TextBoxInput.InputActive == true)
//                            {

//                                if (KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_LEFTSHIFT & KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_RIGHTSHIFT)
//                                {
//                                    if (KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_LEFTCONTROL & KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_RIGHTCONTROL)
//                                    {
//                                        TextBoxInput.KeyPressed(KeyBuffer(i).Key, ShiftPressed, CtrlPressed);
//                                    }
//                                }

//                                if (KeyBuffer(i).Key == CONST_TV_KEY.TV_KEY_LEFTSHIFT | KeyBuffer(i).Key == CONST_TV_KEY.TV_KEY_RIGHTSHIFT)
//                                {
//                                    TextBoxInput.HighlightStart();
//                                }
//                            }

//                        }


//                        foreach (EvoGUI_TextInput TextInput in EvoGUI_TextInput.col)
//                        {
//                            if (TextInput.Active == true)
//                            {
//                                if (KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_LEFTSHIFT & KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_RIGHTSHIFT)
//                                {
//                                    if (KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_LEFTCONTROL & KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_RIGHTCONTROL)
//                                    {
//                                        TextInput.KeyPressed(KeyBuffer(i).Key, ShiftPressed, CtrlPressed);
//                                    }
//                                }
//                            }
//                        }


//                        foreach (EvoGUI_Spinner Spinner in EvoGUI_Spinner.col)
//                        {
//                            if (Spinner.TextInputActive == true)
//                            {
//                                if (KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_LEFTSHIFT & KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_RIGHTSHIFT)
//                                {
//                                    if (KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_LEFTCONTROL & KeyBuffer(i).Key != CONST_TV_KEY.TV_KEY_RIGHTCONTROL)
//                                    {
//                                        Spinner.KeyPressed(KeyBuffer(i).Key);
//                                    }
//                                }
//                            }
//                        }
//                    }



//                }
//            }


//        }


//        //Font

//        public EvoGUI_D3DFont AddD3DFont(string Name, string FontName, int Size, FontWeight Weight, int Colour, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(CharacterSet.Default)]  // ERROR: Optional parameters aren't supported in C#
//CharacterSet CharacterSet, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(FontQuality.ClearType)]  // ERROR: Optional parameters aren't supported in C#
//FontQuality FontQuality)
//        {
//            CurFont2 = new EvoGUI_D3DFont(this, Name, FontName, Size, Weight, Colour, CharacterSet, FontQuality);
//            return CurFont2;
//        }

//        public EvoGUI_TVFont AddTVFont(string UserName, string FontName, int Size, bool Bold, bool Underline, bool Italic, int Colour)
//        {
//            EvoGUI_TVFont CurFont3 = default(EvoGUI_TVFont);
//            CurFont3 = new EvoGUI_TVFont(this, UserName, FontName, Size, Bold, Underline, Italic, Colour);
//            return CurFont3;
//        }

//        public EvoGUI_Button AddButton(string WindowName, string button_name, int x, int y, int Width, string Label, string D3DFontName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(0)]  // ERROR: Optional parameters aren't supported in C#
//int Height)
//        {
//            EvoGUI_Button CurBtn = default(EvoGUI_Button);
//            CurBtn = new EvoGUI_Button(button_name, WindowName, x, y, Width, Label, D3DFontName, Height);
//            return CurBtn;
//        }

//        public EvoGUI_ProgressBar AddProgressBar(string WindowName, string Name, int x, int y, int Width)
//        {
//            EvoGUI_ProgressBar CurProgressBar = default(EvoGUI_ProgressBar);
//            CurProgressBar = new EvoGUI_ProgressBar(WindowName, Name, x, y, Width);
//            return CurProgressBar;
//        }

//        public EvoGUI_ImgButton AddImgButton(string WindowName, string button_name, int x, int y, string TexSegName_Unpressed, string TexSegName_Pressed, EvoGUI_TextureBig BigTexture, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("none")]  // ERROR: Optional parameters aren't supported in C#
//string TexSegName_MouseOver)
//        {
//            //Create a new image button
//            //x and y are coordinates within the active area of the window
//            EvoGUI_ImgButton CurImgBtn = default(EvoGUI_ImgButton);
//            CurImgBtn = new EvoGUI_ImgButton(WindowName, button_name, x, y, TexSegName_Unpressed, TexSegName_Pressed, TexSegName_MouseOver, BigTexture);
//            return CurImgBtn;
//        }

//        public EvoGUI_WindowMenu AddWindowMenu(string WindowName, string Name, string D3DFontName)
//        {
//            EvoGUI_WindowMenu CurWindowMenu = default(EvoGUI_WindowMenu);
//            CurWindowMenu = new EvoGUI_WindowMenu(WindowName, Name, D3DFontName);
//            return CurWindowMenu;
//        }

//        public EvoGUI_Menu AddMenu(string ParentMenuName, string Label, string Name)
//        {
//            EvoGUI_Menu CurMenu = default(EvoGUI_Menu);
//            CurMenu = new EvoGUI_Menu(ParentMenuName, Label, Name);
//            return CurMenu;
//        }

//        public EvoGUI_MenuItem AddItem(string MenuName, string Label, string ItemName)
//        {
//            EvoGUI_MenuItem CurItem = default(EvoGUI_MenuItem);
//            CurItem = new EvoGUI_MenuItem(MenuName, Label, ItemName);
//            return CurItem;
//        }

//        public EvoGUI_MenuSeparator AddSeparator(string ParentMenuName)
//        {
//            EvoGUI_MenuSeparator CurSeparator = default(EvoGUI_MenuSeparator);
//            CurSeparator = new EvoGUI_MenuSeparator(ParentMenuName);
//            return CurSeparator;
//        }

//        public EvoGUI_TextBox AddTextBox(string WindowName, string Name, int Height, int Width, bool MaximiseHeight, bool MaximiseWidth, int x, int y, string D3DFontName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("")]  // ERROR: Optional parameters aren't supported in C#
//string ContentString,
//        [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("")]  // ERROR: Optional parameters aren't supported in C#
//string ContentFile)
//        {
//            EvoGUI_TextBox CurTextBox = default(EvoGUI_TextBox);
//            CurTextBox = new EvoGUI_TextBox(WindowName, Name, Height, Width, MaximiseHeight, MaximiseWidth, x, y, D3DFontName, ContentString,
//            ContentFile);
//            return CurTextBox;
//        }

//        public EvoGUI_TextBoxInput AddTextBoxInput(string WindowName, string Name, int Height, int Width, bool MaximiseHeight, bool MaximiseWidth, int x, int y, string D3DFontName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("")]  // ERROR: Optional parameters aren't supported in C#
//string ContentString,
//        [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("")]  // ERROR: Optional parameters aren't supported in C#
//string ContentFile)
//        {
//            EvoGUI_TextBoxInput CurTextBoxInput = default(EvoGUI_TextBoxInput);
//            CurTextBoxInput = new EvoGUI_TextBoxInput(WindowName, Name, Height, Width, MaximiseHeight, MaximiseWidth, x, y, D3DFontName, ContentString,
//            ContentFile);
//            return CurTextBoxInput;
//        }

//        public EvoGUI_Label AddLabel(string WindowName, string Name, string Content, int x, int y, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("default")]  // ERROR: Optional parameters aren't supported in C#
//string D3DFontName)
//        {
//            EvoGUI_Label CurLabel = default(EvoGUI_Label);
//            CurLabel = new EvoGUI_Label(WindowName, Name, Content, x, y, D3DFontName);
//            return CurLabel;
//        }

//        public EvoGUI_ListBox AddListBox(string ParentWindowName, string ListBoxName, int x, int y, int Width, int Height, int Columns, bool ShowHeading, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("default")]  // ERROR: Optional parameters aren't supported in C#
//string TVFontName)
//        {
//            EvoGUI_ListBox CurListBox = default(EvoGUI_ListBox);
//            CurListBox = new EvoGUI_ListBox(ParentWindowName, ListBoxName, x, y, Width, Height, Columns, ShowHeading, TVFontName);
//            return CurListBox;
//        }

//        public EvoGUI_TextureBig NewBigTexture(string Name, string FileName, string FolderName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(".dds")]  // ERROR: Optional parameters aren't supported in C#
//string Extn)
//        {
//            EvoGUI_TextureBig CurTextureBig = default(EvoGUI_TextureBig);

//            foreach (EvoGUI_TextureBig tbskin in EvoGUI_TextureBig.EvoGUI_TextureBig_col)
//            {
//                if (tbskin.Name == Name & tbskin.FileName == FileName & tbskin.FolderName == FolderName)
//                {
//                    return tbskin;
//                }
//            }

//            CurTextureBig = new EvoGUI_TextureBig(Name, FileName, FolderName, this, Extn);
//            return CurTextureBig;
//        }


//        //Combo box:

//        public EvoGUI_ComboBox AddComboBox(string ParentWindowName, string ComboBoxName, int x, int y, int Width, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("default")]  // ERROR: Optional parameters aren't supported in C#
//string TVFontName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(0)]  // ERROR: Optional parameters aren't supported in C#
//int MaxHeight)
//        {
//            EvoGUI_ComboBox CurComboBox = default(EvoGUI_ComboBox);
//            CurComboBox = new EvoGUI_ComboBox(ParentWindowName, ComboBoxName, x, y, Width, TVFontName, MaxHeight);

//            return CurComboBox;
//        }


//        //Check box:

//        public EvoGUI_CheckBox AddCheckBox(string ParentWindowName, string CheckBoxName, int x, int y, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(false)]  // ERROR: Optional parameters aren't supported in C#
//bool State)
//        {
//            EvoGUI_CheckBox CurCheckBox = default(EvoGUI_CheckBox);
//            CurCheckBox = new EvoGUI_CheckBox(ParentWindowName, CheckBoxName, x, y, State);
//            return CurCheckBox;
//        }

//        //Radio button

//        public EvoGUI_RadioButton AddRadioButton(string ParentWindowName, string RadioButtonName, int x, int y, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute("None")]  // ERROR: Optional parameters aren't supported in C#
//string GroupName)
//        {
//            EvoGUI_RadioButton CurRadio = default(EvoGUI_RadioButton);
//            CurRadio = new EvoGUI_RadioButton(ParentWindowName, RadioButtonName, x, y, GroupName);
//            return CurRadio;
//        }

//        //Group

//        public EvoGUI_Group AddGroup(string ParentWindowName, string GroupName, int x, int y, int Width, int Height)
//        {
//            EvoGUI_Group CurGroup = default(EvoGUI_Group);
//            CurGroup = new EvoGUI_Group(ParentWindowName, GroupName, x, y, Width, Height);
//            return CurGroup;
//        }


//        public EvoGUI_Spinner AddSpinner(string ParentWindowName, string Name, int x, int y, int TextWidth, int TextHeight, string D3DFontName, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(1)]  // ERROR: Optional parameters aren't supported in C#
//int SpinnerStep, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(0)]  // ERROR: Optional parameters aren't supported in C#
//int StartingValue, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(0)]  // ERROR: Optional parameters aren't supported in C#
//int MinValue,
//    [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(10000)]  // ERROR: Optional parameters aren't supported in C#
//int MaxValue)
//        {
//            EvoGUI_Spinner CurSpinner = default(EvoGUI_Spinner);
//            CurSpinner = new EvoGUI_Spinner(ParentWindowName, Name, x, y, TextWidth, TextHeight, D3DFontName, SpinnerStep, StartingValue, MinValue,
//            MaxValue);
//            return CurSpinner;
//        }

//        public EvoGUI_Slider AddSlider(string ParentWindowName, string Name, int x, int y, int Width, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(0)]  // ERROR: Optional parameters aren't supported in C#
//int MinValue, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(100)]  // ERROR: Optional parameters aren't supported in C#
//int MaxValue, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(50)]  // ERROR: Optional parameters aren't supported in C#
//int StartingValue)
//        {
//            EvoGUI_Slider CurSlider = default(EvoGUI_Slider);
//            CurSlider = new EvoGUI_Slider(ParentWindowName, Name, x, y, Width, MinValue, MaxValue, StartingValue);
//            return CurSlider;
//        }

//        public EvoGUI_ScrollBar AddScrollBar(string ParentWindowName, string Name, int x, int y, int Length, bool Vertical, [System.Runtime.InteropServices.OptionalAttribute, System.Runtime.InteropServices.DefaultParameterValueAttribute(false)]  // ERROR: Optional parameters aren't supported in C#
//bool IsRelative)
//        {
//            EvoGUI_ScrollBar CurScrollbar = default(EvoGUI_ScrollBar);
//            CurScrollbar = new EvoGUI_ScrollBar(ParentWindowName, Vertical, Name, Length, IsRelative, false);
//            return CurScrollbar;
//        }


//        public EvoGUI_Image AddImage(string ParentWindowName, string Name, string SegmentName, EvoGUI_TextureBig BigTexture, int x, int y)
//        {
//            EvoGUI_Image CurImage = default(EvoGUI_Image);
//            CurImage = new EvoGUI_Image(ParentWindowName, Name, SegmentName, BigTexture, x, y);
//            return CurImage;
//        }

//        //Text input

//        public EvoGUI_TextInput AddTextInput(string ParentWindowName, string InputName, int x, int y, int Width, string D3DFontName)
//        {
//            EvoGUI_TextInput CurInput = default(EvoGUI_TextInput);
//            CurInput = new EvoGUI_TextInput(ParentWindowName, InputName, x, y, Width, D3DFontName);
//            return CurInput;
//        }
//    }

//}
