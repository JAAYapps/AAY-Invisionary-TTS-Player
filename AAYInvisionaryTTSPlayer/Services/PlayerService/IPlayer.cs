using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer.Services.PlayerService
{
    public interface IPlayer
    {
        void AddToQueue(TTSResult message);

        bool Stop();
        SoundStatus GetPlayStatus();
        void Play();
        void Pause();
        string GetWord();
        float Pitch { get; set; }
        float Volume { get; set; }
    }
}
