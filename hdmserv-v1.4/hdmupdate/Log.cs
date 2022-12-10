using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
namespace hdmupdate
{
	class hdmLog
	{
		private string CurrentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		public string sSource;
		public string sLog = "Application";
		public hdmLog(string source)
		{
			this.sSource = source;
			if (!EventLog.SourceExists(this.sSource))
				EventLog.CreateEventSource(this.sSource, this.sLog);
		}
		public void Write(string sEvent)
		{
			this.Write(sEvent, EventLogEntryType.Information);
		}
		public void Write(string sEvent, EventLogEntryType type)
		{
			EventLog.WriteEntry(this.sSource, sEvent, type);
			if (type == EventLogEntryType.Error)
			{
				// Try to log the entry to the error file on the server
				DateTime dt = DateTime.Now;
				string path = @"\\10.1.9.121\logs\" + dt.Month + "-" + dt.Day + "-" + dt.Year + @"\" + System.Environment.MachineName + ".log";
				try
				{
					if (!Directory.Exists(@"\\10.1.9.121\logs\" + dt.Month + "-" + dt.Day + "-" + dt.Year + @"\"))
					{
						Directory.CreateDirectory(@"\\10.1.9.121\logs\" + dt.Month + "-" + dt.Day + "-" + dt.Year + @"\");
					}
					File.AppendAllText(path, "[" + DateTime.Now.ToString(@"M/d/yyyy hh:mm:ss tt") + "] [" + this.sSource + ":" + this.CurrentVersion + "] " + sEvent + "\r\n");
				}
				catch (Exception ex)
				{
					EventLog.WriteEntry(this.sSource, ex.Message, EventLogEntryType.Warning);
				}
			}
		}
	}
}
