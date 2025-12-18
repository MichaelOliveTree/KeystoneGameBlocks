  using System;
  using System.IO;
  using System.Collections.Generic;
  using System.Xml.Schema;
  using System.Xml.Serialization;
//- user-defined type in plugin (convert to and from string during deserislization - this should be more robust - add asserts for if no match in case we change enum names in the future .   Not sure why we didn't enforce this already except maybe that we don't want keystone.dll to reference game01.dll.  I don't think this is a problem because whether it's an int or string, keystone.dll won't know what it refers to... oh wait, it won't be able to convert the saved string to an enum value as a result... the main cost is memory used by arbitrary string vs 4 bytes for int.
//  our scripts do want to find component types based on enum name eg Component.HULL and not "Hull"
//
//  TODO: this means if we serialize an int, then edit the config, we have same problem. so we have to store the string from int on  write and the reverse on read.
//
//   - we need an agnostic way to convert the string value to an int during read, int to string during write, without a direct call or reference to game01.dll
//
//- a shared dll or configuration file.
//  with a class wrapper for accessing like enums
//
//I think rather than use a shared assembly, it's better to just load separate instances of the class for 
// keystone, the plugin, and game01.dll
namespace KeyCommon
{
    ///<summary>
    /// SharedUserTypes 
    ///</summary>
    [XmlRoot(ElementName="SharedUserTypes", IsNullable=false), Serializable()]
    public class SharedUserTypes
    {
        
        [XmlIgnore()]
        private string mFullPath; //data//mods//config//user_types.config
        [XmlIgnore()]
        private Dictionary<string, uint> mUserTypes;
    
        [XmlElement(Type= typeof(KeyValuePair<string, uint>), ElementName="key", IsNullable=false, Form=XmlSchemaForm.Qualified)]
        private  List<KeyValuePair<string, uint>> KVPs; // this is maintained only for Serialization using simple .net serialiation.
    
    
    
        public SharedUserTypes(string fullPath)
        {
            mFullPath = fullPath;
            KVPs = new List<KeyValuePair<string, uint>>();
        }

        public void AddUserType(string name, uint ID, bool overwriteExisting = false)
        {
            if (mUserTypes == null) mUserTypes = new Dictionary<string, uint>();
            
            uint dummy;
            if (UserTypeExists(name, out dummy))
                if (!overwriteExisting)
                {
                    mUserTypes[name] = ID;
                    return;
                }

            if (!mUserTypes.TryGetValue(name, out dummy))       
              mUserTypes.Add (name, ID);
            else 
              throw new System.Exception ("SharedUserTypes.AddUserType() - UserType '" + name + "' already exists.");
        }
        
        public void RemoveUserType (string name)
        {
            if (mUserTypes == null) return;
            
            if (mUserTypes.ContainsKey(name))
                mUserTypes.Remove(name);
        }
        
        public void RenameType (string previousName, string newName, uint ID)
        {
            
        
        }
        
        public uint GetTypeByName (string name)
        {
            if (mUserTypes == null) throw new System.Exception ("SharedUserTypes.GetUserTypeByName() - There are no UserTypes.");
            
            uint result;
            bool success = mUserTypes.TryGetValue (name, out result);
            if (success) return result;
            
            throw new System.Exception ("SharedUserTypes.GetUserTypeByName() - UserType with Name == '" + name + "' not found.");
        }
        
        public string GetTypeByID (uint ID)
        {
            if (mUserTypes == null) return null;
            
            int index = 0;
            foreach (KeyValuePair<string, uint> kvp in mUserTypes)
            {
                if (kvp.Value == ID)
                {
                    return kvp.Key;
                }
                index++;
            }       
            throw new Exception ("SharedUserTypes.GetUserTypeByName() - UserType with ID == '" + ID + "' not found.");
        }
        
        private bool UserTypeExists (string name, out uint ID)
        {
            uint result = 0;
            bool success = mUserTypes.TryGetValue (name, out result);
            if (success) 
                ID = result;
            
            ID = 0;
            return success;
        }
        
        private bool UserTypeExists (uint ID, out string name)
        {
            throw new NotImplementedException ();
            
            //name = null;
            //if (mUserTypes == null) throw new Exception("SharedUserTypes.UserTypeExists() - UserTypes is empty.");
            
            //if (mUserTypes.TryGetValue(name, out name))
            // return true;
              
            //return false;
            
        }
        
        
        
        // todo: maybe this inherits Settings.INI and uses that XML loader
        //       OR just uses the Settings.INI to load and build our Enums
        public void Read ()
        {
            if (mUserTypes != null) mUserTypes.Clear();
            
            // each line contains a key value pair
            
            
            //System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(assemblyNameToUse + ".dll");
            //System.Type enumTest = assembly.GetType(enumName);
            //string[] values = enumTest.GetEnumNames();
    
            //foreach( object o in Enum.GetValues(finished) )
            //{
            //    Console.WriteLine("{0}.{1} = {2}", finished, o, ((int) o));
            //}
            
            /* This code example produces the following output:
    
                CustomUserTypes.HULL = 0
                CustomUserTypes.HULL_ENGINE_POD = 1
            */
            
        }
        
        public void Write ()
        {
            
        }
        
        /// <summary>
        /// Loads the SharedUserTypes from a file containing a list of INI style key value pairs using .net deserialization
        /// </summary>
        /// <param name="FileName">Full filepath + name</param>
        /// <returns></returns>
        public static SharedUserTypes Load(string fullPath)
        {
            if (!System.IO.File.Exists (fullPath))
            	return null;
            	
            SharedUserTypes sharedUT = new SharedUserTypes(fullPath);
    
            
            
            // we re-assign to sharedUT because within Deserialize, we only use the object passed in to get the type.
            // after that, a new instance is deserialized.
            sharedUT = (SharedUserTypes)Serializer.Deserialize(fullPath, sharedUT);
            sharedUT.mFullPath = fullPath;
            return sharedUT;
        }
    
        /// <summary>
        /// Saves the keyvaluepairs using .net serialization
        /// </summary>
        /// <returns></returns>
        /// <remarks>This function is dependant on sandbox.Serializer class.</remarks>
        public void Save ()
        {
            // delete the existing file otherwise Serializer.Serialize will File.OpenWrite which does not truncate the existing file
            // so if the previous file was longer, that data will remain at the end.
            if (File.Exists(mFullPath))
                File.Delete(mFullPath);
    
            FileStream fs = File.Create(mFullPath);
            fs.Close();
            Serializer.Serialize(mFullPath, this, true);
            
            System.Diagnostics.Debug.WriteLine ("SharedUserAssembly.Save() - Shared User Types saved to '" + mFullPath + "'");
        }
    }
}