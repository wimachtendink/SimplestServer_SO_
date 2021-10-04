using System;
public class PoseDescription
{
	//todo: consider renaming to "PoseDescription" rather than "NeighbourDescription" as poses change more than neighbours and neighbours might-should include style data

	public const int DATA_SIZE = 24;

	//actual data store
	public sbyte[] data;

	public sbyte globalPosition_x { get => data[0]; set => data[0] = value; }
	public sbyte globalPosition_y { get => data[1]; set => data[1] = value; }
	public sbyte globalPosition_z { get => data[2]; set => data[2] = value; }
	//-//Positions
	//head
	public sbyte head_Position_x { get => data[3]; set => data[3] = value; }
	public sbyte head_Position_y { get => data[4]; set => data[4] = value; }
	public sbyte head_Position_z { get => data[5]; set => data[5] = value; }
	//left
	public sbyte hand_Position_Left_x { get => data[6]; set => data[6] = value; }
	public sbyte hand_Position_Left_y { get => data[7]; set => data[7] = value; }
	public sbyte hand_Position_Left_z { get => data[8]; set => data[8] = value; }
	//right
	public sbyte hand_Position_Right_x { get => data[9]; set => data[9] = value; }
	public sbyte hand_Position_Right_y { get => data[10]; set => data[10] = value; }
	public sbyte hand_Position_Right_z { get => data[11]; set => data[11] = value; }
	//-//Rotations
	//head								
	public sbyte head_Rotation_x { get => data[12]; set => data[12] = value; }
	public sbyte head_Rotation_y { get => data[13]; set => data[13] = value; }
	public sbyte head_Rotation_z { get => data[14]; set => data[14] = value; }
	public sbyte head_Rotation_w { get => data[15]; set => data[15] = value; }
	//left hand							
	public sbyte hand_Rotation_Left_x { get => data[16]; set => data[16] = value; }
	public sbyte hand_Rotation_Left_y { get => data[17]; set => data[17] = value; }
	public sbyte hand_Rotation_Left_z { get => data[18]; set => data[18] = value; }
	public sbyte hand_Rotation_Left_w { get => data[19]; set => data[19] = value; }
	//right hand						
	public sbyte hand_Rotation_Right_x { get => data[20]; set => data[20] = value; }
	public sbyte hand_Rotation_Right_y { get => data[21]; set => data[21] = value; }
	public sbyte hand_Rotation_Right_z { get => data[22]; set => data[22] = value; }
	public sbyte hand_Rotation_Right_w { get => data[23]; set => data[23] = value; }

	public byte[] GetBytes()
	{
		byte[] output = new byte[DATA_SIZE];

		Buffer.BlockCopy(data, 0, output, 0, DATA_SIZE);

		return output;
	}

	public PoseDescription()
	{
		data = new sbyte[DATA_SIZE];
	}

	public PoseDescription(byte[] _data_bytes)
	{
		data = new sbyte[DATA_SIZE];
		if (_data_bytes.Length == DATA_SIZE)
		{
			//todo: this should be a warning
			_data_bytes.CopyTo(data, 0);
			Buffer.BlockCopy(_data_bytes, 0, data, 0, PoseDescription.DATA_SIZE);
		}
	}

	public PoseDescription(sbyte[] _data_sbytes)
	{
		data = new sbyte[DATA_SIZE];
		if (_data_sbytes.Length == DATA_SIZE)
		{
			_data_sbytes.CopyTo(data, 0);
		}
		else
		{
			throw new ArgumentException($"Data must be of size {DATA_SIZE}");
		}
	}

	public override string ToString()
	{
		return $"Positions: Head({head_Position_x},{head_Position_y},{head_Position_z}), Left({hand_Position_Left_x},{hand_Position_Left_y},{hand_Position_Left_z}), Right({hand_Position_Right_x},{hand_Position_Right_y},{hand_Position_Right_z})";
	}
}
