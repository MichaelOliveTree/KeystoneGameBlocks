using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Settings
{
    /// <summary>
    /// Shared methods for serializing / deserializing objects
    /// </summary>
    /// <remarks>'  Add a line such as the following for each class you intend to make serializable
    /// [XmlInclude(typeof(clsClass))]</remarks>
    public class Serializer
    {
        public static void Serialize(string fileName, object myObj, bool indented)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException();

            XmlSerializer serializer;
            Stream fs = null;
            XmlTextWriter writer = null;

            const char SPACE = (char)32;
            //const char TAB  = (char)0;

            try
            {
                serializer = new XmlSerializer(myObj.GetType());
                
                fs = File.OpenWrite( fileName);
                writer = new XmlTextWriter(fs, new UTF8Encoding());
                if (indented)
                {
                    writer.Formatting = Formatting.Indented;
                    writer.IndentChar = SPACE;
                    writer.Indentation = 3; //1 for tab
                }
                serializer.Serialize(writer, myObj);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    Debug.WriteLine("Serializer:Serialize() - " + ex.Message + Environment.NewLine + ex.Source + Environment.NewLine + ex.InnerException.ToString());
                else
                    Debug.WriteLine("Serializer:Serialize() - " + ex.Message + Environment.NewLine + ex.Source);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        public static object Deserialize(string fileName, object myNewObj)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException();

            XmlSerializer serializer;
            Stream  fs = null;
            XmlReader reader = null;

            try
            {
              
                fs = File.Open( fileName, FileMode.Open , FileAccess.Read , FileShare.ReadWrite );
                reader = new XmlTextReader(fs);
                serializer = new XmlSerializer(myNewObj.GetType());
                myNewObj = serializer.Deserialize(reader);
                return myNewObj;
            }
            catch (IOException ex)
            {
                Debug.WriteLine("Serializer:Deserialize() - " + ex.Message);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    Debug.WriteLine("Serializer:Deserialize() - " + ex.Message + Environment.NewLine + ex.Source + Environment.NewLine + ex.InnerException.ToString());
                else
                    Debug.WriteLine("Serializer:Deserialize() - " + ex.Message + Environment.NewLine + ex.Source);
            }
            finally
            {
                if (fs != null) 
                {
                    fs.Close();
                    fs.Dispose();
                }
                if (reader != null) reader.Close();
                
            }
            return null;
        }
    }
}