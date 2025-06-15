using System.Collections.Generic;
using ChatterboxTTSNet;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer.Models;

public class TTSResult
{ 
    public SoundBuffer AudioBuffer { get; init; }
    public List<WordTimestamp> WordTimestamps { get; set; } = new List<WordTimestamp>();
    public string MessageType { get; init; } = "Empty";
}