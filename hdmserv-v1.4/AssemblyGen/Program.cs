using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace AssemblyGen
{
	class Program
	{
		static void Main(string[] args)
		{
			int MAJOR_VERSION = 1;
			int MINOR_VERSION = 4;

			string SAF;
			DateTime dt = DateTime.Now;
			int build = int.Parse(dt.Year.ToString().Substring(2) + "" + dt.DayOfYear.ToString().PadLeft(3, '0'));
			string min = dt.Minute.ToString();
			if (min.Length < 2) min = "0" + min;
			int rev = Convert.ToInt16(string.Format("{0:HH}{0:mm}", dt) + Math.Round((double)(dt.Second / 10), 0)) + 10000;
			string version = MAJOR_VERSION.ToString() + "." + MINOR_VERSION.ToString() + "." + (build) + "."+(rev);
			SAF = "using System.Reflection;\n";
			SAF += "using System.Runtime.CompilerServices;\n";
			SAF += "using System.Runtime.InteropServices;\n\n";

			SAF += "[assembly: AssemblyVersion(\""+version+"\")]\n";
			SAF += "[assembly: AssemblyFileVersion(\"" + version + "\")]\n";

			SaveTextToFile(SAF, @"C:\Users\Daniel\Documents\visual studio 2010\Projects\hdm\hdmserv-v1.4\SharedAssembly.cs");
		}
		public static bool SaveTextToFile(string strData, string FullPath, string ErrInfo = "")
		{
			bool bAns = false;
			StreamWriter objReader = default(StreamWriter);
			try
			{

				objReader = new StreamWriter(FullPath);
				objReader.Write(strData);
				objReader.Close();
				bAns = true;
			}
			catch (Exception Ex)
			{
				ErrInfo = Ex.Message;
			}
			return bAns;
		}
	}
}
