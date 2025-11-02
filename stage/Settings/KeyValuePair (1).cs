using System;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Settings
{
    /// <summary>
    /// Structure that holds a key value pair in our XML style INI file.
    /// </summary>
    /// <remarks></remarks>
    [XmlType(TypeName="key"), XmlRoot(), Serializable()] // TypeName "key" is what will appear in the xml output
    public struct KeyValuePair
    {
        [XmlAttributeAttribute(AttributeName="name", Form=XmlSchemaForm.Unqualified, DataType="string")] 
        public string Name;

        [XmlAttributeAttribute(AttributeName="display", Form=XmlSchemaForm.Unqualified, DataType="string")] 
        public string Display;

        [XmlAttributeAttribute(AttributeName="value", Form=XmlSchemaForm.Unqualified, DataType="string")] 
        public string Value;

        [XmlAttributeAttribute(AttributeName="type", Form=XmlSchemaForm.Unqualified, DataType="string")] 
        public string Type;

        [XmlAttributeAttribute(AttributeName="description", Form=XmlSchemaForm.Unqualified, DataType="string")] 
        public string Description;

        [XmlAttributeAttribute(AttributeName="converter", Form=XmlSchemaForm.Unqualified, DataType="string")] 
        public string Converter;

        
        
        public KeyValuePair(string KeyName, string KeyValue)
            : this(KeyName, KeyValue, "", "", "", "")
        {
        }

        public KeyValuePair(string KeyName, string KeyValue, string KeyType)
            : this(KeyName, KeyValue, KeyType, "", "", "")
        {
        }

        public KeyValuePair(string KeyName, string KeyValue, string KeyType, string DisplayName)
            : this(KeyName, KeyValue, KeyType, DisplayName, "", "")
        {
        }

        public KeyValuePair(string KeyName, string KeyValue, string KeyType, string DisplayName, string description)
            : this(KeyName, KeyValue, KeyType, DisplayName, description, "")
        {
        }

        public KeyValuePair(string KeyName, string KeyValue, string KeyType, string DisplayName, string info,
                              string TypeConverter)
        {
            Name = KeyName;
            Value = KeyValue;
            Type = KeyType;
            Display = DisplayName;
            Description = info;
            Converter = TypeConverter;
        }
    }
}