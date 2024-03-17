using System.Net.Sockets;
using System.Threading;
using Utils;

namespace Remote.Server.Core
{
    // The PortForwardBridge class handles the forwarding of data between a client and an endpoint.
    internal class PortForwardBridge
    {
        // Buffers for storing data received from the client and the endpoint.
        private byte[] _LocalClientBuffer;
        private byte[] _PointAClientBuffer;

        // TcpClient objects for the client and endpoint connections.
        public TcpClient localClient;
        // Store information of Local Listen endpoint of this bridge
        private LocalListenEndpoint localListenEndpoint;
        public TcpClient pointAClient;
        private PointAListenServer pointAListenServer;
        private string localClienthashValue;
        private bool isEncrypted;

        // Constructor for initializing a new PortForwardBridge instance.
        public PortForwardBridge(PointAListenServer s, string hashValue, TcpClient pointAClient, bool isEncrypted)
        {
            this.pointAListenServer = s;
            this.localClienthashValue = hashValue;
            localListenEndpoint = (LocalListenEndpoint)s.LocalListenEndpointHashTable[hashValue];
            this.localClient = localListenEndpoint.client;
            this.pointAClient = pointAClient;
            this._LocalClientBuffer = new byte[0];
            this._PointAClientBuffer = new byte[0];
            this.isEncrypted = isEncrypted;            
        }

        // Starts the forwarding process between the client and the endpoint.
        public void Start(object state)
        {
            Logger.WriteLineLog($"Port Forward Bridge has been Created at {DateTime.Now} between {localClient.Client.RemoteEndPoint} and {pointAClient.Client.RemoteEndPoint}");
            _LocalClientBuffer = new byte[localClient.ReceiveBufferSize];
            _PointAClientBuffer = new byte[pointAClient.ReceiveBufferSize];
            localClient.Client.BeginReceive(_LocalClientBuffer, 0, _LocalClientBuffer.Length, SocketFlags.None, OnLocalClientReceive, localClient.Client);
            pointAClient.Client.BeginReceive(_PointAClientBuffer, 0, _PointAClientBuffer.Length, SocketFlags.None, OnPointBClientReceive, pointAClient.Client);
        }

        // Callback method for handling data received from the Local Web Server.
        private void OnLocalClientReceive(IAsyncResult result)
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
                    SocketUtils.Send(this.pointAClient.Client, _LocalClientBuffer, 0, size);
                    // Continue receiving data from the client.
                    if (Program.IsStarting)
                        localClient.Client.BeginReceive(_LocalClientBuffer, 0, _LocalClientBuffer.Length, SocketFlags.None, OnLocalClientReceive, localClient.Client);
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

        // Callback method for handling data received from the Point B.
        private void OnPointBClientReceive(IAsyncResult result)
        {            
            try
            {
                Socket socket = result.AsyncState as Socket;
                SocketError error;
                int size = socket.EndReceive(result, out error);
                if (size > 0)
                {
                    // Apply decrypt when data is received from Point B
                    if (isEncrypted) _PointAClientBuffer = EncryptService.Decrypt(_PointAClientBuffer);
                    // Forward the data to the client.
                    SocketUtils.Send(this.localClient.Client, _PointAClientBuffer, 0, size);
                    // Continue receiving data from the endpoint.
                    if (Program.IsStarting)
                        pointAClient.Client.BeginReceive(_PointAClientBuffer, 0, _PointAClientBuffer.Length, SocketFlags.None, OnPointBClientReceive, pointAClient.Client);
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

        // Closes the connections to the client and endpoint.
        private void Close()
        {
            if (this.localClient != null)
            {
                try
                {
                    Logger.WriteLineLog($"Closed Connection to client {localClient.Client.RemoteEndPoint} at {DateTime.Now} ...");
                    localClient.Close();
                    pointAListenServer.LocalListenEndpointHashTable.Remove(localClienthashValue);
                    localClient = null;
                }
                catch (System.ObjectDisposedException)
                {
                    this.localClient = null;
                }
                catch (System.NullReferenceException)
                {
                    this.localClient = null;
                }
            }
            if (this.pointAClient != null)
            {
                try
                {
                    Logger.WriteLineLog($"Closed Connection to endpoint {pointAClient.Client.RemoteEndPoint} at {DateTime.Now} ...");
                    this.pointAClient.Close();
                    this.pointAClient = null;
                }
                catch (System.ObjectDisposedException)
                {
                    this.pointAClient = null;
                }
                catch (System.NullReferenceException)
                {
                    this.pointAClient = null;
                }
            }
        }

        // Static method for creating and starting a PortForwardBridge.
        static public void CreatePortForwardBridge(PointAListenServer s, string hashValue, TcpClient pointAClient, bool isEncrypted)
        {
            PortForwardBridge bridge = new PortForwardBridge(s,hashValue, pointAClient, isEncrypted);
            _ = ThreadPool.QueueUserWorkItem(new WaitCallback(bridge.Start));
        }
    }
}
