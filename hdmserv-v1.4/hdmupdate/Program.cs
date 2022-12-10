using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace hdmupdate
{
	class Program
	{
		static void Main(string[] args)
		{
			hdmLog Log = new hdmLog("hdmupdate");
			try
			{
				if (args.Length != 1) return;
				else
				{
					if (!File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0] + ".update"))
					{
						// If the file doesn't exist, download the file
						System.Net.WebClient Client = new System.Net.WebClient();
						if (!Directory.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\"))
						{
							DirectoryInfo di = Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\");
							di.Attributes = FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.System;
						}
						if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0].Trim() + ".update"))
						{
							File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0].Trim() + ".update");
						}
						Client.DownloadFile("http://console.hudsonisd.org/hdms/files/" + args[0].Trim() + ".update", Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0].Trim() + ".update");
					}
					// Extract it before we do the update
					if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0] + ".update"))
					{
						ZipUtil.UnZip(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0] + ".update", Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0] + @"\", null);
					}
					else return;
				}
				Log.Write("Updating...");
				ServiceController controller = new ServiceController();
				controller.MachineName = ".";
				controller.ServiceName = "hdmserv";
				// Stop the service
				if (controller.Status != ServiceControllerStatus.Running && controller.Status != ServiceControllerStatus.Stopped)
				{
					System.Threading.Thread.Sleep(2000);
				}
				if (controller.Status != ServiceControllerStatus.Stopped && controller.Status != ServiceControllerStatus.StopPending)
				{
					controller.Stop();
					controller.WaitForStatus(ServiceControllerStatus.Stopped);
				}

				// Make sure the other exes aren't running...
				FindAndKillProcess("hdmlogon");
				FindAndKillProcess("hdmclient");

				System.Threading.Thread.Sleep(2000);

				// Make sure the WMI and TcpIp service is a dependency for hdmserv.
				// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\hdmserv - "DependOnService" (REG_MULTI_SZ) = winmgmt TcpIp

				try
				{
					RegistryKey rkApp = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\hdmserv", true);
					if(rkApp.GetValue("DependOnService") == null)
						rkApp.SetValue("DependOnService", new string[] { @"winmgmt", "TcpIp" }, RegistryValueKind.MultiString);
				}
				catch { }

				// Unzip the update package

				DirectoryInfo dir = new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0] + @"\");
				FileInfo[] rgFiles = dir.GetFiles("*.*");
				foreach (FileInfo fi in rgFiles)
				{
					if (fi.Name == "ICSharpCode.SharpZipLib.dll") continue;
					try
					{
						if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\" + fi.Name))
						{
							File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\" + fi.Name);
						}
						File.Copy(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + args[0] + @"\" + fi.Name, Path.GetDirectoryName(Application.ExecutablePath) + @"\" + fi.Name, true);
					}
					catch
					{

					}
				}
				System.Threading.Thread.Sleep(2000);
				// Start the service & clean up
				controller.Start();
				controller.WaitForStatus(ServiceControllerStatus.Running);
				ForceDeleteDirectory(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\");
			}
			catch (Exception ex)
			{
				Log.Write("Update failed... " + ex.Message);
			}
		}

		public static void ForceDeleteDirectory(string path)
		{
			DirectoryInfo root;
			Stack<DirectoryInfo> fols;
			DirectoryInfo fol;
			fols = new Stack<DirectoryInfo>();
			root = new DirectoryInfo(path);
			fols.Push(root);
			while (fols.Count > 0)
			{
				fol = fols.Pop();
				fol.Attributes = fol.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
				foreach (DirectoryInfo d in fol.GetDirectories())
				{
					fols.Push(d);
				}
				foreach (FileInfo f in fol.GetFiles())
				{
					f.Attributes = f.Attributes & ~(FileAttributes.Archive | FileAttributes.ReadOnly | FileAttributes.Hidden);
					f.Delete();
				}
			}
			root.Delete(true);
		}

		public static bool FindAndKillProcess(string name)
		{
			foreach (Process clsProcess in Process.GetProcesses()) {
				if (clsProcess.ProcessName.StartsWith(name))
				{
					clsProcess.Kill();
					return true;
				}
			}
			return false;
		}
	}
}
