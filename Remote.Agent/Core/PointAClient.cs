using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Utils;
namespace Remote.Agent.Core
{
    
    internal class PointAClient
    {
        static int retrySeconds = 3 * 1000; // Time in milliseconds to wait before retrying connection to Point B.
        private string PointBHost = "127.0.0.1"; // Default host address for Point B.
        private int PointBPort = 8080; // Default port for Point B.        
        TcpClient masterClientOfPointB;
        byte[] _MasterBuffer; // Buffer for storing data received from Point B.
        private bool isEncrypted; // Flag to indicate if encryption is enabled.
        private string LocalWebServerHost = "127.0.0.1"; // Default host address for the local web server.
        private int LocalWebServerPort = 9000; // Default port for the local web server.
        private HashSet<string> allowedSet;
        public PointAClient()
        {
            _MasterBuffer = new byte[0];
            PointBUserName = "";
            PointBPassword = "";
            IsStarting = false;
            isEncrypted = false;
            allowedSet = new HashSet<string>();
        }
        public PointAClient(string pointBHost, int pointBPort, string localWebServerHost, int localWebServerPort, bool isEncrypted, string username, string password, HashSet<string> m)
        {
            PointBHost = pointBHost;
            PointBPort = pointBPort;
            LocalWebServerHost = localWebServerHost;
            LocalWebServerPort = localWebServerPort;
            masterClientOfPointB = new TcpClient();
            allowedSet = m;
            _MasterBuffer = new byte[0];
            this.isEncrypted = isEncrypted;
            PointBUserName = username;
            PointBPassword = password;
        }

        private string PointBUserName;
        private string PointBPassword;
        public bool IsStarting
        {
            get;
            private set;
        }


        // Callback method for handling data received from Point B asynchronously.
        internal void onMasterReceive(IAsyncResult result)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket socket = result.AsyncState as Socket;
                SocketError error;
                // Complete receiving the data and get the size of the received data.
                int size = socket.EndReceive(result, out error);

                // Check if the received data size is 0, indicating the Point B has been disconnected.
                if (size == 0)
                {
                    Logger.WriteLineLog("The Point B has been disconnected");
                    Logger.WriteLineLog("Restarting the Service");
                    // Stop the current service.
                    this.Stop();
                    Logger.WriteLineLog("Restarted the Service");
                    // Attempt to restart the service.
                    this._Start();
                }
                else
                {
                    // If the service is still running, continue to receive data.
                    if (this.IsStarting)
                        this.masterClientOfPointB.Client.BeginReceive(_MasterBuffer, 0, _MasterBuffer.Length, SocketFlags.None, this.onMasterReceive, masterClientOfPointB.Client);

                    // Decrypt the received data if encryption is enabled.
                    if (isEncrypted) _MasterBuffer = EncryptService.Decrypt(_MasterBuffer);

                    // Deserialize the buffer into a packet object.
                    Packet packet = new Packet(_MasterBuffer);
                    byte[] buffer;
                    // Check if the packet is a request to create a new proxy bridge.
                    if (packet.dataIdentifier == (short)DataIdentifier.CREATE_NEW_PROXY_BRIDGE)
                    {
                        Logger.WriteLineLog(string.Format("Create New Proxy Request Accepted", DateTime.Now));
                        string hashvalue = packet.name;
                        // Set the Default Local Host and port
                        string localHost = LocalWebServerHost;
                        int localPort = LocalWebServerPort;
                        
                        
                        if (packet.message != null)
                        {
                            // Get Local Server Host and Port information from Packet
                            string data = Encoding.UTF8.GetString(packet.message);
                            string[] parts = data.Split(':');
                            
                            bool GotHostPort = false;
                            if (parts.Length == 2)
                            {
                                localHost = parts[0];
                                if (int.TryParse(parts[1], out int port))
                                {
                                    localPort = port;
                                    GotHostPort = true;
                                }
                            }
                            if (!GotHostPort || !allowedSet.Contains($"{localHost}:{localPort}"))
                            {
                                Logger.WriteLineLog(string.Format("Create New Proxy Request Didn't contain valid Host:Port information at ", PointBHost, PointBPort, DateTime.Now));
                                packet = new Packet();
                                packet.dataIdentifier = (short)DataIdentifier.FAILED_CREATE_NEW_PROXY_BRIDGE;
                                packet.name = hashvalue;
                                buffer = packet.GetDataStream();
                                if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                                SocketUtils.Send(masterClientOfPointB.Client, buffer);
                                return;
                            }
                        }


                        // Create a new TCP client to connect to Point B.                        
                        TcpClient client = new TcpClient();
                        client.Connect(PointBHost, PointBPort);
                        if (!client.Connected)
                        {
                            // Log connection failure and notify Point B.
                            Logger.WriteLineLog(string.Format("Connecting to the Point B Server Failed for Slave {0}:{1} at {2}", PointBHost, PointBPort, DateTime.Now));
                            packet = new Packet();
                            packet.dataIdentifier = (short)DataIdentifier.FAILED_CREATE_NEW_PROXY_BRIDGE;
                            packet.name = hashvalue;
                            buffer = packet.GetDataStream();
                            if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                            SocketUtils.Send(socket, buffer);
                            return;
                        }

                        /// Do Hand Shake to establish the new proxy connection.
                        /// Send ENDPOINT_CONNECTION request to Point B for new proxy bridge with hashValue. 
                        /// Receive ACCEPTED_CONNECTIOn response from Point B.
                        /// Finally, Bridge has been established between Point A and Point B, Point B and new web server request from user on Point B.
                        /// Here hasValue is for identifying the each request of user on Point B.
                        packet = new Packet();
                        packet.dataIdentifier = (short)DataIdentifier.ENDPOINT_CONNECTION;
                        packet.name = hashvalue;
                        buffer = packet.GetDataStream();
                        if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                        SocketUtils.Send(client, buffer);


                        // Wait for a response from the newly connected client.
                        SocketUtils.Receive(client, 256, out buffer);
                        if (isEncrypted) buffer = EncryptService.Decrypt(buffer);
                        packet = new Packet(buffer);
                        if (packet.dataIdentifier == (short)DataIdentifier.ACCEPTED_CONNECTION)
                        {
                            Logger.WriteLineLog(string.Format("HandShake between Point A and Point B Succeed.", DateTime.Now));
                            // Start forwarding the connection upon successful handshake.
                            if (StartForwarding(client, localHost, localPort))
                            {
                                Logger.WriteLineLog("Forwarding has been started");
                            }
                            else
                            {
                                // Handle forwarding failure.
                                Logger.WriteLineLog("Forwarding has been failed");
                                buffer = Encoding.UTF8.GetBytes("Forwarding has been failed");
                                if (isEncrypted) buffer = EncryptService.Encrypt(buffer);

                                SocketUtils.Send(client, buffer);
                                client.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the process.
                Logger.WriteLineLog(string.Format("An error occurred at {0}, error message: {1}, {2}", DateTime.Now, ex.Message, ex.StackTrace));
            }
        }

        // Performs authentication when the Master Client of Point B initiates a connection.
        private bool _DoAuthentication(TcpClient client)
        {
            byte[] buffer;

            // Create a new packet for the authentication request.
            Packet packet = new Packet();
            packet.dataIdentifier = (Int16)DataIdentifier.AUTHENTICATION_REQUEST;
            // Encode the username and password into the packet's message.
            packet.message = Encoding.UTF8.GetBytes(string.Format("{0} {1}", this.PointBUserName, this.PointBPassword));

            // Serialize the packet into a byte array.
            buffer = packet.GetDataStream();
            // Encrypt the buffer if encryption is enabled.
            if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
            // Send the authentication request to the server.
            SocketUtils.Send(client, buffer);

            // Wait for a response from the server.
            if (SocketUtils.Receive(client, 256, out buffer))
            {
                // Decrypt the response if encryption is enabled.
                if (isEncrypted) buffer = EncryptService.Decrypt(buffer);
                // Deserialize the response into a packet.
                packet = new Packet(buffer);
                // Check if the server authorized the connection.
                if (packet.dataIdentifier == (Int16)DataIdentifier.AUTHORIZED)
                {
                    return true; // Authentication succeeded.
                }
                else if (packet.dataIdentifier == (Int16)DataIdentifier.NOT_AUTHORIZED)
                {
                    // Log if the server did not authorize the connection.
                    Logger.WriteLineLog(string.Format("Not Authorized {1} at {0} ...", DateTime.Now, client.Client.RemoteEndPoint));
                }
                else
                {
                    // Log if the server sent an unknown response.
                    Logger.WriteLineLog(string.Format("Received Unknown Connection Request from {1} at {0} ...", DateTime.Now, client.Client.RemoteEndPoint));
                }
            }
            return false; // Authentication failed.
        }

        // Initiates the forwarding of the client's connection to the destination.
        private bool StartForwarding(TcpClient pointBEndpoint, string localhost, int localPort)
        {
            TcpClient localClient = new TcpClient();
            try
            {
                localClient.Connect(localhost, localPort);
            }
            catch (Exception ex)
            {
                return false;
            }
            // Create a new TCP client for the local web server.
            
            // Attempt to connect to the local web server.
            
            // Check if the connection to the local web server was successful.
            if (localClient.Connected)
            {
                // Create a port forwarding bridge between the client, the pointBEndpoint, and the local web server.
                PortForwardBridge.CreatePortForwardBridge(this, localClient, pointBEndpoint, isEncrypted);
                return true; // Forwarding has been successfully initiated.
            }
            return false; // Failed to initiate forwarding.            
        }

        public void Start(Object state)
        {
            _Start();
        }
        // Method to start the connection process to Point B and handle authentication.
        private void _Start()
        {
            // Indicate that the starting process has begun.
            IsStarting = true;
            // Initialize the TCP client for connecting to Point B.
            masterClientOfPointB = new TcpClient();
            while (true)
            {
                // Exit the loop if the starting process is flagged to stop.
                if (!IsStarting) return;
                try
                {
                    // Log attempt to connect to Point B.
                    Logger.WriteLineLog(string.Format("Connecting to Point B ({1}:{2}) at {0}", DateTime.Now, PointBHost, PointBPort));
                    // Attempt to connect to Point B.
                    masterClientOfPointB.Connect(PointBHost, PointBPort);
                    if (masterClientOfPointB.Connected)
                    {
                        // Successfully connected, prepare a packet to indicate a master connection.
                        Packet packet = new Packet();
                        packet.dataIdentifier = (short)DataIdentifier.MASTER_CONNECTION;
                        byte[] buffer = packet.GetDataStream();

                        // Encrypt the buffer if encryption is enabled.
                        if (isEncrypted) buffer = EncryptService.Encrypt(buffer);
                        // Send the packet to Point B.
                        masterClientOfPointB.Client.Send(buffer);

                        // Wait for a response from Point B.
                        SocketUtils.Receive(masterClientOfPointB, 256, out buffer);
                        if (isEncrypted) buffer = EncryptService.Decrypt(buffer);

                        Packet receivedPacket = new Packet(buffer);
                        // Check if the connection was accepted.
                        if (receivedPacket.dataIdentifier != (short)DataIdentifier.ACCEPTED_CONNECTION)
                        {
                            Logger.WriteLineLog(string.Format("Connecting to Point B has been failed at {0}", DateTime.Now));
                        }
                        else if (_DoAuthentication(masterClientOfPointB))
                        {
                            // Connection accepted and authenticated, start receiving data.
                            _MasterBuffer = new byte[masterClientOfPointB.ReceiveBufferSize];
                            masterClientOfPointB.Client.BeginReceive(_MasterBuffer, 0, _MasterBuffer.Length, SocketFlags.None, onMasterReceive, masterClientOfPointB.Client);
                            Logger.WriteLineLog(string.Format("masterClient has been started at {0}", DateTime.Now));
                            return; // starting process has been completed succesfully.
                        }
                    }
                }
                catch (System.Net.Sockets.SocketException ex)
                {
                    // Log socket exceptions.
                    Logger.WriteLineLog(string.Format("An error occurred at {0}, error message: {1}", DateTime.Now, ex.Message));
                }
                catch (System.IO.IOException ex)
                {
                    // Log IO exceptions and reinitialize the TCP client.
                    Logger.WriteLineLog(string.Format("An error occurred at {0}, error message: {1}", DateTime.Now, ex.Message));
                    masterClientOfPointB = new TcpClient();
                }
                catch (Exception ex)
                {
                    // Log any other exceptions.
                    Logger.WriteLineLog(string.Format("An error occurred at {0}. \n {1}", DateTime.Now, ex.ToString()));
                }
                // Retry connection after a delay.
                Logger.WriteLineLog(string.Format("Trying to connect to Point B {0}:{1} again after {2} seconds", PointBHost, PointBPort, retrySeconds / 1000));
                Thread.Sleep(retrySeconds);
            }
        }

        // Method to stop the connection process and close the connection to Point B.
        public void Stop()
        {
            // Log the closure of the master client.
            Logger.WriteLineLog("Closing Master Client...");
            // Flag the starting process to stop.
            if (this.IsStarting)
            {
                this.IsStarting = false;
            }
            // Close the connection if it's still open.
            if (masterClientOfPointB.Connected)
            {
                masterClientOfPointB.Close();
            }
            // Wait for a moment to ensure all resources are properly released.
            Thread.Sleep(5000);
        }
    }
}
