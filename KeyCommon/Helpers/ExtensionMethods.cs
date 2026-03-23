using System;
using System.Collections.Generic;
using Settings;
using Lidgren.Network;
using System.Xml;

namespace KeyCommon.Helpers
{
    // TODO: This entire class should be moved and renamed to something like  KeyCommon.IO.DataHelper
    public static class ExtensionMethods
    {
        private const ushort TYPE_STRING = 0;
        private const ushort TYPE_STRING_ARRAY = 15;
        private const ushort TYPE_JAGGED_STRING_ARRAY = 24;
        private const ushort TYPE_BOOL = 1;
        private const ushort TYPE_BYTE = 2;
        private const ushort TYPE_INT32 = 3;
        private const ushort TYPE_UINT32 = 4;
        private const ushort TYPE_DOUBLE = 5;
        private const ushort TYPE_FLOAT = 6;
        private const ushort TYPE_FLOAT_ARRAY = 26;
        private const ushort TYPE_JAGGED_FLOAT_ARRAY = 25;
        private const ushort TYPE_VECTOR2_F = 7;
        private const ushort TYPE_VECTOR3_I = 20;
        private const ushort TYPE_VECTOR3_F = 27;
        private const ushort TYPE_VECTOR3_D = 8;
        private const ushort TYPE_VECTOR3_D_ARRAY = 13;
        private const ushort TYPE_QUATERNION = 9;
        private const ushort TYPE_QUATERNION_ARRAY = 16;
        private const ushort TYPE_COLOR = 10;
        private const ushort TYPE_BOUNDING_BOX = 11;
        private const ushort TYPE_INT64 = 12;
        private const ushort TYPE_MATRIX = 14;
        private const ushort TYPE_INT32_ARRAY = 17;
        private const ushort TYPE_BYTE_2D_ARRAY = 18;
        private const ushort TYPE_INT32_2D_ARRAY = 19;
        private const ushort TYPE_PROPERTY_SPEC = 21;
        private const ushort TYPE_PROPERTY_SPEC_ARRAY = 22;
        private const ushort TYPE_UINT32_ARRAY = 23;
        private const ushort TYPE_UINT64_ARRAY = 28;
        private const ushort TYPE_RECTANGLE_F_ARRAY = 29;
        private const ushort TYPE_VECTOR4 = 30;
        private const ushort TYPE_PARTICLE_KEYFRAME_ARRAY = 31;
        private const ushort TYPE_EMITTER_KEYFRAME_ARRAY = 32;
        private const ushort TYPE_PROPERTY_SPEC_ARRAY_MULTI = 33;


        // intrinsic types - really sucks having these here because
        // these can change easily by having the version change
        public const string mFullyQualifiedString = "System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(string).AssemblyQualifiedName;
        public const string mFullyQualifiedBool = "System.Boolean, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(bool).AssemblyQualifiedName;
        private const string mFullyQualifiedByte = "System.Byte, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(byte).AssemblyQualifiedName;
        private const string mFullyQualifiedInt = "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(int).AssemblyQualifiedName;
        private const string mFullyQualifiedInt64 = "System.Int64, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(long).AssemblyQualifiedName;
        private const string mFullyQualifiedUint = "System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(uint).AssemblyQualifiedName;
        public const string mFullyQualifiedDouble = "System.Double, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(double).AssemblyQualifiedName;
        public const string mFullyQualifiedSingle = "System.Single, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"; // typeof(float).AssemblyQualifiedName;

        private const string mFullyQualifiedVector2f = "Keystone.Types.Vector2f, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Vector2f).AssemblyQualifiedName;
        private const string mFullyQualifiedVector3i = "Keystone.Types.Vector3i, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Vector3i).AssemblyQualifiedName;
        private const string mFullyQualifiedVector3f = "Keystone.Types.Vector3f, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Vector3f).AssemblyQualifiedName;
        private const string mFullyQualifiedVector3d = "Keystone.Types.Vector3d, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Vector3d).AssemblyQualifiedName;
        private const string mFullyQualifiedBoundingBox = "Keystone.Types.BoundingBox, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.BoundingBox).AssemblyQualifiedName;
        private const string mFullyQualifiedQuaternion = "Keystone.Types.Quaternion, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Quaternion).AssemblyQualifiedName;
        private const string mFullyQualifiedColor = "Keystone.Types.Color, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Color).AssemblyQualifiedName;
        private const string mFullyQualifiedMatrix = "Keystone.Types.Matrix, KeyStandardLibrary, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Keystone.Types.Matrix).AssemblyQualifiedName;

        private const string mFullyQualifiedPropertySpec = "Settings.PropertySpec, Settings, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Settings.PropertySpec).AssemblyQualifiedName;
        private const string mFullyQualifiedPropertySpecMULTI = "Settings.PropertySpec[,], Settings, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"; // typeof(Settings.PropertySpec).AssemblyQualifiedName;

        public delegate object ReadUserTypeDelegate(NetBuffer buffer, ushort typeID, out string typeName);
        public delegate bool WriteUserTypeDelegate(NetBuffer buffer, object value, string typeName);
        public delegate ushort GetUserTypeIDFromTypename(string typeName);
        public delegate string GetUserTypeNameFromTypeID(ushort typeID);
        public delegate object UserArrrayElementsMerge(string typeName, object currentValue, object value);


        public static ReadUserTypeDelegate mUserTypeReader;
        public static WriteUserTypeDelegate mUserTypeWriter;
        public static GetUserTypeIDFromTypename mUserTypeIDFromTypename;
        public static GetUserTypeNameFromTypeID mUserTypenameFromTypeID;
        public static UserArrrayElementsMerge mUserArrayElementsMerge;


        private static string GetTypenameFromTypeID(ushort typeID)
        {
            switch (typeID)
            {
                case TYPE_PARTICLE_KEYFRAME_ARRAY: return "ParticleKeyframe[]";
                case TYPE_EMITTER_KEYFRAME_ARRAY: return "EmitterKeyframe[]";
                case TYPE_PROPERTY_SPEC: return "PropertySpec";
                case TYPE_PROPERTY_SPEC_ARRAY: return "PropertySpec[]";
                case TYPE_PROPERTY_SPEC_ARRAY_MULTI: return "PropertySpec[,]";
                case TYPE_STRING: return "String";
                case TYPE_STRING_ARRAY: return "String[]";
                case TYPE_JAGGED_STRING_ARRAY: return "String[][]";
                case TYPE_BOOL: return "Bool";
                case TYPE_BYTE: return "Byte";
                case TYPE_BYTE_2D_ARRAY: return "Byte[,]";
                case TYPE_INT32: return "Int32";
                case TYPE_INT32_ARRAY: return "Int32[]";
                case TYPE_INT32_2D_ARRAY: return "Int32[,]";
                case TYPE_UINT32: return "UInt32";
                case TYPE_UINT32_ARRAY: return "UInt32[]";
                case TYPE_UINT64_ARRAY: return "UInt64[]";
                case TYPE_DOUBLE: return "Double";

                case TYPE_FLOAT: return "Float";
                case TYPE_FLOAT_ARRAY: return "Float[]";
                case TYPE_JAGGED_FLOAT_ARRAY: return "Float[][]";

                case TYPE_VECTOR2_F: return "Vector2f";
                case TYPE_VECTOR3_I: return "Vector3i";
                case TYPE_VECTOR3_F: return "Vector3f";
                case TYPE_VECTOR3_D: return "Vector3d";
                case TYPE_VECTOR4: return "Vector4";
                case TYPE_VECTOR3_D_ARRAY: return "Vector3d[]";

                case TYPE_QUATERNION: return "Quaternion";
                case TYPE_QUATERNION_ARRAY: return "Quaternion[]";
                case TYPE_MATRIX: return "Matrix";
                case TYPE_COLOR: return "Color";
                case TYPE_BOUNDING_BOX: return "BoundingBox";
                case TYPE_RECTANGLE_F_ARRAY: return "RectangleF[]";
                case TYPE_INT64: return "Int64";
                default:
                    if (mUserTypenameFromTypeID != null)
                    {
                        string result = mUserTypenameFromTypeID(typeID);
                        if (string.IsNullOrEmpty(result)) throw new Exception("ExtensionMethods.GetTypenameFromTypeID() - ERROR: Unsupported type ID '" + typeID.ToString() + "'");
                        return result;
                    }
                    throw new Exception("ExtensionMethods.GetTypenameFromTypeID() - ERROR: Unsupported type ID '" + typeID.ToString() + "'");

            }
        }

        private static ushort GetTypeIDFromTypename(string typeName)
        {

            switch (typeName)
            {
                case mFullyQualifiedPropertySpec:
                case "PropertySpec":
                case "Settings.PropertySpec":
                    return TYPE_PROPERTY_SPEC;

                case "PropertySpec[]":
                case "Settings.PropertySpec[]":
                    return TYPE_PROPERTY_SPEC_ARRAY;

                case mFullyQualifiedPropertySpecMULTI:
                case "PropertySpec[,]":
                case "Settings.PropertySpec[,]":
                    return TYPE_PROPERTY_SPEC_ARRAY_MULTI;

                case mFullyQualifiedString:
                case "String":
                case "string":
                    return TYPE_STRING;

                case "String[]":
                case "string[]":
                    return TYPE_STRING_ARRAY;

                case "String[][]":
                case "string[][]":
                    return TYPE_JAGGED_STRING_ARRAY;

                case mFullyQualifiedBool:
                case "Bool":
                case "bool":
                case "Boolean":
                    return TYPE_BOOL;

                case mFullyQualifiedByte:
                case "byte":
                case "Byte":
                    return TYPE_BYTE;
                case "Byte[,]":
                    return TYPE_BYTE_2D_ARRAY;

                case mFullyQualifiedInt:
                case "int":
                case "integer":
                case "Integer":
                case "Int32":
                    return TYPE_INT32;

                case "int[]":
                case "Int32[]":
                    return TYPE_INT32_ARRAY;
                case "Int32[,]":
                    return TYPE_INT32_2D_ARRAY;
                case mFullyQualifiedUint:
                case "UInt32":
                    return TYPE_UINT32;
                case "UInt32[]":
                    return TYPE_UINT32_ARRAY;
                case "UInt64[]":
                    return TYPE_UINT64_ARRAY;

                case mFullyQualifiedDouble:
                case "Double":
                case "double":
                    return TYPE_DOUBLE;

                case mFullyQualifiedSingle:
                case "Single":
                case "float":
                case "Float":
                    return TYPE_FLOAT;
                case "Single[]":
                case "Float[]":
                case "float[]":
                    return TYPE_FLOAT_ARRAY;

                case "float[][]":
                case "Float[][]":
                    return TYPE_JAGGED_FLOAT_ARRAY;

                // shader types 
                //case "Float2":
                //case "Float3":
                //case "Float4":

                case mFullyQualifiedVector2f:
                case "Vector2f":
                    return TYPE_VECTOR2_F;
                case mFullyQualifiedVector3f:
                case "Vector3f":
                    return TYPE_VECTOR3_F;
                case mFullyQualifiedVector3d:
                case "Vector3d":
                    return TYPE_VECTOR3_D;
                case "Vector4": // Vector4 is mostly used by shaders for Color parameter
                    return TYPE_VECTOR4; 
                case mFullyQualifiedVector3i:
                case "Vector3i":
                    return TYPE_VECTOR3_I;
                case "Vector3d[]":
                    return TYPE_VECTOR3_D_ARRAY;

                case mFullyQualifiedQuaternion:
                case "Quaternion":
                    return TYPE_QUATERNION;
                case "Quaternion[]":
                    return TYPE_QUATERNION_ARRAY;
                case mFullyQualifiedMatrix:
                case "Matrix":
                    return TYPE_MATRIX;

                case mFullyQualifiedColor:
                case "Color":
                    return TYPE_COLOR;

                case mFullyQualifiedBoundingBox:
                case "BoundingBox":
                    return TYPE_BOUNDING_BOX;
                case "RectangleF[]":
                    return TYPE_RECTANGLE_F_ARRAY;
                case mFullyQualifiedInt64:
                case "long":
                case "Int64":
                    return TYPE_INT64;
                case "ParticleKeyframe[]":
                    return TYPE_PARTICLE_KEYFRAME_ARRAY;
                case "EmitterKeyframe[]":
                    return TYPE_EMITTER_KEYFRAME_ARRAY;
                // these are temporary cases just to prevent the exception. not sure yet how
                // to handle these types which we custom serialize/deserialize'
                // TODO: can we pass in an array of user types that game01.dll will know how to handle?  maybe call a user delegate to see if it can handle the type?
                case "Production[]":
                case "Path[]":
                case "Stack`1":
                    return 100;
                default:
                    {
                        if (mUserTypeIDFromTypename == null) throw new Exception("ExtensionMethods.GetTypeIDFromTypename() - ERROR: Unsupported type '" + typeName + "'");
                        ushort result = mUserTypeIDFromTypename(typeName);
                        if (result > 0) return result;
                        else throw new Exception("ExtensionMethods.GetTypeIDFromTypename() - ERROR: Unsupported type '" + typeName + "'");
                    }
            }
        }


        // NOTE: DomainObject should NOT make this conversion because we still want
        // the shorter names for serialization.
        /// <summary>
        /// The propertyGrid needs fully qualified names in order to display
        /// a propertyspec item in the grid.  
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static string GetFullyQualifiedTypeName(string typeName)
        {
            ushort typeID = GetTypeIDFromTypename(typeName);

            switch (typeID)
            {
                case TYPE_PROPERTY_SPEC:
                    return typeof(Settings.PropertySpec).AssemblyQualifiedName;
                case TYPE_PROPERTY_SPEC_ARRAY:
                    return typeof(Settings.PropertySpec[]).AssemblyQualifiedName;

                case TYPE_PROPERTY_SPEC_ARRAY_MULTI:
                    return typeof(Settings.PropertySpec[,]).AssemblyQualifiedName;

                case TYPE_BOOL:
                    return typeof(bool).AssemblyQualifiedName;

                case TYPE_BYTE:
                    return typeof(byte).AssemblyQualifiedName;

                case TYPE_STRING:
                    return typeof(string).AssemblyQualifiedName;

                case TYPE_STRING_ARRAY:
                    return typeof(string[]).AssemblyQualifiedName;
                case TYPE_JAGGED_STRING_ARRAY:
                    return typeof(string[][]).AssemblyQualifiedName;

                case TYPE_INT32:
                    return typeof(int).AssemblyQualifiedName;

                case TYPE_INT32_ARRAY:
                    return typeof(int[]).AssemblyQualifiedName;
                case TYPE_INT32_2D_ARRAY:
                    return typeof(int[,]).AssemblyQualifiedName;
                case TYPE_UINT32:
                    return typeof(uint).AssemblyQualifiedName;
                case TYPE_UINT32_ARRAY:
                    return typeof(uint[]).AssemblyQualifiedName;
                case TYPE_INT64:
                    return typeof(Int64).AssemblyQualifiedName;
                case TYPE_UINT64_ARRAY:
                    return typeof(UInt64[]).AssemblyQualifiedName;
                case TYPE_DOUBLE:
                    return typeof(double).AssemblyQualifiedName;

                case TYPE_FLOAT:
                    return typeof(float).AssemblyQualifiedName;
                case TYPE_FLOAT_ARRAY:
                    return typeof(float[]).AssemblyQualifiedName;
                case TYPE_JAGGED_FLOAT_ARRAY:
                    return typeof(float[][]).AssemblyQualifiedName;

                case TYPE_VECTOR2_F:
                    return typeof(Keystone.Types.Vector2f).AssemblyQualifiedName;
                case TYPE_VECTOR3_I:
                    return typeof(Keystone.Types.Vector3i).AssemblyQualifiedName;
                case TYPE_VECTOR3_D:
                    return typeof(Keystone.Types.Vector3d).AssemblyQualifiedName;
                case TYPE_VECTOR4:
                    return typeof (Keystone.Types.Vector4).AssemblyQualifiedName;

                case TYPE_VECTOR3_D_ARRAY:
                    return typeof(Keystone.Types.Vector3d[]).AssemblyQualifiedName;
                case TYPE_RECTANGLE_F_ARRAY:
                    return typeof(System.Drawing.RectangleF[]).AssemblyQualifiedName;
                case TYPE_MATRIX:
                    return typeof(Keystone.Types.Matrix).AssemblyQualifiedName;

                case TYPE_COLOR:
                    return typeof(Keystone.Types.Color).AssemblyQualifiedName;
                case TYPE_PARTICLE_KEYFRAME_ARRAY:
                    return typeof(Keystone.KeyFrames.ParticleKeyframe[]).AssemblyQualifiedName;
                case TYPE_EMITTER_KEYFRAME_ARRAY:
                    return typeof(Keystone.KeyFrames.EmitterKeyframe[]).AssemblyQualifiedName;
                default:
                    // it's vital that we return full qualifed name or the propertGrid will throw all sorts of errors!
                    // TEXTURE0 param is no good, we need to exclude uknown types...
                    //throw new ArgumentOutOfRangeException("EditorHost.GetFullyQualifiedTypeName() - Cannot convert typename '" + typeName + "'");
                    // TODO: Scripted UserTypes like SensorContact[] and Craftsmanship?
                    return typeof(string).AssemblyQualifiedName;
            }
        }

        public static string GetFullQualifiedTypeConverterName(string typeName)
        {
            ushort typeID = GetTypeIDFromTypename(typeName);

            switch (typeID)
            {
                case TYPE_VECTOR3_D:
                    return typeof(Keystone.TypeConverters.Vector3dConverter).AssemblyQualifiedName;
                case TYPE_MATRIX:
                    return typeof(Keystone.TypeConverters.MatrixConverter).AssemblyQualifiedName;
                case TYPE_COLOR:
                    return typeof(Keystone.TypeConverters.ColorConverter).AssemblyQualifiedName;
                default:
                    return "";
            }
        }

        // name
        public static string CustomPropertyNamesToString(Settings.PropertySpec[] properties)
        {
            string result = null;
            if (properties == null || properties.Length == 0) return result;

            for (int i = 0; i < properties.Length; i++)
            {
                try
                {
                    // NOTE: we serialize null defaultvalues as well so that the 
                    // array length matches the # of properties
                    // TODO: is stringBuilder faster for appending?
                    if (string.IsNullOrEmpty(result) == false)
                        result += keymath.ParseHelper.English.XMLAttributeNestedDelimiter;
                    result += properties[i].Name;


                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Helpers.ExtensionMethods.CustomPropertyNamesToString() - ERROR: serializing '" + properties[i].Name + "'" + ex.Message);
                }
            }

            return result;
        }

        // typename
        public static string CustomPropertyTypesToString(Settings.PropertySpec[] properties)
        {
            string result = null;
            if (properties == null || properties.Length == 0) return result;

            for (int i = 0; i < properties.Length; i++)
            {
                try
                {
                    // NOTE: we serialize null defaultvalues as well so that the 
                    // array length matches the # of properties
                    // TODO: is stringBuilder faster for appending?
                    if (string.IsNullOrEmpty(result) == false)
                        result += keymath.ParseHelper.English.XMLAttributeNestedDelimiter;
                    result += properties[i].TypeName;


                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Helpers.ExtensionMethods.CustomPropertyTypesToString() - ERROR: serializing '" + properties[i].Name + "'" + ex.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Store just the values of all custom properties in a signle delimited string
        /// so that it can be stored as a single xml attribute
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static string CustomPropertyValuesToString(Settings.PropertySpec[] properties)
        {
            string result = null;
            if (properties == null || properties.Length == 0) return result;

            for (int i = 0; i < properties.Length; i++)
            {
                try
                {
                    // NOTE: we serialize null defaultvalues as well so that the 
                    // array length matches the # of properties
                    // TODO: is stringBuilder faster for appending?
                    if (string.IsNullOrEmpty(result) == false)
                        result += keymath.ParseHelper.English.XMLAttributeNestedDelimiter;
                    result += WriteXMLAttribute(properties[i]);

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Helpers.ExtensionMethods.CustomPropertyValuesToString() - ERROR: serializing '" + properties[i].Name + "'" + ex.Message);
                }
            }

            return result;
        }


        //public static Settings.PropertySpec[] CustomPropertiesFromString(string value)
        //{
        //    Settings.PropertySpec[] result = null;
        //    if (string.IsNullOrEmpty(value)) return result;

        //    try
        //    {
        //        string[] propStrings = value.Split(new string[] { "," }, StringSplitOptions.None);
        //        if (propStrings == null || propStrings.Length == 0) return null;

        //        result = new Settings.PropertySpec[propStrings.Length];
        //        for (int i = 0; i < propStrings.Length; i++)
        //        {
        //            string[] keyValuePair = propStrings[i].Split(new string[] { "=" }, StringSplitOptions.None);
        //            result[i] = new Settings.PropertySpec(keyValuePair[0], keyValuePair[1]);
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}



        

        #region XML attributes
        /// <summary>
        /// Write value to string.
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="xmlNode"></param>
        public static void WriteXMLAttribute(this PropertySpec spec, System.Xml.XmlNode xmlNode)
        {
            string result = WriteXMLAttribute(spec);

            // do not write the attribute string if it is empty
            if (string.IsNullOrEmpty(result)) return;

            CreateAttribute(xmlNode, spec.Name, result);

        }

        private static XmlAttribute CreateAttribute(XmlNode node, string attributeName, string value)
        {
            XmlDocument doc = node.OwnerDocument;
            XmlAttribute attr = null;
            // create new attribute
            attr = doc.CreateAttribute(attributeName);
            attr.Value = value;
            // link attribute to node
            node.Attributes.SetNamedItem(attr);
            return attr;
        }


        public static string WriteXMLAttribute(PropertySpec spec)
        {
            if (spec.DefaultValue == null) return ""; // change from null to "" March.12.2024

            ushort typeID = GetTypeIDFromTypename(spec.TypeName);
            object value = spec.DefaultValue;
            string result = null;

            switch (typeID)
            {
                case TYPE_PROPERTY_SPEC_ARRAY:
                    if (value != null)
                    {
                        //    // hack. use for array of PropertySpec[] such as for Custom Properties
                        //    // or Shader Parameters.  
                        //    result = CustomPropertyValuesToString((Settings.PropertySpec[])spec.DefaultValue);
                    }
                    break;
                case TYPE_PROPERTY_SPEC_ARRAY_MULTI:
                    if (value != null)
                    {
                        //    // hack. use for array of PropertySpec[,] such as for Custom Properties
                        //    // or Shader Parameters.  
                        //    result = CustomPropertyValuesToString((Settings.PropertySpec[,])spec.DefaultValue);
                    }
                    break;
                case TYPE_STRING_ARRAY:
                    if (value != null)
                    {
                        string[] tmpStringArray = (string[])value;
                        char delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars[1];
                        result = string.Empty;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(result);
                        for (int i = 0; i < tmpStringArray.Length; i++)
                        {
                            sb.Append(tmpStringArray[i]);
                            if (i != tmpStringArray.Length - 1)
                                // append delimiter
                                sb.Append(delimiter);
                        }
                        result = sb.ToString();
                    }
                    break;
                case TYPE_JAGGED_STRING_ARRAY:
                    throw new NotImplementedException("ExtensionMethods.WriteXMLAttribute - ERROR: Jagged String Array Write not implemented.");
                    break;
                case TYPE_BYTE_2D_ARRAY:
                    throw new NotImplementedException("ExtensionMethods.WriteXMLAttribute - ERROR: Byte 2D Array Write not implemented.");
                    // http://www.codeproject.com/Articles/80289/Saving-Image-Data-in-an-XML-File?msg=3468583#xx3468583xx
                    // Convert.ToBase64String() and Convert.FromBase64String()
                    //result =  
                    break;
                case TYPE_UINT32_ARRAY:
                    if (value != null)
                    {
                        char delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars[1];
                        result = string.Empty;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(result);
                        //						
                        uint[] tmpUIntArray = (uint[])value;

                        for (int i = 0; i < tmpUIntArray.Length; i++)
                        {
                            sb.Append(tmpUIntArray[i].ToString());
                            if (i != tmpUIntArray.Length - 1)
                                // append delimiter. 
                                sb.Append(delimiter);
                        }
                        result = sb.ToString();
                    }
                    break;
                case TYPE_INT32_ARRAY:
                    if (value != null)
                    {
                        char delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars[1];
                        result = string.Empty;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(result);
                        //						
                        int[] tmpIntArray = (int[])value;

                        for (int i = 0; i < tmpIntArray.Length; i++)
                        {
                            sb.Append(tmpIntArray[i].ToString());
                            if (i != tmpIntArray.Length - 1)
                                // append dilimeter
                                sb.Append(delimiter);
                        }
                        result = sb.ToString();
                    }
                    break;
                case TYPE_INT32_2D_ARRAY:
                    // http://www.codeproject.com/Articles/80289/Saving-Image-Data-in-an-XML-File?msg=3468583#xx3468583xx
                    // Convert.ToBase64String() and Convert.FromBase64String()
                    // x dimension size
                    // z dimension size
                    // data
                    if (value != null)
                    {
                        int[,] tmp = (int[,])value;
                        int sizeX = tmp.GetLength(0);
                        int sizeZ = tmp.GetLength(1);
                        byte[] data = new byte[8 + sizeX * sizeZ * 4];
                        // little endian of course
                        byte[] block1 = BitConverter.GetBytes(sizeX);
                        byte[] block2 = BitConverter.GetBytes(sizeZ);
                        int index = 0;
                        Array.Copy(block1, data, 4); index += 4;
                        Array.Copy(block2, 0, data, 4, 4); index += 4;

                        for (int x = 0; x < sizeX; x++)
                            for (int z = 0; z < sizeZ; z++)
                            {
                                byte[] bytes = BitConverter.GetBytes(tmp[x, z]);
                                Array.Copy(bytes, 0, data, index, 4);
                                index += 4;
                            }

                        result = Convert.ToBase64String(data);
                    }
                    break;
                case TYPE_FLOAT_ARRAY:
                    if (value != null)
                    {
                        char delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars[1];
                        result = string.Empty;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(result);

                        float[] tmpFloatArray = (float[])value;

                        for (int i = 0; i < tmpFloatArray.Length; i++)
                        {
                            sb.Append(tmpFloatArray[i].ToString());
                            if (i != tmpFloatArray.Length - 1)
                                sb.Append(delimiter);
                        }
                        result = sb.ToString();
                    }
                    break;
                case TYPE_JAGGED_FLOAT_ARRAY:
                    if (value != null)
                    {
                        float[][] tmp = (float[][])value;
                        // each jagged subscript can be of arbitrary size
                        // so at start of each, we need to know and write each of those sizes
                        // so that we can parse it and reassemble later.
                        int size0 = tmp.Length;
                        int headerLength = 4;

                        int[] size1 = new int[size0];
                        int dataLength = 0;
                        for (int i = 0; i < size0; i++)
                        {
                            size1[i] = tmp[i].Length;
                            dataLength += 4; // +4 bytes for the actual jagged dimension length
                            dataLength += size1[i] * 4;
                        }


                        byte[] data = new byte[headerLength + dataLength];
                        int index = 0;
                        // write first dimension size - little endian of course
                        byte[] block = BitConverter.GetBytes(size0);
                        Array.Copy(block, data, 4); index += 4;

                        for (int i = 0; i < size0; i++)
                        {
                            // write size of this jagged dimension
                            block = BitConverter.GetBytes(size1[i]);
                            Array.Copy(block, 0, data, index, 4); index += 4;

                            // write data for this jagged dimension
                            for (int j = 0; j < size1[i]; j++)
                            {
                                byte[] block2 = BitConverter.GetBytes(tmp[i][j]);
                                Array.Copy(block2, 0, data, index, 4); index += 4;
                            }
                        }

                        // create a single base64 encoded string so that writing to XML is not a confusing mess with
                        // weird delimiters to break up the subscripts and elements
                        result = Convert.ToBase64String(data);
                    }
                    break;
                case TYPE_VECTOR3_D_ARRAY:
                    result = Keystone.Types.Vector3d.ToString((Keystone.Types.Vector3d[])value);
                    break;

                case TYPE_QUATERNION_ARRAY:
                    result = Keystone.Types.Quaternion.ToString((Keystone.Types.Quaternion[])value);
                    break;

                // types that implement .ToString() are easy
                case TYPE_STRING:
                case TYPE_BOOL:
                case TYPE_BYTE:
                case TYPE_INT32:
                case TYPE_UINT32:
                case TYPE_DOUBLE:
                case TYPE_FLOAT:
                case TYPE_VECTOR2_F:
                case TYPE_VECTOR3_I:
                case TYPE_VECTOR3_F:
                case TYPE_VECTOR3_D:
                case TYPE_QUATERNION:
                case TYPE_COLOR:
                case TYPE_BOUNDING_BOX:
                    result = value.ToString();
                    break;
                case TYPE_RECTANGLE_F_ARRAY:
                    {
                        char delimiter = keymath.ParseHelper.English.XMLAttributeNestedDelimiterChars[0];
                        result = string.Empty;
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(result);

                        System.Drawing.RectangleF[] tmpRectangleFArray = (System.Drawing.RectangleF[])value;

                        for (int i = 0; i < tmpRectangleFArray.Length; i++)
                        {
                            // rectangleF format is x,y,width,height
                            string rectToString = RectangleFToString(tmpRectangleFArray[i]);
                            sb.Append(rectToString);

                            if (i != tmpRectangleFArray.Length - 1)
                                sb.Append(delimiter);
                        }
                        result = sb.ToString();
                    }
                    break;

                    result = null;
                    break;
                default:
                    // HACK: return null for unknown types 
                    //case  "Production[]":
                    //case "HelmState":
                    //case "NavPoint":
                    //case "SensorContact[]":
                    //throw new Exception("Helpers.ExtensionMethods.WriteXMLAttribute() - ERROR: unsupported array type. '" + spec.DefaultValue.GetType().Name);
                    return null;

            }

            return result;
        }

        private static string RectangleFToString(System.Drawing.RectangleF rect)
        {
            char delimiter = keymath.ParseHelper.English.XMLAttributeDelimiterChars[1];
            string result = rect.X.ToString() + delimiter + rect.Y.ToString() + delimiter + rect.Width.ToString() + delimiter + rect.Height.ToString();
            return result;
        }
        /// <summary>
        /// Parse value from XML string.
        /// </summary>
        /// <param name="spec"></param>
        /// <param name="value"></param>
        public static void ReadXMLAttribute(this PropertySpec spec, string value)
        {
            spec.DefaultValue = ReadXMLAttribute(spec.TypeName, value);
        }

        /// <summary>
        /// Parse value from string.
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object ReadXMLAttribute(string typeName, string value)
        {
            object result = null;

            ushort typeID = GetTypeIDFromTypename(typeName);

            switch (typeID)
            {
                case TYPE_STRING:
                    result = value;
                    break;

                case TYPE_STRING_ARRAY:
                    if (string.IsNullOrEmpty(value) == false)
                    {
                        char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
                        string[] values = value.Split(delimiterChars);
                        result = values;
                    }
                    break;
                case TYPE_JAGGED_STRING_ARRAY:
                    throw new NotImplementedException("Keystone.Helpers.Extensionmethods.ReadXMLAttribute() - Jagged String Array not yet implemented.");
                    break;
                case TYPE_BOOL:
                    result = bool.Parse(value);
                    break;

                case TYPE_BYTE:
                    result = byte.Parse(value);
                    break;
                case TYPE_BYTE_2D_ARRAY:
                    throw new NotImplementedException();
                    // http://www.codeproject.com/Articles/80289/Saving-Image-Data-in-an-XML-File?msg=3468583#xx3468583xx
                    // Convert.ToBase64String() and Convert.FromBase64String()
                    //result =  
                    break;
                case TYPE_INT32:
                    result = Int32.Parse(value);
                    break;
                case TYPE_UINT32_ARRAY:
                    {
                        if (string.IsNullOrEmpty(value) == false)
                        {
                            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
                            string[] values = value.Split(delimiterChars);
                            if (values == null || values.Length == 0) throw new ArgumentException();

                            int arraySize = values.Length;
                            uint[] results = new uint[arraySize];

                            for (int i = 0; i < results.Length; i++)
                                results[i] = uint.Parse(values[i]);

                            result = results;
                        }
                    }
                    break;
                case TYPE_INT32_ARRAY:
                    {
                        if (string.IsNullOrEmpty(value) == false)
                        {
                            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
                            string[] values = value.Split(delimiterChars);
                            if (values == null || values.Length == 0) throw new ArgumentException();

                            int arraySize = values.Length;
                            int[] results = new int[arraySize];

                            for (int i = 0; i < results.Length; i++)
                                results[i] = int.Parse(values[i]);

                            result = results;
                        }
                    }
                    break;
                case TYPE_INT32_2D_ARRAY:
                    {
                        // http://www.codeproject.com/Articles/80289/Saving-Image-Data-in-an-XML-File?msg=3468583#xx3468583xx
                        // Convert.ToBase64String() and Convert.FromBase64String()
                        byte[] tmp = Convert.FromBase64String((string)value);
                        int index = 0;
                        int sizeX = BitConverter.ToInt32(tmp, index); index += 4;
                        int sizeZ = BitConverter.ToInt32(tmp, index); index += 4;
                        int[,] data = new int[sizeX, sizeZ];
                        for (int x = 0; x < sizeX; x++)
                            for (int z = 0; z < sizeZ; z++)
                            {
                                int integer = BitConverter.ToInt32(tmp, index);
                                data[x, z] = integer;
                                index += 4;
                            }

                        result = data;
                    }
                    break;
                case TYPE_UINT32:
                    result = UInt32.Parse(value);
                    break;

                case TYPE_DOUBLE:
                    {
                        double tmp;
                        // if double.MaxValue.ToString() is used, the value will overflow when trying to parse
                        if (!double.TryParse(value, out tmp))
                        {
                            result = value.Equals(double.MaxValue) ? double.MaxValue : double.MinValue;
                        }
                        else
                            result = (double)tmp;
                        break;
                    }
                case TYPE_FLOAT:
                    result = float.Parse(value);
                    break;

                case TYPE_FLOAT_ARRAY:
                    {
                        if (string.IsNullOrEmpty(value) == false)
                        {
                            char[] delimiterChars = keymath.ParseHelper.English.XMLAttributeDelimiterChars;
                            string[] values = value.Split(delimiterChars);
                            if (values == null || values.Length == 0) throw new ArgumentException();

                            int arraySize = values.Length;
                            float[] results = new float[arraySize];

                            for (int i = 0; i < results.Length; i++)
                                results[i] = float.Parse(values[i]);

                            result = results;
                        }
                    }
                    break;
                case TYPE_JAGGED_FLOAT_ARRAY:
                    {
                        int index = 0;
                        byte[] tmp = Convert.FromBase64String((string)value);
                        // outter size
                        int size0 = BitConverter.ToInt32(tmp, index); index += 4;
                        float[][] data = new float[size0][];

                        for (int x = 0; x < size0; x++)
                        {
                            // read size of this jagged dimension
                            int size1 = BitConverter.ToInt32(tmp, index); index += 4;
                            data[x] = new float[size1];

                            // read data of this jagged dimension
                            for (int z = 0; z < size1; z++)
                            {
                                float val = BitConverter.ToSingle(tmp, index);
                                data[x][z] = val;
                                index += 4;
                            }
                        }
                        result = data;
                    }
                    break;
                case TYPE_VECTOR2_F:
                    result = Keystone.Types.Vector2f.Parse(value);
                    break;
                case TYPE_VECTOR3_I:
                    result = Keystone.Types.Vector3i.Parse(value);
                    break;
                case TYPE_VECTOR3_F:
                    result = Keystone.Types.Vector3f.Parse(value);
                    break;
                case TYPE_VECTOR3_D:
                    result = Keystone.Types.Vector3d.Parse(value);
                    break;

                case TYPE_VECTOR3_D_ARRAY:
                    result = Keystone.Types.Vector3d.ParseArray(value);
                    break;

                case TYPE_QUATERNION:
                    result = Keystone.Types.Quaternion.Parse(value);
                    break;

                case TYPE_QUATERNION_ARRAY:
                    result = Keystone.Types.Quaternion.ParseArray(value);
                    break;

                case TYPE_COLOR:
                    result = Keystone.Types.Color.Parse(value);
                    break;

                case TYPE_BOUNDING_BOX:
                    result = Keystone.Types.BoundingBox.Parse(value);
                    break;
                case TYPE_RECTANGLE_F_ARRAY:
                    {
                        if (string.IsNullOrEmpty(value) == false)
                        {
                            char nestedDelimiterChar = keymath.ParseHelper.English.XMLAttributeNestedDelimiter[0];
                            string[] values = value.Split(nestedDelimiterChar);
                            if (values == null || values.Length == 0) throw new ArgumentException();

                            int arraySize = values.Length;
                            System.Drawing.RectangleF[] results = new System.Drawing.RectangleF[arraySize];

                            for (int i = 0; i < results.Length; i++)
                            {
                                float x = 0;
                                float y = 0;
                                float w = 0;
                                float h = 0;
                               

                                string[] sa = values[i].Split(keymath.ParseHelper.English.XMLAttributeDelimiterChars);

                                if (sa.Length == 4 && float.TryParse(sa[0], out x) && float.TryParse(sa[1], out y) && float.TryParse(sa[2], out w) && float.TryParse(sa[3], out h))
                                {
                                    results[i] = new System.Drawing.RectangleF(x, y, w, h);
                                }
                                else
                                {
                                    results[i] = new System.Drawing.RectangleF(0, 0, 0, 0);
                                }
                            }
                            result = results;
                        }
                    }
                    break;
                // OBSOLETE - Following should return int's and rely on caller
                //            to cast int result to proper type
                //case "SceneType":
                //    result = (Scene.SceneBase.SceneType)int.Parse(value);
                //    break;
                //case "EntityFlags":
                //    //result = Settings.EnumHelper.EnumeratedMemberValue(typeof(Keystone.Entities.EntityBase.EntityFlags), value);

                //    // for the follwing (CAST)int.Parse to work, the enum must be written to the
                //    // propertyresult as  result = (int)value;
                //    // and NOT simply as value.ToString()
                //    result = (KeyCommon.Flags.EntityFlags)int.Parse(value);
                //    break;

                //case "CONST_TV_DETAILMAP_MODE":
                //    result = (MTV3D65.CONST_TV_DETAILMAP_MODE)int.Parse(value);
                //    break;

                //case "CONST_TV_LANDSCAPE_PRECISION":
                //    result = (MTV3D65.CONST_TV_LANDSCAPE_PRECISION)int.Parse(value);
                //    break;

                //case "CONST_TV_LANDSCAPE_AFFINE":
                //    result = (MTV3D65.CONST_TV_LANDSCAPE_AFFINE)int.Parse(value);
                //    break;

                //case "CONST_TV_ACTORMODE":
                //    result = (MTV3D65.CONST_TV_ACTORMODE)int.Parse(value);
                //    break;
                //case "CONST_TV_MESHFORMAT":
                //    //result = Settings.EnumHelper.EnumeratedMemberValue(typeof(MTV3D65.CONST_TV_MESHFORMAT), value);
                //    result = (MTV3D65.CONST_TV_MESHFORMAT)int.Parse(value);
                //    break;
                //case "CONST_TV_BILLBOARDTYPE":
                //    result = (MTV3D65.CONST_TV_BILLBOARDTYPE)int.Parse(value);
                //    break;
                //case "CONST_TV_CULLING":
                //    //result = Settings.EnumHelper.EnumeratedMemberValue(typeof(MTV3D65.CONST_TV_CULLING), value);
                //    result = (MTV3D65.CONST_TV_CULLING)int.Parse(value);
                //    break;
                //case "CONST_TV_BLENDINGMODE":
                //    //result = Settings.EnumHelper.EnumeratedMemberValue(typeof(MTV3D65.CONST_TV_BLENDINGMODE), value);
                //    result = (MTV3D65.CONST_TV_BLENDINGMODE)int.Parse(value);
                //    break;
                //case "CONST_TV_LIGHTINGMODE":
                //    //result = Settings.EnumHelper.EnumeratedMemberValue(typeof(MTV3D65.CONST_TV_LIGHTINGMODE), value);
                //    result = (MTV3D65.CONST_TV_LIGHTINGMODE)int.Parse(value);
                //    break;

                //case "LUMINOSITY":
                //    result = (Keystone.Celestial.LUMINOSITY)int.Parse(value);
                //    break;
                //case "SPECTRAL_TYPE":
                //    result = (Keystone.Celestial.SPECTRAL_TYPE)int.Parse(value);
                //    break;
                //case "SPECTRAL_SUB_TYPE":
                //    result = (Keystone.Celestial.SPECTRAL_SUB_TYPE)int.Parse(value);
                //    break;
                //case "WorldType":
                //    result = (Keystone.Celestial.WorldType)int.Parse(value);
                //    break;
                default:
                    //throw new Exception("ExtensionMethods.PropertySpec.Read(buffer) -- unexpected variable type '" + typeName + "'");
                    System.Diagnostics.Debug.WriteLine("Keystone.Helpers.ExtensionMethods() - ERROR: unknown type '" + typeName + "'");
                    break;
            }

            return result;
        }

#endregion // end Read/Write XML Attributes


        public static void Read(this PropertySpec spec, NetBuffer buffer)
        {
            spec.Name = buffer.ReadString();
            string typeName;

            object result = ReadType(buffer, out typeName);

            spec.TypeName = typeName;
            spec.DefaultValue = result;
        }

        public static object ReadType(NetBuffer buffer, out string typeName)
        {
            object result = null;
            // TODO: note the full blown typename is stored here!
            // we need to use a byte code
            ushort typeID = buffer.ReadUInt16();

            // we must restore the typename (doesn't have to be fully qualified)
            typeName = GetTypenameFromTypeID(typeID);


            // based on type i think i have to determine how to parse the default value
            switch (typeID)
            {
                case TYPE_PROPERTY_SPEC:
                    // TODO: untested
                    PropertySpec spec = new PropertySpec();
                    spec.Read(buffer);
                    result = spec;
                    break;

                case TYPE_PROPERTY_SPEC_ARRAY:
                    {
                        // TODO: untested
                        uint count = buffer.ReadUInt32();
                        PropertySpec[] specs = new PropertySpec[count];
                        for (int i = 0; i < count; i++)
                        {

                            specs[i].Read(buffer);
                        }
                        result = specs;
                    }
                    break;
                case TYPE_PROPERTY_SPEC_ARRAY_MULTI:
                    break;
                case TYPE_PARTICLE_KEYFRAME_ARRAY:
                    {
                        
                        Keystone.KeyFrames.ParticleKeyframe[] keyframes = null;
                        uint count = buffer.ReadUInt32();
                        if (count > 0)
                        {
                            keyframes = new Keystone.KeyFrames.ParticleKeyframe[count];
                                                         
                            for (uint i = 0; i < count; i++)
                            {
                                keyframes[i].Key = buffer.ReadFloat();
                                Keystone.Types.Color color;
                                color.r = buffer.ReadFloat();
                                color.g = buffer.ReadFloat();
                                color.b = buffer.ReadFloat();
                                color.a = buffer.ReadFloat();
                                keyframes[i].Color = color;

                                Keystone.Types.Vector3f v;
                                v.x = buffer.ReadFloat();
                                v.y = buffer.ReadFloat();
                                v.z = buffer.ReadFloat();
                                keyframes[i].Size = v;

                                v.x = buffer.ReadFloat();
                                v.y = buffer.ReadFloat();
                                v.z = buffer.ReadFloat();

                                keyframes[i].Rotation = v;
                            }
                        }
                        result = keyframes;
                        break;
                    }
                case TYPE_EMITTER_KEYFRAME_ARRAY:
                    {
       
                        Keystone.KeyFrames.EmitterKeyframe[] keyframes = null;
                        uint count = buffer.ReadUInt32();
                        if (count > 0)
                        {
                            keyframes = new Keystone.KeyFrames.EmitterKeyframe[count];
                            for (uint i = 0; i < count; i++)
                            {

                                keyframes[i].Key = buffer.ReadFloat();
                                Keystone.Types.Color color;
                                color.r = buffer.ReadFloat();
                                color.g = buffer.ReadFloat();
                                color.b = buffer.ReadFloat();
                                color.a = buffer.ReadFloat();
                                keyframes[i].Color = color;

                                Keystone.Types.Vector3f v;
                                v.x = buffer.ReadFloat();
                                v.y = buffer.ReadFloat();
                                v.z = buffer.ReadFloat();
                                keyframes[i].LocalPosition = v;

                                v.x = buffer.ReadFloat();
                                v.y = buffer.ReadFloat();
                                v.z = buffer.ReadFloat();
                                keyframes[i].MainDirection = v;

                                v.x = buffer.ReadFloat();
                                v.y = buffer.ReadFloat();
                                v.z = buffer.ReadFloat();
                                keyframes[i].BoxSize = v;

                                keyframes[i].Lifetime = buffer.ReadFloat();
                                keyframes[i].Power = buffer.ReadFloat();
                                keyframes[i].Radius = buffer.ReadFloat();
                                keyframes[i].Speed = buffer.ReadFloat();
                            }
                        }
                        result = keyframes;
                        break;
                    }
                case TYPE_STRING:
                    result = buffer.ReadString();
                    break;

                case TYPE_STRING_ARRAY:
                    {
                        uint count = buffer.ReadUInt32();
                        if (count == 0) return null;
                        string[] stringResult = new string[count];
                        for (uint i = 0; i < count; i++)
                            stringResult[i] = buffer.ReadString();

                        result = stringResult;
                    }
                    break;
                case TYPE_JAGGED_STRING_ARRAY:
                    throw new NotImplementedException("Keystone.Helpers.Extensionmethods.ReadType() - Jagged String Array not yet implemented.");
                    break;
                case TYPE_BOOL:
                    result = buffer.ReadBoolean();
                    break;

                case TYPE_BYTE:
                    result = buffer.ReadByte();
                    break;
                case TYPE_BYTE_2D_ARRAY:
                    uint byte2d_size0 = buffer.ReadUInt32();
                    uint byte2d_size1 = buffer.ReadUInt32();
                    if (byte2d_size0 > 0 && byte2d_size1 > 0)
                    {
                        byte[,] value = new byte[byte2d_size0, byte2d_size1];
                        for (uint i = 0; i < byte2d_size0; i++)
                            for (uint j = 0; j < byte2d_size1; j++)
                                value[i, j] = buffer.ReadByte();

                        result = value;
                    }
                    break;
                case TYPE_INT32:
                    result = buffer.ReadInt32();
                    break;
                case TYPE_INT32_2D_ARRAY:
                    uint int2d_size0 = buffer.ReadUInt32();
                    uint int2d_size1 = buffer.ReadUInt32();
                    if (int2d_size0 > 0 && int2d_size1 > 0)
                    {
                        int[,] value = new int[int2d_size0, int2d_size1];
                        for (uint i = 0; i < int2d_size0; i++)
                            for (uint j = 0; j < int2d_size1; j++)
                                value[i, j] = buffer.ReadInt32();

                        result = value;
                    }
                    break;
                case TYPE_INT32_ARRAY:
                    uint int32count = buffer.ReadUInt32();
                    if (int32count > 0)
                    {
                        int[] value = new int[int32count];
                        for (uint i = 0; i < int32count; i++)
                        {
                            value[i] = buffer.ReadInt32();
                        }
                        result = value;
                    }
                    break;

                case TYPE_INT64:
                    result = buffer.ReadInt64();
                    break;

                case TYPE_UINT32:
                    result = buffer.ReadUInt32();
                    break;
                case TYPE_UINT32_ARRAY:
                    uint uint32count = buffer.ReadUInt32();
                    if (uint32count > 0)
                    {
                        uint[] value = new uint[uint32count];
                        for (uint i = 0; i < uint32count; i++)
                        {
                            value[i] = buffer.ReadUInt32();
                        }
                        result = value;
                    }
                    break;
                case TYPE_UINT64_ARRAY:
                    uint uint64count = buffer.ReadUInt32();
                    if (uint64count > 0)
                    {
                        UInt64[] value = new UInt64[uint64count];
                        for (uint i = 0; i < uint64count; i++)
                        {
                            value[i] = buffer.ReadUInt64();
                        }
                        result = value;
                    }
                    break;
                case TYPE_DOUBLE:
                    result = buffer.ReadDouble();
                    break;

                case TYPE_FLOAT:
                    result = buffer.ReadFloat();
                    break;
                case TYPE_FLOAT_ARRAY:
                    uint floatCount = buffer.ReadUInt32();
                    if (floatCount > 0)
                    {
                        float[] value = new float[floatCount];
                        for (uint i = 0; i < floatCount; i++)
                        {
                            value[i] = buffer.ReadFloat();
                        }
                        result = value;
                    }
                    break;
                case TYPE_VECTOR2_F:
                    float fx, fy;
                    fx = buffer.ReadFloat();
                    fy = buffer.ReadFloat();
                    result = new Keystone.Types.Vector2f(fx, fy);
                    break;

                case TYPE_VECTOR3_I:
                    int vIx, vIy, vIz;
                    vIx = buffer.ReadInt32();
                    vIy = buffer.ReadInt32();
                    vIz = buffer.ReadInt32();
                    result = new Keystone.Types.Vector3i(vIx, vIy, vIz);
                    break;


                case TYPE_VECTOR3_F:
                    float vfx, vfy, vfz;
                    vfx = buffer.ReadSingle();
                    vfy = buffer.ReadSingle();
                    vfz = buffer.ReadSingle();
                    result = new Keystone.Types.Vector3f(vfx, vfy, vfz);
                    break;

                case TYPE_VECTOR3_D:
                    double x, y, z;
                    x = buffer.ReadDouble();
                    y = buffer.ReadDouble();
                    z = buffer.ReadDouble();
                    result = new Keystone.Types.Vector3d(x, y, z);
                    break;

                case TYPE_VECTOR3_D_ARRAY:
                    {
                        uint count = buffer.ReadUInt32();
                        if (count > 0)
                        {
                            Keystone.Types.Vector3d[] value = new Keystone.Types.Vector3d[count];
                            for (uint i = 0; i < count; i++)
                            {
                                value[i].x = buffer.ReadDouble();
                                value[i].y = buffer.ReadDouble();
                                value[i].z = buffer.ReadDouble();
                            }
                            result = value;
                        }
                    }
                    break;
                case TYPE_QUATERNION:
                    double xx, yy, zz, ww;
                    xx = buffer.ReadDouble();
                    yy = buffer.ReadDouble();
                    zz = buffer.ReadDouble();
                    ww = buffer.ReadDouble();
                    result = new Keystone.Types.Quaternion(xx, yy, zz, ww);
                    break;

                case TYPE_QUATERNION_ARRAY:
                    {
                        uint count = buffer.ReadUInt32();
                        if (count > 0)
                        {
                            Keystone.Types.Quaternion[] value = new Keystone.Types.Quaternion[count];
                            for (uint i = 0; i < count; i++)
                            {
                                value[i] = new Keystone.Types.Quaternion();
                                value[i].X = buffer.ReadDouble();
                                value[i].Y = buffer.ReadDouble();
                                value[i].Z = buffer.ReadDouble();
                                value[i].W = buffer.ReadDouble();
                            }
                            result = value;
                        }
                    }
                    break;

                case TYPE_MATRIX:
                    throw new NotImplementedException();
                    break;

                case TYPE_COLOR:
                    float r, g, b, a;
                    r = buffer.ReadFloat();
                    g = buffer.ReadFloat();
                    b = buffer.ReadFloat();
                    a = buffer.ReadFloat();
                    result = new Keystone.Types.Color(r, g, b, a);
                    break;

                case TYPE_BOUNDING_BOX:
                    Keystone.Types.Vector3d min, max;
                    bool notNull = buffer.ReadBoolean();
                    if (notNull)
                    {
                        min.x = buffer.ReadDouble();
                        min.y = buffer.ReadDouble();
                        min.z = buffer.ReadDouble();
                        max.x = buffer.ReadDouble();
                        max.y = buffer.ReadDouble();
                        max.z = buffer.ReadDouble();
                        result = new Keystone.Types.BoundingBox(min, max);
                    }
                    break;
                default:
                    if (mUserTypeReader == null) throw new Exception("ExtensionMethods.PropertySpec.Read(buffer) - ERROR: unexpected variable type '" + typeName + "'"); 
                    result = mUserTypeReader(buffer, typeID, out typeName);
                    if (result == null) throw new Exception("ExtensionMethods.PropertySpec.Read(buffer) - ERROR: unexpected variable type '" + typeName + "'");
                    break;
            }

            return result;
        }

        public static void WriteType(NetBuffer buffer, object value)
        {
            if (value == null) throw new ArgumentNullException();
            string typeName = value.GetType().Name;
            Write(buffer, value, typeName);
        }

        public static void Write(this PropertySpec spec, NetBuffer buffer)
        {
            buffer.Write(spec.Name);
            Write(buffer, spec.DefaultValue, spec.TypeName);
        }



        private static void Write(NetBuffer buffer, object value, string typeName)
        {
            ushort typeID = GetTypeIDFromTypename(typeName);

            // we dont write the actual typename, but a ushort(uint16) code instead
            buffer.Write(typeID);

            // cast to proper type and write
            switch (typeID)
            {
                case TYPE_PROPERTY_SPEC:
                    Write((Settings.PropertySpec)value, buffer);
                    break;
                case TYPE_PROPERTY_SPEC_ARRAY:
                    {
                        // TODO: untested
                        uint count = 0;
                        Settings.PropertySpec[] properties = null;
                        if (value != null)
                        {
                            properties = (Settings.PropertySpec[])value;
                            count = (uint)properties.Length;
                        }

                        buffer.Write(count);

                        for (int i = 0; i < count; i++)
                            Write(properties[i], buffer);
                    }
                    break;
                case TYPE_PROPERTY_SPEC_ARRAY_MULTI:
                {

                }
                    break;
                case TYPE_PARTICLE_KEYFRAME_ARRAY:
                    {
                        object tmpValue = value;
                        Keystone.KeyFrames.ParticleKeyframe[] keyframes = null;
                        uint count = 0;
                        if (tmpValue != null)
                        {
                            keyframes = (Keystone.KeyFrames.ParticleKeyframe[])value;
                            count = (uint)keyframes.Length;
                        }
                        buffer.Write(count);
                        if (count > 0)
                            for (uint i = 0; i < count; i++)
                            {
                                buffer.Write(keyframes[i].Key);
                                buffer.Write(keyframes[i].Color.r);
                                buffer.Write(keyframes[i].Color.g);
                                buffer.Write(keyframes[i].Color.b);
                                buffer.Write(keyframes[i].Color.a);
                                buffer.Write(keyframes[i].Size.x);
                                buffer.Write(keyframes[i].Size.y);
                                buffer.Write(keyframes[i].Size.z);
                                buffer.Write(keyframes[i].Rotation.x);
                                buffer.Write(keyframes[i].Rotation.y);
                                buffer.Write(keyframes[i].Rotation.z);
                            }
                        break;
                    }
                case TYPE_EMITTER_KEYFRAME_ARRAY:
                    {
                        object tmpValue = value;
                        Keystone.KeyFrames.EmitterKeyframe[] keyframes = null;
                        uint count = 0;
                        if (tmpValue != null)
                        {
                            keyframes = (Keystone.KeyFrames.EmitterKeyframe[])value;
                            count = (uint)keyframes.Length;
                        }
                        buffer.Write(count);
                        if (count > 0)
                            for (uint i = 0; i < count; i++)
                            {
                                buffer.Write(keyframes[i].Key);
                                buffer.Write(keyframes[i].Color.r);
                                buffer.Write(keyframes[i].Color.g);
                                buffer.Write(keyframes[i].Color.b);
                                buffer.Write(keyframes[i].Color.a);
                                buffer.Write(keyframes[i].LocalPosition.x);
                                buffer.Write(keyframes[i].LocalPosition.y);
                                buffer.Write(keyframes[i].LocalPosition.z);
                                buffer.Write(keyframes[i].MainDirection.x);
                                buffer.Write(keyframes[i].MainDirection.y);
                                buffer.Write(keyframes[i].MainDirection.z);
                                buffer.Write(keyframes[i].BoxSize.x);
                                buffer.Write(keyframes[i].BoxSize.y);
                                buffer.Write(keyframes[i].BoxSize.z);
                                buffer.Write(keyframes[i].Lifetime);
                                buffer.Write(keyframes[i].Power);
                                buffer.Write(keyframes[i].Radius);
                                buffer.Write(keyframes[i].Speed);
                            }
                        break;
                    }
                case TYPE_STRING:
                    buffer.Write((string)value);
                    break;
                case TYPE_STRING_ARRAY:
                    {
                        string[] results = (string[])value;
                        if (results == null)
                            buffer.Write(0);
                        else
                            buffer.Write(results.Length);

                        for (uint i = 0; i < results.Length; i++)
                            buffer.Write(results[i]);
                    }
                    break;
                case TYPE_JAGGED_STRING_ARRAY:
                    break;

                case TYPE_BOOL:
                    buffer.Write((bool)value);
                    break;

                case TYPE_BYTE:
                    buffer.Write((byte)value);
                    break;
                case TYPE_BYTE_2D_ARRAY:
                    byte[,] byte2DArray = null;
                    uint byte2d_size0 = 0;
                    uint byte2d_size1 = 0;
                    object tmpByte2DArray = value;
                    if (tmpByte2DArray != null)
                    {
                        byte2DArray = (byte[,])value;
                        byte2d_size0 = (uint)byte2DArray.GetLength(0);
                        byte2d_size1 = (uint)byte2DArray.GetLength(1);
                    }
                    buffer.Write(byte2d_size0);
                    buffer.Write(byte2d_size1);
                    if (byte2d_size0 > 0 && byte2d_size1 > 0)
                        for (uint i = 0; i < byte2d_size0; i++)
                            for (uint j = 0; j < byte2d_size1; j++)
                                buffer.Write(byte2DArray[i, j]);

                    break;
                case TYPE_INT32:
                    buffer.Write((int)value);
                    break;
                case TYPE_INT32_ARRAY:
                    int[] int32Array = null;
                    uint int32count = 0;
                    object tmpInt32Array = value;
                    if (tmpInt32Array != null)
                    {
                        int32Array = (int[])value;
                        int32count = (uint)int32Array.Length;
                    }
                    buffer.Write(int32count);
                    if (int32count > 0)
                        for (uint i = 0; i < int32count; i++)
                        {
                            buffer.Write(int32Array[i]);
                        }
                    break;
                case TYPE_INT32_2D_ARRAY:
                    {
                        int[,] int2DArray = null;
                        uint int2d_size0 = 0;
                        uint int2d_size1 = 0;
                        object tmpInt2DArray = value;
                        if (tmpInt2DArray != null)
                        {
                            int2DArray = (int[,])value;
                            int2d_size0 = (uint)int2DArray.GetLength(0);
                            int2d_size1 = (uint)int2DArray.GetLength(1);
                        }
                        buffer.Write(int2d_size0);
                        buffer.Write(int2d_size1);
                        if (int2d_size0 > 0 && int2d_size1 > 0)
                            for (uint i = 0; i < int2d_size0; i++)
                                for (uint j = 0; j < int2d_size1; j++)
                                    buffer.Write(int2DArray[i, j]);
                    }
                    break;
                case TYPE_INT64:
                    buffer.Write((long)value);
                    break;
                case TYPE_UINT32:
                    buffer.Write((UInt32)value);
                    break;
                case TYPE_UINT32_ARRAY:
                    {
                        uint[] uint32Array = null;
                        uint uint32count = 0;
                        object tmpUInt32Array = value;
                        if (tmpUInt32Array != null)
                        {
                            uint32Array = (uint[])value;
                            uint32count = (uint)uint32Array.Length;
                        }
                        buffer.Write(uint32count);
                        if (uint32count > 0)
                            for (uint i = 0; i < uint32count; i++)
                            {
                                buffer.Write(uint32Array[i]);
                            }
                    }
                    break;
                case TYPE_UINT64_ARRAY:
                    {
                        UInt64[] uint64Array = null;
                        UInt64 uint64count = 0;
                        object tmpUInt64Array = value;
                        if (tmpUInt64Array != null)
                        {
                            uint64Array = (UInt64[])value;
                            uint64count = (UInt64)uint64Array.Length;
                        }
                        buffer.Write(uint64count);
                        if (uint64count > 0)
                            for (uint i = 0; i < uint64count; i++)
                            {
                                buffer.Write(uint64Array[i]);
                            }
                    }
                    break;
                case TYPE_DOUBLE:
                    buffer.Write((double)value);
                    break;

                case TYPE_FLOAT:
                    buffer.Write((float)value);
                    break;
                case TYPE_FLOAT_ARRAY:
                    {
                        float[] floatArray = null;
                        uint floatCount = 0;
                        object tmpFloatArray = value;
                        if (tmpFloatArray != null)
                        {
                            floatArray = (float[])value;
                            floatCount = (uint)floatArray.Length;
                        }
                        buffer.Write(floatCount);
                        if (floatCount > 0)
                            for (uint i = 0; i < floatCount; i++)
                            {
                                buffer.Write(floatArray[i]);
                            }
                    }
                    break;
                case TYPE_VECTOR2_F:
                    object tmp2f = value;
                    Keystone.Types.Vector2f v2f = Keystone.Types.Vector2f.Parse(tmp2f.ToString());
                    buffer.Write(v2f.x);
                    buffer.Write(v2f.y);
                    break;
                case TYPE_VECTOR3_I:
                    object tmpVI = value;
                    Keystone.Types.Vector3i vI = Keystone.Types.Vector3i.Parse(tmpVI.ToString());
                    buffer.Write(vI.X);
                    buffer.Write(vI.Y);
                    buffer.Write(vI.Z);
                    break;
                case TYPE_VECTOR3_F:
                    object tmpvf = value;
                    Keystone.Types.Vector3f vf = Keystone.Types.Vector3f.Parse(tmpvf.ToString());
                    buffer.Write(vf.x);
                    buffer.Write(vf.y);
                    buffer.Write(vf.z);
                    break;
                case TYPE_VECTOR3_D:
                    object tmp = value;
                    Keystone.Types.Vector3d v = Keystone.Types.Vector3d.Parse(tmp.ToString());
                    buffer.Write(v.x);
                    buffer.Write(v.y);
                    buffer.Write(v.z);
                    break;
                case TYPE_VECTOR3_D_ARRAY:
                    {
                        Keystone.Types.Vector3d[] vArray = null;
                        uint count = 0;
                        object tmpVectorArray = value;
                        if (tmpVectorArray != null)
                        {
                            vArray = (Keystone.Types.Vector3d[])value;
                            count = (uint)vArray.Length;
                        }
                        buffer.Write(count);
                        if (count > 0)
                            for (uint i = 0; i < count; i++)
                            {
                                buffer.Write(vArray[i].x);
                                buffer.Write(vArray[i].y);
                                buffer.Write(vArray[i].z);
                            }
                    }
                    break;
                case TYPE_QUATERNION:
                    Keystone.Types.Quaternion q = Keystone.Types.Quaternion.Parse(value.ToString());
                    buffer.Write(q.X);
                    buffer.Write(q.Y);
                    buffer.Write(q.Z);
                    buffer.Write(q.W);
                    break;

                case TYPE_QUATERNION_ARRAY:
                    {
                        object tmpValue = value;
                        Keystone.Types.Quaternion[] quats = null;
                        uint count = 0;
                        if (tmpValue != null)
                        {
                            quats = (Keystone.Types.Quaternion[])value;
                            count = (uint)quats.Length;
                        }
                        buffer.Write(count);
                        if (count > 0)
                            for (uint i = 0; i < count; i++)
                            {
                                buffer.Write(quats[i].X);
                                buffer.Write(quats[i].Y);
                                buffer.Write(quats[i].Z);
                                buffer.Write(quats[i].W);
                            }
                        break;
                    }
                case TYPE_MATRIX:
                    throw new NotImplementedException();
                    break;

                case TYPE_COLOR:
                    Keystone.Types.Color color = Keystone.Types.Color.Parse(value.ToString());
                    buffer.Write(color.r);
                    buffer.Write(color.g);
                    buffer.Write(color.b);
                    buffer.Write(color.a);
                    break;

                case TYPE_BOUNDING_BOX:

                    if (value == null)
                        buffer.Write(false);
                    else
                    {
                        Keystone.Types.BoundingBox box = Keystone.Types.BoundingBox.Parse(value.ToString());
                        buffer.Write(true);
                        buffer.Write(box.Min.x);
                        buffer.Write(box.Min.y);
                        buffer.Write(box.Min.z);
                        buffer.Write(box.Max.x);
                        buffer.Write(box.Max.y);
                        buffer.Write(box.Max.z);
                    }
                    break;
                default:
                    if (mUserTypeWriter != null)
                    {
                        if (mUserTypeWriter(buffer, value, typeName))
                            break;
                    }
                    throw new Exception("ExtensionMethods.PropertySpec.Write(buffer) - ERROR: unexpected variable type '" + typeName + "'");
                    break;
            }
        }

        
        #region File I/O
        public static byte[,] Read2DByteArray(System.IO.BinaryReader reader)
        {
            uint byte2d_size0 = reader.ReadUInt32();
            uint byte2d_size1 = reader.ReadUInt32();
            if (byte2d_size0 > 0 && byte2d_size1 > 0)
            {
                byte[,] value = new byte[byte2d_size0, byte2d_size1];
                for (uint i = 0; i < byte2d_size0; i++)
                    for (uint j = 0; j < byte2d_size1; j++)
                        value[i, j] = reader.ReadByte();

                return value;
            }

            return null;
        }

        public static void Write2DByteArray(System.IO.BinaryWriter writer, byte[,] value)
        {
            uint byte2d_size0 = 0;
            uint byte2d_size1 = 0;

            if (value != null)
            {
                byte2d_size0 = (uint)value.GetLength(0);
                byte2d_size1 = (uint)value.GetLength(1);
            }
            writer.Write(byte2d_size0);
            writer.Write(byte2d_size1);
            if (byte2d_size0 > 0 && byte2d_size1 > 0)
                for (uint i = 0; i < byte2d_size0; i++)
                    for (uint j = 0; j < byte2d_size1; j++)
                        writer.Write(value[i, j]);
        }

        public static int[,] Read2DInt32Array(System.IO.BinaryReader reader)
        {
            uint int2d_size0 = reader.ReadUInt32();
            uint int2d_size1 = reader.ReadUInt32();
            if (int2d_size0 > 0 && int2d_size1 > 0)
            {
                int[,] value = new int[int2d_size0, int2d_size1];
                for (uint i = 0; i < int2d_size0; i++)
                    for (uint j = 0; j < int2d_size1; j++)
                        value[i, j] = reader.ReadInt32();

                return value;
            }

            return null;
        }

        public static void Write2DInt32Array(System.IO.BinaryWriter writer, int[,] value)
        {
            uint int2d_size0 = 0;
            uint int2d_size1 = 0;

            if (value != null)
            {
                int2d_size0 = (uint)value.GetLength(0);
                int2d_size1 = (uint)value.GetLength(1);
            }
            writer.Write(int2d_size0);
            writer.Write(int2d_size1);
            if (int2d_size0 > 0 && int2d_size1 > 0)
                for (uint i = 0; i < int2d_size0; i++)
                    for (uint j = 0; j < int2d_size1; j++)
                        writer.Write(value[i, j]);
        }
        #endregion


        #region array extensions
        public static object AddArrayElement(string typeName, object currentValue, object value)
        {
            object result = null;

            ushort typeID = GetTypeIDFromTypename(typeName);
            switch (typeID)
            {

                case TYPE_STRING_ARRAY: // todo: what if we don't want duplicates?
                    result = Keystone.Extensions.ArrayExtensions.ArrayAppendRange((string[])currentValue, (string[])value);
                    break;

                case TYPE_JAGGED_STRING_ARRAY:

                case TYPE_FLOAT_ARRAY:
                case TYPE_JAGGED_FLOAT_ARRAY:

                case TYPE_VECTOR3_D_ARRAY:

                case TYPE_QUATERNION_ARRAY:

                case TYPE_INT32_ARRAY:
                case TYPE_BYTE_2D_ARRAY:
                case TYPE_INT32_2D_ARRAY:

                case TYPE_PROPERTY_SPEC_ARRAY:
                case TYPE_UINT32_ARRAY:
                case TYPE_UINT64_ARRAY:
                default:
                    throw new ArgumentOutOfRangeException();
                    break;
            }

            return result;
        }

        public static object MergeArrayElements(string typeName, object currentValue, object value)
        {
            object result = null;

            ushort typeID = GetTypeIDFromTypename(typeName);
            switch (typeID)
            {

                case TYPE_STRING_ARRAY: // todo: what if we don't want duplicates?
                    result = Keystone.Extensions.ArrayExtensions.ArrayUnion((string[])currentValue, (string[])value);
                    break;

                case TYPE_JAGGED_STRING_ARRAY:

                case TYPE_FLOAT_ARRAY:
                case TYPE_JAGGED_FLOAT_ARRAY:

                case TYPE_VECTOR3_D_ARRAY:

                case TYPE_QUATERNION_ARRAY:

                case TYPE_INT32_ARRAY:
                case TYPE_BYTE_2D_ARRAY:
                case TYPE_INT32_2D_ARRAY:

                case TYPE_PROPERTY_SPEC_ARRAY:
                case TYPE_UINT32_ARRAY:
                case TYPE_UINT64_ARRAY:
                    throw new NotImplementedException ("Not implemented types");
                default:
                    if (currentValue == null && value == null) return null;
                    if (mUserArrayElementsMerge == null) throw new ArgumentException("Unsupported type");
                    result = mUserArrayElementsMerge(typeName, currentValue, value);
                    
                    if (result == null) throw new ArgumentOutOfRangeException();
                    break;
            }

            return result;
        }


        public static object RemoveArrayElement(string typeName, object currentValue)
        {
            object result = null;

            ushort typeID = GetTypeIDFromTypename(typeName);
            switch (typeID)
            {
                // todo: ArrayExtensions should belong in KeyCommon.dll so that our scripts have access to them
               // Keystone.Extensions.ArrayExtensions.ArrayRemove(array, currentValue)
               // Well, they are currently in KeyStandardLibrary...  actually I think that is probably ok... 
               
            }
            return result;
        }

        public static object IncrementNumeric(string typeName, object currentValue, object value)
        {
            object result = null;

            ushort typeID = GetTypeIDFromTypename(typeName);
            switch (typeID)
            {
                case TYPE_VECTOR3_D:
                    result = (Keystone.Types.Vector3d)currentValue + (Keystone.Types.Vector3d)value;
                    break;

            }
            return result;
        }

        public static object DecrementNumeric(string typeName, object currentValue, object value)
        {
            object result = null;

            ushort typeID = GetTypeIDFromTypename(typeName);
            switch (typeID)
            {

            }
            return result;
        }
        #endregion
    }
}
