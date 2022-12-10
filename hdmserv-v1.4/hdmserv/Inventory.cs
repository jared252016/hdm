using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
namespace hdmserv
{
	class Inventory
	{
		public struct MemoryStatus
		{
			public uint Length;
			public uint MemoryLoad;
			public uint TotalPhysical;
			public uint AvailablePhysical;
			public uint TotalPageFile;
			public uint AvailablePageFile;
			public uint TotalVirtual;
			public uint AvailableVirtual;
		} 
		[DllImport("kernel32.dll")]
		public static extern void GlobalMemoryStatus(out MemoryStatus stat);

		public bool CheckForUpdates = false;

		private bool _UpdateAvailable = false;
		private string _UpdateVersion = "";
		private string ComputerName = "";
		private string OSVersion = "";
		private int MemoryTotal = 0;
		public int ProcessorSpeed = 0;
		private int ProcessorCores = 0;
		private string IPAddress = "";
		private string MacAddress = "";
		private string CurrentVersion;
		private string dotNetVersion = "";
        private string softwareInventory = "";

		private DataConn _Conn;
		public Inventory(DataConn conn)
		{
			// Pass in the Mysql Connector
			_Conn = conn; 
			// CPU Name
			ComputerName = System.Environment.MachineName;
			// Current version
			CurrentVersion = GetCurrentVersion();
		}
		public static string GetCurrentVersion()
		{
			string v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			string[] vs = v.Split('.');
			if (vs[3].Length < 5)
			{
				vs[3] = "0" + vs[3];
			}
			return String.Join(".", vs);
		}
        public int GetTotalMemory()
        {
            MemoryStatus stat = new MemoryStatus();
            GlobalMemoryStatus(out stat);
            return (int)(Math.Ceiling((double)((((double)stat.TotalPhysical) / 1024 / 1024) / 64)) * 64);
        }
        private void RunSoftwareInventory()
        {
            RegistryKey rk;
            hdmLog Log = new hdmLog("hdmserv");
            try
            {
                rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                foreach (string sub in rk.GetSubKeyNames())
                {
                    try
                    {
                        RegistryKey local = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + sub);
                        string name = local.GetValue("DisplayName", "").ToString();
                        if (name != "")
                        {
                            //Log.Write(local.GetValue("DisplayName", "Not Found").ToString(), System.Diagnostics.EventLogEntryType.Information);
                        }
                        local.Close();
                    }
                    catch (Exception er)
                    {
                        Log.Write("Inventory.cs > RunSoftwareInventory() 2 - " + er.Message, System.Diagnostics.EventLogEntryType.Error);
                    }
                }
                rk.Close();
            }
            catch (Exception ex)
            {
                Log.Write("Inventory.cs > RunSoftwareInventory() - " + ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }
		public void RunInventory()
		{
			// OS Version
			OSVersion = System.Environment.OSVersion.VersionString;

			// Detect .NET Version
			if (Directory.Exists("C:/WINDOWS/Microsoft.NET/Framework/v4.0.30319/"))
			{
				dotNetVersion = "4.0";
			} else if (Directory.Exists("C:/WINDOWS/Microsoft.NET/Framework/v3.5/")) {
				dotNetVersion = "3.5";
			} else if (Directory.Exists("C:/WINDOWS/Microsoft.NET/Framework/v3.0/"))
			{
				dotNetVersion = "3.0";
			}
			else
			{
				dotNetVersion = "???";
			}

			// Memory
			try
			{
				MemoryStatus stat = new MemoryStatus();
				GlobalMemoryStatus(out stat);
				MemoryTotal = (int)(Math.Ceiling((double)((((double)stat.TotalPhysical) / 1024 / 1024) / 64)) * 64);
			}
			catch { }
			// Processor Speed
			try
			{
				ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

				foreach (ManagementObject queryObj in searcher.Get())
				{
                    if (Convert.ToInt32(queryObj["MaxClockSpeed"]) > 0)
                        ProcessorSpeed = Convert.ToInt32(queryObj["MaxClockSpeed"]);
				}
				searcher.Dispose();
			}
			catch (Exception ex)
			{
				hdmLog Log = new hdmLog("hdmserv");
				Log.Write("Inventory:RunInventory (Processor Speed) " + ex.Message, System.Diagnostics.EventLogEntryType.Error);
				ProcessorSpeed = -1;
			}
			// Processor Cores
			try
			{
				int coreCount = 0;
				foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
				{
					coreCount += int.Parse(item["NumberOfCores"].ToString());
				}
				ProcessorCores = coreCount;
			}
			catch { }
			// IP Adress
			try
			{
				string sHostName = Dns.GetHostName();
                List<string> temp = new List<string>();
				IPHostEntry ipE = Dns.GetHostEntry(sHostName);
				IPAddress[] IpA = ipE.AddressList;
				for (int i = 0; i < IpA.Length; i++)
				{
					if (IpA[i].ToString().StartsWith("10.1."))
					{
                        if(!temp.Contains(IpA[i].ToString())) {
                            temp.Add(IpA[i].ToString());
                        }
					}
				}
				IPAddress = string.Join(",", temp.ToArray());
                temp = null;
			}
			catch { }
			// Mac Address
			try
			{
				ManagementClass oMClass = new ManagementClass("Win32_NetworkAdapterConfiguration");
				ManagementObjectCollection colMObj = oMClass.GetInstances();
				List<string> macas = new List<string>();
				foreach (ManagementObject objMO in colMObj)
				{
					try
					{
						List<string> ipa = new List<string>();
						ipa.AddRange((String[])objMO["IPAddress"]);
						foreach (string addy in ipa)
						{
							if (addy.ToString().StartsWith("10.1."))
							{
								if (!macas.Contains(objMO["MacAddress"].ToString()))
								{
									macas.Add(objMO["MacAddress"].ToString());
								}
							}
						}
					}
					catch { }
				}
				colMObj.Dispose();
				MacAddress = String.Join("\n", macas.ToArray()).Trim();
			}
			catch { }
            RunSoftwareInventory();
		}
		public void Save()
		{
			try
			{
				// It does exist. let's update it.

                //string InventoryQuery = "UPDATE `inventory` SET `DotNetVersion` = '" + dotNetVersion + "', `IPAddress` ='" + IPAddress + "', `MacAddress` = '" + MacAddress + "', `OSVersion` = '" + OSVersion.Replace("'", "\\'") + "', `MemoryTotal` = '" + MemoryTotal + "', `ProcessorSpeed` = '" + ProcessorSpeed + "', `ProcessorCores` = '" + ProcessorCores + "', `HDMSVersion` = '" + CurrentVersion + "', `LastUpdated` = NOW() WHERE `Name` = '" + ComputerName.Replace("'", "\\'") + "' LIMIT 1;";
                string InventoryQuery = "INVENTORY \"" + GetUnixTimestamp() + "\" \"" + dotNetVersion + "\" \"" + IPAddress + "\" \"" + MacAddress + "\" \"" + OSVersion + "\" \"" + MemoryTotal + "\" \"" + ProcessorSpeed + "\" \"" + ProcessorCores + "\" \"" + CurrentVersion + "\"";
                _Conn.AppendTDB(InventoryQuery);
			}
			catch (Exception e)
			{
				hdmLog Log = new hdmLog("hdmserv");
				Log.Write("Inventory:Save " + e.Message, System.Diagnostics.EventLogEntryType.Error);
			}
		}
		
		public bool UpdateAvailable(out string v)
		{
			v = _UpdateVersion;
			return _UpdateAvailable;
		}
        long GetUnixTimestamp()
        {
            return (long) Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds, 0); 
        }
        public static string MD5(string password)
        {
            byte[] textBytes = System.Text.Encoding.Default.GetBytes(password);
            try
            {
                System.Security.Cryptography.MD5CryptoServiceProvider cryptHandler;
                cryptHandler = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] hash = cryptHandler.ComputeHash(textBytes);
                string ret = "";
                foreach (byte a in hash)
                {
                    if (a < 16)
                        ret += "0" + a.ToString("x");
                    else
                        ret += a.ToString("x");
                }
                return ret;
            }
            catch
            {
                throw;
            }
        }
	}
}
