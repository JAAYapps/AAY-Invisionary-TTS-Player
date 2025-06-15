#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer.Services.PlayerService
{
    public class EchoGardenPlayer : IPlayer, IDisposable
    {
        private SFML.Audio.Sound sound = new SFML.Audio.Sound();

        private ConcurrentQueue<TTSResult> messages = new ConcurrentQueue<TTSResult>();

        private TTSResult? currentMessage;

        // A token to allow for clean shutdown of the background task.
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _queueProcessingTask;
        
        private bool started = false;
        
        public EchoGardenPlayer()
        {
            // Start a single, long-running background task to process the audio queue.
            _queueProcessingTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
        }

        public List<string> GetAudioDevices()
        {
            return new List<string>();
        }

        public void AddToQueue(TTSResult message)
        {
            messages.Enqueue(message);
            started = true;
        }

        public bool Stop()
        {
            started = false;
            sound.Stop();
            messages.Clear();
            return false;
        }

        public SoundStatus GetPlayStatus()
        {
            if (started && sound.Status is SoundStatus.Stopped or SoundStatus.Paused)
                return SoundStatus.Paused;
            else if (started && sound.Status == SoundStatus.Playing)
                return SoundStatus.Playing;
            return SoundStatus.Stopped;
        }

        private async Task ProcessQueueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Console.WriteLine(sound.Status.ToString() + " Messages: " + messages.Count);
                if (sound.Status == SoundStatus.Stopped && messages.TryDequeue(out var message))
                {
                    currentMessage = message;
                    sound.SoundBuffer = currentMessage.AudioBuffer;
                    sound.Play();
                }
                else if (sound.Status == SoundStatus.Stopped)
                    started = false;
                // Wait for a short period before checking again. This is more responsive than Thread.Sleep(1000).
                await Task.Delay(100, token);
            }
        }

        public void Play()
        {
            sound.Play();
        }

        public void Pause()
        {
            sound.Pause();
        }

        public string GetWord()
        {
            if (sound.Status != SoundStatus.Playing || currentMessage == null)
                return "Word: ";
            
            var currentTime = sound.PlayingOffset.AsSeconds();
            
            /*foreach (var timeline in currentMessage.timeline)
            {
                foreach (var sentence in timeline.timeline)
                {
                    for (int i = 0; i < sentence.timeline.Count; i++)
                    {
                        TTSMessage.Timeline word = sentence.timeline[i];
                        double nextWord = i < sentence.timeline.Count - 1 ? sentence.timeline[i].startTime - 0.20 : sentence.timeline[i].startTime + 0.25;
                        if (currentTime <= (float)nextWord)
                            return "Word: " + word.text;
                    }
                }
            }*/

            // Old method that had worked. Not sure why the other didn't work.
            if (currentMessage.WordTimestamps != null)
            {
                for (int i = 0; i < currentMessage.WordTimestamps.Count; i++)
                {
                    var word = currentMessage.WordTimestamps[i];
                    double nextWord = i < currentMessage.WordTimestamps.Count - 1
                        ? currentMessage.WordTimestamps[i].Start - 0.20
                        : currentMessage.WordTimestamps[i].Start + 0.25;
                    if (currentTime <= (float)nextWord)
                        return "Word: " + word.Word;
                }
            }
            
            // Some reason this is not working.
            //var currentWord = currentMessage?.WordTimestamps.FirstOrDefault(word => currentTime >= word.Start && currentTime < word.End);
            
            //return currentWord != null ? $"Word: {currentWord.Word}" : "Word: ...";
            return "Word: ...";
        }

        public float Pitch
        {
            get
            {
                return sound.Pitch;
            }
            set
            {
                sound.Pitch = value;
            }
        }

        public float Volume
        {
            get
            {
                return sound.Volume;
            }
            set
            {
                sound.Volume = value;
            }
        }

        public void Dispose()
        {
            sound.Dispose();
        }
    }
}
