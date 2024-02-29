using System.Text;

namespace TcpTunnel.Utils
{
    enum DataIdentifier : Int16
    {
        AUTHENTICATION_REQUEST = 0x01,
        NOT_AUTHORIZED = 0x02,        
        ACCEPT_CONNECTION = 0x10,
        UNKNOWN_COMMAND = 0xFF
    }
    // ---------------
    // Packet Structure
    // Description      -> |dataIdentifier|    data_length    |        data      |
    // Size in Bytes    -> |      2       |         4         |    data_length   |
    internal class Packet
    {
        #region Public Members
        public Int16 dataIdentifier;
        
        public byte[]? data;        
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

            // Read the extra data
            if (6 < dataStream.Length)
            {
                int dataLength = BitConverter.ToInt32(dataStream, 2);
                if (dataLength > 0)
                    this.data = dataStream.Skip(6).Take(dataLength).ToArray();
                else
                    this.data = null;
            }

        }
        // Converts the packet into a byte array fro sending/receiving
        public byte[] GetDataStream()
        {
            System.Collections.Generic.List<byte> dataStream = new System.Collections.Generic.List<byte>();
            // Add the dataIdentifier
            dataStream.AddRange(BitConverter.GetBytes(this.dataIdentifier));           
            if (this.data != null)
            {
                dataStream.AddRange(BitConverter.GetBytes(this.data.Length));
                dataStream.AddRange(this.data);
            }
            return dataStream.ToArray();
        }
        #endregion
    }
}
