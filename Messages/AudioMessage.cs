using System.Net.Sockets;

public class AudioMessage
{
    public static int AudioSliceSize
    {
        get => 8192;
        //get
        //{
        //	return Client.AudioChunkOffsets[1];
        //}
    }
    //-//Static
    private static int _size = int.MinValue;
    public static int Size
    {
        get
        {
            if (_size < 0)
            {
                //checking every time... kinda dumb maybe learn about source generators :D
                _size = MessageHeader.HEADER_SIZE + AudioSliceSize;
            }
            return _size;
        }
    }

    //-//Member
    public MessageHeader header;

    public byte[] data;

    public byte[] GetBytes()
    {
        var output = new byte[MessageHeader.HEADER_SIZE + data.Length];

        header.GetBytes().CopyTo(output, 0);
        data.CopyTo(output, MessageHeader.HEADER_SIZE);

        return output;
    }

    public void ReadDataFromNetworkStream(NetworkStream networkStream, MessageHeader _header)
    {
        header = new MessageHeader(_header.GetBytes());
        networkStream.Read(data, 0, AudioSliceSize);
    }

    public AudioMessage()
    {
        byte[] messageHeaderBytes = new byte[MessageHeader.HEADER_SIZE] {  (byte)MessageHeader.MessageType.audioMessage, byte.MaxValue, byte.MaxValue } ;
        header = new MessageHeader(messageHeaderBytes);
        data = new byte[AudioSliceSize];
    }

    public AudioMessage(NetworkStream networkStream, MessageHeader _header)
    {
        data = new byte[AudioSliceSize];
        ReadDataFromNetworkStream(networkStream, _header);
    }
}