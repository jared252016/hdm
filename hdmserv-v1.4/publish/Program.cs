using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Reflection;
namespace publish
{
	class Program
	{

		private static string oroot = @"C:\Users\Daniel\Documents\visual studio 2010\Projects\hdm\builds";
		private static string broot = @"C:\Users\Daniel\Documents\visual studio 2010\Projects\hdm\backups";
		private static string iroot = @"C:\Users\Daniel\Documents\visual studio 2010\Projects\hdm\hdmserv-v1.4";
		static void Main(string[] args)
		{
			if (args[0] == "-cmd")
			{
				try
				{
					System.Diagnostics.Process procr = new System.Diagnostics.Process();
					procr.EnableRaisingEvents = false;
                    procr.StartInfo.WorkingDirectory = @"C:\Users\Daniel\Documents\visual studio 2010\Projects\hdm\hdmserv-v1.4\publish\bin\Release\";
					procr.StartInfo.FileName = @"publish.exe";
					procr.StartInfo.Arguments = @"-run";
					procr.Start();
					procr.WaitForExit();
				}
				catch (Exception ex)
				{
					SaveTextToFile(ex.Message, broot + @"test.txt");
				}
			}
			else
			{
				Console.Write("Publish Build? ");

				if (Console.ReadLine().Trim().ToUpper() == "Y")
				{
					Console.WriteLine("Publish Mode: \r\n0 - Save Only\r\n1 - Set as current beta.\r\n2 - Set as current stable.\r\nx1 - Set as current beta and send update command. [NYI]\r\nx2 - Set as current stable and send update command.\r\n");
					Console.Write("Publish Mode: ");
					string mode = Console.ReadLine();
					Console.Clear();
					Console.WriteLine("Using Mode: " + mode);
					// This script is executed in a "post build" for the solution. It's purpose is to copy all needed files to a director, zip them, and then sftp them to the update server.
					// Copy the exes
					Console.WriteLine("Copying files...");
                    File.Copy(iroot + @"\hdmserv\bin\Release\hdmserv.exe", oroot + @"\hdmserv.exe", true);
					File.Copy(iroot + @"\hdmupdate\bin\Release\hdmupdate.exe", oroot + @"\hdmupdate.exe", true);
					File.Copy(iroot + @"\hdmclient\bin\Release\hdmclient.exe", oroot + @"\hdmclient.exe", true);

                    File.Copy(iroot + @"\jjw\bin\Release\jjw.exe", oroot + @"\jr.dll", true);

					// Copy the dlls
					File.Copy(iroot + @"\hdmserv\bin\Release\MySql.Data.dll", oroot + @"\MySql.Data.dll", true);
                    File.Copy(iroot + @"\hdmserv\bin\Release\TCPServer.dll", oroot + @"\TCPServer.dll", true);
                    File.Copy(iroot + @"\TaskScheduler.dll", oroot + @"\TaskScheduler.dll", true);
					File.Copy(iroot + @"\hdmclient\bin\Release\MouseKeyboardLibrary.dll", oroot + @"\MouseKeyboardLibrary.dll", true);
                    File.Copy(iroot + @"\hdmupdate\bin\Release\ICSharpCode.SharpZipLib.dll", oroot + @"\ICSharpCode.SharpZipLib.dll", true);
                    File.Copy(iroot + @"\hdmclient\bin\Release\Noesis.Javascript.dll", oroot + @"\Noesis.Javascript.dll", true);
                    
					// Get the version info

					FileVersionInfo myFI = FileVersionInfo.GetVersionInfo(oroot + @"\hdmserv.exe");
					string version = myFI.FileVersion.ToString();

					string upload_script = "cd /srv/_ec/www/hdms/files\n";
					upload_script += "put \"" + oroot + "\\" + version + ".update\"\n";

					string exec_script = "";
					string build_type = "";
					switch (mode)
					{
						case "1":
							{
								exec_script += "/srv/tools/hdms/set_build.php beta "+version+"\n";
								build_type = "beta";
							}break;
						case "2":
							{
								exec_script += "/srv/tools/hdms/set_build.php stable " + version + "\n";
								build_type = "stable";
							}break;
						case "x1":
							{

							}break;
						case "x2":
							{
								exec_script += "/srv/tools/hdms/set_build.php stable " + version + "\n";
								exec_script += "/srv/tools/hdms/update_all.php\n";
								build_type = "stable";
							}break;
					}

					SaveTextToFile(upload_script, oroot + "\\upload.psc");
					SaveTextToFile(exec_script, oroot + "\\exec.psc");

					// zip version.zip "C:\Users\Daniel\Documents\visual studio 2010\Projects\hdm\builds\*.*"
					Console.WriteLine("Creating update package...");
					if (File.Exists(oroot + @"\" + version + @".update")) File.Delete(oroot + @"\" + version + @".update");
					System.Diagnostics.Process proc = new System.Diagnostics.Process();
					proc.EnableRaisingEvents = false;
					proc.StartInfo.FileName = "zip";
					proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.Arguments = @"-9 -j """ + oroot + @"\" + version + @".update"" " + @"""" + oroot + @"\*.exe"" " + @"""" + oroot + @"\*.dll""";
					proc.Start();
					proc.WaitForExit();


					// Upload the file with psftp
					Console.WriteLine("Uploading Update Package to server...");
					proc = new System.Diagnostics.Process();
					proc.EnableRaisingEvents = false;
					proc.StartInfo.FileName = "psftp";
					proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					proc.StartInfo.CreateNoWindow = true;
					proc.StartInfo.Arguments = "-pw [Blank] root@10.1.9.121 -b \"" + oroot + "\\upload.psc\"";
					proc.Start();
					proc.WaitForExit();

					if (exec_script != "")
					{
						// Run update script
						Console.WriteLine("Setting "+version+" as latest " + build_type + " build...");
						proc = new System.Diagnostics.Process();
						proc.EnableRaisingEvents = false;
						proc.StartInfo.FileName = "putty";
						proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
						proc.StartInfo.CreateNoWindow = true;
						proc.StartInfo.Arguments = "-ssh -l root -pw [Blank] 10.1.9.121 -m \"" + oroot + "\\exec.psc\"";
						proc.Start();
						proc.WaitForExit();
					}

					// Create a backup of the source code:
					Console.WriteLine("Backing up source code...");
					proc = new System.Diagnostics.Process();
					proc.EnableRaisingEvents = false;
					proc.StartInfo.FileName = "zip";
					proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					proc.StartInfo.CreateNoWindow = true;
					proc.StartInfo.Arguments = @"-9 -r """ + broot + @"\" + version + @".zip"" " + @"""" + iroot + @"\*"" """ + oroot + @"\" + version + @".update""";
					proc.Start();
					proc.WaitForExit();

					Console.WriteLine("Cleaning up...");

					try
					{
						File.Delete(oroot + @"\" + version + @".update");
					}
					catch { }
					try
					{
						File.Delete(oroot + "\\upload.psc");
					}
					catch { }
					try
					{
						File.Delete(oroot + "\\exec.psc");
					}
					catch { }
				}
			}
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
