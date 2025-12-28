using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;
using AAYInvisionaryTTSPlayer.Services.ErrorHandler;
using ChatterboxTTSNet;
using AAYInvisionaryTTSPlayer.Services.PlayerService;
using AAYInvisionaryTTSPlayer.Utilities;

namespace AAYInvisionaryTTSPlayer.Services.ConnectionService;

public class PythonConnection(IErrorHandler handler) : IConnection
{
    private bool initialized;
    ConcurrentQueue<TTSResult> messageQueue = new ConcurrentQueue<TTSResult>();
    
    public async Task<bool> Connect()
    {
        try
        {
            if (!initialized)
            {
                await Task.Run(ChatterboxTTSFactory.Initialize);
                initialized = true;
            }
            return initialized;
        }
        catch (Exception e)
        {
            await handler.ErrorPlayer(0, $"The Player failed to initialize required Python TTS backend. {e.Message}");
            return initialized;
        }
    }

    public async Task<bool> Disconnect()
    {
        await Task.Run(ChatterboxTTSFactory.Uninitialize);
        try
        {
            if (initialized)
            {
                await Task.Run(ChatterboxTTSFactory.Initialize);
                initialized = false;
            }
            return !initialized;
        }
        catch (Exception e)
        {
            await handler.ErrorPlayer(0, $"The Player failed to uninitialize required Python TTS backend. {e.Message}");
            return false;
        }
    }

    public async Task Send(string message, string TTSVoice)
    {
        try
        {
            if (TTSVoice == "Invalid")
                throw new InvalidDataException("Must have a reference file selected to run text to speech backend.");

            var audio = TTSVoice != "Default" && TTSVoice != String.Empty ? AudioLoader.LoadAudioFromFile(TTSVoice) : ([], 44100);
            //Console.WriteLine(audio.SampleRate);
            //messageQueue.Enqueue(new TTSResult {BitRate = audio.SampleRate, ChannelCount = 1, AudioBuffer = ByteManager.ShortArrayToByteArray(audio.Samples), MessageType = "Samples"});
            short[] samples = audio.Samples;
            long sampleRate = audio.SampleRate;
            var result = await Task.Run<(long sampleRate, short[] audioSamples, List<WordTimestamp> timeStamp)>(() => ChatterboxTTSFactory.GenerateAudio(message, samples, sampleRate));
            messageQueue.Enqueue(new TTSResult()
            {
                AudioBuffer = ByteManager.ShortArrayToByteArray(result.audioSamples), 
                BitRate = (int)result.sampleRate,
                WordTimestamps = result.timeStamp,
                MessageType = "Samples"
            });
        }
        catch (Exception e)
        {
            await handler.ErrorPlayer(0, $"The Player failed to send the request to the TTS backend. {e.Message} \n\r{e.StackTrace}");
        }
    }

    public async Task<TTSResult> Received()
    {
        if (initialized && messageQueue.TryDequeue(out var message))
            return message;
        await handler.ErrorPlayer(0, $"The Player failed to send the request to the TTS backend and tried to retrieve uninitialized TTS messages.");
        return null;
    }
}