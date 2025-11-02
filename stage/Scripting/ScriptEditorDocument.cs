using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace KeyEdit.Scripting
{
    public partial class ScriptEditorDocument : UserControl
    {
        private EventHandler mOnSaveHandler;
        private EventHandler mOnChangeHandler;
        
        public ScriptEditorDocument(string name, EventHandler onSave, EventHandler onChange)
        {

            InitializeComponent();
            Name = name;
            mOnSaveHandler = onSave;
            mOnChangeHandler = onChange;

        }
       
        public ScriptDocument Script { get { return scriptEditorControl.Script; } }

        public void LoadScript(KeyCommon.IO.ResourceDescriptor descriptor)
        {
            try
            {
                string fullPath;
                string code;
                if (descriptor.IsArchivedResource)
                {
                	fullPath = Keystone.Core.FullNodePath (descriptor.ModName);
                    code = KeyCommon.IO.ArchiveIOHelper.GetTextFromArchive(descriptor.EntryName, "", fullPath);
                }
                else
                {
                	fullPath = Keystone.Core.FullNodePath(descriptor.EntryName);
                    code = System.IO.File.ReadAllText(fullPath);
                }
                // and this script can be an .FX or .CSS 
                ScriptDocument script = new ScriptDocument(scriptEditorControl);
                script.Filename = descriptor.ToString ();
                scriptEditorControl.Script = script;
                scriptEditorControl.Text = code;               
            }
            catch (Exception ex)
            {
            	System.Diagnostics.Debug.WriteLine ("ScriptEditorDocument.LoadScript() - " + ex.Message);
            }
        }

        
        // TODO: needs to fire an OnSave_Click event really
        private void buttonSaveScript_Click(object sender, EventArgs e)
        {
            if (mOnSaveHandler != null) mOnSaveHandler(sender, e);
        }

        private void buttonNewScript_Click(object sender, EventArgs e)
        {
            // TODO: popup message box perhaps for user to open the assets gallery and add a new script there then double
            // click it to start editing
        }
        

        //private void LoadHighlightSettings()
        //{
        //    // Add the keywords to the list.

        //    editorRichTextBox.Settings.Keywords.Add("function");
        //    editorRichTextBox.Settings.Keywords.Add("if");
        //    editorRichTextBox.Settings.Keywords.Add("then");
        //    editorRichTextBox.Settings.Keywords.Add("else");
        //    editorRichTextBox.Settings.Keywords.Add("elseif");
        //    editorRichTextBox.Settings.Keywords.Add("end");

        //    // Set the comment identifier. 

        //    // For Lua this is two minus-signs after each other (--).

        //    // For C++ code we would set this property to "//".

        //    editorRichTextBox.Settings.Comment = "--";

        //    // Set the colors that will be used.

        //    editorRichTextBox.Settings.KeywordColor = Color.Blue;
        //    editorRichTextBox.Settings.CommentColor = Color.Green;
        //    editorRichTextBox.Settings.StringColor = Color.Gray;
        //    editorRichTextBox.Settings.IntegerColor = Color.Red;

        //    // Let's not process strings and integers.

        //    editorRichTextBox.Settings.EnableStrings = false;
        //    editorRichTextBox.Settings.EnableIntegers = false;

        //    // Let's make the settings we just set valid by compiling

        //    // the keywords to a regular expression.

        //    editorRichTextBox.CompileKeywords();
        //}
    }
}
