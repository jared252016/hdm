using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace hdmserv
{
    class SystemMonitor
    {
        private hdmLog Log;
		private coreXP parent;
        private DataConn _Conn;
        
		private const int WM_QUERYENDSESSION=0x0011;
        public int state = -1; // 1 Online 2 Logged in.

		public SystemMonitor(coreXP o, DataConn conn)
		{
			_Conn = conn;
			parent = o;
            Log = new hdmLog("hdmserv_monitor");
		}
        public void Start()
        {
            int s60 = 0;
            while (true)
            {
                checkLoginState();
                checkHDMClientState();
                System.Threading.Thread.Sleep(10000);
                s60 += 10000;
                if (s60 >= 90000)
                {
                    // It's been 90 seconds, check the size of tdb.
                    if (_Conn.getTDBSize() >= 500 * 1024)
                    {
                        _Conn.SubmitTDB();
                    }
                    s60 = 0;
                }
            }
        }
		public bool IsProcessOpen(string name)
		{
			foreach (Process clsProcess in Process.GetProcesses()) {
				if (clsProcess.ProcessName.Contains(name))
				{
					return true;
				}
			}
			return false;
		}
        private void checkLoginState()
        {
            try
            {
                bool popen = IsProcessOpen("explorer") || IsProcessOpen("explorer.exe");
                if (popen && (state == 1 || state == 0))
                {
                    parent.onLogin();
                }
                else if (!popen && (state == 2 || state == 0))
                {
                    parent.onLogoff();
                }
            }
            catch { }
        }
        private void checkHDMClientState()
        {
            if (!IsProcessOpen("hdmclient") && (IsProcessOpen("explorer") || IsProcessOpen("explorer.exe")))
            {
                _Conn.StartHDMClient();
            }
        }
    }
}
