using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public enum DataIdentifier : Int16
    {
        MASTER_CONNECTION = 0x01,
        ENDPOINT_CONNECTION = 0x02,
        ACCEPTED_CONNECTION = 0x10,
        REJECTED_CONNECTION = 0x11,
        CREATE_NEW_PROXY_BRIDGE = 0x22, // contain host:port information in message field
        FAILED_CREATE_NEW_PROXY_BRIDGE = 0x23,
        AUTHENTICATION_REQUEST = 0x31,
        AUTHORIZED = 0x32,
        NOT_AUTHORIZED = 0x33,
        UNKNOWN_COMMAND = 0xFF
    }
    // ---------------
    // Packet Structure
    // Description      -> |dataIdentifier|name length| message length | name (Hash UUID Value) | message (socket data) | extra_data_length |     extra_data    |
    // Size in Bytes    -> |      2       |     4     |       4        |        name length     |        message length |         4         | extra_data_length |
    public class Packet
    {
        #region Public Members
        public Int16 dataIdentifier;
        public String name;
        public byte[] extraData;
        public byte[] message;
        private byte[] origin_data;
        #endregion
        #region Methods
        public Packet()
        {

        }
        public Packet(byte[] dataStream)
        {
            origin_data = dataStream;
            //Read the dataIdentifier from the beginning of the stream ( 2 bytes )
            this.dataIdentifier = BitConverter.ToInt16(dataStream, 0);            
            //Read the length of the name(4 bytes)
            int nameLength = BitConverter.ToInt32(dataStream, 2);
            //Read the length of the message (4 bytes)
            int msgLength = BitConverter.ToInt32(dataStream, 6);
            //Read the name field
            if (nameLength > 0)
                this.name = Encoding.UTF8.GetString(dataStream, 10, nameLength);
            else
                this.name = null;
            // Read the message field
            if (msgLength > 0)
                this.message = dataStream.Skip(10 + nameLength).Take(msgLength).ToArray();
            else
                this.message = null;
            // Read the extra data
            if (10 + nameLength + msgLength + 4 < dataStream.Length)
            {
                int extraDataLength = BitConverter.ToInt32(dataStream, 10 + nameLength + msgLength);
                if (extraDataLength > 0)
                    this.extraData = dataStream.Skip(10 + nameLength + msgLength + 4).Take(extraDataLength).ToArray();
                else
                    this.extraData = null;
            }


        }
        // Converts the packet into a byte array fro sending/receiving
        public byte[] GetDataStream()
        {
            System.Collections.Generic.List<byte> dataStream = new System.Collections.Generic.List<byte>();
            // Add the dataIdentifier
            dataStream.AddRange(BitConverter.GetBytes(this.dataIdentifier));
            // Add the name length
            if (this.name != null)
                dataStream.AddRange(BitConverter.GetBytes(this.name.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));
            // Add the mssage length
            if (this.message != null)
                dataStream.AddRange(BitConverter.GetBytes(this.message.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));
            // Add the name
            if (this.name != null)
                dataStream.AddRange(Encoding.UTF8.GetBytes(this.name));
            // Add the message
            if (this.message != null)
                dataStream.AddRange(this.message);
            if (this.extraData != null)
            {
                dataStream.AddRange(BitConverter.GetBytes(this.extraData.Length));
                dataStream.AddRange(this.extraData);
            }
            return dataStream.ToArray();
        }
        #endregion
    }
}
