using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpTunnel.Utils
{
    internal static class SocketUtils
    {
        /// <summary>
        /// 
        /// </summary>
        public const int TIMEOUT = 30000000; //30 seconds timeout
        #region Receive Information
        internal static bool Receive(TcpClient client, uint maxSize, out byte[] buffer)
        {
            byte[] buf = new byte[maxSize];
            NetworkStream stream = client.GetStream();
            int read_bytes = stream.Read(buf, 0, buf.Length);
            buffer = buf.Take(read_bytes).ToArray();
            return read_bytes != 0;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="maxSize">To extract the packet size</param>
        /// <param name="buffer">received data</param>
        /// <returns></returns>
        internal static bool Receive(Socket client, uint maxSize, out byte[] buffer)
        {
            buffer = new byte[0];
            int offset = 0;
            if (client.Connected)
            {
                try
                {
                    buffer = new byte[maxSize];
                    do
                    {
                        if (client.Available == 0)
                        {
                            //Query whether data is readable
                            if (!client.Poll(TIMEOUT, SelectMode.SelectRead)) break;
                            if (client.Available == 0) break;  //Exit if there is no data to read
                        }
                        //read data
                        int size = client.Receive(buffer, offset, buffer.Length - offset, SocketFlags.None);
                        offset += size;

                    } while (offset < buffer.Length);

                    if (offset > 0 && offset < buffer.Length)
                    {
                        //Insufficient data read
                        Array.Resize<byte>(ref buffer, offset);
                    }
                }
                catch
                {
                    offset = 0;
                }
            }
            return offset != 0;
        }
        #endregion

        #region send Message
        internal static void Send(TcpClient client, byte[] data)
        {
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        internal static void Send(Socket client, byte[] data)
        {
            Send(client, data, 0, data.Length);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        internal static void Send(Socket client, byte[] data, int offset, int size)
        {
            if (client.Connected)
            {
                try
                {
                    //Query whether writing data is allowed
                    if (client.Poll(TIMEOUT, SelectMode.SelectWrite))
                    {
                        //client.SendBufferSize = 10;
                        client.Send(data, offset, size, SocketFlags.Partial);
                    }
                }
                catch { }
            }
        }
        #endregion
    }
}
