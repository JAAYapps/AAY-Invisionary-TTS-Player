using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using NAudio.Wave;
using NVorbis;
using Silk.NET.OpenAL;
using AAYInvisionaryTTSPlayer.Utilities;

namespace AAYInvisionaryTTSPlayer.Services.PlayerService;

public class SilkNetPlayer : IPlayer, IDisposable
{
    private readonly AL _al;
    private readonly ALContext _alContext;
    private unsafe Device* _device;
    private unsafe Context* _context;

    // OpenAL Resources
    private uint _sourceId;
    private uint _currentBufferId;

    // Queue and Threading
    private readonly ConcurrentQueue<TTSResult> _playbackQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _queueProcessingTask;
    private TTSResult _currentMessage = new();

    // State
    private float _volume = 100f;
    private float _pitch = 1f;

    public unsafe SilkNetPlayer()
    {
        _al = AL.GetApi();
        _alContext = ALContext.GetApi();

        // 1. Open Device
        _device = _alContext.OpenDevice("");
        if (_device == null)
            throw new Exception("Could not open audio device.");

        // 2. Create Context (but don't make current yet, we do that in the thread)
        _context = _alContext.CreateContext(_device, null);
        
        // 3. Start the Background Loop
        _queueProcessingTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
    }

    /// <summary>
    /// The main background loop. Handles OpenAL context and sequential playback.
    /// </summary>
    private async Task ProcessQueueAsync(CancellationToken token)
    {
        unsafe
        {
            _alContext.MakeContextCurrent(_context);
        }
        
        _sourceId = _al.GenSource();
        _al.SetSourceProperty(_sourceId, SourceBoolean.Looping, false);
        Console.WriteLine($"[SilkNetPlayer] Playing source: {_sourceId}");
        while (!token.IsCancellationRequested)
        {
            _al.SetSourceProperty(_sourceId, SourceFloat.Gain, _volume / 100f);
            _al.SetSourceProperty(_sourceId, SourceFloat.Pitch, _pitch);

            _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out int stateVal);
            var state = (SourceState)stateVal;
            if (state is SourceState.Stopped or SourceState.Initial && _playbackQueue.TryDequeue(out var message))
            {
                _currentMessage = message;
                PlayNextMessage(message);
            }

            await Task.Delay(50, token);
        }
        
        _al.DeleteSource(_sourceId);
        if (_currentBufferId != 0) _al.DeleteBuffer(_currentBufferId);
    }

    private void PlayNextMessage(TTSResult message)
    {
        PcmSound? soundData = null;

        try
        {
            // Case A: Raw Samples (Byte array representing Shorts or Floats)
            if (message.MessageType == "Samples" && message.AudioBuffer != null)
            {
                short[] pcm = ByteManager.ByteArrayToShortArray(message.AudioBuffer);
                
                soundData = new PcmSound(pcm, BufferFormat.Mono16, (int)(message.BitRate != 0 ? message.BitRate : 24000));
            }
            // Case B: Encoded File (WAV, OGG, MP3 bytes)
            else if (message.MessageType == "File" && message.AudioBuffer != null)
            {
                using var ms = new MemoryStream(message.AudioBuffer);
                soundData = DecodeStream(ms);
            }

            if (soundData != null)
            {
                if (_currentBufferId != 0)
                {
                    _al.SetSourceProperty(_sourceId, SourceInteger.Buffer, 0); // Detach
                    _al.DeleteBuffer(_currentBufferId);
                }

                _currentBufferId = _al.GenBuffer();
                _al.BufferData(_currentBufferId, soundData.Format, soundData.PcmData, soundData.SampleRate);
                _al.SetSourceProperty(_sourceId, SourceInteger.Buffer, (int)_currentBufferId);
                _al.SourcePlay(_sourceId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SilkNetPlayer] Error playing message: {ex.Message}");
        }
    }

    // --- Decoding Logic ---

    private PcmSound? DecodeStream(Stream audio)
    {
        // Read header to detect format
        byte[] header = new byte[4];
        if (audio.Read(header, 0, 4) < 4) return null;
        audio.Seek(0, SeekOrigin.Begin);
        string headerString = Encoding.ASCII.GetString(header);

        if (headerString.StartsWith("RIFF")) return DecodeWav(audio);
        if (headerString.StartsWith("OggS")) return DecodeOgg(audio);
        if ((header[0] == 0xFF && (header[1] & 0xE0) == 0xE0) || headerString.StartsWith("ID3")) return DecodeMp3(audio);

        return null; // Unknown
    }

    private PcmSound DecodeOgg(Stream stream)
    {
        using var vorbis = new VorbisReader(stream, true);
        short[] pcmBuffer = new short[vorbis.TotalSamples * vorbis.Channels];
        // NVorbis reads into float, we manually convert to short or let it handle reading samples?
        // NVorbis ReadSamples usually takes floats. Let's do float -> short.
        float[] floatBuf = new float[pcmBuffer.Length];
        vorbis.ReadSamples(floatBuf, 0, floatBuf.Length);
        
        for (int i = 0; i < floatBuf.Length; i++)
            pcmBuffer[i] = (short)(floatBuf[i] * short.MaxValue);

        return new PcmSound(pcmBuffer, vorbis.Channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16, vorbis.SampleRate);
    }

    private PcmSound DecodeWav(Stream stream)
    {
        using var reader = new WaveFileReader(stream);
        return DecodeWaveStream(reader);
    }

    private PcmSound DecodeMp3(Stream stream)
    {
        using var reader = new Mp3FileReader(stream);
        return DecodeWaveStream(reader);
    }

    private PcmSound DecodeWaveStream(WaveStream stream)
    {
        var sampleProvider = stream.ToSampleProvider(); // Automatically converts to IEEE Float
        int channels = sampleProvider.WaveFormat.Channels;
        int rate = sampleProvider.WaveFormat.SampleRate;
        
        // Read all into memory
        List<float> allSamples = new();
        float[] buffer = new float[rate * channels]; // 1 sec buffer
        int read;
        while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
        {
            allSamples.AddRange(buffer.Take(read));
        }

        // Convert Float to Short
        short[] pcm = new short[allSamples.Count];
        for (int i = 0; i < allSamples.Count; i++)
        {
            // Clip to prevent overflow
            float val = allSamples[i];
            if (val > 1.0f) val = 1.0f;
            if (val < -1.0f) val = -1.0f;
            pcm[i] = (short)(val * short.MaxValue);
        }

        return new PcmSound(pcm, channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16, rate);
    }

    private record PcmSound(short[] PcmData, BufferFormat Format, int SampleRate);

    // --- Interface Implementation ---

    public void AddToQueue(TTSResult message)
    {
        if (message.MessageType is "File" or "Samples")
        {
            _playbackQueue.Enqueue(message);
        }
    }

    public void Play() 
    {
        if(_sourceId != 0) _al.SourcePlay(_sourceId);
    } 

    public void Pause() 
    {
        if (_sourceId != 0) _al.SourcePause(_sourceId);
    }

    public bool Stop()
    {
        _playbackQueue.Clear();
        if (_sourceId != 0) _al.SourceStop(_sourceId);
        _currentMessage = new() { MessageType = "Null" };
        return true;
    }

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 100f);
    }

    public float Pitch
    {
        get => _pitch;
        set => _pitch = value;
    }

    public IPlayer.SoundStatus GetPlayStatus()
    {
        // If queue has items, we consider ourselves "Active" even if momentarily between buffers
        if (!_playbackQueue.IsEmpty) return IPlayer.SoundStatus.Playing;

        _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out int state);
        return (SourceState)state == SourceState.Playing ? IPlayer.SoundStatus.Playing : IPlayer.SoundStatus.Stopped;
    }

    public string GetWord()
    {
        _al.GetSourceProperty(_sourceId, GetSourceInteger.SourceState, out int state);
        if ((SourceState)state != SourceState.Playing || _currentMessage.WordTimestamps == null)
            return "Word: ";

        // OpenAL gives offset in Seconds or Samples. Let's use Seconds.
        _al.GetSourceProperty(_sourceId, SourceFloat.SecOffset, out float currentTime);

        var currentWord = _currentMessage.WordTimestamps
            .FirstOrDefault(word => currentTime >= word.Start && currentTime < word.End);

        return currentWord != null ? $"Word: {currentWord.Word}" : "Word: ...";
    }

    public async Task WaitForPlaybackFinishAsync()
    {
        while (GetPlayStatus() != IPlayer.SoundStatus.Stopped)
        {
            await Task.Delay(100);
        }
    }

    public unsafe void Dispose()
    {
        _cts.Cancel();
        try
        {
            _queueProcessingTask.Wait(500); // Give it a moment to cleanup source
        }
        catch { /* Ignore cancellation errors */ }

        _cts.Dispose();
        
        // Context destruction should ideally happen, 
        // but since we made it current on a background thread, 
        // strictly speaking we should close it there or reset current context.
        // For simplicity in simple apps, closing device usually suffices:
        
        _alContext.DestroyContext(_context);
        _alContext.CloseDevice(_device);
        
        _al.Dispose();
        _alContext.Dispose();
    }
}