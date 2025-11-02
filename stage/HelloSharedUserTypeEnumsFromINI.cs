using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KeyCommon
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello, World!");
			
			string path = @"My filesDownloadstest_shared_types.txt";
			
			SharedUserTypes stype = new SharedUserTypes(path);
			
			stype.AddUserType ("HULL", 100);
			stype.AddUserType ("HULL_POD", 200);
			
			stype.Save();
			
			Console.WriteLine("Until next time, World!");
		}
	}
}