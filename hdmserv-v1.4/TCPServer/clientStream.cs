using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TCPServer
{
	public class clientStream
	{
		public StreamReader reader;
		public StreamWriter writer;
        public void Close()
        {
            try
            {
                reader.Close();
            }
            catch { }
            try
            {
                writer.Close();
            }
            catch { }
        }
	}
}
