using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Remote.Agent.Core
{
    // The PortForwardBridge class handles the forwarding of data between a client and an endpoint.
    internal class PortForwardBridge
    {
        // Buffers for storing data received from the client and the endpoint.
        private byte[] _LocalClientBuffer;
        private byte[] _PointBClientBuffer;

        // TcpClient objects for the client and endpoint connections.
        public TcpClient localCLient;
        public TcpClient pointBClient;
        private bool isEncrypted;
        
        // Reference to the PointAClient that initiated this bridge.
        public PointAClient pointAClient;

        // Constructor for initializing a new PortForwardBridge instance.
        public PortForwardBridge(PointAClient a, TcpClient localClient, TcpClient pointBEndpoint, bool isEncrypted)
        {
            this.localCLient = localClient;
            this.pointBClient = pointBEndpoint;            
            this._LocalClientBuffer = new byte[0];
            this._PointBClientBuffer = new byte[0];
            this.isEncrypted = isEncrypted;
            pointAClient = a;
        }

        // Starts the forwarding process between the client and the endpoint.
        public void Start(object state)
        {
            Logger.WriteLineLog($"Port Forward Bridge has been Created at {DateTime.Now} between {localCLient.Client.RemoteEndPoint} and {pointBClient.Client.RemoteEndPoint}");
            _LocalClientBuffer = new byte[localCLient.ReceiveBufferSize];
            _PointBClientBuffer = new byte[pointBClient.ReceiveBufferSize];
            localCLient.Client.BeginReceive(_LocalClientBuffer, 0, _LocalClientBuffer.Length, SocketFlags.None, OnLocalClientReceive, localCLient.Client);
            pointBClient.Client.BeginReceive(_PointBClientBuffer, 0, _PointBClientBuffer.Length, SocketFlags.None, OnPointBClientReceive, pointBClient.Client);
        }

        // Callback method for handling data received from the Local Web Server.
        private void OnLocalClientReceive(IAsyncResult result)
        {
            if (this.pointAClient.IsStarting)
            {
                try
                {
                    Socket socket = result.AsyncState as Socket;
                    SocketError error;
                    int size = socket.EndReceive(result, out error);
                    if (size > 0)
                    {
                        // Apply encrypt when send content to Point B.
                        if (isEncrypted) _LocalClientBuffer = EncryptService.Encrypt(_LocalClientBuffer);
                        
                        // Forward the data to the endpoint.
                        SocketUtils.Send(this.pointBClient.Client, _LocalClientBuffer, 0, size);
                        // Continue receiving data from the client.
                        if (this.pointAClient.IsStarting)
                            localCLient.Client.BeginReceive(_LocalClientBuffer, 0, _LocalClientBuffer.Length, SocketFlags.None, OnLocalClientReceive, localCLient.Client);
                    }
                    else
                    {
                        // Handle client disconnection.
                        this.Close();
                    }
                }
                catch(Exception ex)
                {
                    Logger.WriteLineLog(string.Format("An error occurred at {0}, error message: {1}, Trace:{2}", DateTime.Now, ex.Message, ex.StackTrace));
                    // Close the connection on error.
                    this.Close();
                }
            }
        }

        // Callback method for handling data received from the Point B.
        private void OnPointBClientReceive(IAsyncResult result)
        {
            if (this.pointAClient.IsStarting)
            {
                try
                {
                    Socket socket = result.AsyncState as Socket;
                    SocketError error;
                    int size = socket.EndReceive(result, out error);
                    if (size > 0)
                    {
                        // Apply decrypt when data is received from Point B
                        if(isEncrypted) _PointBClientBuffer = EncryptService.Decrypt(_PointBClientBuffer);                        
                        // Forward the data to the client.
                        SocketUtils.Send(this.localCLient.Client, _PointBClientBuffer, 0, size);
                        // Continue receiving data from the endpoint.
                        if (this.pointAClient.IsStarting)
                            pointBClient.Client.BeginReceive(_PointBClientBuffer, 0, _PointBClientBuffer.Length, SocketFlags.None, OnPointBClientReceive, pointBClient.Client);
                    }
                    else
                    {
                        // Handle endpoint disconnection.
                        this.Close();
                    }
                }
                catch(Exception ex)
                {
                    Logger.WriteLineLog(string.Format("An error occurred at {0}, error message: {1}, Trace:{2}", DateTime.Now, ex.Message, ex.StackTrace));
                    // Close the connection on error.
                    this.Close();
                }
            }
        }

        // Closes the connections to the client and endpoint.
        private void Close()
        {
            if (this.localCLient != null)
            {
                try
                {
                    Logger.WriteLineLog($"Closed Connection to client {localCLient.Client.RemoteEndPoint} at {DateTime.Now} ...");
                    localCLient.Close();
                    localCLient = null;
                }
                catch (System.ObjectDisposedException)
                {
                    this.localCLient = null;
                }
                catch (System.NullReferenceException)
                {
                    this.localCLient = null;
                }
            }
            if (this.pointBClient != null)
            {
                try
                {
                    Logger.WriteLineLog($"Closed Connection to endpoint {pointBClient.Client.RemoteEndPoint} at {DateTime.Now} ...");
                    this.pointBClient.Close();
                    this.pointBClient = null;
                }
                catch (System.ObjectDisposedException)
                {
                    this.pointBClient = null;
                }
                catch (System.NullReferenceException)
                {
                    this.pointBClient = null;
                }
            }
        }

        // Static method for creating and starting a PortForwardBridge.
        static public void CreatePortForwardBridge(PointAClient a, TcpClient localClient, TcpClient pointBEndpoint, bool isEncrypted)
        {
            PortForwardBridge bridge = new PortForwardBridge(a, localClient, pointBEndpoint, isEncrypted);
            _ = ThreadPool.QueueUserWorkItem(new WaitCallback(bridge.Start));
        }
    }
}
