using System;
using System.IO;

public class Program
{
	public static void Main()
	{
		//string p = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		//p = System.IO.Directory.GetCurrentDirectory();
		//p += "/Downloads";
		
		// this will get the path our .NETFiddle app is running on at the remote server
		string filePath = System.IO.Directory.GetCurrentDirectory();
				
		// create a file in the path our app is compiled too
		filePath = System.IO.Path.Combine(filePath, "/test.txt");
		System.IO.FileStream stream = System.IO.File.Create (filePath);
		stream.Close();
		
		// verify it exists
		bool exists = System.IO.File.Exists(filePath);
		Console.WriteLine(filePath + "  exists = "  + exists.ToString() );
		
		// TODO: unfortunately, i dont know how to read in data locally... we would literally have to
		// paste the data into a string variable.
		// eg.
		
		
		// CREATE a file in your Sandbox using .NET Fiddle 
		WriteFile(filePath);
		
		// READ an existing file in your Sandbox using .NET Fiddle
		var s = ReadAllText(filePath);
		
		Console.Write(s);
		
		
	}
	
	// TODO: past all the data we want to load into the below and then pass in the path to write it
	public static void WriteFile(string path)
	{
		File.WriteAllText(path, ".NET Fiddle is AWESOME_!!!");
	}
	
	public static string ReadAllText(string path)
	{
		return File.ReadAllText(path);
	}
	
	
}