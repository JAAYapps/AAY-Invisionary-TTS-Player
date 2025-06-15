using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AAYInvisionaryTTSPlayer.Models;

namespace AAYInvisionaryTTSPlayer.Services.TTSService;

public interface ITtsService
{
    public Task StartBackend();

    public bool IsBackendSendingErrors();

    public bool IsBackendRunning();

    public Task StopBackend();
    
    /// <summary>
    /// Generates speech for the given text and returns a complete result.
    /// </summary>
    Task<TTSResult> GenerateSpeechAsync(string text, string voiceId);
}