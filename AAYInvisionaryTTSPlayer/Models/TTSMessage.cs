using System.Collections.Generic;

namespace AAYInvisionaryTTSPlayer.Models
{
    public class TTSMessage
    {
        public class Audio
        {
            public List<SFML.Audio.SoundBuffer> audioChannels { get; set; } = new List<SFML.Audio.SoundBuffer>();
            public int sampleRate { get; set; }
        }

        public class Timeline
        {
            public string type { get; set; } = string.Empty;
            public string text { get; set; } = string.Empty;
            public double startTime { get; set; }
            public double endTime { get; set; }
            public List<Timeline> timeline { get; set; } = new List<Timeline>();
        }

        public class Message
        {
            public string requestId { get; set; } = string.Empty;
            public string messageType { get; set; } = string.Empty;
            public int index { get; set; }
            public int total { get; set; }
            public Audio audio { get; set; } = new Audio();
            public List<Timeline> timeline { get; set; } = new List<Timeline>();
            public string transcript { get; set; } = string.Empty;
            public string language { get; set; } = string.Empty;
            public double peakDecibelsSoFar { get; set; }
        }
    }
}
