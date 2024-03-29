﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip;

namespace hdmupdate
{
	public static class ZipUtil
	{
		public static void UnZip(string zipPathAndFile, string outputFolder, string password)
		{
			ZipInputStream s = new ZipInputStream(File.OpenRead(zipPathAndFile));
			if (password != null && password != String.Empty)
				s.Password = password;
			ZipEntry theEntry;
			string tmpEntry = String.Empty;
			while ((theEntry = s.GetNextEntry()) != null)
			{
				string directoryName = outputFolder;
				string fileName = Path.GetFileName(theEntry.Name);
				// create directory 
				if (directoryName != "")
				{
					Directory.CreateDirectory(directoryName);
				}
				if (fileName != String.Empty)
				{
					if (theEntry.Name.IndexOf(".ini") < 0)
					{
						string fullPath = directoryName + "\\" + theEntry.Name;
						fullPath = fullPath.Replace("\\ ", "\\");
						string fullDirPath = Path.GetDirectoryName(fullPath);
						if (!Directory.Exists(fullDirPath)) Directory.CreateDirectory(fullDirPath);
						FileStream streamWriter = File.Create(fullPath);
						int size = 2048;
						byte[] data = new byte[2048];
						while (true)
						{
							size = s.Read(data, 0, data.Length);
							if (size > 0)
							{
								streamWriter.Write(data, 0, size);
							}
							else
							{
								break;
							}
						}
						streamWriter.Close();
					}
				}
			}
			s.Close();
		}
	}
}
