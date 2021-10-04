public class PoseMessage_Client
{
	static int _size = -255;
	public static int Size
	{
		get
		{
			if (_size < 0)
			{
				//checking every time... kinda dumb maybe learn about source generators :D
				_size = MessageHeader.HEADER_SIZE + PoseDescription.DATA_SIZE;
			}
			return _size;
		}
	}

	public MessageHeader header;
	public PoseDescription poseDescription;

	public byte[] GetBytes()
	{
		var output = new byte[Size];

		header.GetBytes().CopyTo(output, 0);
		poseDescription.GetBytes().CopyTo(output, MessageHeader.HEADER_SIZE);

		return output;
	}
}