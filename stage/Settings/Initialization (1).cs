using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Settings
{
    /// <summary>
    /// This is an XML version of a configuration file using familiar INI file
    /// terminology such as "sections" and "keys"
    /// </summary>
    /// <remarks>This class currently is dependant on our Serializer class</remarks>
    [XmlRoot(ElementName="configuration", IsNullable=false), Serializable()]
    public class Initialization
    {
        [XmlIgnore()] private List<IniSection> _sections;
        [XmlIgnore()] private string _fullpath;
        private const string COLUMN_KEY = "key";
        private const string COLUMN_VALUE = "value";
        private const string COLUMN_ID = "id";


        public Initialization()
        {
            _sections = new List<IniSection>();
        }

        public void Save ()
        {
            Initialization.Save(_fullpath, this);
        }

        #region "Shared Functions and Subs"
        /// <summary>
        /// Loads the configuration using .net deserialization
        /// </summary>
        /// <param name="FileName">Full filepath + name</param>
        /// <returns></returns>
        /// <remarks>This function is dependant on sandbox.Serializer class.</remarks>
        public static Initialization Load(string fullpath)
        {
            Initialization Config = new Initialization();

            if (!System.IO.File.Exists (fullpath))
            	return null;
            
            // we re-assign to config because within Deserialize, we only use the object passed in to get the type.
            // after that, a new instance is deserialized.
            Config = (Initialization)Serializer.Deserialize(fullpath, Config);
            Config._fullpath = fullpath;
            return Config;
        }

        /// <summary>
        /// Saves the configuration using .net serialization
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="Config"></param>
        /// <returns></returns>
        /// <remarks>This function is dependant on sandbox.Serializer class.</remarks>
        public static bool Save(string filename, Initialization Config)
        {
            // delete the existing file otherwise Serializer.Serialize will File.OpenWrite which does not truncate the existing file
            // so if the previous file was longer, that data will remain at the end.
            if (File.Exists(filename))
                File.Delete(filename);

            FileStream fs = File.Create(filename);
            fs.Close();
            Serializer.Serialize(filename, Config, true);
            Config._fullpath = filename;
            return true;
        }

        //TODO: This function needs to be tested vigourously in different scenarios with different users
        // logged in as well as on OS versions like Win95 and 98 and Me which do not support required user logins.
        // NOTE: in theory, we "should" have permissions to create & modify settings from this login session
        // into the current profiles special folders... this all we need to verify on the 2k/xp side.
        // NOTE: The installer should ideally create this folder as well as the config file so that
        // it can be deleted by uninstall.  However, in the event it gets accidentally deleted, we could just recreate it
        // here.
        /// <summary>
        /// Checks if (your App Data Path exists, if (not create it
        /// </summary>
        /// <param name="CompanyName"></param>
        /// <returns></returns>
        /// <remarks>CompanyName must not end with \ or contain illegal folder path characters</remarks>
        public static string CreateApplicationDataFolder(string CompanyName)
        {
            if (! IsValidFileName(CompanyName)) throw new ArgumentOutOfRangeException();
            if (CompanyName.Substring(CompanyName.Length) == "\\") throw new ArgumentException();

            FileInfo SpecialPath =
                new FileInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyName) +
                    "\\");
            try
            {
                if (! SpecialPath.Directory.Exists)
                    SpecialPath.Directory.Create();

                return SpecialPath.ToString();
            }
            catch
            {
                return null;
            }
        }

        
        /// <summary>
        /// Creates a configuration file in the user's application data folder.  if (the folder does not exist, it creates it.
        /// </summary>
        /// <param name="CompanyName"></param>
        /// <param name="FileName"></param>
        /// <param name="OverWriteExisting"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string CreateApplicationConfigFile(string CompanyName, string FileName, bool OverWriteExisting)
        {
            string FilePath;

            // user passes in CompanyName and FileName 
            // NOTE: FileName is just a file + extentiion and not the full path
            FileInfo SpecialPath;
            try
            {
                //if (!IsValidFileName(FileName)) throw new Exception("Invalid Filename");

                FilePath = CreateApplicationDataFolder(CompanyName);

                //if (FilePath.Length <= 0) throw new Exception("Invalid Filename");

                FilePath = Path.Combine(FilePath, FileName);
                SpecialPath = new FileInfo(FilePath);

                if (!SpecialPath.Exists)
                    SpecialPath.Create();
                else if (OverWriteExisting)
                {
                    SpecialPath.Delete();
                    SpecialPath.Create();
                }
                return FilePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Initialization.CreateAppliationConfigFile() -- " + ex.Message);
                throw ex;
            }
        }

        public static string GetConfigFilePath(string configPath, string bindsFileName, string defaultResourceContents)
        {
            string fullPath = System.IO.Path.Combine(configPath, bindsFileName);


            System.IO.FileInfo info = new System.IO.FileInfo(fullPath);
            if (!info.Exists || info.Length == 0)
            {
                //MessageBox.Show("Configuration file not found.  Using default.", "FYI", MessageBoxButtons.OK);
                System.IO.Directory.CreateDirectory(info.DirectoryName);
                info = null;
                System.IO.File.WriteAllText(fullPath, defaultResourceContents);

                // requery info
                info = new System.IO.FileInfo(fullPath);
                System.Diagnostics.Debug.Assert(info.Length > 0);

                info = null;
            }

            return fullPath;
        }


        //public Shared Function AppPath() As String
        //    Return System.IO.Path.get {DirectoryName(System.Windows.Forms.Application.ExecutablePath)
        //}

        public static bool IsValidFileName(string FileName)
        {
            // NOTE: This is for testing filename only, not a full path
            // 'GetInvalidPathChars' should be used instead for the path portion
            // because it will allow \\c#\\  whereas GetInvalidFileNameChars()
            // will not.
            // http://blogs.msdn.com/b/bclteam/archive/2005/01/11/351060.aspx
            char[] charsInvalid = Path.GetInvalidFileNameChars();
            
            if (FileName.IndexOfAny (charsInvalid) >= 0) return false;
            return true;
        }

        public static bool ValidFolder(string folderpath)
        {
            // note: Dont use GetInvalidFileNameChars() because case in point, it'll report ..\\c#\projects  inalid with # symbol.
            if (folderpath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return false;
            }


            if (Directory.Exists(folderpath))
            {
                return false;
            }
            return true;
        }
        #endregion


        #region "public properties"
        // note: to change path you must open a new config
        public string FilePath { get { return _fullpath; } }

        /// <summary>
        /// Returns ALL sections contained within the configuration file
        /// </summary>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        [XmlElement(Type = typeof(IniSection), ElementName = "section", IsNullable = false, Form = XmlSchemaForm.Qualified)]
        public List<IniSection> sections
        {
            get { return _sections; }
            set { _sections = value; }
        }

        /// <summary>
        /// Returns or adds the section with/of a particular name
        /// </summary>
        /// <param name="name"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks>Using a dictionary and storing sections using their name as a key in the dictionary
        /// would elminate having to loop through the Generic.List and check the name property. However
        /// list types seem to serialize more reliably than other .net collection types.</remarks>
        public IniSection? GetSection(string name)
        {
            foreach (IniSection element in _sections)
            {
                if (element.name == name)
                    return element;
            }
            return null;
        }

        public bool ContainsSection(string name)
        {
            foreach (IniSection element in _sections)
            {
                if (element.name == name)
                    return true;
            }
            return false;
        }

        public void SetSection(string name, IniSection value)
        {
            bool Found = false;
            foreach (IniSection element in _sections)
            {
                if (element.name == value.name)
                    Found = true;
            }
            if (!Found) _sections.Add(value);
        }

        /// <summary>
        /// returns the number of sections that share the same first part of the name passed in.
        /// So Sections window0 and window1 with "window" passed in returns 2.
        /// </summary>
        /// <param name="name"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public int sectionCount(string name)
        {
            int Count = 0;
            foreach (IniSection section in _sections)
            {
                //TODO: test with "" for name and names that are longer than section names
                if (section.name.Length < name.Length) continue;
                if (section.name.Substring(0, name.Length) == name)
                    Count += 1;
            }
            return Count;
        }

        /// <summary>
        /// returns or sets all the key/value pairs listed under a given section
        /// </summary>
        /// <param name="sectionName"></param>
        /// <value></value>
        /// <returns></returns>
        /// <remarks></remarks>
        public List<KeyValuePair> GetKeys(string sectionName)
        {
            foreach (IniSection element in _sections)
            {
                if (element.name == sectionName)
                    return element.keys;
            }
            return null;
        }

        public void SetKeys(string sectionName, List<KeyValuePair> value)
        {
            IniSection foundElement = new IniSection();
            bool found = false;
            int i = 0;
            foreach (IniSection element in _sections)
            {
                if (element.name == sectionName)
                {
                    found = true;
                    foundElement = element;
                    break;
                }
                i++;
            }
            if (found) _sections[i] = foundElement;

            throw new ArgumentOutOfRangeException("Invalid section name.");
        }
        #endregion

        #region "Private Functions and subs"

        //private static bool FindByKey(Of section)(ByRef sec As AppSettingsComponent.IniSection)
        //    if (name = sec.name ) return true;
        //}

        private void addSection(string sectionName)
        {
            if (sectionName.Length > 0)
                _sections.Add(new IniSection(sectionName));
        }

        #endregion

        #region " public Functions and Subs "

        /// <summary>
        /// Usually called after all sections have been retreived and then called 
        /// for each section name. 
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        /// <remarks>The full exact section name must be specified.</remarks>
        public string settingRead(string sectionName, string keyName)
        {
            foreach (IniSection section in sections)
            {
                if ((sectionName == section.name))
                {
                    foreach (KeyValuePair kvp in section.keys)
                    {
                        if ((keyName == kvp.Name))
                        {
                            return kvp.Value;
                        }
                    }
                }
            }
            return "";
            // TODO: could add "TryReadString()
            // TODO: this is horrible.  Returning a default of ""!  No way to know if the read succeeded or failed
        }

        public int settingReadInteger(string sectionName, string keyName)
        {
            foreach (IniSection section in sections)
            {
                if ((sectionName == section.name))
                {
                    foreach (KeyValuePair kvp in section.keys)
                    {
                        if ((keyName == kvp.Name))
                        {
                            return int.Parse(kvp.Value);
                        }
                    }
                }
            }

            return 0;
            // TODO: could add "TryReadInt()
            // TODO: this is horrible.  Returning a default of 0!  No way to know if the read succeeded or failed
        }

        public bool settingReadBool(string sectionName, string keyName)
        {
            foreach (IniSection section in sections)
            {
                if ((sectionName == section.name))
                {
                    foreach (KeyValuePair kvp in section.keys)
                    {
                        if ((keyName == kvp.Name))
                        {
                            return bool.Parse(kvp.Value);
                        }
                    }
                }
            }
            return false;
            // TODO: could add "TryReadBool()
            // TODO: this is horrible.  Returning a default of false!  No way to know if the read succeeded or failed
        }

        // <summary>
        //  
        // </summary>
        // <param name="sectionName"></param>
        // <param name="keyName"></param>
        // <param name="value"></param>
        // <remarks>If the section already exists in the section /key collections then it gets updated.  
        // Otherwise add the new section and/or new key</remarks>
        public void settingWrite(string sectionName, string keyName, string value)
        {
            settingWrite(sectionName, new KeyValuePair(keyName, value));
        }

        public void settingWrite(string sectionName, KeyValuePair KVP)
        {
            bool bFound = false;
            for (int i = 0; i <= _sections.Count - 1; i++)
            {
                if ((sectionName == _sections[i].name))
                {
                    bFound = true;
                    if (_sections[i].keys != null)
                    {
                        for (int j = 0; j <= _sections[i].keys.Count - 1; j++)
                        {
                            //  update an existing key
                            if ((KVP.Name == _sections[i].keys[j].Name))
                            {
                                _sections[i].keys[j] = KVP;
                                return;
                            }
                        }
                    }
                    else
                    {
                        // TODO: IMPORTANT:  all this is lame and hackish but its because of issues with
                        //  reference and value types since IniSection is a structure ( mistake)
                        //  but i dont want to frak with it (later) because it borks serialization changing it to class.
                        IniSection sec = new IniSection(sectionName);
                        sec.keys = new List<KeyValuePair>();
                        _sections[i] = sec;
                    }
                    //  section exists but the key doesnt, add it
                    if (bFound)
                    {
                        _sections[i].keys.Add(KVP);
                        return;
                    }
                }
            }
            //  no section exists so we add a new section and its key value pair item
            IniSection section = new IniSection(sectionName);
            section.keys = new List<KeyValuePair>();
            section.keys.Add(KVP);
            _sections.Add(section);
        }

        public void settingDelete(string sectionName, string key)
        {
            //  find it and delete just the key.  TODO: What if there are no more keys in that section?  Keep the section or delete that too?
            foreach (IniSection section in _sections)
            {
                if ((section.name == sectionName))
                {
                    _sections.Remove(section);
                    break;
                }
            }
        }

        public void sectionAdd(string sectionName)
        {
            foreach (IniSection section in _sections)
            {
                if ((section.name == sectionName))
                {
                    return;
                }
            }
            //  havent existed so the section doesnt already exist and we need to add it
            _sections.Add(new IniSection(sectionName));
        }

        public void sectionDelete(string sectionName)
        {
            foreach (IniSection section in _sections)
            {
                if ((section.name == sectionName))
                {
                    _sections.Remove(section);
                    break;
                }
            }
        }

        #endregion
    }
}