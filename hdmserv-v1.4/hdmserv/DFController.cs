using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace hdmserv
{
	class DFController
	{
		private static string _DFC_Path = Path.GetDirectoryName(Application.ExecutablePath) + @"\dfc.exe";
		#region Password
		private static string _DFC_Pass = "lan2tech";
		#endregion

		/*
			0 SUCCESS or Boolean FALSE, for commands returning a Boolean result
			1 Boolean TRUE
			2 ERROR - User does not have administrator rights
			3 ERROR - DFC command not valid on this installation
			4 ERROR - Invalid command
			5 - * ERROR - Internal error executing command
		 */

		
		public static bool isFrozen()
		{
			Process p = Process.Start(_DFC_Path, "get /ISFROZEN");
			p.WaitForExit();
			if(p.ExitCode == 1) {
				return true;
			} else {
				return false;
			}
		}
		public static int RebootThawed() // /BOOTTHAWED
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /BOOTTHAWED");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int RebootThawedLocked() // /THAWLOCKNEXTBOOT
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /THAWLOCKNEXTBOOT");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int RebootThawedNoInput() // /BOOTTHAWEDNOINPUT
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /BOOTTHAWEDNOINPUT");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int RebootFrozen() // /BOOTFROZEN
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /BOOTFROZEN");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int BootThawedNext() // /THAWNEXTBOOT
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /THAWNEXTBOOT");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int BootFrozenNext() // /FREEZENEXTBOOT
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /FREEZENEXTBOOT");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int Lock() // /LOCK
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /LOCK");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		public static int Unlock() // /UNLOCK
		{
			Process p = Process.Start(_DFC_Path, _DFC_Pass + " /UNLOCK");
			p.WaitForExit();
			if (p.ExitCode != 0)
			{
				LogDFCError(p.ExitCode);
			}
			return p.ExitCode;
		}

		private static void LogDFCError(int code) {
			string msg;
			switch (code)
			{
				case 2:
					msg = "User does not have administrator rights";
					break;
				case 3:
					msg = "DFC command not valid on this installation";
					break;
				case 4:
					msg = "Invalid command";
					break;
				case 5:
					msg = "Internal error executing command";
					break;
				default:
					msg = "Error";
					break;
			}
			hdmLog Log = new hdmLog("hdmserv");
			Log.Write("DFController:dfc.exe " + msg, EventLogEntryType.Error);
		}
	}
}
