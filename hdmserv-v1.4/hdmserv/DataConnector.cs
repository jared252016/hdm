using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Xml;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security;
using Microsoft.Win32;
using TaskScheduler;
namespace hdmserv
{
	public enum ConnMode
	{
		OFFLINE = 0,
		MYSQL = 1,
		HTTP = 2
	}
	public struct DataConnResponse
	{
		public string Query;
		public int Status;
		public string ErrorText;
		public int Count;
		public int AffectedRows;
		public Dictionary<string, string>[] Response;
	}
	public class DataConnXMLField
	{
		private string _name;
		private string _value;
		public DataConnXMLField(string _n, string _v)
		{
			this._name = _n;
			this._value = _v;
		}
		public string Name
		{
			get
			{
				return (_name);
			}
			set
			{
				_name = value;
			}
		}
		public string Value
		{
			get
			{
				return (_value);
			}
			set
			{
				_value = value;
			}
		}
	}
	public class DataConn
	{

		#region Class Init
		private string ComputerName = System.Environment.MachineName;
		private hdmLog Log;
        private StreamWriter _tdb = null;
        private string _tdb_content = null;
        private List<string> _tdb_temp = null;
        private bool _tdb_status = false;
        private string _tdb_file = Path.GetDirectoryName(Application.ExecutablePath) + @"/hdmserv.tdb";
        private string _hdmclient_tdb_file = Path.GetDirectoryName(Application.ExecutablePath) + @"/hdmclient.tdb";
		public DataConn()
		{
			Log = new hdmLog("hdmserv");
            _tdb_temp = new List<string>();
            OpenTDB();
		}
		#endregion

		#region HTTP_Core
		private bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
		{
			return true;
		}

		public DataConnResponse newHTTPCommand(string sql)
		{
			try
			{
			ASCIIEncoding encoding = new ASCIIEncoding();
			string postData = "sql=" + System.Web.HttpUtility.UrlEncode(sql) + "&md5=" + coreXP.MD5(sql);

			byte[] data = encoding.GetBytes(postData);

			ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

			HttpWebRequest reqFP = (HttpWebRequest)HttpWebRequest.Create("https://208.180.35.67:3400/");

			reqFP.ProtocolVersion = HttpVersion.Version10;
			reqFP.AllowAutoRedirect = true;


			reqFP.Method = "POST";
			//reqFP.Headers.Add("host", "hdms.hudsonisd.org");
			//reqFP.Headers.Add("User-Agent", "HDMS");
			reqFP.ContentType = "application/x-www-form-urlencoded";

			reqFP.Credentials = new NetworkCredential("inventory_55", "e93ff0b66906d5fd70a27f67bfa4e15f831853bb");
			reqFP.PreAuthenticate = true;

			Stream newStream = reqFP.GetRequestStream();
			newStream.Write(data, 0, data.Length);
			newStream.Close();

			HttpWebResponse response = (HttpWebResponse)reqFP.GetResponse();

			StreamReader resStream = new StreamReader(response.GetResponseStream());

			string xml = resStream.ReadToEnd();

			var doc = new System.Xml.XmlDocument();

			if (File.Exists("last.txt")) File.Delete("last.txt");

			// create a writer and open the file
			TextWriter tw = new StreamWriter("last.txt");

			// write a line of text to the file
			tw.WriteLine(xml);

			// close the stream
			tw.Close();

			doc.LoadXml(xml);

			DataConnResponse xmlr = new DataConnResponse();
			xmlr.Query = doc.SelectSingleNode("/Object/Query").InnerText;
			xmlr.Status = int.Parse(doc.SelectSingleNode("/Object/Status").InnerText);
			xmlr.ErrorText = doc.SelectSingleNode("/Object/ErrorText").InnerText;
			xmlr.Count = int.Parse(doc.SelectSingleNode("/Object/Response").Attributes.GetNamedItem("count").Value);
			xmlr.AffectedRows = int.Parse(doc.SelectSingleNode("/Object/Response").Attributes.GetNamedItem("affected").Value);

			Dictionary<string, string>[] r = new Dictionary<string, string>[xmlr.Count];
			int i = 0;
			foreach (XmlNode snode in doc.SelectNodes("/Object/Response/Result"))
			{
				r[i] = new Dictionary<string, string>();
				foreach (XmlNode node in snode.SelectNodes("*"))
				{
					r[i].Add(node.Name, node.InnerText);
				}
				++i;
			}

			xmlr.Response = r;
			return xmlr;
			}
			catch (Exception ex)
			{
				Console.Write("DataConnector:newHTTPCommand " + ex.Message);
				return new DataConnResponse();
			}
		}

		public bool getHTTPStatus(string strServer)
		{
			try
			{
				HttpWebRequest reqFP = (HttpWebRequest)HttpWebRequest.Create(strServer);
				reqFP.Method = "HEAD";
				HttpWebResponse rspFP = (HttpWebResponse)reqFP.GetResponse();
				if (HttpStatusCode.OK == rspFP.StatusCode)
				{
					rspFP.Close();
					return true;
				}
				else
				{
					rspFP.Close();
					return false;
				}
			}
			catch (WebException)
			{
				return false;
			}
		}

		#endregion

        #region TDB_Core
        private void OpenTDB()
        {

            string part = "0";
            try
            {
                if (!File.Exists(_tdb_file))
                {
                    part = "0A";
                    File.Create(_tdb_file);
                    _tdb_content = "";
                    part = "1A";
                }
                else
                {
                    part = "0B";
                    WaitForFile(_tdb_file);

                    _tdb_content = File.ReadAllText(_tdb_file);
                    part = "1B";
                }
                System.Threading.Thread.Sleep(5000);

                WaitForFile(_tdb_file);

                part = "2";
                try
                {
                    _tdb = File.AppendText(_tdb_file);
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(10000);
                    if(_tdb_status != true)
                        OpenTDB();
                }
                part = "3";
                _tdb_status = true;

                if (_tdb_temp.Count > 0)
                {
                    foreach (string t in _tdb_temp)
                    {
                        AppendTDB(t);
                    }
                    _tdb_temp.Clear();
                }
                part = "4";

            }
            catch (Exception ex)
            {
                Log.Write("DataConnector.cs > OpenTDB (_tdb - " + part +"): " + ex.Message, EventLogEntryType.Error);
                _tdb_status = false;
            }

        }
        public void AppendTDB(string t)
        {
            try
            {
                if (!_tdb_content.Contains(t))
                {
                    if (_tdb_status == true && _tdb != null)
                    {
                        try
                        {
                            _tdb.WriteLine(t);
                            _tdb_content += t + "\n\r";
                            _tdb.Flush();
                        }
                        catch (Exception ex)
                        {
                            Log.Write("DataConnector.cs > AppendTDB (_tdb): " + ex.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            _tdb_temp.Add(t);
                        }
                        catch (Exception ex)
                        {
                            Log.Write("DataConnector.cs > AppendTDB (_tdb_temp): " + ex.Message);
                        }
                   } 
                }
                if (_tdb == null)
                {
                    try
                    {
                        OpenTDB();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log.Write("DataConnector.cs > AppendTDB (_tdb_content): " + ex.Message);
            }
        }
        public void SubmitTDB(bool shutdown = false)
        {
            try
            {
                // Close the file so we can access it.
                CloseTDB();
                System.Threading.Thread.Sleep(5000);
                // Close HDMClient so we can get access to that one.
                foreach (Process clsProcess in Process.GetProcesses())
                {
                    if (clsProcess.ProcessName.StartsWith("hdmclient"))
                    {
                        clsProcess.Kill();
                    }
                }
                WaitForFile(_hdmclient_tdb_file);
                System.Threading.Thread.Sleep(10000);

                // Upload via HTTP
                HttpUploadFile("http://10.1.9.121/hdms/submit.php", _tdb_file, "tdbfile", "text/html", null);
                HttpUploadFile("http://10.1.9.121/hdms/submit.php", _hdmclient_tdb_file, "tdbfile", "text/html", null);

                System.Threading.Thread.Sleep(10000);

                // Delete the files after submission.
                File.Delete(_tdb_file);
                File.Delete(_hdmclient_tdb_file);

                System.Threading.Thread.Sleep(5000);

                if (shutdown != true)
                {
                    // Start HDMClient back up by adding a scheduled task
                    StartHDMClient();
                    // Open the TDB database back up.
                    OpenTDB();
                }
            }
            catch (Exception ex)
            {
                Log.Write("DataConnector.cs > SubmitTDB: " + ex.Message);
            }
        }
        public void CloseTDB()
        {
            try
            {
                _tdb.Close();
            }
            catch { }

            _tdb_status = false;
            _tdb_content = "";
        }
        public void HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

            Stream rs = wr.GetRequestStream();

            if (nvc == null)
            {
                nvc = new NameValueCollection();
                nvc.Add("compName", ComputerName);
            }
            else
            {
                nvc.Add("compName", ComputerName);
            }

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                Log.Write(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
            }
            catch (Exception ex)
            {
                Log.Write("Error uploading file " + ex);
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;
            }
        }
        public void StartHDMClient() 
        {
            //Get a ScheduledTasks object for the local computer.
            ScheduledTasks st = null;
            Task t = null;
            try
            {
                st = new ScheduledTasks();

                // Create a task
                try
                {
                    t = st.CreateTask("HDMC");
                }
                catch (ArgumentException)
                {
                    try
                    {
                        t = st.OpenTask("HDMC");
                        if (t.Status == TaskStatus.NoMoreRuns || t.Status == TaskStatus.NoTriggerTime || t.Status == TaskStatus.NotScheduled)
                        {
                            t.Close();
                            t = null;
                            st.DeleteTask("HDMC");
                        }
                        else
                        {
                            t.Close();
                            t = null;
                        }
                    }
                    catch { }
                    finally
                    {
                        if (st != null)
                        {
                            st.Dispose();
                        }
                    }
                    return;
                }

                // Fill in the program info
                t.ApplicationName = @"T:\hdmclient.exe";
                t.Parameters = "-startmonitor";
                t.Comment = "";

                t.Flags = TaskFlags.RunOnlyIfLoggedOn | TaskFlags.DeleteWhenDone;

                t.SetAccountInformation(System.Environment.MachineName + @"\User", (string)null);

                RunOnceTrigger trig = new RunOnceTrigger(DateTime.Now.AddMinutes(1));
                t.Triggers.Add(trig);
                t.Save();
            }
            catch (Exception ex)
            {
                Log.Write("DataConnector > StartHDMClient: " + ex.Message, EventLogEntryType.Error);
            }
            finally
            {
                try
                {
                    if (t != null)
                        t.Close();
                }
                catch { }
                try
                {
                    if(st != null)
                        st.Dispose();
                }
                catch { }
            }
        }

        public long getTDBSize()
        {
            FileInfo tdb1 = new FileInfo(_tdb_file);
            FileInfo tdb2 = new FileInfo(_hdmclient_tdb_file);
            return tdb1.Length + tdb2.Length;
        }
        #endregion
        
        #region UDP_Core
        public static void SendUDP(byte[] b)
        {
            try
            {
                UdpClient udpc = new UdpClient();
                udpc.Connect(IPAddress.Parse("10.1.9.121"), 5626);
                udpc.Send(b, b.Length);
            }
            catch (Exception ex)
            {
                hdmLog Log = new hdmLog("hdmserv");
                Log.Write("DataConnector > SendUDP: " + ex.Message);
            }
        }
#endregion

		#region CoreXp Functions
		public void updateStatus(int status)
		{
            // Depricated in v1.4
			//this.newExecuteCommand("UPDATE `inventory` SET `Status` = '" + status.ToString() + "' WHERE `Name` = '" + ComputerName + "' LIMIT 1;");
		}
		public void Initialize()
		{
			
			int Status = coreXP.IsProcessOpen("explorer")?2:1;
			string CurrentVersion = Inventory.GetCurrentVersion();
			try
			{
				int isFrozen = DFController.isFrozen() ? 1 : 0;

                // v1.3 Method
				//this.newExecuteCommand("INSERT INTO `inventory` (`Name`, `Type`, `Status`, `isFrozen`, `HDMSVersion`, `LastUpdated`) VALUES ('" + ComputerName + "', 'DESKTOP', '" + Status + "', '" + isFrozen + "', '" + CurrentVersion + "', NOW()) ON DUPLICATE KEY UPDATE `Status` = '" + Status + "', `isFrozen` = '" + isFrozen + "', `HDMSVersion` = '" + CurrentVersion + "', `LastUpdated` = NOW();");

                // v1.4 Method
                // Startup message. Contains Type, Login State(0/1), Frozen State (0/1/2), 1, length of cname, followed by the computer name, then the version number. 
                byte[] name = Encoding.ASCII.GetBytes(ComputerName);
                byte[] version = Encoding.ASCII.GetBytes(CurrentVersion);
                byte[] prefix = new byte[] { 1, (byte)Status, (byte)isFrozen, (byte) 1,  (byte) (name.Length)};

                byte[] all = new byte[prefix.Length + name.Length + version.Length];
                Array.Copy(prefix, 0, all, 0, prefix.Length);
                Array.Copy(name, 0, all, prefix.Length, name.Length);
                Array.Copy(version, 0, all, prefix.Length + name.Length, version.Length);

                SendUDP(all);
			}
			catch (Exception ex)
			{
				Log.Write("DataConnector:Initialize " + ex.Message, System.Diagnostics.EventLogEntryType.Error);
			}
		}
		public bool CheckForUpdates(out string v)
		{
			try
			{
				// v1.3 Old Method
                #region Old Method
                /*
				DataConnResponse r;
				if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\use_beta"))
				{
					r = this.newQueryCommand("SELECT `Value` FROM `cache` WHERE `Name` = 'HDMSVersion_Beta' LIMIT 1;");
				}
				else
				{
					r = this.newQueryCommand("SELECT `Value` FROM `cache` WHERE `Name` = 'HDMSVersion' LIMIT 1;");
				}
				string version = r.Response[0]["Value"];
				*/
#endregion
                try
                {
                    string path;
                    if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\use_beta"))
                    {
                        path = "http://10.1.9.121/hdms/HDMSVersion_Beta.txt";
                    }
                    else
                    {
                        path = "http://10.1.9.121/hdms/HDMSVersion.txt";
                    }
                    HttpWebRequest reqFP = (HttpWebRequest)HttpWebRequest.Create(path);
                    HttpWebResponse rspFP = (HttpWebResponse)reqFP.GetResponse();
                    StreamReader loResponseStream = new StreamReader(rspFP.GetResponseStream());
                    string version = loResponseStream.ReadToEnd();
                    loResponseStream.Close();
                    rspFP.Close();
                    if (version != null)
                    {
                        if (CompareVersions(version, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()))
                        {
                            v = version;
                            return true;
                        }
                        else
                        {
                            v = null;
                            return false;
                        }
                    }
                    else
                    {
                        v = null;
                        return false;
                    }
                }
                catch (WebException)
                {
                    v = null;
                    return false;
                }
			}
			catch (Exception ex)
			{
				Log.Write("DataConnector:CheckForUpdates " + ex.Message, System.Diagnostics.EventLogEntryType.Error);
				v = null;
				return false;
			}
		}
		public bool CompareVersions(string server_version, string client_version)
		{
			string[] sv = server_version.Split('.');
			string[] cv = client_version.Split('.');

			for (int i = 0; i < 3; i++)
			{
				if (Convert.ToInt32(sv[i]) > Convert.ToInt32(cv[i]))
				{
					return true;
				}
			}
			if (Convert.ToInt32(sv[3]) > Convert.ToInt32(cv[3]) && Convert.ToInt32(sv[2]) >= Convert.ToInt32(cv[2]))
			{
				return true;
			}
			return false;
		}
		#endregion
        bool WaitForFile(string fullPath)
        {
            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    // Attempt to open the file exclusively.
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100))
                    {
                        fs.ReadByte();
                        // If we got this far the file is ready
                        break;
                    }
                }
                catch
                {
                    if (numTries > 20)
                    {
                        return false;
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return true;
        }

    }
}
