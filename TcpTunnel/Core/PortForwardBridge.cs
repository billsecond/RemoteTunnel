using System.Net.Sockets;
using TcpTunnel.Utils;

namespace TcpTunnel.Core
{
    // The PortForwardBridge class handles the forwarding of data between a client and an endpoint.
    internal class PortForwardBridge
    {
        // Buffers for storing data received from the client and the endpoint.
        private byte[] _ClientBuffer;
        private byte[] _EndpointBuffer;

        // TcpClient objects for the client and endpoint connections.
        public TcpClient socks5Client;
        public TcpClient socks5Endpoint;

        // Reference to the TunnelService that initiated this bridge.
        public TunnelService listenrService;

        // Flags indicating whether encryption is applied to client and endpoint communications.
        private bool isClientEncrypted;
        private bool isEndpointEncrypted;

        // Constructor for initializing a new PortForwardBridge instance.
        public PortForwardBridge(TcpClient client, TcpClient endpoint, TunnelService s, bool isClienEncrypted, bool isEndpointEncrypted)
        {
            this.socks5Client = client;
            this.socks5Endpoint = endpoint;
            this.listenrService = s;
            this._ClientBuffer = new byte[0];
            this._EndpointBuffer = new byte[0];
            this.isClientEncrypted = isClienEncrypted;
            this.isEndpointEncrypted = isEndpointEncrypted;
        }

        // Starts the forwarding process between the client and the endpoint.
        public void Start(object state)
        {
            Logger.WriteLineLog($"Port Forward Bridge has been Created at {DateTime.Now} between {socks5Client.Client.RemoteEndPoint} and {socks5Endpoint.Client.RemoteEndPoint}");
            _ClientBuffer = new byte[socks5Client.ReceiveBufferSize];
            _EndpointBuffer = new byte[socks5Endpoint.ReceiveBufferSize];
            socks5Client.Client.BeginReceive(_ClientBuffer, 0, _ClientBuffer.Length, SocketFlags.None, OnClientReceive, socks5Client.Client);
            socks5Endpoint.Client.BeginReceive(_EndpointBuffer, 0, _EndpointBuffer.Length, SocketFlags.None, OnEndpointReceive, socks5Endpoint.Client);
        }

        // Callback method for handling data received from the client.
        private void OnClientReceive(IAsyncResult result)
        {
            if (this.listenrService.IsStarting)
            {
                try
                {
                    Socket socket = result.AsyncState as Socket;
                    SocketError error;
                    int size = socket.EndReceive(result, out error);
                    if (size > 0)
                    {
                        // Apply necessary encryption/decryption transformations.
                        if (isClientEncrypted && !isEndpointEncrypted) _ClientBuffer = EncryptService.Decrypt(_ClientBuffer);
                        if (!isClientEncrypted && isEndpointEncrypted) _ClientBuffer = EncryptService.Encrypt(_ClientBuffer);
                        // Forward the data to the endpoint.
                        SocketUtils.Send(this.socks5Endpoint.Client, _ClientBuffer, 0, size);
                        // Continue receiving data from the client.
                        if (this.listenrService.IsStarting)
                            socks5Client.Client.BeginReceive(_ClientBuffer, 0, _ClientBuffer.Length, SocketFlags.None, OnClientReceive, socks5Client.Client);
                    }
                    else
                    {
                        // Handle client disconnection.
                        this.Close();
                    }
                }
                catch
                {
                    // Close the connection on error.
                    this.Close();
                }
            }
        }

        // Callback method for handling data received from the endpoint.
        private void OnEndpointReceive(IAsyncResult result)
        {
            if (this.listenrService.IsStarting)
            {
                try
                {
                    Socket socket = result.AsyncState as Socket;
                    SocketError error;
                    int size = socket.EndReceive(result, out error);
                    if (size > 0)
                    {
                        // Apply necessary encryption/decryption transformations.
                        if (isClientEncrypted && !isEndpointEncrypted) _EndpointBuffer = EncryptService.Encrypt(_EndpointBuffer);
                        if (!isClientEncrypted && isEndpointEncrypted) _EndpointBuffer = EncryptService.Decrypt(_EndpointBuffer);
                        // Forward the data to the client.
                        SocketUtils.Send(this.socks5Client.Client, _EndpointBuffer, 0, size);
                        // Continue receiving data from the endpoint.
                        if (this.listenrService.IsStarting)
                            socks5Endpoint.Client.BeginReceive(_EndpointBuffer, 0, _EndpointBuffer.Length, SocketFlags.None, OnEndpointReceive, socks5Endpoint.Client);
                    }
                    else
                    {
                        // Handle endpoint disconnection.
                        this.Close();
                    }
                }
                catch
                {
                    // Close the connection on error.
                    this.Close();
                }
            }
        }

        // Closes the connections to the client and endpoint.
        private void Close()
        {
            if (this.socks5Client != null)
            {
                try
                {
                    Logger.WriteLineLog($"Closed Connection to client {socks5Client.Client.RemoteEndPoint} at {DateTime.Now} ...");
                    socks5Client.Close();
                    socks5Client = null;
                }
                catch (System.ObjectDisposedException)
                {
                    this.socks5Client = null;
                }
                catch (System.NullReferenceException)
                {
                    this.socks5Client = null;
                }
            }
            if (this.socks5Endpoint != null)
            {
                try
                {
                    Logger.WriteLineLog($"Closed Connection to endpoint {socks5Endpoint.Client.RemoteEndPoint} at {DateTime.Now} ...");
                    this.socks5Endpoint.Close();
                    this.socks5Endpoint = null;
                }
                catch (System.ObjectDisposedException)
                {
                    this.socks5Endpoint = null;
                }
                catch (System.NullReferenceException)
                {
                    this.socks5Endpoint = null;
                }
            }
        }

        // Static method for creating and starting a PortForwardBridge.
        static public void CreatePortForwardBridge(TunnelService s, TcpClient client, TcpClient endpoint, bool isClientEncrypted, bool isEndpointEncrypted)
        {
            PortForwardBridge bridge = new PortForwardBridge(client, endpoint, s, isClientEncrypted, isEndpointEncrypted);
            _ = ThreadPool.QueueUserWorkItem(new WaitCallback(bridge.Start));
        }
    }
}