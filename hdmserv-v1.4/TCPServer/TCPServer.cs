using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace TCPServer
{
	public class TCPServ
	{
		public delegate void ClientEventHandler(object sender, EventArgs args);
		private readonly object _EventLock = new object();
		private ClientEventHandler _ClientHandle;
		public Thread t;
		private TcpListener tcpListener;
		private int _port;
		public TCPServ(int p)
		{
			this._port = p;
			t = new Thread(ListenCon);
			t.IsBackground = true;
		}
		public void Start()
		{
			t.Start();
		}
		public void Stop()
		{
			t.Abort();
		}
		private void ListenCon()
		{
			tcpListener = new TcpListener(IPAddress.Any, this._port);
			tcpListener.Start();

            try
            {
                while (true)
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    if (((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() == "10.1.9.121" || ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() == "192.168.0.199" || ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString() == "127.0.0.1")
                    {
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        clientThread.IsBackground = true;
                        clientThread.Start(client);
                    }
                    else
                    {
                        client.Close();
                    }
                }
            }
            catch
            {
                tcpListener.Stop();
                tcpListener = new TcpListener(IPAddress.Any, this._port);
                tcpListener.Start();
            }
		}
		public event ClientEventHandler newClient
		{
			add
			{
				lock (this._EventLock)
				{
					this._ClientHandle = (ClientEventHandler)Delegate.Combine(this._ClientHandle, value);
				}
			}
			remove
			{
				lock (this._EventLock)
				{
					this._ClientHandle = (ClientEventHandler)Delegate.Remove(this._ClientHandle, value);
				}
			}
		}
		private void HandleClientComm(object client)
		{
			clientStream cs = new clientStream();
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();
			StreamReader sr = new StreamReader(clientStream);
			StreamWriter sw = new StreamWriter(clientStream);
			sw.AutoFlush = true;
			cs.reader = sr;
			cs.writer = sw;
			this._ClientHandle(cs, EventArgs.Empty);
			sw.Close();
			sr.Close();
			tcpClient.Close();
		}
	}
}
