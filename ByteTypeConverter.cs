using System;
using System.Runtime.CompilerServices;

class ByteTypeConverter
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static sbyte[] BytesToSbytes(byte[] input)
	{
		return (sbyte[])(Array)input;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] SbytesTobytes(sbyte[] input)
	{
		return (byte[])(Array)input;
	}
}
