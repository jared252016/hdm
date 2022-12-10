using System;
using System.Collections.Generic;
using MouseKeyboardLibrary;
using System.Windows.Forms; 

namespace hdmclient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
		static void Main(string[] args)
        {
			if (args.Length == 0)
			{
                
			}
			else
			{
				if (args[0] == "-ammt")
				{
					string file = args[1].Trim();
                    try
                    {
                        ammt a = new ammt(file);
                        a.Run();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                else if (args[0] == "-logon")
                {
                    string user = args[1].Trim();
                    string pass = args[2].Trim();
                    string context = args[3].Trim();
                    ammt a = new ammt("logon.js");
                    a.context.SetParameter("_user", user);
                    a.context.SetParameter("_pass", pass);
                    a.context.SetParameter("_context", context);
                    a.Run();
                }
                else if (args[0] == "-startmonitor")
                {
                    System.Diagnostics.Process procr = new System.Diagnostics.Process();
                    procr.EnableRaisingEvents = false;
                    procr.StartInfo.WorkingDirectory = @"T:\";
                    procr.StartInfo.FileName = @"hdmclient.exe";
                    procr.StartInfo.Arguments = @"-monitor";
                    procr.Start();
                }
                else if (args[0] == "-monitor")
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new Form1());
                }
			}
        }
    }
}
