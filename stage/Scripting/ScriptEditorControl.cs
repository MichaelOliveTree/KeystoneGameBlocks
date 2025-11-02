using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Drawing;

namespace KeyEdit.Scripting
{
    /// <summary>
    /// Public Domain 
    /// - by Rim van Wersch
    /// http://www.mdxinfo.com/resources/scripting.php
    ///
    /// and is based heavily on the Public Domain code
    /// - by Patrik Svensson | 14 Jun 2005 
    /// http://www.codeproject.com/KB/edit/SyntaxRichTextBox.aspx
    /// </summary>
	public class ScriptEditorControl : System.Windows.Forms.RichTextBox
    {
        #region Syntax higlighting members

        private SyntaxSettings syntaxSettings = SyntaxSettings.Default;
        public SyntaxSettings Settings
        {
            get { return syntaxSettings; }
        }

        private bool paintControl = true;
        private bool PaintControl
        {
            get { return paintControl; }
            set
            {
                paintControl = value;
            }
        }

        private Regex keywordsRegexp;
        private Regex typeNamesRegexp;
        private Regex stringsRegexp;
        private Regex commentsRegexp;

        private bool mSuppressHightlighting = false;
        #endregion

        #region Intellisense members
        private ImageList imageList;
        private IntellisenseListBox iBox;

        private Hashtable mVariables = new Hashtable();
        private Hashtable mDefinedTypes = new Hashtable();

        private string mTypedSincePeriod = string.Empty;
        #endregion

        private ScriptDocument mScript;

        private bool mRestrictToBlock = false;
        
        public ScriptEditorControl()
        {            
            InitializeComponent();
            CompileRegexps();
            
            mScript = new ScriptDocument(this);
            
            if (!this.DesignMode)
            {
//                Timer timer = new Timer();
//                timer.Interval = 1000;
//                timer.Tick += new EventHandler(ParseVariables);
//                timer.Start();

				// parsevariables is for intellisense, not highlighting.... maybe we wont use it
				// but i think the timer ahead was hack way to keep the intellisense updating...
                ParseVariables(null, null);
                
                mScript.ReferencedAssemblies.ReferencedAssembliesChanged += new ReferencedAssembliesChanged(this.OnReferencedAssembliesChanged);
                mScript.ScriptCodeChanged += new ScriptSourceChangedHandler(ScriptSourceChanged);
                ParseAssemblies();
            }
        }
                
                
        // Quick hack used not to accept enter keypresses unless the prev line 
        // contains leading whitespace. This effectively limits the editor to
        // the current block (provided the template has whitespace leading the
        // lines in this block). This can be easily circumvented by adding whitespace
        // to block lines, but it's a nice subtle hint to stick with the block anyway.
        public bool RestrictToBlock
        {
            get { return mRestrictToBlock; }
            set { mRestrictToBlock = value; }
        }
       

        [Browsable(false)] 
        [Editor()]
        [EditorBrowsable(EditorBrowsableState.Never)]        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]        
        public ScriptDocument Script
        {
            get 
            {
                return mScript; 
            }
            set 
            {
                mScript = value;
               
                if (mScript != null)
                {
                    // remove listeners from old script
                    mScript.ReferencedAssemblies.ReferencedAssembliesChanged -= new ReferencedAssembliesChanged(this.OnReferencedAssembliesChanged);
                    mScript.ScriptCodeChanged -= new ScriptSourceChangedHandler(ScriptSourceChanged);
                }

                
                if (mScript != null)
                {
                    // add listeners to new script
                    mScript.ScriptCodeChanged += new ScriptSourceChangedHandler(ScriptSourceChanged);
                    mScript.ReferencedAssemblies.ReferencedAssembliesChanged += new ReferencedAssembliesChanged(this.OnReferencedAssembliesChanged);
                    ParseAssemblies();
                }
            }
        }

        void ScriptSourceChanged(string newSource, string oldSource)
        {
            this.Text = newSource;
        }

        public void OnReferencedAssembliesChanged()
        {            
            ParseAssemblies();
        }

                
        void ParseVariables(object sender, EventArgs e)
        {
            try
            {
                mVariables.Clear();
                string text = this.Text;

                foreach (string type in mDefinedTypes.Keys)
                {
                    string typeSearch = type + " ";
                    int searchLen = typeSearch.Length;

                    int index = text.IndexOf(typeSearch);

                    while (index > -1)
                    {
                        int spaceIndex = text.IndexOfAny(new char[] { ' ', ')', ';', '=' }, index + searchLen);
                        int varNameLen = spaceIndex - (index + searchLen);

                        if (spaceIndex > -1 && varNameLen > 0)
                        {
                            string varName = text.Substring(index + searchLen, varNameLen);

                            if (!mVariables.ContainsKey(varName))
                            {
                                mVariables.Add(varName, type);
                            }

                            //System.Diagnostics.Debug.WriteLine(string.Format("Found variable '{0}' of type {1}", varName, type));
                        }

                        index = text.IndexOf(typeSearch, index + 1);
                    }
                }

                //System.Diagnostics.Debug.WriteLine("---");
            }
            catch
            {
            }
        }

        private void InitializeComponent()
        {
            this.AcceptsTab = true;
            this.Font = new Font(FontFamily.GenericMonospace, 8f);
            this.EnableAutoDragDrop = true;
            this.SelectionHangingIndent = 10;
            this.DetectUrls = false;
            this.WordWrap = false;
            this.AutoWordSelection = true;
                        
            this.imageList = new ImageList();

            imageList.ImageSize = new Size(16, 16);
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            // http://msdn.microsoft.com/en-us/magazine/cc163609.aspx
            //imageList.Images.Add(new Bitmap(this.GetType(), "Images.namespace.gif"));
            //imageList.Images.Add(new Bitmap(this.GetType(), "Images.class.gif"));
            //imageList.Images.Add(new Bitmap(this.GetType(), "Images.method.gif"));
            //imageList.Images.Add(new Bitmap(this.GetType(), "Images.property.gif"));
            //imageList.Images.Add(new Bitmap(this.GetType(), "Images.event.gif"));
            //imageList.Images.Add(new Bitmap(this.GetType(), "Images.interface.gif"));

            iBox = new IntellisenseListBox();
            iBox.ImageList = this.imageList;
            iBox.Size = new Size(300, 100);
            iBox.Visible = false;
            this.Controls.Add(iBox);

            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TBKeyDown);        
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TBMouseDown);

            
        }


        private void TBKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {           
            if (e.KeyData == Keys.OemPeriod)
            {
                if (!iBox.Visible)
                {
                    mTypedSincePeriod = string.Empty;

                    string lastWord = GetLastWord();
                    //Type type = Type.GetType( lastWord, false ); //TODO: from script subject assemblies
                    int periodIndex = lastWord.IndexOf('.');

                    if (mVariables.ContainsKey(lastWord))
                    {
                        // shortcut for variables

                        Type type = (Type)mDefinedTypes[mVariables[lastWord]];
                        ShowIntellisenseBox(type);
                    }
                    else if (periodIndex != -1 && periodIndex != lastWord.Length - 1)
                    {
                        // since we have periods in our last works, let's see what we can do...
                        string[] tokens = lastWord.Split('.');

                        // if the first token is a variable of a known type
                        if (mVariables.ContainsKey(tokens[0]))
                        {
                            // get known base type
                            Type type = (Type)mDefinedTypes[mVariables[tokens[0]]];
                            
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                string token = tokens[i];

                                // chop off parentheses and parameters for methods
                                if (token.EndsWith(")"))
                                {
                                    token = token.Substring(0, token.IndexOf("("));
                                }

                                // get the member information for this token
                                MemberInfo[] mi = type.GetMember(token);
                                
                                // extract type info for token
                                if (mi.Length > 0)
                                {
                                    switch (mi[0].MemberType)
                                    {
                                        case MemberTypes.Property:
                                            type = ((PropertyInfo)mi[0]).PropertyType;
                                            break;

                                        case MemberTypes.Method:
                                            type = ((MethodInfo)mi[0]).ReturnType;
                                            break;
                                    }
                                    
                                }


                                // break to prevent null ref on next iteration
                                if (type == null)
                                {
                                    break;
                                }
                            }

                            // if we actually found a type
                            if (type != null)
                            {                                
                                ShowIntellisenseBox(type);
                            }
                        }
                    }
                        
                }
                else
                {
                    ConfirmIntellisenseBox();
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (iBox.Visible)
                {
                    if (mTypedSincePeriod == string.Empty)
                    {
                        iBox.Visible = false;
                    }
                    else
                    {                        
                        mTypedSincePeriod = mTypedSincePeriod.Substring(0, mTypedSincePeriod.Length - 1);

                        for (int i = 0; i < this.iBox.Items.Count; i++)
                        {
                            if (this.iBox.Items[i].ToString().ToLower().StartsWith(mTypedSincePeriod.ToLower()))
                            {
                                iBox.SelectedIndex = i;
                                break;
                            }
                        }                 
                    }
                }
            }
            else if (e.KeyCode == Keys.Up)
            {
                // The up key moves up our member list, if
                // the list is visible
                if (iBox.Visible)
                {
                    if (iBox.SelectedIndex > 0)
                    {
                        iBox.SelectedIndex--;
                    }

                    e.Handled = true;
                    this.Focus();
                }

            }
            else if (e.KeyCode == Keys.Down)
            {
                if (iBox.Visible)
                {
                    if (iBox.SelectedIndex < iBox.Items.Count - 1)
                    {
                        iBox.SelectedIndex++;
                    }

                    e.Handled = true;
                    this.Focus();
                }
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                if (iBox.Visible)
                {
                    if (iBox.SelectedIndex < iBox.Items.Count - 10)
                    {
                        iBox.SelectedIndex += 10;
                    }
                    else
                    {
                        iBox.SelectedIndex = iBox.Items.Count - 1;
                    }

                    e.Handled = true;
                    this.Focus();
                }
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                if (iBox.Visible)
                {
                    if (iBox.SelectedIndex > 10)
                    {
                        iBox.SelectedIndex -= 10;
                    }
                    else
                    {
                        iBox.SelectedIndex = 0;
                    }

                    e.Handled = true;
                    this.Focus();
                }
            }
            else if (e.KeyCode == Keys.End)
            {
                if (iBox.Visible)
                {
                    iBox.SelectedIndex = iBox.Items.Count - 1;
                    e.Handled = true;
                    this.Focus();
                }
            }
            else if (e.KeyCode == Keys.Home)
            {
                if (iBox.Visible)
                {
                    iBox.SelectedIndex = 0;
                    e.Handled = true;
                    this.Focus();
                }
            }
            else if (e.KeyCode == Keys.D9)
            {
                // Trap the open bracket key, displaying a cheap and
                // cheerful tooltip if the word just typed is in our tree
                // (the parameters are stored in the tag property of the node)
            }
            else if (e.KeyCode == Keys.D8)
            {
                // Close bracket key, hide the tooltip textbox
            }
            else if (e.KeyValue < 48 || (e.KeyValue >= 58 && e.KeyValue <= 64) || (e.KeyValue >= 91 && e.KeyValue <= 96) || e.KeyValue > 122)
            {
                // Check for any non alphanumerical key, hiding
                // member list box if it's visible.
                if (e.KeyCode == Keys.Return)
                {
                    if (!iBox.Visible)
                    {
                        string lastLine = GetLastLine();

                        char[] chars = lastLine.ToCharArray();

                        string whiteSpace = "\r\n";
                        int i = 0;

                        while (i < chars.Length && Char.IsWhiteSpace(chars[i]))
                        {
                            whiteSpace += chars[i];
                            i++;
                        }

                        if (whiteSpace == "\r\n")
                        {
                            // we've got no identation on the prev line, so we can 
                            // use this to restrict the editor acces to this block
                            // because of the identation our template provides in
                            // the default block.
                            if (RestrictToBlock)
                            {
                                MessageBox.Show("The editor is limited to the current code block, please type your code in this block." + (System.Diagnostics.Debugger.IsAttached ? "\n\n(Note to developers: this is a bit of a messy hack, refer to the code comments)" : ""), "Editor block restriction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                e.Handled = true;
                            }
                        }
                        else
                        {
                            // just insert the whitespace
                            InsertAtCaret(whiteSpace);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        ConfirmIntellisenseBox();
                        e.Handled = true;
                    }
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    iBox.Visible = false;
                    mTypedSincePeriod = string.Empty;
                }
            }
            // We call Cut() and Paset() directly on Ctrl+X and Ctrl+V
            // then in WndProc, we will handle the cut and paste events.
            else if(e.Control && e.KeyCode == Keys.V  )
            {
                  Paste();
                  //ProcessAllLines();
                  e.Handled = true;
            }
            else if(e.Control && e.KeyCode == Keys.X  )
            {
                  Cut();
                  //ProcessAllLines();
                  e.Handled = true;
            }
            else
            {
                // Letter or number typed, search for it in the listview
                if (iBox.Visible)
                {

                    mTypedSincePeriod += (char)e.KeyValue;

                    for (int i = 0; i < this.iBox.Items.Count; i++)
                    {
                        if (this.iBox.Items[i].ToString().ToLower().StartsWith(mTypedSincePeriod.ToLower()))
                        {
                            iBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    mTypedSincePeriod = string.Empty;
                }
            }
        }

        internal void ConfirmIntellisenseBox()
        {
            if (mTypedSincePeriod != string.Empty)
            {
                int curPos = SelectionStart;
                SelectionStart = curPos - mTypedSincePeriod.Length;
                SelectionLength = mTypedSincePeriod.Length;

                DeleteSelection();
            }

            InsertAtCaret(iBox.SelectedItem.ToString());
            iBox.Visible = false;
            mTypedSincePeriod = string.Empty;
            this.Focus();
        }

        private void DeleteSelection()
        {
            IDataObject clipData = Clipboard.GetDataObject();
            Cut();
            if (clipData != null) Clipboard.SetDataObject(clipData);
        }

        private void ShowIntellisenseBox(Type type)
        {
            iBox.Populate(type);

            if (iBox.Items.Count > 0)
            {
                iBox.SelectedIndex = 0;
            }

            Point topLeft = this.GetPositionFromCharIndex(this.SelectionStart);
            topLeft.Offset(-35, 18);

            if (this.Size.Height < (topLeft.Y + iBox.Height))
            {
                topLeft.Offset(0, -18 - 18 - iBox.Height);
            }

            if (this.Size.Width < (topLeft.X + iBox.Width ))
            {
                topLeft.Offset(35 + 15 - iBox.Width, 0);
            }
            
            if (topLeft.X < 0)
            {
                topLeft.X = 0;
            }

            if (topLeft.Y < 0)
            {
                topLeft.Y = 0;
            }


            iBox.Location = topLeft;

            iBox.Visible = true;
        }

        public void InsertAtCaret(string text)
        {
            /*
            int caretPos = this.SelectionStart;

            suppressHightlighting = true;
            this.Text = this.Text.Insert(caretPos, text);
            this.SelectionStart = caretPos + text.Length - 1;
            suppressHightlighting = false;

            ProcessAllLines();
             */

            IDataObject clipData = Clipboard.GetDataObject();

            Clipboard.SetText(text);
            this.Paste();

            if (clipData != null) Clipboard.SetDataObject(clipData);
        }

        private void TBMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            iBox.Visible = false;                
        }

        private string GetLastWord()
        {
            int pos = this.SelectionStart - 1;

            while (pos > 1)
            {
                string substr = this.Text.Substring(pos - 1, 1);

                if (Char.IsWhiteSpace(substr, 0))
                {
                    return Text.Substring(pos, this.SelectionStart - pos);
                }

                pos--;
            }

            return Text.Substring(0, this.SelectionStart);            
        }


        private string GetLastLine()
        {
            int charIndex = SelectionStart;
            int currentLineNumber = GetLineFromCharIndex(charIndex);

            // the carriage return hasn't happened yet... 
            //      so the 'previous' line is the current one.
            string previousLineText;
            if (Lines.Length <= currentLineNumber)
                previousLineText = Lines[Lines.Length - 1];
            else
                previousLineText = Lines[currentLineNumber];

            return previousLineText;
        }

        private string GetCurrentLine()
        {
            int charIndex = SelectionStart;
            int currentLineNumber = GetLineFromCharIndex(charIndex);

            if (currentLineNumber < Lines.Length)
            {
                return Lines[currentLineNumber];
            }
            else
            {
                return string.Empty;
            }
        }

        private void ParseAssemblies()
        {
            mDefinedTypes.Clear();

            if (mScript != null)
            {
                foreach (string assemblyName in mScript.ReferencedAssemblies)
                {
                    try
                    {
                        // This actually works for all assemblies, but the CLR API 
                        // assemblies are huge and will kill the editor

                        /*
                        string name = assemblyName;

                        if (name.ToLower().EndsWith(".dll") || name.ToLower().EndsWith(".exe"))
                        {
                            //should always be true, but check to make sure
                            name = name.Substring(0, name.Length - 4); // chop extension
                        }

                        Assembly subjectAssembly = Assembly.LoadWithPartialName(name);
                         */


                        // So we'll just load the assemblies we can load from project files directly (no gac)
                        Assembly subjectAssembly = Assembly.LoadFrom(assemblyName);
                        Type[] assemblyTypes = subjectAssembly.GetExportedTypes();

                        // Cycle through types
                        foreach (Type type in assemblyTypes)
                        {
                            if (!mDefinedTypes.ContainsKey(type.Name))
                            {
                                // ignore new additions of this known type
                                mDefinedTypes.Add(type.Name, type);
                            }
                        }
                    }
                    catch (Exception exp)
                    {
                        System.Diagnostics.Debug.WriteLine(exp);
                    }
                }

                CompileTypeNamesRegexp();
                ProcessAllLines (this.Text);
            }
        }

        #region Syntax highlighting functions
       	
        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                                
            	if (!mSuppressHightlighting)
                {
                    value = ProcessAllLines(value);
                }
                

            }
        }
        
		/// <summary>
		/// OnTextChanged
		/// </summary>
		/// <param name="e"></param>
		protected override void OnTextChanged(EventArgs e)
		{
            if (mSuppressHightlighting)
            {
                return;
            }

			// Process this line.

            PaintControl = false;
            string line = GetCurrentLine();
            int lineStart = GetFirstCharIndexOfCurrentLine();

            // TODO: this does not process multiple lines in the case of a cut/paste operation
			ProcessLine(line, lineStart);
			PaintControl = true;
		}
		
		/// <summary>
		/// Process a line.
		/// </summary>
		private void ProcessLine(string line, int lineStart)
		{
			if (string.IsNullOrEmpty (line)) return;
			// Save the position and make the whole line black
			int nPosition = SelectionStart;
			SelectionStart = lineStart;
			SelectionLength = line.Length;
        	SelectionColor = Color.Black;

            
			// Process the keywords
			ProcessRegex(ref line, lineStart, keywordsRegexp, Settings.KeywordColor);

            // Process cached type names
            ProcessRegex(ref line, lineStart, typeNamesRegexp, Settings.TypeColor);

            /*
			// Process numbers, ignore since VS2005 doesn't do this either
			if(Settings.EnableIntegers)
                ProcessRegex(line, lineStart, "\\b(?:[0-9]*\\.)?[0-9]+\\b", Settings.IntegerColor);
             */

			// Process strings
            if (Settings.EnableStrings)
            {
                ProcessRegex(ref line, lineStart, stringsRegexp, Settings.StringColor);
            }

			// Process comments
            if (Settings.EnableComments && !string.IsNullOrEmpty(Settings.Comment))
            {
                ProcessRegex(ref line, lineStart, commentsRegexp, Settings.CommentColor);
            }

			SelectionStart = nPosition;
			SelectionLength = 0;
			SelectionColor = Color.Black;
            
		}

		public string ProcessAllLines(string lines)
		{
			if (string.IsNullOrEmpty (lines)) return null;
			
			PaintControl = false;
            mSuppressHightlighting = true;

            // Save the position and make the whole line black
            int nPosition = SelectionStart;
            SelectionStart = 0;
            SelectionLength = lines.Length;
            SelectionColor = Color.Black;

            // Process the keywords
            ProcessRegex(ref lines, 0, keywordsRegexp, Settings.KeywordColor);

            // Process cached type names
            ProcessRegex(ref lines, 0, typeNamesRegexp, Settings.TypeColor);

            /*
			// Process numbers, ignore since VS2005 doesn't do this either
			if(Settings.EnableIntegers)
                ProcessRegex(Text, 0, "\\b(?:[0-9]*\\.)?[0-9]+\\b", Settings.IntegerColor);
             */

            // Process strings
            if (Settings.EnableStrings)
            {
                ProcessRegex(ref lines, 0, stringsRegexp, Settings.StringColor);
            }

            // Process comments
            if (Settings.EnableComments && !string.IsNullOrEmpty(Settings.Comment))
            {
                ProcessRegex(ref lines, 0, commentsRegexp, Settings.CommentColor);
            }

            SelectionStart = nPosition;
            SelectionLength = 0;
            SelectionColor = Color.Black;


            mSuppressHightlighting = false;
			PaintControl = true;
			
			
			return lines;
        }
        
        /// <summary>
		/// Process a regular expression.
		/// </summary>
		/// <param name="strRegex">The regular expression.</param>
		/// <param name="color">The color.</param>
        private void ProcessRegex(ref string line, int lineStart, Regex regexp, Color color)
		{
            if (regexp == null)
            {
                // for uninitialized typename regexp
                return;
            }

			Match regMatch;

			for (regMatch = regexp.Match(line); regMatch.Success; regMatch = regMatch.NextMatch())
			{
				// Process the words
				int nStart = lineStart + regMatch.Index;
				int nLenght = regMatch.Length;
				SelectionStart = nStart;
				SelectionLength = nLenght;
				SelectionColor = color;
			}
		}

		/// <summary>
		/// Compiles the keywords as a regular expression.
		/// </summary>
        private void CompileRegexps()
		{
            string keywords = string.Empty;

			for (int i = 0; i < Settings.Keywords.Count; i++)
			{
				string strKeyword = Settings.Keywords[i];

				if (i == Settings.Keywords.Count-1)
					keywords += "\\b" + strKeyword + "\\b";
				else
					keywords += "\\b" + strKeyword + "\\b|";
			}

            keywordsRegexp = new Regex(keywords, RegexOptions.Compiled | RegexOptions.Multiline);
            stringsRegexp = new Regex("\"[^\"\\\\\\r\\n]*(?:\\\\.[^\"\\\\\\r\\n]*)*\"", RegexOptions.Compiled | RegexOptions.Multiline);
            commentsRegexp = new Regex(Settings.Comment + ".*$", RegexOptions.Compiled | RegexOptions.Multiline);
		}

        /// <summary>
        /// Compiles the type names as a regular expression.
        /// </summary>
        public void CompileTypeNamesRegexp()
        {
            string typeNames = string.Empty;
            int i = 0;

            foreach( string typeName in mDefinedTypes.Keys)
            {

                if (i == mDefinedTypes.Count - 1)
                {
                    typeNames += "\\b" + typeName + "\\b";
                }
                else
                {
                    typeNames += "\\b" + typeName + "\\b|";
                }

                i++;
            }


            typeNamesRegexp = new Regex(typeNames, RegexOptions.Compiled | RegexOptions.Multiline);
        }
        #endregion
    }


    #region Helper class for storing syntax highlight settings
    /// <summary>
	/// Class to store syntax objects in.
	/// </summary>
	public class SyntaxList
	{
		public List<string> m_rgList = new List<string>();
		public Color m_color = new Color();
	}

	/// <summary>
	/// Settings for the keywords and colors.
	/// </summary>
	public class SyntaxSettings
	{
        SyntaxList keywords = new SyntaxList();
        SyntaxList typeNames = new SyntaxList();
		string commentString = "//";
		Color commentColor = Color.FromArgb( 0, 128, 0 );
		Color stringColor = Color.FromArgb(163, 21, 21 );
		Color literalColor = Color.Black;
		bool highlightComments = true;
		bool highlightLiterals = false;
		bool highlightStrings = true;
        Color typeColor = Color.FromArgb(43, 145, 175);

        public Color TypeColor
        {
            get { return typeColor; }
            set { typeColor = value; }
        }

        public static SyntaxSettings Default
        {
            get
            {
                SyntaxSettings settings = new SyntaxSettings();
                settings.KeywordColor = Color.FromArgb(0, 0, 255);
                settings.Comment = "//";
                settings.Keywords.AddRange(new string[] { "using", "public", "private", "new", "get", "set", 
                    "void", "string", "object", "true", "false", "bool", "int", "float", "short", 
                    "double", "static", "class", "return", "while", "for", "if", "else", "this", "null" });

                return settings;
            }
        }

		#region Properties
		/// <summary>
		/// A list containing all keywords.
		/// </summary>
		public List<string> Keywords
		{
			get { return keywords.m_rgList; }
		}

        /// <summary>
        /// A list containing all keywords.
        /// </summary>
        public string KeywordsExpression
        {
            get 
            {
                return "";// m_rgKeywords.m_rgList; 
            }
        }
		/// <summary>
		/// The color of keywords.
		/// </summary>
		public Color KeywordColor
		{
			get { return keywords.m_color; }
			set { keywords.m_color = value; }
		}
		/// <summary>
		/// A string containing the comment identifier.
		/// </summary>
		public string Comment
		{
			get { return commentString; }
			set { commentString = value; }
		}
		/// <summary>
		/// The color of comments.
		/// </summary>
		public Color CommentColor
		{
			get { return commentColor; }
			set { commentColor = value; }
		}
		/// <summary>
		/// Enables processing of comments if set to true.
		/// </summary>
		public bool EnableComments
		{
			get { return highlightComments; }
			set { highlightComments = value; }
		}
		/// <summary>
		/// Enables processing of integers if set to true.
		/// </summary>
		public bool EnableIntegers
		{
			get { return highlightLiterals; }
			set { highlightLiterals = value; }
		}
		/// <summary>
		/// Enables processing of strings if set to true.
		/// </summary>
		public bool EnableStrings
		{
			get { return highlightStrings; }
			set { highlightStrings = value; }
		}
		/// <summary>
		/// The color of strings.
		/// </summary>
		public Color StringColor
		{
			get { return stringColor; }
			set { stringColor = value; }
		}
		/// <summary>
		/// The color of integers.
		/// </summary>
		public Color IntegerColor
		{
			get { return literalColor; }
			set { literalColor = value; }
		}
		#endregion
    }
    #endregion
    
}
