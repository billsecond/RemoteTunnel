using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpTunnel.Utils;

namespace TcpTunnel.Core
{
    // The TunnelService class is responsible for creating a TCP tunnel from a source to a destination.
    internal class TunnelService
    {
        // Constructor for creating a tunnel with minimal parameters, defaulting to port 2281.
        public TunnelService(string destHost, ushort destPort) : this(2281, destHost, destPort, "", "", "", "", false, false)
        {
        }

        // Main constructor that initializes the tunnel with detailed parameters.
        public TunnelService(ushort port, string destHost, ushort destPort,
            string username, string password,
            string destHostusername, string destHostpassword,
            bool requireEncryption, bool isDestEncrypted
            )
        {
            this.Port = port;
            this.DestHost = destHost;
            this.DestPort = destPort;
            this.UserName = username;
            this.Password = password;
            this.DestHostUsername = destHostusername;
            this.DestHostPassword = destHostpassword;
            this.requireEncryption = requireEncryption;
            this.isDestEncrypted = isDestEncrypted;
        }

        // Private fields for encryption flags.
        private bool requireEncryption;
        private bool isDestEncrypted;

        // Public properties for tunnel configuration.
        public ushort Port { get; private set; }
        public String DestHost { get; set; }
        public ushort DestPort { get; set; }
        internal bool IsStarting { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string DestHostPassword { get; private set; }
        public string DestHostUsername { get; private set; }

        // Indicates whether authentication is required for the tunnel.
        public bool RequireValidate
        {
            get
            {
                return !string.IsNullOrEmpty(this.UserName) || !string.IsNullOrEmpty(this.Password);
            }
        }

        private TcpListener _Listener;

        // Starts the TCP listener and accepts client connections.
        public void Start()
        {
            if (!this.IsStarting)
            {
                this._Listener = new TcpListener(IPAddress.Any, this.Port);
                this._Listener.Start();
                this._Listener.BeginAcceptTcpClient(this.OnBeginAcceptSocket, this._Listener);
                this.IsStarting = true;
                Logger.WriteLineLog($"Listener has been started at {DateTime.Now}.... on {this.Port} to Targeting {DestHost} {DestPort}");
            }
        }

        // Stops the TCP listener.
        public void Stop()
        {
            Logger.WriteLineLog("Stopping Listener ...");
            if (this.IsStarting)
            {
                this.IsStarting = false;
                this._Listener.Stop();
            }
        }

        // Handles the acceptance of a client connection.
        private void OnBeginAcceptSocket(IAsyncResult async)
        {
            TcpListener listener = async.AsyncState as TcpListener;
            if (listener == null)
            {
                Logger.WriteLineLog("Got Null Value at Accepting Socket");
                return;
            }

            TcpClient tcpClient = listener.EndAcceptTcpClient(async);
            Logger.WriteLineLog($"Received Client Connection Request from {tcpClient.Client.RemoteEndPoint} at {DateTime.Now}...");
            if (this.IsStarting) listener.BeginAcceptTcpClient(this.OnBeginAcceptSocket, listener);
            try
            {
                // Authentication process.
                if (this.RequireValidate && !this._DoAuthentication(tcpClient)) return;

                Logger.WriteLineLog($"Accepted Client Connection Request from {tcpClient.Client.RemoteEndPoint} at {DateTime.Now}...");
                // Start forwarding the connection.
                if (StartForwarding(tcpClient))
                {
                    Logger.WriteLineLog("Forwarding has been started");
                }
                else
                {
                    Logger.WriteLineLog("Forwarding has been failed");
                    SocketUtils.Send(tcpClient, Encoding.UTF8.GetBytes("Forwarding has been failed"));
                    tcpClient.Close();
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Logger.WriteLineLog($"An error occurred at {DateTime.Now}, error message: {ex.Message}");
            }
        }

        // Performs authentication of the client.
        private bool _DoAuthentication(TcpClient client)
        {
            byte[] buffer;
            Packet ret_packet = new Packet();
            if (SocketUtils.Receive(client, 256, out buffer))
            {
                // Decrypts the received data if encryption is required.
                if (requireEncryption) buffer = EncryptService.Decrypt(buffer);
                Packet packet = new Packet(buffer);
                if (packet.dataIdentifier == (Int16)DataIdentifier.AUTHENTICATION_REQUEST)
                {
                    if (packet.data != null)
                    {
                        try
                        {
                            string data = Encoding.UTF8.GetString(packet.data);
                            string[] parts = data.Split(' ');
                            if (parts.Length == 2 && parts[0] == this.UserName && parts[1] == this.Password)
                            {
                                ret_packet.dataIdentifier = (Int16)DataIdentifier.ACCEPT_CONNECTION;
                                buffer = ret_packet.GetDataStream();
                                if (requireEncryption) buffer = EncryptService.Encrypt(buffer);
                                SocketUtils.Send(client, buffer);
                                return true;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLineLog($"Exception Raised: {e}");
                        }
                    }
                }
                else
                {
                    Logger.WriteLineLog($"Received Unknown Connection Request from {client.Client.RemoteEndPoint} at {DateTime.Now} ...");
                }
            }
            // Sends a not authorized response if authentication fails.
            ret_packet.dataIdentifier = (Int16)DataIdentifier.NOT_AUTHORIZED;
            buffer = ret_packet.GetDataStream();
            if (requireEncryption) buffer = EncryptService.Encrypt(buffer);
            SocketUtils.Send(client, buffer);
            client.Close();
            return false;
        }

        // Initiates the forwarding of the client's connection to the destination.
        private bool StartForwarding(TcpClient client)
        {
            EndpointService endpointService = new EndpointService(DestPort, DestHost, DestHostUsername, DestHostPassword, isDestEncrypted);
            if (endpointService.Connect())
            {
                PortForwardBridge.CreatePortForwardBridge(this, client, endpointService.Endpoint, requireEncryption, isDestEncrypted);
                return true;
            }
            return false;
        }
    }
}