using System;
using System.IO;

namespace AAYInvisionaryTTSPlayer.Utilities;

public static class ByteManager
{
    public static byte[] ShortArrayToByteArray(short[] shortArray)
    {
        byte[] byteArray = new byte[shortArray.Length * 2];
        Buffer.BlockCopy(shortArray, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }
    
    public static short[] ByteArrayToShortArray(byte[] byteArray)
    {
        if (byteArray.Length % 2 != 0)
            throw new ArgumentException("Byte array must have an even number of bytes.", nameof(byteArray));
        short[] shortArray = new short[byteArray.Length / 2];
        Buffer.BlockCopy(byteArray, 0, shortArray, 0, byteArray.Length);
        return shortArray;
    }

    public static Stream ByteArrayToStream(byte[] byteArray)
    {
        return new MemoryStream(byteArray);
    }

    public static byte[] StreamToByteArray(Stream stream)
    {
        if (stream is MemoryStream ms)
            return ms.ToArray();
        using var memoryStream = new MemoryStream();
        if (stream.CanSeek)
            stream.Position = 0;
        else
            return [];
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    public static Stream ShortArrayToStream(short[] shortArray)
    {
        return new MemoryStream(ShortArrayToByteArray(shortArray));
    }
}