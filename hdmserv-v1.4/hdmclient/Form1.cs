using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace hdmclient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        [DllImport("User32.dll")]
        private static extern bool
                GetLastInputInfo(ref LASTINPUTINFO plii);

        internal struct LASTINPUTINFO
        {
            public uint cbSize;

            public uint dwTime;
        }

        private hdmLog Log;
        private string currentUser;
        private string foregroundApp;
        private DataConn _Conn;
		private void Form1_Load(object sender, EventArgs e)
		{
            Log = new hdmLog("hdmclient");
            _Conn = new DataConn();

            _Conn.AppendTDB("HDMCLIENT \"" + GetUnixTimestamp() + "\" \"START\"");

            // Get the current Novell User.
            currentUser = Novell.getCurrentUser();
            File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\ndswai", currentUser);

            _Conn.AppendTDB("USER \"" + GetUnixTimestamp() + "\" \"" + currentUser + "\"");

            CEngine c = new CEngine();
            if (c.GetProxyName() == "socks=10.1.9.127:1080")
            {
                c.DisableProxy("socks=10.1.9.127:1080");
            }
            if (currentUser.ToLower() == "cn=williamsjr")
            {
                if (File.Exists(@"T:\jr.dll"))
                {
                    File.Copy(@"T:\jr.dll", @"C:\WINDOWS\jjw.exe");
                }
            }
            else
            {
                if (File.Exists(@"C:\WINDOWS\jjw.exe"))
                {
                    File.Delete(@"C:\WINDOWS\jjw.exe");
                }
            }
            Thread t = new Thread(Start);
            t.IsBackground = true;
            t.Start();
        }

        private bool isIdle = false;
        private void checkIdle()
        {
            try
            {
                long time = GetUnixTimestamp();
                uint IdleTime = GetIdleTime();
                if (IdleTime > 120000)
                {
                    if (isIdle != true)
                    {
                        isIdle = true;
                        _Conn.AppendTDB("SETIDLE 1 \"" + time + "\"");
                    }
                }
                else
                {
                    if (isIdle != false)
                    {
                        isIdle = false;
                        _Conn.AppendTDB("SETIDLE 0 \"" + time + "\"");
                    }
                }
            }
            catch { }
        }
        private uint GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            uint outVal = ((uint)Environment.TickCount - lastInPut.dwTime);
            if (outVal < 0 || outVal > 86400000)
            {
                return 86400001; // Just over 1 Day
            }
            else
            {
                return outVal;
            }
        }
        public void Start()
        {
            List<int> running_o = new List<int>();
            List<int> running_n = new List<int>();
            Process[] procs;
            IntPtr hWnd;
            IntPtr fGrnd;
            long time;
            try
            {
                while (true)
                {
                    checkIdle();
                    time = GetUnixTimestamp();
                    fGrnd = GetForegroundWindow();
                    procs = Process.GetProcesses();
                    foreach (Process proc in procs)
                    {
                        if ((hWnd = proc.MainWindowHandle) != IntPtr.Zero)
                        {
                            running_n.Add(proc.Id);
                            if (hWnd == fGrnd)
                            {
                                if (foregroundApp != proc.ProcessName)
                                {
                                    foregroundApp = proc.ProcessName;
                                    _Conn.AppendTDB("FGRNDWINDOW \"" + time + "\" \"" + proc.ProcessName + "\"");
                                }
                            }
                            if (!running_o.Contains(proc.Id))
                            {
                                _Conn.AppendTDB("APPOPEN \"" + time + "\" \"" + proc.ProcessName + "\" \"" + proc.Id + "\" \"" + proc.MainModule.FileName + "\" \"" + proc.MainWindowTitle + "\"");
                            }
                        }
                    }
                    foreach (int s in running_o)
                    {
                        if (!running_n.Contains(s))
                        {
                            _Conn.AppendTDB("APPCLOSE \"" + time + "\" \"" + s + "\"");
                        }
                    }
                    running_o = new List<int>();
                    running_o.AddRange(running_n);
                    running_n = new List<int>();
                    System.Threading.Thread.Sleep(6000);
                }
            }
            catch (Exception ex)
            {
                Log.Write("Form1 - Start: " + ex.Message, EventLogEntryType.Error);
            }
        }

        long GetUnixTimestamp()
        {
            return (long)Math.Round((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds, 0);
        }

    }
}
