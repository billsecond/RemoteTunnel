using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Remote.Server.Core
{
    internal class LocalListenServer
    {
        private ushort port;
        private TcpListener _Listener;
        private bool IsStarting;
        private PointAListenServer _pointAListenServer;
        public Hashtable tcpClientList
        {
            get;
            private set;
        }
        public LocalListenServer(ushort port)
        {
            this.port = port;
            tcpClientList = new Hashtable();
            this.IsStarting = false;

        }
        public void Start(PointAListenServer s)
        {
            _pointAListenServer = s;
            if (!this.IsStarting)
            {
                //no exception handling
                this._Listener = new TcpListener(IPAddress.Any, port);
                this._Listener.Start();
                this._Listener.BeginAcceptTcpClient(this.OnBeginAcceptTcpClient, this._Listener);
                this.IsStarting = true;
                Logger.WriteLineLog(string.Format("Local Listener has been started at {0}.... on {1}", DateTime.Now, port));
            }
        }

        internal void Stop()
        {
            if (this.IsStarting)
            {
                this.IsStarting = false;
                this._Listener.Stop();
                this._Listener = null;
                this._pointAListenServer = null;
                Logger.WriteLineLog(string.Format("Local Listener has been stopped at {0} ", DateTime.Now));
            }
        }
        private void OnBeginAcceptTcpClient(IAsyncResult async)
        {
            TcpListener listener = async.AsyncState as TcpListener;            
            try
            {
                TcpClient tcpClient = listener.EndAcceptTcpClient(async);
                if (this.IsStarting) listener.BeginAcceptTcpClient(this.OnBeginAcceptTcpClient, listener);
                if (!_pointAListenServer.IsStarting) {
                    tcpClient.Close();
                    return;
                }

                String hashKey = Guid.NewGuid().ToString(); ;
                this.tcpClientList.Add(hashKey, tcpClient);
                
                Logger.WriteLineLog(string.Format("Received Client Connection Request from {1} at {0}...", DateTime.Now, tcpClient.Client.RemoteEndPoint));
                //Create Socks5Endpoint Client
                if (_pointAListenServer.StartNewPointAClient(hashKey))
                {
                    Logger.WriteLineLog(string.Format("Start New Point A Client Request has been sent with hashKey {0}", hashKey));
                }

            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Logger.WriteLineLog(string.Format("Local Listen Server: An error occurred at {0}, error message: {1}, stack trace:{2}", DateTime.Now, ex.Message, ex.StackTrace));

            }
        }

    }
}
