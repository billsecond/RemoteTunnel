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
        private HostPort? _pointALocalHostPort; // host and port information of Point A Local Server        

        public LocalListenServer(HostPort? hostPort, int port)
        {
            this._pointALocalHostPort = hostPort;
            this.port = (ushort)port;
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


                LocalListenEndpoint item;
                if (_pointALocalHostPort != null)
                    item = new LocalListenEndpoint(tcpClient, ((HostPort)_pointALocalHostPort).Port, ((HostPort)_pointALocalHostPort).Host);                
                else
                    item = new LocalListenEndpoint(tcpClient);
                Logger.WriteLineLog(string.Format("Received Client Connection Request from {1} at {0}...", DateTime.Now, tcpClient.Client.RemoteEndPoint));
                //Create New Client on Point A side which will be connected to this endpoint
                _pointAListenServer.StartNewPointAClient(item);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Logger.WriteLineLog(string.Format("Local Listen Server: An error occurred at {0}, error message: {1}, stack trace:{2}", DateTime.Now, ex.Message, ex.StackTrace));

            }
        }

    }
}
