// http://www.gamasutra.com/view/news/38977/InDepth_Behavior_Tree_Entrails.php
	// An agent's blackboard aggregates all agent specific game world knowledge. 
	// It's the only data immediate action functions are allowed to access to keep
	// cache misses at bay. A blackboard data structure might just be a C struct with
	// fields like used by Halo 2 or a key-value dictionary. It's favorable if the
	// blackboard can be stored as a data blob that's easily kept or streamed into 
	// local memory/cache.
	// ---
	// WWG Notes: I suspect the above binary blob version of our UserData would 
	//            require a simple FAT type header that shows the offset
	//            type, and (for some types like strings and arrays)the size or length of the data.  Obviously for many non-array intrinsic types 
	//            the length can be inferred.
	// ---
	
	
/*
Google AI Overview

A binary blob file header refers to a structured block of data at the beginning
of a binary file, providing essential metadata about the file's content and 
organization. While the term "binary blob" often implies unstructured data, a 
header imbues it with a defined structure for interpretation.

Common elements found in a binary blob file header include:
    File Type Identifier (Magic Number):
A unique sequence of bytes that identifies the file format, allowing programs to 
recognize and correctly interpret the file.
    File Version Number:
Indicates the version of the file format, crucial for backward compatibility and 
handling format changes.
    Size Information:
        File Size: The total size of the file.
        Header Size: The size of the header itself.
        Data Size: The size of the actual data payload following the header.
    Checksum or CRC:
A value used to verify the integrity of the file, detecting accidental corruption.
    Metadata:
        Timestamps: Creation, modification, or access times.
        Author/Creator Information: Details about the origin of the file.
        Internal Data Structure Information: Pointers or offsets to different sections 
within the blob, if applicable.
        Byte Order: Specifies the endianness (byte order) of the data within the file, 
important for cross-platform compatibility.

The specific content of a binary blob file header depends entirely on the 
application or system that creates and consumes the file. For example, executable 
file formats like ELF (Executable and Linkable Format) or PE (Portable Executable) 
have elaborate headers defining sections, entry points, and other execution-related 
information. Similarly, a custom data format might include specific fields relevant 
to its unique data structure.
*/

namespace KeyCommon.Data
{
    public class BinaryBlob
    {
        struct BlobHeader
        {
            public int Magic;
            public int Version;
            public int Size;
            public int HeaderSize;
            public int DataSize;
            public int Checksum;
            public int TimeStamp;
            public byte[] Author;
        }
    
        struct BlobContent
        {
            // fixed size bytes representing a string since c# does not support 
            // VB style fixed length string declarations "string myString * 32;
            public byte[] Name; 
            public Type Type;
            public int Size;
            public int Offset;
            
        }
    

       public BlobHeader Header;
       public BlobContent[] Content;
       
       public BinaryBlob()
       {
           Header.Magic = MAGIC;
           Header.Version = VERSION;
           Header.TotalSize = 0;
           Header.HeaderSize = sizeof(BloblHeader);
           Header.DataSize;
           Header.Checksum;
           Header.TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
           Header.Author = new byte[256];
           
           Content = null;
       }
       
       public void Add (string name, Type t, int size, int offset, object value)
       {
           
       }

       
       public void GetBoolean (int offset)
       {
           
       }
       
       public int Find (string name, out Type t, out int size, out int offset)
       {
           if (Content == null) return -1;
           
       }
       
       public void GetByte (int offset)
       {
           
       }
       
       public void GetInteger (int offset)
       {
           
       }
}
