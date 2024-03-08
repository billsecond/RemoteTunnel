using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Utils;

namespace Remote.Server.Core
{
    internal struct LocalListenEndpoint
    {
        public TcpClient client;
        public int? PointALocalServerPort;
        public string? PointALocalServerHost;
        public LocalListenEndpoint(TcpClient client, int port, string host)
        {
            this.client = client;
            this.PointALocalServerPort = port;
            this.PointALocalServerHost = host;
        }
        public LocalListenEndpoint(TcpClient client)
        {
            this.client = client;
            this.PointALocalServerPort = null;
            this.PointALocalServerHost = null;
        }

    }
    internal class PointAListenServer
    {
        // Declaration of variables used within the class
        private ushort port;
        private TcpListener _Listener;
        private TcpClient MasterPointAClient;
        private byte[] _MasterPointABuffer;
        private bool isEncrypted;
        private string username;
        private string password;
        public Hashtable LocalListenEndpointHashTable;

        // Property to check if the server has started
        internal bool IsStarting
        {
            get;
            private set;
        }

        // Constructor for initializing the server with specific parameters
        public PointAListenServer(ushort port, bool isEncrypted, string username, string password)
        {
            this.port = port;
            IsStarting = false;
            this.isEncrypted = isEncrypted;
            this.username = username;
            this.password = password;
            LocalListenEndpointHashTable = new Hashtable();
            _MasterPointABuffer = new byte[0];
        }

        // Method to start the server
        public void Start()
        {            
            if (!this.IsStarting)
            {
                this._Listener = new TcpListener(IPAddress.Any, port);
                this._Listener.Start();
                // Begin accepting TCP client connections asynchronously
                this._Listener.BeginAcceptTcpClient(this.OnBeginAcceptTcpClient, this._Listener);
                this.IsStarting = true;
                Logger.WriteLineLog(string.Format("Server for Point A has been started at {0}.... on {1} ", DateTime.Now, this.port));
            }
        }

        // Method to stop the server
        public void Stop()
        {
            if (this.IsStarting)
            {
                this.IsStarting = false;
                this._Listener.Stop();
                if (this.MasterPointAClient != null)
                    this.MasterPointAClient.Close();
                this._Listener = null;
                Logger.WriteLineLog(string.Format("Server for Point A has been stopped at {0} ", DateTime.Now));
            }
        }

        // Method to handle incoming TCP client connections asynchronously
        private void OnBeginAcceptTcpClient(IAsyncResult async)
        {
            TcpListener listener = async.AsyncState as TcpListener;
            try
            {
                TcpClient tcpClient = listener.EndAcceptTcpClient(async);
                if (this.IsStarting)
                {
                    // Continue accepting TCP client connections asynchronously
                    listener.BeginAcceptTcpClient(this.OnBeginAcceptTcpClient, listener);
                }
                // Perform handshake with the client
                if (!this._DoShakeHandle(tcpClient))
                {
                    tcpClient.Close();
                    return;
                }

            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Logger.WriteLineLog(string.Format("Server for Point A: An error occurred at {0}, error message: {1}, Trace:{2}", DateTime.Now, ex.Message, ex.StackTrace));
            }
        }
        // Callback method for handling data received from Master Client of Point A asynchronously.
        private void OnMasterReceive(IAsyncResult async)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket socket = async.AsyncState as Socket;
                SocketError error;
                // Complete receiving the data and get the size of the received data.
                int size = socket.EndReceive(async, out error);
                // If the service is still running, continue to receive data.
                if (this.IsStarting)
                    this.MasterPointAClient.Client.BeginReceive(_MasterPointABuffer, 0, _MasterPointABuffer.Length, SocketFlags.None, OnMasterReceive, MasterPointAClient.Client);

                // Decrypt the received data if encryption is enabled.
                if (isEncrypted) _MasterPointABuffer = EncryptService.Decrypt(_MasterPointABuffer);

                // Deserialize the buffer into a packet object.
                Packet packet = new Packet(_MasterPointABuffer);
                if (packet.dataIdentifier == (Int16)DataIdentifier.FAILED_CREATE_NEW_PROXY_BRIDGE)
                {
                    string hashvalue = packet.name;
                    if (LocalListenEndpointHashTable.Contains(hashvalue))
                    {
                        LocalListenEndpoint e = (LocalListenEndpoint) LocalListenEndpointHashTable[hashvalue];
                        e.client.Close();
                        LocalListenEndpointHashTable.Remove(hashvalue);
                        Logger.WriteLineLog(string.Format("Failed on Creatine new Proxy Request to Point A Local Server {1}:{2} at {0} ", DateTime.Now, e.PointALocalServerHost, e.PointALocalServerPort));
                    }
                }
            }
            catch(Exception ex)
            {
                // Log any exceptions that occur during the process.
                Logger.WriteLineLog(string.Format("An error occurred at {0}, error message: {1}, {2}", DateTime.Now, ex.Message, ex.StackTrace));
            }
        }
        private bool _DoShakeHandle(TcpClient client)
        {
            byte[] buffer;
            Packet ret_packet = new Packet();
            if (SocketUtils.Receive(client, 256, out buffer))
            {
                if (isEncrypted) buffer = EncryptService.Decrypt(buffer);
                Packet packet = new Packet(buffer);
                
                if (packet.dataIdentifier == (Int16)DataIdentifier.MASTER_CONNECTION)
                {
                    ret_packet.dataIdentifier = (Int16)DataIdentifier.ACCEPTED_CONNECTION;
                    
                    buffer = ret_packet.GetDataStream();
                    if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                    SocketUtils.Send(client, buffer);
                    if (!this._DoAuthentication(client))
                    {
                        client.Close();
                        return false;
                    }
                    this.MasterPointAClient = client;
                    _MasterPointABuffer = new byte[MasterPointAClient.ReceiveBufferSize];
                    this.MasterPointAClient.Client.BeginReceive(_MasterPointABuffer, 0, _MasterPointABuffer.Length, SocketFlags.None, OnMasterReceive, MasterPointAClient.Client);
                    Logger.WriteLineLog(string.Format("Recieved Master Connection Request from {1} at {0} ...", DateTime.Now, this.MasterPointAClient.Client.RemoteEndPoint));
                    return true;
                }
                else if (packet.dataIdentifier == (Int16)DataIdentifier.ENDPOINT_CONNECTION)
                {
                    Logger.WriteLineLog(string.Format("Recieved Slave Connection Request from {1} at {0} ...", DateTime.Now, client.Client.RemoteEndPoint));
                    String hashValue = packet.name;
                    if (LocalListenEndpointHashTable.ContainsKey(hashValue))
                    {
                        ret_packet.dataIdentifier = (Int16)DataIdentifier.ACCEPTED_CONNECTION;

                        buffer = ret_packet.GetDataStream();
                        if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                        SocketUtils.Send(client, buffer);

                        PortForwardBridge.CreatePortForwardBridge(this, hashValue, client, isEncrypted);
                        return true;
                    }
                    else
                    {
                        ret_packet.name = "Unknown Hash Value: " + hashValue;
                        Logger.WriteLineLog(ret_packet.name);
                    }
                }
                
                else
                {
                    Logger.WriteLineLog(string.Format("Recieved Unkonwn Connection Request from {1} at {0} ...", DateTime.Now, client.Client.RemoteEndPoint));                    
                }
            }
            ret_packet.dataIdentifier = (Int16)DataIdentifier.REJECTED_CONNECTION;
            // Send Reply
            SocketUtils.Send(client, ret_packet.GetDataStream());            
            return false;
        }
        // Performs authentication of the client.
        private bool _DoAuthentication(TcpClient client)
        {
            byte[] buffer;
            Packet ret_packet = new Packet();
            if (SocketUtils.Receive(client, 256, out buffer))
            {
                // Decrypts the received data if encryption is required.
                if (isEncrypted) buffer = EncryptService.Decrypt(buffer);
                Packet packet = new Packet(buffer);
                if (packet.dataIdentifier == (Int16)DataIdentifier.AUTHENTICATION_REQUEST)
                {
                    if (packet.message != null)
                    {
                        try
                        {
                            string data = Encoding.UTF8.GetString(packet.message);
                            string[] parts = data.Split(' ');
                            if (parts.Length == 2 && parts[0] == this.username && parts[1] == this.password)
                            {
                                ret_packet.dataIdentifier = (Int16)DataIdentifier.AUTHORIZED;
                                buffer = ret_packet.GetDataStream();
                                if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                                SocketUtils.Send(client, buffer);
                                return true;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLineLog($"Exception Raised: {e.ToString()}");
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
            if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
            SocketUtils.Send(client, buffer);
            return false;
        }

        public bool StartNewPointAClient(LocalListenEndpoint item)
        {            
            if (this.MasterPointAClient == null) return false;
            if (!this.MasterPointAClient.Connected) return false;
            
            String hashKey = Guid.NewGuid().ToString(); ;
            LocalListenEndpointHashTable.Add(hashKey, item);

            Packet packet = new Packet();
            packet.dataIdentifier = (Int16)DataIdentifier.CREATE_NEW_PROXY_BRIDGE;
            packet.name = hashKey;
            if(item.PointALocalServerPort!=null && item.PointALocalServerHost!=null)
                packet.message = Encoding.UTF8.GetBytes($"{item.PointALocalServerHost}:{item.PointALocalServerPort}");
            byte[] buffer = packet.GetDataStream();
            if(isEncrypted) buffer = EncryptService.Encrypt(buffer);
            SocketUtils.Send(this.MasterPointAClient, buffer);

            Logger.WriteLineLog(string.Format("Start New Point A Client Request has been sent with hashKey {0}", hashKey));
            return true;
            
        }
    }
}
