using System.Collections.Generic;
using ChatterboxTTSNet;

namespace AAYInvisionaryTTSPlayer.Models;

public class TTSResult
{ 
    public byte[] AudioBuffer { get; init; }
    public int ChannelCount { get; set; } = 1;
    public int BitRate { get; set; }
    public List<WordTimestamp> WordTimestamps { get; set; } = new List<WordTimestamp>();
    public string MessageType { get; init; } = "Empty";
}