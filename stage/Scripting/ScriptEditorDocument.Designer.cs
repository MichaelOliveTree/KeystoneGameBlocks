namespace KeyEdit.Scripting
{
    partial class ScriptEditorDocument
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.buttonNewScript = new System.Windows.Forms.ToolStripButton();
            this.buttonSaveScript = new System.Windows.Forms.ToolStripButton();
            this.scriptEditorControl = new KeyEdit.Scripting.ScriptEditorControl();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.buttonNewScript,
            this.buttonSaveScript});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(538, 25);
            this.toolStrip1.TabIndex = 10;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // buttonNewScript
            // 
            this.buttonNewScript.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.buttonNewScript.Image = global::KeyEdit.Properties.Resources.doc_empty_icon_16;
            this.buttonNewScript.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonNewScript.Name = "buttonNewScript";
            this.buttonNewScript.Size = new System.Drawing.Size(23, 22);
            this.buttonNewScript.Text = "toolStripButton1";
            this.buttonNewScript.Click += new System.EventHandler(this.buttonNewScript_Click);
            // 
            // buttonSaveScript
            // 
            this.buttonSaveScript.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.buttonSaveScript.Image = global::KeyEdit.Properties.Resources.save_icon_24;
            this.buttonSaveScript.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.buttonSaveScript.Name = "buttonSaveScript";
            this.buttonSaveScript.Size = new System.Drawing.Size(23, 22);
            this.buttonSaveScript.Text = "toolStripButton2";
            this.buttonSaveScript.Click += new System.EventHandler(this.buttonSaveScript_Click);
            // 
            // scriptEditorControl
            // 
            this.scriptEditorControl.AcceptsTab = true;
            this.scriptEditorControl.AutoWordSelection = true;
            this.scriptEditorControl.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.scriptEditorControl.DetectUrls = false;
            this.scriptEditorControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scriptEditorControl.EnableAutoDragDrop = true;
            this.scriptEditorControl.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.scriptEditorControl.Location = new System.Drawing.Point(0, 25);
            this.scriptEditorControl.Name = "scriptEditorControl";
            this.scriptEditorControl.RestrictToBlock = false;
            this.scriptEditorControl.Size = new System.Drawing.Size(538, 438);
            this.scriptEditorControl.TabIndex = 0;
            this.scriptEditorControl.Text = "";
            this.scriptEditorControl.WordWrap = false;
            // 
            // ScriptEditorDocument
            // 
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.scriptEditorControl);
            this.Controls.Add(this.toolStrip1);
            this.Name = "ScriptEditorDocument";
            this.Size = new System.Drawing.Size(538, 463);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Scripting.ScriptEditorControl scriptEditorControl;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton buttonNewScript;
        private System.Windows.Forms.ToolStripButton buttonSaveScript;
    }
}
