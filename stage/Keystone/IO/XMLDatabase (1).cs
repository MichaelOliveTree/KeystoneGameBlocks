using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using ComponentAce.Compression.Archiver;
using ComponentAce.Compression.ZipForge;
using Keystone.Appearance;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Lights;
using Keystone.Octree;
using Keystone.Portals;
using Keystone.Scene;
using Keystone.Resource;
using Keystone.DomainObjects;
using Keystone.FX;

namespace Keystone.IO
{
    /// <summary>
    /// Prefab XMLDB and Scene XMLDB use STORE format (i.e. NO COMPRESSION)
    /// </summary>
	public class XMLDatabaseCompressed : XMLDatabase
	{
        private SceneArchiver mArchiver;
            
		public override SceneInfo Create(SceneType type, string filename, string firstNodeTypeName)
		{		
			mFolderName = filename;
            mArchiver = new SceneArchiver();
            // open call will create the archive zip if it doesn't exist
            mArchiver.Open(mFolderName, false, FileMode.Create, FileAccess.Write);
            mArchiver.Comment = "This Keystone Game Blocks database was created at " + System.DateTime.Now.ToString("G");
	

            if (System.IO.File.Exists(filename)  == false) throw new Exception("XMLDatabase.Create() - ERROR: Cannot create XMLDB.  File does not exist.");

            mFolderName = filename;  
            

            Trace.WriteLine("XMLDatabase.Create() - SUCCESS: DB '" + mFolderName + "' created.");
                        
            SceneInfo info = new SceneInfo(mFolderName, new string[] { firstNodeTypeName });
            info.Type = type;

            // nodes are added to the Repository when they are constructed but only ref count is incremented
            // if it's manually done or added to the scene.  Here we do it manually so that on DecrementRef it
            // will automatically be removed from Repository

            Repository.IncrementRef(info);
            Repository.DecrementRef(info);// make sure the temporary Info file we create gets removed from the Repository

            mWriter.WriteSychronous(info, true, false); // dont save the entire xmldb yet since so far we're just saving the SceneInfo
            return info;
		}
		
        /// <summary>
        /// Opens an XMLDB from a stream.  This is ALWAYS used when
        /// we are opening an XMLDB that is embedded as a compressed entry within
        /// another XMLDB.  eg. an entity prefab .KGBEntity.  
        /// So here the stream is the compressed byte stream of a prefab which is itself an XMLDB
        /// and indeed, the XMLDB we are opening here.
        /// </summary>
        /// <param name="stream">Stream must remain open for the life of the XMLDatabase.  
        /// Cannot stream.Close() or stream.Dispose() until we're ready to close the XMLDatabase.</param>
        /// <returns></returns>
        public SceneInfo Open(Stream stream)
        {
            mArchiver = new SceneArchiver();
            mArchiver.Open (stream);
            Info = GetInfo();
        	
            //Debug.WriteLine("XMLDatabaseCompressed.Open() - SUCCESS: DB '" + Info.Name + "' Opened from stream.");
            return Info;
        }

        public SceneInfo Open(KeyCommon.IO.ResourceDescriptor descriptor, out System.IO.Stream stream)
        {
            
            SceneInfo info;

            if (descriptor.IsArchivedResource)
            {
                string archiveFullPath = Keystone.Core.FullNodePath(descriptor.ModName);
                stream = KeyCommon.IO.ArchiveIOHelper.GetStreamFromMod(descriptor.EntryName, "", archiveFullPath);
                info = Open(stream);
            }
            else
            {
                info = Open(Keystone.Core.FullNodePath(descriptor.EntryName), true);
                stream = null;
            }
            
            return info;
        }


        /// <summary>
        /// Append should usually be true or else the file gets overwritten and the existing file data is lost.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="append"></param>
        /// <returns></returns>
		public override SceneInfo Open(string filename, bool append = true)
		{
            // this extension should be .KGBSegment, .KGBEntity or .KGBScene for uncompressed scenes
            // or .KGBCScene for compressed scenes
            bool exists = File.Exists (filename );            
            if (exists == false) throw new Exception ("XMLDatabaseCompressed.Open() - ERROR: Database '" + filename + "' not found.");
            
            mFolderName = filename;
            mArchiver = new SceneArchiver();
            mArchiver.Open(filename, append, FileMode.Open, FileAccess.Read);

            //string ext = Path.GetExtension (filename );
            Info = GetInfo();
            //Debug.WriteLine("XMLDatabaseCompressed.Open() - SUCCESS: DB '" + filename + "' opened.");
            return Info;
		}		
		
		public override void SaveAllChanges()
		{
			base.SaveAllChanges();
		
            if (mArchiver != null)
            {
                lock (mArchiver)
                {
                    mArchiver.Save(this);
                }
            }
		}
		
		internal override XmlDocument GetDocument(string typename, bool setDocumentChangeFlag)
		{
            try
            {
                lock (sycnObject)
                {
                    // if the filemap exists, then the XmlDocument must exist too
                    string tablename = GetTableName(typename);

                    if (FileMaps.ContainsKey(tablename))
                    {
                        FileMap map = FileMaps[tablename];
                        if (setDocumentChangeFlag)
                            map.Changed = true;

                        return map.Document;
                    }
                    else
                    {
                        // IMPORTANT: Note that for the key we are using the tablename based off the typename
                        // because this will always guarantee that if two or m ore different types are sharing the same
                        // file, that we always share the same Document and not use seperate ones that then end up
                        // overwriting each other when added to the archive.
                        // This is Compressed XMLDB so there is no mModPath //System.IO.Path.Combine (mModPath, tablename);
                        string fullPath = GetTempFileName();

                        FileMap map = new FileMap(tablename, fullPath);
                        // if this is a table type that doesnt currently exist in the archive, create a new one
                        Stream stream = null; 
                        // TODO: i think the db itself should maintain the list once it's opened from the archive.  XMLDB does not
                        // need to retain the archive reference i dont think..?  well.. hm.. maybe it should.  Perhaps better than otherwise
                        // having to load the archive each time we wish to change it.
                        if (!mArchiver.FindEntry(tablename, ref stream))
                        {
                            map.Document = XmlHelper.CreateXmlDocument("root");
                        }
                        else
                        {
                           // Debug.Write("XMLDatabase.GetDocument " + tablename + "...");
                            // else we extract the existing  
                            // NOTE: We should not store empty files into the archive.  They must be valid xml
                            map.Document = XmlHelper.OpenXmlDocument(stream);
                            stream.Close();
                            stream.Dispose();
                            //Debug.WriteLine("XMLDattabase.GetDocument() - completed.");
                        }
                    
                        FileMaps.Add(tablename, map);

                        if (setDocumentChangeFlag)
                        {
                            map.Changed = true;
                        }
                        return map.Document;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("XMLDatabaseCompressed.GetDocument() - ERROR getting document '{0}' from archive.  {1}", typename, ex.Message));
                return null;
            }
		} 
	
		
        protected override void DisposeManagedResources()
        {
        	base.DisposeManagedResources ();
        	if (mArchiver != null)
            {
                // normally disposes (which closes) when it is GC'd.
                mArchiver.Dispose();
                mArchiver = null;
            }
        }

        // override for XMLDatabaseCompressed
        protected override void DisposeUnmanagedResources()
        {
	         if (FileMaps != null && FileMaps.Count > 0)
	            foreach (FileMap map in FileMaps.Values)
	            {
	                File.Delete(map.FilePath);
	            }
        }
	}
	
    /// <summary>
    /// Used to handle the xml files that make up a scene or prefab.
    /// The XMLDatabase can work with files in an archive or files that are already extracted.
    /// </summary>
    public class XMLDatabase : IDisposable 
    {
        public class FileMap
        {
            public string FilePath;
            public string StoredName;
            public bool Changed;
            public XmlDocument Document;

            public FileMap(string storedName, string fullPath)
            {
                StoredName = storedName;
                FilePath = fullPath;
                Document = null;
                Changed = true;
            }
        }

        protected string mFolderName;
        private string mModPath;
        public SceneInfo Info;
        protected SceneWriter mWriter;
        protected SceneReader mReader;
        
        private Dictionary<string, string> ClassMappingTables;
        public Dictionary<string, FileMap> FileMaps;
        protected object sycnObject = new object();


        public XMLDatabase()
        {
            ClassMappingTables = GetDefaultClassMappings();
            FileMaps = new Dictionary<string, FileMap>();

            // TODO: only one write at a time is allowed, but we could do multiple reads
            // if we wanted however, only if no writes are occuring.  So reader & writer
            // can use seperate groups with writer concurrency of 1 and reader concurrency > 1
            // but if a write is being performed, we must lock access
            // http://msdn.microsoft.com/en-us/library/system.threading.readerwriterlock(v=vs.90).aspx
            // solution link above for readerwriter locks
            mReader = new SceneReader(this, Core._Core.ThreadPool);
            mWriter = new SceneWriter(this, Core._Core.ThreadPool, null, null);
        }


        /// <summary>
        /// Creates a new XMLDB on disk.   This is usually called when creating
        /// prefab temporarily on disk prior to adding it to mod archive.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="folderName">The folder name underneath the "Data\\Scenes" sub directory</param>
        /// <param name="firstNodeTypeName"></param>
        /// <returns></returns>
        public virtual SceneInfo Create(SceneType type, string folderName, string firstNodeTypeName)
        {
            // we want to create a new XMLDB.  verify a file of same name does not already exist
            string fullPath = System.IO.Path.Combine(Core._Core.ScenesPath, folderName);
            if (System.IO.Directory.Exists(fullPath)  == false) throw new Exception("XMLDatabase.Create() - ERROR: Cannot create XMLDB.  Folder does not exist.");

            mFolderName = fullPath;  // todo: this is actually a fullpath and we no longer use mModPath
            mModPath = mFolderName; 

            Debug.WriteLine("XMLDatabase.Create() - SUCCESS: DB '" + mFolderName + "' created.");
                        
            SceneInfo info = new SceneInfo(folderName + "_SceneInfo", new string[] { firstNodeTypeName });
            info.Type = type;

            // nodes are added to the Repository when they are constructed but only ref count is incremented
            // if it's manually done or added to the scene.  Here we do it manually so that on DecrementRef it
            // will automatically be removed from Repository

            Repository.IncrementRef(info);
            Repository.DecrementRef(info);// make sure the temporary Info file we create gets removed from the Repository

            mWriter.WriteSychronous(info, true, false); // dont save the entire xmldb yet since so far we're just saving the SceneInfo
            return info;
        }

        public virtual FileStream Open(string fullFilePath)
        {
            // NOTE: here all we want to do is just convert the xmldatabase to a stream.  
            //       we don't need to load the scene at all.  just return the filesstream.
            //       This is (for now) easier than reading the entire xmldb into a MemoryStream.
            if (!File.Exists(fullFilePath)) throw new FileNotFoundException("XMLDatabase.Open() - " + fullFilePath + " not found.");
            FileStream fs = new FileStream(fullFilePath, FileMode.Open);
            return fs;
        }

        /// <summary>
        /// Opens an XMLDB without paging in any Regions or Nodes EXCEPT the SceneInfo.
        /// </summary>
        /// <param name="fullFolderPath"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public virtual SceneInfo Open(string fullFolderPath, bool append)
        {
            // this extension should be .KGBEntity or .KGBScene for uncompressed scenes
            // or .KGBCScene for compressed scenes
        	if (Directory.Exists (fullFolderPath) == false) 
        		throw new Exception ("XMLDatabase.Open() - ERROR: Database '" + fullFolderPath + "' not found.");

        	mModPath = fullFolderPath;

            Info = GetInfo();
            Debug.WriteLine("XMLDatabase.Open() - SUCCESS: DB '" + mFolderName + "' opened.");
            return Info;
        }

        protected SceneInfo GetInfo()
        {
            // TODO: info.ID is being set equal to the prefab .kgbentity full path.  It should have a unique GUID instead right?
            //       Or why isn't "generateIDs" == true?
            SceneInfo info = (SceneInfo)ReadSynchronous(typeof(SceneInfo).Name, "", true, false, null, false, false);
            
            if (info != null)
            {
	            Repository.IncrementRef(info);
    	        Repository.DecrementRef(info); // ensure the temporary SceneInfo that was created gets removed from repository  
            }
   	        return info;
        }

        private string GetTempFile(string typename)
        {
            return FileMaps[GetTableName(typename)].FilePath;
        }

        /// <summary>
        /// Returns a temp filename but DOES NOT create the file.
        /// </summary>
        /// <returns></returns>
    	public static string GetTempFileName()
        {
            try
            {
                // NOTE: calls to System.IO.Path.GetTempFileName() is EXTREMELY slow.
                //string tmp = System.IO.Path.GetTempFileName();
                // File.Delete (tmp);

                string tmp = System.IO.Path.GetTempPath();
                string guid = System.Guid.NewGuid().ToString();
                tmp = System.IO.Path.Combine(tmp, guid);
                return tmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XMLDatabase.GetTempFile() - ERROR: Failed to create temporary file");
                return "";
            }
        }
                
        // todo: here we need the ability to write to a stream and read from a stream.  Perhaps we Create() the xmldatabase for a node to a temporary file and use the filestream
        public void WriteSychronous(Node node, bool recursivelyWriteChildren, bool incrementDecrement, bool saveOnWrite)
        {
            mWriter.WriteSychronous(node, recursivelyWriteChildren, saveOnWrite);

            // nodes are added to the Repository when they are constructed but only ref count is incremented
            // if it's manually done or added to the scene.  Here we do it manually so that on DecrementRef it
            // will automatically be removed from Repository
            if (incrementDecrement)
            {
                Repository.IncrementRef(node);
                Repository.DecrementRef(node);
            }
        }

        public void Write(Node node, bool recursivelyWriteChildren, bool saveOnWrite, Amib.Threading.PostExecuteWorkItemCallback writeCompletedHandler)
        {
            // TODO: based on Scene.Security settings, the sender of the write and their access priveledges, perform or do not perform write
            Keystone.IO.SceneWriter writer = new Keystone.IO.SceneWriter(this, Core._Core.ThreadPool, null, null);
            mWriter.Write(node, recursivelyWriteChildren, saveOnWrite, writeCompletedHandler);
        }

        /// <summary>
        /// Returns the specified node.  Usually used when loading prefab and we want to load the root Node specified by SceneInfo.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="name">can be null</param>
        /// <param name="recursivelyLoadChildren"></param>
        /// <param name="generateIDs"></param>
        /// <param name="nodeIDsToUse">The id's to use for entities we clone</param>
        /// <param name="delayTVResourceLoading"></param>
        /// <para name="dummy">param only exists to make overloaded method signature different</para>
        /// <returns></returns>
        public Node ReadSynchronous(string typeName, string name, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, bool delayTVResourceLoading = false, bool dummy = false)
        {
            // nodes including this temporary SceneInfo are added to the Repository when they are constructed but
            // ref count is still at 0 until it's added to the scene.  If we manually increment ref count to 1, then decrement it, it will be automatically
            // removed from the Repository. 
            return mReader.ReadSynchronous(typeName, name, null, recursivelyLoadChildren, generateIDs, nodeIDsToUse, delayTVResourceLoading).Node;
        }

        /// <summary>
        /// Returns all children (optionally recursive) under a specified parent node but does NOT return the specified parent.
        /// </summary>
        /// <param name="parentTypeName"></param>
        /// <param name="parentName"></param>
        /// <param name="recursivelyLoadChildren"></param>
        /// <param name="generateIDs"></param>
        /// /// <param name="nodeIDsToUse">The id's to use for entities we clone</param>
        /// <param name="delayTVResourceLoading"></param>
        /// <returns></returns>
        public Node[] ReadSynchronous(string parentTypeName, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, bool delayTVResourceLoading = false)
        {
            // nodes including this temporary SceneInfo are added to the Repository when they are constructed but
            // ref count is still at 0 until it's added to the scene.  If we manually increment ref count to 1, then decrement it, it will be automatically
            // removed from the Repository. 
            return mReader.ReadSynchronous(parentTypeName, parentName, recursivelyLoadChildren, generateIDs, nodeIDsToUse, delayTVResourceLoading).Nodes;
        }

        public void Read(string typename, string name, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, Amib.Threading.PostExecuteWorkItemCallback readCompletedHandler)
        {
            // TODO: check for nulls and invalid files, wrong versions and such
            Keystone.IO.SceneReader reader = new Keystone.IO.SceneReader(this, Core._Core.ThreadPool);
            mReader.Read(typename, name, parentName, recursivelyLoadChildren, generateIDs, nodeIDsToUse, readCompletedHandler);
        }

        // Returns all children (optionally recursive) under a specified parent node but does NOT return the specified parent.
        public void Read(string parentTypeName, string parentName, bool recursivelyLoadChildren, bool generateIDs, string[] nodeIDsToUse, Amib.Threading.PostExecuteWorkItemCallback readCompletedHandler)
        {
            // TODO: check for nulls and invalid files, wrong versions and such
            Keystone.IO.SceneReader reader = new Keystone.IO.SceneReader(this, Core._Core.ThreadPool);
            mReader.Read(parentTypeName, parentName, recursivelyLoadChildren, generateIDs, nodeIDsToUse, readCompletedHandler);
        }
        


        /// <summary>
        /// Saves changed XMLDocuments to the temporary files.
        /// </summary>
        public virtual void SaveAllChanges()
        {
            foreach (FileMap map in FileMaps.Values)
            {
                lock (map)
                {
                    if (map.Changed == true)
                    {
                        map.Document.Save(map.FilePath);
                    //    Debug.WriteLine("XMLDatabase.SaveAllChanges() - Saving '" + map.StoredName + "' '" + map.FilePath + "'");
                        map.Changed = false;
                        //using (StreamWriter sw = new StreamWriter(GetTempFile(typename)))
                        //{
                        //    GetDocument(typename).Save(sw);
                        //    sw.Close();
                        //}
                    }
                }
            }
        }


        /// <summary>
        /// GetDocument results in that document's changed flag being set to true.  This works
        /// fine for Writes because the only reason to get a document is to change it, however
        /// for Reads this results in all documents flags being inappropriately set
        /// </summary>
        /// <param name="typename"></param>
        /// <returns></returns>
        internal virtual XmlDocument GetDocument(string typename, bool setDocumentChangeFlag)
        {
            int lineNum = 0;
            try
            {
                lock (sycnObject)
                {
                    // if the filemap exists, then the XmlDocument must exist too
                    string tablename = GetTableName(typename);
                    lineNum = 1;
                    if (FileMaps.ContainsKey(tablename))
                    {
                        FileMap map = FileMaps[tablename];
                        if (setDocumentChangeFlag)
                            map.Changed = true;

                        return map.Document;
                    }
                    else
                    {
                        lineNum = 2;
                        // IMPORTANT: Note that for the key we are using the tablename based off the typename
                        // because this will always guarantee that if two or m ore different types are sharing the same
                        // file, that we always share the same Document and not use seperate ones that then end up
                        // overwriting each other when added to the archive.
                        string fullPath = System.IO.Path.Combine (mModPath, tablename);
                        FileMap map = new FileMap(tablename, fullPath);
                        lineNum = 3;

                    	// if the xml doesn't exist on disk, we create it
                    	if (System.IO.File.Exists (map.FilePath))
                    	{
                    		map.Document = XmlHelper.OpenXmlDocument (map.FilePath);
                    	}
                    	else 
                    	{
                    		map.Document = XmlHelper.CreateXmlDocument("root");
                    		
                    	}
                    	

                        FileMaps.Add(tablename, map);
                        lineNum = 10;
                        if (setDocumentChangeFlag)
                        {
                            map.Changed = true;
                            lineNum = 11;
                        }
                        return map.Document;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("XMLDatabase.GetDocument() - ERROR: getting document '{0}' from folder.  {1}", typename, ex.Message));
                return null;
            }
        }

        // NOTE: If the type is not specified here, it will be written 
        // inline (so to speak) in a single xml under it's parent
        private Dictionary<string, string> GetDefaultClassMappings()
        {

            // TODO: need reserved keys.xml in our archive too ?
            Dictionary<string, string> tmp = new Dictionary<string, string>();
            tmp.Add(typeof(SceneInfo).Name, "SceneInfo.xml");
            
            // entities - root regions
            tmp.Add(typeof(Root).Name, "Root.xml");
            tmp.Add(typeof(ZoneRoot).Name, "Root.xml");

            // entities - vehicles
            tmp.Add(typeof(Vehicles.Vehicle).Name, "Vehicles.xml");

            // interior celled regions
            tmp.Add(typeof(Interior).Name, "Interiors.xml");

            // entities - regions
            tmp.Add(typeof(Region).Name, "Regions.xml");
            tmp.Add(typeof(Zone).Name, "Zones.xml");

            // entities - celestials
            tmp.Add(typeof(Celestial.StellarSystem).Name, "StellarSystems.xml");
            tmp.Add(typeof(Celestial.Star).Name, "Stars.xml");
            tmp.Add(typeof(Celestial.World).Name, "Worlds.xml");


            // entities
            //tmp.Add(typeof(Entity).Name, "Entities.xml"); // abstract type but in future maybe not
            tmp.Add(typeof(DefaultEntity).Name, "Entities.xml");
            tmp.Add(typeof(ModeledEntity).Name, "Entities.xml");
            tmp.Add(typeof(BonedEntity).Name, "Entities.xml");
            tmp.Add(typeof(Character).Name, "Entities.xml");
            tmp.Add(typeof(PlayerCharacter).Name, "Entities.xml");
            tmp.Add(typeof(Terrain).Name, "Terrains.xml");

            // entities - lights
            tmp.Add(typeof(Light).Name, "Lights.xml");
            tmp.Add(typeof(DirectionalLight).Name, "Lights.xml");
            tmp.Add(typeof(PointLight).Name, "Lights.xml");
            tmp.Add(typeof(SpotLight).Name, "Lights.xml");

            // things
            tmp.Add(typeof(DomainObjects.DomainObject).Name, "Things.xml");

            // physics 
            tmp.Add(typeof(Physics.RigidBody).Name, "Physics.xml");
            tmp.Add(typeof(Physics.Colliders.BoxCollider).Name, "Physics.xml");

            // elements
            tmp.Add(typeof(Model).Name, "Models.xml");
            tmp.Add(typeof(ModelSelector).Name, "Models.xml");
            tmp.Add(typeof(ModelSequence).Name, "Models.xml");
            tmp.Add(typeof(ModelSwitch).Name, "Models.xml");
            //tmp.Add(typeof (BonedModel).Name, "BonedModels.xml");
            //tmp.Add(typeof(LODModel).Name, "ComplexModels.xml");
            //tmp.Add(typeof(SimpleModel ).Name, "SimpleModels.xml");
            //tmp.Add(typeof(BonedModel).Name, "Models.xml");
            //tmp.Add(typeof(LODModel).Name, "Models.xml");
            //tmp.Add(typeof(SimpleModel).Name, "Models.xml");
            tmp.Add(typeof(Actor3d).Name, "Actors.xml");
            tmp.Add(typeof(Mesh3d).Name, "Meshes.xml");
            tmp.Add(typeof(Billboard).Name, "Billboards.xml");
            tmp.Add(typeof(Minimesh2).Name, "Minimeshes.xml"); 
        	tmp.Add(typeof(MinimeshGeometry).Name, "MinimeshGeometry.xml"); 
            tmp.Add(typeof(ParticleSystem).Name, "ParticleSystems.xml");
            tmp.Add(typeof(BillboardText).Name, "BillboardText.xml");
            tmp.Add(typeof(TexturedQuad2D).Name, "TexturedQuad2D.xml");

            //tmp.Add("LODSwitch", ) ; // similar to GroupAttribute that always exists under Appearance, these will always exist under of the Model's

            // behaviors -
            tmp.Add(typeof(Behavior.Behavior).Name, "Behaviors.xml");
            tmp.Add(typeof(Behavior.Composites.Composite).Name, "Behaviors.xml");
            tmp.Add(typeof(Behavior.Composites.Selector).Name, "Behaviors.xml");
            tmp.Add(typeof(Behavior.Composites.Sequence).Name, "Behaviors.xml");
            //tmp.Add(typeof(Behavior.Composites.Parallel).Name, "Behaviors.xml");
            tmp.Add(typeof(Behavior.Actions.Action).Name, "Behaviors.xml");
            tmp.Add(typeof(Behavior.Actions.Action_RandomPath).Name, "Behaviors.xml"); // todo: this should jst be a script not a hardcoded action
            tmp.Add(typeof(Behavior.Actions.Script).Name, "Behaviors.xml");



            // appearance
            //tmp.Add(typeof(DefaultAppearance).Name, "DefaultAppearances.xml");
            //tmp.Add(typeof (SplatAppearance ).Name, "SplatAppearances.xml");
            tmp.Add(typeof(DefaultAppearance).Name, "Appearances.xml");
            tmp.Add(typeof(SplatAppearance).Name, "Appearances.xml");
            tmp.Add(typeof(GroupAttribute).Name, "Appearances.xml");
            tmp.Add(typeof(Material).Name, "Materials.xml"); // TODO: Material is obsolete, so are Layers
            // July.14.2013 obsolete - merged ProceduralShader's code with GroupAttribute - tmp.Add(typeof(Shaders.ProceduralShader).Name, "Shaders.xml");
            tmp.Add(typeof(Shaders.Shader).Name, "Shaders.xml");

            // textures
            //tmp.Add(typeof(Diffuse).Name, "Diffuses.xml");
            //tmp.Add(typeof(NormalMap).Name, "NormalMaps.xml");
            //tmp.Add(typeof(CubeMap).Name, "CubeMaps.xml");
            //tmp.Add(typeof(Emissive).Name, "Emissives.xml");
            //tmp.Add(typeof(VolumeTexture ).Name, "VolumeTextures.xml");
            //tmp.Add(typeof(Specular).Name, "Speculars.xml");
            //tmp.Add(typeof(SplatAlpha ).Name, "SplatAlphas.xml");
            //tmp.Add(typeof(DUDVTexture ).Name, "DUDVTextures.xml");
            tmp.Add(typeof(Texture).Name, "Textures.xml");
            tmp.Add(typeof(Diffuse).Name, "Textures.xml");
            tmp.Add(typeof(NormalMap).Name, "Textures.xml");
            tmp.Add(typeof(CubeMap).Name, "Textures.xml");
            tmp.Add(typeof(Emissive).Name, "Textures.xml");
            tmp.Add(typeof(VolumeTexture).Name, "Textures.xml");
            tmp.Add(typeof(Specular).Name, "Textures.xml");
            tmp.Add(typeof(SplatAlpha).Name, "Textures.xml");
            tmp.Add(typeof(DUDVTexture).Name, "Textures.xml");

            return tmp;
        }

        internal bool IsInlineType(Node node)
        {
            return IsInlineType(node.TypeName);
        }

        internal bool IsInlineType(string typeName)
        {
            //if (typeName == "StellarSystem")
            //    System.Diagnostics.Debug.WriteLine("XMLDatabase.IsInlineType() - " + typeName);
            // if this type does not have a dedicated xml table, then it IS an inline type
            bool result = !ClassMappingTables.ContainsKey(typeName);
            return result;
        }

        protected string GetTableName(string typename)
        {
            try
            {
                // TODO: if the typename is a Sector or Zone, then these should all have table names that start with TABLENAME + ID
                //  so we probably should use an overloaded GetTableName that also accepts the id?
                return ClassMappingTables[typename];
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SceneArchiver.GetTableName() - ERROR: Table '" + typename + "' does not exist.");
                throw ex;
            }
        }

 #region IDisposable Members
        private bool _disposed = false;

        ~XMLDatabase()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // pass true if managed resources should be disposed. Otherwise false.
        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    DisposeManagedResources();
                    _disposed = true;
                }
                DisposeUnmanagedResources();
            }
        }

        protected virtual void DisposeManagedResources()
        {
        	//foreach (FileMap map in FileMaps.Values)
        	//	map.Document.

			if (mWriter != null) mWriter.Dispose();
			if (mReader != null) mReader.Dispose();
        }

        protected virtual void DisposeUnmanagedResources()
        {
        }

        protected void CheckDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name + " is disposed.");
        }

        protected bool IsDisposed
        {
            get { return _disposed; }
        }

        #endregion
    }
}
