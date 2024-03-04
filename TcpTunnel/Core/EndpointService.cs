using System;
using System.Net.Sockets;
using System.Text;
using TcpTunnel.Utils;

namespace TcpTunnel.Core
{
    internal class EndpointService
    {
        // Fields to store endpoint details and authentication credentials
        String host;
        ushort port;
        private bool isDestEncrypted;

        // Constructor to initialize the EndpointService with necessary details
        public EndpointService(ushort port, string host, string username, string password, bool isDestEncrypted)
        {
            this.UserName = username;
            this.Password = password;
            this.host = host;
            this.port = port;
            this.Endpoint = new TcpClient();
            this.isDestEncrypted = isDestEncrypted;
        }

        // Property to get the username used for authentication of remote server
        public string UserName { get; private set; }

        // Property to get the password used for authentication of remote server
        public string Password { get; private set; }

        // TcpClient instance to manage the TCP connection
        public TcpClient Endpoint { get; private set; }

        // Property to check if authentication is required
        public bool RequireValidate
        {
            get
            {
                return !string.IsNullOrEmpty(this.UserName) || !string.IsNullOrEmpty(this.Password);
            }
        }

        // Method to perform authentication with the remote server
        private bool _DoAuthentication()
        {
            byte[] buffer;

            Packet packet = new Packet();
            packet.dataIdentifier = (Int16)DataIdentifier.AUTHENTICATION_REQUEST;
            packet.data = Encoding.UTF8.GetBytes(string.Format("{0} {1}", this.UserName, this.Password));

            buffer = packet.GetDataStream();
            if (isDestEncrypted) buffer = EncryptService.Encrypt(buffer);
            SocketUtils.Send(Endpoint, buffer);

            if (SocketUtils.Receive(Endpoint, 256, out buffer))
            {
                if (isDestEncrypted) buffer = EncryptService.Decrypt(buffer);
                packet = new Packet(buffer);
                if (packet.dataIdentifier == (Int16)DataIdentifier.ACCEPT_CONNECTION)
                {
                    return true;
                }
                else if (packet.dataIdentifier == (Int16)DataIdentifier.NOT_AUTHORIZED)
                {
                    Logger.WriteLineLog(string.Format("Not Authorized {1} at {0} ...", DateTime.Now, Endpoint.Client.RemoteEndPoint));
                }
                else
                {
                    Logger.WriteLineLog(string.Format("Received Unknown Connection Request from {1} at {0} ...", DateTime.Now, Endpoint.Client.RemoteEndPoint));
                }
            }
            Endpoint.Close();
            return false;
        }

        // Method to establish a connection with the remote server
        public bool Connect()
        {
            Endpoint.Connect(host, port);
            if (!Endpoint.Connected)
            {
                Endpoint.Close();
                return false;
            }
            if (RequireValidate)
            {
                return _DoAuthentication();
            }
            return true;
        }
    }
}