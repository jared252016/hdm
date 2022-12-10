namespace AsyncPipes
{
    using System;
    using System.Collections.Generic;
    using System.IO.Pipes;
    using System.Threading;
    using System.Runtime.InteropServices;

    public class NamedPipeStreamClient : NamedPipeStreamBase
    {
        private ManualResetEvent _ConnectGate;
        private readonly object _InstanceLock;
        private Queue<byte[]> _PendingMessages;
        private readonly object _QueueLock;
        private PipeStream _Stream;
		private Thread ClientThread;

        public NamedPipeStreamClient(string pipeName) : base(pipeName)
        {
            this._InstanceLock = new object();
            this._QueueLock = new object();
            this._ConnectGate = new ManualResetEvent(false);
            this.StartTryConnect();
        }
		public void Abort()
		{
			base.Disconnect();
			try
			{
				this._Stream.Close();
			}
			catch
			{
				try
				{
					if (ClientThread.IsAlive)
					{
						ClientThread.Abort();
					}
				}
				catch
				{

				}
			}
		}
        public override void Disconnect()
        {
            lock (this._InstanceLock)
            {
                base.Disconnect();
				try
				{
					this._Stream.Close();
				}
				catch
				{
					try
					{
						if (ClientThread.IsAlive)
						{
							ClientThread.Abort();
						}
					}
					catch
					{

					}
				}
            }
        }

        private void EndRead(IAsyncResult result)
        {
            // this can throw an exception when it first starts up, so...
            try
            {

                int length = this._Stream.EndRead(result);
                byte[] asyncState = (byte[]) result.AsyncState;
                if (length > 0)
                {
                    byte[] destinationArray = new byte[length];
                    Array.Copy(asyncState, 0, destinationArray, 0, length);
                    this.OnMessageReceived(new MessageEventArgs(destinationArray));
                }
                lock (this._InstanceLock)
                {
                    if (this._Stream.IsConnected)
                    {
                        this._Stream.BeginRead(asyncState, 0, NamedPipeStreamBase.BUFFER_LENGTH, new AsyncCallback(this.EndRead), asyncState);
                    }
                }
            }
            catch
            {
            }

        }

        private void EndSendMessage(IAsyncResult result)
        {
            try
            {
                lock (this._InstanceLock)
                {
                    this._Stream.EndWrite(result);
                    this._Stream.Flush();
                }
            }
            catch
            {
                this.Disconnect();
                this.StartTryConnect();
            }
        }

        private void EnqueMessage(byte[] message)
        {
            lock (this._QueueLock)
            {
                if (this._PendingMessages == null)
                {
                    this._PendingMessages = new Queue<byte[]>();
                }
                this._PendingMessages.Enqueue(message);
            }
        }

        ~NamedPipeStreamClient()
        {
            this.Dispose(false);
        }

        public override void SendMessage(byte[] message)
        {
            try
            {
                if (this._ConnectGate.WaitOne(100))
                {
                    lock (this._InstanceLock)
                    {
                        if (this._Stream.IsConnected)
                        {
                            message = message ?? new byte[0];
                            this._Stream.BeginWrite(message, 0, message.Length, new AsyncCallback(this.EndSendMessage), null);
                            this._Stream.Flush();
                        }
                        else
                        {
                            this.EnqueMessage(message);
                            this.StartTryConnect();
                        }
                    }
                }
                else
                {
                    this.EnqueMessage(message);
                }
            }
            catch
            {
                this._Stream.Close();
                this.StartTryConnect();
            }
        }

        private void SendQueuedMessages()
        {
            lock (this._QueueLock)
            {
                if (this._PendingMessages != null)
                {
                    while (this._PendingMessages.Count > 0)
                    {
                        this.SendMessage(this._PendingMessages.Dequeue());
                    }
                    this._PendingMessages = null;
                }
            }
        }

        private void StartTryConnect()
        {
            this._ConnectGate.Reset();
			ClientThread = new Thread(new ThreadStart(this.TryConnect));
			ClientThread.Name = "NamedPipeStreamClientConnection";
			ClientThread.IsBackground = true;
			ClientThread.Start();
        }

        private void TryConnect()
        {
            this._ConnectGate.Reset();
            lock (this._InstanceLock)
            {
                if (base.PipeName.Contains("\\"))
                {
                    string serverName = base.PipeName.Substring(base.PipeName.IndexOf("\\") +1, base.PipeName.LastIndexOf("\\") - 1);
                    string pipeName = base.PipeName.Substring(base.PipeName.LastIndexOf("\\") + 1);
                    this._Stream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut,
                                                             PipeOptions.Asynchronous);
                }
                else
                    this._Stream = new NamedPipeClientStream(".", base.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                while (!this._Stream.IsConnected)
                {
                    try
                    {
                        ((NamedPipeClientStream)this._Stream).Connect(100);						
                    }
                    catch
                    {
						Thread.Sleep(10000);  // Sleep between attempts, or use a ResetEvent / EventWaitHandle or similar
                    }
                }

                this._Stream.ReadMode = PipeTransmissionMode.Message;
                byte[] buffer = new byte[NamedPipeStreamBase.BUFFER_LENGTH];
                this._Stream.BeginRead(buffer, 0, NamedPipeStreamBase.BUFFER_LENGTH, new AsyncCallback(this.EndRead), buffer);
                this._ConnectGate.Set();
                this.SendQueuedMessages();
            }
        }
        [return: MarshalAs(UnmanagedType.Bool)] [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern bool WaitNamedPipe(string name, int timeout);
        static public bool NamedPipeDoesNotExist(string pipeName)
        {
            try
            {
                int timeout = 0;
                string normalizedPath = System.IO.Path.GetFullPath(
                 string.Format(@"\\.\pipe\{0}", pipeName));
                bool exists = WaitNamedPipe(normalizedPath, timeout);
                if (!exists)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 0) // pipe does not exist
                        return true;
                    else if (error == 2) // win32 error code for file not found
                        return true;
                    // all other errors indicate other issues
                }
                return false;
            }
            catch
            {
                return true; // assume it exists
            }
        }
        public bool IsConnected
        {
            get
            {
                lock (this._InstanceLock)
                {
                    return this._Stream.IsConnected;
                }
            }
        }
    }
}
