using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security;

namespace hdmserv
{
	public class Novell
	{
		private static string _NDSWAI_Path = Path.GetDirectoryName(Application.ExecutablePath) + @"\ndswai";
		public static string getCurrentUser()
		{
			if (File.Exists(_NDSWAI_Path))
			{
				WaitForFile(_NDSWAI_Path, 15);
				try
				{
					return File.ReadAllText(_NDSWAI_Path).Replace("CN=", "").Replace("'", "\\'");
				}
				catch (Exception ex)
				{
					hdmLog Log = new hdmLog("hdmserv");
					Log.Write("Novell:getCurrentUser " + ex.Message, EventLogEntryType.Error);
				}
			}
			return "";
		}

		public static bool WaitForFile(string filePath, int waitTime)
		{
			if (File.Exists(filePath))
			{
				bool StayInLoop;
				int trys = 0;
				do
				{
					StayInLoop = false;
					try
					{
						System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).Close();
					}
					catch (System.IO.FileNotFoundException ex)
					{
						return false;
					}
					catch
					{
						trys++;
						StayInLoop = true;
						System.Threading.Thread.Sleep(100);
					}
				} while (StayInLoop && trys < waitTime / 100);
				if (!(trys < waitTime / 100))
				{
					return false;
				}
				return true;
			}
			return false;
		}  
	}
}
