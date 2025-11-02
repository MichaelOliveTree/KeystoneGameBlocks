using System;
using System.Diagnostics;
using Keystone.Appearance;
using Keystone.Resource;
using MTV3D65;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;
using System.Runtime.InteropServices;

namespace Keystone
{
    // used for importing any resource file (mesh, actor, texture, some other types)
    // since we may need to first deal with extracting the resource from a zip.
    //
    // used for importing a TVA or TVM from file and returning a Actor3d or Mesh3d.
    // Afterwards they are added to the cache of content so that they can be shared.
    // The key here is that if materials and textures are loaded, the KGBTexture, Material
    // GroupAttribute, Appearance nodes are created to properly convert them into KGB scene
    // compatible elements.  

    // In a nuttshell, ifyou want to import a TVA,TVM or X file into the scene, you must
    // use these methods.  Otherwise you are loading / paing XML of serialized KBG nodes.

    // NOTE: I had contemplated using static methods within the respective classes 
    // (e.g. Mesh3d has static method for importing a TVM) but then it occurred to me
    // that if we also want to import the textures and materials, then that static
    // method would also need to call static methods to those other classes and 
    // that seems to get messy.
    // Here at least they are all in one spot and the cache's are accessible too.
    // Incidentally, i believe the import lib should be initialized with the cache's passed in.
    // Afterall, these caches need to be available to the Scene manager.
    public static class ImportLib
    {
        public const string XFILE_EXTENSION = ".X";
        public const string TVMESH_EXTENSION = ".TVM";
        public const string TVACTOR_EXTENSION = ".TVA";
        public const string WAVEFRONTOBJ_EXTENSION = ".OBJ";


        #region Load Functions
        public static Keystone.Elements.Node Load(string archiveFullPath, string entryPath, bool generateIDs, bool recursiveLoadChildren, bool delayPagingResources, string[] nodeIDsToUse)
        {
            Keystone.Elements.Node node = null;
            System.IO.Stream stream = null;

            try
            {
                // TODO: mesh types are being filtered out when looking to add a new mesh3d
                // geometry to a new model under a model sequence in the plugin
                IO.XMLDatabaseCompressed db = new IO.XMLDatabaseCompressed();

                // open the outer database from the paths provided            
                // TODO: sometimes stream is returned as null "file used by another process.." why? we only open FileAccess.Read
                stream = KeyCommon.IO.ArchiveIOHelper.GetStreamFromMod(
                    entryPath, null, archiveFullPath);

                // TODO: GetStreamFromArchive(guid, "", guid); // use GUIDs and a hash table of database and entries to keep network packet small (no need for string paths)

                // now open the inner prefab database from the stream of the outer database
                Scene.SceneInfo info = db.Open(stream);
                node = db.ReadSynchronous(info.FirstNodeTypeName, null, recursiveLoadChildren, generateIDs, nodeIDsToUse, delayPagingResources, false);
            }
            catch (OutOfMemoryException memory)
            {
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
            return node;

        }

        public static Keystone.Elements.Node Load(string fullPath, bool generateIDs, bool recursiveLoadChildren, bool delayPagingResources, string[] nodeIDsToUse)
        {
            Keystone.Elements.Node node = null;
            IO.XMLDatabaseCompressed db = null;

            try
            {
                db = new IO.XMLDatabaseCompressed();
                Scene.SceneInfo info = db.Open(fullPath, true);

                node = db.ReadSynchronous(info.FirstNodeTypeName, null, recursiveLoadChildren, generateIDs, nodeIDsToUse, delayPagingResources, false);
            }
            catch (OutOfMemoryException memory)
            {
                System.Diagnostics.Debug.WriteLine("ImportLib.Load() - Out of Memory Exception " + memory.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ImportLib.Load() - error - " + ex.Message);
            }
            finally
            {
                if (db != null) db.Dispose();
            }
            return node;
        }
        #endregion
        
        #region Imports Functions // routines to assist with quick importing of content into a scene

        // http://msdn.microsoft.com/en-us/library/system.io.unmanagedmemorystream.aspx
        // then we can write extension methods for our various Load (mesh.Load, actor.Load) that accepts the stream directly
        // which then in turn can make a call to an overloaded globals.GetDataSourceFromMemory()

        //<Aeon> bool loaded = Asteroid.Shader.CreateFromEffectString(Core.GetShader("Effects/MiniMesh.fx"));

        // TODO: I think for cases when dealing with 
        public static string GetTVDataSource(string entryName, string password, string zipFilePath, out GCHandle gchandle)
        {
            ZipFile zip = null;
            gchandle = default (GCHandle );
            try
            {
                zip = new ZipFile(zipFilePath);
                if (zip != null)
                    // internally entry names use forward slashes
                    return GetTVDataSource(entryName, password, zip, out gchandle);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (zip != null)
                    zip.Dispose();
            }
            return null;
        }

        //public static string GetTVDataSource(Runtime.InteropServices.GCHandle _gch)
        //{
        //    System.Drawing.Bitmap bmp;
        ////    // TODO: add bitmap loading code bmp.Load(file);

        //    System.IO.MemoryStream mem = new System.IO.MemoryStream();
        //    bmp.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
        //    byte[] b = mem.ToArray();



        //    _b = mem.GetBuffer();
        //    mem.Flush();
        //    mem.Close();
        //    mem.Dispose();

        //}

        /// <summary>
        /// Get's a file from the archive for TV to use in it's .Load* functions.
        /// </summary>
        /// <param name="entryName"></param>
        /// <param name="password"></param>
        /// <param name="zip"></param>
        /// <param name="gchandle"></param>
        /// <returns></returns>
        public static string GetTVDataSource(string entryName, string password, ZipFile zip, out GCHandle gchandle)
        {
            entryName = entryName.Replace("\\", "/");
            gchandle = default (GCHandle );
            foreach (ZipEntry entry in zip.Entries)
            {
            	// TODO: i should go back to allowing case specific.  that means allowing plugin when searching
            	// for resources to assign to models and appearances to not force ToLower().  do search and repalce for "ToLower()"
            	// the only reason i had a problem in first place was due to a hardcoded resource path to dirt.kgbsement that had lower case \terrain\ instead of capital \Terrain\
            	if (entry.FileName.ToLower() == entryName.ToLower())
                {
                    byte[] bBuffer = new byte[(int)(entry.UncompressedSize)];

                    //entry.Password = password;
                    //System.IO.MemoryStream mem = new System.IO.MemoryStream ();
                    //entry.Extract(mem);
                    //mem.Read(bBuffer, 0, (int)mem.Length);
                    //mem.Close();

                    CrcCalculatorStream stream = entry.OpenReader(password);
                    //EventHandler <ExtractProgressEventArgs >(
                    //zip.AddProgress   AddProgressEventArgs
                    //zip.ExtractProgress += ExtractProgressEventArgs
                    //System.ComponentModel.BackgroundWorker();
                    stream.Read(bBuffer, 0, (int)stream.Length);
                    stream.Close();

                    if (bBuffer.Length != 0) // if the entry exists but is 0 size
                    {
                        gchandle = GCHandle.Alloc(bBuffer, GCHandleType.Pinned);
                        string fileName = CoreClient._CoreClient.Globals.GetDataSourceFromMemory(gchandle.AddrOfPinnedObject().ToInt32(), bBuffer.Length);
                        return fileName;
                    }
                    else
                        return null;

                    //unsafe
                    //{
                    //    // TODO: this is Aeon's code but there's a bug most likely
                    //    // since we return filename outside of the fixed block, there's a chance that the underlying
                    //    // bBuffer gets moved around after a garbage collection occurs and so you end up with
                    //    // invalid pointer
                    //    fixed (byte* s = bBuffer)
                    //    {
                    //        IntPtr pointer = new IntPtr(s);
                    //        string filename = CoreClient._CoreClient.Globals.GetDataSourceFromMemory(pointer.ToInt32(), bBuffer.Length);
                    //        return filename;
                    //    }
                    //}
                    //entry.ArchiveStream.Read(bBuffer, (int)entry.FileDataPosition, (int)(entry.UncompressedSize));

                }
            }
            return null;
        }

 
        // if in "edit" we are dealing with uncompressed data (but prefabs and such are always compressed?  maybe we need to
        // stop that?
        // if in release mode, we are dealing with compressed... 
        // The above simplifies how we attempt to create the relative path and GCHandle paths for tv loads or streamreader 
        // for our obj loader.

        // The prefab within archive puts us in a situation where we have to be able to Read the archive from in an archive.
        // The prefabs can use zip "store" (uncompressed) since they'll be compressed in the overall zip for release.
        // That addresses performance question, but still we have to be able to read zip from within zip.
        // 
        // I think the key aspects to this are our gallery needs to be able to browse archives
        // and the resultant 'path' sent to SceneReader.
        //     Or the other option it to not compressed prefabs... i dont think crysis does this.  
        //     This would mean our SceneInfo file should take the name of the main .prefab save file but
        //     have an .info extension.  Thus we'd have .info, .prefab, .ico  or .png 
        //     This really is the sane option so we dont have archives within archives.
        //  When storing a new prefab, we can perhaps enforce a few more things such as
        //  Not allowing prefabs to be stored in child directories of other prefab dirs so that
        //  modName\prefabs\vehicles\sagitarius  is allowed but  modName\prefabs\vehicles\sagitarius\turret is not unless
        //  sagitarius is empty?  Well maybe no restriction is necessary.  User will realize that too deep hiearchies will make browsing
        //  a PITA.  Since it'd be better to have the turret.prefab and hull.prefab all under \sagitarius directly.
        //  Second, when browsing in gallery, the prefabs -- wait, no second needed for same reason.  We already have code
        // that shows the child directory as a group category and the ico or png for the prefabs within displayed directly. That is
        // how we already do things.
        // So consider the above SOLVED.  We just need to enforce .prefab name to match the .info name
        // and allow for SelectDocument to work with final file names and not the .tmp files.

        public static string AbsolutePathToRelative(string absolutePath)
        {
            string temp = absolutePath.ToUpper();
            string result = absolutePath; // default if this path is already relative path
            int length = 0;
            if (temp.Contains(Core._Core.DataPath.ToUpper()))
            {
                length = Core._Core.DataPath.Length;
                // TODO: need to deal with case sensitivity of folder names :/  All we have to do really is
                // compare both as ToUpper() and then find the start index and then in the original string, truncate it
                result = absolutePath.Substring(length, absolutePath.Length - length);
            }
            else 
            {
                System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(Core._Core.DataPath);

                if (temp.Contains(info.FullName.ToUpper ()))
                {
                    length = info.FullName.Length;
                    result = absolutePath.Substring(length, absolutePath.Length - length );
                }
            }
            return result;
        }

        public static string RelativePathToLoadablePath(string relativePath, out System.Runtime.InteropServices.GCHandle ghc)
        {

            string basePath = System.IO.Path.Combine(Core._Core.DataPath, "mods");
            int length;
            

            System.Runtime.InteropServices.GCHandle handle = new System.Runtime.InteropServices.GCHandle();
            ghc = handle;


            // seems to me there's only 3 things to do.
            // 0) on import, must copy things to the mod folder.  That's really simple.
            // 1) set the relative path in the file
            //      a) entails renaming paths in the tvm or tva materials and textures to match...well not exactly.  We're 
            //          simply renaming them in the model prefab'ss xml
            // 2) when loading the relative path needs to be turned into a loadable full path which is what happens here
            //     a) A loadable full path could be a zip memory stream
            //         - if it's in a zip, extract it to the stream and send the memory location to either tv's load or .obj loader

            // TODO: import commands need to set as resource path the relative path.
            string[] splitPath = relativePath.Split(System.IO.Path.DirectorySeparatorChar);
            //Core._Core.ModsPath;

            //// first set of "\\" will mark the end of the mod name
            //length = relativePath.IndexOf("\\");
            //string modName = relativePath.Substring(0, length);

            //// verify this mod directory exists
            //bool result = System.IO.Directory.Exists(basePath + "\\" +  modName);

            //if (result)
            //{
            //    // next bit should be "resources"
            //    length = relativePath.IndexOf("\\", length);
            //    Debug.Assert ("resources" == relativePath.Substring ();

            //    // finally, next bit will be the resource type which will translate into a potential archive name
            //    string resourceName = "meshes";
            //    string relativeArchivePath = modName + "\\resources\\" + resourceName;

            //    // does the archive exist already (we use relative archive path because we can have multiple mods loaded)
            //    result = mArchives.TryGetValue(relativeArchivePath, ref archiver);
            //    if (!result)
            //    {
            //        // does the archive exist at all?
            //        result = System.IO.File.Exists (basePath + "\\" +  relativeArchivePath);

            //        // extract the deserived file if it exists

            //        if (result)
            //        {
            //            // configure a pinned gchandle to the stream so tv can load from it
            //            // GetDataSourceFromMemory HOWEVER
            //            // .obj for our .obj loader would just want the raw stream since we can deal with that
            //            // ourselves.
            //        }
            //        }
            //    }

            //    // first try to load from disk, else try to load from an archive for Meshes based on the mod path
            //    // which is the first folder after \\Data\\$mod_name\\resources\\meshes

            //    switch (type)
            //    {
            //        case typeof(TVMesh):
            //            break;

            //        default:
            //            break;
            //    }

            //    return result;
            //}
            return "";
        }

        
        public static Layer Import(string path, CONST_TV_LAYER typeLayer, CONST_TV_TEXTURETYPE type)
        {
            Layer l = (Layer) Repository.Get(path);
            if (l != null) return l;

            int index;
            //index = Core._CoreClient.TextureFactory.LoadAlphaTexture(path, path);


            switch (type)
            {
                case CONST_TV_TEXTURETYPE.TV_TEXTURE_NORMALMAP:
                    index = CoreClient._CoreClient.TextureFactory.LoadTexture(path, path);
                    //Trace.Assert(typeLayer == CONST_TV_LAYER.TV_LAYER_1);
                    break;
                case CONST_TV_TEXTURETYPE.TV_TEXTURE_BUMPMAP:
                    index = CoreClient._CoreClient.TextureFactory.LoadBumpTexture(path, path);
                    // transforms a heightmap bumpmap into a tv proper normalmap
                    //Core._CoreClient.TextureFactory.ConvertNormalMap(index); // NOTE: I believe this is only needed if you loaded the BumpTexture with regular LoadTexture?
                    Trace.Assert(typeLayer == CONST_TV_LAYER.TV_LAYER_1);
                    break;
                case CONST_TV_TEXTURETYPE.TV_TEXTURE_CUBICMAP:
                    index = CoreClient._CoreClient.TextureFactory.LoadCubeTexture(path, path);
                    break;
                case CONST_TV_TEXTURETYPE.TV_TEXTURE_DIFFUSEMAP:
                    index = CoreClient._CoreClient.TextureFactory.LoadTexture(path, path);
                    //Trace.Assert(typeLayer == CONST_TV_LAYER.TV_LAYER_0);
                    break;
                case CONST_TV_TEXTURETYPE.TV_TEXTURE_DUDVMAP:
                    index = CoreClient._CoreClient.TextureFactory.LoadDUDVTexture(path, path);
                    break;
                case CONST_TV_TEXTURETYPE.TV_TEXTURE_VOLUMEMAP:
                    index = CoreClient._CoreClient.TextureFactory.LoadVolumeTexture(path, path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("TextureType '" + type.ToString() + " ' not supported.");
            }

            Trace.WriteLine("Texture imported '" + path + "'");
            return TextureLayerFromTVTexture(index, typeLayer);
        }

        // TODO: there is an error where if a .x or .tvm has a material that is the same name as an existing one in the repository
        // (which is easy to do when they are just given names like Material1, Material2, etc)
        // then the material get's shared instead of a new one created and in effect, the actual material values for the mesh/actor
        // that is being loaded don't get used.  For materials,  they should always be new when they imported within a mesh
        // and when importing within that mesh if every group has a unique material, we should actually share it instead if the names
        // are different but the values are the same.  Because some exporters wont recycle materials and so we end up with a bunch of
        // different material nodes that all have same values as one another.  So to do this we'd just need a temporary
        // Material node cache to check against previously loaded materials and use those.
        // IMPORTANT: But we then need to delete the TVTExtureFactory textureID that tv loaded for that material!
        public static bool GetMeshTexturesAndMaterials(Keystone.Elements.Mesh3d mesh, out DefaultAppearance appearance)
        {
            TVMesh tvmesh = CoreClient._CoreClient.Globals.GetMesh(mesh.ID);
            GetMeshTexturesAndMaterials(tvmesh, out appearance);
            return true;
        }

        public static bool GetMeshTexturesAndMaterials(TVMesh tvmesh,  out DefaultAppearance appearance)
        {
            appearance = new DefaultAppearance(Repository.GetNewName(typeof(DefaultAppearance)));
            //There are max 4 layers on a Mesh that are represented by
            //- TV_LAYER_0
            //- TV_LAYER_1
            //- TV_LAYER_2
            //- TV_LAYER_3
            //If you want to save/get all textures (for an editor or something), use these 4, you won't forget or do redundant things.

            //Then you have some "helpers" layer, that points to one of the layer. The names are only for ease of use and means nothing to the engine. It's just to not to have to remember that lightmaps or normalmaps are expected to go in the layer1. The engine expects you to have the textures in these layers for the effects.

            //TV_LAYER_BASETEXTURE -> layer 0 ( same as diffuse )
            //TV_LAYER_LIGHTMAP -> layer 1
            //TV_LAYER_DETAILMAP -> layer 1
            //TV_LAYER_NORMALMAP -> layer 1 (same as bumpmap)
            //TV_LAYER_BUMPMAP -> layer 1 (same as normalmap)
            //TV_LAYER_HEIGHTMAP -> layer 2 ( heightmap is used for OffsetBumpmapping)
            //TV_LAYER_SPECULARMAP -> layer 2 ( well it's better to put the specular map in the normalmap alpha :p)
            //TV_LAYER_EMISSIVE -> layer 3 (only used for Glow emissive)
            // -Sylvain
            //
            // also read http://www.truevision3d.com/phpBB2/viewtopic.php?t=14642
            // for Zak's picutres of the different combinations.  Yes its true that in those pictures
            // you wont see detail maps + lightmaps or lightmaps + normalmaps because those two use the same layers.
            // People can do it using custom shaders though and by instructing the shader how to interpret the texture
            // in a particular layer.
            // bool skipManualStages = false;

            GroupAttribute group;
            //  TODO: what if the groupCount <> the found textue count?
            //  we could give the user a chance to add another relative search path
            //  and then we can search both in the TextureLayersFromTVTexture..
            int groupCount = tvmesh.GetGroupCount();
            System.Diagnostics.Debug.Assert(groupCount >= 1, "Import.GetMeshTexturesAndMaterial() - Mesh cannot have less than 1 group!");

            for (int i = 0; i < groupCount; i++)
            {
                if (groupCount == 1) // we will not add a child GroupAttribute if there's only one group in the model!
                    group = appearance;
                else
                {
                    group = new GroupAttribute(Repository.GetNewName(typeof(GroupAttribute)));
                    appearance.AddChild(group);
                }
                int tvtextureIndex;
                int mat = tvmesh.GetMaterial(i);
                if (mat > 0)
                    group.AddChild(Material.Create(mat));
                                tvtextureIndex = tvmesh.GetTextureEx((int)CONST_TV_LAYER.TV_LAYER_0, i);
                if (tvtextureIndex > 0)
                    group.AddChild(TextureLayerFromTVTexture(tvtextureIndex, CONST_TV_LAYER.TV_LAYER_0));

                tvtextureIndex = tvmesh.GetTextureEx((int)CONST_TV_LAYER.TV_LAYER_1, i);
                if (tvtextureIndex > 0)
                    // layer 1 is usually a normal map or bumpmap so we'll change the default lightingmode accordingly
                    group.AddChild(TextureLayerFromTVTexture(tvtextureIndex, CONST_TV_LAYER.TV_LAYER_1));

                tvtextureIndex = tvmesh.GetTextureEx((int)CONST_TV_LAYER.TV_LAYER_2, i);
                if (tvtextureIndex > 0)
                    group.AddChild(TextureLayerFromTVTexture(tvtextureIndex, CONST_TV_LAYER.TV_LAYER_2));

                tvtextureIndex = tvmesh.GetTextureEx((int)CONST_TV_LAYER.TV_LAYER_3, i);
                if (tvtextureIndex > 0)
                    group.AddChild(TextureLayerFromTVTexture(tvtextureIndex, CONST_TV_LAYER.TV_LAYER_3));
                
            }
            return true;
        }

        public static bool GetActorTexturesAndMaterials(TVActor tvactor, out DefaultAppearance appearance)
        {
            appearance = new DefaultAppearance(Repository.GetNewName(typeof(DefaultAppearance)));
            GroupAttribute group;

            //  TODO: what if the groupCount <> the found textue count?
            //  we could give the user a chance to add another relative search path
            //  and then we can search both in the TextureLayersFromTVTexture..
            for (int i = 0; i < tvactor.GetGroupCount(); i++)
            {
                group = new GroupAttribute(Repository.GetNewName(typeof (GroupAttribute)));
                int mat = tvactor.GetMaterial(i);
                if ((mat > 0))
                {
                    group.AddChild(Material.Create(mat));
                }
                // texCollection.Add(New Unity.DefaultAttributeSet)
                //  check every layer... not very pretty but...
                appearance.AddChild(group);

                int j = tvactor.GetTextureEx((int) CONST_TV_LAYER.TV_LAYER_0, i);
                if (j > 0)
                {
                    group.AddChild(TextureLayerFromTVTexture(j, CONST_TV_LAYER.TV_LAYER_0));
                }
                j = tvactor.GetTextureEx((int) CONST_TV_LAYER.TV_LAYER_1, i);
                if (j > 0)
                {
                    group.AddChild(TextureLayerFromTVTexture(j, CONST_TV_LAYER.TV_LAYER_1));
                }
                j = tvactor.GetTextureEx((int) CONST_TV_LAYER.TV_LAYER_2, i);
                if (j > 0)
                {
                    group.AddChild(TextureLayerFromTVTexture(j, CONST_TV_LAYER.TV_LAYER_2));
                }
                j = tvactor.GetTextureEx((int) CONST_TV_LAYER.TV_LAYER_3, i);
                if (j > 0)
                {
                    group.AddChild(TextureLayerFromTVTexture(j, CONST_TV_LAYER.TV_LAYER_3));
                }
            }
            return true;
        }

        private static Layer TextureLayerFromTVTexture(int index, CONST_TV_LAYER typeLayer)
        {
            Layer l = null;
            Texture t = Texture.Create(index);
            string layerName = Repository.GetNewName(typeof(Layer).Name);

            switch (typeLayer)
            {
                case (CONST_TV_LAYER.TV_LAYER_0):
                    l = new Diffuse(layerName);
                    break;
                case (CONST_TV_LAYER.TV_LAYER_1):
                    l = new NormalMap(layerName); // also used for detail, lightmap
                    break;
                case (CONST_TV_LAYER.TV_LAYER_2):
                    l = new Specular(layerName);
                    // Heightmap for offset parallax bump mapping, but can also hold specularmap.  Though specularmap should be added to the alpha of the normal map layer 1
                    break;
                case (CONST_TV_LAYER.TV_LAYER_3):
                    l = new Emissive(layerName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unsupported texture layer type.");
            }

            l.AddChild(t);
            return l;
        }
    }

    #endregion
}