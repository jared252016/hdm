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
namespace hdmclient
{
    public class DataConn
    {

        #region Class Init
        private string ComputerName = System.Environment.MachineName;
        private hdmLog Log;
        private StreamWriter _tdb;
        private string _tdb_content;
        private List<string> _tdb_temp = new List<string>();
        private bool _tdb_status = false;
        private string _tdb_file = Path.GetDirectoryName(Application.ExecutablePath) + @"/hdmclient.tdb";
        public DataConn()
        {
            Log = new hdmLog("hdmclient");

            OpenTDB();
        }
        #endregion

        #region TDB_Core
        private void OpenTDB()
        {

            try
            {
                if (!File.Exists(_tdb_file))
                {
                    File.Create(_tdb_file);
                }

                WaitForFile(_tdb_file);

                _tdb_content = File.ReadAllText(_tdb_file);

                WaitForFile(_tdb_file);

                _tdb = File.AppendText(_tdb_file);

                _tdb_status = true;

                if (_tdb_temp.Count > 0)
                {
                    foreach (string t in _tdb_temp)
                    {
                        AppendTDB(t);
                    }
                    _tdb_temp.Clear();
                }

            }
            catch (Exception ex)
            {
                Log.Write(ex.Message);
                _tdb_status = false;
            }

        }
        public void AppendTDB(string t)
        {
            try
            {
                if (!_tdb_content.Contains(t))
                {
                    if (_tdb_status == true)
                    {
                        _tdb.WriteLine(t);
                        _tdb_content += t + "\n\r";
                        _tdb.Flush();
                    }
                    else
                    {
                        _tdb_temp.Add(t);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }
        }
        public void CloseTDB()
        {
            _tdb_status = false;
            _tdb_content = "";
            try
            {
                _tdb.Close();
            }
            catch { }
        }
        public bool WaitForFile(string fullPath)
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
                    System.Threading.Thread.Sleep(500);
                }
            }
            return true;
        }
        #endregion
        
        #region UDP_Core
        public static void SendUDP(byte[] b)
        {
            UdpClient udpc = new UdpClient();
            udpc.Connect(IPAddress.Parse("10.1.9.121"), 5626);
            udpc.Send(b, b.Length);
        }
        #endregion

    }
}
