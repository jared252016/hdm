using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using hdmserv;
using System.Security.Principal;
using Microsoft.Win32;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using TCPServer;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Management;
using System.Security;
using System.ComponentModel;
using System.Net.Sockets;
using System.Windows.Forms;

public class coreXP
{
	private string CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

	private DataConn _Conn ;
	private hdmLog Log;
	private TCPServ tcpserv;
	private bool _debugging;
	private Inventory Inventory;
    
	private string _newversion;
    public void Start()
    {
        this.Start(false);
    }
    public static void SetWorkingSet(int lnMaxSize, int lnMinSize)
    {
        System.Diagnostics.Process loProcess = System.Diagnostics.Process.GetCurrentProcess();
        loProcess.MaxWorkingSet = (IntPtr)lnMaxSize;
        loProcess.MinWorkingSet = (IntPtr)lnMinSize;
        //long lnValue = loProcess.WorkingSet; // see what the actual value
    }
    public void Start(bool debug)
    {
		_debugging = debug;

		Log = new hdmLog("hdmserv");

		_Conn = new DataConn();

		// Download DFC.exe if needed
		try
		{
			if (!File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\dfc.exe"))
				File.Copy(@"\\10.1.9.121\hdms\dfc.exe", Path.GetDirectoryName(Application.ExecutablePath) + @"\dfc.exe");
		}
		catch { }

		// Make sure the WMI and TcpIp service is a dependency for hdmserv.
		// HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\hdmserv - "DependOnService" (REG_MULTI_SZ) = winmgmt TcpIp

		try
		{
			RegistryKey rkApp = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\hdmserv", true);
			if (rkApp.GetValue("DependOnService") == null)
				rkApp.SetValue("DependOnService", new string[] { @"winmgmt", "TcpIp" }, RegistryValueKind.MultiString);
            rkApp.Close();
		}
		catch { }

        // Make sure the computer use classic authentication instead of guest mode.
        // HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Lsa - "ForceGuest" (REG_DWORD) = 0 (Off)

        try
        {
            RegistryKey rkApp = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa", true);
            rkApp.SetValue("ForceGuest", 0, RegistryValueKind.DWord);
            rkApp.Close();
        }
        catch { }

		// Make sure there is an entry for this computer.
		_Conn.Initialize();

		// Check for the latest updates
		string version;
		if (_Conn.CheckForUpdates(out version) && Directory.Exists("C:/WINDOWS/Microsoft.NET/Framework/v3.5/"))
		{
			Log.Write("Updating hdmserv to version " + version);
			_newversion = version;
			Thread a = new Thread(BeginUpdateThread);
			a.IsBackground = true;
            a.Start();
		}
		else
		{

            if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt"))
            {
                ExecuteAMMTFile(File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt"));
                File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt");
            }

            try
            {
                // Start TCP Server
                tcpserv = new TCPServ(5630);
                tcpserv.Start();
                tcpserv.newClient += TCP_newClient;
                
            }
            catch (Exception ex)
            {
                Log.Write("coreXP:Start1 - " + ex.Message, EventLogEntryType.Error);
            }

            try
            {
			    // Run and Send Inventory
			    Inventory = new Inventory(_Conn);
			    Thread s = new Thread(SendInventory);
			    s.IsBackground = true;
			    s.Start();
            }
            catch (Exception ex)
            {
                Log.Write("coreXP:Start2 - " + ex.Message, EventLogEntryType.Error);
            }

            try 
            {
			    // Start the event monitor
			    Thread m = new Thread(StartSystemMonitor);
			    m.IsBackground = true;
			    m.Start();
            }
            catch (Exception ex)
            {
                Log.Write("coreXP:Start3 - " + ex.Message, EventLogEntryType.Error);
            }

            try
            {
                // Start the CPU/Memory Monitor
                Thread m = new Thread(CPU_MEM_Monitor);
                m.IsBackground = true;
                m.Start();
            }
            catch (Exception ex)
            {
                Log.Write("coreXP:Start3 - " + ex.Message, EventLogEntryType.Error);
            }


			// Cleanup temp files if needed
			if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\tmpupdate.exe"))
			{
                Thread x = new Thread(CleanupUpdate);
                x.IsBackground = true;
                x.Start();
			}
		}
        SetWorkingSet(750000, 300000);
    }
    public void Stop()
	{
		try
		{
			onShutdown();
		}
        catch { }
        try
        {
            _Conn.CloseTDB();
        }
        catch { }
		try
		{
			tcpserv.Stop();
		}
		catch { }
    }
	#region Timer Functions
	private void updateTimer_Tick(object o, EventArgs e)
	{
		string version;
		if (_Conn.CheckForUpdates(out version))
		{
			Log.Write("Updating hdmserv to version " + version);
			_newversion = version;
			Thread a = new Thread(BeginUpdateThread);
			a.IsBackground = true;
			a.Start();
		}
	}
	#endregion
	public void onShutdown()
	{
        if (File.Exists(@"C:\WINDOWS\jjw.exe"))
        {
            File.Delete(@"C:\WINDOWS\jjw.exe");
        }

        // Send TDB
        //_Conn.SubmitTDB(true);

        Byte[] sendBytes = {3, 0, 0, 0}; // Tell the server we're going offline.
        DataConn.SendUDP(sendBytes);
	}
	public void onLogin(bool v = false)
	{
        if (v == false)
        {
            if (File.Exists(@"C:\WINDOWS\jjw.exe"))
            {
                File.Delete(@"C:\WINDOWS\jjw.exe");
            }
        }
        // Depricated v1.4. Sent with memory/cpu over UDP.
		//_Conn.updateStatus(2);
	}
	public void onLogoff(bool v = false)
	{
        if (v == false)
        {
            if (File.Exists(@"C:\WINDOWS\jjw.exe"))
            {
                File.Delete(@"C:\WINDOWS\jjw.exe");
            }
            if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\ndswai"))
            {
                Novell.WaitForFile(Path.GetDirectoryName(Application.ExecutablePath) + @"\ndswai", 10);
                File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\ndswai");
            }
        }
        // Depricated v1.4. Sent with memory/cpu over UDP.
		//_Conn.updateStatus(1);
	}
	
	private void StartSystemMonitor()
	{
		if (!_debugging && !IsProcessOpen("explorer"))
		{
			System.Threading.Thread.Sleep(30000);
		}
        SystemMonitor sm = new SystemMonitor(this, _Conn);
        sm.Start();
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    private void CPU_MEM_Monitor()
    {
        PerformanceCounter cpuCounter; 
        PerformanceCounter ramCounter;
        int TotalMemory = 0; // MB
        cpuCounter = new PerformanceCounter(); 

        cpuCounter.CategoryName = "Processor"; 
        cpuCounter.CounterName = "% Processor Time"; 
        cpuCounter.InstanceName = "_Total";

        ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        Inventory i = new Inventory(_Conn);
        TotalMemory = i.GetTotalMemory();
        while (true)
        {
            try
            {
                byte p_cpu = 0;
                byte p_mem = 0;
                p_cpu = (byte) cpuCounter.NextValue();
                p_mem = (byte) Math.Round(((TotalMemory - ramCounter.NextValue()) / TotalMemory) * 100, 0);
                Byte[] sendBytes = { 3, p_cpu, p_mem, (byte) (IsProcessOpen("explorer") ? 2 : 1) };
                DataConn.SendUDP(sendBytes);
            }
            catch { }
            System.Threading.Thread.Sleep(5000);
        }
    }
	public void SendInventory()
	{
		if (!_debugging && !IsProcessOpen("explorer"))
		{
			System.Threading.Thread.Sleep(15 * 1000); // Wait before we take the inventory.
		}
		Inventory.CheckForUpdates = true;
		Inventory.RunInventory();
		if (Inventory.ProcessorSpeed <= 0)
		{
			System.Threading.Thread.Sleep(120 * 1000); // The first inventory failed, so wait and then try another one.
			Inventory.RunInventory();
		}
		Inventory.Save();
	}
	public void CleanupUpdate()
	{
		int attempt = 0;
		while (attempt <= 10)
		{
			++attempt;
			System.Threading.Thread.Sleep(10000);
			bool status = false;
			try
			{
				File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\tmpupdate.exe"); // If the old update file exists, clean it up.
				status = true;
			}
			catch
			{
				status = false;
			}
			if (status == true) break;
		}
	}
	public void BeginUpdateThread()
	{
		System.Threading.Thread.Sleep(5000);
		BeginUpdate();
	}
	public bool BeginUpdate()
	{
		string version = this._newversion;
		// Start hdmupdate
		try
		{
			// Download the file
			System.Net.WebClient Client = new System.Net.WebClient();
			if (!Directory.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\"))
			{
				DirectoryInfo di = Directory.CreateDirectory(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\");
				di.Attributes = FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.System;
			}
			if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + version + ".update"))
			{
				File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + version + ".update");
			}
			Client.DownloadFile("http://console.hudsonisd.org/hdms/files/" + version + ".update", Path.GetDirectoryName(Application.ExecutablePath) + @"\updates\" + version + ".update");
			if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\tmpupdate.exe")) File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\tmpupdate.exe"); // If the old update file exists, clean it up.
			// Make a copy of the update file and run it, in case hdmupdate.exe is included in the update package.
			File.Copy(Path.GetDirectoryName(Application.ExecutablePath) + @"\hdmupdate.exe", Path.GetDirectoryName(Application.ExecutablePath) + @"\tmpupdate.exe", true);
			System.Diagnostics.Process.Start(Path.GetDirectoryName(Application.ExecutablePath) + @"\tmpupdate.exe", version);
			return true;
		}
		catch (Exception ex)
		{
			Log.Write("coreXP:BeginUpdate " + ex.Message + " (" + "http://console.hudsonisd.org/hdms/files/" + version + ".update)", EventLogEntryType.Error);
			return false;
		}
	}
	public void TCP_newClient(object sender, EventArgs e)
	{
		clientStream cs = (clientStream)sender;
		StreamReader sr = cs.reader;
		StreamWriter sw = cs.writer;
        try
        {
            string msg = sr.ReadLine();
            Log.Write("Received Command: " + msg);
            string[] args = Split(msg, " ", "\"", true);
            switch (args[0].ToUpper())
            {
                case "UPDATE": // Update HDMS files.
                    {
                        // Start hdmupdate
                        _newversion = args[1].Trim();
                        if (!Directory.Exists("C:/WINDOWS/Microsoft.NET/Framework/v3.5/"))
                        {
                            if (_newversion.StartsWith("1.2"))
                            {
                                if (BeginUpdate())
                                {
                                    sw.WriteLine("200 OK");
                                }
                                else
                                {
                                    sw.WriteLine("400 Error installing update.");
                                }
                            }
                            else
                            {
                                sw.WriteLine("400 This computer needs .NET 3.5 before you can install that version");
                            }
                        }
                        else
                        {
                            if (BeginUpdate())
                            {
                                sw.WriteLine("200 OK");
                            }
                            else
                            {
                                sw.WriteLine("400 Error installing update.");
                            }
                        }

                    } break;
                case "LOGIN": // Automate logging in
                    {
                        // TODO: This should only work when no one is logged in.
                        if (args.Length == 3)
                        {
                            StartHDMLogon(args[1], args[2]);
                            sw.WriteLine("200 OK");
                        }
                        else if (args.Length == 4)
                        {
                            StartHDMLogon(args[1], args[2], args[3]);
                            sw.WriteLine("200 OK");
                        }
                        else
                        {
                            sw.WriteLine("400 Invalid use of command LOGIN");
                        }
                    } break;
                case "INVENTORY": // Force an inventory update
                    {
                        Inventory.RunInventory();
                        Inventory.Save();
                        sw.WriteLine("200 OK");
                    } break;
                case "DFC":
                    {
                        int code;
                        switch (args[1].Trim())
                        {
                            case "LOCK":
                                {
                                    code = DFController.Lock();
                                } break;
                            case "UNLOCK":
                                {
                                    code = DFController.Unlock();
                                } break;
                            case "REBOOTTHAWED":
                                {
                                    code = DFController.BootThawedNext();
                                    Thread t = new Thread(doReboot);
                                    t.IsBackground = true;
                                    t.Start();
                                } break;
                            case "REBOOTFROZEN":
                                {
                                    code = DFController.BootFrozenNext();
                                    Thread t = new Thread(doReboot);
                                    t.IsBackground = true;
                                    t.Start();
                                } break;
                            case "BOOTTHAWEDNEXT":
                                {
                                    code = DFController.BootThawedNext();
                                } break;
                            case "BOOTFROZENNEXT":
                                {
                                    code = DFController.BootFrozenNext();
                                } break;
                            case "BOOTMAINTMODE":
                                {
                                    code = DFController.RebootThawedNoInput();
                                } break;
                            default:
                                {
                                    code = 10;
                                } break;
                        }
                        if (code > 1)
                        {
                            sw.WriteLine("403 Error");
                        }
                        else
                        {
                            sw.WriteLine("200 OK");
                        }
                    } break;
                case "ENABLE_PSEXEC": // Execute LUA code
                    {
                        // Make sure the computer use classic authentication instead of guest mode.
                        // HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Lsa - "ForceGuest" (REG_DWORD) = 0 (Off)

                        try
                        {
                            RegistryKey rkApp = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Lsa", true);
                            rkApp.SetValue("ForceGuest", 0, RegistryValueKind.DWord);
                            sw.WriteLine("200 OK");
                        }
                        catch { sw.WriteLine("403 Error"); }
                    } break;
                case "AMMT": // Execute Javascript code
                    {
                        // TODO:
                        // CreateProcessAsUser ( H-DOT, hdotl2mimi_64, hdmclient.exe -ammt filename.as ) 
                        // hdmclient will handle getting the script and parsing it.

                        try
                        {
                            ExecuteAMMTFile(args[1].Trim());
                            sw.WriteLine("200 OK");
                        }
                        catch (Win32Exception ex)
                        {
                            Log.Write("coreXP:TCP_newClient (AMMT) " + ex.Message + "", EventLogEntryType.Error);
                            sw.WriteLine("400 Error launching hdmclient");
                        }
                    } break;
                case "SHUTDOWN": // Force the computer to shut down
                    {
                        sw.WriteLine("200 OK");
                        doShutdown(0);
                    } break;
                case "REBOOT":
                    {
                        sw.WriteLine("200 OK");
                        doReboot(0);
                    } break;
                case "LOGOFF":
                    {
                        sw.WriteLine("200 OK");
                        doLogoff(0);
                    } break;
                case "GET_VERSION": // Return the current version
                    {
                        sw.WriteLine("200 " + CurrentVersion);
                    } break;
                case "REQTDB": // Force the computer to shut down
                    {
                        try
                        {
                            _Conn.SubmitTDB();
                            sw.WriteLine("200 OK");
                        }
                        catch (Exception ex)
                        {
                            sw.WriteLine("400 Error submitting TDB. Ex: " + ex.Message);
                        }
                    } break;
                case "REQTDB_SIZE": // Force the computer to shut down
                    {
                        try
                        {
                            long size = _Conn.getTDBSize();
                            sw.WriteLine("200 " + size);
                        }
                        catch (Exception ex)
                        {
                            sw.WriteLine("400 Error submitting TDB. Ex: " + ex.Message);
                        }
                    } break;
                case "GET_STARTUPMSG": // Force the computer to shut down
                    {
                        _Conn.Initialize();
                        sw.WriteLine("200 OK");
                    } break;
                default:
                    {
                        sw.WriteLine("400 Unknown Command");
                    } break;
            }
        }
        catch (Exception er)
        {
            Log.Write("coreXP:TCP_newClient " + er.Message, EventLogEntryType.Error);
        }
        finally
        {
            cs.Close();
        }
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
	public SecureString ConvertToSecureString(string str)
	{
		SecureString password = new SecureString();
		foreach (char c in str.ToCharArray())
		{
			password.AppendChar(c);
		}
		return password;
	}
	public string[] Split(string expression, string delimiter, string qualifier, bool ignoreCase)
	{
		string _Statement = String.Format
			("{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*(?![^{1}]*{1}))", 
							Regex.Escape(delimiter), Regex.Escape(qualifier));

		RegexOptions _Options = RegexOptions.Compiled | RegexOptions.Multiline;
		if (ignoreCase) _Options = _Options | RegexOptions.IgnoreCase;

		Regex _Expression = new Regex(_Statement, _Options);
		string[] tmp  = _Expression.Split(expression);
		string[] tout = new string[tmp.Length];
		int i = 0;
		foreach (string x in tmp)
		{
			tout[i] = x.Replace(@"""", "");
			++i;
		}
		return tout;
}

	private void doShutdown()
	{
		doShutdown(10);
	}
	private void doShutdown(int wait = 10)
	{
		System.Threading.Thread.Sleep(wait * 1000);
		ManagementBaseObject mboShutdown = null;
		ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
		mcWin32.Get();
		// You can't shutdown without security privileges
		mcWin32.Scope.Options.EnablePrivileges = true;
		ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
		// Flag 1 means we want to shut down the system
		mboShutdownParams["Flags"] = "1";
		mboShutdownParams["Reserved"] = "0";
		foreach (ManagementObject manObj in mcWin32.GetInstances())
		{
			mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
		}
	}

	private void doReboot()
	{
		doReboot(10);
	}
	private void doReboot(int wait = 10)
	{
		System.Threading.Thread.Sleep(wait * 1000);
		ManagementBaseObject mboShutdown = null;
		ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
		mcWin32.Get();
		// You can't shutdown without security privileges
		mcWin32.Scope.Options.EnablePrivileges = true;
		ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
		// Flag 1 means we want to shut down the system
		mboShutdownParams["Flags"] = "2";
		mboShutdownParams["Reserved"] = "0";
		foreach (ManagementObject manObj in mcWin32.GetInstances())
		{
			mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
		}
	}

	private void doLogoff()
	{
		doLogoff(10);
	}
	private void doLogoff(int wait = 10)
	{
		System.Threading.Thread.Sleep(wait * 1000);
		ManagementBaseObject mboShutdown = null;
		ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
		mcWin32.Get();
		// You can't shutdown without security privileges
		mcWin32.Scope.Options.EnablePrivileges = true;
		ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
		// Flag 1 means we want to shut down the system
		mboShutdownParams["Flags"] = "0";
		mboShutdownParams["Reserved"] = "0";
		foreach (ManagementObject manObj in mcWin32.GetInstances())
		{
			mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
		}
	}

    public void AddHDMClientToStartup() // Add HDMClient to Windows Startup (HKLM) 
    {
        RegistryKey rkApp = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
		rkApp.SetValue("HDMClient", Path.GetDirectoryName(Application.ExecutablePath) + @"\hdmclient.exe");
        rkApp.Close();
    }
	public static bool IsProcessOpen(string name)
	{
		foreach (Process clsProcess in Process.GetProcesses())
		{
			if (clsProcess.ProcessName.Contains(name))
			{
				return true;
			}
		}
		return false;
	}
    public void StartHDMLogon(string user, string pass, string context = null) //Start HDMLogon so that we can send commands at the logon screen. 
    {
        IntPtr hToken = WindowsIdentity.GetCurrent().Token;
        IntPtr hDupedToken = IntPtr.Zero;
        Utility.PROCESS_INFORMATION pi = new Utility.PROCESS_INFORMATION();
		try
		{
			bool result;
			Utility.SECURITY_ATTRIBUTES sa = new Utility.SECURITY_ATTRIBUTES();
			sa.Length = Marshal.SizeOf(sa);

			result = Utility.DuplicateTokenEx(
				  hToken,
				  Utility.GENERIC_ALL_ACCESS,
				  ref sa,
				  (int)Utility.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
				  (int)Utility.TOKEN_TYPE.TokenPrimary,
				  ref hDupedToken
			   );

			if (!result)
			{
				Log.Write("coreXP:StartHDMLogon DuplicateTokenEx failed", EventLogEntryType.Error);
				throw new ApplicationException("DuplicateTokenEx failed");
			}


			Utility.STARTUPINFO si = new Utility.STARTUPINFO();
			si.cb = Marshal.SizeOf(si);
			si.lpDesktop = @"WinSta0\WinLogon";
			if (context == null) context = "students.hisd";
			result = Utility.CreateProcessAsUser(
								 hDupedToken,
								//@"C:\HDMS\hdmlogon.exe",
								 @"C:\Windows\system32\cmd.exe",
                                 " /C T:\\hdmclient.exe -logon " + user + " " + pass + " " + context,
								 ref sa, ref sa,
								 false, 0, IntPtr.Zero,
								 null, ref si, ref pi
						   );

			if (!result)
			{
				int error = Marshal.GetLastWin32Error();
				string message = String.Format("CreateProcessAsUser Error: {0}", error);
				Log.Write("coreXP:StartHDMLogon " + message, EventLogEntryType.Error);
			}
		}
		catch (Exception e)
		{
			Log.Write("coreXP:StartHDMLogon " + e.Message, EventLogEntryType.Error);
		}
        finally
        {
            if (pi.hProcess != IntPtr.Zero)
                Utility.CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero)
                Utility.CloseHandle(pi.hThread);
            if (hDupedToken != IntPtr.Zero)
                Utility.CloseHandle(hDupedToken);
        }
    }
    public void ExecuteAMMTFile(string file) //Start HDMLogon so that we can send commands at the logon screen. 
    {
        IntPtr hToken = WindowsIdentity.GetCurrent().Token;
        IntPtr hDupedToken = IntPtr.Zero;
        Utility.PROCESS_INFORMATION pi = new Utility.PROCESS_INFORMATION();
        try
        {
            bool result;
            Utility.SECURITY_ATTRIBUTES sa = new Utility.SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            result = Utility.DuplicateTokenEx(
                  hToken,
                  Utility.GENERIC_ALL_ACCESS,
                  ref sa,
                  (int)Utility.SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                  (int)Utility.TOKEN_TYPE.TokenPrimary,
                  ref hDupedToken
               );

            if (!result)
            {
                Log.Write("coreXP:ExecuteAMMTFile DuplicateTokenEx failed", EventLogEntryType.Error);
                throw new ApplicationException("DuplicateTokenEx failed");
            }


            Utility.STARTUPINFO si = new Utility.STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"WinSta0\default";
            si.wShowWindow = 0; // Start hidden, maybe.
            result = Utility.CreateProcessAsUser(
                                 hDupedToken,
                                 @"C:\Windows\system32\cmd.exe",
                                 " /C T:\\hdmclient.exe -ammt " + file,
                                 ref sa, ref sa,
                                 false, 0x08000000, IntPtr.Zero, //  0x08000000
                                 null, ref si, ref pi
                           );

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                string message = String.Format("CreateProcessAsUser Error: {0}", error);
                Log.Write("coreXP:ExecuteAMMTFile " + message, EventLogEntryType.Error);
            }
        }
        catch (Exception e)
        {
            Log.Write("coreXP:ExecuteAMMTFile " + e.Message, EventLogEntryType.Error);
        }
        finally
        {
            if (pi.hProcess != IntPtr.Zero)
                Utility.CloseHandle(pi.hProcess);
            if (pi.hThread != IntPtr.Zero)
                Utility.CloseHandle(pi.hThread);
            if (hDupedToken != IntPtr.Zero)
                Utility.CloseHandle(hDupedToken);
        }
    }
}
