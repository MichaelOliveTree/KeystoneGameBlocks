using System;
using System.IO;
using System.Reflection;
using CSScriptLibrary;
using System.Collections.Generic;

namespace KeyScript.Host
{


    public static class Generator
    {
    // TODO: it appears I can use the "precompiler" to compile an ENUM at runtime
    //       and then i can use this Enum with our scripts and our scripts can still  use full
    //       EnumName.EnumMember syntax as well as the Game01.dll.  
    //       This will be better than any other option such as building an Enum dynamically at runtime
    //       and then only being able to access it generically and losing the friendly syntax which
    //       makes Enums useful in the first place.

    // NOTE: Once the Enum is generated, we can then use the regular KeyScript.Loader to compile it into a binary

    // 
    // TODO: https://www.csscript.net/help/precompilers.html
    // download latest cs-script from https://github.com/oleg-shilo/cs-script

        public bool GenerateEnum()
        {
            return false;
        }

        
    }



    public class Loader
    {   	
//        private CSScript.LoadedScript PropertyConverters;
//        private CSScript.LoadedScript UserConstants;
//        private CSScript.LoadedScript UserFunctions;   
//        private CSScript.LoadedScript UserTypes;
        private CSScript.LoadedScript[] UserAssemblies;
			
        private const string mSharedAssemblyName = "KeyScript"; // name of this assembly KeyScript.dll
        private string mSharedAssemblyFolder;
        private AsmHelper mAssemblyHelper;
        // we have to manage our own cache because CSScript's cache does not use a dictionary and even if it did
        // it wouldn't want to allow you to replace old assemblies with new ones using the old key.  This is because
        // you cannot unload these assemblies and so their script cache must persist.  So if we want to be able to search 
        // for the active scripts that are most up to date, then when we edit a script and recompile, we must be allowed to
        // update our dictionary to always just have the one most up to date assembly referenced.
        private System.Collections.Generic.Dictionary<string, CSScript.LoadedScript> mScriptCache;
       
        public Loader(string scriptFolder)
        {
            CSScript.CacheEnabled = true;
            
            // Find the path that this KeyScript.dll will be compiled to.  This will be then also be used as
			// the folder that will host the compiled user scripts (both the shared UserAssemblies and DomainObject and AI Behavior scripts)
			// since they must be in same location.
            mSharedAssemblyFolder  = Path.GetDirectoryName(GetLoadedAssemblyLocation(mSharedAssemblyName));
            //CSScript.LoadedScript loadScripts = CSScript.ScriptCache  // this is maintained for us
            mScriptCache = new System.Collections.Generic.Dictionary<string, CSScript.LoadedScript>();

            LoadUserAssemblies(scriptFolder);
        }

        private CSScript.LoadedScript LoadFromFile(string path)
        {
            return LoadFromFile(path, null);
           
        }

        private static string GetLoadedAssemblyLocation(string name)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = Path.GetFileNameWithoutExtension(asm.FullName.Split(",".ToCharArray())[0]);
                if (string.Compare(name, asmName, true) == 0)
                    return asm.Location;
            }
            return "";
        }

        private object mLoadLock = new object();
        public CSScript.LoadedScript LoadCode(string code, string archivePath, bool debug)
        { 
            // we use the archive path to determine the temporary file name in the shared assembly folder
            string fileName = archivePath.Replace("\\", "_");
            fileName = fileName.Replace("/", "_"); // archive paths might have use forward slashes instead of backslashes
            fileName = fileName.Replace(":", "_"); // replace any included drive colon with underscore as well
            fileName = fileName.Replace("|", "_");

            // crc32 assumes ascii string
            int crc = System.BitConverter.ToInt32(Keystone.Utilities.CRC32.Crc32(code), 0);
            System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadCode() - Code CRC32 = " + crc.ToString());
            // create a new name for the copy file that will always be the same given the original
            // fullpath and original sharedAssembly folder path and file crc32
            // as well as never clashing with other folder and file names
            string sharedSourcePath = Path.Combine(mSharedAssemblyFolder, fileName);
            string assemblyPath = System.IO.Path.ChangeExtension(sharedSourcePath, crc.ToString() +  ".dll");
            System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadCode() - Attempting to load '" + assemblyPath +"'");

            // can we find a cached copy of this version of the assembly?
            CSScript.LoadedScript loadedScript = null; //= GetCachedScriptAssembly(scriptCopy);
            
            // we must lock this because if we try to load for instance two Star domain objects
            // from different threads, the second star will not be able to share the previous because
            // it hasnt been compiled yet and until its compiled it cant be added to the cache
            lock (mLoadLock)
            {
                if (mScriptCache.ContainsKey(assemblyPath))
                    loadedScript = mScriptCache[assemblyPath];

                if (loadedScript != null)
                {
                    // if the code version is identical, we can reuse the existing (such as when we are just 
                    // dragging and dropping the same script onto multiple entities)
                    string original = File.ReadAllText(loadedScript.script);
                    if (original == code)
                    {
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadCode() - SUCCESS: Using Cached Assembly '"+ assemblyPath +"'");
                    	return loadedScript;
                    }
                    else
                        mScriptCache.Remove(assemblyPath);
                }

                Assembly assembly = null;

                string sharedScriptAssemblyPath;
                //string te
                //try
                //{
                //    File.Delete(scriptCopy);
                //}
                //catch (Exception ex)
                //{
                //    System.Diagnostics.Debug.WriteLine(ex.Message + " --File.Delete-- " + scriptCopy);
                //}
                //try
                //{
                //    // allows us to have our scripts in another directory but then create a copy we'll copy over
                //    // to the shared assembly's directory.  It's important to compile in the same folder as the loaded
                //    // shared assembly so that there is no conflicts between the host and client apps.
                //    System.Diagnostics.Debug.WriteLine("Writing script to temporary file " + scriptCopy);
                //    File.WriteAllText(scriptCopy, code);
                //}
                //catch (Exception ex)
                //{
                //    System.Diagnostics.Debug.WriteLine(ex.Message + " --File.Copy-- " + scriptCopy);
                //}
                try
                {
                    // TODO: how do you reload an updated code version of the assembly with an existing 
                    // assembly name if you cant even unload the assembly?  I don't think you can..
                    // 1: possible solution is dynamicmethod from IL... seems the trick is yous till compile
                    // your objects, but you dont load them.  Instead you use reflection.emit on the dll
                    // to grab the IL and make a dynamic method from it.
                    // http://www.codeproject.com/Articles/18004/Net-Expression-Evaluator-using-DynamicMethod
                    // 2: but in the meantime, we'll use crc32 and append that to each script's filepath
                    // so that every change will result in a new named dll
                    CSScript.CacheEnabled = false;

                    string[] referencedAssemblies = GetUserAssemblyPaths();
                    
                    if (referencedAssemblies != null)
                    	assembly = CSScript.LoadCode(code, assemblyPath, debug, referencedAssemblies);
                    else
                        assembly = CSScript.LoadCode(code, assemblyPath, debug);
                    
                    System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadCode() - SUCCESS: '" + assemblyPath + "'");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("KeyScript.HostLoader.LoadCode() - ERROR: " + assemblyPath + " - " + ex.Message);
                }

                if (assembly == null) return null;

                // here since we know the assembly loaded, let's grab the loadedScript object from the CSScript cache
                loadedScript = GetCachedScriptAssembly(assembly.Location);
                mScriptCache.Add(assemblyPath, loadedScript);
                System.Diagnostics.Trace.Assert(loadedScript.asm == assembly);
            }

            return loadedScript;
        }

        //https://stackoverflow.com/questions/3065526/how-to-get-an-enum-value-from-an-assembly-using-late-binding-in-c-sharp
        // i should probably place COMPONENT_TYPE in game01.dll 
        // user_functions.cs could be helpful, but not for this case
        //public Enum[] GetTest()
        //{ 
        //    var enumType = UserAssemblies[1].asm.GetType("COMPONENT_TYPE");
        //    FieldInfo[] fields = enumType.GetFields();
        //    List<string> values = new List<string>();

        //    foreach (FieldInfo info in fields)

        //        values.Add (info.GetValue())

        //    return values.ToArray();
        //}

        /// <summary>
        /// Compiles all user scripted assemblies that are to be made available to all DomainObject scripts and Behavior scripts.
        /// </summary>
        /// <param name="modsPath"></param>
        private void LoadUserAssemblies(string scriptFolder)
        {
            // DomainObject (entity) scripts and AI Behavior scripts can both use
			// these UserAssemblies if the dir path of the compiled binaries of these assemblies are passed
			// to the DomainObject scripts and Behavior scripts when they themselves are being compiled.
			// Once that is done, script authors can call the functions using full namespace qualified call 
			// eg: i = UserConstants.CustomFlags.CF_STATS_DIRTY;
            // and find all functions easily
           
            
            string userAssemblyFileListing = "user_assemblies.txt";

            // Read the file and display it line by line.
            string fullPath = System.IO.Path.Combine(scriptFolder, userAssemblyFileListing);
            if (!File.Exists(fullPath))
                throw new System.IO.FileNotFoundException();

            System.IO.StreamReader file = new System.IO.StreamReader(fullPath);
			
			string line = null;
			List<string> filenames = new List<string>();
			while((line = file.ReadLine()) != null)
				filenames.Add (line);
			
			file.Close();

			UserAssemblies = new CSScript.LoadedScript[filenames.Count];
			try 
			{
				for (int i = 0; i < filenames.Count; i++)
					UserAssemblies[i] = LoadFromFile (System.IO.Path.Combine(scriptFolder, filenames[i]));
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ("KeyScript.HostLoader.LoadUserAssemblies() - ERROR: " + ex.Message);
			}
//            // TODO: remove hard coded paths to the following user assemblies and instead  
//            //       use a single hardpath to a "mods\\caesar\\scripts\\user_assemblies.txt" that then lists one per line the
//            //       assemblies i should be loading.  That way adding new user assemblies does not require a recompile of
//            //       KeyScript.dll, Keystone.dll, or KeyEdit.exe 
//            UserAssemblies = new CSScript.LoadedScript[4];
//            UserAssemblies[0] = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\user_types.css"));
//            UserAssemblies[1] = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\user_constants.css"));
//            UserAssemblies[2] = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\user_functions.css"));
//            UserAssemblies[3] = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\property_converters.css"));
            
//            UserTypes = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\user_types.css"));
//            UserConstants = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\user_constants.css"));
//            UserFunctions = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\user_functions.css"));
//            PropertyConverters = LoadFromFile(System.IO.Path.Combine(modsPath, @"common\scripts\property_converters.css"));
        }
        
        /// <summary>
        /// Returns a
        /// </summary>
        /// <returns>string array that contains path to the binary compiled user assembly</returns>
        private string[] GetUserAssemblyPaths ()
        {
        	
        	if (UserAssemblies == null || UserAssemblies[0] == null) 
        		return null;
        	
        	string[] paths = new String[UserAssemblies.Length];
        	
        	for (int i = 0; i < UserAssemblies.Length; i++)
        		paths[i] = UserAssemblies[i].asm.Location;
        	
        	return paths ;
        	
//        	return new string[]
//        	{
//        		UserAssemblies[0].asm.Location,
//        		UserAssemblies[1].asm.Location,
//        		UserAssemblies[2].asm.Location,
//        		UserAssemblies[3].asm.Location
//        	};
        	
//     		if (UserConstants == null) return null;
//
//        	return new string[]
//        		{
//        			UserConstants.asm.Location, 
//        			UserFunctions.asm.Location, 
//        			UserTypes.asm.Location, 
//        			PropertyConverters.asm.Location
//    			};
        }
        
        public CSScript.LoadedScript LoadFromFile(string fullPath, string[] referencedAssemblies)
        {

            fullPath = Path.GetFullPath(fullPath);
            string dictionaryKeyName = fullPath;

            //script (to be compiled) must be in the same folder with shared assembly to guarantee version compatibility of the compiled 
            // scripts and the the host application which all must use the same shared assembly.

            // normalize the filenames so that scripts with the same actual filename but which are located in seperate child directories
            // can co-exist.  We do this by replacing the dir seperators with underscores.
            // Remember, this is necessary because all our scripts need to be compiled in the same folder as the shared assembly
            // and that means no child directories can be used there.  So this renaming using underscores is necessary to avoid any
            // naming conflicts between different scripts.
            string fileName = fullPath.Replace("\\", "_");
            fileName = fileName.Replace(":", "_");
            // create a new name for the copy file that will always be the same given the original fullpath and original sharedAssembly folder path
            // as well as never clashing with other folder and file names.  This is done solely because
            // our shared path will not have subdirectories and we want same named scripts in different
            // paths originally, to have unique names when copied to the single file path.
            string sharedSourcePath = Path.Combine(mSharedAssemblyFolder, fileName);
            string assemblyPath = System.IO.Path.ChangeExtension(sharedSourcePath, "dll");
            System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - Attempting to load '" + assemblyPath +"'");

            // can we find a cached copy of this assembly?
            CSScript.LoadedScript loadedScript = null;
            lock (mLoadLock)
            {
                loadedScript = GetCachedScriptAssembly(assemblyPath);
                if (loadedScript != null) return loadedScript;

                Assembly assembly = null;
                if (string.Compare(Path.GetDirectoryName(fullPath), mSharedAssemblyFolder, true) == 0)
                {
                    try
                    {
                        // this path should never occur.  Our scripts will never start off in the same folder as our shared assembly
                        if (referencedAssemblies != null)
                            assembly = CSScript.Load(fullPath, null, true, referencedAssemblies);
                        else
                            assembly = CSScript.Load(fullPath, null, true);
                        
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - SUCCESS: '"+ assemblyPath +"'");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - ERROR:" + ex.Message);
                    }
                }
                else
                {
                    try
                    {
                        // delete existing shared source copy since it needs to be replaced with updated compiled version
                        File.Delete(sharedSourcePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - " + ex.Message + " --File.Delete-- " + sharedSourcePath);
                    }
                    try
                    {
                        // delete existing assembly since it needs to be replaced with updated compiled version
                        File.Delete(assemblyPath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - " + ex.Message + " --File.Delete-- " + assemblyPath);
                    }
                    try
                    {
                        // It's important to compile in the same folder as the loaded
                        // shared assembly so that there is no conflicts between the host and client apps.
                        // allows us to have our scripts in another directory but then create a copy we'll copy over
                        // to the shared directory.  
                        File.Copy(fullPath, sharedSourcePath, true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - " + ex.Message + " --File.Copy-- " + assemblyPath);
                    }
                    try
                    {
                        if (referencedAssemblies != null)
                            assembly = CSScript.Load(sharedSourcePath, assemblyPath, true, referencedAssemblies);
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Load Script File = " + sharedSourcePath);
                            System.Diagnostics.Debug.WriteLine("Load Assembly File = " + assemblyPath);
                            assembly = CSScript.Load(sharedSourcePath, assemblyPath, true);
                        }
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - SUCCESS: '"+ assemblyPath +"'");
                    }
                    catch (Exception ex)
                    {
                        // TODO: If kgb crashes and all process is not unloaded
                        // we will not be able to reload some of these dll scripts because they will be in use
                        System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.LoadFromFile() - " + ex.Message);
                    }
                }

                if (assembly == null) return null;

                // here since we kno wthe assembly loaded, let's grab the loadedScript object from the CSScript cache
                loadedScript = GetCachedScriptAssembly(assemblyPath);
                System.Diagnostics.Trace.Assert(loadedScript.asm == assembly);
            }
            return loadedScript;
        }


        CSScript.LoadedScript GetCachedScriptAssembly(string scriptFile)
        {
            foreach (CSScript.LoadedScript script in CSScript.ScriptCache)
                if (script.asm.Location == scriptFile )
                    return script;

            return null;
        }

        public object CreateObjectInstance(string scriptPath, string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            try
            {
                string[] typeInfo = type.Split(',');

                CSScript.LoadedScript script = LoadFromFile(scriptPath);
                Assembly assembly = script.asm;
                object instance = assembly.CreateInstance(typeInfo[0]); //type);
                return instance;
            }
            catch
            {
                return null;
            }

        }

        /// <summary>
        /// Finds and caches the allowed scriptable methods.  Methods that are not supported are ignored.
        /// I'm not sure why ive placed that limitation and why I don't support loading of methods that are
        /// specified by HTML for instance for link click event handler.
        /// </summary>
        /// <param name="loadedScript"></param>
        /// <param name="supportedMethodSignatures"></param>
        /// <returns></returns>
        public Dictionary<string, MethodDelegate> GetSupportedMethods(CSScript.LoadedScript loadedScript, Dictionary<string, object[]> supportedMethodSignatures)
        {
            MethodInfo[] methods = GetMethods(loadedScript);
            Dictionary<string, MethodDelegate> results = null;

            // iterate through all methods and add the ones which are supported.
            // any other methods the script writer attempts to add will be ignored.
            // TODO: why?  why don't we allow for methods to be specified
            if (methods != null && methods.Length > 0)
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    object[] args;
                    string typeName = methods[i].Module.GetTypes()[0].Name; // types[0].Name
                    string fullMethodName = typeName + "." + methods[i].Name;
                    //System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.GetSupportedMethods() - found method ' " + methods[i].Name + "'");

                    bool supportedMethod = supportedMethodSignatures.TryGetValue(methods[i].Name, out args);

                    if (supportedMethod)
                    {
                    	
                    	// TODO: i never test to see if the actual method args match those specified in our supportedMethodSignatures
                    	//if (
                    	
                        CSScriptLibrary.MethodDelegate foundMethod = null;
                        try
                        {
                        	// args are passed so that if overloads are present, the specific method will be returned
                            foundMethod = loadedScript.asm.GetStaticMethod(fullMethodName, args);
                            if (foundMethod != null)
                            {
                                if (results == null)
                                    results = new Dictionary<string, CSScriptLibrary.MethodDelegate>();

                                results.Add(methods[i].Name, foundMethod);
                                foundMethod = null;
                            }
                        }
                        catch (ApplicationException ex)
                        {
                            foundMethod = null;
                            System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.GetSupportedMethods() - ERROR: ' " + methods[i].Name + "'" + ex.Message);
                        }
                        catch (Exception ex)
                        {
                            foundMethod = null;
                            System.Diagnostics.Debug.WriteLine("KeyScript.Host.Loader.GetSupportedMethods() - ERROR: ' " + methods[i].Name + "'" + ex.Message);
                        }
                    }
                }
            }

            return results;
        }

        public MethodInfo[] GetMethods(CSScript.LoadedScript loadedScript)
        {
            // TODO: we can determine the classname easily enough, but its problematic if there's more than one in the assembly
            Module[] modules = loadedScript.asm.GetModules();
            Type[] types = modules[0].GetTypes();
            if (modules == null || modules.Length != 1 || modules[0] == null) return null; // only one module allowed in the assembly (for now?)
            if (types == null || types[0] == null) return null; // TODO: I dont know why it's returning 2 array elements so that the following lenth test of 1 fails... || types.Length != 1) return; // only one public type allowed (for now?)
            return types[0].GetMethods();

            
            
            //mScript 
            // todo compile and assign event methods here?  actually cant really assign methods here
            // that has to be done by the entity... or wait, no the entity calls this mScript.Method.Execute() right?

            //  // below method if for loading an entire object instances rather than the static method version we will mostly use
            //  //var update = mScriptLoader.Assembly.GetStaticMethod("*.Update");
            // // KeyScript.Interfaces.IDynamicEntity mod ;//= mScriptLoader.Assembly.GetModule("data.csc").AlignToInterface<KeyScript.Interfaces.IDynamicEntity>();
            ////  mod = (KeyScript.Interfaces.IDynamicEntity)mScriptLoader.Assembly.CreateObject("DefaultDynamicEntity");
            //  //Module[] modules = mScriptLoader.Assembly.GetLoadedModules();
            //  //mod = (KeyScript.Interfaces.IDynamicEntity)modules[0];
            // // mod.Update();

            // older test code
            //string script = @"E:\dev\c#\KeystoneGameBlocks\Data\pool\scripts\ship_update.css";
            //scriptEditor.LoadScript(script);
            //CSScriptLibrary.CSScript.LoadedScript loadedScript = mScriptLoader.Load(script);

            //KeyScript.BaseScript basescript = (KeyScript.BaseScript)loadedScript.asm.CreateInstance(
            //    "Test", false, System.Reflection.BindingFlags.Default, null, 
            //    new object[] { mScriptingHost }, null, null);

            //// method 1 test
            //CSScriptLibrary.MethodDelegate meth = loadedScript.asm.GetStaticMethod("Test.OnSpawn", "");

            //for (int i = 0; i < 100; i++)
            //    meth(i.ToString());

            //// method 2 test
            //CSScriptLibrary.MethodDelegate methOnUpdate = loadedScript.asm.GetStaticMethod("Test.OnUpdate", "", 0);
            //methOnUpdate("77", 100);

            //Keystone.Elements.ScriptNode scriptNode = Keystone.Elements.ScriptNode.Create(scriptPath, scriptPath);
        
        }

        //public FastInvokeDelegate FastDelegate
        //{
        //    get 
        //    {
        //       return mAssemblyHelper.GetMethodInvoker(" mCurrentAssembly
        //    }

        //}

        public AsmHelper Helper { get { return mAssemblyHelper; } }

        //public Assembly Assembly
        //{
        //    get
        //    {
                

        //        System.Diagnostics.Trace.WriteLine("Loading script " + mScriptFullPath);
        //        if (mCurrentAssembly == null)
        //        {
                    

        //            // TODO: trying to cache these assemblies using temp names is not going to work because we wont know
        //            // what the name used was and so when we try to check for existance when loading a script of a particular path
        //            // we wont know what temp name was used.
        //            System.Diagnostics.Trace.Assert(CSScript.GetCachedScriptAssembly(scriptCopy) != null);
        //        }
        //        return mCurrentAssembly;
        //    }
        //}

        public string GetTempFileInSharedAssemblyFolder(string sharedAssemblyDir)
        {
            string tempFile = Path.GetFileName ( CSScriptLibrary.CSScript.GetScriptTempFile());

            tempFile = Path.Combine(sharedAssemblyDir, tempFile);
            // TODO: guarantee this file doesnt already exist or permissions are locked
            return tempFile;
        }

    }
}