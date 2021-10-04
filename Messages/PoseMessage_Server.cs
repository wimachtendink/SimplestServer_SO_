using System;
using System.Collections.Generic;
using System.Net.Sockets;

public class PoseMessage_Server
{
	public MessageHeader header;
	
	public byte QtyPosesDescribed
	{
		get 
		{
			return header.dataByte;
		}
		set
		{
			header.dataByte = value;
		}
	}

	//we will need to fill a large number of PoseDescriptions from server
	public List<PoseDescription> poseDescriptions;

	public byte[] GetBytes()
	{
		var output = new byte[MessageHeader.HEADER_SIZE + (PoseDescription.DATA_SIZE * poseDescriptions.Count)];

		header.GetBytes().CopyTo(output, 0);

		for (int idx = 0; idx < poseDescriptions.Count; idx++)
		{
			poseDescriptions[idx].GetBytes()
				.CopyTo(output, MessageHeader.HEADER_SIZE + (idx * PoseDescription.DATA_SIZE));
		}

		return output;
	}

	public PoseMessage_Server()
	{
		poseDescriptions = new List<PoseDescription>(Client.ClientsList.Count);
		var bytes = new byte[3] { (byte)MessageHeader.MessageType.poseMessage, byte.MaxValue, 0 };
		header = new MessageHeader(bytes);
	}

}