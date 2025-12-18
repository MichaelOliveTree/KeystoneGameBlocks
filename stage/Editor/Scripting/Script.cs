using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Reflection;

namespace KeyEdit.Scripting
{
    public delegate void CompilerOutputDelegate(string outputLine);
    public delegate void ScriptSourceChangedHandler(string newSource, string oldSource);

    /// <summary>
    /// This class is primarily designed to integrate with the ScriptEditorControl
    /// </summary>
    public class ScriptDocument 
    {
        public event ScriptSourceChangedHandler ScriptCodeChanged;
        private ReferencedAssemblyCollection mReferencedAssemblies = new ReferencedAssemblyCollection();
        // TODO: this can reference a KeyStone.ScriptNode ( or we can just query for the existance of a ScriptNode in the
        //  Repository since we know the key in the repository is the resource zip path | archivepath + filename
        //          and our ScriptNode : Node, IPageableTVNode can reference the CSSCript.LoadedScript object
        private Assembly mCachedScriptAssembly = null;
        private System.Windows.Forms.RichTextBox mTextBox;
        private bool mCodeChanged = false;
        private ScriptTemplate template = ScriptTemplate.Default;
        private string mFilename;
        private string mKey;  // repository key which is form of the modname, zipname, pathInArchive, filename

        
        

        public ScriptDocument(System.Windows.Forms.RichTextBox rtb)
        {
			mTextBox = rtb;
        }
        
        
        // for syntax highlighting from form or edit control you'd do
        // Script.ReferencedAssemblies.AddRange(
        //        new string[] { "System.dll", "System.Windows.Forms.dll", "ScriptDemo.exe" });
        // note we don't need to worry about these assemblies for compilng purposes because our use of
        // the shared KeyScript.dll solves this.  User just needs the shared assembly passed to the compiler
        public ReferencedAssemblyCollection ReferencedAssemblies
        {
            get { return mReferencedAssemblies; }
            set { mReferencedAssemblies = value; }
        }

        public string Filename 
        { 
        	get { return mFilename; } 
        	set { mFilename = value; } 
        }
                
        public ScriptTemplate Template
        {
            get { return template; }
            set { template = value; }
        }
        
        public bool CodeChanged
        {
            get { return mCodeChanged; }
            set { mCodeChanged = value; }
        }

        public string Code
        {
            get { return mTextBox.Text; }
        }

        internal string TemplatedSource
        {
            get
            {
                return this.Template.ConstructScript(this.Code);
            }
        }
        
        /// <summary>
        /// This method contructs the default permission set our scripts may use.
        /// Host applications will need to supply an custom PermissionSet to the
        /// script's Run method if more permissions are needed (like the 
        /// SecurityPermissionFlag.UnmanagedCode for MDX applications).
        /// </summary>
        /// <returns>The default permission set our scripts may use</returns>
        public static PermissionSet GetDefaultScriptPermissionSet()
        {
            PermissionSet internalDefScriptPermSet = new PermissionSet(PermissionState.None);

            internalDefScriptPermSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            internalDefScriptPermSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));

            return internalDefScriptPermSet;
        }

    }
}
