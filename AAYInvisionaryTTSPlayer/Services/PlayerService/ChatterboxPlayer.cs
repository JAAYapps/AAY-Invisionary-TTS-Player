using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using SFML.Audio;

namespace AAYInvisionaryTTSPlayer.Services.PlayerService;

public class ChatterboxPlayer : IPlayer, IDisposable
{
    private readonly Sound _sound = new();
    
    // A thread-safe queue to hold upcoming audio clips and their timestamps.
    private readonly ConcurrentQueue<TTSResult> _playbackQueue = new();
    
    private TTSResult _currentMessage = new();
    
    // A token to allow for clean shutdown of the background task.
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _queueProcessingTask;
    
    private bool started = false;
    
    public ChatterboxPlayer()
    {
        // Start a single, long-running background task to process the audio queue.
        _queueProcessingTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }
    
    public void AddToQueue(TTSResult message)
    {
        _playbackQueue.Enqueue(message);
        started = true;
    }
    
    /// <summary>
    /// The main loop that processes the playback queue.
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            // If the player is stopped and there's something in the queue...
            if (_sound.Status == SoundStatus.Stopped && _playbackQueue.TryDequeue(out var message))
            {
                // Play the next item.
                _currentMessage = message;
                _sound.SoundBuffer = _currentMessage.AudioBuffer;
                _sound.Play();
            }
            else if (_sound.Status == SoundStatus.Stopped)
                started = false;
            // Wait for a short period before checking again. This is more responsive than Thread.Sleep(1000).
            await Task.Delay(100, token);
        }
    }
    
    /// <summary>
    /// Stops playback and clears the queue completely.
    /// </summary>
    public bool Stop()
    {
        started = false;
        _sound.Stop();
        _playbackQueue.Clear();
        _currentMessage = null;
        return true;
    }

    public SoundStatus GetPlayStatus()
    {
        if (started && _sound.Status is SoundStatus.Stopped or SoundStatus.Paused)
            return SoundStatus.Paused;
        else if (started && _sound.Status == SoundStatus.Playing)
            return SoundStatus.Playing;
        return SoundStatus.Stopped;
    }

    public void Play() => _sound.Play();

    public void Pause() => _sound.Pause();

    public string GetWord()
    {
        if (_sound.Status != SoundStatus.Playing || _currentMessage == null)
            return "Word: ";
        
        var currentTime = _sound.PlayingOffset.AsSeconds();
        
        var currentWord = _currentMessage.WordTimestamps.FirstOrDefault(word => currentTime >= word.Start && currentTime < word.End);
        
        return currentWord != null ? $"Word: {currentWord.Word}" : "Word: ...";
    }

    public float Pitch
    {
        get => _sound.Pitch;
        set => _sound.Pitch = value;
    }

    public float Volume
    {
        get => _sound.Volume;
        set => _sound.Volume = value;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _sound.Dispose();
    }
}